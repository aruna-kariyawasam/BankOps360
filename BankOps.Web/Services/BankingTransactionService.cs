
using System.Data;
using BankOps.Web.Data;
using BankOps.Web.Models.Banking;
using BankOps.Web.Models.Banking.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BankOps.Web.Services;

public sealed class BankingTransactionService : IBankingTransactionService
{
    private const string CashLedgerCode = "CASH-LKR";
    private const decimal MaximumBalance = 9999999999999999.99m;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<BankingTransactionService> _logger;

    public BankingTransactionService(
        ApplicationDbContext context,
        ILogger<BankingTransactionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<TransactionResponse> DepositAsync(
    DepositRequest request, string? userId)
    {
        return ExecuteAsync(
            TransactionType.Deposit,
            null,
            request.Amount,
            request.IdempotencyKey,
            request.Description,
            userId,
            request.AccountId);
    }

    public Task<TransactionResponse> WithdrawAsync(
        WithdrawalRequest request, string? userId)
    {
        return ExecuteAsync(
            TransactionType.Withdrawal,
            request.AccountId,
            request.Amount,
            request.IdempotencyKey,
            request.Description,
            userId);
    }

    public Task<TransactionResponse> TransferAsync(
        TransferRequest request, string? userId)
    {
        if (request.FromAccountId == request.ToAccountId)
        {
            throw new BankingOperationException(
                400, "Sender and receiver accounts must be different.");
        }

        return ExecuteAsync(
            TransactionType.InternalTransfer,
            request.FromAccountId,
            request.Amount,
            request.IdempotencyKey,
            request.Description,
            userId,
            request.ToAccountId);
    }

    private async Task<TransactionResponse> ExecuteAsync(
        TransactionType type,
        int? fromAccountId,
        decimal amount,
        string idempotencyKey,
        string? description,
        string? userId,
        int? toAccountId = null)
    {
        // 1. Validate financial input
        if (amount <= 0 || amount > MaximumBalance ||
            decimal.Round(amount, 2) != amount)
        {
            throw new BankingOperationException(
                400, "Invalid transaction amount.");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey) ||
            idempotencyKey.Trim().Length < 8 ||
            idempotencyKey.Trim().Length > 64)
        {
            throw new BankingOperationException(
                400, "A valid idempotency key is required.");
        }

        idempotencyKey = idempotencyKey.Trim();
        description = description?.Trim();


        if (type == TransactionType.Withdrawal)
        {
            toAccountId = null;
        }

        // 2. Begin an atomic database transaction
        await using var dbTransaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted);

        try
        {
            // 3. Check whether this request was already processed
            var existing = await _context.BankTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.IdempotencyKey == idempotencyKey);

            if (existing != null)
            {
                if (!MatchesRequest(
                    existing, type, amount,
                    fromAccountId, toAccountId, description))
                {
                    throw new BankingOperationException(
                        409,
                        "Idempotency key has already been used " +
                        "for a different transaction.");
                }

                await dbTransaction.CommitAsync();

                return MapResponse(existing, true);
            }

            // 4. Load customer accounts in a consistent order
            var accountIds = new[]
                {
                    fromAccountId,
                    toAccountId
                }
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            var accounts = await _context.BankAccounts
                .Where(x => accountIds.Contains(x.Id))
                .OrderBy(x => x.Id)
                .ToListAsync();

            if (accounts.Count != accountIds.Length)
            {
                throw new BankingOperationException(
                    404, "One or more bank accounts were not found.");
            }

            var fromAccount = fromAccountId.HasValue
                ? accounts.Single(x => x.Id == fromAccountId.Value)
                : null;

            var toAccount = toAccountId.HasValue
                ? accounts.Single(x => x.Id == toAccountId.Value)
                : null;

            // 5. Validate account status and currency
            foreach (var account in accounts)
            {
                if (account.Status != AccountStatus.Active)
                {
                    throw new BankingOperationException(
                        409,
                        $"Account {account.AccountNumber} is not active.");
                }

                if (account.Currency != "LKR")
                {
                    throw new BankingOperationException(
                        400,
                        "Only LKR accounts are supported.");
                }
            }

            // 6. Load the bank's cash ledger account
            var cashLedger = await _context.GeneralLedgerAccounts
                .SingleOrDefaultAsync(
                    x => x.Code == CashLedgerCode);

            if (cashLedger == null || !cashLedger.IsActive ||
                cashLedger.Currency != "LKR")
            {
                throw new BankingOperationException(
                    500,
                    "The LKR cash ledger account is not configured.");
            }

            // 7. Validate balances and calculate new balances
            if (type == TransactionType.Withdrawal ||
                type == TransactionType.InternalTransfer)
            {
                if (fromAccount == null ||
                    fromAccount.CurrentBalance < amount)
                {
                    throw new BankingOperationException(
                        409, "Insufficient account balance.");
                }
            }

            if (toAccount != null &&
                toAccount.CurrentBalance > MaximumBalance - amount)
            {
                throw new BankingOperationException(
                    409, "Receiver account balance limit exceeded.");
            }

            if (fromAccount != null &&
                (type == TransactionType.Withdrawal ||
                 type == TransactionType.InternalTransfer))
            {
                fromAccount.CurrentBalance -= amount;
            }

            if (toAccount != null)
            {
                toAccount.CurrentBalance += amount;
            }

            // 8. Create the transaction header
            var now = DateTime.UtcNow;

            var bankTransaction = new BankTransaction
            {
                TransactionReference =
                    "TXN-" + Guid.NewGuid().ToString("N").ToUpperInvariant(),

                IdempotencyKey = idempotencyKey,
                Type = type,
                Status = TransactionStatus.Completed,
                Amount = amount,
                Currency = "LKR",
                Description = description,
                FromAccountId = fromAccountId,
                ToAccountId = toAccountId,
                InitiatedByUserId = userId,
                CreatedAtUtc = now,
                CompletedAtUtc = now
            };

            var entries = new List<TransactionEntry>();

            // 9. Create the double-entry ledger postings
            if (type == TransactionType.Deposit)
            {
                // Debit bank cash asset
                entries.Add(new TransactionEntry
                {
                    GeneralLedgerAccount = cashLedger,
                    EntryType = EntryType.Debit,
                    Amount = amount,
                    CreatedAtUtc = now
                });

                // Credit customer deposit liability
                entries.Add(new TransactionEntry
                {
                    BankAccount = toAccount!,
                    EntryType = EntryType.Credit,
                    Amount = amount,
                    CreatedAtUtc = now
                });
            }
            else if (type == TransactionType.Withdrawal)
            {
                // Debit customer deposit liability
                entries.Add(new TransactionEntry
                {
                    BankAccount = fromAccount!,
                    EntryType = EntryType.Debit,
                    Amount = amount,
                    CreatedAtUtc = now
                });

                // Credit bank cash asset
                entries.Add(new TransactionEntry
                {
                    GeneralLedgerAccount = cashLedger,
                    EntryType = EntryType.Credit,
                    Amount = amount,
                    CreatedAtUtc = now
                });
            }
            else if (type == TransactionType.InternalTransfer)
            {
                // Debit sender
                entries.Add(new TransactionEntry
                {
                    BankAccount = fromAccount!,
                    EntryType = EntryType.Debit,
                    Amount = amount,
                    CreatedAtUtc = now
                });

                // Credit receiver
                entries.Add(new TransactionEntry
                {
                    BankAccount = toAccount!,
                    EntryType = EntryType.Credit,
                    Amount = amount,
                    CreatedAtUtc = now
                });
            }
            else
            {
                throw new BankingOperationException(
                    400, "Unsupported transaction type.");
            }

            // 10. Verify that the ledger is balanced
            var totalDebits = entries
                .Where(x => x.EntryType == EntryType.Debit)
                .Sum(x => x.Amount);

            var totalCredits = entries
                .Where(x => x.EntryType == EntryType.Credit)
                .Sum(x => x.Amount);

            if (totalDebits != totalCredits ||
                totalDebits != amount ||
                totalCredits != amount)
            {
                throw new BankingOperationException(
                    500, "Double-entry ledger validation failed.");
            }

            bankTransaction.Entries = entries;

            _context.BankTransactions.Add(bankTransaction);

            // 11. Create an audit record in the same transaction
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "FINANCIAL_TRANSACTION_COMPLETED",
                EntityName = "BankTransaction",
                EntityId = bankTransaction.TransactionReference,
                Details =
                    $"Type={type}; Amount={amount}; Currency=LKR; " +
                    $"FromAccountId={fromAccountId}; ToAccountId={toAccountId}",
                CreatedAtUtc = now
            });

            // 12. Persist all changes atomically
            await _context.SaveChangesAsync();

            await dbTransaction.CommitAsync();

            _logger.LogInformation(
                "Transaction {Reference} completed. Type={Type}, Amount={Amount}",
                bankTransaction.TransactionReference,
                type,
                amount);

            return MapResponse(bankTransaction, false);
        }
        catch (BankingOperationException)
        {
            await dbTransaction.RollbackAsync();
            _context.ChangeTracker.Clear();
            throw;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await dbTransaction.RollbackAsync();
            _context.ChangeTracker.Clear();

            _logger.LogWarning(
                ex, "Concurrent account update detected.");

            throw new BankingOperationException(
                409,
                "An account was modified by another transaction. " +
                "Please refresh the account and retry with a new request key.");
        }
        catch (DbUpdateException ex)
        {
            await dbTransaction.RollbackAsync();
            _context.ChangeTracker.Clear();

            // Another simultaneous request may have used the same key.
            var existing = await _context.BankTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.IdempotencyKey == idempotencyKey);

            if (existing != null)
            {
                if (MatchesRequest(
                    existing, type, amount,
                    fromAccountId, toAccountId, description))
                {
                    return MapResponse(existing, true);
                }

                throw new BankingOperationException(
                    409,
                    "Idempotency key has already been used.");
            }

            _logger.LogError(ex, "Database transaction failed.");

            throw new BankingOperationException(
                500, "The financial transaction could not be completed.");
        }
    }

    private static bool MatchesRequest(
        BankTransaction existing,
        TransactionType type,
        decimal amount,
        int? fromAccountId,
        int? toAccountId,
        string? description)
    {
        return existing.Type == type &&
               existing.Amount == amount &&
               existing.Currency == "LKR" &&
               existing.FromAccountId == fromAccountId &&
               existing.ToAccountId == toAccountId &&
               existing.Description == description;
    }

    private static TransactionResponse MapResponse(
        BankTransaction transaction,
        bool isDuplicate)
    {
        return new TransactionResponse(
            transaction.Id,
            transaction.TransactionReference,
            transaction.Type,
            transaction.Status,
            transaction.Amount,
            transaction.Currency,
            transaction.FromAccountId,
            transaction.ToAccountId,
            transaction.CreatedAtUtc,
            transaction.CompletedAtUtc,
            isDuplicate);
    }
}

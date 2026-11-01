
using System.Data;
using System.Text.Json;
using BankOps.Web.Data;
using BankOps.Web.Models.Banking;
using BankOps.Web.Models.Banking.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BankOps.Web.Services;

public sealed class PaymentProcessingService : IPaymentProcessingService
{
    private const string ClearingLedgerCode = "CLEARING-LKR";
    private const decimal MaximumBalance = 9999999999999999.99m;

    private readonly ApplicationDbContext _context;
    private readonly IClearingBankClient _clearingClient;
    private readonly ILogger<PaymentProcessingService> _logger;
    private readonly IAmlMonitoringService _amlMonitoring;

    public PaymentProcessingService(
    ApplicationDbContext context,
    IClearingBankClient clearingClient,
    IAmlMonitoringService amlMonitoring,
    ILogger<PaymentProcessingService> logger)
    {
        _context = context;
        _clearingClient = clearingClient;
        _amlMonitoring = amlMonitoring;
        _logger = logger;
    }

    public async Task<PaymentResponse> ProcessPaymentAsync(
        CreatePaymentRequest request,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var reservation = await ReservePaymentAsync(
            request, userId, cancellationToken);

        // Never submit a payment again merely because the caller
        // repeated an existing idempotency key.
        if (reservation.IsDuplicate)
            return MapResponse(reservation.Payment, true);

        var paymentId = reservation.Payment.Id;

        var clearingRequest = new ClearingPaymentRequest(
            reservation.Payment.PaymentReference,
            reservation.Payment.IdempotencyKey,
            reservation.Payment.DestinationBankCode,
            reservation.Payment.DestinationAccountNumber,
            reservation.Payment.BeneficiaryName,
            reservation.Payment.Amount,
            "LKR");

        ClearingPaymentResult clearingResult;

        try
        {
            clearingResult = await _clearingClient.SubmitAsync(
                clearingRequest,
                cancellationToken);
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is OperationCanceledException ||
            ex is System.Text.Json.JsonException)
        {
            _logger.LogError(
                ex,
                "Payment {PaymentId} has an unknown external outcome.",
                paymentId);

            await RecordUnknownOutcomeAsync(paymentId);

            var pending = await LoadPaymentAsync(paymentId);

            return MapResponse(pending, false);
        }

        if (clearingResult.IsAccepted)
        {
            await CompletePaymentAsync(
                paymentId,
                clearingResult,
                cancellationToken);
        }
        else
        {
            await ReverseRejectedPaymentAsync(
                paymentId,
                clearingResult,
                cancellationToken);
        }

        var finalPayment = await LoadPaymentAsync(paymentId);

        // Evaluate successfully completed external payments.
        if (finalPayment.BankTransaction.Status ==
            TransactionStatus.Completed)
        {
            try
            {
                await _amlMonitoring.EvaluateTransactionAsync(
                    finalPayment.BankTransaction.Id,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "AML evaluation failed for payment {PaymentReference}.",
                    finalPayment.PaymentReference);
            }
        }

        return MapResponse(finalPayment, false);
    }

    public async Task<PaymentResponse?> GetPaymentAsync(
        string paymentReference,
        CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .AsNoTracking()
            .Include(x => x.BankTransaction)
            .FirstOrDefaultAsync(
                x => x.PaymentReference == paymentReference,
                cancellationToken);

        return payment == null
            ? null
            : MapResponse(payment, false);
    }

    private static void ValidateRequest(CreatePaymentRequest request)
    {
        if (request.SourceAccountId <= 0 ||
            request.Amount <= 0 ||
            request.Amount > MaximumBalance ||
            decimal.Round(request.Amount, 2) != request.Amount)
        {
            throw new BankingOperationException(
                400, "Invalid payment account or amount.");
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) ||
            request.IdempotencyKey.Trim().Length < 8 ||
            request.IdempotencyKey.Trim().Length > 64)
        {
            throw new BankingOperationException(
                400, "Invalid payment idempotency key.");
        }

        if (string.IsNullOrWhiteSpace(request.DestinationBankCode) ||
            string.IsNullOrWhiteSpace(request.DestinationAccountNumber) ||
            string.IsNullOrWhiteSpace(request.BeneficiaryName))
        {
            throw new BankingOperationException(
                400, "Destination payment details are required.");
        }
    }

    private sealed record ReservationResult(
        Payment Payment,
        bool IsDuplicate);

    private async Task<ReservationResult> ReservePaymentAsync(
        CreatePaymentRequest request,
        string? userId,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = request.IdempotencyKey.Trim();

        var bankCode = request.DestinationBankCode.Trim();
        var destinationAccount = request.DestinationAccountNumber.Trim();
        var beneficiary = request.BeneficiaryName.Trim();
        var description = request.Description?.Trim();

        await using var dbTransaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            // Check for an existing payment.
            var existing = await _context.Payments
                .Include(x => x.BankTransaction)
                .FirstOrDefaultAsync(
                    x => x.IdempotencyKey == idempotencyKey,
                    cancellationToken);

            if (existing != null)
            {
                if (!MatchesRequest(
                    existing, request, bankCode,
                    destinationAccount, beneficiary, description))
                {
                    throw new BankingOperationException(
                        409,
                        "Idempotency key already belongs to another payment.");
                }

                await dbTransaction.CommitAsync(cancellationToken);

                return new ReservationResult(existing, true);
            }

            // Load the source account.
            var account = await _context.BankAccounts
                .SingleOrDefaultAsync(
                    x => x.Id == request.SourceAccountId,
                    cancellationToken);

            if (account == null)
            {
                throw new BankingOperationException(
                    404, "Source account was not found.");
            }

            if (account.Status != AccountStatus.Active ||
                account.Currency != "LKR")
            {
                throw new BankingOperationException(
                    409, "Source account is not eligible for payment.");
            }

            if (account.CurrentBalance < request.Amount)
            {
                throw new BankingOperationException(
                    409, "Insufficient account balance.");
            }

            var clearingLedger = await _context.GeneralLedgerAccounts
                .SingleOrDefaultAsync(
                    x => x.Code == ClearingLedgerCode &&
                         x.IsActive &&
                         x.Currency == "LKR",
                    cancellationToken);

            if (clearingLedger == null)
            {
                throw new BankingOperationException(
                    500, "External clearing ledger is not configured.");
            }

            var now = DateTime.UtcNow;

            var transactionReference =
                "PAY-TXN-" + Guid.NewGuid().ToString("N").ToUpperInvariant();

            var paymentReference =
                "PAY-" + Guid.NewGuid().ToString("N").ToUpperInvariant();

            var clearingRequest = new ClearingPaymentRequest(
                paymentReference,
                idempotencyKey,
                bankCode,
                destinationAccount,
                beneficiary,
                request.Amount,
                "LKR");

            // Reserve the source account funds.
            account.CurrentBalance -= request.Amount;

            // Create pending bank transaction.
            var bankTransaction = new BankTransaction
            {
                TransactionReference = transactionReference,
                IdempotencyKey = idempotencyKey,
                Type = TransactionType.ExternalPayment,
                Status = TransactionStatus.Pending,
                Amount = request.Amount,
                Currency = "LKR",
                Description = description,
                FromAccountId = account.Id,
                InitiatedByUserId = userId,
                CreatedAtUtc = now
            };

            // Double-entry posting:
            // Debit customer deposit liability.
            // Credit external clearing payable.
            bankTransaction.Entries.Add(
                new TransactionEntry
                {
                    BankAccount = account,
                    EntryType = EntryType.Debit,
                    Amount = request.Amount,
                    CreatedAtUtc = now
                });

            bankTransaction.Entries.Add(
                new TransactionEntry
                {
                    GeneralLedgerAccount = clearingLedger,
                    EntryType = EntryType.Credit,
                    Amount = request.Amount,
                    CreatedAtUtc = now
                });

            var payment = new Payment
            {
                PaymentReference = paymentReference,
                IdempotencyKey = idempotencyKey,
                BankTransaction = bankTransaction,
                DestinationBankCode = bankCode,
                DestinationAccountNumber = destinationAccount,
                BeneficiaryName = beneficiary,
                Amount = request.Amount,
                Status = PaymentStatus.Processing,
                RequestPayload = JsonSerializer.Serialize(clearingRequest),
                CreatedAtUtc = now
            };

            _context.BankTransactions.Add(bankTransaction);
            _context.Payments.Add(payment);

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "EXTERNAL_PAYMENT_INITIATED",
                EntityName = "Payment",
                EntityId = paymentReference,
                Details =
                    $"SourceAccountId={account.Id}; Amount={request.Amount}; " +
                    $"Currency=LKR",
                CreatedAtUtc = now
            });

            await _context.SaveChangesAsync(cancellationToken);

            await dbTransaction.CommitAsync(cancellationToken);

            return new ReservationResult(payment, false);
        }
        catch
        {
            await dbTransaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static bool MatchesRequest(
        Payment payment,
        CreatePaymentRequest request,
        string bankCode,
        string destinationAccount,
        string beneficiary,
        string? description)
    {
        return payment.Amount == request.Amount &&
               payment.BankTransaction.FromAccountId ==
                   request.SourceAccountId &&
               payment.DestinationBankCode == bankCode &&
               payment.DestinationAccountNumber == destinationAccount &&
               payment.BeneficiaryName == beneficiary &&
               payment.BankTransaction.Description == description;
    }

    private async Task CompletePaymentAsync(
        long paymentId,
        ClearingPaymentResult result,
        CancellationToken cancellationToken)
    {
        await using var dbTransaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var payment = await _context.Payments
                .Include(x => x.BankTransaction)
                .SingleAsync(x => x.Id == paymentId, cancellationToken);

            if (payment.Status != PaymentStatus.Processing)
            {
                throw new BankingOperationException(
                    409, "Payment is no longer awaiting completion.");
            }

            var now = DateTime.UtcNow;

            payment.Status = PaymentStatus.Successful;
            payment.ExternalReference = result.ExternalReference;
            payment.ResponsePayload = result.RawResponse;
            payment.UpdatedAtUtc = now;

            payment.BankTransaction.Status =
                TransactionStatus.Completed;

            payment.BankTransaction.CompletedAtUtc = now;

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "EXTERNAL_PAYMENT_ACCEPTED",
                EntityName = "Payment",
                EntityId = payment.PaymentReference,
                Details = $"ExternalReference={result.ExternalReference}",
                CreatedAtUtc = now
            });

            await _context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await dbTransaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task ReverseRejectedPaymentAsync(
        long paymentId,
        ClearingPaymentResult result,
        CancellationToken cancellationToken)
    {
        await using var dbTransaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            var payment = await _context.Payments
                .Include(x => x.BankTransaction)
                .SingleAsync(x => x.Id == paymentId, cancellationToken);

            if (payment.Status != PaymentStatus.Processing)
            {
                throw new BankingOperationException(
                    409, "Payment is no longer awaiting reversal.");
            }

            var accountId =
                payment.BankTransaction.FromAccountId
                ?? throw new InvalidOperationException(
                    "Payment source account is missing.");

            var account = await _context.BankAccounts
                .SingleAsync(x => x.Id == accountId, cancellationToken);

            var clearingLedger = await _context.GeneralLedgerAccounts
                .SingleAsync(
                    x => x.Code == ClearingLedgerCode,
                    cancellationToken);

            if (account.CurrentBalance >
                MaximumBalance - payment.Amount)
            {
                throw new BankingOperationException(
                    409, "Account balance limit prevents payment reversal.");
            }

            var now = DateTime.UtcNow;

            // Restore funds to the source account.
            account.CurrentBalance += payment.Amount;

            // Mark original posting as reversed.
            payment.Status = PaymentStatus.Failed;
            payment.ResponsePayload = result.RawResponse;
            payment.UpdatedAtUtc = now;

            payment.BankTransaction.Status =
                TransactionStatus.Reversed;

            payment.BankTransaction.CompletedAtUtc = now;

            // Create a separate compensating transaction.
            var reversal = new BankTransaction
            {
                TransactionReference =
                    "REV-" + Guid.NewGuid().ToString("N").ToUpperInvariant(),

                IdempotencyKey = "PAY-REV-" + payment.Id,
                Type = TransactionType.ExternalPayment,
                Status = TransactionStatus.Completed,
                Amount = payment.Amount,
                Currency = "LKR",
                Description =
                    $"Reversal of rejected payment {payment.PaymentReference}",
                ToAccountId = account.Id,
                CreatedAtUtc = now,
                CompletedAtUtc = now
            };

            // Reverse debit/credit postings.
            reversal.Entries.Add(new TransactionEntry
            {
                GeneralLedgerAccount = clearingLedger,
                EntryType = EntryType.Debit,
                Amount = payment.Amount,
                CreatedAtUtc = now
            });

            reversal.Entries.Add(new TransactionEntry
            {
                BankAccount = account,
                EntryType = EntryType.Credit,
                Amount = payment.Amount,
                CreatedAtUtc = now
            });

            _context.BankTransactions.Add(reversal);

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "EXTERNAL_PAYMENT_REVERSED",
                EntityName = "Payment",
                EntityId = payment.PaymentReference,
                Details =
                    $"Reason={result.Message}; " +
                    $"ReversalReference={reversal.TransactionReference}",
                CreatedAtUtc = now
            });

            await _context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await dbTransaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task RecordUnknownOutcomeAsync(long paymentId)
    {
        await using var dbTransaction =
            await _context.Database.BeginTransactionAsync();

        var payment = await _context.Payments
            .Include(x => x.BankTransaction)
            .SingleAsync(x => x.Id == paymentId);

        // Keep the funds reserved. The external result is unknown.
        payment.Status = PaymentStatus.Processing;

        payment.BankTransaction.Status =
            TransactionStatus.Pending;

        payment.ResponsePayload = JsonSerializer.Serialize(new
        {
            Decision = "Unknown",
            Message = "External clearing outcome requires reconciliation.",
            RecordedAtUtc = DateTime.UtcNow
        });

        payment.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await dbTransaction.CommitAsync();
    }

    private Task<Payment> LoadPaymentAsync(long paymentId)
    {
        return _context.Payments
            .AsNoTracking()
            .Include(x => x.BankTransaction)
            .SingleAsync(x => x.Id == paymentId);
    }

    private static PaymentResponse MapResponse(
        Payment payment,
        bool isDuplicate)
    {
        var message = payment.Status switch
        {
            PaymentStatus.Successful => "Payment accepted by clearing service.",
            PaymentStatus.Failed => "Payment rejected and source funds restored.",
            PaymentStatus.Processing => "Payment is processing or awaiting reconciliation.",
            _ => "Payment status updated."
        };

        return new PaymentResponse(
            payment.PaymentReference,
            payment.BankTransaction.TransactionReference,
            payment.Status,
            payment.Amount,
            payment.BankTransaction.Currency,
            payment.DestinationBankCode,
            payment.DestinationAccountNumber,
            payment.BeneficiaryName,
            payment.ExternalReference,
            message,
            payment.CreatedAtUtc,
            isDuplicate);
    }
}

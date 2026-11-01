
using BankOps.Web.Data;
using BankOps.Web.Models.Banking;
using BankOps.Web.Models.Banking.DTOs;
using BankOps.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BankOps.Tests;

public class BankingTransactionServiceTests
{
    private static BankingTransactionService CreateService(
        ApplicationDbContext context)
    {
        return new BankingTransactionService(
            context,
            NullLogger<BankingTransactionService>.Instance);
    }

    [Fact]
    public async Task Deposit_ShouldIncreaseBalanceAndCreateBalancedLedger()
    {
        await using var fixture = await TestDatabase.CreateAsync();

        TransactionResponse result;

        await using (var context = fixture.CreateContext())
        {
            result = await CreateService(context).DepositAsync(
                new DepositRequest
                {
                    AccountId = fixture.SenderAccountId,
                    Amount = 10000m,
                    IdempotencyKey = "TEST-DEPOSIT-001",
                    Description = "Test deposit"
                },
                "test-user");
        }

        await using var verify = fixture.CreateContext();

        var account = await verify.BankAccounts
            .SingleAsync(x => x.Id == fixture.SenderAccountId);

        var entries = await verify.TransactionEntries
            .Where(x => x.BankTransactionId == result.Id)
            .ToListAsync();

        Assert.Equal(10000m, account.CurrentBalance);
        Assert.Equal(TransactionStatus.Completed, result.Status);
        Assert.Equal(2, entries.Count);

        Assert.Equal(
            entries.Where(x => x.EntryType == EntryType.Debit)
                .Sum(x => x.Amount),
            entries.Where(x => x.EntryType == EntryType.Credit)
                .Sum(x => x.Amount));
    }

    [Fact]
    public async Task DuplicateDeposit_ShouldNotPostTwice()
    {
        await using var fixture = await TestDatabase.CreateAsync();

        var request = new DepositRequest
        {
            AccountId = fixture.SenderAccountId,
            Amount = 10000m,
            IdempotencyKey = "TEST-DUPLICATE-001",
            Description = "Duplicate test"
        };

        TransactionResponse first;

        await using (var context = fixture.CreateContext())
        {
            first = await CreateService(context)
                .DepositAsync(request, "test-user");
        }

        TransactionResponse second;

        await using (var context = fixture.CreateContext())
        {
            second = await CreateService(context)
                .DepositAsync(request, "test-user");
        }

        await using var verify = fixture.CreateContext();

        var account = await verify.BankAccounts
            .SingleAsync(x => x.Id == fixture.SenderAccountId);

        var transactionCount = await verify.BankTransactions
            .CountAsync(x => x.IdempotencyKey == request.IdempotencyKey);

        Assert.Equal(first.Id, second.Id);
        Assert.True(second.IsDuplicateRequest);
        Assert.Equal(10000m, account.CurrentBalance);
        Assert.Equal(1, transactionCount);
    }

    [Fact]
    public async Task Withdrawal_ShouldRejectInsufficientFunds()
    {
        await using var fixture = await TestDatabase.CreateAsync();

        await using (var context = fixture.CreateContext())
        {
            var exception =
                await Assert.ThrowsAsync<BankingOperationException>(
                    () => CreateService(context).WithdrawAsync(
                        new WithdrawalRequest
                        {
                            AccountId = fixture.SenderAccountId,
                            Amount = 5000m,
                            IdempotencyKey = "TEST-WITHDRAW-001"
                        },
                        "test-user"));

            Assert.Equal(409, exception.StatusCode);
        }

        await using var verify = fixture.CreateContext();

        var account = await verify.BankAccounts
            .SingleAsync(x => x.Id == fixture.SenderAccountId);

        Assert.Equal(0m, account.CurrentBalance);

        Assert.Empty(await verify.BankTransactions.ToListAsync());
    }

    [Fact]
    public async Task Withdrawal_ShouldReduceBalance()
    {
        await using var fixture = await TestDatabase.CreateAsync();

        await using (var context = fixture.CreateContext())
        {
            await CreateService(context).DepositAsync(
                new DepositRequest
                {
                    AccountId = fixture.SenderAccountId,
                    Amount = 10000m,
                    IdempotencyKey = "TEST-WD-DEP-001"
                },
                "test-user");
        }

        await using (var context = fixture.CreateContext())
        {
            await CreateService(context).WithdrawAsync(
                new WithdrawalRequest
                {
                    AccountId = fixture.SenderAccountId,
                    Amount = 3000m,
                    IdempotencyKey = "TEST-WD-002"
                },
                "test-user");
        }

        await using var verify = fixture.CreateContext();

        var account = await verify.BankAccounts
            .SingleAsync(x => x.Id == fixture.SenderAccountId);

        Assert.Equal(7000m, account.CurrentBalance);
    }

    [Fact]
    public async Task Transfer_ShouldDebitSenderAndCreditReceiver()
    {
        await using var fixture = await TestDatabase.CreateAsync();

        await using (var context = fixture.CreateContext())
        {
            await CreateService(context).DepositAsync(
                new DepositRequest
                {
                    AccountId = fixture.SenderAccountId,
                    Amount = 10000m,
                    IdempotencyKey = "TEST-TR-DEP-001"
                },
                "test-user");
        }

        TransactionResponse transfer;

        await using (var context = fixture.CreateContext())
        {
            transfer = await CreateService(context).TransferAsync(
                new TransferRequest
                {
                    FromAccountId = fixture.SenderAccountId,
                    ToAccountId = fixture.ReceiverAccountId,
                    Amount = 3000m,
                    IdempotencyKey = "TEST-TRANSFER-001"
                },
                "test-user");
        }

        await using var verify = fixture.CreateContext();

        var sender = await verify.BankAccounts
            .SingleAsync(x => x.Id == fixture.SenderAccountId);

        var receiver = await verify.BankAccounts
            .SingleAsync(x => x.Id == fixture.ReceiverAccountId);

        var entries = await verify.TransactionEntries
            .Where(x => x.BankTransactionId == transfer.Id)
            .ToListAsync();

        Assert.Equal(7000m, sender.CurrentBalance);
        Assert.Equal(3000m, receiver.CurrentBalance);
        Assert.Equal(2, entries.Count);

        Assert.Equal(
            entries.Where(x => x.EntryType == EntryType.Debit)
                .Sum(x => x.Amount),
            entries.Where(x => x.EntryType == EntryType.Credit)
                .Sum(x => x.Amount));
    }

    [Fact]
    public async Task TransferToSameAccount_ShouldBeRejected()
    {
        await using var fixture = await TestDatabase.CreateAsync();

        await using var context = fixture.CreateContext();

        await Assert.ThrowsAsync<BankingOperationException>(
            () => CreateService(context).TransferAsync(
                new TransferRequest
                {
                    FromAccountId = fixture.SenderAccountId,
                    ToAccountId = fixture.SenderAccountId,
                    Amount = 1000m,
                    IdempotencyKey = "TEST-SAME-ACCOUNT"
                },
                "test-user"));
    }

    [Fact]
    public async Task ReusingIdempotencyKeyForDifferentRequest_ShouldBeRejected()
    {
        await using var fixture = await TestDatabase.CreateAsync();

        await using (var context = fixture.CreateContext())
        {
            await CreateService(context).DepositAsync(
                new DepositRequest
                {
                    AccountId = fixture.SenderAccountId,
                    Amount = 5000m,
                    IdempotencyKey = "TEST-KEY-REUSE-001",
                    Description = "Original request"
                },
                "test-user");
        }

        await using (var context = fixture.CreateContext())
        {
            var exception =
                await Assert.ThrowsAsync<BankingOperationException>(
                    () => CreateService(context).DepositAsync(
                        new DepositRequest
                        {
                            AccountId = fixture.SenderAccountId,
                            Amount = 9000m,
                            IdempotencyKey = "TEST-KEY-REUSE-001",
                            Description = "Different request"
                        },
                        "test-user"));

            Assert.Equal(409, exception.StatusCode);
        }

        await using var verify = fixture.CreateContext();

        var account = await verify.BankAccounts
            .SingleAsync(x => x.Id == fixture.SenderAccountId);

        Assert.Equal(5000m, account.CurrentBalance);
    }

    // Isolated real SQL Server LocalDB test database.
    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public int SenderAccountId { get; private set; }

        public int ReceiverAccountId { get; private set; }

        private TestDatabase(
            DbContextOptions<ApplicationDbContext> options)
        {
            _options = options;
        }

        public ApplicationDbContext CreateContext()
        {
            return new ApplicationDbContext(_options);
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var databaseName = $"BankOpsTest_{Guid.NewGuid():N}";

            var connectionString =
                $"Server=(localdb)\\MSSQLLocalDB;" +
                $"Database={databaseName};" +
                "Trusted_Connection=True;" +
                "MultipleActiveResultSets=true;" +
                "TrustServerCertificate=True;";

            var options =
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlServer(connectionString)
                    .Options;

            var fixture = new TestDatabase(options);

            await using var context = fixture.CreateContext();

            await context.Database.MigrateAsync();

            var customer = new Customer
            {
                CustomerNumber =
                    "TEST" + Guid.NewGuid().ToString("N")[..12],
                FirstName = "Test",
                LastName = "Customer",
                IsActive = true
            };

            context.Customers.Add(customer);

            context.GeneralLedgerAccounts.Add(
                new GeneralLedgerAccount
                {
                    Code = "CASH-LKR",
                    Name = "Test Cash Ledger",
                    Currency = "LKR",
                    IsActive = true
                });

            var sender = new BankAccount
            {
                AccountNumber =
                    "1" + Guid.NewGuid().ToString("N")[..19],
                Customer = customer,
                AccountType = AccountType.Savings,
                Status = AccountStatus.Active,
                Currency = "LKR",
                CurrentBalance = 0m
            };

            var receiver = new BankAccount
            {
                AccountNumber =
                    "2" + Guid.NewGuid().ToString("N")[..19],
                Customer = customer,
                AccountType = AccountType.Savings,
                Status = AccountStatus.Active,
                Currency = "LKR",
                CurrentBalance = 0m
            };

            context.BankAccounts.AddRange(sender, receiver);

            await context.SaveChangesAsync();

            fixture.SenderAccountId = sender.Id;
            fixture.ReceiverAccountId = receiver.Id;

            return fixture;
        }

        public async ValueTask DisposeAsync()
        {
            await using var context = CreateContext();

            await context.Database.EnsureDeletedAsync();
        }
    }
}

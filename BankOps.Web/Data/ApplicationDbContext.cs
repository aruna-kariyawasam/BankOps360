
using BankOps.Web.Models.Banking;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BankOps.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Banking tables
        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<BankAccount> BankAccounts => Set<BankAccount>();

        public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();

        public DbSet<TransactionEntry> TransactionEntries => Set<TransactionEntry>();

        public DbSet<Payment> Payments => Set<Payment>();

        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        public DbSet<AmlAlert> AmlAlerts => Set<AmlAlert>();

        public DbSet<GeneralLedgerAccount> GeneralLedgerAccounts => Set<GeneralLedgerAccount>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // CUSTOMER
            builder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customers");

                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.CustomerNumber)
                    .IsUnique();

                entity.Property(x => x.CustomerNumber)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.FirstName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.LastName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasMany(x => x.Accounts)
                    .WithOne(x => x.Customer)
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // BANK ACCOUNT
            builder.Entity<BankAccount>(entity =>
            {
                entity.ToTable("BankAccounts", table =>
                {
                    table.HasCheckConstraint(
                        "CK_BankAccounts_CurrentBalance",
                        "[CurrentBalance] >= 0");
                });

                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.AccountNumber)
                    .IsUnique();

                entity.Property(x => x.AccountNumber)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.Currency)
                    .HasMaxLength(3)
                    .IsRequired();

                entity.Property(x => x.CurrentBalance)
                    .HasPrecision(18, 2);

                entity.Property(x => x.RowVersion)
                    .IsRowVersion();

                entity.HasMany(x => x.OutgoingTransactions)
                    .WithOne(x => x.FromAccount)
                    .HasForeignKey(x => x.FromAccountId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(x => x.IncomingTransactions)
                    .WithOne(x => x.ToAccount)
                    .HasForeignKey(x => x.ToAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // GENERAL LEDGER ACCOUNTS
            builder.Entity<GeneralLedgerAccount>(entity =>
            {
                entity.ToTable("GeneralLedgerAccounts");

                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.Code)
                    .IsUnique();

                entity.Property(x => x.Code)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Currency)
                    .HasMaxLength(3)
                    .IsRequired();
            });

            // BANK TRANSACTION
            builder.Entity<BankTransaction>(entity =>
            {
                entity.ToTable("BankTransactions", table =>
                {
                    table.HasCheckConstraint(
                        "CK_BankTransactions_Amount",
                        "[Amount] > 0");
                });

                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TransactionReference)
                    .IsUnique();

                entity.Property(x => x.TransactionReference)
                    .HasMaxLength(40)
                    .IsRequired();
                entity.HasIndex(x => x.IdempotencyKey)
    .IsUnique();

                entity.Property(x => x.IdempotencyKey)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Currency)
                    .HasMaxLength(3)
                    .IsRequired();

                entity.Property(x => x.InitiatedByUserId)
                    .HasMaxLength(450);

                entity.HasIndex(x => x.CreatedAtUtc);
            });

            // TRANSACTION ENTRIES / LEDGER
            builder.Entity<TransactionEntry>(entity =>
            {
                entity.ToTable("TransactionEntries", table =>
                {
                    table.HasCheckConstraint(
                        "CK_TransactionEntries_Amount",
                        "[Amount] > 0");

                    table.HasCheckConstraint(
                        "CK_TransactionEntries_ExactlyOneAccount",
                        "(([BankAccountId] IS NOT NULL AND [GeneralLedgerAccountId] IS NULL) " +
                        "OR ([BankAccountId] IS NULL AND [GeneralLedgerAccountId] IS NOT NULL))");
                });

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.BankTransaction)
                    .WithMany(x => x.Entries)
                    .HasForeignKey(x => x.BankTransactionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.BankAccount)
                    .WithMany(x => x.LedgerEntries)
                    .HasForeignKey(x => x.BankAccountId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.GeneralLedgerAccount)
                    .WithMany(x => x.LedgerEntries)
                    .HasForeignKey(x => x.GeneralLedgerAccountId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new
                {
                    x.BankAccountId,
                    x.CreatedAtUtc
                });

                entity.HasIndex(x => new
                {
                    x.GeneralLedgerAccountId,
                    x.CreatedAtUtc
                });
            });

            // PAYMENTS
            builder.Entity<Payment>(entity =>
            {
                entity.ToTable("Payments", table =>
                {
                    table.HasCheckConstraint(
                        "CK_Payments_Amount",
                        "[Amount] > 0");
                });

                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.PaymentReference)
                    .IsUnique();

                entity.HasIndex(x => x.IdempotencyKey)
                    .IsUnique();

                entity.Property(x => x.PaymentReference)
                    .HasMaxLength(40)
                    .IsRequired();

                entity.Property(x => x.IdempotencyKey)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.BankTransaction)
                    .WithOne(x => x.Payment)
                    .HasForeignKey<Payment>(x => x.BankTransactionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // AUDIT LOGS
            builder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.UserId)
                    .HasMaxLength(450);

                entity.Property(x => x.Action)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.EntityName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasIndex(x => x.CreatedAtUtc);
            });

            // AML ALERTS
            builder.Entity<AmlAlert>(entity =>
            {
                entity.ToTable("AmlAlerts", table =>
                {
                    table.HasCheckConstraint(
                        "CK_AmlAlerts_RiskScore",
                        "[RiskScore] >= 0 AND [RiskScore] <= 100");
                });

                entity.HasKey(x => x.Id);

                entity.Property(x => x.RuleCode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Reason)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.HasOne(x => x.BankTransaction)
                    .WithMany()
                    .HasForeignKey(x => x.BankTransactionId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(x => new
                {
                    x.BankTransactionId,
                    x.RuleCode
                })
                .IsUnique();

                entity.Property(x => x.ReviewedByUserId)
                    .HasMaxLength(450);
            });
        }
    }
}

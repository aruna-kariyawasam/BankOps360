
namespace BankOps.Web.Models.Banking;

public class TransactionEntry
{
    public long Id { get; set; }

    public long BankTransactionId { get; set; }

    // A posting belongs to either a customer bank account
    // or a general ledger account, never both.
    public int? BankAccountId { get; set; }

    public int? GeneralLedgerAccountId { get; set; }

    public EntryType EntryType { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public BankTransaction BankTransaction { get; set; } = null!;

    public BankAccount? BankAccount { get; set; }

    public GeneralLedgerAccount? GeneralLedgerAccount { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace BankOps.Web.Models.Banking;

public class BankAccount
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string AccountNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public AccountType AccountType { get; set; }

    public AccountStatus Status { get; set; }
        = AccountStatus.Active;

    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "LKR";

    public decimal CurrentBalance { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Customer Customer { get; set; } = null!;

    public ICollection<TransactionEntry> LedgerEntries { get; set; }
        = new List<TransactionEntry>();

    public ICollection<BankTransaction> OutgoingTransactions { get; set; }
        = new List<BankTransaction>();

    public ICollection<BankTransaction> IncomingTransactions { get; set; }
        = new List<BankTransaction>();
}
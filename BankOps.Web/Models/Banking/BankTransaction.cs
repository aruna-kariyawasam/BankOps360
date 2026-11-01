using System.ComponentModel.DataAnnotations;
using System.Transactions;

namespace BankOps.Web.Models.Banking;

public class BankTransaction
{
    public long Id { get; set; }

    [Required]
    [StringLength(40)]
    public string TransactionReference { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;

    public TransactionType Type { get; set; }

    public TransactionStatus Status { get; set; }
        = TransactionStatus.Pending;

    public decimal Amount { get; set; }

    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "LKR";

    [StringLength(250)]
    public string? Description { get; set; }

    public int? FromAccountId { get; set; }

    public int? ToAccountId { get; set; }

    public string? InitiatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }

    public BankAccount? FromAccount { get; set; }

    public BankAccount? ToAccount { get; set; }

    public ICollection<TransactionEntry> Entries { get; set; }
        = new List<TransactionEntry>();

    public Payment? Payment { get; set; }
}
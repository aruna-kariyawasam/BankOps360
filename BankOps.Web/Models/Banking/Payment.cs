using System.ComponentModel.DataAnnotations;

namespace BankOps.Web.Models.Banking;

public class Payment
{
    public long Id { get; set; }

    [Required]
    [StringLength(40)]
    public string PaymentReference { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string IdempotencyKey { get; set; } = string.Empty;

    public long BankTransactionId { get; set; }

    [Required]
    [StringLength(20)]
    public string DestinationBankCode { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string DestinationAccountNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string BeneficiaryName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }
        = PaymentStatus.Pending;

    [StringLength(100)]
    public string? ExternalReference { get; set; }

    public string? RequestPayload { get; set; }

    public string? ResponsePayload { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public BankTransaction BankTransaction { get; set; } = null!;
}
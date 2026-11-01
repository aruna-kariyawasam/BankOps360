using System.ComponentModel.DataAnnotations;

namespace BankOps.Web.Models.Banking;

public class AmlAlert
{
    public long Id { get; set; }

    public long BankTransactionId { get; set; }

    [Required]
    [StringLength(50)]
    public string RuleCode { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    public int RiskScore { get; set; }

    public AmlAlertStatus Status { get; set; }
        = AmlAlertStatus.New;

    public string? ReviewedByUserId { get; set; }

    [StringLength(1000)]
    public string? ReviewNotes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAtUtc { get; set; }

    public BankTransaction BankTransaction { get; set; } = null!;
}
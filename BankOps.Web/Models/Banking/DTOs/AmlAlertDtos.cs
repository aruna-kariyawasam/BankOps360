using System.ComponentModel.DataAnnotations;
using BankOps.Web.Models.Banking;

namespace BankOps.Web.Models.Banking.DTOs;

public class ReviewAmlAlertRequest
{
    [EnumDataType(typeof(AmlAlertStatus))]
    public AmlAlertStatus Status { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 5)]
    public string ReviewNotes { get; set; } = string.Empty;
}

public record AmlAlertResponse(
    long Id,
    long BankTransactionId,
    string TransactionReference,
    TransactionType TransactionType,
    decimal TransactionAmount,
    string Currency,
    string RuleCode,
    string Reason,
    int RiskScore,
    AmlAlertStatus Status,
    string? ReviewedByUserId,
    string? ReviewNotes,
    DateTime CreatedAtUtc,
    DateTime? ReviewedAtUtc
);
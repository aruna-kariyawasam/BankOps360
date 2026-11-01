
using System.ComponentModel.DataAnnotations;

namespace BankOps.Web.Models.Banking.DTOs;

public class CreatePaymentRequest
{
    [Range(1, int.MaxValue)]
    public int SourceAccountId { get; set; }

    [Required]
    [StringLength(20, MinimumLength = 2)]
    public string DestinationBankCode { get; set; } = string.Empty;

    [Required]
    [StringLength(30, MinimumLength = 2)]
    public string DestinationAccountNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string BeneficiaryName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(64, MinimumLength = 8)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }
}

public record PaymentResponse(
    string PaymentReference,
    string TransactionReference,
    PaymentStatus Status,
    decimal Amount,
    string Currency,
    string DestinationBankCode,
    string DestinationAccountNumber,
    string BeneficiaryName,
    string? ExternalReference,
    string? Message,
    DateTime CreatedAtUtc,
    bool IsDuplicateRequest
);

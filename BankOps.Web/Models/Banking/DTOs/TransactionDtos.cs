
using System.ComponentModel.DataAnnotations;

namespace BankOps.Web.Models.Banking.DTOs;

public class DepositRequest
{
    [Range(1, int.MaxValue)]
    public int AccountId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(64, MinimumLength = 8)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }
}

public class WithdrawalRequest
{
    [Range(1, int.MaxValue)]
    public int AccountId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(64, MinimumLength = 8)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }
}

public class TransferRequest
{
    [Range(1, int.MaxValue)]
    public int FromAccountId { get; set; }

    [Range(1, int.MaxValue)]
    public int ToAccountId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(64, MinimumLength = 8)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }
}

public record TransactionResponse(
    long Id,
    string TransactionReference,
    TransactionType Type,
    TransactionStatus Status,
    decimal Amount,
    string Currency,
    int? FromAccountId,
    int? ToAccountId,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    bool IsDuplicateRequest
);

namespace BankOps.Web.Models.Banking.DTOs;

public sealed class LedgerReconciliationResult
{
    public long TransactionId { get; set; }

    public string TransactionReference { get; set; } = string.Empty;

    public int TransactionType { get; set; }

    public int TransactionStatus { get; set; }

    public decimal TransactionAmount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public decimal TotalDebits { get; set; }

    public decimal TotalCredits { get; set; }

    public decimal Difference { get; set; }

    public int EntryCount { get; set; }

    public bool IsBalanced { get; set; }
}

public sealed class TransactionSummaryResult
{
    public int TransactionType { get; set; }

    public int TransactionStatus { get; set; }

    public long TransactionCount { get; set; }

    public decimal TotalAmount { get; set; }
}
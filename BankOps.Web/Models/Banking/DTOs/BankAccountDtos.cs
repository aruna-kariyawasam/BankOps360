
using System.ComponentModel.DataAnnotations;

namespace BankOps.Web.Models.Banking.DTOs;

public class CreateBankAccountRequest
{
    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }

    [EnumDataType(typeof(AccountType))]
    public AccountType AccountType { get; set; }

    [Required]
    [RegularExpression("^LKR$")]
    public string Currency { get; set; } = "LKR";
}

public record BankAccountResponse(
    int Id,
    string AccountNumber,
    int CustomerId,
    string CustomerName,
    AccountType AccountType,
    AccountStatus Status,
    string Currency,
    decimal CurrentBalance,
    DateTime CreatedAtUtc
);

public record BankAccountListResponse(
    int Page,
    int PageSize,
    int TotalRecords,
    IReadOnlyList<BankAccountResponse> Accounts
);

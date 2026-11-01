
using System.ComponentModel.DataAnnotations;

namespace BankOps.Web.Models.Banking.DTOs;

public class CreateCustomerRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }
}

public record CustomerResponse(
    int Id,
    string CustomerNumber,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    string? Address,
    DateTime CreatedAtUtc,
    bool IsActive
);

public record CustomerListResponse(
    int Page,
    int PageSize,
    int TotalRecords,
    IReadOnlyList<CustomerResponse> Customers
);

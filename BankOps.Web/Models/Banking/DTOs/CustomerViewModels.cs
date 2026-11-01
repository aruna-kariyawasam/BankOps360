
using System.ComponentModel.DataAnnotations;

namespace BankOps.Web.Models.Banking.DTOs;

public class CustomerFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }
}

public class CustomerListItemViewModel
{
    public int Id { get; set; }

    public string CustomerNumber { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

public class CustomerDetailsViewModel
{
    public int Id { get; set; }

    public string CustomerNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

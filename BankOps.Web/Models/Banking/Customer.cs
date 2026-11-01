using System.ComponentModel.DataAnnotations;

namespace BankOps.Web.Models.Banking;

public class Customer
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string CustomerNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public ICollection<BankAccount> Accounts { get; set; }
        = new List<BankAccount>();
}
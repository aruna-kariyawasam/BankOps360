using System.ComponentModel.DataAnnotations;

namespace BankOps.Web.Models.Banking;

public class AuditLog
{
    public long Id { get; set; }

    public string? UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string EntityName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? EntityId { get; set; }

    public string? Details { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
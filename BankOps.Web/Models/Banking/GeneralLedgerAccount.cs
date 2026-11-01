
using System.ComponentModel.DataAnnotations;

namespace BankOps.Web.Models.Banking;

public class GeneralLedgerAccount
{
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "LKR";

    public bool IsActive { get; set; } = true;

    public ICollection<TransactionEntry> LedgerEntries { get; set; }
        = new List<TransactionEntry>();
}

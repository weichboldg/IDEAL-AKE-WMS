using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class BdeTerminal : AuditableEntity
{
    [Required]
    [Display(Name = "Terminal-Benutzer")]
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    [Required]
    [Display(Name = "Default-Werkbank")]
    public int DefaultProductionWorkplaceId { get; set; }

    public ProductionWorkplace DefaultProductionWorkplace { get; set; } = null!;

    [StringLength(200)]
    [Display(Name = "Beschreibung")]
    public string? Description { get; set; }
}

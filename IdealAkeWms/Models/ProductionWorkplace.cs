using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ProductionWorkplace : AuditableEntity
{
    [Required]
    [MaxLength(200)]
    [Display(Name = "Bezeichnung")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    [Display(Name = "Halle")]
    public string? Hall { get; set; }

    [Display(Name = "Abweichende Vorkommissioniertage")]
    [Range(0, 365)]
    public int? OverridePrePickingDays { get; set; }
}

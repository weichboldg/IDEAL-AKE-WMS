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

    [Display(Name = "BDE aktiv")]
    public bool BdeAktiv { get; set; }

    [StringLength(200)]
    [Display(Name = "Default-Arbeitsgang (BDE)")]
    public string? BdeDefaultArbeitsgang { get; set; }

    public ICollection<ProductionWorkplaceUser> ProductionWorkplaceUsers { get; set; } = new List<ProductionWorkplaceUser>();
    public ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();
    public ICollection<WorkOperation> WorkOperations { get; set; } = new List<WorkOperation>();
}

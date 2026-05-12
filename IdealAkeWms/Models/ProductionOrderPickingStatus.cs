using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ProductionOrderPickingStatus : AuditableEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    [StringLength(50)]
    [Display(Name = "Kommissionierung")]
    public string? PickingStatus { get; set; }

    [Display(Name = "Priorität")]
    public int? PickingPriority { get; set; }

    [Display(Name = "Freigegeben")]
    public bool IsReleasedForPicking { get; set; }

    [Display(Name = "Freigegeben am")]
    public DateTime? ReleasedAt { get; set; }

    [StringLength(200)]
    [Display(Name = "Freigegeben von")]
    public string? ReleasedBy { get; set; }

    public int? AssignedPickerId { get; set; }
    public User? AssignedPicker { get; set; }

    [StringLength(200)]
    [Display(Name = "Kommissionierer")]
    public string? AssignedPickerName { get; set; }

    [Display(Name = "Glas")]
    public bool HasGlass { get; set; }

    [Display(Name = "Zukauf")]
    public bool HasExternalPurchase { get; set; }

    /// <summary>Sync-calculated. Reset von IsCoatingDone wenn auf false flippt (Fallstrick #11).</summary>
    public bool HasCoatingParts { get; set; }

    /// <summary>User-toggleable.</summary>
    public bool IsCoatingDone { get; set; }

    /// <summary>Neu in v1.11.0 — Picking-spezifischer Done-Marker, ergaenzt FA-Master-IsDone.</summary>
    public bool IsDonePicking { get; set; }
}

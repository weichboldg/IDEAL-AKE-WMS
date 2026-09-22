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

    [Display(Name = "Eigener Schichtplan")]
    public bool BdeUseCustomShiftPlan { get; set; }

    /// <summary>IDEAL: Verknuepfungsschluessel zum Sage-Arbeitsplatz (`KHKPpsArbeitsplaetze.Arbeitsplatznummer`).
    /// Sage fuehrend, vom `ProductionWorkplaceSyncService` gesetzt; **immer als Zeichenkette, getrimmt**
    /// (nie int.Parse — `0000` ≠ `0`), eindeutiger gefilterter Index `WHERE IS NOT NULL`. NULL bei
    /// AKE-Werkbaenken. (v1.44.0)</summary>
    [StringLength(31)]
    [Display(Name = "Sage-Arbeitsplatznummer")]
    public string? SageArbeitsplatznummer { get; set; }

    /// <summary>IDEAL: Arbeitsschritt-Kuerzel aus Sage (`USER_ArbeitsSchritt`, z. B. `KA`/`SW`), getrimmt.
    /// Match-Feld fuer die Struktur-Arbeitsgang-Ableitung (Baustein b). (v1.44.0)</summary>
    [MaxLength(20)]
    [Display(Name = "Arbeitsschritt-Kürzel (Sage)")]
    public string? ArbeitsschrittCode { get; set; }

    public ICollection<ProductionWorkplaceUser> ProductionWorkplaceUsers { get; set; } = new List<ProductionWorkplaceUser>();
    public ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();
    public ICollection<WorkOperation> WorkOperations { get; set; } = new List<WorkOperation>();
}

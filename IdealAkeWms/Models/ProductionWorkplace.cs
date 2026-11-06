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

    /// <summary>IDEAL: Erster Lauf der Werkbank-Anlage, in dem die Sage-Arbeitsplatznummer nicht mehr in der Quelle stand.
    /// NULL = aktuell. Nur der <c>ProductionWorkplaceSyncService</c> setzt/loescht es (selbstheilend, nie loeschen);
    /// gekennzeichnete Werkbaenke zaehlen nicht fuer den Kuerzel-Abgleich. (v1.63.0)</summary>
    [Display(Name = "Nicht in Sage seit")]
    public DateTime? SageMissingSince { get; set; }

    /// <summary>BDE ohne Start/Stop: der Werker meldet den Arbeitsgang nur fertig (Buchung mit Start = Ende).
    /// Ohne Wirkung, solange <c>BdeNurFaMeldung</c> global an ist. (v1.63.0)</summary>
    [Display(Name = "Nur AG-Fertigmeldung (ohne Start/Stop)")]
    public bool BdeNurAgFertigmeldung { get; set; }

    // v1.73.0 (Spec 2026-11-06-sage-sdata-stammdaten-sync E6a, F8): nur der SData-Weg (PpsArbeitsplaetzeInfo) liefert diese Felder.
    // Der ProductionWorkplaceSyncService schreibt sie nur, wenn die Quelle sie liefert (nicht null) — ein View-Lauf laesst sie unveraendert.

    /// <summary>Sage <c>BdeTerminalId</c> (string(20), P-S1). (v1.73.0)</summary>
    [StringLength(20)]
    [Display(Name = "BDE-Terminal (Sage)")]
    public string? SageBdeTerminalId { get; set; }

    /// <summary>Sage <c>Kostenstelle</c> (string(20), P-S1). (v1.73.0)</summary>
    [StringLength(20)]
    [Display(Name = "Kostenstelle (Sage)")]
    public string? SageKostenstelle { get; set; }

    /// <summary>Sage <c>Schichtplannummer</c> (string(31), P-S1). (v1.73.0)</summary>
    [StringLength(31)]
    [Display(Name = "Schichtplannummer (Sage)")]
    public string? SageSchichtplannummer { get; set; }

    public ICollection<ProductionWorkplaceUser> ProductionWorkplaceUsers { get; set; } = new List<ProductionWorkplaceUser>();
    public ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();
    public ICollection<WorkOperation> WorkOperations { get; set; } = new List<WorkOperation>();
}

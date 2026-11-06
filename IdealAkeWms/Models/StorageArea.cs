using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>
/// Sage-Lagerort (<c>LagerorteInfo</c>), Elternteil der <see cref="StorageLocation"/> (Spec 2026-11-06-sage-sdata-stammdaten-sync, E1/F3).
/// Wird ausschliesslich vom Lagerplatz-Abgleich bei Quelle SData geschrieben (der View-Reader liefert keine Lagerort-Daten).
/// Nie geloescht: fehlt ein Lagerort in Sage (oder ist er dort inaktiv), setzt der Abgleich <see cref="SageMissingSince"/>.
/// Laengen laut SData-<c>$schema</c> (P-S1, 2026-11-06): Lagerkennung string(10), Bezeichnung string(40), Lagertechnik string(10).
/// </summary>
public class StorageArea : AuditableEntity
{
    [Required]
    [StringLength(10)]
    [Display(Name = "Lagerkennung")]
    public string Lagerkennung { get; set; } = string.Empty;

    [StringLength(40)]
    [Display(Name = "Lagerort")]
    public string? Bezeichnung { get; set; }

    [StringLength(10)]
    [Display(Name = "Lagertechnik")]
    public string? Lagertechnik { get; set; }

    [Display(Name = "Dispo-Lager")]
    public bool IstDispoLager { get; set; }

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;

    /// <summary>Gesetzt, sobald der Lagerort nicht mehr (aktiv) in Sage steht; beim Wiederauftauchen geleert.</summary>
    [Display(Name = "Nicht in Sage seit")]
    public DateTime? SageMissingSince { get; set; }

    public ICollection<StorageLocation> StorageLocations { get; set; } = new List<StorageLocation>();
}

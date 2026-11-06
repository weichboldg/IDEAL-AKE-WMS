using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class BdeOperator : AuditableEntity
{
    [Required(ErrorMessage = "Personalnummer ist erforderlich")]
    [StringLength(50)]
    [Display(Name = "Personalnummer")]
    public string PersonnelNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vorname ist erforderlich")]
    [StringLength(100)]
    [Display(Name = "Vorname")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nachname ist erforderlich")]
    [StringLength(100)]
    [Display(Name = "Nachname")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "WMS-Benutzer")]
    public int? UserId { get; set; }

    public User? User { get; set; }

    /// <summary>Sage-PPS-Mitarbeiternummer (<c>PpsMitarbeiterInfo.Nummer</c>, string(20)); nur manuell zugeordnet (G14),
    /// eindeutig (gefilterter UNIQUE-Index + App-Guard). Null = nicht zugeordnet.</summary>
    [StringLength(20)]
    [Display(Name = "Sage-Mitarbeiternummer")]
    public string? SageMitarbeiternummer { get; set; }

    /// <summary>Herkunft wie <see cref="StorageLocation.Source"/> (<c>Sage</c> | <c>Manual</c>, Default <c>Manual</c>, F11/E9).
    /// Wird <c>Sage</c>, sobald der Mitarbeiter manuell einem Sage-PPS-Mitarbeiter zugeordnet ist (G14).</summary>
    [StringLength(20)]
    [Display(Name = "Herkunft")]
    public string Source { get; set; } = StorageLocationSource.Manual;

    // ----- Uebergabe WMS → Sage (Spec 2026-11-06-sage-sdata-stammdaten-sync F11, E9; vorbereitet, kein Worker) -----
    // Bestand = NichtNoetig (DB-Default); im WMS neu angelegt = Offen (+ Outbox-Eintrag); vom Lese-Abgleich/Zuordnung gefunden = Uebergeben.
    // Kein EF-HasDefaultValue (Sentinel-Fallstrick E8a) – der DEFAULT steht nur in Migration/SQL.

    [Display(Name = "Übergabe an Sage")]
    public SageUebergabeStatus SageUebergabeStatus { get; set; } = SageUebergabeStatus.NichtNoetig;

    [Display(Name = "Übergeben am")]
    public DateTime? SageUebergabeAm { get; set; }

    [StringLength(1000)]
    [Display(Name = "Übergabe-Fehler")]
    public string? SageUebergabeFehler { get; set; }

    public string DisplayName => $"{FirstName} {LastName}";
}

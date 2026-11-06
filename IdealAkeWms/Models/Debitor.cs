using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>
/// Debitor (Kunde) — Stammdaten im WMS (v1.73.0, Spec 2026-11-06-sage-sdata-stammdaten-sync F10, Etappe E8b). Gleicher Aufbau wie
/// <see cref="Creditor"/> ohne <c>IsCoater</c>/<c>SageEKMindestbestellwert</c>, dafuer <see cref="SageVertreter"/> und
/// <see cref="SageKundenNameFA"/>. Herkunft <see cref="Source"/> (<see cref="StorageLocationSource"/>): <c>Manual</c> = im WMS angelegt,
/// <c>Sage</c> = kuenftig vom Lese-Abgleich Kontokorrent (E8c) — dann sind Name, Adresse, Kontakt und alle <c>Sage*</c>-Felder
/// schreibgeschuetzt. Nie geloescht, nur deaktiviert.
/// </summary>
public class Debitor : AuditableEntity, ISageKonto
{
    [Required(ErrorMessage = "Name ist ein Pflichtfeld.")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Kurzname — eindeutig (getrimmt, Gross/Klein egal). Pflicht nur bei <c>Source = Manual</c>.</summary>
    [StringLength(50)]
    [Display(Name = "Kurzname")]
    public string? ShortName { get; set; }

    [StringLength(50)]
    [Display(Name = "Matchcode")]
    public string? Matchcode { get; set; }

    [StringLength(200)]
    [Display(Name = "Straße")]
    public string? Street { get; set; }

    [StringLength(20)]
    [Display(Name = "PLZ")]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    [Display(Name = "Ort")]
    public string? City { get; set; }

    [StringLength(100)]
    [Display(Name = "Land")]
    public string? Country { get; set; }

    [StringLength(200)]
    [EmailAddress(ErrorMessage = "Bitte eine gültige E-Mail-Adresse eingeben.")]
    [Display(Name = "E-Mail")]
    public string? Email { get; set; }

    [StringLength(50)]
    [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    [StringLength(200)]
    [Display(Name = "Ansprechpartner")]
    public string? ContactPerson { get; set; }

    [StringLength(1000)]
    [Display(Name = "Bemerkung")]
    public string? Remark { get; set; }

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;

    [StringLength(20)]
    [Display(Name = "Herkunft")]
    public string Source { get; set; } = StorageLocationSource.Manual;

    /// <summary>Sage-Kontonummer (Kto(20)); immer Zeichenkette, nie numerisch geparst. Eindeutig, wenn gesetzt.</summary>
    [StringLength(20)]
    [Display(Name = "Sage-Konto")]
    public string? SageKto { get; set; }

    [StringLength(50)]
    [Display(Name = "Sage-Matchcode")]
    public string? SageMatchcode { get; set; }

    [StringLength(10)]
    [Display(Name = "Sage-Gruppe")]
    public string? SageGruppe { get; set; }

    [StringLength(15)]
    [Display(Name = "UID-Nummer")]
    public string? SageEUUStID { get; set; }

    [StringLength(10)]
    [Display(Name = "Zahlungskondition")]
    public string? SageZahlungskondition { get; set; }

    /// <summary>Vertreter aus <c>Kontokorrent.Vertreter</c> string(10) (P-S1-Schema, Spec „Prüfabfragen“).</summary>
    [StringLength(10)]
    [Display(Name = "Vertreter")]
    public string? SageVertreter { get; set; }

    /// <summary>Kundenname fuer den FA (<c>KHKAdressen_USER_KundenNameFA(20)</c>).</summary>
    [StringLength(20)]
    [Display(Name = "Kundenname FA")]
    public string? SageKundenNameFA { get; set; }

    /// <summary>Gesetzt vom Lese-Abgleich (E8c), wenn das Konto in Sage fehlt; nie geloescht.</summary>
    [Display(Name = "Nicht in Sage seit")]
    public DateTime? SageMissingSince { get; set; }

    /// <summary>Uebergabe-Status WMS → Sage (F11, vorbereitet, kein Worker).</summary>
    [Display(Name = "Übergabe an Sage")]
    public SageUebergabeStatus SageUebergabeStatus { get; set; } = SageUebergabeStatus.NichtNoetig;

    public DateTime? SageUebergabeAm { get; set; }

    [StringLength(1000)]
    public string? SageUebergabeFehler { get; set; }
}

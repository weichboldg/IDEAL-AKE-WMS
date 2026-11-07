using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>
/// Kreditor (Lieferant/Dienstleister/Beschichter) — Stammdaten im WMS (v1.73.0, Spec 2026-11-06-sage-sdata-stammdaten-sync F10,
/// Etappe E8a; Detail-Anhang 2026-10-06-kreditoren-stammdaten). Herkunft <see cref="Source"/> wie <see cref="StorageLocation.Source"/>
/// (<see cref="StorageLocationSource"/>): <c>Manual</c> = im WMS angelegt (alles pflegbar), <c>Sage</c> = kuenftig vom Lese-Abgleich
/// Kontokorrent (E8c) angelegt — dann sind Name, Adresse, Kontakt und alle <c>Sage*</c>-Felder schreibgeschuetzt; <see cref="ShortName"/>,
/// <see cref="Matchcode"/>, <see cref="Remark"/>, <see cref="IsCoater"/>, <see cref="IsActive"/> bleiben WMS-Felder.
/// Nie geloescht, nur deaktiviert (Kreditoren-Frage 7).
/// </summary>
public class Creditor : AuditableEntity, ISageKonto
{
    [Required(ErrorMessage = "Name ist ein Pflichtfeld.")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Kurzname — Abgleichsschluessel fuer den Freitext <c>FaHierarchyOrderInfo.Dienstleister</c> (getrimmt, Gross/Klein egal,
    /// eindeutig). Pflicht nur bei <c>Source = Manual</c>; an Sage-Konten leer, bis jemand ihn pflegt (F10).</summary>
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

    /// <summary>Beschichter ja/nein (Antwort 1 der Kreditoren-Spec) — Filter in der Liste.</summary>
    [Display(Name = "Beschichter")]
    public bool IsCoater { get; set; }

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

    [Display(Name = "EK-Mindestbestellwert")]
    public decimal? SageEKMindestbestellwert { get; set; }

    /// <summary>Gesetzt vom Lese-Abgleich (E8c), wenn das Konto in Sage fehlt; nie geloescht.</summary>
    [Display(Name = "Nicht in Sage seit")]
    public DateTime? SageMissingSince { get; set; }

    /// <summary>Uebergabe-Status WMS → Sage (F11, vorbereitet, kein Worker). Default <see cref="SageUebergabeStatus.NichtNoetig"/>.</summary>
    [Display(Name = "Übergabe an Sage")]
    public SageUebergabeStatus SageUebergabeStatus { get; set; } = SageUebergabeStatus.NichtNoetig;

    public DateTime? SageUebergabeAm { get; set; }

    [StringLength(1000)]
    public string? SageUebergabeFehler { get; set; }
}

/// <summary>Gemeinsame Felder von <see cref="Creditor"/> und <see cref="Debitor"/>, die der Lese-Abgleich Kontokorrent (E8c) schreibt
/// bzw. liest — ein Abgleich-Kern fuer beide Tabellen statt zwei Kopien.</summary>
public interface ISageKonto
{
    int Id { get; }
    string Name { get; set; }
    string Source { get; set; }
    bool IsActive { get; set; }
    string? Matchcode { get; set; }
    string? Street { get; set; }
    string? PostalCode { get; set; }
    string? City { get; set; }
    string? Country { get; set; }
    string? Email { get; set; }
    string? Phone { get; set; }
    string? ContactPerson { get; set; }
    string? SageKto { get; set; }
    string? SageMatchcode { get; set; }
    string? SageGruppe { get; set; }
    string? SageEUUStID { get; set; }
    string? SageZahlungskondition { get; set; }
    DateTime? SageMissingSince { get; set; }
    SageUebergabeStatus SageUebergabeStatus { get; set; }
    DateTime? SageUebergabeAm { get; set; }
}

/// <summary>Uebergabe-Status WMS → Sage (Spec 2026-11-06-sage-sdata-stammdaten-sync F11). Als int gespeichert.</summary>
public enum SageUebergabeStatus
{
    NichtNoetig = 0,
    Offen = 1,
    Uebergeben = 2,
    Fehler = 3
}

public static class SageUebergabeStatusText
{
    /// <summary>Zelltext der Spalte „Übergabe an Sage“ (E9). Seit v1.79.0 (Spec 2026-11-08-bereinigungen F6) auch NichtNoetig lesbar
    /// („nicht nötig“, gedämpft statt Badge) – vorher leer, was wie ein Fehler aussah.</summary>
    public static string Text(SageUebergabeStatus s) => s switch
    {
        SageUebergabeStatus.Offen => "Offen",
        SageUebergabeStatus.Uebergeben => "Übergeben",
        SageUebergabeStatus.Fehler => "Fehler",
        _ => "nicht nötig"
    };

    /// <summary>„Offene Übergaben an Sage“ = Offen oder Fehler (noch nicht in Sage).</summary>
    public static bool IstOffen(SageUebergabeStatus s) => s is SageUebergabeStatus.Offen or SageUebergabeStatus.Fehler;
}

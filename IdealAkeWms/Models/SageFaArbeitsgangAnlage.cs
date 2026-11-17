namespace IdealAkeWms.Models;

/// <summary>Status einer AG-Anlage in der Warteschlange (v1.99.38). Int-Werte stehen in der DB — nie umnummerieren.</summary>
public enum SageFaArbeitsgangStatus
{
    /// <summary>Eingereiht, wartet auf den Dienst.</summary>
    Offen = 1,
    /// <summary>Vor dem POST gesetzt; bleibt er haengen, wird er „unklar – bitte in Sage prüfen“ (nie blind neu gesendet).</summary>
    Gesendet = 2,
    Angelegt = 3,
    /// <summary>Die Sage-Pruefung vor dem Senden fand die AG am FA – nichts gesendet.</summary>
    InSageVorhanden = 4,
    Fehler = 5,
}

/// <summary>
/// v1.99.38 (Spec 2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen E1a, Freigabe-Antwort 9): eine AG-Position, die der Dienst im
/// Sage-FA <see cref="BelId"/> anlegen soll. Die Web-Seite reiht nur ein und zeigt den Status; gesendet wird im Sage-Buchungs-Worker.
/// Eindeutig je (BelId, SageArbeitsgangnummer): erneutes Einreihen nutzt die Zeile wieder.
/// </summary>
public class SageFaArbeitsgangAnlage : AuditableEntity
{
    public const int SageArbeitsgangnummerMaxLaenge = 31;
    public const int FehlerMaxLaenge = 1000;

    public int BelId { get; set; }
    public int HauptFa { get; set; }
    /// <summary>Kuerzel aus der FA-Struktur (Anzeige, Protokoll).</summary>
    public string Kuerzel { get; set; } = string.Empty;
    /// <summary>Wirksame Sage-Arbeitsgangnummer beim Einreihen (Text, nie int).</summary>
    public string SageArbeitsgangnummer { get; set; } = string.Empty;
    /// <summary>Position des Kuerzels im Feld (Sende-Reihenfolge je FA).</summary>
    public int Reihenfolge { get; set; }
    public SageFaArbeitsgangStatus Status { get; set; } = SageFaArbeitsgangStatus.Offen;
    /// <summary>Echte Sende-Versuche (POST) bzw. gescheiterte Sage-Pruefungen; Grenze fuer die automatische Wiederholung.</summary>
    public int Versuche { get; set; }
    public string? Fehler { get; set; }
    /// <summary>Zeitpunkt der Bestaetigung (angelegt bzw. in Sage gefunden), Lokalzeit.</summary>
    public DateTime? SageAm { get; set; }
}

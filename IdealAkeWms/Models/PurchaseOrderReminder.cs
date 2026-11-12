namespace IdealAkeWms.Models;

/// <summary>
/// v1.99.10 (Spec 2026-11-12-offene-bestellungen-mahnmail-lieferverzug F7): Protokoll der Lieferverzug-Mails, eine Zeile je versendeter Mail
/// und Position. Daraus ergeben sich „heute schon gesendet“ (auch nach einem Dienst-Neustart), die Wiederholung der Kreditor-Erinnerung und
/// der Nachweis. Kein <see cref="AuditableEntity"/> (Freigabe-Antwort 13): nur der Dienst schreibt, angehaengt und nie geaendert, wie der Cache
/// <see cref="SagePurchaseOrderLine"/>. Verweise (<see cref="CreditorId"/>) ohne FK. <see cref="SentAt"/> in Lokalzeit (ADR 0010).
/// </summary>
public class PurchaseOrderReminder
{
    public int Id { get; set; }
    public PurchaseOrderReminderArt Art { get; set; }
    public int BelPosId { get; set; }
    public string Belegnummer { get; set; } = string.Empty;
    public string? LieferantKto { get; set; }
    public int? CreditorId { get; set; }
    /// <summary>Liefertermin der Position beim Versand – ein geaenderter Termin gilt fuer die Wiederholung als neue Position.</summary>
    public DateTime? Liefertermin { get; set; }
    /// <summary>Rest nach der Rest-Regel (<c>ErfasstJeBelPos.Rest</c>) beim Versand, Basiseinheit.</summary>
    public decimal Rest { get; set; }
    /// <summary>Tatsaechliche Empfaenger (im Testbetrieb die Einkauf-Adressen), kommagetrennt.</summary>
    public string Empfaenger { get; set; } = string.Empty;
    /// <summary>Kreditor-Mail im Testbetrieb an den Einkauf umgeleitet; zaehlt nicht fuer die Wiederholung echter Kreditor-Mails.</summary>
    public bool Testbetrieb { get; set; }
    public DateTime SentAt { get; set; }
}

/// <summary>Art der Lieferverzug-Mail, als int gespeichert.</summary>
public enum PurchaseOrderReminderArt
{
    Einkauf = 0,
    Kreditor = 1
}

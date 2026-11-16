namespace IdealAkeWms.Models;

/// <summary>
/// v1.99.30 (Spec 2026-11-16-bestellvorschlaege-nachbesserung F3, Antwort 4, SQL/155): juengste Sage-Bestellposition (EBB/EBZ, Rueckblick
/// <see cref="ArticleSupplier.LetzteBestellungRueckblickJahre"/> Jahre) eines Artikels bei einem Lieferanten OHNE Artikel-Lieferant-Zeile im WMS
/// (in Sage kein Artikel-Lieferant gepflegt oder Kreditor fehlt). Nur vom Lauf „Letzte Bestellung (Einkauf)“ geschrieben, je Lauf abgeglichen.
/// Die Bestellvorschlaege nehmen die Menge als letzte Bestellmenge, wenn sie juenger ist als alle zugeordneten, mit Hinweis
/// „kein Artikel-Lieferant in Sage“. Eindeutig je Artikel + Kto.
/// </summary>
public class ArticleLastOrderUnassigned : AuditableEntity
{
    public int ArticleId { get; set; }
    /// <summary>Sage-Kontonummer des Lieferanten (<c>A0Empfaenger</c>, getrimmt, gross), Laenge wie <c>Creditor.SageKto</c>.</summary>
    public string Kto { get; set; } = string.Empty;
    /// <summary><c>MengeBasis</c> (Basiseinheit), immer &gt; 0.</summary>
    public decimal Menge { get; set; }
    public DateTime Am { get; set; }
    /// <summary>Sage <c>BelegnummerFormatiert</c> (string(45)).</summary>
    public string? Beleg { get; set; }
}

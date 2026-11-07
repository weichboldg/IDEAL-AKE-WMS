namespace IdealAkeWms.Models;

/// <summary>
/// Artikel-Lieferant aus Sage (<c>ArtikelLieferantInfo</c>, v1.73.0, Spec 2026-11-06-sage-sdata-stammdaten-sync F10, Etappe E8c).
/// Nur vom Lese-Abgleich <c>ArtikelLieferantSync</c> geschrieben (Datenbasis Bestellvorschlaege G7, keine Oberflaeche).
/// Eindeutig je Artikel + Kreditor. Laengen laut SData-<c>$schema</c> (P-S1): Artikelnummer/Bestellnummer string(31),
/// Bezeichnung1/2 string(50), Einkaufsmengeneinheit string(5), Wiederbeschaffungszeit short.
/// </summary>
public class ArticleSupplier : AuditableEntity
{
    public int ArticleId { get; set; }
    public int CreditorId { get; set; }

    /// <summary>Sage-Artikelnummer, wie geliefert (getrimmt) — redundant zu <see cref="ArticleId"/>, fuer Nachvollziehbarkeit.</summary>
    public string Artikelnummer { get; set; } = string.Empty;

    /// <summary>Lieferanten-Artikelnummer (Sage <c>Bestellnummer</c>).</summary>
    public string? SageBestellnummer { get; set; }
    public string? Bezeichnung1 { get; set; }
    public string? Bezeichnung2 { get; set; }
    public decimal? Mindestbestellmenge { get; set; }
    public string? Einkaufsmengeneinheit { get; set; }
    public decimal? UmrechnungsfaktorEK { get; set; }
    /// <summary>v1.85.0: Dezimalstellen der EK-Einheit und Umrechnung Verpackungseinheit EK (SData ArtikelLieferantInfo, belegt 2026-11-08).</summary>
    public int? DezimalstellenEK { get; set; }
    public decimal? UmrechnungsfaktorVPEK { get; set; }
    /// <summary>Wiederbeschaffungszeit in Tagen (Sage short).</summary>
    public int? Wiederbeschaffungszeit { get; set; }
    public decimal? Einzelpreis { get; set; }

    /// <summary>Wahr, wenn <c>ArtikelVariantenExtended.Hauptlieferant</c> dieses Artikels = Kontonummer des Kreditors.</summary>
    public bool IstHauptlieferant { get; set; }
}

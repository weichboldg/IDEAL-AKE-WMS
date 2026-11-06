using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>
/// Sage-Zusatzfelder eines Artikels (Spec 2026-11-06-sage-sdata-stammdaten-sync, E3a/F5) — 1:1-Satellit zu <see cref="Article"/>
/// (Muster <see cref="ProductionOrderExtraInfo"/>: UNIQUE-FK + Cascade, ADR 0009). Schreibt AUSSCHLIESSLICH der Artikel-Abgleich mit
/// Quelle SData (Schreiber <c>IDEALAKEWMSService</c>); View- und Vergleichs-Laeufe sowie der deprecated AgentJob fassen die Tabelle nie an —
/// so kann ein Rueckfall auf die View die Felder strukturell nicht nullen. Fehlt die Zeile, wurde der Artikel nie ueber SData gelesen.
/// In der App nur lesend (Artikelinfo). Property-Namen = SData-Feldnamen (1:1 nachvollziehbar), Laengen laut $schema (P-S1).
/// </summary>
public class ArticleSageDetail : AuditableEntity
{
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    // ----- Ressource Artikel -----
    [StringLength(50)] public string? Bezeichnung2 { get; set; }
    [StringLength(15)] public string? Hauptartikelgruppe { get; set; }
    [StringLength(15)] public string? Vaterartikelgruppe { get; set; }
    [StringLength(20)] public string? Hersteller { get; set; }
    [StringLength(20)] public string? HArtikelnummer { get; set; }
    [StringLength(50)] public string? Zeichnungsnummer { get; set; }
    [StringLength(31)] public string? Ersatzartikelnummer { get; set; }
    public string? Langtext { get; set; }
    [StringLength(10)] public string? Gewichtseinheit { get; set; }
    public int? DezimalstellenLager { get; set; }
    public bool? Lagerfuehrung { get; set; }
    /// <summary>Hauptlagerplatz als Sage-PlatzID (<c>Artikel.PlatzID</c>); der Code steht wie bisher in <see cref="Article.SagePrimaryStorageLocation"/>.</summary>
    public int? SagePlatzId { get; set; }
    [StringLength(6)] public string? Entnahmeverfahren { get; set; }
    public int? Inventurmethode { get; set; }
    /// <summary>Sage liefert short (kein Boolean, P-S1) — Bedeutung der Werte ungeprueft, daher Zahl.</summary>
    public int? Chargenpflicht { get; set; }
    /// <summary>Sage liefert short (kein Boolean, P-S1) — Bedeutung der Werte ungeprueft, daher Zahl.</summary>
    public int? Nachweispflicht { get; set; }
    public bool? IstBestellartikel { get; set; }
    public bool? IstFertigungsartikel { get; set; }
    public bool? IstUnterbaugruppe { get; set; }
    public bool? IstErsatzteil { get; set; }
    public bool? IstVerschleissteil { get; set; }
    public bool? Aktiv { get; set; }
    public int? Stuecklistentyp { get; set; }
    [StringLength(1)] public string? Schwundart { get; set; }
    public decimal? Schwund { get; set; }
    public decimal? MinimaleLosgroesse { get; set; }
    public decimal? MaximaleLosgroesse { get; set; }
    public decimal? Laenge { get; set; }
    public decimal? Breite { get; set; }
    public decimal? Hoehe { get; set; }

    // ----- Ressource ArtikelVariantenExtended (Dispo) -----
    [StringLength(15)] public string? EANNummer { get; set; }
    [StringLength(1)] public string? ABCKlasse { get; set; }
    public decimal? Gewicht { get; set; }
    public decimal? Lagerbestand { get; set; }
    public decimal? LagerbestandVerfuegbar { get; set; }
    public decimal? LagerbestandOhneDispo { get; set; }
    public decimal? Bestellmenge { get; set; }
    public decimal? MittlererEK { get; set; }
    public int? Bestellzeitpunkt { get; set; }
    [StringLength(20)] public string? Hauptlieferant { get; set; }
    [StringLength(20)] public string? LetzterLieferant { get; set; }
    /// <summary>Wiederbeschaffungszeit (Sage short; Einheit laut Sage-Stamm, am AKE-Test 14).</summary>
    public int? Wiederbeschaffungszeit { get; set; }
    /// <summary><c>USER_LagerplatzID</c> der Artikelvariante.</summary>
    public int? UserLagerplatzId { get; set; }
    /// <summary><c>USER_Mindestbestand</c> der Artikelvariante (Sage int).</summary>
    public decimal? UserMindestbestand { get; set; }
}

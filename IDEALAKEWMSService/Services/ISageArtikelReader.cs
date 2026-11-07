namespace IDEALAKEWMSService.Services;

/// <summary>
/// Ein Artikel aus Sage, wie ihn der Artikel-Abgleich nach <c>Articles</c> schreibt (Felder = Spalten des bisherigen Selects
/// <see cref="SageArticleSql"/>, bereits geparst). <paramref name="Details"/> liefert nur der SData-Reader; null = „Quelle liefert die
/// Zusatzfelder nicht“ → der Satellit <c>ArticleSageDetails</c> bleibt unberuehrt (Spec 2026-11-06-sage-sdata-stammdaten-sync, F1/F5).
/// <paramref name="Benutzerfelder"/> (E3b): USER-Feld → Wert (Text, leer = null), ebenfalls nur vom SData-Reader; null = Merkmale unberuehrt.
/// </summary>
public sealed record SageArtikelDto(
    string ArticleNumber,
    string? Description,
    string? Unit,
    string? ArticleGroup,
    decimal? ReorderLevel,
    string? PrimaryStorageLocation,
    string? Matchcode,
    SageArtikelDetails? Details = null,
    IReadOnlyDictionary<string, string?>? Benutzerfelder = null);

/// <summary>
/// Ergebnis eines Artikel-Lesevorgangs. <paramref name="QuelleFehltMeldung"/> != null: die konfigurierte Artikel-View
/// (<c>Sync:ArticlesViewName</c>) existiert nicht — der Abgleich protokolliert die Warnung und ueberspringt (Verhalten wie vor E3a).
/// </summary>
public sealed record SageArtikelLesung(List<SageArtikelDto> Artikel, string? QuelleFehltMeldung = null);

/// <summary>Quelle des Artikel-Abgleichs (A3). View/SQL = <see cref="SageArtikelReader"/>, SData = <c>Sdata.SdataArtikelReader</c>.</summary>
public interface ISageArtikelReader
{
    Task<SageArtikelLesung> GetAllAsync(CancellationToken ct = default);
}

/// <summary>
/// Sage-Zusatzfelder fuer den Satelliten <c>ArticleSageDetails</c> (E3a). Property-Namen = Spaltennamen der Entitaet
/// <c>IdealAkeWms.Models.ArticleSageDetail</c> — der Schreiber kopiert per EF <c>SetValues</c> nach Namen.
/// </summary>
public sealed record SageArtikelDetails
{
    // Artikel
    public string? Bezeichnung2 { get; init; }
    public string? Hauptartikelgruppe { get; init; }
    public string? Vaterartikelgruppe { get; init; }
    public string? Hersteller { get; init; }
    public string? HArtikelnummer { get; init; }
    public string? Zeichnungsnummer { get; init; }
    public string? Ersatzartikelnummer { get; init; }
    public string? Langtext { get; init; }
    public string? Gewichtseinheit { get; init; }
    public string? Basismengeneinheit { get; init; }
    public int? DezimalstellenBasis { get; init; }
    public string? Lagermengeneinheit { get; init; }
    public decimal? UmrechnungsFaktorLME { get; init; }
    public string? Verkaufsmengeneinheit { get; init; }
    public int? DezimalstellenVK { get; init; }
    public decimal? UmrechnungsfaktorVK { get; init; }
    public string? VerpackungseinheitVK { get; init; }
    public decimal? UmrechnungsfaktorVPVK { get; init; }
    public int? DezimalstellenLager { get; init; }
    public bool? Lagerfuehrung { get; init; }
    public int? SagePlatzId { get; init; }
    public string? Entnahmeverfahren { get; init; }
    public int? Inventurmethode { get; init; }
    public int? Chargenpflicht { get; init; }
    public int? Nachweispflicht { get; init; }
    public bool? IstBestellartikel { get; init; }
    public bool? IstFertigungsartikel { get; init; }
    public bool? IstUnterbaugruppe { get; init; }
    public bool? IstErsatzteil { get; init; }
    public bool? IstVerschleissteil { get; init; }
    public bool? Aktiv { get; init; }
    public int? Stuecklistentyp { get; init; }
    public string? Schwundart { get; init; }
    public decimal? Schwund { get; init; }
    public decimal? MinimaleLosgroesse { get; init; }
    public decimal? MaximaleLosgroesse { get; init; }
    public decimal? Laenge { get; init; }
    public decimal? Breite { get; init; }
    public decimal? Hoehe { get; init; }

    // ArtikelVariantenExtended (Dispo)
    public string? EANNummer { get; init; }
    public string? ABCKlasse { get; init; }
    public decimal? Gewicht { get; init; }
    public decimal? Lagerbestand { get; init; }
    public decimal? LagerbestandVerfuegbar { get; init; }
    public decimal? LagerbestandOhneDispo { get; init; }
    public decimal? Bestellmenge { get; init; }
    public decimal? MittlererEK { get; init; }
    public int? Bestellzeitpunkt { get; init; }
    public string? Hauptlieferant { get; init; }
    public string? LetzterLieferant { get; init; }
    public int? Wiederbeschaffungszeit { get; init; }
    public int? UserLagerplatzId { get; init; }
    public decimal? UserMindestbestand { get; init; }
}

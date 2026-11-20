using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Data.Repositories;

public interface IStockMovementRepository : IRepository<StockMovement>
{
    /// <summary>Bestand je Artikel und Lagerplatz. <paramref name="filterArticle"/> = Teiltextsuche über Artikelnummer, Bezeichnung und
    /// Matchcode (Suchmasken wie /StockOverview). Für den Bestand eines bestimmten Artikels NICHT verwenden, sondern das Ergebnis nach
    /// <c>ArticleId</c> filtern bzw. <see cref="GetStockByArticleNumbersAsync(List{string}, bool)"/> nehmen (v1.99.45).</summary>
    Task<List<StockOverviewItem>> GetCurrentStockAsync(
        string? filterArticle = null,
        int? filterStorageLocationId = null,
        decimal? filterMinQuantity = null,
        decimal? filterMaxQuantity = null);

    /// <summary>
    /// Zeigt Bestände für Artikel eines Fertigungsauftrags. Die Kandidaten-Lagerplätze werden in
    /// beiden Pfaden über das <c>ProductionOrder</c>-Tag der Bewegungen ermittelt; nur die
    /// Mengenberechnung unterscheidet sich:
    /// <para>
    /// <paramref name="onlyActualStock"/> = <c>true</c> (Default, Einbuchungs-Hinweis + Tracking-Modal):
    /// Menge = tatsächlicher Ist-Bestand am Artikel/Lagerplatz-Paar (Summe ALLER Bewegungen dort,
    /// analog <see cref="GetCurrentStockAtLocationAsync"/>); Kandidaten mit Ist-Bestand ≤ 0 fallen raus.
    /// </para>
    /// <para>
    /// <paramref name="onlyActualStock"/> = <c>false</c> (StockOverview-FA-Filter „Artikelbestände"):
    /// historisches Verhalten — Netto-Summe NUR der FA-getaggten Bewegungen je Paar (inkl. der
    /// bekannten Phantom-Menge bei komplett ungetaggt ausgebuchten FAs); bewusst unverändert.
    /// </para>
    /// </summary>
    Task<List<StockOverviewItem>> GetStockByProductionOrderAsync(string productionOrder, bool onlyActualStock = true);

    Task<(List<MovementHistoryItem> Items, int TotalCount)> GetMovementHistoryAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? filterArticle = null,
        int? filterStorageLocationId = null,
        MovementType? filterMovementType = null,
        int? filterUserId = null,
        string? filterProductionOrder = null,
        int page = 1,
        int pageSize = 50,
        IReadOnlyDictionary<string, string>? columnFilters = null);

    Task<Dictionary<string, List<StockLocationInfo>>> GetStockByArticleNumbersAsync(List<string> articleNumbers);

    /// <summary>Wie <see cref="GetStockByArticleNumbersAsync(List{string})"/>; <paramref name="mitKommissionierwagen"/> = true zaehlt Kommissionierwagen
    /// auf beiden Seiten mit (Bestand wie die Artikelinfo, v1.99.30 Q2 Variante b – nur die Bestellvorschlaege).</summary>
    Task<Dictionary<string, List<StockLocationInfo>>> GetStockByArticleNumbersAsync(List<string> articleNumbers, bool mitKommissionierwagen);

    Task<decimal> GetCurrentStockAtLocationAsync(int articleId, int storageLocationId);

    Task<List<string>> GetProductionOrdersAtLocationAsync(int storageLocationId);

    /// <summary>v1.99.47 (Teil 6): FA-Eigenschaften je Artikel mit Bestand &gt; 0 am Platz – alle Nummern (Spalte), Nummern aus Zugaengen,
    /// Zugang ohne FA-Text. Artikel ohne jede Bewegung mit FA-Text fehlen oder haben leere <c>Nummern</c>.</summary>
    Task<Dictionary<int, FaAmPaar>> GetProductionOrdersByArticleAtLocationAsync(int storageLocationId);

    /// <summary>
    /// Liefert den aggregierten WMS-Bestand pro (ArticleId, StorageLocationId).
    /// Wird vom LagerbestandSyncService genutzt, um effizient gegen Sage-Bestand zu vergleichen.
    /// Beruecksichtigt alle MovementType-Werte inklusive Umbuchung-Quell-Seite.
    /// </summary>
    Task<Dictionary<(int ArticleId, int StorageLocationId), decimal>> GetCurrentStockByArticleAndLocationAsync();

    /// <summary>
    /// v1.99.30 (Spec 2026-11-16-bestellvorschlaege-nachbesserung F4, Antwort 2): Summe der Ausbuchungen je (FA-Id, Artikelnummer) fuer den
    /// FA-Rest „BOM-Menge − schon ausgebucht“. Nur <see cref="Models.MovementType.Ausbuchung"/>: die Einbuchung setzt ebenfalls
    /// <c>ProductionOrderId</c>; welche weiteren Arten zaehlen, klaert P-N8 – bis dahin nicht annehmen. Artikelnummer getrimmt, gross.
    /// </summary>
    Task<Dictionary<(int ProductionOrderId, string ArticleNumber), decimal>> GetAusbuchungenJeFaAsync(IReadOnlyCollection<int> productionOrderIds);
}

/// <summary>v1.99.47: FA-Bezug eines Paars (Artikel, Lagerplatz). <c>Nummern</c> distinct, ordinal sortiert (= Spalteninhalt);
/// <c>ZugangNummern</c> mit OrdinalIgnoreCase; <c>ZugangOhneFa</c> = ein Zugang (Ein-/Sage-Ein-/Umbuchung) ohne FA-Text.</summary>
public sealed record FaAmPaar(List<string> Nummern, HashSet<string> ZugangNummern, bool ZugangOhneFa);

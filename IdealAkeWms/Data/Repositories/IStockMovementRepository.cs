using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Data.Repositories;

public interface IStockMovementRepository : IRepository<StockMovement>
{
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

    Task<decimal> GetCurrentStockAtLocationAsync(int articleId, int storageLocationId);

    Task<List<string>> GetProductionOrdersAtLocationAsync(int storageLocationId);

    /// <summary>
    /// Liefert den aggregierten WMS-Bestand pro (ArticleId, StorageLocationId).
    /// Wird vom LagerbestandSyncService genutzt, um effizient gegen Sage-Bestand zu vergleichen.
    /// Beruecksichtigt alle MovementType-Werte inklusive Umbuchung-Quell-Seite.
    /// </summary>
    Task<Dictionary<(int ArticleId, int StorageLocationId), decimal>> GetCurrentStockByArticleAndLocationAsync();
}

namespace IdealAkeWms.Services.SyncLogger;

/// <summary>
/// Single Source of Truth fuer die Service-Namen, die in der <c>SyncLogs.Service</c>-Spalte
/// erscheinen duerfen. Wird von <see cref="ISyncLogger.BeginRunAsync"/> verwendet und
/// vom <c>SyncLogController</c> als Dropdown-Quelle (<c>KnownServices</c>).
/// </summary>
public static class SyncLogServices
{
    public const string Lagerplatz = "Lagerplatz";
    public const string Lagerbestand = "Lagerbestand";
    public const string BomCache = "BomCache";
    public const string OseonTracking = "OseonTracking";
    public const string OseonWorkplaces = "OseonWorkplaces";
    public const string OseonArticleCategories = "OseonArticleCategories";
    public const string EnaioDms = "EnaioDms";
    public const string Holiday = "Holiday";
    public const string CoatingDetection = "CoatingDetection";
    public const string ProductionOrder = "ProductionOrder";  // SageImport-Teil 1
    public const string Article = "Article";                  // SageImport-Teil 2

    // Non-Sync-Aktivitaeten (seit v1.15.1)
    public const string PartRequisitionEmail = "PartRequisitionEmail";
    public const string WarehouseRequisitionEmail = "WarehouseRequisitionEmail";
    public const string BdeAutoPause = "BdeAutoPause";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        Lagerplatz, Lagerbestand, BomCache,
        OseonTracking, OseonWorkplaces, OseonArticleCategories,
        EnaioDms, Holiday, CoatingDetection, ProductionOrder, Article,
        PartRequisitionEmail, WarehouseRequisitionEmail, BdeAutoPause,
    };
}

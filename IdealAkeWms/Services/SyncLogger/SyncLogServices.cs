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
    public const string FaWorkStepDetection = "FaWorkStepDetection";
    public const string ProductionOrder = "ProductionOrder";  // SageImport-Teil 1
    public const string ProductionOrderReconciliation = "ProductionOrderReconciliation"; // SageImport-Reconcile (v1.25.0)
    public const string Article = "Article";                  // SageImport-Teil 2
    public const string FaZusatzinfo = "FaZusatzinfo";        // FA-Zusatzinfos aus Sage (v1.26.0)
    public const string SageLagerbuchung = "SageLagerbuchung"; // Ausgehende Lagerbuchungen WMS -> Sage (SData)

    // Non-Sync-Aktivitaeten (seit v1.15.1)
    public const string PartRequisitionEmail = "PartRequisitionEmail";
    public const string WarehouseRequisitionEmail = "WarehouseRequisitionEmail";
    public const string BdeAutoPause = "BdeAutoPause";

    // Cleanup-Jobs (seit v1.25.0)
    public const string CleanupActivityLog = "CleanupAktivitaetsprotokoll";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        Lagerplatz, Lagerbestand, BomCache,
        OseonTracking, OseonWorkplaces, OseonArticleCategories,
        EnaioDms, Holiday, CoatingDetection, FaWorkStepDetection,
        ProductionOrder, ProductionOrderReconciliation, Article, FaZusatzinfo,
        SageLagerbuchung,
        PartRequisitionEmail, WarehouseRequisitionEmail, BdeAutoPause,
        CleanupActivityLog,
    };
}

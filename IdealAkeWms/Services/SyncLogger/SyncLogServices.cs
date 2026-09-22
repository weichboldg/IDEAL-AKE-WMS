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
    public const string FaHierarchy = "FaHierarchy"; // Mehrstufiger FA-Struktur-/Stuecklisten-Import (IDEAL) aus Sage-Views
    public const string HierarchieUmstellung = "HierarchieUmstellung"; // Audit: Master-Einwegtor umgelegt / Schreibversuch abgelehnt (Teil 7)
    public const string FaMaterialization = "FaMaterialization"; // Struktur -> ProductionOrders (Teil 7 Etappe C)
    public const string WorkOperationStructureDetection = "WorkOperationStructureDetection"; // IDEAL: WorkOperation-Arbeitsgaenge aus FaHierarchyNode.Arbeitsschritte, Match ueber ProductionWorkplace.ArbeitsschrittCode (v1.44.0, war FaWorkStepStructureDetection/v1.41.0)
    public const string ProductionWorkplaceSync = "ProductionWorkplaceSync"; // IDEAL: Werkbank-Anlage aus Sage KHKPpsArbeitsplaetze (v1.44.0)

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
        SageLagerbuchung, FaHierarchy, HierarchieUmstellung, FaMaterialization,
        WorkOperationStructureDetection, ProductionWorkplaceSync,
        PartRequisitionEmail, WarehouseRequisitionEmail, BdeAutoPause,
        CleanupActivityLog,
    };
}

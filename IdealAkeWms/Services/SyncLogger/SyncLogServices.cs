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
    public const string OperationConfigSync = "OperationConfigSync"; // Arbeitsgang-Abgleich aus der Sage-View in die Arbeitsgang-Stammdaten (v1.61.0)
    public const string SageQuellenvergleich = "SageQuellenvergleich"; // Quellen-Weichen View|SData|Vergleich: Abweichungen + Schalter-Warnungen (v1.73.0)
    public const string PpsMitarbeiter = "PpsMitarbeiter"; // Sage-PPS-Mitarbeiter (SData) -> Cache SagePpsMitarbeiter (v1.73.0 E6b)
    public const string Stillstandsgruende = "Stillstandsgruende"; // Sage-Stillstandsbegruendungen (SData) -> Stillstandsgruende (v1.73.0 E6b)
    public const string Kreditoren = "Kreditoren"; // Sage-Kontokorrent KtoArt K (SData) -> Creditors (v1.73.0 E8c)
    public const string Debitoren = "Debitoren"; // Sage-Kontokorrent KtoArt D (SData) -> Debitors (v1.73.0 E8c)
    public const string ArtikelLieferant = "ArtikelLieferant"; // Sage-ArtikelLieferantInfo (SData) -> ArticleSuppliers (v1.73.0 E8c)

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
        WorkOperationStructureDetection, ProductionWorkplaceSync, OperationConfigSync, SageQuellenvergleich,
        PpsMitarbeiter, Stillstandsgruende, Kreditoren, Debitoren, ArtikelLieferant,
        PartRequisitionEmail, WarehouseRequisitionEmail, BdeAutoPause,
        CleanupActivityLog,
    };

    /// <summary>Deutsche Anzeigenamen fuer Auswahlfeld, Tabellenspalte und Spaltenfilter im Aktivitaets-Protokoll (v1.70.0).
    /// Namen nach dem Text, mit dem der Dienst den Lauf im Log nennt. Technischer Name bleibt Wert/Tooltip.</summary>
    public static IReadOnlyDictionary<string, string> DisplayNames { get; } = new Dictionary<string, string>
    {
        [Lagerplatz] = "Lagerplatz-Sync",
        [Lagerbestand] = "Lagerbestand-Sync",
        [BomCache] = "BOM-Cache-Sync",
        [OseonTracking] = "OSEON-Tracking-Sync",
        [OseonWorkplaces] = "Werkbank-Sync (OSEON → Produktionsaufträge)",
        [OseonArticleCategories] = "OSEON-Artikelkategorie-Sync",
        [EnaioDms] = "enaio DMS-Sync",
        [Holiday] = "Feiertags-Sync",
        [CoatingDetection] = "Lackierteil-Erkennung",
        [FaWorkStepDetection] = "FA-Arbeitsgang-Erkennung (BOM-Cache)",
        [ProductionOrder] = "Produktionsaufträge-Sync",
        [ProductionOrderReconciliation] = "FA-Reconciliation (Storno verwaister FAs)",
        [Article] = "Artikel-Sync",
        [FaZusatzinfo] = "FA-Zusatzinfo-Sync",
        [SageLagerbuchung] = "Sage-Lagerbuchung",
        [FaHierarchy] = "FA-Hierarchie-Sync",
        [HierarchieUmstellung] = "FA-Hierarchie-Umstellung",
        [FaMaterialization] = "FA-Materialisierung",
        [WorkOperationStructureDetection] = "Arbeitsgang-Erkennung (Struktur)",
        [ProductionWorkplaceSync] = "Werkbank-Anlage (Sage-Arbeitsplätze)",
        [OperationConfigSync] = "Arbeitsgang-Abgleich (Sage-View)",
        [SageQuellenvergleich] = "Sage-Quellenvergleich (View/SData)",
        [PpsMitarbeiter] = "PPS-Mitarbeiter-Abgleich (Sage)",
        [Stillstandsgruende] = "Stillstandsgründe-Abgleich (Sage)",
        [Kreditoren] = "Kreditoren-Abgleich (Sage)",
        [Debitoren] = "Debitoren-Abgleich (Sage)",
        [ArtikelLieferant] = "Artikel-Lieferanten-Abgleich (Sage)",
        [PartRequisitionEmail] = "Bedarfsmeldungs-E-Mail-Versand",
        [WarehouseRequisitionEmail] = "Lagerbestellungs-E-Mail-Versand",
        [BdeAutoPause] = "BDE-AutoPause",
        [CleanupActivityLog] = "Aktivitätsprotokoll-Bereinigung",
    };

    /// <summary>Anzeigename; unbekannter Name (Altzeile) bleibt technisch.</summary>
    public static string DisplayName(string service) => DisplayNames.TryGetValue(service, out var name) ? name : service;
}

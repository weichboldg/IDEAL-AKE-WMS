using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using Microsoft.Data.SqlClient;
using System.Text;

namespace IDEALAKEWMSService.Services;

public class SageImportService : ISageImportService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SageImportService> _logger;
    private readonly ISyncLogger _syncLogger;
    private readonly IBomCacheSyncService _bomCacheSync;
    private readonly ICoatingDetectionService _coatingDetection;
    private readonly ISyncErrorNotifier _errorNotifier;

    public SageImportService(
        IConfiguration configuration,
        ILogger<SageImportService> logger,
        ISyncLogger syncLogger,
        IBomCacheSyncService bomCacheSync,
        ICoatingDetectionService coatingDetection,
        ISyncErrorNotifier errorNotifier)
    {
        _configuration = configuration;
        _logger = logger;
        _syncLogger = syncLogger;
        _bomCacheSync = bomCacheSync;
        _coatingDetection = coatingDetection;
        _errorNotifier = errorNotifier;
    }

    public async Task<SyncResult> SyncProductionOrdersAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.ProductionOrder, ct);
        try
        {
            var sageConnection = _configuration.GetConnectionString("SageConnection")
                ?? throw new InvalidOperationException("SageConnection nicht konfiguriert.");
            var wmsConnection = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");

            if (dryRun)
                _logger.LogInformation("[DryRun] Produktionsaufträge-Sync — keine Änderungen werden geschrieben.");

            // Daten aus SAGE lesen
            const string sageSql = """
                SELECT DISTINCT
                    CAST([WA Nummer] AS nvarchar(100))                           AS OrderNumber,
                    CAST([Stückzahl] AS decimal(18,3))                           AS Quantity,
                    CAST([Kunde] AS nvarchar(200)) COLLATE Latin1_General_CI_AS  AS Customer,
                    CAST([Artikelnummer] AS nvarchar(100)) COLLATE Latin1_General_CI_AS AS ArticleNumber,
                    CAST([Bezeichnung1] AS nvarchar(500)) COLLATE Latin1_General_CI_AS  AS Description1,
                    CAST([Bezeichnung2] AS nvarchar(500)) COLLATE Latin1_General_CI_AS  AS Description2,
                    CAST([Fertigungstermin] AS date)                             AS ProductionDate,
                    CAST([Liefertermin] AS date)                                 AS DeliveryDate
                FROM [dbo].[vw_AKE_Kommissionierung_WAListe]
                WHERE [WA Nummer] IS NOT NULL
                """;

            var sageOrders = new List<(string OrderNumber, decimal Quantity, string? Customer,
                string? ArticleNumber, string? Description1, string? Description2,
                DateTime? ProductionDate, DateTime? DeliveryDate)>();

            await using (var sageConn = new SqlConnection(sageConnection))
            {
                await sageConn.OpenAsync(ct);
                await using var cmd = new SqlCommand(sageSql, sageConn);
                cmd.CommandTimeout = 120;
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    sageOrders.Add((
                        OrderNumber: reader.GetString(0),
                        Quantity: reader.IsDBNull(1) ? 0 : reader.GetDecimal(1),
                        Customer: reader.IsDBNull(2) ? null : reader.GetString(2),
                        ArticleNumber: reader.IsDBNull(3) ? null : reader.GetString(3),
                        Description1: reader.IsDBNull(4) ? null : reader.GetString(4),
                        Description2: reader.IsDBNull(5) ? null : reader.GetString(5),
                        ProductionDate: reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                        DeliveryDate: reader.IsDBNull(7) ? null : reader.GetDateTime(7)
                    ));
                }
            }

            _logger.LogInformation("SAGE liefert {Count} Produktionsaufträge.", sageOrders.Count);

            if (dryRun)
            {
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["gelesen"] = sageOrders.Count,
                    ["neu"] = 0,
                    ["aktualisiert"] = 0,
                }, messageSuffix: "[DryRun]", ct: ct);
                return new SyncResult(0, 0, 0, $"DryRun: {sageOrders.Count} Datensätze aus SAGE gelesen.");
            }

            int inserted = 0, updated = 0;
            var newArticleNumbers = new List<string>();
            var newOrderIds = new List<int>();

            await using var wmsConn = new SqlConnection(wmsConnection);
            await wmsConn.OpenAsync(ct);

            // Schema-Bewusstsein: Das IDEAL-Schema hat ProductionOrders.SubOrderNumber (NOT NULL + UNIQUE),
            // das AKE-Schema nicht. Der INSERT-Zweig schreibt SubOrderNumber=OrderNumber NUR wenn die Spalte
            // existiert — sonst wuerde der INSERT gegen das IDEAL-Schema mit SqlException 515 scheitern.
            bool hasSubOrderNumber;
            await using (var colCmd = new SqlCommand(
                "SELECT CASE WHEN COL_LENGTH('dbo.ProductionOrders','SubOrderNumber') IS NULL THEN 0 ELSE 1 END", wmsConn))
            {
                hasSubOrderNumber = Convert.ToInt32(await colCmd.ExecuteScalarAsync(ct)) == 1;
            }
            var mergeSql = SageProductionOrderSql.BuildUpsert(hasSubOrderNumber);
            if (hasSubOrderNumber)
                _logger.LogInformation("ProductionOrders-Sync: SubOrderNumber-Spalte vorhanden — wird mit OrderNumber befuellt (IDEAL-Schema-Kompatibilitaet).");

            foreach (var orderFromSage in sageOrders)
            {
                await using var cmd = new SqlCommand(mergeSql, wmsConn);
                cmd.Parameters.AddWithValue("@OrderNumber", orderFromSage.OrderNumber);
                cmd.Parameters.AddWithValue("@Quantity", orderFromSage.Quantity);
                cmd.Parameters.AddWithValue("@Customer", (object?)orderFromSage.Customer ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ArticleNumber", (object?)orderFromSage.ArticleNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Description1", (object?)orderFromSage.Description1 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Description2", (object?)orderFromSage.Description2 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProductionDate", (object?)orderFromSage.ProductionDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DeliveryDate", (object?)orderFromSage.DeliveryDate ?? DBNull.Value);

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    int? newId = reader.IsDBNull(0) ? null : Convert.ToInt32(reader.GetValue(0));
                    var affected = reader.GetInt32(1);
                    var isInsert = reader.GetInt32(2) == 1;

                    if (affected > 0 && isInsert)
                    {
                        inserted++;
                        if (newId.HasValue && !string.IsNullOrWhiteSpace(orderFromSage.ArticleNumber))
                        {
                            newArticleNumbers.Add(orderFromSage.ArticleNumber);
                            newOrderIds.Add(newId.Value);
                        }
                    }
                    else if (affected > 0)
                    {
                        updated++;
                    }
                }
            }

            // Eager-create der Status-Zeilen fuer neue FAs (Phase 1 Spec 9, analog AgentJob).
            // 2 idempotente MERGEs: PickingStatus (1:1), BdeStatus (1:1).
            // AssemblyGroups-MERGE entfernt in v1.22.0 (FaWorkSteps via Detection-Sync).
            // WHEN NOT MATCHED BY TARGET only — bestehende user-gesetzte Werte werden nie ueberschrieben.
            if (inserted > 0)
            {
                const string eagerCreateSql = """
                    MERGE [dbo].[ProductionOrderPickingStatus] AS s
                    USING (SELECT Id AS ProductionOrderId FROM [dbo].[ProductionOrders]) AS p
                    ON s.ProductionOrderId = p.ProductionOrderId
                    WHEN NOT MATCHED BY TARGET THEN
                        INSERT (ProductionOrderId, IsReleasedForPicking, HasGlass, HasExternalPurchase,
                                HasCoatingParts, IsCoatingDone, IsDonePicking,
                                CreatedAt, CreatedBy, CreatedByWindows)
                        VALUES (p.ProductionOrderId, 0, 0, 0, 0, 0, 0,
                                GETUTCDATE(), 'IDEALAKEWMSService', SYSTEM_USER);

                    MERGE [dbo].[ProductionOrderBdeStatus] AS s
                    USING (SELECT Id AS ProductionOrderId FROM [dbo].[ProductionOrders]) AS p
                    ON s.ProductionOrderId = p.ProductionOrderId
                    WHEN NOT MATCHED BY TARGET THEN
                        INSERT (ProductionOrderId, IsDoneBde,
                                CreatedAt, CreatedBy, CreatedByWindows)
                        VALUES (p.ProductionOrderId, 0,
                                GETUTCDATE(), 'IDEALAKEWMSService', SYSTEM_USER);
                    """;

                await using var eagerCmd = new SqlCommand(eagerCreateSql, wmsConn) { CommandTimeout = 60 };
                var statusRows = await eagerCmd.ExecuteNonQueryAsync(ct);
                _logger.LogInformation("Sage-Import Eager-Create: {Rows} Status-Zeilen ergaenzt fuer neue FAs", statusRows);
            }

            // Hook: BOM-Cache + Coating Detection fuer neue Auftraege
            var bomCacheEnabled = await ServiceSettings.GetBoolAsync(_configuration, "Sync:BomCacheEnabled", false, ct);
            if (bomCacheEnabled && newArticleNumbers.Count > 0)
            {
                try
                {
                    var distinctNew = newArticleNumbers.Distinct().ToList();
                    _logger.LogInformation("Sage-Import Hook: starte narrow BOM-Cache fuer {N} neue Artikel", distinctNew.Count);
                    await _bomCacheSync.SyncSpecificArticleNumbersAsync(distinctNew, dryRun, ct);

                    _logger.LogInformation("Sage-Import Hook: starte Lackierteil-Erkennung fuer {N} neue Auftraege", newOrderIds.Count);
                    await _coatingDetection.DetectAndUpdateCoatingFlagsAsync(dryRun, newOrderIds, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Sage-Import Hook (BOM-Cache / Coating-Detection) fehlgeschlagen");
                }
            }

            // ---- FA-Reconciliation (v1.25.0) ----------------------------------
            // Verwaiste WMS-offene FAs, die nicht mehr in der Sage-View sind, stornieren;
            // wieder aufgetauchte reaktivieren. Guard (leerer Read) + Cap schuetzen vor
            // versehentlichem Massen-Stornieren. Opt-in per Flag; DryRun schreibt nichts.
            //
            // Eigener Aktivitaets-Protokoll-Lauf (v1.25.0-Followup): Der Reconcile ist zwar
            // in den ProductionOrder-Sync eingebettet, erscheint aber als SEPARATER SyncLog-
            // Eintrag (ProductionOrderReconciliation) — auch im Flag-AUS-Fall (Info: was WUERDE
            // passieren). Ein Fehler hier laesst den reconcileRun als "failed" enden und wird
            // (wie bisher) an den aeusseren ProductionOrder-Lauf weitergereicht (re-throw).
            await using var reconcileRun = await _syncLogger.BeginRunAsync(SyncLogServices.ProductionOrderReconciliation, ct);
            try
            {
                var reconcileEnabled = await ServiceSettings.GetBoolAsync(
                    _configuration, "Sync:ProductionOrderReconcileEnabled", false, ct);
                var maxCancelPerRun = await ServiceSettings.GetIntAsync(
                    _configuration, "Sync:ReconcileMaxCancelPerRun", 100, ct);

                var sageOrderNumbers = sageOrders.Select(o => o.OrderNumber).ToList();

                // WMS-Zustaende laden: alle offenen ODER stornierten FAs.
                var wmsStates = new List<WmsOrderState>();
                await using (var stateCmd = new SqlCommand(
                    "SELECT [OrderNumber], [IsDone], [IsCancelled] FROM [dbo].[ProductionOrders] WHERE [IsDone] = 0 OR [IsCancelled] = 1",
                    wmsConn) { CommandTimeout = 120 })
                await using (var stateReader = await stateCmd.ExecuteReaderAsync(ct))
                {
                    while (await stateReader.ReadAsync(ct))
                    {
                        wmsStates.Add(new WmsOrderState(
                            stateReader.GetString(0),
                            stateReader.GetBoolean(1),
                            stateReader.GetBoolean(2)));
                    }
                }

                var reconcilePlan = ProductionOrderReconciler.Plan(sageOrderNumbers, wmsStates, maxCancelPerRun);
                int cancelled = 0, reactivated = 0;

                if (!reconcileEnabled)
                {
                    _logger.LogInformation(
                        "FA-Reconciliation deaktiviert (Sync:ProductionOrderReconcileEnabled=false) — Plan: {Cancel} Storno-Kandidaten, {React} Reaktivierungen (nichts geschrieben).",
                        reconcilePlan.ToCancel.Count, reconcilePlan.ToReactivate.Count);
                    await reconcileRun.LogInfoAsync(
                        $"Deaktiviert (Sync:ProductionOrderReconcileEnabled=false) — Plan: {reconcilePlan.ToCancel.Count} Storno-Kandidaten, {reconcilePlan.ToReactivate.Count} Reaktivierungen — nichts geschrieben", ct: ct);
                    await reconcileRun.FinishSuccessAsync(new Dictionary<string, int>
                    {
                        ["storniert"] = 0,
                        ["reaktiviert"] = 0,
                        ["storno-kandidaten"] = reconcilePlan.ToCancel.Count,
                    }, ct: ct);
                }
                else if (dryRun)
                {
                    // Hinweis: Aktuell nicht erreichbar — der DryRun-Pfad returned frueh (siehe oben,
                    // direkt nach dem Sage-Read), bevor die Reconcile-Sektion laeuft. Defensiv belassen,
                    // falls der fruehe Return spaeter entfaellt (dann greift diese schreibfreie Vorschau).
                    _logger.LogInformation(
                        "[DryRun] FA-Reconciliation — {Cancel} Storno-Kandidaten, {React} Reaktivierungen (nichts geschrieben). Skipped={Skipped} {Reason}",
                        reconcilePlan.ToCancel.Count, reconcilePlan.ToReactivate.Count,
                        reconcilePlan.Skipped, reconcilePlan.SkipReason);
                    await reconcileRun.LogInfoAsync(
                        $"[DryRun] Reconcile: {reconcilePlan.ToCancel.Count} wuerden storniert, {reconcilePlan.ToReactivate.Count} reaktiviert.", ct: ct);
                    await reconcileRun.FinishSuccessAsync(new Dictionary<string, int>
                    {
                        ["storniert"] = 0,
                        ["reaktiviert"] = 0,
                        ["storno-kandidaten"] = reconcilePlan.ToCancel.Count,
                    }, messageSuffix: "[DryRun]", ct: ct);
                }
                else
                {
                    // Reaktivieren laeuft IMMER (auch bei Guard/Cap-Skip — Reaktivierungen sind nie gefaehrlich).
                    foreach (var orderNumber in reconcilePlan.ToReactivate)
                    {
                        await using var reactCmd = new SqlCommand(
                            "UPDATE [dbo].[ProductionOrders] SET [IsCancelled] = 0, [CancelledAt] = NULL, [CancelledBy] = NULL, " +
                            "[ModifiedAt] = GETUTCDATE(), [ModifiedBy] = 'IDEALAKEWMSService', [ModifiedByWindows] = SYSTEM_USER " +
                            "WHERE [OrderNumber] = @OrderNumber",
                            wmsConn) { CommandTimeout = 60 };
                        reactCmd.Parameters.AddWithValue("@OrderNumber", orderNumber);
                        var reactRows = await reactCmd.ExecuteNonQueryAsync(ct);
                        reactivated += reactRows;
                        if (reactRows > 0)
                            await reconcileRun.LogInfoAsync(
                                $"FA {orderNumber} reaktiviert (wieder in Sage)", reference: orderNumber, ct: ct);
                    }

                    if (reconcilePlan.Skipped)
                    {
                        var reason = reconcilePlan.SkipReason ?? "unbekannt";
                        _logger.LogWarning("FA-Reconciliation uebersprungen: {Reason}. Kein Storno geschrieben.", reason);
                        await reconcileRun.LogWarningAsync($"Uebersprungen: {reason}", ct: ct);

                        // Cap-Skip zusaetzlich per Fehlermail melden (Sage-Teil-Read-Verdacht).
                        if (reason.StartsWith("Cap", StringComparison.OrdinalIgnoreCase))
                        {
                            await _errorNotifier.NotifyAsync(
                                SyncLogServices.ProductionOrder,
                                new InvalidOperationException(
                                    $"FA-Reconciliation Cap ueberschritten: {reason}. Sage lieferte {sageOrderNumbers.Count} FAs, " +
                                    $"Cap={maxCancelPerRun}. Kein Storno geschrieben — moeglicher Sage-Teil-Read."),
                                ct);
                        }
                    }
                    else
                    {
                        foreach (var orderNumber in reconcilePlan.ToCancel)
                        {
                            // CancelledAt via GETUTCDATE() (wie ModifiedAt) — beide Audit-Spalten
                            // derselben Zeile in derselben Zeitbasis (kein Local/UTC-Mix).
                            await using var cancelCmd = new SqlCommand(
                                "UPDATE [dbo].[ProductionOrders] SET [IsCancelled] = 1, [CancelledAt] = GETUTCDATE(), [CancelledBy] = 'System-Reconcile', " +
                                "[ModifiedAt] = GETUTCDATE(), [ModifiedBy] = 'IDEALAKEWMSService', [ModifiedByWindows] = SYSTEM_USER " +
                                "WHERE [OrderNumber] = @OrderNumber AND [IsDone] = 0 AND [IsCancelled] = 0",
                                wmsConn) { CommandTimeout = 60 };
                            cancelCmd.Parameters.AddWithValue("@OrderNumber", orderNumber);
                            var cancelRows = await cancelCmd.ExecuteNonQueryAsync(ct);
                            cancelled += cancelRows;
                            if (cancelRows > 0)
                                await reconcileRun.LogInfoAsync(
                                    $"FA {orderNumber} storniert (in Sage nicht mehr vorhanden)", reference: orderNumber, ct: ct);
                        }
                    }

                    _logger.LogInformation(
                        "FA-Reconciliation: {Cancelled} storniert, {Reactivated} reaktiviert.", cancelled, reactivated);

                    await reconcileRun.FinishSuccessAsync(new Dictionary<string, int>
                    {
                        ["storniert"] = cancelled,
                        ["reaktiviert"] = reactivated,
                    }, ct: ct);
                }
            }
            catch (Exception reconcileEx)
            {
                _logger.LogError(reconcileEx, "Fehler bei FA-Reconciliation.");
                await reconcileRun.LogErrorAsync(reconcileEx.Message, ct: ct);
                await reconcileRun.FinishFailedAsync(reconcileEx.Message, ct: ct);
                throw; // Verhalten wie bisher: Fehler propagiert an den aeusseren ProductionOrder-Lauf.
            }
            // ---- Ende FA-Reconciliation ---------------------------------------

            _logger.LogInformation("Produktionsaufträge-Sync abgeschlossen: {Inserted} neu, {Updated} aktualisiert.", inserted, updated);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
            }, ct: ct);

            return new SyncResult(inserted, updated, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Produktionsaufträge-Sync.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }

    public async Task<SyncResult> SyncArticlesAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.Article, ct);
        try
        {
            var sageConnection = _configuration.GetConnectionString("SageConnection")
                ?? throw new InvalidOperationException("SageConnection nicht konfiguriert.");
            var wmsConnection = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");

            if (dryRun)
                _logger.LogInformation("[DryRun] Artikel-Sync — keine Änderungen werden geschrieben.");

            const string sageSql = """
                WITH RawArticles AS (
                    SELECT DISTINCT
                        CAST(r.Ressourcenummer AS nvarchar(100))   AS ArticleNumber,
                        CAST(a.Bezeichnung1 AS nvarchar(500))      AS Description,
                        CAST(a.Lagermengeneinheit AS nvarchar(20)) AS Unit,
                        CAST(a.Artikelgruppe AS nvarchar(100))     AS ArticleGroup,
                        CAST(v.Meldebestand AS nvarchar(20))       AS ReorderLevel,
                        CAST(lp.Kurzbezeichnung AS nvarchar(100))  AS PrimaryStorageLocation
                    FROM [dbo].[KHKPpsRessourcenPositionen] r
                    LEFT JOIN [dbo].[KHKArtikel] a ON a.Artikelnummer = r.Ressourcenummer
                    LEFT JOIN [dbo].[KHKArtikelvarianten] v ON a.Artikelnummer = v.Artikelnummer
                    LEFT JOIN [dbo].[KHKLagerplaetze] lp ON a.PlatzID = lp.PlatzID
                    WHERE r.Ressourcenummer IS NOT NULL AND r.Ressourcenummer != ''

                    UNION

                    SELECT
                        CAST(a.Artikelnummer AS nvarchar(100))     AS ArticleNumber,
                        CAST(a.Bezeichnung1 AS nvarchar(500))      AS Description,
                        CAST(a.Lagermengeneinheit AS nvarchar(20)) AS Unit,
                        CAST(a.Artikelgruppe AS nvarchar(100))     AS ArticleGroup,
                        CAST(v.Meldebestand AS nvarchar(20))       AS ReorderLevel,
                        CAST(lp.Kurzbezeichnung AS nvarchar(100))  AS PrimaryStorageLocation
                    FROM [dbo].[KHKArtikel] a
                    LEFT JOIN [dbo].[KHKArtikelvarianten] v ON a.Artikelnummer = v.Artikelnummer
                    LEFT JOIN [dbo].[KHKLagerplaetze] lp ON a.PlatzID = lp.PlatzID
                    WHERE a.IstBestellartikel = -1 AND a.Aktiv = -1
                )
                SELECT
                    ArticleNumber,
                    MAX(Description)  AS Description,
                    MAX(Unit)         AS Unit,
                    MAX(ArticleGroup) AS ArticleGroup,
                    MAX(ReorderLevel) AS ReorderLevel,
                    MAX(PrimaryStorageLocation) AS PrimaryStorageLocation
                FROM RawArticles
                WHERE ArticleNumber IS NOT NULL AND ArticleNumber != ''
                GROUP BY ArticleNumber
                """;

            var sageArticles = new List<(string ArticleNumber, string? Description, string? Unit, string? ArticleGroup, decimal? ReorderLevel, string? PrimaryStorageLocation)>();

            await using (var sageConn = new SqlConnection(sageConnection))
            {
                await sageConn.OpenAsync(ct);
                await using var cmd = new SqlCommand(sageSql, sageConn);
                cmd.CommandTimeout = 120;
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    string? reorderRaw = reader.IsDBNull(4) ? null : reader.GetString(4);
                    string? primaryRaw = reader.IsDBNull(5) ? null : reader.GetString(5);
                    sageArticles.Add((
                        ArticleNumber: reader.GetString(0),
                        Description:   reader.IsDBNull(1) ? null : reader.GetString(1),
                        Unit:          reader.IsDBNull(2) ? null : reader.GetString(2),
                        ArticleGroup:  reader.IsDBNull(3) ? null : reader.GetString(3),
                        ReorderLevel:  SageImportHelpers.ParseReorderLevel(reorderRaw),
                        PrimaryStorageLocation: SageImportHelpers.NormalizeLocationCode(primaryRaw)
                    ));
                }
            }

            _logger.LogInformation("SAGE liefert {Count} Artikel.", sageArticles.Count);

            if (dryRun)
            {
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["gelesen"]      = sageArticles.Count,
                    ["neu"]          = 0,
                    ["aktualisiert"] = 0,
                }, messageSuffix: "[DryRun]", ct: ct);
                return new SyncResult(0, 0, 0, $"DryRun: {sageArticles.Count} Datensätze aus SAGE gelesen.");
            }

            await using var wmsConn = new SqlConnection(wmsConnection);
            await wmsConn.OpenAsync(ct);

            // Code -> StorageLocationId einmal pro Lauf laden (case-insensitiv, getrimmt).
            var locationIdByCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            await using (var locCmd = new SqlCommand(
                "SELECT [Id], [Code] FROM [dbo].[StorageLocations] WHERE [Code] IS NOT NULL", wmsConn))
            {
                await using var locReader = await locCmd.ExecuteReaderAsync(ct);
                while (await locReader.ReadAsync(ct))
                {
                    var code = locReader.GetString(1).Trim();
                    if (code.Length > 0)
                        locationIdByCode[code] = locReader.GetInt32(0);
                }
            }

            int inserted = 0, updated = 0, missingPrimaryLocation = 0;

            foreach (var article in sageArticles)
            {
                const string upsertSql = """
                    IF EXISTS (SELECT 1 FROM [dbo].[Articles] WHERE [ArticleNumber] = @ArticleNumber)
                    BEGIN
                        UPDATE [dbo].[Articles] SET
                            [Description]                = @Description,
                            [Unit]                       = @Unit,
                            [ArticleGroup]               = @ArticleGroup,
                            [ReorderLevel]               = @ReorderLevel,
                            [SagePrimaryStorageLocation] = @SagePrimaryStorageLocation,
                            [PrimaryStorageLocationId]   = CASE WHEN @UpdatePrimaryId = 1 THEN @PrimaryStorageLocationId ELSE [PrimaryStorageLocationId] END,
                            [ModifiedAt]        = GETUTCDATE(),
                            [ModifiedBy]        = 'IDEALAKEWMSService',
                            [ModifiedByWindows] = SYSTEM_USER
                        WHERE [ArticleNumber] = @ArticleNumber
                          AND (
                              ISNULL([Description],'')   != ISNULL(@Description,'')   OR
                              ISNULL([Unit],'')          != ISNULL(@Unit,'')          OR
                              ISNULL([ArticleGroup],'')  != ISNULL(@ArticleGroup,'')  OR
                              ISNULL([ReorderLevel],-1)  != ISNULL(@ReorderLevel,-1)  OR
                              ISNULL([SagePrimaryStorageLocation],'') != ISNULL(@SagePrimaryStorageLocation,'') OR
                              (@UpdatePrimaryId = 1 AND ISNULL([PrimaryStorageLocationId],-1) != ISNULL(@PrimaryStorageLocationId,-1))
                          )
                        SELECT 0 AS IsInsert, @@ROWCOUNT AS Affected
                    END
                    ELSE
                    BEGIN
                        INSERT INTO [dbo].[Articles]
                            ([ArticleNumber],[Description],[Unit],[ArticleGroup],[ReorderLevel],
                             [PrimaryStorageLocationId],[SagePrimaryStorageLocation],
                             [CreatedAt],[CreatedBy],[CreatedByWindows])
                        VALUES (@ArticleNumber, @Description, @Unit, @ArticleGroup, @ReorderLevel,
                                @PrimaryStorageLocationId, @SagePrimaryStorageLocation,
                                GETUTCDATE(), 'IDEALAKEWMSService', SYSTEM_USER)
                        SELECT 1 AS IsInsert, 1 AS Affected
                    END
                    """;

                // Upsert-Regeln (Spec §Upsert-Logik):
                //  - Sage liefert Wert: SagePrimaryStorageLocation = code; PrimaryStorageLocationId = lookup[code]
                //    oder null (kein Match -> Warn-Count). Immer aktualisieren (@UpdatePrimaryId = 1).
                //  - Sage leer: SagePrimaryStorageLocation = null; PrimaryStorageLocationId NICHT anfassen
                //    (bewahrt manuelle App-Wahl) -> @UpdatePrimaryId = 0.
                string? sageCode = article.PrimaryStorageLocation;
                int? primaryId = null;
                int updatePrimaryId = 0;
                if (sageCode is not null)
                {
                    updatePrimaryId = 1;
                    if (locationIdByCode.TryGetValue(sageCode, out var foundId))
                    {
                        primaryId = foundId;
                    }
                    else
                    {
                        missingPrimaryLocation++;
                        _logger.LogWarning(
                            "Hauptlagerplatz '{Code}' fuer Artikel {ArticleNumber} nicht als WMS-Lagerplatz gefunden — FK bleibt leer.",
                            sageCode, article.ArticleNumber);
                        // Protokoll-Detailzeile (Cap 100 — der Serilog-Eintrag oben bleibt fuer ALLE).
                        // Der Count-Key hauptlagerplatz_fehlt im FinishSuccess bleibt die volle Zahl.
                        if (missingPrimaryLocation <= 100)
                            await run.LogWarningAsync(
                                $"Hauptlagerplatz '{sageCode}' fuer Artikel {article.ArticleNumber} nicht als WMS-Lagerplatz gefunden — FK bleibt leer",
                                reference: article.ArticleNumber, ct: ct);
                    }
                }

                await using var cmd = new SqlCommand(upsertSql, wmsConn);
                cmd.Parameters.AddWithValue("@ArticleNumber", article.ArticleNumber);
                cmd.Parameters.AddWithValue("@Description",  (object?)article.Description  ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Unit",         (object?)article.Unit         ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ArticleGroup", (object?)article.ArticleGroup ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ReorderLevel", (object?)article.ReorderLevel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SagePrimaryStorageLocation", (object?)sageCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PrimaryStorageLocationId", (object?)primaryId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UpdatePrimaryId", updatePrimaryId);

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    var isInsert = reader.GetInt32(0) == 1;
                    var affected = reader.GetInt32(1);
                    if (isInsert) inserted++;
                    else if (affected > 0) updated++;
                }
            }

            _logger.LogInformation("Artikel-Sync abgeschlossen: {Inserted} neu, {Updated} aktualisiert, {Missing} ohne WMS-Hauptlagerplatz.", inserted, updated, missingPrimaryLocation);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["gelesen"]              = sageArticles.Count,
                ["neu"]                  = inserted,
                ["aktualisiert"]         = updated,
                ["hauptlagerplatz_fehlt"] = missingPrimaryLocation,
            }, ct: ct);

            return new SyncResult(inserted, updated, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Artikel-Sync.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }
}

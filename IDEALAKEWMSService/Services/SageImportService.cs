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

    public SageImportService(
        IConfiguration configuration,
        ILogger<SageImportService> logger,
        ISyncLogger syncLogger,
        IBomCacheSyncService bomCacheSync,
        ICoatingDetectionService coatingDetection)
    {
        _configuration = configuration;
        _logger = logger;
        _syncLogger = syncLogger;
        _bomCacheSync = bomCacheSync;
        _coatingDetection = coatingDetection;
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

            foreach (var orderFromSage in sageOrders)
            {
                const string mergeSql = """
                    IF EXISTS (SELECT 1 FROM [dbo].[ProductionOrders] WHERE [OrderNumber] = @OrderNumber)
                    BEGIN
                        UPDATE [dbo].[ProductionOrders] SET
                            [Quantity]       = @Quantity,
                            [Customer]       = @Customer,
                            [ArticleNumber]  = @ArticleNumber,
                            [Description1]   = @Description1,
                            [Description2]   = @Description2,
                            [ProductionDate] = @ProductionDate,
                            [DeliveryDate]   = @DeliveryDate,
                            [ModifiedAt]     = GETUTCDATE(),
                            [ModifiedBy]     = 'IDEALAKEWMSService',
                            [ModifiedByWindows] = SYSTEM_USER
                        WHERE [OrderNumber] = @OrderNumber
                          AND (
                              [Quantity] != @Quantity OR
                              ISNULL([Customer],'') != ISNULL(@Customer,'') OR
                              ISNULL([ArticleNumber],'') != ISNULL(@ArticleNumber,'') OR
                              ISNULL([Description1],'') != ISNULL(@Description1,'') OR
                              ISNULL([Description2],'') != ISNULL(@Description2,'') OR
                              ISNULL(CAST([ProductionDate] AS date),'1900-01-01') != ISNULL(CAST(@ProductionDate AS date),'1900-01-01') OR
                              ISNULL(CAST([DeliveryDate] AS date),'1900-01-01') != ISNULL(CAST(@DeliveryDate AS date),'1900-01-01')
                          )
                        SELECT NULL AS InsertedId, @@ROWCOUNT AS Affected, 0 AS IsInsert
                    END
                    ELSE
                    BEGIN
                        -- Seit v1.11.0: PickingStatus/HasGlass/HasExternalPurchase wurden in
                        -- ProductionOrderPickingStatus ausgelagert. ProductionOrders enthaelt nur
                        -- noch Sage-Master + IsDone + Audit. Status-Zeilen werden nach dem Loop
                        -- per Folge-MERGE eager-created (Phase 1 Spec 9).
                        INSERT INTO [dbo].[ProductionOrders]
                            ([OrderNumber],[Quantity],[Customer],[ArticleNumber],[Description1],[Description2],
                             [ProductionDate],[DeliveryDate],[IsDone],
                             [CreatedAt],[CreatedBy],[CreatedByWindows])
                        VALUES
                            (@OrderNumber,@Quantity,@Customer,@ArticleNumber,@Description1,@Description2,
                             @ProductionDate,@DeliveryDate,0,
                             GETUTCDATE(),'IDEALAKEWMSService',SYSTEM_USER)
                        SELECT SCOPE_IDENTITY() AS InsertedId, 1 AS Affected, 1 AS IsInsert
                    END
                    """;

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
            // 3 idempotente MERGEs: PickingStatus (1:1), BdeStatus (1:1), AssemblyGroups (5/FA: VK/VL/VE/VT/VA).
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

                    MERGE [dbo].[ProductionOrderAssemblyGroups] AS s
                    USING (
                        SELECT po.Id AS ProductionOrderId, g.GroupKey
                        FROM [dbo].[ProductionOrders] po
                        CROSS JOIN (VALUES ('VK'),('VL'),('VE'),('VT'),('VA')) AS g(GroupKey)
                    ) AS p
                    ON s.ProductionOrderId = p.ProductionOrderId AND s.GroupKey = p.GroupKey
                    WHEN NOT MATCHED BY TARGET THEN
                        INSERT (ProductionOrderId, GroupKey, IsApplicable, IsCompleted,
                                CreatedAt, CreatedBy, CreatedByWindows)
                        VALUES (p.ProductionOrderId, p.GroupKey, 0, 0,
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
                SELECT DISTINCT
                    CAST(r.Ressourcenummer AS nvarchar(100))   AS ArticleNumber,
                    CAST(a.Bezeichnung1 AS nvarchar(500))      AS Description,
                    CAST(a.Lagermengeneinheit AS nvarchar(20)) AS Unit,
                    CAST(a.Artikelgruppe AS nvarchar(100))     AS ArticleGroup
                FROM [dbo].[KHKPpsRessourcenPositionen] r
                LEFT JOIN [dbo].[KHKArtikel] a ON a.Artikelnummer = r.Ressourcenummer
                WHERE r.Ressourcenummer IS NOT NULL AND r.Ressourcenummer != ''
                """;

            var sageArticles = new List<(string ArticleNumber, string? Description, string? Unit, string? ArticleGroup)>();

            await using (var sageConn = new SqlConnection(sageConnection))
            {
                await sageConn.OpenAsync(ct);
                await using var cmd = new SqlCommand(sageSql, sageConn);
                cmd.CommandTimeout = 120;
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    sageArticles.Add((
                        ArticleNumber: reader.GetString(0),
                        Description: reader.IsDBNull(1) ? null : reader.GetString(1),
                        Unit: reader.IsDBNull(2) ? null : reader.GetString(2),
                        ArticleGroup: reader.IsDBNull(3) ? null : reader.GetString(3)
                    ));
                }
            }

            _logger.LogInformation("SAGE liefert {Count} Artikel.", sageArticles.Count);

            if (dryRun)
            {
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["gelesen"] = sageArticles.Count,
                    ["neu"] = 0,
                }, messageSuffix: "[DryRun]", ct: ct);
                return new SyncResult(0, 0, 0, $"DryRun: {sageArticles.Count} Datensätze aus SAGE gelesen.");
            }

            int inserted = 0;

            await using var wmsConn = new SqlConnection(wmsConnection);
            await wmsConn.OpenAsync(ct);

            foreach (var article in sageArticles)
            {
                const string insertSql = """
                    IF NOT EXISTS (SELECT 1 FROM [dbo].[Articles] WHERE [ArticleNumber] = @ArticleNumber)
                    BEGIN
                        INSERT INTO [dbo].[Articles] ([ArticleNumber],[Description],[Unit],[ArticleGroup],[CreatedAt],[CreatedBy],[CreatedByWindows])
                        VALUES (@ArticleNumber, @Description, @Unit, @ArticleGroup, GETUTCDATE(), 'IDEALAKEWMSService', SYSTEM_USER)
                        SELECT 1
                    END
                    ELSE
                    BEGIN
                        UPDATE [dbo].[Articles]
                        SET [ArticleGroup] = @ArticleGroup
                        WHERE [ArticleNumber] = @ArticleNumber AND ([ArticleGroup] IS NULL OR [ArticleGroup] != @ArticleGroup)
                        SELECT 0
                    END
                    """;

                await using var cmd = new SqlCommand(insertSql, wmsConn);
                cmd.Parameters.AddWithValue("@ArticleNumber", article.ArticleNumber);
                cmd.Parameters.AddWithValue("@Description", (object?)article.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Unit", (object?)article.Unit ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ArticleGroup", (object?)article.ArticleGroup ?? DBNull.Value);

                var result = await cmd.ExecuteScalarAsync(ct);
                if (result is int i && i == 1) inserted++;
            }

            _logger.LogInformation("Artikel-Sync abgeschlossen: {Inserted} neu eingefügt.", inserted);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = inserted,
            }, ct: ct);

            return new SyncResult(inserted, 0, 0);
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

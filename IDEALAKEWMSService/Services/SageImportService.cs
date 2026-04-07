using Microsoft.Data.SqlClient;
using System.Text;

namespace IDEALAKEWMSService.Services;

public class SageImportService : ISageImportService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SageImportService> _logger;
    private readonly IBomCacheSyncService _bomCacheSync;
    private readonly ICoatingDetectionService _coatingDetection;

    public SageImportService(
        IConfiguration configuration,
        ILogger<SageImportService> logger,
        IBomCacheSyncService bomCacheSync,
        ICoatingDetectionService coatingDetection)
    {
        _configuration = configuration;
        _logger = logger;
        _bomCacheSync = bomCacheSync;
        _coatingDetection = coatingDetection;
    }

    public async Task<SyncResult> SyncProductionOrdersAsync(bool dryRun, CancellationToken ct = default)
    {
        var sageConnection = _configuration.GetConnectionString("SageConnection")
            ?? throw new InvalidOperationException("SageConnection nicht konfiguriert.");
        var wmsConnection = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");

        if (dryRun)
            _logger.LogInformation("[DryRun] Produktionsaufträge-Sync — keine Änderungen werden geschrieben.");

        try
        {
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
                return new SyncResult(0, 0, 0, $"DryRun: {sageOrders.Count} Datensätze aus SAGE gelesen.");

            int inserted = 0, updated = 0;
            var newArticleNumbers = new List<string>();
            var newOrderIds = new List<int>();

            await using var wmsConn = new SqlConnection(wmsConnection);
            await wmsConn.OpenAsync(ct);

            foreach (var orderFromSage in sageOrders)
            {
                const string mergeSql = """
                    IF EXISTS (SELECT 1 FROM [ProductionOrders] WHERE [OrderNumber] = @OrderNumber)
                    BEGIN
                        UPDATE [ProductionOrders] SET
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
                        INSERT INTO [ProductionOrders]
                            ([OrderNumber],[Quantity],[Customer],[ArticleNumber],[Description1],[Description2],
                             [ProductionDate],[DeliveryDate],[IsDone],[PickingStatus],[HasGlass],[HasExternalPurchase],
                             [CreatedAt],[CreatedBy],[CreatedByWindows])
                        VALUES
                            (@OrderNumber,@Quantity,@Customer,@ArticleNumber,@Description1,@Description2,
                             @ProductionDate,@DeliveryDate,0,'',0,0,
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

            // Hook: BOM-Cache + Coating Detection fuer neue Auftraege
            if (_configuration.GetValue<bool>("Sync:BomCacheEnabled") && newArticleNumbers.Count > 0)
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
            return new SyncResult(inserted, updated, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Produktionsaufträge-Sync.");
            return new SyncResult(0, 0, 1, ex.Message);
        }
    }

    public async Task<SyncResult> SyncArticlesAsync(bool dryRun, CancellationToken ct = default)
    {
        var sageConnection = _configuration.GetConnectionString("SageConnection")
            ?? throw new InvalidOperationException("SageConnection nicht konfiguriert.");
        var wmsConnection = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");

        if (dryRun)
            _logger.LogInformation("[DryRun] Artikel-Sync — keine Änderungen werden geschrieben.");

        try
        {
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
                return new SyncResult(0, 0, 0, $"DryRun: {sageArticles.Count} Datensätze aus SAGE gelesen.");

            int inserted = 0;

            await using var wmsConn = new SqlConnection(wmsConnection);
            await wmsConn.OpenAsync(ct);

            foreach (var article in sageArticles)
            {
                const string insertSql = """
                    IF NOT EXISTS (SELECT 1 FROM [Articles] WHERE [ArticleNumber] = @ArticleNumber)
                    BEGIN
                        INSERT INTO [Articles] ([ArticleNumber],[Description],[Unit],[ArticleGroup],[CreatedAt],[CreatedBy],[CreatedByWindows])
                        VALUES (@ArticleNumber, @Description, @Unit, @ArticleGroup, GETUTCDATE(), 'IDEALAKEWMSService', SYSTEM_USER)
                        SELECT 1
                    END
                    ELSE
                    BEGIN
                        UPDATE [Articles]
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
            return new SyncResult(inserted, 0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Artikel-Sync.");
            return new SyncResult(0, 0, 1, ex.Message);
        }
    }
}

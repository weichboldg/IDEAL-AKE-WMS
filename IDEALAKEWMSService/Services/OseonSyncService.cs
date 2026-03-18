using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

public class OseonSyncService : IOseonSyncService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OseonSyncService> _logger;

    public OseonSyncService(IConfiguration configuration, ILogger<OseonSyncService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SyncResult> SyncOseonProductionOrdersAsync(bool dryRun, CancellationToken ct = default)
    {
        var oseonConnection = _configuration.GetConnectionString("OseonConnection")
            ?? throw new InvalidOperationException("OseonConnection nicht konfiguriert.");
        var wmsConnection = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");

        if (dryRun)
            _logger.LogInformation("[DryRun] OSEON-Tracking-Sync — keine Änderungen werden geschrieben.");

        try
        {
            // Daten aus OSEON lesen
            const string oseonSql = """
                SELECT
                    pa.ID,
                    CAST(pa.KundenAuftragsNr AS nvarchar(100)) AS CustomerOrderNumber,
                    CAST(pa.AuftragsNr AS nvarchar(100)) AS OseonOrderNumber,
                    pa.Status AS PaStatus,
                    CAST(aga.ApPositionsNr AS nvarchar(50)) AS PositionNumber,
                    CAST(aga.TaetigkeitsName AS nvarchar(200)) AS ActivityName,
                    CAST(aga.Beschreibung AS nvarchar(500)) AS ActivityDescription,
                    aga.Status AS AgaStatus,
                    CAST(aga.IsFirstAga AS bit) AS IsFirstAga,
                    CAST(aga.IsLastAga AS bit) AS IsLastAga,
                    CAST(a.Name AS nvarchar(100)) AS ArticleNumber,
                    CAST(a.Bezeichnung AS nvarchar(500)) AS Description1,
                    CAST(a.NameExt AS nvarchar(500)) AS Description2,
                    CAST(k.KundenNr AS nvarchar(200)) AS WorkplaceName,
                    pa.MengeSoll,
                    pa.MengeIst,
                    pa.EndTerminSoll
                FROM ProduktionsAuftrag pa
                LEFT JOIN ProduktionsAga aga ON pa.AuftragsNr = aga.AuftragsNr
                LEFT JOIN Artikel a ON pa.ArtikelID = a.ID
                LEFT JOIN Kunde k ON pa.KundenID = k.ID
                WHERE pa.KundenAuftragsNr IS NOT NULL
                  AND (pa.Status NOT IN (90, 95)
                       OR pa.EndTerminSoll >= DATEADD(month, -3, GETDATE()))
                """;

            var oseonRows = new List<OseonRawRow>();

            await using (var oseonConn = new SqlConnection(oseonConnection))
            {
                await oseonConn.OpenAsync(ct);
                await using var cmd = new SqlCommand(oseonSql, oseonConn);
                cmd.CommandTimeout = 120;
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    oseonRows.Add(new OseonRawRow
                    {
                        OseonId = reader.IsDBNull(0) ? 0 : reader.GetInt64(0),
                        CustomerOrderNumber = reader.IsDBNull(1) ? null : reader.GetString(1)?.Trim(),
                        OseonOrderNumber = reader.IsDBNull(2) ? null : reader.GetString(2)?.Trim(),
                        PaStatus = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                        PositionNumber = reader.IsDBNull(4) ? null : reader.GetString(4)?.Trim(),
                        ActivityName = reader.IsDBNull(5) ? null : reader.GetString(5)?.Trim(),
                        ActivityDescription = reader.IsDBNull(6) ? null : reader.GetString(6)?.Trim(),
                        AgaStatus = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                        IsFirstAga = !reader.IsDBNull(8) && reader.GetBoolean(8),
                        IsLastAga = !reader.IsDBNull(9) && reader.GetBoolean(9),
                        ArticleNumber = reader.IsDBNull(10) ? null : reader.GetString(10)?.Trim(),
                        Description1 = reader.IsDBNull(11) ? null : reader.GetString(11)?.Trim(),
                        Description2 = reader.IsDBNull(12) ? null : reader.GetString(12)?.Trim(),
                        WorkplaceName = reader.IsDBNull(13) ? null : reader.GetString(13)?.Trim(),
                        QuantityTarget = reader.IsDBNull(14) ? 0 : Convert.ToDecimal(reader.GetValue(14)),
                        QuantityActual = reader.IsDBNull(15) ? 0 : Convert.ToDecimal(reader.GetValue(15)),
                        DueDate = reader.IsDBNull(16) ? null : reader.GetDateTime(16)
                    });
                }
            }

            _logger.LogInformation("OSEON liefert {Count} Datensätze.", oseonRows.Count);

            // Nach OseonOrderNumber gruppieren → Orders + AGAs
            var orderGroups = oseonRows
                .Where(r => !string.IsNullOrEmpty(r.OseonOrderNumber))
                .GroupBy(r => r.OseonOrderNumber!)
                .ToList();

            _logger.LogInformation("OSEON: {Count} eindeutige Aufträge.", orderGroups.Count);

            if (dryRun)
                return new SyncResult(0, 0, 0, $"DryRun: {oseonRows.Count} Zeilen, {orderGroups.Count} Aufträge aus OSEON gelesen.");

            int inserted = 0, updated = 0, errors = 0;
            var errorDetails = new System.Text.StringBuilder();

            await using var wmsConn = new SqlConnection(wmsConnection);
            await wmsConn.OpenAsync(ct);

            // Cache für Werkbänke
            var workplaceCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            await LoadWorkplaceCacheAsync(wmsConn, workplaceCache, ct);

            foreach (var group in orderGroups)
            {
                try
                {
                    var firstRow = group.First();
                    var oseonOrderNumber = group.Key;

                    // Werkbank auto-anlegen
                    int? workplaceId = null;
                    if (!string.IsNullOrWhiteSpace(firstRow.WorkplaceName))
                    {
                        workplaceId = await EnsureWorkplaceExistsAsync(wmsConn, firstRow.WorkplaceName, workplaceCache, ct);
                    }

                    // Order upsert
                    var (orderId, isInsert) = await UpsertOrderAsync(wmsConn, firstRow, workplaceId, ct);

                    if (isInsert) inserted++;
                    else updated++;

                    // AGAs upsert
                    foreach (var row in group.Where(r => !string.IsNullOrEmpty(r.PositionNumber)))
                    {
                        await UpsertOperationAsync(wmsConn, orderId, row, ct);
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    errorDetails.AppendLine($"Auftrag {group.Key}: {ex.Message}");
                    _logger.LogWarning(ex, "Fehler bei OSEON-Auftrag {OrderNumber}.", group.Key);
                }
            }

            _logger.LogInformation("OSEON-Tracking-Sync abgeschlossen: {Inserted} neu, {Updated} aktualisiert, {Errors} Fehler.",
                inserted, updated, errors);

            return new SyncResult(inserted, updated, errors, errors > 0 ? errorDetails.ToString() : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim OSEON-Tracking-Sync.");
            return new SyncResult(0, 0, 1, ex.Message);
        }
    }

    private static async Task LoadWorkplaceCacheAsync(SqlConnection conn, Dictionary<string, int> cache, CancellationToken ct)
    {
        const string sql = "SELECT [Id], [Name] FROM [ProductionWorkplaces]";
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var name = reader.GetString(1).Trim();
            if (!cache.ContainsKey(name))
                cache[name] = reader.GetInt32(0);
        }
    }

    private static async Task<int> EnsureWorkplaceExistsAsync(SqlConnection conn, string name, Dictionary<string, int> cache, CancellationToken ct)
    {
        if (cache.TryGetValue(name, out var cachedId))
            return cachedId;

        const string insertSql = """
            INSERT INTO [ProductionWorkplaces] ([Name], [CreatedAt], [CreatedBy], [CreatedByWindows])
            OUTPUT INSERTED.Id
            VALUES (@Name, GETUTCDATE(), 'IDEALAKEWMSService', SYSTEM_USER)
            """;

        await using var cmd = new SqlCommand(insertSql, conn);
        cmd.Parameters.AddWithValue("@Name", name);
        var id = (int)(await cmd.ExecuteScalarAsync(ct))!;
        cache[name] = id;
        return id;
    }

    private static async Task<(int orderId, bool isInsert)> UpsertOrderAsync(SqlConnection conn, OseonRawRow row, int? workplaceId, CancellationToken ct)
    {
        const string mergeSql = """
            IF EXISTS (SELECT 1 FROM [OseonProductionOrders] WHERE [OseonId] = @OseonId)
            BEGIN
                UPDATE [OseonProductionOrders] SET
                    [OseonOrderNumber]      = @OseonOrderNumber,
                    [CustomerOrderNumber]   = @CustomerOrderNumber,
                    [OseonStatus]           = @OseonStatus,
                    [ArticleNumber]         = @ArticleNumber,
                    [Description1]          = @Description1,
                    [Description2]          = @Description2,
                    [WorkplaceName]         = @WorkplaceName,
                    [ProductionWorkplaceId] = @ProductionWorkplaceId,
                    [QuantityTarget]        = @QuantityTarget,
                    [QuantityActual]        = @QuantityActual,
                    [DueDate]               = @DueDate,
                    [ModifiedAt]            = GETUTCDATE(),
                    [ModifiedBy]            = 'IDEALAKEWMSService',
                    [ModifiedByWindows]     = SYSTEM_USER
                WHERE [OseonId] = @OseonId
                SELECT [Id], 0 AS IsInsert FROM [OseonProductionOrders] WHERE [OseonId] = @OseonId
            END
            ELSE
            BEGIN
                INSERT INTO [OseonProductionOrders]
                    ([OseonId],[OseonOrderNumber],[CustomerOrderNumber],[OseonStatus],
                     [ArticleNumber],[Description1],[Description2],[WorkplaceName],
                     [ProductionWorkplaceId],[QuantityTarget],[QuantityActual],[DueDate],
                     [CreatedAt],[CreatedBy],[CreatedByWindows])
                VALUES
                    (@OseonId,@OseonOrderNumber,@CustomerOrderNumber,@OseonStatus,
                     @ArticleNumber,@Description1,@Description2,@WorkplaceName,
                     @ProductionWorkplaceId,@QuantityTarget,@QuantityActual,@DueDate,
                     GETUTCDATE(),'IDEALAKEWMSService',SYSTEM_USER)
                SELECT SCOPE_IDENTITY() AS [Id], 1 AS IsInsert
            END
            """;

        await using var cmd = new SqlCommand(mergeSql, conn);
        cmd.Parameters.AddWithValue("@OseonId", row.OseonId);
        cmd.Parameters.AddWithValue("@OseonOrderNumber", row.OseonOrderNumber!);
        cmd.Parameters.AddWithValue("@CustomerOrderNumber", (object?)row.CustomerOrderNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OseonStatus", row.PaStatus);
        cmd.Parameters.AddWithValue("@ArticleNumber", (object?)row.ArticleNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Description1", (object?)row.Description1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Description2", (object?)row.Description2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@WorkplaceName", (object?)row.WorkplaceName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ProductionWorkplaceId", (object?)workplaceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@QuantityTarget", row.QuantityTarget);
        cmd.Parameters.AddWithValue("@QuantityActual", row.QuantityActual);
        cmd.Parameters.AddWithValue("@DueDate", (object?)row.DueDate ?? DBNull.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            var orderId = Convert.ToInt32(reader.GetValue(0));
            var isInsert = reader.GetInt32(1) == 1;
            return (orderId, isInsert);
        }

        throw new InvalidOperationException($"OSEON-Auftrag {row.OseonOrderNumber} konnte nicht geschrieben werden.");
    }

    private static async Task UpsertOperationAsync(SqlConnection conn, int orderId, OseonRawRow row, CancellationToken ct)
    {
        const string mergeSql = """
            IF EXISTS (SELECT 1 FROM [OseonWorkOperations] WHERE [OseonProductionOrderId] = @OrderId AND [PositionNumber] = @PositionNumber)
            BEGIN
                UPDATE [OseonWorkOperations] SET
                    [Name]             = @Name,
                    [Description]      = @Description,
                    [OseonStatus]      = @OseonStatus,
                    [IsFirstOperation] = @IsFirstOperation,
                    [IsLastOperation]  = @IsLastOperation,
                    [ModifiedAt]       = GETUTCDATE(),
                    [ModifiedBy]       = 'IDEALAKEWMSService',
                    [ModifiedByWindows] = SYSTEM_USER
                WHERE [OseonProductionOrderId] = @OrderId AND [PositionNumber] = @PositionNumber
            END
            ELSE
            BEGIN
                INSERT INTO [OseonWorkOperations]
                    ([OseonProductionOrderId],[PositionNumber],[Name],[Description],
                     [OseonStatus],[IsFirstOperation],[IsLastOperation],
                     [CreatedAt],[CreatedBy],[CreatedByWindows])
                VALUES
                    (@OrderId,@PositionNumber,@Name,@Description,
                     @OseonStatus,@IsFirstOperation,@IsLastOperation,
                     GETUTCDATE(),'IDEALAKEWMSService',SYSTEM_USER)
            END
            """;

        await using var cmd = new SqlCommand(mergeSql, conn);
        cmd.Parameters.AddWithValue("@OrderId", orderId);
        cmd.Parameters.AddWithValue("@PositionNumber", row.PositionNumber!);
        cmd.Parameters.AddWithValue("@Name", row.ActivityName ?? string.Empty);
        cmd.Parameters.AddWithValue("@Description", (object?)row.ActivityDescription ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OseonStatus", row.AgaStatus);
        cmd.Parameters.AddWithValue("@IsFirstOperation", row.IsFirstAga);
        cmd.Parameters.AddWithValue("@IsLastOperation", row.IsLastAga);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private class OseonRawRow
    {
        public long OseonId { get; set; }
        public string? CustomerOrderNumber { get; set; }
        public string? OseonOrderNumber { get; set; }
        public int PaStatus { get; set; }
        public string? PositionNumber { get; set; }
        public string? ActivityName { get; set; }
        public string? ActivityDescription { get; set; }
        public int AgaStatus { get; set; }
        public bool IsFirstAga { get; set; }
        public bool IsLastAga { get; set; }
        public string? ArticleNumber { get; set; }
        public string? Description1 { get; set; }
        public string? Description2 { get; set; }
        public string? WorkplaceName { get; set; }
        public decimal QuantityTarget { get; set; }
        public decimal QuantityActual { get; set; }
        public DateTime? DueDate { get; set; }
    }
}

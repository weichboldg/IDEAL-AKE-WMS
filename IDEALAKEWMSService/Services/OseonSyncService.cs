using System.Data;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

public class OseonSyncService : IOseonSyncService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OseonSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public OseonSyncService(
        IConfiguration configuration,
        ILogger<OseonSyncService> logger,
        ISyncLogger syncLogger)
    {
        _configuration = configuration;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<SyncResult> SyncOseonProductionOrdersAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.OseonTracking, ct);

        if (dryRun)
            _logger.LogInformation("[DryRun] OSEON-Tracking-Sync — keine Änderungen werden geschrieben.");

        try
        {
            var oseonConnection = _configuration.GetConnectionString("OseonConnection")
                ?? throw new InvalidOperationException("OseonConnection nicht konfiguriert.");
            var wmsConnection = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");
            await using var wmsConn = new SqlConnection(wmsConnection);
            await wmsConn.OpenAsync(ct);

            // Prüfen ob Delta-Sync-Spalten existieren (Migration noch nicht gelaufen?)
            bool hasTimestampColumns;
            await using (var colCmd = new SqlCommand(
                "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('OseonProductionOrders') AND name = 'LastChangedInOseon'",
                wmsConn) { CommandTimeout = 10 })
            {
                hasTimestampColumns = (int)(await colCmd.ExecuteScalarAsync(ct))! > 0;
            }

            if (!hasTimestampColumns)
                _logger.LogWarning("Spalten [LastChangedInOseon]/[LastStatusReportInOseon] fehlen — Full-Sync ohne Delta. Bitte SQL/31_AddOseonTimestamps.sql ausführen.");

            // Delta-Sync: MAX(LastChangedInOseon) aus WMS lesen
            DateTime? lastSyncDate = null;
            if (hasTimestampColumns)
            {
                await using var maxCmd = new SqlCommand(
                    "SELECT MAX([LastChangedInOseon]) FROM [dbo].[OseonProductionOrders]", wmsConn) { CommandTimeout = 30 };
                var result = await maxCmd.ExecuteScalarAsync(ct);
                if (result != null && result != DBNull.Value)
                    lastSyncDate = (DateTime)result;
            }

            // Sicherheitspuffer: 5 Minuten abziehen
            var deltaFilter = lastSyncDate?.AddMinutes(-5);

            if (deltaFilter.HasValue)
                _logger.LogInformation("Delta-Sync ab {DeltaDate:yyyy-MM-dd HH:mm:ss} (letztes OSEON-Update: {LastSync:yyyy-MM-dd HH:mm:ss}).",
                    deltaFilter.Value, lastSyncDate!.Value);
            else
                _logger.LogInformation("Erster Sync-Lauf — alle OSEON-Daten werden geladen.");

            // Daten aus OSEON lesen (mit optionalem Delta-Filter)
            var oseonSql = $"""
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
                    pa.EndTerminSoll,
                    pa.DateOfLastChange,
                    aga.LetzteStatusMeldung
                FROM ProduktionsAuftrag pa
                LEFT JOIN ProduktionsAga aga ON pa.AuftragsNr = aga.AuftragsNr
                LEFT JOIN Artikel a ON pa.ArtikelID = a.ID
                LEFT JOIN Kunde k ON pa.KundenID = k.ID
                WHERE pa.KundenAuftragsNr IS NOT NULL
                  AND (pa.Status NOT IN (90, 95)
                       OR pa.EndTerminSoll >= DATEADD(month, -3, GETDATE()))
                {(deltaFilter.HasValue ? "  AND pa.DateOfLastChange > @DeltaDate" : "")}
                """;

            var oseonRows = new List<OseonRawRow>();

            await using (var oseonConn = new SqlConnection(oseonConnection))
            {
                await oseonConn.OpenAsync(ct);
                await using var cmd = new SqlCommand(oseonSql, oseonConn);
                cmd.CommandTimeout = 120;
                if (deltaFilter.HasValue)
                    cmd.Parameters.AddWithValue("@DeltaDate", deltaFilter.Value);

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
                        DueDate = reader.IsDBNull(16) ? null : reader.GetDateTime(16),
                        DateOfLastChange = reader.IsDBNull(17) ? null : Convert.ToDateTime(reader.GetValue(17)),
                        LastStatusReport = reader.IsDBNull(18) ? null : Convert.ToDateTime(reader.GetValue(18))
                    });
                }
            }

            var rowsWithTimestamp = oseonRows.Count(r => r.DateOfLastChange.HasValue);
            _logger.LogInformation("OSEON liefert {Count} Datensätze{DeltaInfo}. Davon {WithTs} mit DateOfLastChange.",
                oseonRows.Count,
                deltaFilter.HasValue ? " (Delta-Sync)" : " (Full-Sync)",
                rowsWithTimestamp);

            // Je Auftragsnummer genau eine OSEON-Zeile (hoechste ID), AGs nur aus dieser Zeile (Hotfix v1.30.2).
            var selection = OseonOrderSelection.Select(oseonRows);

            _logger.LogInformation("OSEON: {Count} eindeutige Aufträge.", selection.Orders.Count);

            // Melden statt still behandeln: mehrere OSEON-Datensaetze zu einer Auftragsnummer.
            foreach (var m in selection.Multiples)
            {
                await run.LogWarningAsync(
                    $"Auftrag {m.OrderNumber}: OSEON liefert {m.OseonIds.Count} Datensätze (IDs {string.Join(", ", m.OseonIds)}), übernommen {m.SelectedOseonId}",
                    reference: m.OrderNumber, ct: ct);
            }

            if (dryRun)
            {
                await run.FinishSuccessAsync(Counts(0, 0, 0, selection.Multiples.Count, 0),
                    messageSuffix: "[DryRun]", ct: ct);
                return new SyncResult(0, 0, 0, $"DryRun: {oseonRows.Count} Zeilen, {selection.Orders.Count} Aufträge aus OSEON gelesen.");
            }

            if (selection.Orders.Count == 0)
            {
                await run.FinishSuccessAsync(Counts(0, 0, 0, 0, 0), ct: ct);
                return new SyncResult(0, 0, 0, "Keine geänderten Datensätze in OSEON.");
            }

            // 1. Werkbänke auto-anlegen (Bulk-Insert fehlender)
            await EnsureWorkplacesExistBulkAsync(wmsConn, selection.Orders, ct);

            // 2. Bulk MERGE: Orders
            var orderMerge = await BulkMergeOrdersAsync(wmsConn, selection.Orders, hasTimestampColumns, ct);
            foreach (var (number, oldId, newId) in orderMerge.Reissued)
            {
                await run.LogWarningAsync(
                    $"Auftrag {number}: OSEON-ID {oldId} → {newId} (neu ausgeschrieben)",
                    reference: number, ct: ct);
            }
            foreach (var (number, deliveredId, storedId) in orderMerge.OlderIdsSkipped)
            {
                await run.LogWarningAsync(
                    $"Auftrag {number}: OSEON liefert ältere ID {deliveredId}, gespeichert ist {storedId} — nicht übernommen (höchste ID gewinnt)",
                    reference: number, ct: ct);
            }

            // 3. Bulk MERGE: Work Operations (AGAs)
            var opsCollationDuplicates = await BulkMergeOperationsAsync(wmsConn, selection.Operations, hasTimestampColumns, ct);

            var collationDuplicates = orderMerge.CollationDuplicates + opsCollationDuplicates;
            if (collationDuplicates > 0)
            {
                await run.LogWarningAsync(
                    $"{collationDuplicates} OSEON-Zeile(n) nach DB-Kollation doppelt und verworfen " +
                    $"(Aufträge: {orderMerge.CollationDuplicates}, Arbeitsgänge: {opsCollationDuplicates}). Datenbefund, kein Normalbetrieb.",
                    ct: ct);
            }

            var inserted = orderMerge.Inserted;
            var updated = orderMerge.Updated;

            _logger.LogInformation("OSEON-Tracking-Sync abgeschlossen: {Inserted} neu, {Updated} aktualisiert, {Reissued} neu ausgeschrieben.",
                inserted, updated, orderMerge.Reissued.Count);

            await run.FinishSuccessAsync(Counts(inserted, updated, orderMerge.Reissued.Count,
                selection.Multiples.Count, collationDuplicates, orderMerge.OlderIdsSkipped.Count), ct: ct);

            return new SyncResult(inserted, updated, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim OSEON-Tracking-Sync.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }

    private static Dictionary<string, int> Counts(int inserted, int updated, int reissued, int multiples,
        int collationDuplicates, int olderIdsSkipped = 0) => new()
    {
        ["neu"] = inserted,
        ["aktualisiert"] = updated,
        ["neu_ausgeschrieben"] = reissued,
        ["mehrfach_in_oseon"] = multiples,
        ["doppelt_nach_kollation"] = collationDuplicates,
        ["aeltere_id_uebersprungen"] = olderIdsSkipped,
    };

    /// <summary>
    /// Bulk-Insert aller fehlenden Werkbänke in einem Durchgang.
    /// </summary>
    private async Task EnsureWorkplacesExistBulkAsync(SqlConnection conn,
        IReadOnlyList<OseonRawRow> orders, CancellationToken ct)
    {
        var distinctWorkplaces = orders
            .Select(o => o.WorkplaceName)
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctWorkplaces.Count == 0) return;

        // Temp-Table mit Werkbanknamen erstellen, dann nur fehlende INSERT
        const string sql = """
            CREATE TABLE #TmpWorkplaces ([Name] nvarchar(200) NOT NULL);

            INSERT INTO #TmpWorkplaces ([Name]) VALUES {0};

            INSERT INTO [dbo].[ProductionWorkplaces] ([Name], [CreatedAt], [CreatedBy], [CreatedByWindows])
            SELECT DISTINCT t.[Name], GETUTCDATE(), 'IDEALAKEWMSService', SYSTEM_USER
            FROM #TmpWorkplaces t
            WHERE NOT EXISTS (SELECT 1 FROM [dbo].[ProductionWorkplaces] pw WHERE pw.[Name] = t.[Name]);

            DROP TABLE #TmpWorkplaces;
            """;

        // Parametrisierte VALUES-Liste
        var valuesClauses = new List<string>();
        var cmd = new SqlCommand { Connection = conn, CommandTimeout = 60 };

        for (int i = 0; i < distinctWorkplaces.Count; i++)
        {
            valuesClauses.Add($"(@wp{i})");
            cmd.Parameters.AddWithValue($"@wp{i}", distinctWorkplaces[i]!);
        }

        cmd.CommandText = string.Format(sql, string.Join(", ", valuesClauses));
        await cmd.ExecuteNonQueryAsync(ct);
        await cmd.DisposeAsync();

        _logger.LogDebug("Werkbänke synchronisiert: {Count} eindeutige Namen geprüft.", distinctWorkplaces.Count);
    }

    /// <summary>
    /// Bulk MERGE für OseonProductionOrders über Temp-Table.
    /// Statt ~5000 einzelne Roundtrips → 1 BulkCopy + 1 MERGE Statement.
    /// </summary>
    private async Task<OrderMergeResult> BulkMergeOrdersAsync(SqlConnection conn,
        IReadOnlyList<OseonRawRow> orders, bool hasTimestampColumns, CancellationToken ct)
    {
        // DataTable für BulkCopy aufbauen
        var dt = new DataTable();
        dt.Columns.Add("OseonId", typeof(long));
        dt.Columns.Add("OseonOrderNumber", typeof(string));
        dt.Columns.Add("CustomerOrderNumber", typeof(string));
        dt.Columns.Add("OseonStatus", typeof(int));
        dt.Columns.Add("ArticleNumber", typeof(string));
        dt.Columns.Add("Description1", typeof(string));
        dt.Columns.Add("Description2", typeof(string));
        dt.Columns.Add("WorkplaceName", typeof(string));
        dt.Columns.Add("QuantityTarget", typeof(decimal));
        dt.Columns.Add("QuantityActual", typeof(decimal));
        dt.Columns.Add("DueDate", typeof(DateTime));
        if (hasTimestampColumns)
            dt.Columns.Add("LastChangedInOseon", typeof(DateTime));

        foreach (var row in orders)
        {
            var dr = dt.NewRow();
            dr["OseonId"] = row.OseonId;
            dr["OseonOrderNumber"] = row.OseonOrderNumber ?? (object)DBNull.Value;
            dr["CustomerOrderNumber"] = row.CustomerOrderNumber ?? (object)DBNull.Value;
            dr["OseonStatus"] = row.PaStatus;
            dr["ArticleNumber"] = row.ArticleNumber ?? (object)DBNull.Value;
            dr["Description1"] = row.Description1 ?? (object)DBNull.Value;
            dr["Description2"] = row.Description2 ?? (object)DBNull.Value;
            dr["WorkplaceName"] = row.WorkplaceName ?? (object)DBNull.Value;
            dr["QuantityTarget"] = row.QuantityTarget;
            dr["QuantityActual"] = row.QuantityActual;
            dr["DueDate"] = row.DueDate ?? (object)DBNull.Value;
            if (hasTimestampColumns)
                dr["LastChangedInOseon"] = row.DateOfLastChange ?? (object)DBNull.Value;
            dt.Rows.Add(dr);
        }

        // Temp-Table erstellen
        var createTempSql = $"""
            CREATE TABLE #TmpOseonOrders (
                [OseonId] bigint NOT NULL,
                [OseonOrderNumber] nvarchar(100) NULL,
                [CustomerOrderNumber] nvarchar(100) NULL,
                [OseonStatus] int NOT NULL,
                [ArticleNumber] nvarchar(100) NULL,
                [Description1] nvarchar(500) NULL,
                [Description2] nvarchar(500) NULL,
                [WorkplaceName] nvarchar(200) NULL,
                [QuantityTarget] decimal(18,3) NOT NULL,
                [QuantityActual] decimal(18,3) NOT NULL,
                [DueDate] datetime2 NULL{(hasTimestampColumns ? ",\n                [LastChangedInOseon] datetime2 NULL" : "")}
            )
            """;
        await using (var cmd = new SqlCommand(createTempSql, conn) { CommandTimeout = 30 })
            await cmd.ExecuteNonQueryAsync(ct);

        // BulkCopy in Temp-Table
        using (var bulkCopy = new SqlBulkCopy(conn) { DestinationTableName = "#TmpOseonOrders", BulkCopyTimeout = 120 })
        {
            bulkCopy.ColumnMappings.Add("OseonId", "OseonId");
            bulkCopy.ColumnMappings.Add("OseonOrderNumber", "OseonOrderNumber");
            bulkCopy.ColumnMappings.Add("CustomerOrderNumber", "CustomerOrderNumber");
            bulkCopy.ColumnMappings.Add("OseonStatus", "OseonStatus");
            bulkCopy.ColumnMappings.Add("ArticleNumber", "ArticleNumber");
            bulkCopy.ColumnMappings.Add("Description1", "Description1");
            bulkCopy.ColumnMappings.Add("Description2", "Description2");
            bulkCopy.ColumnMappings.Add("WorkplaceName", "WorkplaceName");
            bulkCopy.ColumnMappings.Add("QuantityTarget", "QuantityTarget");
            bulkCopy.ColumnMappings.Add("QuantityActual", "QuantityActual");
            bulkCopy.ColumnMappings.Add("DueDate", "DueDate");
            if (hasTimestampColumns)
                bulkCopy.ColumnMappings.Add("LastChangedInOseon", "LastChangedInOseon");
            await bulkCopy.WriteToServerAsync(dt, ct);
        }

        _logger.LogDebug("BulkCopy: {Count} Orders in Temp-Table geladen.", dt.Rows.Count);

        // MERGE: Upsert in einem Statement
        var tsUpdate = hasTimestampColumns ? ",\n                    target.[LastChangedInOseon]     = source.[LastChangedInOseon]" : "";
        var tsInsertCols = hasTimestampColumns ? ",\n                        [LastChangedInOseon]" : "";
        var tsInsertVals = hasTimestampColumns ? ",\n                        source.[LastChangedInOseon]" : "";

        // SQL-Waechter: Die C#-Auswahl bildet die DB-Kollation nur nach. Was nach Kollation doppelt ist,
        // verwirft ROW_NUMBER (hoechste OseonId bleibt) - sonst bricht der MERGE (8672) bzw. der Index (2601).
        const string orderPartition = "PARTITION BY t.[OseonOrderNumber] ORDER BY t.[OseonId] DESC";
        var collationDuplicates = await CountRowNumberDuplicatesAsync(conn, "#TmpOseonOrders", orderPartition, ct);

        var dedupedSource = $"""
            SELECT t.*
            FROM (
                SELECT t.*, ROW_NUMBER() OVER ({orderPartition}) AS rn
                FROM #TmpOseonOrders t
            ) t
            WHERE t.rn = 1
            """;

        // "Hoechste ID gewinnt" gilt auch gegen den gespeicherten Stand, nicht nur im Delta: aendert OSEON nach
        // einer Neuausschreibung nur noch die ALTE Zeile (z.B. Storno), liefert das Delta nur die alte ID.
        // Diese Zeilen werden nicht uebernommen (WHEN MATCHED AND ...), sondern gemeldet.
        var olderIds = new List<(string Number, long DeliveredId, long StoredId)>();
        await using (var cmd = new SqlCommand($"""
            SELECT s.[OseonOrderNumber], s.[OseonId], target.[OseonId]
            FROM ({dedupedSource}) s
            INNER JOIN [dbo].[OseonProductionOrders] target ON target.[OseonOrderNumber] = s.[OseonOrderNumber]
            WHERE s.[OseonId] < target.[OseonId]
            """, conn) { CommandTimeout = 60 })
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
                olderIds.Add((reader.IsDBNull(0) ? "" : reader.GetString(0), reader.GetInt64(1), reader.GetInt64(2)));
        }

        // MERGE ueber die Auftragsnummer (eindeutiger Index), nicht ueber die OSEON-ID: OSEON schreibt
        // Auftraege neu aus (gleiche Nummer, neue ID). Die OseonId wird im UPDATE umgeschluesselt,
        // der WMS-Datensatz (Id) und damit seine Arbeitsgaenge bleiben erhalten.
        var mergeSql = $"""
            MERGE [dbo].[OseonProductionOrders] AS target
            USING (
                SELECT t.*, pw.[Id] AS WorkplaceId
                FROM ({dedupedSource}) t
                LEFT JOIN [dbo].[ProductionWorkplaces] pw ON pw.[Name] = t.[WorkplaceName]
            ) AS source
            ON target.[OseonOrderNumber] = source.[OseonOrderNumber]
            WHEN MATCHED AND source.[OseonId] >= target.[OseonId] THEN
                UPDATE SET
                    target.[OseonId]               = source.[OseonId],
                    target.[OseonOrderNumber]      = source.[OseonOrderNumber],
                    target.[CustomerOrderNumber]   = source.[CustomerOrderNumber],
                    target.[OseonStatus]           = source.[OseonStatus],
                    target.[ArticleNumber]         = source.[ArticleNumber],
                    target.[Description1]          = source.[Description1],
                    target.[Description2]          = source.[Description2],
                    target.[WorkplaceName]         = source.[WorkplaceName],
                    target.[ProductionWorkplaceId] = source.[WorkplaceId],
                    target.[QuantityTarget]        = source.[QuantityTarget],
                    target.[QuantityActual]        = source.[QuantityActual],
                    target.[DueDate]               = source.[DueDate]{tsUpdate},
                    target.[ModifiedAt]            = GETUTCDATE(),
                    target.[ModifiedBy]            = 'IDEALAKEWMSService',
                    target.[ModifiedByWindows]     = SYSTEM_USER
            WHEN NOT MATCHED THEN
                INSERT ([OseonId],[OseonOrderNumber],[CustomerOrderNumber],[OseonStatus],
                        [ArticleNumber],[Description1],[Description2],[WorkplaceName],
                        [ProductionWorkplaceId],[QuantityTarget],[QuantityActual],[DueDate]{tsInsertCols},
                        [CreatedAt],[CreatedBy],[CreatedByWindows])
                VALUES (source.[OseonId],source.[OseonOrderNumber],source.[CustomerOrderNumber],source.[OseonStatus],
                        source.[ArticleNumber],source.[Description1],source.[Description2],source.[WorkplaceName],
                        source.[WorkplaceId],source.[QuantityTarget],source.[QuantityActual],source.[DueDate]{tsInsertVals},
                        GETUTCDATE(),'IDEALAKEWMSService',SYSTEM_USER)
            OUTPUT $action, deleted.[OseonId], inserted.[OseonId], inserted.[OseonOrderNumber];
            """;

        int inserted = 0, updated = 0;
        var reissued = new List<(string Number, long OldId, long NewId)>();
        await using (var cmd = new SqlCommand(mergeSql, conn) { CommandTimeout = 120 })
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var action = reader.GetString(0);
                if (action == "INSERT") { inserted++; continue; }

                updated++;
                var oldId = reader.GetInt64(1);
                var newId = reader.GetInt64(2);
                if (oldId != newId)
                    reissued.Add((reader.IsDBNull(3) ? "" : reader.GetString(3), oldId, newId));
            }
        }

        // Temp-Table aufräumen
        await using (var cmd = new SqlCommand("DROP TABLE #TmpOseonOrders", conn))
            await cmd.ExecuteNonQueryAsync(ct);

        return new OrderMergeResult(inserted, updated, reissued, olderIds, collationDuplicates);
    }

    private sealed record OrderMergeResult(int Inserted, int Updated,
        List<(string Number, long OldId, long NewId)> Reissued,
        List<(string Number, long DeliveredId, long StoredId)> OlderIdsSkipped,
        int CollationDuplicates);

    /// <summary>
    /// Anzahl der Zeilen, die der SQL-Waechter (<c>ROW_NUMBER() ... WHERE rn = 1</c>) verwirft:
    /// in C# verschieden, nach DB-Kollation gleich. Erwartet 0.
    /// </summary>
    private static async Task<int> CountRowNumberDuplicatesAsync(SqlConnection conn, string tempTable,
        string partition, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(
            $"SELECT COUNT(*) FROM (SELECT ROW_NUMBER() OVER ({partition}) AS rn FROM {tempTable} t) x WHERE x.rn > 1",
            conn) { CommandTimeout = 60 };
        return (int)(await cmd.ExecuteScalarAsync(ct))!;
    }

    /// <summary>
    /// Bulk MERGE für OseonWorkOperations über Temp-Table.
    /// <paramref name="operationRows"/> kommt aus <see cref="OseonOrderSelection"/>: nur AGs der gewählten
    /// Auftragszeile, je (Nummer, Position) einmal. Nicht mehr gelieferte AGs bleiben stehen (der Sync löscht nie).
    /// Rückgabe: Anzahl der vom SQL-Wächter verworfenen Zeilen (nach DB-Kollation doppelt).
    /// </summary>
    private async Task<int> BulkMergeOperationsAsync(SqlConnection conn,
        IReadOnlyList<OseonRawRow> operationRows, bool hasTimestampColumns, CancellationToken ct)
    {
        if (operationRows.Count == 0) return 0;

        // DataTable für BulkCopy
        var dt = new DataTable();
        dt.Columns.Add("OseonOrderNumber", typeof(string));
        dt.Columns.Add("PositionNumber", typeof(string));
        dt.Columns.Add("Name", typeof(string));
        dt.Columns.Add("Description", typeof(string));
        dt.Columns.Add("OseonStatus", typeof(int));
        dt.Columns.Add("IsFirstOperation", typeof(bool));
        dt.Columns.Add("IsLastOperation", typeof(bool));
        if (hasTimestampColumns)
            dt.Columns.Add("LastStatusReportInOseon", typeof(DateTime));

        foreach (var row in operationRows)
        {
            var dr = dt.NewRow();
            dr["OseonOrderNumber"] = row.OseonOrderNumber!;
            dr["PositionNumber"] = row.PositionNumber!;
            dr["Name"] = row.ActivityName ?? string.Empty;
            dr["Description"] = row.ActivityDescription ?? (object)DBNull.Value;
            dr["OseonStatus"] = row.AgaStatus;
            dr["IsFirstOperation"] = row.IsFirstAga;
            dr["IsLastOperation"] = row.IsLastAga;
            if (hasTimestampColumns)
                dr["LastStatusReportInOseon"] = row.LastStatusReport ?? (object)DBNull.Value;
            dt.Rows.Add(dr);
        }

        // Temp-Table erstellen
        var createTempSql = $"""
            CREATE TABLE #TmpOseonOps (
                [OseonOrderNumber] nvarchar(100) NOT NULL,
                [PositionNumber] nvarchar(50) NOT NULL,
                [Name] nvarchar(200) NOT NULL,
                [Description] nvarchar(500) NULL,
                [OseonStatus] int NOT NULL,
                [IsFirstOperation] bit NOT NULL,
                [IsLastOperation] bit NOT NULL{(hasTimestampColumns ? ",\n                [LastStatusReportInOseon] datetime2 NULL" : "")}
            )
            """;
        await using (var cmd = new SqlCommand(createTempSql, conn) { CommandTimeout = 30 })
            await cmd.ExecuteNonQueryAsync(ct);

        // BulkCopy
        using (var bulkCopy = new SqlBulkCopy(conn) { DestinationTableName = "#TmpOseonOps", BulkCopyTimeout = 120 })
        {
            bulkCopy.ColumnMappings.Add("OseonOrderNumber", "OseonOrderNumber");
            bulkCopy.ColumnMappings.Add("PositionNumber", "PositionNumber");
            bulkCopy.ColumnMappings.Add("Name", "Name");
            bulkCopy.ColumnMappings.Add("Description", "Description");
            bulkCopy.ColumnMappings.Add("OseonStatus", "OseonStatus");
            bulkCopy.ColumnMappings.Add("IsFirstOperation", "IsFirstOperation");
            bulkCopy.ColumnMappings.Add("IsLastOperation", "IsLastOperation");
            if (hasTimestampColumns)
                bulkCopy.ColumnMappings.Add("LastStatusReportInOseon", "LastStatusReportInOseon");
            await bulkCopy.WriteToServerAsync(dt, ct);
        }

        _logger.LogDebug("BulkCopy: {Count} Operations in Temp-Table geladen.", dt.Rows.Count);

        // MERGE über OseonOrderNumber → OseonProductionOrders.Id
        var tsSelect = hasTimestampColumns ? ",\n                       t.[LastStatusReportInOseon]" : "";
        var tsUpdate = hasTimestampColumns ? ",\n                    target.[LastStatusReportInOseon]  = source.[LastStatusReportInOseon]" : "";
        var tsInsertCols = hasTimestampColumns ? ",\n                        [LastStatusReportInOseon]" : "";
        var tsInsertVals = hasTimestampColumns ? ",\n                        source.[LastStatusReportInOseon]" : "";

        // SQL-Waechter wie beim Auftrags-MERGE: je (Nummer, Position) nach DB-Kollation genau eine Zeile.
        var opsPartition = "PARTITION BY t.[OseonOrderNumber], t.[PositionNumber] ORDER BY "
            + (hasTimestampColumns ? "t.[LastStatusReportInOseon] DESC, " : "") + "t.[OseonStatus] DESC";
        var collationDuplicates = await CountRowNumberDuplicatesAsync(conn, "#TmpOseonOps", opsPartition, ct);

        var mergeSql = $"""
            MERGE [dbo].[OseonWorkOperations] AS target
            USING (
                SELECT opo.[Id] AS OrderId, t.[PositionNumber], t.[Name], t.[Description],
                       t.[OseonStatus], t.[IsFirstOperation], t.[IsLastOperation]{tsSelect}
                FROM (
                    SELECT t.*, ROW_NUMBER() OVER ({opsPartition}) AS rn
                    FROM #TmpOseonOps t
                ) t
                INNER JOIN [dbo].[OseonProductionOrders] opo ON opo.[OseonOrderNumber] = t.[OseonOrderNumber]
                WHERE t.rn = 1
            ) AS source
            ON target.[OseonProductionOrderId] = source.[OrderId] AND target.[PositionNumber] = source.[PositionNumber]
            WHEN MATCHED THEN
                UPDATE SET
                    target.[Name]                    = source.[Name],
                    target.[Description]             = source.[Description],
                    target.[OseonStatus]             = source.[OseonStatus],
                    target.[IsFirstOperation]        = source.[IsFirstOperation],
                    target.[IsLastOperation]         = source.[IsLastOperation]{tsUpdate},
                    target.[ModifiedAt]              = GETUTCDATE(),
                    target.[ModifiedBy]              = 'IDEALAKEWMSService',
                    target.[ModifiedByWindows]       = SYSTEM_USER
            WHEN NOT MATCHED THEN
                INSERT ([OseonProductionOrderId],[PositionNumber],[Name],[Description],
                        [OseonStatus],[IsFirstOperation],[IsLastOperation]{tsInsertCols},
                        [CreatedAt],[CreatedBy],[CreatedByWindows])
                VALUES (source.[OrderId],source.[PositionNumber],source.[Name],source.[Description],
                        source.[OseonStatus],source.[IsFirstOperation],source.[IsLastOperation]{tsInsertVals},
                        GETUTCDATE(),'IDEALAKEWMSService',SYSTEM_USER);
            """;

        await using (var cmd = new SqlCommand(mergeSql, conn) { CommandTimeout = 120 })
            await cmd.ExecuteNonQueryAsync(ct);

        // Temp-Table aufräumen
        await using (var cmd = new SqlCommand("DROP TABLE #TmpOseonOps", conn))
            await cmd.ExecuteNonQueryAsync(ct);

        return collationDuplicates;
    }

    public async Task<SyncResult> SyncWorkplacesToProductionOrdersAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.OseonWorkplaces, ct);

        try
        {
            var wmsConnection = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");
            await using var conn = new SqlConnection(wmsConnection);
            await conn.OpenAsync(ct);

            if (dryRun)
            {
                const string countSql = """
                    SELECT COUNT(*)
                    FROM [dbo].[ProductionOrders] po
                    INNER JOIN (
                        SELECT CustomerOrderNumber, MIN(ProductionWorkplaceId) AS ProductionWorkplaceId
                        FROM [dbo].[OseonProductionOrders]
                        WHERE CustomerOrderNumber IS NOT NULL
                          AND ProductionWorkplaceId IS NOT NULL
                        GROUP BY CustomerOrderNumber
                    ) oseon ON po.OrderNumber = oseon.CustomerOrderNumber
                    WHERE po.ProductionWorkplaceId IS NULL
                    """;

                await using var cmd = new SqlCommand(countSql, conn);
                var count = (int)(await cmd.ExecuteScalarAsync(ct))!;
                _logger.LogInformation("[DryRun] Werkbank-Sync: {Count} Produktionsaufträge würden aktualisiert.", count);
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["aktualisiert"] = count,
                }, messageSuffix: "[DryRun]", ct: ct);
                return new SyncResult(0, count, 0, $"DryRun: {count} Aufträge würden Werkbank erhalten.");
            }

            const string updateSql = """
                UPDATE po SET
                    po.ProductionWorkplaceId = oseon.ProductionWorkplaceId,
                    po.ModifiedAt = GETUTCDATE(),
                    po.ModifiedBy = 'IDEALAKEWMSService',
                    po.ModifiedByWindows = SYSTEM_USER
                FROM [dbo].[ProductionOrders] po
                INNER JOIN (
                    SELECT CustomerOrderNumber, MIN(ProductionWorkplaceId) AS ProductionWorkplaceId
                    FROM [dbo].[OseonProductionOrders]
                    WHERE CustomerOrderNumber IS NOT NULL
                      AND ProductionWorkplaceId IS NOT NULL
                    GROUP BY CustomerOrderNumber
                ) oseon ON po.OrderNumber = oseon.CustomerOrderNumber
                WHERE po.ProductionWorkplaceId IS NULL
                """;

            await using var updateCmd = new SqlCommand(updateSql, conn);
            var updated = await updateCmd.ExecuteNonQueryAsync(ct);

            _logger.LogInformation("Werkbank-Sync: {Updated} Produktionsaufträge aktualisiert.", updated);
            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["aktualisiert"] = updated,
            }, ct: ct);
            return new SyncResult(0, updated, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Werkbank-Sync.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }

    public async Task<SyncResult> SyncArticleCategoriesToWmsAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.OseonArticleCategories, ct);

        if (dryRun)
            _logger.LogInformation("[DryRun] OSEON-Artikelkategorie-Sync — keine Aenderungen werden geschrieben.");

        int inserted = 0, updated = 0, errors = 0;
        string? errorDetails = null;

        try
        {
            var oseonConnection = _configuration.GetConnectionString("OseonConnection")
                ?? throw new InvalidOperationException("OseonConnection nicht konfiguriert.");
            var wmsConnection = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");
            // Step 1: Read categories from OSEON
            var oseonCategories = new List<(string Name, string? Bemerkung, int? Typ)>();
            await using (var oseonConn = new SqlConnection(oseonConnection))
            {
                await oseonConn.OpenAsync(ct);
                await using var cmd = new SqlCommand("SELECT Name, Bemerkung, Typ FROM ArtikelKategorie", oseonConn)
                    { CommandTimeout = 60 };
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var name = reader.GetString(0).Trim();
                    var bemerkung = reader.IsDBNull(1) ? null : reader.GetString(1).Trim();
                    var typ = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                    if (!string.IsNullOrEmpty(name))
                        oseonCategories.Add((name, bemerkung, typ));
                }
            }

            _logger.LogInformation("OSEON-Artikelkategorien: {Count} Kategorien gelesen.", oseonCategories.Count);

            if (dryRun)
            {
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["neu"] = oseonCategories.Count,
                    ["aktualisiert"] = 0,
                }, messageSuffix: "[DryRun]", ct: ct);
                return new SyncResult(oseonCategories.Count, 0, 0);
            }

            await using var wmsConn = new SqlConnection(wmsConnection);
            await wmsConn.OpenAsync(ct);

            // Step 2: Upsert categories
            foreach (var (name, bemerkung, typ) in oseonCategories)
            {
                try
                {
                    await using var upsertCmd = new SqlCommand(@"
                        IF EXISTS (SELECT 1 FROM [dbo].[ArticleCategories] WHERE [Name] = @Name)
                        BEGIN
                            UPDATE [dbo].[ArticleCategories]
                            SET [Description] = @Description, [OseonTyp] = @OseonTyp, [Source] = 'OSEON',
                                [ModifiedAt] = GETDATE(), [ModifiedBy] = 'OseonSync', [ModifiedByWindows] = 'SYSTEM'
                            WHERE [Name] = @Name;
                            SELECT 0; -- updated
                        END
                        ELSE
                        BEGIN
                            INSERT INTO [dbo].[ArticleCategories] ([Name], [Description], [OseonTyp], [Source], [CreatedAt], [CreatedBy], [CreatedByWindows])
                            VALUES (@Name, @Description, @OseonTyp, 'OSEON', GETDATE(), 'OseonSync', 'SYSTEM');
                            SELECT 1; -- inserted
                        END", wmsConn) { CommandTimeout = 30 };

                    upsertCmd.Parameters.AddWithValue("@Name", name);
                    upsertCmd.Parameters.AddWithValue("@Description", (object?)bemerkung ?? DBNull.Value);
                    upsertCmd.Parameters.AddWithValue("@OseonTyp", (object?)typ ?? DBNull.Value);

                    var result = (int)(await upsertCmd.ExecuteScalarAsync(ct))!;
                    if (result == 1) inserted++;
                    else updated++;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning(ex, "Fehler beim Upsert der Kategorie '{Name}'.", name);
                }
            }

            // Step 3: Read article-category assignments from OSEON
            var articleCategories = new List<(string ArticleNumber, string CategoryName)>();
            await using (var oseonConn2 = new SqlConnection(oseonConnection))
            {
                await oseonConn2.OpenAsync(ct);
                await using var cmd2 = new SqlCommand(
                    "SELECT CAST(Name AS nvarchar(100)) AS Artikelnummer, CAST(Kategorie AS nvarchar(200)) AS Artikelkategorie FROM Artikel WHERE Kategorie IS NOT NULL AND Kategorie != ''",
                    oseonConn2) { CommandTimeout = 120 };
                await using var reader2 = await cmd2.ExecuteReaderAsync(ct);
                while (await reader2.ReadAsync(ct))
                {
                    var artNr = reader2.GetString(0).Trim();
                    var catName = reader2.GetString(1).Trim();
                    if (!string.IsNullOrEmpty(artNr) && !string.IsNullOrEmpty(catName))
                        articleCategories.Add((artNr, catName));
                }
            }

            _logger.LogInformation("OSEON-Artikel-Zuordnungen: {Count} Zuordnungen gelesen.", articleCategories.Count);

            // Step 4: Bulk-update article category assignments
            var assignUpdated = 0;
            await using var assignCmd = new SqlCommand(@"
                UPDATE a SET a.[ArticleCategoryId] = c.[Id]
                FROM [dbo].[Articles] a
                INNER JOIN [dbo].[ArticleCategories] c ON c.[Name] = @CategoryName COLLATE Latin1_General_CI_AS
                WHERE a.[ArticleNumber] = @ArticleNumber COLLATE Latin1_General_CI_AS
                  AND (a.[ArticleCategoryId] IS NULL OR a.[ArticleCategoryId] != c.[Id])",
                wmsConn) { CommandTimeout = 30 };

            assignCmd.Parameters.Add("@ArticleNumber", System.Data.SqlDbType.NVarChar, 100);
            assignCmd.Parameters.Add("@CategoryName", System.Data.SqlDbType.NVarChar, 200);

            foreach (var (artNr, catName) in articleCategories)
            {
                try
                {
                    assignCmd.Parameters["@ArticleNumber"].Value = artNr;
                    assignCmd.Parameters["@CategoryName"].Value = catName;
                    var rows = await assignCmd.ExecuteNonQueryAsync(ct);
                    if (rows > 0) assignUpdated++;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning(ex, "Fehler beim Zuordnen der Kategorie '{Category}' zu Artikel '{Article}'.", catName, artNr);
                }
            }

            _logger.LogInformation("Artikel-Kategorie-Zuordnungen: {Updated} aktualisiert.", assignUpdated);
            updated += assignUpdated;

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
            }, ct: ct);
            return new SyncResult(inserted, updated, errors, errorDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim OSEON-Artikelkategorie-Sync.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }
}

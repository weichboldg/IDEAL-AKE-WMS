using System.Data;
using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

public class EnaioDmsSyncService : IEnaioDmsSyncService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnaioDmsSyncService> _logger;

    public EnaioDmsSyncService(IConfiguration configuration, ILogger<EnaioDmsSyncService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SyncResult> SyncDocumentsAsync(bool dryRun, CancellationToken ct = default)
    {
        var enaioDmsConnection = _configuration.GetConnectionString("EnaioDmsConnection")
            ?? throw new InvalidOperationException("EnaioDmsConnection nicht konfiguriert.");
        var wmsConnection = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");

        if (dryRun)
            _logger.LogInformation("[DryRun] enaio DMS-Sync — keine Aenderungen werden geschrieben.");

        try
        {
            // 1. Delta-Datum aus WMS lesen
            DateTime? lastSyncDate = null;
            await using (var wmsConn = new SqlConnection(wmsConnection))
            {
                await wmsConn.OpenAsync(ct);
                await using var cmd = new SqlCommand(
                    "SELECT MAX([LastSyncedAt]) FROM [EnaioDmsDocuments]", wmsConn) { CommandTimeout = 30 };
                var result = await cmd.ExecuteScalarAsync(ct);
                if (result is DateTime lastSync)
                    lastSyncDate = lastSync.AddMinutes(-5); // 5 Min Puffer
            }

            // 2. Daten aus enaio lesen
            var docs = new List<EnaioDmsRawRow>();
            await using (var enaioDmsConn = new SqlConnection(enaioDmsConnection))
            {
                await enaioDmsConn.OpenAsync(ct);

                var sql = @"
                    SELECT id, angelegt, feld1 AS Typ,
                           feld44 AS WaNummer,
                           LEFT(feld43, 7) AS WaNummerZeichnung
                    FROM sysadm.object1
                    WHERE feld1 IN ('Werkstattauftrag', 'Zeichnung')";

                if (lastSyncDate.HasValue)
                {
                    sql += " AND angelegt > @DeltaDate";
                }
                else
                {
                    // Erster Sync: nur letztes Jahr
                    sql += " AND angelegt > DATEADD(DAY, -365, GETDATE())";
                }

                await using var cmd = new SqlCommand(sql, enaioDmsConn) { CommandTimeout = 120 };
                if (lastSyncDate.HasValue)
                    cmd.Parameters.AddWithValue("@DeltaDate", lastSyncDate.Value);

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var typ = reader.GetString(2);
                    var waNr = typ == "Werkstattauftrag"
                        ? (reader.IsDBNull(3) ? null : reader.GetString(3))
                        : (reader.IsDBNull(4) ? null : reader.GetString(4));

                    docs.Add(new EnaioDmsRawRow
                    {
                        EnaioDmsObjectId = Convert.ToInt64(reader.GetValue(0)),
                        CreatedInEnaio = reader.GetDateTime(1),
                        DocumentType = typ,
                        OrderNumber = waNr?.Trim()
                    });
                }
            }

            _logger.LogInformation("enaio DMS: {Count} Dokumente aus enaio gelesen{Delta}.",
                docs.Count, lastSyncDate.HasValue ? $" (Delta seit {lastSyncDate:yyyy-MM-dd HH:mm})" : " (Full-Sync)");

            if (docs.Count == 0 || dryRun)
                return new SyncResult(0, 0, 0);

            // 3. Bulk-Insert in Temp-Table + MERGE
            await using var wmsConn2 = new SqlConnection(wmsConnection);
            await wmsConn2.OpenAsync(ct);

            // Temp-Table erstellen
            await using (var createCmd = new SqlCommand(@"
                CREATE TABLE #TmpEnaioDocs (
                    [EnaioDmsObjectId] BIGINT NOT NULL,
                    [DocumentType]     NVARCHAR(100) NOT NULL,
                    [OrderNumber]      NVARCHAR(100) NULL,
                    [CreatedInEnaio]   DATETIME2 NOT NULL
                )", wmsConn2) { CommandTimeout = 30 })
            {
                await createCmd.ExecuteNonQueryAsync(ct);
            }

            // DataTable fuer BulkCopy
            var dt = new DataTable();
            dt.Columns.Add("EnaioDmsObjectId", typeof(long));
            dt.Columns.Add("DocumentType", typeof(string));
            dt.Columns.Add("OrderNumber", typeof(string));
            dt.Columns.Add("CreatedInEnaio", typeof(DateTime));

            foreach (var doc in docs)
            {
                dt.Rows.Add(
                    doc.EnaioDmsObjectId,
                    doc.DocumentType,
                    (object?)doc.OrderNumber ?? DBNull.Value,
                    doc.CreatedInEnaio);
            }

            using (var bulkCopy = new SqlBulkCopy(wmsConn2) { DestinationTableName = "#TmpEnaioDocs", BulkCopyTimeout = 120 })
            {
                bulkCopy.ColumnMappings.Add("EnaioDmsObjectId", "EnaioDmsObjectId");
                bulkCopy.ColumnMappings.Add("DocumentType", "DocumentType");
                bulkCopy.ColumnMappings.Add("OrderNumber", "OrderNumber");
                bulkCopy.ColumnMappings.Add("CreatedInEnaio", "CreatedInEnaio");
                await bulkCopy.WriteToServerAsync(dt, ct);
            }

            // MERGE: Insert oder Update
            var now = DateTime.UtcNow;
            await using var mergeCmd = new SqlCommand($@"
                MERGE [EnaioDmsDocuments] AS target
                USING #TmpEnaioDocs AS source
                ON target.[EnaioDmsObjectId] = source.[EnaioDmsObjectId]
                WHEN MATCHED THEN
                    UPDATE SET
                        target.[DocumentType]   = source.[DocumentType],
                        target.[OrderNumber]    = source.[OrderNumber],
                        target.[CreatedInEnaio] = source.[CreatedInEnaio],
                        target.[LastSyncedAt]   = @Now,
                        target.[ModifiedAt]     = @Now,
                        target.[ModifiedBy]     = 'EnaioDmsSync'
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT ([EnaioDmsObjectId], [DocumentType], [OrderNumber], [CreatedInEnaio],
                            [LastSyncedAt], [CreatedAt], [CreatedBy])
                    VALUES (source.[EnaioDmsObjectId], source.[DocumentType], source.[OrderNumber],
                            source.[CreatedInEnaio], @Now, @Now, 'EnaioDmsSync')
                OUTPUT $action;

                DROP TABLE #TmpEnaioDocs;
            ", wmsConn2) { CommandTimeout = 120 };
            mergeCmd.Parameters.AddWithValue("@Now", now);

            int inserted = 0, updated = 0;
            await using var mergeReader = await mergeCmd.ExecuteReaderAsync(ct);
            while (await mergeReader.ReadAsync(ct))
            {
                var action = mergeReader.GetString(0);
                if (action == "INSERT") inserted++;
                else if (action == "UPDATE") updated++;
            }

            _logger.LogInformation("enaio DMS-Sync: {Inserted} neu, {Updated} aktualisiert.", inserted, updated);
            return new SyncResult(inserted, updated, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim enaio DMS-Sync.");
            return new SyncResult(0, 0, 1, ex.Message);
        }
    }

    private class EnaioDmsRawRow
    {
        public long EnaioDmsObjectId { get; set; }
        public DateTime CreatedInEnaio { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string? OrderNumber { get; set; }
    }
}

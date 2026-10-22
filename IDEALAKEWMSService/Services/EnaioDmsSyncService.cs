using System.Data;
using IDEALAKEWMSService.Common;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

public class EnaioDmsSyncService : IEnaioDmsSyncService
{
    /// <summary>Default/Fallback der Quelle = Katalog-Default von Sync:EnaioDmsViewName (v1.67.0; bis dahin fest im SQL).</summary>
    public const string DefaultViewName = "[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]";

    private readonly IConfiguration _configuration;
    private readonly ILogger<EnaioDmsSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public EnaioDmsSyncService(
        IConfiguration configuration,
        ILogger<EnaioDmsSyncService> logger,
        ISyncLogger syncLogger)
    {
        _configuration = configuration;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<SyncResult> SyncDocumentsAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.EnaioDms, ct);

        if (dryRun)
            _logger.LogInformation("[DryRun] enaio DMS-Sync — keine Aenderungen werden geschrieben.");

        // Quelle aus Sync:EnaioDmsViewName (leer/fehlt/DB-Fehler -> Default). Pruefung VOR dem try: dessen catch beendet den Lauf
        // selbst - hier drin wuerde ein ungueltiger Name den Lauf zweimal als fehlgeschlagen abschliessen.
        string quotedSource;
        try
        {
            quotedSource = ResolveSource(await ServiceSettings.GetValueSafeAsync(_configuration, "Sync:EnaioDmsViewName", ct));
        }
        catch (FaHierarchyViewNameException ex)
        {
            // Kein SQL. Fehler + Fehlermail (throw -> RunResilientAsync).
            await run.LogErrorAsync($"Ungültiger View-Name: {ex.Message}", ct: ct);
            await run.FinishFailedAsync($"Ungültiger View-Name: {ex.Message}", ct: ct);
            throw new InvalidOperationException(
                $"enaio DMS-Sync abgebrochen — ungueltiger View-Name (kein SQL ausgefuehrt): {ex.Message}", ex);
        }

        try
        {
            var enaioDmsConnection = _configuration.GetConnectionString("EnaioDmsConnection")
                ?? throw new InvalidOperationException("EnaioDmsConnection nicht konfiguriert.");
            var wmsConnection = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");
            // Alle relevanten Fertigungsauftrags-Dokumente aus enaio lesen (Full-Sync).
            // Quelle ist die View vw_IDEAL-AKE_Fertigungsauftraege (Default, konfigurierbar), die die Indexdaten
            // bereits aufbereitet liefert: Typ, WaNummer (mit Suffix -01/-02) und
            // WaNummerTrimmed (die FA-Match-Nummer ohne Suffix). Kein Typ-Filter mehr
            // noetig — die View liefert die relevanten Zeilen.
            // Delta via 'angelegt' funktioniert nicht, weil enaio-Dokumente einmalig
            // erstellt werden (angelegt = 2013) und spaeter aktualisiert werden, ohne
            // dass sich angelegt aendert. Das MERGE-Statement verhindert Duplikate und
            // aktualisiert Aenderungen.
            var docs = new List<EnaioDmsRawRow>();
            await using (var enaioDmsConn = new SqlConnection(enaioDmsConnection))
            {
                await enaioDmsConn.OpenAsync(ct);

                var sql = $@"
                    SELECT id, angelegt, Typ, WaNummer, WaNummerTrimmed
                    FROM {quotedSource}";

                await using var cmd = new SqlCommand(sql, enaioDmsConn) { CommandTimeout = 120 };

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    docs.Add(new EnaioDmsRawRow
                    {
                        EnaioDmsObjectId = Convert.ToInt64(reader.GetValue(0)),
                        CreatedInEnaio = reader.GetDateTime(1),
                        DocumentType = reader.GetString(2),
                        OrderNumber = reader.IsDBNull(4) ? null : reader.GetString(4).Trim()
                    });
                }
            }

            _logger.LogInformation("enaio DMS: {Count} Dokumente aus enaio gelesen.", docs.Count);

            if (docs.Count == 0 || dryRun)
            {
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["neu"] = 0,
                    ["aktualisiert"] = 0,
                }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
                return new SyncResult(0, 0, 0);
            }

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
            var now = DateTime.Now;
            await using var mergeCmd = new SqlCommand($@"
                MERGE [dbo].[EnaioDmsDocuments] AS target
                USING #TmpEnaioDocs AS source
                ON target.[EnaioDmsObjectId] = source.[EnaioDmsObjectId]
                WHEN MATCHED THEN
                    UPDATE SET
                        target.[DocumentType]      = source.[DocumentType],
                        target.[OrderNumber]       = source.[OrderNumber],
                        target.[CreatedInEnaio]    = source.[CreatedInEnaio],
                        target.[LastSyncedAt]      = @Now,
                        target.[ModifiedAt]        = @Now,
                        target.[ModifiedBy]        = 'EnaioDmsSync',
                        target.[ModifiedByWindows] = 'EnaioDmsSync'
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT ([EnaioDmsObjectId], [DocumentType], [OrderNumber], [CreatedInEnaio],
                            [LastSyncedAt], [CreatedAt], [CreatedBy], [CreatedByWindows])
                    VALUES (source.[EnaioDmsObjectId], source.[DocumentType], source.[OrderNumber],
                            source.[CreatedInEnaio], @Now, @Now, 'EnaioDmsSync', 'EnaioDmsSync')
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

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
            }, ct: ct);

            return new SyncResult(inserted, updated, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim enaio DMS-Sync.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }

    /// <summary>Konfigurierter Name (leer = <see cref="DefaultViewName"/>), geprueft und geklammert, bis zu drei Teile.</summary>
    internal static string ResolveSource(string? configured) =>
        FaHierarchySql.ValidateAndQuote(string.IsNullOrWhiteSpace(configured) ? DefaultViewName : configured, maxSegments: 3);

    private class EnaioDmsRawRow
    {
        public long EnaioDmsObjectId { get; set; }
        public DateTime CreatedInEnaio { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string? OrderNumber { get; set; }
    }
}

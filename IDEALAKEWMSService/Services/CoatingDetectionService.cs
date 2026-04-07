using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDEALAKEWMSService.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

public class CoatingDetectionService : ICoatingDetectionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CoatingDetectionService> _logger;

    public CoatingDetectionService(
        IConfiguration configuration,
        ILogger<CoatingDetectionService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SyncResult> DetectAndUpdateCoatingFlagsAsync(
        bool dryRun,
        List<int>? specificOrderIds,
        CancellationToken ct)
    {
        int updatedTrue = 0, updatedFalse = 0, errors = 0;
        string? errorDetails = null;

        try
        {
            var connStr = ConnectionStrings.Wms(_configuration);
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync(ct);

            // 1) Read setting
            string? lackName;
            await using (var cmd = new SqlCommand(
                @"SELECT [Value] FROM [AppSettings] WHERE [Key] = 'LackierteilKategorieName'", conn))
            {
                var r = await cmd.ExecuteScalarAsync(ct);
                lackName = r as string;
            }
            if (string.IsNullOrWhiteSpace(lackName))
            {
                _logger.LogWarning("CoatingDetection: LackierteilKategorieName nicht gesetzt - Feature inaktiv");
                return new SyncResult(0, 0, 0);
            }

            // 2) Read orders to evaluate
            var orders = new List<(int Id, string ArticleNumber)>();
            if (specificOrderIds != null && specificOrderIds.Count > 0)
            {
                var paramNames = new List<string>();
                await using var cmd = conn.CreateCommand();
                cmd.CommandTimeout = 60;
                for (int i = 0; i < specificOrderIds.Count; i++)
                {
                    var p = $"@id{i}";
                    paramNames.Add(p);
                    cmd.Parameters.AddWithValue(p, specificOrderIds[i]);
                }
                cmd.CommandText = $@"
                    SELECT [Id], [ArticleNumber]
                    FROM [ProductionOrders]
                    WHERE [Id] IN ({string.Join(",", paramNames)})
                      AND [ArticleNumber] IS NOT NULL";
                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                    orders.Add((r.GetInt32(0), r.GetString(1)));
            }
            else
            {
                var weeksAhead = _configuration.GetValue<int?>("Sync:BomCacheWeeks") ?? 8;
                var maxOrders  = _configuration.GetValue<int?>("Sync:BomCacheMaxOrders") ?? 200;

                await using var cmd = new SqlCommand(@"
                    SELECT TOP (@max) [Id], [ArticleNumber]
                    FROM [ProductionOrders]
                    WHERE [IsDone] = 0
                      AND [ProductionDate] IS NOT NULL
                      AND [ProductionDate] <= DATEADD(week, @weeks, GETDATE())
                      AND [ArticleNumber] IS NOT NULL
                    ORDER BY [ProductionDate] ASC", conn) { CommandTimeout = 60 };
                cmd.Parameters.AddWithValue("@max", maxOrders);
                cmd.Parameters.AddWithValue("@weeks", weeksAhead);
                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                    orders.Add((r.GetInt32(0), r.GetString(1)));
            }

            if (orders.Count == 0)
            {
                _logger.LogInformation("CoatingDetection: Keine Auftraege - nothing to do");
                return new SyncResult(0, 0, 0);
            }

            // 3) Distinct article numbers
            var articleNumbers = orders
                .Select(o => o.ArticleNumber)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            // 4) Lookup which article numbers have coating items in the cache
            var matchSet = new HashSet<string>(StringComparer.Ordinal);
            {
                var paramNames = new List<string>();
                await using var cmd = conn.CreateCommand();
                cmd.CommandTimeout = 120;
                for (int i = 0; i < articleNumbers.Count; i++)
                {
                    var p = $"@a{i}";
                    paramNames.Add(p);
                    cmd.Parameters.AddWithValue(p, articleNumbers[i]);
                }
                cmd.Parameters.AddWithValue("@cat", lackName);
                cmd.CommandText = $@"
                    SELECT DISTINCT h.[Artikelnummer]
                    FROM [CachedBomHeaders] h
                    INNER JOIN [CachedBomItems] i ON i.[CachedBomHeaderId] = h.[Id]
                    INNER JOIN [Articles] a ON a.[ArticleNumber] = i.[Ressourcenummer]
                    INNER JOIN [ArticleCategories] c ON c.[Id] = a.[ArticleCategoryId]
                    WHERE h.[Artikelnummer] IN ({string.Join(",", paramNames)})
                      AND c.[Name] = @cat";
                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                    matchSet.Add(r.GetString(0));
            }

            // 5) Compute flags per order
            var orderIdsTrue = new List<int>();
            var orderIdsFalse = new List<int>();
            foreach (var o in orders)
            {
                if (matchSet.Contains(o.ArticleNumber))
                    orderIdsTrue.Add(o.Id);
                else
                    orderIdsFalse.Add(o.Id);
            }

            if (dryRun)
            {
                _logger.LogInformation("CoatingDetection (DryRun): {Yes} von {Total} Auftraegen haetten HasCoatingParts=true",
                    orderIdsTrue.Count, orders.Count);
                return new SyncResult(0, 0, 0);
            }

            // 6) Bulk update — set HasCoatingParts=1 for matches
            if (orderIdsTrue.Count > 0)
            {
                updatedTrue = await BulkUpdateCoatingFlagAsync(conn, orderIdsTrue, hasCoatingParts: true, ct);
            }

            // 7) Bulk update — set HasCoatingParts=0 AND IsCoatingDone=0 for non-matches
            // (per spec: when flag becomes false, also reset IsCoatingDone)
            if (orderIdsFalse.Count > 0)
            {
                updatedFalse = await BulkUpdateCoatingFlagAsync(conn, orderIdsFalse, hasCoatingParts: false, ct);
            }

            _logger.LogInformation(
                "CoatingDetection: {Total} Auftraege geprueft, {WithCoating} mit Lackierteilen, {Without} ohne",
                orders.Count, updatedTrue, updatedFalse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CoatingDetection fehlgeschlagen");
            errors++;
            errorDetails = ex.Message;
        }

        // Inserted=updatedTrue (orders with coating), Updated=updatedFalse (orders without)
        return new SyncResult(updatedTrue, updatedFalse, errors, errorDetails);
    }

    private async Task<int> BulkUpdateCoatingFlagAsync(
        SqlConnection conn, List<int> orderIds, bool hasCoatingParts, CancellationToken ct)
    {
        // Use a temp table for large IN lists
        var dt = new System.Data.DataTable();
        dt.Columns.Add("Id", typeof(int));
        foreach (var id in orderIds) dt.Rows.Add(id);

        await using (var cmd = new SqlCommand(
            "CREATE TABLE #TmpOrderIds ([Id] INT NOT NULL PRIMARY KEY)", conn))
        {
            await cmd.ExecuteNonQueryAsync(ct);
        }

        using (var bulk = new SqlBulkCopy(conn) { DestinationTableName = "#TmpOrderIds" })
        {
            bulk.ColumnMappings.Add("Id", "Id");
            await bulk.WriteToServerAsync(dt, ct);
        }

        // Update
        var updateSql = hasCoatingParts
            ? @"UPDATE p SET p.[HasCoatingParts] = 1,
                              p.[ModifiedAt] = GETUTCDATE(),
                              p.[ModifiedBy] = 'CoatingDetection',
                              p.[ModifiedByWindows] = SYSTEM_USER
                FROM [ProductionOrders] p
                INNER JOIN #TmpOrderIds t ON t.[Id] = p.[Id]
                WHERE p.[HasCoatingParts] = 0"
            : @"UPDATE p SET p.[HasCoatingParts] = 0,
                              p.[IsCoatingDone] = 0,
                              p.[ModifiedAt] = GETUTCDATE(),
                              p.[ModifiedBy] = 'CoatingDetection',
                              p.[ModifiedByWindows] = SYSTEM_USER
                FROM [ProductionOrders] p
                INNER JOIN #TmpOrderIds t ON t.[Id] = p.[Id]
                WHERE p.[HasCoatingParts] = 1";

        int affected;
        await using (var cmd = new SqlCommand(updateSql, conn) { CommandTimeout = 60 })
        {
            affected = await cmd.ExecuteNonQueryAsync(ct);
        }

        await using (var cmd = new SqlCommand("DROP TABLE #TmpOrderIds", conn))
        {
            await cmd.ExecuteNonQueryAsync(ct);
        }

        return affected;
    }
}

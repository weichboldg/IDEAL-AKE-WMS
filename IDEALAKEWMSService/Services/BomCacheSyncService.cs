using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDEALAKEWMSService.Common;
using IDEALAKEWMSService.Models;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

public class BomCacheSyncService : IBomCacheSyncService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BomCacheSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public BomCacheSyncService(
        IConfiguration configuration,
        ILogger<BomCacheSyncService> logger,
        ISyncLogger syncLogger)
    {
        _configuration = configuration;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    // ============ Main orchestration ============

    public async Task<SyncResult> SyncBomCacheAsync(bool dryRun, CancellationToken ct)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.BomCache, ct);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int inserted = 0, updated = 0, skipped = 0, errors = 0;
        string? errorDetails = null;

        try
        {
            var weeksAhead = await ServiceSettings.GetIntAsync(_configuration, "Sync:BomCacheWeeks", 8, ct);
            var maxOrders  = await ServiceSettings.GetIntAsync(_configuration, "Sync:BomCacheMaxOrders", 200, ct);
            var maxAgeHrs  = await ServiceSettings.GetIntAsync(_configuration, "Sync:BomCacheMaxAgeHours", 24, ct);

            var orders = await ReadOpenOrdersInWindowAsync(weeksAhead, maxOrders, ct);
            _logger.LogInformation("BOM-Cache-Sync: {Count} Auftraege im Window ({Weeks}w / max {Max})",
                orders.Count, weeksAhead, maxOrders);

            var articleNumbers = orders
                .Select(o => o.ArticleNumber)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (articleNumbers.Count == 0)
            {
                _logger.LogInformation("BOM-Cache-Sync: Keine Artikelnummern - nothing to do");
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["neu"] = 0,
                    ["aktualisiert"] = 0,
                    ["uebersprungen"] = 0,
                }, ct: ct);
                return new SyncResult(0, 0, 0);
            }

            var existing = await ReadHeaderHashesAsync(articleNumbers, ct);
            var sageData = await ReadFromSageBatchAsync(articleNumbers, ct);

            var oseonFilled = new HashSet<string>(StringComparer.Ordinal);
            foreach (var art in articleNumbers)
            {
                if (sageData.ContainsKey(art) && sageData[art].Count > 0) continue;

                var oseonItems = await ReadFromOseonAsync(art, ct);
                if (oseonItems.Count > 0)
                {
                    sageData[art] = oseonItems;
                    oseonFilled.Add(art);
                }
            }

            var cutoff = DateTime.UtcNow.AddHours(-maxAgeHrs);

            foreach (var art in articleNumbers)
            {
                ct.ThrowIfCancellationRequested();

                if (!sageData.TryGetValue(art, out var items) || items.Count == 0)
                {
                    _logger.LogDebug("BOM-Cache-Sync: Keine BOM-Daten fuer {Article}", art);
                    continue;
                }

                try
                {
                    var newHash = ComputeContentHash(items);

                    if (existing.TryGetValue(art, out var e)
                        && e.Hash == newHash
                        && e.CachedAt > cutoff)
                    {
                        skipped++;
                        continue;
                    }

                    if (dryRun)
                    {
                        _logger.LogInformation("BOM-Cache-Sync (DryRun): wuerde {Article} upserten ({Count} Items)", art, items.Count);
                    }
                    else
                    {
                        var source = oseonFilled.Contains(art) ? "OSEON" : "SAGE";
                        await UpsertBomToCacheAsync(art, source, newHash, items, ct);
                    }

                    if (existing.ContainsKey(art)) updated++;
                    else inserted++;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex, "BOM-Cache-Sync: Fehler bei {Article}", art);
                }
            }

            if (!dryRun)
            {
                var deleted = await DeleteOrphanHeadersAsync(articleNumbers, ct);
                if (deleted > 0)
                    _logger.LogInformation("BOM-Cache-Sync: {N} verwaiste Header geloescht", deleted);
            }

            sw.Stop();
            _logger.LogInformation(
                "BOM-Cache-Sync abgeschlossen in {Ms}ms: {Ins} neu, {Upd} aktualisiert, {Skip} unveraendert, {Err} Fehler",
                sw.ElapsedMilliseconds, inserted, updated, skipped, errors);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
                ["uebersprungen"] = skipped,
            }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);

            return new SyncResult(inserted, updated, errors, errorDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BOM-Cache-Sync fehlgeschlagen");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }

    public async Task<SyncResult> SyncSpecificArticleNumbersAsync(
        List<string> articleNumbers,
        bool dryRun,
        CancellationToken ct)
    {
        if (articleNumbers == null || articleNumbers.Count == 0)
            return new SyncResult(0, 0, 0);

        int inserted = 0, updated = 0, skipped = 0, errors = 0;
        string? errorDetails = null;

        try
        {
            var maxAgeHrs = await ServiceSettings.GetIntAsync(_configuration, "Sync:BomCacheMaxAgeHours", 24, ct);

            var distinct = articleNumbers
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (distinct.Count == 0) return new SyncResult(0, 0, 0);

            var existing = await ReadHeaderHashesAsync(distinct, ct);
            var sageData = await ReadFromSageBatchAsync(distinct, ct);

            var oseonFilled = new HashSet<string>(StringComparer.Ordinal);
            foreach (var art in distinct)
            {
                if (sageData.ContainsKey(art) && sageData[art].Count > 0) continue;
                var oseon = await ReadFromOseonAsync(art, ct);
                if (oseon.Count > 0)
                {
                    sageData[art] = oseon;
                    oseonFilled.Add(art);
                }
            }

            var cutoff = DateTime.UtcNow.AddHours(-maxAgeHrs);

            foreach (var art in distinct)
            {
                if (!sageData.TryGetValue(art, out var items) || items.Count == 0) continue;

                try
                {
                    var hash = ComputeContentHash(items);
                    if (existing.TryGetValue(art, out var e) && e.Hash == hash && e.CachedAt > cutoff)
                    {
                        skipped++;
                        continue;
                    }

                    if (!dryRun)
                    {
                        var source = oseonFilled.Contains(art) ? "OSEON" : "SAGE";
                        await UpsertBomToCacheAsync(art, source, hash, items, ct);
                    }

                    if (existing.ContainsKey(art)) updated++; else inserted++;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex, "BOM-Cache (narrow) Fehler bei {Article}", art);
                }
            }

            _logger.LogInformation(
                "BOM-Cache narrow Sync: {Ins} neu, {Upd} aktualisiert, {Skip} unveraendert, {Err} Fehler",
                inserted, updated, skipped, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BOM-Cache narrow Sync fehlgeschlagen");
            errors++;
            errorDetails = ex.Message;
        }

        return new SyncResult(inserted, updated, errors, errorDetails);
    }

    // ============ SAGE batch query ============

    private async Task<Dictionary<string, List<BomCacheItem>>> ReadFromSageBatchAsync(
        List<string> articleNumbers,
        CancellationToken ct)
    {
        var result = new Dictionary<string, List<BomCacheItem>>(StringComparer.Ordinal);
        if (articleNumbers == null || articleNumbers.Count == 0) return result;

        var connStr = ConnectionStrings.Sage(_configuration);

        var paramNames = new List<string>();
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        using var cmd = conn.CreateCommand();
        for (int i = 0; i < articleNumbers.Count; i++)
        {
            var name = $"@art{i}";
            paramNames.Add(name);
            cmd.Parameters.AddWithValue(name, articleNumbers[i]);
        }

        cmd.CommandText = $@"
            SELECT Artikelnummer, Position, Baugruppe, Ressourcenummer,
                   Bezeichnung1, Bezeichnung2, Menge, Beschaffungsartikel, Artikelgruppe
            FROM [ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]
            WHERE Artikelnummer IN ({string.Join(",", paramNames)})
            ORDER BY Artikelnummer, Position";
        cmd.CommandTimeout = 120;

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var artikelnummer = reader.IsDBNull(0) ? "" : reader.GetString(0);
            if (string.IsNullOrEmpty(artikelnummer)) continue;

            if (!result.TryGetValue(artikelnummer, out var list))
            {
                list = new List<BomCacheItem>();
                result[artikelnummer] = list;
            }

            list.Add(new BomCacheItem
            {
                Artikelnummer       = artikelnummer,
                Position            = reader.IsDBNull(1) ? "" : reader.GetValue(1).ToString() ?? "",
                Baugruppe           = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Ressourcenummer     = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Bezeichnung1        = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Bezeichnung2        = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Menge               = reader.IsDBNull(6) ? 0m : Convert.ToDecimal(reader.GetValue(6)),
                Beschaffungsartikel = reader.IsDBNull(7) ? "" : reader.GetString(7),
                Artikelgruppe       = reader.IsDBNull(8) ? "" : reader.GetString(8)
            });
        }

        _logger.LogInformation("SAGE-Batch-Query: {Count} Artikel, {Total} Items",
            result.Count, result.Values.Sum(v => v.Count));

        return result;
    }

    // ============ OSEON fallback ============

    private async Task<List<BomCacheItem>> ReadFromOseonAsync(string articleNumber, CancellationToken ct)
    {
        var result = new List<BomCacheItem>();
        if (string.IsNullOrWhiteSpace(articleNumber)) return result;

        var connStr = _configuration.GetConnectionString("OseonConnection");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            _logger.LogDebug("OseonConnection nicht konfiguriert - skip OSEON-Fallback fuer {Article}", articleNumber);
            return result;
        }

        try
        {
            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_AKE_Kommissionierung_OseonStuecklistenDB";
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandTimeout = 60;
            cmd.Parameters.AddWithValue("@Artikelnummer", articleNumber);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                result.Add(new BomCacheItem
                {
                    Artikelnummer       = articleNumber,
                    Position            = SafeString(reader, "Position"),
                    Baugruppe           = SafeString(reader, "Baugruppe"),
                    Ressourcenummer     = SafeString(reader, "Ressourcenummer"),
                    Bezeichnung1        = SafeString(reader, "Bezeichnung1"),
                    Bezeichnung2        = SafeString(reader, "Bezeichnung2"),
                    Menge               = SafeDecimal(reader, "Menge"),
                    Beschaffungsartikel = SafeString(reader, "Beschaffungsartikel"),
                    Artikelgruppe       = SafeString(reader, "Artikelgruppe")
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OSEON-Fallback fuer {Article} fehlgeschlagen", articleNumber);
        }

        return result;
    }

    private static string SafeString(SqlDataReader reader, string col)
    {
        try
        {
            var ord = reader.GetOrdinal(col);
            return reader.IsDBNull(ord) ? "" : reader.GetValue(ord).ToString() ?? "";
        }
        catch { return ""; }
    }

    private static decimal SafeDecimal(SqlDataReader reader, string col)
    {
        try
        {
            var ord = reader.GetOrdinal(col);
            return reader.IsDBNull(ord) ? 0m : Convert.ToDecimal(reader.GetValue(ord));
        }
        catch { return 0m; }
    }

    // ============ WMS DB helpers ============

    private async Task<List<(int OrderId, string ArticleNumber)>> ReadOpenOrdersInWindowAsync(
        int weeksAhead, int maxOrders, CancellationToken ct)
    {
        var connStr = ConnectionStrings.Wms(_configuration);
        var orders = new List<(int, string)>();

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        // "Abgeschlossen" = IsDone (Sage) ODER IsDonePicking (App, Leitstand-Checkbox).
        // Komm-abgeschlossene FAs (IsDone=0, IsDonePicking=1) duerfen das TOP(@max)-Fenster
        // NICHT belegen — sonst draengen alte erledigte FAs (frueher Termin, ORDER BY ASC)
        // echte offene FAs aus dem Cache. Spiegelt die Web-Semantik (v1.21.1) service-seitig.
        var sql = @"
            SELECT TOP (@max) po.[Id], po.[ArticleNumber]
            FROM [dbo].[ProductionOrders] po
            WHERE po.[IsDone] = 0
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[ProductionOrderPickingStatus] ps
                  WHERE ps.[ProductionOrderId] = po.[Id] AND ps.[IsDonePicking] = 1
              )
              AND po.[ProductionDate] IS NOT NULL
              AND po.[ProductionDate] <= DATEADD(week, @weeks, GETDATE())
              AND po.[ArticleNumber] IS NOT NULL
            ORDER BY po.[ProductionDate] ASC";

        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
        cmd.Parameters.AddWithValue("@max", maxOrders);
        cmd.Parameters.AddWithValue("@weeks", weeksAhead);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            orders.Add((reader.GetInt32(0), reader.GetString(1)));
        }
        return orders;
    }

    private async Task<Dictionary<string, (string Hash, DateTime CachedAt)>> ReadHeaderHashesAsync(
        List<string> articleNumbers, CancellationToken ct)
    {
        var result = new Dictionary<string, (string, DateTime)>(StringComparer.Ordinal);
        if (articleNumbers.Count == 0) return result;

        var connStr = ConnectionStrings.Wms(_configuration);
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        var paramNames = new List<string>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;
        for (int i = 0; i < articleNumbers.Count; i++)
        {
            var p = $"@a{i}";
            paramNames.Add(p);
            cmd.Parameters.AddWithValue(p, articleNumbers[i]);
        }
        cmd.CommandText = $@"
            SELECT [Artikelnummer], [ContentHash], [CachedAt]
            FROM [dbo].[CachedBomHeaders]
            WHERE [Artikelnummer] IN ({string.Join(",", paramNames)})";

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result[reader.GetString(0)] = (reader.GetString(1), reader.GetDateTime(2));
        }
        return result;
    }

    private async Task UpsertBomToCacheAsync(
        string articleNumber, string source, string contentHash,
        List<BomCacheItem> items, CancellationToken ct)
    {
        var connStr = ConnectionStrings.Wms(_configuration);
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);

        try
        {
            int headerId;
            await using (var cmd = new SqlCommand(@"
                SELECT [Id] FROM [dbo].[CachedBomHeaders] WHERE [Artikelnummer] = @a", conn, tx))
            {
                cmd.Parameters.AddWithValue("@a", articleNumber);
                var existing = await cmd.ExecuteScalarAsync(ct);
                if (existing != null && existing != DBNull.Value)
                {
                    headerId = Convert.ToInt32(existing);

                    await using var upd = new SqlCommand(@"
                        UPDATE [dbo].[CachedBomHeaders]
                        SET [Source] = @s, [ItemCount] = @c, [ContentHash] = @h, [CachedAt] = GETUTCDATE()
                        WHERE [Id] = @id", conn, tx);
                    upd.Parameters.AddWithValue("@s", source);
                    upd.Parameters.AddWithValue("@c", items.Count);
                    upd.Parameters.AddWithValue("@h", contentHash);
                    upd.Parameters.AddWithValue("@id", headerId);
                    await upd.ExecuteNonQueryAsync(ct);

                    await using var del = new SqlCommand(@"
                        DELETE FROM [dbo].[CachedBomItems] WHERE [CachedBomHeaderId] = @id", conn, tx);
                    del.Parameters.AddWithValue("@id", headerId);
                    await del.ExecuteNonQueryAsync(ct);
                }
                else
                {
                    await using var ins = new SqlCommand(@"
                        INSERT INTO [dbo].[CachedBomHeaders]
                            ([Artikelnummer], [Source], [ItemCount], [ContentHash], [CachedAt])
                        OUTPUT INSERTED.[Id]
                        VALUES (@a, @s, @c, @h, GETUTCDATE())", conn, tx);
                    ins.Parameters.AddWithValue("@a", articleNumber);
                    ins.Parameters.AddWithValue("@s", source);
                    ins.Parameters.AddWithValue("@c", items.Count);
                    ins.Parameters.AddWithValue("@h", contentHash);
                    headerId = Convert.ToInt32(await ins.ExecuteScalarAsync(ct));
                }
            }

            if (items.Count > 0)
            {
                var dt = new System.Data.DataTable();
                dt.Columns.Add("CachedBomHeaderId", typeof(int));
                dt.Columns.Add("Position", typeof(string));
                dt.Columns.Add("Baugruppe", typeof(string));
                dt.Columns.Add("Ressourcenummer", typeof(string));
                dt.Columns.Add("Bezeichnung1", typeof(string));
                dt.Columns.Add("Bezeichnung2", typeof(string));
                dt.Columns.Add("Menge", typeof(decimal));
                dt.Columns.Add("Beschaffungsartikel", typeof(string));
                dt.Columns.Add("Artikelgruppe", typeof(string));
                dt.Columns.Add("SortOrder", typeof(int));

                int sortOrder = 0;
                foreach (var i in items)
                {
                    dt.Rows.Add(
                        headerId,
                        (object?)i.Position ?? DBNull.Value,
                        (object?)i.Baugruppe ?? DBNull.Value,
                        (object?)i.Ressourcenummer ?? DBNull.Value,
                        (object?)i.Bezeichnung1 ?? DBNull.Value,
                        (object?)i.Bezeichnung2 ?? DBNull.Value,
                        i.Menge,
                        (object?)i.Beschaffungsartikel ?? DBNull.Value,
                        (object?)i.Artikelgruppe ?? DBNull.Value,
                        sortOrder++);
                }

                using var bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tx)
                {
                    DestinationTableName = "[dbo].[CachedBomItems]",
                    BulkCopyTimeout = 120
                };
                bulk.ColumnMappings.Add("CachedBomHeaderId", "CachedBomHeaderId");
                bulk.ColumnMappings.Add("Position", "Position");
                bulk.ColumnMappings.Add("Baugruppe", "Baugruppe");
                bulk.ColumnMappings.Add("Ressourcenummer", "Ressourcenummer");
                bulk.ColumnMappings.Add("Bezeichnung1", "Bezeichnung1");
                bulk.ColumnMappings.Add("Bezeichnung2", "Bezeichnung2");
                bulk.ColumnMappings.Add("Menge", "Menge");
                bulk.ColumnMappings.Add("Beschaffungsartikel", "Beschaffungsartikel");
                bulk.ColumnMappings.Add("Artikelgruppe", "Artikelgruppe");
                bulk.ColumnMappings.Add("SortOrder", "SortOrder");
                await bulk.WriteToServerAsync(dt, ct);
            }

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<int> DeleteOrphanHeadersAsync(
        List<string> currentArticleNumbers, CancellationToken ct)
    {
        var connStr = ConnectionStrings.Wms(_configuration);
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        if (currentArticleNumbers.Count == 0)
        {
            await using var cmd = new SqlCommand(@"DELETE FROM [dbo].[CachedBomHeaders]", conn) { CommandTimeout = 120 };
            return await cmd.ExecuteNonQueryAsync(ct);
        }

        var paramNames = new List<string>();
        await using var del = conn.CreateCommand();
        del.CommandTimeout = 120;
        for (int i = 0; i < currentArticleNumbers.Count; i++)
        {
            var p = $"@a{i}";
            paramNames.Add(p);
            del.Parameters.AddWithValue(p, currentArticleNumbers[i]);
        }
        del.CommandText = $@"
            DELETE FROM [dbo].[CachedBomHeaders]
            WHERE [Artikelnummer] NOT IN ({string.Join(",", paramNames)})";
        return await del.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Computes a deterministic SHA256 hash of a BOM item list.
    /// The list is sorted by (Position, Ressourcenummer) before hashing
    /// so the hash is stable regardless of SAGE / OSEON row order.
    /// </summary>
    internal static string ComputeContentHash(List<BomCacheItem> items)
    {
        var sorted = items
            .OrderBy(i => i.Position ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(i => i.Ressourcenummer ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        var sb = new StringBuilder();
        foreach (var item in sorted)
        {
            sb.Append(item.Position ?? "").Append('|');
            sb.Append(item.Ressourcenummer ?? "").Append('|');
            sb.Append(item.Menge.ToString(CultureInfo.InvariantCulture)).Append('|');
            sb.Append(item.Bezeichnung1 ?? "").Append('|');
            sb.Append(item.Bezeichnung2 ?? "").Append('|');
            sb.Append(item.Baugruppe ?? "").Append('|');
            sb.Append(item.Beschaffungsartikel ?? "").Append('|');
            sb.Append(item.Artikelgruppe ?? "").Append('\n');
        }

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDEALAKEWMSService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

public class BomCacheSyncService : IBomCacheSyncService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BomCacheSyncService> _logger;

    public BomCacheSyncService(
        IConfiguration configuration,
        ILogger<BomCacheSyncService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<SyncResult> SyncBomCacheAsync(bool dryRun, CancellationToken ct)
    {
        // Implemented in Task 8
        return Task.FromResult(new SyncResult(0, 0, 0));
    }

    public Task<SyncResult> SyncSpecificArticleNumbersAsync(
        List<string> articleNumbers,
        bool dryRun,
        CancellationToken ct)
    {
        // Implemented in Task 8
        return Task.FromResult(new SyncResult(0, 0, 0));
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

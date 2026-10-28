using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class SyncLogRepository : ISyncLogRepository
{
    private readonly ApplicationDbContext _context;

    public SyncLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SyncLog entry)
    {
        _context.SyncLogs.Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task<List<SyncLog>> GetRecentAsync(string? service, string? level, int limit)
    {
        IQueryable<SyncLog> q = _context.SyncLogs;
        if (!string.IsNullOrWhiteSpace(service))
            q = q.Where(x => x.Service == service);
        if (!string.IsNullOrWhiteSpace(level))
            q = q.Where(x => x.Level == level);
        return await q.OrderByDescending(x => x.Timestamp).Take(limit).ToListAsync();
    }

    public async Task<(List<SyncLog> Rows, int TotalCount)> GetPagedAsync(
        string? service, string? level, string? reference, int page, int pageSize,
        IReadOnlyDictionary<string, string>? columnFilters = null)
    {
        IQueryable<SyncLog> q = _context.SyncLogs;
        if (!string.IsNullOrWhiteSpace(service))
            q = q.Where(x => x.Service == service);
        if (!string.IsNullOrWhiteSpace(level))
            q = q.Where(x => x.Level == level);
        if (!string.IsNullOrWhiteSpace(reference))
            q = q.Where(x => x.Reference != null && x.Reference.Contains(reference));

        // v1.70.0: Spaltenfilter in SQL VOR Count/Skip/Take (vorher nur auf der geladenen Seite), Muster Bewegungshistorie.
        if (columnFilters != null)
        {
            foreach (var (key, raw) in columnFilters)
            {
                var parsed = ColumnFilterSyntax.Parse(raw, ColumnFilterTypes.Of("SyncLog/Index", key));
                if (parsed.IsEmpty) continue;
                q = ApplyColumnFilter(q, key, parsed);
            }
        }

        if (page < 1) page = 1;
        var totalCount = await q.CountAsync();
        var rows = await q
            .OrderByDescending(x => x.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (rows, totalCount);
    }

    /// <summary>Spaltenfilter des Aktivitaets-Protokolls als SQL-Bedingung. internal fuer den ToQueryString-Test.</summary>
    internal static IQueryable<SyncLog> ApplyColumnFilter(IQueryable<SyncLog> q, string key, ParsedColumnFilter f)
    {
        switch (key.ToLowerInvariant())
        {
            case "timestamp":
            {
                // Tag/Monat/Jahr, Vergleich und Bereich tageweise wie die Bewegungshistorie (Antwort 2).
                var pred = ColumnFilterSql.DatePredicate<SyncLog>(f, x => x.Timestamp);
                if (pred != null) return q.Where(pred);
                return f.Negate || f.IsEmpty ? q : q.Where(x => false); // kein lesbares Datum: positiv leer, negiert alle
            }
            case "service":
            {
                // Anzeigename ODER technischer Name; IN ueber die bekannten Konstanten.
                // ponytail: Altnamen ausserhalb von SyncLogServices.All trifft nur der negierte Filter (Antwort 3).
                var set = ColumnFilterSyntax.MatchingValues(SyncLogServices.All, SyncLogServices.DisplayName, f)
                    .Union(ColumnFilterSyntax.MatchingValues(SyncLogServices.All, s => s, f)).ToList();
                return f.Negate ? q.Where(x => !set.Contains(x.Service)) : q.Where(x => set.Contains(x.Service));
            }
        }
        var text = key.ToLowerInvariant() switch
        {
            "level" => ColumnFilterSql.Predicate<SyncLog>(f, x => x.Level),
            "reference" => ColumnFilterSql.Predicate<SyncLog>(f, x => x.Reference),
            "message" => ColumnFilterSql.Predicate<SyncLog>(f, x => x.Message),
            _ => null
        };
        return text == null ? q : q.Where(text);
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff, int batchSize, bool dryRun, CancellationToken ct = default)
    {
        if (dryRun)
            return await _context.SyncLogs.CountAsync(x => x.Timestamp < cutoff, ct);

        var total = 0;
        while (true)
        {
            var batch = await _context.SyncLogs
                .Where(x => x.Timestamp < cutoff)
                .Take(batchSize)
                .ToListAsync(ct);
            if (batch.Count == 0) break;
            _context.SyncLogs.RemoveRange(batch);
            await _context.SaveChangesAsync(ct);
            total += batch.Count;
            if (batch.Count < batchSize) break;
        }
        return total;
    }
}

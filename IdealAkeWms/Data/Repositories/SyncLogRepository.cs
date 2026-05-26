using IdealAkeWms.Data;
using IdealAkeWms.Models;
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
        string? service, string? level, string? reference, int page, int pageSize)
    {
        IQueryable<SyncLog> q = _context.SyncLogs;
        if (!string.IsNullOrWhiteSpace(service))
            q = q.Where(x => x.Service == service);
        if (!string.IsNullOrWhiteSpace(level))
            q = q.Where(x => x.Level == level);
        if (!string.IsNullOrWhiteSpace(reference))
            q = q.Where(x => x.Reference != null && x.Reference.Contains(reference));

        var totalCount = await q.CountAsync();
        var rows = await q
            .OrderByDescending(x => x.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (rows, totalCount);
    }
}

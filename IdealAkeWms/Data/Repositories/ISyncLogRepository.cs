using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface ISyncLogRepository
{
    Task AddAsync(SyncLog entry);
    Task<List<SyncLog>> GetRecentAsync(string? service, string? level, int limit);
    Task<(List<SyncLog> Rows, int TotalCount)> GetPagedAsync(string? service, string? level, string? reference, int page, int pageSize);
}

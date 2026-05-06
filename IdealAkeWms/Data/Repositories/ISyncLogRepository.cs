using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface ISyncLogRepository
{
    Task AddAsync(SyncLog entry);
    Task<List<SyncLog>> GetRecentAsync(string? service, string? level, int limit);
}

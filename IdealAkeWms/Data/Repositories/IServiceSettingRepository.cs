using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IServiceSettingRepository
{
    Task<List<ServiceSetting>> GetAllAsync();
    Task<List<ServiceSetting>> GetByCategoryAsync(string category);
    Task<string?> GetValueAsync(string key);
    Task UpsertAsync(string key, string value, string? category = null, string? description = null);
    Task DeleteAsync(string key);
}

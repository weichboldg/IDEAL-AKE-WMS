using AKEBDELight.Models;

namespace AKEBDELight.Data.Repositories;

public interface IAppSettingRepository
{
    Task<List<AppSetting>> GetAllAsync();
    Task<string?> GetValueAsync(string key);
    Task<int> GetIntValueAsync(string key, int defaultValue);
    Task SetValueAsync(string key, string value);
}

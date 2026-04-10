using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IUserViewPreferenceRepository
{
    Task<UserViewPreference?> GetByUserAndViewAsync(int userId, string viewKey);
    Task SaveAsync(int userId, string viewKey, string settingsJson, string modifiedBy, string modifiedByWindows);
    Task DeleteByUserAndViewAsync(int userId, string viewKey);
    Task DeleteAllByUserAsync(int userId);
    Task<List<UserViewPreference>> GetAllByUserAsync(int userId);
}

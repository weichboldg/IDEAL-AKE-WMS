namespace IdealAkeWms.Services;

public interface ICurrentUserService
{
    string GetWindowsUserName();
    string GetDisplayName();
    int? GetCurrentAppUserId();
    string? GetCurrentAppUserName();
    bool IsLoggedIn();
    Task<bool> HasMasterDataAccessAsync();
    Task<bool> IsAdminAsync();
}

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
    Task<bool> CanViewTrackingAsync();
    Task<bool> CanReportOperationsAsync();
    Task<bool> CanPickAsync();
}

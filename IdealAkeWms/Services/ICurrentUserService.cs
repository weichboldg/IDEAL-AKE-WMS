namespace IdealAkeWms.Services;

public interface ICurrentUserService
{
    string GetWindowsUserName();
    string GetDisplayName();
    int? GetCurrentAppUserId();
    string? GetCurrentAppUserName();
    bool IsLoggedIn();

    Task<bool> HasRoleAsync(string roleKey);
    Task<bool> HasAnyRoleAsync(params string[] roleKeys);

    Task<bool> IsAdminAsync();
    Task<bool> HasMasterDataAccessAsync();
    Task<bool> CanPickAsync();
    Task<bool> CanViewTrackingAsync();
    Task<bool> CanReportOperationsAsync();
    Task<bool> CanAccessStockAsync();
    Task<bool> CanTransferStockAsync();
    Task<bool> CanManagePickingReleaseAsync();
    Task<bool> CanFaCompletionAsync();
    Task<bool> CanUseBdeAsync();
    Task<bool> CanManageBdeShiftleadAsync();
    Task<bool> CanManageBdeAdminAsync();
}

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
    Task<bool> HasMasterDataReadAccessAsync();
    Task<bool> CanPickAsync();
    Task<bool> CanViewTrackingAsync();
    Task<bool> CanReportOperationsAsync();
    Task<bool> CanAccessStockAsync();
    Task<bool> CanProcessLagerAsync();
    Task<bool> CanTransferStockAsync();
    Task<bool> CanManagePickingReleaseAsync();
    Task<bool> CanFaCompletionAsync();
    Task<bool> HasVorbauAccessAsync();
    Task<bool> CanUseBdeAsync();
    Task<bool> CanManageBdeShiftleadAsync();
    Task<bool> CanManageBdeAdminAsync();

    /// <summary>
    /// Returns the user's persisted default page size for paginated lists, or null
    /// when no override is set (caller falls back to <see cref="PageSize.Default"/>).
    /// </summary>
    Task<int?> GetDefaultPageSizeAsync();
}

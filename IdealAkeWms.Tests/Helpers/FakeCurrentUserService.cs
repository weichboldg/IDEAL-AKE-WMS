using IdealAkeWms.Services;

namespace IdealAkeWms.Tests.Helpers;

public class FakeCurrentUserService : ICurrentUserService
{
    public string GetWindowsUserName() => "TEST\\user";
    public string GetDisplayName() => "Test User";
    public int? GetCurrentAppUserId() => 1;
    public string? GetCurrentAppUserName() => "testuser";
    public bool IsLoggedIn() => true;
    public Task<bool> HasRoleAsync(string roleKey) => Task.FromResult(true);
    public Task<bool> HasAnyRoleAsync(params string[] roleKeys) => Task.FromResult(true);
    public Task<bool> IsAdminAsync() => Task.FromResult(true);
    public Task<bool> HasMasterDataAccessAsync() => Task.FromResult(true);
    public Task<bool> CanPickAsync() => Task.FromResult(true);
    public Task<bool> CanViewTrackingAsync() => Task.FromResult(true);
    public Task<bool> CanReportOperationsAsync() => Task.FromResult(true);
    public Task<bool> CanAccessStockAsync() => Task.FromResult(true);
    public Task<bool> CanTransferStockAsync() => Task.FromResult(true);
    public Task<bool> CanManagePickingReleaseAsync() => Task.FromResult(true);
    public Task<bool> CanUseBdeAsync() => Task.FromResult(true);
    public Task<bool> CanManageBdeShiftleadAsync() => Task.FromResult(true);
    public Task<bool> CanManageBdeAdminAsync() => Task.FromResult(true);
    public Task<bool> CanFaCompletionAsync() => Task.FromResult(true);
}

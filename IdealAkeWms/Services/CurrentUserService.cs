using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IdealAkeWms.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRoleRepository _roleRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configuration;

    private HashSet<string>? _cachedRoleKeys;

    public const string SessionKeyUserId = "AppUserId";
    public const string SessionKeyUserName = "AppUserName";

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IRoleRepository roleRepository,
        IMemoryCache memoryCache,
        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _roleRepository = roleRepository;
        _memoryCache = memoryCache;
        _configuration = configuration;
    }

    public string GetWindowsUserName()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "SYSTEM";
    }

    public string GetDisplayName()
    {
        var appUserName = GetCurrentAppUserName();
        if (!string.IsNullOrEmpty(appUserName))
            return appUserName;

        var identity = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "SYSTEM";
        if (identity.Contains('\\'))
            return identity.Split('\\').Last();
        return identity;
    }

    public int? GetCurrentAppUserId()
    {
        return _httpContextAccessor.HttpContext?.Session.GetInt32(SessionKeyUserId);
    }

    public string? GetCurrentAppUserName()
    {
        return _httpContextAccessor.HttpContext?.Session.GetString(SessionKeyUserName);
    }

    public bool IsLoggedIn()
    {
        return GetCurrentAppUserId().HasValue;
    }

    public async Task<bool> HasRoleAsync(string roleKey)
    {
        var roles = await LoadRoleKeysAsync();
        return roles.Contains(roleKey);
    }

    public async Task<bool> HasAnyRoleAsync(params string[] roleKeys)
    {
        var roles = await LoadRoleKeysAsync();
        return roleKeys.Any(roles.Contains);
    }

    public async Task<bool> IsAdminAsync()
        => await HasRoleAsync(RoleKeys.Admin);

    public async Task<bool> HasMasterDataAccessAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.MasterData);

    public async Task<bool> CanPickAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Picking);

    public async Task<bool> CanViewTrackingAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Tracking);

    public async Task<bool> CanReportOperationsAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Reporting);

    public async Task<bool> CanAccessStockAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Stock, RoleKeys.StockKeyUser, RoleKeys.Picking);

    public async Task<bool> CanTransferStockAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.StockKeyUser, RoleKeys.Picking);

    private async Task<HashSet<string>> LoadRoleKeysAsync()
    {
        if (_cachedRoleKeys != null)
            return _cachedRoleKeys;

        var roleKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var userId = GetCurrentAppUserId();
        if (userId.HasValue)
        {
            var directRoles = await _roleRepository.GetRoleKeysByUserIdAsync(userId.Value);
            foreach (var key in directRoles)
                roleKeys.Add(key);
        }

        var adRoles = await GetAdGroupRolesAsync();
        foreach (var key in adRoles)
            roleKeys.Add(key);

        _cachedRoleKeys = roleKeys;
        return roleKeys;
    }

    private async Task<List<string>> GetAdGroupRolesAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var windowsUser = httpContext?.User;
        if (windowsUser?.Identity?.IsAuthenticated != true)
            return new List<string>();

        var cacheMinutes = _configuration.GetValue("Security:AdGroupCacheMinutes", 5);
        var windowsName = windowsUser.Identity.Name ?? "UNKNOWN";
        var cacheKey = $"AdGroupRoles:{windowsName}";

        if (_memoryCache.TryGetValue(cacheKey, out List<string>? cached) && cached != null)
            return cached;

        var rolesWithAdGroup = await _roleRepository.GetRolesWithAdGroupAsync();
        var matchedKeys = new List<string>();

        foreach (var role in rolesWithAdGroup)
        {
            if (!string.IsNullOrEmpty(role.AdGroup) && windowsUser.IsInRole(role.AdGroup))
                matchedKeys.Add(role.Key);
        }

        _memoryCache.Set(cacheKey, matchedKeys, TimeSpan.FromMinutes(cacheMinutes));
        return matchedKeys;
    }
}

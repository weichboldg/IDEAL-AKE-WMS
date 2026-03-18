using IdealAkeWms.Data.Repositories;

namespace IdealAkeWms.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _userRepository;
    private readonly IAppSettingRepository _settingRepository;

    // Per-request cache to avoid redundant DB queries for the same user
    private Models.User? _cachedUser;
    private int? _cachedUserId;

    public const string SessionKeyUserId = "AppUserId";
    public const string SessionKeyUserName = "AppUserName";

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IUserRepository userRepository,
        IAppSettingRepository settingRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _userRepository = userRepository;
        _settingRepository = settingRepository;
    }

    public string GetWindowsUserName()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "SYSTEM";
    }

    public string GetDisplayName()
    {
        // Zuerst Session-Benutzer prüfen
        var appUserName = GetCurrentAppUserName();
        if (!string.IsNullOrEmpty(appUserName))
            return appUserName;

        // Fallback auf Windows-Identity
        var identity = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "SYSTEM";
        if (identity.Contains('\\'))
            return identity.Split('\\').Last();
        return identity;
    }

    public int? GetCurrentAppUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.Session.GetInt32(SessionKeyUserId);
        return userId;
    }

    public string? GetCurrentAppUserName()
    {
        return _httpContextAccessor.HttpContext?.Session.GetString(SessionKeyUserName);
    }

    public bool IsLoggedIn()
    {
        return GetCurrentAppUserId().HasValue;
    }

    public async Task<bool> HasMasterDataAccessAsync()
    {
        // 1. Flag im App-User prüfen
        var user = await GetCurrentUserAsync();
        if (user?.HasMasterDataAccess == true)
            return true;

        // 2. AD-Gruppe prüfen
        var adGroup = await _settingRepository.GetValueAsync("StammdatenADGruppe");
        if (!string.IsNullOrEmpty(adGroup))
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.IsInRole(adGroup) == true)
                return true;
        }

        return false;
    }

    public async Task<bool> IsAdminAsync()
        => await CheckUserFlagAsync(u => u.IsAdmin);

    public async Task<bool> CanViewTrackingAsync()
        => await CheckUserFlagAsync(u => u.CanViewTracking);

    public async Task<bool> CanReportOperationsAsync()
        => await CheckUserFlagAsync(u => u.CanReportOperations);

    public async Task<bool> CanPickAsync()
        => await CheckUserFlagAsync(u => u.CanPick);

    private async Task<Models.User?> GetCurrentUserAsync()
    {
        var userId = GetCurrentAppUserId();
        if (!userId.HasValue)
            return null;

        // Return cached user if same ID within this request
        if (_cachedUserId == userId.Value && _cachedUser != null)
            return _cachedUser;

        _cachedUser = await _userRepository.GetByIdAsync(userId.Value);
        _cachedUserId = userId.Value;
        return _cachedUser;
    }

    private async Task<bool> CheckUserFlagAsync(Func<Models.User, bool> predicate)
    {
        var user = await GetCurrentUserAsync();
        return user != null && predicate(user);
    }
}

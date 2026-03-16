using IdealAkeWms.Data.Repositories;

namespace IdealAkeWms.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _userRepository;
    private readonly IAppSettingRepository _settingRepository;

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
        var userId = GetCurrentAppUserId();
        if (userId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user?.HasMasterDataAccess == true)
                return true;
        }

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

    private async Task<bool> CheckUserFlagAsync(Func<Models.User, bool> predicate)
    {
        var userId = GetCurrentAppUserId();
        if (!userId.HasValue)
            return false;

        var user = await _userRepository.GetByIdAsync(userId.Value);
        return user != null && predicate(user);
    }
}

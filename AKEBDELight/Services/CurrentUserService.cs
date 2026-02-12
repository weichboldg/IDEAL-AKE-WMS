namespace AKEBDELight.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public const string SessionKeyUserId = "AppUserId";
    public const string SessionKeyUserName = "AppUserName";

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
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
}

namespace AKEBDELight.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

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
        var identity = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "SYSTEM";
        // Entferne Domain-Prefix (AKE\username -> username)
        if (identity.Contains('\\'))
            return identity.Split('\\').Last();
        return identity;
    }
}

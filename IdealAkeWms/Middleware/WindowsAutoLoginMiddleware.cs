using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;

namespace IdealAkeWms.Middleware;

public class WindowsAutoLoginMiddleware : IMiddleware
{
    public const string AutoLoginTriedCookie = "IdealAkeWms.AutoLoginTried";
    public const string NoAutoLoginCookie = "IdealAkeWms.NoAutoLogin";

    private readonly IAppSettingRepository _appSettings;
    private readonly IUserRepository _userRepository;
    private readonly IChallengeIssuer _challenge;
    private readonly ILogger<WindowsAutoLoginMiddleware> _logger;

    public WindowsAutoLoginMiddleware(IAppSettingRepository appSettings, IUserRepository userRepository,
        IChallengeIssuer challenge, ILogger<WindowsAutoLoginMiddleware> logger)
    {
        _appSettings = appSettings;
        _userRepository = userRepository;
        _challenge = challenge;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            if (await ShouldTryAsync(context))
            {
                if (await TryAutoLoginOrChallengeAsync(context))
                    return; // Challenge ausgelöst → Response übernommen
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WindowsAutoLogin fehlgeschlagen — Fallback Formular");
        }
        await next(context);
    }

    private async Task<bool> ShouldTryAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (path.StartsWith("/account/") || path.StartsWith("/api/") || path.StartsWith("/lib/")
            || path.StartsWith("/css/") || path.StartsWith("/js/") || path.StartsWith("/_framework/")
            || path.Contains('.'))
            return false;
        if (context.Session.GetInt32(CurrentUserService.SessionKeyUserId).HasValue)
            return false;
        if (context.Request.Cookies.ContainsKey(NoAutoLoginCookie))
            return false;
        var flag = await _appSettings.GetValueAsync(AppSettingKeys.WindowsAuthAktiv);
        return string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <returns>true = Challenge ausgelöst (Response übernommen, kein next()).</returns>
    private async Task<bool> TryAutoLoginOrChallengeAsync(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var sam = WindowsAccountHelper.ExtractSam(context.User.Identity.Name);
            var user = sam == null ? null : await _userRepository.GetActiveByWindowsUserNameAsync(sam);
            if (user != null)
            {
                context.Session.SetInt32(CurrentUserService.SessionKeyUserId, user.Id);
                context.Session.SetString(CurrentUserService.SessionKeyUserName, user.Name);
                context.Response.Cookies.Delete(AutoLoginTriedCookie);
                context.Response.Cookies.Delete(NoAutoLoginCookie);
                return false;
            }
            context.Response.Cookies.Append(AutoLoginTriedCookie, "1", new CookieOptions { HttpOnly = true, IsEssential = true });
            return false;
        }

        if (!context.Request.Cookies.ContainsKey(AutoLoginTriedCookie))
        {
            context.Response.Cookies.Append(AutoLoginTriedCookie, "1", new CookieOptions { HttpOnly = true, IsEssential = true });
            await _challenge.ChallengeAsync(context);
            return true;
        }
        return false;
    }
}

namespace IdealAkeWms.Middleware;

/// <summary>
/// Login-Redirect: ohne Benutzer in der Session auf die Anmeldung umleiten. Ausnahmen: Anmelde-/Abmelde-Pfade, API (schuetzt der Filter),
/// Bibliotheks-/CSS-/JS-Ordner und – seit v1.30.4 (Hotfix Kommissionierfotos) – nur noch Pfade, die eine ECHTE Datei in <c>wwwroot</c> sind.
/// Vorher reichte ein Punkt im Pfad; damit war jede URL mit Punkt (z. B. Fotos) ohne Anmeldung erreichbar.
/// </summary>
public class LoginRedirectMiddleware : IMiddleware
{
    private readonly IWebHostEnvironment _env;

    public LoginRedirectMiddleware(IWebHostEnvironment env) => _env = env;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var original = context.Request.Path.Value ?? "";
        var path = original.ToLower();

        // Login-Seite und statische Dateien ausschliessen (Praefixliste unveraendert, fallstricke „LoginRedirect zaehlt Account-Pfade einzeln auf“)
        if (path.StartsWith("/account/login") ||
            path.StartsWith("/account/logout") ||
            path.StartsWith("/account/windowslogin") ||
            path.StartsWith("/api/") ||
            path.StartsWith("/lib/") ||
            path.StartsWith("/css/") ||
            path.StartsWith("/js/") ||
            path.StartsWith("/_framework/") ||
            (path.Contains('.') && _env.WebRootFileProvider.GetFileInfo(original).Exists))
        {
            await next(context);
            return;
        }

        if (!context.Session.GetInt32("AppUserId").HasValue)
        {
            context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
            return;
        }

        await next(context);
    }
}

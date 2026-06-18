using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IIS;

namespace IdealAkeWms.Middleware;

/// <summary>Kapselt die IIS-Negotiate-Challenge (für Testbarkeit der Middleware).</summary>
public interface IChallengeIssuer
{
    Task ChallengeAsync(HttpContext context);
}

public class IISChallengeIssuer : IChallengeIssuer
{
    public Task ChallengeAsync(HttpContext context)
        => context.ChallengeAsync(IISServerDefaults.AuthenticationScheme);
}

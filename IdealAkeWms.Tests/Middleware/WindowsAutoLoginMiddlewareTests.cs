using System.Security.Claims;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Middleware;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace IdealAkeWms.Tests.Middleware;

public class WindowsAutoLoginMiddlewareTests
{
    private const string WindowsUa =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
    private const string AndroidUa =
        "Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36";

    private static DefaultHttpContext MakeContext(bool authenticated, string? name, string? userAgent = WindowsUa)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/Home/Index";
        if (userAgent != null)
            ctx.Request.Headers.UserAgent = userAgent;
        ctx.User = authenticated && name != null
            ? new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, name) }, "Negotiate", ClaimTypes.Name, ClaimTypes.Role))
            : new ClaimsPrincipal(new ClaimsIdentity());
        ctx.Features.Set<Microsoft.AspNetCore.Http.Features.ISessionFeature>(new SessionFeatureStub());
        return ctx;
    }

    private static WindowsAutoLoginMiddleware Build(Mock<IUserRepository> repo, bool flag, Mock<IChallengeIssuer> challenge)
    {
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.WindowsAuthAktiv)).ReturnsAsync(flag ? "true" : "false");
        return new WindowsAutoLoginMiddleware(settings.Object, repo.Object, challenge.Object,
            Mock.Of<ILogger<WindowsAutoLoginMiddleware>>());
    }

    [Fact]
    public async Task FlagOff_CallsNext_NoSession()
    {
        var challenge = new Mock<IChallengeIssuer>();
        var ctx = MakeContext(true, "AKE\\jmuster");
        var called = false;
        await Build(new Mock<IUserRepository>(), false, challenge).InvokeAsync(ctx, _ => { called = true; return Task.CompletedTask; });
        called.Should().BeTrue();
        ctx.Session.GetInt32(CurrentUserService.SessionKeyUserId).Should().BeNull();
        challenge.Verify(c => c.ChallengeAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task IdentityMatches_SetsSession()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetActiveByWindowsUserNameAsync("jmuster"))
            .ReturnsAsync(new User { Id = 42, Name = "Max", WindowsUserName = "jmuster", IsActive = true });
        var ctx = MakeContext(true, "AKE\\jmuster");
        var called = false;
        await Build(repo, true, new Mock<IChallengeIssuer>()).InvokeAsync(ctx, _ => { called = true; return Task.CompletedTask; });
        called.Should().BeTrue();
        ctx.Session.GetInt32(CurrentUserService.SessionKeyUserId).Should().Be(42);
    }

    [Fact]
    public async Task IdentityNoMatch_SetsTriedCookie_NoSession()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetActiveByWindowsUserNameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        var ctx = MakeContext(true, "AKE\\unknown");
        await Build(repo, true, new Mock<IChallengeIssuer>()).InvokeAsync(ctx, _ => Task.CompletedTask);
        ctx.Session.GetInt32(CurrentUserService.SessionKeyUserId).Should().BeNull();
        ctx.Response.Headers["Set-Cookie"].ToString().Should().Contain(WindowsAutoLoginMiddleware.AutoLoginTriedCookie);
    }

    [Fact]
    public async Task Anonymous_NoTriedCookie_Challenges_NoNext()
    {
        var challenge = new Mock<IChallengeIssuer>();
        var ctx = MakeContext(false, null);
        var called = false;
        await Build(new Mock<IUserRepository>(), true, challenge).InvokeAsync(ctx, _ => { called = true; return Task.CompletedTask; });
        called.Should().BeFalse();
        challenge.Verify(c => c.ChallengeAsync(ctx), Times.Once);
    }

    [Fact]
    public async Task Anonymous_TriedCookieSet_CallsNext_NoChallenge()
    {
        var challenge = new Mock<IChallengeIssuer>();
        var ctx = MakeContext(false, null);
        ctx.Request.Headers["Cookie"] = $"{WindowsAutoLoginMiddleware.AutoLoginTriedCookie}=1";
        var called = false;
        await Build(new Mock<IUserRepository>(), true, challenge).InvokeAsync(ctx, _ => { called = true; return Task.CompletedTask; });
        called.Should().BeTrue();
        challenge.Verify(c => c.ChallengeAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task NoAutoLoginCookie_CallsNext_NoChallenge()
    {
        var challenge = new Mock<IChallengeIssuer>();
        var ctx = MakeContext(false, null);
        ctx.Request.Headers["Cookie"] = $"{WindowsAutoLoginMiddleware.NoAutoLoginCookie}=1";
        var called = false;
        await Build(new Mock<IUserRepository>(), true, challenge).InvokeAsync(ctx, _ => { called = true; return Task.CompletedTask; });
        called.Should().BeTrue();
        challenge.Verify(c => c.ChallengeAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task Anonymous_AndroidUa_NoChallenge_CallsNext_NoTriedCookie()
    {
        var challenge = new Mock<IChallengeIssuer>();
        var ctx = MakeContext(false, null, AndroidUa);
        var called = false;
        await Build(new Mock<IUserRepository>(), true, challenge)
            .InvokeAsync(ctx, _ => { called = true; return Task.CompletedTask; });
        called.Should().BeTrue();
        challenge.Verify(c => c.ChallengeAsync(It.IsAny<HttpContext>()), Times.Never);
        // Fallstrick #2: KEIN AutoLoginTried-Cookie, wenn nicht gechallenged wird
        ctx.Response.Headers["Set-Cookie"].ToString().Should().NotContain(WindowsAutoLoginMiddleware.AutoLoginTriedCookie);
    }

    [Fact]
    public async Task Anonymous_AndroidUa_ForceSsoCookie_Challenges_NoNext()
    {
        var challenge = new Mock<IChallengeIssuer>();
        var ctx = MakeContext(false, null, AndroidUa);
        ctx.Request.Headers["Cookie"] = $"{WindowsAutoLoginMiddleware.ForceSsoCookie}=1";
        var called = false;
        await Build(new Mock<IUserRepository>(), true, challenge)
            .InvokeAsync(ctx, _ => { called = true; return Task.CompletedTask; });
        called.Should().BeFalse();
        challenge.Verify(c => c.ChallengeAsync(ctx), Times.Once);
    }

    [Fact]
    public async Task Anonymous_NoAutoLoginCookie_WithForceSso_Challenges()
    {
        // ForceSso hebt die NoAutoLogin-Sperre nach Logout auf.
        var challenge = new Mock<IChallengeIssuer>();
        var ctx = MakeContext(false, null);
        ctx.Request.Headers["Cookie"] =
            $"{WindowsAutoLoginMiddleware.NoAutoLoginCookie}=1; {WindowsAutoLoginMiddleware.ForceSsoCookie}=1";
        var called = false;
        await Build(new Mock<IUserRepository>(), true, challenge).InvokeAsync(ctx, _ => { called = true; return Task.CompletedTask; });
        called.Should().BeFalse();
        challenge.Verify(c => c.ChallengeAsync(ctx), Times.Once);
    }

    [Fact]
    public async Task Anonymous_AlreadyTried_WithForceSso_Challenges()
    {
        // ForceSso erzwingt die Challenge trotz gesetztem AutoLoginTried-Cookie.
        var challenge = new Mock<IChallengeIssuer>();
        var ctx = MakeContext(false, null);
        ctx.Request.Headers["Cookie"] =
            $"{WindowsAutoLoginMiddleware.AutoLoginTriedCookie}=1; {WindowsAutoLoginMiddleware.ForceSsoCookie}=1";
        var called = false;
        await Build(new Mock<IUserRepository>(), true, challenge).InvokeAsync(ctx, _ => { called = true; return Task.CompletedTask; });
        called.Should().BeFalse();
        challenge.Verify(c => c.ChallengeAsync(ctx), Times.Once);
    }

    [Fact]
    public async Task SessionExists_NormalizesAuthenticatedWindowsUserToAnonymous()
    {
        // Antiforgery-Fix: sobald eine App-Session besteht, wird die (per Verbindung
        // schwankende) Windows-Identitaet auf anonym gesetzt, damit der Antiforgery-Token
        // konsistent gebunden ist und POSTs unter SSO nicht mit 400 fehlschlagen.
        var ctx = MakeContext(true, "AKE\\jmuster");
        ctx.Session.SetInt32(CurrentUserService.SessionKeyUserId, 42);
        await Build(new Mock<IUserRepository>(), true, new Mock<IChallengeIssuer>())
            .InvokeAsync(ctx, _ => Task.CompletedTask);
        ctx.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task IdentityMatches_CapturesWindowsUserNameInSession_AndNormalizes()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetActiveByWindowsUserNameAsync("jmuster"))
            .ReturnsAsync(new User { Id = 42, Name = "Max", WindowsUserName = "jmuster", IsActive = true });
        var ctx = MakeContext(true, "AKE\\jmuster");
        await Build(repo, true, new Mock<IChallengeIssuer>()).InvokeAsync(ctx, _ => Task.CompletedTask);
        // Windows-Name fuers Audit in der Session, User danach normalisiert
        ctx.Session.GetString(CurrentUserService.SessionKeyWindowsUserName).Should().Be("AKE\\jmuster");
        ctx.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task LoginPage_NormalizesAuthenticatedUser_EvenWithoutSession()
    {
        // /account/* wird normalisiert, damit auch der Login-Formular-Token konsistent
        // anonym-gebunden ist (dort laeuft kein SAM-Match, der die Identitaet braucht).
        var ctx = MakeContext(true, "AKE\\jmuster");
        ctx.Request.Path = "/account/login";
        await Build(new Mock<IUserRepository>(), true, new Mock<IChallengeIssuer>())
            .InvokeAsync(ctx, _ => Task.CompletedTask);
        ctx.Session.GetInt32(CurrentUserService.SessionKeyUserId).Should().BeNull();
        ctx.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task StaticPath_NotNormalized_NoSessionAccess()
    {
        // Statische Pfade werden ausgelassen (kein Session-Laden, kein Strippen).
        var ctx = MakeContext(true, "AKE\\jmuster");
        ctx.Request.Path = "/css/site.css";
        ctx.Session.SetInt32(CurrentUserService.SessionKeyUserId, 42);
        await Build(new Mock<IUserRepository>(), true, new Mock<IChallengeIssuer>())
            .InvokeAsync(ctx, _ => Task.CompletedTask);
        ctx.User.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task NoSession_DoesNotNormalize()
    {
        // Ohne App-Session (z. B. Login-Seite) bleibt die Windows-Identitaet erhalten,
        // damit der Login-Flow den Windows-Namen noch lesen kann.
        var ctx = MakeContext(true, "AKE\\jmuster");
        await Build(new Mock<IUserRepository>(), false, new Mock<IChallengeIssuer>())
            .InvokeAsync(ctx, _ => Task.CompletedTask);
        ctx.Session.GetInt32(CurrentUserService.SessionKeyUserId).Should().BeNull();
        ctx.User.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task IdentityMatches_WithForceSso_SetsSession_DeletesForceSso()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetActiveByWindowsUserNameAsync("jmuster"))
            .ReturnsAsync(new User { Id = 42, Name = "Max", WindowsUserName = "jmuster", IsActive = true });
        // authentifiziert -> UA egal (Gate greift nur im anonym-Zweig)
        var ctx = MakeContext(true, "AKE\\jmuster", AndroidUa);
        ctx.Request.Headers["Cookie"] = $"{WindowsAutoLoginMiddleware.ForceSsoCookie}=1";
        await Build(repo, true, new Mock<IChallengeIssuer>()).InvokeAsync(ctx, _ => Task.CompletedTask);
        ctx.Session.GetInt32(CurrentUserService.SessionKeyUserId).Should().Be(42);
        // ForceSso wird beim Erfolg geloescht (Delete emittiert Set-Cookie mit dem Namen)
        ctx.Response.Headers["Set-Cookie"].ToString().Should().Contain(WindowsAutoLoginMiddleware.ForceSsoCookie);
    }
}

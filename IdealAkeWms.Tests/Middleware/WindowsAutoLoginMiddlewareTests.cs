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
    private static DefaultHttpContext MakeContext(bool authenticated, string? name)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/Home/Index";
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
}

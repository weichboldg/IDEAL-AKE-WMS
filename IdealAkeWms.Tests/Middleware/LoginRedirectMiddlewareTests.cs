using FluentAssertions;
using IdealAkeWms.Middleware;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Middleware;

/// <summary>v1.30.4 (Hotfix Kommissionierfotos, AK 1–3): Pfad mit Punkt ist nur dann ohne Anmeldung erreichbar, wenn er eine echte Datei in
/// wwwroot ist.</summary>
public class LoginRedirectMiddlewareTests : IDisposable
{
    private readonly string _webRoot = Path.Combine(Path.GetTempPath(), "wms-login-" + Guid.NewGuid().ToString("N"));

    public LoginRedirectMiddlewareTests()
    {
        Directory.CreateDirectory(Path.Combine(_webRoot, "images"));
        File.WriteAllText(Path.Combine(_webRoot, "favicon.ico"), "x");
        File.WriteAllText(Path.Combine(_webRoot, "images", "ideal-ake-logo.svg"), "<svg/>");
    }

    public void Dispose() => Directory.Delete(_webRoot, true);

    private async Task<(bool Next, int Status, string? Location)> RunAsync(string path, bool angemeldet = false)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.WebRootFileProvider).Returns(new PhysicalFileProvider(_webRoot));
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        var session = new SessionFeatureStub();
        ctx.Features.Set<Microsoft.AspNetCore.Http.Features.ISessionFeature>(session);
        if (angemeldet) session.Session.SetInt32("AppUserId", 1);
        var next = false;
        await new LoginRedirectMiddleware(env.Object).InvokeAsync(ctx, _ => { next = true; return Task.CompletedTask; });
        return (next, ctx.Response.StatusCode, ctx.Response.Headers.Location.ToString());
    }

    [Theory]
    [InlineData("/Fotos/Kommissionierung/FA1_20261108000000_1.jpg")]
    [InlineData("/Home/Index.x")]
    public async Task PunktImPfad_OhneEchteDatei_Ohne_Anmeldung_Login(string path)
    {
        var r = await RunAsync(path);
        r.Next.Should().BeFalse();
        r.Status.Should().Be(302);
        r.Location.Should().StartWith("/Account/Login?returnUrl=");
    }

    [Theory]
    [InlineData("/favicon.ico")]
    [InlineData("/images/ideal-ake-logo.svg")]
    [InlineData("/css/site.css")]
    [InlineData("/api/photos/1")]
    [InlineData("/Account/Login")]
    public async Task EchteDatei_Oder_Ausnahmepraefix_OhneAnmeldung_Durchgelassen(string path) =>
        (await RunAsync(path)).Next.Should().BeTrue();

    [Fact]
    public async Task Angemeldet_FotoUrl_Durchgelassen_FilterEntscheidetDanach() =>
        (await RunAsync("/Fotos/Kommissionierung/FA1_x.jpg", angemeldet: true)).Next.Should().BeTrue();
}

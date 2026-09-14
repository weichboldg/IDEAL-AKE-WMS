using FluentAssertions;
using IdealAkeWms.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class PdfRenderStatusTests
{
    private static PdfRenderStatus Create(string? configuredPath)
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PdfRender:EdgePath"] = configuredPath
        }).Build();
        return new PdfRenderStatus(cfg, NullLogger<PdfRenderStatus>.Instance);
    }

    [Fact]
    public void Refresh_ConfiguredPathExists_IsAvailable()
    {
        var file = Path.GetTempFileName();
        try
        {
            var status = Create(file);
            status.Refresh();
            status.IsAvailable.Should().BeTrue();
            status.EdgePath.Should().Be(file);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void Refresh_ConfiguredPathMissing_IsUnavailable_NoFallback()
    {
        var status = Create(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "msedge.exe"));
        status.Refresh();
        status.IsAvailable.Should().BeFalse("ein explizit konfigurierter, falscher Pfad soll auffallen, nicht still ueberbrueckt werden");
    }

    [Fact]
    public void BeforeRefresh_IsUnavailable()
        => Create(null).IsAvailable.Should().BeFalse();
}

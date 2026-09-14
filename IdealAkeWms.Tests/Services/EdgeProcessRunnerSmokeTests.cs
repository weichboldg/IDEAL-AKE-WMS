using FluentAssertions;
using IdealAkeWms.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace IdealAkeWms.Tests.Services;

/// <summary>Echter msedge-Aufruf. Ohne Edge auf der Maschine wird der Test frueh beendet (gruen, aber
/// ohne Beweis — die Ausgabe sagt es). Auf Dev-Rechnern und dem Web-Server ist Edge da.</summary>
public class EdgeProcessRunnerSmokeTests
{
    private readonly ITestOutputHelper _out;
    public EdgeProcessRunnerSmokeTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public async Task RunAsync_RealEdge_WritesCompletePdf()
    {
        var status = new PdfRenderStatus(new ConfigurationBuilder().Build(), NullLogger<PdfRenderStatus>.Instance);
        status.Refresh();
        if (!status.IsAvailable) { _out.WriteLine("UEBERSPRUNGEN: kein msedge.exe auf dieser Maschine."); return; }

        var dir = Path.Combine(Path.GetTempPath(), "IdealAkeWms-Pdf-Test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var html = Path.Combine(dir, "in.html");
            var pdf = Path.Combine(dir, "out.pdf");
            await File.WriteAllTextAsync(html, "<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body><h1>Smoke ÄÖÜ</h1></body></html>");

            var ok = await new EdgeProcessRunner(NullLogger<EdgeProcessRunner>.Instance)
                .RunAsync(status.EdgePath!, html, pdf, Path.Combine(dir, "profile"), TimeSpan.FromSeconds(30), CancellationToken.None);

            ok.Should().BeTrue();
            var bytes = await File.ReadAllBytesAsync(pdf);
            System.Text.Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
            System.Text.Encoding.ASCII.GetString(bytes[^16..]).Should().Contain("%%EOF");
            _out.WriteLine($"PDF: {bytes.Length} Bytes via {status.EdgePath}");
        }
        finally { try { Directory.Delete(dir, true); } catch (IOException) { } }
    }
}

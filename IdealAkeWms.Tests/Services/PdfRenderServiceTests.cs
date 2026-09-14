using FluentAssertions;
using IdealAkeWms.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class PdfRenderServiceTests
{
    private static readonly byte[] FakePdf = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4 fake\n%%EOF\n");

    private static (PdfRenderService svc, Mock<IEdgeProcessRunner> runner) Create(bool edgeAvailable, int maxConcurrent = 2)
    {
        var edge = Path.GetTempFileName();                       // existiert => Status verfuegbar
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PdfRender:EdgePath"] = edgeAvailable ? edge : Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "x.exe"),
            ["PdfRender:TimeoutSeconds"] = "5",
            ["PdfRender:MaxConcurrent"] = maxConcurrent.ToString()
        }).Build();
        var status = new PdfRenderStatus(cfg, NullLogger<PdfRenderStatus>.Instance);
        status.Refresh();
        var runner = new Mock<IEdgeProcessRunner>(MockBehavior.Strict);
        return (new PdfRenderService(status, runner.Object, cfg, NullLogger<PdfRenderService>.Instance), runner);
    }

    /// <summary>Runner-Mock, der wie Edge die PDF-Datei schreibt.</summary>
    private static void RunnerWrites(Mock<IEdgeProcessRunner> runner, byte[]? content, bool result = true)
        => runner.Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, string, string, TimeSpan, CancellationToken>((_, html, pdf, _, _, _) =>
            {
                File.Exists(html).Should().BeTrue("das HTML muss vor dem Edge-Aufruf geschrieben sein");
                if (content != null) File.WriteAllBytes(pdf, content);
                return Task.FromResult(result);
            });

    private static int RunDirCount() => Directory.Exists(PdfRenderService.WorkRoot) ? Directory.GetDirectories(PdfRenderService.WorkRoot).Length : 0;

    [Fact]
    public async Task Render_Success_ReturnsBytes_AndCleansRunDirectory()
    {
        var (svc, runner) = Create(edgeAvailable: true);
        RunnerWrites(runner, FakePdf);
        var before = RunDirCount();

        var bytes = await svc.RenderHtmlToPdfAsync("<html><body>x</body></html>");

        bytes.Should().Equal(FakePdf);
        RunDirCount().Should().Be(before, "das eigene Laufverzeichnis wird im finally geloescht (AK 5)");
    }

    [Fact]
    public async Task Render_EdgeUnavailable_ThrowsNotConfigured_WithoutRunner()
    {
        var (svc, _) = Create(edgeAvailable: false);   // Strict-Mock ohne Setup => jeder Aufruf wuerde werfen
        var act = () => svc.RenderHtmlToPdfAsync("<html/>");
        (await act.Should().ThrowAsync<PdfRenderException>()).Which.UserMessage.Should().Be(PdfRenderException.NotConfiguredMessage);
    }

    [Fact]
    public async Task Render_RunnerTimeout_ThrowsFailed_AndCleans()
    {
        var (svc, runner) = Create(edgeAvailable: true);
        RunnerWrites(runner, content: null, result: false);
        var before = RunDirCount();
        var act = () => svc.RenderHtmlToPdfAsync("<html/>");
        (await act.Should().ThrowAsync<PdfRenderException>()).Which.UserMessage.Should().Be(PdfRenderException.FailedMessage);
        RunDirCount().Should().Be(before);
    }

    [Fact]
    public async Task Render_RunnerTrueButEmptyPdf_ThrowsFailed()
    {
        var (svc, runner) = Create(edgeAvailable: true);
        RunnerWrites(runner, content: Array.Empty<byte>(), result: true);
        var act = () => svc.RenderHtmlToPdfAsync("<html/>");
        await act.Should().ThrowAsync<PdfRenderException>("Exit 0 / true ohne Inhalt ist ein Fehler, Spec-Schritt 5");
    }

    [Fact]
    public async Task Render_RunnerThrows_ReleasesSemaphore_SecondCallRuns()
    {
        var (svc, runner) = Create(edgeAvailable: true, maxConcurrent: 1);
        var calls = 0;
        runner.Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, string, string, TimeSpan, CancellationToken>((_, _, pdf, _, _, _) =>
            {
                if (calls++ == 0) throw new InvalidOperationException("boom");   // erster Aufruf: Runner-Absturz
                File.WriteAllBytes(pdf, FakePdf);
                return Task.FromResult(true);
            });

        await FluentActions.Awaiting(() => svc.RenderHtmlToPdfAsync("<html/>")).Should().ThrowAsync<PdfRenderException>();
        var second = svc.RenderHtmlToPdfAsync("<html/>");
        (await second.WaitAsync(TimeSpan.FromSeconds(5))).Should().Equal(FakePdf, "das Semaphor muss im finally freigegeben werden");
    }

    [Fact]
    public async Task Render_RemovesOrphanedRunDirectories_KeepsFresh()
    {
        var (svc, runner) = Create(edgeAvailable: true);
        RunnerWrites(runner, FakePdf);
        Directory.CreateDirectory(PdfRenderService.WorkRoot);
        var orphan = Directory.CreateDirectory(Path.Combine(PdfRenderService.WorkRoot, "orphan-" + Guid.NewGuid().ToString("N")));
        Directory.SetCreationTimeUtc(orphan.FullName, DateTime.UtcNow.AddHours(-2));
        var fresh = Directory.CreateDirectory(Path.Combine(PdfRenderService.WorkRoot, "fresh-" + Guid.NewGuid().ToString("N")));
        try
        {
            await svc.RenderHtmlToPdfAsync("<html/>");
            Directory.Exists(orphan.FullName).Should().BeFalse("verwaiste Laeufe (App-Pool-Recycle) werden beim naechsten Lauf entfernt (AK 17)");
            Directory.Exists(fresh.FullName).Should().BeTrue("ein laufender Nachbar-Lauf darf nicht geloescht werden");
        }
        finally { try { Directory.Delete(fresh.FullName, true); } catch (IOException) { } }
    }
}

using System.Diagnostics;

namespace IdealAkeWms.Services;

/// <summary>HTML rein, PDF-Bytes raus — kennt weder Dateinamen noch HTTP-Kontext noch Zielsystem.</summary>
public interface IPdfRenderService
{
    Task<byte[]> RenderHtmlToPdfAsync(string html, CancellationToken ct = default);
}

/// <summary>
/// <b>Singleton (zwingend):</b> das <see cref="SemaphoreSlim"/> begrenzt die gleichzeitigen Edge-Prozesse
/// anfrageuebergreifend; bei Scoped haette jede Anfrage ihr eigenes Semaphor (AK 14, Waechter:
/// <c>PdfRenderServiceDiResolutionTests</c>). Ablauf je Aufruf: verwaiste Laufverzeichnisse entfernen →
/// Semaphor (warten, nicht abweisen) → GUID-Laufverzeichnis → HTML schreiben → Runner → PDF lesen →
/// finally: Verzeichnis loeschen + Semaphor freigeben. Laufzeit wird protokolliert, damit der
/// Timeout spaeter an echten Daten nachgezogen werden kann.
/// </summary>
public sealed class PdfRenderService : IPdfRenderService
{
    public static readonly string WorkRoot = Path.Combine(Path.GetTempPath(), "IdealAkeWms-Pdf");
    private static readonly TimeSpan OrphanGrace = TimeSpan.FromMinutes(5);

    private readonly PdfRenderStatus _status;
    private readonly IEdgeProcessRunner _runner;
    private readonly ILogger<PdfRenderService> _logger;
    private readonly TimeSpan _timeout;
    private readonly SemaphoreSlim _gate;

    public PdfRenderService(PdfRenderStatus status, IEdgeProcessRunner runner, IConfiguration configuration, ILogger<PdfRenderService> logger)
    {
        _status = status;
        _runner = runner;
        _logger = logger;
        _timeout = TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue("PdfRender:TimeoutSeconds", 30)));
        _gate = new SemaphoreSlim(Math.Max(1, configuration.GetValue("PdfRender:MaxConcurrent", 2)));
    }

    public async Task<byte[]> RenderHtmlToPdfAsync(string html, CancellationToken ct = default)
    {
        var edgePath = _status.EdgePath ?? throw PdfRenderException.NotConfigured();

        RemoveOrphanedRunDirectories();

        var total = Stopwatch.StartNew();
        await _gate.WaitAsync(ct);
        var waitedMs = total.ElapsedMilliseconds;

        var runDir = Path.Combine(WorkRoot, Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(runDir);
            var htmlPath = Path.Combine(runDir, "in.html");
            var pdfPath = Path.Combine(runDir, "out.pdf");
            await File.WriteAllTextAsync(htmlPath, html, new System.Text.UTF8Encoding(false), ct);

            bool completed;
            try
            {
                completed = await _runner.RunAsync(edgePath, htmlPath, pdfPath, Path.Combine(runDir, "profile"), _timeout, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "PDF-Erzeugung: Edge-Aufruf fehlgeschlagen ({Edge})", edgePath);
                throw PdfRenderException.Failed("Edge-Prozess konnte nicht ausgefuehrt werden", ex);
            }

            if (!completed)
                throw PdfRenderException.Failed($"Timeout nach {_timeout.TotalSeconds:0}s");

            var bytes = File.Exists(pdfPath) ? await File.ReadAllBytesAsync(pdfPath, ct) : Array.Empty<byte>();
            if (bytes.Length == 0)
                throw PdfRenderException.Failed("Edge meldete Erfolg, aber die PDF-Datei fehlt oder ist leer");

            _logger.LogInformation("PDF erzeugt: {Bytes} Bytes in {ElapsedMs} ms (davon {WaitMs} ms Wartezeit auf das Semaphor)",
                bytes.Length, total.ElapsedMilliseconds, waitedMs);
            return bytes;
        }
        finally
        {
            _gate.Release();
            TryDelete(runDir);
        }
    }

    private void RemoveOrphanedRunDirectories()
    {
        if (!Directory.Exists(WorkRoot)) return;
        var cutoff = DateTime.UtcNow - (_timeout + OrphanGrace);
        foreach (var dir in Directory.GetDirectories(WorkRoot))
        {
            try
            {
                if (Directory.GetCreationTimeUtc(dir) < cutoff) TryDelete(dir);
            }
            catch (Exception ex) { _logger.LogDebug(ex, "PDF-Erzeugung: verwaistes Verzeichnis nicht pruefbar: {Dir}", dir); }
        }
    }

    private void TryDelete(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
        catch (Exception ex) { _logger.LogDebug(ex, "PDF-Erzeugung: Laufverzeichnis noch gesperrt, wird beim naechsten Lauf entfernt: {Dir}", dir); }
    }
}

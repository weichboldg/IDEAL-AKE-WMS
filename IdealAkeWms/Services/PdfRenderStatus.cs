using Microsoft.Win32;

namespace IdealAkeWms.Services;

/// <summary>
/// Einmal beim Anwendungsstart ermittelter <c>msedge.exe</c>-Pfad (Vorbild
/// <see cref="HierarchischeStruktur.HierarchischeStrukturStatus"/>): Registrierung als Singleton,
/// <see cref="Refresh"/> in <c>Program.cs</c> nach <c>builder.Build()</c>. Fehlt Edge, erscheint der
/// PDF-Knopf gar nicht (Freigabe-Antwort 2) — der Anwender soll nicht in eine Wand laufen.
/// Reihenfolge: <c>PdfRender:EdgePath</c> (gesetzt und fehlend ⇒ NICHT verfuegbar, kein stilles
/// Fallback) → Registry App Paths → Standardpfade → PATH.
/// </summary>
public sealed class PdfRenderStatus
{
    private const string AppPathsKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe";

    private readonly IConfiguration _configuration;
    private readonly ILogger<PdfRenderStatus> _logger;
    private volatile string? _edgePath;

    public PdfRenderStatus(IConfiguration configuration, ILogger<PdfRenderStatus> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string? EdgePath => _edgePath;
    public bool IsAvailable => _edgePath != null;

    public void Refresh()
    {
        var configured = _configuration.GetValue<string?>("PdfRender:EdgePath");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (File.Exists(configured))
            {
                _edgePath = configured;
                _logger.LogInformation("PDF-Erzeugung: msedge.exe aus Konfiguration: {Path}", configured);
            }
            else
            {
                _edgePath = null;
                _logger.LogWarning("PDF-Erzeugung NICHT verfuegbar: konfigurierter Pfad PdfRender:EdgePath existiert nicht: {Path}", configured);
            }
            return;
        }

        _edgePath = Candidates().FirstOrDefault(File.Exists);
        if (_edgePath != null)
            _logger.LogInformation("PDF-Erzeugung: msedge.exe gefunden: {Path}", _edgePath);
        else
            _logger.LogWarning("PDF-Erzeugung NICHT verfuegbar: msedge.exe weder in der Registry (App Paths), in den Standardpfaden noch im PATH gefunden — PDF-Knopf wird ausgeblendet.");
    }

    private static IEnumerable<string> Candidates()
    {
        if (OperatingSystem.IsWindows()
            && Registry.GetValue(AppPathsKey, null, null) is string fromRegistry
            && !string.IsNullOrWhiteSpace(fromRegistry))
            yield return fromRegistry;

        foreach (var root in new[] { Environment.GetEnvironmentVariable("ProgramFiles(x86)"), Environment.GetEnvironmentVariable("ProgramFiles") })
            if (!string.IsNullOrEmpty(root))
                yield return Path.Combine(root, "Microsoft", "Edge", "Application", "msedge.exe");

        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            yield return Path.Combine(dir.Trim(), "msedge.exe");
    }
}

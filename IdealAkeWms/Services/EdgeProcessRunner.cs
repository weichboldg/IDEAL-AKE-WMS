using System.Diagnostics;

namespace IdealAkeWms.Services;

/// <summary>Duenne, mockbare Grenze um den echten <c>msedge</c>-Prozess (nur am echten System pruefbar —
/// wie LDAP/Negotiate, siehe fallstricke.md §4). Liefert <c>true</c>, sobald die PDF-Datei fertig ist,
/// <c>false</c> bei Timeout (Prozesse dann beendet). Startfehler werfen.</summary>
public interface IEdgeProcessRunner
{
    Task<bool> RunAsync(string edgePath, string htmlPath, string pdfPath, string profileDir, TimeSpan timeout, CancellationToken ct);
}

/// <summary>
/// Gemessen am 2026-09-14 (Edge 152): <c>msedge.exe</c> ist ein <b>Launcher</b> — der gestartete Prozess
/// endet nach ~250 ms mit Exit-Code 0, das PDF schreiben Kindprozesse ~1,5 s spaeter. Deshalb wird hier
/// auf die <b>fertige Datei</b> gewartet (exklusiv oeffenbar + <c>%%EOF</c>), nicht auf den Prozess;
/// der Exit-Code wird nur protokolliert. Ein eigenes <c>--user-data-dir</c> je Lauf ist Pflicht —
/// sonst haengt sich der Aufruf an eine laufende Edge-Instanz und erzeugt nichts.
/// </summary>
public sealed class EdgeProcessRunner : IEdgeProcessRunner
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan ChildStartWindow = TimeSpan.FromSeconds(3);

    private readonly ILogger<EdgeProcessRunner> _logger;

    public EdgeProcessRunner(ILogger<EdgeProcessRunner> logger) => _logger = logger;

    public async Task<bool> RunAsync(string edgePath, string htmlPath, string pdfPath, string profileDir, TimeSpan timeout, CancellationToken ct)
    {
        var launchedAt = DateTime.Now;
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = edgePath,
                Arguments = $"--headless --disable-gpu --no-first-run --no-default-browser-check " +
                            $"--user-data-dir=\"{profileDir}\" --no-pdf-header-footer " +
                            $"--print-to-pdf=\"{pdfPath}\" \"{htmlPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        _ = process.StandardOutput.ReadToEndAsync(ct);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);
        try
        {
            while (!IsPdfComplete(pdfPath))
                await Task.Delay(PollInterval, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            KillChildren(launchedAt);
            _logger.LogWarning("PDF-Erzeugung: Timeout nach {Timeout}s, Edge-Prozesse beendet. stderr: {Stderr}",
                timeout.TotalSeconds, await SafeStderrAsync(stderrTask));
            return false;
        }

        // Launcher ist laengst weg; Kinder beenden sich nach dem Schreiben — kurz warten, damit das
        // Profilverzeichnis beim Aufraeumen nicht mehr gesperrt ist.
        foreach (var child in ChildrenOf(launchedAt))
            using (child) child.WaitForExit(2000);

        _logger.LogDebug("PDF-Erzeugung: Launcher-ExitCode {ExitCode}, stderr: {Stderr}",
            process.HasExited ? process.ExitCode : -1, await SafeStderrAsync(stderrTask));
        return true;
    }

    /// <summary>Fertig = existiert, exklusiv oeffenbar (Edge haelt sie beim Schreiben) und endet auf %%EOF.</summary>
    private static bool IsPdfComplete(string pdfPath)
    {
        if (!File.Exists(pdfPath)) return false;
        try
        {
            using var fs = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.None);
            if (fs.Length < 16) return false;
            var tail = new byte[16];
            fs.Seek(-16, SeekOrigin.End);
            fs.ReadExactly(tail);
            return System.Text.Encoding.ASCII.GetString(tail).Contains("%%EOF");
        }
        catch (IOException) { return false; }
    }

    // ponytail: Kinder ueber das Startzeit-Fenster statt Parent-PID — bei Timeout UND einem parallelen
    // Lauf, der innerhalb von 3 s startete, wird auch dessen Browser beendet (er meldet dann ebenfalls
    // „nicht erzeugt"). Upgrade-Pfad: Parent-PID via NtQueryInformationProcess.
    private static IEnumerable<Process> ChildrenOf(DateTime launchedAt)
    {
        foreach (var p in Process.GetProcessesByName("msedge"))
        {
            bool inWindow;
            try { inWindow = p.StartTime >= launchedAt.AddSeconds(-1) && p.StartTime <= launchedAt + ChildStartWindow; }
            catch (Exception) { inWindow = false; }   // fremder Prozess (Access denied) oder schon weg
            if (inWindow) yield return p; else p.Dispose();
        }
    }

    private void KillChildren(DateTime launchedAt)
    {
        foreach (var child in ChildrenOf(launchedAt))
        {
            using (child)
            {
                try { child.Kill(entireProcessTree: true); }
                catch (Exception ex) { _logger.LogDebug(ex, "PDF-Erzeugung: Edge-Prozess {Pid} konnte nicht beendet werden", child.Id); }
            }
        }
    }

    private static async Task<string> SafeStderrAsync(Task<string> stderrTask)
    {
        try { return (await stderrTask).Trim(); }
        catch (Exception) { return string.Empty; }
    }
}

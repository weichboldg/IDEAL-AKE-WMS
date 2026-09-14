# PDF-Erzeugung fuer FaHierarchy-Druckdokumente — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ein Querschnitts-Baustein, der aus dem bestehenden Print-HTML (Teil 3 Kommissionierliste, Teil 4 Beschichtungsauftrag) per Headless-Edge ein PDF je HauptFA erzeugt und als Download ausliefert.

**Architecture:** `PdfRenderStatus` (Singleton) ermittelt `msedge.exe` einmal beim Start. `PdfRenderService` (Singleton, `SemaphoreSlim`) schreibt HTML in ein GUID-Laufverzeichnis, laesst `EdgeProcessRunner` Edge starten und auf die **fertige PDF-Datei** warten, liest die Bytes, raeumt auf. `RazorViewRenderer` rendert die unveraenderte `Print.cshtml` zu einem String. Zwei neue `Pdf`-Actions rendern genau eine Gruppe; ein PDF-Knopf je Gruppen-Kopfzeile, nur sichtbar wenn Edge da ist.

**Tech Stack:** ASP.NET Core 10 MVC, `System.Diagnostics.Process`, `Microsoft.Win32.Registry` (inbox), xUnit/FluentAssertions/Moq. Keine neue Abhaengigkeit.

**Spec:** `C:\git\IDEAL-AKE-WMS\secondbrain\specs\freigegeben\2026-08-06-pdf-erzeugung-fahierarchy-druck-spec.md` (Hauptcheckout; der Worktree hat kein `secondbrain/`).

**Worktree:** `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`. Alle Pfade unten relativ zum Worktree.

## Global Constraints

- `IPdfRenderService`, `PdfRenderStatus`, `IEdgeProcessRunner` sind **Singleton**; `IRazorViewRenderer` Scoped. Kein `static`-Feld fuer das Semaphor.
- PDF-Knopf: **Icon UND Text „PDF"**, **kein** `target="_blank"` (der bestehende Drucken-Link hat es — nicht kopieren).
- Route `/<Controller>/Pdf/{hauptFa}` als Routen-Segment; `target` + `colf_*` bleiben Query-Parameter und werden **mitgegeben**.
- Dateiname exakt `<Dokumentart>_<HauptFA>_<yyyyMMdd-HHmm>.pdf`, Dokumentart `Kommissionierliste` bzw. `Beschichtungsauftrag`.
- Konfiguration nur `appsettings.json`: `PdfRender:EdgePath` (optional), `PdfRender:TimeoutSeconds` (Default 30), `PdfRender:MaxConcurrent` (Default 2). Temp-Verzeichnis **nicht** konfigurierbar: `Path.Combine(Path.GetTempPath(), "IdealAkeWms-Pdf")`.
- Nutzermeldungen exakt: Edge fehlt → `PDF-Erzeugung ist auf diesem Server nicht eingerichtet.`; Timeout/Absturz → `Das PDF konnte nicht erzeugt werden. Bitte den Bildschirmdruck verwenden.` Ausgabe ueber `TempData["WarningMessage"]` (kein `ErrorMessage`).
- Laufzeit je Erzeugung mit `Stopwatch` + `ILogger.LogInformation` protokollieren.
- `PdfRenderService.cs` hat **keine** Abhaengigkeit auf `HttpContext`/`IActionResult`.
- Code Englisch, UI-Texte Deutsch. Bestehender Bildschirmdruck (`Print`-Actions, Drucken-Link) bleibt funktional unveraendert.
- Kein Merge, kein Push. Keine Schreibzugriffe auf `secondbrain/` (existiert im Worktree nicht; Brain pflegt der Controller im Hauptcheckout).
- Commit-Trailer: `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>` und `Claude-Session: https://claude.ai/code/session_01HqgAuqzkKHLpYraKhCi797`.

## Code-verifizierte Abweichungen von der Spec (A-Befunde, 2026-09-14 am Dev-System gemessen)

- **A1 — `msedge.exe` ist ein Launcher.** Gemessen (Edge 152.0.4191.66, `System.Diagnostics.Process`): der gestartete Prozess endet nach 200–400 ms mit Exit-Code 0; das PDF (16,7 KB, `%PDF-1.4`) schreiben Kindprozesse erst 1,3–1,8 s spaeter; danach laufen keine Kindprozesse mehr. Keine Flag-Variante aendert das (`--headless=new`, `--no-sandbox`, versionierter Pfad; `--single-process`/`--no-startup-window` erzeugen **gar kein** PDF). Spec-Schritt 4/5 („WaitForExit, dann Exit-Code pruefen") ist damit nicht baubar. **Loesung:** Der Runner wartet auf die **fertige Datei** (existiert, exklusiv oeffenbar, endet auf `%%EOF`) bis Timeout; der Exit-Code wird nur protokolliert. Timeout-Kill: die Kinder sind Waisen, `Kill(entireProcessTree)` greift nicht → alle `msedge`-Prozesse mit `StartTime` im Fenster `[launch, launch+3 s]` beenden (siehe `ponytail:`-Kommentar).
- **A2 — `--user-data-dir` je Lauf ist Pflicht.** Ohne eigenes Profil haengt sich der Aufruf an eine laufende Edge-Instanz desselben Nutzers und erzeugt kein PDF (am Dev-Rechner reproduziert). Das Profil liegt im GUID-Laufverzeichnis und wird mitgeloescht.
- **A3 — Routen-Segment braucht Attribut-Route.** Die konventionelle Route ist `{controller}/{action}/{id?}`; fuer `{hauptFa}` als Segment: `[HttpGet("FaHierarchyKommissionierListen/Pdf/{hauptFa:int}")]` bzw. `[HttpGet("FaHierarchyBeschichtung/Pdf/{hauptFa:int}")]`. `Url.Action("Pdf", new { hauptFa, target })` erzeugt daraus `/…/Pdf/104857?target=…`.
- **A4 — Keine Icon-Bibliothek im Projekt.** `_Layout.cshtml` laedt weder Bootstrap Icons noch FontAwesome. „Icon UND Text" wird mit einem 16-px-Inline-SVG (`aria-hidden="true"`) geloest; keine neue Bibliothek, kein ADR.
- **A5 — Kein Options-Pattern in `Program.cs`.** Konfiguration wird direkt ueber `IConfiguration.GetValue<T>("PdfRender:…", default)` gelesen (hausueblich).
- **A6 — Keine Tests fuer die beiden Verbraucher-Controller/-Services vorhanden** (`KommissionierListenService`/`BeschichtungsauftragService` sind konkrete Klassen). Die `Pdf`-Actions (≈15 Zeilen Glue) bekommen keinen Controller-Test; Nachweis = Service-Tests + Reflection-/Quelltext-Waechter (Route, kein `target="_blank"`) + Manual-UAT.
- **A7 — Filter-Hinweis (AK 18) lebt in `Print.cshtml` selbst** und gilt damit fuer Bildschirmdruck **und** PDF: eine View, kein Sonderpfad, AK 2 („identisch zum Bildschirmdruck") bleibt woertlich wahr. Beim Kommissionier-Druck zaehlt auch der Ziel-Filter als Filter (Teilmenge).

## Design-Entscheidungen

- **D1** `PdfRenderStatus.EdgePath` (string?, `IsAvailable => EdgePath != null`). Reihenfolge: `PdfRender:EdgePath` aus Config (gesetzt + Datei fehlt → **nicht verfuegbar** + Warnung, kein stilles Fallback), sonst Registry `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe` (Default-Wert), sonst die zwei Standardpfade unter `%ProgramFiles(x86)%`/`%ProgramFiles%`, sonst `PATH`. `Refresh()` synchron (nur Registry + `File.Exists`), einmal in `Program.cs` nach `builder.Build()`.
- **D2** `IEdgeProcessRunner.RunAsync(edgePath, htmlPath, pdfPath, profileDir, timeout, ct) → Task<bool>` (true = PDF fertig, false = Timeout). Der Runner kennt nur Prozess + Dateien; Semaphor, Laufverzeichnis, Cleanup, Logging, Exception liegen im `PdfRenderService`.
- **D3** Laufverzeichnis `<Temp>\IdealAkeWms-Pdf\<guid>\` mit `in.html`, `out.pdf`, `profile\`. Verwaist = Unterordner mit `CreationTimeUtc < UtcNow − (Timeout + 5 min)`; Loeschfehler (gesperrte Handles) nur `LogDebug`, nie werfen.
- **D4** `PdfRenderException` traegt `UserMessage`; Controller: `catch (PdfRenderException ex) { TempData["WarningMessage"] = ex.UserMessage; return RedirectToAction(nameof(Index)); }`. Filter gehen im Fehlerfall verloren — bewusst, Fehlerpfad.
- **D5** Edge-Argumente (gemessen funktionierend): `--headless --disable-gpu --no-first-run --no-default-browser-check --user-data-dir="<profile>" --no-pdf-header-footer --print-to-pdf="<pdf>" "<html>"`.
- **D6** `IRazorViewRenderer.RenderToStringAsync(ControllerContext, string viewName, object model)` — Standardmuster `ICompositeViewEngine.FindView` + `ViewContext` + `StringWriter`.
- **D7** Print-ViewModels bekommen `bool IsFiltered`; Banner-CSS `.filter-banner` = Werte von `.ambiguous-banner` (Kontrast #7a5900 auf #fff8e1 ≈ 6:1, AA).
- **D8** Version **1.39.0**, Testkapitel **TS-74** (TS-73 ist vergeben).

---

### Task 1: PdfFileNameBuilder (rein, unit-getestet) — AK 3, 10, 11

**Files:**
- Create: `IdealAkeWms/Services/PdfFileNameBuilder.cs`
- Test: `IdealAkeWms.Tests/Services/PdfFileNameBuilderTests.cs`

**Interfaces:**
- Produces: `public static string PdfFileNameBuilder.Build(string documentArt, int hauptFa, DateTime timestamp)`

- [ ] **Step 1: Failing test**

```csharp
using FluentAssertions;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class PdfFileNameBuilderTests
{
    [Theory]
    [InlineData("Kommissionierliste", 104857, "Kommissionierliste_104857_20260906-1430.pdf")]
    [InlineData("Beschichtungsauftrag", 104857, "Beschichtungsauftrag_104857_20260906-1430.pdf")]
    [InlineData("Kommissionierliste", 7, "Kommissionierliste_7_20260906-1430.pdf")]
    public void Build_FollowsSchema(string art, int hauptFa, string expected)
        => PdfFileNameBuilder.Build(art, hauptFa, new DateTime(2026, 9, 6, 14, 30, 59)).Should().Be(expected);

    [Fact]
    public void Build_PadsMonthDayHourMinute()
        => PdfFileNameBuilder.Build("Kommissionierliste", 1, new DateTime(2026, 1, 2, 3, 4, 0))
            .Should().Be("Kommissionierliste_1_20260102-0304.pdf");

    [Theory]
    [InlineData("Kommissionierliste")]
    [InlineData("Beschichtungsauftrag")]
    public void Build_RealDocumentArts_AreAsciiOnly(string art)
        => PdfFileNameBuilder.Build(art, 1, DateTime.Now).Should().MatchRegex("^[A-Za-z0-9_.-]+$");
}
```

- [ ] **Step 2: Run** `dotnet test IdealAkeWms.Tests --filter PdfFileNameBuilderTests` → FAIL (Typ fehlt).
- [ ] **Step 3: Implementation**

```csharp
using System.Globalization;

namespace IdealAkeWms.Services;

/// <summary>Dateiname eines FaHierarchy-PDF: <c>&lt;Dokumentart&gt;_&lt;HauptFA&gt;_&lt;yyyyMMdd-HHmm&gt;.pdf</c>.
/// Reine Funktion; die Dokumentart liefern die Aufrufer als ASCII-Konstante (kein Sanitizing noetig).</summary>
public static class PdfFileNameBuilder
{
    public static string Build(string documentArt, int hauptFa, DateTime timestamp)
        => $"{documentArt}_{hauptFa.ToString(CultureInfo.InvariantCulture)}_{timestamp.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture)}.pdf";
}
```

- [ ] **Step 4: Run** → PASS.
- [ ] **Step 5: Commit** `feat(pdf): PdfFileNameBuilder`

---

### Task 2: PdfRenderStatus, PdfRenderException, EdgeProcessRunner — AK 6 (Teil), 7, 13 (Teil), 15 (Teil)

**Files:**
- Create: `IdealAkeWms/Services/PdfRenderStatus.cs`
- Create: `IdealAkeWms/Services/PdfRenderException.cs`
- Create: `IdealAkeWms/Services/EdgeProcessRunner.cs` (Interface + Klasse in einer Datei, wie `PrintService.cs`)
- Test: `IdealAkeWms.Tests/Services/PdfRenderStatusTests.cs`
- Test: `IdealAkeWms.Tests/Services/EdgeProcessRunnerSmokeTests.cs`

**Interfaces:**
- Produces: `public sealed class PdfRenderStatus(IConfiguration, ILogger<PdfRenderStatus>)` mit `string? EdgePath`, `bool IsAvailable`, `void Refresh()`
- Produces: `public sealed class PdfRenderException : Exception` mit `string UserMessage`, statische Factories `NotConfigured()` und `Failed(string detail, Exception? inner = null)`
- Produces: `public interface IEdgeProcessRunner { Task<bool> RunAsync(string edgePath, string htmlPath, string pdfPath, string profileDir, TimeSpan timeout, CancellationToken ct); }`

- [ ] **Step 1: Failing tests (Status)**

```csharp
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
```

- [ ] **Step 2: Run** → FAIL.
- [ ] **Step 3: Implementation**

`IdealAkeWms/Services/PdfRenderException.cs`:
```csharp
namespace IdealAkeWms.Services;

/// <summary>Fehler der PDF-Erzeugung mit fertiger Nutzermeldung (Freigabe-Antwort 4: zwei
/// unterscheidbare Faelle, weil sie zu verschiedenen Handlungen fuehren).</summary>
public sealed class PdfRenderException : Exception
{
    public const string NotConfiguredMessage = "PDF-Erzeugung ist auf diesem Server nicht eingerichtet.";
    public const string FailedMessage = "Das PDF konnte nicht erzeugt werden. Bitte den Bildschirmdruck verwenden.";

    public string UserMessage { get; }

    private PdfRenderException(string userMessage, string detail, Exception? inner)
        : base(detail, inner) => UserMessage = userMessage;

    public static PdfRenderException NotConfigured()
        => new(NotConfiguredMessage, "msedge.exe nicht gefunden (PdfRenderStatus.IsAvailable == false)", null);

    public static PdfRenderException Failed(string detail, Exception? inner = null)
        => new(FailedMessage, detail, inner);
}
```

`IdealAkeWms/Services/PdfRenderStatus.cs`:
```csharp
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
```

`IdealAkeWms/Services/EdgeProcessRunner.cs`:
```csharp
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
```

- [ ] **Step 4: Smoke-Test des echten Runners** (laeuft nur, wenn Edge auf der Testmaschine ist — sonst gruen ohne Aussage, das steht im Kommentar):

```csharp
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
```

- [ ] **Step 5: Run** `dotnet test IdealAkeWms.Tests --filter "PdfRenderStatusTests|EdgeProcessRunnerSmokeTests"` → PASS; im Report festhalten, ob der Smoke-Test Edge gefunden hat (Ausgabe mit `--logger "console;verbosity=detailed"`).
- [ ] **Step 6: Commit** `feat(pdf): PdfRenderStatus, EdgeProcessRunner, PdfRenderException`

---

### Task 3: PdfRenderService (Semaphor, Laufverzeichnis, Cleanup, Laufzeit-Log) — AK 4, 5, 6, 8, 14 (Teil), 15, 17

**Files:**
- Create: `IdealAkeWms/Services/PdfRenderService.cs` (Interface + Klasse)
- Test: `IdealAkeWms.Tests/Services/PdfRenderServiceTests.cs`

**Interfaces:**
- Consumes: `PdfRenderStatus`, `IEdgeProcessRunner`, `PdfRenderException` (Task 2)
- Produces: `public interface IPdfRenderService { Task<byte[]> RenderHtmlToPdfAsync(string html, CancellationToken ct = default); }`
- Produces: `public static readonly string PdfRenderService.WorkRoot` (= `Path.Combine(Path.GetTempPath(), "IdealAkeWms-Pdf")`) fuer Tests/Doku

- [ ] **Step 1: Failing tests**

```csharp
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
        runner.SetupSequence(r => r.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"))
            .Returns<string, string, string, string, TimeSpan, CancellationToken>((_, _, pdf, _, _, _) => { File.WriteAllBytes(pdf, FakePdf); return Task.FromResult(true); });

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
```

- [ ] **Step 2: Run** → FAIL.
- [ ] **Step 3: Implementation**

```csharp
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
```

- [ ] **Step 4: Run** `dotnet test IdealAkeWms.Tests --filter PdfRenderServiceTests` → PASS.
- [ ] **Step 5: Commit** `feat(pdf): PdfRenderService mit Semaphor, Laufverzeichnis und zweistufigem Aufraeumen`

---

### Task 4: RazorViewRenderer, DI-Registrierung, Start-Probe, appsettings, DI-Test — AK 13 (Start-Warnung), 14

**Files:**
- Create: `IdealAkeWms/Services/RazorViewRenderer.cs` (Interface + Klasse)
- Modify: `IdealAkeWms/Program.cs` — Registrierungen bei Zeile ~147 (`AddScoped<IPrintService, PrintService>()`), Start-Probe nach Zeile 492 (nach `HierarchischeStrukturStatus…RefreshAsync()`)
- Modify: `IdealAkeWms/appsettings.json` — Abschnitt `PdfRender`
- Test: `IdealAkeWms.Tests/Services/PdfRenderServiceDiResolutionTests.cs`

**Interfaces:**
- Produces: `public interface IRazorViewRenderer { Task<string> RenderToStringAsync(ControllerContext controllerContext, string viewName, object model); }`

- [ ] **Step 1: Failing DI-Test**

```csharp
using FluentAssertions;
using IdealAkeWms.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>Waechter (Vorbild <c>BomDiResolutionTests</c>): <see cref="IPdfRenderService"/> MUSS Singleton
/// sein — bei Scoped/Transient haette jede Anfrage ihr eigenes Semaphor, die Prozess-Begrenzung waere
/// wirkungslos und saehe im Code richtig aus (AK 14).</summary>
public class PdfRenderServiceDiResolutionTests
{
    /// <summary>Spiegel der PdfRender-Zeilen in <c>IdealAkeWms/Program.cs</c>.</summary>
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();

        services.AddSingleton<PdfRenderStatus>();
        services.AddSingleton<IEdgeProcessRunner, EdgeProcessRunner>();
        services.AddSingleton<IPdfRenderService, PdfRenderService>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    [Fact]
    public void Di_PdfRenderService_IsOneInstanceAcrossScopes()
    {
        using var provider = BuildProvider();
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var a = scope1.ServiceProvider.GetRequiredService<IPdfRenderService>();
        var b = scope2.ServiceProvider.GetRequiredService<IPdfRenderService>();

        a.Should().BeOfType<PdfRenderService>();
        b.Should().BeSameAs(a, "das SemaphoreSlim muss anfrageuebergreifend wirken");
        scope2.ServiceProvider.GetRequiredService<PdfRenderStatus>()
            .Should().BeSameAs(scope1.ServiceProvider.GetRequiredService<PdfRenderStatus>());
    }

    [Fact]
    public void ProgramCs_RegistersPdfRenderServiceAsSingleton()
    {
        var programCs = File.ReadAllText(Path.Combine(FindRepoRoot(), "IdealAkeWms", "Program.cs"));
        programCs.Should().Contain("AddSingleton<IPdfRenderService, PdfRenderService>()");
        programCs.Should().Contain("AddSingleton<PdfRenderStatus>()");
        programCs.Should().Contain("AddSingleton<IEdgeProcessRunner, EdgeProcessRunner>()");
        programCs.Should().Contain("GetRequiredService<PdfRenderStatus>().Refresh()", "Start-Probe (Freigabe-Antwort 2)");
        programCs.Should().NotContain("AddScoped<IPdfRenderService");
        programCs.Should().NotContain("AddTransient<IPdfRenderService");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms.slnx"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("IdealAkeWms.slnx nicht gefunden");
    }
}
```

(Vor dem Schreiben pruefen, ob ein bestehender Quelltext-Waechter — z. B. `TableScriptScopedSelectorTests` — bereits einen Repo-Root-Finder hat; dann den wiederverwenden statt duplizieren.)

- [ ] **Step 2: Run** → FAIL.
- [ ] **Step 3: Implementation**

`IdealAkeWms/Services/RazorViewRenderer.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace IdealAkeWms.Services;

/// <summary>Rendert eine Razor-View mit Model zu einem HTML-String ausserhalb der Response — damit
/// dieselbe <c>Print.cshtml</c> an den Browser (Print-Action) UND an den PdfRenderService (Pdf-Action)
/// geht. Kein zweites Layout.</summary>
public interface IRazorViewRenderer
{
    Task<string> RenderToStringAsync(ControllerContext controllerContext, string viewName, object model);
}

public sealed class RazorViewRenderer : IRazorViewRenderer
{
    private readonly ICompositeViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;

    public RazorViewRenderer(ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider)
    {
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
    }

    public async Task<string> RenderToStringAsync(ControllerContext controllerContext, string viewName, object model)
    {
        var viewResult = _viewEngine.FindView(controllerContext, viewName, isMainPage: true);
        if (!viewResult.Success)
            throw new InvalidOperationException($"View '{viewName}' nicht gefunden. Gesucht in: {string.Join(", ", viewResult.SearchedLocations)}");

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model };
        var tempData = new TempDataDictionary(controllerContext.HttpContext, _tempDataProvider);
        await using var writer = new StringWriter();
        var viewContext = new ViewContext(controllerContext, viewResult.View, viewData, tempData, writer, new HtmlHelperOptions());
        await viewResult.View.RenderAsync(viewContext);
        return writer.ToString();
    }
}
```

`Program.cs` — direkt nach `builder.Services.AddScoped<IPrintService, PrintService>();`:
```csharp
// PDF-Erzeugung (Spec 2026-08-06): Status + Renderdienst SINGLETON — das SemaphoreSlim im Dienst
// begrenzt die Edge-Prozesse nur anfrageuebergreifend, wenn es genau eine Instanz gibt.
builder.Services.AddSingleton<PdfRenderStatus>();
builder.Services.AddSingleton<IEdgeProcessRunner, EdgeProcessRunner>();
builder.Services.AddSingleton<IPdfRenderService, PdfRenderService>();
builder.Services.AddScoped<IRazorViewRenderer, RazorViewRenderer>();
```

`Program.cs` — direkt nach der `HierarchischeStrukturStatus`-Start-Probe (Zeile 492):
```csharp
// msedge.exe einmal beim Start suchen (Freigabe-Antwort 2): fehlt Edge, bleibt der PDF-Knopf weg
// und das Log traegt die Warnung — der Anwender laeuft nicht in eine Wand.
app.Services.GetRequiredService<PdfRenderStatus>().Refresh();
```

`appsettings.json` — nach `"Security"`:
```json
  "PdfRender": {
    "EdgePath": "",
    "TimeoutSeconds": 30,
    "MaxConcurrent": 2
  }
```
(`EdgePath` leer = automatische Ermittlung; `IdealAkeWms/Services` ist via `using IdealAkeWms.Services;` in `Program.cs` schon eingebunden — pruefen.)

- [ ] **Step 4: Run** `dotnet build IdealAkeWms.slnx` + `dotnet test IdealAkeWms.Tests --filter PdfRenderServiceDiResolutionTests` → PASS. Zusaetzlich `dotnet run --project IdealAkeWms` kurz starten (max. 20 s, dann abbrechen) und die Log-Zeile `PDF-Erzeugung: msedge.exe gefunden: …` im Report zitieren — das ist der Start-Probe-Beweis.
- [ ] **Step 5: Commit** `feat(pdf): RazorViewRenderer, DI-Registrierung (Singleton) und Start-Probe`

---

### Task 5: Pdf-Actions + Filter-Hinweis in Print.cshtml — AK 1, 2, 4, 6, 9, 12, 18

**Files:**
- Modify: `IdealAkeWms/Controllers/FaHierarchyKommissionierListenController.cs` (Ctor + neue Action + `Print` setzt `IsFiltered`)
- Modify: `IdealAkeWms/Controllers/FaHierarchyBeschichtungController.cs` (analog)
- Modify: `IdealAkeWms/Models/ViewModels/FaHierarchyKommissionierGruppeViewModel.cs` (`FaHierarchyKommissionierPrintViewModel.IsFiltered`)
- Modify: `IdealAkeWms/Models/ViewModels/FaHierarchyBeschichtungViewModels.cs` (`FaHierarchyBeschichtungPrintViewModel.IsFiltered`)
- Modify: `IdealAkeWms/Views/FaHierarchyKommissionierListen/Print.cshtml` (Banner + CSS)
- Modify: `IdealAkeWms/Views/FaHierarchyBeschichtung/Print.cshtml` (Banner)
- Test: `IdealAkeWms.Tests/Controllers/FaHierarchyPdfActionGuardTests.cs` (Reflection: Attribut-Route + Filter geerbt)

**Interfaces:**
- Consumes: `IRazorViewRenderer`, `IPdfRenderService`, `PdfRenderException`, `PdfFileNameBuilder`
- Produces: Actions `GET /FaHierarchyKommissionierListen/Pdf/{hauptFa:int}?target=&colf_*=` und `GET /FaHierarchyBeschichtung/Pdf/{hauptFa:int}?colf_*=`

- [ ] **Step 1: Failing Reflection-Test**

```csharp
using System.Reflection;
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Filters;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>Die Pdf-Actions haben keine Controller-Tests (Verbraucher-Services sind konkrete Klassen
/// ohne Testfabrik). Dieser Waechter sichert die zwei Dinge, die beim Kopieren still verloren gehen:
/// die Attribut-Route mit <c>{hauptFa}</c> als Segment (Freigabe-Antwort 7) und dass der PDF-Download
/// ueber die Class-Level-Filter geschuetzt bleibt (AK 9).</summary>
public class FaHierarchyPdfActionGuardTests
{
    [Theory]
    [InlineData(typeof(FaHierarchyKommissionierListenController), "FaHierarchyKommissionierListen/Pdf/{hauptFa:int}")]
    [InlineData(typeof(FaHierarchyBeschichtungController), "FaHierarchyBeschichtung/Pdf/{hauptFa:int}")]
    public void PdfAction_HasRouteWithHauptFaSegment(Type controller, string template)
    {
        var action = controller.GetMethod("Pdf", BindingFlags.Public | BindingFlags.Instance);
        action.Should().NotBeNull();
        action!.GetCustomAttribute<HttpGetAttribute>()!.Template.Should().Be(template);
    }

    [Fact]
    public void Controllers_KeepClassLevelAccessAndFeatureFilters()
    {
        typeof(FaHierarchyKommissionierListenController).GetCustomAttribute<RequireLagerProcessingAccessAttribute>().Should().NotBeNull();
        typeof(FaHierarchyKommissionierListenController).GetCustomAttribute<RequireFaHierarchyKommissionierlistenAktivAttribute>().Should().NotBeNull();
        typeof(FaHierarchyBeschichtungController).GetCustomAttribute<RequireBeschichtungsauftragAccessAttribute>().Should().NotBeNull();
        typeof(FaHierarchyBeschichtungController).GetCustomAttribute<RequireFaHierarchyBeschichtungAktivAttribute>().Should().NotBeNull();
    }
}
```

- [ ] **Step 2: Run** → FAIL (`Pdf` fehlt).
- [ ] **Step 3: Implementation**

ViewModels — je Print-VM ergaenzen:
```csharp
    /// <summary>Mindestens ein Spaltenfilter (bzw. Ziel-Filter) aktiv: das Dokument zeigt eine Teilmenge
    /// und muss das im Kopf ausweisen (AK 18) — am Bildschirm sieht man die Filterzeile, im Druck/PDF nicht.</summary>
    public bool IsFiltered { get; init; }
```

`FaHierarchyKommissionierListenController` — Ctor um `IRazorViewRenderer viewRenderer, IPdfRenderService pdfRenderService` erweitern (Felder `_viewRenderer`, `_pdfRenderService`), in `Print` das VM um `IsFiltered = ColumnFilterHelper.HasAny(columnFilters) || !string.IsNullOrEmpty(target)` ergaenzen, neue Action:
```csharp
    /// <summary>PDF-Download fuer GENAU EINE HauptFA-Gruppe (Spec 2026-08-06): dieselbe
    /// <c>Print.cshtml</c>, dieselben Query-Filter (<c>target</c> + <c>colf_*</c>) wie der
    /// Bildschirmdruck, aber auf eine Gruppe reduziert. <c>hauptFa</c> als Routen-Segment
    /// (Freigabe-Antwort 7). Fehler werden als <c>WarningMessage</c> mit Rueckfallweg gemeldet.</summary>
    [HttpGet("FaHierarchyKommissionierListen/Pdf/{hauptFa:int}")]
    public async Task<IActionResult> Pdf(int hauptFa, string? target)
    {
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var result = await _service.BuildAsync(target, columnFilters, 1, int.MaxValue, paginate: false);
        var group = result.Groups.FirstOrDefault(g => g.HauptFA == hauptFa);
        if (group is null) return NotFound();

        var vm = new FaHierarchyKommissionierPrintViewModel
        {
            Groups = new List<FaHierarchyListGroupViewModel> { group },
            Barcodes = new Dictionary<int, string> { [hauptFa] = _barcode.EncodeCode128Base64(hauptFa.ToString(CultureInfo.InvariantCulture)) },
            SelectedTarget = target,
            IsFiltered = ColumnFilterHelper.HasAny(columnFilters) || !string.IsNullOrEmpty(target)
        };

        try
        {
            var html = await _viewRenderer.RenderToStringAsync(ControllerContext, "Print", vm);
            var pdfBytes = await _pdfRenderService.RenderHtmlToPdfAsync(html, HttpContext.RequestAborted);
            return File(pdfBytes, "application/pdf", PdfFileNameBuilder.Build("Kommissionierliste", hauptFa, DateTime.Now));
        }
        catch (PdfRenderException ex)
        {
            TempData["WarningMessage"] = ex.UserMessage;
            return RedirectToAction(nameof(Index));
        }
    }
```

`FaHierarchyBeschichtungController` — analog: Route `FaHierarchyBeschichtung/Pdf/{hauptFa:int}`, Signatur `Pdf(int hauptFa)`, `_service.BuildAsync(columnFilters, 1, int.MaxValue, paginate: false)`, VM `FaHierarchyBeschichtungPrintViewModel` mit `IsFiltered = ColumnFilterHelper.HasAny(columnFilters)`, Dokumentart `"Beschichtungsauftrag"`; `Print` setzt ebenfalls `IsFiltered = ColumnFilterHelper.HasAny(columnFilters)`.

Beide `Print.cshtml` — im `<style>` (Kommissionier-View hat noch keine `.ambiguous-banner`, dort dieselben Werte neu):
```css
        .filter-banner { border: 1px solid #b8860b; background: #fff8e1; color: #7a5900; padding: 8px 10px; margin-bottom: 8px; font-size: 10px; }
```
und direkt nach `<div class="fa-group">` (innerhalb, vor `.group-head`), Kommissionier:
```cshtml
                @if (Model.IsFiltered)
                {
                    <div class="filter-banner">
                        <strong>Gefilterte Ansicht</strong> — dieses Dokument zeigt nur die gefilterten
                        Positionen dieses HauptFA, nicht die vollständige Kommissionierliste.
                        @if (!string.IsNullOrEmpty(Model.SelectedTarget)) { <text>Kommissionier-Ziel: @Model.SelectedTarget.</text> }
                    </div>
                }
```
Beschichtung: gleicher Block ohne Ziel-Satz, Text „… nicht den vollständigen Beschichtungsauftrag."

- [ ] **Step 4: Run** `dotnet build IdealAkeWms.slnx` + `dotnet test IdealAkeWms.Tests --filter FaHierarchyPdfActionGuardTests` → PASS.
- [ ] **Step 5: Commit** `feat(pdf): Pdf-Actions (eine Gruppe, Routen-Segment) und Filter-Hinweis im Druckkopf`

---

### Task 6: PDF-Knopf in beiden Index-Views (nur wenn Edge da) + Quelltext-Waechter — AK 1, 13, 16

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/FaHierarchyKommissionierGruppeViewModel.cs` (`FaHierarchyKommissionierListViewModel.PdfVerfuegbar`)
- Modify: `IdealAkeWms/Models/ViewModels/FaHierarchyBeschichtungViewModels.cs` (`FaHierarchyBeschichtungListViewModel.PdfVerfuegbar`)
- Modify: beide Controller `Index` (Ctor um `PdfRenderStatus pdfRenderStatus`, `PdfVerfuegbar = _pdfRenderStatus.IsAvailable`)
- Modify: `IdealAkeWms/Views/FaHierarchyKommissionierListen/Index.cshtml` (Zeile ~91)
- Modify: `IdealAkeWms/Views/FaHierarchyBeschichtung/Index.cshtml` (Zeile ~64)
- Test: `IdealAkeWms.Tests/Helpers/FaHierarchyPdfButtonSourceTests.cs`

- [ ] **Step 1: Failing Quelltext-Waechter** (Muster `TableScriptScopedSelectorTests`; deren Repo-Root-Finder wiederverwenden, falls vorhanden):

```csharp
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>Kein JS/View-Testharness im Projekt — Quelltext-Waechter fuer die zwei Dinge, die beim
/// Kopieren des Drucken-Links still schiefgehen: <c>target="_blank"</c> am PDF-Knopf (AK 16) und ein
/// PDF-Knopf ohne Sichtbarkeits-Bedingung (AK 13).</summary>
public class FaHierarchyPdfButtonSourceTests
{
    [Theory]
    [InlineData("Views/FaHierarchyKommissionierListen/Index.cshtml")]
    [InlineData("Views/FaHierarchyBeschichtung/Index.cshtml")]
    public void PdfButton_IsGatedAndDownloadsDirectly(string relativePath)
    {
        var src = File.ReadAllText(Path.Combine(FindRepoRoot(), "IdealAkeWms", relativePath));

        var anchors = Regex.Matches(src, "<a [^>]*Url\\.Action\\(\"Pdf\"[^>]*>", RegexOptions.Singleline).Select(m => m.Value).ToList();
        anchors.Should().HaveCount(1, "genau ein PDF-Knopf je Gruppen-Kopfzeile");
        anchors[0].Should().NotContain("target=", "direkter Download, kein neuer Tab — nicht vom Drucken-Link erben (AK 16)");
        anchors[0].Should().Contain("Context.Request.QueryString", "target/colf_* muessen mit ins PDF (Klarstellung zu Antwort 7)");
        src.Should().Contain("@if (Model.PdfVerfuegbar)", "ohne Edge erscheint der Knopf gar nicht (AK 13)");
        src.Should().MatchRegex(">\\s*PDF\\s*</a>", "Icon UND Text — der Text ist Pflicht");
    }

    private static string FindRepoRoot() { /* wie in PdfRenderServiceDiResolutionTests bzw. bestehendem Helfer */ }
}
```

- [ ] **Step 2: Run** → FAIL.
- [ ] **Step 3: Implementation**

List-VMs je:
```csharp
    /// <summary>Edge beim Start gefunden (<see cref="Services.PdfRenderStatus"/>): nur dann erscheint der
    /// PDF-Knopf an der Gruppen-Kopfzeile (Freigabe-Antwort 2).</summary>
    public bool PdfVerfuegbar { get; init; }
```

Beide `Index.cshtml` — im Kopfblock der View eine URL-Basis:
```cshtml
    // PDF je Gruppe: dieselben Query-Parameter wie der Bildschirmdruck (target + colf_*); hauptFa ist Routen-Segment.
    var pdfQuery = Context.Request.QueryString.ToString();
```
und in der Gruppen-Kopfzeile direkt nach `<strong>HauptFA @group.HauptFA</strong>`:
```cshtml
                            @if (Model.PdfVerfuegbar)
                            {
                                <a href="@(Url.Action("Pdf", new { hauptFa = group.HauptFA }) + pdfQuery)"
                                   class="btn btn-sm btn-outline-secondary ms-2 py-0"
                                   title="PDF dieser HauptFA-Gruppe herunterladen">
                                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true" class="me-1" style="vertical-align: -2px;"><path d="M14 4.5V14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2h5.5zM9.5 3A1.5 1.5 0 0 0 11 4.5h2V14a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h5.5z"/><path d="M4.603 12.087a.8.8 0 0 1-.438-.42c-.195-.388-.13-.776.08-1.102.198-.307.526-.568.897-.787a7.7 7.7 0 0 1 1.482-.645 20 20 0 0 0 1.062-2.227 7.3 7.3 0 0 1-.43-1.295c-.086-.4-.119-.796-.046-1.136.075-.354.274-.672.65-.823.192-.077.4-.12.602-.077a.7.7 0 0 1 .477.365c.088.164.12.356.127.538.007.187-.012.395-.047.614-.084.51-.27 1.134-.52 1.794a11 11 0 0 0 .98 1.686 5.8 5.8 0 0 1 1.334.05c.364.065.734.195.96.465.12.144.193.32.2.518.007.192-.047.382-.138.563a1.04 1.04 0 0 1-.354.416.86.86 0 0 1-.51.138c-.331-.014-.654-.196-.933-.417a5.7 5.7 0 0 1-.911-.95 11.6 11.6 0 0 0-1.997.406 11.3 11.3 0 0 1-1.021 1.51c-.29.35-.608.655-.926.787a.8.8 0 0 1-.58.029"/></svg>PDF
                                </a>
                            }
```
(Kommissionier: `target` steckt bereits in `pdfQuery`; Beschichtung identisch ohne Ziel.) Kein `target="_blank"`. Kontrast: `btn-outline-secondary` auf `table-active` — pruefen, dass Text + Rahmen sichtbar bleiben (Bootstrap-Standard, AA fuer Bedienelemente 3:1).

Controller `Index` je: Ctor-Parameter `PdfRenderStatus pdfRenderStatus` → Feld; VM `PdfVerfuegbar = _pdfRenderStatus.IsAvailable`.

- [ ] **Step 4: Run** `dotnet build` + `dotnet test IdealAkeWms.Tests --filter "FaHierarchyPdfButtonSourceTests|FaHierarchyPdfActionGuardTests"` → PASS.
- [ ] **Step 5: Commit** `feat(pdf): PDF-Knopf je HauptFA-Gruppe, nur wenn Edge verfuegbar`

---

### Task 7: Version 1.39.0, Anwender-Changelog, README, Testszenarien TS-74

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`, `IDEALAKEWMSService/AppVersion.cs` → `"1.39.0"`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml` — neue Karte **vor** v1.38.0 (gleiche Struktur, Datum 14.09.2026)
- Modify: `README.md` — unter „Voraussetzungen" Punkt `Microsoft Edge auf dem Web-Server (PDF-Erzeugung der IDEAL-Druckdokumente; fehlt er, bleibt der PDF-Knopf weg)`; neuer Abschnitt `## PDF-Erzeugung (appsettings.json)` nach „AppSettings" mit Tabelle `PdfRender:EdgePath` / `TimeoutSeconds` / `MaxConcurrent` + Temp-Verzeichnis `%TEMP%\IdealAkeWms-Pdf` + Log-Zeilen (Start-Warnung, Laufzeit)
- Modify: `docs/TESTSZENARIEN.md` — Kapitel `## TS-74 — IDEAL: PDF-Erzeugung fuer FaHierarchy-Druckdokumente (v1.39.0)` am Ende, Muster TS-73 (Bezug, Pflicht-Vorbedingungen, „Automatisiert abgedeckt", dann Szenarien mit Vorbedingung/Schritte/Erwartung/Negativfall, Manual-UAT-Kennzeichnung)

- [ ] **Step 1: Versionen + Changelog-Karte**

Changelog-Text (Anwendersprache, wie v1.38.0):
```
<h6>PDF je HauptFA für Kommissionierliste und Beschichtungsauftrag (IDEAL)</h6>
<ul>
  <li><strong>PDF-Knopf an jeder HauptFA-Gruppe:</strong> In den Listen „Kommissionierlisten (IDEAL)" und „Beschichtungsauftrag (IDEAL)" lädt der Knopf „PDF" in der Gruppen-Kopfzeile ein PDF genau dieser HauptFA herunter — mailbar und ablegbar. Inhalt und Aussehen entsprechen dem Bildschirmdruck derselben Gruppe. Dateiname: <code>Kommissionierliste_&lt;HauptFA&gt;_&lt;Datum-Uhrzeit&gt;.pdf</code> bzw. <code>Beschichtungsauftrag_…</code>.</li>
  <li><strong>Gefilterte Ansicht wird ausgewiesen:</strong> Ist ein Spalten- oder Ziel-Filter aktiv, tragen Druck und PDF im Kopf den Hinweis „Gefilterte Ansicht" — eine Teilmenge sieht nicht mehr wie das Ganze aus.</li>
  <li><strong>Voraussetzung:</strong> Microsoft Edge muss auf dem Web-Server installiert sein. Fehlt er, erscheint der PDF-Knopf gar nicht; der Bildschirmdruck („Drucken") bleibt wie bisher.</li>
</ul>
```

- [ ] **Step 2: TS-74 schreiben** — Szenarien 1:1 auf AK 1–18 gemappt:
  - TS-74.1 Happy Path Kommissionierliste (AK 1, 2, 3) — **(Manual-UAT)**
  - TS-74.2 Happy Path Beschichtungsauftrag inkl. Kombigeraet-Banner (AK 2, 3) — **(Manual-UAT)**
  - TS-74.3 Dateiname (AK 3, 10, 11) — automatisiert `PdfFileNameBuilderTests`, manuell Sichtpruefung des Downloads
  - TS-74.4 Temp-Aufraeumen inkl. verwaiste Datei (AK 5, 17): Ordner `%TEMP%\IdealAkeWms-Pdf` vorher/nachher; von Hand einen Unterordner anlegen, dessen Erstellzeit > 35 min zurueckliegt (oder per PowerShell `(Get-Item …).CreationTimeUtc = …`), PDF erzeugen, Ordner weg — **(Manual-UAT)**; automatisiert `PdfRenderServiceTests`
  - TS-74.5 Parallelaufruf zwei Tabs (AK 8) — **(Manual-UAT)**
  - TS-74.6 Edge fehlt beim Start (AK 13): `PdfRender:EdgePath` auf einen falschen Pfad setzen, App neu starten → Log-Warnung, Knopf in beiden Listen weg, Drucken-Link bleibt — **(Manual-UAT)**
  - TS-74.7 Timeout (AK 7): `PdfRender:TimeoutSeconds = 1` und ein grosses Dokument bzw. Edge-Pfad auf eine EXE, die nichts schreibt (z. B. `notepad.exe` mit `EdgePath`) → Warnmeldung mit Rueckfallweg, keine `msedge`-Zombies im Task-Manager — **(Manual-UAT)**
  - TS-74.8 Zugriffsschutz (AK 9): Anwender ohne Rolle ruft `/FaHierarchyBeschichtung/Pdf/<HauptFA>` direkt auf → gleicher Zugriffsfehler wie `Index` — **(Manual-UAT)**; automatisiert `FaHierarchyPdfActionGuardTests`
  - TS-74.9 Regression Bildschirmdruck (AK 12): Drucken-Link oeffnet weiterhin neuen Tab mit allen Gruppen — **(Manual-UAT)**
  - TS-74.10 Direkter Download, kein neuer Tab (AK 16) — **(Manual-UAT)**; automatisiert `FaHierarchyPdfButtonSourceTests`
  - TS-74.11 Filter-Hinweis im Kopf (AK 18): Spaltenfilter setzen, PDF und Bildschirmdruck zeigen „Gefilterte Ansicht"; ohne Filter kein Hinweis — **(Manual-UAT)**
  - TS-74.12 Laufzeit-Log (AK 15): Log enthaelt `PDF erzeugt: … Bytes in … ms` — **(Manual-UAT)**
  - TS-74.13 Singleton-Begrenzung (AK 14): automatisiert `PdfRenderServiceDiResolutionTests`; manuell: `MaxConcurrent = 1`, zwei Tabs, der zweite Download kommt nach dem ersten (keine Fehlermeldung „zu viele Anfragen")
  - TS-74.14 Unbekannte HauptFA: `/…/Pdf/999999999` → HTTP 404 — **(Manual-UAT)**

- [ ] **Step 3: Run** `dotnet build IdealAkeWms.slnx` → 0 Fehler; `dotnet test` (beide Projekte) gruen; Zahlen in den Report.
- [ ] **Step 4: Commit** `chore: v1.39.0 — Anwender-Changelog, README (Edge-Voraussetzung, PdfRender-Konfiguration), Testszenarien TS-74`

---

## Self-Review (Spec-Abdeckung)

| AK | Task |
|---|---|
| 1, 2 | 5, 6 (Manual-UAT TS-74.1/2) |
| 3, 10, 11 | 1 |
| 4 | 3 (keine `HttpContext`/`IActionResult`-Abhaengigkeit — Review-Kriterium) |
| 5, 17 | 3 |
| 6 | 2, 3, 5 (Exception → WarningMessage) |
| 7 | 2 (Timeout-Kill), Manual-UAT TS-74.7 |
| 8 | 3 (GUID-Laufverzeichnis) |
| 9 | 5 (Class-Level-Filter, Reflection-Waechter) |
| 12 | Print-Actions unveraendert bis auf `IsFiltered` (TS-74.9) |
| 13 | 2, 4 (Start-Probe + Warnung), 6 (`@if`) |
| 14 | 4 (DI-Test) |
| 15 | 3 (Stopwatch-Log) |
| 16 | 6 (kein `target`, Quelltext-Waechter) |
| 18 | 5 (Banner in `Print.cshtml`) |

Brain (Hauptcheckout, durch den Controller, nicht durch Tasks): Spec-Status, Aufgaben-Notiz, `codebase/services.md`, Buendel-Deploy-Notiz (Edge-Vorbedingung), `feature-map.md`, `tests/testszenarien-index.md`, Brain-Changelog v1.39.0, `fallstricke.md` (Launcher-Verhalten von `msedge.exe` — A1), Backlog-Punkt „Vormontage-Druckansicht" (Antwort 9).

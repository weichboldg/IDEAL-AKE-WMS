# SyncLog-Pflicht fuer alle Sync-Services Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Alle 8 echten Sync-Services (= 9 logische Service-Namen) schreiben einheitlich ueber einen neuen `ISyncLogger` ins `SyncLogs`-Schema: Start (Info), Events (Info/Warning/Error), Ende (Info bei Erfolg / Error bei Failure) mit Counts.

**Architecture:** Neuer Singleton-Service `ISyncLogger` im Web-Projekt, der per `BeginRunAsync` einen `ISyncRun` (IAsyncDisposable) zurueckgibt. Jeder Logger-Aufruf schreibt einen SyncLog-Eintrag ueber einen frischen `ApplicationDbContext` aus `IDbContextFactory<ApplicationDbContext>` — entkoppelt von Sync-Transaktionen. Try/Catch um jeden Insert mit Serilog-Fallback; SyncLog-Schreibfehler crashen Syncs nie. 2 bestehende Services (Lagerplatz/Lagerbestand) werden migriert, 6 neue integriert.

**Tech Stack:** .NET 10 / EF Core 10 / SQL Server, Serilog, xUnit + FluentAssertions, vorhandener `ApplicationDbContext` (kein Schema-Change).

**Spec:** [secondbrain/docs/superpowers/specs/2026-05-26-synclog-pflicht-alle-syncs-design.md](../specs/2026-05-26-synclog-pflicht-alle-syncs-design.md)

---

## File Structure

**Neu (Web-Projekt):**
- `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs` — Service-Namen-Konstanten
- `IdealAkeWms/Services/SyncLogger/ISyncLogger.cs` — Logger-Interface
- `IdealAkeWms/Services/SyncLogger/ISyncRun.cs` — Run-Interface
- `IdealAkeWms/Services/SyncLogger/SyncLogger.cs` — Singleton-Implementierung
- `IdealAkeWms/Services/SyncLogger/SyncRun.cs` — IAsyncDisposable-Implementierung

**Neu (Test):**
- `IdealAkeWms.Tests/Helpers/FakeSyncLogger.cs` — Recording-Test-Helper
- `IdealAkeWms.Tests/Services/SyncLoggerTests.cs` — Unit-Tests fuer Logger

**Modifiziert (Web):**
- `IdealAkeWms/Program.cs:17-20, 75` — AddDbContextFactory + AddSingleton<ISyncLogger>
- `IdealAkeWms/Controllers/SyncLogController.cs:13` — KnownServices → SyncLogServices.All
- `IdealAkeWms/AppVersion.cs` — 1.15.0
- `IdealAkeWms/Views/Help/Changelog.cshtml` — v1.15.0-Eintrag
- `IdealAkeWms/Views/Help/Index.cshtml` — Sync-Protokoll-Hinweis

**Modifiziert (Service):**
- `IDEALAKEWMSService/Program.cs:30-33, 41` — AddDbContextFactory + AddSingleton<ISyncLogger>
- `IDEALAKEWMSService/AppVersion.cs` — 1.15.0
- `IDEALAKEWMSService/Services/LagerplatzSyncService.cs` — Migration
- `IDEALAKEWMSService/Services/LagerbestandSyncService.cs` — Migration
- `IDEALAKEWMSService/Services/HolidaySyncService.cs` — Integration
- `IDEALAKEWMSService/Services/BomCacheSyncService.cs` — Integration
- `IDEALAKEWMSService/Services/OseonSyncService.cs` — Integration
- `IDEALAKEWMSService/Services/EnaioDmsSyncService.cs` — Integration
- `IDEALAKEWMSService/Services/CoatingDetectionService.cs` — Integration
- `IDEALAKEWMSService/Services/SageImportService.cs` — Integration (2 logische Runs)

**Modifiziert (Doku):**
- `docs/TESTSZENARIEN.md` — 8 neue Szenarien
- `PROJECT_STATUS.md` — Hauptfunktionen + Fortschritts-Sektion
- `CLAUDE.md` — Bekannte Fallstricke

---

## Task 0: Pre-flight — Repo-weite Suche nach hartcodierten Service-Strings

**Files:** keine Aenderung — diagnostische Suche.

- [ ] **Step 1: Grep nach den 9 Service-Strings**

```bash
git grep -n -E '"(Lagerplatz|Lagerbestand|BomCache|OseonTracking|EnaioDms|Holiday|CoatingDetection|ProductionOrder|Article)"' -- '*.cs' '*.cshtml'
```

Erwartet: Liste alle Stellen, an denen diese Strings als Literal stehen. Bekannte Treffer:
- `IDEALAKEWMSService/Services/LagerplatzSyncService.cs:11` — `const string ServiceName = "Lagerplatz"`
- `IDEALAKEWMSService/Services/LagerbestandSyncService.cs:11` — `const string ServiceName = "Lagerbestand"`
- `IdealAkeWms/Controllers/SyncLogController.cs:13` — `KnownServices = new[] { ... }`
- `IdealAkeWms.Tests/Controllers/SyncLogControllerTests.cs` — Test-Daten `"Lagerplatz"`, `"OseonTracking"`

Aktion: Wenn weitere Treffer auftauchen (z.B. Repos, andere Tests, Reports), in den Migrations/Integrations-Tasks mappen. Diese Pre-flight produziert nur eine Liste, keinen Commit.

- [ ] **Step 2: Bestaetigen, dass keine ueberraschenden Treffer existieren**

Wenn Treffer ausserhalb der erwarteten Liste auftauchen → in den jeweiligen Task aufnehmen, der die Datei sowieso anfasst, oder einen Mini-Task am Ende anhaengen. Sonst weiter zu Task 1.

---

## Task 1: Service-Namen-Konstanten

**Files:**
- Create: `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs`

- [ ] **Step 1: Datei anlegen**

```csharp
namespace IdealAkeWms.Services.SyncLogger;

/// <summary>
/// Single Source of Truth fuer die Service-Namen, die in der <c>SyncLogs.Service</c>-Spalte
/// erscheinen duerfen. Wird von <see cref="ISyncLogger.BeginRunAsync"/> verwendet und
/// vom <c>SyncLogController</c> als Dropdown-Quelle (<c>KnownServices</c>).
/// </summary>
public static class SyncLogServices
{
    public const string Lagerplatz = "Lagerplatz";
    public const string Lagerbestand = "Lagerbestand";
    public const string BomCache = "BomCache";
    public const string OseonTracking = "OseonTracking";
    public const string EnaioDms = "EnaioDms";
    public const string Holiday = "Holiday";
    public const string CoatingDetection = "CoatingDetection";
    public const string ProductionOrder = "ProductionOrder";  // SageImport-Teil 1
    public const string Article = "Article";                  // SageImport-Teil 2

    public static IReadOnlyList<string> All { get; } = new[]
    {
        Lagerplatz, Lagerbestand, BomCache, OseonTracking, EnaioDms,
        Holiday, CoatingDetection, ProductionOrder, Article,
    };
}
```

- [ ] **Step 2: Build verifizieren**

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
```

Erwartet: `Buildvorgang wurde erfolgreich ausgefuehrt`, 0 Fehler.

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms/Services/SyncLogger/SyncLogServices.cs
git commit -m "feat(synclog): add SyncLogServices constants"
```

---

## Task 2: Logger- und Run-Interfaces

**Files:**
- Create: `IdealAkeWms/Services/SyncLogger/ISyncLogger.cs`
- Create: `IdealAkeWms/Services/SyncLogger/ISyncRun.cs`

- [ ] **Step 1: `ISyncLogger.cs` anlegen**

```csharp
namespace IdealAkeWms.Services.SyncLogger;

/// <summary>
/// Einheitlicher Einstiegspunkt fuer das Sync-Audit-Logging.
/// Jeder Sync-Run startet mit <see cref="BeginRunAsync"/> und endet mit
/// <see cref="ISyncRun.FinishSuccessAsync"/> bzw. <see cref="ISyncRun.FinishFailedAsync"/>.
/// </summary>
public interface ISyncLogger
{
    /// <summary>
    /// Startet einen neuen Run, schreibt einen Start-Eintrag (Info) und liefert das Run-Handle zurueck.
    /// </summary>
    /// <param name="serviceName">Konstante aus <see cref="SyncLogServices"/>.</param>
    Task<ISyncRun> BeginRunAsync(string serviceName, CancellationToken ct = default);
}
```

- [ ] **Step 2: `ISyncRun.cs` anlegen**

```csharp
namespace IdealAkeWms.Services.SyncLogger;

/// <summary>
/// Handle fuer einen laufenden Sync-Run. Pflicht: genau einer der beiden Finish-Aufrufe
/// pro Run. Ein zweiter Finish-Aufruf wird idempotent ignoriert (kein Throw, kein
/// zusaetzlicher Eintrag). Wenn weder Finish noch DisposeAsync den Run sauber schliessen,
/// schreibt DisposeAsync einen Warning-Eintrag "Run wurde unerwartet beendet".
/// </summary>
public interface ISyncRun : IAsyncDisposable
{
    Task LogInfoAsync(string message, string? reference = null, CancellationToken ct = default);
    Task LogWarningAsync(string message, string? reference = null, CancellationToken ct = default);
    Task LogErrorAsync(string message, string? reference = null, CancellationToken ct = default);

    /// <summary>
    /// Sauberer Abschluss. <paramref name="counts"/>-Eintraege werden als
    /// <c>"key=value, key=value"</c> in die End-Message gerendert (siehe Spec §5.3).
    /// </summary>
    Task FinishSuccessAsync(IReadOnlyDictionary<string, int>? counts = null,
                            string? messageSuffix = null,
                            CancellationToken ct = default);

    /// <summary>
    /// Fehler-Abschluss. <paramref name="errorMessage"/> wird in die End-Message geschrieben,
    /// <paramref name="counts"/> mit demselben Formatter wie bei Success angefuegt.
    /// </summary>
    Task FinishFailedAsync(string errorMessage,
                           IReadOnlyDictionary<string, int>? counts = null,
                           CancellationToken ct = default);
}
```

- [ ] **Step 3: Build verifizieren**

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
```

Erwartet: 0 Fehler.

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Services/SyncLogger/ISyncLogger.cs IdealAkeWms/Services/SyncLogger/ISyncRun.cs
git commit -m "feat(synclog): add ISyncLogger and ISyncRun interfaces"
```

---

## Task 3: SyncLogger + SyncRun Implementierung (TDD)

**Files:**
- Create: `IdealAkeWms.Tests/Services/SyncLoggerTests.cs`
- Create: `IdealAkeWms/Services/SyncLogger/SyncLogger.cs`
- Create: `IdealAkeWms/Services/SyncLogger/SyncRun.cs`

- [ ] **Step 1: Erster failing Test — BeginRunAsync schreibt Start-Eintrag**

Datei `IdealAkeWms.Tests/Services/SyncLoggerTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdealAkeWms.Tests.Services;

public class SyncLoggerTests
{
    [Fact]
    public async Task BeginRunAsync_writes_start_entry()
    {
        var factory = new TestApplicationDbContextFactory();
        var logger = new SyncLogger(factory, NullLogger<SyncLogger>.Instance);

        await using var run = await logger.BeginRunAsync(SyncLogServices.BomCache);

        using var verify = factory.CreateDbContext();
        var entries = await verify.SyncLogs.OrderBy(e => e.Id).ToListAsync();
        entries.Should().HaveCount(1);
        entries[0].Service.Should().Be(SyncLogServices.BomCache);
        entries[0].Level.Should().Be(SyncLogLevel.Info);
        entries[0].Message.Should().Be("Run gestartet");
        entries[0].Reference.Should().BeNull();
    }
}
```

- [ ] **Step 2: TestApplicationDbContextFactory-Helper anlegen**

Datei `IdealAkeWms.Tests/Helpers/TestApplicationDbContextFactory.cs` (neu):

```csharp
using IdealAkeWms.Data;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>
/// In-Memory <see cref="IDbContextFactory{ApplicationDbContext}"/>-Implementierung fuer Tests.
/// Alle Contexts teilen dieselbe InMemory-DB, sodass Inserts aus dem SUT in einem
/// separaten Verifier-Context wieder gelesen werden koennen.
/// </summary>
public sealed class TestApplicationDbContextFactory : IDbContextFactory<ApplicationDbContext>, IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public TestApplicationDbContextFactory([CallerMemberName] string dbName = "")
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName + Guid.NewGuid())
            .Options;
    }

    public ApplicationDbContext CreateDbContext() => new TestApplicationDbContext(_options);

    public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken ct = default)
        => Task.FromResult<ApplicationDbContext>(new TestApplicationDbContext(_options));

    public void Dispose() { /* InMemory hat kein File-Handle */ }
}
```

- [ ] **Step 3: Test laufen lassen — soll mit "SyncLogger nicht gefunden" failen**

```bash
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-restore -c Debug --nologo --filter "FullyQualifiedName~SyncLoggerTests"
```

Erwartet: Build-Fehler `CS0246: Der Typ- oder Namespacename "SyncLogger" wurde nicht gefunden`.

- [ ] **Step 4: `SyncLogger.cs` und `SyncRun.cs` implementieren**

Datei `IdealAkeWms/Services/SyncLogger/SyncLogger.cs`:

```csharp
using IdealAkeWms.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdealAkeWms.Services.SyncLogger;

public sealed class SyncLogger : ISyncLogger
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly ILogger<SyncLogger> _logger;

    public SyncLogger(IDbContextFactory<ApplicationDbContext> factory, ILogger<SyncLogger> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<ISyncRun> BeginRunAsync(string serviceName, CancellationToken ct = default)
    {
        await SyncRun.WriteEntryAsync(_factory, _logger, serviceName, Models.SyncLogLevel.Info,
            message: "Run gestartet", reference: null, ct: ct);
        return new SyncRun(_factory, _logger, serviceName);
    }
}
```

Datei `IdealAkeWms/Services/SyncLogger/SyncRun.cs`:

```csharp
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdealAkeWms.Services.SyncLogger;

public sealed class SyncRun : ISyncRun
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly ILogger _logger;
    private readonly string _serviceName;
    private FinishMode _mode = FinishMode.None;

    private enum FinishMode { None, Success, Failed }

    internal SyncRun(IDbContextFactory<ApplicationDbContext> factory, ILogger logger, string serviceName)
    {
        _factory = factory;
        _logger = logger;
        _serviceName = serviceName;
    }

    public Task LogInfoAsync(string message, string? reference = null, CancellationToken ct = default)
        => WriteEntryAsync(_factory, _logger, _serviceName, SyncLogLevel.Info, message, reference, ct);

    public Task LogWarningAsync(string message, string? reference = null, CancellationToken ct = default)
        => WriteEntryAsync(_factory, _logger, _serviceName, SyncLogLevel.Warning, message, reference, ct);

    public Task LogErrorAsync(string message, string? reference = null, CancellationToken ct = default)
        => WriteEntryAsync(_factory, _logger, _serviceName, SyncLogLevel.Error, message, reference, ct);

    public async Task FinishSuccessAsync(IReadOnlyDictionary<string, int>? counts = null,
                                         string? messageSuffix = null,
                                         CancellationToken ct = default)
    {
        if (_mode != FinishMode.None) return;
        _mode = FinishMode.Success;

        var msg = BuildMessage("Run erfolgreich beendet", counts, messageSuffix);
        await WriteEntryAsync(_factory, _logger, _serviceName, SyncLogLevel.Info, msg, reference: null, ct);
    }

    public async Task FinishFailedAsync(string errorMessage,
                                        IReadOnlyDictionary<string, int>? counts = null,
                                        CancellationToken ct = default)
    {
        if (_mode != FinishMode.None) return;
        _mode = FinishMode.Failed;

        var prefix = $"Run fehlgeschlagen: {errorMessage}";
        var msg = BuildMessage(prefix, counts, messageSuffix: null);
        await WriteEntryAsync(_factory, _logger, _serviceName, SyncLogLevel.Error, msg, reference: null, ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_mode != FinishMode.None) return;
        _mode = FinishMode.Failed; // idempotent
        await WriteEntryAsync(_factory, _logger, _serviceName, SyncLogLevel.Warning,
            message: "Run wurde unerwartet beendet (kein FinishXxx-Aufruf)", reference: null,
            ct: CancellationToken.None);
    }

    // ----- Helpers -----

    internal static async Task WriteEntryAsync(
        IDbContextFactory<ApplicationDbContext> factory,
        ILogger logger,
        string serviceName,
        string level,
        string message,
        string? reference,
        CancellationToken ct)
    {
        try
        {
            await using var ctx = await factory.CreateDbContextAsync(ct);
            ctx.SyncLogs.Add(new SyncLog
            {
                Service = serviceName,
                Level = level,
                Message = message,
                Reference = reference,
                Timestamp = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SyncLog write failed for {Service} (level={Level}, msg={Message})",
                serviceName, level, message);
        }
    }

    private static string BuildMessage(string prefix, IReadOnlyDictionary<string, int>? counts, string? messageSuffix)
    {
        var parts = new List<string> { prefix };
        if (counts != null && counts.Count > 0)
            parts.Add(string.Join(", ", counts.Select(kv => $"{kv.Key}={kv.Value}")));
        if (!string.IsNullOrWhiteSpace(messageSuffix))
            parts.Add(messageSuffix);
        return string.Join(" — ", parts);
    }
}
```

- [ ] **Step 5: Test erneut laufen lassen — soll passen**

```bash
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-restore -c Debug --nologo --filter "FullyQualifiedName~SyncLoggerTests"
```

Erwartet: 1 Test bestanden.

- [ ] **Step 6: Restliche Unit-Tests anhaengen**

Erweitere `SyncLoggerTests.cs` (gleiche Datei, in der Klasse) um folgende Tests:

```csharp
[Fact]
public async Task LogWarningAsync_writes_warning_with_reference()
{
    var factory = new TestApplicationDbContextFactory();
    var logger = new SyncLogger(factory, NullLogger<SyncLogger>.Instance);

    await using var run = await logger.BeginRunAsync(SyncLogServices.Lagerplatz);
    await run.LogWarningAsync("Duplikat", reference: "A-001");
    await run.FinishSuccessAsync();

    using var verify = factory.CreateDbContext();
    var warning = await verify.SyncLogs.FirstAsync(e => e.Level == SyncLogLevel.Warning);
    warning.Message.Should().Be("Duplikat");
    warning.Reference.Should().Be("A-001");
    warning.Service.Should().Be(SyncLogServices.Lagerplatz);
}

[Fact]
public async Task FinishSuccessAsync_renders_counts_into_message()
{
    var factory = new TestApplicationDbContextFactory();
    var logger = new SyncLogger(factory, NullLogger<SyncLogger>.Instance);

    await using (var run = await logger.BeginRunAsync(SyncLogServices.BomCache))
    {
        await run.FinishSuccessAsync(new Dictionary<string, int>
        {
            ["neu"] = 12, ["aktualisiert"] = 5, ["uebersprungen"] = 0,
        });
    }

    using var verify = factory.CreateDbContext();
    var end = await verify.SyncLogs.OrderByDescending(e => e.Id).FirstAsync();
    end.Level.Should().Be(SyncLogLevel.Info);
    end.Message.Should().Be("Run erfolgreich beendet — neu=12, aktualisiert=5, uebersprungen=0");
}

[Fact]
public async Task FinishFailedAsync_writes_error_level_with_message()
{
    var factory = new TestApplicationDbContextFactory();
    var logger = new SyncLogger(factory, NullLogger<SyncLogger>.Instance);

    await using (var run = await logger.BeginRunAsync(SyncLogServices.Holiday))
    {
        await run.FinishFailedAsync("HTTP 500 von date.nager.at",
            counts: new Dictionary<string, int> { ["importiert"] = 2 });
    }

    using var verify = factory.CreateDbContext();
    var end = await verify.SyncLogs.OrderByDescending(e => e.Id).FirstAsync();
    end.Level.Should().Be(SyncLogLevel.Error);
    end.Message.Should().Be("Run fehlgeschlagen: HTTP 500 von date.nager.at — importiert=2");
}

[Fact]
public async Task Dispose_without_finish_writes_unexpected_termination_warning()
{
    var factory = new TestApplicationDbContextFactory();
    var logger = new SyncLogger(factory, NullLogger<SyncLogger>.Instance);

    await using (var run = await logger.BeginRunAsync(SyncLogServices.EnaioDms))
    {
        // kein Finish-Aufruf
    }

    using var verify = factory.CreateDbContext();
    var entries = await verify.SyncLogs.OrderBy(e => e.Id).ToListAsync();
    entries.Should().HaveCount(2);
    entries[1].Level.Should().Be(SyncLogLevel.Warning);
    entries[1].Message.Should().Contain("unerwartet beendet");
}

[Fact]
public async Task DoubleFinish_is_ignored()
{
    var factory = new TestApplicationDbContextFactory();
    var logger = new SyncLogger(factory, NullLogger<SyncLogger>.Instance);

    await using (var run = await logger.BeginRunAsync(SyncLogServices.OseonTracking))
    {
        await run.FinishSuccessAsync();
        await run.FinishFailedAsync("doch nicht");  // sollte ignoriert werden
    }

    using var verify = factory.CreateDbContext();
    var entries = await verify.SyncLogs.OrderBy(e => e.Id).ToListAsync();
    // Erwartet: Start + 1x Success-Finish = 2 Eintraege. Kein "Run fehlgeschlagen".
    entries.Should().HaveCount(2);
    entries.Should().NotContain(e => e.Message.StartsWith("Run fehlgeschlagen"));
}
```

- [ ] **Step 7: Alle Logger-Tests laufen lassen**

```bash
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-restore -c Debug --nologo --filter "FullyQualifiedName~SyncLoggerTests"
```

Erwartet: 5 Tests bestanden.

- [ ] **Step 8: Commit**

```bash
git add IdealAkeWms/Services/SyncLogger/SyncLogger.cs IdealAkeWms/Services/SyncLogger/SyncRun.cs IdealAkeWms.Tests/Helpers/TestApplicationDbContextFactory.cs IdealAkeWms.Tests/Services/SyncLoggerTests.cs
git commit -m "feat(synclog): implement SyncLogger + SyncRun with TDD tests"
```

---

## Task 4: FakeSyncLogger Test-Helper

**Files:**
- Create: `IdealAkeWms.Tests/Helpers/FakeSyncLogger.cs`

- [ ] **Step 1: Test-Helper anlegen**

```csharp
using IdealAkeWms.Services.SyncLogger;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>
/// In-Memory <see cref="ISyncLogger"/> fuer Service-Tests. Sammelt alle Aufrufe
/// in einer Liste, sodass Tests <c>fake.Runs[0].Calls.Should()...</c> pruefen koennen.
/// </summary>
public sealed class FakeSyncLogger : ISyncLogger
{
    public List<FakeSyncRun> Runs { get; } = new();

    public Task<ISyncRun> BeginRunAsync(string serviceName, CancellationToken ct = default)
    {
        var run = new FakeSyncRun(serviceName);
        Runs.Add(run);
        return Task.FromResult<ISyncRun>(run);
    }
}

public sealed class FakeSyncRun : ISyncRun
{
    public string ServiceName { get; }
    public List<(string Level, string Message, string? Reference)> Events { get; } = new();
    public bool FinishedSuccess { get; private set; }
    public bool FinishedFailed { get; private set; }
    public IReadOnlyDictionary<string, int>? FinalCounts { get; private set; }
    public string? FinalErrorMessage { get; private set; }
    public bool Disposed { get; private set; }

    public FakeSyncRun(string serviceName)
    {
        ServiceName = serviceName;
    }

    public Task LogInfoAsync(string message, string? reference = null, CancellationToken ct = default)
    {
        Events.Add(("Info", message, reference));
        return Task.CompletedTask;
    }

    public Task LogWarningAsync(string message, string? reference = null, CancellationToken ct = default)
    {
        Events.Add(("Warning", message, reference));
        return Task.CompletedTask;
    }

    public Task LogErrorAsync(string message, string? reference = null, CancellationToken ct = default)
    {
        Events.Add(("Error", message, reference));
        return Task.CompletedTask;
    }

    public Task FinishSuccessAsync(IReadOnlyDictionary<string, int>? counts = null,
                                   string? messageSuffix = null,
                                   CancellationToken ct = default)
    {
        if (FinishedSuccess || FinishedFailed) return Task.CompletedTask;
        FinishedSuccess = true;
        FinalCounts = counts;
        return Task.CompletedTask;
    }

    public Task FinishFailedAsync(string errorMessage,
                                  IReadOnlyDictionary<string, int>? counts = null,
                                  CancellationToken ct = default)
    {
        if (FinishedSuccess || FinishedFailed) return Task.CompletedTask;
        FinishedFailed = true;
        FinalErrorMessage = errorMessage;
        FinalCounts = counts;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
```

- [ ] **Step 2: Build verifizieren**

```bash
dotnet build IdealAkeWms.Tests/IdealAkeWms.Tests.csproj -c Debug --nologo
```

Erwartet: 0 Fehler.

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms.Tests/Helpers/FakeSyncLogger.cs
git commit -m "test(synclog): add FakeSyncLogger helper for service tests"
```

---

## Task 5: DI-Registrierung Web-Projekt

**Files:**
- Modify: `IdealAkeWms/Program.cs:17-20` (Dbcontext-Block) und Zeile 75 (Repo-Block)

- [ ] **Step 1: `Program.cs` anpassen**

Direkt nach dem bestehenden `AddDbContext<ApplicationDbContext>(...)`-Aufruf (Zeile 17-20) einen `AddDbContextFactory`-Aufruf einfuegen:

Vorher (Zeilen 17-20):
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(120)));
```

Nachher:
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(120)));

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(120)));
```

Direkt nach `AddScoped<ISyncLogRepository, SyncLogRepository>()` (Zeile 75) ergaenzen:

```csharp
builder.Services.AddScoped<ISyncLogRepository, SyncLogRepository>();
builder.Services.AddSingleton<IdealAkeWms.Services.SyncLogger.ISyncLogger,
                              IdealAkeWms.Services.SyncLogger.SyncLogger>();
```

- [ ] **Step 2: Build verifizieren**

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
```

Erwartet: 0 Fehler.

- [ ] **Step 3: Tests laufen (volle Web-Test-Suite, Smoke)**

```bash
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: alle Tests bestanden (~576+ bei aktuellem Stand + neue Logger-Tests).

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Program.cs
git commit -m "feat(synclog): register ISyncLogger + DbContextFactory in Web project"
```

---

## Task 6: DI-Registrierung Service-Projekt

**Files:**
- Modify: `IDEALAKEWMSService/Program.cs:30-33` und Zeile 41

- [ ] **Step 1: `Program.cs` anpassen**

Direkt nach `AddDbContext<ApplicationDbContext>(...)` (Zeile 30-33) ergaenzen:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(120)));

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(120)));
```

Direkt nach `AddScoped<ISyncLogRepository, SyncLogRepository>()` (Zeile 41) ergaenzen:

```csharp
builder.Services.AddScoped<ISyncLogRepository, SyncLogRepository>();
builder.Services.AddSingleton<IdealAkeWms.Services.SyncLogger.ISyncLogger,
                              IdealAkeWms.Services.SyncLogger.SyncLogger>();
```

- [ ] **Step 2: Build verifizieren**

```bash
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Debug --nologo
```

Erwartet: 0 Fehler (MailKit-Warnings sind vorhanden und nicht durch diese Aenderung verursacht).

- [ ] **Step 3: Commit**

```bash
git add IDEALAKEWMSService/Program.cs
git commit -m "feat(synclog): register ISyncLogger + DbContextFactory in Service project"
```

---

## Task 7: SyncLogController.KnownServices auf SyncLogServices.All umstellen

**Files:**
- Modify: `IdealAkeWms/Controllers/SyncLogController.cs:13`

- [ ] **Step 1: KnownServices durch SyncLogServices.All ersetzen**

Vorher (Zeile 13):
```csharp
private static readonly string[] KnownServices = new[] { "Lagerplatz", "Lagerbestand", "OseonTracking", "Article", "ProductionOrder", "EnaioDms", "BomCache", "Holiday" };
```

Nachher:
```csharp
private static readonly IReadOnlyList<string> KnownServices = SyncLogServices.All;
```

Zusaetzlich oben in den Usings:
```csharp
using IdealAkeWms.Services.SyncLogger;
```

Falls `AvailableServices = KnownServices.ToList()` (Zeile ~33) referenziert ist, bleibt das weiterhin gueltig.

- [ ] **Step 2: Build verifizieren**

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
```

Erwartet: 0 Fehler.

- [ ] **Step 3: Bestehende SyncLogControllerTests laufen**

```bash
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-restore -c Debug --nologo --filter "FullyQualifiedName~SyncLogControllerTests"
```

Erwartet: alle Tests bestanden.

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Controllers/SyncLogController.cs
git commit -m "refactor(synclog): SyncLogController.KnownServices uses SyncLogServices.All"
```

---

## Task 8: LagerplatzSyncService migrieren

**Files:**
- Modify: `IDEALAKEWMSService/Services/LagerplatzSyncService.cs`
- Modify: `IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs`

**Strategie:** vorhandene Tests bleiben gruen wahrend der Migration. Konstruktor wird umgestellt: `ISyncLogRepository syncLogs` → `ISyncLogger syncLogger`. Alle `_syncLogs.AddAsync(new SyncLog { ... })`-Aufrufe wandern in `await run.LogXxxAsync(...)`. Start- und End-Eintraege werden durch `BeginRunAsync` + `FinishSuccessAsync`/`FinishFailedAsync` erzeugt.

- [ ] **Step 1: Konstruktor umstellen**

In `LagerplatzSyncService.cs`, Felder + Konstruktor (Zeilen 17-31):

```csharp
private readonly ApplicationDbContext _ctx;
private readonly ISageLagerplatzReader _reader;
private readonly IdealAkeWms.Services.SyncLogger.ISyncLogger _syncLogger;
private readonly ILogger<LagerplatzSyncService> _logger;

public LagerplatzSyncService(
    ApplicationDbContext ctx,
    ISageLagerplatzReader reader,
    IdealAkeWms.Services.SyncLogger.ISyncLogger syncLogger,
    ILogger<LagerplatzSyncService> logger)
{
    _ctx = ctx;
    _reader = reader;
    _syncLogger = syncLogger;
    _logger = logger;
}
```

Entferne `private const string ServiceName = "Lagerplatz";` (Zeile 11) — wird durch `SyncLogServices.Lagerplatz` ersetzt.

- [ ] **Step 2: RunAsync auf BeginRunAsync/FinishXxxAsync umstellen**

`RunAsync(CancellationToken ct = default)` (ab Zeile 35) Body komplett in einen `await using var run = ... ; try { ... } catch { ... }`-Block packen.

Pattern:
```csharp
public async Task RunAsync(CancellationToken ct = default)
{
    await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.Lagerplatz, ct);
    int newCount = 0, updated = 0, conflicts = 0, deactivated = 0, skipped = 0, errors = 0;
    try
    {
        // ... bestehende Verarbeitungs-Logik, aber jeder bisherige
        // _syncLogs.AddAsync(new SyncLog { Service = ServiceName, Level = SyncLogLevel.Warning, Message = "...", Reference = "..." })
        // wird zu:
        // await run.LogWarningAsync("...", reference: "...", ct);

        // Alte End-Summary entfaellt (war direkter _syncLogs.AddAsync mit Counts).
        // Stattdessen:
        await run.FinishSuccessAsync(new Dictionary<string, int>
        {
            ["neu"] = newCount,
            ["aktualisiert"] = updated,
            ["konflikte"] = conflicts,
            ["deaktiviert"] = deactivated,
            ["uebersprungen"] = skipped,
            ["fehler"] = errors,
        }, ct: ct);
    }
    catch (Exception ex)
    {
        await run.LogErrorAsync(ex.Message, ct: ct);
        await run.FinishFailedAsync(ex.Message, counts: new Dictionary<string, int>
        {
            ["neu"] = newCount, ["aktualisiert"] = updated, ["konflikte"] = conflicts,
            ["deaktiviert"] = deactivated, ["uebersprungen"] = skipped, ["fehler"] = errors,
        }, ct: ct);
        throw;
    }
}
```

Konkrete Substitutionen (basierend auf den 9 Aufrufen die der Bestandsaufnahme nach existieren):
- `_syncLogs.AddAsync(new SyncLog { Service = ServiceName, Level = SyncLogLevel.Error, Message = msg, Reference = code })` → `await run.LogErrorAsync(msg, reference: code, ct)`
- `_syncLogs.AddAsync(new SyncLog { Service = ServiceName, Level = SyncLogLevel.Warning, Message = msg, Reference = code })` → `await run.LogWarningAsync(msg, reference: code, ct)`
- `_syncLogs.AddAsync(new SyncLog { Service = ServiceName, Level = SyncLogLevel.Info, Message = msg, Reference = code })` → `await run.LogInfoAsync(msg, reference: code, ct)`
- Den finalen Summary-`AddAsync` ersetzen durch `FinishSuccessAsync(counts)`.

Add `using IdealAkeWms.Services.SyncLogger;` und entferne `using IdealAkeWms.Data.Repositories;` falls dadurch ungenutzt.

- [ ] **Step 3: Tests anpassen**

In `IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs`:

- `using IdealAkeWms.Tests.Helpers;` ergaenzen (FakeSyncLogger liegt im Web-Test-Projekt — entweder umziehen, neuen Test-Project-Reference, oder Helper im Service-Test-Project duplizieren. **Empfehlung:** Helper duplizieren als `IDEALAKEWMSService.Tests/Helpers/FakeSyncLogger.cs` mit identischem Inhalt, weil die zwei Test-Projekte heute keinen geteilten Helper-Namespace haben).
- Konstruktor-Aufruf in Tests anpassen: `new LagerplatzSyncService(ctx, reader, new FakeSyncLogger(), NullLogger<LagerplatzSyncService>.Instance)`.
- Assert-Stellen, die heute auf `ctx.SyncLogs` lesen, umstellen auf `fakeLogger.Runs[0].Events.Should().ContainSingle(e => e.Level == "Warning" && e.Reference == "...");` etc.

Konkret: lege zunaechst `IDEALAKEWMSService.Tests/Helpers/FakeSyncLogger.cs` an mit demselben Inhalt wie in Task 4 (gleiche Klasse, Namespace `IDEALAKEWMSService.Tests.Helpers`).

- [ ] **Step 4: Tests laufen**

```bash
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-restore -c Debug --nologo --filter "FullyQualifiedName~LagerplatzSyncServiceTests"
```

Erwartet: alle bisherigen Tests bestanden.

- [ ] **Step 5: Service-Projekt-Build**

```bash
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Debug --nologo
```

Erwartet: 0 Fehler.

- [ ] **Step 6: Commit**

```bash
git add IDEALAKEWMSService/Services/LagerplatzSyncService.cs IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs IDEALAKEWMSService.Tests/Helpers/FakeSyncLogger.cs
git commit -m "refactor(synclog): migrate LagerplatzSyncService to ISyncLogger"
```

---

## Task 9: LagerbestandSyncService migrieren

**Files:**
- Modify: `IDEALAKEWMSService/Services/LagerbestandSyncService.cs`
- Modify: `IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs`

**Strategie:** identisch zu Task 8.

- [ ] **Step 1: Konstruktor umstellen**

Vorher (Zeilen 20-32):
```csharp
public LagerbestandSyncService(
    ApplicationDbContext ctx,
    ISageBestandReader reader,
    IStockMovementRepository stockRepo,
    ISyncLogRepository syncLogs,
    ILogger<LagerbestandSyncService> logger)
```

Nachher:
```csharp
public LagerbestandSyncService(
    ApplicationDbContext ctx,
    ISageBestandReader reader,
    IStockMovementRepository stockRepo,
    IdealAkeWms.Services.SyncLogger.ISyncLogger syncLogger,
    ILogger<LagerbestandSyncService> logger)
```

Felder entsprechend (`_syncLogs` → `_syncLogger`). Entferne `private const string ServiceName = "Lagerbestand";`.

- [ ] **Step 2: RunAsync(bool dryRun, ...) umstellen**

```csharp
public async Task RunAsync(bool dryRun, CancellationToken ct = default)
{
    await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.Lagerbestand, ct);
    int correctedIn = 0, correctedOut = 0, skipped = 0, errors = 0;
    try
    {
        // ... bestehende Verarbeitung, alle _syncLogs.AddAsync-Aufrufe auf run.LogXxxAsync umstellen.
        // DryRun-Praefix bleibt im Message-Text der einzelnen Events erhalten ("[DryRun] ...").

        await run.FinishSuccessAsync(new Dictionary<string, int>
        {
            ["einbuchungen"] = correctedIn,
            ["ausbuchungen"] = correctedOut,
            ["uebersprungen"] = skipped,
            ["fehler"] = errors,
        }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
    }
    catch (Exception ex)
    {
        await run.LogErrorAsync(ex.Message, ct: ct);
        await run.FinishFailedAsync(ex.Message, counts: new Dictionary<string, int>
        {
            ["einbuchungen"] = correctedIn, ["ausbuchungen"] = correctedOut,
            ["uebersprungen"] = skipped, ["fehler"] = errors,
        }, ct: ct);
        throw;
    }
}
```

- [ ] **Step 3: Tests anpassen**

In `IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs`:

1. `using IDEALAKEWMSService.Tests.Helpers;` ergaenzen (FakeSyncLogger wurde in Task 8 unter diesem Namespace dupliziert).
2. Alle Konstruktor-Aufrufe ersetzen — der Parameter `syncLogs: <repo>` wird durch `syncLogger: new FakeSyncLogger()` ersetzt. Konkret:

```csharp
var fakeLogger = new FakeSyncLogger();
var service = new LagerbestandSyncService(ctx, reader, stockRepo, fakeLogger, NullLogger<LagerbestandSyncService>.Instance);
```

3. Assert-Stellen, die heute `ctx.SyncLogs` lesen, umstellen auf den Fake:

```csharp
// statt: var warnings = await ctx.SyncLogs.Where(l => l.Level == SyncLogLevel.Warning).ToListAsync();
// neu:
fakeLogger.Runs.Should().HaveCount(1);
fakeLogger.Runs[0].ServiceName.Should().Be("Lagerbestand");
fakeLogger.Runs[0].Events.Should().Contain(e => e.Level == "Warning" && e.Reference == "<erwarteter-key>");
fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
```

- [ ] **Step 4: Tests laufen**

```bash
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-restore -c Debug --nologo --filter "FullyQualifiedName~LagerbestandSyncServiceTests"
```

Erwartet: alle Tests bestanden.

- [ ] **Step 5: Commit**

```bash
git add IDEALAKEWMSService/Services/LagerbestandSyncService.cs IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs
git commit -m "refactor(synclog): migrate LagerbestandSyncService to ISyncLogger"
```

---

## Task 10: HolidaySyncService integrieren

**Files:**
- Modify: `IDEALAKEWMSService/Services/HolidaySyncService.cs`
- Modify: `IDEALAKEWMSService.Tests/Services/HolidaySyncServiceTests.cs`

**Begruendung der Wahl als erster Neu-Integrationspunkt:** kleinster Service, klare Counts (Anzahl importierte Feiertage), klarer HTTP-Fehlerpfad (date.nager.at).

- [ ] **Step 1: Konstruktor und Felder erweitern**

Vorher (Zeilen 28-42):
```csharp
public HolidaySyncService(
    ApplicationDbContext ctx,
    HttpClient http,
    IOptions<HolidaySyncOptions> options,
    ILogger<HolidaySyncService> logger)
```

Nachher: zusaetzlichen Parameter `IdealAkeWms.Services.SyncLogger.ISyncLogger syncLogger` einfuegen und als Feld speichern.

- [ ] **Step 2: RunAsync mit Logger-Lifecycle umschliessen**

```csharp
public async Task<HolidaySyncResult> RunAsync(CancellationToken ct)
{
    await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.Holiday, ct);
    var result = new HolidaySyncResult();
    try
    {
        // ... bestehende HTTP-Abrufe + Insert-Logik ...
        // Bei einem HTTP-Fehler einer Jahres-Anfrage:
        //   await run.LogWarningAsync($"date.nager.at lieferte {statusCode} fuer Jahr {year}", reference: year.ToString(), ct);
        // Bei einem neu eingefuegten Holiday:
        //   (kein per-record Log — Counts reichen)

        await run.FinishSuccessAsync(new Dictionary<string, int>
        {
            ["importiert"] = result.Imported,
            ["uebersprungen"] = result.Skipped,
        }, ct: ct);
        return result;
    }
    catch (Exception ex)
    {
        await run.LogErrorAsync(ex.Message, ct: ct);
        await run.FinishFailedAsync(ex.Message,
            counts: new Dictionary<string, int>
            {
                ["importiert"] = result.Imported,
                ["uebersprungen"] = result.Skipped,
            }, ct: ct);
        throw;
    }
}
```

- [ ] **Step 3: Tests anpassen**

`HolidaySyncServiceTests.cs`: Konstruktor-Aufruf um `new FakeSyncLogger()` ergaenzen. Mindestens 1 neuer Test:

```csharp
[Fact]
public async Task RunAsync_writes_lifecycle_to_synclogger()
{
    // ... Setup wie bisher ...
    var fakeLogger = new FakeSyncLogger();
    var service = new HolidaySyncService(ctx, http, options, fakeLogger, NullLogger<HolidaySyncService>.Instance);

    await service.RunAsync(CancellationToken.None);

    fakeLogger.Runs.Should().HaveCount(1);
    fakeLogger.Runs[0].ServiceName.Should().Be("Holiday");
    fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
}
```

- [ ] **Step 4: Tests laufen**

```bash
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-restore -c Debug --nologo --filter "FullyQualifiedName~HolidaySyncServiceTests"
```

Erwartet: alle Tests bestanden.

- [ ] **Step 5: Commit**

```bash
git add IDEALAKEWMSService/Services/HolidaySyncService.cs IDEALAKEWMSService.Tests/Services/HolidaySyncServiceTests.cs
git commit -m "feat(synclog): integrate HolidaySyncService with ISyncLogger"
```

---

## Task 11: BomCacheSyncService integrieren

**Files:**
- Modify: `IDEALAKEWMSService/Services/BomCacheSyncService.cs`
- Modify: `IDEALAKEWMSService.Tests/Services/BomCacheSyncServiceHashTests.cs` *(falls Konstruktor erweitert wird, sonst neuer Test)*

- [ ] **Step 1: Konstruktor erweitern**

Vorher:
```csharp
public BomCacheSyncService(IConfiguration configuration, ILogger<BomCacheSyncService> logger)
```

Nachher:
```csharp
public BomCacheSyncService(
    IConfiguration configuration,
    ILogger<BomCacheSyncService> logger,
    IdealAkeWms.Services.SyncLogger.ISyncLogger syncLogger)
```

Feld `_syncLogger` ergaenzen.

- [ ] **Step 2: `SyncBomCacheAsync(bool dryRun, CancellationToken ct)` wrappen**

```csharp
public async Task<SyncResult> SyncBomCacheAsync(bool dryRun, CancellationToken ct)
{
    await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.BomCache, ct);
    var result = new SyncResult();
    try
    {
        // ... bestehende Cache-Berechnung + DB-Inserts ...

        await run.FinishSuccessAsync(new Dictionary<string, int>
        {
            ["neu"] = result.Inserted,
            ["aktualisiert"] = result.Updated,
            ["uebersprungen"] = result.Skipped,
        }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
        return result;
    }
    catch (Exception ex)
    {
        await run.LogErrorAsync(ex.Message, ct: ct);
        await run.FinishFailedAsync(ex.Message, ct: ct);
        throw;
    }
}
```

- [ ] **Step 3: Tests anpassen**

```bash
git grep -l "new BomCacheSyncService(" -- '*.cs'
```

Pro Treffer den Konstruktor-Aufruf um `new FakeSyncLogger()` als dritten Parameter erweitern. `BomCacheSyncServiceHashTests` testet u.U. nur die interne Hash-Logik ohne den Service zu instanziieren — dann gibt es nichts zu tun.

- [ ] **Step 4: Build + Tests**

```bash
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Debug --nologo
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-restore -c Debug --nologo --filter "FullyQualifiedName~BomCache"
```

Erwartet: 0 Fehler, Tests gruen.

- [ ] **Step 5: Commit**

```bash
git add IDEALAKEWMSService/Services/BomCacheSyncService.cs IDEALAKEWMSService.Tests/Services/BomCacheSyncServiceHashTests.cs
git commit -m "feat(synclog): integrate BomCacheSyncService with ISyncLogger"
```

---

## Task 12: OseonSyncService integrieren

**Files:**
- Modify: `IDEALAKEWMSService/Services/OseonSyncService.cs`
- Modify (optional): `IDEALAKEWMSService.Tests/Services/OseonSyncServiceTests.cs` falls existent

- [ ] **Step 1: Konstruktor erweitern + Run wrappen**

Vorher:
```csharp
public OseonSyncService(IConfiguration configuration, ILogger<OseonSyncService> logger)
public async Task<SyncResult> SyncOseonProductionOrdersAsync(bool dryRun, CancellationToken ct = default)
```

Nachher: zusaetzlicher `ISyncLogger`-Parameter. `SyncOseonProductionOrdersAsync` umschliessen mit:

```csharp
await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.OseonTracking, ct);
var result = new SyncResult();
try
{
    // bestehende Logik
    await run.FinishSuccessAsync(new Dictionary<string, int>
    {
        ["neu"] = result.Inserted,
        ["aktualisiert"] = result.Updated,
        ["fertig"] = result.Skipped,
    }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
    return result;
}
catch (Exception ex)
{
    await run.LogErrorAsync(ex.Message, ct: ct);
    await run.FinishFailedAsync(ex.Message, ct: ct);
    throw;
}
```

- [ ] **Step 2: Tests anpassen (nur wenn vorhanden)**

Vor diesem Schritt einmal grep:

```bash
git grep -l "new OseonSyncService(" -- '*.cs'
```

Pro Treffer: den Konstruktor-Aufruf um `new FakeSyncLogger()` als neuen Parameter erweitern. Wenn kein Treffer kommt: Step 2 ueberspringen, weiter zu Step 3 (Build-Verifikation deckt Konstruktor-Signatur ab).

- [ ] **Step 3: Build + Tests**

```bash
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Debug --nologo
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: 0 Fehler, alle Tests gruen.

- [ ] **Step 4: Commit**

```bash
git add IDEALAKEWMSService/Services/OseonSyncService.cs
git commit -m "feat(synclog): integrate OseonSyncService with ISyncLogger"
```

---

## Task 13: EnaioDmsSyncService integrieren

**Files:**
- Modify: `IDEALAKEWMSService/Services/EnaioDmsSyncService.cs`
- Modify (optional): Tests dazu

- [ ] **Step 1: Konstruktor erweitern + Run wrappen**

Vorher:
```csharp
public EnaioDmsSyncService(IConfiguration configuration, ILogger<EnaioDmsSyncService> logger)
public async Task<SyncResult> SyncDocumentsAsync(bool dryRun, CancellationToken ct = default)
```

Nachher: zusaetzlicher `ISyncLogger`-Parameter. `SyncDocumentsAsync` wrappen:

```csharp
await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.EnaioDms, ct);
var result = new SyncResult();
try
{
    // bestehende Logik
    await run.FinishSuccessAsync(new Dictionary<string, int>
    {
        ["neu"] = result.Inserted,
        ["aktualisiert"] = result.Updated,
        ["uebersprungen"] = result.Skipped,
    }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
    return result;
}
catch (Exception ex)
{
    await run.LogErrorAsync(ex.Message, ct: ct);
    await run.FinishFailedAsync(ex.Message, ct: ct);
    throw;
}
```

- [ ] **Step 2: Tests anpassen (nur wenn vorhanden)**

```bash
git grep -l "new EnaioDmsSyncService(" -- '*.cs'
```

Pro Treffer: den Konstruktor-Aufruf um `new FakeSyncLogger()` als neuen Parameter erweitern. Wenn kein Treffer: weiter.

- [ ] **Step 3: Build + Tests**

```bash
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Debug --nologo
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: 0 Fehler, Tests gruen.

- [ ] **Step 4: Commit**

```bash
git add IDEALAKEWMSService/Services/EnaioDmsSyncService.cs
git commit -m "feat(synclog): integrate EnaioDmsSyncService with ISyncLogger"
```

---

## Task 14: CoatingDetectionService integrieren

**Files:**
- Modify: `IDEALAKEWMSService/Services/CoatingDetectionService.cs`
- Modify: `IDEALAKEWMSService.Tests/Services/CoatingDetectionServiceTests.cs`

- [ ] **Step 1: Konstruktor erweitern + DetectAndUpdateCoatingFlagsAsync wrappen**

Vorher:
```csharp
public CoatingDetectionService(IConfiguration configuration, ILogger<CoatingDetectionService> logger)
public async Task<SyncResult> DetectAndUpdateCoatingFlagsAsync(bool dryRun, List<int>? specificOrderIds, CancellationToken ct)
```

Nachher: zusaetzlicher `ISyncLogger`-Parameter. Methode wrappen:

```csharp
await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.CoatingDetection, ct);
var result = new SyncResult();
try
{
    // bestehende Logik
    await run.FinishSuccessAsync(new Dictionary<string, int>
    {
        ["geprueft"] = result.Checked,
        ["aktualisiert"] = result.Updated,
    }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
    return result;
}
catch (Exception ex)
{
    await run.LogErrorAsync(ex.Message, ct: ct);
    await run.FinishFailedAsync(ex.Message, ct: ct);
    throw;
}
```

- [ ] **Step 2: Tests anpassen — Konstruktor-Aufruf erweitern um FakeSyncLogger**

- [ ] **Step 3: Build + Tests**

```bash
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Debug --nologo
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-restore -c Debug --nologo --filter "FullyQualifiedName~CoatingDetection"
```

Erwartet: 0 Fehler, Tests gruen.

- [ ] **Step 4: Commit**

```bash
git add IDEALAKEWMSService/Services/CoatingDetectionService.cs IDEALAKEWMSService.Tests/Services/CoatingDetectionServiceTests.cs
git commit -m "feat(synclog): integrate CoatingDetectionService with ISyncLogger"
```

---

## Task 15: SageImportService integrieren (2 logische Runs pro Tick)

**Files:**
- Modify: `IDEALAKEWMSService/Services/SageImportService.cs`
- Modify: Tests dazu falls existent

**Strategie:** Der Service hat zwei separate Top-Level-Methoden:
- `SyncProductionOrdersAsync` → loggt unter `SyncLogServices.ProductionOrder`
- `SyncArticlesAsync` → loggt unter `SyncLogServices.Article`

Jede Methode bekommt eigenen `BeginRunAsync`/`FinishXxxAsync`-Zyklus. Wenn beide nacheinander aufgerufen werden, entstehen zwei vollstaendige Runs im SyncLog (Start/Ende fuer ProductionOrder, dann Start/Ende fuer Article).

- [ ] **Step 1: Konstruktor erweitern**

Vorher:
```csharp
public SageImportService(IConfiguration configuration, ILogger<SageImportService> logger,
    IBomCacheSyncService bomCacheSync, ICoatingDetectionService coatingDetection)
```

Nachher: zusaetzlicher `ISyncLogger`-Parameter, als Feld speichern.

- [ ] **Step 2: SyncProductionOrdersAsync wrappen**

```csharp
public async Task<SyncResult> SyncProductionOrdersAsync(bool dryRun, CancellationToken ct = default)
{
    await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.ProductionOrder, ct);
    var result = new SyncResult();
    try
    {
        // bestehende Production-Order-Import-Logik
        await run.FinishSuccessAsync(new Dictionary<string, int>
        {
            ["neu"] = result.Inserted,
            ["aktualisiert"] = result.Updated,
            ["uebersprungen"] = result.Skipped,
        }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
        return result;
    }
    catch (Exception ex)
    {
        await run.LogErrorAsync(ex.Message, ct: ct);
        await run.FinishFailedAsync(ex.Message, ct: ct);
        throw;
    }
}
```

- [ ] **Step 3: SyncArticlesAsync wrappen**

```csharp
public async Task<SyncResult> SyncArticlesAsync(bool dryRun, CancellationToken ct = default)
{
    await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.Article, ct);
    var result = new SyncResult();
    try
    {
        // bestehende Artikel-Import-Logik
        await run.FinishSuccessAsync(new Dictionary<string, int>
        {
            ["neu"] = result.Inserted,
            ["aktualisiert"] = result.Updated,
            ["uebersprungen"] = result.Skipped,
        }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
        return result;
    }
    catch (Exception ex)
    {
        await run.LogErrorAsync(ex.Message, ct: ct);
        await run.FinishFailedAsync(ex.Message, ct: ct);
        throw;
    }
}
```

- [ ] **Step 4: Build + Tests**

```bash
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Debug --nologo
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: 0 Fehler, Tests gruen.

- [ ] **Step 5: Commit**

```bash
git add IDEALAKEWMSService/Services/SageImportService.cs
git commit -m "feat(synclog): integrate SageImportService (ProductionOrder + Article) with ISyncLogger"
```

---

## Task 16: Version-Bump auf v1.15.0

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`
- Modify: `IDEALAKEWMSService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`

- [ ] **Step 1: AppVersion.cs in beiden Projekten auf 1.15.0**

`IdealAkeWms/AppVersion.cs`:
```csharp
public const string Version = "1.15.0";
public const string Date = "2026-05-26";
```

`IDEALAKEWMSService/AppVersion.cs`: gleiche Werte.

- [ ] **Step 2: Changelog-Eintrag in `IdealAkeWms/Views/Help/Changelog.cshtml`**

Ganz oben (neuester Eintrag) ergaenzen:

```html
<h3>v1.15.0 — 26.05.2026</h3>
<ul>
    <li><strong>Einheitliches Sync-Audit-Log:</strong> Alle 8 Sync-Services (Lagerplatz, Lagerbestand,
        BomCache, OSEON-Tracking, enaio DMS, Feiertage, Coating Detection, Sage Import = Production
        Orders + Articles) schreiben jetzt Start, Warnings/Errors und Ende-Summary ins Sync-Protokoll.
        Vorher waren nur Lagerplatz und Lagerbestand gelistet.</li>
    <li>Neue interne Architektur: <code>ISyncLogger</code> + <code>ISyncRun</code> (IAsyncDisposable)
        mit eigenem DbContext pro Insert — SyncLog-Schreibfehler crashen Syncs nie und werden nicht
        durch Sync-Transaktionen rolled-back.</li>
    <li>Sync-Protokoll-Dropdown listet automatisch alle 9 Service-Namen (inkl. neuem
        <code>CoatingDetection</code>).</li>
</ul>
```

- [ ] **Step 3: Build verifizieren**

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Debug --nologo
```

Erwartet: 0 Fehler.

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/Changelog.cshtml
git commit -m "feat(version): bump to v1.15.0 (synclog rollout)"
```

---

## Task 17: Hilfe + TESTSZENARIEN + PROJECT_STATUS + CLAUDE.md

**Files:**
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`
- Modify: `docs/TESTSZENARIEN.md`
- Modify: `PROJECT_STATUS.md`
- Modify: `CLAUDE.md`

- [ ] **Step 1: Hilfeseite — Abschnitt Sync-Protokoll erweitern**

In `IdealAkeWms/Views/Help/Index.cshtml`, im bestehenden Abschnitt zum Sync-Protokoll (oder am Ende falls keiner existiert) ergaenzen:

```html
<h3>Sync-Protokoll</h3>
<p>
    Alle Background-Sync-Jobs schreiben Lifecycle- und Diagnose-Eintraege ins Sync-Protokoll
    (verfuegbar unter <em>Stammdaten → Sync-Protokoll</em>, Rolle <code>masterdata</code> oder
    <code>admin</code>):
</p>
<ul>
    <li><strong>Start-Eintrag</strong> (Info): "Run gestartet" — markiert den Beginn jedes Service-Ticks.</li>
    <li><strong>Warnings/Errors</strong> waehrend des Runs: Duplikate, fehlende Stammdaten,
        Konflikte, HTTP-Fehler etc., jeweils mit Referenz (z.B. Lagerplatz-Code, FA-Nummer).</li>
    <li><strong>End-Eintrag</strong> (Info bei Erfolg, Error bei Fehler): "Run erfolgreich beendet — neu=12,
        aktualisiert=5, uebersprungen=0" bzw. "Run fehlgeschlagen: {grund} — counts".</li>
</ul>
<p>
    Service-Filter im Dropdown: Lagerplatz, Lagerbestand, BomCache, OseonTracking, EnaioDms,
    Holiday, CoatingDetection, ProductionOrder, Article.
</p>
```

- [ ] **Step 2: TESTSZENARIEN — 8 neue Szenarien pro Service**

In `docs/TESTSZENARIEN.md`, neues Kapitel ans Ende anhaengen:

```markdown
## Kapitel 27: SyncLog-Pflicht fuer alle Sync-Services (v1.15.0)

### Szenario 27.1: LagerplatzSyncService schreibt Start + Ende
**Vorbedingung:** Lagerplatz-Sync ist aktiviert (`Sync:LagerplatzEnabled = true`).
**Schritt:**
1. Service starten oder Worker-Tick abwarten.
2. Im Sync-Protokoll Service-Filter = "Lagerplatz" setzen.
**Erwartet:** Mindestens 2 Eintraege pro Tick: "Run gestartet" (Info) und "Run erfolgreich beendet
— neu=…, aktualisiert=…, …" (Info).

### Szenario 27.2: LagerbestandSyncService schreibt Start + Ende
Wie 27.1, mit Service-Filter "Lagerbestand".

### Szenario 27.3: BomCacheSyncService — Lifecycle im Sync-Protokoll
**Vorbedingung:** `Sync:BomCacheEnabled = true`.
**Schritt:** Worker-Tick abwarten, Service-Filter "BomCache".
**Erwartet:** Start- + End-Eintrag mit Counts (neu, aktualisiert, uebersprungen).

### Szenario 27.4: OseonSyncService — Lifecycle
Service-Filter "OseonTracking".

### Szenario 27.5: EnaioDmsSyncService — Lifecycle
Service-Filter "EnaioDms".

### Szenario 27.6: HolidaySyncService — Lifecycle und HTTP-Fehlerpfad
**Schritt:** Service-Filter "Holiday".
**Erwartet:** Start + Ende. Falls date.nager.at nicht erreichbar: zusaetzlich ein Warning-Eintrag
"date.nager.at lieferte {statusCode} fuer Jahr {year}".

### Szenario 27.7: CoatingDetectionService — Lifecycle
Service-Filter "CoatingDetection".

### Szenario 27.8: SageImportService — zwei Runs pro Tick (Production Orders + Articles)
**Schritt:** Worker-Tick mit aktiven Sage-Imports.
**Erwartet:** Im Sync-Protokoll erscheinen pro Tick **zwei** logische Runs:
- Service "ProductionOrder": Start + Ende
- Service "Article": Start + Ende
```

- [ ] **Step 3: PROJECT_STATUS.md — Hauptfunktionen + Fortschritts-Sektion**

Im Block "Aktueller Fortschritt (laufend)" eine neue Sub-Sektion anfuegen (analog zur Pagination-Sektion):

```markdown
### SyncLog-Pflicht fuer alle Sync-Services (v1.15.0)

| # | Sub-Task | Status |
|---|---------|--------|
| 1 | Pre-flight grep nach hartcodierten Service-Strings | ⏳ offen |
| 2 | SyncLogServices-Konstanten | ⏳ offen |
| 3 | ISyncLogger / ISyncRun Interfaces | ⏳ offen |
| 4 | SyncLogger + SyncRun Implementierung (TDD, 5 Tests) | ⏳ offen |
| 5 | FakeSyncLogger Test-Helper | ⏳ offen |
| 6 | DI-Registrierung Web | ⏳ offen |
| 7 | DI-Registrierung Service | ⏳ offen |
| 8 | SyncLogController.KnownServices → SyncLogServices.All | ⏳ offen |
| 9 | LagerplatzSyncService migrieren | ⏳ offen |
| 10 | LagerbestandSyncService migrieren | ⏳ offen |
| 11 | HolidaySyncService integrieren | ⏳ offen |
| 12 | BomCacheSyncService integrieren | ⏳ offen |
| 13 | OseonSyncService integrieren | ⏳ offen |
| 14 | EnaioDmsSyncService integrieren | ⏳ offen |
| 15 | CoatingDetectionService integrieren | ⏳ offen |
| 16 | SageImportService integrieren (2 Runs) | ⏳ offen |
| 17 | Version-Bump v1.15.0 + Changelog | ⏳ offen |
| 18 | Hilfe + TESTSZENARIEN + PROJECT_STATUS + CLAUDE.md | ⏳ offen |
```

In der Hauptfunktionen-Tabelle ergaenzen:
```markdown
| SyncLog-Pflicht fuer alle Sync-Services (Lifecycle + Events) | Fertig (v1.15.0) |
```

Roadmap-Zeile ergaenzen:
```markdown
- v1.15.0 (2026-05-26) — SyncLog-Pflicht fuer alle 8 Sync-Services. Neuer ISyncLogger mit DbContextFactory-Isolation.
```

- [ ] **Step 4: CLAUDE.md — Bekannter Fallstrick**

Im Block "Bekannte Fallstricke" einen neuen Punkt ergaenzen:

```markdown
- **SyncLogger nutzt IDbContextFactory, nicht den Scope-DbContext (seit v1.15.0)**: `ISyncLogger`/`ISyncRun` schreiben jede Zeile mit einem **frischen** DbContext (`IDbContextFactory<ApplicationDbContext>`). Grund: Diagnose-Logs duerfen nicht in Sync-Transaktionen mitrollen. Counts-Keys sind deutschsprachig (`"neu"`, `"aktualisiert"`, `"uebersprungen"`, `"fehler"`); neue Services sollen dasselbe Vokabular verwenden. Pattern siehe [secondbrain/docs/superpowers/specs/2026-05-26-synclog-pflicht-alle-syncs-design.md](secondbrain/docs/superpowers/specs/2026-05-26-synclog-pflicht-alle-syncs-design.md).
```

In der AppSettings-/ServiceSettings-Tabelle nichts zu aendern (keine neuen Settings).

- [ ] **Step 5: Build + komplette Test-Suite**

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Debug --nologo
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-restore -c Debug --nologo
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: alle Builds 0 Fehler, alle Tests bestanden.

- [ ] **Step 6: Commit + Push**

```bash
git add IdealAkeWms/Views/Help/Index.cshtml docs/TESTSZENARIEN.md PROJECT_STATUS.md CLAUDE.md
git commit -m "docs(synclog): help, testszenarien, project-status, claude.md for v1.15.0"
git push origin main
```

- [ ] **Step 7: Fortschritts-Sektion in PROJECT_STATUS.md aktualisieren**

Nach Task 17 alle ⏳-Marker auf ✅ setzen und ggf. konkrete Commit-Hashes verlinken.

```bash
# editiere PROJECT_STATUS.md inline, dann
git add PROJECT_STATUS.md
git commit -m "docs(project-status): mark synclog rollout sub-tasks done"
git push origin main
```

---

## Validierung am Schluss (alle Tasks fertig)

- [ ] **Final-Check 1:** `git log --oneline -25` zeigt 17+ atomare Commits, jeder mit klarer Message.
- [ ] **Final-Check 2:** `dotnet test` ueber beide Test-Projekte → alle gruen.
- [ ] **Final-Check 3:** Manuelle Verifikation: Service in DevEnv starten (Connection auf Test-DB), einen Tick durchlaufen lassen, im Sync-Protokoll-UI alle 9 Service-Namen mindestens 1x sehen.
- [ ] **Final-Check 4:** `git push origin main`.

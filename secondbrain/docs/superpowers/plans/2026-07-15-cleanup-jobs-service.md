# Cleanup-Jobs im Service — Aktivitätsprotokoll-Bereinigung — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ein erweiterbarer `CleanupWorker` im Windows-Service, der als ersten Job die `SyncLogs`-Tabelle („Aktivitäts-Protokoll") nach einer konfigurierbaren Aufbewahrung (Default 180 Tage, 0 = aus) täglich bereinigt.

**Architecture:** Neuer `CleanupWorker` (BackgroundService, 24h-Takt) liest die Aufbewahrung + DryRun aus den ServiceSettings und ruft `IActivityLogCleanupService.RunAsync(retentionDays, dryRun, ct)`. Der Service nutzt den reinen `ActivityLogCleanupPlanner` (Stichtag/deaktiviert-Entscheidung), löscht gebatcht via neue `ISyncLogRepository.DeleteOlderThanAsync` und schreibt einen eigenen Protokoll-Lauf. Der DB-Read der Settings liegt im Worker (Manual-UAT), die Löschlogik im Service (unit-testbar). Kein Schema-Change, kein Version-Bump (in v1.25.0 / Worktree `glas-bestellung` gefaltet).

**Tech Stack:** .NET 10, ASP.NET Core, EF Core 10 (InMemory für Tests), xUnit + FluentAssertions + Moq, Serilog. Spec: [2026-07-15-cleanup-jobs-service-design.md](../specs/2026-07-15-cleanup-jobs-service-design.md).

---

## File Structure

**Neu:**
- `IDEALAKEWMSService/Services/ActivityLogCleanupService.cs` — enthält `ActivityLogCleanupPlanner` (static, pure), `ActivityLogCleanupResult` (record), `IActivityLogCleanupService`, `ActivityLogCleanupService`. Eine kohäsive Einheit, kleine Datei.
- `IDEALAKEWMSService/Workers/CleanupWorker.cs` — neuer BackgroundService.
- `IDEALAKEWMSService.Tests/Services/ActivityLogCleanupPlannerTests.cs`
- `IDEALAKEWMSService.Tests/Services/ActivityLogCleanupServiceTests.cs`

**Geändert:**
- `IdealAkeWms/Data/Repositories/ISyncLogRepository.cs` + `SyncLogRepository.cs` — Methode `DeleteOlderThanAsync`.
- `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs` — Konstante `CleanupActivityLog` + `All`.
- `IdealAkeWms/Models/ServiceSettingDefinitions.cs` — neuer Key, Kategorie „Bereinigung".
- `IDEALAKEWMSService/Program.cs` — DI: `IActivityLogCleanupService` + `AddHostedService<CleanupWorker>`.
- `IdealAkeWms.Tests/Repositories/SyncLogRepositoryTests.cs` — DeleteOlderThan-Tests.
- `IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs` — InlineData.
- Doku: `IdealAkeWms/Views/Help/Changelog.cshtml`, `CLAUDE.md`, `docs/TESTSZENARIEN.md`, `PROJECT_STATUS.md`.

---

### Task 0: Pre-Flight Baseline

**Files:** keine Änderung.

- [ ] **Step 1: Baseline-Build + Tests grün**

Run:
```
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung"
dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly
dotnet test IdealAkeWms.Tests --nologo
dotnet test IDEALAKEWMSService.Tests --nologo
```
Expected: Build 0 Fehler; beide Testsuiten grün (Baseline notieren: Web ~942, Service ~146).

- [ ] **Step 2: Verifiziere Ist-Zustand der Anker-Dateien**

Bestätige, dass folgende Registrierungen/Signaturen existieren (keine Änderung, nur lesen): `IDEALAKEWMSService/Program.cs` registriert `ISyncLogRepository` (scoped), `ISyncLogger` (singleton), `IConfiguration`, `AddHostedService<SyncWorker>`/`<NotificationWorker>`. `ISyncLogRepository` hat aktuell nur `AddAsync/GetRecentAsync/GetPagedAsync`.

---

### Task 1: ServiceSetting-Key `Cleanup:AktivitaetsprotokollAufbewahrungTage` + Drift-Guard

**Files:**
- Modify: `IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs`
- Modify: `IdealAkeWms/Models/ServiceSettingDefinitions.cs`

- [ ] **Step 1: Failing Test — InlineData ergänzen**

In `ServiceSettingDefinitionsTests.cs` in der `[Theory]`-Liste von `All_ContainsDocumentedServiceReadKey` (nach der letzten bestehenden `[InlineData(...)]`, vor der Methoden-Signatur) einfügen:
```csharp
    [InlineData("Cleanup:AktivitaetsprotokollAufbewahrungTage")]
```

- [ ] **Step 2: Test rot**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~ServiceSettingDefinitionsTests"`
Expected: FAIL (Key noch nicht im Katalog).

- [ ] **Step 3: Key in den Katalog**

In `ServiceSettingDefinitions.cs` einen neuen Abschnitt direkt vor `// ----- Worker -----` einfügen:
```csharp
        // ----- Bereinigung (Cleanup-Jobs) -----
        new("Cleanup:AktivitaetsprotokollAufbewahrungTage", ServiceSettingType.Int, "180", "Bereinigung", "Aktivitaets-Protokoll: Eintraege aelter als X Tage werden taeglich geloescht (0 = nie loeschen)"),
```

- [ ] **Step 4: Test grün + Katalog-Konsistenztests grün**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~ServiceSettingDefinitionsTests"`
Expected: PASS (alle Theory-Fälle + die Bool/Int-Default-Parsebarkeit-Tests).

- [ ] **Step 5: Commit**

```
git add IdealAkeWms/Models/ServiceSettingDefinitions.cs IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs
git commit -m "feat(cleanup): ServiceSetting Cleanup:AktivitaetsprotokollAufbewahrungTage (Kategorie Bereinigung)"
```

---

### Task 2: `SyncLogServices.CleanupActivityLog`

**Files:**
- Modify: `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs`

- [ ] **Step 1: Konstante + All ergänzen**

Nach `public const string BdeAutoPause = "BdeAutoPause";` einfügen:
```csharp

    // Cleanup-Jobs (seit v1.25.0)
    public const string CleanupActivityLog = "CleanupAktivitaetsprotokoll";
```
Und in der `All`-Initialisierung `BdeAutoPause,` ergänzen zu:
```csharp
        PartRequisitionEmail, WarehouseRequisitionEmail, BdeAutoPause,
        CleanupActivityLog,
```

- [ ] **Step 2: Build**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo -clp:ErrorsOnly`
Expected: 0 Fehler. (`"CleanupAktivitaetsprotokoll"` = 27 Zeichen ≤ `SyncLog.Service` NVARCHAR(50).)

- [ ] **Step 3: Commit**

```
git add IdealAkeWms/Services/SyncLogger/SyncLogServices.cs
git commit -m "feat(cleanup): SyncLogServices.CleanupActivityLog Konstante"
```

---

### Task 3: `ISyncLogRepository.DeleteOlderThanAsync` (gebatcht, dryRun-bewusst)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/ISyncLogRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/SyncLogRepository.cs`
- Modify: `IdealAkeWms.Tests/Repositories/SyncLogRepositoryTests.cs`

- [ ] **Step 1: Failing Tests**

In `SyncLogRepositoryTests.cs` (bestehendes Setup/Helper der Datei verwenden — TestApplicationDbContext/InMemory) drei Tests ergänzen. Falls die Datei einen Helper für Kontext-Erzeugung hat, den nutzen; sonst analog zum bestehenden ersten Test in der Datei aufbauen. Testinhalt:
```csharp
    [Fact]
    public async Task DeleteOlderThanAsync_deletes_only_older_and_returns_count()
    {
        using var ctx = TestDbContextFactory.Create();
        var cutoff = new DateTime(2026, 06, 01);
        ctx.SyncLogs.AddRange(
            new SyncLog { Service = "X", Level = SyncLogLevel.Info, Message = "alt1",  Timestamp = cutoff.AddDays(-10) },
            new SyncLog { Service = "X", Level = SyncLogLevel.Info, Message = "alt2",  Timestamp = cutoff.AddDays(-1)  },
            new SyncLog { Service = "X", Level = SyncLogLevel.Info, Message = "neu",   Timestamp = cutoff.AddDays(1)   });
        await ctx.SaveChangesAsync();
        var repo = new SyncLogRepository(ctx);

        var deleted = await repo.DeleteOlderThanAsync(cutoff, batchSize: 5000, dryRun: false);

        deleted.Should().Be(2);
        (await ctx.SyncLogs.CountAsync()).Should().Be(1);
        (await ctx.SyncLogs.SingleAsync()).Message.Should().Be("neu");
    }

    [Fact]
    public async Task DeleteOlderThanAsync_dryRun_counts_but_does_not_delete()
    {
        using var ctx = TestDbContextFactory.Create();
        var cutoff = new DateTime(2026, 06, 01);
        ctx.SyncLogs.AddRange(
            new SyncLog { Service = "X", Level = SyncLogLevel.Info, Message = "alt", Timestamp = cutoff.AddDays(-1) },
            new SyncLog { Service = "X", Level = SyncLogLevel.Info, Message = "neu", Timestamp = cutoff.AddDays(1)  });
        await ctx.SaveChangesAsync();
        var repo = new SyncLogRepository(ctx);

        var wouldDelete = await repo.DeleteOlderThanAsync(cutoff, batchSize: 5000, dryRun: true);

        wouldDelete.Should().Be(1);
        (await ctx.SyncLogs.CountAsync()).Should().Be(2); // nichts geloescht
    }

    [Fact]
    public async Task DeleteOlderThanAsync_loops_over_batches()
    {
        using var ctx = TestDbContextFactory.Create();
        var cutoff = new DateTime(2026, 06, 01);
        for (var i = 0; i < 5; i++)
            ctx.SyncLogs.Add(new SyncLog { Service = "X", Level = SyncLogLevel.Info, Message = $"alt{i}", Timestamp = cutoff.AddDays(-1) });
        await ctx.SaveChangesAsync();
        var repo = new SyncLogRepository(ctx);

        var deleted = await repo.DeleteOlderThanAsync(cutoff, batchSize: 2, dryRun: false);

        deleted.Should().Be(5);
        (await ctx.SyncLogs.CountAsync()).Should().Be(0);
    }
```
> Hinweis Implementer: Prüfe die konkrete Kontext-Erzeugung in der bestehenden `SyncLogRepositoryTests.cs` (z.B. `TestDbContextFactory.Create()` vs. eigener Helper) und verwende exakt dasselbe Muster; passe die drei Tests entsprechend an. Ergänze fehlende `using`s (`Microsoft.EntityFrameworkCore` für `CountAsync/SingleAsync`).

- [ ] **Step 2: Tests rot**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~SyncLogRepositoryTests.DeleteOlderThanAsync"`
Expected: FAIL (Methode existiert nicht → Compile-Fehler).

- [ ] **Step 3: Interface + Implementierung**

In `ISyncLogRepository.cs` ergänzen:
```csharp
    Task<int> DeleteOlderThanAsync(DateTime cutoff, int batchSize, bool dryRun, CancellationToken ct = default);
```
In `SyncLogRepository.cs` ergänzen:
```csharp
    public async Task<int> DeleteOlderThanAsync(DateTime cutoff, int batchSize, bool dryRun, CancellationToken ct = default)
    {
        if (dryRun)
            return await _context.SyncLogs.CountAsync(x => x.Timestamp < cutoff, ct);

        var total = 0;
        while (true)
        {
            var batch = await _context.SyncLogs
                .Where(x => x.Timestamp < cutoff)
                .Take(batchSize)
                .ToListAsync(ct);
            if (batch.Count == 0) break;
            _context.SyncLogs.RemoveRange(batch);
            await _context.SaveChangesAsync(ct);
            total += batch.Count;
            if (batch.Count < batchSize) break;
        }
        return total;
    }
```

- [ ] **Step 4: Tests grün**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~SyncLogRepositoryTests"`
Expected: PASS (neue + bestehende).

- [ ] **Step 5: Commit**

```
git add IdealAkeWms/Data/Repositories/ISyncLogRepository.cs IdealAkeWms/Data/Repositories/SyncLogRepository.cs IdealAkeWms.Tests/Repositories/SyncLogRepositoryTests.cs
git commit -m "feat(cleanup): ISyncLogRepository.DeleteOlderThanAsync (gebatcht, dryRun-bewusst) + Tests"
```

---

### Task 4: `ActivityLogCleanupPlanner` (reine Logik) + Result + Interface

**Files:**
- Create: `IDEALAKEWMSService/Services/ActivityLogCleanupService.cs` (zunächst nur Planner + Result + Interface; Service-Klasse in Task 5)
- Create: `IDEALAKEWMSService.Tests/Services/ActivityLogCleanupPlannerTests.cs`

- [ ] **Step 1: Failing Tests (Planner)**

`ActivityLogCleanupPlannerTests.cs`:
```csharp
using System;
using FluentAssertions;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class ActivityLogCleanupPlannerTests
{
    [Fact]
    public void ComputeCutoff_positive_retention_subtracts_days()
    {
        var now = new DateTime(2026, 07, 15, 10, 0, 0);
        ActivityLogCleanupPlanner.ComputeCutoff(now, 180).Should().Be(now.AddDays(-180));
    }

    [Fact]
    public void ComputeCutoff_zero_retention_is_disabled()
        => ActivityLogCleanupPlanner.ComputeCutoff(new DateTime(2026, 07, 15), 0).Should().BeNull();

    [Fact]
    public void ComputeCutoff_negative_retention_is_disabled()
        => ActivityLogCleanupPlanner.ComputeCutoff(new DateTime(2026, 07, 15), -5).Should().BeNull();
}
```

- [ ] **Step 2: Test rot**

Run: `dotnet test IDEALAKEWMSService.Tests --nologo --filter "FullyQualifiedName~ActivityLogCleanupPlannerTests"`
Expected: FAIL (Typ existiert nicht → Compile-Fehler).

- [ ] **Step 3: Planner + Result + Interface anlegen**

`IDEALAKEWMSService/Services/ActivityLogCleanupService.cs`:
```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IDEALAKEWMSService.Services;

/// <summary>Reine Entscheidungslogik: berechnet den Stichtag oder "deaktiviert".</summary>
public static class ActivityLogCleanupPlanner
{
    /// <returns>Stichtag (loesche alles aelter); null = deaktiviert (retentionDays &lt;= 0).</returns>
    public static DateTime? ComputeCutoff(DateTime now, int retentionDays)
        => retentionDays <= 0 ? null : now.AddDays(-retentionDays);
}

public record ActivityLogCleanupResult(int Deleted, bool Skipped, string? SkipReason);

public interface IActivityLogCleanupService
{
    Task<ActivityLogCleanupResult> RunAsync(int retentionDays, bool dryRun, CancellationToken ct);
}
```

- [ ] **Step 4: Tests grün**

Run: `dotnet test IDEALAKEWMSService.Tests --nologo --filter "FullyQualifiedName~ActivityLogCleanupPlannerTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```
git add IDEALAKEWMSService/Services/ActivityLogCleanupService.cs IDEALAKEWMSService.Tests/Services/ActivityLogCleanupPlannerTests.cs
git commit -m "feat(cleanup): ActivityLogCleanupPlanner + Result + IActivityLogCleanupService"
```

---

### Task 5: `ActivityLogCleanupService` (Löschen + eigener Protokoll-Lauf)

**Files:**
- Modify: `IDEALAKEWMSService/Services/ActivityLogCleanupService.cs`
- Create: `IDEALAKEWMSService.Tests/Services/ActivityLogCleanupServiceTests.cs`

- [ ] **Step 1: Failing Tests (Service, Moq)**

`ActivityLogCleanupServiceTests.cs`:
```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class ActivityLogCleanupServiceTests
{
    private static (ActivityLogCleanupService svc, Mock<ISyncLogRepository> repo, Mock<ISyncLogger> logger, Mock<ISyncRun> run) Build()
    {
        var repo = new Mock<ISyncLogRepository>();
        var run = new Mock<ISyncRun>();
        var logger = new Mock<ISyncLogger>();
        logger.Setup(l => l.BeginRunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(run.Object);
        var svc = new ActivityLogCleanupService(repo.Object, logger.Object, NullLogger<ActivityLogCleanupService>.Instance);
        return (svc, repo, logger, run);
    }

    [Fact]
    public async Task RunAsync_disabled_skips_without_delete_or_run()
    {
        var (svc, repo, logger, _) = Build();

        var result = await svc.RunAsync(retentionDays: 0, dryRun: false, CancellationToken.None);

        result.Skipped.Should().BeTrue();
        result.Deleted.Should().Be(0);
        logger.Verify(l => l.BeginRunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.DeleteOlderThanAsync(It.IsAny<System.DateTime>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_enabled_deletes_and_writes_one_run_with_count()
    {
        var (svc, repo, logger, run) = Build();
        repo.Setup(r => r.DeleteOlderThanAsync(It.IsAny<System.DateTime>(), It.IsAny<int>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var result = await svc.RunAsync(retentionDays: 180, dryRun: false, CancellationToken.None);

        result.Skipped.Should().BeFalse();
        result.Deleted.Should().Be(7);
        logger.Verify(l => l.BeginRunAsync(SyncLogServices.CleanupActivityLog, It.IsAny<CancellationToken>()), Times.Once);
        run.Verify(r => r.FinishSuccessAsync(
            It.Is<IReadOnlyDictionary<string, int>>(d => d["geloescht"] == 7),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_dryRun_passes_dryRun_to_repo()
    {
        var (svc, repo, _, _) = Build();
        repo.Setup(r => r.DeleteOlderThanAsync(It.IsAny<System.DateTime>(), It.IsAny<int>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var result = await svc.RunAsync(retentionDays: 180, dryRun: true, CancellationToken.None);

        result.Deleted.Should().Be(3);
        repo.Verify(r => r.DeleteOlderThanAsync(It.IsAny<System.DateTime>(), It.IsAny<int>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
```
> Falls `IDEALAKEWMSService.Tests` noch keinen ProjectReference auf das Web-Projekt bzw. `Moq` hat: prüfen und ergänzen (die bestehenden Service-Tests wie `LagerbestandSyncServiceTests` nutzen Moq + Web-Typen, also i.d.R. bereits vorhanden).

- [ ] **Step 2: Test rot**

Run: `dotnet test IDEALAKEWMSService.Tests --nologo --filter "FullyQualifiedName~ActivityLogCleanupServiceTests"`
Expected: FAIL (Service-Klasse existiert noch nicht → Compile-Fehler).

- [ ] **Step 3: Service implementieren**

In `IDEALAKEWMSService/Services/ActivityLogCleanupService.cs` die `using`s ergänzen und die Klasse hinzufügen:
```csharp
using System.Collections.Generic;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.Extensions.Logging;
```
```csharp
public sealed class ActivityLogCleanupService : IActivityLogCleanupService
{
    private const int BatchSize = 5000;

    private readonly ISyncLogRepository _repo;
    private readonly ISyncLogger _syncLogger;
    private readonly ILogger<ActivityLogCleanupService> _logger;

    public ActivityLogCleanupService(ISyncLogRepository repo, ISyncLogger syncLogger,
        ILogger<ActivityLogCleanupService> logger)
    {
        _repo = repo;
        _syncLogger = syncLogger;
        _logger = logger;
    }

    public async Task<ActivityLogCleanupResult> RunAsync(int retentionDays, bool dryRun, CancellationToken ct)
    {
        // Stichtag aus DateTime.Now (Lokalzeit) — SyncLog.Timestamp wird als Lokalzeit gespeichert.
        var cutoff = ActivityLogCleanupPlanner.ComputeCutoff(DateTime.Now, retentionDays);
        if (cutoff is null)
        {
            _logger.LogDebug("Aktivitaetsprotokoll-Bereinigung deaktiviert (Aufbewahrung {Days} Tage <= 0).", retentionDays);
            return new ActivityLogCleanupResult(0, Skipped: true, SkipReason: "deaktiviert (Aufbewahrung <= 0)");
        }

        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.CleanupActivityLog, ct);
        try
        {
            var deleted = await _repo.DeleteOlderThanAsync(cutoff.Value, BatchSize, dryRun, ct);
            var suffix = $"Aufbewahrung {retentionDays} Tage, Stichtag {cutoff.Value:dd.MM.yyyy}"
                       + (dryRun ? " (DryRun — nichts geloescht)" : "");
            await run.FinishSuccessAsync(
                counts: new Dictionary<string, int> { ["geloescht"] = deleted },
                messageSuffix: suffix, ct: ct);
            return new ActivityLogCleanupResult(deleted, Skipped: false, SkipReason: null);
        }
        catch (Exception ex)
        {
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw; // Worker faengt es -> Serilog-Error + Fehlermail
        }
    }
}
```

- [ ] **Step 4: Tests grün**

Run: `dotnet test IDEALAKEWMSService.Tests --nologo --filter "FullyQualifiedName~ActivityLogCleanupServiceTests"`
Expected: PASS (3 Tests).

- [ ] **Step 5: Commit**

```
git add IDEALAKEWMSService/Services/ActivityLogCleanupService.cs IDEALAKEWMSService.Tests/Services/ActivityLogCleanupServiceTests.cs
git commit -m "feat(cleanup): ActivityLogCleanupService (loeschen + eigener Protokoll-Lauf) + Tests"
```

---

### Task 6: `CleanupWorker` (BackgroundService)

**Files:**
- Create: `IDEALAKEWMSService/Workers/CleanupWorker.cs`

- [ ] **Step 1: Worker anlegen**

`CleanupWorker.cs`:
```csharp
using IDEALAKEWMSService.Common;
using IDEALAKEWMSService.Services;

namespace IDEALAKEWMSService.Workers;

/// <summary>
/// Erweiterbarer Cleanup-Abschnitt. Laeuft taeglich (24h-Takt); jeder Cleaner ist
/// per ServiceSetting konfiguriert, DryRun-bewusst und resilient (ein Fehler killt
/// den Loop nicht). Weitere Cleaner: eigenen gegateten Block + Service + Setting ergaenzen.
/// </summary>
public class CleanupWorker : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);

    private readonly ILogger<CleanupWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    public CleanupWorker(ILogger<CleanupWorker> logger, IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CleanupWorker gestartet. Version {Version} ({Date}).",
            IDEALAKEWMSService.AppVersion.Version, IDEALAKEWMSService.AppVersion.Date);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var dryRun = await ServiceSettings.GetBoolSafeAsync(_configuration, "WorkerSettings:SyncDryRun", false, stoppingToken);
                await RunActivityLogCleanupAsync(dryRun, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Unerwarteter Fehler im CleanupWorker.");
                await NotifyErrorAsync("CleanupWorker (unerwartet)", ex, stoppingToken);
            }

            _logger.LogDebug("CleanupWorker: Naechster Durchlauf in {Hours}h.", RunInterval.TotalHours);
            await Task.Delay(RunInterval, stoppingToken);
        }

        _logger.LogInformation("CleanupWorker gestoppt.");
    }

    private async Task RunActivityLogCleanupAsync(bool dryRun, CancellationToken ct)
    {
        var retentionDays = await ServiceSettings.GetIntSafeAsync(
            _configuration, "Cleanup:AktivitaetsprotokollAufbewahrungTage", 180, ct);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IActivityLogCleanupService>();
            var result = await svc.RunAsync(retentionDays, dryRun, ct);
            if (result.Skipped)
                _logger.LogDebug("Aktivitaetsprotokoll-Bereinigung uebersprungen: {Reason}", result.SkipReason);
            else
                _logger.LogInformation("Aktivitaetsprotokoll-Bereinigung fertig: {Deleted} geloescht (DryRun={DryRun}).",
                    result.Deleted, dryRun);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aktivitaetsprotokoll-Bereinigung ist fehlgeschlagen.");
            await NotifyErrorAsync("Aktivitaetsprotokoll-Bereinigung", ex, ct);
        }
    }

    private async Task NotifyErrorAsync(string stepName, Exception ex, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var notifier = scope.ServiceProvider.GetRequiredService<ISyncErrorNotifier>();
            await notifier.NotifyAsync(stepName, ex, ct);
        }
        catch (Exception notifyEx)
        {
            _logger.LogError(notifyEx, "Fehlermail-Benachrichtigung fuer {Step} fehlgeschlagen.", stepName);
        }
    }
}
```
> Implementer: `ISyncErrorNotifier` liegt im Namespace `IDEALAKEWMSService.Services` (bereits via `using`). `GetRequiredService`/`CreateScope` brauchen `using Microsoft.Extensions.DependencyInjection;` — ergänzen falls der Build es verlangt (SyncWorker hat es implizit über GlobalUsings; ansonsten explizit hinzufügen).

- [ ] **Step 2: Build**

Run: `dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj --nologo -clp:ErrorsOnly`
Expected: 0 Fehler. (Noch nicht registriert → läuft noch nicht; das ist Task 7.)

- [ ] **Step 3: Commit**

```
git add IDEALAKEWMSService/Workers/CleanupWorker.cs
git commit -m "feat(cleanup): CleanupWorker (BackgroundService, taeglicher Takt, resilient)"
```

---

### Task 7: DI-Registrierung im Service

**Files:**
- Modify: `IDEALAKEWMSService/Program.cs`

- [ ] **Step 1: Service + Worker registrieren**

Bei den `AddScoped`-Service-Registrierungen (z.B. direkt nach `AddScoped<IBdeAutoPauseService, BdeAutoPauseService>();`) einfügen:
```csharp
    builder.Services.AddScoped<IActivityLogCleanupService, ActivityLogCleanupService>();
```
Bei den `AddHostedService`-Zeilen nach `AddHostedService<NotificationWorker>();` einfügen:
```csharp
    builder.Services.AddHostedService<CleanupWorker>();
```
> `using`s: `IDEALAKEWMSService.Services` (für `IActivityLogCleanupService`/`ActivityLogCleanupService`) und `IDEALAKEWMSService.Workers` (für `CleanupWorker`) — prüfen/ergänzen (Program.cs referenziert diese Namespaces bereits für die anderen Services/Worker).

- [ ] **Step 2: Build**

Run: `dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj --nologo -clp:ErrorsOnly`
Expected: 0 Fehler.

- [ ] **Step 3: Commit**

```
git add IDEALAKEWMSService/Program.cs
git commit -m "feat(cleanup): DI-Registrierung IActivityLogCleanupService + CleanupWorker"
```

---

### Task 8: Doku (Changelog / CLAUDE.md / TESTSZENARIEN / PROJECT_STATUS)

**Files:**
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `CLAUDE.md`
- Modify: `docs/TESTSZENARIEN.md`
- Modify: `PROJECT_STATUS.md`

- [ ] **Step 1: Changelog**

In der bestehenden **v1.25.0**-Sektion von `Changelog.cshtml` einen Punkt/Card ergänzen (Stil an vorhandene Einträge anpassen):
> **Aktivitäts-Protokoll-Bereinigung (neuer Cleanup-Abschnitt):** Ein neuer `CleanupWorker` im Windows-Service löscht täglich Aktivitäts-Protokoll-Einträge, die älter als die eingestellte Aufbewahrung sind (Server-Einstellung `Cleanup:AktivitaetsprotokollAufbewahrungTage`, Default 180 Tage, `0` = nie löschen). Kategorie „Bereinigung" unter Einstellungen → Service-Einstellungen. Grundlage für weitere Bereinigungs-Jobs.

- [ ] **Step 2: CLAUDE.md — Service-Config-Tabelle**

In `CLAUDE.md` in der Tabelle unter „Service-Konfiguration (appsettings.json / ServiceSettings DB)" eine Zeile ergänzen (bei den Sync/Worker-Keys):
```
| `Cleanup:AktivitaetsprotokollAufbewahrungTage` | `180` | Aktivitaets-Protokoll: Eintraege aelter als X Tage werden taeglich vom CleanupWorker geloescht (0 = nie loeschen) (v1.25.0) |
```

- [ ] **Step 3: CLAUDE.md — Fallstrick + Erweiterungsrezept**

Unter „Bekannte Fallstricke" einen neuen Punkt ergänzen:
```
- **Cleanup-Jobs im Service (v1.25.0)**: Der `CleanupWorker` (eigener BackgroundService, 24h-Takt, DryRun-bewusst, resilient) ist der erweiterbare „Bereinigung"-Abschnitt. Erster Cleaner: Aktivitaets-Protokoll (`SyncLogs`) — loescht Zeilen mit `Timestamp < DateTime.Now.AddDays(-N)` (N = `Cleanup:AktivitaetsprotokollAufbewahrungTage`, Default 180; **N<=0 = deaktiviert = still, kein Protokoll-Eintrag**). **Stichtag aus `DateTime.Now` (Lokalzeit), NICHT UtcNow** — konsistent mit der Timestamp-Speicherung. **Self-cleaning:** erst loeschen, dann `FinishSuccess`; der eigene Lauf-Eintrag ist neuer als der Stichtag. **Testbarkeit:** der DB-Read der Aufbewahrung liegt im `CleanupWorker` (Manual-UAT), der `ActivityLogCleanupService.RunAsync(retentionDays, dryRun, ct)` bekommt `retentionDays` als Parameter (unit-testbar); reine Stichtag-Entscheidung im `ActivityLogCleanupPlanner`; gebatchtes Loeschen (5000) via `ISyncLogRepository.DeleteOlderThanAsync` (Load-Batch+RemoveRange statt ExecuteDeleteAsync -> InMemory-testbar). **Weiteren Cleaner hinzufuegen:** (1) `Cleanup:XxxAufbewahrungTage` in `ServiceSettingDefinitions.All` + `InlineData` im Drift-Guard, (2) `SyncLogServices.CleanupXxx` (+ `All`), (3) `IXxxCleanupService.RunAsync(retentionDays,dryRun,ct)` + Repo-Delete + Planner, (4) gegateter Block im `CleanupWorker`, (5) Doku.
```

- [ ] **Step 4: CLAUDE.md — AppSettings/DB-first-Hinweis**

Falls in `CLAUDE.md` eine Liste der ServiceSettings-Kategorien/DB-first-Keys existiert: „Bereinigung" als neue Kategorie erwähnen (der Key ist DB-first wie die übrigen `Sync:`/`WorkerSettings:`-Keys, nur `ConnectionStrings:*`/`MailSettings:*` bleiben appsettings-only).

- [ ] **Step 5: TESTSZENARIEN**

In `docs/TESTSZENARIEN.md` ein neues Kapitel „Aktivitäts-Protokoll-Bereinigung" ergänzen (nächste freie Kapitelnummer). Szenarien:
  - **TS-x.1 Löschen aktiv:** Setting = 30; Protokoll enthält Einträge älter als 30 Tage → nach CleanupWorker-Lauf sind sie weg, jüngere bleiben; ein neuer „CleanupAktivitaetsprotokoll"-Lauf-Eintrag mit `geloescht=<N>` erscheint. (Manual-UAT — Takt 24h; zum sofortigen Test Service neu starten, er läuft beim Start einmal an.)
  - **TS-x.2 Deaktiviert:** Setting = 0 → kein Löschen, KEIN „CleanupAktivitaetsprotokoll"-Eintrag.
  - **TS-x.3 DryRun:** `WorkerSettings:SyncDryRun` = true, Setting = 30 → nichts gelöscht, Lauf-Eintrag mit Suffix „(DryRun — nichts geloescht)" und `geloescht=<Kandidatenzahl>`.
  - **TS-x.4 Editor:** `/ServiceSettings` zeigt die Kategorie „Bereinigung" mit dem Int-Feld; Speichern persistiert; nächster Lauf nutzt den neuen Wert (DB-first).

- [ ] **Step 6: PROJECT_STATUS**

In `PROJECT_STATUS.md` einen kurzen Fortschritts-Eintrag ergänzen (Cleanup-Abschnitt im Service, Aktivitäts-Protokoll-Bereinigung, v1.25.0-Fold).

- [ ] **Step 7: Commit**

```
git add IdealAkeWms/Views/Help/Changelog.cshtml CLAUDE.md docs/TESTSZENARIEN.md PROJECT_STATUS.md
git commit -m "docs(cleanup): Changelog + CLAUDE Fallstrick/Config + TESTSZENARIEN + PROJECT_STATUS"
```

---

### Task 9: Final-Check + Reviews

**Files:** keine Änderung (nur Verifikation).

- [ ] **Step 1: Voller Build**

Run: `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly`
Expected: 0 Fehler.

- [ ] **Step 2: Beide Testsuiten grün**

Run:
```
dotnet test IdealAkeWms.Tests --nologo
dotnet test IDEALAKEWMSService.Tests --nologo
```
Expected: Web-Tests = Baseline + 3 (SyncLogRepository) + 1 (InlineData) grün; Service-Tests = Baseline + 3 (Planner) + 3 (Service) grün. Keine Regression.

- [ ] **Step 3: Sanity-Check**

Bestätige: `SyncLogServices.All` enthält `CleanupActivityLog`; `ServiceSettingDefinitions.All` enthält `Cleanup:AktivitaetsprotokollAufbewahrungTage` (Kategorie „Bereinigung"); `IDEALAKEWMSService/Program.cs` registriert `IActivityLogCleanupService` **und** `AddHostedService<CleanupWorker>`.

- [ ] **Step 4: Spec-Review + Code-Review**

Spec-Compliance-Review (deckt die Implementierung §1–§5 der Spec ab?) + Code-Quality-Review über die neuen/geänderten Dateien.

- [ ] **Step 5: PAUSE für User-Test (NICHT autonom mergen)**

Zusammenfassung an den User + Manual-UAT-Anleitung (TESTSZENARIEN-Kapitel). **Kein Merge ohne ausdrückliche Freigabe.**

---

## Self-Review (durch den Plan-Autor)

- **Spec-Coverage:** §1 CleanupWorker → Task 6/7. §2 Cleaner (Setting/Planner/Service/Delete/Protokoll-Lauf) → Tasks 1,3,4,5. §3 Katalog/Seed/Drift → Task 1. §4 DI → Task 7. §5 Erweiterbarkeit → Task 8 (Rezept). Tests → Tasks 3,4,5. Doku → Task 8. ✓
- **Testbarkeit (Spec-Fallstrick #4):** gelöst — `retentionDays` als Methoden-Parameter; DB-Read im Worker. ✓
- **Fallstrick #7 (ISyncLogRepository-Registrierung):** im Pre-flight verifiziert — bereits registriert, kein Task nötig. ✓
- **Typ-Konsistenz:** `DeleteOlderThanAsync(DateTime, int, bool, CancellationToken)`, `ComputeCutoff(DateTime, int)→DateTime?`, `RunAsync(int, bool, CancellationToken)→ActivityLogCleanupResult`, `SyncLogServices.CleanupActivityLog`, Setting-Key `Cleanup:AktivitaetsprotokollAufbewahrungTage` — überall identisch. ✓
- **Kein Schema-Change/keine Migration/kein Version-Bump.** ✓

# FA-Zusatzinfos (Sage) — Fold 2: Auto-Erledigt + BDE-Buchungs-Sperre — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** FAs, die Sage als „verpackt"/„abgeholt" meldet, werden vom `FaZusatzinfoSyncService` automatisch auf Komm-Erledigt gesetzt (`PickingStatus.IsDonePicking`, einweg, mit Sicherheits-Cap), und das BDE-Terminal sperrt neue Buchungen (Start + Resume) auf solche FAs.

**Architecture:** Fold 2 der Spec `docs/superpowers/specs/2026-07-22-pa-zusatzinfos-design.md` **§10 (10.1–10.7, Rev. 6)** — Quelle der Wahrheit; bei jedem Zweifel dort nachlesen. Kein Schema-Change, keine Migration, kein Versions-Bump (bleibt v1.26.0). Zwei Wirkungen, EINE Status-Wahrheit: neuer statischer Helper `FaZusatzinfoStatus` (Web-Projekt, Service nutzt ihn via ProjectReference). Auto-Erledigt läuft im bestehenden Sync-Lauf (zweiphasig: Kandidaten sammeln → Cap prüfen → Writes), die BDE-Sperre ist ein reiner Request-Guard im `BdeBookingService` (Start/Resume, NICHT Beenden/Pausieren/Mengen) plus Listen-Hygiene (NurFA-Query + `GetOpenByWorkplaceIdAsync`-Opt-in-Filter) plus JS-Toast-Fix.

**Tech Stack:** ASP.NET Core 10.0 MVC, EF Core 10.0 (InMemory für Tests), xUnit + FluentAssertions + Moq, Vanilla JS (`bde-terminal.js`).

**Worktree:** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\pa-zusatzinfos` (Branch `feature/pa-zusatzinfos`, Fold 1 §1–§9 komplett committet). Alle Pfade unten relativ zu diesem Worktree.

**Build:** `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly`
**Tests:** `dotnet test IdealAkeWms.Tests --nologo [--filter ...]` bzw. `dotnet test IDEALAKEWMSService.Tests --nologo [--filter ...]`

---

## File Structure

**Neu:**

| Datei | Verantwortung |
|---|---|
| `IdealAkeWms/Services/FaZusatzinfoStatus.cs` | Gemeinsame Status-Wahrheit: Konstanten `Verpackt`/`Abgeholt` + `IstVerpacktOderAbgeholt(string?)` (Trim + OrdinalIgnoreCase). Genutzt von Sync (Service-Projekt) UND `BdeBookingService`. EF-Queries nutzen NUR die Konstanten inline (statischer Helper ist nicht EF-übersetzbar). |
| `IdealAkeWms.Tests/Services/FaZusatzinfoStatusTests.cs` | Unit-Tests des Helpers (Theory: Treffer/Nicht-Treffer/Trim/Case/null/leer). |

**Geändert:**

| Datei | Änderung |
|---|---|
| `IdealAkeWms/Models/ServiceSettingDefinitions.cs` | Cap-Key `Sync:FaZusatzinfoAutoDoneMaxPerRun` (Int, Default 100) + ACHTUNG-Zusatz in der Beschreibung von `Sync:FaZusatzinfoEnabled`. |
| `IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs` | Drift-Guard-`InlineData` für den Cap-Key. |
| `IDEALAKEWMSService/Services/IFaZusatzinfoSyncService.cs` | Signatur `SyncAsync(bool dryRun, int autoDoneMaxPerRun, CancellationToken ct = default)`. |
| `IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs` | Auto-Erledigt (§10.1–10.3): `ISyncErrorNotifier`-Injektion, `.Include(PickingStatus)`, zweiphasige Kandidaten + Cap, Counts `erledigt-gesetzt`/`erledigt-kandidaten` an allen 3 Stellen, Info-Detailzeilen (Cap 100, mit `reference`), Docblock-Fix. |
| `IDEALAKEWMSService/Workers/SyncWorker.cs` | Cap via `ServiceSettings.GetIntSafeAsync` lesen (INNERHALB des gegateten Blocks) + als Parameter durchreichen. |
| `IDEALAKEWMSService.Tests/Services/FaZusatzinfoSyncServiceTests.cs` | Build-Tupel um `Mock<ISyncErrorNotifier>` erweitert, bestehende 8 Tests mechanisch angepasst, +14 neue Auto-Erledigt-Tests (§10.4), Seed-Helper `SeedPickingStatus`, `SeedOrder` um `isDone`/`isCancelled` erweitert. |
| `IdealAkeWms/Services/BdeBookingService.cs` | Guard `EnsureOrderNotPackedAsync` in `StartPlannedAsync` (nach Werkbank-Gate) + `ResumeAsync` (nach Parent-Load, nur `parent.WorkOperationId.HasValue`). |
| `IdealAkeWms.Tests/Helpers/BdeBookingTestSeed.cs` | Seed-Helper `SetSageStatusAsync` (ExtraInfo-Satellit). |
| `IdealAkeWms.Tests/Services/BdeBookingServiceTests.cs` | +10 Guard-Tests (§10.6 Fälle 1–7, 9, 10). |
| `IdealAkeWms/Data/Repositories/IWorkOperationRepository.cs` + `WorkOperationRepository.cs` | `GetOpenByWorkplaceIdAsync(workplaceId, excludePackedOrders = false)` — Filter IN der EF-Query, ausgeschriebenes Null-Guard-Prädikat. |
| `IdealAkeWms/Controllers/BdeApiController.cs` | Listen-Hygiene: NurFA-Query filtert verpackt/abgeholt aus (~Z.171); `GetOpenByWorkplaceIdAsync(..., excludePackedOrders: true)` (~Z.203). |
| `IdealAkeWms.Tests/Controllers/BdeApiControllerTests.cs` | 4 Moq-Setups auf 2-Parameter-Signatur (`ids.WorkplaceId, true`) + neuer NurFA-Hygiene-Test. |
| `IdealAkeWms.Tests/Repositories/WorkOperationRepositoryExtendedTests.cs` | +2 Tests (excludePackedOrders filtert; Default-Parameter = Tracking bleibt ungefiltert). |
| `IdealAkeWms/wwwroot/js/bde-terminal.js` | `post()`: neuer `InvalidState`-Zweig → `showToast(json.message \|\| 'Aktion nicht möglich', 'danger')`. KEIN catch-all-else. |
| Doku (Task 4) | `Views/Help/Changelog.cshtml`, `Views/Help/Index.cshtml` (4 Stellen), `docs/TESTSZENARIEN.md` (Kap.-55-Umbau + TS-55.10–55.13), `CLAUDE.md`, `README.md`, `PROJECT_STATUS.md`, Spec §7 Schritt 4 — plus identische Spiegelung `docs/` → `secondbrain/docs/` (Worktree OHNE Junctions!). |

**Wichtige Code-Realitäten (beim Implementieren beachten):**

- `TrackingController.cs:153` ruft `GetOpenByWorkplaceIdAsync(id)` — bleibt via Default-Parameter unverändert (Tracking bewusst ungefiltert).
- Moq-Expression-Trees dürfen KEINE Calls mit optionalen Argumenten enthalten → alle `_workOps.Setup(r => r.GetOpenByWorkplaceIdAsync(...))`-Setups MÜSSEN beide Argumente explizit angeben.
- `SyncWorkerTests` mockt `IFaZusatzinfoSyncService` NICHT (nur ein `GetService`-`Times.Never`-Verify) → dort ist trotz Signaturänderung KEINE Anpassung nötig (siehe „Abweichungen").
- Der Resume-Pfad in `bde-terminal.js` (Z.495–544) nutzt `post()` NICHT (eigener fetch, zeigt `data.message` bereits an) — nur `post()` braucht den neuen Zweig.
- SQL-Tabellenname des PickingStatus ist **`ProductionOrderPickingStatus`** (singular), der DbSet heißt `ProductionOrderPickingStatuses`.
- `FakeSyncRun.Events` ist `List<(string Level, string Message, string? Reference)>` — Detailzeilen-Asserts laufen über `e.Reference`.

---

## Task 0: Pre-Flight — Baseline grün

**Files:** keine Änderungen.

- [ ] **Step 1: Worktree + Branch verifizieren**

Run: `git -C C:\Git\IDEAL-AKE-WMS\.claude\worktrees\pa-zusatzinfos status --short && git -C C:\Git\IDEAL-AKE-WMS\.claude\worktrees\pa-zusatzinfos branch --show-current`
Expected: Branch `feature/pa-zusatzinfos`, working tree sauber (keine unerwarteten Änderungen).

- [ ] **Step 2: Baseline-Build**

Run: `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly`
Expected: Exit 0, keine Fehler.

- [ ] **Step 3: Beide Testsuiten grün**

Run: `dotnet test IdealAkeWms.Tests --nologo` und `dotnet test IDEALAKEWMSService.Tests --nologo`
Expected: Alle Tests grün (Stand Fold 1: 977 Web + 162 Service). Bei Abweichung: STOPP, erst Baseline klären.

---

## Task 1: `FaZusatzinfoStatus`-Helper (TDD) + Katalog + Drift-Guard

**Files:**
- Create: `IdealAkeWms.Tests/Services/FaZusatzinfoStatusTests.cs`
- Create: `IdealAkeWms/Services/FaZusatzinfoStatus.cs`
- Modify: `IdealAkeWms/Models/ServiceSettingDefinitions.cs:32`
- Modify: `IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs:79`

- [ ] **Step 1: Failing Tests schreiben (RED)**

Neue Datei `IdealAkeWms.Tests/Services/FaZusatzinfoStatusTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Services;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// Fold 2 (v1.26.0, Spec §10.6): gemeinsame Status-Wahrheit fuer "verpackt"/"abgeholt" —
/// genutzt vom FaZusatzinfoSyncService (Auto-Erledigt) UND der BDE-Buchungs-Sperre.
/// </summary>
public class FaZusatzinfoStatusTests
{
    [Theory]
    [InlineData("verpackt")]
    [InlineData("abgeholt")]
    [InlineData("Verpackt")]
    [InlineData("ABGEHOLT")]
    [InlineData(" Abgeholt ")]
    [InlineData("  verpackt  ")]
    public void IstVerpacktOderAbgeholt_Matches(string status)
    {
        FaZusatzinfoStatus.IstVerpacktOderAbgeholt(status).Should().BeTrue();
    }

    [Theory]
    [InlineData("in Produktion")]
    [InlineData("offen")]
    [InlineData("verpackt und mehr")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IstVerpacktOderAbgeholt_NoMatch(string? status)
    {
        FaZusatzinfoStatus.IstVerpacktOderAbgeholt(status).Should().BeFalse();
    }

    [Fact]
    public void Konstanten_SindLowercase_FuerInlineEfVergleiche()
    {
        // EF-Listen-Filter vergleichen .Trim().ToLower() gegen die Konstanten —
        // die Konstanten MUESSEN deshalb lowercase sein.
        FaZusatzinfoStatus.Verpackt.Should().Be("verpackt");
        FaZusatzinfoStatus.Abgeholt.Should().Be("abgeholt");
    }
}
```

- [ ] **Step 2: Tests laufen lassen — Compile-Fehler erwartet (RED)**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~FaZusatzinfoStatusTests"`
Expected: Build-FEHLER `CS0103: The name 'FaZusatzinfoStatus' does not exist ...` — das ist der RED-Beweis.

- [ ] **Step 3: Helper implementieren (GREEN)**

Neue Datei `IdealAkeWms/Services/FaZusatzinfoStatus.cs`:

```csharp
namespace IdealAkeWms.Services;

/// <summary>
/// Fold 2 (v1.26.0, Spec §10.6): gemeinsame Status-Wahrheit fuer den Sage-Status
/// "verpackt"/"abgeholt". Genutzt vom FaZusatzinfoSyncService (Auto-Erledigt,
/// Service-Projekt via ProjectReference) UND vom BdeBookingService (BDE-Sperre).
/// ACHTUNG: EF-Listen-Filter koennen den statischen Helper NICHT verwenden (nicht
/// EF-uebersetzbar) — dort die Konstanten inline mit dem ausgeschriebenen
/// Null-Guard-Praedikat vergleichen (.Trim().ToLower() != Verpackt/Abgeholt).
/// </summary>
public static class FaZusatzinfoStatus
{
    public const string Verpackt = "verpackt";
    public const string Abgeholt = "abgeholt";

    public static bool IstVerpacktOderAbgeholt(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;
        var s = status.Trim();
        return s.Equals(Verpackt, StringComparison.OrdinalIgnoreCase)
            || s.Equals(Abgeholt, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 4: Tests laufen lassen (GREEN)**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~FaZusatzinfoStatusTests"`
Expected: PASS (13 Tests).

- [ ] **Step 5: Drift-Guard-InlineData ergänzen (RED für den Katalog)**

In `IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs` direkt NACH Zeile 79 (`[InlineData("Sync:FaZusatzinfoEnabled")]`) einfügen:

```csharp
    [InlineData("Sync:FaZusatzinfoAutoDoneMaxPerRun")]
```

- [ ] **Step 6: Drift-Guard laufen lassen — Fehlschlag erwartet (RED)**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~ServiceSettingDefinitionsTests"`
Expected: FAIL genau für den neuen InlineData-Fall (`Sync:FaZusatzinfoAutoDoneMaxPerRun` nicht im Katalog).

- [ ] **Step 7: Katalog erweitern (GREEN)**

In `IdealAkeWms/Models/ServiceSettingDefinitions.cs` Zeile 32 ersetzen. Alt:

```csharp
        new("Sync:FaZusatzinfoEnabled",              ServiceSettingType.Bool, "false", "Sync", "FA-Zusatzinfos (Sage): Kaeltemittel/Ventil/Ausfuehrung/Maschine/Status je FA synchronisieren"),
```

Neu (zwei Zeilen):

```csharp
        new("Sync:FaZusatzinfoEnabled",              ServiceSettingType.Bool, "false", "Sync", "FA-Zusatzinfos (Sage): Kaeltemittel/Ventil/Ausfuehrung/Maschine/Status je FA synchronisieren. ACHTUNG: setzt FAs mit Sage-Status verpackt/abgeholt automatisch auf Komm-Erledigt (vorher DryRun pruefen)"),
        new("Sync:FaZusatzinfoAutoDoneMaxPerRun",    ServiceSettingType.Int,  "100",   "Sync", "Sicherheits-Cap: mehr Auto-Erledigt-Kandidaten (Sage-Status verpackt/abgeholt) je Lauf -> kein Erledigt-Setzen + Warnung + Fehlermail (Schutz vor View-Defekten)"),
```

- [ ] **Step 8: Drift-Guard + Web-Suite laufen lassen (GREEN)**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~ServiceSettingDefinitionsTests"`
Expected: PASS. Danach Voll-Build: `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly` → Exit 0.

- [ ] **Step 9: Commit**

```bash
git add IdealAkeWms/Services/FaZusatzinfoStatus.cs IdealAkeWms.Tests/Services/FaZusatzinfoStatusTests.cs IdealAkeWms/Models/ServiceSettingDefinitions.cs IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs
git commit -m "feat(fa-zusatzinfo): Fold-2-Basis — FaZusatzinfoStatus-Helper + Cap-Key Sync:FaZusatzinfoAutoDoneMaxPerRun

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2: Auto-Erledigt im `FaZusatzinfoSyncService` (§10.1–§10.4) + SyncWorker-Cap

**Files:**
- Modify: `IDEALAKEWMSService/Services/IFaZusatzinfoSyncService.cs`
- Modify: `IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs` (kompletter Datei-Ersatz in Step 4)
- Modify: `IDEALAKEWMSService/Workers/SyncWorker.cs:62-75`
- Modify: `IDEALAKEWMSService.Tests/Services/FaZusatzinfoSyncServiceTests.cs`

- [ ] **Step 1: Signatur + DI mechanisch erweitern (Suite bleibt grün)**

**(a)** `IDEALAKEWMSService/Services/IFaZusatzinfoSyncService.cs` — kompletter neuer Inhalt:

```csharp
namespace IDEALAKEWMSService.Services;

public interface IFaZusatzinfoSyncService
{
    /// <summary>
    /// <paramref name="autoDoneMaxPerRun"/>: Sicherheits-Cap fuer das Auto-Erledigt
    /// (Spec §10.2) — mehr Kandidaten je Lauf → kein Erledigt-Write + Warn + Fehlermail.
    /// Wert kommt als Parameter vom SyncWorker (DB-Read via ServiceSettings),
    /// damit der Service ohne DB-Settings unit-testbar bleibt.
    /// </summary>
    Task<SyncResult> SyncAsync(bool dryRun, int autoDoneMaxPerRun, CancellationToken ct = default);
}
```

**(b)** `IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs` — NUR mechanisch (Logik kommt in Step 4):

Felder/Konstruktor (Notifier-Position VOR `ILogger`, `ISyncLogger` bleibt letzter — v1.15.2-Konvention). Alt (Z.22-37):

```csharp
    private readonly ApplicationDbContext _ctx;
    private readonly ISageZusatzinfoReader _reader;
    private readonly ILogger<FaZusatzinfoSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public FaZusatzinfoSyncService(
        ApplicationDbContext ctx,
        ISageZusatzinfoReader reader,
        ILogger<FaZusatzinfoSyncService> logger,
        ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _reader = reader;
        _logger = logger;
        _syncLogger = syncLogger;
    }
```

Neu:

```csharp
    private readonly ApplicationDbContext _ctx;
    private readonly ISageZusatzinfoReader _reader;
    private readonly ISyncErrorNotifier _errorNotifier;
    private readonly ILogger<FaZusatzinfoSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public FaZusatzinfoSyncService(
        ApplicationDbContext ctx,
        ISageZusatzinfoReader reader,
        ISyncErrorNotifier errorNotifier,
        ILogger<FaZusatzinfoSyncService> logger,
        ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _reader = reader;
        _errorNotifier = errorNotifier;
        _logger = logger;
        _syncLogger = syncLogger;
    }
```

Methodensignatur (Z.39) alt: `public async Task<SyncResult> SyncAsync(bool dryRun, CancellationToken ct = default)` → neu:

```csharp
    public async Task<SyncResult> SyncAsync(bool dryRun, int autoDoneMaxPerRun, CancellationToken ct = default)
```

(Der Parameter bleibt in diesem Step ungenutzt — Warnung gibt es dafür nicht, Build bleibt grün. DI: `ISyncErrorNotifier` ist in `IDEALAKEWMSService/Program.cs:66` bereits als Scoped registriert — die Typ-Registrierung `AddScoped<IFaZusatzinfoSyncService, FaZusatzinfoSyncService>()` in Z.55 löst den neuen Ctor-Parameter automatisch auf, KEINE Program.cs-Änderung.)

**(c)** `IDEALAKEWMSService/Workers/SyncWorker.cs` — den FA-Zusatzinfo-Block (Z.62-75) ersetzen. Alt:

```csharp
                if (await ServiceSettings.GetBoolSafeAsync(_configuration, "Sync:FaZusatzinfoEnabled", false, stoppingToken))
                {
                    await RunResilientAsync("FA-Zusatzinfo-Sync", async () =>
                    {
                        var zusatzinfoSync = scope.ServiceProvider.GetRequiredService<IFaZusatzinfoSyncService>();

                        _logger.LogInformation("FA-Zusatzinfo-Sync startet...");
                        var ziResult = await zusatzinfoSync.SyncAsync(dryRun, stoppingToken);
```

Neu (Rest des Blocks — LogInformation nach dem Aufruf + schließende Klammern — bleibt unverändert):

```csharp
                if (await ServiceSettings.GetBoolSafeAsync(_configuration, "Sync:FaZusatzinfoEnabled", false, stoppingToken))
                {
                    await RunResilientAsync("FA-Zusatzinfo-Sync", async () =>
                    {
                        var zusatzinfoSync = scope.ServiceProvider.GetRequiredService<IFaZusatzinfoSyncService>();
                        // Cap DB-first lesen (Default 100) und als Parameter durchreichen
                        // (Muster ActivityLogCleanupService.RunAsync(retentionDays, ...)).
                        var autoDoneCap = await ServiceSettings.GetIntSafeAsync(
                            _configuration, "Sync:FaZusatzinfoAutoDoneMaxPerRun", 100, stoppingToken);

                        _logger.LogInformation("FA-Zusatzinfo-Sync startet...");
                        var ziResult = await zusatzinfoSync.SyncAsync(dryRun, autoDoneCap, stoppingToken);
```

**(d)** `IDEALAKEWMSService.Tests/Services/FaZusatzinfoSyncServiceTests.cs` — mechanische Anpassungen:

1. `using Moq;` zu den usings ergänzen.
2. `Build()` ersetzen. Alt (Z.19-29):

```csharp
    private static (FaZusatzinfoSyncService svc, FakeSageZusatzinfoReader reader,
                    IdealAkeWms.Data.ApplicationDbContext ctx, FakeSyncLogger fakeLogger)
        Build()
    {
        var ctx = TestDbContextFactory.Create();
        var reader = new FakeSageZusatzinfoReader();
        var fakeLogger = new FakeSyncLogger();
        var svc = new FaZusatzinfoSyncService(
            ctx, reader, NullLogger<FaZusatzinfoSyncService>.Instance, fakeLogger);
        return (svc, reader, ctx, fakeLogger);
    }
```

Neu:

```csharp
    private static (FaZusatzinfoSyncService svc, FakeSageZusatzinfoReader reader,
                    IdealAkeWms.Data.ApplicationDbContext ctx, FakeSyncLogger fakeLogger,
                    Mock<ISyncErrorNotifier> notifier)
        Build()
    {
        var ctx = TestDbContextFactory.Create();
        var reader = new FakeSageZusatzinfoReader();
        var fakeLogger = new FakeSyncLogger();
        var notifier = new Mock<ISyncErrorNotifier>();
        var svc = new FaZusatzinfoSyncService(
            ctx, reader, notifier.Object, NullLogger<FaZusatzinfoSyncService>.Instance, fakeLogger);
        return (svc, reader, ctx, fakeLogger, notifier);
    }
```

3. In ALLEN 8 Bestandstests (Zeilen 73–231): `var (svc, reader, ctx, fakeLogger) = Build();` → `var (svc, reader, ctx, fakeLogger, _) = Build();` (Replace-All, 8 Treffer).
4. Alle Aufrufe erweitern (Replace-All): `svc.SyncAsync(dryRun: false)` → `svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100)` (7 Treffer, davon 2 in `act`-Lambdas) und `svc.SyncAsync(dryRun: true)` → `svc.SyncAsync(dryRun: true, autoDoneMaxPerRun: 100)` (1 Treffer).

**Hinweis SyncWorkerTests:** KEINE Änderung nötig — `SyncWorker_SkipsFaZusatzinfoSync_WhenDbUnreachable` verifiziert nur `GetService(typeof(IFaZusatzinfoSyncService))` mit `Times.Never()` (kein Setup auf `SyncAsync`, also keine Signatur-Abhängigkeit).

- [ ] **Step 2: Build + beide Suiten — mechanischer Umbau grün**

Run: `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly` dann `dotnet test IDEALAKEWMSService.Tests --nologo`
Expected: Build Exit 0, alle Service-Tests grün (Verhalten unverändert — Parameter noch ungenutzt).

- [ ] **Step 3: Die 14 Auto-Erledigt-Tests schreiben (RED)**

An `IDEALAKEWMSService.Tests/Services/FaZusatzinfoSyncServiceTests.cs` — zuerst die Seed-Helper erweitern. `SeedOrder` (Z.31-43) ersetzen durch:

```csharp
    private static ProductionOrder SeedOrder(IdealAkeWms.Data.ApplicationDbContext ctx, string orderNumber,
        bool isDone = false, bool isCancelled = false)
    {
        var order = new ProductionOrder
        {
            OrderNumber = orderNumber,
            IsDone = isDone,
            IsCancelled = isCancelled,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.ProductionOrders.Add(order);
        ctx.SaveChanges();
        return order;
    }
```

Direkt nach `SeedExtraInfo` neuen Helper einfügen:

```csharp
    private static ProductionOrderPickingStatus SeedPickingStatus(
        IdealAkeWms.Data.ApplicationDbContext ctx, int productionOrderId, bool isDonePicking)
    {
        var ps = new ProductionOrderPickingStatus
        {
            ProductionOrderId = productionOrderId,
            IsDonePicking = isDonePicking,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.ProductionOrderPickingStatuses.Add(ps);
        ctx.SaveChanges();
        return ps;
    }
```

Dann ans Klassenende die 14 Tests (§10.4 Fälle 1–14) anfügen:

```csharp
    // ===== Fold 2 (Spec §10.1-§10.4): Auto-Erledigt bei Sage-Status verpackt/abgeholt =====

    // §10.4-1) Status abgeholt + FA offen -> IsDonePicking=true, Audit, Count, Info-Zeile mit reference
    [Fact]
    public async Task AutoDone_StatusAbgeholt_OpenFa_SetsIsDonePicking()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, order.Id, isDonePicking: false);
        reader.Rows = new() { Row("WA-1", status: "abgeholt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        var ps = ctx.ProductionOrderPickingStatuses.Single();
        ps.IsDonePicking.Should().BeTrue();
        ps.ModifiedAt.Should().NotBeNull();
        ps.ModifiedBy.Should().Be(SyncUser);
        ps.ModifiedByWindows.Should().Be(SyncUser);
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(1);
        fakeLogger.Runs[0].Events.Should().Contain(e =>
            e.Level == "Info"
            && e.Message.Contains("auf erledigt gesetzt")
            && e.Message.Contains("abgeholt")
            && e.Reference == "WA-1");
    }

    // §10.4-2) Status verpackt -> dito
    [Fact]
    public async Task AutoDone_StatusVerpackt_OpenFa_SetsIsDonePicking()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, order.Id, isDonePicking: false);
        reader.Rows = new() { Row("WA-1", status: "verpackt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        ctx.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeTrue();
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(1);
    }

    // §10.4-3) FA bereits IsDonePicking -> kein Write, kein Count
    [Fact]
    public async Task AutoDone_AlreadyDonePicking_NoWrite_NoCount()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, order.Id, isDonePicking: true);
        reader.Rows = new() { Row("WA-1", status: "abgeholt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        var ps = ctx.ProductionOrderPickingStatuses.Single();
        ps.ModifiedAt.Should().BeNull(); // kein Blind-Update
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(0);
    }

    // §10.4-4) Sage-IsDone -> kein Write, kein Count (Sage-IsDone wird NIE beschrieben)
    [Fact]
    public async Task AutoDone_SageIsDone_NoWrite_NoCount()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1", isDone: true);
        SeedPickingStatus(ctx, order.Id, isDonePicking: false);
        reader.Rows = new() { Row("WA-1", status: "abgeholt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        ctx.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeFalse();
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(0);
    }

    // §10.4-5) IsCancelled -> kein Write, kein Count (reaktivierte FA soll nicht dauerhaft versteckt zurueckkommen)
    [Fact]
    public async Task AutoDone_Cancelled_NoWrite_NoCount()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1", isCancelled: true);
        SeedPickingStatus(ctx, order.Id, isDonePicking: false);
        reader.Rows = new() { Row("WA-1", status: "abgeholt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        ctx.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeFalse();
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(0);
    }

    // §10.4-6) Anderer Status -> nichts
    [Fact]
    public async Task AutoDone_OtherStatus_NoWrite()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, order.Id, isDonePicking: false);
        reader.Rows = new() { Row("WA-1", status: "in Produktion") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        ctx.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeFalse();
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(0);
    }

    // §10.4-7) Rueckfall: bereits erledigt + Status jetzt "in Produktion" -> bleibt erledigt (Einweg)
    [Fact]
    public async Task AutoDone_StatusFellBack_StaysDone()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, order.Id, isDonePicking: true);
        SeedExtraInfo(ctx, order.Id, status: "abgeholt");
        reader.Rows = new() { Row("WA-1", status: "in Produktion") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        ctx.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeTrue();
        ctx.ProductionOrderExtraInfos.Single().SageStatus.Should().Be("in Produktion"); // Satellit folgt Sage
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(0);
    }

    // §10.4-8) Ping-Pong: manuell wieder geoeffnet + Status weiterhin abgeholt -> wird erneut geschlossen
    [Fact]
    public async Task AutoDone_PingPong_ReclosesAfterManualReopen()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        var ps = SeedPickingStatus(ctx, order.Id, isDonePicking: false); // manuell wieder geoeffnet
        ps.ModifiedBy = "handbenutzer";
        ctx.SaveChanges();
        SeedExtraInfo(ctx, order.Id, status: "abgeholt");
        reader.Rows = new() { Row("WA-1", status: "abgeholt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        var after = ctx.ProductionOrderPickingStatuses.Single();
        after.IsDonePicking.Should().BeTrue();
        after.ModifiedBy.Should().Be(SyncUser); // Sync hat erneut geschlossen
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(1);
    }

    // §10.4-9) DryRun: Count gesetzt, DB unveraendert (weder Flag noch neue Zeile)
    [Fact]
    public async Task AutoDone_DryRun_Counts_NoWrites()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var withPs = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, withPs.Id, isDonePicking: false);
        var withoutPs = SeedOrder(ctx, "WA-2");
        reader.Rows = new() { Row("WA-1", status: "abgeholt"), Row("WA-2", status: "verpackt") };

        await svc.SyncAsync(dryRun: true, autoDoneMaxPerRun: 100);

        ctx.ProductionOrderPickingStatuses.Should().HaveCount(1);        // keine neue Zeile
        ctx.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeFalse(); // kein Flag
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(2); // echte Would-be-Zahl
        fakeLogger.Runs[0].FinalMessageSuffix.Should().Be("[DryRun]");
    }

    // §10.4-10) PickingStatus-Zeile fehlt (Altbestand) -> wird mit IsDonePicking=true angelegt
    [Fact]
    public async Task AutoDone_MissingPickingStatusRow_CreatesRow()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        reader.Rows = new() { Row("WA-1", status: "abgeholt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        var ps = ctx.ProductionOrderPickingStatuses.Single();
        ps.ProductionOrderId.Should().Be(order.Id);
        ps.IsDonePicking.Should().BeTrue();
        ps.CreatedBy.Should().Be(SyncUser);
        ps.CreatedByWindows.Should().Be(SyncUser);
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(1);
    }

    // §10.4-11) Case/Trim: " Abgeholt " -> greift
    [Fact]
    public async Task AutoDone_CaseAndTrim_Matches()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, order.Id, isDonePicking: false);
        reader.Rows = new() { Row("WA-1", status: " Abgeholt ") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        ctx.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeTrue();
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(1);
    }

    // §10.4-12) Unveraendert-Zweig: ExtraInfo identisch + Status abgeholt + FA offen -> trotzdem erledigt
    //           (die if/else-if-Upsert-Kette hat kein continue — der Done-Check laeuft IMMER)
    [Fact]
    public async Task AutoDone_UnchangedUpsertBranch_StillSetsDone()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, order.Id, isDonePicking: false);
        SeedExtraInfo(ctx, order.Id, status: "abgeholt"); // identisch zur Row unten
        reader.Rows = new() { Row("WA-1", status: "abgeholt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        fakeLogger.Runs[0].FinalCounts!["neu"].Should().Be(0);
        fakeLogger.Runs[0].FinalCounts!["aktualisiert"].Should().Be(0); // Upsert unveraendert
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(1);
        ctx.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeTrue();
    }

    // §10.4-13) Mehrfach-Treffer-Mischfall: eine WA -> 2 FAs, eine bereits erledigt, eine offen
    [Fact]
    public async Task AutoDone_MultiFaPerWa_OnlyOpenOneIsSet()
    {
        var (svc, reader, ctx, fakeLogger, _) = Build();
        var doneFa = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, doneFa.Id, isDonePicking: true);
        var openFa = SeedOrder(ctx, "WA-1");
        SeedPickingStatus(ctx, openFa.Id, isDonePicking: false);
        reader.Rows = new() { Row("WA-1", status: "abgeholt") };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100);

        ctx.ProductionOrderPickingStatuses.Single(p => p.ProductionOrderId == openFa.Id)
            .IsDonePicking.Should().BeTrue();
        ctx.ProductionOrderPickingStatuses.Single(p => p.ProductionOrderId == doneFa.Id)
            .ModifiedAt.Should().BeNull();
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(1);
    }

    // §10.4-14) Cap: Kandidaten > Cap -> kein Write, Warn-Zeile, Fehlermail, Counts
    [Fact]
    public async Task AutoDone_OverCap_NoWrites_Warns_Notifies()
    {
        var (svc, reader, ctx, fakeLogger, notifier) = Build();
        for (var i = 1; i <= 3; i++)
        {
            var order = SeedOrder(ctx, $"WA-{i}");
            SeedPickingStatus(ctx, order.Id, isDonePicking: false);
        }
        reader.Rows = new()
        {
            Row("WA-1", status: "abgeholt"),
            Row("WA-2", status: "abgeholt"),
            Row("WA-3", status: "verpackt"),
        };

        await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 2); // Cap+1 Kandidaten

        ctx.ProductionOrderPickingStatuses.Where(p => p.IsDonePicking).Should().BeEmpty();
        ctx.ProductionOrderExtraInfos.Should().HaveCount(3); // Upsert-Teil laeuft normal weiter
        fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue(); // kein throw
        fakeLogger.Runs[0].Events.Should().Contain(e =>
            e.Level == "Warning" && e.Message.Contains("Auto-Erledigt uebersprungen")
            && e.Message.Contains("3") && e.Message.Contains("2"));
        fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(0);
        fakeLogger.Runs[0].FinalCounts!["erledigt-kandidaten"].Should().Be(3);
        notifier.Verify(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<Exception>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
```

- [ ] **Step 4: Tests laufen lassen — 14 Fehlschläge erwartet (RED)**

Run: `dotnet test IDEALAKEWMSService.Tests --nologo --filter "FullyQualifiedName~FaZusatzinfoSyncServiceTests"`
Expected: Die 8 Bestandstests grün, die 14 `AutoDone_*`-Tests ROT — je mit `KeyNotFoundException` auf `erledigt-gesetzt` bzw. `IsDonePicking`-Assert-Fehlern.

- [ ] **Step 5: Auto-Erledigt implementieren (GREEN)**

`IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs` — kompletter neuer Datei-Inhalt:

```csharp
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// FA-Zusatzinfos aus Sage (v1.26.0, Spec §4 + §10): liest die View ueber den Reader-Seam
/// und upsertet den 1:1-Satelliten <see cref="ProductionOrderExtraInfo"/> per EF
/// (shared ApplicationDbContext — kompletter Entscheidungs- UND Schreibpfad InMemory-
/// testbar). Mehrfach-treffer-faehig (IDEAL-Linie: mehrere ProductionOrders je
/// WA-Nummer → Upsert je Id). Kein Loeschen: verschwindet ein WA aus der View,
/// bleibt der letzte Stand stehen.
/// Fold 2 (Spec §10): Sage-Status verpackt/abgeholt setzt offene FAs
/// (!IsDone &amp;&amp; !IsCancelled &amp;&amp; !PickingStatus.IsDonePicking) automatisch
/// auf Komm-Erledigt — einweg, zweiphasig mit Sicherheits-Cap
/// <c>autoDoneMaxPerRun</c> (mehr Kandidaten → kein Write + Warn + Fehlermail).
/// DryRun = voller Plan inkl. echter Would-be-Counts; JEDER Write (Upsert UND
/// Auto-Erledigt) liegt hinter einem if(!dryRun)-Guard VOR der Mutation getrackter
/// Entities — der Service laeuft im zyklusweiten Scope, ein spaeterer SaveChanges
/// wuerde mutierte Entities sonst mitflushen.
/// </summary>
public class FaZusatzinfoSyncService : IFaZusatzinfoSyncService
{
    private const string SyncUser = "FaZusatzinfoSync";
    private const int MaxDetailLinesPerRun = 100;

    private readonly ApplicationDbContext _ctx;
    private readonly ISageZusatzinfoReader _reader;
    private readonly ISyncErrorNotifier _errorNotifier;
    private readonly ILogger<FaZusatzinfoSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public FaZusatzinfoSyncService(
        ApplicationDbContext ctx,
        ISageZusatzinfoReader reader,
        ISyncErrorNotifier errorNotifier,
        ILogger<FaZusatzinfoSyncService> logger,
        ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _reader = reader;
        _errorNotifier = errorNotifier;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<SyncResult> SyncAsync(bool dryRun, int autoDoneMaxPerRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.FaZusatzinfo, ct);
        int read = 0, inserted = 0, updated = 0, skipped = 0, erledigtGesetzt = 0, erledigtKandidaten = 0;

        try
        {
            var readResult = await _reader.ReadAsync(ct);

            // View-fehlt-Guard (Spec §2): Warn-Zeile + regulaeres Lauf-Ende — bewusst
            // KEIN Fehlerpfad (kein throw, keine Fehlermail alle 15 min).
            if (!readResult.ViewExists)
            {
                await run.LogWarningAsync(
                    "View dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen nicht vorhanden — Sync uebersprungen.", ct: ct);
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["gelesen"] = 0,
                    ["neu"] = 0,
                    ["aktualisiert"] = 0,
                    ["uebersprungen"] = 0,
                    ["erledigt-gesetzt"] = 0,
                }, messageSuffix: dryRun ? "View nicht vorhanden [DryRun]" : "View nicht vorhanden", ct: ct);
                return new SyncResult(0, 0, 0, "View nicht vorhanden.");
            }

            var rows = readResult.Rows;
            read = rows.Count;

            // Einmaliger Set-Read der FA-Zuordnung (kein Zeile-fuer-Zeile-Roundtrip):
            // OrderNumber -> List<ProductionOrder> inkl. ExtraInfo-Satellit.
            // Fold 2: + PickingStatus (ein LEFT JOIN mehr, kein zweiter Roundtrip).
            var waNumbers = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.WaNummer))
                .Select(r => r.WaNummer!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var orders = await _ctx.ProductionOrders
                .Include(o => o.ExtraInfo)
                .Include(o => o.PickingStatus)
                .Where(o => waNumbers.Contains(o.OrderNumber))
                .ToListAsync(ct);

            var ordersByNumber = orders
                .GroupBy(o => o.OrderNumber, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            // Fold 2 (Spec §10.2): Auto-Erledigt-Kandidaten NUR sammeln (zweiphasig) —
            // Writes laufen nach der Schleife hinter dem Cap. Dictionary-Keys = Order-Ids
            // (dedupliziert hypothetische View-Duplikate -> kein DryRun-Doppelcount).
            var autoDoneCandidates = new Dictionary<int, (ProductionOrder Order, string SageStatus)>();

            foreach (var row in rows)
            {
                ct.ThrowIfCancellationRequested();

                var wa = row.WaNummer?.Trim();
                if (string.IsNullOrWhiteSpace(wa) || !ordersByNumber.TryGetValue(wa, out var matches))
                {
                    // Kein FA-Treffer: alte/erledigte WAs sind normal — Count, keine Warn-Zeile je WA.
                    skipped++;
                    continue;
                }

                foreach (var order in matches)
                {
                    var info = order.ExtraInfo;
                    if (info == null)
                    {
                        if (!dryRun)
                        {
                            _ctx.ProductionOrderExtraInfos.Add(new ProductionOrderExtraInfo
                            {
                                ProductionOrderId = order.Id,
                                Kaeltemittel = row.Kaeltemittel,
                                Ventil = row.Ventil,
                                AusfuehrungEZ = row.AusfuehrungEZ,
                                Maschine = row.Maschine,
                                SageStatus = row.Status,
                                CreatedAt = DateTime.Now,
                                CreatedBy = SyncUser,
                                CreatedByWindows = SyncUser,
                            });
                        }
                        inserted++;
                    }
                    else if (info.Kaeltemittel != row.Kaeltemittel
                          || info.Ventil != row.Ventil
                          || info.AusfuehrungEZ != row.AusfuehrungEZ
                          || info.Maschine != row.Maschine
                          || info.SageStatus != row.Status)
                    {
                        if (!dryRun)
                        {
                            info.Kaeltemittel = row.Kaeltemittel;
                            info.Ventil = row.Ventil;
                            info.AusfuehrungEZ = row.AusfuehrungEZ;
                            info.Maschine = row.Maschine;
                            info.SageStatus = row.Status;
                            info.ModifiedAt = DateTime.Now;
                            info.ModifiedBy = SyncUser;
                            info.ModifiedByWindows = SyncUser;
                        }
                        updated++;
                    }
                    // unveraendert -> kein Write, kein Count (haelt ModifiedAt aussagekraeftig)

                    // Fold 2 (Spec §10.1): Done-Check je gematchter FA-Zeile — UNABHAENGIG
                    // vom Feld-Diff (kein continue in der Kette -> greift auch im
                    // unveraendert-Zweig). Statusquelle ist row.Status (frischer View-Wert),
                    // NICHT info.SageStatus. Tripel: Sage-IsDone wird NIE beschrieben,
                    // stornierte FAs uebersprungen (sonst kaeme eine reaktivierte FA mit
                    // klebendem IsDonePicking dauerhaft versteckt zurueck).
                    if (FaZusatzinfoStatus.IstVerpacktOderAbgeholt(row.Status)
                        && !order.IsDone
                        && !order.IsCancelled
                        && order.PickingStatus?.IsDonePicking != true
                        && !autoDoneCandidates.ContainsKey(order.Id))
                    {
                        autoDoneCandidates[order.Id] = (order, row.Status!.Trim());
                    }
                }
            }

            // Fold 2 (Spec §10.2/§10.3): Write-Phase mit Sicherheits-Cap. Ein View-Defekt
            // (Status-Spalte flaechendeckend "abgeholt") wuerde sonst in EINEM Lauf alle
            // offenen FAs schliessen — still, einweg, ohne Bulk-Reopen.
            if (autoDoneCandidates.Count > autoDoneMaxPerRun)
            {
                erledigtKandidaten = autoDoneCandidates.Count;
                var warnMessage =
                    $"Auto-Erledigt uebersprungen: {autoDoneCandidates.Count} Kandidaten > Cap {autoDoneMaxPerRun} — moeglicher View-Defekt";
                await run.LogWarningAsync(warnMessage, ct: ct);
                await _errorNotifier.NotifyAsync("FA-Zusatzinfo Auto-Erledigt",
                    new InvalidOperationException(warnMessage), ct);
                // KEIN throw — der Upsert-Teil des Laufs bleibt gueltig und wird gespeichert.
            }
            else
            {
                var detailLines = 0;
                foreach (var (order, sageStatus) in autoDoneCandidates.Values)
                {
                    // DryRun-Guard VOR der Mutation (Spec §10.1): im DryRun wird KEINE
                    // getrackte Entity mutiert; Counts + Detailzeilen laufen ausserhalb.
                    if (!dryRun)
                    {
                        if (order.PickingStatus == null)
                        {
                            // Altbestand-Randfall: PickingStatus-Zeile fehlt -> eager anlegen
                            // (Muster ProductionOrderPickingStatusRepository.SetFieldAsync).
                            _ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus
                            {
                                ProductionOrderId = order.Id,
                                IsDonePicking = true,
                                CreatedAt = DateTime.Now,
                                CreatedBy = SyncUser,
                                CreatedByWindows = SyncUser,
                            });
                        }
                        else
                        {
                            order.PickingStatus.IsDonePicking = true;
                            order.PickingStatus.ModifiedAt = DateTime.Now;
                            order.PickingStatus.ModifiedBy = SyncUser;
                            order.PickingStatus.ModifiedByWindows = SyncUser;
                        }
                    }

                    erledigtGesetzt++;

                    // Detailzeilen gecappt (Muster Hauptlagerplatz-Warnzeilen) —
                    // der Count zaehlt weiterhin ALLE.
                    if (detailLines < MaxDetailLinesPerRun)
                    {
                        detailLines++;
                        await run.LogInfoAsync(
                            $"FA {order.OrderNumber} auf erledigt gesetzt (Sage-Status: {sageStatus})",
                            reference: order.OrderNumber, ct: ct);
                    }
                }
            }

            if (!dryRun) await _ctx.SaveChangesAsync(ct);

            _logger.LogInformation(
                "FA-Zusatzinfo-Sync abgeschlossen: {Read} gelesen, {Inserted} neu, {Updated} aktualisiert, {Skipped} uebersprungen, {Done} erledigt gesetzt{DryRun}",
                read, inserted, updated, skipped, erledigtGesetzt, dryRun ? " [DryRun]" : "");

            var counts = new Dictionary<string, int>
            {
                ["gelesen"] = read,
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
                ["uebersprungen"] = skipped,
                ["erledigt-gesetzt"] = erledigtGesetzt,
            };
            if (erledigtKandidaten > 0)
                counts["erledigt-kandidaten"] = erledigtKandidaten;

            await run.FinishSuccessAsync(counts, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);

            return new SyncResult(inserted, updated, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim FA-Zusatzinfo-Sync.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, counts: new Dictionary<string, int>
            {
                ["gelesen"] = read,
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
                ["uebersprungen"] = skipped,
                ["erledigt-gesetzt"] = erledigtGesetzt,
            }, ct: ct);
            throw;
        }
    }
}
```

- [ ] **Step 6: Tests laufen lassen (GREEN)**

Run: `dotnet test IDEALAKEWMSService.Tests --nologo --filter "FullyQualifiedName~FaZusatzinfoSyncServiceTests"`
Expected: PASS (8 Bestand + 14 neue = 22 Tests).

- [ ] **Step 7: Beide Suiten voll**

Run: `dotnet test IDEALAKEWMSService.Tests --nologo` und `dotnet test IdealAkeWms.Tests --nologo`
Expected: Alle grün (insb. `SyncWorkerTests` unverändert grün).

- [ ] **Step 8: Commit**

```bash
git add IDEALAKEWMSService/Services/IFaZusatzinfoSyncService.cs IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs IDEALAKEWMSService/Workers/SyncWorker.cs IDEALAKEWMSService.Tests/Services/FaZusatzinfoSyncServiceTests.cs
git commit -m "feat(fa-zusatzinfo): Auto-Erledigt bei Sage-Status verpackt/abgeholt (Cap + Protokoll)

Zweiphasig (Kandidaten -> Cap -> Writes), einweg, Tripel-Bedingung
!IsDone && !IsCancelled && !IsDonePicking, Statusquelle row.Status.
Cap Sync:FaZusatzinfoAutoDoneMaxPerRun (Default 100): > Cap -> kein Write
+ Warn + Fehlermail, kein throw. Counts erledigt-gesetzt/-kandidaten,
Info-Detailzeilen (Cap 100, reference=OrderNumber). DryRun-Guard VOR
jeder Mutation, Docblock korrigiert.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3: BDE-Buchungs-Sperre (§10.6) — Guard + Listen-Hygiene + Toast

**Files:**
- Modify: `IdealAkeWms.Tests/Helpers/BdeBookingTestSeed.cs` (Seed-Helper)
- Modify: `IdealAkeWms.Tests/Services/BdeBookingServiceTests.cs` (+10 Tests)
- Modify: `IdealAkeWms/Services/BdeBookingService.cs:36-47` (Start-Guard), `:156-166` (Resume-Guard), neuer privater Helper nach `EnsureWorkplaceIsBdeActiveAsync` (~Z.401)
- Modify: `IdealAkeWms/Data/Repositories/IWorkOperationRepository.cs:11` + `WorkOperationRepository.cs:52-62`
- Modify: `IdealAkeWms/Controllers/BdeApiController.cs:171-176` (NurFA-Query) + `:203` (excludePackedOrders)
- Modify: `IdealAkeWms.Tests/Repositories/WorkOperationRepositoryExtendedTests.cs` (+2 Tests)
- Modify: `IdealAkeWms.Tests/Controllers/BdeApiControllerTests.cs` (4 Moq-Setups + 1 neuer Test)
- Modify: `IdealAkeWms/wwwroot/js/bde-terminal.js:275-291` (`post()`)
- KEINE Änderung: `IdealAkeWms/Controllers/TrackingController.cs:153` (Default-Parameter hält das Tracking-Verhalten)

- [ ] **Step 1: Seed-Helper + 10 Guard-Tests schreiben (RED)**

**(a)** In `IdealAkeWms.Tests/Helpers/BdeBookingTestSeed.cs` vor der schließenden Klassen-Klammer einfügen:

```csharp
    /// <summary>
    /// Fold 2 (v1.26.0, Spec §10.6): legt den ExtraInfo-Satelliten mit gegebenem
    /// SageStatus an (fuer BDE-Sperre-Tests).
    /// </summary>
    public static async Task SetSageStatusAsync(ApplicationDbContext ctx, int productionOrderId, string? sageStatus)
    {
        ctx.ProductionOrderExtraInfos.Add(new ProductionOrderExtraInfo
        {
            ProductionOrderId = productionOrderId,
            SageStatus = sageStatus,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();
    }
```

**(b)** Ans Ende von `IdealAkeWms.Tests/Services/BdeBookingServiceTests.cs` (vor die schließende Klassen-Klammer) die 10 Tests anfügen (§10.6 Fälle 1–7, 9, 10; Fall 8 = Listen-Hygiene folgt in Step 5):

```csharp
    // ===== Fold 2 (v1.26.0, Spec §10.6): BDE-Sperre bei Sage-Status verpackt/abgeholt =====

    // §10.6-1/2) Start mit FA verpackt/abgeholt (+ Case/Trim) -> InvalidState, keine Buchung
    [Theory]
    [InlineData("verpackt")]
    [InlineData("abgeholt")]
    [InlineData(" Verpackt ")]
    public async Task StartProduction_FaPacked_ReturnsInvalidState_NoBooking(string sageStatus)
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var order = await ctx.ProductionOrders.FindAsync(ids.ProductionOrderId);
        await BdeBookingTestSeed.SetSageStatusAsync(ctx, ids.ProductionOrderId, sageStatus);

        var result = await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.InvalidState);
        result.Message.Should().Contain(order!.OrderNumber);
        result.Message.Should().Contain(sageStatus.Trim());
        result.Message.Should().Contain("keine BDE-Buchung mehr moeglich");
        (await ctx.BdeBookings.CountAsync()).Should().Be(0);
    }

    // §10.6-1) Auch der Setup-Start ist gesperrt (StartPlannedAsync deckt beide Typen)
    [Fact]
    public async Task StartSetup_FaPacked_ReturnsInvalidState_NoBooking()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        await BdeBookingTestSeed.SetSageStatusAsync(ctx, ids.ProductionOrderId, "abgeholt");

        var result = await svc.StartSetupAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.InvalidState);
        (await ctx.BdeBookings.CountAsync()).Should().Be(0);
    }

    // §10.6-3) Anderer Status -> Buchung startet normal
    [Fact]
    public async Task StartProduction_OtherSageStatus_Succeeds()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        await BdeBookingTestSeed.SetSageStatusAsync(ctx, ids.ProductionOrderId, "in Produktion");

        var result = await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
    }

    // §10.6-4) Ohne ExtraInfo-Satellit (Sync aus / View fehlt) -> keine Sperre
    [Fact]
    public async Task StartProduction_NoExtraInfo_Succeeds()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var result = await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
    }

    // §10.6-5) Resume einer pausierten Buchung, FA inzwischen abgeholt -> InvalidState, keine neue Buchung
    [Fact]
    public async Task Resume_FaPackedMeanwhile_ReturnsInvalidState_NoNewBooking()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var parent = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Paused,
            startedAt: DateTime.Now.AddHours(-2), endedAt: DateTime.Now.AddHours(-1));
        ctx.BdeBookings.Add(parent);
        await ctx.SaveChangesAsync();
        await BdeBookingTestSeed.SetSageStatusAsync(ctx, ids.ProductionOrderId, "abgeholt");

        var result = await svc.ResumeAsync(parent.Id, ids.OperatorId, BdeBookingType.Production, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.InvalidState);
        (await ctx.BdeBookings.CountAsync()).Should().Be(1); // nur der Parent
        (await ctx.BdeBookings.FindAsync(parent.Id))!.Status.Should().Be(BdeBookingStatus.Paused);
    }

    // §10.6-6a) Laufende Buchung: Finish funktioniert weiter (kein Guard in Beenden-Pfaden)
    [Fact]
    public async Task Finish_RunningBooking_FaPackedMeanwhile_StillWorks()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var running = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: DateTime.Now.AddHours(-1));
        ctx.BdeBookings.Add(running);
        await ctx.SaveChangesAsync();
        await BdeBookingTestSeed.SetSageStatusAsync(ctx, ids.ProductionOrderId, "verpackt");

        var finish = await svc.FinishAsync(running.Id, goodQty: 10m, scrapQty: 1m);

        finish.Outcome.Should().Be(BdeBookingOutcome.Success);
        (await ctx.BdeBookings.FindAsync(running.Id))!.Status.Should().Be(BdeBookingStatus.Finished);
    }

    // §10.6-6b) Laufende Buchung: Pause funktioniert weiter
    [Fact]
    public async Task Pause_RunningBooking_FaPackedMeanwhile_StillWorks()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var running = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: DateTime.Now.AddHours(-1));
        ctx.BdeBookings.Add(running);
        await ctx.SaveChangesAsync();
        await BdeBookingTestSeed.SetSageStatusAsync(ctx, ids.ProductionOrderId, "verpackt");

        var pause = await svc.PauseAsync(running.Id, goodQty: 2m, scrapQty: 0m);

        pause.Outcome.Should().Be(BdeBookingOutcome.Success);
        (await ctx.BdeBookings.FindAsync(running.Id))!.Status.Should().Be(BdeBookingStatus.Paused);
    }

    // §10.6-7) Ungeplante Taetigkeit (werkbank-bezogen, ohne FA) startet trotz gesperrter FA
    [Fact]
    public async Task StartActivity_WorksDespitePackedFa()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        await BdeBookingTestSeed.SetSageStatusAsync(ctx, ids.ProductionOrderId, "abgeholt");

        var result = await svc.StartActivityAsync(ids.OperatorId, ids.ActivityId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
    }

    // §10.6-9) Setup-Transition: laufendes Ruesten + FA wird verpackt -> StartProduction
    //          InvalidState, das Setup bleibt Running (Guard VOR der Auto-Close-Transition)
    [Fact]
    public async Task SetupRunning_FaPacked_StartProductionRejected_SetupStaysRunning()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var setup = await svc.StartSetupAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);
        setup.Outcome.Should().Be(BdeBookingOutcome.Success);
        await BdeBookingTestSeed.SetSageStatusAsync(ctx, ids.ProductionOrderId, "verpackt");

        var prod = await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        prod.Outcome.Should().Be(BdeBookingOutcome.InvalidState);
        var setupDb = await ctx.BdeBookings.FindAsync(setup.Booking!.Id);
        setupDb!.Status.Should().Be(BdeBookingStatus.Running); // kein gestrandeter Operator
        setupDb.EndedAt.Should().BeNull();
    }

    // §10.6-10) Resume einer pausierten Activity-Buchung (WorkOperationId NULL) bleibt moeglich
    [Fact]
    public async Task Resume_PausedActivityBooking_SucceedsDespitePackedFa()
    {
        var svc = NewService(out var ctx);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var pausedActivity = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Activity, BdeBookingStatus.Paused,
            startedAt: DateTime.Now.AddHours(-2), endedAt: DateTime.Now.AddHours(-1));
        ctx.BdeBookings.Add(pausedActivity);
        await ctx.SaveChangesAsync();
        await BdeBookingTestSeed.SetSageStatusAsync(ctx, ids.ProductionOrderId, "verpackt");

        var result = await svc.ResumeAsync(pausedActivity.Id, ids.OperatorId, BdeBookingType.Activity, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
        result.Booking!.WorkOperationId.Should().BeNull();
        result.Booking.BookingType.Should().Be(BdeBookingType.Activity);
    }
```

- [ ] **Step 2: Tests laufen lassen — Guard-Tests rot (RED)**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~BdeBookingServiceTests"`
Expected: Die neuen Sperre-Tests (Packed → InvalidState) FAILEN mit `Expected result.Outcome to be InvalidState, but found Success`; die Durchlass-Tests (OtherStatus/NoExtraInfo/Finish/Pause/Activity/Activity-Resume) sind bereits grün. Bestandstests grün.

- [ ] **Step 3: Guard implementieren (GREEN)**

In `IdealAkeWms/Services/BdeBookingService.cs`:

**(a)** Neuen privaten Helper DIREKT NACH `EnsureWorkplaceIsBdeActiveAsync` (nach Z.401) einfügen:

```csharp
    /// <summary>
    /// Fold 2 (v1.26.0, Spec §10.6): BDE-Sperre — ist der FA in Sage bereits
    /// verpackt/abgeholt (ExtraInfo.SageStatus), sind keine NEUEN Buchungen mehr
    /// moeglich (Start + Resume). Laufende Buchungen bleiben unangetastet (bewusst
    /// KEIN Guard in Beenden-/Pausieren-/Mengen-Pfaden); ohne Zusatzinfo-Daten
    /// (Sync aus / kein Satellit) keine Sperre. Rueckgabe null bedeutet "OK, weiter".
    /// </summary>
    private async Task<BdeBookingResult?> EnsureOrderNotPackedAsync(int workOperationId)
    {
        var orderInfo = await _ctx.WorkOperations
            .Where(w => w.Id == workOperationId)
            .Select(w => new
            {
                w.ProductionOrder.OrderNumber,
                SageStatus = w.ProductionOrder.ExtraInfo != null ? w.ProductionOrder.ExtraInfo.SageStatus : null
            })
            .FirstOrDefaultAsync();

        if (orderInfo != null && FaZusatzinfoStatus.IstVerpacktOderAbgeholt(orderInfo.SageStatus))
            return BdeBookingResult.Invalid(
                $"FA {orderInfo.OrderNumber} ist bereits {orderInfo.SageStatus!.Trim()} — keine BDE-Buchung mehr moeglich.");

        return null;
    }
```

(`FaZusatzinfoStatus` liegt im selben Namespace `IdealAkeWms.Services` — kein zusätzliches using.)

**(b)** In `StartPlannedAsync` DIREKT NACH dem Werkbank-Gate (Z.41-42) einfügen:

```csharp
            // Fold 2 (Spec §10.6): FA in Sage bereits verpackt/abgeholt -> keine neue
            // Buchung. Liegt VOR der Auto-Close-Transition (Rule 1) — ein laufendes
            // Ruesten bleibt bei abgelehntem Produktion-Start unangetastet.
            var packedError = await EnsureOrderNotPackedAsync(workOperationId);
            if (packedError != null) return packedError;
```

**(c)** In `ResumeAsync` DIREKT NACH dem Paused-Check (Z.165-166, `return BdeBookingResult.Invalid("Ziel-Buchung ist nicht pausiert.");`) einfügen:

```csharp
            // Fold 2 (Spec §10.6): Resume erzeugt eine NEUE Buchung -> gleiche Sperre wie
            // Start. Nur fuer geplante Buchungen — pausierte Activity-Buchungen
            // (WorkOperationId NULL, erreichbar ueber das Paused-Panel) NICHT blocken.
            if (parent.WorkOperationId.HasValue)
            {
                var packedError = await EnsureOrderNotPackedAsync(parent.WorkOperationId.Value);
                if (packedError != null) return packedError;
            }
```

- [ ] **Step 4: Tests laufen lassen (GREEN)**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~BdeBookingServiceTests"`
Expected: PASS (alle Bestand + 12 neue Test-Methoden/14 Fälle inkl. Theory).

- [ ] **Step 5: Listen-Hygiene-Tests schreiben (RED — Compile-Fehler)**

**(a)** An `IdealAkeWms.Tests/Repositories/WorkOperationRepositoryExtendedTests.cs` (vor die schließende Klassen-Klammer) anfügen:

```csharp
    // ===== Fold 2 (v1.26.0, Spec §10.6): excludePackedOrders-Filter =====

    [Fact]
    public async Task GetOpenByWorkplaceIdAsync_ExcludePackedOrders_FiltersPackedFas()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new WorkOperationRepository(ctx);

        var wp = CreateWorkplace("Werkbank P");
        ctx.ProductionWorkplaces.Add(wp);
        var poOpen = CreateProductionOrder("WA-OFFEN");
        var poPacked = CreateProductionOrder("WA-VERPACKT");
        var poNullStatus = CreateProductionOrder("WA-NULLSTATUS");
        ctx.ProductionOrders.AddRange(poOpen, poPacked, poNullStatus);
        await ctx.SaveChangesAsync();

        ctx.ProductionOrderExtraInfos.Add(new ProductionOrderExtraInfo
        {
            ProductionOrderId = poPacked.Id, SageStatus = " Abgeholt ", // Case/Trim greift auch hier
            CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "TEST\\user"
        });
        ctx.ProductionOrderExtraInfos.Add(new ProductionOrderExtraInfo
        {
            ProductionOrderId = poNullStatus.Id, SageStatus = null, // Null-Guard: bleibt drin
            CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "TEST\\user"
        });
        ctx.WorkOperations.AddRange(
            CreateOperation(poOpen.Id, "10", "Op offen", 1, wp.Id),
            CreateOperation(poPacked.Id, "10", "Op verpackt", 1, wp.Id),
            CreateOperation(poNullStatus.Id, "10", "Op null-status", 1, wp.Id));
        await ctx.SaveChangesAsync();

        var result = await repo.GetOpenByWorkplaceIdAsync(wp.Id, excludePackedOrders: true);

        result.Select(o => o.ProductionOrder.OrderNumber)
            .Should().BeEquivalentTo(new[] { "WA-OFFEN", "WA-NULLSTATUS" });
    }

    [Fact]
    public async Task GetOpenByWorkplaceIdAsync_DefaultParameter_KeepsPackedFas()
    {
        // Tracking-Aufrufer (TrackingController.ByWorkplace) bleibt bewusst ungefiltert.
        using var ctx = TestDbContextFactory.Create();
        var repo = new WorkOperationRepository(ctx);

        var wp = CreateWorkplace("Werkbank T");
        ctx.ProductionWorkplaces.Add(wp);
        var poPacked = CreateProductionOrder("WA-VERPACKT");
        ctx.ProductionOrders.Add(poPacked);
        await ctx.SaveChangesAsync();

        ctx.ProductionOrderExtraInfos.Add(new ProductionOrderExtraInfo
        {
            ProductionOrderId = poPacked.Id, SageStatus = "verpackt",
            CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "TEST\\user"
        });
        ctx.WorkOperations.Add(CreateOperation(poPacked.Id, "10", "Op verpackt", 1, wp.Id));
        await ctx.SaveChangesAsync();

        var result = await repo.GetOpenByWorkplaceIdAsync(wp.Id);

        result.Should().HaveCount(1);
        result[0].ProductionOrder.OrderNumber.Should().Be("WA-VERPACKT");
    }
```

**(b)** An `IdealAkeWms.Tests/Controllers/BdeApiControllerTests.cs` (vor die schließende Klassen-Klammer) anfügen:

```csharp
    // ===== Fold 2 (v1.26.0, Spec §10.6): Listen-Hygiene NurFA-Modus =====

    [Fact]
    public async Task GetAvailableOperations_NurFaMode_ExcludesPackedOrders()
    {
        var ctx = TestDbContextFactory.Create();
        var wp = new ProductionWorkplace
        {
            Name = "WB", BdeAktiv = true,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();

        var poOpen = new ProductionOrder
        {
            OrderNumber = "FA-OFFEN", ProductionWorkplaceId = wp.Id,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        var poPacked = new ProductionOrder
        {
            OrderNumber = "FA-GESPERRT", ProductionWorkplaceId = wp.Id,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        var poNullStatus = new ProductionOrder
        {
            OrderNumber = "FA-NULLSTATUS", ProductionWorkplaceId = wp.Id,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.ProductionOrders.AddRange(poOpen, poPacked, poNullStatus);
        await ctx.SaveChangesAsync();

        ctx.ProductionOrderExtraInfos.Add(new ProductionOrderExtraInfo
        {
            ProductionOrderId = poPacked.Id, SageStatus = " Verpackt ",
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.ProductionOrderExtraInfos.Add(new ProductionOrderExtraInfo
        {
            ProductionOrderId = poNullStatus.Id, SageStatus = null,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        _settings.Setup(r => r.GetValueAsync("BdeNurFaMeldung")).ReturnsAsync("true");

        var controller = new BdeApiController(_ops.Object, _activities.Object, _bookings.Object,
            _workOps.Object, _workplaces.Object, _settings.Object, ctx);

        var result = await controller.GetAvailableOperations(wp.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        json.Should().Contain("FA-OFFEN");
        json.Should().Contain("FA-NULLSTATUS"); // Null-Guard: ohne Status bleibt der FA drin
        json.Should().NotContain("FA-GESPERRT");
        json.Should().Contain("\"nurFaMode\":true");
    }
```

**(c)** In `IdealAkeWms.Tests/Controllers/BdeApiControllerTests.cs` die 4 bestehenden Moq-Setups (Z.312, 350, 381, 411) auf die neue 2-Parameter-Signatur umstellen — Moq-Expression-Trees erlauben KEINE optionalen Argumente, und der BDE-Controller ruft künftig mit `true`:

Alt (4×): `_workOps.Setup(r => r.GetOpenByWorkplaceIdAsync(ids.WorkplaceId))`
Neu (4×): `_workOps.Setup(r => r.GetOpenByWorkplaceIdAsync(ids.WorkplaceId, true))`

- [ ] **Step 6: Kompilieren — Fehler erwartet (RED)**

Run: `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly`
Expected: Compile-FEHLER (`GetOpenByWorkplaceIdAsync` hat noch keinen zweiten Parameter) — RED-Beweis für die Repo-/API-Erweiterung.

- [ ] **Step 7: Listen-Hygiene implementieren (GREEN)**

**(a)** `IdealAkeWms/Data/Repositories/IWorkOperationRepository.cs` Z.11 ersetzen:

```csharp
    /// <summary>
    /// excludePackedOrders (Fold 2, Spec §10.6): true filtert FAs mit Sage-Status
    /// verpackt/abgeholt aus (NUR der BDE-Aufrufer). Default false erhaelt das
    /// Tracking-Verhalten (TrackingController.ByWorkplace bleibt ungefiltert).
    /// </summary>
    Task<List<WorkOperation>> GetOpenByWorkplaceIdAsync(int workplaceId, bool excludePackedOrders = false);
```

**(b)** `IdealAkeWms/Data/Repositories/WorkOperationRepository.cs` — oben `using IdealAkeWms.Services;` ergänzen, dann Methode (Z.52-62) ersetzen:

```csharp
    public async Task<List<WorkOperation>> GetOpenByWorkplaceIdAsync(int workplaceId, bool excludePackedOrders = false)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(wo => wo.ProductionOrder)
            .Include(wo => wo.ProductionWorkplace)
            .Where(wo => wo.ProductionWorkplaceId == workplaceId && !wo.IsReported);

        if (excludePackedOrders)
        {
            // Fold 2 (Spec §10.6): Filter IN der EF-Query (die Methode laedt ExtraInfo
            // nicht — kein nachgelagerter In-Memory-Filter moeglich). Ausgeschriebenes
            // Null-Guard-Praedikat, InMemory-kompatibel (kein statischer Helper in EF).
            query = query.Where(wo => wo.ProductionOrder.ExtraInfo == null
                || wo.ProductionOrder.ExtraInfo.SageStatus == null
                || (wo.ProductionOrder.ExtraInfo.SageStatus.Trim().ToLower() != FaZusatzinfoStatus.Verpackt
                    && wo.ProductionOrder.ExtraInfo.SageStatus.Trim().ToLower() != FaZusatzinfoStatus.Abgeholt));
        }

        return await query
            .OrderBy(wo => wo.ProductionOrder.OrderNumber)
            .ThenBy(wo => wo.Sequence)
            .ToListAsync();
    }
```

**(c)** `IdealAkeWms/Controllers/BdeApiController.cs` — oben `using IdealAkeWms.Services;` ergänzen, dann:

NurFA-Query (Z.171-176) ersetzen. Alt:

```csharp
            var ordersQuery = _ctx.ProductionOrders
                .Where(po => po.ProductionWorkplaceId == workplaceId && !po.IsDone && !po.IsCancelled
                    && !_ctx.BdeBookingQuantities.Any(q =>
                        q.IsFinal
                        && q.BdeBooking!.WorkOperation!.ProductionOrderId == po.Id
                        && !q.BdeBooking.IsCancelled));
```

Neu:

```csharp
            var ordersQuery = _ctx.ProductionOrders
                .Where(po => po.ProductionWorkplaceId == workplaceId && !po.IsDone && !po.IsCancelled
                    // Fold 2 (Spec §10.6) Listen-Hygiene: FAs mit Sage-Status verpackt/
                    // abgeholt gar nicht erst anbieten (der Start-Guard wuerde ablehnen).
                    && (po.ExtraInfo == null || po.ExtraInfo.SageStatus == null
                        || (po.ExtraInfo.SageStatus.Trim().ToLower() != FaZusatzinfoStatus.Verpackt
                            && po.ExtraInfo.SageStatus.Trim().ToLower() != FaZusatzinfoStatus.Abgeholt))
                    && !_ctx.BdeBookingQuantities.Any(q =>
                        q.IsFinal
                        && q.BdeBooking!.WorkOperation!.ProductionOrderId == po.Id
                        && !q.BdeBooking.IsCancelled));
```

Offene-AGs-Aufruf (Z.203) ersetzen. Alt: `var workOps = await _workOps.GetOpenByWorkplaceIdAsync(workplaceId);` → Neu:

```csharp
        var workOps = await _workOps.GetOpenByWorkplaceIdAsync(workplaceId, excludePackedOrders: true);
```

(Der Scan-Lookup-Endpoint `GetWorkOperation` bleibt UNVERÄNDERT — der Start-Guard liefert dort die sprechende Meldung.)

- [ ] **Step 8: Tests laufen lassen (GREEN)**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~WorkOperationRepositoryExtendedTests|FullyQualifiedName~BdeApiControllerTests"`
Expected: PASS (inkl. der 4 umgestellten Setups und der 3 neuen Tests).

- [ ] **Step 9: Terminal-UX — InvalidState-Toast in `post()` (JS-Bugfix)**

In `IdealAkeWms/wwwroot/js/bde-terminal.js` (Z.281-289) den Erfolgs-/Fehler-Verteiler erweitern. Alt:

```javascript
        if (json.outcome === 'Success') {
            showToast(getSuccessMessage(actionName));
        } else if (json.outcome === 'CollisionOtherOperator') {
            document.getElementById('collisionText').textContent =
                'AG ist bereits in Arbeit durch ' + json.collidingOperator + ' an ' + json.collidingWorkplace + ' seit ' + new Date(json.collidingSince).toLocaleTimeString() + '.';
            bootstrap.Modal.getOrCreateInstance(document.getElementById('collisionModal')).show();
        } else if (json.outcome === 'QuantityRequired') {
            showToast('Mengen-Eingabe erforderlich', 'warning');
        }
```

Neu (NUR der zusätzliche Zweig — bewusst KEIN catch-all-else: `GroupFinishRequired` läuft als Nicht-Success durch `post()` und wird vom Aufrufer behandelt; der Resume-Pfad nutzt `post()` nicht):

```javascript
        if (json.outcome === 'Success') {
            showToast(getSuccessMessage(actionName));
        } else if (json.outcome === 'CollisionOtherOperator') {
            document.getElementById('collisionText').textContent =
                'AG ist bereits in Arbeit durch ' + json.collidingOperator + ' an ' + json.collidingWorkplace + ' seit ' + new Date(json.collidingSince).toLocaleTimeString() + '.';
            bootstrap.Modal.getOrCreateInstance(document.getElementById('collisionModal')).show();
        } else if (json.outcome === 'QuantityRequired') {
            showToast('Mengen-Eingabe erforderlich', 'warning');
        } else if (json.outcome === 'InvalidState') {
            // Fold 2 (Spec §10.6): InvalidState wurde bisher verschluckt (betraf auch die
            // Werkbank-Gate-Meldung). BDE-Sperre + Gate zeigen jetzt die Server-Meldung.
            showToast(json.message || 'Aktion nicht möglich', 'danger');
        }
```

Kein Unit-Test (JS) — Manual-UAT TS-55.12.

- [ ] **Step 10: Voll-Build + komplette Web-Suite**

Run: `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly` dann `dotnet test IdealAkeWms.Tests --nologo`
Expected: Build Exit 0, alle Web-Tests grün (inkl. Tracking-/BdeTerminal-Bestandstests).

- [ ] **Step 11: Commit**

```bash
git add IdealAkeWms/Services/BdeBookingService.cs IdealAkeWms/Data/Repositories/IWorkOperationRepository.cs IdealAkeWms/Data/Repositories/WorkOperationRepository.cs IdealAkeWms/Controllers/BdeApiController.cs IdealAkeWms/wwwroot/js/bde-terminal.js IdealAkeWms.Tests/Helpers/BdeBookingTestSeed.cs IdealAkeWms.Tests/Services/BdeBookingServiceTests.cs IdealAkeWms.Tests/Repositories/WorkOperationRepositoryExtendedTests.cs IdealAkeWms.Tests/Controllers/BdeApiControllerTests.cs
git commit -m "feat(bde): Buchungs-Sperre bei Sage-Status verpackt/abgeholt (Guard + Listen-Hygiene + Toast)

Guard EnsureOrderNotPackedAsync in StartPlannedAsync (nach Werkbank-Gate,
VOR Auto-Close-Transition) + ResumeAsync (nach Parent-Load, nur
WorkOperationId.HasValue — Activity-Resume bleibt frei). Laufende
Buchungen unangetastet (kein Guard in Finish/Pause/Mengen). Listen-
Hygiene: NurFA-Query + GetOpenByWorkplaceIdAsync(excludePackedOrders,
Default false — Tracking ungefiltert). bde-terminal.js post() zeigt
InvalidState als Toast (kein catch-all).

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 4: Doku komplett (§10.5) inkl. secondbrain-Spiegelung

**Files:**
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml:27-39` (v1.26.0-Card, +2 Bullets)
- Modify: `IdealAkeWms/Views/Help/Index.cshtml` (4 Stellen: Z.313-322, Leitstand-Card ~Z.546, Kommissionierung-Card ~Z.707, BDE-Card ~Z.627)
- Modify: `docs/TESTSZENARIEN.md` (Kap. 55 Umbau + TS-55.10–55.13 + Index Z.77)
- Modify: `CLAUDE.md` (Service-Konfig-Tabelle + Fallstrick)
- Modify: `README.md:625`
- Modify: `PROJECT_STATUS.md:11-19`
- Modify: `docs/superpowers/specs/2026-07-22-pa-zusatzinfos-design.md` (§7 Schritt 4)
- Spiegel: `secondbrain/docs/TESTSZENARIEN.md`, `secondbrain/docs/superpowers/specs/2026-07-22-pa-zusatzinfos-design.md`, `secondbrain/docs/superpowers/plans/2026-07-22-pa-zusatzinfos-fold2-auto-done.md`

- [ ] **Step 1: Changelog — v1.26.0-Card um 2 Bullets ergänzen**

In `IdealAkeWms/Views/Help/Changelog.cshtml` zwischen dem „FA-Liste + Leitstand"-Bullet (endet Z.33 `... einblenden.</li>`) und dem „Hinweis fuer Admins"-Bullet (Z.34) einfügen:

```html
                    <li><strong>Automatisch erledigt bei Sage-Status verpackt/abgeholt:</strong>
                        Meldet Sage einen FA als <em>verpackt</em> oder <em>abgeholt</em>, setzt
                        der Sync ihn automatisch auf Komm-Erledigt (wie der Abschliessen-Button)
                        &mdash; der FA verschwindet aus den offenen Sichten. Einweg: faellt der
                        Status zurueck, bleibt der FA erledigt. Sicherheits-Cap
                        <code>Sync:FaZusatzinfoAutoDoneMaxPerRun</code> (Default 100) verhindert
                        Massen-Schliessungen bei View-Defekten. Vor dem Scharfschalten DryRun
                        fahren und den Count <code>erledigt-gesetzt</code> pruefen.</li>
                    <li><strong>BDE-Buchungs-Sperre:</strong> Fuer FAs mit Sage-Status
                        verpackt/abgeholt sind am BDE-Terminal keine NEUEN Buchungen (Start +
                        Fortsetzen) mehr moeglich &mdash; laufende Buchungen lassen sich normal
                        beenden, ungeplante Taetigkeiten bleiben moeglich. Das Terminal zeigt
                        eine Meldung; gesperrte FAs verschwinden aus den Auswahllisten.</li>
```

- [ ] **Step 2: Hilfe — 4 Stellen in `Views/Help/Index.cshtml`**

**(a) FA-Liste/Leitstand-Zusatzinfo-Eintrag (Z.313-322):** Im `<dd>` des `<dt>FA-Zusatzinfos aus Sage in FA-Liste + Leitstand (seit v1.26.0)</dt>` VOR dem Satz „Hinweis: bei ausgeblendeter Spalte ..." einfügen:

```html
                        Zusaetzlich setzt der Sync FAs mit Sage-Status
                        <strong>verpackt</strong>/<strong>abgeholt</strong> automatisch auf
                        Komm-Erledigt (ACHTUNG beim Aktivieren: vorher DryRun pruefen).
```

**(b) Leitstand-Card (`<dl>` ab Z.546):** Nach dem `<dt>Freigabe zuruecknehmen</dt>`-Block (endet Z.562 `... bleiben erhalten.</dd>`) neuen Eintrag einfügen:

```html
                    <dt>Automatisch erledigt bei Sage-Status verpackt/abgeholt (seit v1.26.0)</dt>
                    <dd>Meldet Sage einen FA als <em>verpackt</em> oder <em>abgeholt</em>, setzt
                        der Zusatzinfo-Sync ihn automatisch auf Komm-Erledigt &mdash; der FA
                        verschwindet aus FA-Liste, Leitstand und Komm-Worklist (wie der
                        Abschliessen-Button). Der Grund ist ueber die einblendbare Spalte
                        <strong>Sage-Status</strong> (unter &bdquo;Erledigte anzeigen&ldquo;)
                        nachvollziehbar. Einweg: faellt der Sage-Status zurueck, bleibt der FA
                        erledigt; manuelles Wieder-Oeffnen haelt nur, bis Sage erneut
                        verpackt/abgeholt meldet.</dd>
```

**(c) Kommissionierung-Card (Card-Header Z.707):** Im Card-Body an passender Stelle (nach dem ersten einleitenden `<p>`-Absatz) einfügen:

```html
                <p class="small text-muted mb-2">
                    <strong>Seit v1.26.0:</strong> Ein FA verschwindet automatisch aus der
                    Kommissionierliste, wenn Sage ihn als <em>verpackt</em> oder
                    <em>abgeholt</em> meldet (Auto-Erledigt) &mdash; der Grund ist in
                    FA-Liste/Leitstand ueber die Spalte &bdquo;Sage-Status&ldquo; einblendbar.
                </p>
```

**(d) BDE-Card:** Nach dem Absatz unter „Terminal-Bedienung" (endet Z.627 `</p>`) neuen Abschnitt einfügen:

```html
                <h6 class="mt-3">FA bereits verpackt/abgeholt (seit v1.26.0)</h6>
                <p>
                    Sobald ein FA in Sage den Status <em>verpackt</em> oder <em>abgeholt</em>
                    traegt, sind am Terminal <strong>keine neuen Buchungen</strong> mehr
                    moeglich &mdash; Start und Fortsetzen werden mit der Meldung
                    &bdquo;FA &lt;Nr&gt; ist bereits &lt;Status&gt; &mdash; keine BDE-Buchung
                    mehr moeglich.&ldquo; abgelehnt, und der FA verschwindet aus den
                    Auswahllisten. <strong>Laufende Buchungen</strong> koennen normal beendet,
                    pausiert und mit Mengen gemeldet werden; ungeplante Taetigkeiten bleiben
                    moeglich. Ohne Zusatzinfo-Daten (Sync aus) greift keine Sperre.
                </p>
```

- [ ] **Step 3: TESTSZENARIEN Kap. 55 umbauen + TS-55.10–55.13**

In `docs/TESTSZENARIEN.md`:

**(a) Index-Zeile 77:** `| Kapitel 55: FA-Zusatzinfos (Sage) (v1.26.0) | [→](#kapitel-55-fa-zusatzinfos-sage-v1260) | TS-55.1 – TS-55.9 |` → Range auf `TS-55.1 – TS-55.13` ändern.

**(b) Kopf-Hinweis (Z.5586-5589)** ersetzen durch:

```markdown
> **Hinweis Aktivitaets-Protokoll:** Der Sync erscheint als Eintrag `FaZusatzinfo` mit Counts
> `gelesen/neu/aktualisiert/uebersprungen/erledigt-gesetzt` (bei Cap-Skip zusaetzlich
> `erledigt-kandidaten`). „uebersprungen" = WA-Zeilen ohne FA im WMS (alte/erledigte WAs —
> normal, KEINE Warn-Zeile je WA). Erhoehte Skip-Counts nach einem FA-Import-Fehler sind
> KEIN Bug (heilt sich im Folgezyklus). „erledigt-gesetzt" = FAs, die der Lauf wegen
> Sage-Status verpackt/abgeholt automatisch auf Komm-Erledigt gesetzt hat — je FA eine
> Info-Detailzeile „FA <Nr> auf erledigt gesetzt (Sage-Status: <status>)" (Detailzeilen
> gecappt auf 100/Lauf, der Count zaehlt alle).
```

**(c) TS-55.1 umbauen** (DryRun-Pflicht, Reconcile-Muster) — kompletter Ersatz:

```markdown
### TS-55.1 Gate aus → an (Erstlauf mit DryRun-Pflicht)
1. `/ServiceSettings`: `Sync:FaZusatzinfoEnabled` ist AUS. Einen Sync-Zyklus abwarten.
2. **Erwartet:** KEIN `FaZusatzinfo`-Eintrag im Aktivitaets-Protokoll.
3. **PFLICHT-Vorschritt (Reconcile-Muster):** `WorkerSettings:SyncDryRun = true` setzen,
   DANN das Gate auf AN stellen, Zyklus abwarten.
4. **Erwartet:** `FaZusatzinfo`-Eintrag mit Suffix `[DryRun]`. Count `erledigt-gesetzt`
   kontrollieren — das ist die Erstlauf-Aufraeum-Zahl (ALLE in der App offenen FAs, die in
   Sage bereits verpackt/abgeholt sind, wuerden geschlossen). Plausibel → weiter. Meldet der
   Lauf stattdessen `erledigt-kandidaten` + Warn-Zeile „Auto-Erledigt uebersprungen: N
   Kandidaten > Cap M ..." → Cap `Sync:FaZusatzinfoAutoDoneMaxPerRun` temporaer erhoehen
   ODER in Etappen scharfschalten.
5. DryRun aus → naechster Zyklus schreibt tatsaechlich: `ProductionOrderExtraInfo`-Zeilen
   (CreatedBy `FaZusatzinfoSync`) UND setzt die verpackt/abgeholt-FAs auf Komm-Erledigt
   (`ProductionOrderPickingStatus.IsDonePicking = 1`, ModifiedBy `FaZusatzinfoSync`).
```

**(d) TS-55.7 ergänzen** — nach Punkt 2 anfügen:

```markdown
3. **Erwartet:** Auch die Auto-Erledigt-Automatik stoppt (Gate aus = kein Lauf = kein
   Erledigt-Setzen); bereits automatisch geschlossene FAs bleiben erledigt (Einweg).
```

**(e) Neue Szenarien** nach TS-55.9 (vor „**Negativ/Regression:**") einfügen:

```markdown
### TS-55.10 Auto-Erledigt bei verpackt/abgeholt (inkl. Teilkommissionierung)
1. Einen in der App OFFENEN FA waehlen (nicht erledigt, nicht storniert), dessen WA in Sage
   den Status „verpackt" ODER „abgeholt" traegt. Gate AN (nach TS-55.1-DryRun-Kontrolle),
   Zyklus abwarten.
2. **Erwartet:** Der FA verschwindet aus FA-Liste, Komm-Worklist, FA-Vervollstaendigung und
   FA-Abarbeitungsliste (wie beim manuellen Abschliessen-Button). Aktivitaets-Protokoll:
   Count `erledigt-gesetzt` >= 1 + Info-Zeile „FA <Nr> auf erledigt gesetzt (Sage-Status:
   <status>)". In FA-Liste/Leitstand unter „Erledigte anzeigen" ist der FA sichtbar; die
   einblendbare Spalte „Sage-Status" zeigt den Grund.
3. Teilkomm-Fall: einen FA mit bereits auf den Kommissionierwagen gebuchten Teilen verwenden.
4. **Erwartet:** Auch dieser FA wird geschlossen; die gebuchten Teile bleiben auf dem Wagen
   stehen (bewusste Semantik, wie beim manuellen Abschluss).
5. IDEAL-Linie (mehrere Sub-FAs je WA): ALLE offenen Sub-FAs der WA werden geschlossen
   (N Counts + N Detailzeilen).

### TS-55.11 Einweg-Semantik + Ping-Pong
1. FA aus TS-55.10: In Sage faellt der Status zurueck (z. B. wieder „in Produktion").
   Zyklus abwarten.
2. **Erwartet:** Der FA BLEIBT erledigt (Einweg — kein Auto-Reopen); `aktualisiert` zaehlt
   nur den Satellit-Feldwechsel.
3. Bereits erledigte FAs (Sage-`IsDone` ODER Komm-Erledigt) und stornierte FAs
   (`IsCancelled`) mit Sage-Status verpackt/abgeholt.
4. **Erwartet:** KEIN `erledigt-gesetzt`-Count fuer diese FAs (kein Doppel-Setzen;
   Stornierte bleiben unangetastet).
5. Offener FA mit anderem Status (z. B. „in Produktion") → kein Erledigt-Setzen.
6. Ping-Pong: Einen automatisch geschlossenen FA manuell wieder oeffnen (FA-Liste/Leitstand
   → „Erledigte anzeigen" → Erledigt-Toggle); Sage meldet weiterhin „abgeholt". Zyklus
   abwarten.
7. **Erwartet:** Der naechste Lauf schliesst den FA ERNEUT (bewusst, kein Suppress-Marker —
   dauerhaftes Offenhalten erfordert einen Sage-Statuswechsel).

### TS-55.12 BDE-Sperre: Start/Resume gesperrt + Terminal-Meldung + Listen-Hygiene
Vorbedingung: `BdeAktiv = true`, BDE-aktive Werkbank, FA mit AG an dieser Werkbank, FA traegt
`SageStatus` „verpackt" oder „abgeholt" (Zusatzinfo-Satellit vorhanden).
1. Am BDE-Terminal den AG des gesperrten FA per FA-/AG-Scan aufrufen und „Ruesten starten"
   bzw. „Produktion starten" klicken.
2. **Erwartet:** Roter Toast „FA <Nr> ist bereits <status> — keine BDE-Buchung mehr
   moeglich." KEINE neue Buchung (Buchungsuebersicht kontrollieren).
3. Eine VOR der Statusaenderung pausierte Buchung des FA ueber das Paused-Panel fortsetzen.
4. **Erwartet:** Gleiche Meldung; keine neue Buchung, die pausierte bleibt pausiert.
5. Terminal-Auswahllisten pruefen (normaler AG-Modus UND NurFA-Modus).
6. **Erwartet:** Der gesperrte FA/AG erscheint NICHT mehr in den Auswahllisten; FAs ohne
   Zusatzinfo-Satellit und FAs mit anderem Status bleiben enthalten. Das alte Tracking-Modul
   (Teileverfolgung → ByWorkplace) zeigt die AGs des gesperrten FA WEITERHIN (bewusst
   ungefiltert, Default-Parameter).
7. FA ohne Zusatzinfo-Daten (Sync aus / Satellit fehlt): Start wie bisher moeglich (keine
   Sperre).

### TS-55.13 BDE-Sperre: laufende Buchung bleibt abschliessbar
1. Buchung auf einem FA starten; DANACH wird der FA in Sage „verpackt" (Sync-Zyklus
   abwarten).
2. **Erwartet:** Teilmengen melden, Pausieren und Beenden (inkl. finaler Mengen-Eingabe)
   funktionieren weiter — kein gestrandeter Operator. Nur NEUE Starts/Fortsetzungen sind
   gesperrt.
3. Ungeplante Taetigkeit (z. B. Wartung) an derselben Werkbank starten.
4. **Erwartet:** Startet normal (werkbank-bezogen, ohne FA — keine Sperre).
5. Laufendes Ruesten auf dem FA, dann „Produktion starten" klicken.
6. **Erwartet:** Meldung wie TS-55.12; das Ruesten LAEUFT WEITER (der abgelehnte Start
   schliesst es NICHT — Guard liegt vor der Auto-Close-Transition).

**Recovery (Fehlerfall Auto-Erledigt):** Hat ein View-Defekt trotz Cap FAs faelschlich
geschlossen, laesst sich der Stand zurueckdrehen:
`UPDATE ProductionOrderPickingStatus SET IsDonePicking = 0 WHERE ModifiedBy = 'FaZusatzinfoSync' AND ModifiedAt >= '<Zeitfenster>'`
(Zeitfenster = Beginn des fehlerhaften Laufs; danach zuerst die Ursache beheben — sonst
schliesst der naechste Lauf erneut.)
```

- [ ] **Step 4: CLAUDE.md — Service-Konfig-Tabelle + Fallstrick**

**(a)** In der Tabelle „Service-Konfiguration" die Zeile `| \`Sync:FaZusatzinfoEnabled\` | \`false\` | FA-Zusatzinfos (Sage): ... synchronisieren (v1.26.0) |` ersetzen durch ZWEI Zeilen:

```markdown
| `Sync:FaZusatzinfoEnabled` | `false` | FA-Zusatzinfos (Sage): Kaeltemittel/Ventil/Ausfuehrung/Maschine/Status je FA synchronisieren. ACHTUNG: setzt FAs mit Sage-Status verpackt/abgeholt automatisch auf Komm-Erledigt (vorher DryRun pruefen) (v1.26.0) |
| `Sync:FaZusatzinfoAutoDoneMaxPerRun` | `100` | Sicherheits-Cap: mehr Auto-Erledigt-Kandidaten je Lauf → kein Erledigt-Setzen + Warn-Zeile + Fehlermail (Schutz vor View-Defekten) (v1.26.0) |
```

**(b)** Direkt NACH dem bestehenden Fallstrick-Bullet „**FA-Zusatzinfos (Sage) — read-only Satellit `ProductionOrderExtraInfo` (v1.26.0)**" neuen Bullet anfügen:

```markdown
- **FA-Zusatzinfos Fold 2 — Auto-Erledigt + BDE-Sperre (v1.26.0)**: Der `FaZusatzinfoSyncService` setzt FAs mit Sage-Status **verpackt/abgeholt** automatisch auf Komm-Erledigt (`PickingStatus.IsDonePicking` — Sage-`IsDone` wird NIE beschrieben). Tripel-Bedingung `!IsDone && !IsCancelled && !PickingStatus.IsDonePicking`, Statusquelle ist `row.Status` (frischer View-Wert, NICHT `info.SageStatus`), Check laeuft je gematchter FA-Zeile NACH der Upsert-Kette (greift auch im unveraendert-Zweig; IDEAL-Linie: eine WA schliesst ALLE Sub-FAs). **Einweg + Ping-Pong:** Status-Rueckfall oeffnet nicht; manuelles Wieder-Oeffnen haelt nur bis zum naechsten Lauf mit verpackt/abgeholt. **Zweiphasig + Cap:** Kandidaten sammeln (dedupliziert), Writes nach der Schleife; `> Sync:FaZusatzinfoAutoDoneMaxPerRun` (Default 100, Parameter von `SyncWorker` an `SyncAsync(dryRun, autoDoneMaxPerRun, ct)`) → KEIN Write + Warn „Auto-Erledigt uebersprungen: N Kandidaten > Cap M" + `ISyncErrorNotifier`-Fehlermail, kein throw. Counts `erledigt-gesetzt` (alle 3 Dictionaries) + `erledigt-kandidaten` (nur Cap-Skip), Info-Detailzeilen gecappt 100/Lauf (`reference = OrderNumber`). DryRun-Guard liegt VOR jeder Mutation getrackter Entities (zyklusweiter Scope!). Fehlende `PickingStatus`-Zeile wird eager angelegt (Audit `FaZusatzinfoSync`). **Gemeinsame Status-Wahrheit:** `FaZusatzinfoStatus.IstVerpacktOderAbgeholt` (Trim + OrdinalIgnoreCase, `IdealAkeWms/Services/FaZusatzinfoStatus.cs`) — EF-Listen-Filter nutzen die lowercase-Konstanten INLINE mit ausgeschriebenem Null-Guard-Praedikat (statischer Helper ist nicht EF-uebersetzbar). **BDE-Sperre:** Guard `EnsureOrderNotPackedAsync` in `BdeBookingService.StartPlannedAsync` (nach Werkbank-Gate, VOR der Auto-Close-Transition) + `ResumeAsync` (nach Parent-Load, NUR `parent.WorkOperationId.HasValue` — Activity-Resume bleibt frei) → `InvalidState` „FA <Nr> ist bereits <status> — keine BDE-Buchung mehr moeglich.". KEIN Guard in Beenden-/Pausieren-/Mengen-Pfaden; `IsDoneBde` blockt nichts und bleibt unbeschrieben; ohne Satellit keine Sperre. Listen-Hygiene: NurFA-Query (BdeApiController) + `GetOpenByWorkplaceIdAsync(workplaceId, excludePackedOrders = false)` — Default false erhaelt Tracking/ByWorkplace, NUR der BDE-Aufrufer setzt true (Moq-Setups muessen beide Argumente explizit angeben). `bde-terminal.js post()` zeigt `InvalidState` als Toast (kein catch-all-else — `GroupFinishRequired` behandelt der Aufrufer). Erstlauf-Pflicht: DryRun + `erledigt-gesetzt` kontrollieren; Recovery-SQL in TESTSZENARIEN Kap. 55.
```

- [ ] **Step 5: README, PROJECT_STATUS, Spec §7**

**(a) `README.md:625`** — Bullet ersetzen durch:

```markdown
- **FA-Zusatzinfos (Sage)**: Kältemittel/Ventil/Ausführung E-Z/Maschine/Status je FA aus `vw_IDEAL_AKE_WMS_FAZusatzinformationen` → `ProductionOrderExtraInfo` (1:1-Satellit, in der App read-only, kein Löschen; `Sync:FaZusatzinfoEnabled`, v1.26.0). Fold 2: FAs mit Sage-Status **verpackt/abgeholt** setzt der Sync automatisch auf Komm-Erledigt (`PickingStatus.IsDonePicking` — die einzige Schreib-Wirkung außerhalb des Satelliten; einweg, Cap `Sync:FaZusatzinfoAutoDoneMaxPerRun` Default 100, vor dem Scharfschalten DryRun prüfen), und das BDE-Terminal sperrt neue Buchungen auf solche FAs
```

**(b) `PROJECT_STATUS.md`** — an den „In Arbeit"-Absatz (endet Z.19 `... → Web (Migration+Katalog-Seed) → Service publishen → DryRun → scharf.`) anfügen:

```markdown
**Fold 2 (gleicher Branch, v1.26.0, kein Schema-Change):** Auto-Erledigt bei Sage-Status
verpackt/abgeholt (`PickingStatus.IsDonePicking`, einweg, Cap
`Sync:FaZusatzinfoAutoDoneMaxPerRun` Default 100, Counts `erledigt-gesetzt`/
`erledigt-kandidaten`) + BDE-Buchungs-Sperre (Guard in Start/Resume, Listen-Hygiene,
InvalidState-Toast). Plan: `docs/superpowers/plans/2026-07-22-pa-zusatzinfos-fold2-auto-done.md`.
Erstlauf-Pflicht: DryRun fahren + `erledigt-gesetzt` kontrollieren (TESTSZENARIEN Kap. 55,
TS-55.1/55.10–55.13).
```

**(c) Spec `docs/superpowers/specs/2026-07-22-pa-zusatzinfos-design.md` §7 Schritt 4** — ersetzen:

Alt:
```markdown
4. `WorkerSettings:SyncDryRun = true` + Gate aktivieren → einen Zyklus
   abwarten, Aktivitaets-Protokoll pruefen (`gelesen/neu/…` plausibel).
```

Neu:
```markdown
4. `WorkerSettings:SyncDryRun = true` + Gate aktivieren → einen Zyklus
   abwarten, Aktivitaets-Protokoll pruefen (`gelesen/neu/…` plausibel;
   **explizit `erledigt-gesetzt` kontrollieren — Erstlauf-Aufraeum-Zahl!**
   Bei `erledigt-kandidaten > Cap`: Cap temporaer erhoehen oder in Etappen
   scharfschalten).
```

- [ ] **Step 6: secondbrain-Spiegelung (Worktree OHNE Junctions!)**

Alle geänderten/neuen `docs/`-Dateien identisch nach `secondbrain/docs/` kopieren — inkl. dieses Plans:

```bash
cp docs/TESTSZENARIEN.md secondbrain/docs/TESTSZENARIEN.md
cp docs/superpowers/specs/2026-07-22-pa-zusatzinfos-design.md secondbrain/docs/superpowers/specs/2026-07-22-pa-zusatzinfos-design.md
cp docs/superpowers/plans/2026-07-22-pa-zusatzinfos-fold2-auto-done.md secondbrain/docs/superpowers/plans/2026-07-22-pa-zusatzinfos-fold2-auto-done.md
```

- [ ] **Step 7: Build-Smoke (Views kompilieren)**

Run: `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly`
Expected: Exit 0 (Razor-Views Changelog/Index fehlerfrei).

- [ ] **Step 8: Commit**

```bash
git add IdealAkeWms/Views/Help/Changelog.cshtml IdealAkeWms/Views/Help/Index.cshtml docs/TESTSZENARIEN.md CLAUDE.md README.md PROJECT_STATUS.md docs/superpowers/specs/2026-07-22-pa-zusatzinfos-design.md docs/superpowers/plans/2026-07-22-pa-zusatzinfos-fold2-auto-done.md secondbrain/docs/TESTSZENARIEN.md secondbrain/docs/superpowers/specs/2026-07-22-pa-zusatzinfos-design.md secondbrain/docs/superpowers/plans/2026-07-22-pa-zusatzinfos-fold2-auto-done.md
git commit -m "docs(fa-zusatzinfo): Fold-2-Doku — Changelog, Hilfe (4 Stellen), TESTSZENARIEN 55.1/55.7/55.10-55.13, CLAUDE/README/STATUS, Spec §7 + secondbrain-Spiegel

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 5: Final-Check — Voll-Build + beide Suiten + Zwillings-Hashes (KEIN Merge)

**Files:** keine Änderungen (nur Verifikation).

- [ ] **Step 1: Voll-Build**

Run: `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly`
Expected: Exit 0.

- [ ] **Step 2: Beide Testsuiten komplett**

Run: `dotnet test IdealAkeWms.Tests --nologo` und `dotnet test IDEALAKEWMSService.Tests --nologo`
Expected: Alle grün. Erwartete Zuwächse gegenüber Baseline: Web +13 (`FaZusatzinfoStatusTests`) +1 (Drift-Guard-InlineData) +12 Methoden (`BdeBookingServiceTests`, 14 Fälle inkl. Theory) +2 (`WorkOperationRepositoryExtendedTests`) +1 (`BdeApiControllerTests`); Service +14 (`FaZusatzinfoSyncServiceTests`).

- [ ] **Step 3: Zwillings-Hash-Check (docs vs. secondbrain/docs)**

```bash
git ls-files -s docs/TESTSZENARIEN.md secondbrain/docs/TESTSZENARIEN.md
git ls-files -s docs/superpowers/specs/2026-07-22-pa-zusatzinfos-design.md secondbrain/docs/superpowers/specs/2026-07-22-pa-zusatzinfos-design.md
git ls-files -s docs/superpowers/plans/2026-07-22-pa-zusatzinfos-fold2-auto-done.md secondbrain/docs/superpowers/plans/2026-07-22-pa-zusatzinfos-fold2-auto-done.md
```

Expected: Je Paar identische Blob-Hashes.

- [ ] **Step 4: Arbeitsstand pruefen**

Run: `git status --short && git log --oneline -6`
Expected: Working tree sauber; die 4 Fold-2-Commits (Task 1–4) oben auf `feature/pa-zusatzinfos`.

- [ ] **Step 5: PAUSE — User-Test**

**KEIN Merge, KEIN Push, KEIN Worktree-Cleanup.** Der Branch wartet (wie Fold 1) NICHT-autonom auf den User-Test: Manual-UAT TESTSZENARIEN Kap. 55 (insb. TS-55.1 DryRun-Pflicht, TS-55.10–55.13) am Zielsystem. Dem User den Abschluss melden inkl. Testanzahl und Hinweis auf die Erstlauf-DryRun-Pflicht.

---

## Abweichungen von der Spec

1. **Kandidaten-Container:** Spec §10.2 nennt ein `HashSet<int>` (Order-Ids). Der Plan verwendet `Dictionary<int, (ProductionOrder Order, string SageStatus)>` — die Keys leisten dieselbe Deduplizierung, das Dictionary traegt zusaetzlich die fuer die Write-Phase/Detailzeile noetigen Daten (Order-Referenz + getrimmter Status). Rein mechanische Abweichung, Semantik identisch.
2. **„SyncWorkerTests: Signatur-Anpassung des Mocks"** (Task-Briefing): NICHT noetig — `SyncWorkerTests` mockt `IFaZusatzinfoSyncService` nicht (nur `mockServiceProvider.Verify(GetService(...), Times.Never())`, signatur-unabhaengig). Die Signaturaenderung schlaegt stattdessen in `FaZusatzinfoSyncServiceTests` durch (Build-Tupel + 8 `SyncAsync`-Aufrufe + Ctor).
3. **Detail-Info-Zeilen auch im DryRun:** Spec §10.3 legt das nicht explizit fest; der Plan loggt die „FA <Nr> auf erledigt gesetzt"-Zeilen auch im DryRun (der Lauf traegt das `[DryRun]`-Suffix) — gewollt fuer den PFLICHT-DryRun-Kontrollblick des Admins. Counts identisch zur Spec.
4. **Cap-Verhalten im DryRun:** Kandidaten > Cap fuehrt auch im DryRun zu Warn-Zeile + Fehlermail + `erledigt-kandidaten`/`erledigt-gesetzt=0` (Spec unterscheidet hier nicht; so sieht der Admin das Cap-Problem bereits in der Pflicht-DryRun-Phase). TS-55.1 dokumentiert das.
5. **Status-Text in der BDE-Meldung:** `<status>` wird als getrimmter Original-View-Wert gerendert (z. B. `" Verpackt "` → `Verpackt`), nicht lowercase-normalisiert — die Spec-Vorlage `„FA <Nr> ist bereits <status> …"` laesst das offen.
6. **Hilfe-Zeilenanker:** Der „ServiceSettings-Hinweis Z.~320" liegt real im `<dd>` Z.313-322 (`<dt>FA-Zusatzinfos aus Sage in FA-Liste + Leitstand</dt>`); die Kommissionierungs-Card beginnt Z.707, die Leitstand-Card Z.535 — Zeilennummern verschieben sich beim Editieren, Anker sind die zitierten Textstellen.
7. **TESTSZENARIEN „Stand:"-Zeile** bleibt `v1.26.0 (2026-07-22)` — Fold 2 ist im selben Release gefaltet (kein Versions-Bump laut §10-Kopf).
8. **`SeedOrder`/`SeedExtraInfo` in FaZusatzinfoSyncServiceTests:** `SeedExtraInfo` hat bereits einen `status`-Parameter (Default „in Produktion") — die neuen Tests nutzen ihn unveraendert; nur `SeedOrder` bekommt `isDone`/`isCancelled` (Spec §10.4 nennt nur `SeedPickingStatus` als „noetig", die Faelle 4/5 brauchen aber auch die Order-Flags).

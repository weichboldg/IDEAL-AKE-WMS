# Activity-Log fuer Non-Sync-Services Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Drei Non-Sync-Services (PartRequisitionEmail, WarehouseRequisitionEmail, BdeAutoPause) ueber den bestehenden `ISyncLogger` ins zentrale Audit-Log integrieren, plus UI-Umbenennung "Sync-Protokoll" → "Aktivitaets-Protokoll".

**Architecture:** Wiederverwendung der `ISyncLogger`/`ISyncRun`-Infrastruktur aus v1.15.0. Drei neue `SyncLogServices`-Konstanten, drei Service-Wrappings nach dem etablierten Lifecycle-Pattern (Begin → Mid-Run-Events → Finish), UI-Labels umbenennen ohne Schema-/Routes-/Klassen-Refactor.

**Tech Stack:** .NET 10, EF Core 10, xUnit + FluentAssertions, vorhandener `ApplicationDbContext` (kein Schema-Change), `FakeSyncLogger`-Test-Helper aus v1.15.0.

**Spec:** [secondbrain/docs/superpowers/specs/2026-05-27-activity-log-non-sync-services-design.md](../specs/2026-05-27-activity-log-non-sync-services-design.md) (Commit `a40f978`)

---

## File Structure

**Modifiziert (Web):**
- `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs` — 3 neue Konstanten + erweitertes `All`
- `IdealAkeWms/Views/Shared/_Layout.cshtml:193` — Nav-Label
- `IdealAkeWms/Views/SyncLog/Index.cshtml:3,5,9,42` — Title, Header, Filter-Label, Spalten-Header
- `IdealAkeWms/Views/Help/Index.cshtml` — 5 Occurrences von "Sync-Protokoll" + Service-Liste-Block
- `IdealAkeWms/Views/Help/Changelog.cshtml` — v1.15.1-Eintrag
- `IdealAkeWms/AppVersion.cs` — 1.15.1

**Modifiziert (Service):**
- `IDEALAKEWMSService/AppVersion.cs` — 1.15.1
- `IDEALAKEWMSService/Services/PartRequisitionEmailService.cs` — ISyncLogger-Integration
- `IDEALAKEWMSService/Services/WarehouseRequisitionEmailService.cs` — ISyncLogger-Integration
- `IDEALAKEWMSService/Services/BdeAutoPauseService.cs` — ISyncLogger-Integration

**Modifiziert (Tests):**
- `IDEALAKEWMSService.Tests/Services/WarehouseRequisitionEmailServiceTests.cs` — Konstruktor-Anpassung + Lifecycle-Test + Reference-Test
- `IDEALAKEWMSService.Tests/Services/BdeAutoPauseServiceTests.cs` — Konstruktor-Anpassung + Lifecycle-Test + Reference-Test
- **Neu:** `IDEALAKEWMSService.Tests/Services/PartRequisitionEmailServiceTests.cs` — neuer Test-File, mindestens Lifecycle + Reference

**Modifiziert (Doku):**
- `docs/TESTSZENARIEN.md` — Kapitel 28 (3 Szenarien)
- `PROJECT_STATUS.md` — Fortschritts-Sektion + Hauptfunktionen + Roadmap
- `CLAUDE.md` — Asymmetrie-Hinweis

---

## Task 0: Pre-flight — Repo-weite Suche nach "Sync-Protokoll"-Strings

**Files:** keine Aenderung — diagnostisch.

- [ ] **Step 1: Grep nach allen Vorkommen**

```bash
git grep -n "Sync-Protokoll" -- '*.cs' '*.cshtml' '*.md'
```

Bekannte Treffer (aus Pre-flight-Analyse):
- `IdealAkeWms/Views/Shared/_Layout.cshtml:193` — Nav-Menu
- `IdealAkeWms/Views/SyncLog/Index.cshtml:3` — ViewData Title
- `IdealAkeWms/Views/SyncLog/Index.cshtml:5` — Page-Header
- `IdealAkeWms/Views/Help/Index.cshtml:859, 888, 911, 914, 916` — Hilfeseiten-Block

Wenn weitere Treffer auftauchen → in Task 5 mit aufnehmen.

- [ ] **Step 2: Grep nach "SendPendingEmailsAsync" + "RunAsync" Aufrufstellen**

```bash
git grep -n "PartRequisitionEmailService\|WarehouseRequisitionEmailService\|BdeAutoPauseService" -- '*.cs'
```

Damit alle Konstruktor-Aufrufer (DI-Registrierung + Tests) bekannt sind.

- [ ] **Step 3: Bestaetigen**

Wenn keine ueberraschenden Treffer: weiter zu Task 1. Sonst dokumentiert (z.B. zusaetzliche Test-Datei, andere View-Stelle) und in den entsprechenden Task aufnehmen.

---

## Task 1: SyncLogServices um 3 Konstanten erweitern

**Files:**
- Modify: `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs`

- [ ] **Step 1: 3 Konstanten hinzufuegen + `All` erweitern**

Vorher (aktueller Inhalt):
```csharp
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

Nachher (3 neue Eintraege ergaenzt — VOR `All`):
```csharp
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

    // Non-Sync-Aktivitaeten (seit v1.15.1)
    public const string PartRequisitionEmail = "PartRequisitionEmail";
    public const string WarehouseRequisitionEmail = "WarehouseRequisitionEmail";
    public const string BdeAutoPause = "BdeAutoPause";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        Lagerplatz, Lagerbestand, BomCache, OseonTracking, EnaioDms,
        Holiday, CoatingDetection, ProductionOrder, Article,
        PartRequisitionEmail, WarehouseRequisitionEmail, BdeAutoPause,
    };
}
```

- [ ] **Step 2: Build verifizieren**

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
```

Erwartet: 0 Fehler.

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms/Services/SyncLogger/SyncLogServices.cs
git commit -m "feat(synclog): add 3 non-sync activity service names"
```

---

## Task 2: PartRequisitionEmailService integrieren (mit neuem Test-File)

**Files:**
- Modify: `IDEALAKEWMSService/Services/PartRequisitionEmailService.cs`
- Create: `IDEALAKEWMSService.Tests/Services/PartRequisitionEmailServiceTests.cs`

### Step 1: Konstruktor erweitern + Methode wrappen

Konstruktor (aktuell, Zeilen 12-20):
```csharp
public PartRequisitionEmailService(
    IConfiguration configuration,
    IMailService mailService,
    ILogger<PartRequisitionEmailService> logger)
```

Erweitern auf (neuer Param `syncLogger` nach `logger`, Konvention aus v1.15.0):
```csharp
public PartRequisitionEmailService(
    IConfiguration configuration,
    IMailService mailService,
    ILogger<PartRequisitionEmailService> logger,
    IdealAkeWms.Services.SyncLogger.ISyncLogger syncLogger)
```

- [ ] **Step 1.1: `using IdealAkeWms.Services.SyncLogger;` am Datei-Anfang ergaenzen**

- [ ] **Step 1.2: Field `_syncLogger` ergaenzen und im Konstruktor zuweisen**

- [ ] **Step 1.3: `SendPendingEmailsAsync(bool dryRun, CancellationToken ct = default)` wrappen**

Top-Level-Methode-Struktur nach Aenderung:

```csharp
public async Task<int> SendPendingEmailsAsync(bool dryRun, CancellationToken ct = default)
{
    await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.PartRequisitionEmail, ct);
    int sentCount = 0;
    int skippedCount = 0;
    int errorCount = 0;
    try
    {
        // ...bestehende Logik VOR der foreach-Schleife bleibt unveraendert...

        foreach (var group in groups)
        {
            try
            {
                // ...bestehende Versand-Logik...
                // Bei "keine aktiven Empfaenger" (existing skip path):
                if (recipients.Count == 0)
                {
                    await run.LogWarningAsync("Keine aktiven Empfaenger",
                                              reference: group.OrderNumber, ct);
                    skippedCount++;
                    continue;
                }

                // ...Mail-Versand...
                if (!dryRun) await _mailService.SendAsync(/* ... */, ct);
                await run.LogInfoAsync($"Mail versendet ({recipients.Count} Empfaenger)",
                                       reference: group.OrderNumber, ct);
                sentCount++;
            }
            catch (Exception ex)
            {
                await run.LogWarningAsync($"Mail-Versand fehlgeschlagen: {ex.Message}",
                                          reference: group.OrderNumber, ct);
                errorCount++;
            }
        }

        await run.FinishSuccessAsync(new Dictionary<string, int>
        {
            ["versendet"] = sentCount,
            ["ohne_empfaenger"] = skippedCount,
            ["fehler"] = errorCount,
        }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);

        return sentCount;
    }
    catch (Exception ex)
    {
        await run.LogErrorAsync(ex.Message, ct: ct);
        await run.FinishFailedAsync(ex.Message, ct: ct);
        throw;
    }
}
```

**Reference-Konvention:** `group.OrderNumber` (FA-Nummer).

**Hinweis fuer den Engineer:** Die `skippedCount`-Variable existiert in der heutigen Implementierung evtl. nicht — sie muss neu eingefuehrt werden, gemeinsam mit `errorCount`. Diese sind aber nur Logger-Counter, beeinflussen die bestehende Rueckgabe `sentCount` nicht.

### Step 2: Neuer Test-File

Datei: `IDEALAKEWMSService.Tests/Services/PartRequisitionEmailServiceTests.cs` (NEU)

```csharp
using FluentAssertions;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IDEALAKEWMSService.Tests.Services;

public class PartRequisitionEmailServiceTests
{
    private static PartRequisitionEmailService Build(
        IConfiguration configuration,
        IMailService mailService,
        FakeSyncLogger fakeLogger)
    {
        return new PartRequisitionEmailService(
            configuration,
            mailService,
            NullLogger<PartRequisitionEmailService>.Instance,
            fakeLogger);
    }

    [Fact]
    public async Task Send_writes_lifecycle_to_synclogger_when_no_pending_emails()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var mailMock = new Mock<IMailService>();
        var fakeLogger = new FakeSyncLogger();
        var service = Build(config, mailMock.Object, fakeLogger);

        // Act
        var result = await service.SendPendingEmailsAsync(dryRun: false, ct: CancellationToken.None);

        // Assert
        result.Should().Be(0);
        fakeLogger.Runs.Should().ContainSingle();
        fakeLogger.Runs[0].ServiceName.Should().Be("PartRequisitionEmail");
        fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
        fakeLogger.Runs[0].FinalCounts.Should().NotBeNull();
        fakeLogger.Runs[0].FinalCounts!.Should().ContainKey("versendet");
    }
}
```

**Hinweis fuer den Engineer:** PartRequisitionEmailService hat heute keine Tests. Dieser Lifecycle-Test ist der Minimum-Smoke. Falls die Service-Implementierung eine DB benoetigt (z.B. `IConfiguration` mit ConnectionString fuer einen DB-Read), kann dieser Test entweder mit einem Mock-`IConfiguration` arbeiten der eine leere/in-memory DB liefert, oder das Setup auf `TestApplicationDbContext`-Niveau erweitern. In dem Fall: zusatz-Helper-Test-Builder.

### Step 3: Build + Tests

```bash
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Debug --nologo
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-restore -c Debug --nologo --filter "FullyQualifiedName~PartRequisitionEmailServiceTests"
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: 0 Fehler, mindestens 1 PartRequisitionEmail-Test pass, restliche 79 Tests bleiben gruen.

### Step 4: Commit

```bash
git add IDEALAKEWMSService/Services/PartRequisitionEmailService.cs IDEALAKEWMSService.Tests/Services/PartRequisitionEmailServiceTests.cs
git commit -m "feat(activitylog): integrate PartRequisitionEmailService with ISyncLogger"
```

---

## Task 3: WarehouseRequisitionEmailService integrieren

**Files:**
- Modify: `IDEALAKEWMSService/Services/WarehouseRequisitionEmailService.cs`
- Modify: `IDEALAKEWMSService.Tests/Services/WarehouseRequisitionEmailServiceTests.cs`

### Step 1: Konstruktor erweitern + Methode wrappen

Konstruktor (aktuell, Zeilen 18-26):
```csharp
public WarehouseRequisitionEmailService(
    ApplicationDbContext ctx,
    IWarehouseRequisitionRepository repo,
    IMailService mail,
    IConfiguration config,
    ILogger<WarehouseRequisitionEmailService> logger)
```

Erweitern um `IdealAkeWms.Services.SyncLogger.ISyncLogger syncLogger` als 6. Parameter (nach `logger`):

```csharp
public WarehouseRequisitionEmailService(
    ApplicationDbContext ctx,
    IWarehouseRequisitionRepository repo,
    IMailService mail,
    IConfiguration config,
    ILogger<WarehouseRequisitionEmailService> logger,
    IdealAkeWms.Services.SyncLogger.ISyncLogger syncLogger)
```

- [ ] **Step 1.1: `using IdealAkeWms.Services.SyncLogger;` ergaenzen, `_syncLogger` Field + Constructor-Assignment**

- [ ] **Step 1.2: `SendPendingEmailsAsync` wrappen**

```csharp
public async Task<EmailResult> SendPendingEmailsAsync(bool dryRun, CancellationToken ct = default)
{
    await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.WarehouseRequisitionEmail, ct);
    int submitCount = 0;
    int cancelCount = 0;
    var errors = new List<string>();

    try
    {
        // Submit-Queue (bestehende foreach-Schleife — bleibt strukturell)
        foreach (var req in await _repo.GetPendingSubmitsAsync(ct))
        {
            try
            {
                // ...Mail-Versand...
                if (!dryRun) await _mail.SendAsync(/* ... */, ct);
                await _repo.MarkEmailSentAsync(req, ct);

                await run.LogInfoAsync($"Submit-Mail versendet",
                                       reference: $"submit/{req.Id}", ct);
                submitCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Submit {req.Id}: {ex.Message}");
                await run.LogWarningAsync($"Submit-Mail fehlgeschlagen: {ex.Message}",
                                          reference: $"submit/{req.Id}", ct);
            }
        }

        // Storno-Queue (bestehende foreach-Schleife — bleibt strukturell)
        foreach (var req in await _repo.GetPendingCancelsAsync(ct))
        {
            try
            {
                // ...Mail-Versand...
                if (!dryRun) await _mail.SendAsync(/* ... */, ct);
                await _repo.MarkCancellationSentAsync(req, ct);

                await run.LogInfoAsync($"Storno-Mail versendet",
                                       reference: $"storno/{req.Id}", ct);
                cancelCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Storno {req.Id}: {ex.Message}");
                await run.LogWarningAsync($"Storno-Mail fehlgeschlagen: {ex.Message}",
                                          reference: $"storno/{req.Id}", ct);
            }
        }

        await run.FinishSuccessAsync(new Dictionary<string, int>
        {
            ["submit_versendet"] = submitCount,
            ["storno_versendet"] = cancelCount,
            ["fehler"] = errors.Count,
        }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);

        return new EmailResult(submitCount, cancelCount, errors);
    }
    catch (Exception ex)
    {
        await run.LogErrorAsync(ex.Message, ct: ct);
        await run.FinishFailedAsync(ex.Message, ct: ct);
        throw;
    }
}
```

**Reference-Konvention:** `"submit/{req.Id}"` oder `"storno/{req.Id}"`.

### Step 2: Bestehende Tests anpassen

- [ ] **Step 2.1: `using IdealAkeWms.Tests.Helpers;` ergaenzen** (FakeSyncLogger ist dort, project-reference besteht)

- [ ] **Step 2.2: Konstruktor-Aufrufe in allen `new WarehouseRequisitionEmailService(...)`-Sites erweitern**

```bash
git grep -n "new WarehouseRequisitionEmailService(" -- '*.cs'
```

Jeden Aufruf ergaenzen um `new FakeSyncLogger()` als 6. Argument.

- [ ] **Step 2.3: Neuer Lifecycle-Test ans Ende der Test-Klasse anfuegen**

```csharp
[Fact]
public async Task Send_writes_lifecycle_to_synclogger()
{
    using var ctx = TestDbContextFactory.Create();
    var repo = new WarehouseRequisitionRepository(ctx);
    var mailMock = new Mock<IMailService>();
    var config = new ConfigurationBuilder().Build();
    var fakeLogger = new FakeSyncLogger();
    var service = new WarehouseRequisitionEmailService(ctx, repo, mailMock.Object, config,
        NullLogger<WarehouseRequisitionEmailService>.Instance, fakeLogger);

    await service.SendPendingEmailsAsync(dryRun: false, ct: CancellationToken.None);

    fakeLogger.Runs.Should().ContainSingle();
    fakeLogger.Runs[0].ServiceName.Should().Be("WarehouseRequisitionEmail");
    fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
}
```

- [ ] **Step 2.4: Neuer Reference-Test (Submit + Storno differenziert)**

```csharp
[Fact]
public async Task Send_uses_submit_and_storno_reference_prefixes()
{
    using var ctx = TestDbContextFactory.Create();
    // Seed: ein Submit-Pending + ein Cancel-Pending (Setup analog zu bestehenden Tests)
    // ...

    var fakeLogger = new FakeSyncLogger();
    var service = /* siehe Build() oben */;

    await service.SendPendingEmailsAsync(dryRun: false, ct: CancellationToken.None);

    var events = fakeLogger.Runs[0].Events;
    events.Should().Contain(e => e.Reference != null && e.Reference.StartsWith("submit/"));
    events.Should().Contain(e => e.Reference != null && e.Reference.StartsWith("storno/"));
}
```

**Hinweis fuer den Engineer:** Setup fuer Submit-Pending vs. Cancel-Pending muss aus den bestehenden Tests uebernommen werden (vermutlich `ctx.WarehouseRequisitions.Add(...)` mit unterschiedlichen `EmailSentAt`/`CancellationRequestedAt`-States).

### Step 3: Build + Tests

```bash
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Debug --nologo
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-restore -c Debug --nologo --filter "FullyQualifiedName~WarehouseRequisitionEmail"
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: 0 Fehler, alle Tests gruen.

### Step 4: Commit

```bash
git add IDEALAKEWMSService/Services/WarehouseRequisitionEmailService.cs IDEALAKEWMSService.Tests/Services/WarehouseRequisitionEmailServiceTests.cs
git commit -m "feat(activitylog): integrate WarehouseRequisitionEmailService with ISyncLogger"
```

---

## Task 4: BdeAutoPauseService integrieren

**Files:**
- Modify: `IDEALAKEWMSService/Services/BdeAutoPauseService.cs`
- Modify: `IDEALAKEWMSService.Tests/Services/BdeAutoPauseServiceTests.cs`

### Step 1: Konstruktor erweitern + RunAsync wrappen

Konstruktor (aktuell, Zeilen 17-24):
```csharp
public BdeAutoPauseService(
    ApplicationDbContext ctx,
    IBdeShiftCalendarService calendar,
    IAppSettingRepository settings,
    ILogger<BdeAutoPauseService> logger)
```

Erweitern um `ISyncLogger`:

```csharp
public BdeAutoPauseService(
    ApplicationDbContext ctx,
    IBdeShiftCalendarService calendar,
    IAppSettingRepository settings,
    ILogger<BdeAutoPauseService> logger,
    IdealAkeWms.Services.SyncLogger.ISyncLogger syncLogger)
```

- [ ] **Step 1.1: Using + Field**

- [ ] **Step 1.2: `RunAsync(CancellationToken ct)` wrappen**

```csharp
public async Task<AutoPauseResult> RunAsync(CancellationToken ct)
{
    await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.BdeAutoPause, ct);
    var errors = new List<string>();
    int paused = 0;
    int checkedCount = 0;

    try
    {
        var active = await _ctx.BdeBookings
            .Where(b => b.Status == BdeBookingStatus.Running && b.EndedAt == null)
            .ToListAsync(ct);
        checkedCount = active.Count;

        foreach (var booking in active)
        {
            try
            {
                var shiftEnd = _calendar.GetShiftEnd(booking);
                if (DateTime.Now < shiftEnd) continue;

                booking.Status = BdeBookingStatus.AutoPaused;
                booking.EndedAt = shiftEnd;
                // ...bestehende SaveChanges-Logik...

                await run.LogInfoAsync($"Booking auto-paused (Schichtende {shiftEnd:HH:mm})",
                                       reference: $"booking/{booking.Id}", ct);
                paused++;
            }
            catch (Exception ex)
            {
                errors.Add($"Booking {booking.Id}: {ex.Message}");
                await run.LogWarningAsync($"Auto-Pause fehlgeschlagen: {ex.Message}",
                                          reference: $"booking/{booking.Id}", ct);
            }
        }

        await run.FinishSuccessAsync(new Dictionary<string, int>
        {
            ["geprueft"] = checkedCount,
            ["pausiert"] = paused,
            ["fehler"] = errors.Count,
        }, ct: ct);

        return new AutoPauseResult(checkedCount, paused, errors);
    }
    catch (Exception ex)
    {
        await run.LogErrorAsync(ex.Message, ct: ct);
        await run.FinishFailedAsync(ex.Message, ct: ct);
        throw;
    }
}
```

**Reference-Konvention:** `"booking/{booking.Id}"`.

**Hinweis fuer den Engineer:** Die echte Booking-Query/SaveChanges-Logik im bestehenden Code bleibt unveraendert — nur die Logger-Aufrufe sind ergaenzt. Die `MasterToggleOff`-Pfad (frueher Return wenn `BdeSchichtkalenderAktiv=false`) muss ebenfalls eine FinishSuccessAsync-Pfad bekommen, sonst orphant der Run.

Wenn die heutige Implementation einen Early-Return hat (z.B. wenn ShiftCalendar nicht aktiv ist):

```csharp
// Vor dem Active-Booking-Load:
if (!await _settings.GetBoolAsync("BdeSchichtkalenderAktiv", false, ct))
{
    await run.FinishSuccessAsync(new Dictionary<string, int>
    {
        ["geprueft"] = 0, ["pausiert"] = 0, ["fehler"] = 0,
    }, messageSuffix: "deaktiviert", ct: ct);
    return new AutoPauseResult(0, 0, new List<string>());
}
```

### Step 2: Bestehende Tests anpassen

```bash
git grep -n "new BdeAutoPauseService(" -- '*.cs'
```

Pro Aufruf: 5. Argument `new FakeSyncLogger()` ergaenzen.

Add `using IdealAkeWms.Tests.Helpers;`.

- [ ] **Step 2.1: Neuer Lifecycle-Test**

```csharp
[Fact]
public async Task RunAsync_writes_lifecycle_to_synclogger()
{
    using var ctx = TestDbContextFactory.Create();
    var calendarMock = /* siehe bestehende Test-Setups */;
    var settingsMock = new Mock<IAppSettingRepository>();
    settingsMock.Setup(s => s.GetBoolAsync("BdeSchichtkalenderAktiv", false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
    var fakeLogger = new FakeSyncLogger();
    var service = new BdeAutoPauseService(ctx, calendarMock.Object, settingsMock.Object,
        NullLogger<BdeAutoPauseService>.Instance, fakeLogger);

    await service.RunAsync(CancellationToken.None);

    fakeLogger.Runs.Should().ContainSingle();
    fakeLogger.Runs[0].ServiceName.Should().Be("BdeAutoPause");
    fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
}
```

- [ ] **Step 2.2: Neuer Reference-Test (mit auto-paused Booking-Id 42)**

```csharp
[Fact]
public async Task RunAsync_logs_booking_id_in_reference_on_pause()
{
    using var ctx = TestDbContextFactory.Create();
    var booking = new BdeBooking { Id = 42, Status = BdeBookingStatus.Running, /* ... */ };
    ctx.BdeBookings.Add(booking);
    await ctx.SaveChangesAsync();
    // ShiftCalendar so mocken, dass shiftEnd in der Vergangenheit liegt

    var fakeLogger = new FakeSyncLogger();
    var service = /* Build wie oben */;

    await service.RunAsync(CancellationToken.None);

    fakeLogger.Runs[0].Events.Should().Contain(e =>
        e.Level == "Info" && e.Reference == "booking/42");
}
```

### Step 3: Build + Tests

```bash
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Debug --nologo
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-restore -c Debug --nologo --filter "FullyQualifiedName~BdeAutoPauseService"
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: alle gruen.

### Step 4: Commit

```bash
git add IDEALAKEWMSService/Services/BdeAutoPauseService.cs IDEALAKEWMSService.Tests/Services/BdeAutoPauseServiceTests.cs
git commit -m "feat(activitylog): integrate BdeAutoPauseService with ISyncLogger"
```

---

## Task 5: UI-Umbenennung "Sync-Protokoll" → "Aktivitaets-Protokoll"

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml:193`
- Modify: `IdealAkeWms/Views/SyncLog/Index.cshtml:3, 5, 9, 42`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml` (5 Stellen: Zeilen 859, 888, 911, 914, 916)

### Step 1: Nav-Menue

In `IdealAkeWms/Views/Shared/_Layout.cshtml:193`:

```html
<!-- vorher -->
<li><a class="dropdown-item" asp-controller="SyncLog" asp-action="Index">Sync-Protokoll</a></li>

<!-- nachher -->
<li><a class="dropdown-item" asp-controller="SyncLog" asp-action="Index">Aktivitaets-Protokoll</a></li>
```

### Step 2: SyncLog/Index.cshtml — 4 Labels

In `IdealAkeWms/Views/SyncLog/Index.cshtml`:

- Zeile 3: `ViewData["Title"] = "Sync-Protokoll";` → `ViewData["Title"] = "Aktivitaets-Protokoll";`
- Zeile 5: `<h2 class="page-header">Sync-Protokoll</h2>` → `<h2 class="page-header">Aktivitaets-Protokoll</h2>`
- Zeile 9: `<label class="form-label">Service</label>` → `<label class="form-label">Aktivitaet</label>`
- Zeile 42: `<th data-col-key="Service">Service</th>` → `<th data-col-key="Service">Aktivitaet</th>`

**WICHTIG:** Das Attribut `data-col-key="Service"` bleibt UNVERAENDERT — sonst brechen bestehende URL-Filter `?colf_Service=...`.

### Step 3: Help/Index.cshtml — 5 Stellen

Alle Occurrences von "Sync-Protokoll" durch "Aktivitaets-Protokoll" ersetzen. Auch wo der Block "Sync-Jobs werden ins Sync-Protokoll geschrieben" steht, an "Aktivitaets-Protokoll" anpassen.

Die Service-Filter-Liste im Hilfeseiten-Block (bestätigt vorhanden um Zeile 916 ff. — Card "Sync-Protokoll" → wird umbenannt) auf alle 12 Services erweitern:
> Service-Filter: Lagerplatz, Lagerbestand, BomCache, OseonTracking, EnaioDms, Holiday, CoatingDetection, ProductionOrder, Article, PartRequisitionEmail, WarehouseRequisitionEmail, BdeAutoPause.

### Step 4: Build + Web-Test-Suite

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: 0 Fehler, alle Tests gruen (UI-Aenderungen brechen keine Tests).

### Step 5: Commit

```bash
git add IdealAkeWms/Views/Shared/_Layout.cshtml IdealAkeWms/Views/SyncLog/Index.cshtml IdealAkeWms/Views/Help/Index.cshtml
git commit -m "refactor(ui): rename 'Sync-Protokoll' to 'Aktivitaets-Protokoll'"
```

---

## Task 6: Version-Bump v1.15.1 + Changelog

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`
- Modify: `IDEALAKEWMSService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`

### Step 1: AppVersion-Bump beider Projekte

`IdealAkeWms/AppVersion.cs`:
```csharp
public const string Version = "1.15.1";
public const string Date = "2026-05-27";
```

`IDEALAKEWMSService/AppVersion.cs`: gleiche Werte.

### Step 2: Changelog-Eintrag

Oben in `IdealAkeWms/Views/Help/Changelog.cshtml` einen neuen Block einfuegen (ueber dem v1.15.0-Eintrag):

```html
<h3>v1.15.1 — 27.05.2026</h3>
<ul>
    <li><strong>Aktivitaets-Protokoll erweitert:</strong> Drei weitere Background-Aktivitaeten
        schreiben jetzt ins zentrale Audit-Log: Versand von Bedarfsmeldungen
        (<code>PartRequisitionEmail</code>), Versand von Lagerbestellungen
        (<code>WarehouseRequisitionEmail</code>, sowohl Submits als auch Stornos), und
        automatische BDE-Pausen am Schichtende (<code>BdeAutoPause</code>).</li>
    <li><strong>UI-Umbenennung:</strong> Der Menue-Eintrag "Sync-Protokoll" wird zu
        "Aktivitaets-Protokoll" umbenannt, weil das Log inzwischen mehr als reine Sync-Jobs
        umfasst. URLs/Bookmarks bleiben unveraendert (<code>/SyncLog/Index</code>).</li>
    <li>Pro versendete E-Mail bzw. pausierten Booking ein detaillierter Eintrag mit
        Referenz (FA-Nummer, Requisition-Id, Booking-Id), damit im Log direkt
        nachvollziehbar bleibt welche Entitaet betroffen war.</li>
</ul>
```

Verwende dieselbe Markup-Struktur wie der bestehende v1.15.0-Block (vermutlich card / div mit Bootstrap-Layout — anpassen).

### Step 3: Build

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Debug --nologo
```

Erwartet: 0 Fehler.

### Step 4: Commit

```bash
git add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/Changelog.cshtml
git commit -m "feat(version): bump to v1.15.1 (activity-log rollout)"
```

---

## Task 7: Doku-Updates — TESTSZENARIEN + PROJECT_STATUS + CLAUDE.md

**Files:**
- Modify: `docs/TESTSZENARIEN.md`
- Modify: `PROJECT_STATUS.md`
- Modify: `CLAUDE.md`

### Step 1: TESTSZENARIEN — Kapitel 28

Am ENDE von `docs/TESTSZENARIEN.md` anhaengen:

```markdown
## Kapitel 28: Activity-Log fuer Non-Sync-Services (v1.15.1)

### Szenario 28.1: PartRequisitionEmailService schreibt Lifecycle + Referenzen
**Vorbedingung:** `Sync:PartRequisitionEmailEnabled = true`, ungesendete Bedarfsmeldungen vorhanden.
**Schritt:**
1. Worker-Tick abwarten oder Service manuell starten.
2. Im Aktivitaets-Protokoll Service-Filter = "PartRequisitionEmail" setzen.
**Erwartet:**
- "Run gestartet" (Info)
- Pro versendete Mail-Gruppe ein Info-Eintrag mit Reference = FA-Nummer
- Bei "keine aktiven Empfaenger": Warning mit Reference = FA-Nummer
- "Run erfolgreich beendet — versendet=…, ohne_empfaenger=…, fehler=…"

### Szenario 28.2: WarehouseRequisitionEmailService — Submit + Storno differenziert
**Vorbedingung:** Je ein offener Submit und Storno in der DB.
**Schritt:** Worker-Tick, Filter = "WarehouseRequisitionEmail".
**Erwartet:**
- Pro Submit-Mail: Info-Eintrag mit Reference = "submit/{id}"
- Pro Storno-Mail: Info-Eintrag mit Reference = "storno/{id}"
- End-Summary: "submit_versendet=…, storno_versendet=…, fehler=…"

### Szenario 28.3: BdeAutoPauseService loggt auto-pausierte Bookings
**Vorbedingung:** `BdeSchichtkalenderAktiv = true`, mindestens eine Running-Buchung nach Schichtende.
**Schritt:** Worker-Tick (60-Min-Intervall) abwarten, Filter = "BdeAutoPause".
**Erwartet:**
- "Run gestartet"
- Pro auto-pausierten Booking: Info-Eintrag mit Reference = "booking/{id}"
- End-Summary: "geprueft=…, pausiert=…, fehler=…"
```

### Step 2: PROJECT_STATUS.md

**2.1 — Im Block "Aktueller Fortschritt (laufend)" eine neue Sub-Sektion am Ende anfuegen:**

```markdown
### Activity-Log fuer Non-Sync-Services (v1.15.1)

| # | Sub-Task | Status |
|---|---------|--------|
| 1 | Pre-flight grep | ✅ erledigt |
| 2 | SyncLogServices um 3 Konstanten erweitern | ✅ erledigt |
| 3 | PartRequisitionEmailService integriert + Test | ✅ erledigt |
| 4 | WarehouseRequisitionEmailService integriert + Tests | ✅ erledigt |
| 5 | BdeAutoPauseService integriert + Tests | ✅ erledigt |
| 6 | UI-Umbenennung 'Sync-Protokoll' → 'Aktivitaets-Protokoll' | ✅ erledigt |
| 7 | Version-Bump v1.15.1 + Changelog | ✅ erledigt |
| 8 | Doku (TESTSZENARIEN, PROJECT_STATUS, CLAUDE.md) | ⏳ in Arbeit (dieser Task) |
```

**2.2 — In der Hauptfunktionen-Tabelle eine neue Zeile:**
```markdown
| Activity-Log fuer Non-Sync-Services (Mail-Versand + BDE-Auto-Pause) | Fertig (v1.15.1) |
```

**2.3 — Roadmap-Liste:**
```markdown
- v1.15.1 (2026-05-27) — Activity-Log fuer Non-Sync-Services. UI umbenannt zu "Aktivitaets-Protokoll".
```

### Step 3: CLAUDE.md — Bekannte Fallstricke

Neuer Punkt im "Bekannte Fallstricke"-Block (ans Ende):

```markdown
- **UI 'Aktivitaets-Protokoll' vs. Tabelle 'SyncLogs' (seit v1.15.1)**: Das Menue/UI-Label heisst "Aktivitaets-Protokoll", die DB-Tabelle, Klassen und Interfaces behalten den historischen Namen `SyncLog`/`SyncLogger`/`ISyncLogger`/`SyncLogServices`. Bewusste Asymmetrie (eigene Spec begruendet das in [secondbrain/docs/superpowers/specs/2026-05-27-activity-log-non-sync-services-design.md](secondbrain/docs/superpowers/specs/2026-05-27-activity-log-non-sync-services-design.md) §6). URL-Route bleibt `/SyncLog/Index`. Filter-URL-Parameter heisst weiter `?colf_Service=...`.
```

### Step 4: Build + komplette Test-Suite

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Debug --nologo
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-restore -c Debug --nologo
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: alle Builds 0 Fehler, alle Tests gruen.

### Step 5: Commit + Push

```bash
git add docs/TESTSZENARIEN.md PROJECT_STATUS.md CLAUDE.md
git commit -m "docs(activitylog): testszenarien, project-status, claude.md for v1.15.1"
git push origin main
```

### Step 6: PROJECT_STATUS finalisieren

Edit `PROJECT_STATUS.md` — Sub-Task 8 von ⏳ auf ✅ setzen, Commit-Hash ergaenzen.

```bash
git add PROJECT_STATUS.md
git commit -m "docs(project-status): mark activity-log rollout complete"
git push origin main
```

---

## Validierung am Schluss

- [ ] **Final-Check 1:** `git log --oneline a40f978..HEAD` zeigt ~8 atomare Commits.
- [ ] **Final-Check 2:** `dotnet test` ueber beide Test-Projekte → alle gruen.
- [ ] **Final-Check 3:** Manuelles UI-Smoke: Web-App starten, Nav-Menue zeigt "Aktivitaets-Protokoll", Klick fuehrt zur funktionierenden Index-View, Service-Filter-Dropdown listet 12 Eintraege.
- [ ] **Final-Check 4:** `git push origin main`.

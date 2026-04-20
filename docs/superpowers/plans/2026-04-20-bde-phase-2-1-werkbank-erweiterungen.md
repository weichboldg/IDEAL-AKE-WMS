# BDE Phase 2.1 Werkbank-Erweiterungen Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Pro Werkbank ein `BdeAktiv`-Flag (default false) und optional ein werkbank-spezifischer `BdeDefaultArbeitsgang`-Name mit Fallback auf das globale Setting einführen, und alle BDE-Oberflächen (Cockpit, Terminal, Buchungsübersicht-Dropdown, Master-Data-Dropdown) sowie den BdeBookingService auf dieses Flag filtern.

**Architecture:** Zwei zusätzliche Spalten direkt auf `ProductionWorkplace` (keine Aux-Tabelle). Zentrale Filterung über neue Repository-Methode `GetBdeActiveAsync()`. Booking-Validation-Gate in `BdeBookingService` — inaktive Werkbänke können keine neuen Buchungen starten. Default-AG-Resolution: Werkbank-Wert schlägt Global, Leerstring/Whitespace fällt auf Global zurück.

**Tech Stack:** ASP.NET Core 10.0 MVC, EF Core 10.0, SQL Server, xUnit + FluentAssertions + Moq, Bootstrap 5.

**Scope:** Alle Pfade relativ zum Worktree `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1`. Branch: `feature/bde-phase-1`. Kein Versions-Bump in dieser Phase.

## Abweichungen vom Design-Spec (bewusst pragmatisch)

| Spec sagt | Plan macht | Warum |
|-----------|-----------|-------|
| `ColumnDefinitions.cs` um BDE-Spalte ergänzen | Nicht angefasst | `ProductionWorkplaces/Index.cshtml` nutzt kein filterable-table-Pattern, hat keine `ViewConfig`. Die Spalte wird statisch als `<th>` hinzugefügt |
| `_logger.LogWarning` beim Booking-Reject | Nur Fehler-Response | Serilog-Request-Logging captured Status + Body bereits. Logger-Injection würde alle bestehenden `BdeBookingServiceTests`-Setups auf einen Schlag anfassen — nicht verhältnismäßig für Phase 2.1 |
| `ProductionWorkplaceTests.BdeAktiv_DefaultsToFalse` | Weggelassen | Pure Property-Default — redundant mit dem funktionalen Migration-Test (DEFAULT 0 wird bei jedem Fresh-Install-Test implizit verifiziert) |
| `ProductionWorkplacesControllerTests.Edit_PersistsBdeFields` | Als manueller UI-Smoke-Test in Task 8 / Task 10 | Neue Test-Datei ohne bestehende Vorlage; der UI-Smoke deckt den Round-Trip (GET lädt Felder, POST persistiert sie) vollständig ab |

Diese Abweichungen sind keine Scope-Änderungen — alle funktionalen Spec-Anforderungen (Schema, Filter, Validation-Gate, UI, Migration, Dokumentation) sind abgedeckt.

---

## Task 1: Model-Feld ergänzen + EF-Migration + Test-Seed anpassen

**Files:**
- Modify: `IdealAkeWms/Models/ProductionWorkplace.cs`
- Create (via EF-CLI): `IdealAkeWms/Migrations/YYYYMMDDHHMMSS_AddBdeWerkbankSettings.cs` (+ Designer.cs)
- Modify (via EF-CLI): `IdealAkeWms/Migrations/ApplicationDbContextModelSnapshot.cs`
- Modify: `IdealAkeWms.Tests/Helpers/BdeBookingTestSeed.cs`

- [ ] **Step 1: Felder zum Model hinzufügen**

In `IdealAkeWms/Models/ProductionWorkplace.cs`, vor der `ICollection<ProductionWorkplaceUser> ...`-Zeile einfügen:

```csharp
    [Display(Name = "BDE aktiv")]
    public bool BdeAktiv { get; set; } = false;

    [StringLength(200)]
    [Display(Name = "Default-Arbeitsgang (BDE)")]
    public string? BdeDefaultArbeitsgang { get; set; }
```

- [ ] **Step 2: Test-Seed-Helper so anpassen, dass alle Test-Werkbänke BDE-aktiv sind**

Damit die bestehenden Tests nicht durch die spätere Booking-Validation (Task 4) kippen, setzt der Test-Seed standardmäßig `BdeAktiv = true`.

In `IdealAkeWms.Tests/Helpers/BdeBookingTestSeed.cs`, Zeile 29 (die `ProductionWorkplace`-Erzeugung in `SeedAsync`) ändern von:

```csharp
var wp = new ProductionWorkplace { Name = $"Werkbank{suffix}", CreatedAt = now, CreatedBy = actor, CreatedByWindows = actor };
```

zu:

```csharp
var wp = new ProductionWorkplace { Name = $"Werkbank{suffix}", BdeAktiv = true, CreatedAt = now, CreatedBy = actor, CreatedByWindows = actor };
```

In `IdealAkeWms.Tests/Services/BdeDefaultWorkOperationServiceTests.cs`, `SeedFaAndWorkplace`-Methode Zeile 25 genauso:

```csharp
var wp = new ProductionWorkplace { Name = "WB1", BdeAktiv = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
```

- [ ] **Step 3: EF-Migration generieren**

Im Worktree-Root:

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms" && dotnet ef migrations add AddBdeWerkbankSettings
```

Erwartet: neue Migration-Datei in `IdealAkeWms/Migrations/`, Name endet auf `_AddBdeWerkbankSettings.cs`. `ApplicationDbContextModelSnapshot.cs` wird automatisch aktualisiert.

- [ ] **Step 4: Build zur Verifikation**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -5
```

Erwartet: `0 Fehler`.

- [ ] **Step 5: Alle bestehenden Tests ausführen**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -10
```

Erwartet: alle grün, `Fehler: 0`. Falls ein Test außerhalb der BdeBookingTestSeed-Nutzung eigenständig `ProductionWorkplace`-Instanzen anlegt, ist dieser Test vorerst harmlos (BdeAktiv = false per Default, aber ohne Validation-Gate noch irrelevant).

- [ ] **Step 6: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Models/ProductionWorkplace.cs IdealAkeWms/Migrations/ IdealAkeWms.Tests/Helpers/BdeBookingTestSeed.cs IdealAkeWms.Tests/Services/BdeDefaultWorkOperationServiceTests.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): add BdeAktiv + BdeDefaultArbeitsgang fields to ProductionWorkplace

EF migration AddBdeWerkbankSettings. Test seed helper defaults
workplaces to BdeAktiv=true so existing tests remain green once
validation gate is added in a later task."
```

---

## Task 2: Repository-Methode `GetBdeActiveAsync` (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IProductionWorkplaceRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/ProductionWorkplaceRepository.cs`
- Create: `IdealAkeWms.Tests/Repositories/ProductionWorkplaceRepositoryTests.cs`

- [ ] **Step 1: Test-Datei anlegen mit failing Test**

`IdealAkeWms.Tests/Repositories/ProductionWorkplaceRepositoryTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class ProductionWorkplaceRepositoryTests
{
    private static ProductionWorkplace NewWp(string name, bool bdeAktiv)
        => new()
        {
            Name = name,
            BdeAktiv = bdeAktiv,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };

    [Fact]
    public async Task GetBdeActiveAsync_ReturnsOnlyActiveWorkplaces()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.ProductionWorkplaces.AddRange(
            NewWp("Active A", bdeAktiv: true),
            NewWp("Inactive B", bdeAktiv: false),
            NewWp("Active C", bdeAktiv: true));
        await ctx.SaveChangesAsync();

        var repo = new ProductionWorkplaceRepository(ctx);

        var result = await repo.GetBdeActiveAsync();

        result.Should().HaveCount(2);
        result.Select(w => w.Name).Should().BeEquivalentTo(new[] { "Active A", "Active C" });
    }

    [Fact]
    public async Task GetBdeActiveAsync_OrdersByName()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.ProductionWorkplaces.AddRange(
            NewWp("Zeta", bdeAktiv: true),
            NewWp("Alpha", bdeAktiv: true));
        await ctx.SaveChangesAsync();

        var repo = new ProductionWorkplaceRepository(ctx);

        var result = await repo.GetBdeActiveAsync();

        result.Select(w => w.Name).Should().ContainInOrder("Alpha", "Zeta");
    }
}
```

- [ ] **Step 2: Test ausführen, sollte fehlschlagen (Methode fehlt)**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~ProductionWorkplaceRepositoryTests" 2>&1 | tail -10
```

Erwartet: Build-Fehler "GetBdeActiveAsync not found".

- [ ] **Step 3: Interface-Methode hinzufügen**

In `IdealAkeWms/Data/Repositories/IProductionWorkplaceRepository.cs`, nach `GetAllWithUsersOrderedAsync()`:

```csharp
    Task<List<ProductionWorkplace>> GetBdeActiveAsync();
```

- [ ] **Step 4: Implementation hinzufügen**

In `IdealAkeWms/Data/Repositories/ProductionWorkplaceRepository.cs`, nach der Methode `GetAllWithUsersOrderedAsync`:

```csharp
    public async Task<List<ProductionWorkplace>> GetBdeActiveAsync()
    {
        return await _dbSet
            .Where(w => w.BdeAktiv)
            .OrderBy(w => w.Name)
            .ToListAsync();
    }
```

- [ ] **Step 5: Tests laufen lassen — sollten passen**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~ProductionWorkplaceRepositoryTests" 2>&1 | tail -10
```

Erwartet: beide Tests grün.

- [ ] **Step 6: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Data/Repositories/ IdealAkeWms.Tests/Repositories/ProductionWorkplaceRepositoryTests.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): add GetBdeActiveAsync to ProductionWorkplaceRepository

Dedicated repository method (not an IQueryable extension) so the
filter intent is visible in code review and callers cannot
accidentally use GetAllAsync() where filtering is required."
```

---

## Task 3: Default-AG Resolution mit Werkbank-Override (TDD)

**Files:**
- Modify: `IdealAkeWms/Services/BdeDefaultWorkOperationService.cs`
- Modify: `IdealAkeWms.Tests/Services/BdeDefaultWorkOperationServiceTests.cs`

- [ ] **Step 1: Neue Tests in die bestehende Test-Datei einfügen**

Am Ende von `IdealAkeWms.Tests/Services/BdeDefaultWorkOperationServiceTests.cs` (vor der letzten `}`) einfügen:

```csharp
    [Fact]
    public async Task FindOrCreate_WerkbankOverride_Wins()
    {
        var (ctx, svc) = Setup(defaultAgName: "PRODUKTION");
        var wp = new ProductionWorkplace
        {
            Name = "WB1", BdeAktiv = true, BdeDefaultArbeitsgang = "BOHREN",
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        var po = new ProductionOrder
        {
            OrderNumber = "FA-200", Quantity = 5,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.ProductionOrders.Add(po);
        ctx.SaveChanges();

        var woId = await svc.FindOrCreateDefaultAsync(po.Id, wp.Id);

        var wo = ctx.WorkOperations.First(w => w.Id == woId);
        wo.Name.Should().Be("BOHREN");
    }

    [Fact]
    public async Task FindOrCreate_WhitespaceWerkbank_FallsBackToGlobal()
    {
        var (ctx, svc) = Setup(defaultAgName: "PRODUKTION");
        var wp = new ProductionWorkplace
        {
            Name = "WB1", BdeAktiv = true, BdeDefaultArbeitsgang = "   ",
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        var po = new ProductionOrder
        {
            OrderNumber = "FA-201", Quantity = 5,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.ProductionOrders.Add(po);
        ctx.SaveChanges();

        var woId = await svc.FindOrCreateDefaultAsync(po.Id, wp.Id);

        var wo = ctx.WorkOperations.First(w => w.Id == woId);
        wo.Name.Should().Be("PRODUKTION");
    }

    [Fact]
    public async Task FindOrCreate_WerkbankTrimmed()
    {
        var (ctx, svc) = Setup(defaultAgName: "PRODUKTION");
        var wp = new ProductionWorkplace
        {
            Name = "WB1", BdeAktiv = true, BdeDefaultArbeitsgang = "  BOHREN  ",
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        var po = new ProductionOrder
        {
            OrderNumber = "FA-202", Quantity = 5,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.ProductionOrders.Add(po);
        ctx.SaveChanges();

        var woId = await svc.FindOrCreateDefaultAsync(po.Id, wp.Id);

        var wo = ctx.WorkOperations.First(w => w.Id == woId);
        wo.Name.Should().Be("BOHREN");
    }

    [Fact]
    public async Task FindOrCreate_BothEmpty_Throws()
    {
        var (ctx, svc) = Setup(defaultAgName: "");
        var wp = new ProductionWorkplace
        {
            Name = "WB1", BdeAktiv = true, BdeDefaultArbeitsgang = null,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        var po = new ProductionOrder
        {
            OrderNumber = "FA-203", Quantity = 5,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.ProductionOrders.Add(po);
        ctx.SaveChanges();

        var act = async () => await svc.FindOrCreateDefaultAsync(po.Id, wp.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*nicht konfiguriert*");
    }
```

- [ ] **Step 2: Tests ausführen, 4 neue sollten fehlschlagen**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~BdeDefaultWorkOperationServiceTests" 2>&1 | tail -15
```

Erwartet: 4 Tests rot. `WerkbankOverride_Wins` liefert "PRODUKTION" statt "BOHREN"; `BothEmpty_Throws` wirft Exception mit alter Message "BdeDefaultArbeitsgang ist nicht konfiguriert" oder "ist leer" — muss neue Resolution-Logik bekommen.

- [ ] **Step 3: Resolution-Logik im Service implementieren**

`IdealAkeWms/Services/BdeDefaultWorkOperationService.cs` komplett ersetzen durch:

```csharp
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Services;

public class BdeDefaultWorkOperationService : IBdeDefaultWorkOperationService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IAppSettingRepository _settings;

    public BdeDefaultWorkOperationService(ApplicationDbContext ctx, IAppSettingRepository settings)
    {
        _ctx = ctx;
        _settings = settings;
    }

    public async Task<int> FindOrCreateDefaultAsync(int productionOrderId, int workplaceId)
    {
        var defaultName = await ResolveDefaultArbeitsgangAsync(workplaceId);

        // Find existing
        var existing = await _ctx.WorkOperations
            .FirstOrDefaultAsync(wo => wo.ProductionOrderId == productionOrderId && wo.Name == defaultName);

        if (existing != null)
            return existing.Id;

        // Create new
        var wo = new WorkOperation
        {
            ProductionOrderId = productionOrderId,
            OperationNumber = "01",
            Name = defaultName,
            ProductionWorkplaceId = workplaceId,
            Sequence = 1,
            IsReportable = true,
            CreatedAt = DateTime.Now,
            CreatedBy = "BDE-AutoCreate",
            CreatedByWindows = "BDE-AutoCreate"
        };

        _ctx.WorkOperations.Add(wo);
        await _ctx.SaveChangesAsync();
        return wo.Id;
    }

    private async Task<string> ResolveDefaultArbeitsgangAsync(int workplaceId)
    {
        var workplace = await _ctx.ProductionWorkplaces
            .FirstOrDefaultAsync(w => w.Id == workplaceId);

        if (!string.IsNullOrWhiteSpace(workplace?.BdeDefaultArbeitsgang))
            return workplace.BdeDefaultArbeitsgang.Trim();

        var global = await _settings.GetValueAsync("BdeDefaultArbeitsgang");
        if (!string.IsNullOrWhiteSpace(global))
            return global.Trim();

        throw new InvalidOperationException(
            "Default-Arbeitsgang ist weder auf der Werkbank noch global konfiguriert.");
    }
}
```

- [ ] **Step 4: Tests ausführen — alle grün**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~BdeDefaultWorkOperationServiceTests" 2>&1 | tail -10
```

Erwartet: alle Tests (alte 3 + neue 4 = 7) grün.

- [ ] **Step 5: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Services/BdeDefaultWorkOperationService.cs IdealAkeWms.Tests/Services/BdeDefaultWorkOperationServiceTests.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): resolve default work operation from workplace override

Per-workplace BdeDefaultArbeitsgang overrides the global setting.
Whitespace-only values fall back to global. Both empty throws with
clear message."
```

---

## Task 4: BdeBookingService Validation-Gate für inaktive Werkbänke (TDD)

**Files:**
- Modify: `IdealAkeWms/Services/BdeBookingService.cs`
- Modify: `IdealAkeWms.Tests/Services/BdeBookingServiceTests.cs` (add new tests)

- [ ] **Step 1: Zwei neue Tests ans Ende der Test-Datei anfügen**

Öffne `IdealAkeWms.Tests/Services/BdeBookingServiceTests.cs` und suche das Ende der Klasse (letzte schließende `}`). Direkt davor einfügen:

```csharp
    [Fact]
    public async Task StartProduction_RejectsInactiveWorkplace()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        // Werkbank nachträglich deaktivieren
        var wp = await ctx.ProductionWorkplaces.FindAsync(ids.WorkplaceId);
        wp!.BdeAktiv = false;
        await ctx.SaveChangesAsync();

        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(u => u.GetDisplayName()).Returns("tester");
        userSvc.Setup(u => u.GetWindowsUserName()).Returns("tester");
        var svc = new BdeBookingService(ctx, userSvc.Object);

        var result = await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        result.ResultType.Should().Be(BdeBookingResultType.Invalid);
        result.ErrorMessage.Should().Contain("nicht für BDE aktiviert");
        ctx.BdeBookings.Count().Should().Be(0);
    }

    [Fact]
    public async Task StartActivity_RejectsInactiveWorkplace()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var wp = await ctx.ProductionWorkplaces.FindAsync(ids.WorkplaceId);
        wp!.BdeAktiv = false;
        await ctx.SaveChangesAsync();

        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(u => u.GetDisplayName()).Returns("tester");
        userSvc.Setup(u => u.GetWindowsUserName()).Returns("tester");
        var svc = new BdeBookingService(ctx, userSvc.Object);

        var result = await svc.StartActivityAsync(ids.OperatorId, ids.ActivityId, ids.WorkplaceId, ids.TerminalId);

        result.ResultType.Should().Be(BdeBookingResultType.Invalid);
        result.ErrorMessage.Should().Contain("nicht für BDE aktiviert");
        ctx.BdeBookings.Count().Should().Be(0);
    }

    [Fact]
    public async Task Resume_RejectsInactiveWorkplace()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        // Pausiere Buchung anlegen
        var paused = BdeBookingTestSeed.NewBooking(
            ids, BdeBookingType.Production, BdeBookingStatus.Paused,
            startedAt: DateTime.Now.AddHours(-1),
            endedAt: DateTime.Now.AddMinutes(-30));
        ctx.BdeBookings.Add(paused);
        await ctx.SaveChangesAsync();

        // Werkbank deaktivieren
        var wp = await ctx.ProductionWorkplaces.FindAsync(ids.WorkplaceId);
        wp!.BdeAktiv = false;
        await ctx.SaveChangesAsync();

        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(u => u.GetDisplayName()).Returns("tester");
        userSvc.Setup(u => u.GetWindowsUserName()).Returns("tester");
        var svc = new BdeBookingService(ctx, userSvc.Object);

        var result = await svc.ResumeAsync(paused.Id, ids.OperatorId, BdeBookingType.Production, ids.WorkplaceId, ids.TerminalId);

        result.ResultType.Should().Be(BdeBookingResultType.Invalid);
        result.ErrorMessage.Should().Contain("nicht für BDE aktiviert");
    }
```

**Hinweis:** `BdeBookingResultType` und `ErrorMessage` müssen den Feldern in `BdeBookingResult` entsprechen. Falls die Typen anders heißen, die Test-Assertions mit `grep` gegen die real definierte API abgleichen — die Tests sollen gegen die echte Result-API assertieren, nicht gegen erfundene Namen:

```bash
grep -n "public " "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Services/BdeBookingResult.cs"
```

Bei abweichenden Namen (z.B. `Status` statt `ResultType`, oder `Message` statt `ErrorMessage`) die Tests entsprechend anpassen, bevor du weiterläufst.

- [ ] **Step 2: Tests ausführen — 3 neue sollten fehlschlagen**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~BdeBookingServiceTests" 2>&1 | tail -15
```

Erwartet: 3 neue Tests rot (Buchung wird angelegt obwohl Werkbank inaktiv). Alle alten Tests grün.

- [ ] **Step 3: Validation-Gate in Service einbauen**

In `IdealAkeWms/Services/BdeBookingService.cs`:

1) Am Anfang der Klasse einen privaten Helper einfügen (nach dem Konstruktor):

```csharp
    private async Task<BdeBookingResult?> EnsureWorkplaceIsBdeActiveAsync(int workplaceId)
    {
        var workplace = await _ctx.ProductionWorkplaces.FindAsync(workplaceId);
        if (workplace == null || !workplace.BdeAktiv)
            return BdeBookingResult.Invalid("Werkbank ist nicht für BDE aktiviert.");
        return null;
    }
```

2) In `StartPlannedAsync` (nach dem Öffnen der `InTransactionAsync`-Lambda, vor der Collision-Prüfung) einfügen:

```csharp
            var gateError = await EnsureWorkplaceIsBdeActiveAsync(workplaceId);
            if (gateError != null) return gateError;
```

Konkret so aussehen lassen:

```csharp
    private Task<BdeBookingResult> StartPlannedAsync(int operatorId, int workOperationId, int workplaceId, int terminalId, BdeBookingType type)
    {
        return InTransactionAsync(async () =>
        {
            var gateError = await EnsureWorkplaceIsBdeActiveAsync(workplaceId);
            if (gateError != null) return gateError;

            // 1) Kollision: laeuft WorkOperation bereits bei anderem Operator?
            // ... bestehender Code unverändert ...
        });
    }
```

3) In `StartActivityAsync` direkt nach Start der `InTransactionAsync`-Lambda:

```csharp
    public Task<BdeBookingResult> StartActivityAsync(int operatorId, int activityId, int workplaceId, int terminalId)
    {
        return InTransactionAsync(async () =>
        {
            var gateError = await EnsureWorkplaceIsBdeActiveAsync(workplaceId);
            if (gateError != null) return gateError;

            var existingOwn = await _ctx.BdeBookings
            // ... bestehender Code unverändert ...
        });
    }
```

4) In `ResumeAsync` direkt nach Start der `InTransactionAsync`-Lambda:

```csharp
    public Task<BdeBookingResult> ResumeAsync(int pausedBookingId, int operatorId, BdeBookingType resumeAs, int workplaceId, int terminalId)
    {
        return InTransactionAsync(async () =>
        {
            var gateError = await EnsureWorkplaceIsBdeActiveAsync(workplaceId);
            if (gateError != null) return gateError;

            var parent = await _ctx.BdeBookings.FindAsync(pausedBookingId);
            // ... bestehender Code unverändert ...
        });
    }
```

- [ ] **Step 4: Tests ausführen — alle grün**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo 2>&1 | tail -10
```

Erwartet: `Fehler: 0` gesamt.

- [ ] **Step 5: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Services/BdeBookingService.cs IdealAkeWms.Tests/Services/BdeBookingServiceTests.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): reject bookings on BDE-inactive workplaces

Start/Resume paths return BdeBookingResult.Invalid with message
'Werkbank ist nicht für BDE aktiviert.' — guards against stale
terminal assignments and direct API calls."
```

---

## Task 5: BdeApiController — Cockpit + Available-Operations filtern

**Files:**
- Modify: `IdealAkeWms/Controllers/BdeApiController.cs`
- Modify: `IdealAkeWms.Tests/Controllers/BdeApiControllerTests.cs`

- [ ] **Step 1: Neuen Test in `BdeApiControllerTests.cs` einfügen**

Öffne `IdealAkeWms.Tests/Controllers/BdeApiControllerTests.cs` und lokalisiere den existierenden Test für Cockpit (grep nach `GetActiveCockpit` oder `Cockpit`). Direkt nach dem letzten Cockpit-Test einen neuen Test einfügen:

```csharp
    [Fact]
    public async Task GetActiveCockpit_ExcludesInactiveWorkplaces()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        // Zweite, inaktive Werkbank mit eigener aktiver Buchung
        var wpInactive = new ProductionWorkplace
        {
            Name = "Inaktiv",
            BdeAktiv = false,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wpInactive);
        await ctx.SaveChangesAsync();

        ctx.BdeBookings.Add(new BdeBooking
        {
            BdeOperatorId = ids.OperatorId,
            ProductionWorkplaceId = wpInactive.Id,
            BdeTerminalId = ids.TerminalId,
            BdeActivityId = ids.ActivityId,
            BookingType = BdeBookingType.Activity,
            Status = BdeBookingStatus.Running,
            StartedAt = DateTime.Now.AddMinutes(-10),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);

        var result = await controller.GetActiveCockpit();

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        // Serialisierte Response enthält ein Array/List von Kacheln.
        // Prüfe: keine Kachel referenziert die inaktive Werkbank-Id.
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        json.Should().NotContain($"\"ProductionWorkplaceId\":{wpInactive.Id}");
    }
```

**Hinweis:** Der Test nimmt an, dass `BdeApiControllerTests` einen `CreateController(ctx)`-Helper bereits hat. Wenn nicht, grep die Datei nach dem Setup-Pattern der anderen Tests und baue das Controller-Instanziieren analog nach.

```bash
grep -n "new BdeApiController\|CreateController" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms.Tests/Controllers/BdeApiControllerTests.cs"
```

- [ ] **Step 2: Test ausführen, fehlschlagen erwartet**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~GetActiveCockpit_ExcludesInactiveWorkplaces" 2>&1 | tail -10
```

Erwartet: Test rot (die inaktive Werkbank taucht in der Response auf).

- [ ] **Step 3: Cockpit-Query im Controller filtern**

`IdealAkeWms/Controllers/BdeApiController.cs` öffnen, die `GetActiveCockpit`- bzw. `GetActiveCockpitAsync`-Methode finden (grep):

```bash
grep -n "ActiveCockpit\|GetActiveCockpit" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Controllers/BdeApiController.cs"
```

Die Query, die Werkbänke/Buchungen liest, so anpassen, dass sie `w.BdeAktiv` filtert. Typisches Pattern: Die Query beginnt mit `_ctx.ProductionWorkplaces` oder joint gegen sie. Ergänze `.Where(w => w.BdeAktiv)` direkt nach dem Entry in die ProductionWorkplaces-DbSet.

Beispiel (die tatsächliche Query kann abweichen — die Intent ist: `BdeAktiv = true` wird erzwungen):

```csharp
// VORHER (Beispiel):
var workplaces = await _ctx.ProductionWorkplaces
    .Include(w => w.WorkOperations)
    // ...
    .ToListAsync();

// NACHHER:
var workplaces = await _ctx.ProductionWorkplaces
    .Where(w => w.BdeAktiv)
    .Include(w => w.WorkOperations)
    // ...
    .ToListAsync();
```

Falls der Cockpit-Endpoint aus den Buchungen heraus die Werkbänke ermittelt (`ctx.BdeBookings... Select(b => b.ProductionWorkplace)`), die Filter-Bedingung im `Where`-Clause ergänzen:

```csharp
// Ergänze die BdeAktiv-Prüfung:
.Where(b => b.EndedAt == null && !b.IsCancelled && b.ProductionWorkplace.BdeAktiv)
```

- [ ] **Step 4: Available-Operations-Endpoint absichern**

Im selben Controller die Methode `GetAvailableOperationsAsync` (oder ähnlich, je nach Naming) finden:

```bash
grep -n "AvailableOperations\|available-operations" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Controllers/BdeApiController.cs"
```

Am Anfang der Methode (nach Signatur, vor der eigentlichen Query) einfügen:

```csharp
        var workplace = await _ctx.ProductionWorkplaces.FindAsync(workplaceId);
        if (workplace == null || !workplace.BdeAktiv)
        {
            return Ok(new { productive = Array.Empty<object>(), unplanned = Array.Empty<object>() });
        }
```

Dadurch gibt der Endpoint leere Listen zurück, wenn die Werkbank inaktiv ist — Terminal zeigt dann "keine Aufträge verfügbar".

- [ ] **Step 5: Alle BDE-Controller-Tests laufen lassen**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~BdeApiControllerTests" 2>&1 | tail -10
```

Erwartet: alle grün (inkl. neuer).

- [ ] **Step 6: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Controllers/BdeApiController.cs IdealAkeWms.Tests/Controllers/BdeApiControllerTests.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): filter cockpit + available-operations by BdeAktiv

Cockpit only shows workplaces with BdeAktiv=true. Available-operations
returns empty lists for inactive workplaces so the terminal shows
'keine Aufträge verfügbar' instead of stale data."
```

---

## Task 6: Dropdown-Filter in BdeMasterDataController + BdeBookingsController

**Files:**
- Modify: `IdealAkeWms/Controllers/BdeMasterDataController.cs`
- Modify: `IdealAkeWms/Controllers/BdeBookingsController.cs`

Keine neuen Tests — diese Änderungen sind reine Dropdown-Quellenwechsel und werden im manuellen UI-Test verifiziert.

- [ ] **Step 1: Betroffene Aufrufe finden**

```bash
grep -n "GetAllOrderedAsync\|GetAllWithUsersOrderedAsync" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Controllers/BdeMasterDataController.cs" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Controllers/BdeBookingsController.cs"
```

- [ ] **Step 2: BdeMasterDataController anpassen**

In `IdealAkeWms/Controllers/BdeMasterDataController.cs` die Aufrufe, die das **Terminal-Edit-Dropdown** füttern (typisch in Methoden wie `CreateTerminal`, `EditTerminal`, `EditTerminalGet`), von `_workplaceRepo.GetAllOrderedAsync()` auf `_workplaceRepo.GetBdeActiveAsync()` umstellen.

Andere Werkbank-Lookups im Controller (z.B. Werkbank-Liste in der Übersicht der Master-Daten) bleiben unverändert — Masterdata soll weiterhin alle Werkbänke sehen können. Nur die Dropdown-Quelle für BDE-Terminal-Zuweisung wechselt.

- [ ] **Step 3: BdeBookingsController Werkbank-Filter-Dropdown anpassen**

In `IdealAkeWms/Controllers/BdeBookingsController.cs` die `Index`-Action öffnen. Dort wird in der Regel eine `Workplaces`- oder `WorkplaceOptions`-Liste aufgebaut (für ein Dropdown im Filter-Bereich). Auch hier von `GetAllOrderedAsync()` auf `GetBdeActiveAsync()` umstellen.

**Wichtig:** Die Ergebnis-Liste der Buchungen selbst darf weiterhin Buchungen auf historisch-inaktiven Werkbänken zeigen. Nur die **Dropdown-Quelle** für den Werkbank-Filter wechselt. Das heißt: die Abfrage der Buchungen bleibt wie sie ist, lediglich `vm.WorkplaceOptions = await _workplaceRepo.GetAllOrderedAsync()` wird zu `vm.WorkplaceOptions = await _workplaceRepo.GetBdeActiveAsync()`.

- [ ] **Step 4: Build + alle Tests**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Erwartet: Build OK, alle Tests grün.

- [ ] **Step 5: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Controllers/BdeMasterDataController.cs IdealAkeWms/Controllers/BdeBookingsController.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): filter BDE-related workplace dropdowns to active only

Terminal assignment dropdown and booking overview workplace filter
now show only BdeAktiv workplaces. Historical booking list itself
remains unfiltered to preserve reports."
```

---

## Task 7: ProductionWorkplacesController Create/Edit erweitert + ViewModel

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/ProductionWorkplaceEditViewModel.cs`
- Modify: `IdealAkeWms/Controllers/ProductionWorkplacesController.cs`

- [ ] **Step 1: ViewModel um die zwei Felder erweitern**

In `IdealAkeWms/Models/ViewModels/ProductionWorkplaceEditViewModel.cs`, zwischen `OverridePrePickingDays` und `SelectedUserIds` einfügen:

```csharp
    [Display(Name = "BDE aktiv")]
    public bool BdeAktiv { get; set; }

    [MaxLength(200)]
    [Display(Name = "Default-Arbeitsgang (BDE)")]
    public string? BdeDefaultArbeitsgang { get; set; }
```

- [ ] **Step 2: Controller — Edit GET: Felder ins ViewModel mappen + ViewBag-Hinweis**

In `IdealAkeWms/Controllers/ProductionWorkplacesController.cs`:

a) Konstruktor um `IAppSettingRepository` ergänzen:

```csharp
    private readonly IAppSettingRepository _appSettings;

    public ProductionWorkplacesController(
        IProductionWorkplaceRepository repository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IAppSettingRepository appSettings)
    {
        _repository = repository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _appSettings = appSettings;
    }
```

b) `Edit(int id)` erweitern:

```csharp
    public async Task<IActionResult> Edit(int id)
    {
        var workplace = await _repository.GetByIdWithUsersAsync(id);
        if (workplace == null)
            return NotFound();

        var vm = new ProductionWorkplaceEditViewModel
        {
            Id = workplace.Id,
            Name = workplace.Name,
            Hall = workplace.Hall,
            OverridePrePickingDays = workplace.OverridePrePickingDays,
            BdeAktiv = workplace.BdeAktiv,
            BdeDefaultArbeitsgang = workplace.BdeDefaultArbeitsgang,
            SelectedUserIds = workplace.ProductionWorkplaceUsers.Select(wu => wu.UserId).ToList(),
            AvailableUsers = await _userRepository.GetActiveUsersAsync()
        };

        ViewBag.GlobalDefaultArbeitsgang = await _appSettings.GetValueAsync("BdeDefaultArbeitsgang") ?? "(nicht gesetzt)";
        return View(vm);
    }
```

c) `Edit(int id, ProductionWorkplaceEditViewModel vm)` POST erweitern — nach dem bestehenden `existing.OverridePrePickingDays = vm.OverridePrePickingDays;`:

```csharp
        existing.OverridePrePickingDays = vm.OverridePrePickingDays;
        existing.BdeAktiv = vm.BdeAktiv;
        existing.BdeDefaultArbeitsgang = string.IsNullOrWhiteSpace(vm.BdeDefaultArbeitsgang)
            ? null
            : vm.BdeDefaultArbeitsgang.Trim();
        existing.ModifiedAt = DateTime.Now;
```

Außerdem: im Fehlerfall (`!ModelState.IsValid`) vor dem Return auch den Viewbag-Hinweis setzen:

```csharp
        if (!ModelState.IsValid)
        {
            vm.AvailableUsers = await _userRepository.GetActiveUsersAsync();
            ViewBag.GlobalDefaultArbeitsgang = await _appSettings.GetValueAsync("BdeDefaultArbeitsgang") ?? "(nicht gesetzt)";
            return View(vm);
        }
```

d) `Create()` GET analog mit ViewBag-Hinweis:

```csharp
    public async Task<IActionResult> Create()
    {
        var vm = new ProductionWorkplaceEditViewModel
        {
            AvailableUsers = await _userRepository.GetActiveUsersAsync()
        };
        ViewBag.GlobalDefaultArbeitsgang = await _appSettings.GetValueAsync("BdeDefaultArbeitsgang") ?? "(nicht gesetzt)";
        return View(vm);
    }
```

e) `Create(ProductionWorkplaceEditViewModel vm)` POST — nach `OverridePrePickingDays = vm.OverridePrePickingDays` ergänzen:

```csharp
        var workplace = new ProductionWorkplace
        {
            Name = vm.Name,
            Hall = vm.Hall,
            OverridePrePickingDays = vm.OverridePrePickingDays,
            BdeAktiv = vm.BdeAktiv,
            BdeDefaultArbeitsgang = string.IsNullOrWhiteSpace(vm.BdeDefaultArbeitsgang)
                ? null
                : vm.BdeDefaultArbeitsgang.Trim(),
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };
```

Und im Fehlerfall-Zweig den ViewBag ebenfalls setzen:

```csharp
        if (!ModelState.IsValid)
        {
            vm.AvailableUsers = await _userRepository.GetActiveUsersAsync();
            ViewBag.GlobalDefaultArbeitsgang = await _appSettings.GetValueAsync("BdeDefaultArbeitsgang") ?? "(nicht gesetzt)";
            return View(vm);
        }
```

- [ ] **Step 3: Build**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
```

Erwartet: `0 Fehler`.

- [ ] **Step 4: Alle Tests laufen lassen**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Erwartet: alle grün.

- [ ] **Step 5: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Controllers/ProductionWorkplacesController.cs IdealAkeWms/Models/ViewModels/ProductionWorkplaceEditViewModel.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): persist BdeAktiv + BdeDefaultArbeitsgang from workplace form

ProductionWorkplacesController loads BDE settings into ViewModel on
Edit/Create, persists them on POST. ViewBag exposes the global
default so the edit form can show the current fallback value.
Whitespace-only input is normalized to null."
```

---

## Task 8: Views — Werkbank Edit, Create, Index

**Files:**
- Modify: `IdealAkeWms/Views/ProductionWorkplaces/Edit.cshtml`
- Modify: `IdealAkeWms/Views/ProductionWorkplaces/Create.cshtml`
- Modify: `IdealAkeWms/Views/ProductionWorkplaces/Index.cshtml`

- [ ] **Step 1: Edit.cshtml — BDE-Accordion unterhalb der bestehenden Felder**

Öffne `IdealAkeWms/Views/ProductionWorkplaces/Edit.cshtml`. Suche die schließende Stelle des "Benutzer-Zuordnung"-Formulars oder den letzten Form-Block vor dem Submit-Button. Direkt davor folgenden Block einfügen:

```html
<div class="card mt-3">
    <div class="card-header">BDE-Einstellungen</div>
    <div class="card-body">
        <div class="form-check form-switch mb-3">
            <input asp-for="BdeAktiv" class="form-check-input" role="switch" />
            <label asp-for="BdeAktiv" class="form-check-label"></label>
            <div class="form-text">Werkbank erscheint in BDE-Cockpit, Terminal und Buchungsübersicht.</div>
        </div>
        <div class="mb-3">
            <label asp-for="BdeDefaultArbeitsgang" class="form-label"></label>
            <input asp-for="BdeDefaultArbeitsgang" class="form-control" placeholder="z.B. PRODUKTION (leer = globales Setting verwenden)" />
            <div class="form-text">
                Überschreibt das globale Setting. Aktuell global: <strong>@ViewBag.GlobalDefaultArbeitsgang</strong>
            </div>
            <span asp-validation-for="BdeDefaultArbeitsgang" class="text-danger"></span>
        </div>
    </div>
</div>
```

- [ ] **Step 2: Create.cshtml — analog**

Identischen Block wie in Step 1 in `IdealAkeWms/Views/ProductionWorkplaces/Create.cshtml` an passender Stelle einfügen (analog zu Edit, vor dem Submit-Button).

- [ ] **Step 3: Index.cshtml — neue Spalte "BDE"**

In `IdealAkeWms/Views/ProductionWorkplaces/Index.cshtml`:

a) Im `<thead>`-Block die neue Spalte zwischen "Halle" und "Benutzer" einfügen:

```html
            <tr>
                <th>Bezeichnung</th>
                <th>Halle</th>
                <th>BDE</th>
                <th>Benutzer</th>
                <th>Abw. Vorkommissioniertage</th>
                <th style="width: 100px;"></th>
            </tr>
```

b) In der `foreach`-Schleife zwischen `<td>@wp.Hall</td>` und `<td>Benutzer`-Zelle einfügen:

```html
                    <td>
                        @if (wp.BdeAktiv)
                        {
                            <span class="badge bg-success">Aktiv</span>
                        }
                        else
                        {
                            <span class="badge bg-secondary">Inaktiv</span>
                        }
                    </td>
```

c) Der `colspan` im leeren Zustand:

```html
                    <td colspan="6" class="text-center text-muted py-4">Keine Werkbänke vorhanden.</td>
```

(von 5 auf 6 erhöht)

- [ ] **Step 4: Build und Tests**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Erwartet: Build OK, Tests grün.

- [ ] **Step 5: Manueller Smoke-Test**

Starte lokal:

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms" && dotnet run --no-build --urls="http://localhost:5088"
```

Browser: `http://localhost:5088/ProductionWorkplaces` — prüfe:
1. Neue Spalte "BDE" mit Badge sichtbar
2. Edit einer Werkbank → Accordion "BDE-Einstellungen" sichtbar, Toggle funktioniert, Hinweis-Text zeigt globalen Wert
3. Speichern → Badge wechselt auf der Index-Seite

Server stoppen (Ctrl+C).

- [ ] **Step 6: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Views/ProductionWorkplaces/
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): add BDE settings card + Index badge to workplace views

Edit and Create show a toggle for BdeAktiv plus a text field for
BdeDefaultArbeitsgang, including a hint showing the current global
fallback. Index gets a 'BDE' column with Aktiv/Inaktiv badge."
```

---

## Task 9: SQL-Scripts — `43_AddBdeWerkbankSettings.sql` + `00_FreshInstall.sql`

**Files:**
- Create: `SQL/43_AddBdeWerkbankSettings.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1: Migration-ID und -Name aus EF-Migration-Datei auslesen**

```bash
ls "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Migrations/" | grep -i "AddBdeWerkbankSettings"
```

Erwartet: Eine Datei `<YYYYMMDDHHMMSS>_AddBdeWerkbankSettings.cs`. Die Zahl vorne ist die MigrationId (z.B. `20260420103555`). Speichere sie für Step 3.

- [ ] **Step 2: `SQL/43_AddBdeWerkbankSettings.sql` anlegen**

Inhalt:

```sql
-- Migration: Add BdeAktiv + BdeDefaultArbeitsgang to ProductionWorkplaces
-- Idempotent: safe to re-run.

IF COL_LENGTH('dbo.ProductionWorkplaces', 'BdeAktiv') IS NULL
BEGIN
    ALTER TABLE dbo.ProductionWorkplaces
    ADD BdeAktiv bit NOT NULL CONSTRAINT DF_ProductionWorkplaces_BdeAktiv DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.ProductionWorkplaces', 'BdeDefaultArbeitsgang') IS NULL
BEGIN
    ALTER TABLE dbo.ProductionWorkplaces
    ADD BdeDefaultArbeitsgang nvarchar(200) NULL;
END
GO

-- EFMigrationsHistory-Insert (ersetzt <MIGRATION_ID> durch die tatsächliche Id aus Step 1)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '<MIGRATION_ID>_AddBdeWerkbankSettings')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('<MIGRATION_ID>_AddBdeWerkbankSettings', '10.0.0');
END
GO
```

`<MIGRATION_ID>` durch die aus Step 1 gelesene Zahl ersetzen (2x). ProductVersion-String so wie er in anderen Migrationen der Datei schon vorkommt — bei Unsicherheit grep:

```bash
grep "ProductVersion" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Migrations/ApplicationDbContextModelSnapshot.cs"
```

- [ ] **Step 3: `SQL/00_FreshInstall.sql` erweitern**

Die Datei öffnen und den `CREATE TABLE [dbo].[ProductionWorkplaces]`-Block suchen. Die neuen Spalten dort einfügen, idealerweise nach `OverridePrePickingDays`:

```bash
grep -n "ProductionWorkplaces" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/SQL/00_FreshInstall.sql"
```

In dem Block ergänzen:

```sql
    [BdeAktiv] bit NOT NULL DEFAULT (0),
    [BdeDefaultArbeitsgang] nvarchar(200) NULL,
```

- [ ] **Step 4: SQL-Script-Syntax prüfen durch lokalen Testlauf**

Starte die App gegen die (aktuell v1.8.2-migrierte) Datenbank:

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms" && dotnet run --no-build --urls="http://localhost:5088"
```

Beim Start wird `db.Database.Migrate()` die EF-Migration ausführen — prüfe, dass keine Fehler im Log auftauchen. Server sofort stoppen (Ctrl+C).

- [ ] **Step 5: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add SQL/43_AddBdeWerkbankSettings.sql SQL/00_FreshInstall.sql
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "chore(sql): idempotent script + fresh install for BDE workplace fields

SQL/43_AddBdeWerkbankSettings.sql guards each column with
COL_LENGTH; separate GO batches for __EFMigrationsHistory insert."
```

---

## Task 10: Hilfe-Seite ergänzen + finaler Full-Run

**Files:**
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`

- [ ] **Step 1: Hilfeseite erweitern**

Öffne `IdealAkeWms/Views/Help/Index.cshtml` und suche den bestehenden BDE-Bereich (grep nach "BDE" bzw. "Betriebsdatenerfassung"):

```bash
grep -n "BDE\|Betriebsdatenerfassung" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Views/Help/Index.cshtml"
```

Im BDE-Bereich einen neuen Unterabschnitt "Werkbank für BDE konfigurieren" einfügen:

```html
<h5>Werkbank für BDE konfigurieren</h5>
<ol>
    <li>Menü <strong>Stammdaten → Werkbänke</strong> öffnen.</li>
    <li>Gewünschte Werkbank bearbeiten.</li>
    <li>Im Abschnitt <strong>BDE-Einstellungen</strong> den Schalter <em>BDE aktiv</em> einschalten.</li>
    <li>Optional: Unter <em>Default-Arbeitsgang (BDE)</em> einen werkbank-spezifischen AG-Namen eintragen (z.&nbsp;B. "BOHREN"). Leer lassen, um das globale Setting zu verwenden.</li>
    <li>Speichern.</li>
</ol>
<p class="text-muted">Nur BDE-aktivierte Werkbänke erscheinen im Cockpit, in Terminal-Auswahllisten und in den Dropdown-Filtern der Buchungsübersicht.</p>
```

Im Troubleshooting-Bereich (falls vorhanden, sonst am Ende des BDE-Abschnitts) ergänzen:

```html
<h5>Fehler "Werkbank ist nicht für BDE aktiviert"</h5>
<p>Tritt beim Scan am Terminal auf, wenn die zugewiesene Werkbank nicht als BDE-aktiv markiert ist. Lösung: Stammdaten → Werkbänke → betroffene Werkbank bearbeiten → <em>BDE aktiv</em> einschalten.</p>
```

- [ ] **Step 2: Build + alle Tests final**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -10
```

Erwartet:
- Build: `0 Fehler`
- Tests: alle grün, inkl. der neuen aus Tasks 2, 3, 4, 5

- [ ] **Step 3: Finaler manueller Acceptance-Durchlauf**

App starten:

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms" && dotnet run --no-build --urls="http://localhost:5088"
```

Manuell prüfen (Reihenfolge):
1. `/ProductionWorkplaces` — "BDE"-Spalte vorhanden, alle Werkbänke "Inaktiv" (da Default 0).
2. Eine Werkbank bearbeiten, "BDE aktiv" einschalten, "Default-Arbeitsgang" leer lassen, speichern.
3. Zurück zur Liste — die Werkbank zeigt jetzt "Aktiv".
4. `/BdeCockpit` (falls BdeAktiv global = true) — nur die eine aktive Werkbank erscheint als Kachel (bzw. keine wenn keine laufende Buchung vorhanden).
5. `/BdeBookings` — Werkbank-Filter-Dropdown zeigt nur die aktive Werkbank.
6. `/BdeMasterData/EditTerminal/<id>` — Werkbank-Dropdown zeigt nur die aktive Werkbank.
7. Werkbank bearbeiten → "Default-Arbeitsgang" auf "BOHREN" setzen, speichern.
8. Im NurFA-Modus (BdeNurFaMeldung = true): einen FA an dieser Werkbank scannen → neuer WorkOperation hat Name "BOHREN", nicht "PRODUKTION".
9. Werkbank wieder auf "Inaktiv" setzen → im Terminal erscheint beim Scan "Werkbank ist nicht für BDE aktiviert".

Server stoppen (Ctrl+C).

- [ ] **Step 4: Commit + Final-Log**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Views/Help/Index.cshtml
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "docs(bde): help section for workplace BDE activation + troubleshooting"
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" log --oneline -12
```

Erwartet: die letzten 10 Commits zeigen die Phase-2.1-Umsetzung.

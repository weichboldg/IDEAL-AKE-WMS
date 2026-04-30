# OSEON Reporting — AG-Übersicht Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reporting-Seite `/Reporting/OseonOperations` mit KPI-Cards (Überfällig | Heute geplant | Heute erledigt | Zukunft) und filter-/sortierbarer AG-Liste über OSEON-relevante Arbeitsgänge.

**Architecture:** Single-Action `OseonReportingController` mit `[RequireReportingAccess]`, eine View, Repository-Methode am bestehenden `OseonProductionOrderRepository`. Werktag-/Offset-Logik wird in einen Helper `OseonDueDateCalculator` extrahiert (DRY mit Tracking) — nutzt das bereits existierende `IBusinessDayService.AddBusinessDays`.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10, SQL Server, xUnit + FluentAssertions + Moq + EF InMemory, Bootstrap 5.

**Scope:** Pfade relativ zu `C:/Git/IDEAL-AKE-WMS`, Branch `main`. Versions-Bump: v1.7.0 → v1.7.1 (Spec sagte 1.8.3 — das war ein Übertragsfehler aus dem BDE-Branch; main steht real auf 1.7.0).

**Spec-Anpassungen vs. Spec-Doc:**
- Kein `IHolidayProvider` — `IBusinessDayService.AddBusinessDays(date, offset, holidays)` ist die existierende Abstraktion. Helper wrapped sie nur um die Offset==0-Sonderlogik.
- Berechtigungs-Methode: `ICurrentUserService.CanReportOperationsAsync()` (existiert) — nicht `HasReportingAccessAsync()`.
- Status-Helper: `OseonStatusHelper.GetStatusBadgeClass(int)` und `GetStatusText(int)` (genaue Methoden-Namen).
- Versions-Bump: v1.7.1, Datum 2026-04-30.

---

## Task 1: Helper extrahieren — `OseonDueDateCalculator` + TrackingController-Refactor

**Files:**
- Create: `IdealAkeWms/Services/OseonDueDateCalculator.cs`
- Create: `IdealAkeWms.Tests/Services/OseonDueDateCalculatorTests.cs`
- Modify: `IdealAkeWms/Controllers/TrackingController.cs` (lines 221–231 — siehe Step 5)

- [ ] **Step 1: Test-Datei mit 5 Tests anlegen**

`IdealAkeWms.Tests/Services/OseonDueDateCalculatorTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Services;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class OseonDueDateCalculatorTests
{
    private static IBusinessDayService NewBusinessDayService()
    {
        var m = new Mock<IBusinessDayService>();
        m.Setup(x => x.AddBusinessDays(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<HashSet<DateTime>>()))
            .Returns<DateTime, int, HashSet<DateTime>>((date, days, holidays) =>
            {
                var current = date.Date;
                var sign = Math.Sign(days);
                var remaining = Math.Abs(days);
                while (remaining > 0)
                {
                    current = current.AddDays(sign);
                    if (current.DayOfWeek == DayOfWeek.Saturday) continue;
                    if (current.DayOfWeek == DayOfWeek.Sunday) continue;
                    if (holidays.Contains(current.Date)) continue;
                    remaining--;
                }
                return current;
            });
        return m.Object;
    }

    [Fact]
    public void Calculate_OffsetZero_ReturnsBaseDateOnly()
    {
        var bds = NewBusinessDayService();
        var holidays = new HashSet<DateTime>();
        var result = OseonDueDateCalculator.Calculate(new DateTime(2026, 4, 30, 14, 30, 0), 0, bds, holidays);
        result.Should().Be(new DateTime(2026, 4, 30));
    }

    [Fact]
    public void Calculate_PositiveOffset_DelegatesToBusinessDayService()
    {
        var bds = NewBusinessDayService();
        var holidays = new HashSet<DateTime>();
        var result = OseonDueDateCalculator.Calculate(new DateTime(2026, 4, 30), 3, bds, holidays); // Do
        result.Should().Be(new DateTime(2026, 5, 5)); // Di (überspringt Sa/So)
    }

    [Fact]
    public void Calculate_NegativeOffset_DelegatesToBusinessDayService()
    {
        var bds = NewBusinessDayService();
        var holidays = new HashSet<DateTime>();
        var result = OseonDueDateCalculator.Calculate(new DateTime(2026, 5, 5), -3, bds, holidays); // Di
        result.Should().Be(new DateTime(2026, 4, 30)); // Do
    }

    [Fact]
    public void Calculate_OffsetWithHoliday_HolidayIsSkipped()
    {
        var bds = NewBusinessDayService();
        var holidays = new HashSet<DateTime> { new DateTime(2026, 5, 1) }; // Tag der Arbeit, Fr
        var result = OseonDueDateCalculator.Calculate(new DateTime(2026, 4, 30), 1, bds, holidays); // Do +1 Werktag
        result.Should().Be(new DateTime(2026, 5, 4)); // Mo (überspringt Fr-Feiertag + Sa + So)
    }

    [Fact]
    public void Calculate_OffsetZeroIgnoresHolidays()
    {
        var bds = NewBusinessDayService();
        var holidays = new HashSet<DateTime> { new DateTime(2026, 4, 30) };
        var result = OseonDueDateCalculator.Calculate(new DateTime(2026, 4, 30), 0, bds, holidays);
        result.Should().Be(new DateTime(2026, 4, 30)); // bleibt
    }
}
```

- [ ] **Step 2: Test laufen lassen → Compile-Fehler**

```bash
cd "C:/Git/IDEAL-AKE-WMS" && dotnet test --nologo --filter "FullyQualifiedName~OseonDueDateCalculatorTests" 2>&1 | tail -10
```

Expected: Build error `OseonDueDateCalculator not found`.

- [ ] **Step 3: Helper anlegen**

`IdealAkeWms/Services/OseonDueDateCalculator.cs`:

```csharp
namespace IdealAkeWms.Services;

public static class OseonDueDateCalculator
{
    /// <summary>
    /// Berechnet das relevante AG-Termin-Datum auf Basis von Auftrags-Termin + Offset (Werktage).
    /// Offset 0 → Auftrags-Termin (Date). Sonst Werktage via IBusinessDayService.
    /// </summary>
    public static DateTime Calculate(DateTime baseDate, int offsetDays, IBusinessDayService businessDays, HashSet<DateTime> holidays)
    {
        if (offsetDays == 0) return baseDate.Date;
        return businessDays.AddBusinessDays(baseDate, offsetDays, holidays);
    }
}
```

- [ ] **Step 4: Tests grün**

```bash
cd "C:/Git/IDEAL-AKE-WMS" && dotnet test --nologo --filter "FullyQualifiedName~OseonDueDateCalculatorTests" 2>&1 | tail -10
```

Expected: 5/5 passing.

- [ ] **Step 5: TrackingController.OseonIndex umstellen**

In `IdealAkeWms/Controllers/TrackingController.cs` zwischen Zeilen 220–231 die bestehende Inline-Logik:

```csharp
DateTime? calculatedDueDate = null;
if (o.DueDate.HasValue && hasConfig)
{
    calculatedDueDate = opConfig!.DueDateOffsetDays == 0
        ? o.DueDate.Value.Date
        : _businessDayService.AddBusinessDays(o.DueDate.Value, opConfig.DueDateOffsetDays, holidays);
}
else if (o.DueDate.HasValue)
{
    calculatedDueDate = o.DueDate.Value.Date;
}
```

ersetzen durch:

```csharp
DateTime? calculatedDueDate = null;
if (o.DueDate.HasValue)
{
    calculatedDueDate = hasConfig
        ? OseonDueDateCalculator.Calculate(o.DueDate.Value, opConfig!.DueDateOffsetDays, _businessDayService, holidays)
        : o.DueDate.Value.Date;
}
```

und am Datei-Anfang `using IdealAkeWms.Services;` ergänzen (falls noch nicht vorhanden).

- [ ] **Step 6: Build + Full Test**

```bash
cd "C:/Git/IDEAL-AKE-WMS" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Expected: 0 errors, alle Tests grün (Tracking-Verhalten unverändert).

- [ ] **Step 7: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS" add IdealAkeWms/Services/OseonDueDateCalculator.cs IdealAkeWms.Tests/Services/OseonDueDateCalculatorTests.cs IdealAkeWms/Controllers/TrackingController.cs
git -C "C:/Git/IDEAL-AKE-WMS" commit -m "refactor(oseon): extract OseonDueDateCalculator helper

Tracking-Inline-Logik (DueDate + Offset Werktage, Offset==0 Sonderfall)
in static helper extrahiert. Vorbereitung fuer Reporting-Wiederverwendung.
Verhalten unveraendert."
```

---

## Task 2: AppSettingKey + Seed (Web + SQL)

**Files:**
- Modify: `IdealAkeWms/Models/AppSettingKeys.cs`
- Modify: `IdealAkeWms/Program.cs`
- Create: `SQL/49_AddOseonReportingHorizonSetting.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1: AppSettingKeys-Konstante**

In `IdealAkeWms/Models/AppSettingKeys.cs` neue Konstante (bei den Reporting-/OSEON-Keys):

```csharp
    public const string OseonReportingHorizonDays = "OseonReportingHorizonDays";
```

- [ ] **Step 2: Program.cs Seed**

In `IdealAkeWms/Program.cs` im Tracking-/OSEON-Settings-Tuple-Array (suche nach `OseonRueckmeldungAktiv` oder `TeileverfolgungAktiv`):

```csharp
("OseonReportingHorizonDays", "10", "Reporting: Tage in die Zukunft (Default-Horizont)"),
```

als zusätzlichen Tuple-Eintrag.

- [ ] **Step 3: SQL/49 anlegen**

`SQL/49_AddOseonReportingHorizonSetting.sql`:

```sql
-- Phase: OSEON Reporting v1.7.1
-- Idempotent: AppSetting OseonReportingHorizonDays seeden.

IF NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = 'OseonReportingHorizonDays')
BEGIN
    INSERT INTO dbo.AppSettings ([Key], [Value], [Description])
    VALUES ('OseonReportingHorizonDays', '10', 'Reporting: Tage in die Zukunft (Default-Horizont)');
END
GO
```

- [ ] **Step 4: FreshInstall ergänzen**

In `SQL/00_FreshInstall.sql` im AppSettings-INSERT-Block (suche nach `OseonRueckmeldungAktiv`) eine zusätzliche Zeile hinzufügen:

```sql
    ('OseonReportingHorizonDays', '10', 'Reporting: Tage in die Zukunft (Default-Horizont)'),
```

(Trailing-Komma bzw. letzte Zeile entsprechend setzen — letzte Zeile endet ohne Komma + Semikolon nach allen.)

- [ ] **Step 5: Build + Test**

```bash
cd "C:/Git/IDEAL-AKE-WMS" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Expected: 0 errors, alle Tests grün.

- [ ] **Step 6: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS" add IdealAkeWms/Models/AppSettingKeys.cs IdealAkeWms/Program.cs SQL/49_AddOseonReportingHorizonSetting.sql SQL/00_FreshInstall.sql
git -C "C:/Git/IDEAL-AKE-WMS" commit -m "feat(reporting): seed OseonReportingHorizonDays AppSetting (default 10)"
```

---

## Task 3: ViewModels

**Files:**
- Create: `IdealAkeWms/Models/ViewModels/OseonReportingSlice.cs`
- Create: `IdealAkeWms/Models/ViewModels/OseonReportingFilter.cs`
- Create: `IdealAkeWms/Models/ViewModels/OseonReportingKpiViewModel.cs`
- Create: `IdealAkeWms/Models/ViewModels/OseonReportingRowViewModel.cs`
- Create: `IdealAkeWms/Models/ViewModels/OseonReportingDayGroup.cs`
- Create: `IdealAkeWms/Models/ViewModels/OseonReportingViewModel.cs`

- [ ] **Step 1: Slice-Enum**

`IdealAkeWms/Models/ViewModels/OseonReportingSlice.cs`:

```csharp
namespace IdealAkeWms.Models.ViewModels;

public enum OseonReportingSlice : byte
{
    All = 0,
    Overdue = 1,
    Today = 2,
    Future = 3
}
```

- [ ] **Step 2: Filter-VM**

`IdealAkeWms/Models/ViewModels/OseonReportingFilter.cs`:

```csharp
namespace IdealAkeWms.Models.ViewModels;

public class OseonReportingFilter
{
    public int? WorkplaceId { get; set; }
    public List<string> OperationNames { get; set; } = new();
    public string? CustomerOrderNumber { get; set; }
    public string? FaNumber { get; set; }
    public int? HorizonDaysOverride { get; set; }
    public OseonReportingSlice Slice { get; set; } = OseonReportingSlice.Today;
}
```

- [ ] **Step 3: KPI-VM**

`IdealAkeWms/Models/ViewModels/OseonReportingKpiViewModel.cs`:

```csharp
namespace IdealAkeWms.Models.ViewModels;

public record OseonReportingKpiViewModel(
    int OverdueCount,
    int TodayPlannedCount,
    int TodayDoneCount,
    int FutureCount);
```

- [ ] **Step 4: Row-VM**

`IdealAkeWms/Models/ViewModels/OseonReportingRowViewModel.cs`:

```csharp
namespace IdealAkeWms.Models.ViewModels;

public record OseonReportingRowViewModel(
    string CustomerOrderNumber,
    string FaNumber,
    string PositionNumber,
    string OperationName,
    string? WorkplaceName,
    DateTime CalculatedDueDate,
    int OseonStatus,
    string StatusText,
    string StatusBadgeClass,
    OseonReportingSlice Slice,
    bool IsDoneToday);
```

- [ ] **Step 5: DayGroup-VM**

`IdealAkeWms/Models/ViewModels/OseonReportingDayGroup.cs`:

```csharp
namespace IdealAkeWms.Models.ViewModels;

public record OseonReportingDayGroup(DateTime Date, int Count, List<OseonReportingRowViewModel> Rows);
```

- [ ] **Step 6: Hülle-VM**

`IdealAkeWms/Models/ViewModels/OseonReportingViewModel.cs`:

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class OseonReportingViewModel
{
    public OseonReportingKpiViewModel Kpis { get; set; } = new(0, 0, 0, 0);
    public List<OseonReportingRowViewModel> Rows { get; set; } = new();
    public List<OseonReportingDayGroup> FutureDayGroups { get; set; } = new();
    public int OperationsWithoutConfigCount { get; set; }
    public DateTime? DataAsOf { get; set; }
    public OseonReportingFilter Filter { get; set; } = new();
    public List<string> AvailableOperationNames { get; set; } = new();
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public int HorizonDaysEffective { get; set; } = 10;
}
```

- [ ] **Step 7: Build**

```bash
cd "C:/Git/IDEAL-AKE-WMS" && dotnet build --nologo 2>&1 | tail -3
```

Expected: 0 errors.

- [ ] **Step 8: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS" add IdealAkeWms/Models/ViewModels/OseonReporting*.cs
git -C "C:/Git/IDEAL-AKE-WMS" commit -m "feat(reporting): add OSEON reporting view models

KPI/Row/DayGroup records + Filter + Slice enum + container VM."
```

---

## Task 4: Repository — `GetRelevantOperationsForReportingAsync` (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IOseonProductionOrderRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs`
- Create: `IdealAkeWms/Data/Repositories/OseonReportingQueryResult.cs`
- Create: `IdealAkeWms.Tests/Repositories/OseonProductionOrderRepositoryReportingTests.cs`

- [ ] **Step 1: Query-Result-Datentyp**

`IdealAkeWms/Data/Repositories/OseonReportingQueryResult.cs`:

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public record OseonReportingQueryRow(
    OseonWorkOperation WorkOperation,
    OseonProductionOrder Order,
    OseonOperationConfig? Config);

public record OseonReportingQueryResult(
    List<OseonReportingQueryRow> Rows,
    int OperationsWithoutConfigCount,
    DateTime? DataAsOf);
```

- [ ] **Step 2: Interface erweitern**

In `IdealAkeWms/Data/Repositories/IOseonProductionOrderRepository.cs` neue Methode hinzufügen:

```csharp
    Task<OseonReportingQueryResult> GetRelevantOperationsForReportingAsync(
        int? workplaceId,
        IReadOnlyCollection<string>? operationNames,
        string? customerOrderNumber,
        string? faNumberPrefix,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken ct = default);
```

- [ ] **Step 3: Tests anlegen**

`IdealAkeWms.Tests/Repositories/OseonProductionOrderRepositoryReportingTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class OseonProductionOrderRepositoryReportingTests
{
    private static OseonProductionOrder NewOrder(long oseonId, int? workplaceId, DateTime? dueDate, int status,
        string custOrder = "K-100", string faNumber = "FA-100")
    {
        return new OseonProductionOrder
        {
            OseonId = oseonId,
            OseonOrderNumber = faNumber,
            CustomerOrderNumber = custOrder,
            OseonStatus = status,
            ProductionWorkplaceId = workplaceId,
            DueDate = dueDate,
            LastChangedInOseon = DateTime.Now,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
    }

    private static OseonWorkOperation NewWorkOp(string position, string name, int status,
        DateTime? lastReport = null)
    {
        return new OseonWorkOperation
        {
            PositionNumber = position,
            Name = name,
            OseonStatus = status,
            LastStatusReportInOseon = lastReport,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
    }

    private static OseonOperationConfig NewConfig(string name, int offset, bool relevant = true)
    {
        return new OseonOperationConfig
        {
            OperationName = name,
            DueDateOffsetDays = offset,
            IsOseonRelevant = relevant,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
    }

    [Fact]
    public async Task ExcludesOrdersWithCancelledStatus()
    {
        var ctx = TestDbContextFactory.Create();
        var order = NewOrder(1, null, DateTime.Today, 95); // 95 = Storniert
        order.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 60) };
        ctx.OseonProductionOrders.Add(order);
        ctx.OseonOperationConfigs.Add(NewConfig("B", 0));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, null, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task ExcludesCancelledWorkOperations()
    {
        var ctx = TestDbContextFactory.Create();
        var order = NewOrder(1, null, DateTime.Today, 60);
        order.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 95) }; // 95 = Storniert
        ctx.OseonProductionOrders.Add(order);
        ctx.OseonOperationConfigs.Add(NewConfig("B", 0));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, null, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task ExcludesOrdersWithoutDueDate()
    {
        var ctx = TestDbContextFactory.Create();
        var order = NewOrder(1, null, null, 60); // DueDate null
        order.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 60) };
        ctx.OseonProductionOrders.Add(order);
        ctx.OseonOperationConfigs.Add(NewConfig("B", 0));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, null, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task ExcludesAgsWithoutConfig_AndCounts()
    {
        var ctx = TestDbContextFactory.Create();
        var order = NewOrder(1, null, DateTime.Today, 60);
        order.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "UNKNOWN_AG", 60) };
        ctx.OseonProductionOrders.Add(order);
        // KEIN Config-Eintrag fuer UNKNOWN_AG
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, null, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.Rows.Should().BeEmpty();
        result.OperationsWithoutConfigCount.Should().Be(1);
    }

    [Fact]
    public async Task ExcludesNonRelevantConfigs()
    {
        var ctx = TestDbContextFactory.Create();
        var order = NewOrder(1, null, DateTime.Today, 60);
        order.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 60) };
        ctx.OseonProductionOrders.Add(order);
        ctx.OseonOperationConfigs.Add(NewConfig("B", 0, relevant: false));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, null, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task FiltersByWorkplaceId()
    {
        var ctx = TestDbContextFactory.Create();
        var wp1 = new ProductionWorkplace { Name = "WP1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var wp2 = new ProductionWorkplace { Name = "WP2", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.AddRange(wp1, wp2);
        await ctx.SaveChangesAsync();

        var orderA = NewOrder(1, wp1.Id, DateTime.Today, 60);
        orderA.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 60) };
        var orderB = NewOrder(2, wp2.Id, DateTime.Today, 60);
        orderB.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 60) };
        ctx.OseonProductionOrders.AddRange(orderA, orderB);
        ctx.OseonOperationConfigs.Add(NewConfig("B", 0));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(wp1.Id, null, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.Rows.Should().HaveCount(1);
        result.Rows[0].Order.OseonId.Should().Be(1);
    }

    [Fact]
    public async Task FiltersByOperationNames()
    {
        var ctx = TestDbContextFactory.Create();
        var order = NewOrder(1, null, DateTime.Today, 60);
        order.WorkOperations = new List<OseonWorkOperation>
        {
            NewWorkOp("10", "B", 60),
            NewWorkOp("20", "ST", 60)
        };
        ctx.OseonProductionOrders.Add(order);
        ctx.OseonOperationConfigs.AddRange(NewConfig("B", 0), NewConfig("ST", 0));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, new[] { "B" }, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.Rows.Should().HaveCount(1);
        result.Rows[0].WorkOperation.Name.Should().Be("B");
    }

    [Fact]
    public async Task FiltersByCustomerOrderNumber_PrefixMatch()
    {
        var ctx = TestDbContextFactory.Create();
        var orderA = NewOrder(1, null, DateTime.Today, 60, custOrder: "K-1234");
        orderA.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 60) };
        var orderB = NewOrder(2, null, DateTime.Today, 60, custOrder: "K-9999");
        orderB.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 60) };
        ctx.OseonProductionOrders.AddRange(orderA, orderB);
        ctx.OseonOperationConfigs.Add(NewConfig("B", 0));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, null, "K-12", null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.Rows.Should().HaveCount(1);
        result.Rows[0].Order.CustomerOrderNumber.Should().Be("K-1234");
    }

    [Fact]
    public async Task DataAsOf_IsMaxLastChangedInOseon()
    {
        var ctx = TestDbContextFactory.Create();
        var order = NewOrder(1, null, DateTime.Today, 60);
        order.LastChangedInOseon = new DateTime(2026, 4, 30, 14, 32, 0);
        order.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 60) };
        ctx.OseonProductionOrders.Add(order);
        ctx.OseonOperationConfigs.Add(NewConfig("B", 0));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, null, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.DataAsOf.Should().Be(new DateTime(2026, 4, 30, 14, 32, 0));
    }

    [Fact]
    public async Task DataAsOf_IsNullWhenEmpty()
    {
        var ctx = TestDbContextFactory.Create();
        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, null, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.DataAsOf.Should().BeNull();
    }
}
```

- [ ] **Step 4: Tests laufen → fail**

```bash
cd "C:/Git/IDEAL-AKE-WMS" && dotnet test --nologo --filter "FullyQualifiedName~OseonProductionOrderRepositoryReportingTests" 2>&1 | tail -10
```

Expected: Build error oder NotImplementedException.

- [ ] **Step 5: Implementation in `OseonProductionOrderRepository`**

In `IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs` ergänzen:

```csharp
public async Task<OseonReportingQueryResult> GetRelevantOperationsForReportingAsync(
    int? workplaceId,
    IReadOnlyCollection<string>? operationNames,
    string? customerOrderNumber,
    string? faNumberPrefix,
    DateTime fromDate,
    DateTime toDate,
    CancellationToken ct = default)
{
    // Aktive Aufträge: 20=Gültig, 30=Freigegeben, 60=In Arbeit, 90=Fertig
    var activeOrderStatuses = new[] { 20, 30, 60, 90 };

    var ordersQuery = _context.OseonProductionOrders
        .AsNoTracking()
        .Include(o => o.WorkOperations)
        .Where(o => activeOrderStatuses.Contains(o.OseonStatus))
        .Where(o => o.DueDate != null);

    if (workplaceId.HasValue)
        ordersQuery = ordersQuery.Where(o => o.ProductionWorkplaceId == workplaceId.Value);

    if (!string.IsNullOrWhiteSpace(customerOrderNumber))
        ordersQuery = ordersQuery.Where(o => o.CustomerOrderNumber != null
            && o.CustomerOrderNumber.StartsWith(customerOrderNumber));

    if (!string.IsNullOrWhiteSpace(faNumberPrefix))
        ordersQuery = ordersQuery.Where(o => o.OseonOrderNumber.StartsWith(faNumberPrefix));

    var orders = await ordersQuery.ToListAsync(ct);

    var configs = await _context.OseonOperationConfigs.AsNoTracking().ToListAsync(ct);
    var configByName = configs.ToDictionary(c => c.OperationName, StringComparer.OrdinalIgnoreCase);

    var rows = new List<OseonReportingQueryRow>();
    var noConfigCount = 0;
    var opNameSet = operationNames is { Count: > 0 }
        ? new HashSet<string>(operationNames, StringComparer.OrdinalIgnoreCase)
        : null;

    foreach (var order in orders)
    {
        foreach (var wo in order.WorkOperations.Where(w => w.OseonStatus != 95))
        {
            if (opNameSet != null && !opNameSet.Contains(wo.Name)) continue;

            if (!configByName.TryGetValue(wo.Name, out var cfg))
            {
                noConfigCount++;
                continue;
            }

            if (!cfg.IsOseonRelevant) continue;

            rows.Add(new OseonReportingQueryRow(wo, order, cfg));
        }
    }

    var dataAsOf = orders.Count == 0 ? (DateTime?)null
                                     : orders.Max(o => o.LastChangedInOseon);

    return new OseonReportingQueryResult(rows, noConfigCount, dataAsOf);
}
```

Am Kopf der Datei `using` ergänzen (falls noch nicht da):

```csharp
using IdealAkeWms.Data.Repositories;
using Microsoft.EntityFrameworkCore;
```

- [ ] **Step 6: Tests grün**

```bash
cd "C:/Git/IDEAL-AKE-WMS" && dotnet test --nologo --filter "FullyQualifiedName~OseonProductionOrderRepositoryReportingTests" 2>&1 | tail -10
cd "C:/Git/IDEAL-AKE-WMS" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Expected: 10/10 grün, full suite grün.

- [ ] **Step 7: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS" add IdealAkeWms/Data/Repositories/IOseonProductionOrderRepository.cs IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs IdealAkeWms/Data/Repositories/OseonReportingQueryResult.cs IdealAkeWms.Tests/Repositories/OseonProductionOrderRepositoryReportingTests.cs
git -C "C:/Git/IDEAL-AKE-WMS" commit -m "feat(reporting): repo method GetRelevantOperationsForReportingAsync

Filtert Aufträge nach aktiven Status (20/30/60/90), DueDate NOT NULL,
optional workplace/operationNames/custOrder/faNumber.
Zählt AGs ohne Config separat (Banner-Counter im UI).
DataAsOf = Max(LastChangedInOseon)."
```

---

## Task 5: Controller — `OseonReportingController` (TDD)

**Files:**
- Create: `IdealAkeWms/Controllers/OseonReportingController.cs`
- Create: `IdealAkeWms.Tests/Controllers/OseonReportingControllerTests.cs`
- Modify: `IdealAkeWms/Program.cs` (DI optional, falls noch nicht alle Repos registriert sind — meist via Auto-Scan; nur wenn Build fehlschlägt)

- [ ] **Step 1: Tests anlegen**

`IdealAkeWms.Tests/Controllers/OseonReportingControllerTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class OseonReportingControllerTests
{
    private static OseonReportingController CreateController(ApplicationDbContext ctx,
        Mock<IAppSettingRepository>? settings = null)
    {
        settings ??= new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync("OseonReportingHorizonDays")).ReturnsAsync("10");

        var orderRepo = new OseonProductionOrderRepository(ctx);
        var configRepo = new OseonOperationConfigRepository(ctx);
        var holidayRepo = new HolidayRepository(ctx);

        var businessDays = new Mock<IBusinessDayService>();
        businessDays.Setup(x => x.AddBusinessDays(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<HashSet<DateTime>>()))
            .Returns<DateTime, int, HashSet<DateTime>>((d, n, _) =>
            {
                var current = d.Date;
                var sign = Math.Sign(n);
                var rem = Math.Abs(n);
                while (rem > 0)
                {
                    current = current.AddDays(sign);
                    if (current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday) continue;
                    rem--;
                }
                return current;
            });

        var workplaces = new ProductionWorkplaceRepository(ctx);
        return new OseonReportingController(orderRepo, configRepo, holidayRepo, workplaces, settings.Object, businessDays.Object);
    }

    private static OseonProductionOrder NewOrder(long oseonId, DateTime dueDate, int status = 60,
        string custOrder = "K-100", string faNumber = "FA-100", int? workplaceId = null)
        => new()
        {
            OseonId = oseonId, OseonOrderNumber = faNumber, CustomerOrderNumber = custOrder,
            OseonStatus = status, ProductionWorkplaceId = workplaceId, DueDate = dueDate,
            LastChangedInOseon = DateTime.Now,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };

    private static OseonWorkOperation NewWo(string position, string name, int status, DateTime? lastReport = null)
        => new()
        {
            PositionNumber = position, Name = name, OseonStatus = status,
            LastStatusReportInOseon = lastReport,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };

    private static OseonOperationConfig NewCfg(string name, int offset = 0)
        => new()
        {
            OperationName = name, DueDateOffsetDays = offset, IsOseonRelevant = true,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };

    [Fact]
    public async Task OperationsOverview_DefaultSliceIsToday()
    {
        var ctx = TestDbContextFactory.Create();
        var controller = CreateController(ctx);

        var result = await controller.OperationsOverview(null, null, null, null, null, null) as ViewResult;

        var vm = result!.Model as OseonReportingViewModel;
        vm!.Filter.Slice.Should().Be(OseonReportingSlice.Today);
    }

    [Fact]
    public async Task OperationsOverview_KpiCountsAreCorrect()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.OseonOperationConfigs.Add(NewCfg("B", 0));

        var today = DateTime.Today;
        var orderOverdue = NewOrder(1, today.AddDays(-2));
        orderOverdue.WorkOperations = new List<OseonWorkOperation> { NewWo("10", "B", 60) };

        var orderTodayPlanned = NewOrder(2, today);
        orderTodayPlanned.WorkOperations = new List<OseonWorkOperation> { NewWo("10", "B", 60) };

        var orderTodayDone = NewOrder(3, today);
        orderTodayDone.WorkOperations = new List<OseonWorkOperation> { NewWo("10", "B", 90, today) };

        var orderFuture = NewOrder(4, today.AddDays(3));
        orderFuture.WorkOperations = new List<OseonWorkOperation> { NewWo("10", "B", 60) };

        ctx.OseonProductionOrders.AddRange(orderOverdue, orderTodayPlanned, orderTodayDone, orderFuture);
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);
        var result = await controller.OperationsOverview(null, null, null, null, null, OseonReportingSlice.All) as ViewResult;
        var vm = result!.Model as OseonReportingViewModel;

        vm!.Kpis.OverdueCount.Should().Be(1);
        vm.Kpis.TodayPlannedCount.Should().Be(2); // both today, regardless of done
        vm.Kpis.TodayDoneCount.Should().Be(1);    // only Status=90 with LastStatusReport.Date == today
        vm.Kpis.FutureCount.Should().Be(1);
    }

    [Fact]
    public async Task OperationsOverview_TodayDoneRequiresLastStatusReportToday()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.OseonOperationConfigs.Add(NewCfg("B", 0));

        var order = NewOrder(1, DateTime.Today);
        order.WorkOperations = new List<OseonWorkOperation> {
            NewWo("10", "B", 90, DateTime.Today.AddDays(-3)) // fertig, aber älter
        };
        ctx.OseonProductionOrders.Add(order);
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);
        var result = await controller.OperationsOverview(null, null, null, null, null, OseonReportingSlice.All) as ViewResult;
        var vm = result!.Model as OseonReportingViewModel;

        vm!.Kpis.TodayDoneCount.Should().Be(0);
    }

    [Fact]
    public async Task OperationsOverview_OperationsWithoutConfig_BannerCounterSet()
    {
        var ctx = TestDbContextFactory.Create();
        var order = NewOrder(1, DateTime.Today);
        order.WorkOperations = new List<OseonWorkOperation> { NewWo("10", "UNKNOWN", 60) };
        ctx.OseonProductionOrders.Add(order);
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);
        var result = await controller.OperationsOverview(null, null, null, null, null, null) as ViewResult;
        var vm = result!.Model as OseonReportingViewModel;

        vm!.OperationsWithoutConfigCount.Should().Be(1);
    }

    [Fact]
    public async Task OperationsOverview_HorizonOverrideIsClampedToValidRange()
    {
        var ctx = TestDbContextFactory.Create();
        var controller = CreateController(ctx);

        var result1 = await controller.OperationsOverview(null, null, null, null, -5, null) as ViewResult;
        var vm1 = result1!.Model as OseonReportingViewModel;
        vm1!.HorizonDaysEffective.Should().Be(1); // clamped to min

        var result2 = await controller.OperationsOverview(null, null, null, null, 999, null) as ViewResult;
        var vm2 = result2!.Model as OseonReportingViewModel;
        vm2!.HorizonDaysEffective.Should().Be(60); // clamped to max
    }
}
```

- [ ] **Step 2: Tests laufen → fail**

```bash
cd "C:/Git/IDEAL-AKE-WMS" && dotnet test --nologo --filter "FullyQualifiedName~OseonReportingControllerTests" 2>&1 | tail -10
```

Expected: Build error `OseonReportingController not found`.

- [ ] **Step 3: Controller anlegen**

`IdealAkeWms/Controllers/OseonReportingController.cs`:

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Helpers;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireReportingAccess]
public class OseonReportingController : Controller
{
    private readonly IOseonProductionOrderRepository _orders;
    private readonly IOseonOperationConfigRepository _configs;
    private readonly IHolidayRepository _holidays;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly IAppSettingRepository _settings;
    private readonly IBusinessDayService _businessDays;

    public OseonReportingController(
        IOseonProductionOrderRepository orders,
        IOseonOperationConfigRepository configs,
        IHolidayRepository holidays,
        IProductionWorkplaceRepository workplaces,
        IAppSettingRepository settings,
        IBusinessDayService businessDays)
    {
        _orders = orders;
        _configs = configs;
        _holidays = holidays;
        _workplaces = workplaces;
        _settings = settings;
        _businessDays = businessDays;
    }

    public async Task<IActionResult> OperationsOverview(
        int? workplaceId,
        string? operationNames,         // CSV
        string? customerOrderNumber,
        string? faNumber,
        int? horizonDays,
        OseonReportingSlice? slice)
    {
        var slice_ = slice ?? OseonReportingSlice.Today;

        // Default-Horizont aus AppSettings
        var defaultHorizonText = await _settings.GetValueAsync(AppSettingKeys.OseonReportingHorizonDays);
        var defaultHorizon = int.TryParse(defaultHorizonText, out var parsed) ? parsed : 10;
        var horizonEffective = Math.Clamp(horizonDays ?? defaultHorizon, 1, 60);

        var today = DateTime.Today;
        var fromDate = today.AddDays(-90);   // Überfällige bis 90 Tage zurück
        var toDate = today.AddDays(horizonEffective);

        var opNamesList = string.IsNullOrWhiteSpace(operationNames)
            ? null
            : operationNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var queryResult = await _orders.GetRelevantOperationsForReportingAsync(
            workplaceId, opNamesList, customerOrderNumber, faNumber, fromDate, toDate);

        // Holidays für Werktag-Berechnung im benötigten Range laden
        var allHolidays = await _holidays.GetAllAsync();
        var holidaySet = new HashSet<DateTime>(allHolidays.Select(h => h.Date.Date));

        // Rows berechnen
        var rows = new List<OseonReportingRowViewModel>();
        foreach (var qr in queryResult.Rows)
        {
            var calcDue = OseonDueDateCalculator.Calculate(
                qr.Order.DueDate!.Value, qr.Config!.DueDateOffsetDays, _businessDays, holidaySet);

            // Slice-Bucket
            OseonReportingSlice rowSlice;
            if (calcDue < today && qr.WorkOperation.OseonStatus != 90)
                rowSlice = OseonReportingSlice.Overdue;
            else if (calcDue == today)
                rowSlice = OseonReportingSlice.Today;
            else if (calcDue > today && calcDue <= today.AddDays(horizonEffective))
                rowSlice = OseonReportingSlice.Future;
            else
                continue; // außerhalb des Reporting-Fensters

            var isDoneToday = qr.WorkOperation.OseonStatus == 90
                              && qr.WorkOperation.LastStatusReportInOseon?.Date == today;

            rows.Add(new OseonReportingRowViewModel(
                qr.Order.CustomerOrderNumber ?? "",
                qr.Order.OseonOrderNumber,
                qr.WorkOperation.PositionNumber,
                qr.WorkOperation.Name,
                qr.Order.WorkplaceName,
                calcDue,
                qr.WorkOperation.OseonStatus,
                OseonStatusHelper.GetStatusText(qr.WorkOperation.OseonStatus),
                OseonStatusHelper.GetStatusBadgeClass(qr.WorkOperation.OseonStatus),
                rowSlice,
                isDoneToday));
        }

        // KPIs
        var kpis = new OseonReportingKpiViewModel(
            OverdueCount: rows.Count(r => r.Slice == OseonReportingSlice.Overdue),
            TodayPlannedCount: rows.Count(r => r.Slice == OseonReportingSlice.Today),
            TodayDoneCount: rows.Count(r => r.IsDoneToday),
            FutureCount: rows.Count(r => r.Slice == OseonReportingSlice.Future));

        // Slice-Filter auf Rows
        var filteredRows = slice_ switch
        {
            OseonReportingSlice.Overdue => rows.Where(r => r.Slice == OseonReportingSlice.Overdue).ToList(),
            OseonReportingSlice.Today => rows.Where(r => r.Slice == OseonReportingSlice.Today).ToList(),
            OseonReportingSlice.Future => rows.Where(r => r.Slice == OseonReportingSlice.Future).ToList(),
            _ => rows
        };

        // Sortierung
        filteredRows = filteredRows
            .OrderBy(r => r.CalculatedDueDate)
            .ThenBy(r => r.OseonStatus)
            .ThenBy(r => r.PositionNumber)
            .ToList();

        // Future-Day-Groups (nur wenn Slice == Future): pro Tag bis Tag 14, sonst Wochen-Bucket
        var futureGroups = new List<OseonReportingDayGroup>();
        if (slice_ == OseonReportingSlice.Future)
        {
            futureGroups = filteredRows
                .GroupBy(r => r.CalculatedDueDate)
                .OrderBy(g => g.Key)
                .Select(g => new OseonReportingDayGroup(g.Key, g.Count(), g.ToList()))
                .ToList();
        }

        // Verfügbare Config-Namen für Filter-Dropdown
        var allConfigs = await _configs.GetAllAsync();
        var availableOpNames = allConfigs
            .Where(c => c.IsOseonRelevant)
            .Select(c => c.OperationName)
            .OrderBy(n => n)
            .ToList();

        var availableWorkplaces = await _workplaces.GetAllAsync();

        var vm = new OseonReportingViewModel
        {
            Kpis = kpis,
            Rows = filteredRows,
            FutureDayGroups = futureGroups,
            OperationsWithoutConfigCount = queryResult.OperationsWithoutConfigCount,
            DataAsOf = queryResult.DataAsOf,
            Filter = new OseonReportingFilter
            {
                WorkplaceId = workplaceId,
                OperationNames = opNamesList ?? new(),
                CustomerOrderNumber = customerOrderNumber,
                FaNumber = faNumber,
                HorizonDaysOverride = horizonDays,
                Slice = slice_
            },
            AvailableOperationNames = availableOpNames,
            AvailableWorkplaces = availableWorkplaces.ToList(),
            HorizonDaysEffective = horizonEffective
        };

        return View(vm);
    }
}
```

**Wichtig:** Falls die genauen Repository-Interfaces oder Methoden-Namen abweichen (z.B. `IProductionWorkplaceRepository.GetAllAsync()` heißt anders), kurz vorab prüfen via `grep -n "interface IProductionWorkplaceRepository" IdealAkeWms/Data/Repositories/` und `IHolidayRepository.GetAllAsync` ebenfalls. Bei Abweichung die hier verwendeten Calls 1:1 anpassen.

- [ ] **Step 4: Tests grün**

```bash
cd "C:/Git/IDEAL-AKE-WMS" && dotnet test --nologo --filter "FullyQualifiedName~OseonReportingControllerTests" 2>&1 | tail -10
cd "C:/Git/IDEAL-AKE-WMS" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Expected: 5/5 grün, full suite grün.

- [ ] **Step 5: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS" add IdealAkeWms/Controllers/OseonReportingController.cs IdealAkeWms.Tests/Controllers/OseonReportingControllerTests.cs
git -C "C:/Git/IDEAL-AKE-WMS" commit -m "feat(reporting): OseonReportingController.OperationsOverview

Single-Action Controller mit [RequireReportingAccess]:
- KPIs (Überfällig/Heute geplant/Heute erledigt/Zukunft)
- Slice-Filter via QueryString
- Horizont-Override clamped 1..60
- Banner-Counter fuer AGs ohne Config
- Future-Tab gruppiert pro Tag"
```

---

## Task 6: View — `OperationsOverview.cshtml`

**Files:**
- Create: `IdealAkeWms/Views/OseonReporting/OperationsOverview.cshtml`

- [ ] **Step 1: View anlegen**

`IdealAkeWms/Views/OseonReporting/OperationsOverview.cshtml`:

```html
@model IdealAkeWms.Models.ViewModels.OseonReportingViewModel
@{
    ViewData["Title"] = "OSEON AG-Übersicht";
}

<div class="d-flex justify-content-between align-items-center flex-wrap gap-2 page-header">
    <div>
        <h2 class="mb-0">OSEON AG-Übersicht</h2>
        <small class="text-muted">Geplante vs. erledigte Arbeitsgänge</small>
    </div>
    <div>
        @if (Model.DataAsOf.HasValue)
        {
            <span class="badge bg-secondary">Daten Stand: @Model.DataAsOf.Value.ToString("dd.MM.yyyy HH:mm")</span>
        }
        else
        {
            <span class="badge bg-warning text-dark">noch nie synchronisiert</span>
        }
    </div>
</div>

@if (Model.OperationsWithoutConfigCount > 0)
{
    <div class="alert alert-warning mt-2 mb-2">
        <strong>@Model.OperationsWithoutConfigCount AG(s) ohne Config-Eintrag</strong> &mdash;
        bitte unter <a asp-controller="OseonOperationConfigs" asp-action="Index">Stammdaten &rarr; OSEON-Arbeitsgänge</a> pflegen.
    </div>
}

<form method="get" asp-action="OperationsOverview" class="card card-body mb-3">
    <div class="row g-2 align-items-end">
        <div class="col-md-3">
            <label class="form-label">Werkbank</label>
            <select name="workplaceId" class="form-select form-select-sm">
                <option value="">— alle —</option>
                @foreach (var wp in Model.AvailableWorkplaces)
                {
                    <option value="@wp.Id" selected="@(Model.Filter.WorkplaceId == wp.Id)">@wp.Name</option>
                }
            </select>
        </div>
        <div class="col-md-2">
            <label class="form-label">AG (CSV)</label>
            <input name="operationNames" class="form-control form-control-sm"
                   placeholder="z.B. B,ST"
                   value="@string.Join(",", Model.Filter.OperationNames)" />
        </div>
        <div class="col-md-2">
            <label class="form-label">Kundenauftrag</label>
            <input name="customerOrderNumber" class="form-control form-control-sm"
                   value="@Model.Filter.CustomerOrderNumber" />
        </div>
        <div class="col-md-2">
            <label class="form-label">FA-Nummer</label>
            <input name="faNumber" class="form-control form-control-sm"
                   value="@Model.Filter.FaNumber" />
        </div>
        <div class="col-md-1">
            <label class="form-label">Horizont</label>
            <input name="horizonDays" type="number" min="1" max="60" class="form-control form-control-sm"
                   value="@Model.Filter.HorizonDaysOverride" placeholder="@Model.HorizonDaysEffective" />
        </div>
        <div class="col-md-2 d-flex gap-1">
            <button type="submit" class="btn btn-sm btn-primary flex-grow-1">Anwenden</button>
            <a asp-action="OperationsOverview" class="btn btn-sm btn-outline-secondary">Reset</a>
        </div>
    </div>
</form>

<div class="row g-2 mb-3">
    <div class="col-md-3">
        <div class="card border-danger"><div class="card-body p-2">
            <div class="text-muted small">Überfällig</div>
            <div class="display-6 text-danger">@Model.Kpis.OverdueCount</div>
        </div></div>
    </div>
    <div class="col-md-3">
        <div class="card"><div class="card-body p-2">
            <div class="text-muted small">Heute geplant</div>
            <div class="display-6">@Model.Kpis.TodayPlannedCount</div>
        </div></div>
    </div>
    <div class="col-md-3">
        <div class="card border-success"><div class="card-body p-2">
            <div class="text-muted small">Heute erledigt</div>
            <div class="display-6 text-success">@Model.Kpis.TodayDoneCount</div>
        </div></div>
    </div>
    <div class="col-md-3">
        <div class="card border-primary"><div class="card-body p-2">
            <div class="text-muted small">Zukunft (@Model.HorizonDaysEffective Tage)</div>
            <div class="display-6 text-primary">@Model.Kpis.FutureCount</div>
        </div></div>
    </div>
</div>

@{
    string sliceLink(IdealAkeWms.Models.ViewModels.OseonReportingSlice s)
    {
        var url = $"?slice={s}";
        if (Model.Filter.WorkplaceId.HasValue) url += $"&workplaceId={Model.Filter.WorkplaceId}";
        if (Model.Filter.OperationNames.Count > 0) url += $"&operationNames={string.Join(",", Model.Filter.OperationNames)}";
        if (!string.IsNullOrEmpty(Model.Filter.CustomerOrderNumber)) url += $"&customerOrderNumber={Model.Filter.CustomerOrderNumber}";
        if (!string.IsNullOrEmpty(Model.Filter.FaNumber)) url += $"&faNumber={Model.Filter.FaNumber}";
        if (Model.Filter.HorizonDaysOverride.HasValue) url += $"&horizonDays={Model.Filter.HorizonDaysOverride}";
        return url;
    }
    string activeIf(IdealAkeWms.Models.ViewModels.OseonReportingSlice s) =>
        Model.Filter.Slice == s ? "active" : "";
}

<ul class="nav nav-pills mb-3">
    <li class="nav-item"><a class="nav-link @activeIf(IdealAkeWms.Models.ViewModels.OseonReportingSlice.Today)" href="@sliceLink(IdealAkeWms.Models.ViewModels.OseonReportingSlice.Today)">Heute</a></li>
    <li class="nav-item"><a class="nav-link @activeIf(IdealAkeWms.Models.ViewModels.OseonReportingSlice.Overdue)" href="@sliceLink(IdealAkeWms.Models.ViewModels.OseonReportingSlice.Overdue)">Überfällig</a></li>
    <li class="nav-item"><a class="nav-link @activeIf(IdealAkeWms.Models.ViewModels.OseonReportingSlice.Future)" href="@sliceLink(IdealAkeWms.Models.ViewModels.OseonReportingSlice.Future)">Zukunft</a></li>
    <li class="nav-item"><a class="nav-link @activeIf(IdealAkeWms.Models.ViewModels.OseonReportingSlice.All)" href="@sliceLink(IdealAkeWms.Models.ViewModels.OseonReportingSlice.All)">Alle</a></li>
</ul>

@if (Model.Rows.Count == 0)
{
    <div class="alert alert-info">Keine OSEON-relevanten Arbeitsgänge im gewählten Zeitraum.</div>
}
else if (Model.Filter.Slice == IdealAkeWms.Models.ViewModels.OseonReportingSlice.Future && Model.FutureDayGroups.Count > 0)
{
    foreach (var grp in Model.FutureDayGroups)
    {
        <h6 class="mt-3">@grp.Date.ToString("dddd, dd.MM.yyyy") &mdash; @grp.Count AG(s)</h6>
        @await Html.PartialAsync("_OseonReportingTable", grp.Rows)
    }
}
else
{
    @await Html.PartialAsync("_OseonReportingTable", Model.Rows)
}
```

- [ ] **Step 2: Tabellen-Partial**

`IdealAkeWms/Views/OseonReporting/_OseonReportingTable.cshtml`:

```html
@model List<IdealAkeWms.Models.ViewModels.OseonReportingRowViewModel>

<div class="table-responsive">
    <table class="table table-sm filterable-table">
        <thead>
            <tr>
                <th data-col-key="custOrder">Kunde</th>
                <th data-col-key="faNumber">Auftrag</th>
                <th data-col-key="position">Pos</th>
                <th data-col-key="operationName">AG</th>
                <th data-col-key="workplace">Werkbank</th>
                <th data-col-key="dueDate">Soll-Termin (calc.)</th>
                <th data-col-key="status">Status</th>
                <th data-col-key="slice">Bereich</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var r in Model)
            {
                <tr>
                    <td>@r.CustomerOrderNumber</td>
                    <td><a asp-controller="Tracking" asp-action="OseonIndex" asp-route-search="@r.FaNumber" target="_blank">@r.FaNumber</a></td>
                    <td>@r.PositionNumber</td>
                    <td>@r.OperationName</td>
                    <td>@r.WorkplaceName</td>
                    <td>@r.CalculatedDueDate.ToString("dd.MM.yyyy")</td>
                    <td><span class="badge @r.StatusBadgeClass">@r.StatusText</span></td>
                    <td>
                        @switch (r.Slice)
                        {
                            case IdealAkeWms.Models.ViewModels.OseonReportingSlice.Overdue:
                                <span class="badge bg-danger">Überfällig</span>
                                break;
                            case IdealAkeWms.Models.ViewModels.OseonReportingSlice.Today:
                                <span class="badge bg-secondary">Heute</span>
                                break;
                            case IdealAkeWms.Models.ViewModels.OseonReportingSlice.Future:
                                <span class="badge bg-primary">Zukunft</span>
                                break;
                        }
                        @if (r.IsDoneToday)
                        {
                            <span class="badge bg-success ms-1">heute fertig</span>
                        }
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>
```

- [ ] **Step 3: Build**

```bash
cd "C:/Git/IDEAL-AKE-WMS" && dotnet build --nologo 2>&1 | tail -3
```

Expected: 0 errors.

- [ ] **Step 4: Manual smoke test**

App lokal starten und prüfen, ob `/Reporting/OseonOperations` als eingeloggter Reporting-User aufrufbar ist (404 reicht erst — Layout-Menü-Eintrag kommt in Task 7).

- [ ] **Step 5: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS" add IdealAkeWms/Views/OseonReporting/
git -C "C:/Git/IDEAL-AKE-WMS" commit -m "feat(reporting): View OperationsOverview + table partial

KPI-Cards + 4 Tabs (Heute/Überfällig/Zukunft/Alle) + Filter-Bar
+ Banner für ungepflegte Configs + Daten-Stand-Badge.
Future-Tab nutzt Tagesgruppierung."
```

---

## Task 7: Menü-Eintrag in `_Layout.cshtml`

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Reporting-Dropdown ergänzen**

In `IdealAkeWms/Views/Shared/_Layout.cshtml` einen neuen Top-Level-Menüpunkt "Reporting" einfügen. Suche zuerst eine geeignete Stelle (z.B. nach dem Tracking-Dropdown):

```bash
grep -n "Teileverfolgung\|asp-controller=\"Tracking\"" "C:/Git/IDEAL-AKE-WMS/IdealAkeWms/Views/Shared/_Layout.cshtml"
```

Direkt nach dem schließenden `</li>` des Tracking-Dropdowns einfügen:

```html
@if (await CurrentUserService.CanReportOperationsAsync())
{
    <li class="nav-item dropdown">
        <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown" aria-expanded="false">
            Reporting
        </a>
        <ul class="dropdown-menu">
            <li><a class="dropdown-item" asp-controller="OseonReporting" asp-action="OperationsOverview">OSEON AG-Übersicht</a></li>
        </ul>
    </li>
}
```

- [ ] **Step 2: Build + Test**

```bash
cd "C:/Git/IDEAL-AKE-WMS" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Expected: 0 errors, full suite grün.

- [ ] **Step 3: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS" add IdealAkeWms/Views/Shared/_Layout.cshtml
git -C "C:/Git/IDEAL-AKE-WMS" commit -m "feat(reporting): add Reporting menu entry (OSEON AG-Übersicht)

Sichtbar via CanReportOperationsAsync (Rolle 'reporting')."
```

---

## Task 8: AppVersion + Docs + TESTSZENARIEN

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`
- Modify: `IDEALAKEWMSService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `CLAUDE.md`
- Modify: `PROJECT_STATUS.md`
- Modify: `docs/TESTSZENARIEN.md`

- [ ] **Step 1: AppVersion bump (beide Projekte)**

In `IdealAkeWms/AppVersion.cs`:

```csharp
public const string Version = "1.7.1";
public const string Date = "2026-04-30";
```

In `IDEALAKEWMSService/AppVersion.cs` identisch.

- [ ] **Step 2: Help/Index.cshtml — neue Reporting-Section**

In `IdealAkeWms/Views/Help/Index.cshtml` an passender Stelle (z.B. nach Tracking-Block) einfügen:

```html
<h5 class="mt-4">OSEON Reporting</h5>
<h6 class="mt-3">AG-Übersicht aufrufen</h6>
<p>Im Hauptmenü unter <strong>Reporting &rarr; OSEON AG-Übersicht</strong> öffnen. Sichtbar für Benutzer mit Rolle <code>reporting</code>.</p>

<h6 class="mt-3">KPI-Cards</h6>
<ul>
    <li><strong>Überfällig:</strong> AGs deren berechneter Termin in der Vergangenheit liegt und Status &ne; 90.</li>
    <li><strong>Heute geplant:</strong> AGs deren berechneter Termin = heute (Status egal).</li>
    <li><strong>Heute erledigt:</strong> AGs mit Status = 90 und letzte Statusrückmeldung von heute.</li>
    <li><strong>Zukunft (X Tage):</strong> AGs mit berechnetem Termin im Horizont.</li>
</ul>

<h6 class="mt-3">Filter &amp; Tabs</h6>
<p>Tabs <em>Heute / Überfällig / Zukunft / Alle</em> wechseln den angezeigten Bereich. Filter (Werkbank, AG-Name, Kundenauftrag, FA-Nummer, Horizont) sind kombinierbar. Auftragsnummer in der Tabelle ist Link zum Tracking-Detail.</p>

<h6 class="mt-3">AGs ohne Config</h6>
<p>Ein gelber Banner zeigt, wenn AG-Namen aus OSEON-Aufträgen keine Config-Zeile in <em>Stammdaten &rarr; OSEON-Arbeitsgänge</em> haben — diese werden im Reporting ignoriert. Bitte pflegen, sonst fehlen sie auch in den KPIs.</p>

<h6 class="mt-3">Berechneter AG-Termin</h6>
<p>Berechnet als <code>Auftrags-Termin + Offset (Werktage)</code>. Offset stammt aus der OSEON-Config. Wochenenden und Feiertage werden übersprungen.</p>
```

- [ ] **Step 3: Help/Changelog.cshtml — v1.7.1 Eintrag**

Im obersten Bereich des Changelog (vor v1.7.0):

```html
<h5>v1.7.1 &mdash; 30.04.2026</h5>
<ul>
    <li>Neuer Reporting-Bereich: <strong>OSEON AG-Übersicht</strong> mit KPI-Cards (Überfällig / Heute geplant / Heute erledigt / Zukunft) und filter-/sortierbarer AG-Liste.</li>
    <li>Filter: Werkbank, AG-Name, Kundenauftrag, FA-Nummer, Horizont (Default 10 Tage).</li>
    <li>Banner für AGs ohne Config-Eintrag.</li>
    <li>Berechtigung: Rolle <code>reporting</code> via <code>[RequireReportingAccess]</code>.</li>
    <li>Refactor: Werktag-/Offset-Berechnung in <code>OseonDueDateCalculator</code>-Helper extrahiert (von Tracking + Reporting genutzt).</li>
</ul>
```

- [ ] **Step 4: CLAUDE.md erweitern**

Tabelle "AppSettings (DB-Tabelle)" um neue Zeile ergänzen:

```markdown
| `OseonReportingHorizonDays` | `10` | Reporting: Tage in die Zukunft (Default-Horizont) |
```

In Tabelle "Zugriffsschutz" sicherstellen, dass `[RequireReportingAccess]` als verwendet dokumentiert ist:

```markdown
| `[RequireReportingAccess]` | admin, reporting | OseonReportingController |
```

(Falls die Zeile bereits existierte mit `(fuer spaeteren BDE-Controller)` — auf den neuen Controller umstellen.)

- [ ] **Step 5: PROJECT_STATUS.md erweitern**

Neuen Abschnitt "v1.7.1 — OSEON Reporting (30.04.2026)" mit Bullet-Liste der Hauptfeatures.

- [ ] **Step 6: TESTSZENARIEN.md — neuer Bereich 16**

In `docs/TESTSZENARIEN.md` neuen Abschnitt anhängen:

```markdown
## 16. OSEON Reporting — AG-Übersicht

### TS-16.1 — KPI-Cards zeigen korrekte Counts
**Vorbedingungen:** Reporting-Rolle, mindestens 1 AG je Bereich (Überfällig / Heute / Zukunft) im OSEON-Mirror.
**Schritte:**
1. Menü Reporting -> OSEON AG-Übersicht öffnen.
**Erwartet:** 4 KPI-Cards zeigen plausible Counts; Summe Heute geplant ≥ Heute erledigt.

### TS-16.2 — Tab-Wechsel filtert Tabelle
**Vorbedingungen:** Daten in allen 3 Slices vorhanden.
**Schritte:**
1. Tabs reihum klicken: Heute / Überfällig / Zukunft / Alle.
**Erwartet:** Tabelle zeigt nur Zeilen des aktiven Slice. Default-Tab ist Heute.

### TS-16.3 — Filter Werkbank + AG-Name greifen
**Vorbedingungen:** Mehrere Werkbänke, mehrere AG-Namen.
**Schritte:**
1. Werkbank-Dropdown auf "WB-A1" stellen, AG-CSV "B" eintragen, Anwenden klicken.
**Erwartet:** Nur AGs vom Typ B an WB-A1 sichtbar. KPI-Counts spiegeln Filter.

### TS-16.4 — Auftragsnummer-Link öffnet Tracking
**Schritte:**
1. In der Tabelle auf eine FA-Nummer klicken.
**Erwartet:** OSEON-Tracking-Seite öffnet in neuem Tab, vorgefilltert mit der FA-Nummer.

### TS-16.5 — Banner für ungepflegte Configs
**Vorbedingungen:** AG-Name "FOO" in OSEON-Auftrag, KEIN Eintrag in OseonOperationConfig.
**Schritte:**
1. Reporting öffnen.
**Erwartet:** Gelber Banner "X AG(s) ohne Config-Eintrag".

### TS-16.6 — Berechtigungs-Block
**Vorbedingungen:** User OHNE Rolle reporting.
**Schritte:**
1. Direkter URL-Aufruf /Reporting/OseonOperations.
**Erwartet:** Redirect auf AccessDenied.
```

Und im Header-Block die Stand-Zeile aktualisieren:

```markdown
**Stand:** 2026-04-30 (v1.7.1)
```

- [ ] **Step 7: Build + Final Test**

```bash
cd "C:/Git/IDEAL-AKE-WMS" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS" && dotnet test --nologo --no-build 2>&1 | tail -10
```

Expected: 0 errors, full suite grün.

- [ ] **Step 8: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS" add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/ CLAUDE.md PROJECT_STATUS.md docs/TESTSZENARIEN.md
git -C "C:/Git/IDEAL-AKE-WMS" commit -m "chore(reporting): v1.7.1 docs + AppVersion + TESTSZENARIEN

Help-Seite: neue Reporting-Section.
Changelog: v1.7.1 Eintrag (30.04.2026).
CLAUDE.md: AppSettings + Zugriffsschutz Tabellen aktualisiert.
PROJECT_STATUS: v1.7.1 Eintrag.
TESTSZENARIEN: Bereich 16 (TS-16.1..16.6)."
```

---

## Final Summary

8 Tasks, geschätzte 30+ neue Tests + 6 manuelle Szenarien. Versions-Bump 1.7.0 → 1.7.1 auf `main`.

**Self-Review:**

1. **Spec coverage:** Jede Spec-Sektion hat einen Task:
   - §3 Komponenten → Tasks 1, 3, 4, 5, 6, 7
   - §4 VMs → Task 3
   - §5 Query-Logik → Task 4
   - §6 Helper → Task 1
   - §7 UI → Task 6
   - §8 Berechtigung + Menü → Task 7
   - §9 AppSettings → Task 2
   - §10 Edge Cases → in Tests (Task 4 + 5)
   - §11 Testing → Tests in jeweiligen Tasks + Task 8 (TESTSZENARIEN)
   - §12 Versions-Bump → Task 8
   - §13 YAGNI → keine Tasks (per Definition)

2. **Placeholder scan:** Keine TBDs, keine "implement later". Alle Code-Blöcke vollständig.

3. **Type consistency:** `OseonReportingSlice` (Enum), `OseonReportingRowViewModel` (record), `OseonReportingViewModel` (class) — überall identisch verwendet. `IBusinessDayService.AddBusinessDays(date, n, holidays)` Signatur konsistent in Helper-Tests + Controller-Tests + Helper-Implementation. `OseonStatusHelper.GetStatusText(int)` und `GetStatusBadgeClass(int)` — Namen aus Exploration verifiziert.

4. **Repository-Aufrufe in Controller:** `IProductionWorkplaceRepository.GetAllAsync()`, `IHolidayRepository.GetAllAsync()`, `IOseonOperationConfigRepository.GetAllAsync()`, `IAppSettingRepository.GetValueAsync(string)` — falls einer dieser Methoden-Namen abweicht (kann ich aus der Exploration nur teilweise bestätigen), in Task 5 Step 3 vor der Implementation kurz greppen und 1:1 anpassen.

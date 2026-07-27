# BDE Phase 2.3 Schichtkalender + Auto-Pause Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Schichtkalender (Default + Werkbank-Override) inkl. Auto-Pause-Worker am Schichtende und automatischer Feiertags-Sync von Nager.Date.

**Architecture:** Neue `BdeShift`-Entity (Default-Plan via NULL-FK, Werkbank-Override per FK). Read-only `BdeShiftCalendarService` liefert Schicht-Ende-Lookup. Worker-Service `BdeAutoPauseService` (stündlich) pausiert laufende Buchungen mit `EndedAt = exaktes Schicht-Ende`. Zweiter Worker `HolidaySyncService` (täglich) populiert die `Holidays`-Tabelle additive. Neuer Status `AutoPaused = 5` differenziert system- vs. manuell-pausierte Buchungen.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10, SQL Server, xUnit + FluentAssertions + Moq + EF InMemory, .NET HttpClient + Nager.Date Public API.

**Scope:** Pfade relativ zu `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1`, branch `feature/bde-phase-1`. Kein Versions-Bump (bleibt v1.8.2).

---

## Task 1: Schema-Erweiterungen — BdeShift, HolidaySource, AutoPaused, BdeUseCustomShiftPlan

**Files:**
- Modify: `IdealAkeWms/Models/BdeBookingStatus.cs`
- Create: `IdealAkeWms/Models/HolidaySource.cs`
- Modify: `IdealAkeWms/Models/Holiday.cs`
- Create: `IdealAkeWms/Models/BdeShift.cs`
- Modify: `IdealAkeWms/Models/ProductionWorkplace.cs`
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs`
- Create: `IdealAkeWms/Migrations/YYYYMMDDHHMMSS_AddBdeShiftCalendar.cs` (via EF CLI)
- Modify: `IdealAkeWms.Tests/Helpers/BdeBookingTestSeed.cs`

- [ ] **Step 1: BdeBookingStatus um AutoPaused erweitern**

`IdealAkeWms/Models/BdeBookingStatus.cs`:
```csharp
namespace IdealAkeWms.Models;

public enum BdeBookingStatus : byte
{
    Running = 1,
    Paused = 2,
    Finished = 3,
    Resumed = 4,
    AutoPaused = 5
}
```

- [ ] **Step 2: HolidaySource-Enum anlegen**

`IdealAkeWms/Models/HolidaySource.cs`:
```csharp
namespace IdealAkeWms.Models;

public enum HolidaySource : byte
{
    Manual = 1,
    NagerSync = 2
}
```

- [ ] **Step 3: Holiday um Source-Feld erweitern**

In `IdealAkeWms/Models/Holiday.cs` zwischen `Description` und der schließenden `}` einfügen:

```csharp
    [Required]
    [Display(Name = "Quelle")]
    public HolidaySource Source { get; set; } = HolidaySource.Manual;
```

- [ ] **Step 4: BdeShift-Entity anlegen**

`IdealAkeWms/Models/BdeShift.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class BdeShift : AuditableEntity
{
    [Required]
    [Display(Name = "Wochentag")]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    [Display(Name = "Beginn")]
    public TimeSpan StartTime { get; set; }

    [Required]
    [Display(Name = "Ende")]
    public TimeSpan EndTime { get; set; }

    [Display(Name = "Werkbank")]
    public int? ProductionWorkplaceId { get; set; }
    public ProductionWorkplace? ProductionWorkplace { get; set; }

    [StringLength(50)]
    [Display(Name = "Bezeichnung")]
    public string? Name { get; set; }
}
```

- [ ] **Step 5: ProductionWorkplace um BdeUseCustomShiftPlan erweitern**

In `IdealAkeWms/Models/ProductionWorkplace.cs` neben den bestehenden `BdeAktiv`/`BdeDefaultArbeitsgang`-Feldern:

```csharp
    [Display(Name = "Eigener Schichtplan")]
    public bool BdeUseCustomShiftPlan { get; set; }
```

- [ ] **Step 6: ApplicationDbContext erweitern**

In `IdealAkeWms/Data/ApplicationDbContext.cs`:

a) Neuer DbSet (bei den anderen Bde*-DbSets):
```csharp
    public DbSet<BdeShift> BdeShifts => Set<BdeShift>();
```

b) Im `OnModelCreating`-Block den existierenden CHECK-Constraint anpassen (suche nach `CK_BdeBookings_StatusEnded`):

```csharp
entity.ToTable(t => t.HasCheckConstraint("CK_BdeBookings_StatusEnded",
    "([Status] = 1 AND [EndedAt] IS NULL) OR ([Status] IN (2,3,4,5) AND [EndedAt] IS NOT NULL)"));
```

c) `BdeShift`-Entity-Konfiguration ergänzen:

```csharp
        modelBuilder.Entity<BdeShift>(entity =>
        {
            entity.HasIndex(e => new { e.ProductionWorkplaceId, e.DayOfWeek })
                .HasDatabaseName("IX_BdeShifts_Workplace_Day");

            entity.HasOne(e => e.ProductionWorkplace)
                .WithMany()
                .HasForeignKey(e => e.ProductionWorkplaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
```

- [ ] **Step 7: Test-Seed-Helper sicherstellen**

In `IdealAkeWms.Tests/Helpers/BdeBookingTestSeed.cs` keine direkte Änderung nötig — die seeded `ProductionWorkplace` wird `BdeUseCustomShiftPlan = false` (Default) haben, was Phase-1-Verhalten beibehält.

- [ ] **Step 8: EF-Migration generieren**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms" && dotnet ef migrations add AddBdeShiftCalendar
```

Erwartet: neue Datei `Migrations/*_AddBdeShiftCalendar.cs` mit `CreateTable` für BdeShifts, `AddColumn` für `ProductionWorkplaces.BdeUseCustomShiftPlan` + `Holidays.Source`, sowie Drop+Add des CHECK-Constraints.

- [ ] **Step 9: Migration-Inhalt prüfen**

```bash
cat "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Migrations/"*_AddBdeShiftCalendar.cs
```

Erwartet: `Up()` enthält:
- `migrationBuilder.AddColumn<bool>("BdeUseCustomShiftPlan", "ProductionWorkplaces", ..., defaultValue: false)`
- `migrationBuilder.AddColumn<byte>("Source", "Holidays", ..., defaultValue: (byte)1)`
- `migrationBuilder.CreateTable(name: "BdeShifts", ...)` mit FK auf ProductionWorkplaces
- `migrationBuilder.CreateIndex("IX_BdeShifts_Workplace_Day", ...)`
- DropCheckConstraint + AddCheckConstraint für `CK_BdeBookings_StatusEnded`

Falls unrelated Drift drin: Migration removen, beheben, neu generieren.

- [ ] **Step 10: Build + Tests**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -5
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Erwartet: 0 Fehler, alle bestehenden Tests grün (kein Status-Mismatch im CHECK weil EF InMemory Constraints ignoriert).

- [ ] **Step 11: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Models/ IdealAkeWms/Data/ApplicationDbContext.cs IdealAkeWms/Migrations/
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): add BdeShift entity + AutoPaused status + Holiday source field

EF migration AddBdeShiftCalendar:
- new BdeShifts table (DayOfWeek, StartTime, EndTime, optional FK to Werkbank)
- ProductionWorkplaces.BdeUseCustomShiftPlan flag
- Holidays.Source (Manual=1, NagerSync=2)
- BdeBookingStatus.AutoPaused=5; CHECK extended to IN (2,3,4,5)"
```

---

## Task 2: AppSettingKeys + Settings-Seeding (App + Service)

**Files:**
- Modify: `IdealAkeWms/Models/AppSettingKeys.cs`
- Modify: `IdealAkeWms/Program.cs`
- Modify: `IDEALAKEWMSService/appsettings.json`
- Modify: `IDEALAKEWMSService/Models/WorkerSettings.cs` (oder vergleichbare Service-Settings-Klasse)
- Modify: `IdealAkeWms/Views/Settings/Index.cshtml`

- [ ] **Step 1: AppSettingKeys erweitern**

In `IdealAkeWms/Models/AppSettingKeys.cs` einen neuen Konstanten-Eintrag (bei den Bde-Keys):

```csharp
    public const string BdeSchichtkalenderAktiv = "BdeSchichtkalenderAktiv";
```

- [ ] **Step 2: AppSetting-Seeding in Program.cs**

In `IdealAkeWms/Program.cs` im bestehenden BDE-Seed-Tuple-Array (`bdeSettings`):

```csharp
(AppSettingKeys.BdeSchichtkalenderAktiv, "false", "Schichtkalender + Auto-Pause am Schichtende aktiv")
```

als zusätzlichen Tuple-Eintrag hinzufügen.

- [ ] **Step 3: Service-Settings (Worker)**

Grep zuerst, wo Service-Settings definiert sind:

```bash
grep -rn "SyncIntervalMinutes\|FeiertagSync\|BdeAutoPause" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IDEALAKEWMSService/" 2>/dev/null | head -10
```

In `IDEALAKEWMSService/appsettings.json` im `Sync`-Abschnitt ergänzen:

```json
"BdeAutoPauseIntervalMinutes": 60,
"FeiertagSyncEnabled": false,
"FeiertagCountryCode": "AT",
"FeiertagRegion": "",
"FeiertagJahreVoraus": 2
```

In der `WorkerSettings`/`SyncSettings`-POCO-Klasse (Service-Projekt) entsprechende Properties:

```csharp
public int BdeAutoPauseIntervalMinutes { get; set; } = 60;
public bool FeiertagSyncEnabled { get; set; }
public string FeiertagCountryCode { get; set; } = "AT";
public string FeiertagRegion { get; set; } = "";
public int FeiertagJahreVoraus { get; set; } = 2;
```

(Wenn die Service-Settings via `ServiceSettings`-DB-Tabelle persistiert werden statt nur appsettings.json, müssen entsprechende Seed-Einträge auch in dieser Tabelle ergänzt werden — Pattern aus existierenden Sync-Settings übernehmen.)

- [ ] **Step 4: Settings-UI um neue BDE-Toggle erweitern**

In `IdealAkeWms/Views/Settings/Index.cshtml` die BDE-Gruppe erweitern:

```csharp
("BDE", new[] {
    "BdeAktiv", "BdeNurFaMeldung", "BdeDefaultArbeitsgang",
    "BdeMehrfachBuchungProOperator", "BdeMehrfachBuchungProArbeitsgang",
    "BdeGleichzeitigerAbschlussBeiMehrfachStart",
    "BdeSchichtkalenderAktiv"
}),
```

- [ ] **Step 5: Build + Tests**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Erwartet: alle Tests grün.

- [ ] **Step 6: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/ IDEALAKEWMSService/
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): seed BdeSchichtkalenderAktiv + service settings for Phase 2.3

AppSettings: BdeSchichtkalenderAktiv (default false).
ServiceSettings: BdeAutoPauseIntervalMinutes (60), FeiertagSyncEnabled
(false), FeiertagCountryCode (AT), FeiertagRegion (empty), FeiertagJahreVoraus (2)."
```

---

## Task 3: BdeShiftCalendarService (TDD)

**Files:**
- Create: `IdealAkeWms/Services/IBdeShiftCalendarService.cs`
- Create: `IdealAkeWms/Services/BdeShiftCalendarService.cs`
- Create: `IdealAkeWms.Tests/Services/BdeShiftCalendarServiceTests.cs`
- Modify: `IdealAkeWms/Program.cs` (DI-Registration)

- [ ] **Step 1: Interface anlegen**

`IdealAkeWms/Services/IBdeShiftCalendarService.cs`:

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

public interface IBdeShiftCalendarService
{
    /// <summary>
    /// Liefert das relevante Schichtende fuer eine Buchung — null wenn keine Auto-Pause greift
    /// (Master-Toggle aus, Wochenende, Feiertag, ausserhalb aller Schichten oder leerer Override-Plan).
    /// </summary>
    Task<DateTime?> GetShiftEndForBookingAsync(int workplaceId, DateTime startedAt);

    Task<IReadOnlyList<BdeShift>> GetShiftsAsync(int workplaceId, DayOfWeek day);
}
```

- [ ] **Step 2: Test-Datei mit allen 9 Tests anlegen**

`IdealAkeWms.Tests/Services/BdeShiftCalendarServiceTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class BdeShiftCalendarServiceTests
{
    private static (ApplicationDbContext ctx, BdeShiftCalendarService svc) Setup(bool masterEnabled = true)
    {
        var ctx = TestDbContextFactory.Create();
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeSchichtkalenderAktiv))
                .ReturnsAsync(masterEnabled ? "true" : "false");
        return (ctx, new BdeShiftCalendarService(ctx, settings.Object));
    }

    private static int SeedWorkplace(ApplicationDbContext ctx, bool useCustom = false)
    {
        var wp = new ProductionWorkplace
        {
            Name = "WB", BdeAktiv = true, BdeUseCustomShiftPlan = useCustom,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.SaveChanges();
        return wp.Id;
    }

    private static BdeShift NewShift(DayOfWeek day, int startHour, int endHour, int? workplaceId = null, string? name = null) => new()
    {
        DayOfWeek = day, StartTime = TimeSpan.FromHours(startHour), EndTime = TimeSpan.FromHours(endHour),
        ProductionWorkplaceId = workplaceId, Name = name,
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
    };

    private static DateTime Monday(int hour, int minute = 0)
    {
        var today = DateTime.Today;
        var diff = ((int)today.DayOfWeek + 6) % 7; // Mo=0
        return today.AddDays(-diff).AddHours(hour).AddMinutes(minute);
    }

    [Fact]
    public async Task MasterToggleOff_ReturnsNull()
    {
        var (ctx, svc) = Setup(masterEnabled: false);
        var wp = SeedWorkplace(ctx);
        ctx.BdeShifts.Add(NewShift(Monday(0).DayOfWeek, 6, 14));
        await ctx.SaveChangesAsync();

        var result = await svc.GetShiftEndForBookingAsync(wp, Monday(8));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Sunday_NoShifts_ReturnsNull()
    {
        var (ctx, svc) = Setup();
        var wp = SeedWorkplace(ctx);
        ctx.BdeShifts.Add(NewShift(DayOfWeek.Monday, 6, 14));
        await ctx.SaveChangesAsync();

        var sunday = Monday(8).AddDays(-1); // Sonntag
        var result = await svc.GetShiftEndForBookingAsync(wp, sunday);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Holiday_ReturnsNull()
    {
        var (ctx, svc) = Setup();
        var wp = SeedWorkplace(ctx);
        var monday = Monday(8);
        ctx.BdeShifts.Add(NewShift(monday.DayOfWeek, 6, 14));
        ctx.Holidays.Add(new Holiday { Date = monday.Date, Source = HolidaySource.Manual,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();

        var result = await svc.GetShiftEndForBookingAsync(wp, monday);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Within_EarlyShift_ReturnsShiftEnd()
    {
        var (ctx, svc) = Setup();
        var wp = SeedWorkplace(ctx);
        var monday = Monday(8, 30);
        ctx.BdeShifts.Add(NewShift(monday.DayOfWeek, 6, 14));
        await ctx.SaveChangesAsync();

        var result = await svc.GetShiftEndForBookingAsync(wp, monday);

        result.Should().Be(monday.Date.AddHours(14));
    }

    [Fact]
    public async Task BetweenShifts_ReturnsNextShiftEnd()
    {
        var (ctx, svc) = Setup();
        var wp = SeedWorkplace(ctx);
        var monday = Monday(14, 30);
        ctx.BdeShifts.AddRange(NewShift(monday.DayOfWeek, 6, 14), NewShift(monday.DayOfWeek, 14, 22));
        await ctx.SaveChangesAsync();

        var result = await svc.GetShiftEndForBookingAsync(wp, monday);

        result.Should().Be(monday.Date.AddHours(22));
    }

    [Fact]
    public async Task BeforeFirstShift_ReturnsFirstShiftEnd()
    {
        var (ctx, svc) = Setup();
        var wp = SeedWorkplace(ctx);
        var monday = Monday(4);
        ctx.BdeShifts.Add(NewShift(monday.DayOfWeek, 6, 14));
        await ctx.SaveChangesAsync();

        var result = await svc.GetShiftEndForBookingAsync(wp, monday);

        result.Should().Be(monday.Date.AddHours(14));
    }

    [Fact]
    public async Task AfterLastShift_ReturnsNull()
    {
        var (ctx, svc) = Setup();
        var wp = SeedWorkplace(ctx);
        var monday = Monday(23);
        ctx.BdeShifts.Add(NewShift(monday.DayOfWeek, 6, 14));
        await ctx.SaveChangesAsync();

        var result = await svc.GetShiftEndForBookingAsync(wp, monday);

        result.Should().BeNull();
    }

    [Fact]
    public async Task WorkbenchOverride_PrefersOwnPlan()
    {
        var (ctx, svc) = Setup();
        var wp = SeedWorkplace(ctx, useCustom: true);
        var monday = Monday(8);
        ctx.BdeShifts.Add(NewShift(monday.DayOfWeek, 6, 14, workplaceId: null));        // Default: 06–14
        ctx.BdeShifts.Add(NewShift(monday.DayOfWeek, 8, 16, workplaceId: wp));          // Werkbank: 08–16
        await ctx.SaveChangesAsync();

        var result = await svc.GetShiftEndForBookingAsync(wp, monday);

        result.Should().Be(monday.Date.AddHours(16));
    }

    [Fact]
    public async Task WorkbenchOverrideEmpty_ReturnsNull()
    {
        var (ctx, svc) = Setup();
        var wp = SeedWorkplace(ctx, useCustom: true);
        var monday = Monday(8);
        // Default-Plan vorhanden, aber Override-Toggle erzwingt eigenen (leeren) Plan
        ctx.BdeShifts.Add(NewShift(monday.DayOfWeek, 6, 14, workplaceId: null));
        await ctx.SaveChangesAsync();

        var result = await svc.GetShiftEndForBookingAsync(wp, monday);

        result.Should().BeNull();
    }
}
```

- [ ] **Step 3: Tests laufen lassen, fehlschlagen erwartet**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~BdeShiftCalendarServiceTests" 2>&1 | tail -10
```

Erwartet: Build-Fehler `BdeShiftCalendarService` not found.

- [ ] **Step 4: Service-Implementation**

`IdealAkeWms/Services/BdeShiftCalendarService.cs`:

```csharp
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Services;

public class BdeShiftCalendarService : IBdeShiftCalendarService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IAppSettingRepository _settings;

    public BdeShiftCalendarService(ApplicationDbContext ctx, IAppSettingRepository settings)
    {
        _ctx = ctx;
        _settings = settings;
    }

    public async Task<DateTime?> GetShiftEndForBookingAsync(int workplaceId, DateTime startedAt)
    {
        var enabled = (await _settings.GetValueAsync(AppSettingKeys.BdeSchichtkalenderAktiv))?
            .Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        if (!enabled) return null;

        var workplace = await _ctx.ProductionWorkplaces.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workplaceId);
        if (workplace == null) return null;

        // Feiertag pruefen
        var date = startedAt.Date;
        var isHoliday = await _ctx.Holidays.AsNoTracking().AnyAsync(h => h.Date == date);
        if (isHoliday) return null;

        // Schichten laden — Werkbank-Override hat Vorrang wenn Toggle EIN
        var shifts = workplace.BdeUseCustomShiftPlan
            ? await _ctx.BdeShifts.AsNoTracking().Where(s => s.ProductionWorkplaceId == workplaceId && s.DayOfWeek == startedAt.DayOfWeek).ToListAsync()
            : await _ctx.BdeShifts.AsNoTracking().Where(s => s.ProductionWorkplaceId == null && s.DayOfWeek == startedAt.DayOfWeek).ToListAsync();

        if (shifts.Count == 0) return null;

        var startTimeOfDay = startedAt.TimeOfDay;

        // 1) Schicht in der startedAt liegt
        var current = shifts.FirstOrDefault(s => s.StartTime <= startTimeOfDay && startTimeOfDay <= s.EndTime);
        if (current != null)
            return date + current.EndTime;

        // 2) Naechstfolgende Schicht des Tages
        var nextLater = shifts.Where(s => s.StartTime > startTimeOfDay).OrderBy(s => s.StartTime).FirstOrDefault();
        if (nextLater != null)
            return date + nextLater.EndTime;

        // 3) Alle Schichten des Tages liegen vor startedAt → kein Auto-Pause
        return null;
    }

    public async Task<IReadOnlyList<BdeShift>> GetShiftsAsync(int workplaceId, DayOfWeek day)
    {
        var workplace = await _ctx.ProductionWorkplaces.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workplaceId);
        if (workplace == null) return Array.Empty<BdeShift>();

        return workplace.BdeUseCustomShiftPlan
            ? await _ctx.BdeShifts.AsNoTracking().Where(s => s.ProductionWorkplaceId == workplaceId && s.DayOfWeek == day).ToListAsync()
            : await _ctx.BdeShifts.AsNoTracking().Where(s => s.ProductionWorkplaceId == null && s.DayOfWeek == day).ToListAsync();
    }
}
```

- [ ] **Step 5: DI-Registration in Program.cs**

In `IdealAkeWms/Program.cs` bei den anderen BDE-Service-Registrierungen:

```csharp
builder.Services.AddScoped<IBdeShiftCalendarService, BdeShiftCalendarService>();
```

- [ ] **Step 6: Tests laufen — grün**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~BdeShiftCalendarServiceTests" 2>&1 | tail -10
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Erwartet: 9 neue Tests grün, gesamte Test-Suite weiter grün.

- [ ] **Step 7: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Services/IBdeShiftCalendarService.cs IdealAkeWms/Services/BdeShiftCalendarService.cs IdealAkeWms/Program.cs IdealAkeWms.Tests/Services/BdeShiftCalendarServiceTests.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): add BdeShiftCalendarService for shift-end lookup

Read-only service: given workplaceId + startedAt, returns relevant
shift-end (or null when not auto-paused: master toggle off, holiday,
no shifts at all, or after last shift of the day). Honors
ProductionWorkplace.BdeUseCustomShiftPlan toggle to switch between
default and per-workbench shift plan."
```

---

## Task 4: BdeBookingService.ResumeAsync — AutoPaused akzeptieren (P7)

**Files:**
- Modify: `IdealAkeWms/Services/BdeBookingService.cs`
- Modify: `IdealAkeWms.Tests/Services/BdeBookingServiceTests.cs`

- [ ] **Step 1: Tests anhängen**

In `IdealAkeWms.Tests/Services/BdeBookingServiceTests.cs` vor der schließenden `}`:

```csharp
    [Fact]
    public async Task Resume_AcceptsAutoPausedParent()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var parent = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.AutoPaused,
            startedAt: DateTime.Now.AddHours(-3), endedAt: DateTime.Now.AddHours(-1));
        ctx.BdeBookings.Add(parent);
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx);

        var result = await svc.ResumeAsync(parent.Id, ids.OperatorId, BdeBookingType.Production, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
        result.Booking!.ParentBookingId.Should().Be(parent.Id);
        var updatedParent = await ctx.BdeBookings.FindAsync(parent.Id);
        updatedParent!.Status.Should().Be(BdeBookingStatus.Resumed);
    }

    [Fact]
    public async Task Resume_RejectsRunningParent()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var parent = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: DateTime.Now.AddHours(-1));
        ctx.BdeBookings.Add(parent);
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx);

        var result = await svc.ResumeAsync(parent.Id, ids.OperatorId, BdeBookingType.Production, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.InvalidState);
        result.Message.Should().Contain("nicht pausiert");
    }
```

- [ ] **Step 2: ResumeAsync-Check erweitern**

In `IdealAkeWms/Services/BdeBookingService.cs` in `ResumeAsync` die Status-Prüfung von:

```csharp
if (parent.Status != BdeBookingStatus.Paused)
    return BdeBookingResult.Invalid("Ziel-Buchung ist nicht pausiert.");
```

zu:

```csharp
if (parent.Status != BdeBookingStatus.Paused && parent.Status != BdeBookingStatus.AutoPaused)
    return BdeBookingResult.Invalid("Ziel-Buchung ist nicht pausiert.");
```

- [ ] **Step 3: Tests grün**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Erwartet: Tests grün, neue 2 Tests dabei.

- [ ] **Step 4: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Services/BdeBookingService.cs IdealAkeWms.Tests/Services/BdeBookingServiceTests.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): ResumeAsync accepts AutoPaused parent status

Phase-2.3 introduces Status.AutoPaused (=5). Resume now treats it
identically to manual Paused — same Resumed-transition + new child
booking. Pre-Phase-2.3 behavior preserved (manual Paused works)."
```

---

## Task 5: BdeTerminalController.PausedBookings — AutoPaused einschließen + JS-Label

**Files:**
- Modify: `IdealAkeWms/Controllers/BdeTerminalController.cs`
- Modify: `IdealAkeWms/wwwroot/js/bde-terminal.js`
- Modify: `IdealAkeWms.Tests/Controllers/BdeTerminalControllerTests.cs`

- [ ] **Step 1: Test ergänzen**

In `IdealAkeWms.Tests/Controllers/BdeTerminalControllerTests.cs`:

```csharp
    [Fact]
    public async Task PausedBookings_IncludesAutoPaused()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.AutoPaused,
            startedAt: DateTime.Now.AddHours(-3), endedAt: DateTime.Now.AddHours(-1)));
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Paused,
            startedAt: DateTime.Now.AddHours(-2), endedAt: DateTime.Now.AddMinutes(-30)));
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx);
        var result = await controller.PausedBookings(ids.OperatorId);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
        parsed.GetArrayLength().Should().Be(2);
        json.Should().Contain("AutoPaused");
    }
```

- [ ] **Step 2: PausedBookings-Query erweitern**

In `IdealAkeWms/Controllers/BdeTerminalController.cs` in `PausedBookings` die Where-Klausel `b.Status == BdeBookingStatus.Paused` auf:

```csharp
.Where(b => b.BdeOperatorId == operatorId
         && (b.Status == BdeBookingStatus.Paused || b.Status == BdeBookingStatus.AutoPaused)
         && !b.IsCancelled
         && !_ctx.BdeBookings.Any(child => child.ParentBookingId == b.Id))
```

Die `Select`-Projektion zusätzlich um `status` erweitern:

```csharp
.Select(b => new {
    bookingId = b.Id,
    orderNumber = ...,
    operationNumber = ...,
    operationName = ...,
    pausedAt = b.EndedAt,
    status = b.Status.ToString()      // NEU: Frontend kann zwischen Paused / AutoPaused unterscheiden
})
```

- [ ] **Step 3: JS-Label im Paused-Hint anpassen**

In `IdealAkeWms/wwwroot/js/bde-terminal.js` die Render-Funktion für den Paused-Hint (suche `pausiert seit`):

```javascript
const isAutoPaused = i.status === 'AutoPaused';
const pausedLabel = isAutoPaused ? 'auto-pausiert seit' : 'pausiert seit';
const suffix = isAutoPaused ? ' (Schichtende)' : '';

return `
    <li class="mb-2">
        <strong>${i.orderNumber} / ${i.operationNumber} ${i.operationName || ''}</strong>
        <small class="text-muted d-block">${pausedLabel} ${i.pausedAt ? new Date(i.pausedAt).toLocaleString('de-DE') : ''}${suffix}</small>
        <button type="button" class="btn btn-sm btn-warning mt-1" data-booking-id="${i.bookingId}" data-resume>Fortsetzen</button>
    </li>
`;
```

- [ ] **Step 4: Tests + Build**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Erwartet: alle Tests grün.

- [ ] **Step 5: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Controllers/BdeTerminalController.cs IdealAkeWms/wwwroot/js/bde-terminal.js IdealAkeWms.Tests/Controllers/BdeTerminalControllerTests.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): PausedBookings hint includes AutoPaused entries

Endpoint: query matches Paused OR AutoPaused. Response carries the
status string. Frontend label distinguishes 'pausiert seit' (manual)
vs 'auto-pausiert seit ... (Schichtende)' (system)."
```

---

## Task 6: BdeAutoPauseService (Worker)

**Files:**
- Create: `IDEALAKEWMSService/Services/IBdeAutoPauseService.cs`
- Create: `IDEALAKEWMSService/Services/BdeAutoPauseService.cs`
- Create: `IDEALAKEWMSService.Tests/Services/BdeAutoPauseServiceTests.cs`

- [ ] **Step 1: Interface + Result-Record**

`IDEALAKEWMSService/Services/IBdeAutoPauseService.cs`:

```csharp
namespace IDEALAKEWMSService.Services;

public interface IBdeAutoPauseService
{
    Task<AutoPauseResult> RunAsync(CancellationToken ct);
}

public record AutoPauseResult(int CheckedCount, int PausedCount, List<string> Errors);
```

- [ ] **Step 2: Tests anlegen**

`IDEALAKEWMSService.Tests/Services/BdeAutoPauseServiceTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class BdeAutoPauseServiceTests
{
    private static (ApplicationDbContext ctx, BdeAutoPauseService svc, Mock<IAppSettingRepository> settings)
        Setup(bool masterEnabled = true)
    {
        var ctx = TestDbContextFactory.Create();
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BdeSchichtkalenderAktiv))
            .ReturnsAsync(masterEnabled ? "true" : "false");
        var calendar = new BdeShiftCalendarService(ctx, settings.Object);
        var svc = new BdeAutoPauseService(ctx, calendar, settings.Object, NullLogger<BdeAutoPauseService>.Instance);
        return (ctx, svc, settings);
    }

    [Fact]
    public async Task Run_NoActiveBookings_ReturnsZero()
    {
        var (ctx, svc, _) = Setup();
        var result = await svc.RunAsync(CancellationToken.None);

        result.CheckedCount.Should().Be(0);
        result.PausedCount.Should().Be(0);
    }

    [Fact]
    public async Task Run_MasterToggleOff_NoOps()
    {
        var (ctx, svc, _) = Setup(masterEnabled: false);
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-2)));
        await ctx.SaveChangesAsync();

        var result = await svc.RunAsync(CancellationToken.None);

        result.CheckedCount.Should().Be(0);
        result.PausedCount.Should().Be(0);
    }

    [Fact]
    public async Task Run_BookingPastShiftEnd_PausesWithShiftEndTimestamp()
    {
        var (ctx, svc, _) = Setup();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        // Frühschicht 06–14 für heute
        var today = DateTime.Today;
        ctx.BdeShifts.Add(new BdeShift {
            DayOfWeek = today.DayOfWeek, StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14),
            ProductionWorkplaceId = null,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        // Buchung läuft seit 08:00 heute
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: today.AddHours(8)));
        await ctx.SaveChangesAsync();

        var result = await svc.RunAsync(CancellationToken.None);

        result.PausedCount.Should().Be(1);
        var pausedBooking = ctx.BdeBookings.First();
        pausedBooking.Status.Should().Be(BdeBookingStatus.AutoPaused);
        pausedBooking.EndedAt.Should().Be(today.AddHours(14));
        pausedBooking.ModifiedBy.Should().Be("BDE-AutoPause");
    }

    [Fact]
    public async Task Run_BookingWithinShift_NotPaused()
    {
        var (ctx, svc, _) = Setup();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var today = DateTime.Today;

        ctx.BdeShifts.Add(new BdeShift {
            DayOfWeek = today.DayOfWeek, StartTime = TimeSpan.FromHours(0), EndTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59)),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: DateTime.Now.AddMinutes(-30)));
        await ctx.SaveChangesAsync();

        var result = await svc.RunAsync(CancellationToken.None);

        result.PausedCount.Should().Be(0);
    }

    [Fact]
    public async Task Run_BookingOutsideAnyShift_NotPaused()
    {
        var (ctx, svc, _) = Setup();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var today = DateTime.Today;

        ctx.BdeShifts.Add(new BdeShift {
            DayOfWeek = today.DayOfWeek, StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        // Buchung um 23:00 — nach allen Schichten des Tages
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: today.AddHours(23)));
        await ctx.SaveChangesAsync();

        var result = await svc.RunAsync(CancellationToken.None);

        result.PausedCount.Should().Be(0);
    }

    [Fact]
    public async Task Run_HolidayDay_SkipsAutoPause()
    {
        var (ctx, svc, _) = Setup();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var today = DateTime.Today;

        ctx.BdeShifts.Add(new BdeShift {
            DayOfWeek = today.DayOfWeek, StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.Holidays.Add(new Holiday {
            Date = today, Description = "Heute Feiertag", Source = HolidaySource.Manual,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: today.AddHours(8)));
        await ctx.SaveChangesAsync();

        var result = await svc.RunAsync(CancellationToken.None);

        result.PausedCount.Should().Be(0);
    }

    [Fact]
    public async Task Run_PausesActivityAndSetupAlongProduction()
    {
        var (ctx, svc, _) = Setup();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var today = DateTime.Today;

        ctx.BdeShifts.Add(new BdeShift {
            DayOfWeek = today.DayOfWeek, StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, today.AddHours(8)));
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Activity,   BdeBookingStatus.Running, today.AddHours(8)));
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Setup,      BdeBookingStatus.Running, today.AddHours(8)));
        await ctx.SaveChangesAsync();

        var result = await svc.RunAsync(CancellationToken.None);

        result.PausedCount.Should().Be(3);
        ctx.BdeBookings.All(b => b.Status == BdeBookingStatus.AutoPaused).Should().BeTrue();
    }

    [Fact]
    public async Task Run_DoesNotTouchAlreadyEndedBookings()
    {
        var (ctx, svc, _) = Setup();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var today = DateTime.Today;

        ctx.BdeShifts.Add(new BdeShift {
            DayOfWeek = today.DayOfWeek, StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: today.AddHours(7), endedAt: today.AddHours(13)));
        await ctx.SaveChangesAsync();

        var result = await svc.RunAsync(CancellationToken.None);

        result.PausedCount.Should().Be(0);
    }
}
```

Wenn `IDEALAKEWMSService.Tests` keinen Verweis auf `IdealAkeWms`-Projekt hat, muss er ergänzt werden. Grep:

```bash
grep -n "ProjectReference" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj"
```

Wenn nicht vorhanden, im csproj ergänzen:

```xml
<ProjectReference Include="..\IdealAkeWms\IdealAkeWms.csproj" />
```

(Pattern aus existierenden Tests übernehmen.)

- [ ] **Step 3: Tests laufen — fehlschlagen**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~BdeAutoPauseServiceTests" 2>&1 | tail -10
```

- [ ] **Step 4: Service-Implementation**

`IDEALAKEWMSService/Services/BdeAutoPauseService.cs`:

```csharp
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

public class BdeAutoPauseService : IBdeAutoPauseService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IBdeShiftCalendarService _calendar;
    private readonly IAppSettingRepository _settings;
    private readonly ILogger<BdeAutoPauseService> _logger;

    public BdeAutoPauseService(ApplicationDbContext ctx, IBdeShiftCalendarService calendar,
        IAppSettingRepository settings, ILogger<BdeAutoPauseService> logger)
    {
        _ctx = ctx;
        _calendar = calendar;
        _settings = settings;
        _logger = logger;
    }

    public async Task<AutoPauseResult> RunAsync(CancellationToken ct)
    {
        var enabled = (await _settings.GetValueAsync(AppSettingKeys.BdeSchichtkalenderAktiv))?
            .Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        if (!enabled)
            return new AutoPauseResult(0, 0, new());

        var active = await _ctx.BdeBookings
            .Where(b => b.Status == BdeBookingStatus.Running && b.EndedAt == null && !b.IsCancelled)
            .ToListAsync(ct);

        var errors = new List<string>();
        var now = DateTime.Now;
        var paused = 0;

        foreach (var booking in active)
        {
            try
            {
                var shiftEnd = await _calendar.GetShiftEndForBookingAsync(booking.ProductionWorkplaceId, booking.StartedAt);
                if (shiftEnd == null) continue;
                if (shiftEnd > now) continue; // Schichtende noch in der Zukunft

                booking.Status = BdeBookingStatus.AutoPaused;
                booking.EndedAt = shiftEnd;
                booking.ModifiedAt = DateTime.Now;
                booking.ModifiedBy = "BDE-AutoPause";
                booking.ModifiedByWindows = "BDE-AutoPause";
                await _ctx.SaveChangesAsync(ct);
                paused++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-Pause failed for bookingId={BookingId}", booking.Id);
                errors.Add($"Booking {booking.Id}: {ex.GetType().Name} — {ex.Message}");
            }
        }

        _logger.LogInformation("BdeAutoPause: checked={Checked} paused={Paused} errors={Errors}",
            active.Count, paused, errors.Count);

        return new AutoPauseResult(active.Count, paused, errors);
    }
}
```

- [ ] **Step 5: Tests grün**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~BdeAutoPauseServiceTests" 2>&1 | tail -10
```

Erwartet: 8 Tests grün.

- [ ] **Step 6: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IDEALAKEWMSService/Services/IBdeAutoPauseService.cs IDEALAKEWMSService/Services/BdeAutoPauseService.cs IDEALAKEWMSService.Tests/
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): add BdeAutoPauseService worker

Iterates active Running bookings; for each, computes the relevant
shift-end via BdeShiftCalendarService. If shift-end <= now, sets
Status=AutoPaused with EndedAt=shift-end (NOT now — keeps booking
times accurate). Master toggle gates the whole run. Per-booking
exceptions don't stop the loop; logged and reported in result."
```

---

## Task 7: HolidaySyncService (Worker, Nager.Date)

**Files:**
- Create: `IDEALAKEWMSService/Services/IHolidaySyncService.cs`
- Create: `IDEALAKEWMSService/Services/HolidaySyncService.cs`
- Create: `IDEALAKEWMSService.Tests/Services/HolidaySyncServiceTests.cs`

- [ ] **Step 1: Interface + Result-Record + DTO für Nager-Response**

`IDEALAKEWMSService/Services/IHolidaySyncService.cs`:

```csharp
namespace IDEALAKEWMSService.Services;

public interface IHolidaySyncService
{
    Task<HolidaySyncResult> RunAsync(CancellationToken ct);
}

public record HolidaySyncResult(int FetchedCount, int InsertedCount, List<string> Errors);

public record NagerHoliday(
    string Date,        // "YYYY-MM-DD"
    string LocalName,
    string Name,
    string CountryCode,
    bool Fixed,
    bool Global,
    string[]? Counties,
    int? LaunchYear,
    string[] Types
);
```

- [ ] **Step 2: Tests mit gemocktem HttpClient**

`IDEALAKEWMSService.Tests/Services/HolidaySyncServiceTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class HolidaySyncServiceTests
{
    private record TestSettings(bool Enabled = true, string Country = "AT", string Region = "", int Years = 1, bool DryRun = false);

    private static (ApplicationDbContext ctx, HolidaySyncService svc) Setup(
        TestSettings testSettings, IEnumerable<NagerHoliday>[] perYearResponses)
    {
        var ctx = TestDbContextFactory.Create();

        var handler = new Mock<HttpMessageHandler>();
        var responseQueue = new Queue<IEnumerable<NagerHoliday>>(perYearResponses);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var json = JsonSerializer.Serialize(responseQueue.Dequeue());
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            });

        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("https://date.nager.at/") };
        var options = Options.Create(new HolidaySyncOptions
        {
            Enabled = testSettings.Enabled,
            CountryCode = testSettings.Country,
            Region = testSettings.Region,
            JahreVoraus = testSettings.Years,
            DryRun = testSettings.DryRun
        });

        var svc = new HolidaySyncService(ctx, http, options, NullLogger<HolidaySyncService>.Instance);
        return (ctx, svc);
    }

    private static NagerHoliday Holiday(string date, string name, string[]? counties = null) =>
        new(date, name, name, "AT", true, counties == null, counties, null, new[] { "Public" });

    [Fact]
    public async Task Run_SyncDisabled_NoOps()
    {
        var (ctx, svc) = Setup(new TestSettings(Enabled: false), Array.Empty<IEnumerable<NagerHoliday>>());

        var result = await svc.RunAsync(CancellationToken.None);

        result.FetchedCount.Should().Be(0);
        result.InsertedCount.Should().Be(0);
        ctx.Holidays.Count().Should().Be(0);
    }

    [Fact]
    public async Task Run_FetchesCurrentAndForwardYears()
    {
        var year = DateTime.Today.Year;
        var (ctx, svc) = Setup(new TestSettings(Years: 2), new[] {
            new[] { Holiday($"{year}-01-01", "Neujahr") }.AsEnumerable(),
            new[] { Holiday($"{year+1}-01-01", "Neujahr") }.AsEnumerable(),
            new[] { Holiday($"{year+2}-01-01", "Neujahr") }.AsEnumerable()
        });

        var result = await svc.RunAsync(CancellationToken.None);

        result.FetchedCount.Should().Be(3);
        result.InsertedCount.Should().Be(3);
    }

    [Fact]
    public async Task Run_InsertsOnlyMissingDates()
    {
        var year = DateTime.Today.Year;
        var existingDate = new DateTime(year, 1, 1);
        var (ctx, svc) = Setup(new TestSettings(Years: 0), new[] {
            new[] {
                Holiday($"{year}-01-01", "Neujahr"),
                Holiday($"{year}-01-06", "Heilige Drei Koenige")
            }.AsEnumerable()
        });
        ctx.Holidays.Add(new Holiday {
            Date = existingDate, Description = "Manual", Source = HolidaySource.Manual,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var result = await svc.RunAsync(CancellationToken.None);

        result.InsertedCount.Should().Be(1);
        ctx.Holidays.Count().Should().Be(2);
        ctx.Holidays.First(h => h.Date == existingDate).Source.Should().Be(HolidaySource.Manual);
    }

    [Fact]
    public async Task Run_PreservesManualEntries()
    {
        var year = DateTime.Today.Year;
        var (ctx, svc) = Setup(new TestSettings(Years: 0), new[] {
            new[] { Holiday($"{year}-05-01", "Tag der Arbeit") }.AsEnumerable()
        });
        ctx.Holidays.Add(new Holiday {
            Date = new DateTime(year, 5, 1), Description = "Manueller Eintrag", Source = HolidaySource.Manual,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        await svc.RunAsync(CancellationToken.None);

        var entry = ctx.Holidays.First(h => h.Date == new DateTime(year, 5, 1));
        entry.Source.Should().Be(HolidaySource.Manual);
        entry.Description.Should().Be("Manueller Eintrag");
    }

    [Fact]
    public async Task Run_WithRegion_FiltersCounties()
    {
        var year = DateTime.Today.Year;
        var (ctx, svc) = Setup(new TestSettings(Region: "AT-3", Years: 0), new[] {
            new[] {
                Holiday($"{year}-01-01", "Neujahr"),                                       // global → enthalten
                Holiday($"{year}-09-15", "NÖ Landesfeiertag", counties: new[] { "AT-3" }), // matched
                Holiday($"{year}-11-19", "Tirol Fest",        counties: new[] { "AT-7" }), // skip
            }.AsEnumerable()
        });

        var result = await svc.RunAsync(CancellationToken.None);

        result.InsertedCount.Should().Be(2);
        ctx.Holidays.Any(h => h.Description!.Contains("Neujahr")).Should().BeTrue();
        ctx.Holidays.Any(h => h.Description!.Contains("NÖ")).Should().BeTrue();
        ctx.Holidays.Any(h => h.Description!.Contains("Tirol")).Should().BeFalse();
    }

    [Fact]
    public async Task Run_DryRun_NoInserts()
    {
        var year = DateTime.Today.Year;
        var (ctx, svc) = Setup(new TestSettings(DryRun: true, Years: 0), new[] {
            new[] { Holiday($"{year}-01-01", "Neujahr") }.AsEnumerable()
        });

        var result = await svc.RunAsync(CancellationToken.None);

        result.FetchedCount.Should().Be(1);
        result.InsertedCount.Should().Be(0);
        ctx.Holidays.Count().Should().Be(0);
    }

    [Fact]
    public async Task Run_SetsSourceNagerSync()
    {
        var year = DateTime.Today.Year;
        var (ctx, svc) = Setup(new TestSettings(Years: 0), new[] {
            new[] { Holiday($"{year}-01-01", "Neujahr") }.AsEnumerable()
        });

        await svc.RunAsync(CancellationToken.None);

        ctx.Holidays.First().Source.Should().Be(HolidaySource.NagerSync);
    }

    [Fact]
    public async Task Run_ApiReturnsBadStatus_LogsError()
    {
        var ctx = TestDbContextFactory.Create();
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("https://date.nager.at/") };
        var options = Options.Create(new HolidaySyncOptions { Enabled = true, CountryCode = "AT", JahreVoraus = 0 });

        var svc = new HolidaySyncService(ctx, http, options, NullLogger<HolidaySyncService>.Instance);

        var result = await svc.RunAsync(CancellationToken.None);

        result.Errors.Should().NotBeEmpty();
        result.InsertedCount.Should().Be(0);
    }
}
```

- [ ] **Step 3: HolidaySyncOptions-POCO**

In `IDEALAKEWMSService/Services/HolidaySyncService.cs` (gleiche Datei):

```csharp
public class HolidaySyncOptions
{
    public bool Enabled { get; set; }
    public string CountryCode { get; set; } = "AT";
    public string Region { get; set; } = "";
    public int JahreVoraus { get; set; } = 2;
    public bool DryRun { get; set; }
}
```

- [ ] **Step 4: Service-Implementation**

`IDEALAKEWMSService/Services/HolidaySyncService.cs`:

```csharp
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace IDEALAKEWMSService.Services;

public class HolidaySyncService : IHolidaySyncService
{
    private readonly ApplicationDbContext _ctx;
    private readonly HttpClient _http;
    private readonly IOptions<HolidaySyncOptions> _options;
    private readonly ILogger<HolidaySyncService> _logger;

    public HolidaySyncService(ApplicationDbContext ctx, HttpClient http,
        IOptions<HolidaySyncOptions> options, ILogger<HolidaySyncService> logger)
    {
        _ctx = ctx;
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task<HolidaySyncResult> RunAsync(CancellationToken ct)
    {
        var opts = _options.Value;
        if (!opts.Enabled)
            return new HolidaySyncResult(0, 0, new());

        var errors = new List<string>();
        var fetched = 0;
        var inserted = 0;

        var startYear = DateTime.Today.Year;
        for (int year = startYear; year <= startYear + opts.JahreVoraus; year++)
        {
            try
            {
                var url = $"api/v3/PublicHolidays/{year}/{opts.CountryCode}";
                var holidays = await _http.GetFromJsonAsync<List<NagerHoliday>>(url, ct);
                if (holidays == null) continue;

                var filtered = string.IsNullOrWhiteSpace(opts.Region)
                    ? holidays.Where(h => h.Counties == null || h.Counties.Length == 0)
                    : holidays.Where(h => h.Counties == null || h.Counties.Length == 0 || h.Counties.Contains(opts.Region));

                foreach (var h in filtered)
                {
                    fetched++;
                    if (!DateTime.TryParse(h.Date, out var date)) continue;

                    if (await _ctx.Holidays.AnyAsync(existing => existing.Date == date.Date, ct))
                        continue; // additive only

                    if (opts.DryRun) continue;

                    _ctx.Holidays.Add(new Holiday
                    {
                        Date = date.Date,
                        Description = h.LocalName,
                        Source = HolidaySource.NagerSync,
                        CreatedAt = DateTime.Now,
                        CreatedBy = "HolidaySync",
                        CreatedByWindows = "HolidaySync"
                    });
                    inserted++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HolidaySync failed for year {Year}", year);
                errors.Add($"Year {year}: {ex.GetType().Name} — {ex.Message}");
            }
        }

        if (!opts.DryRun)
            await _ctx.SaveChangesAsync(ct);

        _logger.LogInformation("HolidaySync: fetched={Fetched} inserted={Inserted} errors={Errors}",
            fetched, inserted, errors.Count);

        return new HolidaySyncResult(fetched, inserted, errors);
    }
}
```

- [ ] **Step 5: Tests grün**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~HolidaySyncServiceTests" 2>&1 | tail -10
```

Erwartet: 8 Tests grün.

- [ ] **Step 6: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IDEALAKEWMSService/Services/IHolidaySyncService.cs IDEALAKEWMSService/Services/HolidaySyncService.cs IDEALAKEWMSService.Tests/Services/HolidaySyncServiceTests.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): add HolidaySyncService against Nager.Date API

Daily worker fetches public holidays from Nager.Date for current
year + N future years (configurable). Region filter (AT-3 NOe etc.)
applied client-side. Additive only — never overwrites manual entries.
DryRun honored. Per-year exceptions logged, don't break the loop."
```

---

## Task 8: Worker-Integration in Program.cs + Worker.cs

**Files:**
- Modify: `IDEALAKEWMSService/Program.cs`
- Modify: `IDEALAKEWMSService/Workers/SyncWorker.cs` (oder neuer Worker)

- [ ] **Step 1: DI-Registrierung**

In `IDEALAKEWMSService/Program.cs` bei den anderen Service-Registrierungen:

```csharp
builder.Services.AddScoped<IBdeShiftCalendarService, BdeShiftCalendarService>();
builder.Services.AddScoped<IBdeAutoPauseService, BdeAutoPauseService>();

builder.Services.Configure<HolidaySyncOptions>(builder.Configuration.GetSection("Sync"));
builder.Services.AddHttpClient<IHolidaySyncService, HolidaySyncService>(client =>
{
    client.BaseAddress = new Uri("https://date.nager.at/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

- [ ] **Step 2: Worker-Tick um BdeAutoPause + HolidaySync erweitern**

In `IDEALAKEWMSService/Workers/SyncWorker.cs` finde die existing-`ExecuteAsync`-Schleife. Pattern: pro Tick werden alle aktivierten Sync-Services aufgerufen. Ergänze:

```csharp
// BdeAutoPause — eigenes Intervall via Sync:BdeAutoPauseIntervalMinutes
if (await ShouldRunAutoPause())
{
    using var scope = _scopeFactory.CreateScope();
    var autoPause = scope.ServiceProvider.GetRequiredService<IBdeAutoPauseService>();
    await autoPause.RunAsync(stoppingToken);
}

// HolidaySync — täglich (24h)
if (await ShouldRunHolidaySync())
{
    using var scope = _scopeFactory.CreateScope();
    var holidaySync = scope.ServiceProvider.GetRequiredService<IHolidaySyncService>();
    await holidaySync.RunAsync(stoppingToken);
}
```

(Konkrete Implementierung des Schedulings hängt von Worker-Pattern ab; je nach Bestand: einfacher Vergleich `_lastRun + interval < DateTime.Now` mit gespeichertem Zeitstempel.)

- [ ] **Step 3: Build + Test**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Erwartet: alle Tests grün, Worker kompiliert.

- [ ] **Step 4: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IDEALAKEWMSService/Program.cs IDEALAKEWMSService/Workers/
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): wire BdeAutoPause + HolidaySync into Worker

DI registers both new services + HttpClient for Nager.Date.
SyncWorker invokes BdeAutoPauseService at the configured interval
(default 60min) and HolidaySyncService daily (24h)."
```

---

## Task 9: BdeShiftCalendarController + View (Default-Kalender-UI)

**Files:**
- Create: `IdealAkeWms/Controllers/BdeShiftCalendarController.cs`
- Create: `IdealAkeWms/Views/BdeShiftCalendar/Index.cshtml`
- Create: `IdealAkeWms/Models/ViewModels/BdeShiftEditViewModel.cs`
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml` (BDE-Untermenü)
- Create: `IdealAkeWms.Tests/Controllers/BdeShiftCalendarControllerTests.cs`

- [ ] **Step 1: ViewModel anlegen**

`IdealAkeWms/Models/ViewModels/BdeShiftEditViewModel.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models.ViewModels;

public class BdeShiftEditViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Wochentag")]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    [Display(Name = "Beginn")]
    [DataType(DataType.Time)]
    public TimeSpan StartTime { get; set; }

    [Required]
    [Display(Name = "Ende")]
    [DataType(DataType.Time)]
    public TimeSpan EndTime { get; set; }

    [StringLength(50)]
    [Display(Name = "Bezeichnung")]
    public string? Name { get; set; }

    public int? ProductionWorkplaceId { get; set; }
}
```

- [ ] **Step 2: Controller**

`IdealAkeWms/Controllers/BdeShiftCalendarController.cs`:

```csharp
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Controllers;

[RequireMasterDataAccess]
public class BdeShiftCalendarController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly ICurrentUserService _userSvc;

    public BdeShiftCalendarController(ApplicationDbContext ctx, ICurrentUserService userSvc)
    {
        _ctx = ctx;
        _userSvc = userSvc;
    }

    public async Task<IActionResult> Index()
    {
        var shifts = await _ctx.BdeShifts
            .Where(s => s.ProductionWorkplaceId == null)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .ToListAsync();
        return View(shifts);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BdeShiftEditViewModel vm)
    {
        if (vm.EndTime <= vm.StartTime)
            ModelState.AddModelError(nameof(vm.EndTime), "Ende muss nach dem Beginn liegen.");

        if (!ModelState.IsValid)
        {
            TempData["WarningMessage"] = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        _ctx.BdeShifts.Add(new BdeShift
        {
            DayOfWeek = vm.DayOfWeek,
            StartTime = vm.StartTime,
            EndTime = vm.EndTime,
            Name = vm.Name,
            ProductionWorkplaceId = vm.ProductionWorkplaceId,
            CreatedAt = DateTime.Now,
            CreatedBy = _userSvc.GetDisplayName(),
            CreatedByWindows = _userSvc.GetWindowsUserName()
        });
        await _ctx.SaveChangesAsync();

        TempData["SuccessMessage"] = "Schicht hinzugefuegt.";
        if (vm.ProductionWorkplaceId.HasValue)
            return RedirectToAction("Edit", "ProductionWorkplaces", new { id = vm.ProductionWorkplaceId.Value });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var shift = await _ctx.BdeShifts.FindAsync(id);
        if (shift != null)
        {
            var workplaceId = shift.ProductionWorkplaceId;
            _ctx.BdeShifts.Remove(shift);
            await _ctx.SaveChangesAsync();
            TempData["SuccessMessage"] = "Schicht entfernt.";
            if (workplaceId.HasValue)
                return RedirectToAction("Edit", "ProductionWorkplaces", new { id = workplaceId.Value });
        }
        return RedirectToAction(nameof(Index));
    }
}
```

- [ ] **Step 3: View anlegen**

`IdealAkeWms/Views/BdeShiftCalendar/Index.cshtml`:

```html
@model List<IdealAkeWms.Models.BdeShift>
@{
    ViewData["Title"] = "Schichtkalender (Default)";
    var days = new[] {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };
    var dayLabels = new Dictionary<DayOfWeek, string> {
        { DayOfWeek.Monday, "Montag" }, { DayOfWeek.Tuesday, "Dienstag" },
        { DayOfWeek.Wednesday, "Mittwoch" }, { DayOfWeek.Thursday, "Donnerstag" },
        { DayOfWeek.Friday, "Freitag" }, { DayOfWeek.Saturday, "Samstag" }, { DayOfWeek.Sunday, "Sonntag" }
    };
}

<h2 class="page-header">Schichtkalender (Default)</h2>
<p class="text-muted">Default-Schichtplan fuer alle Werkbaenke ohne eigenen Plan.</p>

<div class="row">
@foreach (var d in days)
{
    var shiftsOfDay = Model.Where(s => s.DayOfWeek == d).OrderBy(s => s.StartTime).ToList();
    <div class="col-md-6 col-lg-4 mb-3">
        <div class="card">
            <div class="card-header">@dayLabels[d]</div>
            <div class="card-body">
                @if (shiftsOfDay.Count == 0)
                {
                    <p class="text-muted small mb-2">Keine Schichten</p>
                }
                else
                {
                    <ul class="list-unstyled mb-2">
                    @foreach (var s in shiftsOfDay)
                    {
                        <li class="d-flex justify-content-between align-items-center mb-1">
                            <span>
                                @if (!string.IsNullOrWhiteSpace(s.Name)) { <strong>@s.Name</strong> <span class="text-muted">|</span> }
                                @s.StartTime.ToString(@"hh\:mm") &ndash; @s.EndTime.ToString(@"hh\:mm")
                            </span>
                            <form asp-action="Delete" method="post" class="d-inline">
                                @Html.AntiForgeryToken()
                                <input type="hidden" name="id" value="@s.Id" />
                                <button type="submit" class="btn btn-sm btn-outline-danger">&times;</button>
                            </form>
                        </li>
                    }
                    </ul>
                }
                <button type="button" class="btn btn-sm btn-primary" data-bs-toggle="modal" data-bs-target="#add-shift-@d">+ Schicht hinzufuegen</button>
            </div>
        </div>
    </div>

    <div class="modal fade" id="add-shift-@d" tabindex="-1">
        <div class="modal-dialog">
            <form asp-action="Create" method="post">
                @Html.AntiForgeryToken()
                <input type="hidden" name="DayOfWeek" value="@((int)d)" />
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Schicht hinzufuegen — @dayLabels[d]</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="mb-3">
                            <label class="form-label">Bezeichnung (optional)</label>
                            <input name="Name" class="form-control" placeholder="z.B. Frühschicht" />
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Beginn</label>
                            <input name="StartTime" type="time" class="form-control" required />
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Ende</label>
                            <input name="EndTime" type="time" class="form-control" required />
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Abbrechen</button>
                        <button type="submit" class="btn btn-primary">Speichern</button>
                    </div>
                </div>
            </form>
        </div>
    </div>
}
</div>
```

- [ ] **Step 4: BDE-Untermenü um Schichtkalender erweitern**

In `IdealAkeWms/Views/Shared/_Layout.cshtml` finde das BDE-Dropdown (grep nach "BDE"):

```bash
grep -n "BDE\|Cockpit\|Buchungsuebersicht" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Views/Shared/_Layout.cshtml" | head -10
```

Im BDE-Dropdown einen neuen Eintrag ergänzen (Reihenfolge nach Stammdaten):

```html
<li><a class="dropdown-item" asp-controller="BdeShiftCalendar" asp-action="Index">Schichtkalender</a></li>
```

Sichtbarkeit nur wenn User MasterData-Zugriff hat (analog zu BDE-Stammdaten-Eintrag).

- [ ] **Step 5: Tests**

`IdealAkeWms.Tests/Controllers/BdeShiftCalendarControllerTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class BdeShiftCalendarControllerTests
{
    private static BdeShiftCalendarController CreateController(ApplicationDbContext ctx)
    {
        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(u => u.GetDisplayName()).Returns("tester");
        userSvc.Setup(u => u.GetWindowsUserName()).Returns("tester");
        var c = new BdeShiftCalendarController(ctx, userSvc.Object)
        {
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
        };
        return c;
    }

    [Fact]
    public async Task Index_ReturnsOnlyDefaultShifts()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.BdeShifts.Add(new BdeShift {
            DayOfWeek = DayOfWeek.Monday, StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        // Werkbank-Schicht — soll NICHT erscheinen
        var wp = new ProductionWorkplace { Name = "WB", BdeAktiv = true,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.SaveChanges();
        ctx.BdeShifts.Add(new BdeShift {
            DayOfWeek = DayOfWeek.Monday, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(16),
            ProductionWorkplaceId = wp.Id,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);
        var result = await controller.Index() as ViewResult;

        var model = result!.Model as List<BdeShift>;
        model.Should().HaveCount(1);
        model![0].ProductionWorkplaceId.Should().BeNull();
    }

    [Fact]
    public async Task Create_AddsDefaultShift()
    {
        var ctx = TestDbContextFactory.Create();
        var controller = CreateController(ctx);

        var vm = new BdeShiftEditViewModel
        {
            DayOfWeek = DayOfWeek.Tuesday,
            StartTime = TimeSpan.FromHours(6),
            EndTime = TimeSpan.FromHours(14),
            Name = "Frueh"
        };
        var result = await controller.Create(vm);

        result.Should().BeOfType<RedirectToActionResult>();
        ctx.BdeShifts.Count().Should().Be(1);
        ctx.BdeShifts.First().Name.Should().Be("Frueh");
    }

    [Fact]
    public async Task Create_RejectsEndBeforeStart()
    {
        var ctx = TestDbContextFactory.Create();
        var controller = CreateController(ctx);

        var vm = new BdeShiftEditViewModel
        {
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = TimeSpan.FromHours(14),
            EndTime = TimeSpan.FromHours(6)
        };
        await controller.Create(vm);

        ctx.BdeShifts.Count().Should().Be(0);
    }

    [Fact]
    public async Task Delete_RemovesShift()
    {
        var ctx = TestDbContextFactory.Create();
        var s = new BdeShift {
            DayOfWeek = DayOfWeek.Monday, StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.BdeShifts.Add(s);
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);
        await controller.Delete(s.Id);

        ctx.BdeShifts.Count().Should().Be(0);
    }
}
```

- [ ] **Step 6: Build + Tests**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Erwartet: alle Tests grün, 4 neue Controller-Tests dabei.

- [ ] **Step 7: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Controllers/BdeShiftCalendarController.cs IdealAkeWms/Views/BdeShiftCalendar/ IdealAkeWms/Models/ViewModels/BdeShiftEditViewModel.cs IdealAkeWms/Views/Shared/_Layout.cshtml IdealAkeWms.Tests/Controllers/BdeShiftCalendarControllerTests.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): add BdeShiftCalendar UI for default shift plan

New page /BdeShiftCalendar (linked from BDE menu) with 7 weekday
cards. Per card: list of shifts + add-modal. Server-side validation
EndTime > StartTime. Filter [RequireMasterDataAccess]."
```

---

## Task 10: ProductionWorkplaces Edit — Werkbank-Schichtplan-Override

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/ProductionWorkplaceEditViewModel.cs`
- Modify: `IdealAkeWms/Controllers/ProductionWorkplacesController.cs`
- Modify: `IdealAkeWms/Views/ProductionWorkplaces/Edit.cshtml`
- Modify: `IdealAkeWms.Tests/Controllers/ProductionWorkplacesControllerTests.cs`

- [ ] **Step 1: ViewModel-Erweiterung**

In `IdealAkeWms/Models/ViewModels/ProductionWorkplaceEditViewModel.cs` ergänzen:

```csharp
    [Display(Name = "Eigener Schichtplan")]
    public bool BdeUseCustomShiftPlan { get; set; }

    public List<BdeShift> CustomShifts { get; set; } = new();
```

- [ ] **Step 2: Controller — Edit GET lädt Custom-Shifts**

In `IdealAkeWms/Controllers/ProductionWorkplacesController.cs` Edit-GET erweitern:

```csharp
        vm.BdeUseCustomShiftPlan = workplace.BdeUseCustomShiftPlan;
        vm.CustomShifts = await _ctx.BdeShifts
            .Where(s => s.ProductionWorkplaceId == workplace.Id)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .ToListAsync();
```

(`_ctx` muss verfügbar sein — falls nicht, Konstruktor um `ApplicationDbContext` ergänzen.)

- [ ] **Step 3: Controller — Edit POST persistiert Toggle**

In Edit-POST nach den BDE-Settings-Field-Updates:

```csharp
existing.BdeUseCustomShiftPlan = vm.BdeUseCustomShiftPlan;
```

- [ ] **Step 4: View-Erweiterung**

In `IdealAkeWms/Views/ProductionWorkplaces/Edit.cshtml` unter dem existierenden BDE-Settings-Block:

```html
<div class="card mt-3">
    <div class="card-header">Schichtplan (BDE)</div>
    <div class="card-body">
        <div class="form-check form-switch mb-3">
            <input asp-for="BdeUseCustomShiftPlan" class="form-check-input" role="switch" />
            <label asp-for="BdeUseCustomShiftPlan" class="form-check-label"></label>
            <div class="form-text">
                Wenn aktiv: nur die unten konfigurierten Schichten gelten fuer diese Werkbank.
                Wenn aus: Default-Schichtkalender wird verwendet.
            </div>
        </div>

        <div id="custom-shift-editor" class="@(Model.BdeUseCustomShiftPlan ? "" : "d-none")">
            @{
                var days = new[] {
                    DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
                    DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
                };
                var dayLabels = new Dictionary<DayOfWeek, string> {
                    { DayOfWeek.Monday, "Mo" }, { DayOfWeek.Tuesday, "Di" },
                    { DayOfWeek.Wednesday, "Mi" }, { DayOfWeek.Thursday, "Do" },
                    { DayOfWeek.Friday, "Fr" }, { DayOfWeek.Saturday, "Sa" }, { DayOfWeek.Sunday, "So" }
                };
            }
            @if (Model.CustomShifts.Count == 0)
            {
                <p class="text-warning small">Keine Schichten konfiguriert &mdash; keine automatische Pausierung fuer diese Werkbank.</p>
            }
            else
            {
                <table class="table table-sm">
                    <thead><tr><th>Tag</th><th>Beginn</th><th>Ende</th><th>Bezeichnung</th><th></th></tr></thead>
                    <tbody>
                    @foreach (var s in Model.CustomShifts)
                    {
                        <tr>
                            <td>@dayLabels[s.DayOfWeek]</td>
                            <td>@s.StartTime.ToString(@"hh\:mm")</td>
                            <td>@s.EndTime.ToString(@"hh\:mm")</td>
                            <td>@s.Name</td>
                            <td>
                                <form asp-controller="BdeShiftCalendar" asp-action="Delete" method="post" class="d-inline">
                                    @Html.AntiForgeryToken()
                                    <input type="hidden" name="id" value="@s.Id" />
                                    <button type="submit" class="btn btn-sm btn-outline-danger">&times;</button>
                                </form>
                            </td>
                        </tr>
                    }
                    </tbody>
                </table>
            }

            <button type="button" class="btn btn-sm btn-primary" data-bs-toggle="modal" data-bs-target="#add-custom-shift">+ Schicht hinzufuegen</button>

            <div class="modal fade" id="add-custom-shift" tabindex="-1">
                <div class="modal-dialog">
                    <form asp-controller="BdeShiftCalendar" asp-action="Create" method="post">
                        @Html.AntiForgeryToken()
                        <input type="hidden" name="ProductionWorkplaceId" value="@Model.Id" />
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title">Schicht hinzufuegen</h5>
                                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                            </div>
                            <div class="modal-body">
                                <div class="mb-3">
                                    <label class="form-label">Wochentag</label>
                                    <select name="DayOfWeek" class="form-select" required>
                                        @foreach (var d in days)
                                        {
                                            <option value="@((int)d)">@dayLabels[d]</option>
                                        }
                                    </select>
                                </div>
                                <div class="mb-3">
                                    <label class="form-label">Bezeichnung (optional)</label>
                                    <input name="Name" class="form-control" />
                                </div>
                                <div class="mb-3">
                                    <label class="form-label">Beginn</label>
                                    <input name="StartTime" type="time" class="form-control" required />
                                </div>
                                <div class="mb-3">
                                    <label class="form-label">Ende</label>
                                    <input name="EndTime" type="time" class="form-control" required />
                                </div>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Abbrechen</button>
                                <button type="submit" class="btn btn-primary">Speichern</button>
                            </div>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    </div>
</div>

<script>
    document.addEventListener('DOMContentLoaded', () => {
        const toggle = document.getElementById('@Html.IdFor(m => m.BdeUseCustomShiftPlan)');
        const editor = document.getElementById('custom-shift-editor');
        if (toggle && editor) {
            toggle.addEventListener('change', () => {
                editor.classList.toggle('d-none', !toggle.checked);
            });
        }
    });
</script>
```

- [ ] **Step 5: Test ergänzen**

In `IdealAkeWms.Tests/Controllers/ProductionWorkplacesControllerTests.cs`:

```csharp
    [Fact]
    public async Task Edit_PersistsBdeUseCustomShiftPlan()
    {
        var ctx = TestDbContextFactory.Create();
        var wp = new ProductionWorkplace {
            Name = "WB", BdeAktiv = true, BdeUseCustomShiftPlan = false,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);
        var vm = new ProductionWorkplaceEditViewModel
        {
            Id = wp.Id, Name = wp.Name, BdeAktiv = true, BdeUseCustomShiftPlan = true
        };
        await controller.Edit(wp.Id, vm);

        var updated = await ctx.ProductionWorkplaces.FindAsync(wp.Id);
        updated!.BdeUseCustomShiftPlan.Should().BeTrue();
    }
```

(Falls `CreateController`-Helper noch nicht existiert: aus dem Pattern in den anderen Controller-Tests übernehmen, evtl. Konstruktor-Erweiterung um `ApplicationDbContext` mitziehen.)

- [ ] **Step 6: Build + Tests + Commit**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5

git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/ IdealAkeWms.Tests/
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): per-workbench shift plan override in workplace edit form

ProductionWorkplaceEditViewModel gets BdeUseCustomShiftPlan + CustomShifts.
Edit view shows toggle and embedded shift editor (delegates to
BdeShiftCalendarController.Create/Delete with workplaceId)."
```

---

## Task 11: SQL-Scripts + Docs + AppVersion + TESTSZENARIEN.md

**Files:**
- Create: `SQL/48_AddBdeShiftCalendar.sql`
- Modify: `SQL/00_FreshInstall.sql`
- Modify: `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs`
- Modify: `CLAUDE.md`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `PROJECT_STATUS.md`
- Modify: `docs/TESTSZENARIEN.md`

- [ ] **Step 1: Migration-ID auslesen**

```bash
ls "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Migrations/" | grep -i AddBdeShiftCalendar
```

Timestamp-Präfix merken (z.B. `20260428...`).

- [ ] **Step 2: `SQL/48_AddBdeShiftCalendar.sql` anlegen**

Inhalt:

```sql
-- Add BdeShifts table + Holiday.Source + ProductionWorkplaces.BdeUseCustomShiftPlan
-- Update CK_BdeBookings_StatusEnded to include AutoPaused (Status=5)
-- Idempotent.

IF COL_LENGTH('dbo.ProductionWorkplaces', 'BdeUseCustomShiftPlan') IS NULL
BEGIN
    ALTER TABLE dbo.ProductionWorkplaces
        ADD BdeUseCustomShiftPlan bit NOT NULL CONSTRAINT DF_ProductionWorkplaces_BdeUseCustomShiftPlan DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.Holidays', 'Source') IS NULL
BEGIN
    ALTER TABLE dbo.Holidays
        ADD Source tinyint NOT NULL CONSTRAINT DF_Holidays_Source DEFAULT (1);
END
GO

IF OBJECT_ID('dbo.BdeShifts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BdeShifts (
        Id int IDENTITY(1,1) PRIMARY KEY,
        DayOfWeek int NOT NULL,
        StartTime time NOT NULL,
        EndTime time NOT NULL,
        ProductionWorkplaceId int NULL,
        Name nvarchar(50) NULL,
        CreatedAt datetime2 NOT NULL,
        CreatedBy nvarchar(450) NOT NULL,
        CreatedByWindows nvarchar(450) NOT NULL,
        ModifiedAt datetime2 NULL,
        ModifiedBy nvarchar(450) NULL,
        ModifiedByWindows nvarchar(450) NULL,
        CONSTRAINT FK_BdeShifts_ProductionWorkplaces FOREIGN KEY (ProductionWorkplaceId)
            REFERENCES dbo.ProductionWorkplaces(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_BdeShifts_Workplace_Day
        ON dbo.BdeShifts (ProductionWorkplaceId, DayOfWeek);
END
GO

-- CHECK-Constraint erweitern
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_BdeBookings_StatusEnded')
BEGIN
    ALTER TABLE dbo.BdeBookings DROP CONSTRAINT CK_BdeBookings_StatusEnded;
END
GO

ALTER TABLE dbo.BdeBookings ADD CONSTRAINT CK_BdeBookings_StatusEnded
    CHECK (([Status] = 1 AND [EndedAt] IS NULL) OR ([Status] IN (2,3,4,5) AND [EndedAt] IS NOT NULL));
GO

-- AppSettings-Seed
IF NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = 'BdeSchichtkalenderAktiv')
    INSERT INTO dbo.AppSettings ([Key], Value, Description, CreatedAt, CreatedBy, CreatedByWindows)
    VALUES ('BdeSchichtkalenderAktiv', 'false', 'Schichtkalender + Auto-Pause am Schichtende aktiv',
            SYSDATETIME(), 'System', 'System');
GO

-- EFMigrationsHistory
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId LIKE '%_AddBdeShiftCalendar')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('<MIGRATION_ID>_AddBdeShiftCalendar', '10.0.2');
END
GO
```

`<MIGRATION_ID>` durch den Timestamp aus Step 1 ersetzen. ProductVersion ggf. an aktuelle Snapshot-Version anpassen (grep `ProductVersion` in `ApplicationDbContextModelSnapshot.cs`).

- [ ] **Step 3: `SQL/00_FreshInstall.sql` aktualisieren**

```bash
grep -n "CK_BdeBookings_StatusEnded\|CREATE TABLE.*ProductionWorkplaces\|CREATE TABLE.*Holidays" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/SQL/00_FreshInstall.sql"
```

Anpassungen:
- CHECK-Statement: `IN (2,3,4)` → `IN (2,3,4,5)`
- `Holidays`-CREATE TABLE: `[Source] tinyint NOT NULL DEFAULT (1)` ergänzen
- `ProductionWorkplaces`-CREATE TABLE: `[BdeUseCustomShiftPlan] bit NOT NULL DEFAULT (0)` ergänzen
- Neue `CREATE TABLE BdeShifts ...` an passender Stelle (nach den anderen Bde*-Tabellen)
- Index `IX_BdeShifts_Workplace_Day`
- Seed der neuen AppSetting `BdeSchichtkalenderAktiv`

- [ ] **Step 4: AppVersion.cs Date-Bump**

In `IdealAkeWms/AppVersion.cs` und `IDEALAKEWMSService/AppVersion.cs`:

```csharp
public const string Date = "2026-04-28";
```

(Version bleibt `1.8.2` — kein Versions-Bump.)

- [ ] **Step 5: CLAUDE.md aktualisieren**

- AppSettings-Tabelle: neue Zeile `BdeSchichtkalenderAktiv`
- Service-Konfiguration-Tabelle: `Sync:BdeAutoPauseIntervalMinutes`, `Sync:FeiertagSync*` Einträge
- Status-Enum-Liste (falls vorhanden): `AutoPaused = 5` ergänzen
- Bekannte Fallstricke: neuer Eintrag "Auto-Pause setzt EndedAt = exaktes Schichtende, nicht DateTime.Now"

- [ ] **Step 6: Help/Index.cshtml — neue BDE-Subsections**

Im BDE-Abschnitt drei neue Blöcke:

```html
<h6 class="mt-3">Schichtkalender konfigurieren</h6>
<ol>
    <li>Einstellungen &rarr; BDE-Gruppe: Toggle "Schichtkalender + Auto-Pause" aktivieren.</li>
    <li>Menue BDE &rarr; Schichtkalender: Default-Plan pro Wochentag hinzufuegen.</li>
    <li>Optional pro Werkbank: Stammdaten &rarr; Werkbaenke &rarr; Bearbeiten &rarr; Schichtplan-Bereich. Toggle "Eigener Schichtplan" + Schichten anlegen.</li>
</ol>

<h6 class="mt-3">Auto-Pause am Schichtende</h6>
<p>Wenn der Schichtkalender aktiv ist, pausiert der BDE-Worker laufende Buchungen am Schichtende automatisch (Status "Auto-pausiert"). Die Buchung wird mit der exakten Schicht-Ende-Zeit als Ende markiert &mdash; unabhaengig davon, wann der Service tatsaechlich tickt. Mitarbeiter sehen am Folge-Tag im Paused-Hint "auto-pausiert seit ... (Schichtende)" und koennen mit "Fortsetzen" eine neue Buchung starten. Activity- und Setup-Buchungen werden gleichbehandelt wie Production.</p>

<h6 class="mt-3">Feiertags-Sync (Nager.Date)</h6>
<p>Der Service kann Feiertage automatisch von date.nager.at synchronisieren (Service-Settings: Toggle aktivieren, optional Bundesland AT-3 NOe / AT-6 Stmk / AT-4 OOe / AT-5 Sbg / AT-7 T / AT-8 V / AT-9 W / AT-1 Bgl / AT-2 K). Der Sync ist additiv &mdash; manuelle Eintraege werden nie ueberschrieben. Sync-Eintraege haben Quelle "Sync"; manuelle Eintraege bleiben "Manuell".</p>
```

- [ ] **Step 7: Changelog.cshtml**

Im v1.8.2-Block neuer Phase-2.3-Abschnitt:

```html
<h6>BDE Phase 2.3 &mdash; Schichtkalender + Auto-Pause + Feiertags-Sync</h6>
<ul>
    <li>Neuer Schichtkalender-Editor (Default + Werkbank-Override) unter BDE &rarr; Schichtkalender.</li>
    <li>Auto-Pause-Worker: laufende Buchungen werden am Schichtende automatisch mit Status "Auto-pausiert" beendet.</li>
    <li>Feiertags-Sync von date.nager.at (additiv, manuelle Eintraege bleiben).</li>
    <li>Neuer Buchungs-Status "Auto-pausiert" (5); Resume akzeptiert ihn analog zu manueller Pause.</li>
    <li>Master-Toggle <code>BdeSchichtkalenderAktiv</code> (Default aus) &mdash; Phase-2.2-Verhalten unveraendert wenn nicht aktiviert.</li>
</ul>
```

- [ ] **Step 8: PROJECT_STATUS.md**

Phase 2.3 als abgeschlossen eintragen mit Bullet-Liste der Hauptfeatures.

- [ ] **Step 9: TESTSZENARIEN.md erweitern — neuer Bereich 15**

In `docs/TESTSZENARIEN.md`:

```markdown
## 15. BDE Phase 2.3 — Schichtkalender + Auto-Pause

### TS-15.1 — Default-Schichtkalender anlegen
**Vorbedingungen:** Admin-Login, `BdeSchichtkalenderAktiv = true`.
**Schritte:**
1. Menue BDE -> Schichtkalender oeffnen.
2. Auf "Montag"-Karte "+ Schicht hinzufuegen" klicken.
3. Eingabe: Name "Frueh", Beginn 06:00, Ende 14:00, speichern.
**Erwartet:** Schicht erscheint in der Mo-Karte; Erfolgs-Toast.
**Negativ:** Beginn 14:00 + Ende 06:00 -> Validation-Hinweis "Ende muss nach dem Beginn liegen".

### TS-15.2 — Werkbank-Override aktivieren
**Vorbedingungen:** Default-Kalender vorhanden, Werkbank "A1" angelegt.
**Schritte:**
1. Stammdaten -> Werkbaenke -> A1 -> Bearbeiten.
2. Im "Schichtplan (BDE)"-Card Toggle "Eigener Schichtplan" einschalten.
3. "+ Schicht hinzufuegen", Wochentag/Beginn/Ende eingeben.
4. Speichern.
**Erwartet:** Werkbank zeigt eigenen Plan; Default wird ignoriert fuer A1.

### TS-15.3 — Auto-Pause greift am Schichtende
**Vorbedingungen:** Default Mo-Fr 06-14, Master-Toggle aktiv, MA scannt um 13:50 ein.
**Schritte:**
1. Buchung startet 13:50.
2. Worker tickt nach 14:00 (max. 1h Latenz).
**Erwartet:** Buchung Status=AutoPaused, EndedAt=14:00. ModifiedBy="BDE-AutoPause".

### TS-15.4 — Resume nach Auto-Pause
**Vorbedingungen:** TS-15.3 ausgefuehrt, MA scannt am Folge-Tag.
**Schritte:**
1. Operator-Scan im Terminal.
**Erwartet:** Paused-Hint zeigt "auto-pausiert seit ... (Schichtende)" mit Fortsetzen-Button.
2. Klick Fortsetzen.
**Erwartet:** Neue Running-Buchung mit ParentBookingId; Parent Status=Resumed.

### TS-15.5 — Feiertag schuetzt vor Auto-Pause
**Vorbedingungen:** Holiday-Eintrag fuer heutigen Tag, Master-Toggle aktiv.
**Schritte:** Buchung laeuft, Worker tickt.
**Erwartet:** Buchung wird NICHT pausiert (Holiday gilt als arbeitsfrei).

### TS-15.6 — Master-Toggle aus
**Vorbedingungen:** `BdeSchichtkalenderAktiv = false`.
**Schritte:** Buchung ueber Schichtende laufen lassen, Worker tickt.
**Erwartet:** Buchung bleibt unveraendert. Phase-2.2-Verhalten.

### TS-15.7 — Buchung startet vor Schichtbeginn
**Vorbedingungen:** Frueh 06-14, MA stempelt um 04:00 ein.
**Schritte:** Worker tickt nach 14:00.
**Erwartet:** Buchung pausiert mit EndedAt=14:00 (P2-Logik).

### TS-15.8 — Buchung startet nach allen Tagesschichten
**Vorbedingungen:** Frueh 06-14, MA stempelt um 23:00 ein.
**Schritte:** Worker tickt naechsten Tag.
**Erwartet:** Buchung NICHT pausiert (kein Schichtende mehr fuer den Tag der StartedAt).

### TS-15.9 — Werkbank Override leer = 24/7 frei
**Vorbedingungen:** Werkbank-Toggle EIN, 0 eigene Schichten konfiguriert.
**Schritte:** Buchung laeuft, Worker tickt.
**Erwartet:** Keine Auto-Pause (Override-Schalter zieht).

### TS-15.10 — Mehrschicht-Uebergang (Frueh -> Spaet)
**Vorbedingungen:** Default Mo Frueh 06-14 + Spaet 14-22.
**Schritte:** MA1 startet 13:50, Worker tickt nach 14:00.
**Erwartet:** MA1-Buchung pausiert mit EndedAt=14:00. MA2 (Spaet) muss neu scannen + Resume.

### TS-15.11 — Feiertags-Sync (Nager.Date)
**Vorbedingungen:** Service-Settings `FeiertagSyncEnabled=true`, CountryCode=AT, Region=AT-3, JahreVoraus=2.
**Schritte:** Service starten.
**Erwartet:** Holidays-Tabelle enthaelt nationale + AT-3-Eintraege fuer aktuelles + 2 Folgejahre. Source=NagerSync.

### TS-15.12 — Manuelle Holiday-Eintraege werden nicht ueberschrieben
**Vorbedingungen:** Manueller Holiday-Eintrag fuer 06.01., Sync-Eintrag fuer dasselbe Datum noch nicht vorhanden.
**Schritte:** Sync starten.
**Erwartet:** Manueller Eintrag bleibt. Source weiterhin Manual. Description unveraendert.
```

- [ ] **Step 10: Build + Final Test + Commit**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -10
```

Erwartet: alle Tests grün, ~38 neue Tests dabei. Insgesamt > 460 Tests.

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add SQL/ IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs CLAUDE.md IdealAkeWms/Views/Help/ PROJECT_STATUS.md docs/TESTSZENARIEN.md
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "chore(bde): Phase 2.3 SQL + Docs + Version date + 12 test scenarios

SQL/48_AddBdeShiftCalendar.sql idempotent. FreshInstall updated.
AppVersion date 2026-04-28. CLAUDE.md AppSettings + Service-Settings
+ Status-Enum updated. Help section gets Schichtkalender, Auto-Pause,
Feiertags-Sync subsections. Changelog Phase-2.3 block under v1.8.2.
PROJECT_STATUS Phase 2.3 entry. TESTSZENARIEN.md new TS-15.1..15.12."
```

---

## Final Summary

Nach allen 11 Tasks: ~38 neue automatisierte Tests, 12 manuelle Testszenarien, 1 EF-Migration, 1 SQL-Script, 2 neue Worker-Services, 1 neuer User-Service, 1 neue Page, Werkbank-Edit-Erweiterung, Settings-UI-Erweiterung, Doku-Updates.

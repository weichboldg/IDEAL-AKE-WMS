# Leitstand — Kommissionier-Freigabe & Priorisierung — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Leitstand-User geben Produktionsaufträge zur Kommissionierung frei und priorisieren sie. Kommissionierer sehen nur freigegebene Aufträge. Feature per AppSetting `LeitstandAktiv` aktivierbar.

**Architecture:** 4 neue Spalten auf `ProductionOrders`, neue Rolle `leitstand`, neuer Action-Filter. WA-Liste wird um Freigabe-Spalte erweitert (Toggle-abhängig). Kommissionierung-View wird von Dropdown auf Tabelle umgebaut (Toggle-abhängig). Bisheriges Verhalten bleibt bei deaktiviertem Toggle erhalten.

**Tech Stack:** ASP.NET Core 10.0, EF Core 10.0, SQL Server, Bootstrap 5, jQuery AJAX

**Spec:** `docs/superpowers/specs/2026-04-03-leitstand-design.md`

---

## File Structure

### Neue Dateien

| Datei | Verantwortung |
|-------|---------------|
| `SQL/37_AddPickingRelease.sql` | DB-Migration: 4 Spalten, Index, Rolle, AppSetting |
| `IdealAkeWms/Filters/RequireLeitstandAccessAttribute.cs` | Action-Filter für `CanManagePickingReleaseAsync` |
| `IdealAkeWms/Models/ViewModels/PickingListViewModel.cs` | ViewModel für neue Kommissionierliste |
| `IdealAkeWms/Views/ProductionOrders/PickingDropdown.cshtml` | Kopie der alten Picking-View (Fallback) |

### Geänderte Dateien

| Datei | Änderung |
|-------|----------|
| `IdealAkeWms/Models/ProductionOrder.cs` | +4 Properties |
| `IdealAkeWms/Models/RoleKeys.cs` | +1 Konstante |
| `IdealAkeWms/Services/ICurrentUserService.cs` | +1 Methode |
| `IdealAkeWms/Services/CurrentUserService.cs` | +1 Implementierung |
| `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs` | +2 Methoden |
| `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs` | +2 Implementierungen |
| `IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs` | +5 Properties |
| `IdealAkeWms/Controllers/ProductionOrdersController.cs` | Index erweitern, Picking überarbeiten, +3 Actions |
| `IdealAkeWms/Views/ProductionOrders/Index.cshtml` | Titel bedingt, Freigabe-Spalte |
| `IdealAkeWms/Views/ProductionOrders/Picking.cshtml` | Komplett neu: Tabelle mit freigegebenen Aufträgen |
| `IdealAkeWms/Views/Shared/_Layout.cshtml` | Menü-Logik + Badge |
| `IdealAkeWms/Program.cs` | AppSetting `LeitstandAktiv` seeden |
| `SQL/00_FreshInstall.sql` | Konsolidierung |
| `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` | Kommentar erweitern |

---

## Task 1: DB-Schema + Model + EF-Migration

**Files:**
- Create: `SQL/37_AddPickingRelease.sql`
- Modify: `IdealAkeWms/Models/ProductionOrder.cs`
- Modify: `IdealAkeWms/Program.cs:187-203`
- Modify: `SQL/00_FreshInstall.sql`
- Modify: `SQL/AgentJobs/01_Import_Produktionsauftraege.sql:17-24`

- [ ] **Step 1: SQL-Migration erstellen**

Datei `SQL/37_AddPickingRelease.sql`:

```sql
-- =============================================
-- 37_AddPickingRelease.sql
-- Leitstand: Kommissionier-Freigabe + Priorisierung
-- =============================================

-- Neue Spalten auf ProductionOrders
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = 'IsReleasedForPicking')
    ALTER TABLE [dbo].[ProductionOrders] ADD [IsReleasedForPicking] BIT NOT NULL DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = 'PickingPriority')
    ALTER TABLE [dbo].[ProductionOrders] ADD [PickingPriority] INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = 'ReleasedAt')
    ALTER TABLE [dbo].[ProductionOrders] ADD [ReleasedAt] DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = 'ReleasedBy')
    ALTER TABLE [dbo].[ProductionOrders] ADD [ReleasedBy] NVARCHAR(200) NULL;
GO

-- Performance-Index fuer Kommissionierliste
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_PickingRelease')
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_PickingRelease]
        ON [dbo].[ProductionOrders]([IsReleasedForPicking], [IsDone])
        INCLUDE ([PickingPriority], [OrderNumber], [ArticleNumber], [Customer], [ProductionDate], [PickingStatus]);
GO

-- Neue Rolle: Leitstand
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'leitstand')
    INSERT INTO [dbo].[Roles] ([Key], [Name], [Description], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('leitstand', 'Leitstand', 'Produktionsauftraege freigeben und Kommissionier-Prioritaeten verwalten', GETUTCDATE(), 'system', 'SYSTEM');
GO

-- AppSetting: Leitstand-Feature
IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = 'LeitstandAktiv')
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES ('LeitstandAktiv', 'false', 'Leitstand-Funktion: Kommissionier-Freigabe und Priorisierung aktivieren');
GO

-- EF Migrations-History
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] LIKE '%_AddPickingRelease')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (CONVERT(VARCHAR(14), GETDATE(), 112) + REPLACE(CONVERT(VARCHAR(8), GETDATE(), 108), ':', '') + '_AddPickingRelease', '10.0.0');
GO
```

- [ ] **Step 2: ProductionOrder Model erweitern**

In `IdealAkeWms/Models/ProductionOrder.cs`, nach dem `ProductionWorkplace`-Navigation Property, vor der schließenden Klammer:

```csharp
// Leitstand: Kommissionier-Freigabe
[Display(Name = "Freigegeben")]
public bool IsReleasedForPicking { get; set; }

[Display(Name = "Priorität")]
public int? PickingPriority { get; set; }

[Display(Name = "Freigegeben am")]
public DateTime? ReleasedAt { get; set; }

[StringLength(200)]
[Display(Name = "Freigegeben von")]
public string? ReleasedBy { get; set; }
```

- [ ] **Step 3: Program.cs — LeitstandAktiv seeden**

In `IdealAkeWms/Program.cs`, nach dem `BestellungenAktiv`-Seeding-Block (nach Zeile 203), einfügen:

```csharp
// Leitstand AppSettings
if (!db.AppSettings.Any(s => s.Key == "LeitstandAktiv"))
{
    db.AppSettings.Add(new IdealAkeWms.Models.AppSetting
    {
        Key = "LeitstandAktiv",
        Value = "false",
        Description = "Leitstand-Funktion: Kommissionier-Freigabe und Priorisierung aktivieren"
    });
    db.SaveChanges();
}
```

- [ ] **Step 4: EF-Migration generieren**

Run: `dotnet ef migrations add AddPickingRelease --project IdealAkeWms`

- [ ] **Step 5: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: 0 errors

- [ ] **Step 6: SQL/00_FreshInstall.sql aktualisieren**

In der `CREATE TABLE ProductionOrders` Sektion (nach `[HasExternalPurchase]` ca. Zeile 223):

```sql
        [IsReleasedForPicking]    BIT               NOT NULL DEFAULT 0,
        [PickingPriority]         INT               NULL,
        [ReleasedAt]              DATETIME2         NULL,
        [ReleasedBy]              NVARCHAR(200)     NULL,
```

In der Index-Sektion (nach `IX_ProductionOrders_IsDone`):

```sql
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_PickingRelease')
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_PickingRelease]
        ON [dbo].[ProductionOrders]([IsReleasedForPicking], [IsDone])
        INCLUDE ([PickingPriority], [OrderNumber], [ArticleNumber], [Customer], [ProductionDate], [PickingStatus]);
```

In der Rollen-Seeding-Sektion:

```sql
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'leitstand')
    INSERT INTO [dbo].[Roles] ([Key], [Name], [Description], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('leitstand', 'Leitstand', 'Produktionsauftraege freigeben und Kommissionier-Prioritaeten verwalten', GETUTCDATE(), 'system', 'SYSTEM');
```

In der AppSettings-Seeding-Sektion:

```sql
IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = 'LeitstandAktiv')
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES ('LeitstandAktiv', 'false', 'Leitstand-Funktion: Kommissionier-Freigabe und Priorisierung aktivieren');
```

- [ ] **Step 7: Agent-Job Kommentar erweitern**

In `SQL/AgentJobs/01_Import_Produktionsauftraege.sql`, Zeile 18:

```sql
--   IsDone, PickingStatus, HasGlass, HasExternalPurchase,
--   IsReleasedForPicking, PickingPriority, ReleasedAt, ReleasedBy
```

- [ ] **Step 8: Commit**

```bash
git add SQL/37_AddPickingRelease.sql SQL/00_FreshInstall.sql SQL/AgentJobs/ IdealAkeWms/Models/ProductionOrder.cs IdealAkeWms/Program.cs IdealAkeWms/Migrations/
git commit -m "feat: add picking release schema — 4 columns, index, leitstand role, LeitstandAktiv setting"
```

---

## Task 2: Rolle + Berechtigung + Filter

**Files:**
- Modify: `IdealAkeWms/Models/RoleKeys.cs`
- Modify: `IdealAkeWms/Services/ICurrentUserService.cs`
- Modify: `IdealAkeWms/Services/CurrentUserService.cs`
- Create: `IdealAkeWms/Filters/RequireLeitstandAccessAttribute.cs`

- [ ] **Step 1: RoleKeys erweitern**

In `IdealAkeWms/Models/RoleKeys.cs`, nach `public const string Reporting = "reporting";`:

```csharp
public const string Leitstand = "leitstand";
```

- [ ] **Step 2: ICurrentUserService erweitern**

In `IdealAkeWms/Services/ICurrentUserService.cs`, nach `Task<bool> CanTransferStockAsync();`:

```csharp
Task<bool> CanManagePickingReleaseAsync();
```

- [ ] **Step 3: CurrentUserService implementieren**

In `IdealAkeWms/Services/CurrentUserService.cs`, nach `CanTransferStockAsync` (Zeile 94):

```csharp
public async Task<bool> CanManagePickingReleaseAsync()
    => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Leitstand);
```

- [ ] **Step 4: RequireLeitstandAccessAttribute erstellen**

Datei `IdealAkeWms/Filters/RequireLeitstandAccessAttribute.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

public class RequireLeitstandAccessAttribute : TypeFilterAttribute
{
    public RequireLeitstandAccessAttribute() : base(typeof(RequireLeitstandAccessFilter)) { }
}

public class RequireLeitstandAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireLeitstandAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanManagePickingReleaseAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
```

- [ ] **Step 5: Build + Tests**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj && dotnet test IdealAkeWms.Tests`
Expected: 0 errors, all tests pass

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms/Models/RoleKeys.cs IdealAkeWms/Services/ IdealAkeWms/Filters/RequireLeitstandAccessAttribute.cs
git commit -m "feat: add leitstand role key, CanManagePickingReleaseAsync, RequireLeitstandAccess filter"
```

---

## Task 3: Repository — Freigegebene Aufträge + Badge-Count

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs`
- Modify or Create: `IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs`

- [ ] **Step 1: Interface erweitern**

In `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs`, nach `SearchAsync`:

```csharp
Task<List<ProductionOrder>> GetReleasedForPickingAsync();
Task<int> GetReleasedForPickingCountAsync();
```

- [ ] **Step 2: GetReleasedForPickingAsync implementieren**

In `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs`:

```csharp
public async Task<List<ProductionOrder>> GetReleasedForPickingAsync()
{
    return await _dbSet
        .Where(o => o.IsReleasedForPicking && !o.IsDone)
        .OrderBy(o => o.PickingPriority.HasValue ? 0 : 1)
        .ThenBy(o => o.PickingPriority)
        .ThenBy(o => o.ProductionDate)
        .ToListAsync();
}

public async Task<int> GetReleasedForPickingCountAsync()
{
    return await _dbSet.CountAsync(o => o.IsReleasedForPicking && !o.IsDone);
}
```

- [ ] **Step 3: Tests schreiben**

```csharp
[Fact]
public async Task GetReleasedForPickingAsync_ReturnsOnlyReleasedAndNotDone()
{
    using var context = TestDbContextFactory.CreateContext();
    context.ProductionOrders.AddRange(
        new ProductionOrder { OrderNumber = "WA-1", IsReleasedForPicking = true, IsDone = false, PickingPriority = 2, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
        new ProductionOrder { OrderNumber = "WA-2", IsReleasedForPicking = true, IsDone = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
        new ProductionOrder { OrderNumber = "WA-3", IsReleasedForPicking = false, IsDone = false, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
        new ProductionOrder { OrderNumber = "WA-4", IsReleasedForPicking = true, IsDone = false, PickingPriority = 1, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
    );
    await context.SaveChangesAsync();

    var repo = new ProductionOrderRepository(context);
    var result = await repo.GetReleasedForPickingAsync();

    result.Should().HaveCount(2);
    result[0].OrderNumber.Should().Be("WA-4"); // Prio 1 zuerst
    result[1].OrderNumber.Should().Be("WA-1"); // Prio 2 danach
}

[Fact]
public async Task GetReleasedForPickingCountAsync_CountsCorrectly()
{
    using var context = TestDbContextFactory.CreateContext();
    context.ProductionOrders.AddRange(
        new ProductionOrder { OrderNumber = "WA-1", IsReleasedForPicking = true, IsDone = false, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
        new ProductionOrder { OrderNumber = "WA-2", IsReleasedForPicking = true, IsDone = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
        new ProductionOrder { OrderNumber = "WA-3", IsReleasedForPicking = false, IsDone = false, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
    );
    await context.SaveChangesAsync();

    var repo = new ProductionOrderRepository(context);
    var count = await repo.GetReleasedForPickingCountAsync();

    count.Should().Be(1);
}
```

- [ ] **Step 4: Tests ausführen**

Run: `dotnet test IdealAkeWms.Tests --filter "ProductionOrderRepository"`
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/Data/Repositories/ IdealAkeWms.Tests/
git commit -m "feat: add GetReleasedForPickingAsync + count with tests"
```

---

## Task 4: ViewModels

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs`
- Create: `IdealAkeWms/Models/ViewModels/PickingListViewModel.cs`

- [ ] **Step 1: ProductionOrderViewModel erweitern**

In `ProductionOrderViewModel`, nach `CanPick`:

```csharp
public bool CanManagePickingRelease { get; set; }
public bool LeitstandAktiv { get; set; }
```

In `ProductionOrderViewItem`, nach `WorkplaceName`:

```csharp
public bool IsReleasedForPicking { get; set; }
public int? PickingPriority { get; set; }
public DateTime? ReleasedAt { get; set; }
public string? ReleasedBy { get; set; }
```

- [ ] **Step 2: PickingListViewModel erstellen**

Datei `IdealAkeWms/Models/ViewModels/PickingListViewModel.cs`:

```csharp
namespace IdealAkeWms.Models.ViewModels;

public class PickingListViewModel
{
    public List<PickingListItem> Items { get; set; } = new();
}

public class PickingListItem
{
    public int Id { get; set; }
    public int? PickingPriority { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public string? Customer { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? KommissionierTermin { get; set; }
    public string? PickingStatus { get; set; }
}
```

- [ ] **Step 3: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Models/ViewModels/
git commit -m "feat: add PickingListViewModel, extend ProductionOrderViewModel with release fields"
```

---

## Task 5: Controller — Index erweitern, Picking überarbeiten, neue Actions

**Files:**
- Modify: `IdealAkeWms/Controllers/ProductionOrdersController.cs`

- [ ] **Step 1: Index-Action — Zugriffsprüfung erweitern + Release-Felder mappen**

Zeile 69: `[RequirePickingOrTrackingAccess]` entfernen und durch manuelle Prüfung im Action-Body ersetzen. Am Anfang der Methode:

```csharp
// Zugriff: Picking, Tracking oder Leitstand
if (!await _currentUserService.CanPickAsync()
    && !await _currentUserService.CanViewTrackingAsync()
    && !await _currentUserService.CanManagePickingReleaseAsync())
{
    return RedirectToAction("AccessDenied", "Account");
}
```

Im ViewItem-Mapping (nach `WorkplaceName = o.ProductionWorkplace?.Name`, Zeile 122):

```csharp
IsReleasedForPicking = o.IsReleasedForPicking,
PickingPriority = o.PickingPriority,
ReleasedAt = o.ReleasedAt,
ReleasedBy = o.ReleasedBy,
```

Im ViewModel-Constructor (nach `CanPick`, Zeile 154):

```csharp
CanManagePickingRelease = await _currentUserService.CanManagePickingReleaseAsync(),
LeitstandAktiv = (await _settingRepository.GetValueAsync("LeitstandAktiv"))
    ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true,
```

- [ ] **Step 2: Picking-Action überarbeiten**

Bestehende Methode (Zeilen 63-67) komplett ersetzen:

```csharp
[RequirePickingAccess]
public async Task<IActionResult> Picking()
{
    var leitstandAktiv = (await _settingRepository.GetValueAsync("LeitstandAktiv"))
        ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

    if (!leitstandAktiv)
        return View("PickingDropdown");

    var releasedOrders = await _productionOrderRepository.GetReleasedForPickingAsync();

    // Kommissioniertermin berechnen
    var kommissionierTage = await _settingRepository.GetIntValueAsync("KommissionierTage", 4);
    var holidays = await _holidayRepository.GetHolidayDatesAsync();

    var items = releasedOrders.Select(o =>
    {
        var item = new PickingListItem
        {
            Id = o.Id,
            PickingPriority = o.PickingPriority,
            OrderNumber = o.OrderNumber,
            ArticleNumber = o.ArticleNumber,
            Description1 = o.Description1,
            Customer = o.Customer,
            Quantity = o.Quantity,
            ProductionDate = o.ProductionDate,
            PickingStatus = o.PickingStatus
        };

        if (o.ProductionDate.HasValue)
        {
            item.KommissionierTermin = _businessDayService.SubtractBusinessDays(
                o.ProductionDate.Value, kommissionierTage, holidays);
        }

        return item;
    }).ToList();

    return View(new PickingListViewModel { Items = items });
}
```

- [ ] **Step 3: ToggleRelease-Action hinzufügen**

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[RequireLeitstandAccess]
public async Task<IActionResult> ToggleRelease(int id, string? returnUrl)
{
    var order = await _productionOrderRepository.GetByIdAsync(id);
    if (order == null)
        return NotFound();

    if (!order.IsReleasedForPicking && string.IsNullOrEmpty(order.ArticleNumber))
    {
        TempData["WarningMessage"] = $"WA {order.OrderNumber} kann nicht freigegeben werden — keine Artikelnummer vorhanden.";
        if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    order.IsReleasedForPicking = !order.IsReleasedForPicking;
    if (order.IsReleasedForPicking)
    {
        order.ReleasedAt = DateTime.UtcNow;
        order.ReleasedBy = _currentUserService.GetDisplayName();

        // Auto-Priorität: nächste freie Nummer vorschlagen
        if (!order.PickingPriority.HasValue)
        {
            var maxPrio = (await _productionOrderRepository.GetReleasedForPickingAsync())
                .Where(o => o.PickingPriority.HasValue)
                .Select(o => o.PickingPriority!.Value)
                .DefaultIfEmpty(0)
                .Max();
            order.PickingPriority = maxPrio + 1;
        }
    }

    order.ModifiedAt = DateTime.UtcNow;
    order.ModifiedBy = _currentUserService.GetDisplayName();
    order.ModifiedByWindows = _currentUserService.GetWindowsUserName();
    await _productionOrderRepository.UpdateAsync(order);

    if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
    return RedirectToAction(nameof(Index));
}
```

- [ ] **Step 4: BulkRelease-Action hinzufügen**

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[RequireLeitstandAccess]
public async Task<IActionResult> BulkRelease(List<int> ids, bool release, string? returnUrl)
{
    if (ids == null || ids.Count == 0)
    {
        if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    var maxPrio = 0;
    if (release)
    {
        var existing = await _productionOrderRepository.GetReleasedForPickingAsync();
        maxPrio = existing
            .Where(o => o.PickingPriority.HasValue)
            .Select(o => o.PickingPriority!.Value)
            .DefaultIfEmpty(0)
            .Max();
    }

    var displayName = _currentUserService.GetDisplayName();
    var windowsUser = _currentUserService.GetWindowsUserName();
    var skipped = new List<string>();

    foreach (var id in ids)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null) continue;

        if (release && string.IsNullOrEmpty(order.ArticleNumber))
        {
            skipped.Add(order.OrderNumber);
            continue;
        }

        order.IsReleasedForPicking = release;
        if (release)
        {
            order.ReleasedAt = DateTime.UtcNow;
            order.ReleasedBy = displayName;
            if (!order.PickingPriority.HasValue)
                order.PickingPriority = ++maxPrio;
        }

        order.ModifiedAt = DateTime.UtcNow;
        order.ModifiedBy = displayName;
        order.ModifiedByWindows = windowsUser;
        await _productionOrderRepository.UpdateAsync(order);
    }

    var count = ids.Count - skipped.Count;
    if (release)
        TempData["SuccessMessage"] = $"{count} Auftrag/Aufträge freigegeben.";
    else
        TempData["SuccessMessage"] = $"{count} Freigabe(n) zurückgenommen.";

    if (skipped.Count > 0)
        TempData["WarningMessage"] = $"Übersprungen (keine Artikelnummer): {string.Join(", ", skipped)}";

    if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
    return RedirectToAction(nameof(Index));
}
```

- [ ] **Step 5: SetPriority-Action hinzufügen**

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[RequireLeitstandAccess]
public async Task<IActionResult> SetPriority(int id, int? priority)
{
    var order = await _productionOrderRepository.GetByIdAsync(id);
    if (order == null)
        return NotFound();

    order.PickingPriority = priority;
    order.ModifiedAt = DateTime.UtcNow;
    order.ModifiedBy = _currentUserService.GetDisplayName();
    order.ModifiedByWindows = _currentUserService.GetWindowsUserName();
    await _productionOrderRepository.UpdateAsync(order);

    return Ok();
}
```

- [ ] **Step 6: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: 0 errors

- [ ] **Step 7: Commit**

```bash
git add IdealAkeWms/Controllers/ProductionOrdersController.cs
git commit -m "feat: extend Index with release fields, overhaul Picking, add ToggleRelease + BulkRelease + SetPriority"
```

---

## Task 6: Views — Index + Picking + PickingDropdown

**Files:**
- Rename: `IdealAkeWms/Views/ProductionOrders/Picking.cshtml` → `PickingDropdown.cshtml`
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml`
- Create: `IdealAkeWms/Views/ProductionOrders/Picking.cshtml` (neue Tabelle)

- [ ] **Step 1: Alte Picking-View als Fallback sichern**

```bash
cp IdealAkeWms/Views/ProductionOrders/Picking.cshtml IdealAkeWms/Views/ProductionOrders/PickingDropdown.cshtml
```

- [ ] **Step 2: Index.cshtml — Titel bedingt ändern**

Zeile 4 und 16 ersetzen:

```csharp
@{
    var pageTitle = Model.LeitstandAktiv && Model.CanManagePickingRelease ? "Produktionsaufträge" : "Werkstattaufträge";
    ViewData["Title"] = pageTitle;
    // ... rest des bestehenden Razor-Blocks
}

<h2 class="page-header">@pageTitle</h2>
```

- [ ] **Step 3: Index.cshtml — Freigabe-Spalte im Tabellenkopf**

Nach der letzten `<th>` (Zeile 73, die leere Actions-Spalte) und VOR `</tr>`, einfügen:

```html
@if (Model.LeitstandAktiv && Model.CanManagePickingRelease)
{
    <th style="width: 160px;">Freigabe</th>
}
```

- [ ] **Step 4: Index.cshtml — Freigabe-Spalte im Tabellenkörper**

In der `@foreach`-Schleife, nach der letzten `<td>` (Actions-Spalte) und VOR `</tr>`:

```html
@if (Model.LeitstandAktiv && Model.CanManagePickingRelease)
{
    <td class="text-center text-nowrap">
        @if (!item.IsDone)
        {
            <form asp-action="ToggleRelease" method="post" style="display:inline">
                @Html.AntiForgeryToken()
                <input type="hidden" name="id" value="@item.Id" />
                <input type="hidden" name="returnUrl" value="@Context.Request.Path@Context.Request.QueryString" />
                @if (item.IsReleasedForPicking)
                {
                    <button type="submit" class="btn btn-sm btn-success me-1" title="Freigabe zurücknehmen">
                        <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
                            <path d="M12.736 3.97a.733.733 0 0 1 1.047 0c.286.289.29.756.01 1.05L7.88 12.01a.733.733 0 0 1-1.065.02L3.217 8.384a.757.757 0 0 1 0-1.06.733.733 0 0 1 1.047 0l3.052 3.093 5.4-6.425z"/>
                        </svg>
                    </button>
                    <input type="number" class="form-control form-control-sm d-inline-block priority-input"
                           value="@item.PickingPriority" min="1" style="width:55px"
                           data-id="@item.Id" title="Priorität (1 = höchste)" />
                }
                else if (!string.IsNullOrEmpty(item.ArticleNumber))
                {
                    <button type="submit" class="btn btn-sm btn-outline-secondary" title="Zur Kommissionierung freigeben">
                        Freigeben
                    </button>
                }
            </form>
        }
    </td>
}
```

- [ ] **Step 5: Index.cshtml — Massenfreigabe-Buttons + Priorität AJAX**

Vor der Tabelle (nach dem Filter-Card, vor `<div class="table-responsive">`):

```html
@if (Model.LeitstandAktiv && Model.CanManagePickingRelease)
{
    <form id="bulkReleaseForm" asp-action="BulkRelease" method="post" class="mb-2">
        @Html.AntiForgeryToken()
        <input type="hidden" name="returnUrl" value="@Context.Request.Path@Context.Request.QueryString" />
        <div id="bulkActions" style="display:none;" class="d-flex gap-2 align-items-center">
            <span class="text-muted small" id="selectedCount">0 ausgewählt</span>
            <button type="submit" name="release" value="true" class="btn btn-sm btn-success">Ausgewählte freigeben</button>
            <button type="submit" name="release" value="false" class="btn btn-sm btn-outline-secondary">Freigabe zurücknehmen</button>
        </div>
    </form>
}
```

Im `@section Scripts`-Block am Ende:

```html
@if (Model.LeitstandAktiv && Model.CanManagePickingRelease)
{
    <script>
        // Priorität AJAX-Save
        document.querySelectorAll('.priority-input').forEach(function (input) {
            input.addEventListener('click', function (e) { e.stopPropagation(); e.preventDefault(); });
            input.addEventListener('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); this.blur(); } });
            input.addEventListener('change', function () {
                var id = this.getAttribute('data-id');
                var priority = this.value ? parseInt(this.value) : null;
                var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
                $.ajax({
                    url: '@Url.Action("SetPriority")',
                    type: 'POST',
                    data: { id: id, priority: priority },
                    headers: { 'RequestVerificationToken': token },
                    error: function () { alert('Fehler beim Speichern der Priorität.'); }
                });
            });
        });

        // Massenfreigabe: Checkboxen
        var bulkForm = document.getElementById('bulkReleaseForm');
        var bulkActions = document.getElementById('bulkActions');
        document.querySelectorAll('.release-checkbox').forEach(function (cb) {
            cb.addEventListener('change', function () {
                var checked = document.querySelectorAll('.release-checkbox:checked');
                bulkActions.style.display = checked.length > 0 ? '' : 'none';
                document.getElementById('selectedCount').textContent = checked.length + ' ausgewählt';
                // Hidden fields für IDs
                bulkForm.querySelectorAll('input[name="ids"]').forEach(function (el) { el.remove(); });
                checked.forEach(function (c) {
                    var hidden = document.createElement('input');
                    hidden.type = 'hidden'; hidden.name = 'ids'; hidden.value = c.value;
                    bulkForm.appendChild(hidden);
                });
            });
        });
        document.getElementById('selectAllRelease')?.addEventListener('change', function () {
            var checked = this.checked;
            document.querySelectorAll('.release-checkbox').forEach(function (cb) { cb.checked = checked; cb.dispatchEvent(new Event('change')); });
        });
    </script>
}
```

Hinweis: Die `.release-checkbox` Checkboxen müssen noch in den Tabellenkörper eingefügt werden — als erste Spalte, nur für Leitstand-User. Im Tabellenkopf eine "Alle auswählen"-Checkbox mit `id="selectAllRelease"`. Im Tabellenkörper `<input type="checkbox" class="release-checkbox" value="@item.Id" />`. Die genaue Einfügestelle hängt von der bestehenden Spaltenstruktur ab — vor der Stückliste-Spalte.

- [ ] **Step 6: Neue Picking.cshtml erstellen**

```html
@model PickingListViewModel
@using System.Globalization
@{
    ViewData["Title"] = "Kommissionierung";
}

<h2 class="page-header">Kommissionierung</h2>

@if (!Model.Items.Any())
{
    <div class="alert alert-info">
        Aktuell sind keine Aufträge zur Kommissionierung freigegeben.
    </div>
}
else
{
    <div class="table-responsive">
        <table class="table table-striped table-hover mb-0 filterable-table">
            <thead>
                <tr>
                    <th style="width: 60px;" data-filterable data-col="0">Prio</th>
                    <th data-filterable data-col="1">WA Nr.</th>
                    <th data-filterable data-col="2">Artikelnummer</th>
                    <th data-filterable data-col="3">Bezeichnung</th>
                    <th data-filterable data-col="4">Kunde</th>
                    <th class="text-end" style="width: 55px;">Stk.</th>
                    <th data-filterable data-col="6" data-date-filter>Komm.-Termin</th>
                    <th data-filterable data-col="7">Status</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var item in Model.Items)
                {
                    var statusBadge = item.PickingStatus switch
                    {
                        "abgeschlossen" => "bg-success",
                        "teilkommissioniert" => "bg-info text-dark",
                        "in Kommissionierung" => "bg-warning text-dark",
                        _ => "bg-secondary"
                    };
                    var statusText = string.IsNullOrEmpty(item.PickingStatus) ? "offen" : item.PickingStatus;
                    var kwStr = item.KommissionierTermin.HasValue
                        ? $"{item.KommissionierTermin.Value:dd.MM.yyyy} KW{ISOWeek.GetWeekOfYear(item.KommissionierTermin.Value)}"
                        : "";

                    <tr class="clickable-row" data-href="@Url.Action("Bom", new { id = item.Id })" style="cursor: pointer;">
                        <td><strong>@(item.PickingPriority?.ToString() ?? "-")</strong></td>
                        <td><strong>@item.OrderNumber</strong></td>
                        <td>@item.ArticleNumber</td>
                        <td>@item.Description1</td>
                        <td>@item.Customer</td>
                        <td class="text-end">@item.Quantity.ToString("N0")</td>
                        <td>@kwStr</td>
                        <td><span class="badge @statusBadge">@statusText</span></td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
}

@section Scripts {
    <script src="~/js/table-filter.js" asp-append-version="true"></script>
    <script>
        document.querySelectorAll('.clickable-row').forEach(function (row) {
            row.addEventListener('click', function () {
                window.location.href = this.getAttribute('data-href');
            });
        });
    </script>
}
```

- [ ] **Step 7: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: 0 errors

- [ ] **Step 8: Commit**

```bash
git add IdealAkeWms/Views/ProductionOrders/
git commit -m "feat: add release column to Index, overhaul Picking to table, keep PickingDropdown as fallback"
```

---

## Task 7: Navigation — Menü + Badge

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml:29-70`

- [ ] **Step 1: Neue Variablen im Nav-Block**

In Zeile 34, nach `bestellungenAktiv`:

```csharp
var canManagePickingRelease = await CurrentUserService.CanManagePickingReleaseAsync();
var leitstandAktiv = (await AppSettings.GetValueAsync("LeitstandAktiv"))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
var releasedPickingCount = leitstandAktiv && canPick
    ? await ProductionOrderRepository.GetReleasedForPickingCountAsync()
    : 0;
```

Hinweis: `ProductionOrderRepository` muss per `@inject` im Layout verfügbar gemacht werden. Am Anfang der Datei (bei den bestehenden `@inject`-Zeilen):

```csharp
@inject IdealAkeWms.Data.Repositories.IProductionOrderRepository ProductionOrderRepository
```

- [ ] **Step 2: Werkstattaufträge/Produktionsaufträge-Menüpunkt**

Zeilen 59-64 ersetzen:

```html
@if (leitstandAktiv)
{
    @if (canManagePickingRelease || canViewTracking)
    {
        <li class="nav-item">
            <a class="nav-link" asp-controller="ProductionOrders" asp-action="Index">Produktionsaufträge</a>
        </li>
    }
}
else
{
    @if (canPick || canViewTracking)
    {
        <li class="nav-item">
            <a class="nav-link" asp-controller="ProductionOrders" asp-action="Index">Werkstattaufträge</a>
        </li>
    }
}
```

- [ ] **Step 3: Kommissionierung-Menüpunkt mit Badge**

Zeilen 65-70 ersetzen:

```html
@if (canPick)
{
    <li class="nav-item">
        <a class="nav-link" asp-controller="ProductionOrders" asp-action="Picking">
            Kommissionierung
            @if (leitstandAktiv && releasedPickingCount > 0)
            {
                <span class="badge rounded-pill" style="background-color: var(--ake-orange); font-size: 0.7em;">@releasedPickingCount</span>
            }
        </a>
    </li>
}
```

- [ ] **Step 4: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/Views/Shared/_Layout.cshtml
git commit -m "feat: conditional menu (Produktionsaufträge/Werkstattaufträge), picking badge count"
```

---

## Task 8: Tests + Dokumentation + Version

**Files:**
- Modify: `CLAUDE.md`, `README.md`, `PROJECT_STATUS.md`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`, `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `IdealAkeWms/AppVersion.cs`, `IDEALAKEWMSService/AppVersion.cs`

- [ ] **Step 1: Alle Tests ausführen**

Run: `dotnet test IdealAkeWms.Tests`
Expected: All tests pass

- [ ] **Step 2: Version auf 1.2.0 setzen**

Beide `AppVersion.cs` (Web + Service): `Version = "1.2.0"`, `Date` auf aktuelles Datum.

- [ ] **Step 3: Changelog.cshtml — v1.2.0 Block hinzufügen**

Neuer Block mit:
- Leitstand-Funktion: Kommissionier-Freigabe und Priorisierung
- Neue Rolle "Leitstand"
- Einzel- und Massenfreigabe
- Numerische Priorisierung
- Neue Kommissionierliste (Tabelle statt Dropdown)
- Feature per Setting aktivierbar

- [ ] **Step 4: Help/Index.cshtml — Leitstand-Sektion hinzufügen**

Neue Card mit Anleitung:
- Aktivierung per Settings
- Freigabe-Workflow erklären
- Priorisierung erklären
- Kommissionierliste erklären

- [ ] **Step 5: README.md, PROJECT_STATUS.md, CLAUDE.md aktualisieren**

- README: Neuer Abschnitt "Leitstand / Kommissionier-Freigabe", AppSetting `LeitstandAktiv`, Rolle `leitstand`
- PROJECT_STATUS: Version 1.2.0, Feature-Status "Fertig"
- CLAUDE.md: Neue Rolle, Berechtigung, Menü-Sichtbarkeitsregeln, Fallstricke (Toggle-Verhalten, BOM-Zugriff ohne Release-Check)

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "docs: update version to 1.2.0, changelog, help, README, CLAUDE.md for Leitstand feature"
```

---

## Zusammenfassung

| Task | Inhalt | Neue Dateien | Geänderte Dateien |
|------|--------|-------------|-------------------|
| 1 | DB-Schema + Model + EF Migration | SQL/37, EF Migration | ProductionOrder.cs, Program.cs, SQL/00, AgentJobs |
| 2 | Rolle + Permission + Filter | RequireLeitstandAccess.cs | RoleKeys.cs, ICurrentUserService.cs, CurrentUserService.cs |
| 3 | Repository-Methoden + Tests | Tests | IProductionOrderRepository.cs, ProductionOrderRepository.cs |
| 4 | ViewModels | PickingListViewModel.cs | ProductionOrderViewModel.cs |
| 5 | Controller (Index, Picking, 3 neue Actions) | — | ProductionOrdersController.cs |
| 6 | Views (Index, Picking, PickingDropdown) | Picking.cshtml, PickingDropdown.cshtml | Index.cshtml |
| 7 | Navigation (Menü + Badge) | — | _Layout.cshtml |
| 8 | Tests + Dokumentation + Version | — | Alle Docs |

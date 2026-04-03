# Leitstand — Kommissionier-Freigabe & Priorisierung

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Leitstand-User können Produktionsaufträge zur Kommissionierung freigeben und priorisieren. Kommissionierer sehen nur freigegebene Aufträge mit Fortschrittsanzeige.

**Architecture:** Neue Rolle `leitstand` mit Freigabe/Prioritäts-Verwaltung auf `ProductionOrders`. Bestehende WA-Liste wird um Freigabe-Spalte erweitert. Kommissionierung-View wird von Dropdown auf Tabelle mit Progress Bars umgebaut. Menü-Trennung: Leitstand/Tracking sehen "Produktionsaufträge", Kommissionierer sehen nur "Kommissionierung".

**Tech Stack:** ASP.NET Core 10.0, EF Core 10.0, SQL Server, Bootstrap 5 Progress Bars, jQuery AJAX

---

## Review-Ergebnisse & Verbesserungen

### 1. Feature-Toggle `LeitstandAktiv` (NEU)
Das Feature muss per AppSetting `LeitstandAktiv` (Default: `false`) aktivierbar sein.
- **Wenn deaktiviert**: Kein Freigabe-Workflow. Kommissionierung funktioniert wie bisher (Dropdown, alle offenen WAs).
- **Wenn aktiviert**: Freigabe + Priorisierung + neue Kommissionierliste.
- Pattern: Wie `BestellungenAktiv` und `TeileverfolgungAktiv` — Toggle in Settings-Seite, Prüfung in Controller + Layout.
- _Layout.cshtml: Menüpunkt "Produktionsaufträge" nur wenn `leitstandAktiv`, sonst wie bisher "Werkstattaufträge"
- Picking-Action: Wenn `leitstandAktiv=false` → altes Dropdown, wenn `true` → neue Tabelle
- Index.cshtml: Freigabe-Spalte nur wenn `leitstandAktiv=true`

### 2. Datenbank-Design Überlegungen
- **`PickingPriority` Uniqueness**: KEIN Unique-Constraint. Gleiche Priorität erlaubt (sortiert dann nach `ProductionDate`). Vereinfacht die UI (kein Umordnen nötig bei gleicher Prio).
- **`ReleasedAt`/`ReleasedBy`**: Bleiben auch nach Un-Release erhalten (Audit-Trail). Nur `IsReleasedForPicking` wird getoggelt.
- **Sage-Import**: Neue Spalten haben `DEFAULT 0`/`NULL` → Import-MERGE berührt sie nicht. Sicher.
- **SetPickingStatus "abgeschlossen"**: Setzt `IsDone=true`. Freigegebener Auftrag verschwindet dann automatisch aus der Kommissionierliste (da `!IsDone` gefiltert).

### 3. Edge Cases
- **Auftrag freigeben ohne Picking-Items**: PickingItems werden erst beim Öffnen der BOM initialisiert (`InitializePickingAsync`). Fortschrittsbalken zeigt 0/0. Das ist OK — in der Kommissionierliste wird "0/0" angezeigt, beim Klick wird die BOM geladen und Items initialisiert.
- **Mehrere Leitstand-User**: Kein Problem — Freigabe ist idempotent (Toggle).  Priorität kann überschrieben werden.
- **Bestehende Daten nach Aktivierung**: Alle bestehenden WAs haben `IsReleasedForPicking=false`. Leitstand muss sie erst freigeben. Das ist Absicht — Rückwärtskompatibilität.

### 4. Potentielle Probleme
- **Picking-User verliert WA-Zugriff**: Wenn `LeitstandAktiv=true`, sieht Picker nicht mehr die WA-Liste. Direktlink auf `/ProductionOrders/Bom/{id}` funktioniert weiterhin (braucht nur `RequirePickingAccess`). **Kein Blocking-Issue.**
- **Admin-Zugriff**: Admin hat sowohl `CanPickAsync` als auch `CanManagePickingReleaseAsync`. Admin sieht beides: "Produktionsaufträge" UND "Kommissionierung". Das ist korrekt.
- **Bom-Action**: Braucht KEINEN Release-Check. Auch nicht-freigegebene Aufträge können per Direktlink geöffnet werden (z.B. vom Leitstand oder Admin). Die Freigabe steuert nur die Sichtbarkeit in der Kommissionierliste.

### 5. Keine Separierung nötig (YAGNI)
- Kein `CommissioningOrder`-Entity nötig — die Freigabe-Felder direkt auf `ProductionOrder` reichen aus
- Keine Prioritäts-History nötig — `ModifiedAt`/`ModifiedBy` auf dem Auftrag reicht
- Kein Drag & Drop — inline Number-Input ist ausreichend und konsistent

---

## File Structure

### Neue Dateien

| Datei | Verantwortung |
|-------|---------------|
| `SQL/37_AddPickingRelease.sql` | DB-Migration: 4 Spalten, 1 Index, Rolle `leitstand` |
| `IdealAkeWms/Filters/RequireLeitstandAccessAttribute.cs` | Action-Filter für Leitstand-Zugriff |
| `IdealAkeWms/Models/ViewModels/PickingListViewModel.cs` | ViewModel für neue Kommissionierliste |

### Geänderte Dateien

| Datei | Änderung |
|-------|----------|
| `IdealAkeWms/Models/ProductionOrder.cs` | +4 Properties (Release-Felder) |
| `IdealAkeWms/Models/RoleKeys.cs` | +1 Konstante `Leitstand` |
| `IdealAkeWms/Services/ICurrentUserService.cs` | +2 Methoden |
| `IdealAkeWms/Services/CurrentUserService.cs` | +2 Implementierungen |
| `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs` | +2 Methoden |
| `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs` | +2 Implementierungen |
| `IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs` | +5 Properties |
| `IdealAkeWms/Controllers/ProductionOrdersController.cs` | Index anpassen, Picking überarbeiten, +2 Actions |
| `IdealAkeWms/Views/ProductionOrders/Index.cshtml` | Titel + Freigabe-Spalte |
| `IdealAkeWms/Views/ProductionOrders/Picking.cshtml` | Komplett-Umbau zu Tabelle |
| `IdealAkeWms/Views/Shared/_Layout.cshtml` | Menü-Umbenennung + Sichtbarkeit |
| `SQL/00_FreshInstall.sql` | Konsolidierung neue Spalten + Rolle |
| `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` | Kommentar: neue Felder app-verwaltet |
| `IdealAkeWms/Program.cs` | AppSetting `LeitstandAktiv` seeden (Default: false) |

### Test-Dateien

| Datei | Tests |
|-------|-------|
| `IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs` | Neue Methoden testen |

---

## Task 1: SQL-Migration + EF-Migration

**Files:**
- Create: `SQL/37_AddPickingRelease.sql`
- Modify: `IdealAkeWms/Models/ProductionOrder.cs`
- Modify: `SQL/00_FreshInstall.sql` (Zeilen ~220-224 ProductionOrders CREATE TABLE)
- Modify: `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` (Kommentar-Block Zeile 17-24)

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

-- Performance-Index fuer Kommissionierliste (freigegebene, offene Auftraege)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_PickingRelease')
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_PickingRelease]
        ON [dbo].[ProductionOrders]([IsReleasedForPicking], [IsDone])
        INCLUDE ([PickingPriority], [OrderNumber], [ArticleNumber], [Customer], [ProductionDate], [PickingStatus]);
GO

-- Neue Rolle: Leitstand
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'leitstand')
BEGIN
    INSERT INTO [dbo].[Roles] ([Key], [Name], [Description], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('leitstand', 'Leitstand', 'Produktionsauftraege freigeben und Kommissionier-Prioritaeten verwalten', GETUTCDATE(), 'system', 'SYSTEM');
END
GO

-- AppSetting: Leitstand-Feature aktivieren
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

- [ ] **Step 2: Model erweitern**

In `IdealAkeWms/Models/ProductionOrder.cs`, nach `ProductionWorkplace` Navigation Property:

```csharp
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

- [ ] **Step 3: EF Migration generieren**

Run: `dotnet ef migrations add AddPickingRelease --project IdealAkeWms`
Expected: Migration file created in `Migrations/`

- [ ] **Step 4: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj --no-restore`
Expected: 0 errors

- [ ] **Step 5: SQL/00_FreshInstall.sql aktualisieren**

In der `CREATE TABLE ProductionOrders` Sektion (nach Zeile ~223 `[HasExternalPurchase]`):

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

In der Rollen-Seeding-Sektion (nach der letzten Rolle):

```sql
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'leitstand')
    INSERT INTO [dbo].[Roles] ([Key], [Name], [Description], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('leitstand', 'Leitstand', 'Produktionsauftraege freigeben und Kommissionier-Prioritaeten verwalten', GETUTCDATE(), 'system', 'SYSTEM');
```

- [ ] **Step 6: Program.cs — AppSetting seeden**

Im Startup-Seeding-Block von `Program.cs` (wo `BestellungenAktiv` geseeded wird), analog ergänzen:

```csharp
if (await settingRepo.GetValueAsync("LeitstandAktiv") == null)
    await settingRepo.SetValueAsync("LeitstandAktiv", "false", "Leitstand-Funktion: Kommissionier-Freigabe und Priorisierung aktivieren");
```

- [ ] **Step 7: Agent-Job Kommentar erweitern**

In `SQL/AgentJobs/01_Import_Produktionsauftraege.sql`, Zeile 18 erweitern:

```sql
--   IsDone, PickingStatus, HasGlass, HasExternalPurchase,
--   IsReleasedForPicking, PickingPriority, ReleasedAt, ReleasedBy
```

- [ ] **Step 8: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: 0 errors

- [ ] **Step 9: Commit**

```bash
git add SQL/37_AddPickingRelease.sql SQL/00_FreshInstall.sql SQL/AgentJobs/01_Import_Produktionsauftraege.sql IdealAkeWms/Models/ProductionOrder.cs IdealAkeWms/Migrations/ IdealAkeWms/Program.cs
git commit -m "feat: add picking release schema — 4 columns on ProductionOrders, leitstand role, LeitstandAktiv setting, EF migration"
```

---

## Task 2: Rolle + Berechtigungen + Filter

**Files:**
- Modify: `IdealAkeWms/Models/RoleKeys.cs`
- Modify: `IdealAkeWms/Services/ICurrentUserService.cs`
- Modify: `IdealAkeWms/Services/CurrentUserService.cs`
- Create: `IdealAkeWms/Filters/RequireLeitstandAccessAttribute.cs`

- [ ] **Step 1: RoleKeys erweitern**

In `IdealAkeWms/Models/RoleKeys.cs`, nach `Reporting`:

```csharp
public const string Leitstand = "leitstand";
```

- [ ] **Step 2: ICurrentUserService erweitern**

In `IdealAkeWms/Services/ICurrentUserService.cs`, nach `CanTransferStockAsync()`:

```csharp
Task<bool> CanManagePickingReleaseAsync();
```

- [ ] **Step 3: CurrentUserService implementieren**

In `IdealAkeWms/Services/CurrentUserService.cs`, nach `CanTransferStockAsync()` (Zeile 94):

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

- [ ] **Step 5: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj --no-restore`
Expected: 0 errors

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms/Models/RoleKeys.cs IdealAkeWms/Services/ICurrentUserService.cs IdealAkeWms/Services/CurrentUserService.cs IdealAkeWms/Filters/RequireLeitstandAccessAttribute.cs
git commit -m "feat: add leitstand role, CanManagePickingReleaseAsync permission, RequireLeitstandAccess filter"
```

---

## Task 3: Repository — Freigegebene Aufträge + Fortschritt

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs`
- Test: `IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs`

- [ ] **Step 1: PickingProgressInfo DTO erstellen**

Am Ende von `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs`:

```csharp
public class PickingProgressInfo
{
    public int TotalItems { get; set; }
    public int PickedItems { get; set; }
    public int TransferredItems { get; set; }
}
```

- [ ] **Step 2: Interface erweitern**

In `IProductionOrderRepository`, nach `SearchAsync`:

```csharp
Task<List<ProductionOrder>> GetReleasedForPickingAsync();
Task<Dictionary<int, PickingProgressInfo>> GetPickingProgressBulkAsync(List<int> productionOrderIds);
```

- [ ] **Step 3: GetReleasedForPickingAsync implementieren**

In `ProductionOrderRepository.cs`:

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
```

- [ ] **Step 4: GetPickingProgressBulkAsync implementieren**

In `ProductionOrderRepository.cs`:

```csharp
public async Task<Dictionary<int, PickingProgressInfo>> GetPickingProgressBulkAsync(List<int> productionOrderIds)
{
    if (productionOrderIds.Count == 0)
        return new Dictionary<int, PickingProgressInfo>();

    return await _context.PickingItems
        .Where(p => productionOrderIds.Contains(p.ProductionOrderId))
        .GroupBy(p => p.ProductionOrderId)
        .Select(g => new
        {
            ProductionOrderId = g.Key,
            Total = g.Count(),
            Picked = g.Count(p => p.IsPicked),
            Transferred = g.Count(p => p.IsTransferred)
        })
        .ToDictionaryAsync(
            x => x.ProductionOrderId,
            x => new PickingProgressInfo
            {
                TotalItems = x.Total,
                PickedItems = x.Picked,
                TransferredItems = x.Transferred
            });
}
```

Hinweis: `_context` ist über die Basis-Klasse `Repository<T>` verfügbar. Prüfe ob dort `protected ApplicationDbContext _context` existiert — falls nur `_dbSet`, muss der Context im Konstruktor gespeichert oder über `_dbSet` hergeleitet werden. Referenz: `PickingRepository.cs` greift ebenfalls auf `_context.PickingItems` zu.

- [ ] **Step 5: Tests schreiben**

In `IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs` (neue Datei oder erweitern):

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
public async Task GetPickingProgressBulkAsync_CalculatesCorrectly()
{
    using var context = TestDbContextFactory.CreateContext();
    var order = new ProductionOrder { OrderNumber = "WA-1", CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" };
    context.ProductionOrders.Add(order);
    await context.SaveChangesAsync();

    context.PickingItems.AddRange(
        new PickingItem { ProductionOrderId = order.Id, BomArticleNumber = "A1", Quantity = 1, IsPicked = true, IsTransferred = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
        new PickingItem { ProductionOrderId = order.Id, BomArticleNumber = "A2", Quantity = 1, IsPicked = true, IsTransferred = false, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
        new PickingItem { ProductionOrderId = order.Id, BomArticleNumber = "A3", Quantity = 1, IsPicked = false, IsTransferred = false, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
    );
    await context.SaveChangesAsync();

    var repo = new ProductionOrderRepository(context);
    var result = await repo.GetPickingProgressBulkAsync(new List<int> { order.Id });

    result.Should().ContainKey(order.Id);
    result[order.Id].TotalItems.Should().Be(3);
    result[order.Id].PickedItems.Should().Be(2);
    result[order.Id].TransferredItems.Should().Be(1);
}
```

- [ ] **Step 6: Tests ausführen**

Run: `dotnet test IdealAkeWms.Tests --filter "ProductionOrderRepository"`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs IdealAkeWms.Tests/
git commit -m "feat: add GetReleasedForPickingAsync + GetPickingProgressBulkAsync with tests"
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
    public string? PickingStatus { get; set; }
    public int TotalItems { get; set; }
    public int PickedItems { get; set; }
    public int TransferredItems { get; set; }
    public int ProgressPercent => TotalItems > 0
        ? (int)Math.Round((PickedItems + TransferredItems) * 100.0 / TotalItems)
        : 0;
}
```

- [ ] **Step 3: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj --no-restore`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs IdealAkeWms/Models/ViewModels/PickingListViewModel.cs
git commit -m "feat: add PickingListViewModel and extend ProductionOrderViewModel with release fields"
```

---

## Task 5: Controller — Index erweitern + Picking überarbeiten + neue Actions

**Files:**
- Modify: `IdealAkeWms/Controllers/ProductionOrdersController.cs`

- [ ] **Step 1: Index-Action Access-Filter ändern**

Zeile 69: `[RequirePickingOrTrackingAccess]` ersetzen durch eine erweiterte Bedingung.

Da wir keinen neuen Filter-Attribute nur für die Index-Action erstellen wollen (zu viel Overhead), nutzen wir die bestehende Methode im Controller:

```csharp
public async Task<IActionResult> Index(...)
{
    // Zugriffsprüfung: Leitstand, Picking oder Tracking
    if (!await _currentUserService.CanPickAsync()
        && !await _currentUserService.CanViewTrackingAsync()
        && !await _currentUserService.CanManagePickingReleaseAsync())
    {
        return RedirectToAction("AccessDenied", "Account");
    }
    // ... rest der Methode
}
```

Entferne `[RequirePickingOrTrackingAccess]` von der Index-Action.

- [ ] **Step 2: Release-Felder ins ViewItem mappen**

Im Index-Action ViewItem-Mapping (wo `IsDone`, `PickingStatus` etc. gemappt werden), ergänzen:

```csharp
IsReleasedForPicking = o.IsReleasedForPicking,
PickingPriority = o.PickingPriority,
ReleasedAt = o.ReleasedAt,
ReleasedBy = o.ReleasedBy,
```

Und im ViewModel-Constructor:

```csharp
CanManagePickingRelease = await _currentUserService.CanManagePickingReleaseAsync(),
```

- [ ] **Step 3: Picking-Action überarbeiten**

Bestehende `Picking()`-Methode (Zeile 63-67) komplett ersetzen.
**Wichtig**: Wenn `LeitstandAktiv=false`, bisheriges Verhalten (Dropdown-View) beibehalten!

```csharp
[RequirePickingAccess]
public async Task<IActionResult> Picking()
{
    var leitstandAktiv = (await _settingRepository.GetValueAsync("LeitstandAktiv"))
        ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

    if (!leitstandAktiv)
    {
        // Bisheriges Verhalten: Dropdown-Auswahl
        return View("PickingDropdown");
    }

    // Neues Verhalten: Tabelle mit freigegebenen Aufträgen
    var releasedOrders = await _productionOrderRepository.GetReleasedForPickingAsync();
    var orderIds = releasedOrders.Select(o => o.Id).ToList();
    var progress = await _productionOrderRepository.GetPickingProgressBulkAsync(orderIds);

    var items = releasedOrders.Select(o =>
    {
        progress.TryGetValue(o.Id, out var p);
        return new PickingListItem
        {
            Id = o.Id,
            PickingPriority = o.PickingPriority,
            OrderNumber = o.OrderNumber,
            ArticleNumber = o.ArticleNumber,
            Description1 = o.Description1,
            Customer = o.Customer,
            Quantity = o.Quantity,
            ProductionDate = o.ProductionDate,
            PickingStatus = o.PickingStatus,
            TotalItems = p?.TotalItems ?? 0,
            PickedItems = p?.PickedItems ?? 0,
            TransferredItems = p?.TransferredItems ?? 0
        };
    }).ToList();

    return View(new PickingListViewModel { Items = items });
}
```

**Hinweis**: Die bisherige `Picking.cshtml` wird zu `PickingDropdown.cshtml` umbenannt. Die neue `Picking.cshtml` enthält die Tabelle. So bleibt der Fallback sauber.

- [ ] **Step 4: ToggleRelease-Action hinzufügen**

Nach der `Picking`-Action:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[RequireLeitstandAccess]
public async Task<IActionResult> ToggleRelease(int id, string? returnUrl)
{
    var order = await _productionOrderRepository.GetByIdAsync(id);
    if (order == null)
        return NotFound();

    order.IsReleasedForPicking = !order.IsReleasedForPicking;
    if (order.IsReleasedForPicking)
    {
        order.ReleasedAt = DateTime.UtcNow;
        order.ReleasedBy = _currentUserService.GetDisplayName();
    }

    order.ModifiedAt = DateTime.UtcNow;
    order.ModifiedBy = _currentUserService.GetDisplayName();
    order.ModifiedByWindows = _currentUserService.GetWindowsUserName();
    await _productionOrderRepository.UpdateAsync(order);

    if (!string.IsNullOrEmpty(returnUrl))
        return Redirect(returnUrl);
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

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj --no-restore`
Expected: 0 errors

- [ ] **Step 7: Commit**

```bash
git add IdealAkeWms/Controllers/ProductionOrdersController.cs
git commit -m "feat: extend Index with release fields, overhaul Picking to table, add ToggleRelease + SetPriority actions"
```

---

## Task 6: Views — Produktionsaufträge (Index) + Kommissionierliste (Picking)

**Files:**
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml`
- Rename: `IdealAkeWms/Views/ProductionOrders/Picking.cshtml` → `PickingDropdown.cshtml` (Fallback)
- Create: `IdealAkeWms/Views/ProductionOrders/Picking.cshtml` (neue Tabelle)

- [ ] **Step 1: Index.cshtml — Titel bedingt ändern**

Der Titel hängt davon ab, ob Leitstand aktiv ist. `CanManagePickingRelease` ist bereits im ViewModel.

```csharp
@{
    var pageTitle = Model.CanManagePickingRelease ? "Produktionsaufträge" : "Werkstattaufträge";
    ViewData["Title"] = pageTitle;
}
<h2 class="page-header">@pageTitle</h2>
```

- [ ] **Step 2: Index.cshtml — Freigabe-Spalte hinzufügen**

Im `<thead>`, nach der letzten bestehenden Spalte und VOR dem `</tr>`, neue Spalte:

```html
@if (Model.CanManagePickingRelease)
{
    <th style="width: 140px;">Freigabe</th>
}
```

Im `<tbody>`, in jeder Zeile am Ende (vor `</tr>`):

```html
@if (Model.CanManagePickingRelease)
{
    <td class="text-center text-nowrap">
        <form asp-action="ToggleRelease" method="post" style="display:inline">
            @Html.AntiForgeryToken()
            <input type="hidden" name="id" value="@item.Id" />
            <input type="hidden" name="returnUrl" value="@Context.Request.Path@Context.Request.QueryString" />
            @if (item.IsReleasedForPicking)
            {
                <button type="submit" class="btn btn-sm btn-success" title="Freigabe zurücknehmen">
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
                        <path d="M12.736 3.97a.733.733 0 0 1 1.047 0c.286.289.29.756.01 1.05L7.88 12.01a.733.733 0 0 1-1.065.02L3.217 8.384a.757.757 0 0 1 0-1.06.733.733 0 0 1 1.047 0l3.052 3.093 5.4-6.425z"/>
                    </svg>
                </button>
                <input type="number" class="form-control form-control-sm d-inline-block priority-input"
                       value="@item.PickingPriority" min="1" style="width:55px"
                       data-id="@item.Id" title="Priorität (1 = höchste)" />
            }
            else
            {
                <button type="submit" class="btn btn-sm btn-outline-secondary" title="Zur Kommissionierung freigeben">
                    Freigeben
                </button>
            }
        </form>
    </td>
}
```

- [ ] **Step 3: Index.cshtml — Priorität AJAX-Save**

Im `@section Scripts` Block:

```html
<script>
    document.querySelectorAll('.priority-input').forEach(function (input) {
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
        // Klick im Input soll NICHT das Form submitten
        input.addEventListener('click', function (e) { e.stopPropagation(); });
        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); this.blur(); }
        });
    });
</script>
```

- [ ] **Step 4: Picking.cshtml komplett neu schreiben**

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
                    <th class="text-end">Stk.</th>
                    <th data-filterable data-col="6" data-date-filter>Fert.-Termin</th>
                    <th>Status</th>
                    <th style="width: 200px;">Fortschritt</th>
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
                    var kw = item.ProductionDate.HasValue ? $" KW{ISOWeek.GetWeekOfYear(item.ProductionDate.Value)}" : "";

                    <tr class="clickable-row" data-href="@Url.Action("Bom", new { id = item.Id })" style="cursor: pointer;">
                        <td><strong>@(item.PickingPriority?.ToString() ?? "-")</strong></td>
                        <td><strong>@item.OrderNumber</strong></td>
                        <td>@item.ArticleNumber</td>
                        <td>@item.Description1</td>
                        <td>@item.Customer</td>
                        <td class="text-end">@item.Quantity.ToString("N0")</td>
                        <td>@(item.ProductionDate?.ToString("dd.MM.yyyy"))@kw</td>
                        <td><span class="badge @statusBadge">@statusText</span></td>
                        <td>
                            <div class="d-flex align-items-center gap-2">
                                <div class="progress flex-grow-1" style="height: 20px;">
                                    @{
                                        var transferPct = item.TotalItems > 0 ? (int)Math.Round(item.TransferredItems * 100.0 / item.TotalItems) : 0;
                                        var pickedPct = item.TotalItems > 0 ? (int)Math.Round((item.PickedItems - item.TransferredItems) * 100.0 / item.TotalItems) : 0;
                                        if (pickedPct < 0) pickedPct = 0;
                                    }
                                    @if (transferPct > 0)
                                    {
                                        <div class="progress-bar bg-success" style="width: @(transferPct)%" title="Umgebucht: @item.TransferredItems"></div>
                                    }
                                    @if (pickedPct > 0)
                                    {
                                        <div class="progress-bar" style="width: @(pickedPct)%; background-color: var(--ake-secondary);" title="Gepickt: @(item.PickedItems - item.TransferredItems)"></div>
                                    }
                                </div>
                                <small class="text-muted text-nowrap">@item.PickedItems/@item.TotalItems</small>
                            </div>
                        </td>
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

- [ ] **Step 5: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj --no-restore`
Expected: 0 errors

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms/Views/ProductionOrders/Index.cshtml IdealAkeWms/Views/ProductionOrders/Picking.cshtml
git commit -m "feat: add release column to Index, overhaul Picking to table with progress bars"
```

---

## Task 7: Navigation — Feature-Toggle-abhängige Menü-Anpassung

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Neue Variablen im Nav-Block**

Im Razor-Block am Anfang der Navigation (wo `canPick`, `canViewTracking` etc. deklariert werden):

```csharp
var canManagePickingRelease = await CurrentUserService.CanManagePickingReleaseAsync();
var leitstandAktiv = (await AppSettings.GetValueAsync("LeitstandAktiv"))
    ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
```

- [ ] **Step 2: Werkstattaufträge / Produktionsaufträge — bedingt nach Toggle**

```html
@if (leitstandAktiv)
{
    @* Leitstand aktiv: Nur Leitstand + Tracking sehen die volle Liste *@
    @if (canManagePickingRelease || canViewTracking)
    {
        <li class="nav-item">
            <a class="nav-link" asp-controller="ProductionOrders" asp-action="Index">Produktionsaufträge</a>
        </li>
    }
}
else
{
    @* Leitstand deaktiviert: Bisheriges Verhalten — Picking + Tracking sehen WA-Liste *@
    @if (canPick || canViewTracking)
    {
        <li class="nav-item">
            <a class="nav-link" asp-controller="ProductionOrders" asp-action="Index">Werkstattaufträge</a>
        </li>
    }
}
```

**Wichtig:** Wenn Leitstand deaktiviert → alles wie bisher. Wenn aktiviert → Kommissionierer sehen nur "Kommissionierung".

- [ ] **Step 3: Kommissionierung bleibt unverändert**

Der Menüpunkt "Kommissionierung" bleibt wie er ist (Bedingung `canPick`). Er zeigt je nach Toggle das Dropdown oder die neue Tabelle.

- [ ] **Step 4: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj --no-restore`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/Views/Shared/_Layout.cshtml
git commit -m "feat: rename menu to Produktionsaufträge, restrict to leitstand+tracking, keep Kommissionierung for pickers"
```

---

## Task 8: Tests ausführen + Dokumentation

**Files:**
- Modify: `CLAUDE.md`, `README.md`, `PROJECT_STATUS.md`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`, `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `IdealAkeWms/AppVersion.cs`, `IDEALAKEWMSService/AppVersion.cs`

- [ ] **Step 1: Alle Tests ausführen**

Run: `dotnet test IdealAkeWms.Tests`
Expected: All tests pass (208+ tests)

- [ ] **Step 2: Version hochzählen**

Beide `AppVersion.cs` auf `1.2.0` setzen.

- [ ] **Step 3: Changelog + Hilfe + README + PROJECT_STATUS + CLAUDE.md aktualisieren**

Inhalte:
- Neue Rolle `leitstand` beschreiben
- Neues Berechtigungskonzept für Produktionsaufträge vs Kommissionierung
- Freigabe-Workflow dokumentieren
- Fortschrittsbalken erklären
- Menü-Änderung (Werkstattaufträge → Produktionsaufträge) dokumentieren
- CLAUDE.md: Neue Fallstricke ergänzen (Picking ist jetzt client-seitig, Menü-Sichtbarkeit, Agent-Job respektiert neue Felder)

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "docs: update version to 1.2.0, changelog, help page, README, CLAUDE.md for Leitstand feature"
```

---

## Verifikation (manuell nach Deployment)

### A. LeitstandAktiv = false (Rückwärtskompatibilität)

| Test | Erwartung |
|------|-----------|
| Menü für Picker | Sieht "Werkstattaufträge" + "Kommissionierung" (wie bisher) |
| Kommissionierung öffnen | Zeigt Dropdown-Auswahl (wie bisher) |
| WA-Liste | Keine Freigabe-Spalte sichtbar |
| Settings-Seite | Zeigt `LeitstandAktiv` Toggle auf "Deaktiviert" |

### B. LeitstandAktiv = true (neues Feature)

| Test | Erwartung |
|------|-----------|
| Leitstand-User öffnet Produktionsaufträge | Sieht Freigabe-Spalte mit Toggle-Button + Prioritäts-Eingabe |
| Leitstand gibt WA frei | Badge "Freigegeben" erscheint, `ReleasedAt`/`ReleasedBy` in DB |
| Leitstand setzt Priorität 1 | Wird per AJAX gespeichert, kein Page-Reload |
| Kommissionierer öffnet Menü | Sieht NUR "Kommissionierung", NICHT "Produktionsaufträge" |
| Kommissionierer öffnet Kommissionierung | Tabelle mit freigegebenen Aufträgen, sortiert nach Priorität |
| Fortschrittsbalken | Grün = umgebucht, Blau = gepickt, Zahl "12/45" |
| Klick auf Zeile in Kommissionierliste | Öffnet Stücklisten-View (Bom) |
| Tracking-User | Sieht "Produktionsaufträge" read-only (keine Freigabe-Spalte) |
| Admin | Sieht alles (Produktionsaufträge mit Freigabe + Kommissionierung) |
| Leitstand nimmt Freigabe zurück | WA verschwindet aus Kommissionierliste, PickingItems bleiben |
| Auftrag ohne PickingItems in Kommissionierliste | Zeigt "0/0", beim Klick wird BOM initialisiert |

---

## Design-Entscheidungen

| Frage | Entscheidung | Begründung |
|-------|-------------|------------|
| Prioritäts-UI | Inline Number-Input + AJAX | Einfacher als Drag & Drop, konsistent mit bestehenden Patterns |
| Freigabe-API | POST mit AntiForgery (kein AJAX-Toggle) | Audit-Trail wichtig (ReleasedAt/By werden gesetzt) |
| Un-Release mit gepickten Items | Erlaubt, Items bleiben | Kein Datenverlust, Re-Release stellt Fortschritt wieder her |
| Fortschritts-Berechnung | Einzige GROUP BY Query | Vermeidet N+1, performant auch bei vielen Aufträgen |
| Menü-Sichtbarkeit | Picking-User sehen Produktionsaufträge NICHT | Klare Trennung: Leitstand steuert, Kommissionierer arbeitet ab |
| Progress Bar | Stacked: grün (transferred) + blau (picked) | Zwei Phasen des Workflows visuell unterscheidbar |
| Feature-Toggle | `LeitstandAktiv` AppSetting (Default: false) | Rückwärtskompatibilität, schrittweises Rollout |
| Deaktivierter Toggle | Altes Verhalten (Dropdown, WA-Liste für alle) | Kein Breaking Change bei bestehenden Installationen |
| Picking.cshtml Fallback | Alte View als `PickingDropdown.cshtml` erhalten | Controller wählt View je nach Toggle |

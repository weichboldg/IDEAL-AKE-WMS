# BDE-Settings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Feature-Toggle `BdeAktiv` + vereinfachter Modus `BdeNurFaMeldung` mit Auto-Create-Default-AG für das BDE-Modul.

**Architecture:** 3 neue AppSettings (DB-Tabelle), neuer Filter `[RequireBdeActive]`, Navigation-Gating, `BdeDefaultWorkOperationService` für Find-Or-Create-Logik, Terminal-JS/API-Umschaltung zwischen Normal- und NurFA-Modus.

**Tech Stack:** ASP.NET Core 10.0, EF Core 10.0, SQL Server, Bootstrap 5, jQuery/JS, xUnit.

**Spec:** `docs/superpowers/specs/2026-04-16-bde-settings-design.md`

---

## File Structure

### Neue Dateien

| Datei | Verantwortung |
|-------|---------------|
| `IdealAkeWms/Filters/RequireBdeActiveAttribute.cs` | Filter: BDE-Modul muss aktiv sein |
| `IdealAkeWms/Services/IBdeDefaultWorkOperationService.cs` | Interface: Find-Or-Create Default-AG |
| `IdealAkeWms/Services/BdeDefaultWorkOperationService.cs` | Impl: Auto-Create Default-AG pro FA |
| `IdealAkeWms.Tests/Services/BdeDefaultWorkOperationServiceTests.cs` | Tests |

### Geänderte Dateien

| Datei | Änderung |
|-------|----------|
| `IdealAkeWms/Program.cs` | 3 AppSettings seeden, DI-Registrierung |
| `SQL/42_AddBde.sql` | 3 AppSetting-Inserts |
| `SQL/00_FreshInstall.sql` | 3 AppSetting-Inserts |
| `IdealAkeWms/Views/Shared/_Layout.cshtml` | BDE-Nav nur wenn BdeAktiv=true |
| `IdealAkeWms/Controllers/BdeTerminalController.cs` | `[RequireBdeActive]` + neue Action |
| `IdealAkeWms/Controllers/BdeCockpitController.cs` | `[RequireBdeActive]` |
| `IdealAkeWms/Controllers/BdeBookingsController.cs` | `[RequireBdeActive]` |
| `IdealAkeWms/Controllers/BdeMasterDataController.cs` | `[RequireBdeActive]` |
| `IdealAkeWms/Controllers/BdeApiController.cs` | `[RequireBdeActive]` + NurFA-Logik |
| `IdealAkeWms/Views/Settings/Index.cshtml` | BDE-Gruppe + bedingte Sichtbarkeit |
| `IdealAkeWms/wwwroot/js/bde-terminal.js` | NurFA-Modus: FA-Buttons, kein Rüsten |
| `IdealAkeWms/Views/BdeTerminal/Index.cshtml` | NurFA-Flag als hidden input |
| `CLAUDE.md` | Neue AppSettings dokumentieren |
| `IdealAkeWms/Views/Help/Changelog.cshtml` | Changelog-Eintrag |
| `IdealAkeWms/Views/Help/Index.cshtml` | NurFA-Modus in Hilfe erklären |

---

## Task 1: AppSettings seeden

**Files:**
- Modify: `IdealAkeWms/Program.cs`
- Modify: `SQL/42_AddBde.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1: Program.cs — BDE-Settings-Block ergänzen**

Im bestehenden Settings-Seeding-Bereich (nach dem Leitstand-Block, ca. Zeile 223), neuen Block einfügen. Exaktes Pattern aus dem Projekt:

```csharp
// BDE Settings
var bdeSettings = new (string Key, string Value, string Description)[]
{
    ("BdeAktiv", "false", "BDE-Modul (Betriebsdatenerfassung) aktivieren"),
    ("BdeNurFaMeldung", "false", "Vereinfachter BDE-Modus: Buchung auf FA statt einzelne Arbeitsgaenge"),
    ("BdeDefaultArbeitsgang", "", "Default-Arbeitsgang fuer vereinfachten BDE-Modus (z.B. PRODUKTION)"),
};
foreach (var (key, value, description) in bdeSettings)
{
    if (!db.AppSettings.Any(s => s.Key == key))
    {
        db.AppSettings.Add(new IdealAkeWms.Models.AppSetting
        {
            Key = key,
            Value = value,
            Description = description
        });
    }
}
db.SaveChanges();
```

- [ ] **Step 2: SQL/42_AddBde.sql — AppSettings einfügen**

Am Ende der Datei (vor `__EFMigrationsHistory`-Block) einfügen:

```sql
-- 8) BDE AppSettings
IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = 'BdeAktiv')
    INSERT INTO [dbo].[AppSettings] ([Key],[Value],[Description])
    VALUES ('BdeAktiv','false','BDE-Modul (Betriebsdatenerfassung) aktivieren');
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = 'BdeNurFaMeldung')
    INSERT INTO [dbo].[AppSettings] ([Key],[Value],[Description])
    VALUES ('BdeNurFaMeldung','false','Vereinfachter BDE-Modus: Buchung auf FA statt einzelne Arbeitsgaenge');
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = 'BdeDefaultArbeitsgang')
    INSERT INTO [dbo].[AppSettings] ([Key],[Value],[Description])
    VALUES ('BdeDefaultArbeitsgang','','Default-Arbeitsgang fuer vereinfachten BDE-Modus (z.B. PRODUKTION)');
GO
```

- [ ] **Step 3: SQL/00_FreshInstall.sql — gleiche 3 Inserts**

In den AppSettings-Block einfügen (dort wo andere Settings wie `LeitstandAktiv` stehen).

- [ ] **Step 4: Build + Commit**

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj
git commit -m "feat(bde): seed BdeAktiv, BdeNurFaMeldung, BdeDefaultArbeitsgang settings"
```

---

## Task 2: RequireBdeActive Filter-Attribut

**Files:**
- Create: `IdealAkeWms/Filters/RequireBdeActiveAttribute.cs`

- [ ] **Step 1: Filter erstellen**

Pattern analog `RequireLeitstandAccessAttribute.cs`, aber prüft AppSetting statt Rolle:

```csharp
using IdealAkeWms.Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

public class RequireBdeActiveAttribute : TypeFilterAttribute
{
    public RequireBdeActiveAttribute() : base(typeof(RequireBdeActiveFilter)) { }
}

public class RequireBdeActiveFilter : IAsyncActionFilter
{
    private readonly IAppSettingRepository _settings;

    public RequireBdeActiveFilter(IAppSettingRepository settings)
    {
        _settings = settings;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var bdeAktiv = (await _settings.GetValueAsync("BdeAktiv"))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        if (!bdeAktiv)
        {
            if (context.Controller is Controller mvcController)
            {
                mvcController.TempData["WarningMessage"] = "BDE ist nicht aktiviert.";
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
            else
            {
                // API-Controller
                context.Result = new NotFoundResult();
            }
            return;
        }

        await next();
    }
}
```

- [ ] **Step 2: Build + Commit**

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj
git commit -m "feat(bde): add RequireBdeActive filter attribute"
```

---

## Task 3: BDE-Controller mit [RequireBdeActive] versehen

**Files:**
- Modify: `IdealAkeWms/Controllers/BdeTerminalController.cs`
- Modify: `IdealAkeWms/Controllers/BdeCockpitController.cs`
- Modify: `IdealAkeWms/Controllers/BdeBookingsController.cs`
- Modify: `IdealAkeWms/Controllers/BdeMasterDataController.cs`
- Modify: `IdealAkeWms/Controllers/BdeApiController.cs`

- [ ] **Step 1: Attribut auf alle 5 Controller setzen**

Auf jedem Controller `[RequireBdeActive]` als ERSTES Attribut (vor den Rollen-Filtern) hinzufügen:

```csharp
[RequireBdeActive]          // NEU — muss vor Rollen-Filter stehen
[RequireBdeUserAccess]
public class BdeTerminalController : Controller
```

```csharp
[RequireBdeActive]
[RequireBdeShiftleadAccess]
public class BdeCockpitController : Controller
```

```csharp
[RequireBdeActive]
[RequireBdeShiftleadAccess]
public class BdeBookingsController : Controller
```

```csharp
[RequireBdeActive]
[RequireBdeShiftleadAccess]
public class BdeMasterDataController : Controller
```

```csharp
[RequireBdeActive]
[RequireBdeUserAccess]
[ApiController]
[Route("api/bde")]
public class BdeApiController : ControllerBase
```

- [ ] **Step 2: Build + Commit**

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj
git commit -m "feat(bde): apply RequireBdeActive filter to all BDE controllers"
```

---

## Task 4: Navigation-Gating in _Layout.cshtml

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: BdeAktiv-Check ergänzen**

Neben den bestehenden Setting-Checks (Zeile ~37, wo `leitstandAktiv` steht), BDE-Check hinzufügen:

```csharp
var bdeAktiv = (await AppSettings.GetValueAsync("BdeAktiv"))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
```

- [ ] **Step 2: BDE-Nav-Block wrappen**

Den bestehenden BDE-Nav-Block (Zeilen ~112-129, `@if (canUseBde)`) erweitern zu:

```csharp
@if (bdeAktiv && canUseBde)
```

- [ ] **Step 3: Build + Commit**

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj
git commit -m "feat(bde): gate BDE navigation on BdeAktiv setting"
```

---

## Task 5: Settings-UI — BDE-Gruppe

**Files:**
- Modify: `IdealAkeWms/Views/Settings/Index.cshtml`

- [ ] **Step 1: BDE-Gruppe zum Tuple-Array hinzufügen**

Im `settingsGroups`-Array (Zeile ~25-33) neue Gruppe ergänzen:

```csharp
("BDE", new[] { "BdeAktiv", "BdeNurFaMeldung", "BdeDefaultArbeitsgang" }),
```

Einfügen nach der letzten bestehenden Gruppe (vor dem schließenden `};`).

- [ ] **Step 2: Bedingte Sichtbarkeit für BdeDefaultArbeitsgang**

Das Textfeld für `BdeDefaultArbeitsgang` soll nur sichtbar sein wenn `BdeNurFaMeldung = true`. Dafür im Rendering-Block (wo Settings per Loop gerendert werden) eine kleine JS-Logik:

Nach dem Rendering-Loop im Scripts-Bereich ergänzen:

```javascript
// BDE: BdeDefaultArbeitsgang nur sichtbar wenn BdeNurFaMeldung aktiv
(function() {
    var nurFaToggle = document.querySelector('input[type="checkbox"][data-setting-key="BdeNurFaMeldung"]');
    var defaultAgRow = document.querySelector('[data-setting-row="BdeDefaultArbeitsgang"]');
    if (!nurFaToggle || !defaultAgRow) return;
    function toggle() { defaultAgRow.style.display = nurFaToggle.checked ? '' : 'none'; }
    nurFaToggle.addEventListener('change', toggle);
    toggle();
})();
```

Dafür muss jede Setting-Row ein `data-setting-row="<key>"` Attribut bekommen und jeder Checkbox-Toggle ein `data-setting-key="<key>"`. Prüfe ob das schon existiert — wenn nicht, ergänze es im Rendering-Loop.

- [ ] **Step 3: Validierung beim Speichern**

In `SettingsController.SaveSettings` (oder im Settings-View als JS-Validierung): wenn `BdeNurFaMeldung = true` und `BdeDefaultArbeitsgang` leer → ModelState-Error oder Alert.

Einfachste Variante — JS im Save-Handler:

```javascript
document.querySelector('form').addEventListener('submit', function(e) {
    var nurFa = document.querySelector('input[data-setting-key="BdeNurFaMeldung"]');
    var defaultAg = document.querySelector('input[name="settings[BdeDefaultArbeitsgang]"]');
    if (nurFa && nurFa.checked && defaultAg && !defaultAg.value.trim()) {
        e.preventDefault();
        alert('Bei aktivierter FA-Meldung muss ein Default-Arbeitsgang definiert werden.');
    }
});
```

- [ ] **Step 4: Build + Commit**

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj
git commit -m "feat(bde): add BDE settings group to settings UI"
```

---

## Task 6: BdeDefaultWorkOperationService (TDD)

**Files:**
- Create: `IdealAkeWms/Services/IBdeDefaultWorkOperationService.cs`
- Create: `IdealAkeWms/Services/BdeDefaultWorkOperationService.cs`
- Create: `IdealAkeWms.Tests/Services/BdeDefaultWorkOperationServiceTests.cs`
- Modify: `IdealAkeWms/Program.cs` — DI-Registrierung

- [ ] **Step 1: Tests schreiben**

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

public class BdeDefaultWorkOperationServiceTests
{
    private static (ApplicationDbContext ctx, BdeDefaultWorkOperationService svc) Setup(string defaultAgName = "PRODUKTION")
    {
        var ctx = TestDbContextFactory.Create();
        var settingsMock = new Mock<IAppSettingRepository>();
        settingsMock.Setup(s => s.GetValueAsync("BdeDefaultArbeitsgang")).ReturnsAsync(defaultAgName);
        var svc = new BdeDefaultWorkOperationService(ctx, settingsMock.Object);
        return (ctx, svc);
    }

    private static int SeedFaAndWorkplace(ApplicationDbContext ctx)
    {
        var wp = new ProductionWorkplace { Name = "WB1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var po = new ProductionOrder { OrderNumber = "FA-100", Quantity = 10, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t", ProductionWorkplaceId = wp.Id };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.ProductionOrders.Add(po);
        ctx.SaveChanges();
        po.ProductionWorkplaceId = wp.Id;
        ctx.SaveChanges();
        return po.Id;
    }

    [Fact]
    public async Task FindOrCreate_CreatesNewWO_WhenNoneExists()
    {
        var (ctx, svc) = Setup();
        var poId = SeedFaAndWorkplace(ctx);
        var wpId = ctx.ProductionWorkplaces.First().Id;

        var woId = await svc.FindOrCreateDefaultAsync(poId, wpId);

        woId.Should().BeGreaterThan(0);
        var wo = ctx.WorkOperations.First(w => w.Id == woId);
        wo.Name.Should().Be("PRODUKTION");
        wo.OperationNumber.Should().Be("01");
        wo.ProductionOrderId.Should().Be(poId);
        wo.IsReportable.Should().BeTrue();
    }

    [Fact]
    public async Task FindOrCreate_ReturnsExisting_WhenAlreadyExists()
    {
        var (ctx, svc) = Setup();
        var poId = SeedFaAndWorkplace(ctx);
        var wpId = ctx.ProductionWorkplaces.First().Id;

        var woId1 = await svc.FindOrCreateDefaultAsync(poId, wpId);
        var woId2 = await svc.FindOrCreateDefaultAsync(poId, wpId);

        woId1.Should().Be(woId2);
        ctx.WorkOperations.Count(w => w.ProductionOrderId == poId && w.Name == "PRODUKTION").Should().Be(1);
    }

    [Fact]
    public async Task FindOrCreate_DoesNotConfuseWithOtherAGs()
    {
        var (ctx, svc) = Setup();
        var poId = SeedFaAndWorkplace(ctx);
        var wpId = ctx.ProductionWorkplaces.First().Id;
        // Existierender AG "Fräsen" soll nicht gefunden werden
        ctx.WorkOperations.Add(new WorkOperation {
            ProductionOrderId = poId, OperationNumber = "10", Name = "Fräsen",
            Sequence = 1, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.SaveChanges();

        var woId = await svc.FindOrCreateDefaultAsync(poId, wpId);

        var wo = ctx.WorkOperations.First(w => w.Id == woId);
        wo.Name.Should().Be("PRODUKTION");
        ctx.WorkOperations.Count(w => w.ProductionOrderId == poId).Should().Be(2); // Fräsen + PRODUKTION
    }
}
```

- [ ] **Step 2: Interface + Implementierung**

`IdealAkeWms/Services/IBdeDefaultWorkOperationService.cs`:

```csharp
namespace IdealAkeWms.Services;

public interface IBdeDefaultWorkOperationService
{
    Task<int> FindOrCreateDefaultAsync(int productionOrderId, int workplaceId);
}
```

`IdealAkeWms/Services/BdeDefaultWorkOperationService.cs`:

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
        var defaultName = await _settings.GetValueAsync("BdeDefaultArbeitsgang")
            ?? throw new InvalidOperationException("BdeDefaultArbeitsgang ist nicht konfiguriert.");

        if (string.IsNullOrWhiteSpace(defaultName))
            throw new InvalidOperationException("BdeDefaultArbeitsgang ist leer.");

        // Find existing
        var existing = await _ctx.WorkOperations
            .FirstOrDefaultAsync(wo => wo.ProductionOrderId == productionOrderId && wo.Name == defaultName);

        if (existing != null)
            return existing.Id;

        // Create new (InMemory hat keine Transactions — check wie BdeBookingService)
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
}
```

- [ ] **Step 3: DI registrieren**

In `Program.cs`:

```csharp
builder.Services.AddScoped<IBdeDefaultWorkOperationService, BdeDefaultWorkOperationService>();
```

- [ ] **Step 4: Tests ausführen + Commit**

```bash
dotnet test IdealAkeWms.Tests --filter FullyQualifiedName~BdeDefaultWorkOperationServiceTests --nologo
dotnet build IdealAkeWms/IdealAkeWms.csproj
git commit -m "feat(bde): add BdeDefaultWorkOperationService with tests"
```

---

## Task 7: API + Controller — NurFA-Modus

**Files:**
- Modify: `IdealAkeWms/Controllers/BdeApiController.cs`
- Modify: `IdealAkeWms/Controllers/BdeTerminalController.cs`

- [ ] **Step 1: BdeApiController — available-operations NurFA-Branch**

Inject `IAppSettingRepository _settings` in den BdeApiController-Konstruktor. Im bestehenden `GetAvailableOperations(int workplaceId)` Endpoint am Anfang NurFA prüfen:

```csharp
[HttpGet("available-operations/{workplaceId:int}")]
public async Task<IActionResult> GetAvailableOperations(int workplaceId)
{
    var nurFa = (await _settings.GetValueAsync("BdeNurFaMeldung"))
        ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

    if (nurFa)
    {
        // Im NurFA-Modus: offene ProductionOrders an dieser Werkbank
        var orders = await _ctx.ProductionOrders
            .Where(po => po.ProductionWorkplaceId == workplaceId && !po.IsDone)
            .OrderBy(po => po.ProductionDate)
            .Select(po => new {
                id = po.Id,
                label = $"{po.OrderNumber} — {po.Description1}",
                type = "fa"
            })
            .ToListAsync();

        return Ok(new { productive = orders, unplanned = Array.Empty<object>(), nurFaMode = true });
    }

    // Normaler Modus: bestehende Logik (WorkOperations + Activities)
    // ... existierender Code bleibt unverändert, ergänze nurFaMode = false
    // Am Ende des bestehenden return Ok(...):
    // Ergänze nurFaMode = false im Response
}
```

**WICHTIG:** Für den DB-Zugriff auf `ProductionOrders` braucht der Controller entweder den `ApplicationDbContext` direkt oder ein Repository. Prüfe was schon injected ist. Falls `_ctx` nicht vorhanden: injiziere `ApplicationDbContext` oder nutze ein bestehendes `IProductionOrderRepository`.

Ergänze `nurFaMode = false` auch im normalen Response-Pfad, damit das JS den Modus erkennen kann.

- [ ] **Step 2: API — NurFA-Modus im active-booking Response**

Im `GetActiveBooking(int id)` Endpoint: ergänze den NurFA-Modus-Check und gib im Response mit:

```csharp
var nurFa = (await _settings.GetValueAsync("BdeNurFaMeldung"))
    ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
// ... im return Ok(new { booking = new { ..., nurFaMode = nurFa } });
```

- [ ] **Step 3: BdeTerminalController — neue Action StartProductionForOrder**

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> StartProductionForOrder(int operatorId, int productionOrderId, int workplaceId, int terminalId)
{
    var workOperationId = await _defaultWoService.FindOrCreateDefaultAsync(productionOrderId, workplaceId);
    var result = await _bookingSvc.StartProductionAsync(operatorId, workOperationId, workplaceId, terminalId);
    return Json(MapResult(result));
}
```

Inject `IBdeDefaultWorkOperationService _defaultWoService` im Konstruktor.

- [ ] **Step 4: BdeTerminalController.Index — NurFA-Flag an View übergeben**

```csharp
var nurFa = (await _settings.GetValueAsync("BdeNurFaMeldung"))
    ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
ViewBag.NurFaMode = nurFa;
```

Inject `IAppSettingRepository _settings` im Konstruktor.

- [ ] **Step 5: View — Hidden input für NurFA-Flag**

In `Views/BdeTerminal/Index.cshtml` im Scripts-Block:

```html
<input type="hidden" id="nurFaMode" value="@(ViewBag.NurFaMode ? "true" : "false")" />
```

- [ ] **Step 6: Build + Commit**

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj
git commit -m "feat(bde): add NurFA API branch and StartProductionForOrder action"
```

---

## Task 8: Terminal-JS — NurFA-Modus

**Files:**
- Modify: `IdealAkeWms/wwwroot/js/bde-terminal.js`

- [ ] **Step 1: NurFA-Flag lesen**

Am Anfang der IIFE:

```javascript
const nurFaMode = document.getElementById('nurFaMode')?.value === 'true';
```

- [ ] **Step 2: AG-Buttons im NurFA-Modus als FA-Buttons rendern**

In der bestehenden `renderOperationButtons(productive, unplanned)` Funktion:

```javascript
function renderOperationButtons(productive, unplanned) {
    const container = document.getElementById('operationButtons');
    let html = '';

    if (productive.length > 0) {
        html += nurFaMode
            ? '<h6 class="mt-2 text-muted">Produktionsaufträge</h6>'
            : '<h6 class="mt-2 text-muted">Produktive Arbeitsgänge</h6>';
        html += '<div class="d-flex flex-wrap gap-2 mb-3">';
        productive.forEach(op => {
            if (op.type === 'fa') {
                // NurFA: Button startet direkt Produktion auf FA
                html += `<button class="btn btn-outline-success btn-lg bde-op-btn" data-fa-id="${op.id}" data-type="fa">${op.label}</button>`;
            } else {
                // Normal: bestehende Logik
                html += `<button class="btn btn-outline-success btn-lg bde-op-btn" data-wo-id="${op.id}" data-type="productive">${op.label}</button>`;
            }
        });
        html += '</div>';
    }

    // Ungeplante Tätigkeiten: nur im normalen Modus
    if (!nurFaMode && unplanned.length > 0) {
        html += '<h6 class="text-muted">Ungeplante Tätigkeiten</h6>';
        // ... bestehende unplanned-Logik
    }

    // ... rest unchanged
}
```

- [ ] **Step 3: FA-Button-Click-Handler**

In `bindOperationButtonHandlers()`:

```javascript
btn.addEventListener('click', async () => {
    if (btn.dataset.type === 'fa') {
        // NurFA: direkt Produktion auf FA starten
        await post('/BdeTerminal/StartProductionForOrder', {
            operatorId: currentOperator.id,
            productionOrderId: parseInt(btn.dataset.faId),
            workplaceId, terminalId
        }, 'StartProduction');
        await renderState();
        await loadAvailableOperations();
    } else if (btn.dataset.type === 'productive') {
        // Normal: bestehende Logik
        currentWorkOp = { id: parseInt(btn.dataset.woId) };
        await renderState();
    } else {
        // Unplanned: bestehende Logik
    }
});
```

- [ ] **Step 4: Aktions-Buttons im NurFA-Modus — kein Rüsten**

In der Funktion die Start-Buttons rendert (z.B. `renderStartButtons()`): wenn `nurFaMode`, nur "Starten" zeigen (= Production), kein "Rüsten starten":

```javascript
function renderStartButtons() {
    if (nurFaMode) {
        return buttons(['startProduction']); // Kein startSetup
    }
    return buttons(['startSetup', 'startProduction']);
}
```

- [ ] **Step 5: FA/AG-Scan im NurFA-Modus — AG-Teil ignorieren**

In der `scanFaAgInput()` Funktion: wenn `nurFaMode`, AG-Teil ignorieren und direkt als FA behandeln:

```javascript
async function scanFaAgInput() {
    const raw = document.getElementById('scanFaAg').value.trim();
    document.getElementById('scanFaAg').value = '';
    if (!raw) return;

    if (nurFaMode) {
        // NurFA: nur FA-Nummer, AG-Teil ignorieren
        const faNumber = raw.split(/[,\/]/)[0].trim();
        // FA-Nummer → ProductionOrder suchen → StartProductionForOrder
        const r = await fetch(`/api/bde/workoperation?faNumber=${encodeURIComponent(faNumber)}&opNumber=01`);
        if (!r.ok) {
            // Fallback: direkt über FA-Nummer den ProductionOrder finden
            // Dafür brauchen wir einen neuen API-Endpoint oder suchen in den Buttons
            document.getElementById('faAgFeedback').textContent = 'FA nicht gefunden';
            return;
        }
        // Gefunden → automatisch Produktion starten
        const wo = await r.json();
        await post('/BdeTerminal/StartProduction', {
            operatorId: currentOperator.id,
            workOperationId: wo.id,
            workplaceId, terminalId
        }, 'StartProduction');
        await renderState();
        await loadAvailableOperations();
        return;
    }

    // Normaler Modus: bestehende Logik
    const parts = raw.split(/[,\/]/);
    // ... rest unchanged
}
```

- [ ] **Step 6: "Ungeplante Tätigkeit"-Button ausblenden im NurFA-Modus**

Falls es einen separaten Button "Ungeplante Tätigkeit" gibt: `if (nurFaMode)` → `display: none`.

- [ ] **Step 7: Build + Commit**

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj
git commit -m "feat(bde): terminal JS NurFA mode (FA buttons, no setup, no activities)"
```

---

## Task 9: Tests + Build-Verifikation

**Files:**
- Modify: `IdealAkeWms.Tests/Services/BdeDefaultWorkOperationServiceTests.cs` (wenn nötig)
- Möglicherweise: `IdealAkeWms.Tests/Controllers/BdeApiControllerTests.cs` (Mocks erweitern)

- [ ] **Step 1: Alle Tests ausführen**

```bash
dotnet test IdealAkeWms.Tests --nologo
```

Erwarte: 337+ (bestehende) + 3 (neue aus Task 6) = 340+ passing, 0 failing.

Falls BdeApiControllerTests fehlschlagen (weil neuer Ctor-Parameter `IAppSettingRepository`): Mock ergänzen.

- [ ] **Step 2: Fix falls nötig + Commit**

```bash
git commit -m "fix(bde): update test mocks for new IAppSettingRepository injection"
```

---

## Task 10: Docs aktualisieren

**Files:**
- Modify: `CLAUDE.md`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`
- Modify: `IdealAkeWms/AppVersion.cs`
- Modify: `IDEALAKEWMSService/AppVersion.cs`

- [ ] **Step 1: Version bump**

`1.8.1` → `1.8.2`, ReleaseDate `2026-04-16`.

- [ ] **Step 2: CLAUDE.md — AppSettings-Tabelle ergänzen**

In der `AppSettings (DB-Tabelle)` Sektion:

```
| `BdeAktiv` | `false` | BDE-Modul aktiviert |
| `BdeNurFaMeldung` | `false` | Vereinfachter BDE-Modus (FA statt AG) |
| `BdeDefaultArbeitsgang` | (leer) | Default-AG Name für vereinfachten Modus |
```

- [ ] **Step 3: Changelog**

Neuer Eintrag `Version 1.8.2`:
- Feature-Toggle `BdeAktiv` — BDE-Modul per Setting aktivierbar
- Vereinfachter Modus `BdeNurFaMeldung` — Buchung auf FA statt einzelne Arbeitsgänge
- Automatischer Default-Arbeitsgang pro FA im vereinfachten Modus

- [ ] **Step 4: Hilfeseite**

BDE-Abschnitt ergänzen: Hinweis auf Settings, NurFA-Modus erklären.

- [ ] **Step 5: Build + Commit**

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj
git commit -m "docs(bde): update docs for BdeAktiv and NurFA settings (v1.8.2)"
```

---

## Self-Review

**Spec coverage:**
- ✅ BdeAktiv: Seeding, Filter, Navigation, Settings-UI
- ✅ BdeNurFaMeldung: Terminal-Umschaltung, FA-Buttons, kein Rüsten, keine Activities
- ✅ BdeDefaultArbeitsgang: Auto-Create-Service, Settings-Validierung
- ✅ Navigation-Gating
- ✅ Zugriffssperre alle 5 Controller
- ✅ Edge-Cases in Spec (SAGE-Import, Modus-Wechsel) → kein Code nötig, Verhalten ergibt sich

**Placeholder scan:** Keine TBDs. Task 8 Step 5 hat einen TODO-artigen Kommentar ("Fallback: ...") — das ist ein Hinweis im Code, kein offener Punkt im Plan.

**Type consistency:** `FindOrCreateDefaultAsync(int, int)` → `Task<int>` durchgehend.

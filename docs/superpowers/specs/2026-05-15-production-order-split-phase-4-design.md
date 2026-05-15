# ProductionOrder-Split — Phase 4 FA-Vervollstaendigung — Design Spec

**Datum:** 2026-05-15
**Branch:** `refactor/fa-logic` (Fortsetzung nach Phase 1+2; aktueller HEAD `72e036a`)
**Status:** Approved → Plan
**Phase:** Phase 4 von 5 (siehe Roadmap). AppVersion-Bump auf `1.13.0`.
**Roadmap-Referenz:** `docs/superpowers/specs/2026-05-12-production-order-split-roadmap.md`, Sektion 8.
**Phase-1-Referenz:** `docs/superpowers/specs/2026-05-12-production-order-split-phase-1-design.md` (Schema-Refactor — `ProductionOrderAssemblyGroup` + `ProductionOrderAssemblyGroupSpec` Entities + Toggle-API `/api/assembly-groups/toggle-applicable`).
**Phase-2-Referenz:** `docs/superpowers/specs/2026-05-12-production-order-split-phase-2-design.md` (View-Split-Praezedenz, Permission-Filter-Pattern, `RequirePickingOrLeitstandAccess`).

---

## 1. Problemstellung

Phase 1 hat das Schema gelegt: `ProductionOrderAssemblyGroups` (5 Zeilen pro FA, GroupKey `VK`/`VL`/`VE`/`VT`/`VA`) und `ProductionOrderAssemblyGroupSpecs` (1:N zu AssemblyGroup, in Phase 1 leer angelegt). Phase 2 hat den View-Split etabliert: schlanker `ProductionOrders/Index` + reicher `PickingLeitstand/Index`. Die Frage **"welche konkreten Ausprägungen sollen am Vormontageplatz verbaut werden"** ist heute jedoch noch nirgendwo pflegbar.

User-Quote (Roadmap Sektion 4.5):

> *"zum FA sollen pro Vormontageplatz zb. (VE) Elektro, (VL) Lüfter ... zusätzliche Merkmalsausprägungen pro Vormontageplatz pro Fertigungsauftrag hinterlegt werden können. ... ich wähle zb. VL (Lüfter) und pflege die definierten Lüfter-Ausprägungen."*

**Heute:**
- `ProductionOrderAssemblyGroupSpecs` ist als Tabelle vorhanden, hat aber keine Repository-Klasse, keinen Controller, keine View. Der `IsApplicable`-Toggle pro Gruppe ist auf `PickingLeitstand/Index` als 5 Bool-Spalten (`HasCooling`/`HasFan`/...) sichtbar, dort aber **nicht** mit Spec-Pflege verknuepft.
- Wer entscheidet, welche Lueft-er-Ausprägungen verbaut werden, muss heute aus Stueckliste, Kundenanforderung und Erfahrung zusammensuchen — kein dedizierter Workflow im WMS.
- Die `IsCompleted`-Flag auf `ProductionOrderAssemblyGroup` ist heute immer `false`, weil es keinen UI-Pfad zum Setzen gibt.

**Ziel von Phase 4:**
- Dediziertes UI-Modul **"FA-Vervollstaendigung"** mit eigener Rolle und eigenem Navigations-Eintrag.
- Pro FA eine Detail-Seite mit 5 Tabs (VK/VL/VE/VT/VA). Pro Tab Pflege der Specs (Artikel + Beschreibung + Menge + Notizen) sowie der zwei Status-Flags pro Gruppe (`IsApplicable`, `IsCompleted`).
- Phase 5 (Arbeitsplatz-BOM-View) kann anschliessend die gepflegten Specs read-only fuer die Werkbank-Sicht uebernehmen.

## 2. Ziele

1. **Neue Rolle `fa_completion`** im `Role`-Seed, im `RoleKeys`-Konstantenklassen-Block und in der Berechtigungstabelle (CLAUDE.md). Analog zu `picking`, `tracking`, `bde_user`.
2. **Neuer Permission-Filter `RequireFaCompletionAccess`** (admin ∨ fa_completion), Klassenebene auf dem neuen `FaCompletionController`.
3. **Toggle-API-Erweiterung:** Der bestehende `[RequirePickingAccess]`-Schutz auf `/api/assembly-groups/toggle-applicable` (Phase 1) wird durch einen neuen `RequirePickingOrFaCompletionAccess`-Filter ersetzt, sodass auch `fa_completion`-User toggeln duerfen — ohne Picking-Rolle haben zu muessen. Phase-2-Use-Case (PickingLeitstand-Toggle) bleibt voll funktional.
4. **Neuer MVC-Controller `FaCompletionController`** mit Actions:
   - `Index` — schlanke FA-Liste mit Link zur Edit-Page (Filterbar nach FA-Nr./Artikel/Kunde/ShowDone).
   - `Edit/{id}` — FA-Detail mit Bootstrap-`nav-tabs` (VK/VL/VE/VT/VA). Aktiver Tab via Query-Param `tab=VK|VL|VE|VT|VA`.
   - `AddSpec` (POST) — Spec anlegen.
   - `EditSpec` (POST) — Spec aktualisieren.
   - `DeleteSpec` (POST) — Spec loeschen.
   - `ToggleIsCompleted` (POST) — `IsCompleted` flippen + `CompletedAt`/`CompletedBy` setzen.
   - `ToggleIsApplicable` (POST) — leitet auf bestehende API `/api/assembly-groups/toggle-applicable` weiter (kein eigener Endpoint, Front-End nutzt direkt die JSON-API).
5. **Neues Repository `IProductionOrderAssemblyGroupSpecRepository` + Impl** — CRUD plus Bulk-Lookup nach AssemblyGroupIds (fuer die Edit-Page).
6. **Neue ViewModels** — `FaCompletionListViewModel` + `FaCompletionListItem`, `FaCompletionEditViewModel` + `AssemblyGroupTabViewModel` + `AssemblyGroupSpecFormModel`.
7. **Neue Views** — `Views/FaCompletion/Index.cshtml` (FA-Liste), `Views/FaCompletion/Edit.cshtml` (Tabs + Spec-Tabelle pro Tab). Optional inline Spec-Form (siehe 8.4).
8. **Nav-Bar-Eintrag "FA-Vervollstaendigung"** unter dem bestehenden "Kommissionierung"-Dropdown bzw. als eigenes Top-Level-Item, sichtbar fuer `fa_completion` ∨ `admin`.
9. **ArticleId-Select2-Reuse** — bestehender Endpoint `GET /api/articles/search` (siehe [`ArticlesApiController.cs:17-26`](IdealAkeWms/Controllers/ArticlesApiController.cs#L17-L26)) wird genutzt. Kein neuer Endpoint.
10. **Tests** — Controller-Tests fuer alle 6 POST-Actions, Repository-Tests fuer CRUD-Pfad, Filter-Test fuer `RequireFaCompletionAccess`, Filter-Test fuer `RequirePickingOrFaCompletionAccess`.
11. **Doku** — AppVersion 1.13.0, Changelog-Card, Help/Index-Section, CLAUDE.md-Updates (neue Rolle, neue Filter, neuer Fallstrick), 5 neue Szenarien in `docs/TESTSZENARIEN.md`.

## 3. Out-of-Scope (Phase 4)

- **Schema-Aenderungen** — Phase 1 hat alle Tabellen angelegt. Phase 4 nutzt vorhandene Tabellen, kein neues Migration-Skript.
- **Phase 5 (Arbeitsplatz-BOM-View)** — read-only Werkbank-Sicht der gepflegten Specs. Eigene Spec + Plan nach Phase 4 Live-Verifikation.
- **BOM-Import** — Phase 4 erlaubt manuelle Pflege der Specs. Auto-Befuellung aus Stueckliste oder OSEON ist ein optionales Folge-Feature, nicht in Phase 4.
- **AssemblyGroup-Spec-Audit-Log** — Auditfelder (`CreatedAt`/`CreatedBy`/`ModifiedAt`/`ModifiedBy`) reichen. Kein dediziertes History-Log.
- **Mehrsprachigkeit** — UI bleibt deutsch wie der Rest der Anwendung.
- **Spec-Reihenfolge per Drag&Drop** — `SortOrder` bleibt manuelles Number-Input. Drag&Drop kommt ggf. spaeter.
- **`OseonRueckmeldungAktiv`-Anbindung** — Spec-Pflege loest keine OSEON-Rueckmeldungen aus.

## 4. Architektur-Uebersicht

```
Heute (nach Phase 1+2):
  ProductionOrderAssemblyGroup (Tabelle, 5 Zeilen/FA, eager-created)
    ├── IsApplicable: Toggle via /api/assembly-groups/toggle-applicable
    │                  (Phase 1, [RequirePickingAccess])
    │                  → genutzt von PickingLeitstand/Index Checkbox
    ├── IsCompleted:   nie gesetzt (kein UI-Pfad)
    └── ICollection<Specs>: Tabelle existiert, immer leer
  ProductionOrderAssemblyGroupSpec (Tabelle, leer)

Nach Phase 4:
  FaCompletionController                  [RequireFaCompletionAccess]
    ├── Index                             schlanke FA-Liste mit Edit-Link
    ├── Edit/{id}?tab=VK                  Tabs VK/VL/VE/VT/VA mit Spec-Liste pro Tab
    │     ├── IsApplicable-Toggle         → JSON-Call /api/assembly-groups/toggle-applicable
    │     ├── IsCompleted-Toggle          → ToggleIsCompleted (eigene Action)
    │     └── Spec-Tabelle                Add/Edit/Delete via Form-Posts
    ├── AddSpec    (POST)
    ├── EditSpec   (POST)
    ├── DeleteSpec (POST)
    └── ToggleIsCompleted (POST)

  IProductionOrderAssemblyGroupSpecRepository
    ├── GetByIdAsync(int)
    ├── GetByAssemblyGroupIdAsync(int)
    ├── GetByAssemblyGroupIdsAsync(IEnumerable<int>)  // Bulk fuer Edit-View
    ├── AddAsync(ProductionOrderAssemblyGroupSpec)
    ├── UpdateAsync(ProductionOrderAssemblyGroupSpec)
    └── DeleteAsync(int)

  /api/assembly-groups/toggle-applicable
    [RequirePickingOrFaCompletionAccess]  // war [RequirePickingAccess]
```

**Datenfluss Edit-Page (`GET /FaCompletion/Edit/123?tab=VL`):**

```
GET → FaCompletionController.Edit(id=123, tab="VL")
     → IProductionOrderRepository.GetByIdAsync(123)                 (Master)
     → IProductionOrderAssemblyGroupRepository.GetByProductionOrderIdAsync(123) (5 Zeilen)
     → IProductionOrderAssemblyGroupSpecRepository
            .GetByAssemblyGroupIdsAsync(groupIds)                   (Specs gruppiert)
     → Mapping → FaCompletionEditViewModel { Order, Tabs: [5x AssemblyGroupTabViewModel] }
     → return View("Edit", vm)

View rendert:
  Top: FA-Header (FA-Nr., Kunde, Artikel, Termine, IsDone)
  nav-tabs mit 5 Eintraegen (VK/VL/VE/VT/VA, ActiveTab=VL)
  Pro Tab (server-rendered partial):
    - IsApplicable-Checkbox  → JS POST /api/assembly-groups/toggle-applicable
    - IsCompleted-Checkbox   → Form-POST /FaCompletion/ToggleIsCompleted
    - Spec-Tabelle           (Artikel | Beschreibung | Menge | Notizen | Sort | Aktionen)
    - Add-Form (inline-Row am Tabellen-Ende ODER Modal — siehe 8.4)
```

## 5. Rolle, Permission-Filter, Toggle-API

### 5.1 Neue Rolle `fa_completion`

Konstante in `IdealAkeWms/Models/RoleKeys.cs`:

```csharp
public const string FaCompletion = "fa_completion";
```

Seed in `Program.cs` (im `defaultRoles`-Array nach `Leitstand`, vor den BDE-Rollen):

```csharp
(RoleKeys.FaCompletion, "FA-Vervollstaendigung",
    "Merkmalsauspraegungen pro Vormontageplatz pro FA pflegen", 80),
```

`Role.AdGroup` bleibt optional (NULL beim Seed). Admin kann via UsersController eine AD-Gruppe nachpflegen.

CLAUDE.md-Rollen-Tabelle bekommt einen neuen Eintrag direkt nach `leitstand`:

```markdown
| `fa_completion` | FA-Vervollstaendigung: Merkmalsauspraegungen pro Vormontageplatz pro FA pflegen |
```

### 5.2 Neuer Filter `RequireFaCompletionAccess`

Analog zu `RequirePickingAccessFilter` (siehe [`RequirePickingAccessAttribute.cs:12-31`](IdealAkeWms/Filters/RequirePickingAccessAttribute.cs#L12-L31)). Datei `IdealAkeWms/Filters/RequireFaCompletionAccessAttribute.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

public class RequireFaCompletionAccessAttribute : TypeFilterAttribute
{
    public RequireFaCompletionAccessAttribute()
        : base(typeof(RequireFaCompletionAccessFilter)) { }
}

public class RequireFaCompletionAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireFaCompletionAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanCompleteFaAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }
        await next();
    }
}
```

Dazu in `ICurrentUserService` + `CurrentUserService`:

```csharp
// ICurrentUserService.cs
Task<bool> CanCompleteFaAsync();

// CurrentUserService.cs
public async Task<bool> CanCompleteFaAsync()
    => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.FaCompletion);
```

### 5.3 Neuer Filter `RequirePickingOrFaCompletionAccess`

Wird auf `/api/assembly-groups/toggle-applicable` angewendet, sodass sowohl Phase-2-PickingLeitstand-User (Picking-Rolle) als auch Phase-4-FA-Vervollstaendigungs-User (fa_completion-Rolle) den Toggle ausloesen koennen.

```csharp
public class RequirePickingOrFaCompletionAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;
    public RequirePickingOrFaCompletionAccessFilter(ICurrentUserService s) { _currentUserService = s; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanPickAsync()
            && !await _currentUserService.CanCompleteFaAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }
        await next();
    }
}
```

**Aenderung an `AssemblyGroupsApiController` (Phase-1-Klasse):**

```diff
-[RequirePickingAccess]
+[RequirePickingOrFaCompletionAccess]
 public class AssemblyGroupsApiController : ControllerBase { ... }
```

**Begruendung gegen "neuer Endpoint im FaCompletionController":** Wir wollen dasselbe Repo-Verhalten + Audit-Felder + Whitelist (`AllowedGroupKeys`). Dupliziertes Backend = Pflege-Risiko. Filter erweitern ist die schlanke Loesung.

CLAUDE.md-Berechtigungstabelle bekommt zwei neue Zeilen:

```markdown
| `[RequireFaCompletionAccess]`               | admin, fa_completion                    | FaCompletionController (class-level) |
| `[RequirePickingOrFaCompletionAccess]`      | admin, picking ODER fa_completion       | AssemblyGroupsApiController          |
```

Und der vorhandene Eintrag `[RequirePickingAccess]` wird im "Angewendet auf"-Feld um `AssemblyGroupsApiController` reduziert (er ist dort durch `RequirePickingOrFaCompletionAccess` ersetzt). Detail: PickingStatusApiController + BdeStatusApiController bleiben `[RequirePickingAccess]`.

### 5.4 Berechtigungsmatrix

| Action | Filter | admin | fa_completion | picking | tracking | leitstand |
|---|---|:---:|:---:|:---:|:---:|:---:|
| `FaCompletion/Index` | `RequireFaCompletionAccess` | ✓ | ✓ | — | — | — |
| `FaCompletion/Edit/{id}` | `RequireFaCompletionAccess` | ✓ | ✓ | — | — | — |
| `FaCompletion/AddSpec` | `RequireFaCompletionAccess` | ✓ | ✓ | — | — | — |
| `FaCompletion/EditSpec` | `RequireFaCompletionAccess` | ✓ | ✓ | — | — | — |
| `FaCompletion/DeleteSpec` | `RequireFaCompletionAccess` | ✓ | ✓ | — | — | — |
| `FaCompletion/ToggleIsCompleted` | `RequireFaCompletionAccess` | ✓ | ✓ | — | — | — |
| `POST /api/assembly-groups/toggle-applicable` | `RequirePickingOrFaCompletionAccess` | ✓ | ✓ | ✓ | — | — |
| `PickingLeitstand/Index` (Phase 2) | `RequirePickingOrLeitstandAccess` | ✓ | — | ✓ | — | ✓ |
| `ProductionOrders/Index` (Phase 2) | `RequirePickingOrTrackingOrLeitstandAccess` | ✓ | — | ✓ | ✓ | ✓ |

Ein User mit kombinierter Rolle `picking` + `fa_completion` sieht beide Module. Reine `fa_completion`-User sehen weder PickingLeitstand noch Picker-Worklist — sie pflegen ausschliesslich Specs.

## 6. Repository: `IProductionOrderAssemblyGroupSpecRepository`

Datei `IdealAkeWms/Data/Repositories/IProductionOrderAssemblyGroupSpecRepository.cs`:

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionOrderAssemblyGroupSpecRepository
{
    Task<ProductionOrderAssemblyGroupSpec?> GetByIdAsync(int id);

    /// <summary>Specs einer AssemblyGroup, sortiert nach SortOrder, Id.</summary>
    Task<List<ProductionOrderAssemblyGroupSpec>> GetByAssemblyGroupIdAsync(int assemblyGroupId);

    /// <summary>Bulk-Lookup fuer Edit-View (5 Tabs pro FA): liefert Specs gruppiert per AssemblyGroupId.</summary>
    Task<Dictionary<int, List<ProductionOrderAssemblyGroupSpec>>>
        GetByAssemblyGroupIdsAsync(IEnumerable<int> assemblyGroupIds);

    Task<int> AddAsync(ProductionOrderAssemblyGroupSpec spec);
    Task UpdateAsync(ProductionOrderAssemblyGroupSpec spec);
    Task DeleteAsync(int id);
}
```

Impl in `ProductionOrderAssemblyGroupSpecRepository.cs`:

```csharp
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ProductionOrderAssemblyGroupSpecRepository : IProductionOrderAssemblyGroupSpecRepository
{
    private readonly ApplicationDbContext _context;
    public ProductionOrderAssemblyGroupSpecRepository(ApplicationDbContext context) => _context = context;

    public Task<ProductionOrderAssemblyGroupSpec?> GetByIdAsync(int id)
        => _context.ProductionOrderAssemblyGroupSpecs
            .Include(s => s.Article)
            .Include(s => s.AssemblyGroup)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<List<ProductionOrderAssemblyGroupSpec>> GetByAssemblyGroupIdAsync(int assemblyGroupId)
        => await _context.ProductionOrderAssemblyGroupSpecs
            .Include(s => s.Article)
            .Where(s => s.AssemblyGroupId == assemblyGroupId)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .ToListAsync();

    public async Task<Dictionary<int, List<ProductionOrderAssemblyGroupSpec>>> GetByAssemblyGroupIdsAsync(
        IEnumerable<int> assemblyGroupIds)
    {
        var ids = assemblyGroupIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, List<ProductionOrderAssemblyGroupSpec>>();

        var rows = await _context.ProductionOrderAssemblyGroupSpecs
            .Include(s => s.Article)
            .Where(s => ids.Contains(s.AssemblyGroupId))
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .ToListAsync();

        return rows.GroupBy(s => s.AssemblyGroupId)
                   .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task<int> AddAsync(ProductionOrderAssemblyGroupSpec spec)
    {
        _context.ProductionOrderAssemblyGroupSpecs.Add(spec);
        await _context.SaveChangesAsync();
        return spec.Id;
    }

    public async Task UpdateAsync(ProductionOrderAssemblyGroupSpec spec)
    {
        _context.ProductionOrderAssemblyGroupSpecs.Update(spec);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var row = await _context.ProductionOrderAssemblyGroupSpecs.FindAsync(id);
        if (row == null) return;
        _context.ProductionOrderAssemblyGroupSpecs.Remove(row);
        await _context.SaveChangesAsync();
    }
}
```

DI-Registrierung in `Program.cs` direkt nach `IProductionOrderAssemblyGroupRepository`:

```csharp
builder.Services.AddScoped<IProductionOrderAssemblyGroupSpecRepository,
                          ProductionOrderAssemblyGroupSpecRepository>();
```

**Audit-Felder:** Werden vom Controller gesetzt (analog zu allen anderen Repos in der Codebase — siehe Phase-1-Pattern in `ProductionOrderAssemblyGroupRepository.SetIsApplicableAsync`). Das Repo macht keinen Audit-Eintrag selbst.

## 7. ViewModels

Datei `IdealAkeWms/Models/ViewModels/FaCompletionViewModels.cs` (eine Datei mit drei ViewModels, analog zur Phase-2-Praezedenz `PickingLeitstandViewModel.cs`):

```csharp
namespace IdealAkeWms.Models.ViewModels;

// --------- Index-Page ---------

public class FaCompletionListViewModel
{
    public List<FaCompletionListItem> Items { get; set; } = new();
    public string? FilterOrderNumber { get; set; }
    public string? FilterArticleNumber { get; set; }
    public string? FilterCustomer { get; set; }
    public bool ShowDone { get; set; }
}

public class FaCompletionListItem
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Customer { get; set; }
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public DateTime? ProductionDate { get; set; }
    public bool IsDone { get; set; }

    /// <summary>Anzahl Gruppen mit IsApplicable=true (Quick-Indicator fuer den Pfleger).</summary>
    public int ApplicableCount { get; set; }

    /// <summary>Anzahl Gruppen mit IsCompleted=true.</summary>
    public int CompletedCount { get; set; }

    /// <summary>Summe Specs ueber alle 5 Gruppen.</summary>
    public int SpecCount { get; set; }
}

// --------- Edit-Page (Tabs) ---------

public class FaCompletionEditViewModel
{
    public int ProductionOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Customer { get; set; }
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public string? Description2 { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public bool IsDone { get; set; }

    /// <summary>Aktiver Tab (VK / VL / VE / VT / VA) — kommt aus Query oder Default "VK".</summary>
    public string ActiveTab { get; set; } = "VK";

    public List<AssemblyGroupTabViewModel> Tabs { get; set; } = new();
}

public class AssemblyGroupTabViewModel
{
    public int AssemblyGroupId { get; set; }
    public string GroupKey { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;  // "Kuehlung", "Lueftung", ...
    public bool IsApplicable { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }
    public List<AssemblyGroupSpecFormModel> Specs { get; set; } = new();
}

// --------- Spec-Form (Add + Edit) ---------

public class AssemblyGroupSpecFormModel
{
    public int Id { get; set; }                  // 0 = Add, >0 = Edit
    public int AssemblyGroupId { get; set; }
    public int? ArticleId { get; set; }
    public string? ArticleText { get; set; }     // "100023 - Lueftermotor 230V" — fuer Select2-Vorbelegung
    public string Description { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}
```

**GroupKey → GroupName-Mapping** (lebt als statisches Dictionary im Controller oder ViewModel):

| GroupKey | GroupName |
|---|---|
| `VK` | Kuehlung |
| `VL` | Lueftung |
| `VE` | Elektro |
| `VT` | Tueren |
| `VA` | Aufbau |

(Reihenfolge identisch zu Phase 1 — `HasCooling` → `VK`, `HasFan` → `VL`, `HasElectric` → `VE`, `HasDoors` → `VT`, `HasSuperstructure` → `VA`.)

## 8. Controller `FaCompletionController`

Datei `IdealAkeWms/Controllers/FaCompletionController.cs`. Folgt dem MVC-Pattern aus Phase 2 (siehe `PickingLeitstandController.cs`).

### 8.1 Klassenebene + DI

```csharp
[RequireFaCompletionAccess]
public class FaCompletionController : Controller
{
    private static readonly Dictionary<string, string> GroupKeyNames = new()
    {
        ["VK"] = "Kuehlung", ["VL"] = "Lueftung", ["VE"] = "Elektro",
        ["VT"] = "Tueren",   ["VA"] = "Aufbau"
    };
    private static readonly string[] GroupKeysOrdered = ["VK", "VL", "VE", "VT", "VA"];

    private readonly IProductionOrderRepository _productionOrderRepository;
    private readonly IProductionOrderAssemblyGroupRepository _assemblyGroupRepository;
    private readonly IProductionOrderAssemblyGroupSpecRepository _specRepository;
    private readonly IArticleRepository _articleRepository;
    private readonly ICurrentUserService _currentUser;

    public FaCompletionController(/* … 5 deps … */) { /* … */ }
    // Actions s.u.
}
```

### 8.2 `Index`-Action

Schlanke FA-Liste mit Filter (analog `ProductionOrders/Index` slim). Pro FA werden die 5 Gruppen + Spec-Counts ueber Bulk-Lookup geladen, damit Index nicht N+1 wird.

```csharp
public async Task<IActionResult> Index(string? filterOrderNumber, string? filterArticleNumber,
    string? filterCustomer, bool showDone = false)
{
    var orders = await _productionOrderRepository.GetAllOrderedAsync();

    if (!string.IsNullOrWhiteSpace(filterOrderNumber))
        orders = orders.Where(o => o.OrderNumber.Contains(filterOrderNumber, StringComparison.OrdinalIgnoreCase)).ToList();
    if (!string.IsNullOrWhiteSpace(filterArticleNumber))
        orders = orders.Where(o => o.ArticleNumber != null && o.ArticleNumber.Contains(filterArticleNumber, StringComparison.OrdinalIgnoreCase)).ToList();
    if (!string.IsNullOrWhiteSpace(filterCustomer))
        orders = orders.Where(o => o.Customer != null && o.Customer.Contains(filterCustomer, StringComparison.OrdinalIgnoreCase)).ToList();
    if (!showDone)
        orders = orders.Where(o => !o.IsDone).ToList();

    var orderIds = orders.Select(o => o.Id).ToList();

    // Bulk-Counts: nutzt Phase-1-Pivot + neuen Spec-Count-Aggregat
    var pivot = await _assemblyGroupRepository.GetIsApplicablePivotAsync(orderIds);
    var groupRows = orderIds.Count == 0
        ? new List<ProductionOrderAssemblyGroup>()
        : await LoadAllGroupsForOrders(orderIds);   // Helper s.u.
    var groupIds = groupRows.Select(g => g.Id).ToList();
    var specsByGroup = await _specRepository.GetByAssemblyGroupIdsAsync(groupIds);

    var completedByOrder = groupRows
        .GroupBy(g => g.ProductionOrderId)
        .ToDictionary(g => g.Key, g => g.Count(x => x.IsCompleted));
    var specCountByOrder = groupRows
        .GroupBy(g => g.ProductionOrderId)
        .ToDictionary(g => g.Key,
            g => g.Sum(x => specsByGroup.TryGetValue(x.Id, out var l) ? l.Count : 0));

    var items = orders.Select(o => new FaCompletionListItem
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        Quantity = o.Quantity,
        Customer = o.Customer,
        ArticleNumber = o.ArticleNumber,
        Description1 = o.Description1,
        ProductionDate = o.ProductionDate,
        IsDone = o.IsDone,
        ApplicableCount = pivot.TryGetValue(o.Id, out var d) ? d.Count(kv => kv.Value) : 0,
        CompletedCount = completedByOrder.GetValueOrDefault(o.Id),
        SpecCount = specCountByOrder.GetValueOrDefault(o.Id),
    }).ToList();

    return View(new FaCompletionListViewModel
    {
        Items = items,
        FilterOrderNumber = filterOrderNumber,
        FilterArticleNumber = filterArticleNumber,
        FilterCustomer = filterCustomer,
        ShowDone = showDone
    });
}

private async Task<List<ProductionOrderAssemblyGroup>> LoadAllGroupsForOrders(List<int> orderIds)
{
    // Helper: nutzt vorhandenes Repo (alle Gruppen, eine Round-Trip). EF-Query inline akzeptabel.
    var result = new List<ProductionOrderAssemblyGroup>();
    const int chunkSize = 1000;
    for (int offset = 0; offset < orderIds.Count; offset += chunkSize)
    {
        var chunk = orderIds.Skip(offset).Take(chunkSize).ToList();
        var rows = await _assemblyGroupRepository.GetByProductionOrderIdsAsync(chunk);   // s.u. (Erweiterung)
        result.AddRange(rows);
    }
    return result;
}
```

**Repo-Erweiterung:** `IProductionOrderAssemblyGroupRepository` bekommt eine Bulk-Methode (Phase 1 hatte nur Single + Pivot):

```csharp
Task<List<ProductionOrderAssemblyGroup>> GetByProductionOrderIdsAsync(IEnumerable<int> orderIds);
```

Impl wieder mit 1000-Chunking (analog zum Pivot in Phase 1, siehe [`ProductionOrderAssemblyGroupRepository.cs:34-53`](IdealAkeWms/Data/Repositories/ProductionOrderAssemblyGroupRepository.cs#L34-L53)).

### 8.3 `Edit`-Action

```csharp
public async Task<IActionResult> Edit(int id, string? tab = null)
{
    var order = await _productionOrderRepository.GetByIdAsync(id);
    if (order == null) return NotFound();

    var groups = await _assemblyGroupRepository.GetByProductionOrderIdAsync(id);
    if (groups.Count == 0) return NotFound("AssemblyGroups fehlen (sollte durch Phase 1 eager-created sein).");

    var specsByGroup = await _specRepository.GetByAssemblyGroupIdsAsync(groups.Select(g => g.Id));

    var activeTab = !string.IsNullOrWhiteSpace(tab) && GroupKeyNames.ContainsKey(tab) ? tab : "VK";

    var vm = new FaCompletionEditViewModel
    {
        ProductionOrderId = order.Id,
        OrderNumber = order.OrderNumber,
        Quantity = order.Quantity,
        Customer = order.Customer,
        ArticleNumber = order.ArticleNumber,
        Description1 = order.Description1,
        Description2 = order.Description2,
        ProductionDate = order.ProductionDate,
        DeliveryDate = order.DeliveryDate,
        IsDone = order.IsDone,
        ActiveTab = activeTab,
        Tabs = GroupKeysOrdered.Select(key =>
        {
            var grp = groups.FirstOrDefault(g => g.GroupKey == key)
                      ?? throw new InvalidOperationException($"AssemblyGroup {key} fehlt fuer FA {id}.");
            var specs = specsByGroup.GetValueOrDefault(grp.Id) ?? new();
            return new AssemblyGroupTabViewModel
            {
                AssemblyGroupId = grp.Id,
                GroupKey = grp.GroupKey,
                GroupName = GroupKeyNames[grp.GroupKey],
                IsApplicable = grp.IsApplicable,
                IsCompleted = grp.IsCompleted,
                CompletedAt = grp.CompletedAt,
                CompletedBy = grp.CompletedBy,
                Specs = specs.Select(s => new AssemblyGroupSpecFormModel
                {
                    Id = s.Id,
                    AssemblyGroupId = s.AssemblyGroupId,
                    ArticleId = s.ArticleId,
                    ArticleText = s.Article != null
                        ? s.Article.ArticleNumber + (s.Article.Description != null ? " - " + s.Article.Description : "")
                        : null,
                    Description = s.Description,
                    Quantity = s.Quantity,
                    Notes = s.Notes,
                    SortOrder = s.SortOrder
                }).ToList()
            };
        }).ToList()
    };

    return View(vm);
}
```

### 8.4 `AddSpec`-Action

```csharp
[HttpPost] [ValidateAntiForgeryToken]
public async Task<IActionResult> AddSpec(AssemblyGroupSpecFormModel form)
{
    if (string.IsNullOrWhiteSpace(form.Description))
        ModelState.AddModelError(nameof(form.Description), "Beschreibung ist erforderlich.");

    var group = await _assemblyGroupRepository.GetByPoAndKeyAsync(0, ""); // no-op – we need group by ID
    var grp = await _context_GroupById(form.AssemblyGroupId);  // siehe Helfer-Note unten
    if (grp == null) return NotFound("AssemblyGroup fehlt.");

    if (!ModelState.IsValid)
    {
        TempData["WarningMessage"] = "Beschreibung ist erforderlich.";
        return RedirectToAction("Edit", new { id = grp.ProductionOrderId, tab = grp.GroupKey });
    }

    var spec = new ProductionOrderAssemblyGroupSpec
    {
        AssemblyGroupId = form.AssemblyGroupId,
        ArticleId = form.ArticleId,
        Description = form.Description.Trim(),
        Quantity = form.Quantity,
        Notes = form.Notes,
        SortOrder = form.SortOrder,
        CreatedAt = DateTime.Now,
        CreatedBy = _currentUser.GetDisplayName(),
        CreatedByWindows = _currentUser.GetWindowsUserName()
    };
    await _specRepository.AddAsync(spec);

    TempData["SuccessMessage"] = "Auspraegung hinzugefuegt.";
    return RedirectToAction("Edit", new { id = grp.ProductionOrderId, tab = grp.GroupKey });
}
```

**Helfer `_context_GroupById`-Note:** entweder eine kleine zusaetzliche Methode auf `IProductionOrderAssemblyGroupRepository.GetByIdAsync(int)`, oder Controller laedt es per `GetByProductionOrderIdAsync` + LINQ. Plan-Empfehlung: neue `GetByIdAsync(int)`-Methode am `IProductionOrderAssemblyGroupRepository` ergaenzen (Phase 1 hatte sie nicht, weil immer per `ProductionOrderId+GroupKey` zugegriffen wurde — Phase 4 braucht Lookup nur per Spec-`AssemblyGroupId`).

### 8.5 `EditSpec`-Action

```csharp
[HttpPost] [ValidateAntiForgeryToken]
public async Task<IActionResult> EditSpec(AssemblyGroupSpecFormModel form)
{
    if (string.IsNullOrWhiteSpace(form.Description))
        ModelState.AddModelError(nameof(form.Description), "Beschreibung ist erforderlich.");

    var existing = await _specRepository.GetByIdAsync(form.Id);
    if (existing == null) return NotFound();
    var grp = existing.AssemblyGroup;

    if (!ModelState.IsValid)
    {
        TempData["WarningMessage"] = "Beschreibung ist erforderlich.";
        return RedirectToAction("Edit", new { id = grp.ProductionOrderId, tab = grp.GroupKey });
    }

    existing.ArticleId = form.ArticleId;
    existing.Description = form.Description.Trim();
    existing.Quantity = form.Quantity;
    existing.Notes = form.Notes;
    existing.SortOrder = form.SortOrder;
    existing.ModifiedAt = DateTime.Now;
    existing.ModifiedBy = _currentUser.GetDisplayName();
    existing.ModifiedByWindows = _currentUser.GetWindowsUserName();

    await _specRepository.UpdateAsync(existing);

    TempData["SuccessMessage"] = "Auspraegung aktualisiert.";
    return RedirectToAction("Edit", new { id = grp.ProductionOrderId, tab = grp.GroupKey });
}
```

### 8.6 `DeleteSpec`-Action

```csharp
[HttpPost] [ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteSpec(int id)
{
    var existing = await _specRepository.GetByIdAsync(id);
    if (existing == null) return NotFound();
    var grp = existing.AssemblyGroup;

    await _specRepository.DeleteAsync(id);
    TempData["SuccessMessage"] = "Auspraegung geloescht.";
    return RedirectToAction("Edit", new { id = grp.ProductionOrderId, tab = grp.GroupKey });
}
```

### 8.7 `ToggleIsCompleted`-Action

`IsCompleted` benoetigt eigene Action, da kein generischer JSON-Endpoint dafuer existiert (Phase 1 hat nur `IsApplicable` als API-Endpoint angelegt).

```csharp
[HttpPost] [ValidateAntiForgeryToken]
public async Task<IActionResult> ToggleIsCompleted(int assemblyGroupId)
{
    var grp = await _assemblyGroupRepository.GetByIdAsync(assemblyGroupId);
    if (grp == null) return NotFound();

    grp.IsCompleted = !grp.IsCompleted;
    if (grp.IsCompleted)
    {
        grp.CompletedAt = DateTime.Now;
        grp.CompletedBy = _currentUser.GetDisplayName();
    }
    else
    {
        grp.CompletedAt = null;
        grp.CompletedBy = null;
    }
    grp.ModifiedAt = DateTime.Now;
    grp.ModifiedBy = _currentUser.GetDisplayName();
    grp.ModifiedByWindows = _currentUser.GetWindowsUserName();

    await _assemblyGroupRepository.UpdateAsync(grp);

    return RedirectToAction("Edit", new { id = grp.ProductionOrderId, tab = grp.GroupKey });
}
```

**Repo-Erweiterung Nr. 2:** `IProductionOrderAssemblyGroupRepository` bekommt zusaetzlich `Task UpdateAsync(ProductionOrderAssemblyGroup grp)` (oder zumindest eine `SetIsCompletedAsync(int, bool, string, string)`-Variante analog zu `SetIsApplicableAsync`). Plan-Empfehlung: dedizierte Methode `SetIsCompletedAsync` analog zur Phase-1-Praezedenz, damit der Audit-Lifecycle gekapselt bleibt:

```csharp
Task SetIsCompletedAsync(int assemblyGroupId, bool value, string completedBy, string modifiedBy, string modifiedByWindows);
```

### 8.8 `ToggleIsApplicable` — KEIN eigener Endpoint

Der bestehende JSON-Endpoint `POST /api/assembly-groups/toggle-applicable` (Phase 1) wird via JS direkt aus der Edit-View aufgerufen. Permission ist durch den neuen `RequirePickingOrFaCompletionAccess`-Filter erweitert (siehe 5.3). Keine Anpassung am `FaCompletionController` noetig.

## 9. Views

### 9.1 `Views/FaCompletion/Index.cshtml`

Layout: Page-Header + Filter-Card + Tabelle. Stil analog `Views/ProductionOrders/Index.cshtml` (slim, Phase 2).

Spalten:
- FA Nr. (Link auf `Edit/{Id}?tab=VK`)
- Stk.
- Kunde
- Artikelnummer
- Bezeichnung 1
- Fert.-Termin
- Status (IsDone-Badge)
- **Anwendbar** (`ApplicableCount` / 5 — z.&nbsp;B. "3 / 5")
- **Vervollstaendigt** (`CompletedCount` / 5)
- **Specs gesamt** (`SpecCount`)
- Aktion: Stift-Icon "Bearbeiten" → `Edit/{Id}`

Filter-Card mit FA-Nr., Artikel, Kunde, ShowDone — identisch zu Phase 2 slim-Index.

```razor
@model IdealAkeWms.Models.ViewModels.FaCompletionListViewModel
@{
    ViewData["Title"] = "FA-Vervollstaendigung";
}
<h2 class="page-header">FA-Vervollstaendigung</h2>

<form method="get" class="card card-body mb-3">
    <div class="row g-2">
        <div class="col-md-3"><input class="form-control" name="filterOrderNumber" value="@Model.FilterOrderNumber" placeholder="FA Nr." /></div>
        <div class="col-md-3"><input class="form-control" name="filterArticleNumber" value="@Model.FilterArticleNumber" placeholder="Artikel" /></div>
        <div class="col-md-3"><input class="form-control" name="filterCustomer" value="@Model.FilterCustomer" placeholder="Kunde" /></div>
        <div class="col-md-2">
            <div class="form-check mt-2">
                <input class="form-check-input" type="checkbox" name="showDone" value="true" @(Model.ShowDone ? "checked" : "") onchange="this.form.submit()" />
                <label class="form-check-label">Erledigte zeigen</label>
            </div>
        </div>
        <div class="col-md-1"><button class="btn btn-primary">Filtern</button></div>
    </div>
</form>

<div class="table-responsive">
    <table class="table table-striped filterable-table" data-view-key="FaCompletion">
        <thead>
            <tr>
                <th data-col-key="order-number">FA Nr.</th>
                <th data-col-key="quantity">Stk.</th>
                <th data-col-key="customer">Kunde</th>
                <th data-col-key="article-number">Artikelnummer</th>
                <th data-col-key="description1">Bezeichnung 1</th>
                <th data-col-key="production-date">Fert.-Termin</th>
                <th data-col-key="applicable">Anwendbar</th>
                <th data-col-key="completed">Vervollstaendigt</th>
                <th data-col-key="spec-count">Auspraegungen</th>
                <th data-col-key="row-actions"></th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in Model.Items)
            {
                <tr class="@(item.IsDone ? "table-secondary" : "")">
                    <td>@item.OrderNumber</td>
                    <td>@item.Quantity</td>
                    <td>@item.Customer</td>
                    <td>@item.ArticleNumber</td>
                    <td>@item.Description1</td>
                    <td>@(item.ProductionDate?.ToString("dd.MM.yyyy"))</td>
                    <td>@item.ApplicableCount / 5</td>
                    <td>@item.CompletedCount / 5</td>
                    <td>@item.SpecCount</td>
                    <td><a asp-action="Edit" asp-route-id="@item.Id" class="btn btn-sm btn-outline-primary">Bearbeiten</a></td>
                </tr>
            }
        </tbody>
    </table>
</div>
```

**`ColumnDefinitions.cs`-Erweiterung:** Neuer `ViewConfig FaCompletion` (analog zu Phase 2 `PickingLeitstand`). 10 Spalten, alle `Locked: false` ausser `row-actions` (Locked).

### 9.2 `Views/FaCompletion/Edit.cshtml`

Top-Block: FA-Header (FA-Nr., Kunde, Artikel, Bezeichnung, Termine, IsDone-Badge).

Tab-Navigation: Bootstrap-`nav-tabs`. URL-State via `asp-route-tab`. Activ-Class auf dem Tab, dessen `GroupKey == Model.ActiveTab`. Pattern analog `Views/BdeMasterData/Index.cshtml` (siehe [`BdeMasterData/Index.cshtml:13-26`](IdealAkeWms/Views/BdeMasterData/Index.cshtml#L13-L26)).

```razor
@model IdealAkeWms.Models.ViewModels.FaCompletionEditViewModel
@{
    ViewData["Title"] = $"FA {Model.OrderNumber} vervollstaendigen";
}

<div class="d-flex justify-content-between align-items-center flex-wrap gap-2 mb-3">
    <h2 class="page-header mb-0">FA @Model.OrderNumber</h2>
    <a asp-action="Index" class="btn btn-outline-secondary">Zur Uebersicht</a>
</div>

<div class="card mb-3">
    <div class="card-body">
        <div class="row">
            <div class="col-md-4"><strong>Kunde:</strong> @Model.Customer</div>
            <div class="col-md-4"><strong>Artikel:</strong> @Model.ArticleNumber</div>
            <div class="col-md-4"><strong>Stk.:</strong> @Model.Quantity</div>
            <div class="col-md-4"><strong>Bezeichnung:</strong> @Model.Description1</div>
            <div class="col-md-4"><strong>Fert.-Termin:</strong> @(Model.ProductionDate?.ToString("dd.MM.yyyy"))</div>
            <div class="col-md-4"><strong>Liefertermin:</strong> @(Model.DeliveryDate?.ToString("dd.MM.yyyy"))</div>
        </div>
    </div>
</div>

<ul class="nav nav-tabs mb-3">
    @foreach (var t in Model.Tabs)
    {
        var indicator = "";
        if (t.IsApplicable && t.IsCompleted) { indicator = " (✓ vollst.)"; }
        else if (t.IsApplicable) { indicator = " (offen)"; }
        <li class="nav-item">
            <a class="nav-link @(t.GroupKey == Model.ActiveTab ? "active" : "")"
               asp-action="Edit" asp-route-id="@Model.ProductionOrderId" asp-route-tab="@t.GroupKey">
                @t.GroupKey — @t.GroupName <span class="text-muted small">@indicator</span>
            </a>
        </li>
    }
</ul>

@{
    var active = Model.Tabs.First(t => t.GroupKey == Model.ActiveTab);
}

<div class="tab-content">
    <div class="tab-pane active">
        @* Flags-Bar *@
        <div class="d-flex gap-3 mb-3 align-items-center">
            <div class="form-check form-switch">
                <input class="form-check-input toggle-applicable" type="checkbox"
                       id="chkApplicable_@active.AssemblyGroupId"
                       data-id="@Model.ProductionOrderId"
                       data-group-key="@active.GroupKey"
                       @(active.IsApplicable ? "checked" : "") />
                <label class="form-check-label" for="chkApplicable_@active.AssemblyGroupId">
                    Anwendbar (<code>IsApplicable</code>)
                </label>
            </div>

            <form method="post" asp-action="ToggleIsCompleted" class="form-check form-switch m-0">
                @Html.AntiForgeryToken()
                <input type="hidden" name="assemblyGroupId" value="@active.AssemblyGroupId" />
                <input class="form-check-input" type="checkbox"
                       onchange="this.form.submit()"
                       @(active.IsCompleted ? "checked" : "") />
                <label class="form-check-label">
                    Vervollstaendigt (<code>IsCompleted</code>)
                    @if (active.IsCompleted)
                    {
                        <span class="text-muted small">— @active.CompletedBy am @active.CompletedAt?.ToString("dd.MM.yyyy HH:mm")</span>
                    }
                </label>
            </form>
        </div>

        @* Spec-Tabelle *@
        <div class="table-responsive">
            <table class="table table-striped">
                <thead>
                    <tr>
                        <th>Artikel</th>
                        <th>Beschreibung</th>
                        <th>Menge</th>
                        <th>Notizen</th>
                        <th style="width: 80px;">Sort.</th>
                        <th style="width: 140px;"></th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var spec in active.Specs)
                    {
                        @await Html.PartialAsync("_SpecRow", spec)
                    }

                    @* Inline-Add-Row als letzte tbody-Zeile *@
                    <tr class="table-light">
                        <td colspan="6">
                            <form method="post" asp-action="AddSpec" class="row g-2 align-items-end">
                                @Html.AntiForgeryToken()
                                <input type="hidden" name="AssemblyGroupId" value="@active.AssemblyGroupId" />

                                <div class="col-md-3">
                                    <label class="form-label small mb-0">Artikel</label>
                                    <select name="ArticleId" class="form-select article-select2"
                                            data-placeholder="Artikel suchen..."></select>
                                </div>
                                <div class="col-md-3">
                                    <label class="form-label small mb-0">Beschreibung *</label>
                                    <input name="Description" class="form-control" required maxlength="500" />
                                </div>
                                <div class="col-md-2">
                                    <label class="form-label small mb-0">Menge</label>
                                    <input name="Quantity" type="number" step="0.001" class="form-control" />
                                </div>
                                <div class="col-md-2">
                                    <label class="form-label small mb-0">Notizen</label>
                                    <input name="Notes" class="form-control" />
                                </div>
                                <div class="col-md-1">
                                    <label class="form-label small mb-0">Sort.</label>
                                    <input name="SortOrder" type="number" class="form-control" value="0" />
                                </div>
                                <div class="col-md-1">
                                    <button type="submit" class="btn btn-primary w-100">+</button>
                                </div>
                            </form>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>
</div>

@section Scripts {
    <script src="~/lib/select2/dist/js/select2.full.min.js" asp-append-version="true"></script>
    <script>
        // Select2 fuer Article-Suche — Phase 4 reuse von /api/articles/search
        $(function () {
            $('.article-select2').select2({
                ajax: {
                    url: '/api/articles/search',
                    data: function (params) { return { q: params.term, limit: 50 }; },
                    processResults: function (data) {
                        return { results: data.map(function (a) { return { id: a.id, text: a.text }; }) };
                    },
                    delay: 250
                },
                minimumInputLength: 2,
                allowClear: true,
                placeholder: 'Artikel suchen...'
            });

            // IsApplicable-Toggle (analog Phase 2 PickingLeitstand-Index)
            $('.toggle-applicable').on('change', function () {
                var cb = $(this);
                $.ajax({
                    url: '/api/assembly-groups/toggle-applicable',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({
                        productionOrderId: cb.data('id'),
                        groupKey: cb.data('group-key'),
                        value: cb.is(':checked')
                    })
                }).fail(function () {
                    cb.prop('checked', !cb.is(':checked'));
                    alert('Fehler beim Speichern.');
                });
            });
        });
    </script>
}
```

### 9.3 Partial `_SpecRow.cshtml`

Datei `IdealAkeWms/Views/FaCompletion/_SpecRow.cshtml`. Rendert eine existierende Spec als inline-editable Form-Row.

```razor
@model IdealAkeWms.Models.ViewModels.AssemblyGroupSpecFormModel

<tr>
    <td>
        <form method="post" asp-action="EditSpec" id="frmEditSpec_@Model.Id" class="d-inline">
            @Html.AntiForgeryToken()
            <input type="hidden" name="Id" value="@Model.Id" />
            <input type="hidden" name="AssemblyGroupId" value="@Model.AssemblyGroupId" />
            <select name="ArticleId" class="form-select form-select-sm article-select2-prefilled"
                    data-prefilled-id="@Model.ArticleId" data-prefilled-text="@Model.ArticleText"></select>
        </form>
    </td>
    <td>
        <input form="frmEditSpec_@Model.Id" name="Description" class="form-control form-control-sm"
               value="@Model.Description" maxlength="500" required />
    </td>
    <td>
        <input form="frmEditSpec_@Model.Id" name="Quantity" type="number" step="0.001"
               class="form-control form-control-sm" value="@(Model.Quantity?.ToString())" />
    </td>
    <td>
        <input form="frmEditSpec_@Model.Id" name="Notes" class="form-control form-control-sm"
               value="@Model.Notes" />
    </td>
    <td>
        <input form="frmEditSpec_@Model.Id" name="SortOrder" type="number"
               class="form-control form-control-sm" value="@Model.SortOrder" />
    </td>
    <td>
        <button type="submit" form="frmEditSpec_@Model.Id" class="btn btn-sm btn-outline-primary">Speichern</button>
        <form method="post" asp-action="DeleteSpec" class="d-inline"
              onsubmit="return confirm('Auspraegung wirklich loeschen?');">
            @Html.AntiForgeryToken()
            <input type="hidden" name="id" value="@Model.Id" />
            <button type="submit" class="btn btn-sm btn-outline-danger">Loeschen</button>
        </form>
    </td>
</tr>
```

**Hinweis zu HTML5-`form`-Attribut:** Die `<select>`/`<input>`-Elemente referenzieren via `form="frmEditSpec_@Model.Id"` die ausserhalb der `<td>` definierte Form. Dadurch koennen wir ein einzelnes Form mehrere TDs umspannen lassen — Browser-Support seit Chrome/Firefox/Edge ≥ 10 Jahren. Bootstrap-Tabellen-Renderer brauchen das Pattern, weil `<form>` im `<tr>` ungueltig ist.

### 9.4 Article-Select2 — Reuse vs. neuer Endpoint

**Entscheidung:** existierender `GET /api/articles/search`-Endpoint wird genutzt (siehe [`ArticlesApiController.cs:17-26`](IdealAkeWms/Controllers/ArticlesApiController.cs#L17-L26)). Liefert `[{ id, text }]` mit `text = "ArticleNumber - Description"` — bereits Select2-kompatibel.

**Open:** Optional Article-Group-Filter (z.B. "VL"-Tab zeigt nur Lueftungs-Artikel). **Recommended default:** kein Filter in Phase 4. Begruendung: Artikelgruppen-Konvention zwischen VK/VL/VE/VT/VA und Article-Master ist nicht garantiert deckungsgleich (SAGE liefert `"940 - Kleinmaterial"`, vgl. CLAUDE.md-Fallstrick "Artikelgruppe BOM vs Articles"). Erfahrene Pfleger geben Artikelnummer-Praefix direkt im Search-String ein. Optional kann Phase 4.1 (Folge-Iteration) einen serverseitigen `?group=`-Filter ergaenzen, wenn echter User-Bedarf da ist.

**Pre-Filled Edit-Rows:** Wenn die Edit-Form eine bestehende Spec mit ArticleId rendert, muss Select2 die Option vorbefuellt haben. Pattern via `data-prefilled-id` / `data-prefilled-text` (siehe `_SpecRow.cshtml`):

```javascript
// Im Edit.cshtml-Script-Section ergaenzen:
$('.article-select2-prefilled').each(function () {
    var sel = $(this);
    var id = sel.data('prefilled-id');
    var text = sel.data('prefilled-text');
    if (id && text) {
        var opt = new Option(text, id, true, true);
        sel.append(opt);
    }
    sel.select2({
        ajax: { /* … wie 9.2 … */ },
        minimumInputLength: 2,
        allowClear: true
    });
});
```

### 9.5 Inline-Add vs. Modal — Entscheidung

**Entscheidung: inline-Add-Row** (letzte tbody-Zeile). Begruendung:
- Pfleger arbeitet oft mehrere Specs hintereinander ein — Modal zwingt zu Klick-Klick-Klick.
- Inline-Form ist kompakter und entspricht dem Pattern in `StockMovements/Inbound` und `Picking/Bom` (beide haben inline-Add-Rows).
- Validation-Errors lassen sich mit `TempData["WarningMessage"]` schlank kommunizieren — kein Modal-State zu verwalten.

**Open:** Falls Pfleger sich beschweren ("Form unter Tabelle ist unuebersichtlich bei vielen Specs"), kann Phase 4.1 eine Modal-Variante nachreichen. Recommended default fuer Phase 4: inline.

### 9.6 Nav-Bar-Entry

Im `Views/Shared/_Layout.cshtml`, nach dem `Kommissionierung`-Dropdown (Phase 2, Zeile 74-95). Variante: eigenes Top-Level-Item, weil FA-Vervollstaendigung kein Sub-Use-Case von Kommissionierung ist.

```razor
@{
    var canCompleteFa = await CurrentUserService.CanCompleteFaAsync();
}

@if (canCompleteFa)
{
    <li class="nav-item">
        <a class="nav-link" asp-controller="FaCompletion" asp-action="Index">FA-Vervollstaendigung</a>
    </li>
}
```

Position im Menue: zwischen `Kommissionierung` und `Bestellungen` (oder zwischen `Bestellungen` und `Teileverfolgung` — Stakeholder-Praeferenz; Plan-Default ist nach `Kommissionierung`).

**Open:** Soll der Nav-Eintrag nur erscheinen, wenn es ueberhaupt nicht-applicable Gruppen gibt? **Recommended default:** immer erscheinen, weil der Pfleger der Liste-Page entscheidet, was zu tun ist. Conditional Sichtbarkeit ist Over-Engineering.

## 10. Permission-Update auf bestehendem API-Endpoint

Konkrete Aenderung in `IdealAkeWms/Controllers/AssemblyGroupsApiController.cs` (Phase-1-Code, siehe [`AssemblyGroupsApiController.cs:10`](IdealAkeWms/Controllers/AssemblyGroupsApiController.cs#L10)):

```diff
 [Route("api/assembly-groups")]
 [ApiController]
-[RequirePickingAccess]
+[RequirePickingOrFaCompletionAccess]
 public class AssemblyGroupsApiController : ControllerBase { ... }
```

**Regression-Check:** Phase-2-PickingLeitstand-Index-View ruft denselben Endpoint via JS-Dispatcher (siehe Phase-2-Spec 6.2). Picker-Rolle bestand bereits `RequirePickingAccess`, besteht jetzt `RequirePickingOrFaCompletionAccess`. Tracking-Only-User bestand schon damals nicht — bleibt blockiert. Keine Regression fuer Phase-2-Workflows.

CLAUDE.md-Tabelle "Zugriffsschutz" wird angepasst:

```diff
-| `[RequirePickingAccess]` | admin, picking | ProductionOrdersApiController, PickingController, AssemblyGroupsApiController, PickingStatusApiController, BdeStatusApiController |
+| `[RequirePickingAccess]` | admin, picking | ProductionOrdersApiController, PickingController, PickingStatusApiController, BdeStatusApiController |
+| `[RequirePickingOrFaCompletionAccess]` | admin, picking ODER fa_completion | AssemblyGroupsApiController |
+| `[RequireFaCompletionAccess]` | admin, fa_completion | FaCompletionController |
```

## 11. ColumnDefinitions-Erweiterung

In `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` (nach dem Phase-2-`PickingLeitstand`-Block, vor `Picking`):

```csharp
public static readonly ViewConfig FaCompletion = new(
    "FaCompletion", "FA-Vervollstaendigung",
    SupportsReorder: true, SupportsSortDefault: true)
{
    Columns =
    [
        new ColumnDef("order-number",   "FA Nr.",        Locked: true,  DefaultWidth: 90),
        new ColumnDef("quantity",       "Stk.",          Locked: false, DefaultWidth: 55),
        new ColumnDef("customer",       "Kunde",         Locked: false),
        new ColumnDef("article-number", "Artikelnummer", Locked: false),
        new ColumnDef("description1",   "Bezeichnung 1", Locked: false),
        new ColumnDef("production-date","Fert.-Termin",  Locked: false),
        new ColumnDef("applicable",     "Anwendbar",     Locked: false, DefaultWidth: 100),
        new ColumnDef("completed",      "Vervollstaendigt", Locked: false, DefaultWidth: 130),
        new ColumnDef("spec-count",     "Auspraegungen", Locked: false, DefaultWidth: 110),
        new ColumnDef("row-actions",    "",              Locked: true,  DefaultWidth: 110),
    ]
};
```

`GetByViewKey`-Switch ergaenzen: `"FaCompletion" => FaCompletion`.

## 12. Tests

### 12.1 Controller-Tests

Datei `IdealAkeWms.Tests/Controllers/FaCompletionControllerTests.cs`:

- `Index_FiltersByOrderNumber_ReturnsOnlyMatching`
- `Index_ShowDoneFalse_ExcludesDoneOrders`
- `Index_BulkCounts_CorrectApplicableCompletedSpecCount`
- `Edit_UnknownOrderId_ReturnsNotFound`
- `Edit_ValidId_ReturnsAllFiveTabs`
- `Edit_TabParam_SelectsCorrectActiveTab`
- `Edit_InvalidTabParam_FallsBackToVK`
- `AddSpec_HappyPath_PersistsSpec_WithAuditFields`
- `AddSpec_MissingDescription_RedirectsWithWarning`
- `AddSpec_UnknownAssemblyGroup_ReturnsNotFound`
- `EditSpec_HappyPath_UpdatesSpec_WithModifiedAudit`
- `EditSpec_UnknownId_ReturnsNotFound`
- `DeleteSpec_HappyPath_RemovesRow`
- `DeleteSpec_UnknownId_ReturnsNotFound`
- `ToggleIsCompleted_True_SetsCompletedAtAndBy`
- `ToggleIsCompleted_False_ClearsCompletedAtAndBy`

### 12.2 Repository-Tests

Datei `IdealAkeWms.Tests/Repositories/ProductionOrderAssemblyGroupSpecRepositoryTests.cs`:

- `GetByIdAsync_IncludesArticleAndAssemblyGroup`
- `GetByAssemblyGroupIdAsync_SortedBySortOrderThenId`
- `GetByAssemblyGroupIdsAsync_GroupsResultPerKey`
- `AddAsync_AssignsIdAndPersists`
- `UpdateAsync_PersistsFieldChanges`
- `DeleteAsync_RemovesRow`
- `DeleteAsync_UnknownId_NoExceptionNoOp`

### 12.3 Filter-Tests

Datei `IdealAkeWms.Tests/Filters/RequireFaCompletionAccessFilterTests.cs`:

- `Admin_PassesThrough`
- `FaCompletionUser_PassesThrough`
- `PickerOnly_RedirectsToAccessDenied`
- `Anonymous_RedirectsToAccessDenied`

Datei `IdealAkeWms.Tests/Filters/RequirePickingOrFaCompletionAccessFilterTests.cs`:

- `Picker_PassesThrough`
- `FaCompletion_PassesThrough`
- `Tracker_RedirectsToAccessDenied`
- `Admin_PassesThrough`

### 12.4 Phase-2-Regression

`AssemblyGroupsApiControllerTests` (Phase 1) wird um einen Test erweitert:

- `ToggleApplicable_FaCompletionUserOnly_PassesAndPersists`

Stellt sicher, dass Phase-1-Endpoint nach dem Filter-Wechsel auch fuer `fa_completion`-User funktioniert.

### 12.5 Test-Datentopf

Phase-1-Praezedenz `TestApplicationDbContext` (CLAUDE.md "InMemory DB unterstuetzt kein `rowversion`") wird verwendet. AssemblyGroups werden im Setup eager erzeugt (5 Zeilen pro Test-FA), Specs werden per Test in der Arrange-Phase angelegt.

## 13. Risiken

### 13.1 Permission-Filter-Wechsel auf `assembly-groups/toggle-applicable` bricht Phase 2

**Risiko:** Phase-2-PickingLeitstand-View toggelt VK/VL/VE/VT/VA-Checkboxen via dem API-Endpoint. Wechsel von `RequirePickingAccess` zu `RequirePickingOrFaCompletionAccess` darf Picker nicht ausschliessen.

**Mitigation:** Der neue Filter pruet `CanPickAsync || CanCompleteFaAsync` — der Picker-Pfad ist unberuehrt. Test `ToggleApplicable_PickerStill_Passes` als expliziter Regression-Test (siehe 12.4).

### 13.2 Article-Select2-Performance bei sehr grossem Article-Master

`/api/articles/search` liefert default `limit=50`. Wenn der Article-Master > 50k Eintraege hat, ist das Server-side LIKE-Query auf `ArticleNumber + Description` der Bottleneck. Mitigation: `IArticleRepository.SearchAsync` ist bereits paginiert (default 50). Phase-4-View setzt `minimumInputLength: 2`, damit keine Volltext-Suche bei leerem Input ausgeloest wird.

**Open:** falls Performance trotzdem leidet (>500 ms typische Response), waere `[OutputCache]` auf dem Endpoint eine Folge-Optimierung. Recommended default fuer Phase 4: kein Caching, beobachten.

### 13.3 IsApplicable-Toggle ohne Page-Reload

`IsApplicable` wird via JSON-Endpoint geschrieben, der Tab-Header zeigt aber `(offen)`/`(vollst.)` server-rendered. Nach Toggle muss entweder ein Reload erfolgen oder das Tab-Label JS-seitig aktualisiert werden.

**Entscheidung:** Phase-4-View macht **keinen** JS-seitigen Re-Render. Toggle gibt sofortiges visuelles Feedback ueber die Checkbox selbst; das Tab-Header-Indicator wird beim naechsten Reload oder Tab-Wechsel aktualisiert. Begruendung: einfach, robust, ausreichend fuer den Use-Case.

### 13.4 Spec-Loeschung bei aktivem `IsCompleted`

Wenn eine Gruppe `IsCompleted=true` ist und User loescht alle Specs der Gruppe — bleibt sie `IsCompleted=true`? **Akzeptiert: ja, manuelle User-Entscheidung.** Wer Specs loescht, hat selbst die Verantwortung den `IsCompleted`-Status zu pruefen. Kein automatisches Zuruecksetzen. (Konsistent mit dem Verhalten in `WorkOperations` o.ae., wo zugehoerige Daten nicht kaskadieren.)

### 13.5 Article-Loeschung im Master-Sync

`Article.OnDelete = SetNull` (Phase 1 Schema). Wenn Sage-Sync einen Artikel hard-deletet, behaelt die Spec `Description`/`Quantity`/`Notes` und verliert nur die FK. UI-Rendering: Select2 zeigt leere Option, Description bleibt sichtbar. Pfleger muss neu zuordnen. **Akzeptiertes Verhalten.**

### 13.6 Concurrent-Edit-Race-Condition

Zwei FA-Vervollstaendigungs-User pflegen gleichzeitig dieselbe Gruppe. EF-`ModifiedAt`-Audit zeigt `LastWriter wins`. Kein Optimistic-Concurrency-Token auf `ProductionOrderAssemblyGroupSpec`. **Akzeptiert** — Tabelle ist klein, Konflikt unwahrscheinlich, Roll-Back-Aufwand groesser als der Nutzen.

### 13.7 Spec-Sort-Order-Kollisionen

Mehrere Specs mit gleichem `SortOrder` werden nach `Id` sekundaer sortiert. Pfleger kann Werte gleich vergeben, Reihenfolge bleibt stabil. Kein Eindeutigkeit-Constraint.

### 13.8 Nav-Bar-Sichtbarkeit fuer reine `fa_completion`-User

Ein User mit nur `fa_completion`-Rolle sieht weder Lager noch Kommissionierung noch Tracking — nur FA-Vervollstaendigung und Home. Das ist erwartet, aber visuell mager. **Akzeptiert:** Rolle ist ausdruecklich fokussiert.

### 13.9 `IsApplicable=false` + Specs vorhanden

User kann Specs in einer Gruppe pflegen, ohne `IsApplicable=true` zu setzen. Phase 5 (Werkbank-View) wird Specs nur fuer `IsApplicable=true` zeigen — also "verwaiste" Specs koennen vorkommen. **Akzeptiert:** Spec-Pflege ist von der Applicability entkoppelt. Pfleger kann z.B. Specs vorbereiten und erst spaeter die Gruppe aktivieren.

### 13.10 Tab-Wechsel verliert nicht-gespeicherte Add-Form-Daten

Inline-Add-Form ist nicht persistent — Tab-Wechsel oder Browser-Back loescht die Eingabe. **Akzeptiert:** Standard-Web-Verhalten. Optional kann localStorage-Backup nachgereicht werden (nicht in Phase 4).

## 14. Manuelle Test-Szenarien (fuer `docs/TESTSZENARIEN.md`)

Werden in einem neuen TS-12-Block "FA-Vervollstaendigung" eingefuegt.

### TS-12.1 — Index lädt mit korrektem Filter

**Vorbedingungen:** User mit Rolle `fa_completion`. Mindestens 3 FAs in DB, eine davon `IsDone=true`.

**Schritte:**
1. Login. Nav-Bar: "FA-Vervollstaendigung" anklicken.
2. Filter: FA-Nr.-Teilstring eingeben → Submit.
3. Filter zuruecksetzen, "Erledigte zeigen" anhaken → Submit.

**Erwartet:**
- URL `/FaCompletion/Index`.
- 9 Spalten: FA Nr., Stk., Kunde, Artikelnummer, Bezeichnung 1, Fert.-Termin, Anwendbar, Vervollstaendigt, Auspraegungen, Aktion.
- Nach Filter: nur passende FAs sichtbar.
- ShowDone=true: erledigte FAs sind eingeblendet, Zeile `table-secondary`.

### TS-12.2 — Edit-Page laedt 5 Tabs in korrekter Reihenfolge

**Vorbedingungen:** Beliebiger FA `Id=123`, alle 5 AssemblyGroups vorhanden (Phase 1 eager-create).

**Schritte:**
1. `/FaCompletion/Edit/123` aufrufen.
2. Tab "VL" anklicken.
3. Tab "VE" anklicken.

**Erwartet:**
- Tabs in Reihenfolge: VK Kuehlung, VL Lueftung, VE Elektro, VT Tueren, VA Aufbau.
- URL nach Tab-Klick: `/FaCompletion/Edit/123?tab=VL` bzw. `?tab=VE`.
- Aktiv markierter Tab passt zum URL-Param.
- FA-Header oben zeigt OrderNumber, Customer, ArticleNumber, Description1, Termine.

### TS-12.3 — Spec hinzufuegen, editieren, loeschen

**Vorbedingungen:** FA `Id=123`, Tab `VL`. Article-Master enthaelt Artikel "100023 - Lueftermotor".

**Schritte:**
1. Edit-Page, Tab VL. Inline-Form ausfuellen: Article "100023", Description "Lueftermotor 230V Standard", Menge `1.000`, Notizen "Modell ABC".
2. "+"-Button.
3. Tabelle: neue Zeile, Description-Feld editieren auf "Lueftermotor 230V Premium", Submit "Speichern".
4. "Loeschen"-Button, Confirm-Dialog bestaetigen.

**Erwartet:**
- Schritt 2: TempData["SuccessMessage"] sichtbar, neue Zeile in Tabelle.
- Schritt 3: TempData["SuccessMessage"], Description aktualisiert, `ModifiedAt`/`ModifiedBy` gesetzt.
- Schritt 4: Zeile verschwindet, TempData["SuccessMessage"] sichtbar.

### TS-12.4 — IsApplicable-Toggle setzt Wert und greift in PickingLeitstand

**Vorbedingungen:** User mit `fa_completion` + `picking`-Rolle. FA `Id=123`, Tab `VL`, `IsApplicable=false`.

**Schritte:**
1. Edit-Page, Tab VL. Checkbox "Anwendbar" anhaken.
2. Browser-DevTools-Network-Tab: erwartet `POST /api/assembly-groups/toggle-applicable` mit Status 200.
3. Wechsel zu `/PickingLeitstand/Index` (in eigenem Tab).

**Erwartet:**
- Schritt 2: 200 OK. Checkbox bleibt angehakt.
- Schritt 3: FA 123 zeigt VL-Spalte als angehakt (Phase-2-Index-View liest aus derselben Tabelle).
- DB-Check: `ProductionOrderAssemblyGroups` Zeile `(123, VL)` hat `IsApplicable=1`.

### TS-12.5 — IsCompleted-Toggle setzt Audit-Felder

**Vorbedingungen:** FA `Id=123`, Tab `VL`, `IsCompleted=false`.

**Schritte:**
1. Edit-Page Tab VL. Checkbox "Vervollstaendigt" anhaken → Form-POST.
2. Page reloadet, Label zeigt "Vervollstaendigt — Max Mustermann am 15.05.2026 14:30".
3. Checkbox erneut anklicken (entfernen).

**Erwartet:**
- Schritt 2: `IsCompleted=true`, `CompletedAt = jetzt`, `CompletedBy = aktueller Benutzer`.
- Schritt 3: `IsCompleted=false`, `CompletedAt=NULL`, `CompletedBy=NULL`.

### TS-12.6 — Permission: Picker ohne FA-Completion-Rolle sieht keine Edit-Page

**Vorbedingungen:** User mit `picking` aber ohne `fa_completion`.

**Schritte:**
1. Nav-Bar erwarten: "FA-Vervollstaendigung"-Eintrag NICHT sichtbar.
2. Direkter URL-Aufruf `/FaCompletion/Index`.

**Erwartet:**
- Schritt 1: kein Menue-Eintrag.
- Schritt 2: Redirect zu `/Account/AccessDenied`.

## 15. Versionierung + Doku

- `IdealAkeWms/AppVersion.cs`: `1.12.0` → `1.13.0`, Date `2026-05-15`.
- `IDEALAKEWMSService/AppVersion.cs`: gleich (auch wenn der Service nicht direkt betroffen ist — Versionsgleichheit zur Vereinfachung der Release-Builds).
- `Views/Help/Changelog.cshtml`: neue Card v1.13.0 mit Schwerpunkten "FA-Vervollstaendigung (Phase 4)", "Neue Rolle `fa_completion`", "Neue API-Permission auf `/api/assembly-groups/toggle-applicable`".
- `Views/Help/Index.cshtml`: neue Section "FA-Vervollstaendigung" mit Beschreibung der Tabs + Workflow (Index → Edit → Specs → IsApplicable/IsCompleted).
- `CLAUDE.md`:
  - Rollen-Tabelle: `fa_completion` ergaenzt.
  - Berechtigungstabelle: `RequireFaCompletionAccess` + `RequirePickingOrFaCompletionAccess` ergaenzt; `RequirePickingAccess` "Angewendet auf"-Spalte gekuerzt.
  - Neuer Fallstrick **"FA-Vervollstaendigung-Toggle vs PickingLeitstand-Toggle"**: beide Views teilen sich den `POST /api/assembly-groups/toggle-applicable`-Endpoint. Filter `RequirePickingOrFaCompletionAccess` erlaubt beide Rollen. Bei zukuenftigen Toggle-Aenderungen muss beachtet werden, dass JS-Code in **beiden** Views (Phase 2 `Views/PickingLeitstand/Index.cshtml` + Phase 4 `Views/FaCompletion/Edit.cshtml`) angefasst werden muss.
- `docs/TESTSZENARIEN.md`: neuer TS-12-Block mit 6 Szenarien (siehe 14).

## 16. Code-Punkte-Referenz

- [ProductionOrderAssemblyGroup.cs](IdealAkeWms/Models/ProductionOrderAssemblyGroup.cs) — Entity (Phase 1).
- [ProductionOrderAssemblyGroupSpec.cs](IdealAkeWms/Models/ProductionOrderAssemblyGroupSpec.cs) — Entity (Phase 1, in Phase 4 erstmals genutzt).
- [ApplicationDbContext.cs:443-470](IdealAkeWms/Data/ApplicationDbContext.cs#L443-L470) — Spec-Entity-Konfiguration (Phase 1).
- [ProductionOrderAssemblyGroupRepository.cs](IdealAkeWms/Data/Repositories/ProductionOrderAssemblyGroupRepository.cs) — bestehendes Repo, in Phase 4 um `GetByIdAsync`, `GetByProductionOrderIdsAsync`, `SetIsCompletedAsync` und `UpdateAsync` erweitert.
- [AssemblyGroupsApiController.cs:10](IdealAkeWms/Controllers/AssemblyGroupsApiController.cs#L10) — `[RequirePickingAccess]` → `[RequirePickingOrFaCompletionAccess]` (Phase 4 Update).
- [ArticlesApiController.cs:17-26](IdealAkeWms/Controllers/ArticlesApiController.cs#L17-L26) — bestehender Article-Search-Endpoint, Phase 4 reuse.
- [PickingLeitstandController.cs](IdealAkeWms/Controllers/PickingLeitstandController.cs) — Phase-2-Vorbild fuer den neuen Controller (DI, Class-Filter, Bulk-Loads).
- [RequirePickingAccessAttribute.cs](IdealAkeWms/Filters/RequirePickingAccessAttribute.cs) — Vorbild fuer beide neuen Filter.
- [RequirePickingOrLeitstandAccessAttribute.cs](IdealAkeWms/Filters/RequirePickingOrLeitstandAccessAttribute.cs) — Phase-2-Vorbild fuer `RequirePickingOrFaCompletionAccess`.
- [CurrentUserService.cs:81-97](IdealAkeWms/Services/CurrentUserService.cs#L81-L97) — Permission-Helpers, `CanCompleteFaAsync` analog ergaenzen.
- [Program.cs:122-135](IdealAkeWms/Program.cs#L122-L135) — Rollen-Seed, `fa_completion` ergaenzen.
- [BdeMasterData/Index.cshtml:13-26](IdealAkeWms/Views/BdeMasterData/Index.cshtml#L13-L26) — Vorbild fuer `nav-tabs`-Pattern mit URL-State.
- [_Layout.cshtml:65-95](IdealAkeWms/Views/Shared/_Layout.cshtml#L65-L95) — Nav-Bar-Block, Phase-4-Eintrag einfuegen.
- [ColumnDefinitions.cs](IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs) — neuer `FaCompletion`-ViewConfig.

## 17. Offene Entscheidungen

| Punkt | Entscheidung | Recommended default fuer Plan |
|---|---|---|
| Article-Search-Endpoint | Reuse vorhandener `/api/articles/search` | **Reuse** — kein neuer Endpoint. |
| Description-Autofill bei Article-Select | leer lassen oder mit Article.Description vorbefuellen? | **Vorbefuellen, aber editierbar** — JS-Snippet im Select2-`select`-Event setzt `input[name=Description]` auf Article.Description, ueberschreibt aber nur wenn aktuell leer. |
| Add-Form: inline vs Modal | inline-Row am Tabellen-Ende | **Inline** (Spec 9.5). |
| CompletedBy: alle `fa_completion`-User oder gesonderte Rolle? | alle `fa_completion`-User duerfen togglen | **Alle FA-Completion** — keine zusaetzliche Sub-Rolle. |
| Permission `assembly-groups/toggle-applicable` | bestehend `[RequirePickingAccess]` → `[RequirePickingOrFaCompletionAccess]` | **Filter erweitern** (Spec 5.3). |
| Sort/Pagination innerhalb eines Tabs | keine Pagination, alle Specs sichtbar | **Keine Pagination** — Spec-Count pro Gruppe in der Praxis < 30. Falls > 100 in der Praxis: spaeter nachreichen. |
| Article-Group-Filter | UI-only Filter pro Tab? | **Nicht in Phase 4** — Pfleger nutzt Volltext-Search. Optional fuer Phase 4.1. |
| Nav-Eintrag-Sichtbarkeit | immer sichtbar fuer `fa_completion` | **Immer sichtbar**, kein Conditional auf "es gibt offene Gruppen". |

Alle anderen Entscheidungen sind aus Roadmap Q1-Q11 + Phase 1+2 abgeleitet.

## 18. Self-Review — Spec-Sektion → Plan-Task-Mapping

| Spec-Sektion | Inhalt | Plan-Task |
|---|---|---|
| 5.1 — Neue Rolle `fa_completion` | RoleKeys-Konstante + Seed | **Task 1, Steps 1-2** |
| 5.2 — `RequireFaCompletionAccess`-Filter + `CanCompleteFaAsync` | neuer Filter + Service-Method | **Task 1, Steps 3-4** |
| 5.3 — `RequirePickingOrFaCompletionAccess`-Filter + API-Update | zweiter Filter + Aenderung an AssemblyGroupsApiController | **Task 1, Steps 5-6** |
| 6 — `IProductionOrderAssemblyGroupSpecRepository` + Impl + DI | neues Repo | **Task 2** |
| 6 Annex — Repo-Erweiterungen `IProductionOrderAssemblyGroupRepository` (GetById, GetByProductionOrderIds, SetIsCompletedAsync) | drei neue Methoden | **Task 2, Steps 4-5** |
| 7 — ViewModels (List + Edit + Tab + SpecForm) | FaCompletionViewModels.cs | **Task 3** |
| 8 — Controller (6 Actions) | FaCompletionController.cs | **Task 4** |
| 9.1 — Index-View | Views/FaCompletion/Index.cshtml | **Task 5, Step 1** |
| 9.2 — Edit-View (Tabs) | Views/FaCompletion/Edit.cshtml | **Task 5, Step 2** |
| 9.3 — `_SpecRow.cshtml` partial | Spec-Row-Partial | **Task 5, Step 3** |
| 9.4 — Article-Select2-Reuse | JS-Snippet | **Task 5, Step 4** |
| 9.5 — Inline-Add (Entscheidung) | Spec-Doku | folgt aus 8.4/9.2 — kein eigener Task |
| 9.6 — Nav-Bar-Entry | _Layout.cshtml | **Task 6** |
| 10 — API-Permission-Update | `[RequirePickingOrFaCompletionAccess]` | **Task 1, Step 6** (gleichzeitig mit Filter) |
| 11 — ColumnDefinitions-Erweiterung | `ViewConfig FaCompletion` | **Task 3, Step 3** |
| 12.1 — Controller-Tests | 16 Tests | **Task 7, Step 1** |
| 12.2 — Repo-Tests | 7 Tests | **Task 7, Step 2** |
| 12.3 — Filter-Tests | 8 Tests | **Task 7, Step 3** |
| 12.4 — Phase-2-Regression-Test | 1 Test in `AssemblyGroupsApiControllerTests` | **Task 7, Step 4** |
| 14 — TESTSZENARIEN | 6 Szenarien TS-12.1..12.6 | **Task 8, Step 4** |
| 15 — Versionierung + Doku | AppVersion, Changelog, Help, CLAUDE.md | **Task 8, Steps 1-3** |
| 17 — Offene Entscheidungen | 8 `**Open:**`-Punkte mit Recommended defaults | werden im Plan im Verlauf entschieden |

**Coverage-Verifikation:** alle nicht-Out-of-Scope-Sektionen sind durch mindestens einen Plan-Task abgedeckt. Sektionen 3 (Out-of-Scope), 13 (Risiken) und 16 (Code-Punkte) sind reine Doku, ohne separaten Task.

---

**Hinweis:** Phase 5 (Arbeitsplatz-BOM-View) wird nach Phase-4-Live-Verifikation (5 Tage) als eigene Detail-Spec geschrieben. Phase 5 nutzt die in Phase 4 gepflegten Specs read-only.

# ProductionOrder-Split — Phase 4 FA-Vervollstaendigung — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Pro FA eine Detail-Page mit Tabs VK/VL/VE/VT/VA aufbauen, in denen Merkmalsauspraegungen (ArticleId + Description + Quantity + Notes + SortOrder) gepflegt werden. Neue Rolle `fa_completion`, eigener Controller `FaCompletionController`, neue Views, Nav-Bar-Eintrag. Bestehender Phase-1-API-Endpoint `POST /api/assembly-groups/toggle-applicable` wird per Filter-Wechsel auch fuer die neue Rolle freigeschaltet.

Phase 4 ist reine Code-Erweiterung: kein Schema-Migration noetig (Phase 1 hat alle Tabellen angelegt). Kein Wartungsfenster, Standard-Deploy.

**Spec:** `docs/superpowers/specs/2026-05-15-production-order-split-phase-4-design.md`.

**Roadmap:** `docs/superpowers/specs/2026-05-12-production-order-split-roadmap.md`, Sektion 8.

**Phase-1-Referenz:** `docs/superpowers/specs/2026-05-12-production-order-split-phase-1-design.md` (Schema, AssemblyGroup + AssemblyGroupSpec).
**Phase-2-Referenz:** `docs/superpowers/specs/2026-05-12-production-order-split-phase-2-design.md` (Controller-Split + Permission-Filter-Pattern).

**Branch:** `refactor/fa-logic` als direkte Fortsetzung von Phase 1+2 (aktueller HEAD `72e036a`).

**AppVersion:** `1.12.0` (Phase 2) → `1.13.0`, Datum `2026-05-15`.

**Commit-Konvention:** `feat(fa-completion): ...` / `refactor(api): ...` / `docs: ...`. Co-Authored-By trailer im HEREDOC.

**Architecture (5 Schichten + Tests + Doku):**
- **Rolle + Filter + Permission-API-Update** — `RoleKeys.FaCompletion`, Seed, `CanCompleteFaAsync`, `RequireFaCompletionAccess`-Filter, `RequirePickingOrFaCompletionAccess`-Filter, API-Endpoint-Filter-Wechsel.
- **Repository** — neues `IProductionOrderAssemblyGroupSpecRepository` + Impl + DI; plus Erweiterungen am bestehenden `IProductionOrderAssemblyGroupRepository`.
- **ViewModels** — `FaCompletionListViewModel` + `FaCompletionEditViewModel` + `AssemblyGroupTabViewModel` + `AssemblyGroupSpecFormModel`; `ColumnDefinitions.FaCompletion`.
- **Controller + Views** — `FaCompletionController` (6 Actions), `Views/FaCompletion/Index.cshtml`, `Views/FaCompletion/Edit.cshtml`, `Views/FaCompletion/_SpecRow.cshtml`.
- **Nav-Bar + Doku** — `_Layout.cshtml`, `CLAUDE.md`, `Changelog.cshtml`, `Help/Index.cshtml`, `TESTSZENARIEN.md`, `AppVersion.cs`.

**Critical sequencing constraints:**
1. Task 1 (Rolle + Filter) MUSS vor Task 4 (Controller) abgeschlossen sein — der Controller referenziert `[RequireFaCompletionAccess]`.
2. Task 2 (Repo) MUSS vor Task 4 abgeschlossen sein — der Controller injiziert das neue Repo.
3. Task 3 (ViewModels) MUSS vor Task 4 + Task 5 abgeschlossen sein — beide brauchen die VMs.
4. Task 5 (Views) MUSS nach Task 4 (Controller) erfolgen — Views referenzieren `asp-action`-Targets.
5. Task 7 (Tests) kann ab Task 4 abschnittweise nachgezogen werden.

**Files (Gesamtuebersicht):**

**New:**
- `IdealAkeWms/Filters/RequireFaCompletionAccessAttribute.cs`
- `IdealAkeWms/Filters/RequirePickingOrFaCompletionAccessAttribute.cs`
- `IdealAkeWms/Data/Repositories/IProductionOrderAssemblyGroupSpecRepository.cs`
- `IdealAkeWms/Data/Repositories/ProductionOrderAssemblyGroupSpecRepository.cs`
- `IdealAkeWms/Models/ViewModels/FaCompletionViewModels.cs`
- `IdealAkeWms/Controllers/FaCompletionController.cs`
- `IdealAkeWms/Views/FaCompletion/Index.cshtml`
- `IdealAkeWms/Views/FaCompletion/Edit.cshtml`
- `IdealAkeWms/Views/FaCompletion/_SpecRow.cshtml`
- `IdealAkeWms.Tests/Controllers/FaCompletionControllerTests.cs`
- `IdealAkeWms.Tests/Repositories/ProductionOrderAssemblyGroupSpecRepositoryTests.cs`
- `IdealAkeWms.Tests/Filters/RequireFaCompletionAccessFilterTests.cs`
- `IdealAkeWms.Tests/Filters/RequirePickingOrFaCompletionAccessFilterTests.cs`

**Modify:**
- `IdealAkeWms/Models/RoleKeys.cs` — neue Konstante `FaCompletion`
- `IdealAkeWms/Program.cs` — Seed + Repo-DI
- `IdealAkeWms/Services/ICurrentUserService.cs` + `CurrentUserService.cs` — `CanCompleteFaAsync`
- `IdealAkeWms/Controllers/AssemblyGroupsApiController.cs` — Filter-Wechsel
- `IdealAkeWms/Data/Repositories/IProductionOrderAssemblyGroupRepository.cs` + Impl — 3 neue Methoden
- `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` — neuer `FaCompletion`-ViewConfig
- `IdealAkeWms/Views/Shared/_Layout.cshtml` — Nav-Bar-Eintrag
- `IdealAkeWms/AppVersion.cs` → `1.13.0`
- `IDEALAKEWMSService/AppVersion.cs` → `1.13.0`
- `IdealAkeWms/Views/Help/Changelog.cshtml` — neue Card v1.13.0
- `IdealAkeWms/Views/Help/Index.cshtml` — neue Section "FA-Vervollstaendigung"
- `CLAUDE.md` — Rolle, Filter, Fallstrick
- `docs/TESTSZENARIEN.md` — TS-12-Block
- `IdealAkeWms.Tests/Controllers/AssemblyGroupsApiControllerTests.cs` — 1 zusaetzlicher Regression-Test

**Delete:** keine.

---

## Task 0: Pre-Conditions verifizieren

**Files:** keine — reiner Read-Only-Check.

- [ ] **Step 1: Branch-Spitze pruefen**

```pwsh
git -C .claude/worktrees/refactor-fa-logic log --oneline -5
```

Erwartet: `72e036a` oder neuer als HEAD. Phase-1+2-Code ist enthalten.

- [ ] **Step 2: Build + Tests gruen**

```pwsh
dotnet build .claude/worktrees/refactor-fa-logic/IdealAkeWms.slnx --nologo
dotnet test  .claude/worktrees/refactor-fa-logic/IdealAkeWms.slnx --nologo --no-build --filter "Category!=SqlServerOnly"
```

Erwartet: alles gruen. Falls rot → Phase-1/2-Hotfixes zuerst.

- [ ] **Step 3: Schema-Vorbedingungen verifizieren**

```pwsh
rg -n "ProductionOrderAssemblyGroupSpecs|ProductionOrderAssemblyGroups" `
  .claude/worktrees/refactor-fa-logic/IdealAkeWms/Data/ApplicationDbContext.cs
```

Erwartet: beide DbSets + Entity-Konfigurationen aus Phase 1 vorhanden (siehe [`ApplicationDbContext.cs:443-470`](IdealAkeWms/Data/ApplicationDbContext.cs#L443-L470)). Falls fehlend → Phase 1 unvollstaendig, Plan blockiert.

- [ ] **Step 4: Article-Search-Endpoint pruefen**

```pwsh
rg -n "api/articles/search|class ArticlesApiController" `
  .claude/worktrees/refactor-fa-logic/IdealAkeWms/Controllers
```

Erwartet: `ArticlesApiController.cs:17` `[HttpGet("search")]` mit `q`+`limit`-QueryParams (Spec 9.4). Falls fehlend → eigener Endpoint im Controller noetig (Spec-Plan haette dann zusaetzlichen Task).

- [ ] **Step 5: Phase-1-Repo-Methoden vorhanden**

```pwsh
rg -n "GetIsApplicablePivotAsync|GetByPoAndKeyAsync|SetIsApplicableAsync" `
  .claude/worktrees/refactor-fa-logic/IdealAkeWms/Data/Repositories/ProductionOrderAssemblyGroupRepository.cs
```

Erwartet: alle drei Methoden existieren. Phase 4 erweitert das Repo um weitere drei Methoden (Task 2).

- [ ] **Step 6: Existing-Filter-Vorbilder lesen**

```pwsh
cat .claude/worktrees/refactor-fa-logic/IdealAkeWms/Filters/RequirePickingAccessAttribute.cs
cat .claude/worktrees/refactor-fa-logic/IdealAkeWms/Filters/RequirePickingOrLeitstandAccessAttribute.cs
```

Bestaetigt Pattern (siehe Spec 5.2/5.3 — analog). Notieren: Konstruktor-Injection von `ICurrentUserService`, AccessDenied-Redirect-Default.

---

## Task 1: Rolle + Filter + API-Permission-Update

**Files:**
- Modify: `IdealAkeWms/Models/RoleKeys.cs`
- Modify: `IdealAkeWms/Program.cs`
- Modify: `IdealAkeWms/Services/ICurrentUserService.cs`
- Modify: `IdealAkeWms/Services/CurrentUserService.cs`
- New: `IdealAkeWms/Filters/RequireFaCompletionAccessAttribute.cs`
- New: `IdealAkeWms/Filters/RequirePickingOrFaCompletionAccessAttribute.cs`
- Modify: `IdealAkeWms/Controllers/AssemblyGroupsApiController.cs`

- [ ] **Step 1: `RoleKeys.FaCompletion`-Konstante ergaenzen**

In `IdealAkeWms/Models/RoleKeys.cs` direkt nach `Leitstand` (vor `BdeUser`) einfuegen:

```csharp
public const string FaCompletion = "fa_completion";
```

- [ ] **Step 2: Rollen-Seed in `Program.cs` ergaenzen**

In `IdealAkeWms/Program.cs` im `defaultRoles`-Array (heutige Zeilen ~122-135) direkt nach dem `Leitstand`-Eintrag einfuegen:

```csharp
(RoleKeys.FaCompletion, "FA-Vervollstaendigung",
    "Merkmalsauspraegungen pro Vormontageplatz pro FA pflegen", 80),
```

`SortOrder=80` haelt `fa_completion` zwischen Leitstand (70) und BDE-Rollen (100+).

- [ ] **Step 3: `CanCompleteFaAsync` im `ICurrentUserService` + Impl**

In `IdealAkeWms/Services/ICurrentUserService.cs` nach `CanManagePickingReleaseAsync` einfuegen:

```csharp
Task<bool> CanCompleteFaAsync();
```

In `IdealAkeWms/Services/CurrentUserService.cs` analog zur Phase-1-Praezedenz ([`CurrentUserService.cs:96-97`](IdealAkeWms/Services/CurrentUserService.cs#L96-L97)) ergaenzen:

```csharp
public async Task<bool> CanCompleteFaAsync()
    => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.FaCompletion);
```

- [ ] **Step 4: `RequireFaCompletionAccessAttribute` anlegen**

Datei `IdealAkeWms/Filters/RequireFaCompletionAccessAttribute.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

public class RequireFaCompletionAccessAttribute : TypeFilterAttribute
{
    public RequireFaCompletionAccessAttribute() : base(typeof(RequireFaCompletionAccessFilter)) { }
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

- [ ] **Step 5: `RequirePickingOrFaCompletionAccessAttribute` anlegen**

Datei `IdealAkeWms/Filters/RequirePickingOrFaCompletionAccessAttribute.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

public class RequirePickingOrFaCompletionAccessAttribute : TypeFilterAttribute
{
    public RequirePickingOrFaCompletionAccessAttribute() : base(typeof(RequirePickingOrFaCompletionAccessFilter)) { }
}

public class RequirePickingOrFaCompletionAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequirePickingOrFaCompletionAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

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

- [ ] **Step 6: API-Endpoint `assembly-groups/toggle-applicable` Filter wechseln**

In `IdealAkeWms/Controllers/AssemblyGroupsApiController.cs` (siehe [`AssemblyGroupsApiController.cs:10`](IdealAkeWms/Controllers/AssemblyGroupsApiController.cs#L10)) das Class-Level-Attribut anpassen:

```diff
 [Route("api/assembly-groups")]
 [ApiController]
-[RequirePickingAccess]
+[RequirePickingOrFaCompletionAccess]
 public class AssemblyGroupsApiController : ControllerBase
```

Body bleibt unveraendert. Picker-Zugriff bleibt durch den OR-Filter erhalten.

- [ ] **Step 7: Build verifizieren**

```pwsh
dotnet build .claude/worktrees/refactor-fa-logic/IdealAkeWms.slnx --nologo
```

Erwartet: gruen. Falls rot:
- `CanCompleteFaAsync`-Signature in `ICurrentUserService` und `CurrentUserService` muessen exakt uebereinstimmen.
- Filter-Klassen-Namen + Konstruktor-Argument-Reihenfolge konsistent.

- [ ] **Step 8: Commit**

```pwsh
git -C .claude/worktrees/refactor-fa-logic add `
  IdealAkeWms/Models/RoleKeys.cs `
  IdealAkeWms/Program.cs `
  IdealAkeWms/Services/ICurrentUserService.cs `
  IdealAkeWms/Services/CurrentUserService.cs `
  IdealAkeWms/Filters/RequireFaCompletionAccessAttribute.cs `
  IdealAkeWms/Filters/RequirePickingOrFaCompletionAccessAttribute.cs `
  IdealAkeWms/Controllers/AssemblyGroupsApiController.cs

git -C .claude/worktrees/refactor-fa-logic commit -m @'
feat(fa-completion): phase 4 task 1 - new role + filters + api permission

Add fa_completion role with seed in Program.cs. New
RequireFaCompletionAccess and RequirePickingOrFaCompletionAccess filters.
The existing /api/assembly-groups/toggle-applicable endpoint changes from
[RequirePickingAccess] to [RequirePickingOrFaCompletionAccess] - both
picker (phase 2 PickingLeitstand) and fa-completion (phase 4 FaCompletion)
roles can now toggle IsApplicable per assembly group.

Spec sections 5.1, 5.2, 5.3, 10. Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 2: Repository — `IProductionOrderAssemblyGroupSpecRepository` + AssemblyGroup-Erweiterungen

**Files:**
- New: `IdealAkeWms/Data/Repositories/IProductionOrderAssemblyGroupSpecRepository.cs`
- New: `IdealAkeWms/Data/Repositories/ProductionOrderAssemblyGroupSpecRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/IProductionOrderAssemblyGroupRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderAssemblyGroupRepository.cs`
- Modify: `IdealAkeWms/Program.cs` (DI-Registrierung)

- [ ] **Step 1: `IProductionOrderAssemblyGroupSpecRepository` Interface**

Datei `IdealAkeWms/Data/Repositories/IProductionOrderAssemblyGroupSpecRepository.cs`:

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionOrderAssemblyGroupSpecRepository
{
    Task<ProductionOrderAssemblyGroupSpec?> GetByIdAsync(int id);

    /// <summary>Specs einer AssemblyGroup, sortiert nach SortOrder, dann Id.</summary>
    Task<List<ProductionOrderAssemblyGroupSpec>> GetByAssemblyGroupIdAsync(int assemblyGroupId);

    /// <summary>Bulk-Lookup fuer Edit-View: liefert Specs gruppiert per AssemblyGroupId.</summary>
    Task<Dictionary<int, List<ProductionOrderAssemblyGroupSpec>>>
        GetByAssemblyGroupIdsAsync(IEnumerable<int> assemblyGroupIds);

    Task<int> AddAsync(ProductionOrderAssemblyGroupSpec spec);
    Task UpdateAsync(ProductionOrderAssemblyGroupSpec spec);
    Task DeleteAsync(int id);
}
```

- [ ] **Step 2: `ProductionOrderAssemblyGroupSpecRepository` Impl**

Datei `IdealAkeWms/Data/Repositories/ProductionOrderAssemblyGroupSpecRepository.cs`:

```csharp
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ProductionOrderAssemblyGroupSpecRepository : IProductionOrderAssemblyGroupSpecRepository
{
    private readonly ApplicationDbContext _context;

    public ProductionOrderAssemblyGroupSpecRepository(ApplicationDbContext context)
    {
        _context = context;
    }

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

    public async Task<Dictionary<int, List<ProductionOrderAssemblyGroupSpec>>>
        GetByAssemblyGroupIdsAsync(IEnumerable<int> assemblyGroupIds)
    {
        var ids = assemblyGroupIds.Distinct().ToList();
        var result = new Dictionary<int, List<ProductionOrderAssemblyGroupSpec>>();
        if (ids.Count == 0) return result;

        // Chunking analog zu Phase-1-Pivot (Spec 6) — defensive Versicherung gegen 2100-Parameter-Limit
        const int chunkSize = 1000;
        var rows = new List<ProductionOrderAssemblyGroupSpec>();
        for (int offset = 0; offset < ids.Count; offset += chunkSize)
        {
            var chunk = ids.Skip(offset).Take(chunkSize).ToList();
            var batch = await _context.ProductionOrderAssemblyGroupSpecs
                .Include(s => s.Article)
                .Where(s => chunk.Contains(s.AssemblyGroupId))
                .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
                .ToListAsync();
            rows.AddRange(batch);
        }

        foreach (var grp in rows.GroupBy(s => s.AssemblyGroupId))
            result[grp.Key] = grp.ToList();

        return result;
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

- [ ] **Step 3: DI-Registrierung in `Program.cs`**

Im DI-Block (direkt nach `IProductionOrderAssemblyGroupRepository`-Registrierung, siehe [`Program.cs:48`](IdealAkeWms/Program.cs#L48)) einfuegen:

```csharp
builder.Services.AddScoped<IProductionOrderAssemblyGroupSpecRepository,
                          ProductionOrderAssemblyGroupSpecRepository>();
```

- [ ] **Step 4: `IProductionOrderAssemblyGroupRepository`-Erweiterung — 3 neue Methoden**

In `IdealAkeWms/Data/Repositories/IProductionOrderAssemblyGroupRepository.cs` nach `SetIsApplicableAsync` ergaenzen:

```csharp
Task<ProductionOrderAssemblyGroup?> GetByIdAsync(int id);

Task<List<ProductionOrderAssemblyGroup>> GetByProductionOrderIdsAsync(IEnumerable<int> productionOrderIds);

Task SetIsCompletedAsync(int assemblyGroupId, bool value, string completedBy,
    string modifiedBy, string modifiedByWindows);
```

- [ ] **Step 5: Impl der drei neuen Methoden**

In `IdealAkeWms/Data/Repositories/ProductionOrderAssemblyGroupRepository.cs` am Ende der Klasse ergaenzen:

```csharp
public Task<ProductionOrderAssemblyGroup?> GetByIdAsync(int id)
    => _context.ProductionOrderAssemblyGroups.FirstOrDefaultAsync(g => g.Id == id);

public async Task<List<ProductionOrderAssemblyGroup>> GetByProductionOrderIdsAsync(
    IEnumerable<int> productionOrderIds)
{
    var ids = productionOrderIds.Distinct().ToList();
    var result = new List<ProductionOrderAssemblyGroup>();
    if (ids.Count == 0) return result;

    const int chunkSize = 1000;
    for (int offset = 0; offset < ids.Count; offset += chunkSize)
    {
        var chunk = ids.Skip(offset).Take(chunkSize).ToList();
        var rows = await _context.ProductionOrderAssemblyGroups
            .Where(g => chunk.Contains(g.ProductionOrderId))
            .ToListAsync();
        result.AddRange(rows);
    }
    return result;
}

public async Task SetIsCompletedAsync(int assemblyGroupId, bool value, string completedBy,
    string modifiedBy, string modifiedByWindows)
{
    var row = await _context.ProductionOrderAssemblyGroups
        .FirstOrDefaultAsync(g => g.Id == assemblyGroupId)
        ?? throw new InvalidOperationException($"AssemblyGroup row missing for Id {assemblyGroupId}.");

    row.IsCompleted = value;
    if (value)
    {
        row.CompletedAt = DateTime.Now;
        row.CompletedBy = completedBy;
    }
    else
    {
        row.CompletedAt = null;
        row.CompletedBy = null;
    }
    row.ModifiedAt = DateTime.Now;
    row.ModifiedBy = modifiedBy;
    row.ModifiedByWindows = modifiedByWindows;
    await _context.SaveChangesAsync();
}
```

- [ ] **Step 6: Build verifizieren**

```pwsh
dotnet build .claude/worktrees/refactor-fa-logic/IdealAkeWms.slnx --nologo
```

Erwartet: gruen.

- [ ] **Step 7: Commit**

```pwsh
git -C .claude/worktrees/refactor-fa-logic add `
  IdealAkeWms/Data/Repositories/IProductionOrderAssemblyGroupSpecRepository.cs `
  IdealAkeWms/Data/Repositories/ProductionOrderAssemblyGroupSpecRepository.cs `
  IdealAkeWms/Data/Repositories/IProductionOrderAssemblyGroupRepository.cs `
  IdealAkeWms/Data/Repositories/ProductionOrderAssemblyGroupRepository.cs `
  IdealAkeWms/Program.cs

git -C .claude/worktrees/refactor-fa-logic commit -m @'
feat(fa-completion): phase 4 task 2 - assembly group spec repository

New IProductionOrderAssemblyGroupSpecRepository with CRUD + bulk lookup
via GetByAssemblyGroupIdsAsync (1000er chunking). Existing
IProductionOrderAssemblyGroupRepository gets GetByIdAsync,
GetByProductionOrderIdsAsync (chunked), and SetIsCompletedAsync.

Spec section 6. Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 3: ViewModels + ColumnDefinitions

**Files:**
- New: `IdealAkeWms/Models/ViewModels/FaCompletionViewModels.cs`
- Modify: `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs`

- [ ] **Step 1: `FaCompletionViewModels.cs` anlegen**

Datei `IdealAkeWms/Models/ViewModels/FaCompletionViewModels.cs`. Inhalt 1:1 wie Spec Sektion 7:

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

    public int ApplicableCount { get; set; }
    public int CompletedCount { get; set; }
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

    public string ActiveTab { get; set; } = "VK";
    public List<AssemblyGroupTabViewModel> Tabs { get; set; } = new();
}

public class AssemblyGroupTabViewModel
{
    public int AssemblyGroupId { get; set; }
    public string GroupKey { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public bool IsApplicable { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }
    public List<AssemblyGroupSpecFormModel> Specs { get; set; } = new();
}

// --------- Spec-Form (Add + Edit) ---------

public class AssemblyGroupSpecFormModel
{
    public int Id { get; set; }
    public int AssemblyGroupId { get; set; }
    public int? ArticleId { get; set; }
    public string? ArticleText { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}
```

- [ ] **Step 2: Build verifizieren (Task-3 isoliert)**

```pwsh
dotnet build .claude/worktrees/refactor-fa-logic/IdealAkeWms.slnx --nologo
```

Erwartet: gruen — neue Datei ist additiv.

- [ ] **Step 3: `ColumnDefinitions.cs`-Erweiterung — `FaCompletion`-ViewConfig**

In `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` direkt nach dem Phase-2-`PickingLeitstand`-Block (vor `Picking`) den neuen `FaCompletion`-Block einfuegen:

```csharp
/// <summary>
/// FaCompletion/Index.cshtml columns — Phase 4 FA-Vervollstaendigung-Liste.
/// </summary>
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

Und im `GetByViewKey`-Switch (am Ende der Klasse) den neuen Case ergaenzen:

```csharp
public static ViewConfig? GetByViewKey(string viewKey) => viewKey switch
{
    "ProductionOrders"  => ProductionOrders,
    "PickingLeitstand"  => PickingLeitstand,
    "FaCompletion"      => FaCompletion,     // neu Phase 4
    "Picking"           => Picking,
    // ...
    _                   => null
};
```

- [ ] **Step 4: Build verifizieren**

```pwsh
dotnet build .claude/worktrees/refactor-fa-logic/IdealAkeWms.slnx --nologo
```

Erwartet: gruen.

- [ ] **Step 5: Commit**

```pwsh
git -C .claude/worktrees/refactor-fa-logic add `
  IdealAkeWms/Models/ViewModels/FaCompletionViewModels.cs `
  IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs

git -C .claude/worktrees/refactor-fa-logic commit -m @'
feat(fa-completion): phase 4 task 3 - view models + column definitions

New FaCompletionListViewModel + FaCompletionEditViewModel +
AssemblyGroupTabViewModel + AssemblyGroupSpecFormModel in
FaCompletionViewModels.cs. ColumnDefinitions gets FaCompletion ViewConfig
(10 columns, order-number + row-actions locked).

Spec sections 7, 11. Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 4: `FaCompletionController`

**Files:**
- New: `IdealAkeWms/Controllers/FaCompletionController.cs`

Dieser Task ist der groesste in Phase 4 (6 Actions). Wir teilen ihn in 8 Steps auf: 1 Skelett, 6 Actions, 1 Build.

- [ ] **Step 1: Datei-Skelett mit DI**

Datei `IdealAkeWms/Controllers/FaCompletionController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

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
    private readonly ICurrentUserService _currentUser;

    public FaCompletionController(
        IProductionOrderRepository productionOrderRepository,
        IProductionOrderAssemblyGroupRepository assemblyGroupRepository,
        IProductionOrderAssemblyGroupSpecRepository specRepository,
        ICurrentUserService currentUser)
    {
        _productionOrderRepository = productionOrderRepository;
        _assemblyGroupRepository = assemblyGroupRepository;
        _specRepository = specRepository;
        _currentUser = currentUser;
    }

    // Actions in nachfolgenden Steps:
    //   - Index             (Step 2)
    //   - Edit/{id}         (Step 3)
    //   - AddSpec    (POST) (Step 4)
    //   - EditSpec   (POST) (Step 5)
    //   - DeleteSpec (POST) (Step 6)
    //   - ToggleIsCompleted (POST) (Step 7)
}
```

- [ ] **Step 2: `Index`-Action**

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
    var pivot = await _assemblyGroupRepository.GetIsApplicablePivotAsync(orderIds);
    var groupRows = await _assemblyGroupRepository.GetByProductionOrderIdsAsync(orderIds);
    var groupIds = groupRows.Select(g => g.Id).ToList();
    var specsByGroup = await _specRepository.GetByAssemblyGroupIdsAsync(groupIds);

    var completedByOrder = groupRows
        .GroupBy(g => g.ProductionOrderId)
        .ToDictionary(g => g.Key, g => g.Count(x => x.IsCompleted));

    var specCountByOrder = groupRows
        .GroupBy(g => g.ProductionOrderId)
        .ToDictionary(
            g => g.Key,
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
```

- [ ] **Step 3: `Edit`-Action**

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

- [ ] **Step 4: `AddSpec`-Action**

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddSpec(AssemblyGroupSpecFormModel form)
{
    var grp = await _assemblyGroupRepository.GetByIdAsync(form.AssemblyGroupId);
    if (grp == null) return NotFound("AssemblyGroup fehlt.");

    if (string.IsNullOrWhiteSpace(form.Description))
    {
        TempData["WarningMessage"] = "Beschreibung ist erforderlich.";
        return RedirectToAction(nameof(Edit), new { id = grp.ProductionOrderId, tab = grp.GroupKey });
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
    return RedirectToAction(nameof(Edit), new { id = grp.ProductionOrderId, tab = grp.GroupKey });
}
```

- [ ] **Step 5: `EditSpec`-Action**

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditSpec(AssemblyGroupSpecFormModel form)
{
    var existing = await _specRepository.GetByIdAsync(form.Id);
    if (existing == null) return NotFound();
    var grp = existing.AssemblyGroup;

    if (string.IsNullOrWhiteSpace(form.Description))
    {
        TempData["WarningMessage"] = "Beschreibung ist erforderlich.";
        return RedirectToAction(nameof(Edit), new { id = grp.ProductionOrderId, tab = grp.GroupKey });
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
    return RedirectToAction(nameof(Edit), new { id = grp.ProductionOrderId, tab = grp.GroupKey });
}
```

- [ ] **Step 6: `DeleteSpec`-Action**

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteSpec(int id)
{
    var existing = await _specRepository.GetByIdAsync(id);
    if (existing == null) return NotFound();
    var grp = existing.AssemblyGroup;

    await _specRepository.DeleteAsync(id);
    TempData["SuccessMessage"] = "Auspraegung geloescht.";
    return RedirectToAction(nameof(Edit), new { id = grp.ProductionOrderId, tab = grp.GroupKey });
}
```

- [ ] **Step 7: `ToggleIsCompleted`-Action**

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ToggleIsCompleted(int assemblyGroupId)
{
    var grp = await _assemblyGroupRepository.GetByIdAsync(assemblyGroupId);
    if (grp == null) return NotFound();

    var newValue = !grp.IsCompleted;
    await _assemblyGroupRepository.SetIsCompletedAsync(
        assemblyGroupId,
        newValue,
        _currentUser.GetDisplayName(),
        _currentUser.GetDisplayName(),
        _currentUser.GetWindowsUserName());

    return RedirectToAction(nameof(Edit), new { id = grp.ProductionOrderId, tab = grp.GroupKey });
}
```

- [ ] **Step 8: Build verifizieren**

```pwsh
dotnet build .claude/worktrees/refactor-fa-logic/IdealAkeWms.slnx --nologo
```

Erwartet: gruen. Falls rot:
- `IProductionOrderAssemblyGroupRepository.GetByIdAsync` / `GetByProductionOrderIdsAsync` / `SetIsCompletedAsync` muessen in Task 2 angelegt sein.
- `IProductionOrderAssemblyGroupSpecRepository` muss DI-registriert sein.

- [ ] **Step 9: Commit**

```pwsh
git -C .claude/worktrees/refactor-fa-logic add IdealAkeWms/Controllers/FaCompletionController.cs

git -C .claude/worktrees/refactor-fa-logic commit -m @'
feat(fa-completion): phase 4 task 4 - FaCompletionController with 6 actions

[RequireFaCompletionAccess] class-level. Actions: Index (filterable FA-list
with applicable/completed/spec-count aggregates), Edit/{id}?tab=VK
(server-rendered 5 tabs VK/VL/VE/VT/VA), AddSpec/EditSpec/DeleteSpec POST
with TempData feedback, ToggleIsCompleted POST setting CompletedAt/By via
new SetIsCompletedAsync repo method. IsApplicable-Toggle reuses phase-1
JSON endpoint /api/assembly-groups/toggle-applicable.

Spec section 8. Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 5: Views — Index, Edit, _SpecRow

**Files:**
- New: `IdealAkeWms/Views/FaCompletion/Index.cshtml`
- New: `IdealAkeWms/Views/FaCompletion/Edit.cshtml`
- New: `IdealAkeWms/Views/FaCompletion/_SpecRow.cshtml`

- [ ] **Step 1: `Views/FaCompletion/Index.cshtml`**

Inhalt 1:1 wie Spec 9.1. Pfade beachten:
- Header `<h2 class="page-header">FA-Vervollstaendigung</h2>`
- Filter-Card mit FA-Nr./Artikel/Kunde/ShowDone — identisch zu Slim-Index (Phase 2).
- Tabelle mit `data-view-key="FaCompletion"` und `data-col-key`-Attributen passend zu `ColumnDefinitions.FaCompletion` (Task 3).
- 10 Spalten inkl. `applicable`, `completed`, `spec-count`.
- "Bearbeiten"-Link in der letzten Spalte: `asp-action="Edit" asp-route-id="@item.Id"` (kein Tab-Param, Default ist VK).

Inhalt komplett (Plan-Snapshot fuer Implementierung):

```razor
@model IdealAkeWms.Models.ViewModels.FaCompletionListViewModel
@{
    ViewData["Title"] = "FA-Vervollstaendigung";
}

<h2 class="page-header">FA-Vervollstaendigung</h2>

@if (TempData["SuccessMessage"] != null)
{
    <div class="alert alert-success">@TempData["SuccessMessage"]</div>
}
@if (TempData["WarningMessage"] != null)
{
    <div class="alert alert-warning">@TempData["WarningMessage"]</div>
}

<form method="get" class="card card-body mb-3">
    <div class="row g-2">
        <div class="col-md-3">
            <input class="form-control" name="filterOrderNumber" value="@Model.FilterOrderNumber" placeholder="FA Nr." />
        </div>
        <div class="col-md-3">
            <input class="form-control" name="filterArticleNumber" value="@Model.FilterArticleNumber" placeholder="Artikel" />
        </div>
        <div class="col-md-3">
            <input class="form-control" name="filterCustomer" value="@Model.FilterCustomer" placeholder="Kunde" />
        </div>
        <div class="col-md-2">
            <div class="form-check mt-2">
                <input class="form-check-input" type="checkbox" id="chkShowDone" name="showDone" value="true"
                       @(Model.ShowDone ? "checked" : "") onchange="this.form.submit()" />
                <label class="form-check-label" for="chkShowDone">Erledigte zeigen</label>
            </div>
        </div>
        <div class="col-md-1">
            <button class="btn btn-primary w-100">Filtern</button>
        </div>
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
                    <td>
                        <a asp-action="Edit" asp-route-id="@item.Id" class="btn btn-sm btn-outline-primary">
                            Bearbeiten
                        </a>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>

@section Scripts {
    <script src="~/js/column-preferences.js" asp-append-version="true"></script>
    <script src="~/js/table-filter.js" asp-append-version="true"></script>
}
```

- [ ] **Step 2: `Views/FaCompletion/Edit.cshtml`**

Inhalt analog Spec 9.2 (siehe vollstaendige Vorlage dort). Wichtige Punkte beim Anlegen:

- `@model IdealAkeWms.Models.ViewModels.FaCompletionEditViewModel`
- FA-Header-Card mit Kunde/Artikel/Stk./Bezeichnung/Termine.
- `<ul class="nav nav-tabs mb-3">`-Loop ueber `Model.Tabs`, `asp-action="Edit" asp-route-id="@Model.ProductionOrderId" asp-route-tab="@t.GroupKey"`.
- Tab-Header zeigt Indikator: `(✓ vollst.)` wenn `IsApplicable && IsCompleted`, `(offen)` wenn `IsApplicable && !IsCompleted`.
- `var active = Model.Tabs.First(t => t.GroupKey == Model.ActiveTab);`
- Flags-Bar mit zwei Switches:
  - "Anwendbar" — JSON-AJAX an `/api/assembly-groups/toggle-applicable` (CSRF-Token wird hier nicht benoetigt, weil API-Call kein `[ValidateAntiForgeryToken]` hat — siehe Phase-1-`AssemblyGroupsApiController`).
  - "Vervollstaendigt" — Form-POST an `ToggleIsCompleted`, `assemblyGroupId` als hidden field, Submit per `onchange="this.form.submit()"`.
- Spec-Tabelle mit `_SpecRow`-Partial pro existierender Spec.
- Inline-Add-Form als letzte tbody-Zeile mit allen 5 Feldern + `+`-Button.
- `@section Scripts` mit Select2-Init fuer `.article-select2` und `.article-select2-prefilled` (siehe Spec 9.4 fuer Pre-Filled-Pattern).

Konkrete Razor-Vorlage siehe Spec 9.2. Beim Anlegen pruefen, dass `<form>` ausserhalb von `<tr>`/`<td>` steht und Inputs via `form="frmEditSpec_@spec.Id"` referenzieren (Spec 9.3-Hinweis).

- [ ] **Step 3: `Views/FaCompletion/_SpecRow.cshtml`**

Inhalt 1:1 wie Spec 9.3. Wichtig:

- Model `AssemblyGroupSpecFormModel`.
- `<form>`-Tag fuer EditSpec liegt am `<td>`-Anfang mit `id="frmEditSpec_@Model.Id"`. Folge-Inputs in anderen `<td>`-Zellen referenzieren das Form via `form="..."`-Attribut.
- `<form>` fuer DeleteSpec liegt als zweite Form in der letzten `<td>` mit Confirm-Dialog.
- `data-prefilled-id` / `data-prefilled-text` auf der Select-Box fuer Select2-Vorbelegung (siehe Spec 9.4).
- Antiforgery-Tokens in beiden Forms.

- [ ] **Step 4: Article-Select2-Glue (im Edit.cshtml-`@section Scripts`-Block)**

Zwei Select2-Initialisierungen:

1. `.article-select2` — fresh Add-Form, kein Vorzustand, leerer Select2.
2. `.article-select2-prefilled` — Edit-Rows mit `data-prefilled-id` / `data-prefilled-text` → vor Select2-Init eine Option appenden, dann `select2` aufrufen.

Beide nutzen `/api/articles/search` mit `q`+`limit`-Query. `minimumInputLength: 2`. `allowClear: true`.

Beispiel-Snippet siehe Spec 9.4.

- [ ] **Step 5: Build verifizieren**

```pwsh
dotnet build .claude/worktrees/refactor-fa-logic/IdealAkeWms.slnx --nologo
```

Erwartet: gruen. Razor-Compile-Errors deuten typischerweise auf falsche Property-Referenzen oder fehlende `@using IdealAkeWms.Models.ViewModels` hin (in `_ViewImports.cshtml` global vorhanden — Phase 2 hat das schon erweitert).

- [ ] **Step 6: Manuelle Smoke-Test-Anweisungen**

```text
1. dotnet run --project IdealAkeWms
2. Login als admin
3. https://localhost:7XXX/FaCompletion/Index
4. Wenn Tabelle nicht leer: Bearbeiten klicken
5. Tab-Klick (VK -> VL -> VE)
6. Add-Form: Description "Test" -> +
7. Edit-Row: Description aendern -> Speichern
8. Delete-Row -> Confirm
9. IsApplicable-Toggle: Browser-DevTools-Network-Tab pruefen: POST /api/assembly-groups/toggle-applicable mit 200 OK
10. IsCompleted-Toggle: Form-POST loest Reload aus, Header zeigt User+Timestamp
```

(Nicht automatisierbar — Phase-4-Plan vermerkt diese Schritte als manuelle Verifikation; automatisierte Tests folgen in Task 7.)

- [ ] **Step 7: Commit**

```pwsh
git -C .claude/worktrees/refactor-fa-logic add `
  IdealAkeWms/Views/FaCompletion/Index.cshtml `
  IdealAkeWms/Views/FaCompletion/Edit.cshtml `
  IdealAkeWms/Views/FaCompletion/_SpecRow.cshtml

git -C .claude/worktrees/refactor-fa-logic commit -m @'
feat(fa-completion): phase 4 task 5 - Index, Edit, _SpecRow views

Index: filterable FA-list with applicable/completed/spec-count counters,
data-view-key="FaCompletion" with 10 columns. Edit: FA header card + 5
bootstrap nav-tabs (VK/VL/VE/VT/VA) with URL-state via asp-route-tab;
flags-bar with IsApplicable (JSON-call to phase-1 endpoint) and
IsCompleted (form-POST) switches; spec table with inline edit/delete
rows; inline add-form as last tbody row. _SpecRow partial uses HTML5
form="..."-attribute to span one EditSpec form across multiple TDs.
Select2 reuses /api/articles/search (no new endpoint).

Spec sections 9.1, 9.2, 9.3, 9.4. Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 6: Nav-Bar-Eintrag

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: `canCompleteFa` Variable-Berechnung**

Im `_Layout.cshtml` im `@{ … }`-Block (heutige Zeilen ~30-42, vor dem Nav-Bar-Rendering) ergaenzen:

```razor
var canCompleteFa = await CurrentUserService.CanCompleteFaAsync();
```

- [ ] **Step 2: Nav-Bar-Eintrag einfuegen**

Direkt nach dem Phase-2-Block "Kommissionierung-Dropdown" (heutige Zeile ~95) und vor dem `Bestellungen`-Block:

```razor
@* Phase 4: FA-Vervollstaendigung *@
@if (canCompleteFa)
{
    <li class="nav-item">
        <a class="nav-link" asp-controller="FaCompletion" asp-action="Index">FA-Vervollstaendigung</a>
    </li>
}
```

- [ ] **Step 3: Build + manuelles Smoke**

```pwsh
dotnet build .claude/worktrees/refactor-fa-logic/IdealAkeWms.slnx --nologo
```

Erwartet: gruen.

Smoke (manuell, App-Start):
- Login als admin → "FA-Vervollstaendigung"-Eintrag sichtbar.
- Login als User mit nur `picking`-Rolle → Eintrag NICHT sichtbar.
- Login als User mit `fa_completion`-Rolle → Eintrag sichtbar.

- [ ] **Step 4: Commit**

```pwsh
git -C .claude/worktrees/refactor-fa-logic add IdealAkeWms/Views/Shared/_Layout.cshtml

git -C .claude/worktrees/refactor-fa-logic commit -m @'
feat(navbar): phase 4 task 6 - FA-Vervollstaendigung menu entry

New top-level nav item between Kommissionierung and Bestellungen,
visible only when CurrentUserService.CanCompleteFaAsync() returns true
(admin OR fa_completion role).

Spec section 9.6. Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 7: Tests

**Files:**
- New: `IdealAkeWms.Tests/Controllers/FaCompletionControllerTests.cs`
- New: `IdealAkeWms.Tests/Repositories/ProductionOrderAssemblyGroupSpecRepositoryTests.cs`
- New: `IdealAkeWms.Tests/Filters/RequireFaCompletionAccessFilterTests.cs`
- New: `IdealAkeWms.Tests/Filters/RequirePickingOrFaCompletionAccessFilterTests.cs`
- Modify: `IdealAkeWms.Tests/Controllers/AssemblyGroupsApiControllerTests.cs` (1 zusaetzlicher Test)

Test-Pattern folgt Phase-1-Vorbild (`TestApplicationDbContext` + `TestDbContextFactory.Create()`). Siehe CLAUDE.md "Test-Setup".

- [ ] **Step 1: `FaCompletionControllerTests.cs`**

16 Tests laut Spec 12.1. Wichtige Setup-Logik:

```csharp
private (TestApplicationDbContext ctx, FaCompletionController ctrl) Build()
{
    var ctx = TestDbContextFactory.Create();
    var prodRepo = new ProductionOrderRepository(ctx);
    var grpRepo = new ProductionOrderAssemblyGroupRepository(ctx);
    var specRepo = new ProductionOrderAssemblyGroupSpecRepository(ctx);
    var user = MockCurrentUser("max.mustermann", "MAX");
    var ctrl = new FaCompletionController(prodRepo, grpRepo, specRepo, user);
    return (ctx, ctrl);
}

private static ProductionOrder SeedOrderWithGroups(TestApplicationDbContext ctx, string orderNumber, bool isDone = false)
{
    var order = new ProductionOrder
    {
        OrderNumber = orderNumber, Quantity = 1, IsDone = isDone,
        CreatedAt = DateTime.Now, CreatedBy = "test", CreatedByWindows = "test"
    };
    ctx.ProductionOrders.Add(order); ctx.SaveChanges();
    foreach (var key in new[] { "VK", "VL", "VE", "VT", "VA" })
    {
        ctx.ProductionOrderAssemblyGroups.Add(new ProductionOrderAssemblyGroup
        {
            ProductionOrderId = order.Id, GroupKey = key, IsApplicable = false, IsCompleted = false,
            CreatedAt = DateTime.Now, CreatedBy = "test", CreatedByWindows = "test"
        });
    }
    ctx.SaveChanges();
    return order;
}
```

Test-Auswahl (Beispiele):

```csharp
[Fact]
public async Task Index_FiltersByOrderNumber_ReturnsOnlyMatching()
{
    var (ctx, ctrl) = Build();
    SeedOrderWithGroups(ctx, "FA-001");
    SeedOrderWithGroups(ctx, "FA-002");
    SeedOrderWithGroups(ctx, "WA-003");

    var result = await ctrl.Index("FA", null, null, false) as ViewResult;
    var vm = result!.Model as FaCompletionListViewModel;

    vm!.Items.Should().HaveCount(2);
    vm.Items.Select(i => i.OrderNumber).Should().BeEquivalentTo(["FA-001", "FA-002"]);
}

[Fact]
public async Task Edit_TabParam_SelectsCorrectActiveTab()
{
    var (ctx, ctrl) = Build();
    var o = SeedOrderWithGroups(ctx, "FA-100");

    var result = await ctrl.Edit(o.Id, "VL") as ViewResult;
    var vm = result!.Model as FaCompletionEditViewModel;

    vm!.ActiveTab.Should().Be("VL");
    vm.Tabs.Should().HaveCount(5);
    vm.Tabs.Select(t => t.GroupKey).Should().BeEquivalentTo(["VK", "VL", "VE", "VT", "VA"]);
}

[Fact]
public async Task AddSpec_HappyPath_PersistsSpec_WithAuditFields()
{
    var (ctx, ctrl) = Build();
    var o = SeedOrderWithGroups(ctx, "FA-200");
    var vlGroup = ctx.ProductionOrderAssemblyGroups.First(g => g.ProductionOrderId == o.Id && g.GroupKey == "VL");

    var form = new AssemblyGroupSpecFormModel
    {
        AssemblyGroupId = vlGroup.Id, Description = "Lueftermotor", Quantity = 2.000m, SortOrder = 10
    };
    var result = await ctrl.AddSpec(form) as RedirectToActionResult;

    result!.ActionName.Should().Be("Edit");
    result.RouteValues!["tab"].Should().Be("VL");

    var spec = ctx.ProductionOrderAssemblyGroupSpecs.Single();
    spec.Description.Should().Be("Lueftermotor");
    spec.CreatedBy.Should().NotBeNullOrEmpty();
}

[Fact]
public async Task ToggleIsCompleted_True_SetsCompletedAtAndBy()
{
    var (ctx, ctrl) = Build();
    var o = SeedOrderWithGroups(ctx, "FA-300");
    var vkGroup = ctx.ProductionOrderAssemblyGroups.First(g => g.ProductionOrderId == o.Id && g.GroupKey == "VK");

    var result = await ctrl.ToggleIsCompleted(vkGroup.Id) as RedirectToActionResult;
    result!.ActionName.Should().Be("Edit");

    ctx.ChangeTracker.Clear();
    var reloaded = ctx.ProductionOrderAssemblyGroups.Find(vkGroup.Id)!;
    reloaded.IsCompleted.Should().BeTrue();
    reloaded.CompletedAt.Should().NotBeNull();
    reloaded.CompletedBy.Should().NotBeNullOrEmpty();
}
```

Restliche 12 Tests analog (siehe Spec 12.1 fuer komplette Liste).

- [ ] **Step 2: `ProductionOrderAssemblyGroupSpecRepositoryTests.cs`**

7 Tests laut Spec 12.2. Beispiel:

```csharp
[Fact]
public async Task GetByAssemblyGroupIdAsync_SortedBySortOrderThenId()
{
    using var ctx = TestDbContextFactory.Create();
    var group = SeedGroup(ctx);  // helper, ProductionOrder + AssemblyGroup
    ctx.ProductionOrderAssemblyGroupSpecs.AddRange(
        new ProductionOrderAssemblyGroupSpec { AssemblyGroupId = group.Id, Description = "B", SortOrder = 20, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
        new ProductionOrderAssemblyGroupSpec { AssemblyGroupId = group.Id, Description = "A", SortOrder = 10, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
        new ProductionOrderAssemblyGroupSpec { AssemblyGroupId = group.Id, Description = "C", SortOrder = 10, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
    await ctx.SaveChangesAsync();

    var repo = new ProductionOrderAssemblyGroupSpecRepository(ctx);
    var result = await repo.GetByAssemblyGroupIdAsync(group.Id);

    result.Select(s => s.Description).Should().Equal("A", "C", "B");
}
```

- [ ] **Step 3: Filter-Tests (zwei Klassen)**

`RequireFaCompletionAccessFilterTests.cs` — 4 Tests (Admin, FaCompletion, PickerOnly, Anonymous).
`RequirePickingOrFaCompletionAccessFilterTests.cs` — 4 Tests (Picker, FaCompletion, Tracker, Admin).

Pattern analog Phase-2-`RequirePickingOrLeitstandAccessFilterTests.cs` — Mock `ICurrentUserService`, ActionExecutionContext builder, Assert auf `RedirectToActionResult` mit ActionName="AccessDenied" oder Pass-through.

- [ ] **Step 4: Phase-2-Regression: 1 Test in `AssemblyGroupsApiControllerTests`**

In `IdealAkeWms.Tests/Controllers/AssemblyGroupsApiControllerTests.cs` einen Test ergaenzen, der verifiziert dass der API-Endpoint nach dem Filter-Wechsel weiterhin fuer Picker funktioniert:

```csharp
[Fact]
public async Task ToggleApplicable_PickerStill_Passes()
{
    using var ctx = TestDbContextFactory.Create();
    var o = SeedOrderWithVkGroup(ctx);  // Helper
    var pickingUser = MockCurrentUser(canPick: true, canCompleteFa: false);
    var groups = new ProductionOrderAssemblyGroupRepository(ctx);
    var prods = new ProductionOrderRepository(ctx);
    var ctrl = new AssemblyGroupsApiController(groups, prods, pickingUser);

    var result = await ctrl.ToggleApplicable(new AssemblyGroupToggleRequest
    {
        ProductionOrderId = o.Id, GroupKey = "VK", Value = true
    });

    result.Should().BeOfType<OkResult>();
}
```

Note: dies testet die Controller-Logik, nicht den Filter (Filter ist Endpoint-Schutz, der nicht im Action-Test mit MockUser direkt aufgerufen wird). Der Filter ist durch Task 7 Step 3 abgedeckt.

- [ ] **Step 5: Test-Run**

```pwsh
dotnet test .claude/worktrees/refactor-fa-logic/IdealAkeWms.slnx --nologo --filter "Category!=SqlServerOnly"
```

Erwartet: alle neuen Tests gruen. Falls rot:
- `TestApplicationDbContext`-Seed muss alle 5 GroupKeys anlegen, sonst schlaegt `Edit_ValidId_ReturnsAllFiveTabs` fehl.
- InMemory-DB-Behavior: `Include()`-Calls funktionieren, `OnDelete.SetNull` wird nicht enforced — Test-Setup darf nicht darauf bauen.

- [ ] **Step 6: Commit**

```pwsh
git -C .claude/worktrees/refactor-fa-logic add `
  IdealAkeWms.Tests/Controllers/FaCompletionControllerTests.cs `
  IdealAkeWms.Tests/Repositories/ProductionOrderAssemblyGroupSpecRepositoryTests.cs `
  IdealAkeWms.Tests/Filters/RequireFaCompletionAccessFilterTests.cs `
  IdealAkeWms.Tests/Filters/RequirePickingOrFaCompletionAccessFilterTests.cs `
  IdealAkeWms.Tests/Controllers/AssemblyGroupsApiControllerTests.cs

git -C .claude/worktrees/refactor-fa-logic commit -m @'
test(fa-completion): phase 4 task 7 - controller, repo, filter tests

FaCompletionControllerTests (16 tests): Index filtering, Edit tab routing,
AddSpec/EditSpec/DeleteSpec happy and not-found, ToggleIsCompleted audit
fields. AssemblyGroupSpecRepositoryTests (7 tests): CRUD + sorted bulk
lookup + chunking happy-path. Filter tests (8 total) for the two new
filters. One regression test in AssemblyGroupsApiControllerTests verifies
that picker-only users still toggle IsApplicable after the filter swap.

Spec section 12. Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 8: AppVersion, Changelog, Help, CLAUDE.md, TESTSZENARIEN

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`
- Modify: `IDEALAKEWMSService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`
- Modify: `CLAUDE.md`
- Modify: `docs/TESTSZENARIEN.md`

- [ ] **Step 1: AppVersion bumpen**

`IdealAkeWms/AppVersion.cs`:

```csharp
namespace IdealAkeWms;

public static class AppVersion
{
    public const string Version = "1.13.0";
    public const string Date = "2026-05-15";
}
```

`IDEALAKEWMSService/AppVersion.cs` analog (gleiche Werte).

- [ ] **Step 2: `Changelog.cshtml` — neue Card oben**

In `IdealAkeWms/Views/Help/Changelog.cshtml` ganz oben (vor der bestehenden v1.12.0-Card) einfuegen:

```razor
<div class="card mb-3">
    <div class="card-header text-white" style="background-color: var(--ake-primary);">
        <strong>v1.13.0</strong> <span class="text-white-50 ms-2">15.05.2026</span>
    </div>
    <div class="card-body">
        <h6>FA-Vervollstaendigung (Phase 4)</h6>
        <ul>
            <li><strong>Neues Modul "FA-Vervollstaendigung":</strong> Pro Fertigungsauftrag koennen jetzt
                Merkmalsauspraegungen pro Vormontageplatz (VK Kuehlung, VL Lueftung, VE Elektro, VT Tueren,
                VA Aufbau) gepflegt werden. Eigene FA-Liste mit Filter, Detail-Seite mit Bootstrap-Tabs pro
                Gruppe, inline Add/Edit/Delete der Auspraegungen.</li>
            <li><strong>Neue Rolle <code>fa_completion</code>:</strong> Gibt Schreibzugriff auf die
                Merkmalsauspraegungen plus die zwei Toggle-Flags <code>IsApplicable</code> (Gruppe
                anwendbar?) und <code>IsCompleted</code> (Pflege abgeschlossen?) pro AssemblyGroup.</li>
            <li><strong>Toggle-API-Permission erweitert:</strong> Der Endpoint
                <code>POST /api/assembly-groups/toggle-applicable</code> nutzt jetzt
                <code>RequirePickingOrFaCompletionAccess</code> statt <code>RequirePickingAccess</code> —
                sowohl Picker (Phase 2 PickingLeitstand) als auch FA-Completion-User koennen togglen.</li>
            <li><strong>Artikel-Suche:</strong> Bestehender Endpoint <code>/api/articles/search</code>
                wird via Select2 in der Add/Edit-Form genutzt.</li>
        </ul>
    </div>
</div>
```

- [ ] **Step 3: `Help/Index.cshtml` — neue Section**

In `IdealAkeWms/Views/Help/Index.cshtml` an passender Stelle (nach "Kommissionier-Leitstand"-Block, vor "BDE") eine neue Section ergaenzen:

```razor
<section class="mb-4">
    <h4>FA-Vervollstaendigung</h4>
    <p>
        Pro Fertigungsauftrag koennen Merkmalsauspraegungen pro Vormontageplatz gepflegt werden.
        Die Detail-Seite zeigt fuenf Tabs (VK Kuehlung, VL Lueftung, VE Elektro, VT Tueren, VA Aufbau).
        Pro Tab kann angegeben werden, ob die Gruppe fuer den FA anwendbar ist (<code>IsApplicable</code>)
        und ob die Pflege abgeschlossen ist (<code>IsCompleted</code>). Innerhalb des Tabs werden die
        konkreten Auspraegungen (Artikel + Beschreibung + Menge + Notizen + Sortierung) ueber
        Inline-Forms gepflegt. Sichtbar fuer Admins und User mit Rolle <code>fa_completion</code>.
    </p>
</section>
```

- [ ] **Step 4: `CLAUDE.md` — Rolle, Filter, Fallstrick**

In `CLAUDE.md` (Repo-Root) drei Bereiche aktualisieren:

**Block "Zugriffsschutz"** — drei Anpassungen:
- `[RequirePickingAccess]` "Angewendet auf"-Spalte: `AssemblyGroupsApiController` entfernen.
- Neuer Eintrag `[RequireFaCompletionAccess]` mit Rolen `admin, fa_completion`, angewendet auf `FaCompletionController`.
- Neuer Eintrag `[RequirePickingOrFaCompletionAccess]` mit Rollen `admin, picking ODER fa_completion`, angewendet auf `AssemblyGroupsApiController`.

**Block "Rollenkonzept"** — neue Zeile in der Rollen-Tabelle:

```markdown
| `fa_completion` | FA-Vervollstaendigung: Merkmalsauspraegungen pro Vormontageplatz pflegen |
```

**Block "Bekannte Fallstricke"** — neuer Eintrag:

```markdown
- **FA-Vervollstaendigung-Toggle vs PickingLeitstand-Toggle (seit v1.13.0)**: Beide Views (`PickingLeitstand/Index` Phase 2, `FaCompletion/Edit` Phase 4) toggeln `IsApplicable` ueber denselben JSON-Endpoint `POST /api/assembly-groups/toggle-applicable`. Permission ist `RequirePickingOrFaCompletionAccess` (admin ODER picking ODER fa_completion). Bei Aenderungen am Endpoint oder am JS-Dispatcher muessen beide Views getestet werden — sie teilen die API-Schnittstelle, haben aber unterschiedliche JS-Wrappers.
```

- [ ] **Step 5: `docs/TESTSZENARIEN.md` — TS-12-Block**

Am Ende des Dokuments einen neuen TS-12-Block "FA-Vervollstaendigung" anhaengen, der die 6 Szenarien aus Spec Sektion 14 enthaelt (TS-12.1 bis TS-12.6).

Format strikt analog zu den existierenden TS-Bloecken: **Vorbedingungen** / **Schritte** / **Erwartet** pro Szenario.

- [ ] **Step 6: Build verifizieren**

```pwsh
dotnet build .claude/worktrees/refactor-fa-logic/IdealAkeWms.slnx --nologo
```

Erwartet: gruen.

- [ ] **Step 7: Commit**

```pwsh
git -C .claude/worktrees/refactor-fa-logic add `
  IdealAkeWms/AppVersion.cs `
  IDEALAKEWMSService/AppVersion.cs `
  IdealAkeWms/Views/Help/Changelog.cshtml `
  IdealAkeWms/Views/Help/Index.cshtml `
  CLAUDE.md `
  docs/TESTSZENARIEN.md

git -C .claude/worktrees/refactor-fa-logic commit -m @'
docs(fa-completion): phase 4 task 8 - v1.13.0 changelog + help + claude.md + testszenarien

AppVersion 1.12.0 -> 1.13.0 (web + service). Changelog card with four
bullets covering the new module, the fa_completion role, the
toggle-applicable permission change, and the article-search reuse. Help
section "FA-Vervollstaendigung" describing tabs + workflow. CLAUDE.md
gets the new role + two new filter entries + new pitfall about shared
toggle endpoint between phase 2 and phase 4 views. TESTSZENARIEN.md adds
the TS-12 block with six scenarios (index filter, edit tabs, spec CRUD,
IsApplicable toggle interplay with PickingLeitstand, IsCompleted audit,
permission boundary).

Spec section 15. Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 9: Manuelle Verifikation (non-executable)

**Files:** keine — Lauf gegen die laufende App.

Diese Schritte sind **vor Merge in main** durchzufuehren. Sie ersetzen nicht die TESTSZENARIEN.md, sondern dienen als Plan-interner Smoke-Lauf.

- [ ] **Step 1: Build + Tests**

```pwsh
dotnet build .claude/worktrees/refactor-fa-logic/IdealAkeWms.slnx --nologo
dotnet test  .claude/worktrees/refactor-fa-logic/IdealAkeWms.slnx --nologo --no-build --filter "Category!=SqlServerOnly"
```

Erwartet: alles gruen.

- [ ] **Step 2: App starten + Seed-Verifikation**

```pwsh
dotnet run --project .claude/worktrees/refactor-fa-logic/IdealAkeWms
```

In SSMS oder via App-Settings-Page:

```sql
SELECT [Key], Name, SortOrder FROM dbo.Roles WHERE [Key] = 'fa_completion';
```

Erwartet: 1 Zeile mit `Name='FA-Vervollstaendigung'`, `SortOrder=80`.

- [ ] **Step 3: Test-User anlegen + Login**

In `/Users/Index` (als admin): neuen User anlegen, Rolle `fa_completion` zuweisen. Logout, Login mit Test-User.

Verifizieren:
- Nav-Bar zeigt **nur** "FA-Vervollstaendigung" + "Hilfe" (keine Lager-, Kommissionierungs-, Tracking-Eintraege).
- "FA-Vervollstaendigung" anklicken → Liste laedt.

- [ ] **Step 4: Edit-Page Tab-Wechsel + Spec-CRUD**

Beliebige FA mit `Bearbeiten`-Button oeffnen. Tab "VL" anklicken.

- Inline-Add-Form: Description "Test-Lueftermotor", Quantity 1.000, "+"-Button.
- Erwartet: Erfolgsmeldung, Zeile in Tabelle, `SpecCount` in Index +1.

Edit-Row: Description aendern, "Speichern".
- Erwartet: Erfolgsmeldung, neue Description sichtbar nach Reload.

Delete-Row: "Loeschen" → Confirm.
- Erwartet: Erfolgsmeldung, Zeile weg.

- [ ] **Step 5: IsApplicable-Toggle + PickingLeitstand-Cross-Check**

Edit-Page Tab VL: Checkbox "Anwendbar" anhaken.

DevTools-Network: `POST /api/assembly-groups/toggle-applicable` mit 200 OK.

DB:
```sql
SELECT * FROM dbo.ProductionOrderAssemblyGroups
WHERE ProductionOrderId = <id> AND GroupKey = 'VL';
```
Erwartet: `IsApplicable=1`.

Wechsel zu `/PickingLeitstand/Index` (mit User, der zusaetzlich `picking`-Rolle hat). VL-Spalte fuer denselben FA zeigt Checkbox als angehakt. ↔ Wert ist konsistent.

- [ ] **Step 6: IsCompleted-Toggle + Audit-Felder**

Edit-Page: Checkbox "Vervollstaendigt" anhaken. Form-POST loest Reload aus.

Erwartet:
- Label zeigt User + Timestamp.
- DB: `IsCompleted=1`, `CompletedAt=<now>`, `CompletedBy=<User>`.

Erneut anklicken: alle drei Felder zuruckgesetzt.

- [ ] **Step 7: Permission-Boundary**

Logout, Login als reiner `picking`-User (kein `fa_completion`).
- Nav-Bar: "FA-Vervollstaendigung"-Eintrag NICHT sichtbar.
- Direkter URL-Aufruf `/FaCompletion/Index` → Redirect zu `/Account/AccessDenied`.
- VK-Toggle auf `/PickingLeitstand/Index` funktioniert (Picker-Permission greift weiter).

- [ ] **Step 8: Tracker-Permission-Boundary**

Logout, Login als reiner `tracking`-User.
- "FA-Vervollstaendigung"-Eintrag NICHT sichtbar.
- API-Call `POST /api/assembly-groups/toggle-applicable` direkt via curl/DevTools → 302 zu AccessDenied (Filter greift).

- [ ] **Step 9: Smoke-Test mit Article-Select2**

Edit-Page Tab VL, Add-Form: Select2 fokussieren, "100" eintippen.
- Erwartet: Liste mit Artikeln, Format "100023 - Beschreibung".
- Auswaehlen → ArticleId im hidden field, Description-Feld kann manuell ergaenzt werden.
- "+"-Button → Spec mit ArticleId persistiert.
- Edit-Row: Article-Select2 ist vorbefuellt mit dem ArticleNumber-Description-Text.

- [ ] **Step 10: Approval-Gate**

Wenn alle Schritte 1-9 gruen sind:
- PR auf `refactor/fa-logic` → `main` (oder direkt-Merge falls Squash gewuenscht).
- Vor PR den Plan-Commit-Block lesen, fuer User-Review verfuegbar.

---

## Self-Review — Spec-Sektion → Plan-Task-Mapping

| Spec-Sektion | Inhalt | Plan-Task |
|---|---|---|
| 5.1 — Neue Rolle `fa_completion` | RoleKeys.cs Konstante + Program.cs Seed | **Task 1, Steps 1-2** |
| 5.2 — `RequireFaCompletionAccess`-Filter + `CanCompleteFaAsync` | Filter-Klasse + Service-Method | **Task 1, Steps 3-4** |
| 5.3 — `RequirePickingOrFaCompletionAccess` + API-Permission-Wechsel | Zweiter Filter + AssemblyGroupsApiController-Update | **Task 1, Steps 5-6** |
| 5.4 — Berechtigungsmatrix | Doku, kein Code | abgedeckt durch Task 1 + Task 8 (CLAUDE.md) |
| 6 — `IProductionOrderAssemblyGroupSpecRepository` + Impl + DI | Neues Repo | **Task 2, Steps 1-3** |
| 6 Annex — Repo-Erweiterungen (GetById, GetByProductionOrderIds, SetIsCompletedAsync) | Drei neue Methoden | **Task 2, Steps 4-5** |
| 7 — ViewModels (List + Edit + Tab + SpecForm) | FaCompletionViewModels.cs | **Task 3, Step 1** |
| 8.1 — Controller-Skelett + DI + statische Tabellen | Klassen-Setup | **Task 4, Step 1** |
| 8.2 — Index-Action | FA-Liste mit Bulk-Counts | **Task 4, Step 2** |
| 8.3 — Edit-Action | 5-Tab-Aufbau | **Task 4, Step 3** |
| 8.4 — AddSpec | POST-Action | **Task 4, Step 4** |
| 8.5 — EditSpec | POST-Action | **Task 4, Step 5** |
| 8.6 — DeleteSpec | POST-Action | **Task 4, Step 6** |
| 8.7 — ToggleIsCompleted | POST-Action mit Audit | **Task 4, Step 7** |
| 8.8 — KEIN eigener IsApplicable-Endpoint | Reuse Phase-1-API | abgedeckt durch Task 1 Step 6 (Filter-Wechsel) |
| 9.1 — Views/FaCompletion/Index.cshtml | FA-Liste-View | **Task 5, Step 1** |
| 9.2 — Views/FaCompletion/Edit.cshtml | Tabs-View | **Task 5, Step 2** |
| 9.3 — Views/FaCompletion/_SpecRow.cshtml | Spec-Row-Partial | **Task 5, Step 3** |
| 9.4 — Article-Select2-Reuse + Pre-Filled | JS-Snippet im Edit.cshtml | **Task 5, Step 4** |
| 9.5 — Inline-Add (Entscheidung) | Spec-Doku | folgt aus Task 5, kein eigener Step |
| 9.6 — Nav-Bar-Entry | _Layout.cshtml | **Task 6** |
| 10 — API-Permission-Update | `[RequirePickingOrFaCompletionAccess]` auf AssemblyGroupsApiController | **Task 1, Step 6** |
| 11 — ColumnDefinitions-Erweiterung | `FaCompletion`-ViewConfig | **Task 3, Step 3** |
| 12.1 — Controller-Tests (16) | FaCompletionControllerTests.cs | **Task 7, Step 1** |
| 12.2 — Repo-Tests (7) | AssemblyGroupSpecRepositoryTests.cs | **Task 7, Step 2** |
| 12.3 — Filter-Tests (8 in 2 Klassen) | Filter-Test-Dateien | **Task 7, Step 3** |
| 12.4 — Phase-2-Regression-Test (1) | Erweiterung in AssemblyGroupsApiControllerTests | **Task 7, Step 4** |
| 14 — TESTSZENARIEN (6 Szenarien TS-12.1-12.6) | TESTSZENARIEN.md | **Task 8, Step 5** |
| 15 — Versionierung + Doku | AppVersion + Changelog + Help + CLAUDE.md | **Task 8, Steps 1-4** |
| 17 — Offene Entscheidungen | 8 `**Open:**`-Punkte mit Recommended defaults | werden im Plan im Verlauf folgenden Defaults: Reuse Article-Endpoint, Description manuell editierbar (nicht autofill), Inline-Add, alle fa_completion-User duerfen togglen, Filter erweitern (kein eigener Endpoint), keine Pagination innerhalb Tab, kein Article-Group-Filter, Nav immer sichtbar |

**Task-zu-Spec-Coverage-Verifikation:**

- Task 0 (Pre-Conditions) hat keine Spec-Korrespondenz (organisatorisch).
- Task 1 deckt Spec 5.1, 5.2, 5.3, 10 ab.
- Task 2 deckt Spec 6 (Haupt + Annex) ab.
- Task 3 deckt Spec 7 und Spec 11 ab.
- Task 4 deckt Spec 8.1-8.7 ab (8.8 ist no-code, abgedeckt durch Task 1).
- Task 5 deckt Spec 9.1-9.4 ab (9.5 = Entscheidung, in 9.2 enthalten; 9.6 = Task 6).
- Task 6 deckt Spec 9.6 ab.
- Task 7 deckt Spec 12.1-12.4 ab.
- Task 8 deckt Spec 15 (+ TESTSZENARIEN Sektion 14) ab.
- Task 9 ist nicht-executable manuelle Verifikation, deckt Spec 13 (Risiken) durch Smoke ab.

Alle Spec-Sektionen mit Code-Output haben mindestens einen Plan-Task. Sektionen 3 (Out-of-Scope), 13 (Risiken), 16 (Code-Punkte), 17 (Offene Entscheidungen) sind reine Doku.

---

**Hinweis:** Nach erfolgreichem Phase-4-Deploy (mind. 5 Tage Live-Verifikation) wird Phase 5 (`Workstation/Specs` Read-Only-View) als eigene Detail-Spec geschrieben. Phase 5 liest die in Phase 4 gepflegten Specs read-only und filtert nach `ProductionWorkplaceAssemblyGroups` (Junction-Tabelle, Phase 1 eager-create).

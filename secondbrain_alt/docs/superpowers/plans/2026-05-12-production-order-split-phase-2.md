# ProductionOrder-Split — Phase 2 Leitstand-Kommissionierung-View extrahieren — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split heutiges `ProductionOrders/Index` (23 Spalten, 3-Persona) in zwei Views:
- `ProductionOrders/Index` — slim FA-Übersicht (Sage-Master + Workplace + IsDone) für Picker/Tracker/Leitstand.
- `PickingLeitstand/Index` — rich Kommissionier-Leitstand mit allen Status-Pivot-Spalten + Bulk-Release + Picker-Assign.

Phase 2 ist reine Code-/View-Reorganisation: keine Schema-Änderung, keine Migration, kein SQL-Skript. Die Phase-1-Repos (`IProductionOrderPickingStatusRepository`, `IProductionOrderAssemblyGroupRepository.GetIsApplicablePivotAsync`) werden weiterverwendet — sie zogen mit Phase 1 ein.

**Spec:** `docs/superpowers/specs/2026-05-12-production-order-split-phase-2-design.md`.

**Roadmap:** `docs/superpowers/specs/2026-05-12-production-order-split-roadmap.md`, Sektion 6.

**Phase-1-Referenz:** `docs/superpowers/specs/2026-05-12-production-order-split-phase-1-design.md` (Schema), `docs/superpowers/plans/2026-05-12-production-order-split-phase-1.md` (Plan).

**Branch:** `refactor/fa-logic` als Fortsetzung nach Phase-1-Merge in `main`, **oder** neuer Sibling-Branch `refactor/production-order-split-phase-2` (Entscheidung beim Phase-2-Start). Docs-Home bleibt `refactor/production-order-split` Worktree.

**AppVersion:** `1.11.0` (Phase 1) → `1.12.0`, Datum analog Tag-des-Phase-2-Cutovers.

**Commit-Konvention:** `refactor(productionorders): ...` / `feat(productionorders): ...` / `feat(picking): ...` / `docs: ...`. Co-Authored-By trailer im HEREDOC.

**Architecture (4 Schichten + Doku):**
- **ViewModels + ColumnDefinitions** — neuer `ProductionOrderListItem` (slim), neuer `PickingLeitstandItem` (rich), zwei neue `ViewConfig`-Einträge.
- **Filters** — zwei neue Permission-Filter (`RequirePickingOrTrackingOrLeitstandAccess`, `RequirePickingOrLeitstandAccess`).
- **Controller** — neuer `PickingLeitstandController` (5 Actions, migrate from ProductionOrdersController), `ProductionOrdersController` slim.
- **Views** — neue `Views/PickingLeitstand/Index.cshtml` (rich, von alter geforkt), `Views/ProductionOrders/Index.cshtml` slim umgeschrieben.
- **Nav-Bar + Doku** — `_Layout.cshtml`, `CLAUDE.md`, `Changelog.cshtml`, `Help/Index.cshtml`, `TESTSZENARIEN.md`, `AppVersion.cs`.

**Critical sequencing constraints:**
1. Phase 1 MUSS in `main` gemerged sein bevor Phase 2 startet. Verify: `git log main --oneline | rg "production-order-split"` zeigt Phase-1-Merge.
2. Tasks 1-3 sind additiv (neue Files, kein Drop) → Build bleibt grün durchgehend. Task 4 (Slim-View) ist der erste Punkt, ab dem die alte `ProductionOrderViewModel.cs` weg fällt — Tasks 1-3 müssen davor vollständig.
3. Task 5 (Slim-Controller-Body) erfordert dass Task 2 (PickingLeitstandController) bereits existiert — sonst sind die migrierten Actions weg, bevor sie ankommen.
4. Task 7 (Filter) MUSS vor Task 5 abgeschlossen sein, weil der Slim-Controller das neue `[RequirePickingOrTrackingOrLeitstandAccess]` referenziert.

**Files (Gesamtübersicht):**

**New:**
- `IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs` (slim ViewModel + Item)
- `IdealAkeWms/Models/ViewModels/PickingLeitstandViewModel.cs` (rich ViewModel + Item)
- `IdealAkeWms/Filters/RequirePickingOrTrackingOrLeitstandAccessAttribute.cs`
- `IdealAkeWms/Filters/RequirePickingOrLeitstandAccessAttribute.cs`
- `IdealAkeWms/Controllers/PickingLeitstandController.cs`
- `IdealAkeWms/Views/PickingLeitstand/Index.cshtml` (forked from old ProductionOrders/Index)
- `IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs`
- `IdealAkeWms.Tests/Controllers/ProductionOrdersControllerSlimTests.cs`
- `IdealAkeWms.Tests/Filters/RequirePickingOrTrackingOrLeitstandAccessFilterTests.cs`
- `IdealAkeWms.Tests/Filters/RequirePickingOrLeitstandAccessFilterTests.cs`

**Modify:**
- `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` (`ProductionOrders` schrumpft, neuer `PickingLeitstand` dazu)
- `IdealAkeWms/Controllers/ProductionOrdersController.cs` (slim: nur `Index` + Redirects + optionale Compat-Stubs)
- `IdealAkeWms/Views/ProductionOrders/Index.cshtml` (slim umgeschrieben)
- `IdealAkeWms/Views/Shared/_Layout.cshtml` (Nav-Bar)
- `IdealAkeWms/AppVersion.cs` → `1.12.0`
- `IDEALAKEWMSService/AppVersion.cs` → `1.12.0`
- `IdealAkeWms/Views/Help/Changelog.cshtml` (v1.12.0 Card)
- `IdealAkeWms/Views/Help/Index.cshtml` (Hinweis-Section)
- `CLAUDE.md` (Berechtigungstabelle + Fallstrick)
- `docs/TESTSZENARIEN.md` (6 neue Szenarien)

**Delete:**
- `IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs` (ersetzt durch zwei neue Files)
- `IdealAkeWms.Tests/Controllers/ProductionOrdersControllerPickerTests.cs` (Inhalt wandert nach `PickingLeitstandControllerTests`)

---

## Task 0: Pre-Conditions verifizieren

**Files:** keine — reiner Read-Only-Check.

- [ ] **Step 1: Phase 1 in main verifizieren**

```pwsh
git fetch origin
git log origin/main --oneline -50 | rg -i "production-order-split|fa-logic|phase 1"
```

Erwartet: mindestens eine Zeile mit "phase 1" oder "production-order-split" als Merge-Commit auf main. Falls leer → Phase 2 nicht starten, Phase 1 zuerst mergen.

- [ ] **Step 2: Branch-Ausgangspunkt prüfen**

```pwsh
git checkout main
git pull
git checkout -b refactor/production-order-split-phase-2  # ODER: bestehender refactor/fa-logic nach Phase-1-Merge
```

Entscheidung: neuer Branch wenn Phase 1 bereits in main, sonst Fortsetzung auf `refactor/fa-logic`. Plan setzt voraus, dass die Branch-Spitze Phase-1-Code enthält (siehe Pre-Condition oben).

- [ ] **Step 3: Build + Tests grün**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build --filter "Category!=SqlServerOnly"
```

Erwartet: alles grün. Falls rot → Phase-1-Hotfixes zuerst.

- [ ] **Step 4: Bestand der zu modifizierenden Files dokumentieren**

```pwsh
rg -c "ProductionOrderViewItem|ProductionOrderViewModel" IdealAkeWms/ IdealAkeWms.Tests/
```

Notieren: heutige Konsumenten. Erwartung nach Phase 1: ~5-10 Stellen (Controller, View, ViewModels, Tests). Task 5/8 müssen alle auf die neuen Records umgestellt werden.

- [ ] **Step 5: Verifikation der Phase-1-Repo-Methoden**

```pwsh
rg -n "GetByProductionOrderIdsAsync|GetIsApplicablePivotAsync" IdealAkeWms/Data/Repositories/
```

Erwartet: beide Methoden existieren in `IProductionOrderPickingStatusRepository` bzw. `IProductionOrderAssemblyGroupRepository`. Falls nicht → Phase 1 unvollständig, blockiert Phase 2.

- [ ] **Step 6: Cross-Referenzen `/ProductionOrders/`-Links zählen**

```pwsh
rg -n "asp-controller=\"ProductionOrders\"" IdealAkeWms/Views/
rg -n "Url\.Action\(.+, \"ProductionOrders\"" IdealAkeWms/
```

Erwartete Treffer (Phase-1-Stand):
- `_Layout.cshtml`: 1-2 (Nav-Link).
- `Views/ProductionOrders/Index.cshtml`: mehrere intern (`asp-action` ohne `asp-controller` zählt nicht).

Andere Treffer (z. B. aus OseonReporting, Picking-Views) im Plan-Task 6 explizit handhaben oder als "Risiko 12.7" entscheiden, dass sie auf den slim-Index zeigen sollen.

---

## Task 1: ViewModels + ColumnDefinitions

**Files:**
- New: `IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs`
- New: `IdealAkeWms/Models/ViewModels/PickingLeitstandViewModel.cs`
- Modify: `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs`
- Delete: `IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs` (im letzten Step)

Build bleibt während der Steps 1-3 grün, weil die alte Datei noch existiert (parallel zu den neuen). Step 4 entfernt sie — ab da brechen die Konsumenten. Reparatur in Tasks 4/5/8.

- [ ] **Step 1: `ProductionOrderListViewModel.cs` anlegen**

```csharp
namespace IdealAkeWms.Models.ViewModels;

public class ProductionOrderListViewModel
{
    public List<ProductionOrderListItem> Items { get; set; } = new();
    public string? FilterOrderNumber { get; set; }
    public string? FilterArticleNumber { get; set; }
    public string? FilterCustomer { get; set; }
    public bool ShowDone { get; set; }
    public int KommissionierTage { get; set; }
    public int VorkommissionierTage { get; set; }
    public int BeschichtungTage { get; set; }
    public bool CanPick { get; set; }

    /// <summary>enaio DMS-Links pro FA-Nummer (Key=OrderNumber, Value=Liste von DMS-Dokumenten)</summary>
    public Dictionary<string, List<Data.Repositories.EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();
}

public class ProductionOrderListItem
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Customer { get; set; }
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public string? Description2 { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public bool IsDone { get; set; }
    public string? WorkplaceName { get; set; }

    // Calculated dates
    public DateTime? KommissionierTermin { get; set; }
    public DateTime? VorkommissionierTermin { get; set; }
    public DateTime? BeschichtungTermin { get; set; }

    // Cross-cutting from PickingStatus (siehe Spec 6.1) — fuer Beschichtungstermin-Logik
    public bool HasCoatingParts { get; set; }
    public bool IsCoatingDone { get; set; }
}
```

- [ ] **Step 2: `PickingLeitstandViewModel.cs` anlegen**

```csharp
namespace IdealAkeWms.Models.ViewModels;

public class PickingLeitstandViewModel
{
    public List<PickingLeitstandItem> Items { get; set; } = new();
    public string? FilterOrderNumber { get; set; }
    public string? FilterArticleNumber { get; set; }
    public string? FilterCustomer { get; set; }
    public bool ShowDone { get; set; }
    public int KommissionierTage { get; set; }
    public int VorkommissionierTage { get; set; }
    public int BeschichtungTage { get; set; }
    public bool CanPick { get; set; }
    public bool CanManagePickingRelease { get; set; }
    public bool LeitstandAktiv { get; set; }
    public bool PickerAssignmentEnabled { get; set; }

    public Dictionary<string, List<Data.Repositories.EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();
}

public class PickingLeitstandItem
{
    // Sage-Master (identisch zu ProductionOrderListItem)
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Customer { get; set; }
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public string? Description2 { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public bool IsDone { get; set; }
    public string? WorkplaceName { get; set; }
    public DateTime? KommissionierTermin { get; set; }
    public DateTime? VorkommissionierTermin { get; set; }
    public DateTime? BeschichtungTermin { get; set; }

    // PickingStatus-Felder (aus ProductionOrderPickingStatus)
    public string? PickingStatus { get; set; }
    public bool HasGlass { get; set; }
    public bool HasExternalPurchase { get; set; }
    public bool HasCoatingParts { get; set; }
    public bool IsCoatingDone { get; set; }
    public bool IsReleasedForPicking { get; set; }
    public int? PickingPriority { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string? ReleasedBy { get; set; }
    public int? AssignedPickerId { get; set; }
    public string? AssignedPickerName { get; set; }

    // AssemblyGroup-Pivot (5 Bools)
    public bool HasCooling { get; set; }        // VK
    public bool HasFan { get; set; }            // VL
    public bool HasElectric { get; set; }       // VE
    public bool HasDoors { get; set; }          // VT
    public bool HasSuperstructure { get; set; } // VA
}
```

- [ ] **Step 3: `ColumnDefinitions.cs` umbauen**

In `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` die bisherige `public static readonly ViewConfig ProductionOrders = …` (Zeilen 19-53 nach Phase 1) **komplett ersetzen** durch:

```csharp
/// <summary>
/// ProductionOrders/Index.cshtml columns — Phase 2 slim FA-Uebersicht.
/// Conditional columns:
///   - "actions" : rendered only when Model.CanPick (Stueckliste-Button column).
/// </summary>
public static readonly ViewConfig ProductionOrders = new(
    "ProductionOrders", "Fertigungsauftraege (Slim)",
    SupportsReorder: true, SupportsSortDefault: true)
{
    Columns =
    [
        new ColumnDef("actions",        "",              Locked: true,  DefaultWidth: 40),
        new ColumnDef("order-number",   "FA Nr.",        Locked: true,  DefaultWidth: 90),
        new ColumnDef("quantity",       "Stk.",          Locked: false, DefaultWidth: 55),
        new ColumnDef("customer",       "Kunde",         Locked: false),
        new ColumnDef("article-number", "Artikelnummer", Locked: false),
        new ColumnDef("description1",   "Bezeichnung 1", Locked: false),
        new ColumnDef("description2",   "Bezeichnung 2", Locked: false),
        new ColumnDef("workbench",      "Werkbank",      Locked: false),
        new ColumnDef("coating-date",   "Beschicht.",    Locked: false),
        new ColumnDef("bg-date",        "BG-Termin",     Locked: false),
        new ColumnDef("picking-date",   "Komm.",         Locked: false),
        new ColumnDef("production-date","Fert.-Termin",  Locked: false),
        new ColumnDef("delivery-date",  "Liefertermin",  Locked: false),
        new ColumnDef("row-actions",    "",              Locked: true,  DefaultWidth: 80),
    ]
};
```

Und direkt unter `ProductionOrders` (vor dem bestehenden `Picking`-Eintrag) den neuen `PickingLeitstand`-Block einfügen:

```csharp
/// <summary>
/// PickingLeitstand/Index.cshtml columns — Phase 2 rich Kommissionier-Leitstand-View.
/// Conditional columns:
///   - "bulk-select" : rendered only when Model.LeitstandAktiv &amp;&amp; Model.CanManagePickingRelease
///   - "actions"     : rendered only when Model.CanPick
///   - "release"     : rendered only when Model.LeitstandAktiv &amp;&amp; Model.CanManagePickingRelease
///   - "picker"      : rendered only when Model.PickerAssignmentEnabled
/// </summary>
public static readonly ViewConfig PickingLeitstand = new(
    "PickingLeitstand", "Kommissionier-Leitstand",
    SupportsReorder: true, SupportsSortDefault: true)
{
    Columns =
    [
        new ColumnDef("bulk-select",    "",              Locked: true,  DefaultWidth: 32),
        new ColumnDef("actions",        "",              Locked: true,  DefaultWidth: 40),
        new ColumnDef("order-number",   "FA Nr.",        Locked: true,  DefaultWidth: 90),
        new ColumnDef("quantity",       "Stk.",          Locked: false, DefaultWidth: 55),
        new ColumnDef("customer",       "Kunde",         Locked: false),
        new ColumnDef("article-number", "Artikelnummer", Locked: false),
        new ColumnDef("description1",   "Bezeichnung 1", Locked: false),
        new ColumnDef("description2",   "Bezeichnung 2", Locked: false),
        new ColumnDef("workbench",      "Werkbank",      Locked: false),
        new ColumnDef("coating-date",   "Beschicht.",    Locked: false),
        new ColumnDef("bg-date",        "BG-Termin",     Locked: false),
        new ColumnDef("picking-date",   "Komm.",         Locked: false),
        new ColumnDef("production-date","Fert.-Termin",  Locked: false),
        new ColumnDef("delivery-date",  "Liefertermin",  Locked: false),
        new ColumnDef("coating-part",   "Lack-T",        Locked: false, DefaultWidth: 55),
        new ColumnDef("glass",          "Glas",          Locked: false, DefaultWidth: 45),
        new ColumnDef("purchase",       "Zukauf",        Locked: false, DefaultWidth: 55),
        new ColumnDef("cooling",        "VK",            Locked: false, DefaultWidth: 40),
        new ColumnDef("fan",            "VL",            Locked: false, DefaultWidth: 40),
        new ColumnDef("electric",       "VE",            Locked: false, DefaultWidth: 40),
        new ColumnDef("doors",          "VT",            Locked: false, DefaultWidth: 40),
        new ColumnDef("superstructure", "VA",            Locked: false, DefaultWidth: 40),
        new ColumnDef("status",         "Status",        Locked: false),
        new ColumnDef("row-actions",    "",              Locked: true,  DefaultWidth: 80),
        new ColumnDef("release",        "Freigabe",      Locked: false, DefaultWidth: 160),
        new ColumnDef("picker",         "Kommissionierer", Locked: false),
    ]
};
```

Im `GetByViewKey`-Switch-Statement (am Ende der Klasse, Zeile 154-162) den neuen Case ergänzen:

```csharp
public static ViewConfig? GetByViewKey(string viewKey) => viewKey switch
{
    "ProductionOrders"  => ProductionOrders,
    "PickingLeitstand"  => PickingLeitstand,
    "Picking"           => Picking,
    "OseonTracking"     => OseonTracking,
    "Bom"               => Bom,
    "BdeBookings"       => BdeBookings,
    _                   => null
};
```

- [ ] **Step 4: Alte `ProductionOrderViewModel.cs` löschen**

```pwsh
git rm IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs
```

Build wird rot — `ProductionOrdersController.Index` und `Views/ProductionOrders/Index.cshtml` referenzieren `ProductionOrderViewItem` und `ProductionOrderViewModel`. Reparatur kommt in Tasks 4-5. Beabsichtigt.

- [ ] **Step 5: Build verifizieren**

```pwsh
dotnet build --nologo 2>&1 | rg "error"
```

Erwartete Fehler (Beispiel):
```
IdealAkeWms/Controllers/ProductionOrdersController.cs(283,...): error CS0246: The type or namespace name 'ProductionOrderViewItem' could not be found
IdealAkeWms/Controllers/ProductionOrdersController.cs(344,...): error CS0246: The type or namespace name 'ProductionOrderViewModel' could not be found
IdealAkeWms/Views/ProductionOrders/Index.cshtml: ... 'ProductionOrderViewModel' could not be found
```

Genau diese Fehler erwartet. Tasks 4-5 reparieren sie.

- [ ] **Step 6: Commit**

```pwsh
git add IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs `
        IdealAkeWms/Models/ViewModels/PickingLeitstandViewModel.cs `
        IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs
git rm IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs
git commit -m @'
refactor(productionorders): split ProductionOrderViewItem into slim + rich VMs

Spec 6. Phase 2 view-split. Slim ProductionOrderListItem (Sage-Master +
Beschichtung) for ProductionOrders/Index, rich PickingLeitstandItem
(+ PickingStatus + AssemblyGroup-Pivot) for new PickingLeitstand/Index.
ColumnDefinitions: ProductionOrders shrinks to 14 columns, new
PickingLeitstand ViewConfig adds 25+ columns.

Build is intentionally red after this commit — consumers in
ProductionOrdersController and Index.cshtml will be repaired in Tasks 4-5.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 2: PickingLeitstandController (neu)

**Files:**
- New: `IdealAkeWms/Controllers/PickingLeitstandController.cs`

Build bleibt rot (von Task 1 Step 4). Dieser Task fügt die neuen Actions hinzu, die per Razor-Konvention auf `Views/PickingLeitstand/Index.cshtml` zeigen — die View kommt erst in Task 3.

- [ ] **Step 1: Datei-Skelett anlegen mit DI**

```csharp
using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequirePickingOrLeitstandAccess]
public class PickingLeitstandController : Controller
{
    private readonly IProductionOrderRepository _productionOrderRepository;
    private readonly IProductionOrderPickingStatusRepository _pickingStatusRepository;
    private readonly IProductionOrderAssemblyGroupRepository _assemblyGroupRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppSettingRepository _settingRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly IBusinessDayService _businessDayService;
    private readonly IEnaioDmsDocumentRepository _enaioDmsDocumentRepository;
    private readonly IUserRepository _userRepository;

    public PickingLeitstandController(
        IProductionOrderRepository productionOrderRepository,
        IProductionOrderPickingStatusRepository pickingStatusRepository,
        IProductionOrderAssemblyGroupRepository assemblyGroupRepository,
        ICurrentUserService currentUserService,
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        IBusinessDayService businessDayService,
        IEnaioDmsDocumentRepository enaioDmsDocumentRepository,
        IUserRepository userRepository)
    {
        _productionOrderRepository = productionOrderRepository;
        _pickingStatusRepository = pickingStatusRepository;
        _assemblyGroupRepository = assemblyGroupRepository;
        _currentUserService = currentUserService;
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _businessDayService = businessDayService;
        _enaioDmsDocumentRepository = enaioDmsDocumentRepository;
        _userRepository = userRepository;
    }

    // Actions in nachfolgenden Steps:
    //   - Index            (Step 2)
    //   - ToggleRelease    (Step 3)
    //   - BulkRelease      (Step 4)
    //   - SetPriority      (Step 5)
    //   - ChangeAssignedPicker (Step 6)
}
```

Class-Level-Attribut `[RequirePickingOrLeitstandAccess]` referenziert das Filter aus Task 7. Solange Task 7 noch nicht fertig ist → Compile-Fehler. **Workflow:** entweder erst Task 7, dann Task 2 (sauberer Build pro Commit), ODER beide in einer Sitzung mit Build erst am Ende. Plan empfiehlt: **erst Task 7 ausführen, dann Task 2**. Step-Reihenfolge im Plan-Dokument ist Doku-Reihenfolge, nicht zwingend Ausführungsreihenfolge.

- [ ] **Step 2: `Index`-Action implementieren (Rich-Mapping aus Phase-1-ProductionOrdersController.Index)**

```csharp
public async Task<IActionResult> Index(
    string? filterOrderNumber,
    string? filterArticleNumber,
    string? filterCustomer,
    bool showDone = false)
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

    var kommissionierTage = await _settingRepository.GetIntValueAsync("KommissionierTage", 4);
    var vorkommissionierTage = await _settingRepository.GetIntValueAsync("VorkommissionierTage", 1);
    var beschichtungTage = await _settingRepository.GetIntValueAsync("BeschichtungTage", 10);
    var beschichtungAbholtageSetting = await _settingRepository.GetValueAsync(AppSettingKeys.BeschichtungAbholtage) ?? "Dienstag,Donnerstag";
    var pickupDays = _businessDayService.ParsePickupDays(beschichtungAbholtageSetting);
    var holidays = await _holidayRepository.GetHolidayDatesAsync();
    var lackierteilName = await _settingRepository.GetValueAsync(AppSettingKeys.LackierteilKategorieName);
    var coatingFeatureActive = !string.IsNullOrWhiteSpace(lackierteilName);
    ViewBag.LackierteilKategorieName = lackierteilName;

    // Pivot-/PickingStatus-Lookups (Phase 1)
    var orderIds = orders.Select(o => o.Id).ToList();
    var groupPivot = await _assemblyGroupRepository.GetIsApplicablePivotAsync(orderIds);
    var pickingStatuses = await _pickingStatusRepository.GetByProductionOrderIdsAsync(orderIds);

    var viewItems = orders.Select(o =>
    {
        var ps = pickingStatuses.GetValueOrDefault(o.Id);
        var grp = groupPivot.GetValueOrDefault(o.Id) ?? new Dictionary<string, bool>();

        var item = new PickingLeitstandItem
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Quantity = o.Quantity,
            Customer = o.Customer,
            ArticleNumber = o.ArticleNumber,
            Description1 = o.Description1,
            Description2 = o.Description2,
            ProductionDate = o.ProductionDate,
            DeliveryDate = o.DeliveryDate,
            IsDone = o.IsDone,
            WorkplaceName = o.ProductionWorkplace?.Name,
            PickingStatus = ps?.PickingStatus,
            HasGlass = ps?.HasGlass ?? false,
            HasExternalPurchase = ps?.HasExternalPurchase ?? false,
            HasCoatingParts = ps?.HasCoatingParts ?? false,
            IsCoatingDone = ps?.IsCoatingDone ?? false,
            IsReleasedForPicking = ps?.IsReleasedForPicking ?? false,
            PickingPriority = ps?.PickingPriority,
            ReleasedAt = ps?.ReleasedAt,
            ReleasedBy = ps?.ReleasedBy,
            AssignedPickerId = ps?.AssignedPickerId,
            AssignedPickerName = ps?.AssignedPickerName,
            HasCooling = grp.GetValueOrDefault("VK"),
            HasFan = grp.GetValueOrDefault("VL"),
            HasElectric = grp.GetValueOrDefault("VE"),
            HasDoors = grp.GetValueOrDefault("VT"),
            HasSuperstructure = grp.GetValueOrDefault("VA"),
        };

        if (o.ProductionDate.HasValue)
        {
            item.KommissionierTermin = _businessDayService.SubtractBusinessDays(
                o.ProductionDate.Value, kommissionierTage, holidays);
            item.VorkommissionierTermin = _businessDayService.SubtractBusinessDays(
                item.KommissionierTermin.Value, vorkommissionierTage, holidays);

            if (!coatingFeatureActive || item.HasCoatingParts)
            {
                var rawBeschichtung = _businessDayService.SubtractBusinessDays(
                    item.VorkommissionierTermin.Value, beschichtungTage, holidays);
                item.BeschichtungTermin = _businessDayService.FindPreviousPickupDay(rawBeschichtung, pickupDays);
            }
        }

        return item;
    }).ToList();

    var orderNumbers = viewItems.Select(i => i.OrderNumber).Distinct().ToList();
    var dmsLinks = await _enaioDmsDocumentRepository.GetByOrderNumbersAsync(orderNumbers);

    var leitstandAktiv = (await _settingRepository.GetValueAsync(AppSettingKeys.LeitstandAktiv))
        ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    var pickerAssignmentEnabled = leitstandAktiv && (await _settingRepository.GetValueAsync(AppSettingKeys.KommissionierungMitZuweisung))
        ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

    var vm = new PickingLeitstandViewModel
    {
        Items = viewItems,
        FilterOrderNumber = filterOrderNumber,
        FilterArticleNumber = filterArticleNumber,
        FilterCustomer = filterCustomer,
        ShowDone = showDone,
        KommissionierTage = kommissionierTage,
        VorkommissionierTage = vorkommissionierTage,
        BeschichtungTage = beschichtungTage,
        CanPick = await _currentUserService.CanPickAsync(),
        CanManagePickingRelease = await _currentUserService.CanManagePickingReleaseAsync(),
        LeitstandAktiv = leitstandAktiv,
        PickerAssignmentEnabled = pickerAssignmentEnabled,
        EnaioDmsLinks = dmsLinks
    };

    if (pickerAssignmentEnabled)
        ViewBag.ActivePickers = await _userRepository.GetActivePickersAsync();

    return View(vm);
}
```

**Verifikation:** Dieser Code ist **strukturell identisch** zur heutigen `ProductionOrdersController.Index` (Phase-1-Stand) — Differenz nur im Output-Typ (`PickingLeitstandViewModel`/`PickingLeitstandItem` statt `ProductionOrderViewModel`/`ProductionOrderViewItem`) und in den drei entfernten Slim-Eigenschaften (die hier alle drin sind, weil rich-View). Vergleiche Side-by-Side mit `IdealAkeWms/Controllers/ProductionOrdersController.cs:235-367` heute.

- [ ] **Step 3: `ToggleRelease`-Action (1:1 aus ProductionOrdersController)**

Kopiere [`ProductionOrdersController.cs:39-101`](IdealAkeWms/Controllers/ProductionOrdersController.cs#L39-L101) wortwörtlich in den neuen Controller. **Nach Phase 1 sollte der Body bereits auf `_pickingStatusRepository` umgestellt sein** — das Code-Snippet hier nutzt heutige Phase-0-Direkt-Property-Setter; nach Phase-1-Merge schaut der Body anders aus (siehe Phase-1-Plan Task 6, Step 3). **Verifiziere bei Übernahme**, dass die kopierte Action die Phase-1-konforme Variante mit Repo-Calls ist:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[RequireLeitstandAccess]
public async Task<IActionResult> ToggleRelease(int id, int? assignedPickerId, string? returnUrl)
{
    var order = await _productionOrderRepository.GetByIdAsync(id);
    if (order == null) return NotFound();

    var ps = await _pickingStatusRepository.GetByProductionOrderIdAsync(id);
    if (ps == null) return NotFound();  // sollte nach Eager-Create nie passieren

    if (!ps.IsReleasedForPicking && string.IsNullOrEmpty(order.ArticleNumber))
    {
        TempData["WarningMessage"] = $"FA {order.OrderNumber} kann nicht freigegeben werden — keine Artikelnummer vorhanden.";
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    var pickerAssignmentEnabled = (await _settingRepository.GetValueAsync(AppSettingKeys.KommissionierungMitZuweisung))
        ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

    if (!ps.IsReleasedForPicking && pickerAssignmentEnabled && !assignedPickerId.HasValue)
    {
        TempData["WarningMessage"] = "Bitte einen Kommissionierer zuweisen.";
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    var displayName = _currentUserService.GetDisplayName();
    var windowsUser = _currentUserService.GetWindowsUserName();
    var newReleased = !ps.IsReleasedForPicking;

    int? newPriority = ps.PickingPriority;
    string? releasedBy = ps.ReleasedBy;
    int? newPickerId = ps.AssignedPickerId;
    string? newPickerName = ps.AssignedPickerName;

    if (newReleased)
    {
        releasedBy = displayName;
        if (!ps.PickingPriority.HasValue)
        {
            var maxPrio = (await _pickingStatusRepository.GetReleasedForPickingAsync())
                .Where(o => o.PickingStatus?.PickingPriority.HasValue == true && o.Id != order.Id)
                .Select(o => o.PickingStatus!.PickingPriority!.Value)
                .DefaultIfEmpty(0)
                .Max();
            newPriority = maxPrio + 1;
        }

        if (assignedPickerId.HasValue)
        {
            var picker = await _userRepository.GetByIdAsync(assignedPickerId.Value);
            newPickerId = assignedPickerId;
            newPickerName = picker?.Name;
        }
    }
    else
    {
        newPickerId = null;
        newPickerName = null;
    }

    await _pickingStatusRepository.SetReleaseAsync(id, newReleased, newPriority, releasedBy, displayName, windowsUser);
    await _pickingStatusRepository.SetAssignedPickerAsync(id, newPickerId, newPickerName, displayName, windowsUser);

    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
    return RedirectToAction(nameof(Index));
}
```

**Hinweis:** das genaue Repo-API hängt vom Phase-1-Plan-Step (Task 4, Step 1+2) ab. Falls `SetReleaseAsync` und `SetAssignedPickerAsync` dort anders geschnitten sind, an die Phase-1-Signatur anpassen. Plan-Spec 7.1 gibt die Soll-Schnittstelle vor.

- [ ] **Step 4: `BulkRelease`-Action (1:1)**

Kopiere [`ProductionOrdersController.cs:103-192`](IdealAkeWms/Controllers/ProductionOrdersController.cs#L103-L192). Analog wie Step 3 — auf Phase-1-Repo-Calls verifizieren. Es ist die einzige Stelle mit `foreach (var id in ids)` und Skip-Logik bei fehlender Artikelnummer.

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[RequireLeitstandAccess]
public async Task<IActionResult> BulkRelease(List<int> ids, bool release, int? assignedPickerId, string? returnUrl)
{
    // 1:1 aus ProductionOrdersController.cs:103-192, mit _pickingStatusRepository statt direkt auf order.Is...
    // (siehe Phase-1-Plan Task 6, Step 4 für die Repo-Variante)
}
```

- [ ] **Step 5: `SetPriority`-Action**

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[RequireLeitstandAccess]
public async Task<IActionResult> SetPriority(int id, int? priority)
{
    var ps = await _pickingStatusRepository.GetByProductionOrderIdAsync(id);
    if (ps == null) return NotFound();

    await _pickingStatusRepository.SetPriorityAsync(
        id, priority,
        _currentUserService.GetDisplayName(),
        _currentUserService.GetWindowsUserName());

    return Ok();
}
```

- [ ] **Step 6: `ChangeAssignedPicker`-Action**

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[RequireLeitstandAccess]
public async Task<IActionResult> ChangeAssignedPicker(int id, int assignedPickerId)
{
    var ps = await _pickingStatusRepository.GetByProductionOrderIdAsync(id);
    if (ps == null) return NotFound();

    var picker = await _userRepository.GetByIdAsync(assignedPickerId);
    if (picker == null) return BadRequest("Kommissionierer nicht gefunden.");

    await _pickingStatusRepository.SetAssignedPickerAsync(
        id, assignedPickerId, picker.Name,
        _currentUserService.GetDisplayName(),
        _currentUserService.GetWindowsUserName());

    return Ok();
}
```

- [ ] **Step 7: Build prüfen**

```pwsh
dotnet build --nologo 2>&1 | rg "error"
```

Erwartet: Errors aus Task 1 Step 5 (`ProductionOrderViewItem`/`ProductionOrderViewModel`) reduziert auf nur noch die ProductionOrdersController.Index-Stelle und Views/ProductionOrders/Index.cshtml. Die neuen `PickingLeitstandController`-Actions müssen kompilieren — falls noch Errors aus Filter-Referenz (`RequirePickingOrLeitstandAccess`) → Task 7 vorher abarbeiten.

- [ ] **Step 8: Commit**

```pwsh
git add IdealAkeWms/Controllers/PickingLeitstandController.cs
git commit -m @'
feat(picking): new PickingLeitstandController with 5 actions migrated from ProductionOrders

Spec 5 + 9.2. Phase 2 Option B (new controller, not new action on PickingController).
Migrates Index, ToggleRelease, BulkRelease, SetPriority, ChangeAssignedPicker
1:1 from ProductionOrdersController. Class-level [RequirePickingOrLeitstandAccess],
action-level [RequireLeitstandAccess] on bulk/release/priority/picker actions.

Uses Phase-1 IProductionOrderPickingStatusRepository + GetIsApplicablePivotAsync
for rich view-mapping.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 3: View `Views/PickingLeitstand/Index.cshtml` (neu, forked)

**Files:**
- New: `IdealAkeWms/Views/PickingLeitstand/Index.cshtml`

Die rich-View entsteht durch Fork der heutigen `Views/ProductionOrders/Index.cshtml` (Phase-1-Stand). Hauptarbeit ist Find-and-Replace + zwei strukturelle Anpassungen.

- [ ] **Step 1: Verzeichnis anlegen + Datei initial kopieren**

```pwsh
mkdir IdealAkeWms/Views/PickingLeitstand
copy IdealAkeWms/Views/ProductionOrders/Index.cshtml IdealAkeWms/Views/PickingLeitstand/Index.cshtml
```

- [ ] **Step 2: Model-Typ ändern**

In Zeile 1 ersetzen:

```razor
@model ProductionOrderViewModel
```

durch:

```razor
@model PickingLeitstandViewModel
```

- [ ] **Step 3: `data-view-key` ändern**

In der Filterable-Table-Zeile (heute Zeile 90):

```razor
<table class="table table-striped mb-0 filterable-table" data-view-key="ProductionOrders" data-fallback-sort-column="picking-date" data-fallback-sort-direction="asc">
```

ändern zu:

```razor
<table class="table table-striped mb-0 filterable-table" data-view-key="PickingLeitstand" data-fallback-sort-column="picking-date" data-fallback-sort-direction="asc">
```

In der `view-config`-JSON (heute Zeile 494-496):

```html
<script type="application/json" id="view-config">
{ "viewKey": "PickingLeitstand", "supportsReorder": true, "supportsSortDefault": true }
</script>
```

In der `data-clear-table-filters`-Referenz (heute Zeile 59):

```razor
<a asp-action="Index" class="btn btn-outline-secondary" data-clear-table-filters="PickingLeitstand">Zurücksetzen</a>
```

In der `table-filter-applied`-Event-Detail-Prüfung (heute Zeile 720-723):

```javascript
document.addEventListener('table-filter-applied', function (e) {
    if (e.detail && e.detail.viewKey === 'PickingLeitstand') {
        bulkSyncSelection();
    }
});
```

- [ ] **Step 4: `asp-controller` setzen für Forms, die heute auf ProductionOrders zeigen**

Heute hat die View Forms wie:

```razor
<form asp-action="BulkRelease" method="post" id="bulkReleaseForm" class="d-inline me-2">
<form asp-action="ToggleRelease" method="post" style="display:inline">
<form asp-action="BulkRelease" method="post" id="bulkReleaseModalForm">
```

Für jede dieser Forms `asp-controller="PickingLeitstand"` ergänzen:

```razor
<form asp-controller="PickingLeitstand" asp-action="BulkRelease" method="post" id="bulkReleaseForm" class="d-inline me-2">
<form asp-controller="PickingLeitstand" asp-action="ToggleRelease" method="post" style="display:inline">
<form asp-controller="PickingLeitstand" asp-action="BulkRelease" method="post" id="bulkReleaseModalForm">
```

Auch die JavaScript-`Url.Action`-Calls für `SetPriority` und `ChangeAssignedPicker`:

```javascript
url: '@Url.Action("SetPriority", "PickingLeitstand")',
url: '@Url.Action("ChangeAssignedPicker", "PickingLeitstand")',
```

- [ ] **Step 5: ToggleDone bleibt auf Picking-Controller**

Die heutige Form für "Erledigt"-Toggle (heute Zeile 287-309):

```razor
<form asp-controller="Picking" asp-action="ToggleDone" method="post" class="d-inline">
```

bleibt unverändert. `ToggleDone` ist eine Picker-Aktion, nicht Leitstand. Im PickingLeitstandController existiert kein `ToggleDone`.

- [ ] **Step 6: ColCount-Berechnung verifizieren (heute Zeile 369-378)**

Der "Keine Einträge"-Fallback berechnet `colCount` aus den sichtbaren Spalten:

```razor
@{
    var colCount = 17;
    if (Model.CanPick) colCount++;
    if (Model.LeitstandAktiv && Model.CanManagePickingRelease) { colCount++; colCount++; }
    if (Model.PickerAssignmentEnabled) colCount++;
}
```

Die `17` entspricht: FA Nr., Stk., Kunde, Artikelnummer, Bez. 1, Bez. 2, Werkbank, Beschicht., BG-Termin, Komm., Fert.-Termin, Liefertermin, Lack-T, Glas, Zukauf, Status, Row-Actions = 17. Aber das stimmt nicht mit der Spalten-Anzahl im neuen Rich-View überein. Verifiziere und korrigiere:

Spalten in rich-View ohne Conditionals: order-number, quantity, customer, article-number, description1, description2, workbench, coating-date, bg-date, picking-date, production-date, delivery-date, coating-part, glass, purchase, cooling, fan, electric, doors, superstructure, status, row-actions = **22 Spalten**. Plus +1 für `bulk-select` (LeitstandAktiv), +1 für `actions` (CanPick), +1 für `release` (LeitstandAktiv), +1 für `picker` (PickerAssignmentEnabled).

```razor
@{
    var colCount = 22;
    if (Model.LeitstandAktiv && Model.CanManagePickingRelease) { colCount++; /* bulk-select */ colCount++; /* release */ }
    if (Model.CanPick) colCount++;  // actions
    if (Model.PickerAssignmentEnabled) colCount++;  // picker
}
```

- [ ] **Step 7: Build + manueller Smoke-Test**

```pwsh
dotnet build --nologo 2>&1 | rg "error"
```

Erwartet: Errors aus Task 1 Step 5 reduziert auf nur noch `Views/ProductionOrders/Index.cshtml` (alte Slim-View, kommt in Task 4). Neue View kompiliert sauber.

```pwsh
dotnet run --project IdealAkeWms
```

In Browser: `/PickingLeitstand/Index` öffnen (nach Login als User mit `picking`+`leitstand`). Erwartet: rich-View lädt, alle Spalten sichtbar, Toggle-Klick funktioniert.

- [ ] **Step 8: Commit**

```pwsh
git add IdealAkeWms/Views/PickingLeitstand/Index.cshtml
git commit -m @'
feat(picking): new Views/PickingLeitstand/Index.cshtml (rich, forked from ProductionOrders/Index)

Spec 8.2. Same 23+ columns as Phase-1 ProductionOrders/Index, but
data-view-key=PickingLeitstand, all forms point asp-controller=PickingLeitstand,
SetPriority/ChangeAssignedPicker URL.Action targets the new controller.
ToggleDone form keeps asp-controller=Picking (Picker action, not Leitstand).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 4: View `Views/ProductionOrders/Index.cshtml` slim umschreiben

**Files:**
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml`

Diese Datei wird drastisch gekürzt: ~580 Zeilen Diff. Strategie: in-place edit, nicht neu schreiben — damit GIT-Diff klar zeigt, was rausfällt.

- [ ] **Step 1: ViewModel-Typ ändern**

Zeile 1:

```razor
@model ProductionOrderListViewModel
```

- [ ] **Step 2: Bulk-Action-Bar entfernen**

Lösche Zeilen 66-87 komplett (das `@if (Model.LeitstandAktiv && Model.CanManagePickingRelease)`-Block mit `bulkActionBar`).

- [ ] **Step 3: Spalten-Header reduzieren**

Im `<thead>` (Zeilen 91-133) entferne:
- Zeilen 93-98: `bulk-checkbox-col`-th.
- Zeilen 115-122: Lack-T, Glas, Zukauf, VK, VL, VE, VT, VA.
- Zeile 123: Status.
- Zeilen 125-132: Release, Picker.

Übrig bleiben: actions (CanPick), order-number, quantity, customer, article-number, description1, description2, workbench, coating-date, bg-date, picking-date, production-date, delivery-date, row-actions.

- [ ] **Step 4: TBody-Rendering reduzieren**

Im `@foreach (var item in Model.Items)`-Block (heute Zeilen 136-364) entferne:
- Zeilen 139-150: bulk-row-checkbox.
- Zeilen 221-256: 8 Checkbox-TDs (Lack-T, Glas, Zukauf, VK, VL, VE, VT, VA).
- Zeilen 257-277: Status-TD (Erledigt/Offen-Badge + Komm-Status-Badge).
- Zeilen 311-353: Freigabe-TD inkl. `priority-input`.
- Zeilen 354-363: Picker-TD inkl. `change-picker-link`.

Übrig bleibt: Beschichtungs-Termin-Render (Zeilen 200-218) — das **bleibt** weil die slim-View die rote Schrift bei überfälligen Beschichtungs-FAs zeigen soll. Aber die `item.HasCoatingParts`-Variable kommt jetzt aus dem Slim-ViewModel (siehe Spec 6.1). Code-Logik unverändert.

Row-Actions (heute Zeilen 278-310) **bleibt**: OSEON-Tracking-Link + ToggleDone-Form.

- [ ] **Step 5: Modals löschen**

Lösche Zeilen 390-491 komplett: `releaseModal`, `changePickerModal`, `bulkReleaseModal`. Auch das umgebende `@if (Model.PickerAssignmentEnabled)` und das innere `@if (Model.LeitstandAktiv && Model.CanManagePickingRelease)`.

- [ ] **Step 6: `column-config`-JSON aktualisieren**

Lösche aus dem `column-config`-Array (heute Zeilen 497-525) die Keys:
- `bulk-select`, `coating-part`, `glass`, `purchase`, `cooling`, `fan`, `electric`, `doors`, `superstructure`, `status`, `release`, `picker`.

Lösche auch die zwei conditional-Razor-Blöcke `@if (Model.LeitstandAktiv && …)` und `@if (Model.PickerAssignmentEnabled)`.

Resultat:

```html
<script type="application/json" id="column-config">
[
    @if (Model.CanPick) { <text>{ "key": "actions", "label": "", "locked": true, "defaultWidth": 40 },</text> }
    { "key": "order-number", "label": "FA Nr.", "locked": true, "defaultWidth": 90 },
    { "key": "quantity", "label": "Stk.", "locked": false, "defaultWidth": 55 },
    { "key": "customer", "label": "Kunde", "locked": false, "defaultWidth": null },
    { "key": "article-number", "label": "Artikelnummer", "locked": false, "defaultWidth": null },
    { "key": "description1", "label": "Bezeichnung 1", "locked": false, "defaultWidth": null },
    { "key": "description2", "label": "Bezeichnung 2", "locked": false, "defaultWidth": null },
    { "key": "workbench", "label": "Werkbank", "locked": false, "defaultWidth": null },
    { "key": "coating-date", "label": "Beschicht.", "locked": false, "defaultWidth": null },
    { "key": "bg-date", "label": "BG-Termin", "locked": false, "defaultWidth": null },
    { "key": "picking-date", "label": "Komm.", "locked": false, "defaultWidth": null },
    { "key": "production-date", "label": "Fert.-Termin", "locked": false, "defaultWidth": null },
    { "key": "delivery-date", "label": "Liefertermin", "locked": false, "defaultWidth": null },
    { "key": "row-actions", "label": "", "locked": true, "defaultWidth": 80 }
]
</script>
```

- [ ] **Step 7: Scripts-Section ausdünnen**

Im `@section Scripts` (heute Zeilen 528-780) lösche:
- Den `toggle-field`-Dispatcher-Block (548-567).
- Den `priority-input`-Block (572-589).
- Den Picker-/Modal-Block (591-636).
- Den Bulk-Selection-Block (638-779).

Übrig bleiben: QR-Scanner-Inits (Zeilen 535-545) + Script-Tags für `barcode-scanner.js`, `column-preferences.js`, `table-filter.js`.

Resultat (Pseudocode):

```razor
@section Scripts {
    <script src="https://unpkg.com/html5-qrcode@2.3.8/html5-qrcode.min.js"></script>
    <script src="~/js/barcode-scanner.js" asp-append-version="true"></script>
    <script src="~/js/column-preferences.js" asp-append-version="true"></script>
    <script src="~/js/table-filter.js" asp-append-version="true"></script>
    <script>
        document.addEventListener('DOMContentLoaded', function () {
            initTextInputScanner('btnScanOrderNumber', 'filterOrderNumber', 'fa', function() {
                var form = document.getElementById('filterOrderNumber').closest('form');
                if (form) form.submit();
            });
            initTextInputScanner('btnScanArticleNumber', 'filterArticleNumber', 'article', function() {
                var form = document.getElementById('filterArticleNumber').closest('form');
                if (form) form.submit();
            });
        });
    </script>
}
```

- [ ] **Step 8: Verifikation: Datei ~200 Zeilen, keine Referenzen auf entfernte Properties**

```pwsh
$lc = (Get-Content IdealAkeWms/Views/ProductionOrders/Index.cshtml).Count
Write-Host "Lines: $lc"  # Erwartung: ~180-220
rg "HasGlass|HasExternalPurchase|HasCooling|HasFan|HasElectric|HasDoors|HasSuperstructure|IsReleasedForPicking|AssignedPicker|PickingPriority|bulk-select" IdealAkeWms/Views/ProductionOrders/Index.cshtml
# Erwartung: keine Treffer
```

Falls Treffer → übriggebliebene Stellen entfernen.

- [ ] **Step 9: Build verifizieren**

```pwsh
dotnet build --nologo 2>&1 | rg "error"
```

Erwartet: keine `ProductionOrderViewModel`-Errors mehr in der View. Verbleibende Errors nur noch im `ProductionOrdersController` (Slim-Umbau in Task 5).

- [ ] **Step 10: Commit**

```pwsh
git add IdealAkeWms/Views/ProductionOrders/Index.cshtml
git commit -m @'
refactor(productionorders): Views/ProductionOrders/Index.cshtml slim (14 columns)

Spec 8.1. Drops 8 status-checkbox columns (Lack-T/Glas/Zukauf + VK/VL/VE/VT/VA),
status badge, release controls, picker column, bulk-action-bar, and all 3 modals.
Keeps: FA-Master columns + Beschicht.-Datum + Row-Actions (OSEON link + ToggleDone).
Scripts reduced to QR-Scanner inits only — no toggle dispatcher, no priority
input, no picker modal, no bulk JS.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 5: `ProductionOrdersController.cs` slim machen

**Files:**
- Modify: `IdealAkeWms/Controllers/ProductionOrdersController.cs`

- [ ] **Step 1: DI-Liste reduzieren**

Heute (Phase-1-Stand) hat der Konstruktor 7-8 Deps. Nach Phase 2:

```csharp
public class ProductionOrdersController : Controller
{
    private readonly IProductionOrderRepository _productionOrderRepository;
    private readonly IProductionOrderPickingStatusRepository _pickingStatusRepository;
    private readonly IAppSettingRepository _settingRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly IBusinessDayService _businessDayService;
    private readonly IEnaioDmsDocumentRepository _enaioDmsDocumentRepository;

    public ProductionOrdersController(
        IProductionOrderRepository productionOrderRepository,
        IProductionOrderPickingStatusRepository pickingStatusRepository,
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        IBusinessDayService businessDayService,
        IEnaioDmsDocumentRepository enaioDmsDocumentRepository)
    {
        _productionOrderRepository = productionOrderRepository;
        _pickingStatusRepository = pickingStatusRepository;
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _businessDayService = businessDayService;
        _enaioDmsDocumentRepository = enaioDmsDocumentRepository;
    }
```

**Removed:** `ICurrentUserService`, `IUserRepository`, `IProductionOrderAssemblyGroupRepository`. Begründung Spec 9.1.

`IProductionOrderPickingStatusRepository` BLEIBT, weil wir `HasCoatingParts`/`IsCoatingDone` weiterhin für die Beschichtungstermin-Logik brauchen (Spec 6.1).

- [ ] **Step 2: ToggleRelease/BulkRelease/SetPriority/ChangeAssignedPicker entfernen**

Lösche aus der Datei (Phase-1-Stand: Zeilen 39-233 entsprechend):
- `ToggleRelease` (Phase-1: ~Z. 39-101).
- `BulkRelease` (~Z. 103-192).
- `SetPriority` (~Z. 194-210).
- `ChangeAssignedPicker` (~Z. 212-233).

Diese vier Actions wandern nach `PickingLeitstandController` (Task 2 hat sie dort schon angelegt — von hier dürfen sie weg).

- [ ] **Step 3: Optionale Redirect-Stubs für Stale-Tab-Posts hinzufügen**

Direkt nach dem Konstruktor (vor `Index`) einfügen:

```csharp
// Backward-Compat-Redirects fuer Stale-Tab-Posts auf alte URLs.
// TODO: nach v1.14.0 entfernen.
[HttpPost]
public IActionResult ToggleRelease() => RedirectToActionPermanent("Index", "PickingLeitstand");

[HttpPost]
public IActionResult BulkRelease() => RedirectToActionPermanent("Index", "PickingLeitstand");

[HttpPost]
public IActionResult SetPriority() => RedirectToActionPermanent("Index", "PickingLeitstand");

[HttpPost]
public IActionResult ChangeAssignedPicker() => RedirectToActionPermanent("Index", "PickingLeitstand");
```

Begründung Spec 9.3 + Risiko 12.1.

- [ ] **Step 4: `Index`-Action slimmen**

Ersetze die Phase-1-`Index`-Action durch die Slim-Variante. Filter-Attribut tauschen, manuellen Permission-Check entfernen, Pivot-/Assembly-Group-Lookups weglassen, ViewModel-Mapping auf `ProductionOrderListItem`:

```csharp
[RequirePickingOrTrackingOrLeitstandAccess]
public async Task<IActionResult> Index(
    string? filterOrderNumber,
    string? filterArticleNumber,
    string? filterCustomer,
    bool showDone = false)
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

    var kommissionierTage = await _settingRepository.GetIntValueAsync("KommissionierTage", 4);
    var vorkommissionierTage = await _settingRepository.GetIntValueAsync("VorkommissionierTage", 1);
    var beschichtungTage = await _settingRepository.GetIntValueAsync("BeschichtungTage", 10);
    var beschichtungAbholtageSetting = await _settingRepository.GetValueAsync(AppSettingKeys.BeschichtungAbholtage) ?? "Dienstag,Donnerstag";
    var pickupDays = _businessDayService.ParsePickupDays(beschichtungAbholtageSetting);
    var holidays = await _holidayRepository.GetHolidayDatesAsync();
    var lackierteilName = await _settingRepository.GetValueAsync(AppSettingKeys.LackierteilKategorieName);
    var coatingFeatureActive = !string.IsNullOrWhiteSpace(lackierteilName);
    ViewBag.LackierteilKategorieName = lackierteilName;

    // PickingStatus-Dict nur fuer HasCoatingParts/IsCoatingDone (Beschichtungstermin-Logik)
    var orderIds = orders.Select(o => o.Id).ToList();
    var pickingStatuses = await _pickingStatusRepository.GetByProductionOrderIdsAsync(orderIds);

    var viewItems = orders.Select(o =>
    {
        var ps = pickingStatuses.GetValueOrDefault(o.Id);
        var item = new ProductionOrderListItem
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Quantity = o.Quantity,
            Customer = o.Customer,
            ArticleNumber = o.ArticleNumber,
            Description1 = o.Description1,
            Description2 = o.Description2,
            ProductionDate = o.ProductionDate,
            DeliveryDate = o.DeliveryDate,
            IsDone = o.IsDone,
            WorkplaceName = o.ProductionWorkplace?.Name,
            HasCoatingParts = ps?.HasCoatingParts ?? false,
            IsCoatingDone = ps?.IsCoatingDone ?? false,
        };

        if (o.ProductionDate.HasValue)
        {
            item.KommissionierTermin = _businessDayService.SubtractBusinessDays(
                o.ProductionDate.Value, kommissionierTage, holidays);
            item.VorkommissionierTermin = _businessDayService.SubtractBusinessDays(
                item.KommissionierTermin.Value, vorkommissionierTage, holidays);

            if (!coatingFeatureActive || item.HasCoatingParts)
            {
                var rawBeschichtung = _businessDayService.SubtractBusinessDays(
                    item.VorkommissionierTermin.Value, beschichtungTage, holidays);
                item.BeschichtungTermin = _businessDayService.FindPreviousPickupDay(rawBeschichtung, pickupDays);
            }
        }

        return item;
    }).ToList();

    var orderNumbers = viewItems.Select(i => i.OrderNumber).Distinct().ToList();
    var dmsLinks = await _enaioDmsDocumentRepository.GetByOrderNumbersAsync(orderNumbers);

    // Picker-Permission fuer Stueckliste-Icon im actions-Col
    var canPick = HttpContext.RequestServices
        .GetRequiredService<ICurrentUserService>()
        .CanPickAsync()
        .GetAwaiter().GetResult();
    // Hinweis: GetRequiredService statt DI-Field, um die Field-Deklaration nicht aufzunehmen
    // — alternative wäre, ICurrentUserService doch in DI zu behalten. Style-Entscheidung.

    var vm = new ProductionOrderListViewModel
    {
        Items = viewItems,
        FilterOrderNumber = filterOrderNumber,
        FilterArticleNumber = filterArticleNumber,
        FilterCustomer = filterCustomer,
        ShowDone = showDone,
        KommissionierTage = kommissionierTage,
        VorkommissionierTage = vorkommissionierTage,
        BeschichtungTage = beschichtungTage,
        CanPick = canPick,
        EnaioDmsLinks = dmsLinks
    };

    return View(vm);
}
```

**Entscheidung — `ICurrentUserService` weg oder behalten:** der `CanPick`-Boolean wird für die actions-Spalte gebraucht. Zwei Optionen:
- **A:** `ICurrentUserService` als DI-Field behalten, simpler Code.
- **B:** `GetRequiredService<ICurrentUserService>()` ad-hoc, DI-Liste schlanker.

**Recommended:** **Option A** behalten. Plan oben zeigt Option A in der "Entscheidung in Step 1": wenn `ICurrentUserService` ohnehin für `CanPick` gebraucht wird, kann es DI sein. **Korrektur Step 1 unten** (Bei Implementierung Step-Reihenfolge umkehren: erst Index-Action schreiben, dann DI auf die tatsächlich genutzten Repos reduzieren). Folgendes ist die final-empfohlene DI-Liste:

```csharp
private readonly IProductionOrderRepository _productionOrderRepository;
private readonly IProductionOrderPickingStatusRepository _pickingStatusRepository;
private readonly ICurrentUserService _currentUserService;
private readonly IAppSettingRepository _settingRepository;
private readonly IHolidayRepository _holidayRepository;
private readonly IBusinessDayService _businessDayService;
private readonly IEnaioDmsDocumentRepository _enaioDmsDocumentRepository;
```

Und im Index-Body: `var canPick = await _currentUserService.CanPickAsync();`. Keine `GetRequiredService`-Akrobatik.

- [ ] **Step 5: Redirect-Stubs am Ende belassen**

Die zwei Bom/Picking-Redirects am Ende der Datei (Phase-1 Zeilen 372-374) bleiben:

```csharp
public IActionResult Bom(int id) => RedirectToActionPermanent("Bom", "Picking", new { id });
public IActionResult Picking() => RedirectToActionPermanent("Index", "Picking");
```

- [ ] **Step 6: Build prüfen**

```pwsh
dotnet build --nologo 2>&1 | rg "error"
```

Erwartet: **keine Errors mehr**. Die alten `ProductionOrderViewModel`-Referenzen sind eliminiert.

- [ ] **Step 7: Smoke-Test im Browser**

```pwsh
dotnet run --project IdealAkeWms
```

- `/ProductionOrders/Index` als Tracking-User: Slim-View lädt, 14 Spalten.
- `/PickingLeitstand/Index` als Picker+Leitstand: Rich-View lädt, 23+ Spalten.
- Bulk-Release auf `/PickingLeitstand`: funktioniert.
- POST `/ProductionOrders/BulkRelease` aus Browser-DevTools: 301 zu `/PickingLeitstand/Index`.

- [ ] **Step 8: Commit**

```pwsh
git add IdealAkeWms/Controllers/ProductionOrdersController.cs
git commit -m @'
refactor(productionorders): slim ProductionOrdersController to Index-only

Spec 9.1. Phase 2 view-split:
- Index slimmed: drops AssemblyGroupRepository, drops 5-Bool-pivot lookup,
  drops Status-mapping, returns ProductionOrderListViewModel
- ToggleRelease/BulkRelease/SetPriority/ChangeAssignedPicker → moved to
  PickingLeitstandController (Task 2)
- Permission filter [RequirePickingOrTrackingOrLeitstandAccess] replaces
  manual 3-check (CLAUDE.md fallstrick "Leitstand Index-Action hat kein
  Filter-Attribut" obsolete)
- Backward-compat redirects for stale-tab POSTs to old URLs
  (TODO: remove in v1.14.0)

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 6: Nav-Bar Update

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Heutige Logik vor Augen führen**

Heute (Zeilen 65-94):

```razor
@if (leitstandAktiv) {
    @if (canManagePickingRelease || canViewTracking) {
        <li class="nav-item">
            <a class="nav-link" asp-controller="ProductionOrders" asp-action="Index">Fertigungsauftraege</a>
        </li>
    }
} else {
    @if (canPick || canViewTracking) {
        <li class="nav-item">
            <a class="nav-link" asp-controller="ProductionOrders" asp-action="Index">Fertigungsauftraege</a>
        </li>
    }
}
@if (canPick) {
    <li class="nav-item">
        <a class="nav-link" asp-controller="Picking" asp-action="Index">
            Kommissionierung
            @if (leitstandAktiv && releasedPickingCount > 0) {
                <span class="badge rounded-pill" style="background-color: var(--ake-orange); font-size: 0.7em;">@releasedPickingCount</span>
            }
        </a>
    </li>
}
```

- [ ] **Step 2: Neue Nav-Struktur**

Ersetze beide Blocks durch:

```razor
@* Phase 2: FA-Uebersicht (slim) — fuer Picker/Tracker/Leitstand sichtbar *@
@if (canPick || canViewTracking || canManagePickingRelease)
{
    <li class="nav-item">
        <a class="nav-link" asp-controller="ProductionOrders" asp-action="Index">Fertigungsauftraege</a>
    </li>
}

@* Phase 2: Kommissionierung-Dropdown — Picker-Worklist + Leitstand *@
@if (canPick || canManagePickingRelease)
{
    <li class="nav-item dropdown">
        <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown" aria-expanded="false">
            Kommissionierung
            @if (leitstandAktiv && releasedPickingCount > 0)
            {
                <span class="badge rounded-pill" style="background-color: var(--ake-orange); font-size: 0.7em;">@releasedPickingCount</span>
            }
        </a>
        <ul class="dropdown-menu">
            @if (canPick)
            {
                <li><a class="dropdown-item" asp-controller="Picking" asp-action="Index">Kommissionierliste</a></li>
            }
            @if (leitstandAktiv && (canPick || canManagePickingRelease))
            {
                <li><a class="dropdown-item" asp-controller="PickingLeitstand" asp-action="Index">Leitstand</a></li>
            }
        </ul>
    </li>
}
```

**Edge-Case Verifikation (siehe Spec 10):**
- `picking` only: sieht "Fertigungsauftraege" + Dropdown mit "Kommissionierliste" (kein Leitstand wenn `leitstandAktiv=false`).
- `picking` + `leitstand`, `leitstandAktiv=true`: sieht beide Dropdown-Einträge.
- `leitstand` only (kein `picking`), `leitstandAktiv=true`: sieht "Fertigungsauftraege" + Dropdown mit nur "Leitstand"-Eintrag.
- `tracking` only: sieht nur "Fertigungsauftraege".

- [ ] **Step 3: `releasedPickingCount`-Quelle prüfen**

Heute lädt das Layout (Zeile 38):

```razor
var releasedPickingCount = leitstandAktiv && canPick ? await ProductionOrderRepository.GetReleasedForPickingCountAsync() : 0;
```

Nach Phase 1 sollte diese Methode in `IProductionOrderPickingStatusRepository` liegen. Verifikation:

```pwsh
rg -n "GetReleasedForPickingCountAsync" IdealAkeWms/Data/Repositories/
```

Falls die Methode in `IProductionOrderRepository` verblieb (Phase-1-Plan Task 4 Step 5 lässt das offen), Layout-Code unverändert lassen. Falls sie wirklich nach `IProductionOrderPickingStatusRepository` verschoben wurde, ändere die DI-Inject-Direktive im Layout-Header:

```razor
@inject IdealAkeWms.Data.Repositories.IProductionOrderPickingStatusRepository PickingStatusRepository
```

und den Aufruf:

```razor
var releasedPickingCount = leitstandAktiv && canPick ? await PickingStatusRepository.GetReleasedForPickingCountAsync() : 0;
```

- [ ] **Step 4: Build + Smoke**

```pwsh
dotnet build --nologo
dotnet run --project IdealAkeWms
```

- Login als Picker (kein Leitstand): Nav zeigt "Fertigungsauftraege" + "Kommissionierung"-Dropdown mit "Kommissionierliste". Kein Leitstand-Eintrag (wenn `LeitstandAktiv=false`) oder mit Leitstand-Eintrag (wenn `=true`).
- Login als Tracking-User: Nav zeigt nur "Fertigungsauftraege". Kein Kommissionierung-Dropdown.

- [ ] **Step 5: Commit**

```pwsh
git add IdealAkeWms/Views/Shared/_Layout.cshtml
git commit -m @'
feat(layout): nav-bar split — slim Fertigungsauftraege + Kommissionierung dropdown

Spec 10. Phase 2 view-split:
- "Fertigungsauftraege" link visible for canPick OR canViewTracking OR canManagePickingRelease.
- "Kommissionierung" dropdown with sub-items "Kommissionierliste" (canPick)
  and "Leitstand" (canPick OR canManagePickingRelease, LeitstandAktiv).
- releasedPickingCount badge moves to dropdown-toggle (still leads to Kommissionierliste).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 7: Permission-Filter (zwei neue Attribute)

**Files:**
- New: `IdealAkeWms/Filters/RequirePickingOrTrackingOrLeitstandAccessAttribute.cs`
- New: `IdealAkeWms/Filters/RequirePickingOrLeitstandAccessAttribute.cs`

- [ ] **Step 1: `RequirePickingOrTrackingOrLeitstandAccessAttribute.cs` anlegen**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

public class RequirePickingOrTrackingOrLeitstandAccessAttribute : TypeFilterAttribute
{
    public RequirePickingOrTrackingOrLeitstandAccessAttribute()
        : base(typeof(RequirePickingOrTrackingOrLeitstandAccessFilter)) { }
}

public class RequirePickingOrTrackingOrLeitstandAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequirePickingOrTrackingOrLeitstandAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanPickAsync()
            && !await _currentUserService.CanViewTrackingAsync()
            && !await _currentUserService.CanManagePickingReleaseAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
```

- [ ] **Step 2: `RequirePickingOrLeitstandAccessAttribute.cs` anlegen**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

public class RequirePickingOrLeitstandAccessAttribute : TypeFilterAttribute
{
    public RequirePickingOrLeitstandAccessAttribute()
        : base(typeof(RequirePickingOrLeitstandAccessFilter)) { }
}

public class RequirePickingOrLeitstandAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequirePickingOrLeitstandAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanPickAsync()
            && !await _currentUserService.CanManagePickingReleaseAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
```

- [ ] **Step 3: Build verifizieren**

```pwsh
dotnet build --nologo 2>&1 | rg "error"
```

Erwartet: Filter-Errors aus Task 2 sind weg.

- [ ] **Step 4: Commit**

```pwsh
git add IdealAkeWms/Filters/RequirePickingOrTrackingOrLeitstandAccessAttribute.cs `
        IdealAkeWms/Filters/RequirePickingOrLeitstandAccessAttribute.cs
git commit -m @'
feat(filters): add RequirePickingOrTrackingOrLeitstandAccess + RequirePickingOrLeitstandAccess

Spec 7.3. Phase 2 view-split permission filters:
- RequirePickingOrTrackingOrLeitstandAccess: ProductionOrdersController.Index
  (slim), replaces manual 3-check.
- RequirePickingOrLeitstandAccess: PickingLeitstandController class-level,
  so pure-leitstand users (no picking role) can still view the Leitstand page
  while action-level [RequireLeitstandAccess] guards bulk/release actions.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 8: Tests

**Files:**
- New: `IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs`
- New: `IdealAkeWms.Tests/Controllers/ProductionOrdersControllerSlimTests.cs`
- New: `IdealAkeWms.Tests/Filters/RequirePickingOrTrackingOrLeitstandAccessFilterTests.cs`
- New: `IdealAkeWms.Tests/Filters/RequirePickingOrLeitstandAccessFilterTests.cs`
- Delete: `IdealAkeWms.Tests/Controllers/ProductionOrdersControllerPickerTests.cs` (Inhalt nach PickingLeitstandControllerTests umgezogen)

- [ ] **Step 1: Pre-Inventar — alte Tests, die jetzt rot werden**

```pwsh
rg -n "ProductionOrderViewItem|ProductionOrderViewModel|ProductionOrdersController" IdealAkeWms.Tests/ | rg -v "\\.git"
```

Erwartet: Treffer in mehreren Test-Files. Jeder Treffer → Anpassung nötig (typischerweise: `ProductionOrderViewItem` → `ProductionOrderListItem` ODER `PickingLeitstandItem`, je nach Test-Kontext).

- [ ] **Step 2: `ProductionOrdersControllerPickerTests` → `PickingLeitstandControllerTests` umziehen**

```pwsh
git mv IdealAkeWms.Tests/Controllers/ProductionOrdersControllerPickerTests.cs `
       IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs
```

In der umbenannten Datei:
- Klassen-Name umbenennen auf `PickingLeitstandControllerTests`.
- Test-Targets umbenennen: `ProductionOrdersController` → `PickingLeitstandController`.
- Constructor-Aufrufe an die neue DI-Liste anpassen (siehe Task 2 Step 1 — 9 Deps).
- Wo Tests heute `productionOrder.IsReleasedForPicking = true` setzen, muss nach Phase 1 stattdessen eine `ProductionOrderPickingStatus`-Entity im InMemory-DB angelegt werden. **Achtung:** das ist Phase-1-Test-Migration; falls Phase 1 das nicht erledigte, sind diese Tests bereits in Phase 1 rot.

Stichprobenhaft drei Tests (Beispiele):

```csharp
[Fact]
public async Task ToggleRelease_LeitstandUser_PersistsRelease()
{
    using var ctx = TestDbContextFactory.Create();
    var order = new ProductionOrder { Id = 1, OrderNumber = "FA-001", ArticleNumber = "ART-1" };
    ctx.ProductionOrders.Add(order);
    ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus
    {
        ProductionOrderId = 1, IsReleasedForPicking = false
    });
    await ctx.SaveChangesAsync();

    var sut = CreateSut(ctx);
    var result = await sut.ToggleRelease(1, null, null);

    result.Should().BeOfType<RedirectToActionResult>();
    var ps = await ctx.ProductionOrderPickingStatuses.FirstAsync(p => p.ProductionOrderId == 1);
    ps.IsReleasedForPicking.Should().BeTrue();
}

[Fact]
public async Task Index_LoadsRichItems_WithPivotAndPickingStatus()
{
    using var ctx = TestDbContextFactory.Create();
    // Setup: 3 FAs mit PickingStatus + AssemblyGroups, eine released, eine mit HasGlass=true, …
    var sut = CreateSut(ctx);
    var result = await sut.Index(null, null, null, false);

    var vm = ((ViewResult)result).Model as PickingLeitstandViewModel;
    vm!.Items.Should().HaveCount(3);
    vm.Items.First(i => i.Id == 1).IsReleasedForPicking.Should().BeTrue();
    vm.Items.First(i => i.Id == 2).HasGlass.Should().BeTrue();
}
```

- [ ] **Step 3: `ProductionOrdersControllerSlimTests` anlegen**

```csharp
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class ProductionOrdersControllerSlimTests
{
    [Fact]
    public async Task Index_LoadsSlimItems_WithoutPickingStatus()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-001", IsDone = false });
        ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus
        {
            ProductionOrderId = 1, HasCoatingParts = true, IsCoatingDone = false
        });
        await ctx.SaveChangesAsync();

        var sut = CreateSut(ctx);
        var result = await sut.Index(null, null, null, false);

        var vm = ((ViewResult)result).Model as ProductionOrderListViewModel;
        vm.Should().NotBeNull();
        vm!.Items.Should().HaveCount(1);

        var item = vm.Items.First();
        // ProductionOrderListItem hat ABSICHTLICH keine PickingStatus-Felder ausser Coating
        typeof(ProductionOrderListItem).GetProperty("PickingStatus").Should().BeNull();
        typeof(ProductionOrderListItem).GetProperty("IsReleasedForPicking").Should().BeNull();
        typeof(ProductionOrderListItem).GetProperty("HasGlass").Should().BeNull();
        item.HasCoatingParts.Should().BeTrue();
    }

    [Fact]
    public async Task Index_TrackingUser_SeesSlimList()
    {
        // Filter-Test ueber separates RequirePickingOrTrackingOrLeitstandAccessFilterTests abgedeckt.
        // Hier nur: Controller selbst macht keinen manuellen Permission-Check mehr.
    }

    // … weitere Slim-Tests …

    private ProductionOrdersController CreateSut(ApplicationDbContext ctx) { /* … */ }
}
```

- [ ] **Step 4: `RequirePickingOrTrackingOrLeitstandAccessFilterTests` anlegen**

```csharp
public class RequirePickingOrTrackingOrLeitstandAccessFilterTests
{
    [Fact]
    public async Task Filter_PickerOnly_AllowsAccess() { /* … */ }

    [Fact]
    public async Task Filter_TrackerOnly_AllowsAccess() { /* … */ }

    [Fact]
    public async Task Filter_LeitstandOnly_AllowsAccess() { /* … */ }

    [Fact]
    public async Task Filter_NoPermission_RedirectsToAccessDenied() { /* … */ }
}
```

Setup-Pattern: Mock `ICurrentUserService` mit jeweils einem `true`-Boolean, verify `next` ist called bzw. `context.Result` ist `RedirectToActionResult` mit `ActionName="AccessDenied"`. Vorbild: bestehende Filter-Tests im Repo (falls vorhanden — grep).

```pwsh
rg -l "RequirePickingOrTrackingAccessFilter" IdealAkeWms.Tests/
```

Falls ein Vorbild existiert → 1:1 Style übernehmen.

- [ ] **Step 5: `RequirePickingOrLeitstandAccessFilterTests` anlegen**

Analog Step 4, mit 3 Tests (Picker / Leitstand / None).

- [ ] **Step 6: Volle Test-Suite ausführen**

```pwsh
dotnet test --nologo --no-build --filter "Category!=SqlServerOnly"
```

Erwartet: alle grün. Falls existing Tests rot (z.B. die `ProductionOrdersControllerTests` ohne `Picker`-Suffix, die nach `ProductionOrderListItem`-Properties greifen) → individuell anpassen.

- [ ] **Step 7: Commit**

```pwsh
git add IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs `
        IdealAkeWms.Tests/Controllers/ProductionOrdersControllerSlimTests.cs `
        IdealAkeWms.Tests/Filters/RequirePickingOrTrackingOrLeitstandAccessFilterTests.cs `
        IdealAkeWms.Tests/Filters/RequirePickingOrLeitstandAccessFilterTests.cs
git rm IdealAkeWms.Tests/Controllers/ProductionOrdersControllerPickerTests.cs
git commit -m @'
test(picking): migrate picker tests to PickingLeitstandController + add slim tests

Spec 11. Phase 2 test migration:
- ProductionOrdersControllerPickerTests renamed/moved to PickingLeitstandControllerTests
  (5 actions migrated 1:1, setup adapted to ProductionOrderPickingStatus entities).
- New ProductionOrdersControllerSlimTests verifies slim Index returns
  ProductionOrderListViewModel without PickingStatus/AssemblyGroup mapping.
- New filter tests for the two new permission attributes.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 9: Doku + Version + TESTSZENARIEN

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`
- Modify: `IDEALAKEWMSService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`
- Modify: `CLAUDE.md`
- Modify: `docs/TESTSZENARIEN.md`

- [ ] **Step 1: AppVersion auf `1.12.0`**

In `IdealAkeWms/AppVersion.cs`:

```csharp
public static class AppVersion
{
    public const string Version = "1.12.0";
    public const string ReleaseDate = "2026-05-XX";  // Cutover-Datum eintragen
}
```

Gleiche Änderung in `IDEALAKEWMSService/AppVersion.cs`.

- [ ] **Step 2: `Changelog.cshtml` — neuer v1.12.0-Card**

Direkt unter dem v1.11.0-Card (Phase 1) einen neuen Card einfügen:

```razor
<div class="card mb-3">
    <div class="card-header bg-primary text-white">
        <strong>v1.12.0</strong> — 2026-05-XX
    </div>
    <div class="card-body">
        <h5>ProductionOrder View-Split (Phase 2)</h5>
        <ul>
            <li>
                <strong>Schlanke FA-Übersicht</strong>: <code>/ProductionOrders/Index</code> zeigt nur noch Sage-Master-Spalten
                (FA-Nr., Stk., Kunde, Artikel, Bezeichnungen, Termine, Werkbank). Picking-Status-Spalten (Glas/Zukauf/Lack-T,
                VK/VL/VE/VT/VA, Freigabe, Kommissionierer) wurden in die neue Leitstand-View verschoben.
            </li>
            <li>
                <strong>Neue Kommissionier-Leitstand-View</strong>: <code>/PickingLeitstand/Index</code> bündelt
                Bulk-Freigabe, Priorisierung und Picker-Zuweisung für Leitstand-User. Im Menü unter "Kommissionierung → Leitstand"
                erreichbar (sichtbar wenn <code>LeitstandAktiv=true</code>).
            </li>
            <li>
                Permission-Modell: <code>ProductionOrders/Index</code> ist jetzt mit dem neuen Filter
                <code>RequirePickingOrTrackingOrLeitstandAccess</code> geschützt; alle drei Personas sehen die Slim-Übersicht.
                Bulk-Freigabe, Priorisierung und Picker-Wechsel bleiben strikt unter <code>RequireLeitstandAccess</code>.
            </li>
            <li>
                Backward-Compat: Alte POST-Endpoints (z. B. <code>/ProductionOrders/BulkRelease</code>) leiten via 301 auf
                die neue Leitstand-Seite weiter. Lesezeichen auf die alte Index-URL führen automatisch auf die Slim-Variante.
            </li>
        </ul>
    </div>
</div>
```

- [ ] **Step 3: `Help/Index.cshtml` — Hinweis-Section**

In der Hilfe-Übersicht (Datei `IdealAkeWms/Views/Help/Index.cshtml`) nach dem bestehenden Abschnitt zur Kommissionierung einen neuen Abschnitt einfügen:

```razor
<section class="mt-4">
    <h3>Fertigungsaufträge vs. Kommissionier-Leitstand</h3>
    <p>
        Seit Version <strong>1.12.0</strong> sind die Fertigungsauftrags-Listen in zwei Ansichten getrennt:
    </p>
    <dl>
        <dt><a asp-controller="ProductionOrders" asp-action="Index">Fertigungsaufträge</a></dt>
        <dd>
            Schlanke Übersicht aller offenen FAs für Tracking, Kommissionierer und Leitstand-User. Zeigt nur Sage-Stammdaten
            (FA-Nr., Stk., Kunde, Artikel, Termine, Werkbank) — ohne Picking-Status, ohne Freigabe-Aktionen.
        </dd>
        <dt><a asp-controller="PickingLeitstand" asp-action="Index">Kommissionier-Leitstand</a></dt>
        <dd>
            Ausführliche Leitstand-Sicht mit Glas/Zukauf/Lack-T-Status, VK/VL/VE/VT/VA-Baugruppen, Freigabe-Workflow,
            Picker-Zuweisung und Bulk-Aktionen. Erreichbar im Menü unter "Kommissionierung → Leitstand", wenn das
            Feature <code>LeitstandAktiv</code> aktiviert ist.
        </dd>
    </dl>
</section>
```

- [ ] **Step 4: `CLAUDE.md` aktualisieren**

In der Tabelle "Zugriffsschutz" zwei neue Zeilen einfügen (alphabetische Reihenfolge — nach `RequirePickingOrStockAccess`):

```markdown
| `[RequirePickingOrLeitstandAccess]` | picking ODER leitstand | PickingLeitstandController (Klassenebene) |
| `[RequirePickingOrTrackingOrLeitstandAccess]` | picking ODER tracking ODER leitstand | ProductionOrdersController.Index |
```

Aktualisiere die `[RequireLeitstandAccess]`-Zeile (Spalte "Angewendet auf"):

```markdown
| `[RequireLeitstandAccess]` | admin, leitstand | PickingLeitstandController (ToggleRelease, BulkRelease, SetPriority, ChangeAssignedPicker) |
```

Im "Sonderfaelle"-Block den Eintrag "Leitstand Index-Action hat kein Filter-Attribut" **entfernen** (durch das neue Filter-Attribut obsolet).

Im Block "Bekannte Fallstricke" am Ende einen neuen Eintrag ergänzen:

```markdown
- **PickingLeitstand vs ProductionOrders (seit v1.12.0)**: `ProductionOrdersController.Index` ist die schlanke FA-Liste (Sage-Master only). Picking-Status (Glas/Zukauf/Lack-T, VK/VL/VE/VT/VA, Freigabe, Picker) gehört in `PickingLeitstandController.Index`. Bulk-/Release-/Priority-/ChangeAssignedPicker-Actions sind ausschliesslich im PickingLeitstandController.
```

- [ ] **Step 5: `docs/TESTSZENARIEN.md` — 6 neue Szenarien**

Die Spec-Sektion 13 liefert die exakten Texte. Nach dem heutigen TS-3-Block (FA-Liste / Leitstand) einfügen:

```markdown
### TS-3.X — Slim-Index laedt ohne Picking-Statusspalten

**Vorbedingungen:** User mit Rolle `tracking` (kein `picking`, kein `leitstand`). Mindestens 5 FAs in der DB.

**Schritte:**
1. Login als Tracking-User.
2. Nav-Bar: "Fertigungsaufträge" anklicken.

**Erwartet:**
- URL = `/ProductionOrders/Index`.
- Tabellenkopf zeigt: FA Nr., Stk., Kunde, Artikelnummer, Bezeichnung 1, Bezeichnung 2, Werkbank, Beschicht., BG-Termin, Komm., Fert.-Termin, Liefertermin (12 Spalten + ggf. Row-Actions mit OSEON-Link).
- KEINE Spalten: Lack-T, Glas, Zukauf, VK, VL, VE, VT, VA, Status, Freigabe, Kommissionierer.
- KEINE Bulk-Action-Bar oben.
- Beschichtungs-Termin-Spalte zeigt rote Schrift bei ueberfaelligen Lackier-FAs (Backward-Compat via HasCoatingParts).
```

Analog 5 weitere TS-3.X-Szenarien wie in Spec 13 (Rich-Leitstand-View, Permission-Boundary-Picker, Bulk-Release-Regression, Pref-Isolation, Leitstand-Only-User).

- [ ] **Step 6: Build + Tests final grün**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build --filter "Category!=SqlServerOnly"
```

Beide grün. Falls rot → Bugfix in den vorherigen Tasks.

- [ ] **Step 7: Commit**

```pwsh
git add IdealAkeWms/AppVersion.cs `
        IDEALAKEWMSService/AppVersion.cs `
        IdealAkeWms/Views/Help/Changelog.cshtml `
        IdealAkeWms/Views/Help/Index.cshtml `
        CLAUDE.md `
        docs/TESTSZENARIEN.md
git commit -m @'
docs: phase 2 v1.12.0 — changelog, help, claude.md, testszenarien

Spec 14. AppVersion bump 1.11.0 → 1.12.0. Changelog card v1.12.0 with
4 bullets covering slim/rich view split + permission filters + backward-compat.
Help/Index gets a new "Fertigungsauftraege vs Leitstand" section.
CLAUDE.md permission table extended by 2 filters, fallstrick "Leitstand Index
hat kein Filter-Attribut" removed (now obsolete), new fallstrick "PickingLeitstand
vs ProductionOrders" added.
TESTSZENARIEN.md: 6 new TS-3.X scenarios for slim/rich/permission/regression/prefs.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Manuelle End-to-End-Verifikation (vor Merge in main)

- [ ] **CI grün:** `dotnet build && dotnet test --filter "Category!=SqlServerOnly"`.
- [ ] **Stage-Smoke-Test** (Stage-DB mit > 200 FAs):
  - `/ProductionOrders/Index` als Tracking-User lädt < 1 s, 14 Spalten sichtbar.
  - `/PickingLeitstand/Index` als Leitstand-User lädt < 2 s, alle Toggles funktional.
  - Bulk-Release-Modal funktioniert wie vor Phase 2.
  - Pref-Reset auf `/PickingLeitstand` (Spalten ausblenden) → Persist → Reload zeigt persistierte Auswahl.
- [ ] **Browser-DevTools-Check (Stale-Tab-Sim):** POST auf `/ProductionOrders/BulkRelease` → 301 zu `/PickingLeitstand/Index`.
- [ ] **Permission-Matrix-Check** (manuell, 4 Test-Logins): Tracking only, Picking only, Leitstand only, Admin. Erwartete Nav-Items + Page-Access laut Spec 7 Tabelle.
- [ ] **OSEON-Reporting-Cross-Links:** Stichprobe — falls Sektion 12.7 aus Task-0-Verifikation Treffer hatte, alle manuell durchklicken.

---

## Self-Review — Spec-Sektion → Plan-Task-Mapping

Jede Spec-Sektion (3-16) → Task-Nummer-Mapping:

| Spec-Sektion | Inhalt | Task |
|---|---|---|
| 3 — Out-of-Scope | reine Doku | — (keine Tasks) |
| 4 — Architektur-Übersicht | Doku, Datenfluss | — |
| 5 — Controller-Wahl Option B | PickingLeitstandController-Begründung | **Task 2** |
| 6.1 — ProductionOrderListItem (slim) | neuer ViewModel | **Task 1, Steps 1 + 6** |
| 6.2 — PickingLeitstandItem (rich) | neuer ViewModel | **Task 1, Step 2** |
| 6.3 — alte ProductionOrderViewModel.cs löschen | File-Removal | **Task 1, Step 4** |
| 7.1 — heutige Permission-Logik | Doku | — |
| 7.2 — Permission-Tabelle nach Phase 2 | Filter-Attribut-Bindings | **Task 5, Step 4** (ProductionOrders) + **Task 2, Step 1** (PickingLeitstand) |
| 7.3 — `RequirePickingOrTrackingOrLeitstandAccess` Filter | neuer Filter | **Task 7, Step 1** |
| 7.3 + 10 — `RequirePickingOrLeitstandAccess` Filter | zweiter neuer Filter | **Task 7, Step 2** |
| 7.4 — CLAUDE.md-Aktualisierung | Berechtigungstabelle + Fallstrick | **Task 9, Step 4** |
| 8.1 — Slim Index.cshtml | Drops + Spalten-Reduktion | **Task 4, Steps 1-9** |
| 8.2 — neuer Views/PickingLeitstand/Index.cshtml | Rich-View | **Task 3, Steps 1-7** |
| 8.3 — Picking/Index unverändert | — | — |
| 8.4 — ColumnDefinitions Slim + PickingLeitstand | zwei ViewConfigs | **Task 1, Step 3** |
| 9.1 — Slim ProductionOrdersController | DI-Ausdünnung + Slim-Mapping | **Task 5, Steps 1-7** |
| 9.2 — neuer PickingLeitstandController | 5 Actions migrieren | **Task 2, Steps 1-7** |
| 9.3 — Redirect-Stubs | optional aber empfohlen | **Task 5, Step 3** |
| 10 — Nav-Bar Update | _Layout.cshtml | **Task 6, Steps 1-4** |
| 11.1-11.3 — Neue Tests (Controller + Filter) | 4 neue Test-Klassen | **Task 8, Steps 3-5** |
| 11.2 — Migrierte Picker-Tests | Datei umbenannt | **Task 8, Step 2** |
| 12 — Risiken | dokumentiert; Mitigations in den Tasks | s. Task 5 Step 3 (12.1), Task 9 Step 4 (12.2), Task 0 Step 6 (12.7) |
| 13 — TESTSZENARIEN | 6 neue Szenarien | **Task 9, Step 5** |
| 14 — Deploy + Versionierung | AppVersion + Changelog + Help + CLAUDE.md | **Task 9, Steps 1-5** |
| 15 — Code-Punkte-Referenz | reine Doku | — |
| 16 — Offene Entscheidungen | drei `**Open:**`-Marker | werden im Plan-Verlauf entschieden — siehe unten |

**Open Decisions remaining at plan-time:**

- **Open:** Picker-Worklist-Link im Dropdown bei reinem Leitstand-User. **Recommended default:** ausblenden via `@if (canPick) { … }` um den Kommissionierliste-Item, sodass reine Leitstand-User nur "Leitstand"-Eintrag im Dropdown sehen. Implementiert in **Task 6, Step 2**.
- **Open:** IsDone-Badge im Slim-Index. **Recommended default:** `table-secondary`-Klasse bleibt als Visual-Marker, keine Badge in der Status-Spalte (Spalte ist eh weg). Implementiert in **Task 4, Steps 3-4** (Status-TD entfernen, Row-Klasse bleibt).
- **Open:** Redirect-Stubs sofort mit-deployen vs. später. **Recommended default:** sofort mit-deployen. Implementiert in **Task 5, Step 3** mit TODO-Marker für v1.14.0-Cleanup.

**Reihenfolge ist wichtig:**

1. Task 0 zuerst — Pre-Conditions checken. Phase 1 muss in main sein.
2. Task 1 baut die neuen ViewModels + ColumnDefinitions. Build wird ROT nach Step 4 (alte Datei weg).
3. Task 7 baut die Permission-Filter — Voraussetzung für Tasks 2 + 5.
4. Task 2 legt den neuen Controller an. Build bleibt rot (View fehlt).
5. Task 3 legt die rich-View an. Build wird grün für PickingLeitstand-Stack.
6. Task 4 schreibt die slim-View neu. Build immer noch rot wegen Slim-Controller.
7. Task 5 macht den Slim-Controller fertig. **Erster Punkt, ab dem der gesamte Build grün ist.**
8. Task 6 aktualisiert die Nav-Bar.
9. Task 8 repariert + erweitert die Tests. **Erster grüner `dotnet test`.**
10. Task 9 Doku + Version + TESTSZENARIEN.

**Alternative Reihenfolge** (falls Agent lieber durchgehend grün baut): Task 0 → Task 7 → Task 2 (mit `// TODO: Filter-Attribut nach Task 7`-Kommentar wenn Task 7 noch nicht da) → Task 3 → Task 1 → Task 4 → Task 5 → Task 6 → Task 8 → Task 9. Plan-Texte sind reihenfolgeneutral — jeder Task hat seinen Files + Steps eigenständig.

**No-Placeholder-Check:** Keine TBDs außer den drei `**Open:**`-Markierungen, alle mit Recommended-Default versehen. Alle Code-Snippets vollständig.

**Commit-Frequency:** 9 Commits (einer pro Task außer Task 0). Kein Wartungsfenster, kein DB-Backup — reine App-Code-Reorganisation.

**Branch-Strategie:** Phase 2 setzt auf dem aus Phase 1 hervorgegangenen Branch fort (`refactor/fa-logic` nach Phase-1-Merge, oder neuer Sibling-Branch `refactor/production-order-split-phase-2`). Docs-Home bleibt der `refactor/production-order-split`-Worktree. Merge in `main` direkt nach Stage-Smoke; **kein Wartungsfenster nötig**.

---

**Hinweis:** Phase 3 (BDE-Leitstand-View) wird nach Phase-2-Live-Verifikation (5 Tage) als eigene Detail-Spec geschrieben. Phase 4 (FA-Vervollständigung) kann parallel zu Phase 2/3 starten — siehe Roadmap Sektion 11.

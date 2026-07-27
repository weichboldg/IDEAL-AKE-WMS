# ProductionOrder-Split — Phase 2 Leitstand-Kommissionierung-View extrahieren — Design Spec

**Datum:** 2026-05-12
**Branch:** `refactor/production-order-split` (Docs-Home-Branch; Implementation auf `refactor/fa-logic` als Fortsetzung nach Phase-1-Merge, oder neuer Sibling-Branch)
**Status:** Approved → Plan
**Phase:** Phase 2 von 5 (siehe Roadmap). AppVersion-Bump auf `1.12.0`.
**Roadmap-Referenz:** `docs/superpowers/specs/2026-05-12-production-order-split-roadmap.md`, Sektion 6.
**Phase-1-Referenz:** `docs/superpowers/specs/2026-05-12-production-order-split-phase-1-design.md` (Schema-Refactor, definiert `ProductionOrderPickingStatus` + Pivot-Aggregation, von der Phase 2 lebt).

---

## 1. Problemstellung

Mit Phase 1 ist das Schema atomar gesplittet (Sage-Master ↔ PickingStatus ↔ BdeStatus ↔ AssemblyGroups). Die View bleibt jedoch eine einzige `ProductionOrders/Index.cshtml` mit **bis zu 23 Spalten**, dreigeteiltem Permission-Check (`CanPick OR CanViewTracking OR CanManagePickingRelease`) und drei Use-Case-Personas, die sich gegenseitig den Bildschirm verstellen:

- **Tracking-User** (`tracking`-Rolle): braucht nur die FA-Übersicht (FA-Nr., Kunde, Artikel, Termine, Status) und schaut Begleitpapiere via OSEON. Die 8 Picking-Checkbox-Spalten + Picker-Spalte + Freigabe-Action sind irrelevant — laden aber Daten + Render-Zeit.
- **Leitstand-User** (`leitstand`-Rolle, oft kombiniert mit `picking`): nutzt die View aktiv für Bulk-Release, Priorisierung und Picker-Zuweisung. Braucht alle Statusspalten + Bulk-Actions.
- **Picker** (`picking`-Rolle): nutzt heute hauptsächlich `Picking/Index` (released-only Liste), und gelegentlich `ProductionOrders/Index` für Toggles + Stückliste-Einstieg.

**Folgen heute:**
- Sortier-/Filter-Logik räumt 23 Spalten — und jeder zusätzliche Status, der via Phase-1-Pivot ankommt (`IsApplicable`, `IsDoneBde`, …), zwingt zur Weiter-Verbreiterung.
- `data-view-key="ProductionOrders"` mischt Pref-Daten zwischen Personas — ein Tracking-User, der "Glas" / "VK" abschaltet, kann seine Auswahl bei späterem Rollenwechsel nicht differenziert wiederherstellen.
- Permission-Logik im Controller ist manuell (kein Filter-Attribut, drei separate `await _currentUserService.CanXxxAsync()`-Aufrufe), siehe CLAUDE.md-Fallstrick "Leitstand Index-Action hat kein Filter-Attribut".
- `ProductionOrdersController.Index` (Datei `IdealAkeWms/Controllers/ProductionOrdersController.cs:235-367` heute, nach Phase 1 ähnliche Zeilenzahl) lädt mit jeder Anfrage Pivot + PickingStatus-Dict für ALLE FAs — auch wenn der Anwender nur Sage-Master will.

**Ziel von Phase 2:**
- `ProductionOrders/Index` wird zur **schlanken FA-Liste** (Sage-Master + ProductionWorkplace + globaler IsDone). Keine Picking-Checkboxes, keine AssemblyGroup-Pivot-Spalten, keine Picker/Release-Spalten. Erreichbar für Picker, Tracker, Leitstand-User — alle drei sehen denselben minimalen Inhalt.
- `PickingLeitstand/Index` (neuer Controller, neue URL) wird zum **Kommissionier-Leitstand**: alle Status-Joins (Glas/Zukauf/Lack-T, VK/VL/VE/VT/VA, Status, Freigabe, Priorität, Picker) + Bulk-Release + Picker-Assign + Priorisierung. Erreichbar nur für `picking`/`leitstand`/`admin`.

## 2. Ziele

1. **View-Split** ohne Verlust von Funktionalität: jede heute existierende Bulk-/Toggle-/Picker-Aktion bleibt erreichbar — sie wandert nur aus `ProductionOrders/Index.cshtml` nach `PickingLeitstand/Index.cshtml`.
2. **ViewModel-Trennung**: schlankes `ProductionOrderListItem` (Sage-Master), reiches `PickingLeitstandItem` (mit Status-Pivot + Bulk-Felder). `ProductionOrderViewItem` aus Phase 1 wird in zwei Records aufgespalten und eliminiert.
3. **Neuer Controller `PickingLeitstandController`** mit klar abgegrenzten Permissions und eigener Route. Begründung siehe 5.
4. **Slim-Controller**: `ProductionOrdersController.Index` verliert Pivot-Lookups, PickingStatus-Lookups, Picker-DI, Bulk-/Release-Actions. Nur noch FA-Master + ProductionWorkplace + DMS-Links.
5. **Permission-Klarheit**: 
   - `ProductionOrdersController.Index`: `[RequirePickingOrTrackingAccess]` (heute manueller Drei-Check fällt weg; Leitstand-User sind eh `picking`-User in der Praxis — Edge-Case "pure leitstand ohne picking" wird über `[RequirePickingOrTrackingOrLeitstandAccess]` gehandhabt, siehe 7.3).
   - `PickingLeitstandController.*`: `[RequirePickingAccess]` als Class-Level + `[RequireLeitstandAccess]` auf den Bulk-/Release-/Priority-/Picker-Actions.
6. **Nav-Update**: "Kommissionier-Leitstand" als neuer Menü-Eintrag unter dem bestehenden "Kommissionierung"-Item, sichtbar nur bei `LeitstandAktiv=true` und `CanPickAsync()`.
7. **UserViewPreferences-Isolation**: neue `data-view-key="PickingLeitstand"` für die rich-View. `ProductionOrders`-Key bleibt mit reduziertem Column-Set (alte unbekannte Keys werden von [`column-preferences.js:65`](IdealAkeWms/wwwroot/js/column-preferences.js#L65) `mergeWithDefaults`-Funktion ohnehin silent ignoriert — keine Migration nötig).
8. **TESTSZENARIEN**: vier neue Szenarien (TS-3.x für slim-Index, TS-3.x für rich-Leitstand, TS-3.x für Permission-Boundary, TS-3.x für Pref-Isolation).
9. **Versionierung**: `1.11.0` (Phase 1) → `1.12.0`. Changelog-Card + Help/Index-Hinweis.

## 3. Out-of-Scope (Phase 2)

- **Schema-Änderungen** — Phase 1 hat alles gemacht. Phase 2 ist reine Code-/View-Reorganisation. Kein neues Repository, keine neue Migration, kein SQL-Skript.
- **BDE-Leitstand-View** — `IsDoneBde` bleibt **nicht** in der rich-Leitstand-View; das gehört in Phase 3 (`Bde/Leitstand`). Phase 2 zeigt im rich-View nur die Picking-bezogenen Status (Glas/Zukauf/Lack-T, IsCoatingDone) und die AssemblyGroup-Bools — KEIN `IsDoneBde`-Toggle.
- **Pref-Daten-Migration** — Roadmap 12.2 lässt die Migration "optional". Wir entscheiden uns gegen Migration: `mergeWithDefaults` ignoriert unbekannte Keys, User pflegen das neue View-Layout selbst.
- **Toggle-API-Änderungen** — die drei Endpoints aus Phase 1 (`/api/picking-status/toggle`, `/api/assembly-groups/toggle-applicable`, `/api/bde-status/toggle`) bleiben unverändert. Die rich-View ruft sie weiterhin via JS-Dispatcher.
- **Neue Rolle** — `fa_completion` kommt in Phase 4. Phase 2 nutzt `picking` + `leitstand` + `tracking`.
- **PickingController.Index** — die released-only Liste für die Picker-Persona bleibt unangetastet. Sie ist ein anderer Use-Case ("ich kommissioniere jetzt") als die Leitstand-Übersicht ("ich verteile Arbeit").

## 4. Architektur-Übersicht

```
Heute (nach Phase 1, vor Phase 2):
┌─────────────────────────────────────────────────────────────┐
│ ProductionOrdersController                                  │
│  ├── Index             — FA-Liste mit 23 Spalten, 3-Persona │
│  ├── ToggleRelease     — [RequireLeitstandAccess]           │
│  ├── BulkRelease       — [RequireLeitstandAccess]           │
│  ├── SetPriority       — [RequireLeitstandAccess]           │
│  └── ChangeAssignedPicker — [RequireLeitstandAccess]        │
└─────────────────────────────────────────────────────────────┘
ViewModel: ProductionOrderViewItem (40+ Properties)
View:      Views/ProductionOrders/Index.cshtml (780 Zeilen)
View-Key:  ProductionOrders

Nach Phase 2:
┌─────────────────────────────────────────────────────────────┐
│ ProductionOrdersController         [RequirePickingOrTrackingOrLeitstandAccess]
│  └── Index             — FA-Liste SLIM (12 Spalten, 1 View)│
└─────────────────────────────────────────────────────────────┘
ViewModel: ProductionOrderListItem (Sage-Master + Workplace + IsDone)
View:      Views/ProductionOrders/Index.cshtml (slim, ~200 Zeilen)
View-Key:  ProductionOrders

┌─────────────────────────────────────────────────────────────┐
│ PickingLeitstandController         [RequirePickingAccess]   │
│  ├── Index             — Rich Komm-Leitstand-Liste          │
│  ├── ToggleRelease     — [RequireLeitstandAccess]           │
│  ├── BulkRelease       — [RequireLeitstandAccess]           │
│  ├── SetPriority       — [RequireLeitstandAccess]           │
│  └── ChangeAssignedPicker — [RequireLeitstandAccess]        │
└─────────────────────────────────────────────────────────────┘
ViewModel: PickingLeitstandItem (Sage-Master + Pivot + PickingStatus + Picker)
View:      Views/PickingLeitstand/Index.cshtml (rich, ~720 Zeilen, von alter Index.cshtml geforkt)
View-Key:  PickingLeitstand
```

**Datenfluss (nach Phase 2):**

```
Slim ProductionOrders/Index:
  GET → ProductionOrdersController.Index
       → IProductionOrderRepository.GetAllOrderedAsync()
       → IEnaioDmsDocumentRepository.GetByOrderNumbersAsync(orderNumbers)
       → IAppSettingRepository (KommissionierTage, …)
       → Mapping zu ProductionOrderListItem
       → return View(vm: ProductionOrderListViewModel)

Rich PickingLeitstand/Index:
  GET → PickingLeitstandController.Index
       → IProductionOrderRepository.GetAllOrderedAsync()            (Master)
       → IProductionOrderPickingStatusRepository.GetByProductionOrderIdsAsync(ids)  (PS-Dict)
       → IProductionOrderAssemblyGroupRepository.GetIsApplicablePivotAsync(ids)     (Group-Dict)
       → IEnaioDmsDocumentRepository.GetByOrderNumbersAsync(orderNumbers)
       → IUserRepository.GetActivePickersAsync()                    (für Modals)
       → IAppSettingRepository (LeitstandAktiv, KommissionierungMitZuweisung, …)
       → Mapping zu PickingLeitstandItem (Master + Pivot + PS)
       → return View(vm: PickingLeitstandViewModel)
```

Die Pivot-/PickingStatus-Lookups bleiben dort, wo Phase 1 sie angelegt hat (siehe Phase-1-Spec 7.3 und 10.2) — sie werden physisch vom alten in den neuen Controller verschoben. Slim-Controller braucht sie nicht mehr.

## 5. Controller-Wahl — Option B (neuer `PickingLeitstandController`)

**Roadmap-Option A:** Existing `PickingController` um eine `Leitstand()`-Action erweitern. Routes: `/Picking/Leitstand`.
**Roadmap-Option B:** Neuer `PickingLeitstandController`. Routes: `/PickingLeitstand/Index`, `/PickingLeitstand/ToggleRelease`, …

**Entscheidung: Option B.**

| Kriterium | Option A | Option B (gewählt) |
|---|---|---|
| URL-Klarheit | `/Picking/Leitstand` — kollidiert mental mit `/Picking/Index` (Picker-Worklist) | `/PickingLeitstand/Index` — sauberer Pfad pro Persona |
| Permission-Granularität | `PickingController` hat heute teils `[RequirePickingAccess]` pro-Action — Leitstand-Actions müssten `[RequireLeitstandAccess]` mischen, was die DI-Konstruktor-Liste aufbläst (User-Repo, Picker-List, ein-Klick-Picker für Modal-Daten) | Neuer Controller hat klar genau die DI, die er braucht — keine Aufblähung von `PickingController` |
| Phase-3-Erweiterbarkeit | Wenn Phase 3 einen `BdeLeitstandController` einführt, ist die Asymmetrie "Picking-Leitstand als Action in PickingController vs. BDE-Leitstand als eigener Controller" ungut | Symmetrisch: `PickingLeitstandController` ↔ `BdeLeitstandController` |
| Code-Bewegungs-Volumen | Alle 4 Release-/Priority-/Picker-Actions müssen aus `ProductionOrdersController` in `PickingController` umziehen — gleich viel Aufwand wie B | Identisch: dieselben 4 Actions ziehen in den neuen Controller |
| Test-Klassen-Trennung | Neue Tests landen in `PickingControllerTests` (existiert bereits, ~500 Zeilen) — Test-Datei wird groß | Eigene `PickingLeitstandControllerTests` — kleinere Test-Datei |

**Folge-Entscheidungen:**
- Route-Prefix: keiner — Standard-MVC-Konvention `/PickingLeitstand/{action}`.
- View-Folder: `Views/PickingLeitstand/Index.cshtml`. Modals (`releaseModal`, `changePickerModal`, `bulkReleaseModal`) bleiben in derselben Datei (wie heute in `Views/ProductionOrders/Index.cshtml`).
- Class-Level-Attribut: `[RequirePickingAccess]` auf der Controller-Klasse (alle Actions brauchen mindestens Picking-Zugriff). Bulk-/Release-/Priority-/Picker-Actions haben zusätzlich `[RequireLeitstandAccess]` per Action.

## 6. ViewModels

### 6.1 `ProductionOrderListItem` (slim, neu)

Ersetzt das heutige `ProductionOrderViewItem` für die Slim-View. Wird neu angelegt in `IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs`.

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

    // Calculated dates (BusinessDayService output)
    public DateTime? KommissionierTermin { get; set; }
    public DateTime? VorkommissionierTermin { get; set; }
    public DateTime? BeschichtungTermin { get; set; }

    // Cross-cutting from PickingStatus — nur für die Beschichtungs-Termin-Auswertung
    // (slim view zeigt KEINE Checkbox, aber Beschichtungstermin-Anzeige hängt davon ab —
    //  Roadmap "Backward-Compat: leeres LackierteilKategorieName → Termin für ALLE")
    public bool HasCoatingParts { get; set; }
    public bool IsCoatingDone { get; set; }
}
```

**Wichtige Auslassungen** (im Vergleich zu Phase-1-`ProductionOrderViewItem`):
- `PickingStatus` (String) — Status-Badge wandert in die rich-View.
- `HasGlass`, `HasExternalPurchase` — Checkbox-Spalten weg.
- `HasCooling`, `HasFan`, `HasElectric`, `HasDoors`, `HasSuperstructure` — 5 AssemblyGroup-Pivot-Bools weg.
- `IsReleasedForPicking`, `PickingPriority`, `ReleasedAt`, `ReleasedBy` — Freigabe-Felder weg.
- `AssignedPickerId`, `AssignedPickerName` — Picker-Spalte weg.

**Warum `HasCoatingParts` + `IsCoatingDone` bleiben im Slim-ViewModel:** Die Beschichtungstermin-Anzeige (rote Schrift wenn überfällig) ist Teil der Sage-Master-Übersicht und gehört auch in die slim-View. Der Backward-Compat-Pfad (CLAUDE.md "Beschichtungstermin Backward-Compat") braucht beide Flags. Die slim-View rendert KEINE Checkbox dafür, aber die Termin-Spalte hängt davon ab.

### 6.2 `PickingLeitstandItem` (rich, neu)

Wandert physisch aus dem alten `ProductionOrderViewItem` in eine neue Datei `IdealAkeWms/Models/ViewModels/PickingLeitstandViewModel.cs`.

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
    // Sage-Master (gleiche Felder wie ProductionOrderListItem)
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

    // PickingStatus-Felder (aus ProductionOrderPickingStatus, via Phase-1-Repo)
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

    // AssemblyGroup-Pivot (5 Bools via Phase-1-Repo `GetIsApplicablePivotAsync`)
    public bool HasCooling { get; set; }       // VK
    public bool HasFan { get; set; }           // VL
    public bool HasElectric { get; set; }      // VE
    public bool HasDoors { get; set; }         // VT
    public bool HasSuperstructure { get; set; }// VA
}
```

**Entscheidung gegen `ProductionOrderListItem`-Inheritance:** wäre Code-Compaction, aber Razor-Views mit Subklasse-Casts werden unsauber. Lieber duplizierte Sage-Master-Felder in beiden Records. C# `record`/`class`-Pattern, kein Inheritance.

### 6.3 `ProductionOrderViewItem` und `ProductionOrderViewModel` — entfernen

Die alten Typen aus `IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs` sind nach Phase 2 nicht mehr referenziert. **Datei wird gelöscht.** Falls Tests darauf referenzieren: anpassen auf die neuen Records (siehe 11).

## 7. Permission-Modell

### 7.1 Heute (nach Phase 1)

`ProductionOrdersController.cs` (Phase-1-Stand):
- Klasse: kein Attribut.
- `Index`: kein Attribut, manueller Check `CanPickAsync || CanViewTrackingAsync || CanManagePickingReleaseAsync` mit Redirect zu AccessDenied.
- `ToggleRelease`, `BulkRelease`, `SetPriority`, `ChangeAssignedPicker`: `[RequireLeitstandAccess]`.

### 7.2 Nach Phase 2

**`ProductionOrdersController` (slim):**

| Action | Attribut | Wer hat Zugriff |
|---|---|---|
| `Index` | `[RequirePickingOrTrackingOrLeitstandAccess]` (neu, siehe 7.3) | admin, picking, tracking, leitstand |

**`PickingLeitstandController` (neu):**

| Action | Attribut | Wer hat Zugriff |
|---|---|---|
| Klassenebene | `[RequirePickingAccess]` | admin, picking |
| `Index` | erbt Klassenebene | admin, picking |
| `ToggleRelease` | `[RequireLeitstandAccess]` (überschreibt? siehe Hinweis) | admin, leitstand |
| `BulkRelease` | `[RequireLeitstandAccess]` | admin, leitstand |
| `SetPriority` | `[RequireLeitstandAccess]` | admin, leitstand |
| `ChangeAssignedPicker` | `[RequireLeitstandAccess]` | admin, leitstand |

**Wichtig — ASP.NET-Core-Filter-Reihenfolge:** Class-Level + Action-Level beide aktiv. Heißt: ein User, der `picking` aber NICHT `leitstand` hat, kann **`Index` sehen** (durchläuft nur Class-Level), **aber `ToggleRelease` nicht** (Action-Level lehnt ab). Das ist genau das gewollte Verhalten. Klassenebene `[RequirePickingAccess]` ist defensive Verstärkung — ein purer `leitstand`-User (ohne `picking`) sieht den Leitstand-Bereich nicht (Edge-Case selten in der Praxis, aber konsistent: wer keinen Picking-Bereich nutzen darf, soll auch nicht im Leitstand toggeln).

### 7.3 Neues Filter-Attribut: `RequirePickingOrTrackingOrLeitstandAccessAttribute`

Heute existiert `RequirePickingOrTrackingAccessAttribute`. Wir brauchen die Variante mit zusätzlichem Leitstand-Recht:

```csharp
// New: IdealAkeWms/Filters/RequirePickingOrTrackingOrLeitstandAccessAttribute.cs
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

**Begründung:** der heutige manuelle 3-fach-Check im `ProductionOrdersController.Index`-Action-Body wird durch dieses Filter-Attribut ersetzt. Reduziert Code, eliminiert den CLAUDE.md-Fallstrick "Leitstand Index-Action hat kein Filter-Attribut".

**Alternative überlegt + verworfen:** `[RequirePickingOrTrackingAccess]` (existiert bereits) plus separater inline-Check für Leitstand. Verworfen, weil das wieder einen manuellen Check produziert und einen Filter-Side-Effect ("warum sieht ein reiner Leitstand-User den Index?") versteckt.

### 7.4 CLAUDE.md-Aktualisierung

Tabelle "Zugriffsschutz" im CLAUDE.md (Datei `IdealAkeWms/CLAUDE.md` bzw. `/CLAUDE.md` im Repo-Root) bekommt einen neuen Zeile:

```markdown
| `[RequirePickingOrTrackingOrLeitstandAccess]` | picking ODER tracking ODER leitstand | ProductionOrdersController.Index |
```

Und der Fallstrick "Leitstand Index-Action hat kein Filter-Attribut" wird entfernt (er ist nach Phase 2 obsolet). Stattdessen neuer Eintrag:

```markdown
- **PickingLeitstand vs ProductionOrders**: `ProductionOrdersController.Index` ist die schlanke FA-Liste (Sage-Master only). Picking-Status (Glas/Zukauf/Lack-T, VK/VL/VE/VT/VA, Freigabe, Picker) gehört in `PickingLeitstandController.Index`. Bulk-/Release-/Priority-Actions sind ausschliesslich im PickingLeitstandController.
```

## 8. View-Files — was bleibt, was wandert, was wird neu

### 8.1 `Views/ProductionOrders/Index.cshtml` — wird **slim** umgeschrieben

Heute ~780 Zeilen. Nach Phase 2 ~200 Zeilen (Schätzung).

**Drops:**
- Bulk-Action-Bar (Z. 66-87 heute).
- `bulk-checkbox-col` und `bulk-row-checkbox` (Z. 93-98, 139-150 heute).
- Spalten: Lack-T (Z. 115), Glas (116), Zukauf (117), VK (118), VL (119), VE (120), VT (121), VA (122). Status (123) wandert ebenfalls — Sage-Master-IsDone-Badge bleibt minimal.
- Spalten: Freigabe (127), Kommissionierer (131).
- TD-Renderings 222-256 (8 Checkboxes), 257-277 (Status-Badge), 311-353 (Freigabe-TD), 354-363 (Picker-TD).
- Modals: `releaseModal`, `changePickerModal`, `bulkReleaseModal` (Z. 395-490 heute).
- Inline-JS: `toggle-field`-Dispatcher (Z. 547-568), `priority-input` (572-589), Picker-Modal-Handler (595-636), Bulk-Selection (638-779).
- ViewModel-Properties: alle Picking-/Picker-/Bulk-/Pivot-Bool-Felder.

**Bleibt:**
- Page-Header (Z. 17).
- Filter-Card mit FA/Artikel/Kunde/ShowDone (Z. 19-64).
- Table-Wrapper + `filterable-table` + Sort-Default `picking-date` (Z. 89-90).
- Spalten: Actions (Stückliste-Button, Z. 99-102), FA-Nr. (103), Stk. (104), Kunde (105), Artikelnummer (106), Description1/2 (107-108), Werkbank (109), Beschicht. (110), BG-Termin (111), Komm. (112), Fert.-Termin (113), Liefertermin (114), Row-Actions (124).
- Inline-Renderings für Datum-Formatierung + KW.
- enaio-DMS-Links für FA-Nr. (Z. 165-181).
- Vault-Link für Artikel (Z. 188-195).
- OSEON-Link in Row-Actions (Z. 279-286).
- `ToggleDone`-Button in Row-Actions (Z. 287-309).
- `column-config`-JSON ohne die 12 entfernten Keys.
- Script-Section: QR-Scanner-Inits bleiben (Z. 535-545); Toggle-/Bulk-/Modal-JS wird gestrichen.

**Resultat:**
- 12 Spalten: actions, order-number, quantity, customer, article-number, description1, description2, workbench, coating-date, bg-date, picking-date, production-date, delivery-date, row-actions.
- 1 Persona, klares Mental-Modell ("FA-Übersicht").

### 8.2 `Views/PickingLeitstand/Index.cshtml` — neu, von alter Index abgeleitet

Die rich-View ist im Kern eine Kopie der heutigen `Views/ProductionOrders/Index.cshtml` mit folgenden Anpassungen:

- `@model PickingLeitstandViewModel` statt `ProductionOrderViewModel`.
- `data-view-key="PickingLeitstand"` statt `ProductionOrders`.
- `asp-action`-Targets der Forms: `BulkRelease` → bleibt `BulkRelease`, aber Controller-Default ist jetzt `PickingLeitstand` (durch Razor-Routing im Verzeichnis-Pfad). **Wichtig:** `asp-controller="PickingLeitstand"` explizit setzen, sonst greift `asp-action="BulkRelease"` auf einen non-existenten `ProductionOrdersController.BulkRelease` zu, der nach Phase 2 weg ist.
- OSEON-Link in Row-Actions bleibt (Tracking-Sicht ist auch hier relevant für Leitstand).
- `ToggleDone`-Button in Row-Actions: zeigt auf `Picking.ToggleDone` (wie heute, nicht auf `PickingLeitstand.ToggleDone` — ToggleDone bleibt in `PickingController`, weil "FA als erledigt setzen" eine Picker-Aktion ist, nicht eine Leitstand-Aktion).
- Inline-Toggle-Dispatcher (`/api/picking-status/toggle` + `/api/assembly-groups/toggle-applicable`) bleibt exakt wie nach Phase 1 — keine Backend-Änderung.
- `view-config`-JSON: `{ "viewKey": "PickingLeitstand", "supportsReorder": true, "supportsSortDefault": true }`.
- `column-config`-JSON: 19+ Keys (FA-Nr., Stk., Kunde, Artikel, Bezeichn. 1/2, Werkbank, Beschicht., BG-Termin, Komm., Fert.-Termin, Liefertermin, Lack-T, Glas, Zukauf, VK, VL, VE, VT, VA, Status, Row-Actions; plus bulk-select + actions wenn `CanPick` und Leitstand/Picker-Spalten wenn `LeitstandAktiv + CanManagePickingRelease` / `PickerAssignmentEnabled`).

### 8.3 `Views/Picking/Index.cshtml` und `IndexDropdown.cshtml`

Bleiben unverändert. Sie sind die released-only Picker-Worklist, nicht der Leitstand-Überblick.

### 8.4 `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs`

Zwei Änderungen:

1. `public static readonly ViewConfig ProductionOrders` wird **drastisch reduziert** auf die 14 Slim-Keys (siehe 8.1).
2. **Neue `ViewConfig PickingLeitstand`** mit der Vollausstattung. Die bisherigen `ProductionOrders`-Keys (mit allen 8 Checkbox-Spalten + Picker + Release) wandern fast 1:1 in die neue ViewConfig.

```csharp
public static readonly ViewConfig ProductionOrders = new(
    "ProductionOrders", "Fertigungsauftraege (Slim)",
    SupportsReorder: true, SupportsSortDefault: true)
{
    Columns =
    [
        new ColumnDef("actions",       "",              Locked: true,  DefaultWidth: 40),  // CanPick only
        new ColumnDef("order-number",  "FA Nr.",        Locked: true,  DefaultWidth: 90),
        new ColumnDef("quantity",      "Stk.",          Locked: false, DefaultWidth: 55),
        new ColumnDef("customer",      "Kunde",         Locked: false),
        new ColumnDef("article-number","Artikelnummer", Locked: false),
        new ColumnDef("description1",  "Bezeichnung 1", Locked: false),
        new ColumnDef("description2",  "Bezeichnung 2", Locked: false),
        new ColumnDef("workbench",     "Werkbank",      Locked: false),
        new ColumnDef("coating-date",  "Beschicht.",    Locked: false),
        new ColumnDef("bg-date",       "BG-Termin",     Locked: false),
        new ColumnDef("picking-date",  "Komm.",         Locked: false),
        new ColumnDef("production-date","Fert.-Termin", Locked: false),
        new ColumnDef("delivery-date", "Liefertermin",  Locked: false),
        new ColumnDef("row-actions",   "",              Locked: true,  DefaultWidth: 80),
    ]
};

public static readonly ViewConfig PickingLeitstand = new(
    "PickingLeitstand", "Kommissionier-Leitstand",
    SupportsReorder: true, SupportsSortDefault: true)
{
    Columns =
    [
        new ColumnDef("bulk-select",   "",              Locked: true,  DefaultWidth: 32),  // LeitstandAktiv + CanManagePickingRelease
        new ColumnDef("actions",       "",              Locked: true,  DefaultWidth: 40),  // CanPick
        new ColumnDef("order-number",  "FA Nr.",        Locked: true,  DefaultWidth: 90),
        new ColumnDef("quantity",      "Stk.",          Locked: false, DefaultWidth: 55),
        new ColumnDef("customer",      "Kunde",         Locked: false),
        new ColumnDef("article-number","Artikelnummer", Locked: false),
        new ColumnDef("description1",  "Bezeichnung 1", Locked: false),
        new ColumnDef("description2",  "Bezeichnung 2", Locked: false),
        new ColumnDef("workbench",     "Werkbank",      Locked: false),
        new ColumnDef("coating-date",  "Beschicht.",    Locked: false),
        new ColumnDef("bg-date",       "BG-Termin",     Locked: false),
        new ColumnDef("picking-date",  "Komm.",         Locked: false),
        new ColumnDef("production-date","Fert.-Termin", Locked: false),
        new ColumnDef("delivery-date", "Liefertermin",  Locked: false),
        new ColumnDef("coating-part",  "Lack-T",        Locked: false, DefaultWidth: 55),
        new ColumnDef("glass",         "Glas",          Locked: false, DefaultWidth: 45),
        new ColumnDef("purchase",      "Zukauf",        Locked: false, DefaultWidth: 55),
        new ColumnDef("cooling",       "VK",            Locked: false, DefaultWidth: 40),
        new ColumnDef("fan",           "VL",            Locked: false, DefaultWidth: 40),
        new ColumnDef("electric",      "VE",            Locked: false, DefaultWidth: 40),
        new ColumnDef("doors",         "VT",            Locked: false, DefaultWidth: 40),
        new ColumnDef("superstructure","VA",            Locked: false, DefaultWidth: 40),
        new ColumnDef("status",        "Status",        Locked: false),
        new ColumnDef("row-actions",   "",              Locked: true,  DefaultWidth: 80),
        new ColumnDef("release",       "Freigabe",      Locked: false, DefaultWidth: 160), // LeitstandAktiv + CanManagePickingRelease
        new ColumnDef("picker",        "Kommissionierer", Locked: false),                   // PickerAssignmentEnabled
    ]
};
```

Und das `GetByViewKey`-Switch wird um `"PickingLeitstand" => PickingLeitstand` erweitert.

## 9. Controller-Code

### 9.1 `ProductionOrdersController.cs` (slim)

Nach Phase 2 enthält die Datei **nur noch** die `Index`-Action plus die zwei Redirect-Stubs am Ende. DI wird drastisch ausgedünnt:

```csharp
public class ProductionOrdersController : Controller
{
    private readonly IProductionOrderRepository _productionOrderRepository;
    private readonly IAppSettingRepository _settingRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly IBusinessDayService _businessDayService;
    private readonly IEnaioDmsDocumentRepository _enaioDmsDocumentRepository;
    // Removed: ICurrentUserService (Filter macht das jetzt)
    // Removed: IUserRepository (Picker-Modal wandert)
    // Removed: IProductionOrderPickingStatusRepository (wandert in PickingLeitstandController)
    // Removed: IProductionOrderAssemblyGroupRepository (dito)

    public ProductionOrdersController(
        IProductionOrderRepository productionOrderRepository,
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        IBusinessDayService businessDayService,
        IEnaioDmsDocumentRepository enaioDmsDocumentRepository)
    {
        _productionOrderRepository = productionOrderRepository;
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _businessDayService = businessDayService;
        _enaioDmsDocumentRepository = enaioDmsDocumentRepository;
    }

    [RequirePickingOrTrackingOrLeitstandAccess]
    public async Task<IActionResult> Index(
        string? filterOrderNumber, string? filterArticleNumber,
        string? filterCustomer, bool showDone = false)
    {
        var orders = await _productionOrderRepository.GetAllOrderedAsync();
        // … filters …
        // (Slim-Mapping ohne PickingStatus/Pivot — siehe Plan Task 5)
        return View(vm: new ProductionOrderListViewModel { Items = viewItems, … });
    }

    public IActionResult Bom(int id) => RedirectToActionPermanent("Bom", "Picking", new { id });
    public IActionResult Picking() => RedirectToActionPermanent("Index", "Picking");
}
```

**Wichtig — auch nach Phase 1 lädt der Controller noch `IProductionOrderPickingStatusRepository` für die `HasCoatingParts`/`IsCoatingDone`-Werte (für die Beschichtungstermin-Logik). Phase 2 muss das behalten** — die Slim-View braucht diese zwei Bool-Felder weiterhin (siehe 6.1 Begründung). Wir laden sie über einen reduzierten Lookup:

```csharp
var orderIds = orders.Select(o => o.Id).ToList();
var pickingStatuses = await _pickingStatusRepository
    .GetByProductionOrderIdsAsync(orderIds);  // Phase-1-Methode, liefert Dict<int, PickingStatus>
// in der Slim-View nur HasCoatingParts + IsCoatingDone aus dem Dict übernehmen
```

DI-Erweiterung dann:

```csharp
private readonly IProductionOrderPickingStatusRepository _pickingStatusRepository;
```

**Open:** Alternativ könnte `HasCoatingParts`/`IsCoatingDone` auch im Slim-Repo direkt mit-projiziert werden (z.B. `IProductionOrderRepository.GetAllWithCoatingFlagsAsync()`), aber das duplizierte Pfade. Empfehlung: schlanker Dict-Lookup wie oben.

### 9.2 `PickingLeitstandController.cs` (neu)

Wird neu angelegt unter `IdealAkeWms/Controllers/PickingLeitstandController.cs`. Inhaltlich Übernahme von:

- `ProductionOrdersController.Index` (Phase-1-Stand, mit Pivot-/PS-Lookups).
- `ProductionOrdersController.ToggleRelease`.
- `ProductionOrdersController.BulkRelease`.
- `ProductionOrdersController.SetPriority`.
- `ProductionOrdersController.ChangeAssignedPicker`.

Skelett:

```csharp
using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequirePickingAccess]
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

    public PickingLeitstandController( /* … 9 deps … */ ) { /* … */ }

    public async Task<IActionResult> Index(
        string? filterOrderNumber, string? filterArticleNumber,
        string? filterCustomer, bool showDone = false)
    {
        // … Rich-Mapping mit Pivot + PS-Dict (Plan Task 2) …
        return View(new PickingLeitstandViewModel { … });
    }

    [HttpPost] [ValidateAntiForgeryToken] [RequireLeitstandAccess]
    public async Task<IActionResult> ToggleRelease(int id, int? assignedPickerId, string? returnUrl) { /* … */ }

    [HttpPost] [ValidateAntiForgeryToken] [RequireLeitstandAccess]
    public async Task<IActionResult> BulkRelease(List<int> ids, bool release, int? assignedPickerId, string? returnUrl) { /* … */ }

    [HttpPost] [ValidateAntiForgeryToken] [RequireLeitstandAccess]
    public async Task<IActionResult> SetPriority(int id, int? priority) { /* … */ }

    [HttpPost] [ValidateAntiForgeryToken] [RequireLeitstandAccess]
    public async Task<IActionResult> ChangeAssignedPicker(int id, int assignedPickerId) { /* … */ }
}
```

**Action-Bodies:** Code wird aus dem Phase-1-`ProductionOrdersController` 1:1 übernommen, nur die Repository-Calls bleiben gleich. Der Phase-1-Refactor hat bereits dafür gesorgt, dass diese Actions auf `IProductionOrderPickingStatusRepository` zugreifen, nicht direkt auf `ProductionOrder.IsReleasedForPicking`. Daher minimaler Anpassungs-Aufwand bei der Übersiedelung.

### 9.3 Redirect-Stub für alte URLs

Optional, aber nutzerfreundlich: für 1-2 Releases lassen wir eine Redirect-Action im `ProductionOrdersController` stehen, die alte Bookmarks auf den slim-Index lenkt und Bulk-Release-/Priority-URLs auf den neuen Controller leitet. Dieser Stub ist nicht erforderlich (siehe Risiko 12.4), aber empfohlen.

```csharp
// Backward-compat redirects (entfernen nach 2 Releases, also v1.14.0)
[HttpPost] public IActionResult BulkRelease()  => RedirectToActionPermanent("Index", "PickingLeitstand");
[HttpPost] public IActionResult ToggleRelease() => RedirectToActionPermanent("Index", "PickingLeitstand");
[HttpPost] public IActionResult SetPriority()   => RedirectToActionPermanent("Index", "PickingLeitstand");
[HttpPost] public IActionResult ChangeAssignedPicker() => RedirectToActionPermanent("Index", "PickingLeitstand");
```

**Entscheidung:** Stubs werden im Phase-2-Deploy mitgegeben (defensive Hilfe für stale-form-Posts), TODO-Marker im Code für v1.14.0-Cleanup.

## 10. Nav-Bar

Im `_Layout.cshtml` heute (Zeile 65-94) ist die Nav-Logik:

```razor
@if (leitstandAktiv) {
    @if (canManagePickingRelease || canViewTracking) {
        <li><a asp-controller="ProductionOrders" asp-action="Index">Fertigungsauftraege</a></li>
    }
} else {
    @if (canPick || canViewTracking) {
        <li><a asp-controller="ProductionOrders" asp-action="Index">Fertigungsauftraege</a></li>
    }
}
@if (canPick) {
    <li><a asp-controller="Picking" asp-action="Index">Kommissionierung ...</a></li>
}
```

**Nach Phase 2:**

```razor
@* FA-Übersicht — slim, für Picker/Tracker/Leitstand sichtbar *@
@if (canPick || canViewTracking || canManagePickingRelease) {
    <li class="nav-item">
        <a class="nav-link" asp-controller="ProductionOrders" asp-action="Index">Fertigungsauftraege</a>
    </li>
}

@* Kommissionierung-Dropdown — Picker-Worklist + Leitstand *@
@if (canPick) {
    <li class="nav-item dropdown">
        <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown">
            Kommissionierung
            @if (leitstandAktiv && releasedPickingCount > 0) {
                <span class="badge rounded-pill" style="background-color: var(--ake-orange); font-size: 0.7em;">@releasedPickingCount</span>
            }
        </a>
        <ul class="dropdown-menu">
            <li><a class="dropdown-item" asp-controller="Picking" asp-action="Index">Kommissionierliste</a></li>
            @if (leitstandAktiv) {
                <li><a class="dropdown-item" asp-controller="PickingLeitstand" asp-action="Index">Leitstand</a></li>
            }
        </ul>
    </li>
}
```

**Wichtig:** wenn `leitstandAktiv=false`, gibt es keinen Leitstand-Link — die Settings-Seite-Toggle gibt dann sowohl Slim-Index als auch Picker-Worklist frei, aber keine Leitstand-Funktionen. Das `releasedPickingCount`-Badge bleibt auf dem Dropdown-Hauptlink (führt zum Picker-Worklist, wo die Anzahl auch passt).

**FA-Übersicht für Picker:** der Slim-Index ist nach Phase 2 auch für reine `picking`-User sichtbar. Das ist kein Regression — sie konnten `ProductionOrders/Index` schon heute sehen. Nach Phase 2 bekommen sie nur weniger Spalten und müssen für Toggles in den Leitstand-Link wechseln.

**Edge-Case "pure leitstand ohne picking":** User mit `leitstand` aber ohne `picking`. In der Praxis selten, aber:
- Sehen Slim-Index (`canManagePickingRelease=true` → erfüllt OR-Bedingung).
- Sehen **kein** Kommissionierung-Dropdown (`canPick=false`).
- → Kommen NICHT zum Leitstand. Das ist ein UX-Bug.

**Mitigation:** Nav-Bedingung für das Dropdown auf `canPick || canManagePickingRelease` erweitern. Wenn ein reiner Leitstand-User vorbeikommt, sieht er das Dropdown, sieht "Kommissionierliste" (greift dann ggf. mit AccessDenied), sieht "Leitstand" (greift, weil `RequirePickingAccess` Class-Level greift — er hat keine Picking-Rolle → AccessDenied). **Hmm.** Damit knallt der Class-Level-`[RequirePickingAccess]` auf dem `PickingLeitstandController`.

→ **Entscheidung:** Class-Level-Attribut auf `[RequirePickingOrLeitstandAccess]` ändern (neues Filter, erstellen). Dann darf ein reiner Leitstand-User den Controller-Index sehen — die Bulk-/Release-/Priority-Actions sind eh nur mit `[RequireLeitstandAccess]` geschützt, also kommt er da auch durch.

**Neues Filter `RequirePickingOrLeitstandAccessAttribute`:** analog zu `RequirePickingOrTrackingAccess`, mit Check `CanPickAsync || CanManagePickingReleaseAsync`. **Zwei neue Filter** in Phase 2 also:

1. `RequirePickingOrTrackingOrLeitstandAccess` (für `ProductionOrders.Index`).
2. `RequirePickingOrLeitstandAccess` (für `PickingLeitstand`-Klasse).

Beide werden in der CLAUDE.md-Berechtigungstabelle dokumentiert.

## 11. Tests

### 11.1 Neue Tests

- **`PickingLeitstandControllerTests`** (~150 Zeilen):
  - `Index_LoadsRichItems_WithPivotAndPickingStatus`
  - `Index_NoLeitstand_RendersWithoutBulkColumns`
  - `Index_PickerAssignment_RendersPickerColumn`
  - `ToggleRelease_LeitstandUser_PersistsRelease` (copy aus heutigen `ProductionOrdersControllerPickerTests`)
  - `BulkRelease_LeitstandUser_PersistsAll`
  - `SetPriority_LeitstandUser_PersistsPriority`
  - `ChangeAssignedPicker_LeitstandUser_PersistsPicker`

- **`ProductionOrdersControllerSlimTests`** (~80 Zeilen, ersetzt heutige `ProductionOrdersControllerTests`):
  - `Index_LoadsSlimItems_WithoutPickingStatus`
  - `Index_PickerUser_SeesSlimList`
  - `Index_TrackingUser_SeesSlimList`
  - `Index_LeitstandUser_SeesSlimList`
  - `Index_PureAccessDenied_RedirectsToAccessDenied`

- **Filter-Tests**:
  - `RequirePickingOrTrackingOrLeitstandAccessFilterTests` (4 Tests: picker only / tracker only / leitstand only / none).
  - `RequirePickingOrLeitstandAccessFilterTests` (3 Tests: picker / leitstand / none).

### 11.2 Migrierte Tests

- **`ProductionOrdersControllerPickerTests`** heute (`IdealAkeWms.Tests/Controllers/ProductionOrdersControllerPickerTests.cs`) → Inhalt wandert nach `PickingLeitstandControllerTests`. **Datei löschen**, neu anlegen unter neuem Namen.

### 11.3 Test-Abdeckung

Erwartung: alle heute existierenden Bulk-/Release-/Priority-Test-Szenarien gibt es nach Phase 2 weiterhin, nur in der neuen Test-Klasse. Tests sind 1:1-Migration.

## 12. Risiken

### 12.1 Stale Browser-Tabs treffen alte `/ProductionOrders/BulkRelease`-URL
**Mitigation:** Redirect-Stubs aus 9.3 fangen POST-Requests ab (`RedirectToActionPermanent` → 301 mit Body-Verlust, aber User wird auf neue Seite umgeleitet, kann Aktion wiederholen). Optional zusätzliches Logging im Stub, um Migrations-Wirkung zu beobachten.

### 12.2 Pref-Daten-Drift (Roadmap 12.2)
Heute gespeicherte `ProductionOrders`-Prefs enthalten Keys wie `release`, `picker`, `glass`, `coating-part`, `cooling` etc. Nach Phase 2 enthält die Slim-`ProductionOrders`-ViewConfig diese Keys nicht mehr. `mergeWithDefaults` ([`column-preferences.js:65`](IdealAkeWms/wwwroot/js/column-preferences.js#L65)) ignoriert unbekannte Keys → User sieht die slim-Defaults, die alten Keys verschwinden silent aus dem UI.

**Pref auf der neuen `PickingLeitstand`-View ist anfangs leer.** User sieht die Default-Reihenfolge + alle Spalten sichtbar. Wenn ein Leitstand-User bisher z. B. "Beschicht." ausgeblendet hatte, müsste er das in der neuen View erneut tun.

**Nicht migriert.** Begründung: die Mapping-Logik zwischen alten/neuen Pref-Daten wäre user-spezifisch (`UserViewPreferences`-Tabelle) und brauchte ein Migrations-Skript. Roadmap 12.2 erlaubt explizit "skip migration". Verifiziert durch TS-3.x.

### 12.3 Edge-Case "tracking-only User klickt auf Picking-Toggle"
Tracking-User sieht den Slim-Index. Er kann KEINE Toggles ausführen — die Slim-View hat keine. Klick auf "Stückliste"-Icon führt nach `PickingController.Bom`, dort schlägt `[RequirePickingAccess]` fehl → AccessDenied. Akzeptables Verhalten.

**Subtle:** der Slim-Index zeigt `actions`-Spalte mit Stückliste-Icon **nur wenn `CanPick=true`**. Ein reiner Tracking-User sieht das Icon gar nicht. Korrektes Verhalten.

### 12.4 Routing-Konflikt mit `RedirectToActionPermanent`-Stubs
ASP.NET-Core sortiert MVC-Routes; `[HttpPost]`-Stubs in `ProductionOrdersController` ohne `[ValidateAntiForgeryToken]` könnten als "weakened endpoint" CSRF-anfällig wirken. **Mitigation:** Stubs IGNORE Body und tun nur 301-Redirect → kein State-Change, kein CSRF-Vektor. Trotzdem `[ValidateAntiForgeryToken]` lassen (für Form-Posts ohnehin Pflicht); bei abgelehntem Token greift Default-Antiforgery-Handler.

### 12.5 Test-Setup-Drift
`ProductionOrdersControllerPickerTests` heute setzt `productionOrder.IsReleasedForPicking = true` direkt — was nach Phase 1 schon nicht mehr funktioniert (Phase 1 hat das in PickingStatus verschoben). Phase 2 verschiebt die Tests in einen anderen Controller, aber die Setup-Daten sind dieselben. **Verifizieren** dass nach Phase-1-Merge die Tests bereits PickingStatus-Entities anlegen.

### 12.6 Nav-Bar `releasedPickingCount` Performance
Aktuell ruft das Layout `await ProductionOrderRepository.GetReleasedForPickingCountAsync()` auf JEDE Page. Nach Phase 1 wandert diese Methode in `IProductionOrderPickingStatusRepository`. **Im Layout muss der DI-Typ angepasst werden** (siehe Plan Task 6 Step 2): `@inject IProductionOrderPickingStatusRepository PickingStatusRepository` statt `IProductionOrderRepository`. Das ist eine Phase-1-Aufgabe — Phase 2 nimmt es lediglich entgegen.

### 12.7 OSEON-Reporting-Cross-Links
Verify: keine Links aus `/OseonReporting/...` auf alte `/ProductionOrders/...`-URLs, die nach Phase 2 brechen.

**Verifikations-Query** (im Plan Task 0/Verify ausgeführt):
```pwsh
rg -n "asp-controller=\"ProductionOrders\"" IdealAkeWms/Views/
```

Erwartung: nur `_Layout.cshtml`-Eintrag (siehe 10). Falls weitere Links existieren, in Plan-Task umgemünzt.

### 12.8 Help/Index + Changelog
Der Hilfetext "Fertigungsaufträge" (heute in `Views/Help/Index.cshtml`, Sektion mit Beschreibung der FA-Liste) muss aufgesplittet werden: ein Abschnitt "FA-Übersicht (slim)" und ein neuer Abschnitt "Kommissionier-Leitstand (rich)". Changelog-Card `v1.12.0` mit zwei Bullets.

## 13. Manuelle Test-Szenarien (für TESTSZENARIEN.md)

Neue Einträge im TS-3-Block (Fertigungsaufträge / Leitstand):

### TS-3.x — Slim-Index lädt ohne Picking-Statusspalten
**Vorbedingungen:** User mit Rolle `tracking` (kein `picking`, kein `leitstand`). Mindestens 5 FAs in der DB.
**Schritte:**
1. Login als Tracking-User.
2. Nav-Bar: "Fertigungsaufträge" anklicken.
**Erwartet:**
- URL = `/ProductionOrders/Index`.
- Tabellenkopf zeigt: FA Nr., Stk., Kunde, Artikelnummer, Bezeichnung 1, Bezeichnung 2, Werkbank, Beschicht., BG-Termin, Komm., Fert.-Termin, Liefertermin (12 Spalten + ggf. Row-Actions mit OSEON-Link).
- KEINE Spalten: Lack-T, Glas, Zukauf, VK, VL, VE, VT, VA, Status, Freigabe, Kommissionierer.
- KEINE Bulk-Action-Bar oben.
- Beschichtungs-Termin-Spalte zeigt rote Schrift bei überfälligen Lackier-FAs (Backward-Compat via `HasCoatingParts`).

### TS-3.x — Rich-Leitstand-View lädt mit allen Status-Spalten
**Vorbedingungen:** User mit `picking` + `leitstand`. AppSetting `LeitstandAktiv=true`, `KommissionierungMitZuweisung=true`. Mindestens 3 FAs, eine davon released.
**Schritte:**
1. Login. Nav-Bar: "Kommissionierung" → "Leitstand".
**Erwartet:**
- URL = `/PickingLeitstand/Index`.
- Tabellenkopf zeigt alle 23+ Spalten inkl. bulk-select, Lack-T, Glas, Zukauf, VK/VL/VE/VT/VA, Status, Freigabe, Kommissionierer.
- Bulk-Action-Bar oberhalb der Tabelle (zunächst hidden, erscheint bei Auswahl).
- Released-FA zeigt grünen "freigegeben"-Button + Prio-Input + Picker-Name.
- Modal "Freigeben" öffnet sich beim Klick auf "Freigeben" mit Picker-Dropdown.

### TS-3.x — Permission-Boundary: Picker ohne Leitstand sieht keine Bulk-Actions
**Vorbedingungen:** User mit `picking` (KEIN `leitstand`). `LeitstandAktiv=true`.
**Schritte:**
1. Login als Picker.
2. Nav-Bar: "Kommissionierung" → "Leitstand" (Eintrag ist sichtbar, weil `canPick=true`).
**Erwartet:**
- URL = `/PickingLeitstand/Index` lädt erfolgreich (Class-Level-Filter erlaubt Picker).
- Spalten **ohne** Bulk-Select-Checkbox, **ohne** Freigabe-Spalte (im Code: `if (Model.LeitstandAktiv && Model.CanManagePickingRelease)`-Block rendert nicht).
- Toggle-Checkboxen (Glas, VK, …) sind funktional → Klick speichert via `/api/picking-status/toggle` bzw. `/api/assembly-groups/toggle-applicable`.
- Wenn Picker die URL `/PickingLeitstand/BulkRelease` POST-direkt aufruft (via Browser-DevTools): 302 zu AccessDenied (Action-Level `[RequireLeitstandAccess]` greift).

### TS-3.x — Bulk-Release auf Leitstand-View funktioniert (Phase-1-Regression)
**Vorbedingungen:** User mit `leitstand`. 5 unreleased FAs mit Artikelnummer.
**Schritte:**
1. Auf `/PickingLeitstand/Index` 3 FAs anhaken.
2. Bulk-Action-Bar: "Markierte freigeben".
3. (PickerAssignment aktiv) Modal: Picker auswählen, Submit.
**Erwartet:**
- 3 FAs in `ProductionOrderPickingStatus.IsReleasedForPicking=1`, Priorität aufsteigend gesetzt, `AssignedPickerName` gesetzt.
- TempData["SuccessMessage"] = "3 Aufträge freigegeben." sichtbar.
- Picker-Worklist `/Picking/Index` zeigt die 3 FAs.

### TS-3.x — Pref-Isolation zwischen Slim-Index und Leitstand-View
**Vorbedingungen:** User mit `picking`+`leitstand`.
**Schritte:**
1. Slim-Index `/ProductionOrders/Index`: Spalte "BG-Termin" ausblenden (über column-preferences-Dialog).
2. Leitstand-View `/PickingLeitstand/Index` öffnen: alle Spalten initial sichtbar.
3. Leitstand-View: Spalte "Glas" ausblenden.
4. Zurück zu Slim-Index: "BG-Termin" weiterhin ausgeblendet, sonst alle Spalten.
5. Wieder Leitstand: "Glas" weiterhin ausgeblendet.
**Erwartet:**
- Prefs für `ProductionOrders` und `PickingLeitstand` werden getrennt persistiert. DB-Check: `UserViewPreferences`-Zeilen für beide ViewKeys vorhanden.

### TS-3.x — Leitstand-User ohne Picking-Rolle sieht Slim-Index, kommt nicht in Leitstand
**Vorbedingungen:** User mit `leitstand` aber ohne `picking`. `LeitstandAktiv=true`.
**Schritte:**
1. Login. Nav-Bar erwarten.
**Erwartet:**
- "Fertigungsaufträge"-Link sichtbar (Slim-Index funktioniert).
- "Kommissionierung"-Dropdown sichtbar mit nur "Leitstand"-Eintrag (Picker-Worklist ohne `picking`-Rolle macht Sinn nicht sichtbar zu sein — TBC: oder zeigt sie ihn und AccessDenied beim Klick? Empfehlung: nicht anzeigen).
- Klick auf "Leitstand": URL `/PickingLeitstand/Index` lädt mit Bulk-Actions sichtbar (`CanManagePickingRelease=true`).

**Open:** TS-3.x letzter Punkt — Picker-Worklist im Dropdown ausblenden wenn User keine `picking`-Rolle hat. Plan-Task 6 verifiziert die Nav-Logik.

## 14. Deploy + Versionierung

- **AppVersion:** `IdealAkeWms/AppVersion.cs` und `IDEALAKEWMSService/AppVersion.cs` auf `1.12.0`, Datum `2026-05-12`.
- **Changelog:** `Views/Help/Changelog.cshtml` Card `v1.12.0` mit zwei Bullets:
  - "ProductionOrder View-Split (Phase 2): schlanke FA-Übersicht + Kommissionier-Leitstand-View."
  - "Neue Permission-Filter `RequirePickingOrTrackingOrLeitstandAccess` und `RequirePickingOrLeitstandAccess`."
- **Help/Index:** Hinweis-Section "Fertigungsaufträge vs Kommissionier-Leitstand" mit Erklärung der Trennung.
- **CLAUDE.md:**
  - Tabelle "Zugriffsschutz" um zwei Filter-Zeilen erweitert.
  - Fallstrick "Leitstand Index-Action hat kein Filter-Attribut" entfernen.
  - Neuer Fallstrick "PickingLeitstand vs ProductionOrders" hinzufügen.
- **TESTSZENARIEN.md:** 6 neue TS-3.x-Szenarien (siehe 13).

Kein Wartungsfenster, kein DB-Backup nötig — reine Code-/View-Reorganisation. Standard-Deploy.

## 15. Code-Punkte-Referenz

**Heutige Phase-1-Stände, die Phase 2 anfasst:**

- [ProductionOrdersController.cs:235-367](IdealAkeWms/Controllers/ProductionOrdersController.cs#L235-L367) — Index-Action (slim umbauen, Pivot/PS-Lookups wandern in PickingLeitstandController).
- [ProductionOrdersController.cs:39-233](IdealAkeWms/Controllers/ProductionOrdersController.cs#L39-L233) — ToggleRelease/BulkRelease/SetPriority/ChangeAssignedPicker (komplett wandern in PickingLeitstandController).
- [ProductionOrdersController.cs:372-374](IdealAkeWms/Controllers/ProductionOrdersController.cs#L372-L374) — Redirect-Stubs für `/ProductionOrders/Bom` und `/ProductionOrders/Picking` (bleiben).
- [Views/ProductionOrders/Index.cshtml:1-780](IdealAkeWms/Views/ProductionOrders/Index.cshtml#L1-L780) — Slim-Umbau (~580 Zeilen Diff).
- [Views/_Layout.cshtml:65-94](IdealAkeWms/Views/Shared/_Layout.cshtml#L65-L94) — Nav-Bar-Logik.
- [Models/ViewModels/ProductionOrderViewModel.cs:1-59](IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs#L1-L59) — wird durch zwei neue Dateien ersetzt.
- [Models/ViewModels/ColumnDefinitions.cs:19-53](IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs#L19-L53) — `ProductionOrders` slim + neuer `PickingLeitstand`.
- [Filters/RequirePickingOrTrackingAccessAttribute.cs](IdealAkeWms/Filters/RequirePickingOrTrackingAccessAttribute.cs) — Vorbild für die zwei neuen Filter.
- [Tests/Controllers/ProductionOrdersControllerPickerTests.cs](IdealAkeWms.Tests/Controllers/ProductionOrdersControllerPickerTests.cs) — wandert nach `PickingLeitstandControllerTests`.

## 16. Offene Entscheidungen

- **Open:** Picker-Worklist-Link im Dropdown bei reinem Leitstand-User ohne Picking-Rolle. **Recommended default:** ausblenden (Konsistenz mit anderen Nav-Items, die strikt nach Permissions filtern). Falls Stakeholder das anders will: zeigen mit AccessDenied-Folge.
- **Open:** Sollte der Slim-Index `IsDone`-Badge zeigen oder nicht? Aktuell zeigt der heutige Index die "Erledigt"-/"Offen"-Badges in der Status-Spalte. Nach Phase 2 ist die Status-Spalte weg, aber `IsDone` macht die Zeile `table-secondary` (grau). **Empfehlung:** weiterhin `table-secondary` als Visual-Marker (kein Badge nötig). `showDone`-Filter bleibt im Filter-Card.
- **Open:** Redirect-Stubs aus 9.3 — soll Phase 2 sie sofort mit-deployen oder erst bei Feedback "User trifft auf 404 nach Update"? **Recommended default:** sofort mit-deployen (3-Zeilen-Cost, hilft bei Stale-Tab-Fall).

Keine weiteren TBDs. Alle ViewModel-Felder, Permission-Filter und View-Splits sind eindeutig spezifiziert.

## 17. Self-Review — Spec-Sektion → Plan-Task-Mapping

| Spec-Sektion | Inhalt | Plan-Task |
|---|---|---|
| 5 — Controller-Wahl Option B | neuer PickingLeitstandController | **Task 2** |
| 6.1 — ProductionOrderListItem (slim) | neuer ViewModel | **Task 1, Step 1** |
| 6.2 — PickingLeitstandItem (rich) | neuer ViewModel | **Task 1, Step 2** |
| 6.3 — alte ProductionOrderViewModel.cs löschen | File-Removal | **Task 1, Step 3** |
| 7.3 — `RequirePickingOrTrackingOrLeitstandAccess` Filter | neuer Filter | **Task 7, Step 1** |
| 7.3 + 10 Edge-Case — `RequirePickingOrLeitstandAccess` Filter | zweiter neuer Filter | **Task 7, Step 2** |
| 8.1 — Slim Index.cshtml | Drops + Spalten-Reduktion | **Task 4** |
| 8.2 — neuer Views/PickingLeitstand/Index.cshtml | Rich-View | **Task 3** |
| 8.4 — ColumnDefinitions Slim + PickingLeitstand | zwei ViewConfigs | **Task 1, Step 4** |
| 9.1 — Slim ProductionOrdersController | DI-Ausdünnung + Slim-Mapping | **Task 5** |
| 9.2 — neuer PickingLeitstandController | 5 Actions migrieren | **Task 2** |
| 9.3 — Redirect-Stubs | optional aber empfohlen | **Task 5, Step 3** |
| 10 — Nav-Bar Update | _Layout.cshtml | **Task 6** |
| 11.1 — Neue Tests (Controller + Filter) | 3 neue Test-Klassen | **Task 8** |
| 11.2 — Migrierte Picker-Tests | Datei umbenannt | **Task 8, Step 2** |
| 13 — TESTSZENARIEN | 6 neue Szenarien | **Task 9, Step 4** |
| 14 — Versionierung + Doku | AppVersion + Changelog + Help + CLAUDE.md | **Task 9, Steps 1-3** |
| 16 — Open Decisions | drei `**Open:**`-Marker | werden im Plan-Verlauf entschieden |

**Coverage-Verifikation:** alle nicht-Out-of-Scope-Sektionen sind durch mindestens einen Plan-Task abgedeckt. Sektionen 3 (Out-of-Scope), 12 (Risiken) und 15 (Code-Punkte-Referenz) sind reine Doku, ohne separaten Task.

---

**Hinweis:** Phase 3 (BDE-Leitstand-View) wird nach Phase-2-Live-Verifikation (5 Tage) als eigene Detail-Spec geschrieben. Phase 4 (FA-Vervollständigung) kann parallel zu Phase 2/3 starten — siehe Roadmap Sektion 11.

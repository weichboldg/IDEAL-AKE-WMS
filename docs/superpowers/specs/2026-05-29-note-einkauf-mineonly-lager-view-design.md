# Notiz EK + MissingParts mineOnly-Default + MissingPartsLager-View — Design v1.19.0

**Datum:** 2026-05-29
**Status:** Draft
**Branch:** `bugfix/missingparts-include-pd`
**Worktree:** `.claude/worktrees/missingparts-include-pd`
**Version-Ziel:** v1.19.0 (gleiche Branch wie ShortageStatus-3-State + Bugfixes)
**Vorgaenger im Branch:** `5232b44 fix(view): Radios mutually exclusive + Soll=Ist-Modal respektiert ShortageStatus`

---

## 1. Ziel

Drei zusammenhaengende Erweiterungen am Lagerbestellungs-Modul, alle innerhalb des laufenden v1.19.0-Branches:

1. **Zweites Notiz-Feld "Notiz EK"** auf `WarehouseRequisitionItem` (NVARCHAR 500, nullable). UI-Label des bestehenden `Note`-Feldes wird zu "Notiz Lager". DB-Property `Note` bleibt im Code (Rename wuerde groesseren Diff verursachen, semantische Klarheit via XML-Doc).

2. **MissingParts-View (Werkbank-Sicht)** Default `mineOnly=true`. User ohne Workplace-Zuordnung sieht leere Liste mit Banner-Hinweis.

3. **Neue `MissingPartsLager`-View** als Klon der jetzigen MissingParts-View, ohne `mineOnly`-Param. Wird spaeter erweitert (Sage-Bestellauftrag, Massenaktionen, etc. — out-of-scope dieses Specs).

## 2. Scope

**In-Scope:**

- Datenmodell: `WarehouseRequisitionItem.NoteEinkauf` (string?, 500 chars)
- EF Migration + idempotentes SQL/66 + FreshInstall-Sync
- Repository (`CloseAsync`, `SaveProgressAsync`): neuer Param `itemNotesEinkauf`
- Repository (`GetMissingPartsAsync`): `MissingPartRow` um `NoteEinkauf` erweitert, Column-Filter um `NoteLager` + `NoteEinkauf`
- ViewModels: `WarehouseRequisitionDetailItemViewModel.NoteEinkauf`, `MissingPartRow.NoteEinkauf`, `MissingPartsListViewModel.HasNoWorkplaceMapping`
- `WarehousePickingController` (Close + SaveProgress + PrintAndClose): neuer Form-Param `notesEinkauf`
- `MissingPartsController.Index` Default `mineOnly = true`, ViewModel-Flag `HasNoWorkplaceMapping`
- Neuer `MissingPartsLagerController` 1:1-Klon ohne mineOnly
- View `Views/MissingPartsLager/Index.cshtml` (Klon von `MissingParts/Index.cshtml`)
- `Views/WarehousePicking/Details.cshtml`: Spalte "Notiz" → "Notiz Lager", neue Spalte "Notiz EK"
- `Views/WarehousePicking/Print.cshtml`: Header "Notiz" → "Notiz Lager", neue Spalte "Notiz EK"
- `Views/MissingParts/Index.cshtml` + `Views/MissingPartsLager/Index.cshtml`: zwei Notiz-Spalten + No-Workplace-Banner (nur Werkbank-View)
- `Views/Shared/_Layout.cshtml`: bestehender Eintrag "Fehlteile" → "Meine Fehlteile", neuer Eintrag "Fehlteile (Lager)"
- Tests fuer alle neuen Aspekte
- Doku: Changelog, PROJECT_STATUS, CLAUDE.md, TESTSZENARIEN — alle innerhalb v1.19.0

**Out-of-Scope:**

- Property-Rename `Note` → `NoteLager` im Code (separater Refactor, falls je noetig)
- Werkbank-Edit-View bekommt KEIN NoteEinkauf-Input (separater Workflow falls Werker EK-Notizen vorbefuellen sollen)
- Rollen-Konzept fuer EK-Notiz (User sagte explizit "Rollenkonzept gehen wir spaeter an")
- MissingPartsLager-Erweiterungen (Sage-Auftraege, Massenaktionen, etc.) — eigene Specs
- Versions-Bump (bleibt v1.19.0, Aenderung ist ungemerged Teil der gleichen Branch)

## 3. Entscheidungen aus Brainstorming

| Frage | Entscheidung |
|-------|--------------|
| Notiz EK Verantwortung | Lager fuellt aus, im Picking/Details. Werkbank-Edit bleibt unberuehrt. Rollen-Erweiterung kommt separat. |
| Notiz-Spalten in MissingParts | Zwei separate Spalten "Notiz Lager" + "Notiz EK", einzeln filterbar |
| Code-Property-Naming | `Note` bleibt, neues `NoteEinkauf`. UI-Label und XML-Doc kommentieren die Semantik. Kein Code-Rename. |
| User ohne Workplace-Mapping | Banner-Hinweis statt automatischem mineOnly-Fallback. User-Awareness vor Bequemlichkeit. |
| Lager-View | Eigener Controller + View. 1:1-Klon, kein gemeinsamer Code (vereinfacht spaetere Lager-Erweiterungen ohne Werkbank-Sicht zu beeinflussen). |
| Layout-Menue | Beide Eintraege unter "Lager" — "Meine Fehlteile" und "Fehlteile (Lager)" |

## 4. Datenmodell

### 4.1 WarehouseRequisitionItem

```csharp
// Bestehend — XML-Doc praeziser:
/// <summary>
/// Notiz vom Lagermitarbeiter zur Position (UI-Label "Notiz Lager" seit v1.19.0).
/// Wird auf dem Druck angezeigt.
/// </summary>
[StringLength(500)]
public string? Note { get; set; }

// NEU:
/// <summary>
/// Notiz fuer den Einkaeufer (z.B. Lieferanten-Hinweis bei endgueltigem Fehlteil).
/// Wird im Picking/Details vom Lagermitarbeiter gefuellt. Werkbank-Edit nicht beeinflusst.
/// </summary>
[StringLength(500)]
public string? NoteEinkauf { get; set; }
```

### 4.2 Migration

`SQL/66_AddNoteEinkaufToWarehouseRequisitionItems.sql` (idempotent):

```sql
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE Name = N'NoteEinkauf'
               AND Object_ID = Object_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    ALTER TABLE [dbo].[WarehouseRequisitionItems]
        ADD [NoteEinkauf] NVARCHAR(500) NULL;
END
GO
```

EF-Migration `AddNoteEinkaufToWarehouseRequisitionItems` analog. `00_FreshInstall.sql`:
- Tabellen-Definition bekommt `[NoteEinkauf] NVARCHAR(500) NULL,`
- `__EFMigrationsHistory`-Block bekommt dritten v1.19.0-Eintrag

Additive Migration — kein DROP, risikoarm.

## 5. Repository-API

### 5.1 CloseAsync + SaveProgressAsync

Neuer Param `IReadOnlyDictionary<int, string?> itemNotesEinkauf` direkt nach `itemNotes`:

```csharp
Task CloseAsync(int id,
    IReadOnlyDictionary<int, decimal> itemQuantitiesPicked,
    IReadOnlyDictionary<int, string?> itemNotes,
    IReadOnlyDictionary<int, string?> itemNotesEinkauf,        // NEU
    IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses,
    int closedByUserId, string user, string winUser, byte[] rowVersion);

Task SaveProgressAsync(int id,
    IReadOnlyDictionary<int, decimal?> itemQuantitiesPicked,
    IReadOnlyDictionary<int, string?> itemNotes,
    IReadOnlyDictionary<int, string?> itemNotesEinkauf,        // NEU
    IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses,
    string user, string winUser);
```

Implementierung: analog zu `itemNotes` — TryGetValue, IsNullOrWhiteSpace.Trim(), Audit-Felder bei Aenderung.

### 5.2 GetMissingPartsAsync

`MissingPartRow` bekommt zusaetzliche Property `NoteEinkauf`:

```csharp
public record MissingPartRow(
    int RequisitionId, int ItemId, int Position,
    string WorkplaceName, string ArticleNumber, string ArticleDescription,
    decimal QuantityRequested, decimal QuantityPicked, decimal QuantityMissing,
    string? Unit, string? Note,
    string CreatedBy, DateTime? ClosedAt, ShortageStatus Status,
    string? NoteEinkauf);   // NEU
```

`Select(i => new MissingPartRow(...))` befuellt das neue Feld aus `i.NoteEinkauf`.

Column-Filter erweitern: bestehender `Note`-Filter wird `NoteLager`-Filter (Filter-Key match auf den `data-col-key`-Wert in der View). Neuer `NoteEinkauf`-Filter analog:

```csharp
if (columnFilters.TryGetValue("NoteLager", out var nl) && !string.IsNullOrWhiteSpace(nl))
    q = ApplyMissingPartsTextFilter(q, nl, isArticleNumber: false, isDescription: false, isWorkplace: false, isNoteLager: true);
if (columnFilters.TryGetValue("NoteEinkauf", out var ne) && !string.IsNullOrWhiteSpace(ne))
    q = ApplyMissingPartsTextFilter(q, ne, isArticleNumber: false, isDescription: false, isWorkplace: false, isNoteEinkauf: true);
```

`ApplyMissingPartsTextFilter` bekommt zwei zusaetzliche bool-Flags `isNoteLager` und `isNoteEinkauf` und filtert entsprechend per `i.Note.Contains(...)` / `i.NoteEinkauf.Contains(...)`.

### 5.3 GetShortageCountsForUserAsync

**Unveraendert** — nur Counts, Notizen nicht relevant.

## 6. ViewModels

```csharp
// WarehouseRequisitionDetailItemViewModel
public record WarehouseRequisitionDetailItemViewModel(
    int Id, int Position, string ArticleNumber, string ArticleDescription, string? Unit,
    decimal QuantityRequested, decimal? QuantityPicked, string StorageLocations,
    string? Note = null,
    ShortageStatus ShortageStatus = ShortageStatus.None,
    string? NoteEinkauf = null);   // NEU

// MissingPartsListViewModel
public class MissingPartsListViewModel
{
    public List<MissingPartRow> Items { get; set; } = new();
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public int? WorkplaceFilter { get; set; }
    public bool MineOnly { get; set; }
    public PaginationState Pagination { get; set; } = new();
    public ShortageStatus ActiveTab { get; set; } = ShortageStatus.WillBeRestocked;
    public int WaitingTotalCount { get; set; }
    public int NoRestockTotalCount { get; set; }

    /// <summary>NEU: True wenn mineOnly aktiv UND User hat keine Workplace-Zuordnung.</summary>
    public bool HasNoWorkplaceMapping { get; set; }
}
```

## 7. Controller-Aenderungen

### 7.1 WarehousePickingController

- `Close`-Action: neuer Form-Param `[FromForm] string?[]? notesEinkauf`, mappt analog `notes` auf `Dictionary<int, string?>`
- `SaveProgress`-Action: analog
- `PrintAndClose`-Action: analog
- `Details`- und `Print`-Mapping: `i.NoteEinkauf` durchreichen in DetailItem-ViewModel

### 7.2 MissingPartsController.Index

```csharp
public async Task<IActionResult> Index(
    ShortageStatus tab = ShortageStatus.WillBeRestocked,
    int? workplaceId = null,
    bool mineOnly = true,    // GEAENDERT: Default jetzt true
    int page = 1, int? pageSize = null)
{
    // ... wie bisher ...

    // NEU: HasNoWorkplaceMapping berechnen
    bool hasNoWorkplaceMapping = false;
    if (mineOnly && userWorkplaceIds != null && userWorkplaceIds.Count == 0)
        hasNoWorkplaceMapping = true;

    var vm = new MissingPartsListViewModel
    {
        // ... bestehende Felder ...
        HasNoWorkplaceMapping = hasNoWorkplaceMapping
    };
    return View(vm);
}
```

**Wichtig:** Werkbank-Karten-Link in `WarehouseRequisitions/Index` setzt schon explizit `mineOnly=true` — funktioniert weiter.

### 7.3 MissingPartsLagerController (NEU)

```csharp
[RequireStockAccess]
public class MissingPartsLagerController : Controller
{
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly ICurrentUserService _user;

    public MissingPartsLagerController(
        IWarehouseRequisitionRepository repo,
        IProductionWorkplaceRepository workplaces,
        ICurrentUserService user)
    {
        _repo = repo; _workplaces = workplaces; _user = user;
    }

    public async Task<IActionResult> Index(
        ShortageStatus tab = ShortageStatus.WillBeRestocked,
        int? workplaceId = null,
        int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        if (tab == ShortageStatus.None) tab = ShortageStatus.WillBeRestocked;

        var userDefaultPageSize = await _user.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var columnFilters = IdealAkeWms.Services.ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);

        var (rows, total) = await _repo.GetMissingPartsAsync(
            tab, workplaceId, columnFilters,
            null, null, page, effectivePageSize);

        var waitingResult = await _repo.GetMissingPartsAsync(
            ShortageStatus.WillBeRestocked, workplaceId, null, null, null, 1, 1);
        var noRestockResult = await _repo.GetMissingPartsAsync(
            ShortageStatus.NoRestock, workplaceId, null, null, null, 1, 1);

        var vm = new MissingPartsListViewModel
        {
            Items = rows.ToList(),
            AvailableWorkplaces = (await _workplaces.GetAllAsync()).OrderBy(w => w.Name).ToList(),
            WorkplaceFilter = workplaceId,
            MineOnly = false,
            ActiveTab = tab,
            WaitingTotalCount = waitingResult.TotalCount,
            NoRestockTotalCount = noRestockResult.TotalCount,
            HasNoWorkplaceMapping = false,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = total
            }
        };
        return View(vm);
    }
}
```

**Kein `mineOnly`-Param.** Lager-User sieht immer alle Fehlteile.

## 8. Views

### 8.1 WarehousePicking/Details.cshtml

Spalten-Header: `<th>Notiz</th>` → `<th>Notiz Lager</th>`. Neue Spalte rechts daneben: `<th>Notiz EK</th>`.

Im editierbaren Modus pro Row:

```html
<td>
    @if (isEditable)
    {
        <input type="text" name="notes" value="@i.Note" maxlength="500"
               class="form-control form-control-sm note-input"
               placeholder="Notiz Lager (optional)" />
    }
    else { <small>@i.Note</small> }
</td>
<td>
    @if (isEditable)
    {
        <input type="text" name="notesEinkauf" value="@i.NoteEinkauf" maxlength="500"
               class="form-control form-control-sm note-einkauf-input"
               placeholder="Notiz EK (optional)" />
    }
    else { <small>@i.NoteEinkauf</small> }
</td>
```

**JS-Update**: `collectProgress()` sammelt zusaetzlich `notesEinkauf`. Note-EK-Input bekommt gleiche Autosave-on-Blur Behandlung wie note-input.

```javascript
function collectProgress() {
    // ... bestehende ...
    const notesEinkauf = Array.from(form.querySelectorAll('input[name="notesEinkauf"]')).map(i => i.value || '');
    return { itemIds, quantitiesPicked, notes, notesEinkauf, shortageStatuses };
}

async function saveProgress() {
    // ... bestehende ...
    notesEinkauf.forEach(v => body.append('notesEinkauf', v));
    // ...
}

// Note-Einkauf-Input dirty + blur-Save
document.querySelectorAll('.note-einkauf-input').forEach(inp => {
    inp.addEventListener('input', () => { dirty = true; });
    inp.addEventListener('blur', saveProgress);
});
```

### 8.2 WarehousePicking/Print.cshtml

Header `<th>Notiz</th>` → `<th>Notiz Lager</th>`. Neue Spalte `<th>Notiz EK</th>`. Im `<tbody>`-Loop nach `<td>@i.Note</td>` einfuegen: `<td>@i.NoteEinkauf</td>`.

### 8.3 MissingParts/Index.cshtml — 2 Notiz-Spalten + No-Workplace-Banner

Header:
- Bestehender `<th data-col-key="Note">Notiz</th>` → `<th data-col-key="NoteLager">Notiz Lager</th>`
- Neuer `<th data-col-key="NoteEinkauf">Notiz EK</th>` direkt daneben

Body:
- `<td>@row.Note</td>` bleibt (Wert kommt aus `row.Note`, Spalten-Key ist nur "NoteLager")
- Neu `<td>@row.NoteEinkauf</td>`

**No-Workplace-Banner** direkt nach Page-Header und vor Tabs:

```html
@if (Model.HasNoWorkplaceMapping)
{
    <div class="alert alert-info">
        Du hast keine Werkbank-Zuordnung. Diese Liste ist deshalb leer.
        Du kannst entweder den Toggle <em>"Nur meine Werkbaenke"</em> deaktivieren
        oder die Zuordnung in den Stammdaten pflegen.
    </div>
}
```

### 8.4 MissingPartsLager/Index.cshtml (NEU)

1:1-Klon von `MissingParts/Index.cshtml` mit folgenden Unterschieden:

- `ViewData["Title"] = "Fehlteile (Lager)";`
- Page-Header zeigt immer "Fehlteile (Lager)" (kein mineOnly-Switch)
- KEINE `<input type="checkbox" name="mineOnly" ...>`-Spalte in der Filter-Card
- Tab-Links: `asp-controller="MissingPartsLager"` statt `MissingParts`
- KEIN `HasNoWorkplaceMapping`-Banner (Default ist global, Banner waere irrelevant)
- KEIN `mineOnly`-Hidden-Input im Filter-Form

### 8.5 WarehouseRequisitions/Index.cshtml

**Unveraendert.** Werkbank-Karten-Links setzen `asp-route-mineOnly="true"` explizit — funktioniert weiter mit dem neuen Default (redundant, aber harmlos).

### 8.6 _Layout.cshtml

Bestehender Eintrag `<a class="dropdown-item" asp-controller="MissingParts" asp-action="Index">Fehlteile</a>` aendern zu `Meine Fehlteile`. Direkt darunter neuer Eintrag:

```html
<a class="dropdown-item" asp-controller="MissingPartsLager" asp-action="Index">Fehlteile (Lager)</a>
```

Beide Eintraege haben dieselben Rollen-Bedingungen.

## 9. Tests

### 9.1 Repository

- `CloseAsync_*` bestehende Tests: alle bekommen `new Dictionary<int, string?>()` als `itemNotesEinkauf`-Param (leeres Dict — verhalten unveraendert)
- `SaveProgressAsync_*` analog
- NEU: `CloseAsync_PersistsNoteEinkauf` — verifiziert dass NoteEinkauf gespeichert wird
- NEU: `SaveProgressAsync_PersistsNoteEinkauf_WithoutStatusChange` — analog SaveProgressAsync_PersistsQuantitiesNotesAndFlags_WithoutStatusChange
- NEU: `GetMissingPartsAsync_FiltersByNoteLager_Column` — Column-Filter `NoteLager`
- NEU: `GetMissingPartsAsync_FiltersByNoteEinkauf_Column` — Column-Filter `NoteEinkauf`

### 9.2 Controller

**WarehousePickingControllerTests:**
- bestehende `Close_*`, `SaveProgress_*`, `PrintAndClose_*` Tests bekommen `notesEinkauf: null` als zusaetzlichen Param
- NEU: `Close_BindsNotesEinkaufArray` analog `Close_BindsShortageStatusesIntArray`

**MissingPartsControllerTests:**
- `Index_NoMineOnly_PassesWorkplaceIdToRepoUnchanged` UMBENENNEN zu `Index_MineOnlyFalse_PassesWorkplaceIdToRepoUnchanged`, expliziter Aufruf mit `mineOnly: false`
- NEU: `Index_DefaultIsMineOnlyTrue` — verifiziert dass ohne expliziten Param `mineOnly` als true behandelt wird (z.B. via `_workplaces.GetByUserIdAsync` Verify)
- NEU: `Index_MineOnly_NoWorkplaceMapping_SetsHasNoWorkplaceMappingTrue` — Mock `GetByUserIdAsync` returns empty list, ViewModel.HasNoWorkplaceMapping == true
- bestehende mineOnly-Tests bleiben (verifizieren Inkonsistenz-Cases)

**MissingPartsLagerControllerTests (NEU):**
- `Index_DefaultReturnsViewModel`
- `Index_TabParam_PassedToRepo`
- `Index_DoesNotApplyMineOnly` (auch wenn User Workplaces hat — Lager-View ignoriert das)

**WarehouseRequisitionsControllerTests:** Werkbank-Karte-Tests unveraendert (4-Tuple-Counts, etc.).

### 9.3 Erwartete Test-Counts

Web: ~640 passed + 1 skipped (Baseline 631 + ~9 neue, einige bestehende umbenannt).
Service: 99 unveraendert.

## 10. TESTSZENARIEN

`docs/TESTSZENARIEN.md` Kapitel 33 um 3 Szenarien erweitern:

**33.11 Notiz EK persistiert** — Im Picking/Details "Notiz EK" ausfuellen, Speichern + Abschliessen, Bestellung neu oeffnen → Wert noch da.

**33.12 MissingParts default mineOnly** — Werkbank-User mit Workplace-Zuordnung + 1 NoRestock-Item ruft `/MissingParts` direkt auf (ohne URL-Param) → sieht nur eigenes Item. Toggle deaktivieren → sieht auch andere.

**33.13 No-Workplace-Banner** — Test-User OHNE Workplace-Zuordnung ruft `/MissingParts` auf → leere Liste mit Info-Banner.

**33.14 Fehlteile (Lager)-View** — Lager-User ruft `/MissingPartsLager` ueber Menue auf → globale Sicht, alle Werkbaenke, KEIN mineOnly-Toggle.

## 11. Doku

- `AppVersion.cs` (Web + Service) **bleibt 1.19.0** (Aenderung ist Teil des laufenden ungemergten Releases)
- `IdealAkeWms/Views/Help/Changelog.cshtml` — bestehende v1.19.0-Card um 2 Bullets erweitern:
  - "Neues Feld 'Notiz EK' pro Lagerbestellungs-Position fuer Hinweise an den Einkauf"
  - "Werkbank- und Lager-Sicht in der Fehlteile-Liste getrennt — Werkbank-Default zeigt nur eigene Werkbaenke, Lager-View zeigt alle"
- `PROJECT_STATUS.md` — bestehende v1.19.0-Sub-Task-Tabelle um Sub-Tasks 13-16 erweitern
- `docs/TESTSZENARIEN.md` — 4 neue Szenarien (33.11–33.14) ans Kapitel 33 anhaengen
- `CLAUDE.md` — neuer Fallstrick:
  - "MissingPartsController default mineOnly=true (seit v1.19.0): Werkbank-Sicht filtert per Default auf eigene Werkbaenke. Lager-Sicht laeuft ueber separaten Controller MissingPartsLagerController ohne mineOnly-Param. Layout-Menue zeigt beide als 'Meine Fehlteile' + 'Fehlteile (Lager)'."
  - "Note vs NoteEinkauf (seit v1.19.0): Property im Code heisst `Note`, UI-Label aber 'Notiz Lager'. Zweite Notiz fuer den Einkauf heisst sowohl im Code als auch im UI `NoteEinkauf` / 'Notiz EK'. Property-Rename Note→NoteLager bewusst NICHT durchgefuehrt (groesserer Diff, keine semantische Notwendigkeit fuer DB-Layer)."

## 12. Reihenfolge der Tasks (grob — Detail in writing-plans)

1. Pre-Flight Baseline (bestaetigt 631 Web + 99 Service)
2. WarehouseRequisitionItem.NoteEinkauf Property + Migration + SQL/66 + FreshInstall
3. ViewModels erweitern (DetailItem, MissingPartRow, ListVM-HasNoWorkplaceMapping)
4. Repository CloseAsync + SaveProgressAsync + Tests (TDD)
5. Repository GetMissingPartsAsync NoteEinkauf-Mapping + Column-Filter + Tests (TDD)
6. WarehousePickingController Close/SaveProgress/PrintAndClose notesEinkauf + Tests
7. MissingPartsController Default mineOnly=true + HasNoWorkplaceMapping + Tests
8. MissingPartsLagerController + View (NEU) + Tests
9. Details.cshtml UI: Spalte umbenennen + Notiz-EK-Input + JS
10. Print.cshtml: Spalten umbenennen + Notiz-EK-Spalte
11. MissingParts/Index.cshtml: 2 Notiz-Spalten + Banner
12. _Layout.cshtml: 2 Menue-Eintraege
13. Changelog + PROJECT_STATUS + CLAUDE.md + TESTSZENARIEN Kap 33.11–33.14
14. Final-Check Build + Tests
15. PAUSE fuer User-Bestaetigung vor Merge
16. Merge in main (nach Confirm)

## 13. Out-of-Scope (Future)

- Werkbank-Edit-View bekommt Notiz-EK-Input
- Rollen-Konzept: "einkauf"-Rolle, Pflicht-Felder, Workflow-Eskalation
- MissingPartsLager-Erweiterungen: Sage-Bestellauftrag erzeugen, Massenaktionen
- Performance: combined `GetMissingPartsAndCountsAsync`-Methode mit einer einzigen Query statt 3 Roundtrips

## 14. Risiken

- **Repository-Sig-Aenderung beruehrt viele Aufrufer**: bestehende Controller-Tests und v1.19.0-Tests muessen den neuen Param ergaenzen. Mechanische Migration.
- **JS-Form-Param-Reihenfolge**: alle 3 Controller-Actions (Close, SaveProgress, PrintAndClose) muessen denselben `notesEinkauf`-Form-Param verarbeiten. Beim manuellen Test verifizieren dass Werte korrekt an Repository ankommen.
- **CLAUDE.md-Hinweis zu Property-Naming**: zukuenftige Entwickler koennten verwirrt sein dass Property `Note` heisst, UI aber "Notiz Lager". Fallstrick ist dokumentiert.
- **MissingPartsLager-Klon-Wartung**: spaetere Code-Aenderungen in `MissingParts/Index.cshtml` muessten ggf. in `MissingPartsLager/Index.cshtml` mitnachgezogen werden. Bewusste Entscheidung (Future-Erweiterungen rechtfertigen die Trennung).

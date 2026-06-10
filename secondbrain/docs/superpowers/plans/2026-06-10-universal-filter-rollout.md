# Universal-Filter-Rollout v1.21.0 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Alle 15 verbleibenden Listen-Views bekommen funktionierende Spaltenfilter (14 Server-Side, 1 Client-Side) + CLAUDE.md-Pflichtregel "jede Tabelle muss filterbar sein".

**Architecture:** In-Memory-Filterung via `ColumnFilterHelper.Apply<T>` im Controller als Standard (13 Views), SQL-Level mit Expression-Trees fuer BdeBookings (unbegrenztes Wachstum), Client-Filter fuer die unpaginierte Tracking/ByWorkplace. Filter laeuft IMMER auf der gerenderten Repraesentation (ViewModel), Reihenfolge: alle Rows laden → ViewModel bauen → filtern → paginieren.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10, xUnit + FluentAssertions + Moq + EF InMemory, table-filter.js (bestehend, keine Aenderung).

**Spec:** [`secondbrain/docs/superpowers/specs/2026-06-10-universal-filter-rollout-design.md`](../specs/2026-06-10-universal-filter-rollout-design.md) (Commit `1a46c66`) — **Sektion 9 (Fallstricke) ist Pflichtlektuere fuer jeden Task.**

**Worktree:** `.claude/worktrees/missingparts-include-pd`, Branch `bugfix/missingparts-include-pd`. Alle Kommandos mit cwd = Worktree-Root.

---

## Zentrales Pattern (Referenz fuer alle In-Memory-Tasks)

Jeder In-Memory-View-Task folgt diesem Schema. Der Task nennt die view-spezifischen Details; das Schema hier ist die vollstaendige Anleitung.

### Controller-Seite

```csharp
// 1. using (falls fehlt):
using IdealAkeWms.Services;

// 2. Statisches ColumnMap als Klassen-Member (Col-Keys == data-col-key der View, lowercase-kebab):
private static readonly Dictionary<string, Func<RowType, string?>> ColumnMap = new()
{
    ["name"] = r => r.Name,
    ["code"] = r => r.Code,
    // ... eine Zeile pro filterbare Spalte; Getter liefert den GERENDERTEN Text
    // (z.B. Status-Badge-Text statt Enum-Wert, formatierte Daten, Join-Namen)
};

// 3. In der Index-Action — Reihenfolge: laden → (ViewModel bauen) → filtern → zaehlen → paginieren:
var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
var allRows = await _repo.GetAllAsync(...);                 // ALLE Rows, kein Skip/Take
// ggf. ViewModel-Projektion / Termin-Berechnung HIER (vor dem Filter!)
var filtered = ColumnFilterHelper.Apply(allRows, columnFilters, ColumnMap).ToList();
var totalCount = filtered.Count;
var pageRows = filtered.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();
```

### View-Seite

```html
<!-- Tabelle: -->
<table class="table table-striped filterable-table"
       data-view-key="<ViewKey>"
       data-server-column-filter="true">

<!-- Filterbare Spalten: -->
<th data-filterable data-col-key="name">Name</th>

<!-- Aktions-/Button-Spalten: NUR data-col-key, KEIN data-filterable: -->
<th data-col-key="actions"></th>
```

ALLE `<th>` brauchen `data-col-key` (CLAUDE.md-Pflicht, column-preferences). `table-filter.js` rendert die Filter-Row automatisch und navigiert zu `?colf_<key>=value` — keine JS-Aenderung.

### Controller-Test (pro View 1 Test, bestehendes Test-Pattern der jeweiligen Controller-Testklasse verwenden)

```csharp
[Fact]
public async Task Index_ColumnFilter_FiltersRows()
{
    // Arrange: Repo-Mock liefert 3 Rows mit unterscheidbaren Namen,
    // DefaultHttpContext mit QueryString "?colf_<key>=<wert-von-row-1>"
    var ctx = new DefaultHttpContext();
    ctx.Request.QueryString = new QueryString("?colf_name=alpha");
    ctrl.ControllerContext = new ControllerContext { HttpContext = ctx };

    // Act
    var result = await ctrl.Index();

    // Assert: ViewModel enthaelt nur die matchende Row, TotalCount == 1
}
```

### Verifikation pro Task

```
dotnet build IdealAkeWms.slnx 2>&1 | tail -3        → 0 Fehler
dotnet test IdealAkeWms.slnx --no-build 2>&1 | tail -4  → alle gruen
```

### Pflicht-Fallstricke (Spec Sektion 9)

- Filter auf ViewModel-/Render-Ebene, nie auf Roh-Entities mit anderem Anzeigetext (9.2)
- Count IMMER aus der gefilterten Menge (9.1)
- Max. 1 filterbare Tabelle pro gerenderter Seite (9.3)

---

## Task 0: Pre-Flight Baseline

**Files:** keine.

- [ ] **Step 1: Worktree clean + Build + Tests gruen**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
git status                                # clean, HEAD = 1a46c66 oder neuer
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
dotnet test IdealAkeWms.slnx --no-build 2>&1 | tail -4
```

Expected: 0 Fehler, 649 Web + 99 Service gruen. Diese Zahlen sind die Baseline.

---

## Task 1: WarehousePicking/Index (Referenz-Implementierung Gruppe B)

Erster Umbau — dient als Muster fuer Tasks 2-5. Sorgfaeltig arbeiten, die Folge-Tasks kopieren das Schema.

**Files:**
- Modify: `IdealAkeWms/Controllers/WarehousePickingController.cs` (Index-Action)
- Modify: `IdealAkeWms/Views/WarehousePicking/Index.cshtml`
- Test: `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs` (+1 Test)

- [ ] **Step 1: View lesen, Spalten + Render-Texte notieren**

`IdealAkeWms/Views/WarehousePicking/Index.cshtml` komplett lesen. Notieren: (a) alle `<th>` mit vorhandenen `data-col-key`-Werten (View hat schon filterable-table client-side), (b) was pro Spalte gerendert wird (Property, Format, Badge-Text). Auch den Controller `WarehousePickingController.Index` lesen: wie wird geladen/paginiert (GetForWarehouseAsync mit Status-Array), welcher Item-Typ traegt die Daten (ViewModel oder Entity).

- [ ] **Step 2: Failing Test schreiben**

In `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs` nach bestehendem Build-Pattern der Testklasse:

```csharp
[Fact]
public async Task Index_ColumnFilter_FiltersAcrossAllRows()
{
    // Repo-Mock: 3 Requisitions mit Werkbank-Namen "A1", "B2", "C3"
    // (Setup analog zu bestehenden Index-Tests der Klasse)
    var ctx = new DefaultHttpContext();
    ctx.Request.QueryString = new QueryString("?colf_workplace=A1");
    ctrl.ControllerContext = new ControllerContext { HttpContext = ctx };

    var result = await ctrl.Index();

    var vm = (result as ViewResult)!.Model as WarehouseRequisitionListViewModel;
    vm!.Items.Should().HaveCount(1);
    vm.Items[0].WorkplaceName.Should().Be("A1");
}
```

Col-Key `workplace` ggf. an den tatsaechlichen data-col-key der View anpassen (Step 1).

- [ ] **Step 3: Test laufen lassen — FAIL**

```bash
dotnet test IdealAkeWms.slnx --filter "FullyQualifiedName~WarehousePickingControllerTests.Index_ColumnFilter" 2>&1 | tail -3
```

Expected: FAIL (Filter wird noch nicht angewendet, 3 Items statt 1).

- [ ] **Step 4: Controller umbauen**

In `WarehousePickingController`:
1. ColumnMap als statisches Feld ergaenzen — ein Eintrag pro filterbarer Spalte der View, Getter auf dem Listen-Item-Typ, GERENDERTE Texte (z.B. Status als deutscher Badge-Text via switch, Datum als `dd.MM.yyyy HH:mm`):

```csharp
private static readonly Dictionary<string, Func<WarehouseRequisitionListItem, string?>> ColumnMap = new()
{
    // Keys = data-col-key aus der View (Step 1), Getter = gerenderter Text.
    // Beispiel-Eintraege — an tatsaechliche Spalten anpassen:
    ["id"] = r => r.Id.ToString(),
    ["workplace"] = r => r.WorkplaceName,
    ["status"] = r => r.Status switch
    {
        WarehouseRequisitionStatus.Submitted => "Abgeschickt",
        WarehouseRequisitionStatus.PartiallyDelivered => "Teilgeliefert",
        WarehouseRequisitionStatus.Closed => "Abgeschlossen",
        _ => r.Status.ToString()
    },
    ["submitted-at"] = r => r.SubmittedAt?.ToString("dd.MM.yyyy HH:mm"),
};
```

(Typ-Name `WarehouseRequisitionListItem` an den tatsaechlichen Item-Typ des ViewModels anpassen; Status-Texte exakt wie in der View gerendert.)

2. Index-Action: nach dem Laden der Rows (vor Skip/Take):

```csharp
var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
var filtered = ColumnFilterHelper.Apply(rows, columnFilters, ColumnMap).ToList();
var totalCount = filtered.Count;
var pageRows = filtered.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();
```

Wenn der Controller bisher SQL-paginiert (Repo mit Skip/Take): Repo-Aufruf auf `page=1, pageSize=int.MaxValue` umstellen (Pattern PickingLeitstand) ODER vorhandene Nicht-paginierte Repo-Methode nutzen. PaginationState.TotalCount aus `totalCount` speisen.

- [ ] **Step 5: Test laufen lassen — PASS**

```bash
dotnet test IdealAkeWms.slnx --filter "FullyQualifiedName~WarehousePickingControllerTests" 2>&1 | tail -3
```

Expected: alle gruen inkl. neuem Test.

- [ ] **Step 6: View umstellen**

In `Views/WarehousePicking/Index.cshtml` am `<table class="... filterable-table" ...>`-Tag ergaenzen: `data-server-column-filter="true"`. Pruefen dass alle filterbaren `<th>` `data-filterable` + `data-col-key` haben (hatte die View schon als Client-Filter — Keys muessen mit ColumnMap uebereinstimmen!). Aktions-Spalten behalten nur `data-col-key`.

- [ ] **Step 7: Vollsuite + Commit**

```bash
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
dotnet test IdealAkeWms.slnx --no-build 2>&1 | tail -4
git add IdealAkeWms/Controllers/WarehousePickingController.cs IdealAkeWms/Views/WarehousePicking/Index.cshtml IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs
git commit -m "feat(filter): WarehousePicking/Index Server-Side-Spaltenfilter"
```

---

## Task 2: WarehouseRequisitions/Index

**Files:**
- Modify: `IdealAkeWms/Controllers/WarehouseRequisitionsController.cs`
- Modify: `IdealAkeWms/Views/WarehouseRequisitions/Index.cshtml`
- Test: `IdealAkeWms.Tests/Controllers/WarehouseRequisitionsControllerTests.cs` (+1 Test)

- [ ] **Step 1:** View + Controller lesen (Spalten, data-col-keys, Lade-/Paginierungsart, Item-Typ). Beachte: View hat "Meine Fehlteile"-Karten mit Tab-Links — die Tabelle selbst ist eine einfache Bestell-Liste.
- [ ] **Step 2:** Failing Test analog Task 1 Step 2 (Filter auf eine eindeutige Spalte, z.B. Werkbank oder Status; QueryString `?colf_<key>=...`; Assert 1 von 3 Rows).
- [ ] **Step 3:** Test FAIL verifizieren.
- [ ] **Step 4:** Controller: ColumnMap (alle View-Spalten, gerenderte Texte — Status-Badges deutsch!) + Apply-vor-Pagination nach Zentral-Pattern. Falls SQL-paginiert: auf alle-Rows-laden umstellen.
- [ ] **Step 5:** Test PASS.
- [ ] **Step 6:** View: `data-server-column-filter="true"` ergaenzen, data-col-keys mit ColumnMap abgleichen.
- [ ] **Step 7:** Vollsuite gruen + Commit `feat(filter): WarehouseRequisitions/Index Server-Side-Spaltenfilter`.

---

## Task 3: Picking/Index (ACHTUNG Fallstrick 9.2)

**Files:**
- Modify: `IdealAkeWms/Controllers/PickingController.cs:75-120` (Index-Action)
- Modify: `IdealAkeWms/Views/Picking/Index.cshtml`
- Test: `IdealAkeWms.Tests/Controllers/PickingControllerTests.cs` (+1 Test)

**Spezialitaet:** Der Controller berechnet Kommissionier-Termine (BusinessDayService) heute NACH `Skip/Take` (Zeile ~102). Umstellen auf: alle `releasedOrders` → ViewModel-Liste MIT Terminen bauen → `ColumnFilterHelper.Apply` → `totalCount` → Skip/Take. Die Termin-Spalte rendert `dd.MM.yyyy KWxx` (lowercase Vergleich) — ColumnMap-Getter muss exakt dieses Format liefern (Helper `FormatDateForFilter` aus `PickingLeitstandController` als Vorlage, ggf. lokal kopieren).

- [ ] **Step 1:** View + Controller lesen. Spalten + bisherige data-col-keys notieren. Pruefen welcher Typ die Zeilen traegt (vermutlich ein PickingIndexItem-ViewModel mit KommissionierTermin).
- [ ] **Step 2:** Failing Test: Filter `?colf_<termin-key>=kw<NN>` ueber Mock-Daten mit ProductionDate so dass genau 1 Order in KW NN faellt; plus simpler Text-Filter-Test auf FA-Nummer.
- [ ] **Step 3:** FAIL verifizieren.
- [ ] **Step 4:** Controller: Reihenfolge umstellen (Termine fuer ALLE berechnen, dann filtern, dann paginieren), ColumnMap inkl. Termin-Getter im Format `dd.MM.yyyy KWxx` lowercase.
- [ ] **Step 5:** PASS.
- [ ] **Step 6:** View: server-flag + `data-date-filter` auf der Termin-Spalte (Kalender-Picker-Button).
- [ ] **Step 7:** Vollsuite + Commit `feat(filter): Picking/Index Server-Side-Spaltenfilter (Termine vor Pagination)`.

---

## Task 4: PartRequisitions/Index

**Files:**
- Modify: `IdealAkeWms/Controllers/PartRequisitionsController.cs`
- Modify: `IdealAkeWms/Views/PartRequisitions/Index.cshtml`
- Test: `IdealAkeWms.Tests/Controllers/PartRequisitionsControllerTests.cs` (+1 Test)

- [ ] **Steps 1-7:** identisches Schema wie Task 2 (lesen → failing Test → FAIL → ColumnMap + Apply → PASS → View-Flag → Vollsuite + Commit `feat(filter): PartRequisitions/Index Server-Side-Spaltenfilter`).

---

## Task 5: StorageLocations/Index

**Files:**
- Modify: `IdealAkeWms/Controllers/StorageLocationsController.cs:23-55`
- Modify: `IdealAkeWms/Views/StorageLocations/Index.cshtml`
- Test: `IdealAkeWms.Tests/Controllers/StorageLocationsControllerTests.cs` (+1 Test)

**Hinweis:** Controller paginiert bereits in-memory (Zeile ~53 `filtered.Skip(...).Take(...)`) und hat Top-Filter (showInactive/onlyBookable) — diese bleiben; `ColumnFilterHelper.Apply` wird NACH den bestehenden Top-Filtern und VOR Skip/Take eingeschoben. Bool-Spalten (Aktiv/Buchbar) rendern als Ja/Nein oder Badge — Getter liefert den gerenderten Text.

- [ ] **Steps 1-7:** Schema wie Task 2. Commit: `feat(filter): StorageLocations/Index Server-Side-Spaltenfilter`.

---

## Task 6: BdeBookings/Index (SQL-Level — Fallstricke 9.1 + 9.5)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IBdeBookingRepository.cs` (Signaturen GetHistoryAsync + GetHistoryCountAsync)
- Modify: `IdealAkeWms/Data/Repositories/BdeBookingRepository.cs`
- Modify: `IdealAkeWms/Controllers/BdeBookingsController.cs:40-130`
- Modify: `IdealAkeWms/Views/BdeBookings/Index.cshtml`
- Test: `IdealAkeWms.Tests/Repositories/BdeBookingRepositoryTests.cs` (+3 Tests) — falls Datei fehlt, neu anlegen nach Muster bestehender Repo-Tests (TestDbContextFactory)

- [ ] **Step 1: Repo + View lesen**

`GetHistoryAsync(skip, take, operatorId, workplaceId, from, to)` + `GetHistoryCountAsync(...)` Signaturen + Implementierung lesen. View-Spalten + data-col-keys + Render-Formate notieren (insb. Start/Ende-Timestamps — exaktes Format aus der View ablesen, z.B. `dd.MM.yyyy HH:mm`).

- [ ] **Step 2: Failing Repo-Tests schreiben (3 Stueck)**

In `BdeBookingRepositoryTests` (TestDbContextFactory.Create, 3 Buchungen mit Operator-Namen "Maier"/"Huber"/"Schulz" seeden):

```csharp
[Fact] public async Task GetHistory_ColumnFilter_Operator_Like() { /* colf operator=maier → 1 Row, Count == 1 */ }
[Fact] public async Task GetHistory_ColumnFilter_Negate() { /* "!maier" → 2 Rows, Count == 2 */ }
[Fact] public async Task GetHistory_ColumnFilter_Or() { /* "maier,huber" → 2 Rows, Count == 2 */ }
```

Jeder Test prueft BEIDE Methoden (Liste UND Count) — Fallstrick 9.1.

- [ ] **Step 3: FAIL verifizieren** (Compile-Error wegen neuem Parameter ist ok — dann erst Signatur, dann Test, dann rot).

- [ ] **Step 4: Repo implementieren**

Beide Methoden bekommen `IReadOnlyDictionary<string, string>? columnFilters = null`. Filter-Anwendung in einer gemeinsamen privaten Methode `ApplyHistoryColumnFilters(IQueryable<...>, columnFilters)`, die von Query UND Count aufgerufen wird. Text-Match mit dem **Expression-Tree-Pattern** — `BuildOrContains`/`BuildNullableOrContains` aus [WarehouseRequisitionRepository.cs](IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs) als Vorlage kopieren/adaptieren (NICHT EF.Functions.Like — Fallstrick 9.5). Datums-Spalten NICHT in SQL filtern: wenn ein Datums-Col-Key im Filter ist, behandelt der CONTROLLER das (Step 5).

- [ ] **Step 5: Controller umbauen**

`BdeBookingsController.Index`: `columnFilters` lesen, Datums-Keys (start/ende laut View) abspalten (Pattern PickingLeitstandController Z.60-72): Text-Filter → Repo; wenn Datums-Filter aktiv → Repo mit `skip=0, take=int.MaxValue` + Text-Filtern, dann in C# gegen das gerenderte Timestamp-Format matchen, dann `totalCount = gefilterte.Count` + Skip/Take in C#. Ohne Datums-Filter: Repo paginiert wie bisher in SQL, `GetHistoryCountAsync` MIT columnFilters.

- [ ] **Step 6: Tests PASS** (Repo-Tests + bestehende BdeBookings-Tests).

- [ ] **Step 7: View:** `data-server-column-filter="true"` + `data-date-filter` auf Timestamp-Spalten. data-col-keys mit Controller/Repo-Mapping abgleichen.

- [ ] **Step 8: Vollsuite + Commit** `feat(filter): BdeBookings SQL-Level-Spaltenfilter (Query+Count, Expression-Trees)`.

---

## Task 7: Users + Roles + Workstations (Gruppe C, Admin-Stammdaten)

**Files:**
- Modify: `IdealAkeWms/Controllers/UsersController.cs`, `RolesController.cs`, `WorkstationsController.cs` (Index-Actions)
- Modify: `IdealAkeWms/Views/Users/Index.cshtml`, `Views/Roles/Index.cshtml`, `Views/Workstations/Index.cshtml`
- Test: jeweilige Controller-Testklassen (+1 Test pro Controller)

Diese 3 Views haben heute KEINE filterable-table. Pro View:

- [ ] **Step 1:** View lesen — Spalten notieren. Kebab-case Col-Keys vergeben (z.B. `username`, `name`, `roles`, `active`).
- [ ] **Step 2-3:** Failing Test pro Controller (analog Zentral-Pattern, 3 Mock-Rows, `?colf_name=...`), FAIL.
- [ ] **Step 4:** Controller: ColumnMap + Apply vor dem bestehenden in-memory Skip/Take (Users paginiert bereits in C#, Zeile ~50). Rollen-Spalte in Users rendert vermutlich kommaseparierte Rollen-Namen — Getter entsprechend.
- [ ] **Step 5:** PASS.
- [ ] **Step 6:** View: Tabelle bekommt `class="... filterable-table" data-view-key="Users|Roles|Workstations" data-server-column-filter="true"`; ALLE th `data-col-key`, filterbare zusaetzlich `data-filterable`. Aktions-Spalte nur `data-col-key="actions"`.
- [ ] **Step 7:** Vollsuite + EIN Commit `feat(filter): Users+Roles+Workstations Server-Side-Spaltenfilter`.

---

## Task 8: ProductionWorkplaces + ArticleCategories + ArticleAttributes (Gruppe C)

**Files:**
- Modify: `IdealAkeWms/Controllers/ProductionWorkplacesController.cs`, `ArticleCategoriesController.cs`, `ArticleAttributesController.cs` (Index)
- Modify: zugehoerige `Views/<X>/Index.cshtml`
- Test: jeweilige Controller-Testklassen (+1 Test pro Controller)

- [ ] **Steps 1-7:** identisches Schema wie Task 7. Beachte ArticleAttributes: View hat Inline-Edit-Zeilen (seit v1.20.0 mit canEdit-Wrap) — nur die Anzeige-Spalten werden filterbar, Inline-Form-Spalten nicht. Commit: `feat(filter): Workplaces+ArticleCategories+ArticleAttributes Server-Side-Spaltenfilter`.

---

## Task 9: OrderRecipients + BdeMasterData (Gruppe C, Sonderfaelle)

**Files:**
- Modify: `IdealAkeWms/Controllers/OrderRecipientsController.cs` (Index), `BdeMasterDataController.cs` (Index)
- Modify: `IdealAkeWms/Views/OrderRecipients/Index.cshtml`, `Views/BdeMasterData/Index.cshtml`
- Test: jeweilige Controller-Testklassen (+1 Test OrderRecipients, +2 Tests BdeMasterData fuer 2 verschiedene Tabs)

**BdeMasterData-Spezialitaet (Fallstrick 9.4):** 3 server-seitige Tabs (operators/activities/terminals), Razor rendert nur den aktiven — also immer 1 Tabelle im DOM. ABER: pro Tab eine eigene ColumnMap im Controller (`OperatorColumnMap`, `ActivityColumnMap`, `TerminalColumnMap`) + eigener `data-view-key` pro Tab-Tabelle (`BdeMasterDataOperators`, `BdeMasterDataActivities`, `BdeMasterDataTerminals`). Die Index-Action waehlt die Map nach `tab`-Parameter. Der `?tab=`-Parameter bleibt bei Filter-Navigation automatisch erhalten.

- [ ] **Step 1:** Beide Views + Controller lesen (BdeMasterData: alle 3 Tab-Tabellen + wie der Controller pro Tab laedt/paginiert).
- [ ] **Step 2-3:** Failing Tests: OrderRecipients 1 Test; BdeMasterData 2 Tests (operators-Tab Filter auf Name, terminals-Tab Filter) — FAIL.
- [ ] **Step 4:** Controller: ColumnMaps + Apply pro Tab-Zweig (BdeMasterData) bzw. Standard (OrderRecipients).
- [ ] **Step 5:** PASS.
- [ ] **Step 6:** Views: alle 4 Tabellen (1 OrderRecipients + 3 BdeMasterData-Tabs) mit filterable-table + view-key + server-flag + th-Attribute.
- [ ] **Step 7:** Vollsuite + Commit `feat(filter): OrderRecipients+BdeMasterData Server-Side-Spaltenfilter (3 Tab-Maps)`.

---

## Task 10: Tracking/ByWorkplace (Client-Filter)

**Files:**
- Modify: `IdealAkeWms/Views/Tracking/ByWorkplace.cshtml` (NUR View)

- [ ] **Step 1:** View lesen — Tabellen-Struktur + Spalten.
- [ ] **Step 2:** Tabelle bekommt `class="... filterable-table"` (OHNE data-view-key wenn keine column-preferences gewuenscht — dann initialisiert table-filter.js sofort bei DOMContentLoaded; OHNE server-flag). Filterbare th: `data-filterable` + `data-col-key`.
- [ ] **Step 3:** Build gruen (Razor-Compile), Vollsuite gruen.
- [ ] **Step 4:** Commit `feat(filter): Tracking/ByWorkplace Client-Spaltenfilter (unpaginiert)`.

---

## Task 11: CLAUDE.md-Pflichtregel + Doku + Version

**Files:**
- Modify: `CLAUDE.md`
- Modify: `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs` (→ `1.21.0`, Datum aktuell)
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml` (v1.21.0-Karte UEBER v1.20.0)
- Modify: `PROJECT_STATUS.md` (v1.21.0-Block)
- Modify: `docs/TESTSZENARIEN.md` (neues Kapitel, naechste freie Nummer)

- [ ] **Step 1: CLAUDE.md — Listen-Views-Pflicht-Pattern ersetzen**

Den Spalten-Filter-Bullet im Block "Listen-Views — Pflicht-Pattern" ersetzen durch:

```markdown
  - **Spalten-Filter (PFLICHT fuer ALLE Tabellen, seit v1.21.0)**: Jede Tabelle in einer Listen-View MUSS filterbar sein. Tabellen **mit Pagination** → `data-server-column-filter="true"` + Controller-seitiges Filtern ueber alle Eintraege (in-memory via `ColumnFilterHelper.Apply<T>` als Default; SQL-Level-Filter im Repo nur bei unbegrenzt wachsenden Tabellen wie BdeBookings — dann MUSS der Count identisch mitfiltern). Tabellen **ohne Pagination** → mindestens `filterable-table` client-seitig. Filter laeuft IMMER auf der gerenderten Repraesentation (ViewModel-Text), nicht auf Roh-Entities. Maximal eine filterbare Tabelle pro gerenderter Seite (table-filter.js ist single-table; mehrere Tabellen → server-seitige Tabs wie BdeMasterData). Ausnahmen nur fuer Detail-/Edit-/Print-/Terminal-/Settings-Views und statische Übersichten (RoleOverview, Hilfe).
```

- [ ] **Step 2: CLAUDE.md — server-filter-Tabellen-Liste aktualisieren**

Im Abschnitt "Pagination & Server-Side Spaltenfilter" den Bullet "Aktuelle Liste der server-filter-Tabellen" ersetzen:

```markdown
- **Aktuelle Liste der server-filter-Tabellen (seit v1.21.0 vollstaendig)**: ProductionOrders, PickingLeitstand, StockOverview, StockMovements, Articles, FaCompletion, MissingParts, MissingPartsLager, SyncLog, Tracking (WMS), WarehousePicking, WarehouseRequisitions, Picking, PartRequisitions, StorageLocations, BdeBookings, Users, Roles, Workstations, ProductionWorkplaces, ArticleCategories, ArticleAttributes, OrderRecipients, BdeMasterData (3 Tab-Maps). Client-Filter (unpaginiert): Tracking/ByWorkplace, OrderRecipients/ArticleGroupMappings, OseonReporting, Picking/Bom.
```

- [ ] **Step 3: Changelog v1.21.0-Karte**

```html
<div class="card mb-3">
    <div class="card-header text-white" style="background-color: var(--ake-primary);">
        <strong>v1.21.0</strong> <span class="text-white-50 ms-2">[aktuelles Datum]</span>
    </div>
    <div class="card-body">
        <h6>Universal-Filter — alle Listen sind jetzt vollstaendig filterbar</h6>
        <ul>
            <li><strong>14 weitere Listen mit Server-Spaltenfiltern:</strong> Lager: Eingehende
                Listen, Meine Bestellungen, Kommissionierung, Bedarfsmeldungen, Lagerplaetze,
                BDE-Buchungen, Benutzer, Rollen, Arbeitsplaetze, Werkbaenke, Artikelkategorien,
                Artikelmerkmale, Empfaenger und BDE-Stammdaten filtern jetzt ueber ALLE
                Eintraege (nicht mehr nur die aktuelle Seite). Datumsspalten mit
                Kalender-/KW-Picker.</li>
            <li><strong>Teileverfolgung nach Werkbank</strong> bekommt Spaltenfilter
                (clientseitig, da ohne Seitenumbruch).</li>
            <li><strong>Einheitliches Verhalten:</strong> Damit sind saemtliche Tabellen der
                Anwendung filterbar — gleiche Filter-Syntax ueberall (OR mit Komma, NOT mit
                Ausrufezeichen).</li>
        </ul>
    </div>
</div>
```

- [ ] **Step 4: PROJECT_STATUS + TESTSZENARIEN**

PROJECT_STATUS: v1.21.0-Block (Stichworte: 14 Server-Filter-Views, 1 Client, CLAUDE.md-Regel). TESTSZENARIEN neues Kapitel mit den 5 Stichproben-Drehbuechern aus Spec Sektion 7.3 (ausformulieren: Vorbedingungen/Schritte/Erwartet — WarehousePicking Seite-2-Fund, Users Filter-Row, BdeBookings Datums-Picker + grosse Datenmenge, ByWorkplace Client, kombinierte Filter Reihenfolge-egal).

- [ ] **Step 5: Build + Commit** `docs+chore: v1.21.0 Version, Changelog, CLAUDE.md-Filterpflicht, TESTSZENARIEN`.

---

## Task 12: Final-Check

**Files:** keine.

- [ ] **Step 1: Vollsuite**

```bash
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
dotnet test IdealAkeWms.slnx --no-build 2>&1 | tail -4
```

Expected: 0 Fehler; Web-Tests = Baseline 649 + mind. 13 neue (1 je Task 1-5 = 5, 3 BdeBookings-Repo, 3 Task 7, 3 Task 8, 3 Task 9) → mind. 662; Service 99.

- [ ] **Step 2: Sanity-Scans**

```bash
# Alle 24 Server-Filter-Views haben das Flag:
grep -rl "data-server-column-filter" IdealAkeWms/Views/ | wc -l     # Expected: 24
# Keine paginierte Listen-View ohne filterable-table mehr (manuelle Pruefung der Inventur-Liste aus Spec Sektion 2)
grep -rL "filterable-table" IdealAkeWms/Views/Tracking/ByWorkplace.cshtml  # Expected: kein Treffer (= Datei HAT filterable-table)
```

- [ ] **Step 3: git log + status clean**

```bash
git log --oneline 1a46c66..HEAD     # ~12 Commits
git status                          # clean
```

---

## Task 13: PAUSE — User-Bestaetigung + Merge

**NICHT autonom ausfuehren.**

- [ ] **Step 1:** Status-Report an User (Commits, Test-Zahlen, manuelle Test-Drehbuecher).
- [ ] **Step 2:** Auf User-Bestaetigung warten (manuelle Tests der Filter in mehreren Views).
- [ ] **Step 3:** Nach Bestaetigung: Merge `bugfix/missingparts-include-pd` → main (`--no-ff`), enthaelt dann v1.19+v1.20+v1.21.
- [ ] **Step 4:** Worktree-Cleanup NUR nach expliziter User-Bestaetigung (Memory-Regel `feedback_worktree_cleanup_ask_first`).

---

## Self-Review (vom Plan-Autor)

**Spec-Coverage:**
- Spec 4.1 In-Memory-Pattern → Zentral-Pattern + Tasks 1-5, 7-9
- Spec 4.2 BdeBookings SQL → Task 6 (Count mitfiltern 9.1, Expression-Trees 9.5, Datums-C#-Pfad)
- Spec 4.3 ByWorkplace Client → Task 10
- Spec 4.4 View-Konventionen → Zentral-Pattern View-Seite
- Spec 5 alle 15 Views → Tasks 1-10 (6 B-Views: T1-T6; 8 C-Views: T7-T9; ByWorkplace: T10)
- Spec 6 CLAUDE.md-Regel → Task 11 Step 1+2
- Spec 7 Tests → TDD-Steps in jedem Task + Task 12 Aggregat
- Spec 8 Doku → Task 11
- Spec 9 Fallstricke → in Task 3 (9.2), Task 6 (9.1/9.5), Task 9 (9.4), Zentral-Pattern (9.2/9.3)

**Bewusste Abweichung vom No-Placeholder-Gebot:** Die konkreten Spalten/Col-Keys pro View werden in Step 1 jedes Tasks aus der View abgelesen statt im Plan vorab gelistet (15 Views x Spalten waere fehleranfaellige Duplikation; die Views sind die Source of Truth). Das Code-Template + das voll ausgearbeitete Beispiel (Task 1) definieren das Vorgehen vollstaendig.

**Typ-Konsistenz:** `ColumnFilterHelper.Apply<T>(source, filters, IReadOnlyDictionary<string, Func<T,string?>>)` — Signatur verifiziert (ColumnFilterHelper.cs Z.71-74). `ReadFromQuery(HttpRequest?)` verifiziert (Z.21).

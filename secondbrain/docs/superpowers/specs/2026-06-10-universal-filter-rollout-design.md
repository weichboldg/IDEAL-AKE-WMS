# Universal-Filter-Rollout v1.21.0 — Design

**Status:** Spec / Approved
**Datum:** 2026-06-10
**Branch:** `bugfix/missingparts-include-pd` (Fortsetzung im bestehenden Worktree, User-Entscheidung)
**Vorgaenger-Specs:** [2026-06-03-finegrained-permissions-design.md](2026-06-03-finegrained-permissions-design.md)

---

## 1. Ziel

Alle Listen-Views mit Tabellen bekommen funktionierende Spaltenfilter — einheitliches Verhalten in der gesamten Anwendung:

1. **14 paginierte Listen-Views** auf Server-Side-Spaltenfilter umstellen (Filter wirkt ueber ALLE Eintraege, nicht nur die aktuelle Seite)
2. **1 unpaginierte View** (Tracking/ByWorkplace) bekommt Client-Filter
3. **CLAUDE.md-Pflichtregel**: jede Tabelle in einer Listen-View MUSS filterbar sein

## 2. Kontext / Ausloeser

Der Leitstand-Bug (Filter nach KW in der Zukunft auf Seite 1 liefert kein Ergebnis, obwohl der Eintrag auf Seite 3 existiert) zeigte das Grundproblem: Client-Side-Filter (`filterable-table` ohne `data-server-column-filter`) filtern nur die DOM-Rows der aktuellen Seite. Eintraege auf anderen Seiten sind unsichtbar fuer den Filter.

### Inventur (Stand 2026-06-10, nach v1.20.0)

**A) Bereits Server-Side (10 Views, Gold-Standard):**
Articles, FaCompletion, MissingParts, MissingPartsLager, PickingLeitstand, ProductionOrders, StockMovements, StockOverview, SyncLog, Tracking/Index.

**B) Client-Side-Filter + Pagination (6 Views — BUG-Potenzial):**
WarehousePicking/Index, WarehouseRequisitions/Index, Picking/Index, PartRequisitions/Index, StorageLocations/Index, BdeBookings/Index.

**C) Gar kein Spalten-Filter (9 echte Listen):**
Users, Roles, Workstations, ProductionWorkplaces, ArticleCategories, ArticleAttributes, OrderRecipients, BdeMasterData (alle paginiert) + Tracking/ByWorkplace (unpaginiert).

**Unveraendert (Client-Filter korrekt, weil unpaginiert):**
OrderRecipients/ArticleGroupMappings, OseonReporting/_OseonReportingTable, Picking/Bom (BOM-Spezialfall).

**Bewusst ohne Filter (CLAUDE.md-Ausnahmen):**
Detail-/Edit-Views, Print-Views, BdeTerminal, Settings/ServiceSettings, Users/RoleOverview, Buchungs-Forms (Inbound/Outbound/Transfer), Tracking/OseonIndex (eigene Lazy-Load-Filter-Architektur).

## 3. Out of Scope

- Tracking/OseonIndex — hat eigene Filter-Architektur (Lazy-Load + filterArticle), kein Umbau
- Picking/Bom — BOM-Baum-Spezialdarstellung, Client-Filter bleibt
- Neue Filter-Features (Range-Filter, Multi-Select-Dropdowns) — nur das bestehende Mini-Syntax-Pattern (OR via `,`, NOT via `!`)
- Performance-Optimierung der bestehenden 10 Server-Side-Views

## 4. Architektur — Hybrid-Ansatz

### 4.1 Standard-Pattern: In-Memory (13 paginierte Views)

Default fuer alle Listen mit ueberschaubarer Datenmenge (Stammdaten, offene Bestellungen):

```csharp
// Controller.Index:
var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
var allRows = await _repo.Get...Async(...);           // alle Rows laden (bis AllCap 5000)
var filtered = ColumnFilterHelper.Apply(allRows, columnFilters, ColumnMap);
var total = filtered.Count;
var pageRows = filtered.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();
```

- `ColumnMap`: statisches Dictionary `col-key -> Func<T, string?>` pro Controller. Beispiel:

```csharp
private static readonly Dictionary<string, Func<User, string?>> ColumnMap = new()
{
    ["username"] = u => u.UserName,
    ["name"] = u => u.Name,
    ["email"] = u => u.Email,
};
```

- `ColumnFilterHelper.Apply<T>` existiert bereits (in-memory Token-Matching, lowercase, OR/NOT-Mini-Syntax)
- View-Aenderung: `<table class="table table-striped filterable-table" data-view-key="..." data-server-column-filter="true">`, filterbare `<th>` bekommen `data-filterable` + `data-col-key="..."`
- `table-filter.js` rendert die Filter-Row automatisch und navigiert debounced (500ms) zu `?colf_<key>=value` — KEINE JS-Aenderung noetig
- Wo der bisherige Controller in SQL paginiert: auf "alle Rows laden, in C# filtern + paginieren" umstellen. Repos brauchen ggf. eine Methode ohne Skip/Take bzw. den `int.MaxValue`-PageSize-Trick (Pattern aus PickingLeitstand-Datumsfilter).

### 4.2 Sonderfall BdeBookings: SQL-Level

`BdeBookings` waechst unbegrenzt (jede Buchung bleibt historisch). In-Memory-Laden aller Buchungen skaliert nicht.

- `BdeBookingRepository.GetPagedAsync(...)` (bzw. die bestehende Listing-Methode) bekommt einen `IReadOnlyDictionary<string, string>? columnFilters`-Parameter
- EF-LIKE-Chains nach dem Pattern `ProductionOrderRepository.ApplyLeitstandColumnFilter` (Token → `%token%`, `patterns.Any(p => EF.Functions.Like(...))`, negate-Pfad)
- Datumsspalten (Start/Ende der Buchung): werden nach etabliertem Pattern serverseitig in C# gegen das gerenderte Format gematcht (der Plan ermittelt das exakte Render-Format der BdeBookings-Timestamps, z.B. `dd.MM.yyyy HH:mm`). Wenn ein Datums-Filter aktiv ist: alle SQL-text-gefilterten Rows materialisieren, in C# datums-filtern, dann paginieren (Pattern aus PickingLeitstandController).

### 4.3 Sonderfall Tracking/ByWorkplace: Client-Filter

Unpaginiert — alle Rows sind im DOM, Client-Filter arbeitet korrekt:

- Tabelle bekommt `class="... filterable-table" data-view-key="TrackingByWorkplace"` (OHNE server-flag)
- Filterbare `<th>` bekommen `data-filterable` + `data-col-key`
- Kein Controller-Umbau

### 4.4 View-Konventionen (alle 15)

- Aktions-/Button-/Checkbox-Spalten: KEIN `data-filterable`, aber `data-col-key` (Pflicht laut CLAUDE.md fuer column-preferences)
- Badge-/Status-Spalten: filterbar ueber den gerenderten Text (z.B. "Offen", "Teilgeliefert")
- Datumsspalten: `data-date-filter`-Attribut fuer den Kalender-Picker, serverseitig C#-Matching gegen das gerenderte Format

## 5. Betroffene Views im Detail

| # | View | Heute | Umbau | Filter-Ort |
|---|---|---|---|---|
| 1 | WarehousePicking/Index | Client + Pagination | server-flag + Controller-Map | in-memory |
| 2 | WarehouseRequisitions/Index | Client + Pagination | server-flag + Controller-Map | in-memory |
| 3 | Picking/Index | Client + Pagination | server-flag + Controller-Map | in-memory |
| 4 | PartRequisitions/Index | Client + Pagination | server-flag + Controller-Map | in-memory |
| 5 | StorageLocations/Index | Client + Pagination | server-flag + Controller-Map | in-memory |
| 6 | BdeBookings/Index | Client + Pagination | server-flag + **Repo-SQL** | SQL + C#-Datum |
| 7 | Users/Index | kein Filter | th-Attribute + Controller-Map | in-memory |
| 8 | Roles/Index | kein Filter | th-Attribute + Controller-Map | in-memory |
| 9 | Workstations/Index | kein Filter | th-Attribute + Controller-Map | in-memory |
| 10 | ProductionWorkplaces/Index | kein Filter | th-Attribute + Controller-Map | in-memory |
| 11 | ArticleCategories/Index | kein Filter | th-Attribute + Controller-Map | in-memory |
| 12 | ArticleAttributes/Index | kein Filter | th-Attribute + Controller-Map | in-memory |
| 13 | OrderRecipients/Index | kein Filter | th-Attribute + Controller-Map | in-memory |
| 14 | BdeMasterData/Index | kein Filter | th-Attribute + Controller-Map | in-memory |
| 15 | Tracking/ByWorkplace | kein Filter | nur th-Attribute (Client) | client |

Views 7-14 hatten bisher keine `filterable-table`-Klasse — sie bekommen sie inkl. `data-view-key` (aktiviert auch column-preferences-Kompatibilitaet; der Plan prueft pro View ob column-preferences-Spalten-Definitionen noetig sind oder die Tabelle ohne auskommt).

## 6. CLAUDE.md-Pflichtregel (neu)

Im Block "Listen-Views — Pflicht-Pattern" wird der Spalten-Filter-Punkt ersetzt durch:

> **Spalten-Filter (PFLICHT fuer ALLE Tabellen, seit v1.21.0)**: Jede Tabelle in einer Listen-View MUSS filterbar sein. Tabellen **mit Pagination** → `data-server-column-filter="true"` + Controller-seitiges Filtern ueber alle Eintraege (in-memory via `ColumnFilterHelper.Apply<T>` als Default; SQL-Level-Filter im Repo nur bei unbegrenzt wachsenden Tabellen wie BdeBookings). Tabellen **ohne Pagination** → mindestens `filterable-table` client-seitig. Ausnahmen nur fuer Detail-/Edit-/Print-/Terminal-/Settings-Views und statische Übersichten (RoleOverview, Hilfe).

Zusaetzlich wird die "Aktuelle Liste der server-filter-Tabellen" im Abschnitt "Pagination & Server-Side Spaltenfilter" auf alle 24 Views aktualisiert.

## 7. Tests

### 7.1 Controller-Tests (pro umgestelltem Controller, 13 Stueck)

Pro Controller 1-2 Tests nach bestehendem Pattern (Mock-Repo + DefaultHttpContext mit QueryString):

```csharp
[Fact]
public async Task Index_ColumnFilter_FiltersRows()
{
    // QueryString ?colf_<key>=value setzen, Repo liefert 3 Rows,
    // Assert: ViewModel.Items enthaelt nur die matchenden
}
```

### 7.2 BdeBookings-Repo-Tests

- Filter nach Operator-Name (LIKE-Match)
- Negate-Filter (`!wert`)
- OR-Filter (`wert1,wert2`)

### 7.3 Manuelle Tests (TESTSZENARIEN, neues Kapitel)

Stichproben-Drehbuch:
1. Eine View aus Gruppe B (z.B. WarehousePicking): Filter findet Eintrag auf Seite 2+
2. Eine View aus Gruppe C (z.B. Users): Filter-Row erscheint, Filter wirkt, URL hat `colf_`-Parameter
3. BdeBookings: Filter + grosse Datenmenge, Datumsspalte via Kalender-Picker
4. Tracking/ByWorkplace: Client-Filter wirkt (unpaginiert)
5. Kombinierte Filter in beliebiger Reihenfolge (Regression Leitstand-Bug)

## 8. Doku

- `AppVersion.cs` (Web + Service): v1.20.0 → v1.21.0
- `Views/Help/Changelog.cshtml`: v1.21.0-Karte
- `CLAUDE.md`: Pflichtregel (Sektion 6) + server-filter-Tabellen-Liste
- `PROJECT_STATUS.md`: Fortschritts-Notiz
- `docs/TESTSZENARIEN.md`: neues Kapitel (Sektion 7.3)

## 9. Architecture Decision Record (Kurz)

**Entscheidung:** Hybrid — in-memory `ColumnFilterHelper.Apply<T>` als Default fuer alle paginierten Listen, SQL-Level nur fuer BdeBookings, Client-Filter fuer unpaginierte Tabellen.

**Verworfen:**
- "Alles SQL-Level" (14 Repos anfassen, Mehraufwand ohne Nutzen bei kleinen Listen)
- "Alles Server-Side inkl. unpaginierter" (Client-Filter ist bei unpaginierten Tabellen funktional korrekt)

**Konsequenz:** Einheitliches User-Verhalten ueberall; Code-Aufwand konzentriert sich auf Controller-Maps + View-Attribute. Bei kuenftigem Wachstum einer Liste kann der Filter-Ort von in-memory auf SQL umgezogen werden, ohne dass sich URL-Schema oder View aendern.

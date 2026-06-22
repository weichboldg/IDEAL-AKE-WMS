# FA-Abarbeitungsliste: Komma-Werkbank-Filter + Bezeichnungs-Spalten — Design

> Status: Entwurf zur Review
> Datum: 2026-06-19
> Branch: `feature/windows-auth-ad-users` (Worktree `.claude/worktrees/missingparts-include-pd`)
> Ziel-Version: v1.23.0 (zusätzliche Changelog-Punkte)

## 1. Kontext & Ziel

Zwei Erweiterungen der FA-Abarbeitungsliste ([FaWorklistController](../../IdealAkeWms/Controllers/FaWorklistController.cs) / [FaWorklist/Index.cshtml](../../IdealAkeWms/Views/FaWorklist/Index.cshtml)):

1. **Komma-separierter Werkbank-Filter in den User-Settings.** Heute hat jeder User einen
   *einzelnen* Standard-Werkbank (`User.DefaultWorkplaceId`, FK, Migration 71), gepflegt über
   einen Dropdown in Profil + Benutzerstamm; die Abarbeitungsliste hat zusätzlich einen
   Einzel-Werkbank-Dropdown (`?workplaceId`). Künftig soll der User in seinen Einstellungen
   **mehrere** Werkbänke als komma-separiertes Textfeld hinterlegen können, und die
   Abarbeitungsliste auf diese Menge vorfiltern.
2. **Artikelbezeichnung anzeigen.** Die Liste zeigt aktuell nur die Artikel*nummer*. Neu sollen
   **Bezeichnung 1** (`ProductionOrder.Description1`) und **Bezeichnung 2**
   (`ProductionOrder.Description2`) als zwei zusätzliche Spalten erscheinen.

## 2. Nicht-Ziele (YAGNI)

- **Keine** Änderung der Werkbank-Stammdaten oder des Werkbank-AG-Mappings.
- **Keine** Registrierung von `FaWorklist` in `ColumnDefinitions` (separates, vorbestehendes
  Thema — die Liste nutzt nur das inline `#column-config`; Server-Persistenz der Spalten-Prefs
  ist nicht Teil dieser Erweiterung).
- **Keine** Multi-Select-Dropdown-Komponente — bewusst ein einfaches Komma-Textfeld
  (User-Entscheid).
- **Keine** Änderung an anderen Listen, die Werkbank/Bezeichnung zeigen (FA-Liste, Leitstand
  bleiben unverändert).

## 3. Requirement 1 — Komma-separierter Werkbank-Filter

### 3.1 Datenmodell
- `User.DefaultWorkplaceId` (int? FK → ProductionWorkplaces, Migration 71) wird **ersatzlos
  ersetzt** durch `User.DefaultWorkbenches` (`string?`, DB `NVARCHAR(400)` NULL,
  komma-separierte Werkbank-**Namen**).
- **Migration 75** `ReplaceUserDefaultWorkplaceWithWorkbenches`: dropt FK-Constraint
  `FK_Users_ProductionWorkplaces_DefaultWorkplaceId` + Index + Spalte `DefaultWorkplaceId`,
  fügt Spalte `DefaultWorkbenches` hinzu. `Down()` reversibel (Spalte/FK/Index wieder anlegen,
  `DefaultWorkbenches` droppen). Daten-Migration NICHT nötig (Spalte existiert erst seit
  Migr 71, minimaler Bestand) — der alte FK-Wert geht verloren (im `Down()` nicht
  rekonstruierbar; dokumentierter Trade-off).
- `SQL/75_ReplaceUserDefaultWorkplaceWithWorkbenches.sql` (idempotenter Guard) +
  `SQL/00_FreshInstall.sql` (Spalte/FK/Index entfernen, neue Spalte ergänzen, History-Insert).

### 3.2 User-Settings-UI
- **Profil** ([Account/Profile.cshtml](../../IdealAkeWms/Views/Account/Profile.cshtml)) und
  **Benutzerstamm** ([Users/Edit.cshtml](../../IdealAkeWms/Views/Users/Edit.cshtml)):
  der `DefaultWorkplaceId`-`<select>` wird ein `<input type="text">` „Standard-Werkbänke
  (kommasepariert)". Eine `<datalist>` mit den vorhandenen Werkbank-Namen dient als Tipphilfe
  (kein Zwang zu exakten Namen).
- ViewModels: `ProfileViewModel.DefaultWorkplaceId` (int?) → `DefaultWorkbenches` (string?);
  `UserEditViewModel.DefaultWorkplaceId` (int?) → `DefaultWorkbenches` (string?).
- POST-Actions (`AccountController.Profile`, `UsersController.Edit`) speichern den String
  unverändert (getrimmt) auf `User.DefaultWorkbenches`. `AvailableWorkplaces` bleibt im
  ViewModel als Quelle der Datalist-Optionen.

### 3.3 FaWorklist-Filter
- Der Einzel-Werkbank-`<select>` im Filter-Block wird ein `<input type="text" name="workbenches">`
  „Werkbänke (kommasepariert)" (+ `<datalist>` der Werkbank-Namen). Initialwert:
  URL-Param `workbenches` falls vorhanden, sonst `User.DefaultWorkbenches`.
- Controller-Signatur: `Index(int? workStepId, string? workbenches = null, bool showDone = false,
  int page = 1, int? pageSize = null)` — `workplaceId`-Parameter entfällt.
- **Default-vs-Override-Logik** (robust gegen „explizit geleert"): Da `workbenches` ein echtes
  GET-Formularfeld ist, wird es beim Submit IMMER mitgeschickt (auch leer).
  - `Request.Query.ContainsKey("workbenches")` → benutze den Param-Wert (leer = „alle Werkbänke").
  - Param fehlt (frische Navigation ohne Query) → benutze `User.DefaultWorkbenches`.
- **Filter-Anwendung:** Tokens = `effective.Split(',')`, je Token getrimmt, leere verworfen.
  Ist die Token-Menge nicht leer: Order bleibt, wenn `WorkplaceName` (case-insensitiv) **einen**
  der Tokens **enthält** (`Contains`, Komma-OR) — gleiche Semantik wie der `workbench`-
  Spaltenfilter (`ColumnFilterHelper`). Leere Token-Menge → kein Werkbank-Filter (alle).
- Der bestehende `workbench`-**Spaltenfilter** (`colf_workbench`) bleibt erhalten und verfeinert
  die Liste per UND (Ad-hoc-Narrowing zusätzlich zum Scope aus dem Komma-Feld).
- `FaWorklistViewModel`: `SelectedWorkplaceId` (int?) → `Workbenches` (string?) für den
  Input-Wert; `AvailableWorkplaces` bleibt (Datalist).

## 4. Requirement 2 — Bezeichnung 1 + 2

- `FaWorklistRow` bekommt `string? Description1` + `string? Description2`.
- `FaWorklistController.Index` befüllt sie aus `order.Description1` / `order.Description2`.
- `BuildColumnMap` bekommt zwei Keys: `["description1"] = r => r.Description1` und
  `["description2"] = r => r.Description2`.
- View [FaWorklist/Index.cshtml](../../IdealAkeWms/Views/FaWorklist/Index.cshtml):
  - zwei neue `<th data-filterable data-col-key="description1">Bezeichnung 1</th>` /
    `…"description2">Bezeichnung 2</th>` direkt **nach** „Artikelnummer".
  - zwei neue `<td>@item.Description1</td>` / `<td>@item.Description2</td>` an gleicher Stelle.
  - zwei neue Einträge im inline `#column-config`-JSON (Keys `description1`/`description2`).
  - `columnCount` von `7 + AttributeColumns.Count + 1` auf `9 + AttributeColumns.Count + 1`.

## 5. Tests
**Unit (xUnit, FaWorklistControllerTests, EF InMemory):**
- `DefaultWorkbenches` greift, wenn `?workbenches` fehlt (User hat z. B. „WB-A,WB-B" → nur FAs
  dieser Werkbänke).
- `?workbenches=WB-A` überschreibt den User-Default.
- `?workbenches=` (present-empty) → alle Werkbänke (Default greift NICHT).
- Contains-Semantik: Token „WB-A" matcht Werkbank „WB-A2".
- `Description1`/`Description2` werden in die Rows übernommen und sind per
  `colf_description1`/`colf_description2` filterbar.

**Unit (AccountController/UsersController-Tests, soweit vorhanden):** POST speichert
`DefaultWorkbenches` (getrimmt) auf den User.

**Manuell (TESTSZENARIEN, neues Kapitel):** Profil-Textfeld pflegen → Abarbeitungsliste
vorgefiltert; In-Listen-Textfeld override/leeren; Bezeichnungs-Spalten sichtbar + filterbar.

## 6. Doku & Versionierung
- `AppVersion` (Web + Service) unverändert auf v1.23.0; `Changelog.cshtml` v1.23.0-Karte um
  Bullet ergänzen.
- `CLAUDE.md`: Fallstrick „FA-Abarbeitung Default-Filter" aktualisieren
  (`DefaultWorkplaceId` → `DefaultWorkbenches`, Komma-Contains-Semantik), Migration-75-Hinweis.
- `docs/TESTSZENARIEN.md`: neues Kapitel.
- `PROJECT_STATUS.md`: kurzer Eintrag.

## 7. Risiken / offene Punkte
- **Migration 75 ist destruktiv** für `DefaultWorkplaceId` (Spalte wird gedroppt). Bestehende
  Default-Werkbank-Zuordnungen gehen verloren; Anwender müssen ihre Werkbänke im neuen Textfeld
  neu setzen. Vor Produktions-Deploy kommunizieren. (Kein Daten-Backfill, da Feature jung.)
- **Werkbank-Namen mit Komma** würden vom Split fälschlich getrennt. Werkbank-Namen enthalten
  praktisch keine Kommas; kein Escaping vorgesehen (YAGNI, dokumentiert).
- Zwei Werkbank-Filtermechanismen (Komma-Textfeld als Scope + `workbench`-Spaltenfilter als
  Verfeinerung) koexistieren bewusst und komponieren per UND — kein Widerspruch, aber im
  TESTSZENARIEN klarstellen.

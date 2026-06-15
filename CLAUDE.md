# IdealAkeWms — Kontext fuer KI-Assistenten

## Projektüberblick

Warehouse-/Operationsmanagement-System für die IDEAL-AKE-Gruppe (Standorte AKE GmbH
und IDEAL). Web-Anwendung plus Windows-Service für Hintergrundaufgaben. Wird
intern in der Produktion und im Lager eingesetzt.

**Hauptkomponenten:**

- `IdealAkeWms/` – ASP.NET Core MVC Web-Anwendung (UI + API)
- `IdealAkeWms.Tests/` – Tests zur Web-Anwendung
- `IDEALAKEWMSService/` – Windows-Service für geplante Jobs und Integrationen
- `IDEALAKEWMSService.Tests/` – Tests zum Service
- `SQL/` – Migrationsskripte und FreshInstall
- `docs/` – Projektdokumentation
- `secondbrain/` – Obsidian-Vault mit ADRs, Bugs, Features (siehe unten)

**Tech Stack:** [TODO: .NET-Version, EF Core / Dapper?, SQL Server-Version, Bootstrap-Version]

---

## Knowledge Base – `secondbrain/`

Strukturierte Wissensbasis als Obsidian-Vault im Repo. Konsultiere ihn aktiv:

@secondbrain/HOME.md

## Workflow

- **Skill-basierter Workflow (ab 2026-05-26 verpflichtend)**:
  - **Vor jeder nicht-trivialen Code-Aenderung**: Plan ueber `superpowers:writing-plans` erstellen. Bei unklarer Anforderung zuerst `superpowers:brainstorming`. *Trivial* = klar abgegrenzter Einzel-Fix wie Typo, einzelne Konstanten-Aenderung, Comment-Update — alles mit Architektur-/Datenmodell-/Mehr-Datei-Impact braucht einen Plan.
  - **Ausfuehrung agentenbasiert**: Plan-Tasks mit Sub-Agents abarbeiten (`superpowers:subagent-driven-development` oder `superpowers:executing-plans`). Wo Tasks voneinander unabhaengig sind: **parallel** via `superpowers:dispatching-parallel-agents` (mehrere Agent-Tool-Calls in einer Nachricht). Sequentiell nur wenn echte Dependencies bestehen.
  - **Qualitaetscheck Pflicht** vor Plan-Abschluss / Commit / PR: `superpowers:verification-before-completion` + Code-Review via Skill `code-review` (bzw. `superpowers:requesting-code-review`). Build + Tests gruen sind Mindestbedingung, nicht Beweis genug.
  - **Bei Debugging**: `superpowers:systematic-debugging` (oder `engineering:debug`) statt Symptome-Patching.
- **Isolation per Worktree (ab 2026-05-27 verpflichtend)**: Groessere Aenderungen — Multi-Task-Rollouts (mehrere Service-Integrationen, Phasen-Refactors, neue Module mit eigenem Spec+Plan) — laufen IMMER in einem eigenen Worktree, nicht direkt auf `main`. Setup: `git worktree add .claude/worktrees/<feature-slug> -b <branch>`. Erst nach abgeschlossenem Rollout + Merge in main den Worktree aufraeumen (`git worktree remove ...`, `git branch -d ...`). **Ausnahme:** Wenn die aktuelle Session bereits in einem Worktree gestartet ist, dort weiterarbeiten. **Trivial-Schwellwert** (kein eigener Worktree noetig): 1-2 Datei-Fixes, Doku-Tweaks, einzelne Konstanten-Aenderungen — alles was sich in einem einzelnen Commit erschoepfend beschreiben laesst.
- **Checkliste nach Aenderungen:**
  - [ ] Migration erstellt + `SQL/XX_*.sql` mit OBJECT_ID-Guard?
  - [ ] `SQL/00_FreshInstall.sql` aktualisiert?
  - [ ] `PROJECT_STATUS.md`, `README.md`, Hilfeseite gepflegt?
  - [ ] Audit-Felder gesetzt (ModifiedAt, ModifiedBy, ModifiedByWindows)?
  - [ ] Version hochgezaehlt + Changelog ergaenzt?
  - [ ] Testszenarien in `docs/TESTSZENARIEN.md` ergaenzt/aktualisiert?
- **Verifikation** — Niemals Task als erledigt ohne Beweis (Build, Tests, View)
- **Einfachheit** — Minimale Code-Auswirkung, Root Cause finden, kein Over-Engineering
- **Testszenarien-Pflicht** — Bei jedem neuen Feature und jedem Bugfix immer ein vollstaendiges manuelles Testszenario liefern: Vorbedingungen, Schritt-fuer-Schritt-Aktionen, erwartetes Verhalten, ggf. Negativ-Faelle. Zusaetzlich `docs/TESTSZENARIEN.md` mit den neuen Szenarien synchronisieren (das Dokument ist die Single Source of Truth fuer die manuelle Abnahme).
- **Listen-Views — Pflicht-Pattern (ab v1.14.0)** — Jede neue Listen-View (`Views/<Controller>/Index.cshtml` oder vergleichbar) MUSS folgendes Standard-Layout liefern:
  - **Pagination**: `PageSize.Resolve` + `PaginationState` im Controller, `_Pagination`-Partial am Ende der View, User-Default ueber `_currentUserService.GetDefaultPageSizeAsync()`. Page-Sizes 25/50/100/Alle (Cap 5000). Kein hartcodierter Take/Cap.
  - **Filter**: `<div class="card filter-card mb-3">` mit Filter-Inputs ueber dem Tabellen-Block. Globale Filter (Freitext, Status-Flags) per `?filter*=...` Query.
  - **Spalten-Filter — PFLICHT fuer ALLE Tabellen-Views (ab v1.21.0)**: Jede Tabellen-View MUSS spaltenfilterbar sein — keine neue Tabelle ohne Filter. **Server-Side** (Standard fuer paginierte/grosse Listen): `data-server-column-filter="true"` auf `<table class="table table-striped filterable-table" data-view-key="...">`. Alle `<th>` brauchen `data-col-key`. Controller liest `ColumnFilterHelper.ReadFromQuery(HttpContext?.Request)` und mappt Col-Keys auf Properties (in-Memory via `ColumnFilterHelper.Apply<T>` ODER LINQ je nach Repo). **Client-Mode** (nur `filterable-table` + `data-filterable`/`data-col-key`, OHNE `data-server-column-filter`): ausschliesslich fuer kleine, unpaginierte, vorgefilterte Ansichten (z.B. Tracking/ByWorkplace).
  - **Datumsspalten**: Falls vorhanden, server-seitig in C# nach Termin-Berechnung filtern (Format `dd.MM.yyyy KWxx` lowercase), nicht in SQL.
  - **Layout-Konsistenz**: `<h2 class="page-header">`, TempData-Alerts (Success/Warning), `<div class="table-responsive">` um die Tabelle, Page-Header-Buttons via `d-flex justify-content-between flex-wrap gap-2`.
  - **Reference-Implementierungen**: [ProductionOrdersController.Index](IdealAkeWms/Controllers/ProductionOrdersController.cs) + [Views/ProductionOrders/Index.cshtml](IdealAkeWms/Views/ProductionOrders/Index.cshtml) (mit Datumsfilter), [StockOverviewController](IdealAkeWms/Controllers/StockOverviewController.cs) (einfacher Fall).
  - **Ausnahmen** (begruendet): Konfig-/Dashboard-/Terminal-Views ohne echte Datenliste (Home, Help, Settings, ServiceSettings, BdeCockpit, BdeShiftCalendar, BdeTerminal). BOM-Tree (hierarchische Spezialdarstellung). Tracking/Index (alte WMS-Teileverfolgung): hierarchische Gruppen-+Detail-Rows mit colspan — `data-filterable` wuerde den Client-Sort aktivieren, der Detail-Rows von ihren Gruppen trennt; kein Spaltenfilter solange die Struktur nicht flach ist.

## Architektur

- ASP.NET Core 10.0 MVC + Repository Pattern + DI
- EF Core 10.0 mit SQL Server (`AKESQL20.ake.at`, DB: `IDEAL_AKE_WMS`)
- Dual-Auth: Windows/Negotiate (IIS) + Session-basierter App-Login (Middleware in Program.cs)
- BOM-Daten: primaer aus SAGE-View; Fallback auf OSEON-SP. `BomRepository` liefert `BomQueryResult(Items, DataSource)`
- `CachedBomRepository` wrapped `BomRepository` (Decorator-Pattern, 5 min MemoryCache)

## Konventionen

- **Sprache**: Code/Variablen auf Englisch, UI-Texte auf Deutsch
- **Entity-Basis**: `AuditableEntity` (Id, CreatedAt, CreatedBy, CreatedByWindows, ModifiedAt?, ModifiedBy?, ModifiedByWindows?)
- **Corporate Design**: `--ake-primary: #053153`, `--ake-secondary: #43A6E2`, `--ake-orange: #E87A1E`
- **TempData**: Nur `SuccessMessage` (alert-success) und `WarningMessage` (alert-warning). Kein `ErrorMessage` (Fehler via ModelState)
- **Versionierung**: `AppVersion.cs` in Web + Service aktualisieren, `Views/Help/Changelog.cshtml` ergaenzen

## Session & Authentifizierung

- **Session-Timeout**: 8 Stunden, Cookie: `IdealAkeWms.Session`
- **Session-Keys**: `AppUserId` (Int32), `AppUserName` (String)
- **Middleware-Reihenfolge**: HttpsRedirection → Routing → Authentication → Authorization → **Session** → SerilogRequestLogging → **LoginRedirect** → StaticFiles → MapControllerRoute
- **Login-Redirect-Ausnahmen**: `/account/*`, `/api/*`, statische Dateien, `/lib/*`, `/css/*`, `/js/*`

## Zugriffsschutz

| Filter-Attribut | Rollen | Angewendet auf |
|----------------|--------|---------------|
| `[RequireMasterDataAccess]` | admin, masterdata | Action-Level fuer Edit-Actions in 6 Stammdaten-Controllern (ArticlesController, StorageLocationsController, ProductionWorkplacesController, OrderRecipientsController, ArticleCategoriesController, ArticleAttributesController). Class-Level ist [RequireMasterDataReadAccess]. |
| `[RequireMasterDataReadAccess]` | admin, masterdata_read, masterdata | Class-Level der 6 operativen Stammdaten-Controller (ArticlesController, StorageLocationsController, ProductionWorkplacesController, OrderRecipientsController, ArticleCategoriesController, ArticleAttributesController). Read-Filter, Edit-Actions verschaerfen mit [RequireMasterDataAccess]. |
| `[RequireAdminAccess]` | admin | UsersController, RolesController, WorkstationsController, SettingsController, SyncLogController, BdeShiftCalendarController (seit v1.20.0-Refactor) sowie ServiceSettingsController |
| `[RequirePickingAccess]` | admin, picking | ProductionOrdersApiController, PickingController (Actions ausser Index) |
| `[RequireFaCompletionAccess]` | admin, fa_completion | FaCompletionController |
| `[RequirePickingOrFaCompletionAccess]` | admin, picking ODER fa_completion | FaWorkStepsApiController (`/api/fa-work-steps/toggle`) — bis v1.21: AssemblyGroupsApiController (`/api/assembly-groups/toggle-applicable`), abgeloest |
| `[RequireVorbauAccess]` | admin, vorbau | FaWorklistController, FaWorkStepsApiController (`/api/fa-work-steps/toggle-completed`) (seit v1.22.0) |
| `[RequirePickingOrVorbauAccess]` | admin, picking ODER vorbau | PickingController.PrintBom (Druck aus read-only Stueckliste, seit v1.22.0) |
| `[RequireTrackingAccess]` | admin, tracking | TrackingController |
| `[RequireStockAccess]` | admin, stock, stock_keyuser, picking | StockMovementsController, StockOverviewController |
| `[RequireLagerProcessingAccess]` | admin, stock, stock_keyuser | WarehousePickingController, MissingPartsLagerController (Lager-Worklist seit v1.20.0 — picker explizit ausgeschlossen) |
| `[RequireStockKeyUserAccess]` | admin, stock_keyuser, picking | StockMovementsController (Lagerplatz ausbuchen/umbuchen) |
| `[RequirePickingOrTrackingOrLeitstandAccess]` | admin, picking ODER tracking ODER leitstand | ProductionOrdersController (slim Index) |
| `[RequirePickingOrLeitstandAccess]` | admin, picking ODER leitstand | PickingLeitstandController (class-level) |
| `[RequirePickingOrStockAccess]` | picking ODER stock | PartRequisitionsController, OrderRecipientGroupsController, WarehouseRequisitionsController, WarehouseRequisitionsApiController |
| `[RequireLeitstandAccess]` | admin, leitstand | (frueher ProductionOrdersController; ab v1.12.0 ueber Composite-Filter auf PickingLeitstandController) |
| `[RequireReportingAccess]` | admin, reporting | OseonReportingController |
| `[RequireBdeUserAccess]` | admin, bde_user, bde_shiftlead, bde_admin | BdeTerminalController, BdeApiController |
| `[RequireBdeShiftleadAccess]` | admin, bde_shiftlead, bde_admin | BdeCockpitController, BdeBookingsController (Index), BdeMasterDataController |
| `[RequireBdeAdminAccess]` | admin, bde_admin | BdeBookingsController (Edit/Cancel), BdeMasterDataController (Terminals) |
| *(kein Filter)* | jeder eingeloggte User | UserViewPreferencesApiController (Login-Check, kein Rollen-Filter) |

**Sonderfaelle:**
- Admin-Reset fuer View-Einstellungen laeuft ueber `UsersController.ResetViewPreferences` (`[RequireMasterDataAccess]`)

## Rollenkonzept

`Role`-Tabelle + `UserRole`-Junction (Many-to-Many), statische Keys in `RoleKeys.cs`. Admin-Wildcard ueberspringt alle Pruefungen. Rollen koennen optional AD-Gruppen haben (`Role.AdGroup`).

| Key | Beschreibung |
|-----|-------------|
| `admin` | Vollzugriff |
| `masterdata` | Artikel, Lagerplaetze, Werkbaenke, Artikelkategorien, Artikelmerkmale, Empfaenger + Artikelgruppen-Zuordnungen (lesen + aendern). Benutzer/Rollen/Settings/Logs sind admin-only. |
| `masterdata_read` | Nur-Lesen-Zugriff auf dieselben 6 Stammdaten-Sichten (Artikel, Lagerplaetze, Werkbaenke, Artikelkategorien, Artikelmerkmale, Empfaenger) (seit v1.20.0) |
| `picking` | Picking + vollstaendiger Lagerzugriff |
| `stock` | Einbuchung, Ausbuchung, Bestaende |
| `stock_keyuser` | Lager + Lagerplatz ausbuchen/umbuchen |
| `tracking` | OSEON Auftraege + Rueckmeldungen |
| `reporting` | Betriebsdaten / BDE (Zukunft) |
| `leitstand` | Produktionsauftraege freigeben und priorisieren |
| `fa_completion` | FA-Vervollstaendigung: Werkbank, Arbeitsgaenge + Merkmale pro FA pflegen (bis v1.21: Merkmalsauspraegungen pro Vormontageplatz) |
| `vorbau` | FA-Abarbeitungsliste: Vorbau-Arbeitsgaenge einsehen und abhaken (seit v1.22.0) |
| `bde_user` | Terminal-Buchung: Arbeitsgaenge scannen, Status wechseln |
| `bde_shiftlead` | + BDE-Stammdaten, Buchungsliste, Cockpit |
| `bde_admin` | + Buchungen korrigieren/stornieren, Terminals konfigurieren |

## Controller-Muster

**MVC-Controller** (erben `Controller`): Validierung via `ModelState`, Erfolg via `TempData["SuccessMessage"]` + `RedirectToAction`, Audit-Felder beim Update aus `ICurrentUserService`.

**API-Controller** (erben `ControllerBase`, `[ApiController]`, `[Route("api/...")]`): Rueckgabe `Ok()` / `NotFound()` / `BadRequest()`, keine Views.

## Bekannte Fallstricke

- **Artikelnummer vs Ressourcenummer**: `Artikelnummer` = Geraete-Artikelnummer, `Ressourcenummer` = Bauteil-Artikelnummer. Immer `Ressourcenummer` fuer Bauteil-Operationen verwenden
- **Boolean-Checkbox in Forms**: Hidden+Checkbox mit gleichem `name` funktioniert NICHT. Loesung: Hidden-Input bekommt `name`, Checkbox hat `onchange`-Handler der den Hidden-Wert synchronisiert
- **data-col-key Pflicht**: Alle `<th>` in filterable-tables MUESSEN `data-col-key` Attribut haben. Bei neuen Spalten: Key in `ColumnDefinitions.cs` definieren und in View verwenden
- **column-preferences Init-Reihenfolge**: `column-preferences.js` MUSS vor `table-filter.js` eingebunden werden (dispatcht `column-preferences-ready` Event)
- **Bootstrap table styles** ueberschreiben custom CSS → `!important` noetig
- **InMemory DB** unterstuetzt kein `rowversion` → Tests nutzen `TestApplicationDbContext`
- **SQL Server Batch-Parsing**: Tabellen in separatem Batch erstellen (GO), `OBJECT_ID`-Guard verwenden
- **EF PendingModelChangesWarning**: Nach Model-Aenderungen immer `dotnet ef migrations add` ausfuehren
- **QR-Code Komma-Suffix**: FA-Nummer kann Komma-Suffix haben → `.split(',')[0]` verwenden
- **Scanner-Endlosschleife**: `confirm()` nach Scan-Fehler → Endlosschleife. Bootstrap-Modal verwenden
- **OSEON pa.ID ist bigint**: `long` verwenden, nicht `int`
- **enaio object1.id ist int**: `Convert.ToInt64(reader.GetValue(0))` verwenden
- **Artikelgruppe BOM vs Articles**: SAGE liefert `"940 - Kleinmaterial"`, Articles speichert `"940"`. Beim Matching `.split(' - ')[0].trim()`
- **Kommissionierwagen (IsPickingTransport)**: Gefiltert in: Stueckliste Quell-Dropdown + Bestand, Meldebestand-Farbcodierung, StockCheckService. NICHT gefiltert in: Bestandsuebersicht-Liste, Bewegungshistorie. Ziel-Dropdown zeigt NUR Wagen
- **Razor v@**: `v@Namespace.Class` wird als E-Mail geparst → `v@(Namespace.Class)` verwenden
- **Select2-Text-Format**: API liefert `"ArticleNumber - Description"`. Parsing: `.split(' - ')[0]`, NICHT Em-Dash
- **Service referenziert Web-Projekt (seit BDE Phase 2.3)**: Bis Phase 2.2 nutzte der Service eigene DTOs und raw SQL (SDK.Worker vs SDK.Web). Mit Phase 2.3 referenziert `IDEALAKEWMSService` jetzt das Web-Projekt direkt (BdeAutoPauseWorker braucht `ApplicationDbContext`, `BdeShiftCalendarService`, `BdeBookingStatus`-Enum). Build/Publish/Tests laufen sauber. Bei neuen Service-Features den DB-Zugriff via shared `ApplicationDbContext` und Web-Repositories nutzen statt Dapper-Duplikate
- **AppSettings-Tabelle**: KEIN AuditableEntity — nur Key (PK), Value, Description
- **Beschichtungstermin Backward-Compat**: Wenn `LackierteilKategorieName` leer → Beschichtungstermin fuer ALLE Auftraege
- **IsActive vs IstBuchbar**: Zwei unabhaengige Status-Flags auf StorageLocation. `IsActive` ist Sage-controlled (Phase-1-Sync setzt es), `IstBuchbar` ist user-controlled. Buchungs-Dropdowns filtern auf BEIDE; Bestand-Aggregation und Sage-Korrektur-Buchungen ignorieren `IstBuchbar`. Default: Manual=true (buchbar), Sage=false (nicht buchbar — Admin schaltet manuell frei).
- **BDE-Buchung Mehrfach-Regel**: Ohne Konfiguration darf ein Operator nur eine aktive Buchung haben und ein Arbeitsgang nur eine aktive Buchung (Enforcement im Service, nicht mehr als UNIQUE-Index). Die Settings `BdeMehrfachBuchungProOperator` und `BdeMehrfachBuchungProArbeitsgang` lockern diese Constraints jeweils unabhaengig. Die Indexes `IX_BdeBookings_BdeOperatorId_Active` und `IX_BdeBookings_WorkOperationId_Active` sind seit Phase 2.2 nicht mehr UNIQUE (nur noch regulaere gefilterte Indexes).
- **BDE-Paused Semantik**: Paused hat `EndedAt = gesetzt`. Fortsetzung erzeugt neue Buchung mit `ParentBookingId`. Cockpit-Query `WHERE EndedAt IS NULL` zeigt nur Running.
- **BDE-Operator deaktiviert waehrend offener Buchung**: Offene Buchungen bleiben sichtbar. Schichtleiter muss manuell schliessen.
- **Save-Ordering in BdeBookingService**: Bei Auto-Close-und-New-Start MUSS `SaveChangesAsync` zwischen Schliessen und Add-Neu laufen (Helfer `FinishAndSaveAsync`), eingebettet in `BeginTransactionAsync()`.
- **enaio DMS-Sync kein Delta**: `angelegt`-Spalte in enaio ist statisch (Bulk-Import 2013). Full-Sync statt Delta — MERGE verhindert Duplikate. `EnaioDmsSyncService.cs` liest ALLE Werkstattauftraege/Zeichnungen ohne Datumsfilter.
- **BDE Auto-Pause EndedAt = exaktes Schichtende**: Der `BdeAutoPauseWorker` setzt `EndedAt` auf den exakten Schicht-Ende-Zeitpunkt (z.&nbsp;B. 14:00:00), NICHT auf `DateTime.Now`. Dadurch ist die Buchungsdauer unabhaengig vom tatsaechlichen Worker-Tick (max. `Sync:BdeAutoPauseIntervalMinutes` Latenz).
- **MovementType-Aggregation**: Bei jeder neuen `MovementType`-Erweiterung muss die Aggregations-Logik in `StockMovementRepository` (5 Stellen) und `PickingTransferService` aktualisiert werden. Insbesondere die kollabierten Switches (z.B. `Ausbuchung ? -Quantity : Quantity`) sind gefaehrlich, weil sie unbekannte Werte still falsch behandeln.
- **ProductionOrder Status-Aufteilung (seit v1.11.0, AssemblyGroups-Teil abgeloest in v1.22.0)**: Die `ProductionOrders`-Tabelle enthaelt nur noch Sage-Master-Daten. App-Status liegt in verbundenen Tabellen: `ProductionOrderPickingStatus` (1:1, HasGlass/HasExternalPurchase/HasCoatingParts/IsCoatingDone/IsReleasedForPicking/PickingPriority/AssignedPicker/IsDonePicking), `ProductionOrderBdeStatus` (1:1, IsDoneBde) und seit v1.22.0 `FaWorkSteps` (1:N, nur tatsaechlich benoetigte AGs je FA — ersetzt `ProductionOrderAssemblyGroups` mit den 5 eager Zeilen VK/VL/VE/VT/VA, Tabelle gedroppt). Sage-AgentJob legt nur noch PickingStatus/BdeStatus ueber Folge-MERGEs eager an (AssemblyGroups-MERGE in v1.22.0 ersatzlos entfernt). Toggle-API: `/api/picking-status/toggle`, `/api/fa-work-steps/toggle` (bis v1.21: `/api/assembly-groups/toggle-applicable`), `/api/bde-status/toggle`. FA.IsDone = Sage-Master; App-Komm-Done = PickingStatus.IsDonePicking; App-BDE-Done = BdeStatus.IsDoneBde.
- **Leitstand-Kommissionierung getrennte View (seit v1.12.0)**: Das `ProductionOrders/Index` ist nur noch eine schlanke FA-Übersicht. Komm-Status-Spalten (PickingStatus/HasGlass/etc.), Bulk-Freigabe und Picker-Zuweisung leben jetzt in `PickingLeitstand/Index`. Routes `/ProductionOrders/ToggleRelease|BulkRelease|SetPriority|ChangeAssignedPicker` sind 301-Stub-Redirects auf die neuen `/PickingLeitstand/...`-Endpoints. Ab v1.14.0 ist "Leitstand" ein eigenes Hauptmenue (vorher Sub-Item unter Kommissionierung).
- **FA-Vervollstaendigung-Daten leben in FaWorkSteps/FaWorkStepSpecs (bis v1.21: AssemblyGroups/AssemblyGroupSpecs, abgeloest)**: Seit v1.22.0 gibt es KEINE 5 eager-created Gruppen je FA mehr — `FaWorkSteps`-Zeilen entstehen via Detection-Sync (`Source='Sync'`) oder manuell (`Source='Manual'`, FaCompletion/Leitstand). AG-Toggle via `/api/fa-work-steps/toggle`; Erledigt-Status (`IsCompleted`) via `FaCompletion/ToggleIsCompleted` oder `/api/fa-work-steps/toggle-completed` (Abarbeitungsliste); Specs-CRUD via `FaCompletion/AddSpec|EditSpec|DeleteSpec`; strukturierte Merkmal-Werte in `FaAttributeValues` (UNIQUE je FA+Definition, Wert gilt je FA auch bei Mehrfach-AG-Zuordnung). Modul-Gate bleibt AppSetting `FaCompletionAktiv` (Default false) — gated auch die FA-Abarbeitungsliste.
- **FreshInstall.sql vs. EF-Migrations**: Bei jeder neuen Migration MUESSEN zwei Stellen in `SQL/00_FreshInstall.sql` synchron gehalten werden: (1) die durch die Migration angelegten/geaenderten Schema-Objekte (Tabellen, Indexe, Constraints) im konsolidierten Schema, UND (2) die `MigrationId` im `__EFMigrationsHistory`-INSERT-Block am Ende. Faehlt einer der beiden Punkte, scheitert entweder FreshInstall direkt oder der erste App-Start danach (EF replayt die fehlende Migration gegen ein Schema, in dem die Objekte bereits existieren → SqlException).
- **Decimal/Culture-Bug in HTML-Form-Inputs (v1.14.0)**: Wenn ein `decimal`-Wert mit `ToString(InvariantCulture)` als String in ein `data-`-Attribut/Input-Wert geschrieben wird und die DB-Spalte z.&nbsp;B. `DECIMAL(18,4)` ist, entsteht "4.0000". Der ASP.NET-Core-Default-Model-Binder verwendet jedoch die aktuelle Request-Culture (Deutsch) — und parst "4.0000" als **40000** (Punkt als Tausender). Loesung in Lagerbestellungen: Mengen-Inputs auf `type="number" step="1"` + `int[]`-Binding statt `decimal[]`. Bei kuenftigen Forms mit Mengen-Eingaben: entweder integer verwenden oder explizit `CultureInfo.InvariantCulture` parsen.
- **Server-Side Column Filter Modus (v1.14.0)**: Tabellen mit `data-server-column-filter="true"` (FA-Liste, Leitstand, Bestand, Bewegungshistorie, Artikel) navigieren bei Filter-Input zur URL `?colf_<col-key>=value` statt clientseitig zu filtern. Controller liest via `ColumnFilterHelper.ReadFromQuery(HttpContext?.Request)` (null-safe fuer Tests). Pro Liste mappt der Controller/Repo die Col-Keys auf Properties. Datumsspalten werden in C# nach Termin-Berechnung gefiltert (Format `dd.MM.yyyy KWxx` lowercase) — NICHT in SQL.
- **Pagination-AllCap (5000)**: `PageSize.Resolve` mapped User-Wahl "Alle" (Sentinel 0) auf `PageSize.AllCap = 5000`. `PaginationState.IsCappedAtAll` ist computed (PageSizeRaw==0 && TotalCount > 5000) und triggert den Banner-Hinweis im `_Pagination`-Partial.
- **Lagermitarbeiter-Notiz-Autosave (Lagerbestellungen, v1.14.0)**: Das Notiz-Feld in `WarehousePicking/Details` hat AJAX-Autosave on `blur` und vor `Drucken` (Tab wird synchron mit `about:blank` geoeffnet, dann nach `await saveNotes()` zur Print-URL navigiert — sonst wuerde Popup-Blocker den verzoegerten `window.open` blockieren).
- **StorageLocation.Code Laenge (v1.14.0)**: DB-Spalte ist `NVARCHAR(50)`. Manuelle Codes bleiben per `IValidatableObject.Validate` auf 12 Zeichen begrenzt (Barcode-Lesbarkeit); Sage-Codes nutzen den vollen Platz. Frontend: Edit-View setzt `maxlength="50"` fuer Sage-Eintraege und `maxlength="12"` fuer manuelle.
- **Picking Source-Lagerplatz-Fallback (v1.14.0)**: Auto-Suggest beruecksichtigt nur Lagerplaetze die im Dropdown angezeigt werden koennen (IstBuchbar=true, nicht-Wagen). Sage-Lagerplaetze mit Bestand aber IstBuchbar=false werden NICHT vorgeschlagen — Fallback ist NAN. Auch bereits gespeicherte SourceStorageLocationIds, die nicht mehr buchbar sind, werden ignoriert und durch den neuen Vorschlag ersetzt.
- **SyncLogger nutzt IDbContextFactory, nicht den Scope-DbContext (seit v1.15.0)**: `ISyncLogger`/`ISyncRun` schreiben jede Zeile mit einem **frischen** DbContext (`IDbContextFactory<ApplicationDbContext>`). Grund: Diagnose-Logs duerfen nicht in Sync-Transaktionen mitrollen. Counts-Keys sind deutschsprachig (`"neu"`, `"aktualisiert"`, `"uebersprungen"`, `"fehler"`); neue Services sollen dasselbe Vokabular verwenden. Pattern siehe [secondbrain/docs/superpowers/specs/2026-05-26-synclog-pflicht-alle-syncs-design.md](secondbrain/docs/superpowers/specs/2026-05-26-synclog-pflicht-alle-syncs-design.md).
- **SyncLog.Timestamp = DateTime.Now, NICHT UtcNow (seit v1.15.3)**: `SyncRun.WriteEntryAsync` setzt den Timestamp **nicht** explizit — der Model-Default `DateTime.Now` (Lokalzeit) greift. Der `SyncLogController.Index`-View zeigt Timestamps ohne UTC-Konversion via `.ToString("dd.MM.yyyy HH:mm:ss")`. Wer `DateTime.UtcNow` setzt, produziert Eintraege die in der DESC-Sortierung 2h "frueher" erscheinen und in der UI unter aelteren Eintraegen verschwinden (das war der v1.15.0-v1.15.2-Bug, gefixed in v1.15.3 / Commit `a2a3275`). Wenn die Anwendung jemals Multi-Timezone-faehig wird, dann konsistent UtcNow + UI-Konversion umstellen — aber nicht halb.
- **UI 'Aktivitaets-Protokoll' vs. Tabelle 'SyncLogs' (seit v1.15.1)**: Das Menue/UI-Label heisst "Aktivitaets-Protokoll", die DB-Tabelle, Klassen und Interfaces behalten den historischen Namen `SyncLog`/`SyncLogger`/`ISyncLogger`/`SyncLogServices`. Bewusste Asymmetrie (eigene Spec begruendet das in [secondbrain/docs/superpowers/specs/2026-05-27-activity-log-non-sync-services-design.md](secondbrain/docs/superpowers/specs/2026-05-27-activity-log-non-sync-services-design.md) §6). URL-Route bleibt `/SyncLog/Index`. Filter-URL-Parameter heisst weiter `?colf_Service=...`.
- **ISyncLogger-Konstruktor-Position (seit v1.15.2)**: Neue Services injecten `ISyncLogger` als **letzten** Konstruktor-Parameter, immer NACH `ILogger<T>`. Konvention vereinheitlicht seit v1.15.2 (vorher hatte HolidaySyncService die umgekehrte Reihenfolge). Verbundene Konvention: Connection-String-Validation MUSS innerhalb des try-Blocks nach `BeginRunAsync` liegen, damit `FinishFailedAsync` bei Config-Fehlern feuert.
- **iOS Safari + getUserMedia (seit v1.16.0)**: `navigator.mediaDevices.getUserMedia()` muss im **synchronen User-Gesture-Stack** aufgerufen werden — also direkt im Click-Handler, **bevor** asynchrone Operationen (Modal-Show, await) laufen. iOS Safari verweigert sonst die Permission. Pattern: erst `await requestCameraPermission()` (Pre-Warm im Click), dann Modal-Show + Scanner-Init. Siehe `barcode-scanner.js` und Spec [secondbrain/docs/superpowers/specs/2026-05-28-oseon-tracking-ios-fix-design.md](secondbrain/docs/superpowers/specs/2026-05-28-oseon-tracking-ios-fix-design.md) §4.3.
- **OSEON-Tracking Lazy-Load (seit v1.16.0)**: Die OseonIndex-Seite rendert nur Top-Level-Gruppen-Rows. SubAuftraege + AGs werden per AJAX (`/Tracking/OseonGroupDetails`) nachgeladen. Bei aktivem `filterArticle` wird inline prefetcht. Wichtig: das Repo-Methode `GetSubOrdersForCustomerOrderAsync` darf WorkOperations NICHT auf `relevantOperationNames` filtern — die Relevanz-Logik im `OseonGroupViewModelBuilder` braucht ALLE Ops um den Spezialfall *"nur nicht-relevante Ops = Fertig"* zu erkennen.
- **Event-Delegation fuer AJAX-Loaded Content**: Im OseonIndex.cshtml-Inline-JS werden Sub-Row-Click-Handler ueber `document.addEventListener('click', ...)` mit `e.target.closest('.oseon-tree-sub')` gebunden — nicht ueber `forEach(row.addEventListener(...))`. Sonst kriegen AJAX-spaeter-eingefuegte Rows die Handler nicht.
- **Sage VB6-Booleans = BIT mit -1 fuer TRUE**: Sage-Tabellen (KHKArtikel, KHKArtikelvarianten, etc.) speichern Boolean-Spalten als BIT, aber mit `-1` fuer TRUE statt `1` (VB6-Legacy). Filter immer als `IstBestellartikel = -1 AND Aktiv = -1` formulieren, nicht `= 1` oder `= TRUE`. Bisher bekannte Spalten: `KHKArtikel.IstBestellartikel`, `KHKArtikel.Aktiv`.
- **PartiallyDelivered ist KEIN End-Status (seit v1.18.0)**: Bestellungen in Status `PartiallyDelivered` bleiben im `WarehousePicking/Index` bearbeitbar. Der Lager kann sie wieder oeffnen, Restlieferung einbuchen oder ein bisheriges "Restlieferung erwartet"-Item auf "endgueltig Fehlteil" umflaggen. Erst beim erneuten Close wird der Status neu abgeleitet (`Closed` wenn alle Items entweder vollstaendig geliefert oder als IsFinalShortage markiert sind).
- **WarehousePicking-Index Default-Filter zeigt Submitted+PartiallyDelivered (seit v1.18.0)**: `GetForWarehouseAsync` nimmt jetzt ein `WarehouseRequisitionStatus[]` statt einem `WarehouseRequisitionStatus?`. Ohne expliziten Filter werden beide "offene" Status angezeigt. OpenCount-Badge zaehlt entsprechend.
- **ShortageStatus-Enum statt IsFinalShortage-Bool (seit v1.19.0)**: `WarehouseRequisitionItem.IsFinalShortage` (bool) wurde durch `ShortageStatus` (Enum None=0/WillBeRestocked=1/NoRestock=2) ersetzt. Status-Ableitung: Order wird `PartiallyDelivered` wenn ein Item `WillBeRestocked` ist, sonst `Closed`. MissingParts-Liste filtert per Tab — `WillBeRestocked` = "Offene Fehlteile", `NoRestock` = "Wird nicht nachgeliefert". Werkbank-Karte zeigt beide Counts.
- **Migration v1.19.0 ist daten-destruktiv**: Die EF-Migration `ReplaceIsFinalShortageWithShortageStatus` (sowie das idempotente `SQL/65`) droppen die `IsFinalShortage`-Spalte NACH einer Daten-Konvertierung (`IsFinalShortage=true` → `NoRestock`; `false + Picked<Requested` → `WillBeRestocked`; sonst → `None`). Up() ist reversibel via Down(), aber Down() verliert die Unterscheidung zwischen None und WillBeRestocked (beide werden zu `IsFinalShortage=false`). **Backup der DB vor Produktions-Deploy** empfohlen.
- **Radio-3-State Pattern (Doppelklick → None)**: In `Details.cshtml` benutzen die ShortageStatus-Radios ein selbstgebautes 3-State-Verhalten — Doppelklick auf den aktiven Radio setzt ihn zurueck zu None. Implementiert via `mousedown`-Snapshot des `checked`-Status und `click`-Handler, der den Radio wieder unchecked setzt wenn er bereits aktiv war. Bootstrap-Radios unterstuetzen das per Default nicht.
- **MissingPartsController default mineOnly=true (seit v1.19.0)**: Werkbank-Sicht (`/MissingParts`) filtert per Default auf eigene Werkbaenke. Lager-Sicht laeuft ueber separaten `MissingPartsLagerController` (`/MissingPartsLager`) ohne mineOnly-Param. Layout-Menue zeigt "Meine Fehlteile" im oberen Bestellungen-Block und "Lager: Fehlteile" im canAccessStock-Sub-Block neben "Lager: Eingehende Listen". User ohne Workplace-Zuordnung sehen leere Liste mit Info-Banner statt automatischem Fallback auf alle Fehlteile. Werkbank-Dropdown in `/MissingParts` filtert die `AvailableWorkplaces` per `GetByUserIdAsync` — der Werker kann nur seine eigenen Werkbaenke als Filter auswaehlen.
- **Note vs NoteEinkauf (seit v1.19.0)**: Property im Code heisst `WarehouseRequisitionItem.Note`, UI-Label aber "Notiz Lager". Zweite Notiz fuer den Einkauf heisst sowohl im Code als auch im UI `NoteEinkauf` / "Notiz EK". Property-Rename Note→NoteLager bewusst NICHT durchgefuehrt (groesserer Diff, keine semantische Notwendigkeit fuer DB-Layer). Form-Param-Reihenfolge in Picking-Controller-Actions: `notes` vor `notesEinkauf` vor `shortageStatuses`.
- **Stammdaten Read/Edit-Pattern (seit v1.20.0)**: Class-Level der 10 Stammdaten-Controller traegt `[RequireMasterDataReadAccess]`, schreibende Actions verschaerfen mit `[RequireMasterDataAccess]`. ASP.NET kumuliert beide Filter — Edit-User passieren beide. Read-User (`masterdata_read`) sehen Index/Listing-Views, bekommen Buttons via Razor-Check `await _user.HasMasterDataAccessAsync()` ausgeblendet. Bei Erweiterung weiterer Module nach demselben Pattern: zusaetzlich `xxx_read`-Rolle + `RequireXxxReadAccess`-Filter + Class-Level-Umhaengung.
- **Rollen-Uebersicht (`/Users/RoleOverview`) ist hand-gepflegt (seit v1.20.0)**: Bei Aenderungen an Controller-Filtern (neuer Controller, Filter-Swap, neue Rolle) bitte `Views/Users/RoleOverview.cshtml` mit-updaten. Pflege-Hinweis steht in der View selbst. Verlinkt aus Users/Index (Page-Header) und Users/Create + Users/Edit ("Was darf welche Rolle?").
- **Lager-Worklist ist NICHT picker-zugaenglich (seit v1.20.0)**: WarehousePicking + MissingPartsLager nutzen `[RequireLagerProcessingAccess]` (admin/stock/stock_keyuser). Picker behalten Bestand + Bewegungshistorie (`[RequireStockAccess]` inkludiert picking weiter). Layout-Menue zeigt "Lager: ..."-Eintraege hinter `await CurrentUserService.CanProcessLagerAsync()`.
- **Admin-only Stammdaten (v1.20.0-Refactor)**: Benutzer, Rollen, Arbeitsplaetze, Settings, Aktivitaets-Protokoll und BDE-Schichtkalender sind nur fuer `admin` zugaenglich. masterdata-Rolle deckt nur die operativen Stammdaten-Sichten (Artikel, Lagerplaetze, Werkbaenke, Artikelkategorien, Artikelmerkmale, Empfaenger, Artikelgruppen-Zuordnungen) ab. Layout-Dropdown teilt das in zwei Bloecke: oben masterdata_read, unten admin-only Sub-Block (Users/Workstations/Roles + Settings/ServiceSettings/SyncLog). BDE-Block-Eintrag "Schichtkalender" haengt ebenfalls hinter `IsAdminAsync()`.
- **Model-Binder Bug: int[] mit leeren Strings (seit v1.20.0)**: ASP.NET-Core-Model-Binder fuer `int[]`-Parameter skippt leere String-Werte stillschweigend statt sie zu 0 zu defaulten. Form-Inputs `name="quantitiesPicked"` mit value="" werden NICHT als 0 ankommen — sie verschwinden, was die Array-Indices verschiebt. Folge: paralleler Mapping-Loop `for(idx=0; idx<itemIds.Length; idx++) qtyDict[itemIds[idx]] = quantitiesPicked[idx]` haut die Werte auf die falschen Items. Loesung in WarehousePicking/Details.cshtml: `normalizeEmptyQuantitiesToZero()` setzt vor jedem Submit alle leeren `quantitiesPicked`-Inputs auf "0", und `collectProgress()` sendet "0" statt empty string. Bei kuenftigen int[]-Form-Bindings das gleiche Pattern anwenden.
- **Date-Picker programmatic value-Setzung loest kein input-Event aus (seit v1.20.0)**: In `wwwroot/js/table-filter.js` werden Filter-Inputs vom Kalender-Popup (KW-Klick, Tag-Klick, "Filter entfernen") programmatisch via `input.value = '...'` gesetzt. Das loest aber KEIN `input`-Event aus — der bestehende Event-Listener (im Server-Mode `scheduleServerNavigate`, im Client-Mode `applyFilters`) wird nicht aufgerufen. Pattern: nach jedem programmatischen `input.value = ...` ein `input.dispatchEvent(new Event('input', { bubbles: true }))` dispatchen.
- **Server-Filter-Mode: kein client-seitiges applyFilters bei Init (seit v1.20.0)**: In `wwwroot/js/table-filter.js` lief beim Page-Init UNCONDITIONAL `applyFilters()` — auch bei Tabellen mit `data-server-column-filter="true"`. Das fuehrte zu doppelter Filterung (Server + Client), wobei der Client-Filter auf DOM-Cell-Text laeuft und mit Datumsspalten ("dd.MM.yyyy KWxx") problematisch war. Fix: applyFilters() bei Init nur noch im Client-Mode aufrufen.
- **Stammdaten Read/Edit-Pattern Update (seit v1.20.0)**: Der Class-Level der **6 operativen** Stammdaten-Controller (Articles, StorageLocations, ProductionWorkplaces, OrderRecipients, ArticleCategories, ArticleAttributes) traegt `[RequireMasterDataReadAccess]`. Die anderen 6 (Users, Roles, Workstations, Settings, SyncLog, BdeShiftCalendar) sind **admin-only** via `[RequireAdminAccess]`. Layout-Dropdown teilt das in masterdata_read-Block (oben) und admin-only Sub-Block (unten). Der frueher dokumentierte Wert "10 Stammdaten-Controller" ist veraltet — es sind jetzt 6+6.
- **Universal-Filter-Pattern (seit v1.21.0)**: ColumnMap-Getter MUESSEN den gerenderten Zellentext liefern (Badges/Ja-Nein/Datumsformate), Apply vor Pagination, TotalCount aus gefilterter Menge. Bei SQL-paginierten Repos: Query UND Count identisch filtern (BdeBookings-Pattern mit Expression-Trees, kein EF.Functions.Like wegen InMemory-Tests). table-filter.js/column-preferences.js sind Single-Table — eine filterbare Tabelle pro gerenderter Seite (BdeMasterData rendert pro Tab-Request nur eine).
- **IsDone vs IsDonePicking — Lese-Seite (seit v1.21.1)**: `Picking/ToggleDone` schreibt `PickingStatus.IsDonePicking` (Sage-`IsDone` darf nicht beschrieben werden — der Sage-Sync wuerde es ueberschreiben). ALLE Stellen, die "FA erledigt" anzeigen oder filtern (FA-Liste, Leitstand, Picking-Worklist), muessen `IsDone || IsDonePicking` pruefen. Von v1.11 bis v1.21.0 las keine Query das Flag — der Abschliessen-Button war wirkungslos. **Seit v1.22.0** filtern AUCH `FaCompletion/Index` (im `if (!showDone)`-Block) und `FaWorklist/Index` (immer, wie Sage-`IsDone`) auf `IsDone || IsDonePicking` — vorher pruefte der Vorbau nur Sage-`IsDone`, sodass komm-erledigte FAs dort weiterhin auftauchten obwohl sie aus der FA-Liste verschwunden waren. Voraussetzung: `ProductionOrderRepository.GetAllOrderedAsync()` laedt `PickingStatus` per `.Include` (sonst ist `o.PickingStatus` null und das Flag wird nie ausgewertet).
- **FaWorkSteps ersetzt ProductionOrderAssemblyGroups (v1.22.0)**: Genau EINE Zeile je FA+WorkStep (UNIQUE), `IsRemoved` toggelt sie statt Delete. **IsRemoved-Semantik**: manuelle Abwahl setzt `IsRemoved=1` — der Detection-Sync darf solche Zeilen NIE re-adden (manuelle Abwahl gewinnt); manuelles Wieder-Hinzufuegen reaktiviert die Zeile (`IsRemoved=0`, `Source='Manual'`). Die Detection arbeitet **nur-hinzufuegend** (entfernt nie). Toggle-API: `/api/fa-work-steps/toggle` (AG an/abwaehlen, picking ODER fa_completion) + `/api/fa-work-steps/toggle-completed` (Erledigt-Haken, vorbau). **DEPLOY-KRITISCH**: Der AssemblyGroups-Folge-MERGE im AgentJob `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` wurde ersatzlos entfernt — Job-Skript MUSS im selben Wartungsfenster wie das DB-Deploy aktualisiert werden, sonst schlaegt der gesamte FA-Import fehl (Cutover-Doc `docs/superpowers/cutover/2026-06-12-fa-vorbau-cutover.md`).
- **FaWorkStep-Detection haengt NICHT am BomCache-ContentHash-Pfad**: Der BomCache-Sync skippt Artikel mit unveraendertem `ContentHash` — wuerde die Erkennung dort haengen, bekaemen neue FAs zu bereits gecachten Artikeln nie eine Erkennung. Deshalb laeuft `FaWorkStepDetectionService` als **eigener idempotenter Schritt** im SyncWorker direkt NACH dem BomCache-Sync (Setting `Sync:FaWorkStepDetectionEnabled`, eigener Aktivitaets-Protokoll-Eintrag "FaWorkStepDetection"): alle offenen FAs mit BOM-Cache-Eintrag werden je Lauf gegen die WorkStep-Suchbegriffe gematcht (kommasepariert, ToLower().Contains auf Bezeichnung1/2). FAs ausserhalb des BomCache-Fensters werden nicht automatisch erkannt — manuelle Pflege immer moeglich.
- **Leitstand-Spalten VK-VA statisch trotz erweiterbarem Katalog (v1.22.0)**: Der WorkStep-Katalog ist erweiterbar, aber der Leitstand zeigt bewusst NUR die 5 statischen Spalten VK/VL/VE/VT/VA (Bool-Properties HasCooling/HasFan/..., Filter-Keys cooling/fan/electric/doors/superstructure unveraendert; Datenquelle `GetWorkStepPivotAsync` mit 1000er-Chunking). Neue Katalog-AGs erscheinen NICHT automatisch im Leitstand (YAGNI-Entscheid Spec §8/§11) — wer eine sechste Spalte braucht, muss View + ViewModel + Pivot erweitern.
- **Zwei Completion-Status auf FaWorkStep — IsSpecComplete vs IsCompleted (v1.22.0)**: `IsSpecComplete` (FA-Vervollstaendigung, Switch "Vollstaendig definiert" — Merkmale/AGs festgelegt, Planer) blendet die FA **NICHT** aus der Abarbeitungsliste aus. `IsCompleted` (FA-Abarbeitungsliste, "Erledigt"-Checkbox — Vorbau physisch gebaut, Werker) ist das EINZIGE Flag mit Ausblende-Logik (`FaWorklistController`: `mappedSteps.All(f => f.IsCompleted)`). FA-Vervollstaendigung schreibt `IsSpecComplete` (`FaCompletionController.ToggleSpecComplete` → `SetIsSpecCompleteAsync`), Abarbeitungsliste schreibt `IsCompleted` (`/api/fa-work-steps/toggle-completed` → `SetIsCompletedAsync`) — NICHT verwechseln. Migration 69 (`20260615070236_SplitFaWorkStepCompletion`) verschob das alte `IsCompleted` nach `IsSpecComplete` (Audit-Felder `SpecCompletedAt/By`) und setzte `IsCompleted` zurueck. FA-Vervollstaendigung-Counts zaehlen `IsSpecComplete` (`FaWorkStepCounts.SpecCompleteCount`).

## Standard-Daten (Neuinstallation)

| Typ | Wert | Beschreibung |
|-----|------|-------------|
| Benutzer | `admin` / Passwort leer | Standard-Admin, Seeding in `Program.cs` |
| Lagerplatz | `NAN` | Fallback fuer negative Buchungen, Seeding in `Program.cs` |

## AppSettings (DB-Tabelle)

| Key | Default | Beschreibung |
|-----|---------|-------------|
| `KommissionierTage` | `4` | Arbeitstage vor Fertigungstermin |
| `VorkommissionierTage` | `1` | Tage vor Kommissioniertermin |
| `BeschichtungTage` | `10` | Arbeitstage vor Kommissionierung |
| `WarningThresholdPercent` | `150` | Meldebestand Warnschwelle (%) |
| `CriticalThresholdPercent` | `100` | Meldebestand kritisch (%) |
| `NegativeBuchungErlaubt` | `false` | Negative Buchungen erlauben |
| `NegativeBuchungLagerplatz` | `NAN` | Fallback-Lagerplatz bei negativem Bestand |
| `BeschichtungAbholtage` | `Dienstag,Donnerstag` | Wochentage fuer Beschichtungs-Abholung |
| `TeileverfolgungAktiv` | `false` | Teileverfolgungs-Modul aktiviert |
| `OseonRueckmeldungAktiv` | `false` | Rueckmeldungen an OSEON |
| `SageRueckmeldungAktiv` | `false` | Rueckmeldungen an Sage |
| `QrMitFaNummer` | `false` | QR-Code enthaelt FA-Nummer an 3. Stelle |
| `OseonAmpelGelbTage` | `1` | OSEON Ampel: Gelb ab X Tagen vor Termin |
| `OseonAmpelBlauTage` | `2` | OSEON Ampel: Blau ab X Tagen vor Termin |
| `BestellungenAktiv` | `false` | Bedarfsmeldungen aus Stueckliste |
| `LeitstandAktiv` | `false` | Leitstand: Freigabe und Priorisierung |
| `KommissionierungMitZuweisung` | `false` | Kommissionierung mit Picker-Zuweisung |
| `FaCompletionAktiv` | `false` | FA-Vervollstaendigungs-Modul aktivieren (v1.14.0+) |
| `LackierteilKategorieName` | (leer) | Artikelkategorie fuer Lackierteile. Leer = Feature inaktiv |
| `BdeAktiv` | `false` | BDE-Modul aktiviert |
| `BdeNurFaMeldung` | `false` | Vereinfachter BDE-Modus (FA statt AG) |
| `BdeDefaultArbeitsgang` | (leer) | Default-AG Name fuer vereinfachten Modus |
| `BdeMehrfachBuchungProOperator` | `false` | Ein Mitarbeiter darf mehrere parallele Buchungen haben (auf verschiedenen Arbeitsgaengen) |
| `BdeMehrfachBuchungProArbeitsgang` | `false` | Ein Arbeitsgang darf mehrere parallele Buchungen haben (durch verschiedene Mitarbeiter) |
| `BdeGleichzeitigerAbschlussBeiMehrfachStart` | `false` | Alle parallel gestarteten Produktionsbuchungen eines Mitarbeiters muessen gemeinsam fertiggemeldet werden (nur wirksam wenn BdeMehrfachBuchungProOperator aktiv) |
| `BdeSchichtkalenderAktiv` | `false` | Schichtkalender + Auto-Pause am Schichtende aktiv |
| `OseonReportingHorizonDays` | `10` | Reporting: Tage in die Zukunft (Default-Horizont) |
| `OseonReportingOverdueLookbackDays` | `90` | Reporting: Tage in die Vergangenheit fuer Ueberfaellig-Slice |
| `DefaultLagerbestellempfaengerId` | (leer) | OrderRecipientGroup-ID fuer Lagerbestellungen (leer = Submit blockt) |

## Service-Konfiguration (appsettings.json / ServiceSettings DB)

| Key | Default | Beschreibung |
|-----|---------|-------------|
| `Sync:ProductionOrdersEnabled` | `true` | Sage-WA-Import aktiv |
| `Sync:ArticlesEnabled` | `true` | Sage-Artikel-Import aktiv |
| `Sync:OseonTrackingEnabled` | `false` | OSEON-Tracking-Sync aktiv |
| `Sync:EnaioDmsEnabled` | `false` | enaio DMS-Sync aktiv |
| `Sync:PartRequisitionEmailEnabled` | `false` | Bedarfsmeldungs-E-Mail-Versand |
| `Sync:OseonArticleCategoryEnabled` | `false` | OSEON-Artikelkategorie-Sync |
| `Sync:BomCacheEnabled` | `false` | BOM-Cache-Sync aktiv |
| `Sync:BomCacheWeeks` | `8` | Wochen in die Zukunft cachen |
| `Sync:BomCacheMaxOrders` | `200` | Max. Auftraege im Cache |
| `Sync:CoatingDetectionEnabled` | `false` | Lackierteil-Erkennung aktiv |
| `Sync:FaWorkStepDetectionEnabled` | `false` | FA-Arbeitsgang-Erkennung aus BOM-Cache aktiv (v1.22.0) |
| `Sync:BdeAutoPauseIntervalMinutes` | `60` | BDE Auto-Pause-Worker Intervall (Minuten) |
| `Sync:FeiertagSyncEnabled` | `false` | Feiertags-Sync von date.nager.at aktiv |
| `Sync:FeiertagCountryCode` | `AT` | Laendercode fuer Feiertags-Sync (ISO 3166-1 alpha-2) |
| `Sync:FeiertagRegion` | (leer) | Optionaler Bundesland-Code (z.&nbsp;B. AT-3 NOe, AT-6 Stmk) |
| `Sync:FeiertagJahreVoraus` | `2` | Jahre in die Zukunft synchronisieren |
| `Sync:WarehouseRequisitionEmailEnabled` | `false` | Aktiviert E-Mail-Versand fuer Lagerbestellungen im SyncWorker |
| `Sync:LagerplaetzeEnabled` | `false` | Sage-Lagerplatz-Stammdaten-Sync aktiv |
| `Sync:LagerbestandEnabled` | `false` | Sage-Lagerbestand-Sync aktiv (Phase 2) |
| `Sync:LagerbestandIntervalMinutes` | `0` | Eigenes Intervall in Min (0 = nutzt SyncIntervalMinutes) |
| `WorkerSettings:SyncIntervalMinutes` | `15` | Sync-Intervall |
| `WorkerSettings:SyncDryRun` | `false` | DryRun-Modus |
| `Security:AdGroupCacheMinutes` | `5` | AD-Gruppen-Cache Dauer |

Connection Strings: `DefaultConnection` (WMS), `SageConnection` (Sage), `OseonConnection` (OSEON), `EnaioDmsConnection` (enaio)

## Migrations-Workflow

1. Model aendern
2. `dotnet ef migrations add <Name>` → generiert Migration in `Migrations/`
3. SQL-Script: `SQL/XX_<Name>.sql` mit `OBJECT_ID`-Guard (idempotent)
4. `__EFMigrationsHistory` Insert in separatem Batch
5. App startet mit `db.Database.Migrate()` → ueberspringt bereits angewendete
6. `SQL/00_FreshInstall.sql` konsolidiert aktualisieren

## SQL Agent Jobs

`SQL/AgentJobs/` — wiederkehrende Import-Scripts:

| Script | Quelle → Ziel |
|--------|--------------|
| `01_Import_Produktionsauftraege.sql` | `vw_AKE_Kommissionierung_WAListe` → `ProductionOrders` |
| `02_Import_Artikel.sql` | `KHKPpsRessourcenPositionen` + `KHKArtikel` → `Articles` |

Bei DB-Strukturaenderungen (neue Pflichtfelder) muessen diese Scripts angepasst werden.

## OSEON Teileverfolgung

- 3-Ebenen-Baumstruktur: KundenAuftragsNr → Subauftraege → Arbeitsgaenge
- Ampelsystem: Rot/Gelb/Blau/Gruen/Grau basierend auf Soll-Terminen und Status
- AG-Konfiguration: `OseonOperationConfig`-Tabelle — Offset-Tage, OSEON-Relevanz pro Arbeitsgang
- Delta-Sync via `LastChangedInOseon` mit 5 Min Puffer
- Status-Codes: 10=Unvollstaendig, 20=Gueltig, 30=Freigegeben, 60=In Arbeit, 70=Gesperrt, 90=Fertig, 95=Storniert
- Server-seitige Paginierung (25 Gruppen/Seite)

## Pagination & Server-Side Spaltenfilter (seit v1.14.0)

- **Shared Pagination-Infrastruktur** in [Services/PageSize.cs](IdealAkeWms/Services/PageSize.cs) + [Models/ViewModels/PaginationState.cs](IdealAkeWms/Models/ViewModels/PaginationState.cs) + [Views/Shared/_Pagination.cshtml](IdealAkeWms/Views/Shared/_Pagination.cshtml)
- **Erlaubte Page-Sizes**: 25 (Default), 50, 100, 0 (= "Alle", in SQL gecappt auf `PageSize.AllCap` = 5000)
- **User-Default**: `User.DefaultPageSize` (nullable int, NULL = System-Default 25). UI in Profile + Users/Edit. Resolve-Reihenfolge: explizit gewaehlte URL-Pagesize > User-Default > System-Default
- **JS-Handler**: in `site.js` — Klick auf `.page-link[data-page]` setzt `?page=N`, Change auf `.pagination-page-size` setzt `?pageSize=N` + reset `page=1`. Andere Query-Parameter bleiben erhalten
- **Server-Side Column Filter**: Tabellen mit `data-server-column-filter="true"` triggern bei Filter-Input debounced (500ms) URL-Navigation mit `?colf_<col-key>=value`. Filter-Werte werden aus URL (nicht sessionStorage) restored
- **Filter-Mini-Syntax** (Server + Client identisch): OR mit `,` (z.B. `960,886`), NOT mit `!` (z.B. `!960`, `!960,886`)
- **Pro Liste Mapping** (im Repo oder Controller): jeder Col-Key → Property. Helper [Services/ColumnFilterHelper.cs](IdealAkeWms/Services/ColumnFilterHelper.cs) parsed Tokens und stellt In-Memory-`Apply<T>` bereit
- **Datumsspalten**: Server-seitig in C# nach Termin-Berechnung gefiltert (Format `dd.MM.yyyy KWxx` lowercase). Wenn Datumsfilter aktiv → Controller laedt alle Text-gefilterten Rows (kein SQL Skip/Take), filtert in C#, paginiert dann
- **Aktuelle Liste der server-filter-Tabellen (Stand v1.21.0)**: ProductionOrders (FA-Liste), PickingLeitstand, StockOverview (Bestand), StockMovements (Bewegungshistorie), Articles, MissingParts, MissingPartsLager, WarehousePicking (Eingehende Listen), WarehouseRequisitions (Meine Lagerbestellungen), Picking (Kommissionierung, mit KW-Datumsfilter), PartRequisitions (Bedarfsmeldungen), StorageLocations (Lagerplaetze), BdeBookings (SQL-Level), Users, Roles, Workstations, ProductionWorkplaces, ArticleCategories, ArticleAttributes, OrderRecipients, BdeMasterData (3 Tabs mit eigenen view-keys), FaCompletion, SyncLog. **Client-Filter** (vorgefilterte unpaginierte View): Tracking/ByWorkplace

## Responsive Design

- Mobile-First Media-Queries in `site.css`
- Touch Targets min-height 44px (WCAG), `font-size: 16px` auf Mobile-Inputs (iOS Zoom)
- `.table-responsive` mit Sticky Scrollbar (`site.js`, `IntersectionObserver`)
- Page Headers: `d-flex justify-content-between` immer mit `flex-wrap gap-2`

## Test-Setup

- xUnit + FluentAssertions + Moq + EF InMemory
- `TestApplicationDbContext` ueberschreibt `SaveChanges` fuer RowVersion-Handling
- `TestDbContextFactory.Create()` erzeugt frischen InMemory-Kontext

## Logging (Serilog)

- Daily Rolling: `logs/idealakewms-YYYYMMDD.log`, 30 Tage Aufbewahrung
- Default `Information`, EF Core auf `Warning`

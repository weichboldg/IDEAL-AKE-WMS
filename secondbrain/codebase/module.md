---
type: codebase-karte
updated: 2026-07-27
---
# Module

Navigierbare Karte der Projektstruktur. Kein Code-Dump — Zweck plus Einstiegspfad.
Geschwister: [[controller]] · [[services]] · [[datenmodell]] · [[integrationen]]

## Solution

`../../IdealAkeWms.slnx` — vier Projekte:

| Projekt | Typ | Zweck |
|---|---|---|
| `../../IdealAkeWms/` | ASP.NET Core 10.0 MVC | Web-Anwendung: UI + API. Enthaelt das Datenmodell, alle Repositories und den `ServiceSettingDefinitions`-Katalog — auch fuer den Service. |
| `../../IDEALAKEWMSService/` | Windows-Service (.NET 10) | Hintergrundjobs: Syncs, Mails, BDE-Auto-Pause, Cleanup. **Referenziert das Web-Projekt** (ProjectReference). |
| `../../IdealAkeWms.Tests/` | xUnit | Tests zur Web-Anwendung (~955). |
| `../../IDEALAKEWMSService.Tests/` | xUnit | Tests zum Service (~153). |

**Abhaengigkeitsrichtung:** Service → Web. Nie umgekehrt. Grund und Historie:
Fallstrick „Service referenziert das Web-Projekt" in [[fallstricke]].

## Web-Anwendung — `../../IdealAkeWms/`

| Ordner | Inhalt |
|---|---|
| `Controllers/` | MVC- und API-Controller → [[controller]] |
| `Controllers/Api/` | Reine API-Controller (Picking, Photo, PartRequisitions, WarehouseRequisitions, UserViewPreferences) |
| `Filters/` | 29 `RequireXxxAccessAttribute` + Feature-Gates → [[controller]], [[0006-rollenkonzept-statische-keys-mit-admin-wildcard]] |
| `Models/` | Entitaeten, Enums, Konstanten (`RoleKeys`, `AppSettingKeys`, `ServiceSettingDefinitions`) → [[datenmodell]] |
| `Models/ViewModels/` | 56 ViewModels inkl. `ColumnDefinitions`, `PaginationState` |
| `Data/ApplicationDbContext.cs` | EF-Kontext (Web **und** Service) |
| `Data/Repositories/` | Repository-Interfaces + Implementierungen + Cache-Decorators → [[services]] |
| `Services/` | Fachservices, Helper, `SyncLogger/`, `Oseon/` → [[services]] |
| `Middleware/` | `WindowsAutoLoginMiddleware`, `IChallengeIssuer` → [[0002-dual-auth-session-login-plus-windows-sso]] |
| `Migrations/` | EF-Migrationen (SQL-Nummernkreis aktuell bis 81) → [[0004-migrations-und-sql-disziplin]] |
| `Views/` | 36 Feature-Ordner + `Shared/` (Partials `_Pagination`, `_EnaioDmsBadges`, `_FaWorkStepStatusSelect`, `_ArtikelinfoTile`) |
| `wwwroot/js/` | `table-filter.js`, `column-preferences.js`, `barcode-scanner.js`, `bde-terminal.js`, `bde-cockpit.js`, `fa-work-step-status.js`, `oseon-tracking-lazy.js`, `photo-upload.js`, `site.js` |
| `Program.cs` | DI, Middleware-Pipeline, Seeding (admin-User, `NAN`-Lagerplatz, ServiceSettings-Katalog) |
| `Media/` | Foto-Uploads aus der Kommissionierung |

**Middleware-Reihenfolge** (fix, in `Program.cs`): HttpsRedirection → Routing → Authentication →
Authorization → **Session** → **WindowsAutoLoginMiddleware** → SerilogRequestLogging →
**LoginRedirect** → StaticFiles → MapControllerRoute.
Die LoginRedirect-Ausnahmeliste zaehlt Account-Pfade **einzeln** auf — siehe [[fallstricke]].

**Session-Parameter:** Timeout 8 Stunden, Cookie `IdealAkeWms.Session`, Session-Keys `AppUserId`
(Int32), `AppUserName` (String) plus `WindowsUserName` fuers Audit. Die Session ist die **einzige**
Autorisierungsquelle → [[0002-dual-auth-session-login-plus-windows-sso]].
DataProtection-Keys muessen **persistent** sein (`Program.cs`) — ephemere Keys nach einem
App-Pool-Recycle machen Antiforgery-Token unentschluesselbar (400er).

## Frontend-Konventionen

| Thema | Regel |
|---|---|
| Corporate Design | CSS-Variablen `--ake-primary: #053153` (Dunkelblau), `--ake-secondary: #43A6E2` (Hellblau), `--ake-orange: #E87A1E` (Warnung/Ausbuchung). Vollstaendig in `../README.md` → „Corporate Design". |
| Mobile-First | Media-Queries in `wwwroot/css/site.css`. Touch-Targets min. **44 px** Hoehe (WCAG), `font-size: 16px` auf Mobile-Inputs (sonst zoomt iOS beim Fokus). |
| Tabellen | Immer in `<div class="table-responsive">`; Sticky-Scrollbar per `IntersectionObserver` in `site.js`. |
| Page-Header | `<h2 class="page-header">`, Buttonzeile `d-flex justify-content-between flex-wrap gap-2` — `flex-wrap gap-2` nie weglassen, sonst brechen Buttons auf Mobile aus. |
| Alerts | Nur `TempData["SuccessMessage"]` und `TempData["WarningMessage"]`; Fehler ueber ModelState. |
| Skript-Reihenfolge | `column-preferences.js` **vor** `table-filter.js` → [[fallstricke]]. |

Listen-Views folgen zusaetzlich verbindlich
[[0005-listen-view-pattern-mit-server-side-spaltenfilter]].

### JS-Kontrakte der Listen-Infrastruktur

| Datei | Kontrakt |
|---|---|
| `site.js` | Klick auf `.page-link[data-page]` setzt `?page=N`; Change auf `.pagination-page-size` setzt `?pageSize=N` **und** `page=1`. Andere Query-Parameter bleiben erhalten. Ausserdem: Sticky-Scrollbar via `IntersectionObserver`. |
| `table-filter.js` | Server-Mode navigiert **erst bei ENTER** (`onServerFilterKeydown` → `applyServerFilters`); Client-Mode filtert live beim Tippen. Filter-Werte werden im Server-Mode aus der **URL** restored (nicht aus `sessionStorage` — das gilt nur im Client-Mode). Programmatische Pfade rufen `applyColumnFilterNow()`. |
| `column-preferences.js` | Spalten-Sichtbarkeit/-Reihenfolge je View-Key; dispatcht `column-preferences-ready`. Muss **vor** `table-filter.js` geladen werden. |

### Inventar: Tabellen im Server-Filter-Mode

Stand v1.21.0 (nach dem Universal-Filter-Rollout) — als Referenz, welche Views das Muster schon
tragen: ProductionOrders (FA-Liste), PickingLeitstand, StockOverview, StockMovements, Articles,
MissingParts, MissingPartsLager, WarehousePicking, WarehouseRequisitions, Picking (mit
KW-Datumsfilter), PartRequisitions, StorageLocations, BdeBookings (SQL-Level), Users, Roles,
Workstations, ProductionWorkplaces, ArticleCategories, ArticleAttributes, OrderRecipients,
BdeMasterData (3 Tabs mit eigenen View-Keys), FaCompletion, SyncLog. Seit v1.26.0 zusaetzlich
FaWorklist (View-Key „FaWorklist" ist in `ColumnDefinitions.GetByViewKey` registriert).

**Client-Mode:** Tracking/ByWorkplace (vorgefiltert, unpaginiert).
**Sonderfall Client-Mode MIT Spalten-Prefs:** WarehousePicking/Details (View-Key
`WarehousePickingDetails`) — Client-Sort/-Filter plus persistierte Spalten, und der Druck spiegelt
Spalten, Sort und Filter.

## Windows-Service — `../../IDEALAKEWMSService/`

| Ordner / Datei | Inhalt |
|---|---|
| `Workers/SyncWorker.cs` | Haupt-Loop: alle Sync-Bloecke, die ersten einzeln via `RunResilientAsync` gekapselt |
| `Workers/NotificationWorker.cs` | Mail-Versand (Bedarfsmeldungen, Lagerbestellungen) |
| `Workers/CleanupWorker.cs` | 24h-Takt, „Bereinigung" — erster Cleaner: Aktivitaets-Protokoll |
| `Services/` | Sync-Services + unit-testbare Planner/Reconciler → [[services]] |
| `Common/ServiceSettings.cs` | DB-first Konfigurationszugriff (`GetBoolSafeAsync`, `GetIntSafeAsync`) → [[0008-servicesettings-db-first-mit-typisiertem-katalog]] |
| `Common/ConnectionStrings.cs`, `Common/BulkCopyHelper.cs` | Infrastruktur |
| `appsettings.json` | Nur noch Default-Referenz + `ConnectionStrings` / `MailSettings` / `Security` |

## SQL — `../../SQL/`

| Pfad | Inhalt |
|---|---|
| `00_FreshInstall.sql` | Konsolidiertes Schema fuer Neuinstallationen + `__EFMigrationsHistory`-Insert |
| `XX_<Name>.sql` | Idempotente Einzelskripte je Migration (aktuell bis `81_AddProductionOrderExtraInfo.sql`) |
| `AgentJobs/01_Import_Produktionsauftraege.sql` | `vw_AKE_Kommissionierung_WAListe` → `ProductionOrders` (+ Folge-MERGEs fuer PickingStatus/BdeStatus) |
| `AgentJobs/02_Import_Artikel.sql` | `KHKPpsRessourcenPositionen` + `KHKArtikel` → `Articles` |

Die AgentJobs sind **Teil des Deploy-Vertrags** — bei Schema-Aenderungen im selben
Wartungsfenster mitziehen. Siehe [[0004-migrations-und-sql-disziplin]].

## Dokumentation — `../../docs/`

| Datei | Inhalt |
|---|---|
| `TESTSZENARIEN.md` | Single Source of Truth der manuellen Abnahme (55 Kapitel) → [[testszenarien-index]] |
| `CLAUDE-full-backup-2026-07.md` | Vorfassung der CLAUDE.md vor der Brain-Umstellung (Archiv) |
| `superpowers/specs/` | 55 Design-Specs (historisch, vor dem Brain) → [[feature-map]] |
| `superpowers/plans/` | Umsetzungsplaene zu den Specs |
| `superpowers/cutover/` | Cutover-Dokumente fuer deploy-kritische Releases |

Im Repo-Root liegen weiterhin `README.md` (Anwender-/Betriebsdoku), `PROJECT_STATUS.md`
(Release-Historie + offene Deploy-Schritte) und `ANALYSIS.md` (Alt-Analyse 05/2026).

## Tests

| Projekt / Ordner | Schwerpunkt |
|---|---|
| `IdealAkeWms.Tests/Controllers` | Controller-Verhalten (ModelState, Redirects, Filterlogik) |
| `IdealAkeWms.Tests/Filters` | Zugriffsschutz-Attribute |
| `IdealAkeWms.Tests/Middleware` | `WindowsAutoLoginMiddleware`-Entscheidungslogik |
| `IdealAkeWms.Tests/Repositories` | Repository-Queries gegen InMemory |
| `IdealAkeWms.Tests/Services`, `Models`, `Helpers`, `Integration` | Fachlogik, Enums, Helper |
| `IDEALAKEWMSService.Tests/Services` | Planner/Reconciler/Coverage (die unit-testbaren Kerne) |
| `IDEALAKEWMSService.Tests/Workers`, `Common`, `Helpers` | Worker-Invarianten, Settings-Zugriff |

Setup: xUnit + FluentAssertions + Moq + EF InMemory. `TestApplicationDbContext` ueberschreibt
`SaveChanges` fuer RowVersion-Handling, `TestDbContextFactory.Create()` liefert einen frischen
Kontext. **Nicht abgedeckt** (Manual-UAT): raw SQL, Negotiate-Handshake, LDAP, echte
Fremdsystem-Reads — siehe [[fallstricke]] Abschnitt 8.

## Logging

Serilog, daily rolling: `logs/idealakewms-YYYYMMDD.log`, 30 Tage Aufbewahrung. Default
`Information`, EF Core auf `Warning`. Fachliche Lauf-Protokolle liegen **nicht** hier, sondern im
Aktivitaets-Protokoll → [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]].

## Hosting / IIS (was auch die App betrifft)

Vollstaendige Installations- und IIS-Anleitung: `../README.md`. Hier nur, was Code-Verhalten
erklaert:

| Einstellung | Wert | Warum es den Code betrifft |
|---|---|---|
| Hosting-Modell | **in-process** (`web.config`) | Bestimmt, dass `IISServerDefaults.AuthenticationScheme` das richtige Auth-Schema ist und `AddNegotiate()` **falsch** waere → [[0002-dual-auth-session-login-plus-windows-sso]] |
| IIS-Auth-Modi | `windowsAuthentication=true` **UND** `anonymousAuthentication=true` | Anonymous haelt den Formular-Fallback offen; ohne ihn sind Nicht-Domaenen-Geraete ausgesperrt |
| `requestTimeout` | `00:05:00` (statt 2 min) | Noetig fuer datenreiche Seiten (OSEON-Teileverfolgung) — deshalb dort auch Lazy-Load |
| App-Pool Idle Timeout | `0`, Start Mode `AlwaysRunning`, Preload `True` | Kein Kaltstart; **und**: jedes App-Pool-Recycling verwirft ephemere DataProtection-Keys → Antiforgery-Token nicht mehr entschluesselbar → 400. Deshalb persistente Keys in `Program.cs` UND wenig Recycling. Siehe [[fallstricke]] Abschnitt 4. |

Regelmaessiges Recycling entweder `0` oder hoch (z. B. 1740 min = 29 h).

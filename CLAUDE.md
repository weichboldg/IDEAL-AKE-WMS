# IdealAkeWms — Kontext fuer KI-Assistenten

## Workflow

- **Plan vor Code** — Bei 3+ Schritten oder Architekturentscheidungen: Plan-Modus verwenden
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
| `[RequireMasterDataAccess]` | admin, masterdata | UsersController, WorkstationsController, SettingsController, RolesController |
| `[RequirePickingAccess]` | admin, picking | ProductionOrdersApiController, PickingController (Actions ausser Index) |
| `[RequireTrackingAccess]` | admin, tracking | TrackingController |
| `[RequireStockAccess]` | admin, stock, stock_keyuser, picking | StockMovementsController, StockOverviewController, WarehousePickingController |
| `[RequireStockKeyUserAccess]` | admin, stock_keyuser, picking | StockMovementsController (Lagerplatz ausbuchen/umbuchen) |
| `[RequirePickingOrTrackingAccess]` | picking ODER tracking | ProductionOrdersController.Index |
| `[RequirePickingOrStockAccess]` | picking ODER stock | PartRequisitionsController, OrderRecipientGroupsController, WarehouseRequisitionsController, WarehouseRequisitionsApiController |
| `[RequireLeitstandAccess]` | admin, leitstand | ProductionOrdersController (ToggleRelease, BulkRelease, SetPriority) |
| `[RequireReportingAccess]` | admin, reporting | OseonReportingController |
| `[RequireBdeUserAccess]` | admin, bde_user, bde_shiftlead, bde_admin | BdeTerminalController, BdeApiController |
| `[RequireBdeShiftleadAccess]` | admin, bde_shiftlead, bde_admin | BdeCockpitController, BdeBookingsController (Index), BdeMasterDataController |
| `[RequireBdeAdminAccess]` | admin, bde_admin | BdeBookingsController (Edit/Cancel), BdeMasterDataController (Terminals) |
| *(kein Filter)* | jeder eingeloggte User | UserViewPreferencesApiController (Login-Check, kein Rollen-Filter) |

**Sonderfaelle:**
- `ProductionOrdersController.Index` prueft Berechtigungen manuell (CanPick OR CanViewTracking OR CanManagePickingRelease)
- Admin-Reset fuer View-Einstellungen laeuft ueber `UsersController.ResetViewPreferences` (`[RequireMasterDataAccess]`)

## Rollenkonzept

`Role`-Tabelle + `UserRole`-Junction (Many-to-Many), statische Keys in `RoleKeys.cs`. Admin-Wildcard ueberspringt alle Pruefungen. Rollen koennen optional AD-Gruppen haben (`Role.AdGroup`).

| Key | Beschreibung |
|-----|-------------|
| `admin` | Vollzugriff |
| `masterdata` | Benutzer, Arbeitsplaetze, Einstellungen |
| `picking` | Picking + vollstaendiger Lagerzugriff |
| `stock` | Einbuchung, Ausbuchung, Bestaende |
| `stock_keyuser` | Lager + Lagerplatz ausbuchen/umbuchen |
| `tracking` | OSEON Auftraege + Rueckmeldungen |
| `reporting` | Betriebsdaten / BDE (Zukunft) |
| `leitstand` | Produktionsauftraege freigeben und priorisieren |
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
- **Leitstand Index-Action hat kein Filter-Attribut**: Prueft Berechtigungen manuell (CanPick OR CanViewTracking OR CanManagePickingRelease)
- **AppSettings-Tabelle**: KEIN AuditableEntity — nur Key (PK), Value, Description
- **Beschichtungstermin Backward-Compat**: Wenn `LackierteilKategorieName` leer → Beschichtungstermin fuer ALLE Auftraege
- **BDE-Buchung Mehrfach-Regel**: Ohne Konfiguration darf ein Operator nur eine aktive Buchung haben und ein Arbeitsgang nur eine aktive Buchung (Enforcement im Service, nicht mehr als UNIQUE-Index). Die Settings `BdeMehrfachBuchungProOperator` und `BdeMehrfachBuchungProArbeitsgang` lockern diese Constraints jeweils unabhaengig. Die Indexes `IX_BdeBookings_BdeOperatorId_Active` und `IX_BdeBookings_WorkOperationId_Active` sind seit Phase 2.2 nicht mehr UNIQUE (nur noch regulaere gefilterte Indexes).
- **BDE-Paused Semantik**: Paused hat `EndedAt = gesetzt`. Fortsetzung erzeugt neue Buchung mit `ParentBookingId`. Cockpit-Query `WHERE EndedAt IS NULL` zeigt nur Running.
- **BDE-Operator deaktiviert waehrend offener Buchung**: Offene Buchungen bleiben sichtbar. Schichtleiter muss manuell schliessen.
- **Save-Ordering in BdeBookingService**: Bei Auto-Close-und-New-Start MUSS `SaveChangesAsync` zwischen Schliessen und Add-Neu laufen (Helfer `FinishAndSaveAsync`), eingebettet in `BeginTransactionAsync()`.
- **enaio DMS-Sync kein Delta**: `angelegt`-Spalte in enaio ist statisch (Bulk-Import 2013). Full-Sync statt Delta — MERGE verhindert Duplikate. `EnaioDmsSyncService.cs` liest ALLE Werkstattauftraege/Zeichnungen ohne Datumsfilter.
- **BDE Auto-Pause EndedAt = exaktes Schichtende**: Der `BdeAutoPauseWorker` setzt `EndedAt` auf den exakten Schicht-Ende-Zeitpunkt (z.&nbsp;B. 14:00:00), NICHT auf `DateTime.Now`. Dadurch ist die Buchungsdauer unabhaengig vom tatsaechlichen Worker-Tick (max. `Sync:BdeAutoPauseIntervalMinutes` Latenz).

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
| `Sync:BdeAutoPauseIntervalMinutes` | `60` | BDE Auto-Pause-Worker Intervall (Minuten) |
| `Sync:FeiertagSyncEnabled` | `false` | Feiertags-Sync von date.nager.at aktiv |
| `Sync:FeiertagCountryCode` | `AT` | Laendercode fuer Feiertags-Sync (ISO 3166-1 alpha-2) |
| `Sync:FeiertagRegion` | (leer) | Optionaler Bundesland-Code (z.&nbsp;B. AT-3 NOe, AT-6 Stmk) |
| `Sync:FeiertagJahreVoraus` | `2` | Jahre in die Zukunft synchronisieren |
| `Sync:WarehouseRequisitionEmailEnabled` | `false` | Aktiviert E-Mail-Versand fuer Lagerbestellungen im SyncWorker |
| `Sync:LagerplaetzeEnabled` | `false` | Sage-Lagerplatz-Stammdaten-Sync aktiv |
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

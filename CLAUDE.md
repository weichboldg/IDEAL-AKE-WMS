# IdealAkeWms — Kontext für KI-Assistenten


## Workflow-Orchestrierung

### 1. Plan-Modus als Standard
- IMMER Plan-Modus vor Code-Änderungen verwenden (gilt für 3+ Schritte oder 
  Architekturentscheidungen)
- Bei Unklarheiten (z.B. SAGE vs. OSEON, Auth-Middleware-Reihenfolge): 
  STOPP — zuerst klären, dann umsetzen
- Detaillierte Spezifikation vor Implementierung: Welche Controller, 
  Repository, Migration, SQL-Script?
- Plan dokumentieren bevor Code entsteht

### 2. Verifikation vor Abschluss
- Niemals Task als erledigt markieren ohne Beweis: 
  läuft die App? Wurde migriert? Zeigt die View korrekte Daten?
- Checkliste nach jeder Änderung:
  - [ ] Migration erstellt + SQL/XX_*.sql mit OBJECT_ID-Guard?
  - [ ] SQL/00_FreshInstall.sql aktualisiert?
  - [ ] PROJECT_STATUS.md gepflegt?
  - [ ] Readme gepflegt?
  - [ ] User Hilfeseite gepflegt?
  - [ ] TempData korrekt (nur SuccessMessage / WarningMessage)?
  - [ ] Audit-Felder gesetzt (ModifiedAt, ModifiedBy, ModifiedByWindows)?

### 3. Selbstverbesserungsschleife
- Nach jeder Korrektur: Fallstrick in der MD-Datei ergänzen
- Bekannte Fallen nie zweimal machen:
  - Artikelnummer ≠ Ressourcenummer → immer Ressourcenummer für Bauteile
  - AppSettings-Tabelle hat kein AuditableEntity
  - Bootstrap überschreibt custom CSS → !important nötig
  - InMemory DB unterstützt kein rowversion → TestApplicationDbContext

### 4. Autonome Fehlerbehebung
- Bei Bugs: direkt in Logs/Serilog schauen, Ursache finden, beheben
- Kein Rückfragen bei bekannten Mustern (z.B. Select2 ViewBag-Re-Init 
  nach POST-Validierungsfehler)
- CI/Tests eigenständig grün bekommen

### 5. Eleganz (ausgewogen)
- Repository Pattern + Decorator konsequent nutzen 
  (z.B. CachedBomRepository wraps BomRepository)
- Kein Over-Engineering bei einfachen CRUD-Operationen
- Bei komplexer Logik (Picking, Transfer, Bestandsberechnung) 
  innehalten: "Gibt es einen saubereren Weg?"

---

## Aufgabenverwaltung

1. **Zuerst planen** — Was ändert sich? Model / Repo / Controller / View / SQL?
2. **Migration prüfen** — Neues Pflichtfeld? Agent-Job-Scripts anpassen!
3. **Fortschritt tracken** — PROJECT_STATUS.md laufend aktuell halten
4. **Änderungen erklären** — Sinnvolle Git Commit Messages
5. **Ergebnisse dokumentieren** — README.md, Hilfeseite etc. aktuell halten.
6. **Lektionen festhalten** — Neue Fallstricke sofort in diese Datei

---

## Kernprinzipien

- **Einfachheit zuerst** — Minimale Code-Auswirkung. 
  Nur anfassen was nötig ist.
- **Keine faulen Fixes** — Root Cause finden. 
  Kein temporäres Patch auf Middleware-Reihenfolge o.ä.
- **Minimale Auswirkung** — Keine Nebeneffekte. 
  Besonders bei Auth, Session, Middleware-Pipeline.


## Architektur

- ASP.NET Core 10.0 MVC + Repository Pattern + DI
- EF Core 10.0 mit SQL Server (`AKESQL20.ake.at`, DB: `IDEAL_AKE_WMS`)
- Dual-Auth: Windows/Negotiate (IIS) + Session-basierter App-Login (Middleware in Program.cs)
- BOM-Daten: primär aus SAGE-View `[ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]`; Fallback bei leerem Ergebnis auf OSEON-SP `sp_AKE_Kommissionierung_OseonStuecklistenDB` (Server: `aketrumpf01.ake.at\TRUMPFSQL2`, DB: `T1000_V01_V001`)
- `BomRepository.GetBomItemsAsync()` liefert `BomQueryResult(Items, DataSource)` — DataSource = `"SAGE"`, `"OSEON"` oder `"KEINE_DATEN"`
- `CachedBomRepository` wrapped `BomRepository` (Decorator-Pattern, 5 min MemoryCache)

## Versionierung

- **Web-App**: `IdealAkeWms/AppVersion.cs` — `Version` + `Date` Konstanten, angezeigt im Footer
- **Service**: `IDEALAKEWMSService/AppVersion.cs` — gleiche Struktur, geloggt beim SyncWorker-Start
- **Changelog**: `Views/Help/Changelog.cshtml` — Anwender-sichtbare Aenderungshistorie, erreichbar ueber Footer-Link "Aenderungen"
- **Bei jeder Aenderung**: Version hochzaehlen, Changelog ergaenzen, `AppVersion.cs` in beiden Projekten aktualisieren

## Responsive Design

- **Mobile-First**: `site.css` enthaelt responsive Media-Queries (`max-width: 575.98px`, `767.98px`, `991.98px`)
- **Touch Targets**: Buttons min-height 44px, Form Controls min-height 44px auf Mobile (WCAG)
- **iOS Zoom**: `font-size: 16px` auf Mobile-Inputs verhindert Safari Auto-Zoom
- **Scrollbare Tabellen**: `.table-responsive` mit `overflow-x: auto`, `width: max-content !important`, `white-space: nowrap`
- **Sticky Scrollbar**: `site.js` erstellt per JS eine synchronisierte Scrollbar (`.table-sticky-scrollbar`) die am Viewport-Rand klebt. Blendet sich per `IntersectionObserver` aus wenn die echte Scrollbar sichtbar ist
- **Scrollbar-Styling**: Webkit (`::-webkit-scrollbar`) + Firefox (`scrollbar-color`) im Corporate Design
- **Navbar Mobile**: `.navbar-user-mobile` Block im Hamburger-Menue mit Profil-Link + Abmelden-Button
- **Page Headers**: Alle `d-flex justify-content-between` haben `flex-wrap gap-2` fuer Mobile-Umbruch
- **Nested Tables**: `.nested-table-responsive` fuer verschachtelte Tabellen (Tracking/Index)

## Konventionen

- **Sprache**: Code/Variablen auf Englisch, UI-Texte auf Deutsch
- **Entity-Basis**: `AuditableEntity` (Id, CreatedAt, CreatedBy, CreatedByWindows, ModifiedAt?, ModifiedBy?, ModifiedByWindows?)
- **Corporate Design**: `--ake-primary: #053153`, `--ake-secondary: #43A6E2`, `--ake-orange: #E87A1E`
- **Dokumentation**: `PROJECT_STATUS.md` im Root pflegen, `README.md` bei Feature-Änderungen aktuell halten
- **Testing**: Bei Code-Änderungen mögliche Testfälle vorschlagen und umsetzen
- **Git**: Sinnvolle Commit Messages, Änderungen nachvollziehbar committen

## Session & Authentifizierung

- **Session-Timeout**: 8 Stunden (`IdleTimeout`), Cookie: `IdealAkeWms.Session`
- **Session-Keys**: `AppUserId` (Int32), `AppUserName` (String) — gesetzt bei Login in `AccountController`
- **Login-Redirect-Middleware**: In `Program.cs` nach `UseSession()` — prüft `Session["AppUserId"]`; redirectet auf `/Account/Login?returnUrl=...` wenn nicht gesetzt
- **Ausnahmen** (kein Redirect): `/account/*`, `/api/*`, statische Dateien (`.`-Extension), `/lib/*`, `/css/*`, `/js/*`
- **Middleware-Reihenfolge**: HttpsRedirection → Routing → Authentication → Authorization → **Session** → SerilogRequestLogging → **LoginRedirect** → StaticFiles → MapControllerRoute

## Zugriffsschutz

- **`[RequireMasterDataAccess]`** — TypeFilterAttribute in `Filters/`, nutzt `ICurrentUserService.HasMasterDataAccessAsync()`
  - Redirectet bei Ablehnung auf `Account/AccessDenied`
  - Angewendet auf: `UsersController`, `WorkstationsController`, `SettingsController`, `RolesController`
  - Rollen: `admin`, `masterdata`
- **`[RequireTrackingAccess]`** — TypeFilterAttribute in `Filters/`, nutzt `ICurrentUserService.CanViewTrackingAsync()`
  - Redirectet bei Ablehnung auf `Account/AccessDenied`
  - Angewendet auf: `TrackingController`
  - Rollen: `admin`, `tracking`
- **`[RequirePickingAccess]`** — TypeFilterAttribute in `Filters/`, nutzt `ICurrentUserService.CanPickAsync()`
  - Redirectet bei Ablehnung auf `Account/AccessDenied`
  - Angewendet auf: `ProductionOrdersApiController`, einzelne Actions in `ProductionOrdersController` (alle außer `Index`)
  - Menüpunkte Kommissionierung nur sichtbar wenn `CanPickAsync() == true`
  - Rollen: `admin`, `picking`
- **`[RequirePickingOrTrackingAccess]`** — TypeFilterAttribute in `Filters/`, nutzt `CanPickAsync() || CanViewTrackingAsync()`
  - Angewendet auf: `ProductionOrdersController.Index` — Tracking-User sehen FA-Liste (read-only, ohne Stückliste/Erledigt), können OSEON Teileverfolgung öffnen
  - Navbar: Fertigungsaufträge-Link erscheint auch für Tracking-User (wenn kein Picking-Zugriff)
- **`[RequireStockAccess]`** — TypeFilterAttribute in `Filters/`, nutzt `ICurrentUserService.CanAccessStockAsync()`
  - Redirectet bei Ablehnung auf `Account/AccessDenied`
  - Angewendet auf: `StockMovementsController` (Einbuchung, Ausbuchung, Umbuchung, Index), `StockOverviewController`
  - Rollen: `admin`, `stock`, `stock_keyuser`, `picking`
- **`[RequireStockKeyUserAccess]`** — TypeFilterAttribute in `Filters/`, nutzt `ICurrentUserService.CanTransferStockAsync()`
  - Redirectet bei Ablehnung auf `Account/AccessDenied`
  - Angewendet auf: `StockMovementsController` (Lagerplatz ausbuchen, Lagerplatz umbuchen)
  - Rollen: `admin`, `stock_keyuser`, `picking`
- **`[RequireReportingAccess]`** — TypeFilterAttribute in `Filters/`, nutzt `ICurrentUserService.CanReportOperationsAsync()`
  - Redirectet bei Ablehnung auf `Account/AccessDenied`
  - Rollen: `admin`, `reporting` (fuer spätere BDE-Controller)
- **`[RequirePickingOrStockAccess]`** — TypeFilterAttribute in `Filters/`, nutzt `CanPickAsync() || CanAccessStockAsync()`
  - Redirectet bei Ablehnung auf `Account/AccessDenied`
  - Angewendet auf: `PartRequisitionsController`, `OrderRecipientGroupsController`
  - Rollen: `admin`, `picking`, `stock`, `stock_keyuser`
- **`[RequireLeitstandAccess]`** — TypeFilterAttribute in `Filters/`, nutzt `ICurrentUserService.CanManagePickingReleaseAsync()`
  - Redirectet bei Ablehnung auf `Account/AccessDenied`
  - Angewendet auf: `ProductionOrdersController` (ToggleRelease, BulkRelease, SetPriority)
  - Rollen: `admin`, `leitstand`

## Rollenkonzept

- **Architektur**: `Role`-Tabelle + `UserRole`-Junction (Many-to-Many), statische Keys in `RoleKeys.cs`
- **Admin-Wildcard**: Admin-Rolle ueberspringt alle Berechtigungspruefungen
- **AD-Gruppen**: Jede Rolle kann optional eine AD-Gruppe (`Role.AdGroup`, SAMAccountName) haben -- Mitglieder erhalten die Rolle automatisch
- **AD-Cache**: `Security:AdGroupCacheMinutes` in `appsettings.json` (default 5 Min)
- **Zwei-Phasen-Migration**: Phase 1 = neue Tabellen + Datenmigration (alte Spalten bleiben), Phase 2 = alte Boolean-Spalten entfernen (nach Verifikation)

| Key | Name | Beschreibung |
|-----|------|-------------|
| `admin` | Administrator | Vollzugriff |
| `masterdata` | Stammdaten | Benutzer, Arbeitsplaetze, Einstellungen |
| `picking` | Kommissionierer | Picking + vollstaendiger Lagerzugriff |
| `stock` | Lager | Einbuchung, Ausbuchung, Bestaende |
| `stock_keyuser` | Lager Keyuser | Lager + Lagerplatz ausbuchen/umbuchen |
| `tracking` | Teileverfolgung | OSEON Auftraege + Rueckmeldungen |
| `reporting` | Betriebsdaten (BDE) | Arbeitsgaenge stempeln (Zukunft) |
| `leitstand` | Leitstand | Produktionsauftraege freigeben und priorisieren |

## ICurrentUserService

```csharp
string GetWindowsUserName();          // HttpContext.User.Identity.Name
string GetDisplayName();              // App-Name (Session) oder Windows-Name (domain\user → user)
int? GetCurrentAppUserId();           // Session["AppUserId"]
string? GetCurrentAppUserName();      // Session["AppUserName"]
bool IsLoggedIn();                    // AppUserId != null
Task<bool> HasRoleAsync(string roleKey);           // Prueft Rolle (DB + AD-Gruppe)
Task<bool> HasAnyRoleAsync(params string[] roleKeys); // Prueft mehrere Rollen (OR)
Task<bool> HasMasterDataAccessAsync(); // admin, masterdata (+ AD-Gruppe der Rolle)
Task<bool> IsAdminAsync();            // admin-Rolle
Task<bool> CanViewTrackingAsync();    // admin, tracking
Task<bool> CanReportOperationsAsync(); // admin, reporting
Task<bool> CanPickAsync();            // admin, picking
Task<bool> CanAccessStockAsync();     // admin, stock, stock_keyuser, picking
Task<bool> CanTransferStockAsync();   // admin, stock_keyuser, picking
Task<bool> CanManagePickingReleaseAsync(); // admin, leitstand
```

Alle Berechtigungsmethoden delegieren intern an `HasAnyRoleAsync()` mit den entsprechenden Rollen-Keys.

## TempData-Meldungen

Nur zwei Keys — kein `ErrorMessage` (Fehler via `ModelState`):

| Key | Bootstrap-Klasse | Verwendung |
|-----|-----------------|------------|
| `TempData["SuccessMessage"]` | `alert-success` | Nach erfolgreichem Speichern |
| `TempData["WarningMessage"]` | `alert-warning` | Bestandswarnung, Konflikte |

Anzeige in `_Layout.cshtml` als dismissable Bootstrap-Alerts.

## Controller-Muster

**MVC-Controller** (erben `Controller`):
- Validierung: `if (!ModelState.IsValid) return View(vm);`
- Erfolg: `TempData["SuccessMessage"] = "..."; return RedirectToAction(nameof(Index));`
- Audit-Felder beim Update: `ModifiedAt = DateTime.UtcNow`, `ModifiedBy`, `ModifiedByWindows` aus `ICurrentUserService`

**API-Controller** (erben `ControllerBase`, `[ApiController]`, `[Route("api/...")]`):
- Rückgabe: `Ok(new { ... })`, `NotFound()`, `BadRequest("message")`
- Exception-Handling: `catch (InvalidOperationException ex) → BadRequest(ex.Message)`
- Keine Views — nur JSON-Responses

## Select2-Integration

- CDN: jsDelivr, Select2 4.1.0-rc.0 + Bootstrap5-Theme, deutsche Lokalisierung (`de.js`)
- Partials: `Views/Shared/_Select2ArticlePartial.cshtml`, `_Select2ProductionOrderPartial.cshtml`
- AJAX-Endpoints: `/api/articles/search?q={term}&limit=50`, `/api/productionorders/search?q={term}&limit=20`
- Aktivierung via CSS-Klasse `.select2-article` / `.select2-productionorder`
- Bei POST-Validierungsfehler: ViewBag-Daten für Re-Initialisierung erforderlich

## Bekannte Fallstricke

- **Artikelnummer vs Ressourcenummer**: `Artikelnummer` = Geräte-Artikelnummer (WA), `Ressourcenummer` = Bauteil-Artikelnummer. Immer `Ressourcenummer` für Bauteil-Operationen verwenden! (`PickingItem.BomArticleNumber` = Ressourcenummer)
- **Bootstrap table styles** überschreiben custom CSS → `!important` nötig
- **Alphabetische Sortierung von Positionsnummern**: `NaturalPositionComparer` verwenden (1, 2, 10 statt 1, 10, 2)
- **InMemory DB** unterstützt kein `rowversion` → Tests nutzen `TestApplicationDbContext` mit Override für RowVersion-Handling
- **SQL Server Batch-Parsing**: Tabellen in separatem Batch erstellen (GO dazwischen), bei Bedarf `OBJECT_ID` Guard verwenden
- **0-Bestände**: `GetCurrentStockAsync()` filtert standardmäßig Bestände mit 0 aus (nur ohne expliziten Min/Max-Filter)
- **Logo/Favicon**: SVG enthält eingebettetes PNG mit weißem Hintergrund → auf dunklem Navbar-Hintergrund `.navbar-logo-wrapper` (CSS: `background:white; border-radius:4px; padding:3px 8px`) verwenden. Favicon: `wwwroot/favicon.ico` + SVG-Link im Layout-Head
- **Startup-Seeding**: `Program.cs` legt fehlende Standard-Daten an (admin-User, NAN-Lagerplatz) — PBKDF2-Hash via `IPasswordService.HashPassword("")` nicht vorweg berechenbar (zufälliger Salt)
- **AppSettings-Tabelle**: KEIN AuditableEntity — nur `Key` (PK), `Value`, `Description`. Kein `CreatedAt`!
- **PickingItem.RowVersion**: `[Timestamp]` für Optimistic Concurrency — EF InMemory unterstützt das nicht → `TestApplicationDbContext`
- **Drucker-Pfad-Format**: UNC-Pfad `\\DRUCKSERVER\Druckername` (Workstation.DefaultPrinter)
- **Boolean-Checkbox in Forms**: Hidden+Checkbox mit gleichem `name` funktioniert NICHT zuverlaessig in ASP.NET Core. Beim Unchecken wird der Wert nicht korrekt uebermittelt. **Loesung**: Nur das Hidden-Input bekommt `name`, die Checkbox hat KEINEN `name` sondern einen `onchange`-Handler der den Hidden-Wert synchronisiert: `onchange="document.getElementById('hidden-id').value = this.checked ? 'true' : 'false'"`. Gilt fuer ALLE Boolean-Checkboxen in Forms — auch bei einfachen bool-Properties, nicht nur bei Dictionaries. Beispiel: `OperationConfig.cshtml` (IsOseonRelevant)
- **QR-Code Komma-Suffix**: FA-Nummer im QR kann Komma-Suffix haben (z.B. `2610063,09`) → immer `.split(',')[0]` verwenden
- **Scanner-Endlosschleife**: `confirm()` nach fehlgeschlagenem Scan → Scanner öffnet sofort → Kamera liest denselben QR → Endlosschleife. Lösung: Bootstrap-Modal statt `confirm()` verwenden
- **EF PendingModelChangesWarning**: Bei neuen Indizes/Model-Änderungen im `ApplicationDbContext.OnModelCreating` immer `dotnet ef migrations add` ausführen, sonst crasht `db.Database.Migrate()` mit `PendingModelChangesWarning`
- **OSEON pa.ID ist bigint**: `ProduktionsAuftrag.ID` in OSEON ist `Int64` (bigint) — `OseonRawRow.OseonId` muss `long` sein, nicht `int`. `reader.GetInt64(0)` verwenden
- **Rollen-Migration Phase 2**: `SQL/33_RemoveOldPermissionColumns.sql` erst nach Verifikation des Rollensystems ausfuehren. Vorher bleiben alte Boolean-Spalten als Fallback
- **Kommissionierwagen (IsPickingTransport)**: Wagen-Lagerplaetze werden an mehreren Stellen gefiltert: (1) Stueckliste: nicht in Lagerplatz-Bestand und nicht in Quell-/Ziel-Dropdown (`GetStockByArticleNumbersAsync`, `GetAllOrderedExcludingPickingTransportAsync`), (2) Bestandsuebersicht: keine Meldebestand-Farbcodierung, (3) StockCheckService: keine Benachrichtigungen. NICHT filtern in: Bestandsuebersicht-Liste (dort sollen Wagen sichtbar bleiben) und Bewegungshistorie
- **Bestandsuebersicht FA-Filter**: `GetStockByProductionOrderAsync()` ist eine eigene Methode — NICHT in `GetCurrentStockAsync` eingebaut. FA-Filter zeigt den Netto-Bestand der Buchungen mit dieser FA, nicht den Gesamtbestand der Artikel
- **enaio object1.id ist int**: Im Gegensatz zu OSEON (bigint) ist enaio `object1.id` ein `int` — daher `Convert.ToInt64(reader.GetValue(0))` verwenden statt `reader.GetInt64(0)`
- **Artikelgruppe BOM vs Articles-Tabelle**: Die SAGE-View liefert Artikelgruppen als `"940 - Kleinmaterial Allgemein"` (Code + Beschreibung), aber die `Articles`-Tabelle speichert nur `"940"` (reiner Code). Beim Matching (z.B. Empfaenger-Routing, Mappings) immer `.split(' - ')[0].trim()` verwenden um den Code-Teil zu extrahieren
- **Picking-Checkbox ist client-seitig**: Checkboxen in der Stueckliste (`Bom.cshtml`) speichern NICHT mehr sofort in die DB. `IsPicked` wird erst beim "Umbuchen" gesetzt. Die `TogglePicked`-Action existiert noch, wird aber nicht mehr aufgerufen
- **Razor v@ wird als E-Mail interpretiert**: `v@Namespace.Class.Property` wird von Razor als E-Mail-Adresse geparst. Immer `v@(Namespace.Class.Property)` mit Klammern verwenden
- **Select2-Text-Format fuer Artikel**: Die Select2-API (`/api/articles/search`) liefert `"ArticleNumber - Description"` (mit Hyphen ` - `). Bei Text-Parsing immer `.split(' - ')[0]` verwenden, NICHT Em-Dash `' — '`
- **Bedarfsmeldung Empfaenger**: Im Bestell-Modal werden Empfaenger per E-Mail-Adresse (nicht per ID) an den Server gesendet. Checkbox-Value = E-Mail, Request-Property = `SelectedEmails`. `OrderRecipientGroupId` wird server-seitig aus dem Artikelgruppen-Mapping ermittelt
- **Leitstand Toggle-Verhalten**: Wenn `LeitstandAktiv=false`, zeigt Picking() die alte Dropdown-View (`PickingDropdown.cshtml`). Wenn `true`, die neue Tabelle. Menuetext ist immer "Fertigungsauftraege"
- **Leitstand Index-Action hat kein Filter-Attribut**: Die Index-Action von ProductionOrdersController prueft Berechtigungen manuell im Methoden-Body (CanPick OR CanViewTracking OR CanManagePickingRelease), weil kein bestehender Filter alle drei Rollen abdeckt
- **Leitstand Freigabe ohne Artikelnummer**: `ToggleRelease` prueft ob `ArticleNumber` vorhanden ist. Ohne Artikelnummer → TempData WarningMessage, keine Freigabe. Die `BulkRelease`-Action ueberspringt solche Auftraege und meldet sie als "uebersprungen"
- **Leitstand PickingPriority NULL = niedrigste**: Auftraege ohne Prioritaet werden ans Ende sortiert (`OrderBy PickingPriority.HasValue ? 0 : 1, ThenBy PickingPriority`)
- **Controller-Split**: Kommissionierung ist jetzt in `PickingController`, nicht mehr in `ProductionOrdersController`. `ProductionOrdersController` hat Redirect-Stubs fuer `/ProductionOrders/Bom` und `/ProductionOrders/Picking` (301-Redirect auf neue URLs)
- **Photo-API**: Fotos werden jetzt ueber `/api/photos/upload`, `/api/photos/{id}`, `/api/photos/delete` angesprochen (war: `/ProductionOrders/UploadPhoto` etc.)

## Standard-Daten (Neuinstallation)

| Typ | Wert | Beschreibung |
|-----|------|-------------|
| Benutzer | `admin` / Passwort leer | Standard-Admin (Rolle `admin` zugewiesen), Seeding in `Program.cs` |
| Lagerplatz | `NAN` | Fallback für negative Buchungen, Seeding in `Program.cs` |

Seeding: `Program.cs` nach `db.Database.Migrate()` — idempotent.
SQL-Fallback: `SQL/00_FreshInstall.sql` Sektion "Standard-Daten" (admin mit `PasswordHash = NULL`, wird beim App-Start befüllt).

## SQL Agent Jobs

`SQL/AgentJobs/` — wiederkehrende Import-Scripts für SQL Server Agent:

| Script | Quelle → Ziel |
|--------|--------------|
| `01_Import_Produktionsauftraege.sql` | `vw_AKE_Kommissionierung_WAListe` → `ProductionOrders` |
| `02_Import_Artikel.sql` | `KHKPpsRessourcenPositionen` + `KHKArtikel` → `Articles` |

Bei DB-Strukturänderungen (neue Pflichtfelder) müssen diese Scripts angepasst werden.

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
| ~~`StammdatenADGruppe`~~ | — | Ersetzt durch `Role.AdGroup` auf der Rolle 'masterdata' |
| `BeschichtungAbholtage` | `Dienstag,Donnerstag` | Wochentage für Beschichtungs-Abholung |
| `TeileverfolgungAktiv` | `false` | Globaler Schalter: Teileverfolgungs-Modul aktiviert |
| `OseonRueckmeldungAktiv` | `false` | Rückmeldungen dürfen an Oseon zurückgeschrieben werden |
| `SageRueckmeldungAktiv` | `false` | Rückmeldungen dürfen an Sage zurückgeschrieben werden |
| `QrMitFaNummer` | `false` | QR-Code enthält Fertigungsauftragsnummer an 3. Stelle |
| `OseonAmpelGelbTage` | `1` | OSEON Ampel: Gelb ab X Tagen vor Termin |
| `OseonAmpelBlauTage` | `2` | OSEON Ampel: Blau ab X Tagen vor Termin |
| `BestellungenAktiv` | `false` | Bedarfsmeldungen aus Stueckliste aktivieren |
| `LeitstandAktiv` | `false` | Leitstand: Kommissionier-Freigabe und Priorisierung |

## Service-Konfiguration (appsettings.json)

Sync-Toggles und Worker-Settings stehen in `IDEALAKEWMSService/appsettings.json` (nicht in der DB-Tabelle AppSettings):

| Key | Default | Beschreibung |
|-----|---------|-------------|
| `Sync:ProductionOrdersEnabled` | `true` | Sage-WA-Import aktiv |
| `Sync:ArticlesEnabled` | `true` | Sage-Artikel-Import aktiv |
| `Sync:OseonTrackingEnabled` | `false` | OSEON-Tracking-Sync aktiv |
| `Sync:EnaioDmsEnabled` | `false` | enaio DMS-Sync aktiv |
| `WorkerSettings:SyncIntervalMinutes` | `15` | Sync-Intervall in Minuten |
| `WorkerSettings:SyncDryRun` | `false` | DryRun-Modus (keine DB-Aenderungen) |
| `Security:AdGroupCacheMinutes` | `5` | AD-Gruppen-Cache Dauer |
| `Sync:PartRequisitionEmailEnabled` | `false` | Bedarfsmeldungs-E-Mail-Versand aktiv |

Connection Strings: `DefaultConnection` (WMS), `SageConnection` (Sage), `OseonConnection` (OSEON), `EnaioDmsConnection` (enaio)

## OSEON Teileverfolgung

- **Entities**: `OseonProductionOrder` + `OseonWorkOperation` (AuditableEntity)
- **Datenquelle**: OSEON DB (`aketrumpf01.ake.at\TRUMPFSQL2`, `T1000_V01_V001`)
- **Sync**: `OseonSyncService` im IDEALAKEWMSService, gesteuert per `Sync:OseonTrackingEnabled` in appsettings.json — Bulk-Verarbeitung via SqlBulkCopy + MERGE (Temp-Tables `#TmpOseonOrders`, `#TmpOseonOps`)
- **Baumstruktur**: 3 Ebenen — KundenAuftragsNr → OseonOrderNumber (Subaufträge) → Arbeitsgänge
- **Ampelsystem**: Rot (ueberfaellig), Gelb (faellig ≤ GelbTage), Blau (faellig ≤ BlauTage), Gruen (Status 90/95), Grau (kein Termin/noch nicht relevant)
- **AG-Konfiguration**: `OseonOperationConfig`-Tabelle — pro Arbeitsgang (Kurzname, z.B. "B", "ST", "BG") werden Soll-Termin-Offset (Arbeitstage relativ zum Stanztermin/OSEON-Endtermin) und OSEON-Relevanz konfiguriert
- **AG-Soll-Termine**: Jeder AG bekommt einen berechneten Soll-Termin = `Stanztermin + OffsetDays` (Arbeitstage, unter Beruecksichtigung von Wochenenden + Feiertagen via `BusinessDayService.AddBusinessDays`)
- **AG-Relevanz**: Wenn `IsOseonRelevant=false`, wird der AG bei der Statusberechnung ignoriert. Ein Auftrag gilt als "fertig" wenn alle OSEON-relevanten AGs Status 90/95 haben
- **AG-Ampel**: Jeder AG hat eigene Ampelfarbe basierend auf seinem berechneten Soll-Termin. Auftrags-Ampel = Worst-Color nur aus relevanten AGs
- **Einstellungen > AG-Konfiguration**: `Settings/OperationConfig` — CRUD-Seite fuer AG-Configs, zeigt auch nicht-konfigurierte AG-Namen aus OSEON-Daten
- **OSEON Status-Codes**: 10=Unvollständig, 20=Gültig, 30=Freigegeben, 60=In Arbeit, 70=Gesperrt, 90=Fertig, 95=Storniert
- **Werkbank Auto-Anlage**: Sync erstellt fehlende `ProductionWorkplaces` automatisch aus OSEON-Feld `Kunde.KundenNr`
- **Werkbank-Sync**: `SyncWorkplacesToProductionOrdersAsync()` überträgt `ProductionWorkplaceId` von OSEON auf Sage-Aufträge (Match: `OrderNumber` ↔ `CustomerOrderNumber`), nur wo noch keine Werkbank gesetzt ist
- **View**: `Tracking/OseonIndex` — 3-Ebenen Baumstruktur (Ordner-Icons, Chevrons, Einrückung) mit Ampel-Punkten
- **Pagination**: Server-seitig, 25 Gruppen pro Seite. Repository-Methode `GetPagedAsync()` paginiert nach CustomerOrderNumber-Gruppen
- **Filter**: Suche durchsucht sowohl `CustomerOrderNumber` als auch `OseonOrderNumber`
- **Gruppen-Logik**: Bei `showFinished=false` werden nur Gruppen mit mind. einem offenen Auftrag angezeigt, aber ALLE Sub-Aufträge der Gruppe (inkl. fertige) geladen
- **FA-Link**: ProductionOrders/Index hat OSEON-Teileverfolgung-Button der FA-Nummer als Filter übergibt (`showFinished=true` automatisch gesetzt, damit auch abgeschlossene Aufträge sichtbar)
- **Navbar**: Teileverfolgung → Dropdown mit "Rückmeldungen" und "OSEON Aufträge"
- **SQL**: `SQL/29_AddOseonTracking.sql`, `SQL/30_OseonPerformanceIndexes.sql`, `SQL/31_AddOseonTimestamps.sql`
- **Delta-Sync**: `OseonProductionOrder.LastChangedInOseon` (pa.DateOfLastChange) + `OseonWorkOperation.LastStatusReportInOseon` (aga.LetzteStatusMeldung) — beim Sync wird `MAX(LastChangedInOseon)` gelesen und als Filter auf die OSEON-Query angewendet (mit 5 Min Puffer). Erster Lauf = Full-Sync, danach nur Delta.
- **Sync-Filter**: OSEON-Query schließt alte fertige Aufträge aus (Status 90/95 UND EndTerminSoll < 3 Monate)
- **OseonId**: `bigint` (Int64) in OSEON, als `long` im Model
- **Ampel-Cache**: `OseonTrafficLightService` ist Scoped — cached `OseonAmpelGelbTage`/`OseonAmpelBlauTage` pro Request (statt 2 DB-Queries pro Auftrag)
- **IIS Timeout**: `web.config` → `requestTimeout="00:05:00"` für langlaufende Requests

## enaio DMS-Integration

- **Entity**: `EnaioDmsDocument` (AuditableEntity) — `EnaioDmsObjectId` (long, unique), `DocumentType` ("Werkstattauftrag"/"Zeichnung"), `OrderNumber`, `CreatedInEnaio`, `LastSyncedAt`
- **Datenquelle**: enaio DB (`AKESQL20`, DB: `enaio`) — `sysadm.object1` (feld1=Typ, feld44=WA-Nr, feld43=Zeichnungs-Nr)
- **Sync**: `EnaioDmsSyncService` im IDEALAKEWMSService, gesteuert per `Sync:EnaioDmsEnabled` in appsettings.json — Delta-Sync via `MAX(LastSyncedAt) - 5 Min`
- **Link-Format**: `http://akeosapp.ake.at/oscontentviewer/viewer/{EnaioDmsObjectId}/?pagecount=1`
- **View**: Fertigungsauftraege-Index zeigt enaio-Icons neben FA-Nummer (orange Dokument/Zeichnungs-Icon)
- **Bulk-Lookup**: `GetByOrderNumbersAsync()` laedt alle DMS-Links fuer die angezeigten FA-Nummern in einem Query
- **SQL**: `SQL/35_AddEnaioDmsDocuments.sql`

## Bedarfsmeldungen

- **Tabellen**: `PartRequisitions` (Bedarfsmeldung), `OrderRecipientGroups` (Empfaengergruppen), `OrderRecipients` (Empfaenger), `ArticleGroupRecipientMappings` (Artikelgruppe-Empfaengergruppe N:M)
- **Status-Workflow**: `Offen` → `Erfuellt` (bei Wareneingang verknuepft) oder `Storniert` (manuell)
- **Prioritaeten**: `Normal`, `Dringend`, `Eilt` — dargestellt als farbige Badges
- **E-Mail-Versand**: `PartRequisitionEmailService` im IDEALAKEWMSService — versendet offene Meldungen (wo `EmailSentAt IS NULL`) per SMTP
- **Empfaenger-Routing**: `ArticleGroupRecipientMappings` ordnet Artikelgruppen zu Empfaengergruppen zu — Meldungen werden automatisch an die passende Gruppe geroutet
- **Wareneingang-Integration**: Bei Einbuchung werden offene Meldungen zum Artikel angezeigt, Verknuepfung ueber `FulfilledByStockMovementId`
- **AppSetting**: `BestellungenAktiv` (`false`) — Globaler Schalter fuer das Feature
- **SQL**: `SQL/36_AddPartRequisitions.sql`

## KW-Filter (Fertigungsauftraege)

- **KW-Anzeige**: Alle 5 Datumsspalten zeigen ISO 8601 KW an (z.B. "07.01.2026 KW2") via `ISOWeek.GetWeekOfYear()`
- **Kalender-Popup**: Datumsspalten (Attribut `data-date-filter`) haben ein Kalender-Icon im Spaltenfilter. Klick oeffnet Monatskalender mit KW-Spalte. Klick auf KW filtert nach "KWxx", Klick auf Tag filtert nach "dd.MM.yyyy"
- **Client-seitig**: Implementiert in `table-filter.js`, Styles in `site.css` (`.date-filter-popup`)

## QR-Code-Format (Artikel)

- **Format**: `Artikelnummer;Feld2;FA-Nummer[,Suffix]` — Semikolon-getrennt, 3 Teile
- **Beispiel**: `87050064;1519503-06;2610063,09` → Artikel: `87050064`, FA: `2610063`
- **Komma-Suffix**: `parts[2]` kann Komma-Suffix haben (z.B. `,09` für anderes System) → wird per `.split(',')[0]` abgeschnitten
- **Setting `QrMitFaNummer`**: Wenn `true`, wird FA-Nummer in das `ProductionOrder`-Feld eingetragen
- **FA-Feld-Leerung**: Bei jedem QR-Scan wird das FA-Feld zuerst geleert, dann nur befüllt wenn FA vorhanden — verhindert alte FA-Nummer bei neuem Scan
- **Steuerung per View**: `data-qr-fa-enabled` und `data-fa-target` Attribute am Scan-Button
- **BOM-Scan (Kommissionierliste)**: Wenn gescannter Artikel nicht in Stückliste → Bootstrap-Modal (kein `confirm()`) mit Option "Nächsten Artikel scannen" oder "Abbrechen"

## Migrations-Workflow

1. Model ändern
2. `dotnet ef migrations add <MigrationName>` → generiert Migration in `Migrations/`
3. SQL-Script erstellen: `SQL/XX_<Name>.sql` mit `OBJECT_ID`-Guard (idempotent)
4. Migrations-History in separatem Batch markieren: `INSERT INTO __EFMigrationsHistory`
5. App startet mit `db.Database.Migrate()` → überspringt bereits angewendete Migrationen
6. `SQL/00_FreshInstall.sql` konsolidiert aktualisieren

## Wichtige Dateien

- `Program.cs` — DI, Middleware-Pipeline, **Startup-Seeding** (admin + NAN)
- `Controllers/AccountController.cs` — Login, Logout, **Profil Self-Service** (Passwort + BOM-Filter)
- `Controllers/ProductionOrdersController.cs` — FA-Liste, Leitstand-Freigabe (+ Redirect-Stubs fuer alte URLs)
- `Controllers/PickingController.cs` — Kommissionierung (Index, Bom, Transfer, Status, Print)
- `Controllers/Api/PhotoController.cs` — Foto-Upload/Download/Delete API
- `Controllers/StockMovementsController.cs` — Ein/Aus/Umbuchung + OutboundAll
- `Controllers/Api/ArticlesApiController.cs` — Select2-Suche für Artikel
- `Filters/RequireMasterDataAccessAttribute.cs` — Zugriffskontrolle für Stammdaten
- `Filters/RequirePickingOrTrackingAccessAttribute.cs` — Kombinierte Zugriffskontrolle (Picking ODER Tracking)
- `Services/PickingTransferService.cs` — Umbuchen gepickter Artikel (Transaktion)
- `Services/PasswordService.cs` — PBKDF2-HMAC-SHA256, 100k Iterationen
- `Services/PrintService.cs` — Drucken via `rundll32.exe mshtml.dll,PrintHTML`
- `Data/Repositories/StockMovementRepository.cs` — Bestandsberechnung
- `Data/Repositories/BomRepository.cs` — SAGE-View → Fallback OSEON-SP; liefert `BomQueryResult`
- `Views/Shared/_Layout.cshtml` — Navbar + TempData-Alerts + User-Dropdown
- `Views/Shared/_Select2ArticlePartial.cshtml` — Select2-Integration (Artikel)
- `Views/Picking/Bom.cshtml` — Stücklisten-View (Baum, Picking, Transfer-AJAX, Datenquelle-Badge)
- `Views/Account/Profile.cshtml` — Profil-Seite (Passwort + BOM-Filter + RecursiveFilterSearch)
- `wwwroot/css/site.css` — Corporate Design, Navbar-Styles, `.navbar-logo-wrapper`
- `wwwroot/images/ideal-ake-logo.svg` — Logo (eingebettetes PNG, weißer Hintergrund)
- `SQL/00_FreshInstall.sql` — Konsolidiertes Neuinstallations-Script
- `SQL/AgentJobs/` — SQL Server Agent Job Scripts (Sage-Import)
- `Controllers/TrackingController.cs` — Teileverfolgung (Rückmeldungen + OSEON Aufträge mit Pagination)
- `Services/OseonTrafficLightService.cs` — Ampelberechnung für OSEON-Aufträge
- `Helpers/OseonStatusHelper.cs` — OSEON Status-Code → Text/Badge-Mapping
- `Data/Repositories/OseonProductionOrderRepository.cs` — Repository mit `GetPagedAsync()` für server-seitige Paginierung
- `Models/ViewModels/OseonTrackingViewModel.cs` — ViewModels für 3-Ebenen Tree-View + Pagination
- `Views/Tracking/OseonIndex.cshtml` — OSEON Tree-View (Ordner/Dokument/Uhr-Icons, Chevrons, Alle auf-/zuklappen)
- `Views/ProductionOrders/Index.cshtml` — FA-Liste (Stückliste-Button vorne, OSEON+Erledigt-Buttons hinten)
- `IDEALAKEWMSService/Services/OseonSyncService.cs` — OSEON-Daten-Sync (filtert alte fertige Aufträge aus)
- `Models/Role.cs` — Rollen-Entity (Key, Name, Description, AdGroup)
- `Models/UserRole.cs` — User-Rolle Junction (Many-to-Many)
- `Models/RoleKeys.cs` — Statische Rollen-Schluessel (admin, masterdata, picking, stock, etc.)
- `Controllers/RolesController.cs` — CRUD fuer Rollen (Name, Beschreibung, AD-Gruppe)
- `Data/Repositories/RoleRepository.cs` — Repository fuer Rollen + UserRoles
- `Filters/RequireStockAccessAttribute.cs` — Zugriffskontrolle fuer Lagerbewegungen
- `Filters/RequireStockKeyUserAccessAttribute.cs` — Zugriffskontrolle fuer Lagerplatz-Operationen
- `Filters/RequireReportingAccessAttribute.cs` — Zugriffskontrolle fuer BDE (Zukunft)
- `Models/OseonOperationConfig.cs` — AG-Konfiguration (OperationName, DisplayName, DueDateOffsetDays, IsOseonRelevant)
- `Data/Repositories/OseonOperationConfigRepository.cs` — Repository fuer AG-Configs
- `Views/Settings/OperationConfig.cshtml` — CRUD-Seite fuer AG-Konfiguration
- `SQL/34_AddOseonOperationConfig.sql` — Migration fuer OseonOperationConfigs-Tabelle
- `Models/EnaioDmsDocument.cs` — enaio DMS-Dokument-Entity (ObjectId, DocumentType, OrderNumber)
- `Data/Repositories/EnaioDmsDocumentRepository.cs` — Repository + Bulk-Lookup fuer DMS-Links
- `IDEALAKEWMSService/Services/EnaioDmsSyncService.cs` — enaio DMS-Sync (Delta, BulkCopy + MERGE)
- `SQL/35_AddEnaioDmsDocuments.sql` — Migration fuer EnaioDmsDocuments-Tabelle
- `AppVersion.cs` (Web + Service) — Zentrale Versionskonstanten (Version, Date)
- `Views/Help/Index.cshtml` — Anwender-Hilfeseite (alle Features dokumentiert)
- `Views/Help/Changelog.cshtml` — Anwender-sichtbare Aenderungshistorie
- `Controllers/HelpController.cs` — Help + Changelog Actions
- `wwwroot/js/site.js` — Sticky Scrollbar fuer responsive Tabellen
- `wwwroot/js/table-filter.js` — Client-seitiger Spaltenfilter + KW-Kalender-Popup
- `wwwroot/js/barcode-scanner.js` — QR/Barcode-Scanner (scanTypes: article, storageLocation, productionOrder)
- `Services/BusinessDayService.cs` — Arbeitstage-Berechnung (Wochenenden + Feiertage)
- `Controllers/StockOverviewController.cs` — Bestandsuebersicht mit FA-Filter
- `Controllers/PartRequisitionsController.cs` — Bedarfsmeldungen (Erstellen, Uebersicht, Stornieren)
- `Controllers/OrderRecipientGroupsController.cs` — CRUD fuer Empfaengergruppen + Empfaenger + Artikelgruppen-Zuordnung
- `Controllers/Api/PartRequisitionsApiController.cs` — API fuer Bedarfsmeldungen (Erstellen aus Stueckliste, Sammelbestellung)
- `Models/PartRequisition.cs` — Bedarfsmeldung-Entity (Status, Prioritaet, E-Mail-Tracking)
- `Models/OrderRecipientGroup.cs` — Empfaengergruppe mit Navigation zu Empfaengern + Artikelgruppen-Mappings
- `Models/OrderRecipient.cs` — Einzelner Empfaenger (Name, E-Mail, IsActive)
- `Models/ArticleGroupRecipientMapping.cs` — Artikelgruppe-Empfaengergruppe Zuordnung (N:M)
- `Data/Repositories/PartRequisitionRepository.cs` — Repository fuer Bedarfsmeldungen (inkl. offene Meldungen pro Artikel)
- `Data/Repositories/OrderRecipientGroupRepository.cs` — Repository fuer Empfaengergruppen + Routing-Logik
- `IDEALAKEWMSService/Services/PartRequisitionEmailService.cs` — E-Mail-Versand fuer Bedarfsmeldungen
- `Views/PartRequisitions/Index.cshtml` — Bestelluebersicht mit Status-Badges und Pagination
- `Views/OrderRecipientGroups/` — CRUD-Views fuer Empfaengergruppen-Verwaltung
- `Filters/RequirePickingOrStockAccessAttribute.cs` — Zugriffskontrolle Picking ODER Lager
- `SQL/36_AddPartRequisitions.sql` — Migration fuer Bedarfsmeldungs-Tabellen
- `Filters/RequireLeitstandAccessAttribute.cs` — Zugriffskontrolle fuer Leitstand-Funktionen
- `Models/ViewModels/PickingListViewModel.cs` — ViewModel fuer Kommissionierliste (Tabelle)
- `Views/Picking/IndexDropdown.cshtml` — Fallback-View (alte Dropdown-Auswahl bei LeitstandAktiv=false)
- `SQL/37_AddPickingRelease.sql` — Migration fuer Leitstand-Felder + Rolle + AppSetting

## Bestandsuebersicht

- **Meldebestand-Farbcodierung**: `stock-warning` (orange) wenn Bestand ≤ `WarningThresholdPercent`% des Meldebestands, `stock-critical` (rot) wenn ≤ `CriticalThresholdPercent`%. Kommissionierwagen (`IsPickingTransport`) werden NICHT farbcodiert
- **FA-Filter**: Eigene Methode `GetStockByProductionOrderAsync()` — zeigt Netto-Bestand der Buchungen mit dieser FA-Nummer pro Artikel+Lagerplatz. NICHT in `GetCurrentStockAsync` integriert (separate Abfrage)
- **QR-Scan**: Artikel-Filter hat QR-Scan-Button (`scanType: article`), FA-Filter hat QR-Scan-Button (`scanType: productionOrder` — extrahiert 3. Position aus QR)
- **Kommissionierwagen**: `StockCheckService` (Benachrichtigungen) schliesst Wagen bereits aus. View-Farbcodierung ebenfalls

## Logging (Serilog)

- **Daily Rolling**: `logs/idealakewms-YYYYMMDD.log`, 30 Tage Aufbewahrung
- **Level**: Default `Information`, EF Core auf `Warning` (Unterdrückung von SQL-Spam)
- **Format**: `{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}`
- **Config**: `appsettings.json` → `Serilog`-Sektion

## Test-Setup

- xUnit + FluentAssertions + Moq + EF InMemory
- `TestApplicationDbContext` überschreibt `SaveChanges` für RowVersion-Handling
- `TestDbContextFactory.CreateContext()` erzeugt frischen InMemory-Kontext
- Kein `rowversion` in InMemory → alle Timestamp-Properties werden manuell gesetzt

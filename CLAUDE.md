# IdealAkeWms — Kontext für KI-Assistenten

## Architektur

- ASP.NET Core 10.0 MVC + Repository Pattern + DI
- EF Core 10.0 mit SQL Server (`AKESQL20.ake.at`, DB: `IDEAL_AKE_WMS`)
- Dual-Auth: Windows/Negotiate (IIS) + Session-basierter App-Login (Middleware in Program.cs)
- BOM-Daten kommen aus externer SQL-View `[ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]`
- `CachedBomRepository` wrapped `BomRepository` (Decorator-Pattern, 5 min MemoryCache)

## Konventionen

- **Sprache**: Code/Variablen auf Englisch, UI-Texte auf Deutsch
- **Workflow**: IMMER Planning Mode verwenden bevor Code-Änderungen gemacht werden
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
  - Angewendet auf: `UsersController`, `WorkstationsController`, `SettingsController`
- **`HasMasterDataAccessAsync()`**: Prüft zuerst `User.HasMasterDataAccess` Flag, dann AD-Gruppe aus AppSetting `StammdatenADGruppe`

## ICurrentUserService

```csharp
string GetWindowsUserName();          // HttpContext.User.Identity.Name
string GetDisplayName();              // App-Name (Session) oder Windows-Name (domain\user → user)
int? GetCurrentAppUserId();           // Session["AppUserId"]
string? GetCurrentAppUserName();      // Session["AppUserName"]
bool IsLoggedIn();                    // AppUserId != null
Task<bool> HasMasterDataAccessAsync(); // Flag + AD-Gruppe
```

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

## Standard-Daten (Neuinstallation)

| Typ | Wert | Beschreibung |
|-----|------|-------------|
| Benutzer | `admin` / Passwort leer | Standard-Admin (`HasMasterDataAccess = true`), Seeding in `Program.cs` |
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
| `StammdatenADGruppe` | `BDE_Stammdaten` | AD-Gruppe für Stammdaten-Zugriff |
| `BeschichtungAbholtage` | `Dienstag,Donnerstag` | Wochentage für Beschichtungs-Abholung |

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
- `Controllers/ProductionOrdersController.cs` — WA + BOM + Kommissionierung
- `Controllers/StockMovementsController.cs` — Ein/Aus/Umbuchung + OutboundAll
- `Controllers/Api/ArticlesApiController.cs` — Select2-Suche für Artikel
- `Filters/RequireMasterDataAccessAttribute.cs` — Zugriffskontrolle für Stammdaten
- `Services/PickingTransferService.cs` — Umbuchen gepickter Artikel (Transaktion)
- `Services/PasswordService.cs` — PBKDF2-HMAC-SHA256, 100k Iterationen
- `Services/PrintService.cs` — Drucken via `rundll32.exe mshtml.dll,PrintHTML`
- `Data/Repositories/StockMovementRepository.cs` — Bestandsberechnung
- `Data/Repositories/BomRepository.cs` — SQL-Query auf externe VIEW
- `Views/Shared/_Layout.cshtml` — Navbar + TempData-Alerts + User-Dropdown
- `Views/Shared/_Select2ArticlePartial.cshtml` — Select2-Integration (Artikel)
- `Views/ProductionOrders/Bom.cshtml` — Stücklisten-View (Baum, Picking, Transfer-AJAX)
- `Views/Account/Profile.cshtml` — Profil-Seite (Passwort + BOM-Filter Self-Service)
- `wwwroot/css/site.css` — Corporate Design, Navbar-Styles, `.navbar-logo-wrapper`
- `wwwroot/images/ideal-ake-logo.svg` — Logo (eingebettetes PNG, weißer Hintergrund)
- `SQL/00_FreshInstall.sql` — Konsolidiertes Neuinstallations-Script
- `SQL/AgentJobs/` — SQL Server Agent Job Scripts (Sage-Import)

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

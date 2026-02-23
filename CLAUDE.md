# IdealAkeWms — Kontext für KI-Assistenten

## Architektur

- ASP.NET Core 10.0 MVC + Repository Pattern + DI
- EF Core 10.0 mit SQL Server (`AKESQL20.ake.at`, DB: `IDEAL_AKE_WMS`)
- Dual-Auth: Windows/Negotiate (IIS) + Session-basierter App-Login (Middleware in Program.cs)
- BOM-Daten kommen aus externer SQL-View `[ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]`
- `CachedBomRepository` wrapped `BomRepository` (Decorator-Pattern, 5min MemoryCache)

## Konventionen

- **Sprache**: Code/Variablen auf Englisch, UI-Texte auf Deutsch
- **Workflow**: IMMER Planning Mode verwenden bevor Code-Änderungen gemacht werden
- **Entity-Basis**: `AuditableEntity` (Id, CreatedAt, CreatedBy, CreatedByWindows, Modified*)
- **Corporate Design**: `--ake-primary: #053153`, `--ake-secondary: #43A6E2`, `--ake-orange: #E87A1E`
- **Dokumentation**: `PROJECT_STATUS.md` im Root pflegen
- **Testing**: Bei Code-Änderungen mögliche Testfälle vorschlagen und nach Adaptierungen umsetzen
- **Git**: Sinnvolle Commit Messages verwenden, Änderungen nachvollziehbar committen
- **README**: `README.md` bei Feature-Änderungen aktuell halten

## Bekannte Fallstricke

- **Artikelnummer vs Ressourcenummer**: `Artikelnummer` = Geräte-Artikelnummer (WA), `Ressourcenummer` = Bauteil-Artikelnummer. Immer `Ressourcenummer` für Bauteil-Operationen verwenden!
- **Bootstrap table styles** überschreiben custom CSS → `!important` nötig
- **Alphabetische Sortierung von Positionsnummern**: `NaturalPositionComparer` verwenden (1, 2, 10 statt 1, 10, 2)
- **InMemory DB** unterstützt kein `rowversion` → Tests nutzen `TestApplicationDbContext` mit Override für RowVersion
- **SQL Server Batch-Parsing**: Tabellen in separatem Batch erstellen (GO dazwischen), bei Bedarf `EXEC sp_executesql` oder `OBJECT_ID` Guard verwenden
- **0-Bestände**: `GetCurrentStockAsync()` filtert standardmäßig Bestände mit 0 aus (nur wenn kein expliziter Min/Max-Filter)

## AppSettings (DB-Tabelle)

| Key | Default | Beschreibung |
|-----|---------|-------------|
| `KommissionierTage` | `4` | Arbeitstage vor Fertigungstermin |
| `VorkommissionierTage` | `1` | Tage vor Kommissioniertermin |
| `BeschichtungTage` | `10` | Tage vor Kommissionierung für Beschichtung |
| `WarningThresholdPercent` | `150` | Meldebestand Warnschwelle (%) |
| `CriticalThresholdPercent` | `100` | Meldebestand kritisch (%) |
| `NegativeBuchungErlaubt` | `false` | Negative Buchungen erlauben |
| `NegativeBuchungLagerplatz` | `NAN` | Fallback-Lagerplatz |

## Migrations-Workflow

1. Model ändern
2. `dotnet ef migrations add <MigrationName>` (generiert Migration in `Migrations/`)
3. SQL-Script für manuelle Produktion erstellen: `SQL/XX_<Name>.sql`
4. SQL-Script mit `OBJECT_ID` Guards idempotent machen
5. Migrations-History markieren: `INSERT INTO __EFMigrationsHistory` (in separatem Batch!)
6. App startet mit `db.Database.Migrate()` → überspringt bereits angewendete Migrationen

## Wichtige Dateien

- `Program.cs` — DI-Registrierung, Middleware-Pipeline, Session-Auth
- `Controllers/ProductionOrdersController.cs` — WA + BOM + Kommissionierung
- `Controllers/StockMovementsController.cs` — Ein/Aus/Umbuchung + OutboundAll
- `Services/PickingTransferService.cs` — Umbuchen gepickter Artikel (Transaktion)
- `Data/Repositories/StockMovementRepository.cs` — Bestandsberechnung (Netto aus Ein/Aus/Umbuchung)
- `Data/Repositories/BomRepository.cs` — SQL-Query auf externe VIEW
- `Views/ProductionOrders/Bom.cshtml` — Stücklisten-View (Baum, Picking, Transfer-AJAX)

## Test-Setup

- xUnit + FluentAssertions + Moq + EF InMemory
- `TestApplicationDbContext` überschreibt SaveChanges für RowVersion-Handling
- `TestDbContextFactory.CreateContext()` erzeugt frischen InMemory-Kontext

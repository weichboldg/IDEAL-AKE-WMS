# IDEAL-AKE WMS — Lagerverwaltung & BDE

Webbasiertes Warehouse Management System (WMS) und Betriebsdatenerfassung für IDEAL-AKE.

## Tech-Stack

- **Backend**: ASP.NET Core 10.0, Entity Framework Core 10.0
- **Datenbank**: SQL Server (AKESQL20.ake.at), DB: `IDEAL_AKE_WMS`
- **Authentifizierung**: Windows/Negotiate (IIS) + Session-basierter App-Login
- **Frontend**: Bootstrap 5, jQuery, Select2, html5-qrcode
- **Logging**: Serilog (daily rolling files)
- **Architektur**: MVC + Repository Pattern + Dependency Injection

## Voraussetzungen

- .NET 10 SDK
- SQL Server (Zugang zu AKESQL20.ake.at)
- IIS mit Windows-Authentifizierung (Produktion)
- AD-Umgebung für Windows-Auth

## Installation

### 1. Datenbank einrichten

#### Neuinstallation (empfohlen)

`SQL/00_FreshInstall.sql` auf dem SQL Server ausführen — erstellt alle Tabellen, Views, Indexes und Standard-Daten in einem Script.

Beim ersten App-Start werden automatisch angelegt (Startup-Seeding in `Program.cs`):
- Benutzer **`admin`** mit leerem Passwort und Stammdaten-Zugriff
- Lagerplatz **`NAN`** als Fallback für negative Buchungen

#### Update bestehender Installation

SQL-Scripte in Reihenfolge auf dem SQL Server ausführen:

| Nr. | Script | Beschreibung |
|-----|--------|-------------|
| 01 | `SQL/01_CreateDatabase.sql` | Datenbank erstellen |
| 02 | `SQL/02_CreateTables.sql` | Tabellen anlegen |
| 03 | `SQL/03_CreateViews.sql` | Views erstellen |
| 04 | `SQL/04_AlterTables.sql` | Tabellen erweitern |
| 05 | `SQL/05_CreateProductionOrders.sql` | Werkstattaufträge |
| 06 | `SQL/06_CreateSettings.sql` | AppSettings + Feiertage + Meldebestand |
| 07 | `SQL/07_Extensions.sql` | Erweiterungen |
| 08 | `SQL/08_PickingStatus.sql` | Picking-Status |
| 09 | `SQL/09_PickingItemIsBaugruppe.sql` | Baugruppen-Flag |
| 10 | `SQL/10_WorkstationDefaultPrinter.sql` | Drucker-Zuordnung |
| 11 | `SQL/11_InitMigrationsHistory.sql` | EF Migrations Baseline |
| 12 | `SQL/12_AddRowVersion.sql` | Optimistic Concurrency |
| 13 | `SQL/13_NegativeBuchungSettings.sql` | Negative Buchungs-Settings |
| 14 | `SQL/14_AddUserDefaultBomFilters.sql` | Standard-BOM-Filter pro Benutzer |
| 15 | `SQL/15_AddUserMasterDataAccess.sql` | Stammdaten-Zugriffsberechtigung |
| 16 | `SQL/16_AddMasterDataAdGroupSetting.sql` | AD-Gruppen-Setting für Stammdaten |
| 17 | `SQL/17_AddStorageLocationIsPickingScale.sql` | Kommissionierwagen-Flag |
| 18 | `SQL/18_ExtendProductionOrderLength.sql` | Feld-Längen erweitern |
| 19 | `SQL/19_AddBeschichtungAbholtage.sql` | Beschichtung-Abholtage Setting |
| 20 | `SQL/20_RenamePickingScaleToTransport.sql` | IsPickingScale → IsPickingTransport |
| 21 | `SQL/21_AddGlassAndPurchaseColumns.sql` | Glas/Zukauf Spalten auf WA |
| 22 | `SQL/22_AddProductionWorkplaces.sql` | Tabelle Werkbänke (ProductionWorkplaces) |
| 23 | `SQL/23_AddRecursiveFilterSearch.sql` | User-Setting: Rekursive Suche in Stückliste |

### 2. ConnectionStrings konfigurieren

In `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=AKESQL20.ake.at;Database=IDEAL_AKE_WMS;Trusted_Connection=True;TrustServerCertificate=True;",
    "OseonConnection": "Server=aketrumpf01.ake.at\\TRUMPFSQL2;Database=T1000_V01_V001;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

`OseonConnection` wird für den OSEON/TRUMPF-Fallback in der Stückliste benötigt (wenn SAGE keine BOM-Daten liefert).

### 3. Starten

```bash
cd IdealAkeWms
dotnet run
```

Die App startet und führt beim ersten Start automatisch `Database.Migrate()` aus.

## Features

### Lagerbewegungen
- **Einbuchung**: Artikel auf Lagerplatz einbuchen (Barcode-Scan oder Auswahl)
- **Ausbuchung**: Artikel von Lagerplatz ausbuchen mit Bestandsprüfung
- **Umbuchung**: Artikel zwischen Lagerplätzen umbuchen
- **Lagerplatz ausbuchen**: Alle Artikel eines Lagerplatzes auf einmal ausbuchen
- **Lagerplatz umbuchen**: Alle Artikel eines Lagerplatzes en bloc auf einen anderen Lagerplatz umbuchen (mit Barcode-Scan für Quell und Ziel, Vorschau vor Bestätigung)
- **Negative Buchungen**: Per AppSetting konfigurierbar — Fallback auf NAN-Lagerplatz

### Bestandsübersicht
- Aktueller Bestand pro Artikel/Lagerplatz
- Filter nach Artikel, Lagerplatz, Min/Max-Menge
- Meldebestand-Warnung (farblich: gelb/rot)
- 0-Bestände werden standardmäßig ausgeblendet

### Bewegungshistorie
- Paginierte Übersicht aller Buchungen
- Filter nach Datum, Artikel, Lagerplatz, Buchungsart, Benutzer, Fertigungsauftrag

### Werkstattaufträge (WA)
- Synchronisation mit Sage (über SQL-View)
- Terminberechnung: Kommissionierung, Vorkommissionierung, Beschichtung
- Status-Management (offen, in Kommissionierung, teilkommissioniert, abgeschlossen)
- **Glas/Zukauf**: Checkbox-Spalten direkt in der Tabelle, sofortige DB-Speicherung

### Kommissionierung (Stückliste)
- Mehrstufiger klappbarer Baumstruktur-View der Stückliste (standardmäßig eingeklappt)
- Picking: Checkbox pro Bauteil mit Quell-Lagerplatz-Auswahl
- Baugruppen-Picking: Komplette Baugruppe auf einmal kommissionieren
- Umbuchen: Gepickte Artikel vom Quell- auf Ziel-Lagerplatz buchen (WA wird automatisch vermerkt)
- Kommissionierwagen-Konfliktprüfung (verschiedene WA auf gleichem Wagen)
- **Spaltenfilter mit erweiterter Syntax**: `960,886` (OR-Verknüpfung), `!960` (Ausschluss)
- **Rekursive Suche**: User-Setting — bei aktivem Filter werden alle passenden Positionen angezeigt, unabhängig vom Baum-Status der übergeordneten Baugruppe
- **Drucken mit Filterübertragung**: Druck übernimmt aktuelle Filterung und Baumstruktur
- Foto-Upload pro Werkstattauftrag
- **BOM-Datenquelle**: SAGE (View) → Fallback OSEON/TRUMPF (Stored Procedure); Quelle wird als Badge im Header angezeigt (SAGE / OSEON / Keine Daten)

### Barcode/QR-Scanner
- html5-qrcode Integration für Kamera-Scan (HTTPS) und Bild-Upload
- Unterstützte Formate: QR-Code, Code 128, Code 39, EAN-13, EAN-8, Code 93
- Lagerplatz-Code max. 12 Zeichen für zuverlässige Barcode-Erkennung

### Mein Profil (Self-Service)
- Jeder angemeldete Benutzer kann unter dem Benutzer-Dropdown → **Mein Profil** sein eigenes Passwort ändern sowie die Standard-BOM-Filter (Beschaffung, Artikelgruppe) einstellen
- Diese Filter werden beim Öffnen einer Stückliste automatisch gesetzt
- **Rekursive Suche**: Checkbox-Option — wenn aktiv, werden bei aktivem Filter alle passenden BOM-Positionen angezeigt, auch wenn ihre Baugruppe eingeklappt ist

### Stammdaten
- **Lagerplätze**: Code (max. 12 Zeichen), Zone, Kapazität, Barcode-Etiketten drucken (A4, 3 pro Seite); `NAN` ist Standard-Fallback-Lagerplatz
- **Artikel**: Artikelnummer, Bezeichnung, Einheit, Meldebestand
- **Anwender**: Name, Personalnummer, Passwort, Aktiv-Flag, Stammdaten-Zugriff, Standard-BOM-Filter; Standard-Admin: `admin` / leer
- **Arbeitsstationen**: Zuordnung Anwender + Default-Drucker
- **Werkbänke**: Produktionsarbeitsplätze mit Bezeichnung, Halle und abweichenden Vorkommissioniertagen
- **Einstellungen**: Key-Value AppSettings + Feiertagsverwaltung

### Hilfe
- Integrierte Hilfe-Seite mit Anleitungen zu allen Funktionen (Footer-Link)

## SQL Server Agent Jobs

Im Ordner `SQL/AgentJobs/` liegen Scripts für automatische Sage-Daten-Imports:

| Script | Quell-Objekt | Ziel |
|--------|-------------|------|
| `01_Import_Produktionsauftraege.sql` | `[ake].[dbo].[vw_AKE_Kommissionierung_WAListe]` | `ProductionOrders` |
| `02_Import_Artikel.sql` | `KHKPpsRessourcenPositionen` + `KHKArtikel` | `Articles` |

`01_Import_Produktionsauftraege.sql` verwendet ein `MERGE`-Statement: neue Aufträge werden eingefügt, bestehende bei Änderungen in SAGE (Fertigungstermin, Liefertermin, Stückzahl etc.) aktualisiert. App-verwaltete Felder (`IsDone`, `PickingStatus`, `HasGlass`, `HasExternalPurchase`) werden nicht überschrieben.

Bei Änderungen der Tabellenstruktur müssen diese Scripts angepasst werden.

## AppSettings

| Schlüssel | Default | Beschreibung |
|-----------|---------|-------------|
| `KommissionierTage` | `4` | Arbeitstage vor Fertigungstermin |
| `VorkommissionierTage` | `1` | Zusätzliche Tage vor Kommissionierung |
| `BeschichtungTage` | `10` | Arbeitstage vor Kommissionierung für Beschichtung |
| `WarningThresholdPercent` | `150` | Meldebestand Warnschwelle (%) |
| `CriticalThresholdPercent` | `100` | Meldebestand kritische Schwelle (%) |
| `NegativeBuchungErlaubt` | `false` | Negative Buchungen erlauben |
| `NegativeBuchungLagerplatz` | `NAN` | Fallback-Lagerplatz bei negativem Bestand |

## Corporate Design

- Primary: `#053153` (Dunkelblau)
- Secondary: `#43A6E2` (Hellblau)
- Orange: `#E87A1E` (Warnung/Ausbuchung)
- CSS-Variablen: `--ake-primary`, `--ake-secondary`, `--ake-orange`

## Projekt-Struktur

```
IdealAkeWms/
├── Controllers/          # MVC Controller (inkl. AccountController mit Profil)
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Repositories/     # Repository-Implementierungen
├── Media/                # Originale Medien-Assets (Logo, Favicon)
├── Models/
│   └── ViewModels/       # View-spezifische Models (inkl. ProfileViewModel)
├── Services/             # Business-Logik
├── Views/                # Razor Views
├── wwwroot/
│   ├── css/site.css      # Custom Styles
│   ├── images/           # Logo (ideal-ake-logo.svg)
│   ├── favicon.ico       # Favicon
│   └── js/               # barcode-scanner, table-filter, photo-upload
├── Migrations/           # EF Core Migrations
└── SQL/
    ├── 00_FreshInstall.sql   # Komplettes Neuinstallations-Script
    ├── 01-22_*.sql           # Einzel-Migrations für bestehende Installationen
    └── AgentJobs/            # SQL Server Agent Job Scripts (Sage-Import)

IdealAkeWms.Tests/
├── Helpers/              # TestDbContextFactory (InMemory + RowVersion-Workaround)
├── Repositories/         # Repository-Tests (StockMovement, Picking, LocationTransfer, ProductionWorkplace)
└── Services/             # Service-Tests (BusinessDay, NaturalPositionComparer, Password)
```

## Tests

```bash
cd IdealAkeWms.Tests
dotnet test
```

- xUnit + FluentAssertions + Moq
- EF Core InMemory Provider für Repository-Tests

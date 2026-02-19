# AKEBDELight — Lagerverwaltung & BDE

Webbasiertes Lagerverwaltungs- und Betriebsdatenerfassungssystem (BDE) für AKE.

## Tech-Stack

- **Backend**: ASP.NET Core 10.0, Entity Framework Core 10.0
- **Datenbank**: SQL Server (AKESQL20.ake.at), DB: `AKE_BDE_Light`
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

### 2. ConnectionString konfigurieren

In `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=AKESQL20.ake.at;Database=AKE_BDE_Light;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 3. Starten

```bash
cd AKEBDELight
dotnet run
```

Die App startet und führt beim ersten Start automatisch `Database.Migrate()` aus.

## Features

### Lagerbewegungen
- **Einbuchung**: Artikel auf Lagerplatz einbuchen (Barcode-Scan oder Auswahl)
- **Ausbuchung**: Artikel von Lagerplatz ausbuchen mit Bestandsprüfung
- **Umbuchung**: Artikel zwischen Lagerplätzen umbuchen
- **Lagerplatz ausbuchen**: Alle Artikel eines Lagerplatzes auf einmal ausbuchen
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

### Kommissionierung (Stückliste)
- Mehrstufiger klappbarer Baumstruktur-View der Stückliste
- Picking: Checkbox pro Bauteil mit Quell-Lagerplatz-Auswahl
- Baugruppen-Picking: Komplette Baugruppe auf einmal kommissionieren
- Umbuchen: Gepickte Artikel vom Quell- auf Ziel-Lagerplatz buchen (WA wird automatisch vermerkt)
- Stückliste drucken (A4 Portrait, mit Lagerplatz-Info)
- Foto-Upload pro Werkstattauftrag

### Barcode/QR-Scanner
- html5-qrcode Integration für Kamera-Scan
- Unterstützt: Lagerplatz-Barcodes, Artikel-QR-Codes

### Stammdaten
- **Lagerplätze**: Code, Zone, Kapazität, Barcode-Etiketten drucken
- **Artikel**: Artikelnummer, Bezeichnung, Einheit, Meldebestand
- **Anwender**: Name, Personalnummer, Passwort, Aktiv-Flag
- **Arbeitsstationen**: Zuordnung Anwender + Default-Drucker
- **Einstellungen**: Key-Value AppSettings + Feiertagsverwaltung

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
AKEBDELight/
├── Controllers/          # MVC Controller
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Repositories/     # Repository-Implementierungen
├── Models/
│   └── ViewModels/       # View-spezifische Models
├── Services/             # Business-Logik
├── Views/                # Razor Views
├── wwwroot/
│   ├── css/site.css      # Custom Styles
│   └── js/               # barcode-scanner, table-filter, photo-upload
├── Migrations/           # EF Core Migrations
└── SQL/                  # Manuelle SQL-Scripte für Produktion
```

## Tests

```bash
cd AKEBDELight.Tests
dotnet test
```

- xUnit + FluentAssertions + Moq
- EF Core InMemory Provider für Repository-Tests

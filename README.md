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
| 24 | `SQL/24_AddUserEmailIsAdminNotify.sql` | User: Email, IsAdmin, NotifyOnReorderLevel |
| 25 | `SQL/25_AddServiceSettings.sql` | Tabelle ServiceSettings + Standard-Einträge |

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

IDEALAKEWMSService/        # Windows Service (Hintergrundprozesse)
├── Workers/
│   ├── SyncWorker.cs         # Placeholder: Schnittstellenabgleich IDEAL_AKE_WMS ↔ SAGE/OSEON
│   └── NotificationWorker.cs # Placeholder: Mail-Notifications (Meldebestand, Fehler)
├── Program.cs            # Startup: UseWindowsService(), DI, Serilog
└── appsettings.json      # ConnectionStrings, MailSettings, WorkerSettings
```

## Windows Service (IDEALAKEWMSService)

Separates Projekt für Hintergrundprozesse, lauffähig als Windows Service.

### Build

#### Variante A: Self-Contained (empfohlen für Produktion)

Kein .NET auf dem Zielserver notwendig — alles in einer EXE:

```bash
dotnet publish IDEALAKEWMSService -c Release -r win-x64 --self-contained -o C:\Deploy\IDEALAKEWMSService
```

#### Variante B: Framework-Dependent

.NET 10 Runtime muss auf dem Zielserver installiert sein:

```bash
dotnet publish IDEALAKEWMSService -c Release -r win-x64 --self-contained false -o C:\Deploy\IDEALAKEWMSService
```

#### Entwicklung / lokaler Test (kein echtes Windows Service, läuft als Konsolenapp)

```bash
dotnet run --project IDEALAKEWMSService
```

---

### Erstinstallation (Deployment)

#### Schritt 1 — Dateien auf den Server kopieren

Das Publish-Verzeichnis (`C:\Deploy\IDEALAKEWMSService`) auf den Zielserver kopieren, z. B. nach:

```
C:\Services\IDEALAKEWMSService\
```

#### Schritt 2 — appsettings.json konfigurieren

`appsettings.json` im Deploy-Verzeichnis mit echten Werten befüllen:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=AKESQL20.ake.at;Database=IDEAL_AKE_WMS;Trusted_Connection=True;TrustServerCertificate=True;",
    "SageConnection":    "Server=AKESQL20.ake.at;Database=ake;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "MailSettings": {
    "SmtpHost":     "mail.ake.at",
    "SmtpPort":     25,
    "SmtpUseSsl":   false,
    "SmtpUsername": "",
    "SmtpPassword": "",
    "SenderName":   "IDEAL AKE WMS",
    "FromAddress":  "wms@ake.at"
  },
  "WorkerSettings": {
    "SyncIntervalMinutes":              15,
    "NotificationCheckIntervalMinutes": 60,
    "SyncDryRun":                       false
  }
}
```

> **DryRun-Tipp**: Zuerst `SyncDryRun: true` setzen und parallel mit den SQL Agent Jobs testen. Erst nach erfolgreichem Test auf `false` umstellen und die Agent Jobs deaktivieren.

#### Schritt 3 — Windows Service registrieren

PowerShell als Administrator auf dem Zielserver:

```powershell
New-Service `
  -Name        "IDEALAKEWMSService" `
  -DisplayName "IDEAL AKE WMS Service" `
  -BinaryPathName "C:\Services\IDEALAKEWMSService\IDEALAKEWMSService.exe" `
  -StartupType Automatic `
  -Description "Hintergrundprozesse fuer IDEAL AKE WMS (Sync + Benachrichtigungen)"
```

Alternativ mit `sc.exe`:

```cmd
sc create "IDEALAKEWMSService" ^
  binPath="C:\Services\IDEALAKEWMSService\IDEALAKEWMSService.exe" ^
  DisplayName="IDEAL AKE WMS Service" ^
  start=auto
```

#### Schritt 4 — Service starten

```powershell
Start-Service -Name "IDEALAKEWMSService"

# Status prüfen
Get-Service -Name "IDEALAKEWMSService"
```

---

### Service-Verwaltung

```powershell
# Starten
Start-Service -Name "IDEALAKEWMSService"

# Stoppen
Stop-Service  -Name "IDEALAKEWMSService"

# Neu starten
Restart-Service -Name "IDEALAKEWMSService"

# Status anzeigen
Get-Service -Name "IDEALAKEWMSService"
```

Oder über **Windows-Dienste** (services.msc): Dienst "IDEAL AKE WMS Service" suchen.

---

### Update (neue Version einspielen)

```powershell
# 1. Service stoppen
Stop-Service -Name "IDEALAKEWMSService"

# 2. Neue Dateien ins Deploy-Verzeichnis kopieren (appsettings.json NICHT überschreiben!)
Copy-Item -Path "C:\Deploy\IDEALAKEWMSService\*" `
          -Destination "C:\Services\IDEALAKEWMSService\" `
          -Recurse -Force `
          -Exclude "appsettings.json"

# 3. Service wieder starten
Start-Service -Name "IDEALAKEWMSService"
```

> **Wichtig**: `appsettings.json` beim Update nie überschreiben — enthält die produktiven Connection Strings.

---

### Deinstallation

```powershell
Stop-Service    -Name "IDEALAKEWMSService" -Force
Remove-Service  -Name "IDEALAKEWMSService"
Remove-Item     -Path "C:\Services\IDEALAKEWMSService\" -Recurse -Force
```

---

### Log-Dateien

Der Service schreibt separate Logs pro Funktion (30 Tage Retention).
Standard-Pfad: relativ zum Executable, also z. B. `C:\Services\IDEALAKEWMSService\logs\`

| Datei | Inhalt |
|-------|--------|
| `logs\service-YYYYMMDD.log` | Gesamtlog des Service (alle Meldungen) |
| `logs\sync\sync-YYYYMMDD.log` | SyncWorker + SageImportService |
| `logs\notifications\notifications-YYYYMMDD.log` | NotificationWorker + StockCheckService + MailService |

### Funktionen

**SyncWorker** (alle `WorkerSettings:SyncIntervalMinutes` Minuten, default 15):
- Produktionsaufträge aus SAGE (`vw_AKE_Kommissionierung_WAListe`) → WMS (`ProductionOrders`) — MERGE (Insert + Update)
- Artikel aus SAGE (`KHKPpsRessourcenPositionen` + `KHKArtikel`) → WMS (`Articles`) — nur neue
- Konfigurierbar: `Sync:ProductionOrdersEnabled`, `Sync:ArticlesEnabled` (in ServiceSettings-Tabelle)
- **DryRun-Modus**: `WorkerSettings:SyncDryRun = true` → nur Logging, keine DB-Änderungen (Testphase neben SQL Agent Jobs)
- Logs in `logs/sync/sync-YYYYMMDD.log` (30 Tage Retention)

**NotificationWorker** (alle `WorkerSettings:NotificationCheckIntervalMinutes` Minuten, default 60):
- Prüft Artikel mit Bestand unter Meldebestand (`Article.ReorderLevel`)
- Sendet HTML5-Mail im AKE CI (Dunkelblau/Hellblau) an konfigurierte Empfänger
- Empfänger: `Notifications:Recipients` (ServiceSettings, kommagetrennt) + alle User mit `NotifyOnReorderLevel = true`
- Konfigurierbar: `Notifications:MeldebestandEnabled`, `MeldebestandSubject`, `AppBaseUrl`
- Logs in `logs/notifications/notifications-YYYYMMDD.log` (30 Tage Retention)

### Konfiguration (appsettings.json)

Datenbankverbindungen und grundlegende Einstellungen in `IDEALAKEWMSService/appsettings.json`:

| Setting | Beschreibung |
|---------|-------------|
| `ConnectionStrings:DefaultConnection` | WMS-Datenbank (`IDEAL_AKE_WMS`) |
| `ConnectionStrings:SageConnection` | SAGE-Datenbank (`ake` auf `AKESQL20.ake.at`) |
| `MailSettings:SmtpHost` | SMTP-Server für Mail-Versand |
| `MailSettings:SmtpPort` | SMTP-Port (default 25) |
| `MailSettings:FromAddress` | Absender-Adresse |
| `WorkerSettings:SyncIntervalMinutes` | Sync-Intervall in Minuten (default 15) |
| `WorkerSettings:NotificationCheckIntervalMinutes` | Benachrichtigungs-Intervall in Minuten (default 60) |
| `WorkerSettings:SyncDryRun` | `true` = nur Logging, keine DB-Änderungen |

Laufzeitveränderliche Einstellungen in der `ServiceSettings`-Tabelle (Admin-Bereich der Web-App):

| Key | Beschreibung |
|-----|-------------|
| `Notifications:MeldebestandEnabled` | Meldebestand-Mail aktiv (`true`/`false`) |
| `Notifications:MeldebestandSubject` | E-Mail-Betreff |
| `Notifications:Recipients` | Feste Empfänger (kommagetrennt) |
| `Notifications:AppBaseUrl` | App-URL für Links in Mails |
| `Sync:ProductionOrdersEnabled` | Produktionsaufträge-Sync aktiv |
| `Sync:ArticlesEnabled` | Artikel-Sync aktiv |

### Service-Einstellungen verwalten

Im Browser: **Stammdaten → Service-Einstellungen** (nur für Benutzer mit `IsAdmin = true` sichtbar).

### Logging

Separate Log-Unterordner pro Funktion (30 Tage Retention):
- `logs/service-YYYYMMDD.log` — Gesamtlog des Service
- `logs/sync/sync-YYYYMMDD.log` — SyncWorker + SageImportService
- `logs/notifications/notifications-YYYYMMDD.log` — NotificationWorker + StockCheckService + MailService

### Zukünftige Erweiterungen (geplant)
- Meldebestand-Mail: Aufsplitten nach Artikelgruppe oder Lagerhalle (eigene Empfängerliste pro Gruppe)
- Lagerplätze in SAGE anlegen wenn neue im WMS erstellt werden
- Bestandsbuchung per SQL in SAGE DB
- XML für Bestandsbuchung im OSEON
- Synchronisierung Artikelzusatzinfos (Einheiten)

## Tests

```bash
# Alle Tests (beide Projekte)
dotnet test

# Nur Web-App Tests
dotnet test IdealAkeWms.Tests

# Nur Service Tests
dotnet test IDEALAKEWMSService.Tests
```

### Testabdeckung

**IdealAkeWms.Tests** — Web-App:
- `PasswordService` — Hash/Verify-Roundtrip, Salt-Randomness
- `BusinessDayService` — Arbeitstage-Berechnung inkl. Wochenenden und Feiertagen
- `NaturalPositionComparer` — natürliche Sortierung von Positionsnummern (1, 2, 10, 10.1)
- `StockMovementRepository` — Bestandsberechnung, Umbuchungen
- `PickingRepository` — Picking-Initialisierung, Toggle, Transfer
- `ProductionWorkplaceRepository` — CRUD, Sortierung
- `ServiceSettingRepository` — Upsert, Delete, GetByCategory, GetValue
- `CurrentUserService.IsAdminAsync` — Admin-Flag-Prüfung, nicht eingeloggt, User nicht gefunden

**IDEALAKEWMSService.Tests** — Windows Service:
- `SyncWorker` — ProductionOrders/Articles enabled/disabled, DryRun-Modus, Exception-Resilienz
- `NotificationWorker` — Mail wird gesendet wenn Artikel unter Meldebestand, nicht gesendet wenn leer, HTML-Inhalt (Artikelnummern, AKE CI Farben, Ampelfarben), korrekte Empfänger, Exception-Resilienz, Fallback-Betreff

**Testinfrastruktur:**
- xUnit + FluentAssertions + Moq
- EF Core InMemory Provider für Repository-Tests (`TestApplicationDbContext` mit RowVersion-Handling)
- `FakeSession` für Session-basierte Service-Tests
- Worker-Tests mit `IServiceScopeFactory`-Mock und konfigurierbaren Intervallen

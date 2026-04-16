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

## IIS-Konfiguration (Performance)

Die `web.config` enthält ein `requestTimeout` von 5 Minuten (Standard: 2 Minuten). Das ist nötig für Seiten mit vielen Daten (z.B. OSEON Teileverfolgung).

**Wichtige IIS-Settings:**

| Setting | Wert | Beschreibung |
|---------|------|-------------|
| `requestTimeout` (web.config) | `00:05:00` | Max. Verarbeitungszeit pro Request (ASP.NET Core Module) |
| **Application Pool → Idle Timeout** | `0` (empfohlen) | Verhindert App-Pool-Recycling bei Inaktivität (IIS Manager → Application Pools → Advanced Settings) |
| **Application Pool → Start Mode** | `AlwaysRunning` | App sofort verfügbar nach IIS-Restart (IIS Manager → Application Pools → Advanced Settings) |
| **Site → Preload Enabled** | `True` | Erster Request ohne Wartezeit (IIS Manager → Site → Advanced Settings) |

**Application Pool konfigurieren (IIS Manager):**
1. Application Pools → den Pool der WMS-App auswählen → Advanced Settings
2. `Idle Time-out (minutes)` auf `0` setzen (kein Timeout)
3. `Start Mode` auf `AlwaysRunning` setzen
4. `Regular Time Interval (minutes)` auf `0` setzen (kein regelmäßiges Recycling) oder auf einen hohen Wert (z.B. `1740` = 29h)

**Site konfigurieren:**
1. Sites → IDEAL AKE WMS → Advanced Settings
2. `Preload Enabled` auf `True` setzen

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
| 05 | `SQL/05_CreateProductionOrders.sql` | Fertigungsaufträge |
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
| 26 | `SQL/26_AddUserCanPickCanViewTracking.sql` | User: CanPick, CanViewTracking Berechtigungen |
| 27 | `SQL/27_AddUserCanReportOperations.sql` | User: CanReportOperations Berechtigung |
| 28 | `SQL/28_AddQrMitFaNummer.sql` | AppSetting: QR-Code mit FA-Nummer |
| 29 | `SQL/29_AddOseonTracking.sql` | OSEON Teileverfolgung Tabellen + AppSettings |
| 30 | `SQL/30_OseonPerformanceIndexes.sql` | Performance-Indizes für OSEON-Tabellen |
| 31 | `SQL/31_AddOseonTimestamps.sql` | OSEON Delta-Sync Timestamps |
| 32 | `SQL/32_AddRoles.sql` | Rollen-Tabelle + UserRoles (Many-to-Many) |
| 33 | `SQL/33_RemoveOldPermissionColumns.sql` | Alte Boolean-Berechtigungsspalten entfernen (Phase 2) |
| 34 | `SQL/34_AddOseonOperationConfig.sql` | AG-Konfiguration fuer OSEON-Arbeitsgaenge |
| 35 | `SQL/35_AddEnaioDmsDocuments.sql` | enaio DMS-Dokument-Verknuepfung |
| 36 | `SQL/36_AddPartRequisitions.sql` | Bedarfsmeldungen + Empfaengergruppen |
| 37 | `SQL/37_AddPickingRelease.sql` | Leitstand-Felder + Rolle + AppSetting |
| 38 | `SQL/38_AddPickerAssignment.sql` | Kommissionierer-Zuweisung |
| 38b | `SQL/38_StockMovementPerformanceIndexes.sql` | Performance-Indizes Lagerbewegungen |
| 39 | `SQL/39_AddArticleCategoriesAndAttributes.sql` | Artikelkategorien + Merkmale (EAV) |
| 40 | `SQL/40_AddBomCacheAndCoatingDetection.sql` | BOM-Cache + Lackierteil-Erkennung |
| 41 | `SQL/41_AddUserViewPreferences.sql` | Benutzerspezifische Ansichts-Einstellungen |

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

### Fertigungsaufträge (FA)
- Synchronisation mit Sage (über SQL-View)
- Terminberechnung: Kommissionierung, Vorkommissionierung, Beschichtung
- Status-Management (offen, in Kommissionierung, teilkommissioniert, abgeschlossen)
- **Werkbank-Spalte**: Zeigt zugeordneten Produktionsarbeitsplatz — automatisch per Werkbank-Sync aus OSEON befüllt
- **OSEON-Button**: Direktlink zur OSEON-Teileverfolgung, vorgefiltert auf FA-Nummer
- **Glas/Zukauf**: Checkbox-Spalten direkt in der Tabelle, sofortige DB-Speicherung
- **Tracking-User**: Benutzer mit Teileverfolgungsberechtigung können die FA-Liste einsehen (read-only, ohne Stückliste/Erledigt)

### OSEON Teileverfolgung
- 3-Ebenen-Baumstruktur: KundenAuftragsNr → Subaufträge (OSEON-Nr.) → Arbeitsgänge
- **Ampelsystem**: Rot (überfällig), Gelb (fällig bald), Blau (demnächst), Grün (fertig/storniert), Grau (kein Termin)
- **Server-seitige Paginierung**: 25 Gruppen pro Seite
- **Filter**: Suche (Kundenauftragsnr. + OSEON-Nr.), Werkbank-Dropdown, Fertige anzeigen
- Gruppenstatus: Aggregiert über alle Subaufträge
- Konfigurierbar: `OseonAmpelGelbTage`, `OseonAmpelBlauTage` in AppSettings
- Voraussetzung: AppSetting `TeileverfolgungAktiv = true` + User-Berechtigung `CanViewTracking`

### Leitstand (Kommissionier-Freigabe)
- **Feature-Toggle**: Per AppSetting `LeitstandAktiv` aktivierbar (Default: aus). Bei Deaktivierung funktioniert alles wie bisher.
- **Freigabe**: Leitstand-User geben Produktionsaufträge zur Kommissionierung frei (Einzel- oder Massenfreigabe). Voraussetzung: Artikelnummer vorhanden.
- **Priorisierung**: Numerische Priorität (1 = höchste). Auto-Vorschlag bei Freigabe (MAX+1), inline änderbar.
- **Kommissionierliste**: Kommissionierer sehen nur freigegebene Aufträge in einer Tabelle mit Priorität, Kommissioniertermin (KW), Status.
- **Menü-Badge**: Anzahl offener freigegebener Aufträge im Menüpunkt "Kommissionierung".
- **Menü-Trennung**: Leitstand/Tracking sehen "Fertigungsaufträge", Kommissionierer sehen nur "Kommissionierung".
- Voraussetzung: Rolle `leitstand` + AppSetting `LeitstandAktiv = true`

### Kommissionierung (Stückliste)
- Mehrstufiger klappbarer Baumstruktur-View der Stückliste (standardmäßig eingeklappt)
- Picking: Checkbox pro Bauteil mit Quell-Lagerplatz-Auswahl (rein client-seitig, kein sofortiges Speichern)
- Baugruppen-Picking: Komplette Baugruppe auf einmal auswählen
- Umbuchen: Erst beim Klick auf "Gepickte Artikel umbuchen" werden alle ausgewählten Artikel gesammelt und in einer Transaktion vom Quell- auf Ziel-Lagerplatz gebucht (WA wird automatisch vermerkt)
- Kommissionierwagen-Konfliktprüfung (verschiedene WA auf gleichem Wagen)
- **Spaltenfilter mit erweiterter Syntax**: `960,886` (OR-Verknüpfung), `!960` (Ausschluss)
- **Rekursive Suche**: User-Setting — bei aktivem Filter werden alle passenden Positionen angezeigt, unabhängig vom Baum-Status der übergeordneten Baugruppe
- **Drucken mit Filterübertragung**: Druck übernimmt aktuelle Filterung und Baumstruktur
- Foto-Upload pro Fertigungsauftrag
- **BOM-Datenquelle**: SAGE (View) → Fallback OSEON/TRUMPF (Stored Procedure); Quelle wird als Badge im Header angezeigt (SAGE / OSEON / Keine Daten)

### Barcode/QR-Scanner
- html5-qrcode Integration für Kamera-Scan (HTTPS) und Bild-Upload
- Unterstützte Formate: QR-Code, Code 128, Code 39, EAN-13, EAN-8, Code 93
- Lagerplatz-Code max. 12 Zeichen für zuverlässige Barcode-Erkennung
- **QR mit FA-Nummer**: Per AppSetting `QrMitFaNummer` — extrahiert Fertigungsauftragsnummer aus QR (3. Teil, Komma-Suffix wird abgeschnitten) und füllt das FA-Feld in Ein/Aus/Umbuchung. Bei jedem Scan wird das FA-Feld zuerst geleert.
- **Kommissionierliste-Scan**: Gescannter Artikel wird in Stückliste gesucht und automatisch als ausgewählt markiert. Nicht gefunden → Modal mit Option "Nächsten Artikel scannen"

### Bedarfsmeldungen
- **Einzelbestellung**: Direkt aus der Stückliste per Bestell-Button neben einem Bauteil — Menge, Priorität (Normal/Dringend/Eilt) und Bemerkung angeben
- **Sammelbestellung**: Mehrere Bauteile per Checkbox auswählen → "Ausgewählte bestellen"
- **Empfänger-Routing**: Automatische Zuordnung der passenden Empfängergruppe über Artikelgruppen-Mapping; alle aktiven Empfänger werden vorausgewählt
- **E-Mail-Versand**: Offene Meldungen werden per Windows-Service automatisch versendet (AppSetting `BestellungenAktiv` + Service-Setting `Sync:PartRequisitionEmailEnabled`)
- **Bestellübersicht**: Alle Bedarfsmeldungen mit Status-Badges, Priorität, Filterung und Pagination
- **Wareneingang-Integration**: Bei Einbuchung werden offene Meldungen zum Artikel angezeigt und können verknüpft werden
- **Empfängergruppen (Stammdaten)**: CRUD für Gruppen + Empfänger + Artikelgruppen-Zuordnung (N:M)

### Betriebsdatenerfassung (BDE)
- **BDE-Terminal**: Scan-basierte Buchung (Personalnummer + FA/AG) mit Statusverwaltung (Ruesten, Produktion, Pause, Fortsetzen, Beenden)
- **Mengen-Erfassung**: Teilfertigmeldungen (Gutmenge + Ausschuss) und Abschlussmeldung
- **Ungeplante Taetigkeiten**: Wartung, Reinigung etc. via konfigurierbare Aktivitaets-Kategorien
- **Live-Cockpit**: 5-Sekunden-Refresh aller Werkbaenke fuer Schichtleiter und Admins
- **BDE-Stammdaten**: Operatoren, Aktivitaets-Kategorien, Terminal-Konfigurationen
- **Admin-Korrekturen**: Buchungs-Editor, Storno mit Grund, manuelles Schliessen vergessener Buchungen
- **Rollen**: `bde_user` (Terminal), `bde_shiftlead` (+ Stammdaten/Cockpit), `bde_admin` (+ Korrekturen/Terminals)

### Individuelle Ansichts-Einstellungen
- **Spalten ein-/ausblenden**: Zahnrad-Icon oben rechts ueber der Tabelle oder Rechtsklick auf Spaltenkopf
- **Spaltenbreiten aendern**: Am rechten Spaltenrand ziehen (Doppelklick = Standard-Breite)
- **Spaltenreihenfolge aendern**: Drag & Drop im Einstellungs-Panel (nur Fertigungsauftraege + Kommissionierliste)
- **Standard-Sortierung**: Im Einstellungs-Panel festlegen
- Einstellungen werden pro Benutzer automatisch gespeichert
- Admin kann Einstellungen im Benutzerstamm zuruecksetzen
- Verfuegbar in: Fertigungsauftraege, Kommissionierliste, OSEON Teileverfolgung, Stueckliste

### Berechtigungen (Rollenbasiert)
- **Rollenkonzept**: `Role`-Tabelle + `UserRole`-Junction (Many-to-Many), statische Keys in `RoleKeys.cs`
- **`admin`**: Vollzugriff (ueberspringt alle Berechtigungspruefungen)
- **`picking`**: Kommissionierung + vollstaendiger Lagerzugriff
- **`stock`**: Einbuchung, Ausbuchung, Bestaende
- **`stock_keyuser`**: Lager + Lagerplatz ausbuchen/umbuchen
- **`masterdata`**: Benutzer, Arbeitsplaetze, Einstellungen
- **`tracking`**: OSEON Auftraege + Rueckmeldungen
- **`leitstand`**: Produktionsauftraege freigeben und priorisieren
- **`reporting`**: Betriebsdaten / BDE (Zukunft)
- **`bde_user`**: Terminal-Buchung: Arbeitsgaenge scannen, Status wechseln
- **`bde_shiftlead`**: + BDE-Stammdaten, Buchungsliste, Cockpit
- **`bde_admin`**: + Buchungen korrigieren/stornieren, Terminals konfigurieren
- Dashboard zeigt nur Kacheln die der Rolle entsprechen

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
- **Einstellungen**: Key-Value AppSettings (Boolean-Werte als Toggle-Switches) + Feiertagsverwaltung

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
| ~~`StammdatenADGruppe`~~ | — | Ersetzt durch `Role.AdGroup` auf der Rolle 'masterdata' |
| `BeschichtungAbholtage` | `Dienstag,Donnerstag` | Wochentage für Beschichtungs-Abholung |
| `TeileverfolgungAktiv` | `false` | Teileverfolgungs-Modul aktiviert |
| `OseonRueckmeldungAktiv` | `false` | Rückmeldungen an OSEON zurückschreiben |
| `SageRueckmeldungAktiv` | `false` | Rückmeldungen an Sage zurückschreiben |
| `QrMitFaNummer` | `false` | QR-Code enthält FA-Nummer an 3. Stelle |
| `OseonAmpelGelbTage` | `1` | OSEON Ampel: Gelb ab X Tagen vor Termin |
| `OseonAmpelBlauTage` | `2` | OSEON Ampel: Blau ab X Tagen vor Termin |
| `BestellungenAktiv` | `false` | Bedarfsmeldungen aus Stückliste aktivieren |
| `LeitstandAktiv` | `false` | Leitstand: Kommissionier-Freigabe und Priorisierung |
| `KommissionierungMitZuweisung` | `false` | Kommissionierung mit Anwenderzuweisung aktivieren |
| `LackierteilKategorieName` | (leer) | Name der Artikelkategorie die als Lackierteil gilt |

## Corporate Design

- Primary: `#053153` (Dunkelblau)
- Secondary: `#43A6E2` (Hellblau)
- Orange: `#E87A1E` (Warnung/Ausbuchung)
- CSS-Variablen: `--ake-primary`, `--ake-secondary`, `--ake-orange`

## Projekt-Struktur

```
IdealAkeWms/
├── Controllers/
│   ├── ProductionOrdersController.cs  # FA-Liste, Leitstand-Freigabe
│   ├── PickingController.cs           # Kommissionierung, Stückliste, Transfer
│   ├── Api/PhotoController.cs         # Foto-Upload API
│   └── ...               # AccountController, TrackingController, etc.
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Repositories/     # Repository-Implementierungen
├── Media/                # Originale Medien-Assets (Logo, Favicon)
├── Models/
│   └── ViewModels/       # View-spezifische Models (inkl. ProfileViewModel)
├── Filters/              # Action Filter (RequirePickingAccess, RequireTrackingAccess, etc.)
├── Helpers/              # OseonStatusHelper
├── Services/             # Business-Logik (inkl. OseonTrafficLightService)
├── Views/                # Razor Views
├── wwwroot/
│   ├── css/site.css      # Custom Styles
│   ├── images/           # Logo (ideal-ake-logo.svg)
│   ├── favicon.ico       # Favicon
│   └── js/               # barcode-scanner, table-filter, column-preferences, site
├── Migrations/           # EF Core Migrations
└── SQL/
    ├── 00_FreshInstall.sql   # Komplettes Neuinstallations-Script
    ├── 01-41_*.sql           # Einzel-Migrations fuer bestehende Installationen
    └── AgentJobs/            # SQL Server Agent Job Scripts (Sage-Import)

IdealAkeWms.Tests/
├── Helpers/              # TestDbContextFactory (InMemory + RowVersion-Workaround)
├── Repositories/         # Repository-Tests (StockMovement, Picking, LocationTransfer, ProductionWorkplace)
└── Services/             # Service-Tests (BusinessDay, NaturalPositionComparer, Password)

IDEALAKEWMSService/        # Windows Service (Hintergrundprozesse)
├── Workers/
│   ├── SyncWorker.cs         # Schnittstellenabgleich SAGE/OSEON → WMS (Aufträge, Artikel, Tracking, Werkbank)
│   └── NotificationWorker.cs # Mail-Notifications (Meldebestand)
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
    "SageConnection":    "Server=AKESQL20.ake.at;Database=ake;Trusted_Connection=True;TrustServerCertificate=True;",
    "OseonConnection":   "Server=aketrumpf01.ake.at\\TRUMPFSQL2;Database=T1000_V01_V001;Trusted_Connection=True;TrustServerCertificate=True;"
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
- **OSEON-Tracking**: Produktionsaufträge + Arbeitsgänge aus OSEON-DB → WMS (`OseonProductionOrders`, `OseonWorkOperations`) — Upsert, Werkbänke auto-anlegen
- **Werkbank-Sync**: Überträgt `ProductionWorkplaceId` von OSEON-Aufträgen auf Sage-Aufträge (Match: `OrderNumber` ↔ `CustomerOrderNumber`), nur wo noch keine Werkbank gesetzt ist
- Konfigurierbar: `Sync:ProductionOrdersEnabled`, `Sync:ArticlesEnabled`, `Sync:OseonTrackingEnabled` (in ServiceSettings-Tabelle)
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
| `Sync:OseonTrackingEnabled` | OSEON-Tracking + Werkbank-Sync aktiv |

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

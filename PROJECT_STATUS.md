# IdealAkeWms - Projektstatus

## Projektbeschreibung
Lagerverwaltungs- und BDE-System (Betriebsdatenerfassung) für AKE.
ASP.NET Core 10.0, SQL Server (AKESQL20.ake.at), Windows-Authentifizierung.

## Architektur
- **Pattern**: MVC + Repository Pattern + DI
- **DB**: Entity Framework Core 10.0, Database: IDEAL_AKE_WMS
- **Auth**: Windows/Negotiate + Session (8h idle timeout)
- **Logging**: Serilog (daily rolling files)
- **Frontend**: Bootstrap 5, jQuery, custom JS (barcode-scanner, photo-upload, table-filter)

## Hauptfunktionen
| Bereich | Status |
|---------|--------|
| Lagerbewegungen (Ein/Aus/Umbuchung) | Fertig |
| Lagerplatz-Umbuchung (en bloc) | Fertig |
| Bestandsübersicht | Fertig |
| Bewegungshistorie | Fertig |
| Fertigungsaufträge | Fertig |
| Kommissionierung (BOM/Stückliste) | Fertig |
| BOM-Filter (Multi-Wert, Ausschluss) | Fertig |
| BOM-Druck mit Filterübertragung | Fertig |
| Barcode/QR-Scanner | Fertig |
| Stammdaten (Artikel, Lagerplätze, Benutzer, Arbeitsplätze) | Fertig |
| Werkbänke / Produktionsarbeitsplätze | Fertig |
| Feiertag-Import | Fertig |
| Foto-Upload bei Kommissionierung | Fertig |
| Server-seitiger Druck | Grundstruktur |
| OSEON Teileverfolgung | Fertig |
| OSEON AG-Konfiguration (Soll-Termine + Relevanz) | Fertig |
| Rollenbasierte Zugriffskontrolle | Fertig (Phase 1) |
| KW-Filter in Fertigungsauftraegen | Fertig |
| enaio DMS-Integration | Fertig |
| Bestandsuebersicht FA-Filter + QR-Scan | Fertig |
| Responsive Design (Mobile) | Fertig |
| Versionierung + Changelog | Fertig |
| Bedarfsmeldungen aus Stueckliste | Fertig |
| Leitstand (Kommissionier-Freigabe + Priorisierung) | Fertig |
| Kommissionierer-Zuweisung bei Freigabe | Fertig |

## Version
- **Web-App**: v1.6.1 (09.04.2026)
- **Service**: v1.4.0 (09.04.2026)

## Aenderungen (09.04.2026)

### v1.6.1 — Artikelinfo: Fertigungsauftraege aus BOM-Cache

#### Verbesserungen
- **Artikelinfo**: Neue Tabelle "Teil enthalten in folgenden Fertigungsauftraegen" in `Views/Articles/Info.cshtml` — zeigt offene Fertigungsauftraege in denen der Artikel als Bauteil enthalten ist (Datenquelle: CachedBomItem → CachedBomHeader → ProductionOrder)

### v1.4.0 — Kommissionierer-Zuweisung

#### Neue Funktionen
- **Kommissionierer-Zuweisung**: Bei der Leitstand-Freigabe kann ein Kommissionierer zugewiesen werden. Picking-Liste filtert standardmaessig auf eigene Auftraege.
- **Benutzermerkmal "Ist Kommissionierer"**: Neue Checkbox im Benutzerstamm — nur markierte Benutzer stehen im Zuweisung-Dropdown zur Auswahl.
- **AppSetting `KommissionierungMitZuweisung`**: Feature-Toggle (Default: false). Voraussetzung: `LeitstandAktiv` muss ebenfalls aktiv sein.

#### Neue DB-Spalten
- `ProductionOrders.AssignedPickerId` (INT, NULL, FK zu AppUsers)
- `AppUsers.IsPickingUser` (BIT, DEFAULT 0)

#### Neue Dateien
- `SQL/38_AddPickerAssignment.sql` — Migration fuer Picker-Zuweisung

## Aenderungen (03.04.2026)

### v1.3.0 — WA→FA Rename, Controller-Split, Menue-Umstrukturierung

#### Verbesserungen
- **Terminologie: Fertigungsauftrag statt Werkstattauftrag**: Gesamte Oberflaeche verwendet einheitlich "Fertigungsauftrag" (FA) statt "Werkstattauftrag" (WA)
- **Controller-Split**: `ProductionOrdersController` aufgeteilt in `PickingController` + `PhotoController`
- **Menue-Umstrukturierung**: Lager als Dropdown, Dashboard nach Domaenen gruppiert

### v1.2.0 — Leitstand: Kommissionier-Freigabe & Priorisierung

#### Neue Funktionen
- **Leitstand-Rolle**: Neue Rolle `leitstand` fuer Produktionsplanung — kann Auftraege freigeben und priorisieren
- **Kommissionier-Freigabe**: Einzel- und Massenfreigabe von Produktionsauftraegen zur Kommissionierung
- **Priorisierung**: Numerische Prioritaet (1 = hoechste), Auto-Vorschlag bei Freigabe, inline aenderbar per AJAX
- **Neue Kommissionierliste**: Tabelle mit freigegebenen Auftraegen (Prio, WA, Artikel, Kunde, Komm.-Termin, Status)
- **Menue-Badge**: Anzahl offener freigegebener Auftraege im Menuepunkt "Kommissionierung"
- **Feature-Toggle**: `LeitstandAktiv` AppSetting (Default: false) — bei Deaktivierung alles wie bisher

#### Neue DB-Spalten
- `ProductionOrders.IsReleasedForPicking` (BIT, DEFAULT 0)
- `ProductionOrders.PickingPriority` (INT, NULL)
- `ProductionOrders.ReleasedAt` (DATETIME2, NULL)
- `ProductionOrders.ReleasedBy` (NVARCHAR(200), NULL)

#### Neue Dateien
- `SQL/37_AddPickingRelease.sql` — Migration
- `Filters/RequireLeitstandAccessAttribute.cs` — Action-Filter
- `Models/ViewModels/PickingListViewModel.cs` — ViewModel
- `Views/ProductionOrders/PickingDropdown.cshtml` — Fallback-View

### v1.1.1 — Bugfixes Bestellungen & Kommissionierung

#### Verbesserungen
- **Kommissionierung: Picking erst beim Umbuchen** — Checkboxen in der Stueckliste sind rein client-seitig. Erst beim Klick auf "Gepickte Artikel umbuchen" werden alle ausgewaehlten Artikel mit Quell-Lagerplatz in einer Transaktion an den Server gesendet
- **Empfaengergruppen: Redirect nach Create auf Edit** — Nach Erstellen direkt zur Bearbeitung, damit sofort Empfaenger hinzugefuegt werden koennen
- **Artikelgruppen-Zuordnung: Spaltenfilter** — filterable-table Klasse ergaenzt
- **Bedarfsmeldungen: Empfaenger vorausgewaehlt** — Alle aktiven Empfaenger der zugeordneten Gruppe werden im Modal automatisch gecheckt

#### Fehlerbehebungen
- Wareneingang: Offene Bedarfsmeldungen werden korrekt angezeigt (Em-Dash → Hyphen im Artikel-Split)
- Bedarfsmeldung: Artikelgruppe wird als reiner Code an API gesendet ("940" statt "940 - Kleinmaterial Allgemein")
- Bedarfsmeldung: ArticleGroup, OrderRecipientGroupId und SentToEmails werden nun korrekt gespeichert
- Footer: Versionsnummer korrekt per Razor-Escaping angezeigt (`v@(...)` statt `v@...`)

### v1.1.0 — Bedarfsmeldungen

#### Neue Funktionen
- **Bedarfsmeldungen aus Stueckliste**: Fehlteile koennen direkt aus der Stueckliste als interne Bedarfsmeldung erfasst werden (Einzel- oder Sammelbestellung)
- **E-Mail-Benachrichtigung**: Bedarfsmeldungen werden automatisch per E-Mail an konfigurierbare Empfaengergruppen versendet (inkl. Prioritaeten: Normal, Dringend, Eilt)
- **Bestelluebersicht**: Neue Seite zur Uebersicht aller Bedarfsmeldungen mit Status-Badges, Prioritaet, Filterung und Pagination
- **Empfaenger-Verwaltung**: Stammdaten fuer Empfaengergruppen mit Empfaenger-Management und Artikelgruppen-Zuordnung (N:M)
- **Wareneingang-Integration**: Offene Bedarfsmeldungen werden bei der Einbuchung angezeigt und koennen mit der Buchung verknuepft werden

#### Neue Tabellen
- `PartRequisitions` — Bedarfsmeldungen (Status, Prioritaet, E-Mail-Tracking, WA-Verknuepfung)
- `OrderRecipientGroups` — Empfaengergruppen
- `OrderRecipients` — Einzelne Empfaenger pro Gruppe
- `ArticleGroupRecipientMappings` — Artikelgruppe-Empfaengergruppe Zuordnung (N:M)

#### Neue Dateien
- `Controllers/PartRequisitionsController.cs` — Bestelluebersicht + Stornierung
- `Controllers/OrderRecipientGroupsController.cs` — CRUD Empfaengergruppen
- `Controllers/Api/PartRequisitionsApiController.cs` — API fuer Bedarfsmeldungen aus Stueckliste
- `Models/PartRequisition.cs`, `OrderRecipientGroup.cs`, `OrderRecipient.cs`, `ArticleGroupRecipientMapping.cs`
- `Data/Repositories/PartRequisitionRepository.cs`, `OrderRecipientGroupRepository.cs`
- `IDEALAKEWMSService/Services/PartRequisitionEmailService.cs`
- `Views/PartRequisitions/Index.cshtml`, `Views/OrderRecipientGroups/` (CRUD)
- `Filters/RequirePickingOrStockAccessAttribute.cs`
- `SQL/36_AddPartRequisitions.sql`

#### Tests
- Unit-Tests fuer PartRequisitionRepository, OrderRecipientGroupRepository
- Unit-Tests fuer PartRequisitionsApiController (Einzel-/Sammelbestellung)
- Unit-Tests fuer RequirePickingOrStockAccessAttribute

## Aenderungen (01.04.2026)

### v1.0.0 — Erstrelease mit Versionierung

#### Neue Funktionen
- **KW-Filter (Fertigungsauftraege)**: ISO 8601 Kalenderwoche in allen 5 Datumsspalten. Kalender-Popup im Spaltenfilter mit KW-Spalte — Klick auf KW oder Tag filtert
- **enaio DMS-Integration**: Entity `EnaioDmsDocument`, Sync-Service im Windows-Service (Delta-Sync aus enaio DB), orange Link-Icons neben FA-Nummern in der Fertigungsauftraege-View. Connection String `EnaioDmsConnection` in appsettings.json
- **Bestandsuebersicht FA-Filter**: Dedizierte Methode `GetStockByProductionOrderAsync()` zeigt Netto-Bestand pro Artikel+Lagerplatz fuer einen Fertigungsauftrag
- **Bestandsuebersicht QR-Scan**: Artikel-QR-Scan-Button und FA-QR-Scan-Button (scanType `productionOrder` extrahiert 3. Position)
- **Responsive Design**: Mobile-first CSS, Touch Targets 44px, Sticky Scrollbar, Navbar-User im Hamburger-Menue, flex-wrap auf Page Headers
- **Versionierung**: `AppVersion.cs` in Web + Service, Version im Footer, Changelog-View unter `/Help/Changelog`

#### Verbesserungen
- **Kommissionierwagen-Filterung**: Wagen nicht in Stuecklisten-Bestand/Dropdowns, keine Meldebestand-Farbcodierung, nicht in BOM `GetStockByArticleNumbersAsync`
- **Spalten optimiert**: FA-Nr + Stk. verkleinert, Stk. ohne Filter, Positions-Spalte in Stueckliste verkleinert
- **iPhone/iPad Hilfe**: Abschnitt auf Help-Seite fuer dauerhaften Kamerazugriff

#### Fehlerbehebungen
- enaio `object1.id` ist `int` nicht `bigint` — `Convert.ToInt64()` statt `GetInt64()`
- enaio MERGE: `CreatedByWindows`/`ModifiedByWindows` fehlten
- enaio Link: kein ungewollter Unterstrich bei mehreren Dokumenten (Whitespace in Razor)

## Aenderungen (30.03.2026)

### Responsive Design
- **CSS-Fix**: `overflow-x: auto` zu `.table-responsive` hinzugefuegt — alle Tabellen haben jetzt horizontale Scrollbars bei Platzmangel

### OSEON Arbeitsgang-Konfiguration
- **Neues Model**: `OseonOperationConfig` — pro AG: Kurzname (z.B. "B", "ST", "BG"), Anzeigename, Soll-Termin-Offset (Arbeitstage relativ zum Stanztermin), OSEON-Relevanz-Flag
- **DB-Migration**: `SQL/34_AddOseonOperationConfig.sql`, EF Migration `AddOseonOperationConfig`
- **Repository**: `IOseonOperationConfigRepository` mit CRUD + Dictionary-Cache + Erkennung nicht-konfigurierter AGs aus OSEON-Daten
- **BusinessDayService erweitert**: Neue `AddBusinessDays(date, days, holidays)` Methode fuer positive/negative Arbeitstage-Berechnung
- **Ampel-Logik pro AG**: Jeder AG bekommt eigene Ampelfarbe basierend auf berechnetem Soll-Termin (Stanztermin + Offset in AT)
- **Status-Relevanz**: Nicht-OSEON-relevante AGs (z.B. "ZB", "A-BT") werden bei der Fertig-Berechnung ignoriert
- **Settings-UI**: Neue Seite `Settings/OperationConfig` — CRUD fuer AG-Konfigurationen, erreichbar ueber Button auf Einstellungen-Seite
- **OseonIndex View**: AG-Zeilen zeigen jetzt Ampel-Punkt, berechneten Soll-Termin, und "nicht relevant"-Badge; nicht-relevante AGs sind ausgegraut/kursiv
- **Default-Daten**: 12 Standard-AGs mit Offsets: B(-1), ST(0), EG(0), BG(+2), BG-SaP1(+2), RO(+2), MS(+4), RS(+4), SL(+5), RE(+5), ZB(0/nicht relevant), A-BT(0/nicht relevant)
- **Tests**: 13 neue Unit-Tests fuer `AddBusinessDays` und `GetColorForOperationAsync`

## Aenderungen (20.03.2026)

### Rollenbasierte Zugriffskontrolle (RBAC)
- **Neue Entities**: `Role` (Key, Name, Description, AdGroup) + `UserRole` Junction-Tabelle (Many-to-Many)
- **Statische Rollen-Keys**: `RoleKeys.cs` mit admin, masterdata, picking, stock, stock_keyuser, tracking, reporting
- **Admin-Wildcard**: Admin-Rolle ueberspringt alle Berechtigungspruefungen
- **AD-Gruppen-Integration**: Jede Rolle kann optional eine AD-Gruppe haben, Mitglieder erhalten die Rolle automatisch (5 Min Cache)
- **Neue Filter**: `[RequireStockAccess]`, `[RequireStockKeyUserAccess]`, `[RequireReportingAccess]`
- **StockMovementsController aufgeteilt**: Basis-Aktionen (Ein/Aus/Umbuchung) mit `[RequireStockAccess]`, Lagerplatz-Operationen (OutboundAll, TransferAll) mit `[RequireStockKeyUserAccess]`
- **CurrentUserService refactored**: Alle Berechtigungsmethoden delegieren an `HasAnyRoleAsync()` mit Rollen-Keys
- **RolesController**: CRUD-Verwaltung fuer Rollen (Name, Beschreibung, AD-Gruppe)
- **UsersController**: Rollen-Zuweisung per Checkbox statt einzelne Boolean-Felder
- **Navbar**: Menueeintraege basieren auf Rollen-Checks
- **Zwei-Phasen-Migration**: Phase 1 = neue Tabellen + Datenmigration (alte Boolean-Spalten bleiben als Fallback), Phase 2 = alte Spalten entfernen (SQL/33)
- **SQL**: `SQL/32_AddRoleTables.sql` (Phase 1), `SQL/33_RemoveOldPermissionColumns.sql` (Phase 2, erst nach Verifikation)
- **Seeding**: Standard-Rollen + admin-User erhaelt admin-Rolle beim Startup
- **Tests**: Unit-Tests fuer CurrentUserService (Rollen-Checks) und Filter-Attribute
- **AppSetting entfernt**: `StammdatenADGruppe` ersetzt durch `Role.AdGroup` auf der masterdata-Rolle

## Änderungen (18.03.2026)

### Delta-Sync mit OSEON-Änderungstimestamps
- **Neue Felder**: `OseonProductionOrders.LastChangedInOseon` (pa.DateOfLastChange), `OseonWorkOperations.LastStatusReportInOseon` (aga.LetzteStatusMeldung)
- **Delta-Logik**: `MAX(LastChangedInOseon)` aus WMS → nur geänderte Datensätze aus OSEON laden (mit 5 Min Sicherheitspuffer)
- **Erster Lauf**: Full-Sync (wenn LastChangedInOseon = NULL)
- **Folge-Läufe**: Nur Delta → dramatisch weniger Daten aus OSEON gelesen
- **SQL**: `SQL/31_AddOseonTimestamps.sql`, `SQL/00_FreshInstall.sql` aktualisiert
- **EF Migration**: `20260318150710_AddOseonTimestamps`

### OSEON Teileverfolgung — UI-Verbesserungen
- **showFinished=true auf OSEON-Link**: Button in FA-Liste (`ProductionOrders/Index`) setzt `asp-route-showFinished="true"`, damit auch abgeschlossene Aufträge sichtbar sind
- **Erst-AG/Letzt-AG Badges entfernt**: `IsFirstOperation`/`IsLastOperation`-Anzeige in `OseonIndex.cshtml` ausgeblendet (wurde falsch dargestellt, nicht benötigt)
- **Hilfe-Seite aktualisiert**: OSEON-Teileverfolgung-Abschnitt beschreibt „Fertige anzeigen" Automatik

### Performance-Optimierung OSEON Sync + Teileverfolgung
- **Bulk MERGE**: `OseonSyncService.SyncOseonProductionOrdersAsync()` komplett auf Bulk-Verarbeitung umgestellt:
  - Statt ~48.000 einzelne SQL-Roundtrips → 2 SqlBulkCopy + 2 MERGE Statements
  - Werkbänke: Bulk-Insert fehlender in einem Statement
  - Orders: DataTable → Temp-Table (`#TmpOseonOrders`) → MERGE (1 Statement für alle Upserts)
  - Operations: DataTable → Temp-Table (`#TmpOseonOps`) → MERGE (1 Statement für alle AGAs)
  - Geschätzte Speedup: ~100x (von Minuten auf Sekunden)
- **TrafficLight Threshold-Cache**: `OseonTrafficLightService` cached `OseonAmpelGelbTage`/`OseonAmpelBlauTage` pro Request (scoped Service). Statt 2 DB-Queries pro Auftrag → 2 DB-Queries pro Seitenaufruf
- **IIS Timeout**: `web.config` → `requestTimeout="00:05:00"` (5 Min statt default 2 Min)
- **IIS App-Pool**: Empfohlene Settings in README dokumentiert (Idle Timeout, AlwaysRunning, Preload)

### Werkbank-Spalte & Sync
- **FA-Liste**: Neue Spalte "Werkbank" in ProductionOrders/Index (filterbar, nach Bezeichnung 2)
- **Werkbank-Sync**: Neuer Sync-Schritt in `OseonSyncService.SyncWorkplacesToProductionOrdersAsync()` — überträgt `ProductionWorkplaceId` von OSEON-Aufträgen auf Sage-Aufträge (Match: `OrderNumber` ↔ `CustomerOrderNumber`)
- **Nur fehlende**: Bereits manuell gesetzte Werkbänke werden nicht überschrieben
- **Automatisch**: Läuft nach OSEON-Tracking-Sync wenn `Sync:OseonTrackingEnabled = true`

### FA-Liste Kompaktierung & Berechtigungen
- **Kompakte Spaltenheader**: Datums-Spalten verkürzt (Beschicht., BG-Termin, Komm., Fert.-Termin), Glas/Zukauf Spalten schmaler
- **Tracking-User Zugriff**: `ProductionOrdersController.Index` erlaubt jetzt auch Tracking-User (neuer Filter `[RequirePickingOrTrackingAccess]`)
- **Read-Only für Tracking-User**: Stückliste-Button, Erledigt-Toggle und Glas/Zukauf-Checkboxen nur für Picking-User sichtbar/aktiv
- **Navbar**: Fertigungsaufträge-Link erscheint auch für Tracking-User (ohne Picking-Berechtigung)

### OSEON Teileverfolgung - Verbesserungen
- **Baumstruktur**: Komplett umgebaut — echte Tree-View mit Ordner-Icons, Dokument-Icons, Uhr-Icons pro Ebene, Einrückung und Chevrons (wie Stückliste)
- **Server-seitige Paginierung**: 25 Gruppen pro Seite, `GetPagedAsync()` im Repository
- **Filter erweitert**: Suche durchsucht jetzt auch `OseonOrderNumber` (nicht nur `CustomerOrderNumber`)
- **Gruppen-Logik**: Bei `showFinished=false` nur Gruppen mit offenen Aufträgen, aber ALLE Sub-Aufträge (inkl. fertige) angezeigt
- **PA-Status**: Aggregierter Status (Badge) auf Gruppenebene (Ebene 0) angezeigt
- **OSEON-Button in FA-Liste**: Neuer Button in ProductionOrders/Index verlinkt zur OSEON-Teileverfolgung mit FA als Filter
- **Erledigt-Button**: In FA-Liste nach rechts verschoben (letzte Spalte)
- **Performance-Indizes**: `SQL/30_OseonPerformanceIndexes.sql` — Indizes auf OseonStatus und WorkplaceName mit INCLUDE
- **Sync-Filter**: OSEON-Query filtert alte fertige Aufträge aus (Status 90/95 älter als 3 Monate)
- **Alle aufklappen/zuklappen**: Buttons in der OSEON-Teileverfolgung (wie Stückliste)
- **Auto-Expand**: Bei nur einer Gruppe wird automatisch aufgeklappt

## Änderungen (17.03.2026)

### OSEON Teileverfolgung - Neues Feature
- **Neue Entities**: `OseonProductionOrder` + `OseonWorkOperation` für OSEON-Produktionsaufträge und Arbeitsgänge
- **3-Ebenen Baumansicht**: KundenAuftragsNr → Subaufträge (OSEON-Nr.) → Arbeitsgänge
- **Ampelsystem**: Rot (überfällig), Gelb (fällig bald), Blau (demnächst), Grün (fertig), Grau (noch nicht relevant)
- **Konfigurierbar**: `OseonAmpelGelbTage`/`OseonAmpelBlauTage` in AppSettings
- **Sync**: `OseonSyncService` im IDEALAKEWMSService liest aus OSEON-DB, auto-erstellt Werkbänke
- **SyncWorker**: Neuer Block `Sync:OseonTrackingEnabled` (default false)
- **Navbar**: Teileverfolgung als Dropdown mit "Rückmeldungen" und "OSEON Aufträge"
- **Filter**: Produktionsauftrag, Werkbank, Fertige anzeigen
- **OSEON Status-Codes**: 10=Unvollständig, 20=Gültig, 30=Freigegeben, 60=In Arbeit, 70=Gesperrt, 90=Fertig, 95=Storniert
- **SQL**: `SQL/29_AddOseonTracking.sql`, `SQL/00_FreshInstall.sql` aktualisiert
- **Tests**: 26 neue Tests (OseonTrafficLightServiceTests, OseonStatusHelperTests, OseonProductionOrderRepositoryTests)

### Simplify-Fixes
- **Bom.cshtml**: `setTimeout(300ms)` durch Bootstrap `hidden.bs.modal` Event ersetzt
- **CurrentUserService**: Per-Request User-Caching hinzugefügt (vermeidet redundante DB-Abfragen)

## BOM-VIEW Struktur (vw_AKE_Kommissionierung_StuecklistenDB)
Die VIEW liegt in der `ake`-Datenbank und liefert:
- **Artikelnummer**: Geräte-Artikelnummer (= WA-Artikel, redundant, nur zur Zuordnung)
- **Position**: Hierarchische Position (z.B. "15", "15.1", "15.1.1") - definiert Baumstruktur
- **Baugruppe**: Artikelnummer der übergeordneten Baugruppe
- **Ressourcenummer**: Die eigentliche Bauteil-Artikelnummer (Kern-Info pro Zeile)
- **Bezeichnung1/2**: Bezeichnung der Ressourcenummer
- **Menge**: Menge in dieser Baugruppe (NICHT Gesamtmenge im Gerät)
- **Beschaffungsartikel**: Sage-Feld "IstBestellartikel", Info für Kommissionierer
- **Artikelgruppe**: Info für Kommissionierer

## Änderungen (16.02.2026)

### Bugfix: StockMovement bei Kommissionierung nicht geschrieben
**Ursache**: `PickingItem.BomArticleNumber` speicherte `bom.Artikelnummer` (Geräte-Artikel) statt `bom.Ressourcenummer` (Bauteil-Artikel). Dadurch konnte `TransferPicked` den Artikel nicht in der Articles-Tabelle finden → kein StockMovement.
**Fix**: Überall `Ressourcenummer` statt `Artikelnummer` für Bauteil-Operationen verwenden.
**Betroffene Dateien**: PickingRepository.cs, ProductionOrdersController.cs, Bom.cshtml
**Hinweis**: Bestehende PickingItems müssen ggf. gelöscht werden (Neu-Initialisierung beim nächsten Öffnen).

### Fertigungsaufträge: Spaltenreihenfolge
- Neue Reihenfolge: FA Nummer, Stückzahl, Kunde, Artikelnummer, Bezeichnung 1, Bezeichnung 2, **Beschichtung**, **Baugruppentermin** (vorher "Vorkommissionierung"), **Kommissionierung**, Fertigungstermin, Liefertermin, Status
- Kommissionierung: hellblau (CI) + fett; bei Überschreitung rot
- **Betroffene Dateien**: Views/ProductionOrders/Index.cshtml, wwwroot/css/site.css

### Kommissionierung: Mehrstufiger klappbarer Baum
- TreeLevel wird aus Position berechnet (Anzahl Punkte = Ebene)
- Baugruppen-Zeilen mit Chevron-Toggle (klick zum Auf-/Zuklappen)
- Default: eingeklappt (nur Top-Level sichtbar)
- Buttons "Alle aufklappen" / "Alle zuklappen" im Filter-Bereich
- Separate Baugruppen-Trennzeilen entfernt; stattdessen Baugruppen-Zeilen mit Folder-Icon + blaue Hintergrundfarbe
- **Betroffene Dateien**: ProductionOrdersController.cs, Bom.cshtml, BomViewModels.cs, site.css

### Kommissionierung: Baugruppen-Picking
- `IsBaugruppe`-Property in BomItemViewModel und PickingItem
- Baugruppe-Checkbox sendet `isBaugruppe=true` an Controller
- Bei Baugruppen-Kommissionierung: nur Bestandsbuchung auf Baugruppe, Teile darunter ignorieren
- **Neue SQL**: 09_PickingItemIsBaugruppe.sql
- **Betroffene Dateien**: PickingItem.cs, PickingRepository.cs, IPickingRepository.cs, ProductionOrdersController.cs, Bom.cshtml

### Arbeitsplätze: Default-Drucker
- Neues Feld `DefaultPrinter` (NVARCHAR 500) am Workstation-Model
- Format: `\\DRUCKSERVER\Druckername`
- Eingabefeld in Create/Edit Views
- Anzeige in Index-Tabelle
- PrintService für Server-seitigen Druck (Grundstruktur)
- **Neue SQL**: 10_WorkstationDefaultPrinter.sql
- **Neue Datei**: Services/PrintService.cs
- **Betroffene Dateien**: Workstation.cs, WorkstationEditViewModel.cs, WorkstationsController.cs, Views/Workstations/*

### Nachbesserungen (Session 2)
- Kommissionierungsdatum in WA-Übersicht: `!important` für CI-Farbe (hellblau) hinzugefügt
- Sortierung Stückliste: `NaturalPositionComparer` für natürliche numerische Sortierung (1, 2, 10, 11 statt 1, 10, 11, 2)
- Druck-Button rechts oben im Kopfbereich der Stückliste (neben Titel)
- **Neue Datei**: Services/NaturalPositionComparer.cs

### Stückliste drucken (PrintBom)
- Header-Druckbutton ("Stückliste drucken") öffnet vollständige Stückliste in druckfreundlichem Format
- Querformat (Landscape), alle BOM-Positionen mit Baumstruktur-Einrückung
- Baugruppen-Zeilen mit hellblauem Hintergrund + fett
- Leere Checkboxen zum manuellen Abhaken beim papiergestützten Kommissionieren
- Spalten: Position, Baugruppe, Ressourcenummer, Bezeichnung 1/2, Menge, Beschaffung, Artikelgruppe
- "Drucken" Button im "Kommissionierung umbuchen" Bereich: druckt NUR gepickte Artikel (bestehende PrintPicking-Logik)
- **Neue Dateien**: Views/ProductionOrders/PrintBom.cshtml
- **Betroffene Dateien**: ProductionOrdersController.cs, BomViewModels.cs, Bom.cshtml

## Änderungen (06.03.2026)

### Feature: Baum standardmäßig eingeklappt + Filter-Bug-Fix (1 + 2.1)
- Neue Funktion `updateBomVisibility()` in `Bom.cshtml`: Baum-State und Spaltenfilter werden kombiniert
- **Baum hat Vorrang**: Zeilen sind nur sichtbar wenn Parent-Baugruppe aufgeklappt UND Filter passt
- Expand/Collapse respektiert aktiven Filter (zuvor wurden alle Kinder angezeigt, auch nicht-passende)
- `window.setColumnFilter` überschrieben damit Default-Filter (Benutzerprofil) den Baum-State respektiert
- **Betroffene Dateien**: `Views/ProductionOrders/Bom.cshtml`

### Feature: Erweiterter Artikelgruppen-Filter (2.2 + 2.3)
- Neue Funktion `matchesFilter()` in `table-filter.js`
- `960,886` → OR-Verknüpfung: zeigt Artikelgruppe 960 ODER 886
- `!960` → Ausschluss: zeigt alles außer Artikelgruppe 960
- Gilt für alle filterbaren Tabellen im System (Bestände, Bewegungshistorie, etc.)
- Rückwärtskompatibel — bestehende Einzel-Filter unverändert
- **Betroffene Dateien**: `wwwroot/js/table-filter.js`

### Feature: Druck mit aktueller Filterung/Baumstruktur (3.1)
- Print-Button liest aktuell sichtbare Zeilen aus dem DOM
- Übergibt `visiblePositions` (kommagetrennte Positionen) als URL-Parameter an `PrintBom`
- Controller filtert Items auf übergebene Positionen
- Aktive Filter werden im Druckdokument-Header angezeigt
- **Betroffene Dateien**: `Bom.cshtml`, `ProductionOrdersController.cs`, `PrintBom.cshtml`, `BomViewModels.cs`

### Feature: Lagerplatz-Umbuchung (4)
- Neuer Menüpunkt "Lagerplatz umbuchen" unter Lagerbewegungen
- Quell-Lagerplatz auswählen (mit Barcode-Scan), Bestandsvorschau-Tabelle, Ziel-Lagerplatz wählen, Bestätigungsdialog
- Alle Artikel mit positivem Bestand werden als `Umbuchung` auf Ziel gebucht
- **Neue Dateien**: `Views/StockMovements/LocationTransfer.cshtml`, `Models/ViewModels/LocationTransferViewModel.cs`
- **Betroffene Dateien**: `StockMovementsController.cs`, `Views/Shared/_Layout.cshtml`

### Feature: Werkbänke / Produktionsarbeitsplätze (5)
- Neue Entität `ProductionWorkplace` mit Bezeichnung, Halle, Abweichende Vorkommissioniertage
- Vollständiges CRUD unter Stammdaten → "Werkbänke" (nur mit Stammdaten-Zugriff, `[RequireMasterDataAccess]`)
- `OverridePrePickingDays` (nullable int) — leer = globaler Standard aus AppSettings
- EF Migration: `20260306081711_AddProductionWorkplaces`
- **Neue Dateien**: `Models/ProductionWorkplace.cs`, Repository + Interface, Controller, Views/ProductionWorkplaces/
- **Neue SQL**: `SQL/22_AddProductionWorkplaces.sql`
- **Betroffene Dateien**: `ApplicationDbContext.cs`, `Program.cs`, `Views/Shared/_Layout.cshtml`

### Tests (06.03.2026)
- `Tests/Repositories/ProductionWorkplaceRepositoryTests.cs` — 7 Tests (CRUD, Sortierung, Nullable-Felder)
- `Tests/Repositories/LocationTransferTests.cs` — 6 Tests (Lagerplatz-Umbuchung Repository-Logik)
- Gesamt: 32 Tests, alle grün

## Änderungen (10.03.2026)

### Feature: BOM-Datenquelle OSEON/TRUMPF Fallback
- `BomRepository` fragt zuerst SAGE-View ab; wenn keine Ergebnisse → Fallback auf Stored Procedure `sp_AKE_Kommissionierung_OseonStuecklistenDB` auf `aketrumpf01.ake.at\TRUMPFSQL2` (DB: `T1000_V01_V001`)
- Rückgabetyp geändert: `IBomRepository.GetBomItemsAsync()` liefert jetzt `BomQueryResult(Items, DataSource)`
- Datenquelle als Badge im BOM-Header: **SAGE** (grau) / **OSEON** (gelb) / **Keine Daten gefunden** (rot)
- Neuer Connection String `OseonConnection` in `appsettings.json`
- **Betroffene Dateien**: `BomRepository.cs`, `CachedBomRepository.cs`, `IBomRepository.cs`, `BomViewModels.cs`, `ProductionOrdersController.cs`, `Bom.cshtml`, `appsettings.json`

### Feature: SQL Agent Job — Produktionsaufträge auch aktualisieren
- `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` auf `MERGE`-Statement umgestellt
- Neue Aufträge: `WHEN NOT MATCHED → INSERT`
- Geänderte Aufträge: `WHEN MATCHED AND (Änderungscheck) → UPDATE` (nur bei tatsächlicher Änderung)
- App-verwaltete Felder werden nicht überschrieben: `IsDone`, `PickingStatus`, `HasGlass`, `HasExternalPurchase`
- Bei Update werden Audit-Felder gesetzt: `ModifiedAt`, `ModifiedBy`, `ModifiedByWindows`

### Feature: Rekursive Suche bei aktiver Filterung (User-Setting)
- Neues Bool-Feld `RecursiveFilterSearch` am User-Model
- Einstellbar im Benutzerprofil (Checkbox unter den Standard-BOM-Filter-Feldern)
- Wenn aktiv + Filter gesetzt: `updateBomVisibility()` ignoriert den Baum-State → alle passenden Positionen werden angezeigt, unabhängig davon ob die übergeordnete Baugruppe aufgeklappt ist
- Wenn kein Filter aktiv: normales Baum-Verhalten unverändert
- EF Migration: `20260310130059_AddRecursiveFilterSearch`
- **Neue SQL**: `SQL/23_AddRecursiveFilterSearch.sql`
- **Betroffene Dateien**: `User.cs`, `ProfileViewModel.cs`, `AccountController.cs`, `Profile.cshtml`, `BomViewModels.cs`, `ProductionOrdersController.cs`, `Bom.cshtml`

## Änderungen (10.03.2026) — Session 2

### Neues Projekt: IDEALAKEWMSService (Windows Service) — Infrastruktur
- Neues Worker-Service-Projekt zur Solution hinzugefügt (`IDEALAKEWMSService.csproj`, SDK: `Microsoft.NET.Sdk.Worker`)
- Läuft als Windows Service (`UseWindowsService()`, Service-Name: "IDEAL AKE WMS Service")
- Infrastruktur vorbereitet: DI, Serilog (File + Console), ConnectionStrings (DefaultConnection + OseonConnection)
- Zwei Placeholder-Worker: `SyncWorker` (Schnittstellenabgleich) + `NotificationWorker` (Mail-Notifications)
- Konfiguration: `appsettings.json` mit `MailSettings`, `WorkerSettings` (Intervalle), Serilog

## Änderungen (11.03.2026)

### Feature: IDEALAKEWMSService Vollausbau
- **User**: `Email`, `IsAdmin`, `NotifyOnReorderLevel` Felder ergänzt
- **Neue Berechtigung**: `IsAdmin`-Flag — Zugriff auf Service-Einstellungen; `[RequireAdminAccess]`-Filter
- **Neue Tabelle**: `ServiceSettings` — laufzeitveränderliche Konfiguration (Admin-only CRUD in Stammdaten)
- **ServiceSettings Seed**: 6 Standard-Einträge (Notifications:MeldebestandEnabled, Recipients, AppBaseUrl, Subject; Sync:ProductionOrdersEnabled/ArticlesEnabled)
- **Profil**: Email + NotifyOnReorderLevel selbst verwaltbar; User/Edit+Create: Email, IsAdmin, NotifyOnReorderLevel
- **Navigation**: "Service-Einstellungen" unter Stammdaten (nur für Admins sichtbar)
- **SyncWorker**: Produktionsaufträge + Artikel aus SAGE importieren (ersetzt SQL Agent Jobs); DryRun-Modus konfigurierbar
- **NotificationWorker**: Meldebestand-Prüfung; HTML5-Mail im AKE CI (Dunkelblau/Hellblau)
- **Empfänger**: ServiceSettings `Notifications:Recipients` (fix) + alle User mit `NotifyOnReorderLevel=true` und Email
- **Logging**: Separate Unterordner `logs/sync/` und `logs/notifications/` mit 30-Tage-Retention via Serilog.Expressions
- **SageConnection**: Neuer ConnectionString in `IDEALAKEWMSService/appsettings.json` → `Server=AKESQL20.ake.at;Database=ake`
- EF Migrations: `AddUserEmailIsAdminNotify`, `AddServiceSettings`
- SQL Scripts: `24_AddUserEmailIsAdminNotify.sql`, `25_AddServiceSettings.sql`
- **Neue Dateien (Service)**: `Services/ISageImportService.cs`, `SageImportService.cs`, `IStockCheckService.cs`, `StockCheckService.cs`, `IMailService.cs`, `MailService.cs`
- **Neue Dateien (Web-App)**: `Models/ServiceSetting.cs`, `Filters/RequireAdminAccessAttribute.cs`, `Data/Repositories/IServiceSettingRepository.cs`, `ServiceSettingRepository.cs`, `Controllers/ServiceSettingsController.cs`, `Views/ServiceSettings/*`

## Änderungen (16.03.2026)

### Feature: Artikelgruppe in Stammdaten anzeigen
- `ArticleGroup`-Spalte wird jetzt in der Artikel-Übersicht, Artikel-Info und Artikel-Edit (readonly) angezeigt
- SQL-Script `26_AddArticleGroup.sql` korrigiert (Migrations-History INSERT statt PRINT)
- **Betroffene Dateien**: `Views/Articles/Index.cshtml`, `Views/Articles/Edit.cshtml`, `Views/Articles/Info.cshtml`, `ArticleInfoViewModel.cs`, `ArticlesController.cs`

### Feature: QR-Code Scan in Kommissionierliste
- Neuer Scan-Button in der Stücklisten-Ansicht zum Scannen von Artikel-QR-Codes
- Gescannter Artikel wird automatisch als "kommissioniert" markiert (Checkbox gesetzt)
- Zeile wird kurz gelb hervorgehoben und in den sichtbaren Bereich gescrollt
- Nicht-gefundene Artikel: Warnung als Bootstrap-Alert (5s auto-dismiss)
- **Betroffene Dateien**: `Views/ProductionOrders/Bom.cshtml`

### Feature: Arbeitsgänge & Teileverfolgung — Phase 1 (Datenmodell)
- **User-Berechtigungen**: 3 neue Flags `CanPick`, `CanViewTracking`, `CanReportOperations` auf User-Model
- **Werkbank-Benutzer-Zuordnung**: Neue M:M Join-Tabelle `ProductionWorkplaceUsers` (Pattern von WorkstationUser)
- **WA-Werkbank-Zuordnung**: `ProductionWorkplaceId` (nullable FK) auf ProductionOrder
- **WorkOperation-Tabelle**: Neues Model für Arbeitsgänge (OperationNumber, Name, Sequence, Reporting-Felder, ExternalSource)
- **AppSettings**: 3 neue Einstellungen: `TeileverfolgungAktiv`, `OseonRueckmeldungAktiv`, `SageRueckmeldungAktiv`
- **Views**: Benutzer-Edit/Create mit Funktionsberechtigungen-Checkboxen; Werkbank-Edit/Create mit Multi-Select Benutzer-Zuordnung; Werkbank-Index mit Benutzer-Spalte
- **Repository**: `ProductionWorkplaceRepository` erweitert (WithUsers-Methoden, SetUsers); neues `WorkOperationRepository`
- EF Migration: `20260316062006_AddWorkOperationsPhase1`
- **Neue SQL**: `SQL/27_AddWorkOperationsPhase1.sql`
- **Neue Dateien**: `Models/ProductionWorkplaceUser.cs`, `Models/WorkOperation.cs`, `Data/Repositories/IWorkOperationRepository.cs`, `Data/Repositories/WorkOperationRepository.cs`
- **Betroffene Dateien**: `User.cs`, `ProductionWorkplace.cs`, `ProductionOrder.cs`, `ApplicationDbContext.cs`, `Program.cs`, `ProductionWorkplaceRepository.cs`, `IProductionWorkplaceRepository.cs`, `ProductionWorkplaceEditViewModel.cs`, `UsersController.cs`, `ProductionWorkplacesController.cs`, `Views/Users/*`, `Views/ProductionWorkplaces/*`
- **Tests**: 10 neue Tests (5x ProductionWorkplaceUser, 5x WorkOperationRepository)

### Feature: Teileverfolgung — Phase 2 (UI-Masken)
- **Neue Ansicht "Teileverfolgung"**: Eigener Menüpunkt in Navbar (sichtbar nur wenn `TeileverfolgungAktiv = true` + User hat `CanViewTracking`)
- **Auftragsübersicht** (`Tracking/Index`): Aufträge mit aufklappbaren Arbeitsgängen, Filter nach FA-Nummer/Werkbank/Status, Fortschritts-Badge (x/y)
- **Werkbank-Ansicht** (`Tracking/ByWorkplace`): Alle Arbeitsgänge einer Werkbank, sortiert nach Auftrag + Reihenfolge
- **Rückmeldung**: POST-Actions `Report`/`UndoReport` — nur für User mit `CanReportOperations`
- **Berechtigungen**: `[RequireTrackingAccess]` Filter-Attribute (Pattern wie RequireMasterDataAccess)
- **ICurrentUserService**: Neue Methoden `CanViewTrackingAsync()`, `CanReportOperationsAsync()`
- **Repository**: 3 neue Methoden (`GetAllWithOrderAndWorkplaceAsync`, `GetByWorkplaceIdAsync`, `GetOpenByWorkplaceIdAsync`)
- **Neue Dateien**: `Filters/RequireTrackingAccessAttribute.cs`, `Controllers/TrackingController.cs`, `Models/ViewModels/TrackingViewModel.cs`, `Views/Tracking/Index.cshtml`, `Views/Tracking/ByWorkplace.cshtml`
- **Betroffene Dateien**: `ICurrentUserService.cs`, `CurrentUserService.cs`, `IWorkOperationRepository.cs`, `WorkOperationRepository.cs`, `Views/Shared/_Layout.cshtml`
- **Tests**: 6 neue Tests (WorkOperationRepositoryExtendedTests)

### Erledigt: Teileverfolgung — Phase 3 (OSEON Import + Anzeige)
- **Oseon-Aufträge**: `OseonProductionOrder` + `OseonWorkOperation` Entities, Sync via `OseonSyncService` (IDEALAKEWMSService)
- **3-Ebenen Baumansicht**: Tree-View mit Ordner/Dokument/Uhr-Icons, Pagination (25/Seite), Ampelsystem
- **FA-Link**: OSEON-Teileverfolgung-Button in Fertigungsaufträge-Liste
- **Details**: Siehe Änderungen 17.03 + 18.03.2026

### Zukünftige Funktionen (geplant, noch nicht implementiert)
- Meldebestand-Mail: Aufsplitten nach Artikelgruppe oder Lagerhalle
- Lagerplätze in SAGE anlegen wenn neue in WMS erstellt
- Bestandsbuchung per SQL in SAGE DB
- XML für Bestandsbuchung im OSEON
- Synchronisierung Artikelzusatzinfos (Einheiten)

## Änderungen (17.03.2026)

### Feature: Berechtigungsbasiertes Dashboard + Menü
- `CanPick`-Berechtigung steuert Sichtbarkeit von Lagerbewegungen, Bestände, Fertigungsaufträge, Kommissionierung (Menü + Dashboard + Controller-Filter `[RequirePickingAccess]`)
- Teileverfolgung als Dashboard-Kachel hinzugefügt (sichtbar wenn `TeileverfolgungAktiv + CanViewTracking`)
- Stammdaten-Kacheln (Benutzer, Arbeitsplätze) nur bei `HasMasterDataAccess`
- **Neue Datei**: `Filters/RequirePickingAccessAttribute.cs`
- **Betroffene Dateien**: `HomeController.cs`, `Views/Home/Index.cshtml`, `Views/Shared/_Layout.cshtml`, `ICurrentUserService.cs`, `CurrentUserService.cs`, `StockMovementsController.cs`, `StockOverviewController.cs`, `ProductionOrdersController.cs`, `ProductionOrdersApiController.cs`

### Feature: QR-Code mit FA-Nummer
- AppSetting `QrMitFaNummer` (SQL/28) — wenn aktiv, wird FA-Nummer aus QR extrahiert
- QR-Format: `Artikelnummer;Feld2;FA-Nummer[,Suffix]` — Komma-Suffix wird abgeschnitten (`.split(',')[0]`)
- FA-Feld wird bei jedem Scan zuerst geleert (verhindert alte FA bei neuem Scan)
- Angewendet auf: Inbound, Outbound, Transfer
- **Betroffene Dateien**: `wwwroot/js/barcode-scanner.js`, `Views/StockMovements/Inbound.cshtml`, `Outbound.cshtml`, `Transfer.cshtml`

### Feature: Boolean-Settings als Toggle-Switches
- Settings-Seite rendert `true`/`false`-Werte als Bootstrap form-switch
- Fix: Checkbox ohne `name`-Attribut, Hidden-Input wird per JS gesteuert (Dictionary-Binding-Problem gelöst)
- **Betroffene Dateien**: `Views/Settings/Index.cshtml`, `Controllers/SettingsController.cs`

### Feature: Mobile WA — BOM-Button erste Spalte
- Stückliste/ToggleDone-Buttons als erste Spalte platziert (bessere Mobile-Erreichbarkeit)
- **Betroffene Dateien**: `Views/ProductionOrders/Index.cshtml`

### Bugfix: Kommissionierliste Artikel-Scan
- Hidden-Input-Bug gefixt: `processScannedValue` übersprung `type="hidden"` Inputs
- Nicht gefundener Artikel: Bootstrap-Modal statt `confirm()` (verhindert Endlosschleife durch sofortiges Kamera-Re-Read)
- **Betroffene Dateien**: `wwwroot/js/barcode-scanner.js`, `Views/ProductionOrders/Bom.cshtml`

### Bugfix: SQL/00_FreshInstall.sql konsolidiert
- Aktualisiert auf Migration 28 (war auf Stand 19)
- MigrationId-Bug in Scripts 24/25 behoben (falsche IDs → EF versuchte Migrations erneut)

## Offene Aufgaben / Nächste Schritte
- [ ] Druck-Integration testen (PrintService mit echtem Drucker)
- [ ] Druck-Button in Kommissionierung mit Arbeitsplatz-Drucker verknüpfen
- [ ] `OverridePrePickingDays` aus Werkbank in Terminberechnung (BusinessDayService) einbeziehen
- [ ] Tests für neue Features (CanPickAsync, QR-Parsing)

## DB-Migrationen (in Reihenfolge ausführen)
- `SQL/09_PickingItemIsBaugruppe.sql` - IsBaugruppe-Flag für PickingItems
- `SQL/10_WorkstationDefaultPrinter.sql` - DefaultPrinter für Workstations
- `SQL/22_AddProductionWorkplaces.sql` - Tabelle ProductionWorkplaces (Werkbänke)
- `SQL/23_AddRecursiveFilterSearch.sql` - User-Setting: Rekursive Suche in Stückliste
- `SQL/24_AddUserEmailIsAdminNotify.sql` - User: Email, IsAdmin, NotifyOnReorderLevel
- `SQL/25_AddServiceSettings.sql` - Tabelle ServiceSettings + Standard-Einträge
- `SQL/26_AddArticleGroup.sql` - Artikelgruppe zu Articles hinzufügen
- `SQL/27_AddWorkOperationsPhase1.sql` - Arbeitsgänge Phase 1 (User-Flags, ProductionWorkplaceUsers, WorkOperations, AppSettings)
- `SQL/28_AddQrMitFaNummer.sql` - AppSetting QrMitFaNummer
- `SQL/29_AddOseonTracking.sql` - OSEON Teileverfolgung Tabellen + AppSettings
- `SQL/30_OseonPerformanceIndexes.sql` - Performance-Indizes für OSEON-Tabellen
- `SQL/31_AddOseonTimestamps.sql` - Delta-Sync Timestamps für OSEON-Tabellen

## Wichtige Dateien
- `Program.cs` - App-Konfiguration, Middleware, DI
- `Controllers/ProductionOrdersController.cs` - Hauptlogik WA + Kommissionierung
- `Controllers/StockMovementsController.cs` - Lagerbewegungen + Lagerplatz-Umbuchung
- `Controllers/TrackingController.cs` - Teileverfolgung (Rückmeldungen + OSEON)
- `Controllers/ProductionWorkplacesController.cs` - Werkbank CRUD
- `Data/Repositories/PickingRepository.cs` - Picking-Datenzugriff
- `Data/Repositories/BomRepository.cs` - BOM-Abfrage: SAGE-View → Fallback OSEON-SP
- `Data/Repositories/StockMovementRepository.cs` - Bestandsberechnung
- `Data/Repositories/ProductionWorkplaceRepository.cs` - Werkbank CRUD
- `Data/Repositories/OseonProductionOrderRepository.cs` - OSEON-Aufträge mit GetPagedAsync()
- `Filters/RequirePickingOrTrackingAccessAttribute.cs` - Kombinierte Picking/Tracking-Berechtigung
- `Services/PrintService.cs` - Server-seitiger Druck
- `Services/OseonTrafficLightService.cs` - OSEON Ampelberechnung
- `IDEALAKEWMSService/Services/OseonSyncService.cs` - OSEON-Daten-Sync
- `Views/ProductionOrders/Index.cshtml` - FA-Liste (Stückliste, OSEON-Link, Erledigt)
- `Views/ProductionOrders/Bom.cshtml` - Stücklisten-View mit Picking + Baum + kombiniertem Filter
- `Views/ProductionOrders/PrintBom.cshtml` - Druck-View: vollständige Stückliste (mit Filterübertragung)
- `Views/ProductionOrders/PrintPicking.cshtml` - Druck-View: nur gepickte Artikel
- `Views/Tracking/OseonIndex.cshtml` - OSEON 3-Ebenen Tree-View mit Pagination
- `Views/StockMovements/LocationTransfer.cshtml` - Lagerplatz-Umbuchung View
- `Views/ProductionWorkplaces/` - Werkbank CRUD Views
- `wwwroot/js/table-filter.js` - Spaltenfilter mit Multi-Wert/Ausschluss-Logik
- `wwwroot/css/site.css` - Corporate Design Styles + OSEON Tree-Styles
- `SQL/` - 31 DB-Init-/Migrationsskripte

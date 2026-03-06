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
| Werkstattaufträge | Fertig |
| Kommissionierung (BOM/Stückliste) | Fertig |
| BOM-Filter (Multi-Wert, Ausschluss) | Fertig |
| BOM-Druck mit Filterübertragung | Fertig |
| Barcode/QR-Scanner | Fertig |
| Stammdaten (Artikel, Lagerplätze, Benutzer, Arbeitsplätze) | Fertig |
| Werkbänke / Produktionsarbeitsplätze | Fertig |
| Feiertag-Import | Fertig |
| Foto-Upload bei Kommissionierung | Fertig |
| Server-seitiger Druck | Grundstruktur |

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

### Werkstattaufträge: Spaltenreihenfolge
- Neue Reihenfolge: WA Nummer, Stückzahl, Kunde, Artikelnummer, Bezeichnung 1, Bezeichnung 2, **Beschichtung**, **Baugruppentermin** (vorher "Vorkommissionierung"), **Kommissionierung**, Fertigungstermin, Liefertermin, Status
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

## Offene Aufgaben / Nächste Schritte
- [ ] Druck-Integration testen (PrintService mit echtem Drucker)
- [ ] Druck-Button in Kommissionierung mit Arbeitsplatz-Drucker verknüpfen
- [ ] `OverridePrePickingDays` aus Werkbank in Terminberechnung (BusinessDayService) einbeziehen
- [ ] 00_FreshInstall.sql um Tabelle ProductionWorkplaces ergänzen

## DB-Migrationen (in Reihenfolge ausführen)
- `SQL/09_PickingItemIsBaugruppe.sql` - IsBaugruppe-Flag für PickingItems
- `SQL/10_WorkstationDefaultPrinter.sql` - DefaultPrinter für Workstations
- `SQL/22_AddProductionWorkplaces.sql` - Tabelle ProductionWorkplaces (Werkbänke)

## Wichtige Dateien
- `Program.cs` - App-Konfiguration, Middleware, DI
- `Controllers/ProductionOrdersController.cs` - Hauptlogik WA + Kommissionierung
- `Controllers/StockMovementsController.cs` - Lagerbewegungen + Lagerplatz-Umbuchung
- `Controllers/ProductionWorkplacesController.cs` - Werkbank CRUD
- `Data/Repositories/PickingRepository.cs` - Picking-Datenzugriff
- `Data/Repositories/BomRepository.cs` - BOM-VIEW Abfrage
- `Data/Repositories/StockMovementRepository.cs` - Bestandsberechnung
- `Data/Repositories/ProductionWorkplaceRepository.cs` - Werkbank CRUD
- `Services/PrintService.cs` - Server-seitiger Druck
- `Views/ProductionOrders/Bom.cshtml` - Stücklisten-View mit Picking + Baum + kombiniertem Filter
- `Views/ProductionOrders/PrintBom.cshtml` - Druck-View: vollständige Stückliste (mit Filterübertragung)
- `Views/ProductionOrders/PrintPicking.cshtml` - Druck-View: nur gepickte Artikel
- `Views/StockMovements/LocationTransfer.cshtml` - Lagerplatz-Umbuchung View
- `Views/ProductionWorkplaces/` - Werkbank CRUD Views
- `wwwroot/js/table-filter.js` - Spaltenfilter mit Multi-Wert/Ausschluss-Logik
- `wwwroot/css/site.css` - Corporate Design Styles
- `SQL/` - 22 DB-Init-/Migrationsskripte

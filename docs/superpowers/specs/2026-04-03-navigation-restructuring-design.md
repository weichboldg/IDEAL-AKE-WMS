# Navigation & Controller Umstrukturierung

## Zusammenfassung

Die App-Struktur wird nach Domänen reorganisiert: Lager, Produktionsaufträge, Kommissionierung, Bestellungen, Teileverfolgung, Stammdaten. Der überladene `ProductionOrdersController` (15 Actions, 3 Domänen) wird in `ProductionOrdersController`, `PickingController` und `PhotoController` aufgeteilt. Views und Menü folgen der neuen Struktur.

## Motivation

Der `ProductionOrdersController` ist ein God Object: 15 Actions, 18 Dependencies, mischt Produktionsplanung (WA-Liste, Leitstand), Kommissionierung (Picking, Bom, Transfer) und Medien (Fotos). Das Menü gruppiert nicht nach Domänen — Kommissionierer und Leitstand teilen sich dieselben Navigationspfade. Die Umstrukturierung schafft klare Zuständigkeiten und ermöglicht zukünftige Erweiterungen (BDE/Betriebsdatenerfassung als eigenen Bereich).

## Keine Datenbankänderungen

Diese Umstrukturierung ist rein Code/UI/Routing. Alle DB-Tabellen, Spalten und Indizes bleiben unverändert. Keine SQL-Migration nötig.

## Neue Menüstruktur

```
LAGER (Dropdown)                          canAccessStock || canPick
  Einbuchung
  Ausbuchung
  Umbuchung
  Lagerplatz ausbuchen                    + CanTransferStock
  Lagerplatz umbuchen                     + CanTransferStock
  ---
  Bestände
  Bewegungshistorie

PRODUKTIONSAUFTRÄGE (Link)                Bedingung abhängig von LeitstandAktiv
  → wenn leitstandAktiv: canManagePickingRelease || canViewTracking
  → sonst:               canPick || canViewTracking

KOMMISSIONIERUNG (Link/Dropdown)          canPick
  → wenn leitstandAktiv: Dropdown mit "Kommissionierliste" + Badge
  → sonst:               einzelner Link

BESTELLUNGEN (Link)                       bestellungenAktiv && (canPick || canAccessStock)

TEILEVERFOLGUNG (Dropdown)                teileverfolgungAktiv && canViewTracking
  Rückmeldungen
  OSEON Aufträge

STAMMDATEN (Dropdown)                     wie bisher
  ...
```

## Controller-Split

### Vorher: ProductionOrdersController (15 Actions)

### Nachher: 3 Controller

#### `ProductionOrdersController` — Produktionsaufträge / WA-Liste

| Action | HTTP | Beschreibung |
|--------|------|-------------|
| `Index` | GET | WA-Liste mit Filtern, Terminen, Freigabe-Spalte |
| `ToggleRelease` | POST | Einzelfreigabe |
| `BulkRelease` | POST | Massenfreigabe |
| `SetPriority` | POST | Priorität setzen (AJAX) |
| `ToggleDone` | POST | Erledigt-Toggle |

Access: Manuelle Prüfung (CanPick OR CanViewTracking OR CanManagePickingRelease), Leitstand-Actions: `[RequireLeitstandAccess]`

#### `PickingController` — Kommissionierung

| Action | HTTP | Beschreibung |
|--------|------|-------------|
| `Index` | GET | Kommissionierliste oder Dropdown-Fallback |
| `Bom` | GET | Stückliste mit Picking-Checkboxen |
| `TransferPicked` | POST | Umbuchen (AJAX) |
| `SetPickingStatus` | POST | Status setzen (AJAX) |
| `PrintBom` | GET | Stückliste drucken |
| `PrintPicking` | GET | Kommissionierliste drucken |

Access: `[RequirePickingAccess]`

#### `PhotoController` — Fotos (API)

| Action | HTTP | Beschreibung |
|--------|------|-------------|
| `Upload` | POST | Foto hochladen |
| `Get` | GET | Fotos abrufen |
| `Delete` | POST | Foto löschen |

Access: `[RequirePickingAccess]`, Route: `api/photos`

#### `ProductionOrdersApiController` — bleibt unverändert

| Action | HTTP | Beschreibung |
|--------|------|-------------|
| `Search` | GET | Select2-Suche |
| `ToggleField` | POST | Glas/Zukauf Toggle |

### `PartRequisitionsApiController` — bleibt unverändert

## View-Ordner-Struktur

### Vorher
```
Views/ProductionOrders/
  Index.cshtml, Picking.cshtml, PickingDropdown.cshtml,
  Bom.cshtml, PrintBom.cshtml, PrintPicking.cshtml
```

### Nachher
```
Views/ProductionOrders/
  Index.cshtml                (WA-Liste)

Views/Picking/
  Index.cshtml                (Kommissionierliste — war Picking.cshtml)
  IndexDropdown.cshtml        (Fallback — war PickingDropdown.cshtml)
  Bom.cshtml                  (Stückliste — verschoben)
  PrintBom.cshtml             (Druck — verschoben)
  PrintPicking.cshtml         (Druck — verschoben)
```

## URL-Änderungen

| Vorher | Nachher |
|--------|---------|
| `/ProductionOrders/Picking` | `/Picking` |
| `/ProductionOrders/Bom/123` | `/Picking/Bom/123` |
| `/ProductionOrders/TransferPicked` | `/Picking/TransferPicked` |
| `/ProductionOrders/SetPickingStatus` | `/Picking/SetPickingStatus` |
| `/ProductionOrders/PrintBom/123` | `/Picking/PrintBom/123` |
| `/ProductionOrders/UploadPhoto` | `/api/photos/upload` |
| `/ProductionOrders/GetPhotos/123` | `/api/photos/123` |
| `/ProductionOrders/Index` | `/ProductionOrders/Index` (unverändert) |

Keine Redirects von alten URLs — sauberer Schnitt.

## Referenz-Updates

Alle Stellen die auf die alten Controller/Actions verweisen müssen aktualisiert werden:

- **Views**: `asp-controller="ProductionOrders"` → `asp-controller="Picking"` wo relevant
- **JavaScript**: `Url.Action("Bom", "ProductionOrders")` → `Url.Action("Bom", "Picking")`
- **Layout.cshtml**: Menü-Links
- **Index.cshtml** (WA-Liste): Link zur Stückliste zeigt jetzt auf Picking/Bom
- **Bom.cshtml**: Alle internen Links (TransferPicked, SetPickingStatus, etc.)
- **_Select2ProductionOrderPartial.cshtml**: URL für Select2-Redirect nach Picking/Bom

## Nicht im Scope

- DB-Änderungen
- Neue Features
- BDE/Betriebsdatenerfassung (nur Menü-Platz vorgesehen)
- Refactoring von anderen Controllern (StockMovements, Tracking, etc.)
- Test-Änderungen (keine Controller-Tests vorhanden die brechen könnten)

## Risiken

- **Viele Dateien betroffen**: ~10 Views mit internen Links die aktualisiert werden müssen
- **Bom.cshtml ist groß**: ~1000 Zeilen, viele interne JS-Referenzen auf Actions
- **Select2-Partial**: Leitet nach WA-Auswahl direkt auf Bom weiter — URL muss angepasst werden
- **Kein automatischer Test-Schutz**: Keine Controller-Tests die Routing-Brüche erkennen

## Testszenarien

| Test | Erwartung |
|------|-----------|
| /Picking → Kommissionierliste | Tabelle (wenn LeitstandAktiv) oder Dropdown |
| /Picking/Bom/123 → Stückliste | Stückliste öffnet, Picking funktioniert |
| /Picking/TransferPicked → Umbuchen | AJAX-Umbuchen funktioniert |
| /ProductionOrders/Index → WA-Liste | WA-Liste mit Freigabe-Spalte |
| Menü-Links | Alle verweisen auf korrekte Controller |
| Foto-Upload in Stückliste | Fotos hochladen/anzeigen/löschen funktioniert |
| Select2-WA-Suche | Wählt WA → öffnet /Picking/Bom/{id} |

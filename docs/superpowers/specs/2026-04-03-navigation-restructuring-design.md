# Navigation, Controller & Terminologie Umstrukturierung

## Zusammenfassung

Drei zusammenhängende Verbesserungen:

1. **Controller-Split**: `ProductionOrdersController` (15 Actions) → `ProductionOrdersController` + `PickingController` + `PhotoController`
2. **Menü-Umstrukturierung**: Klare Domänen-Trennung (Lager, Fertigungsaufträge, Kommissionierung, Bestellungen, Teileverfolgung, Stammdaten) inkl. Dashboard
3. **Terminologie-Rename**: "Werkstattauftrag/WA" → "Fertigungsauftrag/FA" durchgängig in UI, Docs, Labels

## Motivation

- `ProductionOrdersController` mischt 3 Domänen (Produktionsplanung, Kommissionierung, Fotos)
- Menü gruppiert nicht nach Domänen
- Dashboard zeigt nicht die neue Struktur
- Terminologie "Werkstattauftrag" ist intern nicht mehr gebräuchlich — "Fertigungsauftrag" ist der korrekte Term

## Keine Datenbankänderungen

Rein Code/UI/Routing/Docs. DB-Tabellen (`ProductionOrders`), Spalten (`OrderNumber`) und Property-Namen (`OrderNumber`) bleiben unverändert. Nur Display-Attribute und UI-Labels ändern sich.

---

## 1. Terminologie-Rename

### Globale Ersetzung

| Vorher | Nachher | Kontext |
|--------|---------|---------|
| Werkstattauftrag | Fertigungsauftrag | Singular, Prosa |
| Werkstattaufträge | Fertigungsaufträge | Plural, Überschriften |
| WA Nummer | FA Nummer | Labels, Filter |
| WA Nr. | FA Nr. | Spaltenköpfe |
| WA-Nr. | FA-Nr. | Kurzform mit Bindestrich |
| WA-Nummer | FA-Nummer | Labels in Tracking |
| WA-Liste | FA-Liste | Docs/Kommentare |
| WA @Model.OrderNumber | FA @Model.OrderNumber | Seitentitel (Bom, PrintBom) |
| z.B. WA-001 | z.B. FA-001 | Platzhalter |

### Model Display Attribute

`ProductionOrder.cs` Zeile 9:
```
[Display(Name = "WA Nummer")] → [Display(Name = "FA Nummer")]
```

### Betroffene Dateien (~40 Stellen)

**Views** (12 Dateien): Index.cshtml, Picking.cshtml, Bom.cshtml, PrintBom.cshtml, PrintPicking.cshtml, PickingDropdown.cshtml, Home/Index.cshtml, Help/Index.cshtml, Help/Changelog.cshtml, Tracking/Index.cshtml, Tracking/ByWorkplace.cshtml, PartRequisitions/Index.cshtml, StockOverview/Index.cshtml, StockMovements/Inbound.cshtml, StockMovements/OutboundAll.cshtml, StorageLocations/Create+Edit.cshtml, _Layout.cshtml, _Select2ProductionOrderPartial.cshtml

**Docs** (4 Dateien): CLAUDE.md, README.md, PROJECT_STATUS.md, ANALYSIS.md

### Was NICHT umbenannt wird

- C#-Property-Namen (`OrderNumber`, `ProductionOrder`, etc.)
- DB-Spalten und Tabellen
- URL-Routen (`/ProductionOrders/Index` bleibt)
- CSS-Klassen (`.wa-number` — intern)
- Variable-Namen in JavaScript

---

## 2. Menüstruktur

### Neue Navigation

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

FERTIGUNGSAUFTRÄGE (Link)                 Bedingung abhängig von LeitstandAktiv
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

### Dashboard (Home/Index.cshtml)

Das Dashboard wird nach denselben Domänen gruppiert. Aktuell zeigt es 9 Karten in loser Reihenfolge.

**Neue Gruppierung:**

```
LAGER                                     canAccessStock || canPick
  [Einbuchung]  [Ausbuchung]  [Bestände]  [Bewegungshistorie]

FERTIGUNGSAUFTRÄGE                        canPick || canViewTracking || canManagePickingRelease
  [Fertigungsaufträge]

KOMMISSIONIERUNG                          canPick
  [Kommissionierung]

TEILEVERFOLGUNG                           teileverfolgungAktiv && canViewTracking
  [Teileverfolgung]

STAMMDATEN                                HasMasterDataAccess || immer (Artikel, Lagerplätze)
  [Artikel]  [Lagerplätze]  [Benutzer]  [Arbeitsplätze]
```

Jede Gruppe bekommt eine kleine Überschrift (`<h5>` oder `<h6>` mit Trennlinie). Karten innerhalb der Gruppe bleiben als Cards.

---

## 3. Controller-Split

### Vorher: ProductionOrdersController (15 Actions)

### Nachher: 3 Controller

#### `ProductionOrdersController` — Fertigungsaufträge

| Action | HTTP | Beschreibung |
|--------|------|-------------|
| `Index` | GET | FA-Liste mit Filtern, Terminen, Freigabe-Spalte |
| `ToggleRelease` | POST | Einzelfreigabe |
| `BulkRelease` | POST | Massenfreigabe |
| `SetPriority` | POST | Priorität setzen (AJAX) |
| `ToggleDone` | POST | Erledigt-Toggle |

#### `PickingController` — Kommissionierung

| Action | HTTP | Beschreibung |
|--------|------|-------------|
| `Index` | GET | Kommissionierliste oder Dropdown-Fallback |
| `Bom` | GET | Stückliste mit Picking-Checkboxen |
| `TransferPicked` | POST | Umbuchen (AJAX) |
| `SetPickingStatus` | POST | Status setzen (AJAX) |
| `PrintBom` | GET | Stückliste drucken |
| `PrintPicking` | GET | Kommissionierliste drucken |

#### `PhotoController` — Fotos (API)

| Action | HTTP | Route | Beschreibung |
|--------|------|-------|-------------|
| `Upload` | POST | `api/photos/upload` | Foto hochladen |
| `Get` | GET | `api/photos/{productionOrderId}` | Fotos abrufen |
| `Delete` | POST | `api/photos/delete` | Foto löschen |

#### `ProductionOrdersApiController` — unverändert

---

## 4. View-Ordner-Struktur

### Nachher

```
Views/ProductionOrders/
  Index.cshtml                (FA-Liste)

Views/Picking/
  Index.cshtml                (Kommissionierliste)
  IndexDropdown.cshtml        (Fallback bei LeitstandAktiv=false)
  Bom.cshtml                  (Stückliste)
  PrintBom.cshtml             (Druck)
  PrintPicking.cshtml         (Druck)
```

---

## 5. URL-Änderungen

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

---

## 6. Referenz-Updates (Fallstricke)

### Kritische Stellen

| Stelle | Warum kritisch |
|--------|---------------|
| `Bom.cshtml` (~1000 Zeilen) | Viele `Url.Action()`-Aufrufe in JS: TransferPicked, SetPickingStatus, TogglePicked, PrintBom. Alle müssen auf `Picking`-Controller zeigen |
| `_Select2ProductionOrderPartial.cshtml` | Select2-Redirect nach FA-Auswahl → muss auf `/Picking/Bom/{id}` zeigen |
| `Index.cshtml` (FA-Liste) | Link zur Stückliste: `asp-action="Bom"` braucht `asp-controller="Picking"` |
| `_Layout.cshtml` | Alle Menü-Links + Badge-Count |
| `Home/Index.cshtml` | Dashboard-Karten-Links + Gruppierung |
| `Help/Index.cshtml` | Alle Hilfe-Texte mit "Werkstattauftrag" |
| `Help/Changelog.cshtml` | Historische Einträge — NUR aktuelle Version umbenennen, ältere bleiben |

### Regex-Safe Replacements

Die meisten Ersetzungen sind sichere String-Replacements:
- `Werkstattauftrag` → `Fertigungsauftrag` (nur in UI-Texten, nicht in Code)
- `WA Nr.` → `FA Nr.` / `WA Nummer` → `FA Nummer` (Labels)
- `WA @Model.` → `FA @Model.` (Razor-Ausdrücke)
- `asp-controller="ProductionOrders" asp-action="Bom"` → `asp-controller="Picking" asp-action="Bom"`

---

## Nicht im Scope

- DB-Änderungen (Tabellen, Spalten bleiben)
- C#-Property-Renames (OrderNumber, ProductionOrder bleiben)
- Neue Features
- BDE (nur Menü-Platz vorgesehen)
- Redirect von alten URLs
- Refactoring anderer Controller

## Risiken

| Risiko | Mitigation |
|--------|-----------|
| Vergessene Url.Action()-Referenz in Bom.cshtml | Grep nach "ProductionOrders" in allen Views nach dem Umbau |
| Historische Changelog-Einträge mit "WA" | Nur aktuellen v1.2.0 Eintrag umbenennen, ältere als historisch belassen |
| Select2-Partial referenziert alten Controller | Explizit testen nach Umbau |
| Dashboard-Karten zeigen auf falsche Controller | Explizit prüfen nach Umbau |

## Testszenarien

| Test | Erwartung |
|------|-----------|
| /Picking → Kommissionierliste | Tabelle (wenn LeitstandAktiv) oder Dropdown |
| /Picking/Bom/123 → Stückliste | Stückliste öffnet, alle Buttons funktionieren |
| /Picking/TransferPicked → Umbuchen | AJAX-Umbuchen funktioniert |
| /ProductionOrders/Index → FA-Liste | FA-Liste mit Freigabe-Spalte (wenn Leitstand) |
| Dashboard | Karten nach Domänen gruppiert, Links korrekt |
| Menü-Links | Alle verweisen auf korrekte Controller |
| Foto-Upload in Stückliste | /api/photos/* funktioniert |
| Select2-FA-Suche → Bom | Wählt FA → öffnet /Picking/Bom/{id} |
| Alle Labels zeigen "FA" | Kein "WA" mehr in der UI |
| Hilfe-Seite | Texte sagen "Fertigungsauftrag" |
| Grep nach "Werkstattauftrag" in Views | 0 Treffer (nur in Changelog-Historie) |

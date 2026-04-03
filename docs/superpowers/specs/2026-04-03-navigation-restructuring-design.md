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

### Zusätzlich betroffene Dateien (aus Quality Review)

- `IdealAkeWms/Models/ProductionOrder.cs` Zeile 7: `[Required(ErrorMessage = "WA Nummer ist erforderlich")]` → `"FA Nummer ist erforderlich"`
- `IDEALAKEWMSService/Services/PartRequisitionEmailService.cs` Zeile 116: `Werkstattauftrag {first.OrderNumber}` → `Fertigungsauftrag` (HTML-E-Mail-Template!)
- `IdealAkeWms/Controllers/ProductionOrdersController.cs`: TempData-Meldungen mit "WA" (z.B. Zeile 115: `WA {order.OrderNumber} kann nicht freigegeben werden`)
- `IdealAkeWms/Services/PickingTransferService.cs`: Log-Meldungen mit "WA-Id", Kommentare "WA-Nummern zusammenführen"
- `IdealAkeWms/Views/Help/Index.cshtml` Zeile 76: Label "Werkstattauftrag-Dokument" → "Fertigungsauftrag-Dokument" (UI-Label, DB-Wert `DocumentType="Werkstattauftrag"` bleibt!)

### Was NICHT umbenannt wird

- C#-Property-Namen (`OrderNumber`, `ProductionOrder`, etc.)
- `PickingTransferResult.CurrentWaNumbers` / `NewWaNumber` — Properties + JSON-Keys bleiben (Breaking Change für JS-Consumer vermeiden). In der Spec als bewusste Entscheidung dokumentiert.
- DB-Spalten und Tabellen
- DB-Wert `EnaioDmsDocument.DocumentType = "Werkstattauftrag"` — spiegelt das externe enaio-System wider
- URL-Routen (`/ProductionOrders/Index` bleibt)
- CSS-Klassen (`.wa-number` — intern)
- Variable-Namen in JavaScript
- Test-Daten (`"WA-001"` in Tests — repräsentieren echte Auftragsnummern)

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
| `TogglePicked` | POST | Picking-Status toggeln (Legacy, derzeit nicht aufgerufen) |
| `TransferPicked` | POST | Umbuchen (AJAX) |
| `SetPickingStatus` | POST | Status setzen (AJAX) |
| `PrintBom` | GET | Stückliste drucken |
| `PrintPicking` | GET | Kommissionierliste drucken |

**Hinweis:** `TogglePicked` existiert noch im Code, wird aber seit v1.1.1 nicht mehr aus der Bom-View aufgerufen (Picking ist jetzt client-seitig). Bleibt für Abwärtskompatibilität.

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

### Kritische Stellen (MÜSSEN geändert werden, sonst Breakage)

| Stelle | Datei | Problem | Fix |
|--------|-------|---------|-----|
| Select2-Redirect | `_Select2ProductionOrderPartial.cshtml:37` | Hardcoded `'/ProductionOrders/Bom/' + id` | → `'/Picking/Bom/' + id` |
| Foto-Upload JS | `wwwroot/js/photo-upload.js:59,80,118` | 3 hardcoded URLs `/ProductionOrders/UploadPhoto`, `GetPhotos`, `DeletePhoto` | → `/api/photos/upload`, `/api/photos/{id}`, `/api/photos/delete` |
| FA-Liste Bom-Link | `ProductionOrders/Index.cshtml:91` | `asp-action="Bom"` ohne Controller → löst gegen ProductionOrders auf (hat kein Bom mehr!) | → `asp-controller="Picking" asp-action="Bom"` |
| Bestellungen Bom-Link | `PartRequisitions/Index.cshtml:115` | `asp-controller="ProductionOrders" asp-action="Bom"` | → `asp-controller="Picking"` |
| Bom "Zurück"-Link | `Bom.cshtml:456` | `asp-action="Index"` ohne Controller → löst gegen Picking/Index auf statt FA-Liste | → `asp-controller="ProductionOrders" asp-action="Index"` |
| Bom.cshtml JS | `Bom.cshtml` (mehrere Stellen) | `Url.Action("TransferPicked")`, `Url.Action("SetPickingStatus")`, etc. | Lösen sich korrekt auf da Bom jetzt in PickingController liegt — KEIN Fix nötig |
| Picking-Fallback View-Name | `PickingController.cs` | `return View("PickingDropdown")` | → `return View("IndexDropdown")` (neuer Dateiname) |

### Wichtige Stellen (sollten geändert werden)

| Stelle | Datei | Problem | Fix |
|--------|-------|---------|-----|
| E-Mail-Template | `IDEALAKEWMSService/.../PartRequisitionEmailService.cs:116` | "Werkstattauftrag" in HTML-E-Mail | → "Fertigungsauftrag" |
| TempData Messages | `ProductionOrdersController.cs:115,368` | "WA" in User-Meldungen | → "FA" |
| Log Messages | `PickingTransferService.cs:249` | "WA-Id" in Serilog | → "FA-Id" |
| ErrorMessage | `ProductionOrder.cs:7` | `ErrorMessage = "WA Nummer ist erforderlich"` | → "FA Nummer ist erforderlich" |

### Verifikationsschritt nach Implementierung

```bash
# MUSS 0 Treffer ergeben (außer in Changelog-Historie und DB-Wert "Werkstattauftrag" in EnaioDms):
grep -r "ProductionOrders" --include="*.cshtml" IdealAkeWms/Views/Picking/
grep -r "Werkstattauftrag" --include="*.cshtml" IdealAkeWms/Views/ | grep -v Changelog
grep -r "/ProductionOrders/Bom" --include="*.cshtml" --include="*.js" IdealAkeWms/
```

---

## Redirect-Stubs (empfohlen)

Einfache Redirect-Actions im `ProductionOrdersController` für die verschobenen Actions, damit Bookmarks und Browser-History nicht brechen:

```csharp
// Redirect-Stubs für verschobene Actions
public IActionResult Bom(int id) => RedirectToActionPermanent("Bom", "Picking", new { id });
public IActionResult Picking() => RedirectToActionPermanent("Index", "Picking");
```

Niedrig-Aufwand, hoher Nutzen. Können nach einigen Monaten entfernt werden.

## Nicht im Scope

- DB-Änderungen (Tabellen, Spalten bleiben)
- C#-Property-Renames (OrderNumber, ProductionOrder, CurrentWaNumbers bleiben)
- Neue Features
- BDE (nur Menü-Platz vorgesehen)
- Refactoring anderer Controller (StockMovements, Tracking, etc.)

## Risiken

| Risiko | Mitigation |
|--------|-----------|
| Vergessene Controller-Referenz nach Split | Grep-Verifikation (siehe oben) als Pflichtschritt nach Implementierung |
| Historische Changelog-Einträge mit "WA" | Nur aktuellen v1.2.0+ Eintrag umbenennen, ältere als historisch belassen |
| `_ViewImports.cshtml` für neuen Picking-Ordner | Wird automatisch von `Views/_ViewImports.cshtml` geerbt — kein neuer nötig |
| `@section Scripts` in verschobenen Views | Funktioniert mit `_Layout.cshtml` unverändert — verifizieren |
| `photo-upload.js` hat 3 hardcoded URLs | Explizit in Implementierung adressiert (Kritische Stellen) |

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
| E-Mail-Benachrichtigung | Sagt "Fertigungsauftrag" statt "Werkstattauftrag" |
| Alte URL /ProductionOrders/Bom/123 | 301-Redirect auf /Picking/Bom/123 |
| Bestellungen-Seite Bom-Link | Öffnet /Picking/Bom/{id} korrekt |
| Bom "Zurück"-Link | Geht auf FA-Liste (/ProductionOrders/Index), nicht Kommissionierliste |
| Grep "Werkstattauftrag" in Views | 0 Treffer (nur Changelog-Historie + EnaioDms DB-Wert) |
| Grep "ProductionOrders" in Views/Picking/ | 0 Treffer |
| Grep "/ProductionOrders/Bom" in JS/Views | 0 Treffer (nur Redirect-Stubs) |

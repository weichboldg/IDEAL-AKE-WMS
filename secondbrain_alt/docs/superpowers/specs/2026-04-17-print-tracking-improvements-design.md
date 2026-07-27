# Stücklisten-Druck + OSEON-Teileverfolgung — Verbesserungen

## Zusammenfassung

Vier unabhängige Verbesserungen in zwei Bereichen: (A) Stücklisten-Druck zeigt druckenden Anwender + respektiert Spaltenfilter, (B) OSEON-Teileverfolgung bekommt Artikelnummern-Suche (inkl. QR-Scan) + Lagerbestand-Modal pro WA-Nummer.

## A1: Anwender-Name im Stücklisten-Druck

Beide Druckansichten (`PrintBom.cshtml`, `PrintPicking.cshtml`) zeigen den Namen des Anwenders der den Druck ausgelöst hat, dezent unter der FA-Nummer.

### Änderungen

- `PrintBomViewModel` + `PrintPickingViewModel`: neues Feld `string PrintedBy`
- `PickingController.PrintBom()` + `PrintPicking()`: `vm.PrintedBy = _currentUserService.GetDisplayName()`
- Beide Print-Views: `<div class="printed-by">Gedruckt von: @Model.PrintedBy</div>` unterhalb der `.wa-number`
- CSS: `font-size: 12px; color: #666; margin-top: 2px;`

## A2: Spaltenfilter im Druck übernehmen

Aktuell: Zeilen-Filter (ausgeblendete Positionen) werden im Druck berücksichtigt. Spalten-Sichtbarkeit (via column-preferences) wird ignoriert — PrintBom zeigt immer alle Spalten.

### Änderungen

- JS `btnPrintBom`-Handler: liest sichtbare Spalten aus DOM (`<th>` mit `data-col-key` die `display !== 'none'` haben), übergibt als URL-Parameter `visibleColumns=col1,col2,...`
- `PrintBomViewModel`: neues Feld `List<string> VisibleColumns`
- `PickingController.PrintBom()`: Parameter `string? visibleColumns` parsen → ViewModel
- `PrintBom.cshtml`: jede `<th>`/`<td>` nur rendern wenn zugehöriger Column-Key in `VisibleColumns` enthalten ODER `VisibleColumns` leer (Fallback = alle)

## B1: Artikelnummern-Suche in OSEON-Teileverfolgung

Neues Suchfeld im Filter-Bereich von `OseonIndex.cshtml` für Artikelnummern-Suche per Texteingabe oder QR-Code-Scan.

### Funktionalität

- Neues Input-Feld "Artikelnummer" mit Scan-Button (nutzt bestehendes `barcode-scanner.js` Pattern)
- **Client-seitige Filterung** (kein Server-Roundtrip): durchsucht Level-1-Zeilen (Sub-Orders) nach `ArticleNumber`
- Matching: `row.dataset.articleNumber.includes(searchTerm)` (Teilstring-Match)
- Nicht-matchende Sub-Order-Zeilen + deren Operationen werden ausgeblendet
- Übergeordnete Gruppe bleibt sichtbar wenn mindestens ein Sub-Order matcht
- Ergänzt bestehenden `filterCustomerOrder` — beide Filter gleichzeitig aktiv
- Leeres Suchfeld = kein Filter (alle anzeigen)

### Daten-Attribut

`OseonIndex.cshtml` Level-1-Zeilen: `data-article-number="@sub.ArticleNumber"` als Attribut ergänzen für JS-Zugriff.

### QR-Code-Scan

Scan-Input nutzt dasselbe Pattern wie im BDE-Terminal: Input-Feld + Button, Enter-Taste triggert Suche. QR-Code-Scanner sendet Tastatureingabe + Enter. FA-Nummer (Komma-Suffix) wird automatisch bereinigt: `.split(',')[0].trim()`.

## B2: Lagerbestand-Modal pro WA-Nummer

Button in jeder Level-1-Zeile (Sub-Order) der ein Modal mit Kurz-Übersicht der Lagerbestände für diese WA-Nummer zeigt.

### UI

- Button/Icon neben der WA-Nummer: Bootstrap-Icon `bi-box-seam`, Tooltip "Lagerbestand anzeigen"
- Klick öffnet Bootstrap-Modal mit AJAX-geladener Tabelle
- Modal-Inhalt: Tabelle mit Spalten: Artikelnummer, Bezeichnung, Lagerplatz, Menge
- Unter der Tabelle: Link "Details in Bestandsübersicht →" öffnet `/StockOverview?filterProductionOrder=<WA-Nummer>` in neuem Tab
- Leere Tabelle (keine Buchungen): "Keine Lagerbuchungen für diesen Auftrag."

### API-Endpoint

Neuer Endpoint: `GET /api/stock/by-order/{orderNumber}` (oder Erweiterung eines bestehenden)

Response:
```json
{
  "orderNumber": "WA-12345",
  "items": [
    { "articleNumber": "100-123", "description": "Seitenteil", "storageLocation": "L01-A03", "quantity": 5.0 }
  ]
}
```

Logik: bestehende `IStockOverviewRepository.GetStockByProductionOrderAsync(orderNumber)` oder äquivalent.

### Zugriffskontrolle

Endpoint erfordert `[RequireTrackingAccess]` oder `[RequirePickingOrTrackingAccess]` — Tracking-User dürfen Bestände sehen, aber nicht buchen.

## Betroffene Dateien

| Datei | Änderung |
|-------|----------|
| `Models/ViewModels/BomViewModels.cs` | +PrintedBy, +VisibleColumns |
| `Controllers/PickingController.cs` | PrintBom/PrintPicking: User-Name + Spalten |
| `Views/Picking/PrintBom.cshtml` | Anwender + bedingte Spalten |
| `Views/Picking/PrintPicking.cshtml` | Anwender |
| `Views/Picking/Bom.cshtml` | JS: Spalten-Keys an Print-URL |
| `Views/Tracking/OseonIndex.cshtml` | Artikelsuche + Lagerbestand-Button + Modal |
| `Controllers/StockOverviewController.cs` oder neuer API-Controller | Stock-by-Order-Endpoint |

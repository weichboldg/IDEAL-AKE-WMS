---
typ: bug
---
# Lagerbestellung-Details: Zahnrad (Spaltenauswahl) unsichtbar

**Aufgenommen 2026-10-06** – Fund aus dem Fix v1.47.3 ([[2026-10-06-stueckliste-zahnrad-bug-matchcode]]),
nachgeprüft von der Koordination im Hauptcheckout (`main`).

## Befund (am Code belegt, `main`)

- `IdealAkeWms/Views/WarehousePicking/Details.cshtml:53`: `<div id="incomplete-alert" class="alert alert-warning d-none">`
  steht **direkt vor** `.table-responsive` (Z. 54; Tabelle Z. 55, `data-view-key="WarehousePickingDetails"`).
- `column-preferences.js` hängt das Zahnrad an dieses versteckte DIV → **unsichtbar**.
- Wird der Hinweis angezeigt, überschreibt das Skript dessen Inhalt (`incompleteAlert.textContent = text`,
  Z. 359) und **löscht damit das Zahnrad** bis zum Neuladen.
- Eingeführt mit dem AKE-Hotfix v1.30.1 (Commit `f653f8a0`). Betrifft **AKE-Produktion** und – über den
  Vorwärts-Merge – auch das IDEAL-Bündel.
- Folge für Anwender: Spaltenauswahl auf der Lagerbestellungs-Detailseite nicht erreichbar. Keine
  Auswirkung auf Daten oder Buchungen.

## Lösungsrichtung

Wie v1.47.3 bei der Stückliste: leerer Zahnrad-Host als direkter Vorgänger von `.table-responsive`,
`#incomplete-alert` darüber. Regel: [[fallstricke]] §19.

## Entscheidung offen (Gerald)

Hotfix auf `main` (v1.30.2, eigener Deploy AKE) oder Korrektur im Bündel (kommt mit dessen Merge nach AKE).

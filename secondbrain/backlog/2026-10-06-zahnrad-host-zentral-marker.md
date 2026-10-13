---
typ: bug
---
# Spaltenauswahl: Zahnrad-Platzierung zentral über einen Marker statt „Element davor“

**Aufgenommen 2026-10-06** – Fund aus dem Fix v1.47.3 (qa-agent-Review, Prüfung aller 32 Views mit
`column-preferences.js`, Bündel-Stand).

## Befund

`column-preferences.js` (`insertGearButton`, Z. 367-389) hängt das Zahnrad an das Element **direkt vor**
`.table-responsive`, sofern es ein DIV ist. Jede spätere Änderung, die dort ein DIV einfügt, verschiebt
das Zahnrad still. Gemeldete Fälle (Bündel, nicht einzeln nachgeprüft):

- **Unsichtbar:** `WarehousePicking/Details` → eigene Notiz [[2026-10-06-lagerbestellung-zahnrad-unsichtbar]].
- **Sichtbar, aber an falscher Stelle:** Roles, Users, Workstations, WorkSteps, ProductionWorkplaces,
  OrderRecipients (bei Nur-Lesen-Benutzern im blauen `alert-info`-Hinweis); FaCompletion, FaWorklist,
  Picking/ProductionOrders (in der Zählzeile Aufträge/Sub-FA).

## Lösungsrichtung (Vorschlag qa-agent)

Expliziter Marker, z. B. `data-column-prefs-host`, den `column-preferences.js` bevorzugt; erst ohne Marker
gilt das heutige Verhalten. Marker in allen betroffenen Views setzen, Drift-Guard-Test analog
`BomZahnradHostTests`.

## Reihenfolge

Querschnittlich über viele Views → **nach dem Merge des Bündels**, nicht als Hotfix.

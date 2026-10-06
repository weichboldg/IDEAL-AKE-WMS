---
type: changelog
version: 1.47.3
date: 2026-10-06
---
# v1.47.3 — Stückliste: Zahnrad (Spaltenauswahl) wieder sichtbar

Bugfix im **Bündel-**Worktree `feature/2026-08-07-ideal-teile-1-5`. Befund und Ursache:
[[2026-10-06-stueckliste-zahnrad-bug-matchcode]] (Abschnitt „Ursache“). **Keine Migration.**

**Warum:** Seit v1.42.0 ([[2026-09-18-stueckliste-kommissionierziel-filter-spec]]) standen
`#bomDefaultFilterBadges` und `#bomKzEmptyState` (`hidden`) zwischen dem Zahnrad-Toolbar-Host und
`.table-responsive`. `column-preferences.js` `insertGearButton` hängt das Zahnrad an den direkten Vorgänger
der Tabelle — also in den versteckten Leerzustand. `main` (AKE) ist nicht betroffen.

## Kernpunkte

- Fix nur in `Views/Picking/Bom.cshtml`: die beiden DIVs stehen jetzt **vor** dem Host; Kommentar am Host um
  „dazwischen nichts einfügen“ ergänzt. `column-preferences.js` unverändert.
- Wächter `BomZahnradHostTests` (Quelltext: Element direkt vor `<div class="table-responsive">` ist der Host).
- TS-79.20 neu, [[fallstricke]] §19.
- Gleiche Fehlerklasse ohne Fix gemeldet: `Views/WarehousePicking/Details.cshtml` (`#incomplete-alert`,
  `d-none`, direkt vor der Tabelle) — gilt für AKE und IDEAL, nicht Teil dieses Fixes.

## Deploy-Risiken

Nur Web-Publish, geht mit dem Bündel. Keine Daten- oder Konfigurationsänderung.

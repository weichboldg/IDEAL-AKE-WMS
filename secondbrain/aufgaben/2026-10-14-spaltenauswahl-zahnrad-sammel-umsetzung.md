---
typ: notiz
spec: "[[2026-10-14-spaltenauswahl-zahnrad-sammel-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Spaltenauswahl/Zahnrad — zentraler Toolbar-Host, Gruppenkopf, Speicherfehler, Anzeigename

Spec [[2026-10-14-spaltenauswahl-zahnrad-sammel-spec]] · **direkt im Bündel** (Freigabe-Nachtrag 2026-10-21), Start
`d52bc44d` (v1.64.1), Ziel **v1.65.0**, **TS-98**. Keine Migration. Deploy: nur Web.

## Plan (kompakt, Details mit Datei:Zeile in der Spec)

**Global:** Scrollbalken-Fix v1.64.1 (`site.js` Sentinel/ResizeObserver) unangetastet, TS-95.17–95.20 bleiben grün.
Matchcode Stückliste nur prüfen (seit v1.62.0 vorhanden).

1. **A Host** — `site.js`: `window.tableToolbarHost(table)`. `column-preferences.js insertGearButton` und `csv-export.js attach`
   ohne `previousElementSibling`. Marker `data-table-toolbar` an 7 vorhandenen Hosts (inkl. OperationConfigs), 2 Werkzeugzeilen
   und 4 neuen Hosts über der Zählzeile. Kommentare „MUSS direkter Vorgänger“ ersetzen. Wächter `TableToolbarHostTests`
   ersetzt die `*ZahnradHostTests`.
2. **B colspan** — `setColVisibility`: Zeilen mit `:scope > td[colspan]` überspringen. Wächter in
   `TableScriptScopedSelectorTests` ergänzen.
3. **C Fehlerhinweis** — `showPrefsMessage`, `saveSettings`/`resetToDefault` prüfen `response.ok`. Dazu der Hinweis im Panel,
   Z1 (Laden) und Z2 (`pagehide` mit keepalive).
4. **D** — `ColumnDefinitions.cs` DisplayName der zwei Vorschlagslisten.
5. **Doku** — v1.65.0 ×2, Changelog, Hilfe, TS-98, Nachziehen von TS-18.10/TS-79.20.

## Abweichungen

- **Host-Suche nicht dokumentweit, sondern zweistufig.** Erst der direkte Vorgänger des Wrappers mit Marker, dann ein
  **expliziter** Marker im Dokument. Selbst angelegte Hosts tragen `data-table-toolbar="auto"` und werden nur über ihren
  eigenen Wrapper gefunden. Grund: `BdeMasterData/Index.cshtml:27/79/129` hat drei `filterable-table` auf einer Seite. Bei
  dokumentweiter Suche stünden alle drei CSV-Knöpfe über der ersten Tabelle.
- 18 statt 17 Zahnrad-Ansichten: `OperationConfigs/Index` ist seit v1.61.0 hinzugekommen, dort steht ein Host an `:62`.

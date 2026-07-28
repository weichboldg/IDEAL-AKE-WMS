---
type: changelog
version: 1.7.0
date: 2026-04-10
---
# v1.7.0 — Individuell anpassbare Tabellenansichten

- Spalten ein-/ausblenden ueber ein Zahnrad-Panel, Reihenfolge per Drag & Drop, Spaltenbreiten
  ziehbar (Doppelklick setzt zurueck), Standard-Sortierung je View festlegbar,
  Rechtsklick-Kontextmenue auf dem Spaltenkopf, „Auf Standard zuruecksetzen".
- **Per-User-Persistierung** in `UserViewPreference`; Admins koennen die Einstellungen eines
  Benutzers im Benutzerstamm zuruecksetzen.
- Zentrale Spalten-Definition in `ColumnDefinitions.cs`; das Attribut **`data-col-key`** ersetzt die
  alten numerischen `data-col`-Attribute — seither Pflicht auf jedem `<th>`.
- **Init-Reihenfolge:** `column-preferences.js` dispatcht `column-preferences-ready`, auf das
  `table-filter.js` wartet — die Einbinde-Reihenfolge ist seitdem nicht beliebig.

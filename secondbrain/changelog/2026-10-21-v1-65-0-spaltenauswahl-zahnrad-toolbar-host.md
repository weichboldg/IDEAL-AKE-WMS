---
type: changelog
version: 1.65.0
date: 2026-10-21
---
# v1.65.0 — Spaltenauswahl: zentraler Toolbar-Host, Gruppenkopf bleibt, Speicherfehler sichtbar

Spec [[2026-10-14-spaltenauswahl-zahnrad-sammel-spec]], Umsetzung [[2026-10-14-spaltenauswahl-zahnrad-sammel-umsetzung]].
**Direkt im Bündel** (Freigabe-Nachtrag 2026-10-21), Basis `d52bc44d` (v1.64.1), Commits `f1108e94`, `c27f3b6d`. **Keine Migration**, kein SQL.

## Kernpunkte

- `site.js` `window.tableToolbarHost(table)`: ein Ort für Zahnrad und CSV. Den Marker `data-table-toolbar` tragen 13 Views: 7 vorhandene
  Hosts (inkl. OperationConfigs), die 2 Werkzeugzeilen von FA-Struktur/OSEON und 4 neue Hosts über der Zählzeile. Ohne Marker entsteht je
  Tabelle ein Fallback-Host `="auto"`. Die Nachbar-Heuristik `previousElementSibling` ist aus `column-preferences.js` und
  `csv-export.js` entfernt.
- AKE sichtbar geändert: Auf FA-Liste, FA-Vervollständigung, Abarbeitungsliste und Kommissionierung steht das Zahnrad nicht mehr im
  Filterkasten bzw. unter dem Schalter, sondern rechts über der Tabelle neben CSV (Antwort 1).
- Gruppenkopf- und Leerzustandszeilen bleiben stehen, wenn die erste Spalte ausgeblendet wird (`:scope > td[colspan]`-Skip).
- Scheitert Speichern, Laden (Z1) oder Zurücksetzen, erscheint ein gelber Hinweis (`role="alert"`) im Host und im Panel. Zurücksetzen
  lädt nur bei Erfolg neu. Eine noch nicht gespeicherte Änderung geht beim Verlassen der Seite per `keepalive` raus (Z2).
- `DisplayName` „Vorschlagsliste Beschichtung (IDEAL)“ / „… — Summiert (IDEAL)“. Der ViewKey bleibt.
- Wächter: `TableToolbarHostTests` und `ColumnPreferencesFehlerhinweisTests` ersetzen `BomZahnradHostTests.cs` (3 Klassen) und die
  Host-Tests in `BeschichtungsauftraegeViewTests`/`OperationConfigsViewTests`.

## Deploy-Risiken

Nur Web: statische Dateien und Views (`asp-append-version`). Das gemeinsame JS wirkt an **beiden** Standorten auf alle Listen mit CSV
oder Zahnrad. Benutzer ohne App-Konto (401) sehen jetzt bei jeder Spaltenänderung den Hinweis, bisher blieb der Fehler still.
Manual-UAT TS-98.

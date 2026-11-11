---
type: changelog
version: 1.64.1
date: 2026-10-20
---
# v1.64.1 — Tabellen: keine Seiten-Querscrollleiste, genau ein Scrollbalken

Bugfix [[2026-10-19-tabelle-seitenbreite-doppelter-scrollbalken-bug]], Nachbesserung zu v1.62.0
([[2026-10-16-stueckliste-matchcode-lagerplatz-limit-spec]], [[2026-10-16-stueckliste-lagerplatz-zelle-tabellenbreite-bug]]).
**Direkt im Bündel**, Basis `a9558301` (v1.64.0), Commits `4179e38d`, QA-Fix `d52bc44d`. Vorgabe war „v1.62.1“; das Bündel stand schon auf 1.64.0, daher
1.64.1. **Keine Migration**, kein SQL.

## Kernpunkte

- `site.css` `.table-responsive { position: relative; }` (global): Select2-versteckte `<select>` (absolut) hängen nicht mehr an `<html>`
  und ziehen die Seite nicht mehr auf volle Tabellenbreite (Bom/131: 3641 px Dokument bei 943 px Fenster).
- `site.js` sticky Scrollbar: Sentinel **unter** statt in den Wrapper. Im Wrapper scrollte er horizontal aus dem Clip-Bereich, dann
  standen am Tabellenende zwei Balken. `ResizeObserver` statt `resize` + 500 ms, der Balken folgt „Alle aufklappen“.
- Betroffen waren alle Views mit Select2 in `.table-responsive`: Picking/Bom, FaCompletion/Edit, StockMovements/InboundBulk.

## Deploy-Risiken

Nur Web, statische Dateien (`asp-append-version`, kein Cache-Problem). Wirkt auf **alle** Listen. Manual-UAT TS-95.17–95.20 mit Gegenprobe
einer Liste ohne Select2.

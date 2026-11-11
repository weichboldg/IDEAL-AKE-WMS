---
type: changelog
version: 1.53.0
date: 2026-10-12
---
# v1.53.0 — CSV-Export für alle Tabellen (Browser, ein Layout-Skript)

[[2026-10-06-csv-export-alle-tabellen-spec]] (Variante A, Antworten 1–14). **Direkt im Bündel** `feature/2026-08-07-ideal-teile-1-5`, Start `d6d6beb0`
(v1.52.0), Commits `2e6752a1`, `400a7520`, `b43edb25`, QA-Fix `9ad794b1`, `87db96c8`. **Keine Migration.** Umsetzung [[2026-10-12-csv-export-alle-tabellen-umsetzung]].

**Warum:** Listeninhalte gingen per Hand oder Kopieren nach Excel. Ein Baustein statt 40 Einzellösungen, damit Spalten,
Filter und Rechte nicht auseinanderlaufen.

## Kernpunkte

- `wwwroot/js/csv-export.js` im Layout: Knopf „CSV" an jeder `table.filterable-table` (Opt-out Tabelle/Spalte
  `data-csv-export="false"`), im selben Host wie das Zahnrad. Server-Mode: zweiter GET derselben Action mit `pageSize=0`
  (gleiche Rechte/Gates/Filter), Kappung 5000 mit Warnung inkl. Sortier-Satz; Client-Mode aus der Live-Tabelle, Stückliste
  über `data-csv-filter-match` unabhängig vom Klappzustand.
- Sichtbare Spalten in Bildschirm-Reihenfolge, Bildschirm-Sortierung (Vergleich aus `table-filter.js` herausgelöst als
  `window.tableFilterCompareValues`), flache Gruppen mit Spalte HauptFA, Zellwerte (Badges, Ja/Nein, Eingaben, Radios,
  Icons), Datum ohne KW, Injection-Schutz mit Zahlen-Ausnahme, Semikolon/UTF-8-BOM/CRLF, `<Ansicht>_yyyy-MM-dd_HHmm.csv`.
- Pflichtprüfung: alle 37 Listen-Actions ohne Schreibzugriff im GET-Pfad (Tabelle in der Spec). ADR 0005 Nachtrag.
- Wächter `CsvExportCoverageTests` (u. a. jede überschriebene Spalte hat `data-col-key`, paginiert ⇒ Server-Mode) +
  Node-Kernprüfung `csv-export.check.js`. Tests Web 1767 (+131), Service 272. TS-86.

## Deploy-Risiken

Nur Web, keine Migration, Service nur Versions-Bump. Vor der Abnahme Prüfschritt Server-Kultur (Spalte „Stk." der
FA-Liste: `1,000` = deutsch). Zwischen-Commit `400a7520` baut allein nicht (Razor-`@` in der Hilfe, behoben in `b43edb25`).

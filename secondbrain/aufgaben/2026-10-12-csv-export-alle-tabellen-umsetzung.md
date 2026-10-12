---
typ: notiz
spec: "[[2026-10-06-csv-export-alle-tabellen-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: CSV-Export für alle Tabellen

Spec [[2026-10-06-csv-export-alle-tabellen-spec]] · **direkt im Bündel** (Entscheidung Gerald 2026-10-09), Start `d6d6beb0` (v1.52.0). Variante A: ein
Layout-Skript hängt den Knopf an jede `filterable-table`, Server-Mode per zweitem GET mit `pageSize=0`. Deploy: Web, keine
Migration.

## Stand

- 2026-10-12: Pflichtprüfung Wirkungsfreiheit aller 37 Listen-Actions ohne Fund (Tabelle in der Spec) — kein Stopp.
  Umsetzung feature-complete (`2e6752a1`, `400a7520`, Build-Fix `b43edb25`). QA läuft.
- 2026-10-12: QA fand I-1 (Spalten ohne `data-col-key` fielen still weg: Bewegungen ohne Menge, Artikelgruppen ohne Gruppen) und I-2 (Radios) — behoben in `9ad794b1` samt Wächter; QA-Nachlauf ohne Critical/Important → **Testbereit**. Commits `2e6752a1`, `400a7520`, `b43edb25`, QA-Fix `9ad794b1`, `87db96c8`. Build + 1767 Web- / 272 Service-Tests grün. Changelog [[2026-10-12-v1-53-0-csv-export-alle-tabellen]].
- Schranke 2 mit dem Bündel: TS-86 inkl. 86.17; vorher Prüfschritt Server-Kultur.

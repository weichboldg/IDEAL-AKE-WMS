---
typ: notiz
spec: "[[2026-09-25-kommissionierliste-summierung-pdf-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Kommissionierliste — Summierung als Standard, Druck/PDF nur sichtbare Spalten

Spec [[2026-09-25-kommissionierliste-summierung-pdf-spec]] · **bestehender Bündel-Worktree** (kein neuer,
laut Spec-Hinweis „Umsetzungsort“ und Auftrag).

## Pre-Flight-Verdikt (2026-09-28)

- status Freigegeben, open_questions [], freigabe_entscheidung/_von/_am gesetzt.
- Kritische Prüfung: B1 (Weg a, PrintBom-Muster), B2 (Matchcode + Hauptlagerplatz), B3 (blockiert nicht,
  Einheiten-Prüfschritt vor der Abnahme) entschieden; Rückfrage 5 (KW-Konflikt) → Freigabe-Antwort 5,
  Variante (b). Kein offener Blocker.
- Voraussetzung erfüllt: [[2026-09-25-kommissionierung-nur-hauptfa-spec]] Testbereit (v1.46.0),
  `BuildFlagPredicate` delegiert an `KommissionierRelevanzFilter.IsRelevant`.
- Freigabe-Antwort 5, letzter Satz „Umsetzung erst, wenn die Abnahme des Bündels begonnen hat“: Der
  Dev-Lauf wurde vom Menschen am 2026-09-28 ausdrücklich angestoßen — als erfüllt/übersteuert gewertet.
- Worktree-Stand: AppVersion 1.46.0 → dieser Lauf **1.47.0**; TS-79 → **TS-80**; keine Migration.
- **Nicht Teil des Dev-Laufs:** Einheiten-Prüfschritt (`sp_helptext` / `INFORMATION_SCHEMA`, Sage-DB IDEAL)
  — Vorbedingung der Abnahme, erster Schritt in TS-80.

## Stand

- [x] Plan
- [x] Service: KW optional, Matchcode/Hauptlagerplatz, Gruppen-Paging, Umschalter-Query
- [x] ColumnDefinitions Summiert (6) + Registrierung
- [x] Controller: visibleColumns + Rückfall, PrintSummiert/PdfSummiert
- [x] Views: Index/Summiert/Print/PrintSummiert, print-visible-columns.js, _Layout
- [x] Tests: Umkehr NoKw, neue Service-Tests, Drift-Guard
- [x] Version, Changelog, Hilfe, TS-80, Brain (Teil-3-Vermerk N2d, fallstricke Mengeneinheit)

## Verlauf

- 2026-09-28: InUmsetzung gesetzt (Worktree/Branch eingetragen).
- 2026-09-28: QA — Build grün, 1435+268 Tests grün, alle harten Prüfungen (Diff, BuildFlagPredicate/
  WarehousePickingPrintLayout unberührt, Html.Raw 0 Treffer, AppVersion 1.47.0, ToggleSharedQueryKeys,
  _Layout-Links) bestanden, AK 1–20 gegen Code/Tests geprüft, TS-80 vollständig (TS-80.1 = Einheiten-
  Prüfschritt), Testindex nachgezogen. Status **Testbereit** — QA-Nachweis + manuelle Checkliste am
  Ende der Spec. Wartet auf Schranke 2 (Mensch: TS-80.1 auf Sage-DB IDEAL, dann Manual-UAT, dann
  Merge mit dem gesamten Bündel).

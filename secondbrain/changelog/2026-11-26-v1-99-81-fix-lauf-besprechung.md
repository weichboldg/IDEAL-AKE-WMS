---
type: changelog
version: 1.99.81
date: 2026-11-26
---
# v1.99.81 — BDE-Fix-Lauf nach der Besprechung 2026-11-26

Plan [[2026-11-26-fix-lauf-v1-99-81]], Entscheide in [[2026-11-26-besprechung-0830-kritische-pruefung]]. Worktree
`feature/2026-08-07-ideal-teile-1-5`, Commits `e6f76159` (Task 1), `057db9b3` (2), `7307ec66` (3), `2d19fadc` (4), `bba0df86` (4b),
`1da90118` (5), `7a12266d` (6), `d08e8e29` (7, Doku). **Keine Migration, keine neue Einstellung.** TS-202. ADR [[0039-bde-werker-personalnummer-identifiziert-nicht-authentisiert]].
Stand: feature-complete, QA (Task 8) und Paket 21 offen. Nicht gemergt.

## Kernpunkte

- **Wächter an allen Terminal-Aktionen** (2A): `BdeTerminalController.WaechterAsync` (eigenes Terminal + aktiver Werker) und
  `FremdeBuchungAsync` (+ Besitz der Buchung) vor Pause/Finish/ReportPartial/Resume/ClosePaused; FinishGroup/CloseOthers/Start* mit Wächter;
  CloseOthers nur nach eigener Fertigmeldung auf dem AG. Start-Terminal zählt nicht. Feste Texte statt `ex.Message` (`ServerfehlerAntwort`).
  GETs `ActiveBookings`/`PausedBookings` gelöscht.
- **Gruppen-Abschluss** aus `BdeKopfBuchungen` nur für die Werkbank der angeklickten Buchung, mit Sub-FA/Werkbank; Zeilen ohne Menge abgewiesen.
- **0/0 als Abschluss nur nach Teilmenge** (Finish, FinishGroup, ReportCompletionCore); Teilmenge ≥ Soll mit Hinweis „Sollmenge erreicht“.
  Fertig-Dialog: Gut Pflicht, Rest vorbelegt (`BdeKopfBuchung.BisherGut`), Rückfrage über Soll.
- **Kopf einklappbar** (K-1, localStorage mit try/catch), laufende Produktion markiert (U-1), Sperre erst nach Dialog, Enter bestätigt,
  Waisen-Hinweis zuklappbar, Werkbank-Auswahl fuer alle wie bisher, beim BDE-Admin bleibt der Werker angemeldet (Korrektur 44c1011e).
- **Pausierte abschließen** (Entscheid 9): `IBdeBookingService.FortsetzenSperreAsync` (dieselben drei Prüfungen wie Resume, ein Ort) und
  `ClosePausedAsync` (Status beendet, `EndedAt ??= Now`, Audit). Kopf zeigt „Nicht fortsetzbar: <Grund>“ + „Abschließen“; neue Admin-Liste
  `BdeBookings/HaengendePausen` (Schichtleiter lesen, `PauseAbschliessen` nur BDE-Admin), Server-Spaltenfilter + Pagination.
- **Tageshistorie** über Mitternacht an beiden Tagen, Zeit mit Datum, Sub-FA im Ziel.
- **IDEAL**: Glasbestellungen weder in der Teileliste noch meldbar (`Type == Lager` in `LagerZeilenAsync` und Meldeprüfung); CSV Matchcode
  per `data-csv-value`; Summiert-Zellen alle mit `data-col-key`.
- **Fräsen-Pfad**: `Y:\` = `\\idealfs1\Daten$\Produktion\CAM\Format4\` in Einmal-Skript, Fehlertext, Formular, Hilfe; Manifest ohne
  Voraussetzung 15/16, Text „im Prod-Fenster SCHARF“. Trockenlauf DEV IDEAL: TREFFER 3, alle mit `…\CAM\Format4\`, nichts geschrieben.
- **SQL/169**: Vorbelegung `BdeStandardsichtTeile` setzt `ModifiedAt/By/ByWindows` (Wegwerf-DB getestet, gelöscht). Manifest H-9 Text „entfällt“.
- Texte: „Keine BDE-Buchung mehr möglich“ (Umlaut, kein Gedankenstrich), Abweisung endet mit „Nichts gebucht.“ als eigenem Satz.

## Deploy-Risiken

- Kein Schema. SQL/169 wirkt nur, wo es noch nicht lief (AKE-Prod/IDEAL-Prod: erst mit diesem Paket).
- IDEAL: Einmal-Skript Fräsen-Pfad im Prod-Fenster SCHARF (DB-Backup aus dem Lauf).
- Werkbank-Wechsel fuer Werker unveraendert (danach neu anmelden); nur der BDE-Admin behaelt den Werker (44c1011e).
- Kommissionier-Nachtrag läuft beim ersten Dienststart sofort (1C, nur in Doku/Checkliste).

## Offen

- Task 4b: Ursache des gemeldeten Abteilungsfilter-Fehlers nicht belegt (Server filtert korrekt, Test grün; `table-filter.js` liest `th`-Index).
  Repro mit konkretem Wert auf DEV durch QA/Gerald.
- Fotos BDE (Kapitel 11) per Playwright erst nach DEV-Deploy 1.99.81; Sichtprüfung 1280/390 px offen.

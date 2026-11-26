---
type: aufgabe
status: in-arbeit
created: 2026-11-26
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-26: Lagerbestellung-Hinweis + Bündel 1 (Beschichtung summiert, Leitstand HauptFA, Kommissionierauftrag Liste)

Auftrag Gerald 2026-11-26 (`/goal` + `/goal-run`, vier Specs in Auftragsreihenfolge; Übersicht
[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-uebersicht]] regelt Teil 1 → 2 → 3).
Start-Stand Worktree: v1.99.85 (c8814cdf), höchstes TS 206 (TS-205 reserviert für Teil 1), letztes Paket 26, keine SQL-Skripte nötig.

| Spec | Typ | SQL-Block | Version | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|---|
| [[2026-11-26-lagerbestellung-hinweis-fehlteil-bereits-bestellt-spec]] | Spec | – | 1.99.86 | 207 | **Testbereit** | 300dbf25, QA b7704475 | |
| [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-1-spec]] | Spec | – | 1.99.87 | 205 | **Testbereit** | e67e9f6f, QA 9da4b0af | |
| [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]] | Spec | – | 1.99.88 | 208 | InUmsetzung (QA läuft) | 5d7639dc; N-1 Teil 1 a89d98cc | |
| [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-3-spec]] | Epic (A+B) | – | 1.99.89 (geplant) | | InUmsetzung | | |

## Vorprüfung
- Alle vier: status Freigegeben, worktree/branch leer, freigabe_* gesetzt, keine offenen BLOCKER in „Kritische Pruefung“
  (Bündel 1: Antworten Gerald 2026-11-26, Commits 9efc55f5/268e3995/56488adb/0c57f576).
- Worktree sauber, v1.99.85 der Parallel-Session Testbereit (Paket 26).

## Protokoll
- 2026-11-26 Start, alle vier Specs auf InUmsetzung.
- 2026-11-26 Lagerbestellung-Hinweis umgesetzt (300dbf25): Build 0 Fehler, Tests 3794/1 skip + 848; TS-207, Index 6ee2a764.
- 2026-11-26 qa-agent: **Testbereit** (b7704475), keine Blocker/Sollte, H-1..H-3 belassen; DEV AKE :8081 + IDEAL :8082 v1.99.86;
  207.8 (Stücklisten-Schnellerfassung) und 207.9 (Druck) nicht im Browser – Manual-UAT. Foto Entwurf #90 an Gerald.
- 2026-11-26 Brain: Changelog [[2026-11-26-v1-99-86-lagerbestellung-hinweis-fehlteil]], feature-map v1.99.86. Teil 1 gestartet.
- 2026-11-26 Teil 1 umgesetzt (e67e9f6f): Build 0 Fehler, Tests 3799/1 skip + 848; TS-205, Index 22b6f81a.
- 2026-11-26 qa-agent Teil 1: **Testbereit** (9da4b0af), keine Blocker/Sollte; H-1 „ohne FA“-Badge verschwindet mit ausgeblendeter
  Artnr-Spalte (Altbestand seit v1.99.41, Backlog offen), H-2 bewusst; DEV AKE + IDEAL v1.99.87; ◩ im PDF auf DEV ok.
  Fotos BA-12 (QA) + BA-7 (echt) an Gerald. Brain: Changelog [[2026-11-26-v1-99-87-beschichtungsauftrag-druck-summiert]], feature-map.
  Teil 2 gestartet.
- 2026-11-26 Gerald zu den Fotos: „Warum 6 (4 Positionen)?“ → Freigabe-Nachtrag N-1 Teil 1 „Nur Teilesumme“ (563cbc4f).
- 2026-11-26 Teil 2 umgesetzt (5d7639dc): Build 0 Fehler, Tests 3820/1 skip + 848; TS-208. N-1 eingebaut (a89d98cc, v1.99.88,
  Tests grün), Index 0e9d7d01. qa-agent für Teil 2 + N-1 gemeinsam gestartet.
- 2026-11-26 21:0x QA Teil 2 + N-1 abgebrochen (von außen gestoppt, kein Ergebnis) nach Fix-Commit ebb16bd7 → nicht Testbereit;
  QA neu gestartet (prüft 5d7639dc + a89d98cc + ebb16bd7).

## Fuer spaetere Confluence-Uebernahme (geaenderte Kapitel)

## Paket

## Offen fuer Gerald (Schranke 2)

- v1.99.87: Manual-UAT TS-205 am Testsystem (Zahnrad 205.4/205.5, Browser-Druckvorschau 205.6, ◩ im Server-PDF 205.7);
  Entscheidung H-1 („ohne FA“ bei ausgeblendeter Artnr-Spalte) – Backlog ja/nein.
- v1.99.86: Manual-UAT TS-207 inkl. 207.8 Stücklisten-Schnellerfassung und 207.9 Druck-Liefertermin.

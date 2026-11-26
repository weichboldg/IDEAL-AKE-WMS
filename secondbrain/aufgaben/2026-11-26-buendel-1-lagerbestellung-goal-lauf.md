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
| [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]] | Spec | – | 1.99.88 | 208 | Testbereit | 5d7639dc, ebb16bd7 (QA-Fix); N-1 Teil 1 a89d98cc | |
| [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-3-spec]] | Epic (A+B) | – | 1.99.89 | 209 | InUmsetzung (A erledigt, B läuft) | 98ea06fa | |

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
- 2026-11-26 QA Teil 2 + N-1: **Testbereit** (90bc96e5), Build 0 Fehler, Tests 3820/1 skip + 848, DEV AKE + IDEAL v1.99.88,
  N-1 belegt (AKE BA-6 „1035“, IDEAL BA-12 „17“ ohne Klammer), Publish 1.99.88 (ebb16bd7). Befunde F-1..F-3 ohne Blocker.
- 2026-11-26 Fotos Leitstand (ohne/mit Sub-FAs, Hinweis) + BA-12-Druck an Gerald; Brain Changelog
  [[2026-11-26-v1-99-88-leitstand-nur-hauptfa]] + feature-map. Teil 3 Etappe A gestartet.
- 2026-11-26 Teil 3 Etappe A erledigt (98ea06fa, v1.99.89, TS-209): Build 0 Fehler, Tests 3843/1 skip + 848 (1 Reparaturrunde:
  Wächter-Listen um neues Partial). Etappen-Tabelle 676c3c5f. Etappe B gestartet.

## Folgearbeit aus Befunden (Gerald 2026-11-26: „alle Befunde als Backlog, nach der Schluss-QA gleich Spec“)

- [[2026-11-26-leitstand-kaskaden-zaehler-ohne-hauptfa]] (F-1), [[2026-11-26-leitstand-spaltenfilter-glas-zukauf-ohne-wirkung]] (F-2),
  [[2026-11-26-leitstand-hinweis-vk-spalte-breit]] (F-3), [[2026-11-26-beschichtungsauftrag-druck-ohne-fa-und-matchcode-abweichung]] (Teil 1 H-1/H-2),
  [[2026-11-26-lagerbestellung-artikelnummer-varianten]] (Spec 1 H-1..H-3), [[2026-11-26-kommissionierauftrag-filter-springt-auf-bom]] (Teil 3 Zusatzfund A).
- **Nach der Schluss-QA (Teil 3) dieses Laufs: /spec für alle fünf** (Entwürfe nach specs/entwurf, Schranke 1 bleibt).

## Fuer spaetere Confluence-Uebernahme (geaenderte Kapitel)

## Paket

## Offen fuer Gerald (Schranke 2)

- v1.99.87: Manual-UAT TS-205 am Testsystem (Zahnrad 205.4/205.5, Browser-Druckvorschau 205.6, ◩ im Server-PDF 205.7);
  H-1/H-2 als Backlog angelegt (Gerald 2026-11-26).
- v1.99.88: Manual-UAT TS-208 (nicht auf DEV: 208.2 Massenfreigabe, 208.3 Artikelfilter Sub-FA-Artikel, 208.4 Kaskade+CSV,
  208.5 Seite 2, 208.6b W3 + Speicherfehler, 208.7 Banner, 208.8 Hilfe, Rechte-Negativfälle), TS-205.1/.5/.11 (N-1);
  F-1/F-2/F-3 als Backlog angelegt (Gerald 2026-11-26).
- v1.99.86: Manual-UAT TS-207 inkl. 207.8 Stücklisten-Schnellerfassung und 207.9 Druck-Liefertermin.

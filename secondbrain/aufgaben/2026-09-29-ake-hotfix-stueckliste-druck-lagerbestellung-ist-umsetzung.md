---
typ: notiz
spec: "[[2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist"
branch: "feature/2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist"
---
# Umsetzung: AKE-Hotfix v1.30.1 — Stuecklisten-Druck 404.15 + Lagerbestellung IST

Spec [[2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist-spec]] · **eigener kleiner Worktree aus
`main`** (Zielzweig main, NICHT das Buendel). Folgeschritt nach Abnahme + Merge in main: Vorwaerts-Merge
main → `feature/2026-08-07-ideal-teile-1-5` (eigener Schritt, nicht in diesem Lauf).

## Pre-Flight-Verdikt (2026-09-29)

- status Freigegeben, open_questions [], freigabe_entscheidung/_von/_am gesetzt; Konflikte 1+2 im
  Freigabe-Nachtrag entschieden (SQL/Einmalig/, Fehlteil-Zeilen ausgenommen).
- Spec lag trotz status Freigegeben noch in `specs/entwurf/` — beim Start nach `specs/freigegeben/` verschoben.
- main-Stand: AppVersion 1.30.0 → **1.30.1**; keine Migration.

## Stand

- [x] F: Bindungstest `int?[]` mit `["5","","7"]` → `[5,null,7]` (echte Formularbindung, `NullableIntArrayBindingTests`)
- [x] Teil 2 B: CloseAsync `decimal?` + Pflichtpruefung, Controller Close/PrintAndClose `int?[]`, Tests
- [x] Teil 2 A/C/D: Details.cshtml (frontend-design: Inline-Alert statt Modal), Autosave `''`, Modal/normalize/fillSollAsIst weg, Placeholder weg
- [x] Teil 1: PrintBom GET + POST, Bom.cshtml Formular-Submit
- [x] SQL/Einmalig/ Reset-Skript angelegt (NICHT ausgefuehrt — Deploy-Schritt des Menschen)
- [x] v1.30.1, Changelog, TS-5.11, TS-18.10, Brain (fallstricke.md, ADR 0015, feature-map, Index bereits nachgezogen)

## QA (2026-09-29)

**Testbereit.** Build 0 Fehler, Test IdealAkeWms.Tests 1107/1108 gruen (1 vorbestehend uebersprungen),
IDEALAKEWMSService.Tests 197/197 gruen, kein neuer Migrations-Eintrag. Alle 17 AK eingeordnet
(automatisiert oder Manual-UAT). Details, Abweichungs-Bewertung und manuelle Test-Checkliste siehe
Abschnitte „QA-Nachweis (2026-09-29)" und „Manuelle Test-Checkliste (Schranke 2)" in der Spec.

**Code-Review-Befund (nicht blockierend, siehe Spec Abschnitt 6):** `PrintAndClose` sichert bei
Ablehnung durch die Pflichtpruefung — anders als `Close` — keinen Zwischenstand per
`SaveProgressAsync`. Geringe Tragweite (kein Seiten-Reload bei Ablehnung, DOM behaelt die Werte),
aber Randfall (Browser-Absturz/Parallelzugriff) verliert Eingaben serverseitig. Entscheidung vor dem
Merge: jetzt nachziehen oder als Folgeticket. Siehe Checklisten-Punkt 21 in der Spec.

Naechster Schritt: Schranke 2 (Mensch — manueller Test am Testsystem, dann Merge in `main`).
Vorwaerts-Merge ins Buendel (`feature/2026-08-07-ideal-teile-1-5`) ist ein eigener Folgeschritt danach.

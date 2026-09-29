---
typ: notiz
spec: "[[2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist-spec]]"
status: InUmsetzung
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

- [ ] F: Bindungstest `int?[]` mit `["5","","7"]` → `[5,null,7]` (ZUERST)
- [ ] Teil 2 B: CloseAsync `decimal?` + Pflichtpruefung, Controller Close/PrintAndClose `int?[]`, Tests
- [ ] Teil 2 A/C/D: Details.cshtml (frontend-design), Autosave `''`, Modal/normalize/fillSollAsIst weg, Placeholder weg
- [ ] Teil 1: PrintBom GET + POST, Bom.cshtml Formular-Submit
- [ ] SQL/Einmalig/ Reset-Skript (NICHT ausfuehren)
- [ ] v1.30.1, Changelog, TS-5.11, TS-18.10, Brain

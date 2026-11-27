---
type: changelog
version: 1.99.56
date: 2026-11-23
---
# v1.99.56 — Beschichtungsauftrag: Status-Knopf speichert Eingaben mit

Spec [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-3-spec]], Lauf [[2026-11-23-goal-lauf-mengenfelder-tabellen-beschichtung]]. Commit `01a6429a`. Keine Migration, kein SQL, nur Web (Version in beiden `AppVersion.cs`). TS-181. Nicht gemergt.

## Kernpunkte

- Status-Knöpfe (außer Storno) senden bei bearbeitbarem Auftrag das Positionsformular mit (`form="entwurf-form" name="ziel"`); `Bearbeiten(…, ziel)` speichert, dann Statuswechsel mit der neuen RowVersion.
- Teilerfolg: „Gespeichert.“ + Warnung. Druck/PDF fragen bei ungespeicherten Positionen nach (`beschichtung-bemerkung.js`).

## Deploy-Risiken

- RowVersion auf SQL Server von QA auf DEV belegt; Rückfragen im Browser im UAT.

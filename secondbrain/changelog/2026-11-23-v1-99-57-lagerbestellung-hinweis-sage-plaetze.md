---
type: changelog
version: 1.99.57
date: 2026-11-23
---
# v1.99.57 — Lagerbestellung: Hinweis nennt Bestand auf nicht wählbaren Plätzen

Spec [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-4-spec]], Lauf [[2026-11-23-goal-lauf-mengenfelder-tabellen-beschichtung]]. Commit `8bac07bd`. Keine Migration, kein SQL, nur Web (Version in beiden `AppVersion.cs`). TS-182. Nicht gemergt.

## Kernpunkte

- `WarehousePickingController.VorschlagHinweis(…, erlaubteIds)`: liegt Bestand nur auf nicht wählbaren Plätzen (v. a. Sage-Plätze ohne Sage-Buchung), nennt der Hinweis bis zu drei Plätze mit Menge (+n) statt „Kein Bestand“. Regel (`QuellLagerplatzVorschlag.Waehle`) unverändert.
- Dazu QA der Laufzeit-Nachbesserung `b7e2d5a4` (v1.99.53, TS-178.12).

## Deploy-Risiken

- Keine. Reiner Anzeigetext.

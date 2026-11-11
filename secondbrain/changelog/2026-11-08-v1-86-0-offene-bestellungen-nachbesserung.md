---
type: changelog
version: 1.86.0
date: 2026-11-08
---
# v1.86.0 — Offene Bestellungen: Nachbesserung nach Sichtprüfung (B1)

Spec [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]], Lauf [[2026-11-08-buendel-2026-11-08-goal-lauf-2]]. Commit `405e9af5`. Keine Migration.
TS-116.8/116.9.

## Kernpunkte

- Überfällig = rot (`table-danger`) mit Legende, sonst neutral; Knopf gefüllt (Kontrast).
- Sperre „Einheit abweichend“ auch bei Basis- ≠ Lagereinheit (EK003604: PKG/Stk/PKG).
- Hinweis an der Zeile, wenn der Sage-Platz einem nicht buchbaren WMS-Platz zugeordnet ist (`HR;0;0;0`).

## Deploy-Risiken

- Neue Sperre wirkt erst nach dem nächsten Abgleich `Sync:EinkaufBestellungenEnabled` (AKE-DEV: 31 statt 7 gesperrte Positionen).

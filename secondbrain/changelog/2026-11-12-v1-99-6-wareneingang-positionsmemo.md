---
type: changelog
version: 1.99.6
date: 2026-11-12
---
# v1.99.6 — Wareneingang: Positionsmemo-Feld

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commit `23ea96ca` + QA-Korrektur `67322b85` (auf v1.99.5 `c155b1b2`). Spec
[[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]], Abschnitt „Freigabe-Nachtrag 2026-11-12: Positionsmemo-Feld“. Nicht gemergt,
nicht gepusht.

## Kernpunkte

- „Wareneingang zu Bestellung“: je Position ein Feld „Positionsmemo“ (höchstens 200 Zeichen, der Server prüft), gespeichert als `StockMovement.Note`
  (getrimmt, leer = null), immer – auch ohne Sage-Wareneingang. Anzeige in der Bewegungshistorie und, nur bei Sage-WE, in „Wareneingänge an Sage“
  (Spalte mit Filter).
- Sage-Wareneingang: `Artikelmemo` = `<Eingabe> | WE#<id># LS … Platz …`; leer = nur Kennung wie v1.99.5 (`SageWareneingangPayloadBuilder.PositionsMemo`).
- Die Sage-Lagerbuchung liest `Note` nicht – dort keine Änderung.
- Testszenario TS-116.19.

## Deploy-Risiken

Keine Migration (Spalte `Note` seit SQL/57), keine Einstellung, kein Nachlauf-Eintrag. Web und Dienst deployen.
Offen: ob Sage lange Positions-Memos vollständig speichert (TS-116.19, Gerald).
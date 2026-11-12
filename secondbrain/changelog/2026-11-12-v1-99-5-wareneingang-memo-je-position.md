---
type: changelog
version: 1.99.5
date: 2026-11-12
---
# v1.99.5 — Sage-Wareneingang: Memo je Position

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commit `c155b1b2` (auf v1.99.4 `bd9b58d0`). Spec
[[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]], Abschnitt „Freigabe-Nachtrag 2026-11-12“. Nicht gemergt, nicht gepusht.

## Kernpunkte

- Jede Position des Sage-Wareneingangs (ELL) bekommt `Artikelmemo` = `WE#<Receipt-Id># LS <Lieferschein> Platz <Lagerkennung>`;
  ohne Lieferschein entfaellt „LS …“. Erzeugt an einer Stelle: `SageWareneingangPayloadBuilder.PositionsMemo`.
- Zweiter Schreibtest (freigegeben) am AKE-Test: Beleg 2027-398676 aus Wareneingang #7; Rueckbau durch Gerald.
- Testszenario TS-116.18 (Memo in Sage sichten, nur Gerald/Einkauf).

## Deploy-Risiken

Keine Migration, keine Einstellung, kein Nachlauf-Eintrag. Wirksam ist nur der Dienst (Payload); Web traegt Version und Changelog.

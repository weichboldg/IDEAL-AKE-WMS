---
type: changelog
version: 1.76.0
date: 2026-11-07
---
# v1.76.0 — Bestandsübersicht: FA aus dem Artikel-QR nur mit `QrMitFaNummer`

Spec [[2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-spec]] (Bug [[2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-bug]]),
Lauf [[2026-11-07-buendel-2026-11-07-goal-lauf]]. Commit `2fafd073`. **Keine Migration**, nur Web (Dienst nur Version). TS-101.22–101.25.

## Kernpunkte

- Variante A: `StockOverviewController` setzt `ViewBag.QrMitFaNummer` wie Ein-/Aus-/Umbuchung, die View liest es statt `data-qr-fa-enabled="true"`.
  `data-fa-target` bleibt → bei ausgeschalteter Einstellung leert ein Artikel-Scan das FA-Feld. JS unverändert.
- Tote `ViewBag.QrMitFaNummer`-Zuweisungen im Sammel-Wareneingang entfernt (View las sie nie).
- P1 (Gerald 2026-11-07): `QrMitFaNummer` ist an AKE und IDEAL an → im Betrieb keine sichtbare Änderung.

## Deploy-Risiken

- Keine. Wo `QrMitFaNummer` aus ist, übernimmt die Bestandsübersicht keine FA mehr aus dem Artikel-QR.

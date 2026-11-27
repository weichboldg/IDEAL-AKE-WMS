---
type: changelog
version: 1.99.4
date: 2026-11-12
---
# v1.99.4 — Wareneingang Teil 2: Sage-Wareneingangsbeleg (ELL) und Lieferschein-Nr.

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commits `f45412d4`, `aa4655f8`, `f7dc750e`, `053cb28d`, `a449e814`, `0d265005`, `2d09489a`. Spec
[[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]]. Nicht gemergt, nicht gepusht.

## Kernpunkte

- Je Buchung der Maske „Wareneingang zu Bestellung“ ein Sage-Wareneingang (ELL, Belegübernahme) ueber `StandardBelegService`, Schalter
  `SageWareneingangBelegAktiv` (Default aus, AKE); diese Wareneingaenge nie in der Lagerbuchungs-Queue (Decorator + Nacherfassung); Platzregel.
- Lieferschein-Nr. (Lieferant) in der Maske, Pflicht per AppSetting `WareneingangLieferscheinPflicht`.
- Liste „Wareneingänge an Sage“, „Erneut senden“ von Hand (Variante B); Auto-Retry (E5) blockiert bis P-W9.
- Rest-Regel ohne doppelten Abzug uebergebener Wareneingaenge; `SyncedAt` vor dem Lesen.
- Schreibtest am AKE-Test 2026-11-12: Beleg 2027-398675 (Rueckbau Gerald/Einkauf).

## Deploy-Risiken

- **Migration SQL/143** (additiv, 7 Spalten an `PurchaseOrderReceipts`); das Update-Skript spielt sie als Schema-Eintrag ein.
- `SageWareneingangBelegAktiv` schreibt nach Sage: erst nach Abnahme TS-116.14 einschalten; Einkauf informieren (Manifest `v1.99.4-EinkaufWe`).

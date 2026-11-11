---
type: changelog
version: 1.87.0
date: 2026-11-08
---
# v1.87.0 — Lagerbuchungen: FA-Feld verknüpft, geprüft, verlinkt (Epic A5)

Spec [[2026-11-08-lagerbuchungen-fa-feld-und-verlinkung-spec]], Lauf [[2026-11-08-buendel-2026-11-08-goal-lauf-2]]. Commits `f38e82ac` (E1),
`9a435e81` (E2), `96f4dd9b` (E3), `7ab2fcb5` (E4). **Migrationen:** `SQL/109_AddStockMovementProductionOrderId.sql` (Schema, additiv),
`SQL/110_BackfillStockMovementProductionOrderId.sql` (Daten, Default Trockenlauf). TS-115.

## Kernpunkte

- `StockMovements.ProductionOrderId` (FK SetNull) neben dem Text; eine Regel `IProductionOrderRepository.FindForBookingAsync` (SQL/110 spiegelt sie).
- Prüfkarte unter dem FA-Feld (Wareneingang, Sammel-Wareneingang, Warenausgang, Umbuchung), Vorschläge, Warnung nach dem Buchen.
- Einstellung `FaPflichtBuchungsarten` (MultiChoice, Default leer = keine Pflicht).
- Historie/Bestandsübersicht: FA-Link + Status (`_FaLink`), FA-Leserecht an einem Ort (`FaBuchungsText.CanViewProductionOrdersAsync`).
- Bestandsübersicht: Artikel-QR übernimmt nie einen FA (ersetzt v1.76.0-Bindung an `QrMitFaNummer`).

## Deploy-Risiken

- DB-Backup → SQL/109 → Web **und** Dienst (gemeinsames EF-Modell) → SQL/110 Trockenlauf, Ergebnis ablegen → scharf → zweiter Lauf 0.
- Offen nach QA: Q-1 (Kartentext „Buchen ist möglich“ trotz Pflicht), Q-2 (Link-Kontrast in gelber Karte 4,1:1 → `alert-link`).

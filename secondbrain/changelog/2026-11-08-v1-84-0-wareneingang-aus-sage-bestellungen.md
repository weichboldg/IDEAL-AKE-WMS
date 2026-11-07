---
type: changelog
version: 1.84.0
date: 2026-11-08
---
# v1.84.0 — Wareneingang zu Sage-Bestellungen (Epic E1–E4)

Spec [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]], Lauf [[2026-11-08-buendel-2026-11-08-goal-lauf]]. Commits `cf1f8547` (E1),
`0fdfa39d` (E2), `3bba01f6` (E3), `0a941d63` (E4), QA-Fixes `d9621029` (Q-1/2/4/5/6), `26e6a278` (Q-8). **Migrationen:** `SQL/112_AddSagePurchaseOrderLines.sql`,
`SQL/113_AddPurchaseOrderReceipts.sql` (rein additiv). TS-116. E5 (Sage-Wareneingangsbeleg) = Folge-Spec.

## Kernpunkte

- Dienst: `EinkaufBestellungenSyncService` (Gate `Sync:EinkaufBestellungenEnabled`, Default aus), liest offene `EBB`-Positionen aus SData
  `EinkaufPositionenInfo` in den Cache `SagePurchaseOrderLines` (Vollersatz, Empty-Guard), Lauf „Offene Bestellungen (Sage)“.
- Web: `PurchaseOrdersController` – Vorschau „Offene Bestellungen“ (X Wochen, AppSetting `OffeneBestellungenVorschauWochen`, Default 2) und
  „Wareneingang zu Bestellung“ (Einbuchung je Position über `IStockMovementRepository.AddAsync`, `PurchaseOrderReceipt` an der Bewegung).
  Feature-Gate `WareneingangBestellungAktiv` (Default aus, 404).
- Entscheidung E0-Blocker: nur EBB, Basiseinheit, abweichende Einheit nicht buchbar, Kopf aus Positionen, „Lieferant gesperrt“ als Warnung.

## Deploy-Risiken

- Doppelbestand, wenn der Einkauf in Sage zusätzlich einen WE-Beleg **mit** Lagerbuchung erfasst (Rückfrage 1): Einkauf vorher informieren.
- Reihenfolge: SQL/112, SQL/113 → Web → Dienst; dann AKE `Sync:EinkaufBestellungenEnabled` an, P-W6 prüfen, erst dann `WareneingangBestellungAktiv`.

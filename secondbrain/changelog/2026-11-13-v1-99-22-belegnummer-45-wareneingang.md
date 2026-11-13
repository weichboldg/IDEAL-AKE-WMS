---
type: changelog
version: 1.99.22
date: 2026-11-13
---
# v1.99.22 — Belegnummer 45 Zeichen, Wareneingang zu Bestellung: Spalte „Überlieferung“, Hinweis auf offene Bedarfe

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commit `4689922c`. Spec [[2026-11-08-einkauf-bestellvorschlaege-spec]] (Freigabe-Nachtrag 3
2026-11-13 18:07–18:12), Backlog [[2026-11-13-belegnummer-laenge-45]], [[2026-11-12-lagerbestellungen-und-bedarfsmeldungen-gespraech]] Punkt 2.
/goal-Lauf [[2026-11-13-buendel-v1-99-19-goal-lauf]]. Nicht gemergt.

## Kernpunkte

- `Belegnummer` 20 → 45 in `SagePurchaseOrderLines`, `PurchaseOrderReminders`, `PurchaseOrderReceipts`; `SageWeBelegnummer` 30 → 45 (Sage-Views mit
  `BelegnummerFormatiert` sind varchar(45), `ake_TEST2026` lesend belegt). Konstante `SagePurchaseOrderLine.BelegnummerMaxLaenge = 45` auch für
  „Klären → als übergeben markieren“ und die Kürzung im `SageWareneingangService`.
- Wareneingang zu Bestellung: eigene Spalte „Überlieferung“ (Häkchen zentriert, 44 px Trefferfläche, Screenreader-Beschriftung), Zeilen mittig;
  Feldnamen unverändert, Buchen unverändert.
- Je Position Hinweis „Offener Bedarf zu diesem Artikel“: Lagerbestellungen (hinter `LagerbestellungAktiv`, Link `/WarehousePicking/Details/{id}`)
  und Bedarfsmeldungen (hinter `BestellungenAktiv`, Link `/PartRequisitions?searchTerm=<Artikel>` – keine Detailseite); nur Anzeige.

## Migration / SQL

`20261113172929_WidenBelegnummer45`, **SQL/149** (Guard `COL_LENGTH < 90`, Index `IX_SagePurchaseOrderLines_Belegnummer` drop/recreate);
nicht destruktiv, FreshInstall zwei Stellen, Manifest `SQL/149`.

## Deploy-Risiken

DB vor Web und Dienst; Index-Neuaufbau auf `SagePurchaseOrderLines` (kleine Tabelle).

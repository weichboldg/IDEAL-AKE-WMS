---
type: changelog
version: 1.99.53
date: 2026-11-23
---
# v1.99.53 — Lagerbestellung: Entnahme-Platz + Verbrauchsbuchung beim Abschließen

Spec [[2026-11-23-lagerbestellung-verbrauchsbuchung-bei-lieferung-spec]], Lauf [[2026-11-23-goal-lauf-dashboard-we-lagerbestellung]].
Commits `f7b26ebc`, QA-Fix `a93bd910`. **SQL/166** (Migration `20261123124036_AddStockMovementWarehouseRequisitionItem`, FreshInstall an
beiden Stellen). Neue Einstellung `LagerbestellungVerbrauchsbuchungAktiv` (Default aus). Manifest: Schema SQL/166, Einstellung, Info. TS-178.
Nicht gemergt.

## Kernpunkte

- Lager-Detailseite: „Entnahme aus“ je Position, Vorschlag wie Kommissionierung (`QuellLagerplatzVorschlag.Waehle`, aus `PickingController`
  verschoben). Sage-Plätze nur, wenn die Entnahme an Sage geht ([[fallstricke]] §62).
- Abschließen / Drucken + Abschließen: WMS-Ausbuchung über „Ist minus schon gebucht“, verknüpft über `StockMovements.WarehouseRequisitionItemId`;
  Abschluss + Buchungen in einer Transaktion; Status-Guard (nur Abgeschickt/Teilgeliefert). Fehlbestand wie Warenausgang
  (`AusbuchungBestandsregel`). Sage: Entnahme über bestehende Queue, Dienst unverändert. DUMMY/ohne Artikel nie, Glas wie Lager.
- QA-Fix: gleiche Artikel/Platz in mehreren Positionen werden für die Bestandsprüfung summiert.

## Deploy-Risiken

- Schema-Änderung (nullable Spalte + FK + Index, nicht destruktiv).
- **Einschalten von `LagerbestellungVerbrauchsbuchungAktiv`:** Das Lager bucht diese Lieferungen heute von Hand in Sage aus – die Handbuchung
  endet am Einschalttag, sonst doppelte Entnahme. Einschalten entscheidet Gerald nach Deploy.
- Verhaltensänderung auch bei Schalter aus: bereits abgeschlossene/stornierte Listen lassen sich nicht erneut abschließen (Warnung).

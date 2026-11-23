---
type: changelog
version: 1.99.52
date: 2026-11-23
---
# v1.99.52 — Wareneingang zu Bestellung: Bedienung nachgeschärft (AKE)

Spec [[2026-11-23-wareneingang-zu-bestellung-bedienung-spec]], Lauf [[2026-11-23-goal-lauf-dashboard-we-lagerbestellung]].
Commits `e51e80c9`, `9937d357`. Kein SQL, keine Migration, keine Einstellung. TS-177. Nicht gemergt.

## Kernpunkte

- Bestellnummer ohne Jahr: reine Ziffern = Endungssuche `-<Eingabe>` (Sage-Geschäftsjahr ≠ Kalenderjahr); mehrere Treffer = Meldung mit Liste.
- Positionstabelle gestreift + Hover, Fokus-Zeile über `--bs-table-bg-state` + Balken (`table-danger` bleibt), [[fallstricke]] §61.
- Zahnrad: `ColumnDefinitions.PurchaseOrderReceive`, acht Pflichtspalten gesperrt.
- Ziel-Lagerplatz: Sage-Platz der Position hat Vorrang, sonst Hauptlagerplatz (`Article.PrimaryStorageLocationId`) ohne Bestandsbedingung;
  bei Sage-WE nur Sage-Platz mit Sage-Buchung. Neue Ctor-Abhängigkeit `IArticleRepository` im `PurchaseOrdersController`.

## Deploy-Risiken

Keine am Buchen. Web + Dienst (Dienst nur Versionsnummer).

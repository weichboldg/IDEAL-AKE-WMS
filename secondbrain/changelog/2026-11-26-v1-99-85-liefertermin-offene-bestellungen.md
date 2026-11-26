---
type: changelog
version: 1.99.85
date: 2026-11-26
---
# v1.99.85 — Liefertermin offener Bestellungen: Artikelinfo + Lagerbestellung-Details

Spec [[2026-11-26-liefertermin-offene-bestellungen-artikelinfo-lagerbestellung-spec]], Lauf-Notiz
[[2026-11-26-v1-99-85-liefertermin-offene-bestellungen]]. Worktree `feature/2026-08-07-ideal-teile-1-5`, Commits `1f11fa48`
(Umsetzung) + `b3a3809f` (Druck-Filter) + `fa54b418` (Layout „überfällig“). **Keine Migration, keine neue Einstellung.** TS-206. Nicht gemergt.

## Kernpunkte

- Artikelinfo: Karte „Offene Bestellungen“ aus dem Cache `SagePurchaseOrderLines`; Rest = Sage-Offen minus im WMS erfasste,
  noch nicht übergebene Wareneingänge (`ErfasstJeBelPos.Rest`, dieselbe Regel wie Wareneingang zu Bestellung). Nur Rest > 0,
  erfasste Positionen zählt die Fußzeile. Sortiert nach Termin, ohne Termin zuletzt.
- Lager: Eingehende Listen → Details: Spalte „Liefertermin“ (`ColumnDefinitions.WarehousePickingDetails`, DefaultHidden),
  `WarehousePickingController.LieferterminZelle` (nächster Termin, „(+N)“, „überfällig“, „ohne Termin“, „—“), Link zur Artikelinfo
  nur mit Stammdaten lesen. Feste Zahl Abfragen je Seite (Stand, Positionen, Erfasst).
- Druck: zeigt Liefertermin/Fräsen-Pfad nicht, filtert aber mit demselben Zelltext wie der Bildschirm (vorher leerer Text → leerer Druck).
- Cache leer (IDEAL ohne `Sync:EinkaufBestellungenEnabled`): Artikelinfo-Hinweis, keine Spalte.

## Deploy-Risiken

- Nur Web (Dienst nur `AppVersion`). Wirkung am Standort hängt am Bestell-Abgleich – IDEAL-Prod prüfen (P-1).
- Offener Befund B-2: Sortierung nach „Liefertermin“ wird im Druck ignoriert (Pos-Reihenfolge), wie Fräsen-Pfad.

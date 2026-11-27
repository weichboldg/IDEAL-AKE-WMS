---
type: backlog
typ: feature
created: 2026-11-23
status: erledigt
spec: "[[2026-11-23-lagerbestellung-verbrauchsbuchung-bei-lieferung-spec]]"
quelle: Gerald 2026-11-23 (Sammelauftrag „bitte folgende features planen“, Punkt 5 – nachgereicht)
modul: Lagerbestellungen (WarehouseRequisitions) / Sage-Lagerbuchungen
bezug: "[[2026-11-23-wareneingang-zu-bestellung-bedienung]]"
---
# Lagerbestellungen: Lager wählen + Verbrauchsbuchung bei Lieferung

Wortlaut: *In den Lagerbestellungen dem Lager die Möglichkeit bieten, das Lager auszuwählen, und wenn ein Artikel
geliefert wird, eine Buchung „Verbrauchsbuchung“ auslösen. Lagerplatz soll der Hauptlagerplatz vorgeschlagen werden.
Auch mit SAGE-Übergabe.*

Hinweise für die Spec:
- Einstieg im Bündel-Worktree `2026-08-07-ideal-teile-1-5`: `Controllers/WarehouseRequisitionsController.cs`,
  `Controllers/Api/WarehouseRequisitionsApiController.cs`, `WarehousePickingController.cs`.
- „Verbrauchsbuchung“ gibt es als Buchungsart im WMS-Code bisher nicht (Suche „Verbrauch“ ohne Treffer) – belegen,
  welche Bewegungsarten `StockMovement` kennt und wie die Sage-Übergabe heute läuft (Queue + `SageBookingWorker`,
  Spec Sage-Lagerbuchungen v1.28.0, Sage-WE-Beleg). Welche Sage-Belegart/SData-Ressource eine Verbrauchsbuchung ist:
  nicht annehmen, als Prüfabfrage formulieren.
- „Lager auswählen“: klären, ob Lager = Lagerort/Standort oder Lagerplatz, und wer „das Lager“ ist (Rolle).
- Hauptlagerplatz-Vorschlag: dieselbe Quelle wie im Wareneingang (Spec
  [[2026-11-23-wareneingang-zu-bestellung-bedienung-spec]]) – eine Stelle, nicht zweimal bauen.
- Fragen: Verbrauch = Zugang + sofortiger Abgang (Netto 0) oder nur Abgang? Kostenstelle/FA-Bezug? Teil- und Überlieferung?

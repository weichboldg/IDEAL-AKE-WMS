---
type: changelog
version: 1.18.0
date: 2026-05-29
---
# v1.18.0 — Lagerbestellungen: Teilgeliefert + Fehlteile + Drucken-und-Abschliessen

Anlass: der Real-Welt-Fall „Bestellung wurde teilweise geliefert" war im Submitted/Closed-Workflow
nicht abbildbar, und Fehlteile wurden nirgends erfasst — keine Auswertung moeglich.

- Neuer Status `PartiallyDelivered` — **kein End-Status**: solche Bestellungen bleiben in der
  Lager-Worklist bearbeitbar, der Status wird beim erneuten Close neu abgeleitet.
- Fehlteil-Flag je Position plus kombinierter Button „Drucken und Abschliessen".
- `GetForWarehouseAsync` nimmt jetzt ein `WarehouseRequisitionStatus[]` und zeigt ohne expliziten
  Filter **beide** offenen Status; der Offen-Badge zaehlt entsprechend.
- Neue Fehlteile-Liste + „Meine Fehlteile"-Karte in der Bestelluebersicht.

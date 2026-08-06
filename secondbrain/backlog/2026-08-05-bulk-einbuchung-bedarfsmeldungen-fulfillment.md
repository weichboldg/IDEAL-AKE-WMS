---
type: backlog
title: Mehrfach-Einbuchung — Bedarfsmeldungen-Fulfillment pro Zeile
status: neu
created: 2026-08-05
herkunft: "Freigabe-Antwort 3 zu [[2026-08-05-wms-bugs-improvements-teil-2-spec]] (dort bewusst zurueckgestellt / out-of-scope)"
---

# Mehrfach-Einbuchung — Bedarfsmeldungen-Fulfillment pro Zeile

Bewusst zurueckgestellt bei der Umsetzung der Mehrfach-Einbuchung (Teil 2,
[[2026-08-05-wms-bugs-improvements-teil-2-spec]], Freigabe-Antwort 3 „lassen wir bewusst hinten —
als Idee vormerken").

## Kontext
Die Einzel-Einbuchung (`StockMovementsController.Inbound`) verknuepft eine Einbuchung optional mit
offenen **Bedarfsmeldungen** (`fulfilledRequisitionIds` → `IPartRequisitionRepository.FulfillAsync`,
sichtbar nur bei aktivem `BestellungenAktiv`-Toggle). Die neue Seite
`/StockMovements/InboundBulk` bietet dieses Fulfillment **nicht** — pro Zeile fehlt der Platz und
die UX-Klaerung, ob je Zeile eigene Bedarfsmeldungen waehlbar sind.

## Idee / offene Punkte
- Pro Bulk-Zeile die offenen Bedarfsmeldungen des jeweiligen Artikels anzeigen und einzeln
  als erfuellt markieren koennen (analog Einzel-Einbuchung, aber je Zeile).
- Alternativ: nach dem Bulk-Buchen eine Sammel-Ansicht „diese Artikel hatten offene Bedarfe".
- UX gegen Bildschirmplatz am Handscanner abwaegen.

Kein Blocker fuer Teil 2; eigenstaendig spezifizierbar, sobald Bedarf besteht.

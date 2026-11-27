---
type: backlog
typ: bug
status: in-spec
created: 2026-11-26
quelle: "Gerald im Chat 2026-11-26: alle Befunde als Backlog, Spec nach der Schluss-QA von [[2026-11-26-buendel-1-lagerbestellung-goal-lauf]]"
standort: AKE, IDEAL
---
# Lagerbestellung: Artikelnummer in Groß/Klein- oder Leerzeichen-Varianten doppelt in einer Liste

Hinweise aus der QA von [[2026-11-26-lagerbestellung-hinweis-fehlteil-bereits-bestellt-spec]] (v1.99.86).

- H-3: Der Unique-Index `IX_WarehouseRequisitionItems_WarehouseRequisitionId_ArticleNumber` lässt `EK013128` und ` ek013128` in derselben
  Lagerbestellung zu (DEV #81). Prüfen, über welche Schreibwege (Erfassung, Stücklisten-Schnellerfassung, Import) ungetrimmte/kleingeschriebene
  Artikelnummern entstehen, und beim Schreiben normalisieren; Bestandsdaten per Prüfabfrage zählen.
- H-1/H-2 (geringfügig): Reihenfolge der Listennummern im Hinweis „bereits bestellt“ hängt an `Distinct()`-Reihenfolge und bei gleichem
  `ClosedAt` an keiner festen Sortierung — bei Gelegenheit explizit sortieren (ClosedAt, dann Id).

---
type: changelog
version: 1.13.0
date: 2026-05-15
---
# v1.13.0 — FA-Vervollstaendigung (ProductionOrder-Split Phase 4)

- Neues Modul `/FaCompletion` mit eigener Rolle `fa_completion`: je FA die Vormontage-Gruppen
  pflegen (anwendbar ja/nein, Merkmalsauspraegungen, Freitext-Specs).
- Nutzt die in v1.11.0 angelegten Satelliten-Tabellen (`ProductionOrderAssemblyGroups` +
  `...Specs`) — ab v1.22.0 durch `FaWorkSteps` abgeloest →
  [[0009-app-status-in-satelliten-tabellen-neben-sage-master]].
- Spec-CRUD mit Artikel-Auswahl, Audit-Felder bei jeder Aenderung.
- Ab v1.14.0 hinter dem Feature-Toggle `FaCompletionAktiv`.

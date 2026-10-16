---
type: changelog
version: 1.61.0
date: 2026-10-16
---
# v1.61.0 — Arbeitsgang-Stammdaten mit der Sage-View abgleichen

[[2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view-spec]], Umsetzung
[[2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view-umsetzung]]. **Direkt im Bündel**, Start `d3de9022` (v1.60.2),
Commits `6993c774`, `bc134c15`, `7042efcb`, `459cb7b5`, `9731b38a`, QA-Fix `8773ba4f`. **Migration `SQL/96`**
(`20261016090641_AddOperationConfigSourceAndSageMissingSince`).

**Warum:** Am IDEAL-Test standen 15 IDEAL-Kürzel unter „Nicht konfigurierte Kürzel“; die Sage-View
`vw_IDEAL_AKE_WMS_Arbeitsgaenge` ist der Arbeitsgang-Katalog, die Stammdaten wurden aber nur von Hand gepflegt.

## Kernpunkte

- Neuer Dienst `OperationConfigSyncService` (SyncLog `OperationConfigSync`), EIN Gate `Sync:OperationConfigSyncEnabled`
  (Default aus), View-Name `Sync:OperationConfigSyncViewName`, `Sync:OperationConfigSyncFremdsystemRelevant`. Nur anlegen;
  einzige Bestandsänderung Quelle `Manual` → `Sage`; nie überschreiben, nie löschen. Standortneutral.
- `OperationConfigs.Source` (Herkunft) + `SageMissingSince` (Zustand); Bestand `CreatedBy = 'system'` → `ExternalSystem`.
- Reiter *Arbeitsgänge*: Spalte Quelle, Badge/Hinweis „nicht in Sage“ (nur Schalter an, nie Fremdsystem), Zahnrad.
- `OperationConfigRepository.UpdateAsync` schreibt nur Pflegefelder (vorher `Update()` = alle Spalten).
- QA ohne kritische/wichtige Befunde; Build + 2036 Web- / 307 Service-Tests grün.
- [[fallstricke]] §24: `CreatedBy = 'system'` kein Seed-Beleg, `Update()` schreibt alle Spalten, Ausschlussliste-Fallback.

## Deploy-Risiken

Web + Dienst. `SQL/96` additiv, braucht `SQL/95` (gleiches Bündel). **P5 je Standort vor dem Deploy** (live P5b). Mit Defaults
keine Wirkung außer Spalte Quelle und Zahnrad. IDEAL: alte Zeilen erst **nach** dem ersten Lauf löschen (leere Tabelle → OSEON-Seed).

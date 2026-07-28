---
type: changelog
version: 1.15.1
date: 2026-05-27
---
# v1.15.1 — Aktivitaets-Protokoll auch fuer Non-Sync-Services

- `PartRequisitionEmailService`, `WarehouseRequisitionEmailService` und `BdeAutoPauseService`
  protokollieren jetzt ebenfalls — nicht nur die eigentlichen Syncs.
- **UI-Umbenennung** „Sync-Protokoll" → „Aktivitaets-Protokoll". DB-Tabelle, Klassen und Route
  (`/SyncLog/Index`) behalten den historischen Namen — bewusste Asymmetrie, begruendet in der Spec.

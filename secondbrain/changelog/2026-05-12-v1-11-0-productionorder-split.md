---
type: changelog
version: 1.11.0
date: 2026-05-12
---
# v1.11.0 — ProductionOrder-Schema-Refactor (Split Phase 1)

Der architektonisch wichtigste Umbau am FA-Datenmodell →
[[0009-app-status-in-satelliten-tabellen-neben-sage-master]].

- `ProductionOrders` von 28 auf ca. 12 Spalten reduziert: die Tabelle enthaelt nur noch
  **Sage-Master-Daten**. App-Status wandert in Satelliten:
  `ProductionOrderPickingStatus` (1:1), `ProductionOrderBdeStatus` (1:1),
  `ProductionOrderAssemblyGroups` (1:N, 5 Zeilen je FA), `...AssemblyGroupSpecs`,
  `ProductionWorkplaceAssemblyGroups`.
- **Warum:** der Sage-AgentJob ueberschreibt die Master-Tabelle. Solange App-Flags dort lagen,
  konnte jeder Import Benutzereingaben stillschweigend zuruecksetzen.
- Toggle-API in 3 fokussierte Endpoints aufgeteilt (`/api/picking-status/toggle`,
  `/api/assembly-groups/toggle-applicable`, `/api/bde-status/toggle`); der Sammel-Endpoint
  `toggle-field` entfiel.
- **User-Sicht unveraendert** — nur die Datenquelle wechselte auf JOIN-Lookups.
- Datenmigration atomar im Wartungsfenster (`SQL/60_ProductionOrderSplit.sql`); der AgentJob legt
  fuer jeden neuen Auftrag die Status-Zeilen eager an.
- **Nachwirkung:** die Lese-Seite wurde dabei nicht vollstaendig umgestellt — `IsDonePicking` wurde
  erst in v1.21.1 tatsaechlich ausgewertet.

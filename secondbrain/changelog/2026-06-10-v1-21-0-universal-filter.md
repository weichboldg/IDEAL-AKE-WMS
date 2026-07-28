---
type: changelog
version: 1.21.0
date: 2026-06-10
---
# v1.21.0 — Universal-Filter-Rollout

- **Jede** Tabellen-Liste ist jetzt spaltenfilterbar, und Filter wirken ueber alle Eintraege und
  Seiten — nicht mehr nur auf der geladenen Seite.
- Bestehende Listen auf Server-Side-Filter umgestellt: WarehousePicking,
  WarehouseRequisitions, Picking (inkl. KW-Datumsfilter), PartRequisitions, StorageLocations,
  BdeBookings (auf SQL-Level, Query und Count filtern identisch via Expression-Trees).
- Neu filterbar: Users, Roles, Workstations, ProductionWorkplaces, ArticleCategories,
  ArticleAttributes, OrderRecipients, BdeMasterData (3 Tabs mit eigenen View-Keys),
  Tracking/ByWorkplace (Client-Filter).
- **ColumnMap-Pattern verankert:** Getter liefern den *gerenderten* Zellentext, Apply vor der
  Pagination, `TotalCount` aus der gefilterten Menge →
  [[0005-listen-view-pattern-mit-server-side-spaltenfilter]].
- Filter-Mini-Syntax ueberall identisch: OR mit `,`, NOT mit `!`.

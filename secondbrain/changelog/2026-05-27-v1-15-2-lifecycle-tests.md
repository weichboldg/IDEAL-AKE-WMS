---
type: changelog
version: 1.15.2
date: 2026-05-27
---
# v1.15.2 — Lifecycle-Tests + OSEON-Stammdaten-Imports + ctor-Vereinheitlichung

- Zwei weitere Protokoll-Namen: `OseonWorkplaces` und `OseonArticleCategories` — damit erscheinen
  auch die OSEON-Stammdaten-Imports im Aktivitaets-Protokoll.
- Lifecycle-Tests fuer Oseon/EnaioDms/BomCache/SageImport.
- **Bugfix Connection-String-Guards:** die Validierung lag vor `BeginRunAsync`, wodurch
  `FinishFailedAsync` bei Config-Fehlern nicht feuerte und der Lauf ewig „offen" blieb. Sie liegt
  jetzt **innerhalb** des try-Blocks.
- `HolidaySyncService`-Konstruktor auf die Projekt-Konvention gebracht (`ISyncLogger` als letzter
  Parameter, nach `ILogger<T>`).

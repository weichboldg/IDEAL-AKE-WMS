---
typ: notiz
spec: "[[2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Arbeitsgang-Abgleich aus der Sage-View

Spec [[2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view-spec]] · **direkt im Bündel**, Start `d3de9022` (v1.60.2),
Ziel v1.61.0, `SQL/96`. Deploy: Web + Dienst (Migration beim Web-Start), P5 je Standort vorher.

## Plan (kompakt — Details stehen auf Datei:Zeile-Ebene in der Spec)

1. **Model + Migration** — `OperationConfig.Source`/`SageMissingSince` + `OperationConfigSource`; Mapping; `dotnet ef migrations add
   AddOperationConfigSourceAndSageMissingSince` + `migrationBuilder.Sql` (Bestandsbefüllung); `SQL/96`; FreshInstall (Schema, Seed-EXEC,
   MigrationId); OSEON-Seed `ExternalSystem`; `UpdateAsync` nur Pflegefelder (AK 28, Red-Green); Drift-Test.
2. **Service** — 3 Katalog-Keys, `SyncLogServices.OperationConfigSync`, `OperationConfigSyncService` (+ `Plan`, Concurrency-Catch je
   Eintrag), SyncWorker-Block, DI, Reader-Logtext neutral, Standort-Katalog; Tests (AK 4–14, 23, 24, 27, 29).
3. **Web** — Spalte Quelle (`FormatSource`, `ColumnMap`), Kennzeichnung (`MarkerActive`, `NotInSage`, Badge, Hinweis, Legende), `Add`
   setzt `Manual`; Controller-Tests.
4. **Zahnrad** — `ColumnDefinitions.OperationConfigs`, Host, `#view-config`/`#column-config`, `column-preferences.js`; View-Drift-Test.
5. **Doku** — Hilfe, Changelog, 2× AppVersion 1.61.0, README, TS-94; Brain (services.md, datenmodell, feature-map, Testindex, changelog).

## Stand

- 2026-10-16: Umsetzung gestartet.
- 2026-10-16: feature-complete (`6993c774`, `bc134c15`, `7042efcb`, `459cb7b5`, `9731b38a`); QA ohne kritische/wichtige Befunde, QA-Fix `8773ba4f` → **Testbereit**. Build + 2036 Web- / 307 Service-Tests grün. Changelog [[2026-10-16-v1-61-0-arbeitsgang-abgleich-sage-view]].
- Schranke 2 mit dem Bündel: P1–P5 (P5 je Standort vor dem Deploy), TS-94, Manuelle Test-Checkliste in der Spec.

---
type: changelog
version: 1.54.0
date: 2026-10-12
---
# v1.54.0 — Arbeitsgang-Stammdaten vereinheitlichen (OSEON + IDEAL), Reiter „Arbeitsgänge“

[[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]] (Antworten 1–8, Variante U). **Direkt im Bündel** `feature/2026-08-07-ideal-teile-1-5`, Start `87db96c8`
(v1.53.0), Commits `d4bb711a`, `5ac5ea16`, `b5123529`, QA-Fix `d56a340e`. **Migration** `20261012140548_RenameOperationConfigsAndAddAudit` = **`SQL/95`**.
Umsetzung [[2026-10-12-arbeitsgang-stammdaten-vereinheitlichen-umsetzung]].

**Warum:** FA-Struktur Teil 2/3 brauchen Termin-Offsets je IDEAL-Kürzel; statt einer zweiten Pflege wird die OSEON-Tabelle
zur gemeinsamen Arbeitsgang-Stammtabelle mit neutralen Begriffen, einem Pflege-Ort und einem Offset-Lookup.

## Kernpunkte

- Schema: `OseonOperationConfigs` → `OperationConfigs`, `IsOseonRelevant` → `IsExternalSystemRelevant`, PK/Index umbenannt,
  `UQ_`-Constraint (Skript-/FreshInstall-Form) → Unique-Index `IX_OperationConfigs_OperationName`, Audit-Felder (Bestand
  `system`). Migration nur aus guarded `migrationBuilder.Sql` (EF schlug DropTable+CreateTable vor); `SQL/95` identische
  Batches + Halt nach Anomalie (`SET NOEXEC`); FreshInstall Zielzustand. Drift-Wächter `OperationConfigsMigrationSqlTests`.
- `OperationConfigsController` (nur Admin, ohne `BdeAktiv`-Gate), Reiter-Partial `_BdeMasterDataTabs`, Menüeintrag,
  `/Settings/OperationConfig` → 302. Begriffe „Fremdsystem-relevant“ (nur OSEON-Wirkung), „Offset (Arbeitstage) zum
  Bezugstermin“, Legende je System.
- `GetDueDateOffsetsAsync` (case-insensitiv, getrimmt, kleinste Id gewinnt; Schnittstelle für FA-Struktur Teil 2/3) und
  Liste „nicht konfiguriert“ aus OSEON + WMS-Arbeitsgängen ohne `01`. OSEON-Pfade bitgleich (nur Bezeichner).
- TS-87 (+ Nachzüge TS-1.7/7.9/7.10/16.5/36.5); Tests Web 1795 (+28), Service 272.

## Deploy-Risiken

**DB-Backup**, App-Pool **vor** `SQL/95` stoppen (alte Binaries fragen danach die alte Tabelle ab), `SQL/95` vollständig
ausführen (nicht markierte Teile — NOEXEC bliebe sonst an), bei „ABBRUCH“ anhalten; danach neue Web-Binaries, Recycle.
Vorher Prüfschritt P1 (fehlt `20260330142035_AddOseonOperationConfig` in der History → anhalten, R10). IDEAL-DB: OSEON-Seed-
Zeilen nach P3 per UI bereinigen, IDEAL-Kürzel anlegen, bevor FA-Struktur Teil 2/3 abgenommen werden. Folgeaufgabe:
[[2026-10-12-operationconfig-offset-leer-pruefen]].

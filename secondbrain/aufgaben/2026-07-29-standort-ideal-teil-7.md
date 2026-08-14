---
type: aufgabe
title: "IDEAL Teil 7 — Materialisierung nach ProductionOrders (Epic-Umsetzung)"
status: InUmsetzung
created: 2026-08-14
updated: 2026-08-14
spec: "[[2026-07-29-standort-ideal-teil-7-spec]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---

# IDEAL Teil 7 — Epic-Umsetzung

Läuft im **bestehenden** Bündel-Worktree (gemeinsam mit Teile 1–5 v1.31.0, kein neuer Worktree).
Fünf Etappen A–E, ein Merge am Ende. Spec: [[2026-07-29-standort-ideal-teil-7-spec]].

## Etappen-Status

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| A | Schema-Inversion + Migration + Backfill + FreshInstall + tote AgentJobs | **erledigt** | `fe7299b` |
| B | Guard (Choke-Point) + Einwegtor/Umschalt-Seite + Audit-SyncLog + Runbook | offen | — |
| C | Materialisierungs-Sync + drei Sync-Regeln (unit-getestete Planer) | offen | — |
| D | Lookup-Härtung + `GetAllByFaAndOperationAsync` + FA-Zusatzinfos-Review + Auto-Erledigt-Sperre | offen | — |
| E | Doku, Testszenarien, Brain-Update, Version-Bump | offen | — |

## Umsetzungsnotizen

### Etappe A — Schema-Inversion (2026-08-14, `fe7299b`)

- **Modell** `ProductionOrder.cs`: `SubOrderNumber` (`[Required][StringLength(100)]`),
  `ParentSubOrderNumber` (`string?`), `SageMissingSince` (`DateTime?`). Kommentare erklären die
  Invariante `SubOrderNumber == OrderNumber` im flachen AKE-Modus.
- **DbContext**: Unique-Index von `OrderNumber` → `SubOrderNumber`; `OrderNumber` bleibt als
  **nicht**-eindeutiger Index (Zeile 417 war `HasIndex(OrderNumber).IsUnique()`).
- **EF-Migration** `20260814105526_InvertProductionOrderHierarchy`: Up() **manuell korrigiert** —
  die von EF generierte Fassung setzte `SubOrderNumber` mit `defaultValue: ""` und legte sofort
  den Unique-Index an; auf befüllten AKE-Daten hätte `dotnet ef database update` am Unique-Index
  (alle `""`) gebrochen. Jetzt: nullable anlegen → `UPDATE SET SubOrderNumber = OrderNumber` →
  `AlterColumn NOT NULL` → Index-Tausch.
- **SQL/90** (Platzhalter „SQL/87" der Spec war belegt — 75–89 existieren, nächste frei = **90**):
  idempotent mit `COL_LENGTH`- + `is_nullable`- + `sys.indexes`/`sys.key_constraints`-Guards.
  Räumt die alte Eindeutigkeit auf `OrderNumber` in **beiden** Formen ab — `UQ_`-Constraint
  (FreshInstall-DB) **und** `IX_`-Unique-Index (EF-migrierte DB). Batches getrennt (Spalten →
  Backfill → NOT NULL → Index-Tausch → History-Insert).
- **FreshInstall** an beiden Stellen: 3 Spalten in `CREATE TABLE`, `UQ_…_OrderNumber`-Constraint
  entfernt (Endzustand = kein UQ-Constraint, Unique-Index auf `SubOrderNumber`), Index-Sektion
  (OrderNumber jetzt nicht-eindeutig + neuer Unique auf SubOrderNumber), MigrationId-Insert.
- **Toter AgentJob**: `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` → `_archiv/` mit
  „AUSSER BETRIEB"-Kopf; `02_Import_Artikel.sql` bewusst belassen (trägt bereits
  `DEPRECATED seit v1.17.0`-Header, nicht irreführend). Sweep-Nachweis in `_archiv/README.md`.
- **Produktiv-Insert-Prüfung:** kein `new ProductionOrder`/`ProductionOrders.Add` im Produktivcode —
  Inserts laufen ausschließlich über `SageImportService` (raw-SQL, setzt `SubOrderNumber=OrderNumber`
  via `SageProductionOrderSql.BuildUpsert(true)`, sobald die Spalte existiert). Invariante hält.
- **Build** 0 Fehler; **Tests** Web 1174 (+1 skip) + Service 221 grün.
- **Kein** Version-Bump in Etappe A (erst Etappe E). Guard/Umschalt-Seite/Sync-Regeln folgen in B–D.

## Offene Punkte / Deploy-kritisch (aus der Spec, für Schranke 2 / Deploy)

- **DB-Backup vor Deploy zwingend** (Kern-Tabelle `ProductionOrders`, daten-konvertierend).
- **Harte Deploy-Vorbedingung (AK 13):** serverseitig `sysjobs` prüfen, ob ein aktiver Agent-Job
  `ProductionOrders` schreibt → deaktivieren; das Archivieren der Repo-Datei entfernt keinen Job.
- **Irreversibilität:** Etappe A invertiert das Schema unabhängig vom Master-Schalter. Abbruch nach
  A lässt die DB invertiert (inhärent, im Runbook zu benennen — folgt in Etappe B).
- **Kopplung:** selber Branch wie Teile 1–5 (v1.31.0, testbereit) — Merge bringt beides zusammen.
- Schalter-Inventur (S7-8): welche „weiteren abhängigen Schalter" neben Master + Auto-Erledigt —
  vor Etappe B/D explizit benennen.

---
typ: notiz
spec: "[[2026-10-16-aktivitaetsprotokoll-filter-und-namen-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Aktivitäts-Protokoll Spaltenfilter und deutsche Namen

Spec [[2026-10-16-aktivitaetsprotokoll-filter-und-namen-spec]], direkt im Bündel. Start `2c1e3a6d` (v1.69.0), Ziel **v1.70.0**, **TS-102**.
Keine Migration, kein Index. Deploy: Web (Dienst nur Version). Bug-Datensatz [[2026-10-28-aktivitaetsprotokoll-spaltenfilter-nach-paging-bug]].

## Plan (kompakt)

1. `SyncLogServices.DisplayNames` (Tabelle A2) + `DisplayName(string)` mit Rückfall.
2. `SyncLogRepository.GetPagedAsync(..., columnFilters)`: Spaltenfilter vor Count/Skip/Take, `internal static ApplyColumnFilter`
   (Muster `StockMovementRepository.ApplyMovementColumnFilter`), Seiten-Guard `page < 1 → 1`.
3. `SyncLogController.Index`: Filter durchreichen, In-Memory-Apply + `SyncLogColumnMap` weg, Auswahl nach Anzeigename sortiert.
4. View (Text deutsch, `title` technisch), Hilfe, Changelog, v1.70.0, TS-102, Tests (Repository, SQL-Übersetzung, Controller, Namen).

## Stand

- 2026-10-28: umgesetzt, Commit `badbedcc` (wip feature-complete); Build 0 Fehler, Web 2213 / 1 übersprungen, Dienst 328.
- 2026-10-28: **Testbereit** (qa-agent), keine Befunde mittel/hoch; Q-1/Q-4 nachgebessert `d7dc3d03`. Wartet auf Schranke 2.

## Offen nach UAT

- P-2 (Kollation `SyncLogs`) und P-3 (Größe, Altnamen) vor TS-102 auf beiden Servern; ohne `…_CI_…` STOPP und melden.
- Fehlteile „nur meine Werkbänke“: eigener Backlog-Eintrag [[2026-10-28-fehlteile-nur-meine-werkbaenke-trefferzahl]].

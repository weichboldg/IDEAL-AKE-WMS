---
type: changelog
version: 1.15.0
date: 2026-05-26
---
# v1.15.0 — Protokoll-Pflicht fuer alle Sync-Services

- Neue Abstraktion `ISyncLogger` / `ISyncRun` mit Lauf-Lebenszyklus
  (`BeginRunAsync` → Detailzeilen → `FinishSuccess`/`FinishFailedAsync`).
- **Jede Protokollzeile wird mit einem frischen `DbContext` aus `IDbContextFactory` geschrieben** —
  Diagnose-Logs duerfen nicht in Sync-Transaktionen mitrollen und im Fehlerfall verschwinden →
  [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]].
- Alle 8 Sync-Services migriert (Lagerplatz, Lagerbestand, Feiertage, BomCache, Oseon, EnaioDms,
  CoatingDetection, SageImport mit 2 Laeufen).
- Service-Namen als Konstanten in `SyncLogServices.All` — der Protokoll-Filter kennt nur Namen,
  die dort stehen. Counts-Keys sind deutschsprachig (`neu`, `aktualisiert`, `uebersprungen`, `fehler`).

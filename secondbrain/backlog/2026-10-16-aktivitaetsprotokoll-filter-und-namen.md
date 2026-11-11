---
typ: bug
---
# Aktivitäts-Protokoll: Spaltenfilter wirkt nur auf die aktuelle Seite, Aktivitäten nur mit technischen Namen

**Aufgenommen 2026-10-16** (Meldung Gerald: „alle Syncs müssen ins Aktivitätslog – konnte dazu nichts finden“).

## Befund (Koordination, Bündel-Worktree v1.61.0, nur gelesen)

- **Alle Syncs schreiben ins Aktivitäts-Protokoll** (`SyncLogs`): jeder Sync-Service öffnet einen Lauf über
  `ISyncLogger.BeginRunAsync(SyncLogServices.…)` – 24 Dienste, darunter `FaHierarchy`, `FaMaterialization`,
  `ProductionWorkplaceSync`, `OperationConfigSync`, `WorkOperationStructureDetection`, `Article`, `Lagerbestand`.
  Erfolgreiche Läufe schreiben eine Info-Zeile (`SyncRun.cs` FinishSuccess -> `WriteEntryAsync(…Info…)`).
  Alle stehen in `SyncLogServices.All` und damit im Auswahlfeld „Aktivität“ (`SyncLogController.cs:14, :47`).
  Ob die Zeilen auf IDEAL-Test tatsächlich in der DB stehen, ist **ungeprüft** (Abfrage unten).
- **Bug Spaltenfilter:** Die Tabelle ist als server-gefiltert markiert (`Views/SyncLog/Index.cshtml`,
  `data-server-column-filter="true"`), der Controller filtert aber **nach** dem Paging: `GetPagedAsync` liefert eine
  Seite (`SyncLogRepository.cs:43-48`), danach `ColumnFilterHelper.Apply(rows, …)` (`SyncLogController.cs` Index).
  Ein Spaltenfilter (z. B. „Aktivität“ oder „Meldung“) durchsucht nur die geladene Seite; Trefferzahl und Paging bleiben
  ungefiltert. Ältere Einträge sind so über den Spaltenfilter nicht zu finden.
- **Bedienbarkeit:** Das Auswahlfeld „Aktivität“ zeigt die technischen Namen (`OperationConfigSync`,
  `ProductionWorkplaceSync` …), nicht die Namen aus dem Dienst-Log („Arbeitsgang-Abgleich“, „Werkbank-Anlage“).

## Für die Spec (Vorschlag Koordination)

1. Spaltenfilter im Repository **vor** Skip/Take anwenden (Muster anderer server-gefilterter Listen prüfen), TotalCount
   gefiltert.
2. Deutsche Anzeigenamen je `SyncLogServices`-Konstante (eine Zuordnung, Auswahlfeld und Tabellenspalte), technischer
   Name als Wert/Tooltip; Test: jede Konstante in `All` hat einen Anzeigenamen.
3. Prüfen, ob andere Listen mit `data-server-column-filter="true"` dasselbe Muster haben.

## Prüfabfrage (WMS-DB, Gerald)

```sql
SELECT TOP 30 Timestamp, Service, Level, LEFT(Message, 200) AS Message
FROM dbo.SyncLogs
WHERE Service IN ('OperationConfigSync','ProductionWorkplaceSync','WorkOperationStructureDetection',
                  'FaHierarchy','FaMaterialization')
ORDER BY Timestamp DESC;
```

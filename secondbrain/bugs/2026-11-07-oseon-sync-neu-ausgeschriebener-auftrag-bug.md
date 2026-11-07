---
type: bug
title: OSEON-Tracking-Sync bricht mit Fehler 2601 ab, wenn OSEON einen Auftrag neu ausschreibt
status: offen
severity: hoch
created: 2026-11-07
affected_code:
  - "IDEALAKEWMSService/Services/OseonSyncService.cs:148-151 (GroupBy ohne deterministische Auswahl)"
  - "IDEALAKEWMSService/Services/OseonSyncService.cs:272-274 (group.First())"
  - "IDEALAKEWMSService/Services/OseonSyncService.cs:337-371 (MERGE ON OseonId)"
  - "IDEALAKEWMSService/Services/OseonSyncService.cs:399-401, 470-496 (AG-MERGE ohne Dedupe)"
  - "IdealAkeWms/Data/ApplicationDbContext.cs:922 (UNIQUE OseonOrderNumber)"
source_backlog: "[[2026-11-03-oseon-sync-neu-ausgeschriebener-auftrag]]"
spec: "[[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]]"
---

## Symptom

`SqlException 2601` beim OSEON-Tracking-Sync: doppelter Schlüssel in `IX_OseonProductionOrders_OseonOrderNumber`
(Wert `1617675-06`), Stack `OseonSyncService.BulkMergeOrdersAsync` (`OseonSyncService.cs:377`, der Lese-Loop des
MERGE-`OUTPUT`). Der ganze Lauf scheitert, auch die Arbeitsgänge werden nicht abgeglichen. Gemeldet von der
Testinstanz (Absender `wms-testideal@ake.at`, AKENET01, v1.30.0, 07.11.2026 08:03). Die AKE-Produktion ist laut
Verlauf im Backlog vom 07.11. 08:55 nicht betroffen; derselbe Code liegt aber dort.

## Reproduktion

1. In einer WMS-DB mit aktivem `Sync:OseonTrackingEnabled` einen Datensatz in `dbo.OseonProductionOrders` auf eine
   andere `OseonId` setzen, als OSEON für dieselbe `AuftragsNr` liefert. So sieht eine Neuausschreibung aus.
2. Sicherstellen, dass der Auftrag im Delta-Fenster liegt (`DateOfLastChange > MAX(LastChangedInOseon) − 5 min`).
3. Nächster Sync-Lauf: `WHEN NOT MATCHED THEN INSERT` mit derselben Auftragsnummer, Fehler 2601.

## Root Cause

Der Auftrags-MERGE gleicht über `OseonId` ab (`OseonSyncService.cs:344`). Eindeutig ist in der Tabelle aber
`OseonOrderNumber` (`ApplicationDbContext.cs:922`, `SQL/00_FreshInstall.sql:938`); auf `OseonId` liegt nur ein
normaler Index (`ApplicationDbContext.cs:924`). Ändert OSEON die ID einer Nummer, gibt es keinen Treffer, der MERGE
fügt ein, und der eindeutige Index weist das zurück. Weil nichts geschrieben wird, bleibt
`MAX(LastChangedInOseon)` stehen (`OseonSyncService.cs:55-63`), und jeder weitere Lauf scheitert erneut.

Nebenbefund am selben Code: Liefert OSEON zu einer Nummer mehrere `ProduktionsAuftrag`-Zeilen, wählt
`group.First()` (`:274`) eine zufällige ID, denn die OSEON-Abfrage hat kein `ORDER BY` (`:72-101`). Der AG-Join läuft
über `AuftragsNr` (`:94`), deshalb kommen die Arbeitsgänge dann je Auftragszeile doppelt in `#TmpOseonOps`
(`:399-430`). Der AG-MERGE (`:470-496`) scheitert in diesem Fall mit 8672 oder 2601 auf
`IX_OseonWorkOperations_OseonProductionOrderId_PositionNumber`.

## Fix / Verweis

[[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]]: Der MERGE gleicht über die Auftragsnummer ab, je Nummer
wird deterministisch eine Zeile ausgewählt, die AG-Zeilen werden dedupliziert, und Neuausschreibungen werden im
Aktivitäts-Protokoll gemeldet. Bis dahin gibt es als Workaround das Umschlüssel-Skript
`AKE-Oseon-Auftrag-umschluesseln.sql` (siehe Backlog-Verlauf).

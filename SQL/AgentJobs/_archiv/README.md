# SQL/AgentJobs/_archiv

Ausser Betrieb gesetzte SQL-Agent-Job-Skripte. Rein historische Referenz — **nicht ausfuehren**.

## Sweep 2026-08-14 (IDEAL Teil 7, Etappe A)

Der gesamte Ordner `SQL/AgentJobs/` wurde auf tote Artefakte durchgesehen:

- **`01_Import_Produktionsauftraege.sql` → hierher archiviert.** Der Produktionsauftrags-Import
  laeuft produktiv ausschliesslich ueber den C#-Pfad `SageImportService` (IDEALAKEWMSService, via
  `SyncWorker`/DI live). Das Skript trug **keine** Deprecation-Warnung und hat eine sorgfaeltige
  Spec-Pruefung in die Irre gefuehrt (falsche Annahme "raw-SQL ist Produktion"). Nach der
  Schema-Inversion (Migration 90) wuerde sein `INSERT` ohne `SubOrderNumber` in die neue
  `NOT NULL`/`UNIQUE`-Spalte laufen und die AKE-Linie brechen.
- **`02_Import_Artikel.sql` → bewusst am Ort belassen.** Traegt bereits einen prominenten
  `DEPRECATED seit v1.17.0`-Header (produktiver Sync im IDEALAKEWMSService,
  `Sync:ArticlesEnabled = true`; das Skript ist ausdruecklich nur noch manueller Failover). Es ist
  nicht irrefuehrend und dokumentiert seinen Status selbst — daher nicht archiviert.

**Wichtig:** Das Archivieren/Loeschen einer Repo-`.sql`-Datei entfernt **keinen** auf `AKESQL20`
eingerichteten SQL-Agent-Job. Vor der Migration 90 ist serverseitig zu pruefen, ob ein aktiver Job
`ProductionOrders` beschreibt (siehe Deploy-Abschnitt der Teil-7-Spec / AK 13); gefundene Jobs
werden im Wartungsfenster deaktiviert.

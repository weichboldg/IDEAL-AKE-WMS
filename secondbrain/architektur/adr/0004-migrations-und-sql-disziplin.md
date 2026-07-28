---
type: adr
id: 0004
title: Jede Schema-Aenderung dreifach — EF-Migration, idempotentes SQL-Skript, FreshInstall
status: accepted
date: 2026-03-11
supersedes: ""
superseded_by: ""
---

> **Nachtraeglich erfasst** (2026-07-27). Bestandsdokumentation; die Regel ist ueber viele
> Releases gewachsen und hat mehrere Deploy-Zwischenfaelle als Ursache.

## Kontext und Problem

Das Schema wird auf drei Wegen angefasst: die App migriert beim Start selbst
(`db.Database.Migrate()`), der DBA spielt Skripte in Wartungsfenstern ein, und
Neuinstallationen brauchen ein konsolidiertes Schema. Wenn diese Wege auseinanderlaufen,
scheitert entweder die Installation oder — schlimmer — der erste App-Start danach, weil EF eine
Migration gegen ein Schema replayt, in dem die Objekte schon existieren (`SqlException`).
Zusaetzlich parst SQL Server Batches als Ganzes, sodass „Tabelle anlegen und im gleichen Batch
befuellen" fehlschlaegt.

## Betrachtete Optionen

- **Nur EF-Migrationen** — bequem, aber der DBA hat kein einspielbares, pruefbares Artefakt und
  Neuinstallationen laufen 81 Migrationen nacheinander.
- **Nur handgeschriebenes SQL** — volle Kontrolle, aber das EF-Modell driftet weg
  (`PendingModelChangesWarning`) und Tests laufen gegen ein anderes Modell als die DB.
- **Beides, plus konsolidiertes FreshInstall** — mehr Pflegeaufwand pro Aenderung, dafuer sind
  alle drei Wege konsistent.

## Entscheidung

Der Migrations-Workflow ist verbindlich fuenfstufig:

1. Model aendern.
2. `dotnet ef migrations add <Name>` — sonst schlaegt der naechste Start mit
   `PendingModelChangesWarning` fehl.
3. Idempotentes Skript `../../../SQL/XX_<Name>.sql` mit `OBJECT_ID`-Guard; Tabellen-DDL in einem
   **eigenen Batch** (`GO`), weil SQL Server den Batch vorab parst.
4. `__EFMigrationsHistory`-Insert in separatem Batch.
5. `../../../SQL/00_FreshInstall.sql` konsolidiert nachziehen — **zwei** Stellen: (a) die
   Schema-Objekte im konsolidierten Schema, (b) die `MigrationId` im History-INSERT-Block am Ende.

Zusaetzlich: die SQL-Agent-Jobs unter `../../../SQL/AgentJobs/` sind Teil des Schema-Vertrags. Wenn
eine Migration Pflichtfelder oder Folge-MERGEs aendert, muss der Job **im selben Wartungsfenster**
aktualisiert werden (Beispiel v1.22.0: der entfernte AssemblyGroups-MERGE haette sonst den
gesamten FA-Import zum Absturz gebracht).

Daten-destruktive Migrationen (Spalten-Drops nach Konvertierung, z. B. 65, 76) werden als solche
markiert und mit „DB-Backup vor Deploy" dokumentiert.

## Konsequenzen

**Positiv**
- Alle drei Installationswege (Upgrade per App, Upgrade per Skript, Neuinstallation) sind
  konsistent und wiederholbar.
- Skripte sind mehrfach einspielbar, ohne Schaden anzurichten.

**Negativ / Risiken**
- Vier Artefakte pro Schema-Aenderung; wird eines vergessen, bricht es **spaeter** und woanders
  (Punkt 5 ist der haeufigste Fehler — Fallstrick dazu in [[fallstricke]]).
- Das FreshInstall-Skript ist ein handgepflegtes Konsolidat, also selbst eine Fehlerquelle.
- Deploys sind an Wartungsfenster gebunden, sobald AgentJobs mitziehen muessen.

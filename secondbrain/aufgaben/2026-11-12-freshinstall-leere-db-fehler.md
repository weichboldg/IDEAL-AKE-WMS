---
type: aufgabe
title: "SQL/00_FreshInstall.sql läuft auf leerer DB nicht fehlerfrei durch"
status: offen
created: 2026-11-12
quelle: "[[2026-11-12-wareneingang-barcode-lieferschein-csv-export-spec]] (QA-Befund B-6)"
---

# SQL/00_FreshInstall.sql auf leerer DB

Beim QA-Lauf v1.99.11 (2026-11-12) lief `sqlcmd -S . -E -C -I -d <leere DB> -i SQL\00_FreshInstall.sql` (Worktree
`feature/2026-08-07-ideal-teile-1-5`, Stand `0dc0cea0`) mit Fehlern durch:

- Msg 207 „Invalid column name 'AssignedPickerId'“
- Msg 2714 „There is already an object named 'WarehouseRequisitions'“ bzw. `WarehouseRequisitionItems`
- danach fehlt `dbo.__EFMigrationsHistory`

Die Fundstellen liegen nicht im Diff von v1.99.11 (die v1.99.11-Teile – Spalten `Barcode`/`LieferantName`, Tabelle `WareneingangCsvExporte`,
MigrationId – sind korrekt). Zu klären: ob das Skript so nicht (mehr) als Ganzes auf einer leeren DB laufen soll oder ob Reihenfolge/Guards
nachgezogen werden müssen (ADR 0004). Nur lesend nachstellen; Reparatur als eigene Spec.
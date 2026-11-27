---
type: aufgabe
status: Offen
created: 2026-11-11
spec: "[[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]]"
---
# Bündel-Deploy mit dem Update-Skript (Protokoll je System)

Erster Einsatz von `deploy\Update-IdealAkeWms.ps1` (v1.98.0, ADR [[0022-update-je-zielsystem-per-skript-mit-nachlauf-manifest]]) beim
Bündel-Deploy. Nur Gerald, von Hand, in einer Administrator-PowerShell am Zielserver.

## Vorher (je Server)
- [ ] `Get-Command sqlcmd` (sonst ODBC-Treiber + „Microsoft Command Line Utilities for SQL Server“ installieren)
- [ ] Rechte-Abfrage aus Rückfrage 7 der Spec in SSMS als ausführendes Konto (db_owner, BACKUP DATABASE, msdb.sysjobs)
- [ ] Paket `publish\` nach `C:\<FIRMA>\deploy\v<Version>\` kopieren, `publish-info.txt` prüfen

## Je System (Reihenfolge: Test-Systeme, dann Prod)
| System | `-NurPruefen` (Plan vs. Backlog-Tabelle) | Volllauf | Protokoll | Nachlauf erledigt |
|---|---|---|---|---|
| AKE Test | | | | |
| AKE Test-IDEAL | | | | |
| IDEAL Test | | | | |
| AKE Prod | | | | |
| IDEAL Prod | | | | |

Erwartung `-NurPruefen` (TS-125.16): AKE fehlen 89–107, 109, 112, 113, 133, 140, 141; IDEAL-Prod 98–141 ohne 108/110.
Einmal-Skripte laut Freigabe-Antwort 4: v1.47.4 und 00er nur IDEAL-Test; Hotfix-1.30.1 nirgends.

---
type: changelog
version: 1.99.0
date: 2026-11-11
---
# v1.99.0 — Update-Skript prueft und fuehrt aus, mit Bestaetigung

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commits `1b285e9b`, `b30e310d` (auf v1.98.1 `e1ecc0c0`). Spec
[[2026-11-11-update-skript-prueft-und-fuehrt-aus-mit-bestaetigung-spec]], ADR [[0023-daten-und-einmal-skripte-fuehrt-das-update-skript-mit-bestaetigung-aus]].
Nicht gemergt, nicht gepusht.

## Kernpunkte

- Manifest-Typ `Pruefung` (jeder Lauf): Kommissionierbereiche, Standardbereich, RCSI, Rollen Beschichtungsauftrag (AKE), DUMMY-Artikel;
  nach dem Start Anschrift, QrTrennzeichen, Sync:ArticlesViewName. Reparatur nur nach Bestaetigung, Web und Dienst gestoppt.
- Daten-/Einmal-Skripte: Trockenlauf automatisch (`TREFFER: n`, 0 = entfaellt), scharf nur nach `SCHARF` mit Backup aus dem Lauf; ohne
  Schalter in einer Transaktion mit `uebernehmen`/`verwerfen` (Schritt 5b vor dem Start, Phase `VorStart`).
- Marker-Tabelle `dbo.DeployScriptRuns` (Migration `20261111174202_AddDeployScriptRuns`, **SQL/142**, FreshInstall).
- Bugfixes aus dem IDEAL-Test-Lauf: sqlcmd-Ermittlung unter StrictMode, `$PaketVersion?`, Backup-Ordner vorab, Zustand bei jedem Abbruch,
  Startup-Seed der Kommissionierbereiche.

## Deploy-Risiken

- Migration additiv (neue leere Tabelle); das Update-Skript spielt SQL/142 selbst als letzten Schema-Eintrag ein.
- RCSI-Reparatur beendet mit `ROLLBACK IMMEDIATE` alle anderen Sitzungen auf der WMS-DB (das Skript listet sie vorher).
- Scharflaeufe und Transaktionen sind daten-aendernd; Backup aus demselben Lauf Pflicht (Fluss A) bzw. bestaetigt (Fluss B).
- P-B1 vor dem ersten Lauf: SQL-Server-Version (>= 2017 fuer `sys.dm_os_file_exists`) und Recht `ALTER` auf die DB.

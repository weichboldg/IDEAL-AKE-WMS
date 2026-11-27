---
type: changelog
version: 1.99.2
date: 2026-11-11
---
# v1.99.2 — Update-Skript: Altbestand-Trockenlauf in der Vorprüfung

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commit `81b95ac2` (auf v1.99.1 `f79ba893`). Spec
[[2026-11-11-update-skript-prueft-und-fuehrt-aus-mit-bestaetigung-spec]] (Freigabe-Nachtrag 20:40). Nicht gemergt, nicht gepusht.

## Kernpunkte

- Der Trockenlauf des Altbestand-Skripts (v1.88.0) laeuft schon in der Vorpruefung, lesend und vor dem Stoppen (auch bei `-NurPruefen`).
- Treffer (oder nicht bestimmbar) → Backup in Schritt 2 Pflicht; n oder Fehler = Abbruch vor dem Stoppen, Web und Dienst laufen weiter.
- 0 Treffer → kein Haltepunkt; 4a vermerkt nach Bestaetigung Marker `Entfaellt` in `dbo.DeployScriptRuns`.
- Toter Zustand `$script:BackupLiegtVor` entfernt.

## Deploy-Risiken

Keine Migration, kein SQL. Nur `deploy\Update-IdealAkeWms.ps1`, Version und Changelog-Karte. v1.99.0-Ablauf je Zielsystem gilt weiter (inkl.
`SQL/142`, P-B1). Test: TS-126.26/126.27.
---
type: changelog
version: 1.98.0
date: 2026-11-11
---
# v1.98.0 — Update-Skript je Zielsystem mit Nachlauf-Manifest

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commits `1f3d2ce4` (feature-complete) und `1c689a66` (DEV-Laeufe). Spec [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]],
ADR [[0022-update-je-zielsystem-per-skript-mit-nachlauf-manifest]]. Nicht gemergt, nicht gepusht.

## Kernpunkte

- `scripts/deploy/Update-IdealAkeWms.ps1`: Update je System (Firma/System-Abfrage, `-Komponente Web|Dienst|Beide`, `-NurPruefen`),
  Ermittlung ueber Pfade, Schema in Manifest-Reihenfolge mit Bestaetigung, Haltepunkt Altbestand, robocopy ohne Loeschen, Web vor Dienst
  mit Versionspruefung, Nachlauf-Liste, Protokoll je System.
- `nachlauf.psd1` (25 Schema-, 5 Daten-/Einmal-, 1 Ordner- und rund 40 Hand-Eintraege inkl. v1.96.0/v1.97.0; Hotfix-1.30.1 bewusst nicht,
  v1.47.4 und 00er nur IDEAL-Test), `ziele.psd1` (5 Systeme), Modul `IdealAkeWms.Deploy.psm1` (`Test-OrdnerUndRechte`),
  `Install-IdealAkeWms.ps1` standortneutral aus der IDEAL-Installation.
- Drift-Test `NachlaufManifestDriftTests`; README-Update-Abschnitte ersetzt; qa-agent Punkt 7 (Paket mit `SQL\` + `deploy\`,
  publish-info-Format), spec-agent und Spec-Vorlage (Manifest-Pflicht) im Hauptcheckout.

## Deploy-Risiken

Keine Migration, keine Anwendungsaenderung ausser Versionsnummer und Changelog-Karte (Web + Dienst nur wegen v1.98.0). Erster Einsatz des
Skripts am Zielsystem: je System zuerst `-NurPruefen` (TS-125.16), `sqlcmd` und SQL-Rechte vorher pruefen (Freigabe-Antwort 7).

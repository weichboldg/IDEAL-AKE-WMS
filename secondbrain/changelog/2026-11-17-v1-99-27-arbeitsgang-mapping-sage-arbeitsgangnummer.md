---
type: changelog
version: 1.99.27
date: 2026-11-17
---
# v1.99.27 — Arbeitsgang-Mapping über Sage-Arbeitsgangnummer (ausgeliefert mit v1.99.32)

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commit `5960b6a5`. Spec [[2026-11-16-arbeitsgang-mapping-sage-arbeitsgangnummer-spec]], ADR [[0031-sage-arbeitsgangnummer-an-operationconfig-sagewert-plus-handwert]], Lauf [[2026-11-17-goal-lauf-beschichtung-bde-mapping]].
Version 1.99.27 war reserviert; AppVersion bleibt 1.99.32 (Anwender-Changelog: Abschnitt in der v1.99.32-Karte). Nicht gemergt.

## Kernpunkte

- Spalte „Sage-AG“ in `/OperationConfigs` mit Handwert (Vorrang) und Abweichungs-Banner.
- Prüfliste (a)/(b)/(b2)/(c) für IDEAL; (b2) zeigt Werkbänke, deren Nummer einer Sage-AG gleicht.
- Nummern nur aus der View-Quelle; Einstellungstexte korrigiert (View liefert Arbeitsgänge, keine Arbeitsplätze).
- Nichts an Sage geschrieben.

## Migration / SQL

`20261117085624_AddOperationConfigSageArbeitsgangnummer` / `SQL/153_AddOperationConfigSageArbeitsgangnummer.sql` (additiv), FreshInstall an
beiden Stellen. Manifest-Einträge mit Version 1.99.32 (Lehre QA R-1); SQL/153 läuft nach SQL/154.

## Deploy

- SQL/153 vor Web und Dienst (sonst EF-Fehler auf den neuen Spalten).
- PROD: `OperationConfigSync` aus → Nummern leer bis zum Einschalten.
- IDEAL TEST: die 26 Werkbänke aus (b2) prüfen ([[2026-11-17-werkbaenke-mit-arbeitsgangnummer-klaeren]]).

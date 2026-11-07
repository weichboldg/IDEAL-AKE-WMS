---
type: changelog
version: 1.77.0
date: 2026-11-07
---
# v1.77.0 — Einstellungen: Auswahlfelder und Beschreibung (ADR 0020)

Epic [[2026-11-07-einstellungen-auswahlfelder-und-beschreibung-spec]], Lauf [[2026-11-07-buendel-2026-11-07-goal-lauf]], ADR
[[0020-einstellungen-mit-festem-wertebereich-als-auswahl-und-mit-wirkungsbeschreibung]]. Commits E1 `b220fcba`, E2 `924e926e`, E3 `8200dae5`,
E4a `cb23aa58`, E4b `3e28eebd`. **Keine Migration, kein SQL**, nur Web (Dienst nur Version). TS-109.

## Kernpunkte

- `ServiceSettingDefinition`/`ServiceSettingType` → `SettingDefinition`/`SettingType` (+ Choice, MultiChoice, MasterData, optionale Metadaten).
- Neuer Katalog `AppSettingDefinitions` (54 Keys, Default = Wert des Lesers bei fehlender Zeile); Seed in `Program.cs` als Schleife (dieselben 38 Keys/Werte);
  `AppSettingKeys.FesteWerte` entfällt.
- `/Settings` und `/ServiceSettings` über ein Partial `_SettingField` (Anzeigename, Bewirkt, Verwendet in, Standard, Standort, Warnhinweis,
  Badges, Suche, „Nur geänderte“); Bool zeigt den wirksamen Zustand nach Lesart; `Sync:Quelle:*` als Auswahl.
- `SettingValidator`: Speichern ganz oder gar nicht, Allow-List `/Settings`, Pflicht Default-Arbeitsgang bei „nur FA-Meldung“ serverseitig.
- Beim Web-Start Lauf „Einstellungsprüfung“ für ungültige Bestandswerte.

## Verhaltensänderungen (bewusst)

- `/ServiceSettings` speichert bei einem Fehler nichts mehr (vorher die übrigen Werte).
- Default-`true`-Service-Schalter mit leerem Wert kippt beim Speichern nicht mehr auf `false`.
- `/Settings` legt für unbekannte Keys keine Zeile mehr an; unveränderte Werte werden nicht neu geschrieben.

## Deploy-Risiken

- Keine Datenänderung. Nach dem Deploy das Aktivitäts-Protokoll auf „Einstellungsprüfung“ prüfen (Prüfabfragen P-E1a–c der Spec).

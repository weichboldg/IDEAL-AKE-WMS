---
type: aufgabe
status: offen
created: 2026-11-07
quelle: "[[2026-11-07-einstellungen-auswahlfelder-und-beschreibung-spec]]"
---
# AppSettings ohne Leser aufräumen

Antwort 3 der Spec [[2026-11-07-einstellungen-auswahlfelder-und-beschreibung-spec]] (Gerald 2026-11-07): `OseonRueckmeldungAktiv` und
`SageRueckmeldungAktiv` haben keinen Leser im Code; sie stehen im Katalog als „derzeit ohne Wirkung“. Entscheiden, ob sie entfallen
(Seed-Eintrag in `AppSettingDefinitions` entfernen, DB-Zeilen per Einmal-Skript löschen) oder ein künftiges Rückmelde-Epic sie nutzt
([[2026-11-02-sage-pps-fa-bde-abgleich-spec]]).

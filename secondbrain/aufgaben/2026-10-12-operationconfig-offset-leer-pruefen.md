---
typ: aufgabe
status: erledigt
quelle: "[[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]]"
---
# Arbeitsgang-Stammdaten: leeren Offset serverseitig abweisen

QA-Nachlauf 2026-10-12 (N-3): `OperationConfigsController.Add`/`Update` binden `int dueDateOffsetDays` ohne ModelState-Prüfung.
Das Formular verlangt den Wert (`required`), ein manipulierter POST mit leerem Offset speichert aber still `0` — und ab FA-Struktur
Teil 2/3 bestimmt der Offset „überfällig“. Vorschlag: `int?` + `WarningMessage` „Offset fehlt“, Test dazu. Vor der Abnahme von
FA-Struktur Teil 2/3 erledigen (war vorher auf der alten Settings-Seite genauso).

**Erledigt 2026-10-13** in FA-Struktur Teil 2 (FA 4a, AK 19), Commit `539be27f`, v1.55.0 — siehe [[2026-10-13-fa-struktur-arbeitsgaenge-symbole-teil-2-umsetzung]].

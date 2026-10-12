---
typ: aufgabe
status: offen
quelle: "[[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]]"
---
# Arbeitsgang-Stammdaten: leeren Offset serverseitig abweisen

QA-Nachlauf 2026-10-12 (N-3): `OperationConfigsController.Add`/`Update` binden `int dueDateOffsetDays` ohne ModelState-Prüfung.
Das Formular verlangt den Wert (`required`), ein manipulierter POST mit leerem Offset speichert aber still `0` — und ab FA-Struktur
Teil 2/3 bestimmt der Offset „überfällig“. Vorschlag: `int?` + `WarningMessage` „Offset fehlt“, Test dazu. Vor der Abnahme von
FA-Struktur Teil 2/3 erledigen (war vorher auf der alten Settings-Seite genauso).

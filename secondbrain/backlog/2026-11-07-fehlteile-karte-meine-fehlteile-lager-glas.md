---
typ: bug
created: 2026-11-07
quelle: "[[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-spec]]"
---
# Karte „Meine Fehlteile“ zählt Lager und Glas zusammen, Link filtert nach Typ

Zusatzfund aus [[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-spec]] (Antwort 2: eigener Backlog-Eintrag, Gerald 2026-11-07).
Die Karte auf `/WarehouseRequisitions` zählt beide Typen (`WarehouseRequisitionRepository.cs:477-488`), der Link führt in eine nach Typ
gefilterte Liste → Zahl und Liste passen nicht zusammen. Außerdem gemeldet: Werkbank-Dropdown im Modus „Nur meine Werkbänke“ gesperrt und
listet alle Werkbänke (`MissingPartsController.cs:134`, `Views/MissingParts/Index.cshtml:70`).

---
typ: bug
---
# Gruppierte Listen: Gruppen-Kopfzeile verschwindet nach Umsortieren + Ausblenden der ersten Spalte

**Aufgenommen 2026-09-28.** Herkunft: Task-Review zu [[2026-09-25-kommissionierliste-summierung-pdf-spec]]
(v1.47.0) — **vorbestehend**, nicht durch diese Etappe eingeführt.

## Befund

`wwwroot/js/column-preferences.js` blendet eine Spalte aus, indem es in **jeder** `tbody > tr` die Zelle
`cells[physicalIdx]` versteckt (`style.display = 'none'`, ca. Z. 160-175). Gruppierte Listen
(Kommissionierlisten Liste **und** Summiert, vermutlich auch Beschichtung/Vormontage) haben je Gruppe eine
Kopfzeile mit **einer** `<td colspan>`-Zelle. Schiebt ein Anwender die gesperrte `hauptfa`-Spalte von
Position 0 weg und blendet dann die nun **erste physische** Spalte aus, trifft `cells[0]` die colspan-Zelle
— die ganze Gruppen-Kopfzeile inkl. PDF-Link verschwindet.

## Vorschlag

In `column-preferences.js` Zeilen mit `td[colspan]` (wie `table-filter.js` es bereits tut:
`if (row.querySelector('td[colspan]')) return;`) beim Ein-/Ausblenden überspringen. Gemeinsames JS — alle
Listen mit Gruppen-Kopfzeile und Spaltenkonfiguration regressionsprüfen.

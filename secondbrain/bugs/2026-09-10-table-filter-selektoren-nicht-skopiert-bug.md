---
type: bug
title: "table-filter.js: querySelectorAll('tbody') und ('thead tr:first-child th') sind nicht auf direkte Kinder skopiert — verschachtelte Tabellen werden mitsortiert"
status: offen
severity: gering
created: 2026-09-10
affected_code:
  - "IdealAkeWms/wwwroot/js/table-filter.js — sortTable: `_table.querySelectorAll('tbody')`; getPhysicalIndex: `_table.querySelectorAll('thead tr:first-child th')`"
  - "IdealAkeWms/Views/ProductionOrders/Index.cshtml:194-215 (verschachtelte HeadVariants-Tabelle im Kombigeraete-Block, <tbody> bei 201)"
spec: "[[2026-09-10-fa-liste-ausbau-matchcode-spec]]"
---

## Symptom

In der FA-Liste `/ProductionOrders` im **hierarchischen** Modus sortiert ein Klick auf **„FA Nr.",
„Kunde", „Prio", „AB-Nr." oder „Montage-Abt."** auch die **Varianten-Zeilen einer
Kombigeraete-Gruppe** um — nach einer Spalte, die mit der inneren Tabelle nichts zu tun hat.

Die Varianten-Liste steht danach in einer Reihenfolge, die niemand angefordert hat.

## Ursache

`sortTable` iteriert `_table.querySelectorAll('tbody')`. Der Selektor ist **nicht auf direkte
Kinder beschraenkt** und erfasst deshalb auch das `<tbody>` der verschachtelten
`HeadVariants`-Tabelle, die im Kombigeraete-Block einer Gruppe steckt.

Deren Zeilen haben **sieben** `<td>` und **kein** `colspan` — der `td[colspan]`-Filter, der die
Gruppen-Kopfzeilen aussortiert, greift dort also nicht. Der Vergleicher
`a.querySelectorAll('td')[colIndex]` findet bei den **kleinen** Spaltenindizes der Aussentabelle
(`order-number`=1, `customer`=3, `prio`=4, `ab-nummer`=5, `montage-abteilung`=6) tatsaechlich
Zellen und vergleicht deren Text.

> [!warning] Die naheliegende Entlastung stimmt nicht
> Die erste Einschaetzung im Umsetzungslauf lautete, `compareRows` liefere fuer die inneren Zeilen
> ohnehin `0`. Das gilt **nur** fuer `colIndex >= 7`. Dass der Fehler im Alltag nicht auffaellt,
> liegt allein daran, dass `konstruktions-termin` (7) und der Default-Sort `picking-date` (20) ueber
> dieser Grenze liegen — nicht daran, dass der Vergleich neutral waere.

Derselbe Selektor-Fallstrick steckt in `getPhysicalIndex`
(`querySelectorAll('thead tr:first-child th')` zieht die sieben inneren `<th>` mit ein). Dort ist er
**derzeit** wirkungslos, weil diese `<th>` kein `data-col-key` tragen — also eine latente, nicht
eine aktive Fehlfunktion.

## Einordnung

**Vorbestehend, nicht durch die Matchcode-/K1-Aufgabe verursacht.** Die `tbody`-Schleife kam mit
`37e8752` (Etappe 6, der Fix gegen „nur die erste Gruppe wird sortiert"), die verschachtelte Tabelle
mit `86cdc26`. Beide stammen aus **demselben Epic** wie dieser Befund, liegen aber vor ihm.

**Schwere: gering, rein kosmetisch.** `appendChild` haelt jede Zeile in ihrem **eigenen** `tbody`,
jede Varianten-Zeile traegt ihre sieben Felder selbst. Es gehen keine Daten verloren, und es
entstehen **keine falschen Zuordnungen** — der gefaehrliche Fall „ein Wert rutscht unter die falsche
Ueberschrift" tritt nicht ein.

`Views/Tracking/Index.cshtml` hat ebenfalls ein verschachteltes `<tbody>`, ist aber **nicht**
betroffen: Die Tabelle traegt kein `th[data-filterable]`, `init()` bricht bei
`_headers.length === 0` ab. Die dreistufige Arbeitsgang-Reihenfolge kann also nicht verwuerfelt
werden.

## Loesungsvorschlag

Die Selektoren auf direkte Kinder skopieren — `:scope > tbody` bzw. das Aequivalent fuer den
`thead`-Selektor. **Beide Stellen gemeinsam**, weil es derselbe systemische Fehler ist.

> [!important] Eigener Task mit eigenem Regressionsnachweis
> `table-filter.js` haengt an rund 30 Listen. Dieser Befund wurde bewusst **nicht** in den Commit
> `0f14850` mitgenommen, obwohl er dort auffiel: Jener Griff hatte seinen Regressionsnachweis schon
> fuer den `table-sorted`-Hook und [[2026-08-20-fehlerprotokoll-anzeige-epic-ab]] B-2 verbraucht.
> Eine Aenderung an der Selektor-Skopierung veraendert das Sortierverhalten verschachtelter Tabellen
> und braucht ihren **eigenen** Nachweis — je Tabelle erhoben, nicht je View-Datei.
> Das ist dieselbe Regel, nach der B-2 ueberhaupt erst mitkommen durfte.

**Naechster guenstiger Zeitpunkt:** der naechste ohnehin faellige Griff in `table-filter.js`.
Kandidaten, die ebenfalls warten: der `console.warn`-Nachbau fuer weitere stille Pfade, falls noch
welche auftauchen.

## Test

FA-Liste im hierarchischen Modus, eine Gruppe mit **Kombigeraet** (mehrere Auftragskoepfe,
`IsAmbiguous`): Varianten-Tabelle aufklappen, Reihenfolge der Varianten notieren, dann auf „Kunde"
klicken. **Erwartet nach dem Fix:** Die Varianten-Reihenfolge bleibt unveraendert, nur die Sub-FA-
Zeilen der Gruppen sortieren sich. Gegenprobe: eine Liste mit genau einem `<tbody>` (z. B.
`/Articles`) verhaelt sich unveraendert.

## Bezug

[[2026-08-12-tabellen-sortierung-nur-erste-gruppe-bug]] (der Etappe-6-Fix, der die Schleife einfuehrte)
· [[2026-08-20-fehlerprotokoll-anzeige-epic-ab]] (B-2, im selben Commit behoben)
· [[2026-09-10-fa-liste-ausbau-matchcode-umsetzung]]

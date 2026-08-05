---
type: bug
title: "Bewegungshistorie: Spaltenfilter \"Bewegungsart\" (sowie Datum/Zeit und Menge) filtert nicht — stiller No-Op liefert Vollmenge"
status: offen
severity: mittel
created: 2026-08-05
affected_code:
  - IdealAkeWms/Data/Repositories/StockMovementRepository.cs (ApplyMovementColumnFilter, Zeilen 507-537)
  - IdealAkeWms/Views/StockMovements/Index.cshtml (Tabellen-Header, Zeilen 70-81)
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
spec: "[[2026-08-05-wms-bugs-improvements-teil-4-spec]]"
---

## Symptom

In der Bewegungshistorie (`/StockMovements/Index`) liefert der **Spaltenfilter** in der
Kopfzeile der Spalte „Bewegungsart" die **gesamte, ungefilterte Liste** statt einzugrenzen: Gibt
man in den Spaltenfilter „ausbuchung" ein, werden trotzdem auch **Einbuchungen** (und alle anderen
Bewegungsarten) angezeigt. Ein korrekter Contains-Filter dürfte „Einbuchung" gerade **nicht**
zurückliefern — dass er es tut, belegt, dass der Filter gar nicht greift. Der Anwender glaubt, die
Liste auf eine Bewegungsart eingegrenzt zu haben, sieht aber weiterhin alles — genau die
Fehl-Wahrnehmung, die das Backlog vermeiden will.

Dasselbe gilt (bisher unbemerkt) für die Spaltenfilter der Spalten **„Datum/Zeit"** und **„Menge"**:
auch sie sind als filterbar markiert, filtern aber nicht. Drei der sieben als filterbar markierten
Spalten sind stille No-Ops.

## Reproduktion

1. Bewegungshistorie öffnen (`/StockMovements/Index`), Datenbestand mit gemischten Bewegungsarten
   (Ein- und Ausbuchungen).
2. In der **Spaltenkopf-Filterzeile** der Spalte „Bewegungsart" `ausbuchung` eingeben und filtern.
3. Ergebnis: Es werden **auch Einbuchungen** angezeigt — die Liste ist unverändert (Vollmenge).
4. Analog Spalte „Datum/Zeit" und Spalte „Menge": jede Eingabe im Spaltenfilter bleibt wirkungslos,
   die Liste bleibt vollständig.

(Screenshots des Menschen aus der Schranke-1-Antwort: `Pasted image 20260805150518.png`,
`Pasted image 20260805150605.png` — belegen die Eingabe „ausbuchung" mit weiterhin sichtbaren
Einbuchungen.)

## Root Cause

Die Tabelle ist im Server-Mode-Spaltenfilter (`data-server-column-filter="true"`,
`IdealAkeWms/Views/StockMovements/Index.cshtml:70`) und markiert **sieben** Spalten als filterbar
(`Index.cshtml:73-79`): `datetime`, `article`, `quantity`, `storage-location`, `movement-type`,
`user`, `production-order`.

Der serverseitige Handler `StockMovementRepository.ApplyMovementColumnFilter`
(`IdealAkeWms/Data/Repositories/StockMovementRepository.cs:507-537`) behandelt im `switch (key)`
aber nur **vier** Keys: `article`, `storage-location`, `user`, `production-order`. Alle übrigen
Keys — insbesondere `movement-type`, `datetime`, `quantity` — fallen in den Default-Zweig
`_ => q` (Zeile 535) und werden **unverändert** durchgereicht: ein **stiller No-Op**. Der Filter
liefert damit die Vollmenge statt zu filtern, ohne Fehler oder Hinweis.

Doppelter Befund:
1. Fehlende `switch`-Zweige für `movement-type`, `datetime`, `quantity` in
   `ApplyMovementColumnFilter`.
2. Überzählige `data-filterable`/`data-col-key`-Markierungen an den drei betroffenen `<th>` in
   `Index.cshtml`, die dem Anwender einen wirksamen Spaltenfilter suggerieren, den der Server nicht
   bedient.

Für die Bewegungsart existiert bereits ein **exakter** dedizierter Mechanismus: das Dropdown
„Bewegungsart" der Filterkarte (`Index.cshtml:36-45`, `filterMovementType`) filtert per Enum-Wert;
für das Datum existiert die `dateFrom`/`dateTo`-Filterkarte. Der zusätzliche (kaputte)
Text-Spaltenfilter ist dadurch redundant.

## Fix / Verweis

Siehe [[2026-08-05-wms-bugs-improvements-teil-4-spec]]. Gewählte, minimal-riskante Lösung:
`data-filterable`/`data-col-key` an den `<th>` für `movement-type`, `datetime`, `quantity` in
`Index.cshtml` **entfernen** (kein serverseitiger Handler-Ausbau), da für Bewegungsart und Datum
bereits bessere dedizierte Filter existieren und ein Text-Contains auf Menge kaum sinnvoll ist.

Folge-Aufgabe (Klassen-Audit): Alle weiteren Server-Spaltenfilter-Tabellen der App auf denselben
No-Op prüfen — jede als `data-col-key` markierte Spalte muss einen passenden `switch`-Zweig im
zugehörigen `Apply…ColumnFilter` haben, sonst ist der Filter wirkungslos.

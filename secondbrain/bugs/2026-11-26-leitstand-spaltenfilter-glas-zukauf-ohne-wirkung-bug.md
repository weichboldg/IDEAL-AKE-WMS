---
type: bug
title: "Leitstand: Spaltenfilter Glas, Zukauf, Status und Kommissionierer haben ein Filterfeld, filtern aber nichts"
status: offen
severity: mittel
created: 2026-11-26
affected_code:
  - IdealAkeWms/Views/PickingLeitstand/Index.cshtml:190-191, :197, :206 (data-filterable an glass, purchase, status, picker); :189 coating-part OHNE data-filterable
  - IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs:343-373 (ApplyLeitstandColumnFilter, kein Case für glass/purchase/status/picker -> _ => null = No-Op)
  - IdealAkeWms/Controllers/PickingLeitstandController.cs:94-103 (Memory-Filter nur Datum, VK-VA, matchcode), :804-815 (LeitstandMemoryText, sonst string.Empty)
source_backlog: "[[2026-11-26-leitstand-spaltenfilter-glas-zukauf-ohne-wirkung]]"
spec: "[[2026-11-26-leitstand-spaltenfilter-glas-zukauf-ohne-wirkung-spec]]"
---

## Symptom

Im Kommissionier-Leitstand (`/PickingLeitstand`) haben die Spalten Glas, Zukauf, Status und (bei `KommissionierungMitZuweisung`) Kommissionierer
ein Filterfeld. Ein Wert dort (ENTER) lädt die Seite mit `colf_<key>=…` neu, die Liste bleibt aber ungefiltert - ohne Hinweis. QA-Befund F-2 der
Spec [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]], vorbestehend. Beide Standorte (flacher und
hierarchischer Zweig nutzen dieselbe View und denselben Filterbau).

## Reproduktion

1. `/PickingLeitstand` (AKE oder IDEAL), Filterfeld der Spalte „Glas“ → `x` eingeben, ENTER.
2. URL enthält `colf_glass=x`, die Liste zeigt weiter alle Zeilen (auch keine leere Liste).
3. Dasselbe mit „Zukauf“ (`colf_purchase`), „Status“ (`colf_status`, z. B. `erledigt`) und „Kommissionierer“ (`colf_picker`).

## Root Cause

- Der Server-Filter-Modus (`data-server-column-filter="true"`, `Index.cshtml:158`) schickt jeden Filterwert an den Server
  (`table-filter.js:61-73`), clientseitig wird nicht gefiltert.
- Der Controller nimmt Datum-, VK-VA- und Matchcode-Keys als Memory-Filter heraus (`PickingLeitstandController.cs:94-103`); alle anderen gehen an
  `BuildLeitstandQuery` → `ApplyLeitstandColumnFilter` (`ProductionOrderRepository.cs:164-180`). Dort gibt es für `glass`, `purchase`, `status`,
  `picker` keinen Case; `_ => null` lässt die Abfrage unverändert (`:370-372`, Kommentar „Unbekannter Key → No-Op“ `:341`).
- Die Werte liegen nicht in `ProductionOrder`, sondern in `ProductionOrderPickingStatus` (Glas/Zukauf/Kommissionierer) bzw. werden erst in
  `MapItem` gebildet (Status = `IsDone || IsDonePicking` + `PickingStatus`, `PickingLeitstandController.cs:142`, `:149`; Zelle
  `_PickingLeitstandRow.cshtml:171-190`). Im Nur-HauptFA-Modus sind Glas/Zukauf zusätzlich zusammengefasst (`LeitstandHauptFaZusammenfassung.cs:28-34`).
- Gleiches Muster wie der frühere No-Op bei „Übergeordnete Sub-FA“ (`ProductionOrderRepository.cs:362`, „war filterbar, aber ohne Mapping“).

Abweichung von der Backlog-Notiz (belegt): Lack-T (`coating-part`) hat **kein** Filterfeld (`Index.cshtml:189` ohne `data-filterable`);
dafür ist Kommissionierer (`picker`, `:206`) betroffen. Andere Listen sind nicht betroffen: Suche `data-col-key="(glass|purchase)"` in `Views/`
trifft nur den Leitstand; `status`/`picker` anderer Listen haben eigene Getter (z. B. `PickingController.cs:98-99`).

## Fix / Verweis

Spec [[2026-11-26-leitstand-spaltenfilter-glas-zukauf-ohne-wirkung-spec]] (Entwurf).

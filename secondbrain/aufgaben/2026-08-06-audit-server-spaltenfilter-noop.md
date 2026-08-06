---
type: aufgabe
title: Klassen-Audit — Server-Spaltenfilter ohne Handler (stille No-Ops)
status: offen
created: 2026-08-06
herkunft: "Folge-Aufgabe aus [[2026-08-05-wms-bugs-improvements-teil-4-spec]] (SOLLTE S2)"
---

# Klassen-Audit: Server-Spaltenfilter ohne Handler

Teil 4 hat in der **Bewegungshistorie** drei `<th data-filterable data-col-key="…">` gefunden, fuer
die `ApplyMovementColumnFilter` **keinen** `switch`-Zweig hatte → die Filter lieferten stumm die
Vollmenge (gefaehrlicher No-Op). Das ist ein **Muster**, kein Einzelfall.

## Auftrag
Alle Server-Mode-Listen (`data-server-column-filter="true"`) der App pruefen: **jede**
`data-col-key`-Spalte MUSS einen Eintrag in der zugehoerigen `ColumnMap`/`ApplyXxxColumnFilter`-Logik
haben. Fehlt er → entweder Handler ergaenzen oder `data-filterable`/`data-col-key` entfernen (wie in
Teil 4). Inventar der Server-Filter-Tabellen: `secondbrain/codebase/module.md`, Abschnitt
„Inventar: Tabellen im Server-Filter-Mode".

## Kandidaten (zu verifizieren)
- FaWorklist, WarehousePicking, WarehouseRequisitions, ProductionOrders, StockOverview,
  MovementHistory (Teil 4 erledigt), SageBookingQueue, … — je View die `<th>`-Keys gegen die
  Controller-`ColumnMap` abgleichen.

Kein Blocker fuer laufende Arbeit; eigener kleiner Dev-Lauf, sobald priorisiert.

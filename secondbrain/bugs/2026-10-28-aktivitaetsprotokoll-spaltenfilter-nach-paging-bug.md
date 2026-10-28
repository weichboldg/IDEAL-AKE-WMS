---
type: bug
title: "Aktivitäts-Protokoll: Spaltenfilter wirkt nur auf die geladene Seite, Trefferzahl und Seitenleiste ungefiltert"
status: behoben
severity: mittel
created: 2026-10-28
fixed: 2026-10-28
fixed_in: "v1.70.0, Bündel feature/2026-08-07-ideal-teile-1-5, Commits badbedcc, d7dc3d03 (nicht gemergt, wartet Schranke 2)"
affected_code:
  - IdealAkeWms/Controllers/SyncLogController.cs (Index, vorher :36-39 und :53)
  - IdealAkeWms/Data/Repositories/SyncLogRepository.cs (GetPagedAsync, vorher :43-48)
source_backlog: "[[2026-10-16-aktivitaetsprotokoll-filter-und-namen]]"
spec: "[[2026-10-16-aktivitaetsprotokoll-filter-und-namen-spec]]"
---

## Symptom

Im Aktivitäts-Protokoll (`/SyncLog`) findet ein Spaltenfilter (z. B. „Meldung enthält …“) nur Einträge der gerade angezeigten
Seite. Ältere Einträge sind praktisch nicht auffindbar; Trefferzahl und Seitenleiste zeigen die ungefilterte Gesamtzahl, Folgeseiten
sind nach dem Filter leer. Der CSV-Export filtert nur die neuesten 5000 Zeilen, und der Kappungshinweis erscheint auch bei wenigen
Treffern, sobald das Protokoll mehr als 5000 Zeilen hat. Backlog-Meldung: „konnte nichts finden“.

## Reproduktion

1. `/SyncLog` mit mehr als einer Seite Einträge öffnen.
2. In „Meldung“ einen Text filtern, der nur auf Seite 2 oder später steht.
3. Ergebnis (vor v1.70.0): Seite 1 leer bzw. ohne den Eintrag, Trefferzahl unverändert.

## Root Cause

`SyncLogController.Index` holte mit `_repo.GetPagedAsync(...)` genau eine Seite (Count/Skip/Take im Repository vor jedem
Spaltenfilter) und filterte diese Seite danach im Speicher (`ColumnFilterHelper.Apply` mit `SyncLogColumnMap`); `TotalCount` kam aus dem
ungefilterten Count. Die Tabelle war dennoch als server-gefiltert markiert (`data-server-column-filter="true"`), der Browser filterte
also selbst nicht. Prüfung aller anderen Server-Listen (Spec, Abschnitt „Andere server-gefilterte Listen“): nur diese Liste betroffen.

## Fix / Verweis

v1.70.0: Spaltenfilter in SQL vor Count/Skip/Take (`SyncLogRepository.ApplyColumnFilter`, Muster
`StockMovementRepository.ApplyMovementColumnFilter`), Seiten-Guard `page < 1 → 1`, In-Memory-Filter im Controller entfernt.
Tests `SyncLogColumnFilterTests`, manuell TS-102. Spec [[2026-10-16-aktivitaetsprotokoll-filter-und-namen-spec]].
Gleiches Fehlerbild ohne Spaltenfilter bei den Fehlteilen: [[2026-10-28-fehlteile-nur-meine-werkbaenke-trefferzahl]] (eigener Eintrag).

---
type: uebersicht
title: "Uebersicht: WMS Bugs & Improvements (2026-08-05) — 8 unabhaengige Teil-Specs"
slug: 2026-08-05-wms-bugs-improvements-uebersicht
status: Gemerged
created: 2026-08-05
updated: 2026-08-05
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
---

## Zweck

Die Backlog-Datei `[[2026-08-05-WmsBugs&Improvements]]` enthält eine formlose Sammlung von acht
fachlich unabhängigen Punkten aus dem Lager-/Bestellwesen-Bereich (Mix aus Bugfixes und
Feature-Wünschen). Sie wird deshalb — analog einem `split: true`-Auftrag — in acht einzeln
mergbare Teil-Specs zerlegt. Jeder Teil ist für sich testbar, freigebbar und mergbar; es gibt
**keine** Abhängigkeiten zwischen den Teilen (`depends_on` ist bei allen leer).

Ein Bug-Record wurde zusätzlich angelegt für den in Teil 1 verifizierten Bug:
[[2026-08-05-einbuchung-fa-hinweis-bewegungen-statt-bestand-bug]]. Für Teil 4 (vermuteter Bug
„Ausbuchungen nicht sichtbar") wurde **kein** Bug-Record angelegt, weil die Code-Verifikation den
Verdacht nicht bestätigen konnte — siehe dortige offene Rückfragen.

## Teile in Reihenfolge des Backlogs

| #   | Teil-Spec                                        | Typ                                 | Zweck                                                                                                                                                                            |
| --- | ------------------------------------------------ | ----------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | [[2026-08-05-wms-bugs-improvements-teil-1-spec]] | Bugfix                              | FA-Lagerplatz-Hinweis bei der Einbuchung wertet künftig den tatsächlichen Bestand statt des reinen Bewegungssaldos aus (betrifft auch StockOverview-FA-Filter + Tracking-Modal). |
| 2   | [[2026-08-05-wms-bugs-improvements-teil-2-spec]] | Feature                             | Neue Massen-/Mehrfachartikel-Einbuchung: Lagerplatz + FA einmalig, beliebig viele Artikel-Zeilen mit je eigener Menge.                                                           |
| 3   | [[2026-08-05-wms-bugs-improvements-teil-3-spec]] | Feature                             | Scan-Button für WA-Strichcode im Bewegungshistorie-Filter, kürzt den gescannten Wert auf die ersten 7 Zeichen (ignoriert „-"/„_"-Ergänzungen).                                   |
| 4   | [[2026-08-05-wms-bugs-improvements-teil-4-spec]] | Verifikation (kein bestätigter Bug) | Prüft den Verdacht „Ausbuchungen werden in der Bewegungshistorie standardmäßig nicht angezeigt" — am Code nicht nachvollziehbar; stellt Rückfragen zur Reproduktion.             |
| 5   | [[2026-08-05-wms-bugs-improvements-teil-5-spec]] | Feature (klein)                     | Einbuchungsformular: Standardmenge 1 statt 0.                                                                                                                                    |
| 6   | [[2026-08-05-wms-bugs-improvements-teil-6-spec]] | Feature/Bugfix                      | „Lagerplatz ausbuchen": FA-Spalte in der Bestandstabelle + Aktivierung der bisher wirkungslosen FA-Ausbuchung (Feld filtert künftig tatsächlich die auszubuchenden Artikel).     |
| 7   | [[2026-08-05-wms-bugs-improvements-teil-7-spec]] | Feature                             | Lager-/Glasbestellung: Kommentarfunktion auf Bestellungs-Ebene (sichtbar in Eingehenden Listen) + Dummy-Artikel-Anlage bei unbekannter EK-Nummer.                                |
| 8   | [[2026-08-05-wms-bugs-improvements-teil-8-spec]] | Feature                             | FA-Abarbeitungsliste: personalisiert abspeicherbarer Standard-Filter auf „Bezeichnung 1" (analog dem bestehenden Artikelgruppen-Filter-Muster der BOM-Ansicht).                  |

## Querschnitts-Hinweise für die Freigabe (Schranke 1)

- Jede Teil-Spec hat einen eigenen, unabhängigen Fragenblock „Offene Rückfragen" mit
  vorbereitetem „Freigabe-Antworten"-Abschnitt — die Teile können unabhängig voneinander
  freigegeben, umgesetzt und gemergt werden, in beliebiger Reihenfolge.
- Teil 1 und Teil 6 berühren beide `StockMovementRepository`-nahe Bestandsberechnungen
  (Bewegungssaldo vs. tatsächlicher Bestand je FA); Teil 6 stellt dazu eine explizite offene
  Rückfrage (Konsistenzwunsch), blockiert aber nicht auf Teil 1.
- Teil 7 und Teil 8 bringen je eine additive EF-Migration mit (`WarehouseRequisition.Comment`
  bzw. `User.DefaultFilterFaWorklistDescription1`); beide konkurrieren zum Umsetzungszeitpunkt
  um die nächste freie `SQL/XX_*.sql`-Nummer (main ist aktuell bei `SQL/83`) — die tatsächliche
  Nummer ist beim jeweiligen Dev-Lauf neu zu ermitteln, nicht aus dieser Spec zu übernehmen.
- Teil 7 enthält die gewichtigste offene Rückfrage des gesamten Backlogs: ob die
  Dummy-Artikel-Anlage das bestehende Rollenkonzept (Artikel-Neuanlage ist heute
  `masterdata`/`admin`-exklusiv) bewusst für `lagerbestellung`/`glasbestellung`/`stock`/`picking`
  öffnet — diese Entscheidung sollte vor der Freigabe von Teil 7 explizit getroffen werden.

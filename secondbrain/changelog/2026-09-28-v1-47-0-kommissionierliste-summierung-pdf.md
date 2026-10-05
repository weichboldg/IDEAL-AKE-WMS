---
type: changelog
version: 1.47.0
date: 2026-09-28
---
# v1.47.0 — Kommissionierliste: Summiert als Standard, Druck/PDF nur sichtbare Spalten

Umsetzung von [[2026-09-25-kommissionierliste-summierung-pdf-spec]] im **Bündel-**Worktree
`feature/2026-08-07-ideal-teile-1-5`, Umsetzungsnotiz [[2026-09-28-kommissionierliste-summierung-pdf-umsetzung]].
Anlass [[2026-09-23-kommissionierliste-summierung-pdf]]. Setzt v1.46.0
([[2026-09-25-v1-46-0-kommissionierung-nur-hauptfa]]) voraus. **Keine Migration.**

**Warum:** Der Kommissionierer braucht im Alltag die Summe je Artikel und Ziel seines Auftrags, nicht jede
Einzelposition — und ein Ausdruck, der andere Spalten zeigt als der Bildschirm, führt zu Fehlgriffen. Die
Summiert-Ansicht existierte seit v1.31.0, war aber an eine Pflicht-KW gebunden und hatte keinen Druck.

## Kernpunkte

- **Summiert ist Standard-Einstieg** (beide Menüpunkte). KW optional — ohne KW alle HauptFA (AK N2d der
  Teil-3-Spec überholt). Summierschlüssel unverändert `(HauptFA, Artnr, Kommissionieren)`, je HauptFA.
- **Matchcode + Hauptlagerplatz** als Anzeigespalten (`g.First()`, Schlüssel unverändert).
- **Gruppen-Paging je HauptFA** (Muster `FaHierarchyListBuilder.Build`): keine Gruppe reißt über Seiten;
  `Pagination.TotalCount` zählt Gruppen.
- **Druck/PDF = sichtbare Spalten beim Klick:** `print-visible-columns.js` hängt `visibleColumns` an
  `a[data-print-link]`; ohne Parameter (direkte URL, Mittelklick) Rückfall auf die gespeicherte Präferenz;
  gesperrte Spalten nie ausgeblendet; `WarehousePickingPrintLayout` unverändert.
- **Eigener Druck/PDF der Summe** (`PrintSummiert`/`PdfSummiert`) mit Banner „Summierte Ansicht“; PDF-Fehler
  führt zurück auf `Summiert` mit denselben Parametern.
- **Umschalter** Liste↔Summiert über EINE Stelle (`BuildToggleQuery`): Ziel, KW (in der Liste wirkungslos,
  nur Rückweg — Freigabe-Antwort 5) und gemeinsame Spaltenfilter; **nicht** der Mengenfilter.
- **ViewKey `FaHierarchyKommissionierSummiert` registriert** (vorher lehnte die Präferenz-API ihn ab).
- **Drift-Guard** `KommissionierSpaltenKonsistenzTests`: ColumnDefinitions, `#column-config`,
  `<th data-col-key>`, `ShowCol` je View gegeneinander.

## Commits

`323120d8` (Plan), `ab2beed2` (Service), `e407596d` (ColumnDefinitions), `1fb2b59d` (Controller), `1977a819`
(Views/JS/Menü), `58d50738` (Drift-Guard), `e447a12c` (Version, Doku). Merge-Commit: folgt mit dem Bündel.

## Deploy-Risiken

Nur Web-Publish, keine Migration, kein Service. **Vor der Abnahme:** Einheiten-Prüfschritt TS-80.1 auf der
Sage-DB IDEAL ([[fallstricke]] §18) — Treffer melden, nicht still weiterbauen. Anwender-Verhaltensänderung:
Menü öffnet die Summe statt der Liste; Druck/PDF je Anwender verschieden (sichtbare Spalten).

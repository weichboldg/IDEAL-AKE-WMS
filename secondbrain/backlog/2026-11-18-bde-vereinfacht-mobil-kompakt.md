---
typ: backlog
status: in-spec
created: 2026-11-18
herkunft: "Termin IDEAL 2026-11-18 10:00 (Walter, Benjamin u. a.), Notizen Gerald 17:08"
standort: IDEAL
---
# BDE vereinfacht: weniger Leerraum, mobil bedienbar, kleinere Filter

Bezug: [[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]] (Testbereit, Nachtrag Kontrast v1.99.36).

## Wunsch (Termin)
- Whitespace reduzieren.
- Bedienbarkeit auf Mobile optimieren.
- Filter kleiner.

## Hinweise Koordination
- Rein Darstellung (Views BDE-Teileliste, `wwwroot/css/bde.css`), keine Daten-/Schema-Änderung.
- Zielkonflikt: Die Spec verlangt Trefferflächen ≥ 44 px (Fertigungs-Terminal, Spec Z. 294). Kompakter heißt: weniger Abstände und
  Kopfbereich, Filterkarte einklappbar – nicht kleinere Knöpfe.
- Vorschlag: Filterkarte auf Handy standardmäßig eingeklappt mit Kurzanzeige der aktiven Filter, Tabelle auf schmalen Bildschirmen als
  Kartenliste oder mit weniger Spalten, Abstände in `bde.css` reduzieren. Vorher Screenshots am :88 (Handy-Breite 390 px) als Ist-Stand.
- Ohne Rückfrage spezifizierbar (`/spec`).

> [!note] Zurückgestellt (Gerald 2026-11-18 19:20: „Bde mobil hinten anstellen“).

> [!info] Aufgegangen im Epic [[2026-11-25-bde-masken-vereinheitlichung]] (Etappe 1), Gerald 2026-11-25: „wird in diesem Punkt bereits mit abgewickelt“.

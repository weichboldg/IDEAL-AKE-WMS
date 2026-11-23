---
type: changelog
version: 1.99.59
date: 2026-11-23
---
# v1.99.59 — Wareneingang zu Bestellung: Hover hellblau

Rückmeldung Gerald 2026-11-23 beim Nachtest („grau war wenig ersichtlich“), ohne eigene Spec. Lauf [[2026-11-23-goal-lauf-mengenfelder-tabellen-beschichtung]].
Commit `b139ff5b`. Keine Migration, nur Web (CSS). TS-184. Nicht gemergt.

## Kernpunkte

- `site.css`: `.table.we-positionen { --bs-table-hover-bg: var(--ake-light-blue) }` – nur die WE-Positionstabelle; `table-danger` behält Rot, andere Tabellen behalten Bootstrap-Grau.
- Wächter `ViewKeyDriftGuardTests.WareneingangPositionen_DreiQuellenGleich_Gesperrt_FokusZeile` erwartet jetzt drei `.we-positionen`-Regeln.
- Offen (Rückfrage): Die **Fokus-Zeile** (in der man tippt) bleibt grau (`--bs-table-active-bg`) mit dunkelblauem Balken – nicht Teil der Rückmeldung.

## Deploy

Nur Web. Kein Risiko.
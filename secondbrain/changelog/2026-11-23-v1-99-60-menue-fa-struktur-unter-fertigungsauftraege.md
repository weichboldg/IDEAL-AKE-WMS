---
type: changelog
version: 1.99.60
date: 2026-11-23
---
# v1.99.60 — Menü: FA-Struktur unter „Fertigungsaufträge“

Wunsch Gerald 2026-11-23, ohne eigene Spec. Lauf [[2026-11-23-goal-lauf-mengenfelder-tabellen-beschichtung]]. Commit `bcb7a357`, QA `d5866351` (qa-agent, grün). Keine Migration, nur Web (`_Layout.cshtml`). TS-185. Nicht gemergt.

## Kernpunkte

- Eigenständiger Menüpunkt „FA-Struktur“ entfernt; Eintrag im Dropdown „Fertigungsaufträge“ nach „FA-Liste“, Dropdown-Bedingung `ZeigtEines` und Aktiv-Markierung (`FaHierarchy`) erweitert.
- Rechte unverändert: `NavigationSicht` `"FaListe" or "FaStruktur" => faLesen`; Startseiten-Kachel unverändert.
- Offen: Fotos `docs/anwender/bilder/18-0x` zeigen evtl. noch die alte Menüleiste; Paket 15 enthält v1.99.60 nicht.

## Deploy

Nur Web. Kein Risiko.

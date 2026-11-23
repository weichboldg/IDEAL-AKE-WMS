---
type: changelog
version: 1.99.59
date: 2026-11-23
---
# v1.99.59 — Wareneingang zu Bestellung: Hover hellblau

Rückmeldung Gerald 2026-11-23 beim Nachtest („grau war wenig ersichtlich“), ohne eigene Spec. Lauf [[2026-11-23-goal-lauf-mengenfelder-tabellen-beschichtung]].
Commits `b139ff5b`, `c9624562` (Fokus-Zeile), `b5354e51` (Anmeldemaske). Keine Migration, nur Web (CSS). TS-184. Nicht gemergt.

## Kernpunkte

- `site.css`: `.table.we-positionen { --bs-table-hover-bg: var(--ake-light-blue) }` – nur die WE-Positionstabelle; `table-danger` behält Rot, andere Tabellen behalten Bootstrap-Grau.
- Wächter `ViewKeyDriftGuardTests.WareneingangPositionen_DreiQuellenGleich_Gesperrt_FokusZeile` erwartet jetzt drei `.we-positionen`-Regeln.
- Fokus-Zeile (in der man tippt) `--bs-table-active-bg: var(--ake-very-light-blue)` (#D9F4FF), Balken bleibt (Gerald 2026-11-23).
- `Account/Login.cshtml`: unten in der Karte „IDEAL-AKE WMS v@AppVersion.Version“, beide Zweige (TS-184.5). Version damit ohne Anmeldung lesbar – geringes Risiko (Intranet), Entscheidung Gerald.
- Offen: Links „Offener Bedarf“ (#0D6EFD) auf Hellblau 2,95:1 (QA B-1), Foto 05-06 neu.

## Deploy

Nur Web. Kein Risiko.
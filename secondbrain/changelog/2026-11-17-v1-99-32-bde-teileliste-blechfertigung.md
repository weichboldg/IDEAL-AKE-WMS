---
type: changelog
version: 1.99.32
date: 2026-11-17
---
# v1.99.32 — BDE-Teileliste Blechfertigung (+ Auslieferung Beschichtung E2/E4)

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commits `6704fe2a` (E1), `21c7eb4a` (E2), `3fc1f4ed` (E3), `82e2a312` (E4), `b2e1de37` (E5),
`e85fdf35` (E6), `2d81bce9` (E7), `c7c968fe` (E9; E8 laut Spec nur späterer Marker). Spec [[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]], ADR [[0030-teilmeldung-mit-fachlichem-schluessel-neben-workoperation]],
Lauf [[2026-11-17-goal-lauf-beschichtung-bde-mapping]]. Nicht gemergt; Merge-Commit offen.

## Kernpunkte

- Werkbank-Option „BDE-Teileliste“; Terminal-Ansicht Teile/Baugruppen je Kürzel, Melden einzeln/Block/Summiert, Baugruppe meldet ihren AG mit.
- Rücknahme bis Schichtende (ohne Schichtkalender bis Tagesende), Admin-Liste Teilmeldungen mit Storno.
- Filter je Werker gespeichert (KW relativ), Spalte „Beschichtet“, Erkennung V1 (keine Vater-AGs aus Blatt-Kürzeln).
- Mit dieser Version ausgeliefert: Beschichtung E2/E4 ([[2026-11-16-v1-99-25-beschichtungslogik-je-standort]]) – Termin, Tooltip und
  Lack-Haken der drei Listen aus der Beschichtungsquelle, sichtbar nur mit `ProduktionsauftragHierarchisch`; Manifest-Info
  `v1.99.32-BeschichtungListen`.

## Migration / SQL

- `20261117075856_AddBdeTeileliste` / `SQL/154_AddBdeTeileliste.sql` (additiv), FreshInstall nachgezogen.
- Einmal-Skript `SQL/Einmalig/2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste.sql` – **datenlöschend, DB-Backup vor SCHARF**.

## Deploy

1. SQL/154 vor Web und Dienst.
2. Option an den Start-Werkbänken setzen (mit Fertigung abstimmen).
3. Einmal-Skript: Trockenlauf → DB-Backup → SCHARF → zweiter Trockenlauf `TREFFER: 0`.
4. Fertigung/AV über die Beschichtungs-Listen informieren.

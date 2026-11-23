---
type: changelog
version: 1.99.55
date: 2026-11-23
---
# v1.99.55 — Tabellen: Umsortieren in 7 Listen

Spec [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-2-spec]], Lauf [[2026-11-23-goal-lauf-mengenfelder-tabellen-beschichtung]]. Commit `f2fb8b43`. Keine Migration, kein SQL, nur Web (Version in beiden `AppVersion.cs`). TS-180. Nicht gemergt.

## Kernpunkte

- `supportsReorder: true` in `#view-config` und `ColumnDefinitions` für Artikel, Lagerplätze, Kreditoren, Debitoren, Offene Bestellungen, Wareneingang zu Bestellung, Bestellvorschläge.
- `ViewKeyDriftGuardTests.ViewKey_Registriert` prüft jetzt den Gleichlauf `#view-config` ↔ Katalog ([[fallstricke]]: wirksam ist nur `#view-config`).

## Deploy-Risiken

- Wareneingang: Bindung über `zeilen[i].*`, Umsortieren ändert keine Zuordnung – Buchung nach Umsortieren trotzdem im UAT prüfen (TS-180.2).

---
type: changelog
version: 1.9.0
date: 2026-05-05
---
# v1.9.0 — Sage Lagerplatz-Sync (Phase 1)

- Sage-Lagerplatz-Stammdaten (Code, Bereich/Zone, Bezeichnung) werden in den WMS-Stamm
  synchronisiert. Gate `Sync:LagerplaetzeEnabled`.
- Neue Spalte `Source` (Manual/Sage): bei Sage-Records sind Code/Zone/Bezeichnung im UI **gesperrt**
  — Aenderungen erfolgen ausschliesslich in Sage.
- In Sage deaktivierte Plaetze werden im WMS deaktiviert (`IsActive`), mit Toggle „Auch inaktive
  zeigen" und Inaktiv-Badge. Das ist **nicht** dasselbe wie das user-gesteuerte `IstBuchbar`.
- **Neue Tabelle `SyncLogs`** — der Ursprung des spaeteren Aktivitaets-Protokolls: Konflikte
  (manuell vs. Sage gleicher Code), Deaktivierungen, Lauf-Zusammenfassungen.

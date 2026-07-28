---
type: changelog
version: 1.20.0
date: 2026-06-08
---
# v1.20.0 — Feingranulare Berechtigungen

- **Read/Edit-Split** in den Stammdaten-Sichten ueber die neue Rolle `masterdata_read`
  (Migration `AddMasterDataReadRole`): Class-Level liest, schreibende Actions verschaerfen.
- **Lager-Worklist nur fuer Lager-Mitarbeiter:** `WarehousePicking` und `MissingPartsLager` via
  `[RequireLagerProcessingAccess]` — Komm.-Picker explizit ausgeschlossen.
- **Admin-only-Block:** Benutzer, Rollen, Arbeitsplaetze, Einstellungen, Aktivitaets-Protokoll und
  Schichtkalender.
- Neue hand-gepflegte Anwender-Uebersicht `/Users/RoleOverview` — muss bei jeder Filter-Aenderung
  mitgepflegt werden.
- Neue View-Helper `HasMasterDataReadAccessAsync` und `CanProcessLagerAsync`.
- **Bugfix-Welle danach:** Articles und StorageLocations bekamen den Split nachtraeglich
  (Scope-Gap); kein Default-Fehlteil mehr bei Ist-Menge 0; Filter-Bugs behoben (Date-Picker-Event,
  redundantes `applyFilters` im Server-Mode); **Model-Binder-Bug** — leere `int[]`-Inputs
  verschwinden und verschieben die Index-Zuordnung, wodurch Mengen auf falschen Positionen landeten.

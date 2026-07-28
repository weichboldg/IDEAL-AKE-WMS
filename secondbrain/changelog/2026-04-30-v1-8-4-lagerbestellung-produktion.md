---
type: changelog
version: 1.8.4
date: 2026-04-30
---
# v1.8.4 — Lagerbestellung aus der Produktion

Neuer End-to-End-Workflow: die Produktion erfasst Lagerartikel als Bestellliste fuer ihre Werkbank,
das Lager kommissioniert und schliesst mit Ist-Mengen je Position ab.

- **Erfasser-Sicht** mit Werkbank-Auto-Resolution (1 Werkbank → automatisch, 0 → Hinweis
  „Stammdaten pflegen", N → Dropdown), Artikelsuche, Submit/Storno mit RowVersion-Schutz.
- **Lager-Sicht** („Eingehende Listen") mit Detail, Ist-Mengen, A4-Pickup-Druck, Abschliessen und
  Storno mit Grund.
- Submit-/Storno-Mail asynchron ueber den SyncWorker (bis 15 min Verzoegerung), Storno mit
  `[STORNO]`-Prefix im Betreff.
- Neue Tabellen `WarehouseRequisitions` + `...Items` (`SQL/53`), neue AppSetting
  `DefaultLagerbestellempfaengerId` (leer = Submit blockt), Gate
  `Sync:WarehouseRequisitionEmailEnabled`.
- Menue „Bestellungen" wird Dropdown.

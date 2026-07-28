---
type: changelog
version: 1.26.0
date: 2026-07-22
---
# v1.26.0 — FA-Zusatzinfos (Sage)

- Read-only Satellit `ProductionOrderExtraInfo` (Kaeltemittel, Ventil, AusfuehrungEZ, Maschine,
  SageStatus), **Migration 81**, additiv.
- `FaZusatzinfoSyncService` (Gate `Sync:FaZusatzinfoEnabled`, Default aus). View-Guard: fehlt die
  Sage-View, endet der Lauf regulaer mit Warn-Zeile. Kein Loeschen, kein Reconcile.
- Anzeige: FaCompletion-Pseudo-Reiter „ALLGEMEIN", FaWorklist (5 Spalten sichtbar), FA-Liste +
  Leitstand (default versteckt via neuer `defaultHidden`-Mechanik). Nebenfix: ViewKey „FaWorklist"
  in `ColumnDefinitions.GetByViewKey` registriert — vorher antwortete die Prefs-API mit 400.
- **Fold 2 — Auto-Erledigt + BDE-Sperre:** Sage-Status verpackt/abgeholt setzt
  `PickingStatus.IsDonePicking` (einweg; Cap `Sync:FaZusatzinfoAutoDoneMaxPerRun`, Default 100 →
  sonst kein Write + Warn + Fehlermail). BDE-Guard `EnsureOrderNotPackedAsync` in Start/Resume.
  Kein Schema-Change.
- **Erstlauf-Pflicht:** DryRun fahren und Count `erledigt-gesetzt` kontrollieren — der Sync aendert
  FA-Status automatisch. Recovery-SQL in `../../docs/TESTSZENARIEN.md` Kap. 55.
- Gemerged in `main` (`47bd69f`). Produktiv-Deploy und Manual-UAT offen →
  [[2026-07-deploy-v1-25-0]].

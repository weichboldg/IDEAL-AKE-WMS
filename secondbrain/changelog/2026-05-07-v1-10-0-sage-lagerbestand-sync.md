---
type: changelog
version: 1.10.0
date: 2026-05-07
---
# v1.10.0 — Sage Lagerbestand-Sync (Phase 2)

- Automatischer Bestand-Abgleich je (Artikel, Lagerplatz) mit Sage. Abweichungen erzeugen
  **Korrektur-Buchungen** statt stiller Updates — die WMS-Bewegungshistorie bleibt vollstaendig.
- Neue Bewegungsarten `SageEinbuchung` / `SageAusbuchung` mit eigenem Badge und Filter-Option.
  **Merke:** jede `MovementType`-Erweiterung trifft 6 Aggregations-Stellen.
- Neues Notiz-Feld auf `StockMovements`, bei Sage-Korrekturen automatisch gefuellt
  (`Sage-Korrektur: WMS=X, Sage=Y, Diff=Z`).
- **Strikte Filterung:** nur `Source=Sage`-Lagerplaetze werden korrigiert; manuelle Plaetze,
  fehlende Artikel und inaktive Plaetze werden uebersprungen und als Warnung protokolliert.
- DryRun empfohlen fuer den ersten Lauf. Gates `Sync:LagerbestandEnabled`,
  `Sync:LagerbestandIntervalMinutes`. Phase 1 ist Voraussetzung.

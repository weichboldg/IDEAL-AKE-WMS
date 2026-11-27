---
type: changelog
version: 1.99.58
date: 2026-11-23
---
# v1.99.58 — Kommissionierauftrag: eigene Spalte „Menge IST“

Spec [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-5-spec]], Lauf [[2026-11-23-goal-lauf-mengenfelder-tabellen-beschichtung]]. Commit `0f853eed` + QA-Fix `b79a4b1a`. Keine Migration, kein SQL, nur Web (Version in beiden `AppVersion.cs`). TS-183. Nicht gemergt.

## Kernpunkte

- `Picking/Bom.cshtml`: Spalte `ist-menge` „Menge IST“ rechts neben „Menge“, nur mit Auftrag; Eingabe Ist-Menge dorthin verschoben, übertragene Zeilen zeigen die gebuchte Ist-Menge. Registriert in th, `#column-config`, `ColumnDefinitions.Bom`.
- QA-Fix: Leerzeilen-colspan zählt die Spalte mit.

## Deploy-Risiken

- TS-183.2 auf DEV mit HTTP 400 abgebrochen – Ursache Cookie-Kollision 8081/8082 (DEV-Umgebung, [[2026-10-29-dev-umgebung-lokal]]), kein Code-Befund; im UAT wiederholen.

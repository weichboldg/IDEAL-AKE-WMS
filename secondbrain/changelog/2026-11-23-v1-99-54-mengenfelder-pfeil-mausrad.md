---
type: changelog
version: 1.99.54
date: 2026-11-23
---
# v1.99.54 — Mengenfelder: Pfeil/Spinner ±1, Mausrad ohne Wirkung

Spec [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-1-spec]], Lauf [[2026-11-23-goal-lauf-mengenfelder-tabellen-beschichtung]]. Commit `96b60901`. Keine Migration, kein SQL, nur Web (Version in beiden `AppVersion.cs`). TS-179. Nicht gemergt.

## Kernpunkte

- `site.js` (global): bei Bedienung eines Zahlenfelds mit step < 1 kurz `step="any"` (pointerdown/focusin), bei focusout zurück; Mausrad nimmt dem Feld den Fokus statt den Wert zu ändern.
- Server-Validierung unverändert.

## Deploy-Risiken

- Global für alle Zahlenfelder – Sichtprüfung in Dialogfeldern und Ganzzahlfeldern (TS-179.4/179.5).
- QA-Hinweis: Feld mit pointerdown ohne Fokus behält `step="any"` (nur Browser-Prüfung betroffen).

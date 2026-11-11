---
type: changelog
version: 1.99.1
date: 2026-11-11
---
# v1.99.1 — Update-Skript: Nachbesserung aus der QA v1.99.0

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commit `9071dcf7` (auf v1.99.0 `55ec218a`). Spec
[[2026-11-11-update-skript-prueft-und-fuehrt-aus-mit-bestaetigung-spec]] (Freigabe-Nachtrag v1.99.1). Nicht gemergt, nicht gepusht.

## Kernpunkte

- `Pruefung/RollenBeschichtungsauftrag` fuer beide Firmen (DEV-IDEAL/IDEAL-Test haben die Rollen nicht, nur IDEAL-Prod).
- Eine Regel `Test-Faellig` fuer Anzeige und Ausfuehrung: nicht faellige Daten-Skripte erscheinen als „nicht faellig“.
- Backup-Angebot: leerer Ordner = ueberspringen statt Abbruch; SCHARF und uebernehmen nur mit Backup aus demselben Lauf.

## Deploy-Risiken

Keine Migration. Nur Skripte, Version und Changelog-Karte. Fluss B verlangt jetzt ein Backup aus demselben Lauf (vorher reichte „liegt vor“).

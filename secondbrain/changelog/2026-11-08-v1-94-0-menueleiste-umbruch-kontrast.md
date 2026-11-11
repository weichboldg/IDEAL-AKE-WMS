---
type: changelog
version: 1.94.0
date: 2026-11-08
---
# v1.94.0 — Korrigiert: Menüleiste und Fehlteile-Link

Bündel `feature/2026-08-07-ideal-teile-1-5`, Lauf [[2026-11-08-nachbesserungen-vor-buendel-merge-goal-lauf]]. Spec [[2026-11-08-seiten-querscrollen-navigationsleiste-spec]].
Commits `2efdb023`. Nicht gemergt (Schranke 2).

## Kernpunkte

- `_Layout.cshtml`: `navbar-nav … flex-wrap`, Benutzername `flex-shrink-0 text-nowrap`; Fehlteile-Link `text-warning-emphasis`.
- Nachtrag A1: SQL/108 löscht zusätzlich die verwaiste Zeile `StammdatenADGruppe` (`6987e2bd`).

## Deploy-Risiken

Nur Web; SQL/108 (Nachtrag) läuft mit dem nächsten Deploy, idempotent.

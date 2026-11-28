---
type: changelog
version: 1.99.90
date: 2026-11-28
---
# v1.99.90 — Kommissionierauftrag: Filter und „Zurücksetzen“ bleiben im Auftrag

Bugfix [[2026-11-26-kommissionierauftrag-filter-springt-auf-bom-spec]], Lauf-Notiz [[2026-11-28-buendel-review-kommissionierung-goal-lauf]]. Worktree `feature/2026-11-28-buendel-review-kommissionierung`, Commit `48a2ece3`.
**Keine Migration, keine Einstellung.** TS-210. Gemergt in main (Merge-Commit `50952006`, 2026-11-28).

## Kernpunkte

- `Views/Picking/Bom.cshtml`: Filter-Formular und „Zurücksetzen“ zielen bei `Model.Auftrag != null` auf `Picking/Auftrag` statt `Picking/Bom`
  (vorher sprang ein Filter im Kommissionierauftrag in die Stückliste des FA).

## Deploy-Risiken

- Nur Web (Dienst nur `AppVersion`). Offen im Manual-UAT: Stichprobe FaCompletion/Werkbank (gleiche View ohne Auftrag).
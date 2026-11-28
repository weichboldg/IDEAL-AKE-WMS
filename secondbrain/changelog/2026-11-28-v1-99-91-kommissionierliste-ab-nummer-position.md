---
type: changelog
version: 1.99.91
date: 2026-11-28
---
# v1.99.91 — IDEAL-Kommissionierliste: Position zur AB-Nummer

[[2026-10-06-kommissionierliste-ab-nummer-position-spec]], Lauf-Notiz [[2026-11-28-buendel-review-kommissionierung-goal-lauf]]. Worktree `feature/2026-11-28-buendel-review-kommissionierung`, Commit `a57973f7`.
**Keine Migration, keine Einstellung.** TS-211. Nicht gemergt.

## Kernpunkte

- Gruppenkopf `Views/FaHierarchyKommissionierListen/Index.cshtml`: „AB <ABNr> / <Pos>“ (Pos nur wenn gefüllt).
- Druck `Print.cshtml`: neue Spalte „Pos“ nach „AB-Nr.“. Hilfe-Eintrag „seit v1.99.91“.

## Deploy-Risiken

- Nur Web. Offen im Manual-UAT: PDF-Download und Fall „Pos leer“.
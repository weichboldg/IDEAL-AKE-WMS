---
type: aufgabe
status: in-arbeit
created: 2026-11-24
worktree: .claude/worktrees/2026-08-07-ideal-teile-1-5
branch: feature/2026-08-07-ideal-teile-1-5
---
# /goal-Lauf 2026-11-24 – Tabellen-Zeilenfarben global

Auftrag Gerald 2026-11-24: `/goal-run` für [[2026-11-23-tabellen-streifen-hover-ci-hellblau-spec]] im Bündel-Worktree.
Vorprüfung: Status Freigegeben, worktree/branch leer, 9/9 Freigabe-Antworten, freigabe_entscheidung A, kein Blocker – bestanden.

| Spec | Typ | SQL-Block | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-23-tabellen-streifen-hover-ci-hellblau-spec]] | Spec | – | 1.99.62 | InUmsetzung | – | – |

## Freigabe-Abweichungen vom Rumpf (Antworten gehen vor)

- R5 Nein: Hover **ohne** `@media (hover: hover)`; AK 16 / TS-187.12 entfallen bzw. erwartet „Hover bleibt bis zum nächsten Tippen“.
- R9 Ja: Vault-/enaio-Symbol-Kontrast wird **hier** behoben (Rumpf hatte Out-of-Scope).
- R2: zusätzlich `fa-head-table` als Ausnahme.

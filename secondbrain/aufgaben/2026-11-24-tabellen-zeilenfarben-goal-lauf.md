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
| [[2026-11-23-tabellen-streifen-hover-ci-hellblau-spec]] | Spec | – | 1.99.62 | Testbereit | `7fbdd2b7` | – |

## Freigabe-Abweichungen vom Rumpf (Antworten gehen vor)

- R5 Nein: Hover **ohne** `@media (hover: hover)`; AK 16 / TS-187.12 entfallen bzw. erwartet „Hover bleibt bis zum nächsten Tippen“.
- R9 Ja: Vault-/enaio-Symbol-Kontrast wird **hier** behoben (Rumpf hatte Out-of-Scope).
- R2: zusätzlich `fa-head-table` als Ausnahme.

## QA (qa-agent, 2026-11-24)

[[2026-11-23-tabellen-streifen-hover-ci-hellblau-spec]] → **Testbereit**. Build 0 Fehler; Tests Web 3492/1 übersprungen/0, Service 790/0/0.
DEV AKE + IDEAL aus dem Worktree (`-OhneDienst`), Fußzeile v1.99.62, Exit 0. Pixelmessung (PNG + System.Drawing): Streifen #F2F2F2, Hover #A1D9F7,
Fokus #D9F4FF + Balken #052C65, `table-danger` Hover #E5C7CA, Gruppenkopf #E5E5E5, Link #052C65, `text-danger` #58151C, Vault #1C6EA4, enaio #A35400,
Baum-Hover #F5F5F5 unverändert. Review: keine blockierenden Befunde (N-1 bis N-5 Hinweise, Details in der Spec). Kein Publish/Paket (Auftrag).
Offen für den Menschen: 11 Punkte der Restprüfung in der Spec (u. a. Druck, Bäume „Hervorheben“, Leitstand, Modals, Touch).

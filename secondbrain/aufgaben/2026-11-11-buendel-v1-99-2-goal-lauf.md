---
type: aufgabe
status: offen
created: 2026-11-11
---
# /goal-Lauf 2026-11-11 abends: Update-Skript v1.99.2, Vormontage Abteilung, Wareneingang Teil 2

Buendel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Start v1.99.1 `f79ba893`.
Auftrag Gerald 2026-11-11 (Chat). Grenzen: kein Lauf gegen akenet01, idealweb01, idealsql2, AKESQL20; Sage nur lesend ausser dem einen
freigegebenen Test-Beleg (vorher Gerald Beleginhalt und Aufruf zeigen, erst nach OK senden); kein Push, kein Merge.

| Spec | Typ | SQL-Block | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-11-update-skript-prueft-und-fuehrt-aus-mit-bestaetigung-spec]] (nur Freigabe-Nachtrag 20:40) | Spec | – | 1.99.2 | **Testbereit** (QA 2026-11-11) | `81b95ac2`, Brain `e58f4d2d`, QA `8e5ebb56` | – |
| [[2026-11-11-vormontage-spalte-abteilung-hauptfa-spec]] | Spec | – | 1.99.3 | **Testbereit** (QA 2026-11-11) | `d2d4ed46`, Brain `321d546a`, QA `73adcc85` | – |
| [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]] (E1–E4; E5 blockiert bis P-W9) | Epic | 143 | 1.99.4 | **Testbereit** (QA 2026-11-12, E1–E4 + E6) | `f45412d4`, `aa4655f8`, `f7dc750e`, `053cb28d`, `a449e814`, `0d265005`, `2d09489a`, QA `152ab6f1`, `bd9b58d0`; Brain `acfffcd1`, `f75849c9` | E5 blockiert bis P-W9 (Gerald) |

- Update-Skript: Status war Testbereit; der Auftrag nennt die Spec ausdruecklich mit dem Freigabe-Nachtrag 20:40 und ersetzt damit die Freigabe.
- Am Ende: 5 AKE-DEV-Sync-Schalter wieder gesetzt (2026-11-12).
- Schreibtest 2026-11-12 nach Geralds OK: genau ein Beleg 2027-398675 (ake_TEST2026), Lieferschein laut Gerald in Sage angekommen; Rueckbau Gerald.
- Folge (Gerald 2026-11-12): Memo je Position (`Artikelmemo`) als v1.99.5 – **Testbereit** (`c155b1b2`, QA `3ab77a71`); zweiter Test-Beleg 2027-398676 nach OK. Sync-Schalter danach erneut gesetzt.

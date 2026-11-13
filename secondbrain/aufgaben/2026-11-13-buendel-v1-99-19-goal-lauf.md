---
type: aufgabe
status: offen
created: 2026-11-13
---
# /goal-Lauf 2026-11-13 (Abend): Lieferverzug-Nachbesserung, Einkauf Bestellvorschläge (Epic)

Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Start v1.99.18 `d9e4c89e`.
Auftrag Gerald 2026-11-13 (Chat, /goal-run). Reihenfolge: Lieferverzug → Bestellvorschläge. Versionen ab 1.99.19, TS ab TS-136, SQL ab 148
in Lauf-Reihenfolge. Freigabe-Nachtrag 2026-11-13 16:27 geht je Spec dem Rumpf vor; Keyuser-Antworten 2026-11-13 verbindlich (Bestellvorschläge).
Bestellvorschläge: E0 lesend über DEV selbst (Abweichung = Blocker), Schalter Default aus, Rolle `einkauf`, E5 (Dispo-Stapel) blockiert – kein POST.
Nach jedem Build beide Apps nach `<Worktree>\publish`.
Grenzen: nichts an Sage schreiben, keine Mails an externe Adressen (Lieferverzug nur Testbetrieb/Pickup auf DEV), keine Prod-DB, kein Merge, kein Push.

| Spec | Typ | SQL-Block | Version | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|---|
| [[2026-11-13-lieferverzug-nachbesserung-keyuser-spec]] | Spec | – | 1.99.19 | TS-128 erweitert (128.16–128.23) | InUmsetzung | | |
| [[2026-11-08-einkauf-bestellvorschlaege-spec]] | Epic | ab 148 | 1.99.20 | TS-136 | InUmsetzung | | E5 blockiert (gewollt) |

- Vorprüfung 2026-11-13: beide `status: Freigegeben` (Gerald 2026-11-13), Freigabe-Antworten vollständig (Lieferverzug 1–5, Bestellvorschläge
  1–10 + 11–13), keine Varianten-Spec, worktree/branch leer bzw. = Bündel. Nächste freie SQL im Worktree: 148 (letzte 147).
- Hinweis: Der Branch wurde vor diesem Lauf auf Geralds ausdrückliche Anweisung im Chat bis `d9e4c89e` gepusht (vor dem /goal-run-Auftrag);
  im Lauf selbst kein Push.

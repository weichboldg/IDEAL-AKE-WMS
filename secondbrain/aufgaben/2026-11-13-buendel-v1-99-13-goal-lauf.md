---
type: aufgabe
status: offen
created: 2026-11-13
---
# /goal-Lauf 2026-11-13: Bugfix AKE-Regression, SData Teil 2, Wareneingang Teil 3

Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Start v1.99.12 `56906459`.
Auftrag Gerald 2026-11-13 (Chat, /goal-run). Reihenfolge laut Auftrag (Bugfix → SData Teil 2 → WE Teil 3); Versionen ab 1.99.13,
TS ab TS-130, SQL ab 147 in Lauf-Reihenfolge. Freigabe-Nachtrag 2026-11-13 08:39 gilt je Spec vor dem Rumpf.
Grenzen: nichts an Sage schreiben (SData nur GET mit orderBy über DEV), keine Mails an externe Adressen, kein Merge, kein Push.
WE Teil 3: E0 (P-W8 lesend, Abweichung = Blocker), E1, E3; **vor E2 anhalten** – fertigen Request zeigen, auf Geralds OK warten.
Nach jedem Build beide Apps nach `<Worktree>\publish`.

| Spec | Typ | SQL-Block | Version | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|---|
| [[2026-11-12-befunde-ake-regression-dev-spec]] | Spec (Bugfix) | – | 1.99.13 | TS-130 | InUmsetzung | – | – |
| [[2026-11-13-sdata-teil-2-userfelder-stueckliste-spec]] | Spec | ab 147 (Gate-Zeile löschen) | 1.99.14 | TS-131 | InUmsetzung | – | – |
| [[2026-11-11-wareneingang-teil-3-rahmenabrufe-spec]] | Spec | – | 1.99.15 | TS-132 | InUmsetzung | – | E2 wartet auf Gerald (gewollt) |

- Vorprüfung 2026-11-13: alle drei Freigegeben (Gerald 2026-11-13), alle Freigabe-Antworten ausgefüllt, keine Varianten-Spec ohne
  Entscheidung (Bugfix Punkt 2 = Variante A per Antwort 1), worktree/branch = Bündel.

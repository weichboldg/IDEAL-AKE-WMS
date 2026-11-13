---
type: aufgabe
status: offen
created: 2026-11-13
---
# /goal-Lauf 2026-11-13 (2): Updater zweiter Trockenlauf, doppelte Kommissionier-Prio, UTC-Schreiber auf Lokalzeit

Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Start v1.99.15 `e92894c9`.
Auftrag Gerald 2026-11-13 (/goal). Reihenfolge laut Auftrag; Versionen ab 1.99.16, TS ab 133, SQL ab 148 in Lauf-Reihenfolge
(Freigabe-Nachtrag 13:58 je Spec, geht dem Rumpf vor). Grenzen: nichts an Sage schreiben, keine Mails an externe Adressen, kein Lauf
gegen Zielsysteme (Update-Skript nur gegen DEV), kein Merge, kein Push. Nach jedem Build beide Apps nach `<Worktree>\publish`.
Vorgänger-Lauf: [[2026-11-13-buendel-v1-99-13-goal-lauf]]. DEV-Browser: Browser 2 (Gerald).

| Spec | Typ | SQL-Block | Version | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|---|
| [[2026-11-13-updater-zweiter-trockenlauf-altbestand-spec]] | Spec (Bugfix) | – | 1.99.16 | TS-133 | InUmsetzung | – | – |
| [[2026-11-13-doppelte-kommissionier-prio-nach-ruecknahme-spec]] | Spec (Variante B) | – | 1.99.17 | TS-134 | InUmsetzung | – | – |
| [[2026-11-13-utc-schreiber-auf-lokalzeit-spec]] | Spec | – | 1.99.18 | TS-135 | InUmsetzung | – | – |

- Vorprüfung 2026-11-13: alle drei Freigegeben (Gerald 2026-11-13), Freigabe-Antworten vollständig, Prio = Variante B (`freigabe_entscheidung`),
  worktree/branch = Bündel. Updater-Nachtrag erweitert den Umfang (`-NurPruefen`-Endlosschleife ohne Eingabe, DEV-Ziele in `ziele.psd1`).

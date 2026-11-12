---
type: aufgabe
status: offen
created: 2026-11-12
---
# /goal-Lauf 2026-11-12: Wareneingang E5 (v1.99.7), Kommissionierung Matchcode (v1.99.8)

Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Start v1.99.6 `67322b85`
(der Auftrag nennt v1.99.5 `c155b1b2`; seitdem kam v1.99.6 Positionsmemo dazu). Auftrag Gerald 2026-11-12 (Chat): je Spec nur der letzte
Freigabe-Nachtrag. Grenzen: kein POST nach Sage (kein weiterer Schreibtest; sollte der Code senden wollen: STOPP), kein Lauf gegen
Zielsysteme, kein Push, kein Merge.

| Spec | Typ | SQL-Block | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]] (Freigabe-Nachtrag 2026-11-12 „E5 Variante A …“) | Epic | – (keine Migration) | **1.99.7** (Nachtrag nennt 1.99.6, war belegt) | InUmsetzung → QA | `f8d74399`, `af0a75f2` | – |
| [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]] (Freigabe-Nachtrag 2026-11-12 Matchcode) | Spec | – | **1.99.8** (Nachtrag nennt 1.99.7) | InUmsetzung → QA | `557ea63b` | – |

- Beide Specs standen auf Testbereit; der Auftrag nennt sie ausdrücklich mit dem jeweiligen Freigabe-Nachtrag und ersetzt damit die Freigabe.
- Versionen: fortlaufend ab der nächsten freien (1.99.6 = Positionsmemo).
- E5-Live-Test „Memo gefunden“ (2026-11-12 11:38, nur lesend): DEV-Dienst gestoppt, WE 4/7 auf Fehler, Sende-Lauf mit Sende-Client, der immer wirft →
  beide „an Sage übergeben“ mit 2027-398675/398676, **0 Sendeversuche**; Ausgangsstand wiederhergestellt, Dienst wieder gestartet.
- DEV-Absicherung (außerhalb des Repos): `D:\Dev\scripts\dev-absicherung-ake.sql`/`-ideal.sql` setzen zusätzlich `SageWareneingangBelegAktiv`
  auf false bzw. legen ihn an (Backups `*.bak-20261112`). Erledigt QA-Befund Q-8.

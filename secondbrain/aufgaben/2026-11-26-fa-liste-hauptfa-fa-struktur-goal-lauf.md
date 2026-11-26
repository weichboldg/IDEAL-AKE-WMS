---
type: aufgabe
status: in-arbeit
created: 2026-11-26
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-26: FA-Liste nur HauptFA + FA-Struktur neu (Variante D)

Auftrag Gerald 2026-11-26 (`/goal` + `/goal-run`, zwei Specs, Reihenfolge laut
[[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-uebersicht]]: Teil 1 vor Teil 2).
Start-Stand Worktree: v1.99.81 (cf750e19, Tag `backup/buendel-v1.99.81-vor-fa-liste-hauptfa`), höchstes TS 202,
letztes Paket 21. Main-Tag `backup/main-vor-fa-liste-hauptfa-2026-11-26` (d653b939).

| Spec | Typ | SQL-Block | Version | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|---|
| [[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-1-spec]] | Spec | – (keine Migration) | 1.99.82 | 203 | Testbereit | 557a9062 | |
| [[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-2-spec]] | Spec | – (keine Migration) | 1.99.83 | 204 | InUmsetzung | | |

## Vorprüfung
- Beide: status Freigegeben (Gerald 2026-11-26, 77b18ac0), worktree/branch leer, Freigabe-Antworten vollständig
  (Teil 1: 1–6, Teil 2: 1–10 + freigabe_entscheidung D), Kritische Prüfung ohne BLOCKER (d29f0af8, 9440ac4b).
- Worktree sauber bei Start.
- Kopplung: `nurHauptFa`/`CountGruppenOhneHauptFaAsync` aus der (unbeantworteten) Leitstand-Spec
  [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]] — dieser Lauf führt ihn ein.

## Protokoll
- 2026-11-26 Start, beide Specs auf InUmsetzung.
- 2026-11-26 Teil 1 feature-complete im Worktree (wip 557a9062): Build 0 Fehler, Tests Web 3770 / Service 848 gruen; TS-203 + Index; naechster Schritt qa-agent Teil 1.
- 2026-11-26 qa-agent Teil 1: Build 0 Fehler, Tests 3770/1 skip + 848 gruen, DEV IDEAL + AKE v1.99.82 (Exit 0), TS-203 auf DEV OK bis auf NICHT MOEGLICH (Daten/Rechte), Publish worktree\publish 1.99.82; Befunde Q-1..Q-9 ohne Blocker, offen Q-8 Brain-Nachzug am Laufende. Status Testbereit.
- 2026-11-26 Brain Teil 1 nachgezogen (a2e4a58a: Changelog v1.99.82, feature-map, Fallstrick Matchcode = Q-8 Teil Brain).
- 2026-11-26 Teil 2 feature-complete im Worktree (wip ce04f887): Build 0 Fehler, Tests Web 3774 / Service 848 gruen; TS-204 + Index; naechster Schritt qa-agent Teil 2.

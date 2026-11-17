---
type: aufgabe
status: offen
created: 2026-11-17
quelle: Gerald, Chat 2026-11-17 (/goal)
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-17: Beschichtung E2/E4, BDE vereinfacht (Teileliste Blechfertigung), Arbeitsgang-Mapping

Auftrag Gerald 2026-11-17: drei Teile nacheinander im Bündel-Worktree (Stand v1.99.31, `d3a00101`); je Teil Build + Tests grün, Commit,
qa-agent; am Ende Publish und DEV AKE (8081) + IDEAL (8082). Grenzen: nichts an Sage schreiben, keine Einstellungen auf Test/Prod, kein Merge,
kein Push, keine Specs nach freigegeben/ verschieben. Bei Blockern vermerken und weitermachen.

Nummern: Beschichtung bleibt Changelog-Version 1.99.25 (keine Schema-Änderung, AppVersion bleibt); BDE vereinfacht = **1.99.32** (Spec nannte
1.99.28, belegt durch Sicherheit), SQL/154 frei (151/153 reserviert, 155 belegt); Arbeitsgang-Mapping = Changelog-Version 1.99.27, SQL/153
(reserviert). Die AppVersion steigt nur einmal auf 1.99.32 (BDE) – Arbeitsgang trägt das reservierte Label 1.99.27 im Changelog, ohne die
AppVersion zurückzudrehen.

## Übersicht

| Spec | Typ | SQL | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-16-beschichtungslogik-je-standort-spec]] E2/E4 | Epic | – | 1.99.25 (Auslieferung 1.99.32) | InUmsetzung – QA Teilumfang E2/E4 grün | `2336dced` E2, `b4972700` E4 | E5: H2 (P-A5/P-A3/P-A6) und H3 offen |
| [[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]] | Spec | 154 | 1.99.32 | **Testbereit** | `6704fe2a`…`c7c968fe`, F1 `5ced750f` | – |
| [[2026-11-16-arbeitsgang-mapping-sage-arbeitsgangnummer-spec]] | Spec | 153 | 1.99.27 | in Arbeit | | |

## Protokoll

- 2026-11-17: Lauf gestartet.
- 2026-11-17: Beschichtung E2 (`2336dced`) und E4 (`b4972700`) gebaut; QA Teilumfang grün (Web 3024/0/1, Dienst 682/0/0; DEV AKE+IDEAL
  TS-141..143 OK, Regression 140/144 OK, Einstellungen/Testdaten zurückgesetzt). Befunde R-1 (Manifest-Info nur Text im 1.99.25-Eintrag) und R-2
  (Versions-Bump 1.99.32 vor Publish) gehen in den BDE-Teil. Status bleibt InUmsetzung (E5).
- 2026-11-17: BDE vereinfacht gestartet (1.99.32, SQL/154).
- 2026-11-17: BDE-Teileliste E1–E7, E9 gebaut (`6704fe2a`…`c7c968fe`), QA **Testbereit** (Web 3104/0/1, Dienst 685/0/0; DEV AKE+IDEAL v1.99.32,
  TS-152 inkl. N1 6 parallele POSTs → 1 Meldung; Einmal-Skript nur Trockenlauf `TREFFER: 119`; DEV-Daten zurück). Befund F1 (Umlaute) behoben
  `5ced750f`; F2 (Kontrast `.btn-success`/`.btn-secondary`, global, alt) → [[2026-11-17-kontrast-btn-success-secondary]]. Brain `abe66087`.
- 2026-11-17: Arbeitsgang-Mapping gestartet (Changelog 1.99.27, SQL/153).


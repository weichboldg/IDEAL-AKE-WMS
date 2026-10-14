---
typ: notiz
spec: "[[2026-10-13-briefkopf-fusszeile-word-vorlage-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Briefkopf-Fußzeile nach Firmen-Word-Vorlage (v1.59.0)

Spec [[2026-10-13-briefkopf-fusszeile-word-vorlage-spec]] · **direkt im Bündel**. Fußbereich der IT-Vorlage auf jeder
Druckseite (Bauweise V3), „Seite X von Y" nur bei genau einem Dokument, Muster nach Word-Vorlage.

## Stand

- 2026-10-14: P0–P4 an statischer Testseite + lokalem Edge gemessen (V1/V2 scheitern, V3 trägt, P3/P4 positiv im PDF).
  Code-Commit `c2d54f35` (Start `ead16fc4`). Build 0 Fehler, Web 2014 / Service 272 grün. Changelog
  [[2026-10-14-v1-59-0-briefkopf-fusszeile]], [[fallstricke]] §27, TS-92.
- 2026-10-14: QA grün → **Testbereit**, Fix `d4108f1a` (Q-1 `AppVersion.Date`), Minor Q-2..Q-5 in der Spec.
- Offen für die Abnahme: P0 Edge-Version am Server (TS-92.13), P1 Sichtprüfung Druckvorschau (TS-92.1/.4/.5), IT übernimmt
  den Fußbereich in die Server-Vorlage (ohne Fußbereich ändert sich nichts).
- Bleibt offen: [[2026-10-09-briefkopf-seitenzaehlung]] für den Sammeldruck mit mehreren HauptFA (Zählung je Dokument).

---
typ: notiz
spec: "[[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Arbeitsgang-Stammdaten vereinheitlichen

Spec [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]] · **direkt im Bündel**, Start `87db96c8` (v1.53.0). Variante U (Tabelle/Spalte/PK/Index umbenannt, UQ→IX),
Audit, Reiter „Arbeitsgänge“ (nur Admin, ohne BdeAktiv), Offset-Lookup für FA-Struktur Teil 2/3. Deploy: Web + **SQL/95**
(DB-Backup, App-Pool vorher stoppen).

## Stand

- 2026-10-12: feature-complete (`d4bb711a`, `5ac5ea16`, `b5123529`); QA läuft.
- 2026-10-12: QA fand I-1 (SQL/95 lief in SSMS nach der B1-Anomalie weiter und schrieb die History) — behoben in `d56a340e` (`SET NOEXEC`), dazu M-1..M-3; QA-Nachlauf ohne Critical/Important → **Testbereit**. Commits `d4bb711a`, `5ac5ea16`, `b5123529`, QA-Fix `d56a340e`. Build + 1795 Web- / 272 Service-Tests grün. Changelog [[2026-10-12-v1-54-0-arbeitsgang-stammdaten]].
- Schranke 2 mit dem Bündel: Prüfschritte P1–P5, TS-87 inkl. 87.9 auf drei DB-Formen. Folgeaufgabe [[2026-10-12-operationconfig-offset-leer-pruefen]].

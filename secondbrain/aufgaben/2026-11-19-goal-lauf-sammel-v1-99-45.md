---
type: aufgabe
status: in-arbeit
created: 2026-11-19
quelle: Gerald, Chat 2026-11-19 (/goal Sammellauf v1.99.45)
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-19: Sammellauf v1.99.45

Auftrag Gerald 2026-11-19: vier freigegebene Specs im Bündel-Worktree, Basis `f1747e10` (v1.99.44), gemeinsam als **v1.99.45**
(der Auftrag legt eine Version für alle vier fest; die Versionsvorschläge 1.99.46/1.99.47 in den Specs entfallen).
Je Spec Build + alle Tests grün und eigener Commit. Danach Doku (`docs/anwender` + `docs/betrieb`, Stand 1.99.45, Playwright-Fotos gegen
DEV 8081/8082; Confluence nicht, nur Kapitelliste), qa-agent synchron, DEV AKE + IDEAL, Paket 10 nach `publish\paket10-v1.99.45\` + ZIP,
ZIP nach bestandener QA auf beide Shares (Größe + SHA-256).
Grenzen: nichts an Sage, keine Einstellungen auf :88/4445/Prod, `SageFaArbeitsgaengeAnlegenAktiv` bleibt aus, kein Merge, kein Push.

## Übersicht

| Spec | Typ | SQL | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-19-artikelinfo-bestand-exakt-bug-spec]] | Bug | – | 1.99.45 | InUmsetzung | | |
| [[2026-11-19-bf-warenkorb-artikelsuche-type-ahead-bug-spec]] | Bug | – | 1.99.45 | InUmsetzung | | |
| [[2026-11-19-updater-warntext-treffer-exitcode-spec]] | Spec | – | 1.99.45 | InUmsetzung | | |
| [[2026-11-19-dashboard-kacheln-nach-kategorie-und-berechtigung-spec]] | Spec | – | 1.99.45 | InUmsetzung | | |

## Protokoll

- 2026-11-19: Lauf gestartet, Basis `f1747e10`. Vorprüfung: alle vier `Freigegeben`, worktree/branch leer, Freigabe-Antworten vollständig,
  keine BLOCKER. Nummern: Version 1.99.45 frei; SQL höchste 163 (keine Spec braucht SQL); TS höchste vergeben TS-166.

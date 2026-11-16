---
type: aufgabe
status: offen
created: 2026-11-16
quelle: Gerald, Chat 2026-11-16 (/goal + Dev-Lauf-Auftrag)
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-16: Sicherheit Anmeldung/Installer + Wareneingang-CSV

Auftrag Gerald 2026-11-16: zwei freigegebene Specs nacheinander im Bündel-Worktree (Basis v1.99.26, `5163df17`). Beide liegen unter
`specs/entwurf/` mit `status: Freigegeben` – laut Auftrag gelten sie als freigegeben. Rumpf = Auftrag, Nachträge gehen vor,
Freigabe-Antworten gelten. Grenzen: nichts an Sage, keine Einstellungen auf Test/Prod, kein Merge, kein Push. Publish am Ende.

Nummern beim Start geprüft: Version 1.99.26 → Sicherheit **1.99.28**, CSV **1.99.29** (1.99.24 nicht verwenden, 1.99.27 bleibt für
[[2026-11-16-arbeitsgang-mapping-sage-arbeitsgangnummer-spec]]). Letzte SQL 150; Sicherheit **SQL/152** (`MussKennwortAendern`);
SQL/151 nur bei P-2b-Treffern (nicht gebaut, Prüfung im Manifest); 153 reserviert Mapping. TS-139 (Sicherheit), TS-153 (CSV).
ADR 0027 (Sicherheit).

## Übersicht

| Spec | Typ | SQL | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-14-sicherheit-anmeldung-und-installer-spec]] | Spec | 152 | 1.99.28 | **Testbereit** (qa-agent) | `6447db0a`, `4559e0a2`, `4f3bf15d`, QA-Fix `8f24ec51` | QA B-2/B-3 (Haken bei Create vorausgewählt, /Users-Hinweis) zur Entscheidung; Manual-UAT-Checkliste in der Spec |
| [[2026-11-14-wareneingang-csv-erfasser-samaccountname-spec]] | Spec | – | 1.99.29 | InUmsetzung (feature-complete, QA steht aus) | `f12f46f7` | – |

## Fragen an Gerald

- vor Deploy: P-1 (Benutzer ohne Kennwort je System), P-2b (IDEAL-Prod FreshInstall-Indizes), MailSettings je Server.

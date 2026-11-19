---
type: aufgabe
status: erledigt
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
| [[2026-11-19-artikelinfo-bestand-exakt-bug-spec]] | Bug | – | 1.99.45 | Testbereit | `a1ba945d` | |
| [[2026-11-19-bf-warenkorb-artikelsuche-type-ahead-bug-spec]] | Bug | – | 1.99.45 | Testbereit | `980beee9` | |
| [[2026-11-19-updater-warntext-treffer-exitcode-spec]] | Spec | – | 1.99.45 | Testbereit | `b524d262` | |
| [[2026-11-19-dashboard-kacheln-nach-kategorie-und-berechtigung-spec]] | Spec | – | 1.99.45 | Testbereit | `0df40eeb` | |

## Protokoll

- 2026-11-19: Lauf gestartet, Basis `f1747e10`. Vorprüfung: alle vier `Freigegeben`, worktree/branch leer, Freigabe-Antworten vollständig,
  keine BLOCKER. Nummern: Version 1.99.45 frei; SQL höchste 163 (keine Spec braucht SQL); TS höchste vergeben TS-166.
- 2026-11-19: Spec 1 Artikelinfo `a1ba945d`, Spec 2 BF-Warenkorb `980beee9`, Spec 3 Update-Skript-Texte `b524d262` – je Build 0 Fehler,
  Tests grün (zuletzt Service 790, Web 3332 + 1 übersprungen). Spec 3: Betriebsdoku 03/07 und Belegzeilen in docs/betrieb mitgezogen.
- 2026-11-19: Spec 4 Startseite/Menü `0df40eeb` (frontend-design geladen; ADR [[0034-navigation-menue-und-kacheln-aus-einer-quelle]]),
  Release `defa0c05` (beide AppVersion 1.99.45, Changelog, Hilfe, TS-167–170, zwei Info-Einträge `nachlauf.psd1`). Build 0 Fehler,
  Tests Service 790, Web 3390 + 1 übersprungen.
- 2026-11-19: DEV AKE 8081 und IDEAL 8082 per `dev-deploy.ps1 -Quelle <Worktree>`: Exit 0, Fußzeile v1.99.45. Keine Einstellung geändert.
- 2026-11-19: Doku `947bb014` – neu `docs/anwender/18-startseite-und-menue.md` mit 5 Fotos (Playwright gegen DEV, nur angesehen; 390 px ohne
  Querscrollen), Kapitel 03 (Lagerplatz-Text), 14 (Vorschläge), README; `docs/betrieb` 00-index (Stand v1.99.45) und 04 5.5.
  Brain: Changelog [[2026-11-19-v1-99-45-sammellauf]], ADR 0034, [[fallstricke]] §56–58, feature-map, Testindex, Backlogs erledigt.
- 2026-11-19 qa-agent (synchron): alle vier Specs **Testbereit**, keine blockierenden Befunde; QA-Fix `f3cdd143` (F1 Manifest-Info
  `v1.99.45-BfWarenkorbVorschlaege`, F2 Changelog-Text: Kommissionierwagen nur aus dem Lagerplatz-Text der Lager-/Glasbestellung, F3
  `Invoke-Pruefung` ohne widersprüchliche Reparaturzeile bei Nur-Anzeige). Tests 790 + 3390/1 skip, DEV AKE+IDEAL neu deployt.
  Doku-Nachzug zu F2: `c39a9423`. Offen für Manual-UAT: TS-170 mit anderen Benutzern/abgeschalteten Modulen, TS-169
  Vorschlagsliste + Scanner, TS-167 Update-Skript-Lauf auf Test.
- 2026-11-19: **Paket 10** `publish\paket10-v1.99.45\` + ZIP aus `c39a9423` (beide DLLs 1.99.45; Web, Dienst, `SQL/`, `deploy/`,
  `publish-info.txt`). ZIP 236859820 Byte, SHA-256 `3E853578F17CD958ACABEBD5B28FE320FC6A22249A496013DF914913E9941AE9`.
  Hinweis: der qa-agent hat zusätzlich lose Ordner direkt unter `publish\` (IDEALAKEWMSWeb, …WebService, SQL, deploy, publish-info.txt)
  erzeugt – nicht angefasst (Auftrag: andere Dateien in publish\ nicht anfassen).
  - `\AKENET01.ake.at\Installsourcen\paket10-v1.99.45.zip`: 236859820 Byte, SHA-256 gleich – OK.
  - `\IDEALWEB01.ideal.ideal-ake.at\InstallSourcen\paket10-v1.99.45.zip`: 236859820 Byte, SHA-256 gleich – OK.
- 2026-11-19: Lauf abgeschlossen – alle vier Specs Testbereit, wartet Schranke 2 (Manual-UAT + Merge). Kein Merge, kein Push, nichts an Sage.

## Kapitel für spätere Confluence-Übernahme

- Anwender: 18 Startseite und Menü (neu), 03 Lagerbestellung/Glasbestellung, 14 Blechfertigung, README (Übersicht + Stand)
- Betrieb: 00-index, 03 Installation/Update (Meldungsmuster), 04 Konfiguration 5.5, 07 Troubleshooting, 01/02/06/10 (Belegzeilen)

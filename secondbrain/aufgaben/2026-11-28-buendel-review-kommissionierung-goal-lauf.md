---
typ: lauf-notiz
status: Testbereit
created: 2026-11-28
worktree: ".claude/worktrees/2026-11-28-buendel-review-kommissionierung"
branch: "feature/2026-11-28-buendel-review-kommissionierung"
basis: "main 96cf2f8d (v1.99.89)"
---
# /goal-Lauf 2026-11-28: Kommissionierung + Review-Befunde (4 Specs)

Auftrag Gerald 2026-11-28: alte Worktrees lokal bereinigt, danach goal-run in neuem Worktree von main.
Reihenfolge = Auftrag. Versionen/TS fortlaufend ab main 1.99.89 / TS-209 (die vorläufigen Nummern der Specs werden dadurch ersetzt).

| # | Spec | Typ | SQL | Version | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|---|---|
| 1 | [[2026-11-26-kommissionierauftrag-filter-springt-auf-bom-spec]] | Spec (Bugfix) | – | 1.99.90 | TS-210 | Testbereit (QA 2026-11-28, DEV AKE+IDEAL v1.99.90) | 48a2ece3 | – (B-1 erledigt: Brain-Changelog + feature-map nachgezogen) |
| 2 | [[2026-10-06-kommissionierliste-ab-nummer-position-spec]] | Spec | – | 1.99.91 | TS-211 | Testbereit (QA 2026-11-28, DEV IDEAL v1.99.91) | a57973f7 | – (PDF/Pos-leer → Mensch; H-1 AppVersion.Date) |
| 3 | [[2026-11-28-sdata-geheimnisse-review-befunde-spec]] | Spec (Sicherheit) | – | 1.99.92 | TS-212 | Testbereit (QA 2026-11-28, DEV AKE+IDEAL v1.99.92) | c9c9f888 + QA 7abbc46b | – (QA-Fix Q-1 Leerzeichen/Vollbreite im Freitext-Weg; 212.5 HTTP 200/212.7 gekürzt → Mensch, Sage-Test-Kennwort fehlt auf DEV) |
| 4 | [[2026-11-28-update-skript-review-befunde-spec]] | Spec | – | 1.99.93 | TS-213 | Testbereit (QA 2026-11-28, DEV AKE+IDEAL v1.99.93, Paket publish v1.99.93 a83d8756) | ff681409 + QA a83d8756 | – (QA-Fix Q-1: @@TRANCOUNT 0 = „ggf. festgeschrieben“, Rollback wirft nach Server-COMMIT nicht; Q-2 Reparatur-Text; 213.1/213.2/213.7/213.8 + Startkennwort → Mensch, DB-Kopie) |

Hinweis: [[2026-11-26-kommissionierauftrag-liste-nachbesserungen-spec]] (depends_on 1) ist nicht im Lauf (Entwurf, Fragen offen); ihre vorläufige Version 1.99.91 ist hier vergeben – bei Umsetzung neu nummerieren.

## Verlauf
- 2026-11-28: Vorprüfung aller 4 Specs bestanden (Freigegeben, worktree leer, Antworten vollständig, keine BLOCKER). Worktree angelegt.
- 2026-11-28: Spec 1 (48a2ece3) und Spec 2 (a57973f7) umgesetzt, QA Testbereit.
- 2026-11-28: Spec 3 (c9c9f888) umgesetzt; QA-Fix 7abbc46b (Q-1 Freitext-Weg Leerzeichen/Vollbreite, Kollation CI_AS), Testbereit.
- 2026-11-28: Spec 4 (ff681409) umgesetzt; QA-Fix a83d8756 (Q-1: nach Server-COMMIT liefert @@TRANCOUNT 0 und Rollback wirft NICHT -> „ggf. festgeschrieben“ jetzt bei @@TRANCOUNT 0; meine Abweichung im Entwurf war falsch), Testbereit.
- 2026-11-28: Doku-Commit 932c36aa (Stand + Versionszeilen, Foto 16-03 per Headless-Chrome gegen DEV IDEAL v1.99.93). Brain: Changelogs v1.99.90–93, feature-map, Testindex 210–213, ADR 0024 Nachtrag.
- Folge von UP-1 (gewollt, QA-Hinweis): Daten-Einträge sind immer fällig. Auf AKE-DEV hat SQL/108 Spur 0 → erscheint „vor dem Start“ (Web+Dienst gestoppt); SQL/110 je System einmal Trockenlauf bis „entfällt“.

## Doku / Confluence (für spätere Übernahme)
- docs/anwender/01-kommissionierung.md (Stand, Hinweis seit 1.99.90)
- docs/anwender/12-register-sage-test.md (Stand; Fallstrick-Text seit 1.99.92)
- docs/anwender/16-ideal-kommissionierliste.md (Stand, Abschnitt Position zur AB-Nummer, Foto 16-03)
- docs/anwender/README.md (Versionszeilen 1.99.90–93)
- docs/betrieb/00-index.md (Stand, Versionszeilen 1.99.90–93)
- docs/betrieb/03-installation-update-rollback.md (Stand, Schritt 1 Versionsquelle, deploy-info.txt am Laufende)

## Paket
- `publish\paket28-v1.99.93.zip` (Commit a83d8756), 237.850.889 Byte, SHA-256 `722CE711869ED098F02C82DE15EE9924BFEE93A5398838492262F90708AF4614`
- `\\AKENET01.ake.at\Installsourcen\paket28-v1.99.93.zip`: Größe + SHA-256 gleich ✔
- `\\IDEALWEB01.ideal.ideal-ake.at\InstallSourcen\paket28-v1.99.93.zip`: Größe + SHA-256 gleich ✔

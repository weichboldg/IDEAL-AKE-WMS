---
type: aufgabe
status: offen
created: 2026-11-19
quelle: Gerald, Chat 2026-11-19 (/goal Sammel-Bugfix v1.99.44)
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-19: Sammel-Bugfix v1.99.44

Auftrag Gerald 2026-11-19: vier Punkte im Bündel-Worktree (Basis `1d17f156`, v1.99.43 + Doku-Commits), gemeinsam als **v1.99.44**.
Die Punkte sind Backlog-/Review-Befunde ohne eigene Spec – **der Auftrag ersetzt die Freigabe** (goal.md Schritt 1).
Je Punkt Build + Tests grün und eigener Commit; danach Doku (anwender 16, betrieb 03/04, Stand 1.99.44), Confluence (AKE-Dokumentation,
bestehende Seiten), qa-agent synchron, DEV AKE + IDEAL, Paket 9 nach `publish\paket9-v1.99.44\` + ZIP.
Grenzen: nichts an Sage, keine Einstellungen auf :88/4445/Prod, `SageFaArbeitsgaengeAnlegenAktiv` bleibt aus, kein Merge, kein Push.

## Übersicht

| Punkt | Quelle | Version | Status | Commit |
|---|---|---|---|---|
| 1 Ziel-Kästchen Kommissionierliste | [[2026-11-19-bugfix-kommissionierliste-ziele-filter-layout]] | 1.99.44 | umgesetzt | `0fe4cbcf` |
| 2 Menü „Empfänger“ auch bei Blechfertigung | [[2026-11-19-bugfix-empfaengergruppen-menue-nur-bei-bestellungen]] | 1.99.44 | umgesetzt | `6aa5ac4f` |
| 3 Prüfung FaStrukturSchluesselDoppelt ohne LTRIM | [[2026-11-19-review-buendel-v1-99-43]] S-2 | 1.99.44 | umgesetzt | `b799c96e` |
| 4 Rollen beschichtungsauftrag/_schreiben/schachtler | [[2026-11-19-doku-v1-99-43]] Befund 2 | 1.99.44 | umgesetzt (kein Code nötig, Befund 3 angeglichen) | `3c1cb048` |

## Protokoll

- 2026-11-19: Lauf gestartet, Basis `1d17f156`, Version 1.99.43 → 1.99.44 frei.
- 2026-11-19 Punkt 1 `0fe4cbcf`: verstecktes `target`-Feld hinter den Flex-Container (vor die legend geht nicht – legend muss erstes Kind sein); kein JS sucht es, Server liest `target` reihenfolgeunabhängig. frontend-design geladen (Leitplanke Konsistenz, kein Redesign). Wächter-Test. Build 0 Fehler, Tests 790 + 3320 (1 übersprungen) grün.
- Punkt 2 `6aa5ac4f`: Gruppen-Nutzer belegt – Bedarfsmeldungen (BestellungenAktiv), Lager-/Glasbestellung (LagerbestellungAktiv, Default an; `WarehouseRequisitionsController.cs:213-224`), BF (BlechfertigungAktiv; `BfAuftragWorkflow.cs:159`); Lieferverzug nutzt keine Gruppe. Bedingung `bestellungenAktiv || lagerbestellungAktiv || BlechfertigungAktiv`; Artikelgruppen-Zuordnung bleibt. `OrderRecipientsMenuTests`. Tests 790 + 3322 (1 übersprungen).
- Punkt 3 `b799c96e`: Prüfung mit `NULLIF(RTRIM(x), '')` wie `SQL/159:76,89` und `FaStrukturAbgleich.Norm`. Tests grün (NachlaufManifestDriftTests).
- Punkt 4 `3c1cb048`: Rollen-Wege belegt (Program.cs-Seed Altrollen, SQL-Skripte bis stock_read, seit v1.51 nur Manifest-Haltepunkt mit Reparatur). Die drei Rollen haben die Reparatur schon; DEV fehlt sie, weil dev-deploy.ps1 das Manifest nicht ausführt (sqlcmd lesend: IDEAL_WMS_DEV ohne beschichtungsauftrag/_schreiben/schachtler/einkauf; AKE_WMS_DEV hat einkauf vom Update-Skript). Dreiklang vollständig. Befund 3: Hilfe + Reparatur-Name „Beschichtungsauftrag“. Kein SQL.
- Version/Doku `2552e4f4`: 1.99.44 beide AppVersion, Changelog.cshtml, TS-166 (TS-165 reserviert für BDE-mobil), Anwender 14/16/README, Betrieb 00/03/04. Build 0 Fehler, Tests 790 + 3322 (1 übersprungen) grün.
- Offen für den Aufrufer: qa-agent, DEV-Deploy AKE+IDEAL, Foto-Beleg TS-166.1 (Liste + Summiert, 1440/390 px, :8082), Rollen-Reparatur auf IDEAL-DEV (TS-166.4a), Paket 9, Confluence.

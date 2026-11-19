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
| 1 Ziel-Kästchen Kommissionierliste | [[2026-11-19-bugfix-kommissionierliste-ziele-filter-layout]] | 1.99.44 | Testbereit (QA 2026-11-19) | `0fe4cbcf` |
| 2 Menü „Empfänger“ auch bei Blechfertigung | [[2026-11-19-bugfix-empfaengergruppen-menue-nur-bei-bestellungen]] | 1.99.44 | Testbereit (QA 2026-11-19) | `6aa5ac4f` |
| 3 Prüfung FaStrukturSchluesselDoppelt ohne LTRIM | [[2026-11-19-review-buendel-v1-99-43]] S-2 | 1.99.44 | Testbereit (QA 2026-11-19) | `b799c96e` |
| 4 Rollen beschichtungsauftrag/_schreiben/schachtler | [[2026-11-19-doku-v1-99-43]] Befund 2 | 1.99.44 | Testbereit (QA 2026-11-19; kein Code nötig, Befund 3 angeglichen) | `3c1cb048` |

## Protokoll

- 2026-11-19: Lauf gestartet, Basis `1d17f156`, Version 1.99.43 → 1.99.44 frei.
- 2026-11-19 Punkt 1 `0fe4cbcf`: verstecktes `target`-Feld hinter den Flex-Container (vor die legend geht nicht – legend muss erstes Kind sein); kein JS sucht es, Server liest `target` reihenfolgeunabhängig. frontend-design geladen (Leitplanke Konsistenz, kein Redesign). Wächter-Test. Build 0 Fehler, Tests 790 + 3320 (1 übersprungen) grün.
- Punkt 2 `6aa5ac4f`: Gruppen-Nutzer belegt – Bedarfsmeldungen (BestellungenAktiv), Lager-/Glasbestellung (LagerbestellungAktiv, Default an; `WarehouseRequisitionsController.cs:213-224`), BF (BlechfertigungAktiv; `BfAuftragWorkflow.cs:159`); Lieferverzug nutzt keine Gruppe. Bedingung `bestellungenAktiv || lagerbestellungAktiv || BlechfertigungAktiv`; Artikelgruppen-Zuordnung bleibt. `OrderRecipientsMenuTests`. Tests 790 + 3322 (1 übersprungen).
- Punkt 3 `b799c96e`: Prüfung mit `NULLIF(RTRIM(x), '')` wie `SQL/159:76,89` und `FaStrukturAbgleich.Norm`. Tests grün (NachlaufManifestDriftTests).
- Punkt 4 `3c1cb048`: Rollen-Wege belegt (Program.cs-Seed Altrollen, SQL-Skripte bis stock_read, seit v1.51 nur Manifest-Haltepunkt mit Reparatur). Die drei Rollen haben die Reparatur schon; DEV fehlt sie, weil dev-deploy.ps1 das Manifest nicht ausführt (sqlcmd lesend: IDEAL_WMS_DEV ohne beschichtungsauftrag/_schreiben/schachtler/einkauf; AKE_WMS_DEV hat einkauf vom Update-Skript). Dreiklang vollständig. Befund 3: Hilfe + Reparatur-Name „Beschichtungsauftrag“. Kein SQL.
- Version/Doku `2552e4f4`: 1.99.44 beide AppVersion, Changelog.cshtml, TS-166 (TS-165 reserviert für BDE-mobil), Anwender 14/16/README, Betrieb 00/03/04. Build 0 Fehler, Tests 790 + 3322 (1 übersprungen) grün.
- Offen für den Aufrufer: qa-agent, DEV-Deploy AKE+IDEAL, Foto-Beleg TS-166.1 (Liste + Summiert, 1440/390 px, :8082), Rollen-Reparatur auf IDEAL-DEV (TS-166.4a), Paket 9, Confluence.
- 2026-11-19 qa-agent: Testbereit, QA-Fix `f1747e10` (Q-1), Nachweis unten. Offen für den Aufrufer: Paket 9, Confluence.

## QA-Nachweis (qa-agent, 2026-11-19)

Bereich `1d17f156..2552e4f4` (5 Commits) + QA-Commit `f1747e10`. Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, HEAD `f1747e10`. Kein eigenes Spec-Dokument (Auftrag ersetzt Freigabe).

**1. Build und Tests** (vor und nach dem QA-Fix identisch): `dotnet build IdealAkeWms.slnx` 0 Fehler, 30 Warnungen (Bestand).
`dotnet test`: IdealAkeWms.Tests 3321 bestanden / 1 übersprungen / 0 Fehler (3322); IDEALAKEWMSService.Tests 790 / 0 / 0.

**2. Code-Review** (selbst, im Lauf; Diff + umgebender Code gelesen):
- Punkt 1 `0fe4cbcf`: verstecktes `target`-Feld hinter den Flex-Container. Kein JS liest `name="target"` (grep `wwwroot/js`, Views); der Server normalisiert reihenfolgeunabhängig (`KommissionierListenService.ZieleNormalisieren` :328-333, leere Werte fallen weg, leer = alle). Kein `hidden`+`d-*`, kein Script-Einbetten.
- Punkt 2 `6aa5ac4f` – Erweiterung um `LagerbestellungAktiv` **sachlich belegt**: Lager-/Glasbestellung lesen die Gruppe über `DefaultLagerbestellempfaengerId`/`DefaultGlasbestellempfaengerId` (`WarehouseRequisitionsController.cs:213-224`, Klasse hinter `[RequireLagerbestellungAktiv]` :11, Glas = `WarehouseRequisitionType.Glas` im selben Controller), Mails `IDEALAKEWMSService/Services/WarehouseRequisitionEmailService.cs:51,87`; BF über `BfEmpfaengergruppeId` (`BfAuftraegeController.cs:102-104`, `BfAuftragEmailService.cs:34-53`); Bedarfsmeldungen über das Artikelgruppen-Mapping (`PartRequisitionsApiController.cs:79-104`). Auswahl der Gruppen in /Settings (`SettingsController.cs:79`). Im Dienst nutzen nur diese zwei Mail-Services `OrderRecipient` (Lieferverzug nicht). **Rechte unverändert**: `OrderRecipientsController` Klasse `[RequireMasterDataReadAccess]`, Schreib-Aktionen `[RequireMasterDataAccess]`, kein Feature-Gate – die Seite war schon vorher per URL erreichbar, nur der Menüeintrag ändert sich. `BlechfertigungAktiv`-Lesart (nur exakt "true") = `RequireBlechfertigungAktivFilter` :23 und `_Layout.cshtml:90`.
- Punkt 3 `b799c96e`: `NULLIF(RTRIM(x), '')` entspricht dem UNIQUE-Index (SQL Server ignoriert Schluss-Leerzeichen beim Vergleich, führende nicht) und `FaStrukturAbgleich.Norm` (`TrimEnd`, :19); Leerwerte sind nach SQL/159 Schritt 4 ohnehin NULL. Zeilenverweise Betriebsdoku 03 (`nachlauf.psd1:127-130,154-161,166-201,235-238,255-262`) und 04 (`_Layout.cshtml:374-384`) nachgezählt: gültig (der Diff ändert keine Zeilenzahl im Manifest).
- Punkt 4 `3c1cb048`: Behauptung **bestätigt** – die Rollen `beschichtungsauftrag`/`_schreiben`/`schachtler`/`einkauf` kommen nur über `Pruefung/RollenBeschichtungsauftrag` (:180), `Pruefung/RolleEinkauf` (:190), `Pruefung/RolleSchachtler` (:255); kein Seed in `Program.cs`, kein Treffer in `SQL/*.sql`. Die Namensänderung steht nur in der `Reparatur` mit `WHERE NOT EXISTS` → wirkt nur bei Neuanlage; bestehende Namen bleiben. „Beschichtungsauftrag (IDEAL)“ steht sonst nur noch als historischer **Seitenname** in alten Changelog-/Plan-Texten (kein Rollenname).
- Geänderte Bestandstests: keine; `KommissionierSpaltenKonsistenzTests` nur um einen Fakt ergänzt, `OrderRecipientsMenuTests` neu.
- Checkliste: keine Migration, kein SQL, keine neue/geänderte Einstellung (ADR 0020 entfällt), Version in beiden `AppVersion.cs`, `Changelog.cshtml`, Brain-Changelog [[2026-11-19-v1-99-44-sammel-bugfix]], TS-166 in `docs/TESTSZENARIEN.md` und im Testindex. Kein neuer Deploy-Schritt (die geänderte Prüfung läuft im Manifest).

Befunde:

| Nr | Schwere | Befund | Behandlung |
|---|---|---|---|
| Q-1 | gering | `AppVersion.Date` (Web + Dienst) stand bei 1.99.44 noch auf 2026-11-18, der Changelog sagt 19.11.; der Dienst loggt das Datum beim Start | **behoben** `f1747e10` (beide 2026-11-19), Build/Tests danach wie oben |
| Q-2 | Hinweis | `_Layout.cshtml:377` liest `BlechfertigungAktiv` ein zweites Mal (gleiche Lesart wie :90, dort mit Rollen verknüpft) – dieselbe Regel zweimal in einer Datei | offen gelassen; bei der nächsten Layout-Änderung eine Variable `blechfertigungAktiv` neben :90 ziehen |
| Q-3 | Hinweis (DEV) | AKE-DEV Web-Log 10:13:35 ein ERR „antiforgery token could not be decrypted“ – Cookie der anderen localhost-Site (8081/8082 teilen Cookies, getrennte Key-Ringe) | kein Code-Befund; Schlüssel liegen unter ProgramData, das Leeren der Zielordner berührt sie nicht |

**3. Nachweisart je Punkt:** 1 Layout = Quelltext-Wächter + Playwright-Messung (Manual-UAT-Beleg, unten); 2 Menü = Quelltext-Wächter `OrderRecipientsMenuTests`, Laufzeit Manual-UAT; 3 Prüfung = `NachlaufManifestDriftTests` + echter SQL-Server-Lauf (unten, Rollback); 4 Rollen = DEV-Reparatur (unten).

**6. DEV-Deploy** (elevated, `D:\Dev\scripts\dev-deploy.ps1 -Quelle <Worktree>`): Zielordner `D:\Dev\{AKE,IDEAL}\{Web,Service}` vorher bei gestoppter Site/Dienst geleert (erhalten: `appsettings.DEV.json*`, `logs`, `App_Data`). Erst mit `2552e4f4`, nach Q-1 erneut geleert und mit `f1747e10`: AKE Exit 0 „HTTP 200, Fusszeile v1.99.44“, IDEAL Exit 0 „HTTP 200, Fusszeile v1.99.44“; beide Dienste Running, Dienst-Log „Version 1.99.44 (2026-11-19)“; Web-/Dienst-Logs ohne ERR/FTL außer Q-3. Kein Sage-Schalter, kein `Sync:*Enabled`, keine Mail-Schalter auf true (sqlcmd).

**Rollen auf DEV** (Manifest-SQL aus `nachlauf.psd1`, nur wo die Prüfung > 0 zeigte; Protokoll `C:\Users\GERALD~1.WEI\AppData\Local\Temp\claude\8eae370df6a92eae\ec0f5dc9-6d40-450e-bc07-1e14a1b12b59\scratchpad\rollen-reparatur-dev.log`):

| DB | RollenBeschichtungsauftrag | RolleSchachtler | RolleEinkauf |
|---|---|---|---|
| AKE_WMS_DEV | vorher 2 → 2 Zeilen → nachher 0 | 1 → 1 → 0 | 0 (vorhanden, keine Reparatur) |
| IDEAL_WMS_DEV | 2 → 2 → 0 | 1 → 1 → 0 | 1 → 1 → 0 |

Danach je DB: `beschichtungsauftrag` „Beschichtungsauftrag“, `beschichtungsauftrag_schreiben` „Beschichtungsaufträge (Schreibrecht)“, `schachtler` „Schachtler (Blechfertigung)“, `einkauf` „Einkauf“, alle IsSystem 1, CreatedBy „Update-Skript“.

**TS-166 auf DEV** (Playwright, Chromium `--auth-server-allowlist=localhost`):

| Schritt | Ergebnis | |
|---|---|---|
| 166.1 Summiert 1440 | Container 1356 px in Fieldset 1364 px, direkt unter der Legende; 12 Kästchen in 1 Zeile; kein Label-Umbruch („K-01“ 24 px hoch); Karte scrollWidth = clientWidth (1388), Seite ohne Querscrollen | OK |
| 166.1a Liste 1440 | gleiche Werte | OK |
| 166.1b 390 Liste + Summiert | Container 337 in 344 px, 3 Zeilen (4 je Zeile), kein Label-Umbruch, Karte 358 = 358, Seite ohne Querscrollen | OK |
| 166.1c alle Haken weg | FormData sendet `target=[""]` (Liste und Summiert) → „alle“ (ohne Absenden geprüft, kein Gedächtnis geschrieben) | OK |
| 166.2 IDEAL (BestellungenAktiv false, Blechfertigung true, Lagerbestellung true) | Menü Stammdaten: „Empfänger“ sichtbar, „Artikelgruppen-Zuordnung“ nicht | OK |
| 166.2c AKE (BestellungenAktiv true) | beide Einträge sichtbar | OK |
| 166.2a/2b | Einstellungen umschalten – laut Auftrag nicht auf DEV | NICHT MÖGLICH |
| 166.2d Benutzer ohne Stammdaten-Lesen | nur Admin-Windows-Anmeldung verfügbar; Rechte laut Review unverändert | NICHT MÖGLICH |
| 166.3 Prüfung IDEAL-DEV | Manifest-Abfrage = 0 (alt mit LTRIM ebenfalls 0) | OK (per sqlcmd, nicht über `Update-IdealAkeWms.ps1 -NurPruefen`) |
| 166.3a | in einer Transaktion mit ROLLBACK: Kopie eines Knotens mit **führendem** Leerzeichen in `Artnr` speicherbar (Index lässt zu), Prüfung neu 0 / alt 1; Kopie mit **Schluss**-Leerzeichen scheitert an `UX_FaHierarchyNodes_Schluessel`; nach ROLLBACK 0 Zeilen mit führendem Leerzeichen | OK |
| 166.4 / 166.4a | siehe Rollen-Tabelle (vorher fehlend, nachher 0) | OK (Manifest-SQL per sqlcmd statt Update-Skript-Haltepunkt) |
| 166.4b | /Roles, /Users/RoleOverview, /Help auf IDEAL-DEV: kein „Beschichtungsauftrag (IDEAL)“, Schachtler vorhanden | OK |

Fotos: `C:\Users\GERALD~1.WEI\AppData\Local\Temp\claude\8eae370df6a92eae\ec0f5dc9-6d40-450e-bc07-1e14a1b12b59\scratchpad\pw\out\ts166\` – `ts166-1-liste-1440.png`, `ts166-1-summiert-1440.png`, `ts166-1-liste-390.png`, `ts166-1-summiert-390.png`, `ts166-2-menue-stammdaten-ideal.png`.

**Deploy:** Web + Dienst (Dienst nur Versionsnummer/Datum), keine Migration, kein SQL, kein Einmal-Skript. Publish macht der Aufrufer (Paket 9).
Nach dem Merge nur dann neu aus main veröffentlichen, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.

**Ergebnis: Testbereit** (QA 2026-11-19).

### Manuelle Checkliste (offen für den Menschen)
1. 166.2a/2b auf einem Testsystem: `BlechfertigungAktiv` aus → „Empfänger“ bleibt (Lagerbestellung an); zusätzlich `LagerbestellungAktiv = false` → beide Einträge weg; danach Einstellungen zurück.
2. 166.2d mit einem Benutzer ohne Stammdaten-Lesen: kein Menü Stammdaten, `/OrderRecipients` → Zugriff verweigert.
3. 166.3/166.4a über `Update-IdealAkeWms.ps1 -NurPruefen` bzw. den Haltepunkt am Testsystem (QA hat dieselbe Manifest-SQL per sqlcmd belegt).
4. 166.1 Sichtprüfung am echten Terminal/Handy (QA-Fotos s. o.).
5. Testsystem-Lauf mit Paket 9, dann Merge-Entscheid (Schranke 2).

---
type: aufgabe
status: erledigt
created: 2026-11-25
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-25: Kommissionier-Nachtrag, Lagerplatz-Anlage Sage, BDE Isolierfräse, BDE-Masken-Epic

Auftrag Gerald 2026-11-25 (`/goal-run` mit vier Specs, Zielbedingung inkl. sauberer Tests + Definition of Done).
Reihenfolge des Auftrags. Start-Stand Worktree: v1.99.69 (eff16911), höchstes SQL 167 (Lücke 164/165 für die
Isolierfräse-Spec reserviert), höchstes TS 190, höchste ADR 0035, letztes Paket 19.

| Spec | Typ | SQL-Block | Version | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|---|
| [[2026-11-25-kommissionierauftraege-nachtrag-nachts-spec]] | Spec | 168 | 1.99.70 | 191 | **Testbereit** | fa1b754b, Fotos+Publish 6e426113 | Nachtlauf auf DEV nicht beobachtbar (Q4) |
| [[2026-11-24-lagerplatz-anlage-wms-nach-sage-spec]] | Spec | – (ADR 0036) | 1.99.71 | 192 (statt 189, vergeben) | **Testbereit** | 5b6fa696, Fotos 47a23d32, QA f085a8da | Live-Sage-Teil: Testlager + GRANTs fehlen (Gerald) |
| [[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]] | Spec | 164, 165 (reserviert) | 1.99.72 | 193 | **Testbereit** | d49a3f39, Fotos 4e8a344c, QA 557f1a9f | P-1 USER_OSBlechdicke (IDEAL-Sage), Rückfragen 15/16 Einmal-Skript |
| [[2026-11-25-bde-masken-vereinheitlichung-spec]] | Epic (1, 2, 3, 4a, 4b, 5) | 169 (statt 168) | 1.99.73–1.99.78 | 194–199 | **Testbereit** (QA 6d98dd6a/a3287fb0, Brain 8797bc42) | E1 219a69c3, E2 eecc47ac, E3 94be5444, E4a 878aa76d, E4b d61ea22c, E5 fbe29238 | |

ADR des Epics: 0037 (statt 0036, an die Lagerplatz-Spec vergeben).

## Vorprüfung
- Alle vier: status Freigegeben (Gerald 2026-11-25), worktree leer bzw. = Bündel, Freigabe-Antworten vollständig.
- Lagerplatz: Prüfbefunde eingearbeitet (b4b29db0), Sage-Test-SQL freigegeben (qa-agent.md ecc50041).
- Epic: B-1 entschieden (freigabe_entscheidung „Teile-Reiter nur mit BdeTeileliste“), S-1/S-3 nachgezogen (b474c4c5).
- Worktree sauber bei Start.

## Protokoll
- 2026-11-25 Start, Specs auf InUmsetzung.
- Kommissionier-Nachtrag v1.99.70: fa1b754b (Brain 639fbd0f), SQL/168 + Migration 20261125161741_AddPickingStatusStuecklisteKleiner, TS-191.1–191.16 + 191.11a, Build 0 Fehler/keine neuen Warnungen, Tests Web 3551/1 skip/0, Dienst 801/0. Befund: Tabelle heißt ProductionOrderPickingStatus (Spec korrigiert). Deploy-Hinweis: erster Dienststart legt offene Nachträge sofort an (Manifest-Info). QA läuft.
- v1.99.70 QA bestanden: DEV AKE+IDEAL v1.99.70, SQL/168 auf beiden DEV-DBs, Fotos 01-19/01-20/01-21 (6e426113). Befunde Q1–Q3 niedrig (belassen), Q4 mittel: DEV-Absicherung schaltet `Sync:KommissionierNachtragEnabled` (Muster `Sync:%Enabled`) ab → Nachtlauf auf DEV nicht beobachtbar, 191.1–191.7/191.9 NICHT MÖGLICH, bleibt Mensch. Prod-Default true = Sofortlauf beim ersten Dienststart. QA änderte kurz die eigene Rolle (UserRoles 40, DEV AKE) und setzte zurück.
- Lagerplatz-Anlage v1.99.71: 5b6fa696 (Brain 58b0caf1), keine Migration, ADR 0036, Fallstrick §66, TS-192, neue Keys SageLagerplatzAnlageAktiv (false) + SageLagerAnlageLagertechniken (Aus), SyncLog-Service SageLagerplatzAnlage, Manifest 4 Einträge. Build 0 Fehler/28 Warnungen, Web 3586/1/0, Dienst 847/0. Zusatzfund: SageLagerplatzReader.cs:26 verbindet ohne Mandant (nicht gebaut). QA läuft (Live-Teil nur mit Testlager + GRANTs + V3).
- v1.99.71 QA bestanden (f085a8da, Fotos 19-01..19-05 in 47a23d32): DEV beide v1.99.71, Build 0/32 Warnungen, Web 3586/1/0, Dienst 847/0. Befunde N1 (Abbruch beim Dienst-Stopp → Fehler statt Offen, Einzeiler vorgeschlagen), N2 (50014 ohne Sage-PlatzID, AK 13), H1/H2 Hinweise. V1/V2 (Testlager) und GRANTs fehlen in ake_TEST2026; V3 Teil 1+2 belegt (svc_idealakewms, DOTNET_ENVIRONMENT=DEV, AKESQL20/ake_TEST2026). Nichts in Sage angelegt (Tan 4378 unverändert), Schalter Endstand aus. Fotos 19-04/19-05 mit per SQL gesetzten Fehlerzuständen.
- Isolierfräse v1.99.72: d49a3f39 (Brain 6b8ced5d), SQL/164 AddArticleCamPath + SQL/165 AddBdeTeilelisteLagerbestellungen, ADR 0038, Fallstrick §67, Rolle fraesenpfad_pflegen, AppSettings FraesenPfadAktiv/BdeTeilelisteLagerbestellungArbeitstage, TS-193 (statt 171), Einmal-Skript Fräsen-Pfad nur IDEAL. Index UX_BdeTeilMeldungen_Schluessel Filter `[IsCancelled]=0 AND [WarehouseRequisitionItemId] IS NULL` (Epic vereinigt mit IstAbschliessend=1). Build 0/32, Web 3631/1/0, Dienst 847/0. QA läuft.
- v1.99.72 QA bestanden (557f1a9f, Fotos 17-08..17-12, 03-09, 10-08 in 4e8a344c): DEV beide v1.99.72, Build 0/32, Web 3631/1/0, Dienst 847/0; Einmal-Skript Trockenlauf DEV IDEAL ok (6 Zeilen ohne gültigen Pfad, AK 20). Endstand Schalter/Werkbank 75/User 8 zurückgesetzt.
- Epic E1 v1.99.73 (219a69c3, Brain 192e3253): FaAgStatusService + _FaAgSymbole, AG-Status vor Spaltenfilter, Baugruppen-Kinder als Zeilen, Mobil nur unter .bde-tl-seite; TS-194; Build 0/32, Web 3636/1/0, Dienst 847/0.
- Epic E2 v1.99.74 (eecc47ac, Brain b6a75729): Summiert mit aufklappbaren Einzelaufträgen, Summen-Haken, aggregierte Ampel (AgAggregieren über ArbeitsgangStatusRegel); TS-195; Build 0/32, Web 3641/1/0, Dienst 847/0.
- Epic E3 v1.99.75 (94be5444, Brain b21285e3): SQL/169 + Migration 20261125191311_AddBdeTeilMeldungIstAbschliessendUndStandardsicht, ADR 0037, Fallstrick §69, beide UX-Indizes mit `IstAbschliessend = 1`; TS-196; Build 0/32, Web 3655/1/0, Dienst 847/0.
- Epic E4a v1.99.76 (878aa76d, Brain 31610652): WerkbankKuerzelFamilie (Wirksamkeit an einem Ort), BdeFaAngebot als gemeinsame Zeilenquelle, Reiter Teile Einzeln/Summiert/FA, FA-Tabelle mit Anstempeln, Werkbank-Feld BDE-Standardsicht, Manifest 4 Einträge; TS-197; Build 0/32, Web 3685/1/0, Dienst 847/0. Verhaltensänderung IDEAL: Standardsicht FA leitet am Terminal nicht mehr um (gewollt).
- Epic E4b v1.99.77 (d61ea22c, Brain cb9f89c6): „Meine Buchungen“ (BdeKopfBuchungen), gemeinsame Dialoge _BdeDialoge (Terminal + FA-Sicht), bde-fa-sicht.js über Terminal-JSON-Aktionen, Scan „FA scannen“ (resolve-scan faSicht), Tätigkeit, NurAg-Fertig je Zeile; TS-198; Build 0/32, Web 3690/1/0, Dienst 847/0. UI-Bedienung lokal nicht geprüft → Epic-QA.
- Epic E5 v1.99.78 (fbe29238, Brain e780b51b): Tageshistorie eigene Seite (BdeTageshistorie, §69: alle Teilmeldungen, an Meldung hängende Buchung ausgeblendet), Views/BdeTerminal/Index + bde-terminal.js gelöscht, BdeTerminal/Index leitet immer weiter, BdeTerminalMaxHauptFa entfernt (DB-Zeile verwaist); TS-199; Build 0/32, Web 3689+1skip/0 (8 Tests entfallen, 7 neu), Dienst 847/0.
- Epic-QA (qa-agent) 1.99.73–1.99.78: Build 0/32, Web 3689/1/0, Dienst 847/0/0; DEV AKE+IDEAL v1.99.78 EXIT 0; SQL/169 zweifach + FreshInstall ok; TS-194–199 auf DEV (Headless-Chrome) ok bis auf NICHT MOEGLICH TS-198.6/198.7, AKE-Anstempeln, Benutzer ohne Terminal; neue Befunde N1–N10 in der Spec; Fotos 11-01/04/05/06/07, 17-01/03/05/06/07/13 + TS-Doku-Korrekturen (Worktree 6d98dd6a, a3287fb0); Publish v1.99.78; Spec **Testbereit**.

## Sammel-Fixes für den Schlusslauf (Auftrag Gerald 2026-11-25)
Alle Befunde der Specs werden gesammelt und nach der letzten Epic-Etappe in EINEM Fix-Lauf mit eigener Version + qa-agent behoben.

| # | Quelle | Befund | Vorschlag | Schlusslauf v1.99.79 |
|---|---|---|---|---|
| F-1 | v1.99.70 Q1 | `PickingController.cs:258` stiller Fallback `?? new List<…>()` | entfernen (Repository liefert nie null) | behoben (f1f0e4f2) |
| F-2 | v1.99.70 Q3 | „Altbestand geprüft“: Ausnahme in `ErzeugeOffeneAsync` → Fehlerseite, Kennzeichen schon gelöscht | Ausnahme fangen, WarningMessage „Nachtlauf holt nach“ | behoben (f1f0e4f2) |
| F-3 | v1.99.70 Q2 | Merker „Run gestartet“ auch bei Gesamtfehler → Wiederholung erst am Folgetag | laut Spec F2 gewollt – nur prüfen, ggf. belassen | offen Gerald |
| F-4 | v1.99.70 Q4 | DEV-Absicherung (`Sync:%Enabled`) schaltet `Sync:KommissionierNachtragEnabled` ab → Nachtlauf auf DEV nie beobachtbar | Entscheid Gerald: DEV-Ausnahme bzw. QA-Erlaubnis für diesen Schalter (kein Code) | offen Gerald |
| F-5 | v1.99.71 N1 | Dienst-Stopp während Sage-Befehl → Eintrag „Fehler“ statt „Offen“ | Einzeiler `when (!ct.IsCancellationRequested && !IstTechnisch(ex))` in beiden catch | behoben (f1f0e4f2) |
| F-6 | v1.99.71 N2 | Konfliktmeldung 50014 ohne belegende Sage-PlatzID (AK 13) | PlatzID in Meldung; Gerald 2026-11-25: Live-Test erst morgen möglich → im Schlusslauf den Konflikt-SELECT read-only gegen ake_TEST2026 selbst belegen (bekannter Fall `HAUPT;0;0;0` = `Hauptlager;0;0;0`), Live-Schreibtest bleibt Mensch | behoben (f1f0e4f2); SELECT ake_TEST2026 findet PlatzID 1 HAUPT = Hauptlager;0;0;0 |
| F-7 | v1.99.71 QA | Lagercode-Feld kürzt 11 Zeichen still auf 10 | maxlength entfernen/Hinweis, Server-Meldung zeigen | behoben (f1f0e4f2) |
| F-8 | v1.99.71 Umsetzer | `SageLagerplatzReader.cs:26` verbindet ohne Mandant | Mandant wie Schreibweg | behoben (f1f0e4f2) |
| F-9 | v1.99.71 H1/H2 | kurzzeitig „fehlend“ beim parallelen Abgleich; Fehlermail je Takt bei unerwarteter Ausnahme | Hinweise, prüfen | bestätigt (H1 selbstheilend `LagerplatzSyncService.cs:349-356`, H2 Muster) |
| F-10 | v1.99.72 Umsetzer | AK 9 Blechdicke: Sage-Feld nicht konfiguriert → Bestandswerte bleiben statt „–“ (`SdataArtikelReader.cs:360`) | Spec-Erwartung vs. Verhalten klären/angleichen | behoben (f1f0e4f2) |
| F-11 | v1.99.72 Umsetzer | Lagerbestell-Zeilen ohne Bestellart-Filter (Glasbestellung mit IF-Artikel erscheint) | Filter auf Bestellart prüfen | offen Gerald (Glas = Lagerbestellung? `WarehouseRequisitionType` Lager=1/Glas=2) |
| F-12 | v1.99.72 Umsetzer | Bestandsübersicht-Spaltenauswahl: Bestandsspalte nicht fixiert | fixieren | behoben (f1f0e4f2) |
| F-13 | v1.99.72 Umsetzer | Werkbank-Prüfung „Lagerbestellungen nur mit BDE-Teileliste“ ohne Unit-Test | Test ergänzen | behoben (f1f0e4f2) |
| F-14 | v1.99.72 Umsetzer | Einmal-Skript Fräsen-Pfad: Rückfrage 15 als Annahme (Y:\ → \\idealfs1\Daten$\Produktion\CAM\), Rückfrage 16 Belegestation offen | Gerald vor SCHARF | offen Gerald |
| F-15 | v1.99.72 Umsetzer | Fußzeile „Stand“ in docs/TESTSZENARIEN.md veraltet | nachziehen | behoben (f1f0e4f2) |
| F-16 | v1.99.72 QA F-2 (mittel) | `USER_OSBlechdicke` standardmäßig aktiv, Existenz in Sage IDEAL ungeprüft (P-1) → Blechteile-Lesen kann scheitern | Prüfabfrage P-1 vor Einschalten (IDEAL-Test nicht erreichbar → Gerald), ggf. Default aus. **Gerald 2026-11-25:** AKE hat das Feld nicht und muss ohne funktionieren. Code-Beleg: einziger Sage-Leser ist `SdataArtikelReader.BlechteileFelderLesenAsync`, nur über `BlechteileFelderNachlese.LaufAsync` (`SageImportService.cs:576`), Schalter `Sync:BlechteileFelderNachlesen` (AKE false); Fehler dort nur Warnung, Artikel-Abgleich bleibt gültig; Teileliste liest Merkmale aus der WMS-DB (leer = „–“). **Schluss-QA:** Artikel-Abgleich auf DEV AKE (Nachlese aus) und DEV IDEAL (Nachlese an, falls Sage-Test IDEAL erreichbar) laufen lassen, Aktivitäts-Protokoll auf Fehler/Warnungen prüfen, Schalter zurücksetzen. Gerald: Sage-Test IDEAL sollte verfügbar sein. Direkter SQL-Lesezugriff `idealsql2\SAGE100TEST`/IDEAL_TEST_2026_05_03 mit AKENB\gerald.weichbold: Anmeldung abgelehnt → Prüfung über den Dienst-Weg (SData sagetest01) in der QA | offen Gerald |
| F-17 | v1.99.72 QA F-5 | Artikel bearbeiten: bei Validierungsfehler verschwindet die Fräsen-Pfad-Karte | Karte im Fehlerfall wieder rendern | behoben (f1f0e4f2) |
| F-18 | v1.99.72 QA F-7 | Einmal-Skript Fräsen-Pfad ohne eigene Standortprüfung (nur Manifest) | DB-Namen-/Standort-Guard im Skript | behoben (f1f0e4f2) |
| F-19 | v1.99.72 QA F-9 | vorbestehendes `innerHTML` in WarehouseRequisitions/Edit | auf textContent prüfen | behoben (f1f0e4f2) |
| F-20 | Epic E1 | CSV-Export der Teileliste enthält Teile-Zeilen (gemeinsames csv-export.js) | entscheiden; ggf. Kinder-Zeilen im Export ausnehmen | offen Gerald |
| F-21 | Epic E2 | Summen-Haken meldet immer volle Sollmenge – mit Teilfertigmeldung (E3) passend? | in E3/Schlusslauf entscheiden | offen Gerald |
| F-22 | Epic E3 | Baugruppe: „Fertig“ mit 0/0 nach Übermeldung wird abgewiesen (`ReportCompletionCoreAsync`), Blechteile/Lagerbestellung erlauben 0/0 | Entscheid Gerald, ggf. angleichen | offen Gerald |
| F-23 | Epic E3 | Spec-Abschnitt Einstellungen nennt noch „SQL/168“ (gemeint 169) | Text korrigieren | behoben (Brain, Epic-Spec) |
| F-24 | Epic E3 | docs/betrieb/03 Tabelle 5.2a ohne Zeile SQL/168 | nachtragen | behoben (f1f0e4f2) |
| F-25 | Epic E4a | AG-Status in der FA-Sicht ohne Werkbank-Gate (Vereinfachung) | prüfen, ob Gate nötig | widerlegt (Spec Antwort 4: kein Werkbank-Gate) |
| F-26 | Epic E4b | Laufzeit in „Meine Buchungen“ zählt nicht live (Stand Seitenaufbau) | prüfen, ggf. Timer aus Terminal übernehmen | behoben (f1f0e4f2) |
| F-27 | Epic E4b | Scan auf schon laufenden AG meldet „nicht gefunden (oder läuft bereits)“ (Bestand BdeScanResolver) | Meldung schärfen | behoben (f1f0e4f2) |
| F-28 | Epic E4b | JSON-Aktionen nehmen terminalId vom Client (wie Terminal) | in QA bewerten | behoben (f1f0e4f2) |
| F-29 | Epic E5 | Verwaiste Aufrufe ohne Nutzer: BdeTerminal/ActiveBookings, PausedBookings, StartProductionForOrder; API operator/{nr}, active-booking, today-history, available-operations | entfernen (ponytail) nach Grep-Beleg | offen Gerald (nur Tests rufen auf; StartProductionForOrder → [[2026-11-25-bde-nur-fa-meldung-entfernen]]; /api/bde öffentlich) |
| F-30 | Epic E5 | Fotos 11-02/11-03 nicht mehr referenziert | löschen oder belassen | offen Gerald |
| F-31 | Epic E5 | Tageshistorie ohne Anmeldung → nach Login auf Liste statt Historie | returnUrl durchreichen | behoben (f1f0e4f2) |
| F-32 | Epic E1/E5 | Foto-Name 17-08 doppelt belegt (Isolierfräse vs. Handy-Teileliste) | nächste freie 17-NN | bestätigt (Handy = 17-13, 17-08 nur einmal) |
| F-33 | Epic-QA N1 | Kontrast 4,13:1 inaktive Reiter + „Tätigkeit starten“ | auf ≥4,5:1 | behoben (f1f0e4f2) |
| F-34 | Epic-QA N2 | Tageshistorie „Sub-FA 0“ bei Blechteilen, „FA 0 / Sub-FA 0“ bei Lagerbestellung (`BdeTageshistorie.cs:73`) | 0 nicht anzeigen | behoben (f1f0e4f2) |
| F-35 | Epic-QA N4 | „Meine Buchungen“ ohne Sub-FA (wie altes Terminal) | Sub-FA ergänzen | behoben (f1f0e4f2) |
| F-36 | Epic-QA N6 | Alle Einzel-Haken wieder gesetzt → Summen-Haken kommt nicht zurück | koppeln | behoben (f1f0e4f2) |
| F-37 | Epic-QA N7 | Kollisionsdialog: Fokus auf Dialog statt „OK“ | Fokus auf OK | behoben (f1f0e4f2) |
| F-38 | Epic-QA N9 | QA legte 3 Tabellen in lokaler DB `.`/IDEAL_AKE_WMS an (BfAuftraege, BfAuftragPositionen, SageFaArbeitsgangAnlagen) | **Erledigt 2026-11-25: bleiben.** Lokale DB PC-WEICHBOLD/MSSQLSERVER (nicht DEV, nicht AKESQL20); `__EFMigrationsHistory` führt 20261117154645_AddBfAuftraege + 20261117192807_AddSageFaArbeitsgangAnlagen bereits als angewendet → Tabellen machen die Kopie konsistent, Löschen würde sie brechen; leer, kein Repo-Verweis auf diese DB | offen Gerald (erledigt: bleiben) |
| F-39 | Epic-QA N10 | FreshInstall ohne History-Zeile `20260910081155_AddProductionOrderMatchcode` (Altbestand) | nachtragen | widerlegt (Migration in 47de3ba2 entfernt, nie deployt – Zeile gehört NICHT in FreshInstall) |

## QA Schlusslauf v1.99.79 (2026-11-25)

qa-agent, synchron, Review und Szenarien selbst. Worktree `feature/2026-08-07-ideal-teile-1-5`, HEAD `a4feb6af`, Diff-Basis `a3287fb0..a4feb6af` (43 Dateien, +475/-56). **Ergebnis: bestanden** – keine FEHLER, kein Befund hoch/mittel aus dem Diff; F-16 IDEAL nicht abschließbar (Sage-Test-Anmeldung des Dienstes), siehe unten.

**1. Build und Tests (Worktree):** `dotnet build IdealAkeWms.slnx` 0 Fehler / 32 Warnungen (Bestand). `dotnet test`: IdealAkeWms.Tests 3698 bestanden / 1 übersprungen / 0 Fehler; IDEALAKEWMSService.Tests 848 / 0 / 0.

**2. DEV-Deploy** (`D:\Dev\scripts\dev-deploy.ps1`, erhöhte Shell, Quelle Worktree, Web + Dienst, keine Migration): AKE Exit 0, „HTTP 200, Fusszeile v1.99.79“ (http://localhost:8081); IDEAL Exit 0, „HTTP 200, Fusszeile v1.99.79“ (http://localhost:8082). DEV-Absicherung lief.

**3. Code-Review (Diff selbst gelesen):**
- F-1: einzige Implementierung `PickingOrderRepository.GetAktiveZeilenAsync` (`ToListAsync`, nie null), ungecacht registriert (`PickingOrderRegistrierung.cs:45`); kein Caching-Decorator, sonst nur Test-Doubles (`PickingControllerTests` Setup ergänzt, Fallstrick 71). OK.
- F-28: Guard an allen sechs Start-Aktionen (StartProductionForOrder, StartSetup, StartProduction, StartActivity, Resume, ReportCompletion); einzige UI-Aufruferin ist `bde-fa-sicht.js`. Benutzer ohne Terminal: Die BDE-Seite leitet schon vorher mit sichtbarer Warnung „Für diesen Benutzer ist kein Terminal konfiguriert …“ auf die Startseite (`BdeTeilelisteController.cs:101-105`); die JSON-Aktionen antworten „Dieses Terminal ist nicht dem angemeldeten Benutzer zugeordnet – Seite neu laden.“ + „– nichts gebucht.“ (Default-Zweig `bde-fa-sicht.js`). Bewusst und sichtbar, nicht still.
- F-5, F-6, F-8, F-10, F-18, F-19, F-26, F-27, F-34 bis F-37 gelesen: keine Befunde. F-18: Skript ohne `GO`, `RETURN` nach dem Guard beendet das ganze Skript.
- Fehlerklassen geprüft: kein `@Html.Raw` in Skripten (StockOverview-JSON nur Literale), kein `hidden` + `d-*`, kein neuer stiller Fallback (F-1 entfernt einen), kein Parallel-Array-Binding, F-7 serverseitig validiert (`StorageLocationsController.cs:289-290`), keine doppelte Regel (F-27 teilt `FamilieAsync` mit `BuildNormalCandidatesAsync`).
- **Neue Hinweise (niedrig, kein Regress):** H-A `Pause`, `Finish`, `ReportPartial`, `FinishGroup`, `CloseOthers` nehmen `bookingId`/`operatorId` vom Client ohne Terminal-/Besitzprüfung (Bestand, außerhalb F-28) – Entscheid Gerald, ob der Guard dort auch hin soll. H-B F-2 zeigt `ex.Message` im Warntext (ggf. technischer Text für den Leitstand). H-C `STRING_AGG … WITHIN GROUP` (F-6) braucht SQL Server ab 2017 auf dem Sage-Server; an ake_TEST2026 belegt, für IDEAL Prüfabfrage `SELECT @@VERSION` (Gerald).

**4. Geänderte bestehende Tests:** `SageLagerplatzAnlageServiceTests.SqlText_PlatzBefehl` (F-6 bewusst), `BdeTerminalControllerTests` Fixture (GetCurrentAppUserId + Terminal-Mock, F-28 bewusst), `PickingControllerTests` Setup GetAktiveZeilenAsync (F-1, Fallstrick 71), `BdeKopfBuchungenTests` (+Sub-FA, F-35), `BdeScanResolverTests.Normal_Running_…` (F-27 bewusst NotInScope → Running), `FraesenPfadTests.Controller(bool fraesenAktiv = false)` (nur Parameter, Default = alt), `ViewKeyDriftGuardTests` (+F-12). Kein „Sonstiges“.

**5. Checkliste:** keine Migration, kein Schema-SQL; Version 1.99.79 in beiden `AppVersion.cs`; `Views/Help/Changelog.cshtml` + Brain-Changelog [[2026-11-25-v1-99-79-sammel-fixes-goal-lauf]]; TS-200 in `docs/TESTSZENARIEN.md` + Index Zeile 200. Keine neue oder geänderte Einstellung (ADR 0020 entfällt). Kein neuer Deploy-Schritt; Manifest-Eintrag des Einmal-Skripts gilt unverändert.

**6. Szenarien DEV** (Browser claude-in-chrome, eigener Tab; manipulierte Requests per curl mit Windows-Sitzung; SQL `sqlcmd -S . -E -C`). Screenshots nicht möglich (CDP-Timeout), Belege über DOM-Text und HTTP-Antworten.

| Schritt | Ergebnis | Status |
|---|---|---|
| F-28 fremde terminalId (AKE, eigenes Terminal 3, gesendet 1) an StartActivity, StartSetup, StartProduction, ReportCompletion, Resume, StartProductionForOrder | alle `InvalidState` „Dieses Terminal ist nicht dem angemeldeten Benutzer zugeordnet – Seite neu laden.“; `BdeBookings` vorher/nachher 58 | OK |
| F-28 eigenes Terminal AKE: StartActivity, Resume (UI) | Success (Buchung 65) bzw. fachlich „Dieser Arbeitsgang gehört nicht zu dieser Werkbank.“ – Guard passiert | OK |
| F-28 eigenes Terminal IDEAL: Fortsetzen (UI), Anstempeln (UI), StartSetup, ReportCompletion | „Fortgesetzt.“; Kollisionsdialog; `QuantityRequired`; „Diese Werkbank meldet nicht ohne Zeit fertig.“ – Guard passiert überall | OK |
| F-28 Benutzer ohne Terminal | nur Code + Unit-Test (kein zweites Windows-Konto) | NICHT MOEGLICH live |
| F-27 Scan IDEAL Wb 65: laufende Sub-FA 1067191 / unbekannt 99999991 / FA 1060414 an Wb 47 | UI „FA 1067191: Der Arbeitsgang läuft bereits (offene Buchung) …“; API `Running` / `NotFound` / `NotInScope` | OK |
| F-26 Ticker IDEAL | Laufzeit 0:00 h → 0:02 h ohne Neuladen | OK |
| F-35 Sub-FA in „Meine Buchungen“ | IDEAL „1060414 · Sub-FA 1067191 / EG Entgraten Fläche“; AKE „2609192 / 01 ZB“ ohne Zusatz | OK |
| F-37 Kollisionsdialog IDEAL (Werker 9901 bucht Sub-FA 1067193, Werker 123 startet) | Dialog „Arbeitsgang bereits in Arbeit … QA Werker B“; Enter ohne Klick schließt ihn | OK |
| F-31 Tageshistorie ohne Werker | Anmeldeseite mit `ziel=historie`; falsche Nr. → Fehler, `ziel` bleibt; richtige → 302 `/BdeTeileliste/Tageshistorie?…`; `ziel=https://evil.example` → 302 auf Liste (kein Open Redirect) | OK |
| F-34 Ziel-Spalte IDEAL Wb 67 | „FA 1059463 · Pos. 3 · …“ (kein „Sub-FA 0“); Testzeile Lagerbestellung „Lagerbestellung · QA-TS200.14-LB“ (BdeTeilMeldungen Id 33, danach storniert) | OK |
| F-36 Summen-Haken IDEAL Summiert | Summe an → Einzel aus → wieder an → Summe war gesetzt (nächster Klick leert alle, Knopf deaktiviert); Knopf „2 Zeilen, Summe 2“ | OK |
| F-33 Kontrast | `.bde-terminal` an allen BDE-Seiten; #0a58ca auf #F5F5F5 = 5,90:1, auf Weiß 6,44:1 (gerechnet); Browser-Messung nicht möglich | OK (gerechnet) |
| F-12 Bestandsübersicht AKE | „Aktueller Bestand“ und „Artikelnummer“ im Spaltendialog deaktiviert | OK |
| F-17 IDEAL Artikel 7878 (`FraesenPfadAktiv` vorübergehend an) | POST ohne Artikelnummer → „Artikelnummer ist erforderlich“, Karte mit `\\idealfs1\…` bleibt, nichts gespeichert (ModifiedAt unverändert) | OK |
| F-19 AKE Glasbestellung 58, Suche „Flantastic 92“ | Treffer „S5600549 - Schwarzglasbord Flantastic 92-139-Z [T&R] Sonder“ wörtlich | OK |
| F-18 Guard read-only ausgewertet | AKE_WMS_DEV „ABBRUCH (kein IDEAL)“ (Wert false), IDEAL_WMS_DEV „weiter“ (true); Skript selbst nicht ausgeführt (ADR 0015) | OK |
| F-6 Konflikt-SELECT read-only ake_TEST2026 | „… schon anders belegt (PlatzID 1 HAUPT = Hauptlager;0;0;0)“ | OK |
| F-7 Neues Lager 11 Zeichen | Seite braucht `SageLagerplatzAnlageAktiv` (Sage-Schreibschalter) – nicht eingeschaltet; Unit-Test + Code | NICHT MOEGLICH live |
| F-2 Altbestand geprüft, Anlegen scheitert | kein FA mit `AltbestandPruefen` auf DEV, Fehler nicht provozierbar; Unit-Test | NICHT MOEGLICH live |
| F-29 | keine View/kein JS ruft ActiveBookings, PausedBookings, StartProductionForOrder, api operator/today-history/available-operations; bewusst stehen gelassen, keine toten UI-Verweise | OK |

**F-16 Artikel-Abgleich (Auftrag Gerald 2026-11-25, Sage nur lesend):**
- **DEV AKE** (`Sync:ArticlesEnabled` false → true, `Sync:BlechteileFelderNachlesen` false belassen; Dienst-Neustart 22:37:57; SageConnection ake_TEST2026, SData sagetest01 ake_TEST2026;1): „Quelle: SData“ … „Run erfolgreich beendet - gelesen=100599, neu=0, aktualisiert=0, hauptlagerplatz_fehlt=106“ (22:42:17). Fehler 0. Warnungen: 1× „Zugangsdaten aus appsettings - bitte in die Service-Einstellungen übernehmen“, 106× „Hauptlagerplatz 'FTG;0;0;0' fuer Artikel … nicht als WMS-Lagerplatz gefunden - FK bleibt leer“ (DEV-Lagerplatzstand). Nichts zu Blechteilen oder USER_OSBlechdicke – AKE läuft ohne das Feld. **OK.**
- **DEV IDEAL** (`Sync:ArticlesEnabled` und `Sync:BlechteileFelderNachlesen` false → true, Dienst-Neustart 22:44:37): „Run fehlgeschlagen: Die von der Anmeldung angeforderte IDEAL_TEST_2026_05_03-Datenbank kann nicht geöffnet werden. Fehler bei der Anmeldung. Fehler bei der Anmeldung für den Benutzer "AKENB\svc_idealakewms".“ (22:44:49). Der IDEAL-Abgleich liest zuerst die Sage-View per SQL (`idealsql2\SAGE100TEST`); die SData-Nachlese der Blechteile-Felder kommt erst danach (`SageImportService.SyncArticlesAsync`) und wurde nicht erreicht. **Existenz von USER_OSBlechdicke in Sage IDEAL weiter unbelegt – NICHT MOEGLICH.** Hinweis: DEV IDEAL `SData:BaseUrl` ist `https://idealsage2.ideal.ideal-ake.at:5493` (Dataset IDEAL_TEST_2026_03_05;1), nicht sagetest01. Teileliste Wb 67: Spalte Blechdicke vorhanden, Werte „–“, keine Fehlermeldung.
- Für Gerald: dem Dienstkonto `AKENB\svc_idealakewms` Lesezugriff auf `IDEAL_TEST_2026_05_03` geben (oder SageConnection DEV IDEAL auf eine erreichbare Test-DB), dann Abgleich mit Nachlese wiederholen; alternativ Prüfabfrage P-1 in Sage IDEAL.

**Umgeschaltet / zurückgesetzt (DEV):** AKE_WMS_DEV `Sync:ArticlesEnabled` true 22:37 → false 22:44. IDEAL_WMS_DEV `Sync:ArticlesEnabled` + `Sync:BlechteileFelderNachlesen` true 22:44 → false 22:47. IDEAL_WMS_DEV AppSetting `FraesenPfadAktiv` (Zeile fehlte) vorübergehend eingefügt → wieder gelöscht. Danach geprüft: an beiden DEV-DBs kein `Sync:*Enabled` ungleich false. Testdaten: AKE Buchung 65 (Tätigkeit, Werker 5155) läuft; IDEAL Buchung 35 (Werker 123, fortgesetzt) und 36 (Werker 9901, Sub-FA 1067193) laufen; BdeTeilMeldungen Id 33 storniert. Nichts nach Sage geschrieben, Sage-Produktion nicht berührt.

**7. Publish:** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` – IDEALAKEWMSWeb + IDEALAKEWMSWebService (Release, Ordner vorher geleert), SQL\, deploy\, `publish-info.txt` Version 1.99.79, Commit a4feb6afb103e9ce31d6a7c72a235132364bfd3d. Zu deployen: Web + Dienst (beide geändert), keine Migration; Einmal-Skript Fräsen-Pfad bleibt Trockenlauf/Gerald. Nach dem Merge nur neu publizieren, falls der Merge getestete Dateien mit parallelen main-Änderungen kombiniert.

**Offen für den Menschen:**
1. F-16 IDEAL: Lesezugriff des Dienstkontos auf Sage-Test IDEAL, dann Abgleich mit Nachlese an und Protokoll prüfen (USER_OSBlechdicke).
2. F-6 Live-Schreibtest Konflikt (Gerald, Sage-Test) + `SELECT @@VERSION` am IDEAL-Sage-Server (STRING_AGG).
3. F-7 Neues Lager mit 11 Zeichen auf einem System mit `SageLagerplatzAnlageAktiv`.
4. F-2 Altbestand-Fehlerfall, sobald ein FA mit „Altbestand prüfen“ vorhanden ist.
5. F-28 mit einem Benutzer ohne Terminal; Entscheid H-A (Guard auch an Pause/Fertig/Teilmenge).
6. F-33 Kontrast im Browser am Fertigungsterminal nachmessen.
7. Testsystem-Lauf mit dem Publish-Paket, danach Merge (Schranke 2).

## Doku-Nachzug v1.99.80 (2026-11-25)

Auftrag Gerald: Anleitungen für User/Admin/KI nachziehen, damit am 2026-11-26 eine Mail rausgehen kann. Commit `67868774` (Worktree), keine
Logik, keine Migration, TS-201. Build 0 Fehler / 32 Warnungen; Tests Web 3698/1/0, Dienst 848/0. Changelog [[2026-11-25-v1-99-80-doku-nachzug]],
Mail-Entwurf [[2026-11-26-mail-wms-neuerungen-v1-99-70-80]] (nicht gesendet). qa-agent für v1.99.80 steht aus.

**Geänderte Kapitel → Confluence-Seite (Bereich AKEDOKUMEN, für spätere Übernahme):**
Ob eine Seite schon existiert, wurde nicht geprüft (Confluence in diesem Lauf verboten) – vor dem Anlegen im Bereich nach dem Titel suchen.
Seit 2026-11-19 wird Confluence nicht mehr nachgezogen; Kapitel ab dann sind vermutlich „neu anlegen“.

| Datei | Confluence-Seite | Aktion | Seiten-ID | Version (2026-11-26) |
|---|---|---|---|---|
| `docs/anwender/README.md` | WMS-Anleitung – Übersicht | aktualisieren (Stand, Versionstabelle 1.99.80) | 680853506 | v5 → v6, aktualisiert |
| `docs/anwender/01-kommissionierung.md` | WMS-Anleitung 01 – Kommissionierung | aktualisieren | 681082881 | v3 → v4, aktualisiert |
| `docs/anwender/03-lagerbestellung-glasbestellung.md` | WMS-Anleitung 03 – Lagerbestellung und Glasbestellung | aktualisieren | 681115649 | v2 → v3, aktualisiert |
| `docs/anwender/10-bestaende-spaltenfilter.md` | WMS-Anleitung 10 – Bestände und Spaltenfilter-Suche | aktualisieren | 681017366 | v2 → v3, aktualisiert |
| `docs/anwender/11-bde-terminal.md` | WMS-Anleitung 11 – BDE: Arbeitsgänge buchen | aktualisieren (stark geändert seit 1.99.76) | 681017386 | v3 → v4, aktualisiert + Titel umbenannt |
| `docs/anwender/17-bde-teileliste-werkbank.md` | WMS-Anleitung 17 – BDE-Teileliste an der Werkbank | aktualisieren bzw. neu anlegen, falls nicht vorhanden | 688160769 | v1 → v2, aktualisiert |
| `docs/anwender/19-lagerplaetze-in-sage-anlegen.md` | WMS-Anleitung 19 – Lagerplätze in Sage anlegen | **neu anlegen** (Kapitel seit v1.99.71) | 701005871 | v1, neu angelegt (unter WMS Anwender-Anleitungen) |
| `docs/betrieb/00-index.md` | WMS-Betrieb 00 – Übersicht | aktualisieren | 681050113 | v3 → v4, aktualisiert |
| `docs/betrieb/03-installation-update-rollback.md` | WMS-Betrieb 03 – Installation, Update und Rückfall | aktualisieren | 680820744 | v3 → v4, aktualisiert |
| `docs/betrieb/04-konfiguration.md` | WMS-Betrieb 04 – Konfiguration | aktualisieren | 680787999 | v6 → v7, aktualisiert |
| `docs/betrieb/05-schnittstellen.md` | WMS-Betrieb 05 – Schnittstellen zu Fremdsystemen | aktualisieren | 681181205 | v3 → v4, aktualisiert |
| `docs/betrieb/06-logs-ueberwachung.md` | WMS-Betrieb 06 – Logs und Überwachung | aktualisieren | 681050153 | v2 → v3, aktualisiert |
| `docs/betrieb/07-troubleshooting.md` | WMS-Betrieb 07 – Troubleshooting | aktualisieren | 680722435 | v2 → v3, aktualisiert |
| `docs/betrieb/08-diagnose-abfragen.md` | WMS-Betrieb 08 – Diagnose-Abfragen (nur lesend) | aktualisieren | 681246721 | v3 → v4, aktualisiert |
| `docs/betrieb/09-sicherheit-konten-rechte.md` | WMS-Betrieb 09 – Sicherheit, Konten und Rechte | aktualisieren | 680722455 | v2 → v3, aktualisiert |
| `docs/betrieb/AGENTS.md` | WMS-Betrieb – Leitfaden für KI-Assistenten (AGENTS) | aktualisieren bzw. neu anlegen | 681050133 | v3 → v4, aktualisiert |
| WMS-Hilfeseite `/Help` (Karte BDE) | – (liegt in der Anwendung) | kein Confluence | – | – |

Zusätzlich für die Mail gebraucht: Seite „WMS – Fehler melden und Wünsche einreichen“ (Bestand prüfen).

**Übernommen 2026-11-26** (Auftrag Gerald, einmalige Ausnahme von „Confluence derzeit nicht“): alle Zeilen aus `docs/` Stand v1.99.80 (67868774), Versionsmeldung „Übernahme aus docs/ Stand v1.99.80 (67868774)“, Bilder eingebettet, interne `.md`-Links auf die Confluence-Seiten umgestellt. „Fehler melden“ = bestehende Seite 683933697 (unverändert, nur verlinkt). Offen: Kapitel 18 hat keine Confluence-Seite (Verweise als „im Repository“); Obsidian-Wikilinks `[[…]]` in den Betriebs-Kapiteln bleiben Text; Callout `[!warning]` in B08 als einfaches Zitat.

**Offen / Widersprüche aus dem Nachzug:**
- `USER_OSBlechdicke` in Sage IDEAL unbelegt (F-16); Doku nennt nur die Prüfabfrage.
- SQL-Server-Version IDEAL für `STRING_AGG` (50014-Text mit PlatzID) ungeprüft: `SELECT @@VERSION`.
- Kapitel 18 (Startseite und Menü) erwähnt Tageshistorie nicht; Bilder 11-02/11-03 unreferenziert (F-30); Fotos nicht neu erstellt.
- Handy-Konten noch nicht angelegt (Manifest `v1.99.78-BdeHandyKontenInfo`).

## QA Doku-Nachzug v1.99.80 (qa-agent, 2026-11-25)

**Ergebnis: bestanden → Testbereit.** Geprüfter Bereich `a4feb6af..67868774`, QA-Korrektur `3c5a97df` (Worktree, Branch
`feature/2026-08-07-ideal-teile-1-5`). Review synchron in diesem Lauf, nicht delegiert.

1. **Build/Tests** (Worktree): `dotnet build IdealAkeWms.slnx` 0 Fehler / 32 Warnungen; `dotnet test` IdealAkeWms.Tests 3698 bestanden /
   1 übersprungen / 0 Fehler, IDEALAKEWMSService.Tests 848 / 0 / 0 – wie Basis.
2. **Review Diff (keine Logik):** geändert nur `AppVersion.cs` (Web + Dienst, 1.99.79 → 1.99.80), `Views/Help/Changelog.cshtml` (neue Karte,
   statisches HTML), `Views/Help/Index.cshtml` (nur Text + zwei Tag-Helper-Links `BdeTerminal/Index`, `BdeTeileliste/Tageshistorie` – beide
   Aktionen existieren: `BdeTerminalController.cs:39`, `BdeTeilelisteController.cs:222`), `docs/` und `docs/TESTSZENARIEN.md`. Kein
   `<script>`, kein `@Html.Raw`, keine `hidden`/`d-*`-Kombination, keine Einstellung, keine Migration. Defektklassen der QA-Liste: nicht
   betroffen. Geänderte bestehende Tests: keine. ADR 0020: keine neue/geänderte Einstellung. Nachlauf-Manifest: v1.99.80 braucht keinen
   Eintrag (kein Deploy-Schritt).
3. **Nachweisart:** TS-201 komplett Manual-UAT (nur Text); kein AK automatisiert beweisbar.
4. **Befunde und Behandlung:**
   - B-1 `docs/betrieb/03-installation-update-rollback.md` 5.3, Zeile „Nachher“: zwei Beleg-Zellen → Tabelle mit 5 statt 4 Spalten. **Behoben**
     (Fundstellen in eine Zelle, `3c5a97df`).
   - B-2 `docs/betrieb/05-schnittstellen.md` § 3 Blechteile-Nachlese: Pfad `IDEALAKEWMSService/Services/BlechteileFelderNachlese.cs` falsch,
     richtig `…/Services/Sdata/BlechteileFelderNachlese.cs`. **Behoben** (`3c5a97df`).
   - Hinweis (nicht korrigiert): 03 5.3 Zeile „Vorher“ nennt als Beleg weiter `nachlauf.psd1:127-130,…`, der neue Eintrag
     `Pruefung/BdeNurFaMeldungAn` steht bei `:228-231` (in 04 § 5.5 richtig zitiert). Kosmetisch.
5. **Stichproben (12, alle OK):** `BdeNurFaMeldung` Default `false`, Text „Altlast“ (`AppSettingDefinitions.cs:221`); `BdeTerminalMaxHauptFa`
   entfernt, Kommentar `:235`; `Pruefung/BdeNurFaMeldungAn` Phase Vorher, beide (`nachlauf.psd1:228-231`); `Sync:KommissionierNachtragEnabled`
   Default true / `…Uhrzeit` Default 2 (`ServiceSettingDefinitions.cs:292-293`); `SageLagerplatzAnlageAktiv` + `SageLagerAnlageLagertechniken`
   Default aus (`:319-321`); Blechdicke-Hinweistext (`SdataArtikelReader.cs:359-364`, `BfArtikelFelder.Blechdicke = "USER_OSBlechdicke"`);
   Nachlese-Warnung (`Sdata/BlechteileFelderNachlese.cs:48`); Konflikt 50014 mit PlatzID + `STRING_AGG … WITHIN GROUP`
   (`SqlSageLagerplatzAnlage.cs:42-53`); Join Mandant + Lagerkennung (`SageLagerplatzReader.cs:25`) und „Lagerkennung … zu lang“
   (`LagerplatzSyncService.cs:329-333`); Abbruchtext Fräsen-Pfad-Skript (`SQL/Einmalig/…FraesenPfad_Erstimport.sql:35-37`); Meldungen „kein
   Terminal konfiguriert“, „Terminal nicht dem angemeldeten Benutzer zugeordnet“ (+ „– nichts gebucht.“ aus `bde-fa-sicht.js:88`), „Werkbank
   ist nicht BDE-aktiv“, „Sollmenge erreicht – bitte Fertig melden“, „Aktueller Bestand“ gesperrt (`StockOverview/Index.cshtml:203`),
   Altbestand-Warnung (`PickingLeitstandController.cs:581-584`). SQL-Reihenfolge im Manifest 164 → 165 → (166, 167) → 168 → 169 wie in 03 5.4a.
   A-8.8 und A-8.9 lesend gegen `AKE_WMS_DEV` und `IDEAL_WMS_DEV` ausgeführt: laufen fehlerfrei (alle Spalten vorhanden; AKE ohne
   Teilmeldungen, IDEAL mit stornierten QA-Zeilen; A-8.9 listet Terminals/Werkbänke).
   Bildverweise `docs/anwender/*.md`: 120 eindeutige Pfade, alle vorhanden.
6. **DEV-Deploy** (Standort beide, elevated, Quelle Worktree, Web + Dienst, Stand `67868774` – QA-Korrektur betrifft nur `docs/`):
   `dev-deploy.ps1 -Standort AKE` Exit 0 „HTTP 200, Fusszeile v1.99.80“; `-Standort IDEAL` Exit 0 „HTTP 200, Fusszeile v1.99.80“.
   **TS-201 auf DEV (claude-in-chrome):**
   - 201.1 Fußzeile 1.99.80, Änderungen oberste Karte v1.99.80 „Doku-Nachzug“, darunter v1.99.79 – AKE OK, IDEAL OK.
   - 201.2 Hilfe BDE: Abschnitte „BDE-Seite (seit v1.99.78 …)“, „Tageshistorie“, „BDE am Handy“, Teileliste; kein „Terminal-Bedienung“ – AKE OK, IDEAL OK.
   - 201.3.1 Link „BDE › Terminal“ (`/BdeTerminal`) → `BdeTeileliste?workplaceId=…` Werker-Anmeldung – AKE OK (Stanzarei), IDEAL OK.
   - 201.3.2 Link „BDE › Tageshistorie“ → Werker-Anmeldung; nach Anmeldung (AKE, Testwerker) direkt Tageshistorie – AKE OK, IDEAL OK (Anmeldung erscheint).
   - 201.3.3 Benutzer ohne Terminal – NICHT MOEGLICH im Browser (Windows-Konto hat an beiden DEV ein Terminal); Meldungstext im Code belegt
     (`BdeTerminalController.cs:47`).
   - 201.3.4 Links der Hilfeseite: im View genau 4 Link-Ziele (`/SyncLog` 2×, `/BdeTerminal`, `/BdeTeileliste/Tageshistorie`), keine externen;
     alle öffnen ohne 404 an beiden Standorten – OK.
   - 201.4 Repo-Doku: README Zeile 1.99.80, 00-index Versionszeilen 1.99.70–1.99.80 lückenlos, AGENTS Stand v1.99.80, Statustabelle Kap. 17
     durchgehend (6 Zeilen), Bildverweise vollständig – OK.
7. **Publish:** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` (IDEALAKEWMSWeb, IDEALAKEWMSWebService, SQL, deploy,
   `publish-info.txt` Version 1.99.80, Commit `3c5a97df2454ccdef38ecebe2260bba72e630a48`). ZIP/Shares: Orchestrator.
   **Deploy:** Web und Dienst (nur `AppVersion` + Hilfeseite), keine Migration, kein SQL, kein Einmal-Skript. Nach dem Merge nur neu publishen,
   wenn der Merge getestete Dateien mit parallelen main-Änderungen kombiniert.

**Manuelle Restprüfung (Mensch):**
1. TS-201.3.3 mit einem Benutzer ohne Terminal-Zuordnung: Startseite mit gelber Meldung „kein Terminal konfiguriert“.
2. TS-201.3.2 an IDEAL nach Werker-Anmeldung: direkt Tageshistorie.
3. Hilfeseite und Änderungsseite am Testsystem einmal durchsehen (Text gegen Kenntnis der Werker).
## Paket 20 (Definition of Done, 2026-11-26)
- `publish\paket20-v1.99.80.zip` (Commit 3c5a97df, enthält v1.99.70–v1.99.80): **237 621 089 Bytes**, SHA-256 `7A3FD1B48A48224C0DA00ED60AE3A7A6FFAABB33792FE7698824EC3D1D666B48`.
- `\\AKENET01.ake.at\Installsourcen`: 237 621 089, SHA-256 gleich – OK.
- `\\IDEALWEB01.ideal.ideal-ake.at\InstallSourcen`: 237 621 089, SHA-256 gleich – OK.
- Deploy: Web + Dienst + SQL/164, 165, 168, 169 über das Update-Skript (`nachlauf.psd1`), Dienst erst nach dem Schema.
- Confluence (ausdrücklich beauftragt, 12fd3e44): 15 Seiten aktualisiert, Kapitel 19 neu (701005871). Mail-Entwurf [[2026-11-26-mail-wms-neuerungen-v1-99-70-80]] mit echten Links, **nicht gesendet** – erst nach dem Prod-Deploy versenden.

## Offen für Gerald (Schranke 2)
- Entscheide: F-3, F-4, F-11, F-14, F-20, F-21, F-22, F-29, F-30; QA-Hinweise H-A (Terminal-Guard auch an Pause/Fertig/Teilmenge/Gruppe), H-B (ex.Message im Altbestand-Warntext), H-C (`SELECT @@VERSION` am IDEAL-Sage-Server, STRING_AGG ab 2017).
- F-16 IDEAL: Dienstkonto `AKENB\svc_idealakewms` braucht Lesezugriff auf `IDEAL_TEST_2026_05_03`, dann Artikel-Abgleich IDEAL erneut prüfen.
- Live nicht prüfbar: F-2 (kein FA mit „Altbestand prüfen“), F-7 (Sage-Schreibschalter), F-6 Schreibtest, TS-198.6/.7, TS-201.3.3 (Benutzer ohne Terminal), TS-201.3.2 an IDEAL.
- Manual-UAT der vier Specs, dann EIN Merge. Kein Merge, kein Push erfolgt.

## UAT 2 (2026-11-26)

Auftrag Gerald 2026-11-26 06:12 („nochmals UAT und kritische Prüfungen“), qa-agent, 06:13–06:40. Stand DEV: Fußzeile **v1.99.80** an AKE (8081) und
IDEAL (8082), kein Neu-Deploy. Kein Code geändert, kein Build. Werkzeug: claude-in-chrome lieferte keine Screenshots (CDP-Zeitüberschreitung,
Renderer eingefroren) → Ablauf mit `playwright-core` + System-Chrome headless (Windows-Anmeldung `--auth-server-allowlist=localhost`), Konto
Gerald Weichbold (Admin). Werker IDEAL: 9901 „QA Werker B“ (op 3), 123 „Hans Guck“ (op 1); AKE: 4444 „Fred Ferdl“ (op 2).
Screenshots: `C:\Users\gerald.weichbold\AppData\Local\Temp\claude\8eae370df6a92eae\4ec633f6-7496-4a15-ae45-04bfbae6cd66\scratchpad\uat-2026-11-26\` (Dateiname = Funktion).

| # | Prüfpunkt | Standort | Ergebnis | Beleg |
|---|---|---|---|---|
| 1 | Fußzeile v1.99.80 | AKE, IDEAL | OK | Seitenfuß beider Sites |
| 2 | Werker-Anmeldung mit unbekannter Nr. 99999 | IDEAL | OK | „Unbekannte oder inaktive Personalnummer: 99999“, keine Liste |
| 3 | Anmeldung 9901 → FA-Sicht Wb 65 mit „Meine Buchungen“ | IDEAL | OK | `bde-fa-sicht-ideal.png` |
| 4 | Pause → Mengendialog; Abbrechen gibt Knopf frei | IDEAL | AUFFÄLLIG | Knopf zeigt „wird gesendet …“, solange erst der Dialog offen ist → U-6; `bde-mengendialog-pause-ideal.png` |
| 5 | Pause mit **leerer** Gutmenge | IDEAL | AUFFÄLLIG | als 0/0 gebucht (BdeBookingQuantities 33 an Buchung 36) → U-4 |
| 6 | Gutmenge −1 / 0.5 | IDEAL | AUFFÄLLIG | −1 und 0.5 abgewiesen (min 0, step 1); Dezimalmengen in der FA-Sicht nicht möglich, Teileliste erlaubt sie → U-7 |
| 7 | Fortsetzen | IDEAL | OK | „Fortgesetzt.“, Buchung 37 mit Parent 36 |
| 8 | Teilfertig 5 bei Soll 1 | IDEAL | AUFFÄLLIG | angenommen ohne Hinweis, danach Fertig 1 → 6 Gut auf Soll 1 (Mengen 34/35) → U-3; `bde-teilfertig-ueber-soll-ideal.png` |
| 9 | Fertig Gut 0 < Soll → eigenes Bestätigungsfenster; Abbrechen; dann Fertig 1 | IDEAL | OK | „Gutmenge 0 liegt unter der Sollmenge 1 …“, Knopf danach wieder bedienbar, „Fertig gemeldet.“; `bde-fertig-unter-soll-bestaetigung-ideal.png` |
| 10 | Doppelklick „Produktion“ | IDEAL | OK | genau 1 POST (Server-Log), Buchung 38 |
| 11 | Zwei Tabs: Tab B (Hans, veraltet) startet denselben AG | IDEAL | OK | Fenster „AG ist bereits in Arbeit durch QA Werker B an Entgraten Fläche seit 06:22.“, Fokus auf OK; `bde-kollision-zweiter-tab-ideal.png` |
| 12 | Neuladen bei offenem Mengendialog; Zurück-Knopf aus Tageshistorie | IDEAL | OK | nichts gebucht, Kopf unverändert; Zurück landet auf der FA-Sicht im gleichen Zustand |
| 13 | Tageshistorie | IDEAL | AUFFÄLLIG | Zeilen korrekt (06:21–06:21 Gut 6), aber Buchung 36 (Start 25.11. 22:46, Ende 26.11. 06:20) fehlt → U-8; `bde-tageshistorie-ideal.png` |
| 14 | Scan „9999999“, dann Scan Haupt-FA 1060414 | IDEAL | AUFFÄLLIG | „FA „9999999“ nicht gefunden.“ OK; Auswahl 19 AG ohne laufenden/fertigen OK; alte Fehlermeldung bleibt beim nächsten Scan stehen → U-9; `bde-scan-hauptfa-auswahl-ideal.png` |
| 15 | Produktion an zweiter Zeile starten, während eigene Produktion läuft (Standard-Einstellungen) | IDEAL | AUFFÄLLIG | „Mengen-Eingabe erforderlich – nichts gebucht.“, kein Dialog, kein Hinweis, was zu tun ist → **U-1** |
| 16 | Tastatur: Mengendialog „0“ + Enter | IDEAL | AUFFÄLLIG | Fokus in Gutmenge OK, Enter bestätigt nicht (Dialog bleibt) → U-10 |
| 17 | TS-198.6 Weitere aktive Buchungen (Mehrfachbuchung je AG kurz an) | IDEAL | OK | „Auf diesem Arbeitsgang arbeiten noch: Hans Guck – seit 06:29“; „Nein, nur meine“ → Hans läuft weiter; „Ja, alle beenden“ → „Fertig gemeldet. 1 weitere Buchungen beendet.“ (Buchungen 39–43); `bde-weitere-aktive-buchungen-ideal.png`. War bisher NICHT MOEGLICH |
| 18 | TS-198.7 Alle Produktionsaufträge abschliessen (Mehrfach je Werker + gleichzeitiger Abschluss kurz an) | IDEAL | **FEHLER** | Fenster und Abbrechen OK, „2 Buchungen fertig gemeldet.“; aber beide Zeilen „1060414/EG Entgraten Fläche“ (nicht unterscheidbar), Gut mit 0 vorbelegt (eingegebene 1 verworfen), ohne Rückfrage 0/0 final gebucht (Buchungen 44/45), Tabelle rechts abgeschnitten → **U-2**; `bde-alle-produktionsauftraege-abschliessen-ideal.png` |
| 19 | Handy 390 px FA-Sicht und Teileliste | IDEAL | AUFFÄLLIG | keine Querscrollleiste (scrollWidth 390), Touch-Knöpfe 44 px hoch; FA-Sicht als Karten 12 930 px lang, Aktion je Karte ganz unten → U-11; `bde-fa-sicht-handy-390-ideal.png`, `bde-fa-sicht-handy-390-karten-ideal.png`, `bde-teileliste-handy-390-ideal.png` |
| 20 | Teileliste Einzeln Wb 67: Melden leer → Pflichtfeld; Teilfertig 0.5 per Doppelklick; Zurücknehmen mit Bestätigung | IDEAL | OK | 1 POST, Teilmeldung 34 (0,5), „Teilmeldung zurücknehmen? …“ → storniert; `bde-teileliste-teilgemeldet-ideal.png` |
| 21 | Melden-Dialog Vorbelegung, Erfolgstexte | IDEAL | AUFFÄLLIG | Gut vorbelegt „1.0000“; „Gemeldet: 1.“ ohne Matchcode; „1 Zeilen“, „1 weitere Buchungen“, „Eintraege“ → U-12 |
| 22 | AK 14: zweiter Tab meldet dieselbe Position | IDEAL | OK | „Gemeldet: 0. Bereits gemeldet: 1 – nichts geändert (… bereits gemeldet von QA Werker B um 06:35)“, keine zweite Zeile (Meldung 35); `bde-teileliste-bereits-gemeldet-zweiter-tab-ideal.png` |
| 23 | Summiert: Haken am Matchcode → „Auswahl fertigmelden“ (Doppelklick) → Bestätigung | IDEAL | OK | „1 Zeilen mit ihrer Restmenge fertigmelden (Summe 1)?“, 1 POST, Meldung 36, danach zurückgenommen; `bde-summiert-block-bestaetigung-ideal.png` |
| 24 | Admin-Hinweis in der Werker-Liste | IDEAL | AUFFÄLLIG | „Hinweis: 1 Meldung(en) ohne passende Strukturzeile … Prüfung durch den BDE-Admin …“ nach jeder Werker-Aktion → U-13 |
| 25 | Lagerbestell-Zeilen (Werkbank-Option kurz an) | IDEAL | OK | Badge „Lagerbestellung #8 TS-193 QA-A“, Termin 15.12.2026 = 30.11. + 10 AT inkl. Feiertag 8.12.; nur 50001534 („KA LA“); 50002070 („SÄ IF“) und 50001536 („if“) korrekt nicht; `bde-teileliste-lagerbestellung-ideal.png` |
| 26 | Fräsen-Pfad (`FraesenPfadAktiv` kurz an): Link, Kopieren, Pflege, Prüfungen | IDEAL | OK | Link `file://idealfs1/Daten$/Produktion/CAM/EKW%20ISO/Boden%201230-620`; Zwischenablage = UNC-Text; `Y:\…`, Text ohne `\\`, 500 Zeichen ohne Freigabe → Warnung, nichts gespeichert; mit Leerzeichen gespeichert → getrimmt, Modified* = persönliches Konto; Teileliste-Zahnrad listet „Fräsen-Pfad“; `fraesenpfad-pflege-warnung-ideal.png` |
| 27 | Hilfetext Pflegefeld | IDEAL | AUFFÄLLIG | „UNC-Pfad, Y: = \\idealfs1\…“ liest sich wie „Y: geht“, wird aber abgewiesen → U-14 |
| 28 | Link am Fräsen-PC öffnen; „Standard ausgeblendet“ für Konto ohne gespeicherte Spaltenwahl | IDEAL | NICHT MOEGLICH | kein Fräsen-PC; Konto 8 hat gespeicherte Wahl (UserViewPreferences 31) |
| 29 | Blechdicke-Werte aus `USER_OSBlechdicke` | IDEAL | NICHT MOEGLICH | Sync aus + F-16 (Lesezugriff Dienstkonto); Spalte zeigt „–“ |
| 30 | Kommissionier-Nachtrag Admin-Knopf (+ Doppelklick) | AKE, IDEAL | OK | Tooltip „Notfall …“; „0 Kommissionierauftrag/-aufträge für 0 FA angelegt; 3 (IDEAL) / 2 (AKE) FA ohne offene Restmenge.“, je 1 POST, PickingOrders unverändert (IDEAL max 6, AKE 120); `kommissionier-nachtrag-knopf-ideal.png`, `kommissionier-nachtrag-knopf-ake.png` |
| 31 | Nachtlauf Kommissionier-Nachtrag beobachten | AKE, IDEAL | NICHT MOEGLICH | braucht `Sync:KommissionierNachtragEnabled`; keine SyncLogs „Nachtrag“; der Knopf ruft dieselbe Erzeugung (stellvertretend Zeile 30) |
| 32 | Checkliste 8: `Sync:KommissionierNachtragUhrzeit` = 24 | IDEAL | OK | „Nicht gespeichert: … erlaubt: eine ganze Zahl von 0 bis 23.“; ServiceSettings-Abzug vorher/nachher ohne Unterschied; `servicesettings-nachtrag-uhrzeit-24-abgelehnt-ideal.png` |
| 33 | Lagerplatz-Anlage bei Schalter aus: Knopf und direkter Aufruf `CreateInSage` | AKE, IDEAL | OK | Knopf ausgeblendet; Aufruf → Liste mit „Lagerplatz-Anlage in Sage ist ausgeschaltet (…) – es wurde nichts angelegt.“; `lagerplatz-anlage-schalter-aus-ideal.png`. Hinweis: an beiden DEV Banner „1 Lagerplatz wartet auf die Übergabe an Sage“ (QA-Rest 25.11.) |
| 34 | Lagerplatz-Maske bis vor das Senden | AKE, IDEAL | NICHT MOEGLICH | Maske nur mit `SageLagerplatzAnlageAktiv` erreichbar (verboten) |
| 35 | AKE: Anstempeln Rüsten/Produktion/Fertig (AK 32b) | AKE | NICHT MOEGLICH | alle 4 BDE-Werkbänke „Keine offenen Arbeitsgänge an dieser Werkbank.“ |
| 36 | AKE: „Meine Buchungen“ Fred Ferdl → Fortsetzen | AKE | AUFFÄLLIG | 3 auto-pausierte Buchungen vom 07.07. mit „Fortsetzen“; Stanzarei → „Dieser Arbeitsgang gehört nicht zu dieser Werkbank. – nichts gebucht.“, A1 → „FA 2609192 ist bereits abgeholt — keine BDE-Buchung mehr moeglich. – nichts gebucht.“ → **U-5**; `bde-ake-fortsetzen-stanzarei.png`; Tageshistorie AKE „Noch keine Buchungen …“ OK |
| 37 | Benutzer ohne Terminal, Android-Handy mit lokalem Konto, echte Gleichzeitigkeit zweier Geräte (AK 16) | AKE, IDEAL | NICHT MOEGLICH | ein Windows-Konto mit Terminal, kein Gerät; nur nacheinander in zwei Tabs geprüft (Zeilen 11, 22) |

**Zähler:** OK 17 · FEHLER 1 · AUFFÄLLIG 13 · NICHT MOEGLICH 6 (37 Prüfpunkte).

### Neue Befunde UAT 2
- **U-1 (mittel)** FA-Sicht: Produktion/Rüsten an einer zweiten Zeile, während die eigene Produktion läuft → nur „Mengen-Eingabe erforderlich –
  nichts gebucht.“ (`BdeBookingService.cs:102-106`, `bde-fa-sicht.js` `case 'QuantityRequired'`). Häufigster Morgenfall „AG wechseln“; der Werker
  weiß nicht weiter. Vorschlag: Text „Es läuft noch Produktion <FA/AG> – erst Pause oder Fertig melden.“, besser gleich den Mengendialog für die
  laufende Buchung öffnen und danach starten.
- **U-2 (mittel, FEHLER)** Fenster „Alle Produktionsaufträge abschliessen“ (TS-198.7): Zeilen nur „FA/AG“ ohne Sub-FA (zwei gleiche Zeilen), Gut 0
  vorbelegt statt eingegebener Menge/Rest, keine Gut-unter-Soll-Rückfrage (0/0 final gebucht, Buchungen 44/45), Tabelle bei 1400 px rechts
  abgeschnitten (Ausschuss/„Abschl.“ halb sichtbar), „Abbrechen“ hellblau mit weißer Schrift (Kontrast geschätzt unter 3:1). Vorschlag: Sub-FA
  und Matchcode je Zeile, Vorbelegung Rest, Rückfrage wie Einzel-Fertig, `modal-xl` + `table-responsive`, Abbrechen `btn-outline-secondary`.
  Wirkt nur mit `BdeGleichzeitigerAbschlussBeiMehrfachStart` = an (Standard aus).
- **U-3 (niedrig)** FA-Sicht „Teilfertig“ über der Sollmenge ohne Hinweis; die Teileliste meldet „Sollmenge erreicht – bitte Fertig melden“
  (`BdeTeilelisteService.cs:658`) – eine Regel, zwei Masken. Vorschlag: denselben Hinweis in der FA-Sicht.
- **U-4 (niedrig)** Mengendialog FA-Sicht: leere Gutmenge wird still 0 (`bde-fa-sicht.js` `parseFloat(gut.value || '0')`, Feld ohne `required`).
  Bei Fertig fängt die Gut-unter-Soll-Rückfrage das ab, bei Pause/Teilfertig nicht. Vorschlag: `required` wie im Teileliste-Dialog.
- **U-5 (mittel, AKE)** „Meine Buchungen“ zeigt alte auto-pausierte Buchungen (FA abgeholt, andere Werkbank) mit „Fortsetzen“, das nie gelingen
  kann; die Werkbank der Buchung steht nicht da; Meldetexte uneinheitlich („moeglich“, Strich doppelt). Vorschlag: Werkbank im Kopf zeigen,
  bei abgeholtem FA „Beenden“ statt „Fortsetzen“; Altbestand pausierter Buchungen am Testsystem/Prod per Abfrage (Status 5 je Werker) prüfen.
- **U-6 (niedrig)** Kopf-Knopf zeigt „wird gesendet …“, während erst der Mengendialog offen ist. Vorschlag: Sperrtext erst beim Senden.
- **U-7 (niedrig, Rückfrage)** FA-Sicht Mengen nur ganzzahlig (`_BdeDialoge.cshtml:8-9` `step="1"`, vom alten Terminal übernommen); Teileliste
  `step="any"`. Klären, ob AG-Mengen in m/kg vorkommen.
- **U-8 (niedrig, Rückfrage)** Tageshistorie filtert Buchungen nach Startdatum (`BdeTageshistorie.cs`, `StartedAt >= von`): eine Buchung über
  Mitternacht fehlt am Folgetag. Gibt es Nachtschicht? Dann „Start oder Ende am Tag“.
- **U-9 (niedrig)** Alte Meldungen bleiben stehen (Scan-Fehler neben dem nächsten Scan-Ergebnis, „Produktion gestartet.“ neben neuem Dialog).
- **U-10 (niedrig)** Enter im Mengendialog der FA-Sicht bestätigt nicht (kein `<form>`); im Teileliste-Dialog schon. Vorschlag: Enter = Bestätigen.
- **U-11 (niedrig)** Handy 390 px: jede AG-Karte ~470 px mit 15 Feldern, Aktion unten; 25 Karten = 12 930 px. Vorschlag: Aktion oben, auf dem
  Handy nur FA/Sub-FA/AG/Matchcode/Status.
- **U-12 (kosmetisch)** „1.0000“ als Vorbelegung, „Gemeldet: 1.“ ohne Matchcode, „1 Zeilen“, „1 weitere Buchungen“, „Meldung(en)“,
  „Kommissionierauftrag/-aufträge“, „Eintraege“ (Pagination, Bestand).
- **U-13 (niedrig)** Admin-Hinweis „Meldung(en) ohne passende Strukturzeile … BDE-Admin“ steht beim Werker nach jeder Aktion. Vorschlag: nur für
  BDE-Admin zeigen oder einmal je Sitzung.
- **U-14 (kosmetisch)** Hilfetext Fräsen-Pfad „Y: = \\idealfs1\Daten$\Produktion\CAM“ → „Nicht Y:\ eintragen, sondern \\idealfs1\Daten$\Produktion\CAM\…“.

### Testdaten auf DEV (UAT 2)
- **IDEAL_WMS_DEV:** BdeBookings 37–46 (op 3 „QA Werker B“, op 1 „Hans Guck“, Wb 65, AG 820/824/828/832/837/841/845/849); Buchung 36 um 06:20
  pausiert (Menge 0/0), 38, 40 und 46 pausiert, übrige fertig/beendet, keine läuft. BdeBookingQuantities ab 33. BdeTeilMeldungen 34–36 alle
  zurückgenommen. Article 7878 `CamPath` mit gleichem Wert neu gespeichert (Modified 06:27). AppSettings `FraesenPfadAktiv` jetzt als Zeile **false**
  (vorher keine Zeile = Standard aus); `BdeMehrfachBuchungProArbeitsgang`, `BdeMehrfachBuchungProOperator`, `BdeGleichzeitigerAbschlussBeiMehrfachStart`
  kurz an, wieder **false**; Wb 67 `BdeTeilelisteLagerbestellungen` kurz an, wieder **0**. Keine StockMovements, keine PickingOrders, keine Sage-Warteschlange.
- **AKE_WMS_DEV:** keine Datenänderung (Fortsetzen abgewiesen, Nachtrag 0 angelegt).
- Kein Sage-Schreiben, kein `Sync:*Enabled`/`Sage*Aktiv` eingeschaltet, kein Code geändert.

---
type: spec
title: Sage-Abgleiche auf SData umstellen (View als Notfall-Rückfall), Felder erweitern, Rückgabe WMS→Sage vorbereiten
slug: 2026-11-06-sage-sdata-stammdaten-sync-spec
status: Testbereit
created: 2026-11-06
updated: 2026-11-06
source_backlog: "[[2026-10-06-kreditoren-stammdaten]]"
task: "[[2026-11-06-sage-sdata-stammdaten-sync-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - "Code-Stand aller Fundstellen: Bündel-Worktree .claude/worktrees/2026-08-07-ideal-teile-1-5, v1.72.0 (IdealAkeWms/AppVersion.cs:5), am 2026-11-06 nachgeprüft"
  - "SData-Client (wiederverwenden + erweitern): IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs:9-17 (ISageSdataReadClient, heute nur RealTimeData :32, ohne Paging, wirft nie bei HTTP-Fehler :81-86), :30-119; SageSdataFaModels.cs:25-29 (SdataFeed<T>), :32-43 (SdataCall), :49-54 (SdataLeseZiel), :56-60 (SageBool); IdealAkeWms/Services/SdataUrlBuilder.cs:26-39"
  - "SData-Einstellungen: IdealAkeWms/Models/ServiceSettingDefinitions.cs:54-57 (SData:BaseUrl/Application/ServiceContract/Dataset, DB-first), :63 (SageLagerbuchungSslZertifikatPruefen); Zugangsdaten appsettings SageLagerbuchung:Username/Password (IDEALAKEWMSService/Workers/SageBookingWorker.cs:104-105); HttpClient+TLS-Callback IDEALAKEWMSService/Program.cs:98-122"
  - "Reader A1: IDEALAKEWMSService/Services/ISageLagerplatzReader.cs:6 (SageLagerplatzDto), :8 (Interface); SageLagerplatzReader.cs:16-50 (SQL KHKLagerorte/KHKLagerplaetze, Mandant=1 :27); DI IDEALAKEWMSService/Program.cs:58"
  - "Sync A1: IDEALAKEWMSService/Services/LagerplatzSyncService.cs:44 (Reader-Aufruf), :46-61 (Reader-Fehler: FinishFailed + return, kein throw), :85 (Schlüssel = Code/Kurzbezeichnung), :120-177 (Upsert), :186-200 (Deaktivieren), :15 (SyncUser system:sync)"
  - "Reader A2: IDEALAKEWMSService/Services/ISageBestandReader.cs:4 (SageBestandDto), :6; SageBestandReader.cs:24-36 (Mandant=1 :33); DI Program.cs:60"
  - "Sync A2: IDEALAKEWMSService/Services/LagerbestandSyncService.cs:52 (Reader), :54-67 (Reader-Fehler ohne throw/Mail), :96-103 (Vorladen), :205-271 (Nullsetzen-Cap), :14 (SyncUser)"
  - "A3 Artikel (kein Reader-Interface, SQL inline): IDEALAKEWMSService/Services/SageImportService.cs:369-570 (SyncArticlesAsync; Read :384-425, Upsert :461-497, Count-Keys :553-559); SageArticleSql.cs:11-53 (AKE-Filter: Ressourcenummern KHKPpsRessourcenPositionen ∪ IstBestellartikel=-1 und Aktiv=-1), :61-76 (IDEAL-View, Spalten laut P4)"
  - "A4 FA flach (AKE, nur vorbereiten): IDEALAKEWMSService/Services/SageImportService.cs:33-367 (vw_AKE_Kommissionierung_WAListe :47-59)"
  - "A5 FA-Struktur (kein Reader-Interface, SQL inline): IDEALAKEWMSService/Services/FaHierarchySyncService.cs:46-173, ReadNodesAsync :206, ReadOrderInfosAsync :265, Full-Refresh :138-147; FaHierarchySql.cs:139-152 (Knoten-Select mit EXISTS auf FAInfos), :155-163 (FAInfos-Select); FaMaterializationSyncService.cs:30, :32 (ServiceUser); Viewdoku SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql:21-53, vw_IDEAL-AKE_Kommissionierung_FAInfos.sql:9 (Quelle PPSImport, nicht Sage)"
  - "A6 Stückliste (AKE-only, siehe G13): IDEALAKEWMSService/Services/BomCacheSyncService.cs:43-49 (Klasse-D-Gate: im hierarchischen Modus übersprungen), :270-329 (Read [ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB] :294); IdealAkeWms/Data/Repositories/BomRepository.cs:9"
  - "A7/A8: IDEALAKEWMSService/Services/ISageArbeitsplatzReader.cs:6 (SageArbeitsplatzDto), :10, :12; SageArbeitsplatzReader.cs:16-55, SageArbeitsplatzSql :66-94 (Tabelle KHKPpsArbeitsplaetze inkl. USER_ArbeitsSchritt :71-76 oder View :90-93); ProductionWorkplaceSyncService.cs:14, :23, :57; OperationConfigSyncService.cs:35, :47, :112 (liest DENSELBEN Arbeitsplatz-Reader), :272 (Kürzel = Arbeitsschritt)"
  - "A9 unverändert: IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs:26, :32 (ISageZusatzinfoReader); DI Program.cs:61"
  - "A10/Umbuchung: IDEALAKEWMSService/Services/ISageBuchungLookupReader.cs:8; SageBuchungLookupReader.cs:14-15, :22-33; IDEALAKEWMSService/Workers/SageBookingWorker.cs:90, :117-140, :177-190; SageLagerbuchungPayloadBuilder.cs:13-55 (nur Zugang/Entnahme :32-42); IdealAkeWms/Services/SageBookingEnqueueDecision.cs:14-15 (Umbuchung ausgeschlossen), :23-28; IdealAkeWms/Data/Repositories/SageBookingEnqueueingStockMovementRepository.cs:57; SageBookingQueueRepository.cs:142 (eigener Typfilter Ein-/Ausbuchung); IdealAkeWms/Models/MovementType.cs:7; StockMovement.cs:38 (SourceStorageLocationId)"
  - "Steuerung: IDEALAKEWMSService/Workers/SyncWorker.cs:85-96 (Artikel), :314-331 (Lagerplatz), :337-351 (FA-Hierarchie), :377-406 (Werkbank/Arbeitsgang), :431-450 (Lagerbestand), :498-520 (NotifyErrorAsync/RunResilientAsync); IdealAkeWms/Models/ServiceSettingDefinitions.cs:16-101 + Drift-Guard IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs; IdealAkeWms/Services/SyncLogger/SyncLogServices.cs:8, :54-84 (DisplayNames)"
  - "Entitäten: IdealAkeWms/Models/Article.cs:5-59 + ApplicationDbContext.cs:218-249; StorageLocation.cs:5-85 (Source :38, SageLagerkennung :63, SageLagerplatzId :71) + ApplicationDbContext.cs:182-207; BdeOperator.cs:5-31 (keine Kartennummer) + ApplicationDbContext.cs:1089-1094; CachedBomItem.cs:9-41 + ApplicationDbContext.cs:993-1012; ProductionWorkplace.cs:5-58 + ApplicationDbContext.cs:211-215; OperationConfig.cs:11-49; ArticleAttributeValue.cs:3-15 (KEIN TextValue), ArticleAttributeDefinition.cs:9-12, :31-35 (SyncSource/SyncFieldName, von keinem Code geschrieben) + ApplicationDbContext.cs:268-314; Vorlage TextValue nvarchar(1000) ApplicationDbContext.cs:658; FaHierarchyNode.cs:13-63 + ApplicationDbContext.cs:1015-1042; FaHierarchyOrderInfo.cs:12-43; ProductionOrder.cs:54 (SageMissingSince); Satelliten-Vorlage ProductionOrderExtraInfo.cs:13"
  - "Kreditoren-Bezüge (Anhang-Spec, korrigierte Zeilen): IdealAkeWms/Program.cs:66-67, :129, :135 (Repository-Registrierung); Controllers/ProductionWorkplacesController.cs:12, :60, :83; StorageLocationsController.cs:11, :78; BdeMasterDataController.cs:10-12 (BdeOperator-Pflege, BDE-Rollen); Models/ViewModels/ColumnDefinitions.cs:575 (GetByViewKey, war :467); Views/Shared/_Layout.cshtml:316-337 (Menü Stammdaten, war :282-301); Views/Users/RoleOverview.cshtml:42, :55; Services/BeschichtungsauftragService.cs:100-101; Models/Beschichtungsauftrag.cs:24-25 (Dienstleister bleibt Freitext, kein FK); FaHierarchySyncService.cs:138-147 (war :129-138), :265; FaHierarchySql.cs:155-163 (war :135-143)"
  - "Migration/Doku: IdealAkeWms/Migrations/ (letzte 20261019092105_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung = SQL/97), SQL/98_… bis SQL/108_… (Etappen-Tabelle), SQL/00_FreshInstall.sql (je zwei Stellen), SQL/AgentJobs/02_Import_Artikel.sql:1-27 (deprecated, nur INSERT benannter Spalten – nicht betroffen); docs/TESTSZENARIEN.md (TS-106, letzte TS-105 :10686); IdealAkeWms/AppVersion.cs:5 + IDEALAKEWMSService/AppVersion.cs; Views/Help/Changelog.cshtml"
  - "NEU (Pfade festgelegt): IDEALAKEWMSService/Services/Sdata/ (SData-Reader je Bereich + SageQuelleSwitch-Reader + SdataVergleich), IdealAkeWms/Models/{StorageArea,ArticleSageDetail,ProductionOrderPosition,Stillstandsgrund,Creditor,Debitor,ArticleSupplier,SageStammdatenOutboxItem}.cs, Controllers/{Creditors,Debitors}Controller.cs + Views"
open_questions:
  - "G1 Lizenz/Freigabe: Bestätigt DPS, dass CommonWawiServicesResources (+ …Specific) und die PPS-Services für uns freigegeben und lizenziert sind (AKE-Test, IDEAL, produktiv)? Ohne Bestätigung keine Umsetzung von Etappe 1 ff. (Empfehlung: Freigabe abwarten, Etappe 0 trotzdem bauen – sie ist ohne SData nutzlos, aber risikolos.)"
  - "G2 IDEAL-Endpunkt: SData für die IDEAL-Sage ist noch nicht gekauft. Bis dahin entwickeln und testen gegen den AKE-Test (sagetest01, ake_TEST2026;1)? (Empfehlung: ja; die Schalter liegen in den ServiceSettings der jeweiligen Installations-DB, IDEAL bleibt bis zum Kauf auf View. Achtung: der AKE-Test trägt die AKE-Benutzerfelder – IDEAL-Felder sind erst am IDEAL-Endpunkt prüfbar, siehe G11.)"
  - "G3 BEANTWORTET (Gerald 2026-11-06): alle vorgeschlagenen Artikelfelder vorsehen – übernommen in F5 und Felderweiterung."
  - "G4 FA-Zusatzinformationen (Kaeltemittel, Ventil, AusfuehrungEZ, Maschine, SageStatus) sind über SData nicht lesbar (FA-USER-Felder fehlen). Bleibt FaZusatzinfoSyncService auf der View, bis DPS BelegProperty1–4 belegt? (Empfehlung: ja; keine eigene Etappe – der frühere Verweis „Etappe 7“ passte nicht zur Etappenliste, E7 ist der Lagerbuchungs-Lookup. A9 bleibt unverändert, ISageZusatzinfoReader ist die spätere Andockstelle.)"
  - "G5 Datenhoheit WMS-geführter Stammdaten: Lagerplätze und BDE-Personal im WMS anlegen und nach Sage übergeben – dafür gibt es heute KEINEN SData-Service (kein Lagerplatz-, kein Mitarbeiter-Service). Bei DPS beauftragen? (Empfehlung: ja; bis dahin WMS führt, Übergabe-Status 'offen', Anlage in Sage manuell.)"
  - "G6 Kreditoren/Debitoren: Tabellen werden in dieser Spec angelegt (Entscheidung Gerald 2026-11-06). Offen: Felder nur aus Sage füllen (Sage führend) oder zusätzlich WMS-eigene Kreditoren ohne Sage-Nummer zulassen (Kreditoren-Spec: Dienstleister/Beschichter)? (Empfehlung: beides – Herkunft Sage | WMS, Sage-Felder bei Herkunft Sage nicht editierbar.)"
  - "G9 BEANTWORTET (Gerald 2026-11-06): alles auf einmal – der Umfang der Kreditoren-Spec ist Teil dieses Epics (F10, Etappe 8); [[2026-10-06-kreditoren-stammdaten-spec]] ist darin aufgegangen. Deren offene Fragen 1–7 gelten hier weiter (Empfehlungen dort übernommen, siehe F10)."
  - "G7 Bestellvorschläge: Lager-/Glasbestellungen künftig als Dispo-Stapel nach Sage? (Service getestet; Empfehlung: als eigene Folge-Spec, hier nur Datenbasis.)"
  - "G8 Delta: GeaendertAm am FA wird bei Service-Änderungen nicht gesetzt (Befund §14), Timestamp-Filter scheitert. Vollabgleich der offenen FAs je Lauf akzeptiert? (Empfehlung: ja, offene FAs + Fenster beendete letzte 30 Tage.)"
  - "G10 Fehlermail bei Reader-Ausfall A1/A2: Heute schickt ein Lese-Fehler bei Lagerplatz/Lagerbestand KEINE Mail (LagerplatzSyncService.cs:46-61, LagerbestandSyncService.cs:54-67 enden ohne throw; SyncWorker.cs:321, :438 prüft Errors nicht). AK 7 (vor dem Rework AK 5) verlangt eine Mail. Für Quelle SData/Vergleich zusätzlich mailen, View-Verhalten unverändert lassen? (Empfehlung: ja – im bestehenden catch ein NotifyAsync, LagerbestandSyncService hat ISyncErrorNotifier schon :20, LagerplatzSyncService bekommt ihn.)"
  - "G11 FA-Struktur nicht vollständig aus SData: FAInfos stammt aus [PPSImport].[dbo].[vw_PPSData], nicht aus Sage (vw_IDEAL-AKE_Kommissionierung_FAInfos.sql:9), und filtert die Knoten (FaHierarchySql.cs:149-151). 13 Knotenfelder sind laut Viewdoku 'Position bevorzugt, Beleg als Fallback' (FAListe.sql:37-48, u. a. Arbeitsschritte, Kommissionieren, Arbeitsbereich) – im SData-Auszug nicht belegt. Soll E4 SData nur für Kanten + Positionen nutzen und FAInfos/Knotenfelder auf SQL lassen, bis P-F2 sie am IDEAL-Endpunkt belegt? (Empfehlung: ja, Umschalten von Sync:Quelle:FaStruktur auf SData erst nach P-F2; Vergleichsmodus ab E4a.)"
  - "G12 A8 Arbeitsgänge zeigt auf die falsche Quelle: OperationConfig-Einträge sind Arbeitsschritt-Kürzel der ARBEITSPLÄTZE (OperationConfigSyncService.cs:47, :112, :272), nicht Sage-Arbeitsgänge (PpsArbeitsgaengeInfo). A8 dem A7-Reader folgen lassen (PpsArbeitsplaetzeInfo inkl. USER_ArbeitsSchritt, falls P-A1 es belegt) und Rüst-/Stückzeit/Standard-Arbeitsplatz aus F8 streichen bzw. ins Rückmelde-Epic verschieben? (Empfehlung: ja.)"
  - "G13 E5 Stückliste wirkt an IDEAL nicht: BomCacheSyncService ist im hierarchischen Modus hart übersprungen (BomCacheSyncService.cs:43-49, ADR 0013) und liest die AKE-View [ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB] (:294); AKE ist out of scope. E5 – und ebenso der vorbereitete AKE-Reader für FA flach (A4) – aus dem Epic nehmen und bis zur AKE-Freigabe zurückstellen? (Empfehlung: ja, beides; IDEAL-Positions-Benutzerfelder laufen über G11.)"
  - "G14 PPS-Mitarbeiter-Zuordnung: BdeOperator hat KEINE Kartennummer (BdeOperator.cs:5-31); das Terminal identifiziert über PersonnelNumber (BdeApiController.cs:52-58, BdeOperatorRepository.cs:21-22). Gegen welches Sage-Feld wird PersonnelNumber verglichen – Nummer oder Kartennummer? (Empfehlung: neues Feld SageMitarbeiternummer; Zuordnungsvorschlag nur bei exakter Gleichheit PersonnelNumber = Sage-Feld laut P-M1, Übernahme immer manuell bestätigt, nie über den Namen.)"
  - "G15 FA-Kopf-Status doppelt geplant: das Rückmelde-Epic [[2026-11-02-sage-pps-fa-bde-abgleich-spec]] legt in seiner Etappe 1 die Tabelle SageFaStates (Status-Bits je BelID) an und plant ebenfalls SQL/98/99. Status-Bits nur dort führen und in E4b nur ProductionOrderPositions anlegen? (Empfehlung: ja – wer zuerst umgesetzt wird, legt SageFaStates an, der andere nutzt sie; SQL-Nummern vergibt der Zweig, der zuerst startet.)"
  - "G16 Artikel-Filter: Der IDEAL-Filter steckt im Viewtext von vw_IDEAL_AKE_WMS_Artikel (nicht im Repo, SageArticleSql.cs:56-60 belegt nur Spalten); der AKE-Filter liest alle Ressourcenummern der PPS-Ressourcenpositionen (SageArticleSql.cs:21-25, SData 240.552 Zeilen) ∪ IstBestellartikel/Aktiv (:37-40). Welche Menge soll der SData-Reader lesen? (Empfehlung: Nachbau des View-Filters nach P-R1; bis dahin bleibt A3 auf View, E3a baut Reader + Vergleich.)"
  - "G17 SQL-Nummern ab 100: dreistellige Namen sortieren lexikographisch vor 11_…99_ (SQL/100_ vor SQL/11_). Dreistellig weiterzählen und die Reihenfolge über die Deploy-Liste sichern? (Empfehlung: ja, Nachtrag zu ADR 0004 im Brain-Update.)"
epic: true
etappen:
  - "E0 SData-Lese-Client erweitern (Vertrag + Paging), Quellenschalter-Muster, Vergleichsmodus (kein Bereich aktiv, keine Migration)"
  - "E1 Lagerorte und Lagerplätze (SQL/98)"
  - "E2 Lagerbestände (keine Migration)"
  - "E3a Artikel-Reader + Satellit ArticleSageDetails (SQL/99)"
  - "E3b Artikel-Benutzerfelder als Merkmal (ArticleAttributeValues.TextValue, SQL/100)"
  - "E4a FA-Struktur-Reader Kanten + Vergleichsmodus (keine Migration)"
  - "E4b ProductionOrderPositions + Lesefilter eingelastet/aktiv (SQL/101)"
  - "E5 Stückliste BOM-Cache (AKE-only, nur falls G13 sie im Epic lässt; SQL/102 reserviert)"
  - "E6a Arbeitsplätze (+ Arbeitsgang-Kürzel laut G12) (SQL/103)"
  - "E6b PPS-Mitarbeiter + Stillstandsgründe (SQL/104)"
  - "E7 Lagerbuchungs-Lookup über SData; Umbuchung in die Buchungs-Queue (keine Migration)"
  - "E8a Kreditoren-Stamm + Pflege-Oberfläche + Dienstleister-Meldung (SQL/105)"
  - "E8b Debitoren-Stamm + Pflege-Oberfläche (SQL/106)"
  - "E8c Lese-Abgleich Kontokorrent → Kreditoren/Debitoren + ArticleSuppliers (SQL/107)"
  - "E9 Vorbereitung WMS→Sage-Stammdatenübergabe (Outbox, Status, ohne Sage-Service) (SQL/108)"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

# Sage-Abgleiche auf SData umstellen – View bleibt als Notfall-Rückfall

Grundlagen (Befunde, nicht Teil des Auftrags): [[sage-sdata-wms-schnittstellenkatalog]] · [[sage-sdata-commonwawiservicesresources]] ·
[[sage-sdata-commonwawiservices]] · [[sdata-2-0-spezifikation]] · [[sdata-befund-ake-test-2026-10-30]] (§10–§14) ·
DPS-Antwort [[2026-11-03-dps-anfrage-produktion-sdata]] · Rückmeldungen (eigenes Epic) [[2026-11-02-sage-pps-fa-bde-abgleich-spec]] ·
Kreditoren [[2026-10-06-kreditoren-stammdaten-spec]] · Heutige Integrationen [[integrationen]], [[services]], [[datenmodell]].

**Umsetzungsort:** Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Zweig `feature/2026-08-07-ideal-teile-1-5` (Bündel, Entscheidung
Gerald 2026-11-06). Alle Datei:Zeile-Angaben beziehen sich auf diesen Stand (v1.72.0) und sind am 2026-11-06 am Code nachgeprüft (Rework).

**Standort beide:** Die fachliche Wirkung (SData statt View) ist IDEAL; aber Lagerplatz-, Lagerbestand- und Artikel-Abgleich laufen ohne
Master-Gate an beiden Standorten (`SyncWorker.cs:85`, `:314`, `:431`), die Migrationen und das Kreditoren-/Debitoren-Menü kommen mit dem Bündel
auch zu AKE. Der QA-Lauf prüft AKE auf Regression (Default-Schalter, AK 1) und das neue Menü.

> [!note] Rework 2026-11-06 – überholte Annahmen (Beleg im jeweiligen Abschnitt)
> 1. ~~„Neuer `SageSdataReadClient` im Service-Projekt, der Web-Client nicht wiederverwenden"~~ – **überholt:** `ISageSdataReadClient`
>    existiert schon (`IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs:9`) und ist vom Service referenzierbar (der Service nutzt bereits
>    `IdealAkeWms.Services.SdataUrlBuilder`, `SageLagerbuchungClient.cs:4, :30`). Nur der Vertrag ist dort fest `RealTimeData` (`:32`).
> 2. ~~„Reader-Schnittstelle je Bereich neu"~~ – **überholt:** für A1, A2, A7/A8, A9, A10 existieren sie (`ISageLagerplatzReader`, `ISageBestandReader`,
>    `ISageArbeitsplatzReader`, `ISageZusatzinfoReader`, `ISageBuchungLookupReader`, DI `IDEALAKEWMSService/Program.cs:58-74`). Neu nur für A3 und A5.
> 3. ~~„Lagerplatz-Schlüssel bleibt `SageLagerplatzId`"~~ – **überholt:** der Abgleich schlüsselt über `Code` = Kurzbezeichnung (`LagerplatzSyncService.cs:85, :120`).
> 4. ~~„SData-Einstellungen appsettings-only"~~ – **überholt:** Endpunkt in ServiceSettings (`ServiceSettingDefinitions.cs:54-57`), nur Zugangsdaten in appsettings.
> 5. ~~„A8 Arbeitsgänge aus `PpsArbeitsgaengeInfo`"~~ – **widersprüchlich zum Code**, siehe G12.
> 6. ~~„Artikel-Benutzerfelder als `ArticleAttributeValue` Typ Text"~~ – **so nicht vorhanden:** `ArticleAttributeValue` hat keine Textspalte
>    (`ArticleAttributeDefinition.cs:9-12`); E3b legt sie an.
> 7. ~~„Audit-Schreiber `system:sage-sync`"~~ – **überholt:** jeder bestehende Abgleich behält seinen Schreibernamen (siehe Audit-Abschnitt).

## Ziel / Nutzen (das Warum)

Das WMS liest heute alle Sage-Daten per Direkt-SQL auf Views und Tabellen der Sage-Datenbank. Mit dem DPS-Lesevertrag
`CommonWawiServicesResources` (seit 06.11. am AKE-Test lesbar) gibt es einen offiziellen Lesepfad über SData, der

- die IDEAL-FA-Hierarchie samt Positions-IDs und Lagerplätzen liefert (Voraussetzung für BDE-Rückmeldungen),
- Artikel mit allen Benutzerfeldern, Lager, Bestände, Arbeitsplätze, PPS-Mitarbeiter und Stücklisten (mehrstufig) bereitstellt,
- ohne SQL-Zugang zur Sage-Datenbank auskommt (gleicher Weg wie die Lagerbuchungen) – **mit Ausnahme** der IDEAL-FAInfos (PPSImport, G11)
  und der FA-Zusatzinfos (G4), die auch nach der Umstellung per SQL gelesen werden.

Die Spec stellt die **bestehenden** Abgleiche auf SData um, ergänzt sie um die wichtigsten fehlenden Felder und bereitet zwei Folgethemen vor:
Kunden/Lieferanten lesen und WMS-geführte Stammdaten (Lagerstamm, BDE-Personal) nach Sage übergeben.
**Die View-/SQL-Wege bleiben vollständig erhalten** und lassen sich je Abgleich per Schalter zurückschalten (Notfall).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Gemeinsamer SData-Lese-Client (bestehender `ISageSdataReadClient`, erweitert um Paging, Fehler, Timeout, Protokoll) und ein
   **Quellenschalter je Abgleich** (`View` | `SData` | `Vergleich`).
2. Umstellung der heutigen Lese-Abgleiche (Tabelle „Abgleiche“) auf SData-Reader, jeweils **neben** dem bestehenden SQL-Reader.
3. Erweiterung um die Felder in Abschnitt „Felderweiterung“ inkl. Migrationen.
4. Neuer Lese-Abgleich PPS-Mitarbeiter (Grundlage BDE-Personal) und Stillstandsbegründungen.
5. Lagerbuchungs-Lookup über SData; Lagerumbuchung über die bestehende Buchungs-Queue.
6. **Kreditoren und Debitoren** komplett: Tabellen, Lese-Abgleich Lieferanten → Kreditoren, Kunden → Debitoren, Artikel-Lieferant, sowie der
   **gesamte Umfang der Kreditoren-Spec** (Pflege-Oberfläche, Rollen, Dienstleister-Zuordnung) – Entscheidung G9.
7. Vorbereitung der Gegenrichtung WMS → Sage für Lagerplätze und BDE-Personal: Felder, Übergabe-Status, Outbox – **ohne** Sage-Aufruf, weil
   es dafür keinen Service gibt (G5).

**Out-of-Scope**
- BDE-Rückmeldungen nach Sage (Zeit/Material/Fertigmeldung, Kommen/Gehen) → [[2026-11-02-sage-pps-fa-bde-abgleich-spec]].
- Bestellvorschläge als Dispo-Stapel nach Sage, Wareneingang/Bestellungen buchen → Folge-Spec (G7).
- Rückgabe Kreditoren/Debitoren an Sage (nur vorbereitet über F11).
- AKE-Umstellung: AKE bleibt auf den bisherigen Wegen, bis SData dort freigegeben und lizenziert ist; der Schalter macht die Umstellung später ohne Code möglich.
- Entfernen von Views, AgentJobs oder SQL-Readern.

## Abgleiche heute → künftig

Stand am Code nachgeprüft (Fundstellen im Frontmatter). Spalte „SData-Quelle“ am AKE-Test belegt ([[sage-sdata-wms-schnittstellenkatalog]]).

| # | Abgleich (Service) | Heute (View/SQL), belegt | Reader heute | SData-Quelle | Schalter neu (Default `View`) |
|---|---|---|---|---|---|
| A1 | Lagerplätze (`LagerplatzSyncService`) | `KHKLagerorte` ⟕ `KHKLagerplaetze`, `Mandant = 1`, `Aktiv = -1` (`SageLagerplatzReader.cs:23-29`) | `ISageLagerplatzReader` | `LagerplaetzeInfo` + `LagerorteInfo` | `Sync:Quelle:Lagerplaetze` |
| A2 | Lagerbestand (`LagerbestandSyncService`) | `KHKArtikel`/`KHKLagerplatzbestaende`/`KHKLagerplaetze`, `Mandant = 1` hart (`SageBestandReader.cs:24-36`) | `ISageBestandReader` | `LagerbestaendeArtikelInfo` | `Sync:Quelle:Lagerbestand` |
| A3 | Artikel (`SageImportService.SyncArticlesAsync`) | AKE `SageArticleSql.LegacySql` (`:11-53`), IDEAL View aus `Sync:ArticlesViewName` (`:61-76`) | **keiner** (inline `SageImportService.cs:384-425`) | `Artikel` + `ArtikelVariantenExtended` (+ `ArtikelInfo`, `ArtikelGruppen(Info)`) | `Sync:Quelle:Artikel` |
| A4 | FA flach (`SageImportService`, AKE) | `vw_AKE_Kommissionierung_WAListe` (`SageImportService.cs:47-59`) | keiner | `PpsFaBelegeInfo` | kein Schalter in diesem Epic (`Sync:Quelle:Fertigungsauftraege` erst mit AKE-Freigabe; Reader-Vorbereitung G13) |
| A5 | FA-Struktur IDEAL (`FaHierarchySyncService` + Materialisierung) | `Sync:FaHierarchyListeViewName`/`…InfosViewName` (`FaHierarchySyncService.cs:51-56`) | **keiner** (inline `:206`, `:265`) | `PpsFaBelegeInfo` + `PpsFaBelegePositionenInfo`; FAInfos **keine** (PPSImport, G11) | `Sync:Quelle:FaStruktur` (nur Knoten/Kanten) |
| A6 | Stückliste (`BomCacheSyncService`) | `[ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]` (`BomCacheSyncService.cs:291-296`); an IDEAL übersprungen (`:43-49`) | keiner | `…Specific/PpsRessourcenListenInfo` + `include=…ResolvedInfo,SpecificUserfields` | nur falls G13 E5 im Epic lässt: `Sync:Quelle:Stueckliste` |
| A7 | Arbeitsplätze (`ProductionWorkplaceSyncService`) | `Sync:ProductionWorkplaceSyncViewName` = `KHKPpsArbeitsplaetze` oder View (`SageArbeitsplatzSql.cs:66-94`) | `ISageArbeitsplatzReader` | `PpsArbeitsplaetzeInfo` | `Sync:Quelle:Arbeitsplaetze` |
| A8 | Arbeitsgang-Kürzel (`OperationConfigSyncService`) | **derselbe Arbeitsplatz-Reader**, Kürzel = Arbeitsschritt (`OperationConfigSyncService.cs:112, :272`) | `ISageArbeitsplatzReader` | `PpsArbeitsplaetzeInfo` (USER_ArbeitsSchritt, P-A1) – **nicht** `PpsArbeitsgaengeInfo` (G12) | folgt `Sync:Quelle:Arbeitsplaetze` (kein eigener Schalter) |
| A9 | FA-Zusatzinfos (`FaZusatzinfoSyncService`) | View über `ISageZusatzinfoReader` (`FaZusatzinfoSyncService.cs:32`) | `ISageZusatzinfoReader` | **keine** (FA-USER-Felder nicht lesbar) | bleibt View (G4) |
| A10 | Lagerbuchungs-Lookup (`SageBuchungLookupReader`) | `SELECT TOP 1 1 FROM KHKLagerplatzbuchungen WHERE Memo LIKE @pattern` (`SageBuchungLookupReader.cs:14-15`) | `ISageBuchungLookupReader` | `LagerplatzbuchungenInfo where Memo like …` | `Sync:Quelle:LagerbuchungLookup` |
| A11 | **neu** PPS-Mitarbeiter | – | – | `PpsMitarbeiterInfo` (0 Datensätze am AKE-Test) | `Sync:PpsMitarbeiterEnabled` (SData-only, Default `false`) |
| A12 | **neu** Stillstandsbegründungen | – | – | `PpsStillstandsbegruendungenInfo` (0 am AKE-Test) | `Sync:StillstandsgruendeEnabled` (SData-only, Default `false`) |
| A13 | **neu** Lieferanten → Kreditoren, Kunden → Debitoren, Artikel-Lieferant | – | – | `Kontokorrent` (+ `Adressen`, `Ansprechpartner`), `ArtikelLieferantInfo` | `Sync:KreditorenEnabled`, `Sync:DebitorenEnabled`, `Sync:ArtikelLieferantEnabled` (SData-only, Default `false`) |

## Entscheidungs-Nachtrag 2026-11-06 (gilt vor den Abschnitten unten)

Gerald hat G10–G17 am 2026-11-06 bestätigt (Antworten im Freigabe-Block). Wo der Text unten abweicht, gilt dieser Nachtrag:

- **G10:** Lese-Fehler bei Quelle `SData`/`Vergleich` lösen in A1 und A2 eine Fehlermail aus (`ISyncErrorNotifier`); bei Quelle `View` bleibt das Verhalten wie heute.
- **G11:** E4 nutzt SData nur für **Kanten und Positionen** (`PpsFaBelegeInfo`, `PpsFaBelegePositionenInfo`); `FAInfos` (Quelle PPSImport) und die Knotenfelder
  bleiben auf SQL. `Sync:Quelle:FaStruktur = SData` erst nach P-F2; Vergleichsmodus ab E4a.
- **G12:** A8 folgt dem A7-Reader (`PpsArbeitsplaetzeInfo`); Rüst-/Stückzeit und Standard-Arbeitsplatz aus F8 entfallen hier (→ Rückmelde-Epic).
  `USER_ArbeitsSchritt` ist am AKE-Test nicht vorhanden (P-A1) – Prüfung am IDEAL-Endpunkt.
- **G13:** **E5 (Stückliste) und der AKE-Reader FA flach (A4) sind aus dem Epic genommen** und bis zur AKE-Freigabe zurückgestellt.
- **G14:** BDE-Ausweise gibt es noch nicht. `BdeOperator` bekommt `SageMitarbeiternummer` (= Sage `Nummer`, string(20)); Zuordnung nur manuell,
  Vorschlag nur bei exakter Gleichheit `PersonnelNumber = Nummer`; Kartennummer/ExterneNummer werden mitgelesen, nicht abgeglichen.
- **G15:** Dieses Epic startet zuerst, erhält SQL/98 ff. und legt `SageFaStates` in E4b in der Form an, die [[2026-11-02-sage-pps-fa-bde-abgleich-spec]] plant;
  das Rückmelde-Epic nutzt die Tabelle und nummeriert neu.
- **G16:** A3 bleibt auf View, bis P-R1 den IDEAL-Filter zeigt; E3a baut Reader + Vergleichsmodus.
- **G17:** SQL-Nummern dreistellig weiter (100 ff.), Reihenfolge über die Deploy-Liste; Nachtrag zu ADR 0004.
- **P-K1:** Kontoart = `KtoArt` (`K` Kreditor, `D` Debitor) – E8c ist nicht mehr blockiert.

## Fachliche Anforderungen

### F1 Quellenschalter und Notfall-Rückfall
- Jeder umgestellte Abgleich hat einen ServiceSetting-Schalter `Sync:Quelle:<Bereich>` (Typ `String`, `ServiceSettingDefinition.cs:4-9` kennt
  keinen Auswahltyp) mit den Werten `View` (Default), `SData`, `Vergleich`. Ein unbekannter Wert wirkt wie `View` und erzeugt eine Warnung im
  Protokoll (fail-safe, kein stiller Wechsel nach SData).
- Beide Reader liefern **dasselbe interne Modell** (bestehende DTOs `SageLagerplatzDto`, `SageBestandDto`, `SageArbeitsplatzDto`; neue DTOs nur für A3/A5);
  der Abgleich selbst (Upsert, Reconciliation, Protokoll) bleibt **einmal** vorhanden. Umschalten ändert nur die Quelle, nie die Abgleichslogik.
- Rückfall: Schalter auf `View` → nächster Lauf liest wieder per SQL. Kein Datenverlust, keine Migration nötig. Neue Felder, die nur SData
  liefert, bleiben beim View-Lauf **unverändert** (nicht genullt): DTO-Zusatzfelder sind nullable, `null` heißt „Quelle liefert das Feld nicht“ und
  wird nie geschrieben; die Artikel-Zusatzfelder liegen strukturell getrennt im Satelliten `ArticleSageDetails`, den nur der SData-Weg schreibt (F5).
- **Vergleichsmodus** `Sync:Quelle:<Bereich> = Vergleich`: liest beide Quellen, schreibt **nur** nach View, protokolliert Abweichungen je
  Schlüssel (Anzahl + erste 20) in einem eigenen Protokolllauf `SageQuellenvergleich` (Referenz = Bereich). Zweck: Abnahme der Umstellung ohne Risiko.
- Fällt SData aus (Timeout, 5xx, Anmeldung, unlesbare Antwort): Lauf bricht für diesen Bereich ab, **kein** automatischer Rückfall auf View
  (bewusste Entscheidung durch Umschalten). Fehlermail: wie heute über `RunResilientAsync` (`SyncWorker.cs:512-520`) bei A3/A5/A7/A8; für A1/A2
  gibt es heute bei Lese-Fehlern **keine** Mail (`LagerplatzSyncService.cs:46-61`, `LagerbestandSyncService.cs:54-67`) – Verhalten dort laut G10.

### F2 SData-Lese-Client
- **Wiederverwenden statt neu bauen:** `ISageSdataReadClient`/`SageSdataReadClient` (`IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs:9-119`)
  bekommt eine zweite Methode für Lesen mit Paging, z. B. `QueryAllAsync<T>(SdataLeseZiel ziel, string vertrag, string ressource, string? where,
  string select, string orderBy, CancellationToken ct)`. Die bestehende `GetAsync` (RealTimeData, FA-Ansicht v1.71.0) bleibt unverändert.
  `SdataFeed<T>` (`SageSdataFaModels.cs:25-29`) bekommt `$totalResults`/`$itemsPerPage`. Vertrag als Konstante `CommonWawiServicesResources`
  bzw. `CommonWawiServicesResourcesSpecific` (kein neuer Setting-Key: der Wert ändert sich nicht). URL über `SdataUrlBuilder.BuildResourceUrl`
  (`SdataUrlBuilder.cs:26-39`).
- Endpunkt aus den bestehenden ServiceSettings `SData:BaseUrl`, `SData:Application`, `SData:Dataset` (`ServiceSettingDefinitions.cs:54-57`,
  DB-first); Zugangsdaten aus appsettings `SageLagerbuchung:Username`/`Password` (wie `SageBookingWorker.cs:104-105`). Keine neuen Zugangsdaten.
- Im Service eigener typisierter `HttpClient` für `ISageSdataReadClient` mit demselben TLS-Callback-Muster wie `Program.cs:102-122`
  (Schalter `SageLagerbuchungSslZertifikatPruefen`, Beschreibung `ServiceSettingDefinitions.cs:63` nennt schon „Lagerbuchung + FA-Ansicht“ –
  um „Stammdaten-Abgleich“ ergänzen).
- Jede Abfrage: `format=json`, `Accept: application/json` (Pflicht laut [[fallstricke]] „Sage-SData liefert ohne Accept-Header Atom-XML“),
  **`orderBy` immer gesetzt** (sonst HTTP 500, [[sage-sdata-commonwawiservicesresources]]), `count` (Seitengröße, Default 500), `startIndex`,
  `select` nur benötigte Felder, `where` für Filter; Paging bis `$totalResults`.
- Timeout je Anfrage 60 s, höchstens 2 Wiederholungen bei Timeout/503; 500 mit `$diagnoses` → Fehler, keine Wiederholung.
- Für Abgleiche gilt **werfen statt leer liefern**: Ist ein Aufruf nicht `SdataCall.Erfolgreich` (`SageSdataFaModels.cs:37`), wirft der Reader
  eine Ausnahme mit Status, Ressource und `$diagnoses[0].message` (enthält das erzeugte SQL). Grund: `GetAsync` liefert bei Fehlern leere Zeilen
  (`SageSdataReadClient.cs:81-86, :103-112`) – im Abgleich hieße das „alles verschwunden“ (Deaktivieren/Nullsetzen) bzw. beim Lookup
  „nicht gebucht“ (Doppelbuchung).
- Protokoll je Lauf: Quelle, Ressource, Anzahl Seiten/Zeilen, Dauer.
- Sperren: `VerkaufPositionenAllInfo` und `EinkaufPositionenAllInfo` nie ohne `where` (Liste im Code); jede Abfrage ohne `where` bricht nach der
  ersten Seite ab, wenn `$totalResults` > 100.000 (Konstante).
- Sage-Booleans `-1/0` zentral zu `bool` (`SageBool.IsTrue`, `SageSdataFaModels.cs:56-60`, wiederverwenden); Dezimalwerte robust (Zahl oder String).

### F3 Lagerorte und Lagerplätze (A1)
- Quelle `LagerplaetzeInfo` (3.300 am AKE-Test) + `LagerorteInfo` (36). SData-Reader implementiert `ISageLagerplatzReader`
  (`ISageLagerplatzReader.cs:8`) und bildet die SQL-Semantik nach: nur Plätze **aktiver** Lagerorte (`SageLagerplatzReader.cs:28`) und
  `Mandant eq 1` wie `:27`. Ob das Dataset (`ake_TEST2026;1`) den Mandanten schon festlegt, ist **nicht belegt** (P-S2) – bis dahin bleibt der
  Filter wie heute, nicht „aus dem Dataset abgeleitet“.
- Schlüssel bleibt **`Code` = Kurzbezeichnung** (getrimmt, Groß/Klein egal; `LagerplatzSyncService.cs:85, :108, :120`); `SageLagerplatzId` (PlatzID)
  wird wie heute mitgeschrieben (`:135, :162`). Bestehende Regeln bleiben: `IsActive` Sage-geführt (`:163, :190`), `IstBuchbar` und
  `SageBuchungErlaubt` WMS-geführt (Neuanlage `IstBuchbar = false` `:130`; nie überschrieben).
- Neue Felder siehe „Felderweiterung“ (nullable, nur bei Quelle SData geschrieben). Neue Entität **`StorageArea`** (Lagerort: Lagerkennung,
  Bezeichnung, Lagertechnik, IstDispoLager, IstSperrlager, Aktiv) als Elternteil; Verknüpfung `StorageLocation.StorageAreaId` (nullable FK),
  vom Abgleich über die Lagerkennung gesetzt. `StorageLocation.Zone` bleibt wie heute die Lagerkennung bei Sage-Plätzen (`:109, :158`), bei
  manuellen Plätzen frei editierbar – deshalb ein eigener FK statt Zone-Lookup. Lagerorte werden nie gelöscht; fehlt einer in Sage: `SageMissingSince`.
- Anzeige: Lagerort-Spalte + Sperr-Kennzeichen in der Lagerplatzliste (`StorageLocationsController`, Listen-View-Pattern bleibt; neue Spalten
  bekommen `data-col-key` und Eintrag in `ColumnMap`).

### F4 Lagerbestände (A2)
- Quelle `LagerbestaendeArtikelInfo` (Artikel × Platz, 4.544 am AKE-Test). SData-Reader implementiert `ISageBestandReader`
  (`ISageBestandReader.cs:6`) und liefert dieselben Tupel wie `SageBestandReader.cs:24-36`: Artikelnummer, **Kurzbezeichnung** des Platzes,
  Summe Bestand je (Artikel, Platz). Mandant wie heute `1` (`:33`), Ableitung aus dem Dataset erst nach P-S2.
- Bestehende Korrektur-Buchungs-Logik inkl. Dubletten-Skip (`LagerbestandSyncService.cs:74-93`) und Nullsetzen-Cap (`:205-229`) bleibt unverändert.
  Keine Rundung in dieser Etappe; `DezimalstellenLager` wird erst in E3a gespeichert.
- Chargen/Seriennummern: Ressourcen `LagerbestaendeArtikelChargenInfo`/`…SeriennummernInfo` werden **nur gelesen und protokolliert** (Anzahl),
  nicht gespeichert (WMS führt keine Chargen; Vorbereitung). Keine Migration in E2.

### F5 Artikel (A3)
- Quelle `Artikel` (Stamm, alle USER_-Felder) + `ArtikelVariantenExtended` (Bestand/Dispo) – Abgleich je Artikelnummer.
- Neues Reader-Interface `ISageArtikelReader` (heute liest `SageImportService.cs:384-425` inline): SQL-Implementierung = der bestehende Code
  unverändert hinter dem Interface (`SageArticleSql.Select`), SData-Implementierung neu. Der Upsert (`:461-497`) und die Count-Keys (`:553-559`)
  bleiben einmal vorhanden.
- Umfang wie heute; **kein** Vollimport der 279.532 Artikel. Am Code belegt ist nur der **AKE**-Filter (`SageArticleSql.cs:11-53`): alle
  Ressourcenummern aus `KHKPpsRessourcenPositionen` ∪ alle Artikel mit `IstBestellartikel = -1 AND Aktiv = -1`. Der **IDEAL**-Filter steckt im
  Viewtext von `Sync:ArticlesViewName` (nicht im Repo, P-R1). Welche Menge der SData-Reader liest, entscheidet G16; technisch geht beides:
  `Artikel where IstBestellartikel eq -1 and Aktiv eq -1` direkt, die Ressourcenummern als Liste in Blöcken à 200 per `Artikelnummer in (…)`.
  Bis G16 entschieden ist, läuft A3 an IDEAL nur im Modus `Vergleich`.
- **Gruppen-Format:** `Article.ArticleGroup` hält heute den reinen Code (`"940"`), BOM/Struktur `"CODE - Bezeichnung"` ([[fallstricke]]
  „Artikelgruppe BOM vs Articles“). Der SData-Reader liefert für `Article.ArticleGroup` den reinen Code (`Artikel.Artikelgruppe`) – Vergleichsmodus
  prüft das gegen die View.
- **Zusätzliche Felder (Entscheidung G3, alle vorsehen):**
  - Lager: `Lagerfuehrung`, `PlatzID` (Hauptlagerplatz als ID), `Entnahmeverfahren`, `Inventurmethode`, `USER_LagerplatzID`, `USER_Mindestbestand`
  - Fertigung/BDE: `IstFertigungsartikel`, `IstUnterbaugruppe`, `Stuecklistentyp`, `Schwundart`, `Schwund`, `MinimaleLosgroesse`, `MaximaleLosgroesse`, `Zeichnungsnummer`
  - Kennzeichen: `IstBestellartikel`, `IstErsatzteil`, `IstVerschleissteil`, `Aktiv`, `Ersatzartikelnummer`
  - Dispo (`ArtikelVariantenExtended`): `ABCKlasse`, `LetzterLieferant`, `LagerbestandOhneDispo`
  - Maße/Gewicht: `Laenge`, `Breite`, `Hoehe`, `Gewicht`, `Gewichtseinheit`
  - ~~Als typisierte Spalten an `Article` (bzw. Satellit, spec-agent entscheidet …)~~ **Entschieden (Rework):** alle Zusatzfelder in den
    1:1-Satelliten **`ArticleSageDetails`** (Begründung und Typen im Abschnitt „Felderweiterung“). Felder, die `Article` heute schon hat
    (Bezeichnung → `Description`, Matchcode, Artikelgruppe → `ArticleGroup`, Lager-ME → `Unit`, Meldebestand → `ReorderLevel`, Hauptlagerplatz →
    `SagePrimaryStorageLocation`/`PrimaryStorageLocationId`), bleiben in `Articles` und werden von beiden Wegen geschrieben.
- **Benutzerfelder (G3), E3b:** Default-Liste `Sync:ArtikelBenutzerfelder` = `USER_Maschine, USER_Ausfuehrung, USER_VaultID, USER_MasterartikelNr,
  USER_TypenschildArtikel, USER_Artikeltyp, USER_Produktfamilie, USER_IstIDEAL, USER_DruckFertigungsauftrag, USER_AUSCAD, USER_DPSBestand,
  USER_Kaeltemittel, USER_Ventil, USER_PfadGlaszeichnung, USER_PfadSchaltplan` (Typ `String`, kommagetrennt, erweiterbar ohne Code). Werte als
  Artikel-Merkmal: **neu** `ArticleAttributeValues.TextValue nvarchar(1000)` (heute gibt es nur `BooleanValue`/`SelectedOptionId`,
  `ArticleAttributeValue.cs:11-14`; Länge wie `FaAttributeValues.TextValue`, `ApplicationDbContext.cs:658`). Die Definition legt der Abgleich bei
  Bedarf an: `AttributeType.Text` (`ArticleAttributeDefinition.cs:12`), `SyncSource = "Sage"`, `SyncFieldName = <USER_-Feld>` – beide Spalten
  existieren schon (`:31-35`) und werden bisher von keinem Code geschrieben (Repo-Suche `SyncSource|SyncFieldName`: nur Modell, DbContext :273-274,
  Migration, SQL/39, FreshInstall). Definitionen mit `SyncSource = Sage` und ihre Werte sind in der Merkmal-Pflege **nicht editierbar** (Hinweis
  „aus Sage“); die Artikel-Detailansicht zeigt Text-Merkmale an. Werte > 1000 Zeichen: auf 1000 kürzen + Warnung im Protokoll. Ein Name, den es
  schon als manuelles Merkmal gibt (`IX` unique auf `Name`, `ApplicationDbContext.cs:280`), wird nicht überschrieben, sondern als Konflikt gemeldet.
- Vollständige Feldliste: [[sage-sdata-artikel-felder]].

### F6 Fertigungsaufträge und Struktur (A5, IDEAL)
- Quelle `PpsFaBelegeInfo` (Kopf) + `PpsFaBelegePositionenInfo` (Positionen) je `StrukturID`.
- **Was die View heute liefert (belegt, `SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql:21-53`):** eine Zeile je MA-Position
  (`RessourceTyp = 'MA'`) jeder FA der Struktur **plus** eine Wurzelzeile. `HauptFA` = `StrukturID`; `VaterFA` = `BelID` der Position;
  `Position` = Positionsnummer; `SubFA` = `BelIDNachfolger`, **0 = Blatt** (Kaufteil ohne eigenen FA). Wurzel: `VaterFA` und `Position` NULL,
  `SubFA = HauptFA`. Der Knoten-Select nimmt nur Strukturen, zu denen FAInfos eine Zeile hat (`FaHierarchySql.cs:149-151`).
- ~~Hierarchie: Kante = Position mit `RessourceTyp = 'MA'` und `BelIDNachfolger > 0`~~ – **überholt:** Blätter (`BelIDNachfolger = 0`) sind
  ebenfalls Knoten; die SData-Projektion bildet **alle** MA-Positionen + Wurzel nach der Regel oben ab.
- **Grenze der Umstellung (G11):** FAInfos (`FaHierarchyOrderInfo`) kommt aus `[PPSImport].[dbo].[vw_PPSData]` (`FAInfos.sql:9`) – dafür
  gibt es keine SData-Quelle; sie bleibt SQL. Von den Knotenfeldern sind über SData belegbar: `HauptFA`, `VaterFA`, `SubFA`, `Position`,
  `HauptArtnr`, `Artnr`, `Matchcode`, `Bezeichnung1/2`, `Sollmenge`, `Fertigungmenge` (Positionsmenge, `FAListe.sql:32-33`) sowie über den Artikel
  `Beschaffungsartikel` (IstBestellartikel → „Ja“/„Nein“), `Artikelgruppe` („CODE - Bezeichnung“, `:35`), `Hauptlagerplatz`. **Nicht belegt** sind
  die 13 Felder „Position bevorzugt, Beleg als Fallback“ (`:37-48`: BemerkungPN, FertigungsInfoPN, Kommissionieren, Arbeitsbereich,
  Arbeitsschritte, Artikeltyp, Beschichtet, Material, Breite/Hoehe/Tiefe, EKBedarf, VMBedarf) – u. a. Grundlage der Struktur-Arbeitsgang-Erkennung
  (`ServiceSettingDefinitions.cs:43`). Bis P-F2 sie belegt, darf `Sync:Quelle:FaStruktur` nur `View` oder `Vergleich` sein; der Wert `SData`
  wird bis dahin mit Warnung wie `View` behandelt.
- **E4a (ohne Migration):** neues `ISageFaStrukturReader` (SQL-Implementierung = bestehender Code `FaHierarchySyncService.cs:206-263` hinter dem
  Interface; SData-Implementierung neu), Vergleichsmodus für Kanten (`HauptFA`, `VaterFA`, `SubFA`, `Position`) und die belegbaren Felder.
- **E4b – Lesefilter (neu, R7 aus [[2026-11-02-sage-pps-abgleich]]):** `IstEingelastet = -1 and Aktiv = -1` und (`IstBeendet = 0` oder
  `Istendtermin` in den letzten 30 Tagen) – wirkt nur auf die SData-Kopfmenge. Welche Menge die View heute filtert, ist nicht belegt (Viewtext
  fehlt, `FAListe.sql:57`) → P-F1; der Vergleichsmodus zeigt die Differenz.
- **E4b – Positionen:** je FA der gefilterten Menge die Positionen mit `BelPosID`, `RessourceTyp`, `RessourceNummer`, `ArbeitsgangNummer`,
  `BelIDNachfolger`, Mengen/Zeiten, `PlatzID` + Lagerplatz-Kurzbezeichnung in der neuen Cache-Tabelle **`ProductionOrderPositions`**
  (Full-Refresh je Lauf in einer Transaktion wie `FaHierarchySyncService.cs:138-147`) – Grundlage der Rückmeldungen. Wird **immer** aus SData
  gelesen, sobald `Sync:Quelle:FaStruktur` ≠ `View` ist (auch im Vergleichsmodus; es gibt keine View-Quelle dafür).
- **Status-Kennzeichen je FA** (`IstEingelastet` … `Fertigungsart`): nicht hier, sondern in `SageFaStates` des Rückmelde-Epics (G15).
- Verschwundener Unter-FA: Verhalten wie heute (`ProductionOrder.SageMissingSince`, `ProductionOrder.cs:54`, gesetzt von
  `FaMaterializationSyncService`). Ein inaktiver Unter-FA (`Aktiv = 0`, z. B. Fertigungsart Einkauf) fehlt in der gefilterten SData-Menge und wird
  dadurch wie verschwunden behandelt – keine eigene Logik in der Materialisierung.
- Delta: Vollabgleich der gefilterten Menge je Lauf (G8); `GeaendertAm` nur als Info.
- AKE (A4, flach): ursprünglich „Reader vorbereiten, erst mit Freigabe AKE aktivieren“. Ein Reader ohne Aufrufer ist toter Code und am
  AKE-Test nicht abnehmbar → Entscheidung in G13 (Empfehlung: zurückstellen). Bleibt er drin, gehört er in E4a als `ISageFaFlachReader` ohne
  Schalter und ohne Aufruf aus dem `SyncWorker`.

### F7 Stückliste (A6)
- Quelle `PpsRessourcenListenInfo` (Kopf, `where Mandant eq 1 and Artikelnummer eq '…'`) mit `include` der aufgelösten Positionen
  (Ebene, RowGuid/RowGuidParent, BelPosID/BelPosIdParent, ArtikelnummerUBG) und der Benutzerfelder (Kopf: z. B. USER_VKZuschlag; Position:
  USER_VKPreis, USER_VKEinzelpreis, USER_EPDMVERSION, USER_AUSCAD, USER_Positionsnummer).
- Abbildung auf `CachedBomHeader`/`CachedBomItem` wie heute; die heutige hierarchische `Position` (`15.1.1`) wird aus Ebene + Position der
  Eltern **abgeleitet**. Ergebnis muss mit der View übereinstimmen (Vergleichsmodus).
- `IstBestellartikel` (Beschaffungsartikel) und Artikelgruppe kommen aus dem Artikel (F5), nicht aus der Stückliste.
- **Geltung (G13):** `BomCacheSyncService` läuft an IDEAL nicht (`BomCacheSyncService.cs:43-49`, ADR [[0013-bom-bridge-repository-schnittstelle-statt-cache-kopie]]);
  die Stückliste kommt dort aus `FaHierarchyNode`. F7 wirkt nur an AKE und ist nur umzusetzen, wenn G13 E5 im Epic lässt. Die View liest fest aus
  `[ake].[dbo]` (`:294`). `CachedBomItem` ist eine reine Cache-Tabelle ohne Audit (`CachedBomItem.cs:5-8`).

### F8 BDE-Stammdaten (A7, A8, A11, A12)
- **Arbeitsplätze (E6a):** SData-Reader implementiert `ISageArbeitsplatzReader` (`ISageArbeitsplatzReader.cs:12`, Ergebnis
  `SageArbeitsplatzReadResult` `:10`) aus `PpsArbeitsplaetzeInfo`: `Arbeitsplatznummer` (als Zeichenkette, nie `int.Parse`,
  `SageArbeitsplatzReader.cs:21-22`), `Bezeichnung1`, Arbeitsschritt (`USER_ArbeitsSchritt` wie `SageArbeitsplatzSql.cs:72`, über SData nur falls
  P-A1 es belegt), Filter `Aktiv eq -1` wie `:75` – deshalb **keine** `Aktiv`-Spalte (inaktive kommen wie heute nicht in die Menge, die Kennzeichnung
  übernimmt `ProductionWorkplace.SageMissingSince`, `ProductionWorkplace.cs:44-48`). Neu gespeichert: `SageBdeTerminalId` (nvarchar(20)),
  `SageKostenstelle`, `SageSchichtplannummer` (Typen P-S1). Die View-Variante (`vw_IDEAL_AKE_WMS_Arbeitsgaenge`, `:90-93`) liefert diese Felder
  nicht → bleiben beim View-Lauf unverändert.
- **Arbeitsgang-Kürzel (A8):** ~~`PpsArbeitsgaengeInfo` → `OperationConfig`; neu Rüst-/Stückzeit, Standard-Arbeitsplatz~~ – **widerspricht dem Code**:
  `OperationConfigSyncService` liest denselben `ISageArbeitsplatzReader` (`:47, :112`) und legt Arbeitsschritt-Kürzel an (`:272`); Sage-Arbeitsgänge
  sind ein anderes Objekt. Bis G12 entschieden ist, folgt A8 dem Arbeitsplatz-Reader (gleicher Schalter, keine neuen Spalten an `OperationConfig`).
- **PPS-Mitarbeiter (neu, E6b):** `PpsMitarbeiterInfo` (Nummer, Matchcode, Vorname, Nachname, Gruppe, Kartennummer, Aktiv, Kostenstelle,
  Schichtplannummer, UserName) → `BdeOperator` mit neuem Schlüssel `SageMitarbeiternummer` (eindeutiger gefilterter Index `IS NOT NULL`, Muster
  `ApplicationDbContext.cs:211-215`). ~~Zuordnung über `Kartennummer`~~ – **so nicht möglich:** `BdeOperator` hat keine Kartennummer
  (`BdeOperator.cs:5-31`), das Terminal meldet über `PersonnelNumber` an (`BdeApiController.cs:52-58`). Zuordnungsregel laut G14; bis dahin
  Liste „nicht zugeordnet“ (Sage-Mitarbeiter aus der Cache-Tabelle `SagePpsMitarbeiter` ohne verknüpften `BdeOperator`) zur manuellen
  Zuordnung – **nie** automatisch über den Namen, **nie** automatische Neuanlage. Ohne Sage-Mitarbeiter
  keine BDE-Rückmeldung (Befund §11/§14): der Abgleich meldet aktive `BdeOperator` ohne `SageMitarbeiternummer` als Warnung (Anzahl + erste 20).
  Liste und Zuordnung in der BDE-Personalpflege (`BdeMasterDataController`, Filter `[RequireBdeActive]` + `[RequireBdeShiftleadAccess]`,
  `BdeMasterDataController.cs:10-12`) – keine neue Rolle.
- **Stillstandsbegründungen (neu, E6b):** `PpsStillstandsbegruendungenInfo` → neue Stammtabelle `Stillstandsgruende` (Nummer, Bezeichnung, Aktiv,
  `SageMissingSince`); nur lesen, **keine** Oberfläche (Anzahl im Protokoll); Vorbereitung Stillstands-Rückmeldung. Liste erst mit dem Rückmelde-Epic.

### F9 Lagerbuchungen (A10) und Umbuchung
- Lookup vor Wiederholung einer Buchung über `LagerplatzbuchungenInfo where Memo like 'IdealAkeWms Lagerbuchung SM#<id>#%'`
  (am AKE-Test belegt, W4). SData-Reader implementiert `ISageBuchungLookupReader.ExistsAsync(int stockMovementId)` (`ISageBuchungLookupReader.cs:8`),
  Muster aus `SageBookingCorrelation.LikePattern` (wie `SageBuchungLookupReader.cs:29`), `count=1`, `orderBy=BuchungsID`.
  **Fehler werfen, nie `false` liefern:** der Worker sendet bei geworfenem Lookup bewusst nicht neu (`SageBookingWorker.cs:137-140, :187-190`);
  ein leeres Ergebnis wegen eines HTTP-Fehlers würde eine Doppelbuchung auslösen.
- ~~Die Buchungs-Queue kennt heute nur Zugang/Entnahme (laut … R19)~~ – **am Code bestätigt, an drei Stellen:** `SageBookingEnqueueDecision.cs:14-15`
  (Umbuchung ausdrücklich ausgeschlossen, Kommentar `:8-9`), `SageLagerbuchungPayloadBuilder.cs:15-17, :32-42` (nur Zugang/Entnahme) und der
  eigene Typfilter des Nacherfassungs-Laufs `SageBookingQueueRepository.cs:142`. Neu: eine WMS-Umbuchung (`MovementType.Umbuchung`,
  `MovementType.cs:7`; Herkunft `StockMovement.SourceStorageLocationId` `:38`, Ziel `StorageLocationId`) wird
  - zwischen zwei Sage-Plätzen (beide `SageBuchungErlaubt` und mit `SageLagerkennung`/`SageLagerplatzId`) als `Lagerbewegungsart = "Umbuchung"`
    mit Herkunft + Ziel gesendet (am AKE-Test getestet, Befund §14: Bewegungsart IU, Memo gespeichert);
  - zwischen Sage-Platz und WMS-only-Platz als `Entnahme` (Herkunft Sage) bzw. `Zugang` (Ziel Sage) gesendet;
  - zwischen zwei WMS-only-Plätzen nicht gesendet.
  Alle drei Stellen werden **gemeinsam** geändert; die Entscheidung bleibt in der einen reinen Funktion `SageBookingEnqueueDecision`
  (zusätzlicher Parameter: Flag des zweiten Platzes) mit **einer** öffentlichen Typliste, die auch die EF-Abfrage in
  `SageBookingQueueRepository.cs:142` per `Contains` nutzt (übersetzbar) – keine zweite, abweichende Liste mehr. Sage-Korrekturbuchungen
  (`SageEinbuchung`/`SageAusbuchung`) bleiben ausgeschlossen (Feedback-Loop-Schutz, `:8-9`).
  Der Lagerbestand-Abgleich bleibt unverändert: er gleicht nach dem Buchen ohnehin auf Sage ab.
- **Nicht** in dieser Spec: Entnahme mit FA-Bezug (→ Material-Rückmeldung im Rückmelde-Epic). Keine Migration (die Queue verweist auf die
  `StockMovement`, `SageBookingQueueItem.cs:15`).

### F10 Kreditoren und Debitoren (A13) – Tabellen + Lese-Abgleich
- **Neue Tabellen `Creditors` (Kreditoren) und `Debitors` (Debitoren)**, beide `AuditableEntity`, gleicher Aufbau. Code-Namen englisch wie in der
  Kreditoren-Spec (UI deutsch); Sage-Felder mit Präfix `Sage` wie `ProductionWorkplace.SageArbeitsplatznummer`:
  - Stamm (= Feldliste der [[2026-10-06-kreditoren-stammdaten-spec]], dort Antwort 1): `Name` nvarchar(200), `ShortName` (Kurzname) nvarchar(50),
    `Matchcode` nvarchar(50), `Street` nvarchar(200), `PostalCode` nvarchar(20), `City` nvarchar(100), `Country` nvarchar(100), `Email` nvarchar(200),
    `Phone` nvarchar(50), `ContactPerson` nvarchar(200), `Remark` nvarchar(1000), `IsActive` bit Default 1.
    **Zusätzlich aus Antwort 1 der Kreditoren-Spec (Gerald 2026-10-06):** `Matchcode` und Kennzeichen **`IsCoater`** („Beschichter ja/nein“, bit
    Default 0, Filter in der Liste) – nur am Kreditor. Pflichtfelder sind laut dieser Antwort noch offen (Kreditoren-Frage 1 bleibt offen; Vorschlag
    dort: Name + Kurzname).
  - Sage-Bezug: `SageKto` nvarchar(20) (Kontonummer, Länge aus `eptKunden`/`eptLieferanten` `Kto(20)` belegt, [[sage-sdata-api-vertraege]];
    eindeutiger gefilterter Index `IS NOT NULL`, nie numerisch geparst), `SageMatchcode` nvarchar(50), `SageGruppe` nvarchar(10), `SageEUUStID`
    nvarchar(15), `SageZahlungskondition` nvarchar(10) (alle Längen aus denselben Verträgen), `SageMissingSince` datetime2 NULL.
    Hinweis: Die Längen stammen aus `apiKunden`/`apiLieferanten` auf derselben Sage-Tabelle `KHKKontokorrent`; für `Kontokorrent` im Lesevertrag
    sind sie mit P-S1 zu bestätigen.
  - Kreditor zusätzlich: `SageEKMindestbestellwert` decimal(18,2); Debitor zusätzlich: `SageVertreter` (Länge P-S1), `SageKundenNameFA`
    nvarchar(20) (`KHKAdressen_USER_KundenNameFA(20)`, [[sage-sdata-api-vertraege]]).
  - `Source` (UI „Herkunft“, Werte `Sage` | `Manual`, UI „Sage“/„WMS“, nvarchar(20) – gleiches Muster wie `StorageLocation.Source`,
    `StorageLocation.cs:36-38`, und `OperationConfig.Source`, `OperationConfig.cs:41-42`) und `SageUebergabeStatus` (wie F11) – WMS-eigene
    Kreditoren (Dienstleister/Beschichter ohne Sage-Nummer) bleiben möglich.
- Quelle `Kontokorrent` (2.821 am AKE-Test, 11 USER-Felder) mit Adresse aus `Adressen`/`Ansprechpartner`.
- **Kontoart Lieferant/Kunde – Befund aus den Schema-Auszügen (Rework 2026-11-06):** In [[sage-sdata-commonwawiservicesresources]] und
  [[sage-sdata-wms-schnittstellenkatalog]] ist für `Kontokorrent` **kein** Feld aufgeführt (nur Name, Anzahl, „11 USER“). Felder `KtoArt` und
  `KtoTyp*` nennt nur `recLieferantenstamm.Sage.System` im **MasterData**-Vertrag ([[sage-sdata-masterdata]] Z. 170) – ohne Werteliste, anderer
  Vertrag, laut DPS nicht freigegeben. Getrennte Listen liefern die Verträge `apiLieferanten.eptLieferanten` / `apiKunden.eptKunden`
  ([[sage-sdata-api-vertraege]] Z. 72, 83; „Lesen belegt“ mit 200 Zeilen laut Katalog Z. 43) – deren Freigabe/Lizenz ist von G1 nicht erfasst.
  **Damit ist die Kontoart am Lesevertrag nicht belegt** → Prüfabfrage **P-K1**; E8c beginnt erst mit ihrem Ergebnis. Keine Ableitung aus
  Kontonummern-Präfixen (z. B. `K31013`).
- Abgleich Sage → WMS (E8c) nur für `Source = Sage` bzw. bei Treffer über `SageKto`; Sage-Felder (`Sage*`, `Name`, Adresse aus Sage) werden
  überschrieben, WMS-Felder (`ShortName`, `Remark`, `IsCoater`, `IsActive`-Override) nie. Verschwundene Konten: `SageMissingSince`, kein Löschen.
  WMS-eigene Datensätze mit gleichem Namen werden **nicht** automatisch verknüpft, sondern als „mögliche Dublette“ gelistet. Neue Sage-Konten
  werden mit `Source = Sage` angelegt; `ShortName` ist dann leer, bis jemand ihn pflegt (deshalb darf `ShortName` bei `Source = Sage` leer sein –
  der Kurzname-Pflichtvorschlag der Kreditoren-Spec gilt nur für `Manual`).
- `ArticleSuppliers` (Artikel-Lieferant, E8c): Artikelnummer nvarchar(31) + FK `ArticleId` (nur Artikel, die im WMS existieren), FK `CreditorId`
  (über `SageKto`, `Lieferant(20)`), `SageBestellnummer` nvarchar(31), `Bezeichnung1/2` nvarchar(50), `Mindestbestellmenge` decimal(18,3),
  `Einkaufsmengeneinheit` nvarchar(5) + `UmrechnungsfaktorEK` decimal(18,6), `Wiederbeschaffungszeit` int, `Einzelpreis` decimal(18,4),
  `IstHauptlieferant` bit (aus `ArtikelVariantenExtended.Hauptlieferant` = `SageKto`). Längen aus `recArtikelLieferanten` ([[sage-sdata-masterdata]]
  Z. 176-180) – Quelle im Lesevertrag ist `ArtikelLieferantInfo`, Bestätigung P-S1. Datenbasis Bestellvorschläge (G7), keine Oberfläche.
- **Oberfläche (aus der Kreditoren-Spec übernommen, G9; E8a/E8b):** Menü Stammdaten → „Kreditoren“ und „Debitoren“ (`_Layout.cshtml:324-337`,
  sichtbar für `hasMasterDataRead` `:316-318`) mit Liste (Listen-View-Pattern, Spaltenauswahl, viewKeys `Creditors`/`Debitors` in
  `ColumnDefinitions.GetByViewKey`, `ColumnDefinitions.cs:575`), Anlegen/Bearbeiten, **nur Deaktivieren, kein Löschen**; Rollen `masterdata`
  (bearbeiten, `[RequireMasterDataAccess]`) / `masterdata_read` (lesen, `[RequireMasterDataReadAccess]` auf Klassenebene, Muster
  `ProductionWorkplacesController.cs:12, :83`), keine neue Rolle; je Installation ein Stamm ohne Standortspalte, kein eigener Schalter für das
  Menü (Kreditoren-Fragen 4/5, Empfehlung übernommen, offen). Bei `Source = Sage` sind die Sage-Felder schreibgeschützt, `ShortName`/`Remark`/
  `IsCoater`/`IsActive` bleiben pflegbar. Die Kreditorenliste hat zusätzlich den Filter „Beschichter“.
- **Dienstleister-Zuordnung (aus der Kreditoren-Spec, E8a):** Freitext `FaHierarchyOrderInfo.Dienstleister` (`FaHierarchyOrderInfo.cs:27`) →
  Kreditor über `ShortName` exakt (getrimmt, Groß/Klein egal); nicht zuordenbare Texte erscheinen sichtbar in einer Meldungskarte über der
  Kreditorenliste, nie stillschweigend übergangen. Lesequelle `IFaHierarchyOrderInfoRepository.GetAllAsync()` (genutzt in
  `BeschichtungsauftragService.cs:101`). ~~Die Auswahl des Dienstleisters je Beschichtungsauftrag bleibt beim Beschichtungsauftrag (dort Teil 4)~~ –
  **präzisiert:** Teil 4 hat den Kreditor-FK verworfen ([[2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec]], Antwort 4);
  `Beschichtungsauftrag.Dienstleister` bleibt Freitext (`Beschichtungsauftrag.cs:24-25`). Dieses Epic legt **keinen** FK am Beschichtungsauftrag an.
- Detail-Anforderungen, Code-Bezüge und Akzeptanzkriterien der Kreditoren-Pflege: [[2026-10-06-kreditoren-stammdaten-spec]] gilt als Anhang
  dieses Abschnitts; bei Widerspruch gilt diese Spec. Dort veraltete Zeilenangaben (am Code korrigiert): `ColumnDefinitions.cs:467` → `:575`,
  `_Layout.cshtml:282-301` → `:316-337`, `FaHierarchySyncService.cs:129-138` → `:138-147`, `FaHierarchySql.cs:135-143` → `:155-163`,
  `BeschichtungsauftragService.cs:100` → `:101` (`:100` liest die Knoten).
- Default der Schalter: aus.

### F11 Vorbereitung WMS → Sage (Lagerstamm, BDE-Personal)
- **Kein Sage-Service vorhanden** (Befund: weder Lagerplatz- noch Mitarbeiter-Anlage in `CommonWawiServices`; MasterData nicht freigegeben). Diese
  Spec baut nur die WMS-Seite:
  - **Herkunft:** Lagerplatz hat sie schon – `StorageLocation.Source` (`Sage` | `Manual`, `StorageLocation.cs:36-38`, DB-Default `Manual`
    `ApplicationDbContext.cs:196`); **keine** zweite Spalte. `BdeOperator` bekommt `Source` (`Sage` | `Manual`, Default `Manual`) nach demselben Muster.
  - Lagerplatz, BDE-Mitarbeiter (und Kreditor/Debitor, F10) bekommen `SageUebergabeStatus` (int-Enum `NichtNoetig = 0`, `Offen = 1`,
    `Uebergeben = 2`, `Fehler = 3`) plus `SageUebergabeAm` datetime2 NULL, `SageUebergabeFehler` nvarchar(1000) NULL.
  - Bestand bei der Migration: `NichtNoetig` (DB-Default) – auch für bestehende manuelle Plätze, damit die Liste nicht mit Altbestand
    überläuft. **Neu** im WMS angelegte Lagerplätze/Mitarbeiter stehen auf `Offen` und erscheinen in einer Liste „Offene Übergaben an Sage“
    (Lagerplätze: Stammdaten, `masterdata_read`; Mitarbeiter: BDE-Personalpflege, BDE-Filter wie F8).
  - Sobald der Lese-Abgleich den Datensatz in Sage findet (Lagerplatz: gleicher `Code` = Kurzbezeichnung; Mitarbeiter: Regel aus G14), wird er
    verknüpft (`SageLagerplatzId` bzw. `SageMitarbeiternummer`) und auf `Uebergeben` gesetzt. **Achtung Lagerplatz:** heute meldet der Abgleich
    einen manuellen Platz mit gleichem Code als Konflikt und lässt ihn unverändert (`LagerplatzSyncService.cs:170-176`); neu wird ein Platz mit
    `SageUebergabeStatus = Offen` stattdessen übernommen: `Source` wechselt auf `Sage`, `IstBuchbar`/`SageBuchungErlaubt` bleiben (der
    Update-Zweig `:156-168` fasst sie nie an). Folge, im Protokoll als Info ausgewiesen: ab dem nächsten Lauf korrigiert der Lagerbestand-Abgleich
    diesen Platz (`LagerbestandSyncService.cs:147` überspringt nur Nicht-Sage-Plätze). Alle anderen manuellen Plätze bleiben Konflikt wie heute.
  - Outbox-Tabelle `SageStammdatenOutbox` (Typ nvarchar(50), Schlüssel nvarchar(100), Payload-JSON nvarchar(max), Status int, Versuche int,
    Fehler nvarchar(1000), `AuditableEntity`) wird angelegt und bei jeder WMS-Neuanlage befüllt, aber **nicht gesendet** (kein Worker), bis DPS einen
    Service liefert (G5).
- Gilt analog für künftige WMS-geführte Stammdaten („und co.“): die Felder `Source`/`SageUebergabeStatus` und die Outbox sind generisch.

## Felderweiterung (wichtigste Felder)

Feldnamen = SData-Felder. „vorhanden“/„neu“ am Code geprüft (Entity + `ApplicationDbContext` + Migration, Fundstellen im Frontmatter).
**Längen:** belegt sind nur die Längen aus den Schema-Auszügen im Brain – `eptArtikel`/`eptKunden`/`eptLieferanten` ([[sage-sdata-api-vertraege]]
Z. 17-22, 72, 83) und `rec…`-Strukturen des MasterData-Vertrags ([[sage-sdata-masterdata]] Z. 56-57, 160-180). Diese beschreiben dieselben
Sage-Tabellen (`KHKArtikel`, `KHKKontokorrent`, `KHKLagerplaetze` …), aber einen anderen Vertrag. [[sage-sdata-artikel-felder]] nennt nur Feldnamen,
**keine** Längen/Typen. Wo „P-S1“ steht, ist Typ/Länge nicht belegt: die Etappe beginnt mit P-S1 (`$schema` lesen) und trägt das Ergebnis vor
der Migration in die Spec nach. Für das Lesen gilt: lieber zu lang als zu kurz – eine zu kurze Spalte bricht `SaveChanges` ab.

**Satellit oder Spalte (ADR [[0009-app-status-in-satelliten-tabellen-neben-sage-master]]).** ADR 0009 trennt Sage-Master und App-Status, damit ein
Import App-Daten **strukturell** nicht überschreiben kann statt nur per Konvention. Hier ist die Richtung umgekehrt, das Prinzip gleich: Felder,
die nur SData liefert, dürfen von einem View-Lauf nicht genullt werden (F1). Je Gruppe:
- **Artikel-Zusatzfelder → Satellit `ArticleSageDetails`** (1:1, PK = FK `ArticleId`, Cascade, `AuditableEntity` wie `ProductionOrderExtraInfo`,
  `ProductionOrderExtraInfo.cs:13`). Gründe: (1) Der View-Upsert schreibt `Articles` per Raw-SQL (`SageImportService.cs:461-497`) und der
  deprecated AgentJob fügt in `Articles` ein (`SQL/AgentJobs/02_Import_Artikel.sql:26-27`) – den Satelliten fasst keiner von beiden an, F1 ist
  damit strukturell erfüllt. (2) ~35 Spalten würden `Articles` verbreitern, das an vielen Stellen vollständig geladen wird (z. B.
  `LagerbestandSyncService.cs:96-97`). (3) Fehlt der Satellit (View-Betrieb), ist das „nicht aus Sage gelesen“ statt eines Default-Werts.
- **Artikelfelder, die `Articles` schon hat → bleiben Spalten** (beide Wege schreiben sie, wie heute).
- **Lagerplatz, Arbeitsplatz, BDE-Mitarbeiter → Spalten an der bestehenden Tabelle.** Dort mischen sich Sage- und WMS-Felder schon heute
  (`StorageLocation.IstBuchbar`/`SageBuchungErlaubt` neben `SageLagerplatzId`; `ProductionWorkplace.SageArbeitsplatznummer` neben `BdeAktiv`), die
  Abgleiche schreiben per EF nur benannte Properties (`LagerplatzSyncService.cs:156-168`), und es sind je 2-6 nullable Spalten. Regel F1: DTO-Feld
  `null` = nicht schreiben.
- **Cache je Lauf → eigene Tabelle ohne Audit** (`ProductionOrderPositions`, `SagePpsMitarbeiter`), Präzedenz `FaHierarchyNode` (`FaHierarchyNode.cs:7-11`).
- **Stammdaten mit Pflege → eigene Tabelle mit Audit** (`StorageAreas`, `Stillstandsgruende`, `Creditors`, `Debitors`, `ArticleSuppliers`, Outbox).

| Bereich | Feld (SData) | Ziel im WMS | vorhanden/neu | SQL-Typ (Beleg) | Etappe |
|---|---|---|---|---|---|
| Lagerort | Lagerkennung | `StorageAreas.Lagerkennung` (unique) | neu (Tabelle) | nvarchar(10) (`recLagerplaetzestamm` Lagerkennung(10)) | E1 |
| Lagerort | Bezeichnung, Lagertechnik | `StorageAreas.Bezeichnung`, `.Lagertechnik` | neu | P-S1 | E1 |
| Lagerort | IstDispoLager, IstSperrlager, Aktiv | `StorageAreas.IstDispoLager`, `.IstSperrlager`, `.IsActive` + `SageMissingSince` | neu | bit / datetime2 NULL | E1 |
| Lagerplatz | Kurzbezeichnung, Platzbezeichnung, Lagerkennung, PlatzID | `Code`, `Description`, `Zone`/`SageLagerkennung`, `SageLagerplatzId` | **vorhanden** | nvarchar(50)/(200)/(100)/(50), int (`ApplicationDbContext.cs:186-204`); Sage 50/110/10 passt | – |
| Lagerplatz | (Lagerort) | `StorageLocations.StorageAreaId` FK NULL | neu | int NULL, FK SetNull | E1 |
| Lagerplatz | IstGesperrt, IstGesperrtInventur | `StorageLocations.SageIstGesperrt`, `.SageIstGesperrtInventur` | neu | bit NULL | E1 |
| Lagerplatz | Breite, Hoehe, Laenge, Volumen, Tragkraft | `StorageLocations.SageBreite` … `.SageTragkraft` (nicht `Capacity`, das ist WMS-Feld) | neu | decimal(18,3) NULL, Typ P-S1 | E1 |
| Lagerplatz | Dimension1–3, Dimensionsebene | – | **nicht belegt** (in keinem Schema-Auszug, auch nicht in `recLagerplaetzestamm`) | nur wenn P-S1 sie zeigt, sonst entfällt | E1 |
| Bestand | Bestand je Artikel × Platz | `StockMovements` (Korrektur wie heute) | vorhanden | – | E2 |
| Bestand | Lagermengeneinheit, DezimalstellenLager | `Articles.Unit` bzw. `ArticleSageDetails.DezimalstellenLager` | Unit vorhanden / neu | siehe Artikel | E3a |
| Bestand | IstSperrlager | `StorageAreas.IstSperrlager` | neu | bit | E1 |
| Artikel | Bezeichnung1, Matchcode, Artikelgruppe, Lagermengeneinheit, Meldebestand, Hauptlagerplatz | `Articles.Description`, `.Matchcode`, `.ArticleGroup`, `.Unit`, `.ReorderLevel`, `.SagePrimaryStorageLocation`/`PrimaryStorageLocationId` | **vorhanden** (`Article.cs:26-55`) | nvarchar(500)/(200)/(100)/(20), decimal(18,3), nvarchar(100); Sage 50/50/15/5 passt | E3a |
| Artikel | Bezeichnung2 | `ArticleSageDetails.Bezeichnung2` | neu | nvarchar(50) (`eptArtikel`) | E3a |
| Artikel | Hauptartikelgruppe, Vaterartikelgruppe | `ArticleSageDetails.…` | neu | nvarchar(15) (`eptArtikel`) | E3a |
| Artikel | EANNummer, Hersteller, Gewichtseinheit | `ArticleSageDetails.…` | neu | nvarchar(15) / (20) / (10) (`eptArtikel`) | E3a |
| Artikel | HArtikelnummer, Zeichnungsnummer, Ersatzartikelnummer | `ArticleSageDetails.…` | neu | P-S1 (Zeichnungsnummer(50) ist nur am **FA** belegt, nicht übertragen) | E3a |
| Artikel | Langtext | `ArticleSageDetails.Langtext` | neu | nvarchar(max) (Freitext, Präzedenz `ApplicationDbContext.cs:1028-1033`) | E3a |
| Artikel | DezimalstellenLager | `ArticleSageDetails.DezimalstellenLager` | neu | int, Typ P-S1 | E3a |
| Artikel | Chargenpflicht, Nachweispflicht, IstFertigungsartikel, IstUnterbaugruppe, IstBestellartikel, IstErsatzteil, IstVerschleissteil, Aktiv | `ArticleSageDetails.…` | neu | bit (Sage -1/0, [[fallstricke]] „VB6-Booleans“) | E3a |
| Artikel | Lagerfuehrung, Entnahmeverfahren, Inventurmethode, Stuecklistentyp, Schwundart | `ArticleSageDetails.…` | neu | P-S1 (Zahl → int, Text → nvarchar(maxLength)) | E3a |
| Artikel | PlatzID | `ArticleSageDetails.SagePlatzId` | neu | int NULL | E3a |
| Artikel | Schwund, MinimaleLosgroesse, MaximaleLosgroesse, Laenge, Breite, Hoehe, Gewicht | `ArticleSageDetails.…` | neu | decimal(18,3) NULL, Typ P-S1 | E3a |
| Artikel Dispo | Lagerbestand, LagerbestandVerfuegbar, LagerbestandOhneDispo, Bestellmenge, USER_Mindestbestand | `ArticleSageDetails.…` | neu | decimal(18,3) NULL (wie `ReorderLevel`, `ApplicationDbContext.cs:226`) | E3a |
| Artikel Dispo | MittlererEK | `ArticleSageDetails.MittlererEK` | neu | decimal(18,4) NULL | E3a |
| Artikel Dispo | Hauptlieferant, LetzterLieferant | `ArticleSageDetails.…` | neu | nvarchar(20) – Wert `K31013` am AKE-Test im Kto-Format (Kto(20)); P-S1 bestätigen | E3a |
| Artikel Dispo | Wiederbeschaffungszeit | `ArticleSageDetails.Wiederbeschaffungszeit` | neu | int NULL (Wert 14 am AKE-Test), P-S1 | E3a |
| Artikel Dispo | Bestellzeitpunkt, ABCKlasse, USER_LagerplatzID | `ArticleSageDetails.…` | neu | P-S1 | E3a |
| Artikel USER | 15 Felder (Liste F5), erweiterbar über `Sync:ArtikelBenutzerfelder` | `ArticleAttributeValues.TextValue` + Definition `SyncSource`/`SyncFieldName` | TextValue **neu**, Sync-Spalten vorhanden | nvarchar(1000) (`ApplicationDbContext.cs:658`); Pfad*(250) belegt | E3b |
| FA-Kopf | IstEingelastet, IstBegonnen, IstBeendet, IstGesperrt, IstReaktiviert, Aktiv, Fertigungsart | `SageFaStates` des Rückmelde-Epics (G15) | dort neu | dort | – |
| FA-Kopf/Knoten | StrukturID, BelID, Artikel, Mengen | `FaHierarchyNodes` (vorhanden, G11) | vorhanden | – | E4a |
| FA-Position | **BelPosID**, BelID, StrukturID, Position, RessourceTyp, RessourceNummer, ArbeitsgangNummer, BelIDNachfolger, Menge/Soll/Ist, Soll-/Ist-Rüst- und Stückzeit, Status, **PlatzID + Kurzbezeichnung**, Schwund, Bedarfstermin | neu `ProductionOrderPositions` (Cache, kein Audit, `SyncedAt`; UNIQUE `BelPosID`, Index `BelID`, `StrukturID`) | neu | int; RessourceNummer/ArbeitsgangNummer nvarchar(31) (Artikel-/Arbeitsgang-/Arbeitsplatznummer je (31) belegt); Kurzbezeichnung nvarchar(50); Mengen/Zeiten decimal(18,4) (wie `FaHierarchyNode`, `ApplicationDbContext.cs:1034-1038`); RessourceTyp, Status P-S1; Bedarfstermin datetime2 NULL | E4b |
| Stückliste (nur E5) | Ebene, BelPosID, BelPosIdParent, ArtikelnummerUBG, Rüst-/Stückzeit, PlatzID, SpecificUserfields | `CachedBomItems` + neu | neu | int / nvarchar(31) / decimal(18,4) / `UserfieldsJson` nvarchar(max) | E5 (G13) |
| Arbeitsplatz | Arbeitsplatznummer, Bezeichnung1, USER_ArbeitsSchritt | `ProductionWorkplaces.SageArbeitsplatznummer`, `.Name`, `.ArbeitsschrittCode` | **vorhanden** (`ProductionWorkplace.cs:34-42`) | nvarchar(31)/(200)/(20) | E6a |
| Arbeitsplatz | BdeTerminalId | `ProductionWorkplaces.SageBdeTerminalId` | neu | nvarchar(20) (`recArbeitsplaetzestamm` BdeTerminalId(20)) | E6a |
| Arbeitsplatz | Kostenstelle, Schichtplannummer | `ProductionWorkplaces.SageKostenstelle`, `.SageSchichtplannummer` | neu | P-S1 | E6a |
| Arbeitsplatz | Aktiv | – (Filter `Aktiv eq -1` wie `SageArbeitsplatzSql.cs:75`) | entfällt | – | E6a |
| Arbeitsgang | Rüstzeit, Stückzeit, Arbeitsplatznummer, DefaultRessourceTyp/-Nummer | ~~`OperationConfig` + neu~~ (G12) | entfällt bis G12 | – | – |
| PPS-Mitarbeiter | Nummer, Matchcode, Vorname, Nachname, Gruppe, Kartennummer, Aktiv, Kostenstelle, Schichtplannummer, UserName | neu `SagePpsMitarbeiter` (Cache je Lauf, kein Audit, `SyncedAt`) | neu | Längen P-S1 | E6b |
| PPS-Mitarbeiter | Nummer | `BdeOperators.SageMitarbeiternummer` (gefiltert unique) + `Source`, `SageUebergabeStatus` … (F11) | neu | Länge wie Nummer (P-S1) | E6b / E9 |
| Stillstand | Nummer, Bezeichnung, Aktiv | neu `Stillstandsgruende` (+ `SageMissingSince`, Audit) | neu | P-S1 | E6b |

## Technischer Lösungsentwurf

- **Reader-Schnittstellen wiederverwenden.** Für A1, A2, A7/A8, A10 gibt es sie schon (`ISageLagerplatzReader.cs:8`, `ISageBestandReader.cs:6`,
  `ISageArbeitsplatzReader.cs:12`, `ISageBuchungLookupReader.cs:8`); je eine zweite Implementierung `Sdata…Reader` in
  `IDEALAKEWMSService/Services/Sdata/`. Neue Schnittstellen nur dort, wo heute inline gelesen wird: `ISageArtikelReader` (A3) und
  `ISageFaStrukturReader` (A5); die SQL-Implementierung ist jeweils der bestehende Code, unverändert herausgezogen.
- **Quellenwahl als Weiche an der Schnittstelle** – Muster `BomRepositoryMasterSwitch` ([[0013-bom-bridge-repository-schnittstelle-statt-cache-kopie]]):
  je Interface ein `…QuelleSwitch : I…Reader`, der beim Aufruf `Sync:Quelle:<Bereich>` liest (`ServiceSettings.GetValueSafeAsync`, DB-first) und an
  SQL- oder SData-Reader delegiert; bei `Vergleich` ruft er beide, gibt die SQL-Zeilen zurück und schreibt die Abweichungen über `SdataVergleich`
  (reine, testbare Diff-Funktion je Schlüssel) in den Protokolllauf `SageQuellenvergleich`. DI in `IDEALAKEWMSService/Program.cs:58-74` registriert
  die Weiche unter dem Interface, die beiden Reader als konkrete Typen. **Die Abgleich-Services bleiben unverändert** (gleicher Ctor, gleiche Logik).
  DI-Auflösungstest Pflicht ([[fallstricke]] §10, wie ADR 0013).
- **SData-Client:** bestehender `ISageSdataReadClient` um die Paging-Methode erweitern (F2), im Service als typisierter `HttpClient` registrieren.
  Kein zweiter Client.
- Abgleichslogik, Reconciliation, Protokoll, Fehlermail bleiben in den bestehenden Services (Ausnahme G10).
- Neue Schalter im Katalog `ServiceSettingDefinitions` (`ServiceSettingDefinitions.cs:16-101`, DB-first, [[0008-servicesettings-db-first-mit-typisiertem-katalog]],
  Drift-Guard `ServiceSettingDefinitionsTests.cs`): `Sync:Quelle:Lagerplaetze`, `…:Lagerbestand`, `…:Artikel`, `…:FaStruktur`, `…:Arbeitsplaetze`,
  `…:LagerbuchungLookup` (String, Default `View`), `Sync:ArtikelBenutzerfelder` (String, Default Liste F5), `Sync:PpsMitarbeiterEnabled`,
  `Sync:StillstandsgruendeEnabled`, `Sync:KreditorenEnabled`, `Sync:DebitorenEnabled`, `Sync:ArtikelLieferantEnabled` (Bool, Default `false`);
  `Sync:Quelle:Stueckliste` nur mit E5. Default `View` bzw. aus → Verhalten nach Deploy unverändert.
- **Protokoll ([[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]]):** neue Läufe `SageQuellenvergleich`, `PpsMitarbeiter`, `Stillstandsgruende`,
  `Kreditoren`, `Debitoren`, `ArtikelLieferant` in `SyncLogServices.All` + deutsche `DisplayNames` (`SyncLogServices.cs:8, :54-84`), `ISyncLogger`
  als letzter Ctor-Parameter, deutsche Count-Keys. Bestehende Läufe tragen die Quelle als Info-Zeile („Quelle: SData“), ihre Count-Keys bleiben.
- **SyncWorker:** neue Blöcke für A11, A12, A13 je hinter ihrem eigenen Schalter (Default `false`), jeweils in `RunResilientAsync`
  (`SyncWorker.cs:512-520`); A13 nach dem Artikel-Abgleich (`:85-96`), weil `ArticleSuppliers` auf `Articles` verweist. Bestehende Blöcke unverändert.
- Gegenrichtung (F11): Outbox + Status ohne Worker; Struktur so, dass ein späterer `SageStammdatenWorker` analog `SageBookingWorker` andockt.
- Oberflächen (Lagerort-Spalte, Kreditoren, Debitoren, Mitarbeiter-Zuordnung, „Offene Übergaben“): Skill `frontend-design` explizit aufrufen,
  Bootstrap 5 und vorhandene Partials, WCAG AA; Freitext nie per `@Html.Raw` in JS ([[fallstricke]]).

## Migrations-/SQL-Auswirkungen

Additiv, je Etappe mit Schemaänderung **eine** Migration ([[0004-migrations-und-sql-disziplin]]): Model → `dotnet ef migrations add` →
`SQL/<NN>_*.sql` (DDL mit `OBJECT_ID`- bzw. `COL_LENGTH`-Guard im eigenen Batch, Indexe je eigener Batch mit `sys.indexes`-Guard und exakt dem
EF-Namen per `HasDatabaseName`, Muster `SQL/93_AddProductionWorkplaceSageFields.sql`) → `__EFMigrationsHistory`-Insert im separaten Batch →
`SQL/00_FreshInstall.sql` an **zwei** Stellen (Schema + `MigrationId`).

**Nummernlage (ermittelt 2026-11-06):** Bündel-Zweig endet bei `SQL/97_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung.sql` =
Migration `20261019092105_…` (keine Datei `SQL/98*` oder höher in irgendeinem Worktree). Das Rückmelde-Epic
[[2026-11-02-sage-pps-fa-bde-abgleich-spec]] (eigener Worktree) **plant ebenfalls** `SQL/98`/`99` („bei Umsetzung prüfen“). Die Nummern unten
sind daher **Reservierungen**: maßgeblich ist die nächste freie Nummer im Bündel **zum Start der Etappe**; wer zuerst merged, behält seine
Nummern, der andere zählt weiter (G15). Dreistellige Nummern ab 100 siehe G17.

| Etappe | Migration (Name) | SQL | Inhalt | destruktiv? |
|---|---|---|---|---|
| E0 | – | – | keine Schemaänderung (nur Settings-Katalog, Code) | – |
| E1 | `AddStorageAreasAndStorageLocationSageFields` | `SQL/98_…` | Tabelle `StorageAreas` (UNIQUE `Lagerkennung`); `StorageLocations` + `StorageAreaId` (FK SetNull, Index), `SageIstGesperrt`, `SageIstGesperrtInventur`, `SageBreite/Hoehe/Laenge/Volumen/Tragkraft` | nein |
| E2 | – | – | keine | – |
| E3a | `AddArticleSageDetails` | `SQL/99_…` | Tabelle `ArticleSageDetails` (PK/FK `ArticleId`, Cascade) | nein |
| E3b | `AddArticleAttributeValueTextValue` | `SQL/100_…` | `ArticleAttributeValues.TextValue nvarchar(1000) NULL` | nein |
| E4a | – | – | keine | – |
| E4b | `AddProductionOrderPositions` | `SQL/101_…` | Tabelle `ProductionOrderPositions` (UNIQUE `BelPosID`, Index `BelID`, `StrukturID`) | nein |
| E5 | `AddCachedBomItemSdataFields` | `SQL/102_…` (reserviert, entfällt bei G13 = zurückstellen) | `CachedBomItems` + Ebene/BelPosID/BelPosIdParent/ArtikelnummerUBG/Zeiten/PlatzID/UserfieldsJson | nein |
| E6a | `AddProductionWorkplaceSdataFields` | `SQL/103_…` | `ProductionWorkplaces` + `SageBdeTerminalId`, `SageKostenstelle`, `SageSchichtplannummer` | nein |
| E6b | `AddSagePpsMitarbeiterAndStillstandsgruende` | `SQL/104_…` | Tabellen `SagePpsMitarbeiter`, `Stillstandsgruende`; `BdeOperators.SageMitarbeiternummer` (gefiltert UNIQUE `IS NOT NULL`) | nein |
| E7 | – | – | keine (Queue verweist auf `StockMovement`) | – |
| E8a | `AddCreditors` | `SQL/105_…` | Tabelle `Creditors` (UNIQUE gefiltert `SageKto`, Index `ShortName` – UNIQUE nur bei bestätigtem Kurzname-Abgleich, Kreditoren-Frage 2) inkl. `Source`, `SageUebergabeStatus`, `SageUebergabeAm`, `SageUebergabeFehler` | nein |
| E8b | `AddDebitors` | `SQL/106_…` | Tabelle `Debitors` (gleicher Aufbau ohne `IsCoater`/`SageEKMindestbestellwert`, mit `SageVertreter`/`SageKundenNameFA`) | nein |
| E8c | `AddArticleSuppliers` | `SQL/107_…` | Tabelle `ArticleSuppliers` (FK `ArticleId` Cascade, FK `CreditorId` Restrict, UNIQUE `ArticleId+CreditorId`) | nein |
| E9 | `AddSageStammdatenOutboxAndUebergabeStatus` | `SQL/108_…` | Tabelle `SageStammdatenOutbox`; `StorageLocations` + `SageUebergabeStatus` (int NOT NULL DEFAULT 0), `SageUebergabeAm`, `SageUebergabeFehler`; `BdeOperators` + `Source` (nvarchar(20) NOT NULL DEFAULT 'Manual') + dieselben drei Übergabe-Spalten | nein (DEFAULT füllt Bestand) |

- **Keine** Spalte wird entfernt oder umbenannt (Rückfall auf View ohne Rückmigration). Nichts ist daten-destruktiv; DB-Backup vor Deploy wie üblich.
- `SQL/AgentJobs/`: nicht betroffen. Einziger Job auf einer betroffenen Tabelle ist `02_Import_Artikel.sql` (deprecated seit v1.17.0,
  `:1-10`), er fügt nur benannte Spalten in `Articles` ein (`:26-27`) – neue Daten liegen im Satelliten.
- FreshInstall: neue Tabellen nach ihren FK-Zielen (`StorageAreas` vor dem FK an `StorageLocations`, `Creditors` vor `ArticleSuppliers`).
- Index-Anlage und Filter sind Manual-UAT (InMemory führt keine DDL aus; InMemory erzwingt keine UNIQUE-Indexe → App-Layer-Guards für
  `SageKto`, `SageMitarbeiternummer`, `StorageAreas.Lagerkennung`).

## Audit-Feld-Auswirkungen

- Neue fachliche Entitäten erben `AuditableEntity` ([[0003-auditableentity-als-entity-basis]]): `StorageArea`, `ArticleSageDetail`, `Stillstandsgrund`,
  `Creditor`, `Debitor`, `ArticleSupplier`, `SageStammdatenOutboxItem`. Jede Änderung durch einen Abgleich setzt `ModifiedAt`, `ModifiedBy`,
  `ModifiedByWindows`.
- ~~Sync-Schreiber `system:sage-sync`, Outbox `system:sage-outbox`~~ – **überholt:** bestehende Abgleiche behalten ihren Schreibernamen, egal welche
  Quelle: Lagerplatz/Lagerbestand `system:sync` (`LagerplatzSyncService.cs:15`, `LagerbestandSyncService.cs:14`), Artikel `IDEALAKEWMSService`
  (Raw-SQL `SageImportService.cs:473`), Werkbank `ProductionWorkplaceSync` (`ProductionWorkplaceSyncService.cs:14`). Neue Abgleiche nach demselben
  Muster mit eigenem Namen: `PpsMitarbeiterSync`, `StillstandsgruendeSync`, `KontokorrentSync`, `ArtikelLieferantSync`; `ArticleSageDetails` schreibt
  der Artikel-Abgleich als `IDEALAKEWMSService`. `ModifiedByWindows`/`CreatedByWindows` wie im jeweiligen Service (`Environment.MachineName` bzw.
  `SYSTEM_USER`).
- Oberfläche (Kreditoren, Debitoren, Zuordnungen, Übergabe-Status): Werte aus `ICurrentUserService` (`GetDisplayName`, `GetWindowsUserName`),
  Muster `OrderRecipientsController.cs:79-80, :147-148`.
- Cache-Tabellen ohne Audit, Nachvollziehbarkeit über `SyncedAt` + Protokoll: `ProductionOrderPositions`, `SagePpsMitarbeiter` (Präzedenz
  `FaHierarchyNode.cs:7-11`); `CachedBomItems` bleibt ohne Audit (`CachedBomItem.cs:5-8`).

## Akzeptanzkriterien

Etappe in Klammern; ein Kriterium gilt mit dem Commit dieser Etappe als prüfbar.

1. (alle) Nach Deploy mit Default-Schaltern verhalten sich alle Abgleiche an AKE und IDEAL exakt wie vorher: gleiche Count-Keys und Zählerstände
   im Aktivitätsprotokoll wie im Lauf vor dem Deploy, keine zusätzliche Fehlermail, kein Eintrag `SageQuellenvergleich`.
2. (E1, E2, E3a, E4a, E6a, E7) Jeder der Schalter `Sync:Quelle:Lagerplaetze`, `…:Lagerbestand`, `…:Artikel`, `…:FaStruktur`, `…:Arbeitsplaetze`,
   `…:LagerbuchungLookup` lässt sich auf `SData` und zurück auf `View` stellen; der nächste Lauf nutzt die gewählte Quelle und weist sie im
   Protokoll aus („Quelle: SData“). Ausnahme laut F6/G11: `FaStruktur = SData` wirkt bis P-F2 wie `View` und warnt.
3. (E0 + jede Etappe mit Schalter) Im Modus `Vergleich` schreibt der Abgleich nur aus der View; der Protokolllauf `SageQuellenvergleich` nennt je
   Bereich die Anzahl abweichender Schlüssel und höchstens 20 Beispiele (Schlüssel, Feld, View-Wert, SData-Wert).
4. (E0) Ein unbekannter Schalterwert (z. B. `sdata ` mit Leerzeichen oder `Neu`) wirkt wie `View` und erzeugt genau eine Warnung je Lauf.
5. (E0) Jede SData-Abfrage setzt `orderBy`, `Accept: application/json`, paginiert bis `$totalResults` (Unit-Test mit 3 Seiten à `count`);
   eine Abfrage ohne `where` mit `$totalResults` > 100.000 bricht nach der ersten Seite mit Fehler ab; `VerkaufPositionenAllInfo`/
   `EinkaufPositionenAllInfo` ohne `where` werden vor dem Aufruf abgewiesen.
6. (E0) Ein SData-Fehler (HTTP 500 mit `$diagnoses`, 401, Timeout, HTML statt JSON) lässt den SData-Reader **werfen**; kein Reader liefert in
   diesem Fall eine leere Liste. Folge: kein Lagerplatz wird deaktiviert, kein Bestand genullt, keine Buchung als „nicht vorhanden“ gewertet.
7. (E1, E2 + G10) Fällt SData aus, endet nur der betroffene Bereich mit Fehler im Protokoll; andere Bereiche laufen im selben Zyklus weiter;
   kein stiller Rückfall auf View. Fehlermail für A3/A5/A7/A8 über `RunResilientAsync`; für A1/A2 laut Entscheidung G10.
8. (E1) Lagerplätze aus SData: Anzahl angelegter + aktualisierter + unveränderter Sage-Plätze = Plätze aktiver Lagerorte laut
   `LagerorteInfo`/`LagerplaetzeInfo` abzüglich gemeldeter Dubletten; Vergleichsmodus am AKE-Test 0 Abweichungen bei `Code`, `Zone`,
   `Description`, `SageLagerplatzId`; `IstBuchbar`/`SageBuchungErlaubt` bleiben unverändert (DB-Prüfung vor/nach).
9. (E1) `StorageAreas` enthält je aktivem Lagerort eine Zeile; jeder Sage-Platz hat `StorageAreaId`; Sperr-Kennzeichen sind gesetzt; nach
   Rückschalten auf `View` und einem Lauf sind `StorageAreaId`, `SageIstGesperrt*`, `SageBreite…` unverändert (nicht genullt).
10. (E2) Lagerbestand aus SData ergibt dieselben Korrektur-Buchungen wie die View: Vergleichsmodus am AKE-Test 0 Abweichungen; im Modus `SData`
    entstehen im ersten Lauf nach einem View-Lauf **keine** `SageEinbuchung`/`SageAusbuchung` (DB-Zählung `StockMovements` vor/nach).
11. (E3a) Artikel im Modus `SData` (am AKE-Test; im Modus `Vergleich` wird der Satellit nicht geschrieben): `ArticleSageDetails` hat für jeden gelesenen Artikel eine Zeile mit den Feldern aus F5 (Stichprobe
    00722001: Meldebestand 85 in `Articles.ReorderLevel`, Hauptlieferant `K31013`, WBZ 14); `Articles.ArticleGroup` enthält den reinen Code; nach
    Rückschalten auf `View` und einem Lauf ist `ArticleSageDetails` unverändert.
12. (E3b) Die 15 Default-Benutzerfelder erscheinen als Text-Merkmale (Definition mit `SyncSource = Sage`, `SyncFieldName` = USER-Feld) und sind in
    der Merkmal-Pflege nicht editierbar; ein zusätzlich in `Sync:ArtikelBenutzerfelder` eingetragenes Feld erscheint nach dem nächsten Lauf ohne
    Code-Änderung; ein unbekannter Feldname erzeugt eine Warnung statt eines Abbruchs; ein gleichnamiges manuelles Merkmal bleibt unverändert und wird
    als Konflikt gemeldet.
13. (E4a) Vergleichsmodus FA-Struktur meldet für Struktur 835 am AKE-Test 0 Abweichungen bei `HauptFA`/`VaterFA`/`SubFA`/`Position`
    (Wurzel 835 mit `SubFA = 835`; Kanten 835→836…841; Blätter mit `SubFA = 0`) – sofern dort eine FA-Struktur-View existiert, sonst nur die
    SData-Projektion gegen die Regel aus F6 (Unit-Test mit den 17 Positionen aus [[sage-sdata-commonwawiservicesresources]]).
14. (E4b) Nicht eingelastete oder inaktive FAs/Unter-FAs fehlen in der SData-Menge; ein danach fehlender materialisierter Unter-FA bekommt
    `SageMissingSince` wie heute und erscheint nicht als offen.
15. (E4b) Je FA der Menge sind die Positionen mit `BelPosID` und Lagerplatz gespeichert (836: BelPosID 10641, Platz 11 „HR 0 0 0“); ein zweiter
    Lauf ersetzt sie vollständig (keine Dubletten, UNIQUE `BelPosID`).
16. (E5, nur falls G13 E5 behält) Stückliste aus SData: gleiche Positionen/Mengen/Ebenen wie die View für mindestens 5 Testartikel
    (u. a. 86003871); Benutzerfelder je Position gespeichert. An IDEAL bleibt der BOM-Cache-Lauf „übersprungen (hierarchischer Modus)“.
17. (E6a) Werkbank-Anlage und Arbeitsgang-Abgleich im Modus `SData` legen am AKE-Test dieselben Werkbänke/Kürzel an wie der SQL-Weg
    (Vergleichsmodus 0 Abweichungen; Arbeitsplatz 9999); `SageBdeTerminalId` ist gesetzt; keine `OperationConfig`-Spalte kommt hinzu.
18. (E6b) PPS-Mitarbeiter-Abgleich legt **keinen** `BdeOperator` an und verknüpft nie über den Namen; Sage-Mitarbeiter ohne Verknüpfung erscheinen
    in der Liste „nicht zugeordnet“; aktive `BdeOperator` ohne `SageMitarbeiternummer` erzeugen eine Warnung mit Anzahl. Eine bereits vergebene
    `SageMitarbeiternummer` wird bei manueller Zuordnung mit Feldfehler abgewiesen.
19. (E6b) `Stillstandsgruende` enthält nach einem Lauf je Sage-Stillstandsgrund eine Zeile; ein verschwundener bekommt `SageMissingSince`.
20. (E7) Lookup im Modus `SData` findet eine am AKE-Test gebuchte WMS-Buchung per Memo (`true`); bei SData-Fehler wirft er und der Worker sendet
    den haengenden Eintrag **nicht** erneut (Protokoll-Warnung wie heute).
21. (E7) WMS-Umbuchung zwischen zwei Sage-buchbaren Plätzen erzeugt **einen** Queue-Eintrag; der Payload hat `Lagerbewegungsart = "Umbuchung"`
    mit Herkunft und Ziel; Sage-Platz → WMS-only-Platz ergibt `Entnahme`, WMS-only → Sage `Zugang`, WMS-only → WMS-only keinen Eintrag; der
    Nacherfassungs-Lauf (`EnqueueMissingAsync`) erfasst Umbuchungen nach derselben Regel; Sage-Korrekturbuchungen werden nie eingereiht.
22. (E8a, E8b) Kreditoren- und Debitoren-Pflege erfüllt die Akzeptanzkriterien 1–17 der [[2026-10-06-kreditoren-stammdaten-spec]] sinngemäß
    (Liste nach ADR 0005, Anlegen, Bearbeiten, Deaktivieren, Rollen, Audit, Sonderzeichen); Kreditor zusätzlich `Matchcode` und Filter „Beschichter“;
    Sage-Felder bei `Source = Sage` schreibgeschützt (Formular **und** POST serverseitig).
23. (E8a) Die Meldekarte listet jeden nicht-leeren `Dienstleister`-Text aus `FaHierarchyOrderInfos` ohne passenden aktiven Kreditor-Kurznamen;
    `Beschichtungsauftrag.Dienstleister` und dessen Druck sind unverändert (Freitext).
24. (E8c) Bei aktivem Schalter werden Lieferanten → `Creditors` und Kunden → `Debitors` gelesen; die Anzahl je Tabelle entspricht der Treffermenge
    nach der Kontoart-Regel aus P-K1; `ShortName`/`Remark`/`IsCoater` bleiben unverändert; WMS-eigene Kreditoren ohne `SageKto` bleiben erhalten;
    gleichnamige werden als „mögliche Dublette“ gemeldet; `ArticleSuppliers` hat für 00722001 einen Eintrag mit `IstHauptlieferant` für `K31013`.
25. (E9) Ein im WMS neu angelegter Lagerplatz bzw. BDE-Mitarbeiter steht auf `Offen`, erzeugt einen Outbox-Eintrag und erscheint in „Offene
    Übergaben an Sage“; nach Anlage in Sage verknüpft ihn der Lese-Abgleich (`Uebergeben`, `SageUebergabeAm` gesetzt). Bestandsdaten stehen nach der
    Migration auf `NichtNoetig`. Es wird nichts an Sage gesendet (kein HTTP-Aufruf, Outbox-Status bleibt `Offen`).
26. (alle) Alle neuen Schalter stehen in `ServiceSettingDefinitions.All` mit Beschreibung und erscheinen in `/ServiceSettings`; der Drift-Guard-Test ist grün.
27. (alle) Neue Protokollläufe haben deutsche Anzeigenamen in `SyncLogServices.DisplayNames`; Build + alle Tests grün; DI-Auflösungstest für die
    Quellen-Weichen grün.

## Test-Szenarien

Neues Kapitel **TS-106 „Sage-Abgleich über SData“** in `docs/TESTSZENARIEN.md` (letztes Kapitel TS-105, `:10686`), danach
[[testszenarien-index]] nachziehen. Jede Etappe ergänzt ihre Unterszenarien im Commit der Etappe. Browser-Testprotokoll für die neuen Oberflächen.

**Gemeinsame Vorbedingungen:** WMS-Test-DB mit Bündel-Stand; `SData:BaseUrl`/`SData:Dataset` auf `sagetest01` / `ake_TEST2026;1`,
Zugangsdaten in appsettings; `SageLagerbuchungSslZertifikatPruefen` je Testsystem; `WorkerSettings:SyncDryRun = false`; Aktivitätsprotokoll
offen; Testdaten AKE-Test: Struktur 835 (FAs 835–841), Artikel 00722001/86003871, Lagerplätze 1/11/2555/2864/4333, Arbeitsplatz 9999.
IDEAL-Endpunkt erst nach G2.

**Muster je Bereich (TS-106.1 bis .7: Lagerplätze, Lagerbestand, Artikel, FA-Struktur, Arbeitsplätze/Kürzel, Lagerbuchungs-Lookup, Benutzerfelder)**
- **a Default unverändert** – Schritte: Schalter nicht anlegen/`View`, einen Zyklus laufen lassen. Erwartet: Count-Keys wie im Lauf vor dem Deploy,
  kein `SageQuellenvergleich`. Negativ: Schalterwert `Neu` → Warnung, Verhalten wie `View`.
- **b Vergleich** – Schalter `Vergleich`, ein Zyklus. Erwartet: Daten wie bei `View` (DB-Stichprobe), Protokolllauf `SageQuellenvergleich` mit
  Abweichungszahl und ≤ 20 Beispielen. Negativ: künstliche Abweichung (z. B. Platzbezeichnung in Sage ändern) erscheint als Beispiel.
- **c SData** – Schalter `SData`, ein Zyklus. Erwartet: Protokoll „Quelle: SData“, Ergebnis laut AK des Bereichs.
- **d Rückfall** – zurück auf `View`, ein Zyklus. Erwartet: SData-only-Felder unverändert (DB vor/nach), keine Korrekturbuchungen aus dem Wechsel.
- **e Ausfall** – `SData:BaseUrl` auf einen nicht erreichbaren Host bzw. falsches Passwort, ein Zyklus. Erwartet: Bereich endet mit Fehler,
  andere Bereiche laufen, **keine** Deaktivierung/Nullsetzung/Neuversendung; Mail laut AK 7/G10. Negativ: Sage liefert HTML (Proxy-Seite) →
  ebenfalls Fehler, nicht „0 Zeilen“.

**Bereichsspezifisch**
- **TS-106.1 Lagerplätze (E1):** Vorbedingung: ein manueller Platz mit gleichem Code wie ein Sage-Platz. Erwartet c: Lagerort-Spalte in der
  Lagerplatzliste, Spaltenfilter wirkt, `IstBuchbar`/`SageBuchungErlaubt` unverändert; der manuelle Platz bleibt Konflikt. Negativ: Lagerort in Sage
  inaktiv → seine Plätze werden deaktiviert wie heute, der Lagerort bekommt `SageMissingSince`.
- **TS-106.2 Lagerbestand (E2):** Erwartet b: 0 Abweichungen; c: keine Korrekturbuchung im ersten SData-Lauf. Negativ: Bestand 00722001 in Sage
  ändern → genau eine Korrektur wie beim View-Weg; Cap-Test (`Sync:LagerbestandNullsetzenMaxPerRun = 1`) verhält sich wie heute.
- **TS-106.3 Artikel (E3a):** Erwartet c: 00722001 mit `ArticleSageDetails` (Hauptlieferant K31013, WBZ 14), `ArticleGroup` reiner Code. Negativ:
  Artikel außerhalb des Filters (G16) wird nicht angelegt; Hauptlagerplatz ohne WMS-Platz → Warnung `hauptlagerplatz_fehlt` wie heute.
- **TS-106.4 Benutzerfelder (E3b):** Erwartet: 15 Text-Merkmale am Artikel sichtbar, in der Merkmal-Pflege mit Hinweis „aus Sage“ gesperrt;
  `USER_Modell` zusätzlich eintragen → erscheint nach einem Lauf. Negativ: `USER_GibtEsNicht` → Warnung, Lauf läuft; manuelles Merkmal
  „Maschine“ existiert → Konfliktmeldung, Wert unverändert; Wert > 1000 Zeichen → gekürzt + Warnung.
- **TS-106.5 FA-Struktur (E4a, E4b):** Erwartet b: Struktur 835 Kanten ohne Abweichung; `ProductionOrderPositions` mit BelPosID 10641 / Platz 11
  für FA 836. Negativ: `FaStruktur = SData` vor P-F2 → Warnung, Verhalten wie `View`; FA in Sage ausgelastet/inaktiv setzen → fehlt in der
  Menge, Unter-FA bekommt `SageMissingSince`.
- **TS-106.6 Arbeitsplätze/Kürzel (E6a):** Erwartet b: 0 Abweichungen (9999); c: `SageBdeTerminalId` gesetzt; Arbeitsgang-Abgleich legt keine
  neuen Kürzel an. Negativ: Arbeitsplatz in Sage inaktiv → `SageMissingSince` wie heute.
- **TS-106.7 Lookup + Umbuchung (E7):** Vorbedingung: `SageLagerbuchungAktiv = true`, zwei Plätze mit `SageBuchungErlaubt`, ein WMS-only-Platz.
  Schritte: Umbuchung Sage → Sage, Sage → WMS-only, WMS-only → Sage, WMS-only → WMS-only. Erwartet: Queue-Einträge Umbuchung/Entnahme/Zugang/keiner;
  in Sage IU-Buchung mit Memo `IdealAkeWms Lagerbuchung SM#<id>#`; einen Eintrag künstlich auf „Gesendet“ älter als
  `Sync:SageLagerbuchungStuckMinutes` setzen → Lookup (SData) bestätigt ihn. Negativ: SData nicht erreichbar → Eintrag bleibt, wird nicht neu
  gesendet.
- **TS-106.8 PPS-Mitarbeiter + Stillstand (E6b):** Vorbedingung: PPS-Mitarbeiter in Sage angelegt (Voraussetzung 3). Erwartet: Liste „nicht
  zugeordnet“ in der BDE-Personalpflege, manuelle Zuordnung setzt `SageMitarbeiternummer`; Warnung für BdeOperator ohne Nummer. Negativ: Sage-
  Mitarbeiter mit gleichem Namen wie ein BdeOperator → keine automatische Verknüpfung; doppelte Nummer → Feldfehler; Rolle ohne BDE-Schichtleitung →
  AccessDenied.
- **TS-106.9 Kreditoren/Debitoren (E8a, E8b):** Szenarien TS-x.1 bis x.8 der [[2026-10-06-kreditoren-stammdaten-spec]] je für Kreditoren und
  Debitoren, zusätzlich Filter „Beschichter“ und Feld Matchcode. Negativ: `masterdata_read` → keine Knöpfe, Direkt-POST → AccessDenied;
  Sonderzeichen-Name.
- **TS-106.10 Lese-Abgleich Kontokorrent (E8c):** Vorbedingung: P-K1 beantwortet. Erwartet: Anzahl Kreditoren/Debitoren = Treffer laut P-K1;
  Sage-Felder an `Source = Sage` schreibgeschützt; `ArticleSuppliers` für 00722001. Negativ: WMS-Kreditor „Muster GmbH“ und gleichnamiges Sage-Konto
  → „mögliche Dublette“, keine Verknüpfung; Konto in Sage löschen → `SageMissingSince`, kein Löschen.
- **TS-106.11 Übergaben (E9):** Schritte: Lagerplatz und BDE-Mitarbeiter im WMS anlegen. Erwartet: Status „offen“, Outbox-Eintrag, Liste „Offene
  Übergaben“; nach Anlage desselben Platzes in Sage und einem Lagerplatz-Lauf: „übergeben“, `Source = Sage`, Info im Protokoll. Negativ: kein
  HTTP-Aufruf an Sage (Netzwerk-Mitschnitt bzw. Outbox-Status bleibt), Bestandsplätze stehen auf „nicht nötig“.
- **TS-106.12 AKE-Regression:** an der AKE-Test-DB mit Default-Schaltern: Artikel-, Lagerplatz-, Lagerbestand-Abgleich wie vorher; Menü
  „Kreditoren“/„Debitoren“ sichtbar für Stammdaten-Rollen.

## Etappen (nur bei epic: true)

Ein Worktree, ein Zweig (`feature/2026-08-07-ideal-teile-1-5`), **ein** Merge am Ende; je Etappe ein Commit. Den Zweig während der Arbeit mit
`scripts/sync-worktree.ps1` auf `main`-Stand halten. Version: nächste freie beim Start von E0 (heute v1.73.0, `AppVersion.cs:5` = 1.72.0) für das
ganze Epic in **beiden** `AppVersion.cs`; Changelog-Einträge je Etappe sammeln.

| # | Etappe | Migration / SQL | Status | Commit |
|---|--------|-----------------|--------|--------|
| 0 | `ISageSdataReadClient` um Paging + Vertrag erweitern, Service-HttpClient, Quellen-Weichen-Muster (`…QuelleSwitch`), `SdataVergleich`, Protokoll `SageQuellenvergleich`, Settings-Katalog – kein Bereich umgestellt | – | erledigt | `7fdcf656` |
| 1 | Lagerorte + Lagerplätze (A1): SData-Reader `ISageLagerplatzReader`, `StorageAreas`, neue Spalten, Lagerort-Spalte in der Liste | `SQL/98` | erledigt | `a29454f2` |
| 2 | Lagerbestände (A2): SData-Reader `ISageBestandReader`, Chargen/Serien nur zählen | – | erledigt | `4cb02950` |
| 3a | Artikel (A3): `ISageArtikelReader` (SQL herausgezogen + SData), Satellit `ArticleSageDetails`, Anzeige in Artikeldetails | `SQL/99` | erledigt | `0dc6be7f` |
| 3b | Artikel-Benutzerfelder als Text-Merkmal (`TextValue`, Definition-Anlage, Sperre in der Merkmal-Pflege) | `SQL/100` | erledigt | `a8406374` |
| 4a | FA-Struktur (A5): `ISageFaStrukturReader` (SQL herausgezogen + SData-Projektion), Vergleichsmodus Kanten | – | erledigt | `de9fe12d` |
| 4b | `ProductionOrderPositions` + Lesefilter eingelastet/aktiv | `SQL/101` | erledigt | `1da971b7` |
| 5 | Stückliste BOM-Cache (A6) – **nur falls G13 sie im Epic lässt** (AKE-only) | `SQL/102` (reserviert) | zurückgestellt (G13) | – |
| 6a | Arbeitsplätze (A7) + Kürzel (A8 laut G12): SData-Reader `ISageArbeitsplatzReader`, neue Spalten | `SQL/102` (102 frei durch G13) | erledigt | `264929ee` |
| 6b | PPS-Mitarbeiter (A11) + Stillstandsgründe (A12): Cache, Zuordnungsliste in der BDE-Personalpflege | `SQL/103` | erledigt | `debbf741` |
| 7 | Lagerbuchungs-Lookup über SData (A10), Umbuchung in der Queue | – | erledigt | `90a3555f` |
| 8a | Kreditoren: Tabelle, Pflege-Oberfläche, Dienstleister-Meldekarte (Umfang Kreditoren-Spec + Matchcode/Beschichter) | `SQL/104` | erledigt | `276a9077` |
| 8b | Debitoren: Tabelle, Pflege-Oberfläche | `SQL/105` | erledigt | `30ee0f1d` |
| 8c | Lese-Abgleich Kontokorrent → Kreditoren/Debitoren + `ArticleSuppliers` (A13) – erst nach P-K1 | `SQL/106` | erledigt | `9bdbeaed` |
| 9 | Outbox + Übergabe-Status für Lagerplatz/BDE-Mitarbeiter (F11), Liste „Offene Übergaben“ | `SQL/107` | erledigt | `e6821035` |

**Etappen-Schnitt (Rework, Begründung):** Ein Dev-Lauf schafft erfahrungsgemäß eine Migration + einen Reader/Service + eine Oberfläche mit
Tests. Danach geteilt:
- **E3 → 3a/3b:** Satellit mit ~35 Feldern + Reader-Extraktion aus `SageImportService` ist allein eine volle Etappe; die Benutzerfelder brauchen
  eine zweite Migration (`TextValue`) und Änderungen an der Merkmal-Pflege (andere Dateien, eigenes Testbild).
- **E4 → 4a/4b:** 4a zieht den Struktur-Read aus `FaHierarchySyncService` heraus und baut die Projektion + Vergleich ohne Schemaänderung; 4b
  bringt Tabelle, Filter und Full-Refresh. So ist der riskante Umbau des laufenden IDEAL-Imports vor der neuen Tabelle einzeln abnehmbar.
- **E6 → 6a/6b:** Arbeitsplätze ändern zwei bestehende Abgleiche; PPS-Mitarbeiter/Stillstand sind neue Abgleiche mit neuer Oberfläche in einem
  anderen Controller (`BdeMasterDataController`, BDE-Rollen).
- **E8 → 8a/8b/8c:** die Kreditoren-Spec allein war als ganze Spec geplant (Controller, 3 Views, Repository, Migration, 17 AK); Debitoren
  wiederholen das Muster; der Lese-Abgleich hängt an P-K1 und darf die Pflege nicht blockieren.
- E1, E2, E7, E9 bleiben je eine Etappe (je ein Reader bzw. eine Regeländerung, höchstens eine Migration).

Reihenfolge-Begründung: E0 ist Voraussetzung; E1–E3b sind risikoarm und testen den Client an einfachen Stammdaten; E4a/E4b sind der größte
Gewinn für IDEAL/BDE; E6a/E6b sind Voraussetzung des Rückmelde-Epics; E7, E8a–c, E9 hängen nicht voneinander ab (E8c braucht E3a wegen
`ArticleSuppliers.ArticleId`; E9 erweitert die Tabellen aus E1/E6b/E8a).

## Voraussetzungen außerhalb des Codes (vor Freigabe/Etappe 1)

1. DPS: Freigabe + Lizenz Lesevertrag und Services (G1); Preis.
2. IDEAL: SData-Endpunkt kaufen (G2); bis dahin AKE-Test.
3. Sage AKE-Test: PPS-Mitarbeiter anlegen (für E6b und das Rückmelde-Epic); leeren Dispo-Stapel `WMSTEST01` löschen (Befund §14).
4. DPS-Fragen: FA-Benutzerfelder lesbar machen (`BelegProperty1–4`, G4/G11), Timestamp-Delta (G8), Service für Lagerplatz- und Mitarbeiter-Anlage (G5),
   Dispo-Stapel-Löschen wirkt nicht (Befund §14).

## Prüfschritte für den Menschen (Fakten außerhalb des Codes – Aussagen dazu gelten bis zum Ergebnis als unverifiziert)

- **P-S1 – Typ/Länge je neuem Feld** (AKE-Test, vor der Migration der jeweiligen Etappe; Ergebnis als Nachtrag in diese Spec):
  `GET {SData:BaseUrl}/sdata/ol/CommonWawiServicesResources/ake_TEST2026;1/$schema` (Basic-Auth; Vertragsschema, ~0,9 MB) und je Ressource
  (`LagerorteInfo`, `LagerplaetzeInfo`, `Artikel`, `ArtikelVariantenExtended`, `PpsFaBelegePositionenInfo`, `PpsArbeitsplaetzeInfo`, `PpsMitarbeiterInfo`,
  `PpsStillstandsbegruendungenInfo`, `Kontokorrent`, `ArtikelLieferantInfo`) die Elemente der Felder aus „Felderweiterung“ mit `type`,
  `sme:maxLength`, `sme:totalDigits`/`sme:fractionDigits` notieren. Gegenprobe in der Sage-DB:
  ```sql
  SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE
  FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_NAME IN ('KHKLagerorte','KHKLagerplaetze','KHKArtikel','KHKArtikelVarianten','KHKPpsFaBelegePositionen',
                       'KHKPpsArbeitsplaetze','KHKPpsMitarbeiter','KHKKontokorrent','KHKArtikelLieferanten')
  ORDER BY TABLE_NAME, ORDINAL_POSITION;
  ```
  > **Ergebnis 2026-11-06 (Koordination, AKE-Test-`$schema`, nur GET):** `type(sme:maxLength)`; bei `decimal` ist `maxLength` die Byte-Größe (8),
  > **Genauigkeit/Nachkommastellen liefert das Schema nicht** (`totalDigits`/`fractionDigits` fehlen) → für Dezimalfelder die SQL-Gegenprobe noch nötig.
  > - `LagerorteInfo`: Lagerkennung string(10), Bezeichnung string(40), Lagertechnik string(10), IstDispoLager bool, AnzahlLagerplaetze int, WarenflussKZ string(5), Aktiv bool
  > - `LagerplaetzeInfo`: PlatzID int, Lagerkennung string(10), Dimension1–3 int, Dimensionsebene short, Kurzbezeichnung string(50), Platzbezeichnung string(110), IstGesperrt bool, IstGesperrtInventur bool, Hoehe/Breite/Laenge/Volumen/Tragkraft decimal, Status short, Inventurmethode short
  > - `Artikel`: Artikelnummer string(31), Bezeichnung1/2 string(50), Matchcode string(50), Artikelgruppe/Haupt-/Vaterartikelgruppe string(15), Ersatzartikelnummer string(31), Hersteller string(20), HArtikelnummer string(20), Basis-/Lagermengeneinheit string(5), DezimalstellenLager short, UmrechnungsFaktorLME decimal, Lagerfuehrung bool, PlatzID int, Entnahmeverfahren string(6), Inventurmethode short, Chargenpflicht short, Nachweispflicht short, IstBestellartikel/IstFertigungsartikel/IstUnterbaugruppe/IstErsatzteil/IstVerschleissteil/Aktiv bool, Stuecklistentyp short, Schwundart string(1), Schwund decimal, Min-/MaximaleLosgroesse decimal, Zeichnungsnummer string(50), Laenge/Breite/Hoehe decimal, Gewichtseinheit string(10)
  > - `Artikel` USER: Maschine/Ausfuehrung/Kaeltemittel/Ventil/Produktfamilie/MasterartikelNr string(50), TypenschildArtikel string(10), PfadSchaltplan/PfadGlaszeichnung string(250), VaultID int, Artikeltyp short, DPSBestand decimal, IstIDEAL/DruckFertigungsauftrag/AUSCAD bool
  > - `ArtikelVariantenExtended`: EANNummer string(15), ABCKlasse string(1), Lagerbestand/LagerbestandOhneDispo/LagerbestandVerfuegbar/Gewicht/MittlererEK/Bestellmenge/Meldebestand decimal, Bestellzeitpunkt int, Hauptlieferant/LetzterLieferant string(20), Wiederbeschaffungszeit short, USER_LagerplatzID int, USER_Mindestbestand int
  > - `PpsFaBelegePositionenInfo`: BelPosID/BelID/BelIDNachfolger/PlatzID int, Position short, RessourceTyp string(2), RessourceNummer string(31), ArbeitsgangNummer string(50), Menge/Soll-/Istmenge/Soll-/IstRuestzeit/Soll-/IstStueckzeit/Schwund decimal, Status short, Bedarfstermin dateTime, Kurzbezeichnung string(50)
  > - `PpsArbeitsplaetzeInfo`: Arbeitsplatznummer string(31), Bezeichnung1 string(50), Schichtplannummer string(31), Kostenstelle string(20), Aktiv bool, BdeTerminalId string(20)
  > - `PpsMitarbeiterInfo`: Nummer string(20), Matchcode string(50), Vorname/Nachname string(35), Gruppe string(10), Kartennummer string(20), ExterneNummer int, Aktiv bool, Schichtplannummer string(31), Kostenstelle string(20), UserName string(128)
  > - `PpsStillstandsbegruendungenInfo`: Stillstandnummer string(31), Bezeichnung string(50), IstMitarbeiter/IstNachkalkulation/IstLohnkosten/IstArbeitsplatzkosten/IstZusatzkosten bool, Kostenart short, Memo string(max), ReweVerbuchungsart int
  > - `Kontokorrent`: Kto string(20), **KtoArt string(1)**, KtoTyp short, Matchcode string(50), Adresse int (Verweis auf Adressen), EUUStID string(15), Zahlungskond string(10), Gruppe string(10), Vertreter string(10), IstGesperrt/Aktiv bool, USER_EKMindestbestellwert decimal
  > - `ArtikelLieferantInfo`: Lieferant string(20), Artikelnummer string(31), Bestellnummer string(31), Bezeichnung1/2 string(50), Mindestbestellmenge decimal, Einkaufsmengeneinheit string(5), MengenberechnungEK string(10), DezimalstellenEK short, UmrechnungsfaktorEK/VPEK decimal, PreiseinheitEK short, Wiederbeschaffungszeit short, Einzelpreis/Rabattsatz decimal
- **P-S2 – Dataset und Mandant:** `GET …/CommonWawiServicesResources/ake_TEST2026;1/LagerorteInfo?select=Lagerkennung,Mandant&orderBy=Lagerkennung&count=100&format=json`
  – kommen Zeilen mit `Mandant` ≠ 1? Nein → das Dataset legt den Mandanten fest, `Mandant eq 1` kann entfallen.
  > **Ergebnis 2026-11-06 (AKE-Test):** `LagerorteInfo` 36 Zeilen, alle `Mandant = 1`; `LagerplaetzeInfo where Mandant ne 1` → 0. Am AKE-Test legt das
  > Dataset (`ake_TEST2026;1`) den Mandanten fest; `Mandant eq 1` kann im `where` entfallen, außer wo `…Specific` ihn als Pflichtparameter verlangt
  > (`PpsRessourcenListenInfo`). Am IDEAL-Endpunkt erneut prüfen.
- **P-K1 – Kontoart Lieferant/Kunde am Lesevertrag (entscheidet E8c):** 1) im `$schema` von `Kontokorrent` alle Felder notieren, deren Name `Kto`,
  `Art` oder `Typ` enthält; 2) je Kandidat-Feld `<F>`:
  `GET …/Kontokorrent?select=Kto,<F>&orderBy=Kto&count=20&format=json` und `…/Kontokorrent?where=<F> eq '<Wert>'&select=Kto&orderBy=Kto&count=1&format=json`
  (liefert `$totalResults` je Wert); 3) Gegenprobe Sage-DB:
  ```sql
  SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'KHKKontokorrent' ORDER BY ORDINAL_POSITION;
  -- danach je Kandidat <F>:
  SELECT <F>, COUNT(*) AS Anzahl FROM KHKKontokorrent GROUP BY <F> ORDER BY <F>;
  ```
  Festhalten: Feldname, Werte für Lieferant und Kunde, Anzahl je Wert (Summe = 2.821 am AKE-Test). Alternativ: Sind `apiLieferanten`/`apiKunden`
  (getrennte Listen) von DPS mit freigegeben? Dann wäre die Kontoart nicht nötig.
  > **Ergebnis 2026-11-06 (AKE-Test):** Feld **`KtoArt` string(1)**. Werte: `D` = 534, `K` = 2.287, Summe 2.821 = alle Konten; `S` = 0.
  > Beispiele: D10000…D20010 (`KtoArt D`), Lieferant **K31013** (Hauptlieferant von 00722001, `ArtikelLieferantInfo`) hat `KtoArt K`.
  > → **K = Kreditor/Lieferant, D = Debitor/Kunde** (aus den Daten belegt, nicht nur aus dem Namen). `KtoTyp` (short, Werte 0 und 2 gesehen) ist ein
  > anderes Merkmal – Bedeutung ungeprüft, für E8c nicht nötig. Filter E8c: `where KtoArt eq 'K'` bzw. `'D'`. Die SQL-Gegenprobe ist damit optional.
- **P-F1 – Viewtexte IDEAL** (Sage-DB IDEAL; Namen aus den WMS-ServiceSettings):
  ```sql
  -- WMS-DB IDEAL:
  SELECT [Key], [Value] FROM dbo.ServiceSettings
  WHERE [Key] IN ('Sync:FaHierarchyListeViewName','Sync:FaHierarchyInfosViewName','Sync:ArticlesViewName');
  -- Sage-DB IDEAL, je Name:
  SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.<ViewName>'));
  ```
  Festhalten: Filter (WHERE) und Herkunft jeder Spalte; Ergebnis nach `SQL/sage-views/` (dort TODO `FAListe.sql:57`, `FAInfos.sql:50`).
- **P-R1 – Artikel-Filter IDEAL:** wie P-F1 für `Sync:ArticlesViewName` (entscheidet G16).
- **P-F2 – IDEAL-Benutzerfelder am FA** (Sage-DB IDEAL, entscheidet G11):
  ```sql
  SELECT OBJECT_NAME(object_id) AS Tabelle, name, TYPE_NAME(user_type_id) AS Typ, max_length
  FROM sys.columns
  WHERE object_id IN (OBJECT_ID(N'dbo.KHKPpsFaBelege'), OBJECT_ID(N'dbo.KHKPpsFaBelegePositionen')) AND name LIKE N'USER[_]%'
  ORDER BY Tabelle, name;
  ```
  und – sobald der IDEAL-Endpunkt existiert – `$schema` von `PpsFaBelegeInfo`/`PpsFaBelegePositionenInfo`: sind die Felder hinter
  Arbeitsschritte, Kommissionieren, Arbeitsbereich, BemerkungPN … dort enthalten?
- **P-A1 – Arbeitsschritt über SData (entscheidet G12):**
  `GET …/PpsArbeitsplaetzeInfo?select=Arbeitsplatznummer,USER_ArbeitsSchritt&orderBy=Arbeitsplatznummer&count=5&format=json` – HTTP 200 mit Wert
  oder 500 „Ungültiger Spaltenname“ (Text in `$diagnoses`).
  > **Ergebnis 2026-11-06 (AKE-Test):** `PpsArbeitsplaetzeInfo` hat im Schema **kein** USER-Feld; Abfrage mit `select=…,USER_ArbeitsSchritt` → **500**
  > „Ungültiger Spaltenname "USER_ArbeitsSchritt"“. `PpsArbeitsgaengeInfo` hat ebenfalls keins. Am AKE-Test existiert die Spalte nicht (vermutlich
  > IDEAL-eigenes Benutzerfeld) → **am IDEAL-Endpunkt erneut prüfen**; ob der DPS-Lesevertrag dort Benutzerfelder des Arbeitsplatzes mitliefert,
  > ist offen (Frage an DPS, wie G4).
- **P-M1 – Badge-Inhalt BDE (entscheidet G14):** Was steht auf den Mitarbeiter-Ausweisen, die am BDE-Terminal gescannt werden (= `PersonnelNumber`)
  – die Sage-Kartennummer, die Sage-Mitarbeiternummer oder eine eigene Nummer? (Fachfrage an die Fertigung.)

## Deploy

> **Vorläufig** – der QA-Lauf bestätigt gegen den echten Diff.

- **Web-App:** ja (Kreditoren/Debitoren, Lagerort-Spalte, Merkmal-Sperre, Mitarbeiter-Zuordnung, Offene Übergaben; Settings-Katalog liegt im
  Web-Projekt `IdealAkeWms/Models/ServiceSettingDefinitions.cs`).
- **Service:** ja (SData-Reader, Weichen, neue Abgleiche, Umbuchung in der Queue).
- **Migration:** ja – `SQL/98` bis `SQL/108` laut Etappen-Tabelle (ohne `SQL/102`, falls E5 entfällt), Nummern zum Merge-Zeitpunkt prüfen (G15/G17).
  Reihenfolge: DB-Backup → SQL-Skripte in Nummernfolge (inkl. History-Insert) → Service stoppen → Web und Service deployen → Service starten.
  Alle Skripte additiv; Default-Schalter `View`/aus → keine Verhaltensänderung bis zum bewussten Umschalten.
- **Nach dem Deploy (IDEAL, nach G1/G2):** je Bereich erst `Vergleich`, Protokoll `SageQuellenvergleich` prüfen, dann `SData`.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- Weitere Pflichten des Dev-Laufs: Version in beiden `AppVersion.cs`, `Views/Help/Changelog.cshtml`, Hilfeseite mit konkreten Details (Schalter,
  Vergleichsmodus, Kreditoren, Übergaben), `README.md` (neue Service-Schalter nur verweisen), Brain: changelog, [[feature-map]], `codebase/services.md`
  (neue Keys), `codebase/integrationen.md`, `codebase/datenmodell.md`, `codebase/controller.md` (Kreditoren/Debitoren), Glossar „Kreditor“/„Debitor“/
  „Lagerort“, ADR für „Quellen-Weiche View/SData“ (neue Entscheidung), Nachtrag ADR 0004 (G17), [[fallstricke]] (Lese-Reader müssen werfen).

## Offene Rueckfragen

1. **G1 Lizenz/Freigabe** – siehe Frontmatter; Empfehlung: Freigabe abwarten, E0 trotzdem bauen.
2. **G2 IDEAL-Endpunkt** – bis zum Kauf gegen den AKE-Test entwickeln; IDEAL-Benutzerfelder erst am IDEAL-Endpunkt prüfbar.
3. **G3** – BEANTWORTET (Gerald 2026-11-06): alle Artikelfelder vorsehen (F5, Felderweiterung).
4. **G4 FA-Zusatzinformationen** – Empfehlung: bleiben auf der View (A9 unverändert), bis DPS `BelegProperty1–4` belegt.
5. **G5 Datenhoheit WMS-geführter Stammdaten** – Service bei DPS beauftragen; bis dahin WMS führt, Status „offen“.
6. **G6 Kreditoren/Debitoren Herkunft** – Empfehlung: beides (Sage | WMS), Sage-Felder bei Herkunft Sage schreibgeschützt.
7. **G9** – BEANTWORTET (Gerald 2026-11-06): Kreditoren-Spec ist Teil des Epics (F10, E8a–c); deren offene Fragen 1–7 gelten weiter
   (Antwort 1 dort: zusätzlich Matchcode + „Beschichter ja/nein“, Pflichtfelder offen – in F10 übernommen).
8. **G7 Bestellvorschläge** – eigene Folge-Spec; hier nur `ArticleSuppliers` als Datenbasis.
9. **G8 Delta** – Vollabgleich der offenen FAs + beendete der letzten 30 Tage.
10. **G10 Fehlermail bei Reader-Ausfall A1/A2** – heute keine Mail; für SData/Vergleich zusätzlich mailen?
11. **G11 FA-Struktur nicht vollständig aus SData** – FAInfos (PPSImport) und 13 Knotenfelder bleiben SQL bis P-F2?
12. **G12 A8 Arbeitsgänge** – A8 folgt dem Arbeitsplatz-Reader statt `PpsArbeitsgaengeInfo`?
13. **G13 E5 Stückliste und A4-Reader (AKE-only)** – zurückstellen?
14. **G14 PPS-Mitarbeiter-Zuordnung** – gegen welches Sage-Feld wird `PersonnelNumber` verglichen?
15. **G15 FA-Kopf-Status / SQL-Nummern** – nur `SageFaStates` des Rückmelde-Epics nutzen; Nummernvergabe nach Start-Reihenfolge?
16. **G16 Artikel-Filter** – welche Menge liest der SData-Artikel-Reader (nach P-R1)?
17. **G17 SQL-Nummern ab 100** – dreistellig weiterzählen, Reihenfolge über die Deploy-Liste?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. G1 Lizenz/Freigabe DPS →
2. G2 IDEAL-Endpunkt / AKE-Test →
3. G3 Artikelfelder → **beantwortet (Gerald 2026-11-06, aus dem Frontmatter übernommen): alle vorgeschlagenen Artikelfelder vorsehen**
4. G4 FA-Zusatzinformationen auf View →
5. G5 Datenhoheit / DPS-Service →
6. G6 Herkunft Kreditoren/Debitoren →
7. G9 Umfang Kreditoren-Spec → **beantwortet (Gerald 2026-11-06, aus dem Frontmatter übernommen): alles auf einmal, Kreditoren-Spec in F10/E8 aufgegangen**
8. G7 Bestellvorschläge →
9. G8 Delta / Vollabgleich →
10. G10 Fehlermail A1/A2 → **ja (Gerald 2026-11-06): bei Quelle SData/Vergleich Fehlermail; View-Verhalten unverändert**
11. G11 FA-Struktur-Grenze SData → **ja (Gerald 2026-11-06): SData nur für Kanten + Positionen; FAInfos (Quelle PPSImport, nicht Sage) und Knotenfelder bleiben SQL; Umschalten auf SData erst nach P-F2, Vergleichsmodus ab E4a**
12. G12 A8 Arbeitsgänge → **ja (Gerald 2026-11-06): A8 folgt dem A7-Reader; Rüst-/Stückzeit/Standard-Arbeitsplatz ins Rückmelde-Epic. P-A1 am AKE-Test: USER_ArbeitsSchritt nicht vorhanden → am IDEAL-Endpunkt prüfen**
13. G13 E5 + A4-Reader zurückstellen → **ja (Gerald 2026-11-06): E5 Stückliste und AKE-Reader FA flach (A4) zurückgestellt bis zur AKE-Freigabe**
14. G14 PPS-Mitarbeiter-Zuordnung → **Gerald 2026-11-06: BDE-Ausweise gibt es noch nicht (P-M1 entfällt). Neues Feld SageMitarbeiternummer (= Sage `Nummer`); Zuordnung BdeOperator ↔ PPS-Mitarbeiter nur manuell in der Zuordnungsliste, Vorschlag nur bei exakter Gleichheit PersonnelNumber = Sage `Nummer`, nie über den Namen. Kartennummer/ExterneNummer werden mitgelesen, aber nicht zum Abgleich genutzt.**
15. G15 SageFaStates / SQL-Nummern → **Gerald 2026-11-06: dieses Epic zuerst – es erhält SQL/98 ff. und legt SageFaStates in E4b in der Form an, die das Rückmelde-Epic plant; das Rückmelde-Epic nutzt sie und nummeriert seine SQL-Dateien neu**
16. G16 Artikel-Filter → **ja (Gerald 2026-11-06): A3 bleibt auf View bis P-R1 den IDEAL-Filter zeigt; E3a baut Reader + Vergleichsmodus**
17. G17 SQL-Nummern ab 100 → **ja (Gerald 2026-11-06): dreistellig weiterzählen, Reihenfolge über die Deploy-Liste sichern; Nachtrag zu ADR 0004**

## QA-Nachweis 2026-11-06 (qa-agent) — Ergebnis: NICHT Testbereit (Blocker B1)

> [!note] Überholt durch die QA-Wiederholung weiter unten (B1, B2, B5 erledigt, Status Testbereit). Dieser erste Lauf bleibt als Nachweis stehen.

> [!failure] Status bleibt `InUmsetzung`
> Build, Tests und DEV-Deploy sind grün, die Oberflächen funktionieren. Aber jeder SData-Lesezugriff über `QueryAllAsync` scheitert am
> echten Sage-Endpunkt (Befund **B1**, am AKE-Test per GET belegt). Damit ist kein Schalter-Modus `SData`/`Vergleich` abnehmbar.
> Mit Default-Schaltern ist das Verhalten unverändert (B1 wirkt erst nach bewusstem Umschalten und bricht dann sicher mit Fehler ab).

Diff-Basis `d91b1d1d..HEAD` (14 Commits `7fdcf656` … `e6821035`), Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, v1.73.0.

### 1. Build und Tests (im Worktree)
- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 24 Warnungen (NuGet-Advisories, CS8321 – Bestand).
- `dotnet test`: **IdealAkeWms.Tests 2391 bestanden / 1 übersprungen / 0 Fehler**; **IDEALAKEWMSService.Tests 484 bestanden / 0 / 0**.

### 2. Code-Review (selbst, synchron) – Befunde und Umgang
| # | Schwere | Befund (Fundstelle) | Umgang |
|---|---|---|---|
| **B1** | **Blocker** (funktional; fail-safe) | `SageSdataReadClient.QueryAllAsync` beginnt mit `startIndex = 1` („SData: 1-basiert“, `IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs:179`). Der Sage-Server (`CommonWawiServicesResources`, AKE-Test) zählt **0-basiert**: `LagerplatzbuchungenInfo where Memo like 'IdealAkeWms%'` → ohne startIndex 12 Zeilen ab 865807 (`$startIndex` 0); `startIndex=0` → ab 865807; `startIndex=1` → ab 865808 (erste Zeile fehlt); `count=12&startIndex=1` → 11 Zeilen. `LagerorteInfo where Mandant eq 1`: `startIndex=1` → 35 von 36, `startIndex=0` → 36. Folge: es fehlt immer die erste Zeile, am Ende kommt eine leere Seite vor `$totalResults` → `SdataLeseException` „Unvollständig gelesen: n-1 von n“ in **jedem** Reader (Lagerplätze, Bestand, Artikel, FA-Struktur, Arbeitsplätze, PPS-Mitarbeiter, Stillstand, Kontokorrent, Artikel-Lieferant). Lookup (E7): ein Treffer liefert `$totalResults 1`, `$resources []` → wirft statt `true` (`SM#19367#` existiert am AKE-Test). Wirkung: kein Datenverlust, keine Doppelbuchung (alles wirft), aber kein SData-/Vergleichsmodus funktioniert; im Vergleich je Lauf eine Fehlermail (G10). Die Unit-Tests (`SageSdataPagingTests.cs:69, :283`) zementieren `startIndex=1`. | **Offen – Dev-Fix:** Start bei `startIndex = 0`, Folgeseiten `startIndex += seite.Count` (unverändert), Tests auf 0 umstellen, Kommentar + [[fallstricke]] („Sage-SData startIndex ist 0-basiert, entgegen SData-2.0-Spec“). Danach QA erneut. |
| B2 | Mittel (nur mit Schalter `SageLagerbuchungUmbuchungAktiv`) | Umbuchungen **mit FA-Bezug** werden mit eingereiht: `PickingTransferService.cs:293-305` (Kommissionier-Transfer, `ProductionOrder` gesetzt) schreibt per `_context` am Decorator vorbei; der Nacherfassungs-Lauf `SageBookingQueueRepository.EnqueueMissingAsync` (`:143-149`) filtert nicht auf `ProductionOrder` und reiht sie binnen 15 min als Entnahme/Umbuchung ein; ebenso `StockMovementsController.cs:427-443` (`vm.ProductionOrder`). Spec F9: „Nicht in dieser Spec: Entnahme mit FA-Bezug (→ Rückmelde-Epic)“. Zudem uneinheitlich: Kommissionier-Umbuchungen kommen nur über den Sweep. | **Offen – Entscheidung Gerald vor dem Einschalten:** FA-gebundene Umbuchungen ausschließen (eine Regel in `SageBookingEnqueueDecision`, auch in der EF-Abfrage) oder bewusst mitbuchen und Spec anpassen. Default aus → kein Deploy-Risiko. |
| B3 | Niedrig | E7-Lookup `where Memo like '%SM#<id>#%'`: Kodierung `#`→`%23` und führendes `%` am AKE-Test **geprüft** – der Server wertet korrekt aus (`$totalResults` 1 für `SM#19367#`, 0 für `SM#1936#` = kein Präfix-Treffer, 0 für `SM#999999#`). Das TODO(UAT E7) in `SdataBuchungLookupReader.cs` ist für die Kodierung erledigt; `true` kommt erst nach dem B1-Fix an. | Nach B1-Fix TS-106.9 „Lookup SData“ wiederholen. |
| B4 | Niedrig (Hinweis) | Kein Plausibilitäts-Guard im Lagerplatz-SData-Weg: liefert `LagerorteInfo` keinen aktiven Lagerort, deaktiviert der Abgleich alle Sage-Plätze (bewusster `ponytail:`-Kommentar in `LagerplatzSyncService.SyncStorageAreasAsync`; der View-Weg verhält sich gleich). Am AKE-Test kommt `Aktiv` als Zahl `-1/0` – `SageBoolJsonConverter` liest das korrekt. | Akzeptiert (wie View); beim ersten SData-Lauf DB vor/nach prüfen (Checkliste 4). |
| B5 | Niedrig (Doku) | Brain-Pflichten des Dev-Laufs fehlen: Brain-Changelog v1.73.0 in `secondbrain/changelog/`, ADR „Quellen-Weiche View/SData“, Nachtrag ADR 0004 (G17), [[fallstricke]] („Lese-Reader müssen werfen“ + B1), [[feature-map]], `codebase/services.md`/`datenmodell.md`/`controller.md`. Die Testindex-Zeile 106 hat der QA-Lauf nachgetragen. | Offen – vor dem Merge. |
| B6 | Info | Abweichungen von der Spec, im Code als TODO/Kommentar vermerkt: Kontokorrent ohne `Adressen` (Name = Sage-Matchcode, Adressfelder unberührt, TODO(P-S1)); `LagerbestaendeArtikelInfo` ohne `select` (Feldnamen angenommen, TODO(P-S1)); Arbeitsschritt-Kürzel fehlt über SData (P-A1) → Arbeitsgang-Abgleich setzt mit Warnung aus; Artikel `SData` gesperrt bis P-R1, FA-Struktur bis P-F2; neuer Schalter `SageLagerbuchungUmbuchungAktiv` (Spec hatte keinen, Default aus); `Quelle: View` erscheint als zusätzliche Info-Zeile in bestehenden Läufen (Count-Keys unverändert, AK 1 erfüllt). | Keine Aktion; P-Prüfungen bleiben beim Menschen. |

Geprüft ohne Befund:
- **Default-Verhalten:** alle `Sync:Quelle:*` Default `View` (`ServiceSettingDefinitions.cs`); `SageQuelleSchalter.Parse`: null/leer/`View` → View still, unbekannt → View + Warnung, nie still SData; neue Bool-Schalter Default `false`; neue SyncWorker-Blöcke hinter eigenen Gates. Beide DEV-DBs nach Deploy: alle `View`/`false`.
- **Herausgezogene SQL-Reader funktional unverändert:** zeilenweiser Abgleich aller entfernten Zeilen aus `SageImportService.cs`/`FaHierarchySyncService.cs` gegen `SageArtikelReader.cs`/`SageFaStrukturReader.cs` – nicht wiedergefunden werden nur Zeilen, die im Service verblieben sind (Warnung „Quelle nicht vorhanden“, `FaHierarchyViewNameException`-catch jetzt im try vor dem generischen catch). SQL-Texte, Ordinals und Parser identisch.
- **E9 Konflikt-Verhalten:** Übernahme nur bei `SageUebergabeStatusText.IstOffen` (Offen/Fehler); Bestandsplätze `NichtNoetig` bleiben Konflikt (Test `Run_ManualRecord_UebergabeOffen_WirdUebernommen_Bestand_BleibtKonflikt`).
- **E2 Nullsetzen-Schutz:** SData-Bestand wirft bei Zeilen ohne Artikelnummer/PlatzID, bei „alle Bestand null“ und bei unvollständigem Paging; Vergleich liefert View-Zeilen weiter; Cap/Dedup unverändert.
- **E7 Doppelbuchung:** SData-Lookup wirft bei jedem Fehler (Worker sendet dann nicht neu); Vergleich nutzt das View-Ergebnis. Eine Typliste (`SageBookingEnqueueDecision.BookableTypes`, auch per `Contains` in der EF-Abfrage); Sage-Korrekturen nie.
- **Defektklassen:** keine Werte per `@Html.Raw` in `<script>` (nur statische JSON-Konfigblöcke); kein `hidden` + `d-*`; `?? 0` nur für ViewBag-Zähler (Anzeige); keine Parallel-Arrays (Sage-Merkmale serverseitig übersprungen in `ArticleAttributeRepository.SaveValuesAsync`); Sage-Schreibschutz Kreditoren/Debitoren und Merkmale serverseitig am gespeicherten `Source`/`SyncSource`; App-Guards zu allen gefilterten UNIQUE-Indizes.
- **Überposting:** `StorageLocationsController.Edit` übernimmt `SageUebergabeStatus` nicht aus dem POST; Create setzt `Offen` serverseitig.

### 3. Nachweisart je AK
| AK | Nachweis |
|---|---|
| 1 Default unverändert | Unit (`SageQuelleTests`, Drift-Guard) + DEV-DB-Werte; Count-Keys im Echtlauf **Manual-UAT** (Syncs auf DEV aus) |
| 2, 3 Schalter SData/Vergleich | Unit mit Fakes; real **Manual-UAT – derzeit FEHLER durch B1** |
| 4 unbekannter Wert | Unit (`SageQuelleTests`) |
| 5 Paging/orderBy/Sperren | Unit (`SageSdataPagingTests`) – **Annahme startIndex=1 falsch (B1)** |
| 6, 7 Fehler werfen, Mail G10 | Unit; real Manual-UAT |
| 8–11, 13–17, 19, 20, 24 (Reader-Inhalte, Vergleich am AKE-Test, Positionen, Lookup, Kontokorrent) | Unit mit Fakes; **Manual-UAT** (Sage-Reads, Raw-SQL `DELETE ProductionOrderPositions`, UNIQUE-Indizes) – blockiert durch B1 |
| 12 Merkmal-Sperre | Unit (`ArticleAttributesControllerTests`, `ArtikelBenutzerfelderTests`); Browser Manual-UAT |
| 18 Zuordnung | Unit (`BdeMasterDataSageZuordnungTests`) + **DEV-Browser OK**; UNIQUE-Wettlauf Manual-UAT |
| 21 Umbuchung-Queue | Unit (`SageBookingEnqueueDecisionTests`, `SageLagerbuchungPayloadBuilderTests`, `SageBookingEnqueueingStockMovementRepositoryTests`); real NICHT MÖGLICH auf DEV; B2 offen |
| 22, 23 Kreditoren/Debitoren, Meldekarte | Unit (`CreditorsControllerTests`, `DebitorsControllerTests`, `CreditorAssignmentTests`, View-Tests) + **DEV-Browser OK** |
| 25 Übergaben | Unit (`SageUebergabeTests`, `LagerplatzSyncServiceTests`) + **DEV-Browser/DB OK** (Anlage → Offen + Outbox); Übernahme durch Abgleich Manual-UAT |
| 26, 27 Katalog, DisplayNames, DI | Unit (Drift-Guard, DI-Auflösung) + DEV `/ServiceSettings` OK |

### 4. Geänderte Bestandstests (gegen `d91b1d1d`)
| Test | Einordnung |
|---|---|
| `LagerplatzSyncServiceTests.Build` (+ `ISyncErrorNotifier`-Mock) | Typ-Änderung (Ctor, G10) |
| `LagerplatzSyncServiceTests` neuer Fall E9 | neues Verhalten (AK 25), additiv |
| `SageImportServiceTests.Build` (+ `SageArtikelReader`, Ctx) | Typ-Änderung (E3a) |
| `SageLagerbuchungPayloadBuilderTests`: `InlineData(Umbuchung)` aus „NonBookable“ entfernt | gewollte Verhaltensänderung (AK 21, F9); neue Umbuchung-Fälle |
| `SageSdataFaControllerTests.FakeClient` (+ `QueryAllAsync` wirft) | Typ-Änderung (Interface E0) |
| `ServiceSettingDefinitionsTests` (+13 InlineData) | additiv (AK 26) |
| `SageBookingEnqueueDecisionTests`, `SageBookingEnqueueingStockMovementRepositoryTests`, `StorageLocationsControllerTests`, `ArticleAttributesControllerTests` | nur additive Fälle; Bestandsfälle unverändert |

Kein Fall „sonstige“ – keine Fixture wurde grün gebogen. `SageQuelleTests` und `FaStrukturSdataTests` sind im Zweig neu; ihre Änderungen zwischen den Etappen zählen nicht als Bestandstests.

### 5. Checkliste CLAUDE.md
- 10 Migrationen (`20261106181707` … `20261106202615`) + `SQL/98`–`SQL/107`: Guards je Batch, History-Insert separat. **Idempotenz am DEV belegt:** alle zehn Skripte gegen `AKE_WMS_DEV` nach der EF-Migration – keine PRINT-Zeile, also kein Objekt angelegt → Index-/Constraint-Namen stimmen mit EF überein. FreshInstall: alle 10 `MigrationId` je 2×, alle 10 neuen Tabellen vorhanden. DB-Defaults (`DEFAULT 0`/`'Manual'`) im SQL. FreshInstall auf leerer DB nicht ausgeführt (Manual-UAT).
- Audit-Felder: neue Entitäten `AuditableEntity`, Abgleiche setzen `Modified*`; DEV-Anlage Kreditor mit `CreatedBy`/`CreatedByWindows`.
- Version 1.73.0 in beiden `AppVersion.cs`; Anwender-Changelog ja; **Brain-Changelog fehlt (B5)**.
- `docs/TESTSZENARIEN.md` TS-106.0–106.13 vorhanden; Index-Zeile 106 vom QA-Lauf ergänzt.
- `SQL/AgentJobs/` nicht betroffen; keine Einmal-Skripte.

### 6. DEV-Deploy und Testlauf
- Standort `beide` (Frontmatter). Shell elevated: ja.
- `dev-deploy.ps1 -Standort AKE -Quelle <worktree>` → Exit 0, „HTTP 200, Fusszeile v1.73.0“, DEV-Absicherung angewandt.
- `dev-deploy.ps1 -Standort IDEAL -Quelle <worktree>` → Exit 0, „HTTP 200, Fusszeile v1.73.0“.
- Beide DEV-DBs auf `20261106202615_AddSageStammdatenOutboxAndUebergabeStatus`; alle `Sync:Quelle:*` = `View`, alle neuen Bool-Schalter `false`; kein Schalter verändert.

| Schritt | Ergebnis | Status |
|---|---|---|
| TS-106.0 AKE `/ServiceSettings`: Gruppe „Sage-Abgleich (SData)“, `Sync:Quelle:Lagerplaetze`, `SageLagerbuchungUmbuchungAktiv`; Menü Stammdaten → Kreditoren/Debitoren | sichtbar | OK |
| TS-106.10 IDEAL `/Creditors`: Meldekarte „Dienstleister ohne Kreditor: 1 – GL, 2 Auftragszeilen“ | angezeigt | OK |
| TS-106.10 „Als Kreditor anlegen“ GL, Name `QA Glas & Lack "Test" GmbH` | angelegt, Beschichter Ja, Herkunft WMS, Meldekarte verschwindet; DB-Audit gesetzt | OK |
| TS-106.10 negativ: Kurzname `gl` erneut | Feldfehler „Kurzname 'gl' ist bereits vergeben …“ | OK |
| TS-106.11 AKE `/Debitors` | Liste leer, keine Konsolenfehler | OK |
| TS-106.13 Lagerplatz `QA-UEB-1` anlegen | „Offene Übergaben an Sage“ zeigt ihn mit „Offen“; DB `SageUebergabeStatus=1`, Outbox `StorageLocation/230`, Status 0, Payload-JSON | OK |
| TS-106.8 Sage-Zuordnung (Testdaten per SQL: `SagePpsMitarbeiter` 123 und 777 „Hans Guck“) | 123 → BDE „Guck Hans“ (PersonnelNumber 123) vorgeschlagen, 777 trotz gleichem Namen **nicht**; „Zuordnen“ → Erfolgsmeldung | OK |
| SData-GET AKE-Test (nur lesen, `sagetest01`, `ake_TEST2026;1`): Memo-Lookup-Kodierung | korrekt ausgewertet (B3) | OK |
| SData-GET AKE-Test: Paging `startIndex` | 0-basiert, Client erwartet 1 (B1) | **FEHLER** |
| TS-106.1–106.7, 106.9 (Umbuchung), 106.12: Sync-Läufe View/SData/Vergleich/Ausfall | braucht `Sync:*Enabled` bzw. `SageLagerbuchungAktiv` | NICHT MÖGLICH (DEV-Grenze) |

DEV-Testdaten bleiben stehen (IDEAL_WMS_DEV): Kreditor `GL`, Lagerplatz `QA-UEB-1` + Outbox, `SagePpsMitarbeiter` 123/777, BDE-Mitarbeiter Id 1 → Sage-Nr. 123.

### Deploy (aus dem echten Diff; gilt nach B1-Fix)
- `deploy.web: true` (IdealAkeWms/), `deploy.service: true` (IDEALAKEWMSService/), `deploy.migration: true` (10 neue Migrationen unter `IdealAkeWms/Migrations/`).
- Reihenfolge: DB-Backup → `SQL/98` … `SQL/107` in **numerischer** Reihenfolge (98, 99, 100 … 107 – nicht lexikographisch, G17) → Service stoppen → Web + Service publizieren → Service starten. Alle Skripte additiv.
  - `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  - `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- Ablauf Mensch: Publish aus dem Worktree → Testsystem → Test → Merge. Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.

### Offene manuelle Tests (für den Menschen, nach B1-Fix und erneuter QA)
1. B1 beheben lassen, QA wiederholen; danach am Testsystem je Bereich `Vergleich` → Protokoll „Sage-Quellenvergleich“ (TS-106.1–106.7): Lagerplätze, Bestand, Artikel, FA-Struktur 835, Arbeitsplätze (AK 8, 10, 11, 13, 17).
2. Je Bereich `SData` + Rückfall `View`: SData-only-Felder unverändert (DB vor/nach), keine Korrekturbuchungen (AK 9, 10, 11).
3. Ausfall (falsche `SData:BaseUrl`/Passwort, HTML-Antwort): Bereich endet mit Fehler, keine Deaktivierung/Nullsetzung, Mail laut G10 (AK 6, 7).
4. Erster Lagerplatz-Lauf SData: Plätze nicht massenhaft deaktiviert (B4), `StorageAreas` gefüllt.
5. E4b: `ProductionOrderPositions` (836 / BelPosID 10641 / Platz 11), zweiter Lauf ersetzt vollständig (AK 15).
6. E7: Lookup `SData` liefert `true` für eine gebuchte Bewegung (B3); SData nicht erreichbar → Eintrag bleibt, wird nicht neu gesendet (AK 20).
7. B2 entscheiden, dann Umbuchung Sage→Sage / Sage→WMS / WMS→Sage / WMS→WMS mit `SageLagerbuchungAktiv` + `SageLagerbuchungUmbuchungAktiv` am Testsystem (AK 21).
8. E6b/E8c: `Sync:PpsMitarbeiterEnabled`, `Sync:StillstandsgruendeEnabled`, `Sync:KreditorenEnabled`, `Sync:DebitorenEnabled`, `Sync:ArtikelLieferantEnabled` je einmal (AK 18, 19, 24; 00722001 ↔ K31013).
9. E9: Platz `QA-UEB-…` in Sage anlegen, Lagerplatz-Lauf → „übergeben“, `Source = Sage`, Info im Protokoll (AK 25).
10. AKE-Regression mit Default-Schaltern (TS-106.12): Count-Keys Artikel/Lagerplatz/Lagerbestand wie vor dem Deploy, kein `SageQuellenvergleich` (AK 1).
11. `SQL/98`–`107` + FreshInstall am Testsystem (zweiter Lauf ohne Änderung).
12. P-S1/P-S2/P-R1/P-F1/P-F2/P-A1 am IDEAL-Endpunkt.
13. B5 Brain-Pflichten vor dem Merge.

## QA-Wiederholung 2026-11-06 (qa-agent) — Ergebnis: Testbereit

> [!success] Kein Blocker mehr offen
> B1 und B2 behoben (Commit `c8c6b210`), B1 am AKE-Test mit dem echten Client belegt, B2 auch in der echten SQL-Übersetzung belegt.
> B5 im Hauptcheckout nachgetragen (noch uncommittet, Gerald committet). Gate 2 (Mensch: Testsystem-Lauf, dann Merge) bleibt offen.

Diff-Basis der Wiederholung: `e6821035..c8c6b210` (8 Dateien, +32/−9); Gesamtdiff `d91b1d1d..c8c6b210`. Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, v1.73.0.

### 1. Build und Tests (im Worktree, Stand `c8c6b210`)
- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, Warnungen nur Bestand (NU1902/NU1903 Paket-Advisories, CS8321, CS8601 in `BdeTerminalAuftraegeTests.cs:23` – Datei nicht im Fix).
- `dotnet test`: **IdealAkeWms.Tests 2392 bestanden / 1 übersprungen / 0 Fehler** (+1 = `ShouldEnqueue_UmbuchungMitFaBezug_Nie_EinAusUnberuehrt`); **IDEALAKEWMSService.Tests 484 / 0 / 0**.

### 2. Code-Review des Fix-Commits (selbst, synchron)
Geprüft: `SageSdataReadClient.QueryAllAsync` (Start, Fortschaltung `startIndex += seite.Count`, Abbruch bei `zeilen.Count >= gesamt`, leere Seite vor `$totalResults` wirft weiter), alle `startIndex`-Fundstellen in Code und Tests (nur noch `SageSdataReadClient.cs:158/179/183/201` und `SageSdataPagingTests.cs`), alle Aufrufer von `ShouldEnqueue`/`EnqueueAsync`/`EnqueueMissingAsync`, die Überladungen ohne `hatFaBezug` (`SageBookingEnqueueDecision.cs:75-80` setzen `umbuchungAktiv: false` → Umbuchung dort ohnehin nie; der Default `hatFaBezug = false` ist also kein stiller Durchlass), die EF-Abfrage `SageBookingQueueRepository.cs:139-155` und die angepassten Tests.

| # | Status | Nachweis |
|---|---|---|
| **B1** | **Erledigt** | `SageSdataReadClient.cs:179` startet mit `startIndex = 0`. Tests: `Paging_DreiSeiten_BisTotalResults` erwartet jetzt 0, 2, 4 und die URL mit `startIndex=0`; der Stub liefert IDs ab `StartIndex+1` (0-basierter Server simuliert) – Anpassung an die am Server belegte Logik, kein grün gebogener Fall. **Am AKE-Test belegt** (nur GET, sagetest01 / ake_TEST2026;1, echter `SageSdataReadClient` + echte Reader aus dem Worktree-Build in einem Wegwerf-Harness im Scratchpad; kein Schalter umgestellt): Tabelle in Abschnitt 6. |
| **B2** | **Erledigt** (Entscheidung Gerald: Spec F9 wörtlich) | Eine Regel `SageBookingEnqueueDecision.IstUmbuchungMitFaBezug` (`SageBookingEnqueueDecision.cs:71`), im Decorator mit `hatFaBezug: !IsNullOrWhiteSpace(ProductionOrder)` (`SageBookingEnqueueingStockMovementRepository.cs:65-66`), im Nacherfassungs-Lauf als EF-Bedingung (`SageBookingQueueRepository.cs:149-151`, Kommentar verweist auf die Regel). Ein-/Ausbuchungen mit FA unverändert. **Echte SQL-Übersetzung gegen `AKE_WMS_DEV` belegt** (Transaktion, danach ROLLBACK): `… AND ([s].[MovementType] <> 2 OR [s].[ProductionOrder] IS NULL OR LTRIM(RTRIM([s].[ProductionOrder])) = N'') …`. Ergebnis: Umbuchung+FA **nicht** eingereiht; Umbuchung ohne FA und mit FA = nur Leerzeichen eingereiht (deckungsgleich mit `IsNullOrWhiteSpace` für Leerzeichen); Ausbuchung+FA eingereiht. Danach `SageBookingQueueItems` 16 wie vorher, 0 Test-Bewegungen. |
| B3 | Erledigt | Lookup am AKE-Test mit dem echten `SdataBuchungLookupReader`: `SM#19367#` → **true**, `SM#1936#` → false (kein Präfix-Treffer), `SM#999999#` → false. Das `TODO(UAT E7)` in `SdataBuchungLookupReader.cs:32-34` ist inhaltlich erledigt; der Kommentar kann beim nächsten Code-Lauf weg (kein Blocker). |
| B4 | Akzeptiert (unverändert) | Lagerplatz-Reader am AKE-Test: 20 aktive Lagerorte, 3284 Zeilen, kein leeres Ergebnis. Erster SData-Lauf am Testsystem trotzdem DB vor/nach prüfen. |
| **B5** | **Erledigt** (Hauptcheckout, uncommittet) | Vorhanden: [[2026-11-06-v1-73-0-sage-sdata-stammdaten-sync]], [[0019-quellen-weiche-view-sdata-je-abgleich]], Nachtrag G17 in [[0004-migrations-und-sql-disziplin]], [[fallstricke]] „`startIndex` ist 0-basiert“, `codebase/services.md`/`datenmodell.md`/`controller.md`, [[glossar]], [[feature-map]] (QA setzt die beiden v1.73.0-Zeilen auf Testbereit), [[2026-11-06-sage-sdata-folgearbeit]]. |
| B6 | Info (unverändert) | – |
| N1 | Info, neu | `PpsArbeitsplaetzeInfo` liefert am AKE-Test genau **1** aktive Zeile (vorher durch B1 verschluckt). Ob das dem View-Bestand entspricht, klärt der Vergleichslauf am Testsystem (Checkliste 1). Der Arbeitsgang-Abgleich setzt über SData ohnehin wegen P-A1 aus. |
| N2 | Info, neu | Kein Decorator-Test, der `ProductionOrder` bis `hatFaBezug` durchreicht (nur Regel- und Sweep-Test). Eine Zeile Verdrahtung, im Review geprüft; kein Befund. |

Defektklassen im Fix: keine View/kein JS berührt; kein stiller Fallback; die FA-Regel steht an zwei Stellen (C# und EF) – bewusst, mit Querverweis, und am echten SQL Server für Leerzeichen deckungsgleich belegt (Tabulatoren schneidet `LTRIM/RTRIM` nicht – praktisch irrelevant).

### 3. Nachweisart je AK (Änderungen gegenüber dem ersten Lauf)
| AK | Nachweis |
|---|---|
| 5 Paging/orderBy/Sperren | Unit (`SageSdataPagingTests`, jetzt 0-basiert) **+ am AKE-Test mit echtem Client belegt** (5er-Seiten = eine 500er-Seite, identische Schlüssel) |
| 2, 3, 8–11, 13–17, 19, 24 | Unit; Lesen über SData am AKE-Test für Lagerplätze, Bestand, Arbeitsplätze **OK**; der Vergleichs-/SData-Lauf des Dienstes bleibt **Manual-UAT** (auf DEV gesperrt: `Sync:*Enabled` aus, `SData:BaseUrl` leer, View-Seite liest Sage-SQL) |
| 20 Lookup | Reader am AKE-Test **OK** (true/false); Worker-Verhalten Manual-UAT |
| 21 Umbuchung-Queue | Unit + **echte SQL-Übersetzung des Sweeps am DEV (Rollback) OK**; Senden an Sage Manual-UAT |

Alle übrigen AK wie im ersten Lauf.

### 4. Geänderte Bestandstests (Fix-Commit)
| Test | Einordnung |
|---|---|
| `SageSdataPagingTests.Paging_DreiSeiten_BisTotalResults`, `Specific_Vertrag_ImPfad` | gewollte Verhaltensänderung (B1, AK 5): Erwartung 1-basiert → 0-basiert, am Server belegt |
| `SageBookingEnqueueingStockMovementRepositoryTests.EnqueueMissing_UmbuchungNurMitSchalter_NachPlatzFlags_NieSageKorrektur` | additiver Fall `mitFa` (B2, AK 21); Erwartungen 1/3 und Erwartungsliste unverändert → beweist den Ausschluss |
| `SageBookingEnqueueDecisionTests` | neuer Fall, Bestandsfälle unverändert |

Kein Fall „sonstige“.

### 5. Checkliste
Unverändert gegenüber dem ersten Lauf (keine neue Migration, keine neue SQL-Datei, Version bleibt 1.73.0). `docs/TESTSZENARIEN.md` um TS-106.9.o ergänzt; Indexzeile 106 deckt TS-106.0–106.13 ab. Brain-Changelog vorhanden (B5).

### 6. DEV-Deploy und Testlauf
- Standort `beide`. Shell elevated: ja.
- `dev-deploy.ps1 -Standort AKE -Quelle <worktree>` → Exit 0, „HTTP 200, Fusszeile v1.73.0“, DEV-Absicherung angewandt.
- `dev-deploy.ps1 -Standort IDEAL -Quelle <worktree>` → Exit 0, „HTTP 200, Fusszeile v1.73.0“.
- Beide DEV-DBs: letzte Migration `20261106202615_AddSageStammdatenOutboxAndUebergabeStatus`; nach allen Tests alle `Sync:Quelle:*` = `View`, `SageLagerbuchungAktiv`/`SageLagerbuchungUmbuchungAktiv`/`ErrorNotification:Enabled`/`Notifications:MeldebestandEnabled` = `false`, kein `Sync:*Enabled` = `true`. **Kein Schalter wurde umgestellt** – der Nachweis lief über die Reader direkt, nicht über einen Dienst-Lauf; es gab nichts zurückzustellen.

| Schritt (lesend, AKE-Test bzw. DEV mit Rollback) | Ergebnis | Status |
|---|---|---|
| `LagerplatzbuchungenInfo where Memo like 'IdealAkeWms%'`, Seitengröße 5 vs. 500 | 12 Zeilen in 3 Seiten vs. 12 in 1, `$totalResults` 12, Schlüssel identisch, 12 verschieden, erste `865807` (= Wert ohne startIndex aus dem ersten Lauf) | OK |
| `LagerorteInfo where Mandant eq 1`, Seitengröße 5 vs. 500 | 36 in 8 Seiten vs. 36 in 1, identisch, erste `Akademie` (vorher 35 von 36) | OK |
| `SdataLagerplatzReader.LesenAsync` (LagerorteInfo + LagerplaetzeInfo) | 3284 Zeilen aktiver Lagerorte, 20 Lagerorte, keine `SdataLeseException` | OK |
| `SdataBestandReader.LesenAsync` | 4544 Bestandszeilen, keine Ausnahme | OK |
| `SdataArbeitsplatzReader.LesenAsync` | 1 aktive Zeile, `ArbeitsschrittGeliefert=false` (P-A1) – siehe N1 | OK |
| `SdataBuchungLookupReader.ExistsAsync` 19367 / 1936 / 999999 | true / false / false | OK |
| B2: `EnqueueMissingAsync(umbuchungAktiv: true)` gegen `AKE_WMS_DEV` (Transaktion + Rollback) | Umbuchung+FA ausgeschlossen, übrige wie erwartet, Queue danach unverändert | OK |
| TS-106.9.o über Oberfläche + Worker | braucht `SageLagerbuchungAktiv` | NICHT MÖGLICH (DEV-Grenze) |
| Dienst-Lauf `Vergleich`/`SData` (TS-106.1–106.7, 106.12) | braucht `Sync:*Enabled`; `SData:BaseUrl` auf DEV leer; View-Seite liest Sage-SQL | NICHT MÖGLICH (DEV-Grenze) |
| Artikel-/FA-Struktur-Reader | nicht gelaufen (SData gesperrt bis P-R1 bzw. P-F2) | NICHT MÖGLICH (fachlich gesperrt) |

Browser-Schritte des ersten Laufs (TS-106.0, 106.8, 106.10, 106.11, 106.13) nicht wiederholt: der Fix berührt keine View, kein JS, keinen Controller; Deploy-Prüfung (HTTP 200 + Fußzeile) grün.

### Deploy
Der Abschnitt „Deploy (aus dem echten Diff …)“ des ersten Laufs gilt jetzt unverändert: web + service + migration; DB-Backup → `SQL/98`…`SQL/107` numerisch → Service stoppen → Web + Service publizieren → Service starten. Publish aus dem Worktree → Testsystem → Test → Merge; nach dem Merge nur erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.

### Offene manuelle Tests (für den Menschen, Testsystem)
1. Je Bereich `Vergleich` → Protokoll „Sage-Quellenvergleich“ (TS-106.1–106.7): Lagerplätze, Bestand, Artikel, FA-Struktur 835, Arbeitsplätze (AK 8, 10, 11, 13, 17); dabei N1 (Arbeitsplätze: 1 Zeile über SData) gegen die View prüfen.
2. Je Bereich `SData` + Rückfall `View`: SData-only-Felder unverändert (DB vor/nach), keine Korrekturbuchungen (AK 9, 10, 11).
3. Ausfall (falsche `SData:BaseUrl`/Passwort, HTML-Antwort): Bereich endet mit Fehler, keine Deaktivierung/Nullsetzung, Mail laut G10 (AK 6, 7).
4. Erster Lagerplatz-Lauf SData: Plätze nicht massenhaft deaktiviert (B4), `StorageAreas` gefüllt.
5. E4b: `ProductionOrderPositions` (836 / BelPosID 10641 / Platz 11), zweiter Lauf ersetzt vollständig (AK 15).
6. E7 im Worker: Lookup `SData` erkennt eine gebuchte Bewegung (am Reader belegt); SData nicht erreichbar → Eintrag bleibt, wird nicht neu gesendet (AK 20).
7. Umbuchung Sage→Sage / Sage→WMS / WMS→Sage / WMS→WMS mit `SageLagerbuchungAktiv` + `SageLagerbuchungUmbuchungAktiv` (AK 21) und TS-106.9.o (Kommissionier-Umbuchung mit FA und Bewegungsformular mit FA → kein Queue-Eintrag, auch nicht nach dem Nacherfassungs-Lauf).
8. E6b/E8c: `Sync:PpsMitarbeiterEnabled`, `Sync:StillstandsgruendeEnabled`, `Sync:KreditorenEnabled`, `Sync:DebitorenEnabled`, `Sync:ArtikelLieferantEnabled` je einmal (AK 18, 19, 24; 00722001 ↔ K31013).
9. E9: Platz `QA-UEB-…` in Sage anlegen, Lagerplatz-Lauf → „übergeben“, `Source = Sage`, Info im Protokoll (AK 25).
10. AKE-Regression mit Default-Schaltern (TS-106.12): Count-Keys Artikel/Lagerplatz/Lagerbestand wie vor dem Deploy, kein `SageQuellenvergleich` (AK 1).
11. `SQL/98`–`107` + FreshInstall am Testsystem (zweiter Lauf ohne Änderung).
12. P-S1/P-S2/P-R1/P-F1/P-F2/P-A1 am IDEAL-Endpunkt.
13. Brain-Änderungen (B5 + diese QA) committen; danach Merge (Gate 2).
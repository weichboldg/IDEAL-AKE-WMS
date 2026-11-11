# Nachlauf-Manifest fuer Update-IdealAkeWms.ps1 (Spec 2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec, ADR 0022).
# Nur ASCII (PS 5.1 liest .psd1 ohne BOM als ANSI). Gelesen per Import-PowerShellDataFile: nur Literale.
#
# Felder: Id, Version (eingefuehrt mit), Typ (Schema|Daten|Einmalig|Einstellung|Leitstand|Info|Ordner), Firma (AKE|IDEAL|beide),
#   System (optional, Liste: nur diese Systeme der Firma), Phase (Vorher|Schema|Haltepunkt|Nachher), Ausfuehrung (automatisch|Trockenlauf|anzeigen),
#   Datei (relativ zum SQL-Ordner), MigrationId (nur Schema), Braucht (Ids, deren MigrationId in der History stehen muss),
#   Voraussetzung (Text), Spur (lesendes SQL, >0 = lief schon), Pruefabfrage (lesendes SQL, Ergebnis wird angezeigt),
#   Text (Platzhalter {Sqlcmd} {Server} {DB} {SqlOrdner} {LogPfad} {Web} {Dienst}), Quelle (Changelog).
# Typ Ordner (Schritt 5a, bei jedem Lauf): Pfad, RechtAuf (Ordner, auf den das Recht vererbt gesetzt wird), Identitaet (Web|Dienst),
#   Recht (Lesen|LesenAusfuehren|Aendern), Pflicht ($true = Abbruch, wenn nicht herstellbar), Herstellen ($true = anlegen + icacls).
#
# Pflicht: Jede Spec mit SQL-Skript, Einmal-Skript oder Hand-Schritt traegt hier ihren Eintrag nach (Drift-Test NachlaufManifestDriftTests).
# Schema-Eintraege in Deploy-Reihenfolge (ADR 0004 Nachtrag), NIE nach Dateisortierung.
# Bewusst NICHT im Manifest: SQL/Einmalig/2026-09-28_Hotfix-1.30.1_ResetAutosaveZeroQuantityPicked.sql (Freigabe-Antwort 4, Gerald
# 2026-11-11: auf keinem System gelaufen; seit v1.30.1 ist eine getippte 0 eine Bestaetigung, ein Lauf heute loeschte echte Eingaben).
@{
Eintraege = @(

# ---------------------------------------------------------------- Schema (Phase Schema, automatisch mit Bestaetigung) ----------
@{ Id='SQL/89'; Version='1.31.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='89_AddFaHierarchy.sql'; MigrationId='20260807105825_AddFaHierarchy'; Voraussetzung='additiv'; Quelle='2026-08-10-v1-31-0-ideal-teile-1-5' }
@{ Id='SQL/90'; Version='1.32.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='90_InvertProductionOrderHierarchy.sql'; MigrationId='20260814105526_InvertProductionOrderHierarchy'
   Voraussetzung='daten-konvertierend: Backup, sysjobs-Abfrage, Dienst gestoppt (Sonderregel 90)'; Quelle='2026-08-17-v1-32-0-ideal-teil-7' }
@{ Id='SQL/91'; Version='1.40.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='91_AddArticleMatchcode.sql'; MigrationId='20260915094646_AddArticleMatchcode'; Voraussetzung='ersetzt die nie deployte ProductionOrder.Matchcode aus v1.38.0'; Quelle='2026-09-15-v1-40-0-matchcode-artikelstamm' }
@{ Id='SQL/92'; Version='1.42.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='92_AddUserDefaultFilterBomKommissionierziel.sql'; MigrationId='20260921131329_AddUserDefaultFilterBomKommissionierziel'; Voraussetzung='additiv'; Quelle='2026-09-21-v1-42-0-stueckliste-kommissionierziel-filter' }
@{ Id='SQL/93'; Version='1.44.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='93_AddProductionWorkplaceSageFields.sql'; MigrationId='20260922083259_AddProductionWorkplaceSageFields'; Voraussetzung='additiv'; Quelle='2026-09-22-v1-44-0-ideal-bde-arbeitsgaenge-werkbank-anlage' }
@{ Id='SQL/94'; Version='1.50.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='94_AddBeschichtungsauftrag.sql'; MigrationId='20261012073551_AddBeschichtungsauftrag'; Voraussetzung='additiv'; Quelle='2026-10-12-v1-50-0-beschichtungsauftrag-objekt' }
@{ Id='SQL/95'; Version='1.54.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='95_RenameOperationConfigsAndAddAudit.sql'; MigrationId='20261012140548_RenameOperationConfigsAndAddAudit'
   Voraussetzung='Umbenennung OseonOperationConfigs; nur mit 20260330142035_AddOseonOperationConfig in der History; ABBRUCH in der Ausgabe = Abbruch (Sonderregel 95)'; Quelle='2026-10-12-v1-54-0-arbeitsgang-stammdaten' }
@{ Id='SQL/96'; Version='1.61.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'; Braucht=@('SQL/95')
   Datei='96_AddOperationConfigSourceAndSageMissingSince.sql'; MigrationId='20261016090641_AddOperationConfigSourceAndSageMissingSince'; Voraussetzung='braucht 95'; Quelle='2026-10-16-v1-61-0-arbeitsgang-abgleich-sage-view' }
@{ Id='SQL/97'; Version='1.63.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='97_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung.sql'; MigrationId='20261019092105_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung'; Voraussetzung='additiv'; Quelle='2026-10-19-v1-63-0-bde-stufe-1-werkbaenke-ag-fertigmeldung' }
@{ Id='SQL/98'; Version='1.73.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='98_AddStorageAreasAndStorageLocationSageFields.sql'; MigrationId='20261106181707_AddStorageAreasAndStorageLocationSageFields'; Voraussetzung='98-107 additiv'; Quelle='2026-11-06-v1-73-0-sage-sdata-stammdaten-sync' }
@{ Id='SQL/99'; Version='1.73.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='99_AddArticleSageDetails.sql'; MigrationId='20261106184123_AddArticleSageDetails'; Quelle='2026-11-06-v1-73-0-sage-sdata-stammdaten-sync' }
@{ Id='SQL/100'; Version='1.73.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='100_AddArticleAttributeValueTextValue.sql'; MigrationId='20261106185149_AddArticleAttributeValueTextValue'; Quelle='2026-11-06-v1-73-0-sage-sdata-stammdaten-sync' }
@{ Id='SQL/101'; Version='1.73.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='101_AddProductionOrderPositions.sql'; MigrationId='20261106191438_AddProductionOrderPositions'; Quelle='2026-11-06-v1-73-0-sage-sdata-stammdaten-sync' }
@{ Id='SQL/102'; Version='1.73.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='102_AddProductionWorkplaceSdataFields.sql'; MigrationId='20261106192342_AddProductionWorkplaceSdataFields'; Quelle='2026-11-06-v1-73-0-sage-sdata-stammdaten-sync' }
@{ Id='SQL/103'; Version='1.73.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='103_AddSagePpsMitarbeiterAndStillstandsgruende.sql'; MigrationId='20261106193321_AddSagePpsMitarbeiterAndStillstandsgruende'; Quelle='2026-11-06-v1-73-0-sage-sdata-stammdaten-sync' }
@{ Id='SQL/104'; Version='1.73.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='104_AddCreditors.sql'; MigrationId='20261106195956_AddCreditors'; Quelle='2026-11-06-v1-73-0-sage-sdata-stammdaten-sync' }
@{ Id='SQL/105'; Version='1.73.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='105_AddDebitors.sql'; MigrationId='20261106200713_AddDebitors'; Quelle='2026-11-06-v1-73-0-sage-sdata-stammdaten-sync' }
@{ Id='SQL/106'; Version='1.73.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='106_AddArticleSuppliers.sql'; MigrationId='20261106201439_AddArticleSuppliers'; Quelle='2026-11-06-v1-73-0-sage-sdata-stammdaten-sync' }
@{ Id='SQL/107'; Version='1.73.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='107_AddSageStammdatenOutboxAndUebergabeStatus.sql'; MigrationId='20261106202615_AddSageStammdatenOutboxAndUebergabeStatus'; Quelle='2026-11-06-v1-73-0-sage-sdata-stammdaten-sync' }
@{ Id='SQL/109'; Version='1.87.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='109_AddStockMovementProductionOrderId.sql'; MigrationId='20261107190419_AddStockMovementProductionOrderId'; Voraussetzung='Backup'; Quelle='2026-11-08-v1-87-0-lagerbuchungen-fa-feld' }
@{ Id='SQL/112'; Version='1.84.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='112_AddSagePurchaseOrderLines.sql'; MigrationId='20261107172542_AddSagePurchaseOrderLines'; Quelle='2026-11-08-v1-84-0-wareneingang-aus-sage-bestellungen' }
@{ Id='SQL/113'; Version='1.84.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='113_AddPurchaseOrderReceipts.sql'; MigrationId='20261107173341_AddPurchaseOrderReceipts'; Quelle='2026-11-08-v1-84-0-wareneingang-aus-sage-bestellungen' }
@{ Id='SQL/133'; Version='1.88.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='133_AddPickingAreasAndOrders.sql'; MigrationId='20261107193219_AddPickingAreasAndOrders'; Voraussetzung='additiv, Seed; Backup'; Quelle='2026-11-08-v1-88-0-kommissionierauftraege' }
@{ Id='SQL/140'; Version='1.85.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='140_AddArtikelMengeneinheiten.sql'; MigrationId='20261107182445_AddArtikelMengeneinheiten'; Quelle='2026-11-08-v1-85-0-artikel-lieferanten-nachbesserung' }
@{ Id='SQL/141'; Version='1.89.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='141_AddPickingOrderAltbestandPruefen.sql'; MigrationId='20261108071811_AddPickingOrderAltbestandPruefen'; Voraussetzung='vor dem Altbestand-Skript'; Quelle='2026-11-08-v1-89-0-kommissionierauftraege-nachbesserung' }

# ---------------------------------------------------------------- Daten und Einmal-Skripte (nie scharf durch das Skript) -------
@{ Id='Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand'; Version='1.88.0'; Typ='Einmalig'; Firma='beide'; Phase='Haltepunkt'; Ausfuehrung='Trockenlauf'
   Datei='Einmalig\2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand.sql'; Braucht=@('SQL/133','SQL/141')
   Voraussetzung='Web gestoppt, SQL/133 und SQL/141 da, DB-Backup'
   Spur='IF OBJECT_ID(N''dbo.PickingOrders'', N''U'') IS NULL SELECT 0 ELSE SELECT COUNT(*) FROM dbo.PickingOrders WHERE CreatedBy = N''Migration-KO-Altbestand'''
   Text='Trockenlauf (Datei unveraendert, @Trockenlauf = 1) liefert Zaehlung und Liste "Altbestand pruefen" ins Protokoll. Gerald gibt die Zaehlung frei. Scharf: Kopie mit @Trockenlauf = 0 in SSMS ausfuehren (Web bleibt bis dahin gestoppt). An IDEAL mit 0 Treffern ebenso (Freigabe-Antwort 3).'
   Quelle='2026-11-08-v1-88-0-kommissionierauftraege; 2026-11-08-v1-89-0-kommissionierauftraege-nachbesserung' }
@{ Id='SQL/108'; Version='1.79.0'; Typ='Daten'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Datei='108_RemoveAppSettingsRueckmeldungOhneLeser.sql'
   Pruefabfrage='SELECT [Key],[Value] FROM [dbo].[AppSettings] WHERE [Key] IN (N''OseonRueckmeldungAktiv'', N''SageRueckmeldungAktiv'', N''StammdatenADGruppe'')'
   Text='P-4: Werte oben notieren (Protokoll). Dann von Hand: {Sqlcmd} -i "{SqlOrdner}\108_RemoveAppSettingsRueckmeldungOhneLeser.sql". Loescht Settings-Zeilen ohne Leser, idempotent.'
   Quelle='2026-11-08-v1-79-0-bereinigungen-nach-buendel; 2026-11-08-v1-94-0-menueleiste-umbruch-kontrast' }
@{ Id='SQL/110'; Version='1.87.0'; Typ='Daten'; Firma='beide'; Phase='Nachher'; Ausfuehrung='Trockenlauf'; Braucht=@('SQL/109')
   Datei='110_BackfillStockMovementProductionOrderId.sql'
   Voraussetzung='nach Web- und Dienst-Start'
   Text='Trockenlauf-Ergebnis liegt als Datei im Log-Ordner. Scharf von Hand mit einer Kopie (@Trockenlauf = 0): {Sqlcmd} -i "<Kopie>". Danach zweiter Trockenlauf muss 0 liefern.'
   Quelle='2026-11-08-v1-87-0-lagerbuchungen-fa-feld' }
@{ Id='Einmalig/2026-10-07_v1.47.4_RemoveParentWorkOperationsFromChildSubFaTokens'; Version='1.47.4'; Typ='Einmalig'; Firma='IDEAL'; System=@('Test'); Phase='Nachher'; Ausfuehrung='anzeigen'
   Datei='Einmalig\2026-10-07_v1.47.4_RemoveParentWorkOperationsFromChildSubFaTokens.sql'
   Voraussetzung='nach dem Dienst-Deploy >= v1.47.4'
   Text='Laut Gerald 2026-11-11 auf keinem System gelaufen: an IDEAL-Test mit diesem Deploy ausfuehren (kein Trockenlauf-Schalter, DELETE in Transaktion, Skriptkopf lesen): {Sqlcmd} -i "{SqlOrdner}\Einmalig\2026-10-07_v1.47.4_RemoveParentWorkOperationsFromChildSubFaTokens.sql". Meldeliste an den Fachbereich. Entfaellt an IDEAL-Prod (Neuinstallation v1.71.1).'
   Quelle='2026-10-07-v1-47-4-arbeitsgang-erkennung-kind-sub-fa' }
@{ Id='Einmalig/2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel'; Version='1.60.0'; Typ='Einmalig'; Firma='IDEAL'; System=@('Test'); Phase='Nachher'; Ausfuehrung='anzeigen'
   Datei='Einmalig\2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel.sql'
   Voraussetzung='Sync:ArticlesViewName gesetzt, ein Artikel-Lauf, P7 mit Treffern, @Behalten-Liste'
   Spur='SELECT CASE WHEN OBJECT_ID(N''dbo.Articles_Bereinigung00er_Sicherung'') IS NULL THEN 0 ELSE 1 END'
   Text='Laut Gerald 2026-11-11 auf keinem System gelaufen: nur IDEAL-Test und nur wenn P7 Treffer zeigt. Zuerst Listenmodus @Loeschen = 0 in SSMS, dann entscheidet Gerald. Entfaellt an IDEAL-Prod.'
   Quelle='2026-10-15-v1-60-0-ideal-sage-views-umstellung' }

# ---------------------------------------------------------------- Ordner und Rechte (Schritt 5a, jeder Lauf) ------------------
@{ Id='v1.96.0-FotosOrdner'; Version='1.96.0'; Typ='Ordner'; Firma='beide'; Phase='Vorher'; Ausfuehrung='automatisch'
   Pfad='{Web}\App_Data\Fotos\Kommissionierung'; RechtAuf='{Web}\App_Data'; Identitaet='Web'; Recht='Aendern'; Pflicht=$true; Herstellen=$true
   Text='Kommissionierfotos liegen seit v1.30.4/v1.96.0 in App_Data; ohne Schreibrecht startet die App nicht.'
   Quelle='2026-11-11-v1-96-0-vorwaerts-merge-main-hotfixes' }

# ---------------------------------------------------------------- Vorher (Anzeige vor jeder Aenderung) -------------------------
@{ Id='v0-Backup'; Version='0.0.0'; Typ='Info'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='DB-Backup vor dem Deploy (Pflicht bei 90, 95, 109/110, 133/Altbestand, ADR 0004); das Skript bietet es in Schritt 2 an.'; Quelle='ADR 0004' }
@{ Id='v1.32.0-sysjobs'; Version='1.32.0'; Typ='Info'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='Agent-Jobs gegen ProductionOrders pruefen; das Skript zeigt die sysjobs-Abfrage vor SQL/90.'; Quelle='2026-08-17-v1-32-0-ideal-teil-7' }
@{ Id='v1.39.0-Edge'; Version='1.39.0'; Typ='Info'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='Microsoft Edge auf dem Web-Server, ausfuehrbar unter der App-Pool-Identitaet, %TEMP%\IdealAkeWms-Pdf beschreibbar; Edge-Version notieren (Schritt 5a prueft Edge).'
   Quelle='2026-09-14-v1-39-0-ideal-pdf-erzeugung-fahierarchy-druck; 2026-10-14-v1-59-0-briefkopf-fusszeile' }
@{ Id='v1.49.0-Branding'; Version='1.49.0'; Typ='Info'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='Ordner Branding\ im Programmordner (nicht wwwroot\), Leserecht App-Pool, Einstellung BriefkopfVorlagePfad. Das Skript prueft, kopiert ihn nie.'
   Quelle='2026-10-09-v1-49-0-briefkopf-beschichtungsauftrag; 2026-10-14-v1-59-0-briefkopf-fusszeile' }
@{ Id='v1.58.0-P123'; Version='1.58.0'; Typ='Info'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='P-1 gespeicherte Standardfilter, P-2 Kollation DB und je Spalte (Pflicht), P-3 Server-Kultur; Abfragen in der Suchsyntax-Spec.'
   Quelle='2026-10-13-v1-58-0-suchsyntax-spaltenfilter-etappe-a; 2026-10-13-v1-58-1-suchsyntax-spaltenfilter-etappe-b' }
@{ Id='v1.61.0-P5'; Version='1.61.0'; Typ='Info'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='P5 je Standort (Arbeitsgang-Abgleich), Abfrage in der Spec.'; Quelle='2026-10-16-v1-61-0-arbeitsgang-abgleich-sage-view' }
@{ Id='v1.71.0-AppsettingsProd'; Version='1.71.0'; Typ='Info'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='appsettings.Production.json mit Abschnitt SageLagerbuchung im Web-Ordner (Schritt 5a prueft die Datei, ueberschreibt sie nie).'; Quelle='2026-10-30-v1-71-0-sage-sdata-fa-ansicht' }
@{ Id='v1.84.0-Einkauf'; Version='1.84.0'; Typ='Info'; Firma='AKE'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='Einkauf informieren: kein WE-Beleg mit Lagerbuchung in Sage (Doppelbestand).'; Quelle='2026-11-08-v1-84-0-wareneingang-aus-sage-bestellungen' }
@{ Id='v1.93.0-Zaehlung'; Version='1.93.0'; Typ='Info'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='Zaehlabfrage Meldebestand je Standort vor PROD (Spec meldebestand-mail).'; Quelle='2026-11-08-v1-93-0-meldebestand-mail-bestandsregel' }
@{ Id='v1.96.0-FotosZaehlen'; Version='1.96.0'; Typ='Info'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='Das Skript zaehlt vor dem Kopieren die Dateien in {Web}\wwwroot\Fotos\Kommissionierung und sucht nach dem Web-Start im Log "Kommissionierfotos verschoben: n, uebersprungen: m" (n + m = Zaehlung).'
   Quelle='2026-11-11-v1-96-0-vorwaerts-merge-main-hotfixes' }

# ---------------------------------------------------------------- Nachher (Nachlauf-Liste) -------------------------------------
@{ Id='v1.31.0-IDEAL-Schalter'; Version='1.31.0'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Sync:HierarchicalFaEnabled, FaHierarchyKommissionierlistenAktiv/...BeschichtungAktiv/...VormontageAktiv; SageConnection; View-Namen ohne Schema-Praefix; RCSI = 1.'; Quelle='2026-08-10-v1-31-0-ideal-teile-1-5' }
@{ Id='v1.40.0-Matchcode'; Version='1.40.0'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Matchcode erscheint erst nach dem naechsten Artikel-Lauf.'; Quelle='2026-09-15-v1-40-0-matchcode-artikelstamm' }
@{ Id='v1.44.0-Werkbank'; Version='1.44.0'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='DryRun; erst Sync:ProductionWorkplaceSyncEnabled, dann Sync:WorkOperationStructureDetectionEnabled.'; Quelle='2026-09-22-v1-44-0-ideal-bde-arbeitsgaenge-werkbank-anlage' }
@{ Id='v1.49.0-Anschrift'; Version='1.49.0'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Firmenanschrift korrigieren ("38, 4840" -> "28, 4810"), falls noch nicht erledigt.'; Quelle='2026-10-09-v1-49-0-briefkopf-beschichtungsauftrag' }
@{ Id='v1.51.0-Rolle'; Version='1.51.0'; Typ='Einstellung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Rollen beschichtungsauftrag (Lesen) und beschichtungsauftrag_schreiben unter /Roles anlegen und zuweisen (kein Seed; Freigabe-Antwort 6). IDEAL hat sie aus der Installation.'
   Quelle='2026-10-12-v1-51-0-beschichtungsauftraege-oberflaeche' }
@{ Id='v1.56.0-Info'; Version='1.56.0'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Anwender-Info: Beschichtung im Menue.'; Quelle='2026-10-13-v1-56-0-menue-beschichtung-hilfe' }
@{ Id='v1.57.0-PT1'; Version='1.57.0'; Typ='Info'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='P-T1 inkl. Kollisionsabfrage vor der AKE-Abnahme.'; Quelle='2026-10-13-v1-57-0-bde-terminal-arbeitsgang-status' }
@{ Id='v1.58.1-Info'; Version='1.58.1'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Anwender-Info: "oder" = ;  Fehlteile !a;b'; Quelle='2026-10-13-v1-58-1-suchsyntax-spaltenfilter-etappe-b' }
@{ Id='v1.60.0-IDEAL-Views'; Version='1.60.0'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='P1-P5b auf Sage, Sync:ArticlesViewName setzen und nicht mehr leeren.'; Quelle='2026-10-15-v1-60-0-ideal-sage-views-umstellung' }
@{ Id='v1.61.0-IDEAL-Altzeilen'; Version='1.61.0'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Alte OperationConfig-Zeilen erst nach dem ersten Abgleich-Lauf loeschen.'; Quelle='2026-10-16-v1-61-0-arbeitsgang-abgleich-sage-view' }
@{ Id='v1.63.0-IDEAL-DryRun'; Version='1.63.0'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='P1-P3/P6 + DryRun vor dem Scharfschalten.'; Quelle='2026-10-19-v1-63-0-bde-stufe-1-werkbaenke-ag-fertigmeldung' }
@{ Id='v1.63.0-Info'; Version='1.63.0'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Werker-Info: Fortsetzen an fremder Werkbank wird abgewiesen.'; Quelle='2026-10-19-v1-63-0-bde-stufe-1-werkbaenke-ag-fertigmeldung' }
@{ Id='v1.64.0-MaxHauptFa'; Version='1.64.0'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='/Settings > BDE: BdeTerminalMaxHauptFa = 5 (wirkt laut README nur IDEAL); AKE: Pruefabfrage Q4.'; Quelle='2026-10-19-v1-64-0-bde-terminal-liste-scan-ohne-ag-prio' }
@{ Id='v1.67.0-IDEAL-Vault'; Version='1.67.0'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='"Vault-Links anzeigen" aus; enaio entscheidet Gerald.'; Quelle='2026-10-22-v1-67-0-vault-enaio-links-konfigurierbar' }
@{ Id='v1.68.0-IDEAL-Qr'; Version='1.68.0'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='QrErstesFeld = Matchcode erst nach TS-101.'; Quelle='2026-10-27-v1-68-0-qr-matchcode' }
@{ Id='v1.69.0-Trennzeichen'; Version='1.69.0'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Bei QrTrennzeichen = , vorher Lagerplatz-Codes mit Komma pruefen.'; Quelle='2026-10-27-v1-69-0-qr-trennzeichen' }
@{ Id='v1.71.0-IDEAL-SData'; Version='1.71.0'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='SData-FA-Ansicht aus (kein SData-Host).'; Quelle='2026-10-30-v1-71-0-sage-sdata-fa-ansicht' }
@{ Id='v1.72.0-Rollendrucker'; Version='1.72.0'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Lagerplatz-Etikett Rollendrucker nur bei Bedarf einschalten.'; Quelle='2026-11-06-v1-72-0-lagerplatz-etikett-rolle-vormontage-felder' }
@{ Id='v1.73.0-SData-Stamm'; Version='1.73.0'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Sync:KreditorenEnabled, Sync:DebitorenEnabled, Sync:ArtikelLieferantEnabled; IDEAL erst mit SData-Endpunkt und Lizenz, erste Laeufe als Vergleich.'; Quelle='2026-11-06-v1-73-0-sage-sdata-stammdaten-sync' }
@{ Id='v1.77.0-Pruefung'; Version='1.77.0'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Aktivitaets-Protokoll "Einstellungspruefung" pruefen (P-E1a-c).'; Quelle='2026-11-07-v1-77-0-einstellungen-auswahlfelder-und-beschreibung' }
@{ Id='v1.79.0-Speichern'; Version='1.79.0'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Einstellungsmasken je Standort einmal speichern (F4; Altwert 1 wird false, Liste im QA-Abschnitt der Spec).'; Quelle='2026-11-08-v1-79-0-bereinigungen-nach-buendel' }
@{ Id='v1.84.0-Wareneingang'; Version='1.84.0'; Typ='Einstellung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Sync:EinkaufBestellungenEnabled an, P-W6 pruefen, erst dann WareneingangBestellungAktiv. IDEAL bleibt aus bis SData-Endpunkt.'; Quelle='2026-11-08-v1-84-0-wareneingang-aus-sage-bestellungen' }
@{ Id='v1.85.0-Einheiten'; Version='1.85.0'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Mengeneinheiten erst nach Artikel- und Artikel-Lieferanten-Lauf; Q-5 offen.'; Quelle='2026-11-08-v1-85-0-artikel-lieferanten-nachbesserung' }
@{ Id='v1.88.0-Leitstand'; Version='1.88.0'; Typ='Leitstand'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Leitstand: "Fehlende Kommissionierauftraege fuer alle freigegebenen FAs erzeugen" direkt nach dem Web-Start; vorher niemand kommissionieren.'; Quelle='2026-11-08-v1-88-0-kommissionierauftraege' }
@{ Id='v1.89.0-Altbestand'; Version='1.89.0'; Typ='Leitstand'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Liste "Altbestand pruefen" je FA abarbeiten, "Altbestand geprueft", erneut erzeugen; Stichprobe TS-120.11.'; Quelle='2026-11-08-v1-89-0-kommissionierauftraege-nachbesserung' }
@{ Id='v1.90.0-F5'; Version='1.90.0'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Strg+F5 an den Geraeten (fa-feld.js).'; Quelle='2026-11-08-v1-90-0-lagerbuchungen-fa-karte-pflicht' }
@{ Id='v1.92.0-Final'; Version='1.92.0'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Pruefabfrage frueher verlorene Final-Kennzeichen (lesend, Spec oseon-relevanzfilter).'; Quelle='2026-11-08-v1-92-0-oseon-relevanzfilter-bde-checkboxen' }
@{ Id='v1.93.0-Mail'; Version='1.93.0'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Erste Meldebestand-Mail nach dem Deploy ist lang (gewollt).'; Quelle='2026-11-08-v1-93-0-meldebestand-mail-bestandsregel' }
@{ Id='v1.96.0-FotosRest'; Version='1.96.0'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Bei "uebersprungen: m > 0": Rest-Dateien in {Web}\wwwroot\Fotos\Kommissionierung mit App_Data vergleichen und von Hand entfernen. Ruecksprung auf eine Version < v1.96.0 zeigt keine Fotos mehr.'
   Quelle='2026-11-11-v1-96-0-vorwaerts-merge-main-hotfixes' }
@{ Id='v1.96.0-Dienst'; Version='1.96.0'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Dienst mit v1.96.0 (OSEON-MERGE ueber Auftragsnummer): erster OSEON-Lauf im Aktivitaets-Protokoll pruefen.'; Quelle='2026-11-11-v1-96-0-vorwaerts-merge-main-hotfixes' }
@{ Id='v1.97.0-Info'; Version='1.97.0'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Anwender-Info: "Auftrag drucken" in der Stueckliste druckt in der Sortierreihenfolge der Ansicht (alle Zeilen, auch gefilterte).'; Quelle='2026-11-11-v1-97-0-auftrag-drucken-sortierung' }
)
}

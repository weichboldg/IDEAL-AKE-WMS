# Nachlauf-Manifest fuer Update-IdealAkeWms.ps1 (Spec 2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec, ADR 0022).
# Nur ASCII (PS 5.1 liest .psd1 ohne BOM als ANSI). Gelesen per Import-PowerShellDataFile: nur Literale.
#
# Felder: Id, Version (eingefuehrt mit), Typ (Schema|Daten|Einmalig|Pruefung|Einstellung|Leitstand|Info|Ordner), Firma (AKE|IDEAL|beide),
#   System (optional, Liste: nur diese Systeme der Firma), Phase (Vorher|Schema|Haltepunkt|VorStart|Nachher),
#   Ausfuehrung: automatisch (nur Schema/Ordner) | Trockenlauf | Transaktion | anzeigen (Daten/Einmalig) | Reparatur | anzeigen (Pruefung),
#   Datei (relativ zum SQL-Ordner), MigrationId (nur Schema), Braucht (Ids, deren MigrationId in der History stehen muss),
#   Voraussetzung (Text), Spur (lesendes SQL, >0 = lief schon), Pruefabfrage (lesendes SQL, Ergebnis wird angezeigt),
#   Text (Platzhalter {Sqlcmd} {Server} {DB} {SqlOrdner} {LogPfad} {Web} {Dienst}), Quelle (Changelog).
# Typ Ordner (Schritt 5a, bei jedem Lauf; Pfad kennt {Web} {Dienst} und seit v1.99.14 {DataProtection}): Pfad, RechtAuf (Ordner, auf den das Recht vererbt gesetzt wird), Identitaet (Web|Dienst),
#   Recht (Lesen|LesenAusfuehren|Aendern), Pflicht ($true = Abbruch, wenn nicht herstellbar), Herstellen ($true = anlegen + icacls).
# Typ Pruefung (v1.99.0, ADR 0023; bei JEDEM Lauf, kein Versionsfilter): Pruefung (lesendes SQL, EINE Zahl: 0 = in Ordnung, > 0 = Abweichungen),
#   Reparatur (SQL, in einer Transaktion) ODER Datei (idempotentes Skript, sqlcmd), OhneTransaktion ($true fuer ALTER DATABASE).
#   Phase Haltepunkt = nach dem Schema bei gestopptem Web und Dienst, Reparatur nur nach Bestaetigung; Phase Nachher = nur lesend.
#   v1.99.12: Appsettings='Abschnitt:Schluessel' statt Pruefung = Einstellung der Dienst-appsettings*.json (wie die DB-Ermittlung), gesetzt = 1;
#   Text kennt {Wert} {Datei}. Phase Vorher = in der Vorpruefung, auf Prod-Systemen (Prod=$true) Abbruch vor dem Stoppen, sonst Hinweis;
#   die Nachlauf-Liste zeigt jede Pruefung erneut (Stand nachher).
# Daten/Einmalig (v1.99.0): Trockenlauf = automatisch, Zeile "TREFFER: n"; 0 -> entfaellt, > 0 -> SCHARF + Backup aus diesem Lauf.
#   Transaktion = ganze Datei in EINER Transaktion, Ausgabe, dann uebernehmen/verwerfen. Marker je Id in dbo.DeployScriptRuns (SQL/142):
#   ein Eintrag mit Marker wird nie wieder angeboten; soll ein Skript bewusst erneut laufen, bekommt es eine neue Id (z. B. SQL/110#2).
#   Phase VorStart = nach 5a, vor dem Start, Web und Dienst gestoppt (Freigabe-Antwort 1, Spec v1.99.0).
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
@{ Id='SQL/142'; Version='1.99.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='142_AddDeployScriptRuns.sql'; MigrationId='20261111174202_AddDeployScriptRuns'; Voraussetzung='additiv (Marker-Tabelle des Update-Skripts)'; Quelle='2026-11-11-v1-99-0-update-skript-prueft-und-fuehrt-aus' }
@{ Id='SQL/143'; Version='1.99.4'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='143_AddPurchaseOrderReceiptSageWe.sql'; MigrationId='20261111200627_AddPurchaseOrderReceiptSageWe'; Voraussetzung='additiv'; Quelle='2026-11-11-v1-99-4-wareneingang-sage-we-beleg' }
@{ Id='SQL/144'; Version='1.99.9'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='144_AddSageKennungen.sql'; MigrationId='20261112141208_AddSageKennungen'; Voraussetzung='additiv'; Quelle='2026-11-12-v1-99-9-sage-system-kuerzel' }
@{ Id='SQL/145'; Version='1.99.10'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='145_AddPurchaseOrderReminders.sql'; MigrationId='20261112143552_AddPurchaseOrderReminders'; Voraussetzung='additiv'; Quelle='2026-11-12-v1-99-10-lieferverzug-mail' }
@{ Id='SQL/146'; Version='1.99.11'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='146_AddWareneingangBarcodeCsv.sql'; MigrationId='20261112151318_AddWareneingangBarcodeCsv'; Voraussetzung='additiv'; Quelle='2026-11-12-v1-99-11-wareneingang-barcode-csv' }
@{ Id='SQL/148'; Version='1.99.21'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='148_AddArticleSupplierLetzteBestellung.sql'; MigrationId='20261113171808_AddArticleSupplierLetzteBestellung'; Voraussetzung='additiv'; Quelle='2026-11-13-v1-99-21-einkauf-letzte-bestellmenge' }
@{ Id='SQL/149'; Version='1.99.22'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='149_WidenBelegnummer45.sql'; MigrationId='20261113172929_WidenBelegnummer45'; Voraussetzung='Spalten verbreitern (20/30 -> 45), Index neu; nicht destruktiv'; Quelle='2026-11-13-v1-99-22-belegnummer-45-wareneingang' }
@{ Id='SQL/150'; Version='1.99.23'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='150_AddPurchaseOrderReceiptAbsendekennung.sql'; MigrationId='20261114080950_AddPurchaseOrderReceiptAbsendekennung'; Voraussetzung='additiv, Spalte NULL + gefilterter UNIQUE-Index'; Quelle='2026-11-14-v1-99-23-schutz-doppel-absenden' }
@{ Id='SQL/152'; Version='1.99.28'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='152_AddUserMussKennwortAendern.sql'; MigrationId='20261116165705_AddUserMussKennwortAendern'; Voraussetzung='additiv, Spalte bit NOT NULL DEFAULT 0'; Quelle='2026-11-16-v1-99-28-sicherheit-anmeldung-installer' }
@{ Id='SQL/155'; Version='1.99.30'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='155_AddArticleLastOrderUnassigned.sql'; MigrationId='20261116190709_AddArticleLastOrderUnassigned'; Voraussetzung='additiv, neue Tabelle'; Quelle='2026-11-16-v1-99-30-bestellvorschlaege-nachbesserung' }
@{ Id='SQL/154'; Version='1.99.32'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='154_AddBdeTeileliste.sql'; MigrationId='20261117075856_AddBdeTeileliste'; Voraussetzung='additiv, Spalte mit Default + Tabelle mit gefiltertem UNIQUE'; Quelle='2026-11-17-v1-99-32-bde-teileliste-blechfertigung' }
@{ Id='SQL/153'; Version='1.99.32'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'; Braucht=@('SQL/96')
   Datei='153_AddOperationConfigSageArbeitsgangnummer.sql'; MigrationId='20261117085624_AddOperationConfigSageArbeitsgangnummer'; Voraussetzung='additiv, zwei Spalten NULL'; Quelle='2026-11-17-v1-99-27-arbeitsgang-mapping-sage-arbeitsgangnummer' }
@{ Id='SQL/156'; Version='1.99.33'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='156_AddSagePurchaseOrderLineAbNummerKommission.sql'; MigrationId='20261117102804_AddSagePurchaseOrderLineAbNummerKommission'; Voraussetzung='additiv, zwei Spalten NULL (Werte fuellt der naechste Abgleich Offene Bestellungen)'; Quelle='2026-11-17-v1-99-33-wareneingang-sammellieferschein-bestellkopf' }
@{ Id='SQL/157'; Version='1.99.34'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'; Braucht=@('SQL/94')
   Datei='157_AddBeschichtungsauftragPositionText.sql'; MigrationId='20261117141308_AddBeschichtungsauftragPositionText'; Voraussetzung='additiv, Spalte nvarchar(50) NULL, kein Backfill'; Quelle='2026-11-17-v1-99-34-beschichtung-ake-stueckliste' }
@{ Id='SQL/158'; Version='1.99.35'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='158_AddBfAuftraege.sql'; MigrationId='20261117154645_AddBfAuftraege'; Voraussetzung='additiv, zwei neue Tabellen BfAuftraege und BfAuftragPositionen (Blechfertigung), kein Backfill'; Quelle='2026-11-17-v1-99-35-blechfertigung-bf-auftraege' }
@{ Id='SQL/159'; Version='1.99.37'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='159_FaStrukturAbgleich.sql'; MigrationId='20261117183315_FaStrukturAbgleich'; Voraussetzung='Dienst gestoppt; 4 Lebenslauf-Spalten je FaHierarchyNodes/FaHierarchyOrderInfos/ProductionOrderPositions (Startwert SyncedAt), leere Artnr/ABNr/Pos -> NULL, zwei UNIQUE-Indizes; bricht bei doppelten Schluesseln vor dem Index ab'; Quelle='2026-11-17-v1-99-37-fa-struktur-abgleich' }
@{ Id='SQL/160'; Version='1.99.38'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='160_AddSageFaArbeitsgangAnlagen.sql'; MigrationId='20261117192807_AddSageFaArbeitsgangAnlagen'; Voraussetzung='additiv, eine neue Tabelle SageFaArbeitsgangAnlagen (Warteschlange AG-Anlage in Sage-FAs), kein Backfill'; Quelle='2026-11-17-v1-99-38-arbeitsgaenge-in-sage-fa-anlegen' }
@{ Id='SQL/161'; Version='1.99.41'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='161_AddBeschichtungsauftragVersandUndVerpackt.sql'; MigrationId='20261118120203_AddBeschichtungsauftragVersandUndVerpackt'; Voraussetzung='additiv: CreditorId (FK Creditors, Index), VersendetAt/-ByUserId, Verpackt/OhneFa je Position (Default 0), kein Backfill'; Quelle='2026-11-18-v1-99-41-beschichtungsauftrag-ausbau' }
@{ Id='SQL/162'; Version='1.99.42'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='162_AddUserKuerzelUndBfBestellerKuerzel.sql'; MigrationId='20261118144836_AddUserKuerzelUndBfBestellerKuerzel'; Voraussetzung='additiv: Users.Kuerzel (gefiltert UNIQUE), BfAuftraege.BestellerKuerzel; danach Kuerzel der BF-Besteller pflegen, sonst ist die Freigabe gesperrt'; Quelle='2026-11-18-v1-99-42-nachbesserungen' }
@{ Id='SQL/163'; Version='1.99.42'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='163_AddBfPositionArbeitsschritte.sql'; MigrationId='20261118145602_AddBfPositionArbeitsschritte'; Voraussetzung='additiv: BfAuftragPositionen.Arbeitsschritte, Nachtrag nur leerer Felder aus dem Artikel-Merkmal USER_OSArbeitsschritte'; Quelle='2026-11-18-v1-99-42-nachbesserungen' }
@{ Id='SQL/164'; Version='1.99.72'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='164_AddArticleCamPath.sql'; MigrationId='20261125173002_AddArticleCamPath'; Voraussetzung='additiv: Articles.CamPath NVARCHAR(500) NULL (Fraesen-Pfad, WMS-Feld), kein Backfill'; Quelle='2026-11-25-v1-99-72-bde-liste-isolierungsfraese-pfad-material' }
@{ Id='SQL/165'; Version='1.99.72'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='165_AddBdeTeilelisteLagerbestellungen.sql'; MigrationId='20261125173021_AddBdeTeilelisteLagerbestellungen'; Voraussetzung='additiv: Werkbank-Option BdeTeilelisteLagerbestellungen (Default 0), BdeTeilMeldungen.WarehouseRequisitionItemId NULL + FK, UNIQUE-Filter erweitert (Index-Neuaufbau), zweiter gefilterter UNIQUE'; Quelle='2026-11-25-v1-99-72-bde-liste-isolierungsfraese-pfad-material' }
@{ Id='SQL/166'; Version='1.99.53'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='166_AddStockMovementWarehouseRequisitionItem.sql'; MigrationId='20261123124036_AddStockMovementWarehouseRequisitionItem'; Voraussetzung='additiv: StockMovements.WarehouseRequisitionItemId (NULL, FK NO ACTION, Index), kein Backfill'; Quelle='2026-11-23-v1-99-53-lagerbestellung-verbrauchsbuchung' }
@{ Id='SQL/167'; Version='1.99.68'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='167_AddStockMovementAbteilung.sql'; MigrationId='20261125134408_AddStockMovementAbteilung'; Voraussetzung='additiv: StockMovements.Abteilung NVARCHAR(200) NULL (Lagerbewegungen: Spalte Abteilung bei Ausbuchung, geht als Referenz an Sage), kein Backfill'; Quelle='2026-11-25-v1-99-68-sage-buchung-referenz-benutzer-abteilung' }
@{ Id='SQL/168'; Version='1.99.70'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='168_AddPickingStatusStuecklisteKleiner.sql'; MigrationId='20261125161741_AddPickingStatusStuecklisteKleiner'; Voraussetzung='additiv: ProductionOrderPickingStatus.StuecklisteKleiner NVARCHAR(1000) NULL (Marker Stueckliste kleiner als offene Kommissionierzeilen, Leitstand-Badge), kein Backfill'; Quelle='2026-11-25-v1-99-70-kommissionierauftraege-nachtrag-nachts' }
@{ Id='SQL/169'; Version='1.99.75'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
   Datei='169_AddBdeTeilMeldungIstAbschliessendUndStandardsicht.sql'; MigrationId='20261125191311_AddBdeTeilMeldungIstAbschliessendUndStandardsicht'; Voraussetzung='SQL/165; additiv: BdeTeilMeldungen.IstAbschliessend bit NOT NULL DEFAULT 1 (Bestand = abschliessend), beide UNIQUE-Filter um IstAbschliessend = 1 erweitert (Index-Neuaufbau, Dubletten-Pruefung mit Warnung), ProductionWorkplaces.BdeStandardsichtTeile bit DEFAULT 0 mit Vorbelegung bei Erstanlage'; Quelle='2026-11-25-v1-99-75-bde-masken-etappe-3-teilfertigmeldung' }

# ---------------------------------------------------------------- Pruefungen (jeder Lauf; Reparatur nur nach Bestaetigung) -------
# v1.99.28 (ADR 0027): admin zaehlt nicht - der Web-Start stellt admin ohne Kennwort auf das Standard-Erstkennwort mit Pflichtwechsel um.
# v1.99.37 (ADR 0032): SQL/159 legt UNIQUE-Indizes auf den fachlichen Schluessel; Dubletten im Bestand (P-AB1/P-AB2) vorher sichtbar machen. v1.99.44 (S-2): nur RTRIM wie Index/Abgleich.
@{ Id='Pruefung/FaStrukturSchluesselDoppelt'; Version='1.99.37'; Typ='Pruefung'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN OBJECT_ID(N''dbo.FaHierarchyNodes'', N''U'') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM (SELECT 1 AS x FROM dbo.FaHierarchyNodes GROUP BY HauptFA, VaterFA, SubFA, Position, NULLIF(RTRIM(Artnr), N'''') HAVING COUNT(*) > 1) d) + (SELECT COUNT(*) FROM (SELECT 1 AS x FROM dbo.FaHierarchyOrderInfos GROUP BY HauptFA, NULLIF(RTRIM(ABNr), N''''), NULLIF(RTRIM(Pos), N'''') HAVING COUNT(*) > 1) d) END'
   Text='Doppelte FA-Struktur-Schluessel im WMS-Bestand: SQL/159 bricht vor dem UNIQUE-Index ab. Liste mit P-AB1/P-AB2 der Spec 2026-11-17-fa-struktur-abgleich-statt-full-replace an Gerald; nicht automatisch bereinigen (der naechste Lauf des alten Dienstes schreibt die Tabellen ohnehin neu).'
   Quelle='2026-11-17-v1-99-37-fa-struktur-abgleich' }
# v1.99.41: Lackierer-Auswahl am Beschichtungsauftrag braucht aktive Beschichter-Kreditoren; Uebergeben -> Erledigt geht nur noch ueber Versendet.
@{ Id='Pruefung/BeschichterKreditoren'; Version='1.99.41'; Typ='Pruefung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN OBJECT_ID(N''dbo.Creditors'', N''U'') IS NULL THEN 0 WHEN EXISTS (SELECT 1 FROM dbo.Creditors WHERE IsCoater = 1 AND IsActive = 1) THEN 0 ELSE 1 END'
   Text='Kein aktiver Kreditor mit "Beschichter": die Lackierer-Auswahl am Beschichtungsauftrag ist leer, gedruckt wird nur der Dienstleister-Text ohne Anschrift. In /Creditors Beschichter kennzeichnen (P-2 der Spec 2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck).'
   Quelle='2026-11-18-v1-99-41-beschichtungsauftrag-ausbau' }
@{ Id='Pruefung/BeschichtungsauftragUebergebenOffen'; Version='1.99.41'; Typ='Pruefung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN OBJECT_ID(N''dbo.Beschichtungsauftraege'', N''U'') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.Beschichtungsauftraege WHERE Status = 30) END'
   Text='Beschichtungsauftraege im Status Uebergeben: ab dieser Version fuehrt der Weg ueber "Versendet" (alle Positionen verpackt), Uebergeben -> Erledigt direkt geht nicht mehr. Fachbereich informieren (P-4, Rueckfrage 15).'
   Quelle='2026-11-18-v1-99-41-beschichtungsauftrag-ausbau' }
# v1.99.43: Leitstand-Gate der IDEAL-Kommissionierliste (Schalter FaHierarchyKommissionierlisteNurFreigegebene, Seed an).
# Bewusst Phase Nachher statt Vorher (Abweichung von der Spec): Pruefungen laufen bei jedem Update, eine Vorher-Pruefung > 0 bricht Prod vor dem Stoppen ab.
@{ Id='Pruefung/KommissionierlisteNichtFreigegeben'; Version='1.99.43'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'; Braucht=@('SQL/159')
   Pruefung='SELECT CASE WHEN OBJECT_ID(N''dbo.FaHierarchyNodes'', N''U'') IS NULL THEN 0 WHEN EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N''FaHierarchyKommissionierlisteNurFreigegebene'' AND LOWER([Value]) = N''false'') THEN 0 ELSE (SELECT COUNT(DISTINCT n.HauptFA) FROM dbo.FaHierarchyNodes n WHERE n.VerschwundenSeit IS NULL AND LTRIM(RTRIM(ISNULL(n.Kommissionieren, N''''))) <> N'''' AND NOT EXISTS (SELECT 1 FROM dbo.ProductionOrders po JOIN dbo.ProductionOrderPickingStatus ps ON ps.ProductionOrderId = po.Id WHERE po.OrderNumber = CAST(n.HauptFA AS nvarchar(50)) AND (po.SubOrderNumber = N'''' OR po.SubOrderNumber = po.OrderNumber) AND ps.IsReleasedForPicking = 1)) END'
   Text='HauptFA mit Kommissionier-Positionen ohne Leitstand-Freigabe: sie fehlen in der Kommissionierliste, bis der Leitstand sie freigibt (oder Schalter Kommissionierliste nur nach Leitstand-Freigabe aus). Aufschluesselung mit Q-B der Spec 2026-11-18-ideal-kommissionierliste-filter-spalten-leitstand.'
   Quelle='2026-11-18-v1-99-43-kommissionierliste-filter-spalten-leitstand' }
@{ Id='Pruefung/KommissionierlisteLeitstandAus'; Version='1.99.43'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N''LeitstandAktiv'' AND LOWER([Value]) = N''true'') THEN 0 WHEN EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N''FaHierarchyKommissionierlisteNurFreigegebene'' AND LOWER([Value]) = N''false'') THEN 0 ELSE 1 END'
   Text='Gate an, Leitstand aus: Kommissionierliste bleibt leer. LeitstandAktiv einschalten oder Schalter Kommissionierliste nur nach Leitstand-Freigabe aus.'
   Quelle='2026-11-18-v1-99-43-kommissionierliste-filter-spalten-leitstand' }
@{ Id='Pruefung/KommissionierlisteVorfilterZiele'; Version='1.99.43'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'; Braucht=@('SQL/159')
   Pruefung='SELECT CASE WHEN OBJECT_ID(N''dbo.FaHierarchyNodes'', N''U'') IS NULL THEN 0 WHEN EXISTS (SELECT 1 FROM dbo.FaHierarchyNodes WHERE VerschwundenSeit IS NULL AND LTRIM(RTRIM(Kommissionieren)) IN (N''K-01'', N''K-02'')) THEN 0 ELSE 1 END'
   Text='Standard-Ziele K-01/K-02 kommen in der FA-Struktur nicht vor - Einstellung Kommissionierliste: Standard-Ziele unter /Settings pruefen (Q-C der Spec).'
   Quelle='2026-11-18-v1-99-43-kommissionierliste-filter-spalten-leitstand' }
@{ Id='Pruefung/BenutzerOhneKennwort'; Version='1.99.28'; Typ='Pruefung'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT COUNT(*) FROM dbo.Users u WHERE u.IsActive = 1 AND u.Name <> N''admin'' AND (u.PasswordHash IS NULL OR u.PasswordHash = N'''') AND (u.WindowsUserName IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N''WindowsAuthAktiv'' AND LOWER([Value]) = N''true''))'
   Text='Aktive Benutzer ohne Kennwort, die sich ab v1.99.28 nicht mehr anmelden koennen (lokal, bzw. AD bei WindowsAuthAktiv aus): vorher unter /Users/Edit ein Erstkennwort setzen (ab v1.99.28 mit Pflichtwechsel bei der ersten Anmeldung) oder deaktivieren; Liste mit P-1 der Spec 2026-11-14-sicherheit-anmeldung-und-installer.'
   Quelle='2026-11-16-v1-99-28-sicherheit-anmeldung-installer' }
@{ Id='Pruefung/FreshInstallIndizes'; Version='1.99.28'; Typ='Pruefung'; Firma='IDEAL'; System=@('Prod'); Phase='Haltepunkt'; Ausfuehrung='Reparatur'; Datei='151_RepairFreshInstallIndexes.sql'
   Pruefung='SELECT (SELECT COUNT(*) FROM (VALUES (N''IX_StockMovements_StorageLocationId''),(N''IX_StockMovements_Timestamp''),(N''IX_StockMovements_SourceStorageLocationId''),(N''IX_StockMovements_ProductionOrderId''),(N''IX_SageBookingQueueItems_Status''),(N''IX_SageBookingQueueItems_StockMovementId''),(N''IX_StorageLocations_IsActive''),(N''IX_StorageLocations_Source''),(N''IX_StorageLocations_IstBuchbar''),(N''IX_StorageLocations_StorageAreaId''),(N''IX_StorageAreas_Lagerkennung''),(N''UQ_ArticleSageDetails_ArticleId''),(N''IX_SyncLogs_Timestamp_Desc''),(N''IX_SyncLogs_Service_Level''),(N''IX_WorkstationUsers_UserId''),(N''IX_ProductionOrders_OrderNumber''),(N''IX_ProductionOrders_SubOrderNumber''),(N''IX_ProductionOrders_ArticleNumber''),(N''IX_ProductionOrders_IsDone''),(N''IX_ProductionOrders_IsCancelled''),(N''IX_ProductionOrders_ProductionWorkplaceId''),(N''IX_PickingItems_ProductionOrderId''),(N''IX_PickingItems_POId_IsPicked''),(N''IX_ProductionWorkplaceUsers_ProductionWorkplaceId_UserId''),(N''IX_ProductionWorkplaceUsers_UserId''),(N''IX_WorkOperations_ProductionOrderId''),(N''IX_WorkOperations_ProductionOrderId_Sequence''),(N''IX_WorkOperations_ProductionWorkplaceId'')) v(n) WHERE NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.name = v.n)) + CASE WHEN OBJECT_ID(N''dbo.FK_StockMovements_ProductionOrders_ProductionOrderId'', N''F'') IS NULL THEN 1 ELSE 0 END'
   Text='Indizes/FK aus Abschnitt 15 der Erstinstallation fehlen (Meldung 207 verwarf den ganzen Abschnitt beim alten 00_FreshInstall.sql, Erstinstallation 2026-11-04): Reparatur spielt SQL/151 ein (idempotent, additiv, nur IDEAL-Prod; ohne die zwei durch vorhandene Indizes abgedeckten). Grosse Tabellen: kann einige Minuten dauern.'
   Quelle='2026-11-16-v1-99-28-sicherheit-anmeldung-installer' }
@{ Id='Pruefung/FreshInstallIndizesDuplikate'; Version='1.99.63'; Typ='Pruefung'; Firma='IDEAL'; System=@('Prod'); Phase='Vorher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN OBJECT_ID(N''dbo.SageBookingQueueItems'', N''U'') IS NULL OR EXISTS (SELECT 1 FROM sys.indexes WHERE name = N''IX_SageBookingQueueItems_StockMovementId'') THEN 0 ELSE (SELECT COUNT(*) FROM (SELECT StockMovementId FROM dbo.SageBookingQueueItems GROUP BY StockMovementId HAVING COUNT(*) > 1) d) END + CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = N''IX_ProductionOrders_SubOrderNumber'') THEN 0 ELSE (SELECT COUNT(*) FROM (SELECT SubOrderNumber FROM dbo.ProductionOrders GROUP BY SubOrderNumber HAVING COUNT(*) > 1) d) END + CASE WHEN OBJECT_ID(N''dbo.ProductionWorkplaceUsers'', N''U'') IS NULL OR EXISTS (SELECT 1 FROM sys.indexes WHERE name = N''IX_ProductionWorkplaceUsers_ProductionWorkplaceId_UserId'') THEN 0 ELSE (SELECT COUNT(*) FROM (SELECT ProductionWorkplaceId, UserId FROM dbo.ProductionWorkplaceUsers GROUP BY ProductionWorkplaceId, UserId HAVING COUNT(*) > 1) d) END'
   Text='Doppelte Werte in Spalten, die SQL/151 eindeutig indiziert (SageBookingQueueItems.StockMovementId, ProductionOrders.SubOrderNumber, ProductionWorkplaceUsers Werkbank+Benutzer): die Reparatur wuerde mit Fehler abbrechen, waehrend Web und Dienst gestoppt sind. Duplikate vorher mit Gerald klaeren (GROUP BY ... HAVING COUNT(*) > 1), dann Skript neu starten.'
   Quelle='2026-11-24-v1-99-63-sql-151-fresh-install-indizes' }
@{ Id='Pruefung/SmtpBenutzer'; Version='1.99.28'; Typ='Pruefung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Appsettings='MailSettings:SmtpUsername'
   Text='SMTP-Anmeldung als {Wert} ({Datei}): mit SmtpUseSsl = false geht das Kennwort unverschluesselt - anonymes Relay oder SmtpUseSsl = true (Port 465).'
   Quelle='2026-11-16-v1-99-28-sicherheit-anmeldung-installer' }
@{ Id='Pruefung/Kommissionierbereiche'; Version='1.88.0'; Typ='Pruefung'; Firma='beide'; Phase='Haltepunkt'; Ausfuehrung='Reparatur'; Braucht=@('SQL/133')
   Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.PickingAreas) THEN 0 ELSE 1 END'
   Datei='133_AddPickingAreasAndOrders.sql'
   Text='Keine Kommissionierbereiche (Seed stand nur in SQL/133): Reparatur spielt SQL/133 erneut ein (idempotent, Seed nur bei leerer Tabelle). Ohne Standardbereich legt die App keine Kommissionierauftraege an und das Altbestand-Skript bricht ab.'
   Quelle='2026-11-11-v1-99-0-update-skript-prueft-und-fuehrt-aus' }
@{ Id='Pruefung/Standardbereich'; Version='1.88.0'; Typ='Pruefung'; Firma='beide'; Phase='Haltepunkt'; Ausfuehrung='anzeigen'; Braucht=@('SQL/133')
   Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.PickingAreas) AND NOT EXISTS (SELECT 1 FROM dbo.PickingAreas WHERE IsDefault = 1 AND IsActive = 1) THEN 1 ELSE 0 END'
   Text='Kein aktiver Standard-Kommissionierbereich: unter Stammdaten > Kommissionierbereiche einen Standard festlegen.'
   Quelle='2026-11-11-v1-99-0-update-skript-prueft-und-fuehrt-aus' }
@{ Id='Pruefung/RCSI'; Version='1.31.0'; Typ='Pruefung'; Firma='beide'; Phase='Haltepunkt'; Ausfuehrung='Reparatur'; OhneTransaktion=$true
   Pruefung='SELECT CASE WHEN d.is_read_committed_snapshot_on = 0 AND EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] IN (N''Sync:HierarchicalFaEnabled'', N''ProduktionsauftragHierarchisch'') AND LOWER(LTRIM(RTRIM([Value]))) = N''true'') THEN 1 ELSE 0 END FROM sys.databases d WHERE d.name = DB_NAME()'
   Reparatur='ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE'
   Text='RCSI ist aus, der FA-Struktur-Import (Sync:HierarchicalFaEnabled bzw. ProduktionsauftragHierarchisch) braucht is_read_committed_snapshot_on = 1 (v1.31.0, TS-57.0).'
   Quelle='2026-08-10-v1-31-0-ideal-teile-1-5; 2026-11-11-v1-99-0-update-skript-prueft-und-fuehrt-aus' }
@{ Id='Pruefung/RollenBeschichtungsauftrag'; Version='1.51.0'; Typ='Pruefung'; Firma='beide'; Phase='Haltepunkt'; Ausfuehrung='Reparatur'
   Pruefung='SELECT COUNT(*) FROM (VALUES (N''beschichtungsauftrag''), (N''beschichtungsauftrag_schreiben'')) v(k) WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.[Key] = v.k)'
   Reparatur='INSERT INTO dbo.Roles ([Key], [Name], [Description], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
SELECT v.k, v.n, v.d, 1, (SELECT ISNULL(MAX(SortOrder), 0) FROM dbo.Roles) + v.o, SYSDATETIME(), N''Update-Skript'', LEFT(SUSER_SNAME(), 200)
FROM (VALUES (N''beschichtungsauftrag'', N''Beschichtungsauftrag'', N''Beschichtungsauftrag-Liste und Bildschirmdruck (Lesen).'', 1),
             (N''beschichtungsauftrag_schreiben'', N''Beschichtungsauftr'' + NCHAR(228) + N''ge (Schreibrecht)'',
              N''Gespeicherte Beschichtungsauftraege anlegen und bearbeiten; schliesst Lesen ein.'', 2)) v(k, n, d, o)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.[Key] = v.k);'
   Text='Rollen beschichtungsauftrag / beschichtungsauftrag_schreiben fehlen (kein Seed; nur IDEAL-Prod hat sie aus der Installation, v1.99.1). Zuweisen bleibt Hand-Schritt (unter /Roles).'
   Quelle='2026-10-12-v1-51-0-beschichtungsauftraege-oberflaeche; 2026-11-11-v1-99-0-update-skript-prueft-und-fuehrt-aus' }
@{ Id='Pruefung/RolleEinkauf'; Version='1.99.20'; Typ='Pruefung'; Firma='beide'; Phase='Haltepunkt'; Ausfuehrung='Reparatur'
   Pruefung='SELECT COUNT(*) FROM (VALUES (N''einkauf'')) v(k) WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.[Key] = v.k)'
   Reparatur='INSERT INTO dbo.Roles ([Key], [Name], [Description], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
SELECT v.k, v.n, v.d, 1, (SELECT ISNULL(MAX(SortOrder), 0) FROM dbo.Roles) + v.o, SYSDATETIME(), N''Update-Skript'', LEFT(SUSER_SNAME(), 200)
FROM (VALUES (N''einkauf'', N''Einkauf'', N''Einkauf - Bestellvorschlaege, Offene Bestellungen, offene Sage-Wareneingaenge (lesen).'', 1)) v(k, n, d, o)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.[Key] = v.k);'
   Text='Rolle einkauf fehlt (kein Seed). Zuweisen an den Einkauf bleibt Hand-Schritt unter /Roles.'
   Quelle='2026-11-13-v1-99-20-einkauf-bestellvorschlaege' }
@{ Id='Pruefung/DummyArtikel'; Version='1.30.0'; Typ='Pruefung'; Firma='beide'; Phase='Haltepunkt'; Ausfuehrung='Reparatur'
   Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Articles WHERE ArticleNumber = N''DUMMY'') THEN 0 ELSE 1 END'
   Datei='86_SeedDummyArticle.sql'
   Text='DUMMY-Artikel fehlt (Seed nur in SQL/86): ohne ihn lehnt die Lagerbestellung unbekannte EK-Nummern ab. Reparatur spielt SQL/86 erneut ein (idempotent).'
   Quelle='2026-08-06-v1-30-0-wms-bugs-teil-4-5-7-8; 2026-11-11-v1-99-0-update-skript-prueft-und-fuehrt-aus' }
@{ Id='Pruefung/sage-fa-ag-sdata-zugang'; Version='1.99.38'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'; Braucht=@('SQL/160')
   Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''SageFaArbeitsgaengeAnlegenAktiv'' AND LOWER(LTRIM(RTRIM([Value]))) = N''true'') AND (NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''Sync:SageFaArbeitsgaengeBearbeiter'' AND LEN(LTRIM(RTRIM([Value]))) > 0) OR NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''SData:BaseUrl'' AND LEN(LTRIM(RTRIM([Value]))) > 0)) THEN 1 ELSE 0 END'
   Text='Arbeitsgaenge in Sage anlegen ist an, aber Sync:SageFaArbeitsgaengeBearbeiter oder SData:BaseUrl leer: der Dienst sendet nichts und setzt eingereihte Arbeitsgaenge auf Fehler (/ServiceSettings > Sage-FA-Arbeitsgaenge (IDEAL) bzw. SData).'
   Quelle='2026-11-17-v1-99-38-arbeitsgaenge-in-sage-fa-anlegen' }
@{ Id='Pruefung/LagerorteFuerSageAnlage'; Version='1.99.71'; Typ='Pruefung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'; Braucht=@('SQL/98')
   Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''SageLagerplatzAnlageAktiv'' AND LOWER(LTRIM(RTRIM([Value]))) = N''true'') AND (NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''Sync:Quelle:Lagerplaetze'' AND [Value] COLLATE Latin1_General_BIN = N''SData'') OR NOT EXISTS (SELECT 1 FROM dbo.StorageAreas WHERE IsActive = 1 AND SageMissingSince IS NULL)) THEN 1 ELSE 0 END'
   Text='Lagerplatz-Anlage in Sage ist an, aber Sync:Quelle:Lagerplaetze steht nicht auf SData oder es gibt keine aktiven Lagerorte: das Formular Lagerplaetze in Sage anlegen bietet keinen Lagerort an (/ServiceSettings > Quelle Lagerplaetze = SData, Lagerplatz-Abgleich laufen lassen).'
   Quelle='2026-11-25-v1-99-71-lagerplatz-anlage-wms-nach-sage' }
# v1.99.76 (Spec 2026-11-25-bde-masken-vereinheitlichung, G5/AK 32c, Antwort 22): die neue BDE-Seite beachtet BdeNurFaMeldung nicht mehr, der Buchungsdienst in Alt-Zweigen schon.
@{ Id='Pruefung/BdeNurFaMeldungAn'; Version='1.99.76'; Typ='Pruefung'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN OBJECT_ID(N''dbo.AppSettings'', N''U'') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.AppSettings WHERE [Key] = N''BdeNurFaMeldung'' AND LOWER(LTRIM(RTRIM([Value]))) IN (N''true'', N''1'', N''on'')) END'
   Text='BdeNurFaMeldung ist an: die neue BDE-Seite beachtet die Einstellung nicht (Antwort 2), der Buchungsdienst aber weiter in seinen Alt-Zweigen (Starts koennen abgewiesen werden, Antwort 22). Vor dem Update mit Gerald klaeren.'
   Quelle='2026-11-25-v1-99-76-bde-masken-etappe-4a-fa-sicht' }
# v1.99.76 (E4-7, Antwort 15): Standardsicht Teile nur mit verfuegbarer Teile-Sicht; nach SQL/169 erwartet 0 (Vorbelegung nutzt dieselbe Bedingung).
@{ Id='Pruefung/BdeStandardsichtOhneTeileSicht'; Version='1.99.76'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'; Braucht=@('SQL/169')
   Pruefung='SELECT COUNT(*) FROM dbo.ProductionWorkplaces WHERE BdeStandardsichtTeile = 1 AND (BdeAktiv = 0 OR BdeTeileliste = 0 OR ISNULL(LTRIM(RTRIM(ArbeitsschrittCode)), N'''') = N'''')'
   Text='Werkbaenke mit Standardsicht Teile ohne BDE aktiv, ohne BDE-Teileliste bzw. ohne Kuerzel: sie oeffnen die FA-Sicht. In Stammdaten > Werkbaenke korrigieren (BDE-Standardsicht = FA oder Teileliste einschalten).'
   Quelle='2026-11-25-v1-99-76-bde-masken-etappe-4a-fa-sicht' }
@{ Id='Pruefung/ArtikelQuelleSData'; Version='1.99.39'; Typ='Pruefung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''Sync:Quelle:Artikel'' AND [Value] COLLATE Latin1_General_BIN = N''SData'') THEN 0 ELSE 1 END'
   Text='Sync:Quelle:Artikel steht nicht auf SData (Default View): dann bleiben ArticleSageDetails leer (Sage-Spalten in den Bestellvorschlaegen, Masse der Beschichtung AKE). Hand-Schritt, keine automatische Umschaltung: /ServiceSettings > Sage-Abgleich (SData) > Quelle Artikel-Abgleich = SData (exakte Schreibweise), danach im Aktivitaets-Protokoll den Lauf Artikel pruefen.'
   Quelle='2026-11-17-v1-99-39-nachbesserung-abend' }
@{ Id='Pruefung/LieferverzugEmpfaenger'; Version='1.99.10'; Typ='Pruefung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'; Braucht=@('SQL/145')
   Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''Lieferverzug:MailAktiv'' AND LOWER(LTRIM(RTRIM([Value]))) = N''true'') AND NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''Lieferverzug:EinkaufEmpfaenger'' AND LEN(LTRIM(RTRIM([Value]))) > 0) THEN 1 ELSE 0 END'
   Text='Lieferverzug-Mails sind an, aber ohne Einkauf-Empfaenger: es geht keine Mail raus (auch keine Testbetrieb-Mail). /ServiceSettings > Lieferverzug-Mail > Einkauf: Empfaenger eintragen.'
   Quelle='2026-11-12-v1-99-10-lieferverzug-mail' }
@{ Id='Pruefung/WareneingangCsv'; Version='1.99.11'; Typ='Pruefung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'; Braucht=@('SQL/146')
   Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''WareneingangCsvAktiv'' AND LOWER(LTRIM(RTRIM([Value]))) = N''true'') AND (NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''WareneingangCsvPfad'' AND LEN(LTRIM(RTRIM([Value]))) > 0) OR NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''SageWareneingangBelegAktiv'' AND LOWER(LTRIM(RTRIM([Value]))) = N''true'')) THEN 1 ELSE 0 END'
   Text='WareneingangCsvAktiv ist an, aber WareneingangCsvPfad leer oder SageWareneingangBelegAktiv aus: es wird keine CSV geschrieben (/ServiceSettings > Wareneingang-CSV).'
   Quelle='2026-11-12-v1-99-11-wareneingang-barcode-csv' }
@{ Id='Pruefung/WareneingangCsvKollision'; Version='1.99.33'; Typ='Pruefung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'; Braucht=@('SQL/146')
   Pruefung='SELECT COUNT(*) FROM dbo.WareneingangCsvExporte WHERE Status = 3 AND Fehler LIKE N''Datei existiert bereits%'''
   Text='Altzeilen aus der Zeit vor v1.99.33 mit Fehler Datei existiert bereits (Sammellieferschein). Nur anzeigen, nie automatisch: je Zeile auf der Freigabe pruefen, dann in Lager > Sage-Wareneingaenge CSV erneut schreiben (Datei erhaelt dann _1, _2 ...).'
   Quelle='2026-11-17-v1-99-33-wareneingang-sammellieferschein-bestellkopf' }
@{ Id='Pruefung/QrTrennzeichenKomma'; Version='1.69.0'; Typ='Pruefung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N''QrTrennzeichen'' AND [Value] = N'','') THEN (SELECT COUNT(*) FROM dbo.StorageLocations WHERE Code LIKE N''%,%'') ELSE 0 END'
   Text='QrTrennzeichen = Komma, aber Lagerplatz-Codes enthalten ein Komma: Trennzeichen unter /Settings aendern oder Codes bereinigen.'
   Quelle='2026-10-27-v1-69-0-qr-trennzeichen' }
@{ Id='Pruefung/Firmenanschrift'; Version='1.49.0'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT COUNT(*) FROM dbo.AppSettings WHERE [Key] = N''Firmenanschrift'' AND [Value] LIKE N''%38, 4840%'''
   Text='Firmenanschrift unter /Settings korrigieren ("38, 4840" -> "28, 4810").'
   Quelle='2026-10-09-v1-49-0-briefkopf-beschichtungsauftrag' }
@{ Id='Pruefung/ArticlesViewName'; Version='1.60.0'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''Sync:ArticlesViewName'' AND LTRIM(RTRIM(ISNULL([Value], N''''))) <> N'''') THEN 0 ELSE 1 END'
   Text='Sync:ArticlesViewName ist leer: unter /ServiceSettings setzen und nicht mehr leeren (v1.60.0).'
   Quelle='2026-10-15-v1-60-0-ideal-sage-views-umstellung' }
@{ Id='Pruefung/MailPickup'; Version='1.99.12'; Typ='Pruefung'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Appsettings='MailSettings:PickupDirectory'
   Text='Mails gehen nur in {Wert} ({Datei}): alle Dienst-Mails (Lieferverzug, Meldebestand, Fehler, Bestellung) landen als .eml im Ordner statt per SMTP.'
   Quelle='2026-11-12-v1-99-12-update-skript-pickup-pruefung' }
@{ Id='Pruefung/SDataKennwortAppsettings'; Version='1.99.14'; Typ='Pruefung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Appsettings='SageLagerbuchung:Password'
   Text='SData-Kennwort steht noch im Klartext in {Datei}. Erst in den Service-Einstellungen eintragen und den Lauf Sage-Lagerbuchung pruefen, dann aus der Datei entfernen (auch Web: appsettings.Production.json).'
   Quelle='2026-11-13-v1-99-14-sdata-teil-2-userfelder-stueckliste' }
@{ Id='Pruefung/DoppeltePrio'; Version='1.99.17'; Typ='Pruefung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT COUNT(*) FROM (SELECT ps.PickingPriority FROM dbo.ProductionOrderPickingStatus ps JOIN dbo.ProductionOrders po ON po.Id = ps.ProductionOrderId WHERE ps.PickingPriority IS NOT NULL AND po.IsDone = 0 AND po.IsCancelled = 0 AND (po.SubOrderNumber = N'''' OR po.SubOrderNumber = po.OrderNumber) GROUP BY ps.PickingPriority HAVING COUNT(*) > 1) d'
   Text='Offene FAs mit gleicher Kommissionier-Prio (Altbestand vor v1.99.17, freigegeben oder zurueckgenommen): im Leitstand per Prio-Eingabe korrigieren; Liste mit Pruefabfrage 1 der Spec 2026-11-13-doppelte-kommissionier-prio-nach-ruecknahme. Eine bewusst gleiche Prio (zwei FAs gleich dringend) ist erlaubt.'
   Quelle='2026-11-13-v1-99-17-doppelte-kommissionier-prio' }
@{ Id='Pruefung/BeschichtungQuelleKombination'; Version='1.99.25'; Typ='Pruefung'; Firma='beide'; Phase='Haltepunkt'; Ausfuehrung='anzeigen'
   Pruefung='SELECT COUNT(*) FROM dbo.AppSettings a CROSS JOIN (SELECT COALESCE((SELECT LOWER(LTRIM(RTRIM([Value]))) FROM dbo.ServiceSettings WHERE [Key]=N''ProduktionsauftragHierarchisch''), N''false'') m) s WHERE (a.[Key]=N''BeschichtungKennzeichenQuelle'' AND ((a.[Value]=N''Artikelkategorie'' AND s.m=N''true'') OR (a.[Value]=N''FaStruktur'' AND s.m<>N''true''))) OR (a.[Key]=N''BeschichtungTerminQuelle'' AND a.[Value]=N''View'' AND s.m<>N''true'') OR (a.[Key]=N''BeschichtungKennzeichenQuelle'' AND ISNULL(a.[Value],N'''') NOT IN (N'''',N''Automatisch'',N''Artikelkategorie'',N''FaStruktur'')) OR (a.[Key]=N''BeschichtungTerminQuelle'' AND ISNULL(a.[Value],N'''') NOT IN (N'''',N''Automatisch'',N''Berechnet'',N''View''))'
   Text='Beschichtungsquelle passt nicht zum Auftragsmodell oder ist kein gueltiger Wert, wirkt wie Automatisch; unter /Settings korrigieren.'
   Quelle='2026-11-16-v1-99-25-beschichtungslogik-je-standort' }
@{ Id='Pruefung/BeschichtungPositionOhneTextFlach'; Version='1.99.34'; Typ='Pruefung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'; Braucht=@('SQL/157')
   Pruefung='SELECT COUNT(*) FROM dbo.BeschichtungsauftragPositionen p WHERE p.PositionText IS NULL AND COALESCE((SELECT LOWER(LTRIM(RTRIM([Value]))) FROM dbo.ServiceSettings WHERE [Key]=N''ProduktionsauftragHierarchisch''), N''false'') <> N''true'''
   Text='Beschichtungsauftrag-Positionen ohne Positionstext an einem System mit flachem Auftragsmodell: die Vorschlagsliste erkennt sie nicht als vergeben. Positionen pruefen (nur moeglich, wenn vor v1.99.34 Auftraege ohne FA-Struktur angelegt wurden).'
   Quelle='2026-11-17-v1-99-34-beschichtung-ake-stueckliste' }
@{ Id='Pruefung/RolleSchachtler'; Version='1.99.35'; Typ='Pruefung'; Firma='beide'; Phase='Haltepunkt'; Ausfuehrung='Reparatur'
   Pruefung='SELECT COUNT(*) FROM (VALUES (N''schachtler'')) v(k) WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.[Key] = v.k)'
   Reparatur='INSERT INTO dbo.Roles ([Key], [Name], [Description], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
SELECT v.k, v.n, v.d, 1, (SELECT ISNULL(MAX(SortOrder), 0) FROM dbo.Roles) + v.o, SYSDATETIME(), N''Update-Skript'', LEFT(SUSER_SNAME(), 200)
FROM (VALUES (N''schachtler'', N''Schachtler (Blechfertigung)'', N''Blechfertigung - alle BF-Auftraege sehen, bearbeiten, freigeben, fertig melden, stornieren.'', 1)) v(k, n, d, o)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.[Key] = v.k);'
   Text='Rolle schachtler fehlt (kein Seed). Zuweisen an die Schachtler bleibt Hand-Schritt unter /Roles.'
   Quelle='2026-11-17-v1-99-35-blechfertigung-bf-auftraege' }
@{ Id='Pruefung/BfEmpfaengergruppe'; Version='1.99.35'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N''BlechfertigungAktiv'' AND LOWER([Value]) = N''true'') AND NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N''BfEmpfaengergruppeId'' AND LEN(LTRIM(RTRIM(ISNULL([Value], N'''')))) > 0) THEN 1 ELSE 0 END'
   Text='Blechfertigung an, aber keine Empfaengergruppe: Abschicken gesperrt. Gruppe mit bestellung.lagerteile@ideal-ake.at anlegen und unter /Settings > Blechfertigung waehlen.'
   Quelle='2026-11-17-v1-99-35-blechfertigung-bf-auftraege' }
@{ Id='Pruefung/BfCsvPfad'; Version='1.99.35'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''BfCsvAktiv'' AND LOWER(LTRIM(RTRIM([Value]))) = N''true'') AND NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''BfCsvPfad'' AND LEN(LTRIM(RTRIM([Value]))) > 0) THEN 1 ELSE 0 END'
   Text='BfCsvAktiv an, aber BfCsvPfad leer: es wird keine CSV geschrieben.'
   Quelle='2026-11-17-v1-99-35-blechfertigung-bf-auftraege' }
@{ Id='Pruefung/FeiertageIdeal'; Version='1.99.35'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Holidays WHERE [Date] >= CAST(GETDATE() AS date)) THEN 0 ELSE 1 END'
   Text='Keine kuenftigen Feiertage: Schachtel- und FE-Termine der BF-Auftraege ignorieren Feiertage und Betriebsurlaub. Stammdaten > Feiertage pflegen (Excel-Blatt Feiertage_Tab4).'
   Quelle='2026-11-17-v1-99-35-blechfertigung-bf-auftraege' }
@{ Id='Pruefung/LackierteilErkennungImportHook'; Version='1.99.25'; Typ='Pruefung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN COALESCE((SELECT LOWER(LTRIM(RTRIM([Value]))) FROM dbo.ServiceSettings WHERE [Key]=N''ProduktionsauftragHierarchisch''), N''false'') <> N''true'' AND COALESCE((SELECT LOWER(LTRIM(RTRIM([Value]))) FROM dbo.ServiceSettings WHERE [Key]=N''Sync:BomCacheEnabled''), N''false'') = N''true'' AND COALESCE((SELECT LOWER(LTRIM(RTRIM([Value]))) FROM dbo.ServiceSettings WHERE [Key]=N''Sync:CoatingDetectionEnabled''), N''false'') <> N''true'' THEN 1 ELSE 0 END'
   Text='Seit v1.99.25 setzt der Sage-Import das Lack-Kennzeichen neuer Auftraege nur mit Sync:CoatingDetectionEnabled. Hier ist BomCache an, Lackierteil-Erkennung aus: neue Auftraege bekommen KEIN Lack-Kennzeichen mehr. Unter /ServiceSettings einschalten, falls Lackierteile erkannt werden sollen.'
   Quelle='2026-11-16-v1-99-25-beschichtungslogik-je-standort' }

@{ Id='Pruefung/RolleFraesenpfadPflegen'; Version='1.99.72'; Typ='Pruefung'; Firma='beide'; Phase='Haltepunkt'; Ausfuehrung='Reparatur'
   Pruefung='SELECT COUNT(*) FROM (VALUES (N''fraesenpfad_pflegen'')) v(k) WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.[Key] = v.k)'
   Reparatur='INSERT INTO dbo.Roles ([Key], [Name], [Description], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
SELECT v.k, v.n, v.d, 1, (SELECT ISNULL(MAX(SortOrder), 0) FROM dbo.Roles) + v.o, SYSDATETIME(), N''Update-Skript'', LEFT(SUSER_SNAME(), 200)
FROM (VALUES (N''fraesenpfad_pflegen'', N''Fr'' + NCHAR(228) + N''sen-Pfad pflegen'', N''Fr'' + NCHAR(228) + N''sen-Pfad am Artikel '' + NCHAR(228) + N''ndern (Artikelinfo, Artikel bearbeiten); braucht zus'' + NCHAR(228) + N''tzlich Stammdaten lesen; sonst keine Rechte.'', 1)) v(k, n, d, o)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.[Key] = v.k);'
   Text='Rolle fraesenpfad_pflegen fehlt (kein Seed). Zuweisen an das persoenliche Konto des Pflegers (zusammen mit Stammdaten lesen) bleibt Hand-Schritt unter /Roles.'
   Quelle='2026-11-25-v1-99-72-bde-liste-isolierungsfraese-pfad-material' }
@{ Id='Pruefung/FraesenpfadOhneStammdatenLesen'; Version='1.99.72'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT COUNT(*) FROM dbo.UserRoles ur JOIN dbo.Roles r ON r.Id = ur.RoleId WHERE r.[Key] = N''fraesenpfad_pflegen'' AND NOT EXISTS (SELECT 1 FROM dbo.UserRoles u2 JOIN dbo.Roles r2 ON r2.Id = u2.RoleId WHERE u2.UserId = ur.UserId AND r2.[Key] IN (N''admin'', N''masterdata'', N''masterdata_read''))'
   Text='Benutzer mit Fraesen-Pfad pflegen ohne Stammdaten lesen erreichen die Artikelinfo nicht - unter /Roles zusaetzlich Stammdaten lesen zuweisen.'
   Quelle='2026-11-25-v1-99-72-bde-liste-isolierungsfraese-pfad-material' }
@{ Id='Pruefung/BlechteileNachleseAus'; Version='1.99.72'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''Sync:BlechteileFelderNachlesen'' AND LOWER([Value]) IN (N''true'', N''1'')) THEN 0 ELSE 1 END'
   Text='Blechteile-Nachlese aus: Arbeitsschritte/Material/Blechdicke am Artikel fehlen, Lagerbestell-Positionen erscheinen nicht an der Fraese. /ServiceSettings einschalten (liest nur aus Sage).'
   Quelle='2026-11-25-v1-99-72-bde-liste-isolierungsfraese-pfad-material' }
@{ Id='Pruefung/TeilelisteLagerbestellungenOhneWerkbank'; Version='1.99.72'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN COL_LENGTH(N''dbo.ProductionWorkplaces'', N''BdeTeilelisteLagerbestellungen'') IS NULL THEN 0 WHEN EXISTS (SELECT 1 FROM dbo.ProductionWorkplaces WHERE BdeTeilelisteLagerbestellungen = 1 AND BdeTeileliste = 1 AND BdeAktiv = 1) THEN 0 ELSE 1 END'
   Text='Keine Werkbank zeigt Lagerbestellungen in der Teileliste: an der Werkbank der Isolierungsfraese (Kuerzel IF) unter Werkbaenke > Bearbeiten "BDE-Teileliste" und "Lagerbestellungen in der Teileliste" einschalten.'
   Quelle='2026-11-25-v1-99-72-bde-liste-isolierungsfraese-pfad-material' }

# ---------------------------------------------------------------- Daten und Einmal-Skripte (nur mit Eingabe am Zielserver, ADR 0023)
@{ Id='Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand'; Version='1.88.0'; Typ='Einmalig'; Firma='beide'; Phase='Haltepunkt'; Ausfuehrung='Trockenlauf'
   Datei='Einmalig\2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand.sql'; Braucht=@('SQL/133','SQL/141')
   Voraussetzung='Web gestoppt, SQL/133 und SQL/141 da, DB-Backup'
   Spur='IF OBJECT_ID(N''dbo.PickingOrders'', N''U'') IS NULL SELECT 0 ELSE SELECT COUNT(*) FROM dbo.PickingOrders WHERE CreatedBy = N''Migration-KO-Altbestand'''
   Text='Trockenlauf automatisch (Zaehlung und Liste "Altbestand pruefen" ins Protokoll). TREFFER 0 -> entfaellt. Sonst Zaehlung freigeben, SCHARF eintippen (Backup aus diesem Lauf Pflicht); ohne SCHARF Abbruch, Web und Dienst bleiben gestoppt.'
   Quelle='2026-11-08-v1-88-0-kommissionierauftraege; 2026-11-08-v1-89-0-kommissionierauftraege-nachbesserung' }
@{ Id='Einmalig/2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste'; Version='1.99.32'; Typ='Einmalig'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='Trockenlauf'
   Datei='Einmalig\2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste.sql'; Braucht=@('SQL/154')
   Voraussetzung='Dienst und Web >= 1.99.32 laufen, Option BDE-Teileliste an den Start-Werkbaenken gesetzt, DB-Backup'
   Text='Trockenlauf automatisch (Zaehlung je Kuerzel und Meldeliste gebuchter Arbeitsgaenge ins Protokoll). TREFFER 0 -> entfaellt (z. B. Option noch nirgends an: dann nach dem Setzen erneut anstossen). Sonst Zaehlung freigeben, SCHARF eintippen (Backup aus diesem Lauf Pflicht); zweiter Trockenlauf muss 0 liefern. Gebuchte Arbeitsgaenge bleiben stehen - Meldeliste an den Fachbereich.'
   Quelle='2026-11-17-v1-99-32-bde-teileliste-blechfertigung' }
@{ Id='Einmalig/2026-11-18_v1.99.42_BeschichtungsauftragKreditorNachtragen'; Version='1.99.42'; Typ='Einmalig'; Firma='beide'; Phase='Nachher'; Ausfuehrung='Trockenlauf'
   Datei='Einmalig\2026-11-18_v1.99.42_BeschichtungsauftragKreditorNachtragen.sql'; Braucht=@('SQL/161')
   Voraussetzung='Web >= 1.99.41, Beschichter-Kreditoren gekennzeichnet (Pruefung BeschichterKreditoren), DB-Backup'
   Text='Trockenlauf automatisch (Zaehlung, Liste Auftrag -> Kreditor und Meldeliste ohne/mehrdeutigen Beschichter ins Protokoll). TREFFER 0 -> entfaellt. Sonst Zaehlung freigeben, SCHARF eintippen (Backup aus diesem Lauf Pflicht); zweiter Trockenlauf muss 0 liefern. Meldeliste: Kreditor im Auftrag waehlen (auch in Uebergeben moeglich).'
   Quelle='2026-11-18-v1-99-42-nachbesserungen' }
@{ Id='SQL/108'; Version='1.79.0'; Typ='Daten'; Firma='beide'; Phase='VorStart'; Ausfuehrung='Transaktion'
   Datei='108_RemoveAppSettingsRueckmeldungOhneLeser.sql'
   Pruefabfrage='SELECT [Key],[Value] FROM [dbo].[AppSettings] WHERE [Key] IN (N''OseonRueckmeldungAktiv'', N''SageRueckmeldungAktiv'', N''StammdatenADGruppe'')'
   Spur='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] IN (N''OseonRueckmeldungAktiv'', N''SageRueckmeldungAktiv'', N''StammdatenADGruppe'')) THEN 0 ELSE 1 END'
   Text='P-4: Werte oben stehen im Protokoll. Loescht Settings-Zeilen ohne Leser (idempotent); in einer Transaktion, danach uebernehmen/verwerfen. Spur 1 = nichts zu tun.'
   Quelle='2026-11-08-v1-79-0-bereinigungen-nach-buendel; 2026-11-08-v1-94-0-menueleiste-umbruch-kontrast' }
@{ Id='SQL/147'; Version='1.99.14'; Typ='Daten'; Firma='beide'; Phase='VorStart'; Ausfuehrung='Transaktion'
   Datei='147_RemoveAppSettingSageSdataFaAnsichtAktiv.sql'
   Pruefabfrage='SELECT [Key],[Value] FROM [dbo].[AppSettings] WHERE [Key] = N''SageSdataFaAnsichtAktiv'''
   Spur='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N''SageSdataFaAnsichtAktiv'') THEN 0 ELSE 1 END'
   Text='Loescht den entfallenen Schalter (idempotent); in einer Transaktion, danach uebernehmen/verwerfen. Spur 1 = nichts zu tun.'
   Quelle='2026-11-13-v1-99-14-sdata-teil-2-userfelder-stueckliste' }
@{ Id='SQL/110'; Version='1.87.0'; Typ='Daten'; Firma='beide'; Phase='Nachher'; Ausfuehrung='Trockenlauf'; Braucht=@('SQL/109')
   Datei='110_BackfillStockMovementProductionOrderId.sql'
   Voraussetzung='nach Web- und Dienst-Start'
   Text='Trockenlauf automatisch (Ergebnis als Datei im Log-Ordner). TREFFER 0 -> entfaellt; sonst SCHARF mit Backup aus diesem Lauf, danach zweiter Trockenlauf muss 0 liefern.'
   Quelle='2026-11-08-v1-87-0-lagerbuchungen-fa-feld' }
@{ Id='Einmalig/2026-10-07_v1.47.4_RemoveParentWorkOperationsFromChildSubFaTokens'; Version='1.47.4'; Typ='Einmalig'; Firma='IDEAL'; System=@('Test'); Phase='VorStart'; Ausfuehrung='Transaktion'
   Datei='Einmalig\2026-10-07_v1.47.4_RemoveParentWorkOperationsFromChildSubFaTokens.sql'
   Voraussetzung='nach dem Dienst-Deploy >= v1.47.4'
   Text='Laut Gerald 2026-11-11 auf keinem System gelaufen: an IDEAL-Test mit diesem Deploy (kein Trockenlauf-Schalter; Skriptkopf lesen). Laeuft in einer Transaktion: Zaehlung und Meldeliste pruefen, dann uebernehmen oder verwerfen. Meldeliste an den Fachbereich. Entfaellt an IDEAL-Prod (Neuinstallation v1.71.1).'
   Quelle='2026-10-07-v1-47-4-arbeitsgang-erkennung-kind-sub-fa' }
@{ Id='Einmalig/2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel'; Version='1.60.0'; Typ='Einmalig'; Firma='IDEAL'; System=@('Test'); Phase='Nachher'; Ausfuehrung='anzeigen'
   Datei='Einmalig\2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel.sql'
   Voraussetzung='Sync:ArticlesViewName gesetzt, ein Artikel-Lauf, P7 mit Treffern, @Behalten-Liste'
   Spur='SELECT CASE WHEN OBJECT_ID(N''dbo.Articles_Bereinigung00er_Sicherung'') IS NULL THEN 0 ELSE 1 END'
   Text='Laut Gerald 2026-11-11 auf keinem System gelaufen: nur IDEAL-Test und nur wenn P7 Treffer zeigt. Zuerst Listenmodus @Loeschen = 0 in SSMS, dann entscheidet Gerald (bleibt Hand-Schritt: @Behalten-Liste, P7 auf Sage). Danach fragt das Skript, ob es lief (Marker VonHand). Entfaellt an IDEAL-Prod.'
   Quelle='2026-10-15-v1-60-0-ideal-sage-views-umstellung' }

@{ Id='Einmalig/2026-11-25_v1.99.72_FraesenPfad_Erstimport'; Version='1.99.72'; Typ='Einmalig'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='Trockenlauf'
   Datei='Einmalig\2026-11-25_v1.99.72_FraesenPfad_Erstimport.sql'; Braucht=@('SQL/164')
   Voraussetzung='Web >= 1.99.72, DB-Backup'
   Spur='SELECT COUNT(*) FROM dbo.Articles WHERE ModifiedBy LIKE N''Einmal-Skript Fr_sen-Pfad'''
   Text='Uebernimmt die Fraesen-Pfade aus WICAM-Pfade.xlsx je eindeutigem Matchcode in Articles.CamPath (nur leere; Y:\ -> \\idealfs1\Daten$\Produktion\CAM\Format4). Im Prod-Fenster an IDEAL SCHARF ausfuehren (Entscheid 7B, v1.99.81). Trockenlauf automatisch (Zaehlung, Schreibliste, Meldeliste ins Protokoll). TREFFER 0 -> entfaellt. Sonst Zaehlung freigeben, SCHARF eintippen (Backup aus diesem Lauf Pflicht); zweiter Trockenlauf muss 0 liefern. Unbekannte, mehrdeutige, doppelte Matchcodes und Zeilen ohne gueltigen Pfad stehen in der Meldeliste - von Hand pflegen.'
   Quelle='2026-11-25-v1-99-72-bde-liste-isolierungsfraese-pfad-material' }

# ---------------------------------------------------------------- Ordner und Rechte (Schritt 5a, jeder Lauf) ------------------
@{ Id='v1.96.0-FotosOrdner'; Version='1.96.0'; Typ='Ordner'; Firma='beide'; Phase='Vorher'; Ausfuehrung='automatisch'
   Pfad='{Web}\App_Data\Fotos\Kommissionierung'; RechtAuf='{Web}\App_Data'; Identitaet='Web'; Recht='Aendern'; Pflicht=$true; Herstellen=$true
   Text='Kommissionierfotos liegen seit v1.30.4/v1.96.0 in App_Data; ohne Schreibrecht startet die App nicht.'
   Quelle='2026-11-11-v1-96-0-vorwaerts-merge-main-hotfixes' }
@{ Id='v1.99.14-DataProtectionDienst'; Version='1.99.14'; Typ='Ordner'; Firma='beide'; Phase='Vorher'; Ausfuehrung='automatisch'
   Pfad='{DataProtection}'; Identitaet='Dienst'; Recht='Lesen'; Pflicht=$true; Herstellen=$true
   Text='Der Dienst entschluesselt das SData-Kennwort mit dem Schluesselring der Web-App und braucht Leserecht.'
   Quelle='2026-11-13-v1-99-14-sdata-teil-2-userfelder-stueckliste' }

# ---------------------------------------------------------------- Vorher (Anzeige vor jeder Aenderung) -------------------------
@{ Id='v1.99.35-BfPoolRecht'; Version='1.99.35'; Typ='Info'; Firma='IDEAL'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='Dienstkonto braucht Schreib- und Aenderungsrecht auf den WiCAM-Pool \\idealcam.ideal.ideal-ake.at\fertigung\u\pn\apools\ (P-7 der Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv).'
   Quelle='2026-11-17-v1-99-35-blechfertigung-bf-auftraege' }
@{ Id='v0-Backup'; Version='0.0.0'; Typ='Info'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='DB-Backup vor dem Deploy (Pflicht bei 90, 95, 109/110, 133/Altbestand, ADR 0004); das Skript bietet es in Schritt 2 an.'; Quelle='ADR 0004' }
@{ Id='v1.99.0-PB1'; Version='1.99.0'; Typ='Info'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='P-B1 vor dem ersten Lauf v1.99.0 (SSMS auf {Server}, als ausfuehrendes Konto): SELECT SERVERPROPERTY(''ProductVersion''); SELECT * FROM sys.dm_os_file_exists(CAST(SERVERPROPERTY(''InstanceDefaultBackupPath'') AS NVARCHAR(4000))); SELECT HAS_PERMS_BY_NAME(N''{DB}'', ''DATABASE'', ''ALTER'') AS AlterDb; AlterDb = 0 -> RCSI-Reparatur in 4a mit n beantworten (sonst Abbruch bei gestopptem Web und Dienst).'
   Quelle='2026-11-11-v1-99-0-update-skript-prueft-und-fuehrt-aus' }
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
@{ Id='v1.99.28-AnmeldungKennwort'; Version='1.99.28'; Typ='Info'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='Ab v1.99.28 meldet das Formular nur mit Kennwort an; vom Admin vergebene Kennwoerter (Erstkennwort) muss der Benutzer bei der ersten Anmeldung aendern. Vorher P-1 ausfuehren; AD-Benutzer ohne Kennwort nur noch per Windows-Anmeldung. admin ohne Kennwort bekommt beim ersten Start das Standard-Erstkennwort wms-admin mit Pflichtwechsel - sofort danach als admin anmelden; wer das nicht will, setzt admin vorher ein Kennwort.'
   Quelle='2026-11-16-v1-99-28-sicherheit-anmeldung-installer' }
@{ Id='v1.84.0-Einkauf'; Version='1.84.0'; Typ='Info'; Firma='AKE'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='Einkauf informieren: kein WE-Beleg mit Lagerbuchung in Sage (Doppelbestand).'; Quelle='2026-11-08-v1-84-0-wareneingang-aus-sage-bestellungen' }
@{ Id='v1.93.0-Zaehlung'; Version='1.93.0'; Typ='Info'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='Zaehlabfrage Meldebestand je Standort vor PROD (Spec meldebestand-mail).'; Quelle='2026-11-08-v1-93-0-meldebestand-mail-bestandsregel' }
@{ Id='v1.96.0-FotosZaehlen'; Version='1.96.0'; Typ='Info'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='Das Skript zaehlt vor dem Kopieren die Dateien in {Web}\wwwroot\Fotos\Kommissionierung und sucht nach dem Web-Start im Log "Kommissionierfotos verschoben: n, uebersprungen: m" (n + m = Zaehlung).'
   Quelle='2026-11-11-v1-96-0-vorwaerts-merge-main-hotfixes' }

# ---------------------------------------------------------------- Nachher (Nachlauf-Liste) -------------------------------------
@{ Id='v1.31.0-IDEAL-Schalter'; Version='1.31.0'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Sync:HierarchicalFaEnabled, FaHierarchyKommissionierlistenAktiv/...BeschichtungAktiv/...VormontageAktiv; SageConnection; View-Namen ohne Schema-Praefix (RCSI prueft Pruefung/RCSI).'; Quelle='2026-08-10-v1-31-0-ideal-teile-1-5' }
@{ Id='v1.40.0-Matchcode'; Version='1.40.0'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Matchcode erscheint erst nach dem naechsten Artikel-Lauf.'; Quelle='2026-09-15-v1-40-0-matchcode-artikelstamm' }
@{ Id='v1.44.0-Werkbank'; Version='1.44.0'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='DryRun; erst Sync:ProductionWorkplaceSyncEnabled, dann Sync:WorkOperationStructureDetectionEnabled.'; Quelle='2026-09-22-v1-44-0-ideal-bde-arbeitsgaenge-werkbank-anlage' }
@{ Id='v1.51.0-Rolle'; Version='1.51.0'; Typ='Einstellung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Rollen beschichtungsauftrag (Lesen) und beschichtungsauftrag_schreiben unter /Roles zuweisen (Anlegen: Pruefung/RollenBeschichtungsauftrag, Firma beide; IDEAL-Prod hat sie aus der Installation).'
   Quelle='2026-10-12-v1-51-0-beschichtungsauftraege-oberflaeche' }
@{ Id='v1.56.0-Info'; Version='1.56.0'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Anwender-Info: Beschichtung im Menue.'; Quelle='2026-10-13-v1-56-0-menue-beschichtung-hilfe' }
@{ Id='v1.57.0-PT1'; Version='1.57.0'; Typ='Info'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='P-T1 inkl. Kollisionsabfrage vor der AKE-Abnahme.'; Quelle='2026-10-13-v1-57-0-bde-terminal-arbeitsgang-status' }
@{ Id='v1.58.1-Info'; Version='1.58.1'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Anwender-Info: "oder" = ;  Fehlteile !a;b'; Quelle='2026-10-13-v1-58-1-suchsyntax-spaltenfilter-etappe-b' }
@{ Id='v1.60.0-IDEAL-Views'; Version='1.60.0'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='P1-P5b auf Sage (von Hand, das Skript liest Sage nicht); Sync:ArticlesViewName prueft Pruefung/ArticlesViewName.'; Quelle='2026-10-15-v1-60-0-ideal-sage-views-umstellung' }
@{ Id='v1.61.0-IDEAL-Altzeilen'; Version='1.61.0'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Alte OperationConfig-Zeilen erst nach dem ersten Abgleich-Lauf loeschen.'; Quelle='2026-10-16-v1-61-0-arbeitsgang-abgleich-sage-view' }
@{ Id='v1.63.0-IDEAL-DryRun'; Version='1.63.0'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='P1-P3/P6 + DryRun vor dem Scharfschalten.'; Quelle='2026-10-19-v1-63-0-bde-stufe-1-werkbaenke-ag-fertigmeldung' }
@{ Id='v1.63.0-Info'; Version='1.63.0'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Werker-Info: Fortsetzen an fremder Werkbank wird abgewiesen.'; Quelle='2026-10-19-v1-63-0-bde-stufe-1-werkbaenke-ag-fertigmeldung' }
@{ Id='v1.64.0-MaxHauptFa'; Version='1.64.0'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='BdeTerminalMaxHauptFa entfaellt seit 1.99.78 (in /Settings als verwaist sichtbar) - nichts einstellen.'; Quelle='2026-10-19-v1-64-0-bde-terminal-liste-scan-ohne-ag-prio' }
@{ Id='v1.67.0-IDEAL-Vault'; Version='1.67.0'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='"Vault-Links anzeigen" aus; enaio entscheidet Gerald.'; Quelle='2026-10-22-v1-67-0-vault-enaio-links-konfigurierbar' }
@{ Id='v1.68.0-IDEAL-Qr'; Version='1.68.0'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='QrErstesFeld = Matchcode erst nach TS-101.'; Quelle='2026-10-27-v1-68-0-qr-matchcode' }
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
@{ Id='v1.99.4-SageWe'; Version='1.99.4'; Typ='Einstellung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='SageWareneingangBelegAktiv (/ServiceSettings) erst nach dokumentiertem Schreibtest und Abnahme TS-116.14 einschalten; IDEAL bleibt aus (kein SData-Endpunkt). Danach sind nur Sage-Plaetze mit Sage-Buchung erlaubt als Ziel buchbar.'
   Quelle='2026-11-11-v1-99-4-wareneingang-sage-we-beleg' }
@{ Id='v1.99.4-Lieferschein'; Version='1.99.4'; Typ='Einstellung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='WareneingangLieferscheinPflicht (/Settings > Lager und Buchungen) nach Absprache mit Lager und Einkauf setzen (Standard aus = Lieferschein-Nr. optional).'
   Quelle='2026-11-11-v1-99-4-wareneingang-sage-we-beleg' }
@{ Id='v1.99.4-EinkaufWe'; Version='1.99.4'; Typ='Info'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Einkauf informieren: Wareneingaenge mit Status An Sage uebergeben nicht mehr von Hand schliessen; Liste Wareneingaenge an Sage; Fehler nach Pruefung in Sage erneut senden.'
   Quelle='2026-11-11-v1-99-4-wareneingang-sage-we-beleg' }
@{ Id='v1.99.0-Info'; Version='1.99.0'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Betrieb: Das Update-Skript prueft Kommissionierbereiche, RCSI, Rollen und DUMMY-Artikel und repariert nur nach Bestaetigung; Daten-/Einmal-Skripte laufen nur nach SCHARF bzw. uebernehmen; Marker in dbo.DeployScriptRuns.'
   Quelle='2026-11-11-v1-99-0-update-skript-prueft-und-fuehrt-aus' }
@{ Id='v1.99.7-SageWeWiederholen'; Version='1.99.7'; Typ='Einstellung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Neu in /ServiceSettings > Sage-Lagerbuchung: Sync:SageWareneingangMaxWiederholungen (Standard 3, 0 = nur Erneut senden) und Sync:SageWareneingangWiederholAbstandMinuten (Standard 15). Wirkt nur mit SageWareneingangBelegAktiv; vor jeder Wiederholung sucht der Dienst den Beleg in Sage (nur lesend). Werte bestaetigen oder anpassen.'
   Quelle='2026-11-12-v1-99-7-wareneingang-e5-auto-retry' }
@{ Id='v1.99.7-EinkaufKlaeren'; Version='1.99.7'; Typ='Info'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Einkauf/Lager informieren: Liste heisst jetzt Lager -> Sage-Wareneingaenge (eigener Block mit Sage-Lagerbuchungen). Fehler wiederholt der Dienst selbst; Status Klaeren = moeglicher Beleg in Sage ohne WMS-Kennung: in Sage pruefen, dann Als uebergeben markieren oder Erneut senden. Ist der Sage-Wareneingang aus, gehen offene Eintraege auf Klaeren und werden nie automatisch nachgeschickt.'
   Quelle='2026-11-12-v1-99-7-wareneingang-e5-auto-retry' }
@{ Id='v1.99.9-SystemKuerzel'; Version='1.99.9'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Neu in /ServiceSettings > Sage-Lagerbuchung: Sync:SageSystemKuerzel (System-Kuerzel fuer Sage-Kennungen). Kuerzel je Testsystem setzen (DEV, AT = AKE-Test, ATI = AKE Test-IDEAL, IT = IDEAL-Test), Produktion LEER lassen. Wirkt nur auf neue Sendungen (WE#<K>#<Id># bzw. SM#<K>#<id>#); gesendete Eintraege behalten ihre Kennung.'
   Quelle='2026-11-12-v1-99-9-sage-system-kuerzel' }
@{ Id='v1.99.10-Lieferverzug'; Version='1.99.10'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Neu in /ServiceSettings > Lieferverzug-Mail (je Standort): Hauptschalter (Standard aus), Testbetrieb (Standard an), Einkauf-Empfaenger, Tage nach Liefertermin, Uhrzeit, Wiederholung, Grenze je Lauf, Absenderadresse und Antwortadresse der Kreditor-Mail, Absender-Firma (AKE/IDEAL). Erst Einkauf-Empfaenger und Absender-Firma eintragen, dann mit Testbetrieb einschalten. IDEAL bleibt aus bis zur SData-Schnittstelle fuer offene Bestellungen. Testbetrieb erst nach Freigabe durch den Einkauf und Pruefabfrage P-M3 ausschalten.'
   Quelle='2026-11-12-v1-99-10-lieferverzug-mail' }
@{ Id='v1.99.10-LieferverzugInfo'; Version='1.99.10'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Einkauf informieren: taegliche Sammelliste verzoegerter Bestellpositionen; im Testbetrieb kommen auch die Lieferanten-Mails beim Einkauf an (Betreff TESTBETRIEB). Vor dem Echtbetrieb: Mail-Relay fuer externe Empfaenger und die Absenderadresse der Kreditor-Mail mit der IT klaeren (P-M4); Kreditor-Felder Keine Erinnerungsmails / Adresse fuer Erinnerungen unter Stammdaten > Kreditoren pflegen.'
   Quelle='2026-11-12-v1-99-10-lieferverzug-mail' }
@{ Id='v1.99.11-WareneingangCsv'; Version='1.99.11'; Typ='Einstellung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Wareneingang-CSV: zuerst Dienstkonto pruefen (P-C1, LocalSystem = Computerkonto DOMAENE/SERVER$) und Recht Aendern auf Freigabe und NTFS des Zielordners setzen (P-C2), dann /ServiceSettings > Wareneingang-CSV: Pfad (UNC-Pfad server/freigabe/ordner mit Backslashes) setzen, WareneingangCsvAktiv einschalten (wartende Vorgaenge mit Barcode werden sofort geschrieben), zuletzt /Settings > Lager und Buchungen: WareneingangBarcodeAktiv. Wiederholung nach Schreibfehler: Sync:WareneingangCsvMaxWiederholungen (3) / Sync:WareneingangCsvWiederholAbstandMinuten (15). IDEAL bleibt aus.'
   Quelle='2026-11-12-v1-99-11-wareneingang-barcode-csv' }
@{ Id='v1.99.11-LagerBarcode'; Version='1.99.11'; Typ='Info'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Lager informieren: Wareneingang zu Bestellung hat ein Feld Barcode (Lieferschein-Aufkleber); die CSV entsteht erst nach der Uebergabe an Sage. Stand in Lager > Sage-Wareneingaenge, Spalte CSV.'
   Quelle='2026-11-12-v1-99-11-wareneingang-barcode-csv' }
@{ Id='v1.99.14-SDataZugang'; Version='1.99.14'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Service-Einstellungen > Sage-Lagerbuchung: SData: Benutzer und SData: Kennwort eintragen (Kennwort wird verschluesselt gespeichert). IDEAL erst mit SData-Endpunkt. Ist im Web DataProtection:KeysPath gesetzt, braucht die Dienst-appsettings.json denselben Wert.'
   Quelle='2026-11-13-v1-99-14-sdata-teil-2-userfelder-stueckliste' }
@{ Id='v1.99.14-SageTestInfo'; Version='1.99.14'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Menuepunkt FA-Struktur (Sage live) entfaellt; Admins: Einstellungen > Reiter Sage-Test. Anwender informieren, falls sie die Seite genutzt haben.'
   Quelle='2026-11-13-v1-99-14-sdata-teil-2-userfelder-stueckliste' }
@{ Id='v1.99.15-Rahmenabrufe'; Version='1.99.15'; Typ='Info'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Einkauf und Lager informieren: Rahmenabrufe (EBZ) stehen jetzt in Offene Bestellungen und Wareneingang. Offen = Abrufmenge minus im WMS erfasst; in Sage direkt gebuchte Abruf-Lieferungen zieht das WMS nicht ab. Abrufe sind auch in den Lieferverzug-Mails. Abrufe gehen als Lagerbuchung, Einkauf schliesst in Sage von Hand (kein Sage-Wareneingang, auch wenn SageWareneingangBelegAktiv an ist; Barcode/CSV fuer Abrufe gesperrt). Nach dem Deploy einen Abgleich Offene Bestellungen (Sage) laufen lassen.'
   Quelle='2026-11-13-v1-99-15-wareneingang-teil-3-rahmenabrufe' }
@{ Id='v1.99.19-LieferverzugSchwellen'; Version='1.99.19'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='/ServiceSettings > Lieferverzug-Mail: Sammelliste Tage nach Liefertermin = 1 (Einkauf, Till), Kreditor-Mail Tage nach Liefertermin = 3 (neu), Altersgrenze = 180 Tage (neu; aeltere Positionen nur unter Zu klaeren, nie an Lieferanten). Kreditor-Mail auch fuer Rahmenabrufe bleibt aus bis P-L4.'
   Quelle='2026-11-13-v1-99-19-lieferverzug-nachbesserung-keyuser' }
@{ Id='Pruefung/LieferverzugSammellisteTage'; Version='1.99.19'; Typ='Pruefung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''Lieferverzug:TageNachTermin'' AND LTRIM(RTRIM([Value])) = N''1'') THEN 0 ELSE 1 END'
   Text='Lieferverzug: Sammelliste steht nicht auf 1 Tag nach Liefertermin (Vorgabe Einkauf). /ServiceSettings > Lieferverzug-Mail pruefen.'
   Quelle='2026-11-13-v1-99-19-lieferverzug-nachbesserung-keyuser' }
@{ Id='Pruefung/LieferverzugAdressenAKE'; Version='1.99.19'; Typ='Pruefung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN (SELECT COUNT(*) FROM dbo.ServiceSettings WHERE [Key] IN (N''Lieferverzug:KreditorAbsender'', N''Lieferverzug:AntwortAdresse'') AND LOWER(LTRIM(RTRIM([Value]))) = N''einkauf@ideal-ake.at'') = 2 THEN 0 ELSE 1 END'
   Text='Lieferverzug AKE: Absender und Antwortadresse der Kreditor-Mail nicht Einkauf@ideal-ake.at. Setzen in /ServiceSettings; Relay fuer externe Empfaenger mit der IT klaeren (P-M4).'
   Quelle='2026-11-13-v1-99-19-lieferverzug-nachbesserung-keyuser' }
@{ Id='Pruefung/LieferverzugAdressenIDEAL'; Version='1.99.19'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefung='SELECT CASE WHEN (SELECT COUNT(*) FROM dbo.ServiceSettings WHERE [Key] IN (N''Lieferverzug:KreditorAbsender'', N''Lieferverzug:AntwortAdresse'') AND LOWER(LTRIM(RTRIM([Value]))) = N''einkauf_ideal@ideal-ake.at'') = 2 THEN 0 ELSE 1 END'
   Text='Lieferverzug IDEAL: Absender und Antwortadresse der Kreditor-Mail nicht einkauf_ideal@ideal-ake.at. Setzen in /ServiceSettings; Relay P-M4.'
   Quelle='2026-11-13-v1-99-19-lieferverzug-nachbesserung-keyuser' }
@{ Id='v1.99.19-LieferverzugInfo'; Version='1.99.19'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Einkauf informieren: Sammelliste ab 1 Tag, Lieferanten erst ab 3 Tagen; Positionen mit Liefertermin aelter als 180 Tage stehen unter Zu klaeren und gehen nie an Lieferanten - bitte in Sage bereinigen. Rahmenabrufe gehen bis auf Weiteres nicht an Lieferanten. Neue Spalte Lief.-Art.-Nr. in beiden Mails: nur mit Sync:ArtikelLieferantEnabled gefuellt, sonst leer.'
   Quelle='2026-11-13-v1-99-19-lieferverzug-nachbesserung-keyuser' }
@{ Id='v1.99.20-EinkaufAktivieren'; Version='1.99.20'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Bestellvorschlaege: EinkaufAktiv bleibt aus. AKE: erst nach erfolgreichen Laeufen Artikel (SData), Kreditoren, Artikel-Lieferanten und Offene Bestellungen einschalten und Rolle einkauf zuweisen. IDEAL: aus lassen bis zur SData-Schnittstelle.'
   Quelle='2026-11-13-v1-99-20-einkauf-bestellvorschlaege' }
@{ Id='v1.99.23-SeitenNeuLaden'; Version='1.99.23'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Offene Seiten Wareneingang zu Bestellung und Leitstand (auch am Handgeraet) neu laden: Seiten von vor dem Update werden mit Seite ist veraltet abgewiesen, nichts wird gebucht.'
   Quelle='2026-11-14-v1-99-23-schutz-doppel-absenden' }
@{ Id='v1.99.25-Beschichtungsquelle-Info'; Version='1.99.25'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Neue Einstellungen Quelle Beschichtet-Kennzeichen / Quelle Beschichtungstermin (/Settings > Termine und Beschichtung) stehen auf Automatisch. AKE: Verhalten unveraendert. IDEAL mit ProduktionsauftragHierarchisch an: FA-Liste, Leitstand und Arbeitsliste zeigen Start_Beschichtung aus der Sage-View, Haken und Termin nur an Auftraegen mit Beschichtungs-Kennzeichen. Die Lackierteil-Erkennung nach dem Sage-Import laeuft nur noch bei Sync:CoatingDetectionEnabled.'
   Quelle='2026-11-16-v1-99-25-beschichtungslogik-je-standort' }
@{ Id='v1.99.26-IDEAL-FaStrukturVergleich'; Version='1.99.26'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Sync:Quelle:FaStruktur = Vergleich setzen (/ServiceSettings), Lauf FA-Hierarchie abwarten, Protokoll Sage-Quellenvergleich pruefen. SData erst nach Freigabe (Spec 2026-11-16-fa-struktur-ideal-ueber-sdata, Rueckfragen 4/5); bis dahin gesperrt, wirkt wie View.'
   Quelle='2026-11-16-v1-99-26-fa-struktur-ideal-sdata' }
@{ Id='v1.99.26-Info'; Version='1.99.26'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Sage-Test > Stueckliste live: Artikel-Modus heisst jetzt Ressourcenliste (Vorlage); FA-Modus zeigt auch beendete Haupt-FAs.'
   Quelle='2026-11-16-v1-99-26-fa-struktur-ideal-sdata' }
@{ Id='v1.99.28-AnwenderInfo'; Version='1.99.28'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Anwender-Info: Anmeldung am Formular nur noch mit Kennwort; wer vom Administrator ein Erstkennwort bekommt, legt bei der ersten Anmeldung ein eigenes fest; Terminals/Handgeraete mit dem neuen Kennwort anmelden; Domaenen-PCs unveraendert per Windows.'
   Quelle='2026-11-16-v1-99-28-sicherheit-anmeldung-installer' }
@{ Id='v1.99.29-WareneingangCsvFormat'; Version='1.99.29'; Typ='Info'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Wareneingang-CSV: Format ohne Kopfzeile, Erfasser = Windows-Kontoname ohne Domaene - das ist das enaio-Zielformat.'
   Quelle='2026-11-16-v1-99-29-wareneingang-csv-enaio' }
@{ Id='v1.99.30-LetzteBestellungUhrzeiten'; Version='1.99.30'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Letzte Bestellung (Einkauf) laeuft jetzt nur zu Sync:LetzteBestellungUhrzeitNacht/-Mittag (Default 3/12 Uhr, Gate Sync:ArtikelLieferantEnabled); ein verpasster Termin wird direkt nach dem Dienststart nachgeholt. Im Aktivitaetsprotokoll pruefen.'
   Quelle='2026-11-16-v1-99-30-bestellvorschlaege-nachbesserung' }
@{ Id='v1.99.30-ArtikelinfoBedarfe'; Version='1.99.30'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Artikelinfo: Verfuegbarer Bestand rechnet jetzt wie die Bestellvorschlaege (FA-Rest, Lagerbestellungen/Bedarfsmeldungen nur einmal). Lager/Produktion informieren; Zahlen koennen von der Vorversion abweichen.'
   Quelle='2026-11-16-v1-99-30-bestellvorschlaege-nachbesserung' }
@{ Id='v1.99.31-NachWeMinuten'; Version='1.99.31'; Typ='Einstellung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Neu: Sync:EinkaufBestellungenNachWeMinuten (/ServiceSettings, Default 2, 0 = aus). Nach einer Sage-Wareneingang-Uebergabe liest der Dienst die offenen Bestellungen sofort neu, danach hoechstens alle n Minuten. Wirkt nur mit SageWareneingangBelegAktiv und Sync:EinkaufBestellungenEnabled. Laufzeit im Aktivitaets-Protokoll beobachten (P-1); wer den Anstoss nicht will, vor dem Dienststart auf 0 setzen.'
   Quelle='2026-11-17-v1-99-31-offene-bestellungen-sofort-neu-lesen' }
@{ Id='v1.99.31-VorschauWochen'; Version='1.99.31'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='OffeneBestellungenVorschauWochen (/Settings) bleibt, belegt jetzt den Spaltenfilter Liefertermin vor: Montag der aktuellen KW bis Sonntag der N-ten KW (2 = aktuelle + naechste KW). Das Feld Wochen in der Filterkarte entfaellt. Ein gespeicherter Wert gilt weiter.'
   Quelle='2026-11-17-v1-99-31-offene-bestellungen-sofort-neu-lesen' }
@{ Id='v1.99.31-EinkaufAnsichten'; Version='1.99.31'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Rolle einkauf sieht jetzt Einkauf > Offene Bestellungen und Einkauf > Sage-Wareneingaenge (offen), beide nur lesen (Gate EinkaufAktiv). Wareneingang buchen nur mit Lager-Recht, Aktionen an Sage-Wareneingaengen nur mit Lager-Keyuser-Recht. Keine Rollenzuweisung noetig.'
   Quelle='2026-11-17-v1-99-31-offene-bestellungen-sofort-neu-lesen' }
@{ Id='v1.99.32-BeschichtungListen'; Version='1.99.32'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='FA-Liste, Kommissionier-Leitstand und Werkbank-Arbeitsliste nehmen Beschichtungstermin, Tooltip und Lack-Haken jetzt aus der Beschichtungsquelle (Nachtrag v1.99.25). Sichtbar nur mit ProduktionsauftragHierarchisch an: Leitstand und Arbeitsliste zeigen Start Beschichtung aus der Sage-View, Haken und Termin nur an Auftraegen mit Kennzeichen. Fertigung/AV informieren; AKE unveraendert.'
   Quelle='2026-11-17-v1-99-32-bde-teileliste-blechfertigung' }
@{ Id='v1.99.32-TeilelisteOption'; Version='1.99.32'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Neu: Werkbank-Option BDE-Teileliste (Stammdaten > Werkbaenke, Default aus, nur mit BDE aktiv und Kuerzel; wirkungslos bei BdeNurFaMeldung). An den Start-Werkbaenken der Blechfertigung setzen (mit Fertigung abstimmen). Danach das Einmal-Skript RemoveParentWorkOperationsFromLeafTokensTeileliste erneut anstossen, falls sein Trockenlauf vorher TREFFER 0 lieferte.'
   Quelle='2026-11-17-v1-99-32-bde-teileliste-blechfertigung' }
@{ Id='v1.99.32-TeilelisteInfo'; Version='1.99.32'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Werker an Teileliste-Werkbaenken melden per Personalnummer Teile einzeln, als Block oder summiert; nichts geht an Sage. Ruecknahme bis Schichtende (IDEAL ohne Schichtkalender: Ende des Meldetags), danach BDE-Admin unter BDE-Buchungen > Teilmeldungen. Erkennungslauf im Aktivitaets-Protokoll pruefen (Zaehler teileliste_blatt_uebersprungen).'
   Quelle='2026-11-17-v1-99-32-bde-teileliste-blechfertigung' }
@{ Id='v1.99.27-ArbeitsgangNummerPruefen'; Version='1.99.32'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Nach dem ersten Arbeitsgang-Abgleich /OperationConfigs ansehen: Spalte Sage-AG gegen die Sage-Liste pruefen, Pruefliste (Kuerzel ohne Sage-AG, Werkbaenke ohne Sage-Arbeitsplatz, Werkbank-Nummer gleich Sage-AG) an Produktion/AV geben. Nichts wird nach Sage geschrieben.'
   Quelle='2026-11-17-v1-99-27-arbeitsgang-mapping-sage-arbeitsgangnummer' }
@{ Id='v1.99.33-WareneingangCsvSammellieferschein'; Version='1.99.33'; Typ='Info'; Firma='AKE'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='Sammellieferschein: die Wareneingang-CSV heisst ab v1.99.33 beim zweiten Einsatz eines Barcodes <Barcode>_1.csv, _2 ... Auf Prod erst deployen, wenn enaio (E-1) bestaetigt hat, dass der Import Dateien mit Suffix verarbeitet und Feld 1 (Barcode ohne Suffix) zum Ablegen nutzt.'
   Quelle='2026-11-17-v1-99-33-wareneingang-sammellieferschein-bestellkopf' }
@{ Id='v1.99.34-Beschichtungsauftrag-AKE'; Version='1.99.34'; Typ='Einstellung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Optional: Beschichtungsauftrag aktiv (FaHierarchyBeschichtungAktiv) unter /Settings einschalten; dann erscheinen Vorschlagsliste (Lackierteile der WA-Stueckliste) und Auftraege im Menue Beschichtung. Rollen beschichtungsauftrag / beschichtungsauftrag_schreiben zuweisen (Anlage: Pruefung/RollenBeschichtungsauftrag). Dienstleister/RAL/Retour werden am Auftrag von Hand gepflegt.'
   Quelle='2026-11-17-v1-99-34-beschichtung-ake-stueckliste' }
@{ Id='v1.99.35-BlechfertigungAktiv'; Version='1.99.35'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Erst nach Einrichtung (Empfaengergruppe, Rolle schachtler, Pool-Pfad, Feiertage): /Settings > Blechfertigung > Blechfertigung aktiv = true. AKE bleibt aus.'
   Quelle='2026-11-17-v1-99-35-blechfertigung-bf-auftraege' }
@{ Id='v1.99.35-BfCsv'; Version='1.99.35'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='/ServiceSettings > Blechfertigung WiCAM: BfCsvPfad = \\idealcam.ideal.ideal-ake.at\fertigung\u\pn\apools\, BfCsvAktiv = true, Sync:BfAuftragEmailEnabled = true. Das CSV-Layout folgt seit v1.99.42 der WiCAM-Datei BF00000002. Vor dem Einschalten bei allen Bestellern ein Kuerzel unter Stammdaten > Benutzer pflegen (sonst ist die Freigabe gesperrt).'
   Quelle='2026-11-17-v1-99-35-blechfertigung-bf-auftraege' }
@{ Id='v1.99.35-BlechteileFelder'; Version='1.99.35'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='/ServiceSettings: Sync:BlechteileFelderNachlesen = true (SData-Zugang IDEAL vorhanden); nach dem naechsten Artikel-Abgleich Merkmale IstLagerartikel pruefen.'
   Quelle='2026-11-17-v1-99-35-blechfertigung-bf-auftraege' }
@{ Id='v1.99.37-FaStrukturAufbewahrungTage'; Version='1.99.37'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Optional: /ServiceSettings > FA-Hierarchie > Sync:FaStrukturAufbewahrungTage (Default 90, 1-3650). So lange bleibt eine vollstaendig in Sage verschwundene Struktur mit offenem WMS-Auftrag sichtbar; erledigte/stornierte werden sofort aufgeraeumt.'
   Quelle='2026-11-17-v1-99-37-fa-struktur-abgleich' }
@{ Id='v1.99.37-FaStrukturBeendetSichtbar'; Version='1.99.37'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Ab v1.99.37 bleiben in Sage beendete FAs in FA-Struktur, Kommissionierliste, Vormontage, Beschichtung und BDE-Teileliste sichtbar (gelbes Schild "in Sage beendet/inaktiv/ungeklaert"); in der BDE-Teileliste nicht meldbar. Nach dem ersten Lauf das Aktivitaetsprotokoll FA-Hierarchie auf verschwunden_unklar und sage_abfrage_fehler pruefen. Die Sage-Abfrage filtert noch nicht nach Mandant (P-AB4).'
   Quelle='2026-11-17-v1-99-37-fa-struktur-abgleich' }
@{ Id='v1.99.38-sage-fa-ag-schalter'; Version='1.99.38'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='SageFaArbeitsgaengeAnlegenAktiv bleibt AUS (schreibt in Sage-FAs). Einschalten erst nach dem Schreibtest an EINEM FA am IDEAL-Test (TS-159.x, Freigabe Gerald, Spec P-1..P-5) und mit gesetztem Sync:SageFaArbeitsgaengeBearbeiter. AKE: aus lassen.'
   Quelle='2026-11-17-v1-99-38-arbeitsgaenge-in-sage-fa-anlegen' }
@{ Id='v1.99.38-sage-fa-ag-info'; Version='1.99.38'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Neue Admin-Seite Stammdaten > Arbeitsgaenge > Sage: Vorschau je FA und Kuerzel, Sage-Stand nur auf Knopf (nur lesen). Ohne Schalter wird nichts eingereiht und nichts gesendet. Protokoll Sage-FA-Arbeitsgaenge.'
   Quelle='2026-11-17-v1-99-38-arbeitsgaenge-in-sage-fa-anlegen' }
@{ Id='v1.99.40-Userfeld-EKMindestbestellwert-IDEAL'; Version='1.99.40'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='/ServiceSettings > Sage-Benutzerfelder: Kreditoren > Mindestbestellwert (Sync:Userfeld:Kreditoren:USER_EKMindestbestellwert) LEER setzen - das Feld gibt es in Sage IDEAL nicht (sonst bricht der Kreditoren-Abgleich mit Ungueltiger Spaltenname ab). Ebenso Artikel > Lagerplatz-ID und Mindestbestand leer setzen, falls Sage IDEAL sie nicht kennt (Meldung im Aktivitaets-Protokoll nennt die Einstellung). ArtikelLieferantInfo fehlt am IDEAL-Server: Abgleich Artikel-Lieferanten aus lassen.'
   Quelle='2026-11-18-v1-99-40-sage-userfelder-als-einstellung' }
@{ Id='v1.99.40-Userfelder-Info'; Version='1.99.40'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Neu: 27 Service-Einstellungen Sage-Benutzerfelder (Kategorien Sage-Benutzerfelder: ...). Ohne Eintrag gilt der bisherige Feldname - AKE liest unveraendert, nichts einzustellen. Leer = Feld wird nicht gelesen, Anzeige nicht konfiguriert. HTTP 404 einer SData-Resource: eine Meldung, naechster Versuch nach 24 h.'
   Quelle='2026-11-18-v1-99-40-sage-userfelder-als-einstellung' }
@{ Id='v1.99.41-BeschichtungVersendetInfo'; Version='1.99.41'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Beschichtungsauftrag: Positionen werden im Vorschlag angehakt (nur angehakte kommen in den Auftrag), neuer Status Versendet, Lackierer aus den Kreditoren. Hilfeseite Beschichtung.'
   Quelle='2026-11-18-v1-99-41-beschichtungsauftrag-ausbau' }
@{ Id='v1.99.43-IDEAL-Kommissionierliste-Schalter'; Version='1.99.43'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Kommissionierliste nur nach Leitstand-Freigabe ist an (Seed). Je nach Pruefung oben: Auftraege im Leitstand freigeben oder Schalter unter /Settings voruebergehend aus. Standard-Ziele (K-01,K-02) unter /Settings pruefen.'
   Quelle='2026-11-18-v1-99-43-kommissionierliste-filter-spalten-leitstand' }
@{ Id='v1.99.43-IDEAL-Kommissionierliste-Info'; Version='1.99.43'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Kommissionierer informieren: Ziele sind mehrfach waehlbar und werden je Benutzer gemerkt (Knopf Ziele auf Standard), Summiert zeigt nur die gewaehlten Ziele, neue Spalten Abteilung und Lagerplaetze.'
   Quelle='2026-11-18-v1-99-43-kommissionierliste-filter-spalten-leitstand' }
@{ Id='v1.99.45-StartseiteKacheln'; Version='1.99.45'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Startseite zeigt Kacheln nach Bereichen, genau wie das Menue (eine Quelle). Picker ohne Stammdaten-Lesen sehen die Artikelinfo-Kachel nicht mehr (fuehrte auf Zugriff verweigert). OSEON AG-Uebersicht nur bei TeileverfolgungAktiv. Keine Einstellung noetig.'
   Quelle='2026-11-19-v1-99-45-sammellauf' }
@{ Id='v1.99.45-ArtikelinfoBestandExakt'; Version='1.99.45'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Artikelinfo und Lagerbestellung zeigen den Bestand nur noch des gesuchten Artikels (vorher kamen Artikel mit aehnlicher Nummer dazu). Lagerplatz-Text: Hauptlagerplatz zuerst, ohne Kommissionierwagen.'
   Quelle='2026-11-19-v1-99-45-sammellauf' }
@{ Id='v1.99.45-BfWarenkorbVorschlaege'; Version='1.99.45'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='BF-Warenkorb: Artikelsuche schlaegt beim Tippen vor (ab 2 Zeichen, nur Lagerartikel BG/FT). Scan ins Suchfeld sucht sofort, danach fuegt Enter den Artikel hinzu. Besteller informieren.'
   Quelle='2026-11-19-v1-99-45-sammellauf' }
@{ Id='v1.99.47-LagerplatzAusbuchenFaFilter'; Version='1.99.47'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Lagermitarbeiter informieren: Lagerplatz ausbuchen bucht bei FA-Eintrag nur noch Artikel dieses FA aus; am Kommissionierwagen bleiben Artikel ohne FA-Vermerk am Wagen (Feld leeren = alles). Gemischte Artikel (weitere FA, Bestand ohne FA oder FA nur aus einer Ausbuchung) werden im Dialog einzeln abgefragt; aendert sich der Bestand zwischen Anzeige und Bestaetigen, wird nichts gebucht und die Seite neu geladen. IDEAL: Haupt-FA schliesst ihre Sub-FA ein, eine Sub-FA nur sich selbst.'
   Quelle='2026-11-20-v1-99-47-lagerplatz-ausbuchen-fa-filter' }
@{ Id='v1.99.51-StartseiteFavoriten'; Version='1.99.51'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Startseite: neuer Reiter "Favoriten". Jeder Benutzer markiert Kacheln mit dem Stern; gespeichert je Benutzer in der DB (gleich an PC und Terminal). Admin "Ansichten zuruecksetzen" loescht auch die Favoriten.'
   Quelle='2026-11-23-dashboard-favoriten-reiter' }
@{ Id='Einstellung/LagerbestellungVerbrauchsbuchungAktiv'; Version='1.99.53'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='"Lagerbestellung: Lieferung ausbuchen" ist nach dem Update aus (Default). Einschalten unter /Settings entscheidet Gerald nach dem Deploy, je Installation. Am Einschalttag endet die Handbuchung dieser Lieferungen in Sage (sonst doppelte Entnahme).'
   Quelle='2026-11-23-v1-99-53-lagerbestellung-verbrauchsbuchung' }
@{ Id='Info/Lagerbestellung-Entnahmeplatz'; Version='1.99.53'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Nur bei eingeschaltetem Schalter: Lager waehlt je Position "Entnahme aus" (Vorschlag wie Kommissionierung: Hauptlagerplatz mit Bestand, sonst Platz mit dem meisten Bestand); Abschliessen bucht die Ist-Menge als Entnahme aus. Ab dem Einschalttag diese Lieferungen in Sage NICHT mehr von Hand ausbuchen - das WMS uebergibt die Entnahme (sonst doppelte Entnahme).'
   Quelle='2026-11-23-v1-99-53-lagerbestellung-verbrauchsbuchung' }
@{ Id='v1.99.66-SageBuchungenInfo'; Version='1.99.66'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Lager/Einkauf informieren: "Sage-Lagerbuchungen" heisst jetzt "Sage-Buchungen" und enthaelt auch die Sage-Wareneingaenge mit allen Knoepfen (Erneut senden, Als uebergeben markieren, CSV erneut schreiben). Der Menuepunkt "Sage-Wareneingaenge" entfaellt, alte Links leiten um. "Nur in Sage buchen" steht nicht mehr in der Liste.'
   Quelle='2026-11-23-sage-buchungen-gesamtsicht' }
@{ Id='v1.99.68-SageReferenzInfo'; Version='1.99.68'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Sage-Lagerbuchungen tragen ab jetzt im Feld Referenz Kennung, Abteilung und WMS-Benutzer (auf 31 Zeichen gekuerzt) und im Zeilen-Memo den vollen Text WMS: Kennung Benutzer [Abteilung:...]; Sage-Wareneingaenge tragen den vollen Text im Kopf-Memo. Bei Ausbuchung und Lagerplatz ausbuchen kann eine Abteilung (Werkbank) gewaehlt werden. Lager informieren.'
   Quelle='2026-11-25-v1-99-68-sage-buchung-referenz-benutzer-abteilung' }
@{ Id='v1.99.70-KommNachtrag-Lauf'; Version='1.99.70'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Neuer Dienst-Lauf "Kommissionier-Nachtrag": Sync:KommissionierNachtragEnabled (Default true), Sync:KommissionierNachtragUhrzeit (Default 2). Der erste Dienststart holt den Lauf einmal sofort nach - danach Aktivitaetsprotokoll "Kommissionier-Nachtrag" pruefen (Zaehler fehler/altbestand_pruefen).'
   Quelle='2026-11-25-v1-99-70-kommissionierauftraege-nachtrag-nachts' }
@{ Id='v1.99.70-KommNachtrag-Knopf'; Version='1.99.70'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Leitstand: Knoepfe "Fehlende Kommissionierauftraege ... erzeugen" nur noch fuer Admins; Nachtraege legt der Dienst nachts an; "Altbestand geprueft" legt die Auftraege sofort an; neues Badge "Stueckliste kleiner" (offene Zeilen werden nicht gekuerzt). Leitstand-Benutzer informieren.'
   Quelle='2026-11-25-v1-99-70-kommissionierauftraege-nachtrag-nachts' }
@{ Id='v1.99.71-SageLagerplatzRechte'; Version='1.99.71'; Typ='Info'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Text='Lager-/Lagerplatz-Anlage in Sage: DBA vergibt dem Dienstkonto der SageConnection auf der Sage-Mandanten-DB GRANT INSERT KHKLagerplaetze, INSERT+UPDATE KHKLagerorte, EXECUTE spSysTan (SELECT nur falls noetig); Nachpruefung P-4 aus dem Dienstkontext; Pruefabfragen der Spec 2026-11-24-lagerplatz-anlage-wms-nach-sage-spec (IDEAL: P-1 bis P-9 Pflicht vor dem Einschalten).'
   Quelle='2026-11-25-v1-99-71-lagerplatz-anlage-wms-nach-sage' }
@{ Id='v1.99.71-SageLagerplatzAnlageAktiv'; Version='1.99.71'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='SageLagerplatzAnlageAktiv bleibt aus. Einschalten je Standort erst nach Abnahmetest am Testmandanten, Rechte-GRANT, Pruefabfragen gegen die Produktiv-Sage-DB und Sage-DB-Backup.'
   Quelle='2026-11-25-v1-99-71-lagerplatz-anlage-wms-nach-sage' }
@{ Id='v1.99.71-SageLagerAnlageLagertechniken'; Version='1.99.71'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='SageLagerAnlageLagertechniken bleibt Aus. Mitschnitt bzw. Alle erst nach Abnahmevergleich der Lager-Anlage am Testmandanten dieses Standorts; Aus lassen bei USER_-Pflichtfeld ohne Default in KHKLagerorte.'
   Quelle='2026-11-25-v1-99-71-lagerplatz-anlage-wms-nach-sage' }
@{ Id='Einstellung/1.99.72-FraesenPfadAktiv'; Version='1.99.72'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='/Settings > IDEAL: "Fraesen-Pfad anzeigen und pflegen" einschalten (nach dem Erstimport). AKE bleibt aus.'
   Quelle='2026-11-25-v1-99-72-bde-liste-isolierungsfraese-pfad-material' }
@{ Id='v1.99.72-FraesePfadInfo'; Version='1.99.72'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Clemens/Walter informieren: Teileliste der Isolierungsfraese zeigt bestellte Isolierungen (Badge Lagerbestellung, Termin = Abschicken + 10 Arbeitstage), Spalte Fraesen-Pfad ueber das Zahnrad einblenden, Link oeffnet den Ordner (sonst Pfad kopieren -> Explorer Strg+L). Pfad pflegen: der zustaendige Pfleger mit eigenem Konto (Rollen Fraesen-Pfad pflegen + Stammdaten lesen) ueber Stammdaten > Artikelinfo, Stammdaten-Benutzer auch unter Artikel bearbeiten; in der Teileliste nur Anzeige; nur UNC-Pfade. Meldeliste des Erstimports nachpflegen. Browser-Cache am Terminal ggf. leeren.'
   Quelle='2026-11-25-v1-99-72-bde-liste-isolierungsfraese-pfad-material' }
@{ Id='v1.99.73-BdeTeilelisteAgStatusMobilInfo'; Version='1.99.73'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='BDE-Teileliste: neue Spalte AG-Status, Baugruppen-Teile als Zeilen (Knopf neben dem Matchcode), kompakte Ansicht am Handy. Werker kurz informieren; Browser-Cache am Terminal ggf. leeren.'
   Quelle='2026-11-25-v1-99-73-bde-masken-etappe-1-teile-einzeln' }
@{ Id='v1.99.74-BdeTeilelisteSummiertInfo'; Version='1.99.74'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='BDE-Teileliste Summiert: Auftraege je Matchcode aufklappbar (eigener Haken je Auftrag), Haken auf der Summenzeile meldet alle offenen Auftraege darunter fertig, AG-Status je Kuerzel zeigt den schlechtesten Stand. Werker kurz informieren; Browser-Cache am Terminal ggf. leeren.'
   Quelle='2026-11-25-v1-99-74-bde-masken-etappe-2-teile-summiert' }
@{ Id='v1.99.75-BdeTeilfertigInfo'; Version='1.99.75'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='BDE-Teileliste: im Melden-Fenster jetzt Teilfertig melden (Zeile bleibt offen, gemeldet X von Soll) und Fertig melden (schliesst ab, Vorgabe = Restmenge). Auswahl fertigmelden und Summen-Haken melden die Restmenge. Werker kurz informieren; Browser-Cache am Terminal ggf. leeren.'
   Quelle='2026-11-25-v1-99-75-bde-masken-etappe-3-teilfertigmeldung' }
@{ Id='v1.99.76-BdeStandardsichtEinstellung'; Version='1.99.76'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='Werkbank-Stammdaten: neue Auswahl BDE-Standardsicht (Teile / FA) je BDE-Werkbank pruefen; vorbelegt mit Teile, wo bisher die Teileliste wirkte. Der Schalter BDE-Teileliste bleibt in der Maske unveraendert (steuert die Arbeitsgang-Erkennung) und schaltet die Teile-Sicht frei; ohne ihn nur FA-Sicht.'
   Quelle='2026-11-25-v1-99-76-bde-masken-etappe-4a-fa-sicht' }
@{ Id='v1.99.76-BdeFaSichtAkeInfo'; Version='1.99.76'; Typ='Info'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='BDE-Seite mit FA-Sicht gilt auch an AKE (derzeit ohne BDE-Nutzung), erreichbar ueber /BdeTeileliste; das Terminal bleibt bis v1.99.78 unveraendert. Keine Aktion noetig.'
   Quelle='2026-11-25-v1-99-76-bde-masken-etappe-4a-fa-sicht' }
@{ Id='v1.99.77-BdeFaSichtBuchungenInfo'; Version='1.99.77'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='BDE-Seite Reiter FA: Meine Buchungen im Kopf (Pause/Teilfertig/Fertig/Fortsetzen), Scan-Feld FA scannen, Taetigkeit starten, Fertig melden je Zeile an Werkbaenken mit nur Fertigmeldung; Dialoge wie am Terminal. Terminal unveraendert. Werker kurz informieren; Browser-Cache am Terminal ggf. leeren.'
   Quelle='2026-11-25-v1-99-77-bde-masken-etappe-4b-fa-buchungen' }
@{ Id='v1.99.78-BdeTerminalAbloesungInfo'; Version='1.99.78'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='BDE-Terminal oeffnet an beiden Standorten die neue Seite mit Reitern; alte Terminal-Seite entfernt; Tageshistorie im Menue. Einstellung BdeTerminalMaxHauptFa entfaellt (in /Settings als verwaist sichtbar).'
   Quelle='2026-11-25-v1-99-78-bde-masken-etappe-5-terminal-abloesung' }
@{ Id='v1.99.78-BdeHandyKontenInfo'; Version='1.99.78'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='BDE am Android-Handy: je Geraet lokales WMS-Konto (Formular-Login) + Terminal-Zuordnung anlegen (docs/betrieb/09). Konten sind noch nicht angelegt.'
   Quelle='2026-11-25-v1-99-78-bde-masken-etappe-5-terminal-abloesung' }
@{ Id='fa-liste-nur-hauptfa-info'; Version='1.99.82'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='FA-Liste zeigt nur noch HauptFA-Zeilen; Sub-FAs ueber den Knopf FA-Struktur. 17 weitere Strukturfelder (u. a. PPS-Status, RAL, Material) per Zahnrad einblendbar. Matchcode kommt bevorzugt aus der FA-Struktur, sonst aus dem Artikelstamm (wie Stueckliste).'
   Quelle='2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-1-spec' }
@{ Id='fa-struktur-uebersicht-info'; Version='1.99.83'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Text='FA-Struktur: je HauptFA eine Zeile mit allen Auftragsdaten als Spalten, Arbeitsgaenge gleich nach dem Matchcode; Bezeichnung anfangs ausgeblendet, alles ueber das Zahnrad ein-/ausblendbar; Struktur und Matchcode bleiben beim Querscrollen stehen. FA-Liste ohne Zeichen "HauptFA". Baugruppen per Pfeil aufklappen, Kaufteile/Material ueber den Knopf "Teile anzeigen" (Teile mit Arbeitsschritten bleiben immer sichtbar). Gespeicherte Spalteneinstellungen bleiben, neue Spalten kommen sichtbar dazu.'
   Quelle='2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-2-spec' }
)
}

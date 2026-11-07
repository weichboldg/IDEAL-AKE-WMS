---
type: codebase-karte
updated: 2026-10-30
quelle: "$schema des Vertrags CommonWawiServices 9.1.156, AKE-Testserver sagetest01, Dataset ake_TEST2026;1, ausgelesen 2026-10-30"
---
# Sage SData – Vertrag `CommonWawiServices` (25 Service-Operationen)

Übersicht und Aufrufkonventionen: [[sage-sdata-uebersicht]]. Alle Operationen: `POST {Basis}/CommonWawiServices/{Dataset}/$service/<Name>`,
Body JSON mit den Request-Feldern. `*` = `sme:isMandatory`. `(n)` = maxLength. Antworten tragen immer `ResultNumber`, `ResultText`,
`ErrorText`; bei Listen zusätzlich je Position ein Result.

**Im WMS genutzt:** nur `LagerbuchungService` ([[integrationen]] „Sage-Lagerbuchung“, v1.28.0).
**Keine Produktion:** Kein Service für FA-Rückmeldung, Materialentnahme zum FA, Fertigmeldung, FA-Abschluss ([[sage-sdata-masterdata]]).

## Gemeinsame Typen

- `BacklogPrimaryKey`: PrimaryKeyName*, PrimaryKeyValue*, PrimaryKeyType*
- `ChildResponseWithHandleResult`: ChildPrimaryKeys (Liste PrimaryKeyName/Value/Type), ResultNumber*, ResultText*, ErrorText*, BacklogPrimaryKeys
- `GenericUserfield`: DatafieldName*, DatafieldValue*(anyType), DatafieldType* – generischer Weg für USER_-Felder
- `ReportFilter`: ParaName*, DataType* (ReportParaDataTypeSimple), Value*
- `ExportedFile`: BelegHandle*, Filename*, FileAsBase64*
- `DMSArchivIdentityData`: Identity*
- **VKBelegPositionUserField** (AKE-spezifisch): USER_AbwLANS* (Land/Name1/Name2/Ort/PLZ/Strasse/Verwenden/Zusatz), USER_Anschlusswert(50), USER_Bez1VK/Bez2VK(50), USER_CO2e, USER_DPSBSEiltPrio(2), USER_DPSbsMDEDeviceID(50), USER_DPSbsMDEFreigabe(bool), USER_DPSBSMDEMA(128), USER_DPSbsMDESortIndex, USER_DPSBSMDETransitErledigt, USER_DPSBSMDETransitReferenzId, USER_ELverwNormen, **USER_FABelId(int)**, USER_Frequenz, USER_Fuellmenge/2/3, USER_gfksKonsiBuchungsID, USER_GWP, USER_Kaelteleistung/2/3, USER_Kaeltemittel/2/3, USER_Klimaklasse, USER_LeistungsaufnahmeK, USER_MemoVerladung(250), USER_RMAFehlerkategorie/Kommentar/Kostenpflichtig(200), USER_Spannung, USER_TypenschildArtikel, USER_Verpackungsstatus(50), USER_Versand, USER_VT2/VT3
- **EKBelegPositionUserField**: USER_ABPos(15), USER_bestaetigt(bool), USER_DPSBSEiltPrio, USER_DPSbsMDEDeviceID, USER_DPSbsMDEFreigabe, USER_DPSBSMDEMA, USER_DPSbsMDESortIndex, USER_gfksKonsiBuchungsID, USER_RMAFehlerkategorie/Kommentar/Kostenpflichtig, USER_RMAVKBelID, USER_RMAVKBelPosID, USER_VorPosIDRahmenkontrakt, **USER_Zielort(20)**
- **VKBelegUserField**: USER_Angebotsmemo(1000), USER_BestelldatumKunde, USER_CWIStatus, USER_DPSBSBelegherkunft(50), USER_DPSbsBelegsperre, USER_DPSBSEiltPrio, USER_DPSBSMDETransitbeleg, USER_DPSExportCSVPDF, USER_gfksKonsiJobID, USER_IdealAB(70), USER_OLSIWebCommerceID, USER_ParcelID(100), USER_prodtermin, USER_RMA*, USER_TechnBearbeiter(10), USER_Typenschild(10), USER_Versandkz(50), USER_VorgangErfuellt, USER_Zeichnungspfad(250)
- **EKBelegUserField**: USER_AB(250), **USER_ABErhalten(bool)**, USER_CWIStatus, USER_DPSBSBelegherkunft, USER_DPSbsBelegsperre, USER_DPSBSEiltPrio, USER_DPSExportCSVPDF, USER_gfksKonsiJobID, USER_Kom(200), USER_OLSIWebCommerceID, USER_RMA*, USER_WA(250), USER_Zeichnungspfad
- **StandardBelegPosition** (in StandardBeleg/TransitFinalize): PositionHandle*, VorgangPositionHandle*, DoRemoveThisPosition, InsertBeforePositionHandle, Positionstyp*, Artikelnummer*, AuspraegungsHandle*, EanNummer, Variante, Auspraegung1/2Text, Liefertermin, Lieferwoche, Steuercode, SteuercodeKennzeichen, Menge, Mengenart, MengeBasis, Matchcode, Bezeichnung1/2, Langtext, Dimensionstext, IstEinzelpreisManuell, Einzelpreis, IstRabattManuell, Rabatt, RabattBetrag, Kostenstelle, Kostentraeger, Sachkonto, EkBestellnummer, EkReferenzbezeichnung1/2, EkProjektnummer, Artikelmemo, Kontocode, ForceOptionalZubehoerartikel, BelegPositionAutoLagerSeriennummern{Seriennummer*}, BelegPositionAutoLagerChargen{Charge*, Menge*, MengeBasis*, Verfallsdatum}, **BelegPositionLagermengen{PlatzId*, Kurzbezeichnung*, Menge, MengeBasis, LagerPositionSeriennummern, LagerPositionChargen}**, BelegPositionStuecklistePositionen{BelPosHandle*, StuecklistePositionHandle*, VorgangStuecklistePositionHandle*, Menge, MengeBasis, BelegPositionLagermengen}, VK-/EKBelegPositionUserFields, BacklogPrimaryKeys, GenericVK-/EKBelegPositionUserfields
- **BelegPosition** (in BasisBeleg): wie StandardBelegPosition ohne Handles/Lagermengen/Stückliste: Artikelnummer*, AuspraegungsHandle*, Variante, Auspraegung1/2Text, Positionstyp*, Matchcode, Bezeichnung1/2, Langtext, Dimensionstext, IstEinzelpreisManuell, Einzelpreis, IstRabattManuell, Rabatt, RabattBetrag, Menge, Mengenart, MengeBasis, Liefertermin, Lieferwoche, Kostenstelle, Kostentraeger, Sachkonto, EkBestellnummer, EkReferenzbezeichnung1/2, EkProjektnummer, Artikelmemo, Kontocode, UserFields, Seriennummern{Seriennummer*}, Chargen{Charge*, Menge*, Verfallsdatum}, BacklogPrimaryKeys, GenericUserfields
- **Belegkopf-Felder** (BasisBeleg/StandardBeleg/TransitFinalize gemeinsam): Belegjahr, Belegdatum*, Valutadatum, Liefertermin, Lieferwoche, A0Konto*, InteressentAdresse*, Bearbeiter, Versand, Referenznummer, Referenzzeichen, Matchcode, VorgangsMatchcode, VKVertreter, Kopftext (+ReplaceExisting/TextBaustein/Clear), Fusstext (dito), Kostenstelle, Kostentraeger, Lieferbedingung, Sprache, Kontocode (+ChangedApplytoAllPositions), Rabatt1-3/Rabatttext1-3/Rabattbetrag1-3, Zahlungskondition, A1* (Lieferadresse: AdressNummer, AnsprechpartnerId, SetA1DiffDeliveryAddressfieldsManual, Anrede, Name1/2, Zusatz, Strasse, Land, Plz, Ort, AllowResetAnsprechpartnerData), A0* (Kundenadresse manuell), AbwRechnungskonto, A2* (Rechnungsadresse), HatBruttopreise, EULand(2), EuUstId(15), Besteuerungsart, Memo, MemoSpecified, Projektnummer, VKGueltigBis, VkHatKommissionierungsfreigabe, EKLieferschein, VkAngebotsstatus, VKBelegUserFields, EKBelegUserFields, GenericVK-/EKBelegUserfields, VkZahlungskonditionen{Zahlungskondition*, Faelligkeitsdatum*, Betrag*, ProzentAnteilVonBelegbetrag*}
- **Beleg-Response** (BasisBeleg/StandardBeleg/TransitFinalize): Handle*, VorgangHandle, BelegnummerFormatiert*, Projektnummer*, ResultNumber*, ResultText*, ErrorText*, ResponseBelegPositionen{Handle*, VorgangPositionHandle*, Artikelnummer*, AuspraegungsHandle*, ResultText*, BacklogPrimaryKeys}, ChildResponseWithHandleResults

## Enums

| Enum | Werte |
|---|---|
| `ExtErfassungsart` | Verkauf=4000, Einkauf=4100 |
| `ExtLagerbewegungsart` | NotSpecified=0, Zugang=1, Entnahme=2, Umbuchung=3 |
| `ExtPositionstyp` | NotSpecified=-99, Titel=0, Artikel=1, Zwischensumme=2, Texte=3, Alternativ=4, AnzahlungFuerLeistung=5, AnzahlungOhneLeistung=6, Optional=7 |
| `ExtArtikelMengenart` | Undefined=0, Basis=1, Handel=2, Verpackung=3, Lager=4 |
| `Transformart` | Standard=0, Beleguebernahme=1, Vorgangsuebernahme=2 |
| `ExtAngebotsstatus` | None=0, Neuanlage=1, Abgelehnt=3, Angenommen=4, Erfuellt=5 |
| `DispositionStapelTyp` | Manuell=0, Errechnet=1, DirektAusAuftrag=2, ErrechnetFuerETL=3, DispoPPSAuftrag=4, DispoPPSAuftragFF=5, DispoAbsatzplan=9 |
| `DispositionMode` | Wawi=0, Fremdfertigung=1 |
| `EntityOperationMode` | Neu=0, Update=1, Delete=2 |
| `ExtPrintEtikettBasicType` | None=0, Lager=1, Artikel=2, VKPosition=3, EKPosition=4, FAMaterialPosition=5 |
| `ReportParaDataTypeSimple` | Undefined=0, Boolean=1, Integer=2, Decimal=3, Date=4, String=5, DateTime=6, Time=7, Duration=8 |
| `ExtSemaphoreLockMode` / `SemaphoreOperationType` / `ExtSemaphoreState` | Read/Write/Exclusive · Aquire/Release · Succeeded/Failed/Released |
| `DMSDocumentInfoWorkMode` | Beleg=0, KontoAuskunftBuchung=1, OPAuskunft=2, KundeLieferant=3, Artikel=4, Buchungsbeleg=5, GOB_Wawi=10, GOB_Rewe=11, BlankByDocTypeId=20 |
| DMS-Enums | DocumentTypeVersioningMode, -BackgroundImageUse, -VisibilityMode, -ArchivingMode, -IndexDbType, -IndexDataType, -IndexType, -IndexVisibilityMode, -IndexCommand (Details im Schema) |

## Operationen

### Lager und Bestand

**LagerbuchungService** (`$service/LagerbuchungService`) – im WMS genutzt
- Request: Memo, Standardtext, Bearbeiter, Lagerbuchungen[]{Lagerbewegungsart*, Artikelnummer*, AuspraegungHandle*, Variante, Auspraegung1/2Text, HerkunftLagerkennung*, HerkunftLagerplatzId*, ZielLagerkennung*, ZielLagerplatzId*, MengeLager*, Referenz, Memo, Seriennummern{Seriennummer*}, Chargen{Charge*, Menge*, Verfallsdatum}}
- Response: JobHandle*, BuchungenResults[]{BuchungsHandle*, ResultNumber*, ResultText*, ErrorText*}, ResultNumber*, ResultText*, ErrorText*
- Hinweis: Feld `Referenz` je Buchung existiert – im WMS steht die Korrelation `SM#<id>#` heute im `Memo`.

**LagerumbuchungCompleteService** (`$service/LagerumbuchungComplete` – Pfad ohne „Service“!)
- Request: Memo, Standardtext, Bearbeiter, HerkunftLagerkennung*, HerkunftLagerplatzId*, ZielLagerkennung*, ZielLagerplatzId* (bucht den **gesamten** Platzbestand um)
- Response: JobHandle*, Result*

**InventurService**
- Request: ZaehllisteId*, Bearbeiter*, InventurPositionen[]{Position*, Artikelnummer*, EANNummer, AuspraegungId*, LagerplatzId, Lagerkurzbezeichnung, Zaehlmenge, Chargen{Zaehlmenge, Charge, Verfallsdatum, Langtext}, Seriennummern{Seriennummer}}
- Response: Result*, Handle*, ChildResponseWithHandleResults, InventurPositionenResults[]{Handle*, Result*, Artikelnummer, Matchcode, EANNummer, AuspraegungId}

### Belege (VK und EK)

**StandardBelegService** – Beleg anlegen, ändern, übernehmen (Erfassungsart Verkauf **oder Einkauf**)
- Request: Erfassungsart*, BelegTransformArt*, IsBelegTransform*, EditBelegHandle, SemaphoreByExternalApp, BelegKennzeichen*, VorgangHandle, BelegNurParken, ForcePositionenRenumber, TransformSettings[]{SourceBelegHandle*, CopyKopftext, CopyFusstext, HandleZuschlaegeVorgangsbezogen, RecalcBelegzuschlaege}, StandardBelegPositionen[] (s. o.), KonsibelegSettings[]{PlatzId*, Kurzbezeichnung*} + alle Belegkopf-Felder
- Response: Beleg-Response
- Bedeutung: Mit `Erfassungsart=4100` und einem EK-`BelegKennzeichen` können **Bestellungen** angelegt bzw. per `EditBelegHandle` geändert werden; `BelegPositionLagermengen` erlaubt platzgenaue Zugänge (Wareneingang).

**BasisBelegService** – einfache Neuanlage (ohne Handles/Transform/Lagermengen)
- Request: Kennzeichen*, Belegjahr, Belegdatum*, A0Konto*, InteressentAdresse*, BelegPositionen[] + Belegkopf-Felder
- Response: Beleg-Response

**TransitFinalizeService** – wie StandardBelegService (Partner-Erweiterung „Transit“, vgl. USER_DPSBSMDETransit*)

**UpdateBelegEntityService** – einzelnes Kopffeld setzen
- Request: UseSemaphore*, Erfassungsart*, BelegHandle*, EntityFieldName*, NewValue*

**UpdateBelegpositionEntityService** – einzelne Positionsfelder setzen
- Request: UseSemaphore*, Erfassungsart*, BelegHandle*, Positionen[]{Handle*, EntityFieldName*, NewValue*}

**SwitchBelegSperreService** – Request: Erfassungsart*, BelegHandle*, NewValue*

### Kommissionierung / Picklisten

**PicklistItemsEditService**
- Request: Bearbeiter*, PicklistItems[]{PicklistHandle*, KommissionierHandle*, PicklistPositionHandle, LagerplatzHandle*, Lagerkurzbezeichnung*, MengeBasisGepickt*, Artikelnummer*, AuspraegungsHandle*, Seriennummern, Chargen{Charge*, MengeBasis*}, Zugangskennungen{BestandsId*, EntnahmeMengeLagermengeneinheit*, Einzelpreis*, Zugangskennung*, Eingangsdatum}}
- Response: PicklistHandle*, PicklistItemsResults[]{PicklistPositionHandle*, KommissionierHandle*, Result*}, Result*, ChildResponseWithHandleResults

**PicklistCreateLieferscheinService**
- Request: BelegKennzeichen*, Belegjahr, Belegdatum*, Bearbeiter, UseBelegdatumAsLiefertermin, Picklists[]{PicklistHandle*}
- Response: PicklistsResults[]{PicklistHandle*, BelegHandle*, BelegnummerFormatiert*, Result*}, Result*, ChildResponseWithHandleResults

### Disposition (Bestellvorschlag)

**DispositionStapelService** – Request: Stapelkennung, Stapeltyp, Bestelltermin, OperationMode, DispoMode · Response: Stapelkennung*, Result*
**DispositionStapelPositionenService**
- Request: Stapelkennung, Stapeltyp, Bestelltermin, OperationMode, DispoMode, Stapelpositionen[]{Handle, OperationMode, Artikelnummer*, AuspraegungHandle*, Variante, Auspraegung1/2Text, MengeBasis*, MengeBasisFix*, Lieferant*, IsLieferantFix*}
- Response: Stapelkennung*, PositionenResults[]{PosHandle*, Artikelnummer*, AuspraegungHandle*, MengeBasis*, MengeBasisEinheit*, Result*}, Result*, ChildResponseWithHandleResults
- Bedeutung: Bedarfe (z. B. aus WMS-Bedarfsmeldungen) als Dispositionsstapel nach Sage geben, aus dem der Einkauf Bestellungen erzeugt.

### Preise

**PreisanpassungService** – Request: AenderungsBetrag*, AenderungsBetragIsProzent*, ListeID, Kto, PreisanpassungArtikelItems[]{Artikelnummer*, AuspraegungID*} · Response: Result*, PreisanpassungResults{Total*, Successful*, Failed*, Errors[]{Artikelnummer*, AuspraegungID*, ErrorText*}}

### Druck / Export

**PrintBelegeService** – Request: BelegErfassungsart*, BelegHandles[]{BelegHandle*}, PrintAsUser, ExportFilename, ExportPath, PrintBackground, IsCopy, DruckprozessTanPreset, Copies, DMSArchivIdentityData
**ExportPrintBelegeService** – wie Print, ohne Copies/DMS, plus IgnoreERechnungSettings · Response: ExportedFiles[]{BelegHandle*, Filename*, FileAsBase64*} (**Beleg-PDF als Base64 abholbar**)
**PrintStandardReportService** – Request: ReportName*, ReportFilters[], PrintAsUser, PrinterName, PrintBackground, Copies
**ExportStandardReportService** – Request: ReportName*, ReportFilters[], ExportFilename, ExportPath, PrintBackground · Response: ExportedFiles[]
**PrintEtikettenBasicService** – Request: EtikettType* (Lager/Artikel/VKPosition/EKPosition/**FAMaterialPosition**), EtikettBelegData{BelegHandle*, BelegPositionHandles[]{BelegPositionHandle*, Copies*}}, ReportName*, ReportFilters[], PrintAsUser, PrinterName, PrintBackground, Copies

### DMS

**DMSGetDocumentTypesService** – Request: DMSArchivIdentityData · Response: DocumentTypes[] (Key, DmsID, ErpID, Description, TypeName, Indexes[] …)
**DMSGetInsertDocumentInfoService** – Request: DMSArchivIdentityData, Mode*, DocumentInfoByWawiBeleg{BelegHandle*, Erfassungsart*} | ByBuchungsBeleg{BuchungsTan*} | ByDocTypeErpId{DocumentTypeErpID*} · Response: DocumentInfo{…Indexes[]}
**DMSRegisterExternalDocumentsService** – Request: DMSArchivIdentityData, Filename*, FileAsBase64*, DocumentInfo · Response: Result*

### Infrastruktur

**HealthcheckInfoService** – Request: CallingClientName* · Response: Result* (geeignet als Erreichbarkeitsprüfung im Dienst)
**MultiUserServiceInfoService** – Request: CallingClientName* · Response: Pipes[]{Station, DatabaseName, ClientID, ApplicationName, UserName, Semaphoren[]}
**MultiUserServiceSemaphoreService** – Request: SemphoreName*, Atom, LockMode*, OperationType* · Response: SemaphoreState*, Result*

## Ressourcen (109 `resourceKind`)

Nur Request-/Response-Sample-Typen der Operationen (z. B. `BasisBelegServiceRequestSample`, `Lagerbuchung`, `BelegPosition`); keine eigenständig abfragbaren Daten.

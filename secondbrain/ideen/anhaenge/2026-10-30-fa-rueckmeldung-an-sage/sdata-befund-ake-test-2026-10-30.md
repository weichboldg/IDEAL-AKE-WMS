# SData-Schnittstelle Sage 100 – Befund AKE-Testserver (2026-10-30)

Quelle: `https://sagetest01.ake.at:5493/sdata/ol`, Dataset `ake_TEST2026;1`, angemeldet von Gerald im Browser, Koordination hat
nur GET-Abfragen ausgeführt (Vertragsliste, `$schema`, Lese-Stichproben mit `count=1..5`). Keine Buchung, kein POST/PUT.
Hinweis: Chrome meldet auf dem Server einen Zertifikatsfehler („Datenschutzfehler“); die Ausnahme muss je Browsersitzung
bestätigt werden.

## 1. Verträge (19)

| Vertrag | Beschreibung laut Server | Relevanz |
|---|---|---|
| `CommonWawiServices` 9.1.156 | Common Wawi SData Services | **25 Service-Operationen**, darunter `LagerbuchungService` (heute im WMS genutzt). Keine Produktion. |
| `CommonWawiServicesResources` | Generische Ressourcen auf Basis von Queries | `$schema` am AKE-Test **HTTP 500** („Ungültiger Objektname“ – ein Objekt fehlt in der Test-DB) |
| `CommonWawiServicesResourcesSpecific` | Speziell definierte Ressourcen | Picklisten, Sammelmappe, Lizenz; keine Produktion |
| `MasterData` | Sage Office Line Master-Data (AppDesigner-Datenstrukturen) | **790 Ressourcen, 357 Operationen**, darunter die PPS-Strukturen (`str*.Sage.PPS`) |
| `RealTimeData` | Sage Office Line Real-Time-Data (Datenquellen) | **1220 Ressourcen, 79 × `dts*.Sage.PPS`**, nur lesen |
| `apiArtikel/apiBelegerfassung/apiKunden/apiLieferanten.Sage.API` | Sage-API-Verträge 1.x | Stammdaten/Belege, keine Produktion |
| `apiDMSConnector…DPSBS_DMS_Connector` | DMS-Connector des Partners | – |
| `ApplicationServerTest`, `authentication`, `ControlCenter*`, `Database`, `DataContext`, `DataReferences`, `ErrorLogging` | Infrastruktur Sage-Client | – |

Partnernummer `100036671` = der Sage-Partner (Pakete `AKE_*`, `DPSBS_*` – u. a. `AKE_InterneFA`, `DPSBS_Etikettendruck`,
`AKE_OseonDataImport`, `DPSBS_MDE*`-Felder).

## 2. CommonWawiServices – alle Operationen

BasisBelegService, DispositionStapelPositionenService, DispositionStapelService, DMSGetDocumentTypesService,
DMSGetInsertDocumentInfoService, DMSRegisterExternalDocumentsService, ExportPrintBelegeService, ExportStandardReportService,
HealthcheckInfoService, InventurService, **LagerbuchungService**, LagerumbuchungCompleteService, MultiUserServiceInfoService,
MultiUserServiceSemaphoreService, PicklistCreateLieferscheinService, PicklistItemsEditService, PreisanpassungService,
PrintBelegeService, PrintEtikettenBasicService, PrintStandardReportService, StandardBelegService, SwitchBelegSperreService,
TransitFinalizeService, UpdateBelegEntityService, UpdateBelegpositionEntityService.

→ **Kein Service für FA-Rückmeldung, Materialentnahme zum FA, Fertigmeldung oder FA-Abschluss.** Produktion ist in diesem
Vertrag nicht abgebildet.

## 3. MasterData – PPS-Datenstrukturen (`str*.Sage.PPS`, Tag `DataStructures`)

| Struktur | Label | canGet/Put/Post/Delete | Felder (Auszug) |
|---|---|---|---|
| `strFaBeleg.Sage.PPS` | Fertigungsauftrag | **G/P/P/D** + `$service/Execute` | BelegHandle, StrukturHandle, Belegnummer, Artikelnummer, Sollmenge, Fertigungsmenge, **IstMenge**, Soll/IstStart-/Endtermin, **GesamtZeitSoll/Ist**, **IstBegonnen, IstBeendet, IstReaktiviert**, IstUnterfertigungsauftrag, HatUnterbaugruppen, Aenderungsindex, Listen `FaPositionen`, `FaVerursacher`, USER_-Felder (u. a. `USER_DPSBSMDEFreigabe`, `USER_DPSBSBereitstellungBeendet`) |
| `strFaPosition.Sage.PPS` | FA Position | nur Get (als Teil von strFaBeleg) | POS_BelegPositionsHandle, **POS_BelegHandleNachfolger** (= Sub-FA), POS_RessourceTyp, POS_RessourceNummer, POS_ArbeitsgangNummer, POS_Sollmenge/Fertigungsmenge/**IstMenge**, POS_Soll/IstRuestzeit, POS_Soll/IstStueckzeit, **POS_Status**, POS_BdeTerminalHandle, POS_BdeMitarbeiterNummer, POS_FortschrittMenge/-Zeit, Liste `AgPositionen` |
| `strFaAgPosition.Sage.PPS` | AG-Position | – | AGPOS_BelegArbeitsgangPositionsHandle, AGPOS_RessourceNummerAG, Soll-/Ist-Kosten Rüsten/Betrieb, AGPOS_Leistungsgrad |
| `strFaFertigungsprojektBeenden.Sage.PPS` | FA Beenden | nur Get | Fertigungsauftrag:int, UnterbaugruppenMitBeenden:int |
| `strFaFertigungsprojektReaktivieren.Sage.PPS` | FA Reaktivieren | nur Get | Fertigungsauftrag, UnterbaugruppenAuchReaktivieren, MitDispoResourcenbelegung |
| `strFaNeuAnlage.Sage.PPS` | FA anlegen | nur Get | Artikelnummer, Fertigungsmenge, Terminierungsart, UnterbaugruppenErzeugen … |
| `strFaLoeschen`, `strFaGeheZu`, `strFaPosNeuTerminieren`, `strFaRessourcenlisteAktualisieren`, `strFaVerursacher`, `strFaBelegsprache`, `strPpsBeleg`, `strPpsFfBeistell` | Dialoge | nur Get | – |
| `strRessourcenlisteKopf.Sage.PPS` (+ `strRessourcenPosition`, `strRessourcenAgPosition`) | Ressourcenliste (Artikel-Stückliste) | G/P/P/D + Execute | Artikelnummer, Positionen mit RessourceTyp, Arbeitsgang, Rüst-/Stückzeit |
| `recArbeitsgaengestamm`, `recArbeitsplaetzestamm`, `recWerkzeugestamm.Sage.PPS` | Stammdaten | G/P/P/D (DataRecords) | Arbeitsplätze inkl. `BdeTerminalId` |

`Execute`-Request: `MethodName:string, DataKey, PartKey, Data:strFaBeleg, AutoCorrect, NamedParameters`. **Die gültigen
MethodNames stehen nicht im Schema** – das ist die interne Datenstruktur-API des Sage-Clients (AppDesigner), nicht die
dokumentierte Partner-API.

**Lese-Stichprobe:** `GET MasterData/…/strFaBeleg.Sage.PPS?count=1` und `…(818)` → **HTTP 403** für den SData-Benutzer. Die
Datenstrukturen sind mit dem heutigen Service-Benutzer nicht erreichbar (Rechte oder Lizenz; ungeprüft, Sage-Partner).

## 4. RealTimeData – PPS-Datenquellen (`dts*.Sage.PPS`, nur lesen, JSON per `format=json`)

Für den **Abgleich** relevant (Lesetest OK, HTTP 200, JSON):

| Datenquelle | Felder (Auszug) | Test |
|---|---|---|
| `dtsFaUebersicht` | 216 Felder; BelID, StrukturID, Artikelnummer, IstMenge, IstBegonnen, IstBeendet … | 200, Daten (z. B. BelID 818) |
| `dtsFaNachStatus` | AnzahlNeu, AnzahlEingelastet, AnzahlBegonnen | 200: Neu 20, Eingelastet 7, Begonnen 0 |
| `dtsFertigmeldungen` | BelID, RueckmeldungID, MengeGut, DatumZeitEnde, Lagerkurzbezeichnung, Lagerbestand, VerursacherTyp/-ID | 200, leer |
| `dtsRueckKorrektur` | **RueckmeldungID, BelID, BelPosID, Typ, Buchungstyp, Menge, MengeGut, Schwund, DatumZeitStart/Ende, Mitarbeiternummer, Arbeitsplatznummer, Werkzeugnummer, IstRuesten, IstEnde, LagerBuchID, LagerJobID, Status, FibuUebergabe, Ist-Kosten** | 200, leer – das ist der Aufbau der Sage-Rückmeldungstabelle |
| `dtsFaFortschritt` | BelID, AgNummer, FortschrProz, FortschrAgProz, PosSoll/PosIst, IstBegonnen/IstBeendet | 500 ohne Parameter |
| `dtsFaBelegStruktur` | BelegHandle, Position, **BelIDNachfolger**, RessourceTyp, Istmenge, IstStueckzeit, Status, StrukturID, BdeTerminalId, BdeMitarbeiter | 200, leer ohne Parameter (Parameterübergabe noch offen) |
| weitere | dtsBDELogbuch, dtsBDEFehlbuchungen, dtsBDEStempelKorr, dtsBDETerminalSuche, dtsWorkInProgress, dtsFaMaterialentnahmeschein, dtsFaEinzellohnschein, dtsAusschussquote, dtsTaetigkeitsliste, dtsRuestzeiten, dtsFfBeistell*, dtsStkListen*, dtsTeileverwendungsnachweis | – |
| Partner | `dtsAbfLOGFertigungsauftraege.100036671.AKE_InterneFA` (BelID, USER_FAStatus, Ladetermin, IstVerladen …) | – |

Sage 100 hat also ein eigenes BDE-Modul (Rückmeldungen mit Typ, Mitarbeiter, Arbeitsplatz, Terminal, Stempelkorrektur).
Das WMS-BDE bildet dasselbe Fachobjekt ab.

## 5. Schlussfolgerung

1. **Lesen ja:** FA-Status, Rückmeldungen und Fertigmeldungen sind per `RealTimeData` als JSON lesbar – Grundlage für den
   Abgleich WMS ↔ Sage (Stufe 1), ohne Sage-DB-Direktzugriff.
2. **Schreiben nein (dokumentiert):** Es gibt keinen Produktions-Service in `CommonWawiServices`. Der einzige Schreibweg ist die
   interne Datenstruktur `strFaBeleg.Sage.PPS` (PUT/Execute), undokumentiert und für den SData-Benutzer **403**.
3. **Daher:** Für Fertigmeldung, Materialentnahme zum FA, AG-Zeiten und FA-Abschluss braucht es entweder (a) einen vom
   Sage-Partner bereitgestellten Service im Stil des `LagerbuchungService` (der Partner hat erkennbar eigene Pakete auf dem
   Server), oder (b) Rechte + Dokumentation der `MethodName`s für `strFaBeleg`. (a) ist der robustere Weg.

## 6. Postman-Lesetests 2026-10-30 09:20 (Gerald angemeldet, Koordination per Bildschirmsteuerung, nur GET)

| Abfrage | Ergebnis |
|---|---|
| `RealTimeData/…/dtsFaNachStatus.Sage.PPS?format=json` | 200, 202 ms; Begonnen 0, Eingelastet 7, Neu 20. Antwort kam als Atom-XML trotz `format=json` (Postman-Accept-Header) |
| `dtsFaUebersicht.Sage.PPS?count=2&select=BelID,StrukturID,Artikelnummer,Matchcode,Sollmenge,IstMenge,IstEingelastet,IstBegonnen,IstBeendet,SollStarttermin,SollEndtermin` | 200, 37 ms; **`select` funktioniert**. FA 835: Artikel 86003871, Matchcode „Waermebruecke 112-kpl_Basic/ComfortLine“, `IstEingelastet = -1` (Sage-Boolean), IstMenge 0, Sollstart 2026-02-03T00:00:00, Sollende 2026-02-04 |
| `dtsFaBelegStruktur.Sage.PPS?StrukturID=835&…` | 200, **leer** |
| `dtsFaBelegStruktur.Sage.PPS?where=StrukturID eq 835&…` | 200, **leer** |
| `dtsFaPositionen.Sage.PPS?where=BelID eq 835&count=3` | 200, **leer** |
| `dtsFaNachStatus.Sage.PPS/$queries` | leer (keine benannten Abfragen) |
| `dtsFaPositionen.Sage.PPS/$service` | nur `InvalidateSchema` |

**Folgerung:** Kopf-Quellen (`dtsFaUebersicht`, `dtsFaNachStatus`, `dtsFertigmeldungen`, `dtsRueckKorrektur`) sind per SData
direkt nutzbar, mit `select` und `where`. Struktur-/Positionsquellen brauchen einen Kontextparameter des Sage-Clients, der
über SData nicht übergeben werden kann (kein `$queries`, kein Service). Für die FA-Struktur bleibt die Sage-View `FAListe`
der Lesepfad; SData ergänzt den Rückmeldestand je BelID.

**Browser-Hinweis:** Chrome zeigte nach ~30 Minuten erneut eine Fehlerseite, Postman kam gleichzeitig in 200 ms durch – der
Server war nicht weg, die Zertifikatsausnahme in Chrome war verfallen.

## 7. Nachprüfung 2026-11-03 (Koordination, Chrome, nur GET, Gerald angemeldet)

Anlass: [[2026-11-02-sage-pps-abgleich]] Abschnitt F. Alle Abfragen einzeln gegen `RealTimeData/ake_TEST2026;1`. Fetch aus der
Seite braucht `Accept: application/json`, sonst kommt trotz `format=json` Atom-XML. Versehen: Einmal wurde `…/dtsFaBelegStruktur.Sage.PPS/$schema`
abgefragt – das liefert das **ganze** Vertragsschema (5,9 MB), nicht nur die Quelle. Der Server blieb erreichbar; danach aus dem Browser-Cache gelesen.

| Prüfung | Ergebnis |
|---|---|
| Statusfelder in `dtsFaUebersicht` (216 Felder) | vorhanden: `IstEingelastet`, `IstBegonnen`, `IstBeendet`, `Aktiv`, `Fertigungsart`, `AusfuehrungsArt`, `Verursacher`, `StrukturID`, `IstStarttermin`, `IstEndtermin`. `dtsFaBelegsuche` hat zusätzlich `IstGesperrt`, `IstReaktiviert`, `VerursacherTyp`, `ArchiviertAm`, `GeaendertAm`, `Sollmenge` |
| Werte Struktur 835 | 835 `Verursacher = MAN`, 836–841 `Verursacher = FA`; alle `Fertigungsart = EF`, `Aktiv = -1`, `IstEingelastet = -1`, nicht begonnen |
| Verteilung alle 27 FAs AKE-Test | FA/EF/aktiv/eingelastet 6 · FA/EF/aktiv/nicht eingelastet 8 · MAN/EF/aktiv/eingelastet 1 · VK/EF/aktiv/nicht eingelastet 12. **Kein** inaktiver FA, keine andere Fertigungsart, kein begonnener FA |
| `Verursacher` = Eltern-FA? | **Nein.** Das Feld ist ein Typkürzel (MAN/FA/VK), keine BelID. Eine Vater-Kind-Kante liefert keine lesbare Kopfquelle |
| `dtsFaBelegStruktur` | Schema hat `BelIDNachfolger`, `BelIdParent`, `ParentId`, `BelPosID`, `StrukturID`; `where=BelID eq 835`, `where=BelegHandle eq 835` → 200, **leer** (wie am 30.10. mit StrukturID) |
| `dtsFaPositionen` | nur 8 Felder (BelID, BelPosID, Position, Ressourcetyp, Ressourcenummer …), kein Nachfolger |
| `dtsFaVerursacherInfo` | 200, leer (mit und ohne `where`) |
| `dtsVerursacherAnzeige` (`where=BelID eq 836`) | **500** |
| `dtsTeileverwendungsnachweis` | Felder ohne Nachfolger/Parent/Struktur; nur `BelID`, `Artikelnummer`, `Ressourcetyp`, `Menge` |
| Rückmeldungen | `dtsRueckKorrektur` und `dtsFertigmeldungen` ohne Filter → 200, **0 Zeilen**: Die AKE-Test-DB enthält keine Rückmeldungen. `dtsLagerRueckmeldungen.Sage.PPS` → 404 – **Fehler der Abfrage**: Die Quelle steht laut [[sage-sdata-realtimedata]] im Wawi-Teil, also nicht unter `.Sage.PPS`; nicht erneut abgefragt |
| Rückmelde-Grundlagen Produktion | **keine** Datenquelle; Grundlagen-Quellen gibt es nur für Rewe (`dtsGrundlagen*.Sage.Rewe`) |

**Folgerungen**
1. **Status lesen über SData: ja.** Statusfilter (eingelastet, aktiv, Fertigungsart) lässt sich vollständig über `dtsFaBelegsuche`/`dtsFaUebersicht` abbilden.
2. **Hierarchie-Kanten über SData: nein.** Mitglieder einer Struktur ja (`StrukturID`), aber nicht, welcher FA Vater welches Unter-FA ist.
   Alle Quellen mit Kanten (`dtsFaBelegStruktur`, `dtsFaVerursacherInfo`, `dtsVerursacherAnzeige`) brauchen einen Client-Kontext. Die Kante
   bleibt bei der View `FAListe` – oder DPS nennt den Parameter. (Eine Ableitung über „Artikel des Unter-FA = MA-Position im Vater“ wäre
   nur eine Heuristik und bei mehrfach verbautem Artikel mehrdeutig.)
3. **Rückmeldungen lesen: am AKE-Test nicht prüfbar** (keine Daten). Ob `dtsRueckKorrektur` im Echtbetrieb Inhalt liefert, bleibt offen.
4. **Grundlagen:** nur über Sage-Oberfläche oder DPS.
5. Der Fall „Fertigungsart Einkauf → inaktiv“ kommt am AKE-Test nicht vor; prüfbar erst mit Daten, die ihn enthalten.

## 8. Nachprüfung MasterData und Struktur-Quellen 2026-11-03 07:46 (nur GET, je 1 s Abstand)

Hinweis: Die Abfragen liefen mit der **Browser-Anmeldung von Gerald**, nicht nachweislich mit dem WMS-Servicebenutzer.

| Abfrage | Ergebnis |
|---|---|
| `MasterData` Datasets | 200, nur `ake_TEST2026;1` |
| `strFaBeleg.Sage.PPS?count=1`, `(835)`, `('835')` | **403** |
| `recArbeitsplaetzestamm.Sage.PPS?count=1`, `recLieferantenstamm.Sage.System?count=1` | **403** |
| Wortlaut aller 403 | „Die Referenz zwischen dem Datensatz `<Name>` und dem Element stimmt nicht überein.“ (`ApplicationDiagnosis`) – **keine** Meldung zu Rechten oder Lizenz |
| `strFaBeleg.Sage.PPS/$service` | 200; listet **alle 357** Operationen des Vertrags, PPS-relevant: `strFaBeleg_Sage_PPS_Execute`, `strRessourcenlisteKopf_Sage_PPS_Execute`, `rec{Arbeitsgaenge,Arbeitsplaetze,Werkzeuge}stamm_Sage_PPS_CopyRecord`. Keine eigene Rückmelde-Operation |
| `dtsFaBelegStruktur` `$queries` / `$service` | 200; keine benannten Abfragen; Service nur `InvalidateSchema` |
| `dtsFaVerursacherInfo` `$queries` / `$service` | dito |
| `dtsVerursacherAnzeige` `$queries` | 200, keine Abfragen; Datenabruf `where=BelID eq 836` → **500** „Beim Zugriff auf die Datenbank ist ein Fehler aufgetreten.“ |
| `dtsFaBelegStruktur` ohne `where` und `where=BelIdParent eq 835` | 200, leer |

**Folgerungen**
1. Die 403 auf `MasterData` gilt für **jede** Ressource, auch einfache Datensätze (Lieferanten). Die Meldung spricht von einer nicht
   übereinstimmenden „Referenz zwischen Datensatz und Element“ – Laut SData-Spezifikation bedeutet 403 „Consumer darf nicht lesen“
   ([[sdata-2-0-spezifikation]]); der Grund (Rechte, Lizenz, Konfiguration) bleibt offen. (Korrigiert 2026-11-03: vorher als „eher Konfiguration“ gedeutet.) Ob der WMS-Servicebenutzer dasselbe bekommt, ist nicht geprüft.
2. Über SData gibt es **keinen** Weg, den Struktur-Quellen einen Kontext zu übergeben (keine Queries, kein Service). Die Hierarchie-Kante
   bleibt ohne DPS bei der View `FAListe`.
3. Eine Rückmelde-Operation ist in `MasterData` nicht vorhanden; einziger Kandidat bleibt `strFaBeleg … Execute` mit unbekannten MethodNames.

## 9. Schema-Fähigkeiten und Sync/Linking 2026-11-03 08:40 (nur GET, Browser-Anmeldung Gerald)

Abgleich mit [[sdata-2-0-spezifikation]]. Schemas je einmal geladen (CommonWawiServices 0,3 MB, RealTimeData 5,9 MB aus Cache, MasterData 2,5 MB),
danach Einzelabfragen mit 1 s Abstand.

| Vertrag | supportsETag | hasUuid | protocolFilters | invocationMode / batchingMode | isSyncSource/Target |
|---|---|---|---|---|---|
| CommonWawiServices | – | – | – | **syncOrAsync** (alle 25 Ops, auch `LagerbuchungService`) | – |
| RealTimeData | – | – | – | (nur 1 Op) | – |
| MasterData | **true** (789) | **true** (789) | – | **syncOrAsync** (357 Ops, auch `strFaBeleg … Execute`) | – |

`strFaBeleg.Sage.PPS`: canGet/Put/Post/Delete, hasTemplate, hasUuid, supportsETag = true – aber jede Abfrage, auch `$template`, → 403.

| Abfrage | Ergebnis |
|---|---|
| `RealTimeData/…/dtsFaBelegsuche/$syncDigest`, `MasterData/…/strFaBeleg/$syncDigest` | **500** |
| `dtsFaBelegsuche/$linked` | 200, liefert aber die normale Datenliste – `$linked` wird **ignoriert**, kein Linking |
| `dtsFaBelegsuche?includeUuid=true` | kein `$uuid` in den Einträgen |
| `$updated` in Einträgen | ist der **Abfragezeitpunkt** (z. B. `2026-11-03T08:41:02`), keine Änderungszeit |
| `where=$updated gt @2026-01-01@` | **500**, Diagnose zeigt die SQL: „Ungültiger Spaltenname "updated"“ auf `KHKPpsFaBelege` – `where` wird direkt in SQL übersetzt; `dtsFaBelegsuche` liest `KHKPpsFaBelege` |
| `where=GeaendertAm gt @2026-01-01@` (Feld der Quelle) | **200**, filtert |
| `CommonWawiServices/…/$service/LagerbuchungService/$template` | 200; Kopf `Memo, Standardtext, Lagerbuchungen`; Position `Lagerbewegungsart, Artikelnummer, AuspraegungHandle, Herkunft-/ZielLagerkennung, Herkunft-/ZielLagerplatzId, MengeLager, Seriennummern, Chargen` – **kein FA-Bezug, kein UUID-Feld** |

**Folgerungen**
1. **Kein Sync, kein Linking, keine UUIDs, keine ETags** in den lesbaren Verträgen. Sync-Muster der Spezifikation sind für Sage 100 hier nicht nutzbar.
2. **Delta-Lesen** geht nicht über `$updated`, sondern nur über Felder der Quelle, z. B. `GeaendertAm` an `dtsFaBelegsuche`. Ob jede für das WMS
   relevante Änderung (Einlasten, Beenden, Unter-FA) `GeaendertAm` setzt, ist **ungeprüft**.
3. **Idempotenz LagerbuchungService:** Das Schema sagt `invocationMode = syncOrAsync`, also wäre laut Spezifikation `?trackingID=<uuid>` (asynchron,
   202 + Polling) zulässig. **Nicht getestet** (POST). Ob Sage Duplikate an der `trackingID` erkennt, bleibt Frage an DPS. Ohne das bleibt das Memo
   (`IdealAkeWms Lagerbuchung SM#…`) die einzige Korrelation.
4. `MasterData` wäre technisch der „reiche“ Vertrag (ETag-Concurrency, UUIDs, async) – sofern die 403 fällt.

## 10. Neue PPS-Services von DPS am AKE-Test 2026-11-06 11:25 (nur GET: Operationsliste, `$schema`, `$template`)

Anlass: Gerald, „DPS hat etwas installiert“. Keine Buchung, kein POST. Browser-Anmeldung Gerald.

`CommonWawiServices` hat jetzt **36 statt 25** Operationen (Schema 0,41 MB statt 0,30 MB; Versionsattribut im Schema `9.0.156.0`). **Neu (11):**
`FertigungsauftragService`, `PpsRueckmeldungArbeitsplatzService`, `PpsRueckmeldungMaterialService`, `PpsRueckmeldungFertigmeldungService`,
`PpsRueckmeldungFremdfertigungService`, `PpsRueckmeldungStillstandService`, `PpsRueckmeldungVerschnittService`, `PpsMitarbeiterKommtGehtService`,
`PrintFABelegeService`, `UpdateFertigungsauftragEntityService`, `UpdateFertigungsauftragPositionEntityService`.
Alle: `invocationMode = syncOrAsync`, `batchingMode = syncOrAsync`, `hasTemplate = true`; `$template` liefert 200.

### Rückmelde-Services (gemeinsamer Aufbau)

Request: `Bearbeiter`, Liste der Rückmeldungen, **`RueckmeldungGeneralSettings`**.
Response: `ResultNumber*`, `ResultText*`, `ErrorText*`, `ResponseRueckmeldungen[]` (je `Handle*`, `BacklogPrimaryKeys[]`, `ResultNumber`, `ResultText`,
`ErrorText`), `ChildResponseWithHandleResults[]`. (`*` = Pflicht laut Schema.)

| Service | Felder je Rückmeldung (Pflicht *) |
|---|---|
| `PpsRueckmeldungArbeitsplatzService` (Zeit) | `FertigungBelegHandle*`, `FertigungBelegPositionHandle*`, `Mitarbeiternummer*`, `Arbeitsplatznummer*`, `DatumZeitStart*`, `DatumZeitEnde*`, `Menge*`, `MengeGut*`, `IstRuesten*`, `IstEnde*`, `MandantenJahr*`, `WaehrungsKennzeichen*`, optional Arbeitsplatz-/Lohnanteil-Faktor, Ist-Kosten, `Zusatztext`, `BacklogPrimaryKeys[]` |
| `PpsRueckmeldungMaterialService` | `FertigungBelegHandle*`, `FertigungBelegPositionHandle*`, `Artikelnummer*`, `AuspraegungsHandle*`, `Mitarbeiternummer*`, `DatumZeitStart/Ende*`, `Menge*`, `MengeGut*`, `LagerplatzHandle*`, `LagerPositionen[]` (`PlatzId*`, `Kurzbezeichnung*`, `Menge`, `MengeBasis`, Seriennummern, Chargen mit Verfallsdatum), `WaehrungsKennzeichen*`, `Einzelpreis*`, `IstEinzelpreisManuell*`, `MandantenJahr*`, `Zusatztext`, `BacklogPrimaryKeys[]` |
| `PpsRueckmeldungFertigmeldungService` | `FertigungBelegHandle*`, `Mitarbeiternummer*`, `DatumZeitStart/Ende*`, `Menge*`, `MengeGut*`, `IstEnde*`, `IstVerursacherManuell*`, `LagerplatzHandle*`, `LagerPositionen[]`, `MandantenJahr*`, `Variante`, `Zusatztext`, `BacklogPrimaryKeys[]` – **ohne Positions-Handle** (bezieht sich auf den FA) |

`RueckmeldungGeneralSettings` (je Aufruf, laut DPS unabhängig von der Sage-Einstellung):
- `SetMaterialEntnahmeArt` + `MaterialEntnahmeArt`: `-1 = AutomatischMitArbeitsgang`, `0 = ManuellOderGesamtfertigmeldung`
- `SetMaterialZugangArt` + `MaterialZugangArt`: `1 = BeiRueckmeldungAufLetztenArbeitsgang`, `2 = BeiGesamtRueckmeldung`, `3 = BeiFertigmeldung`
- `CreateRueckmeldungenSubAuto` (Bedeutung ungeprüft – Name allein reicht nicht)
- Template-Default: beide `Set…` = false (→ dann gilt vermutlich die Sage-Einstellung; ungeprüft)

`BacklogPrimaryKeys[]` (`PrimaryKeyName*`, `PrimaryKeyValue*`, `PrimaryKeyType*`) gibt es im Request **und** in jeder `ResponseRueckmeldung`. Ob Sage
das nur in der Antwort zurückgibt (Zuordnung im selben Aufruf) oder speichert und später lesbar macht, ist **ungeprüft** – laut DPS-Antwort
vom 04.11. kann keine externe ID mitgegeben werden.

Rückmeldetypen-Enum `ExtRueckmeldungTyp`: 1 Arbeitsplatz, 2 Material, 3 Kosten, 4 Fremdfertigung, 5 Stillstand, 6 Fertigmeldung, 7 Verschnitt, 8 Werkzeug.

### Fertigungsauftrag-Services

- `FertigungsauftragService`: `ServiceOption*` = `1 NeuerFertigungsauftrag`, `2 Fertigungsauftragsaenderung`, `3 AddOrChangePositionMaterial`,
  `4 AddOrChangePositionArbeitsgang`, **`5 FertigungsauftragEinlasten`**; `FaBelegNeu` (Artikel, Sollmenge, Termine, `UnterbaugruppenErzeugen`,
  `SofortEinlasten`, `LosgroessenBeachten` …), Positionslisten Material/Arbeitsgang. Response mit `Handle`, Positions-Handles.
- `UpdateFertigungsauftragEntityService` / `…PositionEntityService`: einzelnes Feld setzen (`BelegHandle`, [`BelegPositionHandle`], `EntityFieldName`,
  `NewValue`, `UseSemaphore`).
- `PpsMitarbeiterKommtGehtService`: Mitarbeiter kommt/geht mit Pause.
- `PrintFABelegeService`: FA-Papiere drucken/exportieren.

### Was fehlt / offen
- **Kein eigener Service „FA beenden“** und kein Lese-Service für FA/Struktur. Ob `IstEnde` an der Fertigmeldung den FA beendet, ist ungeprüft
  (Feldname allein reicht nicht).
- Kein Feld für einen FA-Status-Check; laut DPS prüft der Service den FA-Status nicht.
- Nichts davon getestet (kein POST). Erst nach Freigabe durch Gerald und nur am Testsystem.

## 11. Erste Aufrufe der neuen Rückmelde-Services 2026-11-06 ~11:35 (Auftrag Gerald: „Tests durchführen“)

**Testdaten per SData nicht beschaffbar:** `dtsFaFertigungsauftragPos` (BelID 836) 200 leer; `dtsFaMaterialentnahmeschein`, `dtsFaEinzellohnschein`,
`dtsLagerplatzbestaende`, `dtsArtikelstamm` → 500; `dtsMitarbeiterstamm`, `dtsMitarbeitersuche`, `dtsLagerplaetzestamm` → 200 leer;
`dtsArbeitsplaetzestamm` → nur `9999`. FA 836 laut `dtsFaBelegsuche`: Struktur 835, Artikel 86003870, Soll/Fertigungsmenge 1, eingelastet, nicht begonnen,
**Belegjahr 2027, Belegnummer 0, PlatzID 0**; einzige Position laut `dtsTeileverwendungsnachweis`: MA 00722001, Menge 0,6 (keine AG-Position).
→ Für echte Zeit-/Material-/Fertigmeldungen fehlen Mitarbeiternummer, Lagerplatz (PlatzID) und BelPosID – nur per SQL/Sage-Oberfläche (Gerald).

**Test 1 – Fertigmeldung auf nicht vorhandenen FA (ohne Buchungswirkung):**
`POST CommonWawiServices/ake_TEST2026;1/$service/PpsRueckmeldungFertigmeldungService`, JSON, synchron, `FertigungBelegHandle 999999`,
`Mitarbeiternummer "0"`, `BacklogPrimaryKeys [{WmsTestId, TEST-2026-11-06-01, string}]`, Grundlagen nicht gesetzt.
- Antwort **HTTP 500** nach 0,8 s, JSON: `ResultNumber 500`, `ResultText "Rueckmeldung(en) fehlgeschlagen."`, `ErrorText` mit .NET-Stacktrace
  `System.ArgumentException: ValidateMitarbeiter … Mitarbeiternummer is not valid` (DPSBS.OL.SData.CommonWawiService…PpsRueckmeldungenController).
- `ResponseRueckmeldungen[0]`: `Handle 0`, **`BacklogPrimaryKeys` unverändert zurückgegeben**, eigenes `ResultNumber/ResultText/ErrorText`.
- Geprüft wird der Mitarbeiter **vor** dem FA; ob ein ungültiger FA abgelehnt wird, ist damit noch offen.

**Folgerungen**
1. Fachliche Fehler kommen als **HTTP 500** mit eigenem `ResultNumber`, nicht als 400 mit SData-`$diagnoses`. Die Retry-Entscheidung kann sich also
   **nicht** auf den HTTP-Status stützen (SData-Regel „500 nicht wiederholen“ passt zufällig), sondern muss `ResultNumber`/`ResultText` je
   Rückmeldung auswerten. Der Stacktrace gehört ins Protokoll, nicht in die Oberfläche.
2. `BacklogPrimaryKeys` wird im selben Aufruf zurückgegeben → Zuordnung Antwort ↔ WMS-Buchung im Batch möglich. Ob es gespeichert wird, zeigt erst
   eine erfolgreiche Buchung.

## 12. CommonWawiServicesResources jetzt lesbar 2026-11-06 ~18:00 (nur GET)

Seit der DPS-Installation liefert `CommonWawiServicesResources/$schema` 200 (am 30.10.: 500) mit 165 Ressourcen, darunter FA-Kopf, FA-Positionen
mit **BelIDNachfolger**, Arbeitsgänge, PPS-Mitarbeiter, Arbeitsplätze, Lagerplätze, Artikel mit allen USER-Feldern, Lagerplatzbuchungen.
`CommonWawiServicesResourcesSpecific` liefert die **mehrstufig aufgelöste Ressourcenliste** samt Benutzerfeldern. Details, Regeln (`orderBy` Pflicht)
und Lesetests: [[sage-sdata-commonwawiservicesresources]].

Folgerungen:
1. Struktur 835 vollständig gelesen: Kanten 835 → 836–841 über `BelIDNachfolger`, Positions-IDs (z. B. 836/MA 00722001 = **BelPosID 10641**,
   835/AG 999 = **BelPosID 10656**), Lagerplätze (Platz 11 „HR 0 0 0“, 1 „Hauptlager“). → Die Testdaten für §11 sind damit beschaffbar.
2. **`PpsMitarbeiterInfo` ist am AKE-Test leer** – es gibt keinen PPS-Mitarbeiter. Das erklärt die Ablehnung in §11 („Mitarbeiternummer is not
   valid“). Für jede Rückmeldung braucht es einen angelegten PPS-Mitarbeiter (Gerald/DPS). Arbeitsplatz nur `9999 TESTFertigungsArbeitsplatz`.
3. Kein Lesepfad für gebuchte Rückmeldungen.
4. W4 (Memo-Korrelation Lagerbuchung) bestätigt: 10 Buchungen `IdealAkeWms Lagerbuchung SM#…#` in `LagerplatzbuchungenInfo`.

## 13. Vollständiger Probeabruf und WMS-Katalog 2026-11-06 ~18:30 (nur GET)

Alle 179 Lese-Ressourcen mit count=1 abgefragt (166 × 200), Schreib-Services aus Schema/Enums ausgewertet, Stichproben je WMS-Bereich
(Personal, Lager, Bestand, Lagerbuchung-Delta über BuchungsID, Artikel-Lieferant, offene EK-Positionen, Dispo-Stapel). Ergebnis und Lücken:
[[sage-sdata-wms-schnittstellenkatalog]]. Keine Buchung, kein POST.

## 14. Schreibtests 2026-11-06 ~18:45 (Auftrag Gerald „Tests durchführen“; nur AKE-Test, nur umkehrbare Buchungen)

| Test | Aufruf | Ergebnis | Nachgelesen / zurückgesetzt |
|---|---|---|---|
| A1 Lagerumbuchung hin | `LagerbuchungService`, `Lagerbewegungsart "Umbuchung"`, 00722001, 1 lfm, LL/2864 → HR/11, Memo „IdealAkeWms Test Umbuchung T1 hin“ | 200, 0,8 s, `JobHandle 592143`, `BuchungsHandle 865823`, `Ok` | Bestand 2864: 199, 11: 1; `LagerplatzbuchungenInfo` 865823 Bewegungsart **IU**, Herkunft 2864 → Ziel 11, Memo gespeichert |
| A2 Lagerumbuchung zurück | dito HR/11 → LL/2864 | 200, `BuchungsHandle 865824`, `Ok` | Bestand wieder 2864: 200, Platz 11 leer ✔ |
| B1 Dispo-Stapel anlegen | `DispositionStapelService` `Stapelkennung WMSTEST01`, `Manuell`, `Neu`, Bestelltermin 13.11. | 200 `Ok` (Antwort mit leerer Stapelkennung) | `DispositionStapelInfo`: WMSTEST01, Typ 0 ✔ |
| B2 Bestellvorschlag anlegen | `DispositionStapelPositionenService` 00722001, MengeBasis 200, Lieferant K31013 fix | 200, `PosHandle 383` | `DispositionStapelPositionenInfo` ID 383: Sage ergänzte **Bestellnummer VISB00061, Einzelpreis 1,6725, Liefertermin 27.11.** (WBZ 14) |
| B3 Position löschen | dito `OperationMode Delete`, Handle 383 | 200 `Ok` | Positionen: 0 ✔ |
| B4 Stapel löschen | `DispositionStapelService` `OperationMode Delete` (2×) | 200 `Ok` | **Stapel WMSTEST01 bleibt bestehen (leer)** – Löschen wirkt nicht. Gerald: in Sage manuell löschen |
| C1 Kommen/Gehen | `PpsMitarbeiterKommtGehtService` Mitarbeiter „WMSTEST“ | **400** `ResultNumber 400` „Mitarbeiter 'WMSTEST' nicht gefunden.“ | `PpsMitarbeiterKommtGeht` 0 Einträge |
| C2 Material-RM | `PpsRueckmeldungMaterialService` FA 836 / BelPosID 10641 / 00722001 0,6 von 2864, Grundlagen „ManuellOderGesamtfertigmeldung“, `BacklogPrimaryKeys` | **500** „ValidateMitarbeiter … not valid“; `BacklogPrimaryKeys` zurückgegeben | Bestand 200, Position Istmenge 0 unverändert |
| D1 FA-Feld setzen | `UpdateFertigungsauftragEntityService` FA 841 `Zusatztext = "WMS-Test"` | 200 `Ok` | Zusatztext gesetzt ✔ – **`GeaendertAm` unverändert** (2026-02-02) |
| D2 FA-Feld zurück | dito `""` | 200 `Ok` | Zusatztext wieder null ✔ |

**Folgerungen**
1. **Zurückschreiben funktioniert**: Lagerumbuchung (Bewegungsart IU, Memo wird gespeichert → Idempotenz über Memo wie bei Zugang/Entnahme),
   Bestellvorschläge (Sage ergänzt Lieferanten-Bestellnummer, Preis, Liefertermin), FA-Felder.
2. **Dispo-Stapel löschen** über den Service wirkt nicht, obwohl `Ok` → Befund an DPS; im WMS Stapel wiederverwenden statt löschen.
3. **Fehlercodes uneinheitlich**: Kommen/Gehen 400, Rückmeldung 500 – je Service `ResultNumber` auswerten.
4. **Änderungen per Service setzen `GeaendertAm` am FA nicht** → Delta-Lesen über `GeaendertAm` übersieht sie. Für FAs periodisch voll abgleichen
   oder DPS nach Änderungsmarker fragen.
5. **BDE (Rückmeldungen, Kommen/Gehen) ist erst nach Anlage eines PPS-Mitarbeiters testbar** – technisch ist der Aufruf vollständig, die Ablehnung
   erfolgt fachlich in Sage.

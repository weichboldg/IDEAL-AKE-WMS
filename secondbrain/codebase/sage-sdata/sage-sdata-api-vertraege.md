---
type: codebase-karte
updated: 2026-10-30
quelle: "$schema der Verträge apiArtikel/apiBelegerfassung/apiKunden/apiLieferanten.Sage.API, apiDMSConnector.100036671.DPSBS_DMS_Connector, CommonWawiServicesResourcesSpecific – AKE-Testserver, ausgelesen 2026-10-30"
---
# Sage SData – API-Verträge (`api*.Sage.API`), DMS-Connector, ResourcesSpecific

Übersicht und Aufrufkonventionen: [[sage-sdata-uebersicht]]. Das sind die **dokumentierten Sage-100-API-Verträge** (Versionen 1.0.0/1.1.0):
Entitäten (`ept*`) mit REST-Semantik. Rechte: `G`=GET, `P`=PUT, `C`=POST, `D`=DELETE (aus `sme:canGet/Put/Post/Delete`).
Jede Entität hat eine Schwester `…udf` (nur GET) mit den Benutzerfeldern (`USER_`), und `CustomFields[]{Name, Value}` für generischen
Zugriff darauf. `dtr*` = Nachschlagelisten (Dropdown-Quellen), `USysUserDefRef_*` = AKE-eigene Auswahllisten.
Aufruf: `GET/POST/PUT/DELETE {Basis}/{Vertrag}/{Dataset}/{Entität}` bzw. `…/{Entität}(Schlüssel)`; Lesetests stehen noch aus
(2026-10-30 nur Schema gelesen).

## apiArtikel.Sage.API (v1.0.0)

**eptArtikel** [GPCD, „Artikel“]: Artikelnummer(31)*, Bezeichnung1(50), Bezeichnung2(50), Matchcode(50), Artikelgruppe(15),
Hauptartikelgruppe(15), Vaterartikelgruppe(15), Steuerklasse*, Verkaufsmengeneinheit(5), DezimalstellenVK*, Basismengeneinheit(5)*,
DezimalstellenBasis*, Lagermengeneinheit(5), DezimalstellenLager*, UmrechnungsFaktorLME, UmrechnungsfaktorVK, Lagerfuehrung,
Besteuerungsart*, AuspraegungID, Langtext, Dimensionstext, Erloescode*, Gewichtseinheit(10), Hersteller(20), IstBestellartikel*,
IstRabattfaehig*, IstSkontierfaehig*, IstVerkaufsartikel*, PreiseinheitVK*, Gewicht*, GewichtLME, KalkulatorischerEK,
**Meldebestand***, **Hauptlagerplatz**, EANNummer(15), CustomFields[]
- Für das WMS: deckt die Felder des heutigen `SageImportService.SyncArticlesAsync` (Artikel, Hauptlagerplatz, Meldebestand) – ein
  **Lesepfad ohne Direkt-SQL** auf `KHKArtikel`; schreibend (PUT) wäre Meldebestand-Pflege aus dem WMS möglich.

**eptArtikel.udf** (AKE-Benutzerfelder des Artikelstamms, Auszug der ~150): KHKArtikel_USER_… AbtauFuehler, DruckRL* [Andruck
Stückliste], AnlageDate/AnlageUser, Ausfuehrung [Anschlussart], Anschlusswert, Artikeltyp, **AUSCAD**, Ausstellflaeche B/H/T,
Betriebsdruck, Bez1VK/Bez2VK, Breitemm/Hoehemm/Tiefemm, CO2e, Daempfer, PfadGlaszeichnung, **DPSBestand**, **DruckFertigungsauftrag***,
eClass, EL* (Energielabel-Felder: EEI, EnergieEffKlasse, EnergielabelJN, 24hEnergVerb, jaehrEnergVerb, FaktorC/M/N/P, Klimaklasse,
ModellKennung, SAE, TempKlasse, Warpraeflae, verwNormen, Verwendung, HerkunftEVerbrauch, ArtKuehlmoebel, ArtLichtquelle, Beschreibung),
EPDMVERSION, EPRELID, EKArtNrPraefix, Einbauoeffnung B/H/T, Frequenz, Fuellmenge/2/3, Fuehler, Geraeuschpegel, GWP, HeizplatteZWB,
HeizungBefeuchtung, IdealEnergieEffKl, Innenwanne B/H/T, **IstIDEAL**, ISTEKVorlage, IstKatalogartikel*, IstZubehoer, Kaelteleistung/2/3,
Kaeltemittel/2/3, Klimaklasse, KuehlmoebelFamilie, KurztextD/E/FR/NL(1000), ZusatzheizungLeistung, LeistungsaufnahmeK/W, Licht*,
Beleuchtung1/2 Typ/Anzahl/Farbe, BeleuchtungTrafo, Luefter, **Maschine**, **MasterartikelNr**, Modell, Nennstrom, NettoInhaltLiter,
Netzstecker, NummernkreisVorlageartikel, PfadBenutzerhandbuch/Foto/Programmieranleitung/Schaltplan(250), PraeferenzBeguenstigung,
PraefixNummernkreis, Primaerheizplatte, Produktfamilie, Raumtemperatur, Rahmenheizung1/2, RueckluftFuehler, Schutzklasse,
Softwareversion, Spannung, Steuerung, Stuetzwaerme, Temperaturbereich, tmpBreite/tmpLaenge, TypenschildArtikel, UpdateArtikel,
**VaultID**, Ventil, Verdampfer (+Ein-/AusgangFuehler), shopVideoD/E/FR/NL, VT2/VT3, Waermebruecke, Webshop/-Anzeige/-ID1,
ZuluftFuehler, Zusatzkomponenten; KHKArtikelVarianten_USER_MEKNeu

**eptArtikelgruppenSuche** [G]: Artikelgruppe, Bezeichnung, Gruppenebene, HatUntergruppen, Hauptartikelgruppe, VaterArtikelgruppe
Nachschlagelisten: dtrBesteuerungsartUmkehrschuld, dtrErloescode, dtrSteuerklasse, dtrMengeneinheit; USysUserDefRef 1000001103
(Typenschild-Typen), 1000001115 (Webshop Artikeltyp), 1000001119 (Kaeltemittel)

## apiBelegerfassung.Sage.API (v1.0.0)

**eptEKBeleg** [G C D – kein PUT, „Einkaufsbeleg“]: Handle, Kennzeichen* [Belegtyp], Belegart, Belegjahr, Belegnummer, Belegdatum*,
A0Konto [Lieferant], Referenznummer(35), Nettobetrag, Bruttobetrag, Positionen[]{Handle, Artikelnummer, AuspraegungsHandle, Menge,
Einzelpreis, Gesamtpreis, Rabatt}, CustomFields[]
- **eptEKBeleg.udf**: KHKEKBelege_USER_ **ABErhalten*** [AB Erhalten], **AB(250)** [AB-Nr.], DPSbsBelegsperre, CWIStatus,
  DPSBSBelegherkunft(50), DPSExportCSVPDF, gfksKonsiJobID, Kom(200) [Kommission], DPSBSEiltPrio, OLSIWebCommerceID, RMA*, **WA(250)**
  [WA-Nr.], Zeichnungspfad*; Positionen: RMA*
- **eptEKBelegPosition.udf**: KHKEKBelegePositionen_USER_ **ABPos(15)** [AB-Pos des Kundenauftrags], gfksKonsiBuchungsID,
  **bestaetigt** [Liefertermin bestätigt], DPSbsMDEDeviceID, DPSBSEiltPrio, DPSbsMDEFreigabe, DPSBSMDEMA, DPSbsMDESortIndex, RMA*,
  RMAVKBelID, RMAVKBelPosID, VorPosIDRahmenkontrakt, **Zielort(20)**
- **eptEKBelegSuche** [G]: Belegjahr, Belegnummer, BelID, Lieferantennummer, Lieferantenmatchcode, Belegdatum, Belegart,
  Referenznummer, Liefertermin, Bearbeiter, Nettobetrag, WKz (+udf mit denselben Kopf-USER-Feldern)
- Bedeutung: schlanker **Lesepfad auf Bestellungen** (Suche + Positionen) und Neuanlage; Positionen hier ohne Liefertermin/Lager –
  dafür `StandardBelegService` ([[sage-sdata-commonwawiservices]]).

**eptVKBeleg** [G C D, „Verkaufsbeleg“]: wie EK mit A0Konto [Kunde]; udf u. a. **IdealAB(70)*** [Ideal Belegnummer], ParcelID(100),
prodtermin, TechnBearbeiter, DPSBSMDETransitbeleg, Typenschild, Versandkz, VorgangErfuellt, Zeichnungspfad*, BestelldatumKunde
- **eptVKBelegPosition.udf**: AbwLANS* (abweichende Lieferanschrift), Bez1VK/Bez2VK, CO2e, **FABelId** [FA-Nummer], Frequenz,
  Fuellmenge*, GWP, Kaelteleistung*, Kaeltemittel*, Klimaklasse, LeistungsaufnahmeK, DPSBSMDETransitErledigt, DPSbsMDE*, MemoVerladung,
  Anschlusswert, RMA*, Spannung, TypenschildArtikel, Verpackungsstatus, Versand, ELverwNormen, VT2/VT3
- **eptVKBelegSuche** [G]: wie EK mit Kundennummer/Kundenmatchcode
Nachschlagelisten: dtrBenutzerHistorisch, dtrMandantJahr, dtrWaehrung, dtrAnzeigeEKBelegerfassung/-VK (Kennzeichen, Bezeichnung),
dtrEKBelegart, dtrVKBelegartenAlle; USysUserDefRef 1000001102 (technSB), 1000001103, 1000001104 (Versand), 1000001119

## apiKunden.Sage.API (v1.1.0)

**eptKunden** [GPCD]: Kto(20)*, Matchcode(50)*, Besteuerung*, EUUStID(15), Zahlungskond(10), Preiskennzeichen*, Gruppe(10),
Anrede(50), LieferLand(3), LieferOrt(40), LieferPLZ(24), LieferStrasse(64), AdressenMatchcode(50)*, Name1(50), Name2(50),
A1Besteuerung*, EULand(2), CustomFields[]
- udf: KHKKontokorrent_USER_ GFKSKONSILAGERPLATZID, Kundenrabatt, EKMindestbestellwert, DPSbsHatSammelsendung, DPSbsSammelsendungWann,
  ServiceHotline, Typenschild, Vertreter1/2, Webshop, Zessionstext; KHKAdressen_USER_ AcadDateien, TextDigSig*, EmailDigSig,
  ExterneAdressnummer, **KundenNameFA(20)**, NewsletterErwuenscht*, WebPasswort, FvWebshopAdresstyp, ZustimmungDigSig*
Nachschlagelisten: dtrAnredeBezeichnung, dtrBesteuerungsart, dtrEULand, dtrKundengruppe, dtrLandkennzeichen, dtrOrte,
dtrZahlungskonditionAdressenstamm; USysUserDefRef 1000001103, 1000001114 (Webshop Vertreter)

## apiLieferanten.Sage.API (v1.1.0)

**eptLieferanten** [GPCD]: Kto(20)* [Lieferantennummer], Matchcode(50)*, Besteuerung*, EUUStID(15), Zahlungskond(10),
Preiskennzeichen*, Gruppe(10), A1Besteuerung*, Anrede(50), LieferLand(3), LieferOrt(40), LieferPLZ(24), LieferStrasse(64),
AdressenMatchcode(50)*, Name1(50), Name2(50), EULand(2), CustomFields[]
- udf: identisch mit Kunden (gemeinsame Tabellen KHKKontokorrent/KHKAdressen), u. a. **EKMindestbestellwert**
- Nachschlagelisten wie Kunden, plus dtrLieferantengruppe
- Bedeutung: **Lieferantenstamm lesen** (für Bestellvorschlag/Bedarfsmeldung) ohne Direkt-SQL. Keine Lieferanten-Artikel-Zuordnung
  (Preise, Lieferzeit) in diesem Vertrag – die liegt in `KHKArtikelLieferanten` bzw. den `dts`-Quellen ([[sage-sdata-realtimedata]]).

## apiDMSConnector.100036671.DPSBS_DMS_Connector (v9.0.8001, Sage-Partner)

Zweck: Rechnungseingang/DMS-Anbindung des Partners. Für das WMS interessant sind die **Leseressourcen zum Einkauf**:
- **eptEinkaufsbelege** [G]: VorID, Bestellung, Referenznummer, Referenzzeichen, Bestelldatum, Lieferantennummer, Lieferant, VorPosID,
  Bestellposition, Artikelnummer, Bezeichnung1/2, Menge, MengeBasis, Einzelpreis, Rabatt, Rabattbetrag, GesamtpreisInternEW,
  **VorgangErfuellt, PositionErfuellt**, Wareneingang, WE_Belegdatum, **WE_Menge**, WE_MengeBasis, WE_Einzelpreis, WE_Gesamtpreis,
  Rechnungseingang, RE_*, Stornorechnung, ST_*, Gutschrift, GS_* – **Bestellung → Wareneingang → Rechnung je Position in einer Zeile**
- **eptOffeneVorgaengeEK** [G]: VorID, BelID, Belegkennzeichen, Belegart, BelegnummerFormatiert, Belegdatum, Lieferantennummer,
  Lieferant, Referenznummer, Referenzzeichen, Nettobetrag, Bruttobetrag
- **eptLieferanten** [G]: Kreditorennummer, Kreditorenname1/2, Land/PLZ/Ort/Strasse, Referenz, Email, Telefon, EUUStID, Mandant
- eptKostenstellen, eptKostentraeger, eptSachkonten, eptSteuerschluessel [G]; eptBuchungen/-Positionen [GPCD] (Rewe-Buchungen mit
  externalId), eptRechnungseingang [C] (BelID, Rechnungsnummer, Rechnungsdatum, BelIDZielbeleg); dtrBuchungskreise
- Ob der SData-Benutzer diesen Partner-Vertrag lesen darf, ist **ungeprüft**.

## CommonWawiServicesResourcesSpecific (nur GET)

- **SortedVerkaufKommissionierungPicklistenItems / …Full**: Picklisten-Positionen des Sage-Kommissionierlaufs mit Lagerplatz, Mengen
  (ZuPicken/Gepickt/Geliefert), Chargen/Seriennummern/Zugangskennungen, Kundendaten, Pflicht-Parameter `Mandant`,
  `KommissionierID*`, `PickPosID*`, `PickID*` (Gegenstück zu `PicklistItemsEditService`)
- PicklistenItemLager / -Chargen / -Seriennummern / -Zugangskennungen (Teilstrukturen)
- GetSammelmappenFile / -Content (Mandant*, Id*, Quelle* → FileByteArray), LicenseOptions, UserLicense (Mandant*, UserName* →
  IsSysAdmin, LicenseOptions[]), CustomFields, SpecificUserfields
- Sage-Kommissionierung wird im WMS nicht genutzt (eigene Kommissionierung) – nur relevant, falls Picklisten je aus Sage kommen.

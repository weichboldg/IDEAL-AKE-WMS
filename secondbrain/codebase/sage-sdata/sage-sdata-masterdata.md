---
type: codebase-karte
updated: 2026-10-30
quelle: "$schema des Vertrags MasterData (2,46 MB), AKE-Testserver, Dataset ake_TEST2026;1, ausgelesen 2026-10-30"
---
# Sage SData – Vertrag `MasterData` (AppDesigner-Datenstrukturen)

Übersicht und Aufrufkonventionen: [[sage-sdata-uebersicht]]. **Das ist die interne Objektschicht des Sage-100-Clients**
(„Sage Office Line Master-Data“), nicht die dokumentierte Partner-API. 790 Ressourcen, 357 Service-Operationen.

Drei Ressourcenarten (Präfix), Rechte aus `sme:canGet/Put/Post/Delete` (G/P/C/D):
- **`str…`** = Datenstruktur eines Dialogs/Formulars (Tag `DataStructures`). Meist nur GET; einige mit GPCD und
  `…/$service/Execute` (Request: `MethodName:string, DataKey, PartKey, Data:<Struktur>, AutoCorrect, NamedParameters[]`;
  Response: `Data:<Struktur>`). **Die gültigen `MethodName`s stehen nicht im Schema.** Viele Felder sind UI-Zustand
  (`Menu*`, `*Load`, `*Old`, `IsDirty`, `IsSelected`, `IsTemporarySaved`).
- **`rec…`** = Datensatz einer Tabelle (Tag `DataRecords`, GPCD + `CopyRecord`), z. B. `recLieferantenstamm`, `recLagerplaetzestamm`.
- **`dvl…`** = Einstellungen (GPC).
- `NamedParameter` (G): Hilfsressource für `Execute`.

**Lesetest 2026-10-30:** `GET MasterData/{ds}/strFaBeleg.Sage.PPS?count=1` und `…(818)` → **HTTP 403** für den SData-Benutzer.
Ob `rec…`-Ressourcen lesbar sind, ist **ungeprüft**. Vor jeder Nutzung: Rechte/Lizenz beim Sage-Partner klären.

## Pakete (Name nach dem ersten Punkt)

| Paket | str | rec | dvl | Inhalt |
|---|---|---|---|---|
| `Sage.PPS` | 19 | 3 | – | Produktion (unten vollständig) |
| `Sage.Wawi` | 154 | 125 | 4 | Artikel, Belege VK/EK, Lager, Inventur, Preise, Projekte, Webshop |
| `Sage.System` | 66 | 61 | 3 | Adressen, Kunden/Lieferanten-Stamm, Bank, Steuern, Druckprozesse, E-Rechnung |
| `Sage.Rewe` | 169 | 43 | 5 | Rechnungswesen (nicht dokumentiert) |
| `Sage.xRM` | 37 | 32 | – | CRM (nicht dokumentiert) |
| `100036671.*` (Sage-Partner) | | | | `AKE_ArtikelKontrolllisteVK`, `AKE_DispoMeldebestandBestellung`, `AKE_Energielabel`, `AKE_Erweiterungen` (EK/VK-Beleg, TablarAusfahren, Garantieablöse), `AKE_InterneFA` (LOGFA, LOGFAListe, UnvollstLieferung), **`AKE_InterneLagerbestellungen`**, `AKE_OseonDataImport`, `AKE_RMA`, `DPSBS_DMS_Connector`, **`DPSBS_Etikettendruck`** (11 Etikett-Strukturen inkl. FA-Artikeletikett, Lagerplatzetikett), `DPSBS_Inventurliste`, `DPSBS_KomfortpaketWawi` (Lagerbewegungsprotokoll, Beleghistorie, Ladenhüter), `DPSBS_Konsi_EK_VK`, `DPSBS_MEKKorrektur`, `DPSBS_PDMInterface`, `DPSbs_TF_Etiketten`, **`IdealAKE_BelegStammdatenSync`** |

## Sage.PPS – Produktion (vollständig)

| Ressource | Label | Rechte | Felder |
|---|---|---|---|
| `strFaBeleg.Sage.PPS` | Fertigungsauftrag | **GPCD + Execute** | 152 Felder, s. u. |
| `strFaPosition.Sage.PPS` | FA Position | G (Teil von strFaBeleg) | 149 Felder, s. u. |
| `strFaAgPosition.Sage.PPS` | FA Arbeitsgangposition | G | AGPOS_SollruestzeitSekunden, SollEnd-/Sollstarttermin, IsTerminierungNotwendig, Wkz, Position, BelegHandle, BelegPositionsHandle, **BelegArbeitsgangPositionsHandle**, Leistungsgrad, Prozentanteil, Aenderungsindex/-kennzeichen, Soll-/IstArbeitsplatzkosten Ruesten/Betrieb, Soll-/IstLohnkosten Ruesten/Betrieb, Wartezeit(+Text), RessourceMatchcodeAG, RessourceNummerAG, RessourceTypAG(+Text), DataserviceTag, Dimensionstext |
| `strFaFertigungsprojektBeenden.Sage.PPS` | FA Beenden | G | Fertigungsauftrag:int, UnterbaugruppenMitBeenden:int |
| `strFaFertigungsprojektReaktivieren.Sage.PPS` | FA Reaktivieren | G | Fertigungsauftrag, UnterbaugruppenAuchReaktivieren, MitDispoResourcenbelegung |
| `strFaNeuAnlage.Sage.PPS` | FA anlegen | G | Artikelnummer, Variante, Matchcode, Projektnummer, Kostentraeger, Kostenstelle, Fertigungsmenge, Terminierungsart, Terminierungsdatum, BeachtungDerKapazitaetsbelegung, LosgroessenBeachten, AuftraegeSofortEinlasten, UnterbaugruppenErzeugen, Variantentext, AuspraegungID, ErsteBelegnummer, AufruferHandle, FertigungsmengeNKS, VerursacherAnzeige, Zusatztext, TerminierungsartManuell, AufrufAus, PosKostenstelle/-traeger, Bezeichnung1/2Vk, DimensionstextVk |
| `strFaLoeschen.Sage.PPS` | FA Löschen | G | MitUnterbaugruppen, Fertigungsauftrag, HatUnterbaugruppe |
| `strFaGeheZu.Sage.PPS` | Gehe zu | G | Belegnummer |
| `strFaPosNeuTerminieren.Sage.PPS` | Terminieren | G | Terminierung, KapazitaetsbelegungBeachten, Terminierungsgrenze |
| `strFaRessourcenlisteAktualisieren.Sage.PPS` | Ressourcenliste aktualisieren | G | KapazitaetsgruppenErhalten, MitArbeitsgaengen, MitFremdfertigung, MitMaterial, MitVerschnitt, MitWerkzeugen |
| `strFaVerursacher.Sage.PPS` | FA Verursacher | G | VU_Menge, VU_BasismengeZugeordnet, VU_VerursacherHandle/-SubHandle, VU_ReferenzHandle/-SubHandle, VU_MengeBasis, VU_VerursacherTyp |
| `strFaBelegsprache.Sage.PPS` | Belegsprache | G | Sprache, mitBelegkopf, mitFremdfertigungspositionen, mitMaterialpositionen, TexteBehalten, Aufruf |
| `strPpsBeleg.Sage.PPS` / `strPpsFfBeistell.Sage.PPS` | Druck FA / Beistellartikel | G | ConnID, PrintHandle (+BelID) |
| `strRessourcenlisteKopf.Sage.PPS` | Ressourcenliste (Artikel-Stückliste/Arbeitsplan) | **GPCD + Execute** | Artikelnummer, Variante, Bezeichnung1/2, Matchcode, Memo, ZeichnungsNummer, Von-/BisMenge, IstFremdfertigung, Produktionszeit(+Umrechnungsfaktor), PositionenDeleted, AuspraegungsHandle, Status(+Index), IstBearbeitbar, IstAnlegbar, Von-/BisDatum, Gewicht, SpracheFlags, Sprache, KHKPpsRessourcenKopf_USER_VKZuschlag, `RessPositionen` (Liste strRessourcenPosition) |
| `strRessourcenPosition.Sage.PPS` | Ressourcenliste Position | G | 84 Felder: POS_RessourceTyp, POS_RessourceNummer, POS_ArbeitsgangNummer, POS_Menge, POS_Ruestzeit, POS_Stueckzeit, POS_Planzeit, POS_Pufferzeit, POS_Transportzeit, POS_Ueberlappung*, POS_Leistungsgrad, POS_RessourceNummerArbeitsplatz, POS_Wkz, POS_Fremdfertiger*, POS_Materialteileart, POS_PositionExtern(+Praefix/Suffix), POS_Dim1-3, POS_FormelHandle, POS_IstCadFixPos, KHKPpsRessourcenPositionen_USER_ AUSCAD, EPDMVERSION, Positionsnummer, VKEinzelpreis, VKPreis; `AgPositionen` (strRessourcenAgPosition) |
| `strRessourcenAgPosition.Sage.PPS` | Ressourcenliste AG-Position | G | AGPOS_PositionTyp, BelegArbeitsgangPositionsHandle, BelegPositionsHandle, Leistungsgrad, Position, RessourceMatchcodeAG, RessourceNummerAG, RessourceTypAG(+Text), Wartezeit, Wkz |
| `strRessourcenlisteKopfNeuGewicht.Sage.PPS` | Gewichtsberechnung | G | StufeBerechnung |
| `strTeileverwendungsnachweis.Sage.PPS` | Teileverwendungsnachweis | G | Artikelnummer, Arbeitsgangnummer, Arbeitsplatznummer, Fremdfertiger, Werkzeugnummer, Mitarbeiternummer, RessourceTypTVW, PosAuspraegungID |
| `recArbeitsplaetzestamm.Sage.PPS` | Arbeitsplätzestamm | GPCD | Aktiv*, **Arbeitsplatznummer(31)***, Matchcode*, Bezeichnung1/2, **BdeTerminalId(20)**, BDETerminalBezeichnung, ArbeitsplatzkostenBetrieb/Ruesten, LohnkostenBetrieb/Ruesten, Fertigungsgemeinkostenzuschlag, Zusatzkosten, Kostenstelle, Kostentraeger, Leistungsgrad, NurBarcode*, Pausenabzug*, Schichtplannummer, Wartezeit, Memo |
| `recArbeitsgaengestamm.Sage.PPS` | Arbeitsgängestamm | GPCD | Aktiv*, **Arbeitsgangnummer(31)***, Matchcode*, Bezeichnung1/2, Arbeitsplatznummer, Artikelnummer, AuspraegungId, Lieferant, DefaultRessourceNummer(+Arbeitsplatz/KapGruppen), DefaultRessourceTyp, Ruestzeit, Stueckzeit, StueckzeitFremd, TransportzeitFremd, Pufferzeit, Ueberlappung (menge/prozent/typ/zeit), Umrechnungsfaktor, Dimensionstext, Memo |
| `recWerkzeugestamm.Sage.PPS` | Werkzeugestamm | GPCD | (nicht ausgelesen) |

### strFaBeleg.Sage.PPS (Fertigungsauftrag, 152 Felder)

Kopf: IsDispoDirty, IsTerminierungNotwendig, DezimalstellenLager, **SollStarttermin, SollEndtermin, GesamtZeitSoll, GesamtZeitIst**,
**BelegHandle, StrukturHandle**, Belegjahr, Belegnummer, Artikelnummer, Variante, Bezeichnung1/2(50), FaMatchcode, Memo,
DimensionsText, Zusatztext, PkonfigHandle, Zeichnungsnummer(50), TerminierungsArt, **Sollmenge, Fertigungsmenge, IstMenge**,
**IstStarttermin, IstEndtermin**, VerursacherTyp, **IstEingelastet, IstBegonnen, IstBeendet, IstReaktiviert**, Aenderungsindex,
AusfuehrungsArt, Bestellmenge, IstEinlastungTermin, IstBestelltTermin, IstGeliefertTermin, Fertigungsart, Projekt, Kostentraeger,
Kostenstelle, IstGesperrt, IstCadFreigegeben, Sprache, AuspraegungsHandle, VarianteWawi, FremdfertigerKonto, BelegHandleIntern,
BelegHandleStruktur, BelegPositionsHandleStruktur, BelegArbeitsgangPositionsHandleStruktur, EinlastenBelegungDispo,
SimulationStatus, MitWiederbeschaffungszeit, ProduktionszeitTage(+Umrechnungsfaktor), PKonfigPositionsHandle, LossgroessenBeachten,
VerursacherHandleIntern, **IstUnterfertigungsauftrag**, IstAktiv, AngelegtVon/Am, GeaendertVon/Am, ArchiviertVon/Am, PositionPlatzHandle,
PositionHandle, BestandAktuell, VerfuegbarAktuell, BestandLagerort, IstErsatzteil, IstVerschleissteil, Mengeneinheit1/2,
KST-/KTR-/ProjektBezeichnung, NKSLager, Lagerplatzbezeichnung, ID, Matchcode(50), PlatzHandle, SpracheFlags, DataserviceTag,
IstDMSAktiv, KapazitaetsBelegungPruefen, SofortEinlasten, TerminierungsDatum, UnterbaugruppenErzeugen, VerursacherHandle,
VerursacherBelegHandle, Anzeige* (Matchcode, Belegnummer, Artikelnummer, Fertigungsart), Tag, BelegHandleSearch, **HatUnterbaugruppen**,
VerursacherAnzeige, IstBearbeitbar, IsNeuanlage, AenderungsindexAnzeige, SpeicherModus, SpeichernMitStruktur, IstAnlegbar,
VerursacherPositionsHandleIntern, MatchcodeArtikel, AuspraegungText, AuspraegungID
AKE-USER-Felder (KHKPpsFaBelege_USER_, je 250): Maschine, Auszug, Bedienungsanleitung, K-/WBeleuchtung, **DPSBSBereitstellungBeendet**(bool),
Bodenplatte, Dichtung, Dimmer, Elektronik(+Steuerung), FaSN, **DPSBSMDEFreigabe**(bool), Frontscheibe, Fuellmenge, Gasdruckdaempfer,
Glasdeckel, HeizplatteZBmitHeizung, Kaeltemittel, Luefter, Programmieranleitung, Quarzstrahler, Rahmenheizung, SchiebeFluegeltueren,
Seitenscheiben, Sonderausfuehrung, KktTauwasser, Ventil, Verdampfer, VermerkProduktion, Vorschaltgeraet, Waermestrahler,
Zusatzkomponenten, ZBohneHeizung, Zwischenborde
Listen: `FaPositionen` (strFaPosition), `FaVerursacher` (strFaVerursacher)

### strFaPosition.Sage.PPS (FA Position, 149 Felder)

POS_DezimalstellenLager, POS_HatLagerfuehrung, POS_UmrechnungsfaktorLME, POS_IsTerminierungNotwendig, **POS_BelegHandle,
POS_BelegPositionsHandle, POS_BelegHandleNachfolger** (= Sub-FA-BelID, 0 bei Blatt), **POS_RessourceTyp, POS_RessourceNummer,
POS_ArbeitsgangNummer**, POS_Matchcode(50), POS_Dimensionstext, POS_Menge, POS_MengeNks, POS_MengeBerechnung/-Dimensionen,
POS_IstFixtermin, POS_BelegPositionsHandleIntern, POS_BelegHandleNachfolgerIntern, POS_ArbeitsgangPositionenDeleted,
POS_Planzeit(+Text), POS_Ueberlappung* (Typ/Prozent/Menge/Zeit), POS_BelegungDauer/-Ende, POS_Bedarfstermin, POS_Umrechnungsfaktor,
POS_IstEinmalmenge, POS_Ruestzeit, POS_Stueckzeit, POS_Pufferzeit, POS_Lagermengeneinheit, **POS_Sollmenge, POS_Fertigungsmenge,
POS_IstMenge, POS_SollStarttermin, POS_IstStarttermin, POS_SollEndtermin, POS_IstEndtermin, POS_SollRuestzeit, POS_IstRuestzeit,
POS_SollStueckzeit, POS_IstStueckzeit, POS_Status**, POS_Aenderungsindex/-kennzeichen, POS_Soll-/IstArbeitsplatzkosten
Ruesten/Betrieb, POS_Soll-/IstLohnkosten Ruesten/Betrieb, POS_SollEinzelpreis, POS_IstGesamtpreis, POS_AusfuehrungsArt(+Arbeitsgang),
POS_Bestellmenge, POS_IstEinlastungTermin, POS_IstBestelltTermin, POS_IstGeliefertTermin, POS_UnterBaugruppenErzeugen,
POS_Lieferausgangsmenge, POS_Transportzeit, POS_PlatzHandle, POS_BelegPositionsHandleRessourcenliste, POS_Materialteileart(+Old),
POS_Schwund(+art), POS_IstCadFix, POS_PositionCad, POS_AuspraegungsHandle, POS_VarianteWawi, POS_HatVariantenWawi, POS_Fertigungsart,
POS_Artikelnummer, POS_FertigungsmengeBelegkopf, POS_Leistungsgrad, POS_Variante, POS_BelegPositionsHandleArbeitsplatz,
POS_RessourceNummerArbeitsplatz, POS_Anzahl, POS_Dim1-3, POS_FormelHandle, POS_PlatzHandleProjekt, POS_SimulationStatus,
POS_WiederbeschaffungsTermin, POS_Lagermenge, POS_Bedarfsmenge, POS_VerfuegbareMenge, POS_VerplanteMenge,
POS_WiederbeschaffungszeitTage, POS_Bestelltermin, POS_Ruesten, POS_BestellteMenge, **POS_BdeTerminalHandle,
POS_BdeMitarbeiterNummer, POS_FortschrittMenge, POS_FortschrittZeit**, POS_IstArtikelDispoAktualisieren, POS_IstZeitMenge,
POS_PkonfigPositionsHandle, POS_IstAktiv(+Old), POS_Preis, POS_Fremdfertiger* (Matchcode/Konto/Bezeichnung), POS_Wkz,
POS_AnzahlKapazitaeten, POS_ArbeitsgangMindestMenge, POS_Position, POS_PositionExtern(+Anzeige/Praefix/Suffix),
POS_RessourceMatchcodeAG, POS_RessourceNummerAG, POS_RessourceTypAG(+Text), POS_BdeTerminalBezeichnung, POS_BDEMitarbeiterMatchcode,
POS_LagerplatzText, POS_DataserviceTag, POS_ArbeitsgangMatchcode;
KHKPpsFaBelegePositionen_USER_ **DPSBSBereitstellungHLager**(int), **DPSBSBereitstellungMA**(100), **DPSBSGruppierung**(50);
Liste `AgPositionen` (strFaAgPosition)

## Sage.Wawi – Einkauf (Bestellung, Wareneingang)

**`strEKBeleg.Sage.Wawi`** [GPCD + Execute, „Einkaufsbeleg“, 309 Felder + 18 USER]. Kopf (Auszug): Handle, Erfassungsart, VorgangsHandle,
Belegnummer(+Formatiert), Kennzeichen*, Belegart, Belegdatum*, Belegjahr, Periode, Wkz, Fremdwaehrungskurs, Zwischensumme, Netto-/
Steuer-/Bruttobetrag, Rabatt1-3 (+Betrag/Basis/Text), A0Konto, A0Matchcode, A0/A1/A2/A3/A4 Anrede/Name1/Name2/Zusatz/Strasse/Land/
Postleitzahl/Ort, A1AdressNummer, Ansprechpartner-Handles, **Liefertermin***, Lieferwoche, **LagerplatzHandleWareneingangslager**,
LagerplatzHandleWarenausgangslager, Besteuerung*, Kontocode*, HatBruttopreise*, HatDrucksperre, Bezeichnung, Matchcode(50), Sprache*,
Kopf-/Fusstext(+Rtf), Bearbeiter, Memo, Formularvariante, EULand, EUUmsatzsteuerId, Versand, Lieferbedingung, Referenznummer(35),
Referenzzeichen(80), Kostenstelle, Kostentraeger, Valutadatum*, Status, StatusDatum, StatusBearbeiter, BelegHerkunft, HandleReferenzbeleg,
IstGeparkt, EkBudgetperiode, EkLieferschein(20), EkFremdfertiger(20), IstSammelbeleg, RAV* (Rahmenvertrag), MindestbestellwertUnterschritten,
Konsi*; USER: KHKEKBelege_USER_ AB, ABErhalten, Kom, WA, Zeichnungspfad, DPSbsBelegsperre, DPSBSEiltPrio, … (s. [[sage-sdata-api-vertraege]])
Listen: `Positionen` (strEKBelegPosition), `Zuschlaege`, `Staffelrabatte`, `Steuern`, `ZKDs`, `DMSCloudDokumenteBelege`

**`strEKBelegPosition.Sage.Wawi`** [236 Felder + 15 USER] (Auszug): Handle, BelegHandle, VorgangspositionsHandle, Positionstyp*,
Positionsnummer, Artikelnummer, AuspraegungsHandle, Matchcode, Bezeichnung1/2, Dimensionstext, Langtext, **Menge, MengeBasis,
MengeHandel, MengeVerpackung**, Mengeneinheit*, Basis-/HandelMengeneinheit, Umrechnungsfaktoren, Einzelpreis, Gesamtpreis, Rabatt,
Steuercode, Sachkonto, **GleichgewichtsstatistikBestellt/Geliefert/Berechnet/Retour** (Bestell-/Liefer-/Rechnungsstand),
GG*Status, **IstErledigt**, HatLagerfuehrung, HatLagerplatzzuordnung, ChargenPflicht, SeriennummernPflicht, Entnahmeverfahren,
ArtikelLagerbestand, BestandVerfuegbar, Lagerplaetze, **PosLiefertermin, PosLieferwoche, EKBestaetigterTermin**, EKAlterLiefertermin,
EKMahnstufe, EKLetzteMahnung, EKFristsetzung, EKHatLieferavis, **EKBestellnummer(31)** (Lieferanten-Artikelnummer), EKReferenzbezeichnung1/2,
EKProjektnummer, EKGebindefaktor, EKMindestbestellmenge, EKDispositionsHandle, RahmenvertragsHandle, IstBestellartikel, IstAktiv,
HatErledigtenFertigungsauftrag, HatLieferantenzuordnung, HatBedarfsdecker, PosKostenstelle/-traeger, PosMemo, LagerplatzSBC;
USER: KHKEKBelegePositionen_USER_ ABPos, bestaetigt, Zielort, DPSbsMDE*, RMA*, VorPosIDRahmenkontrakt
Listen: **`Lagerplatz`** (strEKLagerPosition: PlatzHandle, RueckmeldungHandle, MengeBasis, Menge, Kurzbezeichnung, Bestand,
LagerbuchungHandle, BestandsHandle, LaufendeNummer, PicklistenPositionsHandle, BestandBuchen, SeriennummernEintraege, ChargenCollection),
`VorgangspositionsReferenzen` (Menge, MengeBasis, ReferenzEbene1-5), `SeriennummernGebucht`, `ChargenGebucht`, `LagerplatzLoad`,
**`Verursacher`** (strEKBelegPositionVerursacher: VerursacherHandle/-SubHandle, VerursacherTyp, ReferenzHandle/-SubHandle, MengeBasis –
Bedarfsverursacher, z. B. FA), `BelegPosEtiketten` (Partner-Etikettendruck)

Weitere EK-Strukturen (nur G): strEKBelegCopy, strEKBelegStaffelrabatt/-Steuer/-Zkd/-Zuschlag, strEKBelegVorgangspositionsReferenz,
strEKChargenEintragLoad, strEKLagerPosition(Load), strEKMahnung, strEKSeriennummernEintrag(Gebucht), strEKVorPosSuche,
strBelegnachweisWE, strSchnellAenderungLiefertermin („Schnelländerung bestätigter Liefertermin“), strLieferantenauskunft, strLieferant,
strLieferantenArtikel (Lieferant*, Matchcode, Wkz), strWochendispo („Dispobestand“: Artikel/Artikelgruppe/Hauptlieferant Von/Bis,
Datum, KeinHauptlieferant, Unterschreitung)

## Sage.Wawi – Lager

- **`strLagerplatzbuchungen.Sage.Wawi`** [GPCD + Execute, „Lagerbuchungen“, 107 Felder]: Bewegungsart, Lagerbewegungsart*, Artikelnummer*,
  AuspraegungsHandle, MengeLager*, MengeBasis, Bewegungsdatum, Eingangsdatum, Zugangskennung, BestandsHandle, BuchungsHandle, Referenz,
  Memo, Einstandspreis, Einzelpreis, MittlererEKPreis, LagerplatzZiel-/HerkunftHandle(+Kurzbezeichnung), Job_* (JobHandle, Standardtext,
  Erfassungsdatum, Benutzer, IstBelegJob, Memo), VK-/EKBelegPositionsHandle, VK-/EKAbbuchMenge/-Wert, PickListenHandle,
  PicklistenPositionsHandle, KommissionierHandle, **FABelegPositionsHandle, RueckmeldungsHandle**, Umbuchungsart, StornobuchungMotherHandle,
  IstSchwebend, Status, MeldungsText, Bewegungsart-Infos (Bestandswirkung, IstManuell, IstIFAuftrag, IstInventur, IstStorno); Listen
  SerienNummern/Chargen/Zugangskennung (+Verbuchte), Abbuchbestaende (BestandsHandle, Abbuchmenge), LagerplatzCollection
  → **Das Datenmodell hinter `LagerbuchungService`**; `FABelegPositionsHandle`/`RueckmeldungsHandle` zeigen, dass Sage FA-bezogene
  Lagerbuchungen kennt (Materialentnahme/Fertigmeldung) – über den Service aber nicht adressierbar.
- `strLagerbewegungAssistent.Sage.Wawi` [GPCD + Execute, 80 Felder]: Dialogstruktur für Serien-/Chargen-/FLifo-Auswahl je Buchung.
- `strLagerjob` (Job, Erfassungsdatum, Benutzer, Standardtext, InfoText, IstBelegJob), strLagerjobliste, strLagerEinzelbestand,
  strArtikelBestandsauskunft, strLagerBestandszettel, strLagerplaetzeAnlegen/Loeschen, strLagerplatzTreeSelect, Inventur-Dialoge
  (strInventurErfassung, -Eroeffnung, -Uebernahme, -Zaehllisten, -Korrekturen, -Bewertung …)
- **`recLagerplaetzestamm.Sage.Wawi`** [GPCD]: **PlatzID***, Lagerkennung(10), Kurzbezeichnung(50), Platzbezeichnung(110), Status*,
  IstGesperrt*, IstGesperrtInventur*, IstDispoLager*, Breite/Hoehe/Laenge/Volumen/Tragkraft, MaxArtikelmengeLME, MitArtikelzuordnung,
  Artikelzuordnung(31)(+AuspraegungID/Matchcode), Lagertechnik, Inventurmethode, LetztesInventur* → Gegenstück zur heutigen
  `LagerplatzSyncService`-Quelle (`KHKLagerplaetze`)
- **`recLagerstamm.Sage.Wawi`** [GPCD]: Lagerkennung*, Bezeichnung*, Aktiv*, IstPlatzverwaltet*, IstDispoLager*, IstFremdlager*,
  IstSperrlager*, IstKommissionslager*, IstWE-/WALager*, IstHauptWE-/WALager*, Ein-/Auslagerungsregel(+Prioritaet), Lagertechnik*,
  WarenflussKZ*, MitArtikelzuordnung*, Dimensionen (Bloecke/Reihen/Regale/Ebenen/Plaetze), Inventur*, PlatzID*, Kurzbezeichnung

## Sage.Wawi/System – Stammdaten für Einkauf

- **`recLieferantenstamm.Sage.System`** [GPCD, 147 Felder + 20 USER]: Kto(20)*, KtoArt, KtoTyp*, Matchcode*, WKz*, Besteuerung*, EULand,
  EUUStID, Zahlungskond, Inkassoart, Skonto*, Kreditlimit, Vertreter, Zahlungs-/Liefermoral, Kostenstelle/-traeger, Tour, Rechnungskreis,
  Preisliste, Rabattliste, Rabattsatz, Versand, Lieferbedingung, Verkehrszweig, Geschaeftsart, Rabattgruppe, ABCKlasse, Teillieferungen*,
  Aktiv, IstGesperrt, Gruppe, Sammelkonto*, Adresse (Name1/2, Anrede, Liefer-/Post-Adresse, EMail, Telefon, Telefax, Mobilfunk, Homepage,
  Sprache*, Memo), ERechnung*, Konsilagerplatz; Listen `Ansprechpartner`, **`ArtikelLieferanten`** (recArtikelLieferanten),
  Bankverbindungen DE/AT/CH, KonsiZusatzLager, DMSCloudDokumenteLieferanten
- **`recArtikelLieferanten.Sage.Wawi`** / `recLieferantenArtikel` / **`recArtikelstammLieferanten`** [GPCD]: Artikelnummer(31)*,
  AuspraegungID*, **Lieferant(20)***, **Bestellnummer(31)** (Lieferanten-Artikelnummer), Bezeichnung1/2, Einkaufsmengeneinheit(+Original),
  DezimalstellenEK, PreiseinheitEK, **Einzelpreis**, Rabattsatz, UmrechnungsfaktorEK/VPEK, **Mindestbestellmenge**, MengenberechnungEK,
  **Wiederbeschaffungszeit**, Matchcode, WKz; Listen `Staffelpreise` (recArtikelstammLieferantenPreise: AbMenge*, Einzelpreis*),
  `Staffelrabatte` → **die Lieferanten-Artikel-Zuordnung** (Tabelle `KHKArtikelLieferanten`), die in `apiLieferanten` fehlt
- `recArtikelstamm.Sage.Wawi` [GPCD], `recArtikelstammStueckliste` (Stueckliste(31)*, Element(31)*, AuspraegungID*, Menge, Sortierung,
  Typ, Entnahmeverfahren, Stuecklistentyp – Wawi-Stückliste, nicht PPS), `recArtikelstammRessourcenkopf` (Artikelnummer*, AuspraegungID*,
  Variante*, IstFremdfertigung*, IstOffen*, Produktionszeit, Status, Von-/BisMenge, Von-/BisDatum, Zeichnungsnummer …)
- `recLieferantengruppen`, `recLieferantenDetails`, `recKundenstamm`, `recAdressenstamm`, `recMitarbeiterstamm`, `recProjektstamm`

## Partner-Pakete mit WMS-Bezug

- **`recDPSBSInterneLagerbestellungen.100036671.AKE_InterneLagerbestellungen`** [GPCD]: ID*, Artikelnummer(50)*, AuspraegungID, Menge*,
  Besteller(50)*, BestellerSageUser, BestellZeit, Bearbeiter, BearbeiterSageUser, BearbeitungsZeit, Bemerkung → **eine bestehende
  Sage-Lösung für interne Lagerbestellungen** (vgl. WMS-Lagerbestellungen/Bedarfsmeldungen); `strInterneLagerbestellungen` (Besteller*)
- `strLOGFA` (BelID), `strLOGFAListe` (Ladedatum/Kunde/Kundengruppe/Artikel/Artikelgruppe Von/Bis), `strUnvollstLieferung` (SysDate)
  – Paket `AKE_InterneFA` (Logistik-FA-Liste, vgl. `dtsAbfLOGFertigungsauftraege` mit USER_Ladetermin/IstVerladen in
  [[sage-sdata-realtimedata]])
- `recDPSBSOseonDataImportConfig` (Aktiv, ConfigID*, ConfigName, FileExtention, LocalVerzeichnis, PlatzID, ServiceTaskID) – die
  **OSEON→Sage-Schnittstelle des Partners** (Lastenheft S3.3)
- `recDPSBSBelegStammdatenSyncSettings` (TABLE_NAME*, COLUMN_NAME*) – Paket `IdealAKE_BelegStammdatenSync`
- `DPSBS_Etikettendruck`: strArtikeletikett(+Chargen/Serienummer), strBelegArtikelEtikett, strDPSBSBelegPosEtiketten, strFaArtikelEtikett,
  strFABelegPosArtikelEtikett, strLagerplatzetikett, strEK-/VKBelegPosition, dvlEtikettenBasic (Etikettendruck für Artikel, Belegpositionen,
  **FA-Positionen**, Lagerplätze – vgl. `PrintEtikettenBasicService`)
- `strTablarAusfahren.100036671.AKE_Erweiterungen` (Anfrage) – **Lagerlift-Anbindung** (Lastenheft S6)

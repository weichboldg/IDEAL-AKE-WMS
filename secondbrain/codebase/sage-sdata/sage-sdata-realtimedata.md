---
type: codebase-karte
updated: 2026-10-30
quelle: "$schema des Vertrags RealTimeData (5,92 MB), AKE-Testserver, Dataset ake_TEST2026;1, ausgelesen 2026-10-30; Lesetests mit Postman/Chrome"
---
# Sage SData – Vertrag `RealTimeData` (Datenquellen, nur lesen)

Übersicht und Aufrufkonventionen: [[sage-sdata-uebersicht]]. 1219 Datenquellen (`dts…`, Tag `DataSources`, nur GET), 1 Operation
(`InvalidateSchema`). Das sind die Abfragen hinter den Auskünften, Listen und Berichten des Sage-Clients.

**Aufruf:** `GET {Basis}/RealTimeData/{Dataset}/<dts-Name>?count=N&startIndex=N&select=F1,F2&where=<Ausdruck>&format=json`
- `select` und `where` funktionieren (Postman-Test 2026-10-30, `dtsFaUebersicht`).
- `format=json` liefert `{"$resources":[{…}]}`; ohne bzw. mit XML-Accept kommt Atom-XML mit `<sdata:payload>`.
- Datum kommt als `2026-02-03T00:00:00`, Sage-Booleans als `-1`/`0`.
- Viele Quellen tragen zusätzlich **alle USER_-Felder** der beteiligten Stammtabellen (z. B. +154 `KHKArtikel_USER_*`) – mit `select`
  eingrenzen.
- **Parametrisierte Quellen** (Struktur-/Positionslisten, die im Client einen Kontext brauchen) liefern über SData **leer** oder 500:
  `dtsFaBelegStruktur` (auch mit `where=StrukturID eq …` und `?StrukturID=…`), `dtsFaPositionen` (`where=BelID eq …`),
  `dtsFaFortschritt` (500). `$queries` ist leer, `$service` hat nur `InvalidateSchema`. Kein dokumentierter Parameterweg.
- Verträge mit „Verfügbarkeit“: `dtsFaNachStatus`, `dtsFaUebersicht`, `dtsFertigmeldungen`, `dtsRueckKorrektur` lieferten 200 mit Daten
  bzw. leerer Liste (Test-Mandant ohne Rückmeldungen).

## Pakete

| Paket | Anzahl | Inhalt |
|---|---|---|
| `Sage.PPS` | 79 | Produktion (vollständige Liste unten) |
| `Sage.Wawi` | 491 | Artikel, Belege, Lager, Einkauf, Verkauf, Inventur, Projekte, Auswertungen (Auszug unten) |
| `Sage.System` | 151 | Adressen, Kontokorrent, Druck, Benutzer (nicht aufgeführt) |
| `Sage.Rewe` / `Sage.xRM` | 315 / 100 | Rechnungswesen / CRM (nicht aufgeführt) |
| Partner `100036671.*` und `050002003.*` | ~90 | s. unten |

## Sage.PPS (79, vollständig)

Stammdaten: dtsArbeitsgaengestamm, dtsArbeitsplaetzestamm, dtsWerkzeugestamm, dtsKapzitaetsstelle, dtsPpsKapGruppen, dtsSchichtplanSuche,
dtsTauschfelder, dtsPPSArtikelSuche, dtsArtikelsucheFremdfertigung, dtsArtikelDispo
Ressourcenlisten/Stücklisten: dtsRessourcenlisten, dtsPpsRessourcelisteStruktur, dtsStkListenAuswahl(+Detail), dtsStkListenFaAktualisierung(+Detail)
[„Fertigungsaufträge aktualisieren/anlegen“], dtsStkListenKonv [„Daten einlesen“], dtsStkLstIProtokoll, dtsStkCopyDef, dtsTeileverwendungsnachweis,
dtsDAktualisierKopf/-AG/-AP/-MV [Datenaktualisierung]
Fertigungsaufträge: **dtsFaUebersicht**, **dtsFaBelegsuche**, dtsFaNachStatus, dtsFaBelegStruktur, dtsFaPositionen, dtsFaAgPositionen,
dtsFaArtikel, dtsFaFertigungsauftrag/-Kopf/-Pos/-AgPos/-SubVK (Druck), dtsFaFremdfertigungsauftragKopf/-Pos, dtsFaFortschritt,
dtsFaKundenSicht, dtsFaMaterialbegleitschein, dtsFaMaterialentnahmeschein, dtsFaEinzellohnschein(+Fuss), dtsFaVerursacherInfo,
dtsVerursacherAnzeige, dtstFaBelege, dtsLetzteFa, dtsMaterialverzug, **dtsOffeneEK_WEFa**, dtsWorkInProgress
Rückmeldung/BDE: **dtsFertigmeldungen**, **dtsRueckKorrektur**, dtsRueckmldgKorrCC, dtsBDELogbuch, dtsBDEFehlbuchungen, dtsBDEStempelKorr,
dtsBDETerminalSuche, dtsTaetigkeitsliste, dtsRuestzeiten, dtsAusschussquote(+Det, PerArt)
Fremdfertigung: dtsFfUebersicht, dtsFfAuftraegeLaufend, dtsFfBedarfe(2), dtsFfBestaende, dtsFfBeistell(+Sub*), dtsLetzteFf, dtsLetzteRess

### Felder der wichtigsten PPS-Quellen

- **dtsFaUebersicht** (216 Felder inkl. USER): BelID, StrukturID, Artikelnummer, Matchcode, Sollmenge, Fertigungmenge, IstMenge, IstEingelastet,
  IstBegonnen, IstBeendet, SollStarttermin, SollEndtermin, … (Test: FA 835 geliefert)
- **dtsFaBelegsuche** (53 + 35 USER): AenderungsIndex, Aktiv, AngelegtAm/Von, ArchiviertAm/Von, Artikelnummer, AusfuehrungsArt, AuspraegungID,
  Belegjahr, Belegnummer, **BelID**, Bestellmenge, Bezeichnung1/2, DimText, Fertigungmenge, Fertigungsart, GeaendertAm/Von, **IstBeendet,
  IstBegonnen, IstEingelastet, IstReaktiviert, IstGesperrt, IstGedruckt**, IstBestellt-/IstEinlastungs-/IstGeliefertTermin, Istendtermin,
  Iststarttermin, **Istmenge**, IstCadFreigegeben, KapBelegungPruefen, Kostenstelle, Kostentraeger, Matchcode, Memo, PKonfigID/-PosID, PlatzID,
  Projekt, Sollendtermin, Sollmenge, Sollstarttermin, Sprache, **StrukturID**, Terminierung, Variante, VerursacherTyp, Zeichnungsnummer,
  Zusatztext, DezimalstellenLager → **kompakte FA-Kopfquelle mit Änderungsstempel (`GeaendertAm`) – geeignet für Delta-Sync**
- **dtsFaNachStatus**: AnzahlNeu, AnzahlEingelastet, AnzahlBegonnen
- **Hierarchie über dtsFaBelegsuche:** `where=StrukturID eq 835` liefert Haupt-FA und alle Sub-FAs der Struktur (Test: 7 Zeilen, 835 + 836–841);
  Haupt-FA erkennbar an `BelID = StrukturID` und `VerursacherTyp = MAN`, Sub-FAs haben `VerursacherTyp = FA`. `Belegnummer` war in allen
  Testzeilen 0 (FA-Belegnummern im Test-Mandanten offenbar nicht vergeben).
- **dtsFertigmeldungen** [Fertiggemeldete Artikel]: Artikelnummer, **BelID**, DatumZeitEnde, Lagerbestand, Lagerkurzbezeichnung,
  Lagermengeneinheit, Matchcode, **MengeGut, RueckmeldungID**, VerursacherID, VerursacherTyp (+154 Artikel-USER)
- **dtsRueckKorrektur** (= Sage-Rückmeldungstabelle): RueckmeldungID, **BelID, BelPosID**, BatchID, **Typ**, TypDisplayed, Buchungstyp,
  **Menge, MengeGut, Schwund**, **DatumZeitStart, DatumZeitEnde**, Pause, IstPausenabzug, **Mitarbeiternummer, Arbeitsplatznummer**,
  Werkzeugnummer, Stillstandnummer, IstRuesten, IstEnde, IstManuell, Lieferant, Projekt, Referenz, Text, Meldung, Status, StatusBatch,
  **LagerBuchID, LagerJobID**, PlatzID, FibuUebergabe, IstEinzelpreis/-Gesamtpreis, IstLohnkosten, IstApkosten Ruesten/Betrieb,
  Lohn-/Arbeitsplatzanteil, Zusatzkosten, RueckmeldVerursacher, UnterRmAuto, UserErstellt/-geaendert, Datumerstellt/-geaendert
- **dtsFaBelegStruktur** (106, parametrisiert): BelegHandle, Position, **BelIDNachfolger**, RessourceTyp, RessourceNummer, Matchcode,
  ArbeitsgangNummer, BelPosID, ID, ParentId, BelIdParent, StrukturID, Status, Soll-/Istmenge, Soll-/Ist Start-/Endtermin, Soll-/IstRuestzeit,
  Soll-/IstStueckzeit, Ist-/Soll-Kosten, **BdeMitarbeiter, BdeTerminalId**, Bedarfstermin, Fremdfertiger, PlatzID, Wkz, Schwund, …
- **dtsFaFortschritt** (parametrisiert): BelID, AgNummer, RessourcenummerAg, **FortschrProz, FortschrAgProz**, PosSoll, PosIst, PosSollstart/-Ende,
  IstBegonnen, IstBeendet, IstEingelastet, IstGedruckt, AktivFA, AktivPOS, IstBeendetFA
- dtsFaPositionen (8): BelID, BelPosID, Position, Ressourcenummer, Ressourcetyp, Matchcode, Dimensionstext, Aktiv
- dtsFaArtikel (25): BelID, Position, Verursacher, Projektnummer, Artikelnummer, Matchcode, Sollmenge, Fertigungmenge, Istmenge, Termine …
- dtsFaVerursacherInfo (9): BelID, Belegnummer, Jahr, Position, KtoArtikel, Menge, Termin, Verursacher, Bezeichnung
- **dtsOffeneEK_WEFa** [Ausstehende EK-Wareneingänge für Fertigung] (29): **FaBelID, FaBelPosID, FaArtikelnummer, FaMatchcode, FaPosRestbedarf,
  Sollstarttermin**, Artikelnummer, Lieferantennummer/-matchcode, Vorgangsnummer, Liefertermin, **LieferverzugTage, TerminOK**, Bestaetigt,
  OffeneEKBestellmenge, Lagerbestand, BedarfHeute/-Zukunft, Meldebestand, WBZ → **Bestellungen je FA-Position mit Verzug**
- **dtsTeileverwendungsnachweis** [Teileverwendungsnachweis] (37) – **der funktionierende Weg zur FA-Stückliste über SData** (Test 2026-10-30):
  KopfKey1-3, **KopfArtikelnummer**, KopfVariante, KopfAuspraegungID, KopfBezeichnung1/2, KopfMatchcode, **Ressourcetyp** (MA | AG | VS),
  **Ressourcenummer, Matchcode, Menge**, MengeNKS, Arbeitsgangnummer, Arbeitsplatznummer, Werkzeugnummer, Mitarbeiternummer, Fremdfertiger,
  Materialteileart, IstEinmalmenge, Umrechnungsfaktor, MindestmengeAG, Ueberlappungmenge, RessourceTypAg, RessourcenummerAg,
  **Herkunft** (1 = Ressourcenliste/Artikelstückliste, 2 = FA-Beleg), **BelID** (nur bei Herkunft 2), IstBeendet, Dimensionstext,
  PosAuspraegungID/-Text.
  - `where=BelID eq 835` → 11 Positionen des FA (10 × MA, 1 × AG) – **FA-Stückliste je BelID ohne Direkt-SQL**
  - `where=KopfArtikelnummer eq '86003871'` → Artikelstückliste (Herkunft 1) **und** alle FA-Positionen dieses Artikels (Herkunft 2)
  - `where=Artikelnummer eq '86003870'` → Verwendungsnachweis: alle Ressourcenlisten, die das Teil enthalten
  - Fehlt gegenüber der View `FAListe`: BelPosID, Vater-Beziehung (nur über Ressourcenummer = Artikelnummer des Sub-FA ableitbar),
    Soll-/Istmenge je Position, Termine, IDEAL-USER-Felder (Kommissionieren, Arbeitsbereich, Arbeitsschritte, Beschichtet …)
  - **Vorsicht:** `count` wird bei dieser Quelle ignoriert – ohne `where` kamen 237.122 Zeilen. Immer mit `where`.
- dtsBDEFehlbuchungen (8): BelId, BuchungsId, Mitarbeiter, Typ, Startdatum, Endedatum, Fehlertext
- dtsBDELogbuch (10): LogID, DatumZeit, Benutzer, Funktion, Fehlercode, Fehlertext, Konvertername/-typ, Mandant
- dtsBDEStempelKorr (4): Anzahl, Ress, Stempeltyp, Sortierung
- dtsWorkInProgress (3): Bezeichnung, Wert
- dtsFfUebersicht (31): BelID, Artikelnummer, Fremdfertiger, Bestellung, Bestellmenge, Bestellpreis, Liefermenge, Retourmenge, Soll-/Istmenge,
  Termine, IstBeendet/-Begonnen/-Eingelastet
- Druckquellen dtsFaFertigungsauftragKopf (61)/Pos (43)/AgPos (13), dtsFaMaterialentnahmeschein (102): ConnID/PrintID-gebunden

## Sage.Wawi – Einkauf, Lieferanten, Disposition (Auszug mit Feldern)

- **dtsEKBelegMst** [Belegauskunft Einkauf] (101 + 15 USER): BelID, Belegkennzeichen, Belegart, Belegjahr, Belegnummer, Belegdatum,
  A0Matchcode/-Name1/…, A1-A4 Adressen, Bearbeiter, Liefertermin, Lieferwoche, Lieferbedingung, Versand, Netto-/Brutto-/Steuerbetrag,
  Rabatt1-3, Referenznummer/-zeichen, Status, StatusSperre, GGErfuellt, Vorgang, VorID, Lieferschein, Lieferantengruppe, Erfassungsart,
  IstSammelbeleg, FibuTAN, Wareneingangscode
- **dtsEKPositionen** [Belegauskunft Einkauf Positionen] (46 + 185 USER): BelID, BelPosID, Position, Artikelnummer, Bezeichnung1/2, Menge,
  Mengeneinheit, **Bestellt, Geliefert, Berechnet, Retoure, Erfuellt**, Einzelpreis, Gesamtpreis, Rabatt, Liefertermin, Lieferwoche,
  Hauptlieferant, LetzterLieferant, Hersteller, Rahmenvertrag, Fertigungsartikel, Projektnummer, Sachkonto, Kostenstelle/-traeger, EANNummer
- **dtsOffeneEK_WE** [Ausstehende EK-Wareneingänge] (21): Artikelnummer, Artikelmatchcode, AuspraegungID, Lieferantennummer/-matchcode,
  Vorgangsnummer, Vorgangsmatchcode, **Liefertermin, Bestaetigt, OffeneEKBestellmenge**, Lagerbestand, Meldebestand, BedarfHeute/-Zukunft,
  Ausreichend, WBZ, Projektnummer
- **dtsOffenewareneingaenge** (261 + 205 USER): vollständige Sicht Kontokorrent + Artikel + Beleg + Vorgang + Position: u. a. Lieferant,
  Lieferantenmatchcode, Belegnummer, Belegdatum, Vorgang, VorPosID, BelID, Positionsnummer, ArtikelnummerPos, **MengePos, Bestellt, Geliefert,
  Berechnet, Retour, MengeOffen, Verfuegbar**, LieferterminPos, **BestaetigterTerminPos**, AlterLieferterminPos, LetzteMahnungPos,
  BestellnummerPos, MindestbestellmengePos, GebindePos, EinzelpreisPos, ErfuelltPosition, ErfuelltVorgang, Rahmenvertrag, Wiederbeschaffungszeit
- **dtsLiefertermineBestaetigt** [Bestätigte Bestellpositionen] (61 + 184 USER): BelID, BelPosID, VorID, Vorgang, Belegnummer, Lieferant,
  MatchcodeLieferant, Artikelnummer, Menge, **Bestellt, Geliefert, Berechnet, Retoure, Erfuellt, VollstaendigGeliefert**, Liefertermin,
  **LieferterminBestaetigt, LieferterminWareneingang, DeltaTerminBestellt, DeltaTerminWE**, Projektnummer
- dtsLiefertermine [Einhaltung der Liefertermine], dtsLiefertreue [Termintreue EK], dtsMahnstufen, dtsEKMahnung, dtsEKAnfrage, dtsEKRetour,
  dtsEKVorgangMst/-ChrDtl/-VorPosDtl [Vorgangsauskunft Einkauf], dtsEKBelegeDtl, dtsEKBelegProjekte, dtsEKSammelbelegAuskunft*,
  dtsOffeneEKBest13b, dtsOffeneRechnungseingaenge, dtsUngebuchteEKRg, dtsLetzteEkBelege/-Vorgaenge, dtsEKUmsBl* (Umsatzüberblick Einkauf),
  dtsEKPivot* (Pivot-Auswertungen), dtsEKVPBelege/-Positionen [Preisanfragen], dtsEKArtikelAuskunft/-Kartei/-Chargen/-Seriennr
- **dtsLieferantenArtikel** [Lieferantenartikel] (23): Lieferant, Artikelnummer, AuspraegungID, **Bestellnummer, Einzelpreis, Rabattsatz,
  Mindestbestellmenge, Wiederbeschaffungszeit**, Einkaufsmengeneinheit, PreiseinheitEK, UmrechnungsfaktorEK/VPEK, Wkz, Bezeichnung1/2, Matchcode
- dtsLieferantArtikel [Lieferantenauskunft Artikel] (36): Artikel-Stammfelder + Hauptlieferant, LetzterLieferant, Menge/Umsatz (+Vorjahr)
- dtsEKArtikelLief [Artikelauskunft Einkauf Lieferanten] (18): Lieferant, Lieferantengruppe, Adresse, Matchcode, Menge, Umsatz, LetzterUmsatz
- dtsLieferantUmsatz/-UmzatzListe/-Kartei/-AGR, dtsTopLiefUmsaetze, dtsAdressenauskunft(+Ansprechpartner)
- **Disposition:** **dtsFehlbestaende** [Fehlende Lagerbestände] (23): Artikelnummer, Lagerbestand, Meldebestand, BedarfHeute/-Zukunft,
  DispobedarfHeute, Dispomenge, BestandOhneEK, EKBelegeLager, VKBelegeLager, ErwarteterZugang, ZugangWBZ, Wiederbeschaffung, ausreichend,
  IstBestellartikel, IstFertigungsartikel; **dtsDispobestandEinkauf** (19): Dispomenge, VorPosId, PosDatum, PosLieferwoche, Lieferavis, BelID,
  Belegkennzeichen, Lieferantenmatchcode; dtsDispobestandProduktion/-Verkauf, dtsDispositionsbestand(+Week), dtsWochendispoliste [Dispobestand]
  (20): Artikelnummer, Hauptlieferant, Kalenderwoche, Lagerbestand, Anfangsbestand, Bestellmenge, Fertigung, Auftragsmenge, Endbestand,
  Meldebestand, MeldebestandUnterschreitung; dtsBedarfHerkunft [Verursacherinfo] (8): Verursacher, Herkunft, MengeBedarf, TerminBedarf,
  MengeGeplant, DatumGeplant, Plannummer, Wbz; dtsBearbeitDispoStpl [Zu bearbeitende Dispostapel] (4): Stapelkennung, Stapeltyp,
  Bedarfstermin, Mode (→ `DispositionStapel*Service`)

## Sage.Wawi – Lager (Auszug mit Feldern)

- **dtsArtikelLagerbestaende** [Bestandsüberblick] (81 + 154 USER): Artikelnummer, AuspraegungID, **Bestand, Reserviert, Gesperrt,
  Gesamt_inklSperrlager, Bestand_auf_nicht_Dispolager, VerfuegbarerBestand, ZuPicken, Gepickt**, Hauptlagerplatz, Hauptlieferant,
  Meldebestand, Bestellmenge, LetzterEK, MittlererEK, KalkulatorischerEK, Wiederbeschaffungszeit, Lagermengeneinheit, PlatzID, Lagerfuehrung
- **dtsLagerplatzbestaende** [Lagerplatzbestände] (161 + 154 USER): Artikelnummer, AuspraegungID, **Bestand, Lagerkennung, PlatzID,
  Kurzbezeichnung, Platzbezeichnung, Verfuegbarkeit, LetzteBewegung**, IstGesperrt, IstGesperrtInventur, Status, BestandsIDAggregatMinimum + Artikelstamm
  → Gegenstück zur heutigen `LagerbestandSyncService`-Quelle
- dtsLagerplatzbestaendeChargen/-SerienNr(+Entnahmeverfahren), dtsBestandsauskunftLagerplaetze, dtsArtikelLagerbestaendeLagerorte/-plaetze/-EKBelege/-VKBelege,
  dtsArtikelLagerplatzbestaendePickliste/-Reserviert, dtsLagerorteBestand, dtsLagerwert, dtsChargenbestand(+Artikel), dtsChargenEK/-Lager/-Prod/-VK,
  dtsSeriennrLager/-Prod, dtsSeriennummerEK/-VK
- **dtsLagerplatzbuchungen** [Lagerplatzbuchungen] (35 + 154 USER): BuchungsHandle, Artikelnummer, AuspraegungID, BestandsID, Bewegungsart(+Bezeichnung),
  Bestandswirkung, Bewegungsdatum, MengeLager, MengeBasis, Einstandspreis, MEK, HerkunftsLPKennung/-Kurzbezeichnung, ZielLPKennung/-Kurzbezeichnung,
  Job, **Referenz, Memo**, Status, Meldung, Stornobuchung, Chargen, Seriennummern, Intern → **Gegenstück zum heutigen Idempotenz-Lookup auf
  `KHKLagerplatzbuchungen`** (`SageBuchungLookupReader`), ohne Direkt-SQL
- dtsLagerplatzbuchungenJobs, dtsLagerjobliste, **dtsLagerRueckmeldungen** [Rückmeldungen] (21): RueckmeldungID, **RueckmeldungTyp**, BelID, BelPosID,
  BatchID, Artikelnummer, PlatzID, LPBez, **LagerbuchungID, LagerJobID**, Menge, MengeLPGebucht, DatumZeitStart, Lagerbestand, Entnahmeverfahren
  → **Verknüpfung FA-Rückmeldung ↔ Lagerbuchung**
- dtsLagerSchwebendeBuchungen (17): IDSchwebendeBuchung, BuchungsID, Artikelnummer, Herkunfts-/Ziellagerplatz, Belegjahr/-nummer, Position, Menge, Referenz, BelID
- **dtsLagerplaetzestamm** [Lagerplätze] (29): PlatzID, Lagerkennung, Kurzbezeichnung, Platzbezeichnung, Status, Aktiv, IstGesperrt(+Inventur),
  IstDispoLager, IstFremdlager, IstSperrlager, IstPlatzverwaltet, Lagertechnik, Artikelzuordnung(+Matchcode/AuspraegungID), MaxArtikelmengeLME,
  Lagermengeneinheit, Dimensionen, Inventurdaten → Gegenstück zur `LagerplatzSyncService`-Quelle
- dtsLagerstamm, dtsLagerkennung, dtsLagerplatzsuche, dtsLagerAuftrag(+Chargen/SerienNr), dtsLagerBestandszettel(+…), dtsLagerAssistentSeriennummern,
  dtsLagerbewegungAssistentChargen, Inventur: dtsInventurZaehllisten(200), dtsIVBelegePositionen, dtsInventurBewertung, dtsInventurKorrekturen,
  dtsInventurHistoriekorrekturen, dtsInventurUebernahmeProtokoll, dtsFehlendeInventurBestaende, dtsInventurLadenhueterliste

## Sage.Wawi – Artikel, Verkauf, Sonstiges (Namen)

Artikel: dtsArtikelstamm [Artikelauskunft], dtsArtikelstammKunden/-Lieferanten, dtsArtikelgruppen(+Tree*), dtsKategorien, dtsMengeneinheiten,
dtsVerpackungen, dtsZubehoerGruppen, dtsRessourcenstamm/-gruppen, dtsPpsFormeln, dtsPpsGemeinkostensaetze, dtsPreisliste(nstamm*), dtsRabatt*,
dtsStaffelrabatte, dtsSchwellenpreisListe, dtsElektrogesetz*, dtsVV* (VerpackV), dtsIntra* (Intrastat), dtsERechnung*
Verkauf: dtsVKBelegPos/-Vorgang/-Projekte, dtsVKPositionen, dtsVKVorgangMst/-ChrDtl/-VorPosDtl, dtsOffeneVKAngebote/-Auftraege/-LiefVorPos,
**dtsOffeneVKLiefFa [Offene VK-Lieferungen (Fertigungsartikel)]**, dtsOffeneKommission [Offene Kommissionierung], dtsLieferbareAuftraege(+Positionen),
dtsLieferscheineProtokoll*, dtsVKLieferschein, dtsVKRechnung*, dtsVKColli, dtsVKRueckLief, dtsBestelltGeliefert, dtsGeliefertBerechne,
dtsKunden*, dtsVertreter*, dtsVKUmsBl*, dtsVKPivot*, dtsVPBelege/-Positionen (Angebote), dtsVPPivot*, dtsTop*, dtsNeukunden*, dtsPerioden*
Projekte: dtsProjektstamm/-gruppen/-status/-schritte, dtsProjektProjekt*, dtsMeilensteine, dtsTop10Projekte
Sonstiges: dtsMitarbeiterstamm/-gruppen, dtsLohnartenstamm, dtsTouren, dtsVersand, dtsTextbausteine, dtsStatistiken, dtsDruckprozessVarianten,
dtsStapeldruck*, dtsBelegsuche*/Belegarten* (Stapeldruck), dtsWebshop*, dtsWSProtokolle*, dtsVertraege*, dtsWdhVertraege*, dtsRechnungskreise,
dtsVerkaufNummernkreise, dtsWaren*code, dtsSchnellaenderungFeldauswahl, dtsNakaStatus, dtsFibuUebergabe, dtsLetzte* („Meine letzten …“)

## Partner-Pakete

| Paket | Datenquellen |
|---|---|
| `100036671.AKE_InterneFA` | **dtsAbfLOGFertigungsauftraege** (66: BelID, Artikelnummer, Bestell-/Liefertermin, Lieferwoche, DispoStatus, USER_FAStatus, USER_FAFixtermin, USER_FAMemo*, USER_Ladetermin, **USER_IstVerladen, USER_MengeVerladen, USER_Verladezeit, USER_VerladeMA**, Kunde/Adressen, Versand, Beleg), dtsAbfLOGFertigungsauftragDetails, dtsUnvollstLieferung |
| `100036671.AKE_InterneLagerbestellungen` | dtsDPSBSInterneLagerbestellungen, -Bearbeiten (vgl. `recDPSBSInterneLagerbestellungen` in [[sage-sdata-masterdata]]) |
| `100036671.AKE_DispoMeldebestandBestellung` / `AKE_CCElemente` | **dtsDispobestandMeldebestand** (17: Artikelnummer, Matchcode, Lagerbestand, DispoMenge, Meldebestand, Lieferant, Kto, Einzelpreis, Wiederbeschaffungszeit, Mindestbestellmenge, MengeBestelltVJ), -Vertrieb; dtsOffeneVKAngeboteDPS/-AuftraegeDPS/-LiefDPS, dtsZuErstellVkRech, Lieferliste, LieferlisteIFA |
| `100036671.AKE_IFA_Auskuenfte` | dtsDPSLagerbewegungsprotokoll, dtsDPSVerladungsprotokoll |
| `100036671.AKE_LagerbestandIntern` / `AKE_Artikelstamm` / `AKE_Erweiterungen` | dtsArtikelstamm [Artikelauskunft], dtsArtikelstammKunden/-Lieferanten, dtsDPSGarantieabloese |
| `100036671.AKE_OseonDataImport` | dtsDPSBSOseonDataImportAudit, -Config |
| `100036671.AKE_RMA`, `AKE_Energielabel`, `AKE_KundenDetailsUmsatz`, `AKE_ArtikelKontrolllisteVK`, `AKE_RessourcenlisteGlaeser` | dtsRMAEK, dtsArtikelOeDR, Energielabel, dtsKundenDetailsUmsatz, dtsArtikelKontrolllisteVK, dtsvwDPSBSRessourcenlisteGlaeser |
| `100036671.DPSBS_DMS_Connector` | dtsEinkaufsbelege, dtsOffeneVorgaengeEK, dtsLieferanten, dtsKostenstellen, dtsKostentraeger, dtsSachkonten, dtsSteuerschluessel, dtsOpAuskunftHaupt, dtsDPSBSDMSConnectorBuchungen(+Positionen), dtsExterneDokumentenablage(+Beschreibung) |
| `100036671.DPSBS_Etikettendruck` | dtsArtikeletikett(+Chargen/Seriennummern/EK-/VKBelegePositionen), dtsArtikelEtikettFertigartikel, **dtsEtikettFABelegePositionen**, dtsLagerplaetze, dtsNVEEK-/VKBelegePositionen, dtsDPSBSEtikettenSprache |
| `100036671.DPSBS_KomfortpaketWawi` | dtsBeleghistorie, dtsEinkaufshistorie, dtsVerkaufshistorie, dtsLagerbewegungsprotokoll, dtsLagerbewegungenChargen/-SerienNr, dtsLagerbewegungsarten, dtsLagerorte, dtsLadenhueter, dtsInventurZaehllisten, dtsIVDifferenzListeMitArchiv, dtsDetailsVK(Land), dtsUmsatzdatenVK(Land), dtsWoerterbuch |
| `100036671.DPSBS_Inventurliste`, `DPSBS_Konsi_EK_VK`, `DPSBS_MEKKorrektur`, `DPSbs_TF_Etiketten`, `IdealAKE_BelegStammdatenSync` | dtsInventurliste, dtsBelegarten, dtsArtikelMitBestand, dtsKundenBelegetiketten, dtsDPSBSBelegStammdatenSyncSettings |
| `050002003.AKE_Belege` / `IDEALAKE_DMSConnector` | dtsVKSub* (Beleg-Druckteile), dtsDPSBSDMSConnectorBuchungen |

---
type: codebase-karte
updated: 2026-11-06
quelle: "$schema CommonWawiServicesResources am AKE-Test (sagetest01, ake_TEST2026;1), Ressourcen Artikel (260 Einzelfelder), ArtikelVariantenExtended, ArtikelInfo – 2026-11-06, nur Schema gelesen"
---
# Artikelstamm über SData – alle Felder

Bezug: [[sage-sdata-commonwawiservicesresources]] · Spec [[2026-11-06-sage-sdata-stammdaten-sync-spec]] (F5, G3).
Bedeutung der Felder ist **nicht** aus dem Namen abgeleitet; die Spalte „WMS-Relevanz“ ist ein Vorschlag (Koordination), keine Prüfung.
**★** = in der Spec F5 bereits vorgesehen.

## Ressource `Artikel` – 106 Standardfelder

| Gruppe | Felder | WMS-Relevanz (Vorschlag) |
|---|---|---|
| Identität | Artikelnummer★, Mandant, Variante, Matchcode★, Bezeichnung1★, Bezeichnung2★, Langtext★ (+HTML/RTF), Dimensionstext (+HTML/RTF), Memo, Zeichnungsnummer, Ersatzartikelnummer, Artikelart, Herkunft, Aktiv | hoch: Zeichnungsnummer, Aktiv, Ersatzartikelnummer |
| Gruppen | Artikelgruppe★, Hauptartikelgruppe★, Vaterartikelgruppe★, Rabattgruppe | hoch |
| Hersteller | Hersteller★, HArtikelnummer★ | mittel |
| Mengeneinheiten | Basismengeneinheit, DezimalstellenBasis, Lagermengeneinheit★, DezimalstellenLager★, UmrechnungsFaktorLME, Verkaufsmengeneinheit, DezimalstellenVK, UmrechnungsfaktorVK, MengenberechnungVK, VerpackungseinheitVK, UmrechnungsfaktorVPVK, Gewichtseinheit, IntrastatMengeneinheit, UmrechnungsfaktorIntraME, DezimalstellenIntraME | hoch: Basis-/Lager-ME, Dezimalstellen, Umrechnung |
| Lager | Lagerfuehrung, Lagerplatzzuordnung, PlatzID (Hauptlagerplatz), Entnahmeverfahren, Inventurmethode, Chargenpflicht★, Nachweispflicht★ (Seriennummer) | **hoch**: Lagerfuehrung, PlatzID, Entnahmeverfahren |
| Maße | Laenge, Breite, Hoehe, Eigenmasse | mittel (Lagerplatzwahl) |
| Kennzeichen | IstBestellartikel, IstVerkaufsartikel, IstEinmalartikel, IstFavorit, IstVorlage, IstKundenkartei, IstLieferantenkartei, IstRabattfaehig, IstSkontierfaehig, IstProvisionierbar, IstErsatzteil, IstVerschleissteil, IstFremdleistung | hoch: IstBestellartikel (heute „Beschaffungsartikel“ aus der Stücklisten-View), IstErsatzteil |
| Fertigung | IstFertigungsartikel, IstUnterbaugruppe, Stuecklistentyp, RessourcenRef, MinimaleLosgroesse, MaximaleLosgroesse, KalkulationsLosgroesse, Schwundart, Schwund, GemeinkostenID, PKonfigIstKonfigArtikel, PKonfigAnwendung | **hoch für FA/BDE**: IstFertigungsartikel, IstUnterbaugruppe, Schwund |
| Preis/Einkauf | PreiseinheitVK, PreispflegeEK, DezimalstellenPreis, Grundpreiseinheit, UmrechnungsfaktorGP, GrundpreisBasis, Gemeinkostenzuschlag, Bezugskostenzuschlag, Gewinnzuschlag, Bewertungssatz | niedrig |
| Buchhaltung | Steuerklasse, Besteuerungsart, Erloescode, Wareneingangscode, Wareneinsatzcode, Warenbestandcode, SachkontoVK/EK/WZ/WB, Kostenstelle, Kostentraeger, Verbrauchsteuer | niedrig |
| Außenhandel | Warennummer, Ursprungsregion, Ursprungsland, Bestimmungsregion, StatWertEingang, StatWertVersendung, StatVerfahren | niedrig |
| Technik | Timestamp | Änderungsmarker (Filter scheitert, siehe Katalog) |

## Ressource `Artikel` – 154 Benutzerfelder (`USER_`)

| Gruppe | Felder |
|---|---|
| **Fertigung/Konstruktion** | Maschine, Ausfuehrung, DruckFertigungsauftrag, DruckRL, AUSCAD, EPDMVERSION, VaultID, MasterartikelNr, TypenschildArtikel, Modell, Produktfamilie, KuehlmoebelFamilie, Artikeltyp, IstIDEAL, IstZubehoer, IstKatalogartikel, ISTEKVorlage, EKArtNrPraefix, NummernkreisVorlageartikel, PraefixNummernkreis, UpdateArtikel, AnlageUser, AnlageDate, DPSBestand, tmpBreite, tmpLaenge |
| Kältetechnik | Kaeltemittel, Kaeltemittel2, Kaeltemittel3, Fuellmenge, Fuellmenge2, Fuellmenge3, Kaelteleistung, Kaelteleistung2, Kaelteleistung3, Betriebsdruck, Verdampfer, VerdampferEingangFuehler, VerdampferAusgangFuehler, Ventil, Fuehler, RueckluftFuehler, ZuluftFuehler, AbtauFuehler, GWP, CO2e, Klimaklasse, Temperaturbereich, Raumtemperatur, Stuetzwaerme, Waermebruecke, VT2, VT3 |
| Elektrik | Spannung, Frequenz, Anschlusswert, LeistungsaufnahmeK, Schutzklasse, Netzstecker, Steuerung, Softwareversion, Luefter, AnzahlLuefter, Daempfer, Zusatzkomponenten, Rahmenheizung1, Rahmenheizung2, Primaerheizplatte, HeizplatteZWB, HeizungBefeuchtung, ZusatzheizungLeistung, Geraeuschpegel |
| Elektrik „…W“ (zweite Ausprägung) | SpannungW, AnschlusswertW, NetzsteckerW, LeistungsaufnahmeW, BetriebsdruckW, FrequenzW, NennstromW, SchutzklasseW, LuefterW, AnzahlLuefterW, FuehlerW, DaempferW, ZusatzkomponentenW, LichtTrafoW, LichtW, AnzahlW, SteuerungW, AnzahlLichtW |
| Beleuchtung | Beleuchtung1Typ, Beleuchtung1Farbe, Beleuchtung1Anzahl, Beleuchtung2Typ, Beleuchtung2Farbe, Beleuchtung2Anzahl, BeleuchtungTrafo |
| Maße | Breitemm, Hoehemm, Tiefemm, EinbauoeffnungBreite/-Tiefe/-Hoehe, InnenwanneBreite/-Tiefe/-Hoehe, AusstellflaecheBreite/-Tiefe/-Hoehe, NettoInhaltLiter, AnzahlEntnahmeklappen |
| Dokumente | PfadBenutzerhandbuch, PfadProgrammieranleitung, PfadSchaltplan, PfadGlaszeichnung, PfadFoto |
| Energielabel | ELEnergielabelJN, ELEnergieEffKlasse, IdealEnergieEffKl, ELEEI, EL24hEnergVerb, ELjaehrEnergVerb, ELFaktorC/M/N/P, ELKlimaklasse, ELTempKlasse, ELLichtEnergEffKlasse, ELArtKuehlmoebel, ELArtLichtquelle, ELBeschreibung, ELVerwendung, ELverwNormen, ELWarpraeflae, ELSAE, ELHerkunftEVerbrauch, ELModellkennung, EPRELID |
| Verkauf/Webshop | Bez1VK, Bez2VK, KurztextD, KurztextE, KurztextFR, KurztextNL, Webshop, WebshopAnzeige, WebshopID1, shopVideoD/E/FR/NL, eClass, PraeferenzBeguenstigung |

## Ressource `ArtikelVariantenExtended` – zusätzliche Felder (Bestand/Dispo)

AuspraegungID, Auspraegung1/2, **EANNummer**, **ABCKlasse**, **Lagerbestand**, LagerbestandOhneDispo, **LagerbestandVerfuegbar**, Gewicht, GewichtLME,
LetzterEK, **MittlererEK**, KalkulatorischerEK, LetzterFEK, HatZubehoer, **Bestellmenge**, **Bestellzeitpunkt**, **Meldebestand**, Dispoformel,
Dispofaktor, FormelID, **Hauptlieferant**, LetzterLieferant, **Wiederbeschaffungszeit**, LagermodulEKBME, LagermodulVKBME, EAR* (Elektro-Altgeräte:
Meldepflicht, RegNr, Marke, Kategorie, Geraeteart, Produktgruppe, Gewicht), WS* (Webshop, 18 Felder), Benutzerfelder USER_GrundpreisBasis,
USER_Grundpreiseinheit, USER_GrundpreisFaktor, **USER_LagerplatzID**, **USER_Mindestbestand**, USER_Produktnummer, USER_WebshopAktiv, USER_MEKNeu.

## Ressource `ArtikelInfo` – zusätzliche Anzeige-Felder

Lagerkennung, Platzbezeichnung (Hauptlagerplatz als Text), ArtikelgruppeBezeichnung, HauptartikelgruppeBezeichnung, VaterartikelgruppeBezeichnung,
UserProperty1–8 (Platzhalter, am Test leer).

## Vorschlag für die Spec (G3) – über das bisher Vorgesehene hinaus

1. **Lager:** Lagerfuehrung, PlatzID (Hauptlagerplatz direkt als ID), Entnahmeverfahren, Inventurmethode, USER_LagerplatzID, USER_Mindestbestand.
2. **Fertigung/BDE:** IstFertigungsartikel, IstUnterbaugruppe, Stuecklistentyp, Schwundart, Schwund, Min-/Max-Losgröße, Zeichnungsnummer.
3. **Kennzeichen:** IstBestellartikel, IstErsatzteil, IstVerschleissteil, Aktiv, Ersatzartikelnummer.
4. **Dispo:** ABCKlasse, LetzterLieferant, LagerbestandOhneDispo.
5. **Maße/Gewicht:** Laenge, Breite, Hoehe, Gewicht, Gewichtseinheit.
6. **USER (Merkmale):** Maschine, Ausfuehrung, VaultID, MasterartikelNr, TypenschildArtikel, Artikeltyp, Produktfamilie, IstIDEAL,
   DruckFertigungsauftrag, AUSCAD, DPSBestand, Kaeltemittel, Ventil, PfadGlaszeichnung, PfadSchaltplan.

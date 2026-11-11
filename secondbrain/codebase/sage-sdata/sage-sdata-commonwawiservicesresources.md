---
type: codebase-karte
updated: 2026-11-06
quelle: "AKE-Testserver sagetest01, Vertrag CommonWawiServicesResources (165 Ressourcen) und CommonWawiServicesResourcesSpecific (14), $schema + Lesetests nur GET, 2026-11-06 nach DPS-Installation"
---
# Sage SData – CommonWawiServicesResources (DPS-Lesevertrag)

Geschwister: [[sage-sdata-uebersicht]] · [[sage-sdata-commonwawiservices]] · [[sdata-2-0-spezifikation]]. Befund: [[sdata-befund-ake-test-2026-10-30]] §10–§12.

**Einordnung:** Der Vertrag gehört zu den DPS-Paketen (`CommonWawi…`, wie `LagerbuchungService`) – im Unterschied zu `RealTimeData`/`MasterData`,
die laut DPS (04.11.) **nicht freigegeben** sind. Am 30.10. lieferte `$schema` noch HTTP 500; seit der DPS-Installation (06.11.) 200 mit
**165 Ressourcen** (Schema 0,9 MB). Ob DPS diesen Vertrag ausdrücklich für uns freigibt und lizenziert, ist **offen** (Frage an DPS).

## Aufrufregeln (am Server belegt)

- `GET …/CommonWawiServicesResources/{Dataset}/{Ressource}?format=json&…`, Header `Accept: application/json`.
- **`orderBy` ist Pflicht**, sobald `count` gesetzt ist – sonst HTTP 500, weil der Server `ROW_NUMBER() OVER ()` ohne `ORDER BY` erzeugt.
- `count`/`startIndex` werden beachtet; Antwort mit `$totalResults`, `$itemsPerPage`.
- `where` (auch `in (…)`, `like '…%'`, `and`, Datum `@…@`), `select`, `include` funktionieren. `sme:canFilter` bei fast allen Feldern.
- Fehler: HTTP 500 mit `$diagnoses[].message`, die **das erzeugte SQL** zeigt (Tabellen-/Spaltennamen lesbar).
- Keine ETags, keine UUIDs, keine `protocolFilters`.

## Fertigungsaufträge – alles, was der WMS-Sync braucht

| Ressource | Inhalt (Auszug) |
|---|---|
| `PpsFaBelegeInfo` (67 Felder) | BelID, **StrukturID**, Belegjahr/-nummer, Artikel, Soll-/Fertigungs-/Istmenge, Termine, **VerursacherTyp** (10 = MAN, 70 = FA – an Struktur 835 belegt), Verursacher, **IstEingelastet, IstBegonnen, IstBeendet, IstGesperrt, IstReaktiviert, Aktiv, Fertigungsart**, PlatzID + Lagerplatz, Projekt, Kostenstelle, **GeaendertAm**, Timestamp, `BelegProperty1–4`, USER_DPSBSMDEFreigabe/BereitstellungBeendet/MDELagerInaktivBei; Kinder `PpsFaBelegePositionenInfo`, `…ArbeitsgangInfo` |
| `PpsFaBelegePositionenInfo` (120) | **BelPosID**, BelID, Position, **BelIDNachfolger** (= Unter-FA), RessourceTyp (MA/AG …), RessourceNummer, Menge/Soll/Ist, Rüst-/Stückzeit Soll/Ist, Status, **PlatzID + Lagerplatz**, Schwund, Bedarfstermin, AP-Ressource, BdeMitarbeiter, BstHL-Lagerplatz, `PositionProperty1–4`, DPSBS-USER-Felder |
| `PpsFaBelegePositionenArbeitsgangInfo` (73) | BelAGPosID, BelID, BelPosID, Arbeitsplatz-Ressource, Soll-/Ist-Kosten und -Termine |
| `PpsFaBelegeSmallInfo`, `…GroupInfo`, `…SearchByPosOrAGPosInfo`, `…VkBelegSucheInfo`, `…Bereitstellung*` | Varianten/Suchen |
| `PpsMitarbeiterInfo` | Nummer, Matchcode, Gruppe, Aktiv, Kartennummer, Kostenstelle, UserName |
| `PpsArbeitsplaetzeInfo` | Arbeitsplatznummer, Bezeichnung, Kosten, Aktiv, BdeTerminalId |
| `PpsArbeitsgaengeInfo`, `PpsStillstandsbegruendungenInfo`, `PpsMitarbeiterKommtGeht` | Stammdaten/Zeiten |
| `PpsRueckmeldung*Info`, `PpsFaBelegPositionRessourceTypenInfo`, `PpsFertigungsauftragServiceOptionInfo` | Enum-Listen zu den Services |

**Lesetest Struktur 835 (AKE-Test):** `PpsFaBelegeInfo where StrukturID eq 835` → 7 FAs (835 MAN, 836–841 FA, alle eingelastet, Belegjahr 2027,
Belegnummer 0). `PpsFaBelegePositionenInfo where BelID ge 835 and BelID le 841` → 17 Positionen: 835 hat MA-Positionen 1–6 mit
**BelIDNachfolger 836–841**, MA-Positionen ohne Nachfolger, **AG 999 (BelPosID 10656)**; 836 hat MA 00722001 0,6 (BelPosID 10641, Platz 11 „HR 0 0 0“).
→ **Hierarchie-Kanten, Positions-IDs und Lagerplätze sind über SData lesbar** – das, was RealTimeData nicht lieferte.

**Kein** Lesezugriff auf gebuchte **Rückmeldungen** (keine Ressource `PpsRueckmeldungen…`). Indirekter Nachweis nur über `Istmenge`/Ist-Zeiten
der FA-Positionen.

## Artikel, Stückliste, Lager

| Ressource | Inhalt |
|---|---|
| `Artikel` (264 Felder) | Artikelstamm **mit allen 154 USER_-Feldern** (z. B. USER_Maschine, USER_VaultID, USER_IstIDEAL, USER_Kaeltemittel) direkt als Spalten; Kinder ArtikelVarianten, ArtikelStueckliste, ArtikelSammelmappe; Timestamp |
| `ArtikelInfo` (123) | Artikel + Lagerkennung/Platzbezeichnung, Gruppenbezeichnungen, `UserProperty1–8` (konfigurierbare Platzhalter, am Test leer) |
| `ArtikelVarianten`, `ArtikelVariantenExtended` | Bestand, verfügbar, EK-Preise, **Meldebestand**, Bestellmenge, Hauptlieferant, WBZ, USER_LagerplatzID/Mindestbestand/MEKNeu … |
| `ArtikelStueckliste` | Wawi-Stückliste (Element, Menge, Sortierung) – für 86003871 leer (PPS-Artikel) |
| `PpsRessourcenPositionenInfo` (65), `…FilteredInfo` (76, mit Artikelgruppen + `UserProperty1–8`) | PPS-Ressourcenliste je Artikel, eine Ebene: Position, Ressourcetyp, Ressourcenummer, Arbeitsgang, Menge, Zeiten, PlatzID, BelPosID |
| `LagerplaetzeInfo` | PlatzID, Lagerkennung, Kurz-/Platzbezeichnung, gesperrt, Maße |
| `ArtikelLagerbestaendeInfo` (+ Chargen/Seriennummern), `LagerbestaendeArtikel*` | Bestand je Platz |
| `LagerplatzbuchungenInfo` | BuchungsID, Artikel, Bewegungsart, Mengen, **Memo**, Referenz, Bewegungsdatum, Status, Meldung |
| `LagerorteInfo`, `LagerbewegungsartenInfo`, `Inventur*` | Stammdaten Lager/Inventur |

**Lagerbuchung-Korrelation belegt:** `LagerplatzbuchungenInfo where Memo like 'IdealAkeWms%'` → 10 Buchungen, Memo
`IdealAkeWms Lagerbuchung SM#19376#` usw. Damit ist WMS-Annahme **W4** (Korrelation über Memo) am AKE-Test bestätigt, und der
`SageBuchungLookupReader` könnte ohne Direkt-SQL arbeiten.

## CommonWawiServicesResourcesSpecific – Ressourcenliste mehrstufig + Benutzerfelder

- `PpsRessourcenListenInfo` (Kopf, `Mandant*` im `where` nötig) mit `include=PpsRessourcenPositionenResolvedInfo,SpecificUserfields`:
  - **`PpsRessourcenPositionenResolvedInfo`** = **aufgelöste mehrstufige Stückliste**: `Ebene`, `RowGuid`/`RowGuidParent`, `BelPosID`/`BelPosIdParent`,
    `ArtikelnummerUBG` (Unterbaugruppe), alle Positionsfelder. Test 86003871: 11 Positionen Ebene 1 + 6 Positionen Ebene 2 (je Unterbaugruppe).
  - **`SpecificUserfields`** = Benutzerfelder als Liste `{DatafieldName, DatafieldValue, DatafieldType, DatafieldLength}` – am **Kopf**
    (`USER_VKZuschlag`) **und je Position** (`USER_VKPreis`, `USER_VKEinzelpreis`, `USER_EPDMVERSION`, `USER_AUSCAD`, `USER_Positionsnummer`).
- `CustomFields` {Name, Value}, `SpecificUserfields` als eigene Ressourcen; Picklisten, Sammelmappe, Lizenz.

## Benutzerdefinierte Felder – was geht

| Objekt | Lesbar über | Stand |
|---|---|---|
| Artikel | `Artikel` (alle 154 USER_ als Spalten) | belegt |
| Artikelvariante | `ArtikelVariantenExtended` (8 USER_) | Schema |
| Ressourcenliste Kopf + Positionen (PPS-Stückliste) | `PpsRessourcenListenInfo` + `SpecificUserfields` | belegt |
| FA-Kopf/-Positionen | nur DPSBS-Felder + `BelegProperty1–4` / `PositionProperty1–4` | **AKE-eigene FA-USER-Felder** (z. B. USER_Maschine, USER_Auszug … am FA) **nicht** lesbar; Property-Platzhalter vermutlich DPS-konfigurierbar (ungeprüft) |
| Kunden/Lieferanten | `Adressen`, `Kontokorrent` bzw. `apiKunden/apiLieferanten …udf` | Schema |
| EK-/VK-Belege | `EinkaufBelege*`, `Verkauf*` bzw. `apiBelegerfassung …udf` | Schema |

Schreiben: `FertigungsauftragService` nimmt `FaBelegUserFields[]` und `GenericUserfields[]` an ([[sage-sdata-commonwawiservices]]).

## Delta-Abgleich

- Fast alle Ressourcen haben `Timestamp` (SQL-rowversion, base64). **Filtern darauf scheitert** (`where Timestamp gt '…'` → 500 „implizite
  Konvertierung varchar → timestamp“). Sortieren geht.
- `GeaendertAm` gibt es am **FA** (filterbar), **nicht** am Artikel (`Artikel where GeaendertAm` → 500 „Ungültiger Spaltenname“).
- → Delta heute nur für FAs; Artikel/Lager/Ressourcenlisten als Vollabzug bzw. paginiert. Frage an DPS: Timestamp-Filter (z. B. als Zahl) ermöglichen.

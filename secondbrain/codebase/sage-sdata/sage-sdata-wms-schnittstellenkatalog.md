---
type: codebase-karte
updated: 2026-11-06
quelle: "AKE-Testserver sagetest01, Dataset ake_TEST2026;1 – Probeabruf ALLER Ressourcen von CommonWawiServicesResources (165) + …Specific (14) mit count=1 (nur GET), Schema der 36 CommonWawiServices-Operationen, Stichproben je WMS-Bereich. Auftrag Gerald 2026-11-06: Schnittstelle für das komplette WMS ausrollen."
---
# SData-Schnittstellenkatalog für das WMS (lesen + zurückschreiben)

Grundlagen: [[sage-sdata-uebersicht]] · Lesevertrag [[sage-sdata-commonwawiservicesresources]] · Services [[sage-sdata-commonwawiservices]] ·
Spezifikation [[sdata-2-0-spezifikation]] · Befund [[sdata-befund-ake-test-2026-10-30]] §10–§13 · DPS [[2026-11-03-dps-anfrage-produktion-sdata]].

**Rahmen:** Lesen über `CommonWawiServicesResources` (+ `…Specific`), Schreiben über `CommonWawiServices` (`$service/…`). `RealTimeData` und
`MasterData` sind laut DPS nicht freigegeben und hier nicht berücksichtigt. **Freigabe/Lizenz des Lesevertrags durch DPS ist noch offen.**
Alle Mengenangaben unten = `$totalResults` am AKE-Test 2026-11-06. **Schreibtests 2026-11-06:** Befund §14 – Umbuchung, Bestellvorschlag, FA-Feld erfolgreich und zurückgesetzt; BDE scheitert an fehlendem PPS-Mitarbeiter.

## Probeabruf aller Ressourcen (count=1, `orderBy` = erstes Feld)

- **166 von 179 mit HTTP 200.** Ausfälle:
  - `VerkaufPositionenAllInfo` (> 3 min, abgebrochen) und `EinkaufPositionenAllInfo` (> 20 s Timeout) – `Count(*)` über alle Positionen; **nie ohne `where`**.
  - `LagerbewegungsartenInfo` 404 (in der Liste, aber „nicht definiert“).
  - `…Specific`: `PpsRessourcenPositionenResolvedInfo` („Not supported. Try to use a item call“ → nur per `include` am Kopf), `SpecificUserfields`,
    `GetSammelmappenFileContent`, `LicenseOptions`, `PicklistenItemLager*` → 500 ohne Pflichtparameter (Unterressourcen).
- Regeln: `orderBy` Pflicht bei `count`; `$totalResults` kommt; Fehler-SQL in `$diagnoses`; Abfragen auf große Tabellen ohne Filter sind langsam
  (Count läuft immer mit).

## Katalog nach WMS-Bereich

| WMS-Bereich | Lesen (Ressource, Anzahl AKE-Test) | Zurückschreiben (Service) | Stand / Lücke |
|---|---|---|---|
| **Fertigungsaufträge, Struktur** | `PpsFaBelegeInfo` (27), `PpsFaBelegePositionenInfo` (132, mit `BelIDNachfolger`), `…ArbeitsgangInfo`, `…BereitstellungInfo`, `PpsFaBelegeVkBelegSucheInfo` (FA ↔ VK-Beleg) | `FertigungsauftragService` (neu, ändern, Positionen, **einlasten**), `UpdateFertigungsauftrag(Position)EntityService` (Feld setzen) | Lesen belegt (Struktur 835); Feld setzen getestet (§14, `GeaendertAm` bleibt unverändert!). FA-USER-Felder der AKE fehlen |
| **BDE-Rückmeldungen** | – (keine Ressource für gebuchte RM; indirekt `Istmenge`/Ist-Zeiten der FA-Position) | `PpsRueckmeldung{Arbeitsplatz,Material,Fertigmeldung,Stillstand,Verschnitt,Fremdfertigung}Service` mit Grundlagen je Aufruf | Material-RM auf FA 836 aufgerufen: abgelehnt, kein PPS-Mitarbeiter (§11, §14). FA beenden: kein Service |
| **Personal für BDE** | `PpsMitarbeiterInfo` (**0** am AKE-Test), `PpsSageUserInfo` (153 Sage-Benutzer, z. B. `akenb\…`), `SysSageUser` (153) | `PpsMitarbeiterKommtGehtService` (Kommen/Gehen/Pause); Lesen `PpsMitarbeiterKommtGeht` (0) | Kommen/Gehen getestet: 400 „Mitarbeiter nicht gefunden“ (§14). **PPS-Mitarbeiter in Sage anlegen**; kein Service dafür |
| **Arbeitsplätze, Arbeitsgänge** | `PpsArbeitsplaetzeInfo` (1: 9999), `PpsArbeitsgaengeInfo` (1), `PpsStillstandsbegruendungenInfo` (0) | – | Stammdaten in Sage pflegen |
| **Stücklisten** | `PpsRessourcenListenInfo` (28.809 Köpfe) + `include=PpsRessourcenPositionenResolvedInfo,SpecificUserfields` (mehrstufig, mit USER-Feldern), `PpsRessourcenPositionenInfo` (240.552), `ArtikelStueckliste` (Wawi, 136.805) | – | belegt (86003871) |
| **Artikel** | `Artikel` (279.532, **alle 154 USER_**), `ArtikelInfo`, `ArtikelVariantenExtended` (Bestand, verfügbar, **Meldebestand**, Bestellmenge, Hauptlieferant, WBZ, mittlerer EK), `ArtikelGruppen(Info)` (240), `ArtikelBezeichnung`, `GLMengeneinheitenInfo` | – (Meldebestand schreibbar nur über `apiArtikel` PUT, ungeprüft) | belegt (00722001: Bestand 200, Meldebestand 85, K31013, WBZ 14) |
| **Lagerorte, Lagerplätze** | `LagerorteInfo` (36), `LagerplaetzeInfo` (3.300) | – | belegt |
| **Bestände** | `LagerbestaendeArtikelInfo` / `ArtikelLagerbestaendeInfo` (4.544 Zeilen Artikel × Platz), Chargen/Seriennummern-Varianten (0) | – | belegt (00722001: 200 lfm auf LL 2 28 0) |
| **Lagerbuchungen (Zugang/Entnahme)** | `LagerplatzbuchungenInfo` (863.423) – **Delta über `BuchungsID gt …`** belegt; Memo-Korrelation belegt | `LagerbuchungService` (Zugang/Entnahme/Umbuchung je Position) | im WMS eingebaut (v1.28.0), am Test getestet |
| **Lagerumbuchungen** | wie oben (`Bewegungsart`) | `LagerbuchungService` mit `Lagerbewegungsart = 3 Umbuchung` (Herkunft + Ziel je Artikel) **oder** `LagerumbuchungCompleteService` (kompletter Platz → Platz, ohne Artikelliste) | **getestet** (§14): IU-Buchung hin und zurück, Memo gespeichert |
| **Inventur** | `InventurZaehllistenInfo`, `…PositionenInfo`, `…LagerplaetzeInfo` (je 0 am Test) | `InventurService` (`ZaehllisteId*`, Positionen: Artikel, Platz, Zählmenge, Chargen, Seriennummern) | Zähllisten müssen in Sage angelegt sein |
| **Bestellvorschläge / Bedarf** | `DispositionStapelInfo` (0), `DispositionStapelPositionenInfo` (0), `…VerursacherVkInfo`; Grundlage `ArtikelVariantenExtended` + `ArtikelLieferantInfo` (34.802: Bestellnummer, Mindestbestellmenge, EK-ME, WBZ, Preis) | `DispositionStapelService` (Stapel anlegen: Stapelkennung, Typ 0 Manuell … 4 DispoPPSAuftrag) + `DispositionStapelPositionenService` (Artikel, MengeBasis, Lieferant, fix-Kennzeichen; `OperationMode` 0 Neu/1 Update/2 Delete) | **getestet** (§14): Stapel + Position angelegt, Sage ergänzt Bestellnummer/Preis/Liefertermin; Position gelöscht; **Stapel-Löschen wirkt nicht** |
| **Bestellungen (offen) / Wareneingang** | `EinkaufBelegekopfInfo` (484), `EinkaufPositionenInfo` (1.167 offene Positionen mit Liefertermin, Menge, Ziel-Lagerplatz), `AuskunftOffeneEkBestellungenInfo` (519), `AuskunftOffeneEkWareneingaengeInfo` (480), `WareneingangPositionenCountInfo`, `EinkaufBelegeInfo` (168.284 alle) | `StandardBelegService` (Erfassungsart 4100 Einkauf, `BelegTransformArt` 1 Beleg-/2 Vorgangsübernahme, Positionen mit `BelegPositionLagermengen`) = Wareneingang buchen bzw. Bestellung anlegen; `BasisBelegService`, `UpdateBelegEntityService`, `UpdateBelegpositionEntityService` | Schema; nicht getestet |
| **Verkauf / Warenausgang / Kommissionierung** | `VerkaufBelegekopfInfo` (417), `VerkaufPositionenInfo` (972), `WarenausgangPositionenCountInfo` (417), `VerkaufKommissionierungPicklisten(Items)` (0) | `StandardBelegService` (4000 Verkauf, Lieferschein aus Auftrag), `PicklistItemsEditService`, `PicklistCreateLieferscheinService`, `TransitFinalizeService` | Sage-Kommissionierung am Test leer |
| **Lieferanten, Kunden, Adressen** | `Kontokorrent` (2.821, 11 USER), `Adressen` (3.843), `Ansprechpartner`, `ArtikelLieferantInfo`, `apiLieferanten`/`apiKunden` (200) | `apiKunden/apiLieferanten` PUT/POST (Schema) | Lesen belegt |
| **Etiketten, Belegdruck, FA-Papiere** | `AppServerPrintersInfo` (3), `BelegartenDruckprozesseInfo` | `PrintEtikettenBasicService` (Typ 1 Lager, 2 Artikel, 3 VK-Pos, 4 EK-Pos, **5 FA-Material**), `PrintFABelegeService`, `PrintBelegeService`, `ExportPrintBelegeService` (PDF) | Schema |
| **Sperren / Mehrbenutzer** | `StaticSemaphore*` | `MultiUserServiceSemaphoreService` (Acquire/Release, Read/Write/Exclusive), `SwitchBelegSperreService` | Schema |
| **Erreichbarkeit** | `ConnectionTestInfo` | `HealthcheckInfoService` | – |

## Delta-Lesen

| Ressource | Delta-Weg | Stand |
|---|---|---|
| FA | `GeaendertAm gt @…@` | belegt, **erfasst Änderungen über Services nicht** (§14) |
| Lagerplatzbuchungen | `BuchungsID gt <letzte>` | belegt |
| Artikel, Lagerplätze, Bestände, Ressourcenlisten, EK/VK-Belege | nur `Timestamp` (rowversion) – Filter scheitert (500) | **Vollabzug/Paging**; Frage an DPS |

## Was für den Rollout fehlt (in Sage bzw. bei DPS)

1. **Freigabe + Lizenz** `CommonWawiServicesResources` und der PPS-Services; Preis (DPS).
2. **PPS-Mitarbeiter** anlegen (AKE-Test hat keine) – Voraussetzung jeder Rückmeldung; Arbeitsplätze/Arbeitsgänge pflegen.
3. **FA beenden** (kein Service), **FA-Benutzerfelder lesen** (Property-Platzhalter belegen?), **gebuchte Rückmeldungen lesen**.
4. **Delta über `Timestamp`** für Stammdaten.
5. Wiederhol-Sicherheit: externe ID (`BacklogPrimaryKeys` speichern?) – sonst Anpassung.
6. IDEAL: eigener SData-Endpunkt (Kauf), gleicher Vertragsstand prüfen.

## Testplan Schreiben (Stand 2026-11-06: Punkte 5 und 6 ausgeführt, siehe §14; Punkte 2–4 warten auf Punkt 1)

1. PPS-Mitarbeiter in Sage anlegen (Gerald).
2. Material-RM FA 836 / BelPosID 10641 / 00722001 0,6 lfm von Platz 11 bzw. 2864, `BacklogPrimaryKeys` setzen → Bestand und `Istmenge` nachlesen.
3. Zeit-RM FA 835 / BelPosID 10656 (AG 999) / Arbeitsplatz 9999.
4. Fertigmeldung FA 836, 1 Stück, `IstEnde = false`.
5. Lagerumbuchung 1 lfm 00722001 LL 2 28 0 → anderer Platz und zurück (`LagerbuchungService` Bewegungsart 3).
6. Dispo-Stapel „WMS-Test“ (Typ 0 Manuell) mit einer Position 00722001 / K31013 anlegen, danach per `OperationMode 2` löschen.

**Umsetzung (Lesen + Stammdaten-Rückgabe):** Spec-Entwurf [[2026-11-06-sage-sdata-stammdaten-sync-spec]].

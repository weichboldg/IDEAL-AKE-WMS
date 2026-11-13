---
type: codebase-karte
updated: 2026-11-13
quelle: "Lesetest F7 (L1–L10) am AKE-Testserver sagetest01, Dataset ake_TEST2026;1, Verträge CommonWawiServicesResources, …Specific, CommonWawiServices, RealTimeData – nur GET, 2026-11-13; P-W1 lesend per SQL auf AKESQL20 (ake_TEST2026, Viewtext Zusatzinfo aus DB ake)"
---
# Sage SData – Lesetest Benutzerfelder (FA, Werkstattauftrag, Stückliste)

Auftrag: [[2026-11-13-sdata-teil-2-userfelder-stueckliste-spec]] (F7, Schritte L1–L10, P-W1). Grundlagen: [[sage-sdata-commonwawiservicesresources]] ·
[[sage-sdata-commonwawiservices]] · [[sage-sdata-realtimedata]] · [[sage-sdata-uebersicht]]. Die DPS-Fragen ergänzen die des PPS-Epics
[[2026-11-02-sage-pps-fa-bde-abgleich-spec]].

**Ausführung:** 2026-11-13 durch den Dev-Lauf (Claude Code), vom Entwicklungsrechner direkt per HTTPS-GET mit den Zugangsdaten der
AKE-DEV-Konfiguration (SData-Servicebenutzer; Name und Kennwort stehen hier bewusst nicht). **Nur GET**, bei Datenabfragen immer mit
`orderBy`, kleine `count`. Kein Schreibversuch. Die Feldliste des Registers Sage-Test (F3) stand zu diesem Zeitpunkt auf DEV noch nicht;
die Abfragen sind dieselben, die F3/F4 senden. Rohdaten (Schema-XML, JSON) wurden nicht abgelegt.

Spalten der Feldtabellen: **Typ/Länge** aus `$schema` (`sme:maxLength`, im …Specific-Vertrag ohne Längen), **Schlüssel** =
`sme:isUniqueKey`, **in den Daten** = Feld kam in der Datenantwort vor, **Beispiel** = erster nicht leerer Wert (gekürzt; Benutzernamen,
Beträge und Adressdaten durch Platzhalter ersetzt), **Benutzerfeld** = `USER_`-Feld (ja), `Belegproperty/PositionProperty`
(Platzhalter), `BstHLP_USER_…` (Hilfsfeld).

## Ergebnis in Kürze

| # | Abfrage | HTTP | Befund |
|---|---|---|---|
| L1 | `CommonWawiServicesResources/{DS}/PpsFaBelegeInfo/$schema` | **401** | Ressourcen-`$schema` antwortet mit 401 (Daten mit denselben Zugangsdaten 200). Das **Vertrags**-`$schema` (`…/{DS}/$schema`, 0,9 MB) liefert 200 – Felder unten daraus. |
| L1 | `…/PpsFaBelegeInfo?where=BelID eq 835&orderBy=BelID&count=1` | 200 | FA 835 existiert (Artikel 86003871). 67 Felder, 3 USER_ (nur DPSBS-MDE), `BelegProperty1–4` leer. **Keine AKE-FA-Benutzerfelder.** |
| L2 | `PpsFaBelegePositionenInfo` (`orderBy=BelPosID`, count 5) / `PpsFaBelegePositionenArbeitsgangInfo` (`orderBy=BelAGPosID`) | 200 / 200 | 11 Positionen (`$totalResults`), 1 AG-Position. 120 bzw. 73 Felder; Positionen 3 USER_ (DPSBS), `PositionProperty1–4` leer. |
| L3 | `PpsFaBelegeInfo?…&include=SpecificUserfields` in **CommonWawiServicesResources** | 200 | `include` wird still ignoriert (Antwort identisch mit L1, kein `SpecificUserfields` – die Ressource hat keine solche Kind-Liste). |
| L3 | dasselbe in **CommonWawiServicesResourcesSpecific** | 404 | `ResourceKindNotFound` – „Die Resource 'PpsFaBelegeInfo' ist nicht definiert.“ Der Specific-Vertrag kennt nur 14 Ressourcen. |
| L4 | `…Specific/{DS}/$schema` | 200 | 14 Ressourcen (Liste unten). `SpecificUserfields` / `CustomFields` als eigene Ressource (`orderBy`, count 5): **500** „Der Objektverweis wurde nicht auf eine Objektinstanz festgelegt.“ – nur als `include`-Kind nutzbar. |
| L5 | `…Specific/{DS}/PpsRessourcenListenInfo?where=Mandant eq 1 and Artikelnummer eq '86003871'&include=PpsRessourcenPositionenResolvedInfo,SpecificUserfields&orderBy=Artikelnummer&count=1` | 200 | Kopf 34 Felder + 1 Benutzerfeld, 17 aufgelöste Positionen (11 × Ebene 1, 6 × Ebene 2) mit je 5 Benutzerfeldern. |
| L6 | Vertrags-`$schema` (Ressourcen-`$schema` 401 wie L1) | 200 | Nur `ArtikelStueckliste` ist anlegbar/änderbar (Warenwirtschafts-Stückliste, **nicht** die PPS-Ressourcenliste). PPS-Ressourcenlisten: nur Lesen. |
| L7 | `CommonWawiServices/{DS}/$schema` | 200 | 36 Service-Operationen; 3 passen auf den Filter (alle FA). **Keine** Operation für Ressourcenliste/Stückliste/Userfield. `FertigungsauftragService` kennt alle 34 AKE-FA-Benutzerfelder als Request-Typ. |
| L8 | `RealTimeData/{DS}/dtsFaBelegsuche?where=BelID eq 835` | 404 | Name ohne Suffix unbekannt. Richtig: **`dtsFaBelegsuche.Sage.PPS`** → 200, 36 Benutzerfelder mit Präfix `KHKPpsFaBelege_USER_…`. |
| L8 | `dtsAbfLOGFertigungsauftraege` | 404 / 500 | Richtiger Name **`dtsAbfLOGFertigungsauftraege.100036671.AKE_InterneFA`**; mit `where=BelID eq 835` 500 „Mehrdeutiger Spaltenname BelID“ (Diagnose zeigt das SQL: Quelle ist **`KHKIFAuftraege`** + VK-Belege); mit `where=KHKIFAuftraege.BelID eq …` 200, aber leer – die Quelle ist parametrisiert (`KHKIFAuftraege.BelID = NULL` im erzeugten SQL). |
| P-W1 | SQL lesend, `ake_TEST2026` / `ake` | ok | **Werkstattauftrag (WA) = Tabelle `KHKIFAuftraege`** (AKE-eigene „interne FA“), nicht `KHKPpsFaBelege`. 26 USER_-Spalten. |
| L9 | `VerkaufBelegepositionenRawInfo?where=USER_FABelId eq 2646002&orderBy=BelPosID&count=2` | 200 | Über SData nur **indirekt** lesbar: die Auftragsposition (VK) mit `USER_FABelId` = WA-Nummer und 45 USER_. `KHKIFAuftraege` selbst steht in **keinem** CommonWawi-Vertrag. |
| L10 | IDEAL | – | **offen** – kein IDEAL-Endpunkt eingetragen; nichts an IDEAL gesendet. |

**Kernaussagen**
1. **FA (PPS, `KHKPpsFaBelege`):** Über den freigegebenen Lesevertrag `CommonWawiServicesResources` sind die 34 AKE-Benutzerfelder des FA
   (USER_Maschine, USER_Kaeltemittel, USER_Ventil … Liste unter L7) **nicht lesbar**; geliefert werden nur die 3 DPSBS-MDE-Felder und die leeren
   Platzhalter `BelegProperty1–4`. Lesbar sind sie nur über `RealTimeData` (`dtsFaBelegsuche.Sage.PPS`, Präfix `KHKPpsFaBelege_USER_`), das laut
   DPS für den Betrieb nicht freigegeben ist. Schreibbar sind sie über `FertigungsauftragService` (Anlage) – das Schema belegt damit, dass die
   Felder in `KHKPpsFaBelege` existieren.
2. **FA-Positionen:** keine AKE-Benutzerfelder (Request-Typ `FaBelegPositionUserfield` ist leer; Lesen: nur DPSBS + `PositionProperty1–4`).
3. **Werkstattauftrag AKE (P-W1):** eigene Tabelle `KHKIFAuftraege` (BelID = `[WA Nummer]`), verknüpft über `KHKVKBelegePositionen.USER_FABelId`.
   Die WMS-Felder Kältemittel/Ventil/Ausführung/Maschine/Status kommen von dort. Über SData: Tabelle nicht erreichbar; nur die VK-Position
   (`VerkaufBelegepositionenRawInfo`, Filter `USER_FABelId`) und eine parametrisierte RealTimeData-Quelle ohne bekannten Parameterweg.
   Damit ist auch Frage **G13** des PPS-Epics beantwortet: `[WA Nummer]` ist die BelID von `KHKIFAuftraege`, **nicht** die PPS-FA-BelID.
4. **Stückliste (PPS-Ressourcenliste):** vollständig lesbar inkl. Benutzerfelder (Kopf `USER_VKZuschlag`, Positionen `USER_VKPreis`,
   `USER_VKEinzelpreis`, `USER_EPDMVERSION`, `USER_AUSCAD`, `USER_Positionsnummer`) – **aber nicht schreibbar** (kein canPost/canPut am
   Ressourcen-Element, keine Service-Operation).
5. **Für F3 (Feldliste) wichtig:** `{Ressource}/$schema` liefert am AKE-Test **401**; Schritt 1 der Feldliste muss dann „Schema nicht lesbar“
   melden und mit den Daten weitermachen – oder das Vertrags-`$schema` lesen (0,9 MB, ~0,3 s) und die Ressource daraus herausschneiden.
   `include` auf einer Ressource ohne passende Kind-Liste wird still ignoriert (kein Fehler).
6. **RealTimeData-Namen** tragen das Suffix (`.Sage.PPS`, `.100036671.AKE_InterneFA`); ohne Suffix 404 `ResourceKindNotFound`.

## L1 – PpsFaBelegeInfo (FA-Kopf, `CommonWawiServicesResources`)

**Rechte laut Schema:** Lesen ja · Anlegen nein · Ändern nein · Löschen nein

> Schema-Doku: PPS Fertigungsaufträge (Fertigungsaufträge mit Lagerinformationen und Variante für PPS Modul). Enthält Mandant, BelID, Belegnummer, Artikel, Variante, Mengen, Termine, Status. Datenquelle: vwDPSBS_CWS_PPS_FaBelegeInfo. Enthaltene Tabellen: KHKPpsFaBelege, KHKLagerplaetze, KHKLagerorte. Wichtige Spalten: Mandant, BelID, Belegnummer, Artikelnummer, Variante, Sollmenge, Istmenge, Sollstarttermin, Iststarttermin, Sollendtermin, Istendtermin, AusfuehrungsArt, USER_DPSBSMDEFreigabe. Typische Verwendung: Fertigungsauftragsübersicht mit Terminen für PPS.

| Feld | Typ | Länge | Schlüssel | in den Daten | Beispiel | Benutzerfeld |
|---|---|---|---|---|---|---|
| `PpsFaBelegePositionenInfo` | Kind-Liste (child) | | | | | |
| `PpsFaBelegePositionenArbeitsgangInfo` | Kind-Liste (child) | | | | | |
| `BelID` | integer | 4 | ja | ja | 835 | nein |
| `Mandant` | short | 2 | ja | ja | 1 | nein |
| `StrukturID` | integer | 4 |  | ja | 835 | nein |
| `Belegjahr` | short | 2 |  | ja | 2027 | nein |
| `Belegnummer` | integer | 4 |  | ja | 0 | nein |
| `Artikelnummer` | string | 31 |  | ja | 86003871 | nein |
| `Variante` | string | 4 |  | ja | 1 | nein |
| `Bezeichnung1` | string | 50 |  | ja | Waermebruecke 112-kpl_Basic/ComfortLine | nein |
| `Bezeichnung2` | string | 50 |  | ja | Basic/ComfortLine 112 | nein |
| `Matchcode` | string | 50 |  | ja | Waermebruecke 112-kpl_Basic/ComfortLine | nein |
| `Memo` | string | 2147483647 |  | ja |  | nein |
| `Zeichnungsnummer` | string | 50 |  | ja |  | nein |
| `Terminierung` | short | 2 |  | ja | 0 | nein |
| `Sollmenge` | decimal | 8 |  | ja | 1 | nein |
| `Fertigungmenge` | decimal | 8 |  | ja | 1 | nein |
| `Bestellmenge` | decimal | 8 |  | ja | 0 | nein |
| `Istmenge` | decimal | 8 |  | ja | 0 | nein |
| `Sollstarttermin` | dateTime | 8 |  | ja | 02/03/2026 08:45:00 | nein |
| `Iststarttermin` | dateTime | 8 |  | ja |  | nein |
| `Sollendtermin` | dateTime | 8 |  | ja | 02/04/2026 00:00:00 | nein |
| `Istendtermin` | dateTime | 8 |  | ja |  | nein |
| `VerursacherTyp` | short | 2 |  | ja | 10 | nein |
| `IstEingelastet` | boolean | 2 |  | ja | -1 | nein |
| `IstBegonnen` | boolean | 2 |  | ja | 0 | nein |
| `IstBeendet` | boolean | 2 |  | ja | 0 | nein |
| `AenderungsIndex` | short | 2 |  | ja | -1 | nein |
| `IstGedruckt` | boolean | 2 |  | ja | -1 | nein |
| `AusfuehrungsArt` | string | 1 |  | ja |  | nein |
| `IstEinlastungTermin` | dateTime | 8 |  | ja | 02/02/2026 00:00:00 | nein |
| `IstBestelltTermin` | dateTime | 8 |  | ja |  | nein |
| `IstGeliefertTermin` | dateTime | 8 |  | ja |  | nein |
| `AuspraegungID` | integer | 4 |  | ja | 0 | nein |
| `PlatzID` | integer | 4 |  | ja | 2555 | nein |
| `DimText` | string | 2147483647 |  | ja |  | nein |
| `Zusatztext` | string | 2147483647 |  | ja |  | nein |
| `Fertigungsart` | string | 2 |  | ja | EF | nein |
| `Projekt` | string | 31 |  | ja |  | nein |
| `Kostentraeger` | string | 20 |  | ja |  | nein |
| `Kostenstelle` | string | 20 |  | ja |  | nein |
| `IstGesperrt` | boolean | 2 |  | ja | 0 | nein |
| `KapBelegungPruefen` | short | 2 |  | ja | -1 | nein |
| `IstCadFreigegeben` | boolean | 2 |  | ja | 0 | nein |
| `PKonfigID` | string | 20 |  | ja |  | nein |
| `IstReaktiviert` | boolean | 2 |  | ja | 0 | nein |
| `PKonfigPosID` | integer | 4 |  | ja | 0 | nein |
| `Sprache` | string | 3 |  | ja |  | nein |
| `Aktiv` | boolean | 2 |  | ja | -1 | nein |
| `AngelegtVon` | string | 128 |  | ja | (Benutzername) | nein |
| `AngelegtAm` | dateTime | 8 |  | ja | 02/02/2026 14:17:32 | nein |
| `GeaendertVon` | string | 128 |  | ja | (Benutzername) | nein |
| `GeaendertAm` | dateTime | 8 |  | ja | 02/02/2026 14:36:24 | nein |
| `ArchiviertVon` | string | 128 |  | ja |  | nein |
| `ArchiviertAm` | dateTime | 8 |  | ja |  | nein |
| `Timestamp` | base64Binary | 8 |  | ja | AAAAAD2adJU= | nein |
| `Verursacher` | string | 4 |  | ja | MAN | nein |
| `Kurzbezeichnung` | string | 50 |  | ja | HR;9;3;2 | nein |
| `Platzbezeichnung` | string | 110 |  | ja | Hochregal;Regal 9;Ebene 3;Platz 2 | nein |
| `USER_DPSBSMDELagerInaktivBei` | short | 2 |  | ja | 0 | ja |
| `VarianteText` | string | 130 |  | ja | Grundartikel | nein |
| `USER_DPSBSBereitstellungBeendet` | short | 2 |  | ja | 0 | ja |
| `USER_DPSBSMDEFreigabe` | short | 2 |  | ja | 0 | ja |
| `BelegProperty1` | string | 1 |  | ja |  | Platzhalter |
| `BelegProperty2` | string | 1 |  | ja |  | Platzhalter |
| `BelegProperty3` | string | 1 |  | ja |  | Platzhalter |
| `BelegProperty4` | string | 1 |  | ja |  | Platzhalter |

Felder im Schema: 67 (davon Benutzerfelder USER_: 3)

## L2 – PpsFaBelegePositionenInfo (FA-Positionen)

**Rechte laut Schema:** Lesen ja · Anlegen nein · Ändern nein · Löschen nein

> Schema-Doku: PPS Fertigungsaufträge Positionen (Detaillierte Positionen von Fertigungsaufträgen mit Ressourcen, Arbeitsgängen und Mengen für PPS). Enthält Mandant, BelID, BelPosID,Position, Ressourcen, Arbeitsgänge, Mengen, Termine, Kosten. Datenquelle: vwDPSBS_CWS_PPS_FaBelegePositionenInfo. Enthaltene Tabellen: KHKPpsFaBelegePositionen, KHKLagerplaetze, KHKLagerorte, KHKPpsArbeitsplaetze, KHKPpsArbeitsgaenge. Wichtige Spalten: Mandant, BelPosID, BelID, Position, RessourceNummer, RessourceTyp, ArbeitsgangNummer, Menge, Istmenge, Sollstarttermin, Iststarttermin, Sollendtermin, Istendtermin, Status, Kosten. Typische Verwendung: Fertigungsauftragspositionen mit Ressourcen und Terminen für PPS.

| Feld | Typ | Länge | Schlüssel | in den Daten | Beispiel | Benutzerfeld |
|---|---|---|---|---|---|---|
| `PpsFaBelegePositionenArbeitsgangInfo` | Kind-Liste (child) | | | | | |
| `BelPosID` | integer | 4 | ja | ja | 10640 | nein |
| `BelID` | integer | 4 | ja | ja | 835 | nein |
| `Mandant` | short | 2 | ja | ja | 1 | nein |
| `Position` | short | 2 |  | ja | 1 | nein |
| `BelIDNachfolger` | integer | 4 |  | ja | 836 | nein |
| `RessourceTyp` | string | 2 |  | ja | MA | nein |
| `RessourceNummer` | string | 31 |  | ja | 86003870 | nein |
| `ArbeitsgangNummer` | string | 50 |  | ja |  | nein |
| `Matchcode` | string | 50 |  | ja | Schutzschlauch DM6 L=600mm_Basic/Comfort… | nein |
| `Dimensionstext` | string | 2147483647 |  | ja | Schutzschlauch DM6 L=600mm_Basic/Comfort… | nein |
| `Menge` | decimal | 8 |  | ja | 1 | nein |
| `MengeNKS` | integer | 4 |  | ja | 0 | nein |
| `MengeBerechnung` | string | 50 |  | ja |  | nein |
| `MengeDimensionen` | string | 50 |  | ja |  | nein |
| `Umrechnungsfaktor` | decimal | 8 |  | ja | 1 | nein |
| `IstEinmalmenge` | boolean | 2 |  | ja | 0 | nein |
| `Ruestzeittext` | string | 13 |  | ja |  | nein |
| `Ruestzeit` | decimal | 8 |  | ja | 0 | nein |
| `Stueckzeittext` | string | 13 |  | ja |  | nein |
| `Stueckzeit` | decimal | 8 |  | ja | 0 | nein |
| `Pufferzeittext` | string | 13 |  | ja |  | nein |
| `Pufferzeit` | decimal | 8 |  | ja | 0 | nein |
| `Ueberlappungzeittext` | string | 13 |  | ja |  | nein |
| `Ueberlappungzeit` | decimal | 8 |  | ja | 0 | nein |
| `Ueberlappungmenge` | decimal | 8 |  | ja | 0 | nein |
| `Preis` | decimal | 8 |  | ja | (Betrag) | nein |
| `Lagermengeneinheit` | string | 5 |  | ja | Stk | nein |
| `Sollmenge` | decimal | 8 |  | ja | 1 | nein |
| `Fertigungmenge` | decimal | 8 |  | ja | 1 | nein |
| `Istmenge` | decimal | 8 |  | ja | 0 | nein |
| `Sollstarttermin` | dateTime | 8 |  | ja | 02/03/2026 08:45:00 | nein |
| `Iststarttermin` | dateTime | 8 |  | ja |  | nein |
| `Sollendtermin` | dateTime | 8 |  | ja | 02/03/2026 08:45:00 | nein |
| `Istendtermin` | dateTime | 8 |  | ja |  | nein |
| `IstFixtermin` | boolean | 2 |  | ja | 0 | nein |
| `SollRuestzeit` | decimal | 8 |  | ja | 0 | nein |
| `IstRuestzeit` | decimal | 8 |  | ja | 0 | nein |
| `SollStueckzeit` | decimal | 8 |  | ja | 0 | nein |
| `IstStueckzeit` | decimal | 8 |  | ja | 0 | nein |
| `Status` | short | 2 |  | ja | 0 | nein |
| `AenderungsIndex` | short | 2 |  | ja | -1 | nein |
| `Aenderungskennzeichen` | string | 1 |  | ja |  | nein |
| `SollArbeitsplatzkostenRuesten` | decimal | 8 |  | ja | (Betrag) | nein |
| `SollArbeitsplatzkostenBetrieb` | decimal | 8 |  | ja | (Betrag) | nein |
| `SollLohnkostenRuesten` | decimal | 8 |  | ja | (Betrag) | nein |
| `SollLohnkostenBetrieb` | decimal | 8 |  | ja | (Betrag) | nein |
| `SollEinzelpreis` | decimal | 8 |  | ja | (Betrag) | nein |
| `WKz` | string | 3 |  | ja | EUR | nein |
| `Bestellmenge` | decimal | 8 |  | ja | 0 | nein |
| `AusfuehrungsArt` | string | 1 |  | ja |  | nein |
| `IstEinlastungTermin` | dateTime | 8 |  | ja | 02/02/2026 00:00:00 | nein |
| `IstBestelltTermin` | dateTime | 8 |  | ja |  | nein |
| `IstGeliefertTermin` | dateTime | 8 |  | ja |  | nein |
| `IstArbeitsplatzkostenRuesten` | decimal | 8 |  | ja | (Betrag) | nein |
| `IstArbeitsplatzkostenBetrieb` | decimal | 8 |  | ja | (Betrag) | nein |
| `IstLohnkostenRuesten` | decimal | 8 |  | ja | (Betrag) | nein |
| `IstLohnkostenBetrieb` | decimal | 8 |  | ja | (Betrag) | nein |
| `IstGesamtpreis` | decimal | 8 |  | ja | (Betrag) | nein |
| `PositionExtern` | integer | 4 |  | ja | 10 | nein |
| `PositionExternPraefix` | string | 9 |  | ja |  | nein |
| `PositionExternSuffix` | string | 9 |  | ja |  | nein |
| `Lieferausgangsmenge` | decimal | 8 |  | ja | 0 | nein |
| `Transportzeittext` | string | 13 |  | ja |  | nein |
| `Transportzeit` | decimal | 8 |  | ja | 0 | nein |
| `AusfuehrungsArtAG` | string | 1 |  | ja |  | nein |
| `AuspraegungID` | integer | 4 |  | ja | 0 | nein |
| `PlatzID` | integer | 4 |  | ja | 0 | nein |
| `BelPosIDRessourcenliste` | integer | 4 |  | ja | 31860 | nein |
| `Materialteileart` | string | 3 |  | ja |  | nein |
| `Schwundart` | string | 5 |  | ja | Stk | nein |
| `Schwund` | decimal | 8 |  | ja | 0 | nein |
| `IstCadFixPos` | boolean | 2 |  | ja | 0 | nein |
| `Planzeit` | decimal | 8 |  | ja | 0 | nein |
| `Ueberlappungtyp` | string | 1 |  | ja |  | nein |
| `Ueberlappungprozent` | decimal | 8 |  | ja | 0 | nein |
| `Bedarfstermin` | dateTime | 8 |  | ja | 02/03/2026 08:45:00 | nein |
| `Planzeittext` | string | 13 |  | ja |  | nein |
| `PositionCAD` | string | 20 |  | ja |  | nein |
| `BelPosIdAP` | integer | 4 |  | ja | 10656 | nein |
| `RessourcenummerAP` | string | 50 |  | ja | 999 | nein |
| `ResAPBezeichnung1` | string | 50 |  | ja | TESTFertigung | nein |
| `ResAPBezeichnung2` | string | 50 |  | ja | TESTFertigung | nein |
| `ResAPMatchcode` | string | 50 |  | ja | TESTFertigung | nein |
| `Anzahl` | integer | 4 |  | ja | 0 | nein |
| `FormelID` | string | 20 |  | ja |  | nein |
| `DIM1` | decimal | 8 |  | ja | 0 | nein |
| `DIM2` | decimal | 8 |  | ja | 0 | nein |
| `DIM3` | decimal | 8 |  | ja | 0 | nein |
| `Projektlager` | integer | 4 |  | ja | 0 | nein |
| `Fremdfertiger` | string | 20 |  | ja |  | nein |
| `Leistungsgrad` | decimal | 8 |  | ja | 0 | nein |
| `BdeTerminalId` | string | 20 |  | ja |  | nein |
| `BdeMitarbeiter` | string | 20 |  | ja |  | nein |
| `PKonfigPosID` | integer | 4 |  | ja | 0 | nein |
| `RessourceTypAg` | short | 2 |  | ja | 0 | nein |
| `RessourceNummerAg` | string | 31 |  | ja |  | nein |
| `ResAGBezeichnung1` | string | 50 |  | ja |  | nein |
| `ResAGBezeichnung2` | string | 50 |  | ja |  | nein |
| `ResAGMatchcode` | string | 50 |  | ja |  | nein |
| `PositionExternAnzeige` | string | 11 |  | ja | 10 | nein |
| `KapazAnzahlMax` | short | 2 |  | ja | 0 | nein |
| `MindestmengeAG` | decimal | 8 |  | ja | 0 | nein |
| `Aktiv` | boolean | 2 |  | ja | -1 | nein |
| `Timestamp` | base64Binary | 8 |  | ja | AAAAADxqVOE= | nein |
| `Kurzbezeichnung` | string | 50 |  | ja |  | nein |
| `Platzbezeichnung` | string | 110 |  | ja |  | nein |
| `USER_DPSBSMDELagerInaktivBei` | short | 2 |  | ja | 0 | ja |
| `BstHLPlatzId` | integer | 4 |  | ja |  | nein |
| `BstHLKurzbezeichnung` | string | 50 |  | ja |  | nein |
| `BstHLPlatzbezeichnung` | string | 110 |  | ja |  | nein |
| `BstHLP_USER_DPSBSMDELagerInaktivBei` | short | 2 |  | ja | 0 | Hilfsfeld |
| `VarianteText` | string | 130 |  | ja | Grundartikel | nein |
| `MengeBeigestellt` | decimal | 17 |  | ja | 0 | nein |
| `USER_DPSBSBereitstellungMA` | string | 100 |  | ja |  | ja |
| `USER_DPSBSGruppierung` | string | 50 |  | ja |  | ja |
| `PositionProperty1` | string | 1 |  | ja |  | Platzhalter |
| `PositionProperty2` | string | 1 |  | ja |  | Platzhalter |
| `PositionProperty3` | string | 1 |  | ja |  | Platzhalter |
| `PositionProperty4` | string | 1 |  | ja |  | Platzhalter |

Felder im Schema: 120 (davon Benutzerfelder USER_: 3)

## L2 – PpsFaBelegePositionenArbeitsgangInfo (FA-Arbeitsgänge)

**Rechte laut Schema:** Lesen ja · Anlegen nein · Ändern nein · Löschen nein

> Schema-Doku: PPS Fertigungsaufträge Positionen Arbeitsgang (Arbeitsganginformationen zu Fertigungsauftragspositionen für PPS). Enthält Mandant, BelID, BelPosID, Arbeitsgangdaten. Datenquelle: vwDPSBS_CWS_PPS_FaBelegePositionenArbeitsgangInfo. Enthaltene Tabellen: KHKPpsFaBelegePositionen, KHKPpsFaBelegeArbeitsgaenge. Wichtige Spalten: Mandant, BelID, BelPosID, Arbeitsgang, ArbeitsgangBezeichnung, SollDauer, IstDauer, ArbeitsgangStatus. Typische Verwendung: Arbeitsgangübersicht für Fertigungsauftragspositionen für PPS.

| Feld | Typ | Länge | Schlüssel | in den Daten | Beispiel | Benutzerfeld |
|---|---|---|---|---|---|---|
| `BelAGPosID` | integer | 4 | ja | ja | 10657 | nein |
| `Mandant` | short | 2 | ja | ja | 1 | nein |
| `BelID` | integer | 4 |  | ja | 835 | nein |
| `BelPosID` | integer | 4 |  | ja | 10656 | nein |
| `Position` | short | 2 |  | ja | 1 | nein |
| `BelIDNachfolger` | integer | 4 |  | ja | 0 | nein |
| `RessourceTypAG` | short | 2 |  | ja | 1 | nein |
| `Matchcode` | string | 50 |  | ja | TESTFertigungsArbeitsplatz | nein |
| `RessourceNummerAg` | string | 31 |  | ja | 9999 | nein |
| `ResAGBezeichnung1` | string | 50 |  | ja | TESTFertigungsArbeitsplatz | nein |
| `ResAGBezeichnung2` | string | 50 |  | ja | TESTFertigungsArbeitsplatz | nein |
| `ResAGMatchcode` | string | 50 |  | ja | TESTFertigungsArbeitsplatz | nein |
| `Dimensionstext` | string | 2147483647 |  | ja |    | nein |
| `Kapgruppennummer` | string | 20 |  | ja |  | nein |
| `Kapgruppenname` | string | 50 |  | ja |  | nein |
| `Menge` | decimal | 8 |  | ja | 0 | nein |
| `MengeNKS` | integer | 4 |  | ja | 0 | nein |
| `MengeBerechnung` | string | 50 |  | ja |  | nein |
| `MengeDimensionen` | string | 50 |  | ja |  | nein |
| `Umrechnungsfaktor` | decimal | 8 |  | ja | 0 | nein |
| `IstEinmalmenge` | boolean | 2 |  | ja | 0 | nein |
| `Leistungsgrad` | decimal | 8 |  | ja | 100 | nein |
| `Prozentanteil` | decimal | 8 |  | ja | 100 | nein |
| `Ueberlappungzeittext` | string | 13 |  | ja |  | nein |
| `Ueberlappungzeit` | decimal | 8 |  | ja | 0 | nein |
| `Ueberlappungmenge` | decimal | 8 |  | ja | 0 | nein |
| `Preis` | decimal | 8 |  | ja | (Betrag) | nein |
| `Lagermengeneinheit` | string | 5 |  | ja |  | nein |
| `Sollmenge` | decimal | 8 |  | ja | 0 | nein |
| `Fertigungmenge` | decimal | 8 |  | ja | 0 | nein |
| `Istmenge` | decimal | 8 |  | ja | 0 | nein |
| `Sollstarttermin` | dateTime | 8 |  | ja | 02/03/2026 08:45:00 | nein |
| `Iststarttermin` | dateTime | 8 |  | ja |  | nein |
| `Sollendtermin` | dateTime | 8 |  | ja | 02/03/2026 14:30:00 | nein |
| `Istendtermin` | dateTime | 8 |  | ja |  | nein |
| `Status` | short | 2 |  | ja | 0 | nein |
| `AenderungsIndex` | short | 2 |  | ja |  | nein |
| `Aenderungskennzeichen` | string | 1 |  | ja |  | nein |
| `SollArbeitsplatzkostenRuesten` | decimal | 8 |  | ja | (Betrag) | nein |
| `SollArbeitsplatzkostenBetrieb` | decimal | 8 |  | ja | (Betrag) | nein |
| `SollLohnkostenRuesten` | decimal | 8 |  | ja | (Betrag) | nein |
| `SollLohnkostenBetrieb` | decimal | 8 |  | ja | (Betrag) | nein |
| `SollEinzelpreis` | decimal | 8 |  | ja | (Betrag) | nein |
| `WKz` | string | 3 |  | ja |  | nein |
| `Bestellmenge` | decimal | 8 |  | ja | 0 | nein |
| `AusfuehrungsArt` | string | 1 |  | ja |  | nein |
| `IstEinlastungTermin` | dateTime | 8 |  | ja |  | nein |
| `IstBestelltTermin` | dateTime | 8 |  | ja |  | nein |
| `IstGeliefertTermin` | dateTime | 8 |  | ja |  | nein |
| `IstArbeitsplatzkostenRuesten` | decimal | 8 |  | ja | (Betrag) | nein |
| `IstArbeitsplatzkostenBetrieb` | decimal | 8 |  | ja | (Betrag) | nein |
| `IstLohnkostenRuesten` | decimal | 8 |  | ja | (Betrag) | nein |
| `IstLohnkostenBetrieb` | decimal | 8 |  | ja | (Betrag) | nein |
| `IstGesamtpreis` | decimal | 8 |  | ja | (Betrag) | nein |
| `Lieferausgangsmenge` | decimal | 8 |  | ja | 0 | nein |
| `AusfuehrungsArtAG` | string | 1 |  | ja |  | nein |
| `BelPosIDRessourcenliste` | integer | 4 |  | ja | 0 | nein |
| `Materialteileart` | string | 3 |  | ja |  | nein |
| `IstCadFixPos` | boolean | 2 |  | ja | 0 | nein |
| `PositionCAD` | string | 20 |  | ja | 0 | nein |
| `Schwundart` | string | 5 |  | ja |  | nein |
| `Schwund` | decimal | 8 |  | ja | 0 | nein |
| `Wartezeittext` | string | 13 |  | ja |  | nein |
| `Wartezeit` | decimal | 8 |  | ja | 0 | nein |
| `IstManuell` | boolean | 2 |  | ja | -1 | nein |
| `IstFixZuordnung` | boolean | 2 |  | ja | 0 | nein |
| `Sollruestzeit` | decimal | 8 |  | ja | 0 | nein |
| `BelegungDauer` | decimal | 8 |  | ja | 18000 | nein |
| `Timestamp` | base64Binary | 8 |  | ja | AAAAADxqVOQ= | nein |
| `PositionProperty1` | string | 1 |  | ja |  | Platzhalter |
| `PositionProperty2` | string | 1 |  | ja |  | Platzhalter |
| `PositionProperty3` | string | 1 |  | ja |  | Platzhalter |
| `PositionProperty4` | string | 1 |  | ja |  | Platzhalter |

Felder im Schema: 73 (davon Benutzerfelder USER_: 0)

## L3 – SpecificUserfields am FA

- `CommonWawiServicesResources`: HTTP 200, `include=SpecificUserfields` wirkungslos (kein Kind dieses Namens an `PpsFaBelegeInfo`).
- `CommonWawiServicesResourcesSpecific`: HTTP 404 `ResourceKindNotFound`. **Kein `DatafieldName` lesbar.**

## L4 – CommonWawiServicesResourcesSpecific: Ressourcen und Abdeckung

| Ressource | Rechte (Ressource) | Felder / Inhalt |
|---|---|---|
| `PpsRessourcenListenInfo` | Lesen | PPS-Ressourcenliste (Kopf) je Artikel/Variante, Kinder `PpsRessourcenPositionenResolvedInfo`, `SpecificUserfields` |
| `PpsRessourcenPositionenResolvedInfo` | Lesen | aufgelöste mehrstufige Positionen (`Ebene`, `RowGuid`/`RowGuidParent`, `BelPosID`/`BelPosIdParent`, `ArtikelnummerUBG`), Kind `SpecificUserfields` |
| `SpecificUserfields` | Lesen (eigenständig 500) | `DatafieldName`, `DatafieldValue` (anyType), `DatafieldType`, `DatafieldLength` |
| `CustomFields` | Lesen (eigenständig 500) | `Name`, `Value` (anyType) |
| `GetSammelmappenFileContent`, `GetSammelmappenFile` | Lesen | Sammelmappe (Dateien) |
| `LicenseOptions`, `UserLicense` | Lesen | Lizenzinfo |
| `SortedVerkaufKommissionierungPicklistenItems(Full)`, `PicklistenItemLager`, `…Chargen`, `…Seriennummern`, `…Zugangskennungen` | Lesen | Picklisten (VK), je 8 `UserProperty`-Platzhalter |

Schlüsselfelder: `PpsRessourcenListenInfo` und `…ResolvedInfo` nur `Mandant` (`isMandatory`, `isUniqueKey`) – deshalb `Mandant` im `where`
Pflicht. `SpecificUserfields`/`CustomFields` decken **nur** die Objekte ab, an denen sie als Kind hängen: Ressourcenlisten-Kopf und
-Position. Im Schema tragen die Kind-Elemente `canPost/canPut="true"`, die Ressourcen selbst nicht (siehe Schreibbarkeit).

**Alle Ressourcen mit Benutzerfeldern im Lesevertrag** (`CommonWawiServicesResources`, Anzahl Felder / USER_ / Property-Platzhalter):

| Ressource | Felder | USER_ | Property-Platzhalter | Tabellen laut Schema-Doku |
|---|---|---|---|---|
| `Adressen` | 46 | 9 | 0 |  |
| `Ansprechpartner` | 36 | 13 | 0 |  |
| `Kontokorrent` | 115 | 11 | 0 |  |
| `ArtikelVariantenInfo` | 141 | 0 | 8 |  |
| `ArtikelInfo` | 123 | 0 | 8 |  |
| `ArtikelVariantenExtended` | 64 | 8 | 0 |  |
| `DispositionStapelPositionenVerursacherVkInfo` | 23 | 0 | 8 |  |
| `DispositionStapelPositionenInfo` | 73 | 0 | 8 |  |
| `DispositionStapelInfo` | 27 | 0 | 8 |  |
| `EinkaufBelegeInfo` | 151 | 0 | 4 |  |
| `EinkaufBelegePositionenInfo` | 83 | 0 | 4 |  |
| `VerkaufBelegekopfRawInfo` | 187 | 21 | 0 |  |
| `VerkaufBelegepositionenRawInfo` | 151 | 45 | 0 |  |
| `VerkaufBelegekopfInfo` | 49 | 0 | 16 |  |
| `VerkaufPositionenAllInfo` | 142 | 0 | 24 |  |
| `VorVerkaufBelegeInfo` | 49 | 0 | 16 |  |
| `VerkaufBelegeMitBasisPosMitStckNurElementeInfo` | 57 | 0 | 16 |  |
| `VerkaufKommissionierungPicklistenItems` | 69 | 0 | 8 |  |
| `EinkaufBelegekopfInfo` | 50 | 0 | 16 |  |
| `EinkaufPositionenAllInfo` | 134 | 0 | 24 |  |
| `EinkaufPositionenInfo` | 91 | 0 | 24 |  |
| `LagerorteInfo` | 29 | 2 | 0 |  |
| `VerkaufInfoLinq` | 70 | 0 | 24 |  |
| `VerkaufPositionenInfo` | 93 | 0 | 24 |  |
| `VerkaufpositionenMitBasisPosMitStckNurElementeInfo` | 110 | 0 | 24 |  |
| `VorVerkaufPositionenInfo` | 92 | 0 | 24 |  |
| `ArtikelGruppen` | 11 | 1 | 0 |  |
| `Artikel` | 264 | 154 | 0 |  |
| `ArtikelVarianten` | 61 | 8 | 0 |  |
| `PpsArtikelVariantenInfo` | 141 | 0 | 8 | KHKArtikel, KHKArtikelVarianten, KHKVariantenAuspraegungen, KHKArtikelgruppen, KHKLagerplaetze, KHKLagerorte |
| `PpsArtikelVariantenMaterialRessourcenFilteredInfo` | 133 | 0 | 8 | KHKArtikelVarianten, KHKPpsRessourcenPositionen |
| `PpsRessourcenPositionenFilteredInfo` | 76 | 0 | 8 | KHKPpsRessourcenPositionen |
| `PpsBereitstellungsBelegeGroupInfo` | 28 | 5 | 4 | KHKPpsFaBelege |
| `PpsFaBelegeBereitstellungInfo` | 66 | 3 | 4 | KHKPpsFaBelege, KHKLagerplaetze, KHKLagerorte |
| `PpsFaBelegePositionenBereitstellungInfo` | 114 | 3 | 4 | KHKPpsFaBelegePositionen, KHKLagerplaetze, KHKLagerorte |
| `PpsFaBelegePositionenArbeitsgangInfo` | 73 | 0 | 4 | KHKPpsFaBelegePositionen, KHKPpsFaBelegeArbeitsgaenge |
| `PpsFaBelegeGroupInfo` | 28 | 5 | 4 | KHKPpsFaBelege |
| `PpsFaBelegeInfo` | 67 | 3 | 4 | KHKPpsFaBelege, KHKLagerplaetze, KHKLagerorte |
| `PpsFaBelegePositionenInfo` | 120 | 3 | 4 | KHKPpsFaBelegePositionen, KHKLagerplaetze, KHKLagerorte, KHKPpsArbeitsplaetze, KHKPpsArbeitsgaenge |
| `PpsFaBelegeSearchByPosOrAGPosInfo` | 35 | 2 | 8 | KHKPpsFaBelege, KHKPpsFaBelegePositionen |
| `PpsFaBelegeSmallArbeitsgangInfo` | 80 | 4 | 4 | KHKPpsFaBelegePositionen, KHKPpsArbeitsgaenge |
| `PpsFaBelegeSmallBereitstellungInfo` | 68 | 4 | 4 | KHKPpsFaBelegePositionen, KHKPpsBereitstellungen |
| `PpsFaBelegeSmallInfo` | 68 | 4 | 4 | KHKPpsFaBelegePositionen |
| `PpsFaBelegeVkBelegSucheInfo` | 54 | 3 | 4 | KHKPpsFaBelege, KHKVKBelege |

## L5 – PpsRessourcenListenInfo (Stücklisten-Kopf, Artikel 86003871)

**Rechte laut Schema:** Lesen ja · Anlegen nein · Ändern nein · Löschen nein

| Feld | Typ | Länge | Schlüssel | in den Daten | Beispiel | Benutzerfeld |
|---|---|---|---|---|---|---|
| `Mandant` | short |  | ja | ja | 1 | nein |
| `Artikelnummer` | string |  |  | ja | 86003871 | nein |
| `Variante` | string |  |  | ja | 1 | nein |
| `AuspraegungID` | integer |  |  | ja | 0 | nein |
| `Bezeichnung1` | string |  |  | ja | Waermebruecke 112-kpl_Basic/ComfortLine | nein |
| `Bezeichnung2` | string |  |  | ja | Basic/ComfortLine 112 | nein |
| `Matchcode` | string |  |  | ja | Waermebruecke 112-kpl_Basic/ComfortLine | nein |
| `Memo` | string |  |  | ja |  | nein |
| `Zeichnungsnummer` | string |  |  | ja |  | nein |
| `VonMenge` | decimal |  |  | ja | 0 | nein |
| `BisMenge` | decimal |  |  | ja | 0 | nein |
| `VonDatum` | dateTime |  |  | ja |  | nein |
| `BisDatum` | dateTime |  |  | ja |  | nein |
| `IstOffen` | boolean |  |  | ja | 0 | nein |
| `Klassennummer` | string |  |  | nein |  | nein |
| `IstFremdfertigung` | boolean |  |  | ja | 0 | nein |
| `KundenKto` | string |  |  | ja |  | nein |
| `VariantenID` | integer |  |  | ja | 0 | nein |
| `IstDefault` | short |  |  | ja | 0 | nein |
| `Preisfindung` | string |  |  | ja |  | nein |
| `Variantengruppe` | string |  |  | ja |  | nein |
| `Menge` | decimal |  |  | ja |  | nein |
| `VKPreis` | decimal |  |  | ja | (Betrag) | nein |
| `Fertigdatum` | dateTime |  |  | ja | 01/01/1900 00:00:00 | nein |
| `LieferBelID` | integer |  |  | ja | 0 | nein |
| `Status` | string |  |  | ja | G | nein |
| `StatIndex` | short |  |  | ja | 0 | nein |
| `DimText` | string |  |  | ja |  | nein |
| `Produktionszeit` | short |  |  | ja | 0 | nein |
| `UmrechnungsfaktorPZ` | decimal |  |  | ja | 1 | nein |
| `Timestamp` | base64Binary |  |  | ja | AAAAADxqUDU= | nein |
| `Sprache` | string |  |  | ja |  | nein |
| `PpsRessourcenPositionenResolvedInfo` | Kind-Liste (child) | | | | | |
| `SpecificUserfields` | Kind-Liste (child) | | | | | |

Felder im Schema: 34 (davon Benutzerfelder USER_: 0)

**Benutzerfelder Kopf** (`SpecificUserfields`):

| DatafieldName | Typ | Länge | Wert (86003871) |
|---|---|---|---|
| `USER_VKZuschlag` | decimal | 8 | leer |

## L5 – PpsRessourcenPositionenResolvedInfo (Stücklisten-Positionen)

**Rechte laut Schema:** Lesen ja · Anlegen nein · Ändern nein · Löschen nein

| Feld | Typ | Länge | Schlüssel | in den Daten | Beispiel | Benutzerfeld |
|---|---|---|---|---|---|---|
| `Mandant` | short |  | ja | ja | 1 | nein |
| `ArtikelnummerUBG` | string |  |  | ja | 86003870 | nein |
| `AuspraegungIDUBG` | integer |  |  | ja | 0 | nein |
| `StatIndexUBG` | short |  |  | ja | 0 | nein |
| `Ebene` | integer |  |  | ja | 1 | nein |
| `RowGuid` | string |  |  | ja | c956c600-919d-4a4a-a104-0d91138bbbe6 | nein |
| `RowGuidParent` | string |  |  | ja | 7f8231d1-9f70-4b08-8e66-40885595fa75 | nein |
| `BelPosID` | integer |  |  | ja | 31860 | nein |
| `BelPosIdParent` | integer |  |  | ja | 0 | nein |
| `Artikelnummer` | string |  |  | ja | 86003871 | nein |
| `Variante` | string |  |  | ja | 1 | nein |
| `AuspraegungIDKopf` | integer |  |  | ja | 0 | nein |
| `Position` | short |  |  | ja | 1 | nein |
| `Ressourcetyp` | string |  |  | ja | MA | nein |
| `Ressourcenummer` | string |  |  | ja | 86003870 | nein |
| `Arbeitsgangnummer` | string |  |  | ja |  | nein |
| `Matchcode` | string |  |  | ja | Schutzschlauch DM6 L=600mm_Basic/Comfort… | nein |
| `Dimensionstext` | string |  |  | ja | Schutzschlauch DM6 L=600mm_Basic/Comfort… | nein |
| `Menge` | decimal |  |  | ja | 1 | nein |
| `MengeNKS` | integer |  |  | ja | 0 | nein |
| `MengeBerechnung` | string |  |  | ja |  | nein |
| `MengeDimensionen` | string |  |  | ja |  | nein |
| `Umrechnungsfaktor` | decimal |  |  | ja | 1 | nein |
| `IstEinmalmenge` | boolean |  |  | ja | 0 | nein |
| `Ruestzeittext` | string |  |  | ja |  | nein |
| `Ruestzeit` | decimal |  |  | ja | 0 | nein |
| `Stueckzeittext` | string |  |  | ja | 05,00,00 | nein |
| `Stueckzeit` | decimal |  |  | ja | 0 | nein |
| `Pufferzeittext` | string |  |  | ja |  | nein |
| `Pufferzeit` | decimal |  |  | ja | 0 | nein |
| `Ueberlappungzeittext` | string |  |  | ja |  | nein |
| `Ueberlappungzeit` | decimal |  |  | ja | 0 | nein |
| `Ueberlappungmenge` | decimal |  |  | ja | 0 | nein |
| `Preis` | decimal |  |  | ja | (Betrag) | nein |
| `Lagermengeneinheit` | string |  |  | ja | Stk | nein |
| `Transportzeittext` | string |  |  | ja |  | nein |
| `Transportzeit` | decimal |  |  | ja | 0 | nein |
| `AusfuehrungsArtAG` | string |  |  | ja |  | nein |
| `AuspraegungID` | integer |  |  | ja | 0 | nein |
| `PlatzID` | integer |  |  | ja | 0 | nein |
| `IstGeloescht` | boolean |  |  | ja | 0 | nein |
| `IstOffen` | boolean |  |  | ja | 0 | nein |
| `PositionExtern` | integer |  |  | ja | 0 | nein |
| `PositionExternPraefix` | string |  |  | ja |  | nein |
| `PositionExternSuffix` | string |  |  | ja |  | nein |
| `Materialteileart` | string |  |  | ja |  | nein |
| `Auspraegungstyp` | string |  |  | ja |  | nein |
| `Planzeittext` | string |  |  | ja |  | nein |
| `Planzeit` | decimal |  |  | ja |  | nein |
| `Ueberlappungtyp` | string |  |  | ja |  | nein |
| `Ueberlappungprozent` | decimal |  |  | ja | 0 | nein |
| `MengeNach` | decimal |  |  | ja |  | nein |
| `IstTextuebergabe` | boolean |  |  | ja | 0 | nein |
| `IstCadFixPos` | boolean |  |  | ja | 0 | nein |
| `PosVariante` | string |  |  | ja | 1 | nein |
| `PositionCAD` | string |  |  | ja |  | nein |
| `Anzahl` | integer |  |  | ja | 0 | nein |
| `FormelID` | string |  |  | ja |  | nein |
| `DIM1` | decimal |  |  | ja | 0 | nein |
| `DIM2` | decimal |  |  | ja | 0 | nein |
| `DIM3` | decimal |  |  | ja | 0 | nein |
| `Fremdfertiger` | string |  |  | ja |  | nein |
| `SollEinzelpreis` | decimal |  |  | ja | (Betrag) | nein |
| `Leistungsgrad` | decimal |  |  | ja | 0 | nein |
| `RessourceTypAg` | short |  |  | ja | 0 | nein |
| `RessourceNummerAg` | string |  |  | ja |  | nein |
| `PositionExternAnzeige` | string |  |  | ja | 10 | nein |
| `KapazAnzahlMax` | short |  |  | ja | 0 | nein |
| `MindestmengeAG` | decimal |  |  | ja | 0 | nein |
| `SpecificUserfields` | Kind-Liste (child) | | | | | |

Felder im Schema: 70 (davon Benutzerfelder USER_: 0)

**Benutzerfelder je Position** (`SpecificUserfields`, an allen 17 Positionen dieselben 5):

| DatafieldName | Typ | Länge | Beispiel |
|---|---|---|---|
| `USER_VKPreis` | decimal | 8 | leer |
| `USER_VKEinzelpreis` | decimal | 8 | leer |
| `USER_EPDMVERSION` | short | 2 | leer |
| `USER_AUSCAD` | boolean | 2 | -1 |
| `USER_Positionsnummer` | string | 10 | leer |

Positionsbaum 86003871: Ebene 1 = 10 × Material (6 Unterbaugruppen 8600387x/8600385x mit `ArtikelnummerUBG`, 4 Zukaufteile) + 1 × AG
`999`; Ebene 2 = 6 Material-Positionen unter den Unterbaugruppen. Auffällig: Auf Ebene 2 tragen alle Positionen `Position = 1`, und
`00717154` erscheint zweimal mit gleicher Menge – Zuordnung nur über `RowGuidParent`/`BelPosIdParent`, nicht über `Position`.

## L6/L7 – Schreibbarkeit Stückliste (ohne Schreibversuch)

| Ressource / Operation | Vertrag | canGet | canPost | canPut | canDelete | Bemerkung |
|---|---|---|---|---|---|---|
| `PpsRessourcenListenInfo` | …Specific | ja | nein | nein | nein | Kind-Elemente tragen canPost/canPut, die Ressource nicht → kein Anlege-/Änderungs-Endpunkt |
| `PpsRessourcenPositionenResolvedInfo` | …Specific | ja | nein | nein | nein | dito |
| `PpsRessourcenPositionenInfo` | CommonWawiServicesResources | ja | nein | nein | nein | eine Ebene, 65 Felder (Tabelle unten) |
| `ArtikelStueckliste` | CommonWawiServicesResources | ja | **ja** | **ja** | nein | Warenwirtschafts-Stückliste (Set/Stückliste am Artikel, 8 Felder), **nicht** PPS; 86003871 hat keine (0 Zeilen) |
| `Artikel` → Kind `ArtikelStueckliste` | CommonWawiServicesResources | ja | ja | ja | nein | `Artikel` selbst canPost/canPut |
| Service-Operationen (L7) | CommonWawiServices | | | | | Treffer für „Ressource/Stueck/Fertigungsauftrag/Userfield“: nur `FertigungsauftragService`, `UpdateFertigungsauftragEntityService`, `UpdateFertigungsauftragPositionEntityService` |

Ganze Verträge: `CommonWawiServicesResources` 165 Ressourcen, davon mit `canPost="true"` nur Adressen/Ansprechpartner/Kontokorrent/
abw. Lieferanschriften, `AnsprechpartnerZuordnung` (auch Delete), `ArtikelBezeichnung`, `ArtikelStueckliste`, `ArtikelGruppen`, `Artikel`,
`ArtikelVarianten`, `PpsMitarbeiterKommtGeht` (auch Delete), `WarenausgangPositionenCountInfo`, `WareneingangPositionenCountInfo`.

**Befund:** Die **PPS-Stückliste (Ressourcenliste) ist über SData nicht schreibbar** – weder als Ressource noch als Service-Operation.
Schreibbar ist nur die WaWi-`ArtikelStueckliste`. Ein Schreibweg für Ressourcenlisten braucht DPS (Frage unten).

**L7 – Request-Felder der drei FA-Operationen**

- `FertigungsauftragService` (Anlage): `FaBelegNeu` {Bearbeiter, Artikelnummer, AuspraegungsHandle, Variante, Sollmenge, SollStartTermin,
  SollEndTermin, Terminierungsart, Projekt, Kostentraeger, Kostenstelle, Zusatztext, Belegjahr, KapazitaetsBelegungPruefen,
  LosgroessenBeachten, UnterbaugruppenErzeugen, SofortEinlasten, **FaBelegUserFields[]**, **GenericUserfields[]** {DatafieldName,
  DatafieldValue, DatafieldType}, BacklogPrimaryKeys[]}, `PositionenMaterial[]` {Handle, PositionHandle, RessourceTyp, RessourceNummer,
  Artikelnummer, AuspraegungsHandle, Menge, FaBelegPositionUserfields[] (leerer Typ), GenericUserfields[]}, `PositionenArbeitsgang[]`
  {… RessourceTyp, RessourceNummer, Menge …}. Antwort: Handle, ResultNumber/-Text, ErrorText, Positionen-Ergebnisse.
- `FaBelegUserfield` = **34 AKE-FA-Benutzerfelder**: USER_Auszug, USER_Bedienungsanleitung, USER_Bodenplatte, USER_Dichtung, USER_Dimmer,
  USER_DPSBSBeistellungBeendet (bool), USER_DPSBSBereitstellungBeendet (bool), USER_DPSBSMDEFreigabe (bool), USER_Elektronik,
  USER_ElektronikSteuerung, USER_FaSN, USER_Frontscheibe, USER_Fuellmenge, USER_Gasdruckdaempfer, USER_Glasdeckel,
  USER_HeizplatteZBmitHeizung, USER_Kaeltemittel, USER_KBeleuchtung, USER_KktTauwasser, USER_Luefter, USER_Maschine,
  USER_Programmieranleitung, USER_Quarzstrahler, USER_Rahmenheizung, USER_SchiebeFluegeltueren, USER_Seitenscheiben,
  USER_Sonderausfuehrung, USER_Ventil, USER_Verdampfer, USER_VermerkProduktion, USER_Vorschaltgeraet, USER_Waermestrahler,
  USER_WBeleuchtung, USER_ZBohneHeizung, USER_Zusatzkomponenten, USER_Zwischenborde (alle string, außer den drei DPSBS).
- `UpdateFertigungsauftragEntityService` {UseSemaphore, BelegHandle, EntityFieldName, NewValue} und
  `UpdateFertigungsauftragPositionEntityService` {… BelegPositionHandle …}: ein Feld je Aufruf per Name – vermutlich der Weg, einzelne
  FA-(Benutzer-)Felder zu ändern (ungeprüft, kein Schreibversuch).

**Ressourcenpositionen eine Ebene – PpsRessourcenPositionenInfo** (`where=Artikelnummer eq '86003871'&orderBy=BelPosID&count=5`, 200, 11 Treffer):

**Rechte laut Schema:** Lesen ja · Anlegen nein · Ändern nein · Löschen nein

> Schema-Doku: PPS Ressourcenpositionen (Detaillierte Ressourceninformationen zu PPS-Positionen). Enthält Mandant, Artikelnummer, Ressourcen, Arbeitsgänge, Mengen. Datenquelle: vwDPSBS_CWS_PPS_RessourcenPositionenInfo. Enthaltene Tabellen: KHKPpsRessourcen, KHKPpsArbeitsgaenge, KHKPpsArbeitsplaetze. Wichtige Spalten: Mandant, Artikelnummer, RessourceNummer, RessourceTyp, ArbeitsgangNummer, Menge. Typische Verwendung: Ressourcenübersicht für PPS-Produktion.

| Feld | Typ | Länge | Schlüssel | in den Daten | Beispiel | Benutzerfeld |
|---|---|---|---|---|---|---|
| `ArtikelBezeichnung` | Kind-Liste (child) | | | | | |
| `IstFertigungsartikel` | boolean | 2 |  | ja | -1 | nein |
| `Artikelnummer` | string | 31 |  | ja | 86003871 | nein |
| `Variante` | string | 4 |  | ja | 1 | nein |
| `AuspraegungIDKopf` | integer | 4 |  | ja | 0 | nein |
| `Mandant` | short | 2 | ja | ja | 1 | nein |
| `Position` | short | 2 |  | ja | 1 | nein |
| `Ressourcetyp` | string | 2 |  | ja | MA | nein |
| `Ressourcenummer` | string | 31 |  | ja | 86003870 | nein |
| `Arbeitsgangnummer` | string | 31 |  | ja |  | nein |
| `Matchcode` | string | 50 |  | ja | Schutzschlauch DM6 L=600mm_Basic/Comfort… | nein |
| `Dimensionstext` | string | 2147483647 |  | ja | Schutzschlauch DM6 L=600mm_Basic/Comfort… | nein |
| `Menge` | decimal | 8 |  | ja | 1 | nein |
| `MengeNKS` | integer | 4 |  | ja | 0 | nein |
| `MengeBerechnung` | string | 50 |  | ja |  | nein |
| `MengeDimensionen` | string | 50 |  | ja |  | nein |
| `Umrechnungsfaktor` | decimal | 8 |  | ja | 1 | nein |
| `IstEinmalmenge` | boolean | 2 |  | ja | 0 | nein |
| `Ruestzeittext` | string | 13 |  | ja |  | nein |
| `Ruestzeit` | decimal | 8 |  | ja | 0 | nein |
| `Stueckzeittext` | string | 13 |  | ja |  | nein |
| `Stueckzeit` | decimal | 8 |  | ja | 0 | nein |
| `Pufferzeittext` | string | 13 |  | ja |  | nein |
| `Pufferzeit` | decimal | 8 |  | ja | 0 | nein |
| `Ueberlappungzeittext` | string | 13 |  | ja |  | nein |
| `Ueberlappungzeit` | decimal | 8 |  | ja | 0 | nein |
| `Ueberlappungmenge` | decimal | 8 |  | ja | 0 | nein |
| `Preis` | decimal | 8 |  | ja | (Betrag) | nein |
| `Lagermengeneinheit` | string | 5 |  | ja | Stk | nein |
| `Transportzeittext` | string | 13 |  | ja |  | nein |
| `Transportzeit` | decimal | 8 |  | ja | 0 | nein |
| `AusfuehrungsArtAG` | string | 1 |  | ja |  | nein |
| `AuspraegungID` | integer | 4 |  | ja | 0 | nein |
| `PlatzID` | integer | 4 |  | ja | 0 | nein |
| `BelPosID` | integer | 4 | ja | ja | 31860 | nein |
| `IstGeloescht` | boolean | 2 |  | ja | 0 | nein |
| `IstOffen` | boolean | 2 |  | ja | 0 | nein |
| `PositionExtern` | integer | 4 |  | ja | 0 | nein |
| `PositionExternPraefix` | string | 9 |  | ja |  | nein |
| `PositionExternSuffix` | string | 9 |  | ja |  | nein |
| `Materialteileart` | string | 3 |  | ja |  | nein |
| `Auspraegungstyp` | string | 20 |  | ja |  | nein |
| `Planzeit` | decimal | 8 |  | ja |  | nein |
| `Planzeittext` | string | 13 |  | ja |  | nein |
| `Ueberlappungtyp` | string | 1 |  | ja |  | nein |
| `Ueberlappungprozent` | decimal | 8 |  | ja | 0 | nein |
| `MengeNach` | decimal | 8 |  | ja |  | nein |
| `IstTextuebergabe` | short | 2 |  | ja | 0 | nein |
| `IstCadFixPos` | boolean | 2 |  | ja | 0 | nein |
| `PosVariante` | string | 4 |  | ja | 1 | nein |
| `PositionCAD` | string | 10 |  | ja |  | nein |
| `Anzahl` | integer | 4 |  | ja | 0 | nein |
| `FormelID` | string | 20 |  | ja |  | nein |
| `DIM1` | decimal | 8 |  | ja | 0 | nein |
| `DIM2` | decimal | 8 |  | ja | 0 | nein |
| `DIM3` | decimal | 8 |  | ja | 0 | nein |
| `Fremdfertiger` | string | 20 |  | ja |  | nein |
| `SollEinzelpreis` | decimal | 8 |  | ja | (Betrag) | nein |
| `Leistungsgrad` | decimal | 8 |  | ja | 0 | nein |
| `RessourceTypAg` | short | 2 |  | ja | 0 | nein |
| `RessourceNummerAg` | string | 31 |  | ja |  | nein |
| `PositionExternAnzeige` | string | 11 |  | ja |  | nein |
| `KapazAnzahlMax` | short | 2 |  | ja | 0 | nein |
| `MindestmengeAG` | decimal | 8 |  | ja | 0 | nein |
| `Timestamp` | base64Binary | 8 |  | ja | AAAAAAYpXUA= | nein |

Felder im Schema: 65 (davon Benutzerfelder USER_: 0)

**ArtikelStueckliste** (`orderBy=Artikelnummer&count=3`, 200):

**Rechte laut Schema:** Lesen ja · Anlegen ja · Ändern ja · Löschen nein

| Feld | Typ | Länge | Schlüssel | in den Daten | Beispiel | Benutzerfeld |
|---|---|---|---|---|---|---|
| `Artikelnummer` | Kind-Liste (parent) | | | | | |
| `Stueckliste` | string | 31 | ja | ja | 00000003 | nein |
| `Element` | string | 31 | ja | ja | ZZ123456 | nein |
| `Mandant` | short | 2 | ja | ja | 1 | nein |
| `AuspraegungID` | integer | 4 | ja | ja | 0 | nein |
| `Menge` | decimal | 8 |  | ja | 1 | nein |
| `Sortierung` | integer | 4 |  | ja | 0 | nein |
| `Timestamp` | base64Binary | 8 |  | ja | AAAAAD0+YhY= | nein |

Felder im Schema: 8 (davon Benutzerfelder USER_: 0)

## L8 – RealTimeData: zu welcher Sage-Tabelle gehören die AKE-FA-Felder

`dtsFaBelegsuche.Sage.PPS?where=BelID eq 835` (200): 90 Felder, davon 36 `KHKPpsFaBelege_USER_…` → die AKE-FA-Benutzerfelder liegen in
**`KHKPpsFaBelege`** (Präfix = Tabellenname). Dieselben 34 + 2 DPSBS-Felder wie im `FaBelegUserfield`-Typ (L7). Bei FA 835 belegt:
USER_Maschine `-`, USER_Kaeltemittel `-`, USER_Ventil `-`, Rest leer bzw. 0.

**Rechte laut Schema:** Lesen ja · Anlegen nein · Ändern nein · Löschen nein

| Feld | Typ | Länge | Schlüssel | in den Daten | Beispiel | Benutzerfeld |
|---|---|---|---|---|---|---|
| `AenderungsIndex` | integer |  |  | ja | -1 | nein |
| `Aktiv` | integer |  |  | ja | -1 | nein |
| `AngelegtAm` | date |  |  | ja | 02/02/2026 14:17:32 | nein |
| `AngelegtVon` | string |  |  | ja | (Benutzername) | nein |
| `ArchiviertAm` | date |  |  | ja |  | nein |
| `ArchiviertVon` | string |  |  | ja |  | nein |
| `Artikelnummer` | string |  |  | ja | 86003871 | nein |
| `AusfuehrungsArt` | string |  |  | ja |  | nein |
| `AuspraegungID` | integer |  |  | ja | 0 | nein |
| `Belegjahr` | integer |  |  | ja | 2027 | nein |
| `Belegnummer` | integer |  |  | ja | 0 | nein |
| `BelID` | integer |  |  | ja | 835 | nein |
| `Bestellmenge` | decimal |  |  | ja | 0 | nein |
| `Bezeichnung1` | string |  |  | ja | Waermebruecke 112-kpl_Basic/ComfortLine | nein |
| `Bezeichnung2` | string |  |  | ja | Basic/ComfortLine 112 | nein |
| `DimText` | string |  |  | ja |  | nein |
| `Fertigungmenge` | decimal |  |  | ja | 1 | nein |
| `Fertigungsart` | string |  |  | ja | EF | nein |
| `GeaendertAm` | date |  |  | ja | 02/02/2026 14:36:24 | nein |
| `GeaendertVon` | string |  |  | ja | (Benutzername) | nein |
| `IstBeendet` | integer |  |  | ja | 0 | nein |
| `IstBegonnen` | integer |  |  | ja | 0 | nein |
| `IstBestelltTermin` | date |  |  | ja |  | nein |
| `IstCadFreigegeben` | integer |  |  | ja | 0 | nein |
| `IstEingelastet` | integer |  |  | ja | -1 | nein |
| `IstEinlastungTermin` | date |  |  | ja | 02/02/2026 00:00:00 | nein |
| `Istendtermin` | date |  |  | ja |  | nein |
| `IstGedruckt` | integer |  |  | ja | -1 | nein |
| `IstGeliefertTermin` | date |  |  | ja |  | nein |
| `IstGesperrt` | integer |  |  | ja | 0 | nein |
| `Istmenge` | decimal |  |  | ja | 0 | nein |
| `IstReaktiviert` | integer |  |  | ja | 0 | nein |
| `Iststarttermin` | date |  |  | ja |  | nein |
| `KapBelegungPruefen` | integer |  |  | ja | -1 | nein |
| `Kostenstelle` | string |  |  | ja |  | nein |
| `Kostentraeger` | string |  |  | ja |  | nein |
| `Matchcode` | string |  |  | ja | Waermebruecke 112-kpl_Basic/ComfortLine | nein |
| `Memo` | integer |  |  | ja | 0 | nein |
| `PKonfigID` | string |  |  | ja |  | nein |
| `PKonfigPosID` | integer |  |  | ja | 0 | nein |
| `PlatzID` | integer |  |  | ja | 2555 | nein |
| `Projekt` | string |  |  | ja |  | nein |
| `Sollendtermin` | date |  |  | ja | 02/04/2026 00:00:00 | nein |
| `Sollmenge` | decimal |  |  | ja | 1 | nein |
| `Sollstarttermin` | date |  |  | ja | 02/03/2026 08:45:00 | nein |
| `Sprache` | string |  |  | ja |  | nein |
| `StrukturID` | integer |  |  | ja | 835 | nein |
| `Terminierung` | integer |  |  | ja | 0 | nein |
| `Variante` | string |  |  | ja | 1 | nein |
| `VerursacherTyp` | string |  |  | ja | MAN | nein |
| `Zeichnungsnummer` | string |  |  | ja |  | nein |
| `Zusatztext` | string |  |  | ja |  | nein |
| `DezimalstellenLager` | integer |  |  | ja | 0 | nein |
| `UsageForRowInternal` | integer |  |  | ja | 0 | nein |
| `KHKPpsFaBelege_USER_Maschine` | string |  |  | ja | - | ja |
| `KHKPpsFaBelege_USER_Auszug` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Bedienungsanleitung` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_DPSBSBeistellungBeendet` | boolean |  |  | ja | 0 | ja |
| `KHKPpsFaBelege_USER_KBeleuchtung` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_WBeleuchtung` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_DPSBSBereitstellungBeendet` | boolean |  |  | ja | 0 | ja |
| `KHKPpsFaBelege_USER_Bodenplatte` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Dichtung` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Dimmer` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Elektronik` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_ElektronikSteuerung` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_FaSN` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_DPSBSMDEFreigabe` | boolean |  |  | ja | 0 | ja |
| `KHKPpsFaBelege_USER_Frontscheibe` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Fuellmenge` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Gasdruckdaempfer` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Glasdeckel` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_HeizplatteZBmitHeizung` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Kaeltemittel` | string |  |  | ja | - | ja |
| `KHKPpsFaBelege_USER_Luefter` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Programmieranleitung` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Quarzstrahler` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Rahmenheizung` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_SchiebeFluegeltueren` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Seitenscheiben` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Sonderausfuehrung` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_KktTauwasser` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Ventil` | string |  |  | ja | - | ja |
| `KHKPpsFaBelege_USER_Verdampfer` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_VermerkProduktion` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Vorschaltgeraet` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Waermestrahler` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Zusatzkomponenten` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_ZBohneHeizung` | string |  |  | ja |  | ja |
| `KHKPpsFaBelege_USER_Zwischenborde` | string |  |  | ja |  | ja |

Felder im Schema: 90 (davon Benutzerfelder USER_: 36)

`dtsAbfLOGFertigungsauftraege.100036671.AKE_InterneFA` (AKE-eigene Abfrage, Feldliste aus dem Schema, 64 Felder): Quelle
`KHKIFAuftraege` RIGHT JOIN `KHKVKBelegePositionen` (`user_fabelid`) / `KHKVKBelege` / `KHKVKBelegeZKD`. Felder: BelID, MengeBasis(+Einheit,
NKS), Bestellwoche/-termin, Wiederbeschaffungszeit, Lieferwoche/-termin, Artikelnummer, AuspraegungID, Artikelgruppe, Kommissionsnummer,
Bezeichnung1/2, DispoStatus, Haupt-/Vaterartikelgruppe, **USER_FAFixtermin, USER_FAStatus, USER_FAMemo, USER_FAMemo2, USER_Kaeltemittel,
USER_Ausfuehrung, USER_Maschine, USER_Ventil, USER_FASollmenge, USER_FAReferenznr, USER_FAReferenzzeichen, USER_FAKundenArtNr,
USER_FAKundenBez, USER_FAMemoZusatz, USER_ShortCust, USER_BelID2, USER_Versand, USER_Ladresse, USER_FAMemoBAK, USER_Beleg, USER_Kto,
USER_Ladetermin, USER_IstVerladen, USER_MengeVerladen, USER_Verladezeit, USER_VerladeMA**, Belegjahr/-nummer, A0/A1-Adressfelder,
Versand, Bearbeiter, Lieferbedingung, Zahlungskond, InkassoArt, KtoMatchcode, Beleg. Weitere Quellen derselben Familie:
`dtsAbfLOGFertigungsauftragDetails.100036671.AKE_InterneFA`, `dtsUnvollstLieferung.100036671.AKE_InterneFA`.

## P-W1 – Was ist der Werkstattauftrag an AKE

Lesend per SQL (Windows-Anmeldung) auf AKESQL20. `vw_AKE_Kommissionierung_WAListe` liegt in `ake_TEST2026` (und `ake`),
`vw_IDEAL_AKE_WMS_FAZusatzinformationen` **nur in `ake`** (Produktiv-DB; dort nur `OBJECT_DEFINITION` gelesen). Viewtexte gehören nach
`SQL/sage-views/` (Folgearbeit Koordinator, im Dev-Lauf nicht ins Repo gelegt). Kern:

- `vw_AKE_Kommissionierung_WAListe`: `KHKIFAuftraege ia` → `[WA Nummer]` = `ia.BelID`, Stückzahl = `ia.USER_FASollmenge`,
  Fertigungstermin = `ia.Liefertermin`; LEFT JOIN `KHKVKBelegePositionen vbp ON vbp.USER_FABelId = ia.BelID` und `KHKVKBelege vb`
  (Kunde = `vb.A0Matchcode`, `vbp.Liefertermin`). Filter: `vbp.Artikelgruppe IN ('K003','K004','K005','K006')`, `ia.BelID > 2600000`,
  Bezeichnung1 LIKE ukw/green…/cool bottle/gastro/euro/tropical, `vb.Belegart = 'Auftragsbestätigung'`. Am Test 855 Zeilen, höchste WA 2646002.
- `vw_IDEAL_AKE_WMS_FAZusatzinformationen`: WAListe LEFT JOIN `KHKIFAuftraege` → Kältemittel = `USER_Kaeltemittel`, Ventil = `USER_Ventil`,
  „Ausführung E/Z“ = `USER_Ausfuehrung`, Maschine = `USER_Maschine`, Status aus `USER_FAStatus` (1 in Produktion, 2 begonnen, 4 + `USER_IstVerladen`
  0 verpackt / -1 abgeholt, sonst unbekannt).
- `KHKIFAuftraege` (157.905 Zeilen, BelID 1 … 2.646.002) und `KHKIFAuftraegeStuecklisten` (BelPosStID, BelID, Artikelnummer, AuspraegungID,
  MengeFaktor, MengeBasis(+Einheit, NKS), Sortierung, Timestamp – keine USER_).

**Spalten `KHKIFAuftraege`** (SQL `sys.columns`, `ake_TEST2026`):

| Spalte | Typ | Länge | Benutzerfeld |
|---|---|---|---|
| `BelID` | int | 4 | nein |
| `Mandant` | smallint | 2 | nein |
| `MengeBasis` | money | 8 | nein |
| `MengeBasisEinheit` | varchar | 5 | nein |
| `MengeBasisNKS` | smallint | 2 | nein |
| `Bestellwoche` | int | 4 | nein |
| `Bestelltermin` | datetime | 8 | nein |
| `Wiederbeschaffungszeit` | smallint | 2 | nein |
| `Lieferwoche` | int | 4 | nein |
| `Liefertermin` | datetime | 8 | nein |
| `Artikelnummer` | varchar | 31 | nein |
| `AuspraegungID` | int | 4 | nein |
| `Artikelgruppe` | varchar | 15 | nein |
| `Kommissionsnummer` | varchar | 20 | nein |
| `Bezeichnung1` | varchar | 50 | nein |
| `Bezeichnung2` | varchar | 50 | nein |
| `DispoStatus` | KHKBoolean | 2 | nein |
| `Hauptartikelgruppe` | varchar | 15 | nein |
| `Vaterartikelgruppe` | varchar | 15 | nein |
| `Timestamp` | timestamp | 8 | nein |
| `USER_FAFixtermin` | KHKBoolean | 2 | ja |
| `USER_FAStatus` | int | 4 | ja |
| `USER_FAMemo` | text | 16 | ja |
| `USER_FAMemo2` | text | 16 | ja |
| `USER_Kaeltemittel` | varchar | 50 | ja |
| `USER_Ausfuehrung` | varchar | 50 | ja |
| `USER_Maschine` | varchar | 50 | ja |
| `USER_Ventil` | varchar | 50 | ja |
| `USER_FASollmenge` | money | 8 | ja |
| `USER_FAReferenznr` | varchar | 50 | ja |
| `USER_FAReferenzzeichen` | varchar | 50 | ja |
| `USER_FAKundenArtNr` | varchar | 50 | ja |
| `USER_FAKundenBez` | varchar | 50 | ja |
| `USER_FAMemoZusatz` | text | 16 | ja |
| `USER_ShortCust` | text | 16 | ja |
| `USER_BelID2` | int | 4 | ja |
| `USER_Versand` | varchar | 10 | ja |
| `USER_Ladresse` | varchar | 250 | ja |
| `USER_FAMemoBAK` | varchar | 250 | ja |
| `USER_Beleg` | varchar | 20 | ja |
| `USER_Kto` | varchar | 20 | ja |
| `USER_Ladetermin` | datetime | 8 | ja |
| `USER_IstVerladen` | KHKBoolean | 2 | ja |
| `USER_MengeVerladen` | money | 8 | ja |
| `USER_Verladezeit` | datetime | 8 | ja |
| `USER_VerladeMA` | varchar | 10 | ja |

## L9 – Werkstattauftrag über SData

`KHKIFAuftraege` kommt in **keinem** der Verträge `CommonWawiServicesResources`, `…Specific`, `CommonWawiServices` vor (Volltextsuche im
Schema: 0 Treffer); in `RealTimeData` nur als parametrisierte AKE-Abfragen (L8). Lesbar ist die **verknüpfte Auftragsposition**:
`VerkaufBelegepositionenRawInfo?where=USER_FABelId eq 2646002&orderBy=BelPosID&count=2` → 200, 2 Positionen (z. B. Artikel EU253711,
USER_Kaeltemittel R290, USER_Fuellmenge, USER_Kaelteleistung …). Die **WA-eigenen** Felder (USER_FAStatus, USER_Ausfuehrung, USER_Maschine,
USER_Ventil am WA, USER_IstVerladen, Ladetermin …) sind darüber **nicht** erreichbar.

**Rechte laut Schema:** Lesen ja · Anlegen nein · Ändern nein · Löschen nein

| Feld | Typ | Länge | Schlüssel | in den Daten | Beispiel | Benutzerfeld |
|---|---|---|---|---|---|---|
| `BelPosID` | integer | 4 | ja | ja | 1927384 | nein |
| `Mandant` | short | 2 | ja | ja | 1 | nein |
| `BelID` | integer | 4 |  | ja | 1625393 | nein |
| `VorPosID` | integer | 4 |  | ja | 413410 | nein |
| `Index` | integer | 4 |  | ja | 1 | nein |
| `Position` | string | 50 |  | ja | 1 | nein |
| `PosType` | short | 2 |  | ja | 1 | nein |
| `Status` | short | 2 |  | ja | 0 | nein |
| `Artikelnummer` | string | 31 |  | ja | EU253711 | nein |
| `AuspraegungID` | integer | 4 |  | ja | 0 | nein |
| `Artikelgruppe` | string | 15 |  | ja | K003 | nein |
| `Hauptartikelgruppe` | string | 15 |  | ja | AKE | nein |
| `Vaterartikelgruppe` | string | 15 |  | ja | AKE | nein |
| `StuecklistenTyp` | short | 2 |  | ja | 1 | nein |
| `StuecklistenAndruck` | boolean | 2 |  | ja | 0 | nein |
| `Nachweispflicht` | short | 2 |  | ja | 0 | nein |
| `Bezeichnung1` | string | 50 |  | ja | UKW 3-1-E R290 | nein |
| `Bezeichnung2` | string | 50 |  | ja | Umluftkühlwanne: eigengekühlt | nein |
| `Lieferwoche` | integer | 4 |  | ja | 202646 | nein |
| `Liefertermin` | dateTime | 8 |  | ja | 11/09/2026 00:00:00 | nein |
| `Steuercode` | short | 2 |  | ja | 1 | nein |
| `Steuerklasse` | short | 2 |  | ja | 1 | nein |
| `Erloescode` | short | 2 |  | ja | (Betrag) | nein |
| `Wareneinsatzcode` | short | 2 |  | ja | 0 | nein |
| `Warenbestandcode` | short | 2 |  | ja | 0 | nein |
| `Sachkonto` | string | 20 |  | ja |  | nein |
| `SachkontoWZ` | string | 20 |  | ja |  | nein |
| `SachkontoWB` | string | 20 |  | ja |  | nein |
| `Kostenstelle` | string | 20 |  | ja | (Betrag) | nein |
| `Kostentraeger` | string | 20 |  | ja | (Betrag) | nein |
| `Einzelpreis` | decimal | 8 |  | ja | (Betrag) | nein |
| `EinzelpreisNks` | short | 2 |  | ja | (Betrag) | nein |
| `EinzelpreisManuell` | boolean | 2 |  | ja | (Betrag) | nein |
| `RabattfaehigPos` | boolean | 2 |  | ja | (Betrag) | nein |
| `RabattfaehigArtikel` | boolean | 2 |  | ja | (Betrag) | nein |
| `Rabatt` | decimal | 8 |  | ja | (Betrag) | nein |
| `Rabattbetrag` | decimal | 8 |  | ja | (Betrag) | nein |
| `Gesamtpreis` | decimal | 8 |  | ja | (Betrag) | nein |
| `GesamtpreisIntern` | decimal | 8 |  | ja | (Betrag) | nein |
| `GesamtpreisInternEW` | decimal | 8 |  | ja | (Betrag) | nein |
| `IstGewaehrleistung` | boolean | 2 |  | ja | 0 | nein |
| `MittlererEK` | decimal | 8 |  | ja | 6028 | nein |
| `Roherloesberechnung` | short | 2 |  | ja | (Betrag) | nein |
| `Festpreis` | integer | 4 |  | ja | (Betrag) | nein |
| `Roherloes` | decimal | 8 |  | ja | (Betrag) | nein |
| `Menge` | decimal | 8 |  | ja | 3 | nein |
| `Mengeneinheit` | string | 5 |  | ja | Stk | nein |
| `MengeArt` | short | 2 |  | ja | 1 | nein |
| `MengeBasis` | decimal | 8 |  | ja | 3 | nein |
| `MengeBasiseinheit` | string | 5 |  | ja | Stk | nein |
| `MengeBasisNKS` | short | 2 |  | ja | 0 | nein |
| `MengeVKEinheit` | string | 5 |  | ja | Stk | nein |
| `MengeVKUmrechnung` | decimal | 8 |  | ja | 1 | nein |
| `MengeVKBerechnung` | string | 50 |  | ja |  | nein |
| `MengeVKDimensionen` | string | 50 |  | ja |  | nein |
| `MengeVKNKS` | short | 2 |  | ja | 0 | nein |
| `MengeVPEinheit` | string | 5 |  | ja |  | nein |
| `MengeVPUmrechnung` | decimal | 8 |  | ja | 1 | nein |
| `MengeVPNKS` | short | 2 |  | ja | 0 | nein |
| `Dimensionstext` | string | 2147483647 |  | ja |  | nein |
| `Langtext` | string | 2147483647 |  | ja | Multifunktionelle Umluftkühlwanne mit hö… | nein |
| `Preiseinheit` | integer | 4 |  | ja | (Betrag) | nein |
| `IstErledigt` | boolean | 2 |  | ja | 0 | nein |
| `Seitenumbruch` | boolean | 2 |  | ja | 0 | nein |
| `Provisionsfaehig` | boolean | 2 |  | ja | (Betrag) | nein |
| `Provisionssatz` | decimal | 8 |  | ja | (Betrag) | nein |
| `Provision` | decimal | 8 |  | ja | (Betrag) | nein |
| `Chargenpflicht` | short | 2 |  | ja | 0 | nein |
| `Einzelgewicht` | decimal | 8 |  | ja | 75 | nein |
| `Gesamtgewicht` | decimal | 8 |  | ja | 225 | nein |
| `MengeGPEinheit` | string | 5 |  | ja |  | nein |
| `MengeGPUmrechnung` | decimal | 8 |  | ja | 1 | nein |
| `GrundpreisBasis` | decimal | 8 |  | ja | (Betrag) | nein |
| `IstReserviert` | boolean | 2 |  | ja | 0 | nein |
| `Variantendruck` | short | 2 |  | ja | 0 | nein |
| `Artikelart` | short | 2 |  | ja | 0 | nein |
| `RessourcenRef` | string | 20 |  | ja |  | nein |
| `BildTan` | integer | 4 |  | ja | 0 | nein |
| `FibuTanAZ` | integer | 4 |  | ja | 0 | nein |
| `Lagerplatzzuordnung` | boolean | 2 |  | ja | 0 | nein |
| `SaveStatus` | integer | 4 |  | ja | 2 | nein |
| `ProjektEKBelPosID` | integer | 4 |  | ja | 0 | nein |
| `AngKalkPosId` | integer | 4 |  | ja | 0 | nein |
| `Kalkulation` | string | 20 |  | ja |  | nein |
| `PKonfigID` | string | 20 |  | ja |  | nein |
| `UrsprungsID` | integer | 4 |  | ja | 0 | nein |
| `ZubehoerKennung` | short | 2 |  | ja | 0 | nein |
| `ZubehoerText` | string | 2147483647 |  | ja |  | nein |
| `ZubehoerID` | integer | 4 |  | ja | 0 | nein |
| `Timestamp` | base64Binary | 8 |  | ja | AAAAAD2dArQ= | nein |
| `USER_FABelId` | integer | 4 |  | ja | 2646002 | ja |
| `Steuerperiode` | integer | 4 |  | ja | 2027011 | nein |
| `Artikelmemo` | string | 2147483647 |  | ja | Änderung Füllemgen von 0,100kg auf 0,110… | nein |
| `GesamtpreisInternBrutto` | decimal | 8 |  | ja | (Betrag) | nein |
| `MittlererEKManuell` | boolean | 2 |  | ja | 0 | nein |
| `Einzeleigenmasse` | decimal | 8 |  | ja | 75 | nein |
| `Gesamteigenmasse` | decimal | 8 |  | ja | 225 | nein |
| `DimensionstextHTML` | string | 2147483647 |  | ja |  | nein |
| `DimensionstextRTF` | string | 2147483647 |  | ja |  | nein |
| `LangtextHTML` | string | 2147483647 |  | ja |  | nein |
| `LangtextRTF` | string | 2147483647 |  | ja |  | nein |
| `ZubehoerTextHTML` | string | 2147483647 |  | ja |  | nein |
| `ZubehoerTextRTF` | string | 2147483647 |  | ja |  | nein |
| `USER_Verpackungsstatus` | string | 50 |  | ja |  | ja |
| `PKonfigPosID` | integer | 4 |  | ja | 0 | nein |
| `USER_Versand` | string | 10 |  | ja |  | ja |
| `USER_AbwLANSName1` | string | 50 |  | ja |  | ja |
| `USER_AbwLANSOrt` | string | 20 |  | ja |  | ja |
| `USER_AbwLANSName2` | string | 50 |  | ja |  | ja |
| `USER_AbwLANSZusatz` | string | 50 |  | ja |  | ja |
| `USER_AbwLANSLand` | string | 3 |  | ja |  | ja |
| `USER_AbwLANSPLZ` | string | 10 |  | ja |  | ja |
| `USER_AbwLANSVerwenden` | boolean | 2 |  | ja | (Adressdaten) | ja |
| `USER_AbwLANSStrasse` | string | 75 |  | ja |  | ja |
| `USER_MemoVerladung` | string | 250 |  | ja |  | ja |
| `SCKennzeichen` | short | 2 |  | ja | 0 | nein |
| `USER_RMAFehlerkategorie` | string | 200 |  | ja |  | ja |
| `USER_RMAKommentar` | string | 200 |  | ja |  | ja |
| `USER_RMAKostenpflichtig` | string | 200 |  | ja |  | ja |
| `IstSkontierfaehig` | boolean | 2 |  | ja | -1 | nein |
| `BezugPosition` | integer | 4 |  | ja | 0 | nein |
| `USER_Bez1VK` | string | 50 |  | ja | UKW 3-1-E R290 | ja |
| `USER_Bez2VK` | string | 50 |  | ja | Umluftkühlwanne: eigengekühlt | ja |
| `USER_Spannung` | string | 10 |  | ja | 230V | ja |
| `USER_GWP` | string | 15 |  | ja |  | ja |
| `USER_CO2e` | string | 10 |  | ja |  | ja |
| `USER_Kaelteleistung` | string | 25 |  | ja | 490 Watt bei VT -6°C | ja |
| `USER_Kaeltemittel` | string | 50 |  | ja | R290 | ja |
| `USER_Klimaklasse` | string | 30 |  | ja |  | ja |
| `USER_LeistungsaufnahmeK` | string | 20 |  | ja | 222 Watt | ja |
| `USER_Frequenz` | string | 8 |  | ja | 50Hz | ja |
| `USER_ELverwNormen` | string | 20 |  | ja | EN ISO 23953-2 | ja |
| `USER_Anschlusswert` | string | 50 |  | ja | (Betrag) | ja |
| `USER_Fuellmenge` | string | 20 |  | ja | 0,110 kg | ja |
| `USER_Kaeltemittel2` | string | 10 |  | ja |  | ja |
| `USER_Kaeltemittel3` | string | 10 |  | ja |  | ja |
| `USER_Kaelteleistung2` | short | 2 |  | ja |  | ja |
| `USER_Kaelteleistung3` | short | 2 |  | ja |  | ja |
| `USER_VT2` | short | 2 |  | ja |  | ja |
| `USER_VT3` | short | 2 |  | ja |  | ja |
| `USER_Fuellmenge2` | decimal | 8 |  | ja |  | ja |
| `USER_Fuellmenge3` | decimal | 8 |  | ja |  | ja |
| `USER_TypenschildArtikel` | string | 10 |  | ja |  | ja |
| `USER_gfksKonsiBuchungsID` | integer | 4 |  | ja |  | ja |
| `USER_DPSBSEiltPrio` | string | 2 |  | ja | 3 | ja |
| `USER_DPSbsMDEDeviceID` | string | 50 |  | ja |  | ja |
| `USER_DPSbsMDEFreigabe` | boolean | 2 |  | ja | 0 | ja |
| `USER_DPSbsMDESortIndex` | integer | 4 |  | ja |  | ja |
| `USER_DPSBSMDETransitErledigt` | boolean | 2 |  | ja | 0 | ja |
| `USER_DPSBSMDETransitReferenzId` | integer | 4 |  | ja |  | ja |
| `USER_DPSBSMDEMA` | string | 128 |  | ja |  | ja |

Felder im Schema: 151 (davon Benutzerfelder USER_: 45)

## L10 – IDEAL

**Offen.** Am 2026-11-13 ist an IDEAL kein SData-Endpunkt eingetragen (`SData:BaseUrl` leer, [[sage-sdata-uebersicht]] „Zugang“); es wurde
nichts an IDEAL gesendet. Sobald der Endpunkt da ist: L1–L7 mit dem Register Sage-Test (Feldliste) wiederholen und hier einen Abschnitt
„Unterschiede AKE ↔ IDEAL“ ergänzen. IDEAL hat laut Spec keinen Werkstattauftrag (P-W1 nur AKE).

## Fragen an DPS

Ergänzt die DPS-Fragen D1–D9 in [[2026-11-02-sage-pps-fa-bde-abgleich-spec]].

| # | Feld(er) | Sage-Tabelle (Beleg) | Frage / gewünschter Lesevertrag |
|---|---|---|---|
| U1 | 34 AKE-FA-Benutzerfelder (USER_Maschine, USER_Kaeltemittel, USER_Ventil, USER_Auszug, USER_Sonderausfuehrung, USER_VermerkProduktion, USER_FaSN … Liste L7) | `KHKPpsFaBelege` (L8 Präfix, L7 Request-Typ) | In `CommonWawiServicesResources/PpsFaBelegeInfo` als Spalten oder über `include=SpecificUserfields` (wie an `PpsRessourcenListenInfo`) bereitstellen? Alternativ: `BelegProperty1–4` auf welche Felder konfigurierbar (wer, wie viele)? |
| U2 | Benutzerfelder FA-Position (heute keine) und `PositionProperty1–4` | `KHKPpsFaBelegePositionen` | Gibt es Positions-Benutzerfelder an AKE/IDEAL? Wenn ja: `SpecificUserfields` an `PpsFaBelegePositionenInfo`. Wofür sind `PositionProperty1–4` gedacht? |
| U3 | Werkstattauftrag: USER_FAStatus, USER_Ausfuehrung, USER_Maschine, USER_Kaeltemittel, USER_Ventil, USER_FASollmenge, USER_IstVerladen, USER_Ladetermin, USER_MengeVerladen, USER_FAMemo … (26 USER_) | `KHKIFAuftraege` (+ `KHKIFAuftraegeStuecklisten`) | Lesende Ressource (z. B. `IFAuftraegeInfo` mit Kind `IFAuftraegeStuecklisten`) im Lesevertrag – das WMS liest WA-Liste und FA-Zusatzinfos heute per SQL-View. |
| U4 | Parameter von `dtsAbfLOGFertigungsauftraege.100036671.AKE_InterneFA` | `KHKIFAuftraege` | Wie übergibt man den Parameter `BelID` (erzeugtes SQL `KHKIFAuftraege.BelID = NULL`)? Nur relevant, falls RealTimeData doch freigegeben würde. |
| U5 | PPS-Ressourcenliste schreiben (Kopf, Positionen, Benutzerfelder) | `KHKPpsRessourcen*` | Gibt es einen Schreibweg (Ressource mit canPost/canPut oder Service-Operation)? `ArtikelStueckliste` ist nur die WaWi-Stückliste. (Erst mit eigener Freigabe, Entscheidung 2 der Spec.) |
| U6 | Ressourcen-`$schema` | – | Warum liefert `…/{Ressource}/$schema` 401, während Daten und Vertrags-`$schema` mit demselben Benutzer 200 liefern? Fehlt dem Servicebenutzer ein Recht? |
| U7 | `SpecificUserfields`, `CustomFields` als eigene Ressource | – | Eigenständig 500 (NullReference). Nur als `include` vorgesehen? Was liefert `CustomFields` (an welchem Objekt)? |
| U8 | `UpdateFertigungsauftragEntityService` / `…PositionEntityService` | `KHKPpsFaBelege(Positionen)` | Sind damit auch `USER_`-Felder änderbar (`EntityFieldName = 'USER_Maschine'`)? Lizenz/Freigabe? |
| U9 | Vertragsfreigabe | – | Gilt die Freigabe von `CommonWawiServicesResources` und `…Specific` auch für den Produktivbetrieb an AKE **und** IDEAL (Lizenz)? (offen seit [[sage-sdata-commonwawiservicesresources]]) |

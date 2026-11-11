---
typ: feature
---
# Versandmasken: welche Geräte/FAs müssen versendet werden

**Aufgenommen 2026-11-07** (Gerald): „Versandmasken – welche Geräte/FAs müssen versendet werden.“

## Worum geht es
Eine Versandliste: fertige bzw. bald fertige Geräte/FAs mit Liefertermin, Kunde, Auftrag (AB/Pos), Status (fertig gemeldet, geprüft,
verpackt, verladen), Filter nach Termin/Kunde/Tour; Druck/Export. Optional Abhaken „verladen“.

## Was es dafür schon gibt (Koordination)
- Sage-Verkauf über SData: `VerkaufBelegekopfInfo` (417), `VerkaufPositionenInfo` (972), `WarenausgangPositionenCountInfo`; Lieferschein
  aus Auftrag über `StandardBelegService` (4000) – nicht getestet ([[sage-sdata-wms-schnittstellenkatalog]] Z. 42).
- In Sage AKE gibt es FA-Benutzerfelder zum Verladen (`USER_Ladetermin`, `USER_IstVerladen`, `USER_MengeVerladen` in
  `dtsAbfLOGFertigungsauftraege`, RealTimeData – laut DPS nicht freigegeben) – [[sage-sdata-realtimedata]] Z. 181.
- FA-Status/Fertigmeldung aus BDE und Rückmelde-Epic ([[2026-11-02-sage-pps-fa-bde-abgleich-spec]]); AB-Nummer/Position an FAs
  ([[2026-10-06-kommissionierliste-ab-nummer-position-spec]]).

## Offene Fragen / Unklarheiten
- Grundlage: Kundenauftrag (Verkauf) oder FA (Fertigung) – oder beides verknüpft?
- Soll „verladen“ zurück nach Sage (Benutzerfeld/Lieferschein)?
- Verknüpfung mit Warenausgangsprüfung ([[2026-11-07-qualitaetsmasken-fragebogen-foto-enaio]]).

## Recherche Koordination 2026-11-07 (AKE-Test, nur lesend über SData)
**Was Sage liefert**
- `VerkaufBelegekopfInfo` – 417 offene VK-Belege: **VVA = Auftragsbestätigung (395)**, VVG Garantieauftrag (17), VVN Nachlieferauftrag (4),
  VV2 VorKonsi (1); Kunde `A0Empfaenger` + Name/Adresse, **Lieferadresse `A1*`**, `BelegLiefertermin`, `Lieferbedingung`, **`Tour`**, `Versand`,
  `Teillieferungen`, `Liefersperre`. `Tour`/`BelegProperty1–8` am Test leer.
- `VerkaufPositionenInfo` – 972 offene Positionen (93 Felder): Artikel, Menge, `PositionGGBestellt`/`PositionGGGeliefert`, `Liefertermin`,
  `Lieferwoche`, `PlatzID`, `Stuecklistentyp`. `PositionProperty1–8` am Test leer.
- `WarenausgangPositionenCountInfo` (417) – nur Positionsanzahl.
- **Lücke:** Die Verknüpfung VK-Position → FA liegt in Sage als Benutzerfeld (`USER_FABelId` an der VK-Position, [[sage-sdata-api-vertraege]] Z. 63),
  Verladung als FA-Benutzerfelder (`USER_Ladetermin`, `USER_IstVerladen`, `USER_MengeVerladen`). Beides ist über
  `CommonWawiServicesResources` **nicht** lesbar (`include=SpecificUserfields` am VK-Vertrag: kein Feld; `…Specific/VerkaufPositionenInfo` 404),
  `PpsFaBelegeInfo` hat kein Auftragsfeld. → **DPS-Frage:** `USER_FABelId`, `USER_Ladetermin`, `USER_IstVerladen` in `PositionProperty`/
  `BelegProperty` mappen (wie für die FA-Zusatzinfos BelegProperty1–4 angefragt).
- Im WMS gibt es den Bezug FA ↔ AB-Nummer/Position bereits aus den PPS-Views (Kommissionierliste).
**Was Sage zurücknehmen kann (ungetestet):** Lieferschein aus Auftrag per `StandardBelegService` (4000, Belegübernahme) oder über
Picklisten (`PicklistItemsEditService`, `PicklistCreateLieferscheinService` – Sage-Kommissionierung am Test leer); Lieferschein-PDF per
`ExportPrintBelegeService`; Benutzerfeld „verladen“ per `UpdateBelegpositionEntityService`.
**Einbau im WMS (Vorschlag)**
1. Versandliste „Was muss raus“: offene AB-Positionen (VVA/VVN/VVG) aus SData, verknüpft mit WMS-FAs über AB-Nr./Pos. (heute) bzw.
   `USER_FABelId` (nach DPS-Mapping); Spalten Kunde, Lieferadresse, Liefertermin/KW, Tour, FA-Status (fertig gemeldet/geprüft), Menge offen;
   Filter Termin/Kunde/Tour, Druck/CSV.
2. Status „verpackt/verladen“ zunächst im WMS (eigene Tabelle), später nach Sage (Benutzerfeld/Lieferschein).
3. Andockpunkt Warenausgangsprüfung (Qualitätsmasken).

## Anforderung Anwender (über Gerald, 2026-11-07 16:09) – „Versand-Dispomaske“
> Wie schon mal besprochen würden wir für den Versand bei IDEAL & AKE eine Dispo-Maske im WMS benötigen. Fast alle Daten, welche wir benötigen,
> sind in der Sage bzw. sollten wir bei Vergabe selber eintragen können für Auswertungen. Diese Maske sollte für beide Häuser gleich sein.
> Ziel ist es, mit einer Zentraldispo beide Häuser zu betreuen. Somit würden wir auch einen Button benötigen, wo beide Häuser in einer Liste
> erscheinen. Felder in dieser Reihenfolge: Ladedatum · Lieferdatum (Fixtermin bzw. KW) · Kunde · Lieferadresse · AB-Nummer ·
> Bestellnummer Kunde · Verkehrskürzel für Informationen (Hebe, …) · FK-Kunde · Spedition (Extrafeld) · FK-Spedition (um Auswertung zu fahren)

Ist-Stand heute: Excel-Liste ![[2026-11-07-versand-dispo-excel-ist.png]] – Spalten Laden, Zustellung („FIX 13-07“, „bis 17-07“, „x“),
Kunde, Land, Entladeort (PLZ bzw. „Sammelgut“), AB-Nummer (auch mehrere „431253+431254“, Position „431330 P.2“, „Liste“ bei Sammelgut),
Bestellnummer, wichtige Infos Kunde („HEBE“, „KW 29 (Montag/Dienstag)“), Spedition (z. B. SA, DSV, SF Logistik), Preis Kunde,
Preis Spedition, **Ergebnis** (= Preis Kunde − Preis Spedition).

### Feld-Zuordnung (Koordination 2026-11-07)
| # | Feld | Quelle | Stand |
|---|---|---|---|
| 1 | Ladedatum | WMS-Eingabe der Dispo (AKE hat FA-Benutzerfeld `USER_Ladetermin`, über SData nicht lesbar) | eingeben |
| 2 | Lieferdatum Fix/KW | Sage VK-Beleg `BelegLiefertermin`/`Liefertermin`/`Lieferwoche` als Vorschlag; Art „Fixtermin / bis / KW“ + Datum in der Dispo änderbar | Sage + WMS |
| 3 | Kunde | Sage `A0Matchcode`/`A0Name1` (Debitoren seit v1.73.0 im WMS) | Sage |
| 4 | Lieferadresse | Sage `A1Name1/2`, `A1Strasse`, `A1Land`, `A1PLZ`, `A1Ort` (abweichende Lieferanschrift); Sammelgut = Kennzeichen | Sage |
| 5 | AB-Nummer | Sage `BelegnummerFormatiert` der Auftragsbestätigung (VVA); **eine Ladung kann mehrere ABs/Positionen umfassen** | Sage |
| 6 | Bestellnummer Kunde | in Sage üblich als Referenznummer/-zeichen am VK-Beleg – **im Lesevertrag `VerkaufBelegekopfInfo` nicht enthalten**; nicht weiter geprüft (Sage-Anmeldung abgelaufen) → DPS-Frage oder Eingabe | Lücke |
| 7 | Verkehrskürzel | WMS-Auswahlliste (Hebe, Avis, Termin, …) nach ADR 0020, mehrere je Ladung + Freitext | WMS |
| 8 | FK-Kunde | WMS-Eingabe (Frachtkosten an Kunden); ob Sage einen Frachtzuschlag am Beleg führt – ungeprüft | WMS |
| 9 | Spedition | Auswahl aus **Kreditoren** mit neuem Kennzeichen „Spedition“ (wie „Beschichter“) | WMS |
| 10 | FK-Spedition | WMS-Eingabe | WMS |
| + | Ergebnis | berechnet FK-Kunde − FK-Spedition (wie Excel) | WMS |

### Modell-Vorschlag
- Eine **Ladung/Sendung** (Dispo-Eintrag) mit Ladedatum, Lieferdatum/Art, Kunde, Lieferadresse, Verkehrskürzeln, Spedition, Kosten, Status
  (geplant / vergeben / verladen); darunter 1..n **AB-Positionen** aus Sage (Beleg + Position, Menge) – deckt „431253+431254“, „P.2“ und
  Sammelgut-Listen ab. Vorschlagsliste „offene AB-Positionen ohne Ladung“ (Sage VVA/VVN/VVG, Lieferwoche) zum Zuordnen.
- Auswertung je Spedition/Kunde/Zeitraum (FK-Kunde, FK-Spedition, Ergebnis), CSV.

### Zentraldispo für beide Häuser – Architekturfrage
Heute laufen AKE und IDEAL als **getrennte WMS-Installationen mit eigener Datenbank** (AKE `akenet01`, IDEAL `idealweb01`), und IDEAL hat noch
keinen SData-Endpunkt (Lizenz bestellt). „Beide Häuser in einer Liste“ braucht deshalb eine Entscheidung:
- **A (Empfehlung):** Dispo-Modul in **einer** Installation als Zentrale, mit zwei Sage-Quellen (AKE- und IDEAL-Endpunkt) und Feld „Haus“;
  Filter AKE / IDEAL / beide. Dispo-Daten liegen an einer Stelle, Auswertung einfach. Voraussetzung: IDEAL-SData (oder übergangsweise
  IDEAL-View) und Zugriff der Dispo-Anwender beider Häuser auf diese Installation.
- B: Dispo in beiden Installationen, „beide Häuser“ liest die andere Installation per API – zwei Datenbestände, Auswertung über beide nötig.
- C: eigene gemeinsame Dispo-Datenbank, die beide Installationen nutzen – sauber, aber neues Betriebsstück.
**Entscheidung Gerald 2026-11-07 16:17:** Es gibt **keine Zentraldispo** – der Disponent hat Zugriff auf **beide** WMS. Damit gilt **B**:
- Dispo-Modul in **beiden** Installationen, gleicher Code und gleiche Maske (Standortunterschiede nur über Einstellungen); jede Installation
  liest **ihre** Sage und speichert **ihre** Dispo-Daten.
- Knopf „Beide Häuser“: zeigt zusätzlich die Dispo-Liste der anderen Installation – **nur lesend** über eine kleine API zwischen den beiden
  WMS (Adresse/Schlüssel als Einstellung). Bearbeiten nur im eigenen Haus (Link „im anderen WMS öffnen“). Die Auswertung über beide Häuser
  liest ebenso beide Seiten.
- **Nachtrag Gerald 2026-11-07 16:19:** Knopf „Beide Häuser“ wird **derzeit nicht benötigt** – nicht umsetzen (nur vormerken).
- IDEAL hat noch keinen SData-Endpunkt: die IDEAL-Dispo liest die Auftragsdaten bis dahin über die IDEAL-Views (Muster Quellen-Weiche, ADR 0019).
### Offene Fragen
- Wo steht die Kunden-Bestellnummer in Sage (Referenzzeichen?) – DPS um Freigabe im Lesevertrag bitten.
- Soll „verladen“ / Ladedatum zurück nach Sage (z. B. `USER_Ladetermin`, Lieferschein)?
- Verkehrskürzel: feste Liste? Wer pflegt sie?
- Kosten netto/brutto, Währung; Rechte (wer sieht Preise)?

## Reif für den Backlog?

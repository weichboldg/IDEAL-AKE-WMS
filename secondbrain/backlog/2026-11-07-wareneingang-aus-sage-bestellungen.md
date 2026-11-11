---
typ: feature
# epic: true
---
# Lager-Wareneingang auf Basis der Sage-Bestellungen

**Aufgenommen 2026-11-07** (Gerald, über die Koordination): „Lager Wareneingang aufgrund von Sage-Bestellungen.“

## Worum geht es
Wareneingang im WMS gegen offene Sage-Einkaufsbestellungen statt frei: Bestellung/Lieferant wählen oder scannen, offene Positionen
sehen (Artikel, Menge, Liefertermin, Ziel-Lagerplatz), Ist-Menge erfassen, einbuchen; Teil- und Überlieferung erkennbar.

## Was es dafür schon gibt (Koordination, belegt)
- Lesen über SData (AKE-Test): `EinkaufBelegekopfInfo` (484), `EinkaufPositionenInfo` (1.167 offene Positionen mit Liefertermin, Menge,
  Ziel-Lagerplatz), `AuskunftOffeneEkBestellungenInfo` (519), `AuskunftOffeneEkWareneingaengeInfo` (480) – [[sage-sdata-wms-schnittstellenkatalog]] Z. 41.
  Achtung: `EinkaufPositionenAllInfo` nie ohne `where` (Timeout).
- Zurückschreiben: `StandardBelegService` (Erfassungsart 4100 Einkauf, Beleg-/Vorgangsübernahme) = Wareneingang in Sage buchen – **nicht getestet**.
- Kreditoren und Artikel-Lieferanten liegen seit v1.73.0 im WMS ([[2026-11-06-sage-sdata-stammdaten-sync-spec]]).

## Offene Fragen / Unklarheiten
- Wer bucht den Wareneingang in Sage: WMS per `StandardBelegService` (Bestellung → Wareneingang) oder weiter Sage-Einkauf, WMS nur Lager?
  (Empfehlung Koordination: erst lesen + WMS-Lagerbuchung wie heute, Sage-Wareneingangsbeleg als zweite Stufe nach Test am AKE-Test.)
- Etikettendruck beim Eingang (Lagerplatz-/Artikel-QR)?
- Bezug zu Qualitätsprüfung Wareneingang ([[2026-11-07-qualitaetsmasken-fragebogen-foto-enaio]]).
- Standort: beide? IDEAL erst mit SData-Endpunkt (G2).

## Recherche Koordination 2026-11-07 (AKE-Test, nur lesend über SData `CommonWawiServicesResources`)
**Was Sage liefert**
- `EinkaufBelegekopfInfo` – 484 offene EK-Belege (`orderBy=BelID`): Belegkennzeichen **EBB = Bestellung (376), EBZ = Rahmenabruf (108)**;
  Lieferant `A0Empfaenger` (= Kreditor-Kto) + Name/Adresse, `Liefertermin`/`BelegLiefertermin`, `Lieferwoche`, `Teillieferungen`,
  `Liefersperre`, `Versand`, `BelegnummerFormatiert`. `BelegProperty1–8` vorhanden, am Test leer.
- `EinkaufPositionenInfo` – 1.167 offene Positionen (`orderBy=BelPosID`, 91 Felder): `BelPosId`, `Artikelnummer`, `AuspraegungID`,
  `EANNummer`, `Bezeichnung1/2`, `Menge`/`MengeBasis`/EK-Einheit/Umrechnung, **`PositionGGBestellt` / `PositionGGGeliefert`** (bestellt/geliefert,
  in 500 von 500 bzw. 85 Zeilen gefüllt → offene Menge), **`PlatzID` + `Kurzbezeichnung`/`Platzbezeichnung`** (Ziel-Lagerplatz, 484/500 gefüllt),
  `Liefertermin`, `Chargenpflicht`/`Nachweispflicht`. `PositionProperty1–8` am Test leer.
- `AuskunftOffeneEkBestellungenInfo` (519) / `AuskunftOffeneEkWareneingaengeInfo` (480) – Belegebene mit `SummeNochOffen`,
  `NettobetragNochOffen`; zusätzlich EBD = RMA-Anmeldung (31).
- `WareneingangPositionenCountInfo` (449) – nur Anzahl Positionen je Beleg.
- Nicht lesbar über diesen Vertrag: EK-Benutzerfelder (`USER_ABErhalten`, `USER_AB`, `USER_WA` …, [[sage-sdata-commonwawiservices]] Z. 24/26).
**Was Sage zurücknehmen kann (CommonWawiServices, ungetestet)**
- `StandardBelegService` mit `Erfassungsart=4100`, `BelegTransformArt=1` (Belegübernahme Bestellung → Wareneingang/Lieferschein-Kennzeichen),
  `BelegPositionLagermengen` platzgenau → **Wareneingangsbeleg in Sage aus dem WMS**. Kennzeichen des Wareneingangs (z. B. EWE?) am AKE-Test
  belegen.
- `PrintEtikettenBasicService` (EtikettType **EKPosition**) – Wareneingangsetikett aus Sage drucken; `ExportPrintBelegeService` – Beleg-PDF.
**Hinweis:** Die DPS-Benutzerfelder `USER_DPSbsMDE*` an EK-/VK-Positionen deuten auf eine **DPS-MDE-Lösung** (mobile Datenerfassung). Vor dem
Bau klären, ob DPS dafür bereits ein Modul hat/lizenziert ist – sonst Doppelentwicklung.
**Einbau im WMS (Vorschlag)**
1. Stufe 1 (nur lesen): neuer Reader `ISageEinkaufReader` (Muster ADR 0019), Cache `SageEkBelege`/`SageEkPositionen` je Lauf oder live
   abgefragt; Maske „Wareneingang zu Bestellung“: Lieferant/Bestellung wählen oder Bestell-Nr. scannen → offene Positionen (bestellt − geliefert,
   Ziel-Lagerplatz) → Ist-Menge → **bestehende WMS-Einbuchung** (inkl. Sage-Lagerbuchungs-Queue wie heute), Bezug Bestellung/Position am
   `StockMovement` speichern. Teil-/Überlieferung farbig.
2. Stufe 2 (schreiben): Wareneingangsbeleg per `StandardBelegService` statt Lagerbuchung – erst nach Schreibtest am AKE-Test und DPS-Freigabe.
3. Andockpunkt Qualitätsprüfung Wareneingang ([[2026-11-07-qualitaetsmasken-fragebogen-foto-enaio]]): Prüfung je Position/Lieferung optional
   oder Pflicht je Artikelgruppe.

## Entscheidungen Gerald 2026-11-07 16:09
- Vorschlag Stufe 1/2 passt.
- **Zusätzlich:** Maske „Offene Bestellungen der nächsten X Wochen“ (Vorschau für Lager: was kommt wann, je Lieferant/Artikel/Ziel-Lagerplatz);
  **X als Einstellung** (ADR 0020: Zahl mit Grenzen, Einheit Wochen, Default-Vorschlag 2, Standort je Haus).

## Reif für den Backlog?

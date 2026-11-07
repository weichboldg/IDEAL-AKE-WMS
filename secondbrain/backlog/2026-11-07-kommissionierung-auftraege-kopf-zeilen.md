---
typ: feature
epic: true
---
# Kommissionierung: eigene Kommissionieraufträge (Kopf/Zeilen, fortlaufende Nummer, Typ), 1–n je FA

**Aufgenommen 2026-11-07** (Gerald, über die Koordination). Wortlaut:
„Anpassung/Verbesserung Kommissionierung. Ich würde ähnlich wie beim Beschichtungsauftrag bzw. bei den Lagerbestellungen für jede
Kommissionierung eine fortlaufende Nummer vergeben und das von der Stückliste etwas trennen. Recherchiere auch hier, wie andere Systeme das
machen, und bringe Vorschläge. Ich würde eine Tabelle für Kopf und Zeilen vornehmen. Ein FA kann 1–n Kommissionierungen referenzieren bis
vollständig kommissioniert. Weiters würde ich einen Kommissioniertyp vorsehen, um in Zukunft die Lager- bzw. Glasbestellungen auch mittels
Kommissionierung abwickeln zu können.“

## Ist-Stand (Brain, vom /spec am Code zu belegen)
- Kommissionierung hängt heute direkt an FA + Stückliste: `PickingItem` hält den Fortschritt je Stücklistenzeile (datenmodell.md Z. 88),
  FA-Status 1:1 in `ProductionOrderPickingStatus` (Freigabe, Priorität, Kommissionierer, `IsDonePicking`); Oberflächen `PickingController`
  (inkl. `Bom`), `PickingLeitstand`, Fotos (`Media/`). Kein eigener Beleg, keine Nummer, keine Historie „wer hat wann welche Teilmenge“.
- Vorbilder im WMS: Beschichtungsauftrag mit Nummernkreis `BA-JJJJ-NNNNN`, Statusautomat, Eindeutigkeit unter Parallelität
  ([[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]]); Lager-/Glasbestellung `WarehouseRequisition` + `…Item`, Typ Lager=1/Glas=2,
  Status Draft/Submitted/PartiallyDelivered/Closed (datenmodell.md Z. 62–64).

## Wie andere Systeme es machen (Recherche Koordination 2026-11-07)
- **Microsoft Business Central:** eigener Beleg **Lager-Kommissionierung** (Kopf mit Herkunftsbelegart + -nummer, z. B. FA; Zeilen je
  Komponente mit Artikel, Lagerplatz, „Menge zu bearbeiten“). Erzeugt aus dem freigegebenen FA (einzeln oder per Stapel). **Teilmengen**
  möglich, Zeile teilen bei mehreren Lagerplätzen; Kommissionierzeilen bleiben mit den FA-Komponentenzeilen verknüpft, deren
  „Menge kommissioniert“ fortgeschrieben wird. Buchen = Verbrauch bzw. Umlagerung. Derselbe Beleg dient für Verkauf, Umlagerung,
  Produktion, Montage (Herkunftsart = Typ).
- **Odoo:** jede Kommissionierung ist ein „Transfer“ mit **Vorgangstyp** (eigene Nummernfolge je Typ, z. B. `WH/PC/00012` für
  Produktions-Kommissionierung); bei Teilerledigung entsteht ein **Rückstand (Backorder)** als neuer Beleg mit Restmenge.
- **Sage 100:** Picklisten mit Positionen (Kommissionier-Handle, Lagerplatz, gepickte Menge, Serien/Chargen) und Lieferschein aus
  Pickliste (`PicklistItemsEditService`, `PicklistCreateLieferscheinService`, [[sage-sdata-commonwawiservices]]) – am AKE-Test leer, nicht im Einsatz.
- Gemeinsames Muster: **Beleg (Kopf/Zeilen) getrennt vom Herkunftsbeleg**, Typ/Herkunftsart, Nummernkreis je Typ, Status, Teilmengen
  über Folgebelege bzw. Restmengen, Zeile kennt Herkunftszeile, Fortschritt wird auf die Herkunft zurückgeschrieben.

## Vorschlag Koordination
**Datenmodell**
- `PickingOrders` (Kopf): `Nummer` (Nummernkreis je Typ, z. B. `KO-JJJJ-NNNNN`; Muster Beschichtungsauftrag), `Typ`
  (FA=1; später Lagerbestellung=2, Glasbestellung=3), Herkunft (`ProductionOrderId` bzw. `WarehouseRequisitionId`), Status
  (Entwurf → Freigegeben → In Arbeit → Teilweise → Abgeschlossen; Storniert), Priorität, Kommissionierer, Ziel (Werkbank /
  Kommissionierwagen = `StorageLocation.IsPickingTransport`), Fälligkeit, Bemerkung, Audit.
- `PickingOrderLines` (Zeilen): Herkunftszeile (Stücklistenposition bzw. `WarehouseRequisitionItem`), Artikel, Soll, Ist,
  Von-Lagerplatz, Status je Zeile (offen/teilweise/erledigt/Fehlteil), Foto-Bezug, Lagerbewegung (`StockMovementId`).
- Fortschritt je FA = Summe über alle Kommissionieraufträge des FA; **FA vollständig** = alle Soll-Positionen erfüllt →
  `IsDonePicking` wird daraus abgeleitet (statt direkt gepflegt).
**Ablauf**
- Kommissionierauftrag aus dem FA erzeugen (alle offenen Restmengen oder Auswahl), Druck/Anzeige mit Nummer und QR; Teilkommissionierung
  schließt den Auftrag mit „teilweise“ und bietet **Folgeauftrag über die Restmengen** an (Backorder-Prinzip) – so referenziert ein FA 1–n
  Aufträge, bis er vollständig ist.
- Stückliste bleibt Datenquelle (Soll), wird aber nicht mehr selbst „abgehakt“ – Trennung wie gewünscht.
- Typ Lager-/Glasbestellung (Stufe 2): Lagerbestellung erzeugt einen Kommissionierauftrag Typ 2/3, die bisherige Lager-Worklist
  (`WarehousePicking`) wird darauf umgestellt.
**Migration:** bestehende `PickingItem`-Fortschritte je FA in je einen Kommissionierauftrag „Altbestand“ überführen (Trockenlauf mit Zählung).
**Etappen-Vorschlag:** E1 Modell + Nummernkreis + Migration; E2 Erzeugen/Anzeigen/Abarbeiten FA-Typ (bestehende Masken umstellen);
E3 Teil-/Folgeaufträge + FA-Fortschritt; E4 Leitstand/Druck/Fotos auf Auftrag; E5 (später) Typ Lager/Glas.

## Entscheidungen/Ergänzungen Gerald 2026-11-07 16:41
- **Erzeugung:** Der **Leitstand erzeugt den Kopf** (bei der Freigabe). Die Liste der Kommissionierer zeigt dann **Kommissionieraufträge**
  (Nummer, FA, Bereich, Priorität, Fälligkeit, Status, Kommissionierer) statt FAs.
- **Sammelkommissionierung** mehrerer FAs in einem Auftrag: **vormerken** (nicht in dieser Spec umsetzen; Modell so anlegen, dass eine
  Zeile ihre Herkunft selbst trägt und ein Kopf später mehrere FAs umfassen kann).
- **Kommissionierbereiche** (neu): z. B. **Glas**, **Lager**, **Vorkommissionierung** (weiter unterteilbar). Stammdatentabelle
  `PickingAreas` (Name, Kürzel, Reihenfolge, aktiv, ggf. Standard-Kommissionierer/Drucker). Jede Zeile bekommt einen Bereich; der
  Leitstand erzeugt bei der Freigabe **je FA und Bereich einen Auftrag** → getrennte Listen je Bereich. Zuordnungsregel Artikel → Bereich
  in Rangfolge (Spec schlägt vor, Datei:Zeile belegen): Sage-Feld `Kommissionieren` (IDEAL), Lagerort/Hauptlagerplatz, Artikelgruppe,
  Artikel-Ausnahme; Fallback „Lager“.
- **Abgleich mit IDEAL:** IDEAL trennt die Kommissionierlisten heute nach dem Knotenfeld **`Kommissionieren`** (Kommissionier-Ziel aus Sage,
  `KommissionierListenService`, [[2026-07-29-standort-ideal-teil-3-spec]] Z. 47–99; nur Blattebene `SubFA = 0`). → Die Bereiche
  sollen diese Ziele abbilden (IDEAL: Bereich = Wert von `Kommissionieren`, gemappt über die Bereichstabelle); AKE heute über die
  FA-Flags `HasGlass` u. a. (`ProductionOrderPickingStatus`) und Vorkommissionier-Termin. Prüfabfrage für den /spec (IDEAL-WMS-DB):
  `SELECT Kommissionieren, COUNT(*) FROM <FaHierarchy-Knotentabelle> WHERE SubFA = 0 GROUP BY Kommissionieren;` (Tabellen-/Spaltennamen am
  Code belegen) – liefert die heute genutzten Ziele.

## Offene Fragen / Unklarheiten
- Nummernformat und ob Nummernkreis je Typ oder gemeinsam (Vorschlag: gemeinsamer Kreis `KO-JJJJ-NNNNN`, Typ als Feld).
- Wer erzeugt Aufträge: Leitstand (Freigabe) automatisch je FA oder Kommissionierer manuell?
- Mehrere FAs in einem Auftrag (Sammelkommissionierung) – jetzt oder später?
- Lagerbewegung bei Kommissionierung: Umbuchung auf Kommissionierwagen/Werkbank (heute?) – am Code belegen.
- Bezug zur Sage-Rückmeldung (Materialentnahme mit FA-Bezug, Rückmelde-Epic) und zu [[2026-11-07-lagerbuchungen-fa-feld-und-verlinkung]].

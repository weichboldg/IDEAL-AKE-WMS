---
typ: feature
# epic: true
---
# Neuer Hauptmenüpunkt „Einkauf“ – erster Punkt: Bestellvorschläge für Nachbestellartikel

**Aufgenommen 2026-11-07** (Gerald): „Neuer Hauptpunkt Einkauf, und erster Menüpunkt für Bestellvorschläge zu Artikeln, welche vom Einkauf
nachbestellt werden sollen. Recherchiere im Internet nach Logiken aus Sage und anderen Systemen und wie wir das im WMS einbauen könnten.“

## Was es im WMS schon gibt (Brain, belegt)
- Meldebestand-Warnung + Farbcodierung (`Article.MinimumStock`, `StockCheckService`), Meldebestand-Mail (`NotificationWorker`,
  `Notifications:MeldebestandEnabled`) – feature-map Z. 25, services.md Z. 210–220.
- Bedarfsmeldungen aus der Stückliste (`PartRequisitions`, Mail an Empfänger je Artikelgruppe) und Lagerbestellungen aus der Produktion –
  feature-map Z. 76–85. Menü „Bestellungen“ existiert (Bedarfsmeldungen, Lagerbestellungen, Fehlteile).
- Seit v1.73.0 aus Sage (SData): `ArticleSageDetails` mit **Meldebestand, Bestellmenge, Wiederbeschaffungszeit (WBZ), Hauptlieferant,
  letzter Lieferant, Lagerbestand verfügbar/ohne Dispo, mittlerer EK, ABC-Klasse, `USER_Mindestbestand`**; `ArticleSuppliers` (alle
  Lieferanten je Artikel mit Bestellnummer, **Mindestbestellmenge, EK-Einheit/Umrechnung, VPE**, Hauptlieferant-Kennzeichen); Kreditoren.
- Offene Bestellungen lesbar (`EinkaufPositionenInfo`: bestellt/geliefert, Liefertermin) → „Bestellt, noch nicht geliefert“.
- **Zurück nach Sage getestet (AKE-Test 30.10./06.11.):** `DispositionStapelService` + `DispositionStapelPositionenService` – Stapel anlegen,
  Bestellvorschlag-Position 00722001, 200 Stk, Lieferant K31013 → 200, Sage ergänzt Bestellnummer, Einzelpreis, Liefertermin. Löschen des
  Stapels wirkt nicht (Stapel wiederverwenden). Stapeltypen: Manuell=0, Errechnet=1, … ([[sage-sdata-commonwawiservices]] Z. 42, 101–105;
  SData-Befund §14 B1–B4). Freigabe-Antwort G7: Bestellvorschläge als **eigene Folge-Spec**.

## Logiken aus Sage und anderen Systemen (Recherche Koordination 2026-11-07)
- **Sage 100 Disposition:** Artikel-Dispoparameter Meldebestand, Mindestbestand, Bestellmenge, WBZ; Ergebnis landet im **Dispositionsstapel**,
  aus dem der Einkauf Bestellungen erzeugt. Zusatzmodul **„Erweiterte Disposition“** (abacus): Reichweite aus Bestand, Bedarfen und WBZ,
  Verbrauch aus Verkaufszahlen/Absatzplan, Sicherheitsbestand aus ABC-/XYZ-Klasse und Standardabweichung, Lieferanten-Schließzeiten;
  Ergebnis in Dispo-Stapel oder direkt Bestellung.
- **Microsoft Business Central (Beispiel Standard-ERP):** Wiederbeschaffungsverfahren **Feste Bestellmenge** (Meldebestand + feste Menge),
  **Auffüllen auf Maximalbestand** (Meldebestand + Höchstbestand), **Los-für-Los** (Summe der Bedarfe in einem Zeitraum), **Auftrag**
  (1:1 je Bedarf); Parameter Sicherheitsbestand, Sicherheitszuschlag Beschaffungszeit, Zeitraster, **Mindest-/Höchst-Bestellmenge,
  Bestellvielfaches**.
- **Klassische Formeln:** Meldebestand = Ø-Verbrauch × WBZ + Sicherheitsbestand; Sicherheitsbestand = z (Servicegrad, z. B. 1,65 = 95 %)
  × Standardabweichung Verbrauch × √WBZ. Bestellpunktverfahren (bestandsgesteuert) vs. Bestellrhythmusverfahren (feste Termine).

## Vorschlag Einbau im WMS
**Kernformel je Artikel (Projektion):**
`verfügbar = Lagerbestand (WMS bzw. Sage verfügbar) + offene Bestellmenge (Sage EK) − offene Bedarfe (Lagerbestellungen/Bedarfsmeldungen,
später FA-Materialbedarf)` → **Vorschlag, wenn verfügbar ≤ Meldebestand**.
**Vorschlagsmenge** nach Verfahren je Artikel (Stufe 1 aus Sage-Parametern):
- „Feste Menge“: Sage-`Bestellmenge`;
- „Auffüllen“: bis Höchstbestand (falls gepflegt) bzw. Meldebestand + Bestellmenge;
- danach Rundung auf **Mindestbestellmenge / VPE** des gewählten Lieferanten (`ArticleSuppliers`), Lieferant = Hauptlieferant (änderbar
  auf andere Lieferanten des Artikels), gewünschter Liefertermin = heute + WBZ.
**Maske „Einkauf → Bestellvorschläge“:**
- Liste je Lieferant gruppiert (Kreditor), Spalten: Artikel, Bezeichnung, Bestand, offen bestellt, offene Bedarfe, verfügbar, Meldebestand,
  Vorschlag, Einheit/VPE, Lieferant, Bestellnummer Lieferant, WBZ, letzter Lieferant, ABC; Filter Lieferant, Artikelgruppe,
  „nur Nachbestellartikel“, Dringlichkeit (verfügbar < 0 / < Meldebestand).
- Menge und Lieferant anpassen, Zeilen auswählen → **„An Sage übergeben“** als Dispo-Stapel (`DispositionStapelPositionenService`, Stapel
  je Lieferant oder ein WMS-Stapel, wiederverwendet) **oder** CSV/Druck als Stufe 0; Status je Vorschlag (offen / übergeben / ignoriert bis …),
  Protokoll und Wiederholung über die bestehende Outbox (SData-Epic E9).
- Einstellungen nach ADR 0020: Verfahren-Default, Vorschau-Horizont, Stapelkennung, Rundungsregel, Einbezug Lagerbestellungen.
**Stufen:** 0 Liste + Export (nur lesen) → 1 Übergabe Dispo-Stapel → 2 Verbrauchsbasiert (Ø-Verbrauch aus Bewegungshistorie,
Reichweite, Sicherheitsbestand nach ABC/XYZ) und Bedarfe aus FA-Stücklisten.
**Andockpunkte:** [[2026-11-07-wareneingang-aus-sage-bestellungen]] (offene Bestellungen, Wareneingang), Kreditoren/Artikel-Lieferanten
(v1.73.0), Lagerbestellungen/Bedarfsmeldungen.

## Offene Fragen / Unklarheiten
- Was heißt „Nachbestellartikel“ fachlich: Sage-Kennzeichen (`IstBestellartikel`?), Meldebestand > 0, Artikelgruppe, oder eigenes WMS-Kennzeichen?
- Rechnet Sage heute schon Bestellvorschläge (Sage-Disposition im Einsatz, „Erweiterte Disposition“ lizenziert)? Dann WMS nur Anzeige/
  Anstoß, sonst Logik im WMS – Doppellogik vermeiden.
- Welcher Bestand zählt: WMS-Bestand oder Sage-verfügbar? Dispo-relevante Lager?
- Höchstbestand in Sage gepflegt? Mindestbestand (`USER_Mindestbestand`) vs. Meldebestand?
- Wer gibt frei, und soll der Vorschlag in Sage als Dispo-Stapel landen oder direkt als Bestellung (`StandardBelegService` 4100)?
- Standorte: AKE zuerst (SData); IDEAL nach Lizenz.

## Reif für den Backlog?

---
type: testprotokoll
title: "Browser-Testprotokoll Bündel v1.73.0 bis v1.88.0 (AKE-Test + IDEAL-Test) – Abnahme vor dem Merge"
ausfuehrung: "Teil A + C: Gerald · Teil B: Claude Cowork (Abnahme)"
system: "AKE: https://akenet01.ake.at:4444/ · IDEAL: http://idealweb01.ideal.ideal-ake.at:88/ (Stand v1.88.0 laut Fußzeile)"
spec:
  - "[[2026-11-06-sage-sdata-stammdaten-sync-spec]]"
  - "[[2026-11-07-einstellungen-auswahlfelder-und-beschreibung-spec]]"
  - "[[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-spec]]"
  - "[[2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-spec]]"
  - "[[2026-11-07-ean-einkaufsartikel-scan-spec]]"
  - "[[2026-11-08-bereinigungen-nach-buendel-2026-11-07-spec]]"
  - "[[2026-11-08-artikeldetails-lieferanten-anzeigen-spec]]"
  - "[[2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-spec]]"
  - "[[2026-11-08-wareneingang-aus-sage-bestellungen-spec]]"
  - "[[2026-11-08-lagerbuchungen-fa-feld-und-verlinkung-spec]]"
  - "[[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]]"
created: 2026-11-08
---

# Browser-Testprotokoll Bündel v1.73.0 – v1.88.0

Grundlage: die Test-Checklisten am Ende der Specs (TS-106, 108, 101.22–25, 109, 110, 111, 112, 113, 115, 116, 120) und die
Koordinations-Befunde vom 07./08.11. auf DEV. **Teil B prüft nur, was ohne Buchung, ohne Speichern und ohne Freigabe geht.**
Alles mit Speichern, Buchen, Freigeben, Netum/Kamera und SQL steht in Teil A bzw. C.

**Testdaten (AKE):** Artikel `00722001` (Hauptlieferant K31013, Bestelleinheit lfm), Fertigungsartikel `86006871`,
Kreditor `K31013` („carlo“ Loysch), Kreditor mit vielen Artikeln `K30601`, Bestellung `{BESTELLUNG_AKE}` (offene EBB-Bestellung mit
Lagerartikel, von Gerald), FA `{FA_AKE}` (freigegeben, mit Stückliste, von Gerald).
**Testdaten (IDEAL):** Haupt-FA `{FA_IDEAL}` mit Sub-FA (von Gerald).

## Teil A — Gerald, vor Teil B

- [ ] A1 Je Standort Backup, dann SQL `98`–`140` laut Deploy-Abschnitten der Specs in Nummernreihenfolge (ungenutzte Nummern
      sind Lücken), `SQL/110` zuerst als **Trockenlauf** (Ergebnis notieren), `SQL/108` erst nach P-4.
- [ ] A2 Web **und** Dienst v1.88.0 aus dem Bündel-Worktree publizieren (AKE und IDEAL). Fußzeile = v1.88.0.
- [ ] A3 Prüfabfragen vor dem Deploy notiert: Einstellungen P-E1a/b/c + B2; Bereinigungen P-1a/P-1b (nur Bool-Keys werten) und P-4;
      Lagerbuchungen P1/P2.
- [ ] A4 AKE-Test ServiceSettings: `SData:BaseUrl`/`Application`/`Dataset` auf `sagetest01`/`ol`/`ake_TEST2026;1`;
      `Sync:Quelle:Artikel = SData`; `Sync:ArticlesEnabled`, `Sync:ArtikelLieferantEnabled`, `Sync:KreditorenEnabled`,
      `Sync:DebitorenEnabled`, `Sync:EinkaufBestellungenEnabled` = an. Einen Dienstzyklus abwarten (Aktivitätsprotokoll).
- [ ] A5 Testdaten eintragen: `{BESTELLUNG_AKE}` = ______ · `{FA_AKE}` = ______ · `{FA_IDEAL}` = ______
- [ ] A6 `QrMitFaNummer` Ausgangswert notieren: AKE ______ IDEAL ______ (für Teil C, danach zurückstellen).

## Freigabe-Block (Gerald kreuzt an — ohne Haken ist der Schritt NICHT PRUEFBAR)

- [ ] F1 Spaltenauswahl (Zahnrad) auf Artikelliste, Offene Bestellungen, Kommissionierung ändern und am Ende wiederherstellen.
- [ ] F2 Filter „Aufträge aller Kommissionierer“ und Bereichsfilter setzen (nur Anzeige).
- [ ] F3 Bestellung im „Wareneingang zu Bestellung“ öffnen (nur Anzeige, **nicht** „Einbuchen“).
- [ ] F4 Bearbeiten-Seiten (Artikel, Kreditor) öffnen, **ohne** zu speichern.

---

## Teil B — Auftrag an Claude Cowork (ab hier kopieren)

Arbeitsweise ABNAHME. Systeme laut Kopf (AKE ausdrücklich freigegeben). Nichts buchen, nichts speichern, nichts freigeben, keine
Einstellung ändern. Spaltenauswahl/Seitengröße zu Beginn notieren, am Ende wiederherstellen. Druckansichten öffnen ja, drucken nie.
Bericht nach Projektanweisung.

### B0 Rahmen (AKE und IDEAL)
1. Fußzeile. **Erwartet:** v1.88.0 an beiden.
2. `/SyncLog` (AKE): letzter Zyklus nach A4. **Erwartet:** Läufe „Artikel-Sync“ (Quelle SData, `ArticleSageDetails` neu/aktualisiert),
   „Artikel-Lieferanten-Abgleich“ (`ArtikelVariantenExtended` ≈ 23.509 Zeilen, `hauptlieferant` > 0), „Kreditoren-Abgleich“ (Adressen +
   Ansprechpartner gelesen), „Offene Bestellungen (Sage)“; keine Fehlerläufe. Zeilenzahlen notieren.

### B1 Einstellungen (TS-109, TS-111)
1. `/Settings` und `/ServiceSettings` (AKE + IDEAL): Gruppen, Suche, „Nur geänderte“, je Eintrag Anzeigename, Beschreibung,
   „Verwendet in“, Standard, Standort; Auswahllisten statt Freitext bei festen Werten (z. B. `QrErstesFeld`, `QrTrennzeichen`,
   `Sync:Quelle:*`). **Erwartet:** wie Spec; keine Freitextfelder für feste Wertebereiche.
2. IDEAL `/ServiceSettings`, `Sync:Quelle:Artikel`: Sperrhinweis P-R1 steht **im eigenen Eintrag**, nicht über „Quelle FA-Struktur“.
3. Aktivitätsprotokoll „Einstellungsprüfung“ nach dem Start: Anzahl `ungueltig` notieren (AKE-DEV hatte `LagerbestellungAktiv = "1"`).

### B2 Kreditoren / Debitoren (TS-106.10/12)
1. `/Creditors` (AKE): Name, Matchcode, PLZ/Ort, Land, Telefon, E-Mail gefüllt; Knopf „Kreditor anlegen (Notfall)“.
2. `K31013` Details: Adresse, Ansprechpartner, UID, „Letzter Kreditoren-Abgleich“; Karte **„Artikel dieses Lieferanten“** mit Zahl,
   Filter, CSV-Knopf, Artikel-Links. `K30601`: Karte lädt, Hinweis bei vielen Zeilen.
3. `/Debitors`: Spalten gefüllt, Knopf „Debitor anlegen (Notfall)“.

### B3 Artikel (TS-112)
1. `/Articles?search=00722001`: Artikelnummer ist Link auf `/Articles/Info`. Optionale Spalten „Hauptlieferant“/„Letzter Lieferant“
   im Zahnrad vorhanden (F1).
2. Artikelinfo `00722001`: Karten „Sage-Zusatzfelder“ (inkl. **Mengeneinheiten** gefüllt) und „Lieferanten“ (Hauptlieferant + letzter
   Lieferant K31013 mit Link, Tabelle mit Bestellnummer VISB00061, Mindestbestellmenge 200, Bestelleinheit lfm, WBZ 14). Zeitstempel
   „aus Sage gelesen am …“ notieren und mit dem Lauf aus B0.2 vergleichen.
3. Bearbeiten `00722001` (F4): dieselben Karten nur lesend; Benutzerfeld-Merkmale „aus Sage“ gesperrt.
4. `86006871`: Sage-Zusatzfelder vorhanden (Fertigungsartikel).

### B4 Fehlteile (TS-108, TS-113)
1. `/WarehouseRequisitions`: Karte „Meine Fehlteile“ zählt nur den aktiven Typ (Lager bzw. Glas), Links filtern passend.
2. `/MissingParts`: „Nur meine Werkbänke“ – Trefferzahl und Reiter-Zahlen passen zur Liste; Haken per Klick ausschaltbar (F2-artig, nur Anzeige).

### B5 Wareneingang zu Bestellung (TS-116)
1. Menü Bestellungen → „Offene Bestellungen“: Hinweis „nur EBB · Basiseinheit“, Filter Wochen (Default laut Einstellung), Lieferant,
   Bestellung, Artikel, Ziel-Lagerplatz, „nur überfällig“; **überfällig rot mit Legende, sonst neutral**; Stand-Zeitpunkt; CSV.
2. „Wareneingang zu Bestellung“ mit `{BESTELLUNG_AKE}` öffnen (F3): Spalten bestellt / offen (Sage) / im WMS erfasst / Rest / Einheit /
   Ziel-Lagerplatz (**vorbelegt**, wenn Sage-Platz buchbar) / Ist-Menge / „Überlieferung bestätigt“. Position mit abweichender Einheit
   markiert und nicht buchbar. Bei gesperrtem Lieferanten Banner „Lieferant gesperrt“. **Nicht einbuchen.**

### B6 FA in Lagerbuchungen (TS-115)
1. `/StockMovements`: Spalte „Fertigungsauftrag“ als Link mit Status (offen / „Achtung: erledigt“), unbekannte FA ohne Link; Filter
   „Fertigungsauftrag“ mit Scan-Knopf. Link öffnet den FA (AKE: FA-Liste, IDEAL: FA-Struktur).
2. `/StockMovements/Inbound`, `/Outbound`, `/Transfer`: eigenes Feld „Fertigungsauftrag“ mit Vorschlägen (nur ansehen).
3. `/StockOverview`: Artikel-Knopf und FA-Knopf getrennt (Beschriftung/Tooltip); Hinweistext ohne „immer FA“.

### B7 Kommissionierung (TS-120)
1. Stammdaten → `/PickingAreas`: Bereiche Lager (Standard), Glas, Vorkommissionierung; Zuordnungsregeln-Seite öffnet, Reihenfolge
   Artikel → Kommissionierziel → Lagerort → Artikelgruppe.
2. `/Picking` (F2): Liste der **Kommissionieraufträge** (Prio, Auftrag, FA, Werkbank, Bereich, Fällig, Status, Kommissionierer), nach
   Teil C mit Aufträgen zu `{FA_AKE}` je Bereich; Auftrag öffnen → Kopf mit Nummer `KO-JJJJ-NNNNN`, Zeilen mit Soll/Ist/Lagerplatz.
   **Nichts abhaken.**
3. `/PickingLeitstand`: Knopf „Fehlende Kommissionieraufträge … erzeugen“ vorhanden (**nicht drücken**).
4. IDEAL: Kommissionierlisten je Ziel weiter vorhanden; Aufträge von `{FA_IDEAL}` nach Bereich getrennt.

### B8 EAN, QR (TS-110, TS-101.22–25)
1. Artikelinfo eines Artikels mit EAN (`00726502`, EAN `4002872120264`): EAN in den Sage-Zusatzfeldern.
2. Bestandsübersicht/Hilfe: Texte zu QR/FA passen zu B6.3. (Scans selbst → Teil C.)

---

## Teil C — Gerald (Buchen, Freigeben, Geräte)

- [ ] C1 Leitstand: `{FA_AKE}` freigeben → Kommissionieraufträge je Bereich entstehen; Kommissionierer ändern → Aufträge folgen (TS-120.2/7).
- [ ] C2 Kommissionierauftrag teilweise erledigen → Folgeauftrag über die Restmengen (TS-120.3–5); zwei Browser parallel freigeben (120.10).
- [ ] C3 Wareneingang `{BESTELLUNG_AKE}`: eine Position einbuchen, Überlieferung mit Häkchen, abweichende Einheit gesperrt (TS-116);
      **Einkauf schließt die Position in Sage ohne Lagerbuchung** (Freigabe-Antwort 1).
- [ ] C4 Einbuchung mit FA per Artikel-QR (Feld 3) an Kamera und Netum, Pflicht-Einstellung je Standort testen (TS-115.2/12).
- [ ] C5 Bestandsübersicht: Artikel-QR mit FA über Artikel-Knopf → nur Artikelfilter; über FA-Knopf → FA (TS-115.11, TS-101.22–25);
      danach `QrMitFaNummer` auf A6 zurück.
- [ ] C6 EAN-Scan am Netum inkl. Auswahlfenster (zwei gleiche EANs per SQL) und Scan bei offenem Fenster (TS-110.2/3/8).
- [ ] C7 A4 OSEON-Klickhandler am Gerät mit Kamera: doppelte Wirkung beim Scan sichtbar? (sonst keine Änderung).
- [ ] C8 Rollen: Benutzer `stock_read` / ohne Lagerrecht / nur Glas (TS-115.8, 113.9, 120.12).
- [ ] C9 SQL/110 scharf nach Sichtung des Trockenlaufs (P1/P2 vorher), Ergebnis in die Deploy-Aufgabe.
- [ ] C10 Entscheidungen aus den QA-Abschnitten: Einstellungen B1 (Eingaben nach Ablehnung), Lagerbuchungen Q-1/Q-2,
      Kommissionierung Q9/Q10/Q11/Q14, Wareneingang N-1 und `HR;0;0;0` buchbar?

**Danach:** Bericht + Entscheidungen → Merge des Bündels (Schranke 2), dann Vorwärts-Merge der Hotfixes.

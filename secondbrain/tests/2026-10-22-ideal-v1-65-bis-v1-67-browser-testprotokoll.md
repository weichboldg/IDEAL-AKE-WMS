---
type: testprotokoll
title: "Browser-Testprotokoll IDEAL — v1.65.0 bis v1.67.0 (TS-98, TS-99, TS-100) + Nachtest v1.64"
ausfuehrung: "Teil A + C: Gerald · Teil B: Claude Cowork (Abnahme)"
system: "http://idealweb01.ideal.ideal-ake.at:88/ (nur IDEAL, Stand v1.67.0 laut Fußzeile)"
spec:
  - "[[2026-10-14-spaltenauswahl-zahnrad-sammel-spec]]"
  - "[[2026-10-15-scan-handler-hardware-scanner-spec]]"
  - "[[2026-10-21-vault-links-per-schalter-spec]]"
vorlauf: "[[2026-10-21-ideal-v1-59-bis-v1-64-browser-testbericht]]"
created: 2026-10-22
---

# Browser-Testprotokoll IDEAL — v1.65.0 bis v1.67.0 + Nachtest

Grundlage: TS-98, TS-99 und TS-100 in `docs/TESTSZENARIEN.md` im Bündel (v1.67.0) und die Checklisten der drei Specs.
Dazu ein Nachtest aus dem Lauf vom 21.10.: B4 c (Erwartung war im Protokoll falsch), B6.2 (KA-Name), B12 (Druck), Z1/Z5.
**Teil B prüft nur, was ohne Buchung und ohne Speichern geht.** Alles am Handheld (Netum) und alles mit Speichern steht in Teil C.

**Testdaten:** Personalnummer `123`, Haupt-FA `1059463` (Sub-FA `1065150`), Terminal-Werkbank Id `65`, Stückliste `/Picking/Bom/131`.

**Korrektur zum Lauf vom 21.10. (B4 c):** Bei IDEAL klappt ein mehrdeutiger Scan `FA,AG` die Gruppe auf, eine Auswahlliste
„… Treffer“ gibt es dort nicht (Spec v1.64.0, FA-4.2). Das Ergebnis vom 21.10. war daher „Protokoll passt nicht“, kein Fehler.

## Teil A — Gerald, vor Teil B

- [x] A1 v1.67.0 eingespielt (Web **und** Dienst).
- [x] A2 Standorteinstellungen: „Vault-Links anzeigen“ = ______ (an/aus), „enaio-Dokument-Links anzeigen“ = ______ (an/aus).
- [ ] A3 Standorteinstellungen › Hardware-Scanner: aktiv = ______, Präfix = ______.
- [x] A4 Quelle der Arbeitsplätze (Werkbank-Anlage) geprüft/korrigiert: ______ ; danach ein Lauf erfolgt. Anzahl
      „nicht in Sage“ jetzt: ______
- [x] A5 Haupt-FA mit Beschichtungsauftrag im Status „Übergeben“ (für B9): ## BA-2026-00004______

## Freigabe-Block (Gerald kreuzt an — ohne Haken ist der Schritt NICHT PRUEFBAR)

- [ ] F1 Terminal 65: Anmeldung mit `123`, „Wechseln“, Arbeitsgänge **nur antippen**, Nummern ins Scanfeld tippen.
      Start-Knöpfe werden nie gedrückt.
- [ ] F2 Spaltenauswahl (Zahnrad) auf Kommissionierlisten, FA-Struktur und FA-Liste ändern und am Ende wiederherstellen.
- [ ] F3 In der Browser-Konsole den `viewKey` der Seite auf einen ungültigen Wert setzen (TS-98.8, provoziert einen
      abgewiesenen Speicherversuch, ändert keine Daten).
- [ ] F4 Bearbeiten-Seite eines Benutzers öffnen, ohne zu speichern (TS-98.12).

---

## Teil B — Auftrag an Claude Cowork (ab hier kopieren)

Arbeitsweise ABNAHME. System: `http://idealweb01.ideal.ideal-ake.at:88/`. Nichts buchen, nichts speichern, keine Einstellung
ändern. Spaltenfilter und Spaltenauswahl zu Beginn notieren, am Ende wiederherstellen. Druckansichten öffnen ja, drucken nie
(Druckdialog mit Escape schließen); PDF-Downloads nur nach Rückfrage. Bericht nach Projektanweisung.

### B0 Rahmen
1. Fußzeile: Version. **Erwartet:** v1.67.0.
2. `/StandortEinstellungen` nur lesen. **Erwartet:** Gruppen „Hardware-Scanner“ (3 Felder: aktiv, Präfix, Größter Zeichenabstand),
   „Vault (Zeichnungen)“ (Schalter, Vorlage mit `{Artikelnummer}`), „enaio (Dokumente)“ (Schalter, Vorlage mit `{ObjektId}`,
   View-Name `[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]` oder leer). Werte notieren und mit A2/A3 vergleichen.
3. `/ServiceSettings` nur lesen. **Erwartet:** `Sync:EnaioDmsEnabled` steht dort (Kategorie Sync), nicht in den Standorteinstellungen
   (TS-100.11).

### B1 Zahnrad und CSV: Platz (TS-98.1, 98.2)
Je Ansicht: Lage von Zahnrad und CSV-Knopf notieren. **Erwartet:** genau ein Zahnrad rechts über der Tabelle, CSV direkt links daneben;
nie in der Filterkarte, in der Zählzeile, in einem Hinweis oder doppelt.
1. FA-Liste, Leitstand › Kommissionierung, Kommissionierung, FA-Vervollständigung, FA-Abarbeitungsliste: Zahnrad steht **über**
   der Zeile „… Aufträge · … Sub-FAs“.
2. Stückliste `/Picking/Bom/131`, Kommissionierlisten Liste und Summiert, Vorschlagsliste Beschichtung Liste und Summiert,
   Vormontage Einzeln und Summiert, Beschichtungsaufträge Liste.
3. FA-Struktur und OSEON-Teileverfolgung: Zahnrad rechts neben „Alle auf/zu“.
4. Stammdaten › Arbeitsgänge; BDE-Stammdaten mit drei Reitern: **jeder** Reiter hat seinen eigenen CSV-Knopf über seiner Tabelle.
5. Bestandsübersicht, Bewegungshistorie (nur CSV).
Screenshot je auffälliger Ansicht.

### B2 Gruppenkopf und „?“ (TS-98.4, 98.6, 98.7, 98.11) — braucht F2
1. Kommissionierlisten Liste: Zahnrad, Spalte `HauptFA` ans Ende ziehen, die nun erste Spalte ausblenden. **Erwartet:** alle
   Haupt-FA-Köpfe mit PDF-Link sichtbar; nach F5 unverändert. Kein Hinweis zum Speichern.
2. FA-Struktur: „Struktur“ nach hinten, „Bezeichnung“ ausblenden. **Erwartet:** Strukturkopf **und** Kopfdaten-Zeile sichtbar.
3. FA-Liste: erste Spalte ausblenden. **Erwartet:** „?“ steht in der nächsten sichtbaren Filterzelle, Popover öffnet.
4. Stückliste: Kommissionierziel ohne Treffer wählen. **Erwartet:** gelber Hinweis, Zahnrad bleibt sichtbar. Ziel zurücksetzen.
5. Alles auf den notierten Ausgangsstand zurücksetzen.

### B3 Speicherfehler-Hinweis (TS-98.8) — braucht F3
1. FA-Liste öffnen. Konsole: `columnPreferences.getViewConfig().viewKey = 'Unbekannt'`. Eine Spalte ausblenden, 2 s warten.
   **Erwartet:** über der Tabelle links vor CSV/Zahnrad: „Spaltenauswahl konnte nicht gespeichert werden (Fehler 400). Die Anzeige gilt
   nur bis zum Neuladen.“ Zweite Änderung → weiterhin nur **ein** Hinweis.
2. F5. **Erwartet:** Spalte wieder sichtbar, kein Hinweis. Screenshot des Hinweises.

### B4 Anzeigename (TS-98.12) — braucht F4
Benutzerverwaltung, einen Benutzer mit gespeicherter Auswahl der Vorschlagsliste Beschichtung bearbeiten (nicht speichern).
**Erwartet:** „Vorschlagsliste Beschichtung (IDEAL)“ bzw. „… — Summiert (IDEAL)“. Gibt es keinen solchen Benutzer: NICHT PRUEFBAR.

### B5 Hardware-Scanner, was ohne Gerät sichtbar ist (TS-99.1 a, 99.10, 99.12)
1. Startseite, Quelltext per `read_page`/`javascript_tool`: Ist A3 „aktiv“ = an, trägt `<body>` die Attribute `data-hw-scan-*`
   (Präfix wie A3, Abstand), und `hardware-scanner.js` ist geladen; bei aus beides nicht.
2. Mein Profil: Schalter „Dieses Gerät“ sichtbar, wenn A3 an. Nicht umschalten.
3. Hilfe: Abschnitt zum Hardware-Scanner vorhanden (Präfix, Ziel-Regel, Kamera-Fenster).

### B6 Vault- und enaio-Links (TS-100.2–100.4, 100.12)
Je nach A2. Stellen: Artikelinfo (eine Artikelnummer aus B1), Artikel-Liste, Bestandsübersicht, Bewegungshistorie, Stückliste,
FA-Liste, Leitstand, FA-Vervollständigung (Liste und Edit-Kopf einer FA), FA-Abarbeitung.
1. **Vault aus:** kein Vault-Symbol, kein „Im VAULT öffnen“; Artikelnummer steht; keine Lücke. **Vault an:** Link vorhanden,
   `href` enthält die Vorlage aus B0 mit der Nummer. Link nicht öffnen (fremde Adresse).
2. **enaio aus:** keine enaio-Symbole, im Edit-Kopf kein Badge mit Text. **enaio an:** Badges vorhanden, `href` = Vorlage mit Objekt-Id.
   Nicht öffnen.
3. FA-Vervollständigung › Edit und die Listen von FA-Vervollständigung/FA-Abarbeitung: Knopf „Stückliste öffnen“ immer sichtbar,
   Klick öffnet die Stückliste (Seite wechselt, nichts ändern).
4. Hilfe › „Vault- und enaio-Links“: Texte nennen Schalter und Vorlagen.

### B7 Nachtest Terminal (B4 c vom 21.10.) — braucht F1
1. Terminal 65, `123` anmelden, im Scanfeld `1065150` (wählt EG), danach `1059463,EG`.
   **Erwartet:** Gruppe 1059463 offen, keine Auswahlliste; die Auswahl aus `1065150` ist **aufgehoben** (keine Markierung, keine Zeile
   „Gewählt“). Bleibt sie stehen: FEHLER (Kleinbefund aus dem 21.10.). „Wechseln“, abmelden.

### B8 Nachtest Werkbänke und KA (B6.2, Z4 vom 21.10.)
1. `/ProductionWorkplaces`, Filter „Nicht in Sage seit“ = `!(leer)`. Anzahl notieren, mit A4 vergleichen. **Erwartet bei korrigierter
   Quelle:** nur noch alte W1-Werkbänke; „Kanterei W2“ (2350) und Verkauf/Kommissionierung/Verladung nicht mehr dabei. Filter löschen.
2. `/FaHierarchy`, 1059463 bis 1065150 aufklappen, KA-Symbol: `aria-label`. **Erwartet bei korrigierter Quelle:** „KA – Kanten –
   <Werkbank> – <Status>“. Ist A4 nicht korrigiert: Ergebnis „wie 21.10.“ vermerken, kein FEHLER.

### B9 Druck Beschichtungsauftrag mit Fußzeile (TS-92.1, 92.9, 92.12) — braucht A5
1. Beschichtung › Aufträge, Auftrag aus A5, Druckansicht. **Erwartet:** Briefkopf auf Seite 1; Fußzeile mit IDEAL- und AKE-Anschrift,
   www.ideal-ake.at, zweifarbigem Balken; unten rechts „Seite X von Y“; nichts verdeckt.
2. Vorschlagsliste › Summiert ohne Filter, Druckansicht: Fußzeile ja, **keine** Seitenzählung.

### B10 Nachtest Kleinbefunde (Z1, Z5 vom 21.10.)
1. Fenster auf ca. 1380 px Breite (`resize_window`; geht es nicht, vermerken). `/ProductionWorkplaces` als Admin:
   `document.documentElement.scrollWidth` vs. `clientWidth` notieren (Z1 Menüleiste; nicht behoben, nur Stand festhalten).
2. Stückliste: Spalte Quell-Lagerplatz, steht dort „NAN“? (Z5; nur Stand festhalten.)

---

## Teil C — Gerald, von Hand

- [ ] **TS-99 am Netum AIDC** (Checkliste am Ende der Scan-Handler-Spec): zuerst TS-99.1 b (Tottaste `~`), dann 99.2–99.9,
      99.13, 99.14 (Stückliste, Kamera-Fenster nimmt den Scan an) und 99.16 (alle Seiten mit mehreren Scan-Feldern).
- [ ] Artikel-QR von Hannes: Inhalt notieren (`Artikelnummer;…`?) und auf Wareneingang und Stückliste testen.
- [ ] TS-99.15: Zeichenabstand 5/0/-5/501 abgewiesen, 10/500 gespeichert, leer = 50.
- [ ] TS-98.9/98.10/98.13: Netzwerk-Sperre in den DevTools (keine Verbindung, Zurücksetzen, Laden).
- [ ] TS-100.5–100.10: Vorlagen leeren/ändern, CSV, enaio-View-Name ungültig (Dienst, Fehlermail).
- [ ] TS-100.1: AKE-Regression später auf `https://akenet01.ake.at:4445/`.
- [ ] Endstand an IDEAL: Vault aus, enaio laut A2.

## Übersicht

| Schritt | TS | braucht |
| --- | --- | --- |
| B0 | 99.12, 100.1, 100.11 | — |
| B1 | 98.1, 98.2 | — |
| B2 | 98.4, 98.6, 98.7, 98.11 | F2 |
| B3 | 98.8 | F3 |
| B4 | 98.12 | F4 |
| B5 | 99.1 a, 99.10, 99.12 | A3 |
| B6 | 100.2–100.4, 100.12 | A2 |
| B7 | Nachtest 97.5 | F1 |
| B8 | Nachtest 96.1/97.7 | A4 |
| B9 | 92 | A5 |
| B10 | Z1, Z5 | — |

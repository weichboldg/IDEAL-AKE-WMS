---
type: testbericht
title: "Browser-Testbericht Bündel v1.73 bis v1.88 auf DEV v1.94.0 — Teil B"
protokoll: "[[2026-11-08-buendel-v1-73-bis-v1-88-browser-testprotokoll]]"
system: "DEV statt Testsystem: AKE http://localhost:8081 · IDEAL http://localhost:8082 (beide v1.94.0 laut Fußzeile, Windows-Anmeldung)"
datum: 2026-11-08
ausfuehrung: "Claude (Abnahme Teil B auf DEV), Chrome mit Geralds Windows-Anmeldung"
ergebnis: "25 OK, 3 FEHLER, 4 nicht pruefbar, 0 Protokoll passt nicht"
---

# Browser-Testbericht Bündel v1.73 bis v1.88 auf DEV v1.94.0 — Teil B

**Lauf:** 08.11.2026, ca. 10:05–10:35. Teil A war auf DEV nicht gemacht. Der Freigabe-Block F1–F4 war von Gerald erteilt
(Auftrag 2026-11-08).

**Abweichungen vom Protokoll (Auftrag Gerald):**
- DEV statt Testsystem, AKE `localhost:8081`, IDEAL `localhost:8082`. Erwartete Fußzeile v1.94.0.
- Testdaten: `{BESTELLUNG_AKE}` = 2027-168594, `{FA_AKE}` = 2629001, `{FA_IDEAL}` = 1059463 (Sub-FA 1065141).
- Nachtrag Gerald: Der Sync wird mitgetestet. Auf AKE-DEV `/ServiceSettings` habe ich deshalb die fünf Schalter
  eingeschaltet und gespeichert (10:13:52), siehe unten. Den Dienstzyklus habe ich abgewartet (Artikel-Sync-Start 10:26:42).
- Erlaubt war die Freigabe von FA 2629001 im Leitstand. Sie war **nicht möglich**: Die Freigabe-Maske verlangt einen
  Kommissionierer, die Auswahl ist leer (Befund 4). Ich habe mit „Abbrechen“ geschlossen, FA 2629001 ist nicht freigegeben.
- Stand v1.89–v1.94 als Vermerk, nicht als Fehler: Legende „rot hinterlegt“ ist hellrot mit dunklem Text, die Menüleiste
  bricht um (AKE zweizeilig, Stammdaten in Zeile 2).

**Service-Einstellungen AKE-DEV (einzige gespeicherte Änderung):**

| Schlüssel | Ausgang | Ende |
| --- | --- | --- |
| `Sync:ArticlesEnabled` | aus | **an** |
| `Sync:ArtikelLieferantEnabled` | aus | **an** |
| `Sync:KreditorenEnabled` | aus | **an** |
| `Sync:DebitorenEnabled` | aus | **an** |
| `Sync:EinkaufBestellungenEnabled` | aus | **an** |
| `Sync:Quelle:Artikel` | SData | SData (nicht angefasst) |
| `SData:BaseUrl` / `Application` / `Dataset` | `https://sagetest01.ake.at:5493/` / `ol` / `ake_TEST2026;1` | unverändert |

Vor dem Speichern habe ich die Formulardaten vorher/nachher verglichen: genau diese fünf Werte änderten sich (false → true),
sonst nichts. Meldung „Einstellungen gespeichert.“ Die Schalter bleiben laut Gerald an.

**Wiederhergestellt:** Spaltenauswahl Artikelliste (Hauptlieferant aus und wieder an, danach 29 Spalten, alle sichtbar, wie
vorher laut `GET /api/user-view-preferences/Articles`). Spaltenauswahl Kommissionierung (Werkbank aus und an, danach „Auf
Standard zurücksetzen“, `GET /api/user-view-preferences/Picking` = 204 wie vorher). Filter „Nur meine Werkbänke“, „Aufträge
aller Kommissionierer“, Bereich, Status und Spaltenfilter liefen nur über die URL. Ein Aufruf ohne Parameter zeigt wieder die
Standards (geprüft für `/MissingParts` und `/Picking`). „Nur geänderte“ und die Suche in `/Settings` wieder leer. Eingaben in den
FA-Feldern der Buchungsmasken nicht gespeichert, Seiten verlassen. Eigene Tabs geschlossen. Nichts gebucht, nichts eingebucht,
nichts freigegeben, nichts abgehakt, nichts gedruckt, kein JS-Dialog ausgelöst.

## Sync-Lauf AKE-DEV nach dem Einschalten (B0.2)

Dienstzyklus 10:26:42–10:32:36, keine Error-Einträge am 08.11. Warnungen nur „Hauptlagerplatz '…' nicht als WMS-Lagerplatz
gefunden“ (106 Artikel, wie am 07.11.).

| Lauf | Zeit | Status | Zahlen |
| --- | --- | --- | --- |
| Artikel-Sync | 10:26:42–10:31:53 | erfolgreich | Quelle SData. `gelesen=100599, neu=0, aktualisiert=0, hauptlagerplatz_fehlt=106`. ArticleSageDetails: 0 neu, **99.880 aktualisiert** (am 07.11. 0). Merkmale 0/0/0/0. SData-Reads: Artikel 87 S./43.144 Z., Artikel 288 Blöcke/56.736 Z., Artikel 500 Blöcke/99.880 Z., ArtikelVariantenExtended 500 Blöcke/99.880 Z., PpsRessourcenPositionenInfo 49 S./240.552 Z., LagerplaetzeInfo 7 S./3.300 Z. |
| Kreditoren-Abgleich | 10:31:53 | erfolgreich | Kontokorrent (K) 2.287, Adressen 3.843, Ansprechpartner 3.405 gelesen. `gelesen=2287, neu=0, aktualisiert=0, unveraendert=2287, nicht_in_sage=0, moegliche_dubletten=0, wms_eigen=0` |
| Debitoren-Abgleich | 10:31:53–10:31:54 | erfolgreich | Kontokorrent (D) 534, Adressen 3.843, Ansprechpartner 3.405. `gelesen=534, neu=0, aktualisiert=0, unveraendert=534, wms_eigen=0` |
| Artikel-Lieferanten-Abgleich | 10:31:54–10:32:35 | erfolgreich | ArtikelLieferantInfo 34.802 Z., ArtikelVariantenExtended **23.509 Z.** `gelesen=34802, neu=0, aktualisiert=33158, unveraendert=0, entfernt=0, hauptlieferant=15424, artikel_fehlt=1644` |
| Offene Bestellungen (Sage) | 10:32:35–10:32:36 | erfolgreich | EinkaufPositionenInfo 1.059 Z. `positionen=1059, koepfe=341, uebernommen=1059, einheit_abweichend=31, lieferant_gesperrt=0` |

**Schreibt ein Lauf an Sage?** Nein, nach dem Protokoll nicht. Alle fünf Läufe melden nur Lesezugriffe („Quelle SData: …“).
Es gibt keine Zeile „Übergabe an Sage“ oder Outbox, bei Kreditoren und Debitoren `wms_eigen=0`. Einen Sage-seitigen Nachweis
(Änderungsdatum der Konten in Sage) habe ich nicht; das wäre eine Prüfabfrage für Gerald.

**Zeitstempel gegen den Lauf:** Artikelinfo `00722001` „letzte Änderung durch den Artikel-Abgleich: 08.11.2026 10:30“ (vor dem
Lauf: 07.11.2026 09:18). Das passt zum Lauf (Zusatzfelder geschrieben 10:30:31–10:31:14). Die Lieferanten-Karte hat keinen eigenen
Zeitstempel. Kreditor K31013: „Letzter Kreditoren-Abgleich: 07.11.2026 19:30“ (vor dem Lauf gelesen, der heutige Lauf hat
nichts geändert).

## Ergebnis je Schritt

| Schritt | Ergebnis | Beobachtung | Screenshot |
| --- | --- | --- | --- |
| B0.1 AKE | OK | Fußzeile „© 2026 IDEAL-AKE WMS v1.94.0“ | nein |
| B0.1 IDEAL | OK | Fußzeile „© 2026 IDEAL-AKE WMS v1.94.0“ | nein |
| B0.2 AKE | OK | Lauf nach dem Einschalten, siehe Abschnitt oben. Alle vier erwarteten Läufe erfolgreich, `ArtikelVariantenExtended` 23.509 Zeilen, `hauptlieferant=15424`, Kreditoren mit Adressen und Ansprechpartnern, keine Fehlerläufe | nein |
| B1.1 AKE | OK | `/Settings`: 12 Gruppen + Feiertage, 55 Einträge. Suche („Trennzeichen“ → nur `QrTrennzeichen`) und „Nur geänderte“ (20 Einträge) wirken. Jeder Eintrag hat Anzeigename, Key, Beschreibung, „Verwendet in“, Standard, Standort. `QrErstesFeld` und `QrTrennzeichen` sind Auswahllisten (`;`/`,`), Bool-Werte Schalter. `NegativeBuchungLagerplatz` (3.293 Codes) und `BdeDefaultArbeitsgang` (12 Vorschläge) sind Textfeld mit Vorschlagsliste, so in der Spec entschieden. `/ServiceSettings`: 14 Gruppen, 87 Einträge, alle mit Metadaten, alle sechs `Sync:Quelle:*` Auswahl (View/SData/Vergleich), Suche und „Nur geänderte“ vorhanden | nein |
| B1.1 IDEAL | OK | `/Settings` 55, `/ServiceSettings` 90 Einträge, alle mit Metadaten; Qr-Felder und `Sync:Quelle:*` als Auswahl; Suche und „Nur geänderte“ vorhanden | nein |
| B1.2 IDEAL | OK | `Sync:Quelle:Artikel` = SData. Der Sperrhinweis „SData ist hier gesperrt bis P-R1 (Artikel-View '[vw_IDEAL_AKE_WMS_Artikel]' gesetzt …)“ steht im eigenen Eintrag, darunter beginnt „Quelle FA-Struktur“ mit eigenem Hinweis | ja |
| B1.3 AKE | OK | Letzte „Einstellungsprüfung“ 07.11.2026 10:52:18: `ungueltig=1`, `LagerbestellungAktiv`: „1“ ist nicht erlaubt, wirksam „true“ | nein |
| B1.3 IDEAL | NICHT PRUEFBAR | Für „Einstellungsprüfung“ gibt es auf IDEAL-DEV keinen einzigen Eintrag. Der IDEAL-Dienst läuft (FA-Materialisierung 10:01:47), aber der Artikel-Sync bricht ab (Befund 6) | nein |
| B2.1 | OK | `/Creditors` (91 Seiten): Name, Matchcode, PLZ/Ort, Land, Sage-Konto 25/25, Telefon 23/25, E-Mail 24/25 gefüllt; Kurzname leer. Knopf „Kreditor anlegen (Notfall)“ | nein |
| B2.2 | OK | K31013 (`/Creditors/Details/927`): Adresse Pielacher Straße 50, 3390 Melk, AT; UID ATU58386767; „Letzter Kreditoren-Abgleich: 07.11.2026 19:30“; Ansprechpartner „—“. Karte „Artikel dieses Lieferanten · aus Sage (121)“ mit 7 Spaltenfiltern (Server-Mode), CSV, Links auf `/Articles/Info`. K30601 (`/Details/547`): Ansprechpartner „Doris Winkler (+4 weitere)“, Karte lädt, „aus Sage (9166)“, 367 Seiten. Vermerk: statt eines Hinweises bei vielen Zeilen wird geblättert (so in der Spec Q-1/Q-2 gelöst) | nein |
| B2.3 | OK | `/Debitors` (480): Name, Matchcode, PLZ/Ort, Land, Sage-Konto 25/25, Ansprechpartner 20, Telefon 23, E-Mail 18; Knopf „Debitor anlegen (Notfall)“ | nein |
| B3.1 | OK | `/Articles?search=00722001`: Artikelnummer verlinkt auf `/Articles/Info`. Zahnrad: „Hauptlieferant“ und „Letzter Lieferant“ vorhanden (F1: Hauptlieferant aus/an, zurückgestellt) | nein |
| B3.2 | OK | **Nach dem Lauf:** Mengeneinheiten Basis/Lager/Verkauf „lfm, 2 Nachkommastellen“ (vor dem Lauf alle „—“). Lieferanten: Hauptlieferant und letzter Lieferant „"carlo" Loysch GmbH (K31013)“ mit Link `/Creditors/Details/927`; Tabelle VISB00061, 200, lfm, 14, Hauptlieferant Ja. Zeitstempel siehe oben. Vermerk: Die Karte sagt „letzte Änderung durch den Artikel-Abgleich: …“, nicht „aus Sage gelesen am …“ | ja |
| B3.3 | OK | `/Articles/Edit/2963`: Karten „Sage-Zusatzfelder“ und „Lieferanten“ ohne bearbeitbares Feld; Sage-Merkmale `attr-sage-*` readonly; Hauptlagerplatz „aus Sage übernommen (gesperrt): LL;2;28;0“. Nicht gespeichert | nein |
| B3.4 | OK | `86006871`: Sage-Zusatzfelder vorhanden, Fertigungsartikel „Ja“, Bezeichnung 2 „Wanne“ | nein |
| B4.1 | OK | `/WarehouseRequisitions` Lager: Karte „Meine Fehlteile“ 14 (wird nachgeliefert) / 3 (nicht nachgeliefert). Links `/MissingParts?tab=…&type=Lager&mineOnly=true` ergeben 14 bzw. 3 Zeilen. Glas: 0 Fehlteile, keine Karte | nein |
| B4.2 | OK | `/MissingParts`: Haken „Nur meine Werkbaenke“ an, Reiter 14/3, Liste 14. Per Klick aus: Reiter 20/63, Liste 20, URL `mineOnly=false`. Ohne Parameter wieder an | nein |
| B5.1 | **FEHLER** | Hinweis „Stand: 07.11.2026 19:31 · nur normale Bestellungen (EBB) · Mengen in der Basiseinheit …“, Filter Wochen (2 = Einstellung), Lieferant, Bestellung, Artikel, Ziel-Lagerplatz, „nur überfällig“, CSV; 1.017 überfällige Zeilen `table-danger` + „überfällig“ im Hinweis, 4 neutrale Zeilen; Legende vorhanden (hellrot, v1.89). **Aber: kein Zahnrad / keine Spaltenauswahl.** Die Tabelle trägt `data-view-key="PurchaseOrders"`, `GET /api/user-view-preferences/PurchaseOrders` antwortet 400 „Invalid view key: PurchaseOrders“. F1 hier nicht ausführbar | ja |
| B5.2 | OK | `2027-168594` (Schachermayer K31705): Spalten bestellt / offen (Sage) / im WMS erfasst / Rest / Einheit / Ziel-Lagerplatz / Ist-Menge / „Überlieferung bestätigen“. Ziel „— wählen —“ mit „Sage-Platz HR;4;2;3 ist im WMS nicht buchbar – bitte Ziel wählen“ (auf DEV sind nur 8 Testplätze buchbar). Gegenprobe `2027-167378`: Ziel vorbelegt mit GL;0;0;0. Einheit abweichend an `2027-168704`: Ist-Menge „nicht buchbar“, Status „Einheit abweichend (Bestellung RLL, Basis RLL, Lager Stk)“. Kein gesperrter Lieferant vorhanden. Nicht eingebucht | ja (3) |
| B6.1 AKE | **FEHLER** | Spalte „Fertigungsauftrag“ mit Link und Status („offen“, „Achtung: erledigt“), unverknüpft ohne Link mit `title="nicht verknüpft"`; Filter „Fertigungsauftrag“ mit Knopf „WA-/FA-Strichcode scannen“. **Der Link zeigt bei 2602098 („Achtung: erledigt“) und bei 2614027 („offen“, Sage-Status „in Produktion“) eine leere FA-Liste** „Keine Fertigungsaufträge gefunden“, weil `/ProductionOrders?filterOrderNumber=…` ohne `showDone=true` öffnet. Mit `showDone=true` erscheint der FA. 2638001 öffnet korrekt | ja |
| B6.1 IDEAL | NICHT PRUEFBAR | Es gibt keine verknüpfte Buchung auf IDEAL-DEV. Einzige Buchung mit FA: 28.10.2026, 50002134, FA 1065141, ohne Link, `title="nicht verknüpft"` (Altbuchung; die Altdaten-Übernahme `SQL/110` ist Teil C9). Die Prüfkarte in der Umbuchung kennt 1065141 („FA 1065141 (Haupt-FA 1059463) … Status: offen · FA anzeigen“) | ja |
| B6.2 AKE | OK | Inbound/Outbound/Transfer: eigenes Feld „Fertigungsauftrag“ (`list="faVorschlaege"`). Inbound „26290“ → Vorschlag „2629001 – EU253691 – UKW 1-1-E R290 – NordCap Nederland“, Karte „FA 26290 ist im WMS unbekannt …“. Nicht gespeichert | nein |
| B6.2 IDEAL | OK | Transfer: „106514“ → keine Vorschläge (Spec: Vorschläge nur Haupt-FAs, `fa-search?q=1059` liefert 1059463); „1065141“ → Prüfkarte mit Haupt-FA und „FA anzeigen“. Nicht gespeichert | nein |
| B6.3 | OK | `/StockOverview`: drei getrennte Scan-Knöpfe mit Tooltip „Artikel-QR-Code scannen“, „Lagerplatz scannen“, „FA-QR-Code scannen“. Kein Hinweistext mit „immer FA“ auf der Seite | ja |
| B7.1 | OK | `/PickingAreas`: Lager L 10 Standard, Glas G 20, Vorkommissionierung VK 30. Zuordnungsregeln öffnet, Text „Artikel, Kommissionierziel, Lagerort (…), Artikelgruppe. Ohne Treffer: Standardbereich.“ IDEAL gleich | nein |
| B7.2a | OK | `/Picking` (F2): Spalten Prio, Auftrag, FA Nr., Artikelnummer, Bezeichnung, Werkbank, Bereich, Fällig, Status, Kommissionierer. „Aufträge aller Kommissionierer“ + Bereich Lager wirken (URL-Parameter). Mit Status „Alle“: KO-2026-00001/-00002/-00003 zu FA 2614027 (Lager/Glas/Lager). KO-2026-00001 geöffnet: Kopf „Kommissionierauftrag KO-2026-00001 · Bereich Lager · Status Teilweise abgeschlossen“, 149 Positionen mit Menge und Lagerplatz, nur lesbar. Nichts abgehakt. F1: Werkbank aus/an, Standard zurückgesetzt | ja |
| B7.2b | NICHT PRUEFBAR | Aufträge zu FA 2629001 je Bereich: Freigabe nicht möglich, Kommissionierer-Auswahl leer (Befund 4). Offene Aufträge: 0 | nein |
| B7.3 | OK | `/PickingLeitstand`: Knopf „Fehlende Kommissionieraufträge für alle freigegebenen FAs erzeugen“ vorhanden, nicht gedrückt | ja |
| B7.4a | OK | IDEAL: Menü „Kommissionierlisten“ → `/FaHierarchyKommissionierListen/Summiert` mit HauptFA 1059463, Filter Kommissionier-Ziel, PDF-Knopf je HauptFA. Nicht gedruckt | ja |
| B7.4b | NICHT PRUEFBAR | IDEAL hat 0 Kommissionieraufträge (auch mit „Alle“). 1059463 ist im Leitstand nicht freigegeben, eine Freigabe auf IDEAL war nicht beauftragt | nein |
| B8.1 | OK | Artikelinfo `00726502`: EAN 4002872120264 in den Sage-Zusatzfeldern | nein |
| B8.2 | **FEHLER** | Hilfe (AKE und IDEAL): „Die FA aus Feld 3 wird übernommen bei Ein-, Aus- und Umbuchung sowie in der Bestandsübersicht (nur mit QrMitFaNummer); sonst nie.“ Das widerspricht B6.3, der Spec (Anforderung 9, Artikel-QR übernimmt keinen FA mehr) und dem Einstellungstext „Bestandsübersicht seit v1.87.0 nie“ | nein |

## Befunde

1. **B5.1 Offene Bestellungen ohne Spaltenauswahl.** URL `http://localhost:8081/PurchaseOrders`. Kein Zahnrad, die API
   lehnt den View-Key ab: `GET /api/user-view-preferences/PurchaseOrders` → 400 „Invalid view key: PurchaseOrders“. Die Spec
   verlangt „Spaltenauswahl über den Toolbar-Host“ (Wareneingang-Spec, Listen-View-Pattern). Vermutlich fehlt der Key in der
   Liste der erlaubten View-Keys. ![[B5-1-offene-bestellungen-ohne-zahnrad.jpg]]
2. **B6.1 FA-Link aus der Bewegungshistorie führt auf eine leere FA-Liste.** URL `http://localhost:8081/StockMovements`, Link
   `/ProductionOrders?filterOrderNumber=2602098` („Achtung: erledigt“) und `…=2614027` („offen“, Sage „in Produktion“): beide
   „Keine Fertigungsaufträge gefunden“. Erst mit `&showDone=true` erscheint der FA. Gerade beim Warnfall „erledigt“ sieht man
   den FA also nicht. Auffällig ist auch, dass 2614027 im WMS „offen“ heißt, in der FA-Liste aber ausgeblendet ist.
   ![[B6-1-ake-fa-link-2614027-leer.jpg]]
3. **B8.2 Hilfe-Text veraltet.** URL `http://localhost:8081/Help` und `http://localhost:8082/Help`: „… sowie in der
   Bestandsübersicht (nur mit QrMitFaNummer) …“. Seit v1.87.0 übernimmt der Artikel-QR in der Bestandsübersicht keinen FA.
4. **Kein Kommissionierer auf AKE-DEV.** URL `http://localhost:8081/PickingLeitstand`, „Freigeben“ bei 2629001: Die Maske
   „Kommissionier-Freigabe“ verlangt einen Kommissionierer, die Liste enthält nur „— Bitte auswaehlen —“, der Knopf „Freigeben“
   bleibt gesperrt. Grund laut Code: `GetActivePickersAsync` liefert nur aktive Benutzer mit `IsPicker`, auf AKE-DEV gibt es
   keinen. Die Maske sagt nicht, warum die Liste leer ist. Für Teil C muss Gerald einen Benutzer als Kommissionierer
   kennzeichnen. Die bestehenden Aufträge KO-2026-00001…3 sind dem Benutzer „test“ zugeordnet.
5. **Artikel-Lieferanten-Abgleich schreibt alle Zeilen neu.** `http://localhost:8081/SyncLog?service=ArtikelLieferant`:
   07.11. 19:31 `aktualisiert=0, unveraendert=33158`, 08.11. 10:32 `aktualisiert=33158, unveraendert=0`, bei gleicher
   Zeilenzahl. Ebenso Artikel-Sync „ArticleSageDetails: 99.880 aktualisiert“ (07.11.: 0); dort sind die Mengeneinheiten neu
   gefüllt, also erklärbar durch den Versionssprung auf v1.94. Ob der nächste Lauf wieder `unveraendert` meldet, bitte im
   nächsten Zyklus ansehen. Kein Sage-Schreibzugriff erkennbar.
6. **IDEAL-DEV-Dienst ohne Sage-Zugriff.** `http://localhost:8082/SyncLog?service=Article`: 07.11.2026 08:49:23 „Die von der
   Anmeldung angeforderte IDEAL_TEST_2026_05_03-Datenbank kann nicht geöffnet werden … AKENB\svc_idealakewms“. Die
   Einstellungsprüfung ist auf IDEAL-DEV nie gelaufen (B1.3).
7. Vermerk: `einheit_abweichend` stieg von 7 (07.11.) auf 31 (08.11.). Das passt zur Regel aus v1.86 („Bestell- oder
   Basiseinheit ≠ Lagereinheit“), der Lauf vom 07.11. lief noch mit der alten Regel.
8. Vermerk: Auf AKE-DEV sind nur 8 Lagerplätze buchbar (1;1;1, asdfasdf, GL;0;0;0, NAN, qwert, Test, TEST 1, TEST 2). Die
   Vorbelegung des Ziel-Lagerplatzes greift deshalb fast nie (B5.2), das ist Stammdatenstand auf DEV.

## Für Teil C / Gerald

- C1/C2: Vor der Freigabe von 2629001 einen Benutzer als Kommissionierer (`IsPicker`) kennzeichnen (Befund 4). Danach B7.2b
  (Aufträge je Bereich zu 2629001) nachholen.
- B7.4b: Auf IDEAL einen Haupt-FA freigeben (z. B. 1059463), dann prüfen, ob die Aufträge nach Bereich getrennt entstehen.
- B6.1 IDEAL: Erst nach `SQL/110` scharf (C9) gibt es verknüpfte Altbuchungen. Dann prüfen, ob der Link auf die FA-Struktur
  führt; die Buchung zu 1065141 vom 28.10. ist ein Kandidat.
- Befund 2 entscheiden: Soll der FA-Link `showDone=true` mitgeben, oder bei IDEAL/AKE immer auf den Status-unabhängigen
  Einstieg zeigen?
- Befund 5: Nach dem nächsten Zyklus prüfen, ob `ArtikelLieferant` wieder `unveraendert` meldet. Optional als Sage-Prüfabfrage:
  Änderungsdatum der Konten K31013/K30601 in Sage vor/nach 10:31 unverändert?
- Die fünf Sync-Schalter auf AKE-DEV sind an (Ausgangswert aus). Der Dienst liest nun alle 15 Minuten Artikel, Lieferanten,
  Kreditoren, Debitoren und Bestellungen aus `sagetest01`/`ake_TEST2026;1`.
- IDEAL-DEV-Dienst: Datenbank-Anmeldung `IDEAL_TEST_2026_05_03` klären (Befund 6), sonst bleibt B1.3 IDEAL offen.

## Nachtrag Koordination 2026-11-08 11:13 (Befund 5)
Folgeläufe Artikel-Lieferanten-Abgleich AKE-DEV 10:51:25 und 11:10:21: `aktualisiert=0, unveraendert=33158` → Befund 5 erledigt; die
Vollaktualisierung um 10:32 war einmalig (Versionssprung v1.94, neue Einheitenfelder).

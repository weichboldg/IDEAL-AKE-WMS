---
type: testbericht
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
modul: Wareneingang zu Bestellung, Offene Bestellungen, Rahmenabrufe, Sage-Wareneingänge, Lieferverzug-Mail, Bestellvorschläge, Artikelinfo Lieferanten, EAN, Meine Fehlteile
stand: "Bündel v1.99.22 (90bbbbd7), DEV AKE localhost:8081 / AKE_WMS_DEV, DEV IDEAL localhost:8082 / IDEAL_WMS_DEV"
durchgefuehrt: "2026-11-13 20:22–21:06 (Ortszeit DEV-PC)"
---
# Teil B2 – Wareneingang und Einkauf auf DEV

Teil B2 aus [[2026-11-14-wochenend-review-test-anwenderdoku]]. Grundlage: `docs/TESTSZENARIEN.md` im Bündel-Worktree (TS-116 inkl. Teil 2,
TS-128, TS-129, TS-130.2, TS-132, TS-136, TS-137, TS-110, TS-112, TS-113, TS-122) und die Review-Notizen
[[2026-11-14-review-befunde-wareneingang]], [[2026-11-14-review-befunde-lieferverzug]], [[2026-11-14-review-befunde-offene-bestellungen]],
[[2026-11-14-review-befunde-bestellvorschlaege]]. Form wie [[01-kommissionierung]].

**Vorgehen:** Eigener Tab im verbundenen Chrome (die Liste der verbundenen Browser zeigte nur einen Eintrag, „Browser 1“, bereits für die
Sitzung gewählt und auf `localhost:8081` erreichbar – kein anderer Browser gewählt). Teils Oberfläche, überwiegend dieselben GET/POST-Aufrufe,
die die Seiten senden (`fetch` mit Antiforgery-Token im angemeldeten Tab). Wirkung per SQL auf `AKE_WMS_DEV`/`IDEAL_WMS_DEV` und im Web-Log
`D:\Dev\AKE\Web\logs\`. Nicht neu deployt.

**Sage-Schutz vor dem ersten Einbuchen geprüft:** `SageWareneingangBelegAktiv`, `SageLagerbuchungAktiv`, `SageLagerbuchungUmbuchungAktiv` =
`false` (ServiceSettings). Alle Test-Einbuchungen auf Nicht-Sage-Plätze („TEST 1“ Id 3289, „TEST 2“ Id 3290, `SageBuchungErlaubt = 0`).
`SageBookingQueueItems` vorher 16 Zeilen (max. Id 16), nachher unverändert 16. Kein Receipt mit `SageWeVorgang` aus diesem Lauf (alle
`NichtNoetig`). Mails nur im Dienst-PickupDirectory `D:\Dev\mail-pickup\ake` (Web versendet keine Mails – kein Mail-Code im Web-Projekt;
Dienst `appsettings.DEV.json` mit `PickupDirectory`). Lieferverzug im Testbetrieb (`Lieferverzug:Testbetrieb = true`).

**Einschränkungen:**
- Nur ein Windows-Benutzer (gerald.weichbold, Admin) → Rollen-Fälle (Benutzer R nur `stock_read`, Benutzer ohne `stock_keyuser`,
  ohne `einkauf`, ohne `lager_processing`) nicht prüfbar. WE-5 deshalb nur aus Code und Zugriffsmatrix.
- Alles mit eingeschaltetem Sage-Wareneingang ist gesperrt (Schalter bleibt aus): Barcode-Validierung, Platzregel, CSV-Übergabe, Abruf-Kopf
  bei Sage-WE, Kürzel im Memo → NICHT PRUEFBAR (nur Gerald). Prüfbar war der Zustand „Sage-WE aus“.
- `SQL/149` ein zweites Mal ausführen (137.1 Idempotenz) wurde vom Freigabe-Klassifikator der Sitzung abgelehnt → nicht ausgeführt.
- Bestandsänderungen an den Referenzartikeln 00722001/EK002400 bewusst nicht gemacht: deren Bestand liegt nur auf Sage-Plätzen
  (`LL;2;28;0`); eine Ausbuchung dort würde bei späterem Einschalten der Lagerbuchung von `EnqueueMissingAsync` nachgereicht.
- Screenshot-Werkzeug fiel anfangs mit Zeitüberschreitung aus, später ging es (Bilder unten).

## Ergebnis-Tabelle

| TS-Nr. | Standort | Ergebnis | Beleg/Bemerkung |
|---|---|---|---|
| 116.1 Abgleich aus → Hinweis | AKE | NICHT PRUEFBAR | `Sync:EinkaufBestellungenEnabled` muss an bleiben |
| 116.2 Abgleich, Counts, P-W6 | AKE | OK | SyncLogs 5661903 (20:22): `positionen=1167, positionen_ebb=1059, positionen_ebz=108, koepfe=449, uebernommen=1167, artikel_fehlt=0, kreditor_fehlt=0, lagerplatz_fehlt=0, einheit_abweichend=34, menge_fehlt=0`; P-W6 = 0. Negativ (falsches Kennwort) nicht geprüft |
| 116.3 Vorschau Wochen, Einstellung 0/27/„abc“/4 | AKE | OK | 2 Wochen: 1127 Einträge, 4 Wochen: 1142, 26 Wochen: „Liefertermin bis 14.05.2027“; 0/27/„abc“: „… ist nicht erlaubt – erlaubt: eine ganze Zahl von 1 bis 26“, 4 gespeichert und vorbelegt, danach zurück auf 2 |
| 116.4 Benutzer R ohne Knopf | AKE | NICHT PRUEFBAR | kein Benutzer nur `stock_read` |
| 116.4 Schalter `WareneingangBestellungAktiv` aus | AKE | OK | `/PurchaseOrders`, `/Receive`, `/SageWareneingaenge` je 404; Menü ohne „Offene Bestellungen“, „Wareneingang zu Bestellung“, „Sage-Wareneingänge“; danach wieder an (200) |
| 116.5 Teillieferung 2,5 | AKE | OK | 2027-168321 Pos. 1 (EK009055): „1 Position(en) eingebucht (davon 1 Teil-, 0 Überlieferung).“; Receipt 13 / Bewegung 19533, Lieferart Teillieferung, Maske „im WMS erfasst“ steigt; Eingabe der Belegnummer mit Leerzeichen „ 2027-168321 “ öffnet die Bestellung. Hardware-Scan nicht geprüft |
| 116.6 Negativ Zeilen | AKE | OK | Ist 31 > Rest 30 ohne Häkchen: „Überlieferung (31 > Rest 30) bitte bestätigen.“; Menge 0 neben gültiger: „Menge muss größer als 0 sein.“, Eingaben bleiben stehen; „abc“: „„abc“ ist keine Zahl.“; −1, 1,2345 („Höchstens 3 Nachkommastellen …“), Platz leer/nicht buchbar: „Bitte einen Ziel-Lagerplatz wählen.“; nichts gebucht (Receipts unverändert) |
| 116.6 gesperrte Position / unbekannte Bestellung | AKE | OK | 2027-166490 (lfm/Stk): „Einheit abweichend (Bestellung lfm, Basis Stk, Lager lfm) – nicht buchbar, bis geklärt“, kein Mengenfeld; 2099-999999: „Bestellung „2099-999999“ nicht gefunden (nur offene Bestellungen und Rahmenabrufe aus Sage, Stand des letzten Abgleichs).“ |
| 116.6 Negativ: Doppel-Absenden (zwei gleiche POSTs) | AKE | **FEHLER** | = WE-1 reproduziert: zwei gleichzeitige POSTs je 3 Stk → zwei Receipts 14/15 (Bewegungen 19534/19535, 20:25:30.162/.164). ![[WE-1-doppeltes-absenden-zwei-einbuchungen.jpg]] |
| 116.6 Negativ: Doppelklick am Desktop-Chrome | AKE | OK | echter Doppelklick auf „Einbuchen“: nur ein POST im Web-Log (20:25:48), ein Receipt (16) – Chrome unterdrückt das zweite Absenden; Handgerät/Netz-Wiederholung nicht prüfbar |
| 116.7 Lieferant gesperrt | AKE | NICHT PRUEFBAR | `lieferant_gesperrt=0` im Cache, Sage-Testsystem nötig |
| 116.8 Farbe überfällig + Legende | AKE | OK | 4 Wochen, alle Seiten: 1123 rote Zeilen alle mit Hinweis „überfällig“, 19 nicht rote ohne; Legende „rot hinterlegt = überfällig …“ |
| 116.9 Einheitensperre / Ziel-Platz nicht buchbar | AKE | OK | 2027-168321: „Sage-Platz HR;9;2;18 ist im WMS nicht buchbar – bitte Ziel wählen“; Sperre siehe 116.6 |
| 116.10 Zahnrad | AKE | OK | Toolbar-Host vorhanden, `GET /api/user-view-preferences/PurchaseOrders` → 204 |
| 116.11, 116.13, 116.14, 116.16–116.18, 116.20–116.21, 116.26–116.29 | AKE | NICHT PRUEFBAR | brauchen eingeschalteten Sage-Wareneingang bzw. schreiben nach Sage (nur Gerald) |
| 116.12 Lieferschein ohne Pflicht / Pflicht / 21 Zeichen | AKE | OK | „ LS-4711 “ → `Lieferschein = 'LS-4711'` (Receipt 13); Pflicht an: ohne bzw. nur Leerzeichen „Bitte die Lieferschein-Nummer des Lieferanten eingeben.“; 21 Zeichen: „Lieferschein-Nr. höchstens 20 Zeichen (Sage-Feld EKLieferschein).“; nichts gebucht; Pflicht wieder aus |
| 116.15 Liste „Sage-Wareneingänge“ Filter | AKE | OK | Status „übergeben“ 3, Bestellung 2027-168715 → 2, Zeitraum 13.11. → 0 (Leerzustand „Keine Sage-Wareneingänge.“), Spaltenfilter Lieferschein `LS-4711` → 1, Barcode → 1, CSV `wartet` → 0. Rollen-Teil nicht prüfbar |
| 116.19 Positionsmemo | AKE | OK | „Kante beschädigt“ an Bewegung 19533 (Notiz); 201 Zeichen: „Positionsmemo höchstens 200 Zeichen.“, nichts gebucht; Schalter aus → Receipt „nicht nötig“ |
| 116.22 Einstellungen Grenzen | AKE | OK | MaxWiederholungen 11: „… eine ganze Zahl von 0 bis 10“; Abstand 4 und 1441: „… von 5 bis 1440“; nichts gespeichert (Werte 3/15 unverändert) |
| 116.23 Q-2-Hinweis, „Erneut senden“ bei aus | AKE | OK | Receipt 13 per SQL kurz auf „Klären“: Liste und `/ServiceSettings` „⚠ Sage-Wareneingang ist aus – 1 Eintrag offen, werden nicht gesendet. …“; „Erneut senden“: „Nicht eingereiht: Der Sage-Wareneingang ist ausgeschaltet …“ (Meldung doppelt = WE-3) |
| 116.24 Menü-Block | AKE | OK | Menü „Lager“: „Offene Bestellungen“, „Wareneingang zu Bestellung“, „Sage-Wareneingänge“; Seitentitel „Sage-Wareneingänge“; Hilfe beschreibt Klären/Erneut senden |
| 116.25 System-Kürzel negativ | AKE | OK | `dev`, `12`, `ABCDEF`, `D%` je „… ist nicht erlaubt – erlaubt: leer oder 1 bis 5 Zeichen, Großbuchstaben und Ziffern, beginnend mit einem Buchstaben …“; Wert `DEV` unverändert |
| 116.30 Kürzel leer | AKE | NICHT PRUEFBAR | nur automatisiert |
| 116 (Maske) | IDEAL | OK (Hinweis) | `/PurchaseOrders` 200, 1022 Einträge, „Stand: 07.11.2026 19:25“ – IDEAL-DEV hat einen alten Cache (1059 Zeilen, Abgleich aus); `/PurchaseOrders/SageWareneingaenge` 200 |
| 129.1 Barcode-Feld aus, POST ignoriert | AKE | OK | Schalter kurz aus: kein Feld; POST mit `barcode=QA-IGNORE-129` gebucht, Receipt 20 `Barcode = NULL`; Schalter wieder an |
| 129.2 Feld gesperrt bei Sage-WE aus | AKE | OK | Feld `disabled`, Text „Sage-Wareneingang ist aus – für diese Buchung entsteht keine CSV-Datei.“, kein Kamera-Knopf; Buchen ohne Barcode geht; POST mit Barcode: „Barcode nicht möglich: Der Sage-Wareneingang ist aus – …“ |
| 129.3–129.8, 129.10–129.15, 129.17 | AKE | NICHT PRUEFBAR | brauchen Sage-WE an bzw. CSV-Freigabe/Gerät/zweiten Benutzer |
| 129.9 CSV-Pfad negativ | AKE | OK | `C:\temp` und `abc`: „„Wareneingang-CSV: Pfad“: „…“ ist nicht erlaubt – erlaubt: nur ein UNC-Pfad wie \\server\freigabe\ordner oder leer.“; Wert unverändert |
| 129.16 Spaltenfilter CSV/Barcode | AKE | OK | `colf_barcode=AKE1235564654` 1 Zeile, `colf_csv=wartet` 0; CSV-Export nicht geprüft |
| 129.18 IDEAL | IDEAL | OK | `WareneingangCsvAktiv = false`, 0 Protokollzeilen „WareneingangCsv%“ |
| 130.2.1/130.2.2 FA-Hinweis Mischplatz | AKE | OK (Daten) | 00393295 auf TEST 2: 2 ohne FA + 1 mit FA 2627001; `GET /api/stock/by-order/2627001` → TEST 2, 3,000 Stk (Gesamtbestand am Platz, Variante A); Darstellung im Alert nicht geklickt |
| 130.2.4 FA ohne Bestand | AKE | OK | `/api/stock/by-order/9999998` → `[]` |
| 130.2.3 / 130.2.5 | AKE | NICHT PRUEFBAR | in diesem Lauf nicht durchgeführt |
| 132.1.1/132.1.2 Abgleich EBB+EBZ | AKE | OK | Info-Zeilen „EBB: … 1059 Zeile(n)“, „EBZ: … 108 Zeile(n)“; Belegkennzeichen nur EBB/EBZ |
| 132.1.3 Belegnummer zu mehreren Bestellungen | AKE | OK | Abfrage 0 Treffer → Meldungsweg nicht auslösbar |
| 132.1.4 Filter Hinweis „Rahmenabruf“ | AKE | OK | 26 Wochen: 106 Einträge (108 − 2 voll erfasst), alle Hinweis beginnt mit „Rahmenabruf“, „geliefert (Sage)“ = „–“ mit Tooltip; Seite 2 = 26–50 |
| 132.1.5 Abruf gegen Sage | AKE | NICHT PRUEFBAR | Sage-Maske nötig; Cache 2023-147973 = 2.400 Stk (passt zur Spec) |
| 132.1.6 Stand-Text | AKE | OK | „Bestellungen (EBB) und Rahmenabrufe (EBZ); bei Abrufen: offen = Abrufmenge, abzüglich im WMS erfasster Mengen“ |
| 132.1.7 Abruf Einheit abweichend | AKE | OK | 2027-166490 (siehe 116.6) |
| 132.1.8 Benutzer R | AKE | NICHT PRUEFBAR | kein Benutzer R |
| 132.2.1 Abruf öffnen | AKE | OK | 2026-162450: Kopf „Rahmenabruf“ (Text), „Offen = Abrufmenge; in Sage direkt gebuchte Abruf-Lieferungen zieht das WMS nicht ab.“; Barcode gesperrt |
| 132.2.2/132.2.4/132.2.5 Abruf buchen | AKE | OK | 3 → Receipt 17 `NichtNoetig`; 28: „Überlieferung (28 > Rest 27) bitte bestätigen.“; 27 → Rest 0 (Receipt 18), Maske bleibt offen mit Mengenfeld |
| 132.2.6/132.2.7 ausgeblendet + Filter | AKE | OK | ohne Filter „2 Rahmenabrufe im WMS vollständig erfasst – ausgeblendet …“; mit `auchErfassteAbrufe=true` Abruf sichtbar, „Eintraege 1–1 von 1“ |
| 132.2.8 EBB mit Rest 0 | AKE | NICHT PRUEFBAR | keine EBB voll erfasst (nicht angelegt) |
| 132.2.9 Queue | AKE | OK | `SageBookingQueueItems` unverändert 16 (Schalter aus, Nicht-Sage-Platz) |
| 132.2.10 unbekannte Belegnummer | AKE | OK | siehe 116.6 |
| 132.3, 132.4, 132.5 | AKE | NICHT PRUEFBAR | brauchen Sage-WE an / Dienst-Stopp bzw. E2 |
| 132.7 Hilfe | AKE | OK | Absatz „Rahmenabrufe (seit v1.99.15)“ mit Grenze; „werden (noch) nicht angezeigt“ und „Rahmenabrufe nie“ nicht mehr vorhanden |
| 137.1 Schema | AKE | OK | `SagePurchaseOrderLines.Belegnummer`, `PurchaseOrderReceipts.Belegnummer`/`SageWeBelegnummer`, `PurchaseOrderReminders.Belegnummer`, `ArticleSuppliers.LetzteBestellungBeleg` = 90; `IX_SagePurchaseOrderLines_Belegnummer` vorhanden. Zweiter Lauf SQL/149 nicht ausgeführt (siehe Einschränkungen) |
| 137.2 45/46 Zeichen „Als übergeben markieren“ | AKE | OK | Receipt 13 kurz auf „Klären“: Eingabefeld `maxlength=45`; 45 Zeichen gespeichert (`LEN = 45`, Liste zeigt volle Nummer); 46 per POST: „Nicht markiert: Bitte die Sage-Belegnummer angeben (höchstens 45 Zeichen).“; danach per SQL zurückgesetzt |
| 137.3 Layout Spalte „Überlieferung“ | AKE | OK (Desktop) | Spalte „Überlieferung“ rechts neben „Ist-Menge“, Trefferfläche Label 44 × 44 px, alle Zellen `vertical-align: middle`. Bei 1523 px Fensterbreite liegt die Spalte rechts außerhalb (Tabelle quer scrollbar). Tablet/Terminal nicht geprüft |
| 137.4 Überlieferung ohne/mit Häkchen | AKE | OK | siehe 116.6/132.2 |
| 137.5 Hinweis Lagerbestellung | AKE | OK | 2027-168939 (EK005782): „Offener Bedarf zu diesem Artikel: Lagerbestellung Nr. 41 – A1, …: 1 Stk“, Link `/WarehousePicking/Details/41` |
| 137.6 Hinweis teilgeliefert | AKE | OK | 2027-168883 (00720623): Lagerbestellungen Nr. 8 und Nr. 15 (Status Teilgeliefert, Fehlteil „wird nachgeliefert“) je Rest 1 Stk; geschlossene Nr. 16 und Entwurf Nr. 22 (00720624) ohne Hinweis |
| 137.7 Hinweis Bedarfsmeldung | AKE | OK (Hinweis) | 2027-168530 (00395030): „Bedarfsmeldung FA 2625183 – …: 4“ – ohne Einheit, weil `PartRequisitions.Unit` NULL; Link `/PartRequisitions?searchTerm=00395030`. Dort zugleich „Lagerbestellung Nr. 50“ – das ist eine **Glasbestellung** (Seite „Glasbestellung #50“), siehe N-7 |
| 137.8 nur Anzeige | AKE | OK | 1 Stk auf 00395030 eingebucht (Receipt 19): PartRequisition 14 bleibt „Offen“ (`ModifiedAt` NULL), WarehouseRequisition 50 unverändert |
| 137.9 / 137.10 Schalter aus | AKE | OK | `LagerbestellungAktiv=false`: nur Bedarfsmeldung; `BestellungenAktiv=false`: nur Lagerbestellung; beide wieder `true` |
| 137.11 kein Bedarf / Entwurf / geschlossen | AKE | OK | 2027-166725 (Entwurf Nr. 47, EK019355): kein Hinweis an allen 6 Zeilen |
| 137.12 gesperrte Position | AKE | OK | 2027-166490: kein Mengenfeld, keine Überlieferungs-Checkbox |
| 137.13 Kontrast | AKE | OK | Link `text-decoration: underline`, gelber Rand `2.4px solid rgb(255,193,7)` nur zusätzlich, Text „Offener Bedarf …“; Graustufen-Sicht nicht eingestellt |
| 128.10 / 128.22 Einstellungen negativ | AKE | OK | Empfänger `a@b.at, abc@`: „„abc@“ ist keine gültige E-Mail-Adresse.“; Antwortadresse `Einkauf <e@b.at>`: keine gültige Adresse; Tage 61 „… 0 bis 60“; Uhrzeit 24 „… 0 bis 23“; Absender zwei Adressen „… genau eine E-Mail-Adresse“; KreditorTage 61; MaxAlterTage 3651 und −1 abgelehnt; nichts gespeichert |
| 136.1 Gate an, Menü | AKE | OK | `EinkaufAktiv = true`: `/PurchaseProposals` 200; Benutzer B/C und Gate aus nicht prüfbar (Einstellung bleibt an, ein Benutzer) |
| 136.2/136.3/136.4/136.5 Bestand/Bedarfe/Bestellungen bewegen | AKE | NICHT PRUEFBAR | keine Bestandsänderung an Sage-Plätzen (siehe Einschränkungen) |
| 136.6 letzte Bestellmenge 00722001 | AKE | OK | „letzte Bestellmenge“ 400, „letzte Bestellung am“ „09.04.2026 2027-168142“; Protokoll ArtikelLieferant 20:22: `letzte_bestellung_positionen=38047, …_ebz=1759, …_gefunden=10909`, Laufzeit EBB 251 s |
| 136.6a/b/c/d/e, 136.7 | AKE | NICHT PRUEFBAR | 00722001 steht bei Bestand 200 über Meldebestand 85 (kein Vorschlag); Fallback-Filter `Fallback` → 0 Zeilen (kein solcher Artikel auf DEV) |
| 136.6f Einheit Basis ≠ Lager | AKE | OK | EK003604: Hinweis „Einheit Basis ≠ Lager – letzte Bestellmenge nicht verwendet“, Grund „keine letzte Bestellung – Menge prüfen, auf Mindestbestellmenge aufgerundet“, Spalte leer |
| 136.6g Filter letzte Bestellmenge | AKE | OK | `>=400` → 3 Einträge, kleinster Wert 400 |
| 136.8 Banner | AKE | OK | „5 von 2814 Artikeln ohne Sage-Zusatzfelder …“, „30 von 2814 Artikeln ohne Hauptlieferant im WMS …“, „16 offene Bestellpositionen mit abweichender Einheit – nicht eingerechnet …“, „19 Zeilen ohne letzte Bestellung und ohne Sage-Bestellmenge …“ |
| 136.9 Liste, Pagination, Sortierung | AKE | OK | 50 Vorschläge, Seite 2 = 26–50; Sortierung Lieferant (Calorflex, Castolin, Danfoss …), ohne Lieferant zuletzt; `verfuegbar <0` → 0 Zeilen; Zahnrad/CSV nicht geprüft |
| 136.10 IDEAL-Regression | IDEAL | OK | kein Menü „Einkauf“; `/PurchaseProposals` → Startseite mit „Einkauf – Bestellvorschläge ist deaktiviert (Einstellung „Einkauf aktiv“).“ |
| 136.12 / 136.13 | AKE | NICHT PRUEFBAR | `EinkaufAktiv` darf nicht geändert werden; kein Benutzer C |
| 136.17 Filter ohne Treffer | AKE | OK | „Kein Artikel im gewählten Filter.“ |
| 112.1–112.3 Artikelinfo Lieferanten | AKE | OK | 00722001: Hauptlieferant und letzter Lieferant „"carlo" Loysch GmbH (K31013)“ als Link `/Creditors/Details/927`, Tabelle mit VISB00061, 200 lfm, 14 Tage, „Ja“; Kreditor-Details mit Karte „Artikel dieses Lieferanten … (121)“ |
| 112.5 | AKE | OK | Gruppe „Bestand und Dispo (Sage)“ ohne Haupt-/Letzter-Lieferant-Zeilen |
| 112.6 / 112.7 | AKE | OK | 00713171: „Für diesen Artikel liegen noch keine Sage-Zusatzfelder vor.“, Hauptlieferant „—“; 00000905: „Keine Artikel-Lieferanten aus Sage gelesen. … Sync:ArtikelLieferantEnabled …“ |
| 112.10 | IDEAL | OK | Artikel 0039680: Seite 200, Hinweis Sage-Zusatzfelder; Lieferanten-Tabelle vorhanden |
| 110.1 (API) / 110.4 | AKE | OK | `/api/articles/by-ean?ean=4002872120264` → 00726502, `9007646008637` → EK011488, `1111111111111` → leer; Kamera/Hardware-Scanner nicht geprüft |
| 110.2, 110.3, 110.5–110.8 | AKE | NICHT PRUEFBAR | Gerät/Mehrtreffer-SQL nicht im Lauf |
| 113.1/113.2 Karte „Meine Fehlteile“ | AKE | OK | `/WarehouseRequisitions`: „15 Fehlteile (wird nachgeliefert) aus 11 Bestellungen“, Link `type=Lager&mineOnly=true`; `/MissingParts?type=Lager`: 15 Einträge = Karte, Haken an, Werkbank-Auswahl „— Alle —, A1“ |
| 113.6 / 113.8 / 113.11 | AKE | OK | ohne Haken 21 Einträge, 77 Werkbänke; fremde Werkbank: „⚠ Die gewählte Werkbank ist dir nicht zugeordnet – die Liste ist deshalb leer.“; `/MissingPartsLager` 21 |
| 113 Smoke | IDEAL | OK | `/MissingParts`, `?type=Glas`, `/MissingPartsLager`, `/WarehouseRequisitions`, `/PartRequisitions` je 200 |
| 122.1–122.8 Meldebestand-Mail | AKE | NICHT PRUEFBAR | `Notifications:MeldebestandEnabled = false`, Empfänger leer, Takt 60 min ab Dienststart (nicht ohne Dienst-Neustart steuerbar) |
| TS-18 / Bedarfsmeldung (im Bündel geändert) | AKE | OK | nur die gemeinsamen Lese-Abfragen – über 137.5–137.11 geprüft |

### Lieferverzug-Lauf (TS-128, TS-132.6)

Laut Auftrag die heutigen `PurchaseOrderReminders` nur auf DEV gelöscht (1120 Zeilen, Id 11886–13012, alle `Art = 0` aus dem Lauf 17:02, in dem
trotz 99 Kreditoren keine Kreditor-Mail entstand), dann den nächsten Dienst-Zyklus abgewartet. Lauf 20:42:14 (SyncLogs 5661928): `faellig=1119,
kreditoren=98, einkauf_gesendet=1, testbetrieb_umgeleitet=90, ohne_adresse=5, zu_klaeren=75, rahmenabruf_ausgenommen=34, fehler=0`.
91 neue `.eml` in `D:\Dev\mail-pickup\ake` (`20261113-204214-*`), alle `To: gerald.weichbold@ake.at` (Einkauf-Empfänger); keine externe Adresse
als Empfänger. Neue Zeilen: `Art 0` 1119 (101 Lieferanten), `Art 1` 999 mit `Testbetrieb = 1` (90 Lieferanten).

| TS-Nr. | Standort | Ergebnis | Beleg/Bemerkung |
|---|---|---|---|
| 128.1 Schalter aus | AKE | NICHT PRUEFBAR | `Lieferverzug:MailAktiv` bleibt an; IDEAL hat ihn aus und keinen Lauf „LieferverzugMail“ |
| 128.2 Fälligkeit + Rest-Regel | AKE | OK | 2027-168321 in Sammelliste und Kreditor-Mail mit „Rest 20,5 Stk“ (EK009055) und „Rest 29 Stk“ (00393295) = Spalte „Rest“ der Maske nach den Test-Einbuchungen |
| 128.3 Sammelliste, Gruppen, Link | AKE | OK | Betreff „Lieferverzug: 1119 Bestellposition(en) bei 101 Lieferant(en), davon 75 zu klären – 13.11.2026“, Gruppen „Name (Konto) – Kreditor-Mail: …“, Link „Zu den offenen Bestellungen: https://akenet01.ake.at:4444/PurchaseOrders?nurUeberfaellig=true“, Fußzeile mit Firmenadresse und www.ideal-ake.at. Folgezyklus: siehe „Nachtrag Folgezyklus“ |
| 128.4 Testbetrieb | AKE | OK | je Lieferant eine Mail an den Einkauf, Betreff „TESTBETRIEB – wäre an <Name>, <Adresse> gegangen – Erinnerung: Lieferverzug / Reminder: overdue delivery – AKE …“, Status in der Sammelliste „TESTBETRIEB – an Einkauf umgeleitet (wäre an … gegangen)“, Count `testbetrieb_umgeleitet=90`, Zeilen `Testbetrieb = 1` |
| 128.5 Echtbetrieb | AKE | NICHT PRUEFBAR | nur mit Test-Postfächern als Kreditor-Adresse; nicht umgestellt |
| 128.6 Sonderfälle | AKE | OK (teilweise) | „Kreditor-Mail: keine Adresse“ (z. B. AHT Cooling Systems K32332), Count `ohne_adresse=5`; gesperrt/ohne Kreditor/inaktiv im Cache nicht vorhanden |
| 128.7, 128.8, 128.11–128.15, 128.17, 128.18, 128.20, 128.21, 128.25 | AKE | NICHT PRUEFBAR | brauchen Umstellung von Dienst-Einstellungen, synthetische Cache-Zeilen oder SMTP; nicht im Lauf |
| 128.9 Darstellung | AKE | OK (Text) | multipart/alternative mit text/plain + text/html + eingebettetem `image/jpeg` (Logo); Kreditor-Mail zweisprachig („Guten Tag, … / Good day, …“, „bestellt/ordered … offen/open … Liefertermin/delivery date“); Darstellung im Mailclient nicht geprüft |
| 128.16 zwei Schwellen | AKE | OK (Einstellung 3/3) | Einleitung „… 3 oder mehr Tage nach dem Liefertermin … Kreditor-Mails gehen frühestens 3 Tage nach dem Liefertermin raus …“; Schwellen nicht verstellt |
| 128.19 Altersgrenze 180 | AKE | OK | Abschnitt „Zu klären – Liefertermin älter als 180 Tage (keine Erinnerung an Lieferanten)“ mit „Bitte in Sage prüfen: geliefert, storniert oder Termin anpassen.“, 4 × „Bestellung“ + 71 × „Rahmenabruf“ = `zu_klaeren=75`, Betreff „… davon 75 zu klären …“ |
| 128.23 Rahmenabrufe nicht an Lieferanten | AKE | OK | `KreditorMitRahmenabrufen = false`: Abrufe in der Sammelliste („geliefert –“), Status z. B. „ACR … – 7 Rahmenabruf/-abrufe nicht an Lieferanten (Einstellung)“, Count `rahmenabruf_ausgenommen=34`; Schalter an nicht geprüft |
| 128.24 Lief.-Art.-Nr. | AKE | OK | Kreditor-Mail „Lief.-Art.-Nr./supplier item no. 900235.024“ (Störk), Sammelliste „Lief.-Art.-Nr. VISB00041“; Zeilen ohne Eintrag ohne Nummer (z. B. ACR EK007071) |
| 128.26 gleicher Lieferantenname | AKE | OK | Gruppenkopf und Zeile „Zu klären“ zeigen „ACR Automotive Components Reiter GmbH (K30072)“ |
| 132.6.1 Abruf in den Mails | AKE | OK | Abrufe mit „geliefert –“ in Sammelliste und „Zu klären“; nicht in der Kreditor-Mail (Schalter aus) |
| 132.6.3 voll erfasster Abruf fehlt | AKE | OK | 2026-162450 (im Lauf voll erfasst) kommt in keiner der 91 Mails vor |
| 132.2.3 Rest nach Abgleich | AKE | OK | nach dem Abgleich 20:42 bleibt 2026-162450 bei Rest 0 und ausgeblendet |

**Nachtrag Folgezyklus (128.3, Tagessperre):** Der nächste Dienst-Zyklus (Start 20:57, Offene Bestellungen 21:01:58) startete **keinen** Lauf
„LieferverzugMail“ – keine Protokollzeile, keine neue `.eml` (weiter 476 Dateien), `PurchaseOrderReminders` unverändert (Art 0: 1119, Art 1: 999).
Keine zweite Mail am selben Tag → OK.

**Zählung:** OK 79 · FEHLER 1 · NICHT PRUEFBAR 19 (Zeilen beider Tabellen; Einträge „OK (Hinweis/teilweise/Desktop/Daten/Text)“ als OK
gezählt; Sammelzeilen wie „116.11, 116.13 …“ zählen einmal).

## Nachmessung Review-Befunde

| Befund | Ergebnis | Beleg |
|---|---|---|
| WE-1 Doppel-Absenden bucht doppelt | **reproduziert** (serverseitig) | zwei gleichzeitige POSTs → Receipts 14/15, Bewegungen 19534/19535. Echter Doppelklick im Desktop-Chrome: nur 1 POST. ![[WE-1-doppeltes-absenden-zwei-einbuchungen.jpg]] |
| WE-2 nicht atomar | nicht messbar ohne Code-Eingriff | Prüfung vor dem Speichern hält bei allen Zeilenfehlern (0 Bewegungen) |
| WE-3 Meldung doppelt in „Sage-Wareneingänge“ | **reproduziert** | „Nicht eingereiht: …“ und „Nicht markiert: …“ je zweimal. ![[WE-3-sage-wareneingaenge-meldung-doppelt.jpg]] |
| WE-5 Link Lagerbestellung ohne Recht | im Browser nicht prüfbar, im Code **bestätigt** | Wareneingang erlaubt `picking`, Link-Ziel nicht (`CurrentUserService.cs:100-101` gegen `:125-126`) |
| LV-1 Tagessperre an jeder Zeile | nicht reproduzierbar (kein SMTP-Fehler erzeugbar) | siehe Notiz; Sperre im Folgezyklus siehe „Nachtrag Folgezyklus“ |
| LV-3 Fehlermail je Zyklus bei Grenze | nicht beobachtbar | Grenze gilt nur im Echtbetrieb; DEV im Testbetrieb |
| BV-1 Fallback ohne Einheiten-Sperre | latent, auf DEV ohne Treffer | 0 Artikel mit Basis ≠ Lager, Meldebestand, Sage-Bestellmenge > 0 und ohne letzte Bestellung; EK003604 zeigt die Sperre korrekt |

Die Zeilen „DEV-Nachmessung 2026-11-14“ stehen in [[2026-11-14-review-befunde-wareneingang]] (WE-1, WE-2, WE-3, WE-5),
[[2026-11-14-review-befunde-lieferverzug]] (LV-1, LV-3) und [[2026-11-14-review-befunde-bestellvorschlaege]] (BV-1).

## Neue Befunde

Nicht behoben (Schranke 1). Nummern fortlaufend nach [[01-kommissionierung]] (N-1 … N-6).

### N-7 – Hinweis „Offener Bedarf“ nennt Glasbestellungen „Lagerbestellung“ (niedrig)
- **Wo:** `IdealAkeWms/Views/PurchaseOrders/Receive.cshtml:152` (fester Text „Lagerbestellung“), Abfrage
  `PurchaseProposalRepository.GetOffeneLagerbestellungenAsync` (`:84-96`) ohne `WarehouseRequisition.Type`.
- **Befund:** AKE-DEV 2027-168530 (00395030): „Lagerbestellung Nr. 50 – G1, …: 1 Stk“; `/WarehousePicking/Details/50` heißt „Glasbestellung #50“
  (`Type = 2`). Dieselbe Abfrage zählt Glasbestellungen auch in den Bestellvorschlägen als „offene Bedarfe“. Ob Glas mitzählen soll, ist fachlich
  offen; die Beschriftung ist in jedem Fall irreführend.
- **Vorschlag:** Typ mitlesen und „Glasbestellung Nr. …“ bzw. „Lagerbestellung Nr. …“ schreiben; mit dem Einkauf klären, ob Glasbestellungen in
  die Bestellvorschläge gehören. Test: Glasbestellung zu Artikel X → Hinweis „Glasbestellung“.

### N-8 – Bedarfsmeldung ohne Einheit: Hinweis zeigt nur die Zahl (Hinweis)
- **Befund:** „Bedarfsmeldung FA 2625183 – …: 4“ ohne Einheit, weil `PartRequisitions.Unit` NULL ist (PartRequisition 14, Altdaten). TS-137.7
  erwartet „<Menge> <Einheit>“. Ob NULL-Einheiten in den Bestellvorschlägen als „andere Einheit“ zählen (TS-136.16), nicht geprüft.
- **Vorschlag:** Bei fehlender Einheit die Artikel-Einheit anzeigen oder „(Einheit fehlt)“. Prüfabfrage:
  `SELECT COUNT(*) FROM PartRequisitions WHERE Status='Offen' AND Unit IS NULL`.

### N-9 – Maske „Wareneingang zu Bestellung“: Spalte „Überlieferung“ bei 1523 px außerhalb des Bildes (Hinweis)
- **Befund:** Bei 1523 px Fensterbreite ist die Positionstabelle quer scrollbar. „Ist-Menge“ steht am rechten Rand, „Überlieferung“,
  „Positionsmemo“ und „Status“ sind erst nach Querscrollen sichtbar (Sichtprüfung 2027-168530). Am Terminal (schmaler) noch stärker.
- **Vorschlag:** Mit dem Lager klären, z. B. „bestellt“/„offen (Sage)“ über das Zahnrad ausblendbar machen oder den Artikeltext umbrechen. Kein
  Fehler gegen die Spec (137.3 verlangt nur die Reihenfolge).

### N-10 – IDEAL-DEV zeigt alte Bestellungen aus dem Cache (Hinweis, nur DEV)
- **Befund:** IDEAL-DEV hat `WareneingangBestellungAktiv = true` und `Sync:EinkaufBestellungenEnabled = false`. Der Cache enthält 1059 Zeilen vom
  07.11.2026 19:25 („Stand: 07.11.2026 19:25“, 1022 Einträge). TS-116 sagt „IDEAL ohne SData-Endpunkt“. Auf IDEAL-DEV kann damit gegen
  veraltete Bestellungen gebucht werden.
- **Vorschlag:** Auf IDEAL-DEV den Schalter aus oder den Cache leeren (Gerald). In der Deploy-Checkliste festhalten, dass
  `WareneingangBestellungAktiv` auf IDEAL-PROD aus bleibt, solange kein Abgleich läuft.

### Beobachtung ohne Fehler
- Der Dienst-Zyklus dauert durch den E3-Schritt „Letzte Bestellung“ (EBB 251 s) jedes Mal etwa 5 min (bekannt, Q-3). Offene Bestellungen und
  Lieferverzug laufen erst danach (Zyklusstart 20:37, Lieferverzug 20:42).
- Kreditor-Mails im Testbetrieb haben `From: IDEAL AKE WMS DEV <einkauf@ake.at>` und `Reply-To: einkauf@ake.at` (Absender aus der Einstellung),
  die Sammelliste `From: … <wms-dev@ake.at>`.
- Im Lauf 17:02 (vor diesem Test) gab es `kreditoren=99`, aber `testbetrieb_umgeleitet=0` – vermutlich war `KreditorAktiv` da noch aus; nicht
  weiter verfolgt.

## Testdaten-Spur

| Was | Wo | Id / Nummer | Zurückgesetzt? |
|---|---|---|---|
| Einbuchungen über „Wareneingang zu Bestellung“ auf „TEST 1“ (Nicht-Sage-Platz) | AKE `StockMovements` / `PurchaseOrderReceipts` | Bewegungen 19533–19540, Receipts 13–20 (2027-168321: 2,5 + 3 + 3 + 1 + 1; 2026-162450: 3 + 27; 2027-168530: 1) | nein – Bestand bleibt auf TEST 1; alle `SageWeStatus = 0`, keine Queue-Einträge |
| Einbuchungen 00393295 auf „TEST 2“ (2 ohne FA, 1 mit FA 2627001) | AKE `StockMovements` | 19541, 19542 | nein |
| Receipt 13 kurz auf „Klären“ (Vorgang `A0A0A0A0-0000-4000-8000-000000000013`), dann „als übergeben“ mit 45-stelliger Nummer | AKE `PurchaseOrderReceipts` | 13 | ja, per SQL auf `SageWeStatus = 0`, Vorgang/Fehler/Beleg/Am = NULL (`ModifiedAt/By` bleiben vom Markieren) |
| AppSetting `WareneingangLieferscheinPflicht` an/aus | AKE `AppSettings` | – | ja; Zeile neu mit `false` (vorher keine Zeile = Default aus) |
| AppSetting `OffeneBestellungenVorschauWochen` 4 → 2 | AKE `AppSettings` | – | ja, Wert `2` (= Default) |
| AppSettings `WareneingangBestellungAktiv`, `WareneingangBarcodeAktiv`, `LagerbestellungAktiv`, `BestellungenAktiv` kurz `false` | AKE `AppSettings` | – | ja, alle wieder `true` (geprüft) |
| Heutige Lieferverzug-Protokollzeilen gelöscht (laut Auftrag) | AKE `PurchaseOrderReminders` | 1120 Zeilen, Id 11886–13012 | nein (gelöscht); neu entstanden Art 0: 1119, Art 1: 999 (Lauf 20:42) |
| Lieferverzug-Testmails | `D:\Dev\mail-pickup\ake` | 91 Dateien `20261113-204214-*.eml` | nein (Pickup-Ordner, nur lokal) |
| ServiceSettings-Negativversuche (CSV-Pfad, Kürzel, Wiederholungen, Lieferverzug) | AKE `ServiceSettings` | – | nichts gespeichert (alle abgelehnt, Werte danach geprüft) |

Nicht angefasst: KO-2026-00008, Benutzer-/Rollenzuordnung gerald.weichbold, `Sync:KreditorenEnabled`, `Sync:ArtikelLieferantEnabled`,
`Sync:EinkaufBestellungenEnabled`, `EinkaufAktiv`, alle Sage-Schalter, IDEAL-Einstellungen, Sage.

## Screenshot-Kandidaten für die Anwender-Doku

Personendaten: Benutzername oben rechts und die Spalten „Benutzer“/„Besteller“ zeigen echte Namen → ausblenden oder mit Testbenutzer aufnehmen.
Lieferantennamen sind Firmendaten (unkritisch); Kreditor-Mailadressen aus den Mails nicht abbilden.

| URL | Zustand | Zweck |
|---|---|---|
| `http://localhost:8081/PurchaseOrders` | Vorschau 2 Wochen, Legende rot = überfällig, Filterkarte | Offene Bestellungen |
| `http://localhost:8081/PurchaseOrders?wochen=26&colf_hinweis=Rahmenabruf` | nur Abrufe, „geliefert (Sage)“ = „–“ | Rahmenabrufe |
| `http://localhost:8081/PurchaseOrders/Receive?belId=361135` | 2027-168321, zwei Positionen, Teillieferung erfasst | Wareneingang zu Bestellung (Grundmaske) |
| `http://localhost:8081/PurchaseOrders/Receive?belId=361842` | 2027-168530 mit Hinweis „Offener Bedarf …“ (Lagerbestellung + Bedarfsmeldung) | Hinweise offene Bedarfe (nach N-7 neu aufnehmen) |
| dieselbe Seite mit Ist-Menge über dem Rest ohne Häkchen | rote Zeile „Überlieferung (… > Rest …) bitte bestätigen.“ | Überlieferung |
| `http://localhost:8081/PurchaseOrders/Receive?belId=340252` | Rahmenabruf 2026-162450 (Kopf „Rahmenabruf“, Barcode gesperrt) | Abruf buchen |
| `http://localhost:8081/PurchaseOrders/SageWareneingaenge` | Liste mit Status „an Sage übergeben“, Spalten Memo/CSV | Sage-Wareneingänge (Keyuser) |
| `http://localhost:8081/PurchaseProposals` | Banner, 50 Vorschläge, Dringlichkeit | Bestellvorschläge |
| `http://localhost:8081/PurchaseProposals?umfang=alle&colf_artikel=00722001` | letzte Bestellmenge 400, 09.04.2026 | Bestellvorschläge: letzte Bestellmenge |
| `http://localhost:8081/Articles/Info?articleNumber=00722001` | Karte „Lieferanten“ mit K31013 | Artikelinfo Lieferanten |
| `http://localhost:8081/WarehouseRequisitions` | Karte „Meine Fehlteile“ | Meine Fehlteile |
| `D:\Dev\mail-pickup\ake\20261113-204214-934-a7a5c6ce2b0148559d6dbae934da21ad.eml` | Sammelliste mit Abschnitt „Zu klären“ | Lieferverzug-Mail (Adressen schwärzen) |

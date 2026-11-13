---
type: testbericht
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
modul: Übersicht Wochenend-Regression DEV (Teil B1–B4)
stand: "Bündel v1.99.22 (90bbbbd7), DEV AKE localhost:8081 / AKE_WMS_DEV, DEV IDEAL localhost:8082 / IDEAL_WMS_DEV"
durchgefuehrt: "2026-11-13 19:40–22:05 (Ortszeit DEV-PC)"
---
# Wochenend-Regression DEV – Übersicht

Teil B von [[2026-11-14-wochenend-review-test-anwenderdoku]]: alle Bündel-Kapitel aus `docs/TESTSZENARIEN.md` auf DEV AKE und DEV IDEAL,
Nachmessung der Review-Befunde aus Teil A. Nur DEV, nichts an Sage geschrieben, keine externen Mails, nicht neu deployt, kein Code geändert.

## Zählung

| Bericht | OK | FEHLER | NICHT PRUEFBAR |
|---|---|---|---|
| [[01-kommissionierung]] (Kommissionierung, Kommissionierbereiche) | 41 | 5 | 11 |
| [[02-wareneingang-einkauf]] (Wareneingang, Offene Bestellungen, Rahmenabrufe, Lieferverzug, Bestellvorschläge, Artikelinfo, Fehlteile) | 79 | 1 | 19 |
| [[03-ideal-hierarchie-beschichtung-bde]] (FA-Hierarchie, Beschichtung, BDE, Stückliste, Vormontage, Etiketten) | 82 | 1 | 24 |
| [[04-uebrige-module]] (Tabellen, CSV, Filter-Syntax, Zahnrad, Scanner, Links, QR, Protokoll, SData, Einstellungen, Lager-Tools, Menü, Update-Skript, Sage-Test, Lokalzeit) | 74 | 2 | 20 |
| **Summe** | **276** | **9** | **74** |

Gezählt sind Tabellenzeilen (eine Zeile kann mehrere Unterschritte umfassen); „OK (Hinweis/teilweise)“ zählt als OK.

## FEHLER

| Bericht | TS | Schwere | Kurz | Befund / Notiz |
|---|---|---|---|---|
| [[01-kommissionierung]] | 120.3 (AKE) | hoch | Glas-/VK-Auftrag: Unterpositionen ohne Vater-Zeile unsichtbar | N-1 in [[2026-11-14-review-befunde-kommissionierung]] |
| [[01-kommissionierung]] | 120.3 (IDEAL) | hoch | KO-2026-00005: 73 von 79 Zeilen unsichtbar | N-1 in [[2026-11-14-review-befunde-kommissionierung]] |
| [[01-kommissionierung]] | 120.10 | mittel | zwei gleichzeitige Freigaben → HTTP 500 (Deadlock), FA freigegeben ohne Auftrag | N-2 in [[2026-11-14-review-befunde-kommissionierung]] |
| [[01-kommissionierung]] | Negativ Doppelklick „Teilweise“ | mittel | paralleler Abschluss → HTTP 500 (Deadlock) | N-2 (mit K-1) in [[2026-11-14-review-befunde-kommissionierung]] |
| [[01-kommissionierung]] | Negativ Doppelklick „Freigeben“ | mittel | zweiter Klick storniert die gerade angelegten Aufträge | K-6 in [[2026-11-14-review-befunde-kommissionierung]] |
| [[02-wareneingang-einkauf]] | 116.6 Doppel-Absenden | hoch | zwei gleichzeitige POSTs → zwei Einbuchungen | WE-1 in [[2026-11-14-review-befunde-wareneingang]] |
| [[03-ideal-hierarchie-beschichtung-bde]] | 96.7 / BDE-1 | mittel | parallele AG-Fertigmeldung bucht doppelt (4 von 6 Läufen) | BDE-1 in [[2026-11-14-review-befunde-bde]] |
| [[04-uebrige-module]] | 99.1a | niedrig | Hardware-Scanner-Karte im Profil und Feld „Handscanner“ bei Schalter aus | N-B4-1 in [[2026-11-14-review-befunde-einstellungen]] |
| [[04-uebrige-module]] | 86.5 / 86.7 | mittel | CSV: Zahlen ab 1.000 keine Zahl, negative mit Apostroph als Text | N-B4-2 in [[2026-11-14-review-befunde-tabellen]] |

Reproduziert ohne eigene FEHLER-Zeile (siehe Nachmessungen): K-2, K-3, K-4, K-5, K-8, K-9, B-1 ([[01-kommissionierung]]), WE-3
([[02-wareneingang-einkauf]]), BDE-2 statisch ([[03-ideal-hierarchie-beschichtung-bde]]); SD-3, SD-4, UP-1 (Code), LZ-2 bestätigt, SD-1 auf DEV
nicht reproduzierbar ([[04-uebrige-module]]).

## Neue Befunde

| Nr. | Schwere | Kurz | Bericht | Notiz |
|---|---|---|---|---|
| N-1 | hoch | Auftragsseite blendet Unterpositionen ohne Vater-Zeile dauerhaft aus | [[01-kommissionierung]] | [[2026-11-14-review-befunde-kommissionierung]] |
| N-2 | mittel | Deadlock wird nicht wiederholt (`Nummernkreis.IstDeadlock` nur eine Ebene) → HTTP 500 | [[01-kommissionierung]] | [[2026-11-14-review-befunde-kommissionierung]] |
| N-3 | niedrig | Stückliste nicht lesbar bei Sammelfreigabe → HTTP 500 nach gespeicherter Freigabe | [[01-kommissionierung]] | [[2026-11-14-review-befunde-kommissionierung]] |
| N-4 | Hinweis | Sammelfreigabe gibt FAs mit `IsDonePicking = 1` frei | [[01-kommissionierung]] | [[2026-11-14-review-befunde-kommissionierung]] |
| N-5 | Hinweis | Bedarfsmeldung ohne Empfänger anlegbar | [[01-kommissionierung]] | [[2026-11-14-review-befunde-kommissionierung]] |
| N-6 | Hinweis | Altlast: offener Auftrag an nicht freigegebenem FA | [[01-kommissionierung]] | [[2026-11-14-review-befunde-kommissionierung]] |
| N-7 | niedrig | Hinweis „Offener Bedarf“ nennt Glasbestellungen „Lagerbestellung“ | [[02-wareneingang-einkauf]] | [[2026-11-14-review-befunde-wareneingang]] |
| N-8 | Hinweis | Bedarfsmeldung ohne Einheit im Hinweis | [[02-wareneingang-einkauf]] | [[2026-11-14-review-befunde-wareneingang]] |
| N-9 | Hinweis | Spalte „Überlieferung“ bei 1523 px außerhalb des Bildes | [[02-wareneingang-einkauf]] | [[2026-11-14-review-befunde-wareneingang]] |
| N-10 | Hinweis (DEV) | IDEAL-DEV bucht gegen alten Bestell-Cache | [[02-wareneingang-einkauf]] | [[2026-11-14-review-befunde-offene-bestellungen]] |
| N-B3-1 | niedrig | Name einer Sage-Werkbank per manipuliertem POST überschreibbar | [[03-ideal-hierarchie-beschichtung-bde]] | [[2026-11-14-review-befunde-ideal-hierarchie]] |
| N-B3-2 | Hinweis | „Standorteinstellungen gespeichert.“ doppelt | [[03-ideal-hierarchie-beschichtung-bde]] | [[2026-11-14-review-befunde-ideal-hierarchie]] |
| N-B3-3 | Hinweis | IDEAL-DEV Briefkopf-Pfad/Schriften/Anschrift | [[03-ideal-hierarchie-beschichtung-bde]] | [[2026-11-14-review-befunde-ideal-hierarchie]] |
| N-B3-4 | Hinweis | Menüpunkt „FA-Struktur“ bei AKE sichtbar, Seite leer | [[03-ideal-hierarchie-beschichtung-bde]] | [[2026-11-14-review-befunde-ideal-hierarchie]] |
| N-B3-5 | Hinweis | BDE-Buchungsziel ohne Sub-FA | [[03-ideal-hierarchie-beschichtung-bde]] | [[2026-11-14-review-befunde-bde]] |
| N-B3-6 | Hinweis (DEV) | 2 Sub-FAs mit `IsReleasedForPicking = 1` (Altlast) | [[03-ideal-hierarchie-beschichtung-bde]] | [[2026-11-14-review-befunde-ideal-hierarchie]] |
| N-B4-1 | niedrig | Hardware-Scanner-Teile bei Schalter aus (`data-*` leer statt fehlend) | [[04-uebrige-module]] | [[2026-11-14-review-befunde-einstellungen]] |
| N-B4-2 | mittel | CSV-Zahlen ab 1.000 / negative als Text | [[04-uebrige-module]] | [[2026-11-14-review-befunde-tabellen]] |
| N-B4-3 | Hinweis | Fehlteile: „Eintraege 26–15 von 15“ bei Seite außerhalb | [[04-uebrige-module]] | [[2026-11-14-review-befunde-tabellen]] |
| N-B4-4 | Hinweis (DEV) | IDEAL-DEV `SData:BaseUrl` zeigt auf den AKE-Test | [[04-uebrige-module]] | [[2026-11-14-review-befunde-sdata]] |

## Testdaten-Spur (Kurzform)

Details je Bericht im Abschnitt „Testdaten-Spur“.

- **AKE-DEV, bleibt bestehen:** Test-Benutzer 23/24 „QA Kommissionierer (2) WE14“ (deaktiviert); Kommissionieraufträge KO-2026-00020 … -00052
  (offene storniert, abgeschlossene bleiben); 101 Umbuchungen 19432–19532 auf WAGEN N3/Wagen 2 (NAN negativ, Wagen-Texte mit FA-Nummern);
  Einbuchungen Wareneingang 19533–19542 / Receipts 13–20 auf „TEST 1“/„TEST 2“; Prio-Werte der getesteten FAs; `IsDonePicking = 1` an FA
  2607171 und 2629001; Bedarfsmeldung 25 (storniert); 1120 gelöschte und 2118 neue `PurchaseOrderReminders` (Lieferverzug-Lauf 20:42); 91
  Test-Mails in `D:\Dev\mail-pickup\ake`.
- **AKE-DEV, zurückgesetzt:** Bereich „QA Testbereich WE14“, Regeln 850/838, Standardbereich, `LeitstandAktiv`, alle kurz umgeschalteten
  AppSettings (Lieferschein-Pflicht, Vorschau-Wochen, Wareneingang/Barcode/Lagerbestellung/Bestellungen), Receipt 13.
- **IDEAL-DEV, bleibt bestehen:** BDE-Terminal 2 (Werkbank 65) mit stornierten Buchungen 7–17; BA-2026-00005 (storniert); KO-2026-00005
  (storniert); Prio 7/8 an FA 1060414/1043153.
- **IDEAL-DEV, zurückgesetzt:** Werkbank 65 (Option/Name), Prio-Cache, Profilfilter, Spalten-Präferenz, Standorteinstellungen, Arbeitsgang `EM`.
- **B4:** keine bleibenden Daten (nur nicht gespeicherte POSTs, `-NurPruefen`-Protokolle im Scratchpad).
- **Nie angefasst:** KO-2026-00008, Rollenzuordnung gerald.weichbold, `Sync:KreditorenEnabled`, `Sync:ArtikelLieferantEnabled`,
  `Sync:EinkaufBestellungenEnabled`, `EinkaufAktiv`, SData-Zugangsdaten, Sage-Schalter, Master `ProduktionsauftragHierarchisch`, Sage.

## Was der Mensch noch prüfen muss

Zusammengefasste Gründe der 74 NICHT-PRUEFBAR-Zeilen:

1. **Rollen und zweiter Benutzer** – auf DEV nur gerald.weichbold (Admin): alle Fälle „Benutzer ohne Recht“ (R nur lesend, P ohne Leitstand,
   ohne `picking`/`stock_read`/`einkauf`/BDE-Admin/`vorbau`/`beschichtungsauftrag_schreiben`), zweiter Terminal-Benutzer, gleichzeitige Freigabe
   durch zwei Personen.
2. **Sage-Schreibwege und Sage-Wareneingang an** – TS-116.11 ff., 129.3 ff., 132.3–132.5, 131.4.2: nur am Testsystem mit eingeschaltetem Schalter
   (Gerald).
3. **SData-Zugangsdaten im Web** – nicht gesetzt auf beiden DEVs: Register Sage-Test live (Feldliste, Stückliste, FA-Struktur), TS-103,
   TS-131.3–131.8; dabei SD-1 live nachmessen (Verbindungstest an fremde Adresse, Header ja/nein).
4. **Sync-Läufe** – Quellen-Weiche umschalten (TS-106), Materialisierung/Lookup/Arbeitsgang-Erkennung (TS-64, 65, 71, 77, 93, 94, 96.1/96.2),
   Dienst-Zeitstempel (TS-135.8), Meldebestand-Mail (TS-122): IDEAL-DEV ohne Syncs, AKE nur mit Testsystem/Dienst-Neustart.
5. **Gerät/Hardware** – Netum-Scanner (TS-99, 101.8, 110.2/110.8), Rollendrucker (TS-104.3–104.5), Touch-Doppeltipp (BDE-1, WE-1 am Handgerät),
   Terminal-Sicht am Handy.
6. **Update-Skript scharf** – Volllauf, DB-/Paketkopie, Test-/Prod-Ziele (TS-125.3, 125.7–125.18, 126.1–126.25, 133): nur mit eigener DEV-Kopie
   oder durch Gerald; UP-1 dabei nachstellen.
7. **Daten fehlen auf DEV** – Kombigerät (TS-72.1, 58.7/58.8), mehr als 25 Strukturen (Paging), FA auf zwei Plätzen (130.2.5), Altbestand-/
   Einmal-Skripte nur auf Kopie (120.11, 120.14), Sage-Stückliste nachträglich ergänzen (120.6), Lieferant gesperrt (116.7).
8. **Excel und Sichtprüfungen** – CSV in Excel öffnen (TS-86.5 nach Fix N-B4-2), Graustufen/Screenreader (123.10, 98.14), Netzwerk-Sperre per
   DevTools (98.9–98.13), Menü-Dropdowns (123.4).
9. **Einstellungen nicht umgestellt** – QR erstes Feld = Matchcode (101.3 ff.), Vault-/enaio-Vorlagen (100.4 ff.), `BdeAktiv`/`BdeNurFaMeldung`,
   Hierarchie-Umschaltung (63), Lieferverzug Echtbetrieb (128.5).
10. **Prüffragen** – Server-Kultur von akenet01/idealweb01 (Zahl ab 1.000 mit Punkt oder Leerzeichen, N-B4-2); IDEAL-DEV `SData:BaseUrl`
    leeren (N-B4-4) und `WareneingangBestellungAktiv` (N-10); Briefkopf IDEAL-DEV (N-B3-3).

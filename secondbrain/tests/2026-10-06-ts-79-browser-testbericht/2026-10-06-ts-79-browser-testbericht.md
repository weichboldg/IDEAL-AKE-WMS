---
type: testbericht
title: "Browser-Testbericht TS-79 — Kommissionierung nur am HauptFA, „Alle Ziele“ (v1.46.0)"
protokoll: "[[2026-10-06-ts-79-browser-testprotokoll]]"
spec: "[[2026-09-25-kommissionierung-nur-hauptfa-spec]]"
system: "http://idealweb01.ideal.ideal-ake.at:88/ (Fußzeile: v1.47.1)"
ausfuehrung: Claude Cowork (Abnahme)
datum: 2026-10-06
---

# Browser-Testbericht TS-79 — 2026-10-06

**Ergebnis: 13 OK, 1 FEHLER, 1 nicht prüfbar, 0 Protokoll passt nicht.**

## Rahmen
- Benutzer: `admin`. Gerald hat sich selbst angemeldet; Cowork hat kein Kennwort eingegeben.
  A3 verlangt einen eigenen Testbenutzer ohne gespeicherten Standardfilter. Ob `admin` einen
  Standardfilter hat, habe ich nicht geprüft, weil das Profil gesperrt ist. B8 zeigte keinen
  vorbelegten Filter.
- **Testdaten:** Auf Geralds Anweisung („Beispiele aus der Applikation verwenden“) habe ich sie in
  der Anwendung ausgewählt, nicht per SQL aus A2. Jeder Fall wurde in der Oberfläche belegt:

| Platzhalter | Wert | Beleg |
|---|---|---|
| HFA | 1035235 | Leitstand: einzige freigegebene HauptFA; steht in der Warteschlange (A5 damit erfüllt, ohne Freigabe durch Cowork) |
| SUBFA_NUMMERN | 1043421 – 1043458 (38 Sub-FAs) | Leitstand und FA-Liste, Gruppe 1035235 |
| ID_SUBFA | 101 (Sub-FA 1043421) | FA-Liste: Link `/FaWorklist/Bom/101`, Leitstand: verstecktes Feld `id=101` |
| ID_HFA | 24 | Link `/Picking/Bom/24` an der HauptFA-Zeile |
| HFA_ZIELE / ID | 1035235 / 24 | Stückliste: 166 Positionen, davon 73 mit Komm.-Ziel, 93 ohne |
| HFA_OHNE / ID | **KEIN_FALL** | Das System hat nur 4 HauptFA (FA-Liste, auch mit „Erledigte anzeigen“). Alle 4 erscheinen in der Kommissionierliste, haben also mindestens ein Komm.-Ziel. |

- **Anzeige-Einstellungen:** In Leitstand, FA-Liste, Stückliste und Warteschlange habe ich keine
  Spalten verändert; die Seitengröße steht überall auf 50. Die Komm.-Ziel-Auswahl der Stückliste
  ist am Ende leer.

## Ergebnisse

| Schritt | Ergebnis | Beobachtung | Screenshot |
|---|---|---|---|
| B1 Leitstand, Bedienelemente | OK | Menü Leitstand → Kommissionierung (`/PickingLeitstand`), Gruppe „HauptFA 1035235“. Nur die HauptFA-Zeile hat Auswahl-Checkbox, Stücklisten-Symbol (`/Picking/Bom/24`), „Freigabe zurücknehmen“ und Prioritätsfeld. Alle 38 Sub-FA-Zeilen haben in diesen Spalten nichts. Jede der 39 Zeilen hat 32 Zellen bei 32 Spaltenköpfen, nichts verrutscht. | ja, [[B1-leitstand-bedienelemente-nur-hauptfa.jpg]] |
| B2 Gruppen-Kopfzeile | OK | Kein „Alle Sub-FAs freigeben“ auf der ganzen Seite (auch nicht im HTML). Alle 4 Kopfzeilen haben „Alle Sub-FAs fertigmelden“, nicht angeklickt. Die Kopfzeile nennt „39 Sub-FAs“; die Gruppe hat 1 HauptFA-Zeile und 38 Sub-FA-Zeilen. | nein |
| B3 FA-Liste | OK | `/Picking/Bom/…` steht nur an der HauptFA-Zeile (`/Picking/Bom/24`). Alle 38 Sub-FA-Zeilen haben stattdessen das Symbol `/FaWorklist/Bom/<id>`, z. B. `/FaWorklist/Bom/101`. | nein |
| B4 `/Picking/Bom/101` | OK | Weiterleitung auf `/Picking`, gelbe Meldung „Kommissionierung erfolgt nur am HauptFA 1035235.“, keine Stückliste. Geprüft am Seiteninhalt; der Screenshot schlug fehl (siehe Hinweise). | nein |
| B5 `/Picking/Bom/24` | OK | Stückliste „FA 1035235“ ohne Warnmeldung, „166 Positionen“. | nein |
| B6 Warteschlange | OK | `/Picking`: Einen Haken „Alle Kommissionieraufträge anzeigen“ gibt es für diesen Benutzer nicht. Die Liste zeigt die Gruppe 1035235 mit genau 1 Zeile, FA-Nr. 1035235. Keine der Nummern 1043421–1043458 steht auf der Seite. | nein |
| B7 Such-API | OK | `/api/productionorders/search?q=1035235` liefert `[{"id":24,"text":"1035235 - 80132588 - "}]`: genau ein Eintrag, id 24 = HauptFA. | nein |
| B8 Kein gelber Kasten | OK | `/Picking/Bom/24` ohne Filter: kein gelber Kasten, keine Badge-Zeile, Komm.-Ziel-Auswahl leer. | ja, [[B8-stueckliste-ohne-gelben-kasten.jpg]] |
| B9 „Alle Ziele“ | OK | Erste Option heißt „Alle Ziele (nur kommissionier-relevant)“, danach ISO, KOM, LÖT, PG. Nach der Auswahl: Badge „Gefiltert: Alle Ziele (nur kommissionier-relevant)“, 73 Zeilen sichtbar, alle mit Ziel (ISO 11, KOM 54, LÖT 3, PG 5). Das sind genau die 73 von 166 Positionen mit Ziel. Vorher waren 12 Zeilen sichtbar: Der Baum ist beim Laden zugeklappt; mit Filter klappt er auf. | ja, [[B9-alle-ziele-badge.jpg]] |
| B10 Ausschluss | OK | Bei aktivem „Alle Ziele“ KOM gewählt: Nur KOM bleibt ausgewählt, Badge „Gefiltert: Komm.-Ziel = KOM“, 54 Zeilen. Dann wieder „Alle Ziele“: KOM ist abgewählt, wieder 73 Zeilen. | nein |
| B11 Druck | OK | „Stückliste drucken“ öffnet `/Picking/PrintBom/24` in einem neuen Tab. Im Kopf: „Gefilterte Ansicht — Komm.-Ziel=Alle Ziele (nur kommissionier-relevant)“, 73 Positionszeilen wie am Bildschirm. Ein Druckdialog war nicht offen. Tab geschlossen. | ja, [[B11-druck-alle-ziele.jpg]] |
| B12 Neu laden | **FEHLER** | Nach F5 (Ladeart `reload`) ist die Komm.-Ziel-Auswahl leer, es gibt kein Badge und 12 Zeilen statt 73. Einmal wiederholt, gleiches Ergebnis. Zum Vergleich: Auch ein Einzelziel (KOM) ist nach F5 weg. | ja, [[B12-nach-f5-filter-weg.jpg]] |
| B13 Reset über das Badge | OK | Kreuz im Badge geklickt: Badge weg, Auswahl leer, kein Filter mehr. Danach sind 102 Zeilen sichtbar statt 12 vor B9. Die 64 verborgenen Zeilen sind ganze zugeklappte Teilbäume (keine sichtbare Geschwisterzeile, alle ohne Ziel). Es filtert also nichts mehr; nur der Aufklappzustand aus B9 bleibt. Die Zeilenzahl „wie vor B9“ gilt deshalb nicht wörtlich. | nein |
| B14 Nichts zu drucken | NICHT PRÜFBAR | KEIN_FALL: Auf dem Testsystem gibt es keine HauptFA, deren Stückliste ganz ohne Komm.-Ziel ist (siehe Testdaten). | nein |
| B15 Hilfe | OK | `/Help`: „Nur am HauptFA (IDEAL, seit v1.46.0)“ als Begriff im Abschnitt „Leitstand (Kommissionier-Freigabe)“. „Alle Ziele“ im Abschnitt „Kommissionierung“ („Kommissionierziel-Filter „Alle Ziele“ (seit v1.46.0)“) und im Abschnitt „Mein Profil“. | nein |

## FEHLER

**B12 — „Alle Ziele“ bleibt beim Neuladen nicht erhalten.** Laut Protokoll (TS-79.6) steht nach
F5 weiter „Alle Ziele“ im Dropdown, das Badge ist da und die Zeilenzahl bleibt bei 73. Tatsächlich
ist nach F5 die Auswahl leer, das Badge fehlt und es sind wieder 12 Zeilen sichtbar. Ein Einzelziel
(KOM) ging beim Neuladen ebenfalls verloren. Ob TS-79.6 nur für den im Profil gespeicherten
Standardfilter gilt, kann ich nicht beurteilen; der Benutzer `admin` hat keinen solchen Filter
gezeigt.

## Hinweise zum Lauf
- Der Chrome-Tab war für den Browser die ganze Zeit „im Hintergrund“ (`document.visibilityState =
  hidden`). Screenshots auf `/Picking` schlugen dreimal fehl. Mausklicks direkt nach einem Seitenwechsel
  kamen mehrfach nicht an und gingen erst beim zweiten Versuch durch. Die Beobachtung „Drucken
  reagiert erst beim zweiten Klick“ vom 30.09. kann dieselbe Ursache haben. Von Hand nachprüfen,
  mit Chrome im Vordergrund (Teil C des TS-80-Protokolls).
- Ergebnisse habe ich zusätzlich am Seiteninhalt (DOM) gegengeprüft, nicht nur am Bild.

---
type: testbericht
title: "Browser-Testbericht TS-80 (Nachlauf) — Kommissionierliste Summierung/Druck (v1.47.0)"
protokoll: "[[2026-09-29-ts-80-browser-testprotokoll]]"
spec: "[[2026-09-25-kommissionierliste-summierung-pdf-spec]]"
vorlauf: "[[2026-09-30-ts-80-browser-testbericht]]"
system: "http://idealweb01.ideal.ideal-ake.at:88/ (Fußzeile: v1.47.1)"
ausfuehrung: Claude Cowork (Abnahme)
datum: 2026-10-06
---

# Browser-Testbericht TS-80, Nachlauf — 2026-10-06

Nachlauf zu [[2026-09-30-ts-80-browser-testbericht]]. Geprüft wurden nur die damals offenen Schritte:
B2, B3, B4, B6 (mit einer KW, die Daten enthält), B7, B10, B11 (PDF) und B15.

**Ergebnis: 5 OK, 0 FEHLER, 3 nicht prüfbar, 0 Protokoll passt nicht.**

## Rahmen
- Benutzer: `admin`, von Gerald selbst angemeldet.
- **Testdaten:** Auf Geralds Anweisung habe ich sie aus der Anwendung genommen, nicht per SQL aus A2.
  Grundlage ist die Kommissionierliste „Liste“ ohne Filter: alle 4 HauptFA und 249 Positionen auf
  einer Seite. Für Fall A habe ich die erwartete Summe aus den Liste-Zeilen gebildet. B2 prüft damit,
  ob „Summiert“ die Liste richtig zusammenfasst, nicht den Abgleich gegen die Datenbank.

| Platzhalter | Wert | Beleg |
|---|---|---|
| HFA_A / ARTNR_A / ZIEL_A / SUMME_A | 1035235 / 50000423 / KOM / 6 | Liste: drei Zeilen 2 + 2 + 2 |
| HFA_B / ARTNR_B | **KEIN_FALL** | In keiner der 249 Liste-Zeilen hat eine Artikelnummer im selben HauptFA zwei verschiedene Ziele |
| HFA_C / MATCHCODE_C | 1035235 / A-Z-TUER-SCHARNIER-BOLZEN-100-V01 | Artikel 50000298 und 50002009 |
| KW_MIT_DATEN | 2026-W10 | KO-Termin 04.03.2026 bei 1037045 und 1039413 (Gruppenköpfe der Liste) |

- **Anzeige-Einstellungen von `admin` zu Beginn:**
  - „Summiert“: Artnr. ausgeblendet, sonst alles sichtbar, Seitengröße 50.
  - „Liste“: Artikeltyp, Beschichtet und Material ausgeblendet, Seitengröße 50.
  - Für B2 bis B4 habe ich Artnr. in „Summiert“ eingeblendet und am Ende wieder ausgeblendet. Nach
    dem Neuladen geprüft: wieder wie zu Beginn. Filter sind gelöscht.

## Ergebnisse

| Schritt | Ergebnis | Beobachtung | Screenshot |
|---|---|---|---|
| B2 Summe richtig | OK | „Summiert“, Spaltenfilter HauptFA = 1035235 (Adresse `colf_hauptfa=1035235`, 50 Zeilen): genau eine Zeile 50000423 / KOM / K-02;0;0;0, Summe **6,000**. Umschalter „Liste“ nimmt den Filter mit: 73 Zeilen, davon 3 mit 50000423 / KOM, Sollmenge je 2,000. | nein |
| B3 Ziele getrennt | NICHT PRÜFBAR | KEIN_FALL (siehe Testdaten). | nein |
| B4 Gleicher Matchcode | OK | HauptFA 1035235 + Matchcode-Filter: 2 Zeilen, 50000298 (KOM, 1,000) und 50002009 (KOM, 1,000), beide mit A-Z-TUER-SCHARNIER-BOLZEN-100-V01. | ja, [[B4-matchcode-zwei-artikel.jpg]] |
| B6 KW optional | OK | Ohne KW: 174 Zeilen, 4 HauptFA, kein „Bitte KW eingeben“. KW 2026-W10 bis 2026-W10: **105 Zeilen** (1037045: 52, 1039413: 53), also mehr als 0 und weniger als 174. Es sind genau die beiden HauptFA mit KO-Termin 04.03.2026. | ja, [[B6-kw10-zwei-hauptfa.jpg]] |
| B7 Umschalter nimmt Filter mit | OK | Liste mit Ziel KOM und den Spaltenfiltern Artnr. 50000423, Hauptlagerplatz k-02;0;0;0, Arbeitsbereich k-02 und Sollmenge 2,000 (2 Zeilen). Nach dem Umschalten auf „Summiert“ lautet die Adresse `target=KOM&colf_artnr=50000423&colf_hauptlagerplatz=k-02;0;0;0`. Arbeitsbereich und Sollmenge sind weg, Ziel und die beiden Filter stehen in den Feldern. Eine Zeile mit Summe 6,000, keine Fehlermeldung. Den Zusatz (KW) habe ich am 30.09. geprüft, Ergebnis OK. | nein |
| B10 Filter im Druck | OK | Liste, Ziel KOM + Filter Artnr. 50000423 (3 Zeilen), Drucken: Die Druckansicht „Kommissionierliste — HauptFA 1035235“ zeigt 3 Positionen und oben „Gefilterte Ansicht — … Kommissionier-Ziel: KOM.“ Gedruckt werden nur die sichtbaren Spalten (`visibleColumns` ohne Artikeltyp, Beschichtet, Material). | nein |
| B11 PDF-Download | NICHT PRÜFBAR | Nicht heruntergeladen, weil die Freigabe im Chat noch fehlt (Regel: vor jedem Download fragen). Die Hinweise im Druck (OK) stehen im Bericht vom 30.09. | nein |
| B15 Lagerbestellung-Druck | NICHT PRÜFBAR | Keine Bestellung vorhanden: „meine Listen“ (Lager/Glas): „Noch keine Listen erfasst.“ „Lager: Eingehende Listen“ (Lager/Glas, Status alle außer Entwurf): „Keine Listen gefunden.“ Ich habe keine angelegt. | nein |

## Hinweise zum Lauf
- **Filter über die Adresse gesetzt:** In B7 und B10 habe ich die Spaltenfilter direkt über die
  Adresse gesetzt (`colf_…`), in derselben Form, die die Filterfelder selbst erzeugen. Grund:
  Tastatureingaben kamen im Hintergrund-Tab mehrfach nicht an. Geprüft wird der Umschalter bzw.
  Drucken-Link selbst.
- **Druckansicht im selben Tab:** In B10 habe ich den Link per Skript ausgelöst und die Druckansicht
  im selben Tab geöffnet, weil der Skript-Klick keinen neuen Tab öffnete.
- **Umschalter-Links:** Sie reagierten im Hintergrund-Tab auf Mausklicks wiederholt nicht. Siehe
  den Hinweis im [[2026-10-06-ts-79-browser-testbericht]].

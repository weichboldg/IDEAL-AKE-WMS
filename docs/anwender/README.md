# Anwender-Anleitungen IDEAL-AKE WMS

Diese Anleitungen sind für die Menschen geschrieben, die das WMS im Alltag bedienen: Lager, Produktion, Leitstand und Einkauf.
Jede Anleitung folgt demselben Aufbau:

1. **Zweck** – wofür das Modul da ist.
2. **Wer darf** – welche Rolle (Name wie unter *Stammdaten → Rollen*) und welche Einstellungen eingeschaltet sein müssen.
3. **Schritt für Schritt** – mit Bildern aus dem Testsystem.
4. **Hinweise und Stolperfallen** – auch bekannte Fehler, die noch nicht behoben sind, und wie man sie umgeht.
5. **Häufige Fragen**.

**Standorte:** Das WMS läuft bei **AKE** und bei **IDEAL**. Wo sich die Standorte unterscheiden, steht das in einem eigenen Abschnitt.
Die Bilder stammen aus den Testsystemen AKE und IDEAL (je Kapitel angegeben); Namen von Mitarbeitern sind durch „Mitarbeiter A, B, C“ ersetzt. Zahlen,
Aufträge und Artikel in den Bildern sind Testdaten.

**Hilfeseite:** Im WMS unten rechts auf **Hilfe** klicken. Dort stehen alle Funktionen in Kurzform, auch die technischen Einstellungen.
Diese Anleitungen erklären die Abläufe ausführlicher und in der Reihenfolge, in der man sie im Alltag braucht.

**Fragen oder Fehler?** Bitte an den Leitstand bzw. die WMS-Betreuung melden – am besten mit Auftragsnummer (z. B. KO-2026-00057),
FA-Nummer und einem Bildschirmfoto.

## Übersicht

| Nr. | Modul | Für wen | Datei |
|---|---|---|---|
| 01 | Kommissionierung (Leitstand, Freigabe, Kommissionierauftrag, Wagen, Abschluss, Druck) | Leitstand, Lager/Kommissionierer | [01-kommissionierung.md](01-kommissionierung.md) |
| 02 | Kommissionierbereiche (Lager, Glas, Vorkommissionierung, Zuordnungsregeln) | Stammdaten-Pflege, Leitstand | [02-kommissionierbereiche.md](02-kommissionierbereiche.md) |
| 03 | Lagerbestellung und Glasbestellung (Liste erfassen, Lager-Abschluss, Fehlteile) | Produktion, Lager | [03-lagerbestellung-glasbestellung.md](03-lagerbestellung-glasbestellung.md) |
| 04 | Bedarfsmeldung (aus Stückliste, Liste, Erfüllen beim Einbuchen) | Produktion, Lager, Einkauf | [04-bedarfsmeldung.md](04-bedarfsmeldung.md) |
| 05 | Wareneingang zu Bestellung (Scannen, Überlieferung, Hinweise, Sage-Wareneingang) | Lager | [05-wareneingang-zu-bestellung.md](05-wareneingang-zu-bestellung.md) |
| 06 | Offene Bestellungen und Rahmenabrufe | Lager, Einkauf | [06-offene-bestellungen-rahmenabrufe.md](06-offene-bestellungen-rahmenabrufe.md) |
| 07 | Lieferverzug-Mail (Sammelliste, „Zu klären“, Kreditor-Mail, Testbetrieb) | Einkauf | [07-lieferverzug-mail.md](07-lieferverzug-mail.md) |
| 08 | Bestellvorschläge (Rechnung, letzte Bestellmenge, Fallback, Banner, CSV) | Einkauf | [08-bestellvorschlaege.md](08-bestellvorschlaege.md) |
| 09 | Einbuchen, Ausbuchen, Umbuchen (auch Mehrfach-Einbuchung, Lagerplatz aus-/umbuchen, Scanner) | Lager, Kommissionierer | [09-einbuchen-ausbuchen-umbuchen.md](09-einbuchen-ausbuchen-umbuchen.md) |
| 10 | Bestände und Spaltenfilter-Suche (Suchsyntax, Zahnrad, CSV) | alle | [10-bestaende-spaltenfilter.md](10-bestaende-spaltenfilter.md) |
| 11 | BDE-Terminal (Anmelden, Liste nach Haupt-FA, Scan, Melden) | Produktion | [11-bde-terminal.md](11-bde-terminal.md) |
| 12 | Register Sage-Test (Verbindungstest, Feldliste, Stückliste live mit Ressourcenliste und beendeten Haupt-FAs, FA-Struktur live) | Administration | [12-register-sage-test.md](12-register-sage-test.md) |
| 13 | Anmeldung und Benutzer (Windows vs. Formular, Erstkennwort mit Pflichtwechsel, Gerätekonten, AD-Benutzer) | alle; Administration | [13-anmeldung-und-benutzer.md](13-anmeldung-und-benutzer.md) |
| 14 | Blechfertigung – BF-Aufträge (Warenkorb, Schachtler-Freigabe, WiCAM-CSV, Fertig, Storno; IDEAL) | Produktion, Schachtler | [14-blechfertigung-bf-auftraege.md](14-blechfertigung-bf-auftraege.md) |
| 15 | Beschichtung (Vorschlagsliste mit „Verpackt“, Beschichtungsauftrag, Status, Lackierer aus Kreditoren, Druck/PDF, Reklamation) | Produktion, Versand, Leitstand | [15-beschichtung-beschichtungsauftrag.md](15-beschichtung-beschichtungsauftrag.md) |
| 16 | IDEAL-Kommissionierliste (Ziele, Summiert, Abteilung, Lagerplätze, nur nach Leitstand-Freigabe, Druck/PDF; IDEAL) | Lager | [16-ideal-kommissionierliste.md](16-ideal-kommissionierliste.md) |
| 17 | BDE-Teileliste an der Werkbank (nur „fertig“ melden, einzeln oder als Block, Rücknahme, Teilmeldungen; IDEAL) | Produktion, BDE-Schichtleiter | [17-bde-teileliste-werkbank.md](17-bde-teileliste-werkbank.md) |
| 18 | Startseite und Menü (Kacheln nach Bereichen, gleich wie das Menü, Artikelinfo im Menü) | alle | [18-startseite-und-menue.md](18-startseite-und-menue.md) |
| 19 | Lagerplätze in Sage anlegen (Bereich wählen, neues Lager, Übergabe-Fehler, Erneut senden) | Stammdaten-Pflege | [19-lagerplaetze-in-sage-anlegen.md](19-lagerplaetze-in-sage-anlegen.md) |

## Stand und was noch folgt

Stand der Anleitungen: Version **1.99.71** (25.11.2026) für Kapitel 19 (neu, Text; Fotos folgen), Version **1.99.70** (25.11.2026) für die Kapitel 01 und 02 (Text; Fotos siehe Kapitel 01), Version **1.99.69** (25.11.2026) für die Kapitel 05 und 15 (Text), Version **1.99.68** (25.11.2026) für die Kapitel 05 und 09 (Text; Fotos siehe Kapitel), Version **1.99.67** (25.11.2026) für Kapitel 15 (Text; Fotos siehe Kapitel), Version **1.99.66** (25.11.2026) für die Kapitel 05, 09 und 18 (Text, Foto 05-05 neu), Version **1.99.62** (24.11.2026) für die Kapitel 05 und 10 (Text, Kapitel 10 mit Foto), Version **1.99.58** (23.11.2026) für die Kapitel 01, 03 und 10 (Text; Fotos siehe Kapitel), Version **1.99.47** (20.11.2026) für Kapitel 09, Version **1.99.59** (23.11.2026) für die Kapitel 05 und 13 (Text), Version **1.99.53** für die Kapitel 05 (Fotos) und 18 (mit Fotos) und Kapitel 03 (Foto zu „Entnahme aus“ folgt nach Einschalten), Version **1.99.45** (19.11.2026) für Kapitel 14, Version **1.99.44** für Kapitel 16, Version **1.99.43** für die Kapitel 01, 11, 13, 15 und 17; die übrigen Kapitel Version **1.99.29**. Seit Version 1.99.22 neu bzw. ergänzt:

| Version | Änderung | Anleitung |
|---|---|---|
| 1.99.23 | Schutz gegen Doppel-Absenden (Freigabe, Abschluss, Wareneingang, Fertigmeldung; Knopf kurz gesperrt), alle Positionen im Kommissionierauftrag sichtbar, CSV-Mengen ohne Tausendertrenner | [01](01-kommissionierung.md), [05](05-wareneingang-zu-bestellung.md), [10](10-bestaende-spaltenfilter.md), [11](11-bde-terminal.md) |
| 1.99.24 | nicht vergeben | – |
| 1.99.25 | Beschichtung: Quelle für Kennzeichen und Termin einstellbar (Einstellungen, ohne sichtbare Änderung im Alltag) | [15](15-beschichtung-beschichtungsauftrag.md) |
| 1.99.26 | Sage-Test → Stückliste live: „Ressourcenliste (Vorlage)“, FA-Struktur auch für beendete Haupt-FAs | [12](12-register-sage-test.md) |
| 1.99.27 | reserviert, nicht ausgeliefert | – |
| 1.99.28 | Anmeldung nur mit Kennwort, Erstkennwort mit Pflichtwechsel | [13](13-anmeldung-und-benutzer.md) |
| 1.99.29 | Wareneingang-CSV an enaio: Erfasser = Windows-Anmeldename; Erstkennwort-Haken nur mit Kennwort | [05](05-wareneingang-zu-bestellung.md), [13](13-anmeldung-und-benutzer.md) |
| 1.99.32 | BDE-Teileliste an der Werkbank (IDEAL) | [11](11-bde-terminal.md), [17](17-bde-teileliste-werkbank.md) |
| 1.99.34 | Beschichtung bei AKE aus der WA-Stückliste | [15](15-beschichtung-beschichtungsauftrag.md) |
| 1.99.35 | BF-Aufträge (Blechfertigung, IDEAL) | [14](14-blechfertigung-bf-auftraege.md) |
| 1.99.36 | BDE-Terminal: dunkle Schrift auf grünen und grauen Knöpfen; Beschichtung AKE: Maße, kommissionierte WA ausgeblendet | [11](11-bde-terminal.md), [15](15-beschichtung-beschichtungsauftrag.md), [17](17-bde-teileliste-werkbank.md) |
| 1.99.37 | Kennzeichen „in Sage beendet/inaktiv/ungeklärt“ in Kommissionierliste, Beschichtung, BDE-Teileliste | [15](15-beschichtung-beschichtungsauftrag.md), [16](16-ideal-kommissionierliste.md), [17](17-bde-teileliste-werkbank.md) |
| 1.99.41 | Beschichtungsauftrag: Haken „Verpackt“, Position ohne FA, Status Versendet, Lackierer aus Kreditoren, Druck je Position | [15](15-beschichtung-beschichtungsauftrag.md) |
| 1.99.42 | Benutzer-Kürzel; BF-CSV genau nach WiCAM-Vorlage, Test-CSV; Beschichtung: Lackierer nachtragen, Druck mit Seitenzahl | [13](13-anmeldung-und-benutzer.md), [14](14-blechfertigung-bf-auftraege.md), [15](15-beschichtung-beschichtungsauftrag.md) |
| 1.99.43 | IDEAL-Kommissionierliste: Ziele gemerkt, Summiert als Standard, Abteilung und Lagerplätze, nur nach Leitstand-Freigabe | [01](01-kommissionierung.md), [16](16-ideal-kommissionierliste.md) |
| 1.99.44 | Kommissionierliste: Ziel-Kästchen wieder nebeneinander; Menü *Stammdaten → Empfänger* auch bei Lager-/Glasbestellung und Blechfertigung | [14](14-blechfertigung-bf-auftraege.md), [16](16-ideal-kommissionierliste.md) |
| 1.99.45 | Startseite nach Bereichen und Berechtigung (wie das Menü), *Stammdaten → Artikelinfo* im Menü; Artikelinfo und Lagerbestellung: Bestand nur des gesuchten Artikels; Lagerplatz-Angabe der Lagerbestellung Hauptlagerplatz zuerst, ohne Kommissionierwagen; BF-Warenkorb: Vorschläge beim Tippen | [03](03-lagerbestellung-glasbestellung.md), [14](14-blechfertigung-bf-auftraege.md), [18](18-startseite-und-menue.md) |
| 1.99.46 | Startseite: Kacheln wieder mit Symbolen | [18](18-startseite-und-menue.md) |
| 1.99.47 | Lagerplatz ausbuchen: FA-Nummer filtert, Rest bleibt am Platz/Wagen, gemischte Zeilen werden abgefragt | [09](09-einbuchen-ausbuchen-umbuchen.md), [01](01-kommissionierung.md) |
| 1.99.50 | Startseite: jedes Symbol nur einmal, Symbolfamilien Rahmen/Blatt/Tropfen | [18](18-startseite-und-menue.md) |
| 1.99.51 | Startseite: Reiter „Favoriten“, Stern je Kachel | [18](18-startseite-und-menue.md) |
| 1.99.52 | Wareneingang zu Bestellung: Bestellnummer ohne Jahr, Fokus-Zeile, Zahnrad (Spalten), Hauptlagerplatz als Ziel-Vorschlag | [05](05-wareneingang-zu-bestellung.md) |
| 1.99.53 | Lager-/Glasbestellung: Lieferung beim Abschließen ausbuchen („Entnahme aus“, Schalter, Standard aus) | [03](03-lagerbestellung-glasbestellung.md) |
| 1.99.54 | Mengenfelder: Pfeile ±1, Mausrad ändert nichts | [10](10-bestaende-spaltenfilter.md) |
| 1.99.55 | Spalten verschieben auch in Artikel, Lagerplätze, Kreditoren, Debitoren, Offene Bestellungen, Wareneingang, Bestellvorschläge | [10](10-bestaende-spaltenfilter.md) |
| 1.99.56 | Beschichtungsauftrag: Statusknopf speichert Mengen/Haken mit; Druck fragt bei Ungespeichertem | [15](15-beschichtung-beschichtungsauftrag.md) |
| 1.99.57 | Lagerbestellung: Hinweis nennt Bestand auf nicht wählbaren Plätzen | [03](03-lagerbestellung-glasbestellung.md) |
| 1.99.58 | Kommissionierauftrag: eigene Spalte „Menge IST“ | [01](01-kommissionierung.md) |
| 1.99.59 | Wareneingang zu Bestellung: Zeilen hellblau statt grau; Versionsnummer auf der Anmeldeseite | [05](05-wareneingang-zu-bestellung.md), [13](13-anmeldung-und-benutzer.md) |
| 1.99.60 | Menü: FA-Struktur unter „Fertigungsaufträge“ | [18](18-startseite-und-menue.md) |
| 1.99.61 | FA-Struktur: Titel ohne „(IDEAL)“, nur bei hierarchischer Struktur sichtbar | [18](18-startseite-und-menue.md) |
| 1.99.62 | Alle Tabellen: Streifen, Zeile unter der Maus hellblau, markierte Eingabezeile, dunklere Links/Symbole | [10](10-bestaende-spaltenfilter.md), [05](05-wareneingang-zu-bestellung.md) |
| 1.99.65 | Beschichtungsauftrag: ohne Haken anlegen (alle frei, offen), Rückfrage beim Übergeben, Entfernen in Übergeben nur offene, Verpackt oder Entfernen | [15](15-beschichtung-beschichtungsauftrag.md) |
| 1.99.66 | „Sage-Buchungen“: Lagerbuchungen und Sage-Wareneingänge in einer Liste, Menüpunkt „Sage-Wareneingänge“ entfällt | [05](05-wareneingang-zu-bestellung.md), [09](09-einbuchen-ausbuchen-umbuchen.md), [18](18-startseite-und-menue.md) |
| 1.99.67 | Beschichtungsauftrag: Positionen mit VaterFA in Meldungen, kein Übergeben ohne Positionen (stattdessen Entwurf stornieren) | [15](15-beschichtung-beschichtungsauftrag.md) |
| 1.99.68 | Ausbuchung und Lagerplatz ausbuchen: Auswahlfeld Abteilung; Sage-Buchungen tragen Kennung, Benutzer und Abteilung (Referenz/Memo), Spalte „Abteilung“ in Sage-Buchungen | [09](09-einbuchen-ausbuchen-umbuchen.md), [05](05-wareneingang-zu-bestellung.md) |
| 1.99.69 | Beschichtungsauftrag: leerer Entwurf lässt sich wieder speichern und übergeben; Sage-Buchungen zeigen Wareneingänge nur bei eingeschaltetem „Wareneingang zu Bestellung“, auch dem Einkauf | [15](15-beschichtung-beschichtungsauftrag.md), [05](05-wareneingang-zu-bestellung.md) |
| 1.99.70 | Kommissionierung: Stücklisten-Nachträge legt der Dienst nachts an; Knöpfe „Fehlende Kommissionieraufträge erzeugen“ nur für Administratoren; „Altbestand geprüft“ legt die Aufträge sofort an; Badge „⚠ Stückliste kleiner“ im Leitstand und Hinweis in der Stückliste | [01](01-kommissionierung.md), [02](02-kommissionierbereiche.md) |
| 1.99.71 | Lagerplätze in Sage anlegen: Knopf in der Lagerplatzliste, Bereich der Koordinaten, neues Lager, Spalte „Übergabe-Fehler“, „Erneut senden“ (ausgeschaltet ausgeliefert) | [19](19-lagerplaetze-in-sage-anlegen.md) |

Änderungen aus 1.99.30, 1.99.31, 1.99.33 und 1.99.38–1.99.40 (Bestellvorschläge, offene Bestellungen, Wareneingang
Sammellieferschein, Arbeitsgänge in Sage, Sage-Benutzerfelder) sind in den betroffenen Kapiteln noch nicht nachgezogen.

**Beschichtung:** Die Anleitung für den Alltag steht in [Kapitel 15](15-beschichtung-beschichtungsauftrag.md). Die
Beschichtungs-Einstellungen für Administratoren stehen im Betriebshandbuch ([docs/betrieb/04-konfiguration.md](../betrieb/04-konfiguration.md)).

**Betriebshandbuch für Administratoren:** [docs/betrieb/00-index.md](../betrieb/00-index.md).

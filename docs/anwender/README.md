# Anwender-Anleitungen IDEAL-AKE WMS

Diese Anleitungen sind für die Menschen geschrieben, die das WMS im Alltag bedienen: Lager, Produktion, Leitstand und Einkauf.
Jede Anleitung folgt demselben Aufbau:

1. **Zweck** – wofür das Modul da ist.
2. **Wer darf** – welche Rolle (Name wie unter *Stammdaten → Rollen*) und welche Einstellungen eingeschaltet sein müssen.
3. **Schritt für Schritt** – mit Bildern aus dem Testsystem.
4. **Hinweise und Stolperfallen** – auch bekannte Fehler, die noch nicht behoben sind, und wie man sie umgeht.
5. **Häufige Fragen**.

**Standorte:** Das WMS läuft bei **AKE** und bei **IDEAL**. Wo sich die Standorte unterscheiden, steht das in einem eigenen Abschnitt.
Die Bilder stammen meist aus dem AKE-Testsystem; Namen von Mitarbeitern sind durch „Mitarbeiter A, B, C“ ersetzt. Zahlen,
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

## Stand und was noch folgt

Stand der Anleitungen: Version **1.99.29** (16.11.2026). Seit Version 1.99.23 neu bzw. ergänzt:

| Version | Änderung | Anleitung |
|---|---|---|
| 1.99.25 | Beschichtung: Quelle für Kennzeichen und Termin einstellbar (Einstellungen, ohne sichtbare Änderung im Alltag) | folgt (siehe unten) |
| 1.99.26 | Sage-Test → Stückliste live: „Ressourcenliste (Vorlage)“, FA-Struktur auch für beendete Haupt-FAs | [12](12-register-sage-test.md) |
| 1.99.28 | Anmeldung nur mit Kennwort, Erstkennwort mit Pflichtwechsel | [13](13-anmeldung-und-benutzer.md) |
| 1.99.29 | Wareneingang-CSV an enaio: Erfasser = Windows-Anmeldename; Erstkennwort-Haken nur mit Kennwort | [05](05-wareneingang-zu-bestellung.md), [13](13-anmeldung-und-benutzer.md) |

**Beschichtung (Anwenderteil) folgt** nach den Ausbaustufen E2/E4/E5 der Beschichtungslogik. Bis dahin beschreibt die Hilfeseite im WMS
die Beschichtungs-Einstellungen; für Administratoren steht das Wichtigste im Betriebshandbuch
([docs/betrieb/04-konfiguration.md](../betrieb/04-konfiguration.md)).

**Betriebshandbuch für Administratoren:** [docs/betrieb/00-index.md](../betrieb/00-index.md).

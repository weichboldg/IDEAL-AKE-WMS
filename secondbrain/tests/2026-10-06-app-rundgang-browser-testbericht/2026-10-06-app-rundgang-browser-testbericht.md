---
type: testbericht
title: "Browser-Rundgang IDEAL-Testsystem — alle Menüpunkte, nur lesend (v1.47.2)"
system: "http://idealweb01.ideal.ideal-ake.at:88/ (v1.47.2)"
ausfuehrung: Claude Cowork (Abnahme)
datum: 2026-10-06
---

# Browser-Rundgang IDEAL-Testsystem — 2026-10-06

Auftrag von Gerald: „alle anderen Funktionen testen“, Umfang laut Rückfrage „ganze App, nur lesend“.
Dafür gibt es kein Protokoll. Geprüft wurde ein Smoke-Test: Jede Seite lädt, es gibt keine
Fehlerseite und keine Skriptfehler, Filter und Blättern funktionieren an Stichproben. Gespeichert,
gebucht oder freigegeben habe ich dabei nichts.

**Ergebnis: 1 FEHLER, 1 Beobachtung, sonst ohne Befund.**

## Vorgehen
- **Menüpunkte:** Alle 50 Links aus Menü und Startseite (ohne Abmelden) habe ich abgerufen: Status,
  Zieladresse, Seitentitel und Fehlertext im Inhalt.
- **Konsole:** Jede Menüseite habe ich zusätzlich im Browser geöffnet und die Konsole auf Fehler
  geprüft. Einziger Eintrag: eine Meldung der Passwort-Erweiterung im Browser
  (`chrome-extension://…/passkeys-inject.js`), nicht von der Anwendung.
- **Unterseiten:** je Liste bis zu drei Detail-, Bearbeiten- und Druckseiten aufgerufen, aber nicht
  gespeichert.
- **Filter und Blättern** an Stichproben: Artikel (Suche, Spaltenfilter, Seite 2), Bestände,
  Bewegungshistorie und FA-Liste.

## Ergebnisse

| Bereich | Ergebnis | Beobachtung |
|---|---|---|
| Startseite | OK | Dashboard lädt. |
| Lager | OK | Einbuchung, Mehrfach-Einbuchung, Ausbuchung, Umbuchung, Bestände, Bewegungshistorie, Sage-Lagerbuchungen, Lagerplatz ausbuchen, Lagerplatz umbuchen laden ohne Fehler (Formulare nur geöffnet). Filter Bestände `filterArticle=00529104` bzw. „Bodenheizplatte“: 1 Treffer; Bewegungshistorie gleicher Artikel: 1 Treffer. |
| FA-Struktur, Beschichtung, Vormontage | OK | Alle Ansichten laden; Beschichtung-Druck (`/FaHierarchyBeschichtung/Print`) lädt. |
| Fertigungsaufträge, Leitstand | OK / **FEHLER** (siehe unten) | FA-Liste und Leitstand laden; Filter `filterOrderNumber=1037045` → 1 Gruppe, `filterArticleNumber=80132588` → 1 Gruppe. |
| Kommissionierung, Kommissionierlisten | OK | Warteschlange, Stücklisten `/Picking/Bom/24`–`27`, Liste/Summiert, Druck und Druck summiert laden. |
| Bestellungen | OK | Lagerbestellungen (meine Listen, Lager), Meine Fehlteile, Fehlteile (Lager) laden; keine Bestellungen vorhanden. |
| BDE | OK / Beobachtung | Cockpit, Buchungen, Stammdaten (inkl. „Operator bearbeiten“), Schichtkalender laden. `/BdeTerminal` leitet auf die Startseite um, Meldung: „Für diesen Benutzer ist kein Terminal konfiguriert. Bitte an den BDE-Admin wenden.“ Das ist erwartbar für `admin`; das Terminal selbst ist damit nicht geprüft. |
| Reporting, Tracking | OK | OSEON AG-Übersicht, OSEON Teileverfolgung laden. |
| Stammdaten | OK | Artikel (29 377 Einträge, Suche `50000423` → 1, unbekannter Begriff → „Keine Artikel vorhanden.“, Seite 2 → 51–100), Lagerplätze inkl. Bearbeiten und Etikettendruck, Werkbänke, Kategorien, Merkmale, FA-Vorbau-AG, FA-Merkmale, Benutzer, Arbeitsplätze, Rollen (jeweils Bearbeiten-Seiten geöffnet, nicht gespeichert), Einstellungen, Service-Einstellungen, Standorteinstellungen, Aktivitäts-Protokoll laden. |
| Hilfe, Änderungen | OK | Beide laden. |

## FEHLER

**FA-Liste zeigt an Sub-FA-Zeilen einen Stücklisten-Link, den der Benutzer nicht öffnen darf.**
In der FA-Liste (`/ProductionOrders`) haben alle 38 Sub-FA-Zeilen der HauptFA 1035235 das
Stücklisten-Symbol `/FaWorklist/Bom/<id>`, z. B. `/FaWorklist/Bom/101`. Für `admin` führt der Link
auf `/Account/AccessDenied` mit „Zugriff verweigert — Sie haben keine Berechtigung für diesen
Bereich. Bitte wenden Sie sich an einen Administrator, um Stammdaten-Zugriff zu erhalten.“ Auch
`/FaWorklist` und `/FaCompletion` sind für diesen Benutzer gesperrt. Laut Testszenarien-Index
(TS-79) hängt der Link an der Vorbau-Berechtigung; bei einem Benutzer ohne diese Berechtigung dürfte
er nicht erscheinen. Der Text der Sperrseite spricht von „Stammdaten-Zugriff“, obwohl es um die
FA-Abarbeitungsliste geht.

## Nicht geprüft
- **Speichern und Buchen:** alle speichernden und buchenden Abläufe (Einbuchen, Umbuchen,
  Kommissionieren, BDE-Buchungen, Stammdaten speichern), weil der Rundgang nur lesend war.
- **BDE-Terminal:** Für `admin` ist kein Terminal konfiguriert.
- **„Drucken erst beim zweiten Klick“:** Der Chrome-Tab lief im Hintergrund, Mausklicks sind dort
  nicht verlässlich. Das muss von Hand mit Chrome im Vordergrund geprüft werden.
- **AKE-System:** Es war nicht Teil des Auftrags.

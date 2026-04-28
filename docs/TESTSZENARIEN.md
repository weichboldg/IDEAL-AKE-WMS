# Testszenarien — IDEAL-AKE WMS

**Stand:** 2026-04-20 (v1.8.2)

Dieses Dokument enthaelt alle manuellen Testszenarien fuer die End-to-End-Abnahme der Anwendung.
Es ist die **Single Source of Truth fuer die UAT** — bei jedem neuen Feature ODER Bugfix MUSS dieses
Dokument aktualisiert werden (siehe CLAUDE.md → "Testszenarien-Pflicht").

## Konventionen

- Jedes Szenario hat eine eindeutige Id `TS-Bereich.Nummer`.
- Vorbedingungen werden explizit aufgefuehrt — wenn Datensaetze fehlen, kann das Szenario nicht durchgefuehrt werden.
- Erwartetes Verhalten ist beobachtbar (UI-Zustand, Toast-Meldung, DB-Zustand).
- Negativfaelle dokumentieren Validierung und Edge-Cases.
- Konkrete Beispieldaten (FA-Nummern, Benutzernamen) sind Platzhalter — reale Testnummern vor UAT eintragen.

---

## Index

| Bereich | Abschnitt | Szenarien |
|---------|-----------|-----------|
| 1. Authentifizierung & Zugriff | [→](#1-authentifizierung--zugriff) | TS-1.1 – TS-1.7 |
| 2. Lager | [→](#2-lager) | TS-2.1 – TS-2.11 |
| 3. Stammdaten | [→](#3-stammdaten) | TS-3.1 – TS-3.16 |
| 4. Fertigungsauftraege | [→](#4-fertigungsauftraege) | TS-4.1 – TS-4.10 |
| 5. Stueckliste (BOM) | [→](#5-stueckliste-bom) | TS-5.1 – TS-5.9 |
| 6. Kommissionierung / Picking | [→](#6-kommissionierung--picking) | TS-6.1 – TS-6.10 |
| 7. OSEON Teileverfolgung | [→](#7-oseon-teileverfolgung) | TS-7.1 – TS-7.10 |
| 8. BDE Phase 1 | [→](#8-bde-phase-1) | TS-8.1 – TS-8.15 |
| 9. BDE Phase 2.1 — Werkbank-Erweiterungen | [→](#9-bde-phase-21--werkbank-erweiterungen) | TS-9.1 – TS-9.5 |
| 10. BDE Phase 2.2 — Mehrfachanmeldung + Zeit-Split | [→](#10-bde-phase-22--mehrfachanmeldung--zeit-split) | TS-10.1 – TS-10.15 |
| 11. Bestellungen / Bedarfsmeldungen | [→](#11-bestellungen--bedarfsmeldungen) | TS-11.1 – TS-11.7 |
| 12. Print + OSEON-Tracking-Verbesserungen | [→](#12-print--oseon-tracking-verbesserungen) | TS-12.1 – TS-12.6 |
| 13. Spalten-Konfiguration & Filter | [→](#13-spalten-konfiguration--filter) | TS-13.1 – TS-13.7 |
| 14. Service / Sync (read-only Verifikation) | [→](#14-service--sync-read-only-verifikation) | TS-14.1 – TS-14.4 |

---

## 1. Authentifizierung & Zugriff

### TS-1.1 — Login mit korrekten Zugangsdaten

**Vorbedingungen:**
- Benutzer `testuser` mit Passwort `Test1234` existiert im System (Stammdaten → Benutzer).
- Anwendung ist erreichbar unter `https://<server>/`.

**Schritte:**
1. Browser oeffnen und Adresse der WMS-Anwendung aufrufen.
2. Benutzername `testuser` und Passwort `Test1234` eingeben.
3. Auf "Anmelden" klicken.

**Erwartetes Verhalten:**
- Weiterleitung auf das Dashboard.
- In der Navigationsleiste oben rechts erscheint der Benutzername.
- Kein Fehler-Banner sichtbar.

---

### TS-1.2 — Login mit falschem Passwort

**Vorbedingungen:**
- Benutzer `testuser` existiert.

**Schritte:**
1. Login-Seite aufrufen.
2. Benutzername `testuser` und falsches Passwort `FalschXYZ` eingeben.
3. Auf "Anmelden" klicken.

**Erwartetes Verhalten:**
- Seite bleibt auf der Login-Seite.
- Fehlermeldung "Benutzername oder Passwort ist falsch." wird angezeigt.
- Kein Redirect auf das Dashboard.

---

### TS-1.3 — Session-Timeout nach Inaktivitaet

**Vorbedingungen:**
- Benutzer ist eingeloggt.
- Session-Timeout ist auf 8 Stunden konfiguriert (Standard).

**Schritte:**
1. Als Benutzer einloggen.
2. 8 Stunden warten (oder: in der DB die Session-Erstellung auf vor 8 Stunden setzen).
3. Eine beliebige Seite aufrufen.

**Erwartetes Verhalten:**
- Weiterleitung zur Login-Seite.
- Kein Direktzugriff auf geschuetzte Seiten ohne erneuten Login.

---

### TS-1.4 — Rollenbasierter Zugriff: Picking-User kann keine Stammdaten oeffnen

**Vorbedingungen:**
- Benutzer `picker1` hat nur die Rolle `picking`.

**Schritte:**
1. Als `picker1` einloggen.
2. URL `https://<server>/Users` direkt im Browser aufrufen (Stammdaten → Benutzer).

**Erwartetes Verhalten:**
- Zugriff verweigert — Weiterleitung auf "Zugriff verweigert"-Seite ODER Redirect auf Dashboard mit Warn-Toast.
- Keine Benutzerliste sichtbar.

---

### TS-1.5 — Rollenbasierter Zugriff: Tracking-User kann Teileverfolgung oeffnen

**Vorbedingungen:**
- Benutzer `tracker1` hat die Rolle `tracking`.
- AppSetting `TeileverfolgungAktiv = true`.

**Schritte:**
1. Als `tracker1` einloggen.
2. Im Menue auf "Teileverfolgung → OSEON Auftraege" klicken.

**Erwartetes Verhalten:**
- OSEON-Teileverfolgungsliste wird geladen.
- Kein Fehler, kein Zugriff-verweigert.

**Negativfall:**
- `tracker1` versucht, Lagerbestand zu buchen (URL `/StockMovements/In`): Zugriff verweigert.

---

### TS-1.6 — Admin-Wildcard: Admin-User hat Zugriff auf alle Bereiche

**Vorbedingungen:**
- Benutzer `admin` mit Passwort leer (Standard-Seeding).

**Schritte:**
1. Als `admin` einloggen.
2. Folgende Menuepunkte nacheinander aufrufen: Lager, Fertigungsauftraege, Teileverfolgung, Stammdaten, BDE.

**Erwartetes Verhalten:**
- Alle Seiten oeffnen sich ohne Fehler.
- Kein "Zugriff verweigert".

---

### TS-1.7 — BDE-Modul nicht sichtbar wenn BdeAktiv = false

**Vorbedingungen:**
- AppSetting `BdeAktiv = false`.
- Benutzer hat BDE-Rollen (`bde_user`).

**Schritte:**
1. Als BDE-User einloggen.
2. Navigation oben anschauen.

**Erwartetes Verhalten:**
- Kein "BDE"-Menue-Eintrag sichtbar.
- Direktaufruf `https://<server>/BdeTerminal` zeigt Warn-Toast "BDE ist nicht aktiviert." und leitet auf Dashboard weiter.

---

## 2. Lager

### TS-2.1 — Einbuchung eines Artikels auf einen Lagerplatz

**Vorbedingungen:**
- Benutzer hat Rolle `stock` oder `admin`.
- Artikel `100-001` ("Schraube M8") existiert in Stammdaten.
- Lagerplatz `L01-A01` existiert.

**Schritte:**
1. Menue "Lager → Einbuchung" oeffnen.
2. Im Feld "Artikel" die Artikelnummer `100-001` eingeben (oder per Scan).
3. Im Feld "Lagerplatz" den Lagerplatz `L01-A01` eingeben.
4. Menge `10` eingeben.
5. Auf "Einbuchen" klicken.

**Erwartetes Verhalten:**
- Erfolgs-Toast "Einbuchung erfolgreich" erscheint.
- Bestand von `100-001` auf `L01-A01` wurde um 10 erhoehrt (pruefbar ueber Lager → Bestaende).
- Eintrag in der Bewegungshistorie: Typ "Einbuchung", Artikel `100-001`, Menge 10, Lagerplatz `L01-A01`.

**Negativfall:**
- Menge `0` eingeben → Validierungsfehler "Menge muss groesser als 0 sein".

---

### TS-2.2 — Ausbuchung eines Artikels

**Vorbedingungen:**
- Benutzer hat Rolle `stock` oder `admin`.
- Artikel `100-001` hat Bestand >= 5 auf Lagerplatz `L01-A01`.

**Schritte:**
1. Menue "Lager → Ausbuchung" oeffnen.
2. Artikel `100-001` auswaehlen.
3. Lagerplatz `L01-A01` auswaehlen.
4. Menge `5` eingeben.
5. Auf "Ausbuchen" klicken.

**Erwartetes Verhalten:**
- Erfolgs-Toast erscheint.
- Bestand wurde um 5 reduziert.

**Negativfall:**
- Mehr ausbuchen als Bestand vorhanden (z.B. 9999): Fehlermeldung "Bestand nicht ausreichend."

---

### TS-2.3 — Umbuchung von einem auf einen anderen Lagerplatz

**Vorbedingungen:**
- Benutzer hat Rolle `stock` oder `admin`.
- Artikel `100-001` hat Bestand >= 3 auf Lagerplatz `L01-A01`.
- Lagerplatz `L01-B02` existiert.

**Schritte:**
1. Menue "Lager → Umbuchung" oeffnen.
2. Quell-Lagerplatz `L01-A01` und Artikel `100-001` auswaehlen.
3. Ziel-Lagerplatz `L01-B02` auswaehlen.
4. Menge `3` eingeben.
5. Auf "Umbuchen" klicken.

**Erwartetes Verhalten:**
- Erfolgs-Toast erscheint.
- Bestand auf `L01-A01` um 3 reduziert, auf `L01-B02` um 3 erhoehrt.

---

### TS-2.4 — Lagerplatz ausbuchen (en bloc)

**Vorbedingungen:**
- Benutzer hat Rolle `stock_keyuser` oder `admin`.
- Lagerplatz `WAGEN-01` hat mehrere Artikel mit Bestand.

**Schritte:**
1. Menue "Lager → Lagerplatz ausbuchen" oeffnen.
2. Lagerplatz `WAGEN-01` auswaehlen.
3. Auf "Alle Artikel ausbuchen" klicken.
4. Bestaetigung im Bestaetigungs-Dialog anklicken.

**Erwartetes Verhalten:**
- Alle Artikel des Lagerplatzes `WAGEN-01` werden ausgebucht (Bestand = 0).
- Erfolgs-Toast erscheint.

**Negativfall:**
- Benutzer mit Rolle `stock` (kein `stock_keyuser`) versucht, die Seite aufzurufen: Zugriff verweigert.

---

### TS-2.5 — Lagerplatz umbuchen (en bloc)

**Vorbedingungen:**
- Benutzer hat Rolle `stock_keyuser` oder `admin`.
- Lagerplatz `WAGEN-01` hat Artikel mit Bestand.
- Ziel-Lagerplatz `L01-A05` existiert.

**Schritte:**
1. Menue "Lager → Lagerplatz umbuchen" oeffnen.
2. Quell-Lagerplatz `WAGEN-01` auswaehlen → Bestand wird angezeigt.
3. Ziel-Lagerplatz `L01-A05` auswaehlen.
4. Auf "Alle X Artikel umbuchen" klicken.
5. Bestaetigung im Dialog anklicken.

**Erwartetes Verhalten:**
- Alle Artikel von `WAGEN-01` sind jetzt auf `L01-A05` gebucht.
- Bestand auf `WAGEN-01` = 0.
- Erfolgs-Toast erscheint.

---

### TS-2.6 — Bestandsuebersicht anzeigen und filtern

**Vorbedingungen:**
- Benutzer hat Rolle `stock` oder `admin`.
- Mindestens 5 Artikel haben Bestand im Lager.

**Schritte:**
1. Menue "Lager → Bestaende" oeffnen.
2. Im Filter oben "100" eingeben.
3. Ergebnis anschauen.

**Erwartetes Verhalten:**
- Nur Artikel deren Artikelnummer oder Bezeichnung "100" enthalten werden angezeigt.
- Tabelle aktualisiert sich ohne Seiten-Reload.

---

### TS-2.7 — Bewegungshistorie filtern nach Zeitraum

**Vorbedingungen:**
- Benutzer hat Rolle `stock` oder `admin`.
- Bewegungen aus den letzten 7 Tagen vorhanden.

**Schritte:**
1. Menue "Lager → Bewegungshistorie" oeffnen.
2. Von-Datum auf "heute - 7 Tage" und Bis-Datum auf "heute" setzen.
3. Auf "Filtern" klicken.

**Erwartetes Verhalten:**
- Nur Bewegungen des gewaehlten Zeitraums werden angezeigt.
- Spalten: Datum, Artikel, Lagerplatz, Typ, Menge, Benutzer.

---

### TS-2.8 — Artikelinfo aufrufen und Bestand pruefen

**Vorbedingungen:**
- Benutzer ist eingeloggt.
- Artikel `100-001` hat Bestand auf mehreren Lagerplaetzen.

**Schritte:**
1. Dashboard-Karte "Artikelinfo" anklicken oder URL `/Articles/Info` aufrufen.
2. Artikelnummer `100-001` eingeben.
3. Auf "Suchen" klicken.

**Erwartetes Verhalten:**
- Bezeichnung, Einheit, Artikelgruppe, Meldebestand, Gesamtbestand werden angezeigt.
- Tabelle "Lagerbestand nach Lagerplatz" zeigt alle Lagerplaetze mit Menge.
- Link "VAULT" oeffnet Artikel im VAULT-System (neuer Tab).

**Negativfall:**
- Artikelnummer die nicht existiert eingeben → "Artikel nicht gefunden" Hinweis.

---

### TS-2.9 — Meldebestand-Warnung bei Unterschreitung

**Vorbedingungen:**
- Artikel `100-002` hat Meldebestand 20, aktueller Gesamtbestand ist 10.

**Schritte:**
1. Artikelinfo fuer `100-002` aufrufen.

**Erwartetes Verhalten:**
- Warnung sichtbar: "Meldebestand unterschritten" oder farbliche Markierung (rot/orange).
- Gesamtbestand und Meldebestand werden beide angezeigt.

---

### TS-2.10 — Einbuchung mit offenem Bedarf verknuepfen

**Vorbedingungen:**
- AppSetting `BestellungenAktiv = true`.
- Offene Bedarfsmeldung fuer Artikel `100-003` vorhanden.
- Benutzer hat Rolle `stock` oder `admin`.

**Schritte:**
1. Menue "Lager → Einbuchung" oeffnen.
2. Artikel `100-003` auswaehlen.
3. Menge und Lagerplatz eingeben.
4. Das System zeigt einen Hinweis "Offene Bedarfsmeldungen" mit der Bedarfsmeldung.
5. Haken bei der Bedarfsmeldung setzen.
6. Auf "Einbuchen" klicken.

**Erwartetes Verhalten:**
- Einbuchung wird gespeichert.
- Bedarfsmeldung wird als "Erfuellt" markiert.
- Buchung ist in der Bedarfsmeldung als "FulfilledByStockMovementId" hinterlegt.

---

### TS-2.11 — QR-Code Scan bei Einbuchung (Kamera-Scan)

**Vorbedingungen:**
- Anwendung laeuft ueber HTTPS.
- Geraet hat Kamera.
- Artikel `100-001` hat QR-Code im Format `100-001;Bezeichnung;FA-12345`.

**Schritte:**
1. Menue "Lager → Einbuchung" oeffnen.
2. Scan-Button neben dem Artikel-Feld anklicken.
3. QR-Code des Artikels mit der Kamera scannen.

**Erwartetes Verhalten:**
- Artikelnummer `100-001` wird automatisch im Artikelfeld eingetragen.
- Bezeichnung und weitere Daten werden geladen.
- Kamera-Modal schliesst sich.

---

## 3. Stammdaten

### TS-3.1 — Benutzer anlegen

**Vorbedingungen:**
- Benutzer hat Rolle `masterdata` oder `admin`.

**Schritte:**
1. Menue "Stammdaten → Benutzer" oeffnen.
2. Auf "Neuer Benutzer" klicken.
3. Benutzername `neueruser`, Passwort `Test5678`, Anzeigename `Neuer User` eingeben.
4. Auf "Speichern" klicken.

**Erwartetes Verhalten:**
- Erfolgs-Toast "Benutzer wurde angelegt."
- Neuer Benutzer erscheint in der Benutzerliste.

**Negativfall:**
- Benutzername der bereits existiert eingeben → Fehler "Benutzername bereits vergeben."

---

### TS-3.2 — Benutzer-Rollen zuweisen

**Vorbedingungen:**
- Benutzer `neueruser` existiert ohne Rollen.
- Benutzer hat Rolle `masterdata` oder `admin`.

**Schritte:**
1. Stammdaten → Benutzer → `neueruser` bearbeiten.
2. Rolle `picking` in der Rollenliste aktivieren.
3. Auf "Speichern" klicken.

**Erwartetes Verhalten:**
- Erfolgs-Toast erscheint.
- `neueruser` hat nun Zugriff auf Kommissionierungs-Funktionen.

---

### TS-3.3 — Workstation (Werkbank) anlegen und BDE aktivieren

**Vorbedingungen:**
- Benutzer hat Rolle `masterdata` oder `admin`.
- AppSetting `BdeAktiv = true`.

**Schritte:**
1. Stammdaten → Werkbaenke → "Neue Werkbank".
2. Name `WB-TEST`, Beschreibung `Testwerkbank` eingeben.
3. Im Block "BDE-Einstellungen" den Toggle "BDE aktiv" aktivieren.
4. Feld "Default-Arbeitsgang (BDE)" mit `PRODUKTION` befuellen.
5. Auf "Speichern" klicken.

**Erwartetes Verhalten:**
- Werkbank `WB-TEST` erscheint in der Liste.
- Spalte "BDE" zeigt gruenes Badge "Aktiv".
- Die Werkbank erscheint bei Buchungs-Dropdowns und im Cockpit.

**Negativfall:**
- Name leer lassen → Pflichtfeld-Fehler.

---

### TS-3.4 — Werkbank BDE-Inaktiv: Booking wird abgewiesen

**Vorbedingungen:**
- Werkbank `WB-INAKTIV` hat `BdeAktiv = false`.
- AppSetting `BdeAktiv = true`.
- BDE-Operator `OP-001` existiert.
- FA `FA-99999` hat einen offenen Arbeitsgang an `WB-INAKTIV`.

**Schritte:**
1. BDE-Terminal fuer Werkbank `WB-INAKTIV` aufrufen.
2. Personalnummer `OP-001` scannen / eingeben.
3. Einen Arbeitsgang auswaehlen und "Produktion starten" anklicken.

**Erwartetes Verhalten:**
- Fehlermeldung "Werkbank ist nicht fuer BDE aktiviert." erscheint.
- Keine Buchung wird angelegt.

---

### TS-3.5 — Lagerplatz anlegen

**Vorbedingungen:**
- Benutzer hat Rolle `masterdata` oder `admin`.

**Schritte:**
1. Stammdaten → Lagerplaetze → "Neuer Lagerplatz".
2. Bezeichnung `TEST-L01` eingeben.
3. Auf "Speichern" klicken.

**Erwartetes Verhalten:**
- Lagerplatz `TEST-L01` erscheint in der Liste.

---

### TS-3.6 — Artikel bearbeiten: Kategorie setzen

**Vorbedingungen:**
- Benutzer hat Rolle `masterdata` oder `admin`.
- Artikel `100-001` existiert.
- Artikelkategorie `Lackierteile` existiert (Stammdaten → Artikelkategorien).

**Schritte:**
1. Stammdaten → Artikel → `100-001` bearbeiten.
2. Kategorie-Dropdown auf `Lackierteile` setzen.
3. Auf "Speichern" klicken.

**Erwartetes Verhalten:**
- Erfolgs-Toast erscheint.
- Artikel `100-001` ist jetzt der Kategorie `Lackierteile` zugewiesen.

---

### TS-3.7 — Artikelkategorie anlegen

**Vorbedingungen:**
- Benutzer hat Rolle `masterdata` oder `admin`.

**Schritte:**
1. Stammdaten → Artikelkategorien → "Neue Kategorie".
2. Name `Sonderteil` eingeben.
3. Auf "Speichern" klicken.

**Erwartetes Verhalten:**
- Kategorie `Sonderteil` erscheint in der Liste.
- Sie steht als Option im Artikel-Edit-Dropdown zur Verfuegung.

---

### TS-3.8 — Artikelmerkmale pflegen

**Vorbedingungen:**
- Benutzer hat Rolle `masterdata` oder `admin`.
- Artikel `100-001` existiert.
- Merkmal `Lackiert` (Typ Boolean) existiert unter Stammdaten → Artikelmerkmale.

**Schritte:**
1. Stammdaten → Artikel → `100-001` bearbeiten.
2. Merkmal `Lackiert` auf "Ja" setzen.
3. Auf "Speichern" klicken.

**Erwartetes Verhalten:**
- Merkmal wird gespeichert.
- In der Artikeluebersicht erscheint die Spalte `Lackiert` und zeigt den Wert.

---

### TS-3.9 — Settings: Feature-Toggle LeitstandAktiv

**Vorbedingungen:**
- Benutzer hat Rolle `masterdata` oder `admin`.

**Schritte:**
1. Stammdaten → Einstellungen oeffnen.
2. Toggle "Leitstand (Kommissionier-Freigabe)" auf aktiv schalten.
3. Auf "Speichern" klicken.

**Erwartetes Verhalten:**
- Erfolgs-Toast erscheint.
- Im Menue erscheint jetzt der Eintrag "Kommissionierung" (fuer Picking-User) und "Fertigungsauftraege" zeigt Freigabe-Buttons (fuer Leitstand-User).

---

### TS-3.10 — Settings: BdeAktiv aktivieren

**Vorbedingungen:**
- Benutzer hat Rolle `masterdata` oder `admin`.
- AppSetting `BdeAktiv = false`.

**Schritte:**
1. Stammdaten → Einstellungen oeffnen.
2. Toggle "BDE (Betriebsdatenerfassung)" aktivieren.
3. Auf "Speichern" klicken.

**Erwartetes Verhalten:**
- BDE-Menue erscheint in der Navigation fuer Benutzer mit BDE-Rolle.
- BDE-Terminal, Cockpit und Buchungsuebersicht sind erreichbar.

---

### TS-3.11 — Settings: BdeNurFaMeldung aktivieren

**Vorbedingungen:**
- AppSetting `BdeAktiv = true`.
- Benutzer hat Rolle `masterdata` oder `admin`.

**Schritte:**
1. Stammdaten → Einstellungen oeffnen.
2. Toggle "BDE — Nur FA-Meldung" aktivieren.
3. Textfeld "Default-Arbeitsgang" mit `PRODUKTION` befuellen.
4. Auf "Speichern" klicken.

**Erwartetes Verhalten:**
- Einstellungen gespeichert.
- BDE-Terminal zeigt im NurFA-Modus Fertigungsauftraege statt Arbeitsgaenge als Auswahl.

---

### TS-3.12 — Empfaengergruppe anlegen und Empfaenger hinzufuegen

**Vorbedingungen:**
- AppSetting `BestellungenAktiv = true`.
- Benutzer hat Rolle `picking` oder `stock` oder `admin`.

**Schritte:**
1. Stammdaten → Empfaengergruppen → "Neue Gruppe".
2. Name `Einkauf` eingeben.
3. Speichern — Redirect auf Bearbeiten-Seite der neuen Gruppe.
4. Empfaenger `Max Mustermann`, `max@firma.at` hinzufuegen.
5. Speichern.

**Erwartetes Verhalten:**
- Gruppe `Einkauf` mit Empfaenger `Max Mustermann` erscheint in der Liste.
- Empfaenger `max@firma.at` ist als aktiv markiert.

---

### TS-3.13 — BDE-Operator anlegen

**Vorbedingungen:**
- AppSetting `BdeAktiv = true`.
- Benutzer hat Rolle `bde_shiftlead` oder `admin`.

**Schritte:**
1. BDE → Stammdaten → Operatoren → "Neuer Operator".
2. Personalnummer `P-100`, Vorname `Hans`, Nachname `Maier` eingeben.
3. Auf "Speichern" klicken.

**Erwartetes Verhalten:**
- Operator "Hans Maier" (P-100) erscheint in der Operator-Liste.
- Am Terminal kann `P-100` gescannt werden.

**Negativfall:**
- Personalnummer die bereits vergeben ist (`P-100`) nochmals anlegen → Fehler "Personalnummer bereits vorhanden."

---

### TS-3.14 — BDE-Aktivitaetskategorie anlegen

**Vorbedingungen:**
- AppSetting `BdeAktiv = true`.
- Benutzer hat Rolle `bde_shiftlead` oder `admin`.

**Schritte:**
1. BDE → Stammdaten → Aktivitaetskategorien → "Neue Kategorie".
2. Code `WART`, Name `Wartung` eingeben.
3. Auf "Speichern" klicken.

**Erwartetes Verhalten:**
- Kategorie `WART — Wartung` erscheint in der Liste.
- Im BDE-Terminal steht "Wartung" als waehbare Aktivitaet zur Verfuegung (ungeplante Taetigkeiten-Gruppe).

---

### TS-3.15 — BDE-Terminal konfigurieren

**Vorbedingungen:**
- AppSetting `BdeAktiv = true`.
- Benutzer hat Rolle `bde_admin` oder `admin`.
- Benutzer `terminal-user1` und Werkbank `WB-01` (BdeAktiv = true) existieren.

**Schritte:**
1. BDE → Stammdaten → Terminals → "Neues Terminal".
2. Benutzer `terminal-user1`, Default-Werkbank `WB-01` auswaehlen.
3. Beschreibung `Tablet WB-01 West` eingeben.
4. Speichern.

**Erwartetes Verhalten:**
- Terminal ist angelegt.
- Wenn `terminal-user1` das BDE-Terminal aufruft, ist `WB-01` vorausgewaehlt.

---

### TS-3.16 — View-Einstellungen eines Benutzers zuruecksetzen (Admin)

**Vorbedingungen:**
- Benutzer `testuser2` hat individuelle Spalten-Einstellungen gespeichert.
- Aktuell eingeloggter Benutzer hat Rolle `masterdata` oder `admin`.

**Schritte:**
1. Stammdaten → Benutzer → `testuser2` bearbeiten.
2. Schaltflaeche "View-Einstellungen zuruecksetzen" anklicken.
3. Bestaetigung im Dialog.

**Erwartetes Verhalten:**
- Erfolgs-Toast "View-Einstellungen zurueckgesetzt."
- Wenn `testuser2` sich das naechste Mal anmeldet, sieht er die Standard-Spalten-Konfiguration.

---

## 4. Fertigungsauftraege

### TS-4.1 — FA-Liste aufrufen und filtern

**Vorbedingungen:**
- Benutzer hat Rolle `picking`, `tracking` oder `admin`.
- Mindestens 10 FAs in der Datenbank.

**Schritte:**
1. Menue "Fertigungsauftraege" oeffnen.
2. Im Spaltenkopf-Filter fuer "FA-Nummer" den Wert `2604` eingeben.

**Erwartetes Verhalten:**
- Nur FAs deren Nummer `2604` enthaelt werden angezeigt.
- Anzahl Treffer wird aktualisiert.

---

### TS-4.2 — FA nach Kalenderwoche filtern

**Vorbedingungen:**
- FAs mit Fertigungstermin in KW15 vorhanden.

**Schritte:**
1. FA-Liste oeffnen.
2. Im Spaltenfilter der Datumsspalte (Fertigungstermin) das Kalender-Icon anklicken.
3. Im Monatskalender auf die KW-Nummer 15 klicken.

**Erwartetes Verhalten:**
- Nur FAs mit Fertigungstermin in KW15 werden angezeigt.

---

### TS-4.3 — Glas/Zukauf-Flag setzen (Inline-Checkbox)

**Vorbedingungen:**
- FA `FA-2604001` existiert mit `IsGlas = false`.
- Benutzer hat Rolle `picking` oder `admin`.

**Schritte:**
1. FA-Liste oeffnen.
2. Zeile von `FA-2604001` finden.
3. Checkbox in Spalte "Glas" anklicken.

**Erwartetes Verhalten:**
- Checkbox wird sofort gespeichert (kein extra Speichern noetig).
- Bei erneutem Laden der Seite ist die Checkbox noch gesetzt.

---

### TS-4.4 — FA als erledigt markieren

**Vorbedingungen:**
- FA `FA-2604001` ist nicht erledigt.
- Benutzer hat Rolle `picking` oder `admin`.

**Schritte:**
1. FA-Liste oeffnen.
2. Zeile von `FA-2604001` finden.
3. Gruenen Haekchen-Button anklicken.

**Erwartetes Verhalten:**
- FA verschwindet aus der Standard-Liste.
- Ueber Filter "Erledigte anzeigen" ist sie wieder sichtbar, mit entsprechendem Status.

---

### TS-4.5 — enaio DMS-Dokument oeffnen

**Vorbedingungen:**
- AppSetting `EnaioDmsEnabled = true` (Service-Einstellungen).
- FA `FA-2604002` hat ein Dokument in enaio.

**Schritte:**
1. FA-Liste oeffnen.
2. Zeile von `FA-2604002` finden.
3. Orange Dokument-Icon neben der FA-Nummer anklicken.

**Erwartetes Verhalten:**
- Dokument oeffnet sich im enaio Viewer (neuer Browser-Tab).

---

### TS-4.6 — Lack-T Checkbox setzen (Lackierteil-Auftrag)

**Vorbedingungen:**
- AppSetting `LackierteilKategorieName` ist auf `Lackierteile` gesetzt.
- FA `FA-2604003` enthaelt Lackierteile in seiner Stueckliste.

**Schritte:**
1. FA-Liste oeffnen.
2. Zeile von `FA-2604003` finden — Spalte "Lack-T" ist sichtbar und zeigt eine Checkbox.
3. Checkbox anklicken.

**Erwartetes Verhalten:**
- Lackierteil-Status wird gespeichert.
- Beschichtungstermin-Spalte wird nicht mehr rot markiert.

---

### TS-4.7 — OSEON-Teileverfolgung ueber FA-Liste oeffnen

**Vorbedingungen:**
- AppSetting `TeileverfolgungAktiv = true`.
- FA `FA-2604001` hat OSEON-Auftraege.

**Schritte:**
1. FA-Liste oeffnen.
2. Bei FA `FA-2604001` den blauen "i"-Button anklicken.

**Erwartetes Verhalten:**
- OSEON-Teileverfolgung oeffnet sich, vorgefiltert auf die FA-Nummer.
- Filter "Fertige anzeigen" ist automatisch aktiv.

---

### TS-4.8 — Leitstand-Freigabe: FA freigeben

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`.
- Benutzer hat Rolle `leitstand` oder `admin`.
- FA `FA-2604005` hat eine Artikelnummer und ist noch nicht freigegeben.

**Schritte:**
1. FA-Liste oeffnen.
2. Zeile von `FA-2604005` finden.
3. Freigabe-Button anklicken.

**Erwartetes Verhalten:**
- FA ist freigegeben (Status-Aenderung sichtbar).
- Im Menuepunkt "Kommissionierung" steigt der Badge-Zaehlstand.
- Picker sieht `FA-2604005` in der Kommissionierliste.

**Negativfall:**
- FA ohne Artikelnummer freigeben → Fehlermeldung "Artikelnummer fehlt, Freigabe nicht moeglich."

---

### TS-4.9 — Leitstand-Massenfreigabe

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`.
- Benutzer hat Rolle `leitstand` oder `admin`.
- Mehrere FAs mit Artikelnummer, nicht freigegeben.

**Schritte:**
1. FA-Liste oeffnen.
2. Checkboxen fuer 3 FAs setzen (darunter eine ohne Artikelnummer).
3. "Massenfreigabe" Button anklicken.

**Erwartetes Verhalten:**
- 2 FAs mit Artikelnummer werden freigegeben.
- 1 FA ohne Artikelnummer wird uebersprungen.
- Meldung zeigt "2 freigegeben, 1 uebersprungen (keine Artikelnummer)".

---

### TS-4.10 — Prioritaet eines freigegebenen FA aendern

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`.
- FA `FA-2604005` ist freigegeben mit Prioritaet 3.
- Benutzer hat Rolle `leitstand` oder `admin`.

**Schritte:**
1. FA-Liste oeffnen.
2. Prioritaetsfeld der Zeile `FA-2604005` anklicken.
3. Wert auf `1` aendern und Enter druecken.

**Erwartetes Verhalten:**
- Prioritaet wird sofort gespeichert (AJAX).
- Tabelle sortiert sich entsprechend neu.

---

## 5. Stueckliste (BOM)

### TS-5.1 — Stueckliste eines FA aufrufen

**Vorbedingungen:**
- FA `FA-2604001` existiert und hat eine Stueckliste (aus SAGE oder OSEON).
- Benutzer hat Rolle `picking` oder `admin`.

**Schritte:**
1. FA-Liste oeffnen.
2. FA-Nummer `FA-2604001` anklicken.

**Erwartetes Verhalten:**
- Stueckliste wird geoeffnet mit allen Bauteilen.
- Spalten: Position, Artikelnummer, Bezeichnung, Menge, Einheit, Lagerplatz, Bestand, Kategorie.
- Datenquelle (SAGE oder OSEON) wird angezeigt.

---

### TS-5.2 — BOM-Filter: Nur Fehlteile anzeigen

**Vorbedingungen:**
- Stueckliste von `FA-2604001` geoeffnet.
- Mindestens ein Bauteil hat Bestand 0.

**Schritte:**
1. Filter-Checkbox "Nur Fehlteile" aktivieren.

**Erwartetes Verhalten:**
- Nur Positionen mit Bestand < benoetiger Menge werden angezeigt.
- Positionen mit ausreichend Bestand werden ausgeblendet.

---

### TS-5.3 — Stueckliste drucken (Happy Path)

**Vorbedingungen:**
- Stueckliste von `FA-2604001` geoeffnet.

**Schritte:**
1. Button "Drucken" / "BOM drucken" anklicken.

**Erwartetes Verhalten:**
- Druckansicht oeffnet sich.
- FA-Nummer ist oben sichtbar.
- Zeile "Gedruckt von: [Anzeigename des aktuellen Users]" erscheint unterhalb der FA-Nummer.
- Nur die in der Tabellenansicht sichtbaren Spalten werden gedruckt (kein Druck von ausgeblendeten Spalten).

---

### TS-5.4 — Kommissionierung (Picking) aus Stueckliste starten

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = false` ODER FA ist freigegeben.
- FA `FA-2604001` hat Bauteile mit ausreichend Bestand.
- Lagerplatz `WAGEN-01` (Kommissionierwagen) existiert.
- Benutzer hat Rolle `picking` oder `admin`.

**Schritte:**
1. Stueckliste von `FA-2604001` oeffnen.
2. Quell-Lagerplatz auswaehlen (z.B. `L01-A01`).
3. Ziel-Lagerplatz auswaehlen (Kommissionierwagen, z.B. `WAGEN-01`).
4. Checkboxen fuer die zu pickenden Bauteile setzen.
5. Auf "Gepickte Artikel umbuchen" klicken.

**Erwartetes Verhalten:**
- Alle angehakten Artikel werden vom Quell- auf den Ziel-Lagerplatz gebucht.
- Status des FA aendert sich auf "in Kommissionierung" oder "abgeschlossen".
- Erfolgs-Toast erscheint.

---

### TS-5.5 — Bedarfsmeldung aus Stueckliste erstellen

**Vorbedingungen:**
- AppSetting `BestellungenAktiv = true`.
- Benutzer hat Rolle `picking` oder `stock` oder `admin`.
- Empfaengergruppe `Einkauf` fuer die Artikelgruppe des Bauteils konfiguriert.

**Schritte:**
1. Stueckliste eines FA oeffnen.
2. Bei einem Bauteil mit Bestand 0 den "Bestellen"-Button anklicken.
3. Im Modal Menge, Prioritaet (Dringend) und Empfaengergruppe auswaehlen.
4. Auf "Bedarfsmeldung senden" klicken.

**Erwartetes Verhalten:**
- Bedarfsmeldung wird angelegt.
- E-Mail wird an die Empfaengergruppe gesendet (pruefbar in Bestelluebersicht).
- Erfolgs-Toast erscheint.

---

### TS-5.6 — Sammelbestellung aus Stueckliste

**Vorbedingungen:**
- AppSetting `BestellungenAktiv = true`.
- Mehrere Bauteile haben Bestand 0.

**Schritte:**
1. Stueckliste oeffnen.
2. Mehrere Fehlteile-Checkboxen anhaaken.
3. "Sammelbestellung" Button anklicken.
4. Im Modal Empfaenger und Prioritaet setzen.
5. Absenden.

**Erwartetes Verhalten:**
- Fuer jedes ausgewaehlte Bauteil wird eine separate Bedarfsmeldung angelegt.
- Alle an denselben Empfaenger versendeten Positionen kommen in einer E-Mail.

---

### TS-5.7 — Foto bei Picking hochladen

**Vorbedingungen:**
- Stueckliste eines FA geoeffnet.

**Schritte:**
1. Foto-Upload-Button in der Stueckliste anklicken.
2. Bild auswaehlen und hochladen.

**Erwartetes Verhalten:**
- Bild wird gespeichert.
- Vorschau-Thumbnail erscheint in der Ansicht.

---

### TS-5.8 — Artikelinfo: BOM-Cache zeigt FAs in denen Artikel vorkommt

**Vorbedingungen:**
- AppSetting `BomCacheEnabled = true` im Service aktiv.
- BOM-Cache enthaelt Eintraege fuer Artikel `100-001`.

**Schritte:**
1. Artikelinfo fuer `100-001` aufrufen.

**Erwartetes Verhalten:**
- Tabelle "Teil enthalten in folgenden Fertigungsauftraegen" erscheint.
- Mindestens ein offener FA mit FA-Link, Fertigungs- und Liefertermin ist sichtbar.

---

### TS-5.9 — BOM-Druck: Spalten-Filter wird uebertragen

**Vorbedingungen:**
- Stueckliste eines FA geoeffnet.
- Spalte "Kategorie" wurde via Spalten-Konfiguration ausgeblendet.

**Schritte:**
1. Spalte "Kategorie" ausblenden (Zahnrad-Icon → Checkbox deaktivieren).
2. "BOM drucken" anklicken.

**Erwartetes Verhalten:**
- Druckansicht enthaelt keine Spalte "Kategorie".
- Alle anderen sichtbaren Spalten sind im Druck enthalten.

---

## 6. Kommissionierung / Picking

### TS-6.1 — Kommissionierliste aufrufen (mit LeitstandAktiv)

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`.
- Mindestens 2 FAs sind freigegeben.
- Benutzer hat Rolle `picking` oder `admin`.

**Schritte:**
1. Menue "Kommissionierung" oeffnen.

**Erwartetes Verhalten:**
- Tabelle mit freigegebenen FAs erscheint (Prioritaet, FA-Nummer, Artikel, Kunde, Komm.-Termin, Status).
- Nicht freigegebene FAs sind NICHT in der Liste.

---

### TS-6.2 — Kommissionierliste ohne Leitstand (Direktzugriff)

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = false`.
- Benutzer hat Rolle `picking` oder `admin`.

**Schritte:**
1. Menue "Fertigungsauftraege" oeffnen.
2. Einen FA anklicken → Stueckliste oeffnet sich.

**Erwartetes Verhalten:**
- Alle FAs sind direkt sichtbar (kein Freigabe-Filter).
- Stueckliste oeffnet sich direkt, kein extra "Kommissionierliste"-Schritt.

---

### TS-6.3 — Picker-Zuweisung bei Freigabe

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`, `KommissionierungMitZuweisung = true`.
- Benutzer `picker1` hat `IsPickingUser = true`.
- Benutzer hat Rolle `leitstand` oder `admin`.

**Schritte:**
1. FA-Liste oeffnen.
2. FA freigeben.
3. Im Freigabe-Dialog Picker `picker1` auswaehlen.
4. Bestaetigen.

**Erwartetes Verhalten:**
- FA erscheint in der Kommissionierliste von `picker1` als erstes (zugewiesen).
- `picker1` sieht standardmaessig nur seine zugewiesenen Auftraege.
- Ueber "Alle anzeigen" kann `picker1` alle freigegebenen Auftraege sehen.

---

### TS-6.4 — Menue-Badge zeigt Anzahl offener FAs

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`.
- Genau 3 FAs sind freigegeben und nicht erledigt.
- Benutzer hat Rolle `picking` oder `admin`.

**Schritte:**
1. Beliebige Seite laden.
2. Menuepunkt "Kommissionierung" anschauen.

**Erwartetes Verhalten:**
- Badge neben "Kommissionierung" zeigt `3`.

---

### TS-6.5 — Picking: Picking erst beim Umbuchen-Klick gespeichert

**Vorbedingungen:**
- Stueckliste eines FA geoeffnet mit Bauteilen.

**Schritte:**
1. Mehrere Checkboxen in der Stueckliste anhaaken.
2. OHNE auf "Gepickte Artikel umbuchen" zu klicken: Seite neu laden.

**Erwartetes Verhalten:**
- Keine Umbuchung wurde gespeichert.
- Checkboxen sind nach dem Reload wieder leer.

---

### TS-6.6 — Kommissionier-Status: In Kommissionierung

**Vorbedingungen:**
- FA `FA-2604010` hat 5 Bauteile. Davon 3 mit ausreichend Bestand.
- Benutzer hat Rolle `picking`.

**Schritte:**
1. Stueckliste von `FA-2604010` oeffnen.
2. 2 Bauteile anhaaken und "Gepickte Artikel umbuchen" klicken.

**Erwartetes Verhalten:**
- Status des FA wechselt auf "in Kommissionierung" (orange Badge).
- In der FA-Liste erscheint der neue Status.

---

### TS-6.7 — Kommissionier-Druck: Picking-Liste

**Vorbedingungen:**
- Stueckliste eines FA geoeffnet.

**Schritte:**
1. "Picking-Liste drucken" Button anklicken.

**Erwartetes Verhalten:**
- Druckansicht oeffnet sich mit allen Bauteilen.
- Zeile "Gedruckt von: [Anzeigename]" erscheint unter der FA-Nummer.

---

### TS-6.8 — Bestandsuebersicht FA-Filter

**Vorbedingungen:**
- FA `FA-2604010` hat Artikel auf einem Kommissionierwagen.
- Benutzer hat Rolle `stock` oder `picking`.

**Schritte:**
1. Lager → Bestaende oeffnen.
2. In Filterfeld "FA-Nummer" den Wert `FA-2604010` eingeben.

**Erwartetes Verhalten:**
- Nur Positionen die zu diesem FA gehoeren werden angezeigt.

---

### TS-6.9 — Picking-Zuweisung: Nicht-Picker erscheint nicht im Dropdown

**Vorbedingungen:**
- AppSetting `KommissionierungMitZuweisung = true`.
- Benutzer `planner1` hat `IsPickingUser = false`.

**Schritte:**
1. Leitstand-User oeffnet Freigabe-Dialog fuer einen FA.
2. Zuweisung-Dropdown anschauen.

**Erwartetes Verhalten:**
- `planner1` erscheint NICHT im Dropdown.
- Nur Benutzer mit `IsPickingUser = true` sind waehbar.

---

### TS-6.10 — Picking-Kommissionierliste: Spaltenfilter

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`.
- Mindestens 5 FAs in der Kommissionierliste.

**Schritte:**
1. Kommissionierliste oeffnen.
2. Im Spaltenfilter "Artikel" den Wert `Motor` eingeben.

**Erwartetes Verhalten:**
- Nur FAs mit Artikelbezeichnung die "Motor" enthaelt werden angezeigt.

---

## 7. OSEON Teileverfolgung

### TS-7.1 — OSEON-Auftragsliste aufrufen

**Vorbedingungen:**
- AppSetting `TeileverfolgungAktiv = true`.
- OSEON-Auftraege sind in die WMS-Datenbank synchronisiert.
- Benutzer hat Rolle `tracking` oder `admin`.

**Schritte:**
1. Menue "Teileverfolgung → OSEON Auftraege" oeffnen.

**Erwartetes Verhalten:**
- Liste der Kundenauftragsnummern wird angezeigt.
- Jede Gruppe zeigt Ampelfarbe und Summe der Sub-Auftraege.

---

### TS-7.2 — 3-Ebenen-Baum aufklappen

**Vorbedingungen:**
- OSEON-Liste geoeffnet mit mindestens einem Kundenauftrag.

**Schritte:**
1. Einen Kundenauftrag anklicken → Sub-Auftraege werden sichtbar.
2. Einen Sub-Auftrag anklicken → Arbeitsgaenge werden sichtbar.

**Erwartetes Verhalten:**
- Level 1 (Kundenauftragsnummer): Name, Ampel.
- Level 2 (Sub-Auftraege): OSEON-Auftragsnummer, Artikel, Werkbank, Mengen, Termin, Ampel.
- Level 3 (Arbeitsgaenge): AG-Nummer, Name, Status, Soll-Termin, Ampel.

---

### TS-7.3 — Ampelsystem pruefen

**Vorbedingungen:**
- Auftrag mit ueberfalligem Endtermin (gestern) vorhanden.
- Auftrag mit Termin heute vorhanden.
- Auftrag der fertig ist vorhanden.

**Schritte:**
1. OSEON-Liste aufrufen und Auftraege anschauen.

**Erwartetes Verhalten:**
- Ueberfaelliger Auftrag: rote Ampel.
- Auftrag faellig heute: gelbe Ampel (wenn innerhalb `OseonAmpelGelbTage`).
- Fertig/storniert: gruene Ampel.
- Kein Termin: graue Ampel.

---

### TS-7.4 — Filter: Fertige ausblenden/anzeigen

**Vorbedingungen:**
- OSEON-Liste geoeffnet.
- Mindestens ein fertiger Auftrag vorhanden.

**Schritte:**
1. Standard-Ansicht: Filter "Fertige anzeigen" ist deaktiviert.
2. Fertiger Auftrag ist nicht in der Liste sichtbar.
3. Toggle "Fertige anzeigen" aktivieren.

**Erwartetes Verhalten:**
- Nach Aktivierung erscheinen auch fertige Auftraege in der Liste.

---

### TS-7.5 — Filter: Werkbank-Filter

**Vorbedingungen:**
- Auftraege verschiedener Werkbaenke vorhanden.

**Schritte:**
1. OSEON-Liste oeffnen.
2. Im Werkbank-Dropdown eine spezifische Werkbank auswaehlen.

**Erwartetes Verhalten:**
- Nur Sub-Auftraege dieser Werkbank werden angezeigt.
- Gruppen ohne Treffer werden ausgeblendet.

---

### TS-7.6 — Artikelnummern-Suche mit QR-Scan

**Vorbedingungen:**
- OSEON-Liste oeffnet.
- Artikelnummer `100-001` kommt in mindestens einem Sub-Auftrag vor.

**Schritte:**
1. Im Filter-Bereich das Feld "Artikelnummer" anklicken.
2. Artikelnummer `100-001` manuell eingeben (ODER QR-Code scannen).

**Erwartetes Verhalten:**
- Nur Sub-Auftraege mit Artikel `100-001` werden angezeigt.
- Filterung ist client-seitig (kein Seiten-Reload).
- Leeres Feld: alle anzeigen.

---

### TS-7.7 — Lagerbestand-Modal pro Sub-Auftrag

**Vorbedingungen:**
- Sub-Auftrag `WA-12345` hat Artikel im Lager.
- Benutzer hat Rolle `tracking` oder `admin`.

**Schritte:**
1. OSEON-Liste oeffnen, Kundenauftrag aufklappen.
2. Bei Sub-Auftrag `WA-12345` das Lager-Icon (Kiste) anklicken.

**Erwartetes Verhalten:**
- Modal oeffnet sich mit Tabelle: Artikelnummer, Bezeichnung, Lagerplatz, Menge.
- Link "Details in Bestandsuebersicht →" oeffnet gefilterte Bestandsansicht (neuer Tab).

**Negativfall:**
- Sub-Auftrag ohne Lagerbuchungen: Modal zeigt "Keine Lagerbuchungen fuer diesen Auftrag."

---

### TS-7.8 — OSEON-Tracking: Alle aufklappen / zuklappen

**Vorbedingungen:**
- OSEON-Liste geoeffnet mit mehreren Gruppen.

**Schritte:**
1. Button "Alle aufklappen" anklicken.
2. Button "Alle zuklappen" anklicken.

**Erwartetes Verhalten:**
- "Alle aufklappen": alle Gruppen und Sub-Auftraege sind sichtbar.
- "Alle zuklappen": nur Level-1-Gruppen sichtbar, alle anderen eingeklappt.

---

### TS-7.9 — Nur OSEON-relevante AGs anzeigen

**Vorbedingungen:**
- Arbeitsgang-Konfiguration: nicht alle AGs sind als OSEON-relevant markiert.

**Schritte:**
1. OSEON-Liste oeffnen, Gruppe aufklappen bis Arbeitsgaenge sichtbar.
2. Toggle "Nur OSEON-relevante AGs" ist aktiv (Standard).
3. Toggle deaktivieren.

**Erwartetes Verhalten:**
- Deaktiviert: alle Arbeitsgaenge sichtbar (auch nicht-relevante).
- Aktiviert: nur als OSEON-relevant konfigurierte AGs sichtbar.

---

### TS-7.10 — AG-Konfiguration pflegen (Soll-Termin Offset)

**Vorbedingungen:**
- Benutzer hat Rolle `masterdata` oder `admin`.

**Schritte:**
1. Stammdaten → Einstellungen → AG-Konfiguration oeffnen.
2. Fuer AG "10 — Fraesen" den Offset-Wert auf `3` setzen.
3. Speichern.

**Erwartetes Verhalten:**
- Soll-Termin von AG "10" wird als Stanztermin + 3 Arbeitstage berechnet.
- Wochenenden und Feiertage werden beruecksichtigt.

---

## 8. BDE Phase 1

### TS-8.1 — BDE-Terminal aufrufen und Operator scannen

**Vorbedingungen:**
- AppSetting `BdeAktiv = true`.
- Terminal ist konfiguriert fuer Werkbank `WB-01` (BdeAktiv = true).
- Operator `Hans Maier` (Personalnummer `P-100`) existiert und ist aktiv.
- Benutzer hat Rolle `bde_user` oder `admin`.

**Schritte:**
1. Menue "BDE → Terminal" oeffnen.
2. Feld "Personalnummer" mit `P-100` befuellen (oder Barcode scannen).
3. Enter druecken oder Scan-Button bestaetigen.

**Erwartetes Verhalten:**
- Operator-Badge erscheint mit "Hans Maier (P-100)".
- Verfuegbare Arbeitsgaenge werden als Buttons angezeigt (gruppiert: produktiv / ungeplant).

---

### TS-8.2 — Ruesten starten (Setup-Buchung)

**Vorbedingungen:**
- Operator `P-100` ist am Terminal angemeldet.
- Offener Arbeitsgang `FA-2604001 / AG 10 Fraesen` ist an Werkbank `WB-01`.

**Schritte:**
1. Arbeitsgang-Button `FA-2604001 / AG 10 Fraesen` anklicken.
2. "Ruesten starten" waehlen.

**Erwartetes Verhalten:**
- Toast "Ruesten gestartet" erscheint.
- Terminal zeigt aktuelle Buchung mit Live-Timer.
- Im Cockpit: Werkbank `WB-01` zeigt Operator `Hans Maier`, Status "Ruesten", Timer.

---

### TS-8.3 — Produktion starten (von Ruesten wechseln)

**Vorbedingungen:**
- Operator `P-100` hat laufende Setup-Buchung auf `FA-2604001 / AG 10`.

**Schritte:**
1. "Produktion starten" Button klicken.

**Erwartetes Verhalten:**
- Setup-Buchung wird automatisch beendet.
- Neue Production-Buchung mit `ParentBookingId` = Setup-Buchung wird angelegt.
- Toast "Produktion gestartet" erscheint.
- Cockpit zeigt Status "Produktion".

---

### TS-8.4 — Teilfertigmeldung waehrend laufender Produktion

**Vorbedingungen:**
- Operator `P-100` hat laufende Production-Buchung auf `FA-2604001 / AG 10`.

**Schritte:**
1. "Teilfertigmeldung" Button klicken.
2. Im Mengen-Dialog: Gutmenge `5`, Ausschuss `1` eingeben.
3. Bestaetigen.

**Erwartetes Verhalten:**
- Teilmeldung wird gespeichert (BdeBookingQuantity mit IsFinal = false).
- Buchung laeuft weiter (kein Status-Wechsel).
- Toast "Teilmeldung gespeichert" erscheint.

---

### TS-8.5 — Produktion beenden mit Mengenmeldung

**Vorbedingungen:**
- Operator `P-100` hat laufende Production-Buchung auf `FA-2604001 / AG 10`.

**Schritte:**
1. "Beenden" Button klicken.
2. Im Mengen-Dialog: Gutmenge `20`, Ausschuss `0` eingeben.
3. Abschluss-Meldung bestaetigen.

**Erwartetes Verhalten:**
- Buchung wird als "Finished" abgeschlossen.
- BdeBookingQuantity-Eintrag mit IsFinal = true und GoodQuantity = 20 angelegt.
- Toast "Buchung abgeschlossen" erscheint.
- Cockpit: Werkbank `WB-01` zeigt keinen aktiven Operator mehr.

---

### TS-8.6 — Produktion pausieren und fortsetzen

**Vorbedingungen:**
- Operator `P-100` hat laufende Production-Buchung.

**Schritte:**
1. "Pause" Button klicken.
2. Mengen-Dialog (optional, Menge 0 ok).
3. Bestaetigen.
4. Operator `P-100` scannen (Neuanmeldung am Terminal).

**Erwartetes Verhalten:**
- Pausierte Buchung erscheint im Hinweis-Block "Pausierte Auftraege".
- "Fortsetzen"-Button bei der pausierten Buchung sichtbar.
- Nach Klick auf "Fortsetzen": neue Buchung mit `ParentBookingId` wird angelegt, Status wechselt auf "Resumed".

---

### TS-8.7 — Ungeplante Taetigkeit buchen (Aktivitaet)

**Vorbedingungen:**
- Operator `P-100` ist am Terminal angemeldet.
- Aktivitaetskategorie `WART — Wartung` existiert.

**Schritte:**
1. In der Gruppe "Ungeplante Taetigkeiten" den Button `Wartung` anklicken.
2. "Starten" bestaetigen.

**Erwartetes Verhalten:**
- Activity-Buchung wird angelegt.
- Toast "Wartung gestartet" erscheint.
- Terminal zeigt aktuelle Buchung als "Wartung" mit Timer.

---

### TS-8.8 — Cockpit aufrufen (Schichtleiter)

**Vorbedingungen:**
- AppSetting `BdeAktiv = true`.
- Mindestens eine aktive Buchung vorhanden.
- Benutzer hat Rolle `bde_shiftlead` oder `admin`.

**Schritte:**
1. Menue "BDE → Cockpit" oeffnen.

**Erwartetes Verhalten:**
- Kacheln pro BDE-aktiver Werkbank werden angezeigt.
- Jede Kachel zeigt: Werkbank-Name, Operator-Name, Status, Buchungstyp, Timer.
- Cockpit aktualisiert sich alle 5 Sekunden automatisch.

---

### TS-8.9 — Buchungsuebersicht anzeigen und filtern

**Vorbedingungen:**
- Benutzer hat Rolle `bde_shiftlead` oder `admin`.
- Buchungen fuer den heutigen Tag vorhanden.

**Schritte:**
1. Menue "BDE → Buchungsuebersicht" oeffnen.

**Erwartetes Verhalten:**
- Standard-Filter zeigt Buchungen des heutigen Tages.
- Spalten: Datum (KW), Operator, Werkbank, Typ, FA/AG, Start, Ende, Dauer.
- Spaltenfilter vorhanden (filterable-table).

**Schritte zum Filtern:**
1. Im Spaltenfilter "Operator" den Wert `Maier` eingeben.

**Erwartetes Verhalten:**
- Nur Buchungen von Operatoren deren Name "Maier" enthaelt werden angezeigt.

---

### TS-8.10 — Admin-Buchungskorrektur

**Vorbedingungen:**
- Benutzer hat Rolle `bde_admin` oder `admin`.
- Buchung `B-001` existiert mit falscher Menge.

**Schritte:**
1. Buchungsuebersicht oeffnen.
2. Buchung `B-001` per Bearbeiten-Button oeffnen.
3. Gutmenge von `5` auf `8` korrigieren.
4. Speichern.

**Erwartetes Verhalten:**
- Buchung wird mit korrigierter Menge gespeichert.
- Audit-Felder (ModifiedAt, ModifiedBy) werden aktualisiert.

---

### TS-8.11 — Admin-Buchungsstorno mit Grund

**Vorbedingungen:**
- Benutzer hat Rolle `bde_admin` oder `admin`.
- Buchung `B-002` soll storniert werden.

**Schritte:**
1. Buchungsuebersicht oeffnen.
2. Buchung `B-002` per Storno-Button markieren.
3. Storno-Grund eingeben: "Fehlbuchung durch Scan-Fehler".
4. Bestaetigen.

**Erwartetes Verhalten:**
- Buchung wird mit `IsCancelled = true` und `CancellationReason` gespeichert.
- In der Buchungsliste ist die Buchung als storniert markiert (ausgegraut oder Badge).
- Stornierte Buchung wird in Cockpit und Terminal-Ansicht nicht angezeigt.

---

### TS-8.12 — Vergessene Buchung manuell schliessen (Admin)

**Vorbedingungen:**
- Buchung `B-003` ist noch offen (Running), Operator ist schon abgemeldet.
- Benutzer hat Rolle `bde_admin` oder `admin`.

**Schritte:**
1. Buchungsuebersicht oeffnen.
2. Offene Buchung `B-003` finden.
3. "Manuell schliessen" Button anklicken.
4. Abschluss-Zeit bestaetigen.

**Erwartetes Verhalten:**
- Buchung wird als "Finished" geschlossen.
- Cockpit zeigt Werkbank als frei.

---

### TS-8.13 — Tageshistorie des Operators im Terminal anzeigen

**Vorbedingungen:**
- Operator `P-100` hat heute bereits 3 Buchungen abgeschlossen.

**Schritte:**
1. BDE-Terminal oeffnen.
2. Operator `P-100` scannen.

**Erwartetes Verhalten:**
- Unterhalb der aktuellen Buchung erscheint eine Tageshistorie mit den 3 abgeschlossenen Buchungen (FA/AG, Start, Ende, Dauer).

---

### TS-8.14 — BDE NurFA-Modus: FA direkt starten

**Vorbedingungen:**
- AppSetting `BdeAktiv = true`, `BdeNurFaMeldung = true`, `BdeDefaultArbeitsgang = PRODUKTION`.
- Werkbank `WB-01` hat BdeAktiv = true.

**Schritte:**
1. BDE-Terminal oeffnen.
2. Operator `P-100` scannen.
3. In der Auswahl erscheint FA-Liste (statt AG-Buttons).
4. FA `FA-2604001` anklicken.
5. "Starten" bestaetigen.

**Erwartetes Verhalten:**
- System findet oder erstellt automatisch Arbeitsgang "PRODUKTION" fuer FA `FA-2604001`.
- Production-Buchung wird auf diesem Auto-Arbeitsgang gestartet.
- Kein Setup-Modus verfuegbar (direkt Produktion).
- Keine ungeplanten Taetigkeiten sichtbar.

---

### TS-8.15 — BDE-Zugriff fuer bde_user: nur Terminal sichtbar

**Vorbedingungen:**
- Benutzer `werker1` hat nur Rolle `bde_user`.
- AppSetting `BdeAktiv = true`.

**Schritte:**
1. Als `werker1` einloggen.
2. BDE-Menue anschauen.

**Erwartetes Verhalten:**
- "BDE → Terminal" sichtbar.
- "BDE → Cockpit" ist NICHT sichtbar (nur Schichtleiter/Admin).
- "BDE → Buchungsuebersicht" ist NICHT sichtbar.

---

## 9. BDE Phase 2.1 — Werkbank-Erweiterungen

### TS-9.1 — Werkbank BDE-Flag aktivieren und im Cockpit pruefen

**Vorbedingungen:**
- Werkbank `WB-02` hat `BdeAktiv = false`.
- AppSetting `BdeAktiv = true`.

**Schritte:**
1. Stammdaten → Werkbaenke → `WB-02` bearbeiten.
2. Toggle "BDE aktiv" im Block "BDE-Einstellungen" aktivieren.
3. Speichern.
4. BDE → Cockpit oeffnen.

**Erwartetes Verhalten:**
- `WB-02` erscheint jetzt als Kachel im Cockpit.
- In der Werkbank-Liste zeigt Spalte "BDE" gruenes Badge "Aktiv".

---

### TS-9.2 — Werkbank-Inaktiv: Buchung blockiert

**Vorbedingungen:**
- Werkbank `WB-INAKTIV` hat `BdeAktiv = false`.
- Terminal ist konfiguriert mit Default-Werkbank `WB-INAKTIV`.

**Schritte:**
1. BDE-Terminal fuer `WB-INAKTIV` oeffnen.
2. Operator scannen und Arbeitsgang starten.

**Erwartetes Verhalten:**
- Fehlermeldung "Werkbank ist nicht fuer BDE aktiviert." erscheint.
- Buchung wird NICHT angelegt.

---

### TS-9.3 — Werkbank-Default-Arbeitsgang ueberschreibt globales Setting

**Vorbedingungen:**
- Globales AppSetting `BdeDefaultArbeitsgang = GLOBAL-AG`.
- Werkbank `WB-01` hat `BdeDefaultArbeitsgang = WB01-SPECIAL`.
- AppSetting `BdeNurFaMeldung = true`.

**Schritte:**
1. BDE-Terminal fuer Werkbank `WB-01` oeffnen.
2. FA `FA-2604001` auswaehlen und starten.

**Erwartetes Verhalten:**
- Auto-erstellter Arbeitsgang heisst `WB01-SPECIAL` (nicht `GLOBAL-AG`).
- Buchung laeuft auf diesem werkbank-spezifischen AG.

---

### TS-9.4 — Werkbank BdeAktiv-Spalte in der Liste

**Vorbedingungen:**
- Mindestens eine Werkbank mit `BdeAktiv = true` und eine mit `false`.

**Schritte:**
1. Stammdaten → Werkbaenke oeffnen.

**Erwartetes Verhalten:**
- Spalte "BDE" vorhanden.
- Aktive Werkbank: gruenes Badge "Aktiv".
- Inaktive Werkbank: graues Badge "Inaktiv".

---

### TS-9.5 — Neues Terminal: Nur BDE-aktive Werkbaenke im Dropdown

**Vorbedingungen:**
- Werkbank `WB-AKTIV` hat `BdeAktiv = true`.
- Werkbank `WB-INAKTIV` hat `BdeAktiv = false`.

**Schritte:**
1. BDE → Stammdaten → Terminals → "Neues Terminal".
2. Werkbank-Dropdown oeffnen.

**Erwartetes Verhalten:**
- `WB-AKTIV` erscheint im Dropdown.
- `WB-INAKTIV` erscheint NICHT im Dropdown.

---

## 10. BDE Phase 2.2 — Mehrfachanmeldung + Zeit-Split

### TS-10.1 — Multi-MA-Buchung auf 1 Arbeitsgang

**Vorbedingungen:**
- AppSetting `BdeAktiv = true`, `BdeMehrfachBuchungProArbeitsgang = true`.
- Werkbank `WB-01` hat BdeAktiv = true.
- Operator `P-100` (Hans Maier) und Operator `P-200` (Anna Schmidt) existieren.
- Offener Arbeitsgang `FA-2604001 / AG 10` ist an Werkbank `WB-01`.

**Schritte:**
1. Operator `P-100` am Terminal anmelden.
2. `FA-2604001 / AG 10` → "Produktion starten".
3. Operator `P-200` am Terminal anmelden (selbes Terminal oder anderes Terminal).
4. Denselben Arbeitsgang `FA-2604001 / AG 10` auswaehlen → "Produktion starten".

**Erwartetes Verhalten:**
- Beide Buchungen laufen parallel (keine Fehlermeldung "AG bereits belegt").
- Cockpit zeigt beide Operatoren fuer Werkbank `WB-01` (falls selbe Werkbank).
- Buchungsuebersicht zeigt beide parallelen Buchungen auf demselben AG.

**Negativfall (BdeMehrfachBuchungProArbeitsgang = false):**
- Zweiter Operator bekommt Fehlermeldung "Arbeitsgang bereits belegt durch Hans Maier."

---

### TS-10.2 — Multi-FA-Buchung pro Mitarbeiter

**Vorbedingungen:**
- AppSetting `BdeMehrfachBuchungProOperator = true`.
- Operator `P-100` ist aktiv auf `FA-2604001 / AG 10`.
- Zweiter offener Arbeitsgang `FA-2604002 / AG 20` vorhanden.

**Schritte:**
1. Operator `P-100` ist eingeloggt, hat laufende Buchung auf `FA-2604001 / AG 10`.
2. Auf `FA-2604002 / AG 20` klicken → "Produktion starten".

**Erwartetes Verhalten:**
- Keine Fehlermeldung.
- Beide Buchungen laufen parallel fuer Operator `P-100`.
- Terminal zeigt beide aktiven Buchungen.

**Negativfall (BdeMehrfachBuchungProOperator = false):**
- System antwortet "Sie haben noch eine laufende Buchung. Bitte zuerst beenden."

---

### TS-10.3 — Gruppen-Abschluss-Dialog bei Mehrfachstart

**Vorbedingungen:**
- AppSetting `BdeMehrfachBuchungProOperator = true`, `BdeGleichzeitigerAbschlussBeiMehrfachStart = true`.
- Operator `P-100` hat 2 laufende Production-Buchungen auf verschiedenen AGs.

**Schritte:**
1. Operator `P-100` klickt "Fertig" auf einer seiner Buchungen.
2. Mengen-Dialog erscheint (Gut- und Ausschussmenge eingeben).
3. "Abschluss-Meldung" bestaetigen.

**Erwartetes Verhalten:**
- Gruppen-Abschluss-Dialog erscheint mit Tabelle beider aktiver Buchungen.
- Pro Buchung: FA/AG, Sollmenge, Felder fuer Gut-/Ausschussmenge, Abschluss-Checkbox.
- Nach Klick auf "Alle markierten beenden": beide Buchungen werden atomar mit gemeinsamem EndedAt-Zeitstempel geschlossen.

---

### TS-10.4 — Effektive-Zeit-Split bei parallelen Buchungen (3-Segmente-Beispiel)

**Vorbedingungen:**
- AppSetting `BdeMehrfachBuchungProArbeitsgang = true`.
- Buchung A: `P-100` auf `FA-2604001 / AG 10`, Start 08:00, Ende 10:00.
- Buchung B: `P-200` auf `FA-2604001 / AG 10`, Start 09:00, Ende 11:00.
- Buchungen sind abgeschlossen.

**Schritte:**
1. Buchungsuebersicht oeffnen.
2. Spalte "Effektive Zeit" fuer Buchung A und B anschauen.

**Erwartetes Verhalten:**
- Segment 08:00-09:00 (1h): nur P-100 aktiv → P-100 erhaelt volle 60 min.
- Segment 09:00-10:00 (1h): P-100 und P-200 gleichzeitig → je 30 min (oder nach Gutmenge-Gewichtung).
- Segment 10:00-11:00 (1h): nur P-200 aktiv → P-200 erhaelt volle 60 min.
- Buchung A effektiv: ca. 90 min, Buchung B effektiv: ca. 90 min (statt 120 + 120 = Doppelzaehlung).

---

### TS-10.5 — Paused-Hint nach Operator-Scan

**Vorbedingungen:**
- Operator `P-100` hat eine pausierte Buchung auf `FA-2604001 / AG 10`.

**Schritte:**
1. BDE-Terminal oeffnen.
2. Operator `P-100` scannen.

**Erwartetes Verhalten:**
- Prominenter Hinweis-Block erscheint UEBER den AG-Auswahl-Buttons.
- Block zeigt: "Sie haben pausierte Auftraege: FA-2604001 / AG 10 Fraesen — pausiert seit 09:42".
- "Fortsetzen"-Button neben dem Hinweis.

---

### TS-10.6 — Resume eines pausierten Arbeitsgangs

**Vorbedingungen:**
- Operator `P-100` hat pausierte Buchung auf `FA-2604001 / AG 10` (pausiert seit 09:42).

**Schritte:**
1. Operator `P-100` scannen.
2. Im Hinweis-Block "Fortsetzen" fuer `FA-2604001 / AG 10` anklicken.

**Erwartetes Verhalten:**
- Neue Buchung wird angelegt mit `ParentBookingId` = pausierte Buchung.
- Status der alten Buchung bleibt "Paused" (EndedAt gesetzt).
- Neue Buchung hat Status "Running" (Resumed).
- Toast "Buchung fortgesetzt" erscheint.
- Timer laeuft ab dem Resume-Zeitpunkt.

---

### TS-10.7 — Sollmenge-Anzeige im Mengen-Dialog

**Vorbedingungen:**
- FA `FA-2604001` hat Sollmenge 100 Stueck.
- Operator hat laufende Production-Buchung auf diesem FA.

**Schritte:**
1. "Beenden" Button klicken → Mengen-Dialog erscheint.

**Erwartetes Verhalten:**
- Dialog zeigt "Sollmenge: 100 Stk." als Hinweis.
- Felder fuer Gutmenge und Ausschuss sind editierbar.

---

### TS-10.8 — Under-Sollmenge-Bestaetigung bei Fertig-Meldung

**Vorbedingungen:**
- FA `FA-2604001` Sollmenge = 100.
- Operator meldet Gutmenge = 50 (unter Sollmenge) als Abschluss.

**Schritte:**
1. "Beenden" → Mengen-Dialog.
2. Gutmenge `50` eingeben, "Abschluss-Meldung" aktiv.
3. Bestaetigen.

**Erwartetes Verhalten:**
- Bestaetigung-Dialog erscheint: "Gutmenge (50) liegt unter Sollmenge (100). Trotzdem abschliessen?"
- Nach Bestaetigung: Buchung wird abgeschlossen.
- Ohne Bestaetigung: Dialog schliesst sich, Buchung laeuft weiter.

---

### TS-10.9 — Close-Others-Dialog bei Multi-MA-Abschluss

**Vorbedingungen:**
- AppSetting `BdeMehrfachBuchungProArbeitsgang = true`.
- Operator `P-100` und `P-200` haben beide laufende Buchungen auf `FA-2604001 / AG 10`.

**Schritte:**
1. Operator `P-100` klickt "Beenden" auf seiner Buchung.
2. Mengen-Dialog: Gutmenge `20`, Abschluss-Meldung aktiv.
3. Bestaetigen.

**Erwartetes Verhalten:**
- Buchung von `P-100` wird abgeschlossen.
- Sekundaerer Dialog erscheint: "Operator P-200 (Anna Schmidt) hat noch eine aktive Buchung auf diesem AG. Auch beenden?"
- Klick "Ja, alle beenden": `P-200`s Buchung wird ebenfalls mit EndedAt = jetzt geschlossen.
- Klick "Nein, nur meine": `P-200`s Buchung laeuft weiter, nur `P-100`s ist abgeschlossen.

---

### TS-10.10 — Filter: Fertig-gemeldete FAs aus Terminal-Auswahl ausblenden

**Vorbedingungen:**
- FA `FA-2604001` hat alle AGs als "Finished" gebucht.
- FA `FA-2604002` hat noch offene AGs.

**Schritte:**
1. BDE-Terminal oeffnen.
2. Operator scannen.
3. AG-Auswahl anschauen.

**Erwartetes Verhalten:**
- `FA-2604001` erscheint NICHT in der AG-Auswahl (alle AGs fertig).
- `FA-2604002` erscheint weiterhin.

---

### TS-10.11 — Eigene Doppelbuchung auf gleichem AG verhindert

**Vorbedingungen:**
- AppSetting `BdeMehrfachBuchungProOperator = true`.
- Operator `P-100` hat bereits laufende Buchung auf `FA-2604001 / AG 10`.

**Schritte:**
1. Operator `P-100` versucht denselben AG `FA-2604001 / AG 10` nochmals zu starten.

**Erwartetes Verhalten:**
- Fehlermeldung "Sie haben bereits eine aktive Buchung auf diesem Arbeitsgang."
- Keine zweite Buchung wird angelegt.

---

### TS-10.12 — Setup bleibt single-operator-single-AG

**Vorbedingungen:**
- AppSetting `BdeMehrfachBuchungProArbeitsgang = true`.
- Operator `P-100` rueste-bucht bereits auf `FA-2604001 / AG 10`.

**Schritte:**
1. Operator `P-200` versucht, ebenfalls `FA-2604001 / AG 10` zu ruesteten.

**Erwartetes Verhalten:**
- Fehlermeldung "Arbeitsgang wird bereits geruestet." (Kollision, auch wenn ProArbeitsgang = true).
- Setup ist IMMER exklusiv, unabhaengig von Settings.

---

### TS-10.13 — Activity bleibt single-active pro Operator

**Vorbedingungen:**
- AppSetting `BdeMehrfachBuchungProOperator = true`.
- Operator `P-100` hat laufende Activity-Buchung (Wartung).

**Schritte:**
1. Operator `P-100` versucht, eine zweite Aktivitaet (Reinigung) zu starten.

**Erwartetes Verhalten:**
- Erste Activity (Wartung) wird automatisch beendet.
- Neue Activity (Reinigung) wird gestartet.
- Kein Fehler — aber kein paralleles Activity-Booking.

---

### TS-10.14 — BdeAktiv=false blockiert Buchung mit Fehlermeldung

**Vorbedingungen:**
- Werkbank `WB-INAKTIV` hat `BdeAktiv = false`.

**Schritte:**
1. BDE-Terminal fuer `WB-INAKTIV` aufrufen.
2. Operator scannen und Buchung starten.

**Erwartetes Verhalten:**
- Fehlermeldung "Werkbank ist nicht fuer BDE aktiviert."
- Buchung wird abgewiesen.

---

### TS-10.15 — Pausierte Buchungen erscheinen nicht doppelt nach Resume

**Vorbedingungen:**
- Operator `P-100` hat Buchung pausiert und dann fortgesetzt.
- Buchung-Status: pausierte Buchung = Paused, neue Buchung = Running (via Resume).

**Schritte:**
1. BDE-Terminal oeffnen, Operator `P-100` scannen.
2. Paused-Hinweis-Block anschauen.
3. Buchungsuebersicht oeffnen und heutige Buchungen anschauen.

**Erwartetes Verhalten:**
- Im Hinweis-Block erscheint nur die AKTIVE pausierte Buchung (nicht die bereits resumed).
- In der Buchungsuebersicht sind beide Buchungen sichtbar (die pausierte und die resumed).
- Keine Doppelanzeige im Terminal.

---

## 11. Bestellungen / Bedarfsmeldungen

### TS-11.1 — Bedarfsmeldung aus Stueckliste anlegen

**Vorbedingungen:**
- AppSetting `BestellungenAktiv = true`.
- Empfaengergruppe `Einkauf` fuer Artikelgruppe des Bauteils konfiguriert.
- Benutzer hat Rolle `picking` oder `stock` oder `admin`.

**Schritte:**
1. Stueckliste eines FA oeffnen.
2. Bei Bauteil mit Bestand 0 "Bestellen" anklicken.
3. Im Modal: Menge `5`, Prioritaet `Dringend`, Anmerkung `Sofort benoetigt`.
4. Empfaengergruppe `Einkauf` auswaehlen.
5. Absenden.

**Erwartetes Verhalten:**
- Bedarfsmeldung Status `Offen` wird angelegt.
- E-Mail an aktive Empfaenger von `Einkauf` gesendet.
- Toast "Bedarfsmeldung gesendet" erscheint.

---

### TS-11.2 — Bestelluebersicht anzeigen und filtern

**Vorbedingungen:**
- AppSetting `BestellungenAktiv = true`.
- Mindestens 3 Bedarfsmeldungen vorhanden.

**Schritte:**
1. Menue "Bestellungen" oeffnen.
2. Filter auf "Offen" setzen.

**Erwartetes Verhalten:**
- Nur offene Bedarfsmeldungen werden angezeigt.
- Status-Badge `Offen` sichtbar.
- Spalten: FA, Artikel, Menge, Prioritaet, Datum, Empfaengergruppe.

---

### TS-11.3 — Bedarfsmeldung stornieren

**Vorbedingungen:**
- AppSetting `BestellungenAktiv = true`.
- Bedarfsmeldung `BD-001` hat Status `Offen`.

**Schritte:**
1. Bestelluebersicht oeffnen.
2. Bedarfsmeldung `BD-001` anklicken.
3. "Stornieren" Button klicken.
4. Bestaetigen.

**Erwartetes Verhalten:**
- Status wechselt auf `Storniert`.
- Datum und Benutzer der Stornierung werden gespeichert.

---

### TS-11.4 — Bedarfsmeldung als erfuellt markieren (ueber Wareneingang)

**Vorbedingungen:**
- Offene Bedarfsmeldung fuer Artikel `100-003`.

**Schritte:**
1. Lager → Einbuchung fuer Artikel `100-003` starten.
2. Im Hinweis-Block offene Bedarfsmeldung anzeigen lassen.
3. Checkbox der Bedarfsmeldung anhaaken.
4. Einbuchen.

**Erwartetes Verhalten:**
- Einbuchung gespeichert.
- Bedarfsmeldung Status wechselt auf `Erfuellt`.
- `FulfilledAt` und `FulfilledByStockMovementId` gesetzt.

---

### TS-11.5 — Empfaengergruppe Artikelgruppen-Zuordnung

**Vorbedingungen:**
- Empfaengergruppe `Einkauf` existiert.
- Artikelgruppe `940` (Kleinmaterial) existiert.

**Schritte:**
1. Stammdaten → Empfaengergruppen → `Einkauf` bearbeiten.
2. Artikelgruppe `940` zuordnen.
3. Speichern.

**Erwartetes Verhalten:**
- Zuordnung gespeichert.
- Bei Bedarfsmeldungen fuer Artikel aus Gruppe `940` wird `Einkauf` automatisch vorgeschlagen.

---

### TS-11.6 — Sammelbestellung mehrerer Fehlteile

**Vorbedingungen:**
- AppSetting `BestellungenAktiv = true`.
- 3 Fehlteile in Stueckliste, alle aus Gruppe `940 — Kleinmaterial`.

**Schritte:**
1. Stueckliste oeffnen, Fehlteile-Filter aktiv.
2. Checkboxen der 3 Fehlteile anhaaken.
3. "Sammelbestellung" klicken.
4. Empfaengergruppe bestaetigen.
5. Absenden.

**Erwartetes Verhalten:**
- 3 separate Bedarfsmeldungen werden angelegt.
- Alle 3 werden in einer E-Mail an den Empfaenger gebundelt.
- Toast "3 Bedarfsmeldungen gesendet" erscheint.

---

### TS-11.7 — BestellungenAktiv = false: Bestell-Buttons ausgeblendet

**Vorbedingungen:**
- AppSetting `BestellungenAktiv = false`.

**Schritte:**
1. Stueckliste eines FA oeffnen.

**Erwartetes Verhalten:**
- "Bestellen"-Button und "Sammelbestellung"-Button sind NICHT sichtbar.
- Menuepunkt "Bestellungen" ist NICHT sichtbar.

---

## 12. Print + OSEON-Tracking-Verbesserungen

### TS-12.1 — Druckender User erscheint in BOM-Druck

**Vorbedingungen:**
- Benutzer `Max Picker` ist eingeloggt.
- Stueckliste eines FA geoeffnet.

**Schritte:**
1. "BOM drucken" Button anklicken.

**Erwartetes Verhalten:**
- Druckansicht zeigt unterhalb der FA-Nummer:
  `Gedruckt von: Max Picker`
- Schrift kleiner (ca. 12px), grau.

---

### TS-12.2 — Druckender User erscheint in Picking-Liste-Druck

**Vorbedingungen:**
- Benutzer `Max Picker` ist eingeloggt.
- Stueckliste eines FA geoeffnet.

**Schritte:**
1. "Picking-Liste drucken" Button anklicken.

**Erwartetes Verhalten:**
- Druckansicht zeigt "Gedruckt von: Max Picker" unterhalb der FA-Nummer.

---

### TS-12.3 — BOM-Druck uebernimmt ausgeblendete Spalten

**Vorbedingungen:**
- Stueckliste geoeffnet.
- Spalte "Kategorie" ist ausgeblendet (Zahnrad → Kategorie deaktiviert).

**Schritte:**
1. "BOM drucken" klicken.

**Erwartetes Verhalten:**
- Druckansicht hat KEINE "Kategorie"-Spalte.
- Nur die sichtbaren Spalten werden gedruckt.

---

### TS-12.4 — OSEON-Tracking: Artikelnummern-Suche (manuell)

**Vorbedingungen:**
- AppSetting `TeileverfolgungAktiv = true`.
- Sub-Auftrag mit Artikel `100-001` vorhanden.

**Schritte:**
1. OSEON-Teileverfolgung oeffnen.
2. Im Suchfeld "Artikelnummer" den Wert `100-001` eingeben.

**Erwartetes Verhalten:**
- Nur Gruppen/Sub-Auftraege mit Artikel `100-001` werden angezeigt.
- Filterung ist client-seitig (kein Seiten-Reload).

---

### TS-12.5 — OSEON-Tracking: Artikelnummern-Suche per QR-Scan

**Vorbedingungen:**
- OSEON-Tracking geoeffnet.
- Artikel `100-001` hat QR-Code im Format `100-001;Bezeichnung;FA-12345`.

**Schritte:**
1. Scan-Button neben dem Artikelnummer-Suchfeld anklicken.
2. QR-Code scannen.

**Erwartetes Verhalten:**
- Artikelnummer `100-001` (Komma-Suffix wird entfernt) wird ins Suchfeld eingetragen.
- Filter wird sofort angewendet.

---

### TS-12.6 — OSEON-Tracking: Lagerbestand-Modal oeffnen

**Vorbedingungen:**
- Sub-Auftrag `WA-12345` hat Artikel im Lager.

**Schritte:**
1. OSEON-Tracking oeffnen, Sub-Auftrag `WA-12345` sichtbar machen.
2. Lager-Icon neben der WA-Nummer anklicken.

**Erwartetes Verhalten:**
- Modal oeffnet sich mit Tabelle: Artikelnummer, Bezeichnung, Lagerplatz, Menge.
- Link "Details in Bestandsuebersicht →" oeffnet `/StockOverview?filterProductionOrder=WA-12345` in neuem Tab.

---

## 13. Spalten-Konfiguration & Filter

### TS-13.1 — Spalten-Konfiguration oeffnen und Spalte ausblenden

**Vorbedingungen:**
- Benutzer ist eingeloggt.
- FA-Liste ist geoeffnet.

**Schritte:**
1. Zahnrad-Icon oben rechts ueber der FA-Tabelle anklicken.
2. Im Einstellungs-Panel Checkbox "Glas" deaktivieren.
3. Panel schliessen.

**Erwartetes Verhalten:**
- Spalte "Glas" ist in der Tabelle nicht mehr sichtbar.
- Filterkopf dieser Spalte fehlt ebenfalls.

---

### TS-13.2 — Spalten-Konfiguration per Rechtsklick oeffnen

**Vorbedingungen:**
- FA-Liste geoeffnet.

**Schritte:**
1. Rechtsklick auf den Spaltenkopf "FA-Nummer".

**Erwartetes Verhalten:**
- Spalten-Konfiguration-Panel oeffnet sich direkt (Schnellzugriff).

---

### TS-13.3 — Spalten-Reihenfolge per Drag & Drop aendern

**Vorbedingungen:**
- FA-Liste geoeffnet.
- Spalten-Konfiguration geoeffnet.

**Schritte:**
1. Im Einstellungs-Panel Spalte "Kunde" per Drag & Drop vor "FA-Nummer" ziehen.
2. Panel schliessen.

**Erwartetes Verhalten:**
- Spalte "Kunde" erscheint jetzt vor "FA-Nummer" in der Tabelle.
- Reihenfolge wird beibehalten nach Seiten-Reload.

---

### TS-13.4 — Spalten-Einstellungen werden pro Benutzer gespeichert

**Vorbedingungen:**
- Benutzer `testuser1` blendet Spalte "Glas" aus.

**Schritte:**
1. Als `testuser1` einloggen.
2. In FA-Liste Spalte "Glas" ausblenden.
3. Ausloggen.
4. Als `testuser2` einloggen.
5. FA-Liste oeffnen.

**Erwartetes Verhalten:**
- `testuser2` sieht Spalte "Glas" (seine eigenen Einstellungen, nicht die von `testuser1`).
- Nach erneutem Login von `testuser1`: Spalte "Glas" immer noch ausgeblendet.

---

### TS-13.5 — Standard-Sortierung festlegen

**Vorbedingungen:**
- FA-Liste geoeffnet.

**Schritte:**
1. Spalten-Konfiguration oeffnen.
2. Spalte "Fertigungstermin" als Standard-Sortierung auswaehlen (aufsteigend).
3. Seite neu laden.

**Erwartetes Verhalten:**
- Tabelle wird nach Fertigungstermin aufsteigend sortiert (ohne manuellen Klick).

---

### TS-13.6 — "Auf Standard zuruecksetzen" im Einstellungs-Panel

**Vorbedingungen:**
- Benutzer hat individuelle Spalten-Einstellungen gespeichert (Spalten ausgeblendet, Reihenfolge geaendert).

**Schritte:**
1. Spalten-Konfiguration oeffnen.
2. Button "Auf Standard zuruecksetzen" anklicken.

**Erwartetes Verhalten:**
- Alle Spalten wieder sichtbar in Originalreihenfolge.
- Keine gespeicherten Einstellungen mehr vorhanden.

---

### TS-13.7 — Kalender-Datepicker fuer Datumsspalten

**Vorbedingungen:**
- FA-Liste geoeffnet.
- FAs mit verschiedenen Fertigungsterminen vorhanden.

**Schritte:**
1. Im Spaltenfilter der Datumsspalte "Fertigungstermin" das Kalender-Icon anklicken.
2. Im Monatskalender auf KW-Nummer 16 klicken.

**Erwartetes Verhalten:**
- Nur FAs mit Fertigungstermin in KW16 werden angezeigt.
- Alternativ: Manuell `KW16` eingeben erzielt dasselbe Ergebnis.

---

## 14. Service / Sync (read-only Verifikation)

### TS-14.1 — Produktionsauftraege werden aus SAGE importiert

**Vorbedingungen:**
- Service laeuft und `Sync:ProductionOrdersEnabled = true`.
- Neuer Produktionsauftrag `FA-NEU-001` in SAGE-View `vw_AKE_Kommissionierung_WAListe` vorhanden.

**Schritte:**
1. Auf den naechsten Service-Sync-Zyklus warten (max. 15 Minuten).
2. FA-Liste in WMS aufrufen.

**Erwartetes Verhalten:**
- FA `FA-NEU-001` erscheint in der WMS FA-Liste.

---

### TS-14.2 — OSEON-Tracking-Sync importiert Auftraege

**Vorbedingungen:**
- Service laeuft und `Sync:OseonTrackingEnabled = true`.
- Neuer OSEON-Auftrag vorhanden.

**Schritte:**
1. Sync-Zyklus abwarten.
2. OSEON-Teileverfolgung aufrufen.

**Erwartetes Verhalten:**
- Neuer Auftrag erscheint in der OSEON-Liste.
- Ampel und Soll-Termine korrekt berechnet.

---

### TS-14.3 — BOM-Cache wird befuellt (Service)

**Vorbedingungen:**
- Service laeuft und `Sync:BomCacheEnabled = true`.
- Offene FAs mit Stuecklisten vorhanden.

**Schritte:**
1. Service-Sync abwarten.
2. Stueckliste eines FA aufrufen.

**Erwartetes Verhalten:**
- Stueckliste laedt sichtbar schneller (aus Cache, nicht direkt aus SAGE/OSEON).
- Datenquelle-Anzeige zeigt "Cache".

---

### TS-14.4 — Service-Einstellungen im UI anzeigen

**Vorbedingungen:**
- Benutzer hat Rolle `masterdata` oder `admin`.

**Schritte:**
1. Stammdaten → Service-Einstellungen oeffnen.

**Erwartetes Verhalten:**
- Aktuelle Service-Konfiguration wird angezeigt (Sync-Intervall, aktivierte Syncs).
- Einstellungen koennen geaendert und gespeichert werden.

---

## 15. BDE Phase 2.3 — Schichtkalender + Auto-Pause

### TS-15.1 — Default-Schichtkalender anlegen
**Vorbedingungen:** Admin-Login, `BdeSchichtkalenderAktiv = true`.
**Schritte:**
1. Menue BDE -> Schichtkalender oeffnen.
2. Auf "Montag"-Karte "+ Schicht hinzufuegen" klicken.
3. Eingabe: Name "Frueh", Beginn 06:00, Ende 14:00, speichern.
**Erwartet:** Schicht erscheint in der Mo-Karte; Erfolgs-Toast.
**Negativ:** Beginn 14:00 + Ende 06:00 -> Validation-Hinweis "Ende muss nach dem Beginn liegen".

### TS-15.2 — Werkbank-Override aktivieren
**Vorbedingungen:** Default-Kalender vorhanden, Werkbank "A1" angelegt.
**Schritte:**
1. Stammdaten -> Werkbaenke -> A1 -> Bearbeiten.
2. Im "Schichtplan (BDE)"-Card Toggle "Eigener Schichtplan" einschalten.
3. "+ Schicht hinzufuegen", Wochentag/Beginn/Ende eingeben.
4. Speichern.
**Erwartet:** Werkbank zeigt eigenen Plan; Default wird ignoriert fuer A1.

### TS-15.3 — Auto-Pause greift am Schichtende
**Vorbedingungen:** Default Mo-Fr 06-14, Master-Toggle aktiv, MA scannt um 13:50 ein.
**Schritte:**
1. Buchung startet 13:50.
2. Worker tickt nach 14:00 (max. 1h Latenz).
**Erwartet:** Buchung Status=AutoPaused, EndedAt=14:00. ModifiedBy="BDE-AutoPause".

### TS-15.4 — Resume nach Auto-Pause
**Vorbedingungen:** TS-15.3 ausgefuehrt, MA scannt am Folge-Tag.
**Schritte:**
1. Operator-Scan im Terminal.
**Erwartet:** Paused-Hint zeigt "auto-pausiert seit ... (Schichtende)" mit Fortsetzen-Button.
2. Klick Fortsetzen.
**Erwartet:** Neue Running-Buchung mit ParentBookingId; Parent Status=Resumed.

### TS-15.5 — Feiertag schuetzt vor Auto-Pause
**Vorbedingungen:** Holiday-Eintrag fuer heutigen Tag, Master-Toggle aktiv.
**Schritte:** Buchung laeuft, Worker tickt.
**Erwartet:** Buchung wird NICHT pausiert (Holiday gilt als arbeitsfrei).

### TS-15.6 — Master-Toggle aus
**Vorbedingungen:** `BdeSchichtkalenderAktiv = false`.
**Schritte:** Buchung ueber Schichtende laufen lassen, Worker tickt.
**Erwartet:** Buchung bleibt unveraendert. Phase-2.2-Verhalten.

### TS-15.7 — Buchung startet vor Schichtbeginn
**Vorbedingungen:** Frueh 06-14, MA stempelt um 04:00 ein.
**Schritte:** Worker tickt nach 14:00.
**Erwartet:** Buchung pausiert mit EndedAt=14:00 (P2-Logik).

### TS-15.8 — Buchung startet nach allen Tagesschichten
**Vorbedingungen:** Frueh 06-14, MA stempelt um 23:00 ein.
**Schritte:** Worker tickt naechsten Tag.
**Erwartet:** Buchung NICHT pausiert (kein Schichtende mehr fuer den Tag der StartedAt).

### TS-15.9 — Werkbank Override leer = 24/7 frei
**Vorbedingungen:** Werkbank-Toggle EIN, 0 eigene Schichten konfiguriert.
**Schritte:** Buchung laeuft, Worker tickt.
**Erwartet:** Keine Auto-Pause (Override-Schalter zieht).

### TS-15.10 — Mehrschicht-Uebergang (Frueh -> Spaet)
**Vorbedingungen:** Default Mo Frueh 06-14 + Spaet 14-22.
**Schritte:** MA1 startet 13:50, Worker tickt nach 14:00.
**Erwartet:** MA1-Buchung pausiert mit EndedAt=14:00. MA2 (Spaet) muss neu scannen + Resume.

### TS-15.11 — Feiertags-Sync (Nager.Date)
**Vorbedingungen:** Service-Settings `FeiertagSyncEnabled=true`, CountryCode=AT, Region=AT-3, JahreVoraus=2.
**Schritte:** Service starten.
**Erwartet:** Holidays-Tabelle enthaelt nationale + AT-3-Eintraege fuer aktuelles + 2 Folgejahre. Source=NagerSync.

### TS-15.12 — Manuelle Holiday-Eintraege werden nicht ueberschrieben
**Vorbedingungen:** Manueller Holiday-Eintrag fuer 06.01., Sync-Eintrag fuer dasselbe Datum noch nicht vorhanden.
**Schritte:** Sync starten.
**Erwartet:** Manueller Eintrag bleibt. Source weiterhin Manual. Description unveraendert.

---

*Ende des Dokuments. Stand: v1.8.2 (2026-04-28)*
*Bei neuen Features: Szenarien in den entsprechenden Bereich einfuegen und TS-Nummern fortfuehren.*

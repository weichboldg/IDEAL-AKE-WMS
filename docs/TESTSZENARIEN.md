# Testszenarien — IDEAL-AKE WMS

**Stand:** 2026-05-22 (v1.14.0)

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
| 2. Lager | [→](#2-lager) | TS-2.1 – TS-2.21 (inkl. TS-2.12 – TS-2.21 FA-Lagerplatz-Hinweis) |
| 3. Stammdaten | [→](#3-stammdaten) | TS-3.1 – TS-3.16 |
| 4. Fertigungsauftraege | [→](#4-fertigungsauftraege) | TS-4.1 – TS-4.19 (inkl. TS-4.9a/b/c/d Bulk-Freigabe + Filter-Persistenz, TS-4.11 – TS-4.19 Baugruppen-Flags VK/VL/VE/VT/VA) |
| 5. Stueckliste (BOM) | [→](#5-stueckliste-bom) | TS-5.1 – TS-5.9 |
| 6. Kommissionierung / Picking | [→](#6-kommissionierung--picking) | TS-6.1 – TS-6.10 |
| 7. OSEON Teileverfolgung | [→](#7-oseon-teileverfolgung) | TS-7.1 – TS-7.10 (inkl. TS-7.6a/b/c/d Artikel-Filter + Sortierung) |
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

### TS-2.12 — FA neu (keine bestehende Buchung)

**Vorbedingungen:**
- FA-Nummer ohne aktuellen Bestand im WMS.

**Schritte:**
1. Einbuchung oeffnen.
2. Artikel auswaehlen.
3. FA-Nummer manuell eingeben → Tab.

**Erwartetes Verhalten:**
- Kein Info-Alert erscheint unter dem FA-Feld.
- Lagerplatz-Dropdown bleibt unveraendert.

---

### TS-2.13 — FA liegt auf genau 1 Lagerplatz, Storage-Dropdown leer

**Vorbedingungen:**
- FA-Nummer `12345` liegt auf `K-WAGEN-03` mit positiver Menge (5 Stk Artikel 87015207).
- Lagerplatz `K-WAGEN-03` ist aktiv und buchbar.

**Schritte:**
1. Einbuchung oeffnen.
2. Artikel auswaehlen (Lagerplatz NICHT setzen).
3. FA `12345` eingeben oder per QR scannen → Tab.

**Erwartetes Verhalten:**
- Lagerplatz-Dropdown wird automatisch auf `K-WAGEN-03` gesetzt.
- Blaues Info-Alert: "FA `12345` liegt bereits: Lagerplatz **K-WAGEN-03** — 5,000 Stk Artikel 87015207. → Lagerplatz wurde uebernommen."
- Submit-Button bleibt aktiv.

---

### TS-2.14 — FA mit 1 Lagerplatz, Storage bereits gewaehlt → kein Overwrite

**Vorbedingungen:**
- Wie TS-2.13.

**Schritte:**
1. Einbuchung oeffnen.
2. Artikel auswaehlen.
3. Lagerplatz `K-LAGER-05` manuell auswaehlen.
4. FA `12345` eingeben → Tab.

**Erwartetes Verhalten:**
- Lagerplatz bleibt `K-LAGER-05` (kein Overwrite).
- Blaues Info-Alert: Liste mit `K-WAGEN-03` + Hinweis "→ Deine Auswahl **K-LAGER-05** bleibt."

---

### TS-2.15 — FA auf mehreren Lagerplaetzen → kein Auto-Fill

**Vorbedingungen:**
- FA `12345` liegt auf `K-WAGEN-03` (5 Stk) und `K-LAGER-05` (3 Stk).

**Schritte:**
1. Einbuchung oeffnen.
2. FA `12345` eingeben → Tab (Lagerplatz NICHT gewaehlt).

**Erwartetes Verhalten:**
- Lagerplatz-Dropdown bleibt leer.
- Gelbes Warning-Alert: "FA `12345` liegt auf mehreren Lagerplaetzen: K-WAGEN-03 — 5,000 Stk; K-LAGER-05 — 3,000 Stk. → Bitte gezielt buchen."

---

### TS-2.16 — QR-Scan triggert Hint

**Vorbedingungen:**
- AppSetting `QrMitFaNummer = true`.
- QR-Code mit FA-Anteil generierbar (Artikelnummer;...;FA-Nummer[,Suffix];...).
- FA aus QR liegt bereits auf genau 1 Platz.

**Schritte:**
1. Einbuchung oeffnen.
2. Artikel-QR scannen.

**Erwartetes Verhalten:**
- Artikel-Dropdown wird gefuellt.
- FA-Feld wird gefuellt.
- Hint erscheint wie in TS-2.13 mit Auto-Fill des Lagerplatzes.

---

### TS-2.17 — FA-Feld leeren

**Vorbedingungen:**
- Nach TS-2.13 (Hint sichtbar, Auto-Fill aktiv auf `K-WAGEN-03`).

**Schritte:**
1. FA-Feld leeren (alle Zeichen entfernen) → Tab.

**Erwartetes Verhalten:**
- Hint verschwindet.
- Lagerplatz-Dropdown behaelt `K-WAGEN-03` (Auswahl wird nicht zurueckgesetzt — defensive).

---

### TS-2.18 — Lagerplatz aus API nicht im Dropdown (inaktiv)

**Vorbedingungen:**
- FA `12345` liegt auf Lagerplatz `OLD-PLATZ`.
- `OLD-PLATZ` ist `IsActive = false` ODER `IstBuchbar = false` → nicht im Buchungs-Dropdown.

**Schritte:**
1. Einbuchung oeffnen.
2. FA `12345` eingeben → Tab.

**Erwartetes Verhalten:**
- Gelbes Warning-Alert: "FA `12345` liegt bereits: Lagerplatz **OLD-PLATZ** — X Stk Artikel ... → Lagerplatz nicht im Buchungs-Dropdown (evtl. inaktiv oder nicht buchbar). Bitte manuell waehlen."
- Kein Auto-Fill. Dropdown bleibt leer.

---

### TS-2.19 — Permission: stock-only User

**Vorbedingungen:**
- User mit Rolle `stock` (KEINE `picking`, KEINE `tracking`).
- FA `12345` mit Bestand vorhanden.

**Schritte:**
1. Login als `stock`-User.
2. Einbuchung oeffnen.
3. FA `12345` eingeben → Tab.

**Erwartetes Verhalten:**
- Hint erscheint normal (kein 302/AccessDenied im Network-Tab).
- Auto-Fill funktioniert wie in TS-2.13.

---

### TS-2.20 — Validation-Error-Rerender zeigt Hint

**Vorbedingungen:**
- FA `12345` mit Bestand auf `K-WAGEN-03`.

**Schritte:**
1. Einbuchung oeffnen.
2. Artikel waehlen.
3. FA `12345` eingeben → Tab → Hint erscheint, Auto-Fill auf K-WAGEN-03.
4. Menge-Feld leer lassen.
5. "Einbuchung speichern" klicken.

**Erwartetes Verhalten:**
- Server rendert View neu mit ValidationSummary-Fehler "Menge erforderlich".
- FA-Feld ist mit `12345` vorbefuellt.
- Hint-Alert wird beim Page-Load wieder angezeigt (DOM-Ready-Init triggert den Check).
- Lagerplatz-Dropdown ist nach Rerender weiter auf `K-WAGEN-03` (ModelState-Restore + Auto-Fill).

---

### TS-2.21 — Artikel mit Sondereinheit

**Vorbedingungen:**
- Artikel mit `Unit = "m"` (z. B. Stahlband-Meterware).
- FA `78901` liegt auf 1 Platz mit 12,5 m.

**Schritte:**
1. Einbuchung oeffnen.
2. FA `78901` eingeben → Tab.

**Erwartetes Verhalten:**
- Hint zeigt "Lagerplatz **X-Y-Z** — 12,500 **m** Artikel ..." (Einheit aus API-Field `unit`, nicht hardcoded "Stk").
- Falls Artikel-Unit leer ist: Fallback "Stk".

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

### TS-4.9 — Leitstand-Massenfreigabe (Bulk-Freigabe)

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`.
- AppSetting `KommissionierungMitZuweisung = false`.
- Benutzer hat Rolle `leitstand` oder `admin`.
- Mehrere FAs vorhanden, davon mindestens 3 mit Artikelnummer und nicht freigegeben, eine ohne Artikelnummer.

**Schritte:**
1. Leitstand-Liste oeffnen.
2. Checkboxen fuer 4 FAs setzen — 3 mit Artikelnummer, 1 ohne.
3. Sticky-Bar oben zeigt "4 markiert".

**Erwartetes Verhalten:**
- "Markierte freigeben"-Button ist DISABLED — weil eine markierte FA keine Artikelnummer hat.
- Nach Demarkierung der FA ohne Artikelnummer (jetzt 3 markiert) ist der Button aktiv.
- Klick auf "Markierte freigeben" → 3 FAs werden freigegeben. SuccessMessage "3 Auftrag/Auftraege freigegeben."
- Spalten-Filter (falls gesetzt) bleiben nach Reload aktiv.

**Negativfall:**
- Mischauswahl (freigegebene + nicht-freigegebene) → beide Bulk-Buttons disabled.

---

### TS-4.9a — Leitstand-Bulk-Zuruecknehmen

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`.
- Mehrere FAs sind freigegeben.

**Schritte:**
1. Leitstand-Liste oeffnen.
2. Checkboxen fuer 3 freigegebene FAs setzen.
3. "Markierte zuruecknehmen" anklicken.

**Erwartetes Verhalten:**
- 3 FAs sind nicht mehr freigegeben (Status zurueckgesetzt).
- SuccessMessage "3 Freigabe(n) zurueckgenommen."
- Picker-Zuweisungen wurden entfernt.

---

### TS-4.9b — Leitstand-Bulk-Freigabe mit Picker-Zuweisung

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`.
- AppSetting `KommissionierungMitZuweisung = true`.
- Mehrere Picker sind aktiv.
- Mindestens 5 FAs mit Artikelnummer, nicht freigegeben.

**Schritte:**
1. Leitstand-Liste oeffnen.
2. Checkboxen fuer 5 FAs setzen.
3. "Markierte freigeben" anklicken.

**Erwartetes Verhalten:**
- Modal "Sammel-Freigabe" oeffnet sich, zeigt "5 Auftraege werden freigegeben."
- "Freigeben"-Button im Modal ist disabled.
- Nach Picker-Auswahl wird der Button aktiv.
- Submit → alle 5 FAs sind freigegeben mit dem gewaehlten Picker.

---

### TS-4.9c — Leitstand SelectAll respektiert Filter

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`.
- Leitstand zeigt 50+ FAs.

**Schritte:**
1. Leitstand-Liste oeffnen.
2. In der Spalten-Filter-Zeile "Werkbank" auf einen Wert filtern, sodass nur ~8 Zeilen sichtbar sind.
3. Header-Checkbox (SelectAll) anklicken.

**Erwartetes Verhalten:**
- Nur die 8 sichtbaren FAs werden markiert. Counter zeigt "8 markiert".
- Nach Filter-Entfernen sind die 8 weiterhin markiert; die anderen 42 bleiben unmarkiert.

---

### TS-4.9d — Filter-Persistenz nach Freigabe

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`.

**Schritte:**
1. Leitstand-Liste oeffnen.
2. In der Spalten-Filter-Zeile mehrere Filter setzen (z.B. Werkbank: "WB1").
3. Eine FA per Single-Klick "Freigeben" → Page reload.

**Erwartetes Verhalten:**
- Spalten-Filter bleiben nach Reload aktiv.
- "Zuruecksetzen" leert sowohl URL-Filter als auch Spalten-Filter.

**Negativfall:**
- Browser-Tab schliessen und neu oeffnen → Filter sind weg (sessionStorage erwartetes Verhalten).

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

### TS-4.11 — Neuer Auftrag hat alle 5 Baugruppen-Flags auf false

**Vorbedingungen:**
- Frisch importierter Auftrag aus Sage.

**Schritte:**
1. Produktionsauftragsliste oeffnen.

**Erwartetes Verhalten:**
- Spalten **VK**, **VL**, **VE**, **VT**, **VA** zeigen unchecked Checkboxes fuer den neuen Auftrag.

---

### TS-4.12 — Toggle VK persistiert ueber Page-Reload

**Vorbedingungen:**
- Ein Auftrag in der Liste sichtbar, User hat picking-Rolle.

**Schritte:**
1. VK-Checkbox des Auftrags aktivieren.
2. Page neu laden (F5).

**Erwartetes Verhalten:**
- VK-Checkbox bleibt nach Reload aktiviert.
- Andere Flags (VL/VE/VT/VA/Glas/Zukauf) bleiben unveraendert.

---

### TS-4.13 — Alle 5 Baugruppen-Flags unabhaengig toggeln

**Schritte:**
1. Nur VE aktivieren -> Reload -> nur VE checked.
2. Zusaetzlich VL aktivieren -> Reload -> VE und VL checked.
3. VE deaktivieren -> Reload -> nur VL checked.

**Erwartetes Verhalten:**
- Flags sind voneinander unabhaengig, kein Seiteneffekt.

---

### TS-4.14 — Tooltip-Volltext bei Header-Hover

**Schritte:**
1. Maus ueber Header `VK` halten (ohne Klick).
2. Wiederholen fuer VL, VE, VT, VA.

**Erwartetes Verhalten:**
- Browser-Tooltip erscheint mit Volltext: "VK Kaelte", "VL Luefter", "VE Elektro", "VT Tueren", "VA Aufbau".

---

### TS-4.15 — Spalten-Filter auf den neuen Baugruppen-Spalten

**Schritte:**
1. Filter-Funktion in der Liste aktivieren.
2. Spalte VK auf "checked" filtern.

**Erwartetes Verhalten:**
- Nur Auftraege mit VK=true sind sichtbar (Filter-Pattern aus Glas/Zukauf greift identisch).

---

### TS-4.16 — Berechtigung: Nicht-Picking-User sieht disabled Checkboxes

**Vorbedingungen:**
- User OHNE picking-Rolle, aber MIT tracking-Rolle (hat Zugang zur Liste).

**Schritte:**
1. Login als Tracking-User.
2. Produktionsauftragsliste oeffnen.

**Erwartetes Verhalten:**
- VK/VL/VE/VT/VA-Checkboxes sind `disabled` (analog zu Glas/Zukauf).
- Klick darauf hat keine Wirkung. Toggle-API wuerde 302->AccessDenied liefern.

---

### TS-4.17 — Sage-Sync ueberschreibt die Baugruppen-Flags NICHT

**Vorbedingungen:**
- Auftrag mit VK=true, VL=true gesetzt.

**Schritte:**
1. Sage-Import-Job manuell anstossen ODER auf naechsten Sync warten.
2. Produktionsauftragsliste neu laden.

**Erwartetes Verhalten:**
- VK und VL bleiben `true`.
- Andere Felder aus Sage (Lieferdatum, Stueckzahl, ...) sind ggf. aktualisiert, aber VK/VL/VE/VT/VA-Flags wurden nicht ueberschrieben.

---

### TS-4.18 — Column-Preferences-Offcanvas

**Schritte:**
1. "Spalten anpassen"-Button anklicken (oder Offcanvas-Trigger).

**Erwartetes Verhalten:**
- Die 5 neuen Spalten VK/VL/VE/VT/VA erscheinen in der Liste, koennen ein-/ausgeblendet werden wie Glas/Zukauf.
- User-Reihenfolge-Aenderungen werden gespeichert und nach Reload wiederhergestellt.

---

### TS-4.19 — Fresh-Install enthaelt die Baugruppen-Spalten

**Vorbedingungen:**
- Frische DB aus `SQL/00_FreshInstall.sql`.

**Schritte:**
1. `SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProductionOrders') AND name IN ('HasCooling', 'HasFan', 'HasElectric', 'HasDoors', 'HasSuperstructure');`

**Erwartetes Verhalten:**
- Query liefert alle 5 Zeilen — Spalten existieren in der frisch installierten DB.

---

### TS-4.20 — Toggle-Routing nach Refactor (v1.11.0)

**Vorbedingungen:**
- App auf v1.11.0, eingeloggt mit `picking`-Rolle, mindestens ein FA in der Liste sichtbar.
- Browser-DevTools mit aktivem Network-Tab.

**Schritte:**
1. Produktionsauftragsliste oeffnen.
2. Glas-Checkbox eines Auftrags togglen.
3. Zukauf-Checkbox togglen.
4. VK-Checkbox togglen.
5. VL-, VE-, VT-, VA-Checkboxen togglen.
6. Falls Lack-T-Spalte sichtbar: Lackierteile-Checkbox togglen.
7. Network-Tab: Request-URLs und Payloads pruefen.

**Erwartetes Verhalten:**
- Glas-Toggle -> POST `/api/picking-status/toggle` mit `field: "HasGlass"`, Status 200.
- Zukauf-Toggle -> POST `/api/picking-status/toggle` mit `field: "HasExternalPurchase"`, Status 200.
- VK/VL/VE/VT/VA-Toggle -> POST `/api/assembly-groups/toggle-applicable` mit `groupKey: "VK"` etc., Status 200.
- Lack-T-Toggle -> POST `/api/picking-status/toggle` mit `field: "IsCoatingDone"`, Status 200.
- Kein einziger Request an alten Endpoint `/api/productionorders/toggle-field`.

**Negativfall:**
- Bei fehlender picking-Rolle: 403 oder 302 -> AccessDenied. Datenbank bleibt unveraendert.

---

### TS-4.21 — Migrations-Verifikation Post-Cutover

**Vorbedingungen:**
- Migrations-SQL `SQL/60_ProductionOrderSplit.sql` (oder EF-Migration) wurde gegen die Produktiv-DB ausgefuehrt.

**Schritte:**
1. SSMS gegen die DB oeffnen.
2. Folgende Counts ausfuehren:
   ```sql
   SELECT COUNT(*) AS POs FROM dbo.ProductionOrders;
   SELECT COUNT(*) AS PSs FROM dbo.ProductionOrderPickingStatus;
   SELECT COUNT(*) AS BDEs FROM dbo.ProductionOrderBdeStatus;
   SELECT COUNT(*) AS Grps FROM dbo.ProductionOrderAssemblyGroups;
   ```
3. Optional: Schlanke Pruefung auf Orphans:
   ```sql
   SELECT COUNT(*) FROM dbo.ProductionOrderPickingStatus ps
     LEFT JOIN dbo.ProductionOrders po ON po.Id = ps.ProductionOrderId
   WHERE po.Id IS NULL;
   ```

**Erwartetes Verhalten:**
- `POs == PSs == BDEs` (jeweils 1:1).
- `Grps == 5 * POs` (5 Baugruppen-Zeilen pro FA).
- Orphan-Pruefung liefert 0.

**Negativfall:**
- Stimmen die Counts nicht: Migration ist nicht atomar durchgelaufen -> Rollback, Fehler in Sync-Log pruefen, Skript erneut anstossen.

---

### TS-4.22 — AgentJob legt 7 Status-Zeilen eager an

**Vorbedingungen:**
- Sage-View `vw_AKE_Kommissionierung_WAListe` hat einen NEU angelegten FA, der noch nicht in `ProductionOrders` existiert.

**Schritte:**
1. SQL Agent Job `01_Import_Produktionsauftraege` manuell ausfuehren.
2. SSMS: Neuen FA in `ProductionOrders` finden, `Id` notieren.
3. Folge-Queries:
   ```sql
   SELECT * FROM dbo.ProductionOrderPickingStatus WHERE ProductionOrderId = @neuerFaId;
   SELECT * FROM dbo.ProductionOrderBdeStatus     WHERE ProductionOrderId = @neuerFaId;
   SELECT GroupKey, IsApplicable FROM dbo.ProductionOrderAssemblyGroups WHERE ProductionOrderId = @neuerFaId ORDER BY GroupKey;
   ```
4. Job ein zweites Mal ausfuehren (Idempotenz-Check).

**Erwartetes Verhalten:**
- 1 Zeile in `ProductionOrderPickingStatus`, alle Boolean-Flags = 0, kein zugewiesener Picker.
- 1 Zeile in `ProductionOrderBdeStatus`, `IsDoneBde = 0`.
- 5 Zeilen in `ProductionOrderAssemblyGroups` mit `GroupKey IN ('VK','VL','VE','VT','VA')`, alle `IsApplicable = 0`.
- Zweite Job-Ausfuehrung legt KEINE Duplikate an (MERGE).

**Negativfall:**
- Weniger als 7 Zeilen: Folge-MERGEs im AgentJob laufen nicht. Job-History pruefen.

---

### TS-4.23 — Leitstand-Freigabe schreibt in ProductionOrderPickingStatus

**Vorbedingungen:**
- Test-FA mit Artikelnummer existiert, eingeloggt mit `leitstand`- (und ggf. `picking`-)Rolle.
- `LeitstandAktiv` und `KommissionierungMitZuweisung` aktiviert.

**Schritte:**
1. FA-Liste oeffnen, Freigabe-Toggle fuer den Test-FA aktivieren.
2. Picker-Zuweisung im Modal waehlen und bestaetigen.
3. Prioritaet auf z.B. 5 setzen.
4. SSMS pruefen:
   ```sql
   SELECT IsReleasedForPicking, PickingPriority, AssignedPickerId, ReleasedBy, ReleasedAt, ModifiedAt, ModifiedBy
     FROM dbo.ProductionOrderPickingStatus
    WHERE ProductionOrderId = @testFaId;
   SELECT TOP 1 * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProductionOrders') AND name IN ('IsReleasedForPicking','PickingPriority','AssignedPickerId');
   ```

**Erwartetes Verhalten:**
- `ProductionOrderPickingStatus` enthaelt: `IsReleasedForPicking = 1`, `PickingPriority = 5`, `AssignedPickerId = <gewaehlter User>`, `ReleasedBy` + `ReleasedAt` gesetzt, Audit-Felder `ModifiedAt`/`ModifiedBy` aktualisiert.
- Spalten `IsReleasedForPicking`, `PickingPriority`, `AssignedPickerId` existieren NICHT mehr in `dbo.ProductionOrders` (zweite Query liefert 0 Zeilen).

**Negativfall:**
- Toggle ohne `leitstand`-Rolle: 403/302 -> AccessDenied, kein Schreib-Zugriff auf die Status-Tabelle.

---

### TS-4.24 — Slim FA-Index als Tracking-User (v1.12.0)

**Vorbedingungen:**
- App auf v1.12.0. Eingeloggt als User mit AUSSCHLIESSLICH `tracking`-Rolle (keine `picking`-/`leitstand`-Rolle).
- Mindestens ein FA in der Liste sichtbar.

**Schritte:**
1. Menue `Fertigungsauftraege` oeffnen (URL `/ProductionOrders/Index`).
2. Spalten der Tabelle pruefen.
3. Versuchen, eine Inline-Checkbox (Glas, Zukauf, VK ...) zu klicken.

**Erwartetes Verhalten:**
- Tabelle zeigt nur Sage-Master-Spalten (FA-Nr, Artikelnummer, Bezeichnung, Termine, Werkbank, IsDone-Spalte).
- KEINE Komm-Status-Spalten (Glas/Zukauf/VK/VL/VE/VT/VA), keine Picker-Zuweisung, keine Bulk-Freigabe-Checkboxes.
- Network: KEINE Aufrufe an `/api/picking-status/...` oder `/api/assembly-groups/...` beim Laden.

**Negativfall:**
- Ohne `tracking`/`picking`/`leitstand`/`admin`-Rolle: 403/302 -> AccessDenied.

---

### TS-4.25 — Leitstand-Page als Picker-User (v1.12.0)

**Vorbedingungen:**
- App auf v1.12.0, `LeitstandAktiv` aktiv. Eingeloggt als User mit `picking`-Rolle.
- Mindestens ein FA in der Liste sichtbar.

**Schritte:**
1. Menue `Kommissionierung` aufklappen, Sub-Item `Leitstand` waehlen (URL `/PickingLeitstand/Index`).
2. Spalten der Tabelle pruefen.
3. Eine Glas-Checkbox togglen, eine VK-Checkbox togglen.
4. Network-Tab: Request-URLs pruefen.

**Erwartetes Verhalten:**
- Tabelle zeigt die reichen Status-Spalten: Glas, Zukauf, VK, VL, VE, VT, VA, Lack-T, Freigabe, Prioritaet, Picker, IsDonePicking.
- Toggle-Klicks: POST `/api/picking-status/toggle` bzw. `/api/assembly-groups/toggle-applicable`, Status 200.
- Bulk-Auswahl-Header-Checkbox sichtbar, Aktions-Bar fuer Bulk-Freigabe sichtbar.

**Negativfall:**
- Bei `LeitstandAktiv = false`: Sub-Item `Leitstand` ist nicht im Menue. Direkter URL-Aufruf zeigt eine entsprechende Meldung oder fuehrt zu AccessDenied (je nach Implementierung).

---

### TS-4.26 — Permission-Boundary: Tracking-only-User auf Leitstand-Page (v1.12.0)

**Vorbedingungen:**
- App auf v1.12.0. Eingeloggt mit AUSSCHLIESSLICH `tracking`-Rolle (kein `picking`, kein `leitstand`).
- `LeitstandAktiv` aktiv.

**Schritte:**
1. Direkter URL-Aufruf `/PickingLeitstand/Index`.
2. Browser-Verhalten beobachten (HTTP-Status, Ziel-URL).

**Erwartetes Verhalten:**
- HTTP 302 -> AccessDenied (oder 403 Forbidden) durch `[RequirePickingOrLeitstandAccess]` auf dem Controller.
- Keine Anzeige der Status-Spalten, kein Zugriff auf Bulk-Freigabe.

**Negativfall:**
- Falls Page trotzdem laedt: Filter-Attribut wurde entfernt -> Regression im Controller. Sofort eskalieren.

---

### TS-4.27 — Bulk-Release auf Leitstand-Page funktioniert (Phase-1-Regression)

**Vorbedingungen:**
- App auf v1.12.0. Eingeloggt als `admin` oder `leitstand`-User. `LeitstandAktiv` aktiv.
- Mindestens 3 FAs mit Artikelnummer in der Liste, davon mindestens 2 noch nicht freigegeben.

**Schritte:**
1. `/PickingLeitstand/Index` oeffnen.
2. Zwei nicht-freigegebene FAs per Zeilen-Checkbox markieren.
3. Sticky-Action-Bar -> Button `Bulk-Freigabe`.
4. Im Modal Picker waehlen (falls `KommissionierungMitZuweisung` aktiv) und bestaetigen.
5. Liste neu laden und Status der beiden FAs pruefen.

**Erwartetes Verhalten:**
- Beide FAs sind nach Submit `freigegeben`, Prioritaet wird sequentiell vergeben, Picker zugewiesen falls Modus aktiv.
- TempData-Success-Banner zeigt die Anzahl freigegebener Auftraege.
- POST geht an `/PickingLeitstand/BulkRelease` (nicht `/ProductionOrders/BulkRelease`).

**Negativfall:**
- Auftrag ohne Artikelnummer in der Auswahl -> wird uebersprungen + im Banner als "uebersprungen" gemeldet.

---

### TS-4.28 — Compat-Redirect: alte URL liefert 301 (v1.12.0)

**Vorbedingungen:**
- App auf v1.12.0. Eingeloggt als `admin` oder `leitstand`-User.
- Browser-DevTools mit aktivem Network-Tab und der Option `Preserve log`.

**Schritte:**
1. Auf `/PickingLeitstand/Index` eine Freigabe togglen (initialer Roundtrip).
2. Manuell die alte URL als Form-Resubmit triggern (z.&nbsp;B. via Postman oder DevTools "Edit and resend"):
   `POST /ProductionOrders/ToggleRelease` mit gueltigem AntiForgery-Token und FA-Id.
3. Im Network-Tab Status der Antwort pruefen.

**Erwartetes Verhalten:**
- Antwort: HTTP 301 (Moved Permanently) -> Location: `/PickingLeitstand/ToggleRelease`.
- Browser/Client folgt dem Redirect; nach erfolgreichem Form-Resubmit ist die Freigabe persistiert.
- Selbe Logik gilt fuer `BulkRelease`, `SetPriority`, `ChangeAssignedPicker`.

**Negativfall:**
- 404 statt 301: Stub-Action im alten Controller fehlt -> Bookmarks/Stale-Tabs sind broken. Hotfix erforderlich.

---

### TS-4.29 — Nav-Dropdown Sichtbarkeit nach Rolle (v1.12.0)

**Vorbedingungen:**
- App auf v1.12.0. `LeitstandAktiv` aktiv. Drei Test-User: (a) nur `picking`, (b) nur `leitstand`, (c) nur `tracking`.

**Schritte:**
1. Mit User (a) einloggen, Nav-Menue oeffnen.
2. Mit User (b) einloggen, Nav-Menue oeffnen.
3. Mit User (c) einloggen, Nav-Menue oeffnen.

**Erwartetes Verhalten:**
- User (a) Picker: Menue `Kommissionierung` ist Dropdown mit Sub-Items `Kommissionierliste` und `Leitstand`.
- User (b) Leitstand: Menue `Kommissionierung` ist Dropdown mit Sub-Item `Leitstand` (Kommissionierliste ggf. nicht).
- User (c) reines Tracking: KEIN `Kommissionierung`-Dropdown im Menue. Nur `Fertigungsauftraege`, `Teileverfolgung` und ggf. `OSEON`.
- Klick auf `Fertigungsauftraege` fuehrt fuer alle drei zur schlanken FA-Liste (`/ProductionOrders/Index`).

**Negativfall:**
- Bei `LeitstandAktiv = false`: Auch User (a) und (b) sehen kein `Leitstand`-Sub-Item.

---

### TS-4.30 — FA-Vervollstaendigung Index als fa_completion-User oeffnen (v1.13.0)

**Vorbedingungen:**
- App auf v1.13.0. Eingeloggt als User mit AUSSCHLIESSLICH `fa_completion`-Rolle (keine `picking`-/`tracking`-/`leitstand`-Rolle).
- Mindestens ein FA mit Sage-Master-Daten und 5 eager angelegten AssemblyGroups (VK/VL/VE/VT/VA) vorhanden.

**Schritte:**
1. Nav-Bar oeffnen.
2. Menuepunkt `FA-Vervollstaendigung` anklicken (URL `/FaCompletion/Index`).
3. Tabelle und Spalten pruefen.

**Erwartetes Verhalten:**
- Menuepunkt `FA-Vervollstaendigung` ist sichtbar. Andere Bereiche (Lager, Kommissionierung, Tracking) sind nicht sichtbar.
- Tabelle laedt erfolgreich (HTTP 200), zeigt FA-Liste mit Sage-Master-Spalten und einer `SpecCount`-Spalte (Anzahl Specs ueber alle Gruppen).
- Pro Zeile ist ein `Bearbeiten`-Button vorhanden, der zu `/FaCompletion/Edit/{id}` fuehrt.

**Negativfall:**
- Ohne `fa_completion`-/`admin`-Rolle: Menue-Eintrag nicht sichtbar; direkter URL-Aufruf liefert 302 -> AccessDenied.

---

### TS-4.31 — IsApplicable togglen geht an /api/assembly-groups/toggle-applicable (v1.13.0)

**Vorbedingungen:**
- App auf v1.13.0. Eingeloggt als `fa_completion`-User.
- FA `FA-2604001` existiert mit 5 AssemblyGroups (alle initial `IsApplicable=false`).
- DevTools Network-Tab offen.

**Schritte:**
1. `/FaCompletion/Edit/<id-von-FA-2604001>` oeffnen.
2. Tab `VL` anklicken.
3. Checkbox `Anwendbar` anhaken.
4. Network-Tab beobachten.
5. In SSMS pruefen:
   ```sql
   SELECT GroupKey, IsApplicable FROM dbo.ProductionOrderAssemblyGroups
   WHERE ProductionOrderId = <id> AND GroupKey = 'VL';
   ```

**Erwartetes Verhalten:**
- Network: POST `/api/assembly-groups/toggle-applicable` mit Status 200.
- DB: Zeile `GroupKey='VL'` hat `IsApplicable=1`.
- UI: Checkbox bleibt nach Reload aktiv; Spec-Liste innerhalb des Tabs ist nun editierbar.

**Negativfall:**
- Picker ohne `fa_completion`-Rolle aber MIT `picking`-Rolle: Toggle funktioniert ebenfalls (Filter `RequirePickingOrFaCompletionAccess`).
- Tracking-only-User: 302 -> AccessDenied auf den API-Call.

---

### TS-4.32 — Spec mit Artikel-Auswahl hinzufuegen (v1.13.0)

**Vorbedingungen:**
- App auf v1.13.0. Eingeloggt als `fa_completion`-User.
- FA `FA-2604001`, Tab `VL`, `IsApplicable=true` (siehe TS-4.31).
- Artikel `100023` existiert in `Articles`.

**Schritte:**
1. `/FaCompletion/Edit/<id>` oeffnen, Tab `VL`.
2. Inline-Add-Formular: Select2-Feld `Artikel` fokussieren, "100023" eintippen.
3. Aus der Liste `100023 - <Beschreibung>` waehlen.
4. Description manuell mit "Test-Lueftermotor" ergaenzen, Quantity `1.000`.
5. "+"-Button klicken (Form submit).
6. Page reload abwarten, Tabelle pruefen.

**Erwartetes Verhalten:**
- Erfolgs-Banner (`TempData["SuccessMessage"]`).
- Zeile in Spec-Tabelle: ArticleNumber `100023`, Description "Test-Lueftermotor", Quantity `1.000`.
- DB: Neuer Eintrag in `ProductionOrderAssemblyGroupSpecs` mit `CreatedAt`, `CreatedBy`, `CreatedByWindows` gesetzt.
- `SpecCount` in der `/FaCompletion/Index`-Liste fuer diesen FA hat sich um +1 erhoeht.

**Negativfall:**
- Submit ohne Description: `ModelState` invalid -> Form-Validierung zeigt Fehler, kein DB-Insert.

---

### TS-4.33 — Spec editieren setzt Audit-Felder ModifiedAt/By (v1.13.0)

**Vorbedingungen:**
- App auf v1.13.0. Eingeloggt als `fa_completion`-User `userB` (unterschiedlich vom Spec-Ersteller `userA`).
- Spec aus TS-4.32 existiert.

**Schritte:**
1. `/FaCompletion/Edit/<id>` oeffnen, Tab `VL`.
2. In der Spec-Zeile Description von "Test-Lueftermotor" auf "Test-Lueftermotor 230V" aendern.
3. "Speichern" klicken.
4. Nach Reload: Tabelle pruefen.
5. DB pruefen:
   ```sql
   SELECT Description, ModifiedAt, ModifiedBy, ModifiedByWindows
   FROM dbo.ProductionOrderAssemblyGroupSpecs WHERE Id = <spec-id>;
   ```

**Erwartetes Verhalten:**
- UI: Erfolgs-Banner, neue Description sichtbar.
- DB: `Description = 'Test-Lueftermotor 230V'`, `ModifiedAt` aktuell, `ModifiedBy = 'userB'`, `ModifiedByWindows = <user-windows-login>`.
- `CreatedAt`/`CreatedBy` (userA) bleiben unveraendert.

**Negativfall:**
- Submit mit ungueltiger Quantity (z.&nbsp;B. negativ): ModelState invalid, kein Update.

---

### TS-4.34 — Spec loeschen entfernt den Eintrag (v1.13.0)

**Vorbedingungen:**
- App auf v1.13.0. Eingeloggt als `fa_completion`-User.
- Spec aus TS-4.32/4.33 existiert.

**Schritte:**
1. `/FaCompletion/Edit/<id>` oeffnen, Tab `VL`.
2. In der Spec-Zeile "Loeschen" klicken -> Confirm-Modal bestaetigen.
3. Reload abwarten.
4. DB pruefen:
   ```sql
   SELECT COUNT(*) FROM dbo.ProductionOrderAssemblyGroupSpecs WHERE Id = <spec-id>;
   ```

**Erwartetes Verhalten:**
- UI: Erfolgs-Banner, Zeile ist aus der Tabelle entfernt.
- DB: `COUNT = 0` (harter Delete; kein Soft-Delete).
- `SpecCount` in `/FaCompletion/Index` hat sich um -1 reduziert.

**Negativfall:**
- Abbrechen im Confirm-Modal: Spec bleibt unveraendert in DB und UI.

---

### TS-4.35 — IsCompleted togglen setzt CompletedAt + CompletedBy (v1.13.0)

**Vorbedingungen:**
- App auf v1.13.0. Eingeloggt als `fa_completion`-User.
- FA `FA-2604001`, Tab `VL`, `IsApplicable=true`, mindestens 1 Spec vorhanden.

**Schritte:**
1. `/FaCompletion/Edit/<id>` oeffnen, Tab `VL`.
2. Checkbox `Vervollstaendigt` anhaken -> Form-POST `/FaCompletion/ToggleIsCompleted`.
3. Reload abwarten, Label pruefen.
4. DB pruefen:
   ```sql
   SELECT IsCompleted, CompletedAt, CompletedBy
   FROM dbo.ProductionOrderAssemblyGroups
   WHERE ProductionOrderId = <id> AND GroupKey = 'VL';
   ```
5. Erneut Checkbox anklicken (jetzt deaktivieren).

**Erwartetes Verhalten:**
- Nach Schritt 2-4: `IsCompleted=1`, `CompletedAt=<now>`, `CompletedBy=<current-user>`. Label zeigt User + Timestamp.
- Nach Schritt 5: `IsCompleted=0`, `CompletedAt=NULL`, `CompletedBy=NULL` (Zuruecksetzen).

**Negativfall:**
- Toggle ohne `IsApplicable=true`: Je nach Controller-Implementierung ModelState-Fehler oder Toggle bleibt erlaubt. Verifizieren, dass kein unkonsistenter Zustand entsteht.

---

### TS-4.36 — Permission-Boundary: Picker ohne fa_completion kann /FaCompletion nicht oeffnen (v1.13.0)

**Vorbedingungen:**
- App auf v1.13.0. Test-User mit AUSSCHLIESSLICH `picking`-Rolle (kein `fa_completion`, kein `admin`).

**Schritte:**
1. Mit Picker-User einloggen.
2. Nav-Bar oeffnen.
3. Direkter URL-Aufruf `/FaCompletion/Index`.
4. Direkter URL-Aufruf `/FaCompletion/Edit/<beliebige-id>`.

**Erwartetes Verhalten:**
- Nav-Menue: KEIN `FA-Vervollstaendigung`-Eintrag (Picker-Rolle alleine reicht nicht).
- Beide direkte URL-Aufrufe: HTTP 302 -> `/Account/AccessDenied` (Filter `RequireFaCompletionAccess` greift).
- Toggle-Endpoint `/api/assembly-groups/toggle-applicable` bleibt fuer Picker offen (gemeinsamer Filter `RequirePickingOrFaCompletionAccess`).

**Negativfall:**
- User hat sowohl `picking` als auch `fa_completion`: Beide Bereiche (PickingLeitstand + FaCompletion) sind sichtbar und voll bedienbar.

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

### TS-7.6a — OSEON Artikel-Filter zeigt nur matchende Sub-Auftraege

**Vorbedingungen:**
- Ein Kundenauftrag mit vielen Sub-Auftraegen vorhanden, einer der Subs hat Artikel `87015207`.
- Der Kundenauftrag enthaelt z. B. 99 Subs gesamt.

**Schritte:**
1. OSEON-Teileverfolgung oeffnen.
2. Im Filter `Artikelnummer` den Suchbegriff `87015207` eingeben, "Filtern" klicken.
3. Den gefundenen Kundenauftrag aufklappen.

**Erwartetes Verhalten:**
- Kundenauftrag-Header zeigt weiterhin `(2 / 99 fertig)` (volle Stats — Kontext bleibt erhalten).
- Aufgeklappt erscheint nur der eine matchende Sub-Auftrag mit Artikelnummer `87015207`.
- Andere 98 Subs sind ausgeblendet.
- Status-Badge der Gruppe zeigt weiterhin den schlechtesten Status der GESAMTEN Gruppe (nicht nur des matchenden Subs).

---

### TS-7.6b — OSEON Sub-Sortierung per Spalten-Klick

**Vorbedingungen:**
- Ein Kundenauftrag mit mehreren Sub-Auftraegen, unterschiedliche Artikelnummern und Endtermine.

**Schritte:**
1. OSEON-Teileverfolgung oeffnen.
2. Den Kundenauftrag aufklappen → Sub-Auftraege sind nach OseonOrderNumber asc sortiert (Default).
3. Auf Spalten-Header `Artikelnr.` klicken.
4. Erneut auf `Artikelnr.` klicken.
5. Erneut (3. Mal) auf `Artikelnr.` klicken.

**Erwartetes Verhalten:**
- 1. Klick: Sub-Auftraege alphabetisch nach Artikelnummer aufsteigend (▲-Indikator). Operations bleiben unter ihrem Sub.
- 2. Klick: absteigend (▼-Indikator).
- 3. Klick: zurueck zu Default-Sort (OseonOrderNumber asc, kein Indikator).
- Andere Kundenauftraege werden in ihren eigenen Gruppen ebenfalls sortiert (nicht uebergreifend).

---

### TS-7.6c — OSEON Endtermin-Sort: nulls last

**Vorbedingungen:**
- Mindestens 3 Sub-Auftraege im selben Kundenauftrag — einer davon ohne Endtermin.

**Schritte:**
1. OSEON-Teileverfolgung oeffnen.
2. Den Kundenauftrag aufklappen.
3. Auf Spalten-Header `Endtermin` klicken (asc).
4. Erneut auf `Endtermin` klicken (desc).

**Erwartetes Verhalten:**
- Asc: naechste Termine oben, Sub OHNE Endtermin am Ende.
- Desc: spaeteste Termine oben, Sub OHNE Endtermin trotzdem am Ende (nicht oben).
- Keine DOM-Glitches, Operations folgen ihrem Sub.

---

### TS-7.6d — OSEON Filter + Sort kombiniert

**Vorbedingungen:**
- Wie TS-7.6a.

**Schritte:**
1. Artikel-Filter setzen (= 1 matchender Sub pro Gruppe sichtbar).
2. Auf Spalten-Header `Artikelnr.` klicken.

**Erwartetes Verhalten:**
- Sortierung wirkt nur auf die sichtbaren Subs (1 pro Gruppe).
- Kein JS-Fehler, kein DOM-Glitch.

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

## 16. OSEON Reporting — AG-Übersicht

### TS-16.1 — KPI-Cards zeigen korrekte Counts
**Vorbedingungen:** Reporting-Rolle, mindestens 1 AG je Bereich (Überfällig / Heute / Zukunft) im OSEON-Mirror.
**Schritte:**
1. Menü Reporting -> OSEON AG-Übersicht öffnen.

**Erwartet:** 4 KPI-Cards zeigen plausible Counts; Summe Heute geplant ≥ Heute erledigt.

### TS-16.2 — Tab-Wechsel filtert Tabelle
**Vorbedingungen:** Daten in allen 3 Slices vorhanden.
**Schritte:**
1. Tabs reihum klicken: Heute / Überfällig / Zukunft / Alle.

**Erwartet:** Tabelle zeigt nur Zeilen des aktiven Slice. Default-Tab ist Heute.

### TS-16.3 — Filter Werkbank + AG-Name greifen
**Vorbedingungen:** Mehrere Werkbänke, mehrere AG-Namen.
**Schritte:**
1. Werkbank-Dropdown auf "WB-A1" stellen, AG-CSV "B" eintragen, Anwenden klicken.

**Erwartet:** Nur AGs vom Typ B an WB-A1 sichtbar. KPI-Counts spiegeln Filter.

### TS-16.4 — Auftragsnummer-Link öffnet Tracking
**Schritte:**
1. In der Tabelle auf eine FA-Nummer klicken.

**Erwartet:** OSEON-Tracking-Seite öffnet in neuem Tab, vorgefilltert mit der FA-Nummer.

### TS-16.5 — Banner für ungepflegte Configs
**Vorbedingungen:** AG-Name "FOO" in OSEON-Auftrag, KEIN Eintrag in OseonOperationConfig.
**Schritte:**
1. Reporting öffnen.

**Erwartet:** Gelber Banner "X AG(s) ohne Config-Eintrag".

### TS-16.6 — Berechtigungs-Block
**Vorbedingungen:** User OHNE Rolle reporting.
**Schritte:**
1. Direkter URL-Aufruf /OseonReporting/OperationsOverview.

**Erwartet:** Redirect auf AccessDenied.

## 17. OSEON Tracking — Artikel-Filter

### TS-17.1 — Artikelnummer-Filter via Form-Submit
**Vorbedingungen:** Tracking-Rolle, mindestens 3 OSEON-Auftraege mit unterschiedlichen Artikelnummern, davon mindestens einer mit Artikelnummer-Pattern "ART-100*".
**Schritte:**
1. /Tracking/OseonIndex oeffnen.
2. In das Feld "Artikelnummer" `100` eingeben.
3. Auf "Filtern" klicken.
**Erwartet:** Ergebnis enthaelt nur Customer-Order-Gruppen mit mindestens einem Sub-Auftrag dessen ArticleNumber `100` enthaelt. Innerhalb einer matchenden Gruppe werden ALLE Sub-Auftraege angezeigt (auch nicht-matchende — Group-Pagination). Filter-Wert bleibt im Input. Pagination wirkt auf gefiltertes Group-Ergebnis.

### TS-17.2 — Kombinierter Filter (Artikel + Werkbank + Auftrag)
**Vorbedingungen:** Daten mit verschiedenen Werkbaenken + Artikelnummern.
**Schritte:**
1. Artikelnummer eingeben + Werkbank waehlen + Auftragsnummer-Suchterm eingeben.
2. "Filtern" klicken.
**Erwartet:** Schnittmenge aller Filter (konjunktiv auf Group-Ebene). Reset-Link sichtbar.

### TS-17.3 — QR-Scan triggert Form-Submit
**Schritte:**
1. Auf den QR-Button neben dem Artikelnummer-Input klicken.
2. Artikel-QR-Code scannen.
**Erwartet:** Input wird mit dem gescannten Wert befuellt UND Form wird automatisch submitted (Page-Reload mit Filter aktiv).

### TS-17.4 — Reset entfernt alle Filter inkl. Artikel
**Vorbedingungen:** Mindestens ein Filter ist aktiv (Artikel, Auftrag, Werkbank, ShowFinished oder useRelevanceFilter=false).
**Schritte:**
1. "Zuruecksetzen"-Link klicken.
**Erwartet:** Alle Filter zurueckgesetzt (Artikel-Input leer), volle Liste sichtbar.

---

## 18. Lagerbestellung aus der Produktion

### TS-18.1 — Erfassen + Submit (1-Werkbank-User)
Vorbedingungen: User mit genau 1 Werkbank-Zuordnung. AppSetting DefaultLagerbestellempfaengerId gesetzt + Empfaengergruppe mit aktivem Recipient.
Schritte: Bestellungen &rarr; Lagerbestellungen &rarr; "+ Neue Liste". 2 Artikel ueber Suche hinzufuegen. "Abschicken".
Erwartet: Liste in Status "Abgeschickt" sichtbar im Erfasser-Index. Lager-Index zeigt sie unter "Submitted".

### TS-18.2 — Werkbank-Auswahl bei N≥2
Vorbedingungen: User mit 2+ Werkbank-Zuordnungen.
Schritte: "+ Neue Liste" ohne Werkbank waehlen.
Erwartet: WarningMessage "Bitte Werkbank waehlen". Nach Auswahl: Liste wird angelegt.

### TS-18.3 — 0 Werkbank-Zuordnungen
Vorbedingungen: User ohne Werkbank-Zuordnung.
Schritte: "+ Neue Liste".
Erwartet: WarningMessage "Bitte Werkbank-Zuordnung in Stammdaten pflegen".

### TS-18.4 — Duplikat-Artikel
Schritte: zwei Mal denselben Artikel hinzufuegen.
Erwartet: Beim zweiten Klick Toast/Alert "Artikel ist bereits in der Liste".

### TS-18.5 — Submit-Mail
Vorbedingungen: TS-18.1, Service-Setting Sync:WarehouseRequisitionEmailEnabled=true.
Schritte: max. 15 Min warten oder Worker-Tick triggern.
Erwartet: E-Mail im Postfach des Empfaengers, Subject "Lagerbestellung #X — Werkbank Y", Body mit Items + Deep-Link.

### TS-18.6 — Storno-Mail
Schritte: nach Submit (TS-18.1) im Erfasser-Edit "Stornieren" mit Grund.
Erwartet: Liste Status "Storniert". Nach Worker-Tick zweite Mail mit Subject "[STORNO] …".

### TS-18.7 — Lager: Detail + Print + Close
Schritte: Lager-Detail oeffnen, Pro-Position Ist-Menge anpassen, Drucken, Abschliessen.
Erwartet: Print-Tab oeffnet mit A4-Layout, Submit setzt Status "Erledigt", Items.QuantityPicked geschrieben.

### TS-18.8 — RowVersion-Konflikt
Schritte: Detail in zwei Tabs oeffnen. Tab 1 Schliessen, Tab 2 Stornieren.
Erwartet: Tab 2 zeigt WarningMessage "Bestellung wurde inzwischen geaendert — bitte Liste neu laden."

### TS-18.9 — AppSetting nicht gesetzt
Vorbedingungen: DefaultLagerbestellempfaengerId leer.
Schritte: Submit.
Erwartet: WarningMessage "Default-Lagerbestellempfaenger nicht konfiguriert".

---

## 19. Listen-Pagination & User-Default (v1.14.0)

### TS-19.1 — Default-Pagesize ist 25
Vorbedingungen: User ohne gesetzten `DefaultPageSize`.
Schritte: FA-Liste oeffnen.
Erwartet: Footer zeigt "Eintraege 1-25 von N", Drop-Down steht auf "25", Pagination-Bar mit Seiten-Links sichtbar wenn N > 25.

### TS-19.2 — Pro-Seite-Auswahl ueberschreibt Default
Vorbedingungen: User-Default = 25, > 50 FAs vorhanden.
Schritte: FA-Liste &rarr; Drop-Down auf "50" &rarr; URL wird zu `?pageSize=50` &rarr; "Eintraege 1-50".
Erwartet: 50 Eintraege sichtbar, Drop-Down zeigt "50", URL hat `pageSize=50`. Browser-Back fuehrt zurueck zu pageSize=25.

### TS-19.3 — User-Default in Profil setzen
Schritte: Profil &rarr; Sektion "Listen-Ansicht" &rarr; Drop-Down auf "100" &rarr; Speichern &rarr; FA-Liste oeffnen.
Erwartet: Liste laedt mit 100 Eintraegen/Seite (URL ohne pageSize-Param). Wechsel auf andere Liste (z.&nbsp;B. Bestand) ebenfalls 100/Seite.

### TS-19.4 — User-Default "Alle" mit Cap-Banner
Vorbedingungen: > 5000 FAs in DB.
Schritte: Profil &rarr; Default "Alle" &rarr; FA-Liste oeffnen.
Erwartet: Bis zu 5000 Eintraege geladen, gelbes Banner "Treffer auf 5.000 begrenzt &mdash; bitte Filter eingrenzen".

### TS-19.5 — Admin setzt User-Default
Schritte: Stammdaten &rarr; Benutzer &rarr; einen User editieren &rarr; "Eintraege pro Seite (Standard)" auf "50" &rarr; Speichern.
Erwartet: User sieht beim naechsten Login alle Listen mit 50/Seite.

### TS-19.6 — Filter ueberlebt Seitenwechsel + Pagesize-Wechsel
Schritte: FA-Liste &rarr; Filter "Kunde" eingeben &rarr; Seite 2 &rarr; Pagesize 50.
Erwartet: Filter "Kunde" bleibt aktiv (URL `?filterCustomer=Kunde&page=2` bzw. `pageSize=50`).

---

## 20. Server-Side Spaltenfilter (v1.14.0)

### TS-20.1 — Filter findet Treffer auf anderer Seite
Vorbedingungen: FA-Liste mit > 25 Eintraegen; spezifischer Kunde existiert in vorletzter Seite.
Schritte: Spaltenfilter "Kunde" mit dem Kundennamen befuellen.
Erwartet: Liste navigiert zu URL `?colf_customer=<name>`, zeigt alle Treffer (auf Seite 1, da Filter resettet `page`), Pagination zeigt korrekte Treffermenge.

### TS-20.2 — OR-Filter
Schritte: Spaltenfilter "Artikelnummer" mit `886,960` befuellen.
Erwartet: Liste zeigt Treffer mit Artikelnummer enthaelt "886" ODER "960".

### TS-20.3 — NOT-Filter
Schritte: Spaltenfilter "Status" mit `!Erledigt` befuellen.
Erwartet: Liste zeigt nur Auftraege deren Status nicht "Erledigt" enthaelt.

### TS-20.4 — Datumsfilter ueber alle Seiten
Vorbedingungen: FA-Liste, FAs mit Komm.-Termin in KW24 existieren auf Seite 5+.
Schritte: Spaltenfilter "Komm." mit `kw24` befuellen.
Erwartet: Liste zeigt nur Eintraege deren Komm.-Termin in KW24 liegt &mdash; Treffer aus allen Seiten enthalten.

### TS-20.5 — Filter loeschen
Schritte: Aktiven Filter entfernen (Input leeren).
Erwartet: URL-Parameter `colf_*` wird entfernt, Liste laedt ohne diesen Filter.

### TS-20.6 — Kombination mit Form-Filter
Schritte: Filter-Form-Field "Artikelnummer" + Spaltenfilter "Kunde" gleichzeitig.
Erwartet: Beide Filter wirken (UND-Verknuepfung).

### TS-20.7 — Filter-Persistenz in URL
Schritte: Filter aktiv setzen, URL kopieren, in neuem Tab oeffnen.
Erwartet: Filter ist im neuen Tab aktiv.

---

## 21. Leitstand als eigenes Hauptmenue (v1.14.0)

### TS-21.1 — Leitstand-Hauptmenue
Vorbedingungen: User mit Rolle `leitstand` oder `picking`, `LeitstandAktiv=true`.
Schritte: Navigation pruefen.
Erwartet: "Leitstand" als eigenes Hauptmenue (Dropdown) mit Unterpunkt "Kommissionierung". "Kommissionierung" im Hauptmenue ist ein einfacher Link (Picker-Worklist) mit Badge wenn freigegebene Eintraege vorhanden.

### TS-21.2 — Dashboard zeigt Leitstand-Sektion
Schritte: Startseite/Dashboard oeffnen.
Erwartet: Eigene Sektion "Leitstand" mit Kachel "Kommissionierung" (Verlinkung auf PickingLeitstand/Index). Sichtbar wenn `LeitstandAktiv=true` && (canPick OR canManagePickingRelease).

---

## 22. Lagerbestellungen — Notiz + INT-Mengen (v1.14.0)

### TS-22.1 — Notiz speichert on Blur
Vorbedingungen: Lagerbestellung im Status "Abgeschickt", User mit Lager-Berechtigung.
Schritte: Details oeffnen &rarr; Notiz "Eilig" in Position eintippen &rarr; in anderes Feld klicken (Blur).
Erwartet: AJAX-Request an `/WarehousePicking/SaveNotes/{id}` mit 200 OK. Liste neu laden &rarr; Notiz noch vorhanden.

### TS-22.2 — Notiz speichert vor Drucken
Schritte: Notiz eintippen &rarr; sofort auf "Drucken" klicken (ohne Blur).
Erwartet: Tab oeffnet `about:blank` synchron, Notiz wird gespeichert, dann zur Print-URL navigiert. Notiz erscheint im Druck.

### TS-22.3 — Notiz im Druck
Vorbedingungen: Notiz auf mind. einer Position.
Schritte: "Drucken" klicken.
Erwartet: Print-Tab zeigt Spalte "Notiz" mit Inhalt.

### TS-22.4 — Notiz read-only nach Abschluss
Vorbedingungen: Lagerbestellung mit Notiz, Status "Erledigt".
Schritte: Details oeffnen.
Erwartet: Notiz wird als Text angezeigt (kein Input-Feld).

### TS-22.5 — Mengen sind INT
Vorbedingungen: Position mit Bestell-Menge 4.
Schritte: Detail oeffnen &rarr; Ist-Mengen-Feld pruefen.
Erwartet: Anzeige "4" (nicht "4,0000"), Input akzeptiert keine Kommazahlen (step=1), Bestellt-Spalte zeigt "4".

### TS-22.6 — SOLL = IST bucht korrekt
Vorbedingungen: Position mit Bestell-Menge 4, Ist-Feld leer.
Schritte: "Abschliessen" &rarr; Modal "SOLL = IST?" &rarr; "Ja".
Erwartet: Ist-Menge wird auf 4 gesetzt (NICHT 4000). Liste abgeschlossen, QuantityPicked = 4.

---

## 23. FA-Vervollstaendigung als Feature-Toggle (v1.14.0)

### TS-23.1 — Feature standardmaessig OFF
Vorbedingungen: Frisch installiertes oder upgradetes System.
Schritte: User mit Rolle `fa_completion` einloggen.
Erwartet: Kein Menuepunkt "FA-Vervollstaendigung". Direkter URL-Aufruf `/FaCompletion` &rarr; Redirect AccessDenied.

### TS-23.2 — Aktivieren
Schritte: Admin &rarr; Stammdaten &rarr; Einstellungen &rarr; Sektion "FA-Vervollstaendigung" &rarr; `FaCompletionAktiv` auf `true` &rarr; Speichern.
Erwartet: User mit Rolle `fa_completion` sehen nach Reload den Menuepunkt; Zugriff auf `/FaCompletion` funktioniert.

### TS-23.3 — Picker funktionieren weiter
Vorbedingungen: `FaCompletionAktiv=false`, User mit `picking`-Rolle.
Schritte: PickingLeitstand &rarr; VK/VL/...-Toggles antippen.
Erwartet: Toggle funktioniert weiterhin (Endpoint `assembly-groups/toggle-applicable` blockt Picker nicht durch das Setting).

---

## 24. StorageLocation-Code 50 Zeichen (v1.14.0)

### TS-24.1 — Sage-Sync akzeptiert Code > 12 Zeichen
Vorbedingungen: Sage-View liefert Lagerplatz mit Kurzbezeichnung "LAGER1.A.01-RG-12" (17 Zeichen).
Schritte: LagerplatzSync triggern.
Erwartet: Lagerplatz angelegt, Code = "LAGER1.A.01-RG-12", IstBuchbar=false (Default Sage).

### TS-24.2 — Manueller Code max 12 Zeichen
Schritte: Stammdaten &rarr; Lagerplaetze &rarr; "Neu" &rarr; Code "ABCDEFGHIJKLM" (13 Zeichen) &rarr; Speichern.
Erwartet: Validierungs-Fehler "Manuelle Lagerplatz-Codes duerfen maximal 12 Zeichen lang sein...".

### TS-24.3 — Sage-Code im Edit-View nicht abgeschnitten
Vorbedingungen: Sage-Lagerplatz mit 20-Zeichen-Code.
Schritte: Stammdaten &rarr; Lagerplaetze &rarr; Edit.
Erwartet: Code-Input zeigt vollen 20-Zeichen-Wert (kein Abschneiden auf 12).

---

## 25. OSEON-Tabellen-Hover (v1.14.0)

### TS-25.1 — Hover-Farbe in OSEON Tracking
Schritte: OSEON Tracking oeffnen &rarr; mit der Maus ueber eine Auftragsgruppen-/Sub-/AG-Zeile fahren.
Erwartet: Hover-Farbe einheitlich AKE-Hellblau (`#D9F4FF`) &mdash; identisch zur FA-Liste. Vorher waren es dezente Grau-Toene pro Hierarchie-Ebene.

---

## 26. Bestand: Source-Lagerplatz-Vorschlag bei Sage-Stock (v1.14.0)

### TS-26.1 — NAN-Fallback bei nicht-buchbarem Sage-Stock
Vorbedingungen: Artikel mit Bestand auf Sage-Lagerplatz "GL;9;1;1" (IstBuchbar=false), NAN-Bestand -1.
Schritte: BOM-Liste oeffnen &rarr; entsprechende Position.
Erwartet: Quell-Lagerplatz-Dropdown zeigt **NAN** als Vorschlag (vorher: "--" weil Sage-Platz nicht im Dropdown).

### TS-26.2 — Buchbarer Stock wird bevorzugt
Vorbedingungen: Artikel mit Bestand auf buchbarem manuellen Platz UND auf Sage-Platz.
Schritte: BOM-Liste oeffnen.
Erwartet: Vorschlag = manueller Platz (hoechste Menge unter buchbaren).

---

## 27. SyncLog-Pflicht fuer alle Sync-Services (v1.15.0)

### Szenario 27.1: LagerplatzSyncService schreibt Start + Ende
**Vorbedingung:** Lagerplatz-Sync ist aktiviert (`Sync:LagerplaetzeEnabled = true`).
**Schritt:**
1. Service starten oder Worker-Tick abwarten.
2. Im Sync-Protokoll Service-Filter = "Lagerplatz" setzen.
**Erwartet:** Mindestens 2 Eintraege pro Tick: "Run gestartet" (Info) und "Run erfolgreich beendet
— neu=…, aktualisiert=…, …" (Info).

### Szenario 27.2: LagerbestandSyncService schreibt Start + Ende
Wie 27.1, mit Service-Filter "Lagerbestand".

### Szenario 27.3: BomCacheSyncService — Lifecycle im Sync-Protokoll
**Vorbedingung:** `Sync:BomCacheEnabled = true`.
**Schritt:** Worker-Tick abwarten, Service-Filter "BomCache".
**Erwartet:** Start- + End-Eintrag mit Counts (neu, aktualisiert, uebersprungen).

### Szenario 27.4: OseonSyncService — Lifecycle
Service-Filter "OseonTracking".

### Szenario 27.5: EnaioDmsSyncService — Lifecycle
Service-Filter "EnaioDms".

### Szenario 27.6: HolidaySyncService — Lifecycle und HTTP-Fehlerpfad
**Schritt:** Service-Filter "Holiday".
**Erwartet:** Start + Ende. Falls date.nager.at nicht erreichbar: zusaetzlich ein Warning-Eintrag
"date.nager.at lieferte {statusCode} fuer Jahr {year}".

### Szenario 27.7: CoatingDetectionService — Lifecycle
Service-Filter "CoatingDetection".

### Szenario 27.8: SageImportService — zwei Runs pro Tick (Production Orders + Articles)
**Schritt:** Worker-Tick mit aktiven Sage-Imports.
**Erwartet:** Im Sync-Protokoll erscheinen pro Tick **zwei** logische Runs:
- Service "ProductionOrder": Start + Ende
- Service "Article": Start + Ende

---

## 28. Activity-Log fuer Non-Sync-Services (v1.15.1)

### Szenario 28.1: PartRequisitionEmailService schreibt Lifecycle + Referenzen
**Vorbedingung:** `Sync:PartRequisitionEmailEnabled = true`, ungesendete Bedarfsmeldungen vorhanden.
**Schritt:**
1. Worker-Tick abwarten oder Service manuell starten.
2. Im Aktivitaets-Protokoll Service-Filter = "PartRequisitionEmail" setzen.
**Erwartet:**
- "Run gestartet" (Info)
- Pro versendete Mail-Gruppe ein Info-Eintrag mit Reference = FA-Nummer
- Bei "keine aktiven Empfaenger": Warning mit Reference = FA-Nummer
- "Run erfolgreich beendet — versendet=…, ohne_empfaenger=…, fehler=…"

### Szenario 28.2: WarehouseRequisitionEmailService — Submit + Storno differenziert
**Vorbedingung:** Je ein offener Submit und Storno in der DB.
**Schritt:** Worker-Tick, Filter = "WarehouseRequisitionEmail".
**Erwartet:**
- Pro Submit-Mail: Info-Eintrag mit Reference = "submit/{id}"
- Pro Storno-Mail: Info-Eintrag mit Reference = "storno/{id}"
- End-Summary: "submit_versendet=…, storno_versendet=…, fehler=…"

### Szenario 28.3: BdeAutoPauseService loggt auto-pausierte Bookings
**Vorbedingung:** `BdeSchichtkalenderAktiv = true`, mindestens eine Running-Buchung nach Schichtende.
**Schritt:** Worker-Tick (60-Min-Intervall) abwarten, Filter = "BdeAutoPause".
**Erwartet:**
- "Run gestartet"
- Pro auto-pausierten Booking: Info-Eintrag mit Reference = "booking/{id}"
- End-Summary: "geprueft=…, pausiert=…, fehler=…"

---

## Kapitel 29: OSEON Stammdaten-Imports im Aktivitaets-Protokoll (v1.15.2)

### Szenario 29.1: OseonWorkplaces — Lifecycle
**Vorbedingung:** `Sync:OseonTrackingEnabled = true`, OSEON-Werkbank-Zuordnungen aenderbar.
**Schritt:** Worker-Tick, Service-Filter "OseonWorkplaces".
**Erwartet:** Start + End-Summary mit `aktualisiert=N`.

### Szenario 29.2: OseonArticleCategories — Lifecycle
**Vorbedingung:** `Sync:OseonArticleCategoryEnabled = true`.
**Schritt:** Worker-Tick, Service-Filter "OseonArticleCategories".
**Erwartet:** Start + End-Summary mit `neu` + `aktualisiert`.

---

*Ende des Dokuments. Stand: v1.15.2 (2026-05-27)*
*Bei neuen Features: Szenarien in den entsprechenden Bereich einfuegen und TS-Nummern fortfuehren.*

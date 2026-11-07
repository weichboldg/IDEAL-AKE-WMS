# Testszenarien — IDEAL-AKE WMS

**Stand:** 2026-07-22 (v1.26.0)

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
| 2. Lager | [→](#2-lager) | TS-2.1 – TS-2.21, TS-2.26 – TS-2.31 (inkl. TS-2.12 – TS-2.21 FA-Lagerplatz-Hinweis; TS-2.26 – TS-2.31 v1.30.0 Teil 4/5: Bewegungsart-/Datum-Spaltenfilter funktionsfaehig + Einbuchung-Standardmenge 1) |
| 3. Stammdaten | [→](#3-stammdaten) | TS-3.1 – TS-3.16 |
| 4. Fertigungsauftraege | [→](#4-fertigungsauftraege) | TS-4.1 – TS-4.19 (inkl. TS-4.9a/b/c/d Bulk-Freigabe + Filter-Persistenz, TS-4.11 – TS-4.19 Baugruppen-Flags VK/VL/VE/VT/VA) |
| 5. Stueckliste (BOM) | [→](#5-stueckliste-bom) | TS-5.1 – TS-5.11 |
| 6. Kommissionierung / Picking | [→](#6-kommissionierung--picking) | TS-6.1 – TS-6.10 |
| 7. OSEON Teileverfolgung | [→](#7-oseon-teileverfolgung) | TS-7.1 – TS-7.10 (inkl. TS-7.6a/b/c/d Artikel-Filter + Sortierung; Pflege der Offsets seit v1.54.0 in TS-87) |
| 8. BDE Phase 1 | [→](#8-bde-phase-1) | TS-8.1 – TS-8.15 |
| 9. BDE Phase 2.1 — Werkbank-Erweiterungen | [→](#9-bde-phase-21--werkbank-erweiterungen) | TS-9.1 – TS-9.10 |
| 10. BDE Phase 2.2 — Mehrfachanmeldung + Zeit-Split | [→](#10-bde-phase-22--mehrfachanmeldung--zeit-split) | TS-10.1 – TS-10.15 |
| 11. Bestellungen / Bedarfsmeldungen | [→](#11-bestellungen--bedarfsmeldungen) | TS-11.1 – TS-11.7 |
| 12. Print + OSEON-Tracking-Verbesserungen | [→](#12-print--oseon-tracking-verbesserungen) | TS-12.1 – TS-12.6 |
| 13. Spalten-Konfiguration & Filter | [→](#13-spalten-konfiguration--filter) | TS-13.1 – TS-13.7 |
| 14. Service / Sync (read-only Verifikation) | [→](#14-service--sync-read-only-verifikation) | TS-14.1 – TS-14.4 |
| 15. BDE Phase 2.3 — Schichtkalender + Auto-Pause | [→](#15-bde-phase-23--schichtkalender--auto-pause) | TS-15.1 – TS-15.12 |
| 16. OSEON Reporting — AG-Übersicht | [→](#16-oseon-reporting--ag-übersicht) | TS-16.1 – TS-16.6 |
| 17. OSEON Tracking — Artikel-Filter | [→](#17-oseon-tracking--artikel-filter) | TS-17.1 – TS-17.4 |
| 18. Lagerbestellung aus der Produktion | [→](#18-lagerbestellung-aus-der-produktion) | TS-18.1 – TS-18.10 |
| 19. Listen-Pagination & User-Default (v1.14.0) | [→](#19-listen-pagination--user-default-v1140) | TS-19.1 – TS-19.6 |
| 20. Server-Side Spaltenfilter (v1.14.0) | [→](#20-server-side-spaltenfilter-v1140) | TS-20.1 – TS-20.7 |
| 21. Leitstand als eigenes Hauptmenue (v1.14.0) | [→](#21-leitstand-als-eigenes-hauptmenue-v1140) | TS-21.1 – TS-21.2 |
| 22. Lagerbestellungen — Notiz + INT-Mengen (v1.14.0) | [→](#22-lagerbestellungen--notiz--int-mengen-v1140) | TS-22.1 – TS-22.6 |
| 23. FA-Vervollstaendigung als Feature-Toggle (v1.14.0) | [→](#23-fa-vervollstaendigung-als-feature-toggle-v1140) | TS-23.1 – TS-23.3 |
| 24. StorageLocation-Code 50 Zeichen (v1.14.0) | [→](#24-storagelocation-code-50-zeichen-v1140) | TS-24.1 – TS-24.3 |
| 25. OSEON-Tabellen-Hover (v1.14.0) | [→](#25-oseon-tabellen-hover-v1140) | TS-25.1 |
| 26. Bestand: Source-Lagerplatz-Vorschlag bei Sage-Stock (v1.14.0) | [→](#26-bestand-source-lagerplatz-vorschlag-bei-sage-stock-v1140) | TS-26.1 – TS-26.2 |
| 27. SyncLog-Pflicht fuer alle Sync-Services (v1.15.0) | [→](#27-synclog-pflicht-fuer-alle-sync-services-v1150) | — |
| 28. Activity-Log fuer Non-Sync-Services (v1.15.1) | [→](#28-activity-log-fuer-non-sync-services-v1151) | — |
| Kapitel 29: OSEON Stammdaten-Imports im Aktivitaets-Protokoll (v1.15.2) | [→](#kapitel-29-oseon-stammdaten-imports-im-aktivitaets-protokoll-v1152) | — |
| Kapitel 30: OSEON-Tracking iOS-Fix + Lazy-Load (v1.16.0) | [→](#kapitel-30-oseon-tracking-ios-fix--lazy-load-v1160) | — |
| 31. Artikel-Sync-Erweiterung (v1.17.0) | [→](#31-artikel-sync-erweiterung-v1170) | — |
| 32. Lagerbestellungen Teilgeliefert + Fehlteile (v1.18.0) | [→](#32-lagerbestellungen-teilgeliefert--fehlteile-v1180) | — |
| 33. ShortageStatus 3-State + 2-Tab Fehlteile (v1.19.0) | [→](#33-shortagestatus-3-state--2-tab-fehlteile-v1190) | — |
| Kapitel 34: Feingranulare Berechtigungen (v1.20.0) | [→](#kapitel-34-feingranulare-berechtigungen-v1200) | — |
| Kapitel 35: v1.20.0-Bugfixes (Post-Initial-Release) | [→](#kapitel-35-v1200-bugfixes-post-initial-release) | — |
| Kapitel 36: Universal-Filter-Rollout (v1.21.0) | [→](#kapitel-36-universal-filter-rollout-v1210) | — |
| Kapitel 37: FA-Abschliessen (v1.21.1) | [→](#kapitel-37-fa-abschliessen-v1211) | — |
| Kapitel 38: FA-Vorbau (v1.22.0) | [→](#kapitel-38-fa-vorbau-v1220) | — |
| Kapitel 39: UI-Nachzuegler (v1.22.0 Folge-Fixes) | [→](#kapitel-39-ui-nachzuegler-v1220-folge-fixes) | — |
| Kapitel 40: Windows-Authentifizierung & AD-Benutzer (v1.23.0) | [→](#kapitel-40-windows-authentifizierung--ad-benutzer-v1230) | TS-40.1 – TS-40.12 |
| Kapitel 41: Rolle „Lagerbestellung" + Artikelinfo für Stammdaten-ansehen (v1.23.0) | [→](#kapitel-41-rolle-lagerbestellung--artikelinfo-für-stammdaten-ansehen-v1230) | TS-41.1 – TS-41.5 |
| Kapitel 42: Lagerbestellungs-Druck spiegelt GUI (Spalten/Sort/Filter) (v1.23.0) | [→](#kapitel-42-lagerbestellungs-druck-spiegelt-gui-spaltensortfilter-v1230) | TS-42.1 – TS-42.5 |
| Kapitel 43: FA-Abarbeitungsliste — Komma-Werkbank-Filter + Bezeichnung (v1.23.0) | [→](#kapitel-43-fa-abarbeitungsliste--komma-werkbank-filter--bezeichnung-v1230) | TS-43.1 – TS-43.5 (TS-43.5 v1.30.0 Teil 8: personalisierter Default-Filter Bezeichnung 1) |
| Kapitel 44: FA-AG-Erkennung + BOM-Cache — Protokoll-Aufgliederung (v1.23.0) | [→](#kapitel-44-fa-ag-erkennung--bom-cache--protokoll-aufgliederung-v1230) | TS-44.1 – TS-44.3 |
| Kapitel 45: FA-Vorbau 3-Wert-Status + Beschichtungstermin + ENTER-Spaltenfilter (v1.24.0) | [→](#kapitel-45-fa-vorbau-3-wert-status--beschichtungstermin--enter-spaltenfilter-v1240) | TS-45.1 – TS-45.6 |
| Kapitel 46: Glas-Bestellung (Bestelltyp Lager/Glas) (v1.25.0) | [→](#kapitel-46-glas-bestellung-bestelltyp-lagerglas-v1250) | TS-46.1 – TS-46.14 (TS-46.9 – TS-46.14 v1.30.0 Teil 7: Kommentar + DUMMY-Artikel, TS-46.14 UAT-Fix-Regression) |
| Kapitel 47: Service-Resilienz + Fehlermail + ProductionOrders-515-Fix (v1.25.0) | [→](#kapitel-47-service-resilienz--fehlermail--productionorders-515-fix-v1250) | TS-47.1 – TS-47.3 |
| Kapitel 48: Rolle `stock_read` (read-only Lagerbestand) + FA-Liste/Stückliste fuer Vorbau (v1.25.0) | [→](#kapitel-48-rolle-stockread-read-only-lagerbestand--fa-listestückliste-fuer-vorbau-v1250) | TS-48.1 – TS-48.2 |
| Kapitel 49: Hauptlagerplatz am Artikel (v1.25.0) | [→](#kapitel-49-hauptlagerplatz-am-artikel-v1250) | — |
| Kapitel 50: FA-Reconciliation (verwaiste FAs stornieren) (v1.25.0) | [→](#kapitel-50-fa-reconciliation-verwaiste-fas-stornieren-v1250) | — |
| Kapitel 51: Typisierte, vollständige Service-Einstellungen (v1.25.0) | [→](#kapitel-51-typisierte-vollständige-service-einstellungen-v1250) | TS-51.1 – TS-51.8 |
| Kapitel 52: Lagerbestellung aus der Stückliste + Master-Schalter (v1.25.0) | [→](#kapitel-52-lagerbestellung-aus-der-stückliste--master-schalter-v1250) | — |
| Kapitel 53: Lagerbestand-Nullsetzen verwaister Paare (v1.25.0) | [→](#kapitel-53-lagerbestand-nullsetzen-verwaister-paare-v1250) | — |
| Kapitel 54: Aktivitäts-Protokoll-Bereinigung (v1.25.0) | [→](#kapitel-54-aktivitäts-protokoll-bereinigung-v1250) | TS-54.1 – TS-54.4 |
| Kapitel 55: FA-Zusatzinfos (Sage) (v1.26.0) | [→](#kapitel-55-fa-zusatzinfos-sage-v1260) | TS-55.1 – TS-55.13 |

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
- *(seit v1.54.0)* Ein **Admin** findet trotz `BdeAktiv = false` unter Stammdaten den Eintrag „Arbeitsgänge“; die Seite
  „BDE Stammdaten“ zeigt dann nur den Reiter „Arbeitsgänge“ (TS-87.1).

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
- Link "VAULT" oeffnet Artikel im VAULT-System (neuer Tab) — nur bei eingeschalteten Vault-Links (`VaultLinksAktiv`, Standard an,
  ab v1.67.0; ausgeschaltet fehlt der Link, siehe TS-100.2).

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

### TS-2.22 — FA-Hinweis nur bei tatsaechlichem Bestand (v1.29.0, Teil 1)

**Vorbedingungen:**
- Rolle mit Stock-Zugriff (z. B. `stock`).
- Artikel A wurde mit FA-Tag `1234567` auf Lagerplatz X eingebucht (Menge 5) und anschliessend
  **ohne** FA-Tag wieder vollstaendig ausgebucht (Menge 5) → Ist-Bestand am Platz = 0.

**Schritte:**
1. Einbuchung oeffnen.
2. FA `1234567` in das FA-Feld eingeben → Tab (bzw. Feld verlassen).

**Erwartetes Verhalten:**
- **Kein** „FA liegt bereits …"-Hinweis (`faStorageHint` bleibt ausgeblendet), da der tatsaechliche
  Bestand 0 ist. Frueher (Bug) erschien hier faelschlich der Platz X mit Menge 5.

**Negativfall (realer Restbestand):**
- Wurde nur teilweise ausgebucht (z. B. Einbuchung 5, ungetaggte Ausbuchung 2 → Rest 3), erscheint der
  Hinweis **weiterhin**, aber mit der **realen** Restmenge (3), nicht der urspruenglichen Einbuchmenge (5).

**Gegenprobe Bestandsuebersicht (bewusst unveraendert):**
- `Lager → Bestaende`, Filter „Fertigungsauftrag" = `1234567`: die Zeile fuer Artikel A/Lagerplatz X
  wird **weiterhin** angezeigt (historische FA-Zuordnung, `onlyActualStock=false`). Das ist der
  erwartete Regressions-Nachweis, **kein** Fehler.

**Gegenprobe Tracking-Modal:**
- OSEON-Teileverfolgung, Lagerbestand-Modal fuer FA `1234567`: verhaelt sich wie der Einbuchungs-Hinweis
  (keine Zeile bei Ist-Bestand 0).

---

### TS-2.23 — Mehrfach-Einbuchung Happy Path (v1.29.0, Teil 2)

**Vorbedingungen:**
- Rolle mit `RequireStockAccess` (z. B. `stock`), mind. 2 existierende Artikel, ein buchbarer Lagerplatz.

**Schritte:**
1. Menue `Lager → Mehrfach-Einbuchung` oeffnen.
2. Lagerplatz + FA-Nummer einmalig setzen.
3. 3 Artikel-Zeilen mit unterschiedlichen Mengen erfassen („+ Zeile hinzufuegen" fuer weitere Zeilen).
4. „Einbuchung speichern".

**Erwartetes Verhalten:**
- 3 neue Eintraege in der Bewegungshistorie (Typ Einbuchung), alle mit **demselben** Lagerplatz/derselben
  FA-Nummer, jeweils korrekter Artikel/Menge.
- Erfolgsmeldung „**3** Artikel erfolgreich eingebucht.".
- Neue Zeilen sind mit Menge **1** vorbelegt.

---

### TS-2.24 — Mehrfach-Scan zaehlt hoch + keine Teilbuchung (v1.29.0, Teil 2)

**Vorbedingungen:** wie TS-2.23.

**Schritte (Mehrfach-Scan, B1):**
1. Mehrfach-Einbuchung oeffnen, Lagerplatz + FA setzen.
2. „Artikel scannen" und denselben Artikel dreimal nacheinander scannen (jeweils Button erneut druecken,
   Schalter „neue Zeile erzwingen" **aus**).

**Erwartetes Verhalten:**
- **Eine** Zeile mit Menge **3** (nicht drei Zeilen à 1). Nach Absenden ein `StockMovement` mit Menge 3.
- **Variante:** Schalter „neue Zeile erzwingen" **an** → drei Zeilen à Menge 1.

**Schritte (keine Teilbuchung, S2):**
1. 3 Zeilen erfassen, eine davon **ohne Artikelauswahl** oder mit **Menge 0**.
2. „Einbuchung speichern".

**Erwartetes Verhalten:**
- **Nichts** wird gebucht (Bewegungshistorie unveraendert). Das Formular kommt mit **allen 3** Zeilen +
  Kopf-Werten zurueck, die fehlerhafte Zeile ist **rot markiert** mit Fehlermeldung.
- Nach Korrektur + erneutem Absenden werden alle 3 gebucht.

**Sage-Regression (S1):**
- Vorbedingung: Lagerplatz mit `SageBuchungErlaubt = true` + globaler Sage-Toggle aktiv.
- Bulk-Einbuchung mit 2 Zeilen auf diesem Lagerplatz absenden → unter `Lager → Sage-Lagerbuchungen`
  entstehen **2** Queue-Eintraege (einer je Zeile), identisch zur Einzel-Einbuchung.

---

### TS-2.25 — WA-Scan-Button in der Bewegungshistorie kuerzt Suffix (v1.29.0, Teil 3)

**Vorbedingungen:**
- Bewegungshistorie mit Buchungen zu FA `2610063`.

**Schritte:**
1. `Lager → Bewegungshistorie` oeffnen.
2. Scan-Button **neben dem Filterfeld „Fertigungsauftrag"** klicken, Barcode mit Wert `2610063-1`
   scannen (bzw. Testbild verwenden).
3. „Filtern" klicken.

**Erwartetes Verhalten:**
- Filterfeld zeigt `2610063` (Suffix ab erstem Trennzeichen entfernt). Liste zeigt alle Bewegungen zu FA
  `2610063`.
- Kein automatisches Neu-Laden beim Scan — erst „Filtern" (bzw. Enter) laedt die Liste.

**Randfaelle (jeweils gescannt → Filterwert):**
- `2610063_02` → `2610063` (Unterstrich).
- `2610063-1-2` → `2610063` (nur bis zum **ersten** Trennzeichen).
- `2610063-A` → `2610063` (nicht-numerischer Suffix).
- `2610063` → `2610063` (kein Suffix, unveraendert).
- `26100631` (8-stellig, kein Trennzeichen) → `26100631` (**keine** Laengenkuerzung).
- `26100` (kuerzer als 7, kein Trennzeichen) → `26100` (unveraendert, keine Exception).

**Negativfall (nur beim Scannen):**
- Manuelles Eintippen/Einfuegen von `2610063-1` ins Filterfeld → Feld behaelt `2610063-1` (die Kuerzung
  greift **nur** ueber den Scan-Callback, nicht bei manueller Eingabe).

---

### TS-2.26 — Bewegungshistorie: Spaltenfilter Bewegungsart + Datum funktionieren (v1.30.0, Teil 4)

**Vorbedingungen:**
- Benutzer hat Rolle `stock` oder `admin`.
- Bewegungshistorie mit gemischten Bewegungsarten (mind. je eine Ein- und eine Ausbuchung) und
  Buchungen an verschiedenen Tagen/Jahren.

**Schritte + erwartetes Verhalten:**
1. `/StockMovements/Index` oeffnen. **Erwartet:** Die Spaltenkoepfe „Datum/Zeit" und „Bewegungsart"
   haben ein Filter-Eingabefeld; „Menge" hat **keines** (dafuer ist kein sinnvoller Text-Filter
   moeglich). „Artikel"/„Lagerplatz"/„Benutzer"/„Fertigungsauftrag" wie bisher.
2. Im Spaltenfilter **„Bewegungsart"** `ausbuchung` eingeben. **Erwartet:** Es erscheinen **nur**
   Ausbuchungen **und** Sage-Ausbuchungen (Contains-Semantik) — **keine** Einbuchungen. (Frueher
   kam die ganze Liste — das war der gemeldete Bug.)
3. Im Spaltenfilter **„Bewegungsart"** `einbuchung` eingeben. **Erwartet:** nur Einbuchungen +
   Sage-Einbuchungen.
4. Im Spaltenfilter **„Datum/Zeit"** ein volles Datum (z. B. `06.08.2026`) eingeben. **Erwartet:**
   nur Bewegungen dieses Tages — **ueber alle Seiten** (Server-Filter, nicht nur die aktuelle Seite).
5. „Datum/Zeit" mit einem Monat (`08.2026`) bzw. Jahr (`2026`) filtern. **Erwartet:** nur
   Bewegungen des Monats bzw. Jahres.

**Negativfaelle:**
- Bewegungsart-Filter mit einem nicht existierenden Wert (`xyz`) → **leere** Liste (nicht die
  Vollmenge).
- Datum-Filter mit unsinnigem Wert (`abc`) → leere Liste.
- Der `!`-Praefix negiert weiterhin (z. B. `!einbuchung` blendet Ein-/Sage-Einbuchungen aus).

---

### TS-2.27 — Bewegungsart-Filter (Filterkarten-Dropdown) filtert exakt

**Vorbedingungen:** wie TS-2.26.

**Schritte:**
1. In der Filterkarte Dropdown "Bewegungsart" = "Ausbuchung" waehlen, filtern.
2. Dropdown "Bewegungsart" = "Einbuchung" waehlen, filtern.

**Erwartetes Verhalten:**
- Schritt 1: es werden **ausschliesslich** Ausbuchungs-Zeilen angezeigt; **keine** Einbuchung
  erscheint.
- Schritt 2: es werden ausschliesslich Einbuchungen angezeigt.

---

### TS-2.28 — Regression: verbleibende Spaltenfilter weiterhin wirksam

**Vorbedingungen:** wie TS-2.26.

**Schritte:**
1. Spaltenfilter "Artikel" mit einer bekannten Artikelnummer setzen.
2. Analog Spaltenfilter "Lagerplatz"/"Benutzer"/"Fertigungsauftrag" setzen.

**Erwartetes Verhalten:**
- Jeweils nur Zeilen, die zum gesetzten Filterwert passen.

---

### TS-2.29 — Einbuchung oeffnet mit Standardmenge 1 (v1.30.0, Teil 5)

**Vorbedingungen:**
- Benutzer hat Rolle `stock` oder `admin`.

**Schritte:**
1. `/StockMovements/Inbound` oeffnen.

**Erwartetes Verhalten:**
- Mengenfeld zeigt `1` (nicht `0`).

**Negativfall:**
- Menge auf `0` setzen und absenden → weiterhin Validierungsfehler "Menge muss groesser als 0
  sein".

---

### TS-2.30 — Ausbuchung/Umbuchung NICHT betroffen

**Vorbedingungen:** wie TS-2.29.

**Schritte:**
1. `/StockMovements/Outbound` oeffnen.
2. Umbuchung oeffnen.

**Erwartetes Verhalten:**
- In beiden Faellen erscheint **kein** `1`-Default im Mengenfeld — Verhalten bleibt wie bisher
  (Feld zeigt `0`).

---

### TS-2.31 — Validierungsfehler bei der Einbuchung behaelt getippten Wert

**Vorbedingungen:** wie TS-2.29.

**Schritte:**
1. Einbuchung oeffnen (Mengenfeld zeigt `1`).
2. Menge auf `5` aendern.
3. Ein anderes Pflichtfeld (z. B. Artikel) leer lassen und absenden, sodass ein
   Validierungsfehler entsteht.

**Erwartetes Verhalten:**
- Nach dem Rerender steht weiterhin `5` im Mengenfeld — **nicht** zurueckgesetzt auf `1`.

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
- ServiceSetting `Sync:EnaioDmsEnabled = true` (Service-Einstellungen).
- `EnaioLinksAktiv` an (Standard, ab v1.67.0) und `EnaioLinkVorlage` gefüllt (Standorteinstellungen → „enaio (Dokumente)“).
- FA `FA-2604002` hat ein Dokument in enaio.

**Schritte:**
1. FA-Liste oeffnen.
2. Zeile von `FA-2604002` finden.
3. Orange Dokument-Icon neben der FA-Nummer anklicken.

**Erwartetes Verhalten:**
- Dokument oeffnet sich im enaio Viewer (neuer Browser-Tab).

---

### TS-4.5a — enaio "Werkstattauftrag + Zeichnung": eigenes Icon, zuerst angezeigt

**Vorbedingungen:**
- ServiceSetting `Sync:EnaioDmsEnabled = true` (Service-Einstellungen).
- `EnaioLinksAktiv` an (Standard, ab v1.67.0) und `EnaioLinkVorlage` gefüllt.
- Eine FA (z. B. `FA-2604002`) hat in enaio mehrere Dokumente: einen `Werkstattauftrag+Zeichnung` (Zeichnung direkt am WA), zusaetzlich einen reinen `Werkstattauftrag` und eine `Zeichnung`.

**Schritte:**
1. FA-Liste oeffnen (alternativ Leitstand, FA-Abarbeitungsliste oder FA-Vervollstaendigen — gleiche Badge-Liste).
2. Zeile der FA finden und die Icon-Reihe neben der FA-Nummer betrachten.

**Erwartetes Verhalten:**
- Die drei Typen haben drei unterscheidbare Icons:
  - `Werkstattauftrag+Zeichnung` = Dokument-mit-Zeichenbrett-Icon (Tooltip "Werkstattauftrag + Zeichnung in enaio oeffnen").
  - `Werkstattauftrag` = Dokument-Icon.
  - `Zeichnung` = Bild-/Zeichnung-Icon.
- Reihenfolge der Icons: **`Werkstattauftrag+Zeichnung` zuerst**, dann `Werkstattauftrag`, dann `Zeichnung`.
- Jedes Icon oeffnet beim Klick das zugehoerige Dokument im enaio Viewer (neuer Tab).

**Negativ-Fall:**
- FA ohne `Werkstattauftrag+Zeichnung`-Dokument: nur die bisherigen WA-/Zeichnungs-Icons erscheinen, keine Luecke.

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

### TS-5.10 — Gespeicherter BOM-Filter „Bezeichnung 1" (v1.30.0, Teil 8)

**Vorbedingungen:**
- Benutzer mit Zugriff auf die Stueckliste; mind. ein FA mit BOM-Positionen, deren „Bezeichnung 1"
  unterschiedliche Werte hat (z. B. „Verdampfer", „Kondensator").

**Schritte + erwartetes Verhalten:**
1. Im Profil (`/Account/Profile`) unter „Standard-Filter Bezeichnung 1 (Stückliste)" `Verdampfer`
   eintragen, speichern.
2. Eine Stueckliste (BOM) oeffnen. **Erwartet:** Die Spalte „Bezeichnung 1" ist automatisch auf
   `Verdampfer` vorgefiltert (nur passende Positionen sichtbar), der Wert steht **sichtbar** im
   Spaltenfilter-Feld — genau wie der bestehende Artikelgruppen-Filter.
3. Den Filter „Bezeichnung 1" leeren. **Erwartet:** wieder alle Positionen sichtbar.
4. Ein Admin kann denselben Wert fuer einen anderen Benutzer unter `/Users/Edit/{id}` pflegen.

**Negativfall:**
- Leeres Profilfeld → beim Oeffnen der Stueckliste ist „Bezeichnung 1" **nicht** vorbelegt
  (unveraendertes Verhalten). Der FA-Abarbeitungslisten-Filter (Teil 8, TS-43.5) bleibt davon
  unberuehrt — die beiden Default-Filter sind getrennt.

### TS-5.11 — Stueckliste mit vielen Positionen drucken (v1.30.1, Hotfix HTTP 404.15)

**Hintergrund:** Bis v1.30.0 ging die Liste aller sichtbaren Positionen im Adressteil der Druck-URL mit;
bei grossen Stuecklisten brach der Druck mit „HTTP-Fehler 404.15 — Abfragezeichenfolge zu lang" ab.
Seit v1.30.1 wird per Formular (POST) gedruckt.

**Vorbedingungen:** FA mit einer Stueckliste mit **mindestens 300 Positionen** (z. B. das FA aus dem
Befund vom 2026-09-28, ID 3664 auf akenet01); Zugriff Kommissionierung, Vorbau oder FA-Vervollstaendigung.

**Schritte + erwartetes Verhalten:**
1. Stueckliste oeffnen, **keinen** Filter setzen, „Stueckliste drucken". **Erwartet:** neues Tab mit der
   **vollstaendigen** Stueckliste, kein 404.15, keine Fehlerseite, kein Popup-Blocker-Hinweis.
2. Einen Spaltenfilter setzen, der nur wenige Positionen uebrig laesst, erneut drucken. **Erwartet:** nur
   die sichtbaren Positionen, Filter-Hinweis im Kopf, Spaltenauswahl wie am Bildschirm (Regression TS-5.9).
3. Einen Filter setzen, der **nur wenige** der vielen Positionen ausblendet (z. B. eine Artikelgruppe
   ausschliessen), drucken. **Erwartet:** Druck klappt trotz langer Positionsliste (das war der Fall,
   den eine reine „alles sichtbar = keine Liste"-Loesung nicht abgedeckt haette).
4. Dasselbe in der **read-only** Stueckliste der FA-Abarbeitungsliste (Vorbau). **Erwartet:** wie 1.

**Negativfaelle:**
- `/Picking/PrintBom/{id}` direkt in die Adresszeile eingeben (GET, auch mit alten Parametern
  `?visiblePositions=…`). **Erwartet:** vollstaendiger Druck **aller** Positionen, kein Fehler — ein
  GET-Aufruf filtert nicht mehr.
- Popup-Blocker des Browsers aktiv (Standard in Edge/Chrome/Firefox). **Erwartet:** Druck-Tab oeffnet
  trotzdem (Formular-Submit direkt aus dem Klick).

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

### TS-7.9 — Nur fremdsystem-relevante AGs anzeigen

**Vorbedingungen:**
- Arbeitsgang-Stammdaten (Stammdaten → Arbeitsgänge): nicht alle AGs sind als „Fremdsystem-relevant“ markiert.

**Schritte:**
1. OSEON-Liste oeffnen, Gruppe aufklappen bis Arbeitsgaenge sichtbar.
2. Toggle "Fremdsystem-Relevanz" ist aktiv (Standard; bis v1.53.0 „OSEON-Relevanz“).
3. Toggle deaktivieren.

**Erwartetes Verhalten:**
- Deaktiviert: alle Arbeitsgaenge sichtbar (auch nicht-relevante).
- Aktiviert: nur als fremdsystem-relevant markierte AGs sichtbar.

---

### TS-7.10 — Arbeitsgang-Stammdaten pflegen (Offset zum Bezugstermin)

**Vorbedingungen:**
- Benutzer hat Rolle `admin` (der Code liess nie `masterdata` zu; korrigiert v1.54.0).

**Schritte:**
1. Stammdaten → Arbeitsgänge (BDE-Stammdaten, Reiter „Arbeitsgänge“) oeffnen.
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

### TS-9.6 — Werkbank-Override ueberschreibt globale Vorkommissioniertage (FA-Liste)

**Vorbedingungen:**
- Globales AppSetting `VorkommissionierTage = 1`, `KommissionierTage = 4`.
- Werkbank `WB-OVERRIDE` hat „Abweichende Vorkommissioniertage" = `3`
  (Stammdaten → Werkbaenke → Bearbeiten).
- Werkbank `WB-STANDARD` hat das Feld **leer** („Standard" in der Werkbank-Liste).
- FA `FA-TEST-01` ist der Werkbank `WB-OVERRIDE` zugeordnet und hat einen Fertigungstermin
  an einem bekannten Werktag (z. B. Montag).
- FA `FA-TEST-02` ist `WB-STANDARD` zugeordnet, gleicher Fertigungstermin.

**Schritte:**
1. FA-Liste (Fertigungsauftraege → Uebersicht) oeffnen.
2. Spalte „BG-Termin" fuer `FA-TEST-01` und `FA-TEST-02` ablesen.

**Erwartetes Verhalten:**
- `FA-TEST-01`: BG-Termin = Kommissioniertermin **minus 3 Arbeitstage** (nicht minus 1).
- `FA-TEST-02`: BG-Termin = Kommissioniertermin **minus 1 Arbeitstag** (globaler Standard,
  unveraendert gegenueber v1.26.0).
- Wochenenden und Feiertage werden wie bisher uebersprungen (Arbeitstage, keine Kalendertage).

**Negativfall:**
- FA **ohne** Werkbank-Zuordnung verhaelt sich wie `FA-TEST-02` (globaler Wert), keine
  Fehlermeldung, kein leerer BG-Termin.

---

### TS-9.7 — Override konsistent ueber FA-Liste, Leitstand und FA-Abarbeitungsliste je AG

**Vorbedingungen:**
- Wie TS-9.6; `FA-TEST-01` hat zusaetzlich einen aktiven FA-Vorbau-Arbeitsgang (z. B. `VE`),
  Feature `FaCompletionAktiv = true`.

**Schritte:**
1. `FA-TEST-01` in der FA-Liste oeffnen und BG-Termin notieren.
2. Denselben FA im **Leitstand** suchen, BG-Termin ablesen.
3. **FA-Abarbeitungsliste je Arbeitsgang** fuer den AG `VE` oeffnen, BG-Termin ablesen.

**Erwartetes Verhalten:**
- Alle drei Listen zeigen **exakt denselben** BG-Termin (Kommissioniertermin minus 3 Arbeitstage).

**Negativfall:**
- Wird der Override an der Werkbank wieder geleert, zeigen alle drei Listen wieder den
  globalen Wert — ohne App-Neustart (Wert wird pro Request gelesen).

---

### TS-9.8 — Override `0` wird als expliziter Wert behandelt, nicht als „kein Override"

**Vorbedingungen:**
- Werkbank `WB-ZERO` hat „Abweichende Vorkommissioniertage" = `0` (gespeichert, nicht leer).
- FA `FA-TEST-03` ist `WB-ZERO` zugeordnet, Fertigungstermin gesetzt.

**Schritte:**
1. FA-Liste oeffnen, Spalten „Komm." und „BG-Termin" fuer `FA-TEST-03` vergleichen.

**Erwartetes Verhalten:**
- BG-Termin **gleich** Kommissioniertermin (0 Arbeitstage Vorlauf).
- In der Werkbank-Liste steht bei `WB-ZERO` „0 Tage" (nicht „Standard").

**Negativfall:**
- Werkbank mit **leerem** Feld (`null`) zeigt in der Werkbank-Liste „Standard" und verhaelt
  sich in den FA-Listen weiter wie der globale Wert — `0` und leer sind **nicht** dasselbe.

---

### TS-9.9 — Override wirkt kaskadierend auf den Beschichtungstermin

**Vorbedingungen:**
- Beschichtungs-Feature aktiv (`LackierteilKategorieName` gesetzt), `BeschichtungTage = 10`,
  `BeschichtungAbholtage = Dienstag,Donnerstag`.
- `FA-TEST-01` (Werkbank `WB-OVERRIDE`, Override 3) hat Lackierteile (`Lack-T`-Kennzeichen gesetzt).
- Vergleichs-FA `FA-TEST-02` (Werkbank `WB-STANDARD`) mit gleichem Fertigungstermin und
  ebenfalls Lackierteilen.

**Schritte:**
1. Spalte „Beschicht." fuer beide FAs in der FA-Liste bzw. im Leitstand ablesen.

**Erwartetes Verhalten:**
- Der Beschichtungstermin von `FA-TEST-01` liegt gegenueber `FA-TEST-02` um die Differenz
  Override ./. globaler Wert (hier 3 − 1 = 2 Arbeitstage) **frueher**, danach wie bisher auf den
  vorherigen Abholtag zurueckgezogen.

**Negativfall:**
- FA **ohne** Lackierteile bei aktivem Feature hat weiterhin **keinen** Beschichtungstermin —
  der Override aendert daran nichts.

---

### TS-9.10 — UI-Rueckmeldung: Badge/Tooltip an der Werkbank-Zelle

**Vorbedingungen:**
- Wie TS-9.6.

**Schritte:**
1. FA-Liste, Leitstand und FA-Abarbeitungsliste je AG oeffnen.
2. Werkbank-Zelle von `FA-TEST-01` ansehen und mit der Maus darauf zeigen.
3. Mit der Maus auf den Spaltenkopf „BG-Termin" zeigen.

**Erwartetes Verhalten:**
- In **allen drei** Listen steht neben dem Werkbank-Namen ein Badge „BG 3".
- Der Tooltip des Badges lautet „Abweichende Vorkommissioniertage dieser Werkbank: 3
  Arbeitstag(e) statt Standard 1".
- Der Spaltenkopf-Tooltip „BG-Termin" nennt den Standard und weist darauf hin, dass er je
  Werkbank abweichen kann.

**Negativfall:**
- `FA-TEST-02` (kein Override) zeigt **kein** Badge und keinen zusaetzlichen Tooltip —
  die Zelle sieht aus wie vor v1.27.0.
- Der Spaltenfilter „Werkbank" filtert weiterhin nur nach dem Werkbank-Namen; das Badge
  beeinflusst Filter und Sortierung nicht.

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
**Vorbedingungen:** AG-Name "FOO" in OSEON-Auftrag, KEIN Eintrag in den Arbeitsgang-Stammdaten (`dbo.OperationConfigs`).
**Schritte:**
1. Reporting öffnen.

**Erwartet:** Gelber Banner "X AG(s) ohne Config-Eintrag"; dessen Link fuehrt zu BDE-Stammdaten › Arbeitsgänge.

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

### TS-18.5a — Submit-Mail: Link klickbar in Outlook (Plain-Text-Alternative)
Vorbedingungen: wie TS-18.5, Empfaenger nutzt Outlook (Desktop oder Web).
Schritte: Submit-Mail oeffnen, auf "Lagerbestellung oeffnen" tippen/klicken.
Erwartet: Der Link wird als klickbarer Button bzw. Hyperlink dargestellt (NICHT als Rohtext `[URL]Lagerbestellung oeffnen`). Klick oeffnet `/WarehousePicking/Details/{id}`. Hintergrund: die Mail ist multipart/alternative (HTML-Button + Plain-Text-Teil mit nackter URL). Negativ-Fall vor dem Fix: Outlook zeigte den Anchor als `[URL]Text`-Rohtext.

### TS-18.6 — Storno-Mail
Schritte: nach Submit (TS-18.1) im Erfasser-Edit "Stornieren" mit Grund.
Erwartet: Liste Status "Storniert". Nach Worker-Tick zweite Mail mit Subject "[STORNO] …". Auch hier multipart/alternative (HTML + Plain-Text mit Kopf + Grund).

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

### TS-18.10 — Ist-Menge muss bestaetigt werden: keine Vorbefuellung, Pflichtpruefung, Autosave (v1.30.1)

**Grundsatz:** Nicht Geliefertes muss **bestaetigt** werden — durch eine eingetippte Ist-Menge (auch `0`)
oder eine Fehlteil-Markierung („Fehlteil" / „Wird nicht nachgeliefert"). Nichts wird angenommen, ein
leeres Ist-Feld wird nie still als 0 gebucht.

**Vorbedingungen:** Lagerbestellung im Status **Offen/Submitted** mit mindestens drei Positionen, keine
davon bearbeitet. Nach dem Deploy auf einem System mit Altbestand: Einmal-Skript
`SQL/Einmalig/2026-09-28_Hotfix-1.30.1_ResetAutosaveZeroQuantityPicked.sql` wurde **genau einmal**
ausgefuehrt (siehe Deploy).

**Schritte + erwartetes Verhalten:**
1. `WarehousePicking/Details` oeffnen. **Erwartet:** Die Ist-Felder sind **leer** und zeigen **keine**
   graue Bestellt-Zahl mehr; die bestellte Menge steht nur in der Spalte „Bestellt".
2. Pos 1: Ist = `5` eintragen; danach in eine Notiz klicken und wieder hinaus (loest den Autosave aus),
   Seite neu laden. **Erwartet:** Pos 1 zeigt `5`, Pos 2 und 3 sind **leer — nicht `0`** (Autosave-Fix).
3. „Speichern + Abschliessen" klicken, Pos 2/3 leer lassen. **Erwartet:** Es gibt **keinen** Dialog „Soll =
   Ist-Menge buchen?" mehr. Stattdessen erscheint oben ein gelber Hinweis „Noch nicht abgeschlossen. Bitte je
   Position die Ist-Menge eintragen (auch 0) oder Fehlteil markieren: Pos 2 (…), Pos 3 (…)", die Ist-Felder
   von Pos 2/3 sind rot umrandet, der Cursor steht in Pos 2. Die Bestellung bleibt offen.
4. Pos 2: Ist = `0` tippen. **Erwartet:** Pos 2 verliert die Markierung, der Hinweis nennt nur noch Pos 3.
5. Pos 3: Ist leer lassen, aber „Fehlteil" anklicken. **Erwartet:** Hinweis verschwindet.
6. „Speichern + Abschliessen". **Erwartet:** Bestellung wird abgeschlossen; Pos 2 ist mit `0` gebucht,
   Pos 3 mit leerer Ist-Menge und Fehlteil-Markierung (Status Teilgeliefert, weil Restlieferung erwartet).
7. Neue offene Bestellung: „Drucken und Abschliessen" mit einer leeren Position ohne Fehlteil.
   **Erwartet:** gleicher gelber Hinweis, **kein** Druck-Tab bleibt offen, die Eingaben bleiben erhalten.

**Negativfaelle:**
- **Ohne JavaScript / direkter POST** (z. B. Browser-Entwicklerwerkzeuge: das Formular per Konsole
  `document.getElementById('detailsForm').submit()` mit leerer Position absenden). **Erwartet:** Der Server
  lehnt ab — Rueckkehr zur Detailansicht mit gelber Meldung, die die Positionen nennt; nichts gebucht.
  (Automatisiert: `WarehousePickingControllerTests.Close_EmptyQuantityWithoutShortage_IsBlockedServerSide_NothingBooked`,
  `…PrintAndClose_…_ReturnsBadRequest`.)
- **Negative Menge** eintippen und abschliessen. **Erwartet:** wie bisher „Ist-Mengen duerfen nicht negativ sein."
- **Dezimalwert** (`1,5` bzw. `1.5`) in ein Ist-Feld tippen und abschliessen. **Erwartet:** Das Feld wird
  markiert, der Hinweis verlangt ganze Zahlen; ohne JavaScript lehnt der Server ab („Ist-Mengen muessen ganze
  Zahlen ab 0 sein“) und bucht **nichts** — vorher rutschten dabei alle folgenden Mengen eine Zeile nach oben.
- **Server lehnt „Speichern + Abschliessen“ ab** (z. B. leere Zeile ohne JavaScript). **Erwartet:** Die bis
  dahin eingetragenen Mengen, Notizen und Fehlteil-Markierungen sind nach der Rueckkehr noch da (Zwischenstand
  gesichert), leere Felder bleiben leer.
- **Einmal-Skript (Deploy):** Vor dem Lauf die Zaehl-Abfrage im Skript notieren; nach dem Lauf: offene
  (Submitted) Bestellungen, deren Positionen vorher `0` zeigten **ohne** Fehlteil, zeigen jetzt leere
  Ist-Felder; Positionen mit Fehlteil-Markierung und alle teilgelieferten/abgeschlossenen Bestellungen sind
  **unveraendert**. Das Skript wird **nicht** ein zweites Mal ausgefuehrt. Direkt danach alle offenen Lagerbestellungs-Seiten **neu laden** (eine vorher geladene Seite traegt sonst die alte 0 noch im Feld und schickt sie als bestaetigte 0 zurueck).

**Zusatz v1.47.5 — Zahnrad (Spaltenauswahl) sichtbar, auch nach dem Pflicht-Hinweis (Manual-UAT):**
1. Lagerbestellung im Status Offen öffnen. **Erwartet:** Zahnrad-Button rechtsbündig direkt **oberhalb** der
   Tabelle sichtbar; Klick öffnet die Spaltenauswahl (Offcanvas rechts), eine Spalte ein-/ausblenden funktioniert.
2. Eine Position leer lassen und „Abschliessen" klicken → gelber Pflicht-Hinweis erscheint. **Erwartet:** Der
   Hinweis steht **über** der Zahnrad-Zeile; das Zahnrad ist weiter sichtbar und öffnet die Spaltenauswahl
   (vorher: Zahnrad unsichtbar im versteckten Hinweis bzw. beim Einblenden gelöscht).
3. Leere Position ergänzen (Hinweis wird nachgeführt) und F5. **Erwartet:** Zahnrad sichtbar.
- **Negativfall:** Zahnrad fehlt, erscheint im gelben Hinweis oder verschwindet nach Schritt 2 → Regression.
  Automatisiert nur die Platzierung im Quelltext (`WarehousePickingDetailsZahnradHostTests`, seit v1.65.0 `TableToolbarHostTests`, TS-98).

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
Schritte: PickingLeitstand &rarr; VK/VL/...-Status-Auswahlfeld aendern.
Erwartet: Aenderung funktioniert weiterhin. Seit v1.22.0-Followup setzen die VK-VA-Auswahlfelder den Erledigt-Status; seit v1.24.0 als 3-Wert (Offen/in Bearbeitung/Fertig) ueber `/api/fa-work-steps/set-status` (blockt Picker nicht). Details siehe Szenario 38.9.

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
Service-Filter "EnaioDms". Quelle ab v1.67.0 aus `Sync:EnaioDmsViewName` (Standard `[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]`);
ungültiger Name: genau ein Eintrag „fehlgeschlagen — Ungültiger View-Name“ (TS-100.10).

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

## Kapitel 30: OSEON-Tracking iOS-Fix + Lazy-Load (v1.16.0)

### Szenario 30.1: Desktop Chrome — Initial-Load-Performance
**Schritt:**
1. `/Tracking/OseonIndex` ohne Filter oeffnen.
2. Devtools-Network-Tab: Hauptrequest abwarten.
**Erwartet:** Erster Paint < 1s, ~25 Group-Header-Zeilen sichtbar, alle initial collapsed.

### Szenario 30.2: Desktop Chrome — Lazy-Aufklappen
**Schritt:**
1. Klick auf Chevron einer Gruppe.
**Erwartet:** Spinner-Row erscheint, dann SubAuftrags-Zeilen einblenden < 500ms.
Zweites Click-Toggle: instant (kein AJAX im Network-Tab).

### Szenario 30.3: Desktop Chrome — Artikel-Filter Prefetch
**Schritt:**
1. URL: `/Tracking/OseonIndex?filterArticle=12345` (echte Treffer-Nummer).
**Erwartet:** Treffer-Gruppen sind initial expanded sichtbar mit prefetched SubAuftraegen. Kein zusaetzlicher AJAX-Call beim Aufklappen.

### Szenario 30.4: iOS Safari — Bedienbarkeit
**Schritt:**
1. Page auf aktuellem iOS Safari oeffnen (iPhone oder iPad, kein Desktop-Mode).
2. Artikel-Filter-Input antippen, "12345" eintippen, Filtern.
**Erwartet:** Scrolling fluessig, Tipp-Input ohne Verzoegerung, Form-Submit ohne Lock-up.

### Szenario 30.5: iOS Safari — QR-Code-Scan
**Schritt:**
1. Auf "Artikelnummer scannen"-Button tippen.
**Erwartet:** Permission-Prompt (Kamera-Zugriff) erscheint sofort. Bei Accept: Modal mit Kamera-Vorschau. Scan eines QR-Codes -> Filter-Input ist gefuellt, Form auto-submitted nach 50ms.

### Szenario 30.6: iOS Safari — QR-Fallback
**Schritt:**
1. In Safari-Settings den Kamera-Zugriff fuer die Seite explizit verweigern.
2. Page neu laden, QR-Button antippen.
**Erwartet:** Statt Kamera-Modal erscheint File-Upload-UI. User kann Bild auswaehlen, das wird gescannt.

### Szenario 30.7: Sub-Order-Status mit Relevanz-Filter
**Vorbedingung:** Es gibt einen Sub-Auftrag dessen OSEON-relevante AGs alle als Fertig (Status 90/95) gemeldet sind, aber dessen Sub-Order-Status selbst noch z.B. "In Arbeit" ist.
**Schritt:** Page laden mit OSEON-Relevanz-Filter aktiv (Checkbox haken).
**Erwartet:** Der Sub-Auftrag wird mit Status "Fertig" und gruener Ampel angezeigt. (Sub-Order hat keine relevanten offenen AGs mehr — Logik erkennt das.)

### Szenario 30.8: Sub-Order ohne OSEON-relevante AGs
**Vorbedingung:** Sub-Auftrag hat nur nicht-OSEON-relevante AGs (z.B. nur ZB + A-BT).
**Schritt:** Page laden mit OSEON-Relevanz-Filter aktiv.
**Erwartet:** Sub-Auftrag wird automatisch als "Fertig" angezeigt (es gibt nichts zu tracken).

---

## 31. Artikel-Sync-Erweiterung (v1.17.0)

Diese Szenarien decken die erweiterte Sage-Artikel-Synchronisation ab: UNION mit aktiven Bestellartikeln, Meldebestand-Uebernahme aus `KHKArtikelvarianten` und Full-Update bei bestehenden Artikeln.

### 31.1 Neuer Bestellartikel ohne BOM-Beteiligung kommt an

**Vorbedingungen:**
- Test-Artikel `TEST-9999` existiert in Sage `KHKArtikel` mit `IstBestellartikel = -1` und `Aktiv = -1`.
- `TEST-9999` taucht NICHT in `KHKPpsRessourcenPositionen` auf.
- `TEST-9999` ist im WMS noch nicht in der `Articles`-Tabelle.

**Schritte:**
1. Service-Lauf manuell triggern (Service-Restart oder warten auf naechsten Tick).
2. Aktivitaets-Protokoll (`/SyncLog/Index`) oeffnen, Filter Service = `Article`.
3. WMS-DB pruefen: `SELECT * FROM Articles WHERE ArticleNumber = 'TEST-9999'`.

**Erwartetes Verhalten:**
- Aktivitaets-Protokoll-Eintrag mit Counts inkl. `neu >= 1`.
- `Articles.TEST-9999` ist eingefuegt, `CreatedBy = 'IDEALAKEWMSService'`, `CreatedAt` ≈ jetzt.

### 31.2 Meldebestand-Uebernahme

**Vorbedingungen:**
- `TEST-9999` in `KHKArtikelvarianten.Meldebestand = 25.0000`.

**Schritte:**
1. Service-Sync triggern.
2. `SELECT ReorderLevel FROM Articles WHERE ArticleNumber = 'TEST-9999'`.

**Erwartetes Verhalten:**
- `ReorderLevel = 25.0000`.

### 31.3 Meldebestand 0 wird zu NULL

**Vorbedingungen:**
- `TEST-9998` in `KHKArtikel` (mit `IstBestellartikel = -1, Aktiv = -1`) und `KHKArtikelvarianten.Meldebestand = 0`.

**Schritte:**
1. Service-Sync triggern.
2. `SELECT ReorderLevel FROM Articles WHERE ArticleNumber = 'TEST-9998'`.

**Erwartetes Verhalten:**
- `ReorderLevel IS NULL` (nicht 0).

### 31.4 Full-Update bei bestehendem Artikel

**Vorbedingungen:**
- `TEST-9999` existiert bereits im WMS mit `Description = 'alt'`.
- In Sage wurde `KHKArtikel.Bezeichnung1` fuer `TEST-9999` auf `'neu'` geaendert.

**Schritte:**
1. Service-Sync triggern.
2. `SELECT Description, ModifiedAt, ModifiedBy FROM Articles WHERE ArticleNumber = 'TEST-9999'`.

**Erwartetes Verhalten:**
- `Description = 'neu'`.
- `ModifiedAt` ≈ jetzt.
- `ModifiedBy = 'IDEALAKEWMSService'`.
- Aktivitaets-Protokoll-Eintrag mit `aktualisiert >= 1`.

### 31.5 Idempotenz: Zweiter Lauf ohne Aenderungen schreibt nichts

**Vorbedingungen:**
- Sync wurde gerade gelaufen, keine weiteren Aenderungen in Sage.

**Schritte:**
1. Service-Sync sofort nochmal triggern.
2. Aktivitaets-Protokoll-Eintrag pruefen.

**Erwartetes Verhalten:**
- Counts: `neu = 0`, `aktualisiert = 0`, `gelesen > 0`.

### 31.6 DryRun-Modus

**Vorbedingungen:**
- AppSetting `WorkerSettings:SyncDryRun = true` (per `appsettings.json` oder `ServiceSettings`-Tabelle).
- Service neu starten damit Config gelesen wird.

**Schritte:**
1. Service-Sync triggern.
2. Aktivitaets-Protokoll pruefen.
3. WMS-Artikel pruefen: `SELECT COUNT(*) FROM Articles WHERE ModifiedAt > [letzter-Sync]`.

**Erwartetes Verhalten:**
- Aktivitaets-Protokoll-Eintrag mit Suffix `[DryRun]` in der Message und `neu = 0, aktualisiert = 0, gelesen = N`.
- Keine `Articles`-Aenderungen.

---

## 32. Lagerbestellungen Teilgeliefert + Fehlteile (v1.18.0)

### 32.1 Vollstaendige Lieferung -> Closed
**Vorbedingung:** Bestellung #X mit 2 Items (Soll 10, Soll 5). Status Submitted.
**Schritte:** Picking/Details. Ist=10 + Ist=5. "Speichern + Abschliessen".
**Erwartet:** Status Closed, ClosedAt gesetzt. Keine Eintraege in Fehlteile-Liste.

### 32.2 Alle Shortages als endgueltig markiert -> Closed
**Vorbedingung:** Bestellung mit 2 Items (Soll 10, Soll 5).
**Schritte:** Ist=8 + Ist=3, beide Checkboxen "Endgueltig Fehlteil" anhaken. "Speichern + Abschliessen".
**Erwartet:** Status Closed. Beide Items in Fehlteile-Liste (2 + 2 = Soll-Ist).

### 32.3 Eine offen, eine final -> PartiallyDelivered (Item 2 erscheint sofort in Fehlteile-Liste)
**Vorbedingung:** 2 Items.
**Schritte:** Ist=3 (kein final), Ist=2 (final). Abschliessen.
**Erwartet:** Status PartiallyDelivered. Item 1 NICHT in Fehlteile-Liste (kein final). Item 2 IST in Fehlteile-Liste (final markiert, seit v1.18.1 unabhaengig vom Bestell-Status).

### 32.4 Vollfehlteil (Ist=0, final) bei alleinigem Item -> Closed mit Fehlteil
**Vorbedingung:** Bestellung mit 1 Item Soll=5.
**Schritte:** Ist=0, "Endgueltig Fehlteil" anhaken, Abschliessen.
**Erwartet:** Status Closed. Item in Fehlteile-Liste mit Fehlt=5.

### 32.5 Restlieferungs-Workflow
**Vorbedingung:** Bestellung in PartiallyDelivered (Item 1 = 3 von 10 geliefert, kein final).
**Schritte:** Picking/Details neu oeffnen. Wert auf Ist=10 erhoehen. Abschliessen.
**Erwartet:** Status wechselt auf Closed.

### 32.6 "Drucken und Abschliessen"-Workflow
**Vorbedingung:** Submitted-Bestellung.
**Schritte:** Mengen + Notizen ausfuellen, "Drucken und Abschliessen" klicken.
**Erwartet:** Neuer Tab oeffnet sich mit Print-View (zeigt aktuelle Ist + Notizen + Fehlteil-Spalte). Hauptfenster navigiert zu Index. Bestellung jetzt Closed (oder PartiallyDelivered je nach Eingaben).

### 32.7 Werkbank-Karte "Meine Fehlteile" sichtbar
**Vorbedingung:** Werker WB1 hat 1 Closed-Bestellung mit 2 IsFinalShortage-Items.
**Schritte:** Login als Werker. WarehouseRequisitions/Index oeffnen.
**Erwartet:** Karte "Meine Fehlteile" zeigt "2 endgueltige Fehlteile aus 1 abgeschlossenen Bestellung". Link fuehrt zu MissingParts/Index?mineOnly=true.

### 32.8 Werkbank-Karte verschwindet bei 0 Fehlteilen
**Vorbedingung:** Werker ohne Fehlteile.
**Schritte:** Werkbank-Index oeffnen.
**Erwartet:** Karte nicht sichtbar.

### 32.9 MissingParts Spaltenfilter
**Vorbedingung:** 5 IsFinalShortage-Items, Artikel-Nrn AAA-1, AAA-2, BBB-3, CCC-4, DDD-5.
**Schritte:** Spaltenfilter "ArticleNumber" = "AAA,BBB".
**Erwartet:** Nur die 3 Eintraege mit AAA-1, AAA-2, BBB-3 sichtbar.

### 32.10 Negativ-Test: Ist-Menge -1
**Schritte:** Ist=-1 eingeben (per Forms-Manipulation), Abschliessen.
**Erwartet:** WarningMessage "Ist-Mengen duerfen nicht negativ sein.", Status unveraendert.

### 32.11 Hotfix v1.18.1: Endgueltig Fehlteil aus PartiallyDelivered erscheint sofort
**Vorbedingung:** Bestellung in Status Submitted mit 2 Items (Soll=2, Soll=1).
**Schritte:**
1. Picking/Details. Item 1: Ist=1, Checkbox "Endgueltig Fehlteil" anhaken. Item 2: Ist=0 lassen (keine Aenderung am Flag).
2. "Speichern + Abschliessen" — Status wird PartiallyDelivered (Item 2 erwartet noch Restlieferung).
3. Lager-Menue "Fehlteile" (`/MissingParts`) aufrufen.
4. Werkbank-User "Meine Listen" pruefen — Karte "Meine Fehlteile".
**Erwartet:**
- `/MissingParts` zeigt 1 Eintrag: Item 1 mit Fehlt=1, Bestell-ID = die Bestellung, Werkbank korrekt.
- Werkbank-Karte zeigt "1 endgueltigen Fehlteil aus 1 Bestellung".
- Detail-Link auf die Bestellung funktioniert und zeigt sie in PartiallyDelivered (editierbar).
**Negativ:** Vorher (v1.18.0) war die Liste leer — das war der Bug.

---

## 33. ShortageStatus 3-State + 2-Tab Fehlteile (v1.19.0)

### 33.1 Default-Fehlteil bei Ist=0
**Vorbedingung:** Bestellung in Submitted mit 1 Item, Soll=5.
**Schritte:** Picking/Details oeffnen. Ist=0 eintragen.
**Erwartet:** Radio "Fehlteil" wird automatisch aktiv. Hidden-Input shortageStatuses hat Wert "1".

### 33.2 Manueller Wechsel auf "Wird nicht nachgeliefert"
**Vorbedingung:** Item hat "Fehlteil" aktiv.
**Schritte:** Klick auf "Wird nicht nachgeliefert"-Radio.
**Erwartet:** "Fehlteil" deaktiviert, "Wird nicht nachgeliefert" aktiv. Hidden hat Wert "2".

### 33.3 Doppelklick auf aktiven Radio → zurueck zu None
**Vorbedingung:** Item hat "Fehlteil" aktiv.
**Schritte:** Klick erneut auf "Fehlteil".
**Erwartet:** Beide Radios deaktiviert. Hidden hat Wert "0".

### 33.4 Ist=Soll → beide Radios disabled
**Vorbedingung:** Item mit Soll=5.
**Schritte:** Ist=5 eintragen.
**Erwartet:** Beide Radios disabled (grayed out), kein Klick moeglich. Hidden hat Wert "0".

### 33.5 Bestellung mit allen "Fehlteil" → PartiallyDelivered
**Vorbedingung:** 2 Items.
**Schritte:** Beide Ist<Soll, beide Radios auf "Fehlteil". "Speichern + Abschliessen".
**Erwartet:** Status PartiallyDelivered. Bestellung bleibt im Picking/Index.

### 33.6 Bestellung mit allen "Wird nicht nachgeliefert" → Closed
**Schritte:** Beide Ist<Soll, beide Radios auf "Wird nicht nachgeliefert". Abschliessen.
**Erwartet:** Status Closed. Beide Items in /MissingParts Tab "Wird nicht nachgeliefert".

### 33.7 Tab "Offene Fehlteile" zeigt nur WillBeRestocked
**Vorbedingung:** Mischung aus WillBeRestocked und NoRestock Items in der DB.
**Schritte:** /MissingParts oeffnen (Default-Tab = Offene Fehlteile).
**Erwartet:** Nur Items mit ShortageStatus=WillBeRestocked sichtbar. Tab-Badge zeigt korrekte Count.

### 33.8 Tab "Wird nicht nachgeliefert" zeigt nur NoRestock
**Schritte:** /MissingParts?tab=NoRestock oeffnen.
**Erwartet:** Nur Items mit ShortageStatus=NoRestock sichtbar. Tab-Badge zeigt Count.

### 33.9 Werkbank-Karte zeigt beide Counts mit Tab-Links
**Vorbedingung:** User hat eigene Items: 2 WillBeRestocked aus 1 Bestellung, 1 NoRestock aus 1 Bestellung.
**Schritte:** Werkbank-Index (WarehouseRequisitions/Index) oeffnen.
**Erwartet:** Karte "Meine Fehlteile" zeigt 2 Zeilen:
  - "2 Fehlteile (wird nachgeliefert) aus 1 Bestellungen" (orange Link auf /MissingParts?tab=WillBeRestocked&mineOnly=true)
  - "1 Wird nicht nachgeliefert aus 1 Bestellungen" (rot Link auf /MissingParts?tab=NoRestock&mineOnly=true)

### 33.10 Migration: vorhandene v1.18.x PartiallyDelivered-Bestellung bleibt PD nach v1.19.0-Migration
**Vorbedingung:** DB-Snapshot von vor dem Deploy mit mindestens einer PartiallyDelivered-Bestellung.
**Schritte:**
1. v1.19.0 deployen (DB-Backup vorher).
2. Nach Migration-Run die Bestellung in der DB pruefen: Status weiterhin PartiallyDelivered?
3. Items pruefen: alle vorher IsFinalShortage=false mit Ist<Soll haben jetzt ShortageStatus=1 (WillBeRestocked)? Alle IsFinalShortage=true haben jetzt ShortageStatus=2 (NoRestock)?
**Erwartet:** Status bleibt PartiallyDelivered. Items haben korrekte ShortageStatus-Werte. Lager kann die Bestellung erneut oeffnen und editieren wie gewohnt.

### 33.11 Notiz EK persistiert

**Vorbedingung:** Submitted-Bestellung mit mindestens einem Item.
**Schritte:**
1. Picking/Details oeffnen.
2. Im Notiz-EK-Feld "Test-EK-Notiz" eintragen.
3. "Speichern + Abschliessen" klicken.
4. Bestellung wieder oeffnen (falls Status PartiallyDelivered) ODER Print aufrufen.
**Erwartet:** "Test-EK-Notiz" ist nach dem Abschliessen weiterhin sichtbar. Print zeigt die Notiz in der "Notiz EK"-Spalte.

### 33.12 MissingParts default mineOnly

**Vorbedingung:** Werkbank-User mit Workplace-Zuordnung; mindestens 1 IsFinalShortage-Item in eigener Bestellung und mindestens 1 in einer fremden Bestellung (Status Closed).
**Schritte:**
1. `/MissingParts` direkt im Browser aufrufen (ohne URL-Parameter).
**Erwartet:** Tab "Wird nicht nachgeliefert" zeigt nur das eigene Item. Toggle "Nur meine Werkbaenke" ist aktiv. Deaktivieren zeigt auch das fremde Item.
Trefferzahl und Badges bei mehreren Werkbänken über alle Seiten: TS-108 (v1.75.0). Werkbank-Auswahl im Modus „Nur meine“, Haken per Klick
ausschalten, Karte je Bestelltyp: TS-113 (v1.81.0).

### 33.13 No-Workplace-Banner

**Vorbedingung:** Test-User ohne Workplace-Zuordnung.
**Schritte:**
1. Login als Test-User.
2. Menue "Lager" → "Meine Fehlteile" aufrufen.
**Erwartet:** Banner "Du hast keine Werkbank-Zuordnung. Diese Liste ist deshalb leer. Du kannst entweder den Toggle 'Nur meine Werkbaenke' deaktivieren oder die Zuordnung in den Stammdaten pflegen." erscheint. Items-Tabelle ist leer. Seit v1.81.0 wirkt das Deaktivieren per Klick (TS-113.6).

### 33.14 Fehlteile (Lager)-View

**Vorbedingung:** Lager-User (admin/stock/stock_keyuser/picking).
**Schritte:**
1. Menue "Lager" → "Fehlteile (Lager)" aufrufen.
**Erwartet:** Globale Sicht. Kein mineOnly-Toggle in der Filter-Card. Tabs zeigen jeweils Counts aller Werkbaenke. Notiz-Spalten zeigen sowohl "Notiz Lager" als auch "Notiz EK".

---

## Kapitel 34: Feingranulare Berechtigungen (v1.20.0)

### 34.1 Read-Only-User in Stammdaten

**Vorbedingungen:**
- Admin angelegt einen neuen User "test_read" mit nur der Rolle `masterdata_read`
- "test_read" ist eingeloggt

**Schritte:**
1. Im Hauptmenue auf "Stammdaten" klicken → Eintraege sichtbar (Benutzer, Rollen, Arbeitsplaetze, Einstellungen, ...)
2. Auf "Benutzer" klicken → Index-View laedt
3. Pruefen: "Neu"-Button ist NICHT vorhanden. "Bearbeiten"-Link pro Zeile ist NICHT vorhanden. "Rollen-Uebersicht"-Button ist sichtbar
4. URL direkt `/Users/Create` aufrufen → Redirect zu AccessDenied
5. Auf "Einstellungen" klicken → Index-View mit Banner "Nur-Lesen-Modus" oben
6. Alle Input-Felder sind disabled (grau, nicht editierbar). Save-Button ist NICHT vorhanden

**Erwartet:** Alle Read-Aktionen funktionieren, alle Schreib-Aktionen sind blockiert (UI + Server).

### 34.2 Picker verliert Lager-Worklist-Zugriff

**Vorbedingungen:**
- User "test_picker" hat nur die Rolle `picking`
- Es existieren Lagerbestellungen im Status `Submitted`

**Schritte:**
1. "test_picker" loggt sich ein
2. Im Hauptmenue auf "Bestellungen" klicken
3. Pruefen: "Meine Bestellungen" + "Meine Fehlteile" sind sichtbar. "Lager: Eingehende Listen" + "Lager: Fehlteile" sind NICHT sichtbar
4. URL direkt `/WarehousePicking/Index` aufrufen → Redirect zu AccessDenied
5. URL direkt `/MissingPartsLager/Index` aufrufen → Redirect zu AccessDenied
6. "Bestandsuebersicht" im Lager-Menue ist weiterhin sichtbar + funktioniert
7. "Bewegungshistorie" im Lager-Menue ist weiterhin sichtbar + funktioniert

**Erwartet:** Picker behaelt Bestand + Bewegungen, verliert Worklist-Sichten.

### 34.3 Multi-Rollen-User

**Vorbedingungen:**
- User "test_multi" hat Rollen `picking` + `masterdata_read`

**Schritte:**
1. Login als "test_multi"
2. Stammdaten-Menue ist sichtbar (Read), Edit-Buttons sind ausgeblendet
3. Komm.-Sichten (Meine Bestellungen, Picking) sind sichtbar
4. Lager-Worklist ist NICHT sichtbar (kein stock/stock_keyuser)

**Erwartet:** Union der Rechte — alles was eine der Rollen erlaubt.

### 34.4 Rollen-Uebersicht-Seite

**Vorbedingungen:**
- Login als beliebiger User mit `masterdata_read` oder `masterdata` oder `admin`

**Schritte:**
1. Auf Users/Index navigieren
2. Button "Rollen-Uebersicht" oben klicken
3. Seite `/Users/RoleOverview` laedt
4. Tabelle mit 13 Rollen sichtbar (admin, masterdata, masterdata_read, picking, stock, stock_keyuser, tracking, reporting, leitstand, fa_completion, bde_user, bde_shiftlead, bde_admin)
5. Pro Rolle: Beschreibung + Liste sichtbarer Seiten/Aktionen
6. Erklaer-Header oben erklaert Many-to-Many + Union-Semantik
7. Button "Zurueck zur Benutzerliste" funktioniert

**Erwartet:** Alle 13 Rollen-Zeilen mit korrekten Beschreibungen.

### 34.5 Migration

**Vorbedingungen:**
- Update auf v1.20.0 mit Backup-DB

**Schritte:**
1. App startet → Migration `AddMasterDataReadRole` laeuft
2. In SQL pruefen: `SELECT * FROM Roles WHERE [Key]='masterdata_read'` liefert 1 Zeile
3. Felder: `Name='Stammdaten ansehen'`, `IsSystem=1`, `SortOrder=5`, `AdGroup=NULL`
4. Bestehende User mit `masterdata` haben weiterhin Vollzugriff
5. Im Roles/Index ist die neue Rolle sichtbar (zwischen masterdata und picking sortiert)
6. Versuch die Rolle zu loeschen → blockt wegen `IsSystem=1`

**Erwartet:** Migration laeuft idempotent. Rolle ist als Systemrolle markiert.

---

## Kapitel 35: v1.20.0-Bugfixes (Post-Initial-Release)

### 35.1 Lagerbestellung — Kein Default-Fehlteil bei Ist=0

**Vorbedingungen:** Lagerbestellung mit mind. 2 Positionen, beide Ist-Mengen leer.

**Schritte:**
1. Bestellung oeffnen
2. Keine Ist-Mengen eintragen
3. Pruefen: beide "Fehlteil"-Radios sind unchecked (nicht vorausgewaehlt)
4. Eine Ist-Menge mit Soll-Wert ausfuellen
5. Pruefen: Radios in dieser Zeile werden ausgegraut (shortage=false)
6. Ist-Menge wieder auf 0 setzen
7. Pruefen: Radios werden wieder aktiv, aber unchecked (keine Auto-Default)

**Erwartet:** User muss bewusst Fehlteil-Status setzen.

### 35.2 Lagerbestellung — Submit mit gemischten Mengen + Status

**Vorbedingungen:** Lagerbestellung mit 3 Positionen.

**Schritte:**
1. Bestellung oeffnen
2. Pos 1: "Fehlteil" klicken, Ist leer
3. Pos 2: "Wird nicht nachgeliefert" klicken, Ist leer
4. Pos 3: nichts klicken, Ist leer
5. "Speichern + Abschliessen" → Modal "soll=ist?" → Ja
6. DB pruefen via SQL: `SELECT Id, ArticleNumber, QuantityPicked, ShortageStatus FROM WarehouseRequisitionItems WHERE WarehouseRequisitionId = <ID>`

**Erwartet:**
- Pos 1: QuantityPicked=0, ShortageStatus=1 (WillBeRestocked)
- Pos 2: QuantityPicked=0, ShortageStatus=2 (NoRestock)
- Pos 3: QuantityPicked=1, ShortageStatus=0 (None)
- Order-Status: PartiallyDelivered

### 35.3 Leitstand — Datums-Filter via Kalender-Picker

**Vorbedingungen:** PickingLeitstand-Liste mit mind. 2 FAs mit verschiedenen Komm.-Terminen.

**Schritte:**
1. Leitstand oeffnen
2. Auf den Kalender-Button neben "Komm." klicken
3. Eine KW oder ein Datum waehlen
4. Pruefen: URL aendert sich zu `?colf_picking-date=...`
5. Pruefen: nur passende FAs werden angezeigt
6. "Filter entfernen" im Picker klicken
7. Pruefen: URL ohne colf_picking-date, alle FAs sichtbar

**Erwartet:** Kalender-Picker-Filter triggert URL-Navigation.

### 35.4 Leitstand — Kombinierte Filter

**Vorbedingungen:** mind. 1 FA mit Bezeichnung1 enthaelt "green" UND Komm.-Termin in KW24 (Beispiel).

**Schritte:**
1. Leitstand oeffnen
2. Filter Bezeichnung1 = "green" eintragen
3. 500ms warten → URL `?colf_description1=green`
4. Filter Komm. = "KW24" eintragen (oder Kalender-Picker)
5. 500ms warten → URL `?colf_description1=green&colf_picking-date=KW24`
6. Pruefen: nur FAs die BEIDE Kriterien erfuellen sind sichtbar
7. Reihenfolge umkehren: Erst KW24, dann green — gleiches Ergebnis erwartet

**Erwartet:** Reihenfolge der Filter-Eingabe ist egal. Beide Filter sind aktiv.

---

## Kapitel 36: Universal-Filter-Rollout (v1.21.0)

Alle Tabellen-Listen sind ab v1.21.0 spaltenfilterbar; Filter wirken ueber den
Gesamtbestand (alle Seiten), nicht nur die aktuell angezeigte Seite.

### 36.1 Spaltenfilter ueber mehrere Seiten (Beispiel: Benutzer-Liste)

**Vorbedingungen:** Mind. 60 Benutzer in der DB (mehr als 2 Seiten bei
PageSize 25). Ein eindeutig benannter Benutzer (z.B. Username "zztest")
der bei alphabetischer/Default-Sortierung auf Seite 3 landet.

**Schritte:**
1. Als admin `/Users` oeffnen, PageSize 25
2. Pruefen: "zztest" ist auf Seite 1 NICHT sichtbar
3. In der Filter-Zeile unter dem Spaltenkopf "Benutzername" `zztest` eintippen
4. 500ms warten → URL wechselt zu `?colf_username=zztest` (Col-Key je nach View)
5. Pruefen: "zztest" wird angezeigt, obwohl er ohne Filter auf Seite 3 laege
6. Pruefen: Pagination zeigt die gefilterte Gesamtanzahl (z.B. "1 Eintrag"), Seite 1

**Erwartet:** Der Filter durchsucht ALLE Eintraege, nicht nur die aktuelle
Seite. Pagination-Count entspricht der gefilterten Menge.

### 36.2 KW-Datumsfilter Kommissionierung

**Vorbedingungen:** `/Picking` mit mind. 2 freigegebenen Auftraegen, deren
Komm.-Termine in verschiedenen Kalenderwochen liegen (mind. einer in einer
zukuenftigen KW).

**Schritte:**
1. Kommissionierung oeffnen
2. Auf den Kalender-Button neben der Komm.-Termin-Spalte klicken
3. Eine zukuenftige KW (Jahr + Woche) im Picker waehlen
4. Pruefen: URL enthaelt `?colf_picking-date=KWxx`, nur Auftraege dieser KW sichtbar
5. "Filter entfernen" klicken → alle Auftraege wieder sichtbar
6. Manuell `kw25` (Beispiel, lowercase) in das Filter-Feld tippen
7. Pruefen: gleiches Ergebnis wie ueber den Picker
8. Manuell ein konkretes Datum `10.06.2026` tippen → nur Auftraege mit diesem Termin

**Erwartet:** Picker UND manuelle Eingabe filtern identisch. Termine werden
vor Filter/Pagination fuer alle Auftraege berechnet — auch Treffer auf
hinteren Seiten werden gefunden.

### 36.3 OR/NOT-Filter-Syntax

**Vorbedingungen:** Beliebige server-gefilterte Liste (z.B. Artikel) mit
Eintraegen der Artikelgruppen 960 und 886 sowie weiteren Gruppen.

**Schritte:**
1. Artikel-Liste oeffnen
2. Spaltenfilter Artikelgruppe: `960,886` eintragen
3. Pruefen: nur Eintraege mit 960 ODER 886 sichtbar
4. Filter aendern auf `!960`
5. Pruefen: alle Eintraege AUSSER 960 sichtbar
6. Filter aendern auf `!960,886`
7. Pruefen: alle Eintraege ausser 960 und 886 sichtbar

**Erwartet:** OR via Komma, NOT via `!` — identisches Verhalten in allen
Listen (Server- und Client-Modus).

### 36.4 BDE-Buchungsliste: Filter + Pagination-Count konsistent

**Vorbedingungen:** Mind. 30 BDE-Buchungen in der Historie (mehr als 1 Seite),
davon nur wenige (z.B. 3) von einem bestimmten Operator.

**Schritte:**
1. Als bde_shiftlead `/BdeBookings` oeffnen
2. Spaltenfilter Mitarbeiter: Operator-Namen eintragen
3. Pruefen: nur Buchungen dieses Operators sichtbar
4. Pruefen: Pagination zeigt den korrekten gefilterten Count (z.B. "3 Eintraege")
5. Pruefen: KEINE Phantom-Seiten — Seite 2 existiert nicht, wenn nur 3 Treffer
6. Filter loeschen → voller Count + alle Seiten wieder da

**Erwartet:** Query und Count filtern identisch (SQL-Level). Kein
Auseinanderlaufen von angezeigten Zeilen und Seitenzahl.

### 36.5 BDE-Stammdaten: Filter pro Tab

**Vorbedingungen:** BDE-Stammdaten mit mehreren Operatoren, Aktivitaeten und
Terminals.

**Schritte:**
1. Als bde_shiftlead `/BdeMasterData` oeffnen (Tab "Mitarbeiter")
2. Spaltenfilter Name: einen Operator-Namen eintragen → Liste filtert
3. Auf Tab "Aktivitaeten" wechseln (`?tab=activities`)
4. Pruefen: Aktivitaeten-Liste ist UNGEFILTERT (Filter vom Mitarbeiter-Tab
   wirkt nicht hinueber)
5. Im Aktivitaeten-Tab eigenen Filter setzen → nur Aktivitaeten filtern
6. Zurueck auf Tab "Mitarbeiter" → Filter dort ist zurueckgesetzt
7. *(seit v1.54.0)* Als bde_shiftlead gibt es **keinen** vierten Reiter „Arbeitsgänge“; als Admin bei BDE an schon (TS-87.2).

**Erwartet:** Jeder Tab hat eigenen view-key und eigene Filter. Tab-Wechsel
resettet die Filter des vorherigen Tabs.

### 36.6 Negativtest: Filter ohne Treffer

**Vorbedingungen:** Beliebige server-gefilterte Liste mit Eintraegen.

**Schritte:**
1. Liste oeffnen (z.B. `/Users`)
2. Spaltenfilter mit Unsinns-Wert fuellen (z.B. `xyznichtvorhanden`)
3. Pruefen: Tabelle zeigt keine Datenzeilen
4. Pruefen: Pagination zeigt 0 Eintraege / keine Seiten-Links, kein JS-Fehler
   in der Konsole
5. Filter loeschen → Liste ist wieder vollstaendig

**Erwartet:** Leere Liste + korrekte 0-Anzeige, kein Fehler, Filter-Reset
stellt den Ausgangszustand wieder her.

---

## Kapitel 37: FA-Abschliessen (v1.21.1)

Der "Abschliessen"-Button (Picking/ToggleDone) schreibt seit v1.11
`PickingStatus.IsDonePicking` — bis v1.21.0 las aber keine Query/View das Flag,
der Klick war wirkungslos. Ab v1.21.1 gilt ueberall
"erledigt = IsDone || IsDonePicking" (FA-Liste, Leitstand, Komm.-Worklist).

### 37.1 Abschliessen wirkt in Leitstand + Kommissionierungs-Worklist

**Vorbedingungen:** Ein offener FA (Sage-`IsDone=false`, App-Status nicht
abgeschlossen), der im Leitstand (`/PickingLeitstand`) in der Default-Ansicht
sichtbar ist. Falls `LeitstandAktiv`: FA fuer Kommissionierung freigegeben,
sodass er in der Kommissionierungs-Worklist (`/Picking`) erscheint.

**Schritte:**
1. Im Leitstand beim FA den "Abschliessen"-Button klicken
2. Pruefen: Zeile verschwindet aus der Default-Ansicht (ohne "Erledigte anzeigen")
3. "Erledigte anzeigen" aktivieren
4. Pruefen: Zeile erscheint grau dargestellt mit Badge "Erledigt"
5. Kommissionierung (`/Picking`) oeffnen
6. Pruefen: FA ist NICHT mehr in der Worklist
7. Zurueck im Leitstand (showDone-Modus aktiv) beim FA erneut "Abschliessen" klicken (Toggle)
8. Pruefen: FA ist wieder offen (kein Badge, normale Darstellung) und erscheint
   wieder in der Kommissionierungs-Worklist

**Erwartet:** Der Abschliessen-Toggle wirkt sofort auf Erledigt-Anzeige und
Filter in FA-Liste, Leitstand und Komm.-Worklist. Sage-`IsDone` bleibt
unveraendert (wird nicht beschrieben).

---

## Kapitel 38: FA-Vorbau (v1.22.0)

v1.22.0 ersetzt die 5 fixen Vorbau-Kacheln (`ProductionOrderAssemblyGroups`)
durch einen erweiterbaren Arbeitsgaenge-Katalog (`WorkSteps`) + FA-zu-AG-Tabelle
(`FaWorkSteps`, automatische Erkennung aus dem BOM-Cache), konfigurierbare
FA-Merkmale und eine FA-Abarbeitungsliste je Werkbank (neue Rolle `vorbau`).

Allgemeine Vorbedingungen fuer dieses Kapitel:
- AppSetting `FaCompletionAktiv = true`.
- BOM-Cache aktiv (`Sync:BomCacheEnabled = true`) und mindestens ein offener FA
  mit Artikelnummer + BOM-Cache-Eintrag.
- Benutzer mit Rollen `masterdata` (Stammdaten), `fa_completion`
  (FA-Vervollstaendigen) und `vorbau` (Abarbeitungsliste) — oder ein Admin.

### 38.1 End-to-End: Suchbegriff → Erkennung → Vervollstaendigen → Abarbeitungsliste

**Vorbedingungen:**
- Zusaetzlich zu den Kapitel-Vorbedingungen: Service-Setting
  `Sync:FaWorkStepDetectionEnabled = true`, Windows-Service laeuft.
- Test-FA `FA-1001` (Platzhalter) mit BOM-Cache-Eintrag, dessen Stueckliste ein
  Bauteil mit "Luefter" in der Bezeichnung enthaelt. FA hat noch keine
  FA-Arbeitsgaenge.
- Werkbank `WB-Test` existiert (Stammdaten → Werkbaenke).

**Schritte:**
1. *Suchbegriff pflegen:* Stammdaten → Arbeitsgaenge → `VL` bearbeiten →
   Suchbegriffe `Luefter,Ventilator` eintragen → Speichern.
2. *Detection-Lauf:* Naechsten Sync-Tick abwarten (oder Service neu starten).
   Im Aktivitaets-Protokoll (admin) nach Aktivitaet "FaWorkStepDetection" filtern.
3. Pruefen: End-Eintrag "Run erfolgreich beendet" mit `neu >= 1`.
4. *Vervollstaendigen:* Fertigungsauftraege → FA-Vervollstaendigen → `FA-1001`
   oeffnen. Pruefen: AG-Kachel/Tab `VL` ist vorhanden (automatisch erkannt).
5. Werkbank-Dropdown auf `WB-Test` setzen → Speichern. Pruefen: Badge
   "Keine Werkbank zugewiesen" verschwindet.
6. Ueber "AG hinzufuegen" einen zweiten Arbeitsgang (z.B. `VE`) manuell ergaenzen.
7. Bei `VL` ein zugeordnetes Merkmal erfassen (Dropdown-Wert waehlen bzw.
   JA/NEIN setzen) — Wert wird per onchange sofort gespeichert.
8. *Abarbeitungsliste:* Fertigungsauftraege → FA-Abarbeitungsliste →
   Arbeitsgang `VL` waehlen (kein Werkbank-Filter mehr).
9. Pruefen: `FA-1001` erscheint mit der **Werkbank**-Spalte (`WB-Test`), der
   einen Erledigt-Checkbox fuer `VL`, dem erfassten Merkmal-Wert, Terminen
   (BG/Komm./Fert.) und ggf. enaio-Icons. (Kein Orphan-Badge mehr — die Liste
   ist Arbeitsgang-zentriert; `VE` erscheint einfach beim Filter auf `VE`.)
10. *Read-only Stueckliste:* Auf die FA-Nummer klicken.
11. Pruefen: Stueckliste oeffnet OHNE Picking-Checkboxen, Lagerplatz-Dropdowns,
    Umbuchen-Button, Foto-Upload und Bedarfsmeldungs-Modal. Filter,
    Baugruppen-Navigation und "Drucken" funktionieren.
12. Zurueck in der Abarbeitungsliste die `VL`-Checkbox ("Erledigt") anhaken.
13. Pruefen: FA verschwindet aus der Default-Ansicht des `VL`-Filters;
    mit "Erledigte anzeigen" wieder sichtbar.

**Erwartet:** Erkennung, manuelle Pflege, Arbeitsgang-gefilterte Abarbeitungsliste
(mit Werkbank-Spalte) und read-only Stueckliste greifen nahtlos ineinander;
das "Erledigt"-Abhaken in der Abarbeitungsliste blendet den FA aus.

### 38.2 Negativ: Manuell abgewaehlter AG wird vom Sync NICHT wieder hinzugefuegt

**Vorbedingungen:** Zustand nach 38.1 — `FA-1001` hat den Sync-erkannten AG `VL`,
Detection aktiv.

**Schritte:**
1. FA-Vervollstaendigen → `FA-1001` → Tab `VL` → "Arbeitsgang entfernen"
   (Confirm bestaetigen).
2. Pruefen: Kachel `VL` verschwindet; im Leitstand ist die VL-Checkbox leer.
3. Naechsten Detection-Lauf abwarten (Aktivitaets-Protokoll prueft Lauf-Ende).
4. Pruefen: `VL` ist bei `FA-1001` weiterhin NICHT vorhanden — die Erkennung
   hat den manuell abgewaehlten AG nicht re-added (IsRemoved-Sperre).
5. Ueber "AG hinzufuegen" `VL` wieder manuell hinzufuegen.
6. Pruefen: Kachel erscheint wieder (Zeile reaktiviert, kein Duplikat-Fehler).

**Erwartet:** Manuelle Abwahl gewinnt dauerhaft gegen die automatische
Erkennung; manuelles Wieder-Hinzufuegen reaktiviert dieselbe Zeile.

### 38.3 Negativ: Option mit erfassten Werten nicht loeschbar (Loesch-Guard)

**Vorbedingungen:** Dropdown-Merkmal (z.B. "Verdampfergroesse") mit einer
Option, die bei mindestens einem FA als Wert erfasst ist (siehe 38.1 Schritt 7).

**Schritte:**
1. Stammdaten → FA-Merkmale → Merkmal bearbeiten → bei der verwendeten Option
   auf "Loeschen" klicken.
2. Pruefen: Loeschen wird mit Warnmeldung verweigert (Option ist in Verwendung).
3. Option stattdessen deaktivieren.
4. Pruefen: FA-Vervollstaendigen zeigt den bereits erfassten Wert weiterhin an;
   das Dropdown bietet die Option fuer NEUE Auswahl nicht mehr an.

**Erwartet:** Techniker-Eingaben verschwinden nie still — Optionen mit
referenzierten Werten sind nur deaktivierbar.

### 38.4 Negativ: Doppelter Arbeitsgang-Code wird abgelehnt

**Vorbedingungen:** Arbeitsgang `VL` existiert (Seed).

**Schritte:**
1. Stammdaten → Arbeitsgaenge → "Neu" → Code `VL`, Name `Test-Duplikat` →
   Speichern.
2. Pruefen: Validierungsfehler "Code bereits vorhanden" (o.ae.), kein zweiter
   `VL`-Eintrag in der Liste.

**Erwartet:** Der Arbeitsgang-Code ist eindeutig; Duplikat wird im App-Layer
mit ModelState-Fehler abgelehnt (kein 500er).

### 38.5 Migrations-Smoke: Kacheln-Daten nach Update vorhanden

**Vorbedingungen:** Update einer Datenbank vom Stand v1.21.x (mit gepflegten
Vorbau-Kacheln `IsApplicable`/`IsCompleted` + Specs) auf v1.22.0.
**DB-Backup vor dem Deploy** (siehe Cutover-Doc
`docs/superpowers/cutover/2026-06-12-fa-vorbau-cutover.md` — AgentJob-Skript
MUSS im selben Wartungsfenster aktualisiert werden!).

**Schritte:**
1. Migration einspielen (App-Start mit `db.Database.Migrate()` oder `SQL/68`).
2. FA-Vervollstaendigen → einen FA oeffnen, der vor dem Update aktive Kacheln
   (`IsApplicable=true`) hatte.
3. Pruefen: Die frueher aktiven Gruppen erscheinen als AG-Kacheln, der
   Vervollstaendigt-Status (`IsCompleted`) ist erhalten, alle Freitext-Specs
   sind vorhanden.
4. Einen FA pruefen, der Specs an einer INAKTIVEN Gruppe hatte: Specs sind in
   der DB erhalten (`FaWorkStepSpecs`), der AG ist aber nicht aktiv
   (`IsRemoved=1`) — Kachel erscheint erst nach manuellem Hinzufuegen wieder.
5. Leitstand oeffnen: VK-VA-Haken entsprechen dem Stand vor dem Update.
6. SQL-Agent-Job (FA-Import) einmal manuell ausfuehren: Lauf erfolgreich,
   keine Fehler wegen der entfernten `ProductionOrderAssemblyGroups`-Tabelle.

**Erwartet:** Daten-erhaltende Konvertierung — keine Kachel-/Spec-Daten gehen
verloren; der FA-Import laeuft mit dem aktualisierten Job-Skript fehlerfrei.

### 38.6 Negativ: Komm-erledigter FA verschwindet aus den Vorbau-Views

**Vorbedingungen:** `FaCompletionAktiv=true`. Ein offener FA (`FA-2001`,
Sage-`IsDone=false`) mit einem aktiven Arbeitsgang, einer Werkbank-Zuweisung und
einem zu dieser Werkbank gemappten AG, sodass der FA sowohl in
FA-Vervollstaendigen als auch in der FA-Abarbeitungsliste sichtbar ist.

**Schritte:**
1. Kommissionierung → `FA-2001` → "Abschliessen" klicken
   (setzt `PickingStatus.IsDonePicking=true`, NICHT die Sage-`IsDone`).
2. Pruefen: `FA-2001` verschwindet aus der Fertigungsauftraege-Liste/Leitstand
   (Verhalten seit v1.21.1).
3. FA-Vervollstaendigen (`/FaCompletion`) ohne "Erledigte anzeigen" oeffnen.
4. Pruefen: `FA-2001` erscheint NICHT mehr in der Liste.
5. "Erledigte anzeigen" (showDone) aktivieren.
6. Pruefen: `FA-2001` erscheint wieder.
7. FA-Abarbeitungsliste (`/FaWorklist`) mit der zugehoerigen Werkbank oeffnen.
8. Pruefen: `FA-2001` erscheint NICHT in der Abarbeitungsliste (komm-erledigt
   wird wie Sage-`IsDone` immer ausgeblendet).

**Erwartet:** "Erledigt" bedeutet in beiden Vorbau-Views konsistent
`IsDone || IsDonePicking` — ein nur App-komm-erledigter FA verschwindet genauso
wie aus der FA-Liste; mit showDone bleibt er in FA-Vervollstaendigen sichtbar.

### 38.7 "Vollstaendig definiert" blendet NICHT aus — nur "Erledigt" tut es

**Vorbedingungen:** `FaCompletionAktiv=true`. Ein offener FA (`FA-3001`,
Sage-`IsDone=false`, nicht komm-erledigt) mit einem aktiven Arbeitsgang `VL`,
einer Werkbank-Zuweisung und `VL` zum Werkbank-Mapping gemappt — sodass der FA
in der FA-Abarbeitungsliste der Werkbank sichtbar ist. `VL` ist in der
Abarbeitungsliste noch NICHT als "Erledigt" abgehakt.

**Schritte:**
1. FA-Vervollstaendigen → `FA-3001` → Tab `VL`: Schalter
   "Vollstaendig definiert" einschalten (setzt `IsSpecComplete=true`).
2. Pruefen: Erfolgsmeldung; der Tab-Indikator zeigt den Definiert-Status,
   `SpecCompletedBy`/`SpecCompletedAt` werden angezeigt.
3. FA-Abarbeitungsliste (`/FaWorklist`) mit der zugehoerigen Werkbank oeffnen
   (ohne "Erledigte anzeigen").
4. Pruefen: `FA-3001` ist WEITERHIN sichtbar — "Vollstaendig definiert"
   blendet den FA NICHT aus der Abarbeitungsliste aus.
5. Pruefen: Die `VL`-"Erledigt"-Checkbox in der Abarbeitungsliste ist NICHT
   angehakt (der Definiert-Schalter hat den Arbeit-erledigt-Status nicht
   veraendert — `IsCompleted` blieb false).
6. In der Abarbeitungsliste die `VL`-"Erledigt"-Checkbox anhaken
   (setzt `IsCompleted=true`).
7. Pruefen: `FA-3001` verschwindet jetzt aus der Default-Ansicht der Werkbank
   (alle gemappten AGs erledigt); mit "Erledigte anzeigen" wieder sichtbar.
8. FA-Vervollstaendigen → `FA-3001` → Tab `VL`: Pruefen: "Vollstaendig
   definiert" ist weiterhin eingeschaltet (vom "Erledigt"-Haken unberuehrt —
   die beiden Status sind unabhaengig).

**Erwartet:** "Vollstaendig definiert" (Planer, `IsSpecComplete`) und "Erledigt"
(Werker, `IsCompleted`) sind getrennte Status. Nur der "Erledigt"-Haken in der
FA-Abarbeitungsliste blendet den FA aus; "Vollstaendig definiert" hat keinen
Einfluss auf die Sichtbarkeit in der Abarbeitungsliste.

---

### 38.8 Arbeitsgang-Filter + Standard-Arbeitsgang im Profil (Werkbank-uebergreifend)

**Vorbedingungen:** `FaCompletionAktiv=true`. Mindestens zwei offene FAs
(`FA-4001`, `FA-4002`, beide Sage-`IsDone=false`, nicht komm-erledigt), die
**auf verschiedenen Werkbaenken** liegen (`FA-4001` → `WB-A`, `FA-4002` → `WB-B`)
und beide einen aktiven Arbeitsgang `VE` haben (erkannt oder manuell). Optional
hat `FA-4001` zusaetzlich `VL`. Ein Benutzer mit Rolle `vorbau` (oder Admin),
dessen Profil noch keinen Standard-Arbeitsgang hat.

**Schritte:**
1. *Standard-Arbeitsgang setzen:* Benutzer-Dropdown rechts oben → Mein Profil →
   Feld "Standard-Arbeitsgang (FA-Abarbeitungsliste)" auf `VE` setzen → Speichern.
   (Alternativ als Admin im Benutzerstamm: Benutzer → Bearbeiten → selbes Feld.)
2. *Vorgefiltert oeffnen:* Fertigungsauftraege → FA-Abarbeitungsliste oeffnen
   OHNE einen Arbeitsgang in der URL/im Dropdown zu waehlen.
3. Pruefen: Das Arbeitsgang-Dropdown ist bereits auf `VE` vorausgewaehlt (aus dem
   Profil) — die Liste ist sofort vorgefiltert, ohne manuelle Auswahl.
4. Pruefen: Sowohl `FA-4001` (Werkbank `WB-A`) als auch `FA-4002` (Werkbank `WB-B`)
   erscheinen — die Liste ist **werkbank-uebergreifend**.
5. Pruefen: Es gibt eine **Werkbank**-Spalte, die `WB-A` bzw. `WB-B` anzeigt; sie
   ist als Spalte filterbar (z.B. `WB-A` eintippen → nur `FA-4001` bleibt). Filter
   wieder leeren.
6. Pruefen: Pro Zeile gibt es genau EINE Erledigt-Checkbox (Spalten-Header =
   gewaehlter AG `VE`), nicht mehrere AG-Spalten.
7. *Erledigt-Haken blendet aus:* Bei `FA-4001` die `VE`-"Erledigt"-Checkbox anhaken
   (sofort gespeichert).
8. Pruefen: `FA-4001` verschwindet aus der Default-Ansicht; `FA-4002` bleibt
   sichtbar. Mit "Erledigte anzeigen" wird `FA-4001` wieder eingeblendet.
9. *Anderer AG:* Im Dropdown auf `VL` umstellen.
10. Pruefen: Nur FAs mit aktivem `VL` (hier `FA-4001`) erscheinen; `FA-4002`
    (nur `VE`) ist nicht dabei.

**Erwartet:** Die FA-Abarbeitungsliste filtert nach Arbeitsgang (nicht Werkbank);
der im Profil hinterlegte Standard-Arbeitsgang wird ohne URL-Parameter
vorausgewaehlt. Die Liste zeigt FAs verschiedener Werkbaenke mit Werkbank-Spalte;
der "Erledigt"-Haken des gewaehlten AG blendet den FA aus.

### 38.9 Leitstand VK-VA = Erledigt-Status, synchron zur FA-Abarbeitungsliste

**Vorbedingungen:** `FaCompletionAktiv=true`. Ein offener FA (`FA-5001`,
Sage-`IsDone=false`, nicht komm-erledigt) mit zwei aktiven Arbeitsgaengen `VE`
(`Status=Offen`) und `VK` (`Status=Fertig`); KEIN aktiver `VL`. Benutzer
ist eingeloggt mit Rolle `picking` ODER `leitstand` (oder Admin). Browser-DevTools
Network-Tab offen.

**Schritte:**
1. Leitstand Kommissionierung (`/PickingLeitstand`) oeffnen, `FA-5001` finden.
2. Pruefen: In der `VE`-Spalte steht ein Auswahlfeld auf `Offen`. In der
   `VK`-Spalte ein Auswahlfeld auf `Fertig`. Die `VL`-Zelle ist LEER (kein
   Auswahlfeld — AG nicht anwendbar).
3. Das `VE`-Auswahlfeld auf `Fertig` stellen. Network-Tab pruefen.
4. Pruefen: POST an `/api/fa-work-steps/set-status` mit Body
   `{ faWorkStepId: <Id des VE-FaWorkStep>, status: 2 }`, Status 200 (KEIN
   Request an `/api/fa-work-steps/toggle`).
5. FA-Abarbeitungsliste (`/FaWorklist`) mit Arbeitsgang `VE` oeffnen.
6. Pruefen: `FA-5001` ist NICHT mehr in der Default-Ansicht (der `VE`-Status
   `Fertig` aus dem Leitstand wirkt — gleiches Flag `Status`). Mit "Erledigte
   anzeigen" erscheint `FA-5001` mit `VE`-Status `Fertig`.
7. *Gegenrichtung:* In der Abarbeitungsliste (mit "Erledigte anzeigen") den
   `VE`-Status bei `FA-5001` wieder auf `Offen` stellen.
8. Leitstand neu laden: Pruefen, dass das `VE`-Auswahlfeld bei `FA-5001` wieder
   auf `Offen` steht.

**Negativfall:**
- Benutzer ohne picking/leitstand/vorbau/admin: set-status-Request liefert 302 →
  AccessDenied (bzw. 403); `Status` bleibt unveraendert.
- VK-VA-Spaltenfilter: In der `VK`-Spalte `fertig` eintippen → nur FAs mit
  VK-Status `Fertig` bleiben; `offen` bzw. `in bearbeitung` zeigt FAs mit
  anwendbarem VK im jeweiligen Status. Leere VK-Zellen (nicht anwendbar) matchen
  keinen Status-Text.

**Erwartet:** Die VK/VL/VE/VT/VA-Auswahlfelder im Leitstand zeigen/setzen
`Status` (Erledigt, 3-Wert Offen/in Bearbeitung/Fertig) — DASSELBE Flag wie die
FA-Abarbeitungsliste; Aenderungen sind in beide Richtungen sichtbar.
Nicht-anwendbare AGs erscheinen als leere Zelle. "Anwendbar" wird im Leitstand
nicht mehr gesetzt.

### 38.10 Spalten-Einstellung per Zahnrad in den FA-Views

**Vorbedingungen:** `FaCompletionAktiv=true`. Mindestens ein offener FA, sodass
beide Listen Zeilen zeigen. Benutzer mit Rolle `fa_completion` UND `vorbau`
(oder Admin).

**Schritte:**
1. Fertigungsauftraege → FA-Vervollstaendigen oeffnen.
2. Auf das Zahnrad-Symbol (Spalten-Einstellung) ueber der Tabelle klicken.
3. Pruefen: Eine Liste aller Tabellenspalten erscheint mit Ein/Aus-Schaltern.
4. Eine optionale Spalte (z.B. einen Termin oder eine Merkmal-Spalte) ausblenden
   und den Dialog schliessen.
5. Pruefen: Die Spalte ist in der Tabelle nicht mehr sichtbar.
6. Seite neu laden (F5).
7. Pruefen: Die ausgeblendete Spalte bleibt ausgeblendet (Auswahl pro Benutzer
   gespeichert).
8. Schritte 1-7 analog in der FA-Abarbeitungsliste wiederholen.

**Erwartet:** Beide FA-Views haben ein funktionierendes Zahnrad zum
Ein-/Ausblenden von Spalten; die Auswahl ueberlebt einen Reload und ist
pro Benutzer.

### 38.11 Standard-Werkbank im Profil → FA-Abarbeitung zusaetzlich vorgefiltert

**Vorbedingungen:** `FaCompletionAktiv=true`. Zwei offene FAs mit aktivem,
nicht-erledigtem Arbeitsgang `VE` auf **verschiedenen** Werkbaenken
(`FA-6001` → `WB-A`, `FA-6002` → `WB-B`). Benutzer mit Rolle `vorbau` (oder
Admin), dessen Profil noch keine Standard-Werkbank gesetzt hat. Standard-
Arbeitsgang im Profil = `VE` (vgl. 38.8).

**Schritte:**
1. Benutzer-Dropdown rechts oben → Mein Profil → Feld "Standard-Werkbank
   (FA-Abarbeitungsliste)" auf `WB-A` setzen → Speichern. (Alternativ als Admin
   im Benutzerstamm: Benutzer → Bearbeiten → selbes Feld.)
2. Fertigungsauftraege → FA-Abarbeitungsliste oeffnen OHNE Arbeitsgang oder
   Werkbank in der URL zu waehlen.
3. Pruefen: Das Arbeitsgang-Dropdown ist auf `VE` vorausgewaehlt (Standard-AG)
   UND das Werkbank-Dropdown ist auf `WB-A` vorausgewaehlt (Standard-Werkbank).
4. Pruefen: Nur `FA-6001` (Werkbank `WB-A`) erscheint; `FA-6002` (`WB-B`) ist
   ausgeblendet — beide Filter (AG `VE` UND Werkbank `WB-A`) wirken kombiniert.
5. Im Werkbank-Dropdown auf "(alle)" umstellen.
6. Pruefen: Jetzt erscheinen sowohl `FA-6001` als auch `FA-6002` (nur noch der
   AG-Filter `VE` wirkt).

**Negativfall:**
- Profil ohne Standard-Werkbank: Die Abarbeitungsliste oeffnet werkbank-
  uebergreifend (alle Werkbaenke), nur der Standard-Arbeitsgang wirkt.

**Erwartet:** Die im Profil hinterlegte Standard-Werkbank wird ohne URL-Parameter
vorausgewaehlt und UND-verknuepft mit dem Standard-Arbeitsgang angewandt;
"(alle)" hebt den Werkbank-Filter auf.

### 38.12 Text-Merkmal: Definition → Freitext erfassen → Spalte in der Abarbeitung

**Vorbedingungen:** `FaCompletionAktiv=true`. Benutzer mit `masterdata` (fuer
FA-Merkmale) sowie `fa_completion` + `vorbau` (oder Admin). Ein offener FA
(`FA-7001`) mit aktivem Arbeitsgang `VE`.

**Schritte:**
1. *Definition:* Stammdaten → FA-Merkmale → Neu. Bezeichnung z.B.
   "Sonderhinweis", Typ = **Text** waehlen, Arbeitsgang `VE` zuordnen, speichern.
2. Pruefen: Bei Typ "Text" ist kein Optionen-Block noetig/sichtbar.
3. *Gegenprobe Artikelmerkmale:* Stammdaten → Artikelmerkmale → Neu. Pruefen:
   Das Typ-Dropdown bietet **KEIN** "Text" an (nur Dropdown/JA-NEIN).
4. *Erfassen:* Fertigungsauftraege → FA-Vervollstaendigen → `FA-7001` → Tab `VE`.
   Beim Merkmal "Sonderhinweis" einen Freitext eingeben (z.B. "Bitte Schutzfolie
   dran lassen") und das Feld verlassen (Speichern haelt den aktiven Tab).
5. Pruefen: Der eingegebene Text bleibt nach Reload erhalten.
6. *Abarbeitung:* Fertigungsauftraege → FA-Abarbeitungsliste mit Arbeitsgang `VE`.
7. Pruefen: Es gibt eine Spalte "Sonderhinweis", in der bei `FA-7001` der erfasste
   Freitext angezeigt wird.
8. *Leeren:* In FA-Vervollstaendigen den Freitext loeschen und Feld verlassen.
9. Pruefen: Die Spalte ist bei `FA-7001` jetzt leer (der Wert wurde geloescht).

**Erwartet:** Der Typ "Text" steht nur fuer FA-Merkmale zur Verfuegung (nicht fuer
Artikelmerkmale); der erfasste Freitext erscheint als Spalte in der
FA-Abarbeitungsliste; leeren entfernt den Wert.

### 38.13 BOM/enaio/Vault-Buttons in beiden FA-Views sichtbar und konsistent

**Vorbedingungen:** `FaCompletionAktiv=true`. Ein offener FA (`FA-8001`) mit
gepflegter Artikelnummer und mindestens einem zugeordneten enaio-Dokument
(Werkstattauftrag/Zeichnung). Benutzer mit `fa_completion` + `vorbau` (oder
Admin).

**Schritte:**
1. Fertigungsauftraege → FA-Vervollstaendigen → `FA-8001` oeffnen.
2. Pruefen: Im Kopf-/Zeilenbereich erscheinen in einheitlicher Reihenfolge und
   Optik: Stueckliste/BOM-Button, enaio-Badge(s), Vault-Zeichnungs-Link.
3. Den BOM-Button anklicken → Pruefen: read-only Stueckliste oeffnet (Druck
   moeglich).
4. Den Vault-Link anklicken → Pruefen: Vault oeffnet mit der Artikelnummer als
   Suche (neuer Tab).
5. Fertigungsauftraege → FA-Abarbeitungsliste mit dem passenden Arbeitsgang
   oeffnen, `FA-8001` finden.
6. Pruefen: Dieselben drei Buttons (BOM, enaio, Vault) erscheinen in derselben
   Reihenfolge/Optik wie in FA-Vervollstaendigen.

**Erwartet:** BOM-, enaio- und Vault-Buttons sind in beiden FA-Views vorhanden und
einheitlich dargestellt (gemeinsames Partial `_FaDocumentLinks`); die Links fuehren
zur read-only Stueckliste bzw. zum Vault.
**Ab v1.67.0:** enaio- und Vault-Buttons nur bei eingeschaltetem Schalter (`EnaioLinksAktiv` / `VaultLinksAktiv`, Standard an);
der BOM-Button erscheint **immer**, auch wenn beide aus sind (TS-100.4).

---

## Kapitel 39: UI-Nachzuegler (v1.22.0 Folge-Fixes)

Kleine UI-Korrekturen aus dem laufenden Test der v1.22.0: die Lagerbestellung-
Detailansicht bekommt das Standard-Filter/Sortier-Pattern, und der Hover auf
OSEON-Spaltenueberschriften bleibt lesbar.

### 39.1 Lagerbestellung-Detailansicht: Spalten-Filter + Klick-Sortierung

**Vorbedingungen:** Eine offene Lagerbestellung (Status `Submitted` oder
`PartiallyDelivered`) mit mehreren Positionen (verschiedene Artikelnummern/
Bezeichnungen). Benutzer mit Rolle `picking` ODER `stock` (oder Admin).

**Schritte:**
1. Bestellungen → Lagerbestellungen (`/WarehousePicking/Index`) → eine offene
   Liste oeffnen (`/WarehousePicking/Details`).
2. Pruefen: Unter den Spaltenueberschriften **Pos, Artikel-Nr, Bezeichnung,
   Bestellt, ME, Lagerplatz** erscheint je ein Filter-Eingabefeld (wie in den
   uebrigen Tabellen).
3. In das Filter-Feld unter "Artikel-Nr" einen Teil einer Nummer eingeben.
4. Pruefen: Nur Zeilen mit passender Artikelnummer bleiben sichtbar; Filter wieder
   leeren → alle Zeilen wieder da.
5. Auf die Ueberschrift "Bezeichnung" klicken → Sortierung aufsteigend (Pfeil ▲),
   erneuter Klick → absteigend (▼).
6. Auf "Bestellt" klicken → numerische Sortierung (nicht alphabetisch).
7. Bei einigen Positionen Ist-Mengen/Notizen erfassen, dann "Speichern +
   Abschliessen".
8. Pruefen: Alle Positionen werden korrekt gebucht — auch aktuell **ausgefilterte**
   und **umsortierte** Zeilen; die Ist-Mengen landen beim richtigen Artikel
   (kein Index-Versatz zwischen Position und gebuchter Menge).

**Negativfall:**
- Die Eingabe-Spalten (Ist, Notiz Lager, Notiz EK, Fehlteil-Status) haben bewusst
  KEIN Filterfeld — sie enthalten Formular-Inputs (keinen filterbaren Text).

**Erwartet:** Die Positions-Tabelle bietet Spalten-Filter + Klick-Sortierung wie
die uebrigen Tabellen; Filtern/Sortieren beeinflusst das Speichern NICHT
(alle Items bleiben erhalten, Mengen bleiben den richtigen Positionen zugeordnet).

### 39.2 OSEON-Teileverfolgung: Spaltenueberschrift bleibt beim Hover lesbar

**Vorbedingungen:** AppSetting `TeileverfolgungAktiv = true`; OSEON-Liste mit
mindestens einem Auftrag.

**Schritte:**
1. Teileverfolgung → OSEON Auftraege oeffnen.
2. Mit der Maus ueber eine sortierbare Spaltenueberschrift fahren (z.B. Artikel,
   Werkbank, Status, Endtermin).
3. Pruefen: Der Kopf wird nur geringfuegig heller (helleres Blau) — die
   Hintergrundfarbe bleibt blau, die **weisse Schrift bleibt durchgehend gut
   lesbar**.
4. Pruefen (Regression): Der Kopf wird zu keinem Zeitpunkt weiss/sehr hell; die
   Beschriftung verschwindet nie.

**Erwartet:** Der Header-Hover hellt das Blau nur dezent auf (background-image-
Overlay statt ersetzender background-color); die weisse Schrift bleibt lesbar.

---

## Kapitel 40: Windows-Authentifizierung & AD-Benutzer (v1.23.0)

Windows-SSO Auto-Login (hinter Schalter `WindowsAuthAktiv`, Default aus) plus
AD-Benutzer-Anlage ueber eine Berechtigungsgruppe. Rollen werden ausschliesslich
explizit pro Benutzer vergeben — die fruehere automatische AD-Gruppen-Zuordnung
(`Role.AdGroup`) ist entfernt.

> **Hinweis:** Die echte SSO-Strecke (IIS Negotiate-Challenge + Domaenen-Browser)
> und die LDAP-Abfrage sind nur im IIS-Zielsystem voll testbar (nicht in Dev/Kestrel).
> Vorbedingung fuer 40.1–40.4: Deploy auf einem domaenen-gebundenen IIS, `web.config`
> mit aktivierter **Windows-** UND **Anonymer Authentifizierung**, App-Pool-Identitaet
> darf das AD lesen.

### TS-40.1 — Domaenen-User MIT Datensatz → Auto-Login ohne Formular

**Vorbedingungen:**
- `WindowsAuthAktiv = true` (Stammdaten → Einstellungen).
- Im Benutzerstamm existiert ein aktiver AD-Benutzer, dessen `WindowsUserName`
  (SAM) dem angemeldeten Windows-Konto entspricht (siehe TS-40.4 zum Anlegen).
- Aufruf aus einem Domaenen-Browser (Intranet-Zone), KEIN `NoAutoLogin`-Cookie.

**Schritte:**
1. Browser-Session frisch oeffnen (oder Cookies fuer die Seite loeschen).
2. Die App-Startseite aufrufen.

**Erwartet:** Der Benutzer wird **ohne** Anmelde-Formular direkt angemeldet
(landet auf dem Dashboard). Kein Passwort/Benutzername noetig. Der angemeldete
Name entspricht dem AD-Benutzer-Datensatz.

### TS-40.2 — Domaenen-User OHNE Datensatz → Formular-Fallback

**Vorbedingungen:** `WindowsAuthAktiv = true`. Der angemeldete Windows-Benutzer
hat **keinen** (oder nur einen inaktiven) Datensatz im WMS.

**Schritte:**
1. Frische Browser-Session, App-Startseite aufrufen.
2. Die einmalige Negotiate-Challenge laeuft im Domaenen-Browser still ab.

**Erwartet:** Nach der einmaligen Challenge erscheint die **normale Login-Maske**
(kein Auto-Login). Es kommt zu **keiner** Challenge-Schleife (genau eine Challenge
je Browser-Session, per `AutoLoginTried`-Cookie begrenzt). Eine Formular-Anmeldung
mit lokalen Credentials funktioniert normal.

### TS-40.3 — Logout → Formular, kein sofortiges Re-Login, anderer User moeglich

**Vorbedingungen:** Wie TS-40.1 (Auto-Login waere moeglich). Benutzer ist
angemeldet. Zusaetzlich existiert ein lokaler Benutzer mit Passwort.

**Schritte:**
1. Als (auto-angemeldeter) Benutzer auf **Abmelden** klicken.
2. Die App-Startseite erneut aufrufen.
3. Im Formular mit den Credentials des **lokalen** Benutzers anmelden.

**Erwartet:**
- Nach dem Abmelden erscheint das Anmelde-**Formular** — KEIN sofortiges
  automatisches Wieder-Anmelden (der `NoAutoLogin`-Cookie unterdrueckt den
  Auto-Login).
- Die Anmeldung als lokaler Benutzer gelingt; man ist als dieser angemeldet.
- Nach einem erneuten erfolgreichen Login ist der `NoAutoLogin`-Marker
  aufgehoben (Auto-Login bei der naechsten frischen Session wieder moeglich).

### TS-40.4 — AD-Benutzer anlegen (Picker + Rollen)

**Vorbedingungen:**
- Admin angemeldet.
- `WindowsAuthBerechtigungsgruppe` ist auf eine existierende AD-Gruppe gesetzt,
  die mindestens ein Mitglied hat, das noch NICHT im WMS angelegt ist.
- App laeuft auf dem domaenen-gebundenen IIS (LDAP erreichbar).

**Schritte:**
1. Stammdaten → Benutzer öffnen.
2. Button **„AD-Benutzer anlegen"** klicken.
3. Pruefen: Es erscheint eine Liste der Gruppenmitglieder (SAM, Anzeigename,
   E-Mail, aktiv/inaktiv). Bereits importierte Mitglieder fehlen in der Liste.
4. Ein Mitglied auswaehlen, eine oder mehrere Rollen ankreuzen, **Anlegen**.

**Erwartet:**
- Der Benutzer erscheint in der Benutzerliste mit Typ-Spalte **„AD"** (nicht
  „Lokal"), ohne Passwort, aktiv.
- Die vergebenen Rollen sind gesetzt (im Bearbeiten-Dialog sichtbar).
- Der `WindowsUserName` (SAM) ist hinterlegt — danach greift fuer diesen Benutzer
  der Auto-Login (TS-40.1).

**Negativfaelle:**
- Versuch, ein bereits vorhandenes Mitglied erneut anzulegen → abgelehnt
  (Duplikat-Guard auf `WindowsUserName`).
- AD nicht erreichbar / Gruppe leer → Info-Banner („keine Mitglieder gefunden /
  AD nicht erreichbar"); die manuelle Anlage ueber „Benutzer anlegen" bleibt
  verfuegbar. Der Login-Pfad ist davon unberuehrt.

### TS-40.5 — `WindowsAuthAktiv = false` → Verhalten wie bisher

**Vorbedingungen:** `WindowsAuthAktiv = false` (Default).

**Schritte:**
1. Frische Browser-Session, App-Startseite aufrufen.

**Erwartet:** Es erscheint **direkt** das Anmelde-Formular — KEINE Challenge,
KEIN Auto-Login. Verhalten exakt wie vor v1.23.0. (Gilt auch fuer Dev/Kestrel,
wo keine IIS-Windows-Auth verfuegbar ist.)

### TS-40.6 — Rollen nur noch pro Benutzer (AdGroup entfernt)

**Vorbedingungen:** Admin angemeldet.

**Schritte:**
1. Stammdaten → Rollen → eine Rolle bearbeiten.
2. Stammdaten → Benutzer → einen Benutzer bearbeiten.

**Erwartet:**
- In den Rollen-Stammdaten gibt es **kein** AD-Gruppen-Feld mehr (weder in der
  Liste noch im Bearbeiten-Formular).
- Rollen werden ausschliesslich am Benutzer (Checkbox-Liste) zugewiesen; es gibt
  keine automatische Rollen-Zuweisung ueber AD-Gruppenmitgliedschaft mehr.

---

### TS-40.7 — Android/iPhone → sofort Formular, KEIN Windows-Dialog (v1.25.0-Fold)

**Vorbedingungen:** `WindowsAuthAktiv = true`. Aufruf von einem Android-Handy
oder iPhone/iPad (Mobile-Browser), frische Session (keine Cookies).

**Schritte:**
1. Die App-Startseite vom Mobilgeraet aufrufen.

**Erwartet:** Es erscheint **direkt** das Anmelde-Formular. **Kein**
Windows-Anmeldedialog, kein Passwort-Prompt-Popup. Es wird KEINE
Negotiate-Challenge gesendet (UA-Gate). Eine Formular-Anmeldung mit lokalen
Credentials funktioniert normal. (Auch nach mehrfachem Neuladen kein Dialog.)

### TS-40.8 — Windows-Desktop → Auto-Login wie bisher (v1.25.0-Fold)

**Vorbedingungen:** Wie TS-40.1 (`WindowsAuthAktiv = true`, AD-Benutzer-Datensatz
vorhanden), aber ausdruecklich aus einem **Windows-Desktop-Browser** (Chrome/Edge/
Firefox auf Windows, Intranet-Zone).

**Schritte:**
1. Frische Session, App-Startseite aufrufen.

**Erwartet:** Der Windows-Desktop-Browser erhaelt die stille Negotiate-Challenge
und der Benutzer wird **ohne Formular** angemeldet (Verhalten exakt wie vor dem
UA-Gate). Genau eine Challenge je Session (`AutoLoginTried`-Cookie).

### TS-40.9 — Button „Mit Windows anmelden" erzwingt SSO auf Nicht-Windows-Client (v1.25.0-Fold)

**Vorbedingungen:** `WindowsAuthAktiv = true`. Ein Windows-Geraet, das per UA
NICHT als Windows-Desktop erkannt wird (exotischer Browser) ODER ein Mac/Linux im
Domaenennetz, dessen Windows-Identitaet IIS liefern kann. AD-Benutzer-Datensatz
zum angemeldeten Windows-Konto vorhanden.

**Schritte:**
1. App-Startseite aufrufen → es erscheint das Formular (kein Auto-Login, da UA
   nicht als Windows-Desktop erkannt).
2. Pruefen: Der Button **„Mit Windows anmelden"** ist sichtbar.
3. Auf den Button klicken.

**Erwartet:**
- Der Button fuehrt auf `/Account/WindowsLogin` (setzt Force-Cookie) und weiter
  auf die Startseite; dort erzwingt die Middleware die Negotiate-Challenge.
- Nach erfolgreichem SAM-Match ist der Benutzer angemeldet (Dashboard); der
  Force-Cookie ist wieder geloescht (kein Challenge-Loop).
- **Negativfall:** Ist `WindowsAuthAktiv = false`, erscheint der Button **nicht**.

### TS-40.10 — AD-Benutzer anlegen: E-Mail wird aus dem AD uebernommen (v1.25.0)

**Vorbedingungen:** Wie TS-40.4. Mindestens ein noch nicht importiertes Gruppenmitglied
hat im AD eine E-Mail-Adresse hinterlegt, ein weiteres (falls vorhanden) keine.

**Schritte:**
1. Stammdaten → Benutzer → **„AD-Benutzer anlegen"**.
2. Ein Mitglied MIT E-Mail auswaehlen, Rolle(n) ankreuzen, **Anlegen**.
3. Den neuen Benutzer in Stammdaten → Benutzer → **Bearbeiten** oeffnen.

**Erwartet:**
- Das Feld **E-Mail** ist mit der AD-Adresse des Mitglieds vorbefuellt (server-seitig
  uebernommen — unabhaengig davon, was der Browser gesendet hat).
- Die Adresse kann danach im Benutzerstamm normal geaendert werden.

**Negativfaelle:**
- Mitglied OHNE AD-E-Mail → Benutzer wird angelegt, E-Mail bleibt **leer** (kein Fehler).
- Der SAM-Match ist case-insensitiv (Gross-/Kleinschreibung des gewaehlten Eintrags egal).

### TS-40.11 — Formulare unter Windows-SSO speichern ohne Fehler 400 (Antiforgery-Wurzel-Fix, v1.25.0)

**Hintergrund:** Vor dem Fix band ASP.NET den Antiforgery-Token an die (unter IIS
per Verbindung schwankende) Windows-Identitaet — POSTs direkt nach einem SSO-Login
konnten mit **Fehlerseite 400** abbrechen (z. B. „AD-Benutzer anlegen"). Seit dem Fix
normalisiert die Middleware `HttpContext.User` (App-Session + `/account/*`) auf anonym.

**Vorbedingungen:** Deploy am IIS, `WindowsAuthAktiv = true`, Admin-AD-Benutzer vorhanden.

**Schritte:**
1. Frische Browser-Session am Domaenen-PC → Auto-Login laeuft (TS-40.1).
2. **Direkt danach** (ohne Umwege): Stammdaten → Benutzer → „AD-Benutzer anlegen" →
   Mitglied waehlen → **Anlegen** (POST).
3. Beliebige weitere POST-Formulare pruefen (z. B. Einstellungen speichern, Profil speichern).
4. Zusaetzlich am Domaenen-PC: **Abmelden** → im Formular als lokaler Benutzer anmelden (POST).

**Erwartet:**
- **Kein** HTTP 400 („Bad Request"/Fehlerseite) bei irgendeinem POST — weder nach
  SSO-Auto-Login noch beim Formular-Login auf einem Domaenen-Geraet.
- Der Benutzer wird angelegt bzw. das Formular normal gespeichert.
- Audit-Hinweis: Bei SSO-Login wird der Windows-Name (Domaene\User) fuer die
  Audit-Spalten aus der Session uebernommen; beim reinen Formular-Login lokaler
  Konten steht dort weiterhin „SYSTEM" (bewusst akzeptiert).

### TS-40.12 — Logout nach SSO: Desktop UND Mobile ohne 400/405 (v1.25.0)

**Vorbedingungen:** Wie TS-40.11. Zusaetzlich ein Mobilgeraet (Android/iPhone), auf dem
sich ein Benutzer ueber den Button „Mit Windows anmelden" (TS-40.9) oder das Formular
angemeldet hat.

**Schritte:**
1. Am Desktop nach SSO-Login: **Abmelden** klicken.
2. Am Mobilgeraet nach Login: **Abmelden** klicken.

**Erwartet:** In beiden Faellen landet man auf dem Anmelde-Formular — **kein**
Fehler 400 (Antiforgery) und **kein** 405 (Method Not Allowed nach Mobile-Reload).
Hintergrund: Logout war unter SSO auf Mobilgeraeten mit 400/405 fehlgeschlagen, weil
der Token an die Windows-Identitaet gebunden war; behoben durch den Wurzel-Fix
(TS-40.11) plus bewusst tokenfreien Logout.

---

## Kapitel 41: Rolle „Lagerbestellung" + Artikelinfo für Stammdaten-ansehen (v1.23.0)

**Vorbedingung:** `BestellungenAktiv=true`. Ein Benutzer `lb-test` mit NUR der Rolle
`lagerbestellung`. Ein Benutzer `md-test` mit NUR der Rolle `masterdata_read`.

### TS-41.1 — Lagerbestellung-User: erlaubte Sichten
1. Als `lb-test` einloggen.
2. Menü „Bestellungen" öffnen.
   - **Erwartet:** Einträge „Lagerbestellungen" + „Meine Fehlteile" sichtbar.
   - **Erwartet:** „Bedarfsmeldungen" NICHT sichtbar; „Lager: …" NICHT sichtbar.
3. „Lagerbestellungen" öffnen → Liste lädt (kein AccessDenied).
4. „Meine Fehlteile" öffnen → Liste lädt (kein AccessDenied).

### TS-41.2 — Lagerbestellung-User: verweigerte Sichten (Negativ)
1. Als `lb-test` direkt `/PartRequisitions`, `/StockOverview`, `/StockMovements`,
   `/MissingPartsLager` aufrufen.
   - **Erwartet:** jeweils Redirect auf `/Account/AccessDenied`.

### TS-41.3 — Regression picking/stock
1. Als picking-User und als stock-User je „Lagerbestellungen" + „Meine Fehlteile" öffnen.
   - **Erwartet:** unverändert erreichbar; „Bedarfsmeldungen" weiterhin sichtbar.

### TS-41.4 — Artikelinfo-Kachel für masterdata_read
1. Als `md-test` das Dashboard öffnen.
   - **Erwartet:** Sektion „Artikel" mit der Artikelinfo-Kachel sichtbar.
   - **Erwartet:** KEINE Lager-/Kommissionier-Kacheln.
2. Artikelinfo-Kachel klicken → `Articles/Info` öffnet (kein AccessDenied).

### TS-41.5 — Regression picking-Dashboard
1. Als picking-User das Dashboard öffnen.
   - **Erwartet:** Artikelinfo-Kachel weiterhin in der Kommissionier-Sektion
     (nicht doppelt, nicht fehlend).

---

## Kapitel 42: Lagerbestellungs-Druck spiegelt GUI (Spalten/Sort/Filter) (v1.23.0)

**Vorbedingung:** `BestellungenAktiv=true`. Eine Lagerbestellung mit mehreren Positionen
(Status Abgeschickt/Teilgeliefert). Angemeldet als Lager-/Picking-/Admin-User.

### TS-42.1 Spalten ein-/ausblenden + Reihenfolge
1. `WarehousePicking/Details/<id>` öffnen. Zahnrad-Menü (rechts in der Tabelle) erscheint.
2. Spalte „Notiz EK" ausblenden, „Lagerplatz" nach vorne ziehen. Seite neu laden.
   - **Erwartet:** Einstellung bleibt erhalten (persistent pro User).
3. „Drucken" klicken.
   - **Erwartet:** Druckseite zeigt „Notiz EK" NICHT, „Lagerplatz" an der verschobenen Position;
     „Pos" + „Artikel-Nr" sind immer vorhanden (nicht ausblendbar).

### TS-42.2 Sortierung übernehmen
1. In Details auf den Spaltenkopf „Lagerplatz" klicken (absteigend sortieren).
2. „Drucken".
   - **Erwartet:** Druck-Zeilen in derselben Reihenfolge wie der Bildschirm (Lagerplatz absteigend).
3. Numerische Spalte testen: nach „Bestellt" sortieren (z. B. 2, 9, 10).
   - **Erwartet:** numerische Reihenfolge (2, 9, 10) — NICHT lexikalisch (10, 2, 9).

### TS-42.3 Filter übernehmen
1. In Details einen Spaltenfilter setzen (z. B. „Artikel-Nr" enthält einen Teilstring).
2. „Drucken".
   - **Erwartet:** Druck zeigt nur die gefilterten (sichtbaren) Zeilen.

### TS-42.4 Default-Zustand (Regression)
1. Neuer User ohne Spalten-Einstellungen, kein Klick-Sort, kein Filter → „Drucken".
   - **Erwartet:** alle 10 Spalten in Standard-Reihenfolge, sortiert nach Position — wie bisher.

### TS-42.5 Mengen/Notizen-Speichern (Regression)
1. In Details Menge/Notiz ändern, dann „Drucken".
   - **Erwartet:** Änderung wird vor dem Druck gespeichert (Autosave), Druck zeigt den neuen Stand;
     Spalten-Reorder hat das Speichern nicht beschädigt.

---

## Kapitel 43: FA-Abarbeitungsliste — Komma-Werkbank-Filter + Bezeichnung (v1.23.0)

**Vorbedingung:** `FaCompletionAktiv=true`. Mehrere Werkbänke (z. B. „WB-A", „WB-A2", „WB-B").
Offene FAs mit einem FA-Vorbau-AG auf verschiedenen Werkbänken, gefüllten Bezeichnungen.
Angemeldet als vorbau/admin-User.

### TS-43.1 Standard-Werkbänke im Profil (Textfeld)
1. Profil öffnen → Feld „Standard-Werkbänke (FA-Abarbeitungsliste, kommasepariert)".
   - **Erwartet:** Textfeld (kein Dropdown), Datalist schlägt vorhandene Werkbank-Namen vor.
2. „WB-A, WB-B" eintragen, speichern.
3. FA-Abarbeitungsliste öffnen (AG gewählt).
   - **Erwartet:** Werkbank-Feld vorbefüllt „WB-A, WB-B"; Liste zeigt nur FAs auf WB-A/WB-A2/WB-B
     (Enthält: „WB-A" trifft auch „WB-A2").

### TS-43.2 In-Listen-Filter override + leeren
1. In der Abarbeitungsliste ins Werkbank-Textfeld „WB-B" eintragen → Liste neu.
   - **Erwartet:** nur FAs deren Werkbank „WB-B" enthält.
2. Feld leeren, Liste neu.
   - **Erwartet:** ALLE Werkbänke (Default greift nicht, weil explizit geleert).

### TS-43.3 Bezeichnungs-Spalten
1. Abarbeitungsliste mit gewähltem AG.
   - **Erwartet:** Spalten „Bezeichnung 1" + „Bezeichnung 2" nach „Artikelnummer", gefüllt.
2. In „Bezeichnung 1" einen Spaltenfilter setzen.
   - **Erwartet:** Liste auf passende Zeilen reduziert.

### TS-43.4 Benutzerstamm (Admin)
1. Benutzer bearbeiten → „Standard-Werkbänke"-Textfeld pflegen/speichern.
   - **Erwartet:** wie Profil; Wert wird gespeichert.

### TS-43.5 Personalisierter Default-Filter „Bezeichnung 1" (v1.30.0, Teil 8)
**Vorbedingung:** Benutzer mit Rolle `vorbau`, mindestens zwei FAs mit unterschiedlicher
Bezeichnung 1 (z. B. „Verdampfer", „Kondensator") im gewählten Arbeitsgang.
1. Im Profil (`/Account/Profile`) „Verdampfer" als „Standard-Filter Bezeichnung 1
   (FA-Abarbeitungsliste)" speichern.
2. `/FaWorklist?workStepId=<id>` **ohne** weitere Parameter öffnen.
   - **Erwartet:** Liste zeigt nur FAs mit „Verdampfer" in Bezeichnung 1; der Wert „Verdampfer"
     steht **sichtbar** im Spaltenfilter-Feld „Bezeichnung 1"; die URL enthält
     `colf_description1=Verdampfer` sowie den Sentinel `df1=1`.
3. Das Feld „Bezeichnung 1" leeren und bestätigen (ENTER).
   - **Erwartet:** Liste zeigt wieder **alle** FAs des Arbeitsgangs; der Default greift **nicht**
     erneut, solange die View nicht frisch neu geladen wird (Sentinel `df1` verhindert
     Re-Injektion). Erst ein frischer Aufruf von `/FaWorklist` (ohne `df1`) belegt den Default
     wieder vor.
4. **Negativfall (Override):** `/FaWorklist?workStepId=<id>&colf_description1=Kondensator`
   direkt aufrufen.
   - **Erwartet:** Der explizite URL-Parameter gewinnt („Kondensator"), nicht der gespeicherte
     Default.
5. **Admin-Pflege:** Als Admin über `/Users/Edit/{id}` denselben Default für einen anderen
   Benutzer setzen.
   - **Erwartet:** Wert wird gespeichert und wirkt beim nächsten Erstaufruf dieses Benutzers wie
     Schritt 2.
6. **Negativfall (kein Default):** Feld im Profil leer lassen/leeren, speichern,
   `/FaWorklist?workStepId=<id>` öffnen.
   - **Erwartet:** Unveränderte Filterung (kein Default, wie vor dieser Änderung).

---

## Kapitel 44: FA-AG-Erkennung + BOM-Cache — Protokoll-Aufgliederung (v1.23.0)

**Vorbedingung:** `Sync:BomCacheEnabled` + `Sync:FaWorkStepDetectionEnabled` aktiv. WorkSteps mit
Suchbegriffen gepflegt. Service-Lauf auslösen (oder Neustart).

### TS-44.1 Nicht gefundene Suchbegriffe
1. Einen Suchbegriff pflegen, der in keiner gecachten Stückliste vorkommt (z. B. Tippfehler).
2. Lauf abwarten → `/SyncLog` → Eintrag `FaWorkStepDetection`.
   - **Erwartet:** Counts `ohne treffer` ≥ 1; in der Lauf-Message „Ohne Treffer: <begriff> (<Code>)".

### TS-44.2 Begriff je erkanntem FA
1. Einen offenen FA mit gecachter Stückliste, dessen BOM einen Suchbegriff enthält, neu erkennen lassen.
   - **Erwartet:** `FaWorkStepDetection`-Detailzeilen `FA <Nr> → AG <Code> <Name> erkannt (Begriff: <term>)`.
2. Nächster Lauf (nichts Neues) → **keine** neuen Detailzeilen (kein Spam).

### TS-44.3 BOM-Cache Cap-Warnung
1. `Sync:BomCacheMaxOrders` kleiner setzen als die Zahl offener FAs im Fenster.
2. Lauf abwarten → `/SyncLog` → Eintrag `BomCache`.
   - **Erwartet:** Counts `fa im fenster` > `fa gecacht`; Warn-Zeile „Cap erreicht: X von Y … Z FAs
     ohne Cache-Eintrag, werden NICHT automatisch erkannt."
3. Cap groß genug setzen → nächster Lauf: keine Cap-Warnung; die zuvor fehlende FA wird gecacht und erkannt.

---

## Kapitel 45: FA-Vorbau 3-Wert-Status + Beschichtungstermin + ENTER-Spaltenfilter (v1.24.0)

**Vorbedingung global:** `FaCompletionAktiv=true`. Rolle `vorbau` (oder `admin`) fuer die
FA-Abarbeitungsliste, Rolle `picking`/`leitstand` (oder `admin`) fuer den Leitstand.

### TS-45.1 3-Wert-Status in der FA-Abarbeitungsliste (Offen → in Bearbeitung → Fertig)
**Vorbedingung:** Eine offene FA mit einem aktiven Vorbau-AG (`FaWorkStep` mit `IsRemoved=0`).
1. `/FaWorklist` oeffnen, den FA-Vorbau-AG der Test-FA waehlen (oder als Standard-AG im Profil hinterlegt).
   - **Erwartet:** Die FA erscheint, in der Status-Spalte ein Auswahlfeld mit Wert **Offen**.
2. Status auf **in Bearbeitung** stellen.
   - **Erwartet:** Auswahl wird sofort gespeichert (AJAX); die FA **bleibt sichtbar** (kein Ausblenden).
3. Status auf **Fertig** stellen.
   - **Erwartet:** Die FA **verschwindet** aus der Default-Ansicht. Mit „Erledigte anzeigen" ist sie
     weiterhin sichtbar und zeigt den Wert **Fertig**.
4. DB-Pruefung: `SELECT Status, CompletedAt, CompletedBy FROM FaWorkSteps WHERE Id=<FaWorkStepId>`.
   - **Erwartet:** `Status=2` (Fertig) → `CompletedAt`/`CompletedBy` gesetzt. Zurueck auf Offen/in
     Bearbeitung (`Status=0`/`1`) → `CompletedAt`/`CompletedBy` wieder `NULL`.

### TS-45.2 Gleicher Status im Leitstand (VK-VA), synchron zur Abarbeitungsliste
**Vorbedingung:** `LeitstandAktiv=true`; dieselbe FA wie in TS-45.1 mit aktivem VK-VA-AG.
1. `/PickingLeitstand` oeffnen.
   - **Erwartet:** In der zum AG passenden VK-VA-Zelle erscheint dasselbe Status-Auswahlfeld
     (Offen/in Bearbeitung/Fertig). Fehlt der AG fuer die FA → leere Zelle (kein Auswahlfeld).
2. Im Leitstand den Status auf **Fertig** stellen.
   - **Erwartet:** Aenderung wirkt sofort (AJAX, `/api/fa-work-steps/set-status`).
3. `/FaWorklist` neu laden (gleicher AG).
   - **Erwartet:** Die FA spiegelt den im Leitstand gesetzten Status (Fertig → ausgeblendet bzw. unter
     „Erledigte anzeigen" als Fertig).

### TS-45.3 Negativ: ungueltiger Status liefert HTTP 400
1. POST auf `/api/fa-work-steps/set-status` mit Body `{ "faWorkStepId": <gueltigeId>, "status": 9 }`
   (ungueltiger Enum-Wert).
   - **Erwartet:** HTTP **400 Bad Request**; der Status in der DB bleibt unveraendert.

### TS-45.4 Beschichtungstermin-Spalte in der Abarbeitungsliste
**Vorbedingung:** `BeschichtungTage`/`BeschichtungAbholtage` wie ueblich konfiguriert.
1. **Fall A — `LackierteilKategorieName` leer:** `/FaWorklist` oeffnen.
   - **Erwartet:** Spalte „Beschicht." ist fuer **ALLE** FAs gefuellt (Backward-Compat-Regel).
2. **Fall B — `LackierteilKategorieName` gesetzt:** `/FaWorklist` oeffnen.
   - **Erwartet:** „Beschicht." ist nur fuer FAs mit Lackierteilen (`HasCoatingParts`) gefuellt, sonst leer.
3. Spaltenfilter „Beschicht." nutzen (KW-/Datums-Filter ueber das Kalender-Popup oder Texteingabe + ENTER).
   - **Erwartet:** Die Liste filtert korrekt auf die gewaehlte KW/das Datum.
4. Quervergleich: dieselbe FA im Leitstand oeffnen.
   - **Erwartet:** Der Beschichtungstermin ist **identisch** zum Wert in der Abarbeitungsliste
     (gemeinsame Formel `CoatingDateCalculator.Compute`).

### TS-45.5 Server-Mode-Spaltenfilter filtern erst bei ENTER
**Vorbedingung:** Eine Server-Mode-Liste (FA-Liste, Leitstand, Bestand, FA-Abarbeitungsliste).
1. In ein Spaltenfilter-Feld tippen (mehrere Zeichen, ohne ENTER).
   - **Erwartet:** **KEIN** Seiten-Reload/Navigation waehrend des Tippens.
2. **ENTER** druecken.
   - **Erwartet:** Jetzt navigiert die Seite und filtert (URL `?colf_<col-key>=...`).
3. Kalender-KW-Klick auf einer Datumsspalte.
   - **Erwartet:** Filtert **sofort** (ohne ENTER).
4. „Filter entfernen" im Kalender-Popup.
   - **Erwartet:** Wirkt **sofort**.
5. Client-Mode-Gegenprobe: `/Tracking/ByWorkplace` (Client-Filter) — in ein Filter-Feld tippen.
   - **Erwartet:** Filterung bleibt **live beim Tippen** (kein ENTER noetig, da kein Server-Mode).

### TS-45.6 Android-Tablet: Spaltenfilter per Bildschirmtastatur ausloesen
**Vorbedingung:** Android-Tablet (Chrome/Gboard), eine Server-Mode-Liste (z. B. FA-Liste, Leitstand, Bestand, FA-Abarbeitungsliste).
1. In ein Spaltenfilter-Feld tippen, sodass die Bildschirmtastatur erscheint.
   - **Erwartet:** Die Aktionstaste unten rechts ist eine **Such-/Lupe-Taste** (`enterkeyhint="search"`), **nicht** eine „Weiter"-Taste.
2. Die Such-/Lupe-Taste druecken.
   - **Erwartet:** Die Liste **navigiert und filtert** (URL `?colf_<col-key>=...`) — genau wie ENTER am Desktop. Der Fokus springt **nicht** stumm ins naechste Spaltenfeld.
3. Negativ-Gegenprobe Desktop (unveraendert): ENTER filtert, blosses Tippen nicht.

---

## Kapitel 46: Glas-Bestellung (Bestelltyp Lager/Glas) (v1.25.0)

**Vorbedingung global:** `BestellungenAktiv=true` (bzw. Lagerbestellungen freigeschaltet).
Einstellungen: `GlasArtikelgruppen` = z. B. `GLAS,SPIEGEL` (Codes wie in den Artikeln gespeichert),
`GemeinsameArtikelgruppen` = `EUZ`, `DefaultLagerbestellempfaengerId` gesetzt. Es existieren
Artikel in mindestens einer Glas-Gruppe, einer reinen Lager-Gruppe und der gemeinsamen Gruppe `EUZ`.
Der Test-Benutzer hat eine Werkbank-Zuordnung.

### TS-46.1 Glas-Draft anlegen
**Vorbedingung:** Benutzer mit Zugriff auf beide Reiter (z. B. `picking`/`stock`/`admin`).
1. `/WarehouseRequisitions` oeffnen.
   - **Erwartet:** Oben zwei Reiter **Lager** und **Glas**.
2. Auf den Reiter **Glas** wechseln und &bdquo;+ Neue Liste&ldquo; klicken (ggf. Werkbank waehlen).
   - **Erwartet:** Ein neuer Entwurf wird angelegt und die Bearbeiten-Ansicht geoeffnet; ein
     **Typ-Badge Glas** ist sichtbar.
3. DB-Pruefung: `SELECT Type FROM WarehouseRequisitions WHERE Id=<neu>`.
   - **Erwartet:** `Type=2` (Glas).

### TS-46.2 Artikelsuche respektiert den Bestelltyp
**Vorbedingung:** Je ein Glas-Draft und ein Lager-Draft (beide in Bearbeitung).
1. Im **Glas-Draft** im Artikel-Suchfeld nach einem Glas-Artikel suchen.
   - **Erwartet:** Glas-Gruppen-Artikel + `EUZ`-Artikel erscheinen; reine Lager-Gruppen-Artikel
     erscheinen **nicht**.
2. Im **Lager-Draft** nach demselben Glas-Artikel suchen.
   - **Erwartet:** Glas-Gruppen-Artikel erscheinen **nicht**; `EUZ`-Artikel und reine
     Lager-Gruppen-Artikel erscheinen sehr wohl.

### TS-46.3 AddItem-Schutz (Server-Enforcement)
**Vorbedingung:** Ein Glas-Draft.
1. Im Glas-Draft versuchen, einen Artikel einer **reinen Lager-Gruppe** hinzuzufuegen —
   z. B. per zweitem Browser-Tab / manipulierter API-Anfrage (`AddItem` mit einer ArticleId,
   die nicht zum Typ passt).
   - **Erwartet:** Der Server lehnt ab (Fehlermeldung/`BadRequest`); **kein** Item wird angelegt.
   - Gegenprobe: derselbe Aufruf mit einem erlaubten (Glas- oder `EUZ`-)Artikel legt das Item an.

### TS-46.4 Submit Glas — Empfaenger + Betreff
1. **Ohne** `DefaultGlasbestellempfaengerId`: Einen Glas-Draft mit mind. einem Item abschicken.
   - **Erwartet:** Warnung &bdquo;Default-Glasbestellempfaenger nicht konfiguriert&ldquo;; kein Versand.
2. `DefaultGlasbestellempfaengerId` auf eine Glas-Empfaenger-Gruppe setzen, `Sync:WarehouseRequisitionEmailEnabled=true`.
   Denselben (oder einen neuen) Glas-Draft abschicken.
   - **Erwartet:** Mail geht an die **Glas**-Empfaenger-Gruppe; Betreff enthaelt
     &bdquo;Glasbestellung #&hellip;&ldquo;. Gegenprobe Lager-Bestellung → Betreff
     &bdquo;Lagerbestellung #&hellip;&ldquo; an den Lager-Empfaenger.

### TS-46.5 Lager: Eingehende Listen — Reiter-Trennung
**Vorbedingung:** Je eine abgeschickte Lager- und Glas-Bestellung; Benutzer mit Lager-Verarbeitung.
1. `/WarehousePicking` oeffnen.
   - **Erwartet:** Reiter **Lager**/**Glas** trennen die eingehenden Listen; jede Liste erscheint
     nur unter ihrem Typ.
2. Auf dem **Glas**-Reiter eine Glas-Liste oeffnen und **Abschliessen** (oder **Stornieren**).
   - **Erwartet:** Nach dem Zurueckspringen ist man weiterhin auf dem **Glas**-Reiter (Typ-Erhalt).

### TS-46.6 Fehlteile (Werker + Lager) — Typ × Status
**Vorbedingung:** Glas- und Lager-Bestellungen mit Fehlteil-Positionen in verschiedenen
Status (`WillBeRestocked`, `NoRestock`).
1. `/MissingParts` (Werker) oeffnen.
   - **Erwartet:** Aeussere Typ-Reiter (Lager/Glas) × innere Status-Reiter; die Badge-Counts stimmen
     je Kombination (nur die zum gewaehlten Typ passenden Fehlteile werden gezaehlt/gezeigt).
2. `/MissingPartsLager` (Lager) oeffnen.
   - **Erwartet:** Dieselbe Typ×Status-Aufteilung mit korrekten Counts.

### TS-46.7 Rollen-Matrix
1. Benutzer nur mit Rolle `lagerbestellung`.
   - **Erwartet:** In &bdquo;Lagerbestellungen&ldquo; **kein** Glas-Reiter; nur Lager sichtbar.
2. Benutzer nur mit Rolle `glasbestellung`.
   - **Erwartet:** Nur der **Glas**-Reiter; das Menue zeigt **kein** &bdquo;Bedarfsmeldungen&ldquo;.
3. Benutzer mit `picking` bzw. `stock`.
   - **Erwartet:** **Beide** Reiter (Lager + Glas) sichtbar.

### TS-46.8 Bestandsbestellungen aus der Zeit vor dem Update
**Vorbedingung:** Migration 77 (`AddWarehouseRequisitionTypeAndGlasRole`) wurde eingespielt.
1. Eine Bestellung, die **vor** dem Update angelegt wurde, oeffnen bzw. in der Liste suchen.
   - **Erwartet:** Sie hat Typ **Lager** (Migrations-Default `Type=1`) und erscheint im
     **Lager**-Reiter — nicht im Glas-Reiter.

### TS-46.9 Kommentar: Anzeige + Ownership (v1.30.0, Teil 7)
**Vorbedingung:** Migration 85 eingespielt. Draft-Bestellung (Lager) des eigenen Users existiert.
1. Im Edit-Formular (`Views/WarehouseRequisitions/Edit.cshtml`) einen Kommentar eintragen,
   speichern.
2. Zur Lager-Ansicht `/WarehousePicking` wechseln.
   - **Erwartet:** Kommentar ist read-only sichtbar in der Spalte „Kommentar" der Eingehenden
     Listen **und** in der Detailansicht (`/WarehousePicking/Details/{id}`).
3. **Negativfall (leer):** Bestellung ohne Kommentar ansehen.
   - **Erwartet:** leere Zelle, kein Fehler.
4. **Negativfall (Ownership/Status):** Kommentar eines **anderen** Users bzw. einer bereits
   submitteten Bestellung zu aendern versuchen.
   - **Erwartet:** nicht editierbar (Forbid/„nicht mehr im Entwurf").

### TS-46.10 Kommentar im Submit-Mail
**Vorbedingung:** Draft mit Kommentar, Benachrichtigungs-Mail aktiv (`Sync:WarehouseRequisition
EmailEnabled=true`), Windows-Service laeuft (`deploy.service` dieses Teils eingespielt).
1. Bestellung submitten, NotificationWorker durchlaufen lassen, Mail pruefen (HTML- und
   Text-Teil).
   - **Erwartet:** Kommentar erscheint im Kopf des Submit-Mails (HTML **und** Text).
2. **Negativfall:** Bestellung ohne Kommentar submitten.
   - **Erwartet:** Mail ohne Kommentarzeile.
3. **Negativfall (Storno):** Bestellung mit Kommentar stornieren.
   - **Erwartet:** Storno-Mail enthaelt **keinen** Kommentar.

### TS-46.11 DUMMY-Position anlegen (Lager)
**Vorbedingung:** Migration 86 (DUMMY-Seed) eingespielt (`Articles.ArticleNumber='DUMMY'`
existiert genau einmal). EK-Nummer, die nicht existiert.
1. Im Lager-Draft die Artikelsuche mit dieser EK-Nummer ausfuehren → "nicht gefunden".
2. DUMMY-Position mit einer eigenen, aussagekraeftigen Bezeichnung anlegen.
   - **Erwartet:** Position mit der eingegebenen Bezeichnung erscheint sofort in der Bestellung;
     die geteilte `Article.Description` des DUMMY-Artikels bleibt unveraendert.
3. **Negativfall (leer):** DUMMY-Position ohne Bezeichnung anzulegen versuchen.
   - **Erwartet:** serverseitige Ablehnung.
4. **Negativfall (Default-Text):** Bezeichnung unveraendert auf dem Default-Seed-Text
   ("DUMMY – Bezeichnung bitte eintragen") belassen und absenden.
   - **Erwartet:** serverseitige Ablehnung ("Bitte eine eigene Bezeichnung eingeben.").

### TS-46.12 DUMMY mehrfach je Bestellung
**Vorbedingung:** wie TS-46.11.
1. Zwei DUMMY-Positionen mit **unterschiedlichen** Bezeichnungen in **einer** Bestellung
   anlegen.
   - **Erwartet:** beide Positionen erscheinen; **kein** "bereits enthalten"-Fehler (Duplikat-Guard
     ist fuer den DUMMY-Schluessel bewusst ausgenommen).

### TS-46.13 DUMMY in Glasbestellung
**Vorbedingung:** wie TS-46.11, zusaetzlich ein Glas-Draft.
1. In einer **Glas**-Bestellung eine DUMMY-Position anlegen (eigene Bezeichnung).
   - **Erwartet:** anlegbar, **kein** "gehoert in die Lager-Bestellung"-Fehler (Glas-Artikelgruppen-
     Pruefung wird fuer den DUMMY-Schluessel umgangen).

### TS-46.14 DUMMY ist NICHT ueber die reguläre Artikelsuche waehlbar (Regression, UAT-Fix v1.30.0)
**Vorbedingung:** wie TS-46.11. **Hintergrund:** Beim UAT liess sich der DUMMY (NULL-Artikelgruppe,
fuer den Typ `Lager` bislang als erlaubt durchgelassen) ueber die normale Artikelsuche waehlen und
mit der ungeaenderten Default-Bezeichnung hinzufuegen — die Pflicht-Ueberschreibung wurde damit
umgangen. Fix in `ArticlesApiController.Search` (DUMMY in beiden Such-Zweigen ausgeschlossen) +
`WarehouseRequisitionsApiController.AddItem` (lehnt den DUMMY-Schluessel zusaetzlich am normalen
Add-Pfad ab, defense in depth).
1. Im Lager-Draft im Artikelsuchfeld gezielt nach `DUMMY` suchen (Artikelnummer oder Teil der
   Default-Bezeichnung).
   - **Erwartet:** **0** Treffer — es erscheint der „Kein Artikel gefunden"-Block mit
     Pflicht-Bezeichnungsfeld und Button „DUMMY-Position anlegen" (**nicht** ein anklickbarer
     DUMMY-Treffer aus der Ergebnisliste).
2. Dasselbe in einem **Glas**-Draft wiederholen.
   - **Erwartet:** ebenfalls 0 Treffer, gleicher „nicht gefunden"-Block.
3. **Negativfall (API direkt, Absicherung):** `POST /api/warehouserequisitions/{id}/items` direkt
   mit `articleNumber = "DUMMY"` aufrufen (z. B. per Browser-Devtools/Postman), **ohne** den
   `.../items/dummy`-Endpunkt zu verwenden.
   - **Erwartet:** `400 Bad Request` („Der DUMMY-Artikel kann nur über 'Artikel nicht gefunden →
     DUMMY-Position' mit eigener Bezeichnung angelegt werden."), **keine** Position wird angelegt.
4. Regulaere Artikel (Nicht-DUMMY) weiterhin ueber Suche + normalen Add-Pfad hinzufuegen.
   - **Erwartet:** unveraendert moeglich (Fix ist eng auf den DUMMY-Schluessel begrenzt).

## Kapitel 47: Service-Resilienz + Fehlermail + ProductionOrders-515-Fix (v1.25.0)

**Vorbedingung global:** Der Windows-Service (`IDEALAKEWMSService`) laeuft; der ProductionOrders-Sync
ist aktiv (`Sync:ProductionOrdersEnabled=true`). SMTP ist konfiguriert.

### TS-47.1 ProductionOrders-Sync gegen IDEAL-Schema-DB (515-Fix)
**Vorbedingung:** Eine WMS-DB, deren Tabelle `ProductionOrders` die Spalte `SubOrderNumber`
mit Constraint **NOT NULL** hat (IDEAL-Schema). Der Nicht-IDEAL-Service (windows-auth/glas-Linie)
laeuft gegen genau diese DB.
1. Service-Sync ausloesen (Service neu starten oder den naechsten Sync-Zyklus abwarten).
   - **Erwartet:** KEIN Fehler **515** („Cannot insert the value NULL into column 'SubOrderNumber'") mehr.
2. Neue FAs pruefen: `SELECT OrderNumber, SubOrderNumber FROM ProductionOrders WHERE Id=<neu>`.
   - **Erwartet:** Neue FAs werden angelegt; `SubOrderNumber = OrderNumber`.
3. Aktivitaets-Protokoll (`/SyncLog`) oeffnen.
   - **Erwartet:** Der ProductionOrders-Lauf zeigt Erfolg (neu/aktualisiert-Counts, kein Fehler).
   - **Gegenprobe:** Gegen eine DB **ohne** `SubOrderNumber`-Spalte laeuft der Sync weiterhin fehlerfrei
     (der `COL_LENGTH`-Check laesst die Spalte im Upsert dann weg).

### TS-47.2 Sync-Resilienz — ein Fehler stoppt die anderen nicht
**Vorbedingung:** Ein Sync-Schritt wird gezielt zum Scheitern gebracht, z. B. die
ProductionOrders-Quell-View temporaer unerreichbar machen (Verbindung/Rechte entziehen), waehrend
Artikel-/OSEON-/BOM-Sync weiter aktiv und funktionsfaehig sind.
1. Einen vollen Sync-Zyklus laufen lassen.
   - **Erwartet:** Log/Aktivitaets-Protokoll zeigt den **ProductionOrders-Fehler**.
2. Die nachfolgenden Sync-Schritte im **selben Zyklus** pruefen (Artikel/OSEON/BOM …).
   - **Erwartet:** Sie laufen weiter und melden ihre eigenen Ergebnisse (frueher: ein Fehler
     im ProductionOrders-Sync stoppte alle Folge-Syncs des Zyklus).

### TS-47.3 Fehlermail bei Sync-Fehler
**Vorbedingung:** `ErrorNotification:Enabled=true`, gueltige `ErrorNotification:Recipients`
(mind. eine Adresse), SMTP konfiguriert.
1. Einen Sync-Fehler ausloesen (z. B. Quell-View wie in TS-47.2 unerreichbar).
   - **Erwartet:** Eine E-Mail mit Betreff **„[IDEAL-AKE-WMS] Sync-Fehler: &lt;Schritt&gt;"** trifft bei
     den Empfaengern ein. Inhalt enthaelt: betroffener **Schritt**, **Zeitpunkt**, **Maschine**,
     **Version** sowie **Fehlermeldung + Stacktrace**.
2. **Negativ a):** `ErrorNotification:Enabled=false` setzen und denselben Fehler ausloesen.
   - **Erwartet:** **KEINE** Mail wird gesendet (der Sync-Fehler wird weiterhin protokolliert).
3. **Negativ b):** `Enabled=true`, aber `Recipients` leer.
   - **Erwartet:** **KEINE** Mail wird gesendet; der Notifier wirft **keinen** Fehler
     (Sync-Zyklus laeuft unbeeintraechtigt weiter).

---

## Kapitel 48: Rolle `stock_read` (read-only Lagerbestand) + FA-Liste/Stückliste fuer Vorbau (v1.25.0)

**Vorbedingung global:** App laeuft; ein Admin kann Benutzer + Rollen zuweisen.

### TS-48.1 `stock_read` sieht nur Bestände + Bewegungshistorie (read-only)
**Vorbedingung:** Ein Benutzer, dem **NUR** die Rolle `stock_read` zugewiesen ist (keine weitere Rolle).
1. Als dieser Benutzer einloggen und das Hauptmenue oeffnen.
   - **Erwartet:** Das Dropdown **„Lager"** ist sichtbar und enthaelt **ausschliesslich** die Eintraege
     **„Bestände"** und **„Bewegungshistorie"**. **KEINE** Eintraege „Einbuchung", „Ausbuchung",
     „Umbuchung", „Lagerplatz ausbuchen", „Lagerplatz umbuchen" und kein Divider davor.
2. „Bestände" oeffnen (`/StockOverview`) und „Bewegungshistorie" oeffnen (`/StockMovements`).
   - **Erwartet:** Beide Seiten oeffnen sich read-only (Anzeige/Filter, keine Buchungs-Bedienelemente).
3. **Negativ (Direktaufruf Schreib-URLs):** Nacheinander `/StockMovements/Inbound`,
   `/StockMovements/Outbound`, `/StockMovements/OutboundAll` direkt aufrufen.
   - **Erwartet:** Jeweils **AccessDenied** (kein Zugriff — die Schreib-Actions tragen `[RequireStockAccess]`
     bzw. `[RequireStockKeyUserAccess]`).
4. **Gegenprobe (unveraendert):** Als `admin`, `stock`, `stock_keyuser` bzw. `picking` einloggen.
   - **Erwartet:** Das „Lager"-Dropdown zeigt weiterhin **alle** Eintraege (Ein-/Aus-/Umbuchung,
     Bestände, Bewegungshistorie, Lagerplatz-Sonderaktionen bei `stock_keyuser`/`picking`/`admin`);
     Buchungen funktionieren wie bisher.

### TS-48.2 FA-Liste + read-only Stückliste fuer `vorbau`
**Vorbedingung:** `FaCompletionAktiv=true`; ein Benutzer mit **nur** der Rolle `vorbau`.
1. Als dieser Benutzer einloggen und das Menue **„Fertigungsaufträge"** oeffnen.
   - **Erwartet:** Der Eintrag **„FA-Liste"** ist sichtbar; die FA-Liste (`/ProductionOrders`) oeffnet sich.
2. In einer FA-Zeile den **Stückliste-Button** anklicken.
   - **Erwartet:** Die read-only Stückliste `FaWorklist/Bom` oeffnet sich — **keine**
     Kommissionier- oder Erledigt-Bedienelemente (rein lesend, Druck moeglich).
3. **Gegenprobe Picker:** Als Benutzer mit Rolle `picking` einloggen und den Stückliste-Button oeffnen.
   - **Erwartet:** Es oeffnet sich weiterhin `Picking/Bom` (Picker-Variante), nicht die Vorbau-Sicht.

## Kapitel 49: Hauptlagerplatz am Artikel (v1.25.0)

**Feature:** Artikel haben einen Hauptlagerplatz. Wert primär aus Sage (dann gesperrt), sonst in der App setzbar. In allen Bestand-je-Lagerplatz-Anzeigen steht der Hauptlagerplatz zuerst; Bestandsübersicht zeigt ein ⭐-Badge.

### 49.1 Manuellen Hauptlagerplatz setzen (Sage liefert keinen)
**Vorbedingung:** Rolle admin oder masterdata. Ein Artikel ohne Sage-Hauptlagerplatz (`SagePrimaryStorageLocation` leer). Mindestens zwei aktive, nicht-Wagen-Lagerplätze existieren.
1. Stammdaten → Artikel → Artikel bearbeiten öffnen.
2. Feld **Hauptlagerplatz** ist ein editierbares Dropdown mit Leer-Option „—".
3. Einen Lagerplatz wählen, Speichern.
**Erwartet:** Erfolgsmeldung „Artikel gespeichert."; nach erneutem Öffnen ist der gewählte Lagerplatz vorausgewählt.

### 49.2 Sage-Lock (Hauptlagerplatz aus Sage gesperrt)
**Vorbedingung:** Ein Artikel, bei dem `SagePrimaryStorageLocation` gesetzt ist (nach einem Artikel-Sync mit gepflegtem `KHKArtikel.PlatzID`).
1. Artikel bearbeiten öffnen.
**Erwartet:** Das Hauptlagerplatz-Dropdown ist **disabled**; darunter eine blaue Info-Box „Hauptlagerplatz aus Sage übernommen (gesperrt): <Code>". Ein Änderungsversuch (z. B. per Browser-DevTools den Hidden-Wert ändern und posten) darf den Wert NICHT ändern — der Server ignoriert die eingehende Id.

### 49.3 Sortierung „Hauptlagerplatz zuerst" in der Bestandsübersicht
**Vorbedingung:** Ein Artikel mit Bestand auf mindestens zwei Lagerplätzen; Hauptlagerplatz = derjenige mit der ALPHABETISCH späteren/kleineren Menge (bewusst nicht der „natürliche" erste).
1. Bestand → Artikelbestände öffnen, nach dem Artikel filtern.
**Erwartet:** Die Zeile des Hauptlagerplatzes steht je Artikel ganz oben, unabhängig von Code-Alphabet und Menge; sie trägt ein gelbes Badge „★ Haupt". Die übrigen Lagerplatz-Zeilen folgen nach Code sortiert (kein Badge).

### 49.4 Sortierung im Kommissionier-Quellvorschlag
**Vorbedingung:** Ein FA mit einem Bauteil, das Bestand auf Hauptlagerplatz (buchbar, Menge > 0) und einem weiteren buchbaren Lagerplatz mit HÖHERER Menge hat.
1. Kommissionierung → Stückliste des FA öffnen.
**Erwartet:** Der vorgeschlagene Quell-Lagerplatz für dieses Bauteil ist der **Hauptlagerplatz** (nicht der mit der höchsten Menge). Im Quell-Dropdown (Suche) steht der Hauptlagerplatz an erster Stelle. Hat der Hauptlagerplatz keinen buchbaren Bestand, greift der bisherige Fallback (höchste Menge), sonst NAN.

### 49.5 Sortierung in „Lager: Fehlteile"
**Vorbedingung:** Ein Fehlteil-Item, dessen Artikel Bestand auf Hauptlagerplatz (kleinere Menge) + weiterem Lagerplatz (größere Menge) hat.
1. Lager → Lager: Fehlteile öffnen.
**Erwartet:** In der Lagerplatz-Spalte des Items steht der Hauptlagerplatz zuerst, auch bei geringerer Menge.

### 49.6 Fehlender WMS-Lagerplatz (Negativfall)
**Vorbedingung:** Sage liefert einen Hauptlagerplatz-Code (`Kurzbezeichnung`), zu dem KEIN WMS-`StorageLocation` mit passendem `Code` existiert.
1. Artikel-Sync laufen lassen (Windows-Service).
**Erwartet:** Der Artikel behält den Sage-Rohcode sichtbar + gesperrt (49.2), `PrimaryStorageLocationId` bleibt leer, keine Sortier-Priorisierung. Im Aktivitäts-Protokoll (Service „Article") ist der Count `hauptlagerplatz_fehlt` > 0; **zusätzlich erscheint im selben Article-Lauf je fehlendem Lagerplatz eine Warn-Detailzeile** „Hauptlagerplatz '<Code>' fuer Artikel <Nr> nicht als WMS-Lagerplatz gefunden — FK bleibt leer" (gecappt auf max. 100 Zeilen je Lauf; der `hauptlagerplatz_fehlt`-Count zählt aber ALLE). Im Service-Log (Serilog-Datei) steht dieselbe Warnung für alle (ungecappt). Nach Anlegen/Sync des fehlenden Lagerplatzes matcht der FK beim nächsten Lauf.

---

## Kapitel 50: FA-Reconciliation (verwaiste FAs stornieren) (v1.25.0)

**Vorbedingungen:**
- WMS-DB mit mindestens einer offenen FA (`IsDone=0`, `IsCancelled=0`), deren `OrderNumber` in der Sage-View `vw_AKE_Kommissionierung_WAListe` NICHT (mehr) vorkommt (= verwaiste FA).
- Service läuft, `Sync:ProductionOrdersEnabled=true`.
- DB-Backup vor dem ersten scharfen Lauf.

> **Hinweis Aktivitäts-Protokoll:** Die Reconcile läuft im ProductionOrder-Sync, erscheint aber als **eigener** Protokoll-Eintrag mit Service-Namen **`ProductionOrderReconciliation`** (im Protokoll-Filter-Dropdown auswählbar) — getrennt vom `ProductionOrder`-Eintrag (der nur noch `neu`/`aktualisiert` zählt). Die Counts `storniert`/`reaktiviert` stehen ausschließlich im `ProductionOrderReconciliation`-Eintrag.

### 1. DryRun-Kontroll-Lauf (Flag AUS — nichts wird geschrieben)
1. `Sync:ProductionOrderReconcileEnabled = false` (Default), `WorkerSettings:SyncDryRun = false`.
2. Sync-Zyklus auslösen (oder Intervall abwarten).
3. **Erwartet:** Es entsteht ein `ProductionOrderReconciliation`-Eintrag mit einer Info-Detailzeile „Deaktiviert (Sync:ProductionOrderReconcileEnabled=false) — Plan: N Storno-Kandidaten, M Reaktivierungen — nichts geschrieben" und Counts `storniert=0, reaktiviert=0, storno-kandidaten=N`. In der DB ist KEINE FA storniert. Der Admin liest N ab und prüft Plausibilität.

### 2. Scharfschalten — verwaiste FA wird storniert
1. `Sync:ProductionOrderReconcileEnabled = true`. `Sync:ReconcileMaxCancelPerRun` ausreichend hoch (Default 100).
2. Sync-Zyklus auslösen.
3. **Erwartet:** Die verwaiste FA hat `IsCancelled=1`, `CancelledAt`=jetzt (UTC), `CancelledBy='System-Reconcile'`. Der `ProductionOrderReconciliation`-Eintrag zeigt Counts `storniert=N` **und je storniertem FA eine Info-Detailzeile** „FA <Nr> storniert (in Sage nicht mehr vorhanden)" (Reference = FA-Nummer). Die FA verschwindet aus FA-Liste, Leitstand, FA-Vervollständigung, FA-Abarbeitungsliste, Picking-Worklist und aus dem BOM-Cache-Fenster.
4. FA-Liste mit „Erledigte anzeigen" öffnen → die FA erscheint mit rotem Badge **„In Sage gelöscht"** (nicht „erledigt").

### 3. Reaktivierung — FA taucht wieder in Sage auf
1. Die stornierte FA wieder in der Sage-View verfügbar machen (Testdaten).
2. Sync-Zyklus auslösen.
3. **Erwartet:** `IsCancelled=0`, `CancelledAt=NULL`, `CancelledBy=NULL`. Der `ProductionOrderReconciliation`-Eintrag zeigt Counts `reaktiviert=1` **und eine Info-Detailzeile** „FA <Nr> reaktiviert (wieder in Sage)". FA ist wieder in den offenen Sichten.

### 4. Guard — leerer Sage-Read storniert NICHTS
1. Sage-View liefert (simuliert) 0 Zeilen (z. B. View temporär leer / Verbindungsproblem am Read).
2. Sync-Zyklus mit Flag AN auslösen.
3. **Erwartet:** KEINE FA wird storniert. Im `ProductionOrderReconciliation`-Eintrag steht eine Warn-Detailzeile „Uebersprungen: Sage-Read leer". Keine Fehlermail (Guard ist kein Cap).

### 5. Cap — zu viele Storno-Kandidaten → kein Storno + Fehlermail
1. `Sync:ReconcileMaxCancelPerRun = 1`. Mehr als 1 verwaiste offene FA vorhanden.
2. `ErrorNotification:Enabled=true` + `ErrorNotification:Recipients` gesetzt.
3. Sync-Zyklus mit Flag AN auslösen.
4. **Erwartet:** KEINE FA storniert (Cap überschritten). Im `ProductionOrderReconciliation`-Eintrag steht eine Warn-Detailzeile „Uebersprungen: Cap ueberschritten (N > 1)". Fehlermail an die Empfänger mit Sage-Count + Cap. Etwaige Reaktivierungen im selben Lauf werden trotzdem geschrieben (mit je einer „FA <Nr> reaktiviert"-Detailzeile).

**Negativ/Regression:**
- Erledigte FAs (`IsDone=1`), die nicht in Sage sind, werden NIE storniert (nur offene).
- Bereits stornierte FAs, die weiterhin fehlen, werden nicht erneut storniert (kein Doppel-Storno, Counts bleiben 0).

---

## Kapitel 51: Typisierte, vollständige Service-Einstellungen (v1.25.0)

**Vorbedingung:** Als `admin` eingeloggt. Windows-Dienst läuft (für die Wirkungs-Checks).

### TS-51.1 — Vollständigkeit + Typisierung
1. `/ServiceSettings` öffnen.
2. **Erwartet:** Jede Einstellung erscheint typgerecht: Bool-Keys (z. B. `Sync:BomCacheEnabled`, `WorkerSettings:SyncDryRun`, `ErrorNotification:Enabled`) als **Aktiv/Inaktiv**-Schalter; Int-Keys (z. B. `Sync:BomCacheWeeks`, `WorkerSettings:SyncIntervalMinutes`) als **Zahlenfeld**; String-Keys (z. B. `Sync:FeiertagCountryCode`, `ErrorNotification:Recipients`) als **Textfeld/Textarea**. Alle Katalog-Keys sind sichtbar, nach Kategorie gruppiert (Sync, BOM-Cache, FA-Vervollstaendigung, Lackierteile, BDE, Feiertage, Worker, Fehlermail, Benachrichtigungen).

### TS-51.2 — Bool-Toggle speichern + Wirkung (nächster Sync)
1. `Sync:ProductionOrderReconcileEnabled` von Inaktiv auf **Aktiv** schalten → „Einstellungen speichern".
2. **Erwartet:** Erfolgs-Alert; nach Reload steht der Toggle auf Aktiv. In DB `[ServiceSettings]` Key = `Sync:ProductionOrderReconcileEnabled`, Value = `true` (NICHT `1`).
3. Nächsten Sync-Zyklus abwarten → im Aktivitäts-Protokoll erscheint der `ProductionOrderReconciliation`-Lauf scharf (nicht mehr „Deaktiviert").

### TS-51.3 — Int-Feld speichern + Wirkung
1. `WorkerSettings:SyncIntervalMinutes` von `15` auf `5` setzen → speichern.
2. **Erwartet:** Nach Reload steht `5`. Der Dienst taktet ab dem nächsten Loop mit 5 Minuten (Manual-UAT: wertabhängiger Sync-Pfad — nur über die DB-Lesestelle beobachtbar, nicht InMemory getestet).

### TS-51.4 — Int-Validierung (Negativfall)
1. In `Sync:BomCacheWeeks` `abc` eintippen (Zahlenfeld erlaubt das ggf. nur per Paste/DevTools — alternativ ein anderes Int-Feld leeren und Buchstaben einfügen) → speichern.
2. **Erwartet:** KEIN Erfolgs-Redirect; oben ein Warn-Alert „'Sync:BomCacheWeeks' erwartet eine ganze Zahl". Der Wert in der DB bleibt unverändert; gültige Felder im selben Submit wurden gespeichert.

### TS-51.5 — String/Textarea (Empfänger)
1. `ErrorNotification:Recipients` = `a@ake.at, b@ake.at` speichern.
2. **Erwartet:** Nach Reload steht der Komma-String im Feld. Bei einem provozierten Sync-Fehler (und `ErrorNotification:Enabled` = Aktiv) geht eine Fehlermail an beide Adressen (Split auf Komma). (Wertabhängiger Mail-Pfad = Manual-UAT.)

### TS-51.6 — Reconcile scharfschalten via Toggle
1. `Sync:ProductionOrderReconcileEnabled` = Aktiv (TS-51.2) UND eine verwaiste offene FA vorhanden (siehe Kap. 50).
2. Sync-Zyklus abwarten.
3. **Erwartet:** Die FA wird storniert (Wirkung der DB-Einstellung greift ohne appsettings-Änderung). Das beweist: der Bool-Wert wird vom Service aus der DB gelesen, nicht aus appsettings.

### TS-51.7 — DB gewinnt über appsettings
1. In `appsettings.json` des Dienstes `WorkerSettings:SyncIntervalMinutes` = `99` setzen, aber in `/ServiceSettings` `15` lassen.
2. **Erwartet:** Der Dienst taktet mit **15** (DB-Wert), nicht 99 — die appsettings-Zahl wird ignoriert.

### TS-51.8 — Sync-Block-Gate via Toggle scharfschalten (jetzt ALLE Gates DB-first)
1. Einen bisher deaktivierten Sync-Schalter in `/ServiceSettings` auf **Aktiv** setzen — z. B. `Sync:OseonTrackingEnabled` (Default false) — und speichern. In `appsettings.json` diesen Wert unverändert auf `false` lassen.
2. Nächsten Sync-Zyklus abwarten.
3. **Erwartet:** Der OSEON-Tracking-Sync läuft (Aktivitäts-Protokoll-Eintrag erscheint), obwohl `appsettings.json` `false` sagt — der DB-Toggle greift. Das beweist: seit v1.25.0-Followup lesen **alle** `Sync:*Enabled`-Block-Gates (nicht nur die 8 Spec-Keys) ihren Wert DB-first. (Wertabhängiger Sync-Pfad = Manual-UAT.)
4. **Negativ-Fall:** Denselben Schalter wieder auf **Inaktiv** setzen → nach dem nächsten Zyklus läuft der Sync nicht mehr, auch wenn `appsettings.json` `true` stünde.

**Deploy-Checkliste (WICHTIG):** Nach dem Deploy ist die `/ServiceSettings`-Seite die **EINZIGE** Steuerung für alle Sync-/Worker-/Feiertag-/Fehlermail-Einstellungen. Die Blöcke `Sync:*` (inkl. **aller** `*Enabled`-Block-Gates), `WorkerSettings:*`, `ErrorNotification:*` und die `Feiertag`-Keys aus `appsettings.json` werden vom Dienst **nicht mehr gelesen** — die DB gewinnt vollständig. `ConnectionStrings:*` + `MailSettings:*` (SMTP) bleiben `appsettings.json`.

→ **Jeden gewünschten Sync-Schalter nach dem Deploy einmalig in der GUI (`/ServiceSettings`) auf `Aktiv` setzen** — z. B. OSEON-Tracking (`Sync:OseonTrackingEnabled`), OSEON-Artikelkategorie (`Sync:OseonArticleCategoryEnabled`), enaio (`Sync:EnaioDmsEnabled`), Bedarfsmeldungs-Mail (`Sync:PartRequisitionEmailEnabled`), Lagerbestellungs-Mail (`Sync:WarehouseRequisitionEmailEnabled`), Lagerplätze (`Sync:LagerplaetzeEnabled`), Lagerbestand (`Sync:LagerbestandEnabled`), Feiertags-Sync (`Sync:FeiertagSyncEnabled`), FA-Arbeitsgang-Erkennung (`Sync:FaWorkStepDetectionEnabled`), BOM-Cache (`Sync:BomCacheEnabled`), Lackierteil-Erkennung (`Sync:CoatingDetectionEnabled`), FA-Reconcile (`Sync:ProductionOrderReconcileEnabled`) — **sonst laufen sie nach dem Deploy NICHT**, egal was in `appsettings.json` steht. Ebenso `ErrorNotification:Enabled` + `ErrorNotification:Recipients` kontrollieren (sonst gehen keine Fehlermails raus). `Sync:ProductionOrdersEnabled` + `Sync:ArticlesEnabled` haben Default `true` und laufen auch ohne DB-Eintrag weiter (Fail-Safe).

**Test-Coverage-Hinweis:** Katalog-Konsistenz + Drift-Guard (`ServiceSettingDefinitionsTests`) und der Speicher-Merge inkl. Int-Validierung (`ServiceSettingsControllerTests`) sind automatisiert grün. Für den `SyncWorker` sichern `SyncWorkerTests` die **Fail-Safe-Invarianten** ab (ohne erreichbare DB laufen die true-Default-Gates `ProductionOrders`/`Articles` weiter, ein IConfiguration-false-Seed schaltet sie nicht mehr ab, der Worker crasht nie). Die **wertabhängigen** Sync-Pfade (tatsächliches Worker-Takten mit dem DB-Wert, DB-getriebenes Scharfschalten der `Sync:*Enabled`-Block-Gates, Fehlermail-Versand an die DB-Empfänger, Reconcile-Scharfschaltung) laufen über DB-Lesestellen im laufenden Dienst und sind **Manual-UAT** (nicht InMemory testbar) — siehe TS-51.2/51.3/51.5/51.6/51.8.

---

## Kapitel 52: Lagerbestellung aus der Stückliste + Master-Schalter (v1.25.0)

### Vorbedingungen
- `LagerbestellungAktiv` = true (Einstellungen → Bestellungen).
- `GlasArtikelgruppen` (z. B. `GLAS`), `GemeinsameArtikelgruppen` (`EUZ`), `DefaultLagerbestellempfaengerId`, `DefaultGlasbestellempfaengerId` konfiguriert.
- Test-User A: Rolle `picking` (darf Lager UND Glas ordern) + mindestens eine Werkbank zugeordnet.
- Test-User B: nur Rolle `vorbau`, KEIN Bestell-Recht.
- Test-User C: nur Rolle `lagerbestellung` (Lager, nicht Glas).
- Eine offene FA mit Stückliste, darunter ein Lager-Artikel (z. B. Gruppe 940), ein Glas-Artikel (Gruppe GLAS), ein EUZ-Artikel.

### Szenario 1 — Button-Sichtbarkeit je Rolle
1. Als User A: Kommissionierung → FA → Stückliste öffnen.
   - Erwartet: Spalte „Lagerbestellung" mit Checkbox + Button je Nicht-Baugruppen-Zeile; Baugruppen-Zeilen leer.
2. Als User B (nur vorbau): FA-Abarbeitungsliste → Stückliste (read-only) öffnen.
   - Erwartet: KEIN Lagerbestellung-Button (kein Bestell-Recht), obwohl read-only-BOM.
3. Als User A: dieselbe read-only-BOM aus der Abarbeitungsliste (falls Rolle vorhanden) — Button sichtbar (unabhängig von ReadOnly).

### Szenario 2 — Einzelbestellung Lager (neuer Draft)
1. Als User A ohne offenen Lager-Draft: Lager-Artikel-Button klicken → Modal zeigt Artikel + editierbare Menge (BOM-Menge vorbelegt).
2. Menge bestätigen → „Zur Bestellung hinzufuegen".
   - Erwartet: Weiterleitung auf `/WarehouseRequisitions/Edit/{id}`, neuer Lager-Draft mit der Position.

### Szenario 3 — Zweite Position → selber Draft
1. Zweiten Lager-Artikel per Einzel-Button hinzufügen.
   - Erwartet: KEIN neuer Draft — dieselbe offene Bestellung, zweite Position.

### Szenario 4 — Glas-Ableitung
1. Glas-Artikel (Gruppe GLAS) per Button bestellen.
   - Erwartet: landet in einem GLAS-Draft (eigener Typ), Weiterleitung auf dessen Edit.
2. EUZ-Artikel bestellen → landet im Lager-Draft (gemeinsame Gruppe → Lager).

### Szenario 5 — Bulk (gemischt)
1. Lager-Artikel + Glas-Artikel markieren (Checkboxen) → „Lagerbestellung (Auswahl)" erscheint.
2. Bulk-Button → Modal listet beide mit Mengen → bestätigen.
   - Erwartet: EIN Lager-Draft + EIN Glas-Draft, Positionen korrekt verteilt; Weiterleitung auf `/WarehouseRequisitions` (Übersicht) + Meldung „Zu Lager: 1, zu Glas: 1 hinzugefügt".

### Szenario 6 — Rechte-Fehler / Übersprungen
1. Als User C (nur lagerbestellung, kein Glas-Recht): Glas-Artikel per Button bestellen.
   - Erwartet: Fehlermeldung „Nichts hinzugefuegt … Keine Glasbestell-Berechtigung", nichts angelegt.
2. Bulk mit Lager-Artikel + Glas-Artikel als User C.
   - Erwartet: Lager-Artikel hinzugefügt, Glas-Artikel in „Uebersprungen".

### Szenario 7 — Master-Schalter aus
1. Einstellungen → `LagerbestellungAktiv` = false → speichern.
2. Menü prüfen: „Lagerbestellungen", „Meine Fehlteile", „Lager: Eingehende Listen", „Lager: Fehlteile" sind weg; „Bedarfsmeldungen" nur sichtbar wenn `BestellungenAktiv` an.
3. Direkter Aufruf `/WarehouseRequisitions` → Redirect Home + Warnhinweis.
4. Stückliste öffnen → KEIN Lagerbestellung-Button/-Spalte.
5. `LagerbestellungAktiv` wieder auf true → alles wieder sichtbar.

### Stückliste-UX: Spalten umsortieren + Sticky-Auswahlleiste (v1.25.0)

#### TS-52.1 — Spalten in der Stückliste umsortieren
1. Als User A: Kommissionierung → FA → Stückliste öffnen.
2. Zahnrad „Spalten konfigurieren" öffnen → Spalte „Lagerbestellung" per Drag an die 2. Position ziehen → speichern/schließen.
   - Erwartet: „Lagerbestellung" steht jetzt an 2. Stelle; „Auswahl/Pick" und „Position" bleiben als Anker ganz vorne (nicht verschiebbar).
3. Seite neu laden (F5).
   - Erwartet: Die geänderte Spaltenreihenfolge bleibt erhalten.
4. Eine Baugruppen-Zeile auf-/zuklappen.
   - Erwartet: Die Baum-Hierarchie funktioniert unverändert; die Umsortierung hat die Einrückung/Verschachtelung nicht zerstört (es werden nur die Zellen je Zeile umgeordnet).
5. Read-only Vorbau-Stückliste (aus der FA-Abarbeitungsliste) öffnen → dieselbe Umsortierung ist ebenfalls möglich und bleibt nach Reload.

#### TS-52.2 — Sticky-Auswahlleiste in der Stückliste
1. Als User A in der Kommissionierungs-Stückliste: mehrere Lagerbestellungs-Checkboxen (`.warehouse-select`) markieren.
   - Erwartet: Oben klebende Auswahlleiste erscheint, zeigt „N markiert" und den Button „Lagerbestellung (Auswahl)".
2. In der Stückliste nach unten scrollen.
   - Erwartet: Die Auswahlleiste bleibt oben sichtbar (sticky), Zähler + Button bleiben erreichbar.
3. Alle Häkchen wieder entfernen.
   - Erwartet: Die Auswahlleiste verschwindet (bzw. zeigt keine Aktion mehr).
4. Read-only Vorbau-Stückliste: Lagerbestellungs-Zeilen markieren.
   - Erwartet: Sticky-Leiste + „Lagerbestellung (Auswahl)"-Button funktionieren ebenso (read-only-sicher, kein JS-Fehler durch fehlende Bedarfsmeldungs-Checkboxen).
5. Bei aktivierter Bedarfsmeldung (`BestellungenAktiv` = true, `!ReadOnly`): sowohl Bedarfsmeldungs- (`.picking-checkbox`) als auch Lagerbestellungs-Zeilen markieren.
   - Erwartet: Die Sticky-Leiste zeigt beide Abschnitte gleichzeitig — je Auswahl-Set eigener Zähler + eigener Sammel-Button (Bedarfsmeldung UND Lagerbestellung).

---

## Kapitel 53: Lagerbestand-Nullsetzen verwaister Paare (v1.25.0)

**Vorbedingungen:**
- Service läuft, `Sync:LagerbestandEnabled = true` (in `/ServiceSettings`), `WorkerSettings:SyncDryRun = false`.
- Mindestens ein Artikel mit WMS-Bestand > 0 auf einem **Sage**-Lagerplatz (`Source=Sage`, `IsActive=true`), dessen `(Artikel, Lagerplatz)`-Paar in der Sage-Bestand-Quelle NICHT (mehr) vorkommt (Sage liefert 0-Bestand-Zeilen gar nicht — die Zeile verschwindet).

> **Hinweis Aktivitäts-Protokoll:** Das Nullsetzen läuft im bestehenden `Lagerbestand`-Sync (kein eigener Protokoll-Eintrag). Der Counts-Schlüssel `nullgesetzt` zählt die auf 0 gesetzten Paare.

### 1. Verwaistes Paar wird auf 0 gesetzt
1. Bestand für ein Sage-aktives Paar (Artikel X @ Lagerplatz Y) im WMS aufbauen (z. B. +5 Einbuchung), das in Sage 0 ist (Sage liefert die Zeile nicht).
2. Sync-Zyklus auslösen.
3. **Erwartet:** Eine `SageAusbuchung` über 5 wird gebucht (WMS-Bestand auf Y danach 0), Note „Sage-Korrektur: in Sage nicht mehr vorhanden -> auf 0 gesetzt (WMS war 5)". Der `Lagerbestand`-Eintrag zeigt Count `nullgesetzt=1` und eine Info-Detailzeile „Bestand auf 0 gesetzt: <ArtNr> @ <Code> (WMS war 5)".

### 2. Guard — leerer Sage-Read nullt NICHTS
1. Sage-Bestand-Quelle liefert (simuliert) 0 Zeilen.
2. Sync-Zyklus auslösen.
3. **Erwartet:** KEINE Nullbuchung. Warn-Detailzeile „Nullsetzen uebersprungen: Sage-Read leer (0 Zeilen)". Keine Fehlermail (Guard ist kein Cap).

### 3. Cap — zu viele Kandidaten → kein Nullsetzen + Fehlermail
1. `Sync:LagerbestandNullsetzenMaxPerRun` niedrig setzen (z. B. 1), mehr als 1 verwaistes Paar vorhanden. `ErrorNotification:Enabled=true` + Empfänger gesetzt.
2. Sync-Zyklus auslösen.
3. **Erwartet:** KEINE Nullbuchung. Warn-Detailzeile „Nullsetzen uebersprungen: Cap ueberschritten: N > 1 — kein Nullsetzen". Fehlermail an die Empfänger.

### 4. Manueller/inaktiver Lagerplatz bleibt unberührt
1. Verwaistes Paar mit WMS-Bestand > 0 auf einem `Source=Manual`-Platz ODER einem inaktiven Sage-Platz.
2. Sync-Zyklus auslösen.
3. **Erwartet:** KEINE Nullbuchung für dieses Paar (nur Sage-aktive Paare werden genullt). NAN und Kommissionierwagen (Manual) sind ebenfalls ausgeschlossen.

### 5. Duplikat-Paar in Sage bleibt unberührt
1. Sage liefert dasselbe `(Artikel, Lagerplatz)`-Paar mehrfach (mehrdeutig).
2. Sync-Zyklus auslösen.
3. **Erwartet:** Das Paar wird als Duplikat übersprungen (Warn „mehrfach"), aber NICHT genullt (es ist in Sage vorhanden, nur mehrdeutig — `sagePresentKeys` wird aus den Roh-Zeilen gebaut).

### 6. DryRun schreibt nicht
1. `WorkerSettings:SyncDryRun = true`, sonst wie Szenario 1.
2. Sync-Zyklus auslösen.
3. **Erwartet:** KEINE Nullbuchung in der DB, aber `nullgesetzt=1` im Protokoll (Simulation).

**Negativ/Regression:**
- Bestehende Bestandskorrekturen (Delta ≠ 0 für in Sage vorhandene Paare) funktionieren unverändert.
- Netto-0-Paare (z. B. +5/−5) werden NICHT als verwaist genullt (managedStock filtert `qty != 0`).

---

## Kapitel 54: Aktivitäts-Protokoll-Bereinigung (v1.25.0)

**Vorbedingungen:**
- Service läuft. Die Bereinigung läuft im neuen `CleanupWorker` (24h-Takt); beim Service-Start läuft sie einmal an. Zum sofortigen Test den Service neu starten.
- Die Aufbewahrung wird über die Service-Einstellung `Cleanup:AktivitaetsprotokollAufbewahrungTage` gesteuert (Default 180, `0` = nie löschen), Kategorie „Bereinigung" in `/ServiceSettings`.
- Im Aktivitäts-Protokoll (`/SyncLog`) existieren Einträge mit unterschiedlichem Alter (Timestamp).

> **Hinweis Aktivitäts-Protokoll:** Der Bereinigungslauf erscheint als eigener Protokoll-Eintrag mit Service-Namen `CleanupAktivitaetsprotokoll` und Count `geloescht=<N>`. Der eigene Lauf-Eintrag ist neuer als der Stichtag und wird deshalb nicht selbst gelöscht (Self-cleaning).

### TS-54.1 Löschen aktiv
1. `Cleanup:AktivitaetsprotokollAufbewahrungTage = 30` setzen. Sicherstellen, dass Protokoll-Einträge älter als 30 Tage existieren (ggf. Timestamps in der DB manipulieren).
2. Service neu starten (bzw. einen CleanupWorker-Durchlauf abwarten).
3. **Erwartet:** Einträge älter als 30 Tage sind weg, jüngere bleiben. Ein neuer `CleanupAktivitaetsprotokoll`-Lauf-Eintrag mit `geloescht=<N>` und Suffix „Aufbewahrung 30 Tage, Stichtag <dd.MM.yyyy>" erscheint.

### TS-54.2 Deaktiviert
1. `Cleanup:AktivitaetsprotokollAufbewahrungTage = 0` setzen.
2. Service neu starten (bzw. Durchlauf abwarten).
3. **Erwartet:** KEIN Löschen. KEIN `CleanupAktivitaetsprotokoll`-Eintrag (deaktiviert = still).

### TS-54.3 DryRun
1. `WorkerSettings:SyncDryRun = true`, `Cleanup:AktivitaetsprotokollAufbewahrungTage = 30`. Einträge älter als 30 Tage vorhanden.
2. Service neu starten (bzw. Durchlauf abwarten).
3. **Erwartet:** NICHTS gelöscht (alle Einträge bleiben). Ein `CleanupAktivitaetsprotokoll`-Lauf-Eintrag mit Suffix „(DryRun — nichts geloescht)" und `geloescht=<Kandidatenzahl>` (= Zahl der Einträge, die gelöscht würden).

### TS-54.4 Editor
1. `/ServiceSettings` öffnen.
2. **Erwartet:** Die Kategorie „Bereinigung" zeigt das Int-Feld `Cleanup:AktivitaetsprotokollAufbewahrungTage`. Wert ändern und speichern → Wert wird persistiert (DB-first); der nächste CleanupWorker-Durchlauf nutzt den neuen Wert.

**Negativ/Regression:**
- Ein transienter DB-Fehler beim Lesen der Aufbewahrung würgt den Worker-Loop nicht ab (Default-Fallback via `GetIntSafeAsync`).
- Ein Fehler im Bereinigungslauf stoppt den CleanupWorker-Loop nicht (resilient) und löst optional eine Fehlermail aus (bei aktivierter `ErrorNotification`).

---

## Kapitel 55: FA-Zusatzinfos (Sage) (v1.26.0)

**Vorbedingungen:**
- Sage-View `dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen` existiert am Zielsystem und liefert
  Zeilen (`SELECT TOP 1 ...` — Spaltennamen `[WA Nummer]`, `Kaeltemittel`, `Ventil`,
  `[Ausfuehrung E/Z]`, `Maschine`, `Status`).
- **Deploy-Daten-Check:** Pruefen, dass KEIN bestehender FA-Vorbau-AG den Code `ALLGEMEIN` traegt
  (`SELECT * FROM dbo.WorkSteps WHERE UPPER(Code)='ALLGEMEIN'`) — ein Altbestand wuerde mit dem
  neuen Pseudo-Reiter kollidieren (Neuanlage ist seit v1.26.0 gesperrt).
- Web v1.26.0 deployed (Migration 81 gelaufen, `Sync:FaZusatzinfoEnabled` in `/ServiceSettings`
  sichtbar, Default AUS), Windows-Service v1.26.0 published.
- Rollen: `fa_completion` (Reiter ALLGEMEIN), `vorbau` (Abarbeitungsliste), `picking`/`leitstand`
  (FA-Liste/Leitstand), `admin` (ServiceSettings + Aktivitaets-Protokoll).
- `FaCompletionAktiv = true` (fuer FA-Vervollstaendigung + Abarbeitungsliste).

> **Hinweis Aktivitaets-Protokoll:** Der Sync erscheint als Eintrag `FaZusatzinfo` mit Counts
> `gelesen/neu/aktualisiert/uebersprungen/erledigt-gesetzt` (bei Cap-Skip zusaetzlich
> `erledigt-kandidaten`). „uebersprungen" = WA-Zeilen ohne FA im WMS (alte/erledigte WAs —
> normal, KEINE Warn-Zeile je WA). Erhoehte Skip-Counts nach einem FA-Import-Fehler sind
> KEIN Bug (heilt sich im Folgezyklus). „erledigt-gesetzt" = FAs, die der Lauf wegen
> Sage-Status verpackt/abgeholt automatisch auf Komm-Erledigt gesetzt hat — je FA eine
> Info-Detailzeile „FA <Nr> auf erledigt gesetzt (Sage-Status: <status>)" (Detailzeilen
> gecappt auf 100/Lauf, der Count zaehlt alle).

### TS-55.1 Gate aus → an (Erstlauf mit DryRun-Pflicht)
1. `/ServiceSettings`: `Sync:FaZusatzinfoEnabled` ist AUS. Einen Sync-Zyklus abwarten.
2. **Erwartet:** KEIN `FaZusatzinfo`-Eintrag im Aktivitaets-Protokoll.
3. **PFLICHT-Vorschritt (Reconcile-Muster):** `WorkerSettings:SyncDryRun = true` setzen,
   DANN das Gate auf AN stellen, Zyklus abwarten.
4. **Erwartet:** `FaZusatzinfo`-Eintrag mit Suffix `[DryRun]`. Count `erledigt-gesetzt`
   kontrollieren — das ist die Erstlauf-Aufraeum-Zahl (ALLE in der App offenen FAs, die in
   Sage bereits verpackt/abgeholt sind, wuerden geschlossen). Plausibel → weiter. Meldet der
   Lauf stattdessen `erledigt-kandidaten` + Warn-Zeile „Auto-Erledigt uebersprungen: N
   Kandidaten > Cap M ..." → Cap `Sync:FaZusatzinfoAutoDoneMaxPerRun` temporaer erhoehen
   ODER in Etappen scharfschalten.
5. DryRun aus → naechster Zyklus schreibt tatsaechlich: `ProductionOrderExtraInfo`-Zeilen
   (CreatedBy `FaZusatzinfoSync`) UND setzt die verpackt/abgeholt-FAs auf Komm-Erledigt
   (`ProductionOrderPickingStatus.IsDonePicking = 1`, ModifiedBy `FaZusatzinfoSync`).

### TS-55.2 DryRun
1. `WorkerSettings:SyncDryRun = true`, Gate AN. Zyklus abwarten.
2. **Erwartet:** `FaZusatzinfo`-Eintrag mit Suffix `[DryRun]` und ECHTEN Would-be-Counts
   (`neu`/`aktualisiert` wie ein Echtlauf sie schreiben wuerde) — aber KEINE neuen/geaenderten
   Zeilen in `ProductionOrderExtraInfo`.
3. DryRun aus → naechster Zyklus schreibt die Zeilen tatsaechlich.

### TS-55.3 View fehlt (Warn-Skip, keine Mail)
1. Auf einem System OHNE die View (bzw. View temporaer umbenennen): Gate AN, Zyklus abwarten.
2. **Erwartet:** `FaZusatzinfo`-Lauf endet REGULAER (kein Fehler-Status) mit Warn-Zeile
   „View ... nicht vorhanden — Sync uebersprungen" und Message-Suffix „View nicht vorhanden".
   KEINE Fehlermail (auch bei aktivierter `ErrorNotification`). Kein Mail-Spam alle 15 min.

### TS-55.4 WA ohne FA
1. Die View liefert eine WA-Nummer, die es im WMS nicht (mehr) gibt.
2. **Erwartet:** Zeile zaehlt als `uebersprungen`; KEINE Warn-Detailzeile je WA.

### TS-55.5 FA ohne Zusatzinfo
1. Einen FA oeffnen, dessen WA-Nummer die View NICHT liefert: FA-Vervollstaendigung →
   Reiter ALLGEMEIN.
2. **Erwartet:** Hinweis „Noch keine Zusatzinformationen aus Sage vorhanden." In
   Abarbeitungsliste/FA-Liste/Leitstand sind die 5 Zellen dieses FA leer.

### TS-55.6 Update-Fall
1. In Sage einen Wert aendern (z. B. Kaeltemittel), Zyklus abwarten.
2. **Erwartet:** Count `aktualisiert=1`; die Zeile traegt `ModifiedAt`/`ModifiedBy =
   FaZusatzinfoSync`; die Views zeigen den neuen Wert. Unveraenderte Zeilen behalten
   `ModifiedAt = NULL` (kein Blind-Update).

### TS-55.7 Gate an → aus (letzter Stand bleibt)
1. Gate AUS stellen, mehrere Zyklen abwarten.
2. **Erwartet:** Kein neuer `FaZusatzinfo`-Eintrag; die vorhandenen Zusatzinfos bleiben in
   allen Views sichtbar (kein Loeschen). Gleiches gilt fuer WAs, die aus der View
   verschwinden: letzter bekannter Stand bleibt stehen.
3. **Erwartet:** Auch die Auto-Erledigt-Automatik stoppt (Gate aus = kein Lauf = kein
   Erledigt-Setzen); bereits automatisch geschlossene FAs bleiben erledigt (Einweg).

### TS-55.8 Spalten-Defaults + Prefs
1. Als BESTANDS-User mit bereits GESPEICHERTEN Spalten-Einstellungen (Zahnrad je View
   mindestens einmal benutzt) FA-Liste und Leitstand oeffnen.
2. **Erwartet:** Die 5 neuen Spalten sind dort NICHT sichtbar (defaultHidden greift auch im
   Merge mit gespeicherten Prefs). Ueber das Zahnrad lassen sie sich einblenden; die Wahl
   ueberlebt einen Reload.
3. Die FA-Abarbeitungsliste oeffnen.
4. **Erwartet:** Dort sind die 5 Spalten SICHTBAR (Default eingeblendet). Spalten per Zahnrad
   aus-/einblenden → Einstellungen ueberleben den Reload (Prefs-Bug-Fix: vorher 400 der
   Prefs-API fuer ViewKey „FaWorklist"). Spaltenfilter (ENTER) auf `Kaeltemittel` etc.
   filtern serverseitig; in FA-Liste/Leitstand filtert ein per URL geteilter
   `?colf_kaeltemittel=...` auch bei ausgeblendeter Spalte (unsichtbar — dokumentiertes
   Verhalten).

### TS-55.9 Reiter ALLGEMEIN + reservierter Code
1. FA mit FA-Vorbau-AGs oeffnen (FA-Vervollstaendigung → Bearbeiten).
2. **Erwartet:** Reiter ALLGEMEIN steht VOR den AG-Reitern (ohne ✓/●-Indikator); aktiv ist
   weiterhin der ERSTE AG-Reiter. Klick auf ALLGEMEIN zeigt die read-only `dl` (Kaeltemittel,
   Ventil, Ausfuehrung E/Z, Maschine, Sage-Status), keine Eingabefelder. `?tab=allgemein`
   (klein) funktioniert ebenfalls.
3. Werkbank im Kopf aendern, waehrend ALLGEMEIN aktiv ist.
4. **Erwartet:** Nach dem Speichern bleibt ALLGEMEIN der aktive Reiter (Tab-Erhalt).
5. FA OHNE FA-Vorbau-AGs oeffnen.
6. **Erwartet:** ALLGEMEIN ist der aktive (einzige) Reiter; darunter erscheint zusaetzlich der
   Hinweis „+ FA-Vorbau-AG hinzufuegen".
7. Stammdaten → FA-Vorbau-AG → Neu: Code `ALLGEMEIN` (auch `allgemein`) anlegen.
8. **Erwartet:** Validierungsfehler „Code 'ALLGEMEIN' ist reserviert ..." — kein Anlegen;
   dasselbe beim Umbenennen eines bestehenden AGs auf `ALLGEMEIN`.

### TS-55.10 Auto-Erledigt bei verpackt/abgeholt (inkl. Teilkommissionierung)
1. Einen in der App OFFENEN FA waehlen (nicht erledigt, nicht storniert), dessen WA in Sage
   den Status „verpackt" ODER „abgeholt" traegt. Gate AN (nach TS-55.1-DryRun-Kontrolle),
   Zyklus abwarten.
2. **Erwartet:** Der FA verschwindet aus FA-Liste, Komm-Worklist, FA-Vervollstaendigung und
   FA-Abarbeitungsliste (wie beim manuellen Abschliessen-Button). Aktivitaets-Protokoll:
   Count `erledigt-gesetzt` >= 1 + Info-Zeile „FA <Nr> auf erledigt gesetzt (Sage-Status:
   <status>)". In FA-Liste/Leitstand unter „Erledigte anzeigen" ist der FA sichtbar; die
   einblendbare Spalte „Sage-Status" zeigt den Grund.
3. Teilkomm-Fall: einen FA mit bereits auf den Kommissionierwagen gebuchten Teilen verwenden.
4. **Erwartet:** Auch dieser FA wird geschlossen; die gebuchten Teile bleiben auf dem Wagen
   stehen (bewusste Semantik, wie beim manuellen Abschluss).
5. IDEAL-Linie (mehrere Sub-FAs je WA): ALLE offenen Sub-FAs der WA werden geschlossen
   (N Counts + N Detailzeilen).

### TS-55.11 Einweg-Semantik + Ping-Pong
1. FA aus TS-55.10: In Sage faellt der Status zurueck (z. B. wieder „in Produktion").
   Zyklus abwarten.
2. **Erwartet:** Der FA BLEIBT erledigt (Einweg — kein Auto-Reopen); `aktualisiert` zaehlt
   nur den Satellit-Feldwechsel.
3. Bereits erledigte FAs (Sage-`IsDone` ODER Komm-Erledigt) und stornierte FAs
   (`IsCancelled`) mit Sage-Status verpackt/abgeholt.
4. **Erwartet:** KEIN `erledigt-gesetzt`-Count fuer diese FAs (kein Doppel-Setzen;
   Stornierte bleiben unangetastet).
5. Offener FA mit anderem Status (z. B. „in Produktion") → kein Erledigt-Setzen.
6. Ping-Pong: Einen automatisch geschlossenen FA manuell wieder oeffnen (FA-Liste/Leitstand
   → „Erledigte anzeigen" → Erledigt-Toggle); Sage meldet weiterhin „abgeholt". Zyklus
   abwarten.
7. **Erwartet:** Der naechste Lauf schliesst den FA ERNEUT (bewusst, kein Suppress-Marker —
   dauerhaftes Offenhalten erfordert einen Sage-Statuswechsel).

### TS-55.12 BDE-Sperre: Start/Resume gesperrt + Terminal-Meldung + Listen-Hygiene
Vorbedingung: `BdeAktiv = true`, BDE-aktive Werkbank, FA mit AG an dieser Werkbank, FA traegt
`SageStatus` „verpackt" oder „abgeholt" (Zusatzinfo-Satellit vorhanden).
1. Am BDE-Terminal den AG des gesperrten FA per FA-/AG-Scan aufrufen und „Ruesten starten"
   bzw. „Produktion starten" klicken.
2. **Erwartet:** Roter Toast „FA <Nr> ist bereits <status> — keine BDE-Buchung mehr
   moeglich." KEINE neue Buchung (Buchungsuebersicht kontrollieren).
3. Eine VOR der Statusaenderung pausierte Buchung des FA ueber das Paused-Panel fortsetzen.
4. **Erwartet:** Gleiche Meldung; keine neue Buchung, die pausierte bleibt pausiert.
5. Terminal-Auswahllisten pruefen (normaler AG-Modus UND NurFA-Modus).
6. **Erwartet:** Der gesperrte FA/AG erscheint NICHT mehr in den Auswahllisten; FAs ohne
   Zusatzinfo-Satellit und FAs mit anderem Status bleiben enthalten. Das alte Tracking-Modul
   (Teileverfolgung → ByWorkplace) zeigt die AGs des gesperrten FA WEITERHIN (bewusst
   ungefiltert, Default-Parameter).
7. FA ohne Zusatzinfo-Daten (Sync aus / Satellit fehlt): Start wie bisher moeglich (keine
   Sperre).

### TS-55.13 BDE-Sperre: laufende Buchung bleibt abschliessbar
1. Buchung auf einem FA starten; DANACH wird der FA in Sage „verpackt" (Sync-Zyklus
   abwarten).
2. **Erwartet:** Teilmengen melden, Pausieren und Beenden (inkl. finaler Mengen-Eingabe)
   funktionieren weiter — kein gestrandeter Operator. Nur NEUE Starts/Fortsetzungen sind
   gesperrt.
3. Ungeplante Taetigkeit (z. B. Wartung) an derselben Werkbank starten.
4. **Erwartet:** Startet normal (werkbank-bezogen, ohne FA — keine Sperre).
5. Laufendes Ruesten auf dem FA, dann „Produktion starten" klicken.
6. **Erwartet:** Meldung wie TS-55.12; das Ruesten LAEUFT WEITER (der abgelehnte Start
   schliesst es NICHT — Guard liegt vor der Auto-Close-Transition).

**Recovery (Fehlerfall Auto-Erledigt):** Hat ein View-Defekt trotz Cap FAs faelschlich
geschlossen, laesst sich der Stand zurueckdrehen:
`UPDATE ProductionOrderPickingStatus SET IsDonePicking = 0 WHERE ModifiedBy = 'FaZusatzinfoSync' AND ModifiedAt >= '<Zeitfenster>'`
(Zeitfenster = Beginn des fehlerhaften Laufs; danach zuerst die Ursache beheben — sonst
schliesst der naechste Lauf erneut.)

**Negativ/Regression:**
- Ein Fehler im FA-Zusatzinfo-Sync (z. B. Sage nicht erreichbar) stoppt die uebrigen Syncs des
  Zyklus NICHT (`RunResilientAsync`) und loest bei aktivierter `ErrorNotification` eine
  Fehlermail aus; der `FaZusatzinfo`-Lauf steht auf „fehlgeschlagen".
- Leitstand/FA-Liste: SQL-Pagination bleibt bei aktivem Zusatzinfo-Spaltenfilter erhalten
  (kein Force-Full-Load — im Gegensatz zu Datums-/VK-VA-Filtern).

---

## Kapitel 56: Sage-Lagerbuchungen (ausgehend, WMS → Sage via SData) (v1.28.0)

**Kontext:** Manuelle Ein-/Ausbuchungen im WMS werden über eine Queue asynchron an die
Sage-SData-API gemeldet. Gating kumulativ: globaler ServiceSetting `SageLagerbuchungAktiv` **und**
Lagerplatz-Flag `SageBuchungErlaubt`. Verarbeitung durch den `SageBookingWorker` (eigener Kurztakt).
**Der HTTP-/SData-Sende-Pfad ist nicht InMemory-testbar → echte Buchung am Sage-Testsystem
(Manual-UAT).** Automatisiert abgedeckt sind nur `SageLagerbuchungPayloadBuilder`,
`SageBookingEnqueueDecision`, der Enqueue-Decorator (InMemory) und `SageBookingCorrelation`.

**Vorbedingungen (einmalig):**
- Migration 82/83 eingespielt (`StorageLocations.SageBuchungErlaubt/SageLagerkennung/SageLagerplatzId`,
  Tabelle `SageBookingQueueItems`).
- `IDEALAKEWMSService/appsettings.json` → Block `SageLagerbuchung:Username/Password` am Server gesetzt.
- `/ServiceSettings` → `SData:BaseUrl` (Host+Port ohne Pfad, z.B. `https://sagetest01.ake.at:5493`),
  `SData:Application` (Default `ol`), `SData:ServiceContract` (Default `CommonWawiServices`),
  `SData:Dataset` (Mandant, z.B. `ake_TEST2026;1`) gesetzt; `SageLagerbuchungAktiv` zunächst aus.
  Ziel-URL = `{BaseUrl}/sdata/{Application}/{ServiceContract}/{Dataset}/$service/LagerbuchungService`
  (Semikolon im Dataset und `$` der Resource bleiben literal — nicht kodieren).
- Lagerplatz-Sync (`Sync:LagerplaetzeEnabled`) mindestens einmal gelaufen, damit Sage-Plätze
  `SageLagerkennung` (= Code) und `SageLagerplatzId` (= `KHKLagerplaetze.PlatzID`) tragen.
- **TLS am Testsystem:** `sagetest01.ake.at` hat aktuell ein ungültiges Zertifikat (`PartialChain`,
  interne PKI unfertig). Für die Sende-Szenarien (TS-56.3/56.4/56.7/56.8) am Testsystem daher
  `/ServiceSettings` → `SageLagerbuchungSslZertifikatPruefen=false` setzen — sonst scheitert der
  SData-POST an einem TLS-Fehler (`SageResponseRaw`/`LastError` zeigen den Zertifikatsfehler). Siehe
  TS-56.12. **Vor Produktivgang zwingend wieder `true`** (TS-56.13).

- **TS-56.1 — Globaler Toggle aus, Lagerplatz-Flag an: keine Buchung.** Vorbedingung:
  `SageLagerbuchungAktiv=false`, Ziel-Lagerplatz `SageBuchungErlaubt=true`. Schritt: manuelle
  Einbuchung. Erwartung: **kein** Eintrag in `/SageBookingQueue`.
- **TS-56.2 — Globaler Toggle an, Lagerplatz-Flag aus: keine Buchung.** Spiegelbildlich zu 56.1.
- **TS-56.3 — Beide an: Einbuchung durchläuft offen → gesendet → bestätigt.** Vorbedingung:
  Sage-Testsystem erreichbar, gültige Credentials. Schritt: Einbuchung, Worker-Tick abwarten
  (`Sync:SageLagerbuchungIntervalSeconds`). Erwartung: Eintrag wird `Bestätigt`; Buchung im
  Sage-Testsystem als **Zugang** sichtbar (echte Buchung, Manual-UAT).
- **TS-56.4 — Ausbuchung spiegelbildlich.** Wie 56.3, Sage-Buchung als **Entnahme**, Herkunft/Ziel
  vertauscht.
- **TS-56.5 — Sage-Korrektur erzeugt keinen Ping-Pong.** Vorbedingung: `LagerbestandSyncService`
  aktiv, erzeugt `SageEinbuchung`/`SageAusbuchung`. Erwartung: **kein** Queue-Eintrag dafür, auch
  bei beiden Toggles an (Feedback-Loop-Schutz, AK6 — automatisiert im Enqueue-Decorator-Test).
- **TS-56.6 — Fehlende Sage-Referenz (Konfigurationsfehler).** Vorbedingung: `SageBuchungErlaubt=true`
  auf einem **manuellen** Lagerplatz (ohne `SageLagerkennung`/`SageLagerplatzId`). Erwartung:
  Queue-Eintrag landet auf **Fehler** mit sprechender Meldung; Worker läuft weiter (kein Absturz);
  Folgeeinträge werden verarbeitet (AK9).
- **TS-56.7 — Timeout/Doppelbuchungs-Schutz.** Vorbedingung: Sende-Timeout simulieren (Netzwerk kurz
  kappen). Erwartung: Status bleibt `Gesendet`; beim nächsten Tick **kein** zweiter Blind-Send —
  der Recovery-Pfad prüft per Sage-Memo-Lookup (`KHKLagerplatzbuchungen`), ob die Buchung existiert
  (AK10, B2). **Dev-Lauf-Verifikation:** exakte Korrelationsspalte (`Memo` vs. `Referenz`) am
  Testsystem bestätigen; ggf. eine Zeile in `SageBuchungLookupReader` umstellen.
- **TS-56.8 — Requeue eines Fehler-Eintrags.** Schritt: einen `Fehler`-Eintrag in `/SageBookingQueue`
  über „Erneut senden" (Rolle `stock_keyuser`) einreihen. Erwartung: Status wird `Offen`, Worker
  prüft vor dem Senden per Memo-Lookup; existiert die Buchung in Sage bereits → `Bestätigt` (kein
  zweites Buchen), sonst wird gesendet.
- **TS-56.9 — Reconciliation-Sweep.** Vorbedingung: eine Ein-/Ausbuchung auf Sage-freigegebenem
  Platz ohne Queue-Eintrag (Enqueue-Fehler simulieren, z. B. während eines DB-Aussetzers). Erwartung:
  Der Worker reiht sie im Rückblickfenster (15 Min) nachträglich ein und sendet sie (B4).
- **TS-56.10 — Aktivitäts-Protokoll.** Nach einem Lauf `/SyncLog` öffnen. Erwartung: Lauf
  „SageLagerbuchung" mit Counts `gesendet`/`bestätigt`/`fehler`/`nacherfasst`/`uebersprungen`.
- **TS-56.11 — Regression bei Toggle aus (S7).** Vorbedingung: `SageLagerbuchungAktiv=false`.
  Erwartung: Ein-/Aus-/Umbuchung, Bestand, Historie, Kommissionierung verhalten sich **exakt** wie
  vor dem Update (der Decorator ist reiner Pass-through) — automatisiert im Enqueue-Decorator-Test,
  manuell stichprobenartig gegengeprüft.
- **TS-56.12 — TLS-Zertifikatsschalter (Testsystem, v1.28.0).** Der Schalter
  `SageLagerbuchungSslZertifikatPruefen` (`/ServiceSettings`, Default `true`) wirkt **nur** auf den
  Sage-Lagerbuchungs-Client. Schritte/Erwartung:
  - **a) Aus → Buchung läuft:** `SageLagerbuchungSslZertifikatPruefen=false` setzen, dann TS-56.3
    wiederholen. Erwartung: Der SData-POST an `sagetest01.ake.at` (ungültiges Zertifikat) läuft nun
    durch (`Bestätigt`), obwohl das Zertifikat ungültig ist.
  - **b) Sichtbarkeit bei aus:** Der Warnhinweis erscheint an **drei** Stellen — Worker-Start-Log
    („TLS-Zertifikatsprüfung … ist DEAKTIVIERT"), `/ServiceSettings` (Warn-Box am Eintrag) und
    `/SageBookingQueue` (Warn-Banner oben). Kein stilles Kästchen.
  - **c) An → greift ohne Neustart:** Schalter wieder auf `true` setzen (Dienst **nicht** neu starten),
    erneut buchen. Erwartung: Der nächste Sende-Versuch scheitert wieder am Zertifikatsfehler
    (`LastError`/`SageResponseRaw`) — die Änderung greift zur Laufzeit (spätestens mit einer neuen
    Verbindung). Negativfall: fehlt der Wert oder ist er unparsebar, wird **geprüft** (fail-safe).
- **TS-56.13 — Vor Produktivgang.** Prüfen, dass `SageLagerbuchungSslZertifikatPruefen` auf **`true`**
  steht (und kein Warnhinweis mehr erscheint), bevor gegen das Produktiv-Sage gebucht wird.

**Negativ/Regression:**
- Ein Fehler im Sende-Pfad (Sage nicht erreichbar) stoppt die übrigen Worker/Sync-Blöcke NICHT
  (eigener Worker, isolierte Kapselung) und löst bei Überschreitung von
  `Sync:SageLagerbuchungMaxErrorsPerRun` eine Fehlermail aus.
- Nach `Sync:SageLagerbuchungMaxRetries` automatischen Fehlversuchen bleibt der Eintrag auf `Fehler`
  und wird erst durch manuelles Requeue erneut versucht.
- **Ein-Instanz-Voraussetzung:** Der Idempotenz-Baustein schützt nur bei **genau einer** laufenden
  `SageBookingWorker`-Instanz (kein Doppel-Deploy/Failover auf derselben Queue).

---

## Kapitel 57: IDEAL Teil 1 — Struktur-Import FaHierarchy (mehrstufiger FA-Import)

**Kontext:** Reiner Backend-/Sync-Grundstein — keine erreichbare UI-Route. Der
`FaHierarchySyncService` (Windows-Service) liest die zwei IDEAL-Sage-Views
`vw_IDEAL-AKE_Kommissionierung_FAListe`/`..._FAInfos` und befuellt per Full-Refresh die lokalen
Cache-Tabellen `FaHierarchyNodes`/`FaHierarchyOrderInfos`. Gated ueber
`Sync:HierarchicalFaEnabled` (Default aus). Primaerweg: RCSI AN → `DELETE FROM` beide Tabellen +
Neubefuellung in einer Transaktion.

**Vorbedingung:** Zugriff auf das IDEAL-Testsystem (`AKESQL20.ake.at` / `IDEAL_TEST_2026_05_03`),
`Sync:HierarchicalFaEnabled = true`, View-Namen korrekt konfiguriert, `SageConnection` gesetzt.

- **TS-57.0 — RCSI-Check (entscheidet den Full-Refresh-Weg):** Vor dem Migrations-Lauf
  `SELECT is_read_committed_snapshot_on FROM sys.databases WHERE name = DB_NAME();` ausfuehren.
  Ergebnis `1` (erwarteter Regelfall) → Primaerweg, Migration legt nur die zwei Zieltabellen an,
  kein `GRANT ALTER`. Ergebnis `0` → Fallback-Entscheidung (RCSI aktivieren oder Staging/Swap;
  ausserhalb des Standard-Scopes von Teil 1).
- **TS-57.1 — Erstimport:** Toggle aktiv, Service-Lauf ausloesen. Erwartet: erfolgreicher,
  protokollierter Lauf (Aktivitaets-Protokoll, Counts `neu`/`geloescht` plausibel),
  `FaHierarchyNodes`/`FaHierarchyOrderInfos` gefuellt.
- **TS-57.2 — Datenverfuegbarkeits-Regel (Existenzpruefung):** Eine bekannte Struktur ohne
  `FAInfos`-Eintrag darf **nicht** in `FaHierarchyNodes` erscheinen.
- **TS-57.3 — Mehrstufigkeit:** Struktur mit Sub-Sub-FA pruefen — `VaterFA`-Kette laesst sich bis
  zur Wurzel (`VaterFA IS NULL`, `SubFA = HauptFA`) zurueckverfolgen.
- **TS-57.4 — Kombinationsgeraet, keine Verdopplung:** `HauptFA` mit **zwei** `MontageAbteilung`-
  Werten in `FaHierarchyOrderInfos` identifizieren. Erwartet: Positionszahl in `FaHierarchyNodes`
  fuer diesen `HauptFA` **identisch** zur Roh-View-`FAListe`-Zeilenzahl (keine Verdopplung durch die
  EXISTS-Semantik); `FaHierarchyOrderInfos` enthaelt dagegen **beide** Montage-Abteilungs-Zeilen.
- **TS-57.5 — Full-Refresh ohne Blocking (Primaerweg, RCSI AN):** Waehrend eines Laufs
  (`DELETE`+Neubefuellung in einer Transaktion) einen Read gegen `FaHierarchyNodeRepository`
  ausloesen — erwartet: sofortige Antwort mit dem **alten** Datenstand fuer die gesamte Laufzeit,
  kein Blocking/Timeout (RCSI-Snapshot).
- **TS-57.6 — Sage-Boolean-Mapping:** An einer Test-Struktur mit beschichteten und nicht-
  beschichteten Positionen pruefen: `Beschichtet = -1` → `Beschichtet = 1` (bit true) im WMS;
  `Beschaffungsartikel = 'Ja'` → true; `EKBedarf` analog.

**Positiv (Whitelist):**
- **TS-57.7 — Realer View-Name mit Bindestrich:** `Sync:FaHierarchyListeViewName` auf dem Default
  `[vw_IDEAL-AKE_Kommissionierung_FAListe]` belassen, Lauf ausloesen. Erwartet: Whitelist
  akzeptiert (kein `FinishFailedAsync`), SQL laeuft gegen die echte View. (Automatisiert abgedeckt:
  `FaHierarchySqlTests` mit den realen View-Namen als Positivfall.)

**Negativ:**
- **TS-57.8 — Ungueltiger View-Name:** `Sync:FaHierarchyListeViewName` auf einen Wert mit
  Semikolon/`--`/Leerzeichen/kyrillischem Homoglyph setzen, Lauf ausloesen. Erwartet:
  fehlgeschlagener, protokollierter Lauf (`FinishFailedAsync`) **ohne** SQL-Ausfuehrung gegen die
  Sage-DB **und** eine Fehlermail. (Automatisiert abgedeckt: `FaHierarchySqlTests`-Negativfaelle.)
- **TS-57.9 — Leerer View-Read:** Eine der beiden Views testweise leer liefern, Lauf ausloesen.
  Erwartet: Warn-Eintrag im Aktivitaets-Protokoll, Fehlermail, `FaHierarchyNodes`/
  `FaHierarchyOrderInfos` **unveraendert** (kein Replace; Guard prueft ungefilterte Rohzahlen vor
  jeder Mutation).
- **TS-57.10 — Toggle aus (Default):** `Sync:HierarchicalFaEnabled = false` → kein neuer Sync-Block
  wird ausgefuehrt, keine Fehlermeldung, Service laeuft unveraendert.
- **TS-57.11 — Regression:** Alle bestehenden AKE-Testszenarien (FA-Liste, Kommissionierung, BDE)
  unveraendert; `ProductionOrders` byte-identisch unveraendert.

---

## TS-58 — IDEAL Teil 2: FA-Baumanzeige (`FaHierarchyController.Index`)

Rekursive, mehrstufige Struktur-/Baumanzeige auf den in Teil 1 importierten Tabellen
`FaHierarchyNodes`/`FaHierarchyOrderInfos`. Read-only, Web-only (keine Migration, kein Service).
Route: `/FaHierarchy`. Zugriffsfilter (Class-Level): `[RequirePickingOrTrackingOrLeitstandAccess]`
— identisch zu `ProductionOrdersController`.

**Vorbedingungen:** Teil-1-Sync gelaufen, `FaHierarchyNodes`/`FaHierarchyOrderInfos` gefuellt
(mindestens eine Struktur mit Sub-Sub-FA-Ebene). Benutzer mit `picking`- ODER `tracking`- ODER
`leitstand`- (oder `admin`-) Rolle.

- **TS-58.1 — Grunddarstellung mehrstufig:** `/FaHierarchy` aufrufen. Erwartet: Strukturen als
  Karten (Kopfdaten + Baum). Eine bekannte Struktur mit Sub-Sub-FA laesst sich vollstaendig
  aufklappen (Haupt-FA → Sub-FA → Sub-Sub-FA, mehr als zwei Ebenen). Default: Knoten **eingeklappt**;
  „Alle aufklappen"/„Alle zuklappen" und die Chevrons je Knoten funktionieren.
- **TS-58.2 — Struktur-Pagination:** Seitengroesse 25/50/100/Alle wechseln. Erwartet: `TotalCount`
  zaehlt **Strukturen** (Wurzeln + Waisen-Pseudowurzeln), nicht Knoten-Zeilen; keine Struktur wird
  ueber zwei Seiten getrennt; jede Struktur erscheint vollstaendig auf genau einer Seite.
- **TS-58.3 — Struktur-Filter (server-seitig):** Filtertext, der nur auf einen tief liegenden Knoten
  passt, eingeben und „Filtern". Erwartet: die **ganze** Struktur erscheint (nicht nur der
  Treffer-Knoten); eine Struktur ohne jeden Treffer (weder Kopf noch Knoten) verschwindet komplett
  von der Seite. `HauptFA`- und `Kunde`-Feld filtern zusaetzlich (UND-Verknuepfung).
- **TS-58.4 — Client-Hervorhebung + Auto-Expand:** Im Feld „Knoten hervorheben" einen Wert eingeben,
  der auf einen eingeklappten Knoten passt. Erwartet: Treffer werden hervorgehoben, **kein** Knoten
  verschwindet aus dem DOM. Bei User-Setting **`RecursiveFilterSearch` = an** wird der Pfad zum
  Treffer automatisch aufgeklappt; bei **aus** bleibt er eingeklappt (nur Hervorhebung).
- **TS-58.5 — Zyklen-/Tiefen-Test:** Testdatensatz mit kuenstlichem Zyklus (`VaterFA` zeigt im Kreis)
  bzw. Tiefe > Cap praeparieren. Erwartet: **nur** diese eine Struktur wird als „Fehlerhaft"
  markiert (Badge + Hinweis, nicht leer), alle uebrigen Strukturen bleiben normal sichtbar; eine
  `ILogger`-Warnung (mit `HauptFA` + Abbruchstelle `SubFA`) wird geschrieben; ein sichtbares
  UI-Signal (`TempData["WarningMessage"]`-Banner) erscheint. **Keine** Mail, **kein** `SyncLog` aus
  dem Web. (Automatisiert abgedeckt: `FaHierarchyTreeBuilderTests` — Zyklus + Tiefen-Cap.)
- **TS-58.6 — Tiefen-Cap konfigurierbar:** `AppSettings`-Key `FaHierarchyMaxTiefe` setzen (z. B. 3).
  Erwartet: Strukturen tiefer als der Wert werden als fehlerhaft markiert; ohne DB-Zeile gilt der
  Default 500.
- **TS-58.7 — Waisen-Test:** Testdatensatz, dessen `VaterFA` auf keinen importierten Parent zeigt.
  Erwartet: erscheint als eigene, als „Verwaist — VaterFA <Wert> nicht gefunden" markierte
  Pseudo-Wurzel-Struktur; zaehlt als eigene Struktur fuer die Pagination — kein stilles Verschwinden.
  (Automatisiert abgedeckt: `FaHierarchyTreeBuilderTests` — Waise.)
- **TS-58.8 — Kombigeraet-Kopfdaten (mehrdeutig):** HauptFA mit mehreren `FaHierarchyOrderInfo`-
  Zeilen (mehrere `MontageAbteilung`). Erwartet: **alle** Kopfzeilen im Klartext (Tabelle im
  Strukturkopf) + Badge „mehrdeutig (Kombigeraet)". Kein Positions-Split, kein Fan-out.
  (Automatisiert abgedeckt: `FaHierarchyTreeBuilderTests` — ambiguous.)
- **TS-58.9 — Zugriffstest:** Benutzer **ohne** `picking`/`tracking`/`leitstand`/`admin` ruft
  `/FaHierarchy` auf → abgewiesen (403/Redirect wie bei `ProductionOrders`). Benutzer **mit** einer
  der Rollen sieht die Baumanzeige.
- **TS-58.10 — Regression:** `ProductionOrders`/AKE-FA-Liste unveraendert; keine Schema-/Service-
  Aenderung.

---

## TS-59 — IDEAL Teil 3: Kommissionierlisten (`FaHierarchyKommissionierListenController`)

Filterbare, druckbare Kommissionierlisten je Kommissionier-Ziel (`Kommissionieren`) auf den in
Teil 1 importierten Tabellen `FaHierarchyNodes`/`FaHierarchyOrderInfos`. Read-only, Web-only (keine
Migration, kein Service). Route: `/FaHierarchyKommissionierListen`. Zugleich
**Referenzimplementierung des gemeinsamen FaHierarchy-Listen-/Druck-Bausteins**
(`FaHierarchyListBuilder` + `KommissionierListenService` + `BarcodeService`), den Teil 4/5 erweitern.
Zugriff (Class-Level): `[RequireLagerProcessingAccess]` — identisch zu `WarehousePickingController` —
**plus** Feature-Toggle `[RequireFaHierarchyKommissionierlistenAktiv]`
(`FaHierarchyKommissionierlistenAktiv`, Default **aus**, nur exakt `"true"` aktiviert).

**Vorbedingungen:** Teil-1-Sync gelaufen, Tabellen gefuellt. Toggle
`FaHierarchyKommissionierlistenAktiv = true`. Benutzer mit Lager-Verarbeitungs-Zugriff
(`CanProcessLagerAsync`).

### Automatisiert/InMemory (unabhaengig vom IDEAL-Testsystem — `FaHierarchyListBuilderTests`)

- **TS-59.1 — Blatt-nur (`SubFA = 0`):** Fixture mit einem `HauptFA`, das eine `SubFA != 0`-
  Verweiszeile (mit `Kommissionieren`) **und** eigene Blattzeilen (`SubFA = 0`) fuehrt. Erwartet:
  Verweiszeile erscheint **nie**, Blattzeilen je einmal; Blattzeilen ohne gesetztes `Kommissionieren`
  fehlen (AK1/AK2). (`OnlyLeafPositions_SubFaZero_AreListed_ReferenceRowExcluded`.)
- **TS-59.2 — Anomalie-Erkennung + Banner-Daten:** Zeile mit `SubFA != 0` UND gesetztem
  `Kommissionieren` wird ausgeschlossen und im Banner-Aggregat (Anzahl + betroffene `HauptFA`)
  gemeldet; `ILogger`-Warnung je `HauptFA`/Request dedupliziert (AK3).
  (`Anomaly_SubFaNonZero_WithFlag_IsExcluded_AndReportedInBanner`.)
- **TS-59.3 — Kein Banner bei 0 Anomalien** (`NoAnomalies_BannerStaysInvisible`).
- **TS-59.4 — Fan-out-Freiheit:** zwei `FaHierarchyOrderInfo`-Zeilen je `HauptFA` (Kombigeraet) →
  Kopf zeigt beide, Positionsanzahl bleibt 1:1 (kein kartesisches Produkt), Gruppe als mehrdeutig
  markiert (AK4). (`HeaderJoin_MultipleOrderInfosPerHauptFa_DoesNotMultiplyPositions`.)
- **TS-59.5 — Gruppen-Paging:** `TotalCount` zaehlt **Gruppen**, nicht Positionszeilen; Seiten
  liefern die erwarteten `HauptFA`; eine Gruppe wird nie ueber Seiten getrennt (AK5).
  (`GroupPaging_TotalCountCountsGroups_NotPositionRows`,
  `GroupPaging_GroupIsNeverSplitAcrossPages`.)
- **TS-59.6 — Druck ohne Paging** liefert alle Gruppen (`Print_NoPagination_ReturnsAllGroups`).
- **TS-59.7 — Ziel-Filter + Anomalie-Scope:** Ziel-Wert schraenkt Blaetter UND Anomalien konsistent
  ein (`TargetPredicate_RestrictsToSelectedTarget_AndScopesAnomalies`).

### Manuell am IDEAL-Testsystem (Vorbedingung: produktivnahe Daten — leeres Testsystem reicht nicht)

- **TS-59.8 — Grunddarstellung + Ziel-Dropdown:** `/FaHierarchyKommissionierListen` aufrufen.
  Erwartet: Positionen gruppiert nach `HauptFA` (Gruppenkopf zeigt alle Montage-Abteilungen/
  Kopfdaten), Ziel-Dropdown zeigt die vorkommenden `Kommissionieren`-Werte; Auswahl eines Ziels
  liefert die erwartete Teilmenge.
- **TS-59.9 — Server-Spaltenfilter + Gruppen-Paging:** Spaltenfilter (`colf_*`) auf einer
  Positionsspalte; faellt eine Gruppe dadurch auf 0 Positionen, verschwindet ihr Kopf komplett
  (AK6). Seitengroesse 25/50/100/Alle wechseln: `TotalCount` zaehlt Gruppen (AK5).
- **TS-59.10 — Druck spiegelt Bildschirm:** „Drucken" bei aktiven Filtern; Erwartet: ein Ausdruck je
  `HauptFA`-Gruppe (Seitenumbruch), Barcode = `HauptFA`, Kopf listet alle Montage-Abteilungen; exakt
  die gefilterte Menge (inkl. `colf_*` + Ziel) (AK7). Leere gefilterte Menge → Hinweistext, kein
  leeres Blatt (AK8).
- **TS-59.11 — Kombigeraet:** bekannter `HauptFA` mit zwei Montage-Abteilungen erzeugt **einen**
  Ausdruck mit beiden im Kopf, ein Barcode; bekannte Einschraenkung im Druck-Kopf dokumentiert.
- **TS-59.12 — Mengenabgleich Struktur vs. Liste:** manuelle Gegenrechnung an einer bekannten
  mehrstufigen Struktur bestaetigt/widerlegt die `SubFA = 0`-Arbeitsannahme empirisch (Offene
  Rueckfrage 1 der Spec — Vorbedingung fuer Schranke 2).
- **TS-59.13 — Zugriff/Toggle:** ohne Lager-Verarbeitungs-Rolle → Redirect `AccessDenied`; bei
  Toggle `FaHierarchyKommissionierlistenAktiv` aus → Redirect Home + `WarningMessage` (AK9).
- **TS-59.14 — Regression:** `WarehousePicking`/AKE-Kommissionierung unveraendert; keine Schema-/
  Service-Aenderung (AK10).

### UAT-Anpassung 2026-08-13 (Etappe 8) — SubFA-Blattannahme aufgehoben + Summiert-Ansicht

> **Ueberholt durch diesen Abschnitt:** TS-59.1/59.2/59.3/59.7 beschreiben die urspruengliche
> `SubFA = 0`-Blattregel samt Anomalie-Banner. Diese Tests (`FaHierarchyListBuilderTests`) pruefen
> weiterhin die **Baustein-Faehigkeit** (`leafOnly`/`anomalyOnNonLeaf` als Opt-in) und bleiben gruen;
> das **Kommissionier-Verhalten** ist jedoch ab Etappe 8 durch die folgenden Faelle ersetzt: der
> Service ruft den Baustein jetzt mit `leafOnly: false, anomalyOnNonLeaf: false` auf (kein
> Blattfilter, kein Banner).

**Automatisiert/InMemory (`KommissionierListenServiceTests`):**
- **TS-59.30 — Alle Ebenen gelistet, kein Banner (N1a/N1b/N1e):** Fixture mit einer `SubFA != 0`-Zeile
  (mit `Kommissionieren`) und einer Blattzeile — **beide** erscheinen in der Liste; `Anomalies`
  bleibt leer, keine `ILogger`-Warnung. (`BuildAsync_ListsAllSubFaLevels_NoAnomalyBanner`.)
- **TS-59.31 — Ziel-Dropdown unabhaengig von SubFA (N1d):** ein `Kommissionieren`-Wert, der nur auf
  einer `SubFA != 0`-Zeile vorkommt, erscheint im Dropdown.
  (`GetTargetValues_IncludesValuesOnlyOnSubFaNonZero`.)
- **TS-59.32 — Summiert-Aggregation (N2a/N2b):** je `(HauptFA, Artnr, Kommissionieren)` wird die
  `Sollmenge` summiert; gleiche `Artnr` mit **unterschiedlichem** Ziel liefert **zwei** Zeilen.
  (`Summiert_SumsSollmenge_PerHauptFaArtnrZiel`, `Summiert_DifferentZiel_ProducesSeparateRows`.)
- **TS-59.33 — KW-Filter auf `KO_Termin` (N2c):** ein `HauptFA`, dessen `KO_Termin` ausserhalb der
  gewaehlten ISO-KW (Mo–So) liegt, fehlt vollstaendig; einer innerhalb ist vollstaendig enthalten.
  (`Summiert_KwFilter_ExcludesHauptFaOutsideWeek_IncludesInside`.)
- **TS-59.34 — Ohne KW keine Gesamtsumme (N2d):** ohne KW-Eingabe liefert die Aggregation ein
  **leeres** Ergebnis (die View zeigt einen Hinweistext). (`Summiert_NoKw_ReturnsEmpty_NoImplicitTotal`.)
- **TS-59.35 — Summiert unabhaengig von SubFA:** eine `SubFA != 0`-Zeile zaehlt in die Summe mit ein.
  (`Summiert_IsIndependentOfSubFa`.)
- **TS-59.36 — Server-Spaltenfilter auf der Aggregat-Projektion**
  (`Summiert_ColumnFilter_AppliesToAggregateProjection`.)
- **TS-59.37 — KW-Aufloesung (`IsoWeekRangeTests`):** `YYYY-Www` → ISO-Woche Mo–So (Sonntag inklusive,
  folgender Montag exklusiv); Einzelfeld gilt fuer beide Grenzen; vertauschte Reihenfolge wird
  getauscht; `HauptFasInRange` bindet den Termin fan-out-frei je `HauptFA`.

**Manuell am IDEAL-Testsystem:**
- **TS-59.38 — Baugruppen-Zeile sichtbar:** eine `SubFA != 0`-Position mit gesetztem `Kommissionieren`
  erscheint in Liste **und** Druck; **kein** Warnbanner, **kein** Log-Eintrag.
- **TS-59.39 — Summiert-Ansicht bedienen:** Umschaltung „Liste ↔ Summiert" (nav-pills); ohne
  Kalenderwoche erscheint der Hinweistext statt einer Gesamtsumme; nach Eingabe einer KW (von/bis via
  `<input type="week">`) erscheinen die `(HauptFA, Artnr, Ziel)`-Summenzeilen der HauptFAs, deren
  `KO_Termin` in der Woche liegt.
- **TS-59.40 — Mengenabgleich Summiert:** Gegenrechnung an einer bekannten Baugruppe/Bestandteile-
  Konstellation — Doppel-Erfassung in Sage (Baugruppe UND Bestandteile mit `Kommissionieren`) bildet
  korrekt **beide** Vorgaenge ab (Caveat aus Aenderung 1), keine faelschliche Zusammenfassung.

### Spaltenauswahl (Etappe 6 — Zahnrad/`column-preferences.js`, `viewKey` `FaHierarchyKommissionierListen`)

- **TS-59.15 — Zahnrad vorhanden und bedienbar:** `/FaHierarchyKommissionierListen` aufrufen.
  Erwartet: Zahnrad-Symbol zur Spaltenkonfiguration ist vorhanden und oeffnet das Offcanvas.
  Platzierung robust — der Knopf bleibt auffindbar/bedienbar, auch wenn ein Anomalie-Warnbanner
  ueber der Tabelle steht (nicht pixelgleich zu `ProductionOrders` gefordert).
- **TS-59.16 — Sichtbarkeit/Breite/Reihenfolge persistieren:** eine nicht-gesperrte Spalte (z. B.
  `Arbeitsbereich`) ausblenden, eine Spaltenbreite per Ziehgriff aendern, eine Spalte per Drag
  umordnen → Seite neu laden → alle drei Aenderungen bleiben erhalten (Server-Persistenz,
  `PUT /api/user-view-preferences/FaHierarchyKommissionierListen` liefert `200`, **nicht** `400`).
- **TS-59.17 — Erstzustand Default-Hidden:** ohne gespeichertes Profil sind `Artikeltyp`,
  `Beschichtet`, `Material` ausgeblendet; alle anderen Spalten sichtbar.
- **TS-59.18 — Locked-Spalten:** `HauptFA` und `Matchcode` lassen sich im Dialog **nicht**
  ausblenden.
- **TS-59.19 — Standard-Sortierung verfügbar (geändert 2026-08-18, Spec [[2026-08-18-bom-guard-hierarchisch-spec]]):**
  der Dialog **bietet** jetzt die Option „Standard-Sortierung speichern" (`supportsSortDefault: true`) —
  die frühere Sperre (`false`) fiel weg, seit `table-filter.js` alle `<tbody>`-Gruppen sortiert
  ([[2026-08-12-tabellen-sortierung-nur-erste-gruppe-bug]]). Gespeicherte Sortierung ist nach Reload wirksam.
- **TS-59.20 — Benutzer-Trennung:** zwei verschiedene Benutzer (oder Browserprofile) auf derselben
  Liste → unabhaengige Konfigurationen, keine gegenseitige Ueberschreibung.
- **TS-59.21 — Multi-Gruppen Spaltenauswahl (≥2 HauptFA-Gruppen):** Liste mit **mindestens zwei**
  `HauptFA`-Gruppen; eine Spalte ausblenden bzw. per Drag umordnen → die Aenderung wirkt in **jeder**
  Gruppe gleich, nicht nur in der ersten.
- **TS-59.22 — Multi-Gruppen Sortierung (Sortier-Fix, ≥2 HauptFA-Gruppen):** Klick auf eine
  sortierbare Spaltenkopfzeile → Positionszeilen sortieren sich **innerhalb jeder** Gruppe fuer sich;
  Gruppenkopfzeilen bleiben an ihrer Position, keine Position wandert in eine andere Gruppe.
- **TS-59.23 — Server-Spaltenfilter nach Ausblenden:** eine gefilterte Spalte (`colf_...` aktiv)
  ausblenden → Filter bleibt wirksam, die Liste bleibt gekuerzt (bewusst dokumentiertes,
  unveraendertes Verhalten). Der `Zuruecksetzen`-Link (`data-clear-table-filters`) raeumt auch
  Filter unsichtbarer Spalten weg.
- **TS-59.24 — Druck unberuehrt:** `Print` zeigt weiterhin alle Spalten unabhaengig von der
  Bildschirm-Konfiguration.

---

## TS-60 — IDEAL Teil 4: Beschichtungsauftrag (`FaHierarchyBeschichtungController`)

Filterbare, druckbare Liste der zu beschichtenden Teile (`Beschichtet == true`) auf den in Teil 1
importierten Tabellen `FaHierarchyNodes`/`FaHierarchyOrderInfos`. Read-only, Web-only (keine
Migration, kein Service). Route: `/FaHierarchyBeschichtung`. **Erweitert** den in Teil 3
geschnittenen gemeinsamen Baustein (`FaHierarchyListBuilder`) ueber die duenne
`BeschichtungsauftragService`-Schicht — **Kern-Feinheit:** anders als Teil 3 gilt hier `Beschichtet`
ueber **alle** Ebenen (Baustein-Naht `leafOnly: false`), es gibt **keinen** `SubFA = 0`-Blattfilter
und **kein** Anomalie-Banner (`anomalyOnNonLeaf: false`) — eine beschichtete Baugruppe
(`SubFA != 0`) ist legitim. Kopf (`Dienstleister`, `RAL`, `Start_Beschichtung`,
`Beschichten_Retour`, `ABNr`, `Pos`, `MontageAbteilung`) je `HauptFA` in eigener Abfrage (kein
Fan-out). Zugriff (Class-Level): neue Rolle `[RequireBeschichtungsauftragAccess]`
(`beschichtungsauftrag`, Read-Ebene) **plus** Feature-Toggle
`[RequireFaHierarchyBeschichtungAktiv]` (`FaHierarchyBeschichtungAktiv`, Default **aus**,
invertierte Semantik: nur exakt `"true"` aktiviert).

**Vorbedingungen:** Teil-1-Sync gelaufen, Tabellen gefuellt, beschichtete Positionen vorhanden.
Toggle `FaHierarchyBeschichtungAktiv = true`. Benutzer mit Rolle `beschichtungsauftrag` oder
`admin`.

### Automatisiert/InMemory (unabhaengig vom IDEAL-Testsystem — `BeschichtungsauftragServiceTests`)

- **TS-60.1 — Filter `Beschichtet == true`:** nur beschichtete Positionen erscheinen, `Beschichtet
  == false` faellt raus (AK1). (`Filter_OnlyBeschichtetTrue_IsListed`.)
- **TS-60.2 — Alle Ebenen, keine Anomalie (Kern-Feinheit):** eine beschichtete Baugruppe
  (`SubFA != 0`) wird **mitgelistet** und ist **keine** Anomalie; kein Banner (Abgrenzung zu Teil 3).
  (`AllLevels_SubFaNonZero_WithBeschichtet_IsListed_AndNoAnomaly`.)
- **TS-60.3 — Fan-out-Freiheit des Dienstleister-Kopfs:** zwei `FaHierarchyOrderInfo`-Zeilen je
  `HauptFA` (Kombigeraet) → Kopf zeigt beide, Positionsanzahl bleibt 1:1 (kein kartesisches
  Produkt), Gruppe als mehrdeutig markiert (AK2).
  (`HeaderJoin_MultipleOrderInfosPerHauptFa_DoesNotMultiplyPositions`.)
- **TS-60.4 — Gruppen-Paging:** `TotalCount` zaehlt **Gruppen**, nicht Positionszeilen; Seiten
  liefern die erwarteten `HauptFA` (AK4). (`GroupPaging_TotalCountCountsGroups_NotPositionRows`.)
- **TS-60.5 — Leerfall Gruppe:** ein `HauptFA`, dessen Positionen alle wegfiltern, verschwindet ganz
  (AK6). (`EmptyGroup_AfterFilter_Disappears`.)
- **TS-60.6 — Mehrdeutigkeits-`ILogger`-Warnung:** Kombigeraet (mehrere Kopfzeilen) erzeugt genau
  eine `ILogger`-Warnung mit `HauptFA` + Anzahl Kopfzeilen — kein SyncLog (AK3).
  (`BuildAsync_LogsWarning_ForAmbiguousCombigeraet`; Gegenprobe:
  `BuildAsync_NoWarning_WhenHeadUnambiguous`.)
- **TS-60.7 — Regression Baustein-Naht:** ohne die neuen Opt-in-Parameter bleibt das
  Teil-3-Verhalten (Blattfilter + Anomalie) unveraendert.
  (`Teil3Default_StaysLeafOnly_AndAnomalyOn_WhenNewParamsOmitted`; zusaetzlich alle
  `FaHierarchyListBuilderTests` aus Teil 3 weiterhin gruen.)

### Manuell am IDEAL-Testsystem (Vorbedingung: produktivnahe Daten mit beschichteten Positionen — leeres Testsystem reicht nicht)

- **TS-60.8 — Grunddarstellung:** `/FaHierarchyBeschichtung` aufrufen. Erwartet: beschichtete
  Positionen gruppiert nach `HauptFA`, Gruppenkopf zeigt Dienstleister/RAL/Termine; Positionstabelle
  mit `HauptFA`, `HauptArtnr`, `Artnr`, `Matchcode`, `Sollmenge`, `Beschichtet`, `Breite`, `Hoehe`,
  `Tiefe` (AK1).
- **TS-60.9 — Server-Spaltenfilter + Gruppen-Paging:** Spaltenfilter (`colf_*`) auf einer
  Positionsspalte; faellt eine Gruppe dadurch auf 0 Positionen, verschwindet ihr Kopf komplett (AK6).
  Seitengroesse 25/50/100/Alle wechseln: `TotalCount` zaehlt Gruppen (AK4).
- **TS-60.10 — Druck spiegelt Bildschirm:** „Drucken" bei aktiven Filtern; Erwartet: ein Ausdruck je
  `HauptFA`-Gruppe (Seitenumbruch), Barcode = `HauptFA`, Kopf mit Dienstleister/RAL/Termine; exakt
  die gefilterte Menge (inkl. `colf_*`). Leere gefilterte Menge → Hinweistext statt leerem Blatt
  (AK6). Reine Razor-View im Browser (kein PrintService), CSS eingebettet.
- **TS-60.11 — Kombigeraet (mehrdeutig):** bekannter `HauptFA` mit mehreren Kopfzeilen → Dokument
  sichtbar als mehrdeutig gekennzeichnet, alle Kopfvarianten im Klartext; `ILogger`-Warnung im
  Server-Log (nicht SyncLog) (AK3).
- **TS-60.12 — Zugriff/Toggle:** Benutzer **ohne** Rolle `beschichtungsauftrag`/`admin` → Redirect
  `AccessDenied`; Toggle `FaHierarchyBeschichtungAktiv` aus → Redirect Home + `WarningMessage`;
  Toggle-Key fehlt komplett → ebenfalls inaktiv (invertierte Default-Semantik) (AK5).
- **TS-60.13 — Regression:** `WarehousePicking`/AKE-Beschichtungslogik (`CoatingDetectionService`,
  `CoatingDateCalculator`) und Teil 3 (Kommissionierlisten) unveraendert; keine Schema-/
  Service-Aenderung (AK7).

### Spaltenauswahl (Etappe 6 — Zahnrad/`column-preferences.js`, `viewKey` `FaHierarchyBeschichtung`)

- **TS-60.14 — Zahnrad vorhanden und bedienbar:** `/FaHierarchyBeschichtung` aufrufen. Erwartet:
  Zahnrad-Symbol vorhanden und oeffnet das Offcanvas; Platzierung robust (Beschichtung hat die
  Filterkarte davor, kein Anomalie-Banner).
- **TS-60.15 — Sichtbarkeit/Breite/Reihenfolge persistieren:** eine nicht-gesperrte Spalte (z. B.
  `Breite`) ausblenden, Breite aendern, umordnen → Reload → bleibt erhalten
  (`PUT /api/user-view-preferences/FaHierarchyBeschichtung` liefert `200`, **nicht** `400`).
- **TS-60.16 — Locked-Spalten:** `HauptFA` und `Matchcode` lassen sich **nicht** ausblenden.
- **TS-60.17 — Standard-Sortierung verfügbar (geändert 2026-08-18, Spec [[2026-08-18-bom-guard-hierarchisch-spec]]):**
  der Dialog **bietet** jetzt die Option „Standard-Sortierung speichern" (`supportsSortDefault: true`);
  die frühere `false`-Sperre ist weggefallen. Gespeicherte Sortierung ist nach Reload wirksam.
- **TS-60.18 — Benutzer-Trennung:** zwei Benutzer/Profile → unabhaengige Konfigurationen.
- **TS-60.19 — Multi-Gruppen Spaltenauswahl (≥2 HauptFA-Gruppen):** eine Spalte ausblenden/umordnen →
  Aenderung wirkt in **jeder** Gruppe gleich, nicht nur in der ersten.
- **TS-60.20 — Multi-Gruppen Sortierung (Sortier-Fix, ≥2 HauptFA-Gruppen):** Klick auf eine
  Spaltenkopfzeile → Positionszeilen sortieren sich **innerhalb jeder** Gruppe fuer sich;
  Gruppenkopfzeilen bleiben, keine Position wandert in eine andere Gruppe.
- **TS-60.21 — Server-Spaltenfilter nach Ausblenden:** gefilterte Spalte ausblenden → Filter bleibt
  wirksam (dokumentiertes, unveraendertes Verhalten).
- **TS-60.22 — Druck unberuehrt:** `Print` zeigt weiterhin alle Spalten unabhaengig von der
  Bildschirm-Konfiguration.

---

## TS-61 — IDEAL Teil 5: Vormontage-Listen (`FaHierarchyVormontageController`)

Zwei read-only Sichten auf die Vormontage-Bedarfe (`VMBedarf IS NOT NULL AND VMBedarf <> ''`,
nvarchar-Arbeitsbereich, **kein** Sage-Bit/`-1`-Vergleich) auf den in Teil 1 importierten Tabellen
`FaHierarchyNodes`/`FaHierarchyOrderInfos`. Web-only (keine Migration, kein Service). Routen:
`/FaHierarchyVormontage/Index` (**Sicht 1 — Einzelne Teile**), `/FaHierarchyVormontage/Summiert`
(**Sicht 2 — Summiert**), mit Umschaltung und je Arbeitsbereich einem Reiter (`VMBedarf`-Wert, kein
leerer Reiter). Beide Sichten basieren auf **allen** Positionen mit gesetztem `VMBedarf` —
**unabhaengig von `SubFA`** (UAT 2026-08-13, Aenderung 1: kein Blattfilter mehr).

**Sicht 1** nutzt den gemeinsamen Baustein (`FaHierarchyListBuilder`) mit
`leafOnly: false` + `anomalyOnNonLeaf: false` (wie Teil 4): kein Blattfilter, **kein** Anomalie-Banner,
Fan-out-freier Kopf-Join, Gruppierung + Gruppen-Paging nach `HauptFA`. Optionaler KW-Filter auf
`FE_Termin` (ISO Mo–So). **Sicht 2** ist eine
**eigene Aggregat-Logik** (nicht ueber den Baustein): Aggregation je `VMBedarf`-Reiter nach
**`Matchcode`** (nicht Artnr) mit **zwei getrennten Summenspalten** (`Sollmenge` UND
`Fertigungmenge`); Seiteneinheit = **Aggregatzeile** (begruendete ADR-0005-Ausnahme),
Server-Spaltenfilter wirken auf der Aggregat-Projektion. Zugriff (Class-Level):
`[RequireVorbauAccess]` (`vorbau`, identisch zur FA-Abarbeitungsliste `FaWorklistController`, Read)
**plus** Feature-Toggle `[RequireFaHierarchyVormontageAktiv]` (`FaHierarchyVormontageAktiv`, Default
**aus**, invertierte Semantik: nur exakt `"true"` aktiviert). Keine neue Rolle.

**Bewusst NICHT umgesetzt:** der Wochenbezug-Filter (`Neuer_PT_PPS`) — die Filter-Semantik ist noch
offene Rueckfrage (Spec Teil 5, Fachliche Anforderung 6 / Offene Rueckfrage 1). `Neuer_PT_PPS`
erscheint in Sicht 1 hoechstens als reine Kopf-Anzeige, nie als Mengen-Einschraenkung. **Ebenfalls
out-of-scope:** Export in die Isolierfraesen-Software (Backlog).

**Vorbedingungen:** Teil-1-Sync gelaufen, Tabellen gefuellt, Positionen mit gesetztem `VMBedarf`
vorhanden. Toggle `FaHierarchyVormontageAktiv = true`. Benutzer mit Vorbau-Zugriff (`vorbau`/`admin`).

### Automatisiert/InMemory (unabhaengig vom IDEAL-Testsystem — `VormontageServiceTests`)

- **TS-61.1 — Filter `VMBedarf` gesetzt (nvarchar, kein Bit):** gesetzt = `IS NOT NULL AND <> ''`;
  leer/Whitespace/null fallen raus; Reiter-Einschraenkung auf einen konkreten Bereich wirkt (AK1).
  (`Sicht1_FlagPredicate_FiltersVmBedarfSet_NvarcharNotBit`.)
- **TS-61.2 — Sicht 1 alle Ebenen, kein Banner (UAT 2026-08-13, Aenderung 1 / AK9):** eine
  `SubFA != 0`-Zeile mit gesetztem `VMBedarf` wird **jetzt gelistet** (frueher Anomalie/ausgeschlossen);
  `Anomalies` bleibt leer, keine `ILogger`-Warnung; Blatt ohne `VMBedarf` faellt weiterhin raus.
  (`Sicht1_ListsAllSubFaLevels_NoAnomalyBanner`.)
- **TS-61.3 — Sicht 1 Fan-out-Freiheit:** zwei `FaHierarchyOrderInfo`-Zeilen je `HauptFA`
  (Kombigeraet) → Kopf zeigt beide, Positionsanzahl bleibt 1:1, Gruppe als mehrdeutig markiert
  (AK2b). (`Sicht1_HeaderJoin_MultipleOrderInfos_DoesNotMultiplyPositions`.)
- **TS-61.4 — Sicht 1 Reiter-Einschraenkung:** nur Positionen des gewaehlten `VMBedarf`-Reiters
  erscheinen (AK1). (`Sicht1_ReiterRestriction_ScopesToSelectedBereich`.)
- **TS-61.5 — Sicht 1 Gruppen-Paging:** `TotalCount` zaehlt **Gruppen** (AK5).
  (`Sicht1_GroupPaging_TotalCountCountsGroups`.)
- **TS-61.6 — Reiter-Enumeration:** Distinct `VMBedarf`-Werte, sortiert, ohne null/leer, **unabhaengig
  von `SubFA`** (UAT 2026-08-13: auch `VMBedarf` nur auf `SubFA != 0` erzeugt einen Reiter) — kein
  leerer Reiter (AK1/AK9). (`GetBereiche_DistinctNonEmpty_Sorted_IndependentOfSubFa`.)
- **TS-61.7 — Sicht 2 Aggregation nach Matchcode, zwei Summen:** zwei Positionen mit gleichem
  `Matchcode` → eine Zeile, `Sollmenge` und `Fertigungmenge` je getrennt summiert (AK4).
  (`Sicht2_AggregatesByMatchcode_SumsBothQuantities`.)
- **TS-61.8 — Sicht 2 gleicher Artnr, verschiedene Matchcodes → mehrere Zeilen (bewusst):** kein
  Fehler (AK4). (`Sicht2_SameArtnrDifferentMatchcode_ProducesMultipleRows`.)
- **TS-61.9 — Sicht 2 Aggregation je Reiter:** Positionen anderer `VMBedarf`-Bereiche fliessen
  **nicht** in die Summe (AK4). (`Sicht2_AggregatesPerReiter_ExcludesOtherBereich`.)
- **TS-61.10 — Sicht 2 alle Ebenen (UAT 2026-08-13, Aenderung 1 / AK9):** `SubFA != 0`-Zeilen zaehlen
  **jetzt mit** in die Aggregation (frueher ausgeschlossen). (`Sicht2_IncludesAllSubFaLevels_InAggregationBase`.)
- **TS-61.10b — Sicht 2 KW-Filter auf `FE_Termin` VOR der Aggregation (UAT 2026-08-13, Aenderung 2 /
  AK10):** zwei `HauptFA` mit `FE_Termin` in unterschiedlichen ISO-KW, gleicher `Matchcode` — Filter
  auf die KW des ersten `HauptFA` → nur dessen Menge fliesst in die Summe (fan-out-frei ueber die
  HauptFA-Menge). (`Sicht2_KwFilter_OnFeTermin_AppliesBeforeAggregation`.)
- **TS-61.11 — Sicht 2 Aggregat-Paging:** `TotalCount` zaehlt **Aggregatzeilen**, nicht Positionen;
  Seiten liefern die erwarteten Matchcodes (AK5). (`Sicht2_AggregatePaging_TotalCountCountsAggregateRows`.)
- **TS-61.12 — Sicht 2 Spaltenfilter auf Aggregat-Projektion:** `colf_matchcode` filtert die
  Aggregatzeilen, `TotalCount` zaehlt NACH Filter (AK5).
  (`Sicht2_ColumnFilter_AppliesToAggregateProjection`.)

### Manuell am IDEAL-Testsystem (Vorbedingung: produktivnahe Daten mit gesetztem `VMBedarf` — leeres Testsystem reicht nicht)

- **TS-61.13 — Grunddarstellung Sicht 1:** `/FaHierarchyVormontage` aufrufen. Erwartet:
  Blattpositionen mit `VMBedarf` gruppiert nach `HauptFA`, Reiter je Arbeitsbereich, Positionstabelle
  mit `HauptFA`, `HauptArtnr`, `Artnr`, `Matchcode`, `Sollmenge`, `Fertigungmenge`, `VMBedarf`,
  `Material` (AK1).
- **TS-61.14 — Reiter-Wechsel:** zwischen `VMBedarf`-Reitern wechseln — beide Sichten liefern jeweils
  die erwartete Teilmenge des gewaehlten Arbeitsbereichs.
- **TS-61.15 — Grunddarstellung Sicht 2 + Umschaltung:** auf „Summiert" wechseln (Reiter bleibt) —
  je `Matchcode` eine Zeile mit `Summe Sollmenge` und `Summe Fertigungmenge` (AK4).
- **TS-61.16 — Mengenabgleich Struktur vs. Sicht 2:** an einer bekannten mehrstufigen Struktur die
  Summen aus Sicht 2 gegen die Blattpositionen aus Sicht 1 pruefen (keine Doppelzaehlung ueber Ebenen).
- **TS-61.17 — Baugruppen-Zeile sichtbar (UAT 2026-08-13):** ein `SubFA != 0`-Datensatz mit gesetztem
  `VMBedarf` erscheint in beiden Sichten; **kein** Banner, **kein** Log-Eintrag (AK9).
- **TS-61.17b — KW-Filter auf `FE_Termin` bedienen (AK10):** KW-Zeitraum (von/bis via
  `<input type="week">`) eingeben — in Sicht 2 reduzieren sich die Summen auf die HauptFAs, deren
  `FE_Termin` in der ISO-KW (Mo–So) liegt; in Sicht 1 wirkt der Filter optional analog.
- **TS-61.18 — Server-Spaltenfilter + Paging beide Sichten:** Sicht 1 zaehlt Gruppen, Sicht 2 zaehlt
  Aggregatzeilen; Seitengroesse 25/50/100/Alle wechseln (AK5).
- **TS-61.19 — Zugriff/Toggle:** Benutzer **ohne** `vorbau`/`admin` → Redirect `AccessDenied`; Toggle
  `FaHierarchyVormontageAktiv` aus (oder Key fehlt) → Redirect Home + `WarningMessage` (AK6).
- **TS-61.20 — Regression:** `FaWorklist`/AKE unveraendert, Teil 3/4 unveraendert; keine Schema-/
  Service-Aenderung (AK7).

### Spaltenauswahl (Etappe 6 — Zahnrad/`column-preferences.js`)

Sicht 1 „Einzelne Teile" nutzt `viewKey` `FaHierarchyVormontageEinzeln` (gruppiert, je HauptFA ein
`<tbody>`), Sicht 2 „Summiert" nutzt `viewKey` `FaHierarchyVormontageSummiert` (flache Tabelle, ein
`<tbody>`).

- **TS-61.21 — Zahnrad beide Sichten:** `/FaHierarchyVormontage` (Sicht 1) und `/…/Summiert`
  (Sicht 2) aufrufen. Erwartet: Zahnrad je Sicht vorhanden und bedienbar; Platzierung robust (Sicht 1
  hat davor die Reiter `<ul class="nav">` bzw. ein Anomalie-Banner).
- **TS-61.22 — Persistenz Sicht 1:** in Sicht 1 eine nicht-gesperrte Spalte (z. B. `Material`)
  ausblenden, Breite aendern, umordnen → Reload → bleibt erhalten
  (`PUT /api/user-view-preferences/FaHierarchyVormontageEinzeln` liefert `200`, **nicht** `400`).
- **TS-61.23 — Persistenz Sicht 2:** in Sicht 2 eine nicht-gesperrte Spalte ausblenden/umordnen →
  Reload → bleibt erhalten (`…/FaHierarchyVormontageSummiert` liefert `200`, **nicht** `400`).
- **TS-61.24 — Locked-Spalten:** Sicht 1 → `HauptFA` und `Matchcode` nicht ausblendbar; Sicht 2 →
  `Matchcode` (einziger Identifikator) nicht ausblendbar.
- **TS-61.25 — Standard-Sortierung in BEIDEN Sichten (geändert 2026-08-18, Spec [[2026-08-18-bom-guard-hierarchisch-spec]]):**
  Sicht 1 (Einzeln) **bietet** die Option „Standard-Sortierung speichern" jetzt ebenfalls
  (`supportsSortDefault: true`; früher `false`); Sicht 2 (Summiert) bot sie bereits. Gespeicherte
  Standard-Sortierung ist in beiden nach Reload wirksam.
- **TS-61.26 — Benutzer-Trennung:** zwei Benutzer/Profile → unabhaengige Konfigurationen je Sicht.
- **TS-61.27 — Multi-Gruppen Spaltenauswahl Sicht 1 (≥2 HauptFA-Gruppen):** Spalte ausblenden/umordnen
  → Aenderung wirkt in **jeder** Gruppe gleich, nicht nur in der ersten.
- **TS-61.28 — Multi-Gruppen Sortierung Sicht 1 (Sortier-Fix, ≥2 HauptFA-Gruppen):** Klick auf eine
  Spaltenkopfzeile → Positionszeilen sortieren sich **innerhalb jeder** Gruppe fuer sich;
  Gruppenkopfzeilen bleiben, keine Position wandert in eine andere Gruppe. In Sicht 2 (ein `<tbody>`)
  sortiert der Klick die flache Liste wie gewohnt.
- **TS-61.29 — Server-Spaltenfilter nach Ausblenden:** gefilterte Spalte ausblenden → Filter bleibt
  wirksam (dokumentiertes, unveraendertes Verhalten).

### Sortier-Regression flache Listen (Pflicht vor Merge — gemeinsam genutztes `table-filter.js`)

- **TS-61.30 — Regressions-Sichtpruefung Ein-`tbody`-Listen:** Der Sortier-Fix in `table-filter.js`
  (sortiert kuenftig **je `<tbody>` separat**) darf bestehende, nicht-gruppierte Listen nicht
  veraendern. Auf `ProductionOrders/Index` (ein `<tbody>`) nach dem Fix sortieren (Klick auf mehrere
  Spaltenkopfzeilen, auf- und absteigend) und filtern → **exakt** dasselbe Verhalten wie zuvor, keine
  sichtbare Abweichung. Stichprobenartig ebenso an `PickingLeitstand`/`FaWorklist`.

---

## TS-62 — IDEAL Teil 2: FA-Struktur Darstellung (Nachtrag) — Tree-Table / Kontrast / Icons / Baum-Spaltenfilter / Spaltenauswahl

Reiner Praesentations-Nachtrag zu TS-58 (Etappe 7). Kein Server-/Import-/Modell-Eingriff:
`FaHierarchyController`, `FaHierarchyTreeBuilder`, die server-seitige Struktur-Filterkarte, der
Tiefen-Cap/Zyklenschutz, die Struktur-Pagination und der Zugriffsschutz
`[RequirePickingOrTrackingOrLeitstandAccess]` bleiben **unveraendert**. Betroffen sind nur
`Views/FaHierarchy/Index.cshtml` + `_FaHierarchyNode.cshtml`, `wwwroot/js/fa-hierarchy-tree.js`,
`wwwroot/css/site.css` und additiv `ColumnDefinitions.cs` (neuer `viewKey` `FaHierarchyStructure`).

**Vorbedingungen:** wie TS-58 (Teil-1-Sync gelaufen, mind. eine Struktur mit Sub-Sub-FA-Ebene;
Benutzer mit `picking`/`tracking`/`leitstand`/`admin`-Rolle). Fuer die Ausrichtungs- und
Filter-Szenarien produktivnahe Daten: mind. eine Struktur mit **≥4 Ebenen und 50+ Positionen**.

### Automatisiert/InMemory (unabhaengig vom IDEAL-Testsystem)

- **TS-62.1 — Icon-Klassifikation (`FaNodeClassifierTests`):** Praezedenzkette
  Wurzel → Baugruppe → Zukaufteil → Material. Wurzel (`VaterFA IS NULL`) gewinnt auch bei
  `SubFA != 0`; Baugruppe gewinnt gegen Zukauf bei gesetztem `SubFA` **und** `Beschaffungsartikel`;
  Blatt+`Beschaffungsartikel` → Zukauf; Blatt ohne → Material. **Soll:** alle gruen.
- **TS-62.2 — Prefs-API `FaHierarchyStructure` (`UserViewPreferencesApiControllerTests`):**
  `Get`/`Put` mit `viewKey` `FaHierarchyStructure` liefern **204/OK**, **nicht** `400 BadRequest`
  (Regression analog zum `FaWorklist`-Fall). **Soll:** gruen.

### Kontrast (rechnerisch, Defekt-Fix)

- **TS-62.3 — Kontrast Kopfband + Badges + Matchcode (rechnerisch, WCAG AA):** Vorder-/Hintergrund
  je Element aus den CSS-Werten ermitteln und Verhaeltnis gegen 4,5:1 (Fliesstext) / 3:1 (grosse
  Schrift/Bedienelemente) rechnen. Referenzwerte (bestanden):
  - Struktur-Kopfzeile (`.fa-structure-header`): `--ake-text` #4A494A auf `--ake-light-gray` #F5F5F5 = **8,2:1** ✓
  - `bg-primary` (Kunde), weiss auf #0d6efd = **4,5:1** ✓; `bg-secondary` (Status/„beschichtet"), weiss auf #6c757d = **4,7:1** ✓
  - `bg-light text-dark border` (KO/FE/Montage/Knotenzahl), #212529 auf #f8f9fa = **14,6:1** ✓
  - `bg-info text-dark` (Komm.-Ziel/mehrdeutig), #212529 auf #0dcaf0 = **7,9:1** ✓
  - `bg-warning text-dark` (Verwaist), #212529 auf #ffc107 = **9,5:1** ✓; `bg-danger` (Fehler), weiss auf #dc3545 = **4,5:1** ✓
  - Matchcode `text-muted` #6c757d(75%) auf weiss = **6,8:1** ✓
  - Typ-Icons auf weiss: `text-success` #198754 = 4,5:1, `text-primary` #0d6efd = 4,5:1, `text-dark` #212529 = 14,6:1, `text-secondary` #6c757d = 4,7:1 — alle ≥ 3:1 ✓
  **Ergaenzend (nicht Messverfahren):** Sichtpruefung am realen Fertigungsterminal als Bestaetigung.

### Tree-Table-Darstellung

- **TS-62.4 — Grunddarstellung/Ausrichtung:** `/FaHierarchy` mit ≥2 Strukturen unterschiedlicher
  Tiefe auf einer Seite laden, dazu eine Struktur mit ≥4 Ebenen/50+ Positionen. Erwartet: **eine**
  `<table data-view-key="FaHierarchyStructure">`, je Struktur **ein** `<tbody>` mit einer
  Kopfzeile (`colspan`); **nur** die Struktur-Spalte rueckt ein (Chevron+Icon+Artnr), alle anderen
  Spalten (Matchcode, Bezeichnung, Arbeitsbereich, Komm.-Ziel, Soll/Fert., SubFA, Status) fluchten
  ueber alle Ebenen **und** Strukturen hinweg.
- **TS-62.5 — Schmaler Bildschirm (< lg/992px):** Fensterbreite < 992px. Erwartet: hintere Spalten
  (SubFA, Soll/Fert.) werden ausgeblendet, verbleibender Ueberlauf ist horizontal scrollbar
  (`.table-responsive`); die Struktur-Spalte bleibt in voller Breite lesbar, wird **nie**
  komprimiert. (Reale Terminal-Aufloesung bestaetigen — liegt sie < 992px, ist dies der Normalfall.)
- **TS-62.6 — Auf-/Zuklappen Knoten- und Struktur-Ebene (OSEON-Paritaet):** Chevron eines Knotens
  klappt genau dessen Unterbaum auf/zu (Chevron rotiert). Klick auf die **Struktur-Kopfzeile**
  klappt den ganzen `<tbody>`-Block auf/zu (Kopfdaten + Knotenzeilen), andere Strukturen unberuehrt.
  „Alle auf"/„Alle zu" wirken ueber alle Strukturen. Tastatur: Fokus auf einen Chevron, Enter/Space
  toggelt; Fokus ist sichtbar. Verhalten funktional wie `Views/Tracking/OseonIndex.cshtml`.

### Icons + Legende

- **TS-62.7 — Typ-Icons inkl. Praezedenz + Legende:** je ein Beispielknoten pro Typ zeigt ein in
  **Form und Farbe** unterscheidbares Icon (Wurzel=Haus/gruen, Baugruppe=Box/blau, Zukauf=Wagen/dunkel,
  Material=Punkt/grau) mit `title`/`aria-label`. Ein Knoten, der Wurzel **und** Baugruppe waere
  (`VaterFA IS NULL` und `SubFA != 0`), zeigt das **Wurzel**-Icon. Die Legende oberhalb der Liste
  ist vorhanden und ausklappbar (`<details>`). Zustandsmarker „beschichtet" bleibt ein Badge
  (Status-Spalte), nicht ins Typ-Icon gemischt.

### Baum-Spaltenfilter (blendet aus) + Abgrenzung zum Hervorheben

- **TS-62.8 — Baum-Semantik Freitextfilter:** In der Spaltenfilter-Zeile (Kopfzeile) auf einer Spalte
  (z. B. Bezeichnung) einen Wert tief in einer Struktur eingeben. Erwartet: Treffer **hervorgehoben**
  (amber), sein Pfad zur Wurzel **gedimmt** sichtbar (`.fa-node-context`), nicht treffende
  Geschwisterzweige **samt Unterbaum** ausgeblendet; Zaehler „X von Y Knoten" korrekt; faellt eine
  Struktur auf **null Treffer**, verschwindet ihr ganzer `<tbody>`-Block. Kein Knoten verschwindet
  ohne sichtbaren Elternpfad.
- **TS-62.9 — Auswahlfilter (`<select>`):** Spalten Arbeitsbereich, Komm.-Ziel, Status bieten ein
  Dropdown mit genau den **auf der Seite vorkommenden** Werten. Auswahl filtert mit derselben
  Baum-Semantik wie der Freitextfilter.
- **TS-62.10 — Zwei Mechanismen unterscheidbar:** Testperson kann allein anhand der Beschriftung
  benennen, dass „Knoten hervorheben (Struktur bleibt vollstaendig)" nur markiert (blendet nichts
  aus) und der Spaltenfilter in der Kopfzeile ausblendet. Beide gleichzeitig aktiv: Hervorheben
  markiert innerhalb der (durch den Spaltenfilter reduzierten) sichtbaren Zeilen.
- **TS-62.11 — Keine Sortierbarkeit:** Kein Spaltenkopf der Baum-Tabelle traegt `data-sortable`/
  reagiert auf Klick als Sortier-Trigger; der Zahnrad-Dialog bietet **keine** Option
  „Standard-Sortierung speichern" (`SupportsSortDefault: false`).

### Spaltenauswahl (`column-preferences.js`, `viewKey` `FaHierarchyStructure`)

- **TS-62.12 — Zahnrad vorhanden/bedienbar:** Auf `/FaHierarchy` erscheint das Zahnrad (Offcanvas),
  analog `ProductionOrders`/`OseonIndex`.
- **TS-62.13 — Persistenz je Benutzer:** eine nicht-gesperrte Spalte (z. B. SubFA) ausblenden,
  Breite aendern, umordnen → Reload → bleibt erhalten
  (`PUT /api/user-view-preferences/FaHierarchyStructure` liefert **200**, nicht 400;
  `GET` ohne Praeferenz **204**, mit **200**). Zweiter Benutzer sieht seine eigene, unabhaengige
  Konfiguration.
- **TS-62.14 — Locked-Spalten:** Struktur- und Matchcode-Spalte lassen sich **nicht** ausblenden.
- **TS-62.15 — Multi-`tbody`-Tauglichkeit (≥2 Strukturen):** eine Spalte ausblenden bzw. umordnen →
  Aenderung wirkt in **jeder** Struktur (`<tbody>`), nicht nur in der ersten.

### Regression

- **TS-62.16 — Regression Teil 2 unveraendert:** server-seitige Struktur-Filterkarte,
  Struktur-Pagination, Tiefen-Cap-/Zyklen-/Waisen-/Kombigeraet-Verhalten und Zugriffsschutz aus
  TS-58 weiterhin gruen; `FaHierarchyController`/`FaHierarchyTreeBuilder`/Import/Modelle
  byte-identisch. Kein neues NuGet-/npm-/CDN-Paket (Icons als Inline-SVG).

---

## TS-63 — IDEAL Teil 7 Etappe B: FA-Hierarchie-Einwegtor (Master + Guard + Umschalt-Seite)

**Vorbedingungen:** Angemeldet als Administrator. Master `ProduktionsauftragHierarchisch` ist nach
dem Deploy default **aus** (`false`), es liegen **keine** hierarchischen Daten vor
(`ProductionOrders` alle `OrderNumber == SubOrderNumber`). Die Umschalt-Seite ist unter
`/HierarchieUmstellung` erreichbar; die generische Maske unter `/ServiceSettings`.

> Hinweis: Der „gesperrt"-Zustand entsteht erst durch den Materialisierungs-Sync (Etappe C). Für
> die Sperr-Szenarien hier ohne Etappe C: eine Testzeile mit `OrderNumber <> SubOrderNumber` direkt
> in `ProductionOrders` einspielen (DB-Kopie) und die Web-App neu starten (Anzeige-Refresh am Start).

### Umschalten (offener Zustand)

- **TS-63.1 — Master einschalten mit Bestätigung (ohne hierarchische Daten):** `/HierarchieUmstellung`.
  Erwartet: Status-Badge „Flach (AKE-Standard)". Klick „Auf hierarchische Produktionsaufträge
  umstellen" öffnet ein Modal mit dem exakten Text („Nach dem ersten hierarchischen Import ist eine
  Rückkehr zur flachen Struktur nicht mehr möglich. Vorher auf einer Datenbank-Kopie testen. Wirklich
  umstellen?"). „Jetzt umstellen" → Erfolgsmeldung, Status-Badge „Hierarchisch aktiviert".
- **TS-63.2 — Danach noch rückgängig (solange keine Daten):** direkt nach TS-63.1 (keine
  hierarchischen Daten). Erwartet: Button „Wieder deaktivieren" ist sichtbar; Klick setzt zurück auf
  „Flach", Erfolgsmeldung.
- **TS-63.3 — Abbrechen im Modal:** Modal öffnen, „Abbrechen" → keine Änderung, Status unverändert.

### Gesperrter Zustand (hierarchische Daten vorhanden)

- **TS-63.4 — Master read-only + Sperr-Begründung:** mit eingespielter Zeile `OrderNumber <>
  SubOrderNumber` und Neustart. Erwartet auf `/HierarchieUmstellung`: Badge „gesperrt — hierarchische
  Daten vorhanden", kein Umschalt-Button, sondern der Hinweis „Nicht mehr über die Anwendung
  umkehrbar" mit Verweis auf `docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md`.
- **TS-63.5 — Technischer Schreibversuch über generische Maske abgelehnt + protokolliert (AK 2):**
  Auf `/ServiceSettings` bietet die Zeile `ProduktionsauftragHierarchisch` **kein**
  Schreib-Bedienelement (nur Badge + Link „Umschalten (eigene Seite)"). Ein per Tool
  konstruierter POST an `SaveSettings` mit `settings[ProduktionsauftragHierarchisch]=false` wird
  **nicht** gespeichert (Master bleibt aktiviert), es erscheint eine Fehlermeldung, und im
  Aktivitäts-Protokoll steht ein `HierarchieUmstellung`-Eintrag „Technischer Schreibversuch …
  abgelehnt".
- **TS-63.6 — Kein Schreib-Bedienelement in generischer Maske (AK 2):** Sichtprüfung — die
  Master-Zeile in `/ServiceSettings` zeigt ausschließlich Anzeige + Link, keinen Toggle/kein
  Eingabefeld (ersetzt den früheren „Deaktivierung über Teil-6-Maske"-Test; Teil-6-Maske existiert
  nicht).

### Audit + Auto-Erledigt-Abschaltung

- **TS-63.7 — Audit-Eintrag beim Flip:** nach TS-63.1 im Aktivitäts-Protokoll (Service
  `HierarchieUmstellung`) ein Eintrag „FA-Hierarchie-Master auf 'aktiviert' gesetzt durch <Windows-
  Login>".
- **TS-63.8 — Auto-Erledigt-Abschaltung beim Einschalten (vorbereitet für Etappe D):** ist der
  Auto-Erledigt-Schalter (Key `Sync:FaZusatzinfoAutoErledigtEnabled`, ab Etappe D) auf `true`, wird
  er beim Master-Einschalten automatisch auf `false` gesetzt und der Audit-Eintrag vermerkt „Auto-
  Erledigt automatisch abgeschaltet". (In Etappe B ohne den Key ein No-op — kein Fehler.)

### Runbook / Regression

- **TS-63.9 — Runbook erreichbar + verlinkt (AK 15):** `README.md` → Abschnitt „Runbooks" verweist
  auf `docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md`; die Datei existiert und beschreibt Vorbedingungen
  (Backup), betroffene Tabellen, Bereinigungsschritt und die Datenverlust-Warnung. Die Sperr-
  Fehlermeldung des Guards nennt denselben Pfad.
- **TS-63.10 — AKE-Verhalten unverändert (Master aus):** mit Master `false` verhält sich
  `/ServiceSettings` (übrige Keys speicherbar) und jeder bestehende Controller/Sync exakt wie vorher;
  der Guard greift nur bei Schreibversuchen auf die geschützten Keys mit vorhandenen hierarchischen
  Daten.

---

## TS-64 — IDEAL Teil 7 Etappe C: Materialisierungs-Sync (Struktur → ProductionOrders)

**Vorbedingungen:** Master `ProduktionsauftragHierarchisch = true`, `Sync:HierarchicalFaEnabled = true`
(Struktur-Import gefüllt), Windows-Service läuft. Am IDEAL-Standort ist `Sync:ProductionOrdersEnabled
= false` (kein AKE-Import). Der Materialisierungs-Sync läuft im `SyncWorker` **nach** dem
FA-Hierarchie-Sync. Automatisiert abgedeckt: der reine `FaMaterializationPlanner`
(`FaMaterializationPlannerTests`, 7 Fälle). Die folgenden Szenarien prüfen den DB-/Mail-Effekt am
laufenden System.

- **TS-64.1 — Regel 1 (neuer Sub-FA angelegt):** eine Struktur mit ≥1 Sub-FA (`SubFA != 0`) neu in
  den FA-Views → nach dem Lauf existiert je Sub-FA eine `ProductionOrders`-Zeile (`SubOrderNumber =
  SubFA`, `OrderNumber = HauptFA`, `ParentSubOrderNumber = VaterFA`). Blätter (`SubFA = 0`) werden
  **nicht** materialisiert. SyncLog `FaMaterialization` zeigt `angelegt=n`.
- **TS-64.2 — Regel 2 (verschwunden → markiert, nie gelöscht):** einen bereits materialisierten
  Sub-FA aus der Quelle entfernen (mit **und** getrennt ohne Rückmeldung) → die Zeile bleibt, erhält
  `SageMissingSince` (Zeitstempel), Rückmeldedaten bleiben abrufbar. Kein DELETE. SyncLog
  `vermisst_neu=n`.
- **TS-64.3 — Regel 2 selbstheilend (wieder aufgetaucht):** der in TS-64.2 verschwundene Sub-FA
  taucht wieder in der Quelle auf → `SageMissingSince` wird beim nächsten Lauf auf NULL gesetzt
  (SyncLog `wieder_da=n`).
- **TS-64.4 — Sammelmail (eine pro Lauf, nur unerwartet):** mehrere Sub-FAs verschwinden in einem
  Lauf → **genau eine** Mail (Betreff „FA-Materialisierung: N FA nicht mehr in Sage") mit der Liste,
  keine Einzel-Mail je FA. Ein verschwundener FA, der bereits als **erledigt** (`IsDone`/Komm-
  erledigt) bekannt ist, erscheint **nicht** in der Mail (wird nur markiert + geloggt). Mail nur bei
  `ErrorNotification:Enabled=true` + gesetzten `ErrorNotification:Recipients`.
- **TS-64.5 — Regel 3 (Umhängung gemeldet, nicht übernommen):** die `VaterFA` eines bereits
  materialisierten Sub-FA in der Quelle ändern → `ParentSubOrderNumber` in `ProductionOrders` bleibt
  **unverändert**, SyncLog `FaMaterialization` trägt eine Warnung „Umhängung erkannt (NICHT
  übernommen): Sub-FA … Vater X -> Y", `umhaengung_konflikt=n`.
- **TS-64.6 — Empty-Source-Guard:** ist die lokale Struktur (`FaHierarchyNodes`) leer (z. B.
  FA-Hierarchie-Sync übersprungen wegen leerem View-Read) → der Materialisierungs-Lauf markiert
  **nichts** als vermisst (kein Massen-`SageMissingSince`), SyncLog „übersprungen: Struktur-Quelle
  leer".
- **TS-64.7 — Gating / AKE unverändert:** bei `ProduktionsauftragHierarchisch = false` läuft der
  Materialisierungs-Sync **nicht**; der bestehende `SageImportService`-Pfad (AKE) bleibt die einzige
  Quelle, Verhalten identisch zu vorher.
- **TS-64.8 — Mapping-Vorbehalt (erster echter Datenlauf, Schranke 2):** am realen Sage-Datenbestand
  prüfen, dass die Wurzel (`VaterFA = NULL`) mit `HauptFA == SubFA` als Hauptauftrag
  (`OrderNumber == SubOrderNumber`) entsteht und die Parent-Kette (`child.VaterFA == parent.SubFA`)
  intakt ist; Node-Dezimalwerte `NULL→0` (analog Etappe A). Ebenfalls hier zu schärfen: behält die
  IDEAL-View fertige Aufträge oder blendet sie aus (Melderegel).

---

## TS-65 — IDEAL Teil 7 Etappe D: Lookup-Härtung + Auto-Erledigt-Sperre

**Vorbedingungen:** Hierarchischer Modus (Master an, Sub-FAs materialisiert). Automatisiert abgedeckt:
`WorkOperationRepositoryTests.GetAllByFaAndOperation_…`, `FaZusatzinfoSyncServiceTests.Fold2_…` +
`AutoDone_MultiFaPerWa_…`, `ProductionOrderReconcilerTests.Plan_HierarchischeGruppe_…`.

- **TS-65.1 — Mengenwertiger Lookup (AK 10):** zwei Sub-FAs derselben `OrderNumber` mit je einem
  Arbeitsgang „BG". `GetAllByFaAndOperationAsync("<HauptFA>","BG")` liefert **beide**; der bestehende
  `GetByFaAndOperationAsync` liefert **eine** (erste Zeile, kein Crash) und schreibt eine Log-Warnung
  „Mehrfachtreffer … Aufrufer sollte GetAllByFaAndOperationAsync nutzen (Teil 8)". Terminal-/Aufrufer-
  Verhalten unverändert (Teil 7 ändert die Aufrufer nicht).
- **TS-65.2 — Auto-Erledigt Stage 1 (datengetrieben, AK 11a):** Struktur mit zwei Sub-FAs derselben
  `OrderNumber`; ein Sage-Status „verpackt/abgeholt" am HauptFA → **kein** FA der Gruppe wird auf
  Komm-Erledigt gesetzt (`erledigt-gesetzt=0`), SyncLog-Warnung „Auto-Erledigt uebersprungen (WA …):
  N FA-Treffer (hierarchisch)". Greift unabhängig vom Schalter.
- **TS-65.3 — Auto-Erledigt Stage 2 (Schalter, AK 11b):** `Sync:FaZusatzinfoAutoErledigtEnabled` in
  `/ServiceSettings`. Bei **hierarchischen Daten** lässt er sich nicht auf `true` setzen (Guard aus
  Etappe B lehnt ab + protokolliert); beim Umlegen des Masters wird ein aktiver Schalter automatisch
  abgeschaltet (Audit `HierarchieUmstellung`). Bei `false` läuft Fold 2 auch im eindeutigen Fall nicht.
- **TS-65.4 — AKE unverändert:** im flachen Modus (Master aus, `OrderNumber` eindeutig) ist
  `matches.Count == 1` → Fold 2 läuft wie bisher (Schalter default an); kein Verhaltensunterschied.
- **TS-65.5 — Reconcile-Gruppen-Semantik (AK 12):** bei doppelter `OrderNumber` behandelt der
  Reconcile-Planer die Gruppe konsistent — HauptFA in Sage → nichts stornieren; HauptFA weg → ganze
  Gruppe Kandidat. (Reconcile ist am IDEAL i. d. R. aus; Test sichert die Semantik.)
- **TS-65.6 — OrderNumber-Sweep (AK 8):** die 7 kritischen Eindeutigkeits-Lookups (Sweep-Katalog in
  der Aufgaben-Notiz) sind als „Aufrufer-Umstellung Teil 8" dokumentiert; Teil 7 liefert nur die
  Naht (AK 10) und ändert kein Aufrufer-Verhalten. Kein manueller Testschritt außer der Doku-Prüfung.

---

## TS-66 — IDEAL Teil 8: Sub-FA-Rückmeldung / BDE-Disambiguierung (v1.33.0)

**Vorbedingungen:** Hierarchischer Modus (Master an, Sub-FAs materialisiert), BDE-Terminal an einer
Werkbank aktiv, Operator angemeldet. **Testdaten-Vorbedingung (Schranke 2):** ohne produktivnahe
hierarchische Rückmeldedaten im IDEAL-Testsystem sind TS-66.1/.2/.5 nicht grün zu bekommen — vorher
sicherstellen, dass mehrere Sub-FAs derselben `OrderNumber` an einer Werkbank offen sind.
**Automatisiert abgedeckt:** `BdeScanResolverTests` (14 Fälle), `BdeApiControllerTests.ResolveScan_*`
(5 Fälle: BadRequest ×2, Exact, Ambiguous, NurFA→ProductionOrder).

- **TS-66.1 — Mehrere Sub-FAs, Normal-Modus (AK 2/6):** FA scannen, deren `OrderNumber`+AG an dieser
  Werkbank **zwei** Sub-FAs trifft (`FA-Nr,AG-Nr`). Erwartung: Auswahlliste mit Sub-FA-Nummer +
  Bezeichnung + Arbeitsgang; Auswahl bucht **genau** diesen Sub-FA. Zweiten Sub-FA scannen/wählen →
  getrennte Buchung, keine Vermischung. Negativ: keine stille Buchung ohne Auswahl.
- **TS-66.2 — Mehrere Sub-FAs, Nur-FA-Modus (AK 5, KP-5-Fix):** `Sync`/AppSetting Nur-FA-Meldung an.
  FA scannen (ohne AG), die mehrere Sub-FAs trifft. Erwartung: **dieselbe** Auswahlliste — **kein**
  stilles „last wins" mehr auf den zuletzt gefundenen Button. Auswahl startet die Produktion auf dem
  gewählten Sub-FA (`StartProductionForOrder`). Negativ: früher wurde still der letzte Treffer gebucht.
- **TS-66.3 — Eindeutige FA / AKE-Flachfall (AK 1/9):** FA scannen, die genau **einen** Auftrag trifft
  (bzw. AKE: `OrderNumber == SubOrderNumber`). Erwartung: **Direktbuchung ohne Auswahl** (Normal:
  Start-Buttons; Nur-FA: Produktion startet). Kein Verhaltensunterschied zu vorher.
- **TS-66.4 — Auftrag nicht an dieser Werkbank (KP2-6 Randfall a):** FA scannen, die zwar existiert/läuft,
  aber **nicht** an dieser Werkbank offen ist. Erwartung: eigene Meldung *„Dieser Auftrag hat an dieser
  Werkbank keinen offenen Arbeitsgang."* — **nicht** die generische Unbekannt-Fehlermeldung.
- **TS-66.5 — Genau ein Treffer im Werkbank-Scope trotz global mehrerer (KP2-6 Randfall b):** eine
  passende Sub-FA an dieser Werkbank, eine weitere derselben `OrderNumber` an einer **anderen** Werkbank.
  Erwartung: **Direktbuchung** des hiesigen (keine Auswahl, keine Fremd-Sub-FA sichtbar).
- **TS-66.6 — Unbekannte FA (AK 4):** nicht existierende FA scannen. Erwartung: bestehende
  Fehlerbehandlung „Arbeitsgang nicht gefunden" (Normal) unverändert.
- **TS-66.7 — Kombinationsgeräte (AK 7, bekannte Grenze):** zwei Kombi-Aufträge derselben `OrderNumber`.
  Erwartung: Auswahlliste zeigt deren Sub-FAs **positionsweise** (Matchcode/Arbeitsbereich), **keine**
  automatische Auftragstrennung. Das ist die bewusst akzeptierte Grenze (`MontageAbteilung` nicht in
  `ProductionOrders`), **kein** Testfehler.
- **TS-66.8 — Bestehende BDE-Regeln unverändert (AK 8):** Mehrfachbuchung, Pause, Sperre bei
  verpackt/abgeholt, Auto-Pause Schichtende — je Sub-FA wie bisher. Regressionslauf der bestehenden
  BDE-Testszenarien gegen materialisierte Sub-FAs.
- **TS-66.9 — Teileverfolgung (Durchzug):** in der Teileverfolgung nach einer `OrderNumber` filtern.
  Erwartung: der Filter (`Contains`) listet **alle** Sub-FAs der Gruppe (gewünschtes
  Gruppierungsverhalten) — reiner Filter, kein eindeutigkeitsannehmender Lookup, unverändert.
- **TS-66.10 — Fallback SubOrderNumber (AK 3, heute unerreichbar):** Vorrüstung — solange Barcodes keine
  Sub-FA-Nummer tragen, nicht auslösbar. Automatisiert abgedeckt
  (`BdeScanResolverTests.Fallback_SubOrderNumber_…`); manuell erst prüfbar, sobald Sub-FA-Barcodes existieren.

---

## TS-67 — IDEAL Teil 6: Standorteinstellungen-Maske (v1.34.0)

**Vorbedingungen:** Admin-Anmeldung. Neue Seite unter Menü → Einstellungen → „Standorteinstellungen"
(`/StandortEinstellungen`). **Automatisiert abgedeckt:** `StandortEinstellungenControllerTests` (6),
`StandortSettingsWriterTests` (3).

- **TS-67.1 — Gruppierte Anzeige (AK 1):** Die Maske zeigt die Gruppen Firmendaten, Mandant/Sage-Views,
  Feature-Schalter, Struktur-Import. Ein View-Name hier geändert erscheint auch in
  `/ServiceSettings` (und umgekehrt) — kein zweiter Speicherort.
- **TS-67.2 — Master read-only (AK 2):** Der Master `ProduktionsauftragHierarchisch` erscheint
  **nur** als Status (aktiv/flach, gesperrt ja/nein, Stand) mit Link „Umschalten (eigene Seite)" →
  `/HierarchieUmstellung`. **Kein** Bedienelement zum Umschalten. Ein manipulierter POST mit dem
  Master-Key wird serverseitig **ignoriert** (Allow-List; der Wert ändert sich nicht).
- **TS-67.3 — AKE-Standort (AK 3):** Ohne gesetzte IDEAL-Keys zeigt die Maske leere Felder / „Flach
  (AKE-Standard)" ohne Fehler.
- **TS-67.4 — Atomares Speichern, Teil-Scheitern (AK 4):** Ein POST mit einem gültigen AppSettings-Feld
  (z.B. Firmenname) **und** einem ungültigen Int (z.B. Baum-Maximaltiefe = „abc"): **keiner** der
  Werte wird gespeichert, die Fehleranzeige behält **beide** eingegebenen Werte.
- **TS-67.5 — Firmendaten-Erstanlage (AK 5):** Firmenname erstmals befüllen → wird gespeichert, DB-Zeile
  neu angelegt. Erscheint danach **nicht** automatisch in der generischen `/Settings`-Liste (die zeigt
  nur vorhandene DB-Zeilen) — bekannte, akzeptierte Abweichung, **kein** Fehler.
- **TS-67.6 — Admin-only (AK 6):** Ohne Admin-Rolle ist `/StandortEinstellungen` nicht erreichbar
  (Class-Level `[RequireAdminAccess]`, wie `/ServiceSettings`).
- **TS-67.7 — View-Name-Validierung (H-1, UX):** Ein ungültiger View-Name wird hier **roh** gespeichert;
  er fällt erst beim nächsten Struktur-Sync als Fehlermail auf (Hinweistext in der Maske). Die Maske
  täuscht keine Validierung vor.

---

## TS-68 — BOM-Knopf im hierarchischen Modus (Guard, kein HTTP 500)

> **Abgelöst durch TS-70 (v1.36.0, BOM-Bridge).** Der Minimal-Guard (`HierarchicalBomGuardRepository`) ist im
> Code entfernt; im hierarchischen Modus erscheint jetzt die echte Stückliste aus der FA-Struktur. Die
> Szenarien TS-68.2/68.3/68.5 gelten in ihrer alten Erwartung (Hinweisseite, kein Druck-Knopf) **NICHT** mehr
> — siehe TS-70.8. Automatisiert jetzt: `BomRepositoryMasterSwitchTests`, `FaHierarchyBomRepositoryTests`
> (statt `HierarchicalBomGuardRepositoryTests`). TS-68.1 (AKE-Regression) und TS-68.6 (Unterscheidbarkeit
> `KEINE_DATEN`) bleiben sinngemäß gültig (→ TS-70.6). Der folgende Text bleibt als Historie stehen.

**Vorbedingungen:** Bündel deployt. **Automatisiert abgedeckt:** `HierarchicalBomGuardRepositoryTests` (5).

- **TS-68.1 — AKE-Regression (AK 1):** Master `false`, BOM-Knopf an bekanntem Auftrag → Positionen wie
  bisher. Einmal mit gefülltem, einmal mit leerem BOM-Cache (Cache-Treffer/-Miss) — bit-identisch zu vorher.
- **TS-68.2 — IDEAL-Grundfall (AK 2/6):** Master `true`, BOM-Knopf an einem materialisierten FA →
  **Hinweisseite** („Die Stückliste wird im hierarchischen Modus über die FA-Struktur angezeigt",
  Zwischenstand, Badge „FA-Struktur") mit funktionierendem Link/Button **zur FA-Struktur** (`/FaHierarchy`).
  **Kein 500**, keine Exception. **Nachtrag UAT-Lauf 1 (U1):** Browser-Konsole ohne App-Fehler (kein
  `TypeError … addEventListener`); die Kopfzeile nennt die **Sub-FA-Nummer** (bei Sub-FA ≠ HauptFA zusätzlich
  „| HauptFA <Nr>"); der Knopf „Stückliste drucken" wird auf der Hinweisseite **nicht** angeboten.
  Negativ (Flachmodus): Kopfzeile unverändert „FA <Nr>" ohne Zusatz, Druck-Knopf vorhanden.
- **TS-68.3 — Direktaufruf (AK 2):** `/Picking/Bom/<id>` per URL im hierarchischen Modus → gleicher
  Hinweis (belegt, dass der Guard nicht nur an einem ausgeblendeten Knopf hängt).
- **TS-68.4 — Log-Nachweis (AK 2/3):** im hierarchischen Modus erscheint **keine** SQL-Abfrage gegen
  `vw_AKE_Kommissionierung_StuecklistenDB` bzw. `[ake].[dbo]` — der Guard hängt am Master-Schalter, nicht
  an einem gefangenen Fehler (kein try/catch).
- **TS-68.5 — Weitere Aufrufer (AK 5):** Druck (`/Picking/PrintBom/<id>`) und die read-only Vorbau-
  Stückliste (Abarbeitungsliste) im hierarchischen Modus → keine Exception (leeres Ergebnis, gleicher Guard).
- **TS-68.6 — Unterscheidbarkeit (AK 4):** ein FA ohne Stückliste (AKE, „KEINE_DATEN") vs. hierarchisch
  („NICHT_VERFUEGBAR_HIERARCHISCH") sind unterschiedlich dargestellt — die leere Liste wird nicht mit dem
  hierarchischen Hinweis verwechselt.

---

## TS-69 — IDEAL: FA-Liste und verwandte Ansichten hierarchiefähig (v1.35.0)

**Bezug:** Spec `2026-08-18-fa-liste-hierarchie-anzeige-spec` (Epic Etappen A–E). **Vorbedingung:** Bündel
deployt; für die IDEAL-Fälle Master **`ProduktionsauftragHierarchisch = true`** und ein materialisierter
Bestand mit einer **mindestens dreistufigen** Struktur (Sub-FA unter Sub-FA). Für den `SageMissingSince`-
und den Offene-Buchung-Fall müssen die Werte in der Testumgebung von Hand gesetzt werden (heute „0 vermisst",
leeres IDEAL-Terminal).

**Automatisiert abgedeckt:** `ProductionOrderRepositoryGroupedTests` (Gruppierung/Pagination/Z3b),
Controller-Hierarchie-Tests je View (`ProductionOrdersControllerSlimTests`, `PickingLeitstandControllerTests`,
`PickingControllerTests`, `FaCompletionControllerTests`, `FaWorklistControllerTests`, `TrackingControllerTests`),
Kaskade (`ProductionOrderBdeStatusRepositoryTests` cascade + `PickingLeitstandControllerTests` CascadeDone/
Preview), Offene-Buchung-Zählung (`BdeBookingRepositoryTests.CountSubFasWithOpenBooking…`), Z4 Articles/Info
(`ArticlesControllerTests.Info_Hierarchical_…`), Z1-Sync-Invariante
(`FaMaterializationSyncServiceTests.RunAsync_DoesNotResetIsDoneBde…`, Service). Der manuelle Rest ist die
UI-/Bedien-Sichtprüfung am laufenden IDEAL-System (siehe „Kapitel mit besonderem Gewicht").

### Anzeige (alle sechs Ansichten)

- **TS-69.1 — F1: Master aus/an (AK 1/10).** Jede der sechs Ansichten (FA-Liste, Kommissionierung,
  Kommissionier-Leitstand, FA-Vervollständigung, FA-Abarbeitungsliste, Teileverfolgung) **einmal mit Master
  `false`** öffnen → unverändert wie vor v1.35.0 (ein `<tbody>`, keine Kopfzeilen, keine neue sichtbare Spalte,
  zeilenbasierte Paginierung, **kein** neues Bedienelement — insbesondere im Leitstand **kein** „Alle Sub-FAs
  fertigmelden"-Knopf). **Einmal mit Master `true`** → gruppiert nach Haupt-FA.
- **TS-69.2 — Vollständiger Nachfahren-Scope (AK 2).** Dreistufige Struktur: der **Enkel-Sub-FA** erscheint als
  eigene Zeile in der Haupt-FA-Gruppe, nicht nur die direkten Kinder.
- **TS-69.3 — Standardsicht aufgeklappt, Zuklappen je Gruppe (AK 4).** Beim Laden alle Gruppen offen; Klick auf
  den Chevron einer Gruppe klappt **nur diese** zu; erneut laden → wieder alle offen.
- **TS-69.4 — F2: Suchtreffer in zugeklappter Gruppe (AK 5, gefährlichster stiller Fehler).** Eine Gruppe
  zuklappen, dann nach einer `SubOrderNumber` **darin** suchen → die Gruppe klappt **automatisch auf**, Treffer
  hervorgehoben. Negativfall: Suche nach nicht existierender Nummer → klar erkennbar „kein Treffer" (nicht
  scheinbar leer).
- **TS-69.5 — Suchreihenfolge Sub-zuerst-dann-Haupt (AK 6/8).** Suche auf eine `SubOrderNumber` → genau diese
  Zeile; Suche nur auf `OrderNumber` → die ganze Gruppe. (Gilt auch für Filterkarte **und** Spaltenfilter.)
- **TS-69.6 — F3: Paginierung über Gruppen (AK 7).** Filterauswahl mit mehr Gruppen als Seitengröße → **keine
  Gruppe** über zwei Seiten getrennt; Seitenzahl entspricht der **Gruppen**anzahl.
- **TS-69.7 — Spalten-Mapping (AK 3/8).** FA-Nummer-Zelle zeigt im hierarchischen Modus `SubOrderNumber`, die
  Gruppen-Kopfzeile die Haupt-FA (`OrderNumber`). Spalte **„Übergeordnete Sub-FA"** per Zahnrad einblendbar,
  im Erstzustand ausgeblendet, Wert je Sub-FA gegen `/FaHierarchy` gegengeprüft, bei der Wurzel leer.
- **TS-69.8 — `SageMissingSince`-Badge (AK 9).** Ein Sub-FA mit gesetztem `SageMissingSince` (in der
  Testumgebung von Hand) → eigener Badge/Tooltip „Seit … nicht mehr in der Struktur gemeldet", Zeile bleibt
  sichtbar/bedienbar. Gegenprobe: ein AKE-`IsCancelled`-Auftrag im Flachmodus zeigt weiterhin nur den
  bestehenden „In Sage gelöscht"-Badge — die beiden greifen nie gleichzeitig.
- **TS-69.9 — F6: AKE-Regression, hart (AK 10).** Master `false`: alle sechs Ansichten bit-identisch zu heute,
  **kein neues Bedienelement/keine neue Aktion** (weder Zeile noch Kopfzeile), da keine Gruppen-Kopfzeile
  gerendert wird. Ausdrücklich prüfen, nicht nur zufällig erfüllt.

### Per-View-Besonderheiten

- **TS-69.10 — Kommissionier-Leitstand, Bulk-Select über Gruppen.** „Alle sichtbaren auswählen" markiert nur
  **aufgeklappte** Gruppen; eine Gruppe mit markierten Zeilen **einklappen** → Zähler/Markierung fällt auf die
  Sichtbaren zurück (kein Freigeben unsichtbarer Aufträge). `BulkRelease`/Zurücknehmen wirkt genau auf die
  markierten IDs. Memory-Filter (Datum/VK-VA) wirken auf **Zeilen** — die Gruppe bleibt, solange ≥1 Zeile passt
  (Z3b), leere Gruppen verschwinden.
- **TS-69.11 — Kommissionierung: Prioritäts-Reihenfolge.** Dringlichste Haupt-FA-Gruppe zuerst, innerhalb der
  Gruppe die Prio-Reihenfolge der Warteschlange (bewusst **nicht** SubOrderNumber). Klick auf eine Sub-FA-Zeile
  öffnet deren Stückliste.
- **TS-69.12 — FA-Abarbeitungsliste: AG-Status je Sub-FA.** Merkmal-Spalten des gewählten Arbeitsgangs + das
  Status-Auswahlfeld funktionieren je Sub-FA (AJAX `/api/fa-work-steps/set-status`) im gruppierten Modus.
- **TS-69.13 — Teileverfolgung: 3-Ebenen-Baum.** Haupt-FA → Sub-FA → Arbeitsgang (Muster wie OSEON), **zwei-
  stufiger Fortschritt** (Haupt-FA „fertige Sub-FAs / alle", Sub-FA „rückgemeldete AGs / alle"); Chevron auf
  beiden Ebenen + „Alle auf/zu"; **Rückmelden/Zurücknehmen je AG** funktioniert; Flachmodus (AKE) unverändert
  (Bootstrap-Collapse).

### Kaskade „Alle Sub-FAs fertigmelden" (nur Kommissionier-Leitstand)

- **TS-69.14 — Kaskade A: vollständiger Unterbaum (AK 11).** Klick auf „Alle Sub-FAs fertigmelden" in der
  Gruppen-Kopfzeile → **alle** Nachfahren-Sub-FAs (auch tiefere Ebenen, auch aktuell ausgeblendete) auf
  `IsDoneBde = true`.
- **TS-69.15 — Kaskade B: Bestätigungsdialog mit Zahl (AK 12).** Der Dialog nennt die Gesamtzahl der Sub-FAs
  **und separat** die Anzahl **„mit offener Buchung"** (jede nicht beendete, nicht stornierte BDE-Buchung, also
  auch pausierte/auto-pausierte). Vorbedingung: eine offene/pausierte Buchung in der Testumgebung setzen.
  **Abbruch** im Dialog → keine Buchung, keine Änderung. **Nachtrag UAT-Lauf 1 (U2):** Der Zähler „mit
  offener Buchung" ist nach dem Laden der Vorschau **immer** sichtbar — bei 0 neutral („Davon 0 mit offener
  Buchung", `alert-secondary`), bei >0 als Warnung (`alert-warning`). Negativ: schlägt die Vorschau fehl
  (Gesamtzahl „?"), bleibt der Zähler ausgeblendet; Absenden bleibt möglich (Server ist die Wahrheit).
- **TS-69.16 — Kaskade C: Atomarität (AK 13).** Simulierter Fehler mitten in der Kaskade (z. B. gesperrter
  Datensatz) → **kein** Sub-FA „halb erledigt": entweder alle betroffenen Zeilen oder keine.
- **TS-69.17 — Kaskade D: im Log als EINE Kaskade (AK 14).** Ein `ILogger`-Eintrag je Kaskade mit Haupt-FA,
  Anzahl Sub-FAs und Benutzer; die betroffenen Zeilen tragen denselben `ModifiedAt`-Zeitstempel. Keine neue
  Tabelle.
- **TS-69.18 — Keine Gruppen-Rücknahme (AK 15).** Die Gruppen-Aktion setzt **nur** auf `true`; ein Zurücknehmen
  an einem einzelnen Sub-FA verändert **weder** Geschwister **noch** den Haupt-FA.

### Z4 + Rollen

- **TS-69.19 — Z4: Zähl-Konvention.** Wo Aufträge gezählt werden, steht hierarchisch **„N Aufträge · M
  Sub-FAs"**: die Listenköpfe der sechs Ansichten **und** die Artikelinfo-Fußzeile („Teil enthalten in …
  Fertigungsaufträgen" → „N Aufträge · M Sub-FAs offen"; FA-Nr-Spalte zeigt dort die Sub-FA-Nummer). Flachmodus:
  je eine Zahl wie bisher.
- **TS-69.20 — Rollen-Regression (AK 16).** Je Ansicht ein Benutzer **ohne** den jeweiligen Zugriffsfilter →
  Ansicht bleibt gesperrt wie zuvor (unveränderte `RequireXxxAccess`-Filter). Die Kaskade-Endpunkte
  (`CascadeDone`/`CascadeDonePreview`) verlangen `RequirePickingAccess` wie die Einzelaktion.

**Vertagt (nicht Teil von v1.35.0):** Z3-Sicherheitsnetz „neuer Sub-FA unter fertigem Haupt-FA erkennen/melden"
→ Backlog `2026-09-07-invariante-haupt-fertig-sub-erkennen` (Deploy-Fork Service). „Offen anlegen" ist ohnehin
erfüllt; die Z1-Invariante (Sync fasst `IsDoneBde` nicht an) ist getestet.

---

## TS-70 — IDEAL: BOM-Bridge — Stückliste über die Schnittstelle (v1.36.0)

**Bezug:** Spec `2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec`. **Löst TS-68 ab** (Minimal-Guard
entfernt). **Vorbedingungen:** Bündel deployt; Master **`ProduktionsauftragHierarchisch = true`** und ein
materialisierter HauptFA mit **mindestens zweistufiger** Struktur (mind. eine Baugruppe mit eigenen Kindern)
im IDEAL-Testsystem. Für den Flachmodus-Gegentest (TS-70.6) ein AKE-Auftrag bzw. Master `false`.

**Automatisiert abgedeckt:** `FaHierarchyBomRepositoryTests` (13), `BomRepositoryMasterSwitchTests` (4),
`BomQuantityResolverTests` (2), `HierarchicalModeGateTests` (4, Service), `BomDiResolutionTests` (DI-Graph:
beide Interfaces → dieselbe Weiche, Flachkette ohne Zyklus), `PickingControllerTests.Bom_Hierarchical_…`,
`ReadOnlyBomBuilderTests` (hierarchisch), `ArticlesControllerTests.Info_Hierarchical_ResolvesParentSubFas_…`,
`ProductionOrderRepositoryTests.GetBySubOrderNumbersAsync_…`. Der manuelle Rest ist die Sichtprüfung am
laufenden IDEAL-System.

- **TS-70.1 — HauptFA-Vollansicht (AK 7, AK 5).**
  Vorbedingung: Master `ProduktionsauftragHierarchisch = true`, materialisierter HauptFA mit
  mehrstufiger Struktur (mind. 2 Ebenen, mind. eine Baugruppe mit eigenen Kindern) im
  IDEAL-Testsystem.
  Schritte: BOM-Knopf am HauptFA oeffnen; Zeilen jeder Ebene identifizieren; eine Position unter
  Baugruppe A und eine mit identischer `Position`-Nummer unter Baugruppe B pruefen; je eine Zeile
  aus zwei verschiedenen Baugruppen anhaken (Pick-Status).
  Erwartung: alle Ebenen sichtbar, jeder Knoten genau einmal; die beiden gleich positionierten
  Zeilen bleiben unabhaengig anhakbar (kein gemeinsamer/verwechselter Pick-Zustand); Baugruppen-Auf-
  /Zuklappen zeigt die korrekten eigenen Kinder (ohne dass am Frontend etwas geaendert wurde).
  Negativfall: eine der beiden Zeilen anhaken, Seite neu laden → nur die angehakte Zeile bleibt
  markiert, die andere nicht.

- **TS-70.2 — Sub-FA-Ansicht (AK 7).**
  Vorbedingung: derselbe HauptFA, ein Sub-FA mit eigenen direkten Kindern.
  Schritte: BOM-Knopf direkt am Sub-FA oeffnen.
  Erwartung: ausschliesslich die direkten Kinder dieses Sub-FA erscheinen, keine Enkel, keine
  Geschwister-Zweige.

- **TS-70.3 — Kommissionieren-Filter (AK 8).**
  Schritte: BOM-Knopf am HauptFA oeffnen; Spaltenfilter `Kommissionieren` auf einen konkreten
  Zielwert setzen.
  Erwartung: nur Zeilen mit passendem Kommissionier-Ziel sichtbar; Filter zuruecksetzen zeigt wieder
  **alle** Zeilen (kein vorab stumm gefilterter Zustand beim ersten Aufruf).
  Negativfall: Zielwert waehlen, fuer den es keine Zeile gibt → leere, aber erkennbar gefilterte
  Ansicht, kein Fehler.

- **TS-70.4 — Artikelinfo HauptFA/Sub-FA (AK 9).**
  Vorbedingung: ein Artikel, der als Komponente in mind. zwei verschiedenen Sub-FAs derselben oder
  unterschiedlicher HauptFAs vorkommt.
  Schritte: Artikelinfo des Artikels oeffnen.
  Erwartung: je Fundstelle HauptFA (Geraet, fett) + der jeweils direkte Sub-FA-Elternknoten als
  Zusatzspalte (**nicht mehr** Sub-FA fett/primaer wie im heutigen Etappe-D-Zwischenstand); Menge =
  Sollmenge, entsprechend beschriftet.

- **TS-70.5 — Menue „Kommissionierung" mit beiden Einstiegen (AK 11, AK 12).**
  Schritte: Menuepunkt „Kommissionierung" oeffnen.
  Erwartung: ein Dropdown mit sowohl dem Picking-Workflow-Eintrag als auch der Kommissionierliste
  (Teil 3); beide funktionieren unabhaengig voneinander am selben HauptFA (z. B. in der
  Kommissionierliste drucken, im Picking-Workflow parallel Zeilen anhaken — keine gegenseitige
  Blockade).
  Nutzer mit nur einem der beiden Zugriffe sehen keinen Dropdown, sondern den bisherigen
  Einzel-Link (Kommissionierlisten dann im Slot „Kommissionierung").

- **TS-70.6 — Flachmodus bit-identisch (AK 1).**
  **Vorbedingung (harte Deploy-Abhaengigkeit):** Der Buendel-Stand muss **auch auf dem
  AKE-Testsystem `https://akenet01.ake.at:4444` publiziert sein**. Ohne diesen zweiten Deploy
  laeuft dort der alte Stand und das Szenario prueft nichts — genau daran scheiterte der
  UAT-Lauf vom 2026-09-08 (dort faelschlich als „AKE-Instanz fehlt" notiert; es fehlte der
  Deploy, nicht die Instanz).
  **Der Master bleibt auf `akenet01` auf `false`.** Das Szenario prueft den Flachmodus; ein Flip
  dort waere ein Einwegtor (ADR 0012) und ist ausdruecklich nicht Teil des Tests.
  Schritte: BOM-Knopf an bekanntem AKE-FA oeffnen, einmal mit gefuelltem, einmal mit geleertem
  BOM-Cache.
  Erwartung: Verhalten identisch zum Stand vor dieser Spec — keine neuen Spalten, keine
  Kommissionieren-Filterkarte, `Menge` weiterhin hochgerechnet mit der Auftragsmenge.

- **TS-70.7 — Klasse-D-Gate sichtbar im Aktivitaets-Protokoll (AK 10).**
  Vorbedingung: `Sync:CoatingDetectionEnabled`/`Sync:FaWorkStepDetectionEnabled`/`Sync:BomCacheEnabled`
  aktiv, Master `true`.
  Schritte: einen Sync-Zyklus abwarten oder manuell ausloesen; Aktivitaets-Protokoll pruefen; falls
  moeglich zusaetzlich einen neuen IDEAL-Auftrag importieren lassen (um den
  `SyncSpecificArticleNumbersAsync`-Pfad ueber `SageImportService` auszuloesen).
  Erwartung: **drei** Laeufe (Coating-Erkennung, WorkStep-Erkennung, BOM-Cache-Sync) erscheinen im
  Aktivitaets-Protokoll als bewusst uebersprungen (nicht als Fehler). Der vierte Pfad —
  `SyncSpecificArticleNumbersAsync`, ausgeloest aus `SageImportService` — fuehrt **bewusst keinen
  eigenen Run** und ist deshalb **nur im Serilog-Log** sichtbar („BOM-Cache-Sync (spezifisch):
  uebersprungen — hierarchischer Modus"); im Aktivitaets-Protokoll darf dafuer **kein** Eintrag
  erwartet werden.
  In Summe: keine Coating-/WorkStep-/Cache-Erkennung findet fuer hierarchische Auftraege statt.

- **TS-70.8 — Guard vollstaendig ersetzt (AK 13).**
  Schritte: `/Picking/Bom/<id>` an einem hierarchischen Auftrag direkt aufrufen.
  Erwartung: die echte Stueckliste erscheint (nicht mehr der Hinweistext „Stueckliste wird im
  hierarchischen Modus ueber die FA-Struktur angezeigt" aus TS-68).

- **TS-70.9 — Sage-Position sichtbar (Freigabe-Bedingung a).**
  Schritte: BOM am HauptFA oeffnen; Tooltip an der Pos.-Zelle pruefen („Sage-Position: n");
  Zahnrad &rarr; Spalte „Sage-Pos." einblenden.
  Erwartung: die Original-Position je Zeile ist sichtbar, waehrend die Pos.-Spalte den Strukturpfad
  zeigt (z. B. `3.7.2`).
  Negativfall: im Flachmodus gibt es weder den Tooltip noch die Spalte „Sage-Pos.".

- **TS-70.10 — Positions-Kollision (Freigabe-Bedingung b).**
  Vorbedingung: zwei Geschwister unter demselben Sub-FA mit **gleicher Sage-Position** (Testdaten).
  Erwartung: beide Zeilen sind vorhanden, die zweite mit Pfad-Suffix `~2` und Kennzeichen
  „Kollision"; im Log steht eine Warnung (`Positions-Kollision unter Sub-FA …`); **keine Zeile
  fehlt**.
  Negativfall: ohne Kollision erscheint kein solches Kennzeichen.

- **TS-70.11 — Druck der Stueckliste (Umfang begrenzt — Bewertung haengt an der Scope-Entscheidung).**
  Schritte: BOM am HauptFA oeffnen, „Drucken" waehlen (`/Picking/PrintBom/<id>`).
  Beobachtung: der Ausdruck zeigt im hierarchischen Modus die **Vollstruktur** mit den
  Pfad-Positionen (z. B. `3.7.2`), aber **ohne** die Spalten „Komm.-Ziel", „Hauptlagerplatz",
  „Sage-Pos.", „Ebene" und „Vater-Sub-FA" — der Druck hat eine eigene Spalten-Whitelist
  (`PrintBom.cshtml`).

  **Ob das ein bewusster Zustand oder ein Mangel ist, entscheidet die Scope-Frage aus TS-70.1
  (Ruling 4: „HauptFA → Vollstruktur, ja oder nein?"). Zwei Faelle, beide zulaessig:**
  - **Fall A — Entscheidung „keine Vollstruktur im Druck" / Rueckbau auf direkte Kinder.** Dann
    ist die fehlende Whitelist-Ergaenzung **kein Fehler**: der Ausdruck zeigt eine flache Liste
    der obersten Ebene, fuer die Ebene und Vater-Sub-FA keine Aussage haetten. Szenario **PASS**,
    Zustand bleibt dokumentiert wie hier beschrieben.
  - **Fall B — Entscheidung „Vollstruktur bleibt".** Dann ist die Luecke **ein Mangel, kein
    dokumentierter Zustand**: ein mehrstufiger Ausdruck ohne „Ebene" und „Komm.-Ziel" ist in der
    Halle nicht benutzbar, weil man den Zeilen ihre Tiefe und ihr Kommissionier-Ziel nicht mehr
    ansieht. Szenario **FAIL**, und die Whitelist in `PrintBom.cshtml` ist **vor dem Merge** um
    mindestens „Ebene" und „Komm.-Ziel" zu ergaenzen.

  **Wichtig fuer die Reihenfolge:** Die Scope-Entscheidung selbst wird **am Bildschirm** getroffen,
  nicht am Ausdruck — der Ausdruck kann sie mangels dieser beiden Spalten gar nicht tragen und
  wuerde zu einem Rueckbau aus dem falschen Grund verleiten.
  Negativfall: im Flachmodus ist der Ausdruck unveraendert gegenueber dem Stand vor v1.36.0.

### Komm.-Ziel-Filter + gespeicherter Standardfilter + Badge (v1.42.0)

**Bezug:** Spec `2026-09-18-stueckliste-kommissionierziel-filter-spec`. Schliesst den oben (Fall B)
beschriebenen Druck-Befund mit (Komm.-Ziel/Ebene im Ausdruck). Client-Mode-Ansicht — die
Bildschirm-Interaktion ist **Manual-UAT**.

- **Dropdown statt Freitext (AK 1/2/3).** Hierarchische Stueckliste (HauptFA, Vollstruktur) oeffnen →
  die Filterzeile der Spalte „Komm.-Ziel" zeigt ein **Select2-Dropdown**, keine Freitexteingabe. Die
  Optionen sind die tatsaechlich in der Liste vorkommenden Kommissionierziele (DISTINCT), kein
  Eintrag, der nicht vorkommt. Negativfall: Stueckliste ohne Kommissionierziele → Dropdown zeigt
  erkennbaren Leerzustand („keine Werte", deaktiviert), keine funktionslose graue Flaeche.
- **Filtern + Auto-Aufklappen (AK 1/14).** Einen Wert waehlen → nur Zeilen mit diesem Ziel (bzw. deren
  aufgeklappte Eltern) bleiben sichtbar; ein Wert, der nur in einem **zugeklappten** Ast vorkommt,
  klappt den Ast auf und zeigt den Treffer (nicht scheinbar „nichts").
- **Mehrfachauswahl OR (AK 9).** Zwei Ziele waehlen → Zeilen **beider** Ziele sichtbar; im Profil
  speichern → kommasepariert im Feld.
- **Gespeicherter Standard (AK 5).** In Benutzereinstellungen/Profil (nur bei Master `true`) ein
  Standard-Kommissionierziel als **Textfeld** setzen (kein Dropdown, AK 13) → hierarchische Stueckliste
  erneut oeffnen → Filter ist vorbelegt **und** als Badge sichtbar.
- **Benannter Leerzustand (AK 8).** Standard auf einen in der aktuellen Liste **nicht** vorkommenden
  Wert setzen → Meldung „Standardfilter Kommissionierziel `<Wert>` aktiv — keine Treffer in dieser
  Stueckliste" mit Knopf **„Alle Filter zuruecksetzen"**; der Knopf leert **alle** aktiven
  Spaltenfilter (nicht nur Komm.-Ziel) → wieder alle Zeilen sichtbar. Teiltreffer (`KA-02,S-01`, nur
  `KA-02` vorhanden) → `KA-02`-Zeilen sichtbar, **keine** Leerzustands-Meldung.
  Gegenprobe Badge: das **×** an einem einzelnen Standardfilter-Badge setzt weiterhin **nur** diesen
  einen Filter zurueck.
- **Badge fuer ALLE VIER Standardfilter (AK 7/15).** Beschaffung, Artikelgruppe, Bezeichnung 1 und
  Kommissionierziel nacheinander als Standard setzen → je ein sichtbarer Badge oberhalb der Tabelle
  mit dem Wert und Ein-Klick-Reset (Nachweis, dass der vorbestehende stille Filter von
  Beschaffung/Artikelgruppe/Bezeichnung 1 geschlossen ist).
- **AKE-Flachmodus (AK 4/6).** AKE-Stueckliste (Master aus) → keine Komm.-Ziel-Spalte, kein Dropdown;
  ein gesetzter Komm.-Ziel-Standard filtert dort **nichts** leer, Liste vollstaendig. AKE-Einstellungen:
  Feld „Standard-Filter Kommissionierziel" fehlt (AK 12), „Bezeichnung 1" ist da.
- **Migration 92 (AK 11).** `SQL/92_*.sql` zweimal gegen dieselbe DB ausfuehren → zweiter Lauf aendert
  nichts (COL_LENGTH-Guard).
- **Druck (AK 10).** Mit aktivem Komm.-Ziel-Filter drucken → Ausdruck zeigt „Gefilterte Ansicht" im
  Kopf und die Spalten Komm.-Ziel/Hauptlagerplatz/Ebene (soweit am Bildschirm sichtbar); ohne aktiven
  Filter → kein Hinweis, vollstaendige Liste. Schliesst Fall B oben.

---

## TS-71 — IDEAL: Materialisierung: Fachliche Felder (K1/K2) (v1.37.0)

**Bezug:** Spec `2026-08-20-materialisierung-fachliche-felder-spec`, Plan
`2026-09-09-materialisierung-fachliche-felder`. Löst den UAT-Befund „PickingStatus-Zeile fehlt" ab
(TS-71.12).

**Pflicht-Vorbedingungen (für alle Szenarien dieses Kapitels):**
- Master **`ProduktionsauftragHierarchisch = true`**.
- Service-Einstellung **`Sync:ProductionOrdersEnabled = false`** (der AKE-Auftrags-Abgleich aus
  SAGE ist **ausgeschaltet**, damit dieser Sync nicht parallel in dieselben Sub-FA-Zeilen schreibt
  und die Ergebnisse verfälscht).
- Für **alle** in der Test-Struktur vorkommenden Arbeitsbereiche ist vorab je eine
  `ProductionWorkplace`-Stammdatenzeile mit demselben Namen angelegt — außer dort, wo ein Szenario
  ausdrücklich den unbekannten oder mehrdeutigen Fall prüft (TS-71.8, TS-71.9).
- Ein Haupt-FA mit **Kombigerät** (mehrere `FaHierarchyOrderInfo`-Zeilen zur selben
  Haupt-FA-Nummer, z. B. unterschiedliche Montage-Abteilungen) ist in der Testumgebung benannt und
  bekannt.

**Automatisiert abgedeckt:** `FaMaterializationSyncServiceTests` (Eager-Create der Statuszeilen,
Anlege- **und** Update-Pfad, DryRun), `FaMaterializationCoatingTests` (reine Lack-Ableitung „selbst
oder direktes Kind"), `FaMaterializationCoatingWriteTests` (Schreibpfad `HasCoatingParts` inkl.
`IsCoatingDone`-Reset), `FaMaterializationWorkplaceTests` (Zuweisung/Abweichung/unbekannt/mehrdeutig,
case-insensitiv + getrimmt), `FaMaterializationPlannerTests` (erweiterter Quell-Record),
`FaHierarchySyncServiceTests` (Kombigerät-Erkennung, einmal je Lauf), `FaHierarchyOrderInfoRepositoryTests`
(`GetByHauptFaKeysAsync`), `ProductionOrderRepositoryTests` (Kunde-Freitext- und Spaltenfilter über
den Auftragskopf, Flachmodus-Gegentest), `ProductionOrdersControllerTests` (K1 im ViewModel,
Kombigerät-Verhalten, Termin-Kaskade auf den Gruppenwert, Beschichtungstermin). Der manuelle Rest
ist die Sichtprüfung am laufenden IDEAL-Testsystem und der produktive Freigabe-Workflow im Leitstand
(TS-71.12).

- **TS-71.1 — K1 in der Kopfzeile, nicht auf den Zeilen dupliziert.**
  Vorbedingung: ein **eindeutiger** Haupt-FA (genau eine `FaHierarchyOrderInfo`-Zeile) mit gesetztem
  Kunde, Konstruktions-Termin, Fertigungstermin, Liefertermin, Priorität, AB-Nummer und
  Montage-Abteilung.
  Schritte: FA-Liste öffnen, die Gruppen-Kopfzeile dieses Haupt-FA betrachten.
  Erwartung: die Kopfzeile zeigt alle sieben K1-Werte als Abzeichen hinter dem Sub-FA-Zähler; auf
  den einzelnen Sub-FA-Zeilen erscheinen diese Werte **nicht** dupliziert (keine eigene Spalte).
  Negativfall a (**Hauptauftrag ohne Kopfzeile**): ein Haupt-FA ganz ohne
  `FaHierarchyOrderInfo`-Zeile → die Kopfzeile zeigt keines der K1-Abzeichen, Gruppe und Sub-FAs
  bleiben aber normal sichtbar und bedienbar (kein Fehler, keine leere Seite).
  Negativfall b (**Fertigungs- oder Konstruktions-Termin leer**): `FE_Termin` oder `KO_Termin` ist
  am ansonsten eindeutigen Kopf `NULL` → nur das betroffene Abzeichen fehlt, die übrigen K1-Werte
  erscheinen normal; kein Platzhalterdatum (z. B. `01.01.0001`).

- **TS-71.2 — Kombigerät: alle Varianten + Kennzeichen, keine Einzelwerte.**
  Vorbedingung: der benannte Kombigerät-Haupt-FA (mindestens zwei `FaHierarchyOrderInfo`-Zeilen mit
  unterschiedlicher Montage-Abteilung bzw. Terminen).
  Schritte: FA-Liste öffnen, Gruppen-Kopfzeile des Kombigeräts betrachten.
  Erwartung: statt der K1-Abzeichen erscheint das Kennzeichen „mehrdeutig (Kombigerät)"; direkt
  darunter eine eigene Tabelle mit **allen** Kopfvarianten im Klartext (Montage-Abt., Kunde,
  KO-Termin, FE-Termin, Liefertermin, Prio, AB-Nr.) — Zeilenzahl = Anzahl Auftragsköpfe.
  Negativfall: keiner der Einzelwerte (Kunde, Termine, Prio, AB-Nr.) der Gruppe selbst ist gefüllt —
  weder Anzeige noch Termin-Kaskade noch Beschichtungstermin wählen still eine der Varianten aus.

- **TS-71.3 — Termin-Kaskade auf den Gruppenwert; DB-Spalten bleiben leer.**
  Vorbedingung: eindeutiger Haupt-FA, zwei Sub-FAs mit unterschiedlichem Werkbank-Override für die
  Vorlaufzeit, Fertigungstermin am Kopf gesetzt, `ProductionOrder.ProductionDate` in der DB `NULL`.
  Schritte: FA-Liste öffnen; Kommissionier- und Vorkommissioniertermin beider Sub-FAs vergleichen;
  anschließend die DB-Spalte direkt prüfen.
  Erwartung: beide Sub-FAs zeigen denselben Fertigungstermin (aus dem Kopf), aber je nach
  Werkbank-Override **unterschiedliche** berechnete Termine (BG-/Vorkommissioniertermin); die
  DB-Spalte `ProductionOrder.ProductionDate` bleibt dabei `NULL`.
  Negativfall: fehlt der Kopf-Termin (`FE_Termin` `NULL`), fällt die Kaskade auf die — leere —
  DB-Zeile zurück: kein Termin, kein Fehler.

- **TS-71.4 — Kunde-Freitextfilter findet über den Auftragskopf.**
  Vorbedingung: ein Sub-FA ohne eigenen `Customer`-Wert, der Kunde ist nur am Kopf gesetzt.
  Schritte: Filterkarte → Freitextfeld „Kunde" mit einem Teilstring des Kopf-Kunden füllen.
  Erwartung: die Zeile/Gruppe wird gefunden.
  Negativfall: derselbe Filter mit einem Wert, der weder am Kopf noch auf der Zeile vorkommt →
  keine Treffer, keine falsch-positiven Gruppen.

- **TS-71.5 — Kunde-Spaltenfilter findet (auch im Leitstand).**
  Vorbedingung: wie TS-71.4.
  Schritte: Spaltenfilter „Kunde" sowohl in der FA-Liste als auch im Kommissionier-Leitstand mit
  demselben Teilstring füllen; anschließend mit Negation (`!Teilstring`) wiederholen.
  Erwartung: in beiden Ansichten wird die Zeile gefunden; die Negation blendet sie in beiden korrekt
  aus.
  Negativfall: Master `false` (AKE) — derselbe Spaltenfilter arbeitet unverändert nur gegen
  `ProductionOrder.Customer` (siehe TS-71.13).

> **ZURÜCKGEBAUT (v1.43.0, siehe [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]]):** Die
> Szenarien **TS-71.6 / 71.7 / 71.8 / 71.9** (Werkbank-aus-Arbeitsbereich-Ableitung, „Variante B",
> Abweichung/Unbekannt/Mehrdeutig) beschreiben eine **entfernte** Logik. Der Arbeitsbereich ist ein
> **Zielort**, keine Werkbank; die Ableitung samt Meldungen/Countern wurde zurückgebaut, bevor sie je
> produktiv war. Diese Szenarien bleiben zur Nachvollziehbarkeit stehen, sind aber **nicht mehr
> auszuführen**. Ersetzt durch die zwei Rückbau-Szenarien am Kapitelende (TS-71.14/71.15).

- **TS-71.6 — Werkbank wird aus dem Arbeitsbereich gesetzt.** *(zurückgebaut v1.43.0 — s. o.)*
  Vorbedingung: ein Sub-FA ohne Werkbank, dessen Arbeitsbereich in der Struktur exakt dem Namen
  einer vorhandenen Werkbank entspricht (einmal exakt, einmal mit Groß-/Kleinschreibung und
  Leerzeichen abweichend, z. B. `" s-01 "` gegen `"S-01"`).
  Schritte: einen Materialisierungs-Sync-Lauf abwarten bzw. auslösen.
  Erwartung: `ProductionOrder.ProductionWorkplaceId` zeigt danach in beiden Fällen die passende
  Werkbank; im Aktivitäts-Protokoll ist der Zähler „werkbank_gesetzt" entsprechend erhöht.
  Negativfall: derselbe Lauf mit DryRun → die Werkbank bleibt unverändert, es wird nichts
  geschrieben.

- **TS-71.7 — Abweichung wird überschrieben und gemeldet.**
  Vorbedingung: ein Sub-FA trägt bereits eine andere Werkbank (z. B. von Hand gesetzt); der
  Arbeitsbereich der Struktur zeigt auf eine andere, vorhandene Werkbank.
  Schritte: Sync laufen lassen; Aktivitäts-Protokoll prüfen.
  Erwartung: die Werkbank wird auf den Struktur-Wert überschrieben (Sage ist führend); im Protokoll
  erscheint die Meldung „Werkbank weicht vom Quellwert ab" — **nicht** die Behauptung, jemand habe
  manuell zugewiesen; der Zähler „werkbank_abweichend" erhöht sich.
  Negativfall: ein zweiter Lauf direkt danach mit unverändertem Arbeitsbereich → keine erneute
  Abweichungsmeldung (der Wert stimmt jetzt bereits überein).

- **TS-71.8 — Unbekannter Arbeitsbereich meldet, ohne zuzuweisen.**
  Vorbedingung: ein Sub-FA mit einem Arbeitsbereich, zu dem **keine** Werkbank-Stammdatenzeile
  existiert.
  Schritte: Sync laufen lassen; Aktivitäts-Protokoll prüfen; danach die fehlende Werkbank anlegen
  und einen weiteren Lauf abwarten.
  Erwartung: `ProductionOrder.ProductionWorkplaceId` bleibt `NULL` bzw. unverändert; im Protokoll
  erscheint „Unbekannte Arbeitsbereiche ohne Werkbank-Stammsatz" mit dem Namen, Zähler
  „arbeitsbereich_unbekannt" erhöht; nach dem Anlegen der Werkbank füllt der **nächste** Lauf das
  Feld automatisch.
  Negativfall: zwei aufeinanderfolgende Läufe mit identischer Menge unbekannter Namen → nur beim
  ersten Mal geht ggf. eine Sammel-Mail raus (Digest nur bei Änderung der Menge, nicht bei jedem
  Lauf).

- **TS-71.9 — Zwei Werkbänke mit gleichem Namen weisen nicht zu.**
  Vorbedingung: zwei `ProductionWorkplace`-Stammsätze mit identischem Namen (case-insensitiv/
  getrimmt gleich); der Arbeitsbereich der Struktur zeigt auf diesen Namen.
  Schritte: Sync laufen lassen; Aktivitäts-Protokoll prüfen.
  Erwartung: **keine** Zuweisung, ein bereits vorhandener Bestandswert bleibt unangetastet; im
  Protokoll erscheint „Arbeitsbereich-Name mehrfach in den Werkbank-Stammdaten", Zähler
  „arbeitsbereich_mehrdeutig" erhöht.
  Negativfall: einen der beiden Namen umbenennen → der nächste Lauf weist zu wie in TS-71.6 (kein
  „erster Treffer gewinnt" in der Zwischenzeit).

- **TS-71.10 — Lack-Kennzeichen aus der Struktur; Kippen auf „nicht gesetzt" setzt „Lackierung
  fertig" zurück.**
  Vorbedingung: ein Sub-FA, dessen direktes Kind in der Struktur als beschichtet markiert ist; die
  zugehörige `ProductionOrderPickingStatus`-Zeile steht vorab auf `HasCoatingParts = true` **und**
  `IsCoatingDone = true` (in der Testumgebung von Hand gesetzt, um den Reset zu prüfen).
  Schritte: die Struktur so ändern, dass weder der Sub-FA selbst noch ein **direktes** Kind
  beschichtet ist; Sync laufen lassen.
  Erwartung: `HasCoatingParts` kippt auf `false`, **und** `IsCoatingDone` wird ebenfalls auf `false`
  zurückgesetzt (nicht nur `HasCoatingParts`); Zähler „lackflag_gesetzt" zählt nur die im Lauf aktiv
  auf `true` gesetzten Zeilen.
  Negativfall: nur ein **Enkelkind** (nicht direktes Kind) ist beschichtet → `HasCoatingParts` bleibt
  für den Sub-FA `false` (Enkel zählen bewusst nicht).

- **TS-71.10a — Kind-Sub-FA markiert den Vater nicht (v1.47.5, Freigabe-Nachtrag 2026-10-07).**
  Regel: Ein Sub-FA ist beschichtet, wenn er **selbst** beschichtet ist oder ein direktes **Blatt**-Kind
  (`SubFA = 0`) beschichtet ist. Eine Kind-Zeile mit eigenem Sub-FA zählt nur für sich (zum Lackierer gehen
  die Teile, nicht der Korpus; gleiche Regel wie TS-77.18).
  Vorbedingung: IDEAL-Testsystem, `ProduktionsauftragHierarchisch` an. Ein HauptFA mit Sub-FA **V** (Korpus,
  selbst nicht beschichtet) und direktem Kind-Sub-FA **K** (`Beschichtet = 1`), V ohne beschichtetes Blatt-Kind.
  Fundabfrage: `SELECT v.HauptFA, v.SubFA AS V, k.SubFA AS K FROM FaHierarchyNodes v JOIN FaHierarchyNodes k
  ON k.HauptFA = v.HauptFA AND k.VaterFA = v.SubFA AND k.SubFA <> 0 AND k.Beschichtet = 1 WHERE v.SubFA <> 0
  AND v.Beschichtet = 0 AND NOT EXISTS (SELECT 1 FROM FaHierarchyNodes b WHERE b.HauptFA = v.HauptFA AND
  b.VaterFA = v.SubFA AND b.SubFA = 0 AND b.Beschichtet = 1)`.
  Für die Termin-Prüfung zusätzlich: HauptFA ist **kein** Kombigerät (ein eindeutiger Kopf) und
  `Start_Beschichtung` im Kopf ist gesetzt — sonst ist der Beschichtungstermin im hierarchischen Modus
  ohnehin leer (TS-71.11).
  Schritte **(Manual-UAT)**:
  1. **Vor** dem Service-Deploy von v1.47.5: in der FA-Liste an **V** „Beschichtung erledigt" setzen (V trägt
     dort noch das Kennzeichen).
  2. Service v1.47.5 deployen, einen Sync-Zyklus abwarten (FA-Materialisierung läuft in jedem Zyklus;
     `WorkerSettings:SyncDryRun` muss aus sein).
  3. FA-Liste und Kommissionier-Leitstand für den HauptFA öffnen.
  Erwartung: **K** trägt das Beschichtungs-Kennzeichen und einen Beschichtungstermin, **V** nicht (vor v1.47.5:
  V ebenfalls). Das in Schritt 1 an V gesetzte „Beschichtung erledigt" ist nach dem Sync weg (Reset beim
  Kippen auf `false`, TS-71.10).
  Gegenprobe: Ein Sub-FA mit beschichtetem **Blatt**-Kind bleibt gekennzeichnet — auch wenn daneben ein
  beschichteter Kind-Sub-FA hängt.
  Automatisiert: `FaMaterializationCoatingTests.Derive_ChildSubFaCoated_ParentIsFalse`,
  `…Derive_LeafChildCoated_ParentIsTrue_EvenNextToCoatedChildSubFa`.

- **TS-71.11 — Beschichtungstermin nur bei eindeutigem Kopf und gesetztem Lack-Flag.**
  Vorbedingung: eindeutiger Haupt-FA, ein Sub-FA mit `HasCoatingParts = true`, Beschichtungstermin
  (Start_Beschichtung) am Kopf gesetzt.
  Schritte: FA-Liste öffnen, Beschichtungstermin der betroffenen Zeile sowie einer Zeile ohne
  Lack-Flag prüfen.
  Erwartung: nur die Zeile mit `HasCoatingParts = true` zeigt den Termin aus Start_Beschichtung;
  Zeilen ohne Lack-Flag zeigen keinen.
  Negativfall a (**Kombigerät**): derselbe Aufbau am Kombigerät-Haupt-FA aus den
  Pflicht-Vorbedingungen → auf **keiner** Zeile erscheint ein Beschichtungstermin, auch nicht auf
  einer Zeile mit `HasCoatingParts = true`.
  Negativfall b (**kritisch, im Review gefunden**): eindeutiger Kopf, Fertigungstermin (`FE_Termin`)
  **gesetzt**, Beschichtungstermin (Start_Beschichtung) **leer** → auf der Zeile mit
  `HasCoatingParts = true` darf **kein** Beschichtungstermin erscheinen — insbesondere **nicht** der
  Fertigungstermin oder ein anderweitig geschätzter Ersatztermin. Dieser Fall ist eigens zu prüfen,
  weil er im Code-Review als möglicher Fehler identifiziert wurde: ein leerer Beschichtungstermin
  darf nie stillschweigend durch einen anderen Termin ersetzt werden.

- **TS-71.12 — Statuszeilen (UAT-Befund): Freigabe im Leitstand funktioniert.**
  Vorbedingung: (a) ein **frisch materialisierter** Sub-FA (neu in der Struktur, noch nie zuvor
  gesynct) und (b) ein **Bestands-Sub-FA**, dem — wie im gemeldeten Fehler — die
  `ProductionOrderPickingStatus`- und `ProductionOrderBdeStatus`-Zeile fehlt (in der Testumgebung
  durch Löschen der beiden Zeilen simuliert, danach einen Sync-Lauf abwarten).
  Schritte: beide Sub-FAs im Kommissionier-Leitstand einzeln freigeben; Priorität setzen;
  Kommissionierer zuweisen; danach eine Massenfreigabe über mehrere Sub-FAs inklusive dieser beiden
  auslösen.
  Erwartung: beide Sub-FAs lassen sich freigeben — **keine** Fehlermeldung „PickingStatus-Zeile
  fehlt" mehr; Priorität und Picker-Zuweisung werden gespeichert und angezeigt; die Massenfreigabe
  zählt **alle** markierten Sub-FAs mit, einschließlich der beiden geprüften.
  Negativfall: taucht nach einem Sync-Lauf weiterhin ein Sub-FA ohne eine der beiden Statuszeilen
  auf, ist das ein Rückfall des Fixes und sofort zu melden — dieser Zustand darf in der
  Testumgebung nach TS-71.12 nicht mehr vorkommen.

- **TS-71.13 — AKE-Regression bei Master `false`.**
  Vorbedingung: Master `ProduktionsauftragHierarchisch = false` (AKE) bzw. ein AKE-Auftrag.
  Schritte: FA-Liste öffnen; Kunde-Filter (Freitext und Spalte) verwenden; Werkbank- und
  Lack-T-Anzeige prüfen; einen regulären AKE-Sync-Lauf abwarten.
  Erwartung: keine K1-Daten in der Gruppen-Kopfzeile sichtbar (keine Gruppierung überhaupt);
  unveränderte Werkbank-Herkunft (weiterhin aus OSEON, nicht aus der Struktur) und unveränderte
  Lack-T-Heuristik (Artikelkategorie-Schätzung); der Kunde-Filter arbeitet ausschließlich gegen
  `ProductionOrder.Customer` wie vor v1.37.0.
  Negativfall: keiner der neuen Zähler (`statuszeilen_ergaenzt`, `werkbank_gesetzt`,
  `werkbank_abweichend`, `arbeitsbereich_unbekannt`, `arbeitsbereich_mehrdeutig`,
  `lackflag_gesetzt`, `hauptfa_mehrdeutig`) verändert sich bei einem reinen AKE-Lauf — das
  Master-Gate greift vollständig. *(Hinweis v1.43.0: die Zähler `werkbank_gesetzt`,
  `werkbank_abweichend`, `arbeitsbereich_unbekannt`, `arbeitsbereich_mehrdeutig` gibt es nach dem
  Rückbau nicht mehr — siehe die Kennzeichnung bei TS-71.6.)*

- **TS-71.14 — Rückbau-Verifikation: keine Werkbank aus dem Arbeitsbereich mehr (v1.43.0).**
  Bezug: [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]]. Vorbedingung: Master `true`; ein
  materialisierter Sub-FA, dessen `FaHierarchyNode.Arbeitsbereich` (z. B. `K-02`) exakt dem Namen
  einer vorhandenen `ProductionWorkplace` entspricht (vor dem Rückbau hätte das eine Zuweisung
  ausgelöst).
  Schritte: Materialisierungs-Sync (`DryRun=false`) abwarten/auslösen; Aktivitäts-Protokoll prüfen.
  Erwartung: `ProductionOrder.ProductionWorkplaceId` bleibt **`null`** (bzw. unverändert, falls schon
  vorher gesetzt) — **keine** Zuweisung; **kein** SyncLog-Eintrag „Unbekannte Arbeitsbereiche"/
  „Werkbank weicht ab"/„mehrdeutig", **keine** Sammelmail; die Werkbank-Zähler existieren nicht mehr.
  Coating-Regression: ein Sub-FA mit `Beschichtet = true` (selbst oder direktes Kind) zeigt weiterhin
  korrekt `PickingStatus.HasCoatingParts = true` (unverändert).
  Negativfall: taucht doch eine Werkbank-/Arbeitsbereich-Meldung oder eine automatische Zuweisung auf,
  ist der Rückbau unvollständig — sofort melden.

- **TS-71.15 — Manuelle Werkbank überlebt den Sync (AK 10, gewollte Verhaltensänderung, v1.43.0).**
  Vorbedingung: ein IDEAL-Sub-FA (Master `true`). Schritte: in `/FaCompletion` über „Werkbank setzen"
  (`SetWorkplace`) von Hand eine Werkbank zuweisen → anschließend einen Materialisierungs-Sync-Lauf
  abwarten/auslösen.
  Erwartung: die manuell gesetzte Werkbank steht danach **unverändert** am Sub-FA. Vor dem Rückbau
  wurde sie bei jedem Lauf überschrieben (Sage führend); jetzt fasst der Sync das Feld nicht mehr an.
  Negativfall: die manuelle Werkbank verschwindet/ändert sich nach dem Lauf → Rückfall, sofort melden.

Alle Rückbau-Szenarien sind teilweise Manual-UAT (Sync-Lauf-Zeitpunkt, echte Struktur-/Testdaten) —
konsistent mit dem Muster der Materialisierungs-Spec.

---

## TS-72 — Bugfix: Tabellen-Skripte greifen nicht mehr in verschachtelte Tabellen (Sortierung, Spaltenkonfiguration)

**Bezug:** Bug-Record `2026-09-10-table-filter-selektoren-nicht-skopiert-bug` (Schwere hoch).
**Ursache:** `table-filter.js` (und gleichartig `column-preferences.js`) iterierte `tbody`/`tr`/`th`
per **Descendant**-Selektor. In einer Kombigeräte-Gruppe der FA-Liste steckt eine **verschachtelte**
Varianten-Tabelle; ihre Zeilen haben sieben blanke `<td>` ohne `colspan` und passieren deshalb den
`td[colspan]`-Filter. `tbody.appendChild(row)` verschob sie danach in das **äußere** Gruppen-`tbody`.
**Fix:** alle betroffenen Iterationen auf direkte Kinder skopiert (`:scope > …`).
**Merksatz (der Denkfehler, der das verdeckt hat):** `appendChild` hängt an **das Element, auf dem es
gerufen wird** — nicht an das `tbody`, in dem die Zeile stand.

**Automatisiert abgedeckt:** `TableScriptScopedSelectorTests` — **Quelltext-Wächter**, kein
Verhaltenstest. Er hält die skopierten Selektoren fest und wird rot, wenn jemand auf eine
unskopierte Descendant-Abfrage zurückfällt. Die Wirkung selbst ist **nur** manuell nachweisbar
(kein JS-Testharness im Projekt) — dafür TS-72.1 bis TS-72.5. **TS-72.3 ist der Test gegen eine
bereits gespeicherte Spaltenpräferenz** und damit der einzige, der zeigt, wie der Fix sich für
Anwender verhält, die vor dem Fix an den Spalten „herumgestellt" haben.

- **TS-72.1 — Kombigeräte-Gruppe sortieren (der eigentliche Fehlerfall).**
  Vorbedingung: Master `ProduktionsauftragHierarchisch = true`; `/ProductionOrders` im
  hierarchischen Modus; eine HauptFA-Gruppe mit Badge **„mehrdeutig (Kombigerät)"**, also mit
  Varianten-Tabelle.
  Schritte: Varianten-Tabelle ansehen und **N = Anzahl ihrer Zeilen** sowie deren Reihenfolge
  notieren (jede Zeile hat **sieben** Zellen: Montage-Abt., Kunde, KO-Termin, FE-Termin,
  Liefertermin, Prio, AB-Nr.). Dann **einmal** auf den Spaltenkopf „Kunde" klicken.
  **Vorher-Zustand (vor dem Fix, zur Einordnung):** die Varianten-Tabelle ist nach dem Klick
  **leer**, und in der Gruppe stehen zusätzliche Fremdzeilen mit sieben Zellen in der
  25-spaltigen Tabelle.
  Erwartung (nach dem Fix): die Varianten-Tabelle ist **unverändert** — N Zeilen, je sieben Zellen,
  gleiche Reihenfolge; die äußere Tabelle enthält **keine** Fremdzeilen; sortiert werden
  ausschließlich die Sub-FA-Zeilen, und zwar innerhalb ihrer Gruppe.
  Weiter: zweiter Klick auf „Kunde" (Richtungswechsel), danach Klick auf „Komm." und auf „FA Nr." —
  jedes Mal bleibt die Varianten-Tabelle vollständig. Gruppe zu- und wieder aufklappen: die
  Varianten-Tabelle verschwindet und kommt vollständig zurück.
  Negativfall: leere Varianten-Tabelle, eine Zeile mit sieben Zellen in der äußeren Tabelle oder
  eine Sub-FA-Zeile, die in eine andere Gruppe gewandert ist → Rückfall, sofort melden.

- **TS-72.2 — Spaltenkonfiguration in Ansichten mit verschachtelter Tabelle.**
  Dies ist die **zweite** Fundstelle desselben Fehlers (`column-preferences.js`), nicht bloß eine
  Gegenprobe zu TS-72.1 — sie muss eigens geprüft werden.
  Vorbedingung: `/ProductionOrders` hierarchisch mit Kombigeräte-Gruppe **und** `/FaHierarchy`
  (Struktur-Baum mit Kopfdaten-Tabelle je Struktur).
  Schritte je Ansicht: Zahnrad → **eine der ersten Spalten** ausblenden, dann wieder einblenden —
  auf `/ProductionOrders` die Spalte **„Kunde"** (Index 3, wenn die Aktionsspalte sichtbar ist —
  ohne Kommissionier-/Vorbau-Recht Index 2), auf `/FaHierarchy` die Spalte
  **„Bezeichnung"** (Index 2; die acht Außenspalten dort sind Struktur, Matchcode, Bezeichnung,
  Arbeitsbereich, Komm.-Ziel, Soll/Fert., SubFA, Status — eine Spalte „Kunde" gibt es **nicht**).
  Anschließend im Zahnrad-Dialog eine der **ersten** Spalten per Drag-and-drop nach vorne/hinten
  ziehen; Seite neu laden (die Einstellung ist gespeichert).
  **Vorher-Zustand (vor dem Fix, zur Einordnung):** Beim Ausblenden verschwindet **in der inneren
  Tabelle** eine Zelle, die mit der ausgeblendeten Außenspalte nichts zu tun hat: getroffen wird
  die Zelle mit **derselben Positionsnummer** in der inneren Tabelle. Auf `/ProductionOrders` ist
  das die **vierte** Zelle jeder Varianten-Zeile (Index 3 = „FE-Termin" der inneren Tabelle, während
  außen „Kunde" gemeint war; ohne Aktionsspalte entsprechend die dritte Zelle = „KO-Termin"). Auf
  `/FaHierarchy` ist es die **dritte** Zelle jeder
  Kopfdaten-Zeile (Index 2 = „Status", während außen „Bezeichnung" gemeint war). Beim Umordnen der
  ersten Spalten werden die sieben bzw. sechs Zellen der inneren Tabelle **vertauscht**, Werte
  stehen dann unter der falschen Überschrift. Die äußere Tabelle verhält sich dabei korrekt — der
  Schaden ist nur in der inneren Tabelle zu sehen, und ein Neuladen behebt ihn **nicht**, weil die
  Spaltenpräferenz gespeichert ist.
  Erwartung (nach dem Fix): in der Varianten- bzw. Kopfdaten-Tabelle bleiben **alle** Zellen
  sichtbar und in ihrer ursprünglichen Reihenfolge — ausgeblendet und umgeordnet wird ausschließlich
  die äußere Tabelle; auch nach dem Neuladen.
  Negativfall: eine leere Zelle oder vertauschte Werte in der inneren Tabelle (z. B. Kunde unter
  „Montage-Abt.") → Rückfall, sofort melden.

- **TS-72.3 — Bestehende (vor dem Fix entstandene) Spaltenpräferenz laden.**
  Dieser Fall ist **nicht** durch TS-72.2 abgedeckt: Dort wird die Präferenz **auf dem gefixten
  Stand** gesetzt. Wer den Fehler vorher gesehen hat, hat möglicherweise **gegengesteuert** — eine
  Spalte wieder eingeblendet, die Reihenfolge umgestellt, etwas „repariert", das nie kaputt war.
  Die gespeicherte Präferenz ist dann das Produkt **zweier** Fehler: des Programmfehlers und der
  Gegenmaßnahme. Der Code-Fix richtet die **Darstellung**; ob das Spaltenbild danach noch
  **sinnvoll aussieht**, zeigt nur ein Test mit Altbestand.

  **Hinweis zum Format (gehört zur Beurteilung):** Der Fix ändert **nicht**, was gespeichert wird.
  Die Präferenz (`UserViewPreference`, Zahnrad) beschreibt ausschließlich Sichtbarkeit und
  Reihenfolge der **Außen**spalten; eine vor und eine nach dem Fix gesetzte Präferenz sind
  **formatgleich**. Der Unterschied lag allein in der **Nebenwirkung** auf die innere Tabelle.
  Entscheidend ist hier deshalb nicht das Format, sondern die **Beurteilung** des geladenen
  Spaltenbildes.

  Vorbedingung — eine Präferenz, die **vor** dem Fix entstanden ist; zwei Wege, beide gültig:
  - **Realistischer Altbestand (vorzuziehen):** einen Stand **vor** Commit `b9bf342` ausrollen, dort
    auf `/ProductionOrders` (oder `/FaHierarchy`) Spalten aus- und wieder einblenden bzw. umordnen —
    **bis der Fehler sichtbar war und man daran herumgestellt hat**. Danach auf den gefixten Stand
    aktualisieren, **ohne** die Präferenz zurückzusetzen.
  - **Ersatzweg, wenn kein Alt-Stand verfügbar ist:** Präferenz direkt auf dem gefixten Stand über
    das Zahnrad setzen (für den Formatteil genügt das, siehe Hinweis oben) und die „Gegensteuerung"
    von Hand nachbilden — z. B. eine ungewöhnliche Reihenfolge der **ersten drei** Spalten plus eine
    ausgeblendete Spalte mit Index ≤ 6.

  **Vorher-Zustand (Altpräferenz **ohne** Fix, zur Einordnung):** In jeder Varianten-Zeile fehlt die
  Zelle an der **Position** der ausgeblendeten Außenspalte (bzw. die Zellen stehen vertauscht, wenn
  die Reihenfolge geändert war) — und ein **Neuladen behebt es nicht**, weil die Präferenz beim
  Laden jedes Mal erneut angewendet wird. Genau dieser Zustand ist es, gegen den jemand
  gegengesteuert haben kann.

  Schritte und Erwartung:
  1. Mit der gespeicherten Altpräferenz `/ProductionOrders` im hierarchischen Modus laden, Gruppe
     mit **Kombigerät** aufsuchen.
  2. **Erwartet:** Die ausgeblendete Außenspalte ist ausgeblendet (die Präferenz wirkt unverändert).
     Die Varianten-Tabelle zeigt **alle sieben** Zellen je Zeile — keine leere, keine vertauschte.
     Der Fehler ist damit **nicht nur für neue, sondern auch für bestehende** Präferenzen behoben.
  3. **Zusätzlich zu beurteilen und zu protokollieren (nicht nur abzuhaken):** Ist die geladene
     Spaltenauswahl fachlich plausibel? **Sieht sie wirr aus, ist das kein neuer Fehler, sondern
     der Abdruck der damaligen Gegensteuerung** — die richtige Konsequenz ist dann, die Präferenz
     einmal zurückzusetzen (Zahnrad → „Auf Standard zurücksetzen"). Wer das nicht weiß, meldet die
     wirre Reihenfolge als Regression des Fixes.
  4. Gegenprobe `/FaHierarchy` analog: Kopfdaten-Tabelle je Struktur zeigt **alle sechs** Zellen je
     Zeile; Beurteilung und ggf. Zurücksetzen wie in Schritt 3.

  Negativfall: eine leere oder vertauschte Zelle in der inneren Tabelle **trotz** gefixtem Stand →
  echter Rückfall, sofort melden. Abzugrenzen davon: ein nur ungewöhnliches, aber vollständiges
  Außenspaltenbild → Altlast der Präferenz, kein Rückfall.

- **TS-72.4 — Gegenproben je Tabellentyp (unveränderte Sortierung).**
  Schritte: (a) Liste mit genau einem `<tbody>` — `/Articles` (ersatzweise `/StockOverview` oder
  `/Users`): je Spalte auf- und absteigend sortieren. (b) Gruppierte Liste **ohne** verschachtelte
  Tabelle — `/FaHierarchyKommissionierListen`: sortieren. (c) FA-Liste im **Flachmodus** (Master
  `false` bzw. AKE-Auftrag): sortieren.
  Erwartung: (a) Sortierung unverändert zum Verhalten vor dem Fix; (b) jede Gruppe sortiert **in
  sich**, keine Zeile wandert über eine Gruppengrenze, Gruppen-Kopfzeilen bleiben oben;
  (c) unverändert. In allen drei Fällen greifen Spaltenfilter (ENTER) und das Zurücksetzen weiter.
  Negativfall: veränderte Reihenfolge gegenüber dem bisherigen Verhalten, eine Gruppen-Kopfzeile
  mitten in den Daten oder eine Zeile in der falschen Gruppe.

- **TS-72.5 — Wiederholungswerte und Standard-Sortierung.**
  Schritte: FA-Liste hierarchisch öffnen (Standard-Sortierung läuft beim Laden automatisch), dann
  sortieren und eine Gruppe zuklappen/aufklappen.
  Erwartung: die Unterdrückung der Wiederholungswerte (grau/transparent ab der zweiten gleichen
  Zeile) folgt der neuen Reihenfolge und greift **nicht** in die Varianten-Tabelle; keine
  Konsolenwarnung `[table-filter] Sortierung uebersprungen` und keine JS-Fehler in der Konsole.

---

## TS-73 — IDEAL: FA-Liste ausgebaut — Matchcode, Kopfdaten je Zeile, HauptFA-Zeile, Freigabe-Kaskade (v1.38.0)

> **Freigabe-Kaskade-Teil zurückgebaut in v1.46.0, siehe TS-79** (Spec
> `2026-09-25-kommissionierung-nur-hauptfa-spec`): Mit „Kommissionierung nur am HauptFA“ werden
> Sub-FAs künftig nie kommissioniert — eine Freigabe auf Sub-FA-Ebene hätte keine Bedeutung mehr; der
> Button „Alle Sub-FAs freigeben“ (`CascadeReleasePreview`/`CascadeRelease`) wurde entfernt, bevor er
> je gemergt war. Betroffen unten: **TS-73.11–73.14 und TS-73.19** (vollständig, Titel markiert),
> sowie einzelne, button-/`CascadeRelease`-bezogene Sätze in **TS-73.18** (dessen allgemeine
> Zugriffsschutz-Prüfung für `ToggleRelease`/`BulkRelease`/`SetPriority` selbst gültig bleibt).
> Matchcode, Kopfdaten je Zeile und die HauptFA-Zeile selbst (TS-73.1–73.10) sind von diesem Rückbau
> **nicht** betroffen und bleiben gültig.

**Bezug:** Spec `2026-09-10-fa-liste-ausbau-matchcode-spec`.

**Verweis (Doppelung vermeiden):** Die Darstellungs-/Sortier-Szenarien zur gruppierten Tabelle selbst
(Selektor-Skopierung auf verschachtelte Tabellen, Verhalten der Wiederholungswert-Unterdrückung beim
Sortieren/Spaltenkonfigurieren in Kombigeräte-Gruppen) liegen in **TS-72** — hier **nicht** erneut
geprüft, nur referenziert.

**Pflicht-Vorbedingungen (für alle Szenarien dieses Kapitels, sofern nicht abweichend angegeben):**
- Master **`ProduktionsauftragHierarchisch = true`** — außer bei den AKE-Regressionsfällen
  (TS-73.3, TS-73.20), dort `false`.
- Ein eindeutiger Haupt-FA (`IsAmbiguous = false`) mit mehreren Sub-FAs und gesetztem Kunde,
  Konstruktions-/Fertigungs-/Liefertermin, Priorität, AB-Nummer und Montage-Abteilung am Kopf ist
  bekannt.
- Ein Kombigerät-Haupt-FA (`IsAmbiguous = true`, mehrere Auftragsköpfe) ist bekannt (TS-73.7).
- Ein IDEAL-Sub-FA mit bekanntem `FaHierarchyNode.Matchcode` ist bekannt.
- Für TS-73.11/12/13/15: ein Haupt-FA mit mindestens 10 Sub-FAs, davon einige bereits
  `IsReleasedForPicking = true` mit gesetztem Kommissionierer.
- Für TS-73.14 *(zurückgebaut v1.46.0 — siehe TS-79)*: je ein Testdurchlauf mit Service-Einstellung
  `KommissionierungMitZuweisung = true` und einmal `= false`.

**Automatisiert abgedeckt (Auszug):** `FaMaterializationSyncServiceTests`/`FaMaterializationPlannerTests`
(Matchcode Anlege- **und** Update-Pfad, F1/F2-Muster), `ProductionOrderRepositoryTests`/
`ProductionOrdersControllerTests` (K1-Postfilter inkl. Matchcode/Kopfdaten je Zeile, Flachmodus-
Gegentest), `ProductionOrderPickingStatusRepositoryTests` (`SetReleaseForOrderNumberAsync` —
Kaskade, Priorität, bereits Freigegebene bleiben unverändert), `PickingLeitstandControllerTests`
(`CascadeReleasePreview`/`CascadeRelease`, Zuweisungspflicht, Zugriffsschutz)
**— zurückgebaut in v1.46.0, siehe TS-79: `SetReleaseForOrderNumberAsync`, `CascadeReleasePreview`
und `CascadeRelease` samt ihrer Tests existieren nicht mehr.** Der manuelle Rest ist
die Sichtprüfung am laufenden IDEAL-Testsystem (Rendering, Spaltenausrichtung, Badge-Sichtbarkeit,
Dialogtexte) — diese Schritte sind unten ausdrücklich mit **(Manual-UAT)** gekennzeichnet und bilden
zusammen die manuelle Abnahme-Liste für Schranke 2.

- **TS-73.1 — Matchcode materialisiert (AK 1).**
  Vorbedingung: ein bereits materialisierter Bestands-Sub-FA **und** eine neu in die Struktur
  aufgenommene Sub-FA, beide mit bekanntem `FaHierarchyNode.Matchcode`.
  Schritte: einen Materialisierungs-Lauf abwarten bzw. auslösen; `ProductionOrder.Matchcode` beider
  Zeilen in der DB prüfen.
  Erwartung: beide Zeilen zeigen danach den Matchcode-Wert aus `FaHierarchyNode.Matchcode` — der
  Anlege- **und** der Update-Pfad befüllen das Feld gleich.
  Negativfall: Struktur-Matchcode ist leer → `ProductionOrder.Matchcode` bleibt `NULL`, kein
  Platzhaltertext.

- **TS-73.2 — Matchcode-Spalte sichtbar und filterbar in den fünf FA-Zeilen-Ebene-Listen (AK 2).**
  Vorbedingung: Matchcode materialisiert (TS-73.1 bereits gelaufen).
  Schritte: `/ProductionOrders`, `/PickingLeitstand`, `/FaCompletion`, `/FaWorklist` und `/Picking`
  öffnen **(Manual-UAT: Sichtprüfung je Liste)**; prüfen, dass die Spalte „Matchcode" ohne
  Zahnrad-Eingriff bereits sichtbar ist (Default) und direkt neben Artikelnummer/Bezeichnung 1 steht
  **(Manual-UAT: Spaltenposition)**; Server-Spaltenfilter auf einen bekannten Matchcode setzen.
  Erwartung: Spalte ist in allen fünf Listen standardmäßig sichtbar; der Filter liefert korrekte
  Treffer.
  Negativfall: Spalte über das Zahnrad ausblenden, Seite neu laden → bleibt für diesen Benutzer
  ausgeblendet (Präferenz wirkt je Benutzer).
  Dokumentations-Hinweis: `Picking` (Kommissionierliste) ist als FA-Zeilen-Ebene verifiziert und
  Teil des Umfangs; `OseonTracking` ist **nicht** Teil dieser Spec (siehe Erhebung) — dort ist
  keine Matchcode-Spalte zu erwarten, das ist kein Fehlbefund.

- **TS-73.3 — AKE-Regression Matchcode: leer, Filter liefert korrekt „kein Treffer" (AK 3).**
  Vorbedingung: Master `false` bzw. eine AKE-Instanz; Matchcode-Spalte eingeblendet.
  Schritte: eine der fünf Listen öffnen **(Manual-UAT)**; Spaltenfilter auf einen beliebigen Text in
  „Matchcode" setzen.
  Erwartung: die Spalte ist durchgehend leer (kein Fehler, kein Platzhalter); der Filter liefert
  korrekt „kein Treffer" — kein Absturz, keine stillschweigend leere Gesamtliste ohne erkennbaren
  Grund.
  **Abnahme-Hinweis: Der Matchcode erscheint am Standort AKE erst, wenn die Datenquelle ihn liefert
  — bis dahin bleibt die Spalte dort leer. Das ist kein Fehler.**
  Negativfall: Filter zurücksetzen → Liste zeigt wieder alle Zeilen normal an.

- **TS-73.4 — Kopfdaten je Zeile im Anzeige-Modell vorhanden (AK 4).**
  Vorbedingung: eindeutiger Haupt-FA mit mehreren Sub-FAs, Kunde nur am Auftragskopf gesetzt
  (`ProductionOrder.Customer` in der DB leer).
  Schritte: FA-Liste öffnen; Spaltenfilter „Kunde" mit dem Kopf-Kundennamen setzen.
  Erwartung: Treffer auf **Zeilenebene** (einzelne Sub-FA-Zeilen werden gefunden), obwohl
  `ProductionOrder.Customer` in der DB nicht befüllt ist.
  Negativfall: Filterwert, der weder am Kopf noch in der DB vorkommt → keine Treffer.

- **TS-73.5 — „Nur wo der Wert wechselt" an einer normalen, eindeutigen Gruppe (AK 5).**
  Vorbedingung: eindeutige Gruppe mit mindestens drei Sub-FA-Zeilen; die ersten beiden mit
  identischem Kunden direkt hintereinander, die dritte mit abweichender Montage-Abteilung.
  Schritte: FA-Liste öffnen **(Manual-UAT: visuelle Prüfung der Darstellung)**.
  Erwartung: der Kunde erscheint nur in der **ersten** der beiden gleichen Zeilen (die zweite zeigt
  die Darstellung unterdrückt/gedimmt, nicht leer); die abweichende Montage-Abteilung der dritten
  Zeile erscheint wieder normal sichtbar.
  Zusätzlich nachzuweisen: den unterdrückten Wert markieren/kopieren **(Manual-UAT: Textauswahl im
  Browser)** → der Wert ist trotz unterdrückter Darstellung im DOM vorhanden; ein Spaltenfilter auf
  „Kunde" mit demselben Wert findet auch diese Zeile.
  Negativfall: keiner — dies ist der positive Regelfall; der Kombigerät-Gegenfall steht in TS-73.7.

- **TS-73.6 — Markierung hält nach Sortierung (AK 6).**
  Verweis: die Sortier-/Selektor-Mechanik selbst (Skopierung auf verschachtelte Tabellen) ist in
  TS-72 geprüft — hier wird nur nachgewiesen, dass die Wiederholungswert-Logik nach einem Reorder
  erneut korrekt läuft.
  Vorbedingung: wie TS-73.5.
  Schritte: auf einen Spaltenkopf klicken (z. B. „Kunde") und damit einen Reorder auslösen
  **(Manual-UAT)**.
  Erwartung: nach dem Reorder steht der Kunde wieder bei der jeweils **ersten** Zeile jeder
  Wiederholungskette in der **neuen** Zeilenfolge — nicht bei einer beliebigen Zeile in der Mitte.
  Negativfall: der Wert erscheint bei einer anderen als der ersten Zeile der Kette → Regression
  (Markierung wäre serverseitig „einbrannt" statt clientseitig neu berechnet).

- **TS-73.7 — Kombigerät: K1-Zeilenspalten bleiben auf allen Zeilen leer (AK 7).**
  Vorbedingung: der bekannte Kombigerät-Haupt-FA (`IsAmbiguous = true`).
  Schritte: FA-Liste öffnen, Gruppe aufklappen **(Manual-UAT)**.
  Erwartung: auf **allen** Sub-FA-Zeilen dieser Gruppe sind Kunde, Termine, Prio, AB-Nummer und
  Montage-Abteilung leer — keine Zeile wird einer Kopfvariante zugeordnet; die Gruppen-Kopfzeile
  zeigt weiterhin alle Kopfvarianten im Klartext plus das Mehrdeutig-Badge.
  Negativfall: irgendeine Zeile zeigt einen K1-Wert (also eine geratene Zuordnung) → Fehler, weil es
  keinen Trennschlüssel auf Positionsebene gibt.

- **TS-73.8 — Kunde-Filter funktioniert weiterhin ohne SQL-Sonderweg-Join (AK 8).**
  Vorbedingung: wie TS-71.4/TS-71.5 (Sub-FA ohne eigenen `Customer`-Wert, Kunde nur am Kopf).
  Schritte: Freitextfilter „Kunde" **und** Spaltenfilter „Kunde" im hierarchischen Modus setzen —
  sowohl in der FA-Liste als auch im Kommissionier-Leitstand.
  Erwartung: beide liefern weiterhin korrekte Treffer, identisch zum TS-71-Testfall der
  Materialisierungs-Spec — jetzt über den C#-Postfilter statt den SQL-Subquery-Join.
  Negativfall: Master `false` → derselbe Filter arbeitet unverändert nur gegen
  `ProductionOrder.Customer` (das SQL-Prädikat bleibt im Flachmodus bestehen).

- **TS-73.9 — HauptFA-Zeile vorhanden und erkennbar (AK 9).**
  Vorbedingung: Code-Nachweis ist bereits erbracht, dass die Wurzelzeile materialisiert wird; ein
  Haupt-FA mit mehreren Sub-FAs ist bekannt.
  Schritte: FA-Liste im hierarchischen Modus öffnen, Gruppe aufklappen **(Manual-UAT:
  Badge-Sichtbarkeit)**.
  Erwartung: die erste Zeile der Gruppe ist die HauptFA selbst, gekennzeichnet mit dem Badge
  „HauptFA"; ihre Aktionen (z. B. Stückliste-Button) sind direkt in der Zeile bedienbar wie bei
  jeder anderen Zeile.
  Negativfall: keine Zeile trägt das Badge bzw. die Wurzel fehlt in der Gruppe → sofort melden
  (Materialisierungslücke), nicht selbst nachbauen.

- **TS-73.10 — Kopfzeile schlanker, aber nicht leer (AK 10).**
  Vorbedingung: eindeutiger Haupt-FA mit gesetztem Kunde und Fertigungstermin.
  Schritte: FA-Liste öffnen, Gruppen-Kopfzeile betrachten **(Manual-UAT)**; Gruppe zuklappen, erneut
  betrachten **(Manual-UAT: Gegenprobe zugeklappt)**.
  Erwartung: die Kopfzeile zeigt HauptFA-Nummer, Sub-FA-Zahl-Badge, Kunde, Fert.-Termin (bzw. bei
  Kombigerät das Mehrdeutig-Badge statt Kunde/Termin); Prio, AB-Nummer, Montage-Abteilung sowie die
  übrigen Termine (KO-/Liefertermin) erscheinen dort **nicht** mehr. Bei **zugeklappter** Gruppe
  sind Kunde und Fert.-Termin weiterhin sichtbar.
  Negativfall: die zugeklappte Kopfzeile zeigt nur noch Nummern (Kunde/Termin fehlen) → Regression,
  da die zugeklappte Ansicht dann den Überblick verliert, für den sie gebaut wurde.

- **TS-73.11 — Freigabe-Kaskade — Normalfall (AK 11).** *(zurückgebaut v1.46.0 — siehe TS-79)*
  Vorbedingung: HauptFA mit z. B. 10 noch nicht freigegebenen Sub-FAs, `KommissionierungMitZuweisung
  = false`.
  Schritte: Leitstand öffnen, „Alle Sub-FAs freigeben" an der HauptFA-Gruppe auslösen
  **(Manual-UAT: Bestätigungsdialog lesen)**.
  Erwartung: die Vorschau zeigt Gesamtzahl 10, davon 0 bereits freigegeben, 10 neu freizugebende;
  nach Bestätigung sind alle 10 Zeilen `IsReleasedForPicking = true` (ein `SaveChangesAsync`,
  atomar), Erfolgsmeldung nennt „10 Sub-FA(s) freigegeben".
  Negativfall: Abbruch im Dialog vor der Bestätigung → keine Änderung in der DB.

- **TS-73.12 — Freigabe-Kaskade rührt bereits Freigegebene nicht an (AK 12).** *(zurückgebaut v1.46.0 — siehe TS-79)*
  Vorbedingung: HauptFA mit 10 Sub-FAs, davon 3 bereits `IsReleasedForPicking = true`, mit
  gesetztem Kommissionierer und bekannten Audit-Feldwerten.
  Schritte: Kaskade auslösen; die 3 bereits freigegebenen Zeilen vor/nach vergleichen.
  Erwartung: diese 3 Zeilen bleiben unverändert — Wert, `ModifiedAt`/`ModifiedBy`/
  `ModifiedByWindows` **und** der bereits gesetzte Kommissionierer sind identisch zum Vorzustand.
  Negativfall: eine der 3 Zeilen verliert ihren Kommissionierer oder bekommt ein neues `ModifiedAt`
  → Regression.

- **TS-73.13 — Freigabe-Kaskade meldet die korrekte Anzahl (AK 13).** *(zurückgebaut v1.46.0 — siehe TS-79)*
  Vorbedingung: wie TS-73.12 (10 gesamt, 3 bereits freigegeben).
  Schritte: Kaskade auslösen, Dialog-Vorschau und Erfolgsmeldung lesen **(Manual-UAT:
  Meldungstext)**.
  Erwartung: die Vorschau zeigt Gesamt 10, davon 3 bereits freigegeben, 7 neu freizugebende; die
  Erfolgsmeldung nennt 7 — **nicht** 10.
  Negativfall: die Meldung nennt 10 (Gesamtzahl statt tatsächlich neu freigegebener) → Regression.

- **TS-73.14 — Zuweisungspflicht gilt auch für die Kaskade — inkl. Negativtest (AK 14, Pflicht).**
  *(zurückgebaut v1.46.0 — siehe TS-79: das gesamte Szenario prüft die entfernte Freigabe-Kaskade —
  Dialog, „allen N Sub-FAs zugewiesen"-Text, `CascadeRelease` mit/ohne Kommissionierer — und ist
  nicht mehr ausführbar; Text unten zur Nachvollziehbarkeit stehen gelassen)*
  Vorbedingung a: `KommissionierungMitZuweisung = true`, HauptFA mit nicht freigegebenen Sub-FAs.
  Schritte a (Negativfall, **Pflicht-Nachweis**): die Kaskade **ohne** Kommissionierer-Auswahl
  auslösen (Dialog ohne Auswahl bestätigen bzw. direkter `POST` auf `CascadeRelease` ohne
  `assignedPickerId` — **`CascadeRelease` zurückgebaut in v1.46.0, dieser Aufruf ist nicht mehr
  ausführbar, siehe TS-79**) **(Manual-UAT: Pflichtfeld-Verhalten im Dialog)**.
  Erwartung a: die Aktion wird abgewiesen, Meldung „Bitte einen Kommissionierer zuweisen."; **keine**
  Zeile wird freigegeben — die Massenaktion umgeht die bestehende Pflicht nicht (Kern von AK 14/B2).
  Schritte b: dieselbe Kaskade **mit** Kommissionierer-Auswahl auslösen.
  Erwartung b: alle kaskadierten Sub-FAs bekommen denselben Kommissionierer zugewiesen; der Dialog
  nennt vorher im Klartext „Der gewählte Kommissionierer wird allen N Sub-FAs zugewiesen und kann
  danach je Sub-FA geändert werden." **(Manual-UAT: Dialogtext)**; nach der Kaskade lässt sich der
  Kommissionierer je Sub-FA einzeln ändern (`ChangeAssignedPicker`).
  Vorbedingung/Schritt c: `KommissionierungMitZuweisung = false`, Kaskade ohne Kommissionierer
  auslösen.
  Erwartung c: die Kaskade läuft ohne Zuweisungspflicht durch — wie die Einzelfreigabe auch.

- **TS-73.15 — Priorität fortlaufend, in definierter Reihenfolge (AK 15).**
  Vorbedingung: HauptFA mit mehreren noch nicht freigegebenen Sub-FAs mit unterschiedlichen
  `SubOrderNumber`-Werten; aktueller `MAX(PickingPriority)` im Leitstand bekannt.
  Schritte: Kaskade auslösen; `PickingPriority` aller neu freigegebenen Zeilen in der DB prüfen.
  Erwartung: fortlaufende Werte ab `MAX + 1`, vergeben in der Reihenfolge `SubOrderNumber`
  aufsteigend — die Gruppe steht danach im nach Priorität sortierten Leitstand zusammenhängend und
  in Sub-FA-Reihenfolge **(Manual-UAT: Reihenfolge im Leitstand optisch gegenprüfen)**.
  Negativfall: die Reihenfolge folgt der zufälligen DB-Lesereihenfolge statt `SubOrderNumber` →
  Regression.

- **TS-73.16 — Keine Gruppen-Rücknahme (AK 16).**
  Vorbedingung: HauptFA, deren Sub-FAs per Kaskade freigegeben wurden.
  Schritte: Leitstand-Oberfläche nach einer Rücknahme-Aktion für die ganze Gruppe durchsuchen
  **(Manual-UAT)**; danach eine einzelne Zeile per `ToggleRelease` zurücknehmen.
  Erwartung: es gibt **keine** Bedienoberfläche, die die ganze Gruppe zurückzieht; die
  Einzel-Rücknahme über `ToggleRelease` funktioniert weiterhin zeilenbasiert wie vor dieser Spec.
  Negativfall: ein Button/eine Aktion zieht die ganze Gruppe zurück → außerhalb des Scopes, sofort
  melden.

- **TS-73.17 — `BulkRelease` unverändert (AK 17).**
  Vorbedingung: mehrere Sub-FAs unterschiedlicher HauptFAs, per Checkbox markiert (mindestens eine
  Gruppe nur teilweise markiert).
  Schritte: Mehrfachauswahl-Freigabe (`BulkRelease`) auf die markierten Zeilen auslösen.
  Erwartung: Verhalten bit-identisch zum Vorzustand — nur die markierten Zeilen werden freigegeben,
  **kein** kaskadierendes Verhalten über ganze HauptFA-Gruppen, keine neue Option im Dialog.
  Negativfall: `BulkRelease` auf eine einzelne markierte Sub-FA einer Gruppe gibt zusätzlich
  unmarkierte Geschwister-Zeilen derselben Gruppe frei → Regression (Kaskade hat sich in die
  bestehende Aktion eingeschlichen).

- **TS-73.18 — Zugriffsschutz korrekt (AK 18).**
  Vorbedingung: ein Benutzer ohne Leitstand-Recht (erfüllt `RequireLeitstandAccess` nicht).
  Schritte: Oberfläche als dieser Benutzer öffnen **(Manual-UAT: Button-Sichtbarkeit)**; zusätzlich
  einen direkten `POST` auf `CascadeRelease` bzw. `GET` auf `CascadeReleasePreview` auslösen (z. B.
  über die Entwicklertools des Browsers) — **beide Routen liefern seit v1.46.0 grundsätzlich 404
  (Rückbau, siehe TS-79), unabhängig vom Zugriffsrecht; dieser Teilschritt ist damit gegenstandslos.**
  Erwartung: der „Alle Sub-FAs freigeben"-Button ist für diesen Benutzer **nicht** sichtbar (nur
  bei `CanManagePickingRelease`) — **dieser Button-Teil ist seit v1.46.0 gegenstandslos: der Button
  existiert seit dem Rückbau für niemanden mehr, unabhängig vom Recht, siehe TS-79**; der direkte
  Aufruf wird abgewiesen (403/Redirect), wie bei den übrigen `RequireLeitstandAccess`-Actions
  (`ToggleRelease`/`BulkRelease`/`SetPriority`) — diese Actions selbst und ihr Zugriffsschutz bleiben
  von diesem Rückbau **unberührt** und sind weiterhin so zu prüfen.
  Negativfall: der Button ist sichtbar oder der direkte `POST` (auf `CascadeRelease`) gelingt ohne
  das Recht → Zugriffslücke, sofort melden. **Dieser gesamte Negativfall ist seit v1.46.0
  gegenstandslos (Button und `CascadeRelease` existieren nicht mehr, siehe TS-79); ein äquivalenter
  Negativfall für `ToggleRelease`/`BulkRelease`/`SetPriority` ohne das Recht bleibt gültig, ist aber
  nicht Gegenstand dieses (Kaskade-spezifischen) Satzes.**

- **TS-73.19 — Frühere Spec nachgezogen, kein Widerspruch mehr (AK 19).** *(zurückgebaut v1.46.0 —
  siehe TS-79: die hier dokumentierte Aktion `CascadeRelease` existiert nicht mehr)*
  Rein dokumentarisches Kriterium, keine Bildschirmprüfung. Nachweis: in
  `secondbrain/specs/freigegeben/2026-08-18-fa-liste-hierarchie-anzeige-spec.md` (Hauptcheckout) ist
  die frühere Festlegung „`BulkRelease` ist Freigabe, nicht Fertigmeldung — keine Kaskade" um die
  neue Aktion `CascadeRelease` ergänzt/widerrufen dokumentiert, sodass sich die beiden Specs nicht
  mehr widersprechen. Der Nachzug erfolgt im Hauptcheckout, nicht im Worktree — für die QA-Abnahme
  genügt der Verweis auf die entsprechende Brain-Änderung.

- **TS-73.20 — AKE-Regression gesamt, Flachmodus-Regression (AK 20).**
  Vorbedingung: Master `ProduktionsauftragHierarchisch = false` (AKE) bzw. ein AKE-Auftrag.
  Schritte: FA-Liste öffnen **(Manual-UAT: visuelle Gesamtprüfung)**; vorhandene Spalten, Filter und
  Sortierung mit dem Stand vor v1.38.0 vergleichen.
  Erwartung: bit-identisches Verhalten zum Vorzustand — **keine** neue Spalte sichtbar außer dem
  (leeren) Matchcode; **keine** Kaskaden-Aktion (AKE kennt ohnehin keine Gruppen-Kopfzeile);
  unveränderte Filter und unveränderte Sortierung. Der leere Matchcode bei AKE ist **kein Fehler**
  und als solcher in der manuellen Checkliste zu vermerken.
  Negativfall: eine der Kopfdaten-je-Zeile-Spalten (Prio/AB-Nummer/Montage-Abteilung/KO-Termin)
  erscheint bei AKE sichtbar → Regression — diese Spalten sind laut `ColumnDefinitions.cs` an
  `Model.Hierarchical` gebunden (nicht nur `DefaultHidden`), bei Master `false` dürfen sie gar nicht
  gerendert werden, nicht nur leer bleiben.

## TS-74 — IDEAL: PDF-Erzeugung für FaHierarchy-Druckdokumente (v1.39.0)

**Bezug:** Spec `2026-08-06-pdf-erzeugung-fahierarchy-druck-spec`.

**Pflicht-Vorbedingungen (für alle Szenarien dieses Kapitels, sofern nicht abweichend angegeben):**
- Microsoft Edge ist auf dem Test-/Zielsystem installiert und `PdfRenderStatus.IsAvailable == true`
  (Startlog zeigt „PDF-Erzeugung: msedge.exe gefunden: `<Pfad>`").
- Feature-Schalter `FaHierarchyKommissionierlistenAktiv` bzw. `FaHierarchyBeschichtungAktiv` aktiv,
  entsprechender Zugriff (Lager-Verarbeitung bzw. Rolle `beschichtungsauftrag`) vorhanden.
- Ein bekannter HauptFA mit mindestens einer Position ist in beiden Listen bekannt; für TS-74.2
  zusätzlich ein Kombigerät-HauptFA (mehrere Auftragsköpfe).
- `PdfRender`-Werte in `appsettings.json` auf Standard (`TimeoutSeconds = 30`, `MaxConcurrent = 2`,
  `EdgePath` leer), sofern ein Szenario ausdrücklich etwas anderes verlangt.

**Automatisiert abgedeckt (Auszug):** `PdfFileNameBuilderTests` (Dateiname-Format), `PdfRenderStatusTests`
(Edge-Erkennung über Konfigurationspfad/Registry/Standardpfade/PATH, fehlender konfigurierter Pfad ⇒
nicht verfügbar, kein stiller Fallback), `EdgeProcessRunnerSmokeTests` (echter Edge-Aufruf, kein Mock
— produzierte am 2026-09-14 auf dem Entwicklungsrechner eine 15 711-Byte-PDF-Datei),
`PdfRenderServiceTests` (Laufverzeichnis-Lebenszyklus, verwaiste Ordner, Fehlerpfade),
`PdfRenderServiceDiResolutionTests` (Singleton-Registrierung ⇒ ein Semaphor über alle Anfragen, AK 14),
`FaHierarchyPdfActionGuardTests` (Zugriffsschutz der `Pdf`-Action identisch zu `Index`),
`FaHierarchyPdfButtonSourceTests` (Knopf-Quelltext: kein `target`-Attribut, PDF-Route korrekt aus
`hauptFa` + Query zusammengesetzt). Der manuelle Rest ist die Sichtprüfung am laufenden
IDEAL-Testsystem (Edge tatsächlich am System, Datei-Download im Browser, Log-Zeilen im laufenden
Betrieb, Timeout-Verhalten unter echter Prozesslast) — diese Schritte sind unten ausdrücklich mit
**(Manual-UAT)** gekennzeichnet und bilden zusammen die manuelle Abnahme-Liste für Schranke 2.

- **TS-74.1 — Happy Path Kommissionierliste (AK 1, 2, 3).** **(Manual-UAT)**
  Vorbedingung: Lager-Verarbeitungszugriff, `FaHierarchyKommissionierlistenAktiv = true`, Edge
  verfügbar, bekannter HauptFA mit mehreren Positionen, kein aktiver Filter.
  Schritte: `/FaHierarchyKommissionierListen` öffnen; an der Gruppen-Kopfzeile des bekannten HauptFA
  auf „PDF" klicken.
  Erwartung: der Browser lädt sofort eine Datei `Kommissionierliste_<HauptFA>_<Datum-Uhrzeit>.pdf`
  herunter (kein neuer Tab, kein Zwischenschritt); Inhalt und Aussehen entsprechen genau der
  Bildschirmdruck-Ansicht (`Print.cshtml`) derselben Gruppe — gleiche Spalten, gleiche Positionen,
  gleicher Barcode.
  Negativfall: Edge nicht verfügbar (`PdfVerfuegbar = false`) → der Knopf „PDF" fehlt komplett in der
  Gruppen-Kopfzeile (kein deaktivierter Knopf, kein Fehlerdialog) — siehe TS-74.6.

- **TS-74.2 — Happy Path Beschichtungsauftrag inkl. Kombigerät-Banner (AK 2, 3).** **(Manual-UAT)**
  Vorbedingung: Rolle `beschichtungsauftrag`, `FaHierarchyBeschichtungAktiv = true`, Edge verfügbar,
  ein Kombigerät-HauptFA (mehrere Kopfzeilen/Auftragsköpfe) bekannt.
  Schritte: `/FaHierarchyBeschichtung` öffnen; an der Gruppen-Kopfzeile des Kombigerät-HauptFA auf
  „PDF" klicken.
  Erwartung: die heruntergeladene Datei heißt `Beschichtungsauftrag_<HauptFA>_<Datum-Uhrzeit>.pdf`;
  der Inhalt zeigt denselben Kombigerät-Hinweis wie der Bildschirmdruck („… Kopfzeilen
  (Kombinationsgerät). Dienstleister, RAL …") — die PDF-Erzeugung rendert exakt dieselbe Razor-View,
  verliert also keinen Warnhinweis.
  Negativfall: ein normaler (nicht mehrdeutiger) HauptFA erzeugt ein PDF ohne Kombigerät-Hinweis —
  kein falscher Hinweis bei eindeutigen Aufträgen.

- **TS-74.3 — Dateiname (AK 3, 10, 11).** Automatisiert: `PdfFileNameBuilderTests`. Zusätzlich
  manuelle Sichtprüfung des Downloads. **(Manual-UAT)**
  Vorbedingung: wie TS-74.1.
  Schritte: PDF für einen bekannten HauptFA zu einer bekannten Uhrzeit herunterladen; den vom
  Browser vorgeschlagenen Dateinamen ablesen.
  Erwartung: der Dateiname folgt exakt `<Dokumentart>_<HauptFA>_<yyyyMMdd-HHmm>.pdf` (z. B.
  `Kommissionierliste_123456_20260914-1432.pdf`), mit der lokalen Uhrzeit des Downloads
  (`DateTime.Now`, keine UTC-Verschiebung sichtbar) und ohne Sonderzeichen.
  Negativfall: zwei Downloads derselben Gruppe innerhalb derselben Minute erzeugen denselben
  Dateinamen (bewusst so, kein Kollisionsschutz nötig, der Browser nummeriert selbst durch) — kein
  Fehlerfall, nur zur Kenntnis.

- **TS-74.4 — Temp-Aufräumen inkl. verwaister Datei (AK 5, 17).** **(Manual-UAT)**; automatisiert
  `PdfRenderServiceTests`.
  Vorbedingung: Zugriff auf das Dateisystem des Web-Servers (`%TEMP%\IdealAkeWms-Pdf`);
  `PdfRender:TimeoutSeconds` auf Standard (30).
  Schritte: Ordner `%TEMP%\IdealAkeWms-Pdf` vor dem Test prüfen/leeren; von Hand einen Unterordner mit
  beliebigem Namen anlegen und dessen Erstellzeit per PowerShell auf mehr als 35 Minuten
  (`TimeoutSeconds` + 5 Minuten Kulanz) in die Vergangenheit setzen, z. B.
  `(Get-Item $dir).CreationTimeUtc = (Get-Date).ToUniversalTime().AddMinutes(-40)`; danach über die
  Anwendung ein PDF erzeugen.
  Erwartung: nach dem PDF-Aufruf ist der künstlich gealterte Ordner verschwunden (wird vor dem
  eigentlichen Lauf als „verwaist" entfernt); der für den aktuellen Lauf angelegte GUID-Ordner (mit
  `in.html`, `out.pdf`, Unterordner `profile`) existiert nach erfolgreichem Abschluss ebenfalls nicht
  mehr (Aufräumen im `finally`-Block).
  Negativfall: ein Ordner, der jünger als der Cutoff ist (z. B. 10 Minuten alt), bleibt beim
  Aufräum-Lauf unangetastet stehen — kein Übereifer, der einen noch laufenden Parallel-Aufruf stört.

- **TS-74.5 — Parallelaufruf zwei Tabs (AK 8).** **(Manual-UAT)**
  Vorbedingung: `PdfRender:MaxConcurrent` auf Standard (2) oder höher; zwei bekannte
  HauptFA-Gruppen.
  Schritte: in zwei Browser-Tabs nahezu gleichzeitig je einen PDF-Download für unterschiedliche
  HauptFA auslösen.
  Erwartung: beide Downloads laufen unabhängig durch, keiner scheitert oder wartet unnötig lange; im
  `%TEMP%\IdealAkeWms-Pdf`-Ordner sind während der Erzeugung zwei getrennte GUID-Unterordner
  sichtbar, die sich nicht gegenseitig überschreiben.
  Negativfall: einer der beiden Downloads liefert einen Fehler oder ein leeres/beschädigtes PDF →
  Regression im GUID-Laufverzeichnis-Konzept, sofort melden.

- **TS-74.6 — Edge fehlt beim Start (AK 13).** **(Manual-UAT — Änderung an `appsettings.json` und
  Neustart der Anwendung erfolgen auf dem Testsystem, nicht im Worktree.)**
  Vorbedingung: Zugriff auf `appsettings.json` und Neustart-Möglichkeit der Anwendung auf dem
  Testsystem.
  Schritte: `PdfRender:EdgePath` auf einen nicht existierenden Pfad setzen (z. B.
  `C:\nicht-vorhanden\msedge.exe`); Anwendung neu starten; anschließend
  `/FaHierarchyKommissionierListen` und `/FaHierarchyBeschichtung` öffnen.
  Erwartung: im Startlog erscheint die Warnung „PDF-Erzeugung NICHT verfuegbar: konfigurierter Pfad
  PdfRender:EdgePath existiert nicht: `<Pfad>`"; in beiden Listen fehlt der „PDF"-Knopf an jeder
  Gruppen-Kopfzeile vollständig; der „Drucken"-Link bleibt unverändert vorhanden und funktioniert
  weiterhin.
  Zusatzschritt (Notfallpfad AK 6): die URL `/FaHierarchyKommissionierListen/Pdf/<HauptFA>` direkt
  eingeben. Erwartung: Rücksprung auf die Liste mit der gelben Meldung „PDF-Erzeugung ist auf diesem
  Server nicht eingerichtet." — kein HTTP 500, keine Fehlerseite.
  Negativfall: der „PDF"-Knopf erscheint trotzdem (z. B. deaktiviert) oder ein Klick darauf führt zu
  einer Serverfehlerseite statt zu fehlendem Knopf → Regression, `PdfVerfuegbar` wird nicht korrekt
  aus `PdfRenderStatus.IsAvailable` übernommen.

- **TS-74.7 — Timeout (AK 7).** **(Manual-UAT — Änderung an `appsettings.json` und Neustart der
  Anwendung erfolgen auf dem Testsystem, nicht im Worktree.)**
  Vorbedingung: `PdfRender:TimeoutSeconds = 1` gesetzt und `PdfRender:EdgePath` versuchsweise auf eine
  Anwendung, die nichts in die Zieldatei schreibt (z. B. `notepad.exe`), oder alternativ ein sehr
  großes Dokument gegen den echten Edge; Anwendung neu gestartet.
  Schritte: ein PDF für einen bekannten HauptFA anfordern; Windows-Task-Manager während und nach dem
  Aufruf beobachten.
  Erwartung: nach ca. 1 Sekunde bricht die Erzeugung ab, der Anwender landet zurück auf der Liste mit
  der gelben Meldung „Das PDF konnte nicht erzeugt werden. Bitte den Bildschirmdruck verwenden.";
  im Log erscheint „PDF-Erzeugung: Timeout nach 1s, Edge-Prozesse beendet. stderr: …"; im
  Task-Manager sind keine übrig gebliebenen `msedge.exe`-Prozesse aus diesem Aufruf zu finden (werden
  beim Timeout aktiv beendet).
  Negativfall: nach dem Timeout bleiben `msedge.exe`-Zombie-Prozesse im Task-Manager stehen, oder die
  Fehlermeldung fehlt bzw. die Seite hängt → Regression im `KillChildren`-Pfad des
  `EdgeProcessRunner`.

- **TS-74.8 — Zugriffsschutz (AK 9).** **(Manual-UAT)**; automatisiert
  `FaHierarchyPdfActionGuardTests`.
  Vorbedingung: ein Anwender ohne die für `FaHierarchyBeschichtung` nötige Rolle
  (`RequireBeschichtungsauftragAccess` nicht erfüllt) bzw. ohne Lager-Verarbeitungszugriff für die
  Kommissionierliste.
  Schritte: als dieser Anwender direkt `/FaHierarchyBeschichtung/Pdf/<bekannter HauptFA>` bzw.
  `/FaHierarchyKommissionierListen/Pdf/<bekannter HauptFA>` aufrufen (URL-Leiste, ohne über den
  Knopf zu gehen).
  Erwartung: derselbe Zugriffsfehler wie beim direkten Aufruf von `Index` (403/Redirect je nach
  konfiguriertem Zugriffsschutz) — die `Pdf`-Action ist durch denselben Class-Level-Filter geschützt,
  kein Sonderweg am Filter vorbei.
  Negativfall: der direkte Aufruf liefert ein PDF trotz fehlender Rolle → Zugriffslücke, sofort
  melden.

- **TS-74.9 — Regression Bildschirmdruck (AK 12).** **(Manual-UAT)**
  Vorbedingung: wie TS-74.1/TS-74.2, jeweils mit und ohne aktiven Filter.
  Schritte: in beiden Listen auf „Drucken" klicken.
  Erwartung: der Link öffnet weiterhin einen neuen Tab (`target="_blank"`) mit **allen** gefilterten
  Gruppen (nicht nur einer) im bekannten Bildschirmdruck-Layout — unverändert zum Verhalten vor
  Einführung des PDF-Knopfs.
  Negativfall: „Drucken" verhält sich wie „PDF" (nur eine Gruppe, direkter Download) → der PDF-Knopf
  hätte versehentlich den bestehenden Druck-Pfad verändert statt ihn nur zu ergänzen.

- **TS-74.10 — Direkter Download, kein neuer Tab (AK 16).** **(Manual-UAT)**; automatisiert
  `FaHierarchyPdfButtonSourceTests`.
  Vorbedingung: wie TS-74.1.
  Schritte: auf den „PDF"-Knopf einer Gruppe klicken und dabei beobachten, ob ein neuer Tab/Fenster
  aufgeht.
  Erwartung: kein neuer Tab öffnet sich, der Download läuft im aktuellen Tab über die
  Browser-Download-Leiste — der Link trägt bewusst **kein** `target="_blank"` (anders als
  „Drucken").
  Negativfall: ein neuer, leerer oder hängender Tab öffnet sich zusätzlich zum Download → der
  `<a>`-Knopf hat versehentlich ein `target`-Attribut bekommen.

- **TS-74.11 — Filter-Hinweis im Kopf (AK 18).** **(Manual-UAT)**
  Vorbedingung: wie TS-74.1, zusätzlich ein aktiver Spaltenfilter bzw. (nur Kommissionierliste) ein
  aktives Kommissionier-Ziel.
  Schritte: Spaltenfilter bzw. Ziel-Filter setzen; „PDF" und „Drucken" für dieselbe Gruppe aufrufen
  und vergleichen; danach Filter zurücksetzen und beide erneut aufrufen.
  Erwartung: mit aktivem Filter zeigen sowohl das PDF als auch der Bildschirmdruck im Kopf den
  Hinweis „Gefilterte Ansicht — dieses Dokument zeigt nur die gefilterten …"; ohne aktiven Filter
  fehlt der Hinweis in beiden vollständig.
  Negativfall: PDF und Bildschirmdruck zeigen unterschiedliche Filter-Hinweise (z. B. trägt nur
  einer der beiden den Banner) → `IsFiltered` wird in einem der beiden Pfade nicht identisch
  berechnet.

- **TS-74.12 — Laufzeit-Log (AK 15).** **(Manual-UAT)**
  Vorbedingung: Zugriff auf die Serilog-Logdatei des Testsystems; Standard-Konfiguration.
  Schritte: ein PDF erzeugen; anschließend die aktuelle Logdatei nach der Erzeugungszeile
  durchsuchen.
  Erwartung: die Logdatei enthält eine Zeile nach dem Muster „PDF erzeugt: {Bytes} Bytes in
  {ElapsedMs} ms (davon {WaitMs} ms Wartezeit auf das Semaphor)" mit plausiblen, nicht-negativen
  Zahlenwerten.
  Negativfall: die Zeile fehlt, oder die Wartezeit auf das Semaphor ist größer als die Gesamtzeit →
  Fehler in der Stopwatch-Messung.

- **TS-74.13 — Singleton-Begrenzung (AK 14).** Automatisiert `PdfRenderServiceDiResolutionTests`
  (Singleton-Registrierung, ein Semaphor über alle Anfragen). Zusätzlich manuell: **(Manual-UAT —
  Änderung an `appsettings.json` und Neustart der Anwendung erfolgen auf dem Testsystem.)**
  Vorbedingung: `PdfRender:MaxConcurrent = 1` gesetzt und Anwendung neu gestartet.
  Schritte: in zwei Browser-Tabs nahezu gleichzeitig je einen PDF-Download auslösen.
  Erwartung: der zweite Download startet sichtbar verzögert (wartet auf das freiwerdende Semaphor)
  und schließt danach ohne Fehlermeldung ab — keine Meldung „zu viele Anfragen" oder ähnliches, nur
  eine längere Wartezeit (im Log als `WaitMs` sichtbar, siehe TS-74.12).
  Negativfall: der zweite Download schlägt fehl, oder beide laufen trotz `MaxConcurrent = 1`
  gleichzeitig (im Task-Manager zwei parallele `msedge.exe`-Prozessgruppen sichtbar) → das Semaphor
  wirkt nicht anfrageübergreifend (Regression Richtung Scoped- statt Singleton-Registrierung).

- **TS-74.14 — Unbekannte HauptFA.** **(Manual-UAT)**
  Vorbedingung: eine HauptFA-Nummer, die in keiner der beiden Listen vorkommt (z. B.
  `999999999`).
  Schritte: `/FaHierarchyKommissionierListen/Pdf/999999999` bzw.
  `/FaHierarchyBeschichtung/Pdf/999999999` direkt aufrufen.
  Erwartung: HTTP 404 (Not Found) — kein leeres PDF, keine Serverfehlerseite, kein stiller Redirect
  auf die Liste.
  Negativfall: die Route liefert stattdessen ein leeres oder generisches PDF oder eine
  500er-Fehlerseite → die `FirstOrDefault`-Prüfung auf die Gruppe fehlt oder greift nicht.

## TS-75 — Matchcode im Artikelstamm (v1.40.0)

**Bezug:** Spec `2026-09-13-matchcode-artikelstamm-spec`.

**Pflicht-Vorbedingungen (für alle Szenarien dieses Kapitels, sofern nicht abweichend angegeben):**
- Ein Artikel mit bekanntem, nicht-leerem `Article.Matchcode` ist bekannt (z. B. aus einem
  bereits gelaufenen Artikel-Sync), ebenso ein Artikel mit `Matchcode = NULL`.
- Ein Haupt-FA mit mehreren Sub-FAs, deren `ArticleNumber` auf den bekannten Matchcode-Artikel
  zeigt, ist in allen fünf FA-Zeilen-Listen (`/ProductionOrders`, `/PickingLeitstand`,
  `/FaCompletion`, `/FaWorklist`, `/Picking`) sichtbar.
- Für TS-75.7 zusätzlich: eine AKE-Instanz bzw. ein AKE-Standort, an dem der Artikel-Sync
  (`Sync:ArticlesEnabled`) noch **keinen** Matchcode geliefert hat.
- Für TS-75.2: Zugriff auf den Windows-Service-Log (`logs/sync/sync-YYYYMMDD.log`) bzw. das
  Aktivitäts-Protokoll, um den Sync-Lauf und die Änderungserkennung zu prüfen.
- Für TS-75.8: Zugriff auf den Web-App-Log (Serilog), um die Log-Zeile aus `MatchcodeLookup`
  (`Matchcode-Join: …`) je Listenaufbau zu prüfen.

**Automatisiert abgedeckt (Auszug):** `ArticleRepositoryTests` (`GetMatchcodesByArticleNumbersAsync`
liefert Dict nur für existierende Artikel/leere Eingabe/leeren vs. fehlenden Matchcode,
`GetByMatchcodeExactAsync` exakt statt Teilstring, Freitextsuche + Spaltenfilter-Case
`"matchcode"` über `Article`), `ArticlesControllerTests` (`Info`-Exakt-Matchcode-Fallback nach
gescheitertem `GetByArticleNumberAsync`, `Edit`-POST bindet Matchcode + Audit),
`ProductionOrderRepositoryTests` (Matchcode-Postfilter statt SQL-Server-Case, Kunde-Filter über
C#-Postfilter unverändert), `MatchcodeLookupTests` (Batch-Dict-Aufbau, fehlende Artikelnummer ⇒
kein Dict-Eintrag ⇒ AK-14-Zählung, vorhandener aber leerer Matchcode ⇒ Dict-Eintrag mit `null`,
genau **ein** Log-Aufruf je `BuildAsync`, `null`/leere Eingabe wirft nicht),
`StockMovementRepositoryTests`, `PartRequisitionRepositoryTests`,
`WarehouseRequisitionRepositoryTests` (Matchcode-Teilstring als zusätzliches ODER-Glied in
Bestand/Bewegungshistorie/Bedarfsmeldungen/Fehlteile-Freitext, leerer Matchcode kein Treffer, kein
Wurf), `FaCompletionControllerTests` (Freitextfilter FA-Fertigmeldung inkl. Matchcode über den
Article-Batch-Lookup), `ArticleSearchMinLengthSourceTests` (Quelltext-Wächter: die drei
Artikel-Suchwidgets nur mit `minimumInputLength: 3` bzw. dem `q.length < 3`-Guard, kein
Artikel-Suchwidget mehr mit `minimumInputLength: 0`; `_Select2ProductionOrderPartial` und
`Picking/Bom.cshtml` bewusst unberührt). Der manuelle Rest ist die Sichtprüfung am laufenden
System (Sage-Sync-Lauf, Log-Zeilen, Bildschirm-Darstellung) — diese Schritte sind unten
ausdrücklich mit **(Manual-UAT)** gekennzeichnet und bilden zusammen die manuelle Abnahme-Liste
für Schranke 2.

- **TS-75.1 — `Article.Matchcode` additiv, Migration idempotent (AK 1, 16).**
  Vorbedingung: `SQL/91_AddArticleMatchcode.sql` noch nicht bzw. bereits einmal gegen eine
  Testdatenbank gelaufen.
  Schritte: `SQL/91_AddArticleMatchcode.sql` **zweimal** hintereinander gegen dieselbe Datenbank
  ausführen **(Manual-UAT: SSMS/sqlcmd)**; Spaltentyp von `Articles.Matchcode` prüfen.
  Erwartung: der zweite Lauf ändert nichts und bricht nicht ab (Guard `IF COL_LENGTH('dbo.Articles',
  'Matchcode') IS NULL`); die Spalte ist `NVARCHAR(200) NULL`; bestehende Artikel-Datensätze und
  ihre übrigen Felder bleiben unverändert (rein additiv, keine Datenmigration nötig).
  Negativfall: ein zweiter Lauf wirft einen Fehler (z. B. „Column already exists") oder verändert
  eine bestehende Spalte → Migration ist nicht idempotent, Regression.

- **TS-75.2 — Artikel-Sync befüllt Matchcode aus Sage inkl. Änderungserkennung und Audit (AK 11, 16).**
  **(Manual-UAT)**
  Vorbedingung: ein Artikel, dessen `KHKArtikel.Matchcode` in Sage sich seit dem letzten Sync
  geändert hat (oder testweise geändert wird), `Sync:ArticlesEnabled = true`.
  Schritte: einen Artikel-Sync-Lauf abwarten bzw. manuell auslösen; `Article.Matchcode`,
  `ModifiedAt`, `ModifiedBy`, `ModifiedByWindows` des Artikels vor/nach vergleichen; Sync-Log
  (`logs/sync/sync-YYYYMMDD.log`) bzw. Aktivitäts-Protokoll auf den Artikel-Sync-Eintrag prüfen.
  Erwartung: `Article.Matchcode` entspricht danach exakt `KHKArtikel.Matchcode` (auf 200 Zeichen
  `CAST`, nicht abgeschnitten bei kürzeren Werten); die Änderungserkennung erkennt eine reine
  Matchcode-Änderung als Update (nicht als „keine Änderung" übersprungen); Audit-Felder sind mit
  dem Sync-Servicenamen und aktuellem Zeitstempel gesetzt.
  Negativfall: ein Artikel, dessen Matchcode sich **nicht** geändert hat, wird trotzdem als Update
  gezählt/mit neuem `ModifiedAt` versehen → die `ISNULL(...) != ISNULL(...)`-Kette vergleicht
  Matchcode fälschlich immer als „unterschiedlich".

- **TS-75.3 — Fünf FA-Listen: Vorher/Nachher-Regression nach Quellwechsel, Spaltenfilter unverändert
  (AK 2, 3).**
  Vorbedingung: der bekannte Sub-FA mit Matchcode-Artikel ist vor dieser Version bereits in allen
  fünf Listen mit demselben Matchcode-Wert sichtbar gewesen (aus `ProductionOrder.Matchcode`,
  v1.38.0); Version 1.40.0 ist deployt.
  Schritte: `/ProductionOrders`, `/PickingLeitstand`, `/FaCompletion`, `/FaWorklist` und `/Picking`
  öffnen **(Manual-UAT: Sichtprüfung je Liste)**; den bekannten Matchcode-Wert je Liste ablesen und
  mit dem Vorzustand vergleichen; anschließend je Liste einen Spaltenfilter bzw. Freitextfilter auf
  „Matchcode" setzen.
  Erwartung: **keine** Zeile zeigt nach dem Quellwechsel (Artikel-Join statt `ProductionOrder`-Feld)
  einen anderen Matchcode-Wert als vorher — Spalte, Position und Sichtbarkeit sind unverändert
  (B-1-Abdeckungsprüfung); der Filter liefert in allen fünf Listen weiterhin korrekte Treffer, in
  `/ProductionOrders` jetzt über den C#-Postfilter (`FaListComputedColumnKeys`) statt einen
  SQL-Server-Filter-Case.
  Negativfall: eine Zeile zeigt nach dem Wechsel `NULL`/leer, obwohl ihr Artikel weiterhin einen
  Matchcode hat (Join-Fehler auf `ArticleNumber`), oder der Spaltenfilter einer der fünf Listen
  liefert keine Treffer mehr → sofort melden, nicht selbst „reparieren".

- **TS-75.4 — Artikelstammliste: Spalte, Spaltenfilter, Freitextsuche über Matchcode (AK 4).**
  Vorbedingung: mindestens zwei Artikel mit unterschiedlichem, bekanntem Matchcode.
  Schritte: `/Articles` öffnen; Spalte „Matchcode" prüfen (Position neben Artikelnummer/
  Bezeichnung) **(Manual-UAT)**; Freitextsuche mit einem Matchcode-Teilstring ausführen;
  Spaltenfilter „Matchcode" separat mit demselben Teilstring setzen.
  Erwartung: die Spalte ist ohne Zahnrad-Eingriff sichtbar; sowohl Freitextsuche als auch
  Spaltenfilter finden den passenden Artikel über den Matchcode-Teilstring, auch wenn
  Artikelnummer/Bezeichnung nicht zutreffen.
  Negativfall: ein Suchbegriff, der weder in Artikelnummer/Bezeichnung noch in irgendeinem
  Matchcode vorkommt → leere Trefferliste, keine Exception, keine 500er-Fehlerseite.

- **TS-75.5 — Artikelinfo zeigt Matchcode und findet Artikel per exaktem Matchcode-Fallback (AK 5).**
  Vorbedingung: ein Artikel mit bekanntem Matchcode, dessen Matchcode **nicht** gleichzeitig
  Teilstring einer anderen Artikelnummer im System ist.
  Schritte: `/Articles/Info?articleNumber=<Artikelnummer>` aufrufen und Matchcode-Anzeige prüfen;
  danach `/Articles/Info?articleNumber=<Matchcode>` aufrufen (bzw. den Matchcode als Barcode/QR
  scannen, sofern das Scan-Feld dieselbe Route bedient) **(Manual-UAT: Scan-Eingabe)**.
  Erwartung: die erste Abfrage zeigt den Artikel inkl. Matchcode-Feld; die zweite Abfrage (exakter
  Matchcode statt Artikelnummer) findet **denselben** Artikel über den `GetByMatchcodeExactAsync`-
  Fallback, der erst nach einem gescheiterten `GetByArticleNumberAsync`-Treffer greift.
  Negativfall: ein **Teilstring** eines Matchcodes (nicht der exakte Wert) als `articleNumber`
  übergeben → **kein** Treffer (weder über Artikelnummer noch über den Matchcode-Fallback, der
  bewusst exakt und nicht `Contains` matcht); die Seite zeigt „nicht gefunden", keine falschen
  Mehrfachtreffer.

- **TS-75.6 — Matchcode-Suche in Bestand, Bewegungshistorie, Fehlteilen/Lager- und Glasbestellung,
  Bedarfsmeldungen und FA-Fertigmeldung-Freitext (AK 6, 7, 8).**
  Vorbedingung: der Matchcode-Artikel hat aktuellen Bestand (für `/StockOverview`), mindestens eine
  Buchung (für `/StockMovements`), eine offene Position in `/MissingParts` bzw.
  `/MissingPartsLager`/`/WarehouseRequisitions`, sowie eine offene Bedarfsmeldung in
  `/PartRequisitions`.
  Schritte: in jeder der genannten Listen die Freitextsuche (bzw. bei `/StockMovements` zusätzlich
  den Spaltenfilter „Artikel") mit dem Matchcode-Teilstring ausführen; in `/FaCompletion` denselben
  Matchcode-Teilstring in den Artikel-Freitextfilter der FA-Fertigmeldung eingeben.
  Erwartung: alle genannten Stellen finden den erwarteten Datensatz über den Matchcode-Teilstring,
  identisch zur bestehenden Suche über Artikelnummer/Bezeichnung — der Matchcode ist ein
  zusätzliches ODER-Glied, kein Ersatz.
  Negativfall: ein Artikel ohne Matchcode (`NULL`) erscheint bei keiner dieser Suchen als
  Falschtreffer (kein `NULL.Contains(...)`-Wurf, keine 500er-Fehlerseite) — siehe TS-75.7 für die
  Vertiefung des Null-Falls.

- **TS-75.7 — Leerer Matchcode kein Fehler; AKE-Regression (AK 9, 10).**
  Vorbedingung a: ein Artikel mit `Matchcode = NULL` im System (überall verfügbar).
  Vorbedingung b (AKE-Regression): ein AKE-Standort bzw. eine AKE-Instanz, an der der Artikel-Sync
  noch keinen einzigen Matchcode geliefert hat (`Article.Matchcode` durchgehend `NULL`).
  Schritte a: den Matchcode-Artikel ohne Matchcode in Artikelstammliste, Bestand,
  Bewegungshistorie, Fehlteilen, Bedarfsmeldungen und den fünf FA-Listen aufrufen/filtern
  **(Manual-UAT: je Stelle kurz gegenprüfen)**.
  Erwartung a: überall bleibt die Matchcode-Spalte leer bzw. wird der Artikel bei einer
  Matchcode-Suche korrekt **nicht** getroffen — nirgends eine Exception, nirgends eine
  unerwartet leere Gesamtliste ohne erkennbaren Grund (F7, null-sicher).
  Schritte b: an der AKE-Instanz dieselben Listen/Filter wie in TS-75.3/TS-75.4 durchgehen
  **(Manual-UAT)**.
  Erwartung b: die Matchcode-Spalte ist an diesem Standort durchgehend leer, bis dort der erste
  Artikel-Sync-Lauf einen Matchcode liefert — **das ist ausdrücklich kein Fehler**; alle übrigen
  Spalten, Filter und Sortierungen bleiben unverändert funktionsfähig.
  Negativfall: eine der Stellen wirft eine Exception oder liefert bei leerem/`NULL`-Matchcode eine
  leere Gesamtliste (statt nur „kein Treffer auf dieser Spalte") → Regression.

- **TS-75.8 — Fehltreffer-Zähler im Log, eine Zahl je Listenaufbau (AK 14).** **(Manual-UAT: Log
  prüfen)**
  Vorbedingung: mindestens eine FA-Zeile in einer der fünf Listen, deren `ArticleNumber` **keinen**
  passenden Datensatz in `Articles` hat (z. B. ein inzwischen aus Sage gelöschter Artikel).
  Schritte: die betroffene Liste (z. B. `/ProductionOrders`) öffnen und damit einen Listenaufbau
  auslösen; im Web-App-Log nach der Zeile `Matchcode-Join: {Missing} von {Total} FA-Zeilen ohne
  Artikelstamm-Treffer ({Liste})` suchen.
  Erwartung: pro Listenaufbau erscheint **genau eine** solche Log-Zeile (nicht eine je betroffener
  Zeile); `{Missing}` entspricht der Anzahl der FA-Zeilen ohne Artikelstamm-Treffer, `{Total}` der
  Gesamtzahl angefragter Artikelnummern, `{Liste}` nennt die aufrufende Liste (z. B.
  „ProductionOrders", „FaCompletion").
  Negativfall: keine Log-Zeile erscheint, obwohl Fehltreffer vorliegen (Meldung fehlt still —
  verstößt gegen „Melden statt still behandeln"), oder es erscheint eine Zeile **je** fehlender
  Zeile statt einer Sammelzahl je Aufbau.

- **TS-75.9 — Artikel-Typeahead erst ab 3 Zeichen; OSEON/BOM bewusst ohne Matchcode (AK 12, 15).**
  Schritte a: in einem Artikel-Auswahlfeld (z. B. Einbuchung, `InboundBulk`, oder
  `WarehouseRequisitions/Edit`) einzeln 1, dann 2, dann 3 Zeichen eingeben und dabei die
  Netzwerk-Anfragen beobachten **(Manual-UAT: Browser-Entwicklertools, Netzwerk-Tab)**.
  Erwartung a: bei 1 und 2 Zeichen wird **keine** Anfrage an `/api/articles/search` ausgelöst; erst
  ab dem dritten Zeichen (nach der bestehenden Entprellung) feuert die Suche; das
  Produktionsauftrags-Auswahlfeld (`_Select2ProductionOrderPartial`) und die BOM-/Auftragssuche in
  `Picking/Bom.cshtml` sind davon **nicht** betroffen und suchen weiterhin ab dem ersten Zeichen.
  Schritte b (Dokumentations-Hinweis, keine Bildschirmprüfung): OSEON-Teileverfolgung sowie die
  BOM-Ansicht (Stückliste) auf eine Matchcode-Spalte/-Suche prüfen.
  Erwartung b: dort ist **bewusst kein** Matchcode zu erwarten — beides ist nicht Teil dieser Spec
  (Backlog-Kandidat für eine spätere Erweiterung); das ist kein Fehlbefund.
  Negativfall a: eine Anfrage feuert bereits bei 1–2 Zeichen (Regression Richtung
  `minimumInputLength: 0`) → bei ca. 100.000 Artikeln ein voller Suchlauf pro Tastendruck.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen (Hauptcheckout, nicht Worktree).

---

## TS-76 — IDEAL: FA-Arbeitsgang-Erkennung aus der Struktur (v1.41.0) — **ERSETZT durch TS-77**

> **Superseded (v1.44.0):** Dieses Kapitel beschreibt die v1.41.0-Fassung, die gegen `FaWorkStep` und
> den `WorkStep`-Katalog zielte. Sie wurde nie deployt und ist durch **TS-77** ersetzt (Ziel jetzt
> `WorkOperation`, Katalog `ProductionWorkplace.ArbeitsschrittCode`). Zur Abnahme von v1.44.0 gilt
> **TS-77**; TS-76 bleibt nur zur Nachvollziehbarkeit der Historie stehen. Der alte Toggle
> `Sync:FaWorkStepStructureDetectionEnabled` und der SyncLog-Dienst `FaWorkStepStructureDetection`
> existieren nicht mehr (umbenannt zu `Sync:WorkOperationStructureDetectionEnabled` /
> `WorkOperationStructureDetection`).

**Bezug:** Spec `2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec`.

**Pflicht-Vorbedingungen (für alle Szenarien dieses Kapitels, sofern nicht abweichend angegeben):**
- Standort IDEAL: Master `ProduktionsauftragHierarchisch` = true, FA-Struktur importiert und
  materialisiert (Sub-FAs als `ProductionOrder`-Zeilen vorhanden).
- Toggle `Sync:FaWorkStepStructureDetectionEnabled` = true (außer TS-76.7/76.8).
- Arbeitsgang-Katalog (`/WorkSteps`): mindestens ein Eintrag mit `Code` = einem real vorkommenden
  Arbeitsschritt-Kürzel (z. B. `KA`), plus für TS-76.4 ein Kürzel **ohne** passenden Katalogeintrag.
- Zugriff auf das Aktivitäts-Protokoll (SyncLog, Dienst `FaWorkStepStructureDetection`) und den
  Windows-Service-Log für die Sammelmeldung.

**Automatisiert abgedeckt:** `FaWorkStepStructureDetectionServiceTests` (14 Fälle, InMemory) — Grundfall
Source=Struktur (AK 3), Audit-Autor `FaWorkStepStructureDetection` (AK 10), Code-Match
case-insensitiv/getrimmt, **DirectChildren-Gegenprobe** Enkel-Token landet beim Elternteil, nicht beim
Großvater (AK 5), Nur-hinzufügen bei `IsRemoved` und bei aktiver Zeile (AK 4), Match exakt über `Code`
nicht `SearchString` (AK 6), unbekanntes Token gemeldet+gezählt nicht angelegt (AK 7/12), abgeschlossener
Auftrag (`IsDone`/`IsDonePicking`) ohne Zeile (AK 9), fehlende Materialisierung ohne Fehler (AK 8),
DryRun zählt ohne Schreiben, SubFA==0-Blatt ist kein eigener Auftrag, S1-Zustand meldet nur bei
Mengenänderung. Der Master-/Toggle-Gate (AK 1/2) sitzt im `SyncWorker`; der echte Datenlauf am befüllten
IDEAL-System ist **(Manual-UAT)**, unten gekennzeichnet.

- **TS-76.1 — Grundfall (AK 3). (Manual-UAT)** Vorbedingung: Katalog enthält `Code = "KA"`; ein
  materialisierter Sub-FA trägt `KA` in `Arbeitsschritte` auf seiner eigenen Zeile.
  Schritte: Sync-Lauf abwarten/auslösen. Erwartung: genau **eine** neue `FaWorkStep`-Zeile mit
  `Source = "Struktur"` für diesen Sub-FA und den `KA`-Arbeitsgang; sichtbar in FA-Arbeitsliste/Leitstand.
- **TS-76.2 — DirectChildren-Gegenprobe (AK 5). (Manual-UAT)** Sub-FA X → direktes Kind Y (selbst
  Sub-FA) → Enkel Z mit Token `SÄ`, das **nur** auf Z steht. Erwartung: `SÄ` erscheint als Arbeitsgang
  bei **Y** (Y-Scope = Y-Zeile + direkte Kinder inkl. Z), **nicht** bei X (X-Scope = X-Zeile + direkte
  Kinder, enthält Y-Zeile, aber nicht Z). *Automatisiert gespiegelt.*
- **TS-76.3 — Nur-hinzufügen / manuell entfernt (AK 4).** Ein `FaWorkStep` mit `IsRemoved = true` für
  (Sub-FA, Arbeitsgang) existiert bereits. Sync-Lauf, Token weiterhin in der Struktur. Erwartung: keine
  neue Zeile, die entfernte Zeile bleibt `IsRemoved = true`. *Automatisiert abgedeckt.*
- **TS-76.4 — Unbekanntes Token: Meldung, kein Insert (AK 7, AK 12). (Manual-UAT für die Mail)**
  Struktur enthält ein Token ohne passenden Katalogeintrag. Zwei aufeinanderfolgende Läufe mit
  identischer unbekannter Token-Menge. Erwartung: kein `FaWorkStep` für dieses Token; SyncLog zeigt die
  Sammelmeldung (Token-Liste + Anzahl betroffener Aufträge) bei **beiden** Läufen; die Sammelmail geht
  (bei `ErrorNotification:Enabled`) nur beim **ersten** raus. Negativfall: ein zusätzliches unbekanntes
  Token im dritten Lauf → erneute Mail. *Insert-Verhinderung + Counts automatisiert; Mail-Gate =
  S1-Zustand automatisiert; realer Mailversand Manual-UAT.*
- **TS-76.5 — Abgeschlossener Auftrag (AK 9).** Sub-FA mit `IsDone = true` (bzw.
  `PickingStatus.IsDonePicking`) und bekanntem Token. Erwartung: keine neue Zeile. *Automatisiert abgedeckt.*
- **TS-76.6 — Fehlende Materialisierung (AK 8).** `FaHierarchyNode`-Sub-FA existiert, aber noch kein
  `ProductionOrder`. Erwartung: kein Fehler, Sub-FA ausgelassen; nach nächster Materialisierung +
  Arbeitsgang-Erkennung erscheint der Arbeitsgang. *Automatisiert abgedeckt.*
- **TS-76.7 — AKE-Flachmodus-Regression (AK 1).** Master `false`. Erwartung: AKE-Heuristik unverändert
  (`FaWorkStepDetectionService` wie bisher), **kein** SyncLog-Eintrag `FaWorkStepStructureDetection`.
  *Gate im SyncWorker; Manual-UAT/Sicht am Protokoll.*
- **TS-76.8 — Zwischenzustand (AK 2).** Master `true`, `Sync:FaWorkStepStructureDetectionEnabled = false`.
  Erwartung: weder AKE-Heuristik (Klasse-D-Gate) noch Struktur-Service erzeugen `FaWorkStep`-Zeilen; kein
  Fehler. *Gate im SyncWorker.*
- **TS-76.9 — Matching exakt statt heuristisch (AK 6).** Ein Arbeitsgang mit `Code = "XY"` und einem
  `SearchString`, der zufällig ein IDEAL-Token als Teilstring enthält. Erwartung: **kein** Treffer über
  den `SearchString` — nur ein Arbeitsgang mit exakt passendem `Code` erzeugt eine Zeile.
  *Automatisiert abgedeckt.*
- **TS-76.10 — Umlaut-Kürzel zeichengleich (H6). (Manual-UAT bei der Katalogpflege)** Ein Token `SÄ`
  braucht einen Katalogeintrag mit `Code = "SÄ"` (nicht `SAE`). Erwartung: nur der zeichengleiche
  `Code` trifft; ein `SAE`-Eintrag lässt `SÄ` weiter „unbekannt" gemeldet (nicht still verschluckt).

**Betriebshinweis (H5):** Der „zuletzt gemeldete Kürzel-Stand" liegt im Speicher (Singleton) — nach jedem
Dienst-Neustart geht im Fenster zwischen Deploy und Katalogpflege die Sammelmail einmal erneut raus, auch
ohne echte Mengenänderung. Bekannt und akzeptiert.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen (Hauptcheckout, nicht Worktree).

---

## TS-77 — IDEAL: Arbeitsgänge (WorkOperation) aus der Struktur + Werkbank-Anlage aus Sage (v1.44.0)

**Bezug:** Spec `2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec` (Epic, Bausteine a/b/c). Ersetzt
TS-76 (v1.41.0) fachlich.

**Pflicht-Vorbedingungen (für alle Szenarien dieses Kapitels, sofern nicht abweichend angegeben):**
- Standort IDEAL: Master `ProduktionsauftragHierarchisch` = true, FA-Struktur importiert und
  materialisiert (Sub-FAs als `ProductionOrder`-Zeilen vorhanden).
- Toggle Werkbank-Anlage `Sync:ProductionWorkplaceSyncEnabled` = true; Toggle Arbeitsgang-Erkennung
  `Sync:WorkOperationStructureDetectionEnabled` = true (außer TS-77.6/77.15).
- Standorteinstellungen (unter *Werkbänke (Sage-Arbeitsplätze)*): `Sync:ProductionWorkplaceSyncMandant`
  gesetzt (i. d. R. 1), `Sync:ProductionWorkplaceSyncAusschlussliste` z. B. `STO,XXX,x01,BS,BS2,FRE,AKE`.
- Zugriff auf das Aktivitäts-Protokoll (SyncLog, Dienste `ProductionWorkplaceSync` und
  `WorkOperationStructureDetection`) und den Windows-Service-Log für die Sammelmeldungen.
- **DryRun vor dem ersten scharfen Lauf** (Baustein a legt bei IDEAL ~60 Werkbänke neu an — Anzahl
  vorher im DryRun-Log prüfen).

**Automatisiert abgedeckt (InMemory):**
- `ProductionWorkplaceSyncServiceTests` (Baustein a, 6 Fälle) — Neuanlage mit Audit `ProductionWorkplaceSync`
  (AK 1), Wiederfinden bei getrimmter Nummer/Kürzel (AK 21-Teil), Abweichung überschrieben + gemeldet
  (AK 2), keine Abweichung → kein Schreiben (AK 3), Kürzel leer → übersprungen (AK 6-Teil), DryRun zählt
  ohne Schreiben.
- `WorkOperationStructureDetectionServiceTests` (Baustein b, 13 Fälle) — Grundfall `WorkOperation` mit
  `OperationNumber`=Kürzel/`ProductionWorkplaceId` (AK 8), Audit-Autor `WorkOperationStructureDetection`,
  Match case-insensitiv/getrimmt, DirectChildren-Gegenprobe (AK 13), Nur-hinzufügen bei bestehender
  `(ProductionOrderId, OperationNumber)` (AK 12), unbekanntes Kürzel gemeldet+gezählt nicht angelegt
  (AK 9), **Kürzel-Mehrdeutigkeit** gemeldet nicht angelegt (AK 11), Fertig-/Storno-Filter (AK 14),
  fehlende Materialisierung ohne Fehler, DryRun, SubFA==0-Blatt, S1-Zustand.
- `BdeDefaultWorkOperationServiceTests` — Existenz-Check über `OperationNumber = "01"` statt Name: echter
  AG namensgleich zum Default wird **nicht** bebucht (AK 16).

**Nicht InMemory-testbar → Manual-UAT:** Sage-Read (`KHKPpsArbeitsplaetze`), Ausschlusslisten-Wirkung
(Liste liegt in ServiceSettings-DB), eindeutiger Index (InMemory erzwingt UNIQUE nicht), Master-/
Toggle-Gate (SyncWorker), Terminal-Anzeige. Die folgenden Szenarien am befüllten IDEAL-System:

- **TS-77.1 — Baustein a Neuanlage (AK 1). (Manual-UAT)** Sage-Arbeitsplatz ohne WMS-Gegenstück.
  Sync-Lauf. Erwartung: neue `ProductionWorkplace`-Zeile mit Sage-Nr./Name/Kürzel, `BdeAktiv = false`,
  Audit `ProductionWorkplaceSync`. *Kernlogik automatisiert.*
- **TS-77.2 — Baustein a Abweichung (AK 2). (Manual-UAT für die Mail)** Bestehende Zeile mit
  abweichendem Kürzel/Name. Erwartung: überschrieben + Sammelmeldung alt/neu. *Automatisiert (ohne Mail).*
- **TS-77.3 — Baustein a Ausschlussliste (AK 4). (Manual-UAT)** Arbeitsplatz mit Kürzel `STO`.
  Erwartung: keine Zeile angelegt, kein SyncLog-Eintrag für diesen Arbeitsplatz.
- **TS-77.4 — Baustein a Trim (AK 5). (Manual-UAT)** Kürzel `"EG "` (Leerzeichen). Erwartung:
  `ArbeitsschrittCode = "EG"`.
- **TS-77.5 — Baustein a Pflicht-Filter (AK 6). (Manual-UAT)** Falscher Mandant / `Aktiv = 0` / leeres
  Kürzel. Erwartung: übersprungen (kein Insert/Update/Meldung).
- **TS-77.6 — Baustein a Master-Gate (AK 7). (Manual-UAT)** Master `false`. Erwartung: kein Lauf, kein
  SyncLog-Eintrag `ProductionWorkplaceSync`.
- **TS-77.7 — Baustein b Grundfall (AK 8). (Manual-UAT)** Sub-FA mit Token `KA`, Werkbank mit
  `ArbeitsschrittCode = "KA"`. Erwartung: genau eine `WorkOperation` (`OperationNumber = "KA"`, korrekter
  `ProductionWorkplaceId`). *Automatisiert gespiegelt.*
- **TS-77.8 — Baustein b unbekanntes Kürzel (AK 9). (Manual-UAT für die Mail)** Token ohne Werkbank,
  nicht ausgeschlossen. Erwartung: keine `WorkOperation`, Sammelmeldung, keine Exception. *Insert/Counts
  automatisiert.*
- **TS-77.9 — Baustein b ausgeschlossenes Kürzel (AK 10). (Manual-UAT)** Token auf der Ausschlussliste
  (z. B. `BS`). Erwartung: keine `WorkOperation`, **keine** Unbekannt-Meldung. *(Nur Manual-UAT — die
  Ausschlussliste liegt in der ServiceSettings-DB, nicht InMemory injizierbar.)*
- **TS-77.10 — Baustein b Kürzel-Mehrdeutigkeit (AK 11).** Zwei aktive Werkbänke mit identischem
  `ArbeitsschrittCode`. Erwartung: keine `WorkOperation`, Meldung „mehrdeutig". *Automatisiert abgedeckt.*
- **TS-77.11 — Baustein b DirectChildren-Gegenprobe (AK 13).** Wie TS-76.2, Zielentität `WorkOperation`.
  *Automatisiert abgedeckt.* Seit v1.47.4 zählen nur direkte **Blatt**-Kinder zum Scope des Vaters
  (siehe TS-77.18).
- **TS-77.12 — Baustein c Terminal zeigt echten AG (AK 15). (Manual-UAT)** An einer `BdeAktiv`-Werkbank
  mit passendem Kürzel: gescannter Sub-FA zeigt im Normal-Modus den struktur-abgeleiteten Arbeitsgang,
  ohne Default-AG-Auswahl. **Nachweis, dass `BdeScanResolver.cs` NICHT geändert wurde:** Diff der Etappe
  zeigt keine Änderung an dieser Datei.
- **TS-77.13 — Baustein c Existenz-Check-Fix (AK 16).** `BdeDefaultArbeitsgang` der Werkbank identisch
  mit dem Namen einer echten `WorkOperation`. NurFA-Buchung. Erwartung: eigene Zeile `OperationNumber =
  "01"` wird verwendet, **nicht** die echte `"KA"`-Zeile. *Automatisiert abgedeckt.*
- **TS-77.14 — Regressionslauf TS-66. (Manual-UAT)** Bestehende BDE-Disambiguierung (TS-66) läuft mit
  echten, struktur-abgeleiteten `WorkOperation`s unverändert durch (mehrere Sub-FAs derselben
  `OrderNumber`, Auswahlliste, NurFA-Fix).
- **TS-77.15 — AKE-Regression (AK 17). (Manual-UAT)** Master `false`: `FaWorkStepDetectionService`
  unverändert aktiv, kein Lauf der neuen/umbenannten Services (`ProductionWorkplaceSync`,
  `WorkOperationStructureDetection`).
- **TS-77.16 — Doppelanlage (AK 20). (Manual-UAT)** Werkbank mit `SageArbeitsplatznummer = "2300"`
  vorhanden; zweite Zeile mit derselben Nummer provozieren. Erwartung: eindeutiger Index verhindert die
  zweite Zeile, Meldung im SyncLog, **alle übrigen Arbeitsplätze werden im selben Lauf trotzdem
  verarbeitet** (kein Abbruch). *(Nur Manual-UAT — InMemory erzwingt UNIQUE-Indizes nicht.)*
- **TS-77.17 — Schlüssel-Behandlung (AK 21). (Manual-UAT)** Sage liefert `"0000"` und `" 2300 "`.
  Erwartung: `"0000"` bleibt `"0000"`; `" 2300 "` findet die bestehende Werkbank `"2300"`, keine
  Neuanlage. *Trim-Teil automatisiert.*
- **TS-77.18 — Kürzel einer Sub-FA-Zeile nur am eigenen Sub-FA (v1.47.4, Freigabe-Nachtrag 2026-10-07).**
  Struktur wie im Sage-Screenshot: Korpus (eigener Sub-FA, `PG`) → Mittelholmblende (eigener Sub-FA, `VM`)
  und Blech-Blatt ohne eigenen FA (`LS KA`). Erkennung laufen lassen. Erwartung: Korpus hat `PG`, `LS`, `KA`
  — **kein** `VM`; Mittelholmblende hat `VM`. Am BDE-Terminal (Werkbank mit Kürzel `VM`) erscheint der
  Arbeitsgang nur **einmal** (Mittelholmblende), nicht zusätzlich mit gleicher Beschriftung am Korpus.
  *Automatisiert:* `Detect_ChildSubFaToken_NotOnParent`, `Detect_LeafChildToken_StillOnParent`,
  `Detect_GrandchildToken_LandsOnParentNotGrandparent` (nachgezogen). *Terminal-Teil Manual-UAT.*
  **Negativfall:** Blatt-Kürzel (`LS`, `KA`) fehlen am Korpus → Fehler (darf nicht passieren).
- **TS-77.19 — Altbestand bereinigen (v1.47.4, Einmal-Skript, Manual-UAT/Deploy).** Nach Deploy des
  Windows-Service v1.47.4: `SQL/Einmalig/2026-10-07_v1.47.4_RemoveParentWorkOperationsFromChildSubFaTokens.sql`
  **genau einmal** auf der IDEAL-DB. Zählung (Kandidaten / zu löschen / nur melden) und Meldeliste ins
  Deploy-Protokoll. Erwartung: Vater-Arbeitsgänge, deren Kürzel nur von Kind-Sub-FA-Zeilen stammt und die
  **keine** BDE-Buchung bzw. Tracking-Rückmeldung haben, sind weg; gebuchte stehen in der Meldeliste und
  bleiben; Arbeitsgänge mit Kürzel an der eigenen Zeile oder an einem Blatt-Kind bleiben unverändert; der
  nächste Erkennungslauf legt die gelöschten **nicht** wieder an. **Negativfall:** Skript vor dem
  Service-Deploy ausgeführt → der alte Lauf legt sie wieder an (deshalb Reihenfolge einhalten).

**Betriebshinweis:** Der „zuletzt gemeldete Kürzel-Stand" (unbekannte Token) und der problematische
Werkbank-Stand liegen im Speicher (Singletons) — nach jedem Dienst-Neustart geht die jeweilige
Sammelmail einmal erneut raus, auch ohne echte Mengenänderung. Bekannt und akzeptiert. Sage-geführte
Werkbank-Felder (Sage-Nr./Name/Kürzel) überleben eine manuelle Änderung nicht (nächster Sync
überschreibt); eine Änderung der Ausschlussliste entfernt bestehende Werkbänke nicht rückwirkend.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen (Hauptcheckout, nicht Worktree).

## TS-78 — FA-Struktur: erkannte Arbeitsgänge je (Sub-)FA im Modal (v1.45.0)

**Bezug:** Spec `2026-09-23-fa-struktur-arbeitsgaenge-anzeige-spec`. Reine Leseansicht, keine Migration.

**Vorbedingungen:** v1.44.0-Stand (TS-77) mit mindestens einem Lauf der Struktur-Erkennung
(`WorkOperation`-Zeilen an materialisierten Sub-FAs vorhanden). Benutzer mit Zugriff auf
`/FaHierarchy` (Kommissionierung, Tracking oder Leitstand). Browser-Entwicklerwerkzeuge (Netzwerk-Reiter)
für TS-78.4; für die Query-Zählung optional SQL-Profiler/Extended Events auf der WMS-DB.

**Automatisiert abgedeckt:**
- `WorkOperationRepositoryFaStructureTests` (InMemory) — Existenz-Check liefert nur angefragte HauptFAs
  mit Arbeitsgang, leere Eingabe → leer; Modal-Abfrage (`GetByOrderNumberWithWorkplaceAsync`, derselbe
  Schlüssel `OrderNumber == HauptFA` wie der Existenz-Check) lädt Werkbank (Sage-Nr.) und sortiert
  Sub-FA → Sequence. **AK 10:** `OrderNumbersWithWorkOperationsQuery_TranslatesContainsAsSingleParameter`
  übersetzt 5000 HauptFA-Nummern gegen den echten SqlServer-Provider (`ToQueryString`) in **einen**
  Parameter `@set nvarchar(max)` + `OPENJSON` — keine 2100-Parameter-Grenze bei „Alle".
- `FaWorkOperationsViewModelTests` — alle materialisierten Knoten gelistet (Blätter `SubFA == 0` nicht),
  Wurzel ohne AG mit leerer Liste, Zähler „Sub-FAs"/„ohne Arbeitsgang", gleichlautende Sub-FA-Nummer
  einer fremden Struktur wird nicht zugeordnet, Arbeitsgänge einer Sub-FA ohne Baum-Knoten landen in
  `UnmatchedOperations` (Warnung statt still verworfen).

**Manual-UAT:**

- **TS-78.1 — Modal zeigt konsolidierte Liste (AK 1–3, 9, 12).** `/FaHierarchy` öffnen, Struktur mit
  bekannten Arbeitsgängen (z. B. `KA`/`SW`) suchen, Knopf **Arbeitsgänge** am Ende der Kopfzeile klicken.
  Erwartung: Modal mit Titel „FA `<HauptFA>`"; oben „N Sub-FAs" + gelbes Badge „M ohne Arbeitsgang"
  (bei M = 0 stattdessen „· alle mit Arbeitsgang"); erste Zeile(n) „HauptFA `<n>`" (Wurzel, eigene AGs
  oder „— kein Arbeitsgang"), Trennlinie, danach **jede** Sub-FA mit Artnr./Bezeichnung und je AG
  Kürzel, Werkbank, Sage-Nr. Sub-FAs ohne AG grau mit „— kein Arbeitsgang". N und M per Abzählen
  bzw. gegen das SyncLog `WorkOperationStructureDetection` desselben Laufs prüfen.
- **TS-78.2 — Read-only + Schließen (AK 5).** Im Modal nur „Schließen" und X, keine Eingabefelder.
  Schließen per X, „Schließen", Escape und Klick neben das Fenster: keine Datenänderung, Filter und
  Seite bleiben. Der Klick auf den Knopf klappt die Struktur **nicht** auf/zu.
- **TS-78.3 — Kein Knopf ohne Arbeitsgänge (AK 7, 8).** (a) Struktur nur mit Blatt-/Materialzeilen,
  (b) Struktur mit materialisierten Sub-FAs, aber ohne erkannten Arbeitsgang (unbekannte oder
  ausgeschlossene Kürzel). Erwartung beide: kein Knopf, übrige Kopfzeilen-Badges stehen unverändert.
- **TS-78.4 — Zweistufiges Laden (AK 4).** Seite mit `pageSize=50` öffnen; Seitenquelltext enthält
  **keine** Kürzel/Sub-FA-Listen des Modals (nur die leere Modal-Hülle `#faWorkOperationsModal`).
  Modal öffnen: im Netzwerk-Reiter genau **ein** Aufruf `/FaHierarchy/WorkOperations?hauptFa=…`.
  Optional Profiler: Seitenaufbau enthält eine `SELECT DISTINCT … OPENJSON`-Abfrage auf
  `WorkOperations`; das Öffnen liest `FaHierarchyNodes WHERE HauptFA = @p` (nicht die ganze Tabelle;
  innerhalb von 5 min ggf. gar nicht, Cache) plus eine `WorkOperations`-Abfrage.
- **TS-78.5 — Große Seite (AK 10).** Seitengröße „Alle". Erwartung: Seite lädt ohne spürbaren
  Zusatz-Verzug, kein SQL-Fehler; ein beliebiges Modal öffnet korrekt.
- **TS-78.6 — Fehler beim Laden (AK 11).** Modal öffnen, dabei im Netzwerk-Reiter „Offline" setzen
  (oder `/FaHierarchy/WorkOperations?hauptFa=1` direkt aufrufen → 404). Erwartung Modal: erst
  „Arbeitsgänge werden geladen …", dann rote Meldung „Arbeitsgänge konnten nicht geladen werden …" —
  **nie** ein leeres Modal.
- **TS-78.7 — Zugriff (AK 6, Negativfall).** Benutzer ohne Kommissionierung/Tracking/Leitstand:
  `/FaHierarchy` und `/FaHierarchy/WorkOperations?hauptFa=<n>` → Zugriff verweigert (wie bisher).
- **TS-78.8 — Tastatur/Terminal.** Per Tab auf den Knopf, Enter öffnet das Modal, Fokus liegt im
  Modal, Escape schließt, Fokus kehrt zum Knopf zurück. Kontrast der grauen „kein Arbeitsgang"-Zeilen
  am Fertigungsterminal lesbar.
- **TS-78.9 — Arbeitsgänge ohne Baum-Knoten (Review-Befund, Negativfall).** HauptFA, deren Sub-FA mit
  Arbeitsgang aus dem Struktur-Import entfallen ist (`ProductionOrder.SageMissingSince` gesetzt) oder
  deren Struktur als „Fehlerhaft" (Tiefen-Cap/Zyklus) markiert ist. Erwartung: Knopf sichtbar; im Modal
  unten gelber Hinweis „N Sub-FA(s) mit Arbeitsgängen stehen nicht (mehr) in der Struktur" mit Sub-FA-
  Nummer und Kürzel — **nie** Knopf da und Modal zeigt nur Striche ohne Erklärung.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen (Hauptcheckout, nicht Worktree).

## TS-79 — IDEAL: Kommissionierung nur am HauptFA — „Alle Ziele“, Rückbau Freigabe-Kaskade (v1.46.0)

**Bezug:** Spec `2026-09-25-kommissionierung-nur-hauptfa-spec`.

**Pflicht-Vorbedingungen (für alle Szenarien dieses Kapitels, sofern nicht abweichend angegeben):**
- Master **`ProduktionsauftragHierarchisch = true`** (IDEAL) — außer bei den AKE-Regressionsfällen
  (TS-79.9, TS-79.14), dort `false`.
- Ein HauptFA mit mindestens einer Sub-FA ist bekannt; für die Stücklisten-Szenarien eine
  hierarchische Stückliste mit mindestens 2 unterschiedlichen Komm.-Ziel-Werten plus mindestens
  einer leeren Zelle.
- Für die Freigabe-Szenarien: Leitstand aktiv (`LeitstandAktiv`), Benutzer mit
  `CanManagePickingRelease`.

**Automatisiert abgedeckt (Auszug):** `ProductionOrderHauptFaTests` (`IsHauptFa`/`IsSubFa` für `""`,
`Sub==Order`, `Sub!=Order`), `ProductionOrderPickingStatusRepositoryTests`
(`Queue_ContainsOnlyHauptFa_EvenIfSubFaIsReleased`, `SetReleaseBatch_SkipsSubFa_InBothDirections`),
`PickingControllerTests` (`Bom_SubFa_RedirectsToIndexWithWarning`,
`Bom_ViewModel_CarriesSubOrderNumber`, `Bom_Hierarchical_DoesNotMultiplyQuantity_AndFlagsHierarchical`),
`PickingLeitstandControllerTests` (`ToggleRelease_SubFa_NoChange_InBothDirections`,
`BulkRelease_ReportsSkippedSubFa`), `ReadOnlyBomBuilderTests` (explizite Sub-FA-Fixture,
`FullStructure=false`), `KommissionierzielFilterWertTests` (9 Normalisierungsfälle),
`AccountControllerTests`/`UsersControllerTests` (`NormalizesKommissionierziel_*`). Der manuelle Rest
ist die Sichtprüfung am laufenden IDEAL-Testsystem (Select2-Verhalten, Badges, Druckausgabe) — diese
Schritte sind unten mit **(Manual-UAT)** gekennzeichnet.

- **TS-79.1 — Relevanzregel unverändert nach Extraktion (AK 1, 2).**
  Vorbedingung: Kommissionierliste mit mehreren FAs, davon einige mit leerem `Kommissionieren`-Feld.
  Schritte: Kommissionierliste vor und nach dieser Änderung (Regressionsvergleich, gestützt auf die
  bestehenden Unit-Tests der Kommissionierliste) vergleichen.
  Erwartung: identische Positionsmenge wie vor der Spec — `KommissionierRelevanzFilter.IsRelevant`
  ist reine Extraktion, keine Verhaltensänderung; die Methode hat weiterhin genau einen Verbraucher.
  Negativfall: keiner (reine Regression).

- **TS-79.2 — „Alle Ziele“ filtert auf gesetztes Komm.-Ziel (AK 3, 5, 6, 8).**
  Vorbedingung: hierarchische Stückliste (`Model.Hierarchical == true`) mit mind. 2 unterschiedlichen
  Komm.-Ziel-Werten und mind. einer leeren Zelle.
  Schritte: `/Picking/Bom/{HauptFA-id}` öffnen, Komm.-Ziel-Dropdown öffnen **(Manual-UAT)**.
  Erwartung: erste Option ist „Alle Ziele (nur kommissionier-relevant)“, danach die tatsächlichen
  Zielwerte alphabetisch; Auswahl blendet alle Zeilen mit leerem Komm.-Ziel aus; Badge zeigt
  „Gefiltert: Alle Ziele (nur kommissionier-relevant)“ statt einer Werteliste; „Alle Filter
  zurücksetzen“ und der Badge-Reset heben den Filter auf.
  Eine Baugruppen-Zeile ohne eigenes Komm.-Ziel wird dabei **immer** ausgeblendet, auch wenn ihre
  Kinder sichtbar bleiben — **das ist erwartetes Verhalten, kein Fehler** (die Kinder stehen dann
  über das bestehende Vorfahren-Aufklappen ohne Elternzeile als Liste der zu holenden Teile).
  Negativfall a: bei aktiver Einzelziel-Auswahl zusätzlich „Alle Ziele“ wählen → das Einzelziel wird
  automatisch abgewählt, nur „Alle Ziele“ bleibt aktiv — es entsteht nie ein kombinierter Wert wie
  `!(leer),KA-02`.
  Negativfall b: bei aktiver „Alle Ziele“-Auswahl ein Einzelziel zusätzlich wählen → „Alle Ziele“
  wird automatisch abgewählt.

- **TS-79.3 — Druck-Leerzustand statt leerem/irreführendem Ausdruck (AK 9).**
  Vorbedingung: Stückliste ohne ein einziges gesetztes Komm.-Ziel.
  Schritte: „Alle Ziele“ wählen (keine Zeile bleibt sichtbar) **(Manual-UAT)**; auf „Stückliste
  drucken“ klicken.
  Erwartung: **kein** neuer Tab öffnet sich; stattdessen erscheint die gelbe Box mit „Keine
  sichtbaren Positionen — nichts zu drucken.“ und Reset-Möglichkeit. Derselbe Leerzustand erscheint
  bei jedem anderen Spaltenfilter ohne Treffer, auch im AKE-Flachmodus (Markup ist modusunabhängig
  vorhanden, auch wenn dort keine Option „Alle Ziele“ existiert).
  Negativfall (Gegenprobe mit Treffern): bei aktiver „Alle Ziele“-Auswahl **mit** Treffern auf
  „Stückliste drucken“ klicken → Druck-Tab öffnet sich, Kopf zeigt „Komm.-Ziel=Alle Ziele (nur
  kommissionier-relevant)“ statt des Rohwerts `!(leer)`, enthält nur die beim Klick sichtbar
  gewesenen Positionen.
  **Hinweis:** Der Druck-Guard zählt nur sichtbare Zeilen, die eine Position (`data-position`)
  tragen. Eine Stückliste, deren sichtbare Zeilen keine Position haben, zeigt deshalb ebenfalls
  „Keine sichtbaren Positionen — nichts zu drucken.“ — auch im AKE-Flachmodus, unabhängig vom
  Komm.-Ziel-Filter. In der Praxis sind Sage-Positionen immer gesetzt; zeigt ein Tester diese
  Meldung trotz sichtbarer Zeilen, zuerst die Positions-Spalte auf leere Werte prüfen, bevor ein
  Fehler gemeldet wird.

- **TS-79.4 — Neues Ziel bleibt sichtbar, kein Momentaufnahme-Effekt (AK 7).**
  Vorbedingung: „Alle Ziele“ aktiv in einer offenen Stückliste.
  Schritte: ein neues, zuvor nicht vorhandenes Kommissionierziel (z. B. per Sage-Sync) an einer
  Position hinzufügen, ohne den Filter neu zu setzen.
  Erwartung: die Position bleibt bzw. erscheint sichtbar — „Alle Ziele“ ist ein Bedeutungswert
  („Zelle nicht leer“), keine gespeicherte Werteliste der zum Auswahlzeitpunkt existierenden Ziele.
  Negativfall: keiner (positive Eigenschaft).

- **TS-79.5 — Persistenz „Alle Ziele“ + Normalisierung beim Speichern (AK 10, 11).**
  Vorbedingung: Profil-Seite bzw. Benutzerverwaltung (Create und Edit).
  Schritte a: `!(leer)` als `DefaultFilterBomKommissionierziel` im Profil speichern; Stückliste
  öffnen.
  Erwartung a: „Alle Ziele“ ist vorgewählt (Dropdown-Anzeige + Badge), keine stillschweigend
  verworfene Vorbelegung.
  Schritte b: `!(leer),KA-02` im Profil **und** in der Benutzerverwaltung (Create **und** Edit)
  speichern.
  Erwartung b: gespeichert wird exakt `!(leer)`; eine `WarningMessage` „Alle Ziele schließt
  Einzelziele aus — gespeichert wurde nur 'Alle Ziele'.“ erscheint; die Stückliste öffnet danach mit
  „Alle Ziele“ vorgewählt und zeigt alle Positionen mit gesetztem Ziel.
  Negativfall (Schreibweise ohne Hinweis, Freigabe-Nachtrag): `!(LEER)` im Profil speichern →
  gespeichert wird `!(leer)`, **kein** Hinweis (reine Schreibweisen-Korrektur ändert die Bedeutung
  nicht), Stückliste öffnet mit „Alle Ziele“ vorgewählt.

- **TS-79.6 — Persistenz-Reload (AK 10, Regressionshinweis; Befund B-ABN-1 behoben in v1.47.2).**
  Vorbedingung: Hierarchische Stückliste einer HauptFA mit Komm.-Zielen; Testbenutzer **ohne**
  gespeicherten Standard-Filter Kommissionierziel (Profil-Feld leer), sofern nicht anders angegeben.
  Schritte und Erwartung:
  a) „Alle Ziele“ wählen, Zeilenzahl notieren, Seite per F5 neu laden → Dropdown zeigt weiter „Alle
     Ziele“, Badge sichtbar, Zeilenzahl wie vorher, Treffer-Äste aufgeklappt (nicht der zugeklappte Baum).
  b) Ein **Einzelziel** (z. B. `KOM`) wählen, F5 → Dropdown zeigt dieses Ziel, Badge „Gefiltert: Komm.-Ziel =
     KOM“, gleiche Zeilen wie vorher.
  c) Mit gespeichertem Profil-Standard (z. B. `!(leer)`): Stückliste öffnen (zeigt „Alle Ziele“), auf ein
     Einzelziel umstellen, F5 → das **Einzelziel bleibt** (die Auswahl der Sitzung gewinnt vor dem Profil).
     Neuer Browser-Tab bzw. neue Sitzung → wieder der Profil-Standard.
  Ein kurzes, ungefiltertes Zwischenbild beim Laden ist möglich und besteht bereits heute für **jeden**
  Bom-Filter (Timing zwischen `table-filter.js` und `bomMatchesFilter`) — **kein Fehler dieser Spec**.
  Negativfall: Auswahl über das Badge-Kreuz bzw. das Lösch-Kreuz im Dropdown leeren, F5 → kein Filter
  (bzw. mit Profil-Standard: wieder dessen Wert). „Alle Filter zurücksetzen“ im gelben Hinweis, F5 → die
  Filter kommen **nicht** zurück. Automatisierter Wächter: `BomKommissionierzielPersistenzTests`.

- **TS-79.7 — Negativfall leeres Dropdown (AK 5, Regressionsschutz).**
  Vorbedingung: `!(leer)` als Standard im Profil gespeichert.
  Schritte: eine Stückliste ohne ein einziges gesetztes Komm.-Ziel öffnen.
  Erwartung: Select2-Dropdown ist gesperrt (keine Werte), zeigt aber „Alle Ziele“ vorgewählt; der
  Leerzustand-Hinweis ist sichtbar; Zurücksetzen funktioniert über Badge oder über den Reset-Knopf im
  Leerzustand.
  Negativfall: keiner (Absicherung eines Randfalls).

- **TS-79.8 — Legacy-Wert case-insensitiv erkannt (AK 10, H8).**
  Vorbedingung: gespeicherter bzw. aus `sessionStorage` wiederhergestellter Wert `!(LEER)` oder
  `!(LEER),KA-02` liegt bereits vor.
  Schritte: Stückliste mit diesem Wert öffnen.
  Erwartung: Dropdown zeigt exklusiv „Alle Ziele“ markiert, kein zusätzliches Einzelziel-Chip — die
  Vorbelegung wird case-insensitiv auf den exakten Options-Wert `!(leer)` abgebildet, statt still
  verworfen zu werden.
  Negativfall: keiner.

- **TS-79.9 — Negativfall AKE, Stückliste (AK 4).**
  Vorbedingung: Master `false` bzw. AKE-Instanz, Flachmodus.
  Schritte: Stückliste öffnen.
  Erwartung: kein Komm.-Ziel-Dropdown vorhanden (Spalte existiert im Flachmodus nicht), also auch
  keine Option „Alle Ziele“ — unverändert zum bisherigen Verhalten.
  Negativfall: keiner (Regressionsnachweis).

- **TS-79.10 — Rückbau Freigabe-Kaskade (AK 12, 13).**
  Vorbedingung: keine besondere.
  Schritte: `/PickingLeitstand` öffnen **(Manual-UAT: Button-Sichtbarkeit)**; `CascadeReleasePreview`/
  `CascadeRelease`-Routen direkt aufrufen (z. B. per Browser-Devtools).
  Erwartung: kein „Alle Sub-FAs freigeben“-Button mehr in der Gruppen-Kopfzeile; die Routen liefern
  404. `grep -rEn "CascadeRelease|SetReleaseForOrderNumber|CountReleasedByOrderNumber" IdealAkeWms IdealAkeWms.Tests`
  liefert 0 Treffer.
  Negativfall (Gegenprobe): die Fertigmeldungs-Kaskade „Alle Sub-FAs fertigmelden“
  (`CascadeDonePreview`/`CascadeDone`) funktioniert unverändert — sie betrifft `IsDoneBde`, nicht
  `IsReleasedForPicking`, und ist von diesem Rückbau **nicht** betroffen.

- **TS-79.11 — Freigabe/Picking nur am HauptFA (AK 14).**
  Vorbedingung: hierarchische Gruppe mit mehreren Sub-FAs, im Leitstand **und** in der FA-Liste.
  Schritte: Gruppe aufklappen **(Manual-UAT)**.
  Erwartung: nur die HauptFA-Zeile zeigt Freigabe-Button/-Formular, Prioritäts-Input, Bulk-Checkbox
  und den interaktiven Stückliste-Link (`Picking/Bom`); Sub-FA-Zeilen zeigen keines davon. Ein
  Anwender mit Vorbau-Zugriff sieht auf der Sub-FA-Zeile in der FA-Liste stattdessen weiterhin den
  read-only `FaWorklist/Bom`-Link; ohne Vorbau-Zugriff bleibt die Zelle dort leer.
  Negativfall: eines der genannten Bedienelemente ist an einer Sub-FA-Zeile sichtbar oder bedienbar
  → Regression, sofort melden.

- **TS-79.12 — Negativfall direkter Zugriff auf Sub-FA (AK 15, 16).**
  Vorbedingung: bekannte Sub-FA-Id.
  Schritte a: `GET /Picking/Bom/{sub-fa-id}` direkt aufrufen.
  Erwartung a: **keine** Stückliste, sondern eine Weiterleitung auf `/Picking` mit sichtbarer
  `WarningMessage` „Kommissionierung erfolgt nur am HauptFA {OrderNumber}.“.
  Schritte b: `POST /PickingLeitstand/ToggleRelease` mit der Sub-FA-Id auslösen — einmal an einer
  noch nicht freigegebenen, einmal an einer bereits freigegebenen Zeile.
  Erwartung b: in **beiden** Fällen keine Zustandsänderung, sondern eine sichtbare `WarningMessage`
  (Sperre wirkt in beide Richtungen).
  Negativfall: derselbe Aufruf auf die zugehörige HauptFA-Id → funktioniert unverändert.

- **TS-79.13 — BulkRelease gemischt, beide Richtungen (AK 17).**
  Vorbedingung: eine Auswahl aus HauptFA- und Sub-FA-Ids derselben oder unterschiedlicher Gruppen.
  Schritte a: gemischte Auswahl gemeinsam freigeben.
  Erwartung a: nur die HauptFA-Ids werden freigegeben; die Sub-FA-Ids werden übersprungen und
  sichtbar gemeldet, Text „Übersprungen (Sub-FA — Kommissionierung nur am HauptFA): <nummern>“ —
  fällt in derselben Aktion zusätzlich ein `SkippedNoArticle`-Hinweis an, erscheinen beide Texte in
  **einer** `WarningMessage`, durch „ · “ getrennt.
  Schritte b (Gegenprobe): eine bereits freigegebene Sub-FA gemeinsam mit HauptFA-Ids
  „zurücknehmen“ auswählen.
  Erwartung b: die Sub-FA wird ebenfalls übersprungen und gemeldet (keine Rücknahme, keine stille
  Verarbeitung).
  Negativfall: eine Sub-FA-Id wird in der gemischten Auswahl trotzdem verarbeitet → Regression.

- **TS-79.14 — Negativfall AKE, Freigabe (AK 18).**
  Vorbedingung: Master `false` (AKE), eine normale FA-Id (jede Zeile ist dort ihre eigene
  HauptFA-Zeile per Invariante).
  Schritte: `BulkRelease`/`ToggleRelease` auf diese FA-Id auslösen.
  Erwartung: funktioniert unverändert — AK 14-17 zeigen bei AKE keine Wirkung.
  Negativfall: keiner (Regressionsnachweis).

- **TS-79.15 — `IsHauptFa`/`IsSubFa`, Warteschlange strukturell gefiltert (AK 21, automatisiert).**
  Vorbedingung: vier Fixtures — `SubOrderNumber == ""`, `Sub == Order`, `Sub != Order`, jeweils mit
  `IsReleasedForPicking = true`.
  Schritte: die vier Warteschlangen-Abfragen (`GetReleasedForPickingAsync`,
  `GetReleasedForPickingByPickerAsync`, `GetReleasedForPickingCountAsync`,
  `GetMaxPickingPriorityAsync`) gegen diese Fixtures ausführen.
  Erwartung: alle vier liefern bzw. zählen ausschließlich die HauptFA-Zeilen (`""` und
  `Sub==Order`), auch wenn die Sub-FA-Zeile freigegeben ist.
  Negativfall: keiner (reiner Regressionsnachweis, automatisiert per InMemory-Test).

- **TS-79.16 — Warteschlange bereinigt (einmaliger SQL-Lauf, NUR Testsystem, Deploy-Vorbedingung).**
  Vorbedingung: im Testsystem eine Sub-FA mit `IsReleasedForPicking = 1` anlegen (oder eine
  bestehende Altlast nutzen). Vorab folgende Zählung ausführen und das Ergebnis im Testprotokoll
  notieren:
  ```sql
  SELECT COUNT(*)
  FROM [dbo].[ProductionOrderPickingStatus] s
  JOIN [dbo].[ProductionOrders] p ON p.Id = s.ProductionOrderId
  WHERE NOT (p.SubOrderNumber = '' OR p.SubOrderNumber = p.OrderNumber)
    AND s.IsReleasedForPicking = 1;
  ```
  Schritte: Kommissionierer-Warteschlange (`/Picking`) **vor** dem SQL-Lauf öffnen; danach den
  korrigierenden Lauf ausführen:
  ```sql
  UPDATE s
  SET s.IsReleasedForPicking = 0,
      s.ModifiedAt = GETUTCDATE(),
      s.ModifiedBy = 'Testsystem-Bereinigung kommissionierung-nur-hauptfa',
      s.ModifiedByWindows = SYSTEM_USER
  FROM [dbo].[ProductionOrderPickingStatus] s
  JOIN [dbo].[ProductionOrders] p ON p.Id = s.ProductionOrderId
  WHERE NOT (p.SubOrderNumber = '' OR p.SubOrderNumber = p.OrderNumber)
    AND s.IsReleasedForPicking = 1;
  ```
  danach die Warteschlange erneut öffnen.
  Erwartung: die Sub-FA erscheint in der Warteschlange **weder vor noch nach** dem Lauf (die
  Warteschlangen-Abfragen filtern bereits strukturell über `.Where(ProductionOrder.IsHauptFa)`, siehe
  TS-79.15); die Home-Kennzahl (`GetReleasedForPickingCountAsync`) zählt sie nicht mit. Der SQL-Lauf
  räumt nur bestehende Testdaten auf — kein `SQL/XX`-Skript, **kein Produktivlauf**.
  Negativfall: keiner — reine Datenbereinigung mit vorab dokumentierter Zählung.

- **TS-79.17 — Warteschlangen-Gruppierung (Hinweis, kein Fehlerfall).**
  Vorbedingung: Kommissionierer-Warteschlange mit mehreren HauptFAs.
  Schritte: `/Picking` öffnen **(Manual-UAT)**.
  Erwartung: die Warteschlange zeigt je HauptFA-Gruppe nur noch **eine** Zeile; Gruppen mit mehreren
  Sub-FAs sind nicht mehr zu erwarten — **das ist erwartetes Verhalten, kein Fehler** (die
  Zwei-Ebenen-Gruppierung der Warteschlange ist durch diese Spec funktional überflüssig geworden; ihr
  eigener Rückbau ist eine separate, noch offene Backlog-Notiz „2026-09-25-picking-warteschlange-
  gruppierung-rueckbau“, nicht Teil dieser Spec).
  Negativfall: keiner (Hinweis, kein Testfall im eigentlichen Sinn).

- **TS-79.18 — Unberührt: FaWorklist/Bom, Glas/Fremdbezug/Lackierung (AK 19, 20).**
  Vorbedingung: Sub-FA mit gesetzten Glas-/Fremdbezug-/Lackierung-Flags, nie freigegeben.
  Schritte: `FaWorklist/Bom` auf dieser Sub-FA-Zeile öffnen **(Manual-UAT)**; Glas/Fremdbezug/
  Lackierung-Checkboxen bedienen.
  Erwartung: die read-only Stückliste bleibt unverändert auf jeder Sub-FA-Zeile erreichbar; die
  Checkboxen bleiben unabhängig vom Freigabe-Status bedienbar; die
  `ProductionOrderPickingStatus`-Zeile der Sub-FA bleibt bestehen.
  Negativfall: keiner (Regressionsnachweis).

- **TS-79.19 — Hilfeseite (Dokumentations-Kriterium).**
  Schritte: `/Help` öffnen, Abschnitte „Leitstand (Kommissionier-Freigabe)“ und „Kommissionierung“
  (Stückliste, hierarchischer Modus) lesen **(Manual-UAT)**.
  Erwartung: beide Abschnitte nennen „Freigabe und Stückliste nur am HauptFA“ bzw. die Schaltfläche
  „Alle Ziele“ inkl. Profil-Eingabe `!(leer)`.
  Negativfall: keiner (Dokumentationsnachweis).

- **TS-79.20 — Kein Leerzustand-Kasten/Badge-Zeile ohne aktiven Standardfilter (Post-QA-Review, F-A).**
  Vorbedingung: hierarchische Stückliste, kein Standardfilter (`DefaultFilterBomKommissionierziel`
  u. a.) im Profil gesetzt.
  Schritte: `/Picking/Bom/{HauptFA-id}` öffnen **(Manual-UAT)**.
  Erwartung: weder die gelbe Box „Alle Filter zurücksetzen“ (`#bomKzEmptyState`) noch die leere
  Badge-Zeile (`#bomDefaultFilterBadges`) sind sichtbar — beide tragen das `hidden`-Attribut, das
  jetzt greift (globale `[hidden]`-Regel in `site.css`, vorher von `.d-flex !important` überschrieben).
  Negativfall: einen Standardfilter setzen → Badge-Zeile erscheint korrekt mit Reset-Chip.

- **TS-79.21 — Standardfilter mit Apostroph bricht die Stückliste nicht (Post-QA-Review, F-B).**
  Vorbedingung: Profil, `DefaultFilterBomKommissionierziel` editierbar.
  Schritte: `KA'02` als Komm.-Ziel-Standardfilter im Profil speichern; hierarchische Stückliste
  öffnen **(Manual-UAT)**.
  Erwartung: Stückliste lädt normal, kein JS-Fehler in der Konsole, der Filter wird angewendet
  (Badge zeigt `KA'02`, Select2-Vorbelegung passend). Werte werden jetzt JSON-serialisiert statt in
  einfache JS-Anführungszeichen eingebettet.
  Negativfall: leerer Standardfilter bleibt falsy (kein Filter, keine Badge) — Regressionsschutz.

- **TS-79.22 — FA-Suche (Picking/IndexDropdown) listet im Hierarchie-Modus nur HauptFAs (Post-QA-Review, F-C).**
  Vorbedingung: Master `ProduktionsauftragHierarchisch = true`, ein HauptFA mit mindestens einer
  Sub-FA, beide über Matchcode/Nummer treffbar.
  Schritte: `/Picking/IndexDropdown` (Select2-FA-Suche) mit einer Eingabe öffnen, die sowohl den
  HauptFA als auch dessen Sub-FA träfe **(Manual-UAT)**.
  Erwartung: nur der HauptFA erscheint in der Trefferliste (`ProductionOrdersApiController` →
  `ProductionOrderRepository.SearchAsync` filtert jetzt auf `ProductionOrder.IsHauptFa`); die
  Sub-FA taucht nicht mehr auf und füllt nicht mehr das 20-Treffer-Limit.
  Negativfall: AKE-Flachmodus (Sub == Order für jeden Datensatz) bleibt unverändert — jeder flache
  Auftrag gilt als HauptFA und erscheint weiterhin.

- **TS-79.20 — Zahnrad (Spaltenauswahl) der Stückliste sichtbar (Bugfix v1.47.3).**
  Vorbedingung: IDEAL-Testsystem, hierarchischer HauptFA mit Komm.-Ziel-Werten (z. B. FA 1035235),
  Benutzer mit Picking-Zugriff.
  Schritte und Erwartung **(Manual-UAT)**:
  1. `/Picking/Bom` für den HauptFA öffnen, **ohne** aktiven Komm.-Ziel-Filter (ggf. vorher „Alle Filter
     zurücksetzen"). Erwartung: Zahnrad-Button rechtsbündig direkt **oberhalb** der Tabelle sichtbar; Klick
     öffnet die Spaltenauswahl (Offcanvas rechts), Spalten lassen sich ein-/ausblenden.
  2. Komm.-Ziel-Filter auf ein Einzelziel setzen (Badge-Zeile erscheint). Erwartung: Zahnrad weiter oberhalb
     der Tabelle, **unterhalb** der Badge-Zeile, nicht in ihr; Spaltenauswahl öffnet sich.
  3. Leerzustand erzeugen: ein Ziel wählen, das in diesem FA keine Zeile hat, bzw. „Alle Ziele" bei einem FA
     ohne kommissionier-relevante Positionen → gelber Hinweis „… ohne Treffer" erscheint. Erwartung: Zahnrad
     steht **nicht** im gelben Hinweis, sondern darunter direkt über der Tabelle; Spaltenauswahl öffnet sich;
     „Alle Filter zurücksetzen" im Hinweis funktioniert weiter.
  4. F5. Erwartung: wie vor dem Neuladen, Zahnrad sichtbar.
  5. AKE-Flachmodus: Stückliste eines flachen AKE-FA öffnen (keine Komm.-Ziel-Spalte), ebenso die
     read-only Stückliste (`FaWorklist/Bom`). Erwartung: Zahnrad oberhalb der Tabelle sichtbar, Spaltenauswahl
     öffnet sich (gleiche Markup-Stelle, unabhängig vom Modus).
  Negativfall: Zahnrad fehlt oder erscheint nur zusammen mit dem gelben Hinweis → Regression (vor v1.47.3:
  Zahnrad hing im versteckten `#bomKzEmptyState`). Automatisiert nur die Platzierung im Quelltext
  (`BomZahnradHostTests`, seit v1.65.0 `TableToolbarHostTests`, TS-98).

**Hinweis (Out-of-Scope, manuell zu beachten statt zu testen):** Andere Picking/Bom-Einstiege für
eine Sub-FA (z. B. der Zeilen-Link in der Kommissionierer-Warteschlange, `Articles/Info`,
`PartRequisitions`, FA-Dokument-Links, das Select2-Auswahlfeld für Produktionsaufträge) enden am
selben Server-Guard (`PickingController.Bom`) mit der `WarningMessage` — kein Loch, nur ein
zusätzlicher Klick bis zur Abweisung. Nicht Teil dieser Spec, kein eigenes Testszenario nötig.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen (Hauptcheckout, nicht Worktree).

---

## TS-80 — IDEAL: Kommissionierliste — Summiert als Standard, Druck/PDF nur sichtbare Spalten (v1.47.0)

**Bezug:** Spec `2026-09-25-kommissionierliste-summierung-pdf-spec` (FREIGABE-ANTWORT 5 vorrangig).

**Pflicht-Vorbedingungen (für alle Szenarien dieses Kapitels, sofern nicht abweichend angegeben):**
- Master **`ProduktionsauftragHierarchisch = true`** (IDEAL) — außer bei TS-80.16
  (Vormontage/Beschichtung-Regression, Teil 4/5) und TS-80.20 (AKE/Feature-Gate), dort jeweils
  unerheblich.
- Ein HauptFA mit mindestens zwei Positionen derselben Artikelnummer und desselben
  Kommissionierziels ist bekannt (für die Summierungs-Szenarien); für die Gruppen-Paging-Szenarien
  mehr HauptFA-Gruppen als die eingestellte Seitengröße.
- Für die Druck-/PDF-Szenarien: mindestens ein Anwender mit gespeicherter Spalten-Präferenz für
  `FaHierarchyKommissionierListen` bzw. `FaHierarchyKommissionierSummiert` und mindestens einer ohne.

**Automatisiert abgedeckt (Auszug):** `KommissionierListenServiceTests`
(`Summiert_NoKw_IncludesAllHauptFa`, `Summiert_CarriesMatchcodeAndHauptlagerplatz_WithoutChangingKey`,
`Summiert_GroupPaging_NeverSplitsHauptFa`, `Summiert_ColumnFilterOnMatchcode`,
`BuildToggleQuery_KeepsSharedKeys_DropsSollmengeAndListOnly`),
`FaHierarchyKommissionierListenControllerTests` (`Print_WithVisibleColumnsParam_UsesItVerbatim_NoPrefRead`,
`Print_WithoutParam_FallsBackToSavedPref`, `Print_WithoutParam_LockedColumnsNeverHidden`,
`Print_WithoutParam_NoPref_ReturnsEmpty`, `Print_WithoutParam_NoUser_ReturnsEmpty`,
`PrintSummiert_UsesSummiertViewKey`, `Summiert_PaginationCountsGroups`,
`PdfSummiert_UnknownHauptFa_NotFound`, `PdfSummiert_RenderFails_RedirectsToSummiertWithParams`),
`UserViewPreferencesApiControllerTests` (`GetByViewKey_FaHierarchyKommissionierSummiert_HasSixColumns`),
`KommissionierSpaltenKonsistenzTests` (Vier-Quellen-Drift-Guard, beide Views). Der manuelle Rest ist
die Sichtprüfung am laufenden IDEAL-Testsystem (Druckausgabe, Zeitverhalten, Sage-DB-Abfrage) — diese
Schritte sind unten mit **(Manual-UAT)** gekennzeichnet.

- **TS-80.1 — Einheit-Prüfschritt (Vorbedingung der Abnahme, kein UI-Test, B3).**
  Vorbedingung: Zugriff auf die Sage-DB **IDEAL** (nicht AKE).
  Schritte: `EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAListe'` ausführen und daraus die
  Positionstabelle ermitteln, die `Sollmenge` liefert; anschließend
  `SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '<Positionstabelle>' AND
  COLUMN_NAME LIKE '%einheit%'` **(Manual-UAT, vor der Abnahme durchzuführen)**.
  Erwartung: **kein Treffer** — bestätigt, dass die Positionstabelle keine eigene, vom Artikel
  abweichende Mengeneinheit führt; Falle 2 (Mengeneinheit) gilt damit als strukturell erledigt, wie in
  `fallstricke.md` dokumentiert (AK 14).
  Negativfall: **ein Treffer** — sofort melden, **nicht** still weiterbauen. Die abweichende Spalte
  gehört dann in den Summierschlüssel (`(HauptFA, Artnr, Kommissionieren)` → zusätzlich Einheit) —
  eigener Nacharbeits-Punkt, blockiert aber diese Abnahme nicht rückwirkend (B3, nicht blockierend für
  den Dev-Lauf).

- **TS-80.2 — Summierung Grundfall (AK 1).**
  Vorbedingung: HauptFA mit derselben Artikelnummer auf zwei Positionen desselben
  Kommissionierziels.
  Schritte: `/FaHierarchyKommissionierListen/Summiert` öffnen **(Manual-UAT)**.
  Erwartung: genau eine Aggregatzeile mit der Summe beider `Sollmenge`-Werte; die Liste-Ansicht
  (`Index`) zeigt weiterhin beide Einzelpositionen unverändert.
  Negativfall: keiner (Regressionsnachweis der bereits seit v1.31.0 bestehenden Summierung).

- **TS-80.3 — Falle 1: Kommissionierziel bleibt Aggregationsschlüssel (AK 2).**
  Vorbedingung: dieselbe Artikelnummer am selben HauptFA mit zwei **unterschiedlichen**
  Kommissionierzielen.
  Schritte: Summiert-Ansicht öffnen **(Manual-UAT)**.
  Erwartung: **zwei** Aggregatzeilen, keine Vermischung der Ziele bzw. der Mengen.
  Negativfall: eine gemeinsame Zeile über beide Ziele hinweg → Regression, sofort melden.

- **TS-80.4 — Falle 3: Aggregation über Artikelnummer, nicht Matchcode (AK 3).**
  Vorbedingung: zwei unterschiedliche Artikelnummern mit identischem Matchcode.
  Schritte: Summiert-Ansicht öffnen **(Manual-UAT)**.
  Erwartung: **zwei** getrennte Aggregatzeilen (Aggregation über `Artnr`); der gemeinsame Matchcode
  erscheint zwar auf beiden Zeilen als Anzeigespalte, führt aber nicht zur Zusammenlegung.
  Negativfall: eine gemeinsame Zeile über beide Artikelnummern hinweg → Regression, sofort melden.

- **TS-80.5 — Standard-Einstieg (AK 4).**
  Vorbedingung: keine besondere.
  Schritte: Menüpunkt „Kommissionierlisten“ an **beiden** Stellen öffnen (Dropdown-Item und
  Standalone-Nav-Link, `_Layout.cshtml`) **(Manual-UAT)**.
  Erwartung: beide Einstiege führen ohne weitere Parameter direkt auf
  `/FaHierarchyKommissionierListen/Summiert`; die Liste (`Index`) bleibt über den Umschalter
  „Liste“ erreichbar.
  Negativfall: einer der beiden Einstiege landet weiterhin auf `Index` → Regression.

- **TS-80.6 — Umschalter nimmt Filter mit, außer Mengenfilter (AK 5, FREIGABE-ANTWORT 5).**
  Vorbedingung: Liste (`Index`) geöffnet.
  Schritte: `target` (Kommissionier-Ziel) sowie Spaltenfilter auf `artnr` und `hauptlagerplatz`
  setzen; zusätzlich einen Filter auf einer Liste-exklusiven Spalte (z. B. `arbeitsbereich`) und auf
  `sollmenge` setzen; zu „Summiert“ wechseln **(Manual-UAT)**.
  Erwartung: `target`, `artnr`- und `hauptlagerplatz`-Filter sind nach dem Wechsel weiterhin aktiv.
  Negativfall a: der `arbeitsbereich`-Filter ist nach dem Wechsel weg — kein Fehler, keine
  Fehlermeldung nötig, stille aber dokumentierte Grenze (die Spalte existiert in der Zielansicht
  nicht).
  Negativfall b: der `sollmenge`-Filter ist nach dem Wechsel weg — **bewusst so** (derselbe
  Filterwert bedeutet in der Liste „Einzelmenge“, in der Summe „Gesamtmenge“; ein mitgenommener Wert
  wäre ein stiller Bedeutungswechsel, siehe Abschnitt E der Spec).
  Zusatzprobe: in „Summiert“ eine KW setzen, zu „Liste“ wechseln (KW bleibt als toter
  Pass-Through-Parameter in der URL, wirkt dort nicht), dann zurück zu „Summiert“ wechseln → die KW
  ist wieder aktiv.

- **TS-80.7 — Druck/PDF-Spalten-Sichtbarkeit, Spalte ausblenden und sofort drucken (AK 6).**
  Vorbedingung: Liste oder Summiert-Ansicht geöffnet, angemeldeter Anwender.
  Schritte: über das Zahnrad eine nicht gesperrte Spalte ausblenden und **innerhalb von 1,5 Sekunden**
  (vor Ablauf der Speicherverzögerung `column-preferences.js`) auf „Drucken“ bzw. „PDF“ klicken
  **(Manual-UAT)**.
  Erwartung: Kopf **und** Zellen der ausgeblendeten Spalte fehlen im Dokument — der Klick liest den
  Spaltenstand direkt aus dem DOM (`print-visible-columns.js`), nicht aus der ggf. noch nicht
  gespeicherten Präferenz.
  Negativfall a (dokumentierte Grenze): Mittelklick oder „Link kopieren“/„Link in neuem Tab öffnen“
  über das Kontextmenü auf den Drucken-/PDF-Link lösen **kein** Klick-Ereignis aus (Strg-Klick und
  Umschalt-Klick lösen sehr wohl ein Klick-Ereignis aus und lesen den DOM-Stand korrekt) — in den
  genannten Fällen greift stattdessen der Server-Rückfall auf die zuletzt **gespeicherte** Präferenz,
  nicht der gerade sichtbare DOM-Stand. Als bekannte, dokumentierte Grenze zu behandeln, kein Defekt
  dieser Etappe.
  Negativfall b (dokumentierte Grenze, vorbestehend, betrifft auch die Liste): wird nach Umsortieren
  der Spalten ausgerechnet die erste physische Spalte ausgeblendet, kann die Gruppen-Kopfzeile
  verschwinden (`column-preferences.js` blendet `cells[idx]` auch in `colspan`-Zeilen aus). Bekannt,
  vorbestehend, kein Defekt dieser Etappe.

- **TS-80.8 — Druck/PDF ohne Parameter, ohne gespeicherte Präferenz (AK 8).**
  Vorbedingung: Anwender ohne gespeicherte Spalten-Präferenz für die jeweilige Ansicht (oder nicht
  eingeloggt erkennbar).
  Schritte: Druck-/PDF-URL direkt aufrufen, ohne vorherigen Klick auf der Seite (kein
  `visibleColumns`-Parameter) **(Manual-UAT)**.
  Erwartung: **alle** Spalten erscheinen im Dokument.
  Negativfall: eine handgebaute URL mit ausschließlich unbekannten `visibleColumns`-Schlüsseln (z. B.
  `visibleColumns=xyz`) → leere Positionstabelle (kein Server-Rückfall, da der Parameter technisch
  gesetzt ist); dokumentierte Grenze, tritt praktisch nur bei manuell manipulierter URL auf, da das
  JS den Parameter nur mit mindestens einer echten sichtbaren Spalte setzt.

- **TS-80.9 — Druck/PDF ohne Parameter, mit gespeicherter Präferenz (AK 7).**
  Vorbedingung: Anwender mit gespeicherter Spalten-Präferenz, die mindestens eine nicht gesperrte
  Spalte ausblendet.
  Schritte: wie TS-80.8, aber mit dieser Präferenz **(Manual-UAT)**.
  Erwartung: genau die dort als sichtbar markierten Spalten erscheinen (gesperrte Spalten wie
  `hauptfa`/`matchcode` bleiben immer sichtbar); Server-Rückfall.
  Negativfall: eine ausgeblendete gesperrte Spalte im Druck fehlt trotzdem → Regression.

- **TS-80.10 — Filter-Durchschlag im Druck/PDF der Liste (Regression, AK 11).**
  Vorbedingung: Spaltenfilter **und** Ziel-Filter aktiv in der Liste.
  Schritte: „Drucken“ bzw. „PDF je Gruppe“ klicken **(Manual-UAT)**.
  Erwartung: Dokument zeigt nur die gefilterten Zeilen, Kopf trägt das Banner „Gefilterte Ansicht“
  mit dem gewählten Ziel — unverändert zum bisherigen Verhalten.
  Negativfall: ungefilterte oder zusätzliche Zeilen im Ausdruck → Regression.

- **TS-80.11 — Druck/PDF der Summiert-Ansicht, Banner-Kombination (AK 9, 10).**
  Vorbedingung: Summiert-Ansicht mit mindestens einer HauptFA-Gruppe.
  Schritte a: ohne aktiven Filter drucken/PDF je Gruppe erzeugen **(Manual-UAT)**.
  Erwartung a: Dokument zeigt die Aggregatzeilen (nicht die Rohpositionen), Kopf trägt sichtbar den
  Hinweis „Summierte Ansicht“.
  Schritte b: zusätzlich einen Spalten- oder Ziel-Filter aktivieren, erneut drucken.
  Erwartung b: **beide** Hinweise („Gefilterte Ansicht“ und „Summierte Ansicht“) erscheinen
  nebeneinander im Kopf.
  Negativfall: nur einer der beiden Hinweise erscheint trotz beider Bedingungen → Regression.

- **TS-80.12 — Kalenderwoche optional (AK 13).**
  Vorbedingung: Summiert-Ansicht, HauptFAs mit und ohne passenden KO_Termin für eine Test-KW.
  Schritte a: Summiert-Ansicht **ohne** KW-Eingabe öffnen **(Manual-UAT)**.
  Erwartung a: die Summe über **alle** HauptFA erscheint sofort, kein „Bitte KW eingeben“-Hinweis
  mehr (Ablösung der früheren Entscheidung `AK N2d`, siehe Vermerk in
  `2026-07-29-standort-ideal-teil-3-spec`).
  Schritte b: eine KW eingeben.
  Erwartung b: die Ansicht filtert wie bisher auf die HauptFA der gewählten Woche.
  Negativfall: keiner (reine Verhaltensänderung, keine Fehlerbedingung).

- **TS-80.13 — Gruppen-Paging, kein Seitenriss (AK 15).**
  Vorbedingung: mehr HauptFA-Gruppen als die eingestellte Seitengröße.
  Schritte: Summiert-Ansicht seitenweise durchblättern **(Manual-UAT)**; denselben Zustand im
  seitenweisen Ausdruck prüfen (sofern mehrseitig relevant, sonst nur Bildschirm).
  Erwartung: keine HauptFA-Gruppe reißt über zwei Seiten — weder am Bildschirm noch im Ausdruck;
  `TotalCount` der Pagination zählt Gruppen, nicht Zeilen (eine Gruppe mit vielen Positionen kann die
  angezeigte Zeilenzahl einer Seite deutlich über die eingestellte Seitengröße heben).
  Negativfall: eine Gruppe erscheint teils auf Seite 1, teils auf Seite 2 → Regression.

- **TS-80.14 — PdfSummiert-Fehler-Rückweg (AK 16).**
  Vorbedingung: PDF-Erzeugung künstlich zum Scheitern gebracht (z. B. Edge auf dem Web-Server
  vorübergehend deaktiviert/nicht erreichbar).
  Schritte: PDF-Knopf einer HauptFA-Gruppe in der Summiert-Ansicht mit aktivem `target`/`kwVon`/
  `kwBis` klicken **(Manual-UAT)**.
  Erwartung: Rückweg führt auf die **Summiert**-Ansicht mit denselben Query-Parametern (`target`,
  `kwVon`, `kwBis`) zurück, sichtbare `WarningMessage` — **nicht** auf `Index` (abweichend vom
  Vorbild `Pdf`/Liste, SOLLTE-6, fachlich bewusst).
  Negativfall: Rückweg landet auf `Index` oder verliert die Filterwerte → Regression.

- **TS-80.15 — Nebenbefund: Spalten-Präferenz der Summiert-Ansicht speicherbar (AK 12).**
  Vorbedingung: Summiert-Ansicht geöffnet, angemeldeter Anwender.
  Schritte: über das Zahnrad Matchcode oder Hauptlagerplatz ausblenden, Auswahl speichern lassen,
  Seite neu laden, danach drucken **(Manual-UAT)**.
  Erwartung: `GET /api/userviewpreferences?viewKey=FaHierarchyKommissionierSummiert` liefert einen
  `ViewConfig` mit genau 6 Spalten (`hauptfa`, `artnr`, `matchcode`, `kommissionieren`,
  `hauptlagerplatz`, `sollmenge`); die gespeicherte Auswahl wirkt sowohl nach dem Neuladen am
  Bildschirm als auch im Druck (Server-Rückfall, TS-80.9). Vor dieser Etappe lehnte
  `UserViewPreferencesApiController` das Speichern für diese Ansicht mangels `GetByViewKey`-
  Registrierung ab — jetzt behoben.
  Negativfall: Speichern schlägt fehl oder wirkt nicht auf den Druck → Regression.

- **TS-80.16 — Regression Vormontage/Beschichtung (unverändert).**
  Vorbedingung: Teil 4 (Beschichtungsauftrag) und Teil 5 (Vormontage) unverändert nutzbar.
  Schritte: beide Ansichten öffnen, drucken **(Manual-UAT)**.
  Erwartung: unverändert funktionsfähig — diese Spec berührt `WarehousePickingController`/
  `WarehousePickingPrintLayout` und die Vormontage-Druckwege nicht (siehe auch TS-80.19).
  Negativfall: keiner (Regressionsnachweis).

- **TS-80.17 — Skript-Sicherheit bei Sonderzeichen im Filterwert (AK 18).**
  Vorbedingung: Liste oder Summiert-Ansicht geöffnet.
  Schritte: einen Kommissionier-Ziel- oder Spaltenfilterwert mit `</script>`-artigem Inhalt setzen,
  z. B. `LAGER");</script><script>alert(1)</script>` als Testwert **(Manual-UAT)**; Bildschirmansicht,
  Druck und PDF jeweils prüfen.
  Erwartung: weder Bildschirmansicht noch Druck/PDF brechen; kein Skript wird ausgeführt; der Wert
  erscheint als reiner, kodierter Text. `grep -n "Html.Raw"` über die fünf geänderten/neuen Views
  liefert keinen Treffer — keine neue Einbettung eines Filterwerts in einen `<script>`-Block ohne
  JSON-Kodierung.
  Negativfall: ein `alert(1)` löst tatsächlich aus → sofortiger Blocker, kein Merge.

- **TS-80.18 — Vier-Quellen-Konsistenz, Drift-Guard (AK 17, automatisiert).**
  Vorbedingung: keine (automatisierter Test, kein UI-Schritt).
  Schritte: `dotnet test IdealAkeWms.Tests --filter FullyQualifiedName~KommissionierSpaltenKonsistenzTests`
  ausführen; als Gegenprobe einen Spaltenschlüssel in einer der vier Quellen (`ColumnDefinitions`,
  `#column-config`, `<th data-col-key>`, `ShowCol(...)`) für `FaHierarchyKommissionierListen` oder
  `FaHierarchyKommissionierSummiert` künstlich umbenennen und den Test erneut ausführen.
  Erwartung: im Normalzustand grün (2/2, je ein Fall je View); in der Gegenprobe rot, mit einer
  Fehlermeldung, die Quelle, fehlenden und zusätzlichen Schlüssel exakt benennt.
  Negativfall: der Test bleibt bei einer künstlichen Drift grün → Test selbst defekt, sofort melden.

- **TS-80.19 — `WarehousePickingController`/`WarehousePickingPrintLayout` unberührt (AK 20,
  Regressionscheck).**
  Vorbedingung: keine besondere.
  Schritte: `WarehousePickingController.Print` (Wareneingang/Lagerplatz-Etiketten-Druck) unverändert
  aufrufen **(Manual-UAT)**; ergänzend `git diff` bzw. Codeabgleich, dass `WarehousePickingPrintLayout`
  keine neue Überladung erhalten hat.
  Erwartung: unverändertes Verhalten, keine neue Überladung, kein Verhaltenswechsel.
  Negativfall: keiner (reiner Regressionsnachweis).

- **TS-80.20 — AKE/Feature-Gate (AK 4, Zugriffsschutz).**
  Vorbedingung: AppSetting `FaHierarchyKommissionierlistenAktiv` **ungesetzt oder ungleich
  `"true"`** (Default-aus, `RequireFaHierarchyKommissionierlistenAktivAttribute`); Anwender mit
  Lager-Rolle (`RequireLagerProcessingAccess` würde sonst passieren).
  Schritte: `_Layout.cshtml` an beiden Stellen (Dropdown-Item und Standalone-Nav-Link) prüfen; danach
  die drei URLs `/FaHierarchyKommissionierListen/Summiert`, `/FaHierarchyKommissionierListen/PrintSummiert`
  und `/FaHierarchyKommissionierListen/PdfSummiert/100` direkt aufrufen **(Manual-UAT)**.
  Erwartung: Der Menüpunkt „Kommissionierlisten“ erscheint an **keiner** der beiden Stellen (beide
  hinter `faKommListenAktiv` gegated). Alle drei direkten URLs werden vom Feature-Filter
  (`RequireFaHierarchyKommissionierlistenAktivFilter`) abgefangen, **bevor** die Action läuft: da
  `FaHierarchyKommissionierListenController` ein MVC-`Controller` ist (kein `[ApiController]`),
  liefert der Filter **keine 404**, sondern `RedirectToActionResult("Index", "Home")` mit
  `TempData["WarningMessage"] = "Das IDEAL-Kommissionierlisten-Modul ist deaktiviert."` — sichtbares
  gelbes Banner auf der Startseite.
  Negativfall (AK-4-Kopplung): AppSetting auf `"true"` setzen, aber der Anwender hat **keine**
  `RequireLagerProcessingAccess`-Berechtigung → derselbe Redirect-Mechanismus greift bereits vorher
  über den Rollen-Filter (kumulativ, beide Filter müssen passieren); der Kommissionierlisten-Menüpunkt
  bleibt auch hier verborgen. Ein Anwender, der weder die Rolle noch das Feature freigeschaltet hat,
  darf unter keiner Kombination auf die Summiert-/Print-/Pdf-Actions gelangen.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen (Hauptcheckout, nicht Worktree).

---

## TS-81 — IDEAL: Beschichtungsauftrag — Summiert als Standard (v1.48.0)

Spec: `2026-10-06-beschichtungsauftrag-ausbau-teil-1-spec`. Ergänzt TS-60 (Einzelliste, unverändert) und
TS-74 (PDF). **Vorbedingungen (alle Fälle):** IDEAL-Testsystem, `ProduktionsauftragHierarchisch = true`,
AppSetting `FaHierarchyBeschichtungAktiv` an, Benutzer mit Rolle `beschichtungsauftrag` (oder `admin`),
FA-Struktur importiert (`FaHierarchyNodes` mit `Beschichtet = 1`). Automatisiert: `BeschichtungsauftragServiceTests`
(Aggregation, `Summiert_*`), `FaHierarchyBeschichtungControllerTests`, `KommissionierSpaltenKonsistenzTests`
(vier Spaltenquellen), `FaHierarchyBeschichtungSummiertZahnradHostTests` (seit v1.65.0 `TableToolbarHostTests`). Alles Sichtbare ist Manual-UAT.

- **TS-81.1 — Prüfschritt Mengeneinheit (vor der Abnahme, kein UI-Test).** Auf der Sage-DB IDEAL:
  `EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAListe';` → welche Tabelle liefert `Sollmenge`? Dann
  `SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '<Positionstabelle>' AND COLUMN_NAME LIKE '%einheit%';`
  **Erwartet:** kein Treffer ⇒ Summierung je Artnr ist einheitensicher (Einheit = Artikeleigenschaft).
  Treffer ⇒ **melden** (Einheit gehört in den Schlüssel), Abnahme von TS-81.3 bis dahin unter Vorbehalt. Ergebnis
  hier notieren: ______ (gleicher Befund wie TS-80.1, falls dort schon geprüft).
- **TS-81.2 — Navigation und Umschalter (AK 1).** Menüpunkt ~~„Beschichtungsauftrag"~~ (seit v1.56.0: *Beschichtung →
  Vorschlagsliste*) klicken. **Erwartet:**
  URL `/FaHierarchyBeschichtung/Summiert`, Umschalter „Liste | Summiert" mit „Summiert" aktiv, Menüpunkt
  markiert. „Liste" klicken → `/FaHierarchyBeschichtung` (Einzelliste wie TS-60), dort „Liste" aktiv; „Summiert"
  → zurück.
- **TS-81.3 — Summenzeile (AK 2, 4a).** Testdaten: HauptFA mit mehrfach vorkommendem gleichem beschichtetem Teil
  (`SELECT HauptFA, Artnr, COUNT(*) FROM dbo.FaHierarchyNodes WHERE Beschichtet = 1 GROUP BY HauptFA, Artnr HAVING COUNT(*) > 1;`).
  **Erwartet:** genau eine Zeile je (HauptFA, Artnr), „Summe Sollmenge" = Summe der Einzelwerte aus der Liste,
  „Positionen" = Anzahl, Breite/Höhe/Tiefe angezeigt.
- **TS-81.4 — Negativfall andere Artnr / anderer HauptFA (AK 3, 4).** Im selben HauptFA zwei verschiedene
  Artnr → zwei Zeilen. Dieselbe Artnr in zwei HauptFA → je HauptFA eine eigene Gruppe, nie zusammengefasst.
- **TS-81.5 — Maße uneinheitlich (AK 4a).** Testdaten suchen:
  `SELECT HauptFA, Artnr FROM dbo.FaHierarchyNodes WHERE Beschichtet = 1 GROUP BY HauptFA, Artnr HAVING COUNT(DISTINCT CONCAT(Breite,'|',Hoehe,'|',Tiefe)) > 1;`
  Leer ⇒ Fall im Ist-Bestand nicht vorhanden, Ergebnis notieren: ______. Sonst die Zeile öffnen. **Erwartet:**
  eine Zeile (nicht aufgeteilt), Maße der ersten Position, in der Matchcode-Spalte der gelbe Hinweis
  **„Maße uneinheitlich"** (als Text lesbar); im Web-Log eine Warnung „Beschichtungsauftrag summiert: HauptFA …,
  Artnr … — Masse uneinheitlich". Druck und PDF zeigen denselben Hinweis.
- **TS-81.6 — Nur Beschichtet, alle Ebenen (AK 5).** Eine beschichtete Position mit `SubFA <> 0` im HauptFA →
  zählt in der Summe mit; nicht beschichtete Positionen erscheinen nie.
- **TS-81.7 — Kopf je HauptFA, Kombigerät (AK 6).** Gruppenkopf zeigt Dienstleister/RAL/Start/Retour/AB/Pos/
  Montage-Abt. wie die Liste. Kombigerät (falls Testdaten: `SELECT HauptFA FROM dbo.FaHierarchyOrderInfos GROUP BY HauptFA HAVING COUNT(*) > 1;`)
  → alle Kopfvarianten + Badge „mehrdeutig (mehrere Kopfzeilen)" + Log-Warnung. HauptFA ohne Kopfzeile →
  „Keine Auftrags-Kopfdaten zu diesem HauptFA."
- **TS-81.8 — Listen-Muster (AK 7, 8).** Seitengröße 25 → Paginierung zählt HauptFA-Gruppen, keine Gruppe reißt
  über zwei Seiten. Spaltenfilter „Summe Sollmenge" auf einen Summenwert (ohne Komma eingeben, Komma = ODER)
  → nur Zeilen, deren **Summe** passt. Spaltenfilter „Artnr." setzen, „Liste" klicken → Filter bleibt gesetzt;
  ein Filter auf „Summe Sollmenge" wird **nicht** mitgenommen.
- **TS-81.9 — Zahnrad (Spaltenauswahl).** Zahnrad steht rechts direkt über der Tabelle, öffnet die
  Spaltenauswahl; „Tiefe" ausblenden → Spalte verschwindet; HauptFA und Matchcode sind nicht ausblendbar.
- **TS-81.10 — Druck (AK 9).** „Drucken" → neues Fenster, ein Ausdruck je HauptFA mit Barcode, Kopf, Hinweis
  „Summierte Ansicht", dieselben Zeilen wie am Bildschirm ohne Paging; vorher ausgeblendete Spalte („Tiefe") fehlt
  auch im Druck. Mit aktivem Spaltenfilter zusätzlich der Hinweis „Gefilterte Ansicht".
- **TS-81.11 — PDF (AK 9).** PDF-Knopf an einer Gruppe → Datei `Beschichtungsauftrag_<HauptFA>_<yyyyMMdd-HHmm>.pdf`,
  Inhalt wie Druck dieser Gruppe. Ohne Edge auf dem Server → kein PDF-Knopf. PDF-Fehler (z. B. Timeout) → zurück
  auf die summierte Ansicht mit gelber Meldung.
- **TS-81.12 — Leerfall.** Spaltenfilter ohne Treffer (z. B. Artnr `XYZ-NICHTDA`) → Hinweis „Keine beschichteten
  Positionen für die aktuelle Filterung gefunden."; „Drucken" → Hinweis „nichts zu drucken" statt leerem Blatt.
- **TS-81.13 — Zugriff.** Ohne Rolle `beschichtungsauftrag`/`admin` → `/FaHierarchyBeschichtung/Summiert`,
  `/PrintSummiert` und `/PdfSummiert/<HauptFA>` → AccessDenied; Toggle `FaHierarchyBeschichtungAktiv` aus →
  gesperrt, Menüpunkt verborgen (seit v1.56.0: Menü *Beschichtung* verborgen).
- **TS-81.14 — Regression Einzelliste (AK 11).** TS-60 und TS-74 für die Liste wiederholen: Spalten, Kopf,
  Druck, PDF unverändert (Kopf jetzt über dasselbe Partial wie die summierte Ansicht — Inhalt identisch).

---

## TS-82 — IDEAL: Briefkopf auf dem Beschichtungsauftrag (v1.49.0)

Spec: `2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec`. **Vorbedingungen (alle Fälle):** IDEAL-Testsystem,
`FaHierarchyBeschichtungAktiv = true`, Tester mit Rolle `beschichtungsauftrag`, Edge auf dem Webserver (PDF).
Unterordner `Branding\` **im** Programmordner der Web-App (nicht in `wwwroot\`; seit v1.59.0 korrigiert, vorher hieß es
„neben dem Publish-Ordner") mit `briefkopf.html` + `IDEAL_AKE_Logo_rgb_klein.jpg` (aus dem Repo `Branding/`) und den
Schriftdateien `NimbusSanNov-Reg.otf`/`NimbusSanNov-SemBol.otf`; App-Pool-Identität hat Leserecht.
Standorteinstellungen → Firmendaten: „Briefkopf-Vorlage (Pfad)" = voller Pfad zur Datei; Firmenname/Firmenanschrift
gepflegt („In der Schörihub 28, 4810 Gmunden"). Automatisiert: `BriefkopfVorlageServiceTests` (Laden, Bereinigen,
Einbetten, Grenzen, Muster-Vorlage), `BriefkopfEinbindungTests` (Empfänger, Einbindung, keine Kommissionierlisten).
Alles Sichtbare, PDF und Seitenumbruch sind Manual-UAT.

- **TS-82.1 — Prüfschritt Nebenbefund „Dienstleister? · RAL 0" (Sage, vor Abnahme).** Für den betroffenen HauptFA:
  `SELECT [HauptFA],[Abteilung],[Dienstleister],[RAL],[Start_Beschichtung],[Beschichten_Retour] FROM [vw_IDEAL-AKE_Kommissionierung_FAInfos] WHERE [HauptFA] = <HauptFA>;`
  und im WMS `SELECT HauptFA, MontageAbteilung, Dienstleister, RAL FROM dbo.FaHierarchyOrderInfos WHERE HauptFA = <HauptFA>;`
  Gleich ⇒ Datenlücke in Sage; verschieden ⇒ Sync-Befund, melden. Ergebnis: ______
- **TS-82.2 — Happy Path (AK 1, 2, 10).** Beschichtungsauftrag → „Drucken" (Summiert) und „Liste" → „Drucken", je ein
  PDF. **Erwartet:** je HauptFA-Dokument oben Logo (Logozone frei von anderen Elementen), darunter die Absenderzeile
  „IDEAL Kältetechnik GmbH · In der Schörihub 28, 4810 Gmunden · …" (Firmenname IDEAL Blau), dünne hellblaue Linie,
  dann Empfänger (Dienstleister, fett), Dokument „Beschichtungsauftrag[ (summiert)] — HauptFA …", Datum. Kopftabelle,
  Positionen, Barcode und Banner darunter unverändert in Segoe UI. PDF → Eigenschaften → Schriftarten: Nimbus Sans Novus
  eingebettet; Logo im PDF sichtbar (kein Rahmen/kaputtes Bild).
- **TS-82.3 — Nur Seite 1 (AK 8).** HauptFA mit vielen beschichteten Positionen (mehrseitig) als PDF — Kandidaten:
  `SELECT TOP 5 HauptFA, COUNT(*) AS Positionen FROM dbo.FaHierarchyNodes WHERE Beschichtet = 1 GROUP BY HauptFA ORDER BY COUNT(*) DESC;`
  (ab rund 35 Positionen in der Liste wird es mehrseitig; sonst die Einzelliste „Liste" drucken). **Erwartet:**
  Briefkopf nur auf Seite 1, Folgeseiten beginnen mit Positionen. Keine Seitenzählung „Seite X von Y" (bewusst
  weggelassen, AK 14 — Aufgabe im Brain).
- **TS-82.4 — Änderung ohne Deploy (AK 3).** Telefonnummer in `briefkopf.html` ändern, Logo-Datei austauschen
  (gleicher Name) → nächster Druck zeigt beides, ohne Neustart.
- **TS-82.5 — Pfad leer / Datei fehlt / falsche Endung (AK 6, 21).** (a) Pfad-Feld leeren, (b) Datei umbenennen,
  (c) Pfad auf die `appsettings.json` des Webs setzen. **Erwartet:** jeweils Ersatz-Kopf „Firmenname · Anschrift" in
  einer Zeile; am Bildschirm gelber Hinweis „Briefkopf: …" mit Grund („nicht konfiguriert" / „nicht gefunden: <Pfad>" /
  „keine .html/.htm-Datei"); im Ausdruck und im PDF **kein** Hinweis; bei (c) kein Inhalt der JSON-Datei; Web-Log
  Warnung „Briefkopf: …". PDF-Download funktioniert (keine gelbe Umleitungsmeldung). Ab v1.59.0: **keine Fußzeile**.
- **TS-82.6 — Bild fehlt (AK 16).** Logo-Datei umbenennen → Briefkopf ohne Logo, kein Broken-Image-Symbol, Hinweis +
  Log nennen `IDEAL_AKE_Logo_rgb_klein.jpg`.
- **TS-82.7 — Schrift fehlt (AK 17).** Schriftdateien umbenennen → Briefkopf in Segoe UI/Arial, Hinweis + Log nennen die
  Dateien (im Muster ab v1.59.0 `NimbusSanNov-Reg.otf`/`NimbusSanNov-SemBol.otf`).
- **TS-82.8 — Script in der Vorlage (AK 18).** In `briefkopf.html` einfügen: `<script>alert(1)</script>`,
  `<img src="IDEAL_AKE_Logo_rgb_klein.jpg" onerror="alert(2)">`, `<a href="javascript:alert(3)">x</a>`.
  **Erwartet:** kein Dialog (Bildschirm und PDF); Quelltext der Druckseite enthält weder `script` noch `onerror` noch
  `javascript:`; Knöpfe „Drucken"/„Schliessen" funktionieren.
- **TS-82.9 — Fremde Verweise (AK 9, 19).** In die Vorlage: `<img src="..\..\x.png">`, `<img src="C:\Windows\x.png">`,
  `<img src="\\server\share\x.png">`, `<img src="https://www.ideal-ake.at/logo.png">`. **Erwartet:** nichts davon
  eingebettet oder geladen (Browser-Netzwerk-Tab ohne Anfrage an ideal-ake.at), je ein Hinweis „abgelehnt". Quelltext
  der Druckseite: im Briefkopf kein `<link>`, alle `src=`/`url(` beginnen mit `data:`.
- **TS-82.10 — Größengrenzen (AK 20).** (a) `briefkopf.html` über 256 KB aufblähen (langer Kommentar) → Ersatz-Kopf +
  Hinweis „zu groß"; (b) Logo durch eine Datei über 1 MB ersetzen → Briefkopf ohne Logo + Hinweis „zu groß".
- **TS-82.11 — Empfänger (AK 4, 5).** HauptFA ohne Dienstleister in Sage → Empfänger „Empfänger fehlt — in Sage
  nachpflegen" (auch im Ausdruck). Kombigerät (mehrere Kopfzeilen) → „mehrdeutig — siehe Kopfzeilen", die gelbe
  Mehrdeutigkeits-Box darunter bleibt.
- **TS-82.12 — Einstellung (AK 12, 15).** Standorteinstellungen → Firmendaten zeigt „Briefkopf-Vorlage (Pfad)" mit
  Hinweistext; „Anschrift / Adresse" weiter einzeiliges Feld. Nicht-Admin → `/StandortEinstellungen` → AccessDenied.
- **TS-82.13 — Regression (AK 10, 13).** Kommissionierliste (Liste und Summiert) drucken → kein Briefkopf. TS-60/TS-81:
  Inhalt unter dem Briefkopf unverändert.
- **TS-82.15 — Fehlerhaftes Markup in der Vorlage (QA I-1).** Nacheinander je einen Fehler in `briefkopf.html`
  einbauen und drucken: (a) `<!-- offen` ohne `-->`, (b) `<style>` ohne `</style>`, (c) ein überzähliges `</div>`,
  (d) `<div>` ohne Gegenstück. **Erwartet:** jeweils Ersatz-Kopf + gelber Hinweis mit dem Grund („nicht geschlossenen
  Kommentar" / „unvollständiges Element" / „ungleich viele <div>"); Kopftabelle, Positionen und alle weiteren HauptFA
  stehen vollständig darunter (nichts verschluckt, keine zusätzliche Leerseite). Danach Vorlage wiederherstellen.
- **TS-82.14 — CD-Abgleich (AK 11).** Am Ausdruck: Logo-Höhe 17,5 mm (Lineal), Logozone 37,125 mm frei, Farben laut
  CD-Auszug. Hinweis: ohne Fußbereich in der Vorlage bleiben die Seitenränder die heutigen der Druckansicht (kein
  `@page`-Umbau); mit Fußbereich (ab v1.59.0, TS-92) `@page` 10/10/5/10 mm und unten 24,75 mm für die Fußzeile.

---

## TS-83 — IDEAL: Beschichtungsauftrag als eigenes Objekt (Datenbank + Workflow, v1.50.0)

Spec: `2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec`. **Backend ohne Oberfläche** — die Bedienung kommt mit Teil 4
(dort die Klick-Szenarien). Abnahme hier: Prüfschritte auf Sage/WMS-DB, SQL-Skript, Regression. Automatisiert:
`BeschichtungsauftragWorkflowTests` (Nummernkreis inkl. Jahreswechsel und Konflikt-Wiederholung, Statusautomat,
Dienstleister-Pflicht, Doppelvergabe je Status, Storno gibt frei, Anlegen nur freie Positionen, Bearbeiten,
Hinzufügen im Erst- und Reklamations-Entwurf, Reklamation inkl. Kette und Mehrfach-Reklamation, Snapshot nach Sync),
`BeschichtungsauftragDiResolutionTests`, `BeschichtungsauftragMigrationSqlTests` (Objektnamen Migration = SQL/94 =
FreshInstall). Nicht InMemory-testbar und deshalb hier: SQL-Skript, RowVersion-Konflikt, Serializable-Transaktion.

- **TS-83.1 — Prüfschritt Schlüssel-Eindeutigkeit (vor Abnahme, Ergebnis notieren).** Sage-DB IDEAL:
  `SELECT [HauptFA],[VaterFA],[SubFA],[Position],COUNT(*) FROM [vw_IDEAL-AKE_Kommissionierung_FAListe] WHERE [Beschichtet] <> 0 GROUP BY [HauptFA],[VaterFA],[SubFA],[Position] HAVING COUNT(*) > 1;`
  und WMS: `SELECT HauptFA,VaterFA,SubFA,Position,COUNT(*) FROM dbo.FaHierarchyNodes WHERE Beschichtet = 1 GROUP BY HauptFA,VaterFA,SubFA,Position HAVING COUNT(*) > 1;`
  **Erwartet:** leer ⇒ der Herkunftsschlüssel trägt die Doppelvergabe-Prüfung. Treffer ⇒ **melden** (dann gehört z. B.
  `Artnr` in den Schlüssel; der Workflow meldet doppelte Schlüssel beim Anlegen als Hinweis). Ergebnis: ______
- **TS-83.2 — Prüfschritt View-Filter (Sage).** `EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAInfos';` — nach
  welchem Kriterium fällt ein HauptFA aus der View (und damit aus dem Cache)? Ergebnis notieren: ______ (bestimmt, wie
  oft gespeicherte Aufträge auf nicht mehr vorhandene Knoten zeigen; die Aufträge bleiben dank Snapshot lesbar).
- **TS-83.3 — SQL-Skript idempotent (AK 1).** Auf einer Test-DB (Kopie) `SQL/94_AddBeschichtungsauftrag.sql` **zweimal**
  hintereinander ausführen. **Erwartet:** erster Lauf meldet „Tabelle … angelegt", zweiter Lauf ohne Fehler und ohne
  Meldung; `SELECT * FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261012073551_AddBeschichtungsauftrag'` genau
  eine Zeile. Danach die Web-App starten — `Migrate()` darf nichts mehr anlegen (kein Fehler im Log).
- **TS-83.4 — FreshInstall (AK 1).** `SQL/00_FreshInstall.sql` auf einer **leeren** DB. **Erwartet:** beide Tabellen mit
  allen Indizes (`IX_Beschichtungsauftraege_Nummer` und `…_Jahr_LaufendeNummer` UNIQUE), History-Eintrag vorhanden.
- **TS-83.5 — Nummer-Eindeutigkeit in der DB (AK 4, SQL).** Auf der Test-DB zweimal dieselbe Nummer einfügen:
  `INSERT INTO dbo.Beschichtungsauftraege (Nummer,Jahr,LaufendeNummer,HauptFA,Status,CreatedAt,CreatedBy,CreatedByWindows) VALUES ('BA-2026-99999',2026,99999,1,10,GETDATE(),'test','test');`
  zweimal ausführen. **Erwartet:** zweiter Insert scheitert am UNIQUE-Index (`IX_Beschichtungsauftraege_Nummer`).
  Danach die Testzeile **löschen** — sonst wäre die nächste vergebene Nummer `BA-2026-100000`:
  `DELETE FROM dbo.Beschichtungsauftraege WHERE Nummer = 'BA-2026-99999';` (Die Wiederholung mit neuer Nummer prüft der
  Unit-Test.)
- **TS-83.6 — Parallelität (AK 10, Manual-UAT mit Teil 4).** Sobald Teil 4 die Bedienung liefert: zwei Sessions, derselbe
  Auftrag, gleichzeitig Status wechseln → der zweite erhält „wurde inzwischen von jemand anderem geändert"; zwei Sessions
  legen gleichzeitig einen Auftrag für denselben HauptFA an → nur einer übernimmt die freien Positionen, der andere
  meldet sie als vergeben bzw. „bitte erneut versuchen"; zwei Sessions fügen gleichzeitig **dieselbe freie Position** in
  zwei verschiedene Entwürfe desselben HauptFA ein → nur einer erhält sie, der andere eine Klartext-Meldung („bereits
  vergeben" bzw. „… bitte erneut versuchen"), **keine** Fehlerseite/Ausnahme (Serializable-Transaktion, QA I-1).
- **TS-83.7 — Regression (AK 12).** Beschichtungsauftrag Liste/Summiert, Druck und PDF (TS-60, TS-74, TS-81, TS-82)
  unverändert; kein neuer Menüpunkt.

## TS-84 — IDEAL: Beschichtungsaufträge bedienen — Vorschlagsliste, Detail, Reklamation, Druck (v1.51.0)

Spec: `2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec`. Manual-UAT am **IDEAL-Testsystem** mit produktivnahen
beschichteten Positionen (Vorbedingung wie TS-60), Schalter `FaHierarchyBeschichtungAktiv = true`, Teil-3-Tabellen
angelegt (TS-83.3). **Es entstehen echte Aufträge mit fortlaufender Nummer** — Testaufträge am Ende stornieren.
Benutzer: **S** mit Rolle `beschichtungsauftrag_schreiben` (neu, im Deploy anlegen und zuweisen), **L** nur mit
`beschichtungsauftrag`. Automatisiert: `BeschichtungsauftraegeControllerTests` (echter Teil-3-Workflow auf InMemory:
Anlegen, Bearbeiten inkl. leerer/ungültiger Daten und Mengen mit Komma/Punkt, veraltete RowVersion, dieselbe freie
Position in zwei Entwürfe, ungültiger Positionsschlüssel, Dienstleister-Pflicht, Reklamation nur aus Erledigt,
Druck nur ab Übergeben, PDF-Fehler, Liste mit Status-/Spaltenfilter, Schreibfilter an jeder schreibenden Action),
`BeschichtungsauftragVergabeAnzeigeTests`, `BeschichtungsauftraegeViewTests` (Spaltenquellen, Briefkopf; Zahnrad-Host seit v1.65.0 in `TableToolbarHostTests`),
`CurrentUserServiceRoleTests`, `RequireBeschichtungsauftragWriteAccessFilterTests`.

- **TS-84.1 — Anlegen aus der Vorschlagsliste (AK 1, 2).** S öffnet *Beschichtungsauftrag* (Summiert), wählt einen
  HauptFA ohne Auftrag → im Gruppenkopf **„Aus Vorschlagsliste erstellen"** klicken. **Erwartet:** Detail des neuen
  Auftrags, grüne Meldung mit Nummer `BA-JJJJ-NNNNN` und Positionsanzahl, Status *Entwurf*, alle beschichteten
  Positionen des HauptFA, Dienstleister = Sage-Wert. Zurück in der Vorschlagsliste: Gruppenkopf zeigt „Aufträge: BA-…"
  als Link, **kein** Knopf mehr; in der Liste (Einzelpositionen) trägt jede Position in der Spalte *Beschichtet* „in BA-…".
  Spaltenfilter *Beschichtet* = „Ja" findet die Zeilen weiterhin.
- **TS-84.2 — Teilvergabe (AK 2a).** Im Entwurf aus TS-84.1 eine Position per Haken *Entfernen* + „Entwurf speichern"
  entfernen. **Erwartet:** Vorschlagsliste zeigt Nummer **und** Knopf mit „(1 Positionen frei)"; Klick legt einen zweiten
  Auftrag nur mit dieser Position an, gelbe Meldung „… Positionen bereits in BA-… vergeben, nicht übernommen".
- **TS-84.3 — Doppelvergabe/manipuliert (AK 2).** Knopf in einem zweiten Browser-Tab (Seite vor TS-84.1 geladen) erneut
  absenden. **Erwartet:** zurück in der Vorschlagsliste, gelbe Sammelmeldung „Alle … Positionen … bereits vergeben" mit
  Positionen und Auftragsnummer; kein neuer Auftrag.
- **TS-84.4 — Entwurf bearbeiten (Anforderung 4, AK 6).** Dienstleister überschreiben, RAL ändern, Start leeren, Retour
  setzen, eine Menge auf `2,5` und eine auf `3.25` setzen, speichern. **Erwartet:** „Entwurf gespeichert", Werte wie
  eingegeben (Mengen 2,5 und 3,25), leeres Datum bleibt leer; unter dem Dienstleister „Sage (PPS-View): …". Negativ:
  Menge `abc` → gelbe Meldung, **nichts** gespeichert.
- **TS-84.5 — Positionen hinzufügen, Erst-Entwurf (AK 6c).** Im Entwurf eine Position entfernen → sie erscheint unter
  **„Positionen hinzufügen"** (nichts angehakt) → anhaken, „Ausgewählte hinzufügen" → wieder im Auftrag mit Sollmenge,
  grüne Meldung mit Anzahl. Ohne freie Position steht „Keine freien beschichteten Positionen für diesen HauptFA". Ohne
  Haken absenden → gelbe Meldung. Der zweite Auftrag aus TS-84.2 zeigt Positionen des ersten **nicht**.
- **TS-84.6 — Dienstleister-Pflicht (AK 6b).** Dienstleister leeren, speichern (gelingt; gelber Hinweis „Dienstleister
  fehlt — zum Übergeben erforderlich") → „Auf „Übergeben" setzen" → gelbe Meldung „Dienstleister fehlt …", Status bleibt
  *Entwurf*, kein Druckknopf. Dienstleister eintragen, speichern → Übergeben gelingt.
- **TS-84.7 — Nach Übergeben (AK 4, 6a, 7a).** Statusverlauf zeigt *Entwurf angelegt* und *Übergeben* mit Zeit + Benutzer,
  *Erledigt* „ausstehend". Kopf und Positionen sind nur noch Text (keine Eingabefelder, kein „Positionen hinzufügen"),
  Bemerkung bleibt speicherbar. Vorher, im Entwurf: **kein** Drucken/PDF; direkter Aufruf
  `/Beschichtungsauftraege/Print/<id>` bzw. `/Beschichtungsauftraege/Pdf/<id>` → zurück auf das Detail mit „Im Entwurf
  nicht druckbar — erst übergeben.".
- **TS-84.8 — Druck und PDF (AK 7).** Übergebenen Auftrag drucken und als PDF laden. **Erwartet:** Briefkopf aus Teil 2
  (ab v1.59.0 mit Fußbereich in der Vorlage zusätzlich Fußzeile auf jeder Seite und „Seite X von Y", TS-92),
  Empfänger = Dienstleister **des Auftrags** (auch wenn in Sage ein anderer steht), Dokument „Beschichtungsauftrag BA-… —
  HauptFA …", Barcode scannt als Auftragsnummer, Kopf mit RAL/Start/Retour/Bemerkung, Positionen **je Artnr summiert**
  mit Maßen und Spalte *Positionen*, bei abweichenden Maßen „Maße uneinheitlich". PDF-Dateiname
  `Beschichtungsauftrag_BA-…_JJJJMMTT-HHMM.pdf`. Eine im Detail per Zahnrad ausgeblendete Spalte (z. B. *Tiefe*) fehlt
  auch im Druck. Ohne Edge auf dem Server fehlt der PDF-Knopf; ein PDF-Fehler meldet gelb und bleibt im Detail.
- **TS-84.9 — Reklamation (AK 5, 5a, 5b).** Auftrag auf *Erledigt* setzen → **„Reklamation"** → Auswahl ohne Haken;
  ohne Haken absenden → gelbe Meldung, kein Auftrag. Zwei Teile wählen → neuer Entwurf mit genau diesen Teilen,
  Dienstleister/RAL/Start/Retour **wie der Ursprung** (Vorbedingung: im Ursprung vorher einen vom Sage-Wert abweichenden
  Dienstleister eingetragen — der Folgeauftrag trägt diesen). Kopf im Folgeauftrag änderbar. Ursprung zeigt
  „Reklamiert → BA-…", Folgeauftrag „Reklamation zu BA-…", beide verlinkt; die Liste zeigt beides in den Spalten
  *Ursprungsauftrag*/*Reklamiert*. Negativ: in *Entwurf*/*Übergeben*/*Storniert* kein Knopf;
  `/Beschichtungsauftraege/Reklamation/<id>` direkt → gelbe Meldung, zurück auf das Detail.
- **TS-84.10 — Positionen im Reklamations-Entwurf (AK 6d).** Reklamation mit einer von drei Ursprungspositionen →
  Bereich **„Positionen des Ursprungs BA-… hinzufügen"** zeigt genau die zwei anderen (Menge/Maße des Ursprungs), keine
  freien HauptFA-Positionen → eine hinzufügen → verschwindet aus der Auswahl; alle hinzugefügt → „Alle Positionen des
  Ursprungs sind bereits enthalten".
- **TS-84.11 — Mehrfach-/Ketten-Reklamation (AK 5c).** Zweite Reklamation derselben Position, während die erste
  *Übergeben* ist → gelingt, Ursprung zeigt beide. Erste Reklamation erledigen → deren „Reklamation" → Folgeauftrag mit
  Verweis auf die Reklamation. Vorschlagsliste: Gruppenkopf nennt Ursprung und Reklamationen, der Anlege-Knopf übernimmt
  diese Position nicht.
- **TS-84.12 — Storno.** Entwurf → „Stornieren" (Rückfrage bestätigen) → Status *Storniert*, Verlauf zeigt Storno mit
  Zeit + Benutzer, kein Druck; die Positionen sind in der Vorschlagsliste wieder frei (Knopf erscheint).
- **TS-84.13 — Parallelität (AK 10, schließt TS-83.6 ab).** Zwei Browser (S), derselbe Entwurf: in A speichern, in B
  (Seite vorher geladen) Status wechseln → B erhält „… wurde inzwischen von jemand anderem geändert — bitte neu laden",
  nichts geändert. Zwei Entwürfe desselben HauptFA mit derselben freien Position in „Positionen hinzufügen": in A
  hinzufügen, dann in B → B erhält Klartext („vergeben" bzw. „bitte erneut versuchen"), **keine** Fehlerseite. Zwei
  Browser klicken gleichzeitig „Aus Vorschlagsliste erstellen" für denselben HauptFA → nur einer übernimmt die Positionen.
- **TS-84.14 — Rechte und Schalter (AK 8, 9, 11).** L sieht Menüpunkt ~~*Beschichtungsaufträge*~~ (seit v1.56.0:
  *Beschichtung → Aufträge*), Liste, Detail, Druck,
  PDF, aber **keine** Schreib-Knöpfe (kein „Aus Vorschlagsliste erstellen", keine Eingabefelder, keine Status-/
  Reklamations-Knöpfe); ein direkter POST (z. B. per Entwickler-Werkzeug nachgebautes Formular) endet auf AccessDenied.
  Benutzer ohne beide Rollen: kein Menüpunkt, `/Beschichtungsauftraege` → AccessDenied. Schalter aus → beide Menüpunkte
  (seit v1.56.0: das Menü *Beschichtung*) weg, alle Seiten gesperrt. `/Users/RoleOverview` listet `beschichtungsauftrag_schreiben`.
- **TS-84.15 — Liste (AK 3).** `/Beschichtungsauftraege`: Statusfilter (Filterkarte), Spaltenfilter je Spalte inkl.
  *Start*/*Retour* (Text `11.2026`) und *Status* (`übergeben`), Pagination bei > 25 Aufträgen, neueste zuerst; Zahnrad
  blendet Spalten aus und merkt sie sich.
- **TS-84.16 — Regression (AK 12).** Vorschlagsliste (Liste und Summiert), deren Druck und PDF (TS-60, TS-74, TS-81,
  TS-82) unverändert bis auf Gruppenkopf und Badge; L sieht dort keinen Anlege-Knopf.

## TS-85 — IDEAL: Arbeitsgänge als Symbole im FA-Struktur-Baum (Teil 1, v1.52.0)

Spec: `2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec`. Reine Anzeige, keine Migration, kein Status.
**Vorbedingungen:** IDEAL-Testsystem mit synchronisierter FA-Struktur (`FaHierarchyNodes` mit `Arbeitsschritte`),
Werkbank-Katalog mit `ArbeitsschrittCode` (Seite Werkbänke), Benutzer mit Kommissionierung, Tracking oder Leitstand.
Vorab **Prüfschritt P-K1 der Spec** (WMS-Abfrage, ob `Arbeitsschritte` der Sage-Spalte „Arbeitsschritte" entspricht) —
Ergebnis notieren: ______. Automatisiert: `FaArbeitsschrittSymboleTests` (Tokenisierung wie die Struktur-Erkennung,
Feld-Reihenfolge, leeres Feld, Ausschluss-/unbekannte Kürzel bleiben, Namens-Lookup getrimmt/case-insensitiv,
mehrdeutig ohne Namen, je Zeile nur eigene Kürzel inkl. Blatt), `FaHierarchyStrukturSpaltenTests` (Kopf, Filterzeile,
Knotenzeile, column-config, `ColumnDefinitions`, colspan, `data-f-operations`).

- **TS-85.1 — Symbole sichtbar (AK 1, 2, 3).** `/FaHierarchy`, Struktur mit `KA`/`SW` in den Arbeitsschritten aufklappen.
  **Erwartet:** Spalte *Arbeitsgänge* (rechts neben *Arbeitsbereich*) zeigt je Zeile die Kürzel als gleich aussehende
  Umriss-Kästchen in Feld-Reihenfolge; Mauszeiger auf ein Kürzel zeigt „KA – <Werkbank-Name>". Keine Farbe, kein
  Status-Zeichen.
- **TS-85.2 — Abgleich mit Sage (AK 1, 13).** Struktur `KTZ-L3-76-5Z1T RAL` im WMS und in Sage „Fertigungsaufträge
  bearbeiten" nebeneinander: je Zeile dieselben Kürzel (Stichproben A-BODEN `LS KA PG`, FR-4301-…-1530 `ZS`,
  Mittelholmblende `VM`, Korpus-Gepunktet `BE`); Blattzeilen tragen ihre eigenen Kürzel; die Sub-FA-Zeile *nicht*
  die Kürzel ihrer Kinder (Korpus-1320-660-760 nur `PG`); Wurzel ohne Kürzel → Strich.
- **TS-85.3 — Ausschlussliste und unbekannte Kürzel (AK 12).** Zeile mit einem Kürzel der Ausschlussliste
  (Service-Einstellung `Sync:ProductionWorkplaceSyncAusschlussliste`, z. B. `STO`) bzw. einem Kürzel ohne Werkbank
  suchen (Prüfabfrage P-K2 b der Spec) → Kürzel erscheint wie jedes andere; Tooltip/Ansage nur das Kürzel, kein Name,
  keine Fehlermeldung.
- **TS-85.4 — Name per Tastatur/Screenreader (AK 2).** Mit Tab in die Zeile → jedes Kürzel bekommt einen sichtbaren
  blauen Fokusrahmen; Screenreader (NVDA/Sprachausgabe) sagt „KA – <Name>". Touch-Terminal: Antippen löst keinen
  Fehler aus (Namensanzeige beim Antippen kommt später).
- **TS-85.5 — Viele Kürzel (AK 6).** Zeile mit ≥ 8 Kürzeln (oder Fenster schmal ziehen) → Kürzel brechen in der Zelle um,
  keine waagrechte Scrollleiste der Zeile.
- **TS-85.6 — Kontrast (AK 4).** Am Fertigungsterminal: Kürzeltext und Rahmen gut lesbar, auch in einer
  mit der Maus überfahrenen (grauen) und in einer per Suchfeld hervorgehobenen (blau/gelb) Zeile. Erwartet: Text ≥ 4,5:1,
  Rahmen ≥ 3:1 (Werte laut Stylesheet 16:1 bzw. ≥ 7:1).
- **TS-85.7 — Filter und Spaltenauswahl (AK 9).** Filterzeile *Arbeitsgänge* `ka` → nur Zeilen mit `KA` samt Pfad; Zahnrad →
  Spalte aus-/einblenden und verschieben, nach Neuladen bleibt die Einstellung (Prefs-API antwortet 200).
- **TS-85.8 — Modal unverändert (AK 5, 10).** Knopf *Arbeitsgänge* an der Kopfzeile → Fenster wie TS-78 (Zählzeile,
  Strich-Zeilen, Unmatched-Warnung, je AG eine Zeile mit Kürzel/Werkbank/Sage-Nr.).
- **TS-85.9 — Große Seite (AK 8).** Seitengröße „Alle" → Seite lädt in akzeptabler Zeit; im SQL-Profiler je Aufruf genau eine
  Abfrage auf `ProductionWorkplaces` und keine zusätzliche auf `WorkOperations` (nur der bestehende Existenz-Check).
- **TS-85.10 — Negativ (AK 7).** Benutzer ohne Kommissionierung/Tracking/Leitstand → `/FaHierarchy` verweigert; Struktur
  ohne materialisierten Auftrag → Kürzel trotzdem sichtbar, kein Fehler.
- **TS-85.11 — Regression.** TS-78 (Modal) und die übrigen Baum-Spalten/Filter (Arbeitsbereich, Komm.-Ziel, Status)
  unverändert; „Alle auf/zu", Hervorheben-Suchfeld funktionieren wie vorher.

## TS-86 — CSV-Export für alle Tabellen (v1.53.0)

Spec: `2026-10-06-csv-export-alle-tabellen-spec` (Variante A, Erzeugung im Browser). Vorbedingungen: Testsystem AKE
(Master `ProduktionsauftragHierarchisch = false`) und IDEAL (`true`), Excel (Windows, deutsches Gebietsschema),
Hex-/Texteditor (z. B. Notepad++). Vor der Abnahme **Prüfschritt Server-Kultur** (Spec, Abschnitt Deploy): FA-Liste,
Spalte „Stk." — `1,000` ⇒ deutsch, weiter; `1.000` im Sinne von eins ⇒ melden. Automatisiert: `CsvExportCoverageTests`
(Layout bindet ein, jede Tabellen-View nutzt das Layout, paginiert ⇒ Server-Mode, skopierte Iteration, ein
Sortiervergleich, Stücklisten-/Pagination-Attribute, Warntext) und `Kern_NodeSelbstpruefung` (Injection inkl.
Zahlen-Ausnahme, KW-Kürzung, RFC 4180, BOM/CRLF, keine `sep=`-Zeile, Dateiname; braucht Node, sonst übersprungen).

- **TS-86.1 — Grundfall AKE (AK 1, 2).** FA-Liste, Spaltenfilter „Werkbank" setzen, Seitengröße 25, mehrere Seiten →
  Knopf **CSV** neben dem Zahnrad. **Erwartet:** Zeilenzahl im CSV = „von N" der Pagination, Spalten = sichtbare
  Spalten, keine Aktionsspalte. Negativ: Baumansichten (`/FaHierarchy`, Tracking-Baum, OSEON-Tracking) und
  Formular-/Druckseiten haben **keinen** Knopf.
- **TS-86.2 — Spaltenauswahl sofort (AK 3).** Spalte per Zahnrad ausblenden und **ohne** Neuladen exportieren → fehlt
  im CSV; Spalte nach vorn ziehen → CSV in Bildschirm-Reihenfolge.
- **TS-86.3 — IDEAL hierarchisch (AK 6).** FA-Liste, Leitstand, FA-Vervollständigung, FA-Abarbeitungsliste,
  Kommissionierung: erste Spalte „HauptFA", keine Gruppen-Kopfzeilen, Kombigerät ohne Fremdzeilen aus der
  Variantentabelle, jede Zeile gleich viele Spalten.
- **TS-86.4 — IDEAL-Listen (AK 6).** Kommissionierliste (Liste + Summiert), Beschichtung (Liste + Summiert; Gruppenkopf
  mit Aufträgen/„Aus Vorschlagsliste erstellen" erscheint nicht als Zeile), Vormontage (beide), **Beschichtungsaufträge**
  (`/Beschichtungsauftraege`, neu v1.51.0) und die Positionstabelle im Auftragsdetail (Entwurf: Menge = Wert des
  Eingabefelds, Spalte „Entfernen" fehlt). Keine doppelte HauptFA-Spalte; `hauptfa` per Zahnrad aus → fehlt im CSV.
- **TS-86.5 — Format (AK 5, 12).** Datei im Hex-Editor: beginnt mit `EF BB BF`, Trenner `;`, Zeilenende CRLF, keine
  `sep=`-Zeile; Name `<Ansicht>_JJJJ-MM-TT_HHMM.csv`. Doppelklick in Excel: Umlaute (ä ö ü ß „–") korrekt, Spalten
  getrennt, `1.234,500` als Zahl (Summenformel rechnet), Datum ohne KW als Datum erkannt.
- **TS-86.6 — CSV-Injection (AK 7, Negativfall).** Testdatensatz mit Bemerkung `=HYPERLINK("http://x";"klick")`,
  `@SUMME(A1:A2)`, `+43 664 1234567`, `-x` → im CSV mit `'` davor, Excel zeigt Text und führt nichts aus.
- **TS-86.7 — Negative Mengen (AK 7).** Bestand/Bewegung mit negativer Menge (`-5,000`) → ohne `'`, als Zahl erkannt.
- **TS-86.8 — Rechte (AK 8, Negativfall).** Benutzer ohne `stock_read` ruft die Bestandsliste auf → kein Zugriff, kein
  Export. Benutzer mit Leitstand- aber ohne Picking-Recht: FA-Liste und CSV zeigen dieselben Spalten.
- **TS-86.9 — Feature-Gate aus (AK 8).** `FaHierarchyKommissionierlistenAktiv = false` → weder Liste noch Export.
- **TS-86.10 — Kappung (AK 9).** Bewegungshistorie ohne Filter (> 5000), Datum absteigend sortiert, exportieren →
  gelbe Meldung „Export auf 5.000 Treffer begrenzt, bitte Filter eingrenzen. Sortierung gilt nur innerhalb der
  exportierten 5.000." neben dem Knopf, Datei trotzdem da, genau 5000 Zeilen, innerhalb absteigend. Gegenprobe
  < 5000: keine Meldung.
- **TS-86.11 — Fehlerfall (AK 10).** Sitzung ablaufen lassen (bzw. in zweitem Tab abmelden), exportieren → rote Meldung,
  **keine** Datei; während des Abrufs ist der Knopf deaktiviert und zeigt einen Kreisel.
- **TS-86.12 — Client-Mode (AK 18).** Stückliste mit Komm.-Ziel-Filter und einer **zugeklappten** Baugruppe mit
  Treffern darunter → Treffer im CSV; ohne Filter → ganze Stückliste unabhängig vom Klappzustand; Bildschirm
  (Klappzustand, Filter, rekursive Suche) unverändert. Lagerbestellung-Details und Tracking je Werkbank mit
  Spaltenfilter → nur sichtbare Zeilen. Artikelgruppen-Zuordnung und OSEON-Reporting (Arbeitsgang-Übersicht mit
  Zukunftstag-Gruppen) → je Tabelle ein eigener Knopf, alle Zeilen dieser Tabelle.
- **TS-86.13 — Zellwerte (AK 16, 17).** Leitstand: Glas/Fremdbezug/Lack als „Ja"/„Nein", Prio als Zahl;
  erledigte/stornierte Zeile ohne Statusspalte; Lack-T als „Lackierteile erledigt" bzw. „–"; FA-Nr. mit Badge-Text
  („… HauptFA", „… In Sage gelöscht"); Werkbank mit „BG n".
- **TS-86.14 — Stammdaten-Stichprobe.** Artikel, Lagerplätze, Benutzer (Admin), BDE-Stammdaten je Tab.
- **TS-86.15 — Sortierung (AK 15).** Mehrseitige Liste, Datumsspalte absteigend → CSV über alle Treffer absteigend;
  dasselbe mit Zahl und Text. IDEAL-Gruppenliste: je Gruppe sortiert, Gruppenreihenfolge unverändert.
  Default-Sortierung auf ausgeblendete Spalte → trotzdem danach sortiert. **Regression:** Bildschirm-Sortierung (Klick,
  Default, Gruppen, Kombigeräte-Variantentabelle bleibt gefüllt) unverändert.
- **TS-86.17 — Nachbesserung QA (I-1, I-2, M-2).** (a) Lagerbewegungen exportieren → Spalte „Menge" ist enthalten.
  (b) Artikelgruppen-Zuordnung → je Bestellgruppe eine Spalte mit „Ja"/„Nein" je Haken. (c) Lagerbestellung-Details im
  Bearbeitungszustand, Spalte „Fehlteil-Status" → CSV zeigt die Beschriftung der gewählten Option („Fehlteil" bzw. „Wird
  nicht nachgeliefert"), ohne Auswahl leer. (d) BDE-Buchungen → Datum/Uhrzeit als `dd.MM.yyyy HH:mm` ohne KW. (e) Negativ:
  Eine sichtbare Spalte mit Überschrift, die nicht zuordenbar ist (nur durch Fehlkonfiguration möglich, Wächter
  `JedeUeberschriebeneSpalte_HatEinenSchluessel`), bricht mit roter Meldung „Die Spalte … ist nicht zuordenbar" ab — keine
  Datei. (f) Beschichtungsauftrag-Detail im Entwurf → keine Spalte „Entfernen" im CSV.
- **TS-86.16 — Regression AKE (AK 11).** Listen in Darstellung, Filter, Sortierung, Spaltenauswahl und Druck unverändert;
  Zahnrad am alten Platz, CSV-Knopf daneben. *Überholt mit v1.65.0 (TS-98.1): Zahnrad und CSV stehen überall rechts über
  der Tabelle, auch auf den AKE-Listen.*

## TS-87 — Arbeitsgang-Stammdaten: BDE-Stammdaten › Arbeitsgänge (v1.54.0)

Spec: `2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec`. Tabelle `OseonOperationConfigs` → `OperationConfigs`
(Variante U, Migration `20261012140548_RenameOperationConfigsAndAddAudit` / `SQL/95`), Audit-Felder, Pflege im Reiter
„Arbeitsgänge“ (nur Admin, unabhängig von `BdeAktiv`). **Vorbedingungen:** Testsystem mit Admin-, `bde_shiftlead`- und
`masterdata`-Benutzer; ein OSEON-Auftrag mit einem AG-Namen ohne Eintrag (AKE) bzw. eine `WorkOperation` mit Kürzel ohne
Eintrag (IDEAL); `BdeAktiv` umschaltbar; **Prüfschritte P1–P5 der Spec vor dem Deploy** (insbesondere P1: steht
`20260330142035_AddOseonOperationConfig` in der History? Sonst anhalten). Automatisiert: `OperationConfigsMigrationSqlTests`
(Namen in Migration/SQL/95/FreshInstall/Snapshot, keine strukturierte Operation, Batches wortgleich, MigrationId),
`OperationConfigRepositoryTests` (Offset-Lookup case-insensitiv/getrimmt/0 vs. fehlend/kleinste Id, Liste „nicht
konfiguriert“ aus zwei Quellen ohne `01`, Decorator, OSEON-Dictionary case-sensitiv), `OperationConfigsControllerTests`
(Zugriff, Filter, Dubletten, Audit, 302, Begriffe).

- **TS-87.1 — Erreichbar bei BDE aus (AK 1, 4).** `BdeAktiv = false`, Admin → Stammdaten → **Arbeitsgänge** → „BDE
  Stammdaten“ mit genau dem Reiter „Arbeitsgänge“. `/BdeMasterData` → Home mit „BDE ist nicht aktiviert.“.
- **TS-87.2 — Erreichbar bei BDE an (AK 2, 3).** `BdeAktiv = true`: Admin sieht Operatoren, Aktivitäten, Terminals,
  Arbeitsgänge; Wechsel in beide Richtungen, aktiver Reiter markiert. `bde_shiftlead`: drei Reiter, `/OperationConfigs`
  (GET und ein nachgebautes POST) → AccessDenied; ebenso `masterdata`. Nicht-Admins sehen den Menüeintrag nicht.
- **TS-87.3 — Pflege (AK 6, 14).** Kürzel `KA`, Anzeigename „Kanten“, Offset `+3`, Fremdsystem-relevant aus → anlegen
  (grün), ändern, löschen (Rückfrage, grün). Negativ: leeres Kürzel, `ka` bei vorhandenem `KA`, ` KA ` → gelbe Meldung,
  kein Eintrag. Negativ (v1.55.0, FA 4a): Offset-Feld leeren, per Entwicklerwerkzeug `required` entfernen und speichern
  (anlegen **und** ändern) → gelbe Meldung „Offset (Arbeitstage) fehlt – nichts gespeichert.“, kein neuer Eintrag bzw.
  Zeile unverändert (Offset, Anzeigename, Relevanz, `ModifiedAt`); Offset `0` und `-1` speichern weiterhin. Danach `SELECT CreatedBy, CreatedByWindows, ModifiedBy, ModifiedAt FROM dbo.OperationConfigs WHERE
  OperationName = 'KA'` zeigt den Benutzer; Bestandszeile (z. B. `ST`) `CreatedBy = 'system'`, `ModifiedAt = NULL`.
- **TS-87.4 — Nicht konfiguriert (AK 8).** IDEAL-Kürzel ohne Eintrag erscheint mit „WMS-Arbeitsgang“, OSEON-Name mit
  „OSEON“, ein Kürzel aus beiden mit „OSEON + WMS-Arbeitsgang“; `01` erscheint nie; nach Anlegen verschwindet das Kürzel;
  ohne Daten „Alle bekannten Kürzel sind konfiguriert.“. Das Anlege-Dropdown bietet dieselben Kürzel an.
- **TS-87.5 — Alte URL (AK 5).** `/Settings/OperationConfig` → neue Seite (Netzwerk-Reiter: 302). Knopf „Arbeitsgänge
  (BDE-Stammdaten)“ in `/Settings` und der Link im OSEON-Reporting-Banner führen auf die neue Seite.
- **TS-87.6 — Begriffe (AK 7).** Spalte/Schalter „Fremdsystem-relevant“, Feld „Offset (Arbeitstage) zum Bezugstermin“,
  Legende mit Stanztermin (OSEON), FE-Termin (IDEAL), Fertigungstermin (AKE-BDE) und dem Satz, dass Fremdsystem-relevant nur
  für OSEON wirkt; Tracking-Schalter „Fremdsystem-Relevanz“.
- **TS-87.7 — OSEON unverändert (AK 11, 12).** TS-7.9, TS-16.1–16.5 und Szenario 30.8 mit denselben Daten wie vor dem
  Deploy → gleiche Ampeln, gleiche Soll-Termine, gleiche Reporting-Zeilen und Banner.
- **TS-87.8 — Liste (AK 16).** Spaltenfilter Kürzel/Anzeigename/Offset (`+2`)/Relevanz (`nein`) filtern serverseitig,
  Seitengröße „Alle“ und Seitenwechsel funktionieren; CSV-Export liefert alle Spalten mit Überschrift.
- **TS-87.9 — Migration je DB-Form (AK 15, 20, 21, 22; Testsystem, vor dem Produktiv-Deploy).** Drei DBs: (a) EF-migrierte
  Kopie (P1 zeigt `IX_OseonOperationConfigs_OperationName`), (b) Skript-/alte FreshInstall-Kopie (P1 zeigt
  `UQ_OseonOperationConfigs_OperationName`), (c) leere DB per neuem `00_FreshInstall.sql`. (a)/(b) Zeilenanzahl notieren,
  dann je einmal `SQL/95` (zweiter Lauf fehlerfrei, ändert nichts) und einmal nur App-Start (`Migrate()`); (c) nur
  App-Start. **Erwartet:** App startet ohne Migrationsfehler, `/OperationConfigs` zeigt die Einträge, P6 liefert auf allen
  drei DBs dasselbe (Tabelle `OperationConfigs`, Spalte `IsExternalSystemRelevant`, `PK_OperationConfigs`, genau ein
  Unique-Index `IX_OperationConfigs_OperationName`, kein `UQ_`, sechs Audit-Spalten), Zeilenanzahl gleich, Bestand
  `CreatedBy = 'system'`. Negativ: auf einer Wegwerf-Kopie zusätzlich leere `dbo.OperationConfigs` neben
  `OseonOperationConfigs` → `SQL/95` (in SSMS ausgeführt) meldet „… existieren beide - manuell klaeren …“ und
  „ABBRUCH: …“, nichts umbenannt, an der leeren `OperationConfigs` keine Audit-Spalten ergänzt und **keine** History-Zeile
  `20261012140548_RenameOperationConfigsAndAddAudit` (QA I-1); der App-Start bricht mit derselben B1-Meldung ab.
- **TS-87.10 — Negativ.** Spaltenfilter ohne Treffer → Hinweiszeile, kein Fehler; ungültige Id per manipuliertem POST →
  404; keine `OseonWorkOperations`/`WorkOperations` → „Alle bekannten Kürzel sind konfiguriert.“.

## TS-88 — IDEAL: Status je Arbeitsgang-Kürzel im FA-Struktur-Baum (Teil 2, v1.55.0)

Spec: `2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec`. **Vorbedingungen:** Testsystem (nicht produktiv:
`BdeAktiv` ist derzeit an keinem Standort an), `BdeAktiv = true`, IDEAL-Struktur mit materialisierten Sub-FAs und
`WorkOperation`s (`KA`, `SW`, `PG`), Werkbänke `KA`/`SW` BDE-aktiv, `PG` nicht; FE-Termin der Struktur bekannt; Einträge in
BDE-Stammdaten › Arbeitsgänge für `KA` (Offset 1) — `SW` **ohne** Eintrag; Feiertag 01.05. gepflegt. Automatisiert:
`FaArbeitsgangStatusTests` (Status-Tabelle, Zuordnung, W-2/W-3/W-4, Offset, Kombigerät, Gate),
`WorkOperationRepositoryFaStructureTests` (Projektion, erledigt/läuft-Prädikate, OPENJSON),
`OperationConfigsControllerTests` (FA 4a).

- **TS-88.1 — erledigt (AK 1).** Am BDE-Terminal `KA` der Sub-FA mit Menge beenden (Endmeldung) → `/FaHierarchy` neu laden:
  `KA` grün mit Haken, Tab auf das Kürzel → „KA – <Werkbank> – erledigt“. Nur Tracking-Rückmeldung (ohne BDE) → nicht erledigt.
- **TS-88.2 — läuft / pausiert (AK 3).** Buchung auf `SW` starten → blau mit Sanduhr „… – läuft“. Pausieren → wieder grau
  „offen“ (bzw. rot, wenn überfällig).
- **TS-88.3 — überfällig (AK 2, 3, 5).** FE-Termin so wählen, dass Soll < heute: offenes `KA` rot mit Warndreieck
  „… – überfällig (Soll TT.MM.JJJJ)“; laufendes → blau mit rotem Rand, Sanduhr **und** Dreieck „… – läuft, überfällig
  (Soll …)“. Soll = heute → nicht überfällig. Struktur ohne FE-Termin → nie überfällig.
- **TS-88.4 — Storno / Korrektur (AK 4).** Endmeldung stornieren oder `IsFinal` per Korrektur zurücknehmen → nach Neuladen
  wieder offen/überfällig.
- **TS-88.5 — Gate (AK 6).** `PG` (Werkbank nicht BDE-aktiv) bleibt neutral wie v1.52.0, ohne Zeichen. `BdeAktiv = false`
  → alle Kürzel neutral, kein Hinweis „kein BDE-Arbeitsgang“, kein gelber Offset-Hinweis.
- **TS-88.6 — Offset nicht konfiguriert (AK 10a, 10c).** Gelber Hinweis über dem Baum „Für 1 Arbeitsgang-Kürzel ist kein
  Termin-Offset konfiguriert …: SW“; Soll von `SW` = FE-Termin. Nach Anlegen von `SW` (Cache wird beim Speichern geleert,
  also sofort) verschwindet der Hinweis. Mit Tiefen-Cap-Fehler auf derselben Seite sind beide Hinweise sichtbar.
- **TS-88.7 — kein BDE-Arbeitsgang (AK 12, 15 W-2/W-3).** Kürzel `STO` bzw. ein Kürzel einer nicht materialisierten
  Sub-FA → neutral, Tab-Text „… – kein BDE-Arbeitsgang“, nie grün/rot. Blatt in einer Waisen-Struktur → ebenso.
- **TS-88.8 — Kombigerät (AK 10b).** HauptFA mit zwei Kopfzeilen (FE 10.05. und 05.05.) → Soll-Text bezieht sich auf 05.05.;
  Kopfzeile zeigt weiterhin keinen Einzeltermin.
- **TS-88.9 — Mehrfach-Treffer (AK 15 W-4, 17).** Testdaten per SQL: zweite `WorkOperation` `ka` neben `KA` am selben
  Auftrag → Seite lädt, ein Symbol mit zusammengefasstem Status; im Serverprotokoll genau **eine** Warnung „FA-Struktur:
  mehrere Arbeitsgaenge gleichen Kuerzels …“ je Seitenaufruf mit Auftrag, Kürzel, Anzahl, Ids.
- **TS-88.10 — Kontrast / Farbe nie allein (AK 7, 8; Antwort 11).** Graustufen-Screenshot: Haken, Sanduhr, Dreieck, Kreis
  unterscheidbar. Spaltenfilter „Arbeitsgänge“ = `ka`: Treffer amber, Pfadzeilen **kursiv mit grauem Streifen links**, volle
  Schriftstärke (keine Blässe). Kontrastprüfung (z. B. DevTools) in normaler, Hover-, hervorgehobener, Treffer- und
  Kontextzeile: Chip-Text ≥ 10:1, Zeichen/Rand ≥ 4,8:1, Zeilentext ≥ 4,5:1.
- **TS-88.11 — Last (AK 9, 13).** Seitengröße „Alle“ mit BDE an: Seite lädt ohne spürbare Verzögerung gegenüber BDE aus; im
  SQL-Profiler je Aufruf eine `WorkOperations`-Abfrage (OPENJSON) und zwei Status-Abfragen (`BdeBookingQuantities`,
  `BdeBookings`), unabhängig von der Zahl der Strukturen.
- **TS-88.12 — Zuordnung Blatt / Sub-FA / Default-AG (AK 11, 14, 16; QA-Nachtrag).** (a) Blattzeile (ohne eigene Sub-FA) mit
  `KA`, deren Vater-Sub-FA `KA` am Terminal abgeschlossen hat → Blatt-Symbol `KA` grün mit Haken. (b) Sub-FA `S` mit eigenem
  `KA` unter Sub-FA `P`: nur den `KA`-Arbeitsgang von `S` abschließen → `KA` an Zeile `S` grün; Zeile `P` zeigt `KA` nur, wenn
  `P` selbst oder ein Blatt-Kind von `P` `KA` trägt (dann mit dem Status von `P`s eigenem `KA`). (c) Sub-FA mit Default-AG
  `01` (NurFA-Buchung) → kein Symbol `01`. (d) Negativ: alle Einträge in BDE-Stammdaten › Arbeitsgänge fehlen (Testsystem)
  → Seite lädt, Soll = FE-Termin, Sammelhinweis nennt alle Kürzel mit Status, kein Fehler.

## TS-89 — IDEAL: Menü „Beschichtung“ + Kurzanleitungen in der Hilfe (v1.56.0)

Spec: `2026-10-13-menue-beschichtung-und-hilfe-kurzanleitungen-spec`. **Vorbedingungen:** IDEAL-Testsystem, Benutzer **S**
(`beschichtungsauftrag_schreiben`), **L** (nur `beschichtungsauftrag`), **N** (keine der beiden, aber z. B. Kommissionierung),
**A** (`admin`); `FaHierarchyBeschichtungAktiv` umschaltbar. Automatisiert: Quelltext-Wächter in
`BeschichtungsauftraegeViewTests` (Dropdown, Einträge, Active-Ausdruck, keine Alt-Links, Titel, Hilfe-Karte, Kurzanleitungen),
Rollenlogik `CurrentUserServiceRoleTests`. Sichtbarkeit, Active-Zustand und Hilfetexte sind Manual-UAT.

- **TS-89.1 — Dropdown sichtbar (AK 1, 2).** L meldet sich an → ein Menüpunkt „Beschichtung“ zwischen „FA-Struktur“ und
  „Vormontage“, keine Einträge „Beschichtungsauftrag“/„Beschichtungsaufträge“ mehr in der Leiste. Aufklappen → genau zwei
  Einträge **Vorschlagsliste** (`/FaHierarchyBeschichtung/Summiert`) und **Aufträge** (`/Beschichtungsauftraege`), keine
  Trennlinie.
- **TS-89.2 — Active-Markierung (AK 4).** Nacheinander Vorschlagsliste, Umschalter „Liste“ (`/FaHierarchyBeschichtung`),
  Aufträge, ein Auftragsdetail, (S) Reklamationsseite → „Beschichtung“ jeweils markiert. FA-Struktur, FA-Liste →
  „Beschichtung“ nicht markiert.
- **TS-89.3 — Nur Schreibrolle (AK 3).** S sieht dasselbe Dropdown wie L; auf den Seiten zusätzlich die Schreib-Knöpfe
  (Regression TS-84.14).
- **TS-89.4 — Nur Leserolle (AK 3, 5).** L sieht das Dropdown, auf den Seiten keine Schreib-Knöpfe (Regression TS-84.14).
- **TS-89.5 — Negativ: Rolle ohne Rechte (AK 3, 5).** N: kein „Beschichtung“ in der Leiste (auch nicht im Mobil-Menü);
  `/FaHierarchyBeschichtung/Summiert` und `/Beschichtungsauftraege` direkt → AccessDenied.
- **TS-89.6 — Negativ: Schalter aus (AK 3, 5).** A setzt `FaHierarchyBeschichtungAktiv = false` → für A, S, L kein
  „Beschichtung“; Direktaufrufe gesperrt wie bisher. Schalter wieder an → Dropdown zurück.
- **TS-89.7 — AKE (AK 3).** AKE-Testsystem (Schalter aus/nicht gesetzt) → kein Menüpunkt „Beschichtung“, Leiste sonst
  unverändert.
- **TS-89.8 — Bedienung (AK 6).** Nur Tastatur: Tab bis „Beschichtung“, Enter öffnet, Pfeiltasten wählen, Esc schließt.
  Schmales Fenster/Tablet: Hamburger-Menü → „Beschichtung“ aufklappbar, beide Einträge erreichbar. Markierter Toggle sieht
  aus wie bei „Bestellungen“.
- **TS-89.9 — Seitentitel (AK 14).** Vorschlagsliste (Summiert und „Liste“): Überschrift und Browser-Tab
  „Vorschlagsliste Beschichtung (IDEAL)“; „Drucken“ → Druckdokument weiter „Beschichtungsauftrag …“, PDF-Dateiname
  unverändert.
- **TS-89.10 — Hilfe Beschichtung (AK 7–9, 12, 13).** Hilfe → Karte „Beschichtung (IDEAL)“ direkt nach
  „Fertigungsaufträge“ mit Kurzanleitung, Vorschlagsliste, Status, Reklamation, Druck und PDF, Wenn etwas nicht klappt,
  Einrichtung (Admin). Kurzanleitung mit S einmal Schritt für Schritt nachgehen (anlegen bis Erledigt) — Knopf- und
  Menütexte stimmen. Strg+F „Menuepunkt Beschichtungsauftrag“ → kein Treffer; in der Karte „Fertigungsaufträge“ stehen die
  alten Abschnitte v1.48/v1.49/v1.51 nicht mehr. Rollen-Schlüssel und Schalter-Name nur im Absatz „Einrichtung (Admin)“.
- **TS-89.11 — Hilfe Kürzel und CSV (AK 10, 11).** Beide Abschnitte beginnen mit nummerierten Schritten; am Testsystem
  nachvollziehen (FA-Struktur: Kürzel, Tooltip, Filter `ka`, Knopf *Arbeitsgaenge*; FA-Liste: CSV mit Filter und
  ausgeblendeter Spalte). Die Status-Punkte aus v1.55.0 stehen einmal, nicht doppelt.
- **TS-89.12 — Regression.** TS-81.2 (neuer Menüweg), TS-84.14, TS-86.4 (CSV auf beiden Vorschlagslisten und Aufträgen)
  unverändert bis auf den Menüweg; übrige Navigationspunkte (Lager, FA, Kommissionierung, Bestellungen) unverändert.

## TS-90 — BDE-Terminal: offene Aufträge mit Arbeitsgang-Status (FA-Struktur Teil 3, v1.57.0)

Spec: `2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-3-spec`. **Vorab-Prüfschritte (vor der Abnahme, kein UI-Test):**

- **P-T1 (AKE-WMS-DB):** Welche Arbeitsgänge gibt es, ist NurFA an?
  `SELECT wo.OperationNumber, COUNT(*) AS Anzahl, MIN(wo.CreatedBy) AS AngelegtVon, SUM(CASE WHEN wo.IsReported = 0 THEN 1 ELSE 0 END) AS Offen FROM dbo.WorkOperations wo GROUP BY wo.OperationNumber ORDER BY Anzahl DESC;`
  und `SELECT [Key], [Value] FROM dbo.AppSettings WHERE [Key] IN ('BdeAktiv', 'BdeNurFaMeldung');`
  Nur `01` bzw. `BdeNurFaMeldung = true` → an der AKE greift nur der NurFA-Zweig (T9). Ergebnis: ______
- **P-T1 Kollision (AKE-WMS-DB):** OSEON-AG-Namen ↔ AKE-BDE-Kürzel in `dbo.OperationConfigs`:
  `SELECT c.OperationName, c.DueDateOffsetDays, wo.Anzahl FROM dbo.OperationConfigs c JOIN (SELECT OperationNumber, COUNT(*) AS Anzahl FROM dbo.WorkOperations GROUP BY OperationNumber) wo ON UPPER(LTRIM(RTRIM(wo.OperationNumber))) = UPPER(LTRIM(RTRIM(c.OperationName)));`
  Leer → kein Thema. Treffer → **vor der AKE-Abnahme melden** (das Terminal rechnet deren Offset auf den Fertigungstermin). Ergebnis: ______
- **P-T2 (IDEAL-WMS-DB):** `SELECT COUNT(*) AS Auftraege, SUM(CASE WHEN ProductionDate IS NULL THEN 1 ELSE 0 END) AS OhneProductionDate FROM dbo.ProductionOrders WHERE IsDone = 0 AND IsCancelled = 0;` Ergebnis: ______

**Vorbedingungen:** Testsystem mit **`BdeAktiv = true`** (Einstellung — freigabepflichtig, produktiv derzeit aus), Werkbank
`BdeAktiv`, Terminal für den Testbenutzer, zwei Werker A und B (`bde_user`); an der Werkbank ≥ 3 Aufträge mit offenen
Arbeitsgängen: Termin in der Vergangenheit, heute + 5 Werktage, ohne Termin; Arbeitsgang-Stammdaten (`SQL/95` gelaufen) mit
einem Kürzel Offset ≠ 0 und einem ohne Eintrag; Feiertage gepflegt. IDEAL- **und** AKE-Testsystem. Automatisiert:
`BdeTerminalAuftraegeTests` (Termin-Regel, Sortierung, Status, Offset, kein W-4), `BdeApiControllerTests` (Fremdbuchung
sichtbar/eigene ausgeblendet, Termin + überfällig aus ProductionDate + Stammdaten, NurFA-Ausgabe unverändert), Teil-2-Tests
(gemeinsame Regel).

- **TS-90.1 — Gruppierung + Sortierung (AK 1, 3).** Werker A scannen → „2. Arbeitsgang wählen“ zeigt Blöcke je Auftrag (Kopf:
  Auftrag, bei IDEAL `/ Sub-FA`, Bezeichnung, Termin bzw. „ohne Termin“); Reihenfolge: vergangener Termin, künftiger, ohne
  Termin; je Block die Arbeitsgänge dieser Werkbank in Sequenz-Reihenfolge. Darunter „Ungeplante Tätigkeiten“ wie bisher.
- **TS-90.2 — Überfällig / offen (AK 5, 6).** Auftrag mit vergangenem Termin → Knopf rot mit Warndreieck, Screenreader
  „<Kürzel> – <Name> – überfällig (Soll TT.MM.JJJJ)“; künftiger → grau mit leerem Kreis „… – offen“. Termin heute + Offset 0 →
  offen. Kürzel ohne Stammdaten-Eintrag → Soll = Termin, **kein** Hinweis am Terminal.
- **TS-90.3 — Läuft (Antwort 6 = A, AK 2, 7).** Werker B startet einen Arbeitsgang dieser Werkbank → bei Werker A nach
  Neuladen blau mit Sanduhr „… – läuft“; bei B steht er nicht in der Liste, sondern unter „Aktuelle Buchungen“. A wählt ihn →
  „Rüsten“ → Modal „AG ist bereits in Arbeit durch B …“; „Produktion“ → Modal bei `BdeMehrfachBuchungProArbeitsgang` = aus,
  sonst startet eine zweite Buchung. B pausiert → bei A nach Neuladen offen bzw. überfällig.
- **TS-90.4 — Läuft + überfällig.** Arbeitsgang mit vergangenem Soll, von B gestartet → bei A Sanduhr **und** Warndreieck,
  roter Rand, „… – läuft, überfällig (Soll …)“.
- **TS-90.5 — Bedienung (AK 7).** Knopf antippen → Rüsten/Produktion wie bisher; Abschluss mit Menge → Arbeitsgang
  verschwindet, Auftrag verschwindet mit seinem letzten Arbeitsgang. Scan „FA-Nr,AG-Nr“ und Sub-FA-Auswahl unverändert.
- **TS-90.6 — IDEAL-Termin (AK 4).** HauptFA mit einer Kopfzeile → Termin = `FE_Termin` aus `/FaHierarchy`; Kombigerät →
  frühester `FE_Termin`; Kopfzeile ohne `FE_Termin` → `ProductionDate` (Antwort 9), überfällig bezogen darauf; ohne
  `ProductionDate` „ohne Termin“, am Ende. Gegenprobe `/FaHierarchy`: dieselbe Struktur zeigt dort nie „überfällig“ (bewusst).
- **TS-90.7 — AKE (AK 12).** AKE-Testsystem, TS-90.1/.2 → Sortierung und Überfällig nach `ProductionDate` + Offset. Laut P-T1
  nur `01`/NurFA → TS-90.9.
- **TS-90.8 — Barrierefreiheit (AK 5).** Graustufen-Screenshot: Kreis, Dreieck, Sanduhr, Sanduhr + Dreieck unterscheidbar;
  Screenreader liest Kürzel, Name, Status; Kontrast am Terminal: Knopftext ≥ 10:1, Zeichen/Rand ≥ 4,8:1 (Werte wie TS-88.10),
  auch bei Hover/Druck. Knöpfe in Touch-Größe (≥ 60 px hoch).
- **TS-90.9 — NurFA unverändert (AK 8, Antwort 8).** `BdeNurFaMeldung` an → „Produktionsaufträge“ wie vorher (Knöpfe
  „FA — Bezeichnung“, nach `ProductionDate`), keine Blöcke, keine Zeichen; FA-Start wie bisher.
- **TS-90.10 — Viele Aufträge (AK 10).** Werkbank mit ≥ 100 offenen Arbeitsgängen → lädt ohne spürbare Verzögerung, Blöcke
  umbrechen, kein horizontales Scrollen; SQL-Profiler: gegenüber v1.56.0 höchstens drei zusätzliche Abfragen
  (`FaHierarchyOrderInfos`, Feiertage, `OperationConfigs` — die letzten zwei gecacht).
- **TS-90.11 — Negativ (AK 9, 11).** Benutzer ohne BDE-Rolle → `/api/bde/available-operations/<id>` verweigert; Werkbank nicht
  BDE-aktiv → leer; Werkbank ohne offene Arbeitsgänge → „Keine offenen Arbeitsgänge an dieser Werkbank.“; Auftragstext mit
  `<b>x</b>` → als Text sichtbar.

## TS-91 — Spaltenfilter-Suchsyntax (Epic, v1.58.0 + v1.58.1)

Spec: `2026-09-23-suchsyntax-spaltenfilter-erweitern-spec` (Epic, Etappe A + B). Grammatik: `7%` beginnt, `%7` endet, `=x` genau,
`;` oder (Text auch `,`), `!` nicht (alle Teile), `(leer)`; Zahl/Datum zusätzlich `>`, `>=`, `<`, `<=`, `a...b`.
**Vor dem Deploy:** P-1 (gespeicherte Standardfilter, auf AKE **und** IDEAL), P-2 Kollation
`SELECT DATABASEPROPERTYEX('IDEAL_AKE_WMS', 'Collation');` → erwartet `…_CI_…` (sonst unterscheiden die SQL-Wege Groß/Klein —
melden), P-3 Server-Kultur (Zahl mit Nachkommastellen zeigt `2,5`, nicht `2.5`). Automatisiert: `ColumnFilterSyntaxTests`
(gemeinsame Tabelle `IdealAkeWms.Tests/Js/column-filter-cases.json`: C#-Auswerter, SQL-Bauer, Node-Lauf des JS-Kerns),
`ProductionOrderRepositoryTests` (SQL per `ToQueryString`: ESCAPE, `LIKE N'4711%'` ohne `LOWER`, Freitext, InMemory-Syntax),
`FaListePostfilterSyntaxTests`, Quelltext-Wächter.

### TS-91.A — Etappe A: Kern, Hilfe, Referenzliste FA-Liste (v1.58.0)

**Vorbedingung:** Testsystem mit IDEAL-Daten, Master `ProduktionsauftragHierarchisch` an; zusätzlich ein Lauf mit Master aus
(AKE-Flachmodus).

- **TS-91.A1 — FA-Liste Text (AK 1).** Spaltenfilter + ENTER: FA-Nr. `4711%`, `%-1`, `=<eine FA>`, `<FA1>;<FA2>`,
  `!<FA1>;<FA2>`; Kunde/Matchcode/Artikel `(leer)` und `!(leer)`. **Erwartet:** Treffer plausibel, „von N“ der Paginierung
  stimmt, Seitenwechsel behält den Filter.
- **TS-91.A2 — FA-Liste Datum/Zahl (AK 2, 3).** Produktionsdatum `>=01.10.2026`, `01.10.2026...31.10.2026`, `<01.10.2026`;
  KO-Termin ebenso; `kw42`, `10.2026` und Kalender-Klick wie bisher; `kw41;kw42` = beide Wochen, `kw41,kw42` = keine Treffer.
  Prio `>2`, `1...3`, `=1`.
- **TS-91.A3 — Escaping (AK 4, 10).** Spaltenfilter Artikelnummer `A_1` trifft nicht `AX1`; Filterkarte FA-Nr. bzw.
  Artikelnummer `A_1` ebenso nicht. Bezeichnung mit `%` im Inneren (`5%0`) sucht das Zeichen.
- **TS-91.A4 — Ungültig (AK 7, Antwort 6).** Artikelnummer `1...5` → Feld rot, darunter „1...5: Bereich nur in Zahl- und
  Datumsspalten — ignoriert“, Liste ungefiltert; ENTER → nach dem Neuladen aus der URL weiterhin rot. Prio `>abc` → „Zahl nicht
  lesbar“. `>5;4711` in FA-Nr.: rot, aber `4711` wirkt.
- **TS-91.A5 — Client/Server gleich (AK 6).** Lager › Eingehende Listen › Details (Client-Filter): Bezeichnung `=x`, `a;b`,
  `(leer)` setzen, dann Drucken → Druck zeigt dieselben Positionen.
- **TS-91.A6 — Hilfe (AK 8, 9).** Tab bis „?“ (erste Filterzelle), Enter/Leertaste → Popover „Filter-Syntax“, Escape schließt;
  Screenreader liest am Filterfeld den Kurztext (aria-describedby); Kontrast der Tabelle/Hinweise WCAG AA. Hilfeseite
  „Filter-Syntax (seit v1.58.0)“ inkl. Hinweis `kw41;kw42`.
- **TS-91.A7 — Mitgeschaltete Listen (Antwort 4).** Eine Liste mit Server-Filter über `Apply` (z. B. Stammdaten › Lagerplätze)
  und eine Client-Liste (z. B. Beschichtungsauftrag-Detail, Positionen): `=`, `;`, `%`, `(leer)` wirken; Zahl-/Datumsspalten
  dort zählen bis Etappe B als Text (`>5` → rot, ignoriert).
- **TS-91.A8 — Negativ / unverändert.** Alte Eingaben `960,886` und `!960` in Textspalten liefern dasselbe wie vor der
  Änderung; Leitstand: SQL-Spalten (FA-Nr., Kunde, Artikel) verstehen die neue Syntax, seine C#-Spalten (VK–VA, Termine) noch
  die alte (Zwischenzustand bis Etappe B, erwartet).
- **TS-91.A9 — Zwischenzustand sichtbar korrekt (QA I-1/I-2).** Leitstand, BDE-Buchungen, FA-Abarbeitungsliste,
  FA-Vervollständigung, Kommissionierliste: Datumsspalte `>=01.10.2026` → Feld rot „Vergleich nur in Zahl- und Datumsspalten —
  ignoriert“ (diese Listen behandeln Datum bis Etappe B als Text). Stückliste: **kein** „?“-Knopf, keine Rot-Markierung, Filter
  wie bisher (`!(leer)`, Komm.-Ziel) — die Stückliste kommt in Etappe B.

### TS-91.B — Etappe B: alle übrigen Listen, Typmarkierung, Rückbau Zwischenzustand (v1.58.1)

**Vorbedingung:** wie TS-91.A; P-2 (Kollation **der Datenbank und je gefilterter Spalte**, auf beiden Servern) ist
**Deploy-Pflicht**: `SELECT t.name AS Tabelle, c.name AS Spalte, c.collation_name FROM sys.columns c JOIN sys.tables t ON t.object_id
= c.object_id WHERE c.collation_name IS NOT NULL AND c.collation_name NOT LIKE '%[_]CI[_]%';` → erwartet leer (sonst Groß/Klein
in dieser Spalte unterschieden — melden). Automatisiert: `ColumnFilterTypesDriftTests` (je View `<th>`-Typ = Register, jede
Server-Liste reicht ihr Register weiter), `ColumnFilterSyntaxQuelltextTests` (kein `EF.Functions.Like`, keine alte
`Parse`-Signatur, Stückliste/FA-Baum ohne eigene Syntax, kein `data-filter-syntax` mehr), gemeinsame Tabelle (84 Fälle C# + Node),
`StockMovementRepositoryTests` (Datum/Zeit AK 16, Bewegungsart), `WarehouseRequisitionRepositoryTests` (Antwort 5),
`ArticleColumnFilterSqlTests`.

- **TS-91.B1 — Artikelliste + Laufzeit (AK 12).** Stammdaten › Artikel: Artikelnummer `5000%`, `%-1`, `=<Nr>`, Bezeichnung `a_1`
  (findet nur echtes `a_1`). **Laufzeitmessung am Testsystem (vorher/nachher dokumentieren):** In SSMS `SET STATISTICS TIME, IO
  ON;` dann (a) vorher (Stand v1.57): `SELECT COUNT(*) FROM dbo.Articles WHERE ArticleNumber LIKE N'%5000%';` und
  `… WHERE Description LIKE N'%schraube%';` (b) nachher: `… WHERE ArticleNumber LIKE N'%5000%' ESCAPE N'\';` (enthält, gleicher
  Plan erwartet), `… WHERE ArticleNumber LIKE N'5000%' ESCAPE N'\';` (beginnt mit — **Index Seek** auf dem UQ-Index von
  `ArticleNumber` erwartet; im Ausführungsplan prüfen). Dasselbe für die FA-Liste: `SELECT COUNT(*) FROM dbo.ProductionOrders
  WHERE OrderNumber LIKE N'4711%' ESCAPE N'\';` gegen `… LIKE N'%4711%'`. Ergebnis (CPU-/Gesamtzeit, logische Lesevorgänge, Plan):
  ______
- **TS-91.B2 — Bewegungshistorie Datum/Zeit (AK 16).** `01.10.2026`, `10.2026`, `2026` wie bisher; neu `>=01.10.2026`, `>01.10.2026`
  (ab 02.10.), `<=01.10.2026` (bis 01.10. 23:59), `01.10.2026...31.10.2026`, `30.09.2026;02.11.2026`. Bewegungsart `=ausbuchung`
  (nicht „Sage-Ausbuchung“), `!ausbuchung`.
- **TS-91.B3 — BDE-Buchungen (AK 17).** Start `>=01.10.2026`, Ende `<01.10.2026`, Gut `>0`, Ausschuss `=0`; Operator `%huber`, Typ
  und Status wie bisher (Enum-Namen).
- **TS-91.B4 — Fehlteile (Antwort 5).** Artikel-Nr. `!a;b` blendet a **und** b aus (vorher: a UND NICHT b); `a%;=<Nr>`; Matchcode
  aus dem Artikelstamm trifft weiter.
- **TS-91.B5 — Stückliste (AK 14).** Standardfilter Komm.-Ziel „Alle Ziele“ (`!(leer)`) und das Komm.-Ziel-Dropdown wie vorher,
  Vorfahren-Aufklappen, Leerzustand „keine Treffer“; neu Menge `>=10`, `1...5`, Bezeichnung `=x`, `(leer)`; „?“-Knopf vorhanden;
  CSV-Export der gefilterten Stückliste enthält genau die Treffer.
- **TS-91.B6 — FA-Struktur-Baum (AK 15).** Textfilter `=KA` (Arbeitsgänge), `4711%`, `KA;SW`; Auswahlfilter (Arbeitsbereich,
  Komm.-Ziel, Status) unverändert exakt; Kontextzeilen und Zähler „X von Y“ wie vorher; „?“-Knopf in der Filterzeile.
- **TS-91.B7 — Leitstand (M2).** VK-Spalte `=fertig`, `!offen`; Matchcode `%x`; Termine `>=01.10.2026`, Bereich, `kw42`.
- **TS-91.B8 — Zahl-/Datumsspalten übriger Listen (Stichprobe).** Beschichtungsaufträge `start` `>=01.10.2026`, `positionen` `>2`;
  Summiert-Sichten `sollmenge` `>10`; Benutzer/Lagerplätze „Erstellt am“ `<01.01.2026`; Sage-Buchungen „Vers.“ `>1`;
  Lagerbestellungen `#` `>100`; FA-Abarbeitungsliste Stk. `>1` trifft Menge `2` (AK 18). Kalenderknopf in allen markierten
  Datumsspalten.
- **TS-91.B9 — Hilfe und Rückbau.** Stückliste und alle Listen zeigen den „?“-Knopf; erste Spalte per Zahnrad ausblenden → „?“
  wandert in die nächste sichtbare Filterzelle (auch nach Neuladen). Datumsvergleich in Leitstand/BDE-Buchungen wird nicht mehr
  rot (jetzt typisiert). Hilfeseite: Hinweis „oder mit ;“.
- **TS-91.B10 — Negativ.** P-1-Treffer (falls vorhanden) vorher/nachher prüfen; alte Eingaben `960,886` und `!960` in
  Textspalten unverändert; in Zahlspalten `960,886` = Dezimalzahl (kein ODER mehr) — erwartete Änderung.

---

## TS-92 — IDEAL: Fußzeile nach Word-Vorlage auf dem Beschichtungsauftrag (v1.59.0)

Spec: `2026-10-13-briefkopf-fusszeile-word-vorlage-spec`. **Vorbedingungen:** wie TS-82. Zusätzlich `Branding\` im
Programmordner der Web-App mit dem neuen Muster (Fußbereich `<footer class="briefkopf-fuss">`) und den Schriftdateien
`NimbusSanNov-Reg.otf`/`NimbusSanNov-SemBol.otf`. **Vor dem Deploy** an einem mehrseitigen HauptFA (TS-92.1) die Seitenzahl
von Druck und PDF notieren (für TS-92.6). Kandidaten mit vielen Positionen: Abfrage aus TS-82.3. Automatisiert:
`BriefkopfVorlageServiceTests` (Fußbereich, Fehlerfälle, Muster inkl. Kontrast-Ausnahme), `BriefkopfEinbindungTests`
(Fuß-Partial einmal hinter den Dokumenten, Zählung nur bei einem Dokument). Seitenumbruch und Darstellung sind Manual-UAT;
Headless-Edge-PDF wurde im Dev-Lauf an einer statischen Testseite gemessen (Spec, Abschnitt Umsetzung).

- **TS-92.1 — Bildschirmdruck, ein HauptFA, mehrseitig (AK 1, 3, 4).** „Liste" → HauptFA mit ≥ 40 Positionen →
  „Drucken" → Druckvorschau (Edge, Ränder „Standard"; zusätzlich Chrome). **Erwartet:** Kopf nur Seite 1, Fußzeile auf
  jeder Seite unten, letzte Zeile jeder Seite vollständig über der Fußzeile, nächste Zeile oben auf der Folgeseite;
  unten rechts „Seite X von Y".
- **TS-92.2 — Sammeldruck, mehrere HauptFA (AK 1, 4, 11, 16).** Summiert ohne HauptFA-Filter (≥ 3 HauptFA, einer davon
  mehrseitig) → „Drucken". **Erwartet:** jeder HauptFA auf neuer Seite mit Kopf, Fußzeile auf allen Seiten, **keine**
  Seitenzählung, keine leere letzte Seite. Quelltext (Strg+U): `briefkopf-fussbereich` genau einmal, kein `counter(pages)`.
- **TS-92.3 — PDF (AK 2, 3, 16).** PDF-Knopf am mehrseitigen HauptFA (Liste und Summiert) und PDF eines übergebenen
  Auftrags. **Erwartet:** wie TS-92.1, „Seite 1 von N" … „Seite N von N"; PDF → Eigenschaften → Schriftarten: Nimbus Sans
  Novus eingebettet.
- **TS-92.4 — Lange Positionsliste, Grenzzeile (AK 3).** HauptFA aus TS-92.1 und ein HauptFA mit Bemerkung bzw. gelbem
  Banner unter der Tabelle: an jedem Seitenübergang die letzte Zeile am Ausdruck mit Lineal prüfen. **Erwartet:** ≥ 2 mm
  Abstand zur Fußzeile, keine halbe Zeile, keine Zeile doppelt oder fehlend (Zeilenzahl am Ausdruck = am Bildschirm);
  Bemerkung/Banner nie unter der Fußzeile.
- **TS-92.5 — Inhalt und CD (AK 5, 6).** Am Ausdruck: links IDEAL-Block (Name IDEAL Blau, semibold), Mitte AKE-Block (Name
  AKE Blau `#43a6e2`, semibold — die einzige Stelle mit AKE Blau als Schrift), rechts `www.ideal-ake.at`, beide Mails
  `office@ideal-ake.at`, darunter Balken zweifarbig mit 45°-Übergang **innerhalb** der Seitenränder. Zusätzlich drucken mit
  **ausgeschaltetem** „Hintergrundgrafiken": Der Balken bleibt sichtbar.
- **TS-92.6 — Vorlage ohne Fußbereich (AK 7).** `<footer class="briefkopf-fuss">…</footer>` aus der Vorlage löschen,
  TS-92.1 wiederholen. **Erwartet:** keine Fußzeile, keine Seitenzählung, kein gelber Hinweis, gleiche Seitenzahl wie vor
  dem Deploy, Quelltext ohne `@page`.
- **TS-92.7 — Ersatz-Kopf (AK 8).** Pfad in den Standorteinstellungen leeren. **Erwartet:** Ersatz-Kopf, keine Fußzeile,
  gelber Hinweis nur am Bildschirm. Pfad wiederherstellen.
- **TS-92.8 — Fehler im Fußbereich (AK 9, 10).** Nacheinander im Fuß: (a) `<img src="https://www.ideal-ake.at/x.png">`,
  (b) `<img src="fehlt.png">`, (c) `<script>alert(1)</script>`, (d) `</footer>` löschen, (e) ein überzähliges `</div>`
  im Fuß, (f) den Fußbereich verdoppeln. **Erwartet:** (a)/(b) Fuß ohne Bild + Hinweis „abgelehnt" bzw. „nicht
  gefunden"; (c) kein Dialog, Quelltext ohne `script`; (d)/(e) Ersatz-Kopf + Hinweis („unvollständiges Element <footer…" bzw.
  „(Fußzeile): ungleich viele <div>"), alle HauptFA vollständig; (f) eine Fußzeile + Hinweis „2 Fußzeilen-Bereiche". Im
  Ausdruck/PDF nie ein Hinweis. Danach wiederherstellen.
- **TS-92.9 — Am Bildschirm (AK 12).** Druckansicht öffnen, nicht drucken. **Erwartet:** Fußzeile einmal am Ende, nichts
  überdeckt, beim Scrollen kein schwebender Balken.
- **TS-92.10 — Regression Kommissionierliste (AK 13).** Kommissionierliste Liste und Summiert drucken. **Erwartet:** kein
  Kopf, keine Fußzeile, Seitenränder wie bisher.
- **TS-92.11 — Ablage nach Deploy (AK 15, E5).** Nach dem Publish auf das Testsystem prüfen, dass
  `<Programmordner>\Branding\briefkopf.html` noch existiert und `https://<host>/Branding/briefkopf.html` **nicht**
  ausgeliefert wird (404).
- **TS-92.12 — Seitenzählung je Dokument (AK 16).** PDF eines mehrseitigen HauptFA: „Seite 1 von N" … „Seite N von N".
  Sammeldruck mit drei HauptFA: keine Zählung (TS-92.2). Summiert gefiltert auf einen HauptFA: korrekte Zählung.
  Auftragsdruck (`Beschichtungsaufträge` → Drucken): korrekte Zählung.
- **TS-92.13 — Edge-Version Server (P0).** Auf dem Webserver `msedge.exe` → Eigenschaften → Details: Version notieren
  (Dev-Messung: 155.0.4283.45). Bei deutlich älterer Version TS-92.3 besonders genau prüfen. Ergebnis: ______

---

## TS-93 — IDEAL: Sage-Quellen auf die WMS-Views umstellen (v1.60.0, Fixes v1.60.1/v1.60.2)

Spec: `2026-10-14-ideal-sage-views-umstellung-spec`. **Vorbedingungen:** IDEAL-Testsystem, Master
`ProduktionsauftragHierarchisch = true`, `Sync:HierarchicalFaEnabled = true`, `Sync:ProductionWorkplaceSyncEnabled = true`,
`Sync:ArticlesEnabled = true`. Auf der **Sage-Test-DB** sind P1 (inkl. P1b), P2–P4 und P5b ausgeführt und protokolliert
(Prüfabfragen in der Spec; die Views existieren dort). DB-Backup der WMS-Test-DB liegt vor; P6 „vorher“ und P7 sind
ausgeführt. Automatisiert: `FaHierarchySqlTests` (neue Namen), `SageArticleSqlTests` (leer = bisheriger Select, View-Select
mit gleichen Aliasen), `ProductionWorkplaceSyncServiceTests` (Default-Quelle, Quelle fehlt = Skip), Drift-Guard
`ServiceSettingDefinitionsTests`. Die Sage-Reads und das Einmal-Skript sind Manual-UAT.

- **TS-93.1 — FA-Views per Einstellung (AK 1, Antwort 9).** Nach P2 = 0 (auch `Artnr`): in `/StandortEinstellungen`
  FAListe = `[vw_IDEAL_AKE_WMS_FAListe]`, FAInfos = `[vw_IDEAL_AKE_WMS_FAInfos]`, nächsten Zyklus abwarten.
  **Erwartet:** Lauf `FaHierarchy` erfolgreich, `neu` in der Größenordnung des Laufs davor; Stichprobe einer FA in
  `/FaHierarchy` und in der Kommissionierliste unverändert.
- **TS-93.2 — Bestehender Wert bleibt (AK 2).** Vor dem Deploy die Werte der FA-Keys notieren, deployen, Web neu
  starten. **Erwartet:** Werte unverändert; die neuen Keys `Sync:ProductionWorkplaceSyncViewName` (`KHKPpsArbeitsplaetze`)
  und `Sync:ArticlesViewName` (leer) sind angelegt und in der Standort-Maske sichtbar.
- **TS-93.3 — Arbeitsplätze (AK 5, 6).** Nach dem Deploy Zyklus abwarten. **Erwartet:** Lauf `ProductionWorkplaceSync`
  erfolgreich mit denselben Counts wie davor. Dann (P3b = 0, siehe TS-93.3b) Key auf `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]`, Zyklus abwarten.
  **Erwartet:** erfolgreich, `neu = 0`, `abweichung = 0`, Werkbank-Liste unverändert.
- **TS-93.3b — Arbeitsplätze aus der View mit deren Spalten (Fix v1.60.2).** Befund v1.60.1: Lauf
  `ProductionWorkplaceSync` scheiterte mit „Ungültiger Spaltenname Mandant/Aktiv/Arbeitsplatznummer/…“, weil die View
  laut `sys.columns` nur `Arbeitsgangnummer`, `Bezeichnung`, `AG` liefert. Vorbedingung **P3b** (View gegen
  `KHKPpsArbeitsplaetze` mit Mandant/`Aktiv = -1` per `EXCEPT` in beide Richtungen) = leer. Mit v1.60.2 Key auf
  `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]`, Zyklus abwarten. **Erwartet:** Lauf erfolgreich, `neu = 0`, `abweichung = 0`,
  Werkbank-Liste unverändert (Nummern mit führenden Nullen wie `0000` unverändert). **Negativ:** Key zurück auf
  `KHKPpsArbeitsplaetze` (auch `[dbo].[KHKPpsArbeitsplaetze]`) → Lauf mit Mandant/Aktiv-Filter wie vor v1.60.0, Counts
  wie vorher.
- **TS-93.4 — Artikel aus der View (AK 9, 11).** `Sync:ArticlesViewName = [vw_IDEAL_AKE_WMS_Artikel]`, Zyklus abwarten.
  **Erwartet:** Lauf `Article` erfolgreich, `gelesen` ≈ P4d; Stichprobe 5 Artikel in `/Articles` gegen Sage
  (Bezeichnung, Einheit, Artikelgruppe **als Code**, Hauptlagerplatz, Matchcode). P6 „nachher“: keine neuen 00er,
  keine neuen Dubletten. Fehlt eine Spalte (Spaltennamen unverifiziert bis P4): Lauf schlägt fehl + Fehlermail →
  Befund an die Entwicklung, Key leeren.
- **TS-93.4b — View mit WMS-Spaltennamen (Fix v1.60.1).** Befund v1.60.0: Lauf `Article` scheiterte mit „Ungültiger
  Spaltenname Artikelnummer/Bezeichnung1/…“, weil die View laut P4 (2026-10-15) bereits `ArticleNumber, Description,
  Matchcode, Unit, ArticleGroup, ReorderLevel, PrimaryStorageLocation` liefert. Mit v1.60.1 TS-93.4 wiederholen.
  **Erwartet:** Lauf `Article` erfolgreich (kein „Ungültiger Spaltenname“), `gelesen` ≈ P4d, Stichprobe wie TS-93.4
  (Artikelgruppe als reiner Code, P4b; keine Dubletten je `ArticleNumber`, P4a). **Negativ:** AKE (Key leer) — Lauf
  `Article` unverändert mit dem bisherigen Select.
- **TS-93.5 — Folgeprozesse (Risiko Nummernformat).** Kommissionierung an einem HauptFA mit 2 Positionen umbuchen.
  **Erwartet:** je Position eine `StockMovement` (kein stiller Skip). Eine Lagerbuchung erzeugen → Sage-Lagerbuchung
  erfolgreich. Lagerbestand-Sync ohne neue Warnungen „Artikel … nicht im WMS“.
- **TS-93.6 — Negativ: Artikel-/Arbeitsplatz-Quelle fehlt (AK 7, 12).** `Sync:ArticlesViewName = [vw_GibtEsNicht]`.
  **Erwartet:** Lauf `Article` mit Warnung „Sage-Quelle [vw_GibtEsNicht] nicht vorhanden oder keine Leseberechtigung —
  Sync uebersprungen“, Suffix „Quelle nicht vorhanden“, **keine** Fehlermail, `Articles`-Anzahl unverändert, andere
  Syncs im Zyklus laufen. Dasselbe mit `Sync:ProductionWorkplaceSyncViewName = [vw_GibtEsNicht]` (keine Mail,
  `ProductionWorkplaces` unverändert). Keys danach zurücksetzen.
- **TS-93.7 — Negativ: FA-View fehlt (AK 3).** FAListe-Key = `[vw_GibtEsNicht]` (danach dasselbe mit FAInfos).
  **Erwartet:** Lauf `FaHierarchy` **fehlgeschlagen** mit „… nicht vorhanden oder keine Leseberechtigung — FA-Struktur-Sync
  abgebrochen“, **Fehlermail** (bei `ErrorNotification:Enabled`), Zeilenzahl `FaHierarchyNodes`/`FaHierarchyOrderInfos`
  unverändert, Service läuft weiter. Key zurücksetzen.
- **TS-93.8 — Negativ: ungültiger Name (AK 4, 7, 12).** `Sync:ArticlesViewName = vw_x;DROP`. **Erwartet:** Lauf als
  Fehler, Fehlermail, kein SQL. Dasselbe für den Arbeitsplatz-Key.
- **TS-93.9 — Rückweg ohne Deploy.** `Sync:ArticlesViewName` leeren → nächster Lauf nutzt den alten Select (`gelesen`
  wie vor der Umstellung); Arbeitsplatz-Key zurück auf `KHKPpsArbeitsplaetze` → Counts wie vorher. **Achtung:** den
  Artikel-Rückweg erst **nach** TS-93.10–93.12 testen oder danach den View-Namen wieder setzen und einen Sync abwarten,
  sonst holt der alte Select die 00er zurück.
- **TS-93.10 — AKE unberührt (AK 8, 10; AKE-Testsystem).** Nach dem Deploy ohne Einstellungsänderung. **Erwartet:** Lauf
  `Article` mit Counts wie zuvor, `Sync:ArticlesViewName` leer, kein Lauf `ProductionWorkplaceSync`, kein Lauf `FaHierarchy`.
- **TS-93.11 — 00er-Bereinigung, Liste (AK 16, 17).** Vorbedingung TS-93.4 (View gesetzt, Artikel-Sync danach
  erfolgreich). Testdaten: vier 00er-Zeilen ohne Sage-Bezug wählen/anlegen und je **eine** Referenz geben (eine
  Bewegung, ein Artikel-Merkmal, eine `FaWorkStepSpec`, ein `PickingItem` mit dieser `BomArticleNumber`); `@Behalten` mit
  dem P5b-Ergebnis füllen. Skript `SQL/Einmalig/2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel.sql` mit
  `@Loeschen = 0`. **Erwartet:** `Articles`-Anzahl unverändert; die vier Zeilen unter „referenziert“ mit der richtigen
  Zählspalte; Zählzeile `Kandidaten` = P6-Anzahl00 minus `@Behalten`-Treffer. Bestätigung der Liste „loeschbar“ im
  Protokoll vermerken.
- **TS-93.12 — 00er-Bereinigung, Löschen (AK 18).** DB-Backup, Skript mit `@Loeschen = 1`. **Erwartet:** `Geloescht` =
  `Gesichert` = „loeschbar“ aus TS-93.11 = Zeilen in `dbo.Articles_Bereinigung00er_Sicherung`; die vier referenzierten
  Zeilen samt Merkmal/Spezifikation unverändert; nächster Artikel-Sync legt keine gelöschte Nummer neu an (P6 „nach
  Bereinigung“). Kommissionierung des `PickingItem`-Testfalls bucht weiter. Zweiter Lauf mit `@Loeschen = 1`: Abbruch
  „…Sicherung existiert bereits…“, keine Änderung.
- **TS-93.13 — Negativ: Guards und Rückweg (AK 19).** (a) AKE-Testsystem: Skript ausführen → Abbruch „kein
  IDEAL-System“, keine Änderung. (b) IDEAL-Test, `Sync:ArticlesViewName` kurz leeren → Abbruch mit Reihenfolge-Hinweis;
  Key sofort wieder setzen (vor dem nächsten Zyklus). (c) Rückweg-Block ausführen → gelöschte Zeilen mit derselben `Id`
  zurück (`COUNT` = Sicherung). Danach Sicherungstabelle droppen und TS-93.12 wiederholen oder Testsystem aus dem
  Backup herstellen.

---

## TS-94 — Arbeitsgang-Stammdaten mit der Sage-View abgleichen (v1.61.0)

Spec: `2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view-spec`. **Vorbedingungen:** Bündel-Stand v1.61.0 auf dem
Testsystem (Web + Dienst + `SQL/96` bzw. Migration beim Web-Start); Admin-Login; Zugriff auf `/ServiceSettings`,
Standorteinstellungen, Aktivitäts-Protokoll (Dienst `OperationConfigSync`). Prüfabfragen P1–P5 stehen in der Spec
(P1/P2 nur Sage, P3/P4/P5 WMS-DB); **P5 vor dem Deploy** je Standort ausführen und protokollieren: P5a auf Systemen nach
`SQL/95` (`dbo.OperationConfigs`, `CreatedBy = 'system'` → künftig Fremdsystem), P5b auf Live vor `SQL/95`
(`dbo.OseonOperationConfigs` — jede Zeile wird Fremdsystem). Automatisiert: `OperationConfigSyncServiceTests` (Anlage,
Ausschluss, Mehrdeutigkeit, Kennzeichnung, Quelle-Wechsel, gelöschter Eintrag während des Laufs, Quelle fehlt/leer, DryRun),
`OperationConfigRepositoryTests.UpdateAsync_LaesstSourceUndSageMissingSinceUnveraendert` (rot gegen den alten Code),
`OperationConfigsControllerTests` (Kennzeichnung nur bei Schalter an, nie Fremdsystem, Spaltenfilter Quelle),
`OperationConfigsViewTests` (Zahnrad-Drift), `OperationConfigSourceMigrationSqlTests` (Bestandsbefüllung wortgleich).
Sage-Read, Settings-DB, Unique-Index, Kollation, Skripte und Fehlermail sind Manual-UAT.

- **TS-94.1 — Deploy mit Defaults (AKE und IDEAL, AK 3/16/19/21).** Web starten. **Erwartet:** `/ServiceSettings` zeigt
  `Sync:OperationConfigSyncEnabled = false`, `Sync:OperationConfigSyncViewName = [vw_IDEAL_AKE_WMS_Arbeitsgaenge]`,
  `Sync:OperationConfigSyncFremdsystemRelevant = false`; zwei Zyklen → kein Lauf `OperationConfigSync`. P4: Altspalten
  unverändert, `Source` je Zeile wie P5 vorhergesagt, `SageMissingSince` überall leer. Reiter *Arbeitsgänge*: Spalte
  „Quelle“, Zahnrad, kein Badge, kein Hinweis.
- **TS-94.2 — Erster Lauf (IDEAL-Test, AK 4).** Schalter an, Zyklus abwarten. **Erwartet:** Lauf `OperationConfigSync`
  erfolgreich, `neu` = P2-Kürzel ohne Bestandszeile (minus Ausschlussliste), Info „angelegt: KA (…), …“. Neue Zeilen: Offset 0,
  Fremdsystem-relevant „Nein“, Quelle „Sage“, Anzeigename aus Sage, `CreatedBy = OperationConfigSync`; `SÄ` korrekt (P4).
- **TS-94.3 — Bestand bleibt (AK 5).** Vorher ein vorhandenes Kürzel per UI auf Offset `+2`, Anzeigename „Test“. Nächster
  Lauf. **Erwartet:** Werte unverändert, `vorhanden` zählt es.
- **TS-94.4 — Ausschluss (AK 6).** Ausschlussliste um ein View-Kürzel ergänzen, das noch nicht angelegt ist (oder vorher
  löschen). **Erwartet:** nicht angelegt, nicht gekennzeichnet, `uebersprungen` +1. Liste zurückstellen.
- **TS-94.5 — Kennzeichnung + Bearbeiten (AK 8/15/22/28).** `ZZTEST` per UI anlegen (P4: Quelle `Manual`). Nächster Lauf,
  Reiter nach bis zu 10 min (Web-Zwischenspeicher). **Erwartet:** Badge „nicht in Sage“ an `ZZTEST` (Datum im Tooltip),
  gelber Hinweis oben listet ihn, Protokoll-Warnung „neu nicht in Sage: ZZTEST“; Folgelauf ohne neue Warnung,
  `nicht_in_sage` stabil. Dann `ZZTEST` bearbeiten (Offset `+1`, speichern). **Erwartet:** Badge bleibt; P4:
  `SageMissingSince` und `Source` unverändert, nur Offset und `Modified*` neu. `ZZTEST` löschen.
- **TS-94.6 — Schalter aus (AK 16).** **Erwartet:** Badge und Hinweis sofort weg, kein Lauf mehr; Spalte „Quelle“ bleibt.
- **TS-94.7 — Spaltenfilter Kürzel (AK 17).** Filter `=SL` (gekennzeichnet oder nicht). **Erwartet:** genau eine Zeile.
- **TS-94.13 — Quelle anzeigen und filtern (AK 22/25).** **Erwartet:** „Sage“ (Abgleich), „Manuell“ (UI), „Fremdsystem“
  (Bestand laut P5); Filter `=Manuell` → nur manuelle, `!Fremdsystem` → alle anderen; CSV-Export enthält „Quelle“ als
  Klartext. Eintrag bearbeiten (Offset) → Quelle unverändert.
- **TS-94.14 — Fremdsystem nie gekennzeichnet (AK 24).** Fremdsystem-Eintrag, dessen Kürzel nicht in der View steht
  (P2/P4); nächster Lauf, Reiter nach bis zu 10 min. **Erwartet:** kein Badge, nicht im Hinweis, nicht in „neu nicht in Sage“.
- **TS-94.15 — Quelle-Wechsel Manuell → Sage (AK 23).** Ein View-Kürzel, das noch nicht existiert (oder nach Löschen), per
  UI anlegen (Quelle „Manuell“). Nächster Lauf (P4 sofort, Reiter nach bis zu 10 min). **Erwartet:** Quelle „Sage“, Info
  „Quelle Manuell → Sage: …“, `quelle_sage` = 1, Offset/Anzeigename wie eingegeben; Folgelauf ohne Meldung. Gegenprobe:
  Fremdsystem-Eintrag `SL`/`ST` (falls vorhanden) bleibt „Fremdsystem“.
- **TS-94.16 — Spaltenauswahl (AK 26).** Zahnrad rechts über der Tabelle; „Quelle“ ausblenden, Reload → bleibt ausgeblendet;
  einblenden, Reihenfolge ändern, Reload → bleibt; „Kürzel“ nicht ausblendbar. Netzwerk: kein 400 auf
  `/api/user-view-preferences/OperationConfigs`.
- **TS-94.17 — Fremdsystem-relevant-Key (AK 27).** Key `true`, ein View-Kürzel löschen, nächster Lauf. **Erwartet:** neu
  angelegt mit Fremdsystem-relevant „Ja“; übrige Einträge unverändert. Key zurück auf `false`.
- **TS-94.18 — Seeds (nur Wegwerf-DB, AK 18/22).** (a) `SQL/00_FreshInstall.sql` auf leerer DB → P4: 12 Zeilen, alle
  `ExternalSystem`, `MigrationId` `20261016090641_AddOperationConfigSourceAndSageMissingSince` in der History. (b) Alle
  `OperationConfigs` löschen, Web starten → 12 Seed-Zeilen mit Quelle „Fremdsystem“.
- **Negativfälle:** **TS-94.8** View-Name `[vw_GibtEsNicht]` → Warnung „… nicht vorhanden oder keine Leseberechtigung — Lauf
  übersprungen“, keine Mail, folgende Sync-Blöcke laufen (AK 10). **TS-94.9** View-Name `vw;DROP` → Fehler + Fehlermail, kein
  Sage-SQL (AK 12). **TS-94.10** `WorkerSettings:SyncDryRun = true` → Counts, keine Änderung (AK 13). **TS-94.11** `SQL/96`
  zweimal → nur `PRINT`, Bestandsbefüllung im zweiten Lauf 0 Zeilen (AK 18). **TS-94.12** nur falls P3 `_AI` zeigt: `SA` per UI,
  dann `SÄ` aus der View → Warnung, `fehler` +1, Lauf fertig (AK 14). **TS-94.19** `SQL/96` auf einer Wegwerf-DB mit
  `OseonOperationConfigs` (vor SQL/95) → Warnung, keine Spalte, **keine** History-Zeile (AK 18). AK 29 (Eintrag während des
  Laufs gelöscht) ist nur per Unit-Test prüfbar.

**Testprotokoll IDEAL-Test** (Reihenfolge: alte Zeilen erst **nach** dem ersten Lauf löschen; Erwartungen je Bestandszeile
nach P5a): P5a `ExternalSystem` → nie gekennzeichnet; P5a `Manual` und Kürzel ∉ P2 → gekennzeichnet; P5a `Manual` und ∈ P2 →
Quelle wechselt auf Sage.
1. P1, P2, P3, P4, P5a ausführen, ins Protokoll (Erwartungsbasis für 3, 4, 6, 7).
2. DB-Backup, Deploy (`SQL/96` bzw. Migration, Web, dann Dienst) → TS-94.1.
3. Schalter an, ein Zyklus → TS-94.2; Bestandszeilen (z. B. SL/ST) `vorhanden`, unverändert; Kennzeichnung nach der Regel.
4. Reiter öffnen (ggf. 10 min) → „Nicht konfigurierte Kürzel“ ohne die neuen; Quelle je Zeile; Badge genau an „`Manual` und ∉ P2“.
5. Die 7 AKE-Zeilen per UI löschen (Tabelle nie leer → kein OSEON-Seed).
6. Ein Zyklus → `neu` = gelöschte Kürzel, die in P2 stehen (erwartet SL „Schweißen Laser“, ST „Stanzen“) mit Quelle Sage.
7. App-Pool recyceln → P4: keine OSEON-Seed-Zeilen (B, BG, MS …) aufgetaucht.
8. Folgelauf → `neu = 0`, keine Warnungen.
9. TS-94.3 bis TS-94.17 und TS-94.8–12.

**AKE-Regression (Schalter aus):** P5 auf der AKE-DB vorher; Deploy, zwei Zyklen → TS-94.1; OSEON-Teileverfolgung und
-Reporting stichprobenartig wie vorher (TS-16.5, TS-7.10 sinngemäß), P4 vorher/nachher: `IsExternalSystemRelevant` und
Offsets unverändert; Reiter ohne Badge/Hinweis, Quelle „Fremdsystem“ laut P5.

---

## TS-95 — Stückliste: Matchcode und Lagerplatz-Limit (v1.62.0)

Spec: `2026-10-16-stueckliste-matchcode-lagerplatz-limit-spec` (Matchcode Variante B). Regressionen: Kapitel 5 Stückliste,
TS-70 (BOM-Bridge), TS-79.20 (Zahnrad), TS-86 (CSV), TS-91 (Suchsyntax). **Vorbedingungen:** Testsysteme IDEAL
(`ProduktionsauftragHierarchisch` an) und AKE (aus); Benutzer mit Rolle picking, einer mit vorbau bei `FaCompletionAktiv` an,
einer mit fa_completion; ein Benutzer **mit** und einer **ohne** gespeicherte Spaltenauswahl „Bom“; Edge am PC und ein Tablet.
Eine Komponente mit > 3 Bestandsplätzen in der geprüften Stückliste (Kandidaten: Abfrage in der Spec, notfalls per Einbuchung auf
5 Plätze verteilen) — für TS-95.9 einen Artikel **mit** WMS-Hauptlagerplatz (viele IDEAL-Artikel haben keinen; dann gilt nur
„Menge absteigend“, kein Fehler). Automatisiert: `ReadOnlyBomBuilderTests` (Kopf ≠ Komponente, Variante B mit Fallback),
`FaHierarchyBomRepositoryTests.Map_Matchcode_Getrimmt_LeerWirdNull`, `StockMovementRepositoryTests` (Reihenfolge),
`BomLagerplatzZelleTests` (Zelle, Popover vor dem Read-only-Return), `BomZahnradHostTests` (seit v1.65.0 `TableToolbarHostTests`), CSV-Abdeckung.

- **TS-95.1 — Matchcode sichtbar (IDEAL, AK 1/2/8).** FA 1059463 → Stückliste. **Erwartet:** Spalte „Matchcode“ rechts von
  „Bezeichnung 2“; Stichprobe 3 Zeilen = Matchcode der Ressourcenummer (Strukturknoten bzw. Artikelinfo, wenn der Knoten
  keinen hat). **Negativ:** Matchcode des Kopfartikels → Fehler.
- **TS-95.2 — Matchcode AKE (AK 1/2).** Stückliste eines AKE-FA (Quelle SAGE, wenn vorhanden auch OSEON). **Erwartet:** wie 95.1,
  Wert aus dem Artikelstamm.
- **TS-95.3 — Read-only-Varianten.** Abarbeitungsliste → Stückliste, FA-Vervollständigung → Stückliste, an IDEAL ein Sub-FA.
  **Erwartet:** Matchcode-Spalte, Popover funktioniert, keine Picking-Spalten.
- **TS-95.4 — Fehlender Matchcode (AK 3).** Komponente ohne Artikelstamm/ohne Matchcode. **Erwartet:** leere Zelle, Seite lädt;
  Filter `(leer)` zeigt sie, `!(leer)` blendet sie aus; Log „Matchcode-Join … (Stueckliste)“.
- **TS-95.5 — Matchcode filtern (AK 4).** Teil eines Matchcodes in Kleinschrift. **Erwartet:** Treffer; `a, b` und `!x` wie TS-91.
- **TS-95.6 — Spaltenauswahl (AK 5).** Benutzer **mit** gespeicherter Auswahl. **Erwartet:** Matchcode sichtbar hinter
  „Bezeichnung 2“; ausblenden, F5 → bleibt aus; nach vorne ziehen, F5 → bleibt; Netzwerk: PUT `/api/user-view-preferences/Bom`
  200/204, nicht 400.
- **TS-95.7 — CSV (AK 6/15).** **Erwartet:** Spalte Matchcode vorhanden (ausgeblendet → fehlt); Lagerplatz der Komponente mit
  > 3 Plätzen = volle Liste `Code (Menge), …` ohne „weitere“.
- **TS-95.8 — Druck (AK 7).** Mit sichtbarem Matchcode drucken. **Erwartet:** Spalte im Druck, Lagerplatz volle Liste in der
  neuen Reihenfolge (umgebrochen); Filter auf Matchcode bzw. Lagerplatz → Druckkopf „Matchcode=…“ / „Lagerplatz=…“.
- **TS-95.9 — Limit und Reihenfolge (AK 9/10/11).** **Erwartet:** genau 3 Plätze + „+n weitere“ mit n = Anzahl − 3; erster Platz
  = Hauptlagerplatz (sofern dort Bestand), dann absteigende Menge; Zeile mit ≤ 3 Plätzen ohne Badge.
- **TS-95.10 — Popover Maus (AK 12/13).** Hover → alle Plätze, Hauptlagerplatz benannt; wegbewegen schließt; Klick → bleibt offen,
  Klick daneben schließt. Badge in der letzten Spalte/Zeile: nicht abgeschnitten. Lange Liste scrollt im Popover.
- **TS-95.11 — Popover Touch (AK 12).** Tippen öffnet, daneben tippen schließt; zwei Badges nacheinander → nur ein Popover offen.
- **TS-95.12 — Popover Tastatur (AK 12/17).** Tab → öffnet, Fokusrahmen sichtbar; Escape schließt; Tab weiter schließt.
  Screenreader sagt „Alle n Lagerplätze anzeigen“ und die Liste.
- **TS-95.13 — Filtern nach verdecktem Platz (AK 14).** Code eines Platzes an Position ≥ 4 in den Lagerplatz-Filter. **Erwartet:**
  Zeile sichtbar, Platz im Popover; mit rekursiver Suche auch im zugeklappten Ast. **Negativ:** nicht vorhandener Code → keine Zeile.
- **TS-95.14 — Viele Positionen (AK 18).** FA 1059463 (263 Positionen), „Alle aufklappen“. **Erwartet:** Lagerplatz-Spalte nicht
  breiter als 3 Plätze + Badge; Hovern über mehrere Badges flüssig.
- **TS-95.15 — Kontrast (AK 17).** Badge in normaler, gestreifter, grüner und grauer Zeile messen. **Erwartet (berechnet):** Text
  Weiß auf `#6c757d` 4,69:1; Fokusrahmen `#212529` ≥ 11,8:1; Trefferfläche ≥ 24 × 24 px.
- **TS-95.16 — Regression (AK 16/19/20).** Ohne Bestand weiter „-“, keine Kommissionierwagen; TS-79.20 (Zahnrad bei
  Leerzustand), Baum auf/zu, Komm.-Ziel-Dropdown, Picking-Checkbox + Umbuchen, Quell-Lagerplatz-Select2 unverändert.
  **QA-Fix:** Bestellen aktiv, eine **Baugruppen**-Zeile und eine Bauteil-Zeile anhaken → „Ausgewählte bestellen“. **Erwartet:**
  im Bestell-Modal für die Baugruppe Bezeichnung 1, die echte Menge und die Artikelgruppe (nicht der Matchcode, nicht „NaN“);
  dasselbe nach Umordnen der Spalten per Zahnrad.
- **TS-95.17 — Keine Seiten-Querscrollleiste (Bugfix v1.64.1).** Vorbedingung: IDEAL-Test, Fenster ca. 950 px breit. `Picking/Bom/131`
  (FA 1059463), „Alle aufklappen“. **Erwartet:** unter der Navigation keine Querscrollleiste der Seite; DevTools-Konsole
  `document.documentElement.scrollWidth === document.documentElement.clientWidth` → `true`. Kopfzeile, Filterkarte und
  Zahnrad bleiben beim Querscrollen der Tabelle stehen. **Negativ:** Fenster breiter als die Tabelle → auch keine Tabellen-Scrollleiste.
- **TS-95.18 — Genau ein blauer Balken (Bugfix v1.64.1).** Wie TS-95.17, Seite oben. **Erwartet:** am unteren Fensterrand ein
  mitlaufender Balken; Tabelle damit ganz nach rechts ziehen → Tabelle folgt, der Balken reicht bis zur letzten Spalte. Dann nach
  unten bis zum Tabellenende scrollen → der mitlaufende Balken verschwindet, nur der Balken der Tabelle bleibt (auch in der
  rechten Endstellung). Zurück nach oben → mitlaufender Balken wieder da, auf derselben Position. Beide Balken bewegen die Tabelle.
- **TS-95.19 — Quell-Lagerplatz-Dropdown (Bugfix v1.64.1).** Picking-Modus, Quell-Lagerplatz-Auswahl in der **letzten sichtbaren
  Zeile** und in einer Zeile ganz rechts nach dem Querscrollen öffnen. **Erwartet:** Liste vollständig sichtbar, nicht im
  Tabellenbereich abgeschnitten, keine zusätzliche senkrechte Scrollleiste in der Tabelle; Auswahl übernimmt den Platz. Ebenso:
  Lagerplatz-Popover „+n weitere“, Spaltenfilter-Datums-Popup, Rechtsklick-Spaltenmenü, Spaltenbreite ziehen, Druckansicht.
- **TS-95.20 — Gegenprobe andere Listen (Bugfix v1.64.1).** FA-Vervollständigung (`FaCompletion/Edit`, Tab Stückliste,
  Artikel-Select2 in der Tabelle) und Mehrfach-Wareneingang (`StockMovements/InboundBulk`, Zeilen-Artikel). **Erwartet:**
  Dropdown nicht abgeschnitten, keine Seiten-Querscrollleiste. Eine lange Liste ohne Select2 (z. B. Fertigungsaufträge):
  genau ein Balken wie in TS-95.18.

---

## TS-96 — BDE Stufe 1: Arbeitsgang an mehreren Werkbänken, veraltete Sage-Werkbänke, nur AG-Fertigmeldung (v1.63.0)

Spec: `2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-spec` (Variante B, Freigabe-Antworten 1–12). Regressionen: TS-77
(Struktur-Erkennung), TS-90 (Terminal-Status), TS-94 (Arbeitsgang-Abgleich). **Vorbedingungen:** SQL/97 eingespielt; Testsysteme
IDEAL (`ProduktionsauftragHierarchisch` an) und AKE (aus); BDE global an; Prüfabfragen P1–P6 aus der Spec ausgeführt, Ergebnisse
hier notieren (abhängige Erwartungen bis dahin ungeprüft). Testdaten IDEAL: FA 1059463 / Sub-FA 1065150 (Teil 50000404, Struktur
„ST EG KA“), EG an Werkbank Id 65 (Sage-Nr. 140, BDE aktiv) und Id 19 (2150, veraltet), KA an 150 (aktuell) und 2300 (veraltet).
Automatisiert: `WerkbankKuerzelFamilieTests`, `ProductionWorkplaceSyncServiceTests` (Kennzeichnung), `WorkOperationStructureDetectionServiceTests`
(Mehrfach, gekennzeichnet), `BdeScanResolverTests`/`BdeApiControllerTests` (Familie), `BdeBookingServiceStufe1Tests` (FA-a9, Fertigmeldung),
`BdeTerminalControllerTests.ReportCompletion_*`, `ProductionWorkplacesControllerTests`/`ProductionWorkplaceRepositoryTests` (FA-a8).

- **TS-96.1 — Kennzeichnung veralteter Sage-Werkbänke (IDEAL, AK 1–8).** Vorbedingung: `Sync:ProductionWorkplaceSyncEnabled` an,
  Quelle = View (P1). Schritte: 1. DryRun-Zyklus → Aktivitäts-Protokoll `ProductionWorkplaceSync`. 2. Scharfer Zyklus. 3. Stammdaten
  › Werkbänke, Filter „Nicht in Sage seit“ = `!(leer)`. 4. Zweiter Zyklus. **Erwartet:** 1. `nicht_in_sage_neu` = Anzahl aus P2 minus
  P3, keine DB-Änderung. 2. Id 19 (2150) und die KA-Werkbank 2300 haben ein Datum, Id 65 (140) und 150 nicht; Warnzeile „Nicht mehr in
  Sage (… neu gekennzeichnet …): 2150 Flächen entgr beendet (EG), …“. 3. genau die gekennzeichneten. 4. `nicht_in_sage_neu = 0`,
  Datum unverändert. **Negativ:** a) Quelle testweise auf eine leere View → Warnung „liefert keine Zeilen — keine Kennzeichnung“,
  nichts geändert. b) Quelle auf einen nicht existierenden Namen → bestehender Skip „Quelle nicht vorhanden“. c) Anzahl Werkbänke
  (P2) vorher = nachher. d) Gekennzeichnete Werkbank BDE-aktiv setzen, Zyklus → Warnung „… ist BDE-aktiv, steht aber nicht mehr in
  Sage.“, BDE bleibt an. e) Keine Mail wegen der Kennzeichnung.
- **TS-96.2 — Struktur-Erkennung legt EG/KA an (Bug-Fix, AK 10–12).** Vorbedingung: TS-96.1, `Sync:WorkOperationStructureDetectionEnabled`
  an, Sub-FA 1065150 offen. Schritte: Zyklus abwarten → P4 → Protokoll `WorkOperationStructureDetection`. **Erwartet:** Sub-FA 1065150
  hat EG (`ProductionWorkplaceId` = 65) und KA (Werkbank 150) zusätzlich zu ST; kein Count `arbeitsschritt_mehrdeutig` mehr,
  `arbeitsschritt_mehrere_werkbaenke` = 0, solange kein Kürzel doppelt aktuell ist. **Negativ:** MK, ZS, MO weiter unter „unbekannt“;
  ST einmal (kein Doppel-Insert); ein Kürzel, das nur noch an gekennzeichneten Werkbänken hängt, steht unter „unbekannt“.
- **TS-96.3 — Terminal Werkbank 65 findet EG (AK 13–15).** Vorbedingung: TS-96.2, Terminal-Benutzer mit Werkbank 65, Operator.
  Schritte: 1. Terminal öffnen, Operator scannen. 2. Liste prüfen. 3. Scan `1059463,EG`. 4. „Produktion starten“, „Beenden (mit
  Mengen)“. **Erwartet:** 2. Block FA 1059463 / 1065150 mit EG. 3. direkt gewählt bzw. Auswahl bei mehreren Sub-FAs. 4. Buchungsliste:
  Werkbank = Entgraten Fläche; FA-Baum: EG erledigt. **Negativ:** Scan `1059463,ST` an 65 → „Dieser Auftrag hat an dieser Werkbank
  keinen offenen Arbeitsgang.“
- **TS-96.4 — Derselbe Arbeitsgang an zwei Werkbänken (AK 13/15/19).** Vorbedingung: manuelle Werkbank „Entgraten 2 (Test)“ ohne
  Sage-Nr., Kürzel `EG` im WMS gepflegt, BDE aktiv, zweiter Terminal-Benutzer; `BdeMehrfachBuchungProArbeitsgang` aus. Schritte:
  1. Beide Terminals zeigen den EG-AG von 1065150. 2. Werker A startet Produktion an „Entgraten 2“. 3. Werker B an 65 startet denselben
  AG. 4. A beendet mit Menge. **Erwartet:** 1. gleicher AG. 2. Buchung mit Werkbank „Entgraten 2“, Cockpit-Kachel „Entgraten 2“ aktiv.
  3. 65 zeigt den AG als „läuft“, Start → Kollisionsdialog „… durch A an Entgraten 2 …“. 4. AG an beiden Terminals weg. **Negativ:**
  Werkbank ohne Kürzel zeigt den EG-AG nicht; „Entgraten 2“ auf BDE aus → nicht mehr in der Terminal-Auswahl, 65 sieht den AG weiter.
- **TS-96.5 — Altbestand an einer W1-Werkbank (AK 13/24).** Vorbedingung: P6 ≥ 1 offener AG an 2150 (sonst „nicht anwendbar“
  notieren). Schritte: Terminal 65. **Erwartet:** der AG an Werkbank 19 erscheint an 65 und ist buchbar; Buchung trägt Werkbank 65;
  FA-Baum zeigt den Status.
- **TS-96.6 — FA-Struktur-Symbole (AK 17).** Schritte: FA-Struktur, FA 1059463 aufklappen. **Erwartet:** EG mit Name „Entgraten
  Fläche“ (nicht leer) und Status gemäß Buchung, egal an welcher Familien-Werkbank gebucht; KA ohne Buchung offen bzw. überfällig.
- **TS-96.7 — Werkbank-Option „nur AG-Fertigmeldung“ (AK 25–33).** Vorbedingung: Werkbank 65 (oder Testwerkbank) → Edit → „Nur
  AG-Fertigmeldung“ an; `BdeNurFaMeldung` aus; offener EG- oder KA-AG. Schritte: 1. Terminal, AG antippen. 2. „Fertig melden“,
  Vorbelegung prüfen, bestätigen. 3. Buchungsliste und Tagesverlauf. 4. Cockpit. 5. FA-Baum. 6. Denselben AG noch einmal scannen.
  **Erwartet:** 1. nur grüner Knopf „Fertig melden“ (kein Rüsten/Produktion). 2. Gut = Sollmenge, Ausschuss 0, Hinweis „Sollmenge: …“;
  Toast „Fertig gemeldet“; AG an allen Familien-Werkbänken weg. 3. eine Produktionsbuchung, Start = Ende, Dauer 0, Werkbank 65,
  Operator, Menge. 4. keine Aktivität. 5. AG erledigt. 6. Scan → „keinen offenen Arbeitsgang“. **Negativ:** a) Gut leeren und
  bestätigen → „Bitte eine Gut- oder Ausschussmenge angeben.“ b) Werker B bucht den AG laufend an einer normalen Familien-Werkbank,
  A meldet fertig → Kollisionsdialog; mit `BdeMehrfachBuchungProArbeitsgang` an → gespeichert + Andere-schließen-Dialog.
  c) `BdeNurFaMeldung` an → Terminal im FA-Modus, Option wirkungslos, Edit zeigt Hinweis. d) Direkter POST `StartProduction` an der
  Werkbank → „An dieser Werkbank wird nur fertig gemeldet.“ e) Tätigkeit starten geht. f) FA verpackt → bestehende Sperrmeldung.
  g) Direkter POST `ReportCompletion` für einen schon gemeldeten AG → „Arbeitsgang ist bereits fertig gemeldet.“
- **TS-96.8 — Stammdaten-Liste und Rechte (AK 9/19/20).** Schritte: als `masterdata_read` Liste öffnen; als `masterdata` Edit, Option
  setzen; Spaltenfilter BDE = `Fertigmeldung`. **Erwartet:** Lesen ohne Bearbeiten-Knopf, Spalte „Nicht in Sage seit“ sichtbar;
  Speichern setzt `ModifiedBy`; Filter trifft (Badge „nur Fertigmeldung“); Pagination korrekt. **Negativ:** a) Sage-Werkbank → Kürzel und
  Name read-only. b) **Serverseitig (FA-a8):** Edit-Formular einer Sage-Werkbank in den Entwicklerwerkzeugen ändern (`readonly` am
  Kürzel entfernen, Kürzel „XX“, Sage-Nummer leeren), speichern → P2/Edit: `ArbeitsschrittCode` und `SageArbeitsplatznummer`
  unverändert. c) Nicht-Sage-Werkbank: Kürzel „ EG “ → gespeichert „EG“; leeren → NULL. d) Kürzel mit 21 Zeichen → Validierung.
- **TS-96.9 — AKE-Regression (AK 34).** Vorbedingung: AKE-Test, Master aus, P5 = 0, BDE an, AKE-Werkbank mit offenen AGs. Schritte:
  Terminal (Liste, Scan `FA,AG`, Start/Beenden), Cockpit, Tracking › je Werkbank, Stammdaten › Werkbänke. **Erwartet:** wie v1.62.0;
  Spalte „Nicht in Sage seit“ leer; Option aus; kein Protokolleintrag `ProductionWorkplaceSync`. **Negativ:** AKE-Werkbank mit Option
  an → Fertigmeldung geht auch bei AKE (gewollt).
- **TS-96.10 — Familien-Prüfung beim Buchen, manipulierter POST (AK 21–24).** Vorbedingung: zwei BDE-aktive Werkbänke A (Kürzel EG,
  z. B. 65) und B (anderes oder kein Kürzel); offener AG X an B, offener EG-AG Y an einer anderen EG-Werkbank (19 oder „Entgraten 2“);
  Terminal an A, Operator angemeldet; Ids von X/Y per P4; `BdeNurFaMeldung` aus. Schritte (POSTs aus der Browser-Konsole mit dem
  Antiforgery-Token der Seite): 1. Tätigkeit starten, dann `POST /BdeTerminal/StartProduction` mit `workOperationId = X`,
  `workplaceId = A`. 2. Dasselbe mit `StartSetup`. 3. An B einen AG von B starten und pausieren, auf A umschalten, Pausiert-Liste
  „Fortsetzen“. 4. A auf „Nur AG-Fertigmeldung“, `POST /BdeTerminal/ReportCompletion` mit X, `goodQty = 1`. 5. Option aus,
  `StartProduction` mit Y an A. 6. `StartProduction` mit einer nicht existierenden Id. **Erwartet:** 1./2. `InvalidState`
  „Dieser Arbeitsgang gehört nicht zu dieser Werkbank.“, keine neue Buchung, Tätigkeit läuft weiter. 3. Toast mit derselben Meldung,
  Buchung bleibt pausiert; an B fortsetzen geht. 4. abgewiesen, keine Buchung, keine Menge. 5. angenommen, Buchung mit Werkbank A.
  6. abgewiesen ohne Serverfehler. **Negativ:** a) AKE: AG einer anderen AKE-Werkbank per POST → abgewiesen; eigener → angenommen.
  b) Pausierte Tätigkeit an B, an A fortsetzen → geht. c) NurFA-Modus nur per Unit-Test (`NurFa_ChecksOrderWorkplace_NotAgWorkplace`):
  FA der Terminal-Werkbank angenommen (auch mit „01“ an alter Werkbank), FA einer anderen oder ohne Werkbank abgewiesen. d) Normal-Modus: `POST /BdeTerminal/StartProductionForOrder` mit einer beliebigen `productionOrderId` → `InvalidState` „Dieser Arbeitsgang gehört nicht zu dieser Werkbank.“, P4: kein neuer AG „01“ an diesem FA (QA-Fix, Unit-Test `StartProductionForOrder_NotOffered_Rejected_NoDefaultAgCreated`).

## TS-97 — BDE-Terminal: Liste nach Haupt-FA, Scan ohne AG, Prio (v1.64.0)

Spec: `2026-10-19-bde-terminal-liste-scan-ohne-ag-prio-spec` (Freigabe-Antworten 1–3). Regressionen: TS-90 (Terminal-Status),
TS-96.3/96.7/96.10 (Familie, Fertigmeldung, manipulierte POSTs). **Vorbedingungen:** IDEAL-Testsystem mit BDE an,
`ProduktionsauftragHierarchisch` an, `BdeNurFaMeldung` aus; Werkbank-Id 65 „Entgraten Fläche“ BDE-aktiv; Terminal-Benutzer mit
Default-Werkbank 65; FA 1059463 mit Sub-FA 1065150 (EG offen, TS-96.2/96.3). Prüfabfragen P1–P6 aus der Spec vorher ausführen.
Automatisiert: `BdeTerminalAuftraegeTests` (Gruppen-Sortierung), `BdeScanResolverTests` (Sub-FA-Scan, NotInScope),
`BdeNegativeMengenTests` (M1–M6), `FaArbeitsschrittSymboleTests` (bevorzugte Werkbank), `BdeTerminalControllerTests.Index_*`.

- **TS-97.1 — Gruppierung und Grenze (AK 1, 2, 6, 21).** Vorbedingung: an 65 mehr als 5 Haupt-FAs mit offenen EG-AGs (P1).
  Schritte: 1. Operator scannen. 2. Gruppenköpfe zählen. 3. „Weitere 5 anzeigen“. 4. Eine Gruppe aufklappen, einen AG starten.
  5. `/Settings` › BDE: `BdeTerminalMaxHauptFa = 3`, Terminal neu laden. **Erwartet:** 1. `/Settings` zeigt den Key mit 5. 2. 5 Köpfe,
  alle zugeklappt (▸). 3. 10 Köpfe. 4. Nach dem Start bleibt die Gruppe offen. 5. 3 Köpfe. **Negativ:** a) Wert `0`, `-1` bzw. `abc`
  → 5 Köpfe. b) Nur 2 Haupt-FAs → kein „Weitere“-Knopf. c) Operator wechseln → alle Gruppen wieder zu.
- **TS-97.2 — Sortierung Prio/Termin (AK 3, 4).** Vorbedingung: P1 liefert Haupt-FAs mit und ohne Prio. Schritte: Reihenfolge der Köpfe
  gegen P1 abgleichen. **Erwartet:** Prio gesetzt zuerst, kleinere Zahl oben, dann frühester Termin, dann FA-Nummer; ohne Prio nach
  Termin. **Negativ:** Haupt-FA ohne FAInfos-Zeile steht bei „ohne Prio“ und zeigt kein Prio-Badge.
- **TS-97.3 — Aufklappen 1059463 (AK 5).** Schritte: Kopf „FA 1059463“ antippen, danach erneut. **Erwartet:** Block Sub-FA 1065150 mit
  Knopf EG (Status-Zeichen wie TS-90), kein ST; Kopf zeigt ▾ und ist hervorgehoben; zweites Antippen klappt zu (▸). **Negativ:** Ein von
  Werker B laufender EG-AG erscheint als „läuft“ (blau, Sanduhr), nicht ausgeblendet.
- **TS-97.4 — Markierung und Knöpfe (AK 7, 8, 22).** Schritte: EG von 1065150 antippen; DevTools › Rendering › „Emulate vision
  deficiency: Achromatopsia“; DevTools-Accessibility-Baum. **Erwartet:** Dunkler Rahmen und „✓“ auch in Graustufen sichtbar,
  `aria-pressed="true"`; Zeile „Gewählt: 1059463 / 1065150 · EG …“ und „Rüsten starten / Produktion starten“ direkt unter der Karte,
  ohne Scrollen sichtbar; unter beiden Spalten keine Knöpfe mehr. **Negativ:** zweiten AG antippen → nur dieser markiert.
- **TS-97.5 — Scan ohne AG (AK 9–12).** Schritte im Feld „Oder FA-Nr scannen (optional ,AG-Nr)…“: a) `1059463` b) `1065150`
  c) `1059463,EG` d) Haupt-FA jenseits der ersten 5. **Erwartet:** a) Gruppe offen und im Sichtbereich, nichts vorgewählt. b) EG gewählt
  und markiert, Knöpfe sichtbar. c) wie v1.63.0 gewählt, Gruppe offen. d) eingeblendet und aufgeklappt. **Negativ:** e) Sub-FA, deren
  EG fertig gemeldet ist → „Dieser Auftrag hat an dieser Werkbank keinen offenen Arbeitsgang.“ f) `9999999` → „Arbeitsgang nicht
  gefunden“. g) `1059463,ST` → Meldung wie TS-96.3 Negativ. h) Leere Eingabe → nichts. i) Echtes Sage-Etikett scannen (P3), Ergebnis: ______
- **TS-97.6 — Negative Mengen (AK 13–16).** Schritte: Produktion starten, im Mengen-Dialog `-1` eintippen bei a) Pause,
  b) Teilfertigmeldung, c) Beenden. d) Werkbank mit „nur AG-Fertigmeldung“: „Fertig melden“ mit Gut `-5`. e) Gruppen-Abschluss
  (Mehrfach-Einstellungen an, zwei Produktionen) mit einem Eintrag `-1`. f) BDE-Admin: Buchungen › Bearbeiten › Gutmenge `-1` ›
  Speichern. **Erwartet:** a–e Meldung „Mengen dürfen nicht negativ sein.“, Buchung unverändert (P5); f Formular mit „Menge darf nicht
  negativ sein.“ am Feld, DB unverändert. **Negativ (Positivkontrolle):** `0`/`3` → wie v1.63.0.
- **TS-97.7 — FA-Baum-Name (AK 17).** Vorbedingung: Werkbank „Entgraten 2 (Test)“ mit Kürzel EG aus TS-96.4. Schritte: FA-Struktur,
  FA 1059463, EG-Symbol (Tooltip/aria-label). **Erwartet:** „EG – Entgraten Fläche“ (bevorzugt: BDE-aktiv, dann kleinste Id; P5b).
  **Negativ:** Kürzel an „Entgraten 2“ leeren → weiterhin „EG – Entgraten Fläche“; Kürzel nur an gekennzeichneter Werkbank → ohne Namen.
- **TS-97.8 — Hilfe (AK 18).** Hilfe › Listen: Absatz „Andere Listen“ lesen; BDE-Buchungen nach Operator filtern, Seite 2 enthält nur
  Treffer. Hilfe › BDE › Terminal-Bedienung: Gruppierung, Prio, Auswahl, Scan ohne AG, `BdeTerminalMaxHauptFa` beschrieben.
- **TS-97.9 — AKE-Regression (AK 19).** AKE-Testsystem (Master aus, BDE an, NurFA aus). Schritte: Operator scannen; Liste mit v1.63.0
  vergleichen; AG antippen und starten; `FA,AG` scannen; Pause/Beenden mit Menge. **Erwartet:** gleiche Blöcke, gleiche Reihenfolge,
  alle FAs, keine Gruppenköpfe, kein „Weitere“; neu nur Markierung und Knöpfe unter der Karte. **Negativ:** FA ohne AG scannen → bei
  genau einem AG gewählt, bei mehreren Auswahl „FA …: n Treffer – bitte auswählen“. P4 = 0.
- **TS-97.10 — NurFA unverändert (AK 20).** `BdeNurFaMeldung` an: Liste „Produktionsaufträge“, FA-Scan startet wie v1.63.0, keine
  Gruppenköpfe.

## TS-98 — Spaltenauswahl/Zahnrad: zentraler Toolbar-Host, Gruppenkopf, Speicherfehler, Anzeigename (v1.65.0)

Spec `2026-10-14-spaltenauswahl-zahnrad-sammel-spec`. Automatisiert (Quelltext-Wächter): `TableToolbarHostTests` (kein
`previousElementSibling`, Marker höchstens einmal je View und nie versteckt, Marker über der Zählzeile, jeder `viewKey` registriert,
Anzeigename), `ColumnPreferencesFehlerhinweisTests` (Laden/Speichern/Zurücksetzen, `pagehide`), `TableScriptScopedSelectorTests`
(`:scope > td[colspan]`). Alles Sichtbare ist Manual-UAT. **Vorbedingung allgemein:** Testsystem, Edge; einmal IDEAL
(`ProduktionsauftragHierarchisch` an), einmal AKE (aus); ein Benutzer mit und einer ohne gespeicherte Spaltenauswahl.
**Regression:** TS-18.10 (Lagerbestellung-Hinweis), TS-79.20 (Stückliste-Leerzustand), TS-91.B9 („?“-Knopf), TS-72 (skopierte
Selektoren), TS-86 (CSV-Inhalt), TS-95.17–95.20 (keine Seiten-Querscrollleiste, genau ein Scrollbalken).

- **TS-98.1 — Platz von Zahnrad und CSV (Matrix, AK 1–4).** Jede der 18 Ansichten öffnen, in beiden Modi: FA-Liste, Leitstand,
  Kommissionierung, FA-Vervollständigung, Abarbeitungsliste (mit AG), Stückliste, Lagerbestellung-Details, Kommissionierlisten
  Liste/Summiert, Vorschlagsliste Beschichtung Liste/Summiert, Vormontage Einzeln/Summiert, Beschichtungsaufträge Liste/Details,
  FA-Struktur, OSEON-Teileverfolgung, BDE-Stammdaten › Arbeitsgänge. **Erwartet:** genau ein Zahnrad rechts über der Tabelle, CSV direkt
  links daneben (wo vorhanden). Auf FA-Struktur und OSEON steht das Zahnrad rechts neben „Alle auf/zu“. Stichprobe CSV-only: Bestand,
  Lagerbewegungen, BDE-Stammdaten (drei Reiter: **jeder** Reiter hat seinen eigenen CSV-Knopf über seiner Tabelle). **Negativ:** Ein
  Knopf in der Filterkarte, in der Zählzeile, im Schalterblock oder in einem Hinweis, oder ein doppelter Knopf, ist ein Fehler.
- **TS-98.2 — Leitstand hierarchisch (AK 5).** **Erwartet:** Das Zahnrad steht **über** der Zeile „… Aufträge · … Sub-FAs“, nicht darin.
  Dasselbe gilt für FA-Liste, Kommissionierung, FA-Vervollständigung und Abarbeitungsliste.
- **TS-98.3 — Lagerbestellung-Details, Pflichthinweis (AK 6).** Bestellung mit nicht vollständig gelieferter Position schließen
  wollen. **Erwartet:** Der gelbe Hinweis erscheint, das Zahnrad bleibt sichtbar und öffnet das Panel.
- **TS-98.4 — Stückliste, Leerzustand (AK 7).** Kommissionierziel ohne Treffer wählen. **Erwartet:** gelber Hinweis über dem
  Zahnrad, das Zahnrad bleibt sichtbar.
- **TS-98.5 — Nur-Lesen Rollen (AK 8).** Benutzer ohne Stammdatenrecht öffnet Rollen. **Erwartet:** CSV rechts über der Tabelle,
  nicht im blauen Hinweis.
- **TS-98.6 — Gruppenkopf bleibt (AK 10–12).** Kommissionierlisten (IDEAL): Panel öffnen, `HauptFA` ans Ende ziehen, die nun erste
  Spalte („Haupt-Artnr.“) ausblenden. **Erwartet:** Alle HauptFA-Köpfe mit PDF-Link sind sichtbar, der PDF-Link lädt. Nach F5 ist der
  Stand unverändert. Wiederholen in: Summiert, Vorschlagsliste Beschichtung, Vormontage Einzeln, FA-Struktur (Struktur nach hinten,
  „Bezeichnung“ ausblenden → Strukturkopf **und** Kopfdaten-Zeile sichtbar), FA-Liste hierarchisch mit Kombigerät (Varianten-Tabelle
  bleibt gefüllt). Filter auf nicht vorhandenen Wert → Leerzustandszeile sichtbar. **Negativ:** Daten unter falscher Überschrift sind
  ein Fehler. Spalte wieder einblenden → Ausgangsbild.
- **TS-98.7 — „?“-Knopf (AK 13).** Erste Spalte ausblenden. **Erwartet:** „?“ steht in der nächsten sichtbaren Filterzelle, das
  Popover öffnet (wie TS-91.B9).
- **TS-98.8 — Speicherfehler 400 (AK 15–17).** Liste öffnen, in der Konsole `columnPreferences.getViewConfig().viewKey = 'Unbekannt'`,
  dann eine Spalte ausblenden und 2 s warten. **Erwartet:** über der Tabelle (links vor CSV/Zahnrad) der Hinweis „Spaltenauswahl konnte
  nicht gespeichert werden (Fehler 400). Die Anzeige gilt nur bis zum Neuladen.“ Bei offenem Panel steht derselbe Text oben im Panel.
  Die Liste bleibt bedienbar, der Fokus bleibt, wo er war. Zweite Änderung → weiterhin nur **ein** Hinweis. F5 → Spalte wieder
  sichtbar.
- **TS-98.9 — Verbindungsfehler (AK 15, 17).** DevTools › Netzwerk › Anforderungs-URL `/api/user-view-preferences/*` blockieren,
  Spalte ausblenden. **Erwartet:** „… (keine Verbindung) …“. Sperre aufheben, nächste Änderung → Hinweis verschwindet, F5 zeigt den
  neuen Stand.
- **TS-98.10 — Zurücksetzen scheitert (AK 18).** Sperre wie TS-98.9, Panel › „Auf Standard zurücksetzen“. **Erwartet:** kein
  Neuladen, Hinweis „Zurücksetzen fehlgeschlagen (keine Verbindung). Die gespeicherte Spaltenauswahl ist unverändert.“ Ohne Sperre:
  Die Seite lädt im Standard neu.
- **TS-98.11 — Erfolgsfall (AK 19).** Ohne Eingriff Spalten ändern. **Erwartet:** kein Hinweis, nach F5 bleibt der Stand erhalten.
- **TS-98.12 — Anzeigename (AK 22).** Als Admin einen Benutzer mit gespeicherter Auswahl der Vorschlagsliste Beschichtung bearbeiten.
  **Erwartet:** „Vorschlagsliste Beschichtung (IDEAL)“ bzw. „Vorschlagsliste Beschichtung — Summiert (IDEAL)“. „Zurücksetzen“ meldet
  denselben Namen. Die vorher gespeicherte Auswahl wirkte bis dahin unverändert.
- **TS-98.13 — Laden scheitert (Z1) und Wegnavigieren (Z2) (AK 21).** a) GET `/api/user-view-preferences/*` blockieren, Liste laden.
  **Erwartet:** „Gespeicherte Spaltenauswahl konnte nicht geladen werden (keine Verbindung). Standardansicht wird gezeigt.“ b) Ohne
  Sperre auf der FA-Liste eine Spalte ausblenden und **sofort** (< 1 s) ENTER im Server-Spaltenfilter. **Erwartet:** Nach dem Laden
  bleibt die Spalte ausgeblendet (Netzwerk: PUT mit `keepalive`).
- **TS-98.14 — Tastatur und Kontrast (AK 20).** Tab erreicht CSV und Zahnrad in Lesereihenfolge, der Screenreader sagt den Hinweis an
  (`role="alert"`). Kontrast des Hinweistexts: Bootstrap-`alert-warning` #664d03 auf #fff3cd = 7,2 : 1 (gemessen, Ziel ≥ 4,5 : 1).
- **TS-98.15 — Bestandsverhalten (AK 23).** Auf drei Ansichten (Stückliste, FA-Liste AKE, Kommissionierlisten): Spalten ein/aus,
  umordnen, Breite ziehen und Doppelklick, Kontextmenü, Standard-Sortierung, Laden gespeicherter Einstellungen; Filter, Sortierung,
  Paginierung und CSV-Inhalt. **Erwartet:** wie v1.64.1. Dazu TS-95.17–95.20 auf der Stückliste: keine Seiten-Querscrollleiste, genau
  ein blauer Balken.

## TS-99 — Hardware-Barcodescanner: Tastatur-Scan mit Präfix (v1.66.0)

Spec `2026-10-15-scan-handler-hardware-scanner-spec` (Präfix-Variante B, kein AIM). Automatisiert: `HardwareScannerTests`
(Node-Selbstprüfung `hardware-scanner.check.js`: Präfix erkannt/abgelehnt, Zeitgrenze, Enter-/Tab-Ende, Ende per Timeout, Replay bei
Fehlstart, Präfix-Zeichen mitten im Text, Shift zwischen Zeichen; Quelltext-Wächter für Layout, Register, BDE, Katalog, Profil) und
`StandortEinstellungenControllerTests.Save_EmptyInt_LeavesKeyUnchanged_AndSavesTheRest`. Alles am Gerät ist Manual-UAT.
**Vorbedingungen:** Testsystem; Admin setzt Standorteinstellungen → Hardware-Scanner: aktiv, Präfix `~09`, Abstand leer (= 50).
Testgerät **Netum AIDC** (Android) mit Scanner-Ausgabe als Tastendrücke, **DE-Layout**, Präfix `~09`, Suffix Enter. Etiketten:
Lagerplatz (Code 128), Kommissionierliste mit Haupt-FA-Barcode, Beschichtungsauftrag (Code 128), Artikel-QR
(`Artikelnummer;…;FA`), ein Code mit `y`, `z` und `-` im Inhalt.

- **TS-99.1 — Kalibrierung und Tottaste (AK 1, Nachtrag 16.10. Punkt 3).** a) Schalter **aus**: Quelltext enthält kein
  `hardware-scanner.js`, `<body>` hat keine `data-hw-scan-*`-Attribute; Scan in ein leeres Textfeld (z. B. Spaltenfilter). **Erwartet:**
  Rohtext `~09<Code>` genau so, `y`/`z`/`-` richtig. b) **Tottaste:** Erscheint statt `~09` nur `09`, `˜09` oder das `~` erst nach dem
  nächsten Zeichen, ist `~` auf dem Gerät eine Tottaste. Dann am Scanner und in den Standorteinstellungen einen anderen Präfix ohne
  Tottaste setzen (z. B. `#09`) und a) wiederholen; Ergebnis notieren. c) Schalter **an**, Abstand 50 / 30 / 100 probieren; der
  kleinste Wert, bei dem 10 Scans hintereinander erkannt werden, wird eingetragen.
- **TS-99.2 — Wareneingang (AK 2, 3, 4).** StockMovements/Inbound, Artikel-Feld antippen, Artikel-QR scannen. **Erwartet:** Artikel im
  Auswahlfeld gewählt, „✓ Gescannt: <Nr>“, kein Präfix oder Code als Text in einem Feld, **kein** Absenden des Formulars. Lagerplatz-Feld
  antippen, Lagerplatz-Etikett scannen. **Erwartet:** Option gewählt wie beim Kamera-Scan.
- **TS-99.3 — Umbuchung mit drei Zielen.** StockMovements/Transfer: nacheinander Artikel, Quell- und Ziel-Lagerplatz antippen und
  scannen. **Erwartet:** jeder Scan im angetippten Feld. Ohne angetipptes Feld: Hinweis „bitte zuerst das Feld antippen“.
- **TS-99.4 — Stückliste (AK 10, Variante B).** Picking/Bom im Kommissionier-Modus, nichts angetippt, Artikel-QR scannen. **Erwartet:**
  gelber Hinweis oben „Code … gescannt – bitte zuerst das Feld antippen, in das er soll“. Am **PC** den Knopf „Artikel scannen“ per
  Tab-Taste fokussieren (nicht auslösen) und scannen. **Erwartet:** Kommissionier-Ablauf wie beim Kamera-Scan. Am **Touch-Gerät** löst
  Antippen des Knopfs die Kamera aus (es gibt kein antippbares Artikelfeld): Scan bei offenem Kamera-Fenster ergibt ebenfalls den Hinweis
  — Ergebnis notieren (QA-Befund N-1: Artikel-Kommissionierung per Hardware-Scanner am Touch-Gerät auf der Stückliste nicht erreichbar,
  Variante-B-Grenze). **Ab v1.66.1 für den Touch-Fall überholt durch TS-99.14.** Ziel-Lagerplatz antippen, Wagen-Etikett scannen.
  **Erwartet:** Wagen gewählt.
- **TS-99.5 — FA-Liste und Leitstand.** ProductionOrders/Index: FA-Filter antippen, Haupt-FA-Barcode der Kommissionierliste scannen.
  **Erwartet:** Filter gesetzt und angewendet wie nach dem Kamera-Scan. Artikel-Filter mit Artikel-QR. Dasselbe auf PickingLeitstand.
- **TS-99.6 — Seite mit genau einem Ziel (AK 9).** Articles/Info, nichts antippen, Artikel-QR scannen. **Erwartet:** Artikelnummer gefüllt,
  Info lädt.
- **TS-99.7 — BDE-Terminal (AK 7, Schwerpunkt 3).** a) Vor der Anmeldung nichts antippen, Personalnummer scannen. **Erwartet:**
  Anmeldung wie bisher, Feld ohne Präfix. b) Nach der Anmeldung nichts antippen, **Haupt-FA**-Barcode scannen. **Erwartet:** landet im
  Feld „Oder FA-Nr scannen (optional ,AG-Nr)…“, die Gruppe der Haupt-FA klappt auf (Auflösung v1.64.0). c) **Sub-FA** scannen.
  **Erwartet:** Sub-FA direkt gewählt. d) Mit angetipptem Feld `FA,AG` scannen. **Erwartet:** wie Eingabe von Hand plus Enter.
  e) b) zusätzlich am PC in **Firefox** wiederholen (je nach Browser bleibt der Fokus auf dem ausgeblendeten Personalnummer-Feld). **Erwartet:**
  FA landet im FA-Feld, **kein** Operator-Wechsel und keine Meldung „Unbekannte Personalnummer“ (QA-Befund B-1).
- **TS-99.8 — Freies Feld (AK 8).** Server-Spaltenfilter der FA-Liste antippen, Code scannen. **Erwartet:** Code ohne Präfix im Filter, der
  Filter wird angewendet (ENTER-Verhalten). Bemerkungsfeld (Textarea) einer Buchung: Code an der Cursorposition, kein Absenden.
- **TS-99.9 — Kein Ziel (AK 11).** Startseite, nichts antippen, scannen. **Erwartet:** oben „⚠ Code … gescannt – auf dieser Seite nicht
  verwendbar“, verschwindet nach ca. 5 s. Ein Code mit `<b>` im Inhalt (Testetikett oder Simulation TS-99.11) erscheint als Text, nicht
  fett. Screenreader (TalkBack/NVDA) sagt den Hinweis an (`role="status"`). Kontrast: Bootstrap-`alert-warning` #664d03 auf #fff3cd =
  7,2 : 1, Zeichen ⚠ plus Text (nicht nur Farbe).
- **TS-99.10 — Geräte-Schalter (AK 12).** Mein Profil → „Dieses Gerät“ (nur sichtbar bei Schalter an) ausschalten. **Erwartet:** sofort,
  ohne Speichern, verhält sich der Scanner wie bei TS-99.1 a) (Rohtext). Am PC desselben Benutzers bleibt er aktiv. Wieder einschalten.
- **TS-99.11 — Simulation ohne Gerät (AK 5).** Am PC per Textersetzer/AutoHotkey `~09ABC{Enter}` mit < 20 ms je Zeichen senden.
  **Erwartet:** Scan erkannt. `~09` von Hand langsam tippen in einem Textfeld. **Erwartet:** `~09` steht vollständig und in richtiger
  Reihenfolge da (das `~` erscheint höchstens 50 ms verzögert). `a~b` normal tippen: steht als `a~b` da.
- **TS-99.12 — Standorteinstellungen (AK 13).** Gruppe „Hardware-Scanner“ mit 3 Feldern; Speichern mit leerem Zeichenabstand gelingt
  (kein Fehler „erwartet eine ganze Zahl“), Wert bleibt leer = 50. Abstand 80 speichern, Seite neu laden: `data-hw-scan-abstand="80"`.
  Als Nicht-Admin: Maske nicht erreichbar.
- **TS-99.13 — Regression Kamera (AK 14).** Kamera-Scan auf Inbound, Bom und ProductionOrders wie vorher.
- **Negativfälle.** a) Passwortfeld (Mein Profil, neues Passwort), schnell `~09x` per Simulation: **Erwartet:** steht unverändert im Feld
  (AK 6). b) Scanner auf **US-Layout**: **Erwartet:** kein Scan erkannt bzw. `y`/`z` vertauscht — dokumentiert die Pflicht zum DE-Layout.
  c) Scan bei offenem Kamera-Modal auf einer Seite mit genau einem Scan-Feld (Articles/Info, Ausgang „Alles“, Bewegungsliste):
  nach Regel einziges Ziel behandelt, Modal bleibt offen (Seiten mit mehreren Scan-Feldern: TS-99.14/99.16). **Ab v1.67.1 überholt,
  solange das Feld „Handscanner“ im Fenster den Fokus hat (Standard beim Öffnen):** Feld gefüllt, Fenster schließt (TS-99.17 d); nur
  nach Antippen von z. B. „Bild hochladen“ (Fokus weg) gilt noch „Modal bleibt offen“. d) EAN-Etikett im Artikel-Feld:
  „✗ Nicht gefunden“ (EAN eigene Spec). e) Normale Bildschirmtastatur-Eingabe auf Android: unverändert (IME wird nie abgefangen).
- **TS-99.14 — Stückliste am Handheld: Kamera-Fenster nimmt den Scan an (v1.66.1, Nachtrag 2 Punkt 1).** Picking/Bom im
  Kommissionier-Modus am Netum: „Artikel scannen“ antippen, das Kamera-Fenster ist offen; Artikel-QR mit dem **eingebauten Scanner**
  lesen. **Erwartet:** gleicher Ablauf wie ein Kamera-Treffer — Zeile gefunden, Kommissionier-Häkchen gesetzt, Kamera-Fenster schließt
  sich, kein gelber Hinweis. Negativ: a) Fenster mit „Abbrechen“ schließen, nichts antippen, scannen → Hinweis „bitte zuerst das Feld
  antippen“ (bisherige Regel). b) Kamera-Fenster „Lagerplatz“ (Ziel-Lagerplatz, Kamera-Knopf daneben) offen, Wagen-Etikett mit dem
  Scanner: **Ab v1.66.2:** Wagen gewählt, Fenster schließt sich (TS-99.16 e). c) Artikel-QR eines Artikels, der nicht in der Stückliste steht: Meldung wie beim Kamera-Scan. d) Scan
  sofort nach dem Antippen, bevor das Kamera-Fenster sichtbar ist (erste Kamera-Freigabe läuft noch): **Erwartet:** Hinweis „bitte
  zuerst das Feld antippen“ — erst das offene Fenster ist Ziel. e) Kamera auf einen anderen Artikel-QR der Liste richten und
  gleichzeitig mit dem Scanner scannen: **Erwartet:** genau ein Artikel kommissioniert, kein zweiter Treffer, kein „nicht gefunden“
  nach dem Schließen (QA-Fix v1.66.1).
- **TS-99.16 — Kamera-Fenster als Ziel auf allen Seiten mit mehreren Scan-Feldern (v1.66.2, Nachtrag 3).** Am Netum, Hardware-Scanner
  an. Je Seite den genannten Kamera-Knopf antippen, warten bis das Kamera-Fenster offen ist, dann mit dem **eingebauten Scanner** scannen.
  **Erwartet** jeweils: gleiches Ergebnis wie ein Kamera-Treffer in diesem Feld, das Fenster schließt sich, kein gelber Hinweis.
  a) **Buchungsmasken mit Auswahlfeld** — StockMovements/Inbound (Artikel-QR, Lagerplatz), Outbound (Artikel, Lagerplatz), Transfer
  (Artikel, Quell- und Ziel-Lagerplatz), LocationTransfer (Quell-, Ziel-Lagerplatz): Option gewählt, „✓ Gescannt“, Formular **nicht**
  abgeschickt.
  b) **Wareneingang Sammel** — StockMovements/InboundBulk: Lagerplatz-Knopf → Lagerplatz gewählt; Artikel-Knopf → Artikel-Zeile wie
  beim Kamera-Scan.
  c) **Bestandsübersicht** — StockOverview: Artikel-, Lagerplatz- und FA-Knopf je einmal; Filter gesetzt wie beim Kamera-Scan.
  d) **Filterlisten mit Absenden** — ProductionOrders, PickingLeitstand (FA-Nummer, Artikelnummer), Tracking/OseonIndex (Kundenauftrag,
  Artikel): Filter gefüllt und angewendet (Seite lädt gefiltert), genau einmal.
  e) **Stückliste Ziel-Lagerplatz** — Picking/Bom, Kamera-Knopf am Ziel-Lagerplatz, Wagen-Etikett: Wagen gewählt. „Artikel scannen“
  wie TS-99.14.
  Negativ: f) Fenster per „Abbrechen“ schließen, nichts antippen, scannen → Hinweis „bitte zuerst das Feld antippen“ (bisherige Regel).
  g) Kamera auf einen Code richten und gleichzeitig mit dem Scanner scannen (je eine Seite aus a und d): genau **eine** Auswertung,
  kein zweiter Treffer nach dem Schließen (QA-Fix v1.66.1; alle Seiten nutzen dasselbe Kamera-Fenster).
  h) Gerät **ohne** Hardware-Scanner (oder Geräte-Schalter aus): Kamera-Knöpfe und Kamera-Scan unverändert.
- **TS-99.17 — Fokus-Eingabe am Netum (v1.67.1, Nachtrag 4).** Vorbedingung: Standort-Schalter Hardware-Scanner an, Präfix `~09`,
  Netum auf Ausgabe **„Fokus-Eingabe“**, Präfix `~09`, Suffix Enter. „Mein Profil“ → „Scanner-Diagnose“ an (zum Mitlesen).
  Je Fall das Feld bzw. den Knopf **antippen**, dann scannen. **Erwartet** jeweils: im Feld steht nie `~09`; Diagnose zeigt
  `input … "~09…" → Präfix erkannt (Text)`, danach `Scan "…" an Feld #…`; das Formular wird **nicht** abgeschickt.
  a) StockMovements/Inbound: Select2 „Artikel“ öffnen (Suchfeld hat Fokus), Artikel-QR scannen → Artikel gewählt, Dropdown zu, „✓ Gescannt“.
  b) Outbound: Lagerplatz-Auswahl öffnen, Lagerplatz-Code 128 → Lagerplatz gewählt.
  c) Kamera-Fenster: auf Transfer den Kamera-Knopf „Quell-Lagerplatz“ antippen → Feld „Handscanner“ im Fenster ist ausgewählt, **keine**
  Bildschirmtastatur; scannen → Lagerplatz gewählt, Fenster zu. Ebenso Picking/Bom „Artikel scannen“ → Artikel kommissioniert (wie TS-99.14).
  d) Auf einer Seite mit **einem** Scan-Feld (Artikel-Info) den Kamera-Knopf antippen und scannen → Artikel gesucht, Fenster zu.
  e) BDE-Terminal nach der Anmeldung: „FA-Nr scannen“ antippen, Haupt-FA scannen → Gruppe klappt auf (wie TS-99 BDE); Sub-FA → direkt gewählt.
  f) FA-Liste: Spaltenfilter „FA-Nummer“ antippen, FA scannen → Filter angewendet, im Filterfeld steht die Nummer ohne `~09`.
  g) Normales Textfeld (z. B. Bemerkung): Code wird an der Cursor-Position eingefügt, vorhandener Text bleibt.
  Negativ: h) Netum-Suffix auf „kein Enter“: Scan wird nach der Zeitgrenze trotzdem ausgewertet (Diagnose „Zeitgrenze → Text-Scan
  beendet“). i) Nichts antippen und scannen: es kommt nichts an (Gerätegrenze, Hilfe-Text), kein Fehler. j) Passwortfeld der Anmeldung
  antippen und scannen: Text landet unverändert im Feld, Diagnose ohne Inhalt. k) Von Hand `~09abc` langsam tippen: bleibt Text.
  l) „Dieses Gerät“ aus: Text landet unverändert, Diagnose „ignoriert: Gerät aus“. m) USB-Scanner mit Tastatur-Emulation am PC: TS-99.1 ff.
  unverändert, genau **eine** Auswertung je Scan (keine Doppelauswertung).
- **TS-99.18 — Scanner-Diagnose (v1.67.1, Nachtrag 4 Punkt 3).** a) Frisches Gerät: „Mein Profil“ → Schalter „Scanner-Diagnose“ ist
  **aus**, keine Liste am Seitenende. b) Einschalten, Seite neu laden: Karte „Scanner-Diagnose – nur dieses Gerät, nichts wird gesendet“
  vor dem Seitenfuß, Text „Noch keine Ereignisse…“. c) Tippen und scannen: je Ereignis eine Zeile mit `+… ms`, Taste/keyCode/isComposing
  bzw. eingefügtem Text und Entscheidung; höchstens 30 Zeilen. d) „Kopieren“ → „In die Zwischenablage kopiert.“, eingefügt in eine Mail:
  Präfix, Zeichenabstand, Browser-Kennung und die Zeilen; ohne Zwischenablage (HTTP) ist der Text markiert mit Hinweis. e) Browser-
  Entwicklertools → Netzwerk: beim Tippen/Scannen/Kopieren **keine** Anfrage. f) Am Handheld lesbar (Schrift nicht kleiner als die
  Tabellen, Zeilen umbrechen, kein Querscrollen der Seite). g) Ausschalten → nach dem Neuladen keine Liste.
- **TS-99.19 — Einbuchung ohne Antippen am Netum (v1.67.2, Nachtrag 5; v1.68.1 sichtbares Feld, Nachtrag 6).** Vorbedingung: wie TS-99.17
  (Netum „Fokus-Eingabe“, Präfix `~09`, Suffix Enter), „Mein Profil“ → **„Scannen ohne Feld“ an**, „Scanner-Diagnose“ an.
  StockMovements/Inbound öffnen. **v1.68.1:** Oben steht das Feld **„✓ Scan bereit“ grün** (Diagnose: „Scan-Feld Fokus an … [aktiv:
  #hwScanOhneFeld]“). Ist es grau „Scan: tippen zum Aktivieren“, einmal antippen → grün, keine Bildschirmtastatur. Danach **kein
  Formularfeld antippen**. Jede Diagnose-Zeile endet mit `[aktiv: …]`; nach jedem Scan ist das Scan-Feld leer.
  a) Artikel-QR `G-KT-FRR-510-2010--1760B-250-V01;;1065141;09` bzw. `102044;;1065141;09` scannen → Artikel gewählt („✓ Gescannt“); FA
  `1065141` nur bei `QrMitFaNummer` an, sonst bleibt FA leer. Diagnose: `Scan "…" über Scan-Feld (ohne Fokus) → ArticleId (Artikel-QR)`.
  b) Lagerplatz-Code 128 scannen → Lagerplatz gewählt, Diagnose `… → StorageLocationId (Lagerplatz)`. c) Reihenfolge umgekehrt (erst
  Lagerplatz, dann Artikel) → beide gesetzt. d) Reine Artikelnummer (Code ohne `;`, kein Lagerplatz) → Artikel gewählt
  (`ArticleId (Artikelnummer)`). e) Beim Laden, Scrollen und Scannen: **keine** Bildschirmtastatur, die Seite springt nicht. f) Nach
  Auswahl eines Artikels im Select2-Dropdown von Hand und nach dem Schließen des Kamera-Fensters: nächster Scan ohne Antippen kommt an.
  g) Menge eintippen, dann auf eine freie Stelle tippen und scannen → kommt an. h) Formular wird durch keinen Scan abgeschickt.
  Negativ: i) Unbekannter Code ohne `;` (z. B. `XYZ999`) → gelber Hinweis „Code XYZ999 nicht zuordenbar“, Artikel und Lagerplatz
  unverändert. j) Textfeld „Fertigungsauftrag“ angetippt und scannen → Code landet wie bisher an der Cursor-Position in diesem Feld,
  nicht in Artikel/Lagerplatz (Vorrang angetipptes Feld, TS-99.17 g; nicht „Menge“ nehmen, ein Zahlenfeld kann `~09…` nicht aufnehmen).
  Kamera-Knopf Lagerplatz antippen und scannen → Lagerplatz über das Kamera-Fenster (TS-99.17 c). k) Schalter aus → Seite exakt wie
  v1.67.1: ohne Antippen kommt nichts an (TS-99.17 i). l) Andere Seite (z. B. Warenausgang) mit Schalter an → unverändert wie TS-99.17 i.
  m) Screenreader (TalkBack): das Scan-Feld wird als „Scan-Feld“ vorgelesen (v1.68.1, nicht mehr verborgen). n) Schalter aus: **kein** Scan-Feld
  sichtbar, Seite wie v1.68.0.
  o) (QA v1.68.1) Drei Codes direkt hintereinander scannen, ohne dazwischen etwas anzutippen (z. B. Artikel, Lagerplatz, Artikel) → **jeder**
  wird erkannt (Diagnose je Scan „über Scan-Feld (ohne Fokus)“), das Scan-Feld ist nach jedem Scan leer, nie steht `~09…` darin.
  p) (QA v1.68.1) Feld „Fertigungsauftrag“ antippen → Scan-Feld grau, Diagnose genau eine Zeile „Scan-Feld Fokus aus“; auf eine freie Stelle
  tippen → wieder grün, genau eine Zeile „Scan-Feld Fokus an“ – keine sich wiederholenden Fokus-Zeilen. q) (QA v1.68.1) Am Handheld hochkant:
  Scan-Feld ganz sichtbar, Text „Scan: tippen zum Aktivieren“ nicht abgeschnitten, kein Querscrollen; Zustand auch ohne Farbe am Text und
  an der Rahmenform (gestrichelt/durchgezogen) erkennbar.
  **v1.71.1 (Freigabe-Nachtrag 7, geht der Vorbedingung oben sowie p) und q) vor):** Grün „✓ Scan bereit“ erst nach einem **Tipp** auf das
  Scan-Feld; Skript-Fokus (beim Laden, nach freier Stelle, nach Select2/Kamera-Fenster) zeigt grau „Zum Scannen hier tippen“. Das Feld ist so
  breit wie die Formularkarte, Höhe wie `form-control-lg`. Netum, **Browser-Cache leeren**, Schalter wie oben:
  a7) Einbuchung frisch öffnen → Feld **grau** „Zum Scannen hier tippen“, obwohl der Cursor drin steht; Diagnose „Scan-Feld Fokus an (per Skript),
  inputmode=–“. Ein Scan jetzt kommt nicht an (Befund 1, festhalten). b7) Einmal auf das Scan-Feld tippen → Diagnose „Scan-Feld Tipp bei Fokus,
  inputmode=–“ (bzw. „Fokus an (durch Tipp)“), nach ca. 0,3 s „Scan-Feld bereit, inputmode=none“, Feld **grün durchgezogen** „✓ Scan bereit“;
  Bildschirmtastatur klappt ein **oder** bleibt stehen und lässt sich mit Zurück schließen – Beobachtung festhalten. c7) Ohne weiteren Tipp einen
  Artikel-QR scannen → Artikel gewählt, Diagnose „über Scan-Feld (ohne Fokus) → ArticleId“. d7) Drei Codes hintereinander (Artikel, Lagerplatz,
  Artikel) ohne Tipp → jeder zugeordnet, Feld bleibt grün und nach jedem Scan leer. e7) Feld „Fertigungsauftrag“ antippen (Tastatur erscheint),
  etwas tippen → Scan-Feld grau, „Scan-Feld Fokus aus“; danach auf das Scan-Feld tippen → wieder grün nach ca. 0,3 s, nächster Scan kommt an.
  Nur auf eine freie Stelle tippen → grau (Skript-Fokus), Scan kommt nicht an. f7) Einbuchung speichern bzw. Seite wechseln und zurück → wieder
  **grau**, erst nach einem Tipp grün. g7) PC (Chrome/Edge) mit Schalter an: Feld per Maus anklickbar, wird grün, Tastatur-Fokus (Tab) sichtbar
  (grau mit Ring), Formular sonst unverändert bedienbar, keine Störung. h7) Schalter aus → **kein** Scan-Feld, Seite wie v1.68.0.
  Negativ: Ohne Farbe (Graustufen-Ansicht) sind grau/grün an Text und Rahmenform (gestrichelt/durchgezogen) unterscheidbar.
- **TS-99.15 — Zeichenabstand prüfen (v1.66.1, Nachtrag 2 Punkt 2).** Standorteinstellungen → Hardware-Scanner, „Größter
  Zeichenabstand (ms)“: a) `5`, `0`, `-5`, `501` speichern. **Erwartet:** „Nicht gespeichert: 'Größter Zeichenabstand (ms)' muss zwischen
  10 und 500 liegen (war: …)“, auch die übrigen Felder bleiben ungespeichert, die Eingabe bleibt sichtbar. b) `10` und `500` speichern:
  gespeichert, `data-hw-scan-abstand` zeigt den Wert. c) Feld leeren und speichern: gespeichert, `data-hw-scan-abstand="50"`. d) Frisches
  System (Zeile nie gespeichert), nur Firmenname ändern und speichern: gespeichert, kein Fehler. e) Wert per SQL auf `-5` setzen (Web-Cache
  abwarten oder Web neu starten): Scanner erkennt weiterhin mit 50 ms (Client nimmt bei ungültigem Wert den Standard).

---

## TS-100 — Vault- und enaio-Links konfigurierbar (v1.67.0)

Spec `2026-10-21-vault-links-per-schalter-spec`. Pflege: Standorteinstellungen (Admin), Gruppen „Vault (Zeichnungen)“ und
„enaio (Dokumente)“. Keys: `VaultLinksAktiv`, `VaultLinkVorlage` (`{Artikelnummer}`), `EnaioLinksAktiv`, `EnaioLinkVorlage`
(`{ObjektId}`), ServiceSetting `Sync:EnaioDmsViewName`. Die zehn Vault-Stellen: Artikelinfo, Artikel-Liste, Bestandsübersicht,
Bewegungshistorie, Stückliste, FA-Liste, Kommissionier-Leitstand, FA-Vervollständigung (Liste und Edit-Kopf), FA-Abarbeitung.

- **TS-100.1 — AKE-Regression (Seed, nichts eingestellt).** Vorbedingung: frisches Deploy, keine Einstellung angefasst; eine FA mit
  enaio-Dokument und gepflegter Artikelnummer. Schritte: alle zehn Vault-Stellen öffnen, je einen Vault-Link und ein enaio-Badge
  klicken. **Erwartet:** alles wie vor dem Deploy; neuer Tab, Vault-Suche bzw. enaio-Viewer mit der richtigen Nummer/ID. Die
  Standorteinstellungen zeigen beide Gruppen mit „an“, den AKE-Vorlagen und dem View-Namen `[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]`.
- **TS-100.2 — Vault aus (IDEAL-Fall).** Standorteinstellungen → „Vault-Links anzeigen“ aus → Speichern; die zehn Stellen öffnen.
  **Erwartet:** kein Vault-Symbol, kein „Im VAULT öffnen“; die Artikelnummer steht überall; keine Lücke, keine leere Spalte; die
  enaio-Badges unverändert da. Sichtprüfung Desktop und Handheld.
- **TS-100.3 — enaio aus.** „enaio-Dokument-Links anzeigen“ aus → Speichern; FA-Liste, Leitstand, FA-Vervollständigung (Liste,
  Edit), FA-Abarbeitung. **Erwartet:** keine enaio-Symbole, im Edit-Kopf kein Badge mit Text; FA-Nummer steht; keine Lücke; Vault-Links
  unverändert. Der enaio-Sync läuft weiter, wenn `Sync:EnaioDmsEnabled=true` (Aktivitäts-Protokoll).
- **TS-100.4 — Stücklisten-Knopf bleibt.** Vault und enaio aus, FA-Vervollständigung → Edit. **Erwartet:** Knopf „Stückliste öffnen“
  sichtbar, Klick öffnet die read-only Stückliste. Ebenso der Knopf „Stückliste“ in den Listen von FA-Vervollständigung und
  FA-Abarbeitung.
- **TS-100.5 — Vorlage leer.** Schalter an, Vault-Vorlage leeren → Speichern. **Erwartet:** keine Vault-Links. Dasselbe mit der
  enaio-Vorlage: keine enaio-Badges. Vorlagen wiederherstellen → Links wieder da.
- **TS-100.6 — Vorlage ändern.** Vault-Vorlage `https://test.example/s?q={Artikelnummer}&x=1`, enaio-Vorlage
  `https://test.example/view?id={ObjektId}`. **Erwartet:** die Links zeigen genau diese Adressen mit Nummer bzw. ID an der
  Platzhalter-Stelle, sofort beim nächsten Seitenaufruf.
- **TS-100.7 — URL-Kodierung.** Artikel mit Leerzeichen oder `&` in der Nummer (falls vorhanden, sonst Unit-Test
  `LinkVorlageTests.Wert_wird_url_kodiert_eingesetzt` als Nachweis). **Erwartet:** im `href` steht `%20` bzw. `%26`.
- **TS-100.8 — Masken-Regression.** Schalter an; in den Standorteinstellungen nur den Firmennamen ändern → Speichern. **Erwartet:**
  beide Schalter weiterhin an, Vorlagen und View-Name unverändert, Links da.
- **TS-100.9 — CSV.** Bei Schaltern an und aus je ein CSV-Export aus Artikel-Liste, Stückliste und FA-Liste. **Erwartet:** keine URL in
  der Datei, Artikel- und FA-Nummer-Spalte gleich. Stücklistenzeile ohne Ressourcennummer: Zelle leer (nicht „Zeichnung in Vault öffnen“).
- **TS-100.10 — enaio-View-Name (Dienst, Testsystem).** Vorbedingung `Sync:EnaioDmsEnabled=true`, `ErrorNotification:Enabled=true`.
  a) Default unverändert → nächster Lauf „erfolgreich“ mit Zählern wie vorher. b) enaio-View auf `vw;DROP` → Speichern → nächster
  Lauf: **genau ein** Eintrag „fehlgeschlagen — Ungültiger View-Name“, Fehlermail, `EnaioDmsDocuments` unverändert. c)
  `[enaio].[dbo].[vw_gibtsnicht]` → Lauf „fehlgeschlagen“ wie bei fehlender View, Fehlermail. d) Feld leeren → Speichern → nächster
  Lauf nimmt den Default, „erfolgreich“. e) Wert zurück auf den Default. Kein Dienst-Neustart zwischen den Schritten.
- **TS-100.11 — Sync-Schalter unverändert.** `/ServiceSettings` → `Sync:EnaioDmsEnabled` steht dort wie bisher (Kategorie Sync), nicht in
  den Standorteinstellungen; aus → kein enaio-Lauf im Aktivitäts-Protokoll, auch bei gültigem View-Namen.
- **TS-100.12 — Hilfe.** Hilfe → Abschnitt „Vault- und enaio-Links“, Artikelinfo, FA-Liste (enaio), FA-Vervollständigung,
  FA-Abarbeitung. **Erwartet:** Texte nennen die Bedingung und alle Keys.
- **Negativfälle.** a) In `/Settings` `VaultLinksAktiv=FALSE` → aus. b) Wert `nein` → an (nur `false` sperrt). c) Nicht-Admin ruft
  `/StandortEinstellungen` auf → kein Zugriff. d) Vorlage ohne Platzhalter (`https://test.example/?q=`) → Nummer bzw. ID wird angehängt.

## TS-101 — QR-Code: erstes Feld Matchcode (v1.68.0), Trennzeichen (v1.69.0)

Spec `2026-10-23-qr-code-matchcode-statt-artikelnummer-spec`. Regressionsbezug: QR mit FA (TS zu `QrMitFaNummer`), TS-99 (Hardware-Scanner).

**Vorbedingungen:** Testsystem IDEAL (für OSEON zusätzlich AKE), Prüfabfragen P1–P5 der Spec gelaufen (P1: keine Dubletten an IDEAL),
Artikel 50002134 mit Bestand, ein FA mit 50002134 als Komponente (Stückliste). Etiketten **E1** = `G-KT-FRR-510-2010--1760B-250-V01;;1065141;09`
(Matchcode von 50002134), **E2** = `102044;;1065141;09`. Für Groß/Klein, Leerzeichen und „kein Treffer“ QR-Bilder erzeugen und über „Bild
hochladen“ scannen, ersatzweise in der Browser-Konsole `processScannedValue('<Text>', 'ArticleId', 'article', true, 'ProductionOrder')`.
Einstellungen unter `/Settings` → Allgemein.

| Nr | Einstellung | Schritte | Erwartung |
|---|---|---|---|
| 101.1 | Artikelnummer / FA aus | Wareneingang: E2, dann E1 | E2: Artikel 102044, FA leer, im Netzwerk-Tab **kein** `by-matchcode`. E1: „Nicht gefunden: G-KT-FRR-510-2010--1760B-250-V01“ wie bisher. `<body>` ohne `data-qr-erstes-feld`. |
| 101.2 | Artikelnummer / FA an | Wareneingang: E2 | Artikel 102044, FA `1065141`. |
| 101.3 | Matchcode / FA aus | Wareneingang: E1, dann E2 | E1: Artikel 50002134, FA leer. E2: „✗ Matchcode „102044“ nicht gefunden“, kein Artikel gewählt, FA unverändert (Ausnahme: P2 zeigt 102044 als Matchcode). `<body data-qr-erstes-feld="Matchcode">`. |
| 101.4 | Matchcode / FA an | Ein-, Ausgang, Umbuchung: je E1 | Artikel 50002134 und FA `1065141`. |
| 101.5 | Matchcode | Sammel-Wareneingang: E1 zweimal | Eine Zeile 50002134, Menge 1, dann 2. |
| 101.6 | Matchcode | Stückliste mit 50002134 bzw. ohne: E1 | Mit: Zeile abgehakt/hervorgehoben. Ohne: bestehendes Modal „nicht in der Kommissionierliste“ mit `50002134`. |
| 101.7 | Matchcode | Artikelinfo, Bestandsübersicht, FA-Liste, Leitstand, OSEON (AKE): E1 | Artikelinfo zeigt 50002134. Bestandsübersicht: Artikelfilter `50002134`, FA-Filter `1065141` **nur bei `QrMitFaNummer` an**, sonst leer (v1.76.0). FA-Liste/Leitstand/OSEON: Filter „Artikelnummer“ `50002134`, Formular abgeschickt. |
| 101.8 | Matchcode | 101.3 mit Kamera, Bild-Upload, Hardware-Scanner (Tastatur-Emulation **und** Netum Fokus-Eingabe) sowie Einbuchung mit „Scannen ohne Feld“ (TS-99.19) | jeweils wie 101.3. Bei „Scannen ohne Feld“ und kein Treffer zusätzlich der gelbe Hinweis „Code … nicht zuordenbar“. |
| 101.9 | Matchcode | `G-KT-GIBT-ES-NICHT;;1065141;09`; `g-kt-frr-510-2010--1760b-250-v01;;1065141;09`; `  G-KT-FRR-510-2010--1760B-250-V01  ;;1065141;09`; `;;1065141;09` | „Matchcode „G-KT-GIBT-ES-NICHT“ nicht gefunden“, nichts befüllt, auf Filterseiten **kein** Submit. Groß/Klein und Leerzeichen → 50002134. Leeres Feld 1: **kein** `by-matchcode`, Verhalten wie im Modus Artikelnummer. |
| 101.10 | Matchcode | An einem zweiten Artikel („Artikel bearbeiten“) den Matchcode von E1 eintragen, E1 scannen; danach zurücksetzen | „Matchcode „…“ ist mehrdeutig (2 Artikel: 50002134, …) – bitte den Artikel von Hand wählen“, ca. 10 s sichtbar, nichts gewählt/abgeschickt. Artikelinfo mit dem Matchcode eingetippt: Meldung „… ist mehrdeutig (2 Artikel: …) – bitte die Artikelnummer eingeben.“ **Hinweis:** Der nächste Artikel-Sync überschreibt den Hand-Wert, zügig testen. |
| 101.11 | Matchcode | Lagerplatz-Scan; FA-Scan mit E1 („FA Nummer“ in der FA-Liste, Ziel-Lagerplatz der Stückliste) | unverändert: Lagerplatz wie bisher, FA `1065141`. |
| 101.12 | Matchcode | Netzwerk im Browser trennen (Entwicklertools offline), E1 | „Matchcode „…“ konnte nicht geprüft werden“, nicht „nicht gefunden“. |
| 101.13 | — | `/Settings`: `QrErstesFeld` = `matchcode` speichern, dann `Matchcod` | `matchcode` → gespeichert als `Matchcode`. `Matchcod` → gelbe Meldung „QrErstesFeld: erlaubt sind „Artikelnummer“ oder „Matchcode“ – nichts gespeichert.“, auch andere geänderte Werte der Maske bleiben ungespeichert. |
| 101.14 | — | Meldungen auf Stückliste und Sammel-Wareneingang (verstecktes Ziel) | Meldung steht am Scan-Knopf, Text mit ✗ (nicht nur Farbe), lesbar. |

**v1.69.0 (Freigabe-Nachtrag 2): Auswahllisten und `QrTrennzeichen`.** Zusätzliche Etiketten mit Komma: **E1k** =
`G-KT-FRR-510-2010--1760B-250-V01,,1065141,09`, **E2k** = `102044,,1065141,09`. Vorher Prüfabfrage (Lagerplatz-Codes mit Komma, Nachtrag 2
Punkt 4 ungeprüft): `SELECT Code, BarcodeValue FROM StorageLocations WHERE Code LIKE '%,%' OR BarcodeValue LIKE '%,%'` – Treffer notieren.

| Nr | Einstellung | Schritte | Erwartung |
|---|---|---|---|
| 101.15 | — | `/Settings` → Allgemein ansehen, nur mit Tastatur bedienen (Tab, Pfeiltasten) | `QrErstesFeld` und `QrTrennzeichen` sind Auswahllisten (Artikelnummer/Matchcode bzw. „; (Semikolon)“/„, (Komma)“), kein Textfeld; nach dem ersten Start nach Deploy steht `QrTrennzeichen` = „; (Semikolon)“. Andere Einstellungen unverändert (Schalter, Textfelder). Ohne Bearbeitungsrecht: Listen gesperrt. |
| 101.16 | Trennzeichen `;` (Standard) | 101.1–101.4 wiederholen | genau wie v1.68.x; `<body>` **ohne** `data-qr-trennzeichen`. E2k im Wareneingang: „Nicht gefunden: 102044,,1065141,09“, FA leer. |
| 101.17 | `,` / Artikelnummer / FA an | Speichern, Wareneingang: E2k, dann E2 | `<body data-qr-trennzeichen=",">`. E2k: Artikel 102044, FA `1065141`. E2: „Nicht gefunden: 102044;;1065141;09“ (Semikolon trennt nicht mehr), FA leer. |
| 101.18 | `,` / Matchcode / FA an | Ein-, Ausgang, Umbuchung, Sammel-Wareneingang, Artikelinfo, Bestandsübersicht: E1k | Artikel 50002134, FA `1065141` in Ein-, Ausgang, Umbuchung und Bestandsübersicht (nur mit `QrMitFaNummer`); Netzwerk: `by-matchcode?matchcode=G-KT-FRR-510-2010--1760B-250-V01` (ohne Rest nach dem Komma). E1 (mit `;`): „Matchcode „G-KT-…;;1065141;09“ nicht gefunden“. |
| 101.19 | `,` | Einbuchung mit „Scannen ohne Feld“ (TS-99.19) am Netum: E2k, Lagerplatz-Etikett, E2 | E2k → Artikel (Diagnose „(Artikel-QR)“), Lagerplatz → Lagerplatz, E2 → als Artikelnummer gesucht, nicht gefunden + Hinweis „Code … nicht zuordenbar“. Ein Lagerplatz-Code mit Komma (Prüfabfrage) würde als Artikel-QR gelesen – Befund melden. |
| 101.20 | — | `/Settings`: in den Entwicklertools den Wert einer Option von `QrTrennzeichen` auf `\|` ändern, speichern; danach per SQL `UPDATE AppSettings SET Value='x' WHERE [Key]='QrTrennzeichen'` (Testsystem) und `/Settings` neu laden, speichern; zurück auf `;` | `\|`: gelb „QrTrennzeichen: erlaubt sind „;“ oder „,“ – nichts gespeichert.“, nichts gespeichert. `x`: Liste zeigt „„x“ (ungültig – bitte wählen)“, Speichern ohne Auswahl ergibt dieselbe Meldung; Seiten trennen bei `x` wie mit `;` (kein Attribut). |
| 101.21 | — | Artikelinfo-Hinweis unter dem Feld, Hilfe „QR-Code: erstes Feld“ | nennen `;` oder `,` und die Einstellung `QrTrennzeichen`. |
| 101.22 | `QrMitFaNummer` an (v1.76.0) | Bestandsübersicht, E2 per Kamera bzw. Bild hochladen | Artikelfilter `102044`, FA-Filter `1065141`; „Filtern“ → FA-Bestand (die FA übersteuert den Artikelfilter, Hilfe). |
| 101.23 | `QrMitFaNummer` aus, Seite neu laden | FA-Filter von Hand `999`, dann E2 scannen | Artikelfilter `102044`, FA-Filter **leer**; „Filtern“ → nur Artikel 102044. |
| 101.24 | wie 101.22 / 101.23 | per Hardware-Scanner (Tastatur-Emulation, Netum Fokus-Eingabe, Kamera-Fenster mit Handscanner-Feld) | gleiche Ergebnisse wie 101.22 bzw. 101.23. |
| 101.25 | Negativ/Regression, `QrMitFaNummer` aus | FA-Knopf der Bestandsübersicht mit E2; danach 101.1/101.2 (Ein-/Aus-/Umbuchung); `QrMitFaNummer` in der DB auf `TRUE` | FA-Knopf → FA `1065141` (unabhängig von der Einstellung); Ein-/Aus-/Umbuchung unverändert; `TRUE` wirkt wie an. Sammel-Wareneingang scannt wie vorher (101.5). Automatisiert: `StockOverviewControllerTests`, `HardwareScannerTests.Bestandsuebersicht_QrFaUebernahme_FolgtQrMitFaNummer_NichtFest`. |

## TS-102 — Aktivitäts-Protokoll: Spaltenfilter über alle Einträge, deutsche Aktivitätsnamen (v1.70.0)

Bezug: TS-27/29/44/54 (Aktivitäts-Protokoll), TS-91 (Suchsyntax), TS-86 (CSV). Bug [[2026-10-28-aktivitaetsprotokoll-spaltenfilter-nach-paging-bug]].

**Vorbedingungen:** Admin-Anmeldung, `/SyncLog`, Seitengröße 25. Prüfabfragen der Spec vorher auf dem Testsystem: **P-2** (Kollation
der `SyncLogs`-Spalten, erwartet `…_CI_…`, sonst STOPP und melden) und **P-3** (Zeilenzahl > 50, Altnamen notieren):
`SELECT DATABASEPROPERTYEX(DB_NAME(), 'Collation')`, `SELECT c.name, c.collation_name FROM sys.columns c JOIN sys.tables t ON
t.object_id = c.object_id WHERE t.name = 'SyncLogs' AND c.collation_name IS NOT NULL`, `SELECT Service, COUNT(*) FROM dbo.SyncLogs
GROUP BY Service ORDER BY Service`.

| Nr | Schritte | Erwartung |
|---|---|---|
| 102.1 | Eine Meldung von Seite 3 oder älter wählen, in „Meldung“ einen Teil davon filtern (ENTER) | Eintrag auf Seite 1; Trefferzahl = `SELECT COUNT(*) FROM dbo.SyncLogs WHERE Message LIKE '%…%'`; keine leeren Folgeseiten. |
| 102.2 | Spaltenfilter „Aktivität“ `abgleich`, dann `!abgleich`, dann `werkbank`, dann `OperationConfig%` | `abgleich`/`OperationConfig%`: nur „Arbeitsgang-Abgleich (Sage-View)“; `!abgleich`: alle anderen; `werkbank`: „Werkbank-Anlage (Sage-Arbeitsplätze)“ und „Werkbank-Sync (OSEON → …)“. Tooltip der Zelle = technischer Name. |
| 102.3 | Auswahlfeld „Aktivität“ aufklappen, „Arbeitsgang-Abgleich (Sage-View)“ wählen; danach altes Lesezeichen `/SyncLog?service=OperationConfigSync` | deutsche Namen alphabetisch (25 Einträge); URL `service=OperationConfigSync`, nur diese Zeilen, Auswahl bleibt; Lesezeichen funktioniert gleich. |
| 102.4 | „Zeitpunkt“: `TT.MM.JJJJ` (heute), `MM.JJJJ`, `>=TT.MM.JJJJ`, `TT.MM.JJJJ...TT.MM.JJJJ` | nur Zeilen im Zeitraum (Grenzen ganze Tage), Trefferzahl gefiltert. `14:3`: keine Zeile, `!14:3`: alle (kein Datum, bewusst, Antwort 2). |
| 102.5 | Mit aktivem Spaltenfilter „CSV“ drücken | Datei enthält genau die Trefferzahl an Zeilen, Spalte Aktivität deutsch; kein Kappungshinweis bei < 5000 Treffern. |
| 102.6 | „Meldung“: `FEHLGESCHLAGEN`, dann `fehlgeschlagen` | gleiche Treffer (gilt nur bei CI-Kollation, P-2). |
| 102.7 | Filterkarte „Stufe = Warning“ + Spaltenfilter „Aktivität“ `lager%` | nur Warnungen von Lagerplatz-/Lagerbestand-Sync und Lagerbestellungs-E-Mail-Versand. |
| 102.8 | Negativfälle: „Zeitpunkt“ `32.13.2026`, dann `!32.13.2026`; URL `?colf_Unbekannt=x`; URL `?page=0`; Altname aus P-3 (falls vorhanden); Anmeldung ohne Admin | keine Zeile bzw. alle Zeilen; Liste unverändert; Seite 1 ohne Fehler; Altname steht technisch in der Spalte und wird von `!…` mitgezählt, vom Positivfilter nicht; kein Zugriff. |
| 102.9 | Hilfe → „Aktivitäts-Protokoll“ | Rolle nur `admin`, deutsche Namen mit technischem Namen in Klammern, Hinweis „Spaltenfilter durchsuchen alle Einträge“. |


## TS-103 — FA-Struktur live aus Sage (SData, nur lesen) (v1.71.0)

Bezug: Spec [[2026-10-30-sage-sdata-fa-ansicht-spec]]. Nur am **AKE-Testsystem** (akenet01.ake.at:4445 gegen `sagetest01.ake.at:5493`,
Dataset `ake_TEST2026;1`) prüfbar; für IDEAL gibt es keinen SData-Host – dort Schalter aus lassen.

**Vorbedingungen:** `/ServiceSettings`: `SData:BaseUrl` = `https://sagetest01.ake.at:5493`, `SData:Dataset` = `ake_TEST2026;1`.
Auf dem Server im Web-Ordner `appsettings.Production.json` mit `SageLagerbuchung:Username/Password` des SData-Servicebenutzers
(README „Web-App: Sage-Zugangsdaten“), App-Pool recycelt. Vom IIS-Host aus `sagetest01.ake.at:5493` erreichbar (Firewall prüfen).
Ein Benutzer mit Rolle picking und einer ohne picking/tracking/Leitstand-Freigabe/Vorbau.

| Nr | Schritte | Erwartung |
|---|---|---|
| 103.1 | Schalter „Sage-Live-Ansicht FA-Struktur aktiv“ aus; Menü ansehen, dann `/SageSdataFa` direkt aufrufen | Kein Menüpunkt; Startseite mit Warnung „Die Sage-Live-Ansicht der FA-Struktur ist deaktiviert.“ |
| 103.2 | Standorteinstellungen → Feature-Schalter → „Sage-Live-Ansicht FA-Struktur aktiv“ an; als picking-Benutzer und als Benutzer ohne Rolle anmelden | picking: Menüpunkt „FA-Struktur (Sage live)“, Seite mit Eingabefeld, keine Diagnose. Ohne Rolle: kein Menüpunkt, URL → Zugriff verweigert. |
| 103.3 | FA-Nummer `835` → „Aus Sage laden“ | 7 FAs: Haupt-FA 835 (offen, „Haupt-FA“, `MAN`) mit 11 Positionen (10 MA, 1 AG „999 TESTFertigung“, Arbeitsplatz 9999); die 6 MA-Positionen 86003870/66/62/57/55/53 zeigen „Sub-FA 836…841“, Klick springt zur Sub-FA-Karte. Sub-FA 836: 1 Position 00722001, „Menge je Einheit“ 0,6. Keine Warnung. Rückmeldungen/Fertigmeldungen „keine Rückmeldung“/„keine Fertigmeldung“. Diagnose: **5** Aufrufe, alle 200; die URL der Positionen enthält `BelID eq 835 or BelID eq 836 …`, keine Zugangsdaten. |
| 103.3b | FA-Nummer `846` | Struktur mit Haupt-FA 846 (Badge `VK`) und ihren Sub-FAs (3 FAs), keine Warnung „kein Haupt-FA“. |
| 103.4 | FA-Nummer `836` | Hinweis „BelID 836 gehört zur Struktur 835 – angezeigt wird die ganze Struktur.“, danach wie 103.3. |
| 103.5 | `999999999`; dann Eingabe `0`, dann URL `?belId=abc` | „BelID 999999999 in Sage nicht gefunden.“, Diagnose 1 Aufruf. `0`/`abc`: Meldung „Bitte eine FA-Nummer (ganze Zahl größer 0) eingeben.“, keine Diagnose. |
| 103.6 | Mehrdeutige Zuordnung (Struktur aus Prüfabfrage P-2 mit gleichem Artikel in zwei Sub-FAs) | Gelbe Warnung „Sub-FAs … haben denselben Artikel … – Zuordnung nicht eindeutig.“, beide unter „Nicht eindeutig zugeordnete Sub-FAs“. Im Test-Mandanten gibt es keinen solchen Fall: als „nur Unit-Test belegt“ protokollieren. |
| 103.7 | Eine FA ohne Positionen (falls vorhanden) aufklappen | „Keine Positionen.“ |
| 103.8 | `/ServiceSettings`: `SData:BaseUrl` leeren, Seite neu laden; danach `https://user:pw@sagetest01.ake.at:5493` eintragen | „Sage-SData nicht konfiguriert: SData:BaseUrl“ bzw. „… enthält Zugangsdaten …“, kein Eingabefeld, keine Diagnose. Wert zurücksetzen. |
| 103.9 | `SData:BaseUrl` auf einen nicht erreichbaren Port (`https://sagetest01.ake.at:5999`), `835` laden | Nach höchstens ~20 s rotes Banner „Sage nicht erreichbar: …“; Diagnose: 1. Aufruf „0 keine Verbindung“, die übrigen 4 „übersprungen“. Keine Fehlerseite. Wert zurücksetzen. |
| 103.10 | Falsches Passwort in `appsettings.Production.json`, App-Pool recyceln, `835` laden | Banner „FA-Suche fehlgeschlagen (HTTP 401 …)“ (oder 403), Diagnose mit Status und gekürztem Body, übrige Aufrufe „übersprungen“. Danach richtiges Passwort, recyceln. |
| 103.11 | `SageLagerbuchungSslZertifikatPruefen` = `false`, Seite neu laden; dann `true` | `false`: gelbe Warnung zur TLS-Prüfung, Aufrufe 200. `true`: Warnung weg. Hat der Server eine nicht vertraute Kette, scheitert der Aufruf mit Status 0 (TLS) – **erst auf einer neuen Verbindung**: bis zu 2 Minuten warten oder App-Pool recyceln. Bleibt es bei 200, ist die Kette am Server vertraut (Befund notieren). |
| 103.12 | Diagnose aufklappen, bei einem Aufruf „Rohdaten anzeigen“ | JSON-Antwort, höchstens 4000 Zeichen; URL ohne Benutzer/Passwort. |
| 103.13 | Vergleich (informativ): dieselbe Struktur unter „FA-Struktur“ (materialisiert) öffnen | Nur Beobachtung für den Entscheid zur Materialisierung, keine Abnahmebedingung (P-5). |

## TS-104 — Lagerplatz-Etiketten im Rollenformat 93 × 55 mm (v1.72.0)

Bezug: Spec [[2026-11-06-lagerplatz-etikett-rolle-und-vormontage-felder-spec]], Teil 1. Beide Standorte. Querverweis TS-67 (Standorteinstellungen).
Automatisiert: `LagerplatzEtikettRolleTests` (Katalog + Quelltext-Wächter). Das Druckbild ist nur manuell prüfbar.

**Vorbedingungen:** Rollendrucker mit Etiketten 93 × 55 mm (fortlaufend) am Arbeitsplatz installiert; Modell/dpi notieren. Ein Admin und ein
Benutzer mit `masterdata_read`. Mindestens 4 Lagerplätze, einer mit Beschreibung und Zone, einer ohne beides, einer mit 200 Zeichen Beschreibung,
der längste Barcode-Wert laut P9 (IDEAL-Test: `Magazin-W1;0;0;0`, 16 Zeichen) und ein manueller 12-stelliger Code.

| Nr | Schritte | Erwartung |
|---|---|---|
| 104.1 | Schalter aus (bzw. nie gespeichert): Lagerplätze → „Alle Etiketten drucken“, Druckvorschau; dann Einzeldruck eines Platzes | Wie bisher: A4 hoch, drei Etiketten je Seite mit Rahmen, Seitenzahl ⌈n/3⌉; Einzeldruck eine A4-Seite. Kein Hinweis zum Rollenformat in der blauen Leiste. |
| 104.2 | Admin: Standorteinstellungen → Gruppe „Druck“ → „Lagerplatz-Etiketten im Rollenformat 93 × 55 mm“ an, Speichern, neu laden | Feld mit Hinweistext (Rollendrucker, Papierformat 93 × 55 mm, Ränder „Keine“); Haken bleibt nach dem Neuladen. In `/Settings` steht der Key in der Gruppe „Lager“. |
| 104.3 | Einzeldruck eines Platzes mit Beschreibung und Zone; Druckdialog: Rollendrucker, Papier 93 × 55 mm, Ränder „Keine“ | Leiste zeigt den Treiber-Hinweis. Vorschau: genau **eine** Seite 93 × 55 mm, ohne Rahmen; Code, Beschreibung, Barcode, Zone vollständig auf der Seite; Barcode nicht verzerrt. Ausdruck auf dem Rollendrucker entspricht der Vorschau. |
| 104.4 | „Alle Etiketten drucken“ | n Seiten zu je einem Etikett, **keine leere Schlussseite**. Einige Etiketten drucken. |
| 104.5 | Scan mit dem Handscanner (Netum): manueller 12-stelliger Code, dann der längste Code aus P9 | Gelesener Wert = Code. Ergebnis je Code protokollieren: ______ (nicht lesbar ⇒ melden, nicht still beheben). |
| 104.6 | Negativ: Platz ohne Beschreibung und Zone; Platz mit 200 Zeichen Beschreibung | Keine Lücken-Artefakte; lange Beschreibung nach zwei Zeilen abgeschnitten, nichts ragt über den Rand oder auf eine zweite Seite. |
| 104.7 | Negativ: Benutzer ohne Stammdaten-Lesezugriff ruft `/StorageLocations/PrintLabels` auf | Zugriff verweigert (unverändert). |
| 104.8 | Ausdruck: blaue Leiste „Drucken/Zurück“ | Erscheint nicht im Ausdruck. |
| 104.9 | Schalter wieder aus, Speichern, Sammeldruck | A4 wie in 104.1. |

## TS-105 — Vormontage-Listen: neue Spalten Arbeitsbereich, HauptFA, Kunde, FE-Termin, Bemerkung PN, Fertigungsinfo PN (v1.72.0)

Bezug: Spec [[2026-11-06-lagerplatz-etikett-rolle-und-vormontage-felder-spec]], Teil 2 (nimmt [[2026-10-06-vormontage-abteilung-spalte-filter-spec]] auf).
Nur **IDEAL**. Querverweise TS-61 (Vormontage), TS-86 (CSV-Export), TS-91 (Suchsyntax), TS-98 (Zahnrad).
Automatisiert: `VormontageNeueSpaltenTests` (Formatierer, Filter Sicht 1, Vorfilter Sicht 2, Drift-Wächter), bestehende `VormontageServiceTests`.
**Hinweis:** Dass „Arbeitsbereich“ der Sage-Spalte „Abteilung“ in „Fertigungsaufträge bearbeiten“ entspricht, ist **ungeprüft** (P5 entfällt,
Gerald 2026-11-06) – Schritt 105.1 vergleicht nur informativ.

**Vorbedingungen:** IDEAL-Testsystem, `FaHierarchyVormontageAktiv = true`, Benutzer mit `vorbau` (oder admin) und einer ohne. Daten nach
aktuellem Sync; ein HauptFA mit zwei Kopfzeilen (Kombigerät, P8) und Positionen mit verschiedenem Arbeitsbereich; P7 bekannt
(IDEAL-Test: BemerkungPN 6 gefüllt, FertigungsInfoPN 0).

| Nr | Schritte | Erwartung |
|---|---|---|
| 105.1 | „Einzelne Teile“ öffnen | Spalten: HauptFA, **Arbeitsbereich**, Haupt-Artnr., Artnr., Matchcode, Sollmenge, Fertigungmenge, VMBedarf, Material, **Kunde**, **FE-Termin**, **Bemerkung PN**, **Fertigungsinfo PN**. Arbeitsbereich je Position (zwei Positionen einer Gruppe können verschieden sein). Kunde/FE-Termin in allen Positionen einer Gruppe gleich wie im Gruppenkopf. Informativ: Arbeitsbereich mit Sage-Spalte „Abteilung“ vergleichen (Befund notieren). |
| 105.2 | Leere Werte | Position ohne Arbeitsbereich/Bemerkung: leere Zelle, kein Platzhalter. Gruppenkopf unverändert. |
| 105.3 | Filter je neuer Spalte (Arbeitsbereich `H1`, Kunde, Bemerkung PN) mit ENTER, dann `!H1`, Filter entfernen | URL enthält `colf_<key>=`; nur passende Positionen, Gruppe bleibt mit genau diesen Positionen; Seitenzahl/Anzahl Gruppen passt; `!H1` blendet nur betroffene Positionen aus. |
| 105.4 | FE-Termin-Filter `>=01.12.2026`, dann `01.12.2026...31.12.2026` | Filtert wie andere Datumsspalten. Kombigerät zeigt „A, B“ bzw. zwei Termine aufsteigend; der Datumsfilter vergleicht den **früheren** Termin. Positionszahl des Kombigeräts unverändert. |
| 105.5 | „Summiert“ öffnen | Spalten: Matchcode, **Arbeitsbereich**, Summe Sollmenge, Summe Fertigungmenge, **HauptFA**, **Kunde**, **FE-Termin**, **Bemerkung PN**, **Fertigungsinfo PN**. Je Matchcode eine Zeile, neue Spalten zeigen die verschiedenen Werte (", "-getrennt). Hinweistext: „… summierte Mengensicht des gewählten VM-Bedarfs …“. |
| 105.6 | „Summiert“: Filter Arbeitsbereich `H1`, dann HauptFA = eine Nummer | Summen sinken auf die Mengen der passenden Positionen (Vergleich mit „Einzelne Teile“ gleicher Filter); Matchcodes ohne passende Position verschwinden. Filter auf Summe Sollmenge wirkt weiter auf die Summe. |
| 105.7 | Zahnrad beider Sichten: jede neue Spalte aus- und einblenden, verschieben, neu laden; Benutzer mit alten gespeicherten Spalten | Wahl bleibt, kein Fehler (Prefs-API nicht 400). Alte Prefs: neue Spalten erscheinen an der Nachbarposition, sichtbar. |
| 105.8 | CSV-Export beider Sichten mit allen Spalten, dann mit ausgeblendetem „Kunde“ | Neue Spalten mit angezeigtem Text; ausgeblendete fehlen. |
| 105.9 | Reiterwechsel und KW-Filter kombiniert mit einem Spaltenfilter | Ergebnisse konsistent; Reiter und KW verhalten sich wie bisher. |
| 105.10 | Negativ: Benutzer ohne Vorbau-Zugriff; Toggle aus | Zugriff verweigert; Toggle aus ⇒ Weiterleitung Startseite. |
| 105.11 | Regression: Kommissionierlisten, Beschichtung, FA-Liste, FA-Struktur; AKE (Toggle aus) | Unverändert. |

## TS-106 — Sage-Abgleich über SData (v1.73.0)

Bezug: Spec [[2026-11-06-sage-sdata-stammdaten-sync-spec]] (Epic). Jede Etappe ergänzt hier ihr Unterszenario (TS-106.1 ff. laut Spec);
TS-106.0 deckt Etappe E0 ab. Standort **beide** (AKE und IDEAL).
Automatisiert: `SageSdataPagingTests` (Paging, orderBy-Pflicht, Fehler werfen, Sperrliste, Grenze 100.000, Bool/Dezimal), `SageQuelleTests`
(Schalter, Weiche, Vergleich, SData-Ziel, DI), Drift-Guard `ServiceSettingDefinitionsTests`.

### TS-106.0 — E0: Schalter vorhanden, Default View, Verhalten unverändert

**Vorbedingungen:** Test-DB mit Bündel-Stand v1.73.0, Web und Dienst neu gestartet (Seed der ServiceSettings beim Web-Start), Admin-Benutzer;
Aktivitäts-Protokoll offen; zum Vergleich der Zählerstände ein Abgleich-Lauf **vor** dem Deploy notiert (Lagerplatz, Lagerbestand, Artikel,
Werkbank/Arbeitsgang, FA-Hierarchie – je nach aktivierten `Sync:*Enabled`).

| Nr | Schritte | Erwartung |
|---|---|---|
| 106.0.1 | `/ServiceSettings` öffnen | Neue Gruppe „Sage-Abgleich (SData)“ mit `Sync:Quelle:Lagerplaetze`, `…:Lagerbestand`, `…:Artikel`, `…:FaStruktur`, `…:Arbeitsplaetze`, `…:LagerbuchungLookup` (je Wert `View`), `Sync:ArtikelBenutzerfelder` (15 USER-Felder), `Sync:PpsMitarbeiterEnabled`, `Sync:StillstandsgruendeEnabled`, `Sync:KreditorenEnabled`, `Sync:DebitorenEnabled`, `Sync:ArtikelLieferantEnabled` (je aus). Jede Zeile mit Beschreibung. Kein `Sync:Quelle:Stueckliste`. |
| 106.0.2 | Beschreibung `SageLagerbuchungSslZertifikatPruefen` lesen | Nennt „Lagerbuchung + FA-Ansicht Sage live + Stammdaten-Abgleich“. Wert unverändert. |
| 106.0.3 | Einen Sync-Zyklus des Dienstes abwarten | Alle Abgleiche laufen wie vor dem Deploy: gleiche Count-Keys, Zählerstände plausibel gleich, keine zusätzliche Fehlermail, **kein** Eintrag „Sage-Quellenvergleich“. |
| 106.0.4 | Aktivitäts-Protokoll: Auswahlliste „Service“ öffnen | Eintrag „Sage-Quellenvergleich (View/SData)“ vorhanden (deutscher Name); Filter darauf liefert 0 Zeilen. |
| 106.0.5 | Hilfe → Änderungen; Fußzeile/Versionsanzeige | Eintrag v1.73.0 vom 06.11.2026; Version 1.73.0 in Web und Dienst (Dienst-Log beim Start). |
| 106.0.6 | Dienst-Start-Log prüfen | Dienst startet ohne DI-Fehler (der SData-Lese-Client ist registriert, wird aber noch von keinem Abgleich benutzt). |
| 106.0.7 | Negativ: `Sync:Quelle:Lagerplaetze` auf `Neu` bzw. `sdata ` (mit Leerzeichen) stellen, einen Zyklus abwarten | In E0 **keine** Wirkung und keine Warnung: noch liest kein Abgleich den Schalter (Weichen folgen ab E1, dort wirkt der Wert wie `View` + genau eine Warnung je Lauf). Danach Wert auf `View` zurück. |
| 106.0.8 | Negativ: `Sync:KreditorenEnabled = true` setzen, einen Zyklus abwarten | Keine Wirkung in E0 (Abgleich folgt mit E8c), kein Fehler. Danach auf `false` zurück. |
| 106.0.10 | Nachtrag B6 (2026-11-07): `/ServiceSettings` an AKE (`Sync:ArticlesViewName` leer) und an IDEAL (gesetzt) | `Sync:Quelle:FaStruktur`: gelber Hinweis „SData ist hier gesperrt bis P-F2 …“ an beiden Standorten. `Sync:Quelle:Artikel`: **nur** an IDEAL „gesperrt bis P-R1 (Artikel-View '…' gesetzt …)“, an AKE kein Hinweis. Steht der Wert auf `SData`, sagt der Hinweis „Der eingestellte Wert SData wirkt deshalb wie View …“. Übrige Quellen ohne Hinweis. Text ist lesbar (Kontrast wie TLS-Hinweis), nicht nur Farbe. |
| 106.0.11 | Nachtrag B6: gesperrte Quelle auf `SData`, ein Zyklus | Im Lauf des Bereichs (z. B. „FA-Hierarchie-Sync“) steht statt „Quelle: View“ die **Warnung** „Quelle: View – Sync:Quelle:FaStruktur = SData ist gesperrt bis P-F2 … – Quelle View.“; zusätzlich wie bisher die Warnung im Lauf „Sage-Quellenvergleich“. |
| 106.0.9 | Regression AKE (Default-Schalter) | Artikel-, Lagerplatz-, Lagerbestand-Abgleich, Sage-Lagerbuchung und FA-Ansicht „Sage live“ (falls aktiv) wie vorher. |

### TS-106.1 — E1: Lagerorte und Lagerplätze über SData

**Vorbedingungen:** wie TS-106.0; Migration `SQL/98_AddStorageAreasAndStorageLocationSageFields.sql` eingespielt; `Sync:LagerplaetzeEnabled = true`;
`SData:BaseUrl`/`SData:Dataset` auf `sagetest01` / `ake_TEST2026;1`, Zugangsdaten `SageLagerbuchung:Username/Password` in appsettings;
`SageLagerbuchungSslZertifikatPruefen` passend zum Testsystem; Sync-Fehlermail aktiv mit Empfänger. Ein **manueller** Lagerplatz mit
demselben Code wie ein Sage-Platz (z. B. Code des Platzes PlatzID 11 „HR 0 0 0“) angelegt. Vor dem Test notieren (SQL):
`SELECT COUNT(*) FROM StorageLocations WHERE Source='Sage' AND IsActive=1`, sowie `IstBuchbar`/`SageBuchungErlaubt` von drei Sage-Plätzen.
Automatisiert: `LagerplatzSdataTests` (Reader-Mapping, Weiche alle Modi inkl. SData-Fehler im Vergleich, Nicht-null-Regel, Lagerorte, G10, DI),
`StorageLocationsControllerTests.Index_ColumnFilter_LagerortUndSperreAusSage_v1_73`, `SageQuelleTests`.

| Nr | Schritte | Erwartung |
|---|---|---|
| 106.1.a | Default: `Sync:Quelle:Lagerplaetze` = `View`, einen Zyklus abwarten | Lauf „Lagerplatz“ mit denselben Count-Keys/Zählern wie vor dem Deploy, eine Info-Zeile „Quelle: View“, **kein** Lauf „Sage-Quellenvergleich“, keine Mail. `StorageAreas` leer, neue Spalten der Lagerplätze leer. |
| 106.1.a-neg | Wert `Neu` eintragen, einen Zyklus | Wirkt wie `View` („Quelle: View“); genau eine Warnung im Lauf „Sage-Quellenvergleich“ mit Text „Unbekannter Wert 'Neu'“. Danach zurück auf `View`. |
| 106.1.b | `Vergleich`, einen Zyklus | Lagerplätze unverändert wie bei `View` (DB-Stichprobe, `StorageAreas` weiter leer). Lauf „Sage-Quellenvergleich“ (Referenz `Lagerplaetze`) mit „… abweichende Schlüssel (View n, SData m)“ und höchstens 20 Beispielen; erwartet am AKE-Test 0 Abweichungen bei Zone/Description/SageLagerplatzId (AK 8). |
| 106.1.b-neg | In Sage eine Platzbezeichnung ändern (Testsystem), Zyklus mit `Vergleich` | Abweichung erscheint als Beispiel „[Kurzbezeichnung] Description: View '…' / SData '…'“. Danach in Sage zurückändern. |
| 106.1.c | `SData`, einen Zyklus | Lauf „Lagerplatz“ mit „Quelle: SData“ und Info „Lagerorte: x neu, …“. `StorageAreas` enthält je aktivem Lagerort eine Zeile (AKE-Test 36 abzüglich inaktiver); jeder aktive Sage-Platz hat `StorageAreaId`; `SageIstGesperrt`/`…Inventur` gesetzt. `IstBuchbar`/`SageBuchungErlaubt` der drei notierten Plätze **unverändert**. Der manuelle Platz bleibt Konflikt (Warnung) ohne Lagerort. |
| 106.1.c2 | `/StorageLocations` öffnen | Neue Spalten „Lagerort“ (Bezeichnung des Lagerorts, ohne Bezeichnung die Lagerkennung) und „Sperre (Sage)“ (Badge „Gesperrt“ bzw. „Inventur“, Text lesbar ohne Farbe). Spaltenfilter „Lagerort“ = Teil einer Bezeichnung filtert über alle Seiten; Filter „Sperre“ = `Gesperrt` zeigt nur gesperrte. Zahnrad über der Tabelle: Spalten ein-/ausblenden, nach Neuladen erhalten. Mit „Auch inaktive zeigen“ erscheint die Spalte Aktiv weiterhin. |
| 106.1.d | Zurück auf `View`, einen Zyklus | „Quelle: View“; per SQL vor/nach: `StorageAreaId`, `SageIstGesperrt*`, `SageBreite…SageTragkraft` unverändert (nicht genullt), `StorageAreas` unverändert (kein `SageMissingSince`). Keine Deaktivierungen aus dem Wechsel. |
| 106.1.e | `SData`, `SData:BaseUrl` auf nicht erreichbaren Host (bzw. falsches Passwort), einen Zyklus | Lauf „Lagerplatz“ endet mit Fehler („Quelle: SData“, Sage-Connection fehlgeschlagen: SData …), **keine** Deaktivierung (Zähler aktiver Sage-Plätze unverändert), Fehlermail „Lagerplatz-Sync“. Andere Abgleiche des Zyklus laufen weiter. |
| 106.1.e2 | Wie 106.1.e, aber Modus `Vergleich` | Lauf „Sage-Quellenvergleich“ endet mit Fehler „Lagerplaetze: SData nicht lesbar …“, Fehlermail „Sage-Quellenvergleich Lagerplaetze“; Lauf „Lagerplatz“ **erfolgreich** aus den View-Daten („Quelle: Vergleich“). |
| 106.1.e3 | Negativ: Antwort ist HTML (z. B. Proxy-Seite statt SData), Modus `SData` | Fehler wie 106.1.e, nicht „0 Zeilen“; keine Deaktivierung. |
| 106.1.f | Negativ: Lagerort in Sage inaktiv setzen (Testsystem), Zyklus mit `SData` | Seine Plätze werden wie heute deaktiviert (Info je Platz); der Lagerort bleibt in `StorageAreas` mit `SageMissingSince` und `IsActive = 0` (Info „nicht (mehr) aktiv in Sage“). Wieder aktiv setzen → nächster Lauf leert `SageMissingSince`. |
| 106.1.g | Rechte: Benutzer nur mit Stammdaten-Lesen öffnet `/StorageLocations` | Liste mit den neuen Spalten sichtbar, keine Bearbeiten-Knöpfe (unverändert). |

### TS-106.2 — E2: Lagerbestand über SData

**Vorbedingungen:** wie TS-106.1 (SData-Endpunkt, Zugangsdaten, TLS-Schalter, Fehlermail aktiv); `Sync:LagerbestandEnabled = true`;
`WorkerSettings:SyncDryRun = false`; Lagerplätze vorher abgeglichen. Keine Migration. Vor jedem Schritt Zähler notieren (SQL):
`SELECT MovementType, COUNT(*) FROM StockMovements WHERE CreatedBy='system:sync' GROUP BY MovementType`. Testartikel 00722001
(AKE-Test: 200 lfm auf „LL 2 28 0“).
Automatisiert: `BestandSdataTests` (Reader-Mapping/Summen wie SQL, Chargen/Seriennummern nur gezählt, Fehler und falsch gemappte Zeilen
werfen, Weiche alle Modi inkl. SData-Fehler im Vergleich, kein Nullsetzen bei Lesefehler, G10-Mail, DI), `LagerbestandSyncServiceTests`.

| Nr | Schritte | Erwartung |
|---|---|---|
| 106.2.a | Default: `Sync:Quelle:Lagerbestand` = `View`, einen Zyklus | Lauf „Lagerbestand“ mit denselben Count-Keys (`einbuchungen`, `ausbuchungen`, `nullgesetzt`, `uebersprungen`, `fehler`) wie vor dem Deploy, Info-Zeile „Quelle: View“, **kein** Lauf „Sage-Quellenvergleich“, keine Mail. |
| 106.2.a-neg | Wert `Neu` eintragen, einen Zyklus | Wirkt wie `View`; genau eine Warnung „Unbekannter Wert 'Neu'“ im Lauf „Sage-Quellenvergleich“. Danach zurück auf `View`. |
| 106.2.b | `Vergleich`, einen Zyklus | Buchungen nur aus der View (Zähler wie bei `View`). Lauf „Sage-Quellenvergleich“ (Referenz `Lagerbestand`) mit „… abweichende Schlüssel (View n, SData m)“; erwartet am AKE-Test **0 Abweichungen** (AK 10). Lauf „Lagerbestand“ zeigt „Quelle: Vergleich“ und Info-Zeilen „Quelle SData: LagerbestaendeArtikelInfo, … Zeile(n)“, „Chargen (nur gezählt, nicht gespeichert): n Zeile(n) …“, „Seriennummern …“. |
| 106.2.b-neg | In Sage den Bestand von 00722001 auf einem Platz ändern (Testsystem, z. B. Inventurbuchung), sofort einen Zyklus mit `Vergleich` | Wenn die Änderung zwischen den beiden Lesevorgängen liegt oder einer Quelle nachhinkt: Beispiel „[00722001 @ …] Bestand: View '…' / SData '…'“; sonst 0 Abweichungen. Buchung nur aus der View. |
| 106.2.c | `SData` direkt nach einem View-Lauf, einen Zyklus | Lauf „Lagerbestand“ mit „Quelle: SData“; **keine** neue `SageEinbuchung`/`SageAusbuchung`, `nullgesetzt` = 0 (Zähler-SQL vor/nach gleich, AK 10). |
| 106.2.c-neg | Bestand 00722001 in Sage ändern, einen Zyklus mit `SData` | Genau **eine** Korrekturbuchung für (00722001, Platz) mit Notiz „Sage-Korrektur: WMS=…, Sage=…“, wie beim View-Weg. |
| 106.2.c-cap | `Sync:LagerbestandNullsetzenMaxPerRun = 1`, in Sage zwei Sage-Bestände eines WMS-geführten Platzes auf 0 ausbuchen/entfernen, Zyklus mit `SData` | Wie heute: Warnung „Nullsetzen uebersprungen: Cap ueberschritten“, Fehlermail „Lagerbestand-Nullsetzen: Cap ueberschritten“, nichts auf 0 gesetzt. Cap danach zurück (Default 100). |
| 106.2.d | Zurück auf `View`, einen Zyklus | „Quelle: View“; keine Korrekturbuchungen aus dem Wechsel (Zähler-SQL vor/nach). |
| 106.2.e | `SData`, `SData:BaseUrl` auf nicht erreichbaren Host (bzw. falsches Passwort), einen Zyklus | Lauf „Lagerbestand“ endet mit Fehler („Quelle: SData“, „Sage-Connection fehlgeschlagen: SData …“), **keine** Buchung, **kein** Nullsetzen (Zähler-SQL unverändert), Fehlermail „Lagerbestand-Sync“. Andere Abgleiche des Zyklus laufen weiter. |
| 106.2.e2 | Wie 106.2.e, aber `Vergleich` | Lauf „Sage-Quellenvergleich“ endet mit Fehler „Lagerbestand: SData nicht lesbar …“ + Fehlermail „Sage-Quellenvergleich Lagerbestand“; Lauf „Lagerbestand“ **erfolgreich** aus der View. |
| 106.2.e3 | Negativ: Feldnamen passen nicht (erste UAT-Prüfung, P-S1) — Lauf mit `Vergleich` | Liefert Sage HTTP 500 (Meldung enthält das erzeugte SQL) oder Zeilen ohne Artikelnummer/PlatzID/Bestand, endet der Vergleich mit Fehler „… Feldnamen der Ressource prüfen (P-S1)“, **nie** „0 Zeilen“ und nie Nullsetzen. Befund an die Entwicklung (Feldnamen von `LagerbestaendeArtikelInfo`). |
| 106.2.e4 | Negativ: Chargen-/Seriennummern-Ressource nicht lesbar | Nur eine Warnung „Chargen nicht gezählt – …“ im Lauf „Lagerbestand“; der Bestand-Abgleich läuft normal. |

### TS-106.3 — E3a: Artikel über SData (Reader, Vergleich, Satellit `ArticleSageDetails`)

**Vorbedingungen:** wie TS-106.1 (SData-Endpunkt AKE-Test, Zugangsdaten, TLS-Schalter, Fehlermail aktiv); `SQL/99_AddArticleSageDetails.sql`
eingespielt (Tabelle `ArticleSageDetails`, Index `UQ_ArticleSageDetails_ArticleId`, History-Zeile `20261106184123_AddArticleSageDetails`);
`WorkerSettings:SyncDryRun = false`. Testartikel 00722001 (AKE-Test: Meldebestand 85, Hauptlieferant `K31013`, WBZ 14) und **86006871** (Fertigungsartikel, in 427 PPS-Ressourcenpositionen → in der AKE-Menge; ersetzt 86003871, Gerald 2026-11-07).
Zählung vorher notieren: `SELECT COUNT(*) FROM ArticleSageDetails` (nach Deploy 0) und
`SELECT ArticleNumber, Description, Unit, ArticleGroup, ReorderLevel, SagePrimaryStorageLocation, Matchcode, ModifiedAt FROM Articles WHERE ArticleNumber IN ('00722001','86006871')`.
**Nachtrag G16 neu (Gerald 2026-11-07, gilt vor den Zeilen unten):** An **AKE** (`Sync:ArticlesViewName` leer) ist `SData` frei – der
SData-Reader liest dieselbe Menge wie der AKE-SQL-Filter (`SageArticleSql.LegacySql`) und füllt `ArticleSageDetails` + Benutzerfeld-Merkmale.
An **IDEAL** (`Sync:ArticlesViewName` gesetzt) bleibt `SData` bis P-R1 gesperrt (106.3.c gilt dort unverändert). Lesend geprüft
2026-11-07 (Sage-DB `ake_TEST2026`): AKE-SQL-Menge = **100.600** Artikel; **86003871** ist darin **nicht** enthalten (Fertigungsartikel,
`IstBestellartikel = 0`, in keiner PPS-Ressourcenposition) – an AKE bekommt 86003871 daher weder über View noch über SData eine Zeile; deshalb Testartikel **86006871**.
Wenn die Datenlage am Testsystem abweicht, gilt das Ergebnis von 106.3.f. Automatisiert: `ArtikelSdataTests` (Mapping wie SQL-Select inkl. Gruppe als reiner Code, Meldebestand 0 = leer,
Ressourcenummer ohne Artikelstamm, Pflichtfeld-Prüfung, Blockabfragen `in (…)`, Weiche View/SData-gesperrt/Vergleich, Satellit-Schreibregel
inkl. Audit und View-Lauf ohne Änderung, Namensgleichheit DTO↔Entität, DI), `SageImportServiceTests`, `SageArticleSqlTests` (SQL-Text unverändert).

| Nr | Schritte | Erwartung |
|---|---|---|
| 106.3.a | Default: `Sync:Quelle:Artikel` fehlt bzw. `View`, einen Zyklus (AKE- und IDEAL-Test-DB) | Lauf „Artikel“ mit denselben Count-Keys (`gelesen`, `neu`, `aktualisiert`, `hauptlagerplatz_fehlt`) und Zählerständen wie vor dem Deploy, zusätzlich nur die Info-Zeile „Quelle: View“; **kein** Lauf „Sage-Quellenvergleich“, keine Mail; `ArticleSageDetails` bleibt leer. |
| 106.3.a-neg | Wert `Neu` eintragen, einen Zyklus | Wirkt wie `View`; genau eine Warnung „Unbekannter Wert 'Neu' für Sync:Quelle:Artikel …“ im Lauf „Sage-Quellenvergleich“. Zurück auf `View`. |
| 106.3.a-view | Nur IDEAL-Test: `Sync:ArticlesViewName` auf einen nicht existierenden View-Namen, einen Zyklus mit `View` | Wie vor E3a: Warnung „… — Sync uebersprungen.“, Lauf erfolgreich mit `gelesen = 0`, Zusatz „Quelle nicht vorhanden“, keine Mail. Namen zurücksetzen. |
| 106.3.b | `Vergleich`, einen Zyklus | Lauf „Artikel“: Count-Keys/Zähler wie bei `View`, „Quelle: Vergleich“ und Info-Zeilen „Quelle SData: Artikel, …“, „… PpsRessourcenPositionenInfo …“, „… ArtikelVariantenExtended, n Block/Blöcke …“, „… LagerplaetzeInfo …“. Lauf „Sage-Quellenvergleich“ (Referenz `Artikel`) mit „… abweichende Schlüssel (View n, SData m)“ und ≤ 20 Beispielen (Feld `Description`, `Unit`, `ArticleGroup`, `ReorderLevel`, `PrimaryStorageLocation`, `Matchcode`). Erwartet am AKE-Test (Legacy-Select): keine `ArticleGroup`-Abweichung im Format (beide reiner Code, z. B. `940`). `ArticleSageDetails` bleibt leer (DB-Zählung), `Articles.ModifiedAt` der Testartikel unverändert, sofern Sage nichts geändert hat. **Befund festhalten** (Anzahl Abweichungen, Beispiele) – er entscheidet zusammen mit P-R1 über die Freigabe. |
| 106.3.b-neg | Vergleich: in Sage (Testsystem) die Bezeichnung von 00722001 ändern und sofort einen Zyklus laufen lassen | Entweder Beispiel „[00722001] Description: View '…' / SData '…'“ (wenn die View nachhinkt) oder 0 Abweichungen; `Articles` erhält den Wert ausschließlich aus der View. Bezeichnung zurücksetzen. |
| 106.3.c | `SData` (gesperrt bis P-R1), einen Zyklus | Wirkt wie `View`: Warnung „Sync:Quelle:Artikel = SData ist gesperrt bis P-R1 – Quelle View.“ im Lauf „Sage-Quellenvergleich“, Lauf „Artikel“ mit „Quelle: View“, `ArticleSageDetails` bleibt leer. (Nach Freigabe P-R1 + Entsperren: 00722001 hat eine Zeile mit `Hauptlieferant = K31013`, `Wiederbeschaffungszeit = 14`, `Articles.ReorderLevel = 85`, `ArticleGroup` reiner Code; AK 11.) |
| 106.3.f | AKE (Nachtrag G16 neu): `Vergleich`, ein Zyklus | Lauf „Sage-Quellenvergleich“ (Referenz `Artikel`): „Schlüssel View“ = „Schlüssel SData“ (≈ 100.600 am AKE-Test), `abweichend` nur mit erklärbaren Beispielen (Gruppenformat/Nachlauf), **keine** Mengen-Differenz. Lauf „Artikel“ Count-Keys wie bei `View`. Weicht die Schlüsselzahl ab: **nicht** auf `SData` umschalten, Beispiele an Dev. |
| 106.3.g | AKE: danach `SData`, ein Zyklus | Lauf „Artikel“ mit „Quelle: SData“ (Info, keine Warnung), dieselben Count-Keys; `SELECT COUNT(*) FROM ArticleSageDetails` ≈ `gelesen`; 00722001: `Hauptlieferant = K31013`, `Wiederbeschaffungszeit = 14`, Artikelinfo zeigt „Sage-Zusatzfelder“ und die Text-Merkmale aus Sage (TS-106.4). 86006871: Zeile vorhanden, `IstFertigungsartikel = 1`, `IstBestellartikel = 0` (kam nur über die Ressourcenpositionen in die Menge – belegt den Filter-Fix TS-106.3.i). Kein Schreibaufruf an Sage. |
| 106.3.h | IDEAL: `SData`, ein Zyklus | Wie 106.3.c: wirkt wie `View`, Warnung „gesperrt bis P-R1 (Artikel-View '…' gesetzt …)“ im Lauf „Artikel“ (B6) und im Lauf „Sage-Quellenvergleich“; `ArticleSageDetails` unverändert. |
| 106.3.i | Nachtrag Filter-Fix (2026-11-07): `Vergleich` an AKE, Info-Zeile „Quelle SData: PpsRessourcenPositionenInfo …“ | Zeilen ≈ 240.552 (nicht 0); Filter `Ressourcenummer like '_%'`. Negativ (nur automatisiert, `LesenAsync_RessourcenLeer_Wirft_FilterNieNe`): liefert die Ressource 0 Zeilen, endet der SData-Lesevorgang mit Fehler „0 Zeilen … Filter prüfen“ statt still nur Bestellartikel zu liefern. |
| 106.3.d | Artikelinfo `/Articles/Info?articleNumber=00722001` mit leerem Satelliten | *Überholt durch TS-112.6 (v1.80.0): statt fehlender Karte erscheint ein Hinweis.* Kein Abschnitt „Sage-Zusatzfelder“; Rest der Seite unverändert. |
| 106.3.d2 | Nur Testsystem: eine Satelliten-Zeile per SQL anlegen (`INSERT INTO ArticleSageDetails (ArticleId, Hauptlieferant, Wiederbeschaffungszeit, IstBestellartikel, Laenge, Langtext, CreatedAt, CreatedBy, CreatedByWindows) SELECT Id, 'K31013', 14, 1, 1200.5, N'Zeile 1' + CHAR(10) + N'<b>Zeile 2</b>', GETUTCDATE(), 'UAT', 'UAT' FROM Articles WHERE ArticleNumber = '00722001'`), Artikelinfo öffnen | Karte „Sage-Zusatzfelder“ mit „nur lesend · aus Sage gelesen am …“, Gruppen Stamm/Lager/Bestand und Dispo/Fertigung/Kennzeichen/Maße; Hauptlieferant K31013, Wiederbeschaffungszeit 14, Bestellartikel „Ja“, Länge 1200,5 (leere Felder „—“); Langtext zweizeilig, `<b>` als Text (nicht fett). Keine Bearbeiten-Elemente. Auf Telefonbreite untereinander lesbar. Danach `DELETE FROM ArticleSageDetails WHERE CreatedBy = 'UAT'`. |
| 106.3.d3 | Rückfall: mit der Zeile aus 106.3.d2 einen Zyklus mit `View` und einen mit `Vergleich` | Zeile unverändert (`ModifiedAt` leer, Werte gleich) – View- und Vergleichs-Läufe fassen den Satelliten nie an. |
| 106.3.e | `Vergleich`, `SData:BaseUrl` auf nicht erreichbaren Host (bzw. falsches Passwort), einen Zyklus | Lauf „Sage-Quellenvergleich“ endet mit Fehler „Artikel: SData nicht lesbar …“ + Fehlermail „Sage-Quellenvergleich Artikel“; Lauf „Artikel“ **erfolgreich** aus der View, Zähler wie bei `View`. |
| 106.3.e2 | Negativ: Filter/Feldnamen passen nicht (erste UAT-Prüfung, P-S1/P-R1) — Lauf mit `Vergleich` | Liefert Sage HTTP 500 (Meldung enthält das erzeugte SQL, z. B. zu `IstBestellartikel eq -1` oder `Ressourcenummer ne ''`/`BelPosID`) oder Zeilen ohne Artikelnummer, endet der Vergleich mit Fehler (… „Feldnamen prüfen (P-S1)“), **nie** „0 Zeilen“; der Artikel-Abgleich läuft aus der View. Befund an die Entwicklung. |
| 106.3.e3 | Negativ: Hauptlagerplatz ohne WMS-Platz (Artikel mit Sage-Hauptlagerplatz, der im WMS fehlt), Zyklus mit `View` | Wie heute: Warnung „Hauptlagerplatz '…' fuer Artikel … nicht als WMS-Lagerplatz gefunden — FK bleibt leer“, Count-Key `hauptlagerplatz_fehlt` zählt ihn. |

### TS-106.4 — E3b: Artikel-Benutzerfelder als Text-Merkmal

**Vorbedingungen:** wie TS-106.3; zusätzlich `SQL/100_AddArticleAttributeValueTextValue.sql` eingespielt (Spalte
`ArticleAttributeValues.TextValue nvarchar(1000) NULL`, History-Zeile `20261106185149_AddArticleAttributeValueTextValue`; zweiter Lauf des
Skripts ändert nichts). Ein Benutzer mit Stammdaten-Schreibrecht und einer mit nur Lesen. `Sync:ArtikelBenutzerfelder` mit Default
(15 Felder). Ein manuelles Merkmal „Maschine“ gibt es **nicht** (außer in 106.4.e).
**Wichtig:** Definitionen und Werte schreibt nur der SData-Weg; `Sync:Quelle:Artikel = SData` ist bis P-R1 gesperrt. Bis dahin: Lesen per
`Vergleich` (106.4.b) und die Oberfläche über eine per SQL angelegte Sage-Definition (106.4.c ff.). Automatisiert: `ArtikelBenutzerfelderTests`
(Liste/Validierung, 15 Default-Felder gültig, Werte als Text, unbekanntes Feld per HTTP 500 einzeln ermittelt, echter 500 wirft,
Definition-Anlage idempotent, nur SData-Zeilen, Konflikt, Kürzung, Leeren entfernt, Audit), `ArticleAttributesControllerTests` (Sperre
Name/Löschen/Optionen, kein manuelles Text-Merkmal, Speichern der Artikel-Bearbeitung lässt Sage-Werte unberührt),
`ArticleAttributeTextValueMigrationSqlTests` (Migration, SQL/100, FreshInstall an zwei Stellen).

| Nr | Schritte | Erwartung |
|---|---|---|
| 106.4.a | `View` (Default), einen Zyklus | Lauf „Artikel“ unverändert, **keine** Zeile „Sage-Benutzerfelder (Merkmale) …“; `SELECT COUNT(*) FROM ArticleAttributeDefinitions WHERE SyncSource = 'Sage'` = 0. |
| 106.4.b | `Vergleich`, einen Zyklus | Zusätzliche Info-Zeile „Quelle SData: Artikel, n Block/Blöcke …“ für die Benutzerfeld-Abfrage; Merkmale und Werte unverändert (DB-Zählung wie 106.4.a). Warnung „Benutzerfeld … unbekannt“ nur, wenn ein Feld am AKE-Test fehlt – **Befund festhalten** (IDEAL-Felder erst am IDEAL-Endpunkt prüfbar, G2). |
| 106.4.c | Nach Entsperren (P-R1) `SData`, einen Zyklus. Bis dahin Testsystem: `INSERT INTO ArticleAttributeDefinitions (Name, AttributeType, SortOrder, IsActive, SyncSource, SyncFieldName, CreatedAt, CreatedBy, CreatedByWindows) VALUES (N'Maschine', 2, 99, 1, 'Sage', 'USER_Maschine', GETUTCDATE(), 'UAT', 'UAT')` und `INSERT INTO ArticleAttributeValues (ArticleId, ArticleAttributeDefinitionId, TextValue, CreatedAt, CreatedBy, CreatedByWindows) SELECT a.Id, d.Id, N'M-200 <b>x</b>', GETUTCDATE(), 'UAT', 'UAT' FROM Articles a CROSS JOIN ArticleAttributeDefinitions d WHERE a.ArticleNumber = '00722001' AND d.SyncFieldName = 'USER_Maschine'` | Nach SData-Lauf: 15 Definitionen mit `SyncSource = Sage`, Typ Text (2), Name ohne `USER_`, Info-Zeile „Sage-Benutzerfelder (Merkmale): 15 Merkmal(e) angelegt, n Wert(e) neu …“. Artikelliste: Spalte „Maschine“ zeigt `M-200 <b>x</b>` als Text (nicht fett); Artikelinfo 00722001: Zeile „Maschine“ mit dem Wert, lange Werte umbrechen. |
| 106.4.c2 | Zweiter SData-Lauf ohne Änderung in Sage | „0 Merkmal(e) angelegt, 0 Wert(e) neu, 0 aktualisiert, 0 entfernt“; `ModifiedAt` der Werte unverändert. |
| 106.4.d | `/ArticleAttributes` als Schreib-Benutzer | „Maschine“: Typ-Badge „Text“, Kennzeichen „aus Sage“ + „Benutzerfeld `USER_Maschine` – Name und Werte nicht änderbar“; Namensfeld nur lesbar, statt Löschen ein gesperrtes Schloss-Symbol. Reihenfolge ändern + Speichern → Erfolgsmeldung, neue Position in der Artikelliste; Aktiv aus → Spalte verschwindet aus Liste/Info/Bearbeitung. Spaltenfilter Typ = `Text` zeigt nur Sage-Merkmale. Tastatur: Fokus sichtbar, Badges lesbar (Kontrast). Als Nur-Lesen-Benutzer: keine Aktionen. |
| 106.4.d-neg | Namen per Entwicklertools (readonly entfernen) auf „Anders“ ändern und speichern; Löschen per nachgebautem POST `DeleteDefinition`; neues Merkmal mit Typ `2` per POST | Gelbe Meldung „Merkmal 'Maschine' kommt aus Sage (Benutzerfeld USER_Maschine) — Name, Typ, Optionen und Werte sind hier nicht aenderbar.“; nichts geändert/gelöscht. Typ 2 → „Typ nicht erlaubt …“, kein Merkmal angelegt. |
| 106.4.d2 | Artikel 00722001 bearbeiten | „Maschine“ mit Kennzeichen „aus Sage“, Wert im nur lesbaren Feld, Hinweis „Wird vom Sage-Abgleich gepflegt – hier nicht änderbar.“; Boolean/Dropdown-Merkmale wie bisher. Speichern → `TextValue` unverändert (DB), andere Merkmale gespeichert. |
| 106.4.e | Negativ Konflikt: manuelles Merkmal „Maschine“ (Boolean) **vor** der ersten Sage-Anlage anlegen, dann SData-Lauf | Warnung „Benutzerfeld USER_Maschine: Merkmal „Maschine“ existiert bereits (manuell) – nicht übernommen, Wert unverändert (Konflikt).“ in jedem Lauf; manuelles Merkmal und seine Werte unverändert; die übrigen 14 Felder laufen. |
| 106.4.f | Erweiterung: `Sync:ArtikelBenutzerfelder` um `USER_Modell` ergänzen, SData-Lauf | Merkmal „Modell“ erscheint nach dem Lauf ohne Code-Änderung (AK 12). |
| 106.4.f-neg | `USER_GibtEsNicht` und `Kaputt` ergänzen, SData- bzw. Vergleichs-Lauf | Warnungen „Benutzerfeld USER_GibtEsNicht ist in Sage (Artikel) unbekannt – übersprungen …“ und „Benutzerfeld „Kaputt“ ist kein gültiger Feldname …“; Lauf läuft, übrige Felder werden gelesen. Liste zurücksetzen. |
| 106.4.g | Negativ Länge: in Sage (Testsystem) ein Benutzerfeld eines Artikels auf > 1000 Zeichen setzen (falls Sage so lange Werte zulässt), SData-Lauf | `TextValue` auf 1000 Zeichen gekürzt, Warnung „… n Zeichen, auf 1000 gekürzt.“ |
| 106.4.h | Rückfall: nach 106.4.c je ein Zyklus `View` und `Vergleich`; danach Wert in Sage leeren + SData-Lauf | View/Vergleich: Merkmale und Werte unverändert (DB vor/nach). Geleerter Wert: Wert-Zeile entfernt („1 entfernt“). Testdaten aufräumen: `DELETE FROM ArticleAttributeValues WHERE CreatedBy = 'UAT'`, danach die UAT-Definition. |

### TS-106.5 — E4a: FA-Struktur-Reader (Kanten über SData, Vergleichsmodus)

**Standort:** IDEAL (FA-Struktur-Sync `Sync:HierarchicalFaEnabled = true`); der Vergleich gegen SData ist bis zum Kauf des IDEAL-Endpunkts
nur am AKE-Test möglich (G2) – dort nur, wenn eine FA-Struktur-View (`Sync:FaHierarchyListeViewName`/`…InfosViewName`) existiert, sonst
gilt AK 13 über den automatisierten Test. Keine Migration.
**Vorbedingungen:** SData-Endpunkt, Zugangsdaten, TLS-Schalter und Fehlermail wie TS-106.1; `WorkerSettings:SyncDryRun = false`. Vorher
notieren: `SELECT COUNT(*) FROM FaHierarchyNodes`, `SELECT COUNT(*) FROM FaHierarchyOrderInfos` und den letzten Lauf „FA-Hierarchie-Sync“
im Aktivitäts-Protokoll (Count-Keys `neu`, `geloescht`, `hauptfa_mehrdeutig`).
**Wichtig:** `Sync:Quelle:FaStruktur = SData` ist bis zur Prüfabfrage P-F2 gesperrt (G11) – SData liefert nur Kanten, FAInfos und die
Knotenfelder (Arbeitsschritte, Kommissionieren, Arbeitsbereich …) bleiben SQL. Automatisiert: `FaStrukturSdataTests` (Projektion Struktur 835
mit 17 Positionen: Wurzel/Kanten/Blätter, AG fällt weg; Zwischenebene; Struktur ohne Haupt-FA; Pflichtfelder; Lesefenster G8; Positionen in
`or`-Blöcken; Weiche View/SData-gesperrt/Vergleich inkl. SData-Fehler; Schlüssel; Abgleich über das Interface: Empty-Guard, ungültiger
View-Name, DryRun, Lesefehler; DI), `FaHierarchySyncServiceTests`, `FaHierarchySqlTests` (SQL-Text unverändert).

| Nr | Schritte | Erwartung |
|---|---|---|
| 106.5.a | Default: `Sync:Quelle:FaStruktur` fehlt bzw. `View`, einen Zyklus | Lauf „FA-Hierarchie-Sync“ mit denselben Count-Keys und Zählerständen wie vor dem Deploy (bei unveränderten Sage-Daten), zusätzlich nur die Info-Zeile „Quelle: View“; Tabellen-Zählungen wie vorher; **kein** Lauf „Sage-Quellenvergleich“, keine Mail. FA-Liste, Kommissionierlisten und FA-Struktur-Modal zeigen dieselben Daten. |
| 106.5.a-neg | Wert `Neu` eintragen, einen Zyklus | Wirkt wie `View`; genau eine Warnung „Unbekannter Wert 'Neu' für Sync:Quelle:FaStruktur …“ im Lauf „Sage-Quellenvergleich“. Zurück auf `View`. |
| 106.5.a-view | `Sync:FaHierarchyListeViewName` auf `x;DROP` setzen, einen Zyklus | Wie vor E4a: Lauf „FA-Hierarchie-Sync“ mit Fehler „Ungueltiger View-Name: …“, Mail „FaHierarchy-Sync: ungueltiger View-Name“, Tabellen unverändert. Namen auf einen nicht existierenden, gültigen Namen setzen → Fehler „Sage-Quelle … nicht vorhanden oder keine Leseberechtigung — FA-Struktur-Sync abgebrochen.“ + Fehlermail „FA-Hierarchie-Sync“. Namen zurücksetzen. |
| 106.5.b | `Vergleich`, einen Zyklus | Lauf „FA-Hierarchie-Sync“: Zähler wie bei `View` (geschrieben wird nur aus der View), „Quelle: Vergleich“, Info-Zeilen „Quelle SData: PpsFaBelegeInfo, …“ und „Quelle SData: PpsFaBelegePositionenInfo, n Block/Bloecke, m Zeile(n)“. Lauf „Sage-Quellenvergleich“ (Referenz `FaStruktur`) mit „… abweichende Schlüssel (View n, SData m)“ und ≤ 20 Beispielen der Form „[FA 836] VaterFA: View '…' / SData '…'“ bzw. „[Blatt 836/1] Artnr …“. Am AKE-Test für Struktur 835 (AK 13): **keine** Abweichung bei `FA 835` (Wurzel, `SubFA = 835`), `FA 836`…`FA 841` (`VaterFA = 835`) und den Blättern (`SubFA = 0`). **Befund festhalten** (Anzahl, Beispiele; „fehlt in SData“ = FA außerhalb des Lesefensters bzw. View-Filter P-F1) – er entscheidet zusammen mit P-F2 über die Freigabe. |
| 106.5.b2 | Vergleich: Strukturen ohne FAInfos-Zeile | Erscheinen **nicht** als „fehlt in View“ (der Vergleich nimmt auf der SData-Seite nur Strukturen mit FAInfos, wie der View-Select). |
| 106.5.c | `SData` (gesperrt bis P-F2), einen Zyklus | Wirkt wie `View`: Warnung „Sync:Quelle:FaStruktur = SData ist gesperrt bis P-F2 – Quelle View.“ im Lauf „Sage-Quellenvergleich“; Lauf „FA-Hierarchie-Sync“ mit „Quelle: View“ und Zählern wie bei `View`. |
| 106.5.d | `Vergleich`, `SData:BaseUrl` auf nicht erreichbaren Host (bzw. falsches Passwort), einen Zyklus | Lauf „Sage-Quellenvergleich“ endet mit Fehler „FaStruktur: SData nicht lesbar …“ + Fehlermail „Sage-Quellenvergleich FaStruktur“; Lauf „FA-Hierarchie-Sync“ **erfolgreich** aus der View, Materialisierung läuft danach normal. |
| 106.5.d2 | Negativ: Feldnamen/Filter passen nicht (erste UAT-Prüfung, P-S1) — Lauf mit `Vergleich` | Liefert Sage HTTP 500 (Meldung mit dem erzeugten SQL, z. B. zu `IstBeendet eq 0 or Istendtermin ge @…@` oder `BelIDNachfolger`/`RessourceTyp`), endet der Vergleich mit Fehler; Zeilen ohne BelID/StrukturID → „Feldnamen prüfen (P-S1)“, **nie** „0 Zeilen“. Der FA-Struktur-Sync läuft aus der View. Befund an die Entwicklung. |
| 106.5.e | Negativ Lesefenster (G8): in Sage (Testsystem) einen FA einer Struktur beenden, `Istendtermin` > 30 Tage zurück, Zyklus mit `Vergleich` | Dessen eigene Positionen (Blätter) erscheinen als „fehlt in SData“; die Kante vom Vater-FA bleibt (sie ist eine Position des Vaters). Ist der Haupt-FA selbst außerhalb des Fensters: Warnung „… Struktur(en) ohne Haupt-FA in der SData-Menge (keine Wurzel): …“ im Lauf „FA-Hierarchie-Sync“. (Nicht eingelastete/inaktive FAs: E4b.) |
| 106.5.f | Leerer View-Read unverändert: nur Testsystem, FAInfos-View leer (z. B. `Sync:FaHierarchyInfosViewName` auf eine leere View mit gleichen Spalten), Zyklus mit `View` | Warnung „Leerer View-Read (FAListe=n, FAInfos=0) — kein Replace …“, Zusatz „[uebersprungen: leerer View-Read]“, Mail „FaHierarchy-Sync: leerer View-Read“; Tabellen unverändert. Namen zurücksetzen. |

### TS-106.6 — E4b: FA-Positionen (`ProductionOrderPositions`) + Lesefilter eingelastet/aktiv

**Standort:** IDEAL (FA-Struktur-Sync `Sync:HierarchicalFaEnabled = true`); der SData-Teil ist bis zum IDEAL-Endpunkt nur am AKE-Test prüfbar
(Struktur 835, G2). **Migration:** `SQL/101_AddProductionOrderPositions.sql` (Tabellen `ProductionOrderPositions` + `SageFaStates`).
**Vorbedingungen:** wie TS-106.5; `SQL/101` eingespielt. Vorher notieren: `SELECT COUNT(*) FROM ProductionOrderPositions` (= 0 nach Deploy),
`SELECT COUNT(*) FROM SageFaStates` (= 0) und den letzten Lauf „FA-Hierarchie-Sync“ (Count-Keys).
**Regeln:** Positionen gibt es nur aus SData. Sie werden geschrieben, sobald der Lauf SData gelesen hat – heute also nur im Modus `Vergleich`
(`SData` ist bis P-F2 gesperrt und wirkt wie `View`). Full-Refresh in eigener Transaktion **nach** dem Struktur-Commit; Count-Key `positionen`
erscheint nur in solchen Läufen. `SageFaStates` wird in dieser Etappe nur angelegt und bleibt leer (befüllt das Rückmelde-Epic).
Lesefilter: nur FAs mit `IstEingelastet = -1` und `Aktiv = -1` (im Code gefiltert, Lesefenster G8 wie E4a).
Automatisiert: `FaStrukturSdataTests` (Filter inkl. fehlendem Feld, inaktiver Unter-FA, Positionen-Projektion Struktur 835 mit BelPosID 10641 /
Platz 11 „HR 0 0 0“, doppelte BelPosID, unbekannter Platz, unlesbarer Bedarfstermin, Lesen mit Lagerplätzen, Weiche: Positionen nur bei
Vergleich, nicht bei SData-Fehler; DryRun; Leer-Guard; Schreibfehler → Protokoll + Mail).

| Nr | Schritte | Erwartung |
|---|---|---|
| 106.6.a | Migration: `SQL/101` zweimal ausführen | 1. Lauf: „Tabelle ProductionOrderPositions erstellt.“, drei Indexe (`UQ_ProductionOrderPositions_BelPosID`, `IX_…_BelID`, `IX_…_StrukturID`), „Tabelle SageFaStates erstellt.“, `UQ_SageFaStates_Mandant_BelID`, History `20261106191438_AddProductionOrderPositions`. 2. Lauf: keine Meldung außer `SET`-Ausgaben, keine zweite History-Zeile. Frische Installation: `SQL/00_FreshInstall.sql` legt beide Tabellen samt Indexen an und trägt die MigrationId ein. |
| 106.6.b | Default `View`, einen Zyklus | Lauf „FA-Hierarchie-Sync“ mit genau den Count-Keys `neu`, `geloescht`, `hauptfa_mehrdeutig` (kein `positionen`), Zählerstände wie vor dem Deploy; `ProductionOrderPositions` und `SageFaStates` bleiben leer. |
| 106.6.c | `SData` (gesperrt), einen Zyklus | Wie 106.6.b (Warnung „gesperrt bis P-F2“ wie TS-106.5.c); `ProductionOrderPositions` unverändert. |
| 106.6.d | `Vergleich`, einen Zyklus (AKE-Test) | Lauf „FA-Hierarchie-Sync“: Info „ProductionOrderPositions: n Positionen aus SData ersetzt (m gelöscht).“, Count-Key `positionen = n`; Struktur-Zähler wie bei `View`. DB: `SELECT * FROM ProductionOrderPositions WHERE BelID = 836` → Zeile `BelPosID = 10641`, `StrukturID = 835`, `RessourceTyp = 'MA'`, `RessourceNummer = '00722001'`, `Menge = 0.6`, `PlatzID = 11`, `LagerplatzKurzbezeichnung = 'HR 0 0 0'` (AK 15); AG-Position `BelPosID = 10656` an 835 vorhanden. `Sollmenge`/`Istmenge`/Rüst-/Stückzeiten/`ArbeitsgangNummer` sind NULL (Namen bis P-S1 nicht abgefragt). `Status`, `Schwund`, `Bedarfstermin` Stichprobe gegen Sage notieren (P-S1-Befund). |
| 106.6.e | Zweiter Zyklus mit `Vergleich` | Positionsanzahl gleich, `SELECT BelPosID, COUNT(*) FROM ProductionOrderPositions GROUP BY BelPosID HAVING COUNT(*) > 1` leer, `SyncedAt` aller Zeilen = Zeit des zweiten Laufs (vollständig ersetzt, AK 15). |
| 106.6.f | Zurück auf `View`, ein Zyklus | `ProductionOrderPositions` unverändert (gleiche Anzahl, gleiches `SyncedAt` wie in 106.6.e) – der View-Lauf leert nichts. |
| 106.6.g | Negativ Lesefilter (Testsystem): in Sage einen Unter-FA der Struktur auf inaktiv setzen bzw. auslasten, Zyklus mit `Vergleich` | Info „1 FA(s) nicht eingelastet oder inaktiv – nicht in der SData-Menge: <BelID>“; seine Positionen fehlen in `ProductionOrderPositions`; im Lauf „Sage-Quellenvergleich“ erscheinen seine Blätter als „fehlt in SData“ (die View filtert anders – Befund für P-F1 festhalten). Erst nach P-F2 (`SData`): der materialisierte Unter-FA bekommt `SageMissingSince` und erscheint nicht als offen (AK 14). Sage zurücksetzen. |
| 106.6.h | Negativ Feldnamen (P-S1): Liefert Sage HTTP 500 für `IstEingelastet`/`Aktiv`/`PlatzID`/`Status`/`Schwund`/`Bedarfstermin` bzw. fehlt `IstEingelastet` in einer Zeile | Lauf „Sage-Quellenvergleich“ endet mit Fehler (Meldung mit erzeugtem SQL bzw. „… Zeilen ohne IstEingelastet/Aktiv – Feldnamen prüfen (P-S1)“) + Fehlermail; `ProductionOrderPositions` **unverändert** (nie geleert); FA-Struktur-Sync aus der View erfolgreich. Befund an die Entwicklung. |
| 106.6.i | Negativ Schreibfehler (nur Testsystem): vor einem `Vergleich`-Zyklus `ALTER TABLE ProductionOrderPositions ALTER COLUMN LagerplatzKurzbezeichnung NVARCHAR(2)` | Lauf „FA-Hierarchie-Sync“ erfolgreich (Struktur geschrieben), dazu Fehlerzeile „ProductionOrderPositions nicht ersetzt (alter Stand bleibt): …“, Count `positionen = 0`, Mail „FaHierarchy-Sync: FA-Positionen nicht ersetzt“; alte Positionen unverändert. Spalte zurück auf `NVARCHAR(50)`. |
| 106.6.j | Hinweise: Platz einer Position fehlt in `LagerplaetzeInfo` bzw. Bedarfstermin unlesbar | Warnung „n PlatzID(s) der Positionen nicht in LagerplaetzeInfo …“ bzw. „n Bedarfstermin(e) nicht lesbar …“ im Lauf „FA-Hierarchie-Sync“; Positionen trotzdem geschrieben (Kurzbezeichnung bzw. Termin leer). |

### TS-106.7 — E6a: Arbeitsplätze und Arbeitsgang-Kürzel über SData

**Standort:** IDEAL (Werkbank-Anlage `Sync:ProductionWorkplaceSyncEnabled = true` + Master `ProduktionsauftragHierarchisch`; Arbeitsgang-Abgleich
`Sync:OperationConfigSyncEnabled = true`, beide Standorte); der SData-Teil ist bis zum IDEAL-Endpunkt nur am AKE-Test prüfbar (Arbeitsplatz 9999, G2).
**Migration:** `SQL/102_AddProductionWorkplaceSdataFields.sql` (Spalten `SageBdeTerminalId` nvarchar(20), `SageKostenstelle` nvarchar(20),
`SageSchichtplannummer` nvarchar(31) an `ProductionWorkplaces`).
**Vorbedingungen:** SData-Endpunkt, Zugangsdaten, TLS-Schalter, Fehlermail wie TS-106.1; `WorkerSettings:SyncDryRun = false`; `SQL/102` eingespielt.
Vorher notieren: letzter Lauf „Werkbank-Anlage (Sage-Arbeitsplätze)“ und „Arbeitsgang-Abgleich (Sage-View)“ (Count-Keys + Zähler),
`SELECT Id, SageArbeitsplatznummer, Name, ArbeitsschrittCode, SageMissingSince, SageBdeTerminalId, SageKostenstelle, SageSchichtplannummer FROM ProductionWorkplaces`,
`SELECT OperationName, Source, SageMissingSince FROM OperationConfigs`.
**Regeln:** Ein Schalter `Sync:Quelle:Arbeitsplaetze` für beide Läufe (G12). SData liefert am AKE-Test **kein** Arbeitsschritt-Kürzel
(`USER_ArbeitsSchritt`, P-A1): im Modus `SData` keine Werkbank-Neuanlage, kein Name-/Kürzel-Abgleich, Arbeitsgang-Abgleich setzt aus.
Die drei neuen Felder schreibt nur der SData-Weg, nur nicht-leere Werte. Automatisiert: `ArbeitsplatzSdataTests` (Mapping, Aktiv-Filter, kein
USER-Feld im select, Fehler wirft, Weiche View/SData/Vergleich, Werkbank: Zusatzfelder/keine Anlage/Rückfall/Nicht-null/DryRun/Neuanlage/
Kennzeichnung, Kürzel: Aussetzen bei SData, View über die Weiche, DI), `ProductionWorkplaceSyncServiceTests`, `OperationConfigSyncServiceTests` unverändert grün.

| Nr | Schritte | Erwartung |
|---|---|---|
| 106.7.a | Migration: `SQL/102` zweimal ausführen | 1. Lauf: drei „Spalte ProductionWorkplaces.… hinzugefuegt.“, History `20261106192342_AddProductionWorkplaceSdataFields`. 2. Lauf: keine Meldung außer `SET`-Ausgaben, keine zweite History-Zeile. Frische Installation: `SQL/00_FreshInstall.sql` legt die Spalten an und trägt die MigrationId ein. |
| 106.7.b | Default: `Sync:Quelle:Arbeitsplaetze` fehlt bzw. `View`, einen Zyklus | Beide Läufe mit denselben Count-Keys und Zählerständen wie vor dem Deploy, zusätzlich nur die Info-Zeile „Quelle: View“; neue Spalten bleiben NULL; **kein** Lauf „Sage-Quellenvergleich“, keine Mail. |
| 106.7.b-neg | Wert `Neu` eintragen, einen Zyklus | Wirkt wie `View`; je Lauf (Werkbank-Anlage und Arbeitsgang-Abgleich lesen getrennt) genau eine Warnung „Unbekannter Wert 'Neu' für Sync:Quelle:Arbeitsplaetze …“ im Lauf „Sage-Quellenvergleich“. Zurück auf `View`. |
| 106.7.c | `Vergleich`, einen Zyklus | Beide Läufe: Zähler wie bei `View` (geschrieben wird nur aus der View), „Quelle: Vergleich“. Zwei Läufe „Sage-Quellenvergleich“ (Referenz `Arbeitsplaetze`, je Leser einer) mit „… abweichende Schlüssel (View n, SData m)“ und ≤ 20 Beispielen „[<Nummer>] Name: View '…' / SData '…'“. Am AKE-Test für Arbeitsplatz 9999 **keine** Abweichung (AK 17); das Kürzel wird bis P-A1 nicht verglichen. Befund festhalten (Anzahl, Beispiele; an IDEAL: View `vw_IDEAL_AKE_WMS_Arbeitsgaenge` filtert ggf. anders). |
| 106.7.d | `SData`, einen Zyklus | Werkbank-Anlage: „Quelle: SData“, Warnung „Quelle liefert kein Arbeitsschritt-Kuerzel (USER_ArbeitsSchritt, Pruefabfrage P-A1) …“, `neu = 0`, `aktualisiert = 0`, alle Zeilen `uebersprungen`; Info „Sage-Zusatzfelder (BDE-Terminal, Kostenstelle, Schichtplan) aktualisiert: n“. DB: vorhandene Werkbank 9999 hat `SageBdeTerminalId` (AK 17), `SageKostenstelle`, `SageSchichtplannummer` gesetzt, `Name`/`ArbeitsschrittCode` unverändert. Arbeitsgang-Abgleich: „Quelle: SData“, gleiche Warnung, Lauf erfolgreich mit Zusatz „Kuerzel nicht geliefert“, alle Zähler 0; `OperationConfigs` unverändert (insbesondere **kein** neues `SageMissingSince`). Keine Spalte an `OperationConfigs` hinzugekommen. |
| 106.7.e | Zurück auf `View`, einen Zyklus | Die drei Zusatzfelder unverändert (nicht genullt), Werkbank-Anlage und Arbeitsgang-Abgleich wie in 106.7.b. |
| 106.7.f | Negativ Kennzeichnung: in Sage (Testsystem) einen Arbeitsplatz inaktiv setzen, Zyklus mit `SData` | Seine Werkbank bekommt `SageMissingSince` wie heute (Warnung „Nicht mehr in Sage …“, Count `nicht_in_sage_neu = 1`); bei BDE-aktiver Werkbank zusätzlich die BDE-Warnung. Sage zurücksetzen, nächster Zyklus: „Wieder in Sage“, `SageMissingSince` NULL. |
| 106.7.g | Negativ Ausfall: `SData`, `SData:BaseUrl` auf nicht erreichbaren Host bzw. falsches Passwort, einen Zyklus | Beide Läufe enden mit Fehler „Sage-Connection fehlgeschlagen: …“ + Fehlermail (wie bisher bei Lesefehlern); keine Werkbank und kein Kürzel wird gekennzeichnet oder geändert; andere Bereiche laufen weiter. Mit `Vergleich` statt `SData`: Lauf „Sage-Quellenvergleich“ mit Fehler + Mail „Sage-Quellenvergleich Arbeitsplaetze“, beide Läufe erfolgreich aus der View. |
| 106.7.h | Negativ Feldnamen (P-S1): Liefert Sage HTTP 500 für `Schichtplannummer`/`Kostenstelle`/`BdeTerminalId`/`Aktiv` | Wie 106.7.g (Fehler mit der Sage-Meldung, nie „0 Zeilen“); Befund an die Entwicklung. |
| 106.7.i | Nur bei IDEAL-Endpunkt: P-A1 dort prüfen (`…/PpsArbeitsplaetzeInfo?select=Arbeitsplatznummer,USER_ArbeitsSchritt&orderBy=Arbeitsplatznummer&count=5&format=json`) | Ergebnis an die Entwicklung (HTTP 200 mit Wert → Kürzel kann in den SData-Reader aufgenommen werden; 500 → bleibt ohne Kürzel). Bis dahin `Sync:Quelle:Arbeitsplaetze = SData` an IDEAL **nicht** produktiv setzen (keine Werkbank-Neuanlage über SData). |

### TS-106.8 — E6b: PPS-Mitarbeiter (Zuordnung in der BDE-Personalpflege) und Stillstandsgründe

**Standort:** beide (am AKE-Test prüfbar, G2). **Migration:** `SQL/103_AddSagePpsMitarbeiterAndStillstandsgruende.sql` (Tabellen
`SagePpsMitarbeiter`, `Stillstandsgruende`; Spalte `BdeOperators.SageMitarbeiternummer` nvarchar(20) mit gefiltertem UNIQUE-Index).
**Vorbedingungen:** SData-Endpunkt, Zugangsdaten, TLS-Schalter, Fehlermail wie TS-106.1; `WorkerSettings:SyncDryRun = false`; `SQL/103` eingespielt;
`BdeAktiv = true`; Benutzer A mit BDE-Schichtleitung (`bde_shiftlead`), Benutzer B ohne BDE-Rolle. **In Sage (AKE-Test) mindestens drei
PPS-Mitarbeiter angelegt** (Spec-Voraussetzung 3, am AKE-Test waren es 0): M1 mit `Nummer` = Personalnummer eines vorhandenen BDE-Mitarbeiters,
M2 mit gleichem Vor-/Nachnamen wie ein anderer BDE-Mitarbeiter, aber anderer Nummer, M3 ohne Gegenstück. Mindestens zwei Stillstandsbegründungen.
**Regeln:** Neue Lese-Abgleiche ohne View-Weg (keine Weiche), je eigener Schalter, Default aus. Der PPS-Mitarbeiter-Abgleich legt **nie** einen
BDE-Mitarbeiter an und verknüpft **nie** — Zuordnung nur per Knopf; Vorschlag nur bei exakt gleicher Personalnummer. Kartennummer/externe Nummer
nur Anzeige. Automatisiert: `PpsMitarbeiterStillstandTests` (Mapping/Abfrage, Vollersatz, keine Anlage, keine Namens-Verknüpfung, Warnungen,
Empty-Guard, Dubletten, DryRun, SData-Fehler, Stillstand anlegen/aktualisieren/kennzeichnen/wieder da/Audit, Gates aus = kein Lauf, DI),
`BdeMasterDataSageZuordnungTests` (Vorschlag exakt, Liste/Filter/Paging, Zuordnen mit Audit, Duplikat-Guard, Lösen).

| Nr | Schritte | Erwartung |
|---|---|---|
| 106.8.a | Migration: `SQL/103` zweimal ausführen | 1. Lauf: Spalte, Index `IX_BdeOperators_SageMitarbeiternummer`, zwei Tabellen mit Index, History `20261106193321_AddSagePpsMitarbeiterAndStillstandsgruende`. 2. Lauf: keine Meldung, keine zweite History-Zeile. Frische Installation: `SQL/00_FreshInstall.sql` legt alles an und trägt die MigrationId ein. |
| 106.8.b | Default: beide Schalter `false`, einen Zyklus | Keine Läufe „PPS-Mitarbeiter-Abgleich (Sage)“ / „Stillstandsgründe-Abgleich (Sage)“ im Aktivitäts-Protokoll; Tabellen leer; alle anderen Läufe unverändert. BDE-Stammdaten › Sage-Zuordnung zeigt den Hinweis „Es wurden noch keine Sage-Mitarbeiter gelesen …“. |
| 106.8.c | `Sync:PpsMitarbeiterEnabled = true`, einen Zyklus | Lauf „PPS-Mitarbeiter-Abgleich (Sage)“ erfolgreich: Info „Quelle SData: PpsMitarbeiterInfo, … Zeile(n)“, Counts `gelesen`, `uebersprungen`, `nicht_zugeordnet`, `bde_ohne_sage_nummer`, `sage_nummer_unbekannt`; Warnung „n aktive BDE-Mitarbeiter ohne Sage-Mitarbeiternummer (erste 20): …“. `SELECT * FROM SagePpsMitarbeiter` = Sage-Bestand; `BdeOperators` unverändert (Anzahl gleich, alle `SageMitarbeiternummer` NULL). |
| 106.8.d | Benutzer A: BDE-Stammdaten › Reiter „Sage-Zuordnung“ | Liste aller Sage-Mitarbeiter, nicht zugeordnete zuerst; Spalten Sage-Nr., Name, Matchcode, Kartennr., Externe Nr., Aktiv, Status (Text „Nicht zugeordnet“/„Zugeordnet“), BDE-Mitarbeiter. Gelbe Warnung mit der Anzahl aktiver BDE-Mitarbeiter ohne Sage-Nummer. M1: Auswahl ist mit dem BDE-Mitarbeiter gleicher Personalnummer vorbelegt, darunter „Vorschlag: gleiche Personalnummer“. M2 und M3: Auswahl „– auswählen –“ (kein Vorschlag, auch nicht bei gleichem Namen). Spaltenfilter (z. B. Status „Nicht zugeordnet“) wirkt über alle Seiten; Seitenwechsel funktioniert. |
| 106.8.e | M1 „Zuordnen“ | Grüne Meldung „Sage-Mitarbeiter … ist jetzt '…' zugeordnet.“; Zeile zeigt „Zugeordnet“ + BDE-Mitarbeiter + Knopf „Lösen“; im Reiter Operatoren Spalte „Sage-Nr.“ gefüllt. DB: `SageMitarbeiternummer`, `ModifiedAt/By/ByWindows` des angemeldeten Benutzers. Warnzahl um 1 kleiner. |
| 106.8.f | M3 einem freien BDE-Mitarbeiter manuell zuordnen (Auswahl, „Zuordnen“), dann „Lösen“ | Zuordnung gesetzt; nach „Lösen“ grüne Meldung „Zuordnung … gelöst.“, Nummer NULL, Audit-Felder aktualisiert, Zeile wieder „Nicht zugeordnet“. |
| 106.8.g | Negativ: „Zuordnen“ ohne Auswahl | Browser verlangt eine Auswahl (Pflichtfeld); per Direkt-POST ohne `operatorId`: gelbe Meldung „Bitte einen BDE-Mitarbeiter auswählen.“, nichts geändert. |
| 106.8.h | Negativ doppelte Nummer: zwei Browser-Tabs auf der Sage-Zuordnung; in Tab 1 M1 → Mitarbeiter X zuordnen; in Tab 2 (alte Seite) M1 → Mitarbeiter Y zuordnen | Gelbe Meldung „Die Sage-Mitarbeiternummer … ist bereits '…X…' zugeordnet.“; Y bleibt ohne Nummer. Gleiches Spiel mit X für eine zweite Nummer: „… ist bereits der Sage-Nummer … zugeordnet – zuerst die Zuordnung lösen.“ |
| 106.8.i | Negativ Rolle: Benutzer B ruft `/BdeMasterData?tab=sage` auf bzw. sendet einen Direkt-POST an `/BdeMasterData/SageZuordnen` | AccessDenied (Filter `RequireBdeShiftleadAccess`); bei `BdeAktiv = false` ist der ganze Bereich gesperrt (`RequireBdeActive`). |
| 106.8.j | Zugeordnete Nummer in Sage löschen bzw. umnummern, einen Zyklus | Warnung „1 zugeordnete Sage-Mitarbeiternummern stehen nicht (mehr) in Sage (erste 20): <Nr> → <Personalnr> …“, Count `sage_nummer_unbekannt = 1`; die Zuordnung bleibt (nur melden). |
| 106.8.k | `Sync:StillstandsgruendeEnabled = true`, einen Zyklus; dann in Sage eine Begründung umbenennen und eine löschen, Zyklus; dann wieder anlegen, Zyklus | 1. Lauf „Stillstandsgründe-Abgleich (Sage)“: `neu` = Sage-Anzahl, `Stillstandsgruende` gefüllt (`CreatedBy = StillstandsgruendeSync`). 2. Lauf: `aktualisiert = 1` (neue Bezeichnung, `ModifiedBy = StillstandsgruendeSync`), `nicht_in_sage_neu = 1` + Warnung „Nicht mehr in Sage …“, Zeile **nicht** gelöscht, `SageMissingSince` gesetzt. 3. Lauf: `wieder_in_sage = 1`, `SageMissingSince` NULL. Keine Oberfläche für Stillstandsgründe (erst Rückmelde-Epic). |
| 106.8.l | Negativ Empty-Guard: Sage liefert 0 Zeilen (z. B. Testsystem ohne Daten bzw. alle gelöscht) | PPS-Mitarbeiter: Warnung „Sage liefert keine PPS-Mitarbeiter — Cache unveraendert (Empty-Guard).“, `SagePpsMitarbeiter` unverändert. Stillstand: Warnung „… keine Kennzeichnung "nicht in Sage" (Empty-Guard).“, kein `SageMissingSince` gesetzt. |
| 106.8.m | Negativ Ausfall: `SData:BaseUrl` auf nicht erreichbaren Host bzw. falsches Passwort, einen Zyklus mit beiden Schaltern an | Beide Läufe enden mit Fehler „Sage (SData) nicht lesbar: …“ + je eine Fehlermail („PPS-Mitarbeiter-Abgleich (Sage)“, „Stillstandsgruende-Abgleich (Sage)“); Tabellen unverändert; andere Bereiche laufen weiter. |
| 106.8.n | Negativ Feldnamen (P-S1): Liefert Sage HTTP 500 für ein Feld des select (`Nummer,Matchcode,Vorname,Nachname,Gruppe,Kartennummer,ExterneNummer,Aktiv,Schichtplannummer,Kostenstelle,UserName` bzw. `Stillstandnummer,Bezeichnung`) | Wie 106.8.m mit der Sage-Meldung (nie „0 Zeilen“); Befund an die Entwicklung. |

### TS-106.9 — E7: Lagerbuchungs-Lookup über SData und Umbuchung in der Buchungs-Queue

**Standort:** beide (am AKE-Test prüfbar: `sagetest01`, Dataset `ake_TEST2026;1`). **Migration:** keine.
**Vorbedingungen:** SData-Endpunkt, Zugangsdaten, TLS-Schalter, Fehlermail wie TS-106.1; Sage-Lagerbuchung eingerichtet wie TS-56
(`SageLagerbuchungAktiv = true`, `SageBookingWorker` läuft); Lagerplätze: **S1** und **S2** mit „Sage-Buchung erlaubt“ und Sage-Referenz
(`SageLagerkennung`/`SageLagerplatzId`, z. B. LL/2864 und HR/11), **W1** und **W2** WMS-only (ohne „Sage-Buchung erlaubt“); Artikel 00722001 mit
Bestand auf S1 und W1. Vorher notieren: `SELECT MAX(Id) FROM SageBookingQueueItems`, Bestand 00722001 je Platz in Sage.
**Regeln:** Neue Schalter `SageLagerbuchungUmbuchungAktiv` (Bool, Default `false`, Kategorie „Sage-Lagerbuchung“) und – schon seit E0 im Katalog,
jetzt wirksam – `Sync:Quelle:LagerbuchungLookup` (`View` Default | `SData` | `Vergleich`). Umbuchung (Spec F9): S → S = **eine** Sage-Umbuchung
(Herkunft + Ziel, in Sage Bewegungsart IU), S → W = Entnahme an S, W → S = Zugang an S, W → W = nichts. Sage-Korrekturbuchungen werden nie
eingereiht. Ein Fehler des Lookups wirft immer (nie „nicht gebucht“); der Worker sendet dann **nicht** neu. Automatisiert:
`BuchungLookupSdataTests` (Abfrage/Mapping, Fehler wirft, ohne Konfiguration wirft, URL-Kodierung `#`/`%`, Weiche View/SData/Vergleich/
unbekannter Wert, DI), `SageLagerbuchungPayloadBuilderTests` (drei Umbuchungs-Varianten, W → W wirft, fehlende Referenz/Herkunft wirft),
`SageBookingEnqueueDecisionTests` (Typliste, Matrix, Schalter aus, Sage-Korrektur nie), `SageBookingEnqueueingStockMovementRepositoryTests`
(Decorator-Matrix, Schalter fehlt/aus, Nacherfassungs-Lauf mit/ohne Schalter). **Achtung:** Schritte c–f buchen echt in Sage – nur am Testsystem.

| Nr | Schritte | Erwartung |
|---|---|---|
| 106.9.a | Nach Deploy `/ServiceSettings` öffnen | Kategorie „Sage-Lagerbuchung“ enthält `SageLagerbuchungUmbuchungAktiv = false`; `Sync:Quelle:LagerbuchungLookup = View`. |
| 106.9.b | Default (Schalter aus): Lager › Umbuchung 1 Stück S1 → S2; Lagerplatz umbuchen W1 → S1; Kommissionierung „Umbuchen“ eines FA (TransferPicked) | WMS-Buchungen gespeichert wie bisher; **kein** neuer Eintrag in `/SageBookingQueue` (auch nicht nach 1–2 Worker-Takten über den Nacherfassungs-Lauf); Lauf „Sage-Lagerbuchung“ mit denselben Count-Keys wie vorher. Eine Einbuchung auf S1 wird weiter eingereiht und bestätigt. |
| 106.9.c | `SageLagerbuchungUmbuchungAktiv = true`; Umbuchung 1 Stück S1 → S2 | Genau **ein** Queue-Eintrag (Bewegung = die Umbuchung), nach einem Takt „Bestätigt“. In Sage: `LagerplatzbuchungenInfo` mit Bewegungsart IU, Herkunft S1 → Ziel S2, Memo `IdealAkeWms Lagerbuchung SM#<id>#`; Bestand S1 −1, S2 +1. |
| 106.9.d | Umbuchung 1 Stück S1 → W1 | Ein Queue-Eintrag; Sage-Buchung als **Entnahme** an S1 (Bestand S1 −1), Memo mit `SM#<id>#`. |
| 106.9.e | Umbuchung 1 Stück W1 → S1 | Ein Queue-Eintrag; Sage-Buchung als **Zugang** an S1 (Bestand S1 +1). |
| 106.9.f | Umbuchung W1 → W2 | Kein Queue-Eintrag, auch nicht nach dem Nacherfassungs-Lauf. |
| 106.9.g | Nacherfassung: Kommissionierung „Umbuchen“ mit Herkunft S1 (schreibt am Decorator vorbei) | Innerhalb eines Worker-Takts Info „n Buchung(en) ohne Queue-Eintrag nachtraeglich eingereiht“, Count `nacherfasst`; Art nach derselben Regel wie c–f. |
| 106.9.h | Lookup View (Default): einen bestätigten Eintrag aus c per SQL auf `Status = Gesendet` (1) und `SentAt` älter als `Sync:SageLagerbuchungStuckMinutes` setzen | Nächster Takt: Eintrag „Bestätigt“ mit „Recovery: Buchung in Sage bereits vorhanden (Memo-Lookup).“; im Lauf Info „Quelle Lookup: View“; **keine** zweite Sage-Buchung. |
| 106.9.i | Wie h, aber `Sync:Quelle:LagerbuchungLookup = SData` | Wie h, Info „Quelle Lookup: SData“. **Wichtig (UAT, ungeprüft):** Die Memo-Suche läuft über `where=Memo like '%SM#<id>#%'` (in der URL `%25SM%23<id>%23%25`); am AKE-Test belegt war nur `Memo like 'IdealAkeWms%'`. Liefert der Lookup hier „nicht vorhanden“ (Eintrag geht auf „Offen“ und würde neu gesendet), sofort `View` zurückstellen und den Befund melden. Vor dem Schritt im Browser prüfen: `…/CommonWawiServicesResources/ake_TEST2026;1/LagerplatzbuchungenInfo?format=json&orderBy=BuchungsID&count=1&select=BuchungsID&where=Memo%20like%20'%25SM%23<id>%23%25'` liefert eine Zeile. |
| 106.9.j | `Vergleich`, Vorgehen wie h | Ergebnis aus der View (bestätigt); Lauf „Sage-Quellenvergleich“ (Referenz `LagerbuchungLookup`) mit „0 abweichende Schlüssel“ (bei Abweichung: Beispiel `[SM#<id>#] (fehlt in SData)`). |
| 106.9.k | Negativ Ausfall: `SData`, `SData:BaseUrl` auf nicht erreichbaren Host, Vorgehen wie h | Eintrag bleibt „Gesendet“, Warnung „Sage-Lookup fuer haengenden Eintrag #… fehlgeschlagen: …“, Count `uebersprungen = 1`; **nicht** neu gesendet, keine Sage-Buchung. Ein „Offen“-Eintrag mit `SentAt` (Requeue) geht stattdessen auf „Fehler“ („Sage-Lookup vor Requeue fehlgeschlagen, kein erneutes Senden“). Mit `Vergleich` statt `SData`: Recovery aus der View wie h, zusätzlich Lauf „Sage-Quellenvergleich“ mit Fehler + Fehlermail „Sage-Quellenvergleich LagerbuchungLookup“. |
| 106.9.l | Negativ Wert: `Sync:Quelle:LagerbuchungLookup = sdata` | Wirkt wie `View`; je Lookup eine Warnung „Unbekannter Wert 'sdata' für Sync:Quelle:LagerbuchungLookup …“ im Lauf „Sage-Quellenvergleich“. |
| 106.9.m | Negativ Referenz: S2 „Sage-Buchung erlaubt“ an, aber Sage-Referenz leer; Umbuchung S1 → S2 | Queue-Eintrag geht auf „Fehler“ „Lagerplatz 'S2' hat keine Sage-Referenz …“; nichts an Sage gesendet. |
| 106.9.n | Rückfall: `SageLagerbuchungUmbuchungAktiv = false` | Neue Umbuchungen werden nicht mehr eingereiht; bereits eingereihte offene Umbuchungen werden noch gesendet (Schalter wirkt beim Einreihen). |
| 106.9.o | Negativ FA-Bezug: Kommissionierung „Umbuchen“ mit FA (Herkunft S1, Ziel S2) bzw. Umbuchung im Bewegungsformular mit FA-Feld | **Kein** Queue-Eintrag, auch nicht nach dem Nacherfassungs-Lauf (Spec F9: Entnahme mit FA-Bezug → Rückmelde-Epic). Ein-/Ausbuchungen mit FA unverändert wie bisher. |

### TS-106.10 — E8a: Kreditoren-Stamm, Pflege-Oberfläche, Dienstleister-Meldung

**Standort:** beide (Meldekarte nur IDEAL, sonst identisch). **Migration:** `SQL/104_AddCreditors.sql` (neue Tabelle `Creditors`, rein additiv).
**Vorbedingungen:** `SQL/104` eingespielt; Benutzer **P** mit `masterdata`, **L** mit nur `masterdata_read`, **O** ohne Stammdaten-Rolle, **A** `admin`.
Für 106.10.a: ≥ 30 Kreditoren (z. B. per wiederholtem Anlegen oder SQL-Insert mit `Source = 'Manual'`), davon einige inaktiv und einige Beschichter.
IDEAL: FA-Hierarchie-Sync gelaufen; Prüfabfrage A der Kreditoren-Spec liefert die Dienstleister-Texte:
`SELECT LTRIM(RTRIM([Dienstleister])) AS DienstleisterText, COUNT(*) AS Zeilen FROM dbo.FaHierarchyOrderInfos GROUP BY LTRIM(RTRIM([Dienstleister])) ORDER BY 1;`
**Regeln:** Pflicht Name + Kurzname (Kurzname nur bei Herkunft WMS); Kurzname eindeutig (getrimmt, Groß/Klein egal), Sage-Konto eindeutig, wenn gesetzt
(Text, nie Zahl). Herkunft `WMS` (hier angelegt) oder `Sage` (kommt erst mit dem Lese-Abgleich E8c; zum Test per SQL setzen). Bei `Sage` sind Name,
Adresse, Ansprechpartner, Telefon, E-Mail und Sage-Konto nur lesbar; Kurzname, Matchcode, Bemerkung, Beschichter, Aktiv pflegbar. Kein Löschen.
Automatisiert: `CreditorsControllerTests` (Read/Edit-Split, Audit, Schreibschutz, Duplikat-Guards, Filter, Spaltenfilter vor Pagination, Meldekarte),
`CreditorAssignmentTests`, `CreditorRepositoryTests`, `CreditorsViewTests` (Spaltenschlüssel, viewKey, Menüpunkt). Index-DDL ist Manual-UAT.

| Nr | Schritte | Erwartung |
|---|---|---|
| 106.10.0 | **Nachtrag G6 (Gerald 2026-11-07, gilt vor .c–.g, .i, .l):** Sage ist führend. „Kreditor anlegen (Notfall)“ und „Als Kreditor anlegen (Notfall)“ sieht nur **A** (`admin`); P sieht keinen Anlegen-Knopf, `/Creditors/Create` als P → AccessDenied. Die Schritte .c–.e und .l laufen als **A**. Bei Herkunft Sage: Name, **Matchcode**, Adresse, Kontakt, Sage-Konto readonly; **kein** „Deaktivieren“/„Reaktivieren“ (aktiv/inaktiv kommt aus Sage); `POST SetActive` für einen Sage-Kreditor → Warnung „… aktiv/inaktiv wird in Sage gepflegt“, nichts geändert. Herkunft WMS: unverändert alles pflegbar inkl. Deaktivieren. Liste: Hinweis „Kreditoren kommen aus Sage …“; gibt es Kreditoren mit Herkunft WMS, gelbe Meldung „n Kreditoren sind nur im WMS angelegt …“. Meldekarte: statt „Als Kreditor anlegen“ für P der Text „Kurznamen <Text> am passenden Sage-Kreditor eintragen (Bearbeiten)“; Kurzname am Sage-Kreditor setzen → Text verschwindet aus der Karte. |
| 106.10.a | P: Stammdaten → Kreditoren | Liste mit Filterkarte (Status **Aktiv** vorgewählt, Schalter „Nur Beschichter“), Pagination 25/50/100/Alle (Benutzer-Default greift), Spaltenfilter auf jeder Spalte, Zahnrad. Status „Inaktiv“/„Alle“ und „Nur Beschichter“ filtern richtig; Spaltenfilter (auch `a,b` ODER und `!x` NICHT) wirkt über alle Seiten, „x Einträge“ stimmt. Spalte ausblenden, Reload → bleibt ausgeblendet (kein 400 von `UserViewPreferencesApi` in der Konsole). |
| 106.10.b | Negativ Filter: Spaltenfilter Name = `zzzz` | Leere Tabelle „Keine Kreditoren für diese Filterung.“, Gesamtzahl 0. |
| 106.10.c | P: „Kreditor anlegen“ ohne Name und ohne Kurzname speichern; dann E-Mail `abc` | Feldfehler „Name ist ein Pflichtfeld.“ und „Kurzname ist ein Pflichtfeld.“, bzw. „Bitte eine gültige E-Mail-Adresse eingeben.“; nichts gespeichert, keine Fehlerseite. |
| 106.10.d | Anlegen Kurzname ` Pulver `, Name `Müller & Söhne 'Pulver' <GmbH>`, Sage-Konto ` K31013 `, Beschichter an | Erfolgsmeldung; in der Liste Kurzname `Pulver`, Sage-Konto `K31013` (getrimmt), Herkunft „WMS“, Beschichter „Ja“, Name mit allen Sonderzeichen korrekt angezeigt; Spaltenfilter `söhne` findet ihn; keine Konsolenfehler. DB: `CreatedAt/By/ByWindows` gesetzt, `Source = 'Manual'`, `SageUebergabeStatus = 0`. |
| 106.10.e | Negativ Duplikate: zweiten Kreditor mit Kurzname `PULVER` anlegen; dritten mit Sage-Konto `K31013 `; zwei Kreditoren ohne Sage-Konto | Feldfehler „Kurzname 'PULVER' ist bereits vergeben …“ bzw. „Sage-Konto 'K31013' ist bereits einem anderen Kreditor zugeordnet.“, keine Exception; zwei Kreditoren ohne Konto sind erlaubt. |
| 106.10.f | P: Details → Bearbeiten, Ort ändern, speichern | Gespeichert; DB `ModifiedAt/By/ByWindows` gesetzt. Eigenen Kurznamen nur in der Schreibweise ändern (`pulver`) → kein Duplikatfehler. |
| 106.10.g | P: Details → „Deaktivieren“ | Meldung „… wurde deaktiviert“, Liste zeigt Status „Inaktiv“; im Default-Filter verschwunden; DB `Modified*` erneut gesetzt. Details → „Reaktivieren“ → wieder unter „Aktiv“. Nirgends ein Lösch-Knopf. |
| 106.10.h | Herkunft Sage simulieren: `UPDATE Creditors SET Source='Sage', ShortName=NULL WHERE Id=<id>`; P: Bearbeiten | Hinweis „Dieser Kreditor stammt aus Sage …“; Name, Adresse, Kontakt, E-Mail, Sage-Konto **readonly**; Kurzname ohne Pflicht-Stern. Bemerkung + Matchcode + Beschichter ändern, Kurzname leer lassen → gespeichert. Readonly-Feld per Browser-Entwicklertools ändern (z. B. Name) und speichern → Name in der DB **unverändert**. Liste zeigt Herkunft „Sage“. |
| 106.10.i | Rollen: L öffnet Kreditoren und Details; L ruft `/Creditors/Create`, `/Creditors/Edit/<id>` direkt auf | Liste + Details sichtbar, Hinweis „Nur-Lesen-Zugriff“, kein „Kreditor anlegen“/„Bearbeiten“/„Deaktivieren“, Meldekarte ohne „Als Kreditor anlegen“; Direktaufrufe → AccessDenied. |
| 106.10.j | Rollen: O | Menüpunkt „Kreditoren“ fehlt (ganzes Stammdaten-Menü fehlt); `/Creditors` → AccessDenied. A: alles wie P. Rollenübersicht nennt Kreditoren bei `masterdata`/`masterdata_read`. |
| 106.10.k | IDEAL Meldekarte: Liste öffnen | Gelbe Karte „Dienstleister ohne Kreditor: n“ listet genau die nicht-leeren Texte aus Prüfabfrage A ohne aktiven Kreditor mit gleichem Kurznamen, je mit Zahl der Auftragszeilen (wie `Zeilen` der Abfrage). |
| 106.10.l | P: bei einem Text „Als Kreditor anlegen“ | Formular mit vorbelegtem Kurznamen (= Text) und Beschichter an; Name ergänzen, speichern → Text verschwindet aus der Karte. Kurzname in anderer Groß-/Kleinschreibung bzw. mit Leerzeichen am Rand wirkt genauso. |
| 106.10.m | Diesen Kreditor deaktivieren | Text erscheint wieder in der Karte mit Kennzeichen „nur inaktiv“ und Link „Inaktive Kreditoren zeigen“. |
| 106.10.n | AKE (`FaHierarchyOrderInfos` leer) bzw. IDEAL, wenn alle Texte zugeordnet sind | Keine Karte, keine Fehlermeldung; Liste voll nutzbar. |
| 106.10.o | Regression Beschichtungsauftrag (IDEAL): Beschichtung → Liste und Druck eines Auftrags | Dienstleister weiterhin als Text wie vorher, unabhängig von Kreditoren. |
| 106.10.p | DB: `SQL/104` ein zweites Mal ausführen; auf einer FreshInstall-DB `SQL/00_FreshInstall.sql` | Zweiter Lauf ohne Fehler und ohne Änderung; FreshInstall legt `Creditors` mit `IX_Creditors_SageKto`/`IX_Creditors_ShortName` (gefiltert `IS NOT NULL`) an und trägt `20261106195956_AddCreditors` ein. Negativ: `INSERT` mit doppeltem `SageKto` per SQL → Fehler 2601 (Index als zweite Linie). |

### TS-106.11 — E8b: Debitoren-Stamm und Pflege-Oberfläche

**Standort:** beide. **Migration:** `SQL/105_AddDebitors.sql` (neue Tabelle `Debitors`, rein additiv).
**Vorbedingungen:** `SQL/104` und `SQL/105` eingespielt; Benutzer **P** mit `masterdata`, **L** mit nur `masterdata_read`, **O** ohne Stammdaten-Rolle,
**A** `admin`. Für 106.11.a: ≥ 30 Debitoren (wiederholtes Anlegen oder SQL-Insert mit `Source = 'Manual'`), davon einige inaktiv.
**Regeln:** wie TS-106.10 (Kreditoren), aber ohne Beschichter-Filter/-Feld und ohne Dienstleister-Karte; zusätzlich zeigt die Detailseite
„Vertreter“ und „Kundenname FA“ (Sage-Felder, füllt erst der Lese-Abgleich E8c). Kein Löschen.
Automatisiert: `DebitorsControllerTests` (Read/Edit-Split, Audit, Schreibschutz, Duplikat-Guards, Status-Filter, Spaltenfilter vor Pagination),
`DebitorRepositoryTests`, `DebitorsViewTests` (Spaltenschlüssel, viewKey, Menüpunkt). Index-DDL ist Manual-UAT.

| Nr | Schritte | Erwartung |
|---|---|---|
| 106.11.a | P: Stammdaten → Debitoren | Menüpunkt „Debitoren“ direkt unter „Kreditoren“. Liste mit Filterkarte (Status **Aktiv** vorgewählt, **kein** „Nur Beschichter“), Pagination 25/50/100/Alle (Benutzer-Default greift), Spaltenfilter auf jeder Spalte, Zahnrad. „Inaktiv“/„Alle“ filtern richtig; Spaltenfilter (auch `a,b` und `!x`) wirkt über alle Seiten, „x Einträge“ stimmt. Spalte ausblenden, Reload → bleibt ausgeblendet (kein 400 in der Konsole). Keine gelbe Dienstleister-Karte. |
| 106.11.b | Negativ Filter: Spaltenfilter Name = `zzzz` | „Keine Debitoren für diese Filterung.“, Gesamtzahl 0. |
| 106.11.c | P: „Debitor anlegen“ ohne Name und Kurzname speichern; dann E-Mail `abc` | Feldfehler „Name ist ein Pflichtfeld.“ / „Kurzname ist ein Pflichtfeld.“ bzw. „Bitte eine gültige E-Mail-Adresse eingeben.“; nichts gespeichert. Formular ohne Beschichter-Schalter. |
| 106.11.d | Anlegen Kurzname ` Hofer `, Name `Hofer & Co 'KG' <AT>`, Sage-Konto ` D10001 ` | Erfolgsmeldung „Debitor 'Hofer' wurde angelegt.“; Liste zeigt `Hofer`, `D10001`, Herkunft „WMS“, Sonderzeichen korrekt. DB: `CreatedAt/By/ByWindows` gesetzt, `Source = 'Manual'`, `SageUebergabeStatus = 0`, `SageVertreter`/`SageKundenNameFA` NULL. |
| 106.11.e | Negativ Duplikate: Kurzname `HOFER`; Sage-Konto `D10001 `; zwei Debitoren ohne Konto | Feldfehler „Kurzname 'HOFER' ist bereits vergeben …“ bzw. „Sage-Konto 'D10001' ist bereits einem anderen Debitor zugeordnet.“; zwei ohne Konto erlaubt. Ein Kreditor mit gleichem Kurznamen/Konto stört **nicht** (getrennte Stämme). |
| 106.11.f | P: Details → Bearbeiten, Ort ändern; eigenen Kurznamen nur in Schreibweise ändern | Gespeichert, `Modified*` gesetzt; kein Duplikatfehler. |
| 106.11.g | P: Details → „Deaktivieren“, danach „Reaktivieren“ | „… wurde deaktiviert“ / „… reaktiviert“, Listenfilter folgt, `Modified*` jeweils gesetzt. Kein Lösch-Knopf. |
| 106.11.h | Herkunft Sage simulieren: `UPDATE Debitors SET Source='Sage', ShortName=NULL, SageVertreter='V01', SageKundenNameFA='HOFER LINZ' WHERE Id=<id>`; P: Details, dann Bearbeiten | Details: Herkunft „Sage“, Vertreter `V01`, Kundenname FA `HOFER LINZ`. Bearbeiten: Hinweis „Dieser Debitor stammt aus Sage …“; Name, Adresse, Kontakt, E-Mail, Sage-Konto readonly; Kurzname ohne Stern. Bemerkung ändern, Kurzname leer → gespeichert. Readonly-Feld per Entwicklertools ändern → DB unverändert; `SageVertreter`/`SageKundenNameFA` bleiben. |
| 106.11.i | Rollen: L öffnet Liste und Details; ruft `/Debitors/Create`, `/Debitors/Edit/<id>` direkt auf, sendet POST `/Debitors/SetActive` | Liste + Details sichtbar mit „Nur-Lesen-Zugriff“, keine Knöpfe; Direktaufrufe und POST → AccessDenied. |
| 106.11.j | Rollen: O; A | O: kein Menüpunkt, `/Debitors` → AccessDenied. A: alles wie P. Rollenübersicht und Hilfe nennen Debitoren bei `masterdata`/`masterdata_read`. |
| 106.11.k | DB: `SQL/105` zweimal ausführen; FreshInstall-DB mit `SQL/00_FreshInstall.sql` | Zweiter Lauf fehlerfrei ohne Änderung; FreshInstall legt `Debitors` mit `IX_Debitors_SageKto`/`IX_Debitors_ShortName` (gefiltert) an und trägt `20261106200713_AddDebitors` ein. Negativ: doppeltes `SageKto` per SQL-`INSERT` → Fehler 2601. |
| 106.11.m | Nachtrag G6: wie TS-106.10.0 für Debitoren | Anlegen nur A („Debitor anlegen (Notfall)“), Matchcode/aktiv bei Herkunft Sage aus Sage, kein Deaktivieren bei Herkunft Sage, Meldung „n Debitoren sind nur im WMS angelegt“. |
| 106.11.l | Regression Kreditoren | TS-106.10.a und .k unverändert (Beschichter-Filter, Meldekarte nur bei Kreditoren). |

### TS-106.12 — E8c: Lese-Abgleich Kontokorrent → Kreditoren/Debitoren, Artikel-Lieferanten

**Standort:** beide (Test am AKE-Test `sagetest01` / `ake_TEST2026;1`). **Migration:** `SQL/106_AddArticleSuppliers.sql` (neue Tabelle
`ArticleSuppliers`, rein additiv). **Vorbedingungen:** `SQL/104`–`SQL/106` eingespielt; SData-Endpunkt, Zugangsdaten, TLS-Schalter und
Fehlermail wie TS-106.1; `WorkerSettings:SyncDryRun = false`; Artikel-Abgleich gelaufen (00722001 im WMS). P-K1 beantwortet: `KtoArt` K = 2.287,
D = 534 am AKE-Test. Vorher im WMS anlegen (Pflege-Oberfläche): Kreditor **A** Name „Muster GmbH“ ohne Sage-Konto; Kreditor **B** mit
Sage-Konto eines echten Lieferanten (z. B. `K31013`), Name „Pulver Test“, Kurzname `PT`, Bemerkung „WMS-Notiz“, Beschichter an, deaktiviert.
Für .e den Sage-Namen eines Kontos notieren (Q5: Name1 + Name2 aus `Adressen`, z. B. `K31013` → `"carlo" Loysch GmbH`; Rückfall
Matchcode nur ohne Adresse) und Kreditor **C** ohne Sage-Konto mit genau diesem Namen anlegen.
Automatisiert: `KontokorrentArtikelLieferantTests` (Abfrage/Filter, Sage- vs. WMS-Felder, Verknüpfung, Dublette, Missing, Empty-Guard,
DryRun, SData-Fehler, Debitoren, Artikel-Lieferanten-Upsert/Hauptlieferant/Entfernen, Gates aus, DI; Nachtrag QA-B6: Adressen/Ansprechpartner,
Sage gewinnt inkl. Matchcode/aktiv (G6), Adressen nicht lesbar, Sage-Wert zu lang für die WMS-Spalte). Index-DDL ist Manual-UAT.
**Nachtrag 2026-11-07 (QA-Befund B6, Spec-Nr. TS-106.10):** Der Abgleich liest zusätzlich `Adressen` (Verweis `Kontokorrent.Adresse`) und
`Ansprechpartner`: Name = `Name1` (+ `Name2`), Straße/PLZ/Ort/Land = `LieferStrasse`/`LieferPLZ`/`LieferOrt`/`LieferLand`, Telefon, E-Mail,
Ansprechpartner = erster nach Nummer („Vorname Nachname“, weitere als „(+n weitere)“), Debitor zusätzlich `USER_KundenNameFA`. Der WMS-Matchcode
ist der Sage-Matchcode (G6: Sage gewinnt, auch bei `aktiv`); der Kurzname bleibt leer (WMS-Zuordnungsschlüssel der Dienstleister-Texte –
Sage-Matchcodes sind nicht eindeutig: 2.287 Kreditoren-Konten, 2.280 verschiedene Matchcodes). Referenzwerte AKE-Test (Sage-DB
`ake_TEST2026`, lesend geprüft 2026-11-07): `K31013` → Name `"carlo" Loysch GmbH`, `Pielacher Straße 50`, `3390` `Melk`, `AT`,
`+43-2752-52911`, `office@carlo.at`, kein Ansprechpartner.

| Nr | Schritte | Erwartung |
|---|---|---|
| 106.12.a | Default: alle drei Schalter fehlen bzw. `false`; einen Zyklus laufen lassen | Keine Läufe „Kreditoren-“, „Debitoren-“, „Artikel-Lieferanten-Abgleich (Sage)“; `Creditors`/`Debitors`/`ArticleSuppliers` unverändert; übrige Läufe wie vorher. |
| 106.12.b | `Sync:KreditorenEnabled = true`, ein Zyklus | Lauf „Kreditoren-Abgleich (Sage)“ erfolgreich, Info „Quelle SData: Kontokorrent … (KtoArt eq 'K')“; `gelesen` = 2.287 (P-K1). `SELECT COUNT(*) FROM Creditors WHERE Source='Sage'` ≈ `gelesen`; neue Zeilen: `ShortName` NULL, Name = Sage-`Name1` (+`Name2`), `Matchcode` = `SageMatchcode`, Straße/PLZ/Ort/Land/Telefon/E-Mail aus der Sage-Adresse, `SageKto`/`SageMatchcode`/`SageGruppe`/`SageZahlungskondition`/`SageEUUStID`/`SageEKMindestbestellwert` gefüllt (wo in Sage gesetzt), `CreatedBy = 'KontokorrentSync'`; je eine Info-Zeile „Quelle SData: Adressen …“ und „… Ansprechpartner …“, **keine** Warnung „nicht lesbar“. Liste Stammdaten → Kreditoren zeigt sie mit Herkunft „Sage“; Bearbeiten: Sage-Felder readonly. |
| 106.12.c | Kreditor B prüfen | Verknüpft: Herkunft jetzt „Sage“, Name jetzt der Sage-Name (`"carlo" Loysch GmbH`), Adresse/Telefon/E-Mail aus Sage, Matchcode = Sage-Matchcode (`Loysch "carlo" GmbH, Melk`), **aktiv** wie in Sage (G6: Sage gewinnt, das WMS-„deaktiviert“ wird überschrieben); Kurzname `PT`, Bemerkung, Beschichter unverändert; Sage-Felder gefüllt; Info „Mit Sage verknuepft … K31013“, Count `verknuepft` = 1; `ModifiedBy = 'KontokorrentSync'`. |
| 106.12.d | Kreditor A | Bleibt `Source = 'Manual'`, ohne Sage-Konto; kein zweiter Datensatz „Muster GmbH“, außer Sage hat ein Konto mit diesem Matchcode. |
| 106.12.e | Negativ Dublette: Kreditor C (Name = Sage-Name1 (+Name2) eines Kontos, ohne Konto; Nachtrag Q5: verglichen wird mit dem Namen aus `Adressen`, nur ohne Adresse mit dem Matchcode) | Warnung „1 moegliche Dublette(n), nicht verknuepft …“ mit Name, WMS-Id und Sage-Konto; C bleibt unverändert, das Sage-Konto ist zusätzlich als eigener Kreditor angelegt. Warnung erscheint bei jedem Lauf, bis C deaktiviert oder umbenannt ist (Sage-Konto bei C nachtragen geht nicht: eindeutig, schon vergeben). |
| 106.12.f | Zweiter Zyklus ohne Änderung in Sage | `neu` = 0, `aktualisiert` = 0, `unveraendert` = `gelesen`; keine `ModifiedAt`-Änderung. |
| 106.12.g | Negativ Missing: per SQL ein Sage-Kreditor auf ein nicht existierendes Konto setzen (`UPDATE Creditors SET SageKto='KZZZZZ' WHERE Id=<id>`), Zyklus | Warnung „Nicht mehr in Sage (1 neu gekennzeichnet): KZZZZZ …“; Datensatz bleibt, Details zeigt „Nicht in Sage seit“. Konto zurücksetzen, Zyklus → `wieder_in_sage` = 1, Feld leer. |
| 106.12.h | `Sync:DebitorenEnabled = true`, ein Zyklus | Lauf „Debitoren-Abgleich (Sage)“, Filter `KtoArt eq 'D'`, `gelesen` = 534; Debitoren mit Herkunft „Sage“, `SageVertreter` gefüllt wo in Sage gesetzt, `SageKundenNameFA` aus `Adressen.USER_KundenNameFA` gefüllt, wo in Sage gesetzt; Name/Adresse wie .b. Keine Kreditoren angelegt. |
| 106.12.i | `Sync:ArtikelLieferantEnabled = true`, ein Zyklus (nach .b) | Lauf „Artikel-Lieferanten-Abgleich (Sage)“ nach dem Kreditoren-Lauf; zwei Info-Zeilen (`ArtikelLieferantInfo`, `ArtikelVariantenExtended`). `SELECT * FROM ArticleSuppliers s JOIN Creditors c ON c.Id=s.CreditorId WHERE s.Artikelnummer='00722001'` → Zeile mit `c.SageKto = 'K31013'`, `IstHauptlieferant = 1`, Bestellnummer/Mindestmenge/WBZ/Preis gefüllt. Count `artikel_fehlt` = Zeilen zu Artikeln, die es im WMS nicht gibt (Info, keine Warnung). |
| 106.12.j | Negativ Reihenfolge: Kreditoren-Schalter aus, `Creditors` ohne Sage-Konten (frische Test-DB), Artikel-Lieferanten an | Warnung „… Lieferant(en) ohne Kreditor im WMS (Kreditoren-Abgleich Sync:KreditorenEnabled aktiv?) …“ und „Keine Artikel-Lieferant-Zeile zuordenbar — nichts entfernt (Empty-Guard)“; vorhandene `ArticleSuppliers` bleiben. |
| 106.12.k | Zuordnung entfernt: per SQL eine `ArticleSuppliers`-Zeile mit Phantom-Paar einfügen (anderer Artikel × anderer Kreditor), Zyklus | Info „1 Zuordnung(en) nicht mehr in Sage, entfernt“, Zeile weg; Kreditor und Artikel bleiben. |
| 106.12.l | Negativ SData-Fehler: `SData:BaseUrl` auf einen falschen Port, Kreditoren an | Lauf Fehler „Sage (SData) nicht lesbar …“, Fehlermail; Kreditoren unverändert (nichts als „nicht in Sage“ gekennzeichnet). URL zurück. |
| 106.12.m | DryRun: `WorkerSettings:SyncDryRun = true`, alle drei an, ein Sage-Konto per SQL löschen (`DELETE` eines frisch angelegten Sage-Kreditors ohne Artikel-Lieferanten) | Läufe mit „[DryRun]“ und Zählern (`neu` = 1), aber DB unverändert. |
| 106.12.n | DB: `SQL/106` zweimal; FreshInstall-DB mit `SQL/00_FreshInstall.sql` | Zweiter Lauf fehlerfrei ohne Änderung; FreshInstall legt `ArticleSuppliers` mit `IX_ArticleSuppliers_ArticleId_CreditorId` (UNIQUE) und `IX_ArticleSuppliers_CreditorId` an und trägt `20261106201439_AddArticleSuppliers` ein. Negativ: doppeltes Paar per `INSERT` → Fehler 2601; `DELETE` eines Kreditors mit Artikel-Lieferanten → FK-Fehler 547 (Restrict); `DELETE` eines Artikels löscht seine Zeilen (Cascade). |
| 106.12.p | Nach .b: `SELECT TOP 5 SageKto, Name, Street, PostalCode, City, Country, Phone, Email, ContactPerson FROM Creditors WHERE Source='Sage' AND ContactPerson LIKE '%weitere%'`; Gegenprobe in Sage (Ansprechpartner des Kontos) | Werte = Sage-Adresse; Ansprechpartner = kleinste Nummer, Zusatz „(+n weitere)“ mit n = Anzahl − 1. `K31013` wie im Nachtrag oben. Ein Konto ohne Ansprechpartner in Sage: `ContactPerson` bleibt wie vorher. |
| 106.12.q | Details eines Sage-Kreditors und eines WMS-Kreditors öffnen (Karte „Sage“), ebenso eines Debitors | Sage: „Name, Adresse, Kontakt und Sage-Felder füllt der Kreditoren-Abgleich aus Sage. Zuletzt vom Abgleich geändert: …“; WMS: „Herkunft WMS: Die Sage-Felder bleiben leer, bis …“; darunter „Letzter Kreditoren-Abgleich: <Zeit>“ (Badge „fehlgeschlagen“ bzw. „mit Warnung“, wenn der letzte Protokolleintrag Fehler/Warnung ist). Ohne Lauf: „Der Kreditoren-Abgleich ist noch nicht gelaufen (Schalter `Sync:KreditorenEnabled` …)“. Der alte Text „(folgt)“ erscheint nirgends. |
| 106.12.r | Bearbeiten des Sage-Kreditors aus .b | Name, Straße, PLZ, Ort, Land, Ansprechpartner, Telefon, E-Mail, Sage-Konto readonly (unverändert TS-106.10.h); Matchcode readonly (G6, Sage-Matchcode); Kurzname, Bemerkung, Beschichter änderbar und bleiben beim nächsten Zyklus erhalten. Herkunft-WMS-Kreditor: alles wie bisher editierbar. |
| 106.12.v | Nachtrag QA-Q1: Prüfabfrage am Zielsystem (AKE-Test erledigt: PLZ max 20, E-Mail max 60): `SELECT MAX(LEN(a.LieferPLZ)), MAX(LEN(a.EMail)) FROM KHKKontokorrent k JOIN KHKAdressen a ON a.Adresse=k.Adresse AND a.Mandant=k.Mandant WHERE k.Mandant=1` | Ist ein Wert länger als die WMS-Spalte (PLZ 20, E-Mail 200): Lauf trotzdem erfolgreich, Warnung „n Sage-Wert(e) laenger als die WMS-Spalte, nicht uebernommen …“ mit Konto und Feld; das Feld behält den alten Wert. Automatisiert: `Kreditoren_SageWertZuLang_NichtUebernommen_LaufLaeuft`. |
| 106.12.w | Nachtrag Filter-Fix: Artikel-Lieferanten-Lauf (nach .i) | Info-Zeile „Quelle SData: ArtikelVariantenExtended …“ mit ≈ 23.509 Zeilen (Filter `Hauptlieferant like '_%'`, vorher `ne ''` = 0); Count `hauptlieferant` > 0; 00722001 ↔ K31013 `IstHauptlieferant = 1`. Negativ (automatisiert `ArtikelLieferant_VariantenLeer_Warnung_HauptlieferantBleibt`): 0 Varianten → Warnung „… 0 Zeilen mit Hauptlieferant … Filter pruefen“, Kennzeichen bleiben unverändert. |
| 106.12.t | Nachtrag G6: Kreditor A (Herkunft WMS, ohne Sage-Konto) aus der Vorbedingung, ein Zyklus | Warnung „1 Kreditoren nur im WMS (ohne Sage-Konto; Sage ist fuehrend …): Muster GmbH (WMS-Id …)“, Count `wms_eigen` = 1; A bleibt unverändert (nicht gelöscht). |
| 106.12.u | Nachtrag G6: in Sage (Testsystem) ein Konto auf inaktiv setzen bzw. – lesend – ein inaktives Konto suchen, Zyklus | Kreditor im WMS wird inaktiv (`IsActive = 0`), `ModifiedBy = 'KontokorrentSync'`. |
| 106.12.s | Negativ Adressen nicht lesbar (nur automatisiert: `Kreditoren_AdressenNichtLesbar_NameBleibt`; manuell nur, falls Sage das Feld im Lesevertrag umbenennt) | Warnung „Adressen aus Sage (SData) nicht lesbar — Name, Adresse und Kontakt bleiben unveraendert …“, Lauf trotzdem erfolgreich, Sage-Felder aus `Kontokorrent` aktualisiert, Name **nicht** auf den Matchcode zurückgesetzt. |
| 106.12.o | AKE-Regression (Spec TS-106.12): AKE-Test-DB mit Default-Schaltern | Artikel-, Lagerplatz-, Lagerbestand-Abgleich wie vorher; Menü „Kreditoren“/„Debitoren“ sichtbar für Stammdaten-Rollen. |

### TS-106.13 — E9: Offene Übergaben an Sage (Lagerplätze, BDE-Mitarbeiter; Outbox ohne Senden)

**Standort:** beide. **Migration:** `SQL/107_AddSageStammdatenOutboxAndUebergabeStatus.sql` (Tabelle `SageStammdatenOutbox`; Spalten
`StorageLocations.SageUebergabeStatus/-Am/-Fehler`, `BdeOperators.Source/SageUebergabeStatus/-Am/-Fehler`; rein additiv, DEFAULT füllt den Bestand).
**Vorbedingungen:** `SQL/107` eingespielt; Benutzer A mit Stammdaten (`masterdata`), Benutzer R nur `masterdata_read`, Benutzer S mit
BDE-Schichtleitung (`bde_shiftlead`), `BdeAktiv = true`; Sage-Zugang (AKE-Test bzw. IDEAL), um einen Lagerplatz anzulegen; `Sync:LagerplaetzeEnabled = true`.
Für den Mitarbeiterteil ein Sage-PPS-Mitarbeiter M, dessen `Nummer` noch keinem BDE-Mitarbeiter zugeordnet ist, und `Sync:PpsMitarbeiterEnabled = true`
(TS-106.8). Vorher notieren: `SELECT COUNT(*) FROM StorageLocations WHERE SageUebergabeStatus <> 0` (= 0 nach Deploy).
**Regeln:** Status `0` nicht nötig (Bestand, Anzeige seit v1.79.0 „nicht nötig“ als gedämpfter Text, vorher leer), `1` Offen, `2` Übergeben, `3` Fehler (heute ungenutzt). Nur Neuanlagen über die Oberfläche
werden `Offen`. Outbox-Status wie die Lagerbuchungs-Queue (`0` Offen, `2` Bestätigt). Es gibt **keinen** Worker: nichts wird gesendet.
Automatisiert: `SageUebergabeTests` (Web: Neuanlage Offen + Outbox, Bestand ohne Outbox, Liste/Filter/Zählung, Zuordnung → Übergeben + Outbox
bestätigt, Bestand-Zuordnung bleibt nicht nötig, Lösen), `LagerplatzSyncServiceTests.Run_ManualRecord_UebergabeOffen_WirdUebernommen_Bestand_BleibtKonflikt`.

| Nr | Schritte | Erwartung |
|---|---|---|
| 106.13.a | Migration: `SQL/107` zweimal ausführen; frische DB mit `SQL/00_FreshInstall.sql` | 1. Lauf: sieben Spalten, Tabelle `SageStammdatenOutbox` mit `IX_SageStammdatenOutbox_EntityType_EntityKey` und `IX_SageStammdatenOutbox_Status`, History `20261106202615_AddSageStammdatenOutboxAndUebergabeStatus`. 2. Lauf: keine Meldung, keine zweite History-Zeile. FreshInstall legt alles an und trägt die MigrationId ein. `SELECT SageUebergabeStatus, COUNT(*) FROM StorageLocations GROUP BY SageUebergabeStatus` → nur `0`; `SELECT DISTINCT Source FROM BdeOperators` → nur `Manual`. |
| 106.13.b | Benutzer A: Stammdaten › Lagerplätze | Neue Spalte „Übergabe an Sage“ („nicht nötig“ bei allen vorhandenen Plätzen, gedämpfter Text, seit v1.79.0), mit Spaltenfilter und im Zahnrad ein-/ausblendbar; kein Hinweis über der Liste. |
| 106.13.c | Benutzer A: Lagerplatz `E9TEST` anlegen | Liste: Spalte „Übergabe an Sage“ = gelbes Badge **Offen**, Quelle „Manuell“; gelber Hinweis „1 Lagerplatz wartet auf die Übergabe an Sage …“ mit Knopf „Offene Übergaben anzeigen“. DB: `SELECT * FROM SageStammdatenOutbox` → eine Zeile `EntityType = 'StorageLocation'`, `EntityKey` = Id des Platzes, `Status = 0`, `AttemptCount = 0`, `PayloadJson` mit Code/Bezeichnung/Zone, `CreatedBy` = Benutzer A. |
| 106.13.d | „Offene Übergaben anzeigen“ | Blauer Kasten „Offene Übergaben an Sage“ mit Handlungsanweisung; Liste zeigt nur `E9TEST` (und andere offene); Seitenwechsel und Spaltenfilter behalten den Filter; „Alle Lagerplätze zeigen“ hebt ihn auf. Ohne offene Plätze: „Keine offenen Übergaben an Sage.“ |
| 106.13.e | Negativ kein Senden: 15 Minuten warten bzw. einen Dienst-Zyklus laufen lassen (vor dem Anlegen in Sage) | Outbox-Zeile unverändert `Status = 0`, `AttemptCount = 0`; im Aktivitäts-Protokoll kein Lauf, der Stammdaten an Sage schickt; in Sage kein Platz `E9TEST`. |
| 106.13.f | In Sage den Lagerplatz mit Kurzbezeichnung `E9TEST` anlegen; Lagerplatz-Abgleich laufen lassen | Lauf „Lagerplatz“: Info „Lagerplatz E9TEST (im WMS angelegt, Übergabe offen) in Sage gefunden -> übernommen, Quelle jetzt Sage …“, **keine** Konflikt-Warnung, `konflikte` ohne diesen Platz, `aktualisiert` +1. Liste: Quelle „Sage“, Übergabe **Übergeben** (grün), Buchbar und Sage-Buchung unverändert. DB: `SageUebergabeAm` gesetzt, `SageLagerkennung = 'E9TEST'`, `SageLagerplatzId` gefüllt, `ModifiedBy = 'system:sync'`; Outbox-Zeile `Status = 2`, `ModifiedBy = 'system:sync'`. Hinweis über der Liste verschwunden. |
| 106.13.g | Nächster Lagerbestand-Abgleich | Bestand von `E9TEST` wird jetzt wie bei Sage-Plätzen mit Sage abgeglichen (vorher übersprungen). |
| 106.13.h | Negativ Bestand: ein vor dem Deploy angelegter manueller Platz, dessen Code es in Sage gibt (oder per SQL einen Bestands-Platz auf einen Sage-Code setzen), Lagerplatz-Abgleich | Unverändert wie vor v1.73.0: Warnung „Konflikt: Lagerplatz … existiert manuell, Sage-Eintrag ignoriert.“, Platz bleibt „Manuell“, Übergabe „nicht nötig“. |
| 106.13.i | Negativ Rolle: Benutzer R öffnet die Lagerplatzliste und `/StorageLocations?onlyUebergabeOffen=true` | Liste und Hinweis sichtbar (Lesen), kein „Neuen Lagerplatz anlegen“; Direkt-POST an `/StorageLocations/Create` → AccessDenied. |
| 106.13.j | Benutzer S: BDE-Stammdaten › Operatoren, neuen Operator mit Personalnummer = Nummer von M anlegen | Spalte „Übergabe an Sage“ = **Offen**, Hinweis „1 BDE-Mitarbeiter wartet …“; „Offene Übergaben anzeigen“ filtert auf ihn. DB: `Source = 'Manual'`, `SageUebergabeStatus = 1`; Outbox-Zeile `EntityType = 'BdeOperator'`, `Status = 0`. Vorhandene Operatoren: „nicht nötig“ (gedämpft, seit v1.79.0). |
| 106.13.k | Reiter „Sage-Zuordnung“: M dem neuen Operator zuordnen (Vorschlag ist vorbelegt) | Grüne Meldung wie TS-106.8.e; Operatoren: Übergabe **Übergeben**, Sage-Nr. gefüllt. DB: `Source = 'Sage'`, `SageUebergabeStatus = 2`, `SageUebergabeAm` gesetzt, Audit = Benutzer S; Outbox-Zeile `Status = 2`, `ModifiedBy` = Benutzer S. |
| 106.13.l | Negativ Bestand-Operator zuordnen bzw. Zuordnung lösen | Ein Operator aus der Zeit vor dem Deploy wird zugeordnet: `Source = 'Sage'`, Übergabe bleibt „nicht nötig“ (`0`), keine Outbox-Zeile. „Lösen“: `Source = 'Manual'`, Übergabe-Status unverändert. |
| 106.13.m | Negativ Rolle: Benutzer ohne BDE-Schichtleitung ruft `/BdeMasterData?onlyUebergabeOffen=true` auf | AccessDenied (wie TS-106.8.i). |

## TS-108 — Fehlteile „Nur meine Werkbänke“: Trefferzahl und Badges über alle Seiten (v1.75.0)

Spec `2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-spec` (Bug). **Standort:** beide (wo `LagerbestellungAktiv` gesetzt ist).
**Keine Migration.** Ergänzt 33.12–33.14.
**Vorbedingungen:** Benutzer **U** mit Werkbänken **A** und **B**, fremde Werkbank **C**; Seitengröße **25** (die Liste kennt nur
25/50/100/Alle, `PageSize.cs:15`). Tab „Wird nicht nachgeliefert“: mindestens 26 Fehlteile auf C mit jüngerem Abschlussdatum, 3 auf A,
25 auf B (zusammen 28 eigene → zwei Seiten); analog einige „Offene Fehlteile“ auf A und C. (Auf DEV AKE von der QA so angelegt: QA108-A/B/C.)
**Regel:** Die Werkbank-Menge (eigene Werkbänke bzw. gewählte Werkbank) wird im Repository **vor** Zählen und Blättern gefiltert (ADR 0005);
Liste und beide Tab-Badges nutzen dieselbe Menge.
Automatisiert: `MissingPartsControllerTests` (`…_AK3`, `…_AK4`, `…_AK6`, bestehende angepasst), `MissingPartsLagerControllerTests`,
`WarehouseRequisitionRepositoryTests.GetMissingPartsAsync_AppliesWorkplaceFilter` (Menge, Seite 2, leere Menge).

| Nr | Schritte | Erwartung |
|---|---|---|
| 108.1 | Als U `Lager → Meine Fehlteile`, Tab „Wird nicht nachgeliefert“ | Trefferzahl 28 (A+B), Seite 1 mit 25 Zeilen, keine Zeile von C. Badge „Wird nicht nachgeliefert“ = 28, Badge „Offene Fehlteile“ = Anzahl auf A+B (nicht mehr alle Werkbänke). |
| 108.2 | Seite 2 | 3 Zeilen; Seiten 1+2 zusammen genau die 28 eigenen Zeilen, Trefferzahl 28 auf beiden Seiten. (Vorher: Seite 1 leer, weil die 25 jüngsten Zeilen von C stammten, bzw. Trefferzahl = Zeilen der Seite.) |
| 108.3 | Spaltenfilter Werkbank = `A` | 3 Zeilen, Trefferzahl 3. |
| 108.4 | Haken „Nur meine Werkbänke“ aus | Alle Werkbänke inkl. C; Badges zählen alle. |
| 108.5 | Negativ: URL `/MissingParts?mineOnly=true&workplaceId=<Id von C>` | Leere Liste, Trefferzahl 0, **beide Badges 0**. |
| 108.6 | Benutzer mit genau einer Werkbank | Wie bisher: nur diese Werkbank, Badges passend. |
| 108.7 | Benutzer ohne Werkbank-Zuordnung | Banner wie 33.13, keine Fehlermeldung. |
| 108.8 | Regression `Lager → Fehlteile (Lager)` mit und ohne Werkbank-Auswahl | Unverändert wie 33.14 (Liste, Lagerplatz-Bestand, Badges). |

## TS-109 — Einstellungen: Auswahlfelder und Beschreibung (v1.77.0)

Spec `2026-11-07-einstellungen-auswahlfelder-und-beschreibung-spec` (Epic E1–E4b, ADR 0020). **Standort:** beide. **Keine Migration.** Nur Web.
**Vorbedingungen:** Admin-Benutzer; P-E1c (`SELECT [Key],[Value] FROM dbo.AppSettings ORDER BY [Key]`) und dasselbe für `ServiceSettings` vorher
notieren. Automatisiert: `AppSettingDefinitionsTests` (Seed-Gleichheit 38 Keys, Katalog vollständig), `SettingsControllerKatalogTests`,
`SettingValidatorTests` (inkl. Drift-Guards beider Kataloge, Standorteinstellungen), `EinstellungspruefungTests`, `ServiceSettingsControllerTests`,
`SettingsControllerQrErstesFeldTests`.

| Nr | Schritte | Erwartung |
|---|---|---|
| 109.1 | `/Settings` öffnen | Gruppen in Katalog-Reihenfolge (Termine und Beschichtung … Links), je Feld Anzeigename, Key klein, „Bewirkt“, „Verwendet in“, Standard, Standort; Warnhinweise gelb bei „Buchungen ins Minus erlauben“, „Windows-Anmeldung“; „Hardware-Barcodescanner“ sichtbar, auch ohne DB-Zeile. Suchfeld `scanner` zeigt nur passende Felder, „Nur geänderte“ nur abweichende (Badge „weicht vom Standard ab (Standard: …)“ als Text). Negativ: Benutzer ohne Stammdaten-Recht → Felder gesperrt, Nur-Lesen-Hinweis. |
| 109.2 | Unverändert „Einstellungen speichern“, danach P-E1c erneut | Identisch, keine neue Zeile. Negativ: Zeile `BeschichtungAbholtage` per SQL löschen, speichern → Zeile bleibt weg; FA-Liste zeigt weiter Abholtage Di/Do. |
| 109.3 | Bool-Lesart: `UPDATE AppSettings SET Value='1' WHERE [Key]='LagerbestellungAktiv'`; `/Settings` | Schalter „Lagerbestellungen“ steht auf an, Badge „ungültig: „1““, Sammelbanner oben; Menü Lagerbestellung sichtbar. Speichern (mit `BdeDefaultArbeitsgang` gesetzt, falls „nur FA-Meldung“ an) → Wert `true`. `/ServiceSettings`: `UPDATE ServiceSettings SET Value='' WHERE [Key]='Sync:ProductionOrdersEnabled'` → „Aktiviert“, Speichern schreibt `true`. |
| 109.4 | `/ServiceSettings` Quellenschalter | Sechs Dropdowns View/SData/Vergleich mit Text; IDEAL mit gesetzter Artikel-View: Sperrhinweis an Artikel und FA-Struktur, AKE nur FA-Struktur. `UPDATE ServiceSettings SET Value='sdata' WHERE [Key]='Sync:Quelle:Lagerplaetze'` → Option „„sdata“ (ungültig – bitte wählen)“; Speichern ohne Korrektur → Meldung mit „Quelle Lagerplatz-Abgleich“ und erlaubten Werten, nichts gespeichert. Nach Korrektur auf View: Lauf „Lagerplatz“ ohne Quellen-Warnung. |
| 109.5 | Serverprüfung: per Entwicklerwerkzeug im Formular `settings[OseonReportingHorizonDays]` = `99`, `settings[Erfunden]` = `x` hinzufügen, speichern | Meldung „Nicht gespeichert: „OSEON-Reporting: Horizont“: „99“ … von 1 bis 60“, P-E1c unverändert; `Erfunden` nie angelegt. „BDE: nur FA-Meldung“ an + leerer Default-Arbeitsgang → abgelehnt mit Hinweis auf den Default-Arbeitsgang. Gleiches für ungültigen Abholtag und Empfängergruppe, die es nicht gibt. |
| 109.6 | Bestandsprüfung: ungültigen Wert per SQL setzen (z. B. 109.3), App-Pool recyceln | Aktivitäts-Protokoll: Lauf „Einstellungsprüfung“ mit je einer Warnzeile (Key, Rohwert, erlaubte Werte, wirksamer Wert). Nach Korrektur und erneutem Recycle: kein neuer Lauf. |
| 109.7 | Regression | Sage-Verbindungstest liefert mit denselben Werten dasselbe Ergebnis; Master-Anzeige mit Link „Umschalten (eigene Seite)“; TLS-Warnung bei `false`; Standorteinstellungen speichern unverändert; QR-Einstellungen wirken wie TS-101. |

## TS-110 — EAN von Einkaufsartikeln scannen (v1.78.0)

Spec `2026-11-07-ean-einkaufsartikel-scan-spec`. **Standort:** beide (wirkt nur, wo `ArticleSageDetails.EANNummer` gefüllt ist = AKE mit
`Sync:Quelle:Artikel = SData`). **Keine Migration.** Nur Web.
**Vorbedingungen:** AKE-Testsystem nach einem Artikel-Lauf mit Quelle `SData` (TS-106.3.g). Prüfabfrage P-E5:
`SELECT COUNT(*), COUNT(NULLIF(LTRIM(RTRIM(EANNummer)),'')) FROM dbo.ArticleSageDetails`. Testartikel (AKE-Test, P-E2): **00726502** = EAN
`4002872120264`, **EK011488** = EAN `9007646008637`. Für Mehrtreffer per SQL an zwei weiteren Satelliten dieselbe EAN setzen (Testsystem).
Automatisiert: `ArticlesApiControllerTests.GetByEan_*`, `HardwareScannerTests.EanZweig_*`.

| Nr | Schritte | Erwartung |
|---|---|---|
| 110.1 | Wareneingang, Artikel-Scan (Kamera/Bild) eines EAN-13-Barcodes `4002872120264` | Artikel 00726502 gewählt, Rückmeldung „Gescannt: 00726502“, weiter wie ein Artikelnummer-Scan. |
| 110.2 | Dasselbe per Hardware-Scanner (Fokus im Artikelfeld) und über „Scannen ohne Feld“ | Gleiches Ergebnis. |
| 110.3 | Mehrtreffer: zwei Artikel mit gleicher EAN (SQL), scannen | Auswahlfenster „Mehrere Artikel zur EAN …“ mit beiden Einträgen (Artikelnummer – Bezeichnung), Fokus auf dem ersten; Auswahl füllt den Artikel; „Abbrechen“/Escape füllt nichts. |
| 110.4 | Negativ: EAN, die keinem Artikel gehört (`1111111111111`) | Verhalten wie vor v1.78.0 (Meldung „Nicht gefunden …“ bzw. Matchcode-Modus). |
| 110.5 | Negativ: Artikelnummer in GTIN-Form (rein numerisch, 8 Stellen, z. B. ein Artikel `12345678`) scannen | Wird über die Artikelnummer gefunden (F2), nie über eine EAN. |
| 110.6 | Negativ: Verbindung trennen (Netzwerk im Browser offline) und EAN scannen | Meldung „EAN … konnte nicht geprüft werden“, nichts befüllt. |
| 110.7 | Regression | Artikel-QR mit Trennzeichen, Lagerplatz- und FA-Scans unverändert (TS-101). IDEAL: EAN-Scan ohne Wirkung (wie 110.4). |
| 110.8 | Mehrtreffer-Fenster aus 110.3 offen lassen und mit dem Handscanner (Netum) einen zweiten Code scannen (EAN oder Artikelnummer); danach einen Treffer wählen und sofort weiterscannen | Zweiter Scan wird verworfen: Hinweis „Bitte zuerst den Artikel im Auswahlfenster wählen“, nichts unter dem Fenster befüllt, kein zweites Fenster, Seite nicht grau gesperrt. Nach Auswahl/Abbrechen liegt der Fokus wieder im Artikelfeld, der nächste Scan kommt ohne Antippen an (QA E2/E3). |

## TS-111 — Bereinigungen nach dem Bündel 2026-11-07 (v1.79.0)

Spec `2026-11-08-bereinigungen-nach-buendel-2026-11-07-spec`. **Standort:** beide. **SQL:** `SQL/108_RemoveAppSettingsRueckmeldungOhneLeser.sql`
(Daten-Bereinigung, keine Migration). Web + Dienst (Version).
Automatisiert: `BereinigungenV179Tests` (F1 Platz der Hinweise, F2 Ausweich-Lagerplatz + QA-W2 beider Masken, F5, F6), Worker-Tests ohne festes
Zeitfenster (`WorkerTestLauf`, F3).

| Nr | Schritte | Erwartung |
|---|---|---|
| 111.1 | IDEAL: `/ServiceSettings` öffnen (Artikel-View gesetzt). Danach Suche „Werkbänke“. Auf DEV `SageLagerbuchungSslZertifikatPruefen = false` setzen und neu laden (danach zurück). | Hinweis „SData ist hier gesperrt bis P-R1 …“ steht im Feld „Quelle Artikel-Abgleich“, „… P-F2“ im Feld „Quelle FA-Struktur“, jeweils über dem Trennstrich ihres Felds. Suche „Werkbänke“: kein fremder Hinweis bleibt stehen. TLS-Warnung steht im eigenen Feld. Negativ: AKE ohne Artikel-View zeigt bei Artikel keinen Sperrhinweis, nur bei FA-Struktur. |
| 111.2 | DEV: Zeile `NegativeBuchungLagerplatz` per SQL löschen und Platz `NAN` per SQL umbenennen (kein App-Recycle). `/Settings` öffnen, einen anderen Wert ändern und speichern; danach zurücksetzen. | Badge „ungültig: „NAN“ (Standard, keine Zeile)“ und Sammelbanner; Speichern des anderen Werts klappt. Negativ: mit vorhandenem `NAN` kein Badge. |
| 111.3 | DEV: `LagerbestellungAktiv = '1'` und `BestellungenAktiv = '1'` per SQL. `/Settings` öffnen, einmal speichern, dann P-1a. | Vorher: Lagerbestellungen „Aktiviert“ + „ungültig“, Bedarfsmeldungen „Deaktiviert“ + „ungültig“. Nachher: `true` bzw. `false` (wirksamer Zustand), Menü unverändert, kein „ungültig“. Negativ: `QrErstesFeld = 'matchcode'` bleibt nach dem Speichern stehen, bis „Matchcode“ gewählt wird. |
| 111.4 | P-4, dann `SQL/108` zweimal ausführen; `/Settings` öffnen; frische DB mit `SQL/00_FreshInstall.sql`. | 1. Lauf löscht die vorhandenen Zeilen (PRINT mit Anzahl), 2. Lauf „nichts zu tun“, kein Fehler. Gruppe „Teileverfolgung und OSEON“ ohne beide Schalter, nichts unter „Nicht im Katalog“. FreshInstall-DB ohne beide Zeilen. |
| 111.5 | Stammdaten › Lagerplätze und BDE-Stammdaten › Operatoren; Spaltenfilter „Übergabe an Sage“ = `nicht nötig` bzw. `Offen`; „Offene Übergaben anzeigen“. | Bestand zeigt „nicht nötig“ als grauen Text (kein Badge), neu angelegte Plätze/Operatoren „Offen“. Filter liefert genau die passenden Zeilen über alle Seiten. Negativ: „Offene Übergaben anzeigen“ listet keine „nicht nötig“-Zeile; Hinweis-Zähler unverändert. |
| 111.6 | `dotnet test IDEALAKEWMSService.Tests --filter "FullyQualifiedName~Workers"` 20× hintereinander | Alle Läufe grün (Nachweis im QA-Abschnitt). |

## TS-112 — Artikelinfo: Lieferanten mit Kreditor, Hinweise, Verlinkung (v1.80.0)

Spec `2026-11-08-artikeldetails-lieferanten-anzeigen-spec`. **Standort:** beide (Daten nur an AKE; IDEAL = Hinweis-Fälle). **Keine Migration.**
**Vorbedingungen:** AKE-DEV mit `Sync:Quelle:Artikel = SData`, Kreditoren- und Artikel-Lieferanten-Lauf gelaufen. Testartikel 00722001
(Hauptlieferant `K31013`). P1 (2026-11-08, AKE-DEV): 16.496 Hauptlieferanten und 22.677 letzte Lieferanten, je 0 ohne Kreditor → Kontoformat
gleich. P2: höchstens 8 Lieferanten je Artikel. Benutzer mit `masterdata_read` ohne `masterdata` und einer mit `masterdata`.
Automatisiert: `ArtikelinfoLieferantenTests`.

| Nr | Schritte | Erwartung |
|---|---|---|
| 112.1 | `/Articles`, Artikelnummer 00722001 anklicken | Artikelinfo von 00722001 öffnet sich. |
| 112.2 | Karte „Lieferanten“ | Hauptlieferant `<Name> (K31013)` als Link, letzter Lieferant ebenso (oder nur Konto ohne Kreditor); Tabelle mit der Zeile K31013 und „Ja“ in Hauptlieferant ganz oben. |
| 112.3 | Lieferanten-Link anklicken | Kreditor-Details mit Sage-Konto K31013. |
| 112.4 | Als Benutzer mit `masterdata`: `/Articles/Edit/<id>` → Knopf „Artikelinfo“ | Artikelinfo desselben Artikels. |
| 112.5 | Gruppe „Bestand und Dispo (Sage)“ | Keine Zeilen „Hauptlieferant“/„Letzter Lieferant“ mehr. |
| 112.6 | Negativ: Artikel ohne Satellit (`SELECT TOP 1 a.ArticleNumber FROM Articles a WHERE NOT EXISTS (SELECT 1 FROM ArticleSageDetails d WHERE d.ArticleId = a.Id)`) | Karte „Sage-Zusatzfelder“ mit Hinweis auf `Sync:Quelle:Artikel`; Haupt- und letzter Lieferant „—“. |
| 112.7 | Negativ: Artikel ohne `ArticleSuppliers`-Zeile | Hinweis mit `Sync:ArtikelLieferantEnabled` statt Tabelle. |
| 112.8 | Negativ (nur Testsystem): `UPDATE ArticleSageDetails SET LetzterLieferant = 'K00000' WHERE ArticleId = <id>`, danach zurück | Nur „K00000“, kein Link, kein Fehler. |
| 112.9 | Negativ (nur Testsystem): Kreditor `IsActive = 0`, danach zurück | Link mit „– inaktiv“ (Text). |
| 112.10 | IDEAL-DEV (`localhost:8082`), beliebiger Artikel | Hinweise aus 112.6/112.7, Seite lädt ohne Fehler. |
| 112.11 | Spaltenfilter Artikelnummer und CSV-Export der Artikelliste | Wie vorher (Text, kein HTML). |
| 112.12 | v1.85.0: Als Benutzer mit `masterdata` `/Articles/Edit/<id>` von 00722001 | Unter dem Formular die Karten „Sage-Zusatzfelder“ und „Lieferanten“ (nur lesend, gleiche Inhalte wie Artikelinfo); auch nach einem Speicherfehler (Pflichtfeld leeren, speichern) weiter sichtbar. |
| 112.13 | v1.85.0: Nach einem Artikellauf mit Quelle SData und einem Artikel-Lieferanten-Lauf Artikel EK012566 bzw. EK023609 öffnen | Gruppe „Mengeneinheiten“: EK012566 Basis RLL, Lager Stk; EK023609 Verkaufs-/Verpackungseinheit PKG; Lieferanten-Tabelle „Bestelleinheit“ PKG mit „(1 PKG = … Stk)“, wenn die Bestelleinheit von der Basis abweicht. Vor dem Lauf: „—“. |
| 112.14 | v1.85.0: Kreditor-Details von K31013 | Karte „Artikel dieses Lieferanten“ mit Zeilen, Spalten Hauptlieferant/Letzter Lieferant „Ja“ wo zutreffend, Artikelnummer verlinkt die Artikelinfo; Spaltenfilter (Server, vor dem Blättern) und CSV wirken; K30601 (~9.000 Artikel) lädt seitenweise mit Pagination, Kopf zeigt die Gesamtzahl; ein Filter wandert beim Wechsel auf einen anderen Kreditor nicht mit (steht nur in der URL). Kreditor ohne Artikel: Hinweis mit `Sync:ArtikelLieferantEnabled`. |
| 112.15 | v1.85.0: Artikelliste → Zahnrad: „Hauptlieferant“ und „Letzter Lieferant“ einblenden, Spaltenfilter „Huber“, Seite wechseln, neu laden | Spalten standardmäßig aus, nach Einblenden „Name (Konto)“ bzw. nur Konto; Filter über alle Seiten; Spaltenwahl bleibt nach Reload erhalten. |
| 112.16 | v1.85.0: Artikelinfo-Kopf der Karte „Sage-Zusatzfelder“ | Text „letzte Änderung durch den Artikel-Abgleich: …“ in Ortszeit; ändert sich nicht durch einen Lauf ohne Wertänderung. |

## TS-113 — Karte „Meine Fehlteile“ je Bestelltyp, Werkbank-Auswahl „Nur meine Werkbänke“ (v1.81.0)

Spec `2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-spec`, Bug `2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-bug`.
**Standort:** beide. **Keine Migration.** Ergänzt 33.12/33.13. Seitengröße nur aus {25, 50, 100, Alle}.
**Vorbedingungen:** `LagerbestellungAktiv = true`. User U mit Lager- **und** Glas-Bestellrecht, zugeordnet zu Werkbank A und B; Werkbank C fremd.
Fehlteile `WillBeRestocked`: 2 Lager auf A, 3 Glas auf B (in 2 Bestellungen), 4 Lager auf C.
Automatisiert: `FehlteileKarteV181Tests` (Zählung je Typ, Controller-Typ, Werkbank-Auswahl, fremde Werkbank, View-Wächter).

| Nr | Schritte | Erwartung |
|---|---|---|
| 113.1 | Als U `/WarehouseRequisitions`, Reiter Lager | Karte „2 Fehlteile (wird nachgeliefert) aus 1 Bestellungen“. |
| 113.2 | Link anklicken | `/MissingParts`, Typ-Reiter Lager aktiv, Trefferzahl 2 = Zahl der Karte; URL enthält `type=Lager`. |
| 113.3 | Zurück, Reiter Glas | Karte „3 … aus 2 Bestellungen“; der Link öffnet Typ-Reiter Glas mit 3 Treffern. |
| 113.4 | `/MissingParts` (Lager, Haken „Nur meine Werkbänke“ an), Werkbank-Dropdown öffnen | Bedienbar; nur „— Alle —“, A und B; C fehlt. |
| 113.5 | Werkbank A wählen | Nur Zeilen von A, Trefferzahl und beide Badges passend; Haken bleibt an. |
| 113.6 | Haken abwählen (Klick, nicht URL) | Seite lädt mit `mineOnly=false`, Haken aus, Titel „Fehlteile“; Dropdown listet alle Werkbänke inkl. C. |
| 113.7 | Haken wieder anwählen | Zurück auf die eigenen Werkbänke. |
| 113.8 | Negativ: URL `/MissingParts?mineOnly=true&workplaceId=<Id von C>` | Hinweis „Die gewählte Werkbank ist dir nicht zugeordnet – die Liste ist deshalb leer.“; Liste leer, Badges 0, Dropdown auf „— Alle —“. |
| 113.9 | Negativ: User nur mit Glas-Recht | Karte und Link nur Glas; kein Weg in die Lager-Liste. |
| 113.10 | Negativ: User ohne Werkbank-Zuordnung | Karte auf `/WarehouseRequisitions` fehlt (Zähler 0); `/MissingParts` zeigt den Banner wie 33.13. |
| 113.11 | `/MissingPartsLager` | Unverändert wie 33.14. |

## TS-116 — Wareneingang zu Sage-Bestellung (v1.84.0)

Spec `2026-11-08-wareneingang-aus-sage-bestellungen-spec` (Epic E1–E4; E5 Folge-Spec). **Standort:** AKE (IDEAL ohne SData-Endpunkt).
**Migration:** `SQL/112_AddSagePurchaseOrderLines.sql`, `SQL/113_AddPurchaseOrderReceipts.sql`. Web + Dienst.
**Vorbedingungen:** AKE-Test-SData erreichbar; `Sync:KreditorenEnabled` und Lagerplatz-Abgleich gelaufen; Benutzer S mit `stock`, Benutzer R nur
`stock_read`. E0-Befund (Spec): nur `EBB` (1.059 offene Positionen am AKE-Test), Mengen in der Basiseinheit, Kopf aus den Positionen,
`Liefersperre` = Lieferant gesperrt. Automatisiert: `EinkaufBestellungenSyncTests` (Dienst), `PurchaseOrdersControllerTests`, `PurchaseOrdersReceiveTests`.

| Nr | Schritte | Erwartung |
|---|---|---|
| 116.1 | Schalter `Sync:EinkaufBestellungenEnabled` aus, einen Zyklus abwarten; `WareneingangBestellungAktiv = true`, Lager → Offene Bestellungen | Kein Lauf „Offene Bestellungen (Sage)“; Seite zeigt den Hinweis mit `Sync:EinkaufBestellungenEnabled` statt einer leeren Tabelle. |
| 116.2 | Schalter an, Zyklus abwarten; Aktivitäts-Protokoll; WMS-DB `SELECT COUNT(*) FROM SagePurchaseOrderLines` | Lauf mit Counts `positionen`, `koepfe`, `uebernommen`, `artikel_fehlt`, `kreditor_fehlt`, `lagerplatz_fehlt`, `einheit_abweichend`; Zeilen ≈ offene EBB-Positionen (P-W4). Eine bekannte Bestellung in der Vorschau gegen Sage vergleichen (Positionen, offen, Ziel-Platz). Negativ: SData-Kennwort falsch → Lauf „Fehler“ + Fehlermail, Cache unverändert. P-W6: `SELECT COUNT(*) FROM SagePurchaseOrderLines WHERE SagePlatzId IS NOT NULL AND StorageLocationId IS NULL` = 0. |
| 116.3 | Vorschau ohne Filter mit Einstellung 2; dann Filterkarte „Wochen“ 4; `/Settings` Wochen 0, 27, „abc“ und 4 | Nur Liefertermin ≤ heute + 14 Tage, dazu überfällige („überfällig“) und ohne Termin („ohne Termin“); mit 4 mehr Zeilen. 0/27/„abc“ abgewiesen, 4 gespeichert und beim nächsten Aufruf vorbelegt. Filter Lieferant, Spaltenfilter und Seitenwechsel wirken zusammen. |
| 116.4 | Benutzer R: Vorschau öffnen, `/PurchaseOrders/Receive` direkt aufrufen; `WareneingangBestellungAktiv = false` | R sieht die Vorschau ohne Knopf „Wareneingang“, `Receive` abgewiesen. Schalter aus: Menüpunkte weg, `/PurchaseOrders` → 404. |
| 116.5 | Benutzer S: Bestellnummer per Hardware-Scanner und per Eingabe öffnen; eine Position mit Ist < Rest (z. B. „2,5“) einbuchen | Bestellung öffnet sich; Zeile zeigt „Teillieferung“; nach dem Buchen Meldung „1 Position(en) eingebucht (davon 1 Teil-, 0 Überlieferung)“, Bewegungshistorie: Einbuchung 2,5; „im WMS erfasst“ in Vorschau und Maske = 2,5, Rest entsprechend kleiner. Ziel-Platz mit Sage-Buchung + `SageLagerbuchungAktiv`: Eintrag in Sage-Lagerbuchungen. |
| 116.6 | Negativ: Ist > Rest ohne Häkchen; mit Häkchen; Menge 0 neben einer gültigen Zeile; Position „Artikel nicht im WMS“ bzw. „Einheit abweichend“; unbekannte Bestellnummer | Ohne Häkchen: „Überlieferung … bitte bestätigen“, nichts gebucht; mit Häkchen gebucht („Überlieferung“). Menge 0: nichts gebucht, Eingaben bleiben stehen. Nicht buchbare Zeilen ohne Eingabefeld mit Begründung. Unbekannt: „Bestellung „…“ nicht gefunden“. |
| 116.7 | Lieferant in Sage gesperrt (Testsystem) | Warnbanner „Lieferant gesperrt“ in der Maske, Hinweis-Spalte „Lieferant gesperrt“; Buchen möglich. |

---

*Ende des Dokuments. Stand: IDEAL-Epic Teil 1–5 + FA-Struktur-Darstellung Etappe 7 + Teil 7 Etappe B/C/D + Teil 8 (Sub-FA-BDE, v1.33.0) + Teil 6 (Standorteinstellungen, v1.34.0) + BOM-Guard hierarchisch (2026-08-18, abgelöst) + FA-Liste-Hierarchie (v1.35.0) + BOM-Bridge (v1.36.0) + Materialisierung fachliche Felder K1/K2 (v1.37.0) + Skopierung der Tabellen-Selektoren (TS-72) + FA-Liste ausgebaut: Matchcode, Kopfdaten je Zeile, HauptFA-Zeile, Freigabe-Kaskade (v1.38.0, TS-73) + PDF-Erzeugung für FaHierarchy-Druckdokumente (v1.39.0, TS-74) + Matchcode im Artikelstamm (v1.40.0, TS-75) + FA-Arbeitsgang-Erkennung aus der Struktur (v1.41.0, TS-76, ersetzt durch TS-77) + Arbeitsgänge (WorkOperation) aus der Struktur + Werkbank-Anlage aus Sage (v1.44.0, TS-77) + Arbeitsgänge je (Sub-)FA im FA-Struktur-Modal (v1.45.0, TS-78) + Kommissionierung nur am HauptFA
(v1.46.0, TS-79) + Kommissionierliste Summiert als Standard, Druck/PDF nur sichtbare Spalten (v1.47.0, TS-80) + Beschichtungsauftrag Summiert als Standard (v1.48.0, TS-81) + Briefkopf Beschichtungsauftrag (v1.49.0, TS-82) + Beschichtungsauftrag als eigenes Objekt (v1.50.0, TS-83) + Beschichtungsaufträge bedienen (v1.51.0, TS-84) + Arbeitsgänge als Symbole im FA-Baum (v1.52.0, TS-85) + CSV-Export für alle Tabellen (v1.53.0, TS-86) + Arbeitsgang-Stammdaten (v1.54.0, TS-87) + Status je Arbeitsgang-Kürzel im FA-Baum (v1.55.0, TS-88) + Menü Beschichtung + Kurzanleitungen (v1.56.0, TS-89) + BDE-Terminal mit Arbeitsgang-Status (v1.57.0, TS-90) + Spaltenfilter-Suchsyntax Etappe A + B (v1.58.0/v1.58.1, TS-91.A/B) + Fußzeile nach Word-Vorlage (v1.59.0, TS-92) + Sage-Quellen auf WMS-Views (v1.60.0–v1.60.2, TS-93) + Arbeitsgang-Abgleich aus der Sage-View (v1.61.0, TS-94) + Stückliste Matchcode und Lagerplatz-Limit (v1.62.0, TS-95) + BDE Stufe 1: Werkbänke, AG-Fertigmeldung (v1.63.0, TS-96) + BDE-Terminal Liste nach Haupt-FA, Scan ohne AG (v1.64.0, TS-97) + Spaltenauswahl/Zahnrad zentraler Toolbar-Host (v1.65.0, TS-98) + Hardware-Barcodescanner (v1.66.0–v1.66.2, TS-99) + Vault- und enaio-Links konfigurierbar (v1.67.0, TS-100) + QR erstes Feld Matchcode / Trennzeichen (v1.68.0/v1.69.0, TS-101) + Aktivitäts-Protokoll Spaltenfilter/Namen (v1.70.0, TS-102) + FA-Struktur live aus Sage (v1.71.0, TS-103) + Lagerplatz-Etikett 93 × 55 mm und Vormontage-Felder (v1.72.0, TS-104/TS-105) + Sage-Abgleich über SData (v1.73.0, TS-106) + Fehlteile „Nur meine Werkbänke“ Trefferzahl (v1.75.0, TS-108) + Bestandsübersicht QR-FA nur mit `QrMitFaNummer` (v1.76.0, TS-101.22–101.25) + Einstellungen Auswahlfelder und Beschreibung (v1.77.0, TS-109) + EAN-Scan Einkaufsartikel (v1.78.0, TS-110) + Bereinigungen nach dem Bündel 2026-11-07 (v1.79.0, TS-111) + Artikelinfo Lieferanten (v1.80.0, TS-112) + Fehlteile-Karte je Bestelltyp (v1.81.0, TS-113) + Wareneingang zu Sage-Bestellung (v1.84.0, TS-116) + Artikel Lieferanten/Mengeneinheiten (v1.85.0, TS-112.12–112.16)*
*Bei neuen Features: Szenarien in den entsprechenden Bereich einfuegen und TS-Nummern fortfuehren.*

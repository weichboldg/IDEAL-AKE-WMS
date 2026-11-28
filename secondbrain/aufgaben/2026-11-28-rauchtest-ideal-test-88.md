---
type: aufgabe
status: offen
created: 2026-11-28
quelle: "Gerald im Chat 2026-11-28: QA am IDEAL-Testsystem :88 (Rauchtest installierte Version, Testbuchungen erlaubt)"
---
# Rauchtest IDEAL-Test :88 (v1.99.89)

**Umfang tatsächlich:** nur Datenbank-Sicht (`IDEAL_AKE_WMS_TEST` auf `idealsql2\SAGE100TEST`, lesend). Die Oberfläche ist
**nicht** geprüft: Anmeldung mit Kennwort am Host `idealweb01…:88` darf der Agent nicht ausführen (Hostname ohne Test-Kennzeichen,
gleicher Host wie Prod :80), Gerald hatte keinen PC. Weg für später: DNS-Alias mit Test im Namen (z. B. `wms-test.ideal.ideal-ake.at`)
auf die :88-Site binden, dann Agent-Anmeldung mit Testbenutzer möglich.

## Stand
- Installiert: v1.99.89 (Anmeldeseite), letzte Migration `20261125191311_AddBdeTeilMeldungIstAbschliessendUndStandardsicht`.
- Update-Lauf 1.99.89 am 2026-11-27 11:14: `FraesenPfad_Erstimport` scharf, 169 Treffer.

## Befunde (Aktivitäts-Protokoll, letzte 24–48 h)
1. **ProductionOrder-Sync schlägt jeden Lauf fehl** (~186 Fehler/Tag, ab 2026-08-11): `Ungültiger Objektname "dbo.vw_AKE_Kommissionierung_WAListe"`.
   `Sync:ProductionOrdersEnabled = true` am :88, laut [[2026-08-07-ideal-teile-1-5]] am IDEAL auf `false` zu setzen (AKE-Import,
   View gibt es am IDEAL nicht; `SageImportService.cs:91` hartcodiert). → Einstellung am :88 (und IDEAL-Prod prüfen) auf `false`.
2. **ArtikelLieferant-Sync:** SData-Resource `ArtikelLieferantInfo` am IDEAL-Sage nicht vorhanden (HTTP 404), `Sync:ArtikelLieferantEnabled = true`.
   Meldung empfiehlt selbst: Abgleich ausschalten (oder Resource in Sage freischalten).
3. **9 BDE-aktive Werkbänke fehlen seit 2026-10-19 in Sage** (`SageMissingSince`): 2000 AV, 2200 SW, 2500 PG, 2550 PL, 3996 ZUI,
   4000 KT, 4101 TK, 6000 TP, 6100 TP2. Klären: in Sage umbenannt/umnummeriert oder BDE dort abschalten.
4. **Unbekannte Arbeitsschritt-Kürzel** ohne Werkbank: MK (15 FA), ISO (3), MO (3).
5. **Lagerbestand-Warnungen 386 640/Tag** („Artikel … nicht im WMS, übersprungen“, ~17 MB Text/Tag); `SyncLogs` hat 26,4 Mio. Zeilen.
   Kandidat für Sammelmeldung statt Einzelzeile.
6. Hauptlagerplätze aus Sage ohne WMS-Lagerplatz (ALT-BEST, EM-TK, EM-VIT, VM-ELEKTRO, …) — Lagerplatz-Abgleich/Stammdaten.
7. Userfelder am IDEAL-Sage unbekannt: `USER_KundenNameFA` (Debitoren), `KHKEKBelege_USER_AB`/`_USER_Kom` (Einkauf) → Einstellungen
   `Sync:Userfeld:*` für IDEAL leeren bzw. richtigen Feldnamen eintragen.
8. Einzelfälle: SData `EinkaufPositionenAllInfo` Zeitüberschreitung (LetzteBestellung), Lagerbestand-Verbindungsabbruch 27.11. 16:44.

## Offen
- [ ] Gerald: Einstellungen 1, 2, 7 am :88 (und Prod) entscheiden — Agent ändert keine Einstellungen am :88.
- [ ] Gerald/Fachbereich: Befund 3 + 4.
- [ ] Befund 5 als Backlog (Sammelmeldung Lagerbestand) — auf Zuruf.
- [ ] Oberflächen-Rauchtest nachholen, sobald angemeldet werden kann.

## AKE-Test :4444 (v1.99.89), Oberfläche — 2026-11-28 ~12:05
Anmeldung über „Mit Windows anmelden“ (Gerald, ohne Kennwort-Eingabe). Nur angesehen, nichts gebucht (Buchungen gingen über die
Sage-Warteschlange an Sage — bewusst ausgelassen).
- **Alle 59 Menü-Masken** laden mit HTTP 200, kein Fehlertext, kein rotes Banner. Stichprobe FA-Liste, Artikel, Leitstand: keine JS-Fehler in der Konsole.
- Aktivitäts-Protokoll: letzte Fehler 26.11. 11:51 („SData-Kennwort nicht entschlüsselbar“, je 79× Offene Bestellungen, Artikel-Lieferanten,
  Debitoren) — seitdem keine Fehler mehr, Dienste laufen (letzter Eintrag 28.11. 12:05).
- Warnungen (Hinweise, kein Fehler):
  - SageBookingQueue: TLS-Zertifikatsprüfung aus (nur Test zulässig; vor Prod einschalten); CSV-Übergabe an, aber kein Pfad gesetzt.
  - Kommissionier-Leitstand: kein Benutzer als Kommissionierer gekennzeichnet → keine Freigabe möglich.
  - Benutzer: 7 aktive lokale Benutzer ohne Kennwort.
  - BDE/OSEON-Übersicht: 11 713 AG ohne Arbeitsgang-Config.
  - Bestellvorschläge: 30 Artikel ohne Hauptlieferant, 16 Bestellpositionen mit nicht umrechenbarer Einheit, 19 ohne Menge.
  - Bedarfsmeldungs-Mail: keine aktiven Empfänger; Artikel-Sync: Hauptlagerplätze GL-AKV, FTG nicht als WMS-Lagerplatz.
  - Sage-Buchungsqueue: 6 alte Fehler vom 03.08. (Sage `GetSchema failed`/MissingMethodException) stehen noch in der Liste.
- Auffällig klein: Seite `/PickingLeitstand` trägt die Überschrift „Fertigungsaufträge“ (wie die FA-Liste) — prüfen, ob gewollt.

## Als Jira-Bugs gemeldet (2026-11-28)
- WMS-11 Lagerbestand-Abgleich: eine Warnung je übersprungenem Artikel (Befund 5) — Code.
- WMS-12 Leitstand Kommissionierung mit Überschrift „Fertigungsaufträge“ — Code.
- WMS-13 IDEAL-Test: ProductionOrders/ArtikelLieferant/Userfelder scheitern je Lauf (Befunde 1, 2, 7) — Konfiguration.
- WMS-14 IDEAL: 9 BDE-Werkbänke fehlen in Sage, Kürzel MK/ISO/MO (Befunde 3, 4) — Fachbereich.
Nicht gemeldet (Einstellungen/Stammdaten, kein Fehler): AKE-Hinweise (TLS, CSV-Pfad, Kommissionierer, Benutzer ohne Kennwort, fehlende Hauptlagerplätze).
Standort-Stichwort gesetzt: WMS-11/12 `standort-beide`, WMS-13/14 `standort-ideal` (WMS-10 `standort-ideal`).

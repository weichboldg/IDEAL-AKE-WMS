---
typ: feature
---
# Scan-Handler für Hardware-Barcodescanner (Tastatur-Eingabe mit Präfix)

**Aufgenommen 2026-10-15 (Gerald), ausformuliert von der Koordination für die Spec.** Standort: AKE und IDEAL.

## Anforderung (vom Menschen)

- Mobile Geräte mit **integriertem Barcodescanner** (z. B. Android-Handy) sollen EAN-, QR- und andere Codes über den
  **Scanner** statt über die Kamera erfassen.
- Gewünscht ist ein **Scan-Handler, der auf „Tastatureingabe" hört**: Der Scanner sendet den Code wie eine Tastatur.
- Beginnt die Eingabe mit einem bestimmten **Startstring** (Beispiel `~09`), wird die Barcode-Erkennung aktiviert.
  Präfix-Vorschläge sind ausdrücklich erwünscht.
- Ob der Scan-Handler aktiv ist: **Einstellung in den Settings**, eventuell **pro Benutzer** aktiv/inaktiv.

## Ist-Stand (Koordination, Bündel `d4108f1a`, nur gelesen)

- Kamera-Scan existiert: `IdealAkeWms/wwwroot/js/barcode-scanner.js` (html5-qrcode, Kamera + Bild-Upload).
  `initScanner(buttonId, targetSelectId, scanType)` mit `scanType` `article` / `storageLocation` / `productionOrder`
  (+ QR-FA-Option), Auswertung zentral in `processScannedValue(...)` (Z. 269 ff.); dazu `initTextInputScanner(...)` (Z. 391).
- Eingebunden u. a. in StockMovements (Inbound, InboundBulk, Index, LocationTransfer, Outbound, OutboundAll, Transfer),
  Picking/Bom, PickingLeitstand, ProductionOrders, Articles/Info, StockOverview, Home (Artikelinfo-Kachel).
- BDE-Terminal hat eigene Scan-Felder mit Enter-Abschluss (`wwwroot/js/bde-terminal.js:88` Personalnummer, `:153` FA/AG)
  — dort funktioniert ein Tastatur-Scanner heute schon „von selbst", wenn das Feld den Fokus hat.
- Barcode-**Erzeugung** (Code 128) für Drucke: `Services/BarcodeService.cs` (HauptFA, Beschichtungsauftrag).

## Ausformulierung für die Spec (Vorschlag Koordination)

**Ziel:** Ein globaler, seitenweiter Tastatur-Listener erkennt Scanner-Eingaben am Präfix, fängt sie ab (sie landen
nicht als Text im gerade fokussierten Feld) und leitet den Code an **dieselbe Auswertung wie der Kamera-Scan** weiter
(`processScannedValue` bzw. die Scan-Ziele der Seite). Ein Scanner ersetzt damit den Kamera-Knopf, ohne dass jede Seite
neu gebaut wird.

**Erkennung (Vorschlag, Rückfrage):**
1. **Präfix** konfigurierbar. Vorschläge:
   - **A (Empfehlung): AIM-Symbologie-Kennung** — der Scanner sendet vor jedem Code `]` + Buchstabe + Ziffer
     (`]E0` EAN-13, `]C1` Code 128, `]Q1` QR, `]d2` DataMatrix). Standard bei Zebra/Honeywell/Datalogic u. a., auf dem
     Gerät nur einzuschalten; liefert zusätzlich die Code-Art (EAN vs. QR) für das Routing. `]` tippt im WMS praktisch niemand.
   - **B: eigener Präfix**, z. B. `~~` oder Geralds `~09`. Einfach, aber auf jedem Gerät einzeln zu konfigurieren; `~`/`^`
     können je nach Tastaturlayout als Tottaste ankommen (vorher am Gerät testen).
   - Nicht empfohlen: Steuerzeichen (STX/ETX) — kommen im Browser auf Android nicht verlässlich als Tasten-Events an.
2. **Ende** des Scans: Suffix **Enter** (am Scanner einstellen) **oder** Zeit-Grenze (kein Zeichen > z. B. 80 ms).
3. **Schutz gegen Fehlerkennung:** Präfix nur gültig, wenn die Folgezeichen schnell kommen (Scanner < ~30 ms je Zeichen,
   Mensch deutlich langsamer); angefangener Präfix ohne Folgezeichen → Zeichen normal durchreichen.

**Wirkung je Seite (Vorschlag):**
- Seiten mit Scan-Zielen (`initScanner`): gescannter Code geht an das passende Ziel nach Code-Art/Inhalt
  (Artikel / Lagerplatz / FA) — gleiche Rückmeldung wie Kamera-Scan (`showScanFeedback`).
- Fokus in einem Eingabefeld ohne Scan-Ziel: Code wird ins Feld geschrieben (ohne Präfix).
- BDE-Terminal: bestehende Scan-Felder unverändert lassen (Handler greift dort nicht ein bzw. füllt das aktive Scan-Feld).
- Kein passendes Ziel: sichtbarer Hinweis „Code … gescannt – auf dieser Seite nicht verwendbar".
- **Später (nicht Teil 1):** globale Aktionen von jeder Seite aus (FA scannen → FA öffnen, Lagerplatz → Bestand).

**Einstellungen (Vorschlag):**
- Standort-Einstellung `Scan:HardwareScannerAktiv` (Default aus), `Scan:Praefix` (Default nach Rückfrage),
  `Scan:Suffix` (Enter), `Scan:MaxZeichenabstandMs`.
- **Pro Benutzer** an/aus (Mein Profil / Benutzerpflege), wie gewünscht.
- **Zusätzlich empfohlen: pro Gerät** an/aus (im Browser gespeichert) — der Scanner hängt am Gerät, nicht am Benutzer;
  derselbe Benutzer arbeitet am PC ohne Scanner.

## Offen / Rückfragen an Gerald

1. Welche Geräte/Scanner-Software (Hersteller, z. B. Zebra DataWedge, Honeywell, Urovo)? Bestimmt, ob AIM-Kennung
   und Enter-Suffix einstellbar sind.
2. Präfix A (AIM) oder B (eigener, welcher)?
3. Welche Codes kommen vor: EAN der Artikel (gibt es eine EAN im Artikelstamm/Sage? im WMS bisher nicht gesehen —
   Spec prüfen), Artikelnummer, Lagerplatz, FA-Barcode (Code 128 aus `BarcodeService`), QR-FA?
4. Pro Benutzer **und** pro Gerät, oder nur eines davon?
5. Welche Seiten zuerst (Lagerbewegungen, Stückliste/Kommissionierung, BDE)?

---
type: changelog
version: 1.67.2
date: 2026-10-23
---
# v1.67.2 — Einbuchung: Scannen ohne Feld (Pilot)

Spec [[2026-10-15-scan-handler-hardware-scanner-spec]], Freigabe-Nachtrag 5. Basis `a2651019` (v1.67.1), Commit `20b573de`.
**Keine Migration**, nur Web. TS-99.19.

## Kernpunkte

- Der Netum („Fokus-Eingabe“) gibt ohne Feld mit Fokus keinen Text aus. Auf `StockMovements/Inbound` bekommt darum ein unsichtbares Feld
  `#hwScanOhneFeld` (`inputmode="none"`, `aria-hidden`, außerhalb des Formulars) den Fokus, sobald nichts anderes ihn hat.
- Nur mit Geräte-Schalter „Scannen ohne Feld“ (Mein Profil, localStorage `wms.hardwareScanner.ohneFeld`, Standard aus).
- Zuordnung nach Inhalt (`HwScanCore.zuordnung`): mit `;` Artikel-QR, sonst genauer Lagerplatz-Code, sonst Artikelnummer; kein Treffer →
  „Code … nicht zuordenbar“. Nie Submit. Kamera-Fenster und angetipptes Feld haben Vorrang.
- `processScannedValue` mit optionalem Rückruf `nichtGefunden` (andere Aufrufer unverändert).

## Deploy-Risiken

- Nur Web; wirkt nur bei `HardwareScannerAktiv = true` **und** Profil-Schalter an.
- Grenzen: nach Auswahl in der nativen Lagerplatz-Liste erst auf eine freie Stelle tippen; Tastatur-Bedienung am PC mit Schalter an gestört.

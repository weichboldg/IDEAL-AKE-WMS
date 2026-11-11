---
type: changelog
version: 1.66.1
date: 2026-10-21
---
# v1.66.1 — Hardware-Scanner: Stückliste am Handheld, Zeichenabstand geprüft

Nachbesserung zu [[2026-10-21-v1-66-0-hardware-barcodescanner]] laut Freigabe-Nachtrag 2 der Spec
[[2026-10-15-scan-handler-hardware-scanner-spec]] (QA-Befunde N-1/N-2). Basis `789d0a78`, Commit `c72be07e`. **Keine Migration**.

## Kernpunkte

- Stückliste: Ist das Kamera-Fenster von „Artikel scannen“ offen, wird ein Hardware-Scan wie ein Kamera-Treffer verarbeitet
  (sperren, `processScannedValue`, Fenster schließen). Freigeschaltet per `data-hw-scan-kamera-ziel` am Knopf, nur `btnScanBomArticle`.
  Andere Seiten sind geprüft und nicht ausgeweitet, die Kandidaten stehen in der Spec.
- `HardwareScannerMaxZeichenabstandMs` nur 10–500 (Meldung beim Speichern), leer speichern = Standard 50, das nie gespeicherte Feld
  blockiert nicht. Der Client nimmt bei ungültigem Wert 50.

- QA-Fix `1d78e7f5`: Der Kamera-Treffer-Callback (`startCameraScanner`) ignoriert Treffer eines bereits gestoppten Scanners
  (`_activeScanner !== html5QrCode`). html5-qrcode ruft den Callback auch für einen Decode, der vor `stop()` begann; ohne den Wächter
  konnte er nach einem Hardware-Scan (oder „Abbrechen“) noch einmal auswerten.

## Deploy-Risiken

Nur Web (Dienst nur wegen `AppVersion.cs`). Ein schon gespeicherter Wert außerhalb von 10–500 wirkt im Client als 50 und lässt sich in der
Maske erst nach Korrektur wieder speichern (Meldung nennt den Wert).

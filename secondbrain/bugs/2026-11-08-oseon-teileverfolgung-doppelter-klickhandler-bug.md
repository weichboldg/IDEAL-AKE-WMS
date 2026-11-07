---
type: bug
title: OSEON-Teileverfolgung – doppelter Klickhandler am Artikel-QR-Knopf (ungeprüft zur Laufzeit)
status: offen
severity: niedrig
created: 2026-11-08
affected_code:
  - "IdealAkeWms/wwwroot/js/barcode-scanner.js:12 (initScanner), :671 (globaler Start)"
  - "IdealAkeWms/Views/…/OseonIndex.cshtml:26, :312"
  - "IdealAkeWms/Views/StockMovements/LocationTransfer.cshtml:128 (gleiches Muster)"
source_backlog: "[[2026-11-07-oseon-teileverfolgung-doppelter-klickhandler]]"
spec: "[[2026-11-08-oseon-teileverfolgung-doppelter-klickhandler-spec]]"
---

## Symptom

Vermutet (Codelesung, nicht zur Laufzeit bestätigt): Ein Klick auf den Artikel-QR-Knopf der OSEON-Teileverfolgung (nur AKE) öffnet das
Kamera-Fenster zweimal; welches bleibt, hängt vom Timing ab.

## Reproduktion

Pflicht-Schritt 1 der Spec: am AKE-DEV `localhost:8081` im Browser nachstellen (Konsole `getEventListeners`, Anzahl Fenster/Kamera-Abfragen).
Nur ein sichtbarer Fehler gilt als reproduziert.

## Root Cause

Der Knopf heißt `btnScanArticle` (`OseonIndex.cshtml:26`); der globale Start in `barcode-scanner.js:671` hängt dafür einen Handler für das
dort nicht vorhandene `ArticleId` an – zusätzlich zum seiteneigenen Handler (`OseonIndex.cshtml:312`). `initScanner` (`:12`) prüft nicht auf
Doppelregistrierung, anders als `initTextInputScanner` (`:418`).

## Fix / Verweis

[[2026-11-08-oseon-teileverfolgung-doppelter-klickhandler-spec]]

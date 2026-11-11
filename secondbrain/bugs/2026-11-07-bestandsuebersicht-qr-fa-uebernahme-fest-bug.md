---
type: bug
title: Bestandsübersicht übernimmt die FA aus dem Artikel-QR immer (QrMitFaNummer wirkt dort nicht)
status: offen
severity: niedrig
created: 2026-11-07
affected_code:
  - "IdealAkeWms/Views/StockOverview/Index.cshtml:17-18 (data-qr-fa-enabled hart \"true\")"
  - "IdealAkeWms/Controllers/StockOverviewController.cs:41-95 (liest QrMitFaNummer nicht)"
  - "IdealAkeWms/wwwroot/js/barcode-scanner.js:14, :311-313, :396-403"
source_backlog: "[[2026-10-27-bestandsuebersicht-qr-fa-uebernahme-fest]]"
spec: "[[2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-spec]]"
---

## Symptom

In der Bestandsübersicht (`/StockOverview`, „Artikelbestände“) landet beim Scan eines Artikel-QR mit FA im dritten Feld
(z. B. `102044;;1065141;09`) die FA `1065141` im FA-Filter, auch wenn `QrMitFaNummer` = `false` ist. Nach „Filtern“
zeigt die Seite dann den Bestand aller Artikel dieses FA statt des gescannten Artikels, weil ein FA-Filter alle anderen
Filter übersteuert (`StockOverviewController.cs:48-54`, so auch in der Hilfe `Help/Index.cshtml:1781`).
Auf Ein-, Aus- und Umbuchung hält sich dieselbe Funktion an die Einstellung.

## Reproduktion

1. `/Settings` → Allgemein: `QrMitFaNummer` aus.
2. Artikelbestände öffnen, Artikel-QR-Knopf (Kamera, Bild hochladen oder Hardware-Scanner), Etikett `102044;;1065141;09`.
3. Erwartet laut Einstellung: Artikelfilter `102044`, FA-Filter leer. Ist: FA-Filter `1065141`.
4. Gegenprobe Wareneingang: dort bleibt die FA leer.

## Root Cause

Der Scan-Knopf der Bestandsübersicht trägt das Attribut fest: `data-qr-fa-enabled="true"`
(`Views/StockOverview/Index.cshtml:18`). `initScanner` liest genau dieses Attribut einmal beim Laden
(`barcode-scanner.js:14`) und gibt es an alle Eingabewege weiter (Kamera `:18`, Hardware-Scanner `:20`/`:559`,
Kamera-Fenster `:544`). Ein-/Aus-/Umbuchung setzen das Attribut aus `ViewBag.QrMitFaNummer`
(`Inbound.cshtml:40`, `Outbound.cshtml:27`, `Transfer.cshtml:27`; ViewBag aus `StockMovementsController.cs:97/118/280/299/367/384`).
`StockOverviewController` liest die Einstellung nicht (`StockOverviewController.cs:41-95`).
Das ist alt und dokumentiert (`Help/Index.cshtml:120-121`, „Bestandsübersicht (immer)“), widerspricht aber der
allgemeinen Lesart der Einstellung.

## Fix / Verweis

Spec [[2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-spec]] (Entwurf, v1.76.0, Bündel
[[2026-11-07-buendel-uebersicht]]). Ob überhaupt geändert wird, entscheidet Schranke 1 (Variante A binden / B lassen).

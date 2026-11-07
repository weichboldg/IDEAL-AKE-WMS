---
typ: feature
---
# QR-Code: Matchcode statt Artikelnummer im ersten Feld (Einstellung)

**Aufgenommen 2026-10-23 (Gerald)** nach dem Netum-Test (TS-99.17): „Für die QR-Code-Erkennung eine zweite Logik einbauen:
QR mit Artikelnummer (bestehend), QR mit FA-Nummer (bestehend), QR mit Matchcode – hier soll der Matchcode anstelle der
Artikelnummer für Filter etc. herangezogen werden –, QR mit Matchcode und FA. Als Settings.“

## Befund (Koordination, Bündel v1.67.1, nur gelesen)

- QR-Format heute: `Artikelnummer;Feld2;FA-Nummer[,Suffix];…`. `processScannedValue` nimmt Feld 1 als Artikelnummer
  (`wwwroot/js/barcode-scanner.js` ab :284, Artikel-Zweig „Erster Teil vor ; ist die Artikelnummer“), Feld 3 als FA nur bei
  `qrFaEnabled` (Knopf-Attribut `data-qr-fa-enabled` aus `ViewBag.QrMitFaNummer`).
- `QrMitFaNummer` ist ein AppSetting (`Models/AppSettingKeys.cs:32`, Seed `Program.cs:460`, `/Settings` `Views/Settings/Index.cshtml:45`),
  ausgewertet in `StockMovementsController.cs` (8 Stellen) und den Views Inbound/Outbound/Transfer `:27`.
- Stückliste sucht die Zeile über `data-article-number` = Ressourcenummer (`Views/Picking/Bom.cshtml:197/291/329`, Lookup `:768`).
- Testetiketten (Hannes, 23.10.): `G-KT-FRR-510-2010--1760B-250-V01;;1065141;09` (Matchcode von 50002134) und
  `102044;;1065141;09` (Artikelnummer, funktioniert am Netum).
- Hardware-Scanner (v1.66/v1.67.1) und Kamera laufen beide über `processScannedValue` → eine Stelle für beide Wege.

## Vorschlag Koordination für die Spec

1. **Zwei unabhängige Einstellungen statt vier Kombinationen:**
   - neu `QrErstesFeld` = `Artikelnummer` (Standard, heutiges Verhalten) | `Matchcode`
   - bestehend `QrMitFaNummer` (Feld 3 = FA) bleibt
   → ergibt genau die vier gewünschten Fälle (Artikelnr., Artikelnr.+FA, Matchcode, Matchcode+FA).
2. Bei `Matchcode`: Feld 1 wird über den Artikelstamm in die Artikelnummer übersetzt (exakter Matchcode-Treffer, getrimmt, ohne
   Groß/Klein) und danach wie heute verwendet (Auswahlfelder, Filter, Stückliste, Artikelinfo, Bestandsübersicht …).
   **Mehrdeutig** (mehrere Artikel mit gleichem Matchcode) oder **kein Treffer** → klare Meldung, keine stille Wahl.
3. Offen für die Spec: Gilt die Einstellung je Standort (Standorteinstellungen) oder global (/Settings wie QrMitFaNummer)?
   Soll es zusätzlich „Automatisch“ geben (erst Artikelnummer, sonst Matchcode)?
4. Prüfabfrage für Gerald (WMS-DB IDEAL): Eindeutigkeit der Matchcodes
   `SELECT Matchcode, COUNT(*) FROM dbo.Articles WHERE Matchcode IS NOT NULL GROUP BY Matchcode HAVING COUNT(*) > 1;`

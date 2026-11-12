---
type: changelog
version: 1.99.11
date: 2026-11-12
---
# v1.99.11 — Wareneingang zu Bestellung: Barcode des Lieferscheins und CSV-Übergabe auf einen UNC-Pfad

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commits `895646a5` (Code, Migration, SQL/146, Tests, Manifest), `0dc0cea0` (TS-129, Hilfe,
Changelog, README, Version). Spec [[2026-11-12-wareneingang-barcode-lieferschein-csv-export-spec]]. Nicht gemergt, nicht gepusht, **vor QA**
(umgesetzt, Status setzt der qa-agent); wartet danach Schranke 2.

## Kernpunkte

- Feld „Barcode (Lieferschein)“ in „Wareneingang zu Bestellung“ neben der Lieferschein-Nr. (AppSetting `WareneingangBarcodeAktiv`, Default aus).
  Roh übernommen, nur `A–Z a–z 0–9 - _`, 1–50 Zeichen, keine reservierten Gerätenamen; Duplikat ohne Groß/Klein gesperrt; an jedem Receipt des
  Absendens im selben Insert, dazu `LieferantName` als Snapshot. Bei ausgeschaltetem Sage-Wareneingang gesperrt.
- Scan: `'raw'` in `barcode-scanner.js` wird jetzt roh ausgewertet (war `'article'`: Abschneiden am QR-Trennzeichen, EAN-/Matchcode-Auflösung).
- Ausgangstabelle `WareneingangCsvExporte` (eine Zeile je Sage-WE-Vorgang). Neuer Schritt im `SageBookingWorker` nach dem Sage-WE-Schritt:
  `<Barcode>.tmp` → `<Barcode>.csv` auf `WareneingangCsvPfad` (UNC), nie überschreiben, höchstens 10 Dateien je Takt, automatische Wiederholung
  nach Netzfehler (3×, ab 15 min), Kollision nur von Hand („CSV erneut schreiben“). Lauf „Wareneingang-CSV“ im Aktivitäts-Protokoll.
- Liste „Sage-Wareneingänge“: Spalten „Barcode“ und „CSV“, Hinweis „Sage-Wareneingang deaktiviert – CSV-Übergabe wartet“ (auch `/ServiceSettings`)
  und „kein Pfad gesetzt“.
- CSV: UTF-8 mit BOM, `;`, Kopfzeile, CRLF, `yyyy-MM-dd HH:mm`, RFC-4180-Quoting; Spalten Barcode, Kreditornummer, Kreditorname, Bestellnummer,
  Lieferschein, SageBelegnummer, Erfasser, Datum, UebergebenAm.

## Migration / SQL

Migration `20261112151318_AddWareneingangBarcodeCsv`, **SQL/146_AddWareneingangBarcodeCsv.sql** (additiv, nicht daten-destruktiv):
`PurchaseOrderReceipts.Barcode` (+ Index), `PurchaseOrderReceipts.LieferantName`, Tabelle `WareneingangCsvExporte`. FreshInstall Schema + MigrationId.
SQL/AgentJobs nicht betroffen.

## Einstellungen

AppSetting `WareneingangBarcodeAktiv`; ServiceSettings „Wareneingang-CSV“: `WareneingangCsvAktiv`, `WareneingangCsvPfad`,
`Sync:WareneingangCsvMaxWiederholungen` (3, 0–10), `Sync:WareneingangCsvWiederholAbstandMinuten` (15, 5–1440). Alle aus/leer; IDEAL bleibt aus.

## Deploy-Risiken

Web + Dienst + Schema (Reihenfolge: Dienst stoppen → SQL/146 → Web → Dienst). Vor dem Einschalten: Dienstkonto am AKE-Dienst-Server prüfen
(P-C1, `LocalSystem` = Computerkonto) und auf Freigabe **und** NTFS „Ändern“ setzen (P-C2) – sonst Fehler „Zugriff verweigert“ je Wareneingang.
Beim Einschalten von `WareneingangCsvAktiv` werden alle wartenden übergebenen Vorgänge mit Barcode sofort geschrieben. Ein hängender
SMB-Zugriff hält den Worker-Takt (und damit die Sage-Lagerbuchungen derselben Instanz) auf – höchstens 10 Dateien je Takt. Manifest: `SQL/146`,
`Pruefung/WareneingangCsv`, `v1.99.11-WareneingangCsv`, `v1.99.11-LagerBarcode`.

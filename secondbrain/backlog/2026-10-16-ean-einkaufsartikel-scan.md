---
typ: feature
---
# EAN für Einkaufsartikel: im Artikelstamm führen und scannen

**Aufgenommen 2026-10-16 (Gerald), ausformuliert von der Koordination.** Standort: AKE und IDEAL.
Abgespalten aus [[2026-10-15-scan-handler-hardware-scanner-spec]] (dort ausgeklammert, Antwort 4).

## Anforderung (vom Menschen)

- Bei **Einkaufsartikeln** soll die Hersteller-EAN (GTIN) auf der Verpackung gescannt werden können
  (z. B. Wareneingang, Lagerbewegungen) und den WMS-Artikel finden.
- Der Artikel-Sync aus Sage kann dafür erweitert werden.

## Ist-Stand (Koordination, Bündel v1.60.2, nur gelesen)

- Das WMS führt keine EAN (`Models/Article.cs:24-58`); ein EAN-Scan endet heute in „nicht gefunden“.
- Artikel-Sync: `SageImportService.SyncArticlesAsync`, Quelle per `Sync:ArticlesViewName`:
  leer = alter Select auf `KHKArtikel` (AKE), gesetzt = View `vw_IDEAL_AKE_WMS_Artikel` (IDEAL,
  Spalten laut sys.columns: ArticleNumber, Description, Matchcode, Unit, ArticleGroup, ReorderLevel,
  PrimaryStorageLocation). Für IDEAL müsste die **View** eine EAN-Spalte liefern (Änderung durch Gerald).
- Scan-Auswertung zentral in `barcode-scanner.js` `processScannedValue` (Z. 269 ff.), Artikel-Lookup über die
  Artikelnummer.

## Vorfragen (vor der Spec, Sage-DB, Gerald)

1. Wo führt Sage die EAN, und wie voll ist sie bei Einkaufsartikeln?
```sql
SELECT c.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME LIKE 'KHKArtikel%'
  AND (c.COLUMN_NAME LIKE '%EAN%' OR c.COLUMN_NAME LIKE '%GTIN%'
       OR c.COLUMN_NAME LIKE '%Barcode%' OR c.COLUMN_NAME LIKE '%Strichcode%');
-- je Treffer: SELECT COUNT(*) gesamt, COUNT(NULLIF(LTRIM(RTRIM(<Spalte>)),'')) befuellt FROM dbo.<Tabelle>;
```
2. Eine EAN je Artikel oder mehrere (Varianten, je Lieferant, Verpackungseinheit)? Davon hängt ab, ob
   ein Feld am Artikel reicht oder eine eigene Tabelle nötig ist.
3. Ist eine EAN eindeutig (nie bei zwei WMS-Artikeln)? Sonst Auswahl statt Direkttreffer.

## Antworten (Gerald 2026-10-16)

- Die EAN wird in **Sage** geführt (Tabelle/Spalte noch offen -> Vorfrage 1).
- Für IDEAL wird die EAN-Spalte in die View `vw_IDEAL_AKE_WMS_Artikel` **aufgenommen** (Änderung Gerald).
- Hinweis Koordination: Der Artikel-Sync fasst die View je `ArticleNumber` zusammen (`GROUP BY` + `MAX`,
  `SageArticleSql.BuildViewSql`). Liefert die View mehrere EAN-Zeilen je Artikel, bliebe nur eine übrig.
  Vorschlag: Spaltenname **`EAN`** (nvarchar), **eine EAN je Artikel** in dieser View; mehrere EAN je Artikel
  (Lieferant, Verpackungseinheit) nur über eine eigene View/Tabelle (Vorfrage 2).

## Skizze für die Spec (Vorschlag Koordination)

- Artikelstamm um EAN erweitern (Feld oder Tabelle je Vorfrage 2), Migration.
- Artikel-Sync liest die EAN mit (AKE: alter Select, IDEAL: neue View-Spalte); fehlt die Spalte in der
  View, läuft der Sync wie bisher ohne EAN (kein Abbruch).
- Scan-Lookup: Artikelnummer zuerst, danach EAN; Treffer zeigt den Artikel, mehrere Treffer -> Auswahl.
- Baut auf dem Scan-Handler auf, unabhängig davon aber auch für den Kamera-Scan nutzbar.

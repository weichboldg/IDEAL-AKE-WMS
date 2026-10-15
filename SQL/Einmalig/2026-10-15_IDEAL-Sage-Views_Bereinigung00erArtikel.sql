-- =====================================================================================================
-- !!! NICHT ERNEUT AUSFUEHREN !!!
-- =====================================================================================================
-- SQL/Einmalig/2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel.sql
--
-- EINMALIGES DATENSKRIPT — DATEN-DESTRUKTIV (loescht Zeilen aus dbo.Articles). DB-BACKUP VOR DEM LAUF.
-- KEINE Schema-Aenderung, KEIN __EFMigrationsHistory-Eintrag, KEIN Eintrag in SQL/00_FreshInstall.sql,
-- NICHT Teil der nummerierten SQL-Reihe (ADR 0015). NUR IDEAL (Guard bricht auf AKE ab).
--
-- Warum nie zweimal: Die Ausnahmeliste (Artikel, die die View noch liefert) ist von Hand aus P5b eingetragen
-- und ein Stand vom Tag der Bestaetigung. Ein spaeterer zweiter Lauf wuerde mit dieser veralteten Liste
-- 00er-Artikel loeschen, die die View inzwischen wieder liefert oder die seither neu angelegt wurden —
-- ungeprueft. Deshalb bricht das Skript ab, sobald die Sicherungstabelle existiert; ein neuer Lauf braucht
-- eine neue Liste und eine neue Bestaetigung.
--
-- Hintergrund (Spec 2026-10-14-ideal-sage-views-umstellung, v1.60.0): Der alte Artikel-Select las jede
-- Ressourcenummer aus KHKPpsRessourcenPositionen (auch Nicht-Artikel) und legte so "00er"-Zeilen an. Ab der
-- Umstellung auf die View vw_IDEAL_AKE_WMS_Artikel kommen sie nicht mehr nach; der Altbestand wird hier
-- bereinigt — NUR Zeilen ohne jede Referenz. Referenzierte Zeilen werden nur aufgelistet (Entscheidung Gerald,
-- nicht Teil dieses Skripts).
--
-- REIHENFOLGE (je System, zuerst IDEAL-Test, dann IDEAL live):
--   1. v1.60.0 deployen (Web, dann Service).
--   2. Sync:ArticlesViewName = [vw_IDEAL_AKE_WMS_Artikel] setzen (nach Pruefabfrage P4) und EINEN
--      erfolgreichen Lauf "Article" im Aktivitaets-Protokoll abwarten (sonst holt der alte Select die 00er
--      zurueck). P6 "nachher" ausfuehren.
--   3. P7 (Referenz-Vollstaendigkeit) auf der WMS-DB ausfuehren; weicht es ab, dieses Skript ergaenzen.
--   4. @Behalten unten mit dem Ergebnis von P5b fuellen (erwartet: leer).
--   5. Lauf mit @Loeschen = 0 (nur auflisten). Gerald bestaetigt die Liste "loeschbar".
--   6. ERNEUT DB-BACKUP. Lauf mit @Loeschen = 1 in einem ruhigen Fenster (keine laufende Kommissionierung,
--      keine Lagerbuchung).
--   7. Ergebnis ins Deploy-Protokoll der Spec: System, Datum, Kandidaten, referenziert, Geloescht.
--   8. Naechsten Artikel-Sync abwarten, P6 "nach Bereinigung" (nur noch referenzierte 00er).
--   9. Nach der Abnahme: DROP TABLE dbo.Articles_Bereinigung00er_Sicherung (eigener Protokolleintrag).
-- Danach Sync:ArticlesViewName NICHT mehr leeren (der alte Select legt die 00er mit neuer Id wieder an).
--
-- Kandidat = ArticleNumber LIKE '00%' und nicht in @Behalten. Referenziert = mindestens eine Zaehlspalte > 0:
--   FKs:  StockMovements.ArticleId (Restrict; deckt Bestand, Sage-Lagerbuchung, Bestandsabgleich ab),
--         ArticleAttributeValues.ArticleId (Cascade), FaWorkStepSpecs.ArticleId (SetNull)
--   Text: PickingItems.BomArticleNumber, WarehouseRequisitionItems.ArticleNumber, PartRequisitions.ArticleNumber,
--         ProductionOrders.ArticleNumber, OseonProductionOrders.ArticleNumber,
--         BeschichtungsauftragPositionen.Artnr/HauptArtnr
--   Sage-Spiegel (konservativ): FaHierarchyNodes.Artnr/HauptArtnr, CachedBomItems.Ressourcenummer,
--         CachedBomHeaders.Artikelnummer
-- Audit: ein DELETE hat keine Modified*-Felder; Nachweis = Sicherungstabelle (GesichertAm, GesichertVon) und
-- Deploy-Protokoll (ADR 0015).
-- =====================================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Loeschen bit = 0;   -- 0 = nur auflisten (Vorgabe), 1 = loeschbare Zeilen sichern und loeschen

-- P5b-Ergebnis: 00er-Nummern, die vw_IDEAL_AKE_WMS_Artikel NOCH liefert (erwartet: leer). Eine Zeile je Nummer:
DECLARE @Behalten TABLE (ArticleNumber nvarchar(100) PRIMARY KEY);
-- INSERT INTO @Behalten (ArticleNumber) VALUES (N'00...'), (N'00...');

-- ----- Guards (ohne GO dazwischen: RAISERROR + RETURN beendet den Batch) -----
IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings
               WHERE [Key] = N'ProduktionsauftragHierarchisch' AND LOWER(LTRIM(RTRIM([Value]))) = N'true')
BEGIN
    RAISERROR(N'Abbruch: kein IDEAL-System (ProduktionsauftragHierarchisch <> true). Keine Aenderung.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings
               WHERE [Key] = N'Sync:ArticlesViewName' AND LTRIM(RTRIM(ISNULL([Value], N''))) <> N'')
BEGIN
    RAISERROR(N'Abbruch: Sync:ArticlesViewName ist leer. Erst die View setzen und einen erfolgreichen Artikel-Sync abwarten (sonst kommen die 00er zurueck). Keine Aenderung.', 16, 1);
    RETURN;
END;

IF @Loeschen = 1 AND OBJECT_ID(N'dbo.Articles_Bereinigung00er_Sicherung', N'U') IS NOT NULL
BEGIN
    RAISERROR(N'Abbruch: dbo.Articles_Bereinigung00er_Sicherung existiert bereits — das Skript lief schon. Ein neuer Lauf braucht eine neue Liste und Bestaetigung. Keine Aenderung.', 16, 1);
    RETURN;
END;

-- ----- Kandidaten mit je einer Zaehlspalte pro Referenz -----
IF OBJECT_ID(N'tempdb..#Kandidaten') IS NOT NULL DROP TABLE #Kandidaten;

SELECT q.*,
       q.Bewegungen + q.Merkmale + q.Vorbauspezifikation + q.Kommissionierpositionen + q.Lagerbestellpositionen
     + q.Fehlteile + q.Fertigungsauftraege + q.OseonAuftraege + q.Beschichtungspositionen + q.FaStruktur
     + q.StuecklistenCachePositionen + q.StuecklistenCacheKoepfe AS Referenzen
INTO #Kandidaten
FROM (
SELECT a.Id, a.ArticleNumber, a.Description, a.CreatedAt,
       (SELECT COUNT(*) FROM dbo.StockMovements x            WHERE x.ArticleId = a.Id)                    AS Bewegungen,
       (SELECT COUNT(*) FROM dbo.ArticleAttributeValues x    WHERE x.ArticleId = a.Id)                    AS Merkmale,
       (SELECT COUNT(*) FROM dbo.FaWorkStepSpecs x           WHERE x.ArticleId = a.Id)                    AS Vorbauspezifikation,
       (SELECT COUNT(*) FROM dbo.PickingItems x              WHERE x.BomArticleNumber = a.ArticleNumber)  AS Kommissionierpositionen,
       (SELECT COUNT(*) FROM dbo.WarehouseRequisitionItems x WHERE x.ArticleNumber = a.ArticleNumber)     AS Lagerbestellpositionen,
       (SELECT COUNT(*) FROM dbo.PartRequisitions x          WHERE x.ArticleNumber = a.ArticleNumber)     AS Fehlteile,
       (SELECT COUNT(*) FROM dbo.ProductionOrders x          WHERE x.ArticleNumber = a.ArticleNumber)     AS Fertigungsauftraege,
       (SELECT COUNT(*) FROM dbo.OseonProductionOrders x     WHERE x.ArticleNumber = a.ArticleNumber)     AS OseonAuftraege,
       (SELECT COUNT(*) FROM dbo.BeschichtungsauftragPositionen x
                                                             WHERE x.Artnr = a.ArticleNumber OR x.HauptArtnr = a.ArticleNumber) AS Beschichtungspositionen,
       (SELECT COUNT(*) FROM dbo.FaHierarchyNodes x          WHERE x.Artnr = a.ArticleNumber OR x.HauptArtnr = a.ArticleNumber) AS FaStruktur,
       (SELECT COUNT(*) FROM dbo.CachedBomItems x            WHERE x.Ressourcenummer = a.ArticleNumber)   AS StuecklistenCachePositionen,
       (SELECT COUNT(*) FROM dbo.CachedBomHeaders x          WHERE x.Artikelnummer = a.ArticleNumber)     AS StuecklistenCacheKoepfe
FROM dbo.Articles a
WHERE a.ArticleNumber LIKE N'00%'
  AND NOT EXISTS (SELECT 1 FROM @Behalten b WHERE b.ArticleNumber = a.ArticleNumber)
) q;

-- (a) Zaehlzeile — ins Deploy-Protokoll
SELECT COUNT(*)                                        AS Kandidaten,
       SUM(CASE WHEN Referenzen > 0 THEN 1 ELSE 0 END) AS DavonReferenziert,
       SUM(CASE WHEN Referenzen = 0 THEN 1 ELSE 0 END) AS DavonLoeschbar,
       (SELECT COUNT(*) FROM @Behalten)                AS AusnahmenAusP5b
FROM #Kandidaten;

-- (b) referenziert — bleibt stehen, Entscheidung getrennt
SELECT N'referenziert' AS Liste, * FROM #Kandidaten WHERE Referenzen > 0 ORDER BY ArticleNumber;

-- (c) loeschbar — diese Liste bestaetigt Gerald vor dem Lauf mit @Loeschen = 1
SELECT N'loeschbar' AS Liste, Id, ArticleNumber, Description, CreatedAt FROM #Kandidaten WHERE Referenzen = 0 ORDER BY ArticleNumber;

IF @Loeschen = 0
BEGIN
    PRINT N'Nur aufgelistet (@Loeschen = 0) — keine Aenderung.';
    RETURN;
END;

-- ----- Loeschen: Referenzen erneut pruefen, sichern, loeschen, abgleichen — in EINER Transaktion -----
BEGIN TRANSACTION;

    -- Zeitfenster zwischen Liste und Loeschen: nur Ids, die JETZT noch ohne Referenz sind.
    DELETE k FROM #Kandidaten k
    WHERE k.Referenzen > 0
       OR EXISTS (SELECT 1 FROM dbo.StockMovements x            WHERE x.ArticleId = k.Id)
       OR EXISTS (SELECT 1 FROM dbo.ArticleAttributeValues x    WHERE x.ArticleId = k.Id)
       OR EXISTS (SELECT 1 FROM dbo.FaWorkStepSpecs x           WHERE x.ArticleId = k.Id)
       OR EXISTS (SELECT 1 FROM dbo.PickingItems x              WHERE x.BomArticleNumber = k.ArticleNumber)
       OR EXISTS (SELECT 1 FROM dbo.WarehouseRequisitionItems x WHERE x.ArticleNumber = k.ArticleNumber)
       OR EXISTS (SELECT 1 FROM dbo.PartRequisitions x          WHERE x.ArticleNumber = k.ArticleNumber)
       OR EXISTS (SELECT 1 FROM dbo.ProductionOrders x          WHERE x.ArticleNumber = k.ArticleNumber)
       OR EXISTS (SELECT 1 FROM dbo.OseonProductionOrders x     WHERE x.ArticleNumber = k.ArticleNumber)
       OR EXISTS (SELECT 1 FROM dbo.BeschichtungsauftragPositionen x WHERE x.Artnr = k.ArticleNumber OR x.HauptArtnr = k.ArticleNumber)
       OR EXISTS (SELECT 1 FROM dbo.FaHierarchyNodes x          WHERE x.Artnr = k.ArticleNumber OR x.HauptArtnr = k.ArticleNumber)
       OR EXISTS (SELECT 1 FROM dbo.CachedBomItems x            WHERE x.Ressourcenummer = k.ArticleNumber)
       OR EXISTS (SELECT 1 FROM dbo.CachedBomHeaders x          WHERE x.Artikelnummer = k.ArticleNumber);

    SELECT a.*, SYSDATETIME() AS GesichertAm, SYSTEM_USER AS GesichertVon
    INTO dbo.Articles_Bereinigung00er_Sicherung
    FROM dbo.Articles a
    WHERE a.Id IN (SELECT Id FROM #Kandidaten);

    DECLARE @Gesichert int = @@ROWCOUNT;

    DELETE FROM dbo.Articles WHERE Id IN (SELECT Id FROM dbo.Articles_Bereinigung00er_Sicherung);

    DECLARE @Geloescht int = @@ROWCOUNT;

    IF @Geloescht <> @Gesichert
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR(N'Abbruch: Geloescht (%d) <> Gesichert (%d). ROLLBACK — keine Aenderung.', 16, 1, @Geloescht, @Gesichert);
        RETURN;
    END;

COMMIT TRANSACTION;

SELECT @Geloescht AS Geloescht, @Gesichert AS Gesichert;   -- ins Deploy-Protokoll

-- =====================================================================================================
-- RUECKWEG (nur bei Bedarf, von Hand; vorher pruefen, dass keine der Nummern inzwischen neu angelegt wurde —
-- sonst scheitert der INSERT am Unique-Index: UQ_Articles_ArticleNumber auf per SQL-Skript angelegten DBs,
-- IX_Articles_ArticleNumber auf per EF angelegten DBs (fallstricke §9)). Alternativ: DB-Backup aus Schritt 6.
-- =====================================================================================================
-- SET IDENTITY_INSERT dbo.Articles ON;
-- INSERT INTO dbo.Articles (Id, ArticleNumber, Description, Matchcode, Unit, ReorderLevel, ArticleGroup,
--                           ArticleCategoryId, PrimaryStorageLocationId, SagePrimaryStorageLocation,
--                           CreatedAt, CreatedBy, CreatedByWindows, ModifiedAt, ModifiedBy, ModifiedByWindows)
-- SELECT Id, ArticleNumber, Description, Matchcode, Unit, ReorderLevel, ArticleGroup,
--        ArticleCategoryId, PrimaryStorageLocationId, SagePrimaryStorageLocation,
--        CreatedAt, CreatedBy, CreatedByWindows, ModifiedAt, ModifiedBy, ModifiedByWindows
-- FROM dbo.Articles_Bereinigung00er_Sicherung;
-- SET IDENTITY_INSERT dbo.Articles OFF;

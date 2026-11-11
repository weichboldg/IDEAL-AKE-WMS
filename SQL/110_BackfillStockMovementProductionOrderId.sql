-- =============================================================================
-- 110_BackfillStockMovementProductionOrderId.sql  (v1.87.0, Spec 2026-11-08-lagerbuchungen-fa-feld-und-verlinkung, Etappe 2)
-- Altdaten: StockMovements.ProductionOrderId aus dem FA-TEXT StockMovements.ProductionOrder (Antwort 1; die Notiz wird nur gezaehlt).
-- Regel = IProductionOrderRepository.FindForBookingAsync (C#, einziger Ort der Regel); hier gespiegelt:
--   getrimmt; ';' oder ',' -> Liste; genau ein FA mit SubOrderNumber = Text -> uebernommen; sonst Haupt-FAs
--   (SubOrderNumber = '' OR SubOrderNumber = OrderNumber) mit OrderNumber = Text: einer -> uebernommen, mehrere -> mehrdeutig, keiner -> unbekannt.
--   ponytail: Regel doppelt C#/SQL, weil Altdaten einmalig (bzw. nach dem Kommissionier-Epic erneut) laufen.
--
-- DEFAULT: @Trockenlauf = 1 -> aendert NICHTS, zeigt nur die Zaehlung. Scharf: @Trockenlauf = 0 (vorher DB-Backup), setzt nur
-- Klasse "uebernommen", in einer Transaktion, mit Audit-Feldern (ADR 0003). Idempotent: nur Zeilen mit ProductionOrderId IS NULL.
-- Text (ProductionOrder) und Note bleiben unveraendert. Ergebnis (beide Laeufe) als Datei in der Deploy-Aufgabe ablegen (Antwort 6).
-- Voraussetzung: SQL/109.
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Trockenlauf BIT = 1;   -- <<< 0 = scharf

IF COL_LENGTH(N'dbo.StockMovements', N'ProductionOrderId') IS NULL
BEGIN
    RAISERROR(N'SQL/109 fehlt (StockMovements.ProductionOrderId). Abbruch, nichts geaendert.', 16, 1);
    RETURN;
END

IF OBJECT_ID('tempdb..#Kandidat') IS NOT NULL DROP TABLE #Kandidat;

SELECT sm.Id, sm.MovementType, LTRIM(RTRIM(sm.ProductionOrder)) AS FaText
INTO #Kandidat
FROM dbo.StockMovements sm
WHERE sm.ProductionOrderId IS NULL AND NULLIF(LTRIM(RTRIM(sm.ProductionOrder)), N'') IS NOT NULL;

IF OBJECT_ID('tempdb..#Text') IS NOT NULL DROP TABLE #Text;

-- Klassifizierung je distinct Text (Regel siehe Kopf)
SELECT t.FaText,
       CASE
           WHEN t.FaText LIKE N'%;%' OR t.FaText LIKE N'%,%' THEN N'Liste'
           WHEN s.n = 1 THEN N'uebernommen'
           WHEN h.n = 1 THEN N'uebernommen'
           WHEN h.n > 1 THEN N'mehrdeutig'
           ELSE N'unbekannt'
       END AS Klasse,
       CASE
           WHEN t.FaText LIKE N'%;%' OR t.FaText LIKE N'%,%' THEN NULL
           WHEN s.n = 1 THEN s.Id
           WHEN h.n = 1 THEN h.Id
       END AS FaId
INTO #Text
FROM (SELECT DISTINCT FaText FROM #Kandidat) t
OUTER APPLY (SELECT COUNT(*) AS n, MIN(po.Id) AS Id FROM dbo.ProductionOrders po WHERE po.SubOrderNumber = t.FaText) s
OUTER APPLY (SELECT COUNT(*) AS n, MIN(po.Id) AS Id FROM dbo.ProductionOrders po
             WHERE po.OrderNumber = t.FaText AND (po.SubOrderNumber = N'' OR po.SubOrderNumber = po.OrderNumber)) h;

PRINT N'--- Zaehlung je Klasse und Bewegungsart (0=Einbuchung, 1=Ausbuchung, 2=Umbuchung, 3/4=Sage) – VOR dem Lauf';
SELECT x.Klasse, k.MovementType, COUNT(*) AS Bewegungen
FROM #Kandidat k JOIN #Text x ON x.FaText = k.FaText
GROUP BY x.Klasse, k.MovementType ORDER BY x.Klasse, k.MovementType;

PRINT N'--- Detail je Text';
SELECT x.FaText, x.Klasse, x.FaId, COUNT(*) AS Bewegungen
FROM #Kandidat k JOIN #Text x ON x.FaText = k.FaText
GROUP BY x.FaText, x.Klasse, x.FaId ORDER BY x.Klasse, Bewegungen DESC;

PRINT N'--- Notiz (nur Zaehlung, keine Uebernahme; Erwartung: nur "Sage-Korrektur:")';
SELECT LEFT(Note, 15) AS Anfang, COUNT(*) AS n FROM dbo.StockMovements WHERE Note IS NOT NULL GROUP BY LEFT(Note, 15) ORDER BY n DESC;

IF @Trockenlauf = 1
BEGIN
    -- v1.99.0: Zeile TREFFER fuer das Update-Skript (0 -> entfaellt); genau die Menge, die der Scharflauf aendert
    DECLARE @Treffer INT = (SELECT COUNT(*) FROM #Kandidat k JOIN #Text x ON x.FaText = k.FaText WHERE x.Klasse = N'uebernommen');
    PRINT CONCAT(N'TREFFER: ', @Treffer);
    PRINT N'TROCKENLAUF: nichts geaendert. Scharf mit @Trockenlauf = 0 (vorher DB-Backup).';
    RETURN;
END

BEGIN TRANSACTION;
    UPDATE sm
       SET sm.ProductionOrderId = x.FaId,
           sm.ModifiedAt = SYSDATETIME(),
           sm.ModifiedBy = N'SQL110-FA-Zuordnung',
           sm.ModifiedByWindows = LEFT(SUSER_SNAME(), 200)
    FROM dbo.StockMovements sm
    JOIN #Kandidat k ON k.Id = sm.Id
    JOIN #Text x ON x.FaText = k.FaText
    WHERE x.Klasse = N'uebernommen' AND sm.ProductionOrderId IS NULL;
    PRINT CONCAT(N'Verknuepft: ', @@ROWCOUNT, N' Bewegung(en).');
COMMIT TRANSACTION;

PRINT N'--- Rest nach dem Lauf (ProductionOrderId IS NULL mit Text)';
SELECT COUNT(*) AS OhneVerknuepfung FROM dbo.StockMovements
WHERE ProductionOrderId IS NULL AND NULLIF(LTRIM(RTRIM(ProductionOrder)), N'') IS NOT NULL;

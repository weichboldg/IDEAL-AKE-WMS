/* TS-91 Testdaten - alle Platzhalter fuer Teil B in EINEM Lauf (IDEAL-Testsystem-DB, NUR LESEN)
   Zum Protokoll [[2026-10-13-ts-91-browser-testprotokoll]], ersetzt die Einzelabfragen in A6.
   Ergebnis: Tabelle "Platzhalter | Wert" - so wie sie ist an den Abnahme-Agenten geben.
   Koordination 2026-10-13. Kein Schreibzugriff (nur SELECT in Tabellenvariable). */
SET NOCOUNT ON;
DECLARE @out TABLE (Nr int IDENTITY(1,1), Platzhalter nvarchar(60), Wert nvarchar(400));
DECLARE @KF nvarchar(10) = N'KEIN_FALL';

-- Schalter (nur zur Info fuer die Bestaetigungen)
INSERT @out (Platzhalter, Wert)
SELECT N'Schalter ' + [Key], [Value] FROM dbo.AppSettings
WHERE [Key] IN ('ProduktionsauftragHierarchisch','LeitstandAktiv','FaHierarchyBeschichtungAktiv',
                'FaHierarchyKommissionierlistenAktiv','BdeAktiv','LagerbestellungAktiv','FaCompletionAktiv');

-- Bewegungshistorie: Stichtag = drittjuengster Tag mit Bewegungen (davor und danach gibt es weitere Tage)
DECLARE @d date = (SELECT Tag FROM (SELECT CAST([Timestamp] AS date) AS Tag,
                     ROW_NUMBER() OVER (ORDER BY CAST([Timestamp] AS date) DESC) AS rn
                   FROM dbo.StockMovements GROUP BY CAST([Timestamp] AS date)) x WHERE rn = 3);
IF @d IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.StockMovements WHERE [Timestamp] < @d)
    INSERT @out (Platzhalter, Wert) SELECT P, @KF FROM (VALUES (N'STICHTAG_BEW'),(N'BEW_GESAMT'),(N'BEW_AM_TAG'),(N'BEW_AB_TAG'),
        (N'BEW_NACH_TAG'),(N'BEW_BIS_TAG'),(N'BEW_VOR_TAG'),(N'BEW_AUS_GENAU'),(N'BEW_AUS_ENTH'),(N'BEW_NICHT_AUS')) v(P);
ELSE
BEGIN
    INSERT @out (Platzhalter, Wert) VALUES (N'STICHTAG_BEW', CONVERT(nvarchar(10), @d, 104));
    INSERT @out (Platzhalter, Wert)
    SELECT v.P, CAST(v.W AS nvarchar(20)) FROM (
        SELECT COUNT(*) AS Gesamt,
               SUM(CASE WHEN CAST([Timestamp] AS date) = @d THEN 1 ELSE 0 END) AS AmTag,
               SUM(CASE WHEN [Timestamp] >= @d THEN 1 ELSE 0 END) AS AbTag,
               SUM(CASE WHEN [Timestamp] >= DATEADD(day, 1, CAST(@d AS datetime2)) THEN 1 ELSE 0 END) AS NachTag,
               SUM(CASE WHEN [Timestamp] <  DATEADD(day, 1, CAST(@d AS datetime2)) THEN 1 ELSE 0 END) AS BisTag,
               SUM(CASE WHEN [Timestamp] < @d THEN 1 ELSE 0 END) AS VorTag,
               SUM(CASE WHEN MovementType = 1 THEN 1 ELSE 0 END) AS AusGenau,
               SUM(CASE WHEN MovementType IN (1, 4) THEN 1 ELSE 0 END) AS AusEnth,
               SUM(CASE WHEN MovementType NOT IN (1, 4) THEN 1 ELSE 0 END) AS NichtAus
        FROM dbo.StockMovements) s
    CROSS APPLY (VALUES (N'BEW_GESAMT', s.Gesamt), (N'BEW_AM_TAG', s.AmTag), (N'BEW_AB_TAG', s.AbTag),
                        (N'BEW_NACH_TAG', s.NachTag), (N'BEW_BIS_TAG', s.BisTag), (N'BEW_VOR_TAG', s.VorTag),
                        (N'BEW_AUS_GENAU', s.AusGenau), (N'BEW_AUS_ENTH', s.AusEnth), (N'BEW_NICHT_AUS', s.NichtAus)) v(P, W);
END

-- Beschichtung > Auftraege: Stichtag = zweitfruehestes Start-Datum (so ist "ab" kleiner als Gesamt)
DECLARE @s date = (SELECT Tag FROM (SELECT CAST(StartBeschichtung AS date) AS Tag,
                     ROW_NUMBER() OVER (ORDER BY CAST(StartBeschichtung AS date)) AS rn
                   FROM dbo.Beschichtungsauftraege WHERE StartBeschichtung IS NOT NULL
                   GROUP BY CAST(StartBeschichtung AS date)) x WHERE rn = 2);
IF NOT EXISTS (SELECT 1 FROM dbo.Beschichtungsauftraege)
    INSERT @out (Platzhalter, Wert) VALUES (N'STICHTAG_BA', @KF), (N'BA_GESAMT', @KF), (N'BA_START_AB', @KF),
                                           (N'BA_RETOUR_LEER', @KF), (N'BA_POS_UEBER_2', @KF);
ELSE
BEGIN
    INSERT @out (Platzhalter, Wert) VALUES (N'STICHTAG_BA', ISNULL(CONVERT(nvarchar(10), @s, 104), @KF));
    INSERT @out (Platzhalter, Wert)
    SELECT v.P, CASE WHEN v.W IS NULL THEN @KF ELSE CAST(v.W AS nvarchar(20)) END FROM (
        SELECT COUNT(*) AS Gesamt,
               CASE WHEN @s IS NULL THEN NULL ELSE SUM(CASE WHEN b.StartBeschichtung >= @s THEN 1 ELSE 0 END) END AS StartAb,
               SUM(CASE WHEN b.BeschichtenRetour IS NULL THEN 1 ELSE 0 END) AS RetourLeer,
               SUM(CASE WHEN x.Pos > 2 THEN 1 ELSE 0 END) AS PosUeber2
        FROM dbo.Beschichtungsauftraege b
        OUTER APPLY (SELECT COUNT(*) AS Pos FROM dbo.BeschichtungsauftragPositionen p WHERE p.BeschichtungsauftragId = b.Id) x) s
    CROSS APPLY (VALUES (N'BA_GESAMT', s.Gesamt), (N'BA_START_AB', s.StartAb),
                        (N'BA_RETOUR_LEER', s.RetourLeer), (N'BA_POS_UEBER_2', s.PosUeber2)) v(P, W);
END

-- ID_BA: Auftrag Uebergeben/Erledigt (30/40), moeglichst mit Nachkomma, viele Positionen
INSERT @out (Platzhalter, Wert)
SELECT N'ID_BA', ISNULL((SELECT TOP 1 CAST(b.Id AS nvarchar(20)) + N'  (' + b.Nummer + N')'
    FROM dbo.Beschichtungsauftraege b JOIN dbo.BeschichtungsauftragPositionen p ON p.BeschichtungsauftragId = b.Id
    WHERE b.Status IN (30, 40) GROUP BY b.Id, b.Nummer
    ORDER BY SUM(CASE WHEN p.Breite <> FLOOR(p.Breite) OR p.Menge <> FLOOR(p.Menge) THEN 1 ELSE 0 END) DESC, COUNT(*) DESC), @KF);

-- Escaping: Artikelnummer mit "_" und eine andere, die ein UNmaskiertes "_" faelschlich traefe
DECLARE @u nvarchar(100), @f nvarchar(100);
SELECT TOP 1 @u = a.ArticleNumber, @f = b.ArticleNumber
FROM (SELECT TOP 500 ArticleNumber FROM dbo.Articles
      WHERE ArticleNumber LIKE '%[_]%' AND ArticleNumber NOT LIKE '%[%[]%' ORDER BY ArticleNumber) a
CROSS APPLY (SELECT TOP 1 bb.ArticleNumber FROM dbo.Articles bb
             WHERE bb.ArticleNumber LIKE a.ArticleNumber AND bb.ArticleNumber <> a.ArticleNumber) b;
IF @u IS NULL SELECT TOP 1 @u = ArticleNumber FROM dbo.Articles WHERE ArticleNumber LIKE '%[_]%' ORDER BY ArticleNumber;
INSERT @out (Platzhalter, Wert) VALUES (N'ARTNR_UNTERSTRICH', ISNULL(@u, @KF)), (N'ARTNR_FALSCHTREFFER', ISNULL(@f, @KF));

INSERT @out (Platzhalter, Wert)
SELECT N'TEXT_PROZENT', ISNULL((SELECT TOP 1
    SUBSTRING(Description, CASE WHEN CHARINDEX('%', Description) > 4 THEN CHARINDEX('%', Description) - 3 ELSE 1 END, 7)
    + N'   (Artikel ' + ArticleNumber + N': ' + Description + N')'
    FROM dbo.Articles WHERE Description LIKE '%_[%]_%' ORDER BY ArticleNumber), @KF);

INSERT @out (Platzhalter, Wert)
SELECT N'ARTNR_FA_UNTERSTRICH', ISNULL((SELECT TOP 1 ArticleNumber + N'   (FA ' + OrderNumber + N')'
    FROM dbo.ProductionOrders WHERE ArticleNumber LIKE '%[_]%' ORDER BY OrderNumber DESC), @KF);

-- Stueckliste mit Komm.-Zielen: Id der HauptFA-Zeile
DECLARE @hfaBom nvarchar(100) = (SELECT TOP 1 HauptFA FROM dbo.FaHierarchyNodes GROUP BY HauptFA
    HAVING SUM(CASE WHEN LTRIM(RTRIM(ISNULL(Kommissionieren,''))) <> '' THEN 1 ELSE 0 END) > 0
    ORDER BY SUM(CASE WHEN LTRIM(RTRIM(ISNULL(Kommissionieren,''))) <> '' THEN 1 ELSE 0 END) DESC);
INSERT @out (Platzhalter, Wert)
SELECT N'ID_BOM', ISNULL((SELECT TOP 1 CAST(Id AS nvarchar(20)) + N'  (HauptFA ' + @hfaBom + N')'
    FROM dbo.ProductionOrders WHERE OrderNumber = @hfaBom AND SubOrderNumber = OrderNumber), @KF);

-- FA-Struktur: Zeilen genau "KA" und Zeilen mit KA neben anderen Kuerzeln, kleinste passende Struktur
INSERT @out (Platzhalter, Wert)
SELECT v.P, ISNULL(v.W, @KF) FROM (SELECT 1 AS d) z
OUTER APPLY (SELECT TOP 1 HauptFA,
        SUM(CASE WHEN LTRIM(RTRIM(Arbeitsschritte)) = 'KA' THEN 1 ELSE 0 END) AS NurKA,
        SUM(CASE WHEN ' ' + Arbeitsschritte + ' ' LIKE '% KA %' THEN 1 ELSE 0 END) AS MitKA
    FROM dbo.FaHierarchyNodes GROUP BY HauptFA
    HAVING SUM(CASE WHEN LTRIM(RTRIM(Arbeitsschritte)) = 'KA' THEN 1 ELSE 0 END) > 0
       AND SUM(CASE WHEN ' ' + Arbeitsschritte + ' ' LIKE '% KA %' THEN 1 ELSE 0 END)
         > SUM(CASE WHEN LTRIM(RTRIM(Arbeitsschritte)) = 'KA' THEN 1 ELSE 0 END)
    ORDER BY COUNT(*)) h
CROSS APPLY (VALUES (N'HFA_BAUM', h.HauptFA), (N'ANZ_NUR_KA', CAST(h.NurKA AS nvarchar(20))),
                    (N'ANZ_MIT_KA', CAST(h.MitKA AS nvarchar(20)))) v(P, W);

-- Lagerbestellungen
INSERT @out (Platzhalter, Wert)
SELECT N'ID_EINGEHEND', ISNULL((SELECT TOP 1 CAST(r.Id AS nvarchar(20))
    FROM dbo.WarehouseRequisitions r JOIN dbo.WarehouseRequisitionItems i ON i.WarehouseRequisitionId = r.Id
    WHERE r.Status = 3 GROUP BY r.Id HAVING COUNT(*) >= 6 ORDER BY r.Id DESC), @KF);

INSERT @out (Platzhalter, Wert)
SELECT v.P, ISNULL(f.ArticleNumber, @KF)
FROM (VALUES (1, N'ARTNR_F1'), (2, N'ARTNR_F2')) v(n, P)
LEFT JOIN (SELECT i.ArticleNumber, ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC, i.ArticleNumber) AS n
           FROM dbo.WarehouseRequisitionItems i JOIN dbo.WarehouseRequisitions r ON r.Id = i.WarehouseRequisitionId
           WHERE i.ShortageStatus = 1 AND r.Status IN (3, 5) GROUP BY i.ArticleNumber) f ON f.n = v.n;

-- BDE
INSERT @out (Platzhalter, Wert)
SELECT N'BDE_BUCHUNGEN', CASE WHEN COUNT(*) = 0 THEN @KF ELSE CAST(COUNT(*) AS nvarchar(20)) END FROM dbo.BdeBookings;

SELECT Platzhalter, Wert FROM @out ORDER BY Nr;

-- =====================================================================================================
-- SQL/Einmalig/2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste.sql
--
-- EINMALIGES DATENSKRIPT (ADR 0015/0023) - KEINE Schema-Aenderung, KEIN __EFMigrationsHistory-Eintrag, KEIN Eintrag in
-- SQL/00_FreshInstall.sql, NICHT Teil der nummerierten SQL-Reihe. NUR Standort IDEAL (FaHierarchyNodes nur dort befuellt).
-- DATEN-LOESCHEND -> DB-BACKUP VOR DEM SCHARFLAUF.
-- Spec 2026-11-13-bde-vereinfacht-teileliste-blechfertigung, Etappe E7, Freigabe-Antwort 1 = V1 (ADR 0030).
--
-- Hintergrund: Werkbaenke mit Option "BDE-Teileliste" melden Blechteile (Blatt-Zeilen der FA-Struktur) als Teilmeldung. Seit
-- v1.99.32 legt die Arbeitsgang-Erkennung fuer Kuerzel solcher Werkbaenke KEINEN Arbeitsgang mehr an der Vater-Sub-FA an, wenn
-- das Kuerzel nur von Blatt-Kindern stammt. Die Erkennung loescht nie (Nur-hinzufuegen) - dieses Skript entfernt den Bestand.
--
-- Kandidat = WorkOperation, angelegt von der Erkennung (CreatedBy = 'WorkOperationStructureDetection'), deren OperationNumber
--   * das Kuerzel einer Werkbank mit BdeAktiv = 1, BdeTeileliste = 1, SageMissingSince IS NULL ist, UND
--   * an einer Sub-FA P haengt, deren EIGENE Arbeitsschritte das Kuerzel NICHT enthalten, UND
--   * in den Arbeitsschritten eines direkten BLATT-Kindes von P (SubFA = 0) steht.
-- Geloescht werden nur ungebuchte Kandidaten (keine BdeBookings - auch stornierte, FK ist Restrict - und IsReported = 0).
-- Gebuchte/rueckgemeldete werden nur gemeldet (Liste ins Deploy-Protokoll, Klaerung mit dem Fachbereich).
-- Manuell angelegte Arbeitsgaenge (anderes CreatedBy) werden NIE angefasst.
--
-- Meldung 130 (P-TL3): EXISTS darf nicht in einer Aggregatfunktion stehen. Deshalb wird IsBooked je Kandidat in einer
-- NICHT aggregierenden Abfrage nach #Cand berechnet (CASE WHEN EXISTS ...), gezaehlt wird danach mit SUM(CASE ...) ueber #Cand.
--
-- Wiederholbar ohne Sperre: nach v1.99.32 entstehen solche Arbeitsgaenge nicht mehr, ein zweiter Trockenlauf liefert TREFFER: 0
-- (gebuchte Kandidaten zaehlen nicht als Treffer). Kuerzel, deren Werkbank die Option spaeter bekommt, findet ein erneuter Lauf.
--
-- REIHENFOLGE: 1. Dienst und Web >= 1.99.32 laufen (vorher sinnlos: der alte Dienst legt die Arbeitsgaenge wieder an).
--   2. Option "BDE-Teileliste" an den Start-Werkbaenken gesetzt.  3. Trockenlauf (Default), Zaehlung + Meldeliste ins Protokoll.
--   4. DB-Backup.  5. @Trockenlauf = 0 (Update-Skript: SCHARF).  6. Zweiter Trockenlauf muss TREFFER: 0 liefern.
--   P-TL3 (IDEAL TEST 2026-11-17): IF 4, KA 120, SM 6, ST 120 Arbeitsgaenge, 0 gebucht - Erwartung, sobald die Option dort an ist.
-- =====================================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Trockenlauf BIT = 1;   -- <<< 0 = ausfuehren (nach DB-Backup und Freigabe der Zaehlung)

IF COL_LENGTH(N'dbo.ProductionWorkplaces', N'BdeTeileliste') IS NULL
BEGIN
    RAISERROR(N'SQL/154 fehlt (dbo.ProductionWorkplaces.BdeTeileliste). Abbruch, nichts geaendert.', 16, 1);
    RETURN;
END

IF OBJECT_ID('tempdb..#Codes') IS NOT NULL DROP TABLE #Codes;
IF OBJECT_ID('tempdb..#Tok') IS NOT NULL DROP TABLE #Tok;
IF OBJECT_ID('tempdb..#Cand') IS NOT NULL DROP TABLE #Cand;

-- Kuerzel wirksamer Teileliste-Werkbaenke (getrimmt; Vergleiche case-insensitiv ueber die DB-Collation Latin1_General_CI_AS).
SELECT DISTINCT LTRIM(RTRIM(ArbeitsschrittCode)) AS Code
INTO #Codes
FROM dbo.ProductionWorkplaces
WHERE BdeAktiv = 1 AND BdeTeileliste = 1 AND SageMissingSince IS NULL
  AND ArbeitsschrittCode IS NOT NULL AND LTRIM(RTRIM(ArbeitsschrittCode)) <> N'';

-- Token je Strukturzeile (Leerzeichen-getrennt wie TokenSeparators im Dienst, getrimmt, leere weg).
SELECT n.SubFA, n.VaterFA, LTRIM(RTRIM(s.value)) AS Token
INTO #Tok
FROM dbo.FaHierarchyNodes n
CROSS APPLY STRING_SPLIT(n.Arbeitsschritte, ' ') s
WHERE n.Arbeitsschritte IS NOT NULL AND LTRIM(RTRIM(s.value)) <> N'';

-- Kandidaten mit IsBooked je Zeile (nicht aggregierend -> keine Meldung 130).
SELECT DISTINCT wo.Id AS WorkOperationId, wo.OperationNumber, po.OrderNumber, po.SubOrderNumber,
       CASE WHEN wo.IsReported = 1 OR EXISTS (SELECT 1 FROM dbo.BdeBookings b WHERE b.WorkOperationId = wo.Id) THEN 1 ELSE 0 END AS IsBooked
INTO #Cand
FROM dbo.WorkOperations wo
JOIN dbo.ProductionOrders po ON po.Id = wo.ProductionOrderId
JOIN dbo.FaHierarchyNodes p  ON p.SubFA <> 0 AND po.SubOrderNumber = CAST(p.SubFA AS NVARCHAR(100))
WHERE wo.CreatedBy = N'WorkOperationStructureDetection'
  AND wo.OperationNumber IN (SELECT Code FROM #Codes)
  -- nicht von der eigenen Zeile der Sub-FA ...
  AND NOT EXISTS (SELECT 1 FROM #Tok t WHERE t.SubFA = p.SubFA AND t.Token = wo.OperationNumber)
  -- ... sondern von einem direkten Blatt-Kind.
  AND EXISTS (SELECT 1 FROM #Tok t WHERE t.VaterFA = p.SubFA AND t.SubFA = 0 AND t.Token = wo.OperationNumber);

PRINT N'--- Zaehlung je Kuerzel (Trockenlauf und Ausfuehrung)';
SELECT OperationNumber AS Kuerzel, COUNT(*) AS Kandidaten,
       SUM(CASE WHEN IsBooked = 0 THEN 1 ELSE 0 END) AS ZuLoeschen_OhneBuchung,
       SUM(CASE WHEN IsBooked = 1 THEN 1 ELSE 0 END) AS NurMelden_GebuchtOderRueckgemeldet
FROM #Cand GROUP BY OperationNumber ORDER BY OperationNumber;

PRINT N'--- Meldeliste: gebuchte oder rueckgemeldete Kandidaten bleiben stehen (Klaerung mit dem Fachbereich)';
SELECT OrderNumber AS HauptFA, SubOrderNumber AS SubFA, OperationNumber AS Kuerzel, WorkOperationId
FROM #Cand WHERE IsBooked = 1
ORDER BY OrderNumber, SubOrderNumber, OperationNumber;

IF @Trockenlauf = 1
BEGIN
    DECLARE @Treffer INT = (SELECT COUNT(*) FROM #Cand WHERE IsBooked = 0);
    PRINT CONCAT(N'TREFFER: ', @Treffer);
    PRINT N'TROCKENLAUF: nichts geaendert. Ausfuehren mit @Trockenlauf = 0 (vorher DB-Backup, Zaehlung freigeben).';
    RETURN;
END

BEGIN TRANSACTION;
    DELETE wo
    FROM dbo.WorkOperations wo
    JOIN #Cand c ON c.WorkOperationId = wo.Id
    WHERE c.IsBooked = 0
      AND wo.IsReported = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.BdeBookings b WHERE b.WorkOperationId = wo.Id); -- Doppelsicherung
    PRINT CONCAT(N'Arbeitsgaenge geloescht: ', @@ROWCOUNT);
COMMIT TRANSACTION;

DROP TABLE #Cand;
DROP TABLE #Tok;
DROP TABLE #Codes;

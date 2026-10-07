-- =====================================================================================================
-- !!! NICHT ERNEUT AUSFUEHREN !!!
-- =====================================================================================================
-- SQL/Einmalig/2026-10-07_v1.47.4_RemoveParentWorkOperationsFromChildSubFaTokens.sql
--
-- EINMALIGES DATENSKRIPT (ADR 0015) — KEINE Schema-Aenderung, KEIN __EFMigrationsHistory-Eintrag, KEIN
-- Eintrag in SQL/00_FreshInstall.sql, NICHT Teil der nummerierten SQL-Reihe. NUR Standort IDEAL
-- (FaHierarchyNodes ist nur dort befuellt; auf AKE findet Schritt 1 nichts).
--
-- Hintergrund (Freigabe-Nachtrag 2026-10-07 zu 2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec):
-- Ein Kuerzel auf einer Strukturzeile, die SELBST ein Sub-FA ist, gehoert nur zu diesem Sub-FA. Bis v1.47.3
-- hat die Arbeitsgang-Erkennung solche Kuerzel ZUSAETZLICH am Vater-Sub-FA angelegt (DirectChildren-Scope
-- ohne Blatt-Filter). Die Erkennung loescht nie (Nur-hinzufuegen) — dieses Skript entfernt die vor v1.47.4
-- angelegten Vater-Arbeitsgaenge, deren Kuerzel AUSSCHLIESSLICH von Kind-Sub-FA-Zeilen stammt.
--
-- Kandidat = WorkOperation am Vater-Sub-FA P, angelegt von der Erkennung (CreatedBy =
-- 'WorkOperationStructureDetection'), deren OperationNumber
--   * in den Arbeitsschritten eines direkten Kindes von P mit eigenem Sub-FA (SubFA <> 0) steht, UND
--   * weder in P's eigenen Arbeitsschritten noch in denen eines direkten BLATT-Kindes (SubFA = 0) steht
--     (diese bleiben nach neuer Regel legitim am Vater).
--
-- NICHT geloescht, nur gemeldet (Schritt 2): Kandidaten mit BDE-Buchung (BdeBookings, auch storniert —
-- FK ist Restrict) oder mit manueller Tracking-Rueckmeldung (IsReported = 1). Die bleiben stehen;
-- Klaerung mit dem Fachbereich.
-- Manuell angelegte Arbeitsgaenge (anderes CreatedBy) und der Default-AG werden NIE angefasst.
--
-- REIHENFOLGE:
--   1. DB-Backup.
--   2. Windows-Service v1.47.4 (und Web) deployen. Vorher laufen lassen ist sinnlos: der alte
--      Erkennungs-Lauf (v1.47.3) legt die geloeschten Vater-Arbeitsgaenge beim naechsten Sync wieder an.
--   3. DANN dieses Skript genau EINMAL; Ergebnisse (Zaehlung, Meldeliste, ROWCOUNT) ins Deploy-Protokoll.
-- Warum nie zweimal: Nach v1.47.4 legt die Erkennung solche Vater-Arbeitsgaenge nicht mehr an; ein zweiter
-- Lauf faende hoechstens Arbeitsgaenge, die seither bewusst angelegt wurden (z. B. nach einer
-- Strukturaenderung in Sage) — die gehoeren nicht still geloescht.
-- =====================================================================================================
SET NOCOUNT ON;

IF OBJECT_ID('tempdb..#Tok') IS NOT NULL DROP TABLE #Tok;
IF OBJECT_ID('tempdb..#Cand') IS NOT NULL DROP TABLE #Cand;

-- Token je Strukturzeile (Leerzeichen-getrennt wie TokenSeparators im Service, getrimmt, leere weg).
SELECT n.SubFA, n.VaterFA, LTRIM(RTRIM(s.value)) AS Token
INTO #Tok
FROM dbo.FaHierarchyNodes n
CROSS APPLY STRING_SPLIT(n.Arbeitsschritte, ' ') s
WHERE n.Arbeitsschritte IS NOT NULL AND LTRIM(RTRIM(s.value)) <> '';

-- Kandidaten bestimmen (Vergleiche case-insensitiv ueber die DB-Collation Latin1_General_CI_AS).
SELECT DISTINCT wo.Id AS WorkOperationId, wo.OperationNumber, po.OrderNumber, po.SubOrderNumber,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.BdeBookings b WHERE b.WorkOperationId = wo.Id)
                 OR wo.IsReported = 1 THEN 1 ELSE 0 END AS IsBooked
INTO #Cand
FROM dbo.WorkOperations wo
JOIN dbo.ProductionOrders po ON po.Id = wo.ProductionOrderId
JOIN dbo.FaHierarchyNodes p  ON p.SubFA <> 0 AND po.SubOrderNumber = CAST(p.SubFA AS NVARCHAR(100))
WHERE wo.CreatedBy = 'WorkOperationStructureDetection'
  -- stammt von einem direkten Kind mit eigenem Sub-FA ...
  AND EXISTS (SELECT 1 FROM #Tok t WHERE t.VaterFA = p.SubFA AND t.SubFA <> 0 AND t.Token = wo.OperationNumber)
  -- ... und NICHT von der eigenen Zeile ...
  AND NOT EXISTS (SELECT 1 FROM #Tok t WHERE t.SubFA = p.SubFA AND t.Token = wo.OperationNumber)
  -- ... und NICHT von einem direkten Blatt-Kind (bleibt nach neuer Regel am Vater).
  AND NOT EXISTS (SELECT 1 FROM #Tok t WHERE t.VaterFA = p.SubFA AND t.SubFA = 0 AND t.Token = wo.OperationNumber);

-- 1) Vorab zaehlen — Ergebnis ins Deploy-Protokoll.
SELECT COUNT(*) AS KandidatenGesamt,
       SUM(CASE WHEN IsBooked = 0 THEN 1 ELSE 0 END) AS ZuLoeschen_OhneBuchung,
       SUM(CASE WHEN IsBooked = 1 THEN 1 ELSE 0 END) AS NurMelden_GebuchtOderRueckgemeldet
FROM #Cand;

-- 2) Meldeliste: gebuchte/rueckgemeldete Kandidaten bleiben stehen (Klaerung mit dem Fachbereich).
SELECT c.OrderNumber AS HauptFA, c.SubOrderNumber AS VaterSubFA, c.OperationNumber, c.WorkOperationId
FROM #Cand c WHERE c.IsBooked = 1
ORDER BY c.OrderNumber, c.SubOrderNumber, c.OperationNumber;

-- 3) Loeschen: nur ungebuchte Kandidaten (keine BDE-Buchung, nicht rueckgemeldet).
BEGIN TRANSACTION;
    DELETE wo
    FROM dbo.WorkOperations wo
    JOIN #Cand c ON c.WorkOperationId = wo.Id
    WHERE c.IsBooked = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.BdeBookings b WHERE b.WorkOperationId = wo.Id); -- Doppelsicherung
    SELECT @@ROWCOUNT AS ArbeitsgaengeGeloescht;
COMMIT TRANSACTION;

DROP TABLE #Cand;
DROP TABLE #Tok;

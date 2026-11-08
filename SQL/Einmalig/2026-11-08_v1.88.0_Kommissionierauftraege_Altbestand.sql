-- =====================================================================================================
-- !!! NICHT ERNEUT AUSFUEHREN !!!
-- =====================================================================================================
-- SQL/Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand.sql
--
-- EINMALIGES DATENSKRIPT (ADR 0015) — KEINE Schema-Aenderung, KEIN __EFMigrationsHistory-Eintrag, KEIN Eintrag in
-- SQL/00_FreshInstall.sql, NICHT Teil der nummerierten SQL-Reihe. Beide Standorte. DATEN-UMBAUEND → DB-BACKUP VOR DEPLOY.
-- Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen, Etappe E5, Antworten 3 und 10.
--
-- Was: je FA mit Kommissionier-Fortschritt im Altbestand (PickingItems mit IsPicked = 1 ODER IsTransferred = 1) GENAU EIN
-- Kommissionierauftrag im Standardbereich, Typ FA, Nummer KO-<Jahr>-NNNNN fortlaufend ab MAX+1 des Jahres, Bemerkung
-- „Altbestand vor v1.88.0“. Zeilen nur aus beruehrten PickingItems:
--   IsTransferred = 1 → Umgebucht (IstMenge = Quantity = tatsaechlich gebuchte Menge, StockMovementId NULL – nicht sicher zuordenbar)
--   IsPicked = 1 ohne Umbuchung → Gepickt (IstMenge NULL)
--   SollMenge = Quantity (Antwort 3: Altbestand-Ist = gebuchte Menge; Befund B1 – AKE buchte je Stueck).
-- Kopfstatus: alle Zeilen umgebucht ODER FA erledigt (IsDone / IsDonePicking) → Abgeschlossen, sonst In Arbeit.
-- Prioritaet/Kommissionierer aus ProductionOrderPickingStatus. PickingItems bleiben UNVERAENDERT (Antwort 11).
-- v1.89.0 (Entscheidung Q11, Gerald 2026-11-08): offene FAs mit Auftragsmenge <> 1 bekommen KEINEN automatischen Rest-Auftrag. Ihr
-- Altbestand-Auftrag traegt AltbestandPruefen = 1 (SQL/141); das Skript gibt sie als Liste aus (Trockenlauf und Ausfuehrung, ins
-- Deploy-Protokoll). Die Kommissionierung legt fuer diese FAs nichts an, bis der Leitstand „Altbestand geprueft“ bestaetigt.
-- Offene Freigaben OHNE Fortschritt legt danach der Leitstand-Knopf „Fehlende Kommissionieraufträge erzeugen“ an (mit Bereichen).
--
-- REIHENFOLGE (Spec, Abschnitt Deploy):
--   1. DB-Backup.  2. Web stoppen.  3. SQL/133 und SQL/141.  4. DIESES Skript im Trockenlauf (Default), Zaehlungen ins Deploy-Protokoll.
--   5. Freigabe der Zaehlung durch Gerald.  6. @Trockenlauf = 0 ausfuehren.  7. Web v1.88.0 starten.
--   8. Leitstand: „Fehlende Kommissionieraufträge für alle freigegebenen FAs erzeugen“ (ueberspringt die Liste „Altbestand pruefen“).
--   9. Je FA der Liste im Leitstand pruefen, „Altbestand geprüft“, dann erneut erzeugen.  10. Stichprobe TS-120.11.
--   Zwischen 6 und 8 darf niemand kommissionieren (Wartungsfenster).
-- =====================================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Trockenlauf BIT = 1;   -- <<< 0 = ausfuehren (nach DB-Backup und Freigabe der Zaehlung)
DECLARE @Jahr INT = YEAR(GETDATE());
DECLARE @Wer NVARCHAR(200) = N'Migration-KO-Altbestand';

IF OBJECT_ID(N'dbo.PickingOrders', N'U') IS NULL
BEGIN
    RAISERROR(N'SQL/133 fehlt (dbo.PickingOrders). Abbruch, nichts geaendert.', 16, 1);
    RETURN;
END
IF COL_LENGTH(N'dbo.PickingOrders', N'AltbestandPruefen') IS NULL
BEGIN
    RAISERROR(N'SQL/141 fehlt (dbo.PickingOrders.AltbestandPruefen). Abbruch, nichts geaendert.', 16, 1);
    RETURN;
END
IF EXISTS (SELECT 1 FROM dbo.PickingOrders WHERE CreatedBy = @Wer)
BEGIN
    RAISERROR(N'Altbestand wurde bereits ueberfuehrt (Auftraege mit CreatedBy = Migration-KO-Altbestand vorhanden). NICHT ERNEUT AUSFUEHREN. Abbruch.', 16, 1);
    RETURN;
END
DECLARE @Standard INT = (SELECT TOP 1 Id FROM dbo.PickingAreas WHERE IsDefault = 1 AND IsActive = 1);
IF @Standard IS NULL
BEGIN
    RAISERROR(N'Kein aktiver Standard-Kommissionierbereich (SQL/133-Seed). Abbruch, nichts geaendert.', 16, 1);
    RETURN;
END

IF OBJECT_ID('tempdb..#Fa') IS NOT NULL DROP TABLE #Fa;
SELECT p.ProductionOrderId,
       MIN(ps.ReleasedAt) AS ReleasedAt,
       SUM(CASE WHEN p.IsTransferred = 1 THEN 1 ELSE 0 END) AS Umgebucht,
       SUM(CASE WHEN p.IsPicked = 1 AND p.IsTransferred = 0 THEN 1 ELSE 0 END) AS NurGepickt,
       MAX(CASE WHEN po.IsDone = 1 OR ISNULL(ps.IsDonePicking, 0) = 1 THEN 1 ELSE 0 END) AS FaErledigt,
       MAX(CASE WHEN po.Quantity <> 1 THEN 1 ELSE 0 END) AS MengeUngleich1
INTO #Fa
FROM dbo.PickingItems p
JOIN dbo.ProductionOrders po ON po.Id = p.ProductionOrderId
LEFT JOIN dbo.ProductionOrderPickingStatus ps ON ps.ProductionOrderId = p.ProductionOrderId
WHERE p.IsPicked = 1 OR p.IsTransferred = 1
GROUP BY p.ProductionOrderId;

PRINT N'--- Zaehlung (Trockenlauf und Ausfuehrung)';
SELECT COUNT(*) AS FAsMitFortschritt,
       SUM(CASE WHEN FaErledigt = 1 OR NurGepickt = 0 THEN 1 ELSE 0 END) AS davonAbgeschlossen,
       SUM(CASE WHEN FaErledigt = 0 AND NurGepickt > 0 THEN 1 ELSE 0 END) AS davonInArbeit,
       SUM(Umgebucht) AS ZeilenUmgebucht,
       SUM(NurGepickt) AS ZeilenGepickt,
       SUM(MengeUngleich1) AS FAsMitMengeUngleich1_B1
FROM #Fa;
SELECT COUNT(*) AS FreigegebenOffenOhneFortschritt_FuerNachziehenKnopf
FROM dbo.ProductionOrderPickingStatus ps JOIN dbo.ProductionOrders po ON po.Id = ps.ProductionOrderId
WHERE ps.IsReleasedForPicking = 1 AND ps.IsDonePicking = 0 AND po.IsDone = 0 AND po.IsCancelled = 0
  AND NOT EXISTS (SELECT 1 FROM #Fa f WHERE f.ProductionOrderId = ps.ProductionOrderId);

PRINT N'--- Liste „Altbestand pruefen“ (offen, Auftragsmenge <> 1: kein automatischer Rest-Auftrag, Leitstand bestaetigt je FA)';
SELECT po.OrderNumber AS FA, po.Quantity AS Auftragsmenge, f.Umgebucht AS ZeilenUmgebucht, f.NurGepickt AS ZeilenNurGepickt
FROM #Fa f JOIN dbo.ProductionOrders po ON po.Id = f.ProductionOrderId
WHERE f.FaErledigt = 0 AND f.MengeUngleich1 = 1
ORDER BY po.OrderNumber;

IF @Trockenlauf = 1
BEGIN
    PRINT N'TROCKENLAUF: nichts geaendert. Ausfuehren mit @Trockenlauf = 0 (vorher DB-Backup, Zaehlung freigeben).';
    RETURN;
END

BEGIN TRANSACTION;
    DECLARE @Max INT = ISNULL((SELECT MAX(LaufendeNummer) FROM dbo.PickingOrders WITH (UPDLOCK, HOLDLOCK) WHERE Jahr = @Jahr), 0);

    INSERT INTO dbo.PickingOrders (Nummer, Jahr, LaufendeNummer, Typ, ProductionOrderId, PickingAreaId, Status, PickingPriority,
                                   AssignedPickerId, AssignedPickerName, Bemerkung, AbgeschlossenAm, AbgeschlossenVon, AltbestandPruefen,
                                   CreatedAt, CreatedBy, CreatedByWindows)
    SELECT CONCAT(N'KO-', @Jahr, N'-', RIGHT(CONCAT(N'00000', n.Lfd), 5)), @Jahr, n.Lfd, 1, f.ProductionOrderId, @Standard,
           CASE WHEN f.FaErledigt = 1 OR f.NurGepickt = 0 THEN 40 ELSE 20 END,
           ps.PickingPriority, ps.AssignedPickerId, ps.AssignedPickerName, N'Altbestand vor v1.88.0',
           CASE WHEN f.FaErledigt = 1 OR f.NurGepickt = 0 THEN GETDATE() END,
           CASE WHEN f.FaErledigt = 1 OR f.NurGepickt = 0 THEN @Wer END,
           CASE WHEN f.FaErledigt = 0 AND f.MengeUngleich1 = 1 THEN 1 ELSE 0 END,
           GETDATE(), @Wer, LEFT(SYSTEM_USER, 200)
    FROM #Fa f
    JOIN (SELECT ProductionOrderId, @Max + ROW_NUMBER() OVER (ORDER BY ReleasedAt, ProductionOrderId) AS Lfd FROM #Fa) n
         ON n.ProductionOrderId = f.ProductionOrderId
    LEFT JOIN dbo.ProductionOrderPickingStatus ps ON ps.ProductionOrderId = f.ProductionOrderId;
    PRINT CONCAT(N'Auftraege angelegt: ', @@ROWCOUNT);

    INSERT INTO dbo.PickingOrderLines (PickingOrderId, ProductionOrderId, ArticleNumber, BomPosition, IsBaugruppe, SollMenge, IstMenge,
                                       SourceStorageLocationId, Status, PickedAt, PickedBy, PickedByWindows, TransferredAt, StockMovementId,
                                       CreatedAt, CreatedBy, CreatedByWindows)
    SELECT o.Id, p.ProductionOrderId, p.BomArticleNumber, p.BomPosition, p.IsBaugruppe, p.Quantity,
           CASE WHEN p.IsTransferred = 1 THEN p.Quantity END,
           p.SourceStorageLocationId,
           CASE WHEN p.IsTransferred = 1 THEN 20 ELSE 10 END,
           p.PickedAt, p.PickedBy, p.PickedByWindows, p.TransferredAt, NULL,
           GETDATE(), @Wer, LEFT(SYSTEM_USER, 200)
    FROM dbo.PickingItems p
    JOIN dbo.PickingOrders o ON o.ProductionOrderId = p.ProductionOrderId AND o.CreatedBy = @Wer
    WHERE p.IsPicked = 1 OR p.IsTransferred = 1;
    PRINT CONCAT(N'Zeilen angelegt: ', @@ROWCOUNT);
COMMIT TRANSACTION;

PRINT N'--- Ergebnis je Status';
SELECT Status, COUNT(*) AS Auftraege, SUM(CASE WHEN AltbestandPruefen = 1 THEN 1 ELSE 0 END) AS davonAltbestandPruefen
FROM dbo.PickingOrders WHERE CreatedBy = @Wer GROUP BY Status;

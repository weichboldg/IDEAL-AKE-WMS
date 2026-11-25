-- =============================================================================
-- 168_AddPickingStatusStuecklisteKleiner.sql  (v1.99.70, Spec 2026-11-25-kommissionierauftraege-nachtrag-nachts)
-- dbo.ProductionOrderPickingStatus + StuecklisteKleiner NVARCHAR(1000) NULL (Marker „Stueckliste kleiner als offene
-- Kommissionierzeilen“, Leitstand-Badge; einziger Schreiber PickingOrderWorkflow.ErzeugeOffeneAsync).
-- Additiv, nullable, nicht daten-destruktiv, kein Backfill (den Marker setzt der erste Lauf „Kommissionier-Nachtrag“).
-- SQL/AgentJobs nicht betroffen (kein Pflichtfeld, kein MERGE auf ProductionOrderPickingStatus).
-- Objektnamen wie die EF-Migration 20261125161741_AddPickingStatusStuecklisteKleiner (Tabelle heisst SINGULAR ProductionOrderPickingStatus).
-- Idempotent (COL_LENGTH-/OBJECT_ID-Guard), DDL im eigenen Batch, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.ProductionOrderPickingStatus', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.ProductionOrderPickingStatus fehlt - falsche Datenbank? Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.ProductionOrderPickingStatus', N'StuecklisteKleiner') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductionOrderPickingStatus] ADD [StuecklisteKleiner] NVARCHAR(1000) NULL;
    PRINT 'Spalte ProductionOrderPickingStatus.StuecklisteKleiner angelegt.';
END
GO

IF COL_LENGTH(N'dbo.ProductionOrderPickingStatus', N'StuecklisteKleiner') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261125161741_AddPickingStatusStuecklisteKleiner')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261125161741_AddPickingStatusStuecklisteKleiner', '10.0.2');
    PRINT 'Migration 20261125161741_AddPickingStatusStuecklisteKleiner eingetragen.';
END
GO

SET NOEXEC OFF;
GO

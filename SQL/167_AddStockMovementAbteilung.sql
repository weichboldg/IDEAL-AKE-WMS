-- =============================================================================
-- 167_AddStockMovementAbteilung.sql  (v1.99.68, Spec 2026-11-24-sage-buchung-referenz-benutzer-abteilung)
-- dbo.StockMovements + Abteilung NVARCHAR(200) NULL (Werkbank-Name bei Ausbuchung / Lagerplatz ausbuchen; geht als Referenz an Sage).
-- Additiv, nullable, nicht daten-destruktiv, kein Backfill. SQL/AgentJobs nicht betroffen (kein Pflichtfeld, kein MERGE auf StockMovements).
-- Objektnamen wie die EF-Migration 20261125134408_AddStockMovementAbteilung.
-- Idempotent (COL_LENGTH-/OBJECT_ID-Guard), DDL im eigenen Batch, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.StockMovements', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.StockMovements fehlt - falsche Datenbank? Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.StockMovements', N'Abteilung') IS NULL
BEGIN
    ALTER TABLE [dbo].[StockMovements] ADD [Abteilung] NVARCHAR(200) NULL;
    PRINT 'Spalte StockMovements.Abteilung angelegt.';
END
GO

IF COL_LENGTH(N'dbo.StockMovements', N'Abteilung') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261125134408_AddStockMovementAbteilung')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261125134408_AddStockMovementAbteilung', '10.0.2');
    PRINT 'Migration 20261125134408_AddStockMovementAbteilung eingetragen.';
END
GO

SET NOEXEC OFF;
GO

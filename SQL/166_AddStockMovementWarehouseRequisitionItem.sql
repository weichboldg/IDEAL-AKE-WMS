-- =============================================================================
-- 166_AddStockMovementWarehouseRequisitionItem.sql  (v1.99.53, Spec 2026-11-23-lagerbestellung-verbrauchsbuchung-bei-lieferung)
-- dbo.StockMovements + WarehouseRequisitionItemId INT NULL (Lieferung einer Lager-/Glasbestellposition = Ausbuchung beim Abschliessen)
-- + FK auf dbo.WarehouseRequisitionItems (ON DELETE NO ACTION) + Index.
-- Additiv, nicht daten-destruktiv, keine Datenmigration (Altlieferungen bleiben ungebucht). SQL/AgentJobs nicht betroffen.
-- Objektnamen wie die EF-Migration 20261123124036_AddStockMovementWarehouseRequisitionItem.
-- Idempotent (COL_LENGTH-/OBJECT_ID-/sys.indexes-Guard), DDL im eigenen Batch, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.StockMovements', N'U') IS NULL OR OBJECT_ID(N'dbo.WarehouseRequisitionItems', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.StockMovements oder dbo.WarehouseRequisitionItems fehlt - falsche Datenbank? Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.StockMovements', N'WarehouseRequisitionItemId') IS NULL
BEGIN
    ALTER TABLE [dbo].[StockMovements] ADD [WarehouseRequisitionItemId] INT NULL;
    PRINT 'Spalte StockMovements.WarehouseRequisitionItemId angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StockMovements_WarehouseRequisitionItemId' AND object_id = OBJECT_ID(N'dbo.StockMovements'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_StockMovements_WarehouseRequisitionItemId] ON [dbo].[StockMovements]([WarehouseRequisitionItemId]);
    PRINT 'Index IX_StockMovements_WarehouseRequisitionItemId angelegt.';
END
GO

IF OBJECT_ID(N'[dbo].[FK_StockMovements_WarehouseRequisitionItems_WarehouseRequisitionItemId]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[StockMovements] ADD CONSTRAINT [FK_StockMovements_WarehouseRequisitionItems_WarehouseRequisitionItemId]
        FOREIGN KEY ([WarehouseRequisitionItemId]) REFERENCES [dbo].[WarehouseRequisitionItems]([Id]);
    PRINT 'FK FK_StockMovements_WarehouseRequisitionItems_WarehouseRequisitionItemId angelegt.';
END
GO

IF COL_LENGTH(N'dbo.StockMovements', N'WarehouseRequisitionItemId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261123124036_AddStockMovementWarehouseRequisitionItem')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261123124036_AddStockMovementWarehouseRequisitionItem', '10.0.2');
    PRINT 'Migration 20261123124036_AddStockMovementWarehouseRequisitionItem eingetragen.';
END
GO

SET NOEXEC OFF;
GO

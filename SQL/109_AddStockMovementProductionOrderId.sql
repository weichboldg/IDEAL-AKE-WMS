-- =============================================================================
-- 109_AddStockMovementProductionOrderId.sql  (v1.87.0, Spec 2026-11-08-lagerbuchungen-fa-feld-und-verlinkung, Etappe 1)
-- Verknuepfter FA an der Lagerbewegung: StockMovements.ProductionOrderId (FK auf ProductionOrders, ON DELETE SET NULL) + Index.
-- Der Text StockMovements.ProductionOrder bleibt unveraendert. Rein additiv, keine Datenaenderung (Altdaten: SQL/110).
-- Objektnamen wie die EF-Migration 20261107190419_AddStockMovementProductionOrderId. Idempotent, DDL je Batch, History separat.
-- Danach Web UND Dienst neu publizieren (gemeinsames EF-Modell, LagerbestandSyncService schreibt StockMovements).
-- =============================================================================
SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.StockMovements', N'ProductionOrderId') IS NULL
    ALTER TABLE [dbo].[StockMovements] ADD [ProductionOrderId] INT NULL;
GO

IF OBJECT_ID(N'[dbo].[FK_StockMovements_ProductionOrders_ProductionOrderId]', N'F') IS NULL
    ALTER TABLE [dbo].[StockMovements] ADD CONSTRAINT [FK_StockMovements_ProductionOrders_ProductionOrderId]
        FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders]([Id]) ON DELETE SET NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StockMovements_ProductionOrderId' AND object_id = OBJECT_ID(N'dbo.StockMovements'))
    CREATE NONCLUSTERED INDEX [IX_StockMovements_ProductionOrderId] ON [dbo].[StockMovements]([ProductionOrderId]);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261107190419_AddStockMovementProductionOrderId')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261107190419_AddStockMovementProductionOrderId', '10.0.2');
    PRINT 'Migration 20261107190419_AddStockMovementProductionOrderId eingetragen.';
END
GO

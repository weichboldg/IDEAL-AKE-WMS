-- =============================================================================
-- 151_RepairFreshInstallIndexes.sql  (v1.99.63, Spec 2026-11-14-sicherheit-anmeldung-und-installer, Rueckfrage 4 / P-2b; Entscheid Gerald 2026-11-24)
-- NUR IDEAL-Prod (Erstinstallation 2026-11-04 mit altem 00_FreshInstall.sql: Meldung 207 verwarf den ganzen Abschnitt 15).
-- Legt die Indizes/FK aus Abschnitt 15 an, die dort fehlen - Namen und Definition wie 00_FreshInstall.sql Abschnitt 15.
-- Ohne IX_StockMovements_ArticleId (abgedeckt durch IX_StockMovements_ArticleId_StorageLocationId) und IX_WorkstationUsers_WorkstationId
-- (abgedeckt durch UQ_WorkstationUsers) - keine Duplikate.
-- Auf upgegradeten DBs (EF-Namen) NICHT laufen lassen: das Update-Skript startet es nur ueber Pruefung/FreshInstallIndizes (System Prod, IDEAL).
-- Doppelte Werte in den UNIQUE-Spalten prueft vorher Pruefung/FreshInstallIndizesDuplikate (Abbruch vor dem Stoppen).
-- Additiv, nicht daten-destruktiv, keine Migration (kein History-Eintrag). Idempotent, je Anweisung ein Batch.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StockMovements_StorageLocationId')
    CREATE NONCLUSTERED INDEX [IX_StockMovements_StorageLocationId] ON [dbo].[StockMovements]([StorageLocationId]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StockMovements_Timestamp')
    CREATE NONCLUSTERED INDEX [IX_StockMovements_Timestamp] ON [dbo].[StockMovements]([Timestamp]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StockMovements_SourceStorageLocationId')
    CREATE NONCLUSTERED INDEX [IX_StockMovements_SourceStorageLocationId] ON [dbo].[StockMovements]([SourceStorageLocationId]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StockMovements_ProductionOrderId')
    CREATE NONCLUSTERED INDEX [IX_StockMovements_ProductionOrderId] ON [dbo].[StockMovements]([ProductionOrderId]);
GO
IF OBJECT_ID(N'[dbo].[FK_StockMovements_ProductionOrders_ProductionOrderId]', N'F') IS NULL
    ALTER TABLE [dbo].[StockMovements] ADD CONSTRAINT [FK_StockMovements_ProductionOrders_ProductionOrderId]
        FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders]([Id]) ON DELETE SET NULL;
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SageBookingQueueItems_Status')
    CREATE NONCLUSTERED INDEX [IX_SageBookingQueueItems_Status] ON [dbo].[SageBookingQueueItems]([Status]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SageBookingQueueItems_StockMovementId')
    CREATE UNIQUE NONCLUSTERED INDEX [IX_SageBookingQueueItems_StockMovementId] ON [dbo].[SageBookingQueueItems]([StockMovementId]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StorageLocations_IsActive')
    CREATE NONCLUSTERED INDEX [IX_StorageLocations_IsActive] ON [dbo].[StorageLocations]([IsActive]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StorageLocations_Source')
    CREATE NONCLUSTERED INDEX [IX_StorageLocations_Source] ON [dbo].[StorageLocations]([Source]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StorageLocations_IstBuchbar')
    CREATE NONCLUSTERED INDEX [IX_StorageLocations_IstBuchbar] ON [dbo].[StorageLocations]([IstBuchbar]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StorageLocations_StorageAreaId')
   AND COL_LENGTH(N'dbo.StorageLocations', N'StorageAreaId') IS NOT NULL -- sonst steht SQL/98 noch aus
    CREATE NONCLUSTERED INDEX [IX_StorageLocations_StorageAreaId] ON [dbo].[StorageLocations]([StorageAreaId]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StorageAreas_Lagerkennung')
    CREATE UNIQUE NONCLUSTERED INDEX [IX_StorageAreas_Lagerkennung] ON [dbo].[StorageAreas]([Lagerkennung]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_ArticleSageDetails_ArticleId')
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_ArticleSageDetails_ArticleId] ON [dbo].[ArticleSageDetails]([ArticleId]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SyncLogs_Timestamp_Desc')
    CREATE NONCLUSTERED INDEX [IX_SyncLogs_Timestamp_Desc] ON [dbo].[SyncLogs]([Timestamp] DESC);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SyncLogs_Service_Level')
    CREATE NONCLUSTERED INDEX [IX_SyncLogs_Service_Level] ON [dbo].[SyncLogs]([Service], [Level]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkstationUsers_UserId')
    CREATE NONCLUSTERED INDEX [IX_WorkstationUsers_UserId] ON [dbo].[WorkstationUsers]([UserId]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_OrderNumber')
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_OrderNumber] ON [dbo].[ProductionOrders]([OrderNumber]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_SubOrderNumber')
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ProductionOrders_SubOrderNumber] ON [dbo].[ProductionOrders]([SubOrderNumber]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_ArticleNumber')
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_ArticleNumber] ON [dbo].[ProductionOrders]([ArticleNumber]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_IsDone')
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_IsDone] ON [dbo].[ProductionOrders]([IsDone]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_IsCancelled')
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_IsCancelled] ON [dbo].[ProductionOrders]([IsCancelled]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_ProductionWorkplaceId')
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_ProductionWorkplaceId] ON [dbo].[ProductionOrders]([ProductionWorkplaceId]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PickingItems_ProductionOrderId')
    CREATE NONCLUSTERED INDEX [IX_PickingItems_ProductionOrderId] ON [dbo].[PickingItems]([ProductionOrderId]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PickingItems_POId_IsPicked')
    CREATE NONCLUSTERED INDEX [IX_PickingItems_POId_IsPicked] ON [dbo].[PickingItems]([ProductionOrderId], [IsPicked]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionWorkplaceUsers_ProductionWorkplaceId_UserId')
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ProductionWorkplaceUsers_ProductionWorkplaceId_UserId] ON [dbo].[ProductionWorkplaceUsers]([ProductionWorkplaceId], [UserId]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionWorkplaceUsers_UserId')
    CREATE NONCLUSTERED INDEX [IX_ProductionWorkplaceUsers_UserId] ON [dbo].[ProductionWorkplaceUsers]([UserId]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkOperations_ProductionOrderId')
    CREATE NONCLUSTERED INDEX [IX_WorkOperations_ProductionOrderId] ON [dbo].[WorkOperations]([ProductionOrderId]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkOperations_ProductionOrderId_Sequence')
    CREATE NONCLUSTERED INDEX [IX_WorkOperations_ProductionOrderId_Sequence] ON [dbo].[WorkOperations]([ProductionOrderId], [Sequence]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkOperations_ProductionWorkplaceId')
    CREATE NONCLUSTERED INDEX [IX_WorkOperations_ProductionWorkplaceId] ON [dbo].[WorkOperations]([ProductionWorkplaceId]);
GO

PRINT N'SQL/151: Indizes/FK aus Abschnitt 15 vorhanden.';
GO

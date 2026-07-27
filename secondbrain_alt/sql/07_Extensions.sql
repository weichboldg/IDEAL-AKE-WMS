USE [IDEAL_AKE_WMS]
GO

-- =============================================
-- 07_Extensions.sql
-- Umbuchung (SourceStorageLocationId) + PickingItems
-- =============================================

-- 1a. SourceStorageLocationId Spalte + FK
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('StockMovements')
    AND name = 'SourceStorageLocationId')
BEGIN
    ALTER TABLE [dbo].[StockMovements]
        ADD [SourceStorageLocationId] INT NULL;

    ALTER TABLE [dbo].[StockMovements]
        ADD CONSTRAINT [FK_StockMovements_SourceStorageLocation]
        FOREIGN KEY ([SourceStorageLocationId])
        REFERENCES [dbo].[StorageLocations]([Id]);

    PRINT 'SourceStorageLocationId zu StockMovements hinzugefügt.';
END
GO

-- 1b. Index separat (Spalte muss bereits existieren)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('StockMovements')
    AND name = 'IX_StockMovements_SourceStorageLocationId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_StockMovements_SourceStorageLocationId]
        ON [dbo].[StockMovements]([SourceStorageLocationId])
        WHERE [SourceStorageLocationId] IS NOT NULL;

    PRINT 'Index IX_StockMovements_SourceStorageLocationId erstellt.';
END
GO

-- 2. PickingItems Tabelle
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PickingItems')
BEGIN
    CREATE TABLE [dbo].[PickingItems] (
        [Id]                       INT            IDENTITY(1,1) NOT NULL,
        [ProductionOrderId]        INT            NOT NULL,
        [BomArticleNumber]         NVARCHAR(100)  NOT NULL,
        [BomPosition]              NVARCHAR(50)   NULL,
        [Quantity]                 DECIMAL(18,3)  NOT NULL DEFAULT 0,
        [SourceStorageLocationId]  INT            NULL,
        [IsPicked]                 BIT            NOT NULL DEFAULT 0,
        [PickedAt]                 DATETIME2      NULL,
        [PickedBy]                 NVARCHAR(200)  NULL,
        [PickedByWindows]          NVARCHAR(200)  NULL,
        [IsTransferred]            BIT            NOT NULL DEFAULT 0,
        [TransferredAt]            DATETIME2      NULL,
        [CreatedAt]                DATETIME2      NOT NULL DEFAULT GETDATE(),
        [CreatedBy]                NVARCHAR(200)  NOT NULL,
        [CreatedByWindows]         NVARCHAR(200)  NOT NULL,
        [ModifiedAt]               DATETIME2      NULL,
        [ModifiedBy]               NVARCHAR(200)  NULL,
        [ModifiedByWindows]        NVARCHAR(200)  NULL,
        CONSTRAINT [PK_PickingItems] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_PickingItems_ProductionOrder]
            FOREIGN KEY ([ProductionOrderId])
            REFERENCES [dbo].[ProductionOrders]([Id]),
        CONSTRAINT [FK_PickingItems_SourceStorageLocation]
            FOREIGN KEY ([SourceStorageLocationId])
            REFERENCES [dbo].[StorageLocations]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_PickingItems_ProductionOrderId]
        ON [dbo].[PickingItems]([ProductionOrderId]);

    CREATE NONCLUSTERED INDEX [IX_PickingItems_POId_IsPicked]
        ON [dbo].[PickingItems]([ProductionOrderId], [IsPicked]);

    PRINT 'PickingItems Tabelle erstellt.';
END
GO

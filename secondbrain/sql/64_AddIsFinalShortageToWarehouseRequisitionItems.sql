-- =============================================
-- 64_AddIsFinalShortageToWarehouseRequisitionItems
-- Adds IsFinalShortage BIT NOT NULL DEFAULT 0 + filtered index.
-- Used by v1.18.0 Lagerbestellungen-Erweiterung.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE Name = N'IsFinalShortage'
               AND Object_ID = Object_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    ALTER TABLE [dbo].[WarehouseRequisitionItems]
        ADD [IsFinalShortage] BIT NOT NULL
            CONSTRAINT DF_WarehouseRequisitionItems_IsFinalShortage DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_WarehouseRequisitionItems_IsFinalShortage'
               AND object_id = OBJECT_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    CREATE INDEX IX_WarehouseRequisitionItems_IsFinalShortage
        ON [dbo].[WarehouseRequisitionItems]([IsFinalShortage])
        WHERE [IsFinalShortage] = 1;
END
GO

-- EF Migration History wird vom App-Startup geschrieben falls dieses Skript manuell laeuft.

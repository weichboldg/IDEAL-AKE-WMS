-- =============================================
-- 65_ReplaceIsFinalShortageWithShortageStatus
-- Replace IsFinalShortage BIT with ShortageStatus TINYINT enum.
-- Data conversion preserves status semantics of existing orders.
-- Used by v1.19.0.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE Name = N'ShortageStatus'
               AND Object_ID = Object_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    ALTER TABLE [dbo].[WarehouseRequisitionItems]
        ADD [ShortageStatus] TINYINT NOT NULL
            CONSTRAINT DF_WarehouseRequisitionItems_ShortageStatus DEFAULT 0;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE Name = N'IsFinalShortage'
           AND Object_ID = Object_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    UPDATE [dbo].[WarehouseRequisitionItems]
    SET [ShortageStatus] = CASE
        WHEN [IsFinalShortage] = 1 THEN 2
        WHEN ([QuantityPicked] IS NULL OR [QuantityPicked] < [QuantityRequested]) THEN 1
        ELSE 0
    END
    WHERE [ShortageStatus] = 0;
END
GO

IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = N'IX_WarehouseRequisitionItems_IsFinalShortage'
           AND object_id = OBJECT_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    DROP INDEX IX_WarehouseRequisitionItems_IsFinalShortage
        ON [dbo].[WarehouseRequisitionItems];
END
GO

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE Name = N'IsFinalShortage'
           AND Object_ID = Object_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    DECLARE @c NVARCHAR(200) = (
        SELECT dc.name FROM sys.default_constraints dc
        INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id
            AND dc.parent_column_id = c.column_id
        WHERE c.object_id = OBJECT_ID('[dbo].[WarehouseRequisitionItems]')
          AND c.name = 'IsFinalShortage');
    IF @c IS NOT NULL EXEC('ALTER TABLE [dbo].[WarehouseRequisitionItems] DROP CONSTRAINT [' + @c + ']');

    ALTER TABLE [dbo].[WarehouseRequisitionItems] DROP COLUMN [IsFinalShortage];
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_WarehouseRequisitionItems_ShortageStatus_WillBeRestocked'
               AND object_id = OBJECT_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    CREATE INDEX IX_WarehouseRequisitionItems_ShortageStatus_WillBeRestocked
        ON [dbo].[WarehouseRequisitionItems]([ShortageStatus])
        WHERE [ShortageStatus] = 1;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_WarehouseRequisitionItems_ShortageStatus_NoRestock'
               AND object_id = OBJECT_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    CREATE INDEX IX_WarehouseRequisitionItems_ShortageStatus_NoRestock
        ON [dbo].[WarehouseRequisitionItems]([ShortageStatus])
        WHERE [ShortageStatus] = 2;
END
GO

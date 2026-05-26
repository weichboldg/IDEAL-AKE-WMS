-- Phase: Lagerbestellung — stabiler User-Filter
-- Idempotent: CreatedByUserId-Spalte + Index in WarehouseRequisitions hinzufuegen.

IF COL_LENGTH('dbo.WarehouseRequisitions', 'CreatedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.WarehouseRequisitions
        ADD CreatedByUserId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_WarehouseRequisitions_CreatedByUserId'
      AND object_id = OBJECT_ID('dbo.WarehouseRequisitions'))
BEGIN
    CREATE INDEX IX_WarehouseRequisitions_CreatedByUserId
        ON dbo.WarehouseRequisitions(CreatedByUserId);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260502172901_AddWarehouseRequisitionCreatedByUserId')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260502172901_AddWarehouseRequisitionCreatedByUserId', '10.0.2');
END
GO

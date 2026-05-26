-- SQL/63_AddWarehouseRequisitionItemNote.sql
-- Notiz-Spalte pro Position einer Lagerbestellung.
-- Wird vom Lagermitarbeiter im Detail-Screen erfasst und auf dem Druck angezeigt.

IF COL_LENGTH('dbo.WarehouseRequisitionItems', 'Note') IS NULL
BEGIN
    ALTER TABLE dbo.WarehouseRequisitionItems
        ADD Note NVARCHAR(500) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260522070823_AddWarehouseRequisitionItemNote')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    SELECT '20260522070823_AddWarehouseRequisitionItemNote', '10.0.2';
END
GO

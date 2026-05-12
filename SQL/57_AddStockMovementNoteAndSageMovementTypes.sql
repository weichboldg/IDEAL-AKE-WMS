-- SQL/57_AddStockMovementNoteAndSageMovementTypes.sql
-- Phase: Sage Lagerbestand-Sync — neues Note-Feld auf StockMovements.
-- MovementType-Erweiterung (3=SageEinbuchung, 4=SageAusbuchung) ist nur C#-Enum-Erweiterung,
-- die DB-Spalte (int) braucht keine Schema-Aenderung.

IF COL_LENGTH('dbo.StockMovements', 'Note') IS NULL
BEGIN
    ALTER TABLE dbo.StockMovements
        ADD Note NVARCHAR(500) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260506134758_AddStockMovementNoteAndSageMovementTypes')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    SELECT '20260506134758_AddStockMovementNoteAndSageMovementTypes', '10.0.2';
END
GO

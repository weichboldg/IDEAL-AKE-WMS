-- SQL/58_AddStorageLocationIstBuchbar.sql
-- Phase-2-Erweiterung: User-gesteuertes IstBuchbar-Flag.
-- Default 1 (alle bestehenden Manual-Plaetze bleiben buchbar).
-- Initial-UPDATE setzt existing Sage-Records auf 0 — NUR beim ersten Migrations-Lauf,
-- geschuetzt durch Migrations-History-Guard.

IF COL_LENGTH('dbo.StorageLocations', 'IstBuchbar') IS NULL
BEGIN
    ALTER TABLE dbo.StorageLocations
        ADD IstBuchbar BIT NOT NULL CONSTRAINT DF_StorageLocations_IstBuchbar DEFAULT 1;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_StorageLocations_IstBuchbar'
      AND object_id = OBJECT_ID('dbo.StorageLocations'))
BEGIN
    CREATE INDEX IX_StorageLocations_IstBuchbar ON dbo.StorageLocations(IstBuchbar);
END
GO

-- Initial-Setup: existing Sage-Records auf 0 — NUR beim ersten Lauf.
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260507133624_AddStorageLocationIstBuchbar')
BEGIN
    UPDATE dbo.StorageLocations SET IstBuchbar = 0 WHERE Source = 'Sage';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260507133624_AddStorageLocationIstBuchbar')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    SELECT '20260507133624_AddStorageLocationIstBuchbar', '10.0.2';
END
GO

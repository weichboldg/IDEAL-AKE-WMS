-- SQL/55_AddStorageLocationSyncFields.sql
-- Phase: Sage Lagerplatz-Sync — Source + IsActive auf StorageLocations
-- Idempotent: COL_LENGTH-Guard pro Spalte.

IF COL_LENGTH('dbo.StorageLocations', 'Source') IS NULL
BEGIN
    ALTER TABLE dbo.StorageLocations
        ADD Source NVARCHAR(20) NOT NULL CONSTRAINT DF_StorageLocations_Source DEFAULT 'Manual';
END
GO

IF COL_LENGTH('dbo.StorageLocations', 'IsActive') IS NULL
BEGIN
    ALTER TABLE dbo.StorageLocations
        ADD IsActive BIT NOT NULL CONSTRAINT DF_StorageLocations_IsActive DEFAULT 1;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_StorageLocations_IsActive'
      AND object_id = OBJECT_ID('dbo.StorageLocations'))
BEGIN
    CREATE INDEX IX_StorageLocations_IsActive ON dbo.StorageLocations(IsActive);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_StorageLocations_Source'
      AND object_id = OBJECT_ID('dbo.StorageLocations'))
BEGIN
    CREATE INDEX IX_StorageLocations_Source ON dbo.StorageLocations(Source);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId LIKE '%_AddStorageLocationSyncFields')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    SELECT '20260506053444_AddStorageLocationSyncFields', '10.0.2';
END
GO

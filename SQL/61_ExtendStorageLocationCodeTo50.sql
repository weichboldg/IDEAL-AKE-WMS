-- SQL/61_ExtendStorageLocationCodeTo50.sql
-- Verbreitert dbo.StorageLocations.Code von NVARCHAR(12) auf NVARCHAR(50).
-- Sage liefert Lagerplatz-Codes mit 13+ Zeichen, die der LagerplatzSyncService
-- bisher per Warning uebersprungen hat.
-- Idempotent: prueft aktuellen max_length vor ALTER (sys.columns speichert
-- max_length in Bytes — 24 = NVARCHAR(12), 100 = NVARCHAR(50)).

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.StorageLocations')
      AND name = 'Code'
      AND max_length < 100
)
BEGIN
    -- UNIQUE-Index muss vor ALTER COLUMN abgehaengt werden.
    IF EXISTS (SELECT 1 FROM sys.indexes
        WHERE name = 'IX_StorageLocations_Code'
          AND object_id = OBJECT_ID('dbo.StorageLocations'))
    BEGIN
        DROP INDEX [IX_StorageLocations_Code] ON [dbo].[StorageLocations];
    END

    IF EXISTS (SELECT 1 FROM sys.indexes
        WHERE name = 'UQ_StorageLocations_Code'
          AND object_id = OBJECT_ID('dbo.StorageLocations'))
    BEGIN
        ALTER TABLE [dbo].[StorageLocations] DROP CONSTRAINT [UQ_StorageLocations_Code];
    END

    ALTER TABLE [dbo].[StorageLocations]
        ALTER COLUMN [Code] NVARCHAR(50) NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_StorageLocations_Code'
      AND object_id = OBJECT_ID('dbo.StorageLocations'))
BEGIN
    CREATE UNIQUE INDEX [IX_StorageLocations_Code]
        ON [dbo].[StorageLocations]([Code]);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260521141926_ExtendStorageLocationCodeTo50')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    SELECT '20260521141926_ExtendStorageLocationCodeTo50', '10.0.2';
END
GO

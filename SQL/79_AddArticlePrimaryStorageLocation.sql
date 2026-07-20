-- SQL/79_AddArticlePrimaryStorageLocation.sql
-- Migration 79: AddArticlePrimaryStorageLocation (v1.25.0)
-- Hauptlagerplatz am Artikel: PrimaryStorageLocationId (FK -> StorageLocations, SetNull)
-- + SagePrimaryStorageLocation (Sage-Rohcode). Additiv, idempotent.
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.Articles', 'PrimaryStorageLocationId') IS NULL
BEGIN
    ALTER TABLE dbo.Articles ADD PrimaryStorageLocationId INT NULL;
    PRINT 'Spalte [Articles].[PrimaryStorageLocationId] erstellt.';
END
GO

IF COL_LENGTH('dbo.Articles', 'SagePrimaryStorageLocation') IS NULL
BEGIN
    ALTER TABLE dbo.Articles ADD SagePrimaryStorageLocation NVARCHAR(100) NULL;
    PRINT 'Spalte [Articles].[SagePrimaryStorageLocation] erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Articles_PrimaryStorageLocationId'
      AND object_id = OBJECT_ID('dbo.Articles'))
BEGIN
    CREATE INDEX IX_Articles_PrimaryStorageLocationId
        ON dbo.Articles(PrimaryStorageLocationId);
    PRINT 'Index IX_Articles_PrimaryStorageLocationId erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Articles_StorageLocations_PrimaryStorageLocationId'
      AND parent_object_id = OBJECT_ID('dbo.Articles'))
BEGIN
    ALTER TABLE dbo.Articles
        ADD CONSTRAINT FK_Articles_StorageLocations_PrimaryStorageLocationId
        FOREIGN KEY (PrimaryStorageLocationId)
        REFERENCES dbo.StorageLocations(Id) ON DELETE SET NULL;
    PRINT 'FK FK_Articles_StorageLocations_PrimaryStorageLocationId erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260707131400_AddArticlePrimaryStorageLocation')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260707131400_AddArticlePrimaryStorageLocation', '10.0.2');
END
GO

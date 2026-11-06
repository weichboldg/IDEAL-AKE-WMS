-- =============================================================================
-- 98_AddStorageAreasAndStorageLocationSageFields.sql  (v1.73.0, Spec 2026-11-06-sage-sdata-stammdaten-sync, Etappe E1)
-- Sage-Lagerorte als eigene Tabelle [StorageAreas] (UNIQUE Lagerkennung, Audit-Felder) und Sage-Felder aus SData an
-- [StorageLocations]: StorageAreaId (FK ON DELETE SET NULL + Index), SageIstGesperrt, SageIstGesperrtInventur,
-- SageBreite/SageHoehe/SageLaenge/SageVolumen/SageTragkraft (DECIMAL(18,3), Genauigkeit P-S1 offen).
-- Rein additiv, alle neuen Spalten NULL, keine Bestandsbefuellung: befuellt nur der Lagerplatz-Abgleich mit
-- Sync:Quelle:Lagerplaetze = SData. Nicht daten-destruktiv; DB-Backup vor Deploy wie ueblich.
-- Objektnamen wie die EF-Migration 20261106181707_AddStorageAreasAndStorageLocationSageFields.
-- Idempotent: zweiter Lauf aendert nichts. DDL je in eigenem Batch, History-Insert in separatem Batch.
-- =============================================================================
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'[dbo].[StorageLocations]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: Tabelle dbo.StorageLocations fehlt - zuerst SQL/00_FreshInstall.sql ausfuehren. Keine Aenderung, keine History-Zeile.';
    SET NOEXEC ON;
END
GO

IF OBJECT_ID(N'[dbo].[StorageAreas]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StorageAreas] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [Lagerkennung]      NVARCHAR(10)      NOT NULL,
        [Bezeichnung]       NVARCHAR(40)      NULL,
        [Lagertechnik]      NVARCHAR(10)      NULL,
        [IstDispoLager]     BIT               NOT NULL,
        [IsActive]          BIT               NOT NULL CONSTRAINT [DF_StorageAreas_IsActive] DEFAULT 1,
        [SageMissingSince]  DATETIME2         NULL,
        [CreatedAt]         DATETIME2         NOT NULL,
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_StorageAreas] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT 'Tabelle StorageAreas erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StorageAreas_Lagerkennung' AND object_id = OBJECT_ID('dbo.StorageAreas'))
BEGIN
    CREATE UNIQUE INDEX [IX_StorageAreas_Lagerkennung] ON [dbo].[StorageAreas] ([Lagerkennung]);
    PRINT 'Index IX_StorageAreas_Lagerkennung angelegt.';
END
GO

IF COL_LENGTH(N'dbo.StorageLocations', N'StorageAreaId') IS NULL
BEGIN
    ALTER TABLE [dbo].[StorageLocations] ADD [StorageAreaId] INT NULL;
    PRINT 'Spalte StorageLocations.StorageAreaId hinzugefuegt.';
END
GO

IF COL_LENGTH(N'dbo.StorageLocations', N'SageIstGesperrt') IS NULL
    ALTER TABLE [dbo].[StorageLocations] ADD [SageIstGesperrt] BIT NULL;
GO

IF COL_LENGTH(N'dbo.StorageLocations', N'SageIstGesperrtInventur') IS NULL
    ALTER TABLE [dbo].[StorageLocations] ADD [SageIstGesperrtInventur] BIT NULL;
GO

IF COL_LENGTH(N'dbo.StorageLocations', N'SageBreite') IS NULL
    ALTER TABLE [dbo].[StorageLocations] ADD [SageBreite] DECIMAL(18,3) NULL;
GO

IF COL_LENGTH(N'dbo.StorageLocations', N'SageHoehe') IS NULL
    ALTER TABLE [dbo].[StorageLocations] ADD [SageHoehe] DECIMAL(18,3) NULL;
GO

IF COL_LENGTH(N'dbo.StorageLocations', N'SageLaenge') IS NULL
    ALTER TABLE [dbo].[StorageLocations] ADD [SageLaenge] DECIMAL(18,3) NULL;
GO

IF COL_LENGTH(N'dbo.StorageLocations', N'SageVolumen') IS NULL
    ALTER TABLE [dbo].[StorageLocations] ADD [SageVolumen] DECIMAL(18,3) NULL;
GO

IF COL_LENGTH(N'dbo.StorageLocations', N'SageTragkraft') IS NULL
    ALTER TABLE [dbo].[StorageLocations] ADD [SageTragkraft] DECIMAL(18,3) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StorageLocations_StorageAreaId' AND object_id = OBJECT_ID('dbo.StorageLocations'))
BEGIN
    CREATE INDEX [IX_StorageLocations_StorageAreaId] ON [dbo].[StorageLocations] ([StorageAreaId]);
    PRINT 'Index IX_StorageLocations_StorageAreaId angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_StorageLocations_StorageAreas_StorageAreaId' AND parent_object_id = OBJECT_ID('dbo.StorageLocations'))
BEGIN
    ALTER TABLE [dbo].[StorageLocations] ADD CONSTRAINT [FK_StorageLocations_StorageAreas_StorageAreaId]
        FOREIGN KEY ([StorageAreaId]) REFERENCES [dbo].[StorageAreas]([Id]) ON DELETE SET NULL;
    PRINT 'FK FK_StorageLocations_StorageAreas_StorageAreaId angelegt.';
END
GO

-- Migrations-History (separater Batch, exakte MigrationId)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261106181707_AddStorageAreasAndStorageLocationSageFields')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261106181707_AddStorageAreasAndStorageLocationSageFields', '10.0.2');
    PRINT 'Migration 20261106181707_AddStorageAreasAndStorageLocationSageFields eingetragen.';
END
GO

SET NOEXEC OFF;
GO

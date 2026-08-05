-- SQL/82_AddStorageLocationSageLagerbuchung.sql
-- Migration 82: AddStorageLocationSageLagerbuchung (Sage-Lagerbuchungen, Step 1)
-- Rein additiv, idempotent: drei neue Spalten auf StorageLocations fuer die
-- ausgehende Sage-Lagerbuchung (WMS -> Sage via SData).
--   SageBuchungErlaubt : BIT NOT NULL DEFAULT 0  (user-controlled Opt-in)
--   SageLagerkennung   : NVARCHAR(50) NULL       (volle Kurzbezeichnung = Code)
--   SageLagerplatzId   : INT NULL                (KHKLagerplaetze.PlatzID)
-- DDL in eigenem Batch, __EFMigrationsHistory-Insert in separatem Batch.
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.StorageLocations', 'SageBuchungErlaubt') IS NULL
BEGIN
    ALTER TABLE [dbo].[StorageLocations]
        ADD [SageBuchungErlaubt] BIT NOT NULL CONSTRAINT [DF_StorageLocations_SageBuchungErlaubt] DEFAULT 0;
    PRINT 'Spalte StorageLocations.SageBuchungErlaubt hinzugefuegt.';
END
GO

IF COL_LENGTH('dbo.StorageLocations', 'SageLagerkennung') IS NULL
BEGIN
    ALTER TABLE [dbo].[StorageLocations] ADD [SageLagerkennung] NVARCHAR(50) NULL;
    PRINT 'Spalte StorageLocations.SageLagerkennung hinzugefuegt.';
END
GO

IF COL_LENGTH('dbo.StorageLocations', 'SageLagerplatzId') IS NULL
BEGIN
    ALTER TABLE [dbo].[StorageLocations] ADD [SageLagerplatzId] INT NULL;
    PRINT 'Spalte StorageLocations.SageLagerplatzId hinzugefuegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260803104055_AddStorageLocationSageLagerbuchung')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260803104055_AddStorageLocationSageLagerbuchung', '10.0.2');
END
GO

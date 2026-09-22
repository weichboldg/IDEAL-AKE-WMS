-- SQL/93_AddProductionWorkplaceSageFields.sql
-- Migration 93: AddProductionWorkplaceSageFields (IDEAL Baustein a, v1.44.0)
-- Rein additiv, idempotent: zwei neue Spalten auf ProductionWorkplaces fuer die
-- Werkbank-Anlage aus den Sage-Arbeitsplaetzen + eindeutiger, auf IS NOT NULL gefilterter
-- Index auf dem Verknuepfungsschluessel (B6: verhindert Doppelanlage).
--   SageArbeitsplatznummer : NVARCHAR(31) NULL  (Verknuepfungsschluessel)
--   ArbeitsschrittCode     : NVARCHAR(20) NULL  (Match-Feld Baustein b)
-- DDL je in eigenem Batch, __EFMigrationsHistory-Insert in separatem Batch.
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.ProductionWorkplaces', 'SageArbeitsplatznummer') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductionWorkplaces] ADD [SageArbeitsplatznummer] NVARCHAR(31) NULL;
    PRINT 'Spalte ProductionWorkplaces.SageArbeitsplatznummer hinzugefuegt.';
END
GO

IF COL_LENGTH('dbo.ProductionWorkplaces', 'ArbeitsschrittCode') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductionWorkplaces] ADD [ArbeitsschrittCode] NVARCHAR(20) NULL;
    PRINT 'Spalte ProductionWorkplaces.ArbeitsschrittCode hinzugefuegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ProductionWorkplaces_SageArbeitsplatznummer'
      AND object_id = OBJECT_ID('dbo.ProductionWorkplaces'))
BEGIN
    CREATE UNIQUE INDEX [IX_ProductionWorkplaces_SageArbeitsplatznummer]
        ON [dbo].[ProductionWorkplaces] ([SageArbeitsplatznummer])
        WHERE [SageArbeitsplatznummer] IS NOT NULL;
    PRINT 'Index IX_ProductionWorkplaces_SageArbeitsplatznummer angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260922083259_AddProductionWorkplaceSageFields')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260922083259_AddProductionWorkplaceSageFields', '10.0.2');
END
GO

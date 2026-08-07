-- SQL/84_AddUserDefaultFilterFaWorklistDescription1.sql
-- Migration 84: AddUserDefaultFilterFaWorklistDescription1 (Teil-8)
-- Rein additiv, idempotent: eine neue Spalte auf Users fuer den personalisierten
-- Standard-Spaltenfilter „Bezeichnung 1" der FA-Abarbeitungsliste.
--   DefaultFilterFaWorklistDescription1 : NVARCHAR(200) NULL
-- DDL in eigenem Batch, __EFMigrationsHistory-Insert in separatem Batch.
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.Users', 'DefaultFilterFaWorklistDescription1') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [DefaultFilterFaWorklistDescription1] NVARCHAR(200) NULL;
    PRINT 'Spalte Users.DefaultFilterFaWorklistDescription1 hinzugefuegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260806081121_AddUserDefaultFilterFaWorklistDescription1')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260806081121_AddUserDefaultFilterFaWorklistDescription1', '10.0.2');
END
GO

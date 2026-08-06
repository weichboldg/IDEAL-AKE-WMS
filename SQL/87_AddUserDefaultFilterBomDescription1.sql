-- SQL/87_AddUserDefaultFilterBomDescription1.sql
-- Migration 87: AddUserDefaultFilterBomDescription1 (Teil-8-Nachtrag)
-- Rein additiv, idempotent: eine neue Spalte auf Users fuer den personalisierten
-- Standard-Spaltenfilter „Bezeichnung 1" der Stueckliste (BOM, Client-Mode).
--   DefaultFilterBomDescription1 : NVARCHAR(200) NULL
-- DDL in eigenem Batch, __EFMigrationsHistory-Insert in separatem Batch.
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.Users', 'DefaultFilterBomDescription1') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [DefaultFilterBomDescription1] NVARCHAR(200) NULL;
    PRINT 'Spalte Users.DefaultFilterBomDescription1 hinzugefuegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260806105617_AddUserDefaultFilterBomDescription1')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260806105617_AddUserDefaultFilterBomDescription1', '10.0.2');
END
GO

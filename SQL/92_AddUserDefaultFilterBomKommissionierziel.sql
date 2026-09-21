-- SQL/92_AddUserDefaultFilterBomKommissionierziel.sql
-- Migration 92: AddUserDefaultFilterBomKommissionierziel
-- Rein additiv, idempotent: eine neue Spalte auf Users fuer den personalisierten
-- Standard-Spaltenfilter „Komm.-Ziel" der Stueckliste (BOM, Client-Mode, nur hierarchischer Modus).
--   DefaultFilterBomKommissionierziel : NVARCHAR(200) NULL
-- DDL in eigenem Batch, __EFMigrationsHistory-Insert in separatem Batch.
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.Users', 'DefaultFilterBomKommissionierziel') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [DefaultFilterBomKommissionierziel] NVARCHAR(200) NULL;
    PRINT 'Spalte Users.DefaultFilterBomKommissionierziel hinzugefuegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260921131329_AddUserDefaultFilterBomKommissionierziel')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260921131329_AddUserDefaultFilterBomKommissionierziel', '10.0.2');
END
GO

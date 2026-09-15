-- SQL/91_AddArticleMatchcode.sql
-- Migration 91: AddArticleMatchcode
-- Rein additiv, idempotent: neue Spalte Articles.Matchcode (NVARCHAR(200), NULL).
-- Ersetzt den frueheren (nie deployten) Versuch ProductionOrders.Matchcode (Migration
-- 20260910081155_AddProductionOrderMatchcode, zurueckgebaut) — der Matchcode lebt jetzt im
-- Artikelstamm statt auf der FA-Zeile.
-- DDL in eigenem Batch, __EFMigrationsHistory-Insert in separatem Batch.
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.Articles', 'Matchcode') IS NULL
BEGIN
    ALTER TABLE [dbo].[Articles] ADD [Matchcode] NVARCHAR(200) NULL;
    PRINT 'Spalte Articles.Matchcode hinzugefuegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260915094646_AddArticleMatchcode')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260915094646_AddArticleMatchcode', '10.0.2');
END
GO

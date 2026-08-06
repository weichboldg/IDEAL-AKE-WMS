-- SQL/85_AddWarehouseRequisitionComment.sql
-- Migration 85: AddWarehouseRequisitionComment (Teil-7)
-- Rein additiv, idempotent: eine neue Spalte auf WarehouseRequisitions fuer den
-- Freitext-Kommentar auf Kopf-Ebene der Bestellung.
--   Comment : NVARCHAR(1000) NULL
-- DDL in eigenem Batch, __EFMigrationsHistory-Insert in separatem Batch.
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.WarehouseRequisitions', 'Comment') IS NULL
BEGIN
    ALTER TABLE [dbo].[WarehouseRequisitions] ADD [Comment] NVARCHAR(1000) NULL;
    PRINT 'Spalte WarehouseRequisitions.Comment hinzugefuegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260806081737_AddWarehouseRequisitionComment')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260806081737_AddWarehouseRequisitionComment', '10.0.2');
END
GO

-- SQL/91_AddProductionOrderMatchcode.sql
-- Migration 91: AddProductionOrderMatchcode
-- Rein additiv, idempotent: neue Spalte ProductionOrders.Matchcode (NVARCHAR(200), NULL).
-- Laenge spiegelt die Quelle FaHierarchyNodes.Matchcode — keine Kuerzung, kein Datenverlust.
-- Bestandszeilen bleiben NULL und fuellen sich beim naechsten Materialisierungs-Lauf.
-- DDL in eigenem Batch, __EFMigrationsHistory-Insert in separatem Batch.
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.ProductionOrders', 'Matchcode') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [Matchcode] NVARCHAR(200) NULL;
    PRINT 'Spalte ProductionOrders.Matchcode hinzugefuegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260910081155_AddProductionOrderMatchcode')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260910081155_AddProductionOrderMatchcode', '10.0.2');
END
GO

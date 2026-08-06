-- SQL/88_AllowMultipleDummyRequisitionItems.sql
-- Migration 88: AllowMultipleDummyRequisitionItems (Teil-7 UAT-Fix)
-- Der Unique-Index (WarehouseRequisitionId, ArticleNumber) wird zu einem GEFILTERTEN
-- Unique-Index umgebaut, der den reservierten DUMMY-Schluessel ausnimmt. Damit sind
-- mehrere DUMMY-Positionen (je eigene Bezeichnung) in einer Bestellung erlaubt, waehrend
-- normale Artikel weiterhin je Bestellung eindeutig bleiben.
-- Idempotent: Index (gefiltert oder ungefiltert) droppen, dann gefiltert neu anlegen.
SET NOCOUNT ON;
GO

IF EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_WarehouseRequisitionItems_WarehouseRequisitionId_ArticleNumber'
      AND object_id = OBJECT_ID('dbo.WarehouseRequisitionItems'))
BEGIN
    DROP INDEX [IX_WarehouseRequisitionItems_WarehouseRequisitionId_ArticleNumber]
        ON [dbo].[WarehouseRequisitionItems];
    PRINT 'Alter Unique-Index auf WarehouseRequisitionItems entfernt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_WarehouseRequisitionItems_WarehouseRequisitionId_ArticleNumber'
      AND object_id = OBJECT_ID('dbo.WarehouseRequisitionItems'))
BEGIN
    CREATE UNIQUE INDEX [IX_WarehouseRequisitionItems_WarehouseRequisitionId_ArticleNumber]
        ON [dbo].[WarehouseRequisitionItems] ([WarehouseRequisitionId], [ArticleNumber])
        WHERE [ArticleNumber] <> 'DUMMY';
    PRINT 'Gefilterter Unique-Index (ohne DUMMY) auf WarehouseRequisitionItems angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260806120650_AllowMultipleDummyRequisitionItems')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260806120650_AllowMultipleDummyRequisitionItems', '10.0.2');
END
GO

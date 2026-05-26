-- Performance-Indexes fuer StockMovements
-- Optimiert die haeufigsten Abfrage-Patterns: Bestandsberechnung, FA-Filter, Umbuchungs-Quell-Abfragen

-- Composite Index: Bestandsberechnung (GetCurrentStockAsync, GetStockByArticleNumbersAsync)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_ArticleId_StorageLocationId' AND object_id = OBJECT_ID('StockMovements'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_StockMovements_ArticleId_StorageLocationId]
    ON [StockMovements] ([ArticleId], [StorageLocationId])
    INCLUDE ([Quantity], [MovementType])
END
GO

-- Composite Index: Umbuchungs-Quell-Abfragen (source deduction in stock calculation)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_ArticleId_SourceStorageLocationId_MovementType' AND object_id = OBJECT_ID('StockMovements'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_StockMovements_ArticleId_SourceStorageLocationId_MovementType]
    ON [StockMovements] ([ArticleId], [SourceStorageLocationId], [MovementType])
    INCLUDE ([Quantity])
END
GO

-- Index: FA-Nummer fuer Bestandsuebersicht FA-Filter und Bewegungshistorie
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_ProductionOrder' AND object_id = OBJECT_ID('StockMovements'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_StockMovements_ProductionOrder]
    ON [StockMovements] ([ProductionOrder])
END
GO

-- EF Migrations History
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260404095516_StockMovementPerformanceIndexes')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260404095516_StockMovementPerformanceIndexes', '10.0.0')
END
GO

-- Phase: OSEON Tracking Article Filter Fix v1.7.2
-- Idempotent: Index auf ArticleNumber für Filter-Performance.

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_OseonProductionOrders_ArticleNumber'
      AND object_id = OBJECT_ID('dbo.OseonProductionOrders'))
BEGIN
    CREATE INDEX IX_OseonProductionOrders_ArticleNumber
        ON dbo.OseonProductionOrders(ArticleNumber);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId LIKE '%_AddOseonArticleNumberIndex')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260430134516_AddOseonArticleNumberIndex', '10.0.2');
END
GO

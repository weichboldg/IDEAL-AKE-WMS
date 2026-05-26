-- 30_OseonPerformanceIndexes.sql
-- Zusätzliche Indizes für OSEON-Teileverfolgung Performance

-- Index auf OseonStatus (Filter offene/fertige)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'OseonProductionOrders') AND name = N'IX_OseonProductionOrders_OseonStatus')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OseonProductionOrders_OseonStatus]
    ON [OseonProductionOrders] ([OseonStatus])
    INCLUDE ([CustomerOrderNumber], [OseonOrderNumber]);
END
GO

-- Index auf WorkplaceName (Werkbank-Filter)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'OseonProductionOrders') AND name = N'IX_OseonProductionOrders_WorkplaceName')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OseonProductionOrders_WorkplaceName]
    ON [OseonProductionOrders] ([WorkplaceName])
    INCLUDE ([CustomerOrderNumber], [OseonStatus]);
END
GO

-- EF Migration History
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260318000001_OseonPerformanceIndexes')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260318000001_OseonPerformanceIndexes', '10.0.0');
END
GO

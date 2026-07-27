-- ============================================================
-- 31_AddOseonTimestamps.sql
-- OSEON-Änderungstimestamps für Delta-Sync
-- pa.DateOfLastChange → OseonProductionOrders.LastChangedInOseon
-- aga.LetzteStatusMeldung → OseonWorkOperations.LastStatusReportInOseon
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('OseonProductionOrders') AND name = 'LastChangedInOseon')
    ALTER TABLE [dbo].[OseonProductionOrders] ADD [LastChangedInOseon] DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('OseonWorkOperations') AND name = 'LastStatusReportInOseon')
    ALTER TABLE [dbo].[OseonWorkOperations] ADD [LastStatusReportInOseon] DATETIME2 NULL;
GO

-- EF Migrations History
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260318150710_AddOseonTimestamps')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260318150710_AddOseonTimestamps', '10.0.2');
GO

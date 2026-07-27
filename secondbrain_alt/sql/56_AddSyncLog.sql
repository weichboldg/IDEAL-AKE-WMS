-- SQL/56_AddSyncLog.sql
-- Phase: Sage Lagerplatz-Sync — service-uebergreifendes Sync-Protokoll.

IF OBJECT_ID('dbo.SyncLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SyncLogs (
        Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SyncLogs PRIMARY KEY,
        Timestamp DATETIME2 NOT NULL CONSTRAINT DF_SyncLogs_Timestamp DEFAULT SYSDATETIME(),
        Service   NVARCHAR(50)  NOT NULL,
        Level     NVARCHAR(10)  NOT NULL,
        Message   NVARCHAR(1000) NOT NULL,
        Reference NVARCHAR(100)  NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_SyncLogs_Timestamp_Desc'
      AND object_id = OBJECT_ID('dbo.SyncLogs'))
BEGIN
    CREATE INDEX IX_SyncLogs_Timestamp_Desc ON dbo.SyncLogs([Timestamp] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_SyncLogs_Service_Level'
      AND object_id = OBJECT_ID('dbo.SyncLogs'))
BEGIN
    CREATE INDEX IX_SyncLogs_Service_Level ON dbo.SyncLogs([Service], [Level]);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260506060206_AddSyncLog')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    SELECT '20260506060206_AddSyncLog', '10.0.2';
END
GO

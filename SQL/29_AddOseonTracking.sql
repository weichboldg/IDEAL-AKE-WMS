-- =====================================================
-- 29: OSEON Teileverfolgung - Tabellen + AppSettings
-- =====================================================

-- OseonProductionOrders
IF OBJECT_ID(N'[dbo].[OseonProductionOrders]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OseonProductionOrders] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [OseonId] BIGINT NOT NULL DEFAULT 0,
        [OseonOrderNumber] NVARCHAR(100) NOT NULL,
        [CustomerOrderNumber] NVARCHAR(100) NULL,
        [OseonStatus] INT NOT NULL DEFAULT 0,
        [ArticleNumber] NVARCHAR(100) NULL,
        [Description1] NVARCHAR(500) NULL,
        [Description2] NVARCHAR(500) NULL,
        [WorkplaceName] NVARCHAR(200) NULL,
        [ProductionWorkplaceId] INT NULL,
        [QuantityTarget] DECIMAL(18,3) NOT NULL DEFAULT 0,
        [QuantityActual] DECIMAL(18,3) NOT NULL DEFAULT 0,
        [DueDate] DATE NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [CreatedBy] NVARCHAR(200) NOT NULL,
        [CreatedByWindows] NVARCHAR(200) NOT NULL,
        [ModifiedAt] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [ModifiedByWindows] NVARCHAR(200) NULL,
        CONSTRAINT [PK_OseonProductionOrders] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_OseonProductionOrders_ProductionWorkplaces_ProductionWorkplaceId]
            FOREIGN KEY ([ProductionWorkplaceId]) REFERENCES [dbo].[ProductionWorkplaces]([Id]) ON DELETE SET NULL
    );
    PRINT 'Tabelle OseonProductionOrders erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OseonProductionOrders_OseonOrderNumber')
    CREATE UNIQUE INDEX [IX_OseonProductionOrders_OseonOrderNumber] ON [dbo].[OseonProductionOrders]([OseonOrderNumber]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OseonProductionOrders_CustomerOrderNumber')
    CREATE INDEX [IX_OseonProductionOrders_CustomerOrderNumber] ON [dbo].[OseonProductionOrders]([CustomerOrderNumber]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OseonProductionOrders_OseonId')
    CREATE INDEX [IX_OseonProductionOrders_OseonId] ON [dbo].[OseonProductionOrders]([OseonId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OseonProductionOrders_ProductionWorkplaceId')
    CREATE INDEX [IX_OseonProductionOrders_ProductionWorkplaceId] ON [dbo].[OseonProductionOrders]([ProductionWorkplaceId]);
GO

-- OseonWorkOperations
IF OBJECT_ID(N'[dbo].[OseonWorkOperations]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OseonWorkOperations] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [OseonProductionOrderId] INT NOT NULL,
        [PositionNumber] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [OseonStatus] INT NOT NULL DEFAULT 0,
        [IsFirstOperation] BIT NOT NULL DEFAULT 0,
        [IsLastOperation] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL,
        [CreatedBy] NVARCHAR(200) NOT NULL,
        [CreatedByWindows] NVARCHAR(200) NOT NULL,
        [ModifiedAt] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [ModifiedByWindows] NVARCHAR(200) NULL,
        CONSTRAINT [PK_OseonWorkOperations] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_OseonWorkOperations_OseonProductionOrders_OseonProductionOrderId]
            FOREIGN KEY ([OseonProductionOrderId]) REFERENCES [dbo].[OseonProductionOrders]([Id]) ON DELETE CASCADE
    );
    PRINT 'Tabelle OseonWorkOperations erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OseonWorkOperations_OseonProductionOrderId')
    CREATE INDEX [IX_OseonWorkOperations_OseonProductionOrderId] ON [dbo].[OseonWorkOperations]([OseonProductionOrderId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OseonWorkOperations_OseonProductionOrderId_PositionNumber')
    CREATE UNIQUE INDEX [IX_OseonWorkOperations_OseonProductionOrderId_PositionNumber] ON [dbo].[OseonWorkOperations]([OseonProductionOrderId], [PositionNumber]);
GO

-- AppSettings für Ampel-Konfiguration
IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = 'OseonAmpelGelbTage')
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES ('OseonAmpelGelbTage', '1', 'OSEON Ampel: Gelb ab X Tagen vor Termin');
GO
IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = 'OseonAmpelBlauTage')
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES ('OseonAmpelBlauTage', '2', 'OSEON Ampel: Blau ab X Tagen vor Termin');
GO

-- EF Migrations History
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260317140041_AddOseonTracking')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260317140041_AddOseonTracking', '10.0.2');
GO

PRINT 'Migration 29_AddOseonTracking abgeschlossen.';
GO

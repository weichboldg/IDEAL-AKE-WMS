-- =============================================
-- IDEAL-AKE WMS - Konsolidiertes Neuinstallations-Script
-- Erstellt alle Tabellen, Views und Standarddaten im finalen Zustand.
-- Für bestehende Installationen die einzelnen Migrations-Scripts (01-21) verwenden.
-- =============================================

-- Datenbank erstellen
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'IDEAL_AKE_WMS')
BEGIN
    CREATE DATABASE [IDEAL_AKE_WMS]
    COLLATE Latin1_General_CI_AS;
    PRINT 'Datenbank IDEAL_AKE_WMS erstellt.';
END
GO

USE [IDEAL_AKE_WMS]
GO

-- =============================================
-- 1. Users
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id]                        INT IDENTITY(1,1) NOT NULL,
        [Name]                      NVARCHAR(200)     NOT NULL,
        [PersonalNumber]            NVARCHAR(50)      NULL,
        [PasswordHash]              NVARCHAR(500)     NULL,
        [IsActive]                  BIT               NOT NULL DEFAULT 1,
        [DefaultFilterBeschaffung]  NVARCHAR(100)     NULL,
        [DefaultFilterArtikelgruppe] NVARCHAR(100)    NULL,
        [HasMasterDataAccess]       BIT               NOT NULL DEFAULT 0,
        [CreatedAt]                 DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]                 NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]          NVARCHAR(200)     NOT NULL,
        [ModifiedAt]                DATETIME2         NULL,
        [ModifiedBy]                NVARCHAR(200)     NULL,
        [ModifiedByWindows]         NVARCHAR(200)     NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT 'Tabelle Users erstellt.';
END
GO

-- =============================================
-- 2. Workstations
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Workstations')
BEGIN
    CREATE TABLE [dbo].[Workstations] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [Name]              NVARCHAR(200)     NOT NULL,
        [Location]          NVARCHAR(200)     NULL,
        [DefaultUserId]     INT               NULL,
        [DefaultPrinter]    NVARCHAR(500)     NULL,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_Workstations] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Workstations_DefaultUser] FOREIGN KEY ([DefaultUserId]) REFERENCES [dbo].[Users]([Id])
    );
    PRINT 'Tabelle Workstations erstellt.';
END
GO

-- =============================================
-- 3. WorkstationUsers
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkstationUsers')
BEGIN
    CREATE TABLE [dbo].[WorkstationUsers] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [WorkstationId]     INT               NOT NULL,
        [UserId]            INT               NOT NULL,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_WorkstationUsers] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_WorkstationUsers_Workstation] FOREIGN KEY ([WorkstationId]) REFERENCES [dbo].[Workstations]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WorkstationUsers_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [UQ_WorkstationUsers] UNIQUE ([WorkstationId], [UserId])
    );
    PRINT 'Tabelle WorkstationUsers erstellt.';
END
GO

-- =============================================
-- 4. StorageLocations
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StorageLocations')
BEGIN
    CREATE TABLE [dbo].[StorageLocations] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [Code]              NVARCHAR(12)      NOT NULL,
        [Description]       NVARCHAR(200)     NULL,
        [Zone]              NVARCHAR(100)     NULL,
        [Capacity]          DECIMAL(18,2)     NULL,
        [BarcodeValue]      NVARCHAR(50)      NULL,
        [IsPickingTransport] BIT              NOT NULL DEFAULT 0,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_StorageLocations] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_StorageLocations_Code] UNIQUE ([Code])
    );
    PRINT 'Tabelle StorageLocations erstellt.';
END
GO

-- =============================================
-- 5. Articles
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Articles')
BEGIN
    CREATE TABLE [dbo].[Articles] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [ArticleNumber]     NVARCHAR(100)     NOT NULL,
        [Description]       NVARCHAR(500)     NULL,
        [Unit]              NVARCHAR(20)      NULL,
        [ReorderLevel]      DECIMAL(18,3)     NULL,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_Articles] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_Articles_ArticleNumber] UNIQUE ([ArticleNumber])
    );
    PRINT 'Tabelle Articles erstellt.';
END
GO

-- =============================================
-- 6. StockMovements
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StockMovements')
BEGIN
    CREATE TABLE [dbo].[StockMovements] (
        [Id]                      INT IDENTITY(1,1) NOT NULL,
        [ArticleId]               INT               NOT NULL,
        [Quantity]                DECIMAL(18,3)     NOT NULL,
        [StorageLocationId]       INT               NOT NULL,
        [SourceStorageLocationId] INT               NULL,
        [ProductionOrder]         NVARCHAR(500)     NULL,
        [MovementType]            INT               NOT NULL,  -- 0=Einbuchung, 1=Ausbuchung
        [Timestamp]               DATETIME2         NOT NULL DEFAULT GETDATE(),
        [UserId]                  INT               NULL,
        [WindowsUser]             NVARCHAR(200)     NOT NULL,
        [CreatedAt]               DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]               NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]        NVARCHAR(200)     NOT NULL,
        [ModifiedAt]              DATETIME2         NULL,
        [ModifiedBy]              NVARCHAR(200)     NULL,
        [ModifiedByWindows]       NVARCHAR(200)     NULL,
        CONSTRAINT [PK_StockMovements] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_StockMovements_Article] FOREIGN KEY ([ArticleId]) REFERENCES [dbo].[Articles]([Id]),
        CONSTRAINT [FK_StockMovements_StorageLocation] FOREIGN KEY ([StorageLocationId]) REFERENCES [dbo].[StorageLocations]([Id]),
        CONSTRAINT [FK_StockMovements_SourceStorageLocation] FOREIGN KEY ([SourceStorageLocationId]) REFERENCES [dbo].[StorageLocations]([Id]),
        CONSTRAINT [FK_StockMovements_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
    );
    PRINT 'Tabelle StockMovements erstellt.';
END
GO

-- =============================================
-- 7. ProductionOrders
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductionOrders')
BEGIN
    CREATE TABLE [dbo].[ProductionOrders] (
        [Id]                  INT IDENTITY(1,1) NOT NULL,
        [OrderNumber]         NVARCHAR(100)     NOT NULL,
        [Quantity]            DECIMAL(18,3)     NULL,
        [Customer]            NVARCHAR(500)     NULL,
        [ArticleNumber]       NVARCHAR(100)     NULL,
        [Description1]        NVARCHAR(500)     NULL,
        [Description2]        NVARCHAR(500)     NULL,
        [ProductionDate]      DATETIME2         NULL,
        [DeliveryDate]        DATETIME2         NULL,
        [IsDone]              BIT               NOT NULL DEFAULT 0,
        [PickingStatus]       NVARCHAR(50)      NULL,
        [HasGlass]            BIT               NOT NULL DEFAULT 0,
        [HasExternalPurchase] BIT               NOT NULL DEFAULT 0,
        [CreatedAt]           DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]           NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]    NVARCHAR(200)     NOT NULL,
        [ModifiedAt]          DATETIME2         NULL,
        [ModifiedBy]          NVARCHAR(200)     NULL,
        [ModifiedByWindows]   NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionOrders] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ProductionOrders_OrderNumber] UNIQUE ([OrderNumber])
    );
    PRINT 'Tabelle ProductionOrders erstellt.';
END
GO

-- =============================================
-- 8. PickingItems
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PickingItems')
BEGIN
    CREATE TABLE [dbo].[PickingItems] (
        [Id]                      INT IDENTITY(1,1) NOT NULL,
        [ProductionOrderId]       INT               NOT NULL,
        [BomArticleNumber]        NVARCHAR(100)     NOT NULL,
        [BomPosition]             NVARCHAR(50)      NULL,
        [Quantity]                DECIMAL(18,3)     NOT NULL DEFAULT 0,
        [SourceStorageLocationId] INT               NULL,
        [IsPicked]                BIT               NOT NULL DEFAULT 0,
        [PickedAt]                DATETIME2         NULL,
        [PickedBy]                NVARCHAR(200)     NULL,
        [PickedByWindows]         NVARCHAR(200)     NULL,
        [IsTransferred]           BIT               NOT NULL DEFAULT 0,
        [TransferredAt]           DATETIME2         NULL,
        [IsBaugruppe]             BIT               NOT NULL DEFAULT 0,
        [RowVersion]              ROWVERSION        NOT NULL,
        [CreatedAt]               DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]               NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]        NVARCHAR(200)     NOT NULL,
        [ModifiedAt]              DATETIME2         NULL,
        [ModifiedBy]              NVARCHAR(200)     NULL,
        [ModifiedByWindows]       NVARCHAR(200)     NULL,
        CONSTRAINT [PK_PickingItems] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_PickingItems_ProductionOrder] FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PickingItems_SourceStorageLocation] FOREIGN KEY ([SourceStorageLocationId]) REFERENCES [dbo].[StorageLocations]([Id])
    );
    PRINT 'Tabelle PickingItems erstellt.';
END
GO

-- =============================================
-- 9. AppSettings
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppSettings')
BEGIN
    CREATE TABLE [dbo].[AppSettings] (
        [Key]         NVARCHAR(100) NOT NULL,
        [Value]       NVARCHAR(500) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        CONSTRAINT [PK_AppSettings] PRIMARY KEY CLUSTERED ([Key])
    );
    PRINT 'Tabelle AppSettings erstellt.';

    -- Standard-Einstellungen
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description]) VALUES
        ('KommissionierTage', '4', 'Arbeitstage vor Fertigungstermin fuer Kommissionierung'),
        ('VorkommissionierTage', '1', 'Zusaetzliche Tage vor Kommissioniertermin'),
        ('BeschichtungTage', '10', 'Arbeitstage vor Kommissionierung fuer Beschichtung'),
        ('WarningThresholdPercent', '150', 'Meldebestand Warnschwelle in Prozent'),
        ('CriticalThresholdPercent', '100', 'Meldebestand kritische Schwelle in Prozent'),
        ('NegativeBuchungErlaubt', 'false', 'Negative Buchungen erlauben (true/false)'),
        ('NegativeBuchungLagerplatz', 'NAN', 'Fallback-Lagerplatz bei negativem Bestand'),
        ('StammdatenADGruppe', 'BDE_Stammdaten', 'AD-Gruppe fuer Stammdaten-Zugriff'),
        ('BeschichtungAbholtage', 'Dienstag,Donnerstag', 'Wochentage fuer Beschichtungs-Abholung');
    PRINT 'Standard-Einstellungen eingefuegt.';
END
GO

-- =============================================
-- 10. Holidays
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Holidays')
BEGIN
    CREATE TABLE [dbo].[Holidays] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [Date]              DATE              NOT NULL,
        [Description]       NVARCHAR(200)     NULL,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_Holidays] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_Holidays_Date] UNIQUE ([Date])
    );
    PRINT 'Tabelle Holidays erstellt.';
END
GO

-- =============================================
-- 11. Indexes
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StockMovements_ArticleId')
    CREATE NONCLUSTERED INDEX [IX_StockMovements_ArticleId] ON [dbo].[StockMovements]([ArticleId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StockMovements_StorageLocationId')
    CREATE NONCLUSTERED INDEX [IX_StockMovements_StorageLocationId] ON [dbo].[StockMovements]([StorageLocationId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StockMovements_Timestamp')
    CREATE NONCLUSTERED INDEX [IX_StockMovements_Timestamp] ON [dbo].[StockMovements]([Timestamp]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StockMovements_SourceStorageLocationId')
    CREATE NONCLUSTERED INDEX [IX_StockMovements_SourceStorageLocationId] ON [dbo].[StockMovements]([SourceStorageLocationId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkstationUsers_WorkstationId')
    CREATE NONCLUSTERED INDEX [IX_WorkstationUsers_WorkstationId] ON [dbo].[WorkstationUsers]([WorkstationId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkstationUsers_UserId')
    CREATE NONCLUSTERED INDEX [IX_WorkstationUsers_UserId] ON [dbo].[WorkstationUsers]([UserId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_OrderNumber')
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ProductionOrders_OrderNumber] ON [dbo].[ProductionOrders]([OrderNumber]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_ArticleNumber')
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_ArticleNumber] ON [dbo].[ProductionOrders]([ArticleNumber]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_IsDone')
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_IsDone] ON [dbo].[ProductionOrders]([IsDone]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PickingItems_ProductionOrderId')
    CREATE NONCLUSTERED INDEX [IX_PickingItems_ProductionOrderId] ON [dbo].[PickingItems]([ProductionOrderId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PickingItems_POId_IsPicked')
    CREATE NONCLUSTERED INDEX [IX_PickingItems_POId_IsPicked] ON [dbo].[PickingItems]([ProductionOrderId], [IsPicked]);
GO

PRINT 'Indexes erstellt.';
GO

-- =============================================
-- 12. Views
-- =============================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_CurrentStock')
    DROP VIEW [dbo].[vw_CurrentStock];
GO

CREATE VIEW [dbo].[vw_CurrentStock]
AS
SELECT
    a.Id AS ArticleId,
    a.ArticleNumber,
    a.Description AS ArticleDescription,
    a.Unit,
    sl.Id AS StorageLocationId,
    sl.Code AS StorageLocationCode,
    sl.Description AS StorageLocationDescription,
    sl.Zone,
    SUM(
        CASE
            WHEN sm.MovementType = 0 THEN sm.Quantity
            WHEN sm.MovementType = 1 THEN -sm.Quantity
            ELSE 0
        END
    ) AS CurrentQuantity
FROM [dbo].[StockMovements] sm
INNER JOIN [dbo].[Articles] a ON sm.ArticleId = a.Id
INNER JOIN [dbo].[StorageLocations] sl ON sm.StorageLocationId = sl.Id
GROUP BY
    a.Id, a.ArticleNumber, a.Description, a.Unit,
    sl.Id, sl.Code, sl.Description, sl.Zone;
GO

IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_MovementHistory')
    DROP VIEW [dbo].[vw_MovementHistory];
GO

CREATE VIEW [dbo].[vw_MovementHistory]
AS
SELECT
    sm.Id,
    sm.Timestamp,
    a.ArticleNumber,
    a.Description AS ArticleDescription,
    sm.Quantity,
    sl.Code AS StorageLocationCode,
    sl.Description AS StorageLocationDescription,
    CASE sm.MovementType
        WHEN 0 THEN 'Einbuchung'
        WHEN 1 THEN 'Ausbuchung'
    END AS MovementTypeName,
    sm.MovementType,
    u.Name AS UserName,
    sm.WindowsUser,
    sm.ProductionOrder,
    sm.CreatedAt
FROM [dbo].[StockMovements] sm
INNER JOIN [dbo].[Articles] a ON sm.ArticleId = a.Id
INNER JOIN [dbo].[StorageLocations] sl ON sm.StorageLocationId = sl.Id
LEFT JOIN [dbo].[Users] u ON sm.UserId = u.Id;
GO

PRINT 'Views erstellt.';
GO

-- =============================================
-- 13. EF Migrations History
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId]    NVARCHAR(150) NOT NULL,
        [ProductVersion] NVARCHAR(32)  NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED ([MigrationId])
    );
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260217074157_InitialCreate')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260217074157_InitialCreate', '10.0.0-preview.1.25081.3');
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260217083306_AddPickingItemRowVersion')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260217083306_AddPickingItemRowVersion', '10.0.0-preview.1.25081.3');
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260218084152_AddUserFieldsAndPickingScale')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260218084152_AddUserFieldsAndPickingScale', '10.0.0-preview.1.25081.3');
GO

PRINT 'EF Migrations History initialisiert.';
PRINT '====================================';
PRINT 'IDEAL-AKE WMS Installation abgeschlossen.';
GO

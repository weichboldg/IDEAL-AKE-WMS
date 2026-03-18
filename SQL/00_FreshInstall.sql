-- =============================================
-- IDEAL-AKE WMS - Konsolidiertes Neuinstallations-Script
-- Erstellt alle Tabellen, Views und Standarddaten im finalen Zustand.
-- Fuer bestehende Installationen die einzelnen Migrations-Scripts (01-28) verwenden.
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
        [RecursiveFilterSearch]     BIT               NOT NULL DEFAULT 0,
        [Email]                     NVARCHAR(200)     NULL,
        [IsAdmin]                   BIT               NOT NULL DEFAULT 0,
        [NotifyOnReorderLevel]      BIT               NOT NULL DEFAULT 0,
        [CanPick]                   BIT               NOT NULL DEFAULT 0,
        [CanViewTracking]           BIT               NOT NULL DEFAULT 0,
        [CanReportOperations]       BIT               NOT NULL DEFAULT 0,
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
        [ArticleGroup]      NVARCHAR(100)     NULL,
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
-- 7. ProductionWorkplaces
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductionWorkplaces')
BEGIN
    CREATE TABLE [dbo].[ProductionWorkplaces] (
        [Id]                     INT IDENTITY(1,1) NOT NULL,
        [Name]                   NVARCHAR(200)     NOT NULL,
        [Hall]                   NVARCHAR(200)     NULL,
        [OverridePrePickingDays] INT               NULL,
        [CreatedAt]              DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]              NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]       NVARCHAR(200)     NOT NULL,
        [ModifiedAt]             DATETIME2         NULL,
        [ModifiedBy]             NVARCHAR(200)     NULL,
        [ModifiedByWindows]      NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionWorkplaces] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT 'Tabelle ProductionWorkplaces erstellt.';
END
GO

-- =============================================
-- 8. ProductionOrders
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductionOrders')
BEGIN
    CREATE TABLE [dbo].[ProductionOrders] (
        [Id]                      INT IDENTITY(1,1) NOT NULL,
        [OrderNumber]             NVARCHAR(100)     NOT NULL,
        [Quantity]                DECIMAL(18,3)     NULL,
        [Customer]                NVARCHAR(500)     NULL,
        [ArticleNumber]           NVARCHAR(100)     NULL,
        [Description1]            NVARCHAR(500)     NULL,
        [Description2]            NVARCHAR(500)     NULL,
        [ProductionDate]          DATETIME2         NULL,
        [DeliveryDate]            DATETIME2         NULL,
        [IsDone]                  BIT               NOT NULL DEFAULT 0,
        [PickingStatus]           NVARCHAR(50)      NULL,
        [HasGlass]                BIT               NOT NULL DEFAULT 0,
        [HasExternalPurchase]     BIT               NOT NULL DEFAULT 0,
        [ProductionWorkplaceId]   INT               NULL,
        [CreatedAt]               DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]               NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]        NVARCHAR(200)     NOT NULL,
        [ModifiedAt]              DATETIME2         NULL,
        [ModifiedBy]              NVARCHAR(200)     NULL,
        [ModifiedByWindows]       NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionOrders] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ProductionOrders_OrderNumber] UNIQUE ([OrderNumber]),
        CONSTRAINT [FK_ProductionOrders_ProductionWorkplaces_ProductionWorkplaceId]
            FOREIGN KEY ([ProductionWorkplaceId]) REFERENCES [dbo].[ProductionWorkplaces]([Id]) ON DELETE SET NULL
    );
    PRINT 'Tabelle ProductionOrders erstellt.';
END
GO

-- =============================================
-- 9. PickingItems
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
-- 10. ProductionWorkplaceUsers
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductionWorkplaceUsers')
BEGIN
    CREATE TABLE [dbo].[ProductionWorkplaceUsers] (
        [Id]                       INT IDENTITY(1,1) NOT NULL,
        [ProductionWorkplaceId]    INT               NOT NULL,
        [UserId]                   INT               NOT NULL,
        [CreatedAt]                DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]                NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]         NVARCHAR(200)     NOT NULL,
        [ModifiedAt]               DATETIME2         NULL,
        [ModifiedBy]               NVARCHAR(200)     NULL,
        [ModifiedByWindows]        NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionWorkplaceUsers] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ProductionWorkplaceUsers_ProductionWorkplaces_ProductionWorkplaceId]
            FOREIGN KEY ([ProductionWorkplaceId]) REFERENCES [dbo].[ProductionWorkplaces]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductionWorkplaceUsers_Users_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION
    );
    PRINT 'Tabelle ProductionWorkplaceUsers erstellt.';
END
GO

-- =============================================
-- 11. WorkOperations
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkOperations')
BEGIN
    CREATE TABLE [dbo].[WorkOperations] (
        [Id]                       INT IDENTITY(1,1) NOT NULL,
        [ProductionOrderId]        INT               NOT NULL,
        [OperationNumber]          NVARCHAR(50)      NOT NULL,
        [Name]                     NVARCHAR(200)     NOT NULL,
        [ProductionWorkplaceId]    INT               NULL,
        [Sequence]                 INT               NOT NULL,
        [IsReportable]             BIT               NOT NULL,
        [IsExternalSystem]         BIT               NOT NULL,
        [IsReported]               BIT               NOT NULL,
        [ReportedAt]               DATETIME2         NULL,
        [ReportedBy]               NVARCHAR(200)     NULL,
        [ReportedByWindows]        NVARCHAR(200)     NULL,
        [ExternalSource]           NVARCHAR(100)     NULL,
        [CreatedAt]                DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]                NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]         NVARCHAR(200)     NOT NULL,
        [ModifiedAt]               DATETIME2         NULL,
        [ModifiedBy]               NVARCHAR(200)     NULL,
        [ModifiedByWindows]        NVARCHAR(200)     NULL,
        CONSTRAINT [PK_WorkOperations] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_WorkOperations_ProductionOrders_ProductionOrderId]
            FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WorkOperations_ProductionWorkplaces_ProductionWorkplaceId]
            FOREIGN KEY ([ProductionWorkplaceId]) REFERENCES [dbo].[ProductionWorkplaces]([Id]) ON DELETE SET NULL
    );
    PRINT 'Tabelle WorkOperations erstellt.';
END
GO

-- =============================================
-- 12. OseonProductionOrders
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OseonProductionOrders')
BEGIN
    CREATE TABLE [dbo].[OseonProductionOrders] (
        [Id]                      INT IDENTITY(1,1) NOT NULL,
        [OseonId]                 BIGINT NOT NULL DEFAULT 0,
        [OseonOrderNumber]        NVARCHAR(100) NOT NULL,
        [CustomerOrderNumber]     NVARCHAR(100) NULL,
        [OseonStatus]             INT NOT NULL DEFAULT 0,
        [ArticleNumber]           NVARCHAR(100) NULL,
        [Description1]            NVARCHAR(500) NULL,
        [Description2]            NVARCHAR(500) NULL,
        [WorkplaceName]           NVARCHAR(200) NULL,
        [ProductionWorkplaceId]   INT NULL,
        [QuantityTarget]          DECIMAL(18,3) NOT NULL DEFAULT 0,
        [QuantityActual]          DECIMAL(18,3) NOT NULL DEFAULT 0,
        [DueDate]                 DATE NULL,
        [CreatedAt]               DATETIME2 NOT NULL,
        [CreatedBy]               NVARCHAR(200) NOT NULL,
        [CreatedByWindows]        NVARCHAR(200) NOT NULL,
        [ModifiedAt]              DATETIME2 NULL,
        [ModifiedBy]              NVARCHAR(200) NULL,
        [ModifiedByWindows]       NVARCHAR(200) NULL,
        CONSTRAINT [PK_OseonProductionOrders] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_OseonProductionOrders_ProductionWorkplaces_ProductionWorkplaceId]
            FOREIGN KEY ([ProductionWorkplaceId]) REFERENCES [dbo].[ProductionWorkplaces]([Id]) ON DELETE SET NULL
    );
    CREATE UNIQUE INDEX [IX_OseonProductionOrders_OseonOrderNumber] ON [dbo].[OseonProductionOrders]([OseonOrderNumber]);
    CREATE INDEX [IX_OseonProductionOrders_CustomerOrderNumber] ON [dbo].[OseonProductionOrders]([CustomerOrderNumber]);
    CREATE INDEX [IX_OseonProductionOrders_OseonId] ON [dbo].[OseonProductionOrders]([OseonId]);
    CREATE INDEX [IX_OseonProductionOrders_ProductionWorkplaceId] ON [dbo].[OseonProductionOrders]([ProductionWorkplaceId]);
    CREATE INDEX [IX_OseonProductionOrders_OseonStatus] ON [dbo].[OseonProductionOrders]([OseonStatus]) INCLUDE ([CustomerOrderNumber], [OseonOrderNumber]);
    CREATE INDEX [IX_OseonProductionOrders_WorkplaceName] ON [dbo].[OseonProductionOrders]([WorkplaceName]) INCLUDE ([CustomerOrderNumber], [OseonStatus]);
    PRINT 'Tabelle OseonProductionOrders erstellt.';
END
GO

-- =============================================
-- 13. OseonWorkOperations
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OseonWorkOperations')
BEGIN
    CREATE TABLE [dbo].[OseonWorkOperations] (
        [Id]                        INT IDENTITY(1,1) NOT NULL,
        [OseonProductionOrderId]    INT NOT NULL,
        [PositionNumber]            NVARCHAR(50) NOT NULL,
        [Name]                      NVARCHAR(200) NOT NULL,
        [Description]               NVARCHAR(500) NULL,
        [OseonStatus]               INT NOT NULL DEFAULT 0,
        [IsFirstOperation]          BIT NOT NULL DEFAULT 0,
        [IsLastOperation]           BIT NOT NULL DEFAULT 0,
        [CreatedAt]                 DATETIME2 NOT NULL,
        [CreatedBy]                 NVARCHAR(200) NOT NULL,
        [CreatedByWindows]          NVARCHAR(200) NOT NULL,
        [ModifiedAt]                DATETIME2 NULL,
        [ModifiedBy]                NVARCHAR(200) NULL,
        [ModifiedByWindows]         NVARCHAR(200) NULL,
        CONSTRAINT [PK_OseonWorkOperations] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_OseonWorkOperations_OseonProductionOrders_OseonProductionOrderId]
            FOREIGN KEY ([OseonProductionOrderId]) REFERENCES [dbo].[OseonProductionOrders]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_OseonWorkOperations_OseonProductionOrderId] ON [dbo].[OseonWorkOperations]([OseonProductionOrderId]);
    CREATE UNIQUE INDEX [IX_OseonWorkOperations_OseonProductionOrderId_PositionNumber] ON [dbo].[OseonWorkOperations]([OseonProductionOrderId], [PositionNumber]);
    PRINT 'Tabelle OseonWorkOperations erstellt.';
END
GO

-- =============================================
-- 14. AppSettings
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
        ('BeschichtungAbholtage', 'Dienstag,Donnerstag', 'Wochentage fuer Beschichtungs-Abholung'),
        ('TeileverfolgungAktiv', 'false', 'Globaler Schalter: Teileverfolgungs-Modul aktiviert'),
        ('OseonRueckmeldungAktiv', 'false', 'Rueckmeldungen duerfen an Oseon zurueckgeschrieben werden'),
        ('SageRueckmeldungAktiv', 'false', 'Rueckmeldungen duerfen an Sage zurueckgeschrieben werden'),
        ('QrMitFaNummer', 'false', 'QR-Code enthaelt Fertigungsauftragsnummer an 3. Stelle'),
        ('OseonAmpelGelbTage', '1', 'OSEON Ampel: Gelb ab X Tagen vor Termin'),
        ('OseonAmpelBlauTage', '2', 'OSEON Ampel: Blau ab X Tagen vor Termin');
    PRINT 'Standard-Einstellungen eingefuegt.';
END
GO

-- =============================================
-- 13. ServiceSettings
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ServiceSettings')
BEGIN
    CREATE TABLE [dbo].[ServiceSettings] (
        [Key]         NVARCHAR(100) NOT NULL,
        [Value]       NVARCHAR(500) NOT NULL,
        [Category]    NVARCHAR(100) NULL,
        [Description] NVARCHAR(500) NULL,
        CONSTRAINT [PK_ServiceSettings] PRIMARY KEY CLUSTERED ([Key])
    );
    PRINT 'Tabelle ServiceSettings erstellt.';

    -- Standard-Eintraege
    INSERT INTO [dbo].[ServiceSettings] ([Key], [Value], [Category], [Description]) VALUES
        ('Notifications:MeldebestandEnabled', 'true', 'Notifications', 'Meldebestand-Mail aktiv (true/false)'),
        ('Notifications:MeldebestandSubject', 'Meldebestand unterschritten — IDEAL AKE WMS', 'Notifications', 'Betreff der Meldebestand-Mail'),
        ('Notifications:Recipients', '', 'Notifications', 'Feste Empfaenger fuer Meldebestand-Mail (kommagetrennt)'),
        ('Notifications:AppBaseUrl', '', 'Notifications', 'Basis-URL der App fuer Links in Mails (z.B. https://wms.ake.at)'),
        ('Sync:ProductionOrdersEnabled', 'true', 'Sync', 'Produktionsauftraege-Sync aus SAGE aktiv (true/false)'),
        ('Sync:ArticlesEnabled', 'true', 'Sync', 'Artikel-Sync aus SAGE aktiv (true/false)');
    PRINT 'Standard-ServiceSettings eingefuegt.';
END
GO

-- =============================================
-- 14. Holidays
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
-- 15. Indexes
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
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_ProductionWorkplaceId')
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_ProductionWorkplaceId] ON [dbo].[ProductionOrders]([ProductionWorkplaceId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PickingItems_ProductionOrderId')
    CREATE NONCLUSTERED INDEX [IX_PickingItems_ProductionOrderId] ON [dbo].[PickingItems]([ProductionOrderId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PickingItems_POId_IsPicked')
    CREATE NONCLUSTERED INDEX [IX_PickingItems_POId_IsPicked] ON [dbo].[PickingItems]([ProductionOrderId], [IsPicked]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionWorkplaceUsers_ProductionWorkplaceId_UserId')
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ProductionWorkplaceUsers_ProductionWorkplaceId_UserId] ON [dbo].[ProductionWorkplaceUsers]([ProductionWorkplaceId], [UserId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionWorkplaceUsers_UserId')
    CREATE NONCLUSTERED INDEX [IX_ProductionWorkplaceUsers_UserId] ON [dbo].[ProductionWorkplaceUsers]([UserId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkOperations_ProductionOrderId')
    CREATE NONCLUSTERED INDEX [IX_WorkOperations_ProductionOrderId] ON [dbo].[WorkOperations]([ProductionOrderId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkOperations_ProductionOrderId_Sequence')
    CREATE NONCLUSTERED INDEX [IX_WorkOperations_ProductionOrderId_Sequence] ON [dbo].[WorkOperations]([ProductionOrderId], [Sequence]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkOperations_ProductionWorkplaceId')
    CREATE NONCLUSTERED INDEX [IX_WorkOperations_ProductionWorkplaceId] ON [dbo].[WorkOperations]([ProductionWorkplaceId]);
GO

PRINT 'Indexes erstellt.';
GO

-- =============================================
-- 16. Views
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
-- 17. Standard-Daten
-- =============================================

-- NAN-Lagerplatz (Fallback fuer nicht zugeordnete Artikel)
IF NOT EXISTS (SELECT 1 FROM [dbo].[StorageLocations] WHERE [Code] = 'NAN')
BEGIN
    INSERT INTO [dbo].[StorageLocations]
        ([Code], [Description], [IsPickingTransport], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES
        ('NAN', 'Nicht zugeordnet (Fallback)', 0, GETDATE(), 'system', 'system');
    PRINT 'Standard-Lagerplatz NAN eingefuegt.';
END

-- Admin-Benutzer (PasswordHash wird beim ersten App-Start automatisch gesetzt)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Name] = 'admin')
BEGIN
    INSERT INTO [dbo].[Users]
        ([Name], [IsActive], [HasMasterDataAccess], [CanPick], [IsAdmin], [PasswordHash], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES
        ('admin', 1, 1, 1, 1, NULL, GETDATE(), 'system', 'system');
    PRINT 'Standard-Benutzer admin eingefuegt (PasswordHash wird beim App-Start gesetzt).';
END
GO

-- =============================================
-- 18. EF Migrations History
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
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260306081711_AddProductionWorkplaces')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260306081711_AddProductionWorkplaces', '10.0.0');
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260310130059_AddRecursiveFilterSearch')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260310130059_AddRecursiveFilterSearch', '10.0.0');
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260311071826_AddUserEmailIsAdminNotify')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260311071826_AddUserEmailIsAdminNotify', '10.0.0');
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260311071840_AddServiceSettings')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260311071840_AddServiceSettings', '10.0.0');
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260311145014_AddArticleGroup')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260311145014_AddArticleGroup', '10.0.2');
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260316062006_AddWorkOperationsPhase1')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260316062006_AddWorkOperationsPhase1', '10.0.2');
GO

PRINT 'EF Migrations History initialisiert.';
PRINT '====================================';
PRINT 'IDEAL-AKE WMS Installation abgeschlossen.';
GO

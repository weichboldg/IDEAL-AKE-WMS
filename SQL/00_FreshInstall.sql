-- =============================================
-- IDEAL-AKE WMS - Konsolidiertes Neuinstallations-Script
-- Erstellt alle Tabellen, Views und Standarddaten im finalen Zustand.
-- Fuer bestehende Installationen die einzelnen Migrations-Scripts (01-37) verwenden.
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
        [IsReleasedForPicking]    BIT               NOT NULL DEFAULT 0,
        [PickingPriority]         INT               NULL,
        [ReleasedAt]              DATETIME2         NULL,
        [ReleasedBy]              NVARCHAR(200)     NULL,
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
        [LastChangedInOseon]      DATETIME2 NULL,
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
        [LastStatusReportInOseon]   DATETIME2 NULL,
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
-- 14. Roles
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [Key]               NVARCHAR(50)      NOT NULL,
        [Name]              NVARCHAR(100)     NOT NULL,
        [Description]       NVARCHAR(500)     NULL,
        [AdGroup]           NVARCHAR(200)     NULL,
        [IsSystem]          BIT               NOT NULL DEFAULT 0,
        [SortOrder]         INT               NOT NULL DEFAULT 0,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id])
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Roles_Key] ON [dbo].[Roles]([Key]);
    PRINT 'Tabelle Roles erstellt.';
END
GO

-- =============================================
-- 15. UserRoles
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserRoles')
BEGIN
    CREATE TABLE [dbo].[UserRoles] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [UserId]            INT               NOT NULL,
        [RoleId]            INT               NOT NULL,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_UserRoles_UserId_RoleId] ON [dbo].[UserRoles]([UserId], [RoleId]);
    CREATE NONCLUSTERED INDEX [IX_UserRoles_RoleId] ON [dbo].[UserRoles]([RoleId]);
    PRINT 'Tabelle UserRoles erstellt.';
END
GO

-- =============================================
-- 16. AppSettings
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
        ('BeschichtungAbholtage', 'Dienstag,Donnerstag', 'Wochentage fuer Beschichtungs-Abholung'),
        ('TeileverfolgungAktiv', 'false', 'Globaler Schalter: Teileverfolgungs-Modul aktiviert'),
        ('OseonRueckmeldungAktiv', 'false', 'Rueckmeldungen duerfen an Oseon zurueckgeschrieben werden'),
        ('SageRueckmeldungAktiv', 'false', 'Rueckmeldungen duerfen an Sage zurueckgeschrieben werden'),
        ('QrMitFaNummer', 'false', 'QR-Code enthaelt Fertigungsauftragsnummer an 3. Stelle'),
        ('OseonAmpelGelbTage', '1', 'OSEON Ampel: Gelb ab X Tagen vor Termin'),
        ('OseonAmpelBlauTage', '2', 'OSEON Ampel: Blau ab X Tagen vor Termin'),
        ('BestellungenAktiv', 'false', 'Bedarfsmeldungen aus Stueckliste aktivieren'),
        ('LeitstandAktiv', 'false', 'Leitstand-Modul: Kommissionier-Freigabe und Priorisierung aktivieren');
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
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_IsReleasedForPicking_IsDone')
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_IsReleasedForPicking_IsDone] ON [dbo].[ProductionOrders]([IsReleasedForPicking], [IsDone]) INCLUDE ([PickingPriority], [ProductionDate]);
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
-- 16b. OseonOperationConfigs
-- =============================================
IF OBJECT_ID(N'[dbo].[OseonOperationConfigs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OseonOperationConfigs] (
        [Id]                 INT            IDENTITY(1,1) NOT NULL,
        [OperationName]      NVARCHAR(100)  NOT NULL,
        [DisplayName]        NVARCHAR(200)  NULL,
        [DueDateOffsetDays]  INT            NOT NULL DEFAULT 0,
        [IsOseonRelevant]    BIT            NOT NULL DEFAULT 1,
        CONSTRAINT [PK_OseonOperationConfigs] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_OseonOperationConfigs_OperationName] UNIQUE ([OperationName])
    );
    PRINT 'Tabelle [OseonOperationConfigs] erstellt.';
END
GO

-- Performance-Indexes StockMovements (38)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_ArticleId_StorageLocationId' AND object_id = OBJECT_ID('StockMovements'))
    CREATE NONCLUSTERED INDEX [IX_StockMovements_ArticleId_StorageLocationId]
    ON [StockMovements] ([ArticleId], [StorageLocationId]) INCLUDE ([Quantity], [MovementType]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_ArticleId_SourceStorageLocationId_MovementType' AND object_id = OBJECT_ID('StockMovements'))
    CREATE NONCLUSTERED INDEX [IX_StockMovements_ArticleId_SourceStorageLocationId_MovementType]
    ON [StockMovements] ([ArticleId], [SourceStorageLocationId], [MovementType]) INCLUDE ([Quantity]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_ProductionOrder' AND object_id = OBJECT_ID('StockMovements'))
    CREATE NONCLUSTERED INDEX [IX_StockMovements_ProductionOrder]
    ON [StockMovements] ([ProductionOrder]);

-- =============================================
-- 16c. ArticleCategories
-- =============================================
IF OBJECT_ID(N'[dbo].[ArticleCategories]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleCategories] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [Name]              NVARCHAR(200)  NOT NULL,
        [Description]       NVARCHAR(500)  NULL,
        [OseonTyp]          INT            NULL,
        [Source]            NVARCHAR(50)   NULL,
        [CreatedAt]         DATETIME2      NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [CreatedByWindows]  NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [ModifiedAt]        DATETIME2      NULL,
        [ModifiedBy]        NVARCHAR(200)  NULL,
        [ModifiedByWindows] NVARCHAR(200)  NULL,
        CONSTRAINT [PK_ArticleCategories] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ArticleCategories_Name] UNIQUE ([Name])
    );
    PRINT 'Tabelle [ArticleCategories] erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Articles') AND name = 'ArticleCategoryId')
BEGIN
    ALTER TABLE [dbo].[Articles] ADD [ArticleCategoryId] INT NULL;
    ALTER TABLE [dbo].[Articles] ADD CONSTRAINT [FK_Articles_ArticleCategories]
        FOREIGN KEY ([ArticleCategoryId]) REFERENCES [dbo].[ArticleCategories]([Id])
        ON DELETE SET NULL;
    CREATE NONCLUSTERED INDEX [IX_Articles_ArticleCategoryId]
        ON [dbo].[Articles] ([ArticleCategoryId]);
    PRINT 'Spalte [Articles].[ArticleCategoryId] mit FK + Index erstellt.';
END
GO

-- =============================================
-- 16d. ArticleAttributeDefinitions + Options + Values
-- =============================================
IF OBJECT_ID(N'[dbo].[ArticleAttributeDefinitions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleAttributeDefinitions] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [Name]              NVARCHAR(200)  NOT NULL,
        [AttributeType]     INT            NOT NULL DEFAULT 0,
        [SortOrder]         INT            NOT NULL DEFAULT 0,
        [IsActive]          BIT            NOT NULL DEFAULT 1,
        [SyncSource]        NVARCHAR(50)   NULL,
        [SyncFieldName]     NVARCHAR(200)  NULL,
        [CreatedAt]         DATETIME2      NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [CreatedByWindows]  NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [ModifiedAt]        DATETIME2      NULL,
        [ModifiedBy]        NVARCHAR(200)  NULL,
        [ModifiedByWindows] NVARCHAR(200)  NULL,
        CONSTRAINT [PK_ArticleAttributeDefinitions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ArticleAttributeDefinitions_Name] UNIQUE ([Name])
    );
    PRINT 'Tabelle [ArticleAttributeDefinitions] erstellt.';
END
GO

IF OBJECT_ID(N'[dbo].[ArticleAttributeOptions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleAttributeOptions] (
        [Id]                              INT            IDENTITY(1,1) NOT NULL,
        [ArticleAttributeDefinitionId]    INT            NOT NULL,
        [Value]                           NVARCHAR(200)  NOT NULL,
        [SortOrder]                       INT            NOT NULL DEFAULT 0,
        CONSTRAINT [PK_ArticleAttributeOptions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ArticleAttributeOptions_Definitions]
            FOREIGN KEY ([ArticleAttributeDefinitionId])
            REFERENCES [dbo].[ArticleAttributeDefinitions]([Id])
            ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_ArticleAttributeOptions_DefinitionId]
        ON [dbo].[ArticleAttributeOptions] ([ArticleAttributeDefinitionId]);
    PRINT 'Tabelle [ArticleAttributeOptions] erstellt.';
END
GO

IF OBJECT_ID(N'[dbo].[ArticleAttributeValues]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleAttributeValues] (
        [Id]                              INT            IDENTITY(1,1) NOT NULL,
        [ArticleId]                       INT            NOT NULL,
        [ArticleAttributeDefinitionId]    INT            NOT NULL,
        [BooleanValue]                    BIT            NULL,
        [SelectedOptionId]                INT            NULL,
        [CreatedAt]                       DATETIME2      NOT NULL DEFAULT GETDATE(),
        [CreatedBy]                       NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [CreatedByWindows]                NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [ModifiedAt]                      DATETIME2      NULL,
        [ModifiedBy]                      NVARCHAR(200)  NULL,
        [ModifiedByWindows]               NVARCHAR(200)  NULL,
        CONSTRAINT [PK_ArticleAttributeValues] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ArticleAttributeValues_Articles]
            FOREIGN KEY ([ArticleId]) REFERENCES [dbo].[Articles]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ArticleAttributeValues_Definitions]
            FOREIGN KEY ([ArticleAttributeDefinitionId]) REFERENCES [dbo].[ArticleAttributeDefinitions]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ArticleAttributeValues_Options]
            FOREIGN KEY ([SelectedOptionId]) REFERENCES [dbo].[ArticleAttributeOptions]([Id]) ON DELETE NO ACTION
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ArticleAttributeValues_ArticleId_DefinitionId]
        ON [dbo].[ArticleAttributeValues] ([ArticleId], [ArticleAttributeDefinitionId]);
    CREATE NONCLUSTERED INDEX [IX_ArticleAttributeValues_ArticleId]
        ON [dbo].[ArticleAttributeValues] ([ArticleId]);
    PRINT 'Tabelle [ArticleAttributeValues] erstellt.';
END
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

-- Standard-Rollen
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'admin')
BEGIN
    INSERT INTO [dbo].[Roles] ([Key], [Name], [Description], [AdGroup], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows]) VALUES
        ('admin',         'Administrator',      'Vollzugriff auf alle Funktionen',                                          NULL, 1, 0, GETDATE(), 'system', 'system'),
        ('masterdata',    'Stammdaten',          'Zugriff auf Stammdatenverwaltung (Benutzer, Arbeitsplaetze, Einstellungen)', 'BDE_Stammdaten', 1, 1, GETDATE(), 'system', 'system'),
        ('picking',       'Kommissionierung',    'Kommissionierung, Lagerbewegungen, Bestaende',                             NULL, 1, 2, GETDATE(), 'system', 'system'),
        ('stock',         'Lager',               'Lagerbewegungen und Bestandsuebersicht',                                   NULL, 1, 3, GETDATE(), 'system', 'system'),
        ('stock_keyuser', 'Lager Key-User',      'Erweiterte Lagerfunktionen (Korrekturbuchungen, Bestandsbereinigung)',     NULL, 1, 4, GETDATE(), 'system', 'system'),
        ('tracking',      'Teileverfolgung',     'Teileverfolgung und OSEON-Auftraege anzeigen',                             NULL, 1, 5, GETDATE(), 'system', 'system'),
        ('reporting',     'Rueckmeldung',        'Arbeitsgaenge rueckmelden',                                                NULL, 1, 6, GETDATE(), 'system', 'system'),
        ('leitstand',    'Leitstand',           'Produktionsauftraege freigeben und priorisieren',                              NULL, 1, 7, GETDATE(), 'system', 'system');
    PRINT 'Standard-Rollen eingefuegt.';
END
GO

-- Standard-Arbeitsgang-Konfigurationen (OSEON)
IF NOT EXISTS (SELECT 1 FROM [dbo].[OseonOperationConfigs])
BEGIN
    INSERT INTO [dbo].[OseonOperationConfigs] ([OperationName], [DisplayName], [DueDateOffsetDays], [IsOseonRelevant])
    VALUES
        (N'B',       N'Belegen',         -1, 1),
        (N'ST',      N'Stanzen',          0, 1),
        (N'EG',      N'Entgraten',        0, 1),
        (N'BG',      N'Biegen',           2, 1),
        (N'BG-SaP1', N'Biegen SaP1',     2, 1),
        (N'RO',      N'Rollen',           2, 1),
        (N'MS',      N'Maschinenschub',   4, 1),
        (N'RS',      N'Restschweissen',   4, 1),
        (N'SL',      N'Schlosser',        5, 1),
        (N'RE',      N'Reinigen',         5, 1),
        (N'ZB',      N'Zusammenbau',      0, 0),
        (N'A-BT',    N'Anlegen BT',       0, 0);
    PRINT 'Standard-Arbeitsgang-Konfigurationen eingefuegt.';
END
GO

-- Admin-Benutzer bekommt admin-Rolle
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoles] ur
    INNER JOIN [dbo].[Users] u ON ur.[UserId] = u.[Id]
    INNER JOIN [dbo].[Roles] r ON ur.[RoleId] = r.[Id]
    WHERE u.[Name] = 'admin' AND r.[Key] = 'admin')
BEGIN
    DECLARE @adminUserId INT = (SELECT [Id] FROM [dbo].[Users] WHERE [Name] = 'admin');
    DECLARE @adminRoleId INT = (SELECT [Id] FROM [dbo].[Roles] WHERE [Key] = 'admin');
    IF @adminUserId IS NOT NULL AND @adminRoleId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [CreatedAt], [CreatedBy], [CreatedByWindows])
        VALUES (@adminUserId, @adminRoleId, GETDATE(), 'system', 'system');
        PRINT 'Admin-Benutzer hat admin-Rolle erhalten.';
    END
END
GO

-- =============================================
-- 17b. OrderRecipientGroups
-- =============================================
IF OBJECT_ID(N'[dbo].[OrderRecipientGroups]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderRecipientGroups] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
        [CreatedBy] nvarchar(200) NULL,
        [CreatedByWindows] nvarchar(200) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [ModifiedByWindows] nvarchar(200) NULL,
        CONSTRAINT [PK_OrderRecipientGroups] PRIMARY KEY ([Id])
    );
    PRINT 'Tabelle OrderRecipientGroups erstellt.';
END
GO

-- =============================================
-- 17c. OrderRecipients
-- =============================================
IF OBJECT_ID(N'[dbo].[OrderRecipients]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderRecipients] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [OrderRecipientGroupId] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Email] nvarchar(300) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
        [CreatedBy] nvarchar(200) NULL,
        [CreatedByWindows] nvarchar(200) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [ModifiedByWindows] nvarchar(200) NULL,
        CONSTRAINT [PK_OrderRecipients] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderRecipients_OrderRecipientGroups] FOREIGN KEY ([OrderRecipientGroupId])
            REFERENCES [dbo].[OrderRecipientGroups]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_OrderRecipients_GroupId] ON [dbo].[OrderRecipients]([OrderRecipientGroupId]);
    PRINT 'Tabelle OrderRecipients erstellt.';
END
GO

-- =============================================
-- 17d. ArticleGroupRecipientMappings
-- =============================================
IF OBJECT_ID(N'[dbo].[ArticleGroupRecipientMappings]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleGroupRecipientMappings] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [ArticleGroup] nvarchar(100) NOT NULL,
        [OrderRecipientGroupId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
        [CreatedBy] nvarchar(200) NULL,
        [CreatedByWindows] nvarchar(200) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [ModifiedByWindows] nvarchar(200) NULL,
        CONSTRAINT [PK_ArticleGroupRecipientMappings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ArticleGroupRecipientMappings_OrderRecipientGroups] FOREIGN KEY ([OrderRecipientGroupId])
            REFERENCES [dbo].[OrderRecipientGroups]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_ArticleGroupRecipientMappings_ArticleGroup] ON [dbo].[ArticleGroupRecipientMappings]([ArticleGroup]);
    CREATE UNIQUE INDEX [UX_ArticleGroupRecipientMappings_Group_Recipient] ON [dbo].[ArticleGroupRecipientMappings]([ArticleGroup], [OrderRecipientGroupId]);
    PRINT 'Tabelle ArticleGroupRecipientMappings erstellt.';
END
GO

-- =============================================
-- 17e. PartRequisitions
-- =============================================
IF OBJECT_ID(N'[dbo].[PartRequisitions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PartRequisitions] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [ProductionOrderId] int NOT NULL,
        [ArticleNumber] nvarchar(100) NOT NULL,
        [ArticleDescription] nvarchar(500) NULL,
        [ArticleGroup] nvarchar(100) NULL,
        [Position] nvarchar(50) NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [Unit] nvarchar(20) NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT 'Offen',
        [Priority] nvarchar(20) NOT NULL DEFAULT 'Normal',
        [Notes] nvarchar(1000) NULL,
        [OrderRecipientGroupId] int NULL,
        [SentToEmails] nvarchar(1000) NULL,
        [EmailSentAt] datetime2 NULL,
        [FulfilledByStockMovementId] int NULL,
        [FulfilledAt] datetime2 NULL,
        [CancelledAt] datetime2 NULL,
        [CancelledBy] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
        [CreatedBy] nvarchar(200) NULL,
        [CreatedByWindows] nvarchar(200) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [ModifiedByWindows] nvarchar(200) NULL,
        CONSTRAINT [PK_PartRequisitions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PartRequisitions_ProductionOrders] FOREIGN KEY ([ProductionOrderId])
            REFERENCES [dbo].[ProductionOrders]([Id]),
        CONSTRAINT [FK_PartRequisitions_OrderRecipientGroups] FOREIGN KEY ([OrderRecipientGroupId])
            REFERENCES [dbo].[OrderRecipientGroups]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_PartRequisitions_StockMovements] FOREIGN KEY ([FulfilledByStockMovementId])
            REFERENCES [dbo].[StockMovements]([Id]) ON DELETE SET NULL
    );

    CREATE INDEX [IX_PartRequisitions_ProductionOrderId] ON [dbo].[PartRequisitions]([ProductionOrderId]);
    CREATE INDEX [IX_PartRequisitions_ArticleNumber] ON [dbo].[PartRequisitions]([ArticleNumber]);
    CREATE INDEX [IX_PartRequisitions_Status] ON [dbo].[PartRequisitions]([Status]);
    CREATE INDEX [IX_PartRequisitions_EmailSentAt_Status] ON [dbo].[PartRequisitions]([EmailSentAt], [Status]);
    PRINT 'Tabelle PartRequisitions erstellt.';
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
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260317140041_AddOseonTracking')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260317140041_AddOseonTracking', '10.0.2');
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260318150710_AddOseonTimestamps')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260318150710_AddOseonTimestamps', '10.0.2');
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260320101049_AddRolesAndUserRoles')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260320101049_AddRolesAndUserRoles', '10.0.2');
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] LIKE '%_AddOseonOperationConfig')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260330120000_AddOseonOperationConfig', '10.0.0');
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260403055243_AddPartRequisitions')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260403055243_AddPartRequisitions', '10.0.0');
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260403161339_AddPickingRelease')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260403161339_AddPickingRelease', '10.0.0');

IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] LIKE '%_AddArticleCategoriesAndAttributes')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260407061642_AddArticleCategoriesAndAttributes', '10.0.0');
GO

PRINT 'EF Migrations History initialisiert.';
PRINT '====================================';
PRINT 'IDEAL-AKE WMS Installation abgeschlossen.';
GO

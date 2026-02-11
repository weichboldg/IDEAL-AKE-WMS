-- AKE BDE Light - Tabellen erstellen
USE [AKE_BDE_Light]
GO

-- =============================================
-- Users
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [Name]              NVARCHAR(200)     NOT NULL,
        [PersonalNumber]    NVARCHAR(50)      NULL,
        [IsActive]          BIT               NOT NULL DEFAULT 1,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id])
    )
END
GO

-- =============================================
-- Workstations
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Workstations')
BEGIN
    CREATE TABLE [dbo].[Workstations] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [Name]              NVARCHAR(200)     NOT NULL,
        [Location]          NVARCHAR(200)     NULL,
        [DefaultUserId]     INT               NULL,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_Workstations] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Workstations_DefaultUser] FOREIGN KEY ([DefaultUserId]) REFERENCES [dbo].[Users]([Id])
    )
END
GO

-- =============================================
-- WorkstationUsers (Zuordnung Benutzer zu Arbeitsplatz)
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
    )
END
GO

-- =============================================
-- StorageLocations (Lagerplätze)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StorageLocations')
BEGIN
    CREATE TABLE [dbo].[StorageLocations] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [Code]              NVARCHAR(50)      NOT NULL,
        [Description]       NVARCHAR(200)     NULL,
        [Zone]              NVARCHAR(100)     NULL,
        [Capacity]          DECIMAL(18,2)     NULL,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_StorageLocations] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_StorageLocations_Code] UNIQUE ([Code])
    )
END
GO

-- =============================================
-- Articles (Artikel)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Articles')
BEGIN
    CREATE TABLE [dbo].[Articles] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [ArticleNumber]     NVARCHAR(100)     NOT NULL,
        [Description]       NVARCHAR(500)     NULL,
        [Unit]              NVARCHAR(20)      NULL,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_Articles] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_Articles_ArticleNumber] UNIQUE ([ArticleNumber])
    )
END
GO

-- =============================================
-- StockMovements (Lagerbewegungen)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StockMovements')
BEGIN
    CREATE TABLE [dbo].[StockMovements] (
        [Id]                  INT IDENTITY(1,1) NOT NULL,
        [ArticleId]           INT               NOT NULL,
        [Quantity]            DECIMAL(18,3)     NOT NULL,
        [StorageLocationId]   INT               NOT NULL,
        [ProductionOrder]     NVARCHAR(100)     NULL,
        [MovementType]        INT               NOT NULL,  -- 0 = Einbuchung, 1 = Ausbuchung
        [Timestamp]           DATETIME2         NOT NULL DEFAULT GETDATE(),
        [UserId]              INT               NULL,
        [WindowsUser]         NVARCHAR(200)     NOT NULL,
        [CreatedAt]           DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]           NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]    NVARCHAR(200)     NOT NULL,
        [ModifiedAt]          DATETIME2         NULL,
        [ModifiedBy]          NVARCHAR(200)     NULL,
        [ModifiedByWindows]   NVARCHAR(200)     NULL,
        CONSTRAINT [PK_StockMovements] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_StockMovements_Article] FOREIGN KEY ([ArticleId]) REFERENCES [dbo].[Articles]([Id]),
        CONSTRAINT [FK_StockMovements_StorageLocation] FOREIGN KEY ([StorageLocationId]) REFERENCES [dbo].[StorageLocations]([Id]),
        CONSTRAINT [FK_StockMovements_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
    )
END
GO

-- =============================================
-- Indizes
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StockMovements_ArticleId')
    CREATE NONCLUSTERED INDEX [IX_StockMovements_ArticleId] ON [dbo].[StockMovements]([ArticleId]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StockMovements_StorageLocationId')
    CREATE NONCLUSTERED INDEX [IX_StockMovements_StorageLocationId] ON [dbo].[StockMovements]([StorageLocationId]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StockMovements_Timestamp')
    CREATE NONCLUSTERED INDEX [IX_StockMovements_Timestamp] ON [dbo].[StockMovements]([Timestamp]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkstationUsers_WorkstationId')
    CREATE NONCLUSTERED INDEX [IX_WorkstationUsers_WorkstationId] ON [dbo].[WorkstationUsers]([WorkstationId]);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkstationUsers_UserId')
    CREATE NONCLUSTERED INDEX [IX_WorkstationUsers_UserId] ON [dbo].[WorkstationUsers]([UserId]);
GO

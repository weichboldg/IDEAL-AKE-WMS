-- =============================================
-- 42_AddBde.sql
-- Betriebsdatenerfassung (BDE) Phase 1
-- =============================================

-- 1) BdeTerminals
IF OBJECT_ID(N'dbo.BdeTerminals', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BdeTerminals] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL,
        [DefaultProductionWorkplaceId] INT NOT NULL,
        [Description] NVARCHAR(200) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [CreatedBy] NVARCHAR(200) NOT NULL,
        [CreatedByWindows] NVARCHAR(200) NOT NULL,
        [ModifiedAt] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [ModifiedByWindows] NVARCHAR(200) NULL,
        CONSTRAINT [FK_BdeTerminals_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_BdeTerminals_ProductionWorkplaces] FOREIGN KEY ([DefaultProductionWorkplaceId]) REFERENCES [dbo].[ProductionWorkplaces]([Id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeTerminals_UserId' AND object_id = OBJECT_ID(N'dbo.BdeTerminals'))
    CREATE UNIQUE INDEX [IX_BdeTerminals_UserId] ON [dbo].[BdeTerminals]([UserId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeTerminals_DefaultProductionWorkplaceId' AND object_id = OBJECT_ID(N'dbo.BdeTerminals'))
    CREATE INDEX [IX_BdeTerminals_DefaultProductionWorkplaceId] ON [dbo].[BdeTerminals]([DefaultProductionWorkplaceId]);
GO

-- 2) BdeOperators
IF OBJECT_ID(N'dbo.BdeOperators', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BdeOperators] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PersonnelNumber] NVARCHAR(50) NOT NULL,
        [FirstName] NVARCHAR(100) NOT NULL,
        [LastName] NVARCHAR(100) NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [UserId] INT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [CreatedBy] NVARCHAR(200) NOT NULL,
        [CreatedByWindows] NVARCHAR(200) NOT NULL,
        [ModifiedAt] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [ModifiedByWindows] NVARCHAR(200) NULL,
        CONSTRAINT [FK_BdeOperators_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE SET NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeOperators_PersonnelNumber' AND object_id = OBJECT_ID(N'dbo.BdeOperators'))
    CREATE UNIQUE INDEX [IX_BdeOperators_PersonnelNumber] ON [dbo].[BdeOperators]([PersonnelNumber]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeOperators_UserId' AND object_id = OBJECT_ID(N'dbo.BdeOperators'))
    CREATE UNIQUE INDEX [IX_BdeOperators_UserId] ON [dbo].[BdeOperators]([UserId]) WHERE [UserId] IS NOT NULL;
GO

-- 3) BdeActivities
IF OBJECT_ID(N'dbo.BdeActivities', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BdeActivities] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Code] NVARCHAR(20) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL,
        [CreatedBy] NVARCHAR(200) NOT NULL,
        [CreatedByWindows] NVARCHAR(200) NOT NULL,
        [ModifiedAt] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [ModifiedByWindows] NVARCHAR(200) NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeActivities_Code' AND object_id = OBJECT_ID(N'dbo.BdeActivities'))
    CREATE UNIQUE INDEX [IX_BdeActivities_Code] ON [dbo].[BdeActivities]([Code]);
GO

-- 4) BdeBookings
IF OBJECT_ID(N'dbo.BdeBookings', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BdeBookings] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [BdeOperatorId] INT NOT NULL,
        [ProductionWorkplaceId] INT NOT NULL,
        [BdeTerminalId] INT NOT NULL,
        [WorkOperationId] INT NULL,
        [BdeActivityId] INT NULL,
        [BookingType] TINYINT NOT NULL,
        [Status] TINYINT NOT NULL,
        [StartedAt] DATETIME2 NOT NULL,
        [EndedAt] DATETIME2 NULL,
        [IsCancelled] BIT NOT NULL DEFAULT 0,
        [CancellationReason] NVARCHAR(500) NULL,
        [ParentBookingId] INT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [CreatedBy] NVARCHAR(200) NOT NULL,
        [CreatedByWindows] NVARCHAR(200) NOT NULL,
        [ModifiedAt] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [ModifiedByWindows] NVARCHAR(200) NULL,
        CONSTRAINT [FK_BdeBookings_Operators] FOREIGN KEY ([BdeOperatorId]) REFERENCES [dbo].[BdeOperators]([Id]),
        CONSTRAINT [FK_BdeBookings_Workplaces] FOREIGN KEY ([ProductionWorkplaceId]) REFERENCES [dbo].[ProductionWorkplaces]([Id]),
        CONSTRAINT [FK_BdeBookings_Terminals] FOREIGN KEY ([BdeTerminalId]) REFERENCES [dbo].[BdeTerminals]([Id]),
        CONSTRAINT [FK_BdeBookings_WorkOperations] FOREIGN KEY ([WorkOperationId]) REFERENCES [dbo].[WorkOperations]([Id]),
        CONSTRAINT [FK_BdeBookings_Activities] FOREIGN KEY ([BdeActivityId]) REFERENCES [dbo].[BdeActivities]([Id]),
        CONSTRAINT [FK_BdeBookings_Parent] FOREIGN KEY ([ParentBookingId]) REFERENCES [dbo].[BdeBookings]([Id]),
        CONSTRAINT [CK_BdeBookings_Target] CHECK (
            ([WorkOperationId] IS NOT NULL AND [BdeActivityId] IS NULL) OR
            ([WorkOperationId] IS NULL AND [BdeActivityId] IS NOT NULL)
        ),
        CONSTRAINT [CK_BdeBookings_TypeTarget] CHECK (
            ([BookingType] = 3 AND [BdeActivityId] IS NOT NULL) OR
            ([BookingType] IN (1,2) AND [WorkOperationId] IS NOT NULL)
        ),
        CONSTRAINT [CK_BdeBookings_StatusEnded] CHECK (
            ([Status] = 1 AND [EndedAt] IS NULL) OR
            ([Status] IN (2,3) AND [EndedAt] IS NOT NULL)
        )
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookings_WorkOperationId_Active' AND object_id = OBJECT_ID(N'dbo.BdeBookings'))
    CREATE UNIQUE INDEX [IX_BdeBookings_WorkOperationId_Active]
        ON [dbo].[BdeBookings]([WorkOperationId])
        WHERE [EndedAt] IS NULL AND [IsCancelled] = 0 AND [WorkOperationId] IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookings_BdeOperatorId_Active' AND object_id = OBJECT_ID(N'dbo.BdeBookings'))
    CREATE UNIQUE INDEX [IX_BdeBookings_BdeOperatorId_Active]
        ON [dbo].[BdeBookings]([BdeOperatorId])
        WHERE [EndedAt] IS NULL AND [IsCancelled] = 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookings_Workplace_EndedAt' AND object_id = OBJECT_ID(N'dbo.BdeBookings'))
    CREATE INDEX [IX_BdeBookings_Workplace_EndedAt] ON [dbo].[BdeBookings]([ProductionWorkplaceId],[EndedAt]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookings_Operator_StartedAt' AND object_id = OBJECT_ID(N'dbo.BdeBookings'))
    CREATE INDEX [IX_BdeBookings_Operator_StartedAt] ON [dbo].[BdeBookings]([BdeOperatorId],[StartedAt]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookings_StartedAt' AND object_id = OBJECT_ID(N'dbo.BdeBookings'))
    CREATE INDEX [IX_BdeBookings_StartedAt] ON [dbo].[BdeBookings]([StartedAt]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookings_BdeActivityId' AND object_id = OBJECT_ID(N'dbo.BdeBookings'))
    CREATE INDEX [IX_BdeBookings_BdeActivityId] ON [dbo].[BdeBookings]([BdeActivityId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookings_BdeTerminalId' AND object_id = OBJECT_ID(N'dbo.BdeBookings'))
    CREATE INDEX [IX_BdeBookings_BdeTerminalId] ON [dbo].[BdeBookings]([BdeTerminalId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookings_ParentBookingId' AND object_id = OBJECT_ID(N'dbo.BdeBookings'))
    CREATE INDEX [IX_BdeBookings_ParentBookingId] ON [dbo].[BdeBookings]([ParentBookingId]);
GO

-- 5) BdeBookingQuantities
IF OBJECT_ID(N'dbo.BdeBookingQuantities', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BdeBookingQuantities] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [BdeBookingId] INT NOT NULL,
        [BdeOperatorId] INT NOT NULL,
        [GoodQuantity] DECIMAL(18,4) NOT NULL DEFAULT 0,
        [ScrapQuantity] DECIMAL(18,4) NOT NULL DEFAULT 0,
        [IsFinal] BIT NOT NULL,
        [ReportedAt] DATETIME2 NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [CreatedBy] NVARCHAR(200) NOT NULL,
        [CreatedByWindows] NVARCHAR(200) NOT NULL,
        [ModifiedAt] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [ModifiedByWindows] NVARCHAR(200) NULL,
        CONSTRAINT [FK_BdeBookingQuantities_Bookings] FOREIGN KEY ([BdeBookingId]) REFERENCES [dbo].[BdeBookings]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BdeBookingQuantities_Operators] FOREIGN KEY ([BdeOperatorId]) REFERENCES [dbo].[BdeOperators]([Id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookingQuantities_Booking_ReportedAt' AND object_id = OBJECT_ID(N'dbo.BdeBookingQuantities'))
    CREATE INDEX [IX_BdeBookingQuantities_Booking_ReportedAt] ON [dbo].[BdeBookingQuantities]([BdeBookingId],[ReportedAt]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookingQuantities_Booking_Final' AND object_id = OBJECT_ID(N'dbo.BdeBookingQuantities'))
    CREATE UNIQUE INDEX [IX_BdeBookingQuantities_Booking_Final] ON [dbo].[BdeBookingQuantities]([BdeBookingId]) WHERE [IsFinal] = 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookingQuantities_BdeOperatorId' AND object_id = OBJECT_ID(N'dbo.BdeBookingQuantities'))
    CREATE INDEX [IX_BdeBookingQuantities_BdeOperatorId] ON [dbo].[BdeBookingQuantities]([BdeOperatorId]);
GO

-- 6) Rollen-Seed (Role-Felder: Key, Name, Description, AdGroup, IsSystem, SortOrder)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'bde_user')
    INSERT INTO [dbo].[Roles] ([Key],[Name],[Description],[AdGroup],[IsSystem],[SortOrder],[CreatedAt],[CreatedBy],[CreatedByWindows])
    VALUES ('bde_user','BDE-Mitarbeiter','Terminal-Buchung: Arbeitsgänge scannen, Status wechseln, Mengen melden',NULL,1,100,GETDATE(),'System','System');
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'bde_shiftlead')
    INSERT INTO [dbo].[Roles] ([Key],[Name],[Description],[AdGroup],[IsSystem],[SortOrder],[CreatedAt],[CreatedBy],[CreatedByWindows])
    VALUES ('bde_shiftlead','BDE-Schichtleiter','BDE-Anwender + Aktivitäts-Kategorien pflegen, Buchungsliste + Cockpit',NULL,1,101,GETDATE(),'System','System');
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'bde_admin')
    INSERT INTO [dbo].[Roles] ([Key],[Name],[Description],[AdGroup],[IsSystem],[SortOrder],[CreatedAt],[CreatedBy],[CreatedByWindows])
    VALUES ('bde_admin','BDE-Admin','Vollzugriff: Buchungen korrigieren und stornieren, Terminals konfigurieren',NULL,1,102,GETDATE(),'System','System');
GO

-- 8) BDE AppSettings
IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = 'BdeAktiv')
    INSERT INTO [dbo].[AppSettings] ([Key],[Value],[Description])
    VALUES ('BdeAktiv','false','BDE-Modul (Betriebsdatenerfassung) aktivieren');
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = 'BdeNurFaMeldung')
    INSERT INTO [dbo].[AppSettings] ([Key],[Value],[Description])
    VALUES ('BdeNurFaMeldung','false','Vereinfachter BDE-Modus: Buchung auf FA statt einzelne Arbeitsgaenge');
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = 'BdeDefaultArbeitsgang')
    INSERT INTO [dbo].[AppSettings] ([Key],[Value],[Description])
    VALUES ('BdeDefaultArbeitsgang','','Default-Arbeitsgang fuer vereinfachten BDE-Modus (z.B. PRODUKTION)');
GO

-- 9) __EFMigrationsHistory
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260415121811_AddBde')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId],[ProductVersion])
    VALUES ('20260415121811_AddBde','10.0.0');
GO

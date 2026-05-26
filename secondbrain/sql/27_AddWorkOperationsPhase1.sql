-- =============================================
-- Migration 27: Work Operations Phase 1
-- Neue Berechtigungs-Flags, Werkbank-Benutzer-Zuordnung,
-- WA-Werkbank-Zuordnung, WorkOperations-Tabelle, AppSettings
-- Ziel: [IDEAL_AKE_WMS]
-- =============================================

-- 1. Users: 3 neue Berechtigungs-Flags
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Users]')
      AND name = N'CanPick'
)
BEGIN
    ALTER TABLE [dbo].[Users] ADD [CanPick] bit NOT NULL DEFAULT 0;
    PRINT 'Spalte Users.CanPick hinzugefuegt.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Users]')
      AND name = N'CanViewTracking'
)
BEGIN
    ALTER TABLE [dbo].[Users] ADD [CanViewTracking] bit NOT NULL DEFAULT 0;
    PRINT 'Spalte Users.CanViewTracking hinzugefuegt.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Users]')
      AND name = N'CanReportOperations'
)
BEGIN
    ALTER TABLE [dbo].[Users] ADD [CanReportOperations] bit NOT NULL DEFAULT 0;
    PRINT 'Spalte Users.CanReportOperations hinzugefuegt.';
END
GO

-- 2. ProductionOrders: FK zu ProductionWorkplace
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[ProductionOrders]')
      AND name = N'ProductionWorkplaceId'
)
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [ProductionWorkplaceId] int NULL;
    PRINT 'Spalte ProductionOrders.ProductionWorkplaceId hinzugefuegt.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[ProductionOrders]')
      AND name = N'IX_ProductionOrders_ProductionWorkplaceId'
)
BEGIN
    CREATE INDEX [IX_ProductionOrders_ProductionWorkplaceId]
        ON [dbo].[ProductionOrders] ([ProductionWorkplaceId]);
    PRINT 'Index IX_ProductionOrders_ProductionWorkplaceId erstellt.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_ProductionOrders_ProductionWorkplaces_ProductionWorkplaceId'
)
BEGIN
    ALTER TABLE [dbo].[ProductionOrders]
        ADD CONSTRAINT [FK_ProductionOrders_ProductionWorkplaces_ProductionWorkplaceId]
        FOREIGN KEY ([ProductionWorkplaceId])
        REFERENCES [dbo].[ProductionWorkplaces] ([Id])
        ON DELETE SET NULL;
    PRINT 'FK ProductionOrders -> ProductionWorkplaces erstellt.';
END
GO

-- 3. ProductionWorkplaceUsers (M:M Join-Tabelle)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductionWorkplaceUsers')
BEGIN
    CREATE TABLE [dbo].[ProductionWorkplaceUsers] (
        [Id]                       int            IDENTITY(1,1) NOT NULL,
        [ProductionWorkplaceId]    int            NOT NULL,
        [UserId]                   int            NOT NULL,
        [CreatedAt]                datetime2(7)   NOT NULL,
        [CreatedBy]                nvarchar(200)  NOT NULL,
        [CreatedByWindows]         nvarchar(200)  NOT NULL,
        [ModifiedAt]               datetime2(7)   NULL,
        [ModifiedBy]               nvarchar(200)  NULL,
        [ModifiedByWindows]        nvarchar(200)  NULL,
        CONSTRAINT [PK_ProductionWorkplaceUsers] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ProductionWorkplaceUsers_ProductionWorkplaces_ProductionWorkplaceId]
            FOREIGN KEY ([ProductionWorkplaceId])
            REFERENCES [dbo].[ProductionWorkplaces] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [FK_ProductionWorkplaceUsers_Users_UserId]
            FOREIGN KEY ([UserId])
            REFERENCES [dbo].[Users] ([Id])
            ON DELETE NO ACTION
    );
    PRINT 'Tabelle ProductionWorkplaceUsers erstellt.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[ProductionWorkplaceUsers]')
      AND name = N'IX_ProductionWorkplaceUsers_ProductionWorkplaceId_UserId'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductionWorkplaceUsers_ProductionWorkplaceId_UserId]
        ON [dbo].[ProductionWorkplaceUsers] ([ProductionWorkplaceId], [UserId]);
    PRINT 'Unique Index ProductionWorkplaceUsers (WorkplaceId+UserId) erstellt.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[ProductionWorkplaceUsers]')
      AND name = N'IX_ProductionWorkplaceUsers_UserId'
)
BEGIN
    CREATE INDEX [IX_ProductionWorkplaceUsers_UserId]
        ON [dbo].[ProductionWorkplaceUsers] ([UserId]);
    PRINT 'Index ProductionWorkplaceUsers.UserId erstellt.';
END
GO

-- 4. WorkOperations
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkOperations')
BEGIN
    CREATE TABLE [dbo].[WorkOperations] (
        [Id]                       int            IDENTITY(1,1) NOT NULL,
        [ProductionOrderId]        int            NOT NULL,
        [OperationNumber]          nvarchar(50)   NOT NULL,
        [Name]                     nvarchar(200)  NOT NULL,
        [ProductionWorkplaceId]    int            NULL,
        [Sequence]                 int            NOT NULL,
        [IsReportable]             bit            NOT NULL,
        [IsExternalSystem]         bit            NOT NULL,
        [IsReported]               bit            NOT NULL,
        [ReportedAt]               datetime2(7)   NULL,
        [ReportedBy]               nvarchar(200)  NULL,
        [ReportedByWindows]        nvarchar(200)  NULL,
        [ExternalSource]           nvarchar(100)  NULL,
        [CreatedAt]                datetime2(7)   NOT NULL,
        [CreatedBy]                nvarchar(200)  NOT NULL,
        [CreatedByWindows]         nvarchar(200)  NOT NULL,
        [ModifiedAt]               datetime2(7)   NULL,
        [ModifiedBy]               nvarchar(200)  NULL,
        [ModifiedByWindows]        nvarchar(200)  NULL,
        CONSTRAINT [PK_WorkOperations] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_WorkOperations_ProductionOrders_ProductionOrderId]
            FOREIGN KEY ([ProductionOrderId])
            REFERENCES [dbo].[ProductionOrders] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [FK_WorkOperations_ProductionWorkplaces_ProductionWorkplaceId]
            FOREIGN KEY ([ProductionWorkplaceId])
            REFERENCES [dbo].[ProductionWorkplaces] ([Id])
            ON DELETE SET NULL
    );
    PRINT 'Tabelle WorkOperations erstellt.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[WorkOperations]')
      AND name = N'IX_WorkOperations_ProductionOrderId'
)
BEGIN
    CREATE INDEX [IX_WorkOperations_ProductionOrderId]
        ON [dbo].[WorkOperations] ([ProductionOrderId]);
    PRINT 'Index WorkOperations.ProductionOrderId erstellt.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[WorkOperations]')
      AND name = N'IX_WorkOperations_ProductionOrderId_Sequence'
)
BEGIN
    CREATE INDEX [IX_WorkOperations_ProductionOrderId_Sequence]
        ON [dbo].[WorkOperations] ([ProductionOrderId], [Sequence]);
    PRINT 'Index WorkOperations (ProductionOrderId+Sequence) erstellt.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[WorkOperations]')
      AND name = N'IX_WorkOperations_ProductionWorkplaceId'
)
BEGIN
    CREATE INDEX [IX_WorkOperations_ProductionWorkplaceId]
        ON [dbo].[WorkOperations] ([ProductionWorkplaceId]);
    PRINT 'Index WorkOperations.ProductionWorkplaceId erstellt.';
END
GO

-- 5. AppSettings fuer Teileverfolgung
IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = N'TeileverfolgungAktiv')
BEGIN
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES (N'TeileverfolgungAktiv', N'false', N'Globaler Schalter: Teileverfolgungs-Modul aktiviert');
    PRINT 'AppSetting TeileverfolgungAktiv eingefuegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = N'OseonRueckmeldungAktiv')
BEGIN
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES (N'OseonRueckmeldungAktiv', N'false', N'Rueckmeldungen duerfen an Oseon zurueckgeschrieben werden');
    PRINT 'AppSetting OseonRueckmeldungAktiv eingefuegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = N'SageRueckmeldungAktiv')
BEGIN
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES (N'SageRueckmeldungAktiv', N'false', N'Rueckmeldungen duerfen an Sage zurueckgeschrieben werden');
    PRINT 'AppSetting SageRueckmeldungAktiv eingefuegt.';
END
GO

-- 6. EF Migrations-History markieren
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260316062006_AddWorkOperationsPhase1'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260316062006_AddWorkOperationsPhase1', N'10.0.2');
    PRINT 'Migration 20260316062006_AddWorkOperationsPhase1 in History eingetragen.';
END
GO

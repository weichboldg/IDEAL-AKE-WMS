-- =============================================================================
-- 133_AddPickingAreasAndOrders.sql  (v1.88.0, Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen, Etappe E1)
-- Kommissionierbereiche (PickingAreas, PickingAreaRules) und Kommissionierauftraege (PickingOrders, PickingOrderLines) + Seed der Bereiche.
-- EINE EF-Migration 20261107193219_AddPickingAreasAndOrders fuer alle vier Tabellen; Nummer 134 der Buendel-Reservierung wird nicht gebraucht (zurueckgegeben).
-- Rein additiv. Idempotent (OBJECT_ID-/sys.indexes-Guards, DDL je Batch), History in separatem Batch. SQL/AgentJobs nicht betroffen.
-- Altbestand (PickingItems) NICHT hier: Einmal-Skript SQL/Einmalig/…_Kommissionierauftraege_Altbestand.sql (E5, ADR 0015).
-- =============================================================================
SET NOCOUNT ON;
-- Gefilterter UNIQUE-Index UX_PickingAreas_IsDefault braucht QUOTED_IDENTIFIER ON (sqlcmd setzt ihn ohne -I aus).
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'[dbo].[PickingAreas]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PickingAreas] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Kuerzel] nvarchar(10) NOT NULL,
        [Reihenfolge] int NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [IsDefault] bit NOT NULL,
        [IstVorkommissionierung] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(200) NOT NULL,
        [CreatedByWindows] nvarchar(200) NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [ModifiedByWindows] nvarchar(200) NULL,
        CONSTRAINT [PK_PickingAreas] PRIMARY KEY ([Id])
    );
    PRINT 'Tabelle PickingAreas erstellt.';
END
GO

IF OBJECT_ID(N'[dbo].[PickingAreaRules]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PickingAreaRules] (
        [Id] int NOT NULL IDENTITY,
        [PickingAreaId] int NOT NULL,
        [RuleType] tinyint NOT NULL,
        [Value] nvarchar(200) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(200) NOT NULL,
        [CreatedByWindows] nvarchar(200) NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [ModifiedByWindows] nvarchar(200) NULL,
        CONSTRAINT [PK_PickingAreaRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PickingAreaRules_PickingAreas_PickingAreaId] FOREIGN KEY ([PickingAreaId]) REFERENCES [dbo].[PickingAreas] ([Id]) ON DELETE CASCADE
    );
    PRINT 'Tabelle PickingAreaRules erstellt.';
END
GO

IF OBJECT_ID(N'[dbo].[PickingOrders]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PickingOrders] (
        [Id] int NOT NULL IDENTITY,
        [Nummer] nvarchar(20) NOT NULL,
        [Jahr] int NOT NULL,
        [LaufendeNummer] int NOT NULL,
        [Typ] tinyint NOT NULL,
        [ProductionOrderId] int NULL,
        [PickingAreaId] int NOT NULL,
        [Status] tinyint NOT NULL,
        [PickingPriority] int NULL,
        [AssignedPickerId] int NULL,
        [AssignedPickerName] nvarchar(200) NULL,
        [Bemerkung] nvarchar(1000) NULL,
        [UrsprungsauftragId] int NULL,
        [AbgeschlossenAm] datetime2 NULL,
        [AbgeschlossenVon] nvarchar(200) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(200) NOT NULL,
        [CreatedByWindows] nvarchar(200) NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [ModifiedByWindows] nvarchar(200) NULL,
        CONSTRAINT [PK_PickingOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PickingOrders_PickingAreas_PickingAreaId] FOREIGN KEY ([PickingAreaId]) REFERENCES [dbo].[PickingAreas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PickingOrders_PickingOrders_UrsprungsauftragId] FOREIGN KEY ([UrsprungsauftragId]) REFERENCES [dbo].[PickingOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PickingOrders_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PickingOrders_Users_AssignedPickerId] FOREIGN KEY ([AssignedPickerId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE SET NULL
    );
    PRINT 'Tabelle PickingOrders erstellt.';
END
GO

IF OBJECT_ID(N'[dbo].[PickingOrderLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PickingOrderLines] (
        [Id] int NOT NULL IDENTITY,
        [PickingOrderId] int NOT NULL,
        [ProductionOrderId] int NOT NULL,
        [ArticleNumber] nvarchar(100) NOT NULL,
        [BomPosition] nvarchar(50) NULL,
        [IsBaugruppe] bit NOT NULL,
        [SollMenge] decimal(18,3) NOT NULL,
        [IstMenge] decimal(18,3) NULL,
        [SourceStorageLocationId] int NULL,
        [Status] tinyint NOT NULL,
        [PickedAt] datetime2 NULL,
        [PickedBy] nvarchar(200) NULL,
        [PickedByWindows] nvarchar(200) NULL,
        [TransferredAt] datetime2 NULL,
        [StockMovementId] int NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(200) NOT NULL,
        [CreatedByWindows] nvarchar(200) NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [ModifiedByWindows] nvarchar(200) NULL,
        CONSTRAINT [PK_PickingOrderLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PickingOrderLines_PickingOrders_PickingOrderId] FOREIGN KEY ([PickingOrderId]) REFERENCES [dbo].[PickingOrders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PickingOrderLines_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PickingOrderLines_StockMovements_StockMovementId] FOREIGN KEY ([StockMovementId]) REFERENCES [dbo].[StockMovements] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PickingOrderLines_StorageLocations_SourceStorageLocationId] FOREIGN KEY ([SourceStorageLocationId]) REFERENCES [dbo].[StorageLocations] ([Id]) ON DELETE SET NULL
    );
    PRINT 'Tabelle PickingOrderLines erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PickingAreaRules_PickingAreaId' AND object_id = OBJECT_ID(N'dbo.PickingAreaRules'))
    CREATE INDEX [IX_PickingAreaRules_PickingAreaId] ON [dbo].[PickingAreaRules] ([PickingAreaId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PickingAreaRules_RuleType_Value' AND object_id = OBJECT_ID(N'dbo.PickingAreaRules'))
    CREATE UNIQUE INDEX [IX_PickingAreaRules_RuleType_Value] ON [dbo].[PickingAreaRules] ([RuleType], [Value]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PickingAreas_Kuerzel' AND object_id = OBJECT_ID(N'dbo.PickingAreas'))
    CREATE UNIQUE INDEX [IX_PickingAreas_Kuerzel] ON [dbo].[PickingAreas] ([Kuerzel]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PickingAreas_Name' AND object_id = OBJECT_ID(N'dbo.PickingAreas'))
    CREATE UNIQUE INDEX [IX_PickingAreas_Name] ON [dbo].[PickingAreas] ([Name]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PickingAreas_IsDefault' AND object_id = OBJECT_ID(N'dbo.PickingAreas'))
    CREATE UNIQUE INDEX [UX_PickingAreas_IsDefault] ON [dbo].[PickingAreas] ([IsDefault]) WHERE [IsDefault] = 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PickingOrderLines_PickingOrderId' AND object_id = OBJECT_ID(N'dbo.PickingOrderLines'))
    CREATE INDEX [IX_PickingOrderLines_PickingOrderId] ON [dbo].[PickingOrderLines] ([PickingOrderId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PickingOrderLines_ProductionOrderId_ArticleNumber_BomPosition' AND object_id = OBJECT_ID(N'dbo.PickingOrderLines'))
    CREATE INDEX [IX_PickingOrderLines_ProductionOrderId_ArticleNumber_BomPosition] ON [dbo].[PickingOrderLines] ([ProductionOrderId], [ArticleNumber], [BomPosition]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PickingOrderLines_SourceStorageLocationId' AND object_id = OBJECT_ID(N'dbo.PickingOrderLines'))
    CREATE INDEX [IX_PickingOrderLines_SourceStorageLocationId] ON [dbo].[PickingOrderLines] ([SourceStorageLocationId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PickingOrderLines_StockMovementId' AND object_id = OBJECT_ID(N'dbo.PickingOrderLines'))
    CREATE INDEX [IX_PickingOrderLines_StockMovementId] ON [dbo].[PickingOrderLines] ([StockMovementId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PickingOrders_AssignedPickerId' AND object_id = OBJECT_ID(N'dbo.PickingOrders'))
    CREATE INDEX [IX_PickingOrders_AssignedPickerId] ON [dbo].[PickingOrders] ([AssignedPickerId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PickingOrders_Jahr_LaufendeNummer' AND object_id = OBJECT_ID(N'dbo.PickingOrders'))
    CREATE UNIQUE INDEX [IX_PickingOrders_Jahr_LaufendeNummer] ON [dbo].[PickingOrders] ([Jahr], [LaufendeNummer]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PickingOrders_Nummer' AND object_id = OBJECT_ID(N'dbo.PickingOrders'))
    CREATE UNIQUE INDEX [IX_PickingOrders_Nummer] ON [dbo].[PickingOrders] ([Nummer]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PickingOrders_PickingAreaId' AND object_id = OBJECT_ID(N'dbo.PickingOrders'))
    CREATE INDEX [IX_PickingOrders_PickingAreaId] ON [dbo].[PickingOrders] ([PickingAreaId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PickingOrders_ProductionOrderId' AND object_id = OBJECT_ID(N'dbo.PickingOrders'))
    CREATE INDEX [IX_PickingOrders_ProductionOrderId] ON [dbo].[PickingOrders] ([ProductionOrderId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PickingOrders_Status' AND object_id = OBJECT_ID(N'dbo.PickingOrders'))
    CREATE INDEX [IX_PickingOrders_Status] ON [dbo].[PickingOrders] ([Status]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PickingOrders_UrsprungsauftragId' AND object_id = OBJECT_ID(N'dbo.PickingOrders'))
    CREATE INDEX [IX_PickingOrders_UrsprungsauftragId] ON [dbo].[PickingOrders] ([UrsprungsauftragId]);
GO

-- Seed (Spec: Lager = Standard, Glas, Vorkommissionierung); nur wenn noch kein Bereich existiert.
IF NOT EXISTS (SELECT 1 FROM [dbo].[PickingAreas])
BEGIN
    INSERT INTO [dbo].[PickingAreas] ([Name], [Kuerzel], [Reihenfolge], [IsActive], [IsDefault], [IstVorkommissionierung], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES (N'Lager', N'L', 10, 1, 1, 0, SYSDATETIME(), N'SQL133-Seed', LEFT(SUSER_SNAME(), 200)),
           (N'Glas', N'G', 20, 1, 0, 0, SYSDATETIME(), N'SQL133-Seed', LEFT(SUSER_SNAME(), 200)),
           (N'Vorkommissionierung', N'VK', 30, 1, 0, 1, SYSDATETIME(), N'SQL133-Seed', LEFT(SUSER_SNAME(), 200));
    PRINT 'Kommissionierbereiche Lager/Glas/Vorkommissionierung angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261107193219_AddPickingAreasAndOrders')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20261107193219_AddPickingAreasAndOrders', '10.0.2');
    PRINT 'Migration 20261107193219_AddPickingAreasAndOrders eingetragen.';
END
GO

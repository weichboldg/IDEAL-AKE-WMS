-- =============================================
-- 68_FaWorkStepsAndAttributes.sql (v1.22.0)
-- FA-Vorbau: Arbeitsgaenge-Katalog + FA-zu-AG + Merkmale
-- ersetzt ProductionOrderAssemblyGroups/-Specs (daten-erhaltend).
-- Idempotent, kann mehrfach ausgefuehrt werden (Reapply-fest).
--
-- Entspricht der EF-Migration 20260612102225_FaWorkStepsAndAttributes.
-- ACHTUNG deploy-kritisch (Spec 2026-06-12 §3):
--   - DB-Backup vor Produktions-Deploy ist Pflicht (Drop der alten Tabellen,
--     Down()/Rollback stellt nur leere Tabellen wieder her).
--   - Im selben Wartungsfenster MUSS der AgentJob
--     SQL/AgentJobs/01_Import_Produktionsauftraege.sql auf den Stand ohne
--     AssemblyGroups-MERGE gebracht werden, sonst schlaegt der FA-Import fehl.
--
-- Ausfuehrungs-Reihenfolge:
--   A) 8 neue Tabellen anlegen (OBJECT_ID-Guard, exakt wie EF-Migration)
--   B) Seed WorkSteps (VK/VL/VE/VT/VA)
--   C) Konvertierung ProductionOrderAssemblyGroups -> FaWorkSteps
--   D) Specs kopieren -> FaWorkStepSpecs
--   E) Seed Merkmale + Optionen
--   F) Seed Rolle 'vorbau'
--   G) Alte Tabellen droppen (Specs zuerst, dann Groups)
--   H) __EFMigrationsHistory-Eintrag
-- =============================================

-- =============================================
-- SECTION A: TABELLEN ANLEGEN (idempotent)
-- =============================================

-- A1. WorkSteps (Arbeitsgaenge-Katalog)
IF OBJECT_ID(N'dbo.WorkSteps', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WorkSteps] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [Code]              NVARCHAR(20)      NOT NULL,
        [Name]              NVARCHAR(100)     NOT NULL,
        [SearchString]      NVARCHAR(500)     NULL,
        [SortOrder]         INT               NOT NULL,
        [IsActive]          BIT               NOT NULL,
        [CreatedAt]         DATETIME2         NOT NULL,
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_WorkSteps] PRIMARY KEY CLUSTERED ([Id])
    );
    CREATE UNIQUE INDEX [IX_WorkSteps_Code] ON [dbo].[WorkSteps]([Code]);
    PRINT 'Tabelle WorkSteps erstellt.';
END
ELSE
    PRINT 'Tabelle WorkSteps bereits vorhanden - kein Anlegen.';
GO

-- A2. FaAttributeDefinitions (Merkmal-Katalog)
IF OBJECT_ID(N'dbo.FaAttributeDefinitions', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FaAttributeDefinitions] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [Name]              NVARCHAR(200)     NOT NULL,
        [AttributeType]     INT               NOT NULL,
        [SortOrder]         INT               NOT NULL,
        [IsActive]          BIT               NOT NULL,
        [CreatedAt]         DATETIME2         NOT NULL,
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_FaAttributeDefinitions] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT 'Tabelle FaAttributeDefinitions erstellt.';
END
ELSE
    PRINT 'Tabelle FaAttributeDefinitions bereits vorhanden - kein Anlegen.';
GO

-- A3. FaAttributeOptions (Dropdown-Werte)
IF OBJECT_ID(N'dbo.FaAttributeOptions', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FaAttributeOptions] (
        [Id]                       INT IDENTITY(1,1) NOT NULL,
        [FaAttributeDefinitionId]  INT               NOT NULL,
        [Value]                    NVARCHAR(200)     NOT NULL,
        [SortOrder]                INT               NOT NULL,
        [IsActive]                 BIT               NOT NULL,
        [CreatedAt]                DATETIME2         NOT NULL,
        [CreatedBy]                NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]         NVARCHAR(200)     NOT NULL,
        [ModifiedAt]               DATETIME2         NULL,
        [ModifiedBy]               NVARCHAR(200)     NULL,
        [ModifiedByWindows]        NVARCHAR(200)     NULL,
        CONSTRAINT [PK_FaAttributeOptions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_FaAttributeOptions_FaAttributeDefinitions_FaAttributeDefinitionId]
            FOREIGN KEY ([FaAttributeDefinitionId]) REFERENCES [dbo].[FaAttributeDefinitions]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_FaAttributeOptions_FaAttributeDefinitionId]
        ON [dbo].[FaAttributeOptions]([FaAttributeDefinitionId]);
    PRINT 'Tabelle FaAttributeOptions erstellt.';
END
ELSE
    PRINT 'Tabelle FaAttributeOptions bereits vorhanden - kein Anlegen.';
GO

-- A4. FaAttributeWorkSteps (Merkmal -> AG, N:M)
IF OBJECT_ID(N'dbo.FaAttributeWorkSteps', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FaAttributeWorkSteps] (
        [Id]                       INT IDENTITY(1,1) NOT NULL,
        [FaAttributeDefinitionId]  INT               NOT NULL,
        [WorkStepId]               INT               NOT NULL,
        [CreatedAt]                DATETIME2         NOT NULL,
        [CreatedBy]                NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]         NVARCHAR(200)     NOT NULL,
        [ModifiedAt]               DATETIME2         NULL,
        [ModifiedBy]               NVARCHAR(200)     NULL,
        [ModifiedByWindows]        NVARCHAR(200)     NULL,
        CONSTRAINT [PK_FaAttributeWorkSteps] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_FaAttributeWorkSteps_FaAttributeDefinitions_FaAttributeDefinitionId]
            FOREIGN KEY ([FaAttributeDefinitionId]) REFERENCES [dbo].[FaAttributeDefinitions]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_FaAttributeWorkSteps_WorkSteps_WorkStepId]
            FOREIGN KEY ([WorkStepId]) REFERENCES [dbo].[WorkSteps]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_FaAttributeWorkSteps_FaAttributeDefinitionId_WorkStepId]
        ON [dbo].[FaAttributeWorkSteps]([FaAttributeDefinitionId], [WorkStepId]);
    CREATE INDEX [IX_FaAttributeWorkSteps_WorkStepId]
        ON [dbo].[FaAttributeWorkSteps]([WorkStepId]);
    PRINT 'Tabelle FaAttributeWorkSteps erstellt.';
END
ELSE
    PRINT 'Tabelle FaAttributeWorkSteps bereits vorhanden - kein Anlegen.';
GO

-- A5. FaWorkSteps (FA -> AG)
IF OBJECT_ID(N'dbo.FaWorkSteps', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FaWorkSteps] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [ProductionOrderId] INT               NOT NULL,
        [WorkStepId]        INT               NOT NULL,
        [IsCompleted]       BIT               NOT NULL,
        [CompletedAt]       DATETIME2         NULL,
        [CompletedBy]       NVARCHAR(200)     NULL,
        [Source]            NVARCHAR(20)      NOT NULL,
        [IsRemoved]         BIT               NOT NULL,
        [CreatedAt]         DATETIME2         NOT NULL,
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_FaWorkSteps] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_FaWorkSteps_ProductionOrders_ProductionOrderId]
            FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_FaWorkSteps_WorkSteps_WorkStepId]
            FOREIGN KEY ([WorkStepId]) REFERENCES [dbo].[WorkSteps]([Id]) -- NO ACTION (Restrict)
    );
    CREATE INDEX [IX_FaWorkSteps_ProductionOrderId_IsRemoved]
        ON [dbo].[FaWorkSteps]([ProductionOrderId], [IsRemoved]);
    CREATE UNIQUE INDEX [IX_FaWorkSteps_ProductionOrderId_WorkStepId]
        ON [dbo].[FaWorkSteps]([ProductionOrderId], [WorkStepId]);
    CREATE INDEX [IX_FaWorkSteps_WorkStepId]
        ON [dbo].[FaWorkSteps]([WorkStepId]);
    PRINT 'Tabelle FaWorkSteps erstellt.';
END
ELSE
    PRINT 'Tabelle FaWorkSteps bereits vorhanden - kein Anlegen.';
GO

-- A6. FaWorkStepSpecs (Freitext-Specs, Nachfolger AssemblyGroupSpecs)
IF OBJECT_ID(N'dbo.FaWorkStepSpecs', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FaWorkStepSpecs] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [FaWorkStepId]      INT               NOT NULL,
        [ArticleId]         INT               NULL,
        [Description]       NVARCHAR(500)     NOT NULL,
        [Quantity]          DECIMAL(18,3)     NULL,
        [Notes]             NVARCHAR(MAX)     NULL,
        [SortOrder]         INT               NOT NULL,
        [CreatedAt]         DATETIME2         NOT NULL,
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_FaWorkStepSpecs] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_FaWorkStepSpecs_Articles_ArticleId]
            FOREIGN KEY ([ArticleId]) REFERENCES [dbo].[Articles]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_FaWorkStepSpecs_FaWorkSteps_FaWorkStepId]
            FOREIGN KEY ([FaWorkStepId]) REFERENCES [dbo].[FaWorkSteps]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_FaWorkStepSpecs_ArticleId]
        ON [dbo].[FaWorkStepSpecs]([ArticleId]);
    CREATE INDEX [IX_FaWorkStepSpecs_FaWorkStepId]
        ON [dbo].[FaWorkStepSpecs]([FaWorkStepId]);
    PRINT 'Tabelle FaWorkStepSpecs erstellt.';
END
ELSE
    PRINT 'Tabelle FaWorkStepSpecs bereits vorhanden - kein Anlegen.';
GO

-- A7. FaAttributeValues (eingegebene Werte je FA)
IF OBJECT_ID(N'dbo.FaAttributeValues', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FaAttributeValues] (
        [Id]                       INT IDENTITY(1,1) NOT NULL,
        [ProductionOrderId]        INT               NOT NULL,
        [FaAttributeDefinitionId]  INT               NOT NULL,
        [SelectedOptionId]         INT               NULL,
        [BooleanValue]             BIT               NULL,
        [CreatedAt]                DATETIME2         NOT NULL,
        [CreatedBy]                NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]         NVARCHAR(200)     NOT NULL,
        [ModifiedAt]               DATETIME2         NULL,
        [ModifiedBy]               NVARCHAR(200)     NULL,
        [ModifiedByWindows]        NVARCHAR(200)     NULL,
        CONSTRAINT [PK_FaAttributeValues] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_FaAttributeValues_FaAttributeDefinitions_FaAttributeDefinitionId]
            FOREIGN KEY ([FaAttributeDefinitionId]) REFERENCES [dbo].[FaAttributeDefinitions]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_FaAttributeValues_FaAttributeOptions_SelectedOptionId]
            FOREIGN KEY ([SelectedOptionId]) REFERENCES [dbo].[FaAttributeOptions]([Id]), -- NO ACTION (Restrict)
        CONSTRAINT [FK_FaAttributeValues_ProductionOrders_ProductionOrderId]
            FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_FaAttributeValues_FaAttributeDefinitionId]
        ON [dbo].[FaAttributeValues]([FaAttributeDefinitionId]);
    CREATE UNIQUE INDEX [IX_FaAttributeValues_ProductionOrderId_FaAttributeDefinitionId]
        ON [dbo].[FaAttributeValues]([ProductionOrderId], [FaAttributeDefinitionId]);
    CREATE INDEX [IX_FaAttributeValues_SelectedOptionId]
        ON [dbo].[FaAttributeValues]([SelectedOptionId]);
    PRINT 'Tabelle FaAttributeValues erstellt.';
END
ELSE
    PRINT 'Tabelle FaAttributeValues bereits vorhanden - kein Anlegen.';
GO

-- A8. ProductionWorkplaceWorkSteps (Werkbank -> AG, N:M)
IF OBJECT_ID(N'dbo.ProductionWorkplaceWorkSteps', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductionWorkplaceWorkSteps] (
        [Id]                    INT IDENTITY(1,1) NOT NULL,
        [ProductionWorkplaceId] INT               NOT NULL,
        [WorkStepId]            INT               NOT NULL,
        [CreatedAt]             DATETIME2         NOT NULL,
        [CreatedBy]             NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]      NVARCHAR(200)     NOT NULL,
        [ModifiedAt]            DATETIME2         NULL,
        [ModifiedBy]            NVARCHAR(200)     NULL,
        [ModifiedByWindows]     NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionWorkplaceWorkSteps] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ProductionWorkplaceWorkSteps_ProductionWorkplaces_ProductionWorkplaceId]
            FOREIGN KEY ([ProductionWorkplaceId]) REFERENCES [dbo].[ProductionWorkplaces]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductionWorkplaceWorkSteps_WorkSteps_WorkStepId]
            FOREIGN KEY ([WorkStepId]) REFERENCES [dbo].[WorkSteps]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_ProductionWorkplaceWorkSteps_ProductionWorkplaceId_WorkStepId]
        ON [dbo].[ProductionWorkplaceWorkSteps]([ProductionWorkplaceId], [WorkStepId]);
    CREATE INDEX [IX_ProductionWorkplaceWorkSteps_WorkStepId]
        ON [dbo].[ProductionWorkplaceWorkSteps]([WorkStepId]);
    PRINT 'Tabelle ProductionWorkplaceWorkSteps erstellt.';
END
ELSE
    PRINT 'Tabelle ProductionWorkplaceWorkSteps bereits vorhanden - kein Anlegen.';
GO

-- =============================================
-- SECTION B: SEED WORKSTEPS (VK-VA, idempotent)
-- =============================================
INSERT INTO [dbo].[WorkSteps] ([Code], [Name], [SearchString], [SortOrder], [IsActive], [CreatedAt], [CreatedBy], [CreatedByWindows])
SELECT v.Code, v.Name, NULL, v.SortOrder, 1, GETDATE(), 'Migration', 'Migration'
FROM (VALUES ('VK','Kuehlung',1),('VL','Lueftung',2),('VE','Elektro',3),('VT','Tueren',4),('VA','Aufbau',5)) v(Code, Name, SortOrder)
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[WorkSteps] w WHERE w.[Code] = v.Code);
PRINT 'Seed WorkSteps (VK-VA) abgeschlossen.';
GO

-- =============================================
-- SECTION C: KONVERTIERUNG AssemblyGroups -> FaWorkSteps
-- (IsApplicable=1 ODER Spec-Traeger; Specs an inaktiven Gruppen mit IsRemoved=1)
-- =============================================
IF OBJECT_ID(N'dbo.ProductionOrderAssemblyGroups', N'U') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[FaWorkSteps] ([ProductionOrderId], [WorkStepId], [IsCompleted], [CompletedAt], [CompletedBy], [Source], [IsRemoved], [CreatedAt], [CreatedBy], [CreatedByWindows])
    SELECT g.ProductionOrderId, w.Id, g.IsCompleted, NULL, NULL, 'Manual',
           CASE WHEN g.IsApplicable = 1 THEN 0 ELSE 1 END,
           GETDATE(), 'Migration', 'Migration'
    FROM [dbo].[ProductionOrderAssemblyGroups] g
    JOIN [dbo].[WorkSteps] w ON w.[Code] = g.GroupKey
    WHERE (g.IsApplicable = 1
           OR EXISTS (SELECT 1 FROM [dbo].[ProductionOrderAssemblyGroupSpecs] s WHERE s.AssemblyGroupId = g.Id))
      AND NOT EXISTS (SELECT 1 FROM [dbo].[FaWorkSteps] f
                      WHERE f.ProductionOrderId = g.ProductionOrderId AND f.WorkStepId = w.Id);
    PRINT 'Konvertierung ProductionOrderAssemblyGroups -> FaWorkSteps abgeschlossen.';
END
ELSE
    PRINT 'ProductionOrderAssemblyGroups nicht vorhanden - Konvertierung uebersprungen.';
GO

-- =============================================
-- SECTION D: SPECS KOPIEREN -> FaWorkStepSpecs
-- =============================================
IF OBJECT_ID(N'dbo.ProductionOrderAssemblyGroupSpecs', N'U') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[FaWorkStepSpecs] ([FaWorkStepId], [ArticleId], [Description], [Quantity], [Notes], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows], [ModifiedAt], [ModifiedBy], [ModifiedByWindows])
    SELECT f.Id, s.ArticleId, s.Description, s.Quantity, s.Notes, s.SortOrder,
           s.CreatedAt, s.CreatedBy, s.CreatedByWindows, s.ModifiedAt, s.ModifiedBy, s.ModifiedByWindows
    FROM [dbo].[ProductionOrderAssemblyGroupSpecs] s
    JOIN [dbo].[ProductionOrderAssemblyGroups] g ON g.Id = s.AssemblyGroupId
    JOIN [dbo].[WorkSteps] w ON w.[Code] = g.GroupKey
    JOIN [dbo].[FaWorkSteps] f ON f.ProductionOrderId = g.ProductionOrderId AND f.WorkStepId = w.Id;
    PRINT 'Specs-Kopie -> FaWorkStepSpecs abgeschlossen.';
END
ELSE
    PRINT 'ProductionOrderAssemblyGroupSpecs nicht vorhanden - Specs-Kopie uebersprungen.';
GO

-- =============================================
-- SECTION E: SEED MERKMALE + OPTIONEN (idempotent)
-- =============================================
INSERT INTO [dbo].[FaAttributeDefinitions] ([Name], [AttributeType], [SortOrder], [IsActive], [CreatedAt], [CreatedBy], [CreatedByWindows])
SELECT v.Name, v.AttrType, v.SortOrder, 1, GETDATE(), 'Migration', 'Migration'
FROM (VALUES ('Verdampfergroesse',1,1),('Leitungsausgang',1,2),('Verdampfergehaeuse',1,3),('Ventil aussenliegend',0,4)) v(Name, AttrType, SortOrder)
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[FaAttributeDefinitions] d WHERE d.[Name] = v.Name);

INSERT INTO [dbo].[FaAttributeOptions] ([FaAttributeDefinitionId], [Value], [SortOrder], [IsActive], [CreatedAt], [CreatedBy], [CreatedByWindows])
SELECT d.Id, v.Value, v.SortOrder, 1, GETDATE(), 'Migration', 'Migration'
FROM (VALUES
 ('Verdampfergroesse','UKW 2/1',1),('Verdampfergroesse','UKW 3/1',2),('Verdampfergroesse','UKW 4/1',3),
 ('Verdampfergroesse','UKW 5/1 (Euro 4)',4),('Verdampfergroesse','UKW 6/1',5),('Verdampfergroesse','Euro 2',6),
 ('Verdampfergroesse','Euro 3',7),('Verdampfergroesse','Caleo 80',8),('Verdampfergroesse','Caleo 120',9),
 ('Verdampfergroesse','Breite 60',10),
 ('Leitungsausgang','Standard',1),('Leitungsausgang','RG',2),('Leitungsausgang','Links',3),('Leitungsausgang','Links RG',4),
 ('Verdampfergehaeuse','2/1 Standard',1),('Verdampfergehaeuse','3/1 RG',2),('Verdampfergehaeuse','Sonder',3)
) v(DefName, Value, SortOrder)
JOIN [dbo].[FaAttributeDefinitions] d ON d.[Name] = v.DefName
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[FaAttributeOptions] o WHERE o.[FaAttributeDefinitionId] = d.Id AND o.[Value] = v.Value);
PRINT 'Seed Merkmale + Optionen abgeschlossen.';
GO

-- =============================================
-- SECTION F: SEED ROLLE vorbau (idempotent)
-- =============================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'vorbau')
BEGIN
    INSERT INTO [dbo].[Roles] ([Key], [Name], [Description], [AdGroup], [IsSystem], [SortOrder],
                               [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('vorbau', 'Vorbau', 'FA-Abarbeitungsliste: Vorbau-Arbeitsgaenge einsehen und abhaken', NULL, 1,
            (SELECT MAX([SortOrder]) + 1 FROM [dbo].[Roles]), GETDATE(), 'Migration', 'Migration');
    PRINT 'Rolle vorbau angelegt.';
END
ELSE
    PRINT 'Rolle vorbau existiert bereits - uebersprungen.';
GO

-- =============================================
-- SECTION G: ALTE TABELLEN DROPPEN (nach Konvertierung)
-- =============================================
IF OBJECT_ID(N'dbo.ProductionOrderAssemblyGroupSpecs', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[ProductionOrderAssemblyGroupSpecs];
    PRINT 'Tabelle ProductionOrderAssemblyGroupSpecs geloescht.';
END
GO

IF OBJECT_ID(N'dbo.ProductionOrderAssemblyGroups', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[ProductionOrderAssemblyGroups];
    PRINT 'Tabelle ProductionOrderAssemblyGroups geloescht.';
END
GO

-- =============================================
-- SECTION H: EF MIGRATIONS HISTORY
-- =============================================
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260612102225_FaWorkStepsAndAttributes')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260612102225_FaWorkStepsAndAttributes', '10.0.2');
GO

PRINT '68_FaWorkStepsAndAttributes abgeschlossen.';
GO

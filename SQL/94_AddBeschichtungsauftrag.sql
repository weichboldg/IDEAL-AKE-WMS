-- SQL/94_AddBeschichtungsauftrag.sql
-- Migration 94: AddBeschichtungsauftrag (IDEAL Beschichtungsauftrag-Ausbau Teil 3, v1.50.0)
-- Rein additiv, idempotent, NICHT daten-destruktiv: zwei neue Tabellen fuer den gespeicherten Beschichtungsauftrag.
--   dbo.Beschichtungsauftraege          Kopf je HauptFA, Nummer BA-JJJJ-NNNNN (UNIQUE), Status, Selbstverweis Reklamation
--   dbo.BeschichtungsauftragPositionen  Herkunftsschluessel + Snapshot (bewusst KEIN FK auf FaHierarchyNodes)
-- Objektnamen exakt wie die EF-Migration 20261012073551_AddBeschichtungsauftrag, damit App-Start-Migrate() und dieses
-- Skript dasselbe Schema ergeben (fallstricke "EF-Migration UND idempotentes SQL-Skript").
-- Reihenfolge Deploy: dieses Skript VOR dem IIS-Recycle der neuen Web-Binaries; der History-Insert unten verhindert,
-- dass Migrate() die Tabellen ein zweites Mal anlegen will. DDL je in eigenem Batch, History in separatem Batch.
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.Beschichtungsauftraege', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Beschichtungsauftraege] (
        [Id]                 INT IDENTITY(1,1) NOT NULL,
        [Nummer]             NVARCHAR(20)   NOT NULL,
        [Jahr]               INT            NOT NULL,
        [LaufendeNummer]     INT            NOT NULL,
        [HauptFA]            INT            NOT NULL,
        [Status]             TINYINT        NOT NULL,
        [Dienstleister]      NVARCHAR(200)  NULL,
        [Ral]                NVARCHAR(200)  NULL,
        [StartBeschichtung]  DATETIME2      NULL,
        [BeschichtenRetour]  DATETIME2      NULL,
        [Bemerkung]          NVARCHAR(1000) NULL,
        [UrsprungsauftragId] INT            NULL,
        [UebergebenAt]       DATETIME2      NULL,
        [UebergebenByUserId] INT            NULL,
        [ErledigtAt]         DATETIME2      NULL,
        [ErledigtByUserId]   INT            NULL,
        [StorniertAt]        DATETIME2      NULL,
        [StorniertByUserId]  INT            NULL,
        [RowVersion]         ROWVERSION     NOT NULL,
        [CreatedAt]          DATETIME2      NOT NULL,
        [CreatedBy]          NVARCHAR(200)  NOT NULL,
        [CreatedByWindows]   NVARCHAR(200)  NOT NULL,
        [ModifiedAt]         DATETIME2      NULL,
        [ModifiedBy]         NVARCHAR(200)  NULL,
        [ModifiedByWindows]  NVARCHAR(200)  NULL,
        CONSTRAINT [PK_Beschichtungsauftraege] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Beschichtungsauftraege_Beschichtungsauftraege_UrsprungsauftragId]
            FOREIGN KEY ([UrsprungsauftragId]) REFERENCES [dbo].[Beschichtungsauftraege] ([Id])
    );
    PRINT 'Tabelle Beschichtungsauftraege angelegt.';
END
GO

IF OBJECT_ID(N'dbo.BeschichtungsauftragPositionen', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BeschichtungsauftragPositionen] (
        [Id]                     INT IDENTITY(1,1) NOT NULL,
        [BeschichtungsauftragId] INT            NOT NULL,
        [HauptFA]                INT            NOT NULL,
        [VaterFA]                INT            NULL,
        [SubFA]                  INT            NOT NULL,
        [Position]               INT            NULL,
        [HauptArtnr]             NVARCHAR(200)  NULL,
        [Artnr]                  NVARCHAR(200)  NULL,
        [Matchcode]              NVARCHAR(200)  NULL,
        [Menge]                  DECIMAL(18,4)  NOT NULL,
        [Breite]                 DECIMAL(18,4)  NOT NULL,
        [Hoehe]                  DECIMAL(18,4)  NOT NULL,
        [Tiefe]                  DECIMAL(18,4)  NOT NULL,
        [CreatedAt]              DATETIME2      NOT NULL,
        [CreatedBy]              NVARCHAR(200)  NOT NULL,
        [CreatedByWindows]       NVARCHAR(200)  NOT NULL,
        [ModifiedAt]             DATETIME2      NULL,
        [ModifiedBy]             NVARCHAR(200)  NULL,
        [ModifiedByWindows]      NVARCHAR(200)  NULL,
        CONSTRAINT [PK_BeschichtungsauftragPositionen] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BeschichtungsauftragPositionen_Beschichtungsauftraege_BeschichtungsauftragId]
            FOREIGN KEY ([BeschichtungsauftragId]) REFERENCES [dbo].[Beschichtungsauftraege] ([Id]) ON DELETE CASCADE
    );
    PRINT 'Tabelle BeschichtungsauftragPositionen angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Beschichtungsauftraege_HauptFA' AND object_id = OBJECT_ID(N'dbo.Beschichtungsauftraege'))
    CREATE INDEX [IX_Beschichtungsauftraege_HauptFA] ON [dbo].[Beschichtungsauftraege] ([HauptFA]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Beschichtungsauftraege_Jahr_LaufendeNummer' AND object_id = OBJECT_ID(N'dbo.Beschichtungsauftraege'))
    CREATE UNIQUE INDEX [IX_Beschichtungsauftraege_Jahr_LaufendeNummer] ON [dbo].[Beschichtungsauftraege] ([Jahr], [LaufendeNummer]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Beschichtungsauftraege_Nummer' AND object_id = OBJECT_ID(N'dbo.Beschichtungsauftraege'))
    CREATE UNIQUE INDEX [IX_Beschichtungsauftraege_Nummer] ON [dbo].[Beschichtungsauftraege] ([Nummer]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Beschichtungsauftraege_Status' AND object_id = OBJECT_ID(N'dbo.Beschichtungsauftraege'))
    CREATE INDEX [IX_Beschichtungsauftraege_Status] ON [dbo].[Beschichtungsauftraege] ([Status]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Beschichtungsauftraege_UrsprungsauftragId' AND object_id = OBJECT_ID(N'dbo.Beschichtungsauftraege'))
    CREATE INDEX [IX_Beschichtungsauftraege_UrsprungsauftragId] ON [dbo].[Beschichtungsauftraege] ([UrsprungsauftragId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BeschichtungsauftragPositionen_BeschichtungsauftragId' AND object_id = OBJECT_ID(N'dbo.BeschichtungsauftragPositionen'))
    CREATE INDEX [IX_BeschichtungsauftragPositionen_BeschichtungsauftragId] ON [dbo].[BeschichtungsauftragPositionen] ([BeschichtungsauftragId]);
GO

-- Herkunftsschluessel bewusst NICHT unique (Reklamationen, stornierte Auftraege) — die Doppelvergabe prueft der Workflow.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BeschichtungsauftragPositionen_HauptFA_VaterFA_SubFA_Position' AND object_id = OBJECT_ID(N'dbo.BeschichtungsauftragPositionen'))
    CREATE INDEX [IX_BeschichtungsauftragPositionen_HauptFA_VaterFA_SubFA_Position]
        ON [dbo].[BeschichtungsauftragPositionen] ([HauptFA], [VaterFA], [SubFA], [Position]);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20261012073551_AddBeschichtungsauftrag')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261012073551_AddBeschichtungsauftrag', '10.0.2');
END
GO

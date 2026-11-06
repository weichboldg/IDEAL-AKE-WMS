-- =============================================================================
-- 103_AddSagePpsMitarbeiterAndStillstandsgruende.sql  (v1.73.0, Spec 2026-11-06-sage-sdata-stammdaten-sync, Etappe E6b)
-- 1) Cache-Tabelle [SagePpsMitarbeiter] (UNIQUE Nummer): Sage-PPS-Mitarbeiter aus SData PpsMitarbeiterInfo, Vollersatz je Lauf
--    durch den PpsMitarbeiterSyncService (Sync:PpsMitarbeiterEnabled, Default false). Kein Audit (Cache, SyncedAt).
-- 2) Stammtabelle [Stillstandsgruende] (UNIQUE Nummer, AuditableEntity, SageMissingSince) aus SData PpsStillstandsbegruendungenInfo
--    (Sync:StillstandsgruendeEnabled, Default false). Nie geloescht.
-- 3) [BdeOperators].[SageMitarbeiternummer] NVARCHAR(20) NULL + gefilterter UNIQUE-Index (IS NOT NULL); nur manuelle Zuordnung (G14).
-- Laengen laut SData-$schema (P-S1, 2026-11-06). Keine FKs. Rein additiv, keine Bestandsbefuellung, nicht daten-destruktiv;
-- DB-Backup vor Deploy wie ueblich.
-- Nummer: 103 = naechste freie im Buendel (Reservierung 104 laut Spec rueckt nach, weil E5/102 entfiel); laeuft NACH
-- 102_AddProductionWorkplaceSdataFields.sql (G17, Deploy-Liste beachten).
-- Objektnamen wie die EF-Migration 20261106193321_AddSagePpsMitarbeiterAndStillstandsgruende.
-- Idempotent: zweiter Lauf aendert nichts. DDL je in eigenem Batch, History-Insert in separatem Batch.
-- =============================================================================
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: Tabelle dbo.__EFMigrationsHistory fehlt - zuerst SQL/00_FreshInstall.sql ausfuehren. Keine Aenderung, keine History-Zeile.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH('dbo.BdeOperators', 'SageMitarbeiternummer') IS NULL
BEGIN
    ALTER TABLE [dbo].[BdeOperators] ADD [SageMitarbeiternummer] NVARCHAR(20) NULL;
    PRINT 'Spalte BdeOperators.SageMitarbeiternummer hinzugefuegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeOperators_SageMitarbeiternummer' AND object_id = OBJECT_ID('dbo.BdeOperators'))
BEGIN
    CREATE UNIQUE INDEX [IX_BdeOperators_SageMitarbeiternummer] ON [dbo].[BdeOperators] ([SageMitarbeiternummer])
        WHERE [SageMitarbeiternummer] IS NOT NULL;
    PRINT 'Index IX_BdeOperators_SageMitarbeiternummer angelegt.';
END
GO

IF OBJECT_ID(N'[dbo].[SagePpsMitarbeiter]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SagePpsMitarbeiter] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [Nummer]            NVARCHAR(20)      NOT NULL,
        [Matchcode]         NVARCHAR(50)      NULL,
        [Vorname]           NVARCHAR(35)      NULL,
        [Nachname]          NVARCHAR(35)      NULL,
        [Gruppe]            NVARCHAR(10)      NULL,
        [Kartennummer]      NVARCHAR(20)      NULL,
        [ExterneNummer]     INT               NULL,
        [Aktiv]             BIT               NULL,
        [Schichtplannummer] NVARCHAR(31)      NULL,
        [Kostenstelle]      NVARCHAR(20)      NULL,
        [UserName]          NVARCHAR(128)     NULL,
        [SyncedAt]          DATETIME2         NOT NULL,
        CONSTRAINT [PK_SagePpsMitarbeiter] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT 'Tabelle SagePpsMitarbeiter erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SagePpsMitarbeiter_Nummer' AND object_id = OBJECT_ID('dbo.SagePpsMitarbeiter'))
BEGIN
    CREATE UNIQUE INDEX [IX_SagePpsMitarbeiter_Nummer] ON [dbo].[SagePpsMitarbeiter] ([Nummer]);
    PRINT 'Index IX_SagePpsMitarbeiter_Nummer angelegt.';
END
GO

IF OBJECT_ID(N'[dbo].[Stillstandsgruende]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Stillstandsgruende] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [Nummer]            NVARCHAR(31)      NOT NULL,
        [Bezeichnung]       NVARCHAR(50)      NULL,
        [SageMissingSince]  DATETIME2         NULL,
        [CreatedAt]         DATETIME2         NOT NULL,
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_Stillstandsgruende] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT 'Tabelle Stillstandsgruende erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Stillstandsgruende_Nummer' AND object_id = OBJECT_ID('dbo.Stillstandsgruende'))
BEGIN
    CREATE UNIQUE INDEX [IX_Stillstandsgruende_Nummer] ON [dbo].[Stillstandsgruende] ([Nummer]);
    PRINT 'Index IX_Stillstandsgruende_Nummer angelegt.';
END
GO

-- Migrations-History (separater Batch, exakte MigrationId)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261106193321_AddSagePpsMitarbeiterAndStillstandsgruende')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261106193321_AddSagePpsMitarbeiterAndStillstandsgruende', '10.0.2');
    PRINT 'Migration 20261106193321_AddSagePpsMitarbeiterAndStillstandsgruende eingetragen.';
END
GO

SET NOEXEC OFF;
GO

-- =============================================================================
-- 107_AddSageStammdatenOutboxAndUebergabeStatus.sql  (v1.73.0, Spec 2026-11-06-sage-sdata-stammdaten-sync, Etappe E9 / F11)
-- Vorbereitung WMS -> Sage-Stammdatenuebergabe, OHNE Senden (kein Worker, kein Sage-Aufruf):
--   * Tabelle [SageStammdatenOutbox] (AuditableEntity; EntityType, EntityKey = WMS-Id, PayloadJson, Status, AttemptCount, LastError)
--     Status int wie SageBookingQueueItems (0 Offen, 1 Gesendet, 2 Bestaetigt, 3 Fehler).
--   * StorageLocations + SageUebergabeStatus INT NOT NULL DEFAULT 0, SageUebergabeAm, SageUebergabeFehler
--   * BdeOperators     + Source NVARCHAR(20) NOT NULL DEFAULT 'Manual' + dieselben drei Uebergabe-Spalten
--   SageUebergabeStatus: 0 NichtNoetig, 1 Offen, 2 Uebergeben, 3 Fehler. Der DEFAULT fuellt den Bestand mit 0 (NichtNoetig)
--   bzw. 'Manual' -> Verhalten nach Deploy unveraendert (Lagerplatz-Abgleich meldet Bestandsplaetze weiter als Konflikt).
-- Rein additiv, nicht daten-destruktiv; DB-Backup vor Deploy wie ueblich. SQL/AgentJobs nicht betroffen.
-- Nummer: 107 = naechste freie im Buendel (Reservierung 108 laut Spec rueckt nach, weil E5/102 entfiel);
--   laeuft NACH 106_AddArticleSuppliers.sql (G17, Deploy-Liste beachten).
-- Objektnamen wie die EF-Migration 20261106202615_AddSageStammdatenOutboxAndUebergabeStatus.
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

IF COL_LENGTH('dbo.StorageLocations', 'SageUebergabeStatus') IS NULL
BEGIN
    ALTER TABLE [dbo].[StorageLocations] ADD [SageUebergabeStatus] INT NOT NULL
        CONSTRAINT [DF_StorageLocations_SageUebergabeStatus] DEFAULT (0);
    PRINT 'Spalte StorageLocations.SageUebergabeStatus hinzugefuegt (Bestand = 0 NichtNoetig).';
END
GO

IF COL_LENGTH('dbo.StorageLocations', 'SageUebergabeAm') IS NULL
BEGIN
    ALTER TABLE [dbo].[StorageLocations] ADD [SageUebergabeAm] DATETIME2 NULL;
    PRINT 'Spalte StorageLocations.SageUebergabeAm hinzugefuegt.';
END
GO

IF COL_LENGTH('dbo.StorageLocations', 'SageUebergabeFehler') IS NULL
BEGIN
    ALTER TABLE [dbo].[StorageLocations] ADD [SageUebergabeFehler] NVARCHAR(1000) NULL;
    PRINT 'Spalte StorageLocations.SageUebergabeFehler hinzugefuegt.';
END
GO

IF COL_LENGTH('dbo.BdeOperators', 'Source') IS NULL
BEGIN
    ALTER TABLE [dbo].[BdeOperators] ADD [Source] NVARCHAR(20) NOT NULL
        CONSTRAINT [DF_BdeOperators_Source] DEFAULT (N'Manual');
    PRINT 'Spalte BdeOperators.Source hinzugefuegt (Bestand = Manual).';
END
GO

IF COL_LENGTH('dbo.BdeOperators', 'SageUebergabeStatus') IS NULL
BEGIN
    ALTER TABLE [dbo].[BdeOperators] ADD [SageUebergabeStatus] INT NOT NULL
        CONSTRAINT [DF_BdeOperators_SageUebergabeStatus] DEFAULT (0);
    PRINT 'Spalte BdeOperators.SageUebergabeStatus hinzugefuegt (Bestand = 0 NichtNoetig).';
END
GO

IF COL_LENGTH('dbo.BdeOperators', 'SageUebergabeAm') IS NULL
BEGIN
    ALTER TABLE [dbo].[BdeOperators] ADD [SageUebergabeAm] DATETIME2 NULL;
    PRINT 'Spalte BdeOperators.SageUebergabeAm hinzugefuegt.';
END
GO

IF COL_LENGTH('dbo.BdeOperators', 'SageUebergabeFehler') IS NULL
BEGIN
    ALTER TABLE [dbo].[BdeOperators] ADD [SageUebergabeFehler] NVARCHAR(1000) NULL;
    PRINT 'Spalte BdeOperators.SageUebergabeFehler hinzugefuegt.';
END
GO

IF OBJECT_ID(N'[dbo].[SageStammdatenOutbox]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SageStammdatenOutbox] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [EntityType]        NVARCHAR(50)      NOT NULL,
        [EntityKey]         NVARCHAR(100)     NOT NULL,
        [PayloadJson]       NVARCHAR(MAX)     NOT NULL,
        [Status]            INT               NOT NULL,
        [AttemptCount]      INT               NOT NULL,
        [LastError]         NVARCHAR(1000)    NULL,
        [CreatedAt]         DATETIME2         NOT NULL,
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_SageStammdatenOutbox] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT 'Tabelle SageStammdatenOutbox erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SageStammdatenOutbox_EntityType_EntityKey' AND object_id = OBJECT_ID('dbo.SageStammdatenOutbox'))
BEGIN
    CREATE INDEX [IX_SageStammdatenOutbox_EntityType_EntityKey] ON [dbo].[SageStammdatenOutbox] ([EntityType], [EntityKey]);
    PRINT 'Index IX_SageStammdatenOutbox_EntityType_EntityKey angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SageStammdatenOutbox_Status' AND object_id = OBJECT_ID('dbo.SageStammdatenOutbox'))
BEGIN
    CREATE INDEX [IX_SageStammdatenOutbox_Status] ON [dbo].[SageStammdatenOutbox] ([Status]);
    PRINT 'Index IX_SageStammdatenOutbox_Status angelegt.';
END
GO

-- Migrations-History (separater Batch, exakte MigrationId)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261106202615_AddSageStammdatenOutboxAndUebergabeStatus')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261106202615_AddSageStammdatenOutboxAndUebergabeStatus', '10.0.2');
    PRINT 'Migration 20261106202615_AddSageStammdatenOutboxAndUebergabeStatus eingetragen.';
END
GO

SET NOEXEC OFF;
GO

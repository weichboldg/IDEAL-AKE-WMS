-- =============================================================================
-- 154_AddBdeTeileliste.sql  (v1.99.32, Spec 2026-11-13-bde-vereinfacht-teileliste-blechfertigung E1, ADR 0030)
-- BDE-Teileliste (vereinfacht) fuer die Blechfertigung (IDEAL):
--   * Spalte dbo.ProductionWorkplaces.BdeTeileliste (Werkbank-Option, BIT NOT NULL DEFAULT 0 - ueberall aus).
--   * Tabelle dbo.BdeTeilMeldungen (Fertigmeldung je Strukturzeile + Kuerzel) mit fachlichem Schluessel
--     (HauptFA, VaterFA, SubFA, Position, Artnr, Kuerzel) und gefiltertem UNIQUE WHERE IsCancelled = 0 (Doppel-Schutz, ADR 0026;
--     NULL gilt im Unique-Index als gleich - gewollt). FKs Restrict auf ProductionWorkplaces, BdeOperators, BdeTerminals, BdeBookings.
-- Rein additiv, nicht daten-destruktiv, keine Bestandsbefuellung. SQL/AgentJobs nicht betroffen.
-- SQL/151 und 153 sind fuer andere Laeufe reserviert.
-- Objektnamen wie die EF-Migration 20261117075856_AddBdeTeileliste. Idempotent (COL_LENGTH-/OBJECT_ID-Guards), DDL je eigener
-- Batch, History separat und nur, wenn beide Objekte da sind.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'[dbo].[ProductionWorkplaces]', N'U') IS NULL OR OBJECT_ID(N'[dbo].[BdeBookings]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.ProductionWorkplaces bzw. dbo.BdeBookings fehlt - falsche Datenbank? Keine Aenderung, keine History-Zeile.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.ProductionWorkplaces', N'BdeTeileliste') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductionWorkplaces] ADD [BdeTeileliste] BIT NOT NULL
        CONSTRAINT [DF_ProductionWorkplaces_BdeTeileliste] DEFAULT 0;
    PRINT N'Spalte ProductionWorkplaces.BdeTeileliste hinzugefuegt.';
END
ELSE
    PRINT N'Spalte ProductionWorkplaces.BdeTeileliste existiert bereits.';
GO

IF OBJECT_ID(N'[dbo].[BdeTeilMeldungen]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BdeTeilMeldungen] (
        [Id]                    INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_BdeTeilMeldungen] PRIMARY KEY,
        [HauptFA]               INT               NOT NULL,
        [VaterFA]               INT               NULL,
        [SubFA]                 INT               NOT NULL,
        [Position]              INT               NULL,
        [Artnr]                 NVARCHAR(200)     NULL,
        [Matchcode]             NVARCHAR(200)     NULL,
        [Kuerzel]               NVARCHAR(20)      NOT NULL,
        [ProductionWorkplaceId] INT               NOT NULL CONSTRAINT [FK_BdeTeilMeldungen_ProductionWorkplaces_ProductionWorkplaceId] REFERENCES [dbo].[ProductionWorkplaces]([Id]),
        [BdeOperatorId]         INT               NOT NULL CONSTRAINT [FK_BdeTeilMeldungen_BdeOperators_BdeOperatorId] REFERENCES [dbo].[BdeOperators]([Id]),
        [BdeTerminalId]         INT               NOT NULL CONSTRAINT [FK_BdeTeilMeldungen_BdeTerminals_BdeTerminalId] REFERENCES [dbo].[BdeTerminals]([Id]),
        [GoodQuantity]          DECIMAL(18,4)     NOT NULL,
        [ScrapQuantity]         DECIMAL(18,4)     NOT NULL,
        [SollmengeBeiMeldung]   DECIMAL(18,4)     NOT NULL,
        [GemeldetAm]            DATETIME2         NOT NULL,
        [BdeBookingId]          INT               NULL CONSTRAINT [FK_BdeTeilMeldungen_BdeBookings_BdeBookingId] REFERENCES [dbo].[BdeBookings]([Id]),
        [IsCancelled]           BIT               NOT NULL,
        [CancellationReason]    NVARCHAR(500)     NULL,
        [CreatedAt]             DATETIME2         NOT NULL,
        [CreatedBy]             NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]      NVARCHAR(200)     NOT NULL,
        [ModifiedAt]            DATETIME2         NULL,
        [ModifiedBy]            NVARCHAR(200)     NULL,
        [ModifiedByWindows]     NVARCHAR(200)     NULL
    );
    PRINT N'Tabelle BdeTeilMeldungen angelegt.';
END
ELSE
    PRINT N'Tabelle BdeTeilMeldungen existiert bereits.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BdeTeilMeldungen_Schluessel' AND object_id = OBJECT_ID(N'dbo.BdeTeilMeldungen'))
    CREATE UNIQUE INDEX [UX_BdeTeilMeldungen_Schluessel] ON [dbo].[BdeTeilMeldungen]([HauptFA], [VaterFA], [SubFA], [Position], [Artnr], [Kuerzel])
        WHERE [IsCancelled] = 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BdeTeilMeldungen_Workplace_GemeldetAm' AND object_id = OBJECT_ID(N'dbo.BdeTeilMeldungen'))
    CREATE INDEX [IX_BdeTeilMeldungen_Workplace_GemeldetAm] ON [dbo].[BdeTeilMeldungen]([ProductionWorkplaceId], [GemeldetAm]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BdeTeilMeldungen_Operator_GemeldetAm' AND object_id = OBJECT_ID(N'dbo.BdeTeilMeldungen'))
    CREATE INDEX [IX_BdeTeilMeldungen_Operator_GemeldetAm] ON [dbo].[BdeTeilMeldungen]([BdeOperatorId], [GemeldetAm]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BdeTeilMeldungen_BdeTerminalId' AND object_id = OBJECT_ID(N'dbo.BdeTeilMeldungen'))
    CREATE INDEX [IX_BdeTeilMeldungen_BdeTerminalId] ON [dbo].[BdeTeilMeldungen]([BdeTerminalId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BdeTeilMeldungen_BdeBookingId' AND object_id = OBJECT_ID(N'dbo.BdeTeilMeldungen'))
    CREATE INDEX [IX_BdeTeilMeldungen_BdeBookingId] ON [dbo].[BdeTeilMeldungen]([BdeBookingId]);
GO

-- Migrations-History (separater Batch, exakte MigrationId, nur wenn beide Objekte existieren)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261117075856_AddBdeTeileliste')
   AND COL_LENGTH(N'dbo.ProductionWorkplaces', N'BdeTeileliste') IS NOT NULL
   AND OBJECT_ID(N'dbo.BdeTeilMeldungen', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261117075856_AddBdeTeileliste', '10.0.2');
    PRINT 'Migration 20261117075856_AddBdeTeileliste eingetragen.';
END
GO

SET NOEXEC OFF;
GO

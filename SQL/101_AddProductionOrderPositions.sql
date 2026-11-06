-- =============================================================================
-- 101_AddProductionOrderPositions.sql  (v1.73.0, Spec 2026-11-06-sage-sdata-stammdaten-sync, Etappe E4b)
-- 1) Cache-Tabelle [ProductionOrderPositions]: Positionen der Sage-FAs aus SData PpsFaBelegePositionenInfo (UNIQUE BelPosID,
--    Index BelID, StrukturID). Kein Audit (Cache wie FaHierarchyNodes), Full-Refresh je Lauf durch den FaHierarchySyncService -
--    nur wenn Sync:Quelle:FaStruktur SData liest (Vergleich; SData erst nach P-F2). Im View-Betrieb bleibt sie leer.
-- 2) Cache-Tabelle [SageFaStates] (UNIQUE Mandant+BelID) in der Form, die das Rueckmelde-Epic 2026-11-02-sage-pps-fa-bde-abgleich
--    plant (G15). Wird hier NUR angelegt; befuellt erst der SageFaStateSyncService des Rueckmelde-Epics.
-- Keine FKs. Rein additiv, keine Bestandsbefuellung, nicht daten-destruktiv; DB-Backup vor Deploy wie ueblich.
-- Reihenfolge (G17): dreistellig, laeuft NACH 100_AddArticleAttributeValueTextValue.sql (Deploy-Liste beachten).
-- Objektnamen wie die EF-Migration 20261106191438_AddProductionOrderPositions.
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

IF OBJECT_ID(N'[dbo].[ProductionOrderPositions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductionOrderPositions] (
        [Id]                        INT IDENTITY(1,1) NOT NULL,
        [BelPosID]                  INT               NOT NULL,
        [BelID]                     INT               NOT NULL,
        [StrukturID]                INT               NOT NULL,
        [Position]                  INT               NULL,
        [RessourceTyp]              NVARCHAR(20)      NULL,
        [RessourceNummer]           NVARCHAR(31)      NULL,
        [ArbeitsgangNummer]         NVARCHAR(31)      NULL,
        [BelIDNachfolger]           INT               NULL,
        [Menge]                     DECIMAL(18,4)     NULL,
        [Sollmenge]                 DECIMAL(18,4)     NULL,
        [Istmenge]                  DECIMAL(18,4)     NULL,
        [SollRuestzeit]             DECIMAL(18,4)     NULL,
        [IstRuestzeit]              DECIMAL(18,4)     NULL,
        [SollStueckzeit]            DECIMAL(18,4)     NULL,
        [IstStueckzeit]             DECIMAL(18,4)     NULL,
        [Status]                    INT               NULL,
        [PlatzID]                   INT               NULL,
        [LagerplatzKurzbezeichnung] NVARCHAR(50)      NULL,
        [Schwund]                   DECIMAL(18,4)     NULL,
        [Bedarfstermin]             DATETIME2         NULL,
        [SyncedAt]                  DATETIME2         NOT NULL,
        CONSTRAINT [PK_ProductionOrderPositions] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT 'Tabelle ProductionOrderPositions erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_ProductionOrderPositions_BelPosID' AND object_id = OBJECT_ID('dbo.ProductionOrderPositions'))
BEGIN
    CREATE UNIQUE INDEX [UQ_ProductionOrderPositions_BelPosID] ON [dbo].[ProductionOrderPositions] ([BelPosID]);
    PRINT 'Index UQ_ProductionOrderPositions_BelPosID angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductionOrderPositions_BelID' AND object_id = OBJECT_ID('dbo.ProductionOrderPositions'))
BEGIN
    CREATE INDEX [IX_ProductionOrderPositions_BelID] ON [dbo].[ProductionOrderPositions] ([BelID]);
    PRINT 'Index IX_ProductionOrderPositions_BelID angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductionOrderPositions_StrukturID' AND object_id = OBJECT_ID('dbo.ProductionOrderPositions'))
BEGIN
    CREATE INDEX [IX_ProductionOrderPositions_StrukturID] ON [dbo].[ProductionOrderPositions] ([StrukturID]);
    PRINT 'Index IX_ProductionOrderPositions_StrukturID angelegt.';
END
GO

IF OBJECT_ID(N'[dbo].[SageFaStates]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SageFaStates] (
        [Id]                     INT IDENTITY(1,1) NOT NULL,
        [Mandant]                INT               NOT NULL,
        [BelID]                  INT               NOT NULL,
        [StrukturID]             INT               NOT NULL,
        [IstEingelastet]         BIT               NOT NULL,
        [IstBegonnen]            BIT               NOT NULL,
        [IstBeendet]             BIT               NOT NULL,
        [IstGesperrt]            BIT               NOT NULL,
        [IstReaktiviert]         BIT               NULL,
        [MengeGutRueckgemeldet]  DECIMAL(18,4)     NULL,
        [MengeGutFertiggemeldet] DECIMAL(18,4)     NULL,
        [SyncedAt]               DATETIME2         NOT NULL,
        CONSTRAINT [PK_SageFaStates] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT 'Tabelle SageFaStates erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_SageFaStates_Mandant_BelID' AND object_id = OBJECT_ID('dbo.SageFaStates'))
BEGIN
    CREATE UNIQUE INDEX [UQ_SageFaStates_Mandant_BelID] ON [dbo].[SageFaStates] ([Mandant], [BelID]);
    PRINT 'Index UQ_SageFaStates_Mandant_BelID angelegt.';
END
GO

-- Migrations-History (separater Batch, exakte MigrationId)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261106191438_AddProductionOrderPositions')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261106191438_AddProductionOrderPositions', '10.0.2');
    PRINT 'Migration 20261106191438_AddProductionOrderPositions eingetragen.';
END
GO

SET NOEXEC OFF;
GO

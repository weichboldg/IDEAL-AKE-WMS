-- =============================================================================
-- 112_AddSagePurchaseOrderLines.sql  (v1.84.0, Spec 2026-11-08-wareneingang-aus-sage-bestellungen E1)
-- Tabelle [SagePurchaseOrderLines]: Cache offener Sage-Einkaufsbestellpositionen (nur EBB), gefuellt vom Lese-Abgleich
--   „Offene Bestellungen (Sage)“ (Schalter Sync:EinkaufBestellungenEnabled, Default aus). Je Lauf vollstaendig ersetzt.
--   Kein AuditableEntity (SyncedAt), keine FKs (Artikel/Kreditor/Lagerplatz sind beim Lauf aufgeloeste Werte).
--   UNIQUE BelPosId; Indizes BelId, Belegnummer, ArticleId, Liefertermin. Laengen laut Sage-SQL (E0/P-W1).
-- Rein additiv (neue Tabelle), nicht daten-destruktiv. SQL/AgentJobs nicht betroffen.
-- Objektnamen wie die EF-Migration 20261107172542_AddSagePurchaseOrderLines.
-- Idempotent: zweiter Lauf aendert nichts. DDL je in eigenem Batch, History-Insert in separatem Batch.
-- =============================================================================
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.__EFMigrationsHistory fehlt - zuerst SQL/00_FreshInstall.sql ausfuehren. Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF OBJECT_ID(N'[dbo].[SagePurchaseOrderLines]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SagePurchaseOrderLines] (
        [Id]                        INT IDENTITY(1,1) NOT NULL,
        [BelPosId]                  INT               NOT NULL,
        [BelId]                     INT               NOT NULL,
        [Belegkennzeichen]          NVARCHAR(3)       NOT NULL,
        [Belegnummer]               NVARCHAR(20)      NOT NULL,
        [LieferantKto]              NVARCHAR(20)      NULL,
        [LieferantName]             NVARCHAR(50)      NULL,
        [LieferantGesperrt]         BIT               NOT NULL,
        [CreditorId]                INT               NULL,
        [Artikelnummer]             NVARCHAR(50)      NOT NULL,
        [ArticleId]                 INT               NULL,
        [Bezeichnung1]              NVARCHAR(50)      NULL,
        [Bezeichnung2]              NVARCHAR(50)      NULL,
        [MengeBestellt]             DECIMAL(18,4)     NOT NULL,
        [MengeGeliefert]            DECIMAL(18,4)     NOT NULL,
        [MengeOffen]                DECIMAL(18,4)     NOT NULL,
        [Mengeneinheit]             NVARCHAR(5)       NULL,
        [MengeBasiseinheit]         NVARCHAR(5)       NULL,
        [Lagermengeneinheit]        NVARCHAR(5)       NULL,
        [EinheitAbweichend]         BIT               NOT NULL,
        [SagePlatzId]               INT               NULL,
        [SagePlatzKurzbezeichnung]  NVARCHAR(50)      NULL,
        [StorageLocationId]         INT               NULL,
        [Liefertermin]              DATE              NULL,
        [Chargenpflicht]            BIT               NOT NULL,
        [Nachweispflicht]           BIT               NOT NULL,
        [SyncedAt]                  DATETIME2         NOT NULL,
        CONSTRAINT [PK_SagePurchaseOrderLines] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT 'Tabelle SagePurchaseOrderLines erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SagePurchaseOrderLines_BelPosId' AND object_id = OBJECT_ID('dbo.SagePurchaseOrderLines'))
    CREATE UNIQUE INDEX [IX_SagePurchaseOrderLines_BelPosId] ON [dbo].[SagePurchaseOrderLines] ([BelPosId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SagePurchaseOrderLines_BelId' AND object_id = OBJECT_ID('dbo.SagePurchaseOrderLines'))
    CREATE INDEX [IX_SagePurchaseOrderLines_BelId] ON [dbo].[SagePurchaseOrderLines] ([BelId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SagePurchaseOrderLines_Belegnummer' AND object_id = OBJECT_ID('dbo.SagePurchaseOrderLines'))
    CREATE INDEX [IX_SagePurchaseOrderLines_Belegnummer] ON [dbo].[SagePurchaseOrderLines] ([Belegnummer]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SagePurchaseOrderLines_ArticleId' AND object_id = OBJECT_ID('dbo.SagePurchaseOrderLines'))
    CREATE INDEX [IX_SagePurchaseOrderLines_ArticleId] ON [dbo].[SagePurchaseOrderLines] ([ArticleId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SagePurchaseOrderLines_Liefertermin' AND object_id = OBJECT_ID('dbo.SagePurchaseOrderLines'))
    CREATE INDEX [IX_SagePurchaseOrderLines_Liefertermin] ON [dbo].[SagePurchaseOrderLines] ([Liefertermin]);
GO

-- Migrations-History (separater Batch, exakte MigrationId)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261107172542_AddSagePurchaseOrderLines')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261107172542_AddSagePurchaseOrderLines', '10.0.2');
    PRINT 'Migration 20261107172542_AddSagePurchaseOrderLines eingetragen.';
END
GO

SET NOEXEC OFF;
GO

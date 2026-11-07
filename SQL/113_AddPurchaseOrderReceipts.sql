-- =============================================================================
-- 113_AddPurchaseOrderReceipts.sql  (v1.84.0, Spec 2026-11-08-wareneingang-aus-sage-bestellungen E3)
-- Tabelle [PurchaseOrderReceipts]: Wareneingang zu einer Sage-Bestellposition, genau eine StockMovement (Einbuchung).
--   AuditableEntity. FK StockMovementId -> StockMovements NO ACTION (Restrict), UNIQUE StockMovementId; Index BelPosId.
--   Snapshot (BelId, BelPosId, Belegnummer, LieferantKto, Menge, offen laut Sage, Sage-Platz, Lieferart) – kein FK auf den Cache.
-- Rein additiv (neue Tabelle), nicht daten-destruktiv. Laeuft nach SQL/112 (keine Abhaengigkeit) . SQL/AgentJobs nicht betroffen.
-- Objektnamen wie die EF-Migration 20261107173341_AddPurchaseOrderReceipts.
-- Idempotent: zweiter Lauf aendert nichts. DDL je in eigenem Batch, History-Insert in separatem Batch.
-- =============================================================================
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL OR OBJECT_ID(N'[dbo].[StockMovements]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.__EFMigrationsHistory oder dbo.StockMovements fehlt - zuerst SQL/00_FreshInstall.sql ausfuehren. Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF OBJECT_ID(N'[dbo].[PurchaseOrderReceipts]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PurchaseOrderReceipts] (
        [Id]                        INT IDENTITY(1,1) NOT NULL,
        [StockMovementId]           INT               NOT NULL,
        [BelId]                     INT               NOT NULL,
        [BelPosId]                  INT               NOT NULL,
        [Belegnummer]               NVARCHAR(20)      NOT NULL,
        [LieferantKto]              NVARCHAR(20)      NULL,
        [Quantity]                  DECIMAL(18,4)     NOT NULL,
        [OffenLautSageBeimBuchen]   DECIMAL(18,4)     NULL,
        [SagePlatzId]               INT               NULL,
        [Lieferart]                 TINYINT           NOT NULL,
        [CreatedAt]                 DATETIME2         NOT NULL,
        [CreatedBy]                 NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]          NVARCHAR(200)     NOT NULL,
        [ModifiedAt]                DATETIME2         NULL,
        [ModifiedBy]                NVARCHAR(200)     NULL,
        [ModifiedByWindows]         NVARCHAR(200)     NULL,
        CONSTRAINT [PK_PurchaseOrderReceipts] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_PurchaseOrderReceipts_StockMovements_StockMovementId] FOREIGN KEY ([StockMovementId]) REFERENCES [dbo].[StockMovements] ([Id])
    );
    PRINT 'Tabelle PurchaseOrderReceipts erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PurchaseOrderReceipts_StockMovementId' AND object_id = OBJECT_ID('dbo.PurchaseOrderReceipts'))
    CREATE UNIQUE INDEX [IX_PurchaseOrderReceipts_StockMovementId] ON [dbo].[PurchaseOrderReceipts] ([StockMovementId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PurchaseOrderReceipts_BelPosId' AND object_id = OBJECT_ID('dbo.PurchaseOrderReceipts'))
    CREATE INDEX [IX_PurchaseOrderReceipts_BelPosId] ON [dbo].[PurchaseOrderReceipts] ([BelPosId]);
GO

-- Migrations-History (separater Batch, exakte MigrationId)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261107173341_AddPurchaseOrderReceipts')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261107173341_AddPurchaseOrderReceipts', '10.0.2');
    PRINT 'Migration 20261107173341_AddPurchaseOrderReceipts eingetragen.';
END
GO

SET NOEXEC OFF;
GO

-- =============================================================================
-- 143_AddPurchaseOrderReceiptSageWe.sql  (v1.99.4, Spec 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe, Etappe E1)
-- Sage-Wareneingangsbeleg (ELL) und Lieferschein-Nr. je Wareneingang zu einer Bestellposition: 7 Spalten + 2 Indizes an PurchaseOrderReceipts.
-- Nummer 143 = erste freie nach SQL/142 (Pruefung beim Start des Dev-Laufs 2026-11-11, Freigabe-Antwort 1). Rein additiv, nicht
-- daten-destruktiv: Altbestand erhaelt SageWeStatus = 0 (NichtNoetig) = Verhalten wie Teil 1. SQL/AgentJobs nicht betroffen.
-- Objektnamen wie die EF-Migration 20261111200627_AddPurchaseOrderReceiptSageWe. Idempotent (COL_LENGTH/sys.indexes-Guard), History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[dbo].[PurchaseOrderReceipts]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.PurchaseOrderReceipts fehlt - zuerst SQL/113 ausfuehren. Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.PurchaseOrderReceipts', N'SageWeStatus') IS NULL
BEGIN
    ALTER TABLE [dbo].[PurchaseOrderReceipts] ADD
        [SageWeStatus]      INT               NOT NULL CONSTRAINT [DF_PurchaseOrderReceipts_SageWeStatus] DEFAULT 0,
        [SageWeVorgang]     UNIQUEIDENTIFIER  NULL,
        [SageWeAm]          DATETIME2         NULL,
        [SageWeBelegnummer] NVARCHAR(30)      NULL,
        [SageWeFehler]      NVARCHAR(1000)    NULL,
        [SageWeVersuche]    INT               NOT NULL CONSTRAINT [DF_PurchaseOrderReceipts_SageWeVersuche] DEFAULT 0,
        [Lieferschein]      NVARCHAR(20)      NULL;
    PRINT N'Spalten SageWe* und Lieferschein an PurchaseOrderReceipts angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseOrderReceipts_SageWeVorgang' AND object_id = OBJECT_ID(N'dbo.PurchaseOrderReceipts'))
    CREATE INDEX [IX_PurchaseOrderReceipts_SageWeVorgang] ON [dbo].[PurchaseOrderReceipts] ([SageWeVorgang]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseOrderReceipts_SageWeStatus' AND object_id = OBJECT_ID(N'dbo.PurchaseOrderReceipts'))
    CREATE INDEX [IX_PurchaseOrderReceipts_SageWeStatus] ON [dbo].[PurchaseOrderReceipts] ([SageWeStatus]);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261111200627_AddPurchaseOrderReceiptSageWe')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261111200627_AddPurchaseOrderReceiptSageWe', '10.0.2');
    PRINT 'Migration 20261111200627_AddPurchaseOrderReceiptSageWe eingetragen.';
END
GO

SET NOEXEC OFF;
GO

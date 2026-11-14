-- =============================================================================
-- 150_AddPurchaseOrderReceiptAbsendekennung.sql  (v1.99.23, Spec 2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen C3, WE-1/WE-4)
-- Kennung des Absendens der Maske „Wareneingang zu Bestellung“ an jedem Wareneingang; gefilterter UNIQUE (Absendekennung, BelPosId):
-- ein zweites Absenden derselben Seite (Doppeltipp, Netz-Wiederholung, Zurueck-Taste) bucht nichts doppelt (ADR 0026).
--   PurchaseOrderReceipts.Absendekennung  UNIQUEIDENTIFIER NULL  (bestehende Zeilen bleiben NULL, vom Filter ausgenommen)
--   IX_PurchaseOrderReceipts_Absendekennung_BelPosId  UNIQUE ... WHERE [Absendekennung] IS NOT NULL
-- Additiv, nicht daten-destruktiv. SQL/AgentJobs nicht betroffen. Objektnamen wie die EF-Migration 20261114080950_AddPurchaseOrderReceiptAbsendekennung.
-- Idempotent, DDL je eigener Batch, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;   -- gefilterter Index
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'[dbo].[PurchaseOrderReceipts]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: Tabelle PurchaseOrderReceipts (SQL/113) fehlt - zuerst SQL/113 ausfuehren. Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.PurchaseOrderReceipts', N'Absendekennung') IS NULL
BEGIN
    ALTER TABLE [dbo].[PurchaseOrderReceipts] ADD [Absendekennung] UNIQUEIDENTIFIER NULL;
    PRINT N'Spalte PurchaseOrderReceipts.Absendekennung angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseOrderReceipts_Absendekennung_BelPosId' AND object_id = OBJECT_ID(N'dbo.PurchaseOrderReceipts'))
BEGIN
    CREATE UNIQUE INDEX [IX_PurchaseOrderReceipts_Absendekennung_BelPosId] ON [dbo].[PurchaseOrderReceipts]([Absendekennung], [BelPosId])
        WHERE [Absendekennung] IS NOT NULL;
    PRINT N'Index IX_PurchaseOrderReceipts_Absendekennung_BelPosId angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261114080950_AddPurchaseOrderReceiptAbsendekennung')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261114080950_AddPurchaseOrderReceiptAbsendekennung', '10.0.2');
    PRINT 'Migration 20261114080950_AddPurchaseOrderReceiptAbsendekennung eingetragen.';
END
GO

SET NOEXEC OFF;
GO

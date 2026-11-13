-- =============================================================================
-- 149_WidenBelegnummer45.sql  (v1.99.22, Spec 2026-11-08-einkauf-bestellvorschlaege Freigabe-Nachtrag 3, Backlog 2026-11-13-belegnummer-laenge-45)
-- Sage-Belegnummern (BelegnummerFormatiert: SData string(45), Sage-Views vwDPSBS_CWS_* varchar(45)) im WMS von 20 bzw. 30 auf 45 Zeichen:
--   SagePurchaseOrderLines.Belegnummer      NVARCHAR(20) -> NVARCHAR(45) NOT NULL  (Index IX_SagePurchaseOrderLines_Belegnummer: drop/recreate)
--   PurchaseOrderReminders.Belegnummer      NVARCHAR(20) -> NVARCHAR(45) NOT NULL  (kein Index auf der Spalte)
--   PurchaseOrderReceipts.Belegnummer       NVARCHAR(20) -> NVARCHAR(45) NOT NULL  (kein Index auf der Spalte)
--   PurchaseOrderReceipts.SageWeBelegnummer NVARCHAR(30) -> NVARCHAR(45) NULL
-- Nur Verbreiterung: nicht daten-destruktiv, vorhandene Werte bleiben. SQL/AgentJobs nicht betroffen.
-- Objektnamen wie die EF-Migration 20261113172929_WidenBelegnummer45. Idempotent (COL_LENGTH in Bytes: NVARCHAR(45) = 90),
-- DDL je eigener Batch, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[dbo].[SagePurchaseOrderLines]', N'U') IS NULL OR OBJECT_ID(N'[dbo].[PurchaseOrderReceipts]', N'U') IS NULL
   OR OBJECT_ID(N'[dbo].[PurchaseOrderReminders]', N'U') IS NULL OR COL_LENGTH(N'dbo.PurchaseOrderReceipts', N'SageWeBelegnummer') IS NULL
BEGIN
    PRINT N'WARNUNG: Tabellen aus SQL/112, SQL/113, SQL/143 bzw. SQL/145 fehlen - zuerst diese ausfuehren. Keine Aenderung.';
    SET NOEXEC ON;
END
GO

-- SagePurchaseOrderLines.Belegnummer: Index haengt an der Spalte -> erst entfernen, nach dem ALTER neu anlegen.
IF COL_LENGTH(N'dbo.SagePurchaseOrderLines', N'Belegnummer') < 90
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SagePurchaseOrderLines_Belegnummer' AND object_id = OBJECT_ID(N'dbo.SagePurchaseOrderLines'))
BEGIN
    DROP INDEX [IX_SagePurchaseOrderLines_Belegnummer] ON [dbo].[SagePurchaseOrderLines];
    PRINT N'Index IX_SagePurchaseOrderLines_Belegnummer entfernt (wird neu angelegt).';
END
GO

IF COL_LENGTH(N'dbo.SagePurchaseOrderLines', N'Belegnummer') < 90
BEGIN
    ALTER TABLE [dbo].[SagePurchaseOrderLines] ALTER COLUMN [Belegnummer] NVARCHAR(45) NOT NULL;
    PRINT N'Spalte SagePurchaseOrderLines.Belegnummer auf NVARCHAR(45).';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SagePurchaseOrderLines_Belegnummer' AND object_id = OBJECT_ID(N'dbo.SagePurchaseOrderLines'))
BEGIN
    CREATE INDEX [IX_SagePurchaseOrderLines_Belegnummer] ON [dbo].[SagePurchaseOrderLines]([Belegnummer]);
    PRINT N'Index IX_SagePurchaseOrderLines_Belegnummer angelegt.';
END
GO

IF COL_LENGTH(N'dbo.PurchaseOrderReminders', N'Belegnummer') < 90
BEGIN
    ALTER TABLE [dbo].[PurchaseOrderReminders] ALTER COLUMN [Belegnummer] NVARCHAR(45) NOT NULL;
    PRINT N'Spalte PurchaseOrderReminders.Belegnummer auf NVARCHAR(45).';
END
GO

IF COL_LENGTH(N'dbo.PurchaseOrderReceipts', N'Belegnummer') < 90
BEGIN
    ALTER TABLE [dbo].[PurchaseOrderReceipts] ALTER COLUMN [Belegnummer] NVARCHAR(45) NOT NULL;
    PRINT N'Spalte PurchaseOrderReceipts.Belegnummer auf NVARCHAR(45).';
END
GO

IF COL_LENGTH(N'dbo.PurchaseOrderReceipts', N'SageWeBelegnummer') < 90
BEGIN
    ALTER TABLE [dbo].[PurchaseOrderReceipts] ALTER COLUMN [SageWeBelegnummer] NVARCHAR(45) NULL;
    PRINT N'Spalte PurchaseOrderReceipts.SageWeBelegnummer auf NVARCHAR(45).';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261113172929_WidenBelegnummer45')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261113172929_WidenBelegnummer45', '10.0.2');
    PRINT 'Migration 20261113172929_WidenBelegnummer45 eingetragen.';
END
GO

SET NOEXEC OFF;
GO

-- =============================================================================
-- 144_AddSageKennungen.sql  (v1.99.9, Spec 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe, Freigabe-Nachtrag 2026-11-12 14:20)
-- System-Kuerzel in den Sage-Kennungen: die beim Senden verwendete Memo-Kennung wird gespeichert, damit die Suche vor dem erneuten Senden
-- genau sie nutzt (auch nach einer Aenderung von ServiceSetting Sync:SageSystemKuerzel).
--   PurchaseOrderReceipts.SageWeKennung  NVARCHAR(20) NULL  (WE#[K#]<Id>#,  Sage-Wareneingang)
--   SageBookingQueueItems.SageKennung    NVARCHAR(20) NULL  (SM#[K#]<id>#,  Sage-Lagerbuchung)
-- Rein additiv, nicht daten-destruktiv: Altbestand bleibt NULL = altes Format ohne Kuerzel. SQL/AgentJobs nicht betroffen.
-- Objektnamen wie die EF-Migration 20261112141208_AddSageKennungen. Idempotent (COL_LENGTH-Guard), History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[dbo].[PurchaseOrderReceipts]', N'U') IS NULL OR COL_LENGTH(N'dbo.PurchaseOrderReceipts', N'SageWeStatus') IS NULL
   OR OBJECT_ID(N'[dbo].[SageBookingQueueItems]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.PurchaseOrderReceipts (mit SQL/143) bzw. dbo.SageBookingQueueItems fehlt - zuerst SQL/143 bzw. SQL/83 ausfuehren. Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.PurchaseOrderReceipts', N'SageWeKennung') IS NULL
BEGIN
    ALTER TABLE [dbo].[PurchaseOrderReceipts] ADD [SageWeKennung] NVARCHAR(20) NULL;
    PRINT N'Spalte PurchaseOrderReceipts.SageWeKennung angelegt.';
END
GO

IF COL_LENGTH(N'dbo.SageBookingQueueItems', N'SageKennung') IS NULL
BEGIN
    ALTER TABLE [dbo].[SageBookingQueueItems] ADD [SageKennung] NVARCHAR(20) NULL;
    PRINT N'Spalte SageBookingQueueItems.SageKennung angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261112141208_AddSageKennungen')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261112141208_AddSageKennungen', '10.0.2');
    PRINT 'Migration 20261112141208_AddSageKennungen eingetragen.';
END
GO

SET NOEXEC OFF;
GO

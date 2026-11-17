-- =============================================================================
-- 156_AddSagePurchaseOrderLineAbNummerKommission.sql  (v1.99.33, Spec 2026-11-17-wareneingang-sammellieferschein-und-bestellkopf Teil B)
-- Offene Bestellungen: AB-Nummer und Kommission aus dem Sage-Belegkopf (KHKEKBelege.USER_AB / USER_Kom, gelesen ueber SData
-- RealTimeData dtsEKBelegMst.Sage.Wawi) als Kopfwert an jeder Position des Caches dbo.SagePurchaseOrderLines.
--   AbNummer   NVARCHAR(250) NULL  (SData-Schema EKBelegUserField USER_AB 250)
--   Kommission NVARCHAR(200) NULL  (SData-Schema EKBelegUserField USER_Kom 200)
-- Nur Anzeige (Wareneingang zu Bestellung, Liste Offene Bestellungen Lager/Einkauf), nicht in der enaio-CSV.
-- Rein additiv, nicht daten-destruktiv: die Werte fuellt der naechste Abgleich "Offene Bestellungen (Sage)" (Vollersatz des Caches).
-- SQL/AgentJobs nicht betroffen (den Cache schreibt nur der Dienst).
-- Objektnamen wie die EF-Migration 20261117102804_AddSagePurchaseOrderLineAbNummerKommission. Idempotent (COL_LENGTH-Guard),
-- DDL im eigenen Batch, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[dbo].[SagePurchaseOrderLines]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.SagePurchaseOrderLines fehlt - falsche Datenbank oder SQL/112 fehlt? Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.SagePurchaseOrderLines', N'AbNummer') IS NULL
BEGIN
    ALTER TABLE [dbo].[SagePurchaseOrderLines] ADD [AbNummer] NVARCHAR(250) NULL;
    PRINT N'Spalte SagePurchaseOrderLines.AbNummer angelegt.';
END
ELSE
    PRINT N'Spalte SagePurchaseOrderLines.AbNummer existiert bereits.';
GO

IF COL_LENGTH(N'dbo.SagePurchaseOrderLines', N'Kommission') IS NULL
BEGIN
    ALTER TABLE [dbo].[SagePurchaseOrderLines] ADD [Kommission] NVARCHAR(200) NULL;
    PRINT N'Spalte SagePurchaseOrderLines.Kommission angelegt.';
END
ELSE
    PRINT N'Spalte SagePurchaseOrderLines.Kommission existiert bereits.';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261117102804_AddSagePurchaseOrderLineAbNummerKommission')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261117102804_AddSagePurchaseOrderLineAbNummerKommission', '10.0.2');
    PRINT 'Migration 20261117102804_AddSagePurchaseOrderLineAbNummerKommission eingetragen.';
END
GO

SET NOEXEC OFF;
GO

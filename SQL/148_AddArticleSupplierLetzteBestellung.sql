-- =============================================================================
-- 148_AddArticleSupplierLetzteBestellung.sql  (v1.99.21, Spec 2026-11-08-einkauf-bestellvorschlaege E3)
-- Bestellvorschlaege: letzte Bestellung (juengste Sage-Bestellposition EBB/EBZ der letzten 3 Jahre) je Artikel + Lieferant.
--   ArticleSuppliers.LetzteBestellmenge     DECIMAL(18,3) NULL  (MengeBasis = Basiseinheit)
--   ArticleSuppliers.LetzteBestellungAm     DATE          NULL  (Belegdatum)
--   ArticleSuppliers.LetzteBestellungBeleg  NVARCHAR(45)  NULL  (BelegnummerFormatiert, Sage string(45))
-- Geschrieben nur vom Dienst-Lauf "Artikel-Lieferanten" (Sync:ArtikelLieferantEnabled). Rein additiv, nicht daten-destruktiv:
-- Altbestand bleibt NULL bis zum naechsten Lauf. SQL/AgentJobs nicht betroffen.
-- Objektnamen wie die EF-Migration 20261113171808_AddArticleSupplierLetzteBestellung. Idempotent (COL_LENGTH-Guard),
-- DDL je eigener Batch, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[dbo].[ArticleSuppliers]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.ArticleSuppliers (SQL/106) fehlt - zuerst SQL/106 ausfuehren. Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.ArticleSuppliers', N'LetzteBestellmenge') IS NULL
BEGIN
    ALTER TABLE [dbo].[ArticleSuppliers] ADD [LetzteBestellmenge] DECIMAL(18,3) NULL;
    PRINT N'Spalte ArticleSuppliers.LetzteBestellmenge angelegt.';
END
GO

IF COL_LENGTH(N'dbo.ArticleSuppliers', N'LetzteBestellungAm') IS NULL
BEGIN
    ALTER TABLE [dbo].[ArticleSuppliers] ADD [LetzteBestellungAm] DATE NULL;
    PRINT N'Spalte ArticleSuppliers.LetzteBestellungAm angelegt.';
END
GO

IF COL_LENGTH(N'dbo.ArticleSuppliers', N'LetzteBestellungBeleg') IS NULL
BEGIN
    ALTER TABLE [dbo].[ArticleSuppliers] ADD [LetzteBestellungBeleg] NVARCHAR(45) NULL;
    PRINT N'Spalte ArticleSuppliers.LetzteBestellungBeleg angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261113171808_AddArticleSupplierLetzteBestellung')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261113171808_AddArticleSupplierLetzteBestellung', '10.0.2');
    PRINT 'Migration 20261113171808_AddArticleSupplierLetzteBestellung eingetragen.';
END
GO

SET NOEXEC OFF;
GO

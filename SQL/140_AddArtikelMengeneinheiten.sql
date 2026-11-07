-- =============================================================================
-- 140_AddArtikelMengeneinheiten.sql  (v1.85.0, Spec 2026-11-08-artikeldetails-lieferanten-anzeigen, Nachbesserung 5)
-- Mengeneinheiten aus SData (Feldnamen am AKE-Test 2026-11-08 belegt):
--   ArticleSageDetails: Basismengeneinheit, DezimalstellenBasis, Lagermengeneinheit, UmrechnungsFaktorLME, Verkaufsmengeneinheit,
--                       DezimalstellenVK, UmrechnungsfaktorVK, VerpackungseinheitVK, UmrechnungsfaktorVPVK
--   ArticleSuppliers:   DezimalstellenEK, UmrechnungsfaktorVPEK (Einkaufsmengeneinheit/UmrechnungsfaktorEK gibt es seit SQL/106)
-- Nummer 140 = erste freie nach allen reservierten Bloecken der Buendel-Uebersicht 2026-11-08 (Lauf 2).
-- Rein additiv (nullable Spalten, gefuellt erst vom naechsten SData-Artikel- bzw. Artikel-Lieferanten-Lauf). SQL/AgentJobs nicht betroffen.
-- Objektnamen wie die EF-Migration 20261107182445_AddArtikelMengeneinheiten. Idempotent (COL_LENGTH-Guard), History in separatem Batch.
-- =============================================================================
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'[dbo].[ArticleSageDetails]', N'U') IS NULL OR OBJECT_ID(N'[dbo].[ArticleSuppliers]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.ArticleSageDetails oder dbo.ArticleSuppliers fehlt - zuerst SQL/99 bzw. SQL/106 ausfuehren. Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.ArticleSageDetails', N'Basismengeneinheit') IS NULL
    ALTER TABLE [dbo].[ArticleSageDetails] ADD
        [Basismengeneinheit]     NVARCHAR(5)    NULL,
        [DezimalstellenBasis]    INT            NULL,
        [Lagermengeneinheit]     NVARCHAR(5)    NULL,
        [UmrechnungsFaktorLME]   DECIMAL(18,6)  NULL,
        [Verkaufsmengeneinheit]  NVARCHAR(5)    NULL,
        [DezimalstellenVK]       INT            NULL,
        [UmrechnungsfaktorVK]    DECIMAL(18,6)  NULL,
        [VerpackungseinheitVK]   NVARCHAR(5)    NULL,
        [UmrechnungsfaktorVPVK]  DECIMAL(18,6)  NULL;
GO

IF COL_LENGTH(N'dbo.ArticleSuppliers', N'DezimalstellenEK') IS NULL
    ALTER TABLE [dbo].[ArticleSuppliers] ADD
        [DezimalstellenEK]       INT            NULL,
        [UmrechnungsfaktorVPEK]  DECIMAL(18,6)  NULL;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261107182445_AddArtikelMengeneinheiten')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261107182445_AddArtikelMengeneinheiten', '10.0.2');
    PRINT 'Migration 20261107182445_AddArtikelMengeneinheiten eingetragen.';
END
GO

SET NOEXEC OFF;
GO

-- =============================================================================
-- 100_AddArticleAttributeValueTextValue.sql  (v1.73.0, Spec 2026-11-06-sage-sdata-stammdaten-sync, Etappe E3b)
-- [ArticleAttributeValues].[TextValue] NVARCHAR(1000) NULL fuer Sage-Benutzerfelder als Text-Merkmal (Laenge wie
-- FaAttributeValues.TextValue). Befuellt nur der Artikel-Abgleich mit Sync:Quelle:Artikel = SData (bis P-R1 gesperrt).
-- Rein additiv, keine Bestandsbefuellung, nicht daten-destruktiv; DB-Backup vor Deploy wie ueblich.
-- Reihenfolge (G17): dreistellig, laeuft NACH 99_AddArticleSageDetails.sql (lexikographisch sortiert 100_ vor 11_ - Deploy-Liste beachten).
-- Objektnamen wie die EF-Migration 20261106185149_AddArticleAttributeValueTextValue.
-- Idempotent: zweiter Lauf aendert nichts. DDL in eigenem Batch, History-Insert in separatem Batch.
-- =============================================================================
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'[dbo].[ArticleAttributeValues]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: Tabelle dbo.ArticleAttributeValues fehlt - zuerst SQL/00_FreshInstall.sql ausfuehren. Keine Aenderung, keine History-Zeile.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.ArticleAttributeValues', N'TextValue') IS NULL
BEGIN
    ALTER TABLE [dbo].[ArticleAttributeValues] ADD [TextValue] NVARCHAR(1000) NULL;
    PRINT 'Spalte ArticleAttributeValues.TextValue angelegt.';
END
GO

-- Migrations-History (separater Batch, exakte MigrationId)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261106185149_AddArticleAttributeValueTextValue')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261106185149_AddArticleAttributeValueTextValue', '10.0.2');
    PRINT 'Migration 20261106185149_AddArticleAttributeValueTextValue eingetragen.';
END
GO

SET NOEXEC OFF;
GO

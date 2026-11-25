-- =============================================================================
-- 164_AddArticleCamPath.sql  (v1.99.72, Spec 2026-11-20-bde-liste-isolierungsfraese-pfad-material, Antwort 19)
-- dbo.Articles + CamPath NVARCHAR(500) NULL ("Fräsen-Pfad", reines WMS-Feld, UNC-Pfad).
-- Der Sage-Artikelabgleich (UPDATE mit fester Spaltenliste) und SQL/AgentJobs (INSERT/UPDATE mit fester Spaltenliste)
-- fassen die Spalte nicht an -> SQL/AgentJobs nicht betroffen. Kein Default, kein Index.
-- Additiv, nullable, nicht daten-destruktiv, kein Backfill (Erstimport: SQL/Einmalig/2026-11-25_v1.99.72_FraesenPfad_Erstimport.sql).
-- Objektnamen wie die EF-Migration 20261125173002_AddArticleCamPath.
-- Idempotent (COL_LENGTH-/OBJECT_ID-Guard), DDL im eigenen Batch, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Articles', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.Articles fehlt - falsche Datenbank? Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.Articles', N'CamPath') IS NULL
BEGIN
    ALTER TABLE [dbo].[Articles] ADD [CamPath] NVARCHAR(500) NULL;
    PRINT 'Spalte Articles.CamPath angelegt.';
END
GO

IF COL_LENGTH(N'dbo.Articles', N'CamPath') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261125173002_AddArticleCamPath')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261125173002_AddArticleCamPath', '10.0.2');
    PRINT 'Migration 20261125173002_AddArticleCamPath eingetragen.';
END
GO

SET NOEXEC OFF;
GO

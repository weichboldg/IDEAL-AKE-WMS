-- =============================================================================
-- 157_AddBeschichtungsauftragPositionText.sql  (v1.99.34, Spec 2026-11-16-beschichtungslogik-je-standort E5, Nachtrag 3)
-- Beschichtungsauftrag an der AKE (flache Quelle = WA-Stueckliste): Stuecklisten-Position als Text (z. B. 51.5.2) als Teil
-- des Positionsschluessels (mit Artnr, Freigabe-Antwort 20).
--   PositionText NVARCHAR(50) NULL  (Breite wie CachedBomItems.Position, einzige Quelle)
-- Rein additiv, nullable, kein Default, kein Backfill: Bestandszeilen behalten NULL = IDEAL-Semantik. Nicht daten-destruktiv,
-- das Standard-Backup des Update-Skripts genuegt. Kein neuer Index. SQL/AgentJobs nicht betroffen.
-- Objektnamen wie die EF-Migration 20261117141308_AddBeschichtungsauftragPositionText. Idempotent (COL_LENGTH-Guard), DDL im eigenen Batch, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[dbo].[BeschichtungsauftragPositionen]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.BeschichtungsauftragPositionen fehlt - falsche Datenbank oder SQL/94 fehlt? Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.BeschichtungsauftragPositionen', N'PositionText') IS NULL
BEGIN
    ALTER TABLE [dbo].[BeschichtungsauftragPositionen] ADD [PositionText] NVARCHAR(50) NULL;
    PRINT N'Spalte BeschichtungsauftragPositionen.PositionText angelegt.';
END
ELSE
    PRINT N'Spalte BeschichtungsauftragPositionen.PositionText existiert bereits.';
GO

IF COL_LENGTH(N'dbo.BeschichtungsauftragPositionen', N'PositionText') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261117141308_AddBeschichtungsauftragPositionText')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261117141308_AddBeschichtungsauftragPositionText', '10.0.2');
    PRINT 'Migration 20261117141308_AddBeschichtungsauftragPositionText eingetragen.';
END
GO

SET NOEXEC OFF;
GO

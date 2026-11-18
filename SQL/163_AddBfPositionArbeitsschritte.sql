-- =============================================================================
-- 163_AddBfPositionArbeitsschritte.sql  (v1.99.42, Nachbesserung BF-CSV Befund P-1 (2), Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv)
-- dbo.BfAuftragPositionen + Arbeitsschritte NVARCHAR(200) NULL: Snapshot des Sage-Merkmals USER_OSArbeitsschritte -> ORDER_REMARK_1 der WiCAM-CSV.
-- Additiv. Nachtrag fuer vorhandene Positionen (nur wo leer) aus dem Artikel-Merkmal, damit offene Auftraege nach dem Update nicht ohne
-- Arbeitsschritte in die CSV gehen. Kein Merkmal am Artikel = bleibt leer (leeres Feld in der CSV).
-- SQL/AgentJobs nicht betroffen. Objektnamen wie die EF-Migration 20261118145602_AddBfPositionArbeitsschritte.
-- Idempotent (COL_LENGTH-Guard, Nachtrag WHERE Arbeitsschritte IS NULL), DDL im eigenen Batch, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.BfAuftragPositionen', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.BfAuftragPositionen fehlt - falsche Datenbank? Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.BfAuftragPositionen', N'Arbeitsschritte') IS NULL
BEGIN
    ALTER TABLE [dbo].[BfAuftragPositionen] ADD [Arbeitsschritte] NVARCHAR(200) NULL;
    PRINT 'Spalte BfAuftragPositionen.Arbeitsschritte angelegt.';
END
GO

UPDATE p
   SET p.Arbeitsschritte = LEFT(LTRIM(RTRIM(v.TextValue)), 200)
  FROM dbo.BfAuftragPositionen p
  JOIN dbo.Articles a ON a.ArticleNumber = p.ArticleNumber
  JOIN dbo.ArticleAttributeValues v ON v.ArticleId = a.Id
  JOIN dbo.ArticleAttributeDefinitions d ON d.Id = v.ArticleAttributeDefinitionId
 WHERE p.Arbeitsschritte IS NULL
   AND d.SyncSource = N'Sage' AND d.SyncFieldName = N'USER_OSArbeitsschritte'
   AND NULLIF(LTRIM(RTRIM(v.TextValue)), N'') IS NOT NULL;
PRINT CONCAT('Arbeitsschritte nachgetragen: ', @@ROWCOUNT, ' Position(en).');
GO

IF COL_LENGTH(N'dbo.BfAuftragPositionen', N'Arbeitsschritte') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261118145602_AddBfPositionArbeitsschritte')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261118145602_AddBfPositionArbeitsschritte', '10.0.2');
    PRINT 'Migration 20261118145602_AddBfPositionArbeitsschritte eingetragen.';
END
GO

SET NOEXEC OFF;
GO

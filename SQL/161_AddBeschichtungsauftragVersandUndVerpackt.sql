-- =============================================================================
-- 161_AddBeschichtungsauftragVersandUndVerpackt.sql  (v1.99.41, Spec 2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck, E1)
-- Beschichtungsauftrag: Status Versendet (Wert 35, tinyint ohne CHECK - kein Schema-Eingriff), Haken "verpackt" und freie
-- Positionen ohne FA je Position, Lackierer aus den Kreditoren.
--   dbo.Beschichtungsauftraege          + CreditorId INT NULL (FK Creditors, NO ACTION, Index), VersendetAt, VersendetByUserId
--   dbo.BeschichtungsauftragPositionen  + Verpackt BIT NOT NULL DEFAULT 0, OhneFa BIT NOT NULL DEFAULT 0
-- Rein additiv, nicht daten-destruktiv. Altbestand: alle Positionen Verpackt = 0, OhneFa = 0 (kein Datenskript, Freigabe-Antwort 15).
-- SQL/AgentJobs nicht betroffen. Objektnamen wie die EF-Migration 20261118120203_AddBeschichtungsauftragVersandUndVerpackt.
-- Idempotent (COL_LENGTH-/sys.*-Guards), DDL je Objekt im eigenen Batch, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Beschichtungsauftraege', N'U') IS NULL OR OBJECT_ID(N'dbo.BeschichtungsauftragPositionen', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Creditors', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.Beschichtungsauftraege/BeschichtungsauftragPositionen/Creditors fehlt - falsche Datenbank? Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.Beschichtungsauftraege', N'CreditorId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Beschichtungsauftraege] ADD [CreditorId] INT NULL;
    PRINT 'Spalte Beschichtungsauftraege.CreditorId angelegt.';
END
GO

IF COL_LENGTH(N'dbo.Beschichtungsauftraege', N'VersendetAt') IS NULL
BEGIN
    ALTER TABLE [dbo].[Beschichtungsauftraege] ADD [VersendetAt] DATETIME2 NULL;
    PRINT 'Spalte Beschichtungsauftraege.VersendetAt angelegt.';
END
GO

IF COL_LENGTH(N'dbo.Beschichtungsauftraege', N'VersendetByUserId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Beschichtungsauftraege] ADD [VersendetByUserId] INT NULL;
    PRINT 'Spalte Beschichtungsauftraege.VersendetByUserId angelegt.';
END
GO

IF COL_LENGTH(N'dbo.BeschichtungsauftragPositionen', N'Verpackt') IS NULL
BEGIN
    ALTER TABLE [dbo].[BeschichtungsauftragPositionen] ADD [Verpackt] BIT NOT NULL
        CONSTRAINT [DF_BeschichtungsauftragPositionen_Verpackt] DEFAULT 0;
    PRINT 'Spalte BeschichtungsauftragPositionen.Verpackt angelegt.';
END
GO

IF COL_LENGTH(N'dbo.BeschichtungsauftragPositionen', N'OhneFa') IS NULL
BEGIN
    ALTER TABLE [dbo].[BeschichtungsauftragPositionen] ADD [OhneFa] BIT NOT NULL
        CONSTRAINT [DF_BeschichtungsauftragPositionen_OhneFa] DEFAULT 0;
    PRINT 'Spalte BeschichtungsauftragPositionen.OhneFa angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Beschichtungsauftraege_CreditorId' AND object_id = OBJECT_ID(N'dbo.Beschichtungsauftraege'))
BEGIN
    CREATE INDEX [IX_Beschichtungsauftraege_CreditorId] ON [dbo].[Beschichtungsauftraege] ([CreditorId]);
    PRINT 'Index IX_Beschichtungsauftraege_CreditorId angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Beschichtungsauftraege_Creditors_CreditorId')
BEGIN
    ALTER TABLE [dbo].[Beschichtungsauftraege] ADD CONSTRAINT [FK_Beschichtungsauftraege_Creditors_CreditorId]
        FOREIGN KEY ([CreditorId]) REFERENCES [dbo].[Creditors] ([Id]);
    PRINT 'FK_Beschichtungsauftraege_Creditors_CreditorId angelegt.';
END
GO

IF COL_LENGTH(N'dbo.BeschichtungsauftragPositionen', N'OhneFa') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261118120203_AddBeschichtungsauftragVersandUndVerpackt')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261118120203_AddBeschichtungsauftragVersandUndVerpackt', '10.0.2');
    PRINT 'Migration 20261118120203_AddBeschichtungsauftragVersandUndVerpackt eingetragen.';
END
GO

SET NOEXEC OFF;
GO

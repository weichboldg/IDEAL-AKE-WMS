-- =============================================================================
-- 162_AddUserKuerzelUndBfBestellerKuerzel.sql  (v1.99.42, Nachbesserung BF-CSV, Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv)
-- Personenkuerzel am WMS-Benutzer (z. B. WAK) und Snapshot am BF-Auftrag fuer ORDER_REMARK_4 der WiCAM-CSV.
--   dbo.Users        + Kuerzel NVARCHAR(10) NULL, gefilterter UNIQUE-Index UQ_Users_Kuerzel (WHERE Kuerzel IS NOT NULL)
--   dbo.BfAuftraege  + BestellerKuerzel NVARCHAR(10) NULL (bei der Freigabe festgeschrieben)
-- Rein additiv, nicht daten-destruktiv. Altbestand: Kuerzel leer -> BF-Freigabe gesperrt, bis der Besteller ein Kuerzel hat.
-- SQL/AgentJobs nicht betroffen. Objektnamen wie die EF-Migration 20261118144836_AddUserKuerzelUndBfBestellerKuerzel.
-- Idempotent (COL_LENGTH-/sys.indexes-Guards), DDL je Objekt im eigenen Batch, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL OR OBJECT_ID(N'dbo.BfAuftraege', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.Users/BfAuftraege fehlt - falsche Datenbank? Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.Users', N'Kuerzel') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [Kuerzel] NVARCHAR(10) NULL;
    PRINT 'Spalte Users.Kuerzel angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Users_Kuerzel' AND object_id = OBJECT_ID(N'dbo.Users'))
BEGIN
    CREATE UNIQUE INDEX [UQ_Users_Kuerzel] ON [dbo].[Users] ([Kuerzel]) WHERE [Kuerzel] IS NOT NULL;
    PRINT 'Index UQ_Users_Kuerzel angelegt.';
END
GO

IF COL_LENGTH(N'dbo.BfAuftraege', N'BestellerKuerzel') IS NULL
BEGIN
    ALTER TABLE [dbo].[BfAuftraege] ADD [BestellerKuerzel] NVARCHAR(10) NULL;
    PRINT 'Spalte BfAuftraege.BestellerKuerzel angelegt.';
END
GO

IF COL_LENGTH(N'dbo.BfAuftraege', N'BestellerKuerzel') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Users_Kuerzel' AND object_id = OBJECT_ID(N'dbo.Users'))
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261118144836_AddUserKuerzelUndBfBestellerKuerzel')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261118144836_AddUserKuerzelUndBfBestellerKuerzel', '10.0.2');
    PRINT 'Migration 20261118144836_AddUserKuerzelUndBfBestellerKuerzel eingetragen.';
END
GO

SET NOEXEC OFF;
GO

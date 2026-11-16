-- =============================================================================
-- 152_AddUserMussKennwortAendern.sql  (v1.99.28, Spec 2026-11-14-sicherheit-anmeldung-und-installer, ADR 0027)
-- Kennzeichen am Benutzer: das gespeicherte Kennwort hat ein Admin bzw. der Start-Seed vergeben (Erstkennwort);
-- die naechste Formular-Anmeldung verlangt erst ein eigenes Kennwort (Pflichtwechsel, Regel P).
-- Rein additiv (bit NOT NULL DEFAULT 0): Bestandsbenutzer bekommen 0 = kein Pflichtwechsel. Kein Datenverlust.
-- Objektnamen wie die EF-Migration 20261116165705_AddUserMussKennwortAendern. Idempotent (COL_LENGTH-Guard), History separat.
-- SQL/151 ist fuer die bedingte Index-Reparatur reserviert (nur bei P-2b-Treffern auf IDEAL-Prod). SQL/AgentJobs nicht betroffen.
-- REIHENFOLGE beim Deploy: vor dem Web-Start (das neue Web liest die Spalte beim admin-Seed und im Login).
-- =============================================================================
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.Users fehlt - falsche Datenbank? Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.Users', N'MussKennwortAendern') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [MussKennwortAendern] bit NOT NULL CONSTRAINT [DF_Users_MussKennwortAendern] DEFAULT CAST(0 AS bit);
    PRINT N'Spalte Users.MussKennwortAendern angelegt.';
END
ELSE
    PRINT N'Spalte Users.MussKennwortAendern existiert bereits.';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261116165705_AddUserMussKennwortAendern')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261116165705_AddUserMussKennwortAendern', '10.0.2');
    PRINT 'Migration 20261116165705_AddUserMussKennwortAendern eingetragen.';
END
GO

SET NOEXEC OFF;
GO

-- =============================================
-- Migration: Email, IsAdmin, NotifyOnReorderLevel zu Users hinzufügen
-- Beschreibung: E-Mail-Adresse für Benachrichtigungen, Admin-Flag für
--               Service-Einstellungen, Notification-Flag für Meldebestand-Mail.
-- =============================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'Email'
)
    ALTER TABLE [dbo].[Users]
        ADD [Email] nvarchar(200) NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'IsAdmin'
)
    ALTER TABLE [dbo].[Users]
        ADD [IsAdmin] bit NOT NULL DEFAULT 0;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'NotifyOnReorderLevel'
)
    ALTER TABLE [dbo].[Users]
        ADD [NotifyOnReorderLevel] bit NOT NULL DEFAULT 0;
GO

-- EF Core Migrations-History markieren
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260311071826_AddUserEmailIsAdminNotify')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260311071826_AddUserEmailIsAdminNotify', '10.0.0');
-- Alten falschen Eintrag bereinigen (falls vorhanden)
DELETE FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260311000001_AddUserEmailIsAdminNotify';
GO

-- =============================================
-- 73_AddWindowsUserNameDropAdGroup.sql (v1.23.0)
-- Fuegt der Users-Tabelle die Spalte WindowsUserName (NVARCHAR(200) NULL) als
-- Windows-Auth-Login-Schluessel hinzu (gefilterter Unique-Index UQ_Users_WindowsUserName)
-- und entfernt die bereits aus dem Code entfernte Spalte Roles.AdGroup.
-- Idempotent, kann mehrfach ausgefuehrt werden (Reapply-fest).
--
-- Entspricht der EF-Migration 20260618070606_AddWindowsUserNameDropAdGroup.
-- =============================================

-- =============================================
-- SECTION A: SPALTE Users.WindowsUserName ANLEGEN (idempotent, eigener Guard)
-- =============================================
IF COL_LENGTH('dbo.Users', 'WindowsUserName') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD [WindowsUserName] NVARCHAR(200) NULL;
    PRINT 'Spalte Users.WindowsUserName erstellt.';
END
ELSE
    PRINT 'Spalte Users.WindowsUserName bereits vorhanden - uebersprungen.';
GO

-- =============================================
-- SECTION B: GEFILTERTER UNIQUE-INDEX (idempotent)
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Users_WindowsUserName' AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
    CREATE UNIQUE INDEX [UQ_Users_WindowsUserName]
        ON [dbo].[Users] ([WindowsUserName])
        WHERE [WindowsUserName] IS NOT NULL;
    PRINT 'Index UQ_Users_WindowsUserName erstellt.';
END
ELSE
    PRINT 'Index UQ_Users_WindowsUserName bereits vorhanden - uebersprungen.';
GO

-- =============================================
-- SECTION C: SPALTE Roles.AdGroup ENTFERNEN (idempotent)
-- =============================================
IF COL_LENGTH('dbo.Roles', 'AdGroup') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Roles] DROP COLUMN [AdGroup];
    PRINT 'Spalte Roles.AdGroup entfernt.';
END
ELSE
    PRINT 'Spalte Roles.AdGroup bereits entfernt - uebersprungen.';
GO

-- =============================================
-- SECTION D: EF MIGRATIONS HISTORY
-- =============================================
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260618070606_AddWindowsUserNameDropAdGroup')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260618070606_AddWindowsUserNameDropAdGroup', '10.0.2');
GO

PRINT '73_AddWindowsUserNameDropAdGroup abgeschlossen.';
GO

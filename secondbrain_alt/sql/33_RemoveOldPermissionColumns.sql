-- =============================================
-- 33: Alte Berechtigungs-Spalten entfernen (Phase 2)
-- ACHTUNG: Erst ausfuehren nachdem Phase 1 verifiziert wurde!
-- Stellt sicher, dass alle Berechtigungen korrekt in UserRoles migriert sind.
-- =============================================

USE [IDEAL_AKE_WMS]
GO

-- IsAdmin entfernen
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = 'IsAdmin')
BEGIN
    ALTER TABLE [dbo].[Users] DROP COLUMN [IsAdmin];
    PRINT 'Spalte IsAdmin entfernt.';
END
GO

-- HasMasterDataAccess entfernen
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = 'HasMasterDataAccess')
BEGIN
    ALTER TABLE [dbo].[Users] DROP COLUMN [HasMasterDataAccess];
    PRINT 'Spalte HasMasterDataAccess entfernt.';
END
GO

-- CanPick entfernen
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = 'CanPick')
BEGIN
    ALTER TABLE [dbo].[Users] DROP COLUMN [CanPick];
    PRINT 'Spalte CanPick entfernt.';
END
GO

-- CanViewTracking entfernen
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = 'CanViewTracking')
BEGIN
    ALTER TABLE [dbo].[Users] DROP COLUMN [CanViewTracking];
    PRINT 'Spalte CanViewTracking entfernt.';
END
GO

-- CanReportOperations entfernen
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = 'CanReportOperations')
BEGIN
    ALTER TABLE [dbo].[Users] DROP COLUMN [CanReportOperations];
    PRINT 'Spalte CanReportOperations entfernt.';
END
GO

-- =============================================
-- EF Migrations History (Phase 2 Migration)
-- =============================================
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = 'Phase2_RemoveOldPermissionColumns')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('Phase2_RemoveOldPermissionColumns', '10.0.2');
GO

PRINT 'Migration 33_RemoveOldPermissionColumns abgeschlossen.';
GO

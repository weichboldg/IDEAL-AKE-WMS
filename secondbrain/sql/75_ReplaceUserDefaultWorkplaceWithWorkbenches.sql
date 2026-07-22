-- =============================================
-- 75_ReplaceUserDefaultWorkplaceWithWorkbenches.sql (v1.23.0)
-- Ersetzt Users.DefaultWorkplaceId (FK -> ProductionWorkplaces) durch
-- Users.DefaultWorkbenches (NVARCHAR(400), kommasepariert). Idempotent.
-- Entspricht der EF-Migration 20260625061803_ReplaceUserDefaultWorkplaceWithWorkbenches.
-- HINWEIS: destruktiv — bestehende DefaultWorkplaceId-Werte gehen verloren.
-- =============================================

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Users_ProductionWorkplaces_DefaultWorkplaceId')
BEGIN
    ALTER TABLE [dbo].[Users] DROP CONSTRAINT [FK_Users_ProductionWorkplaces_DefaultWorkplaceId];
    PRINT 'FK FK_Users_ProductionWorkplaces_DefaultWorkplaceId entfernt.';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_DefaultWorkplaceId' AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
    DROP INDEX [IX_Users_DefaultWorkplaceId] ON [dbo].[Users];
    PRINT 'Index IX_Users_DefaultWorkplaceId entfernt.';
END
GO

IF COL_LENGTH('dbo.Users', 'DefaultWorkplaceId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Users] DROP COLUMN [DefaultWorkplaceId];
    PRINT 'Spalte Users.DefaultWorkplaceId entfernt.';
END
GO

IF COL_LENGTH('dbo.Users', 'DefaultWorkbenches') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [DefaultWorkbenches] NVARCHAR(400) NULL;
    PRINT 'Spalte Users.DefaultWorkbenches erstellt.';
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260625061803_ReplaceUserDefaultWorkplaceWithWorkbenches')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260625061803_ReplaceUserDefaultWorkplaceWithWorkbenches', '10.0.2');
GO

PRINT '75_ReplaceUserDefaultWorkplaceWithWorkbenches abgeschlossen.';
GO

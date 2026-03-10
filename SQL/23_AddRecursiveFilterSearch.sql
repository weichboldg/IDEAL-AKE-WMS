-- =============================================
-- Migration: RecursiveFilterSearch zu Users hinzufügen
-- Beschreibung: Benutzer-Einstellung "Rekursive Suche bei aktiver Filterung"
--               in der Stücklisten-Ansicht.
-- =============================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'RecursiveFilterSearch'
)
    ALTER TABLE [dbo].[Users]
        ADD [RecursiveFilterSearch] bit NOT NULL DEFAULT 0;
GO

-- EF Core Migrations-History markieren
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260310130059_AddRecursiveFilterSearch')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260310130059_AddRecursiveFilterSearch', '10.0.0');
GO

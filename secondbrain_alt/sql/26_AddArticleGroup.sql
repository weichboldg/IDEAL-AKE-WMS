-- =============================================
-- Migration 26: Artikelgruppe zu Articles hinzufügen
-- Ziel: [IDEAL_AKE_WMS].[dbo].[Articles]
-- =============================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Articles]')
      AND name = N'ArticleGroup'
)
BEGIN
    ALTER TABLE [dbo].[Articles]
        ADD [ArticleGroup] nvarchar(100) NULL;
END
GO

-- EF Migrations-History markieren
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260311145014_AddArticleGroup'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260311145014_AddArticleGroup', N'10.0.2');
END
GO

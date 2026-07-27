-- =============================================
-- 43_AddBdeWerkbankSettings.sql
-- BdeAktiv + BdeDefaultArbeitsgang auf ProductionWorkplaces
-- =============================================

IF COL_LENGTH('dbo.ProductionWorkplaces', 'BdeAktiv') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductionWorkplaces]
    ADD [BdeAktiv] BIT NOT NULL CONSTRAINT [DF_ProductionWorkplaces_BdeAktiv] DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.ProductionWorkplaces', 'BdeDefaultArbeitsgang') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductionWorkplaces]
    ADD [BdeDefaultArbeitsgang] NVARCHAR(200) NULL;
END
GO

-- __EFMigrationsHistory
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260420104948_AddBdeWerkbankSettings')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId],[ProductVersion])
    VALUES ('20260420104948_AddBdeWerkbankSettings','10.0.2');
GO

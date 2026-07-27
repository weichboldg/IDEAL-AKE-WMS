-- Migration 22: Tabelle ProductionWorkplaces anlegen
-- Werkbänke / Produktionsarbeitsplätze

IF OBJECT_ID('dbo.ProductionWorkplaces', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductionWorkplaces] (
        [Id]                    INT            NOT NULL IDENTITY(1,1),
        [Name]                  NVARCHAR(200)  NOT NULL,
        [Hall]                  NVARCHAR(200)  NULL,
        [OverridePrePickingDays] INT           NULL,
        [CreatedAt]             DATETIME2      NOT NULL,
        [CreatedBy]             NVARCHAR(200)  NOT NULL,
        [CreatedByWindows]      NVARCHAR(200)  NOT NULL,
        [ModifiedAt]            DATETIME2      NULL,
        [ModifiedBy]            NVARCHAR(200)  NULL,
        [ModifiedByWindows]     NVARCHAR(200)  NULL,
        CONSTRAINT [PK_ProductionWorkplaces] PRIMARY KEY ([Id])
    );
END
GO

-- EF Migrations-History-Eintrag
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260306081711_AddProductionWorkplaces')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260306081711_AddProductionWorkplaces', '10.0.0');
END
GO

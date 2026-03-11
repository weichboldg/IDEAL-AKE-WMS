-- =============================================
-- Migration: ServiceSettings-Tabelle erstellen
-- Beschreibung: Laufzeitveränderliche Konfiguration für den IDALAKEWMSService.
--               Verwaltung im Web-App unter Stammdaten → Service-Einstellungen
--               (nur für Administratoren zugänglich).
-- =============================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[ServiceSettings]')
)
BEGIN
    CREATE TABLE [dbo].[ServiceSettings] (
        [Key]         nvarchar(100) NOT NULL,
        [Value]       nvarchar(500) NOT NULL,
        [Category]    nvarchar(100) NULL,
        [Description] nvarchar(500) NULL,
        CONSTRAINT [PK_ServiceSettings] PRIMARY KEY ([Key])
    );
END
GO

-- Standard-Einträge (idempotent)
IF NOT EXISTS (SELECT 1 FROM [dbo].[ServiceSettings] WHERE [Key] = 'Notifications:MeldebestandEnabled')
    INSERT INTO [dbo].[ServiceSettings] ([Key], [Value], [Category], [Description])
    VALUES ('Notifications:MeldebestandEnabled', 'true', 'Notifications', 'Meldebestand-Mail aktiv (true/false)');

IF NOT EXISTS (SELECT 1 FROM [dbo].[ServiceSettings] WHERE [Key] = 'Notifications:MeldebestandSubject')
    INSERT INTO [dbo].[ServiceSettings] ([Key], [Value], [Category], [Description])
    VALUES ('Notifications:MeldebestandSubject', 'Meldebestand unterschritten — IDEAL AKE WMS', 'Notifications', 'Betreff der Meldebestand-Mail');

IF NOT EXISTS (SELECT 1 FROM [dbo].[ServiceSettings] WHERE [Key] = 'Notifications:Recipients')
    INSERT INTO [dbo].[ServiceSettings] ([Key], [Value], [Category], [Description])
    VALUES ('Notifications:Recipients', '', 'Notifications', 'Feste Empfänger für Meldebestand-Mail (kommagetrennt)');

IF NOT EXISTS (SELECT 1 FROM [dbo].[ServiceSettings] WHERE [Key] = 'Notifications:AppBaseUrl')
    INSERT INTO [dbo].[ServiceSettings] ([Key], [Value], [Category], [Description])
    VALUES ('Notifications:AppBaseUrl', '', 'Notifications', 'Basis-URL der App für Links in Mails (z.B. https://wms.ake.at)');

IF NOT EXISTS (SELECT 1 FROM [dbo].[ServiceSettings] WHERE [Key] = 'Sync:ProductionOrdersEnabled')
    INSERT INTO [dbo].[ServiceSettings] ([Key], [Value], [Category], [Description])
    VALUES ('Sync:ProductionOrdersEnabled', 'true', 'Sync', 'Produktionsaufträge-Sync aus SAGE aktiv (true/false)');

IF NOT EXISTS (SELECT 1 FROM [dbo].[ServiceSettings] WHERE [Key] = 'Sync:ArticlesEnabled')
    INSERT INTO [dbo].[ServiceSettings] ([Key], [Value], [Category], [Description])
    VALUES ('Sync:ArticlesEnabled', 'true', 'Sync', 'Artikel-Sync aus SAGE aktiv (true/false)');
GO

-- EF Core Migrations-History markieren
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260311000002_AddServiceSettings')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260311000002_AddServiceSettings', '10.0.0');
GO

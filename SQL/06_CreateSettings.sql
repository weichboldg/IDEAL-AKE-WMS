-- AKE BDE Light - Settings & Holidays + Article ReorderLevel
USE [AKE_BDE_Light]
GO

-- AppSettings (Key-Value)
CREATE TABLE [dbo].[AppSettings] (
    [Key]           NVARCHAR(100)   NOT NULL,
    [Value]         NVARCHAR(500)   NOT NULL,
    [Description]   NVARCHAR(500)   NULL,
    CONSTRAINT [PK_AppSettings] PRIMARY KEY CLUSTERED ([Key] ASC)
);
GO

-- Default-Werte
INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description]) VALUES
    ('KommissionierTage', '4', 'Arbeitstage vor Fertigungstermin für Kommissionierung'),
    ('VorkommissionierTage', '1', 'Arbeitstage vor Kommissioniertermin für Vorkommissionierung'),
    ('BeschichtungTage', '10', 'Arbeitstage vor Kommissioniertermin für Beschichtung'),
    ('WarningThresholdPercent', '150', 'Warnschwelle Meldebestand in % (orange)'),
    ('CriticalThresholdPercent', '100', 'Kritische Schwelle Meldebestand in % (rot)');
GO

-- Holidays (Feiertage)
CREATE TABLE [dbo].[Holidays] (
    [Id]                INT             IDENTITY(1,1) NOT NULL,
    [Date]              DATE            NOT NULL,
    [Description]       NVARCHAR(200)   NULL,
    [CreatedAt]         DATETIME2       NOT NULL DEFAULT GETDATE(),
    [CreatedBy]         NVARCHAR(200)   NOT NULL,
    [CreatedByWindows]  NVARCHAR(200)   NOT NULL,
    [ModifiedAt]        DATETIME2       NULL,
    [ModifiedBy]        NVARCHAR(200)   NULL,
    [ModifiedByWindows] NVARCHAR(200)   NULL,
    CONSTRAINT [PK_Holidays] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Holidays_Date]
    ON [dbo].[Holidays]([Date] ASC);
GO

-- Article: Meldebestand hinzufügen
ALTER TABLE [dbo].[Articles] ADD [ReorderLevel] DECIMAL(18,3) NULL;
GO

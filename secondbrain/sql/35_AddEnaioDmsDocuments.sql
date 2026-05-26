-- Migration: AddEnaioDmsDocuments
-- Tabelle fuer enaio DMS-Dokumente (Werkstattauftraege + Zeichnungen)
-- Daten werden per IDEALAKEWMSService aus enaio synchronisiert

IF OBJECT_ID(N'dbo.EnaioDmsDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EnaioDmsDocuments] (
        [Id]                 INT            IDENTITY(1,1) NOT NULL,
        [EnaioDmsObjectId]   BIGINT         NOT NULL,
        [DocumentType]       NVARCHAR(100)  NOT NULL,
        [OrderNumber]        NVARCHAR(100)  NULL,
        [CreatedInEnaio]     DATETIME2      NOT NULL,
        [LastSyncedAt]       DATETIME2      NOT NULL,
        [CreatedAt]          DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]          NVARCHAR(200)  NULL,
        [CreatedByWindows]   NVARCHAR(200)  NULL,
        [ModifiedAt]         DATETIME2      NULL,
        [ModifiedBy]         NVARCHAR(200)  NULL,
        [ModifiedByWindows]  NVARCHAR(200)  NULL,
        CONSTRAINT [PK_EnaioDmsDocuments] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_EnaioDmsDocuments_ObjectId] UNIQUE ([EnaioDmsObjectId])
    );

    CREATE NONCLUSTERED INDEX [IX_EnaioDmsDocuments_OrderNumber]
        ON [dbo].[EnaioDmsDocuments] ([OrderNumber]);

    PRINT 'Tabelle EnaioDmsDocuments erstellt.';
END
GO

-- EF Migrations-History
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] LIKE '%_AddEnaioDmsDocuments')
BEGIN
    DECLARE @migrationId NVARCHAR(150);
    SELECT TOP 1 @migrationId = [MigrationId] FROM [__EFMigrationsHistory] ORDER BY [MigrationId] DESC;
    -- Manuell den korrekten MigrationId eintragen nach Erstellung der EF-Migration
    PRINT 'Hinweis: MigrationId manuell in __EFMigrationsHistory eintragen.';
END
GO

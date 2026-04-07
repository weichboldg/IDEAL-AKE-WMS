-- =============================================
-- 39: ArticleCategories + ArticleAttributes (EAV)
-- =============================================

-- ArticleCategories
IF OBJECT_ID(N'[dbo].[ArticleCategories]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleCategories] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [Name]              NVARCHAR(200)  NOT NULL,
        [Description]       NVARCHAR(500)  NULL,
        [OseonTyp]          INT            NULL,
        [Source]            NVARCHAR(50)   NULL,
        [CreatedAt]         DATETIME2      NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [CreatedByWindows]  NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [ModifiedAt]        DATETIME2      NULL,
        [ModifiedBy]        NVARCHAR(200)  NULL,
        [ModifiedByWindows] NVARCHAR(200)  NULL,
        CONSTRAINT [PK_ArticleCategories] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ArticleCategories_Name] UNIQUE ([Name])
    );
    PRINT 'Tabelle [ArticleCategories] erstellt.';
END
GO

-- Articles: ArticleCategoryId FK
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Articles') AND name = 'ArticleCategoryId')
BEGIN
    ALTER TABLE [dbo].[Articles] ADD [ArticleCategoryId] INT NULL;

    ALTER TABLE [dbo].[Articles] ADD CONSTRAINT [FK_Articles_ArticleCategories]
        FOREIGN KEY ([ArticleCategoryId]) REFERENCES [dbo].[ArticleCategories]([Id])
        ON DELETE SET NULL;

    CREATE NONCLUSTERED INDEX [IX_Articles_ArticleCategoryId]
        ON [dbo].[Articles] ([ArticleCategoryId]);

    PRINT 'Spalte [Articles].[ArticleCategoryId] mit FK + Index erstellt.';
END
GO

-- ArticleAttributeDefinitions
IF OBJECT_ID(N'[dbo].[ArticleAttributeDefinitions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleAttributeDefinitions] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [Name]              NVARCHAR(200)  NOT NULL,
        [AttributeType]     INT            NOT NULL DEFAULT 0,
        [SortOrder]         INT            NOT NULL DEFAULT 0,
        [IsActive]          BIT            NOT NULL DEFAULT 1,
        [SyncSource]        NVARCHAR(50)   NULL,
        [SyncFieldName]     NVARCHAR(200)  NULL,
        [CreatedAt]         DATETIME2      NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [CreatedByWindows]  NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [ModifiedAt]        DATETIME2      NULL,
        [ModifiedBy]        NVARCHAR(200)  NULL,
        [ModifiedByWindows] NVARCHAR(200)  NULL,
        CONSTRAINT [PK_ArticleAttributeDefinitions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ArticleAttributeDefinitions_Name] UNIQUE ([Name])
    );
    PRINT 'Tabelle [ArticleAttributeDefinitions] erstellt.';
END
GO

-- ArticleAttributeOptions
IF OBJECT_ID(N'[dbo].[ArticleAttributeOptions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleAttributeOptions] (
        [Id]                              INT            IDENTITY(1,1) NOT NULL,
        [ArticleAttributeDefinitionId]    INT            NOT NULL,
        [Value]                           NVARCHAR(200)  NOT NULL,
        [SortOrder]                       INT            NOT NULL DEFAULT 0,
        CONSTRAINT [PK_ArticleAttributeOptions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ArticleAttributeOptions_Definitions]
            FOREIGN KEY ([ArticleAttributeDefinitionId])
            REFERENCES [dbo].[ArticleAttributeDefinitions]([Id])
            ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_ArticleAttributeOptions_DefinitionId]
        ON [dbo].[ArticleAttributeOptions] ([ArticleAttributeDefinitionId]);

    PRINT 'Tabelle [ArticleAttributeOptions] erstellt.';
END
GO

-- ArticleAttributeValues
IF OBJECT_ID(N'[dbo].[ArticleAttributeValues]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleAttributeValues] (
        [Id]                              INT            IDENTITY(1,1) NOT NULL,
        [ArticleId]                       INT            NOT NULL,
        [ArticleAttributeDefinitionId]    INT            NOT NULL,
        [BooleanValue]                    BIT            NULL,
        [SelectedOptionId]                INT            NULL,
        [CreatedAt]                       DATETIME2      NOT NULL DEFAULT GETDATE(),
        [CreatedBy]                       NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [CreatedByWindows]                NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [ModifiedAt]                      DATETIME2      NULL,
        [ModifiedBy]                      NVARCHAR(200)  NULL,
        [ModifiedByWindows]               NVARCHAR(200)  NULL,
        CONSTRAINT [PK_ArticleAttributeValues] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ArticleAttributeValues_Articles]
            FOREIGN KEY ([ArticleId])
            REFERENCES [dbo].[Articles]([Id])
            ON DELETE CASCADE,
        CONSTRAINT [FK_ArticleAttributeValues_Definitions]
            FOREIGN KEY ([ArticleAttributeDefinitionId])
            REFERENCES [dbo].[ArticleAttributeDefinitions]([Id])
            ON DELETE CASCADE,
        -- NO ACTION (statt SET NULL): verhindert "multiple cascade paths" Fehler
        -- in SQL Server. UI verhindert Loeschen einer Option in Verwendung.
        CONSTRAINT [FK_ArticleAttributeValues_Options]
            FOREIGN KEY ([SelectedOptionId])
            REFERENCES [dbo].[ArticleAttributeOptions]([Id])
            ON DELETE NO ACTION
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_ArticleAttributeValues_ArticleId_DefinitionId]
        ON [dbo].[ArticleAttributeValues] ([ArticleId], [ArticleAttributeDefinitionId]);

    CREATE NONCLUSTERED INDEX [IX_ArticleAttributeValues_ArticleId]
        ON [dbo].[ArticleAttributeValues] ([ArticleId]);

    PRINT 'Tabelle [ArticleAttributeValues] erstellt.';
END
GO

-- EF Migrations History
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] LIKE '%_AddArticleCategoriesAndAttributes')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260407061642_AddArticleCategoriesAndAttributes', '10.0.0');
GO

PRINT 'Migration 39 (ArticleCategories + ArticleAttributes) abgeschlossen.';
GO

-- =============================================
-- 36: Bedarfsmeldungen + Empfänger-Verwaltung
-- =============================================

-- OrderRecipientGroups
IF OBJECT_ID(N'[dbo].[OrderRecipientGroups]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderRecipientGroups] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [CreatedByWindows] nvarchar(200) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [ModifiedByWindows] nvarchar(200) NULL,
        CONSTRAINT [PK_OrderRecipientGroups] PRIMARY KEY ([Id])
    );
END
GO

-- OrderRecipients
IF OBJECT_ID(N'[dbo].[OrderRecipients]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderRecipients] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [OrderRecipientGroupId] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Email] nvarchar(300) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [CreatedByWindows] nvarchar(200) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [ModifiedByWindows] nvarchar(200) NULL,
        CONSTRAINT [PK_OrderRecipients] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderRecipients_OrderRecipientGroups] FOREIGN KEY ([OrderRecipientGroupId])
            REFERENCES [dbo].[OrderRecipientGroups]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_OrderRecipients_GroupId] ON [dbo].[OrderRecipients]([OrderRecipientGroupId]);
END
GO

-- ArticleGroupRecipientMappings
IF OBJECT_ID(N'[dbo].[ArticleGroupRecipientMappings]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleGroupRecipientMappings] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [ArticleGroup] nvarchar(100) NOT NULL,
        [OrderRecipientGroupId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [CreatedByWindows] nvarchar(200) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [ModifiedByWindows] nvarchar(200) NULL,
        CONSTRAINT [PK_ArticleGroupRecipientMappings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ArticleGroupRecipientMappings_OrderRecipientGroups] FOREIGN KEY ([OrderRecipientGroupId])
            REFERENCES [dbo].[OrderRecipientGroups]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_ArticleGroupRecipientMappings_ArticleGroup] ON [dbo].[ArticleGroupRecipientMappings]([ArticleGroup]);
    CREATE UNIQUE INDEX [UX_ArticleGroupRecipientMappings_Group_Recipient] ON [dbo].[ArticleGroupRecipientMappings]([ArticleGroup], [OrderRecipientGroupId]);
END
GO

-- PartRequisitions
IF OBJECT_ID(N'[dbo].[PartRequisitions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PartRequisitions] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [ProductionOrderId] int NOT NULL,
        [ArticleNumber] nvarchar(100) NOT NULL,
        [ArticleDescription] nvarchar(500) NULL,
        [ArticleGroup] nvarchar(100) NULL,
        [Position] nvarchar(50) NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [Unit] nvarchar(20) NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT 'Offen',
        [Priority] nvarchar(20) NOT NULL DEFAULT 'Normal',
        [Notes] nvarchar(1000) NULL,
        [OrderRecipientGroupId] int NULL,
        [SentToEmails] nvarchar(1000) NULL,
        [EmailSentAt] datetime2 NULL,
        [FulfilledByStockMovementId] int NULL,
        [FulfilledAt] datetime2 NULL,
        [CancelledAt] datetime2 NULL,
        [CancelledBy] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [CreatedByWindows] nvarchar(200) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [ModifiedByWindows] nvarchar(200) NULL,
        CONSTRAINT [PK_PartRequisitions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PartRequisitions_ProductionOrders] FOREIGN KEY ([ProductionOrderId])
            REFERENCES [dbo].[ProductionOrders]([Id]),
        CONSTRAINT [FK_PartRequisitions_OrderRecipientGroups] FOREIGN KEY ([OrderRecipientGroupId])
            REFERENCES [dbo].[OrderRecipientGroups]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_PartRequisitions_StockMovements] FOREIGN KEY ([FulfilledByStockMovementId])
            REFERENCES [dbo].[StockMovements]([Id]) ON DELETE SET NULL
    );

    CREATE INDEX [IX_PartRequisitions_ProductionOrderId] ON [dbo].[PartRequisitions]([ProductionOrderId]);
    CREATE INDEX [IX_PartRequisitions_ArticleNumber] ON [dbo].[PartRequisitions]([ArticleNumber]);
    CREATE INDEX [IX_PartRequisitions_Status] ON [dbo].[PartRequisitions]([Status]);
    CREATE INDEX [IX_PartRequisitions_EmailSentAt_Status] ON [dbo].[PartRequisitions]([EmailSentAt], [Status]);
END
GO

-- AppSetting: BestellungenAktiv
IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = 'BestellungenAktiv')
BEGIN
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES ('BestellungenAktiv', 'false', 'Bedarfsmeldungen aus Stückliste aktivieren');
END
GO

-- EF Migrations History
INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT '20260403055243_AddPartRequisitions', '10.0.0'
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = '20260403055243_AddPartRequisitions'
);
GO

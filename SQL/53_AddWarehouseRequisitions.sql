-- Phase: Lagerbestellung v1.8.4
-- Idempotent: WarehouseRequisitions + WarehouseRequisitionItems anlegen.

IF OBJECT_ID('dbo.WarehouseRequisitions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WarehouseRequisitions (
        Id int IDENTITY(1,1) PRIMARY KEY,
        ProductionWorkplaceId int NOT NULL,
        Status tinyint NOT NULL,
        OrderRecipientGroupId int NULL,
        SubmittedAt datetime2 NULL,
        SubmittedByUserId int NULL,
        ClosedAt datetime2 NULL,
        ClosedByUserId int NULL,
        CancelledAt datetime2 NULL,
        CancelledByUserId int NULL,
        CancellationReason nvarchar(500) NULL,
        EmailSentAt datetime2 NULL,
        CancellationEmailSentAt datetime2 NULL,
        RowVersion rowversion NOT NULL,
        CreatedAt datetime2 NOT NULL,
        CreatedBy nvarchar(200) NOT NULL,
        CreatedByWindows nvarchar(200) NOT NULL,
        ModifiedAt datetime2 NULL,
        ModifiedBy nvarchar(200) NULL,
        ModifiedByWindows nvarchar(200) NULL,
        CONSTRAINT FK_WarehouseRequisitions_ProductionWorkplaces FOREIGN KEY (ProductionWorkplaceId)
            REFERENCES dbo.ProductionWorkplaces(Id),
        CONSTRAINT FK_WarehouseRequisitions_OrderRecipientGroups FOREIGN KEY (OrderRecipientGroupId)
            REFERENCES dbo.OrderRecipientGroups(Id)
    );

    CREATE INDEX IX_WarehouseRequisitions_Status ON dbo.WarehouseRequisitions(Status);
    CREATE INDEX IX_WarehouseRequisitions_ProductionWorkplaceId ON dbo.WarehouseRequisitions(ProductionWorkplaceId);
    CREATE INDEX IX_WarehouseRequisitions_SubmittedAt ON dbo.WarehouseRequisitions(SubmittedAt);
END
GO

IF OBJECT_ID('dbo.WarehouseRequisitionItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WarehouseRequisitionItems (
        Id int IDENTITY(1,1) PRIMARY KEY,
        WarehouseRequisitionId int NOT NULL,
        ArticleNumber nvarchar(100) NOT NULL,
        ArticleDescription nvarchar(500) NOT NULL,
        Unit nvarchar(20) NULL,
        QuantityRequested decimal(18,4) NOT NULL,
        QuantityPicked decimal(18,4) NULL,
        Position int NOT NULL,
        CreatedAt datetime2 NOT NULL,
        CreatedBy nvarchar(200) NOT NULL,
        CreatedByWindows nvarchar(200) NOT NULL,
        ModifiedAt datetime2 NULL,
        ModifiedBy nvarchar(200) NULL,
        ModifiedByWindows nvarchar(200) NULL,
        CONSTRAINT FK_WarehouseRequisitionItems_WarehouseRequisitions FOREIGN KEY (WarehouseRequisitionId)
            REFERENCES dbo.WarehouseRequisitions(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_WarehouseRequisitionItems_RequisitionId_Position
        ON dbo.WarehouseRequisitionItems(WarehouseRequisitionId, Position);
    CREATE UNIQUE INDEX IX_WarehouseRequisitionItems_RequisitionId_ArticleNumber
        ON dbo.WarehouseRequisitionItems(WarehouseRequisitionId, ArticleNumber);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId LIKE '%_AddWarehouseRequisitions')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260430183954_AddWarehouseRequisitions', '10.0.2');
END
GO

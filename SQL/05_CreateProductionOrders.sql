-- AKE BDE Light - ProductionOrders (Werkstattaufträge)
USE [AKE_BDE_Light]
GO

CREATE TABLE [dbo].[ProductionOrders] (
    [Id]                INT             IDENTITY(1,1) NOT NULL,
    [OrderNumber]       NVARCHAR(100)   NOT NULL,
    [Quantity]          DECIMAL(18,3)   NOT NULL DEFAULT 0,
    [Customer]          NVARCHAR(200)   NULL,
    [ArticleNumber]     NVARCHAR(100)   NULL,
    [Description1]      NVARCHAR(500)   NULL,
    [Description2]      NVARCHAR(500)   NULL,
    [ProductionDate]    DATETIME2       NULL,
    [DeliveryDate]      DATETIME2       NULL,
    [IsDone]            BIT             NOT NULL DEFAULT 0,
    [CreatedAt]         DATETIME2       NOT NULL DEFAULT GETDATE(),
    [CreatedBy]         NVARCHAR(200)   NOT NULL,
    [CreatedByWindows]  NVARCHAR(200)   NOT NULL,
    [ModifiedAt]        DATETIME2       NULL,
    [ModifiedBy]        NVARCHAR(200)   NULL,
    [ModifiedByWindows] NVARCHAR(200)   NULL,
    CONSTRAINT [PK_ProductionOrders] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_ProductionOrders_OrderNumber]
    ON [dbo].[ProductionOrders]([OrderNumber] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_ProductionOrders_ArticleNumber]
    ON [dbo].[ProductionOrders]([ArticleNumber] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_ProductionOrders_IsDone]
    ON [dbo].[ProductionOrders]([IsDone] ASC);
GO

-- SQL/81_AddProductionOrderExtraInfo.sql
-- Migration 81: AddProductionOrderExtraInfo (v1.26.0)
-- FA-Zusatzinfos aus Sage: 1:1-Satellit ProductionOrderExtraInfo (UNIQUE-FK, Cascade).
-- Rein additiv, idempotent. Befuellt AUSSCHLIESSLICH vom FaZusatzinfoSyncService
-- (kein AgentJob-Eager-Create).
SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.ProductionOrderExtraInfo', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductionOrderExtraInfo] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [ProductionOrderId] INT               NOT NULL,
        [Kaeltemittel]      NVARCHAR(200)     NULL,
        [Ventil]            NVARCHAR(200)     NULL,
        [AusfuehrungEZ]     NVARCHAR(200)     NULL,
        [Maschine]          NVARCHAR(200)     NULL,
        [SageStatus]        NVARCHAR(200)     NULL,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionOrderExtraInfo] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ProductionOrderExtraInfo_ProductionOrderId] UNIQUE ([ProductionOrderId]),
        CONSTRAINT [FK_ProductionOrderExtraInfo_ProductionOrder]
            FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders]([Id]) ON DELETE CASCADE
    );
    PRINT 'Tabelle ProductionOrderExtraInfo erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260722132805_AddProductionOrderExtraInfo')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260722132805_AddProductionOrderExtraInfo', '10.0.2');
END
GO

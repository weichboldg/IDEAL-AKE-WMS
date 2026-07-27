-- 18_ExtendProductionOrderLength.sql
-- Feature A: ProductionOrder-Feld von 100 auf 500 Zeichen erweitern
-- (für semikolon-separierte WA-Nummern bei Kommissionierwaagen)

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.StockMovements') AND name = N'ProductionOrder')
BEGIN
    ALTER TABLE [dbo].[StockMovements] ALTER COLUMN [ProductionOrder] NVARCHAR(500) NULL;
    PRINT 'StockMovements.ProductionOrder auf NVARCHAR(500) erweitert.';
END
GO

-- EF Migrations History aktualisieren (Scripts 14-18 = eine EF Migration)
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260218084152_AddUserFieldsAndPickingScale')
    BEGIN
        INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES (N'20260218084152_AddUserFieldsAndPickingScale', N'10.0.0-preview.1.25081.3');
        PRINT 'Migration AddUserFieldsAndPickingScale als angewendet markiert.';
    END
END
GO

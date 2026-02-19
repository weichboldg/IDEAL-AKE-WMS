-- 21_AddGlassAndPurchaseColumns.sql
-- Feature 3: Glas und Zukauf Spalten auf ProductionOrders

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = N'HasGlass')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [HasGlass] BIT NOT NULL CONSTRAINT [DF_ProductionOrders_HasGlass] DEFAULT (0);
    PRINT 'Spalte HasGlass zu ProductionOrders hinzugefügt.';
END
ELSE
BEGIN
    PRINT 'Spalte HasGlass existiert bereits.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = N'HasExternalPurchase')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [HasExternalPurchase] BIT NOT NULL CONSTRAINT [DF_ProductionOrders_HasExternalPurchase] DEFAULT (0);
    PRINT 'Spalte HasExternalPurchase zu ProductionOrders hinzugefügt.';
END
ELSE
BEGIN
    PRINT 'Spalte HasExternalPurchase existiert bereits.';
END
GO

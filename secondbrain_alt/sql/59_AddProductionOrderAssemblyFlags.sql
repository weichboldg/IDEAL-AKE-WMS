-- 59_AddProductionOrderAssemblyFlags.sql
-- Feature: VK/VL/VE/VT/VA Checkbox-Flags auf ProductionOrders (Kaelte, Luefter, Elektro, Tueren, Aufbau)

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = N'HasCooling')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [HasCooling] BIT NOT NULL CONSTRAINT [DF_ProductionOrders_HasCooling] DEFAULT (0);
    PRINT 'Spalte HasCooling zu ProductionOrders hinzugefügt.';
END
ELSE
BEGIN
    PRINT 'Spalte HasCooling existiert bereits.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = N'HasFan')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [HasFan] BIT NOT NULL CONSTRAINT [DF_ProductionOrders_HasFan] DEFAULT (0);
    PRINT 'Spalte HasFan zu ProductionOrders hinzugefügt.';
END
ELSE
BEGIN
    PRINT 'Spalte HasFan existiert bereits.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = N'HasElectric')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [HasElectric] BIT NOT NULL CONSTRAINT [DF_ProductionOrders_HasElectric] DEFAULT (0);
    PRINT 'Spalte HasElectric zu ProductionOrders hinzugefügt.';
END
ELSE
BEGIN
    PRINT 'Spalte HasElectric existiert bereits.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = N'HasDoors')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [HasDoors] BIT NOT NULL CONSTRAINT [DF_ProductionOrders_HasDoors] DEFAULT (0);
    PRINT 'Spalte HasDoors zu ProductionOrders hinzugefügt.';
END
ELSE
BEGIN
    PRINT 'Spalte HasDoors existiert bereits.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = N'HasSuperstructure')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [HasSuperstructure] BIT NOT NULL CONSTRAINT [DF_ProductionOrders_HasSuperstructure] DEFAULT (0);
    PRINT 'Spalte HasSuperstructure zu ProductionOrders hinzugefügt.';
END
ELSE
BEGIN
    PRINT 'Spalte HasSuperstructure existiert bereits.';
END
GO

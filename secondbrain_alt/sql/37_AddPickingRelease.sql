-- =============================================
-- 37: Kommissionier-Freigabe (Leitstand)
-- 4 neue Spalten auf ProductionOrders + Index + Rolle leitstand + AppSetting LeitstandAktiv
-- =============================================

USE [IDEAL_AKE_WMS]
GO

-- Spalte IsReleasedForPicking
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProductionOrders') AND name = 'IsReleasedForPicking')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [IsReleasedForPicking] BIT NOT NULL DEFAULT 0;
    PRINT 'Spalte IsReleasedForPicking hinzugefuegt.';
END
GO

-- Spalte PickingPriority
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProductionOrders') AND name = 'PickingPriority')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [PickingPriority] INT NULL;
    PRINT 'Spalte PickingPriority hinzugefuegt.';
END
GO

-- Spalte ReleasedAt
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProductionOrders') AND name = 'ReleasedAt')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [ReleasedAt] DATETIME2 NULL;
    PRINT 'Spalte ReleasedAt hinzugefuegt.';
END
GO

-- Spalte ReleasedBy
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProductionOrders') AND name = 'ReleasedBy')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [ReleasedBy] NVARCHAR(200) NULL;
    PRINT 'Spalte ReleasedBy hinzugefuegt.';
END
GO

-- Index fuer Freigabe-Abfragen
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductionOrders_IsReleasedForPicking_IsDone')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_IsReleasedForPicking_IsDone]
        ON [dbo].[ProductionOrders] ([IsReleasedForPicking], [IsDone])
        INCLUDE ([PickingPriority], [ProductionDate]);
    PRINT 'Index IX_ProductionOrders_IsReleasedForPicking_IsDone erstellt.';
END
GO

-- Rolle leitstand
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'leitstand')
BEGIN
    INSERT INTO [dbo].[Roles] ([Key], [Name], [Description], [AdGroup], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('leitstand', 'Leitstand', 'Produktionsauftraege freigeben und priorisieren', NULL, 1, 70, GETDATE(), 'system', 'system');
    PRINT 'Rolle leitstand eingefuegt.';
END
GO

-- AppSetting LeitstandAktiv
IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = 'LeitstandAktiv')
BEGIN
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES ('LeitstandAktiv', 'false', 'Leitstand-Modul: Kommissionier-Freigabe und Priorisierung aktivieren');
    PRINT 'AppSetting LeitstandAktiv eingefuegt.';
END
GO

PRINT '37_AddPickingRelease.sql abgeschlossen.';
GO

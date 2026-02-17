USE [AKE_BDE_Light]
GO

-- =============================================
-- 09_PickingItemIsBaugruppe.sql
-- Baugruppen-Flag für Picking-Items
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PickingItems')
    AND name = 'IsBaugruppe')
BEGIN
    ALTER TABLE [dbo].[PickingItems]
        ADD [IsBaugruppe] BIT NOT NULL DEFAULT 0;

    PRINT 'IsBaugruppe zu PickingItems hinzugefügt.';
END
GO

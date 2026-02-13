USE [AKE_BDE_Light]
GO

-- =============================================
-- 08_PickingStatus.sql
-- Kommissionierungs-Status für Werkstattaufträge
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProductionOrders')
    AND name = 'PickingStatus')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders]
        ADD [PickingStatus] NVARCHAR(50) NULL;

    PRINT 'PickingStatus zu ProductionOrders hinzugefügt.';
END
GO

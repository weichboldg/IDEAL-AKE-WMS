USE [IDEAL_AKE_WMS]
GO

-- =============================================
-- 10_WorkstationDefaultPrinter.sql
-- Default-Drucker für Arbeitsplätze
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Workstations')
    AND name = 'DefaultPrinter')
BEGIN
    ALTER TABLE [dbo].[Workstations]
        ADD [DefaultPrinter] NVARCHAR(500) NULL;

    PRINT 'DefaultPrinter zu Workstations hinzugefügt.';
END
GO

-- =============================================
-- 66_AddNoteEinkaufToWarehouseRequisitionItems
-- Adds NoteEinkauf NVARCHAR(500) NULL — separate Notiz fuer Einkauf
-- (semantische Trennung zu Note = "Notiz Lager").
-- Used by v1.19.0.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE Name = N'NoteEinkauf'
               AND Object_ID = Object_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    ALTER TABLE [dbo].[WarehouseRequisitionItems]
        ADD [NoteEinkauf] NVARCHAR(500) NULL;
END
GO

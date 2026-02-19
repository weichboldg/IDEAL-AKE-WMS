-- 20_RenamePickingScaleToTransport.sql
-- Feature 2: Spalte IsPickingScale -> IsPickingTransport umbenennen

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.StorageLocations') AND name = N'IsPickingScale')
BEGIN
    EXEC sp_rename N'dbo.StorageLocations.IsPickingScale', N'IsPickingTransport', N'COLUMN';
    PRINT 'Spalte IsPickingScale zu IsPickingTransport umbenannt.';
END
ELSE
BEGIN
    PRINT 'Spalte IsPickingScale existiert nicht (evtl. bereits umbenannt).';
END
GO

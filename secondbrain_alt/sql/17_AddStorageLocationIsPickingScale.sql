-- 17_AddStorageLocationIsPickingScale.sql
-- Feature A: Kommissionierwaage-Flag auf StorageLocations

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.StorageLocations') AND name = N'IsPickingScale')
BEGIN
    ALTER TABLE [dbo].[StorageLocations] ADD [IsPickingScale] BIT NOT NULL CONSTRAINT [DF_StorageLocations_IsPickingScale] DEFAULT (0);
    PRINT 'Spalte IsPickingScale zu StorageLocations hinzugefügt (Default: 0).';
END
ELSE
BEGIN
    PRINT 'Spalte IsPickingScale existiert bereits.';
END
GO

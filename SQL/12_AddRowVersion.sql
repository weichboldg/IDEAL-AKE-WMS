USE [AKE_BDE_Light]
GO

-- ============================================================
-- 12_AddRowVersion.sql
-- Fügt RowVersion-Spalte zur PickingItems-Tabelle hinzu
-- (Optimistic Concurrency Control)
--
-- WICHTIG: Zuerst 11_InitMigrationsHistory.sql ausführen!
-- ============================================================

-- RowVersion-Spalte hinzufügen (nur wenn noch nicht vorhanden)
IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID(N'PickingItems') AND name = N'RowVersion'
)
BEGIN
    ALTER TABLE [PickingItems] ADD [RowVersion] rowversion NOT NULL;
    PRINT 'RowVersion-Spalte zu PickingItems hinzugefügt.';
END
ELSE
BEGIN
    PRINT 'RowVersion-Spalte existiert bereits.';
END
GO

-- Migration als angewendet markieren
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260217083306_AddPickingItemRowVersion')
    BEGIN
        INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES (N'20260217083306_AddPickingItemRowVersion', N'10.0.2');
        PRINT 'Migration AddPickingItemRowVersion in History eingetragen.';
    END
    ELSE
    BEGIN
        PRINT 'Migration AddPickingItemRowVersion war bereits eingetragen.';
    END
END
GO

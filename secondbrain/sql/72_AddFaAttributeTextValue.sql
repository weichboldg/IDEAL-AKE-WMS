-- =============================================
-- 72_AddFaAttributeTextValue.sql (v1.22.0)
-- Fuegt der FaAttributeValues-Tabelle die Spalte TextValue (NVARCHAR(1000) NULL) hinzu,
-- damit FA-Merkmale vom Typ Text (AttributeType.Text = 2) einen Freitext-Wert speichern.
-- Idempotent, kann mehrfach ausgefuehrt werden (Reapply-fest).
--
-- Entspricht der EF-Migration 20260616071945_AddFaAttributeTextValue.
-- =============================================

-- =============================================
-- SECTION A: SPALTE ANLEGEN (idempotent, eigener Guard)
-- =============================================
IF COL_LENGTH('dbo.FaAttributeValues', 'TextValue') IS NULL
BEGIN
    ALTER TABLE [dbo].[FaAttributeValues]
        ADD [TextValue] NVARCHAR(1000) NULL;
    PRINT 'Spalte FaAttributeValues.TextValue erstellt.';
END
ELSE
    PRINT 'Spalte FaAttributeValues.TextValue bereits vorhanden - uebersprungen.';
GO

-- =============================================
-- SECTION B: EF MIGRATIONS HISTORY
-- =============================================
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260616071945_AddFaAttributeTextValue')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260616071945_AddFaAttributeTextValue', '10.0.2');
GO

PRINT '72_AddFaAttributeTextValue abgeschlossen.';
GO

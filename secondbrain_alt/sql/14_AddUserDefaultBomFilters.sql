-- 14_AddUserDefaultBomFilters.sql
-- Feature B: Default-BOM-Filter pro Benutzer
-- Fügt DefaultFilterBeschaffung und DefaultFilterArtikelgruppe zur Users-Tabelle hinzu

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'DefaultFilterBeschaffung')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [DefaultFilterBeschaffung] NVARCHAR(100) NULL;
    PRINT 'Spalte DefaultFilterBeschaffung zu Users hinzugefügt.';
END
ELSE
BEGIN
    PRINT 'Spalte DefaultFilterBeschaffung existiert bereits.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'DefaultFilterArtikelgruppe')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [DefaultFilterArtikelgruppe] NVARCHAR(100) NULL;
    PRINT 'Spalte DefaultFilterArtikelgruppe zu Users hinzugefügt.';
END
ELSE
BEGIN
    PRINT 'Spalte DefaultFilterArtikelgruppe existiert bereits.';
END
GO

-- 15_AddUserMasterDataAccess.sql
-- Feature C: Stammdaten-Berechtigungen
-- Fügt HasMasterDataAccess-Flag zur Users-Tabelle hinzu

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'HasMasterDataAccess')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [HasMasterDataAccess] BIT NOT NULL CONSTRAINT [DF_Users_HasMasterDataAccess] DEFAULT (0);
    PRINT 'Spalte HasMasterDataAccess zu Users hinzugefügt (Default: 0).';
END
ELSE
BEGIN
    PRINT 'Spalte HasMasterDataAccess existiert bereits.';
END
GO

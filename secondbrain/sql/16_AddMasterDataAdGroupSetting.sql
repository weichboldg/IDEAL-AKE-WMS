-- 16_AddMasterDataAdGroupSetting.sql
-- Feature C: AppSetting für AD-Gruppe Stammdaten-Zugriff

IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = N'StammdatenADGruppe')
BEGIN
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES (N'StammdatenADGruppe', N'BDE_Stammdaten', N'AD-Gruppe für Stammdaten-Zugriff');
    PRINT 'AppSetting StammdatenADGruppe eingefügt.';
END
ELSE
BEGIN
    PRINT 'AppSetting StammdatenADGruppe existiert bereits.';
END
GO

-- 19_AddBeschichtungAbholtage.sql
-- Feature 3: Beschichtung-Abholtage als AppSetting

IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = N'BeschichtungAbholtage')
BEGIN
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES (N'BeschichtungAbholtage', N'Dienstag,Donnerstag', N'Wochentage für Beschichtungs-Abholung (kommasepariert, deutsche Namen)');
    PRINT 'AppSetting BeschichtungAbholtage eingefügt.';
END
ELSE
BEGIN
    PRINT 'AppSetting BeschichtungAbholtage existiert bereits.';
END
GO

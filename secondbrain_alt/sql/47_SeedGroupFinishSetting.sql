-- Idempotent seed for BdeGleichzeitigerAbschlussBeiMehrfachStart AppSetting.

IF NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = 'BdeGleichzeitigerAbschlussBeiMehrfachStart')
BEGIN
    INSERT INTO dbo.AppSettings ([Key], [Value], [Description])
    VALUES (
        'BdeGleichzeitigerAbschlussBeiMehrfachStart',
        'false',
        'Alle parallel gestarteten Produktionsbuchungen eines Mitarbeiters muessen gemeinsam fertiggemeldet werden (nur wirksam wenn BdeMehrfachBuchungProOperator aktiv)'
    );
END
GO

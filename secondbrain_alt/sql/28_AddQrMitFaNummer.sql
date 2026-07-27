-- =============================================
-- Migration 28: AppSetting QrMitFaNummer
-- QR-Code-Erkennung mit Fertigungsauftragsnummer
-- Ziel: [IDEAL_AKE_WMS]
-- =============================================

IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = N'QrMitFaNummer')
BEGIN
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES (N'QrMitFaNummer', N'false', N'QR-Code enthält Fertigungsauftragsnummer an 3. Stelle');
    PRINT 'AppSetting QrMitFaNummer hinzugefuegt.';
END
GO

USE [IDEAL_AKE_WMS]
GO

-- ============================================================
-- 13_NegativeBuchungSettings.sql
-- AppSettings für konfigurierbare negative Lagerstandsbuchung
-- ============================================================

-- NegativeBuchungErlaubt: Darf bei fehlendem Bestand trotzdem gebucht werden?
IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = N'NegativeBuchungErlaubt')
BEGIN
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES (N'NegativeBuchungErlaubt', N'false', N'Negative Lagerstandsbuchungen erlauben (true/false)');
    PRINT 'Setting NegativeBuchungErlaubt eingefügt.';
END
ELSE
BEGIN
    PRINT 'Setting NegativeBuchungErlaubt existiert bereits.';
END

-- NegativeBuchungLagerplatz: Fallback-Lagerplatz bei negativer Buchung
IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = N'NegativeBuchungLagerplatz')
BEGIN
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES (N'NegativeBuchungLagerplatz', N'NAN', N'Lagerplatz-Code für Buchungen bei fehlendem Bestand');
    PRINT 'Setting NegativeBuchungLagerplatz eingefügt.';
END
ELSE
BEGIN
    PRINT 'Setting NegativeBuchungLagerplatz existiert bereits.';
END
GO

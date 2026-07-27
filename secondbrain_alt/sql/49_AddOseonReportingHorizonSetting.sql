-- Phase: OSEON Reporting v1.7.1
-- Idempotent: AppSetting OseonReportingHorizonDays seeden.

IF NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = 'OseonReportingHorizonDays')
BEGIN
    INSERT INTO dbo.AppSettings ([Key], [Value], [Description])
    VALUES ('OseonReportingHorizonDays', '10', 'Reporting: Tage in die Zukunft (Default-Horizont)');
END
GO

-- Phase: OSEON Reporting v1.8.3 — Overdue-Lookback konfigurierbar
-- Idempotent: AppSetting OseonReportingOverdueLookbackDays seeden.

IF NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = 'OseonReportingOverdueLookbackDays')
BEGIN
    INSERT INTO dbo.AppSettings ([Key], [Value], [Description])
    VALUES ('OseonReportingOverdueLookbackDays', '90', 'Reporting: Tage in die Vergangenheit fuer Ueberfaellig-Slice');
END
GO

-- SQL/62_AddUserDefaultPageSize.sql
-- Per-User-Default fuer die Seitengroesse in Listen.
-- NULL = System-Default (25). Erlaubte Werte: 25, 50, 100, 0 (= Alle, gecappt auf 5000).

IF COL_LENGTH('dbo.Users', 'DefaultPageSize') IS NULL
BEGIN
    ALTER TABLE dbo.Users
        ADD DefaultPageSize INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260521143627_AddUserDefaultPageSize')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    SELECT '20260521143627_AddUserDefaultPageSize', '10.0.2';
END
GO

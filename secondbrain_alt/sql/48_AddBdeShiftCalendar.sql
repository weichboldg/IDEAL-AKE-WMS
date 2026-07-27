-- Add BdeShifts table + Holiday.Source + ProductionWorkplaces.BdeUseCustomShiftPlan
-- Update CK_BdeBookings_StatusEnded to include AutoPaused (Status=5)
-- Idempotent.

IF COL_LENGTH('dbo.ProductionWorkplaces', 'BdeUseCustomShiftPlan') IS NULL
BEGIN
    ALTER TABLE dbo.ProductionWorkplaces
        ADD BdeUseCustomShiftPlan bit NOT NULL CONSTRAINT DF_ProductionWorkplaces_BdeUseCustomShiftPlan DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.Holidays', 'Source') IS NULL
BEGIN
    ALTER TABLE dbo.Holidays
        ADD Source tinyint NOT NULL CONSTRAINT DF_Holidays_Source DEFAULT (1);
END
GO

IF OBJECT_ID('dbo.BdeShifts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BdeShifts (
        Id int IDENTITY(1,1) PRIMARY KEY,
        DayOfWeek int NOT NULL,
        StartTime time NOT NULL,
        EndTime time NOT NULL,
        ProductionWorkplaceId int NULL,
        Name nvarchar(50) NULL,
        CreatedAt datetime2 NOT NULL,
        CreatedBy nvarchar(450) NOT NULL,
        CreatedByWindows nvarchar(450) NOT NULL,
        ModifiedAt datetime2 NULL,
        ModifiedBy nvarchar(450) NULL,
        ModifiedByWindows nvarchar(450) NULL,
        CONSTRAINT FK_BdeShifts_ProductionWorkplaces FOREIGN KEY (ProductionWorkplaceId)
            REFERENCES dbo.ProductionWorkplaces(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_BdeShifts_Workplace_Day
        ON dbo.BdeShifts (ProductionWorkplaceId, DayOfWeek);
END
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_BdeBookings_StatusEnded')
BEGIN
    ALTER TABLE dbo.BdeBookings DROP CONSTRAINT CK_BdeBookings_StatusEnded;
END
GO

ALTER TABLE dbo.BdeBookings ADD CONSTRAINT CK_BdeBookings_StatusEnded
    CHECK (([Status] = 1 AND [EndedAt] IS NULL) OR ([Status] IN (2,3,4,5) AND [EndedAt] IS NOT NULL));
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = 'BdeSchichtkalenderAktiv')
    INSERT INTO dbo.AppSettings ([Key], Value, Description)
    VALUES ('BdeSchichtkalenderAktiv', 'false', 'Schichtkalender + Auto-Pause am Schichtende aktiv');
GO

-- BDE Auto-Pause + Feiertags-Sync ServiceSettings
IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = 'Sync:BdeAutoPauseIntervalMinutes')
    INSERT INTO dbo.ServiceSettings ([Key], Value, Category, Description)
    VALUES ('Sync:BdeAutoPauseIntervalMinutes', '60', 'BDE', 'Intervall (Minuten) fuer Auto-Pause am Schichtende');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = 'Sync:FeiertagSyncEnabled')
    INSERT INTO dbo.ServiceSettings ([Key], Value, Category, Description)
    VALUES ('Sync:FeiertagSyncEnabled', 'false', 'BDE', 'Feiertags-Sync aus Nager.Date aktiv');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = 'Sync:FeiertagCountryCode')
    INSERT INTO dbo.ServiceSettings ([Key], Value, Category, Description)
    VALUES ('Sync:FeiertagCountryCode', 'AT', 'BDE', 'Laendercode fuer Feiertags-Sync (ISO-3166 alpha-2, z.B. AT, DE)');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = 'Sync:FeiertagRegion')
    INSERT INTO dbo.ServiceSettings ([Key], Value, Category, Description)
    VALUES ('Sync:FeiertagRegion', '', 'BDE', 'Optionale Region fuer Feiertags-Sync (z.B. AT-3 fuer Niederoesterreich)');

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = 'Sync:FeiertagJahreVoraus')
    INSERT INTO dbo.ServiceSettings ([Key], Value, Category, Description)
    VALUES ('Sync:FeiertagJahreVoraus', '2', 'BDE', 'Anzahl Folgejahre, die Feiertage vorausgesynct werden');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId LIKE '%_AddBdeShiftCalendar')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260428071503_AddBdeShiftCalendar', '10.0.2');
END
GO

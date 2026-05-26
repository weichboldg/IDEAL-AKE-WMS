-- =============================================
-- 34: OseonOperationConfigs Tabelle
-- Konfiguration pro Arbeitsgang: Soll-Termin-Offset + OSEON-Relevanz
-- =============================================

IF OBJECT_ID(N'[dbo].[OseonOperationConfigs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OseonOperationConfigs] (
        [Id]                 INT            IDENTITY(1,1) NOT NULL,
        [OperationName]      NVARCHAR(100)  NOT NULL,
        [DisplayName]        NVARCHAR(200)  NULL,
        [DueDateOffsetDays]  INT            NOT NULL DEFAULT 0,
        [IsOseonRelevant]    BIT            NOT NULL DEFAULT 1,
        CONSTRAINT [PK_OseonOperationConfigs] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_OseonOperationConfigs_OperationName] UNIQUE ([OperationName])
    );
    PRINT 'Tabelle [OseonOperationConfigs] erstellt.';
END
GO

-- Default-Daten einfuegen (nur wenn Tabelle leer)
IF NOT EXISTS (SELECT 1 FROM [dbo].[OseonOperationConfigs])
BEGIN
    INSERT INTO [dbo].[OseonOperationConfigs] ([OperationName], [DisplayName], [DueDateOffsetDays], [IsOseonRelevant])
    VALUES
        (N'B',       N'Belegen',         -1, 1),
        (N'ST',      N'Stanzen',          0, 1),
        (N'EG',      N'Entgraten',        0, 1),
        (N'BG',      N'Biegen',           2, 1),
        (N'BG-SaP1', N'Biegen SaP1',     2, 1),
        (N'RO',      N'Rollen',           2, 1),
        (N'MS',      N'Maschinenschub',   4, 1),
        (N'RS',      N'Restschweissen',   4, 1),
        (N'SL',      N'Schlosser',        5, 1),
        (N'RE',      N'Reinigen',         5, 1),
        (N'ZB',      N'Zusammenbau',      0, 0),
        (N'A-BT',    N'Anlegen BT',       0, 0);
    PRINT 'Default-Daten fuer OseonOperationConfigs eingefuegt.';
END
GO

-- EF Migrations-History
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] LIKE '%_AddOseonOperationConfig')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260330120000_AddOseonOperationConfig', N'10.0.0');
    PRINT 'Migration AddOseonOperationConfig in History eingetragen.';
END
GO

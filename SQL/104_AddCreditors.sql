-- =============================================================================
-- 104_AddCreditors.sql  (v1.73.0, Spec 2026-11-06-sage-sdata-stammdaten-sync, Etappe E8a; Anhang 2026-10-06-kreditoren-stammdaten)
-- Stammtabelle [Creditors] (Kreditoren: Lieferanten/Dienstleister/Beschichter), AuditableEntity, gepflegt in /Creditors.
--   Source 'Manual' (im WMS angelegt) | 'Sage' (kuenftig Lese-Abgleich Kontokorrent, E8c). Nie geloescht, nur IsActive = 0.
--   SageUebergabeStatus int (0 NichtNoetig, 1 Offen, 2 Uebergeben, 3 Fehler; F11, ohne Worker).
-- Indexe: IX_Creditors_SageKto UNIQUE gefiltert (IS NOT NULL), IX_Creditors_ShortName UNIQUE gefiltert (IS NOT NULL; Kurzname-Abgleich
--   Kreditoren-Frage 2). Die App prueft beides vorher (InMemory erzwingt keine UNIQUE-Indexe) - der Index ist die zweite Linie.
-- DEFAULTs (IsActive 1, IsCoater 0, Source Manual, SageUebergabeStatus 0) nur hier: EF setzt die Werte immer selbst.
-- Rein additiv (neue Tabelle), nicht daten-destruktiv; DB-Backup vor Deploy wie ueblich. SQL/AgentJobs nicht betroffen.
-- Nummer: 104 = naechste freie im Buendel (Reservierung 105 laut Spec rueckt nach, weil E5/102 entfiel); laeuft NACH
-- 103_AddSagePpsMitarbeiterAndStillstandsgruende.sql (G17, Deploy-Liste beachten).
-- Objektnamen wie die EF-Migration 20261106195956_AddCreditors.
-- Idempotent: zweiter Lauf aendert nichts. DDL je in eigenem Batch, History-Insert in separatem Batch.
-- =============================================================================
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: Tabelle dbo.__EFMigrationsHistory fehlt - zuerst SQL/00_FreshInstall.sql ausfuehren. Keine Aenderung, keine History-Zeile.';
    SET NOEXEC ON;
END
GO

IF OBJECT_ID(N'[dbo].[Creditors]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Creditors] (
        [Id]                        INT IDENTITY(1,1) NOT NULL,
        [Name]                      NVARCHAR(200)     NOT NULL,
        [ShortName]                 NVARCHAR(50)      NULL,
        [Matchcode]                 NVARCHAR(50)      NULL,
        [Street]                    NVARCHAR(200)     NULL,
        [PostalCode]                NVARCHAR(20)      NULL,
        [City]                      NVARCHAR(100)     NULL,
        [Country]                   NVARCHAR(100)     NULL,
        [Email]                     NVARCHAR(200)     NULL,
        [Phone]                     NVARCHAR(50)      NULL,
        [ContactPerson]             NVARCHAR(200)     NULL,
        [Remark]                    NVARCHAR(1000)    NULL,
        [IsActive]                  BIT               NOT NULL CONSTRAINT [DF_Creditors_IsActive] DEFAULT (1),
        [IsCoater]                  BIT               NOT NULL CONSTRAINT [DF_Creditors_IsCoater] DEFAULT (0),
        [Source]                    NVARCHAR(20)      NOT NULL CONSTRAINT [DF_Creditors_Source] DEFAULT (N'Manual'),
        [SageKto]                   NVARCHAR(20)      NULL,
        [SageMatchcode]             NVARCHAR(50)      NULL,
        [SageGruppe]                NVARCHAR(10)      NULL,
        [SageEUUStID]               NVARCHAR(15)      NULL,
        [SageZahlungskondition]     NVARCHAR(10)      NULL,
        [SageEKMindestbestellwert]  DECIMAL(18,2)     NULL,
        [SageMissingSince]          DATETIME2         NULL,
        [SageUebergabeStatus]       INT               NOT NULL CONSTRAINT [DF_Creditors_SageUebergabeStatus] DEFAULT (0),
        [SageUebergabeAm]           DATETIME2         NULL,
        [SageUebergabeFehler]       NVARCHAR(1000)    NULL,
        [CreatedAt]                 DATETIME2         NOT NULL,
        [CreatedBy]                 NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]          NVARCHAR(200)     NOT NULL,
        [ModifiedAt]                DATETIME2         NULL,
        [ModifiedBy]                NVARCHAR(200)     NULL,
        [ModifiedByWindows]         NVARCHAR(200)     NULL,
        CONSTRAINT [PK_Creditors] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT 'Tabelle Creditors erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Creditors_SageKto' AND object_id = OBJECT_ID('dbo.Creditors'))
BEGIN
    CREATE UNIQUE INDEX [IX_Creditors_SageKto] ON [dbo].[Creditors] ([SageKto])
        WHERE [SageKto] IS NOT NULL;
    PRINT 'Index IX_Creditors_SageKto angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Creditors_ShortName' AND object_id = OBJECT_ID('dbo.Creditors'))
BEGIN
    CREATE UNIQUE INDEX [IX_Creditors_ShortName] ON [dbo].[Creditors] ([ShortName])
        WHERE [ShortName] IS NOT NULL;
    PRINT 'Index IX_Creditors_ShortName angelegt.';
END
GO

-- Migrations-History (separater Batch, exakte MigrationId)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261106195956_AddCreditors')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261106195956_AddCreditors', '10.0.2');
    PRINT 'Migration 20261106195956_AddCreditors eingetragen.';
END
GO

SET NOEXEC OFF;
GO

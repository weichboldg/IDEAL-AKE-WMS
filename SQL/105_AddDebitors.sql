-- =============================================================================
-- 105_AddDebitors.sql  (v1.73.0, Spec 2026-11-06-sage-sdata-stammdaten-sync F10, Etappe E8b)
-- Stammtabelle [Debitors] (Debitoren = Kunden), AuditableEntity, gepflegt in /Debitors. Aufbau wie [Creditors] (SQL/104)
--   ohne IsCoater/SageEKMindestbestellwert, dafuer SageVertreter NVARCHAR(10) (Kontokorrent.Vertreter, P-S1) und
--   SageKundenNameFA NVARCHAR(20) (KHKAdressen_USER_KundenNameFA(20)).
--   Source 'Manual' (im WMS angelegt) | 'Sage' (kuenftig Lese-Abgleich Kontokorrent, E8c). Nie geloescht, nur IsActive = 0.
--   SageUebergabeStatus int (0 NichtNoetig, 1 Offen, 2 Uebergeben, 3 Fehler; F11, ohne Worker).
-- Indexe: IX_Debitors_SageKto / IX_Debitors_ShortName UNIQUE gefiltert (IS NOT NULL); die App prueft vorher (zweite Linie).
-- DEFAULTs (IsActive 1, Source Manual, SageUebergabeStatus 0) nur hier: EF setzt die Werte immer selbst.
-- Rein additiv (neue Tabelle), nicht daten-destruktiv; DB-Backup vor Deploy wie ueblich. SQL/AgentJobs nicht betroffen.
-- Nummer: 105 = naechste freie im Buendel (Spec reservierte 106; rueckt nach wie SQL/104). Laeuft NACH 104_AddCreditors.sql.
-- Objektnamen wie die EF-Migration 20261106200713_AddDebitors.
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

IF OBJECT_ID(N'[dbo].[Debitors]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Debitors] (
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
        [IsActive]                  BIT               NOT NULL CONSTRAINT [DF_Debitors_IsActive] DEFAULT (1),
        [Source]                    NVARCHAR(20)      NOT NULL CONSTRAINT [DF_Debitors_Source] DEFAULT (N'Manual'),
        [SageKto]                   NVARCHAR(20)      NULL,
        [SageMatchcode]             NVARCHAR(50)      NULL,
        [SageGruppe]                NVARCHAR(10)      NULL,
        [SageEUUStID]               NVARCHAR(15)      NULL,
        [SageZahlungskondition]     NVARCHAR(10)      NULL,
        [SageVertreter]             NVARCHAR(10)      NULL,
        [SageKundenNameFA]          NVARCHAR(20)      NULL,
        [SageMissingSince]          DATETIME2         NULL,
        [SageUebergabeStatus]       INT               NOT NULL CONSTRAINT [DF_Debitors_SageUebergabeStatus] DEFAULT (0),
        [SageUebergabeAm]           DATETIME2         NULL,
        [SageUebergabeFehler]       NVARCHAR(1000)    NULL,
        [CreatedAt]                 DATETIME2         NOT NULL,
        [CreatedBy]                 NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]          NVARCHAR(200)     NOT NULL,
        [ModifiedAt]                DATETIME2         NULL,
        [ModifiedBy]                NVARCHAR(200)     NULL,
        [ModifiedByWindows]         NVARCHAR(200)     NULL,
        CONSTRAINT [PK_Debitors] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT 'Tabelle Debitors erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Debitors_SageKto' AND object_id = OBJECT_ID('dbo.Debitors'))
BEGIN
    CREATE UNIQUE INDEX [IX_Debitors_SageKto] ON [dbo].[Debitors] ([SageKto])
        WHERE [SageKto] IS NOT NULL;
    PRINT 'Index IX_Debitors_SageKto angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Debitors_ShortName' AND object_id = OBJECT_ID('dbo.Debitors'))
BEGIN
    CREATE UNIQUE INDEX [IX_Debitors_ShortName] ON [dbo].[Debitors] ([ShortName])
        WHERE [ShortName] IS NOT NULL;
    PRINT 'Index IX_Debitors_ShortName angelegt.';
END
GO

-- Migrations-History (separater Batch, exakte MigrationId)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261106200713_AddDebitors')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261106200713_AddDebitors', '10.0.2');
    PRINT 'Migration 20261106200713_AddDebitors eingetragen.';
END
GO

SET NOEXEC OFF;
GO

-- =============================================================================
-- 160_AddSageFaArbeitsgangAnlagen.sql  (v1.99.38, Spec 2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen, E1a)
-- Warteschlange der Arbeitsgang-Anlagen in Sage-Fertigungsauftraegen (IDEAL): die Web-Seite "Arbeitsgaenge -> Sage" reiht ein,
-- der Dienst (Sage-Buchungs-Worker) sendet - nur bei ServiceSetting SageFaArbeitsgaengeAnlegenAktiv = true (Default aus).
--   dbo.SageFaArbeitsgangAnlagen  eine Zeile je FA (BelId) und Sage-Arbeitsgangnummer (UNIQUE), Status 1 Offen, 2 Gesendet,
--                                 3 Angelegt, 4 InSageVorhanden, 5 Fehler
-- Rein additiv (eine neue Tabelle), nicht daten-destruktiv, keine Seed-Daten. SQL/AgentJobs nicht betroffen.
-- Objektnamen wie die EF-Migration 20261117192807_AddSageFaArbeitsgangAnlagen. Idempotent (OBJECT_ID-Guard), DDL im eigenen Batch,
-- History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[dbo].[FaHierarchyNodes]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.FaHierarchyNodes fehlt - falsche Datenbank? Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF OBJECT_ID(N'dbo.SageFaArbeitsgangAnlagen', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SageFaArbeitsgangAnlagen] (
        [Id]                    INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_SageFaArbeitsgangAnlagen] PRIMARY KEY,
        [BelId]                 INT            NOT NULL,  -- Sage-FA (BelID), Haupt- oder Unter-FA
        [HauptFa]               INT            NOT NULL,
        [Kuerzel]               NVARCHAR(100)  NOT NULL,
        [SageArbeitsgangnummer] NVARCHAR(31)   NOT NULL,  -- Text, wie OperationConfigs
        [Reihenfolge]           INT            NOT NULL,
        [Status]                INT            NOT NULL,  -- 1 Offen, 2 Gesendet, 3 Angelegt, 4 InSageVorhanden, 5 Fehler
        [Versuche]              INT            NOT NULL,
        [Fehler]                NVARCHAR(1000) NULL,
        [SageAm]                DATETIME2      NULL,
        [CreatedAt]             DATETIME2      NOT NULL,
        [CreatedBy]             NVARCHAR(200)  NOT NULL,
        [CreatedByWindows]      NVARCHAR(200)  NOT NULL,
        [ModifiedAt]            DATETIME2      NULL,
        [ModifiedBy]            NVARCHAR(200)  NULL,
        [ModifiedByWindows]     NVARCHAR(200)  NULL
    );
    CREATE INDEX [IX_SageFaArbeitsgangAnlagen_Status] ON [dbo].[SageFaArbeitsgangAnlagen]([Status]);
    CREATE UNIQUE INDEX [UX_SageFaArbeitsgangAnlagen_BelId_Ag] ON [dbo].[SageFaArbeitsgangAnlagen]([BelId], [SageArbeitsgangnummer]);
    PRINT 'Tabelle SageFaArbeitsgangAnlagen angelegt.';
END
GO

IF OBJECT_ID(N'dbo.SageFaArbeitsgangAnlagen', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261117192807_AddSageFaArbeitsgangAnlagen')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261117192807_AddSageFaArbeitsgangAnlagen', '10.0.2');
    PRINT 'Migration 20261117192807_AddSageFaArbeitsgangAnlagen eingetragen.';
END
GO

SET NOEXEC OFF;
GO

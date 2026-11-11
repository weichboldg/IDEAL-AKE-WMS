-- =============================================================================
-- 142_AddDeployScriptRuns.sql  (v1.99.0, Spec 2026-11-11-update-skript-prueft-und-fuehrt-aus-mit-bestaetigung, ADR 0023)
-- Marker-Tabelle der Daten-/Einmal-Laeufe des Update-Skripts (scripts/deploy/Update-IdealAkeWms.ps1): eine Zeile je Manifest-Eintrag
-- (UNIQUE ManifestId). Das Skript schreibt sie in derselben Transaktion wie die Datenaenderung; die Anwendung liest sie nicht.
-- Nummer 142 = erste freie nach SQL/141 (Pruefung beim Start des Dev-Laufs 2026-11-11). Rein additiv, nicht daten-destruktiv.
-- SQL/AgentJobs nicht betroffen. Objektnamen wie die EF-Migration 20261111174202_AddDeployScriptRuns. Idempotent, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[dbo].[DeployScriptRuns]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DeployScriptRuns] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [ManifestId] nvarchar(200) NOT NULL,
        [FileName] nvarchar(400) NULL,
        [FileSha256] nvarchar(64) NULL,
        [Mode] nvarchar(30) NOT NULL,
        [Hits] int NULL,
        [Output] nvarchar(max) NULL,
        [PackageVersion] nvarchar(20) NOT NULL,
        [PackageCommit] nvarchar(64) NULL,
        [ExecutedAt] datetime2 NOT NULL,
        [ExecutedBy] nvarchar(200) NOT NULL,
        [Note] nvarchar(500) NULL,
        CONSTRAINT [PK_DeployScriptRuns] PRIMARY KEY ([Id])
    );
    PRINT N'Tabelle dbo.DeployScriptRuns angelegt.';
END
ELSE
    PRINT N'Tabelle dbo.DeployScriptRuns existiert bereits.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DeployScriptRuns_ManifestId' AND object_id = OBJECT_ID(N'dbo.DeployScriptRuns'))
    CREATE UNIQUE INDEX [UX_DeployScriptRuns_ManifestId] ON [dbo].[DeployScriptRuns] ([ManifestId]);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261111174202_AddDeployScriptRuns')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261111174202_AddDeployScriptRuns', '10.0.2');
    PRINT 'Migration 20261111174202_AddDeployScriptRuns eingetragen.';
END
GO

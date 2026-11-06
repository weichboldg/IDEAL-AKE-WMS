-- =============================================================================
-- 106_AddArticleSuppliers.sql  (v1.73.0, Spec 2026-11-06-sage-sdata-stammdaten-sync F10, Etappe E8c)
-- Tabelle [ArticleSuppliers] (Artikel-Lieferant aus Sage ArtikelLieferantInfo), AuditableEntity. Nur der Lese-Abgleich
--   ArtikelLieferantSync (Schalter Sync:ArtikelLieferantEnabled, Default aus) schreibt; keine Oberflaeche (Datenbasis Bestellvorschlaege G7).
--   FK ArticleId -> Articles CASCADE, FK CreditorId -> Creditors NO ACTION (Restrict; Kreditoren werden nie geloescht).
--   UNIQUE (ArticleId, CreditorId); der Abgleich prueft vorher (InMemory erzwingt UNIQUE nicht).
--   Laengen laut SData-$schema (P-S1): Artikelnummer/Bestellnummer 31, Bezeichnung1/2 50, Einkaufsmengeneinheit 5.
-- Rein additiv (neue Tabelle), nicht daten-destruktiv; DB-Backup vor Deploy wie ueblich. SQL/AgentJobs nicht betroffen.
-- Nummer: 106 = naechste freie im Buendel (Spec reservierte 107). Laeuft NACH 104_AddCreditors.sql (FK-Ziel).
-- Objektnamen wie die EF-Migration 20261106201439_AddArticleSuppliers.
-- Idempotent: zweiter Lauf aendert nichts. DDL je in eigenem Batch, History-Insert in separatem Batch.
-- =============================================================================
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL OR OBJECT_ID(N'[dbo].[Creditors]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.__EFMigrationsHistory oder dbo.Creditors fehlt - zuerst SQL/00_FreshInstall.sql bzw. SQL/104 ausfuehren. Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF OBJECT_ID(N'[dbo].[ArticleSuppliers]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleSuppliers] (
        [Id]                        INT IDENTITY(1,1) NOT NULL,
        [ArticleId]                 INT               NOT NULL,
        [CreditorId]                INT               NOT NULL,
        [Artikelnummer]             NVARCHAR(31)      NOT NULL,
        [SageBestellnummer]         NVARCHAR(31)      NULL,
        [Bezeichnung1]              NVARCHAR(50)      NULL,
        [Bezeichnung2]              NVARCHAR(50)      NULL,
        [Mindestbestellmenge]       DECIMAL(18,3)     NULL,
        [Einkaufsmengeneinheit]     NVARCHAR(5)       NULL,
        [UmrechnungsfaktorEK]       DECIMAL(18,6)     NULL,
        [Wiederbeschaffungszeit]    INT               NULL,
        [Einzelpreis]               DECIMAL(18,4)     NULL,
        [IstHauptlieferant]         BIT               NOT NULL,
        [CreatedAt]                 DATETIME2         NOT NULL,
        [CreatedBy]                 NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]          NVARCHAR(200)     NOT NULL,
        [ModifiedAt]                DATETIME2         NULL,
        [ModifiedBy]                NVARCHAR(200)     NULL,
        [ModifiedByWindows]         NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ArticleSuppliers] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ArticleSuppliers_Articles_ArticleId] FOREIGN KEY ([ArticleId]) REFERENCES [dbo].[Articles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ArticleSuppliers_Creditors_CreditorId] FOREIGN KEY ([CreditorId]) REFERENCES [dbo].[Creditors] ([Id])
    );
    PRINT 'Tabelle ArticleSuppliers erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ArticleSuppliers_ArticleId_CreditorId' AND object_id = OBJECT_ID('dbo.ArticleSuppliers'))
BEGIN
    CREATE UNIQUE INDEX [IX_ArticleSuppliers_ArticleId_CreditorId] ON [dbo].[ArticleSuppliers] ([ArticleId], [CreditorId]);
    PRINT 'Index IX_ArticleSuppliers_ArticleId_CreditorId angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ArticleSuppliers_CreditorId' AND object_id = OBJECT_ID('dbo.ArticleSuppliers'))
BEGIN
    CREATE INDEX [IX_ArticleSuppliers_CreditorId] ON [dbo].[ArticleSuppliers] ([CreditorId]);
    PRINT 'Index IX_ArticleSuppliers_CreditorId angelegt.';
END
GO

-- Migrations-History (separater Batch, exakte MigrationId)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261106201439_AddArticleSuppliers')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261106201439_AddArticleSuppliers', '10.0.2');
    PRINT 'Migration 20261106201439_AddArticleSuppliers eingetragen.';
END
GO

SET NOEXEC OFF;
GO

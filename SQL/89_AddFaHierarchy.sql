-- SQL/89_AddFaHierarchy.sql
-- Migration 89: AddFaHierarchy (IDEAL Teil 1 — Struktur-Fundament)
-- Rein additiv, idempotent: zwei neue Cache-Tabellen fuer den mehrstufigen
-- FA-Struktur-/Stuecklisten-Import aus den IDEAL-Sage-Views FAListe/FAInfos.
--   dbo.FaHierarchyNodes      : Projektion von FAListe (Struktur + Stueckliste, mehrstufig)
--   dbo.FaHierarchyOrderInfos : Projektion von FAInfos (PPS-/Auftragsdaten je HauptFA)
-- KEINE AuditableEntity (reine, vom FaHierarchySyncService per Full-Refresh befuellte
-- Cache-Tabellen, Praezedenzfall CachedBomHeaders). Nachvollziehbarkeit via SyncLog + SyncedAt.
--
-- Primaerweg (RCSI AN, Standard-Scope Teil 1): NUR die zwei Zieltabellen, KEIN GRANT ALTER,
-- KEINE Staging-Tabellen (Full-Refresh = DELETE FROM + Neubefuellung in einer Transaktion).
--
-- Tabellen-DDL in eigenem Batch (SQL Server parst Batches vorab -> Index-Referenz auf eine
-- im selben Batch neu erstellte Tabelle scheitert sonst). __EFMigrationsHistory-Insert in
-- separatem Batch.
SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.FaHierarchyNodes', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FaHierarchyNodes] (
        [Id]                  INT IDENTITY(1,1) NOT NULL,
        [HauptFA]             INT               NOT NULL,
        [VaterFA]             INT               NULL,
        [SubFA]               INT               NOT NULL,
        [Position]            INT               NULL,
        [HauptArtnr]          NVARCHAR(200)     NULL,
        [Artnr]               NVARCHAR(200)     NULL,
        [Matchcode]           NVARCHAR(200)     NULL,
        [Bezeichnung1]        NVARCHAR(MAX)     NULL,
        [Bezeichnung2]        NVARCHAR(MAX)     NULL,
        [Artikelgruppe]       NVARCHAR(200)     NULL,
        [Hauptlagerplatz]     NVARCHAR(200)     NULL,
        [BemerkungPN]         NVARCHAR(MAX)     NULL,
        [FertigungsInfoPN]    NVARCHAR(MAX)     NULL,
        [Kommissionieren]     NVARCHAR(200)     NULL,
        [Arbeitsbereich]      NVARCHAR(200)     NULL,
        [Arbeitsschritte]     NVARCHAR(MAX)     NULL,
        [Artikeltyp]          NVARCHAR(200)     NULL,
        [Material]            NVARCHAR(200)     NULL,
        [VMBedarf]            NVARCHAR(200)     NULL,
        [Sollmenge]           DECIMAL(18,4)     NOT NULL,
        [Fertigungmenge]      DECIMAL(18,4)     NOT NULL,
        [Breite]              DECIMAL(18,4)     NOT NULL,
        [Hoehe]               DECIMAL(18,4)     NOT NULL,
        [Tiefe]               DECIMAL(18,4)     NOT NULL,
        [Beschaffungsartikel] BIT               NOT NULL,
        [Beschichtet]         BIT               NOT NULL,
        [EKBedarf]            BIT               NOT NULL,
        [SyncedAt]            DATETIME2         NOT NULL,
        CONSTRAINT [PK_FaHierarchyNodes] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT 'Tabelle FaHierarchyNodes erstellt.';
END
GO

IF OBJECT_ID('dbo.FaHierarchyOrderInfos', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FaHierarchyOrderInfos] (
        [Id]                 INT IDENTITY(1,1) NOT NULL,
        [HauptFA]            INT               NOT NULL,
        [MontageAbteilung]   NVARCHAR(200)     NULL,
        [ABNr]               NVARCHAR(200)     NULL,
        [Pos]                NVARCHAR(200)     NULL,
        [Kunde]              NVARCHAR(MAX)     NULL,
        [Status]             NVARCHAR(200)     NULL,
        [Dienstleister]      NVARCHAR(200)     NULL,
        [RAL]                NVARCHAR(200)     NULL,
        [Bemerkung_Uhrzeit]  NVARCHAR(MAX)     NULL,
        [KO_Termin]          DATETIME2         NULL,
        [FE_Termin]          DATETIME2         NULL,
        [Start_Beschichtung] DATETIME2         NULL,
        [Beschichten_Retour] DATETIME2         NULL,
        [Neuer_PT_PPS]       DATETIME2         NULL,
        [Verladetermin_Vsl]  DATETIME2         NULL,
        [Montagestunden]     DECIMAL(18,2)     NULL,
        [Prio]               INT               NULL,
        [SyncedAt]           DATETIME2         NOT NULL,
        CONSTRAINT [PK_FaHierarchyOrderInfos] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT 'Tabelle FaHierarchyOrderInfos erstellt.';
END
GO

IF OBJECT_ID('dbo.FaHierarchyNodes', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FaHierarchyNodes_HauptFA')
    CREATE NONCLUSTERED INDEX [IX_FaHierarchyNodes_HauptFA]
        ON [dbo].[FaHierarchyNodes]([HauptFA]);
GO

IF OBJECT_ID('dbo.FaHierarchyNodes', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FaHierarchyNodes_VaterFA')
    CREATE NONCLUSTERED INDEX [IX_FaHierarchyNodes_VaterFA]
        ON [dbo].[FaHierarchyNodes]([VaterFA]);
GO

IF OBJECT_ID('dbo.FaHierarchyOrderInfos', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FaHierarchyOrderInfos_HauptFA')
    CREATE NONCLUSTERED INDEX [IX_FaHierarchyOrderInfos_HauptFA]
        ON [dbo].[FaHierarchyOrderInfos]([HauptFA]);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260807105825_AddFaHierarchy')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260807105825_AddFaHierarchy', '10.0.2');
END
GO

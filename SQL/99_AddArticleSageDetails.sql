-- =============================================================================
-- 99_AddArticleSageDetails.sql  (v1.73.0, Spec 2026-11-06-sage-sdata-stammdaten-sync, Etappe E3a)
-- Satellit [ArticleSageDetails] (1:1 zu [Articles], UNIQUE ArticleId, FK ON DELETE CASCADE, Audit-Felder) fuer die Sage-Zusatzfelder
-- aus SData (Artikel + ArtikelVariantenExtended). Laengen laut SData-$schema (P-S1); DECIMAL(18,3) bzw. MittlererEK (18,4), Genauigkeit
-- P-S1 offen. Rein additiv, keine Bestandsbefuellung: befuellt nur der Artikel-Abgleich mit Sync:Quelle:Artikel = SData (bis P-R1
-- gesperrt). View-Laeufe und der deprecated AgentJob 02_Import_Artikel fassen die Tabelle nie an. Nicht daten-destruktiv; DB-Backup
-- vor Deploy wie ueblich. Objektnamen wie die EF-Migration 20261106184123_AddArticleSageDetails.
-- Idempotent: zweiter Lauf aendert nichts. DDL je in eigenem Batch, History-Insert in separatem Batch.
-- =============================================================================
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'[dbo].[Articles]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: Tabelle dbo.Articles fehlt - zuerst SQL/00_FreshInstall.sql ausfuehren. Keine Aenderung, keine History-Zeile.';
    SET NOEXEC ON;
END
GO

IF OBJECT_ID(N'[dbo].[ArticleSageDetails]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleSageDetails] (
        [Id]                      INT IDENTITY(1,1) NOT NULL,
        [ArticleId]               INT               NOT NULL,
        [Bezeichnung2]            NVARCHAR(50)      NULL,
        [Hauptartikelgruppe]      NVARCHAR(15)      NULL,
        [Vaterartikelgruppe]      NVARCHAR(15)      NULL,
        [Hersteller]              NVARCHAR(20)      NULL,
        [HArtikelnummer]          NVARCHAR(20)      NULL,
        [Zeichnungsnummer]        NVARCHAR(50)      NULL,
        [Ersatzartikelnummer]     NVARCHAR(31)      NULL,
        [Langtext]                NVARCHAR(MAX)     NULL,
        [Gewichtseinheit]         NVARCHAR(10)      NULL,
        [DezimalstellenLager]     INT               NULL,
        [Lagerfuehrung]           BIT               NULL,
        [SagePlatzId]             INT               NULL,
        [Entnahmeverfahren]       NVARCHAR(6)       NULL,
        [Inventurmethode]         INT               NULL,
        [Chargenpflicht]          INT               NULL,
        [Nachweispflicht]         INT               NULL,
        [IstBestellartikel]       BIT               NULL,
        [IstFertigungsartikel]    BIT               NULL,
        [IstUnterbaugruppe]       BIT               NULL,
        [IstErsatzteil]           BIT               NULL,
        [IstVerschleissteil]      BIT               NULL,
        [Aktiv]                   BIT               NULL,
        [Stuecklistentyp]         INT               NULL,
        [Schwundart]              NVARCHAR(1)       NULL,
        [Schwund]                 DECIMAL(18,3)     NULL,
        [MinimaleLosgroesse]      DECIMAL(18,3)     NULL,
        [MaximaleLosgroesse]      DECIMAL(18,3)     NULL,
        [Laenge]                  DECIMAL(18,3)     NULL,
        [Breite]                  DECIMAL(18,3)     NULL,
        [Hoehe]                   DECIMAL(18,3)     NULL,
        [EANNummer]               NVARCHAR(15)      NULL,
        [ABCKlasse]               NVARCHAR(1)       NULL,
        [Gewicht]                 DECIMAL(18,3)     NULL,
        [Lagerbestand]            DECIMAL(18,3)     NULL,
        [LagerbestandVerfuegbar]  DECIMAL(18,3)     NULL,
        [LagerbestandOhneDispo]   DECIMAL(18,3)     NULL,
        [Bestellmenge]            DECIMAL(18,3)     NULL,
        [MittlererEK]             DECIMAL(18,4)     NULL,
        [Bestellzeitpunkt]        INT               NULL,
        [Hauptlieferant]          NVARCHAR(20)      NULL,
        [LetzterLieferant]        NVARCHAR(20)      NULL,
        [Wiederbeschaffungszeit]  INT               NULL,
        [UserLagerplatzId]        INT               NULL,
        [UserMindestbestand]      DECIMAL(18,3)     NULL,
        [CreatedAt]               DATETIME2         NOT NULL,
        [CreatedBy]               NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]        NVARCHAR(200)     NOT NULL,
        [ModifiedAt]              DATETIME2         NULL,
        [ModifiedBy]              NVARCHAR(200)     NULL,
        [ModifiedByWindows]       NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ArticleSageDetails] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ArticleSageDetails_Articles_ArticleId] FOREIGN KEY ([ArticleId]) REFERENCES [dbo].[Articles]([Id]) ON DELETE CASCADE
    );
    PRINT 'Tabelle ArticleSageDetails erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_ArticleSageDetails_ArticleId' AND object_id = OBJECT_ID('dbo.ArticleSageDetails'))
BEGIN
    CREATE UNIQUE INDEX [UQ_ArticleSageDetails_ArticleId] ON [dbo].[ArticleSageDetails] ([ArticleId]);
    PRINT 'Index UQ_ArticleSageDetails_ArticleId angelegt.';
END
GO

-- Migrations-History (separater Batch, exakte MigrationId)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261106184123_AddArticleSageDetails')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261106184123_AddArticleSageDetails', '10.0.2');
    PRINT 'Migration 20261106184123_AddArticleSageDetails eingetragen.';
END
GO

SET NOEXEC OFF;
GO

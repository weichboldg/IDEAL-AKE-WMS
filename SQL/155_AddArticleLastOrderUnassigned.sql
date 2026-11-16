-- =============================================================================
-- 155_AddArticleLastOrderUnassigned.sql  (v1.99.30, Spec 2026-11-16-bestellvorschlaege-nachbesserung E2, F3 / Antwort 4)
-- Bestellvorschlaege: juengste Sage-Bestellposition (EBB/EBZ, 3 Jahre) bei Lieferanten OHNE Artikel-Lieferant-Zeile im WMS.
--   Tabelle dbo.ArticleLastOrdersUnassigned (ArticleId FK Articles CASCADE, Kto, Menge (Basis), Am, Beleg, Audit-Felder),
--   UNIQUE (ArticleId, Kto).
-- Geschrieben nur vom Dienst-Lauf "Letzte Bestellung (Einkauf)" (Sync:ArtikelLieferantEnabled, Uhrzeiten
-- Sync:LetzteBestellungUhrzeitNacht/-Mittag). Rein additiv, nicht daten-destruktiv: die Tabelle fuellt der naechste Lauf.
-- SQL/151, 153, 154 sind fuer andere Laeufe reserviert. SQL/AgentJobs nicht betroffen.
-- Objektnamen wie die EF-Migration 20261116190709_AddArticleLastOrderUnassigned. Idempotent (OBJECT_ID-Guard),
-- DDL im eigenen Batch, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[dbo].[Articles]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.Articles fehlt - falsche Datenbank? Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF OBJECT_ID(N'[dbo].[ArticleLastOrdersUnassigned]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleLastOrdersUnassigned] (
        [Id]                INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ArticleLastOrdersUnassigned] PRIMARY KEY,
        [ArticleId]         INT               NOT NULL CONSTRAINT [FK_ArticleLastOrdersUnassigned_Articles_ArticleId] REFERENCES [dbo].[Articles]([Id]) ON DELETE CASCADE,
        [Kto]               NVARCHAR(20)      NOT NULL,
        [Menge]             DECIMAL(18,3)     NOT NULL,
        [Am]                DATE              NOT NULL,
        [Beleg]             NVARCHAR(45)      NULL,
        [CreatedAt]         DATETIME2         NOT NULL,
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL
    );
    CREATE UNIQUE INDEX [IX_ArticleLastOrdersUnassigned_ArticleId_Kto] ON [dbo].[ArticleLastOrdersUnassigned]([ArticleId], [Kto]);
    PRINT N'Tabelle ArticleLastOrdersUnassigned angelegt.';
END
ELSE
    PRINT N'Tabelle ArticleLastOrdersUnassigned existiert bereits.';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261116190709_AddArticleLastOrderUnassigned')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261116190709_AddArticleLastOrderUnassigned', '10.0.2');
    PRINT 'Migration 20261116190709_AddArticleLastOrderUnassigned eingetragen.';
END
GO

SET NOEXEC OFF;
GO

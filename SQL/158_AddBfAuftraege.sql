-- =============================================================================
-- 158_AddBfAuftraege.sql  (v1.99.35, Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv)
-- Blechfertigung (IDEAL): BF-Auftraege (Warenkorb -> Schachtler-Freigabe -> WiCAM-CSV durch den Dienst -> Fertig).
--   dbo.BfAuftraege          Kopf, Nummer BF + 8 Ziffern (gefilterter UNIQUE, Entwurf ohne Nummer), RowVersion, CSV-Status
--   dbo.BfAuftragPositionen  Positionen (Artikel-Snapshot), UNIQUE (BfAuftragId, Position), FK CASCADE zum Kopf
-- Rein additiv (zwei neue Tabellen), nicht daten-destruktiv, keine Seed-Daten. SQL/AgentJobs nicht betroffen.
-- Objektnamen wie die EF-Migration 20261117154645_AddBfAuftraege. Idempotent (OBJECT_ID-Guard), DDL je Tabelle im eigenen Batch,
-- History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[dbo].[OrderRecipientGroups]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.OrderRecipientGroups fehlt - falsche Datenbank? Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF OBJECT_ID(N'dbo.BfAuftraege', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BfAuftraege] (
        [Id]                    INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_BfAuftraege] PRIMARY KEY,
        [Nummer]                NVARCHAR(10)   NULL,      -- BF + 8 Ziffern, beim Abschicken; Entwurf ohne Nummer
        [Status]                INT            NOT NULL,  -- 0 Entwurf, 1 Bestellt, 2 Freigegeben, 3 Fertig, 4 Storniert
        [BestellerUserId]       INT            NULL,
        [BestellerName]         NVARCHAR(200)  NOT NULL,
        [BestelltAm]            DATETIME2      NULL,
        [OrderRecipientGroupId] INT            NULL CONSTRAINT [FK_BfAuftraege_OrderRecipientGroups_OrderRecipientGroupId]
                                               REFERENCES [dbo].[OrderRecipientGroups]([Id]),
        [EmailSentAt]           DATETIME2      NULL,
        [FreigegebenAm]         DATETIME2      NULL,
        [FreigegebenVon]        NVARCHAR(200)  NULL,
        [Schachteltermin]       DATE           NULL,
        [FeTermin]              DATE           NULL,
        [CsvStatus]             INT            NOT NULL,  -- 0 Keine, 1 Offen, 2 Geschrieben, 3 Fehler
        [CsvVersuche]           INT            NOT NULL,
        [CsvFehler]             NVARCHAR(1000) NULL,
        [CsvDatei]              NVARCHAR(400)  NULL,
        [CsvGeschriebenAm]      DATETIME2      NULL,
        [FertigAm]              DATETIME2      NULL,
        [FertigVon]             NVARCHAR(200)  NULL,
        [StorniertAm]           DATETIME2      NULL,
        [StorniertVon]          NVARCHAR(200)  NULL,
        [StornoGrund]           NVARCHAR(500)  NULL,
        [RowVersion]            ROWVERSION     NOT NULL,
        [CreatedAt]             DATETIME2      NOT NULL,
        [CreatedBy]             NVARCHAR(200)  NOT NULL,
        [CreatedByWindows]      NVARCHAR(200)  NOT NULL,
        [ModifiedAt]            DATETIME2      NULL,
        [ModifiedBy]            NVARCHAR(200)  NULL,
        [ModifiedByWindows]     NVARCHAR(200)  NULL
    );
    CREATE UNIQUE INDEX [UX_BfAuftraege_Nummer] ON [dbo].[BfAuftraege]([Nummer]) WHERE [Nummer] IS NOT NULL;
    CREATE INDEX [IX_BfAuftraege_Status] ON [dbo].[BfAuftraege]([Status]);
    CREATE INDEX [IX_BfAuftraege_CsvStatus] ON [dbo].[BfAuftraege]([CsvStatus]);
    CREATE INDEX [IX_BfAuftraege_OrderRecipientGroupId] ON [dbo].[BfAuftraege]([OrderRecipientGroupId]);
    PRINT 'Tabelle BfAuftraege angelegt.';
END
GO

IF OBJECT_ID(N'dbo.BfAuftragPositionen', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BfAuftragPositionen] (
        [Id]                INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_BfAuftragPositionen] PRIMARY KEY,
        [BfAuftragId]       INT            NOT NULL CONSTRAINT [FK_BfAuftragPositionen_BfAuftraege_BfAuftragId]
                                           REFERENCES [dbo].[BfAuftraege]([Id]) ON DELETE CASCADE,
        [Position]          INT            NOT NULL,
        [ArticleNumber]     NVARCHAR(100)  NOT NULL,
        [Matchcode]         NVARCHAR(200)  NULL,
        [Teileart]          NVARCHAR(50)   NULL,
        [Material]          NVARCHAR(200)  NULL,
        [Materialnummer]    NVARCHAR(50)   NULL,
        [FertigungsInfoPN]  NVARCHAR(200)  NULL,
        [Hauptlagerplatz]   NVARCHAR(100)  NULL,
        [Menge]             DECIMAL(18,3)  NOT NULL,
        [SageBestellmenge]  DECIMAL(18,3)  NULL,
        [Status]            NVARCHAR(50)   NULL,
        [Bemerkung]         NVARCHAR(400)  NULL,
        [Gestrichen]        BIT            NOT NULL,
        [SageFa]            NVARCHAR(50)   NULL,      -- Stufe 2 (Sage-FA je Position), in Stufe 1 leer
        [CreatedAt]         DATETIME2      NOT NULL,
        [CreatedBy]         NVARCHAR(200)  NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)  NOT NULL,
        [ModifiedAt]        DATETIME2      NULL,
        [ModifiedBy]        NVARCHAR(200)  NULL,
        [ModifiedByWindows] NVARCHAR(200)  NULL
    );
    CREATE UNIQUE INDEX [UX_BfAuftragPositionen_Auftrag_Position] ON [dbo].[BfAuftragPositionen]([BfAuftragId], [Position]);
    PRINT 'Tabelle BfAuftragPositionen angelegt.';
END
GO

IF OBJECT_ID(N'dbo.BfAuftraege', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.BfAuftragPositionen', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261117154645_AddBfAuftraege')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261117154645_AddBfAuftraege', '10.0.2');
    PRINT 'Migration 20261117154645_AddBfAuftraege eingetragen.';
END
GO

SET NOEXEC OFF;
GO

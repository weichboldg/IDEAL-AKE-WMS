-- =============================================================================
-- 146_AddWareneingangBarcodeCsv.sql  (v1.99.11, Spec 2026-11-12-wareneingang-barcode-lieferschein-csv-export)
-- Wareneingang zu Bestellung: Barcode des Lieferschein-Aufklebers und CSV-Uebergabe auf einen UNC-Pfad.
--   PurchaseOrderReceipts.Barcode        NVARCHAR(50) NULL  + Index IX_PurchaseOrderReceipts_Barcode (Duplikat-Pruefung)
--   PurchaseOrderReceipts.LieferantName  NVARCHAR(50) NULL  (Kreditorname als Snapshot beim Buchen)
--   dbo.WareneingangCsvExporte (neu): eine Zeile je Sage-WE-Vorgang (UNIQUE), Status 1 Offen / 2 Geschrieben / 3 Fehler, Audit-Felder
-- Rein additiv, nicht daten-destruktiv: Altbestand bleibt NULL (keine CSV). SQL/AgentJobs nicht betroffen.
-- Objektnamen wie die EF-Migration 20261112151318_AddWareneingangBarcodeCsv. Idempotent (OBJECT_ID-/COL_LENGTH-/sys.indexes-Guard),
-- DDL je eigener Batch, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[dbo].[PurchaseOrderReceipts]', N'U') IS NULL OR COL_LENGTH(N'dbo.PurchaseOrderReceipts', N'SageWeVorgang') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.PurchaseOrderReceipts (mit SQL/143) fehlt - zuerst SQL/143 ausfuehren. Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.PurchaseOrderReceipts', N'Barcode') IS NULL
BEGIN
    ALTER TABLE [dbo].[PurchaseOrderReceipts] ADD [Barcode] NVARCHAR(50) NULL;
    PRINT N'Spalte PurchaseOrderReceipts.Barcode angelegt.';
END
GO

IF COL_LENGTH(N'dbo.PurchaseOrderReceipts', N'LieferantName') IS NULL
BEGIN
    ALTER TABLE [dbo].[PurchaseOrderReceipts] ADD [LieferantName] NVARCHAR(50) NULL;
    PRINT N'Spalte PurchaseOrderReceipts.LieferantName angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseOrderReceipts_Barcode' AND object_id = OBJECT_ID(N'dbo.PurchaseOrderReceipts'))
BEGIN
    CREATE INDEX [IX_PurchaseOrderReceipts_Barcode] ON [dbo].[PurchaseOrderReceipts]([Barcode]);
    PRINT N'Index IX_PurchaseOrderReceipts_Barcode angelegt.';
END
GO

IF OBJECT_ID(N'[dbo].[WareneingangCsvExporte]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WareneingangCsvExporte] (
        [Id]                 INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_WareneingangCsvExporte] PRIMARY KEY,
        [SageWeVorgang]      UNIQUEIDENTIFIER  NOT NULL,
        [Barcode]            NVARCHAR(50)      NOT NULL,
        [Status]             INT               NOT NULL,  -- 1 Offen, 2 Geschrieben, 3 Fehler
        [Versuche]           INT               NOT NULL CONSTRAINT [DF_WareneingangCsvExporte_Versuche] DEFAULT 0,
        [Fehler]             NVARCHAR(1000)    NULL,
        [Datei]              NVARCHAR(400)     NULL,
        [GeschriebenAm]      DATETIME2         NULL,
        [CreatedAt]          DATETIME2         NOT NULL,
        [CreatedBy]          NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]   NVARCHAR(200)     NOT NULL,
        [ModifiedAt]         DATETIME2         NULL,
        [ModifiedBy]         NVARCHAR(200)     NULL,
        [ModifiedByWindows]  NVARCHAR(200)     NULL
    );
    PRINT N'Tabelle WareneingangCsvExporte angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_WareneingangCsvExporte_SageWeVorgang' AND object_id = OBJECT_ID(N'dbo.WareneingangCsvExporte'))
BEGIN
    CREATE UNIQUE INDEX [UX_WareneingangCsvExporte_SageWeVorgang] ON [dbo].[WareneingangCsvExporte]([SageWeVorgang]);
    PRINT N'Index UX_WareneingangCsvExporte_SageWeVorgang angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WareneingangCsvExporte_Status' AND object_id = OBJECT_ID(N'dbo.WareneingangCsvExporte'))
BEGIN
    CREATE INDEX [IX_WareneingangCsvExporte_Status] ON [dbo].[WareneingangCsvExporte]([Status]);
    PRINT N'Index IX_WareneingangCsvExporte_Status angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261112151318_AddWareneingangBarcodeCsv')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261112151318_AddWareneingangBarcodeCsv', '10.0.2');
    PRINT 'Migration 20261112151318_AddWareneingangBarcodeCsv eingetragen.';
END
GO

SET NOEXEC OFF;
GO

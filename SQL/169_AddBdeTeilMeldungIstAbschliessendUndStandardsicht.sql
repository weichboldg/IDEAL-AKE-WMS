-- =============================================================================
-- 169_AddBdeTeilMeldungIstAbschliessendUndStandardsicht.sql
--   (v1.99.75, Spec 2026-11-25-bde-masken-vereinheitlichung Etappe 3, ADR 0037)
--   * dbo.BdeTeilMeldungen + IstAbschliessend BIT NOT NULL DEFAULT 1. Bestand = abschliessend (der DEFAULT fuellt beim
--     ALTER alle vorhandenen Zeilen mit 1 - VOR dem Index-Neubau). Teilfertig-Meldungen schreibt die Anwendung mit 0.
--   * UX_BdeTeilMeldungen_Schluessel und UX_BdeTeilMeldungen_Lagerbestellung neu mit zusaetzlich [IstAbschliessend] = 1:
--     eindeutig je Schluessel ist nur noch die ABSCHLIESSENDE Meldung, Teilmeldungen beliebig oft.
--     Neubau nur, wenn filter_definition IstAbschliessend noch nicht enthaelt; WarehouseRequisitionItemId IS [NOT] NULL bleibt.
--     Der Filter wird nur ENGER - Dubletten sind daher nicht zu erwarten. Trotzdem Pruefung vorab: Treffer -> WARNUNG, Abbruch
--     ohne Index-Neubau und ohne History-Eintrag (melden statt still scheitern).
--   * dbo.ProductionWorkplaces + BdeStandardsichtTeile BIT NOT NULL DEFAULT 0 (Startreiter der BDE-Seite, Leser ab Etappe 4a).
--     Vorbelegung NUR bei Erstanlage: 1 fuer aktive BDE-Teilelisten-Werkbaenke mit Arbeitsschritt-Kuerzel.
-- Additiv bis auf den Index-Neuaufbau (kleine Tabelle), nicht daten-destruktiv. Voraussetzung SQL/165. SQL/AgentJobs nicht betroffen.
-- Objektnamen wie die EF-Migration 20261125191311_AddBdeTeilMeldungIstAbschliessendUndStandardsicht.
-- Idempotent (COL_LENGTH-/OBJECT_ID-/sys.indexes-Guards), DDL je eigener Batch, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.ProductionWorkplaces', N'U') IS NULL OR OBJECT_ID(N'dbo.BdeTeilMeldungen', N'U') IS NULL
   OR COL_LENGTH(N'dbo.BdeTeilMeldungen', N'WarehouseRequisitionItemId') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.ProductionWorkplaces / dbo.BdeTeilMeldungen fehlt oder SQL/165 steht noch aus (WarehouseRequisitionItemId). Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.BdeTeilMeldungen', N'IstAbschliessend') IS NULL
BEGIN
    ALTER TABLE [dbo].[BdeTeilMeldungen] ADD [IstAbschliessend] BIT NOT NULL
        CONSTRAINT [DF_BdeTeilMeldungen_IstAbschliessend] DEFAULT 1;
    PRINT N'Spalte BdeTeilMeldungen.IstAbschliessend angelegt (Bestand = 1, abschliessend).';
END
GO

IF COL_LENGTH(N'dbo.ProductionWorkplaces', N'BdeStandardsichtTeile') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductionWorkplaces] ADD [BdeStandardsichtTeile] BIT NOT NULL
        CONSTRAINT [DF_ProductionWorkplaces_BdeStandardsichtTeile] DEFAULT 0;
    -- Vorbelegung nur bei Erstanlage (EXEC: die Spalte existiert erst nach dem ALTER dieses Batches)
    EXEC (N'UPDATE dbo.ProductionWorkplaces SET BdeStandardsichtTeile = 1 WHERE BdeAktiv = 1 AND BdeTeileliste = 1 AND ISNULL(LTRIM(RTRIM(ArbeitsschrittCode)), N'''') <> N''''');
    PRINT N'Spalte ProductionWorkplaces.BdeStandardsichtTeile angelegt und vorbelegt.';
END
GO

-- Dubletten-Pruefung vor dem Neubau: mehr als eine abschliessende, nicht stornierte Meldung je Schluessel bzw. Lagerposition+Kuerzel?
IF EXISTS (SELECT 1 FROM dbo.BdeTeilMeldungen WHERE IsCancelled = 0 AND IstAbschliessend = 1 AND WarehouseRequisitionItemId IS NULL
           GROUP BY HauptFA, VaterFA, SubFA, Position, Artnr, Kuerzel HAVING COUNT(*) > 1)
   OR EXISTS (SELECT 1 FROM dbo.BdeTeilMeldungen WHERE IsCancelled = 0 AND IstAbschliessend = 1 AND WarehouseRequisitionItemId IS NOT NULL
           GROUP BY WarehouseRequisitionItemId, Kuerzel HAVING COUNT(*) > 1)
BEGIN
    PRINT N'WARNUNG: doppelte abschliessende Teilmeldungen gefunden - Indizes NICHT neu gebaut, Migration NICHT eingetragen.';
    PRINT N'Pruefabfrage: SELECT HauptFA, VaterFA, SubFA, Position, Artnr, WarehouseRequisitionItemId, Kuerzel, COUNT(*) FROM dbo.BdeTeilMeldungen'
        + N' WHERE IsCancelled = 0 AND IstAbschliessend = 1 GROUP BY HauptFA, VaterFA, SubFA, Position, Artnr, WarehouseRequisitionItemId, Kuerzel HAVING COUNT(*) > 1';
    SET NOEXEC ON;
END
GO

DECLARE @filter NVARCHAR(MAX) = (SELECT filter_definition FROM sys.indexes
    WHERE name = N'UX_BdeTeilMeldungen_Schluessel' AND object_id = OBJECT_ID(N'dbo.BdeTeilMeldungen'));
IF @filter IS NULL OR @filter NOT LIKE N'%IstAbschliessend%'
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BdeTeilMeldungen_Schluessel' AND object_id = OBJECT_ID(N'dbo.BdeTeilMeldungen'))
        DROP INDEX [UX_BdeTeilMeldungen_Schluessel] ON [dbo].[BdeTeilMeldungen];
    CREATE UNIQUE INDEX [UX_BdeTeilMeldungen_Schluessel] ON [dbo].[BdeTeilMeldungen]([HauptFA], [VaterFA], [SubFA], [Position], [Artnr], [Kuerzel])
        WHERE [IsCancelled] = 0 AND [IstAbschliessend] = 1 AND [WarehouseRequisitionItemId] IS NULL;
    PRINT N'Index UX_BdeTeilMeldungen_Schluessel neu angelegt (nur abschliessende Meldungen eindeutig).';
END
GO

DECLARE @filter NVARCHAR(MAX) = (SELECT filter_definition FROM sys.indexes
    WHERE name = N'UX_BdeTeilMeldungen_Lagerbestellung' AND object_id = OBJECT_ID(N'dbo.BdeTeilMeldungen'));
IF @filter IS NULL OR @filter NOT LIKE N'%IstAbschliessend%'
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BdeTeilMeldungen_Lagerbestellung' AND object_id = OBJECT_ID(N'dbo.BdeTeilMeldungen'))
        DROP INDEX [UX_BdeTeilMeldungen_Lagerbestellung] ON [dbo].[BdeTeilMeldungen];
    CREATE UNIQUE INDEX [UX_BdeTeilMeldungen_Lagerbestellung] ON [dbo].[BdeTeilMeldungen]([WarehouseRequisitionItemId], [Kuerzel])
        WHERE [IsCancelled] = 0 AND [IstAbschliessend] = 1 AND [WarehouseRequisitionItemId] IS NOT NULL;
    PRINT N'Index UX_BdeTeilMeldungen_Lagerbestellung neu angelegt (nur abschliessende Meldungen eindeutig).';
END
GO

IF COL_LENGTH(N'dbo.BdeTeilMeldungen', N'IstAbschliessend') IS NOT NULL
   AND COL_LENGTH(N'dbo.ProductionWorkplaces', N'BdeStandardsichtTeile') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BdeTeilMeldungen_Schluessel' AND object_id = OBJECT_ID(N'dbo.BdeTeilMeldungen')
               AND filter_definition LIKE N'%IstAbschliessend%')
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BdeTeilMeldungen_Lagerbestellung' AND object_id = OBJECT_ID(N'dbo.BdeTeilMeldungen')
               AND filter_definition LIKE N'%IstAbschliessend%')
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261125191311_AddBdeTeilMeldungIstAbschliessendUndStandardsicht')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261125191311_AddBdeTeilMeldungIstAbschliessendUndStandardsicht', '10.0.2');
    PRINT 'Migration 20261125191311_AddBdeTeilMeldungIstAbschliessendUndStandardsicht eingetragen.';
END
GO

SET NOEXEC OFF;
GO

-- =============================================================================
-- 165_AddBdeTeilelisteLagerbestellungen.sql  (v1.99.72, Spec 2026-11-20-bde-liste-isolierungsfraese-pfad-material F6-F11, ADR 0038)
--   * dbo.ProductionWorkplaces + BdeTeilelisteLagerbestellungen BIT NOT NULL DEFAULT 0 (Werkbank-Option, ueberall aus).
--   * dbo.BdeTeilMeldungen + WarehouseRequisitionItemId INT NULL, FK auf dbo.WarehouseRequisitionItems (ON DELETE NO ACTION).
--   * UX_BdeTeilMeldungen_Schluessel neu mit Filter [IsCancelled] = 0 AND [WarehouseRequisitionItemId] IS NULL
--     (der Filter wird nur ENGER -> keine Dubletten moeglich). Neuaufbau nur, wenn filter_definition die Spalte
--     WarehouseRequisitionItemId noch nicht enthaelt. Traegt der vorhandene Filter schon [IstAbschliessend] (BDE-Masken-Epic,
--     SQL/169, falls es zuerst lief), wird der vereinigte Filter
--     [IsCancelled] = 0 AND [IstAbschliessend] = 1 AND [WarehouseRequisitionItemId] IS NULL gebaut - nie ein Teil verloren.
--     Umgekehrt muss SQL/169 seinen Neuaufbau ebenfalls an filter_definition festmachen (ohne IstAbschliessend -> neu bauen,
--     dabei WarehouseRequisitionItemId IS NULL behalten).
--   * Neu UX_BdeTeilMeldungen_Lagerbestellung UNIQUE (WarehouseRequisitionItemId, Kuerzel)
--     WHERE [IsCancelled] = 0 AND [WarehouseRequisitionItemId] IS NOT NULL (Doppelschutz Lagerbestell-Meldung, ADR 0026).
--     Fuehrende Spalte = FK-Spalte, daher kein eigener IX auf WarehouseRequisitionItemId (wie die EF-Migration).
-- Additiv bis auf den Index-Neuaufbau (kleine Tabelle), nicht daten-destruktiv, kein Backfill. SQL/AgentJobs nicht betroffen.
-- Objektnamen wie die EF-Migration 20261125173021_AddBdeTeilelisteLagerbestellungen.
-- Idempotent (COL_LENGTH-/OBJECT_ID-/sys.indexes-Guards), DDL je eigener Batch, History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.ProductionWorkplaces', N'U') IS NULL OR OBJECT_ID(N'dbo.BdeTeilMeldungen', N'U') IS NULL
   OR OBJECT_ID(N'dbo.WarehouseRequisitionItems', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.ProductionWorkplaces, dbo.BdeTeilMeldungen oder dbo.WarehouseRequisitionItems fehlt - falsche Datenbank / SQL/154 fehlt? Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.ProductionWorkplaces', N'BdeTeilelisteLagerbestellungen') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductionWorkplaces] ADD [BdeTeilelisteLagerbestellungen] BIT NOT NULL
        CONSTRAINT [DF_ProductionWorkplaces_BdeTeilelisteLagerbestellungen] DEFAULT 0;
    PRINT N'Spalte ProductionWorkplaces.BdeTeilelisteLagerbestellungen angelegt.';
END
GO

IF COL_LENGTH(N'dbo.BdeTeilMeldungen', N'WarehouseRequisitionItemId') IS NULL
BEGIN
    ALTER TABLE [dbo].[BdeTeilMeldungen] ADD [WarehouseRequisitionItemId] INT NULL;
    PRINT N'Spalte BdeTeilMeldungen.WarehouseRequisitionItemId angelegt.';
END
GO

IF OBJECT_ID(N'[dbo].[FK_BdeTeilMeldungen_WarehouseRequisitionItems_WarehouseRequisitionItemId]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[BdeTeilMeldungen] ADD CONSTRAINT [FK_BdeTeilMeldungen_WarehouseRequisitionItems_WarehouseRequisitionItemId]
        FOREIGN KEY ([WarehouseRequisitionItemId]) REFERENCES [dbo].[WarehouseRequisitionItems]([Id]);
    PRINT N'FK FK_BdeTeilMeldungen_WarehouseRequisitionItems_WarehouseRequisitionItemId angelegt.';
END
GO

-- FA-Schluessel: nur neu bauen, wenn der Filter die Spalte WarehouseRequisitionItemId noch nicht kennt.
DECLARE @filter NVARCHAR(MAX) = (SELECT filter_definition FROM sys.indexes
    WHERE name = N'UX_BdeTeilMeldungen_Schluessel' AND object_id = OBJECT_ID(N'dbo.BdeTeilMeldungen'));
IF @filter IS NULL OR @filter NOT LIKE N'%WarehouseRequisitionItemId%'
BEGIN
    DECLARE @neu NVARCHAR(400) = N'[IsCancelled] = 0'
        + CASE WHEN @filter LIKE N'%IstAbschliessend%' THEN N' AND [IstAbschliessend] = 1' ELSE N'' END
        + N' AND [WarehouseRequisitionItemId] IS NULL';
    IF @filter IS NOT NULL OR EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BdeTeilMeldungen_Schluessel' AND object_id = OBJECT_ID(N'dbo.BdeTeilMeldungen'))
        DROP INDEX [UX_BdeTeilMeldungen_Schluessel] ON [dbo].[BdeTeilMeldungen];
    DECLARE @sql NVARCHAR(MAX) = N'CREATE UNIQUE INDEX [UX_BdeTeilMeldungen_Schluessel] ON [dbo].[BdeTeilMeldungen]'
        + N'([HauptFA], [VaterFA], [SubFA], [Position], [Artnr], [Kuerzel]) WHERE ' + @neu + N';';
    EXEC sys.sp_executesql @sql;
    PRINT N'Index UX_BdeTeilMeldungen_Schluessel neu angelegt mit Filter ' + @neu;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BdeTeilMeldungen_Lagerbestellung' AND object_id = OBJECT_ID(N'dbo.BdeTeilMeldungen'))
BEGIN
    CREATE UNIQUE INDEX [UX_BdeTeilMeldungen_Lagerbestellung] ON [dbo].[BdeTeilMeldungen]([WarehouseRequisitionItemId], [Kuerzel])
        WHERE [IsCancelled] = 0 AND [WarehouseRequisitionItemId] IS NOT NULL;
    PRINT N'Index UX_BdeTeilMeldungen_Lagerbestellung angelegt.';
END
GO

IF COL_LENGTH(N'dbo.ProductionWorkplaces', N'BdeTeilelisteLagerbestellungen') IS NOT NULL
   AND COL_LENGTH(N'dbo.BdeTeilMeldungen', N'WarehouseRequisitionItemId') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BdeTeilMeldungen_Lagerbestellung' AND object_id = OBJECT_ID(N'dbo.BdeTeilMeldungen'))
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261125173021_AddBdeTeilelisteLagerbestellungen')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261125173021_AddBdeTeilelisteLagerbestellungen', '10.0.2');
    PRINT 'Migration 20261125173021_AddBdeTeilelisteLagerbestellungen eingetragen.';
END
GO

SET NOEXEC OFF;
GO

-- =============================================================================
-- 96_AddOperationConfigSourceAndSageMissingSince.sql  (v1.61.0, Spec 2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view)
-- Arbeitsgang-Stammdaten: Herkunft [Source] (Sage | ExternalSystem | Manual, UI "Quelle") und Zustand [SageMissingSince]
-- ("nicht in Sage seit", Arbeitsgang-Abgleich). Additiv, nicht destruktiv; Bestandsbefuellung aendert nur die neue Spalte:
-- CreatedBy = 'system' (OSEON-Seed bzw. gesamter Altbestand vor v1.54.0, SQL/95 B8) -> ExternalSystem, uebrige Manual.
-- Vorher Pruefabfrage P5 je Standort (Spec). Voraussetzung: SQL/95 (Tabelle OperationConfigs).
-- Objektnamen wie die EF-Migration 20261016090641_AddOperationConfigSourceAndSageMissingSince; Bestandsbefuellung wortgleich
-- (Drift-Test OperationConfigSourceMigrationSqlTests). Idempotent: zweiter Lauf aendert nichts.
-- Default-Constraint hier benannt (DF_OperationConfigs_Source), EF vergibt einen Systemnamen: ein spaeteres DROP muss den
-- Namen ueber sys.default_constraints suchen (Muster SQL/65, SQL/76).
-- =============================================================================
SET NOCOUNT ON;
GO

-- Guard: vor SQL/95 heisst die Tabelle noch OseonOperationConfigs -> hier NICHTS ausfuehren (sonst History ohne Spalten)
IF OBJECT_ID(N'[dbo].[OperationConfigs]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: Tabelle dbo.OperationConfigs fehlt - zuerst SQL/95_RenameOperationConfigsAndAddAudit.sql ausfuehren. Keine Aenderung, keine History-Zeile.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.OperationConfigs', N'Source') IS NULL
BEGIN
    ALTER TABLE [dbo].[OperationConfigs] ADD [Source] NVARCHAR(20) NOT NULL CONSTRAINT [DF_OperationConfigs_Source] DEFAULT N'Manual';
    PRINT 'Spalte OperationConfigs.Source hinzugefuegt.';
END
GO

IF COL_LENGTH(N'dbo.OperationConfigs', N'SageMissingSince') IS NULL
BEGIN
    ALTER TABLE [dbo].[OperationConfigs] ADD [SageMissingSince] DATETIME2 NULL;
    PRINT 'Spalte OperationConfigs.SageMissingSince hinzugefuegt.';
END
GO

-- Bestandsbefuellung (eigener Batch: referenziert die Spalte von oben)
UPDATE [dbo].[OperationConfigs] SET [Source] = N'ExternalSystem' WHERE [CreatedBy] = N'system' AND [Source] = N'Manual';
PRINT CONCAT('Bestandsbefuellung Source = ExternalSystem: ', @@ROWCOUNT, ' Zeile(n).');
GO

-- Migrations-History (separater Batch, exakte MigrationId)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261016090641_AddOperationConfigSourceAndSageMissingSince')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261016090641_AddOperationConfigSourceAndSageMissingSince', '10.0.2');
    PRINT 'Migration 20261016090641_AddOperationConfigSourceAndSageMissingSince eingetragen.';
END
GO

SET NOEXEC OFF;
GO

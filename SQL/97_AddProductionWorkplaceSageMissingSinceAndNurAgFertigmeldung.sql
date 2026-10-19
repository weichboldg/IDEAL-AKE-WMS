-- =============================================================================
-- 97_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung.sql  (v1.63.0, Spec 2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung)
-- Werkbaenke: [SageMissingSince] ("nicht in Sage seit", setzt nur die Werkbank-Anlage) und [BdeNurAgFertigmeldung]
-- (Werkbank-Option "BDE nur AG-Fertigmeldung", Default 0). Additiv, nicht destruktiv, keine Bestandsbefuellung: die erste
-- Kennzeichnung macht der Dienst (nachvollziehbar im Aktivitaets-Protokoll).
-- Objektnamen wie die EF-Migration 20261019092105_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung.
-- Idempotent: zweiter Lauf aendert nichts. Default-Constraint hier benannt, EF vergibt einen Systemnamen: ein spaeteres DROP
-- muss den Namen ueber sys.default_constraints suchen (Muster SQL/65, SQL/76).
-- =============================================================================
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'[dbo].[ProductionWorkplaces]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: Tabelle dbo.ProductionWorkplaces fehlt - zuerst SQL/00_FreshInstall.sql ausfuehren. Keine Aenderung, keine History-Zeile.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.ProductionWorkplaces', N'SageMissingSince') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductionWorkplaces] ADD [SageMissingSince] DATETIME2 NULL;
    PRINT 'Spalte ProductionWorkplaces.SageMissingSince hinzugefuegt.';
END
GO

IF COL_LENGTH(N'dbo.ProductionWorkplaces', N'BdeNurAgFertigmeldung') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductionWorkplaces] ADD [BdeNurAgFertigmeldung] BIT NOT NULL
        CONSTRAINT [DF_ProductionWorkplaces_BdeNurAgFertigmeldung] DEFAULT 0;
    PRINT 'Spalte ProductionWorkplaces.BdeNurAgFertigmeldung hinzugefuegt.';
END
GO

-- Migrations-History (separater Batch, exakte MigrationId)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261019092105_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261019092105_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung', '10.0.2');
    PRINT 'Migration 20261019092105_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung eingetragen.';
END
GO

SET NOEXEC OFF;
GO

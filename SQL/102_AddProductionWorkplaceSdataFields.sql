-- =============================================================================
-- 102_AddProductionWorkplaceSdataFields.sql  (v1.73.0, Spec 2026-11-06-sage-sdata-stammdaten-sync, Etappe E6a)
-- Drei nullable Spalten auf [ProductionWorkplaces] fuer Felder, die nur der SData-Weg (PpsArbeitsplaetzeInfo) liefert:
--   SageBdeTerminalId     : NVARCHAR(20) NULL  (BdeTerminalId string(20), P-S1)
--   SageKostenstelle      : NVARCHAR(20) NULL  (Kostenstelle string(20), P-S1)
--   SageSchichtplannummer : NVARCHAR(31) NULL  (Schichtplannummer string(31), P-S1)
-- Der Werkbank-Abgleich schreibt sie nur, wenn die Quelle sie liefert; ein View-Lauf laesst sie unveraendert (F1).
-- Nummer: 102 = naechste freie im Buendel (die Reservierung 102 fuer E5 Stueckliste entfaellt, G13); laeuft NACH
-- 101_AddProductionOrderPositions.sql (G17, Deploy-Liste beachten).
-- Rein additiv, keine Bestandsbefuellung, nicht daten-destruktiv; DB-Backup vor Deploy wie ueblich.
-- Objektnamen wie die EF-Migration 20261106192342_AddProductionWorkplaceSdataFields.
-- Idempotent: zweiter Lauf aendert nichts. DDL je in eigenem Batch, History-Insert in separatem Batch.
-- =============================================================================
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.ProductionWorkplaces', 'SageBdeTerminalId') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductionWorkplaces] ADD [SageBdeTerminalId] NVARCHAR(20) NULL;
    PRINT 'Spalte ProductionWorkplaces.SageBdeTerminalId hinzugefuegt.';
END
GO

IF COL_LENGTH('dbo.ProductionWorkplaces', 'SageKostenstelle') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductionWorkplaces] ADD [SageKostenstelle] NVARCHAR(20) NULL;
    PRINT 'Spalte ProductionWorkplaces.SageKostenstelle hinzugefuegt.';
END
GO

IF COL_LENGTH('dbo.ProductionWorkplaces', 'SageSchichtplannummer') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductionWorkplaces] ADD [SageSchichtplannummer] NVARCHAR(31) NULL;
    PRINT 'Spalte ProductionWorkplaces.SageSchichtplannummer hinzugefuegt.';
END
GO

-- Migrations-History (separater Batch, exakte MigrationId)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261106192342_AddProductionWorkplaceSdataFields')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261106192342_AddProductionWorkplaceSdataFields', '10.0.2');
    PRINT 'Migration 20261106192342_AddProductionWorkplaceSdataFields eingetragen.';
END
GO

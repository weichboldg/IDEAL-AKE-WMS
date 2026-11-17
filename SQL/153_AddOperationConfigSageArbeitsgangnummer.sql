-- =============================================================================
-- 153_AddOperationConfigSageArbeitsgangnummer.sql  (v1.99.27, ausgeliefert mit v1.99.32;
-- Spec 2026-11-16-arbeitsgang-mapping-sage-arbeitsgangnummer)
-- Arbeitsgang-Stammdaten: [SageArbeitsgangnummer] (Sage-Wert, schreibt nur der Arbeitsgang-Abgleich) und
-- [SageArbeitsgangnummerManuell] (Handwert, schreibt nur /OperationConfigs). Additiv, nicht destruktiv, keine Datenbefuellung
-- (der erste Abgleich-Lauf nach dem Deploy fuellt die Sage-Werte). Voraussetzung: SQL/96.
-- Objektnamen wie die EF-Migration 20261117085624_AddOperationConfigSageArbeitsgangnummer. Idempotent.
-- =============================================================================
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'[dbo].[OperationConfigs]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: Tabelle dbo.OperationConfigs fehlt - zuerst SQL/95 und SQL/96 ausfuehren. Keine Aenderung, keine History-Zeile.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.OperationConfigs', N'SageArbeitsgangnummer') IS NULL
BEGIN
    ALTER TABLE [dbo].[OperationConfigs] ADD [SageArbeitsgangnummer] NVARCHAR(31) NULL;
    PRINT 'Spalte OperationConfigs.SageArbeitsgangnummer hinzugefuegt.';
END
GO

IF COL_LENGTH(N'dbo.OperationConfigs', N'SageArbeitsgangnummerManuell') IS NULL
BEGIN
    ALTER TABLE [dbo].[OperationConfigs] ADD [SageArbeitsgangnummerManuell] NVARCHAR(31) NULL;
    PRINT 'Spalte OperationConfigs.SageArbeitsgangnummerManuell hinzugefuegt.';
END
GO

-- Migrations-History (separater Batch, exakte MigrationId)
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20261117085624_AddOperationConfigSageArbeitsgangnummer')
   AND COL_LENGTH(N'dbo.OperationConfigs', N'SageArbeitsgangnummer') IS NOT NULL
   AND COL_LENGTH(N'dbo.OperationConfigs', N'SageArbeitsgangnummerManuell') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261117085624_AddOperationConfigSageArbeitsgangnummer', N'10.0.2');
    PRINT 'Migration 20261117085624_AddOperationConfigSageArbeitsgangnummer eingetragen.';
END
GO

SET NOEXEC OFF;
GO

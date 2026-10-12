-- =============================================================================
-- 95_RenameOperationConfigsAndAddAudit.sql  (v1.54.0, Spec 2026-10-09-arbeitsgang-stammdaten-vereinheitlichen)
-- Arbeitsgang-Stammdaten: OseonOperationConfigs -> OperationConfigs, IsOseonRelevant -> IsExternalSystemRelevant,
-- PK/Index umbenannt, UQ_-Constraint (Skript-/FreshInstall-Form) -> Unique-Index IX_OperationConfigs_OperationName,
-- Audit-Felder (Bestand CreatedBy/CreatedByWindows = 'system', CreatedAt = Ausfuehrungszeitpunkt).
-- Objektnamen exakt wie die EF-Migration 20261012140548_RenameOperationConfigsAndAddAudit (identische Batches, Drift-Test
-- OperationConfigsMigrationSqlTests). Idempotent: jeder Batch geguarded, zweiter Lauf aendert nichts.
-- Nicht daten-destruktiv, aber Schema-Umbenennung -> DB-BACKUP VOR DEPLOY. App-Pool vorher stoppen: alte Web-Binaries
-- fragen nach dem Skript die alte Tabelle ab.
-- =============================================================================

-- B1 Tabelle umbenennen; existieren alte UND neue Tabelle, abbrechen (melden statt still)
IF OBJECT_ID(N'dbo.OseonOperationConfigs', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.OperationConfigs', N'U') IS NOT NULL
    THROW 50001, N'OseonOperationConfigs und OperationConfigs existieren beide - manuell klaeren, keine automatische Umbenennung.', 1;
IF OBJECT_ID(N'dbo.OseonOperationConfigs', N'U') IS NOT NULL
    EXEC sp_rename N'dbo.OseonOperationConfigs', N'OperationConfigs';
GO

-- B1a Nur im Skript (QA I-1): THROW beendet in SSMS/sqlcmd ohne -b nur SEINEN Batch, die folgenden GO-Batches liefen weiter
-- (B5-B9 auf der leeren neuen Tabelle + History-Eintrag -> Migrate() ueberspraenge die Migration). Steht die alte Tabelle
-- nach B1 noch da, wurde nicht umbenannt: ab hier NICHTS mehr ausfuehren (SET NOEXEC OFF am Skriptende).
IF OBJECT_ID(N'dbo.OseonOperationConfigs', N'U') IS NOT NULL
BEGIN
    PRINT N'ABBRUCH: dbo.OseonOperationConfigs besteht weiter (siehe Meldung von B1) - keine weiteren Aenderungen, keine History-Zeile.';
    SET NOEXEC ON;
END
GO

-- B2 Primaerschluessel (beide DB-Formen heissen gleich)
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'PK_OseonOperationConfigs' AND parent_object_id = OBJECT_ID(N'dbo.OperationConfigs'))
    EXEC sp_rename N'dbo.PK_OseonOperationConfigs', N'PK_OperationConfigs', N'OBJECT';
GO

-- B3 Spalte IsOseonRelevant -> IsExternalSystemRelevant
IF COL_LENGTH(N'dbo.OperationConfigs', N'IsOseonRelevant') IS NOT NULL AND COL_LENGTH(N'dbo.OperationConfigs', N'IsExternalSystemRelevant') IS NULL
    EXEC sp_rename N'dbo.OperationConfigs.IsOseonRelevant', N'IsExternalSystemRelevant', N'COLUMN';
GO

-- B4 EF-Form: Unique-Index umbenennen (muss vor B5 laufen)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OseonOperationConfigs_OperationName' AND object_id = OBJECT_ID(N'dbo.OperationConfigs'))
    EXEC sp_rename N'dbo.OperationConfigs.IX_OseonOperationConfigs_OperationName', N'IX_OperationConfigs_OperationName', N'INDEX';
GO

-- B5 Skript-/FreshInstall-Form: Ziel-Index anlegen, falls noch keiner da ist
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OperationConfigs_OperationName' AND object_id = OBJECT_ID(N'dbo.OperationConfigs'))
    CREATE UNIQUE INDEX [IX_OperationConfigs_OperationName] ON [dbo].[OperationConfigs] ([OperationName]);
GO

-- B6 ... und erst danach den alten UQ-Constraint entfernen (keine Luecke in der Eindeutigkeit)
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_OseonOperationConfigs_OperationName' AND parent_object_id = OBJECT_ID(N'dbo.OperationConfigs'))
    ALTER TABLE [dbo].[OperationConfigs] DROP CONSTRAINT [UQ_OseonOperationConfigs_OperationName];
GO

-- B7 Audit-Spalten nullable anlegen
IF COL_LENGTH(N'dbo.OperationConfigs', N'CreatedAt') IS NULL         ALTER TABLE [dbo].[OperationConfigs] ADD [CreatedAt] DATETIME2 NULL;
IF COL_LENGTH(N'dbo.OperationConfigs', N'CreatedBy') IS NULL         ALTER TABLE [dbo].[OperationConfigs] ADD [CreatedBy] NVARCHAR(200) NULL;
IF COL_LENGTH(N'dbo.OperationConfigs', N'CreatedByWindows') IS NULL  ALTER TABLE [dbo].[OperationConfigs] ADD [CreatedByWindows] NVARCHAR(200) NULL;
IF COL_LENGTH(N'dbo.OperationConfigs', N'ModifiedAt') IS NULL        ALTER TABLE [dbo].[OperationConfigs] ADD [ModifiedAt] DATETIME2 NULL;
IF COL_LENGTH(N'dbo.OperationConfigs', N'ModifiedBy') IS NULL        ALTER TABLE [dbo].[OperationConfigs] ADD [ModifiedBy] NVARCHAR(200) NULL;
IF COL_LENGTH(N'dbo.OperationConfigs', N'ModifiedByWindows') IS NULL ALTER TABLE [dbo].[OperationConfigs] ADD [ModifiedByWindows] NVARCHAR(200) NULL;
GO

-- B8 Bestand befuellen (eigener Batch: referenziert die Spalten aus B7)
UPDATE [dbo].[OperationConfigs] SET [CreatedAt] = GETDATE() WHERE [CreatedAt] IS NULL;
UPDATE [dbo].[OperationConfigs] SET [CreatedBy] = N'system' WHERE [CreatedBy] IS NULL;
UPDATE [dbo].[OperationConfigs] SET [CreatedByWindows] = N'system' WHERE [CreatedByWindows] IS NULL;
GO

-- B9 Pflichtspalten NOT NULL (Guard is_nullable)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OperationConfigs') AND name = N'CreatedAt' AND is_nullable = 1)
    ALTER TABLE [dbo].[OperationConfigs] ALTER COLUMN [CreatedAt] DATETIME2 NOT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OperationConfigs') AND name = N'CreatedBy' AND is_nullable = 1)
    ALTER TABLE [dbo].[OperationConfigs] ALTER COLUMN [CreatedBy] NVARCHAR(200) NOT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OperationConfigs') AND name = N'CreatedByWindows' AND is_nullable = 1)
    ALTER TABLE [dbo].[OperationConfigs] ALTER COLUMN [CreatedByWindows] NVARCHAR(200) NOT NULL;
GO

-- Migrations-History (separater Batch, exakte MigrationId)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261012140548_RenameOperationConfigsAndAddAudit')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261012140548_RenameOperationConfigsAndAddAudit', '10.0.2');
    PRINT 'Migration 20261012140548_RenameOperationConfigsAndAddAudit eingetragen.';
END
GO

SET NOEXEC OFF;
GO

-- =============================================
-- 70_AddUserDefaultWorkStep.sql (v1.22.0)
-- Fuegt der Users-Tabelle den per-User Standard-Arbeitsgang fuer die
-- FA-Abarbeitungsliste hinzu: Spalte DefaultWorkStepId (FK -> WorkSteps, SET NULL).
-- Idempotent, kann mehrfach ausgefuehrt werden (Reapply-fest).
--
-- Entspricht der EF-Migration 20260615074318_AddUserDefaultWorkStep.
-- =============================================

-- =============================================
-- SECTION A: SPALTE ANLEGEN (idempotent, eigener Guard)
-- =============================================
IF COL_LENGTH('dbo.Users', 'DefaultWorkStepId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD [DefaultWorkStepId] INT NULL;
    PRINT 'Spalte Users.DefaultWorkStepId erstellt.';
END
ELSE
    PRINT 'Spalte Users.DefaultWorkStepId bereits vorhanden - uebersprungen.';
GO

-- =============================================
-- SECTION B: INDEX ANLEGEN (idempotent, eigener Guard)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_DefaultWorkStepId' AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
    CREATE INDEX [IX_Users_DefaultWorkStepId] ON [dbo].[Users] ([DefaultWorkStepId]);
    PRINT 'Index IX_Users_DefaultWorkStepId erstellt.';
END
ELSE
    PRINT 'Index IX_Users_DefaultWorkStepId bereits vorhanden - uebersprungen.';
GO

-- =============================================
-- SECTION C: FK-CONSTRAINT ANLEGEN (idempotent, eigener Guard)
-- WorkSteps muss bereits existieren (Migration 67, v1.22.0).
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Users_WorkSteps_DefaultWorkStepId')
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD CONSTRAINT [FK_Users_WorkSteps_DefaultWorkStepId]
        FOREIGN KEY ([DefaultWorkStepId]) REFERENCES [dbo].[WorkSteps] ([Id])
        ON DELETE SET NULL;
    PRINT 'FK FK_Users_WorkSteps_DefaultWorkStepId erstellt.';
END
ELSE
    PRINT 'FK FK_Users_WorkSteps_DefaultWorkStepId bereits vorhanden - uebersprungen.';
GO

-- =============================================
-- SECTION D: EF MIGRATIONS HISTORY
-- =============================================
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260615074318_AddUserDefaultWorkStep')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260615074318_AddUserDefaultWorkStep', '10.0.2');
GO

PRINT '70_AddUserDefaultWorkStep abgeschlossen.';
GO

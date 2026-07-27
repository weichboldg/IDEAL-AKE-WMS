-- =============================================
-- 71_AddUserDefaultWorkplace.sql (v1.22.0)
-- Fuegt der Users-Tabelle den per-User Standard-Werkbank-Zusatzfilter fuer die
-- FA-Abarbeitungsliste hinzu: Spalte DefaultWorkplaceId (FK -> ProductionWorkplaces, SET NULL).
-- Idempotent, kann mehrfach ausgefuehrt werden (Reapply-fest).
--
-- Entspricht der EF-Migration 20260616070943_AddUserDefaultWorkplace.
-- =============================================

-- =============================================
-- SECTION A: SPALTE ANLEGEN (idempotent, eigener Guard)
-- =============================================
IF COL_LENGTH('dbo.Users', 'DefaultWorkplaceId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD [DefaultWorkplaceId] INT NULL;
    PRINT 'Spalte Users.DefaultWorkplaceId erstellt.';
END
ELSE
    PRINT 'Spalte Users.DefaultWorkplaceId bereits vorhanden - uebersprungen.';
GO

-- =============================================
-- SECTION B: INDEX ANLEGEN (idempotent, eigener Guard)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_DefaultWorkplaceId' AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
    CREATE INDEX [IX_Users_DefaultWorkplaceId] ON [dbo].[Users] ([DefaultWorkplaceId]);
    PRINT 'Index IX_Users_DefaultWorkplaceId erstellt.';
END
ELSE
    PRINT 'Index IX_Users_DefaultWorkplaceId bereits vorhanden - uebersprungen.';
GO

-- =============================================
-- SECTION C: FK-CONSTRAINT ANLEGEN (idempotent, eigener Guard)
-- ProductionWorkplaces muss bereits existieren (Migration 20260306081711).
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Users_ProductionWorkplaces_DefaultWorkplaceId')
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD CONSTRAINT [FK_Users_ProductionWorkplaces_DefaultWorkplaceId]
        FOREIGN KEY ([DefaultWorkplaceId]) REFERENCES [dbo].[ProductionWorkplaces] ([Id])
        ON DELETE SET NULL;
    PRINT 'FK FK_Users_ProductionWorkplaces_DefaultWorkplaceId erstellt.';
END
ELSE
    PRINT 'FK FK_Users_ProductionWorkplaces_DefaultWorkplaceId bereits vorhanden - uebersprungen.';
GO

-- =============================================
-- SECTION D: EF MIGRATIONS HISTORY
-- =============================================
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260616070943_AddUserDefaultWorkplace')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260616070943_AddUserDefaultWorkplace', '10.0.2');
GO

PRINT '71_AddUserDefaultWorkplace abgeschlossen.';
GO

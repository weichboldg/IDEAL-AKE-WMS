-- Relax filtered UNIQUE indexes on BdeBookings to allow multi-booking.
-- Idempotent: safe to re-run.

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookings_BdeOperatorId_Active' AND object_id = OBJECT_ID(N'dbo.BdeBookings') AND is_unique = 1)
BEGIN
    DROP INDEX [IX_BdeBookings_BdeOperatorId_Active] ON [dbo].[BdeBookings];
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookings_BdeOperatorId_Active' AND object_id = OBJECT_ID(N'dbo.BdeBookings'))
BEGIN
    CREATE INDEX [IX_BdeBookings_BdeOperatorId_Active]
        ON [dbo].[BdeBookings] ([BdeOperatorId])
        WHERE [EndedAt] IS NULL AND [IsCancelled] = 0;
END
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookings_WorkOperationId_Active' AND object_id = OBJECT_ID(N'dbo.BdeBookings') AND is_unique = 1)
BEGIN
    DROP INDEX [IX_BdeBookings_WorkOperationId_Active] ON [dbo].[BdeBookings];
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookings_WorkOperationId_Active' AND object_id = OBJECT_ID(N'dbo.BdeBookings'))
BEGIN
    CREATE INDEX [IX_BdeBookings_WorkOperationId_Active]
        ON [dbo].[BdeBookings] ([WorkOperationId])
        WHERE [EndedAt] IS NULL AND [IsCancelled] = 0 AND [WorkOperationId] IS NOT NULL;
END
GO

-- Seed der neuen AppSettings (idempotent)
IF NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = 'BdeMehrfachBuchungProOperator')
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES ('BdeMehrfachBuchungProOperator', 'false', 'Ein Mitarbeiter darf mehrere parallele Buchungen haben (auf verschiedenen Arbeitsgaengen)');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = 'BdeMehrfachBuchungProArbeitsgang')
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES ('BdeMehrfachBuchungProArbeitsgang', 'false', 'Ein Arbeitsgang darf mehrere parallele Buchungen haben (durch verschiedene Mitarbeiter)');
GO

-- EFMigrationsHistory-Insert
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId LIKE '%_RelaxBdeBookingConstraints')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260421082847_RelaxBdeBookingConstraints', '10.0.2');
END
GO

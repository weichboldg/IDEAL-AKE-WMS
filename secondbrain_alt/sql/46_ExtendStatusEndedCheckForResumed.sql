-- Extend CK_BdeBookings_StatusEnded to allow Status=4 (Resumed) with EndedAt NOT NULL.
-- Idempotent: safe to re-run.

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_BdeBookings_StatusEnded')
BEGIN
    ALTER TABLE [dbo].[BdeBookings] DROP CONSTRAINT [CK_BdeBookings_StatusEnded];
END
GO

ALTER TABLE [dbo].[BdeBookings] ADD CONSTRAINT [CK_BdeBookings_StatusEnded]
    CHECK (([Status] = 1 AND [EndedAt] IS NULL) OR ([Status] IN (2,3,4) AND [EndedAt] IS NOT NULL));
GO

-- EF migration history
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId LIKE '%_ExtendStatusEndedCheckForResumed')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260422092844_ExtendStatusEndedCheckForResumed', '10.0.2');
END
GO

-- ============================================================================
-- Migration 74: AddLagerbestellungRole
-- ============================================================================
-- Fuegt die Rolle 'lagerbestellung' hinzu (Zugriff nur auf Meine
-- Lagerbestellungen + Meine Fehlteile). Idempotent via IF NOT EXISTS.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'lagerbestellung')
    BEGIN
        INSERT INTO Roles ([Key], [Name], [Description], [IsSystem], [SortOrder],
                           [CreatedAt], [CreatedBy], [CreatedByWindows])
        VALUES ('lagerbestellung', 'Lagerbestellungen',
                'Lagerbestellungen erfassen und eigene Fehlteile verfolgen (Zugriff nur auf Meine Lagerbestellungen + Meine Fehlteile).',
                1, 8,
                SYSDATETIME(), 'system', 'system');
        PRINT 'Rolle lagerbestellung angelegt.';
    END
    ELSE
    BEGIN
        PRINT 'Rolle lagerbestellung existiert bereits — uebersprungen.';
    END

    IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260619063919_AddLagerbestellungRole')
    BEGIN
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20260619063919_AddLagerbestellungRole', '10.0.2');
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH

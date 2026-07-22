-- ============================================================================
-- Migration 67: AddMasterDataReadRole
-- ============================================================================
-- Fuegt eine neue Rolle 'masterdata_read' hinzu (Nur-Lesen-Zugriff auf
-- Stammdaten-Sichten). Idempotent via IF NOT EXISTS.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'masterdata_read')
    BEGIN
        INSERT INTO Roles ([Key], [Name], [Description], [AdGroup], [IsSystem], [SortOrder],
                           [CreatedAt], [CreatedBy], [CreatedByWindows])
        VALUES ('masterdata_read', 'Stammdaten ansehen',
                'Nur-Lesen-Zugriff auf alle Stammdaten-Sichten (Benutzer, Rollen, Arbeitsplaetze, Einstellungen, Werkbaenke, Empfaenger, Artikelkategorien/-attribute, Schichtkalender, Aktivitaets-Protokoll).',
                NULL, 1, 5,
                SYSDATETIME(), 'system', 'system');
        PRINT 'Rolle masterdata_read angelegt.';
    END
    ELSE
    BEGIN
        PRINT 'Rolle masterdata_read existiert bereits — uebersprungen.';
    END

    IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260608060227_AddMasterDataReadRole')
    BEGIN
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20260608060227_AddMasterDataReadRole', '10.0.0');
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH

-- ============================================================================
-- Migration 78: AddStockReadRole (v1.25.0)
-- ============================================================================
-- Rolle 'stock_read' (Nur-Lesen: Bestände + Bewegungshistorie). Idempotent.
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'stock_read')
    BEGIN
        INSERT INTO Roles ([Key], [Name], [Description], [IsSystem], [SortOrder],
                           [CreatedAt], [CreatedBy], [CreatedByWindows])
        VALUES ('stock_read', 'Lagerbestand-Ansicht',
                'Nur-Lesen-Zugriff auf Bestände und Bewegungshistorie.',
                1, 35,
                SYSDATETIME(), 'system', 'system');
        PRINT 'Rolle stock_read angelegt.';
    END
    IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260707113155_AddStockReadRole')
    BEGIN
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20260707113155_AddStockReadRole', '10.0.2');
    END
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH

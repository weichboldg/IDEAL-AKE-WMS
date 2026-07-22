-- ============================================================================
-- Migration 77: AddWarehouseRequisitionTypeAndGlasRole (v1.25.0)
-- ============================================================================
-- 1) Spalte WarehouseRequisitions.Type (1=Lager, 2=Glas), Default 1.
-- 2) Rolle 'glasbestellung'. Idempotent.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.WarehouseRequisitions', 'Type') IS NULL
    BEGIN
        ALTER TABLE [dbo].[WarehouseRequisitions]
            ADD [Type] INT NOT NULL CONSTRAINT [DF_WarehouseRequisitions_Type] DEFAULT 1;
        PRINT 'Spalte WarehouseRequisitions.Type angelegt.';
    END

    IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'glasbestellung')
    BEGIN
        INSERT INTO Roles ([Key], [Name], [Description], [IsSystem], [SortOrder],
                           [CreatedAt], [CreatedBy], [CreatedByWindows])
        VALUES ('glasbestellung', 'Glasbestellungen',
                'Glas-Bestellungen erfassen und eigene Fehlteile verfolgen (Zugriff nur auf Lagerbestellungen (Reiter Glas) + Meine Fehlteile).',
                1, 9,
                SYSDATETIME(), 'system', 'system');
        PRINT 'Rolle glasbestellung angelegt.';
    END

    IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260706074119_AddWarehouseRequisitionTypeAndGlasRole')
    BEGIN
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20260706074119_AddWarehouseRequisitionTypeAndGlasRole', '10.0.2');
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH

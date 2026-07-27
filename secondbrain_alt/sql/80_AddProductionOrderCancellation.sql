-- ============================================================================
-- Migration 80: AddProductionOrderCancellation (v1.25.0)
-- ============================================================================
-- FA-Reconciliation: verwaiste FAs, die in Sage geloescht wurden, werden vom
-- Service-Sync auf IsCancelled=1 gesetzt (verschwinden aus offenen Sichten).
-- Additiv, Default IsCancelled=0 -> Alt-FAs bleiben unveraendert offen. Idempotent.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.ProductionOrders', 'IsCancelled') IS NULL
    BEGIN
        ALTER TABLE [dbo].[ProductionOrders]
            ADD [IsCancelled] BIT NOT NULL CONSTRAINT [DF_ProductionOrders_IsCancelled] DEFAULT 0;
        PRINT 'Spalte ProductionOrders.IsCancelled angelegt.';
    END

    IF COL_LENGTH('dbo.ProductionOrders', 'CancelledAt') IS NULL
    BEGIN
        ALTER TABLE [dbo].[ProductionOrders] ADD [CancelledAt] DATETIME2 NULL;
        PRINT 'Spalte ProductionOrders.CancelledAt angelegt.';
    END

    IF COL_LENGTH('dbo.ProductionOrders', 'CancelledBy') IS NULL
    BEGIN
        ALTER TABLE [dbo].[ProductionOrders] ADD [CancelledBy] NVARCHAR(256) NULL;
        PRINT 'Spalte ProductionOrders.CancelledBy angelegt.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductionOrders_IsCancelled' AND object_id = OBJECT_ID('dbo.ProductionOrders'))
    BEGIN
        CREATE INDEX [IX_ProductionOrders_IsCancelled] ON [dbo].[ProductionOrders] ([IsCancelled]);
        PRINT 'Index IX_ProductionOrders_IsCancelled angelegt.';
    END

    IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260707140249_AddProductionOrderCancellation')
    BEGIN
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20260707140249_AddProductionOrderCancellation', '10.0.2');
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH

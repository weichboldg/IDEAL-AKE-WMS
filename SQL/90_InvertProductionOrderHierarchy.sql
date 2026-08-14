-- SQL/90_InvertProductionOrderHierarchy.sql
-- Migration 90: InvertProductionOrderHierarchy (IDEAL Teil 7, Etappe A — Schema-Inversion)
--
-- Kern-Tabelle dbo.ProductionOrders: OrderNumber (= Sage StrukturID/HauptFA) ist nicht mehr
-- eindeutig; die Eindeutigkeit wandert auf die neue Spalte SubOrderNumber (= Sage BelID).
-- Zusaetzlich neu: ParentSubOrderNumber (echter Elternzeiger, nullable) und SageMissingSince
-- (datetime2 NULL, selbstheilender "aus der Quelle verschwunden"-Zeitstempel fuer Sync-Regel 2).
--
-- DATEN-KONVERTIEREND, NICHT DESTRUKTIV (Backfill fuellt die neue Spalte, loescht nichts).
-- Trotzdem: Kern-Tabelle betroffen -> DB-BACKUP VOR DEPLOY ZWINGEND. Ausserdem VOR der Migration
-- serverseitig pruefen, dass kein SQL-Agent-Job ProductionOrders beschreibt (siehe Spec Deploy-
-- Abschnitt / AK 13) — sonst feuert ein vergessener Job nach der Inversion in die NOT-NULL-Spalte.
--
-- Idempotent: COL_LENGTH-Guards fuer die Spalten, is_nullable-Guard fuer den NOT-NULL-Schritt,
-- sys.indexes/sys.key_constraints-Guards fuer den Index-/Constraint-Tausch. Reihenfolge daten-
-- konvertierend: Spalten additiv/nullable -> Backfill (SubOrderNumber = OrderNumber) -> NOT NULL
-- -> alte Eindeutigkeit auf OrderNumber loesen -> neue Indizes. DDL/Daten in getrennten Batches
-- (SQL Server parst Batches vorab; ein UPDATE auf eine im selben Batch neu angelegte Spalte
-- scheitert sonst). __EFMigrationsHistory-Insert im letzten, separaten Batch.
SET NOCOUNT ON;
GO

-- 1. Neue Spalten additiv, nullable (COL_LENGTH-Guards).
IF COL_LENGTH('dbo.ProductionOrders', 'SubOrderNumber') IS NULL
    ALTER TABLE [dbo].[ProductionOrders] ADD [SubOrderNumber] NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.ProductionOrders', 'ParentSubOrderNumber') IS NULL
    ALTER TABLE [dbo].[ProductionOrders] ADD [ParentSubOrderNumber] NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.ProductionOrders', 'SageMissingSince') IS NULL
    ALTER TABLE [dbo].[ProductionOrders] ADD [SageMissingSince] DATETIME2 NULL;
GO

-- 2. Backfill: im flachen (AKE-)Bestand gilt SubOrderNumber == OrderNumber. OrderNumber ist
--    NOT NULL, damit erhaelt jede Zeile einen Wert und der NOT-NULL-Schritt (3) kann greifen.
UPDATE [dbo].[ProductionOrders]
   SET [SubOrderNumber] = [OrderNumber]
 WHERE [SubOrderNumber] IS NULL;
GO

-- 3. SubOrderNumber auf NOT NULL setzen (nur wenn aktuell nullable — idempotent).
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.ProductionOrders')
             AND name = 'SubOrderNumber'
             AND is_nullable = 1)
    ALTER TABLE [dbo].[ProductionOrders] ALTER COLUMN [SubOrderNumber] NVARCHAR(100) NOT NULL;
GO

-- 4a. Alte Eindeutigkeit auf OrderNumber loesen — je nach DB-Herkunft ein UNIQUE-CONSTRAINT
--     (UQ_..., FreshInstall) ODER ein UNIQUE-INDEX (IX_..., EF-migriert). Beide Formen abraeumen.
IF EXISTS (SELECT 1 FROM sys.key_constraints
           WHERE name = 'UQ_ProductionOrders_OrderNumber'
             AND parent_object_id = OBJECT_ID('dbo.ProductionOrders'))
    ALTER TABLE [dbo].[ProductionOrders] DROP CONSTRAINT [UQ_ProductionOrders_OrderNumber];
GO
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = 'IX_ProductionOrders_OrderNumber'
             AND object_id = OBJECT_ID('dbo.ProductionOrders')
             AND is_unique = 1)
    DROP INDEX [IX_ProductionOrders_OrderNumber] ON [dbo].[ProductionOrders];
GO

-- 4b. Nicht-eindeutiger Index auf OrderNumber (nur wenn kein Index dieses Namens existiert).
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_ProductionOrders_OrderNumber'
                 AND object_id = OBJECT_ID('dbo.ProductionOrders'))
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_OrderNumber]
        ON [dbo].[ProductionOrders]([OrderNumber]);
GO

-- 4c. Neuer Unique-Index auf SubOrderNumber (sys.indexes-Guard, H7-1).
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_ProductionOrders_SubOrderNumber'
                 AND object_id = OBJECT_ID('dbo.ProductionOrders'))
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ProductionOrders_SubOrderNumber]
        ON [dbo].[ProductionOrders]([SubOrderNumber]);
GO

-- 5. Migrations-History (separater Batch).
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260814105526_InvertProductionOrderHierarchy')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260814105526_InvertProductionOrderHierarchy', '10.0.2');
END
GO

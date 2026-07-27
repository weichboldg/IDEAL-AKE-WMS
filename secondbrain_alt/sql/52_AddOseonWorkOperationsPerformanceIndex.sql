-- Phase: OSEON Tracking Performance v1.8.4
-- Composite Index fuer EXISTS-Subquery in OseonProductionOrderRepository.GetPagedAsync:
-- WHERE OseonProductionOrderId = X AND OseonStatus NOT IN (90,95) AND Name IN (...)
-- Idempotent.

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_OseonWorkOperations_OrderStatusName'
      AND object_id = OBJECT_ID('dbo.OseonWorkOperations'))
BEGIN
    CREATE INDEX IX_OseonWorkOperations_OrderStatusName
        ON dbo.OseonWorkOperations(OseonProductionOrderId, OseonStatus, Name);
END
GO

-- Move any app tables from svc_idealakewms schema to dbo (idempotent).
-- Tables are identified by the known table names from ApplicationDbContext DbSets.

DECLARE @tableNames TABLE (Name sysname);
INSERT INTO @tableNames (Name) VALUES
    ('Users'),
    ('Workstations'),
    ('WorkstationUsers'),
    ('StorageLocations'),
    ('Articles'),
    ('StockMovements'),
    ('ProductionOrders'),
    ('AppSettings'),
    ('Holidays'),
    ('PickingItems'),
    ('ProductionWorkplaces'),
    ('ProductionWorkplaceUsers'),
    ('WorkOperations'),
    ('OseonProductionOrders'),
    ('OseonWorkOperations'),
    ('ServiceSettings'),
    ('Roles'),
    ('UserRoles'),
    ('OseonOperationConfigs'),
    ('EnaioDmsDocuments'),
    ('OrderRecipientGroups'),
    ('OrderRecipients'),
    ('ArticleGroupRecipientMappings'),
    ('PartRequisitions'),
    ('ArticleCategories'),
    ('ArticleAttributeDefinitions'),
    ('ArticleAttributeOptions'),
    ('ArticleAttributeValues'),
    ('BdeActivities'),
    ('BdeBookings'),
    ('BdeBookingQuantities'),
    ('BdeOperators'),
    ('BdeTerminals'),
    ('CachedBomHeaders'),
    ('CachedBomItems'),
    ('UserViewPreferences'),
    ('__EFMigrationsHistory');

DECLARE @sql nvarchar(max) = N'';

SELECT @sql = @sql
    + N'ALTER SCHEMA dbo TRANSFER '
    + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N';' + CHAR(13) + CHAR(10)
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
INNER JOIN @tableNames n ON t.name = n.Name
WHERE s.name <> 'dbo';

IF LEN(@sql) > 0
BEGIN
    PRINT @sql;
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    PRINT 'No tables to transfer — all are already in dbo schema.';
END

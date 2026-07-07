namespace IDEALAKEWMSService.Services;

/// <summary>
/// Baut das MERGE-artige Upsert-SQL fuer ProductionOrders. Der INSERT-Zweig schreibt
/// SubOrderNumber = OrderNumber NUR wenn die Spalte in der Ziel-DB existiert (IDEAL-Schema,
/// NOT NULL + UNIQUE). Gegen eine DB ohne die Spalte bleibt der INSERT unveraendert.
/// Der UPDATE-Zweig laesst SubOrderNumber unangetastet.
/// </summary>
public static class SageProductionOrderSql
{
    public static string BuildUpsert(bool includeSubOrderNumber)
    {
        var insertCols = includeSubOrderNumber
            ? "[OrderNumber],[SubOrderNumber],[Quantity],[Customer],[ArticleNumber],[Description1],[Description2],[ProductionDate],[DeliveryDate],[IsDone],[CreatedAt],[CreatedBy],[CreatedByWindows]"
            : "[OrderNumber],[Quantity],[Customer],[ArticleNumber],[Description1],[Description2],[ProductionDate],[DeliveryDate],[IsDone],[CreatedAt],[CreatedBy],[CreatedByWindows]";
        var insertVals = includeSubOrderNumber
            ? "@OrderNumber,@OrderNumber,@Quantity,@Customer,@ArticleNumber,@Description1,@Description2,@ProductionDate,@DeliveryDate,0,GETUTCDATE(),'IDEALAKEWMSService',SYSTEM_USER"
            : "@OrderNumber,@Quantity,@Customer,@ArticleNumber,@Description1,@Description2,@ProductionDate,@DeliveryDate,0,GETUTCDATE(),'IDEALAKEWMSService',SYSTEM_USER";

        return $"""
            IF EXISTS (SELECT 1 FROM [dbo].[ProductionOrders] WHERE [OrderNumber] = @OrderNumber)
            BEGIN
                UPDATE [dbo].[ProductionOrders] SET
                    [Quantity]       = @Quantity,
                    [Customer]       = @Customer,
                    [ArticleNumber]  = @ArticleNumber,
                    [Description1]   = @Description1,
                    [Description2]   = @Description2,
                    [ProductionDate] = @ProductionDate,
                    [DeliveryDate]   = @DeliveryDate,
                    [ModifiedAt]     = GETUTCDATE(),
                    [ModifiedBy]     = 'IDEALAKEWMSService',
                    [ModifiedByWindows] = SYSTEM_USER
                WHERE [OrderNumber] = @OrderNumber
                  AND (
                      [Quantity] != @Quantity OR
                      ISNULL([Customer],'') != ISNULL(@Customer,'') OR
                      ISNULL([ArticleNumber],'') != ISNULL(@ArticleNumber,'') OR
                      ISNULL([Description1],'') != ISNULL(@Description1,'') OR
                      ISNULL([Description2],'') != ISNULL(@Description2,'') OR
                      ISNULL(CAST([ProductionDate] AS date),'1900-01-01') != ISNULL(CAST(@ProductionDate AS date),'1900-01-01') OR
                      ISNULL(CAST([DeliveryDate] AS date),'1900-01-01') != ISNULL(CAST(@DeliveryDate AS date),'1900-01-01')
                  )
                SELECT NULL AS InsertedId, @@ROWCOUNT AS Affected, 0 AS IsInsert
            END
            ELSE
            BEGIN
                INSERT INTO [dbo].[ProductionOrders]
                    ({insertCols})
                VALUES
                    ({insertVals})
                SELECT SCOPE_IDENTITY() AS InsertedId, 1 AS Affected, 1 AS IsInsert
            END
            """;
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsFinalShortageWithShortageStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "ShortageStatus",
                table: "WarehouseRequisitionItems",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.Sql(@"
                UPDATE [dbo].[WarehouseRequisitionItems]
                SET [ShortageStatus] = CASE
                    WHEN [IsFinalShortage] = 1 THEN 2
                    WHEN ([QuantityPicked] IS NULL OR [QuantityPicked] < [QuantityRequested]) THEN 1
                    ELSE 0
                END;
            ");

            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_WarehouseRequisitionItems_IsFinalShortage
                    ON [dbo].[WarehouseRequisitionItems];
            ");

            migrationBuilder.Sql(@"
                DECLARE @c NVARCHAR(200) = (
                    SELECT dc.name FROM sys.default_constraints dc
                    INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id
                        AND dc.parent_column_id = c.column_id
                    WHERE c.object_id = OBJECT_ID('[dbo].[WarehouseRequisitionItems]')
                      AND c.name = 'IsFinalShortage');
                IF @c IS NOT NULL EXEC('ALTER TABLE [dbo].[WarehouseRequisitionItems] DROP CONSTRAINT [' + @c + ']');
            ");
            migrationBuilder.DropColumn(
                name: "IsFinalShortage",
                table: "WarehouseRequisitionItems");

            migrationBuilder.Sql(@"
                CREATE INDEX IX_WarehouseRequisitionItems_ShortageStatus_WillBeRestocked
                    ON [dbo].[WarehouseRequisitionItems]([ShortageStatus])
                    WHERE [ShortageStatus] = 1;
                CREATE INDEX IX_WarehouseRequisitionItems_ShortageStatus_NoRestock
                    ON [dbo].[WarehouseRequisitionItems]([ShortageStatus])
                    WHERE [ShortageStatus] = 2;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_WarehouseRequisitionItems_ShortageStatus_WillBeRestocked
                    ON [dbo].[WarehouseRequisitionItems];
                DROP INDEX IF EXISTS IX_WarehouseRequisitionItems_ShortageStatus_NoRestock
                    ON [dbo].[WarehouseRequisitionItems];
            ");

            migrationBuilder.AddColumn<bool>(
                name: "IsFinalShortage",
                table: "WarehouseRequisitionItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE [dbo].[WarehouseRequisitionItems]
                SET [IsFinalShortage] = CASE WHEN [ShortageStatus] = 2 THEN 1 ELSE 0 END;
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IX_WarehouseRequisitionItems_IsFinalShortage
                    ON [dbo].[WarehouseRequisitionItems]([IsFinalShortage])
                    WHERE [IsFinalShortage] = 1;
            ");

            migrationBuilder.DropColumn(
                name: "ShortageStatus",
                table: "WarehouseRequisitionItems");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddIsFinalShortageToWarehouseRequisitionItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFinalShortage",
                schema: "dbo",
                table: "WarehouseRequisitionItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                CREATE INDEX IX_WarehouseRequisitionItems_IsFinalShortage
                    ON [dbo].[WarehouseRequisitionItems]([IsFinalShortage])
                    WHERE [IsFinalShortage] = 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_WarehouseRequisitionItems_IsFinalShortage
                    ON [dbo].[WarehouseRequisitionItems];
            ");

            migrationBuilder.DropColumn(
                name: "IsFinalShortage",
                schema: "dbo",
                table: "WarehouseRequisitionItems");
        }
    }
}

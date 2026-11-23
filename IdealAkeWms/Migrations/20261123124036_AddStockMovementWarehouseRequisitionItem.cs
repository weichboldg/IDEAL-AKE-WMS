using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovementWarehouseRequisitionItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WarehouseRequisitionItemId",
                schema: "dbo",
                table: "StockMovements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_WarehouseRequisitionItemId",
                schema: "dbo",
                table: "StockMovements",
                column: "WarehouseRequisitionItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_WarehouseRequisitionItems_WarehouseRequisitionItemId",
                schema: "dbo",
                table: "StockMovements",
                column: "WarehouseRequisitionItemId",
                principalSchema: "dbo",
                principalTable: "WarehouseRequisitionItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_WarehouseRequisitionItems_WarehouseRequisitionItemId",
                schema: "dbo",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_WarehouseRequisitionItemId",
                schema: "dbo",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "WarehouseRequisitionItemId",
                schema: "dbo",
                table: "StockMovements");
        }
    }
}

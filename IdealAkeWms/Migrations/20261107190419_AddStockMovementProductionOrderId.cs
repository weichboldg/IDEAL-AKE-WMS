using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovementProductionOrderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductionOrderId",
                schema: "dbo",
                table: "StockMovements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductionOrderId",
                schema: "dbo",
                table: "StockMovements",
                column: "ProductionOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_ProductionOrders_ProductionOrderId",
                schema: "dbo",
                table: "StockMovements",
                column: "ProductionOrderId",
                principalSchema: "dbo",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_ProductionOrders_ProductionOrderId",
                schema: "dbo",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ProductionOrderId",
                schema: "dbo",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ProductionOrderId",
                schema: "dbo",
                table: "StockMovements");
        }
    }
}

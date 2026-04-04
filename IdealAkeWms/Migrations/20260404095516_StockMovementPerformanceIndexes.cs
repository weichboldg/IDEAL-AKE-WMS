using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class StockMovementPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ArticleId_SourceStorageLocationId_MovementType",
                table: "StockMovements",
                columns: new[] { "ArticleId", "SourceStorageLocationId", "MovementType" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ArticleId_StorageLocationId",
                table: "StockMovements",
                columns: new[] { "ArticleId", "StorageLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductionOrder",
                table: "StockMovements",
                column: "ProductionOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ArticleId_SourceStorageLocationId_MovementType",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ArticleId_StorageLocationId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ProductionOrder",
                table: "StockMovements");
        }
    }
}

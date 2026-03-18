using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class OseonPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OseonProductionOrders_OseonStatus",
                table: "OseonProductionOrders",
                column: "OseonStatus");

            migrationBuilder.CreateIndex(
                name: "IX_OseonProductionOrders_WorkplaceName",
                table: "OseonProductionOrders",
                column: "WorkplaceName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OseonProductionOrders_OseonStatus",
                table: "OseonProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_OseonProductionOrders_WorkplaceName",
                table: "OseonProductionOrders");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddPickingRelease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReleasedForPicking",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PickingPriority",
                table: "ProductionOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleasedAt",
                table: "ProductionOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReleasedBy",
                table: "ProductionOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_IsReleasedForPicking_IsDone",
                table: "ProductionOrders",
                columns: new[] { "IsReleasedForPicking", "IsDone" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_IsReleasedForPicking_IsDone",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "IsReleasedForPicking",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "PickingPriority",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "ReleasedAt",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "ReleasedBy",
                table: "ProductionOrders");
        }
    }
}

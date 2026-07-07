using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionOrderCancellation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                schema: "dbo",
                table: "ProductionOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledBy",
                schema: "dbo",
                table: "ProductionOrders",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                schema: "dbo",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_IsCancelled",
                schema: "dbo",
                table: "ProductionOrders",
                column: "IsCancelled");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_IsCancelled",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "IsCancelled",
                schema: "dbo",
                table: "ProductionOrders");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class InvertProductionOrderHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Schema-Inversion der Kern-Tabelle ProductionOrders (IDEAL Teil 7).
            // Reihenfolge daten-konvertierend, nicht destruktiv: alten Unique-Index loesen,
            // Spalten additiv/nullable anlegen, SubOrderNumber = OrderNumber backfillen, erst
            // dann SubOrderNumber NOT NULL setzen und den neuen Unique-Index anlegen. Das
            // vermeidet die Unique-Verletzung, die ein Default '' auf Bestandsdaten ausloesen
            // wuerde. Das idempotente Deploy-Skript SQL/90 bildet dieselbe Reihenfolge nach.
            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_OrderNumber",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.AddColumn<string>(
                name: "ParentSubOrderNumber",
                schema: "dbo",
                table: "ProductionOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SageMissingSince",
                schema: "dbo",
                table: "ProductionOrders",
                type: "datetime2",
                nullable: true);

            // Zunaechst nullable anlegen, damit der Backfill greifen kann.
            migrationBuilder.AddColumn<string>(
                name: "SubOrderNumber",
                schema: "dbo",
                table: "ProductionOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Backfill: im flachen (AKE-)Bestand gilt die Invariante SubOrderNumber == OrderNumber.
            migrationBuilder.Sql(
                "UPDATE [dbo].[ProductionOrders] SET [SubOrderNumber] = [OrderNumber] WHERE [SubOrderNumber] IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "SubOrderNumber",
                schema: "dbo",
                table: "ProductionOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_OrderNumber",
                schema: "dbo",
                table: "ProductionOrders",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_SubOrderNumber",
                schema: "dbo",
                table: "ProductionOrders",
                column: "SubOrderNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_OrderNumber",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_SubOrderNumber",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "ParentSubOrderNumber",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "SageMissingSince",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "SubOrderNumber",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_OrderNumber",
                schema: "dbo",
                table: "ProductionOrders",
                column: "OrderNumber",
                unique: true);
        }
    }
}

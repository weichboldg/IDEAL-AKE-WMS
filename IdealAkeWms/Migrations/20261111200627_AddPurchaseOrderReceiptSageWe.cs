using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderReceiptSageWe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Lieferschein",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SageWeAm",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SageWeBelegnummer",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SageWeFehler",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SageWeStatus",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SageWeVersuche",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SageWeVorgang",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderReceipts_SageWeStatus",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                column: "SageWeStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderReceipts_SageWeVorgang",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                column: "SageWeVorgang");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderReceipts_SageWeStatus",
                schema: "dbo",
                table: "PurchaseOrderReceipts");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderReceipts_SageWeVorgang",
                schema: "dbo",
                table: "PurchaseOrderReceipts");

            migrationBuilder.DropColumn(
                name: "Lieferschein",
                schema: "dbo",
                table: "PurchaseOrderReceipts");

            migrationBuilder.DropColumn(
                name: "SageWeAm",
                schema: "dbo",
                table: "PurchaseOrderReceipts");

            migrationBuilder.DropColumn(
                name: "SageWeBelegnummer",
                schema: "dbo",
                table: "PurchaseOrderReceipts");

            migrationBuilder.DropColumn(
                name: "SageWeFehler",
                schema: "dbo",
                table: "PurchaseOrderReceipts");

            migrationBuilder.DropColumn(
                name: "SageWeStatus",
                schema: "dbo",
                table: "PurchaseOrderReceipts");

            migrationBuilder.DropColumn(
                name: "SageWeVersuche",
                schema: "dbo",
                table: "PurchaseOrderReceipts");

            migrationBuilder.DropColumn(
                name: "SageWeVorgang",
                schema: "dbo",
                table: "PurchaseOrderReceipts");
        }
    }
}

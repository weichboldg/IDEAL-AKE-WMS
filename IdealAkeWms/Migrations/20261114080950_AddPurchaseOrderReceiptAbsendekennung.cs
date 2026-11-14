using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderReceiptAbsendekennung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Absendekennung",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderReceipts_Absendekennung_BelPosId",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                columns: new[] { "Absendekennung", "BelPosId" },
                unique: true,
                filter: "[Absendekennung] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderReceipts_Absendekennung_BelPosId",
                schema: "dbo",
                table: "PurchaseOrderReceipts");

            migrationBuilder.DropColumn(
                name: "Absendekennung",
                schema: "dbo",
                table: "PurchaseOrderReceipts");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddSageKennungen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SageKennung",
                schema: "dbo",
                table: "SageBookingQueueItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SageWeKennung",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SageKennung",
                schema: "dbo",
                table: "SageBookingQueueItems");

            migrationBuilder.DropColumn(
                name: "SageWeKennung",
                schema: "dbo",
                table: "PurchaseOrderReceipts");
        }
    }
}

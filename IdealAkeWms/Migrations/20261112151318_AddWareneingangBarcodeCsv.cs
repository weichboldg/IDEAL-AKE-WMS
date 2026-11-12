using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddWareneingangBarcodeCsv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LieferantName",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WareneingangCsvExporte",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SageWeVorgang = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Versuche = table.Column<int>(type: "int", nullable: false),
                    Fehler = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Datei = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    GeschriebenAm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WareneingangCsvExporte", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderReceipts_Barcode",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_WareneingangCsvExporte_Status",
                schema: "dbo",
                table: "WareneingangCsvExporte",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_WareneingangCsvExporte_SageWeVorgang",
                schema: "dbo",
                table: "WareneingangCsvExporte",
                column: "SageWeVorgang",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WareneingangCsvExporte",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderReceipts_Barcode",
                schema: "dbo",
                table: "PurchaseOrderReceipts");

            migrationBuilder.DropColumn(
                name: "Barcode",
                schema: "dbo",
                table: "PurchaseOrderReceipts");

            migrationBuilder.DropColumn(
                name: "LieferantName",
                schema: "dbo",
                table: "PurchaseOrderReceipts");
        }
    }
}

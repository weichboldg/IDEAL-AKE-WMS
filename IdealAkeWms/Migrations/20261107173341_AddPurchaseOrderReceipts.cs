using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurchaseOrderReceipts",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockMovementId = table.Column<int>(type: "int", nullable: false),
                    BelId = table.Column<int>(type: "int", nullable: false),
                    BelPosId = table.Column<int>(type: "int", nullable: false),
                    Belegnummer = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LieferantKto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OffenLautSageBeimBuchen = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    SagePlatzId = table.Column<int>(type: "int", nullable: true),
                    Lieferart = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderReceipts_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalSchema: "dbo",
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderReceipts_BelPosId",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                column: "BelPosId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderReceipts_StockMovementId",
                schema: "dbo",
                table: "PurchaseOrderReceipts",
                column: "StockMovementId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseOrderReceipts",
                schema: "dbo");
        }
    }
}

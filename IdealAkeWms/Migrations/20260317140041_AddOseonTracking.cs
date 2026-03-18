using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddOseonTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OseonProductionOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OseonId = table.Column<long>(type: "bigint", nullable: false),
                    OseonOrderNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerOrderNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OseonStatus = table.Column<int>(type: "int", nullable: false),
                    ArticleNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WorkplaceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProductionWorkplaceId = table.Column<int>(type: "int", nullable: true),
                    QuantityTarget = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    QuantityActual = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OseonProductionOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OseonProductionOrders_ProductionWorkplaces_ProductionWorkplaceId",
                        column: x => x.ProductionWorkplaceId,
                        principalTable: "ProductionWorkplaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OseonWorkOperations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OseonProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    PositionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OseonStatus = table.Column<int>(type: "int", nullable: false),
                    IsFirstOperation = table.Column<bool>(type: "bit", nullable: false),
                    IsLastOperation = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OseonWorkOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OseonWorkOperations_OseonProductionOrders_OseonProductionOrderId",
                        column: x => x.OseonProductionOrderId,
                        principalTable: "OseonProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OseonProductionOrders_CustomerOrderNumber",
                table: "OseonProductionOrders",
                column: "CustomerOrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_OseonProductionOrders_OseonId",
                table: "OseonProductionOrders",
                column: "OseonId");

            migrationBuilder.CreateIndex(
                name: "IX_OseonProductionOrders_OseonOrderNumber",
                table: "OseonProductionOrders",
                column: "OseonOrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OseonProductionOrders_ProductionWorkplaceId",
                table: "OseonProductionOrders",
                column: "ProductionWorkplaceId");

            migrationBuilder.CreateIndex(
                name: "IX_OseonWorkOperations_OseonProductionOrderId",
                table: "OseonWorkOperations",
                column: "OseonProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OseonWorkOperations_OseonProductionOrderId_PositionNumber",
                table: "OseonWorkOperations",
                columns: new[] { "OseonProductionOrderId", "PositionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OseonWorkOperations");

            migrationBuilder.DropTable(
                name: "OseonProductionOrders");
        }
    }
}

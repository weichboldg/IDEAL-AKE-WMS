using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOperationsPhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanPick",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanReportOperations",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewTracking",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ProductionWorkplaceId",
                table: "ProductionOrders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductionWorkplaceUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionWorkplaceId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionWorkplaceUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionWorkplaceUsers_ProductionWorkplaces_ProductionWorkplaceId",
                        column: x => x.ProductionWorkplaceId,
                        principalTable: "ProductionWorkplaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionWorkplaceUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOperations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    OperationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductionWorkplaceId = table.Column<int>(type: "int", nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    IsReportable = table.Column<bool>(type: "bit", nullable: false),
                    IsExternalSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsReported = table.Column<bool>(type: "bit", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReportedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReportedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExternalSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOperations_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkOperations_ProductionWorkplaces_ProductionWorkplaceId",
                        column: x => x.ProductionWorkplaceId,
                        principalTable: "ProductionWorkplaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_ProductionWorkplaceId",
                table: "ProductionOrders",
                column: "ProductionWorkplaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionWorkplaceUsers_ProductionWorkplaceId_UserId",
                table: "ProductionWorkplaceUsers",
                columns: new[] { "ProductionWorkplaceId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionWorkplaceUsers_UserId",
                table: "ProductionWorkplaceUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOperations_ProductionOrderId",
                table: "WorkOperations",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOperations_ProductionOrderId_Sequence",
                table: "WorkOperations",
                columns: new[] { "ProductionOrderId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOperations_ProductionWorkplaceId",
                table: "WorkOperations",
                column: "ProductionWorkplaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_ProductionWorkplaces_ProductionWorkplaceId",
                table: "ProductionOrders",
                column: "ProductionWorkplaceId",
                principalTable: "ProductionWorkplaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_ProductionWorkplaces_ProductionWorkplaceId",
                table: "ProductionOrders");

            migrationBuilder.DropTable(
                name: "ProductionWorkplaceUsers");

            migrationBuilder.DropTable(
                name: "WorkOperations");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_ProductionWorkplaceId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "CanPick",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanReportOperations",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanViewTracking",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProductionWorkplaceId",
                table: "ProductionOrders");
        }
    }
}

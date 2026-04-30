using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseRequisitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarehouseRequisitions",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionWorkplaceId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    OrderRecipientGroupId = table.Column<int>(type: "int", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedByUserId = table.Column<int>(type: "int", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByUserId = table.Column<int>(type: "int", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledByUserId = table.Column<int>(type: "int", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationEmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseRequisitions_OrderRecipientGroups_OrderRecipientGroupId",
                        column: x => x.OrderRecipientGroupId,
                        principalSchema: "dbo",
                        principalTable: "OrderRecipientGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseRequisitions_ProductionWorkplaces_ProductionWorkplaceId",
                        column: x => x.ProductionWorkplaceId,
                        principalSchema: "dbo",
                        principalTable: "ProductionWorkplaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseRequisitionItems",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseRequisitionId = table.Column<int>(type: "int", nullable: false),
                    ArticleNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ArticleDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    QuantityRequested = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QuantityPicked = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseRequisitionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseRequisitionItems_WarehouseRequisitions_WarehouseRequisitionId",
                        column: x => x.WarehouseRequisitionId,
                        principalSchema: "dbo",
                        principalTable: "WarehouseRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseRequisitionItems_WarehouseRequisitionId_ArticleNumber",
                schema: "dbo",
                table: "WarehouseRequisitionItems",
                columns: new[] { "WarehouseRequisitionId", "ArticleNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseRequisitionItems_WarehouseRequisitionId_Position",
                schema: "dbo",
                table: "WarehouseRequisitionItems",
                columns: new[] { "WarehouseRequisitionId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseRequisitions_OrderRecipientGroupId",
                schema: "dbo",
                table: "WarehouseRequisitions",
                column: "OrderRecipientGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseRequisitions_ProductionWorkplaceId",
                schema: "dbo",
                table: "WarehouseRequisitions",
                column: "ProductionWorkplaceId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseRequisitions_Status",
                schema: "dbo",
                table: "WarehouseRequisitions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseRequisitions_SubmittedAt",
                schema: "dbo",
                table: "WarehouseRequisitions",
                column: "SubmittedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarehouseRequisitionItems",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "WarehouseRequisitions",
                schema: "dbo");
        }
    }
}

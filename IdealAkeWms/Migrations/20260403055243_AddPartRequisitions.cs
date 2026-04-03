using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddPartRequisitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderRecipientGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderRecipientGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArticleGroupRecipientMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleGroup = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrderRecipientGroupId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleGroupRecipientMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleGroupRecipientMappings_OrderRecipientGroups_OrderRecipientGroupId",
                        column: x => x.OrderRecipientGroupId,
                        principalTable: "OrderRecipientGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderRecipientGroupId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderRecipients_OrderRecipientGroups_OrderRecipientGroupId",
                        column: x => x.OrderRecipientGroupId,
                        principalTable: "OrderRecipientGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartRequisitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    ArticleNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ArticleDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ArticleGroup = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Position = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrderRecipientGroupId = table.Column<int>(type: "int", nullable: true),
                    SentToEmails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FulfilledByStockMovementId = table.Column<int>(type: "int", nullable: true),
                    FulfilledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartRequisitions_OrderRecipientGroups_OrderRecipientGroupId",
                        column: x => x.OrderRecipientGroupId,
                        principalTable: "OrderRecipientGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PartRequisitions_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartRequisitions_StockMovements_FulfilledByStockMovementId",
                        column: x => x.FulfilledByStockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleGroupRecipientMappings_ArticleGroup",
                table: "ArticleGroupRecipientMappings",
                column: "ArticleGroup");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleGroupRecipientMappings_OrderRecipientGroupId",
                table: "ArticleGroupRecipientMappings",
                column: "OrderRecipientGroupId");

            migrationBuilder.CreateIndex(
                name: "UX_ArticleGroupRecipientMappings_Group_Recipient",
                table: "ArticleGroupRecipientMappings",
                columns: new[] { "ArticleGroup", "OrderRecipientGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderRecipients_GroupId",
                table: "OrderRecipients",
                column: "OrderRecipientGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PartRequisitions_ArticleNumber",
                table: "PartRequisitions",
                column: "ArticleNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PartRequisitions_EmailSentAt_Status",
                table: "PartRequisitions",
                columns: new[] { "EmailSentAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PartRequisitions_FulfilledByStockMovementId",
                table: "PartRequisitions",
                column: "FulfilledByStockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_PartRequisitions_OrderRecipientGroupId",
                table: "PartRequisitions",
                column: "OrderRecipientGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PartRequisitions_ProductionOrderId",
                table: "PartRequisitions",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PartRequisitions_Status",
                table: "PartRequisitions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleGroupRecipientMappings");

            migrationBuilder.DropTable(
                name: "OrderRecipients");

            migrationBuilder.DropTable(
                name: "PartRequisitions");

            migrationBuilder.DropTable(
                name: "OrderRecipientGroups");
        }
    }
}

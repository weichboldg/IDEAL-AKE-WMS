using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddPickingAreasAndOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PickingAreas",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kuerzel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Reihenfolge = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IstVorkommissionierung = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickingAreas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PickingAreaRules",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PickingAreaId = table.Column<int>(type: "int", nullable: false),
                    RuleType = table.Column<byte>(type: "tinyint", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickingAreaRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickingAreaRules_PickingAreas_PickingAreaId",
                        column: x => x.PickingAreaId,
                        principalSchema: "dbo",
                        principalTable: "PickingAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PickingOrders",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nummer = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Jahr = table.Column<int>(type: "int", nullable: false),
                    LaufendeNummer = table.Column<int>(type: "int", nullable: false),
                    Typ = table.Column<byte>(type: "tinyint", nullable: false),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: true),
                    PickingAreaId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    PickingPriority = table.Column<int>(type: "int", nullable: true),
                    AssignedPickerId = table.Column<int>(type: "int", nullable: true),
                    AssignedPickerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Bemerkung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UrsprungsauftragId = table.Column<int>(type: "int", nullable: true),
                    AbgeschlossenAm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AbgeschlossenVon = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_PickingOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickingOrders_PickingAreas_PickingAreaId",
                        column: x => x.PickingAreaId,
                        principalSchema: "dbo",
                        principalTable: "PickingAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickingOrders_PickingOrders_UrsprungsauftragId",
                        column: x => x.UrsprungsauftragId,
                        principalSchema: "dbo",
                        principalTable: "PickingOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickingOrders_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "dbo",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickingOrders_Users_AssignedPickerId",
                        column: x => x.AssignedPickerId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PickingOrderLines",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PickingOrderId = table.Column<int>(type: "int", nullable: false),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    ArticleNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BomPosition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsBaugruppe = table.Column<bool>(type: "bit", nullable: false),
                    SollMenge = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    IstMenge = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    SourceStorageLocationId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    PickedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PickedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PickedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TransferredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StockMovementId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_PickingOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickingOrderLines_PickingOrders_PickingOrderId",
                        column: x => x.PickingOrderId,
                        principalSchema: "dbo",
                        principalTable: "PickingOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PickingOrderLines_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "dbo",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickingOrderLines_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalSchema: "dbo",
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickingOrderLines_StorageLocations_SourceStorageLocationId",
                        column: x => x.SourceStorageLocationId,
                        principalSchema: "dbo",
                        principalTable: "StorageLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PickingAreaRules_PickingAreaId",
                schema: "dbo",
                table: "PickingAreaRules",
                column: "PickingAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_PickingAreaRules_RuleType_Value",
                schema: "dbo",
                table: "PickingAreaRules",
                columns: new[] { "RuleType", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickingAreas_Kuerzel",
                schema: "dbo",
                table: "PickingAreas",
                column: "Kuerzel",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickingAreas_Name",
                schema: "dbo",
                table: "PickingAreas",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PickingAreas_IsDefault",
                schema: "dbo",
                table: "PickingAreas",
                column: "IsDefault",
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PickingOrderLines_PickingOrderId",
                schema: "dbo",
                table: "PickingOrderLines",
                column: "PickingOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PickingOrderLines_ProductionOrderId_ArticleNumber_BomPosition",
                schema: "dbo",
                table: "PickingOrderLines",
                columns: new[] { "ProductionOrderId", "ArticleNumber", "BomPosition" });

            migrationBuilder.CreateIndex(
                name: "IX_PickingOrderLines_SourceStorageLocationId",
                schema: "dbo",
                table: "PickingOrderLines",
                column: "SourceStorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PickingOrderLines_StockMovementId",
                schema: "dbo",
                table: "PickingOrderLines",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_PickingOrders_AssignedPickerId",
                schema: "dbo",
                table: "PickingOrders",
                column: "AssignedPickerId");

            migrationBuilder.CreateIndex(
                name: "IX_PickingOrders_Jahr_LaufendeNummer",
                schema: "dbo",
                table: "PickingOrders",
                columns: new[] { "Jahr", "LaufendeNummer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickingOrders_Nummer",
                schema: "dbo",
                table: "PickingOrders",
                column: "Nummer",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickingOrders_PickingAreaId",
                schema: "dbo",
                table: "PickingOrders",
                column: "PickingAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_PickingOrders_ProductionOrderId",
                schema: "dbo",
                table: "PickingOrders",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PickingOrders_Status",
                schema: "dbo",
                table: "PickingOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PickingOrders_UrsprungsauftragId",
                schema: "dbo",
                table: "PickingOrders",
                column: "UrsprungsauftragId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PickingAreaRules",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PickingOrderLines",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PickingOrders",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PickingAreas",
                schema: "dbo");
        }
    }
}

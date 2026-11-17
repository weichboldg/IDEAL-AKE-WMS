using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddBdeTeileliste : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BdeTeileliste",
                schema: "dbo",
                table: "ProductionWorkplaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BdeTeilMeldungen",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HauptFA = table.Column<int>(type: "int", nullable: false),
                    VaterFA = table.Column<int>(type: "int", nullable: true),
                    SubFA = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: true),
                    Artnr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Matchcode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Kuerzel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProductionWorkplaceId = table.Column<int>(type: "int", nullable: false),
                    BdeOperatorId = table.Column<int>(type: "int", nullable: false),
                    BdeTerminalId = table.Column<int>(type: "int", nullable: false),
                    GoodQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ScrapQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SollmengeBeiMeldung = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    GemeldetAm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BdeBookingId = table.Column<int>(type: "int", nullable: true),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BdeTeilMeldungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BdeTeilMeldungen_BdeBookings_BdeBookingId",
                        column: x => x.BdeBookingId,
                        principalSchema: "dbo",
                        principalTable: "BdeBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BdeTeilMeldungen_BdeOperators_BdeOperatorId",
                        column: x => x.BdeOperatorId,
                        principalSchema: "dbo",
                        principalTable: "BdeOperators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BdeTeilMeldungen_BdeTerminals_BdeTerminalId",
                        column: x => x.BdeTerminalId,
                        principalSchema: "dbo",
                        principalTable: "BdeTerminals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BdeTeilMeldungen_ProductionWorkplaces_ProductionWorkplaceId",
                        column: x => x.ProductionWorkplaceId,
                        principalSchema: "dbo",
                        principalTable: "ProductionWorkplaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BdeTeilMeldungen_BdeBookingId",
                schema: "dbo",
                table: "BdeTeilMeldungen",
                column: "BdeBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BdeTeilMeldungen_BdeTerminalId",
                schema: "dbo",
                table: "BdeTeilMeldungen",
                column: "BdeTerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_BdeTeilMeldungen_Operator_GemeldetAm",
                schema: "dbo",
                table: "BdeTeilMeldungen",
                columns: new[] { "BdeOperatorId", "GemeldetAm" });

            migrationBuilder.CreateIndex(
                name: "IX_BdeTeilMeldungen_Workplace_GemeldetAm",
                schema: "dbo",
                table: "BdeTeilMeldungen",
                columns: new[] { "ProductionWorkplaceId", "GemeldetAm" });

            migrationBuilder.CreateIndex(
                name: "UX_BdeTeilMeldungen_Schluessel",
                schema: "dbo",
                table: "BdeTeilMeldungen",
                columns: new[] { "HauptFA", "VaterFA", "SubFA", "Position", "Artnr", "Kuerzel" },
                unique: true,
                filter: "[IsCancelled] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BdeTeilMeldungen",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "BdeTeileliste",
                schema: "dbo",
                table: "ProductionWorkplaces");
        }
    }
}

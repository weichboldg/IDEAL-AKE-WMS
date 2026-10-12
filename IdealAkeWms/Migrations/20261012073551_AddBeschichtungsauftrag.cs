using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddBeschichtungsauftrag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Beschichtungsauftraege",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nummer = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Jahr = table.Column<int>(type: "int", nullable: false),
                    LaufendeNummer = table.Column<int>(type: "int", nullable: false),
                    HauptFA = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Dienstleister = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Ral = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartBeschichtung = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BeschichtenRetour = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Bemerkung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UrsprungsauftragId = table.Column<int>(type: "int", nullable: true),
                    UebergebenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UebergebenByUserId = table.Column<int>(type: "int", nullable: true),
                    ErledigtAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErledigtByUserId = table.Column<int>(type: "int", nullable: true),
                    StorniertAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StorniertByUserId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Beschichtungsauftraege", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Beschichtungsauftraege_Beschichtungsauftraege_UrsprungsauftragId",
                        column: x => x.UrsprungsauftragId,
                        principalSchema: "dbo",
                        principalTable: "Beschichtungsauftraege",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BeschichtungsauftragPositionen",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BeschichtungsauftragId = table.Column<int>(type: "int", nullable: false),
                    HauptFA = table.Column<int>(type: "int", nullable: false),
                    VaterFA = table.Column<int>(type: "int", nullable: true),
                    SubFA = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: true),
                    HauptArtnr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Artnr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Matchcode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Menge = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Breite = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Hoehe = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Tiefe = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeschichtungsauftragPositionen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeschichtungsauftragPositionen_Beschichtungsauftraege_BeschichtungsauftragId",
                        column: x => x.BeschichtungsauftragId,
                        principalSchema: "dbo",
                        principalTable: "Beschichtungsauftraege",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beschichtungsauftraege_HauptFA",
                schema: "dbo",
                table: "Beschichtungsauftraege",
                column: "HauptFA");

            migrationBuilder.CreateIndex(
                name: "IX_Beschichtungsauftraege_Jahr_LaufendeNummer",
                schema: "dbo",
                table: "Beschichtungsauftraege",
                columns: new[] { "Jahr", "LaufendeNummer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Beschichtungsauftraege_Nummer",
                schema: "dbo",
                table: "Beschichtungsauftraege",
                column: "Nummer",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Beschichtungsauftraege_Status",
                schema: "dbo",
                table: "Beschichtungsauftraege",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Beschichtungsauftraege_UrsprungsauftragId",
                schema: "dbo",
                table: "Beschichtungsauftraege",
                column: "UrsprungsauftragId");

            migrationBuilder.CreateIndex(
                name: "IX_BeschichtungsauftragPositionen_BeschichtungsauftragId",
                schema: "dbo",
                table: "BeschichtungsauftragPositionen",
                column: "BeschichtungsauftragId");

            migrationBuilder.CreateIndex(
                name: "IX_BeschichtungsauftragPositionen_HauptFA_VaterFA_SubFA_Position",
                schema: "dbo",
                table: "BeschichtungsauftragPositionen",
                columns: new[] { "HauptFA", "VaterFA", "SubFA", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BeschichtungsauftragPositionen",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Beschichtungsauftraege",
                schema: "dbo");
        }
    }
}

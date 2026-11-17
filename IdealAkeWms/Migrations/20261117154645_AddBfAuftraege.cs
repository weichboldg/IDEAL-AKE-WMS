using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddBfAuftraege : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BfAuftraege",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nummer = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BestellerUserId = table.Column<int>(type: "int", nullable: true),
                    BestellerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BestelltAm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrderRecipientGroupId = table.Column<int>(type: "int", nullable: true),
                    EmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FreigegebenAm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FreigegebenVon = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Schachteltermin = table.Column<DateTime>(type: "date", nullable: true),
                    FeTermin = table.Column<DateTime>(type: "date", nullable: true),
                    CsvStatus = table.Column<int>(type: "int", nullable: false),
                    CsvVersuche = table.Column<int>(type: "int", nullable: false),
                    CsvFehler = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CsvDatei = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CsvGeschriebenAm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FertigAm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FertigVon = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StorniertAm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StorniertVon = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StornoGrund = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_BfAuftraege", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BfAuftraege_OrderRecipientGroups_OrderRecipientGroupId",
                        column: x => x.OrderRecipientGroupId,
                        principalSchema: "dbo",
                        principalTable: "OrderRecipientGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BfAuftragPositionen",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BfAuftragId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    ArticleNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Matchcode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Teileart = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Material = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Materialnummer = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FertigungsInfoPN = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Hauptlagerplatz = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Menge = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    SageBestellmenge = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Bemerkung = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Gestrichen = table.Column<bool>(type: "bit", nullable: false),
                    SageFa = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BfAuftragPositionen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BfAuftragPositionen_BfAuftraege_BfAuftragId",
                        column: x => x.BfAuftragId,
                        principalSchema: "dbo",
                        principalTable: "BfAuftraege",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BfAuftraege_CsvStatus",
                schema: "dbo",
                table: "BfAuftraege",
                column: "CsvStatus");

            migrationBuilder.CreateIndex(
                name: "IX_BfAuftraege_OrderRecipientGroupId",
                schema: "dbo",
                table: "BfAuftraege",
                column: "OrderRecipientGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_BfAuftraege_Status",
                schema: "dbo",
                table: "BfAuftraege",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_BfAuftraege_Nummer",
                schema: "dbo",
                table: "BfAuftraege",
                column: "Nummer",
                unique: true,
                filter: "[Nummer] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_BfAuftragPositionen_Auftrag_Position",
                schema: "dbo",
                table: "BfAuftragPositionen",
                columns: new[] { "BfAuftragId", "Position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BfAuftragPositionen",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "BfAuftraege",
                schema: "dbo");
        }
    }
}

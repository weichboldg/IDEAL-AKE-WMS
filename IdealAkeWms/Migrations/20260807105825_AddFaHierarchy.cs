using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddFaHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FaHierarchyNodes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HauptFA = table.Column<int>(type: "int", nullable: false),
                    VaterFA = table.Column<int>(type: "int", nullable: true),
                    SubFA = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: true),
                    HauptArtnr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Artnr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Matchcode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Bezeichnung1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bezeichnung2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Artikelgruppe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Hauptlagerplatz = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BemerkungPN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FertigungsInfoPN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kommissionieren = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Arbeitsbereich = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Arbeitsschritte = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Artikeltyp = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Material = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VMBedarf = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Sollmenge = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Fertigungmenge = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Breite = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Hoehe = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Tiefe = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Beschaffungsartikel = table.Column<bool>(type: "bit", nullable: false),
                    Beschichtet = table.Column<bool>(type: "bit", nullable: false),
                    EKBedarf = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaHierarchyNodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FaHierarchyOrderInfos",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HauptFA = table.Column<int>(type: "int", nullable: false),
                    MontageAbteilung = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ABNr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Pos = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Kunde = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Dienstleister = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RAL = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Bemerkung_Uhrzeit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KO_Termin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FE_Termin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Start_Beschichtung = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Beschichten_Retour = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Neuer_PT_PPS = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Verladetermin_Vsl = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Montagestunden = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Prio = table.Column<int>(type: "int", nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaHierarchyOrderInfos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaHierarchyNodes_HauptFA",
                schema: "dbo",
                table: "FaHierarchyNodes",
                column: "HauptFA");

            migrationBuilder.CreateIndex(
                name: "IX_FaHierarchyNodes_VaterFA",
                schema: "dbo",
                table: "FaHierarchyNodes",
                column: "VaterFA");

            migrationBuilder.CreateIndex(
                name: "IX_FaHierarchyOrderInfos_HauptFA",
                schema: "dbo",
                table: "FaHierarchyOrderInfos",
                column: "HauptFA");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaHierarchyNodes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FaHierarchyOrderInfos",
                schema: "dbo");
        }
    }
}

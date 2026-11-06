using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleSageDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArticleSageDetails",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    Bezeichnung2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Hauptartikelgruppe = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Vaterartikelgruppe = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Hersteller = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HArtikelnummer = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Zeichnungsnummer = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Ersatzartikelnummer = table.Column<string>(type: "nvarchar(31)", maxLength: 31, nullable: true),
                    Langtext = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gewichtseinheit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DezimalstellenLager = table.Column<int>(type: "int", nullable: true),
                    Lagerfuehrung = table.Column<bool>(type: "bit", nullable: true),
                    SagePlatzId = table.Column<int>(type: "int", nullable: true),
                    Entnahmeverfahren = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    Inventurmethode = table.Column<int>(type: "int", nullable: true),
                    Chargenpflicht = table.Column<int>(type: "int", nullable: true),
                    Nachweispflicht = table.Column<int>(type: "int", nullable: true),
                    IstBestellartikel = table.Column<bool>(type: "bit", nullable: true),
                    IstFertigungsartikel = table.Column<bool>(type: "bit", nullable: true),
                    IstUnterbaugruppe = table.Column<bool>(type: "bit", nullable: true),
                    IstErsatzteil = table.Column<bool>(type: "bit", nullable: true),
                    IstVerschleissteil = table.Column<bool>(type: "bit", nullable: true),
                    Aktiv = table.Column<bool>(type: "bit", nullable: true),
                    Stuecklistentyp = table.Column<int>(type: "int", nullable: true),
                    Schwundart = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    Schwund = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    MinimaleLosgroesse = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    MaximaleLosgroesse = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    Laenge = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    Breite = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    Hoehe = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    EANNummer = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    ABCKlasse = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    Gewicht = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    Lagerbestand = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    LagerbestandVerfuegbar = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    LagerbestandOhneDispo = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    Bestellmenge = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    MittlererEK = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Bestellzeitpunkt = table.Column<int>(type: "int", nullable: true),
                    Hauptlieferant = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LetzterLieferant = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Wiederbeschaffungszeit = table.Column<int>(type: "int", nullable: true),
                    UserLagerplatzId = table.Column<int>(type: "int", nullable: true),
                    UserMindestbestand = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleSageDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleSageDetails_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "dbo",
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_ArticleSageDetails_ArticleId",
                schema: "dbo",
                table: "ArticleSageDetails",
                column: "ArticleId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleSageDetails",
                schema: "dbo");
        }
    }
}

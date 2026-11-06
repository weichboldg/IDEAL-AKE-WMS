using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionOrderPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductionOrderPositions",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BelPosID = table.Column<int>(type: "int", nullable: false),
                    BelID = table.Column<int>(type: "int", nullable: false),
                    StrukturID = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: true),
                    RessourceTyp = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RessourceNummer = table.Column<string>(type: "nvarchar(31)", maxLength: 31, nullable: true),
                    ArbeitsgangNummer = table.Column<string>(type: "nvarchar(31)", maxLength: 31, nullable: true),
                    BelIDNachfolger = table.Column<int>(type: "int", nullable: true),
                    Menge = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Sollmenge = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Istmenge = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    SollRuestzeit = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IstRuestzeit = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    SollStueckzeit = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IstStueckzeit = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: true),
                    PlatzID = table.Column<int>(type: "int", nullable: true),
                    LagerplatzKurzbezeichnung = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Schwund = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Bedarfstermin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderPositions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SageFaStates",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Mandant = table.Column<int>(type: "int", nullable: false),
                    BelID = table.Column<int>(type: "int", nullable: false),
                    StrukturID = table.Column<int>(type: "int", nullable: false),
                    IstEingelastet = table.Column<bool>(type: "bit", nullable: false),
                    IstBegonnen = table.Column<bool>(type: "bit", nullable: false),
                    IstBeendet = table.Column<bool>(type: "bit", nullable: false),
                    IstGesperrt = table.Column<bool>(type: "bit", nullable: false),
                    IstReaktiviert = table.Column<bool>(type: "bit", nullable: true),
                    MengeGutRueckgemeldet = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    MengeGutFertiggemeldet = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SageFaStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderPositions_BelID",
                schema: "dbo",
                table: "ProductionOrderPositions",
                column: "BelID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderPositions_StrukturID",
                schema: "dbo",
                table: "ProductionOrderPositions",
                column: "StrukturID");

            migrationBuilder.CreateIndex(
                name: "UQ_ProductionOrderPositions_BelPosID",
                schema: "dbo",
                table: "ProductionOrderPositions",
                column: "BelPosID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_SageFaStates_Mandant_BelID",
                schema: "dbo",
                table: "SageFaStates",
                columns: new[] { "Mandant", "BelID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionOrderPositions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SageFaStates",
                schema: "dbo");
        }
    }
}

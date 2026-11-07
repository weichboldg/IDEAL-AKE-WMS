using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddSagePurchaseOrderLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SagePurchaseOrderLines",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BelPosId = table.Column<int>(type: "int", nullable: false),
                    BelId = table.Column<int>(type: "int", nullable: false),
                    Belegkennzeichen = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Belegnummer = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LieferantKto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LieferantName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LieferantGesperrt = table.Column<bool>(type: "bit", nullable: false),
                    CreditorId = table.Column<int>(type: "int", nullable: true),
                    Artikelnummer = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: true),
                    Bezeichnung1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Bezeichnung2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MengeBestellt = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MengeGeliefert = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MengeOffen = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Mengeneinheit = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    MengeBasiseinheit = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Lagermengeneinheit = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    EinheitAbweichend = table.Column<bool>(type: "bit", nullable: false),
                    SagePlatzId = table.Column<int>(type: "int", nullable: true),
                    SagePlatzKurzbezeichnung = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StorageLocationId = table.Column<int>(type: "int", nullable: true),
                    Liefertermin = table.Column<DateTime>(type: "date", nullable: true),
                    Chargenpflicht = table.Column<bool>(type: "bit", nullable: false),
                    Nachweispflicht = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SagePurchaseOrderLines", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SagePurchaseOrderLines_ArticleId",
                schema: "dbo",
                table: "SagePurchaseOrderLines",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_SagePurchaseOrderLines_Belegnummer",
                schema: "dbo",
                table: "SagePurchaseOrderLines",
                column: "Belegnummer");

            migrationBuilder.CreateIndex(
                name: "IX_SagePurchaseOrderLines_BelId",
                schema: "dbo",
                table: "SagePurchaseOrderLines",
                column: "BelId");

            migrationBuilder.CreateIndex(
                name: "IX_SagePurchaseOrderLines_BelPosId",
                schema: "dbo",
                table: "SagePurchaseOrderLines",
                column: "BelPosId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SagePurchaseOrderLines_Liefertermin",
                schema: "dbo",
                table: "SagePurchaseOrderLines",
                column: "Liefertermin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SagePurchaseOrderLines",
                schema: "dbo");
        }
    }
}

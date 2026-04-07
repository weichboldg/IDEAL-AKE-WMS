using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddBomCacheAndCoatingDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasCoatingParts",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCoatingDone",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CachedBomHeaders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Artikelnummer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CachedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedBomHeaders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CachedBomItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CachedBomHeaderId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Baugruppe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Ressourcenummer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Bezeichnung1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Bezeichnung2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Menge = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Beschaffungsartikel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Artikelgruppe = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedBomItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CachedBomItems_CachedBomHeaders_CachedBomHeaderId",
                        column: x => x.CachedBomHeaderId,
                        principalTable: "CachedBomHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CachedBomHeaders_Artikelnummer",
                table: "CachedBomHeaders",
                column: "Artikelnummer",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CachedBomItems_CachedBomHeaderId",
                table: "CachedBomItems",
                column: "CachedBomHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedBomItems_Ressourcenummer",
                table: "CachedBomItems",
                column: "Ressourcenummer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedBomItems");

            migrationBuilder.DropTable(
                name: "CachedBomHeaders");

            migrationBuilder.DropColumn(
                name: "HasCoatingParts",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "IsCoatingDone",
                table: "ProductionOrders");
        }
    }
}

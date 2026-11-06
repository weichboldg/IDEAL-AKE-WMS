using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleSuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArticleSuppliers",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    CreditorId = table.Column<int>(type: "int", nullable: false),
                    Artikelnummer = table.Column<string>(type: "nvarchar(31)", maxLength: 31, nullable: false),
                    SageBestellnummer = table.Column<string>(type: "nvarchar(31)", maxLength: 31, nullable: true),
                    Bezeichnung1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Bezeichnung2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Mindestbestellmenge = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    Einkaufsmengeneinheit = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    UmrechnungsfaktorEK = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    Wiederbeschaffungszeit = table.Column<int>(type: "int", nullable: true),
                    Einzelpreis = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IstHauptlieferant = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleSuppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleSuppliers_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "dbo",
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleSuppliers_Creditors_CreditorId",
                        column: x => x.CreditorId,
                        principalSchema: "dbo",
                        principalTable: "Creditors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleSuppliers_ArticleId_CreditorId",
                schema: "dbo",
                table: "ArticleSuppliers",
                columns: new[] { "ArticleId", "CreditorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleSuppliers_CreditorId",
                schema: "dbo",
                table: "ArticleSuppliers",
                column: "CreditorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleSuppliers",
                schema: "dbo");
        }
    }
}

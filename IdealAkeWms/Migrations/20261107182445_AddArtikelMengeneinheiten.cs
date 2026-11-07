using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddArtikelMengeneinheiten : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DezimalstellenEK",
                schema: "dbo",
                table: "ArticleSuppliers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UmrechnungsfaktorVPEK",
                schema: "dbo",
                table: "ArticleSuppliers",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Basismengeneinheit",
                schema: "dbo",
                table: "ArticleSageDetails",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DezimalstellenBasis",
                schema: "dbo",
                table: "ArticleSageDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DezimalstellenVK",
                schema: "dbo",
                table: "ArticleSageDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Lagermengeneinheit",
                schema: "dbo",
                table: "ArticleSageDetails",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UmrechnungsFaktorLME",
                schema: "dbo",
                table: "ArticleSageDetails",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UmrechnungsfaktorVK",
                schema: "dbo",
                table: "ArticleSageDetails",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UmrechnungsfaktorVPVK",
                schema: "dbo",
                table: "ArticleSageDetails",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Verkaufsmengeneinheit",
                schema: "dbo",
                table: "ArticleSageDetails",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerpackungseinheitVK",
                schema: "dbo",
                table: "ArticleSageDetails",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DezimalstellenEK",
                schema: "dbo",
                table: "ArticleSuppliers");

            migrationBuilder.DropColumn(
                name: "UmrechnungsfaktorVPEK",
                schema: "dbo",
                table: "ArticleSuppliers");

            migrationBuilder.DropColumn(
                name: "Basismengeneinheit",
                schema: "dbo",
                table: "ArticleSageDetails");

            migrationBuilder.DropColumn(
                name: "DezimalstellenBasis",
                schema: "dbo",
                table: "ArticleSageDetails");

            migrationBuilder.DropColumn(
                name: "DezimalstellenVK",
                schema: "dbo",
                table: "ArticleSageDetails");

            migrationBuilder.DropColumn(
                name: "Lagermengeneinheit",
                schema: "dbo",
                table: "ArticleSageDetails");

            migrationBuilder.DropColumn(
                name: "UmrechnungsFaktorLME",
                schema: "dbo",
                table: "ArticleSageDetails");

            migrationBuilder.DropColumn(
                name: "UmrechnungsfaktorVK",
                schema: "dbo",
                table: "ArticleSageDetails");

            migrationBuilder.DropColumn(
                name: "UmrechnungsfaktorVPVK",
                schema: "dbo",
                table: "ArticleSageDetails");

            migrationBuilder.DropColumn(
                name: "Verkaufsmengeneinheit",
                schema: "dbo",
                table: "ArticleSageDetails");

            migrationBuilder.DropColumn(
                name: "VerpackungseinheitVK",
                schema: "dbo",
                table: "ArticleSageDetails");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleSupplierLetzteBestellung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LetzteBestellmenge",
                schema: "dbo",
                table: "ArticleSuppliers",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LetzteBestellungAm",
                schema: "dbo",
                table: "ArticleSuppliers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LetzteBestellungBeleg",
                schema: "dbo",
                table: "ArticleSuppliers",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LetzteBestellmenge",
                schema: "dbo",
                table: "ArticleSuppliers");

            migrationBuilder.DropColumn(
                name: "LetzteBestellungAm",
                schema: "dbo",
                table: "ArticleSuppliers");

            migrationBuilder.DropColumn(
                name: "LetzteBestellungBeleg",
                schema: "dbo",
                table: "ArticleSuppliers");
        }
    }
}

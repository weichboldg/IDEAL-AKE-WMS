using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionWorkplaceSageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArbeitsschrittCode",
                schema: "dbo",
                table: "ProductionWorkplaces",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SageArbeitsplatznummer",
                schema: "dbo",
                table: "ProductionWorkplaces",
                type: "nvarchar(31)",
                maxLength: 31,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionWorkplaces_SageArbeitsplatznummer",
                schema: "dbo",
                table: "ProductionWorkplaces",
                column: "SageArbeitsplatznummer",
                unique: true,
                filter: "[SageArbeitsplatznummer] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductionWorkplaces_SageArbeitsplatznummer",
                schema: "dbo",
                table: "ProductionWorkplaces");

            migrationBuilder.DropColumn(
                name: "ArbeitsschrittCode",
                schema: "dbo",
                table: "ProductionWorkplaces");

            migrationBuilder.DropColumn(
                name: "SageArbeitsplatznummer",
                schema: "dbo",
                table: "ProductionWorkplaces");
        }
    }
}

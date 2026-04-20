using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddBdeWerkbankSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BdeAktiv",
                table: "ProductionWorkplaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BdeDefaultArbeitsgang",
                table: "ProductionWorkplaces",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BdeAktiv",
                table: "ProductionWorkplaces");

            migrationBuilder.DropColumn(
                name: "BdeDefaultArbeitsgang",
                table: "ProductionWorkplaces");
        }
    }
}

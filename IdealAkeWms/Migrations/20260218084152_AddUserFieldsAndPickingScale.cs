using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFieldsAndPickingScale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultFilterArtikelgruppe",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultFilterBeschaffung",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasMasterDataAccess",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPickingTransport",
                table: "StorageLocations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultFilterArtikelgruppe",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DefaultFilterBeschaffung",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HasMasterDataAccess",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsPickingTransport",
                table: "StorageLocations");
        }
    }
}

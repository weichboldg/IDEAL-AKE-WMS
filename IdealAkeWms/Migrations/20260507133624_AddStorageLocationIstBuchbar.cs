using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddStorageLocationIstBuchbar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IstBuchbar",
                schema: "dbo",
                table: "StorageLocations",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_IstBuchbar",
                schema: "dbo",
                table: "StorageLocations",
                column: "IstBuchbar");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StorageLocations_IstBuchbar",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "IstBuchbar",
                schema: "dbo",
                table: "StorageLocations");
        }
    }
}

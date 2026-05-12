using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddStorageLocationSyncFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "dbo",
                table: "StorageLocations",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                schema: "dbo",
                table: "StorageLocations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_IsActive",
                schema: "dbo",
                table: "StorageLocations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_Source",
                schema: "dbo",
                table: "StorageLocations",
                column: "Source");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StorageLocations_IsActive",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropIndex(
                name: "IX_StorageLocations_Source",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "dbo",
                table: "StorageLocations");
        }
    }
}

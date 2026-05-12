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

            // Existing Sage-records markieren als nicht buchbar — User schaltet manuell frei.
            // Manual-records bleiben default=true.
            migrationBuilder.Sql("UPDATE [dbo].[StorageLocations] SET [IstBuchbar] = 0 WHERE [Source] = 'Sage';");

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

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddStorageLocationSageLagerbuchung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SageBuchungErlaubt",
                schema: "dbo",
                table: "StorageLocations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SageLagerkennung",
                schema: "dbo",
                table: "StorageLocations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SageLagerplatzId",
                schema: "dbo",
                table: "StorageLocations",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SageBuchungErlaubt",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "SageLagerkennung",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "SageLagerplatzId",
                schema: "dbo",
                table: "StorageLocations");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddArticlePrimaryStorageLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrimaryStorageLocationId",
                schema: "dbo",
                table: "Articles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SagePrimaryStorageLocation",
                schema: "dbo",
                table: "Articles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_PrimaryStorageLocationId",
                schema: "dbo",
                table: "Articles",
                column: "PrimaryStorageLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_StorageLocations_PrimaryStorageLocationId",
                schema: "dbo",
                table: "Articles",
                column: "PrimaryStorageLocationId",
                principalSchema: "dbo",
                principalTable: "StorageLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_StorageLocations_PrimaryStorageLocationId",
                schema: "dbo",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_PrimaryStorageLocationId",
                schema: "dbo",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "PrimaryStorageLocationId",
                schema: "dbo",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "SagePrimaryStorageLocation",
                schema: "dbo",
                table: "Articles");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDefaultWorkplace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultWorkplaceId",
                schema: "dbo",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DefaultWorkplaceId",
                schema: "dbo",
                table: "Users",
                column: "DefaultWorkplaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ProductionWorkplaces_DefaultWorkplaceId",
                schema: "dbo",
                table: "Users",
                column: "DefaultWorkplaceId",
                principalSchema: "dbo",
                principalTable: "ProductionWorkplaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_ProductionWorkplaces_DefaultWorkplaceId",
                schema: "dbo",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DefaultWorkplaceId",
                schema: "dbo",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DefaultWorkplaceId",
                schema: "dbo",
                table: "Users");
        }
    }
}

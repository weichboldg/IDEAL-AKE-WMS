using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddWindowsUserNameDropAdGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdGroup",
                schema: "dbo",
                table: "Roles");

            migrationBuilder.AddColumn<string>(
                name: "WindowsUserName",
                schema: "dbo",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_WindowsUserName",
                schema: "dbo",
                table: "Users",
                column: "WindowsUserName",
                unique: true,
                filter: "[WindowsUserName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Users_WindowsUserName",
                schema: "dbo",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WindowsUserName",
                schema: "dbo",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "AdGroup",
                schema: "dbo",
                table: "Roles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}

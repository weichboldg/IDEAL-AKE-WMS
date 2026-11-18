using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddUserKuerzelUndBfBestellerKuerzel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kuerzel",
                schema: "dbo",
                table: "Users",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BestellerKuerzel",
                schema: "dbo",
                table: "BfAuftraege",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Kuerzel",
                schema: "dbo",
                table: "Users",
                column: "Kuerzel",
                unique: true,
                filter: "[Kuerzel] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Users_Kuerzel",
                schema: "dbo",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Kuerzel",
                schema: "dbo",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BestellerKuerzel",
                schema: "dbo",
                table: "BfAuftraege");
        }
    }
}

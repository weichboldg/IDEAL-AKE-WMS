using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationConfigSageArbeitsgangnummer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SageArbeitsgangnummer",
                schema: "dbo",
                table: "OperationConfigs",
                type: "nvarchar(31)",
                maxLength: 31,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SageArbeitsgangnummerManuell",
                schema: "dbo",
                table: "OperationConfigs",
                type: "nvarchar(31)",
                maxLength: 31,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SageArbeitsgangnummer",
                schema: "dbo",
                table: "OperationConfigs");

            migrationBuilder.DropColumn(
                name: "SageArbeitsgangnummerManuell",
                schema: "dbo",
                table: "OperationConfigs");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionWorkplaceSdataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SageBdeTerminalId",
                schema: "dbo",
                table: "ProductionWorkplaces",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SageKostenstelle",
                schema: "dbo",
                table: "ProductionWorkplaces",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SageSchichtplannummer",
                schema: "dbo",
                table: "ProductionWorkplaces",
                type: "nvarchar(31)",
                maxLength: 31,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SageBdeTerminalId",
                schema: "dbo",
                table: "ProductionWorkplaces");

            migrationBuilder.DropColumn(
                name: "SageKostenstelle",
                schema: "dbo",
                table: "ProductionWorkplaces");

            migrationBuilder.DropColumn(
                name: "SageSchichtplannummer",
                schema: "dbo",
                table: "ProductionWorkplaces");
        }
    }
}

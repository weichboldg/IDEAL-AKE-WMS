using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BdeNurAgFertigmeldung",
                schema: "dbo",
                table: "ProductionWorkplaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SageMissingSince",
                schema: "dbo",
                table: "ProductionWorkplaces",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BdeNurAgFertigmeldung",
                schema: "dbo",
                table: "ProductionWorkplaces");

            migrationBuilder.DropColumn(
                name: "SageMissingSince",
                schema: "dbo",
                table: "ProductionWorkplaces");
        }
    }
}

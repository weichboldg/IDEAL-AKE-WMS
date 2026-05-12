using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionOrderAssemblyFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasCooling",
                schema: "dbo",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasDoors",
                schema: "dbo",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasElectric",
                schema: "dbo",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasFan",
                schema: "dbo",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasSuperstructure",
                schema: "dbo",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasCooling",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "HasDoors",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "HasElectric",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "HasFan",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "HasSuperstructure",
                schema: "dbo",
                table: "ProductionOrders");
        }
    }
}

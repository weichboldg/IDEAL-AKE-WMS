using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddPickerAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPicker",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AssignedPickerId",
                table: "ProductionOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedPickerName",
                table: "ProductionOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_AssignedPickerId",
                table: "ProductionOrders",
                column: "AssignedPickerId",
                filter: "[AssignedPickerId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_Users_AssignedPickerId",
                table: "ProductionOrders",
                column: "AssignedPickerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_Users_AssignedPickerId",
                table: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_AssignedPickerId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "IsPicker",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssignedPickerId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "AssignedPickerName",
                table: "ProductionOrders");
        }
    }
}

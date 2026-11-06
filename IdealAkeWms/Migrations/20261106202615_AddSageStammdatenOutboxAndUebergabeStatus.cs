using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddSageStammdatenOutboxAndUebergabeStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SageUebergabeAm",
                schema: "dbo",
                table: "StorageLocations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SageUebergabeFehler",
                schema: "dbo",
                table: "StorageLocations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SageUebergabeStatus",
                schema: "dbo",
                table: "StorageLocations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SageUebergabeAm",
                schema: "dbo",
                table: "BdeOperators",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SageUebergabeFehler",
                schema: "dbo",
                table: "BdeOperators",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SageUebergabeStatus",
                schema: "dbo",
                table: "BdeOperators",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                schema: "dbo",
                table: "BdeOperators",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.CreateTable(
                name: "SageStammdatenOutbox",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SageStammdatenOutbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SageStammdatenOutbox_EntityType_EntityKey",
                schema: "dbo",
                table: "SageStammdatenOutbox",
                columns: new[] { "EntityType", "EntityKey" });

            migrationBuilder.CreateIndex(
                name: "IX_SageStammdatenOutbox_Status",
                schema: "dbo",
                table: "SageStammdatenOutbox",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SageStammdatenOutbox",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "SageUebergabeAm",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "SageUebergabeFehler",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "SageUebergabeStatus",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "SageUebergabeAm",
                schema: "dbo",
                table: "BdeOperators");

            migrationBuilder.DropColumn(
                name: "SageUebergabeFehler",
                schema: "dbo",
                table: "BdeOperators");

            migrationBuilder.DropColumn(
                name: "SageUebergabeStatus",
                schema: "dbo",
                table: "BdeOperators");

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "dbo",
                table: "BdeOperators");
        }
    }
}

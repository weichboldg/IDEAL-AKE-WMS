using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddStorageAreasAndStorageLocationSageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SageBreite",
                schema: "dbo",
                table: "StorageLocations",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SageHoehe",
                schema: "dbo",
                table: "StorageLocations",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SageIstGesperrt",
                schema: "dbo",
                table: "StorageLocations",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SageIstGesperrtInventur",
                schema: "dbo",
                table: "StorageLocations",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SageLaenge",
                schema: "dbo",
                table: "StorageLocations",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SageTragkraft",
                schema: "dbo",
                table: "StorageLocations",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SageVolumen",
                schema: "dbo",
                table: "StorageLocations",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StorageAreaId",
                schema: "dbo",
                table: "StorageLocations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StorageAreas",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Lagerkennung = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Bezeichnung = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Lagertechnik = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IstDispoLager = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SageMissingSince = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageAreas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_StorageAreaId",
                schema: "dbo",
                table: "StorageLocations",
                column: "StorageAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageAreas_Lagerkennung",
                schema: "dbo",
                table: "StorageAreas",
                column: "Lagerkennung",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StorageLocations_StorageAreas_StorageAreaId",
                schema: "dbo",
                table: "StorageLocations",
                column: "StorageAreaId",
                principalSchema: "dbo",
                principalTable: "StorageAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StorageLocations_StorageAreas_StorageAreaId",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropTable(
                name: "StorageAreas",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "IX_StorageLocations_StorageAreaId",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "SageBreite",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "SageHoehe",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "SageIstGesperrt",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "SageIstGesperrtInventur",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "SageLaenge",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "SageTragkraft",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "SageVolumen",
                schema: "dbo",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "StorageAreaId",
                schema: "dbo",
                table: "StorageLocations");
        }
    }
}

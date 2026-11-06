using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddSagePpsMitarbeiterAndStillstandsgruende : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SageMitarbeiternummer",
                schema: "dbo",
                table: "BdeOperators",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SagePpsMitarbeiter",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nummer = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Matchcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Vorname = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    Nachname = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    Gruppe = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Kartennummer = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ExterneNummer = table.Column<int>(type: "int", nullable: true),
                    Aktiv = table.Column<bool>(type: "bit", nullable: true),
                    Schichtplannummer = table.Column<string>(type: "nvarchar(31)", maxLength: 31, nullable: true),
                    Kostenstelle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SagePpsMitarbeiter", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stillstandsgruende",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nummer = table.Column<string>(type: "nvarchar(31)", maxLength: 31, nullable: false),
                    Bezeichnung = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_Stillstandsgruende", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BdeOperators_SageMitarbeiternummer",
                schema: "dbo",
                table: "BdeOperators",
                column: "SageMitarbeiternummer",
                unique: true,
                filter: "[SageMitarbeiternummer] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SagePpsMitarbeiter_Nummer",
                schema: "dbo",
                table: "SagePpsMitarbeiter",
                column: "Nummer",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stillstandsgruende_Nummer",
                schema: "dbo",
                table: "Stillstandsgruende",
                column: "Nummer",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SagePpsMitarbeiter",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Stillstandsgruende",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "IX_BdeOperators_SageMitarbeiternummer",
                schema: "dbo",
                table: "BdeOperators");

            migrationBuilder.DropColumn(
                name: "SageMitarbeiternummer",
                schema: "dbo",
                table: "BdeOperators");
        }
    }
}

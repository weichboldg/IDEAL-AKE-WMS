using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationConfigSourceAndSageMissingSince : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SageMissingSince",
                schema: "dbo",
                table: "OperationConfigs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                schema: "dbo",
                table: "OperationConfigs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual");

            // Bestandsbefuellung (eigener Batch, referenziert die Spalte von oben): Seed/Altbestand -> Fremdsystem, uebrige bleiben
            // Manual. Wortgleich in SQL/96 und im FreshInstall-EXEC (Drift-Test). Spec 2026-10-16 FA 6, Antwort 7.
            migrationBuilder.Sql(@"UPDATE [dbo].[OperationConfigs] SET [Source] = N'ExternalSystem' WHERE [CreatedBy] = N'system' AND [Source] = N'Manual';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SageMissingSince",
                schema: "dbo",
                table: "OperationConfigs");

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "dbo",
                table: "OperationConfigs");
        }
    }
}

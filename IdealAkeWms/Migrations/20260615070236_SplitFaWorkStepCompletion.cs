using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class SplitFaWorkStepCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSpecComplete",
                schema: "dbo",
                table: "FaWorkSteps",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SpecCompletedAt",
                schema: "dbo",
                table: "FaWorkSteps",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecCompletedBy",
                schema: "dbo",
                table: "FaWorkSteps",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // Altes IsCompleted war semantisch "Spec fertig" (v1.13-/Konvertierungs-Herkunft).
            // -> nach IsSpecComplete uebernehmen, Arbeit-erledigt frisch starten.
            migrationBuilder.Sql(@"
UPDATE dbo.FaWorkSteps
SET IsSpecComplete = IsCompleted,
    SpecCompletedAt = CompletedAt,
    SpecCompletedBy = CompletedBy,
    IsCompleted = 0,
    CompletedAt = NULL,
    CompletedBy = NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSpecComplete",
                schema: "dbo",
                table: "FaWorkSteps");

            migrationBuilder.DropColumn(
                name: "SpecCompletedAt",
                schema: "dbo",
                table: "FaWorkSteps");

            migrationBuilder.DropColumn(
                name: "SpecCompletedBy",
                schema: "dbo",
                table: "FaWorkSteps");
        }
    }
}

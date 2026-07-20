using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceFaWorkStepIsCompletedWithStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "dbo",
                table: "FaWorkSteps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE [dbo].[FaWorkSteps]
                SET [Status] = CASE WHEN [IsCompleted] = 1 THEN 2 ELSE 0 END;
            ");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                schema: "dbo",
                table: "FaWorkSteps");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                schema: "dbo",
                table: "FaWorkSteps",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE [dbo].[FaWorkSteps]
                SET [IsCompleted] = CASE WHEN [Status] = 2 THEN 1 ELSE 0 END;
            ");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "dbo",
                table: "FaWorkSteps");
        }
    }
}

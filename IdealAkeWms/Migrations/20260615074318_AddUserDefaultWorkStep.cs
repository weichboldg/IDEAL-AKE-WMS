using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDefaultWorkStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultWorkStepId",
                schema: "dbo",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DefaultWorkStepId",
                schema: "dbo",
                table: "Users",
                column: "DefaultWorkStepId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_WorkSteps_DefaultWorkStepId",
                schema: "dbo",
                table: "Users",
                column: "DefaultWorkStepId",
                principalSchema: "dbo",
                principalTable: "WorkSteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_WorkSteps_DefaultWorkStepId",
                schema: "dbo",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DefaultWorkStepId",
                schema: "dbo",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DefaultWorkStepId",
                schema: "dbo",
                table: "Users");
        }
    }
}

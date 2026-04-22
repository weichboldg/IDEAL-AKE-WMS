using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class ExtendStatusEndedCheckForResumed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BdeBookings_StatusEnded",
                schema: "dbo",
                table: "BdeBookings");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BdeBookings_StatusEnded",
                schema: "dbo",
                table: "BdeBookings",
                sql: "([Status] = 1 AND [EndedAt] IS NULL) OR ([Status] IN (2,3,4) AND [EndedAt] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BdeBookings_StatusEnded",
                schema: "dbo",
                table: "BdeBookings");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BdeBookings_StatusEnded",
                schema: "dbo",
                table: "BdeBookings",
                sql: "([Status] = 1 AND [EndedAt] IS NULL) OR ([Status] IN (2,3) AND [EndedAt] IS NOT NULL)");
        }
    }
}

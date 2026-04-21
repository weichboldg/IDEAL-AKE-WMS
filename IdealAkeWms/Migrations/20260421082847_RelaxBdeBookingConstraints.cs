using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class RelaxBdeBookingConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BdeBookings_BdeOperatorId_Active",
                schema: "dbo",
                table: "BdeBookings");

            migrationBuilder.DropIndex(
                name: "IX_BdeBookings_WorkOperationId_Active",
                schema: "dbo",
                table: "BdeBookings");

            migrationBuilder.CreateIndex(
                name: "IX_BdeBookings_BdeOperatorId_Active",
                schema: "dbo",
                table: "BdeBookings",
                column: "BdeOperatorId",
                filter: "[EndedAt] IS NULL AND [IsCancelled] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BdeBookings_WorkOperationId_Active",
                schema: "dbo",
                table: "BdeBookings",
                column: "WorkOperationId",
                filter: "[EndedAt] IS NULL AND [IsCancelled] = 0 AND [WorkOperationId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BdeBookings_BdeOperatorId_Active",
                schema: "dbo",
                table: "BdeBookings");

            migrationBuilder.DropIndex(
                name: "IX_BdeBookings_WorkOperationId_Active",
                schema: "dbo",
                table: "BdeBookings");

            migrationBuilder.CreateIndex(
                name: "IX_BdeBookings_BdeOperatorId_Active",
                schema: "dbo",
                table: "BdeBookings",
                column: "BdeOperatorId",
                unique: true,
                filter: "[EndedAt] IS NULL AND [IsCancelled] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BdeBookings_WorkOperationId_Active",
                schema: "dbo",
                table: "BdeBookings",
                column: "WorkOperationId",
                unique: true,
                filter: "[EndedAt] IS NULL AND [IsCancelled] = 0 AND [WorkOperationId] IS NOT NULL");
        }
    }
}

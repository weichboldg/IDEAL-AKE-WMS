using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddBdeShiftCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BdeBookings_StatusEnded",
                schema: "dbo",
                table: "BdeBookings");

            migrationBuilder.AddColumn<bool>(
                name: "BdeUseCustomShiftPlan",
                schema: "dbo",
                table: "ProductionWorkplaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte>(
                name: "Source",
                schema: "dbo",
                table: "Holidays",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.CreateTable(
                name: "BdeShifts",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ProductionWorkplaceId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BdeShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BdeShifts_ProductionWorkplaces_ProductionWorkplaceId",
                        column: x => x.ProductionWorkplaceId,
                        principalSchema: "dbo",
                        principalTable: "ProductionWorkplaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_BdeBookings_StatusEnded",
                schema: "dbo",
                table: "BdeBookings",
                sql: "([Status] = 1 AND [EndedAt] IS NULL) OR ([Status] IN (2,3,4,5) AND [EndedAt] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_BdeShifts_Workplace_Day",
                schema: "dbo",
                table: "BdeShifts",
                columns: new[] { "ProductionWorkplaceId", "DayOfWeek" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BdeShifts",
                schema: "dbo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BdeBookings_StatusEnded",
                schema: "dbo",
                table: "BdeBookings");

            migrationBuilder.DropColumn(
                name: "BdeUseCustomShiftPlan",
                schema: "dbo",
                table: "ProductionWorkplaces");

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "dbo",
                table: "Holidays");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BdeBookings_StatusEnded",
                schema: "dbo",
                table: "BdeBookings",
                sql: "([Status] = 1 AND [EndedAt] IS NULL) OR ([Status] IN (2,3,4) AND [EndedAt] IS NOT NULL)");
        }
    }
}

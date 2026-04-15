using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddBde : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BdeActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BdeActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BdeOperators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonnelNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BdeOperators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BdeOperators_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BdeTerminals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DefaultProductionWorkplaceId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BdeTerminals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BdeTerminals_ProductionWorkplaces_DefaultProductionWorkplaceId",
                        column: x => x.DefaultProductionWorkplaceId,
                        principalTable: "ProductionWorkplaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BdeTerminals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BdeBookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BdeOperatorId = table.Column<int>(type: "int", nullable: false),
                    ProductionWorkplaceId = table.Column<int>(type: "int", nullable: false),
                    BdeTerminalId = table.Column<int>(type: "int", nullable: false),
                    WorkOperationId = table.Column<int>(type: "int", nullable: true),
                    BdeActivityId = table.Column<int>(type: "int", nullable: true),
                    BookingType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ParentBookingId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BdeBookings", x => x.Id);
                    table.CheckConstraint("CK_BdeBookings_StatusEnded", "([Status] = 1 AND [EndedAt] IS NULL) OR ([Status] IN (2,3) AND [EndedAt] IS NOT NULL)");
                    table.CheckConstraint("CK_BdeBookings_Target", "([WorkOperationId] IS NOT NULL AND [BdeActivityId] IS NULL) OR ([WorkOperationId] IS NULL AND [BdeActivityId] IS NOT NULL)");
                    table.CheckConstraint("CK_BdeBookings_TypeTarget", "([BookingType] = 3 AND [BdeActivityId] IS NOT NULL) OR ([BookingType] IN (1,2) AND [WorkOperationId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_BdeBookings_BdeActivities_BdeActivityId",
                        column: x => x.BdeActivityId,
                        principalTable: "BdeActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BdeBookings_BdeBookings_ParentBookingId",
                        column: x => x.ParentBookingId,
                        principalTable: "BdeBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BdeBookings_BdeOperators_BdeOperatorId",
                        column: x => x.BdeOperatorId,
                        principalTable: "BdeOperators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BdeBookings_BdeTerminals_BdeTerminalId",
                        column: x => x.BdeTerminalId,
                        principalTable: "BdeTerminals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BdeBookings_ProductionWorkplaces_ProductionWorkplaceId",
                        column: x => x.ProductionWorkplaceId,
                        principalTable: "ProductionWorkplaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BdeBookings_WorkOperations_WorkOperationId",
                        column: x => x.WorkOperationId,
                        principalTable: "WorkOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BdeBookingQuantities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BdeBookingId = table.Column<int>(type: "int", nullable: false),
                    BdeOperatorId = table.Column<int>(type: "int", nullable: false),
                    GoodQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ScrapQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsFinal = table.Column<bool>(type: "bit", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BdeBookingQuantities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BdeBookingQuantities_BdeBookings_BdeBookingId",
                        column: x => x.BdeBookingId,
                        principalTable: "BdeBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BdeBookingQuantities_BdeOperators_BdeOperatorId",
                        column: x => x.BdeOperatorId,
                        principalTable: "BdeOperators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BdeActivities_Code",
                table: "BdeActivities",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BdeBookingQuantities_BdeOperatorId",
                table: "BdeBookingQuantities",
                column: "BdeOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_BdeBookingQuantities_Booking_Final",
                table: "BdeBookingQuantities",
                column: "BdeBookingId",
                unique: true,
                filter: "[IsFinal] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_BdeBookingQuantities_Booking_ReportedAt",
                table: "BdeBookingQuantities",
                columns: new[] { "BdeBookingId", "ReportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BdeBookings_BdeActivityId",
                table: "BdeBookings",
                column: "BdeActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_BdeBookings_BdeOperatorId_Active",
                table: "BdeBookings",
                column: "BdeOperatorId",
                unique: true,
                filter: "[EndedAt] IS NULL AND [IsCancelled] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BdeBookings_BdeTerminalId",
                table: "BdeBookings",
                column: "BdeTerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_BdeBookings_Operator_StartedAt",
                table: "BdeBookings",
                columns: new[] { "BdeOperatorId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BdeBookings_ParentBookingId",
                table: "BdeBookings",
                column: "ParentBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BdeBookings_StartedAt",
                table: "BdeBookings",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BdeBookings_WorkOperationId_Active",
                table: "BdeBookings",
                column: "WorkOperationId",
                unique: true,
                filter: "[EndedAt] IS NULL AND [IsCancelled] = 0 AND [WorkOperationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BdeBookings_Workplace_EndedAt",
                table: "BdeBookings",
                columns: new[] { "ProductionWorkplaceId", "EndedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BdeOperators_PersonnelNumber",
                table: "BdeOperators",
                column: "PersonnelNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BdeOperators_UserId",
                table: "BdeOperators",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BdeTerminals_DefaultProductionWorkplaceId",
                table: "BdeTerminals",
                column: "DefaultProductionWorkplaceId");

            migrationBuilder.CreateIndex(
                name: "IX_BdeTerminals_UserId",
                table: "BdeTerminals",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BdeBookingQuantities");

            migrationBuilder.DropTable(
                name: "BdeBookings");

            migrationBuilder.DropTable(
                name: "BdeActivities");

            migrationBuilder.DropTable(
                name: "BdeOperators");

            migrationBuilder.DropTable(
                name: "BdeTerminals");
        }
    }
}

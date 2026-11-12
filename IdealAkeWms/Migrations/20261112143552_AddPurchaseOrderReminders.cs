using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReminderDisabled",
                schema: "dbo",
                table: "Creditors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReminderEmail",
                schema: "dbo",
                table: "Creditors",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PurchaseOrderReminders",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Art = table.Column<int>(type: "int", nullable: false),
                    BelPosId = table.Column<int>(type: "int", nullable: false),
                    Belegnummer = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LieferantKto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreditorId = table.Column<int>(type: "int", nullable: true),
                    Liefertermin = table.Column<DateTime>(type: "date", nullable: true),
                    Rest = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Empfaenger = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Testbetrieb = table.Column<bool>(type: "bit", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderReminders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderReminders_BelPosId_Art_SentAt",
                schema: "dbo",
                table: "PurchaseOrderReminders",
                columns: new[] { "BelPosId", "Art", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderReminders_SentAt",
                schema: "dbo",
                table: "PurchaseOrderReminders",
                column: "SentAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseOrderReminders",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "ReminderDisabled",
                schema: "dbo",
                table: "Creditors");

            migrationBuilder.DropColumn(
                name: "ReminderEmail",
                schema: "dbo",
                table: "Creditors");
        }
    }
}

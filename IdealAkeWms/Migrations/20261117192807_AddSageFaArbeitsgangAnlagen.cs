using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddSageFaArbeitsgangAnlagen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SageFaArbeitsgangAnlagen",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BelId = table.Column<int>(type: "int", nullable: false),
                    HauptFa = table.Column<int>(type: "int", nullable: false),
                    Kuerzel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SageArbeitsgangnummer = table.Column<string>(type: "nvarchar(31)", maxLength: 31, nullable: false),
                    Reihenfolge = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Versuche = table.Column<int>(type: "int", nullable: false),
                    Fehler = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SageAm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SageFaArbeitsgangAnlagen", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SageFaArbeitsgangAnlagen_Status",
                schema: "dbo",
                table: "SageFaArbeitsgangAnlagen",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_SageFaArbeitsgangAnlagen_BelId_Ag",
                schema: "dbo",
                table: "SageFaArbeitsgangAnlagen",
                columns: new[] { "BelId", "SageArbeitsgangnummer" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SageFaArbeitsgangAnlagen",
                schema: "dbo");
        }
    }
}

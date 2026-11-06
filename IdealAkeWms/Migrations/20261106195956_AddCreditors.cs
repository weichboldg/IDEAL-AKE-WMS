using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Creditors",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Matchcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsCoater = table.Column<bool>(type: "bit", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SageKto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SageMatchcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SageGruppe = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SageEUUStID = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    SageZahlungskondition = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SageEKMindestbestellwert = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SageMissingSince = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SageUebergabeStatus = table.Column<int>(type: "int", nullable: false),
                    SageUebergabeAm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SageUebergabeFehler = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Creditors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Creditors_SageKto",
                schema: "dbo",
                table: "Creditors",
                column: "SageKto",
                unique: true,
                filter: "[SageKto] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Creditors_ShortName",
                schema: "dbo",
                table: "Creditors",
                column: "ShortName",
                unique: true,
                filter: "[ShortName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Creditors",
                schema: "dbo");
        }
    }
}

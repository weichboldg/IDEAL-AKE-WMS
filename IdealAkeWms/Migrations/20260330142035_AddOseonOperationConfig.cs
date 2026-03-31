using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddOseonOperationConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OseonOperationConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DueDateOffsetDays = table.Column<int>(type: "int", nullable: false),
                    IsOseonRelevant = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OseonOperationConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OseonOperationConfigs_OperationName",
                table: "OseonOperationConfigs",
                column: "OperationName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OseonOperationConfigs");
        }
    }
}

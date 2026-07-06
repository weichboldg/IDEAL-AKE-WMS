using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseRequisitionTypeAndGlasRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                schema: "dbo",
                table: "WarehouseRequisitions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'glasbestellung')
BEGIN
    INSERT INTO Roles ([Key], [Name], [Description], [IsSystem], [SortOrder],
                       [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('glasbestellung', 'Glasbestellungen',
            'Glas-Bestellungen erfassen und eigene Fehlteile verfolgen (Zugriff nur auf Lagerbestellungen (Reiter Glas) + Meine Fehlteile).',
            1, 9,
            SYSDATETIME(), 'system', 'system')
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM Roles WHERE [Key] = 'glasbestellung'");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "dbo",
                table: "WarehouseRequisitions");
        }
    }
}

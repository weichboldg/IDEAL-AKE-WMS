using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddLagerbestellungRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'lagerbestellung')
BEGIN
    INSERT INTO Roles ([Key], [Name], [Description], [IsSystem], [SortOrder],
                       [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('lagerbestellung', 'Lagerbestellungen',
            'Lagerbestellungen erfassen und eigene Fehlteile verfolgen (Zugriff nur auf Meine Lagerbestellungen + Meine Fehlteile).',
            1, 8,
            SYSDATETIME(), 'system', 'system')
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM Roles WHERE [Key] = 'lagerbestellung'");
        }
    }
}

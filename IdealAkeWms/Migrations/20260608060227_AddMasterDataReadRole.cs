using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterDataReadRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'masterdata_read')
BEGIN
    INSERT INTO Roles ([Key], [Name], [Description], [AdGroup], [IsSystem], [SortOrder],
                       [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('masterdata_read', 'Stammdaten ansehen',
            'Nur-Lesen-Zugriff auf alle Stammdaten-Sichten (Benutzer, Rollen, Arbeitsplaetze, Einstellungen, Werkbaenke, Empfaenger, Artikelkategorien/-attribute, Schichtkalender, Aktivitaets-Protokoll).',
            NULL, 1, 5,
            SYSDATETIME(), 'system', 'system')
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM Roles WHERE [Key] = 'masterdata_read'");
        }
    }
}

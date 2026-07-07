using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddStockReadRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'stock_read')
BEGIN
    INSERT INTO Roles ([Key], [Name], [Description], [IsSystem], [SortOrder],
                       [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('stock_read', 'Lagerbestand-Ansicht',
            'Nur-Lesen-Zugriff auf Bestände und Bewegungshistorie.',
            1, 35,
            SYSDATETIME(), 'system', 'system')
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM Roles WHERE [Key] = 'stock_read'");
        }
    }
}

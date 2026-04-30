using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddOseonWorkOperationsPerformanceIndex : Migration
    {
        // Catch-up migration: alle vorigen Schema-Aenderungen (BDE-Tabellen, dbo-Schema-Move,
        // Holidays.Source, ProductionWorkplaces.Bde* Spalten) wurden bereits via SQL/00_FreshInstall.sql
        // bzw. SQL/4x_*.sql Scripts auf der DB angelegt. Diese Migration synchronisiert nur den neuen
        // Composite-Index fuer OseonWorkOperations (Performance-Optimierung) — siehe SQL/52_*.sql.
        // Der Snapshot enthaelt den vollen Drift, damit nachfolgende Migrations sauber relativ dazu
        // generiert werden.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_OseonWorkOperations_OrderStatusName'
                      AND object_id = OBJECT_ID('dbo.OseonWorkOperations'))
                BEGIN
                    CREATE INDEX [IX_OseonWorkOperations_OrderStatusName]
                        ON [dbo].[OseonWorkOperations] ([OseonProductionOrderId], [OseonStatus], [Name]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_OseonWorkOperations_OrderStatusName'
                      AND object_id = OBJECT_ID('dbo.OseonWorkOperations'))
                BEGIN
                    DROP INDEX [IX_OseonWorkOperations_OrderStatusName] ON [dbo].[OseonWorkOperations];
                END
            ");
        }
    }
}

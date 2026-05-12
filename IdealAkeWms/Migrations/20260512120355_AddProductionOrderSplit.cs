using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionOrderSplit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Schema + Daten + Drop wurden im Wartungsfenster über SQL/60_ProductionOrderSplit.sql
            // ausgeführt. __EFMigrationsHistory-Eintrag wird vom SQL-Skript selbst gesetzt
            // (Section G). Diese Migration ist ein History-Marker für EF-Snapshot-Konsistenz.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback erfolgt per Backup-Restore (Roadmap 5.5). Down() ist intentional leer.
        }
    }
}

using System;
using System.IO;
using System.Text.RegularExpressions;
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
            // Schema + Daten + Drop werden aus SQL/60_ProductionOrderSplit.sql geladen
            // (single source of truth). Die SQL-Datei wird per .csproj-CopyToOutput in
            // bin/.../Migrations/Scripts/ deployed.
            //
            // Production-Cutover-Pfad: DBA kann das SQL-Skript manuell VOR App-Start in SSMS
            // ausfuehren. Section G des Skripts inserted den __EFMigrationsHistory-Eintrag,
            // daraufhin ueberspringt EF diese Up()-Methode beim naechsten db.Database.Migrate().
            //
            // Dev/Test-Pfad: App-Start ruft Migrate(), File ist vorhanden, Migration laeuft
            // automatisch durch (idempotent, Batches via GO-Split).

            var sqlPath = Path.Combine(AppContext.BaseDirectory, "Migrations", "Scripts", "60_ProductionOrderSplit.sql");
            if (!File.Exists(sqlPath))
            {
                // Kein SQL-File deployed → Production-Cutover-Modus erwartet manuelle Ausfuehrung.
                // EF wird die Migration trotzdem als "applied" markieren, sobald diese Up() durchlaeuft.
                return;
            }

            var sqlContent = File.ReadAllText(sqlPath);

            // Defensive (Round 6): SQL/60 versions before commit 2483ace hatten eine
            // "Section G", die INSERT INTO __EFMigrationsHistory selbst gemacht hat.
            // Wenn dieser Build mit einer stale SQL-Datei im Output deployed wird,
            // wuerde Section G mit ProductVersion='10.0.0' inserten, EF danach mit
            // ProductVersion='10.0.2' --> PK violation. Wir strippen daher robust
            // alle History-INSERTs aus dem SQL bevor wir es ausfuehren --
            // EF erledigt den History-Insert selbst nach erfolgreichem Up().
            sqlContent = Regex.Replace(
                sqlContent,
                @"INSERT\s+INTO\s+(?:\[dbo\]\.)?(?:dbo\.)?(?:\[__EFMigrationsHistory\]|__EFMigrationsHistory)\b[^;]*;",
                "-- (Round 6) inline __EFMigrationsHistory INSERT stripped",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var batches = Regex.Split(sqlContent, @"(?im)^\s*GO\s*$");
            foreach (var batch in batches)
            {
                var trimmed = batch.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    migrationBuilder.Sql(trimmed);
                }
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback erfolgt per Backup-Restore (Roadmap 5.5). Down() ist intentional leer.
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AKEBDELight.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-Op: Datenbank-Schema existiert bereits.
            // Diese Migration dient nur als Baseline für zukünftige Migrations.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-Op: Baseline-Migration kann nicht rückgängig gemacht werden.
        }
    }
}

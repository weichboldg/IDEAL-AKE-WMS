using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class ExtendStorageLocationCodeTo50 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sage-Lagerplatznamen koennen 13+ Zeichen haben. Der bisherige
            // NVARCHAR(12)-Cap fuehrte zum Skip im LagerplatzSyncService.
            // EF-ModelSnapshot meldete bereits 50 (Drift gegen tatsaechliche DB);
            // diese Migration zieht das DB-Schema nach.
            //
            // UNIQUE-Index auf Code muss vor dem ALTER abgehaengt werden.
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StorageLocations_Code' AND object_id = OBJECT_ID('dbo.StorageLocations'))
                    DROP INDEX [IX_StorageLocations_Code] ON [dbo].[StorageLocations];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_StorageLocations_Code' AND object_id = OBJECT_ID('dbo.StorageLocations'))
                    ALTER TABLE [dbo].[StorageLocations] DROP CONSTRAINT [UQ_StorageLocations_Code];
            ");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "dbo",
                table: "StorageLocations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(12)",
                oldMaxLength: 12);

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_Code",
                schema: "dbo",
                table: "StorageLocations",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StorageLocations_Code' AND object_id = OBJECT_ID('dbo.StorageLocations'))
                    DROP INDEX [IX_StorageLocations_Code] ON [dbo].[StorageLocations];
            ");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "dbo",
                table: "StorageLocations",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_Code",
                schema: "dbo",
                table: "StorageLocations",
                column: "Code",
                unique: true);
        }
    }
}

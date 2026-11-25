using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddBdeTeilMeldungIstAbschliessendUndStandardsicht : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_BdeTeilMeldungen_Lagerbestellung",
                schema: "dbo",
                table: "BdeTeilMeldungen");

            migrationBuilder.DropIndex(
                name: "UX_BdeTeilMeldungen_Schluessel",
                schema: "dbo",
                table: "BdeTeilMeldungen");

            migrationBuilder.AddColumn<bool>(
                name: "BdeStandardsichtTeile",
                schema: "dbo",
                table: "ProductionWorkplaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // v1.99.75 (Spec 2026-11-25-bde-masken-vereinheitlichung, Antwort 3a): Vorbelegung nur bei Erstanlage, identisch zu SQL/169.
            migrationBuilder.Sql("UPDATE dbo.ProductionWorkplaces SET BdeStandardsichtTeile = 1 WHERE BdeAktiv = 1 AND BdeTeileliste = 1 AND ISNULL(LTRIM(RTRIM(ArbeitsschrittCode)), N'') <> N''");

            // ADR 0037: Bestand = abschliessend (DEFAULT 1 nur hier/SQL/FreshInstall, kein HasDefaultValue im Modell - bool-Sentinel).
            migrationBuilder.AddColumn<bool>(
                name: "IstAbschliessend",
                schema: "dbo",
                table: "BdeTeilMeldungen",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "UX_BdeTeilMeldungen_Lagerbestellung",
                schema: "dbo",
                table: "BdeTeilMeldungen",
                columns: new[] { "WarehouseRequisitionItemId", "Kuerzel" },
                unique: true,
                filter: "[IsCancelled] = 0 AND [IstAbschliessend] = 1 AND [WarehouseRequisitionItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_BdeTeilMeldungen_Schluessel",
                schema: "dbo",
                table: "BdeTeilMeldungen",
                columns: new[] { "HauptFA", "VaterFA", "SubFA", "Position", "Artnr", "Kuerzel" },
                unique: true,
                filter: "[IsCancelled] = 0 AND [IstAbschliessend] = 1 AND [WarehouseRequisitionItemId] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Achtung: der alte UNIQUE (eine Meldung je Schluessel) scheitert, solange mehrere nicht stornierte Meldungen je Schluessel existieren.
            migrationBuilder.DropIndex(
                name: "UX_BdeTeilMeldungen_Lagerbestellung",
                schema: "dbo",
                table: "BdeTeilMeldungen");

            migrationBuilder.DropIndex(
                name: "UX_BdeTeilMeldungen_Schluessel",
                schema: "dbo",
                table: "BdeTeilMeldungen");

            migrationBuilder.DropColumn(
                name: "BdeStandardsichtTeile",
                schema: "dbo",
                table: "ProductionWorkplaces");

            migrationBuilder.DropColumn(
                name: "IstAbschliessend",
                schema: "dbo",
                table: "BdeTeilMeldungen");

            migrationBuilder.CreateIndex(
                name: "UX_BdeTeilMeldungen_Lagerbestellung",
                schema: "dbo",
                table: "BdeTeilMeldungen",
                columns: new[] { "WarehouseRequisitionItemId", "Kuerzel" },
                unique: true,
                filter: "[IsCancelled] = 0 AND [WarehouseRequisitionItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_BdeTeilMeldungen_Schluessel",
                schema: "dbo",
                table: "BdeTeilMeldungen",
                columns: new[] { "HauptFA", "VaterFA", "SubFA", "Position", "Artnr", "Kuerzel" },
                unique: true,
                filter: "[IsCancelled] = 0 AND [WarehouseRequisitionItemId] IS NULL");
        }
    }
}

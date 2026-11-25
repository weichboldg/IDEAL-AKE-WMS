using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddBdeTeilelisteLagerbestellungen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_BdeTeilMeldungen_Schluessel",
                schema: "dbo",
                table: "BdeTeilMeldungen");

            migrationBuilder.AddColumn<bool>(
                name: "BdeTeilelisteLagerbestellungen",
                schema: "dbo",
                table: "ProductionWorkplaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseRequisitionItemId",
                schema: "dbo",
                table: "BdeTeilMeldungen",
                type: "int",
                nullable: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_BdeTeilMeldungen_WarehouseRequisitionItems_WarehouseRequisitionItemId",
                schema: "dbo",
                table: "BdeTeilMeldungen",
                column: "WarehouseRequisitionItemId",
                principalSchema: "dbo",
                principalTable: "WarehouseRequisitionItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BdeTeilMeldungen_WarehouseRequisitionItems_WarehouseRequisitionItemId",
                schema: "dbo",
                table: "BdeTeilMeldungen");

            migrationBuilder.DropIndex(
                name: "UX_BdeTeilMeldungen_Lagerbestellung",
                schema: "dbo",
                table: "BdeTeilMeldungen");

            migrationBuilder.DropIndex(
                name: "UX_BdeTeilMeldungen_Schluessel",
                schema: "dbo",
                table: "BdeTeilMeldungen");

            migrationBuilder.DropColumn(
                name: "BdeTeilelisteLagerbestellungen",
                schema: "dbo",
                table: "ProductionWorkplaces");

            migrationBuilder.DropColumn(
                name: "WarehouseRequisitionItemId",
                schema: "dbo",
                table: "BdeTeilMeldungen");

            migrationBuilder.CreateIndex(
                name: "UX_BdeTeilMeldungen_Schluessel",
                schema: "dbo",
                table: "BdeTeilMeldungen",
                columns: new[] { "HauptFA", "VaterFA", "SubFA", "Position", "Artnr", "Kuerzel" },
                unique: true,
                filter: "[IsCancelled] = 0");
        }
    }
}

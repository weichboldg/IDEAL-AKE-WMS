using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleDummyRequisitionItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WarehouseRequisitionItems_WarehouseRequisitionId_ArticleNumber",
                schema: "dbo",
                table: "WarehouseRequisitionItems");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseRequisitionItems_WarehouseRequisitionId_ArticleNumber",
                schema: "dbo",
                table: "WarehouseRequisitionItems",
                columns: new[] { "WarehouseRequisitionId", "ArticleNumber" },
                unique: true,
                filter: "[ArticleNumber] <> 'DUMMY'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WarehouseRequisitionItems_WarehouseRequisitionId_ArticleNumber",
                schema: "dbo",
                table: "WarehouseRequisitionItems");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseRequisitionItems_WarehouseRequisitionId_ArticleNumber",
                schema: "dbo",
                table: "WarehouseRequisitionItems",
                columns: new[] { "WarehouseRequisitionId", "ArticleNumber" },
                unique: true);
        }
    }
}

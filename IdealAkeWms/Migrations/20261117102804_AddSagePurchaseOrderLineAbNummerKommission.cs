using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddSagePurchaseOrderLineAbNummerKommission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AbNummer",
                schema: "dbo",
                table: "SagePurchaseOrderLines",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kommission",
                schema: "dbo",
                table: "SagePurchaseOrderLines",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbNummer",
                schema: "dbo",
                table: "SagePurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "Kommission",
                schema: "dbo",
                table: "SagePurchaseOrderLines");
        }
    }
}

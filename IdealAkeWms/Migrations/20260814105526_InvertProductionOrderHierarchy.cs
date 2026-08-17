using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class InvertProductionOrderHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Schema-Inversion der Kern-Tabelle ProductionOrders (IDEAL Teil 7).
            // Reihenfolge daten-konvertierend, nicht destruktiv: alte Eindeutigkeit auf OrderNumber
            // loesen, Spalten additiv/nullable anlegen, SubOrderNumber = OrderNumber backfillen, erst
            // dann SubOrderNumber NOT NULL setzen und den neuen Unique-Index anlegen. Das
            // vermeidet die Unique-Verletzung, die ein Default '' auf Bestandsdaten ausloesen wuerde.
            //
            // Alte Eindeutigkeit auf OrderNumber loesen — je nach DB-Herkunft ein UNIQUE-CONSTRAINT
            // (UQ_..., FreshInstall-DB) ODER ein UNIQUE-INDEX (IX_..., EF-migrierte DB). BEIDE Formen
            // guarded abraeumen. Ohne den Guard bricht db.Database.Migrate() beim Start auf einer
            // FreshInstall-DB (der IX_-Index existiert dort nicht, nur der UQ_-Constraint -> SqlError
            // 3701). Deckungsgleich + idempotent mit dem Deploy-Skript SQL/90 (dort Schritt 4a).
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.key_constraints
           WHERE name = 'UQ_ProductionOrders_OrderNumber'
             AND parent_object_id = OBJECT_ID('dbo.ProductionOrders'))
    ALTER TABLE [dbo].[ProductionOrders] DROP CONSTRAINT [UQ_ProductionOrders_OrderNumber];
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = 'IX_ProductionOrders_OrderNumber'
             AND object_id = OBJECT_ID('dbo.ProductionOrders')
             AND is_unique = 1)
    DROP INDEX [IX_ProductionOrders_OrderNumber] ON [dbo].[ProductionOrders];");

            migrationBuilder.AddColumn<string>(
                name: "ParentSubOrderNumber",
                schema: "dbo",
                table: "ProductionOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SageMissingSince",
                schema: "dbo",
                table: "ProductionOrders",
                type: "datetime2",
                nullable: true);

            // Zunaechst nullable anlegen, damit der Backfill greifen kann.
            migrationBuilder.AddColumn<string>(
                name: "SubOrderNumber",
                schema: "dbo",
                table: "ProductionOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Backfill: im flachen (AKE-)Bestand gilt die Invariante SubOrderNumber == OrderNumber.
            migrationBuilder.Sql(
                "UPDATE [dbo].[ProductionOrders] SET [SubOrderNumber] = [OrderNumber] WHERE [SubOrderNumber] IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "SubOrderNumber",
                schema: "dbo",
                table: "ProductionOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            // Nicht-eindeutiger Index auf OrderNumber (guarded, deckungsgleich SQL/90 Schritt 4b).
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_ProductionOrders_OrderNumber'
                 AND object_id = OBJECT_ID('dbo.ProductionOrders'))
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_OrderNumber]
        ON [dbo].[ProductionOrders]([OrderNumber]);");

            // Neuer Unique-Index auf SubOrderNumber (guarded, deckungsgleich SQL/90 Schritt 4c).
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_ProductionOrders_SubOrderNumber'
                 AND object_id = OBJECT_ID('dbo.ProductionOrders'))
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ProductionOrders_SubOrderNumber]
        ON [dbo].[ProductionOrders]([SubOrderNumber]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_OrderNumber",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_SubOrderNumber",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "ParentSubOrderNumber",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "SageMissingSince",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "SubOrderNumber",
                schema: "dbo",
                table: "ProductionOrders");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_OrderNumber",
                schema: "dbo",
                table: "ProductionOrders",
                column: "OrderNumber",
                unique: true);
        }
    }
}

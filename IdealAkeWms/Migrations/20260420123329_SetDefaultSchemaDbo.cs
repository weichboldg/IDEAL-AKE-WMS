using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class SetDefaultSchemaDbo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.RenameTable(
                name: "WorkstationUsers",
                newName: "WorkstationUsers",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Workstations",
                newName: "Workstations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "WorkOperations",
                newName: "WorkOperations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "UserViewPreferences",
                newName: "UserViewPreferences",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Users",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "UserRoles",
                newName: "UserRoles",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "StorageLocations",
                newName: "StorageLocations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "StockMovements",
                newName: "StockMovements",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ServiceSettings",
                newName: "ServiceSettings",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "Roles",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ProductionWorkplaceUsers",
                newName: "ProductionWorkplaceUsers",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ProductionWorkplaces",
                newName: "ProductionWorkplaces",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ProductionOrders",
                newName: "ProductionOrders",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "PickingItems",
                newName: "PickingItems",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "PartRequisitions",
                newName: "PartRequisitions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "OseonWorkOperations",
                newName: "OseonWorkOperations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "OseonProductionOrders",
                newName: "OseonProductionOrders",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "OseonOperationConfigs",
                newName: "OseonOperationConfigs",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "OrderRecipients",
                newName: "OrderRecipients",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "OrderRecipientGroups",
                newName: "OrderRecipientGroups",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Holidays",
                newName: "Holidays",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "EnaioDmsDocuments",
                newName: "EnaioDmsDocuments",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "CachedBomItems",
                newName: "CachedBomItems",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "CachedBomHeaders",
                newName: "CachedBomHeaders",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "BdeTerminals",
                newName: "BdeTerminals",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "BdeOperators",
                newName: "BdeOperators",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "BdeBookings",
                newName: "BdeBookings",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "BdeBookingQuantities",
                newName: "BdeBookingQuantities",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "BdeActivities",
                newName: "BdeActivities",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Articles",
                newName: "Articles",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ArticleGroupRecipientMappings",
                newName: "ArticleGroupRecipientMappings",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ArticleCategories",
                newName: "ArticleCategories",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ArticleAttributeValues",
                newName: "ArticleAttributeValues",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ArticleAttributeOptions",
                newName: "ArticleAttributeOptions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ArticleAttributeDefinitions",
                newName: "ArticleAttributeDefinitions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "AppSettings",
                newName: "AppSettings",
                newSchema: "dbo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "WorkstationUsers",
                schema: "dbo",
                newName: "WorkstationUsers");

            migrationBuilder.RenameTable(
                name: "Workstations",
                schema: "dbo",
                newName: "Workstations");

            migrationBuilder.RenameTable(
                name: "WorkOperations",
                schema: "dbo",
                newName: "WorkOperations");

            migrationBuilder.RenameTable(
                name: "UserViewPreferences",
                schema: "dbo",
                newName: "UserViewPreferences");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "dbo",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "UserRoles",
                schema: "dbo",
                newName: "UserRoles");

            migrationBuilder.RenameTable(
                name: "StorageLocations",
                schema: "dbo",
                newName: "StorageLocations");

            migrationBuilder.RenameTable(
                name: "StockMovements",
                schema: "dbo",
                newName: "StockMovements");

            migrationBuilder.RenameTable(
                name: "ServiceSettings",
                schema: "dbo",
                newName: "ServiceSettings");

            migrationBuilder.RenameTable(
                name: "Roles",
                schema: "dbo",
                newName: "Roles");

            migrationBuilder.RenameTable(
                name: "ProductionWorkplaceUsers",
                schema: "dbo",
                newName: "ProductionWorkplaceUsers");

            migrationBuilder.RenameTable(
                name: "ProductionWorkplaces",
                schema: "dbo",
                newName: "ProductionWorkplaces");

            migrationBuilder.RenameTable(
                name: "ProductionOrders",
                schema: "dbo",
                newName: "ProductionOrders");

            migrationBuilder.RenameTable(
                name: "PickingItems",
                schema: "dbo",
                newName: "PickingItems");

            migrationBuilder.RenameTable(
                name: "PartRequisitions",
                schema: "dbo",
                newName: "PartRequisitions");

            migrationBuilder.RenameTable(
                name: "OseonWorkOperations",
                schema: "dbo",
                newName: "OseonWorkOperations");

            migrationBuilder.RenameTable(
                name: "OseonProductionOrders",
                schema: "dbo",
                newName: "OseonProductionOrders");

            migrationBuilder.RenameTable(
                name: "OseonOperationConfigs",
                schema: "dbo",
                newName: "OseonOperationConfigs");

            migrationBuilder.RenameTable(
                name: "OrderRecipients",
                schema: "dbo",
                newName: "OrderRecipients");

            migrationBuilder.RenameTable(
                name: "OrderRecipientGroups",
                schema: "dbo",
                newName: "OrderRecipientGroups");

            migrationBuilder.RenameTable(
                name: "Holidays",
                schema: "dbo",
                newName: "Holidays");

            migrationBuilder.RenameTable(
                name: "EnaioDmsDocuments",
                schema: "dbo",
                newName: "EnaioDmsDocuments");

            migrationBuilder.RenameTable(
                name: "CachedBomItems",
                schema: "dbo",
                newName: "CachedBomItems");

            migrationBuilder.RenameTable(
                name: "CachedBomHeaders",
                schema: "dbo",
                newName: "CachedBomHeaders");

            migrationBuilder.RenameTable(
                name: "BdeTerminals",
                schema: "dbo",
                newName: "BdeTerminals");

            migrationBuilder.RenameTable(
                name: "BdeOperators",
                schema: "dbo",
                newName: "BdeOperators");

            migrationBuilder.RenameTable(
                name: "BdeBookings",
                schema: "dbo",
                newName: "BdeBookings");

            migrationBuilder.RenameTable(
                name: "BdeBookingQuantities",
                schema: "dbo",
                newName: "BdeBookingQuantities");

            migrationBuilder.RenameTable(
                name: "BdeActivities",
                schema: "dbo",
                newName: "BdeActivities");

            migrationBuilder.RenameTable(
                name: "Articles",
                schema: "dbo",
                newName: "Articles");

            migrationBuilder.RenameTable(
                name: "ArticleGroupRecipientMappings",
                schema: "dbo",
                newName: "ArticleGroupRecipientMappings");

            migrationBuilder.RenameTable(
                name: "ArticleCategories",
                schema: "dbo",
                newName: "ArticleCategories");

            migrationBuilder.RenameTable(
                name: "ArticleAttributeValues",
                schema: "dbo",
                newName: "ArticleAttributeValues");

            migrationBuilder.RenameTable(
                name: "ArticleAttributeOptions",
                schema: "dbo",
                newName: "ArticleAttributeOptions");

            migrationBuilder.RenameTable(
                name: "ArticleAttributeDefinitions",
                schema: "dbo",
                newName: "ArticleAttributeDefinitions");

            migrationBuilder.RenameTable(
                name: "AppSettings",
                schema: "dbo",
                newName: "AppSettings");
        }
    }
}

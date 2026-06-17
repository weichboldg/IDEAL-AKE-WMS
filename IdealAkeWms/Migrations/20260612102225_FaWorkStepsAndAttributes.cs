using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class FaWorkStepsAndAttributes : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// FA-Vorbau v1.22.0 (Spec 2026-06-12): 8 neue Tabellen, daten-erhaltende
        /// Konvertierung ProductionOrderAssemblyGroups/-Specs -> FaWorkSteps/-Specs,
        /// danach DROP der alten Tabellen. Die Migration ist daten-destruktiv fuer das
        /// Alt-Schema (Down() stellt nur leere Tabellen wieder her) — DB-Backup vor
        /// Produktions-Deploy ist Pflicht (siehe Cutover-Doc).
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FaAttributeDefinitions",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AttributeType = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaAttributeDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkSteps",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SearchString = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSteps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FaAttributeOptions",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FaAttributeDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaAttributeOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaAttributeOptions_FaAttributeDefinitions_FaAttributeDefinitionId",
                        column: x => x.FaAttributeDefinitionId,
                        principalSchema: "dbo",
                        principalTable: "FaAttributeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FaAttributeWorkSteps",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FaAttributeDefinitionId = table.Column<int>(type: "int", nullable: false),
                    WorkStepId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaAttributeWorkSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaAttributeWorkSteps_FaAttributeDefinitions_FaAttributeDefinitionId",
                        column: x => x.FaAttributeDefinitionId,
                        principalSchema: "dbo",
                        principalTable: "FaAttributeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FaAttributeWorkSteps_WorkSteps_WorkStepId",
                        column: x => x.WorkStepId,
                        principalSchema: "dbo",
                        principalTable: "WorkSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FaWorkSteps",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    WorkStepId = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaWorkSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaWorkSteps_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "dbo",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FaWorkSteps_WorkSteps_WorkStepId",
                        column: x => x.WorkStepId,
                        principalSchema: "dbo",
                        principalTable: "WorkSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionWorkplaceWorkSteps",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionWorkplaceId = table.Column<int>(type: "int", nullable: false),
                    WorkStepId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionWorkplaceWorkSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionWorkplaceWorkSteps_ProductionWorkplaces_ProductionWorkplaceId",
                        column: x => x.ProductionWorkplaceId,
                        principalSchema: "dbo",
                        principalTable: "ProductionWorkplaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionWorkplaceWorkSteps_WorkSteps_WorkStepId",
                        column: x => x.WorkStepId,
                        principalSchema: "dbo",
                        principalTable: "WorkSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FaAttributeValues",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    FaAttributeDefinitionId = table.Column<int>(type: "int", nullable: false),
                    SelectedOptionId = table.Column<int>(type: "int", nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaAttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaAttributeValues_FaAttributeDefinitions_FaAttributeDefinitionId",
                        column: x => x.FaAttributeDefinitionId,
                        principalSchema: "dbo",
                        principalTable: "FaAttributeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FaAttributeValues_FaAttributeOptions_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalSchema: "dbo",
                        principalTable: "FaAttributeOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FaAttributeValues_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "dbo",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FaWorkStepSpecs",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FaWorkStepId = table.Column<int>(type: "int", nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaWorkStepSpecs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaWorkStepSpecs_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "dbo",
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FaWorkStepSpecs_FaWorkSteps_FaWorkStepId",
                        column: x => x.FaWorkStepId,
                        principalSchema: "dbo",
                        principalTable: "FaWorkSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaAttributeOptions_FaAttributeDefinitionId",
                schema: "dbo",
                table: "FaAttributeOptions",
                column: "FaAttributeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FaAttributeValues_FaAttributeDefinitionId",
                schema: "dbo",
                table: "FaAttributeValues",
                column: "FaAttributeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FaAttributeValues_ProductionOrderId_FaAttributeDefinitionId",
                schema: "dbo",
                table: "FaAttributeValues",
                columns: new[] { "ProductionOrderId", "FaAttributeDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaAttributeValues_SelectedOptionId",
                schema: "dbo",
                table: "FaAttributeValues",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_FaAttributeWorkSteps_FaAttributeDefinitionId_WorkStepId",
                schema: "dbo",
                table: "FaAttributeWorkSteps",
                columns: new[] { "FaAttributeDefinitionId", "WorkStepId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaAttributeWorkSteps_WorkStepId",
                schema: "dbo",
                table: "FaAttributeWorkSteps",
                column: "WorkStepId");

            migrationBuilder.CreateIndex(
                name: "IX_FaWorkSteps_ProductionOrderId_IsRemoved",
                schema: "dbo",
                table: "FaWorkSteps",
                columns: new[] { "ProductionOrderId", "IsRemoved" });

            migrationBuilder.CreateIndex(
                name: "IX_FaWorkSteps_ProductionOrderId_WorkStepId",
                schema: "dbo",
                table: "FaWorkSteps",
                columns: new[] { "ProductionOrderId", "WorkStepId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaWorkSteps_WorkStepId",
                schema: "dbo",
                table: "FaWorkSteps",
                column: "WorkStepId");

            migrationBuilder.CreateIndex(
                name: "IX_FaWorkStepSpecs_ArticleId",
                schema: "dbo",
                table: "FaWorkStepSpecs",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_FaWorkStepSpecs_FaWorkStepId",
                schema: "dbo",
                table: "FaWorkStepSpecs",
                column: "FaWorkStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionWorkplaceWorkSteps_ProductionWorkplaceId_WorkStepId",
                schema: "dbo",
                table: "ProductionWorkplaceWorkSteps",
                columns: new[] { "ProductionWorkplaceId", "WorkStepId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionWorkplaceWorkSteps_WorkStepId",
                schema: "dbo",
                table: "ProductionWorkplaceWorkSteps",
                column: "WorkStepId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSteps_Code",
                schema: "dbo",
                table: "WorkSteps",
                column: "Code",
                unique: true);

            // ----------------------------------------------------------------
            // Daten-Konvertierung + Seeds (VOR dem Drop der alten Tabellen!)
            // ----------------------------------------------------------------

            // 1) Seed WorkSteps (VK-VA, idempotent)
            migrationBuilder.Sql(@"
INSERT INTO WorkSteps (Code, Name, SearchString, SortOrder, IsActive, CreatedAt, CreatedBy, CreatedByWindows)
SELECT v.Code, v.Name, NULL, v.SortOrder, 1, GETDATE(), 'Migration', 'Migration'
FROM (VALUES ('VK','Kuehlung',1),('VL','Lueftung',2),('VE','Elektro',3),('VT','Tueren',4),('VA','Aufbau',5)) v(Code, Name, SortOrder)
WHERE NOT EXISTS (SELECT 1 FROM WorkSteps w WHERE w.Code = v.Code);");

            // 2) AssemblyGroups -> FaWorkSteps (IsApplicable=1 ODER Spec-Traeger; Specs an
            //    inaktiven Gruppen kommen mit IsRemoved=1)
            migrationBuilder.Sql(@"
INSERT INTO FaWorkSteps (ProductionOrderId, WorkStepId, IsCompleted, CompletedAt, CompletedBy, Source, IsRemoved, CreatedAt, CreatedBy, CreatedByWindows)
SELECT g.ProductionOrderId, w.Id, g.IsCompleted, NULL, NULL, 'Manual',
       CASE WHEN g.IsApplicable = 1 THEN 0 ELSE 1 END,
       GETDATE(), 'Migration', 'Migration'
FROM ProductionOrderAssemblyGroups g
JOIN WorkSteps w ON w.Code = g.GroupKey
WHERE (g.IsApplicable = 1
       OR EXISTS (SELECT 1 FROM ProductionOrderAssemblyGroupSpecs s WHERE s.AssemblyGroupId = g.Id))
  AND NOT EXISTS (SELECT 1 FROM FaWorkSteps f
                  WHERE f.ProductionOrderId = g.ProductionOrderId AND f.WorkStepId = w.Id);");

            // 3) Specs kopieren
            migrationBuilder.Sql(@"
INSERT INTO FaWorkStepSpecs (FaWorkStepId, ArticleId, Description, Quantity, Notes, SortOrder, CreatedAt, CreatedBy, CreatedByWindows, ModifiedAt, ModifiedBy, ModifiedByWindows)
SELECT f.Id, s.ArticleId, s.Description, s.Quantity, s.Notes, s.SortOrder,
       s.CreatedAt, s.CreatedBy, s.CreatedByWindows, s.ModifiedAt, s.ModifiedBy, s.ModifiedByWindows
FROM ProductionOrderAssemblyGroupSpecs s
JOIN ProductionOrderAssemblyGroups g ON g.Id = s.AssemblyGroupId
JOIN WorkSteps w ON w.Code = g.GroupKey
JOIN FaWorkSteps f ON f.ProductionOrderId = g.ProductionOrderId AND f.WorkStepId = w.Id;");

            // 4) Seed Merkmale + Optionen (idempotent)
            migrationBuilder.Sql(@"
INSERT INTO FaAttributeDefinitions (Name, AttributeType, SortOrder, IsActive, CreatedAt, CreatedBy, CreatedByWindows)
SELECT v.Name, v.AttrType, v.SortOrder, 1, GETDATE(), 'Migration', 'Migration'
FROM (VALUES ('Verdampfergroesse',1,1),('Leitungsausgang',1,2),('Verdampfergehaeuse',1,3),('Ventil aussenliegend',0,4)) v(Name, AttrType, SortOrder)
WHERE NOT EXISTS (SELECT 1 FROM FaAttributeDefinitions d WHERE d.Name = v.Name);

INSERT INTO FaAttributeOptions (FaAttributeDefinitionId, Value, SortOrder, IsActive, CreatedAt, CreatedBy, CreatedByWindows)
SELECT d.Id, v.Value, v.SortOrder, 1, GETDATE(), 'Migration', 'Migration'
FROM (VALUES
 ('Verdampfergroesse','UKW 2/1',1),('Verdampfergroesse','UKW 3/1',2),('Verdampfergroesse','UKW 4/1',3),
 ('Verdampfergroesse','UKW 5/1 (Euro 4)',4),('Verdampfergroesse','UKW 6/1',5),('Verdampfergroesse','Euro 2',6),
 ('Verdampfergroesse','Euro 3',7),('Verdampfergroesse','Caleo 80',8),('Verdampfergroesse','Caleo 120',9),
 ('Verdampfergroesse','Breite 60',10),
 ('Leitungsausgang','Standard',1),('Leitungsausgang','RG',2),('Leitungsausgang','Links',3),('Leitungsausgang','Links RG',4),
 ('Verdampfergehaeuse','2/1 Standard',1),('Verdampfergehaeuse','3/1 RG',2),('Verdampfergehaeuse','Sonder',3)
) v(DefName, Value, SortOrder)
JOIN FaAttributeDefinitions d ON d.Name = v.DefName
WHERE NOT EXISTS (SELECT 1 FROM FaAttributeOptions o WHERE o.FaAttributeDefinitionId = d.Id AND o.Value = v.Value);");

            // 5) Seed Rolle vorbau (Spaltenliste exakt wie Migration AddMasterDataReadRole / SQL/67)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'vorbau')
INSERT INTO Roles ([Key], [Name], [Description], [AdGroup], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
VALUES ('vorbau', 'Vorbau', 'FA-Abarbeitungsliste: Vorbau-Arbeitsgaenge einsehen und abhaken', NULL, 1,
        (SELECT MAX(SortOrder) + 1 FROM Roles), GETDATE(), 'Migration', 'Migration');");

            // ----------------------------------------------------------------
            // Altsystem droppen (nach erfolgter Konvertierung)
            // ----------------------------------------------------------------

            migrationBuilder.DropTable(
                name: "ProductionOrderAssemblyGroupSpecs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProductionOrderAssemblyGroups",
                schema: "dbo");

            // Relikt aus v1.11 (nie im Code gelesen/geschrieben) — ersetzt durch
            // ProductionWorkplaceWorkSteps. Guarded Sql statt DropTable, weil die
            // Tabelle nicht mehr im Model-Snapshot ist (Down stellt sie nicht wieder her).
            migrationBuilder.Sql(
                "IF OBJECT_ID(N'dbo.ProductionWorkplaceAssemblyGroups', N'U') IS NOT NULL DROP TABLE [dbo].[ProductionWorkplaceAssemblyGroups];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaAttributeValues",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FaAttributeWorkSteps",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FaWorkStepSpecs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProductionWorkplaceWorkSteps",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FaAttributeOptions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FaWorkSteps",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FaAttributeDefinitions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "WorkSteps",
                schema: "dbo");

            migrationBuilder.CreateTable(
                name: "ProductionOrderAssemblyGroups",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GroupKey = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsApplicable = table.Column<bool>(type: "bit", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderAssemblyGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderAssemblyGroups_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "dbo",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderAssemblyGroupSpecs",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: true),
                    AssemblyGroupId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedByWindows = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderAssemblyGroupSpecs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderAssemblyGroupSpecs_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "dbo",
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProductionOrderAssemblyGroupSpecs_ProductionOrderAssemblyGroups_AssemblyGroupId",
                        column: x => x.AssemblyGroupId,
                        principalSchema: "dbo",
                        principalTable: "ProductionOrderAssemblyGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderAssemblyGroups_GroupKey_IsApplicable",
                schema: "dbo",
                table: "ProductionOrderAssemblyGroups",
                columns: new[] { "GroupKey", "IsApplicable" });

            migrationBuilder.CreateIndex(
                name: "UQ_ProductionOrderAssemblyGroups_PO_Key",
                schema: "dbo",
                table: "ProductionOrderAssemblyGroups",
                columns: new[] { "ProductionOrderId", "GroupKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderAssemblyGroupSpecs_ArticleId",
                schema: "dbo",
                table: "ProductionOrderAssemblyGroupSpecs",
                column: "ArticleId",
                filter: "[ArticleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderAssemblyGroupSpecs_AssemblyGroupId",
                schema: "dbo",
                table: "ProductionOrderAssemblyGroupSpecs",
                column: "AssemblyGroupId");
        }
    }
}

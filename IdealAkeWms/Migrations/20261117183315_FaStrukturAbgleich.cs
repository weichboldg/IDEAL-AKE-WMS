using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class FaStrukturAbgleich : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Gleich SQL/159 (Schritte 1-6): Spalten nullable, Startwert = SyncedAt, NOT NULL, leer -> NULL, Dubletten-Check, UNIQUE-Indizes.
            migrationBuilder.Sql(@"IF COL_LENGTH(N'dbo.FaHierarchyNodes', N'ErstGesehen') IS NULL
    ALTER TABLE dbo.FaHierarchyNodes ADD ErstGesehen datetime2 NULL, ZuletztGesehen datetime2 NULL,
        VerschwundenSeit datetime2 NULL, VerschwundenGrund nvarchar(20) NULL;");

            migrationBuilder.Sql(@"IF COL_LENGTH(N'dbo.FaHierarchyOrderInfos', N'ErstGesehen') IS NULL
    ALTER TABLE dbo.FaHierarchyOrderInfos ADD ErstGesehen datetime2 NULL, ZuletztGesehen datetime2 NULL,
        VerschwundenSeit datetime2 NULL, VerschwundenGrund nvarchar(20) NULL;");

            migrationBuilder.Sql(@"IF COL_LENGTH(N'dbo.ProductionOrderPositions', N'ErstGesehen') IS NULL
    ALTER TABLE dbo.ProductionOrderPositions ADD ErstGesehen datetime2 NULL, ZuletztGesehen datetime2 NULL,
        VerschwundenSeit datetime2 NULL, VerschwundenGrund nvarchar(20) NULL;");

            migrationBuilder.Sql(@"UPDATE dbo.FaHierarchyNodes SET ErstGesehen = SyncedAt, ZuletztGesehen = SyncedAt WHERE ErstGesehen IS NULL;
UPDATE dbo.FaHierarchyOrderInfos SET ErstGesehen = SyncedAt, ZuletztGesehen = SyncedAt WHERE ErstGesehen IS NULL;
UPDATE dbo.ProductionOrderPositions SET ErstGesehen = SyncedAt, ZuletztGesehen = SyncedAt WHERE ErstGesehen IS NULL;");

            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.FaHierarchyNodes') AND name = N'ErstGesehen' AND is_nullable = 1)
    ALTER TABLE dbo.FaHierarchyNodes ALTER COLUMN ErstGesehen datetime2 NOT NULL;");

            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.FaHierarchyNodes') AND name = N'ZuletztGesehen' AND is_nullable = 1)
    ALTER TABLE dbo.FaHierarchyNodes ALTER COLUMN ZuletztGesehen datetime2 NOT NULL;");

            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.FaHierarchyOrderInfos') AND name = N'ErstGesehen' AND is_nullable = 1)
    ALTER TABLE dbo.FaHierarchyOrderInfos ALTER COLUMN ErstGesehen datetime2 NOT NULL;");

            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.FaHierarchyOrderInfos') AND name = N'ZuletztGesehen' AND is_nullable = 1)
    ALTER TABLE dbo.FaHierarchyOrderInfos ALTER COLUMN ZuletztGesehen datetime2 NOT NULL;");

            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrderPositions') AND name = N'ErstGesehen' AND is_nullable = 1)
    ALTER TABLE dbo.ProductionOrderPositions ALTER COLUMN ErstGesehen datetime2 NOT NULL;");

            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrderPositions') AND name = N'ZuletztGesehen' AND is_nullable = 1)
    ALTER TABLE dbo.ProductionOrderPositions ALTER COLUMN ZuletztGesehen datetime2 NOT NULL;");

            migrationBuilder.Sql(@"UPDATE dbo.FaHierarchyNodes SET Artnr = NULL WHERE LTRIM(RTRIM(Artnr)) = N'';
UPDATE dbo.FaHierarchyOrderInfos SET ABNr = NULL WHERE LTRIM(RTRIM(ABNr)) = N'';
UPDATE dbo.FaHierarchyOrderInfos SET Pos = NULL WHERE LTRIM(RTRIM(Pos)) = N'';");

            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_FaHierarchyNodes_Schluessel' AND object_id = OBJECT_ID(N'dbo.FaHierarchyNodes'))
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.FaHierarchyNodes GROUP BY HauptFA, VaterFA, SubFA, Position, Artnr HAVING COUNT(*) > 1)
    BEGIN
        RAISERROR(N'Doppelte FA-Struktur-Schluessel in dbo.FaHierarchyNodes - Pruefung/FaStrukturSchluesselDoppelt (P-AB1). SQL/159 abgebrochen, Index nicht angelegt.', 16, 1);
    END
    ELSE
        CREATE UNIQUE INDEX [UX_FaHierarchyNodes_Schluessel] ON [dbo].[FaHierarchyNodes]([HauptFA],[VaterFA],[SubFA],[Position],[Artnr]);
END");

            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_FaHierarchyOrderInfos_Schluessel' AND object_id = OBJECT_ID(N'dbo.FaHierarchyOrderInfos'))
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.FaHierarchyOrderInfos GROUP BY HauptFA, ABNr, Pos HAVING COUNT(*) > 1)
    BEGIN
        RAISERROR(N'Doppelte FA-Struktur-Schluessel in dbo.FaHierarchyOrderInfos (HauptFA, ABNr, Pos; P-AB2). SQL/159 abgebrochen, Index nicht angelegt.', 16, 1);
    END
    ELSE
        CREATE UNIQUE INDEX [UX_FaHierarchyOrderInfos_Schluessel] ON [dbo].[FaHierarchyOrderInfos]([HauptFA],[ABNr],[Pos]);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_FaHierarchyOrderInfos_Schluessel",
                schema: "dbo",
                table: "FaHierarchyOrderInfos");

            migrationBuilder.DropIndex(
                name: "UX_FaHierarchyNodes_Schluessel",
                schema: "dbo",
                table: "FaHierarchyNodes");

            migrationBuilder.DropColumn(
                name: "ErstGesehen",
                schema: "dbo",
                table: "ProductionOrderPositions");

            migrationBuilder.DropColumn(
                name: "VerschwundenGrund",
                schema: "dbo",
                table: "ProductionOrderPositions");

            migrationBuilder.DropColumn(
                name: "VerschwundenSeit",
                schema: "dbo",
                table: "ProductionOrderPositions");

            migrationBuilder.DropColumn(
                name: "ZuletztGesehen",
                schema: "dbo",
                table: "ProductionOrderPositions");

            migrationBuilder.DropColumn(
                name: "ErstGesehen",
                schema: "dbo",
                table: "FaHierarchyOrderInfos");

            migrationBuilder.DropColumn(
                name: "VerschwundenGrund",
                schema: "dbo",
                table: "FaHierarchyOrderInfos");

            migrationBuilder.DropColumn(
                name: "VerschwundenSeit",
                schema: "dbo",
                table: "FaHierarchyOrderInfos");

            migrationBuilder.DropColumn(
                name: "ZuletztGesehen",
                schema: "dbo",
                table: "FaHierarchyOrderInfos");

            migrationBuilder.DropColumn(
                name: "ErstGesehen",
                schema: "dbo",
                table: "FaHierarchyNodes");

            migrationBuilder.DropColumn(
                name: "VerschwundenGrund",
                schema: "dbo",
                table: "FaHierarchyNodes");

            migrationBuilder.DropColumn(
                name: "VerschwundenSeit",
                schema: "dbo",
                table: "FaHierarchyNodes");

            migrationBuilder.DropColumn(
                name: "ZuletztGesehen",
                schema: "dbo",
                table: "FaHierarchyNodes");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <summary>
    /// Arbeitsgang-Stammdaten vereinheitlichen (Spec 2026-10-09, Antworten 1/2/8): Tabelle OseonOperationConfigs ->
    /// OperationConfigs, Spalte IsOseonRelevant -> IsExternalSystemRelevant, PK/Index umbenannt, UQ-Constraint der Skript-/
    /// FreshInstall-Form konvergiert auf den Unique-Index IX_OperationConfigs_OperationName, Audit-Felder (Bestand 'system').
    /// <para>Up()/Down() bewusst NUR guarded migrationBuilder.Sql (je Aufruf ein Batch, kein GO): EF generierte hier
    /// DropTable+CreateTable (Datenverlust) bzw. haette RenameIndex auf eine UQ-Form-DB losgelassen (SqlError 3701/15248,
    /// fallstricke §9). Snapshot/Designer stammen unveraendert aus `migrations add`. Identische Batches in
    /// SQL/95_RenameOperationConfigsAndAddAudit.sql (Drift-Waechter OperationConfigsMigrationSqlTests).</para>
    /// </summary>
    public partial class RenameOperationConfigsAndAddAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // B1 Tabelle umbenennen; existieren alte UND neue Tabelle, abbrechen (melden statt still)
            migrationBuilder.Sql(@"IF OBJECT_ID(N'dbo.OseonOperationConfigs', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.OperationConfigs', N'U') IS NOT NULL
                    THROW 50001, N'OseonOperationConfigs und OperationConfigs existieren beide - manuell klaeren, keine automatische Umbenennung.', 1;
                IF OBJECT_ID(N'dbo.OseonOperationConfigs', N'U') IS NOT NULL
                    EXEC sp_rename N'dbo.OseonOperationConfigs', N'OperationConfigs';");

            // B2 Primaerschluessel (beide DB-Formen heissen gleich)
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'PK_OseonOperationConfigs' AND parent_object_id = OBJECT_ID(N'dbo.OperationConfigs'))
                    EXEC sp_rename N'dbo.PK_OseonOperationConfigs', N'PK_OperationConfigs', N'OBJECT';");

            // B3 Spalte IsOseonRelevant -> IsExternalSystemRelevant
            migrationBuilder.Sql(@"IF COL_LENGTH(N'dbo.OperationConfigs', N'IsOseonRelevant') IS NOT NULL AND COL_LENGTH(N'dbo.OperationConfigs', N'IsExternalSystemRelevant') IS NULL
                    EXEC sp_rename N'dbo.OperationConfigs.IsOseonRelevant', N'IsExternalSystemRelevant', N'COLUMN';");

            // B4 EF-Form: Unique-Index umbenennen (muss vor B5 laufen)
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OseonOperationConfigs_OperationName' AND object_id = OBJECT_ID(N'dbo.OperationConfigs'))
                    EXEC sp_rename N'dbo.OperationConfigs.IX_OseonOperationConfigs_OperationName', N'IX_OperationConfigs_OperationName', N'INDEX';");

            // B5 Skript-/FreshInstall-Form: Ziel-Index anlegen, falls noch keiner da ist
            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OperationConfigs_OperationName' AND object_id = OBJECT_ID(N'dbo.OperationConfigs'))
                    CREATE UNIQUE INDEX [IX_OperationConfigs_OperationName] ON [dbo].[OperationConfigs] ([OperationName]);");

            // B6 ... und erst danach den alten UQ-Constraint entfernen (keine Luecke in der Eindeutigkeit)
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_OseonOperationConfigs_OperationName' AND parent_object_id = OBJECT_ID(N'dbo.OperationConfigs'))
                    ALTER TABLE [dbo].[OperationConfigs] DROP CONSTRAINT [UQ_OseonOperationConfigs_OperationName];");

            // B7 Audit-Spalten nullable anlegen
            migrationBuilder.Sql(@"IF COL_LENGTH(N'dbo.OperationConfigs', N'CreatedAt') IS NULL         ALTER TABLE [dbo].[OperationConfigs] ADD [CreatedAt] DATETIME2 NULL;
                IF COL_LENGTH(N'dbo.OperationConfigs', N'CreatedBy') IS NULL         ALTER TABLE [dbo].[OperationConfigs] ADD [CreatedBy] NVARCHAR(200) NULL;
                IF COL_LENGTH(N'dbo.OperationConfigs', N'CreatedByWindows') IS NULL  ALTER TABLE [dbo].[OperationConfigs] ADD [CreatedByWindows] NVARCHAR(200) NULL;
                IF COL_LENGTH(N'dbo.OperationConfigs', N'ModifiedAt') IS NULL        ALTER TABLE [dbo].[OperationConfigs] ADD [ModifiedAt] DATETIME2 NULL;
                IF COL_LENGTH(N'dbo.OperationConfigs', N'ModifiedBy') IS NULL        ALTER TABLE [dbo].[OperationConfigs] ADD [ModifiedBy] NVARCHAR(200) NULL;
                IF COL_LENGTH(N'dbo.OperationConfigs', N'ModifiedByWindows') IS NULL ALTER TABLE [dbo].[OperationConfigs] ADD [ModifiedByWindows] NVARCHAR(200) NULL;");

            // B8 Bestand befuellen (eigener Batch: referenziert die Spalten aus B7)
            migrationBuilder.Sql(@"UPDATE [dbo].[OperationConfigs] SET [CreatedAt] = GETDATE() WHERE [CreatedAt] IS NULL;
                UPDATE [dbo].[OperationConfigs] SET [CreatedBy] = N'system' WHERE [CreatedBy] IS NULL;
                UPDATE [dbo].[OperationConfigs] SET [CreatedByWindows] = N'system' WHERE [CreatedByWindows] IS NULL;");

            // B9 Pflichtspalten NOT NULL (Guard is_nullable)
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OperationConfigs') AND name = N'CreatedAt' AND is_nullable = 1)
                    ALTER TABLE [dbo].[OperationConfigs] ALTER COLUMN [CreatedAt] DATETIME2 NOT NULL;
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OperationConfigs') AND name = N'CreatedBy' AND is_nullable = 1)
                    ALTER TABLE [dbo].[OperationConfigs] ALTER COLUMN [CreatedBy] NVARCHAR(200) NOT NULL;
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OperationConfigs') AND name = N'CreatedByWindows' AND is_nullable = 1)
                    ALTER TABLE [dbo].[OperationConfigs] ALTER COLUMN [CreatedByWindows] NVARCHAR(200) NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // D1 Audit-Spalten entfernen
            migrationBuilder.Sql(@"IF COL_LENGTH(N'dbo.OperationConfigs', N'ModifiedByWindows') IS NOT NULL ALTER TABLE [dbo].[OperationConfigs] DROP COLUMN [ModifiedByWindows];
                IF COL_LENGTH(N'dbo.OperationConfigs', N'ModifiedBy') IS NOT NULL        ALTER TABLE [dbo].[OperationConfigs] DROP COLUMN [ModifiedBy];
                IF COL_LENGTH(N'dbo.OperationConfigs', N'ModifiedAt') IS NOT NULL        ALTER TABLE [dbo].[OperationConfigs] DROP COLUMN [ModifiedAt];
                IF COL_LENGTH(N'dbo.OperationConfigs', N'CreatedByWindows') IS NOT NULL  ALTER TABLE [dbo].[OperationConfigs] DROP COLUMN [CreatedByWindows];
                IF COL_LENGTH(N'dbo.OperationConfigs', N'CreatedBy') IS NOT NULL         ALTER TABLE [dbo].[OperationConfigs] DROP COLUMN [CreatedBy];
                IF COL_LENGTH(N'dbo.OperationConfigs', N'CreatedAt') IS NOT NULL         ALTER TABLE [dbo].[OperationConfigs] DROP COLUMN [CreatedAt];");

            // D2 Index zurueck auf den EF-Namen der Ursprungsmigration (bewusst ohne Rueckbau zum UQ-Constraint)
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OperationConfigs_OperationName' AND object_id = OBJECT_ID(N'dbo.OperationConfigs'))
                    EXEC sp_rename N'dbo.OperationConfigs.IX_OperationConfigs_OperationName', N'IX_OseonOperationConfigs_OperationName', N'INDEX';");

            // D3 Spalte zurueck
            migrationBuilder.Sql(@"IF COL_LENGTH(N'dbo.OperationConfigs', N'IsExternalSystemRelevant') IS NOT NULL AND COL_LENGTH(N'dbo.OperationConfigs', N'IsOseonRelevant') IS NULL
                    EXEC sp_rename N'dbo.OperationConfigs.IsExternalSystemRelevant', N'IsOseonRelevant', N'COLUMN';");

            // D4 Primaerschluessel zurueck
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'PK_OperationConfigs' AND parent_object_id = OBJECT_ID(N'dbo.OperationConfigs'))
                    EXEC sp_rename N'dbo.PK_OperationConfigs', N'PK_OseonOperationConfigs', N'OBJECT';");

            // D5 Tabelle zurueck
            migrationBuilder.Sql(@"IF OBJECT_ID(N'dbo.OperationConfigs', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.OseonOperationConfigs', N'U') IS NULL
                    EXEC sp_rename N'dbo.OperationConfigs', N'OseonOperationConfigs';");
        }
    }
}

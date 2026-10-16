using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// DRIFT-WAECHTER fuer Quelle/SageMissingSince der Arbeitsgang-Stammdaten (Spec 2026-10-16-arbeitsgang-stammdaten-abgleich-aus-
/// sage-view, AK 18/21; Muster <see cref="OperationConfigsMigrationSqlTests"/>). Die Bestandsbefuellung muss auf allen drei
/// Deploy-Wegen (EF-Migration, <c>SQL/96</c>, FreshInstall-EXEC) wortgleich sein. Beweist NICHT, dass die Skripte auf SQL Server
/// laufen und idempotent sind — das ist Manual-UAT (TS-94.11/18/19).
/// </summary>
public class OperationConfigSourceMigrationSqlTests
{
    private const string MigrationId = "20261016090641_AddOperationConfigSourceAndSageMissingSince";
    private static readonly string Root;

    static OperationConfigSourceMigrationSqlTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        Root = dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
    }

    private static string Read(params string[] p) => File.ReadAllText(Path.Combine(new[] { Root }.Concat(p).ToArray()));
    private static string Migration => Read("IdealAkeWms", "Migrations", MigrationId + ".cs");
    private static string Sql96 => Read("SQL", "96_AddOperationConfigSourceAndSageMissingSince.sql");
    private static string Fresh => Read("SQL", "00_FreshInstall.sql");
    private static string Norm(string s) => Regex.Replace(s, @"\s+", " ").Trim();

    private static string Befuellung()
    {
        var up = Migration[..Migration.IndexOf("protected override void Down", StringComparison.Ordinal)];
        var batches = Regex.Matches(up, @"migrationBuilder\.Sql\(@""(.*?)""\);", RegexOptions.Singleline)
            .Select(m => Norm(m.Groups[1].Value.Replace("\"\"", "\""))).ToList();
        batches.Should().ContainSingle("die Migration hat genau einen SQL-Batch: die Bestandsbefuellung");
        return batches[0];
    }

    [Fact]
    public void Befuellung_StehtWortgleichInSql96_UndImFreshInstallExec()
    {
        var befuellung = Befuellung();
        befuellung.Should().Contain("SET [Source] = N'ExternalSystem' WHERE [CreatedBy] = N'system' AND [Source] = N'Manual'");
        Norm(Sql96).Should().Contain(befuellung);
        var exec = Regex.Match(Fresh, @"EXEC\(N'(UPDATE \[dbo\]\.\[OperationConfigs\] SET \[Source\].*?;)'\);").Groups[1].Value;
        Norm(exec.Replace("''", "'")).Should().Be(befuellung, "FreshInstall-EXEC = Migration (Seed-Zeilen -> ExternalSystem)");
    }

    [Fact]
    public void Migration_NurAddColumn_UndDropColumn()
    {
        foreach (var op in new[] { "RenameTable(", "RenameColumn(", "AlterColumn(", "DropTable(", "CreateTable(", "CreateIndex(", "DropIndex(" })
            Migration.Should().NotContain("migrationBuilder." + op);
        Regex.Matches(Migration, @"migrationBuilder\.AddColumn<").Count.Should().Be(2);
        Regex.Matches(Migration, @"migrationBuilder\.DropColumn\(").Count.Should().Be(2);
    }

    [Fact]
    public void Sql96_HaeltVorSql95_UndTraegtDieMigrationId()
    {
        var noexecOn = Sql96.IndexOf("SET NOEXEC ON;", StringComparison.Ordinal);
        var firstAlter = Sql96.IndexOf("ALTER TABLE", StringComparison.Ordinal);
        var history = Sql96.IndexOf("INSERT INTO dbo.__EFMigrationsHistory", StringComparison.Ordinal);
        var noexecOff = Sql96.LastIndexOf("SET NOEXEC OFF;", StringComparison.Ordinal);
        (noexecOn > 0 && noexecOn < firstAlter && history < noexecOff).Should().BeTrue("Guard vor der DDL, NOEXEC OFF am Ende");
        Sql96.Should().Contain("COL_LENGTH(N'dbo.OperationConfigs', N'Source') IS NULL")
            .And.Contain("COL_LENGTH(N'dbo.OperationConfigs', N'SageMissingSince') IS NULL");
        Regex.Matches(Sql96, Regex.Escape($"'{MigrationId}'")).Count.Should().Be(2, "Existenzpruefung + Insert");
    }

    [Fact]
    public void FreshInstall_LegtBeideSpaltenAn_UndTraegtDieMigrationIdNurBeiVorhandenenSpalten()
    {
        Fresh.Should().Contain("[Source]                   NVARCHAR(20)   NOT NULL CONSTRAINT [DF_OperationConfigs_Source] DEFAULT N'Manual'")
            .And.Contain("[SageMissingSince]         DATETIME2      NULL");
        Regex.Matches(Fresh, Regex.Escape($"'{MigrationId}'")).Count.Should().Be(2);
        var historyBlock = Fresh[Fresh.IndexOf($"'{MigrationId}'", StringComparison.Ordinal)..];
        historyBlock[..historyBlock.IndexOf("INSERT", StringComparison.Ordinal)]
            .Should().Contain("COL_LENGTH(N'dbo.OperationConfigs', N'SageMissingSince') IS NOT NULL");
    }
}

using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// DRIFT-WAECHTER der Marker-Tabelle DeployScriptRuns (Spec 2026-11-11-update-skript-prueft-und-fuehrt-aus-mit-bestaetigung, v1.99.0,
/// AK 17; Muster <see cref="OperationConfigsMigrationSqlTests"/>). Drei Deploy-Wege (EF-Migration, <c>SQL/142</c>, <c>00_FreshInstall.sql</c>
/// an beiden Stellen) muessen dieselben Namen tragen; SQL/142 und FreshInstall mit Guards (ADR 0004). Beweist NICHT, dass die Batches auf
/// SQL Server laufen und idempotent sind - das ist TS-126 (Update-Skript spielt SQL/142 auf der DEV-Kopie ein).
/// </summary>
public class DeployScriptRunsMigrationSqlTests
{
    private const string MigrationId = "20261111174202_AddDeployScriptRuns";
    private static readonly string Root;

    static DeployScriptRunsMigrationSqlTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SQL", "00_FreshInstall.sql"))) dir = dir.Parent;
        Root = dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
    }

    private static string Read(params string[] p) => File.ReadAllText(Path.Combine(new[] { Root }.Concat(p).ToArray()));

    private static readonly string[] Namen =
    {
        "DeployScriptRuns", "PK_DeployScriptRuns", "UX_DeployScriptRuns_ManifestId", "ManifestId", "FileName", "FileSha256", "Mode", "Hits",
        "Output", "PackageVersion", "PackageCommit", "ExecutedAt", "ExecutedBy", "Note",
    };

    [Fact]
    public void Migration_Sql142_FreshInstall_TragenDieselbenNamen()
    {
        foreach (var (datei, text) in new[] { ("Migration", Read("IdealAkeWms", "Migrations", MigrationId + ".cs")),
                     ("SQL/142", Read("SQL", "142_AddDeployScriptRuns.sql")), ("FreshInstall", Read("SQL", "00_FreshInstall.sql")) })
            foreach (var name in Namen) text.Should().Contain(name, $"{datei} muss {name} nennen");
    }

    [Fact]
    public void Sql142_UndFreshInstall_MitGuards_UndMigrationId()
    {
        var sql = Read("SQL", "142_AddDeployScriptRuns.sql");
        sql.Should().Contain("IF OBJECT_ID(N'[dbo].[DeployScriptRuns]', N'U') IS NULL").And.Contain("FROM sys.indexes WHERE name = N'UX_DeployScriptRuns_ManifestId'")
            .And.Contain($"MigrationId = '{MigrationId}'").And.Contain("CREATE UNIQUE INDEX");
        var fresh = Read("SQL", "00_FreshInstall.sql");
        fresh.Should().Contain("IF OBJECT_ID(N'[dbo].[DeployScriptRuns]', N'U') IS NULL");
        fresh.Should().Contain($"VALUES ('{MigrationId}', '10.0.2')", "FreshInstall Stelle 2: MigrationId");
        fresh.IndexOf("CREATE TABLE [dbo].[DeployScriptRuns]", StringComparison.Ordinal)
            .Should().BeLessThan(fresh.IndexOf("18. EF Migrations History", StringComparison.Ordinal), "Stelle 1: Schema vor der History");
    }
}

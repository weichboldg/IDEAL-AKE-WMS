using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// DRIFT-WAECHTER E3b (Spec 2026-11-06-sage-sdata-stammdaten-sync, ADR 0004): TextValue auf allen drei Deploy-Wegen (EF-Migration,
/// <c>SQL/100</c>, FreshInstall an zwei Stellen). Beweist NICHT, dass das Skript auf SQL Server laeuft – Manual-UAT (TS-106.4).
/// </summary>
public class ArticleAttributeTextValueMigrationSqlTests
{
    private const string MigrationId = "20261106185149_AddArticleAttributeValueTextValue";
    private static readonly string Root;

    static ArticleAttributeTextValueMigrationSqlTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        Root = dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
    }

    private static string Read(params string[] p) => File.ReadAllText(Path.Combine(new[] { Root }.Concat(p).ToArray()));

    [Fact]
    public void Migration_NurTextValueNvarchar1000()
    {
        var m = Read("IdealAkeWms", "Migrations", MigrationId + ".cs");
        Regex.Matches(m, @"migrationBuilder\.\w+").Select(x => x.Value).Should().Equal("migrationBuilder.AddColumn", "migrationBuilder.DropColumn");
        m.Should().Contain("name: \"TextValue\"").And.Contain("table: \"ArticleAttributeValues\"").And.Contain("type: \"nvarchar(1000)\"");
    }

    [Fact]
    public void Sql100_GuardDdlUndHistoryInEigenenBatches()
    {
        var sql = Read("SQL", "100_AddArticleAttributeValueTextValue.sql");
        var batches = Regex.Split(sql, @"^GO\s*$", RegexOptions.Multiline);
        var ddl = batches.Single(b => b.Contains("ALTER TABLE"));
        ddl.Should().Contain("COL_LENGTH(N'dbo.ArticleAttributeValues', N'TextValue') IS NULL").And.Contain("[TextValue] NVARCHAR(1000) NULL");
        ddl.Should().NotContain("__EFMigrationsHistory");
        batches.Single(b => b.Contains("INSERT INTO dbo.__EFMigrationsHistory")).Should().Contain($"'{MigrationId}'");
        sql.IndexOf("SET NOEXEC ON;", StringComparison.Ordinal).Should().BeLessThan(sql.IndexOf("ALTER TABLE", StringComparison.Ordinal));
    }

    [Fact]
    public void FreshInstall_SpalteUndMigrationId()
    {
        var fresh = Read("SQL", "00_FreshInstall.sql");
        var tabelle = fresh[fresh.IndexOf("CREATE TABLE [dbo].[ArticleAttributeValues]", StringComparison.Ordinal)..];
        tabelle[..tabelle.IndexOf(");", StringComparison.Ordinal)].Should().MatchRegex(@"\[TextValue\]\s+NVARCHAR\(1000\)\s+NULL");
        Regex.Matches(fresh, Regex.Escape($"'{MigrationId}'")).Count.Should().Be(2, "Existenzpruefung + Insert");
        fresh.Should().Contain("COL_LENGTH(N'dbo.ArticleAttributeValues', N'TextValue') IS NOT NULL");
    }
}

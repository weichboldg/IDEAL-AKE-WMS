using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// DRIFT-WAECHTER (Quelltext) fuer die Migrations-Disziplin (ADR 0004) bei Teil 3 des Beschichtungsauftrag-Ausbaus:
/// EF-Migration (App-Start <c>Migrate()</c>), <c>SQL/94_AddBeschichtungsauftrag.sql</c> (Deploy) und
/// <c>SQL/00_FreshInstall.sql</c> muessen dieselben Objektnamen (PK/FK/IX) und dieselbe MigrationId tragen — sonst
/// legen die beiden Deploy-Wege unterschiedliche Schemas an und eine spaetere Migration scheitert (fallstricke
/// „EF-Migration UND idempotentes SQL-Skript"). Was der Test NICHT beweist: dass das Skript auf SQL Server laeuft und
/// idempotent ist — das ist Manual-UAT (TS-83).
/// </summary>
public class BeschichtungsauftragMigrationSqlTests
{
    private const string MigrationId = "20261012073551_AddBeschichtungsauftrag";
    private static readonly string Root;

    static BeschichtungsauftragMigrationSqlTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        Root = dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
    }

    private static string Read(params string[] p) => File.ReadAllText(Path.Combine(new[] { Root }.Concat(p).ToArray()));

    [Fact]
    public void Migration_Sql94_FreshInstall_HabenDieselbenObjektnamen_UndMigrationId()
    {
        var migration = Read("IdealAkeWms", "Migrations", MigrationId + ".cs");
        var sql94 = Read("SQL", "94_AddBeschichtungsauftrag.sql");
        var fresh = Read("SQL", "00_FreshInstall.sql");

        var namen = Regex.Matches(migration, @"""((?:PK|FK|IX)_[A-Za-z_]+)""").Select(m => m.Groups[1].Value).Distinct().ToList();
        namen.Should().HaveCount(11, "2 PK + 2 FK + 7 IX laut EF-Migration — aendert sich die Migration, diesen Test nachziehen")
            .And.Contain("IX_Beschichtungsauftraege_Nummer");

        foreach (var name in namen)
        {
            sql94.Should().Contain($"[{name}]", $"SQL/94 muss {name} genauso nennen wie die EF-Migration");
            fresh.Should().Contain($"[{name}]", $"00_FreshInstall muss {name} genauso nennen wie die EF-Migration");
        }
        foreach (var tabelle in new[] { "Beschichtungsauftraege", "BeschichtungsauftragPositionen" })
        {
            sql94.Should().Contain($"OBJECT_ID(N'dbo.{tabelle}', N'U') IS NULL");
            fresh.Should().Contain($"OBJECT_ID(N'dbo.{tabelle}', N'U') IS NULL");
        }
        sql94.Should().Contain($"'{MigrationId}'");
        Regex.Matches(fresh, Regex.Escape($"'{MigrationId}'")).Count.Should().Be(2, "Existenzpruefung + Insert in der History");
    }
}

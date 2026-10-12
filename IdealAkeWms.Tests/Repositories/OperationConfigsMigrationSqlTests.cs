using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// DRIFT-WAECHTER der Umbenennung OseonOperationConfigs → OperationConfigs (Spec 2026-10-09-arbeitsgang-stammdaten-
/// vereinheitlichen, v1.54.0, AK 19; Muster <see cref="BeschichtungsauftragMigrationSqlTests"/>). Drei Deploy-Wege
/// (EF-Migration beim App-Start, <c>SQL/95</c>, <c>00_FreshInstall.sql</c>) muessen dieselben Namen ergeben, und die
/// Migration darf KEINE strukturierte Operation enthalten (EF erzeugte DropTable+CreateTable = Datenverlust bzw. RenameIndex
/// auf UQ-Form-DBs = SqlError 3701/15248, fallstricke §9). Beweist NICHT, dass die Batches auf SQL Server laufen und
/// idempotent sind — das ist Manual-UAT (TS-87.9).
/// </summary>
public class OperationConfigsMigrationSqlTests
{
    private const string MigrationId = "20261012140548_RenameOperationConfigsAndAddAudit";
    private static readonly string Root;

    static OperationConfigsMigrationSqlTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        Root = dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
    }

    private static string Read(params string[] p) => File.ReadAllText(Path.Combine(new[] { Root }.Concat(p).ToArray()));
    private static string Migration => Read("IdealAkeWms", "Migrations", MigrationId + ".cs");
    private static string Sql95 => Read("SQL", "95_RenameOperationConfigsAndAddAudit.sql");
    private static string Fresh => Read("SQL", "00_FreshInstall.sql");
    private static string Snapshot => Read("IdealAkeWms", "Migrations", "ApplicationDbContextModelSnapshot.cs");

    private static readonly string[] Namen =
    {
        "OperationConfigs", "PK_OperationConfigs", "IX_OperationConfigs_OperationName", "IsExternalSystemRelevant",
        "CreatedAt", "CreatedBy", "CreatedByWindows", "ModifiedAt", "ModifiedBy", "ModifiedByWindows",
    };

    [Fact]
    public void Migration_Sql95_FreshInstall_TragenDieNeuenNamen()
    {
        foreach (var (datei, text) in new[] { ("Migration", Migration), ("SQL/95", Sql95), ("FreshInstall", Fresh) })
            foreach (var name in Namen)
                text.Should().Contain(name, $"{datei} muss {name} nennen");
    }

    [Fact]
    public void Migration_HatKeineStrukturierteOperation_NurGuardedSql()
    {
        foreach (var op in new[] { "RenameTable(", "RenameColumn(", "RenameIndex(", "DropPrimaryKey(", "AddPrimaryKey(", "AddColumn(",
                     "AlterColumn(", "DropTable(", "CreateTable(", "DropIndex(", "CreateIndex(", "DropColumn(" })
            Migration.Should().NotContain("migrationBuilder." + op);
        Regex.Matches(Migration, @"migrationBuilder\.Sql\(").Count.Should().Be(14, "9 Up-Batches (B1-B9) + 5 Down-Batches (D1-D5)");
    }

    [Fact]
    public void JederUpBatch_StehtWortgleichInSql95()
    {
        static string Norm(string s) => Regex.Replace(s, @"\s+", " ").Trim();
        var up = Migration[..Migration.IndexOf("protected override void Down", StringComparison.Ordinal)];
        var batches = Regex.Matches(up, @"migrationBuilder\.Sql\(@""(.*?)""\);", RegexOptions.Singleline)
            .Select(m => Norm(m.Groups[1].Value.Replace("\"\"", "\""))).ToList();
        batches.Should().HaveCount(9);
        var sql = Norm(Sql95);
        foreach (var b in batches) sql.Should().Contain(b, "SQL/95 und die EF-Migration muessen dieselben Batches ausfuehren");
    }

    [Fact]
    public void Snapshot_KenntNurNochDieNeueTabelle()
    {
        Snapshot.Should().Contain("b.ToTable(\"OperationConfigs\", \"dbo\")").And.Contain("\"IsExternalSystemRelevant\"")
            .And.NotContain("\"IdealAkeWms.Models.OseonOperationConfig\"");
    }

    [Fact]
    public void FreshInstall_LegtNurDenZielzustandAn_UndTraegtDieMigrationId()
    {
        Fresh.Should().NotContain("CREATE TABLE [dbo].[OseonOperationConfigs]").And.NotContain("INSERT INTO [dbo].[OseonOperationConfigs]")
            .And.NotContain("UQ_OperationConfigs").And.Contain("CREATE TABLE [dbo].[OperationConfigs]");
        Regex.Matches(Fresh, Regex.Escape($"'{MigrationId}'")).Count.Should().Be(2, "Existenzpruefung + Insert in der History");
        Regex.Matches(Sql95, Regex.Escape($"'{MigrationId}'")).Count.Should().Be(2, "Existenzpruefung + Insert in SQL/95");
        Migration.Should().NotContain("UQ_OperationConfigs");
    }
}

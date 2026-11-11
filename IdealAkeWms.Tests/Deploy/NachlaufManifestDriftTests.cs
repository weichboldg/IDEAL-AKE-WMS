using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Deploy;

/// <summary>
/// DRIFT-WAECHTER des Nachlauf-Manifests <c>scripts/deploy/nachlauf.psd1</c> (Spec 2026-11-09-update-skript-je-zielsystem-mit-nachlauf,
/// v1.98.0, AK 25-27/32; Muster <see cref="Repositories.OperationConfigsMigrationSqlTests"/>). Liest das Manifest als Text (kein
/// PowerShell im Test). Ein vergessener Manifest-Eintrag faellt damit im naechsten Dev-Lauf auf, nicht erst am Zielserver.
/// Beweist NICHT, dass das Update-Skript auf einem Server richtig laeuft - das ist TS-125.
/// </summary>
public class NachlaufManifestDriftTests
{
    private const string VorManifestMigration = "20260806120650_AllowMultipleDummyRequisitionItems";   // SQL/88

    /// <summary>Bewusst nicht im Manifest (Freigabe-Antwort 4, Gerald 2026-11-11): ein Lauf heute loeschte echte Eingaben.</summary>
    private static readonly string[] NichtImManifest = { "2026-09-28_Hotfix-1.30.1_ResetAutosaveZeroQuantityPicked.sql" };

    /// <summary>Deploy-Reihenfolge der Schema-Skripte (ADR 0004 Nachtrag, Spec-Tabelle), nie Dateisortierung.</summary>
    private static readonly string[] SchemaReihenfolge =
    {
        "SQL/89", "SQL/90", "SQL/91", "SQL/92", "SQL/93", "SQL/94", "SQL/95", "SQL/96", "SQL/97", "SQL/98", "SQL/99", "SQL/100", "SQL/101",
        "SQL/102", "SQL/103", "SQL/104", "SQL/105", "SQL/106", "SQL/107", "SQL/109", "SQL/112", "SQL/113", "SQL/133", "SQL/140", "SQL/141",
    };

    private static readonly string Root;

    static NachlaufManifestDriftTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "scripts", "deploy", "nachlauf.psd1"))) dir = dir.Parent;
        Root = dir?.FullName ?? throw new FileNotFoundException("Repository-Root mit scripts/deploy/nachlauf.psd1 nicht gefunden");
    }

    private static string ManifestText => File.ReadAllText(Path.Combine(Root, "scripts", "deploy", "nachlauf.psd1"));

    /// <summary>Eintraege als Feld-Woerterbuch: jeder Block beginnt mit "@{ Id='...'".</summary>
    internal static List<Dictionary<string, string>> Eintraege(string text) =>
        Regex.Split(text, @"(?m)^\s*(?=@\{ Id=)").Where(b => b.TrimStart().StartsWith("@{ Id=")).Select(b =>
            Regex.Matches(b, @"(\w+)='((?:[^']|'')*)'").ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.Replace("''", "'"))).ToList();

    private static List<string> MigrationIdsNachSql88() =>
        Directory.GetFiles(Path.Combine(Root, "IdealAkeWms", "Migrations"), "*.Designer.cs")
            .Select(f => Regex.Match(File.ReadAllText(f), @"\[Migration\(""([^""]+)""\)\]").Groups[1].Value)
            .Where(id => string.CompareOrdinal(id, VorManifestMigration) > 0).OrderBy(id => id, StringComparer.Ordinal).ToList();

    /// <summary>Fehlende Eintraege: jede Migration nach SQL/88, jede SQL/&lt;Nr&gt;_*.sql ab 89, jede SQL/Einmalig/*.sql.</summary>
    internal static List<string> Fehlend(string text)
    {
        var e = Eintraege(text);
        var ids = e.Where(x => x.ContainsKey("MigrationId")).Select(x => x["MigrationId"]).ToHashSet();
        var dateien = e.Where(x => x.ContainsKey("Datei")).Select(x => x["Datei"].Replace('\\', '/')).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sql = Directory.GetFiles(Path.Combine(Root, "SQL"), "*.sql")
            .Select(Path.GetFileName).Where(n => int.TryParse(n!.Split('_')[0], out var nr) && nr >= 89).Select(n => n!);
        var einmalig = Directory.GetFiles(Path.Combine(Root, "SQL", "Einmalig"), "*.sql")
            .Select(Path.GetFileName).Where(n => !NichtImManifest.Contains(n)).Select(n => "Einmalig/" + n);
        return MigrationIdsNachSql88().Where(id => !ids.Contains(id))
            .Concat(sql.Concat(einmalig).Where(d => !dateien.Contains(d))).ToList();
    }

    [Fact]
    public void Manifest_EnthaeltJedeMigrationUndJedesSkript() => Fehlend(ManifestText).Should().BeEmpty();

    [Fact]
    public void Manifest_EntfernterEintrag_MachtWaechterRot()
    {
        var ohne141 = Regex.Replace(ManifestText, @"(?ms)^@\{ Id='SQL/141'.*?\}\r?$", "");
        ohne141.Should().NotBe(ManifestText);
        Fehlend(ohne141).Should().Contain("20261108071811_AddPickingOrderAltbestandPruefen").And.Contain("141_AddPickingOrderAltbestandPruefen.sql");
    }

    [Fact]
    public void Manifest_HotfixEinmalSkript_NichtEnthalten() =>
        ManifestText.Should().NotContain("Datei='Einmalig\\2026-09-28_Hotfix-1.30.1", "Freigabe-Antwort 4: Hotfix-1.30.1 entfaellt ueberall");

    [Fact]
    public void Manifest_DateienExistierenUndEnthaltenIhreMigrationId()
    {
        foreach (var x in Eintraege(ManifestText).Where(x => x.ContainsKey("Datei")))
        {
            var pfad = Path.Combine(Root, "SQL", x["Datei"].Replace('\\', Path.DirectorySeparatorChar));
            File.Exists(pfad).Should().BeTrue($"{x["Id"]}: {pfad}");
            if (x.ContainsKey("MigrationId")) File.ReadAllText(pfad).Should().Contain(x["MigrationId"], x["Id"]);
        }
    }

    [Fact]
    public void Manifest_SchemaInDeployReihenfolge_UndAllesAutomatisch()
    {
        var schema = Eintraege(ManifestText).Where(x => x.GetValueOrDefault("Typ") == "Schema").ToList();
        schema.Select(x => x["Id"]).Should().Equal(SchemaReihenfolge);
        schema.Should().OnlyContain(x => x["Ausfuehrung"] == "automatisch" && x["Phase"] == "Schema" && x["Firma"] == "beide");
    }

    [Fact]
    public void Manifest_DatenUndEinmalSkripte_NieAutomatisch()
    {
        var nichtSchema = Eintraege(ManifestText).Where(x => x.ContainsKey("Datei") && x["Typ"] != "Schema").ToList();
        nichtSchema.Select(x => x["Id"]).Should().Contain(new[] { "SQL/108", "SQL/110" });
        nichtSchema.Should().OnlyContain(x => x["Ausfuehrung"] == "anzeigen" || x["Ausfuehrung"] == "Trockenlauf");
        foreach (var x in nichtSchema.Where(x => x["Ausfuehrung"] == "Trockenlauf"))
            File.ReadAllText(Path.Combine(Root, "SQL", x["Datei"].Replace('\\', Path.DirectorySeparatorChar)))
                .Should().Contain("DECLARE @Trockenlauf BIT = 1;", x["Id"]);
    }

    [Fact]
    public void Manifest_IdsEindeutig_FelderGueltig()
    {
        var e = Eintraege(ManifestText);
        e.Select(x => x["Id"]).Should().OnlyHaveUniqueItems();
        e.Should().OnlyContain(x => new[] { "AKE", "IDEAL", "beide" }.Contains(x["Firma"]));
        e.Should().OnlyContain(x => new[] { "Vorher", "Schema", "Haltepunkt", "Nachher" }.Contains(x["Phase"]));
        e.Select(x => Version.TryParse(x["Version"], out var v) ? v : null).Should().NotContainNulls();
    }

    [Fact]
    public void DeploySkripte_NurAscii_OhneSage_RechteNurHinzufuegend()
    {
        foreach (var f in Directory.GetFiles(Path.Combine(Root, "scripts", "deploy")))
        {
            var bytes = File.ReadAllBytes(f);
            bytes.Should().OnlyContain(b => b < 0x80, $"{Path.GetFileName(f)}: PS 5.1 liest Dateien ohne BOM als ANSI");
            var text = Encoding.ASCII.GetString(bytes);
            if (Path.GetFileName(f) is "Update-IdealAkeWms.ps1" or "IdealAkeWms.Deploy.psm1")
                text.Should().NotContainAny("SageConnection", "SData", "Sage100");
            text.Should().NotContainAny("/reset", "/remove", "/inheritance:r", "/setowner");
        }
    }

    /// <summary>F3 (v1.98.1): die Url aus ziele.psd1 hat Vorrang vor der IIS-Bindung - jedes der 5 Systeme hat eine gueltige http/https-Url.</summary>
    [Fact]
    public void Ziele_AlleFuenfSystemeMitGueltigerHttpUrl()
    {
        var ziele = Regex.Matches(File.ReadAllText(Path.Combine(Root, "scripts", "deploy", "ziele.psd1")), @"(?m)^\s*@\{ Firma=.*$")
            .Select(z => Regex.Matches(z.Value, @"(\w+)='([^']*)'").ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value)).ToList();
        ziele.Select(z => z["Firma"] + "-" + z["System"]).Should().BeEquivalentTo("IDEAL-Prod", "IDEAL-Test", "AKE-Prod", "AKE-Test", "AKE-Test-IDEAL");
        foreach (var z in ziele)
        {
            z.Should().ContainKey("Url", $"{z["Firma"]}-{z["System"]}");
            Uri.TryCreate(z["Url"], UriKind.Absolute, out var u).Should().BeTrue(z["Url"]);
            u!.Scheme.Should().BeOneOf(new[] { "http", "https" }, z["Url"]);
            u.Host.Should().NotBeNullOrEmpty();
        }
    }
}

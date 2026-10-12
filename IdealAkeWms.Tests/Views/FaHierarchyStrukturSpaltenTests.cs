using System.Text.RegularExpressions;
using FluentAssertions;
using IdealAkeWms.Models.ViewModels;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>
/// DRIFT-WAECHTER der FA-Struktur-Baumtabelle (<c>FaHierarchy/Index.cshtml</c> + <c>_FaHierarchyNode.cshtml</c>), angelegt mit
/// der Spalte „Arbeitsgänge" (Spec 2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1, v1.52.0). Eine Baumspalte lebt an
/// sechs Stellen: Kopf-th, Filterzeilen-th, Knotenzeilen-td, #column-config, <see cref="ColumnDefinitions.FaHierarchyStructure"/>
/// und colspan der Struktur-Kopfzeilen; jeder Baum-Filter braucht sein <c>data-f-&lt;key&gt;</c> an der Knotenzeile.
/// Aussehen ist Manual-UAT (TS-85).
/// </summary>
public class FaHierarchyStrukturSpaltenTests
{
    private static readonly string Index;
    private static readonly string Node;

    static FaHierarchyStrukturSpaltenTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        var views = Path.Combine(dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden"), "IdealAkeWms", "Views", "FaHierarchy");
        Index = File.ReadAllText(Path.Combine(views, "Index.cshtml"));
        Node = File.ReadAllText(Path.Combine(views, "_FaHierarchyNode.cshtml"));
    }

    private static List<string> Definition => ColumnDefinitions.FaHierarchyStructure.Columns.Select(c => c.Key).ToList();

    [Fact]
    public void KopfFilterzeileKonfigUndDefinition_GleicheSpalten()
    {
        var split = Index.IndexOf("<tr class=\"filter-row\">", StringComparison.Ordinal);
        split.Should().BeGreaterThan(0);
        var theadEnd = Index.IndexOf("</thead>", split, StringComparison.Ordinal);

        KommissionierSpaltenKonsistenzTests.ExtractThDataColKeys(Index[..split]).Should().Equal(Definition, "Kopfzeile");
        KommissionierSpaltenKonsistenzTests.ExtractThDataColKeys(Index[split..theadEnd]).Should().Equal(Definition, "Filterzeile");
        KommissionierSpaltenKonsistenzTests.ExtractColumnConfigKeys(Index).Should().Equal(Definition, "#column-config");
        Definition.Should().Contain("operations");
    }

    [Fact]
    public void Knotenzeile_HatJeSpalteEineZelle()
        => Regex.Matches(Node, @"<td data-col-key=""([^""]+)""").Select(m => m.Groups[1].Value).Should().Equal(Definition);

    [Fact]
    public void StrukturKopfzeilen_ColspanGleichSpaltenzahl()
        => Regex.Matches(Index, @"<td colspan=""(\d+)""").Select(m => int.Parse(m.Groups[1].Value))
            .Should().NotBeEmpty().And.OnlyContain(c => c == Definition.Count);

    [Fact]
    public void JederBaumFilter_HatSeinDatenattributAnDerKnotenzeile()
    {
        var keys = Regex.Matches(Index, @"data-filter-key=""([^""]+)""").Select(m => m.Groups[1].Value).ToList();
        keys.Should().Contain("operations");
        foreach (var k in keys)
            Node.Should().Contain($"data-f-{k}=\"", $"der Baum-Filter '{k}' liest data-f-{k} (fa-hierarchy-tree.js)");
    }
}

using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// v1.99.83 (Spec 2026-11-26 FA-Struktur neu, Teil 2, Variante D): Ausduennen „Teile ausblenden" (D-5), Filterkarte findet die
/// Struktur weiter ueber eine Teilenummer, Kopfspalten deckungsgleich mit der FA-Liste (D-2). Aussehen, Sticky-Spalten,
/// Popover = Manual-UAT TS-204.
/// </summary>
public class FaStrukturVarianteDTests
{
    private static FaHierarchyNode N(int sub, int? vater, string artnr, string? schritte = null) =>
        new() { HauptFA = 100, SubFA = sub, VaterFA = vater, Artnr = artnr, Arbeitsschritte = schritte };

    // 100 (Wurzel) -> 101 (Baugruppe) -> Teil T1 (ohne Schritte), Teil T2 (mit Schritten); 100 -> Teil T3 (ohne Schritte)
    private static readonly List<FaHierarchyNode> Knoten = new()
    {
        N(100, null, "ROOT"), N(101, 100, "BG"),
        N(0, 101, "T1"), N(0, 101, "T2", "Laser;Kanten"), N(0, 100, "T3", "  ")
    };

    private static List<FaHierarchyStructure> Bauen() =>
        new FaHierarchyTreeBuilder(NullLogger<FaHierarchyTreeBuilder>.Instance).Build(Knoten, new List<FaHierarchyOrderInfo>(), 10);

    private static IEnumerable<FaHierarchyTreeNode> Alle(FaHierarchyTreeNode n) => new[] { n }.Concat(n.Children.SelectMany(Alle));

    [Fact]
    public void TeileAusblenden_EntferntBlaetterOhneSchritte_BehaeltTeileMitSchritten()
    {
        var structures = Bauen();
        var vorher = Alle(structures.Single().Roots.Single()).Count();

        FaHierarchyTreeBuilder.TeileAusblenden(structures);

        var s = structures.Single();
        Alle(s.Roots.Single()).Select(x => x.Node.Artnr).Should().BeEquivalentTo("ROOT", "BG", "T2");
        s.AusgeblendeteTeile.Should().Be(2);
        (vorher - s.AusgeblendeteTeile).Should().Be(3, "Zaehler „n Knoten“ = NodeCount - AusgeblendeteTeile");
    }

    [Fact]
    public void OhneAusduennen_AlleKnotenBleiben()
    {
        var s = Bauen().Single();
        Alle(s.Roots.Single()).Should().HaveCount(5);
        s.AusgeblendeteTeile.Should().Be(0);
    }

    [Fact]
    public void Filterkarte_FindetStrukturUeberTeilenummer()
        => FaHierarchyController.FilterHauptFas(Knoten, new List<FaHierarchyOrderInfo>(), null, null, "T1")
            .Should().BeEquivalentTo(new[] { 100 });

    [Fact]
    public void Kopfspalten_KeysUndLabelsWieFaListe_AlleSichtbar()
    {
        var kopf = new[] { "customer", "pps-status", "konstruktions-termin", "production-date", "montage-abteilung", "ab-nummer",
            "prio", "delivery-date", "ab-position", "dienstleister", "ral", "beschichtung-retour", "neuer-pt-pps",
            "montagestunden", "bemerkung-uhrzeit" };
        var struktur = ColumnDefinitions.FaHierarchyStructure.Columns.Where(c => kopf.Contains(c.Key)).ToList();
        var liste = ColumnDefinitions.ProductionOrders.Columns.Where(c => kopf.Contains(c.Key)).ToDictionary(c => c.Key, c => c.Label);

        struktur.Select(c => c.Key).Should().Equal(kopf);
        struktur.Should().OnlyContain(c => liste[c.Key] == c.Label && !c.DefaultHidden && !c.Locked);
    }
}

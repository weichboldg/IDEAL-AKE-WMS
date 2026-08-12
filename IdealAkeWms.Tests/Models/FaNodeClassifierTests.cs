using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Xunit;

namespace IdealAkeWms.Tests.Models;

/// <summary>
/// Praesentative Typ-Icon-Klassifikation der FA-Struktur-Baumtabelle (IDEAL Teil 2, Etappe 7).
/// Geordnete Praezedenzkette: Wurzel -> Baugruppe -> Zukaufteil -> Material.
/// </summary>
public class FaNodeClassifierTests
{
    private static FaHierarchyNode Node(int? vaterFa, int subFa, bool beschaffung) =>
        new FaHierarchyNode { VaterFA = vaterFa, SubFA = subFa, Beschaffungsartikel = beschaffung };

    [Fact]
    public void Root_WhenVaterFaIsNull()
    {
        FaNodeClassifier.Classify(Node(null, 5, false)).Should().Be(FaNodeIconType.Root);
    }

    [Fact]
    public void Root_WinsOverAssembly_WhenRootAlsoHasSubFa()
    {
        // Eine Wurzel ist typischerweise zugleich Baugruppe (SubFA != 0) — Wurzel gewinnt.
        FaNodeClassifier.Classify(Node(null, 42, false)).Should().Be(FaNodeIconType.Root);
    }

    [Fact]
    public void Assembly_WhenNotRootAndHasSubFa()
    {
        FaNodeClassifier.Classify(Node(1, 7, false)).Should().Be(FaNodeIconType.Assembly);
    }

    [Fact]
    public void Assembly_WinsOverPurchased_WhenSubFaSetAndBeschaffung()
    {
        FaNodeClassifier.Classify(Node(1, 7, true)).Should().Be(FaNodeIconType.Assembly);
    }

    [Fact]
    public void Purchased_WhenLeafAndBeschaffungsartikel()
    {
        FaNodeClassifier.Classify(Node(1, 0, true)).Should().Be(FaNodeIconType.Purchased);
    }

    [Fact]
    public void Material_WhenLeafAndNotBeschaffungsartikel()
    {
        FaNodeClassifier.Classify(Node(1, 0, false)).Should().Be(FaNodeIconType.Material);
    }
}

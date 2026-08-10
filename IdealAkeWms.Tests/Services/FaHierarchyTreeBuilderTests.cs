using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class FaHierarchyTreeBuilderTests
{
    private static FaHierarchyTreeBuilder NewBuilder() =>
        new FaHierarchyTreeBuilder(NullLogger<FaHierarchyTreeBuilder>.Instance);

    private static FaHierarchyNode Node(int hauptFa, int? vaterFa, int subFa, string? artnr = null) =>
        new FaHierarchyNode { HauptFA = hauptFa, VaterFA = vaterFa, SubFA = subFa, Artnr = artnr };

    private static FaHierarchyOrderInfo Info(int hauptFa, string? montage, string? kunde = null) =>
        new FaHierarchyOrderInfo { HauptFA = hauptFa, MontageAbteilung = montage, Kunde = kunde };

    [Fact]
    public void Build_Multilevel_RecursesBeyondTwoLevels()
    {
        // HauptFA 100: Wurzel(1) -> Kind(2) -> Enkel(Blatt 0)
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, null, 1, "ROOT"),
            Node(100, 1, 2, "SUB"),
            Node(100, 2, 0, "LEAF"),
        };

        var structures = NewBuilder().Build(nodes, new List<FaHierarchyOrderInfo>(), 500);

        structures.Should().HaveCount(1);
        var s = structures[0];
        s.HasError.Should().BeFalse();
        s.IsOrphan.Should().BeFalse();
        s.NodeCount.Should().Be(3);

        var root = s.Roots.Single();
        root.Level.Should().Be(0);
        var sub = root.Children.Single();
        sub.Level.Should().Be(1);
        var leaf = sub.Children.Single();
        leaf.Level.Should().Be(2);
        leaf.Node.Artnr.Should().Be("LEAF");
        leaf.HasChildren.Should().BeFalse();
    }

    [Fact]
    public void Build_Cycle_AbortsOnlyThatStructure_AndMarksError()
    {
        // Zyklische Struktur (HauptFA 100): 1 -> 2 -> zurueck auf 1
        var cyclic = new List<FaHierarchyNode>
        {
            Node(100, null, 1),
            Node(100, 1, 2),
            Node(100, 2, 1), // Zeiger zurueck auf SubFA 1 -> Zyklus
        };
        // Intakte Struktur (HauptFA 200)
        var healthy = new List<FaHierarchyNode>
        {
            Node(200, null, 10),
            Node(200, 10, 0),
        };

        var structures = NewBuilder().Build(cyclic.Concat(healthy).ToList(), new List<FaHierarchyOrderInfo>(), 500);

        structures.Should().HaveCount(2);

        var bad = structures.Single(s => s.HauptFA == 100);
        bad.HasError.Should().BeTrue();
        bad.ErrorMessage.Should().Contain("Zyklus");
        bad.Roots.Should().NotBeEmpty("die betroffene Struktur wird markiert, nicht leer dargestellt");

        var good = structures.Single(s => s.HauptFA == 200);
        good.HasError.Should().BeFalse("ein Verstoss bricht nur die betroffene Struktur ab");
        good.NodeCount.Should().Be(2);
    }

    [Fact]
    public void Build_Orphan_BecomesOwnMarkedPseudoRoot()
    {
        // VaterFA 999 existiert in der Gruppe nicht -> Waise
        var nodes = new List<FaHierarchyNode>
        {
            Node(300, 999, 5, "ORPHAN"),
            Node(300, 5, 0, "ORPHAN-CHILD"),
        };

        var structures = NewBuilder().Build(nodes, new List<FaHierarchyOrderInfo>(), 500);

        structures.Should().HaveCount(1);
        var s = structures[0];
        s.IsOrphan.Should().BeTrue();
        s.OrphanVaterFa.Should().Be(999);
        s.HasError.Should().BeFalse();
        // Waise samt ihrem Kind wird als eigene Struktur gerendert
        s.Roots.Single().Node.Artnr.Should().Be("ORPHAN");
        s.Roots.Single().Children.Single().Node.Artnr.Should().Be("ORPHAN-CHILD");
    }

    [Fact]
    public void Build_DepthCapExceeded_AbortsStructure_AndMarksError()
    {
        // Kette tiefer als der Cap: 1 -> 2 -> 3 ...
        var nodes = new List<FaHierarchyNode>
        {
            Node(400, null, 1),
            Node(400, 1, 2),
            Node(400, 2, 3),
            Node(400, 3, 4),
        };

        var structures = NewBuilder().Build(nodes, new List<FaHierarchyOrderInfo>(), depthCap: 2);

        structures.Should().HaveCount(1);
        var s = structures[0];
        s.HasError.Should().BeTrue();
        s.ErrorMessage.Should().Contain("Tiefen-Cap");
        s.Roots.Should().NotBeEmpty();
    }

    [Fact]
    public void Build_MultipleMontageAbteilungen_MarksStructureAmbiguous()
    {
        var nodes = new List<FaHierarchyNode> { Node(500, null, 1) };
        var infos = new List<FaHierarchyOrderInfo>
        {
            Info(500, "Kälte"),
            Info(500, "Elektro"),
        };

        var s = NewBuilder().Build(nodes, infos, 500).Single();

        s.IsAmbiguous.Should().BeTrue();
        s.OrderInfos.Should().HaveCount(2);
    }

    [Fact]
    public void Build_SingleMontageAbteilung_NotAmbiguous()
    {
        var nodes = new List<FaHierarchyNode> { Node(600, null, 1) };
        var infos = new List<FaHierarchyOrderInfo> { Info(600, "Kälte", "Kunde A") };

        var s = NewBuilder().Build(nodes, infos, 500).Single();

        s.IsAmbiguous.Should().BeFalse();
        s.OrderInfos.Single().Kunde.Should().Be("Kunde A");
    }

    [Fact]
    public void Build_OrdersStructuresByHauptFa_RootBeforeOrphan()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(20, null, 1),      // HauptFA 20 Wurzel
            Node(10, null, 1),      // HauptFA 10 Wurzel
            Node(10, 777, 2),       // HauptFA 10 Waise (nach der Wurzel)
        };

        var structures = NewBuilder().Build(nodes, new List<FaHierarchyOrderInfo>(), 500);

        structures.Should().HaveCount(3);
        structures[0].HauptFA.Should().Be(10);
        structures[0].IsOrphan.Should().BeFalse();
        structures[1].HauptFA.Should().Be(10);
        structures[1].IsOrphan.Should().BeTrue();
        structures[2].HauptFA.Should().Be(20);
    }
}

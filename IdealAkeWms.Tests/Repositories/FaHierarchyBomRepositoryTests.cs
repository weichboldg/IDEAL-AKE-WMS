using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class FaHierarchyBomRepositoryTests
{
    // Struktur HauptFA 100 (Wurzel SubFA=100, HauptArtnr DEV, eigene Artnr bewusst abweichend
    // "DEV-ROOTROW" — so faellt auf, wenn der Kopf faelschlich die Artnr der Wurzelzeile nimmt):
    //   Pos 3  -> Sub-FA 1043421 (Artnr A1, Baugruppe)
    //       Pos 7 -> Sub-FA 1043422 (Artnr A2, Baugruppe)
    //           Pos 2 -> Blatt L1 (Sollmenge 5, Komm. "PG", Lagerplatz "R1")
    //       Pos 2 -> Blatt L2
    //   Pos 5  -> Blatt L3 (Beschaffungsartikel)
    private static List<FaHierarchyNode> Sample() => new()
    {
        N(100, null, 100, pos: null, artnr: "DEV-ROOTROW", hauptArtnr: "DEV"),
        N(100, 100, 1043421, pos: 3, artnr: "A1"),
        N(100, 1043421, 1043422, pos: 7, artnr: "A2"),
        N(100, 1043422, 0, pos: 2, artnr: "L1", sollmenge: 5m, komm: "PG", lager: "R1"),
        N(100, 1043421, 0, pos: 2, artnr: "L2"),
        N(100, 100, 0, pos: 5, artnr: "L3", beschaffung: true),
    };

    private static FaHierarchyNode N(int hauptFa, int? vaterFa, int subFa, int? pos, string artnr,
        string? hauptArtnr = null, decimal sollmenge = 1m, string? komm = null, string? lager = null,
        bool beschaffung = false, string? artikelgruppe = null) => new()
    {
        HauptFA = hauptFa, VaterFA = vaterFa, SubFA = subFa, Position = pos, Artnr = artnr,
        HauptArtnr = hauptArtnr ?? "DEV", Bezeichnung1 = "B1-" + artnr, Bezeichnung2 = "B2-" + artnr,
        Sollmenge = sollmenge, Kommissionieren = komm, Hauptlagerplatz = lager,
        Beschaffungsartikel = beschaffung, Artikelgruppe = artikelgruppe ?? "940 - Kleinmaterial",
    };

    private static FaHierarchyBomRepository Sut(List<FaHierarchyNode> nodes,
        ILogger<FaHierarchyBomRepository>? logger = null)
    {
        var repo = new Mock<IFaHierarchyNodeRepository>();
        repo.Setup(r => r.GetByHauptFaAsync(100)).ReturnsAsync(nodes);
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(nodes);
        return new FaHierarchyBomRepository(repo.Object,
            new FaHierarchyTreeBuilder(NullLogger<FaHierarchyTreeBuilder>.Instance),
            logger ?? NullLogger<FaHierarchyBomRepository>.Instance);
    }

    /// <summary>Zaehlt Warnungen, deren formatierte Nachricht <paramref name="fragment"/> enthaelt.</summary>
    private static int WarningsContaining(Mock<ILogger<FaHierarchyBomRepository>> logger, string fragment) =>
        logger.Invocations.Count(i =>
            i.Method.Name == nameof(ILogger.Log)
            && (LogLevel)i.Arguments[0] == LogLevel.Warning
            && i.Arguments[2]?.ToString()?.Contains(fragment, StringComparison.Ordinal) == true);

    [Fact]
    public async Task DirectChildren_OfSubFa_MapsFields_AndFlagsBaugruppe()
    {
        var r = await Sut(Sample()).GetBomItemsAsync(new BomKey("A1", "1043421", "100"), BomScope.DirectChildren);

        r.DataSource.Should().Be(BomDataSources.FaHierarchie);
        r.MengeIstAuftragsmenge.Should().BeTrue();
        r.Items.Should().HaveCount(2);
        var bg = r.Items.OfType<FaHierarchyBomItem>().Single(i => i.Ressourcenummer == "A2");
        bg.IsBaugruppe.Should().BeTrue();
        bg.Position.Should().Be("7");
        bg.SagePosition.Should().Be("7");
        bg.Artikelnummer.Should().Be("A1");          // Kopf = Artikel des Eltern-Sub-FA
        bg.Baugruppe.Should().Be("A1");
        bg.VaterSubFa.Should().Be("1043421");
        bg.Ebene.Should().Be(1);
        var leaf = r.Items.OfType<FaHierarchyBomItem>().Single(i => i.Ressourcenummer == "L2");
        leaf.IsBaugruppe.Should().BeFalse();
        leaf.Beschaffungsartikel.Should().Be("Nein");
        leaf.Bezeichnung1.Should().Be("B1-L2");
        leaf.Artikelgruppe.Should().Be("940 - Kleinmaterial");
    }

    [Fact]
    public async Task DirectChildren_OfRoot_UsesHauptArtnrAsHeader_AndSollmenge()
    {
        var r = await Sut(Sample()).GetBomItemsAsync(new BomKey("DEV", "100", "100"), BomScope.DirectChildren);

        r.Items.Should().HaveCount(2);
        var l3 = r.Items.OfType<FaHierarchyBomItem>().Single(i => i.Ressourcenummer == "L3");
        // Wurzel: HauptArtnr gewinnt gegen die abweichende Artnr der Wurzelzeile ("DEV-ROOTROW")
        l3.Artikelnummer.Should().Be("DEV");
        l3.Baugruppe.Should().Be("DEV");
        l3.Beschaffungsartikel.Should().Be("Ja");
        l3.Menge.Should().Be(1m);
    }

    [Fact]
    public async Task FullStructure_BuildsRecursivePaths_EveryNodeOnce()
    {
        var r = await Sut(Sample()).GetBomItemsAsync(new BomKey("DEV", "100", "100"), BomScope.FullStructure);

        var items = r.Items.Cast<FaHierarchyBomItem>().ToList();
        items.Should().HaveCount(5); // alle Nicht-Wurzel-Knoten genau einmal
        items.Select(i => i.Position).Should().BeEquivalentTo(new[] { "3", "3.7", "3.7.2", "3.2", "5" });
        items.Single(i => i.Ressourcenummer == "L1").SagePosition.Should().Be("2");   // Sage-Position bleibt sichtbar
        items.Single(i => i.Ressourcenummer == "L1").Ebene.Should().Be(3);
        items.Single(i => i.Ressourcenummer == "L1").VaterSubFa.Should().Be("1043422");
        items.Single(i => i.Ressourcenummer == "L1").Kommissionieren.Should().Be("PG");
        items.Single(i => i.Ressourcenummer == "L1").Hauptlagerplatz.Should().Be("R1");
        items.Single(i => i.Ressourcenummer == "L1").Menge.Should().Be(5m);
        // Baugruppe = Artnr des unmittelbaren Elternknotens
        items.Single(i => i.Ressourcenummer == "L1").Baugruppe.Should().Be("A2");
        // ... an der Wurzel ist das die HauptArtnr, nicht die Artnr der Wurzelzeile ("DEV-ROOTROW")
        items.Single(i => i.Ressourcenummer == "A1").Baugruppe.Should().Be("DEV");
        items.Single(i => i.Ressourcenummer == "A1").Artikelnummer.Should().Be("DEV");
    }

    [Fact]
    public async Task FullStructure_SamePositionUnderDifferentParents_YieldsDistinctPaths()
    {
        // L1 (unter 1043422) und L2 (unter 1043421) tragen beide Sage-Position 2 -> "3.7.2" vs "3.2".
        var r = await Sut(Sample()).GetBomItemsAsync(new BomKey("DEV", "100", "100"), BomScope.FullStructure);

        r.Items.Select(i => i.Position).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task FullStructure_CollisionAmongSiblings_IsMarkedNotOverwritten()
    {
        var nodes = Sample();
        nodes.Add(N(100, 100, 0, pos: 5, artnr: "L4")); // zweites Wurzel-Kind mit Position 5

        var r = await Sut(nodes).GetBomItemsAsync(new BomKey("DEV", "100", "100"), BomScope.FullStructure);

        var items = r.Items.Cast<FaHierarchyBomItem>().ToList();
        items.Should().HaveCount(6);
        items.Select(i => i.Position).Should().OnlyHaveUniqueItems();
        items.Count(i => i.PositionsKollision).Should().Be(1);
        items.Single(i => i.PositionsKollision).Position.Should().Be("5~2");
    }

    [Fact]
    public async Task DirectChildren_CollisionAmongSiblings_IsMarkedNotOverwritten()
    {
        var nodes = Sample();
        nodes.Add(N(100, 1043421, 0, pos: 2, artnr: "L5")); // zweites Kind von 1043421 mit Position 2
        var logger = new Mock<ILogger<FaHierarchyBomRepository>>();

        var r = await Sut(nodes, logger.Object).GetBomItemsAsync(new BomKey("A1", "1043421", "100"), BomScope.DirectChildren);

        var items = r.Items.Cast<FaHierarchyBomItem>().ToList();
        items.Should().HaveCount(3);
        items.Select(i => i.Position).Should().OnlyHaveUniqueItems();
        items.Count(i => i.PositionsKollision).Should().Be(1);
        items.Single(i => i.PositionsKollision).Position.Should().Be("2~2");
        items.Single(i => i.PositionsKollision).SagePosition.Should().Be("2"); // Sage-Position bleibt sichtbar
        // Freigabe-Bedingung (b): markieren UND protokollieren — auch auf dem DirectChildren-Pfad
        WarningsContaining(logger, "Positions-Kollision unter Sub-FA 1043421").Should().Be(1);
    }

    [Fact]
    public async Task Collision_SuffixAssignment_IsDeterministic_RegardlessOfInputOrder()
    {
        // Eingangsreihenfolge kommt aus SQL Server und ist bei Gleichstand unspezifiziert. Ohne
        // deterministischen Tiebreak koennten "5" und "5~2" nach Cache-Ablauf tauschen — beide
        // PickingItem-Zeilen (Schluessel BomArticleNumber + BomPosition) verloeren ihren Pick-Zustand.
        var forward = Sample();
        forward.Add(N(100, 100, 0, pos: 5, artnr: "L4")); // zweites Wurzel-Kind mit Position 5
        var reversed = Enumerable.Reverse(forward).ToList();

        foreach (var nodes in new[] { forward, reversed })
        {
            var full = await Sut(nodes).GetBomItemsAsync(new BomKey("DEV", "100", "100"), BomScope.FullStructure);
            full.Items.Cast<FaHierarchyBomItem>().Single(i => i.Position == "5~2")
                .Ressourcenummer.Should().Be("L4");
            full.Items.Cast<FaHierarchyBomItem>().Single(i => i.Ressourcenummer == "L3")
                .Position.Should().Be("5");

            var direct = await Sut(nodes).GetBomItemsAsync(new BomKey("DEV", "100", "100"), BomScope.DirectChildren);
            direct.Items.Cast<FaHierarchyBomItem>().Single(i => i.Position == "5~2")
                .Ressourcenummer.Should().Be("L4");
        }
    }

    [Fact]
    public async Task FullStructure_SecondRealRoot_IsLoggedAsWarning()
    {
        var nodes = Sample();
        nodes.Add(N(100, null, 200, pos: null, artnr: "DEV2", hauptArtnr: "DEV")); // zweite echte Wurzel
        var logger = new Mock<ILogger<FaHierarchyBomRepository>>();

        var r = await Sut(nodes, logger.Object).GetBomItemsAsync(new BomKey("DEV", "100", "100"), BomScope.FullStructure);

        r.Items.Should().HaveCount(5); // Verhalten unveraendert: nur die abgefragte Wurzel
        WarningsContaining(logger, "HauptFA 100 hat 2 Wurzeln").Should().Be(1);
    }

    [Fact]
    public async Task FullStructure_LeafOrphan_IsAppendedMarked()
    {
        var nodes = Sample();
        nodes.Add(N(100, 999999, 0, pos: 1, artnr: "ORPHAN")); // VaterFA existiert nicht

        var r = await Sut(nodes).GetBomItemsAsync(new BomKey("DEV", "100", "100"), BomScope.FullStructure);

        var orphan = r.Items.Cast<FaHierarchyBomItem>().Single(i => i.Ressourcenummer == "ORPHAN");
        orphan.IsWaise.Should().BeTrue();
        orphan.Position.Should().Be("W1");
        orphan.VaterSubFa.Should().Be("999999");
    }

    [Fact]
    public async Task DirectChildren_Partition_CoversEveryNonRootNodeExactlyOnce()
    {
        // Property (Spec AK 4): Vereinigung aller DirectChildren ueber alle Sub-FAs (inkl. Wurzel) =
        // alle Nicht-Wurzel-Knoten, ohne Doppelung.
        var nodes = Sample();
        var sut = Sut(nodes);
        var parents = nodes.Where(n => n.SubFA != 0).ToList();
        var union = new List<string>();
        foreach (var p in parents)
        {
            var r = await sut.GetBomItemsAsync(new BomKey(p.Artnr!, p.SubFA.ToString(), "100"), BomScope.DirectChildren);
            union.AddRange(r.Items.Select(i => i.Ressourcenummer!));
        }
        union.Should().BeEquivalentTo(nodes.Where(n => n.VaterFA != null).Select(n => n.Artnr));
        union.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task UnknownSubFa_ReturnsEmptyResult_NotException()
    {
        var r = await Sut(Sample()).GetBomItemsAsync(new BomKey("X", "424242", "100"));

        r.Items.Should().BeEmpty();
        r.DataSource.Should().Be(BomDataSources.FaHierarchie);
    }

    [Fact]
    public async Task ReverseLookup_ReturnsParentSubFas_AndSollmengePerParent()
    {
        var nodes = Sample();
        nodes.Add(N(100, 1043421, 0, pos: 9, artnr: "L1", sollmenge: 2m)); // L1 auch unter 1043421

        var sut = Sut(nodes);
        var parents = await sut.GetDeviceArticleNumbersByComponentAsync("L1");
        var menge = await sut.GetComponentMengePerDeviceAsync("L1");

        parents.Should().BeEquivalentTo(new[] { "1043422", "1043421" });
        menge.Should().BeEquivalentTo(new Dictionary<string, decimal> { ["1043422"] = 5m, ["1043421"] = 2m });
    }

    [Fact]
    public async Task WriteMethods_AreNoOps()
    {
        var sut = Sut(Sample());
        await sut.DeleteOrphansAsync(new List<string> { "X" });
        (await sut.GetHeaderHashesAsync(new List<string> { "X" })).Should().BeEmpty();
    }
}

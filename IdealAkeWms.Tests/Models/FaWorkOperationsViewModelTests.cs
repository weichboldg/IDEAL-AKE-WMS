using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IdealAkeWms.Tests.Models;

/// <summary>Spec 2026-09-23 AK 2/9/12: alle materialisierten Knoten, Luecken sichtbar, Zaehler stimmen.</summary>
public class FaWorkOperationsViewModelTests
{
    private static FaHierarchyNode Node(int vaterFa, int subFa) =>
        new() { HauptFA = 100, VaterFA = vaterFa == 0 ? null : vaterFa, SubFA = subFa };

    private static WorkOperation Op(string hauptFa, string subFa, string code, int seq) => new()
    {
        OperationNumber = code, Name = "WB " + code, Sequence = seq,
        ProductionOrder = new ProductionOrder { OrderNumber = hauptFa, SubOrderNumber = subFa },
        ProductionWorkplace = new ProductionWorkplace { SageArbeitsplatznummer = "S-" + code }
    };

    [Fact]
    public void Create_ListsAllMaterializedNodes_CountsGaps_IgnoresLeavesAndForeignStructures()
    {
        // Wurzel 1 (ohne AG) -> Sub 2 (2 AGs), Sub 3 (ohne AG) -> Blatt 0
        var nodes = new List<FaHierarchyNode> { Node(0, 1), Node(1, 2), Node(1, 3), Node(3, 0) };
        var structures = new FaHierarchyTreeBuilder(NullLogger<FaHierarchyTreeBuilder>.Instance)
            .Build(nodes, new List<FaHierarchyOrderInfo>(), 500);
        var ops = new List<WorkOperation>
        {
            Op("100", "2", "SW", 2), Op("100", "2", "KA", 1),
            Op("999", "3", "LA", 1) // gleiche Sub-FA-Nummer, fremde Struktur -> darf nicht zugeordnet werden
        };

        var vm = FaWorkOperationsViewModel.Create(100, structures, ops);

        vm.Entries.Select(e => e.Node.SubFA).Should().Equal(1, 2, 3); // Blatt (SubFA 0) nicht gelistet
        vm.Entries[0].IsRoot.Should().BeTrue();
        vm.Entries[0].Operations.Should().BeEmpty(); // Wurzel ohne AG -> Strich im HauptFA-Abschnitt
        vm.Entries[1].Operations.Select(o => o.OperationNumber).Should().Equal("KA", "SW");
        vm.Entries[1].Operations[0].SageArbeitsplatznummer.Should().Be("S-KA");
        vm.Entries[2].Operations.Should().BeEmpty();
        vm.SubFaCount.Should().Be(2);
        vm.SubFaWithoutOperationsCount.Should().Be(1);
    }
}

using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class BomQuantityResolverTests
{
    private static ProductionOrder Order(decimal quantity) => new()
    {
        OrderNumber = "1", SubOrderNumber = "1", Quantity = quantity,
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
    };

    [Fact]
    public void Flat_MultipliesByOrderQuantity()
    {
        var result = new BomQueryResult(new List<BomItem>(), BomDataSources.Sage);
        var item = new BomItem { Menge = 2.5m };

        BomQuantityResolver.Resolve(result, item, Order(4m)).Should().Be(10m);
    }

    [Fact]
    public void Hierarchical_KeepsSollmengeUnchanged()
    {
        var result = new BomQueryResult(new List<BomItem>(), BomDataSources.FaHierarchie, MengeIstAuftragsmenge: true);
        var item = new BomItem { Menge = 2.5m };

        BomQuantityResolver.Resolve(result, item, Order(4m)).Should().Be(2.5m);
    }
}

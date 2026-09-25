using FluentAssertions;
using IdealAkeWms.Models;
using Xunit;

namespace IdealAkeWms.Tests.Models;

public class ProductionOrderHauptFaTests
{
    [Theory]
    [InlineData("", false)]          // leer gilt als HauptFA (AKE-sichere Richtung)
    [InlineData("100", false)]       // Sub == Order -> HauptFA
    [InlineData("100-7", true)]      // Sub != Order -> Sub-FA
    public void IsSubFa_FollowsIsHauptFa(string sub, bool expected)
    {
        new ProductionOrder { OrderNumber = "100", SubOrderNumber = sub }.IsSubFa.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]        // Projektions-Default: wie "" -> HauptFA
    [InlineData("", false)]
    [InlineData("100", false)]
    [InlineData("100-7", true)]
    public void IsSubFaOf_MatchesEntityRule(string? sub, bool expected)
    {
        ProductionOrder.IsSubFaOf("100", sub).Should().Be(expected);
    }
}

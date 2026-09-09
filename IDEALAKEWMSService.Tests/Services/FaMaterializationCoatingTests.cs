using FluentAssertions;
using IdealAkeWms.Models;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class FaMaterializationCoatingTests
{
    private static FaHierarchyNode N(int hauptFa, int subFa, int? vaterFa, bool beschichtet)
        => new() { HauptFA = hauptFa, SubFA = subFa, VaterFA = vaterFa, Beschichtet = beschichtet };

    [Fact]
    public void Derive_NodeItselfCoated_IsTrue()
        => FaMaterializationCoating.Derive(new[] { N(1, 10, null, true) })["10"].Should().BeTrue();

    [Fact]
    public void Derive_DirectChildCoated_IsTrue()
    {
        var map = FaMaterializationCoating.Derive(new[] { N(1, 10, null, false), N(1, 0, 10, true) });
        map["10"].Should().BeTrue();
    }

    [Fact]
    public void Derive_OnlyGrandchildCoated_IsFalse()
    {
        // 10 -> 20 -> Blatt(beschichtet). Fuer 10 zaehlt nur die DIREKTE Kindebene.
        var map = FaMaterializationCoating.Derive(new[]
        {
            N(1, 10, null, false), N(1, 20, 10, false), N(1, 0, 20, true)
        });
        map["10"].Should().BeFalse();
        map["20"].Should().BeTrue();
    }

    [Fact]
    public void Derive_NothingCoated_IsFalse()
        => FaMaterializationCoating.Derive(new[] { N(1, 10, null, false), N(1, 0, 10, false) })["10"].Should().BeFalse();
}

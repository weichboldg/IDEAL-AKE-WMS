using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class BomCacheCoverageTests
{
    [Fact]
    public void Build_CapHit_SetsCountsAndCapWarning()
    {
        var r = BomCacheCoverage.Build(totalEligibleFas: 643, cachedFas: 500, cap: 500,
            articlesWithoutBom: new List<string>());

        r.Counts["fa im fenster"].Should().Be(643);
        r.Counts["fa gecacht"].Should().Be(500);
        r.Counts["artikel ohne bom"].Should().Be(0);
        r.CapWarning.Should().NotBeNull();
        r.CapWarning.Should().Contain("143 FAs ohne Cache-Eintrag");
        r.NoBomWarning.Should().BeNull();
    }

    [Fact]
    public void Build_WithinCap_NoCapWarning()
    {
        var r = BomCacheCoverage.Build(50, 50, 500, new List<string>());
        r.CapWarning.Should().BeNull();
    }

    [Fact]
    public void Build_ArticlesWithoutBom_SetsWarningAndCount()
    {
        var r = BomCacheCoverage.Build(50, 50, 500, new List<string> { "ART-A", "ART-B" });
        r.Counts["artikel ohne bom"].Should().Be(2);
        r.NoBomWarning.Should().Contain("ART-A").And.Contain("ART-B");
    }

    [Fact]
    public void Build_ManyArticlesWithoutBom_CapsListWithRemainder()
    {
        var many = Enumerable.Range(1, 40).Select(i => $"ART{i}").ToList();
        var r = BomCacheCoverage.Build(10, 10, 500, many);
        r.NoBomWarning.Should().Contain("(+10 weitere)"); // 40 - Cap 30
    }
}

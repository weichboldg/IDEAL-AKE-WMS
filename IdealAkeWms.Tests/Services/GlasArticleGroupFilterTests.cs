using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class GlasArticleGroupFilterTests
{
    private static readonly IReadOnlySet<string> Glas = GlasArticleGroupFilter.ParseGroups("GLAS,SPIEGEL");
    private static readonly IReadOnlySet<string> Shared = GlasArticleGroupFilter.ParseGroups("EUZ");

    [Theory]
    [InlineData("940", WarehouseRequisitionType.Lager, true)]   // normale Gruppe -> Lager ok
    [InlineData("940", WarehouseRequisitionType.Glas, false)]   // normale Gruppe -> kein Glas
    [InlineData("GLAS", WarehouseRequisitionType.Glas, true)]   // Glas-Gruppe -> Glas ok
    [InlineData("GLAS", WarehouseRequisitionType.Lager, false)] // Glas-Gruppe -> im Lager ausgenommen
    [InlineData("SPIEGEL", WarehouseRequisitionType.Glas, true)]
    [InlineData("EUZ", WarehouseRequisitionType.Lager, true)]   // gemeinsame Gruppe -> beide
    [InlineData("EUZ", WarehouseRequisitionType.Glas, true)]
    public void IsAllowedForType_Basisfaelle(string group, WarehouseRequisitionType type, bool expected)
        => GlasArticleGroupFilter.IsAllowedForType(group, type, Glas, Shared).Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsAllowedForType_LeereGruppe_NurLager(string? group)
    {
        GlasArticleGroupFilter.IsAllowedForType(group, WarehouseRequisitionType.Lager, Glas, Shared).Should().BeTrue();
        GlasArticleGroupFilter.IsAllowedForType(group, WarehouseRequisitionType.Glas, Glas, Shared).Should().BeFalse();
    }

    [Theory]
    [InlineData(" glas ")]                 // Trim + Case
    [InlineData("GLAS - Glasteile")]       // Sage-Format "Code - Name"
    [InlineData("glas - glasteile")]
    public void IsAllowedForType_Normalisierung(string group)
        => GlasArticleGroupFilter.IsAllowedForType(group, WarehouseRequisitionType.Glas, Glas, Shared).Should().BeTrue();

    [Fact]
    public void LeereGlasKonfig_GlasZeigtNurGemeinsame_LagerAlles()
    {
        var empty = GlasArticleGroupFilter.ParseGroups("");
        GlasArticleGroupFilter.IsAllowedForType("940", WarehouseRequisitionType.Glas, empty, Shared).Should().BeFalse();
        GlasArticleGroupFilter.IsAllowedForType("EUZ", WarehouseRequisitionType.Glas, empty, Shared).Should().BeTrue();
        GlasArticleGroupFilter.IsAllowedForType("940", WarehouseRequisitionType.Lager, empty, Shared).Should().BeTrue();
        GlasArticleGroupFilter.IsAllowedForType("GLAS", WarehouseRequisitionType.Lager, empty, Shared).Should().BeTrue();
    }

    [Fact]
    public void ParseGroups_TrimtNormalisiertUndIgnoriertLeereTokens()
    {
        var set = GlasArticleGroupFilter.ParseGroups(" glas , ,SPIEGEL - Spiegelteile,euz ");
        set.Should().BeEquivalentTo(new[] { "GLAS", "SPIEGEL", "EUZ" });
    }
}

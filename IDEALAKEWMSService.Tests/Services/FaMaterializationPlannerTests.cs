using FluentAssertions;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class FaMaterializationPlannerTests
{
    private static MaterializationSourceOrder Src(string sub, string order, string? parent = null)
        => new(sub, order, parent, false);
    private static MaterializationExistingOrder Ex(string sub, string order, string? parent = null,
        bool missing = false, bool done = false) => new(sub, order, parent, missing, done);

    [Fact]
    public void LeereQuelle_WirdUebersprungen_NichtsMarkiert()
    {
        var plan = FaMaterializationPlanner.Plan(
            new MaterializationSourceOrder[] { },
            new[] { Ex("100", "100") });
        plan.Skipped.Should().BeTrue();
        plan.ToMarkMissing.Should().BeEmpty();
        plan.ToCreate.Should().BeEmpty();
    }

    [Fact]
    public void Regel1_NeuerSubFa_WirdAngelegt()
    {
        var plan = FaMaterializationPlanner.Plan(
            new[] { Src("100", "100"), Src("101", "100", "100") },
            new[] { Ex("100", "100") });
        plan.ToCreate.Should().BeEquivalentTo(new[] { "101" });
    }

    [Fact]
    public void Regel2_Verschwunden_WirdMarkiert_NieGeloescht_UndGemeldet()
    {
        var plan = FaMaterializationPlanner.Plan(
            new[] { Src("100", "100") },
            new[] { Ex("100", "100"), Ex("101", "100", "100") });
        plan.ToMarkMissing.Should().BeEquivalentTo(new[] { "101" });
        plan.MissingToNotify.Should().BeEquivalentTo(new[] { "101" });
    }

    [Fact]
    public void Regel2_VerschwundenAberErledigt_MarkiertAberNichtGemeldet()
    {
        var plan = FaMaterializationPlanner.Plan(
            new[] { Src("100", "100") },
            new[] { Ex("101", "100", "100", missing: false, done: true) });
        plan.ToMarkMissing.Should().Contain("101");
        plan.MissingToNotify.Should().BeEmpty();
    }

    [Fact]
    public void Regel2_BereitsMarkiert_WirdNichtErneutMarkiert()
    {
        var plan = FaMaterializationPlanner.Plan(
            new[] { Src("100", "100") },
            new[] { Ex("101", "100", "100", missing: true) });
        plan.ToMarkMissing.Should().BeEmpty();
        plan.MissingToNotify.Should().BeEmpty();
    }

    [Fact]
    public void Regel2_WiederAufgetaucht_WirdEntmarkt_Selbstheilend()
    {
        var plan = FaMaterializationPlanner.Plan(
            new[] { Src("100", "100"), Src("101", "100", "100") },
            new[] { Ex("101", "100", "100", missing: true) });
        plan.ToClearMissing.Should().BeEquivalentTo(new[] { "101" });
        plan.ToMarkMissing.Should().BeEmpty();
    }

    [Fact]
    public void Regel3_Umhaengung_WirdGemeldet_NichtUebernommen()
    {
        var plan = FaMaterializationPlanner.Plan(
            new[] { Src("101", "100", "200") },              // Quelle: neuer Vater 200
            new[] { Ex("101", "100", "100") });              // WMS: alter Vater 100
        plan.ReparentConflicts.Should().ContainSingle(c =>
            c.SubOrderNumber == "101" && c.OldParent == "100" && c.NewParent == "200");
    }
}

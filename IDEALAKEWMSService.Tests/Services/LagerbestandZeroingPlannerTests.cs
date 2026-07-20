using System.Collections.Generic;
using FluentAssertions;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class LagerbestandZeroingPlannerTests
{
    // (a) Leerer Sage-Read -> Skip "leer", selbst wenn managedStock verwaiste Paare hat.
    [Fact]
    public void Plan_EmptySageRead_Skips_EvenWithOrphans()
    {
        var present = new HashSet<(int, int)>();
        var managed = new Dictionary<(int, int), decimal> { [(1, 1)] = 5m };

        var plan = LagerbestandZeroingPlanner.Plan(
            sageRowCountRaw: 0, present, managed, maxPerRun: 100);

        plan.Skipped.Should().BeTrue();
        plan.SkipReason.Should().Contain("leer");
        plan.ToZero.Should().BeEmpty();
    }

    // (b) Verwaistes Paar (in managedStock, nicht in sagePresentKeys) -> ToZero (mit Menge).
    [Fact]
    public void Plan_OrphanPair_GoesToZero_WithQuantity()
    {
        var present = new HashSet<(int, int)> { (1, 1) };
        var managed = new Dictionary<(int, int), decimal> { [(1, 1)] = 5m, [(2, 2)] = 3m };

        var plan = LagerbestandZeroingPlanner.Plan(
            sageRowCountRaw: 1, present, managed, maxPerRun: 100);

        plan.Skipped.Should().BeFalse();
        plan.ToZero.Should().ContainSingle();
        plan.ToZero[0].Should().Be((2, 2, 3m));
    }

    // (c) Paar in sagePresentKeys -> NICHT in ToZero.
    [Fact]
    public void Plan_PairPresentInSage_NotZeroed()
    {
        var present = new HashSet<(int, int)> { (1, 1) };
        var managed = new Dictionary<(int, int), decimal> { [(1, 1)] = 5m };

        var plan = LagerbestandZeroingPlanner.Plan(
            sageRowCountRaw: 1, present, managed, maxPerRun: 100);

        plan.Skipped.Should().BeFalse();
        plan.ToZero.Should().BeEmpty();
    }

    // (d) Kandidaten > Cap -> Skip "Cap".
    [Fact]
    public void Plan_CandidatesExceedCap_Skips()
    {
        var present = new HashSet<(int, int)>();
        var managed = new Dictionary<(int, int), decimal>
        {
            [(1, 1)] = 1m, [(2, 2)] = 1m, [(3, 3)] = 1m
        };

        var plan = LagerbestandZeroingPlanner.Plan(
            sageRowCountRaw: 5, present, managed, maxPerRun: 2);

        plan.Skipped.Should().BeTrue();
        plan.SkipReason.Should().Contain("Cap");
        plan.ToZero.Should().BeEmpty();
    }

    // (e) Kandidaten == Cap -> NICHT skipped.
    [Fact]
    public void Plan_CandidatesEqualCap_NotSkipped()
    {
        var present = new HashSet<(int, int)>();
        var managed = new Dictionary<(int, int), decimal> { [(1, 1)] = 1m, [(2, 2)] = 1m };

        var plan = LagerbestandZeroingPlanner.Plan(
            sageRowCountRaw: 5, present, managed, maxPerRun: 2);

        plan.Skipped.Should().BeFalse();
        plan.ToZero.Should().HaveCount(2);
    }
}

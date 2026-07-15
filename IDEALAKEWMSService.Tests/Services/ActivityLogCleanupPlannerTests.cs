using System;
using FluentAssertions;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class ActivityLogCleanupPlannerTests
{
    [Fact]
    public void ComputeCutoff_positive_retention_subtracts_days()
    {
        var now = new DateTime(2026, 07, 15, 10, 0, 0);
        ActivityLogCleanupPlanner.ComputeCutoff(now, 180).Should().Be(now.AddDays(-180));
    }

    [Fact]
    public void ComputeCutoff_zero_retention_is_disabled()
        => ActivityLogCleanupPlanner.ComputeCutoff(new DateTime(2026, 07, 15), 0).Should().BeNull();

    [Fact]
    public void ComputeCutoff_negative_retention_is_disabled()
        => ActivityLogCleanupPlanner.ComputeCutoff(new DateTime(2026, 07, 15), -5).Should().BeNull();
}

using FluentAssertions;
using IdealAkeWms.Services;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class CoatingDateCalculatorTests
{
    private static Mock<IBusinessDayService> MockDays(DateTime raw, DateTime pickup)
    {
        var m = new Mock<IBusinessDayService>();
        m.Setup(s => s.SubtractBusinessDays(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<HashSet<DateTime>>()))
            .Returns(raw);
        m.Setup(s => s.FindPreviousPickupDay(It.IsAny<DateTime>(), It.IsAny<HashSet<DayOfWeek>>()))
            .Returns(pickup);
        return m;
    }

    [Fact]
    public void FeatureInactive_ComputesForAll_RegardlessOfCoatingParts()
    {
        var pickup = new DateTime(2026, 6, 16);
        var m = MockDays(new DateTime(2026, 6, 18), pickup);

        var result = CoatingDateCalculator.Compute(
            new DateTime(2026, 7, 1), 10, new HashSet<DateTime>(), new HashSet<DayOfWeek>(),
            hasCoatingParts: false, featureActive: false, m.Object);

        result.Should().Be(pickup);
    }

    [Fact]
    public void FeatureActive_NoCoatingParts_ReturnsNull()
    {
        var m = MockDays(new DateTime(2026, 6, 18), new DateTime(2026, 6, 16));

        var result = CoatingDateCalculator.Compute(
            new DateTime(2026, 7, 1), 10, new HashSet<DateTime>(), new HashSet<DayOfWeek>(),
            hasCoatingParts: false, featureActive: true, m.Object);

        result.Should().BeNull();
    }

    [Fact]
    public void FeatureActive_WithCoatingParts_ComputesDate()
    {
        var pickup = new DateTime(2026, 6, 16);
        var m = MockDays(new DateTime(2026, 6, 18), pickup);

        var result = CoatingDateCalculator.Compute(
            new DateTime(2026, 7, 1), 10, new HashSet<DateTime>(), new HashSet<DayOfWeek>(),
            hasCoatingParts: true, featureActive: true, m.Object);

        result.Should().Be(pickup);
    }

    [Fact]
    public void NullVorkommissionierTermin_ReturnsNull()
    {
        var m = MockDays(new DateTime(2026, 6, 18), new DateTime(2026, 6, 16));

        var result = CoatingDateCalculator.Compute(
            null, 10, new HashSet<DateTime>(), new HashSet<DayOfWeek>(),
            hasCoatingParts: true, featureActive: false, m.Object);

        result.Should().BeNull();
    }
}

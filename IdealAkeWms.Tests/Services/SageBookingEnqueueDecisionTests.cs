using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IdealAkeWms.Tests.Services;

public class SageBookingEnqueueDecisionTests
{
    [Theory]
    [InlineData(MovementType.Einbuchung, true)]
    [InlineData(MovementType.Ausbuchung, true)]
    [InlineData(MovementType.Umbuchung, false)]
    [InlineData(MovementType.SageEinbuchung, false)]
    [InlineData(MovementType.SageAusbuchung, false)]
    public void IsBookableType_OnlyManualInOut(MovementType type, bool expected)
    {
        SageBookingEnqueueDecision.IsBookableType(type).Should().Be(expected);
    }

    [Fact]
    public void ShouldEnqueue_BothConditionsAndBookable_True()
    {
        SageBookingEnqueueDecision.ShouldEnqueue(MovementType.Einbuchung, globalToggleAktiv: true, locationSageBuchungErlaubt: true)
            .Should().BeTrue();
    }

    [Fact]
    public void ShouldEnqueue_GlobalToggleOff_False()
    {
        SageBookingEnqueueDecision.ShouldEnqueue(MovementType.Einbuchung, globalToggleAktiv: false, locationSageBuchungErlaubt: true)
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldEnqueue_LocationFlagOff_False()
    {
        SageBookingEnqueueDecision.ShouldEnqueue(MovementType.Ausbuchung, globalToggleAktiv: true, locationSageBuchungErlaubt: false)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(MovementType.SageEinbuchung)]
    [InlineData(MovementType.SageAusbuchung)]
    [InlineData(MovementType.Umbuchung)]
    public void ShouldEnqueue_NonBookableType_AlwaysFalse_EvenWithBothOn(MovementType type)
    {
        // AK6 / Feedback-Loop-Schutz: Sage-Korrekturen (und Umbuchung) NIE einreihen.
        SageBookingEnqueueDecision.ShouldEnqueue(type, globalToggleAktiv: true, locationSageBuchungErlaubt: true)
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldEnqueue_MovementOverload_UsesMovementType()
    {
        var sageCorrection = new StockMovement { MovementType = MovementType.SageEinbuchung };
        SageBookingEnqueueDecision.ShouldEnqueue(sageCorrection, globalToggleAktiv: true, locationSageBuchungErlaubt: true)
            .Should().BeFalse();
    }
}

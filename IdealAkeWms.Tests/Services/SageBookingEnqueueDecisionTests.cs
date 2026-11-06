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

    // ----- Umbuchung (v1.73.0 E7, Spec F9) -----

    [Fact]
    public void BookableTypes_DefaultOhneUmbuchung_MitSchalterMit_NieSageKorrektur()
    {
        SageBookingEnqueueDecision.BookableTypes(false).Should().Equal(MovementType.Einbuchung, MovementType.Ausbuchung);
        SageBookingEnqueueDecision.BookableTypes(true).Should().Equal(MovementType.Einbuchung, MovementType.Ausbuchung, MovementType.Umbuchung);
    }

    [Theory]
    [InlineData(true, true, SageBuchungsart.Umbuchung)]
    [InlineData(true, false, SageBuchungsart.Entnahme)]
    [InlineData(false, true, SageBuchungsart.Zugang)]
    [InlineData(false, false, null)]
    public void Buchungsart_Umbuchung_NachPlatzFlags(bool herkunft, bool ziel, SageBuchungsart? erwartet)
    {
        SageBookingEnqueueDecision.Buchungsart(MovementType.Umbuchung, ziel, herkunft).Should().Be(erwartet);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void ShouldEnqueue_Umbuchung_MitSchalter_NachPlatzFlags(bool herkunft, bool ziel, bool erwartet)
    {
        SageBookingEnqueueDecision.ShouldEnqueue(MovementType.Umbuchung, globalToggleAktiv: true, umbuchungAktiv: true, ziel, herkunft)
            .Should().Be(erwartet);
    }

    [Fact]
    public void ShouldEnqueue_Umbuchung_SchalterAus_Nie()
    {
        SageBookingEnqueueDecision.ShouldEnqueue(MovementType.Umbuchung, globalToggleAktiv: true, umbuchungAktiv: false, true, true)
            .Should().BeFalse("Default aus = Verhalten nach Deploy unveraendert");
        SageBookingEnqueueDecision.ShouldEnqueue(MovementType.Umbuchung, globalToggleAktiv: false, umbuchungAktiv: true, true, true)
            .Should().BeFalse("globaler Schalter bleibt kumulativ");
    }

    [Theory]
    [InlineData(MovementType.SageEinbuchung)]
    [InlineData(MovementType.SageAusbuchung)]
    public void ShouldEnqueue_SageKorrektur_AuchMitUmbuchungSchalter_Nie(MovementType type)
    {
        SageBookingEnqueueDecision.ShouldEnqueue(type, true, umbuchungAktiv: true, true, true).Should().BeFalse();
    }

    [Fact]
    public void ShouldEnqueue_EinAusbuchung_HerkunftFlagWirktNicht()
    {
        SageBookingEnqueueDecision.ShouldEnqueue(MovementType.Einbuchung, true, true, locationSageBuchungErlaubt: false, sourceSageBuchungErlaubt: true)
            .Should().BeFalse();
    }
}

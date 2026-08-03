using FluentAssertions;
using IdealAkeWms.Models;
using IDEALAKEWMSService.Services;

namespace IDEALAKEWMSService.Tests.Services;

public class SageLagerbuchungPayloadBuilderTests
{
    private static StockMovement Movement(MovementType type, string kennung = "LL;1;4;0", int? platzId = 42)
        => new()
        {
            Id = 7,
            MovementType = type,
            Quantity = 5m,
            Article = new Article { ArticleNumber = "ART-1" },
            StorageLocation = new StorageLocation
            {
                Code = "LL;1;4;0",
                SageLagerkennung = kennung,
                SageLagerplatzId = platzId
            }
        };

    [Fact]
    public void Build_Einbuchung_ProducesZugang_ZielGesetzt_HerkunftLeer()
    {
        var payload = SageLagerbuchungPayloadBuilder.Build(Movement(MovementType.Einbuchung));

        payload.Standardtext.Should().Be("Zugang Material");
        payload.Memo.Should().Contain("SM#7#");
        var zeile = payload.Lagerbuchungen.Should().ContainSingle().Subject;
        zeile.Lagerbewegungsart.Should().Be("Zugang");
        zeile.Artikelnummer.Should().Be("ART-1");
        zeile.MengeLager.Should().Be(5m);
        zeile.AuspraegungHandle.Should().Be(0);
        // Ziel gesetzt, Herkunft leer/0
        zeile.ZielLagerkennung.Should().Be("LL;1;4;0");
        zeile.ZielLagerplatzId.Should().Be(42);
        zeile.HerkunftLagerkennung.Should().BeEmpty();
        zeile.HerkunftLagerplatzId.Should().Be(0);
        // Serien/Chargen leer, aber je ein Element (Schema-Konformitaet)
        zeile.Seriennummern.Should().ContainSingle();
        zeile.Chargen.Should().ContainSingle();
    }

    [Fact]
    public void Build_Ausbuchung_ProducesEntnahme_HerkunftGesetzt_ZielLeer()
    {
        var payload = SageLagerbuchungPayloadBuilder.Build(Movement(MovementType.Ausbuchung));

        payload.Standardtext.Should().Be("Abgang / Entnahme Material");
        var zeile = payload.Lagerbuchungen.Should().ContainSingle().Subject;
        zeile.Lagerbewegungsart.Should().Be("Entnahme");
        // Spiegelbildlich: Herkunft gesetzt, Ziel leer/0
        zeile.HerkunftLagerkennung.Should().Be("LL;1;4;0");
        zeile.HerkunftLagerplatzId.Should().Be(42);
        zeile.ZielLagerkennung.Should().BeEmpty();
        zeile.ZielLagerplatzId.Should().Be(0);
    }

    [Fact]
    public void Build_MissingSageLagerkennung_ThrowsPayloadException()
    {
        var act = () => SageLagerbuchungPayloadBuilder.Build(Movement(MovementType.Einbuchung, kennung: null!));
        act.Should().Throw<SageBookingPayloadException>().WithMessage("*keine Sage-Referenz*");
    }

    [Fact]
    public void Build_MissingSagePlatzId_ThrowsPayloadException()
    {
        var act = () => SageLagerbuchungPayloadBuilder.Build(Movement(MovementType.Einbuchung, platzId: null));
        act.Should().Throw<SageBookingPayloadException>();
    }

    [Theory]
    [InlineData(MovementType.Umbuchung)]
    [InlineData(MovementType.SageEinbuchung)]
    [InlineData(MovementType.SageAusbuchung)]
    public void Build_NonBookableType_ThrowsPayloadException(MovementType type)
    {
        var act = () => SageLagerbuchungPayloadBuilder.Build(Movement(type));
        act.Should().Throw<SageBookingPayloadException>();
    }
}

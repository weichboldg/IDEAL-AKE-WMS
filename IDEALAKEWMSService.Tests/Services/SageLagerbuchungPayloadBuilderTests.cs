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
    [InlineData(MovementType.SageEinbuchung)]
    [InlineData(MovementType.SageAusbuchung)]
    public void Build_NonBookableType_ThrowsPayloadException(MovementType type)
    {
        var act = () => SageLagerbuchungPayloadBuilder.Build(Movement(type));
        act.Should().Throw<SageBookingPayloadException>();
    }

    // ----- Umbuchung (v1.73.0 E7, Spec F9): Art nach den Platz-Flags -----

    private static StorageLocation Platz(string code, bool sage, int? platzId) => new()
    {
        Code = code,
        SageBuchungErlaubt = sage,
        SageLagerkennung = platzId is null ? null : "LL",
        SageLagerplatzId = platzId
    };

    private static StockMovement Umbuchung(StorageLocation? herkunft, StorageLocation ziel) => new()
    {
        Id = 9,
        MovementType = MovementType.Umbuchung,
        Quantity = 2m,
        Article = new Article { ArticleNumber = "00722001" },
        StorageLocation = ziel,
        SourceStorageLocation = herkunft,
        SourceStorageLocationId = herkunft is null ? null : 1
    };

    [Fact]
    public void Build_Umbuchung_SageZuSage_Umbuchung_HerkunftUndZiel()
    {
        var payload = SageLagerbuchungPayloadBuilder.Build(Umbuchung(Platz("2864", true, 2864), Platz("11", true, 11)));

        payload.Memo.Should().Be("IdealAkeWms Lagerbuchung SM#9#");
        payload.Standardtext.Should().Be("Umbuchung Material");
        var z = payload.Lagerbuchungen.Should().ContainSingle().Subject;
        z.Lagerbewegungsart.Should().Be("Umbuchung");
        (z.HerkunftLagerkennung, z.HerkunftLagerplatzId, z.ZielLagerkennung, z.ZielLagerplatzId).Should().Be(("LL", 2864, "LL", 11));
        z.MengeLager.Should().Be(2m);
    }

    [Fact]
    public void Build_Umbuchung_SageZuWmsOnly_EntnahmeAnHerkunft()
    {
        var z = SageLagerbuchungPayloadBuilder.Build(Umbuchung(Platz("2864", true, 2864), Platz("WMS1", false, null))).Lagerbuchungen.Single();

        z.Lagerbewegungsart.Should().Be("Entnahme");
        (z.HerkunftLagerkennung, z.HerkunftLagerplatzId, z.ZielLagerkennung, z.ZielLagerplatzId).Should().Be(("LL", 2864, "", 0));
    }

    [Fact]
    public void Build_Umbuchung_WmsOnlyZuSage_ZugangAmZiel()
    {
        var z = SageLagerbuchungPayloadBuilder.Build(Umbuchung(Platz("WMS1", false, null), Platz("11", true, 11))).Lagerbuchungen.Single();

        z.Lagerbewegungsart.Should().Be("Zugang");
        (z.HerkunftLagerkennung, z.HerkunftLagerplatzId, z.ZielLagerkennung, z.ZielLagerplatzId).Should().Be(("", 0, "LL", 11));
    }

    [Fact]
    public void Build_Umbuchung_OhneHerkunft_ZugangAmZiel()
    {
        SageLagerbuchungPayloadBuilder.Build(Umbuchung(null, Platz("11", true, 11))).Lagerbuchungen.Single().Lagerbewegungsart.Should().Be("Zugang");
    }

    [Fact]
    public void Build_Umbuchung_WmsOnlyZuWmsOnly_Wirft()
    {
        var act = () => SageLagerbuchungPayloadBuilder.Build(Umbuchung(Platz("A", false, null), Platz("B", false, null)));
        act.Should().Throw<SageBookingPayloadException>().WithMessage("*ohne 'Sage-Buchung erlaubt'*");
    }

    [Fact]
    public void Build_Umbuchung_SagePlatzOhneReferenz_Wirft()
    {
        var act = () => SageLagerbuchungPayloadBuilder.Build(Umbuchung(Platz("2864", true, null), Platz("11", true, 11)));
        act.Should().Throw<SageBookingPayloadException>().WithMessage("*keine Sage-Referenz*");
    }

    [Fact]
    public void Build_Umbuchung_HerkunftNichtGeladen_Wirft()
    {
        var m = Umbuchung(null, Platz("11", true, 11));
        m.SourceStorageLocationId = 5;
        var act = () => SageLagerbuchungPayloadBuilder.Build(m);
        act.Should().Throw<SageBookingPayloadException>().WithMessage("*nicht geladen*");
    }
}

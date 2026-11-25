using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.66 (Spec 2026-11-23-sage-buchungen-gesamtsicht F2/F3/F5/F6/F8, AK 3/4/6/11/12): reine Funktionen der Seite „Sage-Buchungen“.</summary>
public class SageBuchungenGesamtsichtTests
{
    [Theory]
    [InlineData(SageBookingQueueStatus.Offen, SageBuchungAnzeige.Wartet)]
    [InlineData(SageBookingQueueStatus.Gesendet, SageBuchungAnzeige.Gesendet)]
    [InlineData(SageBookingQueueStatus.Bestaetigt, SageBuchungAnzeige.Uebergeben)]
    [InlineData(SageBookingQueueStatus.Fehler, SageBuchungAnzeige.Fehler)]
    public void Anzeige_Queue_JederWert(SageBookingQueueStatus s, SageBuchungAnzeige erwartet)
        => SageBuchungenGesamtsicht.Anzeige(s).Should().Be(erwartet);

    [Fact]
    public void Anzeige_Queue_NeuerEnumWertBrichtDenTest_AK3()
        => Enum.GetValues<SageBookingQueueStatus>().Should().HaveCount(4, "neuer Status -> Zuordnung F2 und Theory ergänzen");

    [Fact]
    public void Anzeige_We_JederWert_NichtNoetigUndNurInSageNull_AK3()
    {
        var erwartet = new Dictionary<SageWareneingangStatus, SageBuchungAnzeige?>
        {
            [SageWareneingangStatus.NichtNoetig] = null,
            [SageWareneingangStatus.Offen] = SageBuchungAnzeige.Wartet,
            [SageWareneingangStatus.Gesendet] = SageBuchungAnzeige.Gesendet,
            [SageWareneingangStatus.Uebergeben] = SageBuchungAnzeige.Uebergeben,
            [SageWareneingangStatus.Fehler] = SageBuchungAnzeige.Fehler,
            [SageWareneingangStatus.NurInSage] = null,
            [SageWareneingangStatus.Klaeren] = SageBuchungAnzeige.Klaeren,
        };
        Enum.GetValues<SageWareneingangStatus>().Should().BeEquivalentTo(erwartet.Keys, "neuer Enum-Wert bricht den Test");
        foreach (var (s, a) in erwartet) SageBuchungenGesamtsicht.Anzeige(s).Should().Be(a, s.ToString());
    }

    [Fact]
    public void IstOffen_UebergebenNicht_RestJa()
    {
        Enum.GetValues<SageBuchungAnzeige>().Where(SageBuchungenGesamtsicht.IstOffen)
            .Should().BeEquivalentTo(new[] { SageBuchungAnzeige.Wartet, SageBuchungAnzeige.Gesendet, SageBuchungAnzeige.Fehler, SageBuchungAnzeige.Klaeren });
    }

    private static SageBookingQueueItem Q(int id, MovementType t, int? lagerbestellung = null, string movementBy = "Hans",
        string queueBy = "Hans", DateTime? am = null, SageBookingQueueStatus st = SageBookingQueueStatus.Bestaetigt) => new()
    {
        Id = id, Status = st, CreatedAt = am ?? new DateTime(2026, 11, 20, 8, 0, 0), CreatedBy = queueBy,
        StockMovement = new StockMovement
        {
            MovementType = t, WarehouseRequisitionItemId = lagerbestellung, CreatedBy = movementBy, Quantity = 1m,
            Article = new Article { ArticleNumber = "A1" }, StorageLocation = new StorageLocation { Code = "P1" },
        },
    };

    [Fact]
    public void Herkunft_Lagerbestellung_VorBewegungsart_AK4()
    {
        SageBuchungenGesamtsicht.Herkunft(Q(1, MovementType.Ausbuchung, lagerbestellung: 7).StockMovement).Should().Be(SageBuchungHerkunft.Lagerbestellung);
        SageBuchungenGesamtsicht.Herkunft(Q(1, MovementType.Ausbuchung).StockMovement).Should().Be(SageBuchungHerkunft.Ausbuchung);
        SageBuchungenGesamtsicht.Herkunft(Q(1, MovementType.Einbuchung).StockMovement).Should().Be(SageBuchungHerkunft.Einbuchung);
        SageBuchungenGesamtsicht.Herkunft(Q(1, MovementType.Umbuchung).StockMovement).Should().Be(SageBuchungHerkunft.Umbuchung);
    }

    [Fact]
    public void ErfasstVon_Nacherfasst_NurMitAkteurAnDerBewegung_AK12()
    {
        SageBuchungenGesamtsicht.ErfasstVon(Q(1, MovementType.Einbuchung)).Should().Be("Hans");
        SageBuchungenGesamtsicht.ErfasstVon(Q(1, MovementType.Einbuchung, queueBy: SageBookingQueueRepository.ServiceActor)).Should().Be("Hans (nacherfasst)");
        SageBuchungenGesamtsicht.ErfasstVon(Q(1, MovementType.Einbuchung, movementBy: "", queueBy: SageBookingQueueRepository.ServiceActor))
            .Should().Be(SageBookingQueueRepository.ServiceActor);
    }

    private static SageWareneingangZeile W(int id, Guid vorgang, DateTime am, SageWareneingangStatus st = SageWareneingangStatus.Fehler) => new()
    {
        Receipt = new PurchaseOrderReceipt
        {
            Id = id, SageWeVorgang = vorgang, SageWeStatus = st, CreatedAt = am, CreatedBy = "Eva", Belegnummer = "2026-1", Quantity = 2m,
            StockMovement = new StockMovement { Article = new Article { ArticleNumber = "W1" }, StorageLocation = new StorageLocation { Code = "WE" } },
        },
        Memo = $"WE#{id}#",
    };

    [Fact]
    public void Build_NeuesteZuerst_DannId_UndErsteZeileJeVorgangAufDerSeite_AK6()
    {
        var v = Guid.NewGuid();
        var am = new DateTime(2026, 11, 21, 9, 0, 0);
        var zeilen = SageBuchungenGesamtsicht.Build([Q(5, MovementType.Einbuchung)], [W(12, v, am), W(11, v, am), W(13, v, am)]);

        zeilen.Select(z => z.Id).Should().Equal(11, 12, 13, 5);
        zeilen.Should().OnlyContain(z => !z.ZeigtWeAktionen, "Markierung erst nach dem Paging");
        zeilen[0].Herkunft.Should().Be(SageBuchungHerkunft.Wareneingang);
        zeilen[0].ErfasstVon.Should().Be("Eva");

        // Seite ohne die erste Position (Spaltenfilter/Seitenwechsel): Knöpfe an der ersten Zeile, die auf der Seite steht
        var seite = zeilen.Skip(1).ToList();
        SageBuchungenGesamtsicht.MarkiereErsteJeVorgang(seite);
        seite.Select(z => z.ZeigtWeAktionen).Should().Equal(true, false, false);
    }

    [Fact]
    public void Build_WeOhneAnzeige_NichtNoetigNurInSage_FaelltWeg_AK2()
    {
        var am = DateTime.Now;
        SageBuchungenGesamtsicht.Build([], [W(1, Guid.NewGuid(), am, SageWareneingangStatus.NurInSage), W(2, Guid.NewGuid(), am, SageWareneingangStatus.NichtNoetig)])
            .Should().BeEmpty();
    }

    [Fact]
    public void ImZeitraum_BisInklusiveGanzerTag_OhneBisOffen_AK11()
    {
        var von = new DateTime(2026, 11, 1);
        SageBuchungenGesamtsicht.ImZeitraum(new DateTime(2026, 10, 31, 23, 59, 0), von, null).Should().BeFalse();
        SageBuchungenGesamtsicht.ImZeitraum(von, von, null).Should().BeTrue();
        SageBuchungenGesamtsicht.ImZeitraum(new DateTime(2026, 11, 5, 23, 59, 0), von, new DateTime(2026, 11, 5)).Should().BeTrue();
        SageBuchungenGesamtsicht.ImZeitraum(new DateTime(2026, 11, 6), von, new DateTime(2026, 11, 5)).Should().BeFalse();
    }

    [Fact]
    public void AltStatus_Umleitung_OffenWirdWartet_NurInSageWeg_F10()
    {
        SageBuchungenGesamtsicht.Anzeige(SageWareneingangStatus.Offen).Should().Be(SageBuchungAnzeige.Wartet);
        Enum.TryParse<SageBuchungAnzeige>("Offen", out _).Should().BeFalse("?status=Offen bindet nicht -> Alle");
        Enum.TryParse<SageBuchungAnzeige>("Fehler", out _).Should().BeTrue("alte Lesezeichen binden weiter");
    }
}

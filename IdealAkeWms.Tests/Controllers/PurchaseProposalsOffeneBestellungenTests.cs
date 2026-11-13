using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.20 Spec 2026-11-08-einkauf-bestellvorschlaege E2 (AK 6): offen bestellt aus dem Cache mit der Rest-Regel.</summary>
public partial class PurchaseProposalsControllerTests
{
    private static SagePurchaseOrderLine Pos(int belPosId, int? articleId, decimal offen, DateTime? termin, string kz = "EBB", bool einheitAbweichend = false) => new()
    {
        BelPosId = belPosId, BelId = belPosId / 10, Belegkennzeichen = kz, Belegnummer = "2026-" + belPosId, Artikelnummer = "A1", ArticleId = articleId,
        MengeBestellt = offen, MengeOffen = offen, Liefertermin = termin, EinheitAbweichend = einheitAbweichend, SyncedAt = new DateTime(2026, 11, 13, 6, 0, 0),
    };

    [Fact]
    public async Task OffenBestellt_SummeDerReste_EbbUndEbz_WmsErfasstMindert_EinheitAbweichendGezaehlt_AK6()
    {
        var heute = DateTime.Today;
        var bestellungen = new List<SagePurchaseOrderLine>
        {
            Pos(11, 1, 50m, heute.AddDays(10)),
            Pos(21, 1, 30m, heute.AddDays(3), kz: "EBZ"),
            Pos(31, 1, 20m, heute.AddDays(1), einheitAbweichend: true),
            Pos(41, null, 99m, heute),           // kein WMS-Artikel
            Pos(51, 2, 5m, heute.AddDays(2)),    // anderer Artikel, kein Nachbestellartikel
        };
        // EBB: 10 im WMS erfasst, noch nicht an Sage → Rest 40; EBZ: 30 erfasst (auch uebergeben zieht ab) → Rest 0, Termin zaehlt nicht.
        var erfasst = new Dictionary<int, ErfasstJeBelPos>
        {
            [11] = new(10m, Array.Empty<(DateTime, decimal)>()),
            [21] = new(30m, new List<(DateTime, decimal)> { (new DateTime(2026, 11, 12), 30m) }),
        };
        var (ctrl, _) = Build(new() { A(1, "A1", 85m) }, Bestand(("A1", 10m)), bestellungen: bestellungen, erfasst: erfasst);
        var vm = Vm(await ctrl.Index(umfang: "alle"));
        var r = vm.Items.Single();
        r.OffenBestellt.Should().Be(40m);
        r.NaechsterLiefertermin.Should().Be(heute.AddDays(10));
        r.Ergebnis.Verfuegbar.Should().Be(50m);
        vm.PositionenEinheitAbweichend.Should().Be(1);
        vm.BestellungenStand.Should().NotBeNull();
        r.Hinweis.Should().NotContain("ohne offene Bestellungen");
    }

    [Fact]
    public async Task CacheLeer_OffenBestelltNull_HinweisOhneOffeneBestellungen_AK6()
    {
        var (ctrl, _) = Build(new() { A(1, "A1", 85m) }, Bestand(("A1", 10m)));
        var vm = Vm(await ctrl.Index());
        vm.BestellungenStand.Should().BeNull();
        var r = vm.Items.Single();
        r.OffenBestellt.Should().BeNull();
        r.Hinweis.Should().Contain("ohne offene Bestellungen");
        r.Ergebnis.Verfuegbar.Should().Be(10m);
    }

    [Fact]
    public async Task CacheGefuellt_ArtikelOhneBestellung_Null_Wird0()
    {
        var (ctrl, _) = Build(new() { A(1, "A1", 85m) }, bestellungen: new List<SagePurchaseOrderLine>());
        Vm(await ctrl.Index()).Items.Single().OffenBestellt.Should().Be(0m);
    }
}

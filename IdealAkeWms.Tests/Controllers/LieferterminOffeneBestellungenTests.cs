using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.85 (Spec 2026-11-26-liefertermin-offene-bestellungen-artikelinfo-lagerbestellung): Artikelinfo-Karte + Details-Spalte.</summary>
public class LieferterminOffeneBestellungenTests
{
    private static readonly DateTime Heute = new(2026, 11, 26);

    private static SagePurchaseOrderLine Line(int belPosId, string art, DateTime? termin, decimal offen = 5m, string beleg = "B-1") => new()
    {
        BelPosId = belPosId, BelId = belPosId, Belegkennzeichen = "EBB", Belegnummer = beleg, Artikelnummer = art,
        MengeBestellt = offen, MengeOffen = offen, Liefertermin = termin, SyncedAt = new DateTime(2026, 11, 26, 6, 0, 0),
    };

    // ---- Repository ----

    [Fact]
    public async Task GetByArtikelnummern_Treffer_GetrimmteEingabe_LeereListeOhneAbfrage()
    {
        var db = TestDbContextFactory.Create();
        db.SagePurchaseOrderLines.AddRange(Line(1, "A-1", Heute), Line(2, "A-2", Heute), Line(3, "B-9", Heute));
        await db.SaveChangesAsync();
        var repo = new SagePurchaseOrderLineRepository(db);

        (await repo.GetByArtikelnummernAsync(new[] { " A-1 ", "A-2" })).Select(l => l.BelPosId).Should().BeEquivalentTo(new[] { 1, 2 });
        (await repo.GetByArtikelnummernAsync(Array.Empty<string>())).Should().BeEmpty();
        (await repo.GetByArtikelnummernAsync(new[] { "  " })).Should().BeEmpty();
    }

    // ---- Zelltext F2.3 ----

    [Fact]
    public void LieferterminZelle_Faelle_AK9()
    {
        string Z(params (DateTime? T, decimal O)[] p) => WarehousePickingController.LieferterminZelle(p, Heute);
        Z().Should().Be("—");
        Z((new DateTime(2026, 12, 12), 3m)).Should().Be("12.12.2026");
        Z((new DateTime(2026, 12, 20), 1m), (new DateTime(2026, 12, 12), 3m)).Should().Be("12.12.2026 (+1)");
        Z((new DateTime(2026, 11, 5), 1m), (null, 1m), (new DateTime(2026, 12, 1), 1m)).Should().Be("05.11.2026 überfällig (+2)");
        Z((null, 2m)).Should().Be("ohne Termin");
        Z((Heute, 2m)).Should().Be("26.11.2026", "heute ist nicht überfällig");
        Z((new DateTime(2026, 12, 1), 0m)).Should().Be("—", "Rest 0 = vollständig erfasst, zählt nicht");
        Z((new DateTime(2026, 11, 1), 0m), (new DateTime(2026, 12, 1), 4m)).Should().Be("01.12.2026", "Rest-0-Position zählt weder Termin noch (+N)");
    }

    // ---- Artikelinfo F1 ----

    private static ArticleInfoViewModel Vm(IActionResult r) => (ArticleInfoViewModel)((ViewResult)r).Model!;

    [Fact]
    public async Task Info_CacheLeer_StandNull_KeineAbfrageDerPositionen_AK6()
    {
        var po = new Mock<ISagePurchaseOrderLineRepository>();
        po.Setup(p => p.GetStandAsync()).ReturnsAsync((DateTime?)null);
        var (ctrl, _, _) = ArtikelinfoLieferantenTests.Build(null, po.Object);

        var vm = Vm(await ctrl.Info("00722001"));

        vm.OffeneBestellungenStand.Should().BeNull();
        vm.OffeneBestellungen.Should().BeEmpty();
        po.Verify(p => p.GetByArtikelnummernAsync(It.IsAny<IReadOnlyCollection<string>>()), Times.Never);
    }

    [Fact]
    public async Task Info_RestRegel_Rest0Gezaehlt_SortiertNachTermin_OhneTerminZuletzt_AK1_AK4()
    {
        var stand = new DateTime(2026, 11, 26, 6, 0, 0);
        var lines = new List<SagePurchaseOrderLine>
        {
            Line(1, "00722001", null, 2m, "B-3"),
            Line(2, "00722001", new DateTime(2026, 12, 20), 4m, "B-2"),
            Line(3, "00722001", new DateTime(2026, 12, 1), 5m, "B-1"),
            Line(4, "00722001", new DateTime(2026, 11, 30), 3m, "B-4"),   // im WMS vollständig erfasst
        };
        var po = new Mock<ISagePurchaseOrderLineRepository>();
        po.Setup(p => p.GetStandAsync()).ReturnsAsync(stand);
        po.Setup(p => p.GetByArtikelnummernAsync(It.Is<IReadOnlyCollection<string>>(n => n.Single() == "00722001"))).ReturnsAsync(lines);
        var rc = new Mock<IPurchaseOrderReceiptRepository>();
        rc.Setup(r => r.GetErfasstJeBelPosAsync(It.IsAny<IReadOnlyCollection<int>>())).ReturnsAsync(new Dictionary<int, ErfasstJeBelPos>
        {
            [2] = new(1m, Array.Empty<(DateTime, decimal)>()),   // teilgeliefert im WMS: Rest 3
            [4] = new(3m, Array.Empty<(DateTime, decimal)>()),   // Rest 0
        });
        var (ctrl, _, _) = ArtikelinfoLieferantenTests.Build(null, po.Object, rc.Object);

        var vm = Vm(await ctrl.Info("00722001"));

        vm.OffeneBestellungenStand.Should().Be(stand);
        vm.OffeneBestellungen.Select(x => (x.Line.BelPosId, x.Offen)).Should().Equal((3, 5m), (2, 3m), (1, 2m));
        vm.OffeneBestellungenErfasst.Should().Be(1);
    }

    // ---- Details F2 ----

    private static (WarehousePickingController ctrl, int id) Details(bool mitCache, bool masterRead = true)
    {
        var db = TestDbContextFactory.Create();
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        db.ProductionWorkplaces.Add(wp);
        db.SaveChanges();
        var r = new WarehouseRequisition
        {
            ProductionWorkplaceId = wp.Id, Status = WarehouseRequisitionStatus.Submitted, SubmittedAt = DateTime.Now,
            CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x",
        };
        foreach (var (art, pos) in new[] { ("A-1", 1), ("DUMMY", 2), ("C-3", 3) })
            r.Items.Add(new WarehouseRequisitionItem
            {
                ArticleNumber = art, ArticleDescription = "x", QuantityRequested = 1, Position = pos,
                CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x",
            });
        db.WarehouseRequisitions.Add(r);
        if (mitCache)
            db.SagePurchaseOrderLines.AddRange(Line(1, "A-1", DateTime.Today.AddDays(10)), Line(2, "A-1", DateTime.Today.AddDays(20)),
                Line(3, "DUMMY", DateTime.Today));
        db.SaveChanges();

        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.HasMasterDataReadAccessAsync()).ReturnsAsync(masterRead);
        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(s => s.GetStockByArticleNumbersAsync(It.IsAny<List<string>>(), false))
            .ReturnsAsync(new Dictionary<string, List<StockLocationInfo>>());
        var ctrl = new WarehousePickingController(new WarehouseRequisitionRepository(db), new ProductionWorkplaceRepository(db), stock.Object,
            user.Object, Mock.Of<IUserViewPreferenceRepository>(), new ArticleRepository(db), new StorageLocationRepository(db),
            new AppSettingRepository(db), new ServiceSettingRepository(db), db, new SagePurchaseOrderLineRepository(db),
            new PurchaseOrderReceiptRepository(db));
        return (ctrl, r.Id);
    }

    private static WarehouseRequisitionDetailViewModel DetailVm(IActionResult r) => (WarehouseRequisitionDetailViewModel)((ViewResult)r).Model!;

    [Fact]
    public async Task Details_CacheLeer_KeineSpalte_AK11()
    {
        var (ctrl, id) = Details(mitCache: false);
        var vm = DetailVm(await ctrl.Details(id));
        vm.Liefertermine.Should().BeNull();
        vm.LieferterminStand.Should().BeNull();
    }

    [Fact]
    public async Task Details_NaechsterTerminMitPlusN_DummyUndOhneBestellungStrich_LinkNachRolle_AK9_AK10()
    {
        var (ctrl, id) = Details(mitCache: true);
        var vm = DetailVm(await ctrl.Details(id));

        vm.Liefertermine!.GetValueOrDefault("a-1").Should().Be(DateTime.Today.AddDays(10).ToString("dd.MM.yyyy") + " (+1)", "Groß/Klein egal");
        vm.Liefertermine.ContainsKey("DUMMY").Should().BeFalse("DUMMY → „—“");
        vm.Liefertermine.ContainsKey("C-3").Should().BeFalse("ohne offene Bestellung → „—“");
        vm.ArtikelinfoLink.Should().BeTrue();

        var (ctrl2, id2) = Details(mitCache: true, masterRead: false);
        DetailVm(await ctrl2.Details(id2)).ArtikelinfoLink.Should().BeFalse();
    }

    [Fact]
    public async Task Print_SpaltenfilterLiefertermin_FiltertWieDerBildschirm_NichtGegenLeerenText()
    {
        var (ctrl, id) = Details(mitCache: true);
        var http = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        http.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString("?colf_liefertermin=" + DateTime.Today.AddDays(10).ToString("dd.MM.yyyy"));
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };

        var vm = (WarehouseRequisitionPrintViewModel)((ViewResult)await ctrl.Print(id)).Model!;

        vm.Items.Select(i => i.ArticleNumber).Should().Equal("A-1");
        vm.Columns.Select(c => c.Key).Should().NotContain("liefertermin", "der Druck zeigt die Spalte nicht");
    }

    [Fact]
    public async Task Print_SortierungNachLiefertermin_WirktImDruck_B2()
    {
        async Task<int> PosVonA1(string dir)
        {
            var (ctrl, id) = Details(mitCache: true);
            ctrl.ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() };
            var vm = (WarehouseRequisitionPrintViewModel)((ViewResult)await ctrl.Print(id, "liefertermin", dir)).Model!;
            return vm.Items.FindIndex(i => i.ArticleNumber == "A-1");
        }
        // A-1 hat als einzige Position einen Termin; bliebe der Druck in Pos-Reihenfolge, stuende A-1 in beiden Richtungen vorn.
        (await PosVonA1("asc")).Should().NotBe(await PosVonA1("desc"));
    }

    // ---- Views ----

    private static string ViewText(string ordner, string datei)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Views"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(dir!.FullName, "IdealAkeWms", "Views", ordner, datei));
    }

    [Fact]
    public void Views_SpalteStandardAusgeblendet_LinkNeuerTab_KarteMitHinweisen_AK5_AK6_AK8()
    {
        ViewText("WarehousePicking", "Details.cshtml")
            .Should().Contain("\"key\": \"liefertermin\", \"label\": \"Liefertermin\", \"locked\": false, \"defaultWidth\": null, \"defaultHidden\": true")
            .And.Contain("data-col-key=\"liefertermin\"").And.Contain("target=\"_blank\"");
        ViewText("Articles", "Info.cshtml").Should().Contain("Offene Bestellungen").And.Contain("Sync:EinkaufBestellungenEnabled")
            .And.Contain("Keine offene Bestellung für diesen Artikel.").And.Contain("bereits vollständig erfasst").And.Contain(">überfällig</div>");
    }
}

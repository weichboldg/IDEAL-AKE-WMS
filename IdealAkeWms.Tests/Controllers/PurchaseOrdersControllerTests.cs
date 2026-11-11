using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.Settings;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>Spec 2026-11-08-wareneingang-aus-sage-bestellungen E2 (AK 6-11).</summary>
public partial class PurchaseOrdersControllerTests
{
    private static SagePurchaseOrderLine L(int belPosId, DateTime? termin, string kto = "K1", string art = "A1", decimal offen = 5m) => new()
    {
        BelPosId = belPosId, BelId = belPosId / 10, Belegkennzeichen = "EBB", Belegnummer = $"2026-{belPosId / 10}", LieferantKto = kto,
        LieferantName = "Lieferant " + kto, Artikelnummer = art, MengeBestellt = offen, MengeOffen = offen, Liefertermin = termin,
        MengeBasiseinheit = "Stk", SyncedAt = new DateTime(2026, 11, 8, 6, 0, 0),
    };

    internal static (PurchaseOrdersController ctrl, Mock<ISagePurchaseOrderLineRepository> lines, Mock<ICurrentUserService> user) Build(
        List<SagePurchaseOrderLine> zeilen, int wochen = 2, bool canStock = true)
    {
        var lines = new Mock<ISagePurchaseOrderLineRepository>();
        lines.Setup(l => l.GetAllAsync()).ReturnsAsync(zeilen);
        lines.Setup(l => l.GetStandAsync()).ReturnsAsync(zeilen.Count == 0 ? null : zeilen.Max(z => z.SyncedAt));
        var locations = new Mock<IStorageLocationRepository>();
        locations.Setup(l => l.GetAllOrderedAsync()).ReturnsAsync(new List<StorageLocation>());
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetIntValueAsync(AppSettingKeys.OffeneBestellungenVorschauWochen, 2)).ReturnsAsync(wochen);
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.CanAccessStockAsync()).ReturnsAsync(canStock);
        var receipts = new Mock<IPurchaseOrderReceiptRepository>();
        receipts.Setup(r => r.GetErfasstJeBelPosAsync(It.IsAny<IReadOnlyCollection<int>>())).ReturnsAsync(new Dictionary<int, ErfasstJeBelPos>());
        var ctrl = new PurchaseOrdersController(lines.Object, locations.Object, settings.Object, user.Object, receipts.Object,
            Mock.Of<IStockMovementRepository>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return (ctrl, lines, user);
    }

    private static PurchaseOrderListViewModel Vm(IActionResult r) => (PurchaseOrderListViewModel)((ViewResult)r).Model!;

    [Fact]
    public async Task Vorschau_HorizontXWochen_UeberfaelligUndOhneTerminImmer_AK7()
    {
        var heute = DateTime.Today;
        var (ctrl, _, _) = Build(new List<SagePurchaseOrderLine>
        {
            L(11, heute.AddDays(14)), L(21, heute.AddDays(15)), L(31, heute.AddDays(-3)), L(41, null),
        }, wochen: 2);

        var vm = Vm(await ctrl.Index());

        vm.Items.Select(i => i.Line.BelPosId).Should().Equal(31, 11, 41);
        vm.Items.Single(i => i.Line.BelPosId == 31).Ueberfaellig.Should().BeTrue();
        PurchaseOrdersController.Hinweis(vm.Items.Single(i => i.Line.BelPosId == 41)).Should().Contain("ohne Termin");
        Vm(await ctrl.Index(wochen: 4)).Items.Should().HaveCount(4, "Filter „Wochen“ ueberschreibt die Einstellung je Aufruf");
        Vm(await ctrl.Index(nurUeberfaellig: true)).Items.Select(i => i.Line.BelPosId).Should().Equal(31);
    }

    [Fact]
    public async Task Filter_Lieferant_UndPagination_AK9()
    {
        var t = DateTime.Today.AddDays(1);
        var zeilen = Enumerable.Range(1, 30).Select(i => L(i * 10 + 1, t, kto: i % 2 == 0 ? "K2" : "K1")).ToList();
        var (ctrl, _, _) = Build(zeilen);

        var vm = Vm(await ctrl.Index(lieferant: "k2", pageSize: 25));
        vm.Pagination.TotalCount.Should().Be(15);
        vm.Items.Should().OnlyContain(i => i.Line.LieferantKto == "K2");
        vm.Lieferanten.Select(l => l.Kto).Should().BeEquivalentTo(new[] { "K1", "K2" });
    }

    [Fact]
    public async Task StockRead_OhneKnopf_AK10_LeererCache_Hinweis_AK11()
    {
        var (ctrl, _, _) = Build(new List<SagePurchaseOrderLine>(), canStock: false);
        var vm = Vm(await ctrl.Index());
        (vm.CanReceive, vm.Stand).Should().Be((false, (DateTime?)null));
        File.ReadAllText(ViewPfad("Index.cshtml")).Should().Contain("Sync:EinkaufBestellungenEnabled").And.Contain("@if (Model.CanReceive)");
    }

    [Fact]
    public void Nachbesserung_v1_86_Farbkonzept_UeberfaelligRotMitLegende_ZielHinweis()
    {
        var index = File.ReadAllText(ViewPfad("Index.cshtml"));
        index.Should().Contain("r.Ueberfaellig ? \"table-danger\"").And.Contain("rot hinterlegt").And.NotContain("table-warning");
        // v1.91.0 (N-1): Legende im Zeilenton (Hellrot, dunkler Text), nicht kraeftig rot
        index.Should().Contain("badge bg-danger-subtle text-danger-emphasis").And.NotContain("text-bg-danger");
        File.ReadAllText(ViewPfad("Receive.cshtml")).Should().Contain("ist im WMS nicht buchbar – bitte Ziel wählen");
    }

    internal static string ViewPfad(string datei)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Views"))) dir = dir.Parent;
        return Path.Combine(dir!.FullName, "IdealAkeWms", "Views", "PurchaseOrders", datei);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("false", false)]
    [InlineData(" true", false)]
    [InlineData("TRUE", true)]
    public async Task FeatureGate_NurTrue_Sonst404_AK6(string? wert, bool durch)
    {
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.WareneingangBestellungAktiv)).ReturnsAsync(wert);
        var filter = new RequireWareneingangBestellungAktivFilter(settings.Object);
        var ctx = new ActionExecutingContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
        var weiter = false;

        await filter.OnActionExecutionAsync(ctx, () => { weiter = true; return Task.FromResult<ActionExecutedContext>(null!); });

        weiter.Should().Be(durch);
        if (!durch) ctx.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void Einstellungen_Katalog_Wochen1bis26_AK8()
    {
        var d = AppSettingDefinitions.All.Single(x => x.Key == AppSettingKeys.OffeneBestellungenVorschauWochen);
        SettingValidator.Validate(d, "0").Gueltig.Should().BeFalse();
        SettingValidator.Validate(d, "27").Gueltig.Should().BeFalse();
        SettingValidator.Validate(d, "abc").Gueltig.Should().BeFalse();
        SettingValidator.Validate(d, "4").Kanonisch.Should().Be("4");
        AppSettingDefinitions.All.Single(x => x.Key == AppSettingKeys.WareneingangBestellungAktiv).DefaultValue.Should().Be("false");
        typeof(PurchaseOrdersController).GetCustomAttributes(false).Select(a => a.GetType())
            .Should().Contain(new[] { typeof(RequireStockReadAccessAttribute), typeof(RequireWareneingangBestellungAktivAttribute) });
    }
}

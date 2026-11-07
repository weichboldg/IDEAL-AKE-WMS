using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>Bestandsuebersicht: seit v1.87.0 (Spec 2026-11-08-lagerbuchungen-fa-feld-und-verlinkung, Antwort 2) uebernimmt der Artikel-QR-Knopf
/// NIE einen FA – ersetzt die QrMitFaNummer-Bindung aus v1.76.0 (Spec 2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest). Dazu FA-Kopf.</summary>
public class StockOverviewControllerTests
{
    private static StockOverviewController Ctrl(IProductionOrderRepository? po = null)
    {
        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(s => s.GetCurrentStockAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>()))
            .ReturnsAsync(new List<StockOverviewItem>());
        stock.Setup(s => s.GetStockByProductionOrderAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new List<StockOverviewItem>());
        var locations = new Mock<IStorageLocationRepository>();
        locations.Setup(l => l.GetAllOrderedAsync()).ReturnsAsync(new List<StorageLocation>());
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.QrMitFaNummer)).ReturnsAsync("true");
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        return new StockOverviewController(stock.Object, locations.Object, settings.Object, user.Object, po ?? Mock.Of<IProductionOrderRepository>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task Index_ArtikelQr_UebernimmtNieFa_AuchMitQrMitFaNummer_AK14()
    {
        var ctrl = Ctrl();
        (await ctrl.Index(null, null, null, null, null)).Should().BeOfType<ViewResult>();
        ((object?)ctrl.ViewBag.QrMitFaNummer).Should().BeNull();
        var view = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "IdealAkeWms", "Views", "StockOverview", "Index.cshtml"));
        view.Should().Contain("data-qr-fa-enabled=\"false\" data-fa-target=\"filterProductionOrder\"");
    }

    [Fact]
    public async Task Index_FaFilter_FaKopf_AK13()
    {
        var po = new Mock<IProductionOrderRepository>();
        po.Setup(r => r.FindForBookingAsync("4711")).ReturnsAsync(new FaAufloesung(FaAufloesungStatus.Unbekannt, null));
        var ctrl = Ctrl(po.Object);
        var vm = (StockOverviewViewModel)((ViewResult)await ctrl.Index(null, null, null, null, "4711")).Model!;
        vm.FaKopf!.Status.Should().Be(FaAufloesungStatus.Unbekannt);
        var ohne = (StockOverviewViewModel)((ViewResult)await Ctrl(po.Object).Index(null, null, null, null, null)).Model!;
        ohne.FaKopf.Should().BeNull();
    }
}

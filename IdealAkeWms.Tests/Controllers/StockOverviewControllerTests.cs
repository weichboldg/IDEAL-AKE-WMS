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

/// <summary>Spec 2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest (Variante A): FA-Uebernahme aus dem Artikel-QR folgt QrMitFaNummer.</summary>
public class StockOverviewControllerTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public async Task Index_QrMitFaNummer_InViewBag(string? wert, bool erwartet)
    {
        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(s => s.GetCurrentStockAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>()))
            .ReturnsAsync(new List<StockOverviewItem>());
        var locations = new Mock<IStorageLocationRepository>();
        locations.Setup(l => l.GetAllOrderedAsync()).ReturnsAsync(new List<StorageLocation>());
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.QrMitFaNummer)).ReturnsAsync(wert);
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        var ctrl = new StockOverviewController(stock.Object, locations.Object, settings.Object, user.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await ctrl.Index(null, null, null, null, null);

        result.Should().BeOfType<ViewResult>();
        ((bool)ctrl.ViewBag.QrMitFaNummer).Should().Be(erwartet);
    }
}

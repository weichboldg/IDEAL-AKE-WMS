using FluentAssertions;
using IdealAkeWms.Controllers.Api;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.45: Lagerplatz-Text der Lager-/Glasbestellung nur des Positionsartikels (Spec 2026-11-19-artikelinfo-bestand-exakt-bug-spec).</summary>
public class LagerplatzTextExaktTests
{
    private static (WarehouseRequisitionsApiController ctrl, Mock<IStockMovementRepository> stock) Api()
    {
        var ctx = TestDbContextFactory.Create();
        var stock = new Mock<IStockMovementRepository>();
        var ctrl = new WarehouseRequisitionsApiController(new WarehouseRequisitionRepository(ctx), new ArticleRepository(ctx),
            stock.Object, new Mock<ICurrentUserService>().Object, new Mock<IAppSettingRepository>().Object, new ProductionWorkplaceRepository(ctx));
        return (ctrl, stock);
    }

    private static string Locations(IActionResult r)
        => (string)r.Should().BeOfType<OkObjectResult>().Subject.Value!.GetType().GetProperty("locations")!.GetValue(((OkObjectResult)r).Value)!;

    [Fact]
    public void LagerplatzText_NurPositiv_HauptlagerplatzDannCode()
    {
        var text = StockLocationInfo.LagerplatzText(new[]
        {
            new StockLocationInfo { Code = "B", Quantity = 5m },
            new StockLocationInfo { Code = "A", Quantity = 1m },
            new StockLocationInfo { Code = "Z", Quantity = 2m, IsPrimaryStorageLocation = true },
            new StockLocationInfo { Code = "C", Quantity = 0m },
            new StockLocationInfo { Code = "D", Quantity = -1m },
        });
        text.Should().Be($"Z ({2m:N3}), A ({1m:N3}), B ({5m:N3})");
    }

    [Fact]
    public async Task Stock_SchluesselInAndererSchreibweise_TextTrotzdemGefuellt_OhneWagen()
    {
        var (ctrl, stock) = Api();
        stock.Setup(s => s.GetStockByArticleNumbersAsync(It.Is<List<string>>(l => l.Single() == "ek002995"), false))
            .ReturnsAsync(new Dictionary<string, List<StockLocationInfo>>
            {
                ["EK002995"] = new() { new StockLocationInfo { Code = "LL;1;25;0", Quantity = 12m } }
            });

        Locations(await ctrl.Stock("ek002995 ")).Should().Be($"LL;1;25;0 ({12m:N3})");
        stock.Verify(s => s.GetCurrentStockAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Stock_LeereArtikelnummer_LeererText_KeinRepositoryAufruf(string? nr)
    {
        var (ctrl, stock) = Api();
        Locations(await ctrl.Stock(nr)).Should().BeEmpty();
        stock.VerifyNoOtherCalls();
    }
}

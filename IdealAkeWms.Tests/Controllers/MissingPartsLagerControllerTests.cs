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

public class MissingPartsLagerControllerTests
{
    private static (MissingPartsLagerController ctrl, Mock<IWarehouseRequisitionRepository> repo,
                    Mock<IProductionWorkplaceRepository> wp, Mock<IStockMovementRepository> stock,
                    Mock<ICurrentUserService> user) Build()
    {
        var repo = new Mock<IWarehouseRequisitionRepository>();
        var wp = new Mock<IProductionWorkplaceRepository>();
        var stock = new Mock<IStockMovementRepository>();
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        user.Setup(u => u.GetCurrentAppUserId()).Returns(1);
        wp.Setup(w => w.GetAllAsync()).ReturnsAsync(new List<ProductionWorkplace>());
        repo.Setup(r => r.GetMissingPartsAsync(It.IsAny<ShortageStatus>(),
                It.IsAny<int?>(),
                It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(((IReadOnlyList<MissingPartRow>)new List<MissingPartRow>(), 0));
        stock.Setup(s => s.GetStockByArticleNumbersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new Dictionary<string, List<StockLocationInfo>>());
        var ctrl = new MissingPartsLagerController(repo.Object, wp.Object, stock.Object, user.Object);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return (ctrl, repo, wp, stock, user);
    }

    [Fact]
    public async Task Index_DefaultReturnsViewModel()
    {
        var (ctrl, _, _, _, _) = Build();
        var result = await ctrl.Index();
        result.Should().BeOfType<ViewResult>();
        var vm = (result as ViewResult)!.Model as MissingPartsListViewModel;
        vm!.MineOnly.Should().BeFalse();
        vm.ActiveTab.Should().Be(ShortageStatus.WillBeRestocked);
    }

    [Fact]
    public async Task Index_TabParam_PassedToRepo()
    {
        var (ctrl, repo, _, _, _) = Build();
        await ctrl.Index(tab: ShortageStatus.NoRestock);
        repo.Verify(r => r.GetMissingPartsAsync(ShortageStatus.NoRestock,
            It.IsAny<int?>(), It.IsAny<IReadOnlyDictionary<string, string>?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Index_DoesNotApplyMineOnly()
    {
        var (ctrl, _, wp, _, _) = Build();
        await ctrl.Index();
        wp.Verify(w => w.GetByUserIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Index_EnrichesRowsWithStorageLocations()
    {
        var (ctrl, repo, _, stock, _) = Build();
        var row = new MissingPartRow(
            RequisitionId: 1, ItemId: 10, Position: 1,
            WorkplaceName: "A1",
            ArticleNumber: "20623",
            ArticleDescription: "LED-Stromversorger",
            QuantityRequested: 4m, QuantityPicked: 0m, QuantityMissing: 4m,
            Unit: "Stk", Note: null,
            CreatedBy: "user", ClosedAt: null,
            Status: ShortageStatus.WillBeRestocked,
            NoteEinkauf: null);
        repo.Setup(r => r.GetMissingPartsAsync(It.IsAny<ShortageStatus>(),
                It.IsAny<int?>(),
                It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(((IReadOnlyList<MissingPartRow>)new List<MissingPartRow> { row }, 1));
        stock.Setup(s => s.GetStockByArticleNumbersAsync(It.Is<List<string>>(l => l.Contains("20623"))))
            .ReturnsAsync(new Dictionary<string, List<StockLocationInfo>>
            {
                ["20623"] = new List<StockLocationInfo>
                {
                    new StockLocationInfo { Code = "LP-A1", Quantity = 5m, StorageLocationId = 1 },
                    new StockLocationInfo { Code = "LP-B2", Quantity = 0m, StorageLocationId = 2 },
                    new StockLocationInfo { Code = "LP-C3", Quantity = 2m, StorageLocationId = 3 }
                }
            });

        var result = await ctrl.Index();
        var vm = (result as ViewResult)!.Model as MissingPartsListViewModel;
        vm!.Items.Should().HaveCount(1);
        // Lagerplaetze nach Bestand absteigend sortiert, 0-Bestand ausgefiltert
        vm.Items[0].StorageLocations.Should().Be("LP-A1 (5,000), LP-C3 (2,000)");
    }

    [Fact]
    public async Task Index_RowsWithoutStock_HaveNullStorageLocations()
    {
        var (ctrl, repo, _, stock, _) = Build();
        var row = new MissingPartRow(
            RequisitionId: 1, ItemId: 10, Position: 1,
            WorkplaceName: "A1",
            ArticleNumber: "99999",
            ArticleDescription: "Niemals auf Lager",
            QuantityRequested: 1m, QuantityPicked: 0m, QuantityMissing: 1m,
            Unit: "Stk", Note: null,
            CreatedBy: "user", ClosedAt: null,
            Status: ShortageStatus.WillBeRestocked,
            NoteEinkauf: null);
        repo.Setup(r => r.GetMissingPartsAsync(It.IsAny<ShortageStatus>(),
                It.IsAny<int?>(),
                It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(((IReadOnlyList<MissingPartRow>)new List<MissingPartRow> { row }, 1));
        // Stock-Dict liefert kein Mapping fuer 99999 → null bleibt null
        stock.Setup(s => s.GetStockByArticleNumbersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new Dictionary<string, List<StockLocationInfo>>());

        var result = await ctrl.Index();
        var vm = (result as ViewResult)!.Model as MissingPartsListViewModel;
        vm!.Items[0].StorageLocations.Should().BeNull();
    }
}

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

public class MissingPartsControllerTests
{
    private static (MissingPartsController ctrl, Mock<IWarehouseRequisitionRepository> repo,
                    Mock<IProductionWorkplaceRepository> wp, Mock<ICurrentUserService> user) Build()
    {
        var repo = new Mock<IWarehouseRequisitionRepository>();
        var wp = new Mock<IProductionWorkplaceRepository>();
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        user.Setup(u => u.GetCurrentAppUserId()).Returns(1);
        user.Setup(u => u.GetDisplayName()).Returns("test");
        user.Setup(u => u.GetWindowsUserName()).Returns("test\\test");
        wp.Setup(w => w.GetAllAsync()).ReturnsAsync(new List<ProductionWorkplace>());
        wp.Setup(w => w.GetByUserIdAsync(It.IsAny<int>())).ReturnsAsync(new List<ProductionWorkplace>());
        repo.Setup(r => r.GetMissingPartsAsync(It.IsAny<ShortageStatus>(),
                It.IsAny<WarehouseRequisitionType?>(),
                It.IsAny<IReadOnlyCollection<int>?>(),
                It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(((IReadOnlyList<MissingPartRow>)new List<MissingPartRow>(), 0));
        var ctrl = new MissingPartsController(repo.Object, wp.Object, user.Object);
        // HttpContext fuer ColumnFilterHelper
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return (ctrl, repo, wp, user);
    }

    [Fact]
    public async Task Index_MineOnlyFalse_PassesWorkplaceIdToRepoUnchanged()
    {
        var (ctrl, repo, _, _) = Build();
        await ctrl.Index(tab: ShortageStatus.NoRestock, workplaceId: 5, mineOnly: false);
        // Haupt-Listing-Call (mit columnFilters Dictionary, pageSize > 1)
        repo.Verify(r => r.GetMissingPartsAsync(ShortageStatus.NoRestock, It.IsAny<WarehouseRequisitionType?>(), Ids(5),
            It.Is<IReadOnlyDictionary<string, string>?>(d => d != null),
            null, null, 1, It.Is<int>(s => s > 1)), Times.Once);
    }

    private static IReadOnlyCollection<int>? Ids(params int[] ids) =>
        It.Is<IReadOnlyCollection<int>?>(w => w != null && w.OrderBy(x => x).SequenceEqual(ids.OrderBy(x => x)));

    /// <summary>Alle drei Repo-Aufrufe (Liste + zwei Tab-Badges) mit derselben Werkbank-Menge (Spec 2026-11-07-fehlteile, AK 3-6).</summary>
    private static void AlleDreiMit(Mock<IWarehouseRequisitionRepository> repo, Func<IReadOnlyCollection<int>?, bool> pruefen)
    {
        repo.Verify(r => r.GetMissingPartsAsync(It.IsAny<ShortageStatus>(), It.IsAny<WarehouseRequisitionType?>(),
            It.Is<IReadOnlyCollection<int>?>(w => pruefen(w)), It.IsAny<IReadOnlyDictionary<string, string>?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Index_MineOnlyFalse_OhneWerkbank_KeinFilter_AK6()
    {
        var (ctrl, repo, _, _) = Build();
        await ctrl.Index(tab: ShortageStatus.NoRestock, workplaceId: null, mineOnly: false);
        AlleDreiMit(repo, w => w == null);
    }

    [Fact]
    public async Task Index_MineOnly_MehrereWerkbaenke_AlleAufrufeMitMenge_TotalAusRepo_AK3()
    {
        var (ctrl, repo, wp, user) = Build();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(42);
        wp.Setup(w => w.GetByUserIdAsync(42)).ReturnsAsync(new List<ProductionWorkplace>
            { new ProductionWorkplace { Id = 7, Name = "WB7" }, new ProductionWorkplace { Id = 8, Name = "WB8" } });
        var zeilen = Enumerable.Range(1, 10).Select(i => new MissingPartRow(i, i, 1, "FREMD", "A" + i, "D", 1m, 0m, 1m, null, null, "u", null,
            ShortageStatus.NoRestock, null)).ToList();
        repo.Setup(r => r.GetMissingPartsAsync(ShortageStatus.NoRestock, It.IsAny<WarehouseRequisitionType?>(), It.IsAny<IReadOnlyCollection<int>?>(),
                It.Is<IReadOnlyDictionary<string, string>?>(d => d != null), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(((IReadOnlyList<MissingPartRow>)zeilen, 25));

        var vm = (MissingPartsListViewModel)((ViewResult)await ctrl.Index(tab: ShortageStatus.NoRestock, workplaceId: null, mineOnly: true)).Model!;

        AlleDreiMit(repo, w => w != null && w.OrderBy(x => x).SequenceEqual(new[] { 7, 8 }));
        vm.Pagination.TotalCount.Should().Be(25, "Gesamtzahl aus dem Repository, nicht die Zeilen der Seite");
        vm.Items.Should().HaveCount(10, "kein Nachfilter im Speicher (Werkbank-Name „FREMD“ bleibt drin, der Filter ist im Repository)");
    }

    [Fact]
    public async Task Index_MineOnly_FremdeWerkbank_AlleAufrufeLeereMenge_AK4()
    {
        var (ctrl, repo, wp, user) = Build();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(42);
        wp.Setup(w => w.GetByUserIdAsync(42)).ReturnsAsync(new List<ProductionWorkplace> { new ProductionWorkplace { Id = 7, Name = "WB7" } });

        var vm = (MissingPartsListViewModel)((ViewResult)await ctrl.Index(tab: ShortageStatus.NoRestock, workplaceId: 99, mineOnly: true)).Model!;

        AlleDreiMit(repo, w => w != null && w.Count == 0);
        (vm.Items.Count, vm.Pagination.TotalCount).Should().Be((0, 0));
    }

    [Fact]
    public async Task Index_MineOnly_SingleUserWorkplace_FiltersByThatId()
    {
        var (ctrl, repo, wp, user) = Build();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(42);
        wp.Setup(w => w.GetByUserIdAsync(42)).ReturnsAsync(new List<ProductionWorkplace>
            { new ProductionWorkplace { Id = 7, Name = "WB7" } });
        await ctrl.Index(tab: ShortageStatus.NoRestock, workplaceId: null, mineOnly: true);
        // Haupt-Listing-Call (mit columnFilters Dictionary, pageSize > 1)
        AlleDreiMit(repo, w => w != null && w.SequenceEqual(new[] { 7 }));
        repo.Verify(r => r.GetMissingPartsAsync(ShortageStatus.NoRestock, It.IsAny<WarehouseRequisitionType?>(), Ids(7),
            It.Is<IReadOnlyDictionary<string, string>?>(d => d != null),
            null, null, 1, It.Is<int>(s => s > 1)), Times.Once);
    }

    [Fact]
    public async Task Index_MineOnly_WithInconsistentWorkplaceId_ReturnsEmpty()
    {
        var (ctrl, _, wp, user) = Build();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(42);
        wp.Setup(w => w.GetByUserIdAsync(42)).ReturnsAsync(new List<ProductionWorkplace>
            { new ProductionWorkplace { Id = 7, Name = "WB7" } });
        var result = await ctrl.Index(tab: ShortageStatus.NoRestock, workplaceId: 99, mineOnly: true);
        var vm = (result as ViewResult)?.Model as MissingPartsListViewModel;
        vm!.Items.Should().BeEmpty();
        vm.Pagination.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Index_ReturnsViewWithModel()
    {
        var (ctrl, _, _, _) = Build();
        var result = await ctrl.Index(workplaceId: null, mineOnly: false);
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Index_DefaultTab_WillBeRestocked()
    {
        var (ctrl, repo, _, _) = Build();
        await ctrl.Index(mineOnly: false);
        repo.Verify(r => r.GetMissingPartsAsync(ShortageStatus.WillBeRestocked,
            It.IsAny<WarehouseRequisitionType?>(),
            It.IsAny<IReadOnlyCollection<int>?>(), It.IsAny<IReadOnlyDictionary<string,string>?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Index_TabNone_NormalizedToWillBeRestocked()
    {
        var (ctrl, repo, _, _) = Build();
        await ctrl.Index(tab: ShortageStatus.None, mineOnly: false);
        repo.Verify(r => r.GetMissingPartsAsync(ShortageStatus.WillBeRestocked,
            It.IsAny<WarehouseRequisitionType?>(),
            It.IsAny<IReadOnlyCollection<int>?>(), It.IsAny<IReadOnlyDictionary<string,string>?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Index_ViewModelHasBothCounts()
    {
        var (ctrl, repo, _, _) = Build();
        repo.Setup(r => r.GetMissingPartsAsync(ShortageStatus.WillBeRestocked,
                It.IsAny<WarehouseRequisitionType?>(),
                It.IsAny<IReadOnlyCollection<int>?>(), It.IsAny<IReadOnlyDictionary<string,string>?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(((IReadOnlyList<MissingPartRow>)new List<MissingPartRow>(), 3));
        repo.Setup(r => r.GetMissingPartsAsync(ShortageStatus.NoRestock,
                It.IsAny<WarehouseRequisitionType?>(),
                It.IsAny<IReadOnlyCollection<int>?>(), It.IsAny<IReadOnlyDictionary<string,string>?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(((IReadOnlyList<MissingPartRow>)new List<MissingPartRow>(), 5));
        var result = await ctrl.Index(mineOnly: false);
        var vm = (result as ViewResult)?.Model as MissingPartsListViewModel;
        vm!.WaitingTotalCount.Should().Be(3);
        vm.NoRestockTotalCount.Should().Be(5);
    }

    [Fact]
    public async Task Index_DefaultIsMineOnlyTrue()
    {
        var (ctrl, _, wp, user) = Build();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(42);
        wp.Setup(w => w.GetByUserIdAsync(42)).ReturnsAsync(new List<ProductionWorkplace>
            { new ProductionWorkplace { Id = 7, Name = "WB7" } });
        await ctrl.Index();
        wp.Verify(w => w.GetByUserIdAsync(42), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Index_MineOnly_NoWorkplaceMapping_SetsHasNoWorkplaceMappingTrue()
    {
        var (ctrl, _, wp, user) = Build();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(99);
        wp.Setup(w => w.GetByUserIdAsync(99)).ReturnsAsync(new List<ProductionWorkplace>());
        var result = await ctrl.Index(mineOnly: true);
        var vm = (result as ViewResult)?.Model as MissingPartsListViewModel;
        vm!.HasNoWorkplaceMapping.Should().BeTrue();
        vm.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Index_TypGlas_ReichtTypScharfDurch()
    {
        var (ctrl, repo, _, user) = Build();
        user.Setup(u => u.CanOrderLagerAsync()).ReturnsAsync(true);
        user.Setup(u => u.CanOrderGlasAsync()).ReturnsAsync(true);
        var capturedTypes = new List<WarehouseRequisitionType?>();
        repo.Setup(r => r.GetMissingPartsAsync(It.IsAny<ShortageStatus>(),
                It.IsAny<WarehouseRequisitionType?>(),
                It.IsAny<IReadOnlyCollection<int>?>(),
                It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .Callback<ShortageStatus, WarehouseRequisitionType?, IReadOnlyCollection<int>?,
                IReadOnlyDictionary<string, string>?, DateTime?, DateTime?, int, int>(
                (_, type, _, _, _, _, _, _) => capturedTypes.Add(type))
            .ReturnsAsync(((IReadOnlyList<MissingPartRow>)new List<MissingPartRow>(), 0));

        var result = await ctrl.Index(tab: ShortageStatus.WillBeRestocked,
            type: WarehouseRequisitionType.Glas, mineOnly: false) as ViewResult;

        // Haupt-Listen-Aufruf (erster Repo-Call) MUSS exakt den gewaehlten Typ durchreichen —
        // und auch beide Count-Aufrufe sind typ-bezogen (alle Calls = Glas).
        capturedTypes.Should().NotBeEmpty();
        capturedTypes[0].Should().Be(WarehouseRequisitionType.Glas);
        capturedTypes.Should().OnlyContain(t => t == WarehouseRequisitionType.Glas);

        var vm = result!.Model as MissingPartsListViewModel;
        vm!.ActiveType.Should().Be(WarehouseRequisitionType.Glas);
    }
}

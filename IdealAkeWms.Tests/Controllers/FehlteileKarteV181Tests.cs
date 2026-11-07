using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>Spec 2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas (v1.81.0): AK 1-8.</summary>
public class FehlteileKarteV181Tests
{
    // ---- AK 1: Repository zaehlt je Typ ----
    [Fact]
    public async Task ShortageCounts_JeTyp_NullAlle_AK1()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        db.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
        {
            UserId = 42, ProductionWorkplaceId = 1, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        int pos = 0;
        void Bestellung(int id, WarehouseRequisitionType typ, ShortageStatus status, int anzahl)
        {
            db.WarehouseRequisitions.Add(new WarehouseRequisition
            {
                Id = id, ProductionWorkplaceId = 1, Type = typ, Status = WarehouseRequisitionStatus.Closed,
                CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
            });
            for (var i = 0; i < anzahl; i++)
                db.WarehouseRequisitionItems.Add(new WarehouseRequisitionItem
                {
                    WarehouseRequisitionId = id, Position = ++pos, ArticleNumber = "A" + pos, ArticleDescription = "x", QuantityRequested = 1,
                    QuantityPicked = 0, ShortageStatus = status, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
                });
        }
        Bestellung(1, WarehouseRequisitionType.Lager, ShortageStatus.WillBeRestocked, 2);
        Bestellung(2, WarehouseRequisitionType.Glas, ShortageStatus.WillBeRestocked, 2);
        Bestellung(3, WarehouseRequisitionType.Glas, ShortageStatus.WillBeRestocked, 1);
        Bestellung(4, WarehouseRequisitionType.Glas, ShortageStatus.NoRestock, 1);
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);

        (await repo.GetShortageCountsForUserAsync(42, WarehouseRequisitionType.Lager)).Should().Be((2, 1, 0, 0));
        (await repo.GetShortageCountsForUserAsync(42, WarehouseRequisitionType.Glas)).Should().Be((3, 2, 1, 1));
        (await repo.GetShortageCountsForUserAsync(42, null)).Should().Be((5, 3, 1, 1));
    }

    // ---- AK 2: Controller uebergibt den aktiven Typ ----
    [Theory]
    [InlineData(WarehouseRequisitionType.Glas, WarehouseRequisitionType.Glas)]
    [InlineData(null, WarehouseRequisitionType.Lager)]
    public async Task Lagerbestellungen_KarteMitAktivemTyp_AK2(WarehouseRequisitionType? angefragt, WarehouseRequisitionType erwartet)
    {
        var repo = new Mock<IWarehouseRequisitionRepository>();
        repo.Setup(r => r.GetForUserAsync(It.IsAny<int>())).ReturnsAsync(new List<WarehouseRequisition>());
        repo.Setup(r => r.GetShortageCountsForUserAsync(It.IsAny<int>(), It.IsAny<WarehouseRequisitionType?>())).ReturnsAsync((0, 0, 0, 0));
        var wp = new Mock<IProductionWorkplaceRepository>();
        wp.Setup(w => w.GetByUserIdAsync(It.IsAny<int>())).ReturnsAsync(new List<ProductionWorkplace>());
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(7);
        user.Setup(u => u.CanOrderLagerAsync()).ReturnsAsync(true);
        user.Setup(u => u.CanOrderGlasAsync()).ReturnsAsync(true);
        var ctrl = new WarehouseRequisitionsController(repo.Object, wp.Object, Mock.Of<IOrderRecipientRepository>(), user.Object,
            Mock.Of<IAppSettingRepository>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        ctrl.TempData = new TempDataDictionary(ctrl.HttpContext, Mock.Of<ITempDataProvider>());

        await ctrl.Index(angefragt);

        repo.Verify(r => r.GetShortageCountsForUserAsync(7, erwartet), Times.Once);
    }

    // ---- AK 4-6: Werkbank-Auswahl ----
    private static (MissingPartsController ctrl, Mock<IProductionWorkplaceRepository> wp) BuildMp()
    {
        var repo = new Mock<IWarehouseRequisitionRepository>();
        repo.Setup(r => r.GetMissingPartsAsync(It.IsAny<ShortageStatus>(), It.IsAny<WarehouseRequisitionType?>(),
                It.IsAny<IReadOnlyCollection<int>?>(), It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(((IReadOnlyList<MissingPartRow>)new List<MissingPartRow>(), 0));
        var wp = new Mock<IProductionWorkplaceRepository>();
        wp.Setup(w => w.GetByUserIdAsync(1)).ReturnsAsync(new List<ProductionWorkplace>
        {
            new() { Id = 8, Name = "Werkbank B" }, new() { Id = 7, Name = "Werkbank A" }
        });
        wp.Setup(w => w.GetAllAsync()).ReturnsAsync(new List<ProductionWorkplace>
        {
            new() { Id = 7, Name = "Werkbank A" }, new() { Id = 8, Name = "Werkbank B" }, new() { Id = 99, Name = "Fremd" }
        });
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(1);
        user.Setup(u => u.CanOrderLagerAsync()).ReturnsAsync(true);
        var ctrl = new MissingPartsController(repo.Object, wp.Object, user.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return (ctrl, wp);
    }

    private static MissingPartsListViewModel Vm(IActionResult r) => (MissingPartsListViewModel)((ViewResult)r).Model!;

    [Fact]
    public async Task MineOnly_NurEigeneWerkbaenkeNachName_KeinGetAll_AK4()
    {
        var (ctrl, wp) = BuildMp();
        var vm = Vm(await ctrl.Index(mineOnly: true));
        vm.AvailableWorkplaces.Select(w => w.Id).Should().Equal(7, 8);
        vm.ForeignWorkplaceSelected.Should().BeFalse();
        wp.Verify(w => w.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task MineOnlyAus_AlleWerkbaenke_AK5()
    {
        var (ctrl, _) = BuildMp();
        Vm(await ctrl.Index(mineOnly: false)).AvailableWorkplaces.Select(w => w.Id).Should().Equal(99, 7, 8);  // nach Name: Fremd, Werkbank A, Werkbank B
    }

    [Theory]
    [InlineData(99, true)]
    [InlineData(7, false)]
    public async Task MineOnly_FremdeWerkbank_Hinweis_Leer_AK6(int workplaceId, bool fremd)
    {
        var (ctrl, _) = BuildMp();
        var vm = Vm(await ctrl.Index(workplaceId: workplaceId, mineOnly: true));
        vm.ForeignWorkplaceSelected.Should().Be(fremd);
        vm.WorkplaceFilter.Should().Be(fremd ? null : workplaceId, "Dropdown steht bei fremder Werkbank auf „— Alle —“");
        (vm.Items.Count, vm.WaitingTotalCount, vm.NoRestockTotalCount).Should().Be((0, 0, 0));
    }

    // ---- AK 3, 7, 8: Views ----
    private static string ViewText(params string[] teile)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Views"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(new[] { dir!.FullName, "IdealAkeWms", "Views" }.Concat(teile).ToArray()));
    }

    [Fact]
    public void Views_KartenLinksMitTyp_DropdownNichtGesperrt_HakenAbwaehlbar_AK3_AK7_AK8()
    {
        var karte = ViewText("WarehouseRequisitions", "Index.cshtml");
        System.Text.RegularExpressions.Regex.Matches(karte, "asp-route-type=\"@Model.ActiveType\" asp-route-mineOnly=\"true\"").Count.Should().Be(2);

        var mp = ViewText("MissingParts", "Index.cshtml");
        var select = mp.Substring(mp.IndexOf("<select name=\"workplaceId\"", StringComparison.Ordinal));
        select.Substring(0, select.IndexOf('>')).Should().NotContain("disabled");
        var checkbox = mp.IndexOf("id=\"mineOnly\"", StringComparison.Ordinal);
        mp.IndexOf("<input type=\"hidden\" name=\"mineOnly\" value=\"false\" />", StringComparison.Ordinal).Should().BeGreaterThan(checkbox,
            "das versteckte false-Feld steht HINTER der Checkbox (Binder nimmt den ersten Wert)");
        mp.Should().Contain("@if (Model.ForeignWorkplaceSelected)");
    }
}

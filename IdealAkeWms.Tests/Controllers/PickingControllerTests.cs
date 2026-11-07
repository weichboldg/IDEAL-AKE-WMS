using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class PickingControllerTests
{
    /// <summary>
    /// v1.88.0 Auftragsliste: PickingController mit Kommissionierauftraegen aus <see cref="IPickingOrderRepository.GetListeAsync"/>.
    /// LeitstandAktiv=true (sonst IndexDropdown), KommissionierTage=4, VorkommissionierTage=1, keine Feiertage.
    /// </summary>
    private static PickingController Setup(List<PickingOrder> auftraege, string queryString, bool zuweisung = false, int? userId = null)
    {
        var current = new Mock<ICurrentUserService>();
        current.Setup(s => s.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        current.Setup(s => s.GetCurrentAppUserId()).Returns(userId);

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.LeitstandAktiv)).ReturnsAsync("true");
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.KommissionierungMitZuweisung)).ReturnsAsync(zuweisung ? "true" : "false");
        settings.Setup(s => s.GetIntValueAsync("KommissionierTage", 4)).ReturnsAsync(4);
        settings.Setup(s => s.GetIntValueAsync("VorkommissionierTage", 1)).ReturnsAsync(1);

        var holidays = new Mock<IHolidayRepository>();
        holidays.Setup(h => h.GetHolidayDatesAsync()).ReturnsAsync(new HashSet<DateTime>());

        var po = new Mock<IPickingOrderRepository>();
        po.Setup(r => r.GetListeAsync(It.IsAny<bool>())).ReturnsAsync(auftraege);
        var areas = new Mock<IPickingAreaRepository>();
        areas.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PickingArea>());

        var ctrl = new PickingController(
            Mock.Of<IProductionOrderRepository>(), Mock.Of<IProductionOrderPickingStatusRepository>(), current.Object, settings.Object,
            holidays.Object, new BusinessDayService(), Mock.Of<IBomRepository>(),
            Mock.Of<IStockMovementRepository>(), Mock.Of<IStorageLocationRepository>(), Mock.Of<IArticleRepository>(),
            Mock.Of<IPickingTransferService>(), Mock.Of<IUserRepository>(), Mock.Of<IEnaioDmsDocumentRepository>(),
            Mock.Of<IPartRequisitionRepository>(), Mock.Of<IArticleAttributeRepository>(), Mock.Of<IServiceSettingRepository>(),
            po.Object, areas.Object, null!, NullLogger<PickingController>.Instance);

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString(queryString);
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };
        return ctrl;
    }

    private static PickingOrder Auftrag(int id, string fa, DateTime? termin = null, bool vorkomm = false, int? picker = null, string bereich = "Lager",
        string? werkbank = null) => new()
    {
        Id = id, Nummer = $"KO-2026-{id:D5}", ProductionOrderId = id, Status = PickingOrderStatus.Offen, AssignedPickerId = picker,
        PickingArea = new PickingArea { Name = bereich, IstVorkommissionierung = vorkomm, CreatedBy = "t", CreatedByWindows = "t" },
        ProductionOrder = new ProductionOrder
        {
            Id = id, OrderNumber = fa, ProductionDate = termin, CreatedBy = "t", CreatedByWindows = "t",
            ProductionWorkplace = werkbank == null ? null : new ProductionWorkplace { Name = werkbank },
        },
        CreatedBy = "t", CreatedByWindows = "t",
    };

    private static ProductionOrder Order(string orderNumber, DateTime? productionDate = null) => new()
    {
        OrderNumber = orderNumber,
        ProductionDate = productionDate,
        CreatedAt = DateTime.Now,
        CreatedBy = "t",
        CreatedByWindows = "t"
    };

    // Hierarchie-Order: OrderNumber = HauptFA (Gruppenschluessel), SubOrderNumber = Sub-FA,
    // mit PickingStatus (Prio steuert die Warteschlangen-Reihenfolge).
    private static ProductionOrder OrderH(string orderNumber, string subOrderNumber, int? priority = null,
        string? parentSub = null) => new()
    {
        OrderNumber = orderNumber,
        SubOrderNumber = subOrderNumber,
        ParentSubOrderNumber = parentSub,
        CreatedAt = DateTime.Now,
        CreatedBy = "t",
        CreatedByWindows = "t",
        PickingStatus = new ProductionOrderPickingStatus
        {
            PickingPriority = priority,
            IsReleasedForPicking = true,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t",
        },
    };

    /// <summary>
    /// Controller fuer die Bom-Action: alle Listen-/Dictionary-Rueckgaben explizit leer gemockt
    /// (Moq liefert fuer konkrete List/Dictionary sonst null), Benutzer ohne App-Id, alle Toggles aus.
    /// </summary>
    private static PickingController SetupForBom(ProductionOrder order, BomQueryResult bomResult)
    {
        var orders = new Mock<IProductionOrderRepository>();
        orders.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var bom = new Mock<IBomRepository>();
        bom.Setup(r => r.GetBomItemsAsync(It.IsAny<BomKey>(), It.IsAny<BomScope>())).ReturnsAsync(bomResult);


        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(r => r.GetStockByArticleNumbersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new Dictionary<string, List<StockLocationInfo>>());

        var attrs = new Mock<IArticleAttributeRepository>();
        attrs.Setup(r => r.GetCategoryNamesByArticleNumbersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new Dictionary<string, string?>());

        var locations = new Mock<IStorageLocationRepository>();
        locations.Setup(r => r.GetActiveOrderedExcludingPickingTransportAsync()).ReturnsAsync(new List<StorageLocation>());
        locations.Setup(r => r.GetActivePickingTransportLocationsAsync()).ReturnsAsync(new List<StorageLocation>());
        locations.Setup(r => r.GetByCodeAsync("NAN")).ReturnsAsync((StorageLocation?)null);

        var current = new Mock<ICurrentUserService>();
        current.Setup(s => s.GetCurrentAppUserId()).Returns((int?)null);
        current.Setup(s => s.GetDisplayName()).Returns("t");
        current.Setup(s => s.GetWindowsUserName()).Returns("t");
        current.Setup(s => s.CanOrderLagerAsync()).ReturnsAsync(false);
        current.Setup(s => s.CanOrderGlasAsync()).ReturnsAsync(false);

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.LeitstandAktiv)).ReturnsAsync("true"); // v1.88.0: sonst legt Bom Auftraege an
        var po = new Mock<IPickingOrderRepository>();
        po.Setup(r => r.GetByProductionOrderAsync(It.IsAny<int>())).ReturnsAsync(new List<PickingOrder>());

        var serviceSetting = new Mock<IServiceSettingRepository>();
        serviceSetting.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var ctrl = new PickingController(
            orders.Object,
            Mock.Of<IProductionOrderPickingStatusRepository>(),
            current.Object,
            settings.Object,
            Mock.Of<IHolidayRepository>(),
            new BusinessDayService(),
            bom.Object,
            stock.Object,
            locations.Object,
            Mock.Of<IArticleRepository>(),
            Mock.Of<IPickingTransferService>(),
            Mock.Of<IUserRepository>(),
            Mock.Of<IEnaioDmsDocumentRepository>(),
            Mock.Of<IPartRequisitionRepository>(),
            attrs.Object,
            serviceSetting.Object,
            po.Object,
            Mock.Of<IPickingAreaRepository>(),
            null!, // PickingOrderWorkflow: in diesen Tests nicht aufgerufen
            NullLogger<PickingController>.Instance);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return ctrl;
    }

    /// <summary>
    /// UAT-Lauf 1, Befund U1: Die Stuecklisten-Kopfzeile nennt die eigene FA-Nummer (Z4-Konvention).
    /// Beide Faelle hier sind HauptFA (Kommissionierung nur am HauptFA, Spec 2026-09-25 — auf einer
    /// Sub-FA leitet Picking/Bom um, siehe <see cref="Bom_SubFa_RedirectsToIndexWithWarning"/>). Der
    /// Sub-FA-Kopfzeilen-Zusatz lebt seitdem in der read-only Stueckliste (ReadOnlyBomBuilderTests).
    /// </summary>
    [Theory]
    [InlineData("1035235", "1035235")] // hierarchisch: HauptFA (SubOrderNumber == OrderNumber)
    [InlineData("4711", "4711")]       // flach: Backfill-Invariante SubOrderNumber == OrderNumber
    public async Task Bom_ViewModel_CarriesSubOrderNumber(string orderNumber, string subOrderNumber)
    {
        var order = OrderH(orderNumber, subOrderNumber);
        order.Id = 101;
        order.ArticleNumber = "50001929";
        var ctrl = SetupForBom(order,
            new BomQueryResult(new List<BomItem>(), BomDataSources.FaHierarchie));

        var result = await ctrl.Bom(101, null);

        var vm = result.Should().BeOfType<ViewResult>().Which.Model.Should().BeOfType<BomViewModel>().Subject;
        vm.OrderNumber.Should().Be(orderNumber);
        vm.SubOrderNumber.Should().Be(subOrderNumber);
        vm.DataSource.Should().Be(BomDataSources.FaHierarchie);
    }

    /// <summary>
    /// BOM-Bridge: die FA-Hierarchie liefert die Sollmenge bereits als Auftragsgesamtmenge
    /// (<c>MengeIstAuftragsmenge</c>) — der Controller darf sie NICHT noch einmal mit der
    /// FA-Stueckzahl multiplizieren. Zusaetzlich landen die hierarchischen Zusatzfelder im
    /// ViewModel und die Anzeige-Flags stehen richtig (HauptFA → Vollstruktur; Picking/Bom ist seit
    /// Spec 2026-09-25 ohnehin nur auf der HauptFA erreichbar).
    /// </summary>
    [Fact]
    public async Task Bom_Hierarchical_DoesNotMultiplyQuantity_AndFlagsHierarchical()
    {
        var order = OrderH("1035235", "1035235");
        order.Id = 101; order.ArticleNumber = "A1"; order.Quantity = 4m;
        var items = new List<BomItem> { new FaHierarchyBomItem { Ressourcenummer = "L1", Position = "2", Menge = 5m, Kommissionieren = "PG" } };
        var ctrl = SetupForBom(order, new BomQueryResult(items, BomDataSources.FaHierarchie, MengeIstAuftragsmenge: true));

        var vm = (await ctrl.Bom(101, null)).Should().BeOfType<ViewResult>().Which.Model.Should().BeOfType<BomViewModel>().Subject;

        vm.Hierarchical.Should().BeTrue();
        vm.FullStructure.Should().BeTrue();             // HauptFA -> Vollstruktur
        vm.Items.Single().Menge.Should().Be(5m);        // keine × Quantity
        vm.Items.Single().Kommissionieren.Should().Be("PG");
    }

    /// <summary>
    /// Kommissionierung nur am HauptFA (Spec 2026-09-25): interaktive Stueckliste einer Sub-FA
    /// leitet mit Hinweis um, statt zu rendern — <see cref="IBomRepository"/> darf gar nicht
    /// erst gefragt werden (kein unnoetiger BOM-Query fuer eine Ansicht, die nie erscheint).
    /// </summary>
    [Fact]
    public async Task Bom_SubFa_RedirectsToIndexWithWarning()
    {
        var order = new ProductionOrder
        {
            Id = 7,
            OrderNumber = "100",
            SubOrderNumber = "100-7",
            ArticleNumber = "A",
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t",
        };
        var orders = new Mock<IProductionOrderRepository>();
        orders.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(order);
        var bom = new Mock<IBomRepository>();

        var ctrl = new PickingController(
            orders.Object,
            Mock.Of<IProductionOrderPickingStatusRepository>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IAppSettingRepository>(),
            Mock.Of<IHolidayRepository>(),
            new BusinessDayService(),
            bom.Object,
            Mock.Of<IStockMovementRepository>(),
            Mock.Of<IStorageLocationRepository>(),
            Mock.Of<IArticleRepository>(),
            Mock.Of<IPickingTransferService>(),
            Mock.Of<IUserRepository>(),
            Mock.Of<IEnaioDmsDocumentRepository>(),
            Mock.Of<IPartRequisitionRepository>(),
            Mock.Of<IArticleAttributeRepository>(),
            Mock.Of<IServiceSettingRepository>(),
            Mock.Of<IPickingOrderRepository>(),
            Mock.Of<IPickingAreaRepository>(),
            null!, // PickingOrderWorkflow: in diesen Tests nicht aufgerufen
            NullLogger<PickingController>.Instance);
        ctrl.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await ctrl.Bom(7, null);

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Index");
        ctrl.TempData["WarningMessage"].Should().Be("Kommissionierung erfolgt nur am HauptFA 100.");
        bom.Verify(r => r.GetBomItemsAsync(It.IsAny<BomKey>(), It.IsAny<BomScope>()), Times.Never);
    }

    // ---------- v1.88.0 Auftragsliste (AK 10) ----------

    [Fact]
    public async Task Index_ListetAuftraege_MitWerkbankBereichFaelligkeit_AK10()
    {
        // KommissionierTage=4: 17.07.2026 (Fr) -> 13.07.2026 (Mo); Vorkommissionierung zusaetzlich 1 AT -> 10.07.2026 (Fr)
        var ctrl = Setup(new List<PickingOrder>
        {
            Auftrag(1, "FA-1", new DateTime(2026, 7, 17), werkbank: "WB-3"),
            Auftrag(2, "FA-2", new DateTime(2026, 7, 17), vorkomm: true, bereich: "Vorkommissionierung"),
        }, "");
        var vm = ((ViewResult)await ctrl.Index()).Model.Should().BeOfType<PickingListViewModel>().Subject;
        vm.Items.Select(i => (i.Nummer, i.OrderNumber, i.WorkplaceName, i.Bereich, i.Faelligkeit)).Should().Equal(
            ("KO-2026-00001", "FA-1", "WB-3", "Lager", (DateTime?)new DateTime(2026, 7, 13)),
            ("KO-2026-00002", "FA-2", null, "Vorkommissionierung", (DateTime?)new DateTime(2026, 7, 10)));
        vm.Pagination.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Index_SpaltenfilterTextUndDatum_VorPaging_AK10()
    {
        var auftraege = new List<PickingOrder>
        {
            Auftrag(1, "FA-1001", new DateTime(2026, 7, 17)), Auftrag(2, "FA-2002", new DateTime(2026, 8, 14)),
        };
        ((PickingListViewModel)((ViewResult)await Setup(auftraege, "?colf_order-number=1001").Index()).Model!).Items
            .Should().ContainSingle().Which.OrderNumber.Should().Be("FA-1001");
        var vm = (PickingListViewModel)((ViewResult)await Setup(auftraege, "?colf_picking-date=kw33").Index()).Model!;
        vm.Items.Should().ContainSingle().Which.Nummer.Should().Be("KO-2026-00002");
        vm.Pagination.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Index_NurMeine_BeiZuweisung_AK10()
    {
        var auftraege = new List<PickingOrder> { Auftrag(1, "FA-1", picker: 7), Auftrag(2, "FA-2", picker: 8) };
        ((PickingListViewModel)((ViewResult)await Setup(auftraege, "", zuweisung: true, userId: 7).Index()).Model!).Items
            .Should().ContainSingle().Which.OrderNumber.Should().Be("FA-1");
        ((PickingListViewModel)((ViewResult)await Setup(auftraege, "", zuweisung: true, userId: 7).Index(showAll: true)).Model!).Items
            .Should().HaveCount(2);
    }

    [Fact]
    public void Auftrag_RendertStuecklistenView_QaQ1()
    {
        // QA Q1: die Action „Auftrag“ teilt BomSeiteAsync mit „Bom“ – ohne expliziten View-Namen suchte MVC „Auftrag.cshtml“ (HTTP 500).
        var src = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "IdealAkeWms", "Controllers", "PickingController.cs"));
        src.Should().Contain("return View(\"Bom\", vm);");
        File.Exists(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "IdealAkeWms", "Views", "Picking", "Auftrag.cshtml")).Should().BeFalse();
    }
}

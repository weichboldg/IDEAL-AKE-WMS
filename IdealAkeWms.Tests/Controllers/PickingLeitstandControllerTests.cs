using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>
/// Controller-Tests fuer den neuen <see cref="PickingLeitstandController"/> (Phase 2 / v1.12.0).
/// Migriert die 12 Tests aus <c>ProductionOrdersControllerPickerTests</c> auf den neuen
/// Controller; ergaenzt um Index-Test fuer Rich-ViewModel + WorkStep-Pivot
/// (seit v1.22.0 aus <see cref="IFaWorkStepRepository"/> statt AssemblyGroups).
/// </summary>
public class PickingLeitstandControllerTests
{
    private readonly Mock<IProductionOrderRepository> _orderRepo = new();
    private readonly Mock<IProductionOrderPickingStatusRepository> _pickingStatusRepo = new();
    private readonly Mock<IFaWorkStepRepository> _faWorkStepRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAppSettingRepository> _settingRepo = new();
    private readonly Mock<IHolidayRepository> _holidayRepo = new();
    private readonly Mock<IBusinessDayService> _businessDayService = new();
    private readonly Mock<IEnaioDmsDocumentRepository> _enaioDmsRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IServiceSettingRepository> _serviceSettingRepo = new();
    private readonly Mock<IProductionOrderBdeStatusRepository> _bdeStatusRepo = new();
    private readonly Mock<IBdeBookingRepository> _bdeBookingRepo = new();
    private readonly Mock<ILogger<PickingLeitstandController>> _logger = new();
    private readonly PickingLeitstandController _controller;

    public PickingLeitstandControllerTests()
    {
        _currentUser.Setup(x => x.GetDisplayName()).Returns("TestUser");
        _currentUser.Setup(x => x.GetWindowsUserName()).Returns("DOMAIN\\testuser");

        // Default-Setups (alle Settings/Lookups leer)
        _settingRepo.Setup(s => s.GetIntValueAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((string _, int defaultVal) => defaultVal);
        _settingRepo.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        _holidayRepo.Setup(h => h.GetHolidayDatesAsync()).ReturnsAsync(new HashSet<DateTime>());
        _businessDayService.Setup(b => b.ParsePickupDays(It.IsAny<string>())).Returns(new HashSet<DayOfWeek>());
        _enaioDmsRepo.Setup(e => e.GetByOrderNumbersAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, List<EnaioDmsDocumentLink>>());
        // Master ProduktionsauftragHierarchisch nicht gesetzt (null) -> Flachmodus (AKE-Verhalten).
        _serviceSettingRepo.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        _controller = new PickingLeitstandController(
            _orderRepo.Object,
            _pickingStatusRepo.Object,
            _faWorkStepRepo.Object,
            _currentUser.Object,
            _settingRepo.Object,
            _holidayRepo.Object,
            _businessDayService.Object,
            _enaioDmsRepo.Object,
            _userRepo.Object,
            _serviceSettingRepo.Object,
            _bdeStatusRepo.Object,
            _bdeBookingRepo.Object,
            _logger.Object);

        // TempData requires a provider
        _controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());

        // UrlHelper for IsLocalUrl
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns(false);
        _controller.Url = urlHelper.Object;
    }

    private static ProductionOrder MakeOrder(int id, string number, string? articleNumber = "ART-001", bool isDone = false, string? customer = null) =>
        new()
        {
            Id = id,
            OrderNumber = number,
            ArticleNumber = articleNumber,
            IsDone = isDone,
            Customer = customer,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

    private static ProductionOrderPickingStatus MakePs(int productionOrderId, bool released = false,
        int? assignedPickerId = null, string? assignedPickerName = null, int? priority = null,
        bool hasGlass = false, bool hasExternalPurchase = false, bool hasCoatingParts = false) =>
        new()
        {
            ProductionOrderId = productionOrderId,
            IsReleasedForPicking = released,
            AssignedPickerId = assignedPickerId,
            AssignedPickerName = assignedPickerName,
            PickingPriority = priority,
            HasGlass = hasGlass,
            HasExternalPurchase = hasExternalPurchase,
            HasCoatingParts = hasCoatingParts,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

    // --- Index Tests ---

    private static LeitstandOrderRow MakeRow(int id, string number, string? articleNumber = "ART-001", bool isDone = false,
        string? customer = null, bool isDonePicking = false,
        DateTime? productionDate = null, string? workplaceName = null, int? overridePrePickingDays = null) =>
        new(id, number, 1m, customer, articleNumber, null, null, productionDate, null, isDone, isDonePicking, false, workplaceName,
            null, null, null, null, null, overridePrePickingDays);

    private static LeitstandOrderPage MakePage(params LeitstandOrderRow[] rows) =>
        new(rows.ToList(), rows.Length);

    // Hierarchie-Zeile: OrderNumber = HauptFA (Gruppenschluessel), SubOrderNumber = Sub-FA.
    private static LeitstandOrderRow MakeRowH(int id, string orderNumber, string subOrderNumber,
        string? parentSub = null) =>
        new(id, orderNumber, 1m, null, "ART-001", null, null, null, null, false, false, false, null,
            SubOrderNumber: subOrderNumber, ParentSubOrderNumber: parentSub);

    // Aktiviert den hierarchischen Modus (Master ProduktionsauftragHierarchisch = true).
    private void EnableHierarchical() =>
        _serviceSettingRepo
            .Setup(s => s.GetValueAsync(
                IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturKeys.Master))
            .ReturnsAsync("true");

    [Fact]
    public async Task Index_Hierarchical_GroupsByHauptFa_WithZ4Counts_AndPaginatesOverGroups()
    {
        EnableHierarchical();
        // HauptFA H1 mit zwei Sub-FAs, HauptFA H2 mit einer Sub-FA.
        var grouped = new LeitstandOrderGroupedPage(
            new List<LeitstandOrderGroup>
            {
                new("H1", new List<LeitstandOrderRow>
                {
                    MakeRowH(1, "H1", "H1-1"),
                    MakeRowH(2, "H1", "H1-2", parentSub: "H1-1"),
                }),
                new("H2", new List<LeitstandOrderRow> { MakeRowH(3, "H2", "H2-1") }),
            },
            TotalGroupCount: 2,
            TotalRowCount: 3);

        _orderRepo.Setup(r => r.GetForLeitstandGroupedAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(grouped);
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>());
        _faWorkStepRepo.Setup(r => r.GetWorkStepDetailPivotAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, Dictionary<string, FaWorkStepPivotCell>>());

        var result = await _controller.Index(null, null, null, false, 1, null);

        var vm = ((ViewResult)result).Model.Should().BeOfType<PickingLeitstandViewModel>().Subject;
        vm.Hierarchical.Should().BeTrue();
        vm.Groups.Should().HaveCount(2);
        vm.HierarchicalRowCount.Should().Be(3);
        vm.Pagination.TotalCount.Should().Be(2); // Paginierung ueber Gruppen

        var g1 = vm.Groups.Single(g => g.OrderNumber == "H1");
        g1.Items.Select(i => i.SubOrderNumber).Should().ContainInOrder("H1-1", "H1-2");
        g1.Items.Single(i => i.SubOrderNumber == "H1-2").ParentSubOrderNumber.Should().Be("H1-1");
        // Der flache GetForLeitstandAsync darf im hierarchischen Modus NICHT aufgerufen werden.
        _orderRepo.Verify(r => r.GetForLeitstandAsync(
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()), Times.Never);
    }

    [Fact]
    public async Task Index_FlatMode_LeavesHierarchicalFalse()
    {
        // Regression: Master aus (default null) → bit-identisch zu heute, keine Gruppierung.
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(MakePage(MakeRow(1, "FA-100")));
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>());
        _faWorkStepRepo.Setup(r => r.GetWorkStepDetailPivotAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, Dictionary<string, FaWorkStepPivotCell>>());

        var result = await _controller.Index(null, null, null, false, 1, null);

        var vm = ((ViewResult)result).Model.Should().BeOfType<PickingLeitstandViewModel>().Subject;
        vm.Hierarchical.Should().BeFalse();
        vm.Groups.Should().BeEmpty();
        vm.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Index_ReturnsRichViewModel_WithStatusPivot()
    {
        var row1 = MakeRow(1, "FA-100", articleNumber: "ART-1", customer: "Kunde A");
        var row2 = MakeRow(2, "FA-200", articleNumber: "ART-2", customer: "Kunde B");
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(MakePage(row1, row2));

        var ps1 = MakePs(1, released: true, priority: 1, hasGlass: true);
        var ps2 = MakePs(2, released: false, hasExternalPurchase: true);
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>
            {
                { 1, ps1 },
                { 2, ps2 }
            });

        // Detail-Pivot: Code -> Cell(FaWorkStepId, Status). Fehlender Code = AG nicht anwendbar.
        _faWorkStepRepo.Setup(r => r.GetWorkStepDetailPivotAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, Dictionary<string, FaWorkStepPivotCell>>
            {
                { 1, new Dictionary<string, FaWorkStepPivotCell>
                    {
                        { "VK", new FaWorkStepPivotCell(101, FaWorkStepStatus.Fertig) },
                        { "VE", new FaWorkStepPivotCell(103, FaWorkStepStatus.Offen) }
                    } },
                { 2, new Dictionary<string, FaWorkStepPivotCell>
                    {
                        { "VL", new FaWorkStepPivotCell(202, FaWorkStepStatus.Fertig) },
                        { "VT", new FaWorkStepPivotCell(204, FaWorkStepStatus.Offen) },
                        { "VA", new FaWorkStepPivotCell(205, FaWorkStepStatus.Fertig) }
                    } }
            });

        _currentUser.Setup(u => u.CanPickAsync()).ReturnsAsync(true);
        _currentUser.Setup(u => u.CanManagePickingReleaseAsync()).ReturnsAsync(true);

        var result = await _controller.Index(null, null, null, false, 1, null);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var vm = viewResult.Model.Should().BeOfType<PickingLeitstandViewModel>().Subject;
        vm.Items.Should().HaveCount(2);

        var item1 = vm.Items.First(i => i.Id == 1);
        item1.IsReleasedForPicking.Should().BeTrue();
        item1.PickingPriority.Should().Be(1);
        item1.HasGlass.Should().BeTrue();
        item1.WorkSteps.Should().ContainKey("VK");
        item1.WorkSteps["VK"].Status.Should().Be(FaWorkStepStatus.Fertig);   // VK erledigt
        item1.WorkSteps["VK"].FaWorkStepId.Should().Be(101);
        item1.WorkSteps.Should().ContainKey("VE");
        item1.WorkSteps["VE"].Status.Should().Be(FaWorkStepStatus.Offen);  // VE offen
        item1.WorkSteps.Should().NotContainKey("VL");          // nicht anwendbar -> leere Zelle

        var item2 = vm.Items.First(i => i.Id == 2);
        item2.IsReleasedForPicking.Should().BeFalse();
        item2.HasExternalPurchase.Should().BeTrue();
        item2.WorkSteps["VL"].Status.Should().Be(FaWorkStepStatus.Fertig);   // VL erledigt
        item2.WorkSteps["VA"].Status.Should().Be(FaWorkStepStatus.Fertig);   // VA erledigt
        item2.WorkSteps["VT"].Status.Should().Be(FaWorkStepStatus.Offen);  // VT offen
        item2.WorkSteps.Should().NotContainKey("VK");

        vm.CanPick.Should().BeTrue();
        vm.CanManagePickingRelease.Should().BeTrue();
    }

    [Fact]
    public async Task Index_FilteringHidesDoneOrders_ByDefault()
    {
        // Filter happens server-side in repo; mock returns already-filtered list.
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), false,
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(MakePage(MakeRow(1, "FA-OPEN", isDone: false)));
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>());
        _faWorkStepRepo.Setup(r => r.GetWorkStepDetailPivotAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, Dictionary<string, FaWorkStepPivotCell>>());

        var result = await _controller.Index(null, null, null, showDone: false, page: 1, pageSize: null);

        var vm = (PickingLeitstandViewModel)((ViewResult)result).Model!;
        vm.Items.Should().HaveCount(1);
        vm.Items.Single().OrderNumber.Should().Be("FA-OPEN");
    }

    [Fact]
    public async Task Index_MapsIsDoneCombined_WhenIsDonePickingTrue()
    {
        // ToggleDone schreibt PickingStatus.IsDonePicking — die View bindet item.IsDone.
        // Erwartung: ViewModel-IsDone = Sage-IsDone ODER App-IsDonePicking.
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(MakePage(MakeRow(1, "FA-100", isDone: false, isDonePicking: true)));
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>());
        _faWorkStepRepo.Setup(r => r.GetWorkStepDetailPivotAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, Dictionary<string, FaWorkStepPivotCell>>());

        var result = await _controller.Index(null, null, null, showDone: true, page: 1, pageSize: null);

        var vm = (PickingLeitstandViewModel)((ViewResult)result).Model!;
        vm.Items.Should().HaveCount(1);
        vm.Items.Single().IsDone.Should().BeTrue();
    }

    // --- ToggleRelease Tests ---

    [Fact]
    public async Task ToggleRelease_FlipsIsReleased_AndAuditFields()
    {
        var order = MakeOrder(1, "FA-100");
        var ps = MakePs(productionOrderId: 1, released: false);

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdAsync(1)).ReturnsAsync(ps);
        _pickingStatusRepo.Setup(r => r.GetReleasedForPickingAsync()).ReturnsAsync(new List<ProductionOrder>());
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("false");

        var result = await _controller.ToggleRelease(1, assignedPickerId: null, returnUrl: null);

        result.Should().BeOfType<RedirectToActionResult>();
        _pickingStatusRepo.Verify(r => r.SetReleaseAsync(
            1, true, It.IsAny<int?>(),
            "TestUser", "TestUser", "DOMAIN\\testuser"), Times.Once);
    }

    [Fact]
    public async Task ToggleRelease_UnknownOrder_ReturnsNotFound()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ProductionOrder?)null);

        var result = await _controller.ToggleRelease(999, assignedPickerId: null, returnUrl: null);

        result.Should().BeOfType<NotFoundResult>();
        _pickingStatusRepo.Verify(r => r.SetReleaseAsync(
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ToggleRelease_MissingPickingStatusRow_ReturnsNotFound()
    {
        var order = MakeOrder(1, "FA-100");
        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdAsync(1)).ReturnsAsync((ProductionOrderPickingStatus?)null);

        var result = await _controller.ToggleRelease(1, assignedPickerId: null, returnUrl: null);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ToggleRelease_ReleaseWithPicker_SetsAssignedPickerFields()
    {
        var order = MakeOrder(1, "FA-100");
        var ps = MakePs(productionOrderId: 1, released: false);
        var picker = new User
        {
            Id = 5,
            Name = "Max Mustermann",
            IsActive = true,
            IsPicker = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdAsync(1)).ReturnsAsync(ps);
        _pickingStatusRepo.Setup(r => r.GetReleasedForPickingAsync()).ReturnsAsync(new List<ProductionOrder>());
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("true");
        _userRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(picker);

        await _controller.ToggleRelease(1, assignedPickerId: 5, returnUrl: null);

        _pickingStatusRepo.Verify(r => r.SetReleaseAsync(
            1, true, It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _pickingStatusRepo.Verify(r => r.SetAssignedPickerAsync(
            1, 5, "Max Mustermann",
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ToggleRelease_ReleaseWithoutPicker_WhenFeatureEnabled_ReturnsWarning()
    {
        var order = MakeOrder(1, "FA-100");
        var ps = MakePs(productionOrderId: 1, released: false);

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdAsync(1)).ReturnsAsync(ps);
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("true");

        var result = await _controller.ToggleRelease(1, assignedPickerId: null, returnUrl: null);

        result.Should().BeOfType<RedirectToActionResult>();
        _controller.TempData["WarningMessage"].Should().Be("Bitte einen Kommissionierer zuweisen.");
        _pickingStatusRepo.Verify(r => r.SetReleaseAsync(
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ToggleRelease_Revoke_DoesNotCallSetAssignedPicker()
    {
        var order = MakeOrder(1, "FA-100");
        var ps = MakePs(productionOrderId: 1, released: true, assignedPickerId: 5, assignedPickerName: "Max Mustermann");

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdAsync(1)).ReturnsAsync(ps);
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("true");

        await _controller.ToggleRelease(1, assignedPickerId: null, returnUrl: null);

        _pickingStatusRepo.Verify(r => r.SetReleaseAsync(
            1, false, It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _pickingStatusRepo.Verify(r => r.SetAssignedPickerAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // --- BulkRelease Tests ---

    [Fact]
    public async Task BulkRelease_FlipsMultipleOrders()
    {
        _pickingStatusRepo.Setup(r => r.SetReleaseBatchAsync(
                It.IsAny<IEnumerable<int>>(), true, It.IsAny<int?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new BulkReleaseResult { Processed = 2 });
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("false");

        await _controller.BulkRelease(new List<int> { 1, 2 }, release: true, assignedPickerId: null, returnUrl: null);

        _pickingStatusRepo.Verify(r => r.SetReleaseBatchAsync(
            It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 1, 2 })),
            true, null, null, "TestUser", "TestUser", "DOMAIN\\testuser"), Times.Once);
    }

    [Fact]
    public async Task BulkRelease_SkipsOrdersWithoutArticleNumber()
    {
        // Repo-Layer is responsible for the skip; controller relays the warning.
        _pickingStatusRepo.Setup(r => r.SetReleaseBatchAsync(
                It.IsAny<IEnumerable<int>>(), true, It.IsAny<int?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new BulkReleaseResult
            {
                Processed = 1,
                SkippedNoArticle = new List<string> { "FA-2-NO-ART" }
            });
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("false");

        await _controller.BulkRelease(new List<int> { 1, 2 }, release: true, assignedPickerId: null, returnUrl: null);

        _controller.TempData["WarningMessage"].Should().NotBeNull();
        ((string)_controller.TempData["WarningMessage"]!).Should().Contain("FA-2-NO-ART");
    }

    [Fact]
    public async Task BulkRelease_EmptyIds_DoesNothing()
    {
        var result = await _controller.BulkRelease(new List<int>(), release: true, assignedPickerId: null, returnUrl: null);

        result.Should().BeOfType<RedirectToActionResult>();
        _pickingStatusRepo.Verify(r => r.SetReleaseBatchAsync(
            It.IsAny<IEnumerable<int>>(), It.IsAny<bool>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // --- SetPriority Tests ---

    [Fact]
    public async Task SetPriority_PersistsValue()
    {
        var order = MakeOrder(1, "FA-100");
        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _controller.SetPriority(1, priority: 42);

        result.Should().BeOfType<OkResult>();
        _pickingStatusRepo.Verify(r => r.SetPriorityAsync(
            1, 42,
            "TestUser", "DOMAIN\\testuser"), Times.Once);
    }

    [Fact]
    public async Task SetPriority_UnknownOrder_ReturnsNotFound()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ProductionOrder?)null);

        var result = await _controller.SetPriority(999, priority: 1);

        result.Should().BeOfType<NotFoundResult>();
        _pickingStatusRepo.Verify(r => r.SetPriorityAsync(
            It.IsAny<int>(), It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // --- ChangeAssignedPicker Tests ---

    [Fact]
    public async Task ChangeAssignedPicker_PersistsPickerName()
    {
        var order = MakeOrder(1, "FA-100");
        var newPicker = new User
        {
            Id = 10,
            Name = "New Picker",
            IsActive = true,
            IsPicker = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _userRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(newPicker);

        var result = await _controller.ChangeAssignedPicker(1, 10);

        result.Should().BeOfType<OkResult>();
        _pickingStatusRepo.Verify(r => r.SetAssignedPickerAsync(
            1, 10, "New Picker",
            "TestUser", "DOMAIN\\testuser"), Times.Once);
    }

    [Fact]
    public async Task ChangeAssignedPicker_NonExistentOrder_ReturnsNotFound()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ProductionOrder?)null);

        var result = await _controller.ChangeAssignedPicker(999, 10);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ChangeAssignedPicker_NonExistentPicker_ReturnsBadRequest()
    {
        var order = MakeOrder(1, "FA-100");

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _userRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        var result = await _controller.ChangeAssignedPicker(1, 999);

        result.Should().BeOfType<BadRequestObjectResult>();
        ((BadRequestObjectResult)result).Value.Should().Be("Kommissionierer nicht gefunden.");
    }

    // ---------------------------------------------------- FA-Zusatzinfos (Sage, v1.26.0)

    [Fact]
    public async Task Index_MapsExtraInfoFields_FromLeitstandRow()
    {
        var row = new LeitstandOrderRow(
            1, "FA-100", 1m, null, "ART-001", null, null, null, null,
            false, false, false, null,
            Kaeltemittel: "R290", Ventil: "Danfoss", AusfuehrungEZ: "Z",
            Maschine: "M2", SageStatus: "abgeholt");
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(MakePage(row));
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>());
        _faWorkStepRepo.Setup(r => r.GetWorkStepDetailPivotAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, Dictionary<string, FaWorkStepPivotCell>>());

        var result = await _controller.Index(null, null, null) as ViewResult;

        var vm = result!.Model.Should().BeOfType<PickingLeitstandViewModel>().Subject;
        var item = vm.Items.Should().ContainSingle().Subject;
        item.Kaeltemittel.Should().Be("R290");
        item.Ventil.Should().Be("Danfoss");
        item.AusfuehrungEZ.Should().Be("Z");
        item.Maschine.Should().Be("M2");
        item.SageStatus.Should().Be("abgeholt");
    }

    [Fact]
    public async Task Index_ZusatzinfoColumnFilter_IsPassedToSqlFilters_NotMemoryFiltered()
    {
        // Invariante: die 5 neuen Keys duerfen NICHT in LeitstandDateColumnKeys /
        // LeitstandWorkStepColumnKeys landen — sie laufen als SQL-Filter durch
        // (SQL-Pagination bleibt erhalten, Spec §5.3).
        IReadOnlyDictionary<string, string>? captured = null;
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .Callback<string?, string?, string?, bool, int, int, IReadOnlyDictionary<string, string>?>(
                (_, _, _, _, _, _, colf) => captured = colf)
            .ReturnsAsync(MakePage(MakeRow(1, "FA-100")));
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>());
        _faWorkStepRepo.Setup(r => r.GetWorkStepDetailPivotAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, Dictionary<string, FaWorkStepPivotCell>>());

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_maschine=m1");
        _controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        await _controller.Index(null, null, null);

        captured.Should().NotBeNull();
        captured!.Should().ContainKey("maschine").WhoseValue.Should().Be("m1");
    }

    // ------------------------- Werkbank-Override „Abweichende Vorkommissioniertage" (v1.27.0)

    /// <summary>Kalendertage statt Arbeitstage — testet die verwendete TAGE-ANZAHL, nicht BusinessDayService.</summary>
    private void SetupCalendarDaySubtraction() =>
        _businessDayService.Setup(b => b.SubtractBusinessDays(
                It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<HashSet<DateTime>>()))
            .Returns((DateTime d, int days, HashSet<DateTime> _) => d.AddDays(-days));

    private void SetupRow(LeitstandOrderRow row)
    {
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(MakePage(row));
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>());
        _faWorkStepRepo.Setup(r => r.GetWorkStepDetailPivotAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, Dictionary<string, FaWorkStepPivotCell>>());
    }

    [Fact]
    public async Task Index_WerkbankOverride_SchlaegtGlobalenVorkommissionierWert()
    {
        // Muss dasselbe Ergebnis liefern wie ProductionOrdersController (gemeinsamer
        // PrePickingDaysResolver, gemeinsame LeitstandOrderRow-Projection).
        SetupCalendarDaySubtraction();
        SetupRow(MakeRow(1, "FA-100", productionDate: new DateTime(2026, 8, 10),
            workplaceName: "A1", overridePrePickingDays: 7));

        var vm = (PickingLeitstandViewModel)((ViewResult)await _controller.Index(null, null, null)).Model!;

        var item = vm.Items.Single();
        item.KommissionierTermin.Should().Be(new DateTime(2026, 8, 6));
        item.VorkommissionierTermin.Should().Be(new DateTime(2026, 7, 30));
        item.PrePickingDaysOverride.Should().Be(7);
    }

    [Fact]
    public async Task Index_OhneWerkbankOverride_NutztGlobalenWert()
    {
        SetupCalendarDaySubtraction();
        SetupRow(MakeRow(1, "FA-100", productionDate: new DateTime(2026, 8, 10),
            workplaceName: "B2", overridePrePickingDays: null));

        var vm = (PickingLeitstandViewModel)((ViewResult)await _controller.Index(null, null, null)).Model!;

        var item = vm.Items.Single();
        item.VorkommissionierTermin.Should().Be(new DateTime(2026, 8, 5));
        item.PrePickingDaysOverride.Should().BeNull();
    }

    [Fact]
    public async Task Index_WerkbankOverrideNull_IstExpliziterWert_NichtKeinOverride()
    {
        SetupCalendarDaySubtraction();
        SetupRow(MakeRow(1, "FA-100", productionDate: new DateTime(2026, 8, 10),
            workplaceName: "C3", overridePrePickingDays: 0));

        var vm = (PickingLeitstandViewModel)((ViewResult)await _controller.Index(null, null, null)).Model!;

        var item = vm.Items.Single();
        item.VorkommissionierTermin.Should().Be(item.KommissionierTermin);
        item.PrePickingDaysOverride.Should().Be(0);
    }

    [Fact]
    public async Task Index_WerkbankOverride_VerschiebtBeschichtungsterminKaskadierend()
    {
        // Kaskade ueber CoatingDateCalculator: frueherer BG-Termin => fruehere Beschichtung.
        // Feature inaktiv (LackierteilKategorieName = null) => Termin fuer ALLE Auftraege.
        SetupCalendarDaySubtraction();
        _businessDayService.Setup(b => b.FindPreviousPickupDay(It.IsAny<DateTime>(), It.IsAny<HashSet<DayOfWeek>>()))
            .Returns((DateTime d, HashSet<DayOfWeek> _) => d);
        SetupRow(MakeRow(1, "FA-100", productionDate: new DateTime(2026, 8, 10),
            workplaceName: "A1", overridePrePickingDays: 7));

        var vm = (PickingLeitstandViewModel)((ViewResult)await _controller.Index(null, null, null)).Model!;

        var item = vm.Items.Single();
        // BG 30.07. - BeschichtungTage (Default 10) = 20.07. — ohne Override waere es der 26.07.
        item.BeschichtungTermin.Should().Be(new DateTime(2026, 7, 20));
    }

    // --- Kaskade „Alle Sub-FAs fertigmelden" (Etappe C) ---

    [Fact]
    public async Task CascadeDonePreview_ReturnsTotalAndOpenBookingCounts()
    {
        _orderRepo.Setup(r => r.CountByOrderNumberAsync("H1")).ReturnsAsync(4);
        _bdeBookingRepo.Setup(r => r.CountSubFasWithOpenBookingForOrderNumberAsync("H1")).ReturnsAsync(2);

        var result = await _controller.CascadeDonePreview("H1");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        // Anonymes Objekt { orderNumber, totalSubFas, withOpenBooking } per Reflection pruefen.
        var val = ok.Value!;
        val.GetType().GetProperty("totalSubFas")!.GetValue(val).Should().Be(4);
        val.GetType().GetProperty("withOpenBooking")!.GetValue(val).Should().Be(2);
    }

    [Fact]
    public async Task CascadeDonePreview_BadRequest_WhenOrderNumberMissing()
    {
        (await _controller.CascadeDonePreview(null)).Should().BeOfType<BadRequestObjectResult>();
        (await _controller.CascadeDonePreview("  ")).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CascadeDone_SetsIsDoneBdeForWholeOrderNumber_AndRedirects()
    {
        _bdeStatusRepo
            .Setup(r => r.SetIsDoneBdeForOrderNumberAsync("H1", true, "TestUser", "DOMAIN\\testuser"))
            .ReturnsAsync(3);

        var result = await _controller.CascadeDone("H1", returnUrl: null);

        result.Should().BeOfType<RedirectToActionResult>();
        // Kaskade setzt ausschliesslich true (keine Gruppen-Ruecknahme) auf ALLE Nachfahren der HauptFA.
        _bdeStatusRepo.Verify(r => r.SetIsDoneBdeForOrderNumberAsync("H1", true, "TestUser", "DOMAIN\\testuser"), Times.Once);
    }

    [Fact]
    public async Task CascadeDone_DoesNothing_WhenOrderNumberMissing()
    {
        var result = await _controller.CascadeDone("", returnUrl: null);

        result.Should().BeOfType<RedirectToActionResult>();
        _bdeStatusRepo.Verify(r => r.SetIsDoneBdeForOrderNumberAsync(
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}

using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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

        _controller = new PickingLeitstandController(
            _orderRepo.Object,
            _pickingStatusRepo.Object,
            _faWorkStepRepo.Object,
            _currentUser.Object,
            _settingRepo.Object,
            _holidayRepo.Object,
            _businessDayService.Object,
            _enaioDmsRepo.Object,
            _userRepo.Object);

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

    private static LeitstandOrderRow MakeRow(int id, string number, string? articleNumber = "ART-001", bool isDone = false, string? customer = null, bool isDonePicking = false) =>
        new(id, number, 1m, customer, articleNumber, null, null, null, null, isDone, isDonePicking, null);

    private static LeitstandOrderPage MakePage(params LeitstandOrderRow[] rows) =>
        new(rows.ToList(), rows.Length);

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

        // Detail-Pivot: Code -> Cell(FaWorkStepId, IsCompleted). Fehlender Code = AG nicht anwendbar.
        _faWorkStepRepo.Setup(r => r.GetWorkStepDetailPivotAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, Dictionary<string, FaWorkStepPivotCell>>
            {
                { 1, new Dictionary<string, FaWorkStepPivotCell>
                    {
                        { "VK", new FaWorkStepPivotCell(101, true) },
                        { "VE", new FaWorkStepPivotCell(103, false) }
                    } },
                { 2, new Dictionary<string, FaWorkStepPivotCell>
                    {
                        { "VL", new FaWorkStepPivotCell(202, true) },
                        { "VT", new FaWorkStepPivotCell(204, false) },
                        { "VA", new FaWorkStepPivotCell(205, true) }
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
        item1.WorkSteps["VK"].IsCompleted.Should().BeTrue();   // VK erledigt
        item1.WorkSteps["VK"].FaWorkStepId.Should().Be(101);
        item1.WorkSteps.Should().ContainKey("VE");
        item1.WorkSteps["VE"].IsCompleted.Should().BeFalse();  // VE offen
        item1.WorkSteps.Should().NotContainKey("VL");          // nicht anwendbar -> leere Zelle

        var item2 = vm.Items.First(i => i.Id == 2);
        item2.IsReleasedForPicking.Should().BeFalse();
        item2.HasExternalPurchase.Should().BeTrue();
        item2.WorkSteps["VL"].IsCompleted.Should().BeTrue();   // VL erledigt
        item2.WorkSteps["VA"].IsCompleted.Should().BeTrue();   // VA erledigt
        item2.WorkSteps["VT"].IsCompleted.Should().BeFalse();  // VT offen
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
}

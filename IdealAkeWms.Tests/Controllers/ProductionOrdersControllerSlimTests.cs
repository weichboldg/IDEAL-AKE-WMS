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
/// Controller-Tests fuer den nach Phase 2 (v1.12.0) abgespeckten <see cref="ProductionOrdersController"/>.
/// Index liefert nur noch <see cref="ProductionOrderListViewModel"/> (Sage-Master + Coating-Flags) und
/// die 4 alten Mutationen sind durch 301-Compat-Redirects auf PickingLeitstand ersetzt.
/// </summary>
public class ProductionOrdersControllerSlimTests
{
    private readonly Mock<IProductionOrderRepository> _orderRepo = new();
    private readonly Mock<IProductionOrderPickingStatusRepository> _pickingStatusRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAppSettingRepository> _settingRepo = new();
    private readonly Mock<IHolidayRepository> _holidayRepo = new();
    private readonly Mock<IBusinessDayService> _businessDayService = new();
    private readonly Mock<IEnaioDmsDocumentRepository> _enaioDmsRepo = new();
    private readonly ProductionOrdersController _controller;

    public ProductionOrdersControllerSlimTests()
    {
        _currentUser.Setup(x => x.GetDisplayName()).Returns("TestUser");
        _currentUser.Setup(x => x.GetWindowsUserName()).Returns("DOMAIN\\testuser");

        _settingRepo.Setup(s => s.GetIntValueAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((string _, int defaultVal) => defaultVal);
        _settingRepo.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        _holidayRepo.Setup(h => h.GetHolidayDatesAsync()).ReturnsAsync(new HashSet<DateTime>());
        _businessDayService.Setup(b => b.ParsePickupDays(It.IsAny<string>())).Returns(new HashSet<DayOfWeek>());
        _enaioDmsRepo.Setup(e => e.GetByOrderNumbersAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, List<EnaioDmsDocumentLink>>());

        _controller = new ProductionOrdersController(
            _orderRepo.Object,
            _pickingStatusRepo.Object,
            _currentUser.Object,
            _settingRepo.Object,
            _holidayRepo.Object,
            _businessDayService.Object,
            _enaioDmsRepo.Object);

        _controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());
    }

    private static ProductionOrder MakeOrder(int id, string number, bool isDone = false) =>
        new()
        {
            Id = id,
            OrderNumber = number,
            IsDone = isDone,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

    private static LeitstandOrderRow MakeRow(int id, string number, bool isDone = false, bool isDonePicking = false,
        DateTime? productionDate = null, string? workplaceName = null, int? overridePrePickingDays = null) =>
        new(id, number, 1m, null, "ART-001", null, null, productionDate, null, isDone, isDonePicking, false, workplaceName,
            null, null, null, null, null, overridePrePickingDays);

    private static LeitstandOrderPage MakePage(params LeitstandOrderRow[] rows) =>
        new(rows.ToList(), rows.Length);

    [Fact]
    public async Task Index_ReturnsSlimViewModel_NoStatusPivot()
    {
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(MakePage(MakeRow(1, "FA-100")));

        var ps = new ProductionOrderPickingStatus
        {
            ProductionOrderId = 1,
            HasCoatingParts = true,
            IsCoatingDone = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus> { { 1, ps } });

        _currentUser.Setup(u => u.CanPickAsync()).ReturnsAsync(true);

        var result = await _controller.Index(null, null, null, false, 1, null);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var vm = viewResult.Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;
        vm.Items.Should().HaveCount(1);

        var item = vm.Items.Single();
        // ProductionOrderListItem hat ABSICHTLICH keine PickingStatus-Felder ausser Coating (Spec 6.1)
        typeof(ProductionOrderListItem).GetProperty("PickingStatus").Should().BeNull();
        typeof(ProductionOrderListItem).GetProperty("IsReleasedForPicking").Should().BeNull();
        typeof(ProductionOrderListItem).GetProperty("HasGlass").Should().BeNull();
        typeof(ProductionOrderListItem).GetProperty("HasExternalPurchase").Should().BeNull();
        typeof(ProductionOrderListItem).GetProperty("HasCooling").Should().BeNull();
        typeof(ProductionOrderListItem).GetProperty("HasFan").Should().BeNull();
        typeof(ProductionOrderListItem).GetProperty("PickingPriority").Should().BeNull();
        typeof(ProductionOrderListItem).GetProperty("AssignedPickerId").Should().BeNull();

        item.HasCoatingParts.Should().BeTrue();
        item.IsCoatingDone.Should().BeFalse();
        vm.CanPick.Should().BeTrue();
    }

    [Fact]
    public async Task Index_SetztHasVorbauAccessAusService()
    {
        // vorbau bekommt den read-only Stueckliste-Button in der FA-Liste (v1.25.0).
        // Das ViewModel muss HasVorbauAccess aus ICurrentUserService.HasVorbauAccessAsync() spiegeln.
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(MakePage(MakeRow(1, "FA-100")));
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>());

        // Fall 1: vorbau-Zugriff vorhanden
        _currentUser.Setup(u => u.CanPickAsync()).ReturnsAsync(false);
        _currentUser.Setup(u => u.HasVorbauAccessAsync()).ReturnsAsync(true);

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = (ProductionOrderListViewModel)((ViewResult)result).Model!;
        vm.HasVorbauAccess.Should().BeTrue();
        vm.CanPick.Should().BeFalse();

        // Fall 2: kein vorbau-Zugriff
        _currentUser.Setup(u => u.HasVorbauAccessAsync()).ReturnsAsync(false);

        var result2 = await _controller.Index(null, null, null, false, 1, null);
        var vm2 = (ProductionOrderListViewModel)((ViewResult)result2).Model!;
        vm2.HasVorbauAccess.Should().BeFalse();
    }

    [Fact]
    public async Task Index_FilterByOrderNumber_AppliesContainsFilter()
    {
        // Server-side filtering happens in the repo; controller relays the filter
        // and uses the rows that came back.
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                "100", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(MakePage(MakeRow(1, "FA-100")));
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>());

        var result = await _controller.Index("100", null, null, false, 1, null);

        var vm = (ProductionOrderListViewModel)((ViewResult)result).Model!;
        vm.Items.Should().HaveCount(1);
        vm.Items.Single().OrderNumber.Should().Be("FA-100");
        vm.FilterOrderNumber.Should().Be("100");
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

        var result = await _controller.Index(null, null, null, showDone: true, page: 1, pageSize: null);

        var vm = (ProductionOrderListViewModel)((ViewResult)result).Model!;
        vm.Items.Should().HaveCount(1);
        vm.Items.Single().IsDone.Should().BeTrue();
    }

    [Fact]
    public void ToggleRelease_Redirects301_ToPickingLeitstand()
    {
        var result = _controller.ToggleRelease();

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.Permanent.Should().BeTrue();
        redirect.ActionName.Should().Be("Index");
        redirect.ControllerName.Should().Be("PickingLeitstand");
    }

    [Fact]
    public void BulkRelease_Redirects301_ToPickingLeitstand()
    {
        var result = _controller.BulkRelease();

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.Permanent.Should().BeTrue();
        redirect.ActionName.Should().Be("Index");
        redirect.ControllerName.Should().Be("PickingLeitstand");
    }

    [Fact]
    public void SetPriority_Redirects301_ToPickingLeitstand()
    {
        var result = _controller.SetPriority();

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.Permanent.Should().BeTrue();
        redirect.ActionName.Should().Be("Index");
        redirect.ControllerName.Should().Be("PickingLeitstand");
    }

    [Fact]
    public void ChangeAssignedPicker_Redirects301_ToPickingLeitstand()
    {
        var result = _controller.ChangeAssignedPicker();

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.Permanent.Should().BeTrue();
        redirect.ActionName.Should().Be("Index");
        redirect.ControllerName.Should().Be("PickingLeitstand");
    }

    // ---------------------------------------------------- FA-Zusatzinfos (Sage, v1.26.0)

    [Fact]
    public async Task Index_MapsExtraInfoFields_FromLeitstandRow()
    {
        var row = new LeitstandOrderRow(
            1, "FA-100", 1m, null, "ART-001", null, null, null, null,
            false, false, false, null,
            Kaeltemittel: "R290", Ventil: "Danfoss", AusfuehrungEZ: "E",
            Maschine: "M1", SageStatus: "verpackt");
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(MakePage(row));
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>());

        var result = await _controller.Index(null, null, null) as ViewResult;

        var vm = result!.Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;
        var item = vm.Items.Should().ContainSingle().Subject;
        item.Kaeltemittel.Should().Be("R290");
        item.Ventil.Should().Be("Danfoss");
        item.AusfuehrungEZ.Should().Be("E");
        item.Maschine.Should().Be("M1");
        item.SageStatus.Should().Be("verpackt");
    }

    [Fact]
    public async Task Index_ZusatzinfoColumnFilter_IsPassedToSqlFilters()
    {
        // Invariante: die 5 neuen Keys sind KEINE Datums-Keys -> sie MUESSEN in den
        // SQL-Filter-Pfad (GetForLeitstandAsync columnFilters) laufen, nicht in den
        // C#-Memory-Filter (kein Force-Full-Load, Spec §5.3).
        IReadOnlyDictionary<string, string>? captured = null;
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .Callback<string?, string?, string?, bool, int, int, IReadOnlyDictionary<string, string>?>(
                (_, _, _, _, _, _, colf) => captured = colf)
            .ReturnsAsync(MakePage(MakeRow(1, "FA-100")));
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>());

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_kaeltemittel=r290&colf_sage-status=verpackt");
        _controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        await _controller.Index(null, null, null);

        captured.Should().NotBeNull();
        captured!.Should().ContainKey("kaeltemittel").WhoseValue.Should().Be("r290");
        captured!.Should().ContainKey("sage-status").WhoseValue.Should().Be("verpackt");
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
    }

    [Fact]
    public async Task Index_WerkbankOverride_SchlaegtGlobalenVorkommissionierWert()
    {
        // Global VorkommissionierTage = 1 (Default), Werkbank-Override = 7 -> 7 gewinnt.
        SetupCalendarDaySubtraction();
        SetupRow(MakeRow(1, "FA-100", productionDate: new DateTime(2026, 8, 10),
            workplaceName: "A1", overridePrePickingDays: 7));

        var vm = (ProductionOrderListViewModel)((ViewResult)await _controller.Index(null, null, null)).Model!;

        var item = vm.Items.Single();
        item.KommissionierTermin.Should().Be(new DateTime(2026, 8, 6));    // 10.08. - 4
        item.VorkommissionierTermin.Should().Be(new DateTime(2026, 7, 30)); // 06.08. - 7 (nicht -1)
        item.PrePickingDaysOverride.Should().Be(7);
    }

    [Fact]
    public async Task Index_OhneWerkbankOverride_NutztGlobalenWert()
    {
        SetupCalendarDaySubtraction();
        SetupRow(MakeRow(1, "FA-100", productionDate: new DateTime(2026, 8, 10),
            workplaceName: "B2", overridePrePickingDays: null));

        var vm = (ProductionOrderListViewModel)((ViewResult)await _controller.Index(null, null, null)).Model!;

        var item = vm.Items.Single();
        item.VorkommissionierTermin.Should().Be(new DateTime(2026, 8, 5));  // 06.08. - 1 (global)
        item.PrePickingDaysOverride.Should().BeNull();                      // keine UI-Rueckmeldung
    }

    [Fact]
    public async Task Index_WerkbankOverrideNull_IstExpliziterWert_NichtKeinOverride()
    {
        // 0 = Vorkommissionierung am selben Tag wie der Kommissioniertermin.
        SetupCalendarDaySubtraction();
        SetupRow(MakeRow(1, "FA-100", productionDate: new DateTime(2026, 8, 10),
            workplaceName: "C3", overridePrePickingDays: 0));

        var vm = (ProductionOrderListViewModel)((ViewResult)await _controller.Index(null, null, null)).Model!;

        var item = vm.Items.Single();
        item.VorkommissionierTermin.Should().Be(item.KommissionierTermin);
        item.PrePickingDaysOverride.Should().Be(0);
    }
}

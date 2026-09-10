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
/// Controller-Tests fuer die K1-Materialisierung (Task 8, Plan 2026-09-09): Auftragsdaten je
/// HauptFA (Kunde, Termine, Prio, AB-Nr., Montage-Abteilung) werden im hierarchischen Modus aus
/// <see cref="IFaHierarchyOrderInfoRepository"/> geladen und NUR im Anzeige-Modell (nicht in der
/// DB) an die Gruppe und — fuer die Termin-Kaskade und den Beschichtungstermin — an die Zeilen
/// angereichert. Kombigeraete (mehrere Auftragskoepfe) setzen IsAmbiguous und lassen alle
/// Einzelwerte NULL.
/// </summary>
public class ProductionOrdersControllerTests
{
    private readonly Mock<IProductionOrderRepository> _orderRepo = new();
    private readonly Mock<IProductionOrderPickingStatusRepository> _pickingStatusRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAppSettingRepository> _settingRepo = new();
    private readonly Mock<IHolidayRepository> _holidayRepo = new();
    private readonly Mock<IBusinessDayService> _businessDayService = new();
    private readonly Mock<IEnaioDmsDocumentRepository> _enaioDmsRepo = new();
    private readonly Mock<IServiceSettingRepository> _serviceSettingRepo = new();
    private readonly Mock<IFaHierarchyOrderInfoRepository> _orderInfoRepo = new();
    private readonly ProductionOrdersController _controller;

    public ProductionOrdersControllerTests()
    {
        _currentUser.Setup(x => x.GetDisplayName()).Returns("TestUser");
        _currentUser.Setup(x => x.GetWindowsUserName()).Returns("DOMAIN\\testuser");

        _settingRepo.Setup(s => s.GetIntValueAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((string _, int defaultVal) => defaultVal);
        _settingRepo.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        _holidayRepo.Setup(h => h.GetHolidayDatesAsync()).ReturnsAsync(new HashSet<DateTime>());
        _enaioDmsRepo.Setup(e => e.GetByOrderNumbersAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, List<EnaioDmsDocumentLink>>());
        _orderInfoRepo.Setup(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<FaHierarchyOrderInfo>());

        // Kalendertage statt Arbeitstage — testet die verwendete TAGE-ANZAHL, nicht BusinessDayService.
        _businessDayService.Setup(b => b.ParsePickupDays(It.IsAny<string>())).Returns(new HashSet<DayOfWeek>());
        _businessDayService.Setup(b => b.SubtractBusinessDays(
                It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<HashSet<DateTime>>()))
            .Returns((DateTime d, int days, HashSet<DateTime> _) => d.AddDays(-days));
        _businessDayService.Setup(b => b.FindPreviousPickupDay(It.IsAny<DateTime>(), It.IsAny<HashSet<DayOfWeek>>()))
            .Returns((DateTime d, HashSet<DayOfWeek> _) => d);

        EnableHierarchical();

        _controller = new ProductionOrdersController(
            _orderRepo.Object,
            _pickingStatusRepo.Object,
            _currentUser.Object,
            _settingRepo.Object,
            _holidayRepo.Object,
            _businessDayService.Object,
            _enaioDmsRepo.Object,
            _serviceSettingRepo.Object,
            _orderInfoRepo.Object);

        _controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());
    }

    private void EnableHierarchical() =>
        _serviceSettingRepo
            .Setup(s => s.GetValueAsync(
                IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturKeys.Master))
            .ReturnsAsync("true");

    // Hierarchie-Zeile: OrderNumber = HauptFA (Gruppenschluessel), SubOrderNumber = Sub-FA.
    private static LeitstandOrderRow MakeRowH(int id, string orderNumber, string subOrderNumber,
        string? parentSub = null, DateTime? productionDate = null, string? workplaceName = null,
        int? overridePrePickingDays = null) =>
        new(id, orderNumber, 1m, null, "ART-001", null, null, productionDate, null, false, false, false,
            workplaceName, null, null, null, null, null, overridePrePickingDays,
            SubOrderNumber: subOrderNumber, ParentSubOrderNumber: parentSub);

    private static FaHierarchyOrderInfo MakeHead(int hauptFa, string? montageAbteilung = "M1",
        string? kunde = "Mustermann", DateTime? koTermin = null, DateTime? feTermin = null,
        DateTime? verladetermin = null, int? prio = null, string? abNr = null,
        DateTime? startBeschichtung = null) =>
        new()
        {
            HauptFA = hauptFa,
            MontageAbteilung = montageAbteilung,
            Kunde = kunde,
            KO_Termin = koTermin,
            FE_Termin = feTermin,
            Verladetermin_Vsl = verladetermin,
            Prio = prio,
            ABNr = abNr,
            Start_Beschichtung = startBeschichtung,
        };

    private void SetupGrouped(LeitstandOrderGroupedPage page) =>
        _orderRepo.Setup(r => r.GetForLeitstandGroupedAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(page);

    private void SetupPickingStatuses(Dictionary<int, ProductionOrderPickingStatus> statuses) =>
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(statuses);

    private static ProductionOrderPickingStatus MakePs(int productionOrderId, bool hasCoatingParts) =>
        new()
        {
            ProductionOrderId = productionOrderId,
            HasCoatingParts = hasCoatingParts,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

    [Fact]
    public async Task Index_Hierarchical_FillsGroupHeadFromOrderInfo()
    {
        SetupGrouped(new LeitstandOrderGroupedPage(
            new List<LeitstandOrderGroup> { new("1000", new List<LeitstandOrderRow> { MakeRowH(1, "1000", "1001") }) },
            TotalGroupCount: 1, TotalRowCount: 1));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>());

        var head = MakeHead(1000, montageAbteilung: "Montage-1", kunde: "Mustermann GmbH",
            koTermin: new DateTime(2026, 8, 1), feTermin: new DateTime(2026, 8, 10),
            verladetermin: new DateTime(2026, 8, 12), prio: 3, abNr: "AB-4711");
        _orderInfoRepo.Setup(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<FaHierarchyOrderInfo> { head });

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        var group = vm.Groups.Should().ContainSingle().Subject;
        group.IsAmbiguous.Should().BeFalse();
        group.HeadVariants.Should().BeEmpty();
        group.Customer.Should().Be("Mustermann GmbH");
        group.KonstruktionsTermin.Should().Be(new DateTime(2026, 8, 1));
        group.ProductionDate.Should().Be(new DateTime(2026, 8, 10));
        group.DeliveryDate.Should().Be(new DateTime(2026, 8, 12));
        group.Prio.Should().Be(3);
        group.AbNummer.Should().Be("AB-4711");
        group.MontageAbteilung.Should().Be("Montage-1");
    }

    [Fact]
    public async Task Index_Hierarchical_AmbiguousHauptFa_SetsFlagAndAllVariants_AndNoSingleValues()
    {
        SetupGrouped(new LeitstandOrderGroupedPage(
            new List<LeitstandOrderGroup> { new("1000", new List<LeitstandOrderRow> { MakeRowH(1, "1000", "1001") }) },
            TotalGroupCount: 1, TotalRowCount: 1));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>());

        var head1 = MakeHead(1000, montageAbteilung: "Montage-1", kunde: "Kunde A", feTermin: new DateTime(2026, 8, 10));
        var head2 = MakeHead(1000, montageAbteilung: "Montage-2", kunde: "Kunde B", feTermin: new DateTime(2026, 8, 15));
        _orderInfoRepo.Setup(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<FaHierarchyOrderInfo> { head1, head2 });

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        var group = vm.Groups.Should().ContainSingle().Subject;
        group.IsAmbiguous.Should().BeTrue();
        group.HeadVariants.Should().HaveCount(2);
        group.HeadVariants.Select(v => v.MontageAbteilung).Should().BeEquivalentTo("Montage-1", "Montage-2");

        group.Customer.Should().BeNull();
        group.KonstruktionsTermin.Should().BeNull();
        group.ProductionDate.Should().BeNull();
        group.DeliveryDate.Should().BeNull();
        group.Prio.Should().BeNull();
        group.AbNummer.Should().BeNull();
        group.MontageAbteilung.Should().BeNull();
    }

    [Fact]
    public async Task Index_Hierarchical_UsesGroupProductionDateForCascade_NotRowValue()
    {
        // ProductionOrder.ProductionDate == null, FE_Termin gesetzt -> KommissionierTermin und
        // VorkommissionierTermin werden anhand des GRUPPENWERTS berechnet.
        SetupGrouped(new LeitstandOrderGroupedPage(
            new List<LeitstandOrderGroup>
            {
                new("1000", new List<LeitstandOrderRow> { MakeRowH(1, "1000", "1001", productionDate: null) })
            },
            TotalGroupCount: 1, TotalRowCount: 1));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>());

        var head = MakeHead(1000, feTermin: new DateTime(2026, 8, 10));
        _orderInfoRepo.Setup(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<FaHierarchyOrderInfo> { head });

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        var item = vm.Groups.Single().Items.Single();
        // Default: KommissionierTage=4, VorkommissionierTage=1 (Settings-Mock liefert Defaults).
        item.KommissionierTermin.Should().Be(new DateTime(2026, 8, 6));
        item.VorkommissionierTermin.Should().Be(new DateTime(2026, 8, 5));
        item.ProductionDate.Should().Be(new DateTime(2026, 8, 10));
    }

    [Fact]
    public async Task Index_Hierarchical_TwoWorkplacesDifferentOverride_SameProductionDate_DifferentBgTermin()
    {
        // Gleicher Fertigungstermin (Gruppenwert), aber unterschiedliche Werkbank-Overrides ->
        // unterschiedlicher Vorkommissioniertermin (BG-Termin) je Zeile.
        SetupGrouped(new LeitstandOrderGroupedPage(
            new List<LeitstandOrderGroup>
            {
                new("1000", new List<LeitstandOrderRow>
                {
                    MakeRowH(1, "1000", "1001", workplaceName: "A1", overridePrePickingDays: 1),
                    MakeRowH(2, "1000", "1002", workplaceName: "A2", overridePrePickingDays: 7),
                })
            },
            TotalGroupCount: 1, TotalRowCount: 2));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>());

        var head = MakeHead(1000, feTermin: new DateTime(2026, 8, 10));
        _orderInfoRepo.Setup(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<FaHierarchyOrderInfo> { head });

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        var items = vm.Groups.Single().Items;
        items.Should().OnlyContain(i => i.ProductionDate == new DateTime(2026, 8, 10));
        items.Should().OnlyContain(i => i.KommissionierTermin == new DateTime(2026, 8, 6));
        items.Single(i => i.SubOrderNumber == "1001").VorkommissionierTermin.Should().Be(new DateTime(2026, 8, 5));
        items.Single(i => i.SubOrderNumber == "1002").VorkommissionierTermin.Should().Be(new DateTime(2026, 7, 30));
    }

    [Fact]
    public async Task Index_Hierarchical_CoatingDateFromStartBeschichtung_OnlyWhenUnambiguousAndFlagged()
    {
        SetupGrouped(new LeitstandOrderGroupedPage(
            new List<LeitstandOrderGroup>
            {
                new("1000", new List<LeitstandOrderRow>
                {
                    MakeRowH(1, "1000", "1001"),
                    MakeRowH(2, "1000", "1002"),
                })
            },
            TotalGroupCount: 1, TotalRowCount: 2));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>
        {
            { 1, MakePs(1, hasCoatingParts: true) },
            { 2, MakePs(2, hasCoatingParts: false) },
        });

        var head = MakeHead(1000, feTermin: new DateTime(2026, 8, 10), startBeschichtung: new DateTime(2026, 7, 20));
        _orderInfoRepo.Setup(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<FaHierarchyOrderInfo> { head });

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        var items = vm.Groups.Single().Items;
        items.Single(i => i.SubOrderNumber == "1001").BeschichtungTermin.Should().Be(new DateTime(2026, 7, 20));
        items.Single(i => i.SubOrderNumber == "1002").BeschichtungTermin.Should().BeNull();
    }

    [Fact]
    public async Task Index_Hierarchical_UnambiguousHead_NoStartBeschichtung_ShowsNoCoatingDate()
    {
        // Fixrunde 1: eindeutiger Kopf, FE_Termin gesetzt (Kaskade wuerde rechnen koennen),
        // HasCoatingParts = true, aber Start_Beschichtung ist leer (in Sage noch nicht geplant).
        // Erwartung: KEIN Beschichtungstermin — bei IDEAL kommt er AUSSCHLIESSLICH aus dem Kopf,
        // die AKE-Rueckwaertsformel darf hier nicht als Ersatz einspringen (kein Raten).
        SetupGrouped(new LeitstandOrderGroupedPage(
            new List<LeitstandOrderGroup>
            {
                new("1000", new List<LeitstandOrderRow> { MakeRowH(1, "1000", "1001") })
            },
            TotalGroupCount: 1, TotalRowCount: 1));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>
        {
            { 1, MakePs(1, hasCoatingParts: true) },
        });

        var head = MakeHead(1000, feTermin: new DateTime(2026, 8, 10), startBeschichtung: null);
        _orderInfoRepo.Setup(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<FaHierarchyOrderInfo> { head });

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        vm.Groups.Single().Items.Single().BeschichtungTermin.Should().BeNull();
    }

    [Fact]
    public async Task Index_Hierarchical_HauptFaWithoutHeadRow_ShowsEmptyGroupHead_NoCrash()
    {
        // HauptFA ohne Eintrag in den Kopfdaten (z.B. Full-Refresh noch nicht gelaufen).
        // Erwartung: saubere Leeranzeige, kein Absturz, weder Einzelwerte noch Mehrdeutig-Flag.
        SetupGrouped(new LeitstandOrderGroupedPage(
            new List<LeitstandOrderGroup>
            {
                new("1000", new List<LeitstandOrderRow> { MakeRowH(1, "1000", "1001") })
            },
            TotalGroupCount: 1, TotalRowCount: 1));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>());
        _orderInfoRepo.Setup(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<FaHierarchyOrderInfo>());

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        var group = vm.Groups.Should().ContainSingle().Subject;
        group.IsAmbiguous.Should().BeFalse();
        group.HeadVariants.Should().BeEmpty();
        group.Customer.Should().BeNull();
        group.KonstruktionsTermin.Should().BeNull();
        group.ProductionDate.Should().BeNull();
        group.DeliveryDate.Should().BeNull();
        group.Prio.Should().BeNull();
        group.AbNummer.Should().BeNull();
        group.MontageAbteilung.Should().BeNull();
        group.Items.Should().ContainSingle();
        group.Items.Single().BeschichtungTermin.Should().BeNull();
    }

    [Fact]
    public async Task Index_Hierarchical_AmbiguousHauptFa_ShowsNoCoatingDateOnAnyRow()
    {
        SetupGrouped(new LeitstandOrderGroupedPage(
            new List<LeitstandOrderGroup>
            {
                new("1000", new List<LeitstandOrderRow> { MakeRowH(1, "1000", "1001") })
            },
            TotalGroupCount: 1, TotalRowCount: 1));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>
        {
            { 1, MakePs(1, hasCoatingParts: true) },
        });

        var head1 = MakeHead(1000, montageAbteilung: "M1", startBeschichtung: new DateTime(2026, 7, 20));
        var head2 = MakeHead(1000, montageAbteilung: "M2", startBeschichtung: new DateTime(2026, 7, 25));
        _orderInfoRepo.Setup(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<FaHierarchyOrderInfo> { head1, head2 });

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        vm.Groups.Single().Items.Should().OnlyContain(i => i.BeschichtungTermin == null);
    }

    [Fact]
    public async Task Index_FlatMode_MapItemUnchanged_NoGroupEnrichment()
    {
        // AKE-Regression: Flachmodus ruft MapItem ohne Gruppenwerte auf -> bit-identisches Verhalten.
        _serviceSettingRepo.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        var row = new LeitstandOrderRow(1, "FA-100", 1m, null, "ART-001", null, null,
            new DateTime(2026, 8, 10), new DateTime(2026, 8, 20), false, false, false, null);
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(new LeitstandOrderPage(new List<LeitstandOrderRow> { row }, 1));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>());

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        var item = vm.Items.Should().ContainSingle().Subject;
        item.ProductionDate.Should().Be(new DateTime(2026, 8, 10));
        item.DeliveryDate.Should().Be(new DateTime(2026, 8, 20));
        _orderInfoRepo.Verify(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()), Times.Never);
    }

    [Fact]
    public async Task Index_Hierarchical_FillsRowCustomerAndHeadFields_FromGroupHead()
    {
        // K1: eine HauptFA-Gruppe mit EINEM Kopf (nicht mehrdeutig). Die DB-Spalte
        // ProductionOrder.Customer bleibt bewusst leer (K1-Regel) — die Zeile bekommt Kunde,
        // Prio, AB-Nr., Montage-Abteilung und KO_Termin aus dem Gruppenkopf durchgereicht.
        SetupGrouped(new LeitstandOrderGroupedPage(
            new List<LeitstandOrderGroup> { new("1000", new List<LeitstandOrderRow> { MakeRowH(1, "1000", "1001") }) },
            TotalGroupCount: 1, TotalRowCount: 1));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>());

        var head = MakeHead(1000, montageAbteilung: "Montage 1", kunde: "Mustermann GmbH",
            koTermin: new DateTime(2026, 9, 1), feTermin: new DateTime(2026, 9, 10),
            verladetermin: new DateTime(2026, 9, 12), prio: 3, abNr: "AB-42");
        _orderInfoRepo.Setup(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<FaHierarchyOrderInfo> { head });

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        var row = vm.Groups.Single().Items.Single();
        row.Customer.Should().Be("Mustermann GmbH");
        row.Prio.Should().Be(3);
        row.AbNummer.Should().Be("AB-42");
        row.MontageAbteilung.Should().Be("Montage 1");
        row.KonstruktionsTermin.Should().Be(new DateTime(2026, 9, 1));
    }

    [Fact]
    public async Task Index_Flat_LeavesNewRowFieldsNull_BitIdentical()
    {
        // AKE-Regression (wichtiger als der Positivtest oben): Master false -> flacher Zweig,
        // MapItem ohne Gruppenargumente. Die neuen Zeilenfelder bleiben NULL, Customer kommt
        // unveraendert aus ProductionOrder.Customer.
        _serviceSettingRepo.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        var row = new LeitstandOrderRow(1, "FA-100", 1m, "AUS DER DB", "ART-001", null, null,
            new DateTime(2026, 8, 10), new DateTime(2026, 8, 20), false, false, false, null);
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(new LeitstandOrderPage(new List<LeitstandOrderRow> { row }, 1));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>());

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        var item = vm.Items.Should().ContainSingle().Subject;
        item.Customer.Should().Be("AUS DER DB");
        item.Prio.Should().BeNull();
        item.AbNummer.Should().BeNull();
        item.MontageAbteilung.Should().BeNull();
        item.KonstruktionsTermin.Should().BeNull();
    }
}

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

    /// <summary>
    /// Die FA-Liste kennzeichnet die HauptFA-Zeile in <c>_ProductionOrderRow.cshtml</c> ueber
    /// <c>SubOrderNumber == OrderNumber</c> — ohne eigenes ViewModel-Feld. Dieser Test haelt die
    /// Voraussetzung fest: Die Wurzel kommt als normale Zeile ihrer eigenen Gruppe im Modell an.
    /// </summary>
    [Fact]
    public async Task Index_Hierarchical_RootRow_KeepsSubOrderNumberEqualToOrderNumber()
    {
        SetupGrouped(new LeitstandOrderGroupedPage(
            new List<LeitstandOrderGroup>
            {
                new("1000", new List<LeitstandOrderRow>
                {
                    MakeRowH(1, "1000", "1000"),
                    MakeRowH(2, "1000", "1001", parentSub: "1000")
                })
            },
            TotalGroupCount: 1, TotalRowCount: 2));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>());
        _orderInfoRepo.Setup(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<FaHierarchyOrderInfo> { MakeHead(1000) });

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        var items = vm.Groups.Should().ContainSingle().Subject.Items;
        items.Should().HaveCount(2);
        items.Should().ContainSingle(i => i.SubOrderNumber == i.OrderNumber)
            .Which.SubOrderNumber.Should().Be("1000");
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
    public async Task Index_Hierarchical_AmbiguousHauptFa_RowCustomerFromDb_IsNulledOut_NotLeaked()
    {
        // Fix-Runde 1 (Important-Befund): Kombigeraet (heads.Count > 1) UND die DB-Zeile traegt
        // ausnahmsweise einen nicht-leeren Customer (Altdaten/anderer Schreibpfad — die K1-Regel
        // "DB-Spalte bleibt leer" ist eine aeussere Annahme, keine Code-Garantie). Erwartung:
        // item.Customer muss trotzdem NULL sein, strukturell wie Prio/AbNummer/MontageAbteilung/
        // KonstruktionsTermin — kein Fallback auf den DB-Wert der Zeile.
        var row = new LeitstandOrderRow(1, "1000", 1m, "Kunde aus DB-Zeile", "ART-001", null, null,
            null, null, false, false, false, null,
            SubOrderNumber: "1001", ParentSubOrderNumber: null);
        SetupGrouped(new LeitstandOrderGroupedPage(
            new List<LeitstandOrderGroup> { new("1000", new List<LeitstandOrderRow> { row }) },
            TotalGroupCount: 1, TotalRowCount: 1));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>());

        var head1 = MakeHead(1000, montageAbteilung: "Montage-1", kunde: "Kunde A");
        var head2 = MakeHead(1000, montageAbteilung: "Montage-2", kunde: "Kunde B");
        _orderInfoRepo.Setup(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<FaHierarchyOrderInfo> { head1, head2 });

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        vm.Groups.Single().Items.Single().Customer.Should().BeNull();
    }

    [Fact]
    public async Task Index_Hierarchical_ColumnFilter_Prio_FiltersOnComputedRowValue()
    {
        // K1-Postfilter (Task 6): ProductionOrder.Prio existiert NICHT in der DB -- Prio ist ein
        // berechneter Gruppenkopf-Wert (Task 5). Der Spaltenfilter "prio" muss deshalb NACH der
        // Zeilen-Anreicherung in C# greifen, nicht als SQL-Praedikat. Zwei Gruppen, Prio 3 und 9;
        // Filter "3" laesst nur die erste Gruppe -- die zweite faellt komplett weg (Z3b).
        SetupGrouped(new LeitstandOrderGroupedPage(
            new List<LeitstandOrderGroup>
            {
                new("1000", new List<LeitstandOrderRow> { MakeRowH(1, "1000", "1001") }),
                new("2000", new List<LeitstandOrderRow> { MakeRowH(2, "2000", "2001") }),
            },
            TotalGroupCount: 2, TotalRowCount: 2));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>());

        var head1 = MakeHead(1000, prio: 3);
        var head2 = MakeHead(2000, prio: 9);
        _orderInfoRepo.Setup(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<FaHierarchyOrderInfo> { head1, head2 });

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_prio=3");
        _controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        vm.Groups.Should().HaveCount(1);
        vm.Groups.Single().Items.Should().OnlyContain(i => i.Prio == 3);
        // Important-Befund (Fix-Runde 1): Z3b heisst nicht nur "leere Gruppen verschwinden",
        // sondern auch "die Zaehlungen stimmen danach" -- die weggefallene Gruppe (Prio 9) darf in
        // keiner Zaehlung mehr auftauchen.
        vm.HierarchicalRowCount.Should().Be(1);
        vm.Pagination.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Index_Flat_ColumnFilter_Customer_StaysSqlPredicate_PaginationUnaffected()
    {
        // Fix-Runde 1, Critical-Befund: "customer" ist am Standort AKE (Flachmodus) eine echte,
        // befuellte DB-Spalte -- ApplyLeitstandColumnFilter filtert sie per SQL. Die K1-Postfilter-
        // Schluesselmenge (FaListComputedColumnKeys) darf den Flachzweig NICHT beruehren: weder
        // darf "customer" aus sqlColumnFilters herausfallen, noch darf sqlPageSize/sqlPage auf
        // "ungepaginiert" gezwungen werden (das waere nur fuer Datumsfilter korrekt).
        _serviceSettingRepo.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        var row = new LeitstandOrderRow(1, "FA-100", 1m, "Mustermann GmbH", "ART-001", null, null,
            null, null, false, false, false, null);
        // TotalCount (50) bewusst groesser als die zurueckgegebene Seite (1 Zeile) -- simuliert eine
        // SQL-seitig bereits gefilterte, aber noch nicht vollstaendig geladene Ergebnismenge. Wird
        // dieser Wert spaeter durch Items.Count (1) ueberschrieben, ist das der Bug-Pfad (alles
        // ungepaginiert geladen und in C# neu gezaehlt statt dem Repo zu vertrauen).
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(new LeitstandOrderPage(new List<LeitstandOrderRow> { row }, 50));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>());

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_customer=muster");
        _controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        // (a) "customer" bleibt ein SQL-Praedikat -- unveraendert im an das Repo uebergebenen Dict.
        _orderRepo.Verify(r => r.GetForLeitstandAsync(
            null, null, null, false,
            1,                                  // sqlPage: NICHT auf 1-erzwungen-wegen-Postfilter,
                                                  // sondern der tatsaechlich angeforderte page=1
            25,                                  // sqlPageSize: effectivePageSize (Default), NICHT int.MaxValue
            It.Is<IReadOnlyDictionary<string, string>?>(d => d != null && d.ContainsKey("customer"))),
            Times.Once);

        // (b) Paginierung greift weiterhin -- TotalCount kommt vom Repo, wird NICHT auf
        // Items.Count kollabiert (das waere das Datumsfilter-Postprocessing, hier fehl am Platz).
        vm.Items.Should().HaveCount(1);
        vm.Pagination.TotalCount.Should().Be(50);
    }

    [Fact]
    public async Task Index_Hierarchical_ColumnFilter_OnFieldEmptyForKombigeraet_YieldsNoMatches_NoThrow()
    {
        // F7-Analogon: bei Kombigeraeten (mehrere Auftragskoepfe, IsAmbiguous) bleiben die
        // Kopf-Felder je Zeile NULL. Ein Filter auf so einer Spalte darf nicht werfen und muss
        // korrekt KEINE Treffer liefern (leerer Wert matcht nie), statt die Gruppe stillschweigend
        // durchzulassen.
        SetupGrouped(new LeitstandOrderGroupedPage(
            new List<LeitstandOrderGroup>
            {
                new("1000", new List<LeitstandOrderRow> { MakeRowH(1, "1000", "1001") }),
            },
            TotalGroupCount: 1, TotalRowCount: 1));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>());

        var head1 = MakeHead(1000, kunde: "Kunde A");
        var head2 = MakeHead(1000, kunde: "Kunde B");
        _orderInfoRepo.Setup(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<FaHierarchyOrderInfo> { head1, head2 });

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_customer=kunde");
        _controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var act = async () => await _controller.Index(null, null, null, false, 1, null);
        var result = await act.Should().NotThrowAsync();
        var vm = ((ViewResult)result.Subject).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        vm.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task Index_Hierarchical_ColumnFilter_KonstruktionsTermin_FiltersOnComputedDate()
    {
        // konstruktions-termin ist Datum UND berechneter Wert -- behandelt ueber den bestehenden
        // Datums-Mechanismus (FaListDateColumnKeys / MatchFaListDateFilter), nicht doppelt.
        SetupGrouped(new LeitstandOrderGroupedPage(
            new List<LeitstandOrderGroup>
            {
                new("1000", new List<LeitstandOrderRow> { MakeRowH(1, "1000", "1001") }),
                new("2000", new List<LeitstandOrderRow> { MakeRowH(2, "2000", "2001") }),
            },
            TotalGroupCount: 2, TotalRowCount: 2));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>());

        var head1 = MakeHead(1000, koTermin: new DateTime(2026, 8, 1));
        var head2 = MakeHead(2000, koTermin: new DateTime(2026, 9, 1));
        _orderInfoRepo.Setup(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<FaHierarchyOrderInfo> { head1, head2 });

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_konstruktions-termin=01.08.2026");
        _controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await _controller.Index(null, null, null, false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        vm.Groups.Should().HaveCount(1);
        vm.Groups.Single().Items.Should().OnlyContain(i => i.KonstruktionsTermin == new DateTime(2026, 8, 1));
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

    [Fact]
    public async Task Index_Hierarchical_CustomerFreetextFilter_FindsGroupViaComputedPostfilter_EmptyGroupsDisappear_CountsCorrect()
    {
        // Task 7, AK8: der Kunde-FREITEXT (Filterkarte, filterCustomer) muss im hierarchischen
        // Modus genauso wie der Spaltenfilter (Task 6) ueber den C#-Postfilter laufen --
        // ProductionOrder.Customer ist je Sub-FA leer, der Kunde haengt am HauptFA. Zwei Gruppen,
        // nur eine passt auf den Freitext "mustermann"; die zweite muss komplett verschwinden (Z3b),
        // die Zaehlungen (HierarchicalRowCount/Pagination.TotalCount) muessen das widerspiegeln.
        SetupGrouped(new LeitstandOrderGroupedPage(
            new List<LeitstandOrderGroup>
            {
                new("1000", new List<LeitstandOrderRow> { MakeRowH(1, "1000", "1001") }),
                new("2000", new List<LeitstandOrderRow> { MakeRowH(2, "2000", "2001") }),
            },
            TotalGroupCount: 2, TotalRowCount: 2));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>());

        var head1 = MakeHead(1000, kunde: "Mustermann GmbH");
        var head2 = MakeHead(2000, kunde: "Andere Firma");
        _orderInfoRepo.Setup(r => r.GetByHauptFaKeysAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<FaHierarchyOrderInfo> { head1, head2 });

        var result = await _controller.Index(null, null, "Mustermann", false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        vm.Groups.Should().HaveCount(1);
        vm.Groups.Single().Customer.Should().Be("Mustermann GmbH");
        vm.Groups.Single().Items.Should().OnlyContain(i => i.Customer == "Mustermann GmbH");
        vm.HierarchicalRowCount.Should().Be(1);
        vm.Pagination.TotalCount.Should().Be(1);

        // Kein SQL-Subquery mehr fuer die FA-Liste -- filterCustomer geht NICHT mehr an das Repo
        // (im Gegensatz zum Leitstand, siehe PickingLeitstandControllerTests).
        _orderRepo.Verify(r => r.GetForLeitstandGroupedAsync(
            null, null, null, false,
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()),
            Times.Once);
    }

    [Fact]
    public async Task Index_Flat_CustomerFreetextFilter_StaysSqlPredicate_PaginationUnaffected()
    {
        // Gegenstueck zum Test oben: im Flachmodus (AKE) ist Customer eine echte, befuellte
        // DB-Spalte -- filterCustomer bleibt unveraendert ein SQL-Praedikat an GetForLeitstandAsync,
        // inklusive greifender Paginierung (Harte Bedingung des Tasks, Punkt 2).
        _serviceSettingRepo.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        var row = new LeitstandOrderRow(1, "FA-100", 1m, "Mustermann GmbH", "ART-001", null, null,
            null, null, false, false, false, null);
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(new LeitstandOrderPage(new List<LeitstandOrderRow> { row }, 50));
        SetupPickingStatuses(new Dictionary<int, ProductionOrderPickingStatus>());

        var result = await _controller.Index(null, null, "muster", false, 1, null);
        var vm = ((ViewResult)result).Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;

        _orderRepo.Verify(r => r.GetForLeitstandAsync(
            null, null, "muster", false,
            1,   // sqlPage: nicht auf 1-erzwungen, sondern der tatsaechlich angeforderte page=1
            25,  // sqlPageSize: effectivePageSize (Default), NICHT int.MaxValue
            It.IsAny<IReadOnlyDictionary<string, string>?>()),
            Times.Once);

        vm.Items.Should().HaveCount(1);
        vm.Pagination.TotalCount.Should().Be(50);
    }
}

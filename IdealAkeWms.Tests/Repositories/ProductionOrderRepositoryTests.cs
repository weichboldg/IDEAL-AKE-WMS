using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// Tests fuer das ProductionOrderRepository (Sage-Master-Daten).
/// Released-/Picker-/Priority-Logik liegt seit Phase 1 (v1.11.0) im
/// <see cref="ProductionOrderPickingStatusRepository"/> — siehe dort die migrierten Tests.
/// </summary>
public class ProductionOrderRepositoryTests
{
    [Fact]
    public async Task GetAllOrderedAsync_OrdersByOrderNumber()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "WA-3");
        TestDataHelper.CreateOrderWithStatuses(context, "WA-1");
        TestDataHelper.CreateOrderWithStatuses(context, "WA-2");

        var repo = new ProductionOrderRepository(context);
        var result = await repo.GetAllOrderedAsync();

        result.Should().HaveCount(3);
        result.Select(o => o.OrderNumber).Should().ContainInOrder("WA-1", "WA-2", "WA-3");
    }

    [Fact]
    public async Task GetOpenOrdersAsync_ExcludesDoneOrders()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "WA-OPEN", isDone: false);
        TestDataHelper.CreateOrderWithStatuses(context, "WA-DONE", isDone: true);

        var repo = new ProductionOrderRepository(context);
        var result = await repo.GetOpenOrdersAsync();

        result.Should().ContainSingle().Which.OrderNumber.Should().Be("WA-OPEN");
    }

    [Fact]
    public async Task GetByOrderNumberAsync_ReturnsCorrectOrder()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "WA-100");
        TestDataHelper.CreateOrderWithStatuses(context, "WA-200");

        var repo = new ProductionOrderRepository(context);
        var result = await repo.GetByOrderNumberAsync("WA-200");

        result.Should().NotBeNull();
        result!.OrderNumber.Should().Be("WA-200");
    }

    [Fact]
    public async Task GetForLeitstand_ExcludesKommDoneOrders_WhenShowDoneFalse()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);

        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-OPEN", IsDone = false });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-KOMMDONE", IsDone = false });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 3, OrderNumber = "FA-SAGEDONE", IsDone = true });
        ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus { ProductionOrderId = 1, IsDonePicking = false });
        ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus { ProductionOrderId = 2, IsDonePicking = true });
        await ctx.SaveChangesAsync();

        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: false, page: 1, pageSize: 100);

        page.Rows.Should().ContainSingle(r => r.OrderNumber == "FA-OPEN");
        page.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetForLeitstand_IncludesKommDoneWithFlag_WhenShowDoneTrue()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);

        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-OPEN", IsDone = false });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-KOMMDONE", IsDone = false });
        ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus { ProductionOrderId = 2, IsDonePicking = true });
        await ctx.SaveChangesAsync();

        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: true, page: 1, pageSize: 100);

        page.Rows.Should().HaveCount(2);
        page.Rows.Single(r => r.OrderNumber == "FA-KOMMDONE").IsDonePicking.Should().BeTrue();
        page.Rows.Single(r => r.OrderNumber == "FA-OPEN").IsDonePicking.Should().BeFalse();
    }

    [Fact]
    public async Task GetOpenOrdersInWindow_ExcludesKommDoneOrders()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);

        var inWindow = DateTime.Now.AddDays(7);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-OPEN", IsDone = false, ProductionDate = inWindow });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-KOMMDONE", IsDone = false, ProductionDate = inWindow });
        ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus { ProductionOrderId = 1, IsDonePicking = false });
        ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus { ProductionOrderId = 2, IsDonePicking = true });
        await ctx.SaveChangesAsync();

        var result = await repo.GetOpenOrdersInWindowAsync(weeksAhead: 8, maxCount: 200);

        result.Should().ContainSingle().Which.OrderNumber.Should().Be("FA-OPEN");
    }

    [Fact]
    public async Task GetOpenOrdersAsync_ExcludesCancelledOrders()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionOrders.Add(new ProductionOrder { OrderNumber = "FA-OPEN", IsDone = false, IsCancelled = false });
        ctx.ProductionOrders.Add(new ProductionOrder { OrderNumber = "FA-CANCELLED", IsDone = false, IsCancelled = true });
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderRepository(ctx);
        var result = await repo.GetOpenOrdersAsync();

        result.Should().ContainSingle().Which.OrderNumber.Should().Be("FA-OPEN");
    }

    [Fact]
    public async Task GetForLeitstand_ExcludesCancelled_WhenShowDoneFalse()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);

        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-OPEN", IsDone = false, IsCancelled = false });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-CANCELLED", IsDone = false, IsCancelled = true });
        await ctx.SaveChangesAsync();

        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: false, page: 1, pageSize: 100);

        page.Rows.Should().ContainSingle(r => r.OrderNumber == "FA-OPEN");
        page.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetForLeitstand_IncludesCancelledWithFlag_WhenShowDoneTrue()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);

        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-OPEN", IsDone = false, IsCancelled = false });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-CANCELLED", IsDone = false, IsCancelled = true });
        await ctx.SaveChangesAsync();

        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: true, page: 1, pageSize: 100);

        page.Rows.Should().HaveCount(2);
        page.Rows.Single(r => r.OrderNumber == "FA-CANCELLED").IsCancelled.Should().BeTrue();
        page.Rows.Single(r => r.OrderNumber == "FA-OPEN").IsCancelled.Should().BeFalse();
    }

    [Fact]
    public async Task GetOpenOrdersInWindow_ExcludesCancelled()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);

        var inWindow = DateTime.Now.AddDays(7);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-OPEN", IsDone = false, IsCancelled = false, ProductionDate = inWindow });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-CANCELLED", IsDone = false, IsCancelled = true, ProductionDate = inWindow });
        await ctx.SaveChangesAsync();

        var result = await repo.GetOpenOrdersInWindowAsync(weeksAhead: 8, maxCount: 200);

        result.Should().ContainSingle().Which.OrderNumber.Should().Be("FA-OPEN");
    }

    [Fact]
    public async Task SearchAsync_ExcludesCancelledOrders()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionOrders.Add(new ProductionOrder { OrderNumber = "FA-OPEN", IsDone = false, IsCancelled = false });
        ctx.ProductionOrders.Add(new ProductionOrder { OrderNumber = "FA-CANCELLED", IsDone = false, IsCancelled = true });
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderRepository(ctx);
        var result = await repo.SearchAsync("FA-");

        result.Should().ContainSingle().Which.OrderNumber.Should().Be("FA-OPEN");
    }

    // ------------------------------------------------------------------
    // FA-Zusatzinfos (Sage, v1.26.0)
    // ------------------------------------------------------------------

    private static ProductionOrderExtraInfo MakeExtraInfo(int productionOrderId,
        string? kaelte = "R290", string? ventil = "Danfoss", string? ausfuehrung = "E",
        string? maschine = "M1", string? status = "in Produktion") =>
        new()
        {
            ProductionOrderId = productionOrderId,
            Kaeltemittel = kaelte,
            Ventil = ventil,
            AusfuehrungEZ = ausfuehrung,
            Maschine = maschine,
            SageStatus = status,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };

    [Fact]
    public async Task GetExtraInfoAsync_ReturnsSatellite_OrNull()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-1" });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-2" });
        ctx.ProductionOrderExtraInfos.Add(MakeExtraInfo(1));
        await ctx.SaveChangesAsync();

        var info = await repo.GetExtraInfoAsync(1);
        info.Should().NotBeNull();
        info!.Kaeltemittel.Should().Be("R290");

        (await repo.GetExtraInfoAsync(2)).Should().BeNull();
    }

    [Fact]
    public async Task GetAllOrderedAsync_IncludesExtraInfo()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-1" });
        ctx.ProductionOrderExtraInfos.Add(MakeExtraInfo(1));
        await ctx.SaveChangesAsync();

        var result = await repo.GetAllOrderedAsync();

        result.Should().ContainSingle().Which.ExtraInfo.Should().NotBeNull();
        result[0].ExtraInfo!.Kaeltemittel.Should().Be("R290");
    }

    [Fact]
    public async Task GetForLeitstand_ProjectsExtraInfoFields()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-1" });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-OHNE" });
        ctx.ProductionOrderExtraInfos.Add(MakeExtraInfo(1));
        await ctx.SaveChangesAsync();

        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: false, page: 1, pageSize: 100);

        var row = page.Rows.Single(r => r.OrderNumber == "FA-1");
        row.Kaeltemittel.Should().Be("R290");
        row.Ventil.Should().Be("Danfoss");
        row.AusfuehrungEZ.Should().Be("E");
        row.Maschine.Should().Be("M1");
        row.SageStatus.Should().Be("in Produktion");

        var empty = page.Rows.Single(r => r.OrderNumber == "FA-OHNE");
        empty.Kaeltemittel.Should().BeNull();
        empty.SageStatus.Should().BeNull();
    }

    [Fact]
    public async Task GetForLeitstand_FiltersOnKaeltemittel_CaseInsensitive_WithNullGuard()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-R290" });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-R134" });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 3, OrderNumber = "FA-NULL" });
        ctx.ProductionOrderExtraInfos.Add(MakeExtraInfo(1, kaelte: "R290"));
        ctx.ProductionOrderExtraInfos.Add(MakeExtraInfo(2, kaelte: "R134a"));
        await ctx.SaveChangesAsync();

        // Positiv: Token ist lowercase (ColumnFilterHelper.Parse lowercased) — Wert "R290"
        // matcht via ToLower().Contains. FA ohne ExtraInfo faellt beim Positiv-Filter raus.
        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: false, page: 1, pageSize: 100,
            columnFilters: new Dictionary<string, string> { ["kaeltemittel"] = "r290" });
        page.Rows.Should().ContainSingle(r => r.OrderNumber == "FA-R290");
        page.TotalCount.Should().Be(1);

        // Negation: !r290 zeigt R134a UND die Null-Zeile (leere Zelle matcht NOT).
        var negPage = await repo.GetForLeitstandAsync(null, null, null, showDone: false, page: 1, pageSize: 100,
            columnFilters: new Dictionary<string, string> { ["kaeltemittel"] = "!r290" });
        negPage.Rows.Select(r => r.OrderNumber).Should().BeEquivalentTo(new[] { "FA-R134", "FA-NULL" });
    }

    [Fact]
    public async Task GetBySubOrderNumbersAsync_ReturnsOnlyMatchingSubFas()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-1", SubOrderNumber = "1" });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-1", SubOrderNumber = "2" });
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderRepository(ctx);
        var result = await repo.GetBySubOrderNumbersAsync(new List<string> { "2" });

        result.Should().ContainSingle().Which.SubOrderNumber.Should().Be("2");
    }

    [Fact]
    public async Task GetBySubOrderNumbersAsync_ReturnsEmpty_ForEmptyInput()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-1", SubOrderNumber = "1" });
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderRepository(ctx);
        var result = await repo.GetBySubOrderNumbersAsync(new List<string>());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetForLeitstand_FiltersOnSageStatus_OrTokens()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-1" });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-2" });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 3, OrderNumber = "FA-3" });
        ctx.ProductionOrderExtraInfos.Add(MakeExtraInfo(1, status: "verpackt"));
        ctx.ProductionOrderExtraInfos.Add(MakeExtraInfo(2, status: "abgeholt"));
        ctx.ProductionOrderExtraInfos.Add(MakeExtraInfo(3, status: "begonnen"));
        await ctx.SaveChangesAsync();

        // Komma-OR: verpackt ODER abgeholt
        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: false, page: 1, pageSize: 100,
            columnFilters: new Dictionary<string, string> { ["sage-status"] = "verpackt,abgeholt" });

        page.Rows.Select(r => r.OrderNumber).Should().BeEquivalentTo(new[] { "FA-1", "FA-2" });
    }

    // ------------------------------------------------------------------
    // Matchcode-Spaltenfilter (Task 4, F7): ProductionOrders/PickingLeitstand filtern
    // SQL-seitig ueber ApplyLeitstandColumnFilter. Am Standort AKE bleibt die Spalte bis
    // zur View-Erweiterung durchgehend NULL — der zweite Test ist der F7-Nachweis dafuer.
    // ------------------------------------------------------------------

    [Fact]
    public async Task ColumnFilter_Matchcode_FindsRow_AndIgnoresNullRows()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionOrders.AddRange(
            new ProductionOrder { OrderNumber = "1", ArticleNumber = "A", Matchcode = "KESSEL-7" },
            new ProductionOrder { OrderNumber = "2", ArticleNumber = "B", Matchcode = null });
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderRepository(ctx);
        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: true, page: 1,
            pageSize: 50, columnFilters: new Dictionary<string, string> { ["matchcode"] = "kessel" });

        page.Rows.Should().HaveCount(1);
        page.Rows[0].OrderNumber.Should().Be("1");
    }

    [Fact]
    public async Task ColumnFilter_Matchcode_AllNull_ReturnsNoHits_WithoutThrowing()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionOrders.AddRange(
            new ProductionOrder { OrderNumber = "1", Matchcode = null },
            new ProductionOrder { OrderNumber = "2", Matchcode = null });
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderRepository(ctx);
        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: true, page: 1,
            pageSize: 50, columnFilters: new Dictionary<string, string> { ["matchcode"] = "x" });

        page.Rows.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
    }

    // ------------------------------------------------------------------
    // Kunde-Filter im hierarchischen Modus (K1, IDEAL): ProductionOrder.Customer
    // ist je Sub-FA leer, der Kunde haengt am HauptFA in FaHierarchyOrderInfo.Kunde.
    // ------------------------------------------------------------------

    [Fact]
    public async Task Leitstand_Hierarchical_CustomerFreetextFilter_MatchesViaOrderInfo()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionOrders.Add(new ProductionOrder
        {
            Id = 1, OrderNumber = "1000", SubOrderNumber = "1001", Customer = null,
            CreatedAt = DateTime.UtcNow, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.FaHierarchyOrderInfos.Add(new FaHierarchyOrderInfo { HauptFA = 1000, Kunde = "Mustermann GmbH" });
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderRepository(ctx);
        var page = await repo.GetForLeitstandGroupedAsync(null, null, "muster", showDone: true, page: 1, pageSize: 25);

        page.TotalRowCount.Should().Be(1);
        page.Groups.Should().ContainSingle().Which.Rows.Single().SubOrderNumber.Should().Be("1001");
    }

    [Fact]
    public async Task Leitstand_Hierarchical_CustomerColumnFilter_MatchesViaOrderInfo()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionOrders.Add(new ProductionOrder
        {
            Id = 1, OrderNumber = "1000", SubOrderNumber = "1001", Customer = null,
            CreatedAt = DateTime.UtcNow, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.ProductionOrders.Add(new ProductionOrder
        {
            Id = 2, OrderNumber = "2000", SubOrderNumber = "2001", Customer = null,
            CreatedAt = DateTime.UtcNow, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.FaHierarchyOrderInfos.Add(new FaHierarchyOrderInfo { HauptFA = 1000, Kunde = "Mustermann GmbH" });
        ctx.FaHierarchyOrderInfos.Add(new FaHierarchyOrderInfo { HauptFA = 2000, Kunde = "Andere Firma" });
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderRepository(ctx);
        var page = await repo.GetForLeitstandGroupedAsync(null, null, null, showDone: true, page: 1, pageSize: 25,
            columnFilters: new Dictionary<string, string> { ["customer"] = "muster" });

        page.TotalRowCount.Should().Be(1);
        page.Groups.Should().ContainSingle().Which.Rows.Single().SubOrderNumber.Should().Be("1001");

        // Negation: eine Zeile faellt nur raus, wenn WEDER die Zeile NOCH der Kopf passt.
        var negPage = await repo.GetForLeitstandGroupedAsync(null, null, null, showDone: true, page: 1, pageSize: 25,
            columnFilters: new Dictionary<string, string> { ["customer"] = "!muster" });

        negPage.TotalRowCount.Should().Be(1);
        negPage.Groups.Should().ContainSingle().Which.Rows.Single().SubOrderNumber.Should().Be("2001");
    }

    [Fact]
    public async Task Leitstand_Flat_CustomerFilter_UnchangedAgainstProductionOrderCustomer()
    {
        using var ctx = TestDbContextFactory.Create();
        // Master false (AKE, flach): Customer haengt an der Zeile selbst. Ein OrderInfo-Kopf
        // mit demselben "HauptFA" darf NICHT mitgetroffen werden, wenn ProductionOrder.Customer leer ist.
        ctx.ProductionOrders.Add(new ProductionOrder
        {
            Id = 1, OrderNumber = "1000", SubOrderNumber = "1000", Customer = null,
            CreatedAt = DateTime.UtcNow, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.FaHierarchyOrderInfos.Add(new FaHierarchyOrderInfo { HauptFA = 1000, Kunde = "Mustermann GmbH" });
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderRepository(ctx);

        var freetextPage = await repo.GetForLeitstandAsync(null, null, "muster", showDone: true, page: 1, pageSize: 25);
        freetextPage.TotalCount.Should().Be(0);

        // Der Spaltenfilter "customer" bleibt fuer !hierarchical bewusst der unveraenderte
        // Alt-Ausdruck (Fixrunde 1, Punkt a: F4, kein neues Risiko auf dem AKE-Pfad). Dieser
        // Alt-Ausdruck (patterns.Any(...) ueber eine lokale Liste) war schon VOR Task 7 nicht
        // InMemory-uebersetzbar — ein vorbestehender, hier nicht angefasster Zustand (siehe
        // Fallstrick "Tests, EF, SQL Server": nicht alles ist InMemory-testbar). Die Regression
        // gegen den Auftragskopf wird deshalb auf SQL-Text-Ebene abgesichert, siehe
        // CustomerColumnFilter_Flat_SqlServer_NeverReferencesOrderInfoHead unten.
    }

    // Fixrunde 1, Punkt (b): mit nur einem Token ruft Aggregate() die Faltfunktion nie auf —
    // OrElseProductionOrderPredicate und ParameterRebinder (der riskanteste neue Code) waeren
    // sonst ungetestet. Zwei Tokens, je einer matcht nur ueber den Kopf der jeweils ANDEREN Zeile.
    [Fact]
    public async Task Leitstand_Hierarchical_CustomerColumnFilter_TwoTokens_OrAcrossRows()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionOrders.Add(new ProductionOrder
        {
            Id = 1, OrderNumber = "1000", SubOrderNumber = "1001", Customer = null,
            CreatedAt = DateTime.UtcNow, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.ProductionOrders.Add(new ProductionOrder
        {
            Id = 2, OrderNumber = "2000", SubOrderNumber = "2001", Customer = null,
            CreatedAt = DateTime.UtcNow, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.FaHierarchyOrderInfos.Add(new FaHierarchyOrderInfo { HauptFA = 1000, Kunde = "Mustermann GmbH" });
        ctx.FaHierarchyOrderInfos.Add(new FaHierarchyOrderInfo { HauptFA = 2000, Kunde = "Andere Firma" });
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderRepository(ctx);

        // Positiv: "muster" ODER "andere" -> beide Zeilen treffen (jede ueber ihren eigenen Kopf).
        var page = await repo.GetForLeitstandGroupedAsync(null, null, null, showDone: true, page: 1, pageSize: 25,
            columnFilters: new Dictionary<string, string> { ["customer"] = "muster,andere" });

        page.TotalRowCount.Should().Be(2);
        page.Groups.SelectMany(g => g.Rows).Select(r => r.SubOrderNumber)
            .Should().BeEquivalentTo(new[] { "1001", "2001" });

        // Negiert: eine Zeile bleibt nur sichtbar, wenn WEDER "muster" NOCH "andere" passt —
        // hier matcht jede Zeile mindestens einen Token, also bleibt keine uebrig.
        var negPage = await repo.GetForLeitstandGroupedAsync(null, null, null, showDone: true, page: 1, pageSize: 25,
            columnFilters: new Dictionary<string, string> { ["customer"] = "!muster,andere" });

        negPage.TotalRowCount.Should().Be(0);
    }

    // Fixrunde 1, Punkt (c): InMemory ist kein Zeuge fuer den produktiven SQL-Server-Provider.
    // Baut denselben Ausdrucksbaum gegen einen SqlServer-Context (nie verbunden — ToQueryString()
    // kompiliert nur, oeffnet keine Verbindung) und prueft am erzeugten SQL-Text, dass (1) die
    // korrelierte Subquery inkl. HauptFA-Konvertierung uebersetzt wird, und (2) im NOT-Zweig die
    // IS NOT NULL-Pruefung von ProductionOrder.Customer erhalten bleibt (nicht wegoptimiert).
    [Fact]
    public void CustomerColumnFilter_Hierarchical_SqlServer_TranslatesCorrelatedSubqueryAndConvert()
    {
        using var ctx = CreateSqlServerContext();
        var repo = new ProductionOrderRepository(ctx);

        var query = repo.BuildLeitstandQuery(null, null, null, showDone: true,
            new Dictionary<string, string> { ["customer"] = "muster" }, hierarchical: true);

        var sql = query.ToQueryString();

        sql.Should().Contain("EXISTS", "die Kopf-Subquery muss als korrelierte Subquery uebersetzt werden");
        sql.Should().Contain("FaHierarchyOrderInfos", "die Subquery muss gegen die Kopftabelle laufen");
        sql.Should().Contain("CONVERT", "HauptFA.ToString() muss serverseitig konvertiert werden (kein Client-Eval)");
    }

    [Fact]
    public void CustomerColumnFilter_Hierarchical_Negated_SqlServer_PreservesNullGuardUnderNot()
    {
        using var ctx = CreateSqlServerContext();
        var repo = new ProductionOrderRepository(ctx);

        var query = repo.BuildLeitstandQuery(null, null, null, showDone: true,
            new Dictionary<string, string> { ["customer"] = "!muster" }, hierarchical: true);

        var sql = query.ToQueryString();

        sql.Should().Contain("NOT", "der Negativfall muss als NOT ueber die OR-Verknuepfung uebersetzt werden");
        sql.Should().Contain("IS NOT NULL",
            "die Null-Pruefung auf ProductionOrder.Customer darf unter NOT nicht wegoptimiert werden");
    }

    // Fixrunde 1, Punkt (a): !hierarchical laeuft weiterhin durch den unveraenderten Alt-Ausdruck.
    // Dieser ist unter InMemory nicht ausfuehrbar (vorbestehende Einschraenkung, s.o.), aber die
    // SQL-Uebersetzung laesst sich pruefen — und genau das ist der Beleg fuer F4 (bit-identisch):
    // keine Referenz auf FaHierarchyOrderInfos. (Der SqlServer-Provider uebersetzt den Alt-Ausdruck
    // patterns.Any(...) selbst schon als EXISTS gegen eine VALUES-Tabelle der Token-Liste — ein
    // EXISTS im SQL ist hier also kein Hinweis auf die neue Kopf-Subquery, nur "FaHierarchyOrderInfos"
    // waere das.)
    [Fact]
    public void CustomerColumnFilter_Flat_SqlServer_NeverReferencesOrderInfoHead()
    {
        using var ctx = CreateSqlServerContext();
        var repo = new ProductionOrderRepository(ctx);

        var sql = repo.BuildLeitstandQuery(null, null, null, showDone: true,
            new Dictionary<string, string> { ["customer"] = "muster" }, hierarchical: false).ToQueryString();

        sql.Should().NotContain("FaHierarchyOrderInfos", "AKE (Master false) darf nie gegen den Auftragskopf pruefen (F4)");
    }

    /// <summary>
    /// Context gegen den SqlServer-Provider mit einer nicht aufloesbaren Server-Adresse — es wird
    /// NIE eine Verbindung geoeffnet, <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToQueryString"/>
    /// kompiliert nur den Ausdrucksbaum zu SQL-Text. Ausschliesslich fuer SQL-Uebersetzungs-Checks,
    /// nie fuer echte Query-Ausfuehrung (das wuerde einen Verbindungsversuch ausloesen).
    /// </summary>
    private static ApplicationDbContext CreateSqlServerContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=nonexistent-host-for-tests;Database=x;TrustServerCertificate=True;Connect Timeout=1")
            .Options;
        return new ApplicationDbContext(options);
    }
}

using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
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
}
